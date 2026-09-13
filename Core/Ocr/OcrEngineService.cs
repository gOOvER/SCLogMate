using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace SCLogMate.Core.Ocr;

/// <summary>
/// Thread-sicherer Dienst zur Texterkennung mit der nativen Windows.Media.Ocr Engine.
/// Serialisiert Aufrufe über ein SemaphoreSlim, um COMException (Vorgang abgebrochen) zu verhindern.
/// </summary>
public sealed class OcrEngineService : IDisposable
{
    private OcrEngine? _engine;
    private readonly SemaphoreSlim _ocrLock = new(1, 1);
    public bool IsAvailable { get; }

    public OcrEngineService()
    {
        try
        {
            _engine = OcrEngine.TryCreateFromUserProfileLanguages()
                   ?? OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en-US"))
                   ?? OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("de-DE"));
            IsAvailable = _engine != null;
        }
        catch (Exception ex)
        {
            Logger.Error("OcrEngine Init", ex);
            IsAvailable = false;
        }
    }

    /// <summary>Führt Dual-Pass OCR (invertiert + normal) über einem BGRA-Puffer aus.</summary>
    public async Task<(string? InvertedText, string? PlainText)> RecognizeDualPassAsync(
        byte[] bgra, int w, int h, int scale = 2, int padding = 8, bool boostContrast = true)
    {
        if (!IsAvailable || _engine == null || bgra.Length == 0)
            return (null, null);

        await _ocrLock.WaitAsync();
        try
        {
            if (_engine == null) return (null, null);

            var invBuf = Preprocess(bgra, w, h, scale, padding, invert: true, boostContrast: boostContrast, out int iw, out int ih);
            var plainBuf = Preprocess(bgra, w, h, scale, padding, invert: false, boostContrast: boostContrast, out int pw, out int ph);

            using var invBmp = ToSoftwareBitmap(invBuf, iw, ih);
            using var plainBmp = ToSoftwareBitmap(plainBuf, pw, ph);

            var invResult = await _engine.RecognizeAsync(invBmp);
            var plainResult = await _engine.RecognizeAsync(plainBmp);

            return (FormatOcrLines(invResult), FormatOcrLines(plainResult));
        }
        catch (Exception ex)
        {
            Logger.Error("RecognizeDualPass", ex);
            return (null, null);
        }
        finally
        {
            _ocrLock.Release();
        }
    }

    /// <summary>Einfacher OCR-Durchlauf für allgemeinen Text (NexusApp Invert+Kontrast Muster für maximale Geschwindigkeit).</summary>
    public async Task<string?> RecognizeSinglePassAsync(byte[] bgra, int w, int h, int scale = 1, int padding = 12)
    {
        if (!IsAvailable || _engine == null || bgra.Length == 0)
            return null;

        await _ocrLock.WaitAsync();
        try
        {
            if (_engine == null) return null;

            var buf = Preprocess(bgra, w, h, scale, padding, invert: true, boostContrast: true, out int outW, out int outH);
            using var bmp = ToSoftwareBitmap(buf, outW, outH);
            var result = await _engine.RecognizeAsync(bmp);
            return FormatOcrLines(result);
        }
        catch (Exception ex)
        {
            Logger.Error("RecognizeSinglePass", ex);
            return null;
        }
        finally
        {
            _ocrLock.Release();
        }
    }

    /// <summary>
    /// Formatiert das OcrResult unter Erhaltung der physikalischen Zeilenumbrüche (\n),
    /// da OcrResult.Text alle Zeilen zu einem einzigen Fließtext-Absatz ohne Newlines verbindet.
    /// </summary>
    private static string? FormatOcrLines(OcrResult? res)
    {
        if (res == null) return null;
        if (res.Lines != null && res.Lines.Count > 0)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < res.Lines.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(res.Lines[i].Text);
            }
            return sb.ToString();
        }
        return res.Text;
    }

    private static byte[] Preprocess(byte[] bgra, int w, int h, int scale, int padding, bool invert, bool boostContrast, out int outW, out int outH)
    {
        int origW = w;
        int origH = h;
        int step = 1;

        // Falls das Ausgangsbild bereits extrem groß ist, Sub-Sampling verwenden
        if (w > 2600 || h > 2600)
        {
            step = 2;
            w /= 2;
            h /= 2;
            scale = 1;
            padding = 4;
        }

        // Adaptiver Skalierungsfaktor: Zeilenhöhe sollte für Windows.Media.Ocr optimal im Bereich ~35px bis ~90px liegen.
        // Zu hohe Skalierung (z.B. scale=6 bei 144px Bildhöhe) erzeugt Treppeneffekte und deformiert kursive Ziffern (z.B. 8, wird zu 4).
        int maxDim = Math.Max(w, h);
        while (scale > 1 && (h * scale > 450 || (maxDim * scale + padding * 2) > 2600))
        {
            scale--;
        }

        if ((maxDim * scale + padding * 2) > 2600)
        {
            padding = Math.Max(0, (2600 - maxDim * scale) / 2);
        }

        int targetW = w * scale;
        int targetH = h * scale;
        outW = targetW + padding * 2;
        outH = targetH + padding * 2;

        var output = new byte[outW * outH * 4];
        Array.Fill(output, invert ? (byte)255 : (byte)0);

        if (scale == 1)
        {
            for (int sy = 0; sy < h; sy++)
            {
                int srcRow = (sy * step) * origW * 4;
                int dstRow = ((sy + padding) * outW + padding) * 4;
                for (int sx = 0; sx < w; sx++)
                {
                    int src = srcRow + ((sx * step) * 4);
                    int dst = dstRow + (sx * 4);

                    byte b = bgra[src];
                    byte g = bgra[src + 1];
                    byte r = bgra[src + 2];

                    if (boostContrast)
                    {
                        b = (byte)Math.Min(255, (invert ? 255 - b : b) * 14 / 10);
                        g = (byte)Math.Min(255, (invert ? 255 - g : g) * 14 / 10);
                        r = (byte)Math.Min(255, (invert ? 255 - r : r) * 14 / 10);
                    }
                    else if (invert)
                    {
                        b = (byte)(255 - b);
                        g = (byte)(255 - g);
                        r = (byte)(255 - r);
                    }

                    output[dst] = b;
                    output[dst + 1] = g;
                    output[dst + 2] = r;
                    output[dst + 3] = 255;
                }
            }
        }
        else
        {
            // Hochwertige bilineare Interpolation für glatte Glyphen ohne Treppeneffekte
            for (int dy = 0; dy < targetH; dy++)
            {
                float srcY = (dy + 0.5f) / scale - 0.5f;
                int sy0 = Math.Clamp((int)Math.Floor(srcY), 0, h - 1);
                int sy1 = Math.Clamp(sy0 + 1, 0, h - 1);
                float wy1 = srcY - sy0;
                float wy0 = 1.0f - wy1;

                int srcRow0 = (sy0 * step) * origW * 4;
                int srcRow1 = (sy1 * step) * origW * 4;
                int dstRow = ((dy + padding) * outW + padding) * 4;

                for (int dx = 0; dx < targetW; dx++)
                {
                    float srcX = (dx + 0.5f) / scale - 0.5f;
                    int sx0 = Math.Clamp((int)Math.Floor(srcX), 0, w - 1);
                    int sx1 = Math.Clamp(sx0 + 1, 0, w - 1);
                    float wx1 = srcX - sx0;
                    float wx0 = 1.0f - wx1;

                    int p00 = srcRow0 + (sx0 * step * 4);
                    int p10 = srcRow0 + (sx1 * step * 4);
                    int p01 = srcRow1 + (sx0 * step * 4);
                    int p11 = srcRow1 + (sx1 * step * 4);

                    float fb = (bgra[p00] * wx0 + bgra[p10] * wx1) * wy0 + (bgra[p01] * wx0 + bgra[p11] * wx1) * wy1;
                    float fg = (bgra[p00 + 1] * wx0 + bgra[p10 + 1] * wx1) * wy0 + (bgra[p01 + 1] * wx0 + bgra[p11 + 1] * wx1) * wy1;
                    float fr = (bgra[p00 + 2] * wx0 + bgra[p10 + 2] * wx1) * wy0 + (bgra[p01 + 2] * wx0 + bgra[p11 + 2] * wx1) * wy1;

                    byte b = (byte)Math.Clamp((int)Math.Round(fb), 0, 255);
                    byte g = (byte)Math.Clamp((int)Math.Round(fg), 0, 255);
                    byte r = (byte)Math.Clamp((int)Math.Round(fr), 0, 255);

                    if (boostContrast)
                    {
                        b = (byte)Math.Min(255, (invert ? 255 - b : b) * 14 / 10);
                        g = (byte)Math.Min(255, (invert ? 255 - g : g) * 14 / 10);
                        r = (byte)Math.Min(255, (invert ? 255 - r : r) * 14 / 10);
                    }
                    else if (invert)
                    {
                        b = (byte)(255 - b);
                        g = (byte)(255 - g);
                        r = (byte)(255 - r);
                    }

                    int dst = dstRow + (dx * 4);
                    output[dst] = b;
                    output[dst + 1] = g;
                    output[dst + 2] = r;
                    output[dst + 3] = 255;
                }
            }
        }

        return output;
    }

    private static SoftwareBitmap ToSoftwareBitmap(byte[] bgra, int w, int h)
    {
        var bmp = new SoftwareBitmap(BitmapPixelFormat.Bgra8, w, h, BitmapAlphaMode.Ignore);
        bmp.CopyFromBuffer(bgra.AsBuffer());
        return bmp;
    }

    public void Dispose()
    {
        _ocrLock.Wait();
        _engine = null;
        _ocrLock.Release();
        _ocrLock.Dispose();
    }
}
