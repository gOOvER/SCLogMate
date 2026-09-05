using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;
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
    private int _busy;
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
        _pending = 0;
        _pendingCount = 0;
        _lastEmitted = 0;
        _missedFrames = 0;
        // 200 ms Intervall (schnelle Reaktion auf 1,5s Radar-Pings, ~20ms Scan-Dauer, <1% CPU)
        _timer = new Timer(200) { AutoReset = false };
        _timer.Elapsed += OnTick;
        _timer.Start();
        StatusChanged?.Invoke("Auto-Scan aktiv (Intervall: 200 ms)");
    }

    public void Stop()
    {
        _running = false;
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
        _pending = 0;
        _pendingCount = 0;
        _lastEmitted = 0;
        _missedFrames = 0;
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
            // Genau die vom Benutzer kalibrierte Region erfassen (NexusApp Standard - keine Verfälschung durch Margins)
            capX = region.X;
            capY = region.Y;
            capW = region.Width;
            capH = region.Height;
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
            // 4x Upscaling (wie in sc-ore-scanner) mit Invert + 1.4x Contrast Boost:
            // 4x erzeugt bei hochauflösenden Displays weniger Weichzeichner als 6x und ist ressourcenschonender.
            var ocrText = await _ocrEngine.RecognizeSinglePassAsync(raw, capW, capH, scale: 4, padding: 24);
            if (!string.IsNullOrWhiteSpace(ocrText))
            {
                var val = ExtractRsValue(ocrText);
                if (val.HasValue)
                {
                    Logger.Log($"[RsOcr] Signal erkannt: {val.Value:N0} RS (OCR: '{ocrText.Trim()}')");
                    return val.Value;
                }
                else if (logDiagnostics || (DateTime.UtcNow - _lastNoMatchLogTime).TotalSeconds >= 2.0)
                {
                    _lastNoMatchLogTime = DateTime.UtcNow;
                    Logger.Log($"[RsOcr] Kein RS-Kandidat in OCR-Text: '{ocrText.Trim()}' (Bereich: {capW}x{capH} @ ({capX},{capY}))");
                }
            }
            else if (logDiagnostics)
            {
                Logger.Log($"[RsOcr] OCR ergab leeren Text für Bereich {capW}x{capH} @ ({capX},{capY})");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"[RsOcr] Fehler beim OCR-Scan: {ex.Message}");
        }

        return null;
    }

    private DateTime _lastNoMatchLogTime = DateTime.MinValue;

    private int _pending;
    private int _pendingCount;
    private int _lastEmitted;
    private int _missedFrames;

    private int? ConfirmDebounce(int? val)
    {
        if (!val.HasValue)
        {
            _missedFrames++;
            // Nach 3 Ticks ohne Signal (~600 ms) Zustände für den nächsten Ping freigeben
            if (_missedFrames >= 3)
            {
                _pending = 0;
                _pendingCount = 0;
                _lastEmitted = 0;
            }
            return null;
        }

        _missedFrames = 0;

        // 1. Exakter Treffer im Star Citizen Katalog (z. B. 7200, 3400, 2000, 8000, 10200, 21350):
        // Star Citizen Signaturen sind spezifisch - ein exakter Katalogwert ist NIEMALS Zufallsrauschen!
        // Sofort auslösen beim ersten Erkennen, damit der Nutzer nicht auf 2 Frames warten muss!
        bool isCatalogMatch = RsDecoderCatalog.Decode(val.Value).Any(m => m.IsExact);
        if (isCatalogMatch)
        {
            if (val.Value != _lastEmitted)
            {
                _lastEmitted = val.Value;
                _pending = val.Value;
                _pendingCount = 1;
                return val.Value;
            }
            return null;
        }

        // 2. Nicht im Katalog gelistete Signaturen (z. B. Wracks oder ungelistete Ping-Echoes):
        // 2 aufeinanderfolgende Frames zur Rauschfilterung fordern
        if (val.Value == _pending)
            _pendingCount++;
        else
        {
            _pending = val.Value;
            _pendingCount = 1;
        }

        if (_pendingCount >= 2 && val.Value != _lastEmitted)
        {
            _lastEmitted = val.Value;
            return val.Value;
        }

        return null;
    }

    private async void OnTick(object? sender, ElapsedEventArgs e)
    {
        if (!_running) return;
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return;

        try
        {
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
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);

            if (_running && _timer != null)
            {
                try { _timer.Start(); }
                catch (ObjectDisposedException) { }
            }
        }
    }

    [GeneratedRegex(@"(?i)\b(?:RS|RADAR|SIG|SIGNATURE)[\s:=-]*([0-9OolI\.,\s]{3,12})")]
    private static partial Regex ExplicitRsPrefixRegex();

    [GeneratedRegex(@"(?i)([0-9OolI\.,\s]{3,12})[\s:=-]*(?:RS|RADAR|SIG|SIGNATURE)\b")]
    private static partial Regex ExplicitRsSuffixRegex();

    [GeneratedRegex(@"(?<!\d)(\d) (\d{3})(?!\d)")]
    private static partial Regex SplitThousandsRegex();

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

        // Vorab: Star Citizen HUD OCR Glyphen säubern (z. B. 'Ø'/'ø' -> '0', '*' -> '.', 'IO' -> '10' etc.)
        var sanitized = SanitizeOcrText(text);

        var candidates = new HashSet<int>();

        // 1. Explizite "RS: 7200" oder "7,200 RS" Marker prüfen (sowohl auf Roh-Text als auch auf Sanitized-Text)
        int? explicitVal = null;
        var prefixMatch = ExplicitRsPrefixRegex().Match(sanitized);
        if (!prefixMatch.Success) prefixMatch = ExplicitRsPrefixRegex().Match(text);

        if (prefixMatch.Success)
        {
            explicitVal = CleanAndParseNumber(prefixMatch.Groups[1].Value);
        }
        else
        {
            var suffixMatch = ExplicitRsSuffixRegex().Match(sanitized);
            if (!suffixMatch.Success) suffixMatch = ExplicitRsSuffixRegex().Match(text);

            if (suffixMatch.Success)
            {
                explicitVal = CleanAndParseNumber(suffixMatch.Groups[1].Value);
            }
        }

        if (explicitVal.HasValue)
        {
            AddCandidateWithVariants(candidates, explicitVal.Value);
        }

        // 2. Token-basierte Prüfung nach Zeilen und Whitespace (analog sc-ore-scanner):
        // Isoliert einzelne Zahlenblöcke und filtert Distanz-Suffixe (z. B. "18.8km") sauber heraus.
        var textLines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var l in textLines)
        {
            var tokens = l.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (Regex.IsMatch(token, @"(?i)\b\d+[\.,]?\d*\s*(?:km|m\/s|bn|deg|kts)\b"))
                    continue;

                var cleaned = token.Replace(",", "").Replace(".", "").Replace("'", "").Replace("’", "").Trim();
                if (int.TryParse(cleaned, out int tVal) && tVal >= 1000 && tVal <= 200000)
                {
                    AddCandidateWithVariants(candidates, tVal);
                }
            }
        }

        var sanitizedLines = sanitized.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var l in sanitizedLines)
        {
            var tokens = l.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                var cleaned = token.Replace(",", "").Replace(".", "").Replace("'", "").Replace("’", "").Trim();
                if (int.TryParse(cleaned, out int tVal) && tVal >= 1000 && tVal <= 200000)
                {
                    AddCandidateWithVariants(candidates, tVal);
                }
            }
        }

        // 3. Direkte Ziffernfolgen (2.000 bis 200.000)
        // Strip commas/periods sitting between two digit characters ("17,200" -> "17200")
        var sb = new StringBuilder(sanitized.Length);
        for (int i = 0; i < sanitized.Length; i++)
        {
            char c = sanitized[i];
            if ((c == ',' || c == '.') &&
                i > 0 && i < sanitized.Length - 1 &&
                char.IsDigit(sanitized[i - 1]) && char.IsDigit(sanitized[i + 1]))
                continue;
            sb.Append(c);
        }

        // Collapse "X XXX" -> "XXXX" for cases where OCR reads the comma as a space
        var normalizedNexus = SplitThousandsRegex().Replace(sb.ToString(), "$1$2");

        int start = -1;
        for (int i = 0; i <= normalizedNexus.Length; i++)
        {
            bool isDigit = i < normalizedNexus.Length && char.IsDigit(normalizedNexus[i]);
            if (isDigit && start == -1)
            {
                start = i;
            }
            else if (!isDigit && start != -1)
            {
                int len = i - start;
                if (len >= 4 && len <= 6 &&
                    int.TryParse(normalizedNexus.AsSpan(start, len), out var val) &&
                    val >= 2000 && val <= 200000)
                {
                    AddCandidateWithVariants(candidates, val);
                }
                start = -1;
            }
        }

        // 3. Ergänzende Muster (Misread-Kommas, abgeschnittene Nullen)
        var recoveredCommaDot = FormattedThousandsMisreadCommaRegex().Replace(sanitized, "$1$2");
        var recoveredTrailing1 = TrailingOneThousandsRegex().Replace(recoveredCommaDot, "$1$2");
        var recoveredTruncated = TruncatedZeroThousandsRegex().Replace(recoveredTrailing1, "${1}${2}0");
        var normalized = FormattedThousandsRegex().Replace(recoveredTruncated, "$1$2");

        // 4. Alle plausiblen 4- bis 6-stelligen Zahlenfolgen sammeln
        foreach (Match m in PlainDigitsRegex().Matches(normalized))
        {
            if (int.TryParse(m.Value, out int v))
            {
                AddCandidateWithVariants(candidates, v);
            }
        }

        if (candidates.Count == 0) return null;

        var orderedCandidates = candidates.OrderByDescending(c => c).ToList();

        // 5. Intelligente Priorisierung mit dem Star Citizen Erz-Katalog
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

        // Priorität 5: Jeder gültige RS-Kandidat im Bereich 2.000 bis 200.000 (NexusApp Standard)
        // Verhindert, dass ungelistete Signaturen, Wracks oder Ping-Zahlen im Scanbereich ignoriert werden!
        foreach (var cand in orderedCandidates)
        {
            if (cand >= 2000 && cand <= 200000)
            {
                return cand;
            }
        }

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

        // 9. Ziffernverwechslung slashed zero '0' <-> '8' / '6' in den letzten beiden Stellen:
        // Im Star Citizen HUD hat die '0' einen Diagonalstrich (Ø), weshalb Windows OCR '0' oft als '8', '6' oder 'B' liest (z. B. 3.480 / 3.488 -> 3.400 für Lindinium).
        if (s.Length >= 4)
        {
            // Beide letzten Ziffern als '00' versuchen:
            if (s.EndsWith("80") || s.EndsWith("88") || s.EndsWith("68") || s.EndsWith("66") || s.EndsWith("08") || s.EndsWith("86") || s.EndsWith("60") || s.EndsWith("06"))
            {
                if (int.TryParse($"{s[..^2]}00", out int recZeroZero) && recZeroZero >= 1000)
                {
                    candidates.Add(recZeroZero);
                }
            }
            // Vorletzte Ziffer '8' oder '6' als '0' versuchen (z. B. 3480 -> 3400):
            if (s[^2] is '8' or '6')
            {
                if (int.TryParse($"{s[..^2]}0{s[^1]}", out int recPenult) && recPenult >= 1000)
                {
                    candidates.Add(recPenult);
                }
            }
            // Letzte Ziffer '8' oder '6' als '0' versuchen (z. B. 3408 -> 3400):
            if (s[^1] is '8' or '6')
            {
                if (int.TryParse($"{s[..^1]}0", out int recUlt) && recUlt >= 1000)
                {
                    candidates.Add(recUlt);
                }
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
            else if (c is 'O' or 'o' or 'Q' or 'Ø' or 'ø' or 'e' or 'E')
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

        // Sternchen zwischen Ziffern zu Punkt vereinheitlichen ("3*400" -> "3.400", "7*2ee" -> "7.2ee")
        result = Regex.Replace(result, @"(?<=\d)\*(?=\d)", ".");
        // Sternchen/Apostroph am Ende als Ziffer 0 ("3.48*" -> "3.480", "3.48''" -> "3.480")
        result = Regex.Replace(result, @"(?<=\d)[\*'\’]{1,2}(?!\d)", "0");

        // B am Anfang vor Ziffer/Punkt/Trennzeichen als 8 (z. B. "Breøø" oder "B.000" -> "8.000")
        result = Regex.Replace(result, @"(?i)\bB(?=[\.\,r\d])", "8");
        // r zwischen Ziffern als Trennzeichen . ("8r000" -> "8.000")
        result = Regex.Replace(result, @"(?<=\d)r(?=[\deEøØoO])", ".");

        // 1. Vorab: Spezifische 'IO' / 'lO' / '|O' / '1O' / 'I0' Kombinationen vor Trennzeichen/Ziffern zu '10' konvertieren
        result = TenPrefixRegex().Replace(result, "10");
        result = TenSuffixRegex().Replace(result, "10");

        // Z/z vor Ziffern als 2 interpretieren (z. B. "Z1.350" -> "21.350")
        result = ZPrefixRegex().Replace(result, "2");

        // Spezifische SO/So am Ende nach Ziffern (z. B. "21.3SO" -> "21.350")
        result = Regex.Replace(result, @"(?i)(?<=\d[\.,])(\d)[Ss][OoQq]", "${1}50");

        // 2. Iteratives Ersetzen: Bis zu 5 Durchläufe für mehrstellige Ketten wie "IO.2OO" -> "10.200", "7.2ee" -> "7.200", "2.eee" -> "2.000"
        char[] chars = result.ToCharArray();
        bool changed = true;
        int iterations = 0;
        while (changed && iterations++ < 5)
        {
            changed = false;
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                // Star Citizen slashed zero Ø wird von Windows OCR häufig als 'e' / 'E' oder 'ø' / 'Ø' gelesen
                if (c is 'O' or 'o' or 'Q' or 'D' or 'Ø' or 'ø' or 'e' or 'E')
                {
                    bool nearDigit = (i > 0 && (char.IsDigit(chars[i - 1]) || chars[i - 1] is '.' or ',' or '\'' or '*')) ||
                                     (i < chars.Length - 1 && (char.IsDigit(chars[i + 1]) || chars[i + 1] is '.' or ',' or '\'' or '*'));
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
