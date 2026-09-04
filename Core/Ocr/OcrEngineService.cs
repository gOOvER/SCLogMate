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
        byte[] bgra, int w, int h, int scale = 2, int padding = 8)
    {
        if (!IsAvailable || _engine == null || bgra.Length == 0)
            return (null, null);

        await _ocrLock.WaitAsync();
        try
        {
            if (_engine == null) return (null, null);

            var invBuf = Preprocess(bgra, w, h, scale, padding, invert: true, out int iw, out int ih);
            var plainBuf = Preprocess(bgra, w, h, scale, padding, invert: false, out int pw, out int ph);

            using var invBmp = ToSoftwareBitmap(invBuf, iw, ih);
            using var plainBmp = ToSoftwareBitmap(plainBuf, pw, ph);

            var invResult = await _engine.RecognizeAsync(invBmp);
            var plainResult = await _engine.RecognizeAsync(plainBmp);

            return (invResult?.Text, plainResult?.Text);
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

            var buf = Preprocess(bgra, w, h, scale, padding, invert: true, out int outW, out int outH);
            using var bmp = ToSoftwareBitmap(buf, outW, outH);
            var result = await _engine.RecognizeAsync(bmp);
            return result?.Text;
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

    private static byte[] Preprocess(byte[] bgra, int w, int h, int scale, int padding, bool invert, out int outW, out int outH)
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

        // Windows OCR Limit: MaxImageDimension = 2600 Pixel
        int maxDim = Math.Max(w, h);
        while (scale > 1 && (maxDim * scale + padding * 2) > 2600)
        {
            scale--;
        }

        if ((maxDim * scale + padding * 2) > 2600)
        {
            padding = Math.Max(0, (2600 - maxDim * scale) / 2);
        }

        outW = w * scale + padding * 2;
        outH = h * scale + padding * 2;

        var output = new byte[outW * outH * 4];
        Array.Fill(output, invert ? (byte)255 : (byte)0);

        for (int sy = 0; sy < h; sy++)
        {
            int srcRow = (sy * step) * origW * 4;
            for (int sx = 0; sx < w; sx++)
            {
                int src = srcRow + ((sx * step) * 4);

                // Farbiger Star Citizen HUD-Text (Bernstein, Orange, Cyan, Grün, Weiß)
                // Nutze maximale Farbkanalintensität für optimalen Signal-Rausch-Abstand
                int maxColor = Math.Max(bgra[src], Math.Max(bgra[src + 1], bgra[src + 2]));

                byte v;
                if (invert)
                {
                    // Invertieren: Dunkler Text auf reinem Weiß
                    // Text (maxColor > 60) wird tiefschwarz (0..20), dunkler Weltraum (<45) wird reinweiß (255)
                    v = (byte)Math.Clamp(255 - (maxColor - 45) * 3, 0, 255);
                }
                else
                {
                    // Plain: Heller Text auf reinem Schwarz
                    // Text (maxColor > 60) wird reinweiß (235..255), dunkler Weltraum (<45) wird pechschwarz (0)
                    v = (byte)Math.Clamp((maxColor - 45) * 3, 0, 255);
                }

                if (scale == 1)
                {
                    int dst = ((sy + padding) * outW + (sx + padding)) * 4;
                    output[dst] = v;
                    output[dst + 1] = v;
                    output[dst + 2] = v;
                    output[dst + 3] = 255;
                }
                else
                {
                    for (int dy = 0; dy < scale; dy++)
                    {
                        int dstRow = ((sy * scale + dy + padding) * outW + padding) * 4;
                        for (int dx = 0; dx < scale; dx++)
                        {
                            int dst = dstRow + ((sx * scale + dx) * 4);
                            output[dst] = v;
                            output[dst + 1] = v;
                            output[dst + 2] = v;
                            output[dst + 3] = 255;
                        }
                    }
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
