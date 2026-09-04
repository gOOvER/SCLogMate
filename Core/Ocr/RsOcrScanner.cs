using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using SCLogMate.Core;
using SCLogMate.Models;

namespace SCLogMate.Core.Ocr;

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
        _timer = new Timer(150); // NexusApp Standard: 150ms Loop
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

    public async Task<int?> ScanOnceAsync(bool logDiagnostics = false)
    {
        if (!_ocrEngine.IsAvailable)
        {
            Logger.Log("[RsOcr] OCR-Engine ist auf diesem System nicht verfügbar.");
            return null;
        }

        var region = _regionProvider();
        var (screenW, screenH) = ScreenCapture.GetPrimaryScreenSize();

        int capX, capY, capW, capH;
        if (region != null && region.IsValid)
        {
            // Safety Margin: +30px links/rechts, +15px oben/unten, um HUD-Wackeln / Drift abzufangen
            capX = Math.Max(0, region.X - 30);
            capY = Math.Max(0, region.Y - 15);
            capW = Math.Min(screenW - capX, region.Width + 60);
            capH = Math.Min(screenH - capY, region.Height + 30);
        }
        else
        {
            var def = ScreenCapture.GetDefaultRsRegion();
            capX = def.X;
            capY = def.Y;
            capW = def.Width;
            capH = def.Height;
        }

        if (capW <= 0 || capH <= 0) return null;

        var raw = ScreenCapture.Capture(capX, capY, capW, capH);
        if (raw == null || raw.Length == 0)
        {
            Logger.Log($"[RsOcr] ScreenCapture fehlgeschlagen für {capW}x{capH} @ ({capX},{capY}).");
            return null;
        }

        try
        {
            // Pass 1: Scale 6, Padding 24 (NexusApp Standard - hebt winzige 10-14px HUD-Schriften auf 60-84px)
            var (inv, plain) = await _ocrEngine.RecognizeDualPassAsync(raw, capW, capH, scale: 6, padding: 24);

            if (!string.IsNullOrWhiteSpace(inv))
            {
                var val = ExtractRsValue(inv);
                if (val.HasValue)
                {
                    Logger.Log($"[RsOcr] Treffer in Invert-Pass (Scale 6): {val.Value:N0} RS (Roh: '{inv.Trim()}')");
                    return val.Value;
                }
            }
            if (!string.IsNullOrWhiteSpace(plain))
            {
                var val = ExtractRsValue(plain);
                if (val.HasValue)
                {
                    Logger.Log($"[RsOcr] Treffer in Plain-Pass (Scale 6): {val.Value:N0} RS (Roh: '{plain.Trim()}')");
                    return val.Value;
                }
            }

            // Pass 2 Fallback: Scale 4, Padding 16
            var (inv4, plain4) = await _ocrEngine.RecognizeDualPassAsync(raw, capW, capH, scale: 4, padding: 16);

            if (!string.IsNullOrWhiteSpace(inv4))
            {
                var val = ExtractRsValue(inv4);
                if (val.HasValue)
                {
                    Logger.Log($"[RsOcr] Treffer in Invert-Pass (Scale 4): {val.Value:N0} RS (Roh: '{inv4.Trim()}')");
                    return val.Value;
                }
            }
            if (!string.IsNullOrWhiteSpace(plain4))
            {
                var val = ExtractRsValue(plain4);
                if (val.HasValue)
                {
                    Logger.Log($"[RsOcr] Treffer in Plain-Pass (Scale 4): {val.Value:N0} RS (Roh: '{plain4.Trim()}')");
                    return val.Value;
                }
            }

            // Pass 3 Fallback: Scale 2, Padding 10
            var (inv2, plain2) = await _ocrEngine.RecognizeDualPassAsync(raw, capW, capH, scale: 2, padding: 10);

            if (!string.IsNullOrWhiteSpace(inv2))
            {
                var val = ExtractRsValue(inv2);
                if (val.HasValue)
                {
                    Logger.Log($"[RsOcr] Treffer in Invert-Pass (Scale 2): {val.Value:N0} RS (Roh: '{inv2.Trim()}')");
                    return val.Value;
                }
            }
            if (!string.IsNullOrWhiteSpace(plain2))
            {
                var val = ExtractRsValue(plain2);
                if (val.HasValue)
                {
                    Logger.Log($"[RsOcr] Treffer in Plain-Pass (Scale 2): {val.Value:N0} RS (Roh: '{plain2.Trim()}')");
                    return val.Value;
                }
            }
            if (!string.IsNullOrWhiteSpace(inv) || !string.IsNullOrWhiteSpace(plain))
            {
                if ((DateTime.UtcNow - _lastNoMatchLogTime).TotalSeconds >= 2.0)
                {
                    _lastNoMatchLogTime = DateTime.UtcNow;
                    Logger.Log($"[RsOcr] Kein RS-Treffer für OCR-Text: Invert='{inv?.Trim()}', Plain='{plain?.Trim()}'");
                }
            }

            if (logDiagnostics)
            {
                Logger.Log($"[RsOcr] Kein RS-Treffer bei {capW}x{capH} @ ({capX},{capY}). OCR-Text: Invert='{inv?.Trim()}', Plain='{plain?.Trim()}'");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"[RsOcr] Fehler beim OCR-Scan: {ex.Message}");
        }

        return null;
    }

    private DateTime _lastNoMatchLogTime = DateTime.MinValue;

    private int _lastEmitted;
    private DateTime _lastEmittedTime = DateTime.MinValue;

    private int? ConfirmDebounce(int? val)
    {
        if (!val.HasValue) return null;

        // Entprellen: denselben Wert nicht innerhalb von 2 Sekunden erneut triggern
        if (val.Value == _lastEmitted && (DateTime.UtcNow - _lastEmittedTime).TotalSeconds < 2.0)
            return null;

        _lastEmitted = val.Value;
        _lastEmittedTime = DateTime.UtcNow;
        return val.Value;
    }

    private async void OnTick(object? sender, ElapsedEventArgs e)
    {
        if (!_running) return;

        if (_autoScanEnabled())
        {
            try
            {
                var detected = await ScanOnceAsync(logDiagnostics: false);
                if (detected.HasValue)
                {
                    Logger.Log($"[RsOcr] Signal erfasst: {detected.Value:N0} RS");
                }
                var confirmed = ConfirmDebounce(detected);
                if (confirmed.HasValue)
                {
                    _lastDetectedRs = confirmed.Value;
                    Logger.Log($"[RsOcr] Signal bestätigt & gesendet: {confirmed.Value:N0} RS");
                    RsValueDetected?.Invoke(confirmed.Value);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[RsOcr] Auto-Scan Tick Fehler: {ex.Message}");
            }
        }

        if (_running && _timer != null)
        {
            try { _timer.Start(); }
            catch (ObjectDisposedException) { }
        }
    }

    [GeneratedRegex(@"(?i)\b(?:RS|RADAR|SIG|SIGNATURE)[\s:=-]*([0-9OolI\.,\s]{3,12})")]
    private static partial Regex ExplicitRsPrefixRegex();

    [GeneratedRegex(@"(?i)([0-9OolI\.,\s]{3,12})[\s:=-]*(?:RS|RADAR|SIG|SIGNATURE)\b")]
    private static partial Regex ExplicitRsSuffixRegex();

    [GeneratedRegex(@"(?<!\d)(\d{1,3})[\s.,'’]+(\d{3})(?!\d)")]
    private static partial Regex FormattedThousandsRegex();

    [GeneratedRegex(@"(?<!\d)(\d{1,2})[\s.,'’]+1(\d{3})(?!\d)")]
    private static partial Regex FormattedThousandsMisreadCommaRegex();

    [GeneratedRegex(@"(?<!\d)(\d{1,2})[\s.,'’]+(\d{3})1(?!\d)")]
    private static partial Regex TrailingOneThousandsRegex();

    [GeneratedRegex(@"(?<!\d)(\d{1,2})[\s.,'’]+(\d{2})(?!\d)")]
    private static partial Regex TruncatedZeroThousandsRegex();

    [GeneratedRegex(@"(?<!\d)(\d{4,6})(?!\d)")]
    private static partial Regex PlainDigitsRegex();

    [GeneratedRegex(@"(?i)\b(?:[Il|!][OoQq0]|[1][OoQq])(?=[\s.,'’\d])")]
    private static partial Regex TenPrefixRegex();

    [GeneratedRegex(@"(?<=[\s.,'’])(?:[Il|!][OoQq0]|[1][OoQq])\b")]
    private static partial Regex TenSuffixRegex();

    [GeneratedRegex(@"(?i)\b[Zz](?=[0-9Il|!])")]
    private static partial Regex ZPrefixRegex();

    public static int? ExtractRsValue(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var candidates = new HashSet<int>();

        // 1. Explizite "RS: 7200" oder "7,200 RS" Marker prüfen
        int? explicitVal = null;
        var prefixMatch = ExplicitRsPrefixRegex().Match(text);
        if (prefixMatch.Success)
        {
            explicitVal = CleanAndParseNumber(prefixMatch.Groups[1].Value);
        }
        else
        {
            var suffixMatch = ExplicitRsSuffixRegex().Match(text);
            if (suffixMatch.Success)
            {
                explicitVal = CleanAndParseNumber(suffixMatch.Groups[1].Value);
            }
        }

        if (explicitVal.HasValue)
        {
            AddCandidateWithVariants(candidates, explicitVal.Value);
        }

        // 2. Text säubern & Tausendertrennzeichen vereinheitlichen
        var sanitized = SanitizeOcrText(text);

        // A) Trennzeichen fälschlich als '.1' gelesen (z. B. 10.1200 -> 10200 oder 7.1200 -> 7200)
        var recoveredCommaDot = FormattedThousandsMisreadCommaRegex().Replace(sanitized, "$1$2");

        // B) Nachfolgende HUD-Klammer ']' fälschlich als '1' gelesen (z. B. 21.3501 -> 21350)
        var recoveredTrailing1 = TrailingOneThousandsRegex().Replace(recoveredCommaDot, "$1$2");

        // C) Abgeschnittene End-Null nach Trennzeichen (z. B. 21.35 -> 21350 oder 10.20 -> 10200)
        var recoveredTruncated = TruncatedZeroThousandsRegex().Replace(recoveredTrailing1, "${1}${2}0");

        // D) Standard-Tausendertrennzeichen (z. B. 10.200 oder 21,350 -> 21350)
        var normalized = FormattedThousandsRegex().Replace(recoveredTruncated, "$1$2");

        // 3. Alle plausiblen 4- bis 6-stelligen Zahlenfolgen sammeln
        foreach (Match m in PlainDigitsRegex().Matches(normalized))
        {
            if (int.TryParse(m.Value, out int v))
            {
                AddCandidateWithVariants(candidates, v);
            }
        }

        if (candidates.Count == 0) return null;

        var orderedCandidates = candidates.OrderByDescending(c => c).ToList();

        // 4. Intelligente Priorisierung mit dem Star Citizen Erz-Katalog
        // Priorität 1: Expliziter RS-Wert mit exaktem Treffer
        if (explicitVal.HasValue && candidates.Contains(explicitVal.Value))
        {
            var expMatches = RsDecoderCatalog.Decode(explicitVal.Value);
            if (expMatches.Count > 0 && expMatches[0].IsExact)
            {
                return explicitVal.Value;
            }
        }

        // Priorität 2: Exakter Katalog-Treffer (z. B. 21350, 10200, 7200, 3400, 3170, 3600, 2000, 14400, 10800)
        // Höchste Werte zuerst prüfen, damit 21.350 / 10.200 Vorrang vor HUD-Rauschen wie 2.000 haben
        foreach (var cand in orderedCandidates)
        {
            var matches = RsDecoderCatalog.Decode(cand);
            if (matches.Count > 0 && matches[0].IsExact)
            {
                return cand;
            }
        }

        // Priorität 3: Expliziter RS-Wert (auch ohne exakten Treffer)
        if (explicitVal.HasValue && candidates.Contains(explicitVal.Value))
        {
            return explicitVal.Value;
        }

        // Priorität 4: Fuzzy-Treffer (innerhalb 0.6% Toleranz), höchster Wert zuerst
        foreach (var cand in orderedCandidates)
        {
            var matches = RsDecoderCatalog.Decode(cand);
            if (matches.Count > 0)
            {
                return cand;
            }
        }

        // Kein Treffer im Star Citizen Katalog und kein expliziter "RS:" Marker:
        // Kein RS-Signal zurückgeben (verhindert Fehllesungen von HUD-Rauschen wie 222.000, 11.000 oder 9.110).
        return null;
    }

    private static void AddCandidateWithVariants(HashSet<int> candidates, int v)
    {
        if (v < 1000 || v > 300000) return;
        candidates.Add(v);

        // 1. Ziffernverwechslung '8' <-> '3' am Anfang von 4-stelligen Werten:
        // Star Citizen Top-Erze haben Signaturen zwischen 3170 und 3900 (Lindinium 3400, Quantanium 3170, Bexalite 3600 usw.).
        // Kein Star Citizen Erz hat eine 4-stellige Signatur mit 8xxx.
        if (v >= 8000 && v <= 8999)
        {
            candidates.Add(v - 5000);
        }

        string s = v.ToString();

        // 2. Tausendertrennzeichen ',' fälschlich als '1' gelesen:
        // A) 4-stellige RS-Werte (z. B. 7,200 -> 71.200 -> 71200 oder 3,400 -> 31400):
        if (s.Length == 5 && s[1] == '1')
        {
            if (int.TryParse($"{s[0]}{s[2..]}", out int dropped1) && dropped1 >= 1000)
            {
                candidates.Add(dropped1);
                if (dropped1 >= 8000 && dropped1 <= 8999)
                {
                    candidates.Add(dropped1 - 5000);
                }
            }
        }

        // B) 5-stellige RS-Werte (z. B. 21,350 -> 211.350 -> 211350 oder 10,200 -> 101.200 -> 101200):
        if (s.Length == 6 && s[2] == '1')
        {
            if (int.TryParse($"{s[..2]}{s[3..]}", out int droppedComma1) && droppedComma1 >= 1000)
            {
                candidates.Add(droppedComma1);
                if (droppedComma1 >= 8000 && droppedComma1 <= 8999)
                {
                    candidates.Add(droppedComma1 - 5000);
                }
                // '8' an 3. Stelle als '3' probieren (z. B. 211850 -> 21850 -> 21350):
                string dStr = droppedComma1.ToString();
                if (dStr.Length == 5 && dStr[2] == '8')
                {
                    if (int.TryParse($"{dStr[..2]}3{dStr[3..]}", out int rec3B) && rec3B >= 1000)
                    {
                        candidates.Add(rec3B);
                    }
                }
            }
        }

        // 3. Führendes HUD-Element / Klammer als '1' gelesen bei 5-stelligen Zahlen (z. B. [ 7.200 oder | 7.200 -> 17200):
        // WICHTIG: s[1] != '0' schützt 5-stellige 10xxx Werte (wie 10.200 oder 10.800) vor fälschlicher Verkürzung!
        if (s.Length == 5 && s[0] == '1' && s[1] != '0')
        {
            if (int.TryParse(s[1..], out int droppedLead1) && droppedLead1 >= 1000)
            {
                candidates.Add(droppedLead1);
                if (droppedLead1 >= 8000 && droppedLead1 <= 8999)
                {
                    candidates.Add(droppedLead1 - 5000);
                }
            }
        }

        // 4. Führendes HUD-Element bei 6-stelligen Zahlen mit führender '1' vor 5-stelligen Zahlen (z. B. | 21.350 -> 121350 oder | 10.200 -> 110200):
        if (s.Length == 6 && s[0] == '1')
        {
            if (int.TryParse(s[1..], out int droppedLead1Six) && droppedLead1Six >= 1000)
            {
                candidates.Add(droppedLead1Six);
                string leadStr = droppedLead1Six.ToString();
                if (leadStr.Length == 5 && leadStr[2] == '8')
                {
                    if (int.TryParse($"{leadStr[..2]}3{leadStr[3..]}", out int rec3Lead) && rec3Lead >= 1000)
                    {
                        candidates.Add(rec3Lead);
                    }
                }
            }
        }

        // 5. Schließendes HUD-Element bei 6-stelligen Zahlen mit nachfolgender '1' (z. B. 21.350 ] -> 213501):
        if (s.Length == 6 && s[^1] == '1')
        {
            if (int.TryParse(s[..^1], out int droppedTrail1Six) && droppedTrail1Six >= 1000)
            {
                candidates.Add(droppedTrail1Six);
            }
        }

        // 6. '8' <-> '3' Verwechslung bei 5-stelligen Werten:
        // A) an 2. Stelle (z. B. 18.600 -> 13.600 für 4x Lindinium):
        if (s.Length == 5 && s[0] == '1' && s[1] == '8')
        {
            if (int.TryParse($"13{s[2..]}", out int recovered3) && recovered3 >= 1000)
            {
                candidates.Add(recovered3);
            }
        }
        // B) an 3. Stelle (z. B. 21.850 -> 21.350 für 5x Iron):
        if (s.Length == 5 && s[2] == '8')
        {
            if (int.TryParse($"{s[..2]}3{s[3..]}", out int recovered3Middle) && recovered3Middle >= 1000)
            {
                candidates.Add(recovered3Middle);
            }
        }

        // 7. '7' <-> '2' Verwechslung am Anfang von 5-stelligen Zahlen (z. B. 71.350 -> 21.350):
        if (s.Length == 5 && s[0] == '7' && s[1] == '1')
        {
            if (int.TryParse($"2{s[1..]}", out int recovered2) && recovered2 >= 1000)
            {
                candidates.Add(recovered2);
            }
        }

        // 8. Fehlende End-Null (z. B. 2135 -> 21350):
        if (v >= 1000 && v <= 30000 && v % 10 != 0)
        {
            int withZero = v * 10;
            if (withZero <= 300000)
            {
                candidates.Add(withZero);
            }
        }
    }

    private static int? CleanAndParseNumber(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var sb = new StringBuilder(raw.Length);
        foreach (char c in raw)
        {
            if (char.IsDigit(c))
            {
                sb.Append(c);
            }
            else if (c is 'O' or 'o' or 'Q')
            {
                sb.Append('0');
            }
            else if (c is 'l' or 'I' or '|')
            {
                sb.Append('1');
            }
            else if (c is 'Z' or 'z')
            {
                sb.Append('2');
            }
            else if (c is 'S' or 's')
            {
                sb.Append('5');
            }
            else if (c is 'B')
            {
                sb.Append('8');
            }
        }

        if (int.TryParse(sb.ToString(), out int val))
            return val;

        return null;
    }

    private static string SanitizeOcrText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        // 0. Entfernungs- und Geschwindigkeitsangaben vom HUD entfernen (z. B. "16.5km", "2.0km", "20.2km", "500m/s", "17.0bn")
        string result = Regex.Replace(text, @"(?i)\b\d+[\.,]?\d*\s*(?:km|m\/s|bn|deg|kts)\b", " ");

        // 1. Vorab: Spezifische 'IO' / 'lO' / '|O' / '1O' / 'I0' Kombinationen vor Trennzeichen/Ziffern zu '10' konvertieren
        result = TenPrefixRegex().Replace(result, "10");
        result = TenSuffixRegex().Replace(result, "10");

        // Z/z vor Ziffern als 2 interpretieren (z. B. "Z1.350" -> "21.350")
        result = ZPrefixRegex().Replace(result, "2");

        // Spezifische SO/So am Ende nach Ziffern (z. B. "21.3SO" -> "21.350")
        result = Regex.Replace(result, @"(?i)(?<=\d[\.,])(\d)[Ss][OoQq]", "${1}50");

        // 2. Iteratives Ersetzen: Bis zu 4 Durchläufe für mehrstellige Ketten wie "IO.2OO" -> "10.200"
        char[] chars = result.ToCharArray();
        bool changed = true;
        int iterations = 0;
        while (changed && iterations++ < 4)
        {
            changed = false;
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (c is 'O' or 'o' or 'Q' or 'D')
                {
                    bool nearDigit = (i > 0 && (char.IsDigit(chars[i - 1]) || chars[i - 1] is '.' or ',' or '\'')) ||
                                     (i < chars.Length - 1 && (char.IsDigit(chars[i + 1]) || chars[i + 1] is '.' or ',' or '\''));
                    if (nearDigit)
                    {
                        chars[i] = '0';
                        changed = true;
                    }
                }
                else if (c is 'l' or 'I' or '|' or '!')
                {
                    bool nearDigit = (i > 0 && (char.IsDigit(chars[i - 1]) || chars[i - 1] is '.' or ',' or '\'')) ||
                                     (i < chars.Length - 1 && (char.IsDigit(chars[i + 1]) || chars[i + 1] is '.' or ',' or '\''));
                    if (nearDigit)
                    {
                        chars[i] = '1';
                        changed = true;
                    }
                }
                else if (c is 'Z' or 'z')
                {
                    bool nearDigit = (i > 0 && (char.IsDigit(chars[i - 1]) || chars[i - 1] is '.' or ',' or '\'')) ||
                                     (i < chars.Length - 1 && (char.IsDigit(chars[i + 1]) || chars[i + 1] is '.' or ',' or '\''));
                    if (nearDigit)
                    {
                        chars[i] = '2';
                        changed = true;
                    }
                }
                else if (c is 'S' or 's')
                {
                    // 'S' nur zu '5' wenn beidseitig von Ziffern umgeben (verhindert 'RS' -> 'R5')
                    bool betweenDigits = (i > 0 && char.IsDigit(chars[i - 1])) &&
                                         (i < chars.Length - 1 && char.IsDigit(chars[i + 1]));
                    if (betweenDigits)
                    {
                        chars[i] = '5';
                        changed = true;
                    }
                }
                else if (c is 'B')
                {
                    bool nearDigit = (i > 0 && char.IsDigit(chars[i - 1])) ||
                                     (i < chars.Length - 1 && char.IsDigit(chars[i + 1]));
                    if (nearDigit)
                    {
                        chars[i] = '8';
                        changed = true;
                    }
                }
            }
        }

        return new string(chars);
    }

    public void Dispose()
    {
        Stop();
    }
}
