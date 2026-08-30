using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SCLogReader.Models;

namespace SCLogReader.Core.Ocr;

/// <summary>
/// Extrahiert Missions-Details (Belohnung, sauberer Titel, Auftraggeber) aus dem OCR-Text
/// des Star Citizen mobiGlas Auftragsmanagers (Contracts-Panel).
/// Unterstützt sowohl englische als auch deutsche Sprachversionen von Star Citizen.
/// </summary>
public static partial class ContractParser
{
    // Reward = Betrag direkt vor dem "N/A" Deadline-Feld (z. B. "139,250 N/A", "26 750 N/A", "45.000 N/A", "58k N/A", "15000 N/A")
    [GeneratedRegex(@"(?:¤|Reward|Belohnung|Payment|Auszahlung)?\s*(?<num>\d{1,3}(?:[\s.,]\d{3})+|\d{4,8})\s*(?:¤|aUEC)?\s+N\s*/\s*A", RegexOptions.IgnoreCase)]
    private static partial Regex RewardBeforeNaRegex();

    [GeneratedRegex(@"\b(\d{1,3})[kK]\s+N\s*/\s*A", RegexOptions.IgnoreCase)]
    private static partial Regex RewardKBeforeNaRegex();

    [GeneratedRegex(@"(?:¤|Reward|Belohnung|Payment|Auszahlung)\s*[:\s]*¤?\s*(?<num>\d{1,3}(?:[\s.,]\d{3})+|\d{4,8})", RegexOptions.IgnoreCase)]
    private static partial Regex RewardSymbolRegex();

    // Auftraggeber-Organisation (EN & DE) - max 35 Zeichen, um nicht den Kartentext einzuschlucken
    [GeneratedRegex(
        @"(?:Contracted\s+By|Auftraggeber\s*:?)\s+(?<org>[A-Za-zÄÖÜäöüß][A-Za-zÄÖÜäöüß .'&\-]{1,35}?)\s*" +
        @"(?=\[|\d|DETAILS|PRIMARY|HAUPTZIELE|OBJECTIVES|ZIELE|ACCEPT|ANNEHMEN|TRACK|VERFOLGEN|UNTRACK|NICHT|ABANDON|AUFGEBEN|ABBRECHEN|Deliver|Liefern|Collect|Sammeln|Abholen|Share|Teilen|Goto|Gehe|Availability|Verfügbarkeit|$)",
        RegexOptions.IgnoreCase)]
    private static partial Regex ExplicitContractorRegex();

    [GeneratedRegex(
        @"N\s*/\s*A\s+(?:Contracted\s+By|Auftraggeber\s*:?)?\s*(?<org>[A-Za-zÄÖÜäöüß][A-Za-zÄÖÜäöüß .'&\-]{1,35}?)\s*" +
        @"(?=\[|\d|DETAILS|PRIMARY|HAUPTZIELE|OBJECTIVES|ZIELE|ACCEPT|ANNEHMEN|TRACK|VERFOLGEN|UNTRACK|NICHT|ABANDON|AUFGEBEN|ABBRECHEN|Deliver|Liefern|Collect|Sammeln|Abholen|Share|Teilen|Goto|Gehe|Availability|Verfügbarkeit|$)",
        RegexOptions.IgnoreCase)]
    private static partial Regex ContractorAfterNaRegex();

    [GeneratedRegex(@"\[[^\]]*\]")]
    private static partial Regex RepTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    // Tab-Header, Kategorien und Metadaten-Schlagwörter (EN & DE)
    [GeneratedRegex(@"\b(?:OFFERS|ANGEBOTE|GENERAL|ALLGEMEIN|PERSONAL|PERSÖNLICH|HISTORY|HISTORIE|VERLAUF|BEACONS|NOTRUF-BAKEN|BAKEN|GADGETS|MARK\s+ALL\s+READ|ALLE\s+ALS\s+GELESEN\s+MARKIEREN|MARK\s+READ|MARK|GELESEN|VERIFIED|VERIFIZIERT|CONTRACTS?|AUFTRÄGE?|AUFTRAG|DEADLINE|FRIST|ZEITLIMIT|REWARD|BELOHNUNG|CONTRACTED\s+BY|AUFTRAGGEBER|PRIMARY\s+OBJECTIVES?|HAUPTZIELE?|OBJECTIVES?|ZIELE?|PRIMARY|DETAILS|FPS|GPU|CPU|LAT|BATTAGLIA|INVESTIGATION|BOUNTY\s+HUNTER|MERCENARY|SALVAGE|HAULING|COURIER|SHIP\s+MINING|MINING|DEFENSE|SECURITY|RECOVERY)\b", RegexOptions.IgnoreCase)]
    private static partial Regex TabBarAndCategoryNoiseRegex();

