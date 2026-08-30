using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using SCLogReader.Models;

namespace SCLogReader.Core.Ocr;

public sealed partial class RsOcrScanner : IDisposable
{
    private readonly OcrEngineService _ocrEngine;
    private readonly Func<ScanRegion?> _regionProvider;
    private readonly Func<bool> _autoScanEnabled;
    private Timer? _timer;
    private bool _running;
    private int _lastDetectedRs = -1;
    private DateTime _lastDetectionTime = DateTime.MinValue;

    public event Action<int>? RsValueDetected;
    public event Action<string>? StatusChanged;

    public bool IsRunning => _running;
    public bool IsAvailable => _ocrEngine.IsAvailable;

    public RsOcrScanner(OcrEngineService ocrEngine, Func<ScanRegion?> regionProvider, Func<bool> autoScanEnabled)
    {
        _ocrEngine = ocrEngine;
        _regionProvider = regionProvider;
        _autoScanEnabled = autoScanEnabled;
    }

    public void Start()
    {
        if (_running) return;
        _running = true;
        StatusChanged?.Invoke("⚡ Auto-Scan aktiv");
        _timer = new Timer(350);
        _timer.Elapsed += OnTick;
        _timer.AutoReset = false;
        _timer.Start();
    }

    public void Stop()
    {
        _running = false;
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
        StatusChanged?.Invoke("Auto-Scan gestoppt");
    }

    public async Task<int?> ScanOnceAsync()
    {
        if (!_ocrEngine.IsAvailable) return null;

        var region = _regionProvider();
        byte[]? raw;
        int w, h;

        if (region != null && region.IsValid)
        {
            raw = ScreenCapture.Capture(region.X, region.Y, region.Width, region.Height);
            w = region.Width;
            h = region.Height;
        }
        else
        {
            // Standard: Zentraler HUD- & Scanner-Bereich des primären Bildschirms
            var (sw, sh) = ScreenCapture.GetPrimaryScreenSize();
            if (sw <= 0 || sh <= 0) return null;

            int cw = Math.Min(1200, (int)(sw * 0.70));
            int ch = Math.Min(800, (int)(sh * 0.55));
            int cx = (sw - cw) / 2;
            int cy = (sh - ch) / 2;

            raw = ScreenCapture.Capture(cx, cy, cw, ch);
            w = cw;
            h = ch;
        }

        if (raw == null || raw.Length == 0) return null;

        try
        {
            var text = await _ocrEngine.RecognizeSinglePassAsync(raw, w, h, scale: 3, padding: 12);
            if (!string.IsNullOrWhiteSpace(text))
            {
                var val = ExtractRsValue(text);
                if (val.HasValue) return val.Value;
            }

            // Dual Pass Fallback
            var (inv, plain) = await _ocrEngine.RecognizeDualPassAsync(raw, w, h, scale: 3, padding: 12);
            if (!string.IsNullOrWhiteSpace(inv))
            {
                var val = ExtractRsValue(inv);
                if (val.HasValue) return val.Value;
            }
            if (!string.IsNullOrWhiteSpace(plain))
            {
                var val = ExtractRsValue(plain);
                if (val.HasValue) return val.Value;
            }
        }
        catch { }

        return null;
    }

    private async void OnTick(object? sender, ElapsedEventArgs e)
    {
        if (!_running) return;

        if (_autoScanEnabled())
        {
            try
            {
                var detected = await ScanOnceAsync();
                if (detected.HasValue)
                {
                    var now = DateTime.UtcNow;
                    if (detected.Value != _lastDetectedRs || (now - _lastDetectionTime).TotalSeconds > 1.5)
                    {
                        _lastDetectedRs = detected.Value;
                        _lastDetectionTime = now;
                        RsValueDetected?.Invoke(detected.Value);
                    }
                }
            }
            catch { }
        }

        if (_running && _timer != null)
        {
            _timer.Start();
        }
    }

    [GeneratedRegex(@"(?i)\b(?:RS|RADAR|SIG|SIGNATURE)[\s:=-]*([0-9][0-9\.,\s]{2,7})")]
    private static partial Regex ExplicitRsRegex();

    [GeneratedRegex(@"(?<!\d)(\d)\s+(\d{3})(?!\d)")]
    private static partial Regex SplitThousandsRegex();

    public static int? ExtractRsValue(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        // 1. Zuerst nach explizitem "RS: 7200" oder "RS 7.200" Marker suchen
        var explicitMatch = ExplicitRsRegex().Match(text);
        if (explicitMatch.Success)
        {
            var rawNum = explicitMatch.Groups[1].Value.Replace(".", "").Replace(",", "").Replace(" ", "").Trim();
            if (int.TryParse(rawNum, out var expVal) && expVal >= 1000 && expVal <= 300000)
            {
                return expVal;
            }
        }

        // 2. Tausenderpunkte und Kommas zwischen Ziffern bereinigen ("7.200" -> "7200")
        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if ((c == ',' || c == '.' || c == '\'') &&
                i > 0 && i < text.Length - 1 &&
                char.IsDigit(text[i - 1]) && char.IsDigit(text[i + 1]))
            {
                continue;
            }
            sb.Append(c);
        }

        var normalized = SplitThousandsRegex().Replace(sb.ToString(), "$1$2");

        // 3. Alle Zahlenfolgen sammeln
        var candidates = new List<int>();
        int start = -1;
        for (int i = 0; i <= normalized.Length; i++)
        {
            bool isDigit = i < normalized.Length && char.IsDigit(normalized[i]);
            if (isDigit && start == -1)
            {
                start = i;
            }
            else if (!isDigit && start != -1)
            {
                int len = i - start;
                if (len >= 4 && len <= 6 &&
                    int.TryParse(normalized.AsSpan(start, len), out var val) &&
                    val >= 1000 && val <= 300000)
                {
                    candidates.Add(val);
                }
                start = -1;
            }
        }

        if (candidates.Count == 0) return null;

        // 4. Intelligente Priorisierung: Match gegen den Star Citizen Erz-Katalog
        foreach (var cand in candidates)
        {
            var matches = RsDecoderCatalog.Decode(cand);
            if (matches.Count > 0 && matches[0].IsExact)
            {
                return cand; // Perfekter Treffer (z. B. 7200, 3170, 3600, 2000, 14400)
            }
        }

        foreach (var cand in candidates)
        {
            var matches = RsDecoderCatalog.Decode(cand);
            if (matches.Count > 0)
            {
                return cand; // Fuzzy Treffer (innerhalb 0.6% Toleranz)
            }
        }

        // Falls kein bekannter Katalog-Treffer: erster plausibler Kandidat
        return candidates[0];
    }

    public void Dispose()
    {
        Stop();
    }
}
