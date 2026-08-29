using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace SCLogReader.Core.Ocr;

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

            var invBmp = ToSoftwareBitmap(invBuf, iw, ih);
            var plainBmp = ToSoftwareBitmap(plainBuf, pw, ph);

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

        try
        {
            if (_engine == null) return null;

            var buf = Preprocess(bgra, w, h, scale, padding, invert: true, out int outW, out int outH);
            var bmp = ToSoftwareBitmap(buf, outW, outH);
            var result = await _engine.RecognizeAsync(bmp);
            return result?.Text;
        }
        catch (Exception ex)
        {
            Logger.Error("RecognizeSinglePass", ex);
            return null;
        }
    }

    private static byte[] Preprocess(byte[] bgra, int w, int h, int scale, int padding, bool invert, out int outW, out int outH)
    {
        outW = w * scale + padding * 2;
        outH = h * scale + padding * 2;

        var output = new byte[outW * outH * 4];
        Array.Fill(output, invert ? (byte)255 : (byte)0);

        for (int sy = 0; sy < h; sy++)
        {
            int srcRow = sy * w * 4;
            for (int sx = 0; sx < w; sx++)
            {
                int src = srcRow + (sx * 4);
                byte ib = invert ? (byte)Math.Min(255, (255 - bgra[src]) * 14 / 10) : bgra[src];
                byte ig = invert ? (byte)Math.Min(255, (255 - bgra[src + 1]) * 14 / 10) : bgra[src + 1];
                byte ir = invert ? (byte)Math.Min(255, (255 - bgra[src + 2]) * 14 / 10) : bgra[src + 2];

                if (scale == 1)
                {
                    int dst = ((sy + padding) * outW + (sx + padding)) * 4;
                    output[dst] = ib;
                    output[dst + 1] = ig;
                    output[dst + 2] = ir;
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
                            output[dst] = ib;
                            output[dst + 1] = ig;
                            output[dst + 2] = ir;
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
        _engine = null;
        _ocrLock.Dispose();
    }
}