    // Tab-Zähler wie "ACCEPTED (1/10)" oder "ANGENOMMEN (1/10)"
    [GeneratedRegex(@"\b(?:ACCEPTE[DO0]|ANGENOMMEN)\s*\(\d+/\d+\)", RegexOptions.IgnoreCase)]
    private static partial Regex AcceptedHeaderRegex();

    [GeneratedRegex(@"^[\s\-–—\(\)\[\]/:,.*·•◇◆Ø§#\d\)]+")]
    private static partial Regex LeadingNoiseRegex();

    [GeneratedRegex(@"\b(?:FPS\s*\d+|GPU\s*\d+%?\s*\d*°?C?|CPU\s*\d+%?\s*\d*°?C?|LAT\s*[\d.]+\s*ms|\d+\s*ms|\d+°C|\d+%|AUF\s+MONITOR\s*\d*.*?\)|Monitor\s+wechseln|Aktuell\s*:\s*\d+.*?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex DiagOverlayRegex();

    /// <summary>
    /// Prüft, ob der OCR-Text einen ECHTEN angenommenen Auftrag im "ACCEPTED"/"ANGENOMMEN"-Tab darstellt.
    /// </summary>
    public static bool IsAcceptedContract(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        // 1. Kein Auftrag im Detailbereich geöffnet (EN & DE)
        if (text.Contains("Please select a contract", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Bitte wähle einen Auftrag", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Bitte wählen Sie einen Auftrag", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Kein Auftrag ausgewählt", StringComparison.OrdinalIgnoreCase))
            return false;

        // 2. Wenn der [ACCEPT] / [ANNEHMEN] Button vorhanden ist UND kein ABANDON/AUFGEBEN -> unangenommenes Angebot
        bool hasAccept = Regex.IsMatch(text, @"\b(?:ACCEPT|ANNEHMEN)\b", RegexOptions.IgnoreCase);
        bool hasAbandon = text.Contains("ABANDON", StringComparison.OrdinalIgnoreCase) ||
                          text.Contains("AUFGEBEN", StringComparison.OrdinalIgnoreCase) ||
                          text.Contains("ABBRECHEN", StringComparison.OrdinalIgnoreCase);

        if (hasAccept && !hasAbandon)
            return false;

        return true;
    }

    /// <summary>Parst OCR-Text aus dem mobiGlas Auftragsfenster in ein ContractDetails-Objekt.</summary>
    public static ContractDetails? Parse(string ocrText, bool requireAccepted = true)
    {
        if (string.IsNullOrWhiteSpace(ocrText)) return null;

        // Strikt: Nur Aufträge aus dem Accepted-Tab übernehmen
        bool isAccepted = IsAcceptedContract(ocrText);
        if (requireAccepted && !isAccepted)
            return null;

        var reward = ParseReward(ocrText);
        if (reward <= 0)
            return null;

        var contractedBy = "";
        if (ExplicitContractorRegex().Match(ocrText) is { Success: true } ecb)
        {
            contractedBy = CleanContractor(ecb.Groups["org"].Value);
        }
        else if (ContractorAfterNaRegex().Match(ocrText) is { Success: true } cb)
        {
            contractedBy = CleanContractor(cb.Groups["org"].Value);
        }

        var title = ExtractTitle(ocrText, contractedBy);
        if (title.Length < 3 && contractedBy.Length >= 3 && contractedBy.Length <= 35)
            title = contractedBy;

        // Abgleich mit MissionCatalog für automatische Rechtschreibkorrektur, Fraktion und Belohnung
        var cat = MissionCatalog.FuzzyLookup(title);
        if (cat != null)
        {
            title = cat.Title;
            if (string.IsNullOrWhiteSpace(contractedBy) && !string.IsNullOrWhiteSpace(cat.Contractor))
                contractedBy = cat.Contractor;
            if (reward <= 0 && cat.BaseReward > 0)
                reward = cat.BaseReward;
        }

        if (!IsValidMissionTitle(title))
            return null;

        return new ContractDetails
        {
            Title = title,
            Reward = reward,
            ContractedBy = contractedBy,
            ScannedAt = DateTime.UtcNow
        };
    }

    public static int ParseReward(string text)
    {
        // 1. Text bereinigen: Zeilen mit Kontostand/Wallet-Informationen (z. B. "2.463.039 aUEC", "KONTOSTAND") ausblenden
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var contractLines = new List<string>();
        foreach (var l in lines)
        {
            var trimmed = l.Trim();
            if (trimmed.StartsWith("KONTOSTAND", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("BALANCE", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("ACCOUNT", StringComparison.OrdinalIgnoreCase) ||
                trimmed.EndsWith("aUEC", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            contractLines.Add(trimmed);
        }
        var cleanText = string.Join(" ", contractLines);
        if (string.IsNullOrWhiteSpace(cleanText)) cleanText = text;

        // 2. Betrag direkt vor N/A: "139,250 N/A", "26 750 N/A", "45.000 N/A", "15000 N/A"
        if (RewardBeforeNaRegex().Match(cleanText) is { Success: true } r)
        {
            var digits = Regex.Replace(r.Groups["num"].Value, @"[\s.,]", "");
            if (int.TryParse(digits, out var v) && v > 0) return v;
        }

        // 3. Kurzform vor N/A: "58k N/A" oder "25k N/A"
        if (RewardKBeforeNaRegex().Match(cleanText) is { Success: true } k)
        {
            if (int.TryParse(k.Groups[1].Value, out var kv) && kv > 0)
                return kv * 1000;
        }

        // 4. Explizites Symbol: "¤ 58,000", "Reward: 58,000", "Belohnung: 45.000", "Payment 15,000"
        if (RewardSymbolRegex().Match(cleanText) is { Success: true } s)
        {
            var digits = Regex.Replace(s.Groups["num"].Value, @"[\s.,]", "");
            if (int.TryParse(digits, out var v) && v > 0) return v;
        }

        return 0;
    }

    private static string CleanContractor(string org)
    {
        var cleaned = DiagOverlayRegex().Replace(org, " ");
        cleaned = RepTagRegex().Replace(cleaned, " ");
        cleaned = TabBarAndCategoryNoiseRegex().Replace(cleaned, " ");
        cleaned = AcceptedHeaderRegex().Replace(cleaned, " ");
        cleaned = LeadingNoiseRegex().Replace(cleaned, "");
        var res = Collapse(cleaned);

        if (res.Equals("PRIMARY", StringComparison.OrdinalIgnoreCase) ||
            res.Equals("DETAILS", StringComparison.OrdinalIgnoreCase) ||
            res.Equals("OBJECTIVES", StringComparison.OrdinalIgnoreCase) ||
            res.Equals("HAUPTZIELE", StringComparison.OrdinalIgnoreCase) ||
            res.Equals("ZIELE", StringComparison.OrdinalIgnoreCase))
        {
            return "";
        }

        return res;
    }

    public static string ExtractTitle(string text, string knownOrg)
    {
        // 1. Direkt "RE: <Titel>" überall im Text finden (höchste Trefferquote in SC)
        var reMatch = Regex.Match(text, @"\bRE\s*:\s*(?<title>[A-Za-zÄÖÜäöüß0-9][A-Za-zÄÖÜäöüß0-9 '\-–,]+?)(?=\s*(?:[\r\n]|\bWORK\b|\bBRIEF\b|\bPAYMENT\b|\bDETAILS\b|\bPRIMARY\b|\bHAUPTZIELE\b|\bOBJECTIVES\b|\bAUTHORIZATION\b|\bABANDON\b|\bSHARE\b|\bTRACK\b|$))", RegexOptions.IgnoreCase);
        if (reMatch.Success)
        {
            var t = CleanTitleString(reMatch.Groups["title"].Value, knownOrg);
            if (t.Length >= 3 && IsValidMissionTitle(t)) return t;
        }

        // 2. Kartentitel vor DETAILS / HAUPTZIELE / [BP] (z. B. "Missing Mining Team DETAILS")
        var detMatch = Regex.Match(text, @"(?<title>[A-Za-zÄÖÜäöüß0-9][A-Za-zÄÖÜäöüß0-9 '\-–,]{2,40}?)\s*(?:\[BP\]|\[[^\]]+\]|\bDETAILS\b|\bHAUPTZIELE\b)", RegexOptions.IgnoreCase);
        if (detMatch.Success)
        {
            var t = CleanTitleString(detMatch.Groups["title"].Value, knownOrg);
            if (t.Length >= 3 && IsValidMissionTitle(t)) return t;
        }

        // 3. Titel nach ACCEPTED / ANGENOMMEN / BEACONS / HISTORY vor Reward / Deadline
        var headerMatch = Regex.Match(text, @"\b(?:BEACONS|OFFERS|ANGEBOTE|ACCEPTED\s*\(\d+/\d+\)|ANGENOMMEN\s*\(\d+/\d+\))\s+[\r\n\s]*(?<title>[A-Za-zÄÖÜäöüß0-9][A-Za-zÄÖÜäöüß0-9 '\-–,]{2,40}?)\s*(?:\[BP\]|\[[^\]]+\]|\bReward\b|\bBelohnung\b|\bContract Deadline\b|\bDETAILS\b|\bPRIMARY\b|\bHAUPTZIELE\b)", RegexOptions.IgnoreCase);
        if (headerMatch.Success)
        {
            var t = CleanTitleString(headerMatch.Groups["title"].Value, knownOrg);
            if (t.Length >= 3 && IsValidMissionTitle(t)) return t;
        }

        // 4. Titel mit Suffix [BP] oder [Rep]
        var bpMatches = Regex.Matches(text, @"\b(?<title>[A-Za-zÄÖÜäöüß0-9][A-Za-zÄÖÜäöüß0-9 '\-–,]{2,40}?)\s*\[(?:BP|Rep|[^\]]+)\]", RegexOptions.IgnoreCase);
        if (bpMatches.Count > 0)
        {
            var last = CleanTitleString(bpMatches[bpMatches.Count - 1].Groups["title"].Value, knownOrg);
            if (last.Length >= 3 && IsValidMissionTitle(last)) return last;
        }

        return "";
    }

    private static string CleanTitleString(string s, string knownOrg)
    {
        // 1. Spalten-Müll, Objectives und Blueprint-Tags entfernen
        s = Regex.Replace(s, @"\b(?:Goto|Gehe\s+zu|Availability|Verf[üu]gbarkeit|Collect|Sammeln|Abholen|Deliver|Liefern|Destroy|Zerst[öo]ren|Eliminate|Eliminieren|Ausschalten|Investigate|Untersuchen|Repair|Reparieren)\b[^xÄ\d]*", " ", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\[[^\]]*\]|tBP\d*|\bBP\b|\bBPJ\b", " ");

        // 2. Sidebar-Timer & Tab-Leisten Fragmente entfernen ("Brn", "31s", "6m 5s", "BEACONS", "BATTAGLIA", "GADGETS", "SIS", "S8s", "S3s")
        s = Regex.Replace(s, @"\b(?:BEACONS|HISTORY|HISTORIE|OFFERS|ANGEBOTE|ACCEPTED|ANGENOMMEN|BATTAGLIA|GADGETS|Brn|SIS|[0-9SsgOBblI]+[smh]|\d+m\s*\d+s)\b", " ", RegexOptions.IgnoreCase);

        // 3. Organisation und bekannte Auftraggeber-Namen aus dem Titel entfernen
        if (!string.IsNullOrEmpty(knownOrg))
        {
            s = Regex.Replace(s, Regex.Escape(knownOrg), " ", RegexOptions.IgnoreCase);
        }
        s = Regex.Replace(s, @"\b(?:Recco|Battaglia|Adagio|Holdings|RedWind|Red\s+Wind|Linehaul|Covalex|Citizens|Prosperity|Ling\s+Family|Northrock|Vaughn|Hockrow|InterSec|HeadHunters)\b", " ", RegexOptions.IgnoreCase);

        s = Regex.Replace(s, @"\b\d{1,3}(?:[.,]\d{3})+\b", " ");
        s = Regex.Replace(s, @"\b[0-9SsgOBb]+[kK]\b", " ");
        s = Regex.Replace(s, @"\b(?:SBk|S8k|S6k|S6OW|96om)\b", " ", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\bN\s*/\s*A\b", " ", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\baUEC\b", " ", RegexOptions.IgnoreCase);

        // 4. Stray Bullets & Checkboxen entfernen
        s = Regex.Replace(s, @"\s+[\)\(\]\[xXoO◇◆•·]\s+", " ");
        s = RepTagRegex().Replace(s, " ");
        s = LeadingNoiseRegex().Replace(s, "");

        // 5. Einzelne herrenlose führende Buchstaben / Zahlen wie "A " oder "1 " am Anfang entfernen
        s = Regex.Replace(s, @"^[A-Za-z0-9]\s+(?=[A-Z])", "");

        s = Regex.Replace(s, @"\b(?:gOOvER|MiwiDot|miwi|SOOVER|useg|usu|useeu)\b", "", RegexOptions.IgnoreCase);

        var collapsed = Collapse(s);
        collapsed = LeadingNoiseRegex().Replace(collapsed, "").Trim();
        return collapsed.Length >= 3 ? collapsed : (knownOrg.Length >= 3 ? knownOrg : "");
    }

    public static bool IsValidMissionTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length < 4) return false;
        // Reiner Timer-/Fragmentmüll wie "S3s", "SIS SIS", "Brn 31s", "6m 5s", "S8s"
        if (Regex.IsMatch(title, @"^(?:\b[0-9SsgOBblI]+[smh]\b|\bSIS\b|\bBrn\b|\s)+$", RegexOptions.IgnoreCase))
            return false;
        // Muss mindestens ein echtes Wort mit >= 3 Buchstaben enthalten, das kein Timer-Kürzel ist
        var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int validWordCount = 0;
        foreach (var w in words)
        {
            if (w.Length >= 3 && !Regex.IsMatch(w, @"^(?:[0-9SsgOBblI]+[smh]|Brn|SIS)$", RegexOptions.IgnoreCase))
                validWordCount++;
        }
        return validWordCount >= 1;
    }

    public static string NormalizeTitle(string title)
    {
        var noDiag = DiagOverlayRegex().Replace(title, " ");
        var noTab = AcceptedHeaderRegex().Replace(noDiag, " ");
        var noTag = RepTagRegex().Replace(noTab, " ");
        var noLead = LeadingNoiseRegex().Replace(noTag, " ");
        var lower = noLead.ToLowerInvariant();
        var sb = new System.Text.StringBuilder(lower.Length);
        foreach (var ch in lower)
            sb.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
        return WhitespaceRegex().Replace(sb.ToString(), " ").Trim();
    }

    /// <summary>
    /// Prüft, ob zwei Aufträge denselben realen Auftrag darstellen.
    /// Unterstützt auch Vergleiche zwischen Log-Events (noch ohne Belohnung) und mobiGlas OCR (mit Belohnung).
    /// </summary>
    public static bool AreSameContract(ContractDetails a, ContractDetails b)
    {
        if (a == null || b == null) return false;

        var aNorm = NormalizeTitle(a.Title);
        var bNorm = NormalizeTitle(b.Title);

        // Exakte oder weitgehende Titelübereinstimmung
        bool titlesMatch = (aNorm == bNorm && aNorm.Length >= 3) ||
                           (aNorm.Length >= 5 && bNorm.Length >= 5 && (aNorm.Contains(bNorm) || bNorm.Contains(aNorm)));

        if (!titlesMatch)
            return false;

        // Wenn beide Aufträge unterschiedliche, explizite Beträge (> 0) haben, prüfen wir, ob die Titel exakt identisch sind
        if (a.Reward > 0 && b.Reward > 0 && Math.Abs(a.Reward - b.Reward) > 0)
        {
            // Wenn die Titel identisch sind (z.B. "Missing Persons"), ist es derselbe Auftrag (evtl. korrigierte Belohnung)
            return aNorm == bNorm;
        }

        // Wenn einer von beiden Reward == 0 hat (z.B. aus "Contract Accepted"-Logzeile) und Titel passt -> Derselbe Auftrag!
        return true;
    }

    private static string Collapse(string s) => WhitespaceRegex().Replace(s.Replace('\r', ' ').Replace('\n', ' '), " ").Trim();
}
