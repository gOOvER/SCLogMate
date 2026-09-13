using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SCLogMate.Models;

namespace SCLogMate.Core.Ocr;

public class RefineryParseResult
{
    public string MaterialName { get; set; } = "Quantanium";
    public double ScuQuantity { get; set; } = 32.0;
    public string RefineryLocation { get; set; } = "CRU-L1 Ambitious Dream";
    public string Method { get; set; } = "Dinyx Dodecathetic";
    public double YieldPercent { get; set; } = 93.0;
    public int CostAuec { get; set; } = 2500;
    public int RemainingSeconds { get; set; } = 7200;
    public string Status { get; set; } = "Refining";
    public string RawMatchedText { get; set; } = "";
}

public static partial class RefineryParser
{
    // Bekannte Rohstoffe und Minerale in Star Citizen (DE & EN)
    private static readonly string[] KnownMaterials = new[]
    {
        "Quantanium", "Quantainium", "Bexalite", "Gold", "Taranite", "Larinite", "Laranite",
        "Agricium", "Hephaestanite", "Beryl", "Beryll", "Diamond", "Diamant", "Titanium",
        "Titan", "Tungsten", "Wolfram", "Corundum", "Korund", "Quartz", "Quarz", "Copper",
        "Kupfer", "Iron", "Eisen", "Lindinium", "RMC", "Aluminium", "Aluminum", "Borase",
        "Inert Materials", "Inert Material", "Hadranite", "Aphorite", "Dolivine"
    };

    // Bekannte Veredelungsmethoden
    private static readonly (string Canonical, string[] Keywords)[] KnownMethods = new[]
    {
        ("Dinyx Dodecathetic", new[] { "Dinyx", "Dodecathetic" }),
        ("Ferron Exchange", new[] { "Ferron", "Exchange" }),
        ("Cormack Method", new[] { "Cormack" }),
        ("Electrostatic Purification", new[] { "Electrostatic", "Purification", "Elektrostat" }),
        ("Pyroxeres", new[] { "Pyroxeres", "Pyro" }),
        ("Gaskin-Kandah", new[] { "Gaskin", "Kandah" }),
        ("Thermite Processing", new[] { "Thermite", "Thermit" })
    };

    [GeneratedRegex(@"\b(\d+(?:[.,]\d+)?)\s*(?:cSCU|SCU|Units|Einheiten)\b", RegexOptions.IgnoreCase)]
    private static partial Regex QuantityRegex();

    [GeneratedRegex(@"\b(?:(?:(?<d>\d{1,2})\s*d(?:ays?)?\s*)?(?<h>\d{1,2})\s*(?:h|hrs?|std?)\b\s*(?:(?<m>\d{1,2})\s*(?:m|min)\b)?|(?<m>\d{1,2})\s*(?:m|min)\b\s*(?:(?<s>\d{1,2})\s*(?:s|sec)\b)?|(?<h>\d{1,2})[:.;](?<m>\d{2})(?:[:.;](?<s>\d{2}))?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex DurationRegex();

    [GeneratedRegex(@"\b(\d{1,3}(?:[.,]\d{3})*|\d+)\s*(?:aUEC|UEC)\b", RegexOptions.IgnoreCase)]
    private static partial Regex CostRegex();

    [GeneratedRegex(@"\b(\d{1,2}(?:[.,]\d)?)\s*%\b")]
    private static partial Regex YieldPercentRegex();

    public static List<RefineryParseResult> ParseKioskText(string rawText, string? defaultLocation = null)
    {
        var results = new List<RefineryParseResult>();
        if (string.IsNullOrWhiteSpace(rawText)) return results;

        var lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(l => l.Trim())
                           .Where(l => l.Length > 2)
                           .ToList();

        // 1. Suche nach bekannten Mineralien zeilenweise oder blockweise
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            string? foundMaterial = null;

            foreach (var mat in KnownMaterials)
            {
                if (Regex.IsMatch(line, $@"\b{Regex.Escape(mat)}\b", RegexOptions.IgnoreCase))
                {
                    foundMaterial = mat;
                    break;
                }
            }

            if (foundMaterial != null)
            {
                // Kontext-Fenster bilden (aktuelle Zeile + 2 Zeilen davor und danach)
                int start = Math.Max(0, i - 1);
                int count = Math.Min(lines.Count - start, 4);
                string block = string.Join(" ", lines.GetRange(start, count));

                var res = ParseBlock(block, foundMaterial, defaultLocation);
                if (res != null)
                {
                    // Duplikate vermeiden
                    if (!results.Any(r => r.MaterialName.Equals(res.MaterialName, StringComparison.OrdinalIgnoreCase) &&
                                          Math.Abs(r.ScuQuantity - res.ScuQuantity) < 0.1))
                    {
                        results.Add(res);
                    }
                }
            }
        }

        // Falls zeilenweise kein Treffer, Gesamten Text als einen Block prüfen
        if (results.Count == 0)
        {
            foreach (var mat in KnownMaterials)
            {
                if (Regex.IsMatch(rawText, $@"\b{Regex.Escape(mat)}\b", RegexOptions.IgnoreCase))
                {
                    var singleRes = ParseBlock(rawText, mat, defaultLocation);
                    if (singleRes != null)
                    {
                        results.Add(singleRes);
                        break;
                    }
                }
            }
        }

        // 3. Fallback: Falls keine bekannten Erze explizit namentlich erkannt wurden, aber Methoden oder Zeiten vorhanden sind
        if (results.Count == 0)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                bool hasMethod = KnownMethods.Any(m => m.Keywords.Any(k => line.Contains(k, StringComparison.OrdinalIgnoreCase)));
                bool hasDuration = DurationRegex().IsMatch(line);
                bool hasStatus = line.Contains("Ready", StringComparison.OrdinalIgnoreCase) ||
                                 line.Contains("Bereit", StringComparison.OrdinalIgnoreCase) ||
                                 line.Contains("Refining", StringComparison.OrdinalIgnoreCase) ||
                                 line.Contains("Processing", StringComparison.OrdinalIgnoreCase);

                if (hasMethod || hasDuration || hasStatus)
                {
                    int start = Math.Max(0, i - 1);
                    int count = Math.Min(lines.Count - start, 4);
                    string block = string.Join(" ", lines.GetRange(start, count));
                    var res = ParseBlock(block, "Gemischtes Erz", defaultLocation);
                    if (res != null)
                    {
                        results.Add(res);
                        break;
                    }
                }
            }
        }

        return results;
    }

    private static RefineryParseResult? ParseBlock(string block, string material, string? defaultLocation)
    {
        var res = new RefineryParseResult
        {
            MaterialName = NormalizeMaterial(material),
            RawMatchedText = block
        };

        // Menge extrahieren
        var qMatch = QuantityRegex().Match(block);
        if (qMatch.Success)
        {
            string numStr = qMatch.Groups[1].Value.Replace(',', '.');
            if (double.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedQty))
            {
                if (qMatch.Value.Contains("cSCU", StringComparison.OrdinalIgnoreCase))
                {
                    res.ScuQuantity = Math.Round(parsedQty / 100.0, 2);
                }
                else
                {
                    res.ScuQuantity = Math.Round(parsedQty, 2);
                }
            }
        }

        // Methode extrahieren
        foreach (var (canonical, keywords) in KnownMethods)
        {
            if (keywords.Any(k => block.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                res.Method = canonical;
                break;
            }
        }

        // Restzeit extrahieren (z. B. "1d 02h 45m", "02h 45m", "45m" oder "01:30:15")
        int parsedSeconds = 0;
        var durMatch = DurationRegex().Match(block);
        if (durMatch.Success)
        {
            int d = durMatch.Groups["d"].Success && int.TryParse(durMatch.Groups["d"].Value, out int dv) ? dv : 0;
            int h = durMatch.Groups["h"].Success && int.TryParse(durMatch.Groups["h"].Value, out int hv) ? hv : 0;
            int m = durMatch.Groups["m"].Success && int.TryParse(durMatch.Groups["m"].Value, out int mv) ? mv : 0;
            int s = durMatch.Groups["s"].Success && int.TryParse(durMatch.Groups["s"].Value, out int sv) ? sv : 0;
            parsedSeconds = (d * 86400) + (h * 3600) + (m * 60) + s;
        }

        if (parsedSeconds > 0)
        {
            res.RemainingSeconds = parsedSeconds;
            res.Status = "Refining";
        }
        else if (block.Contains("Ready", StringComparison.OrdinalIgnoreCase) ||
                 block.Contains("Bereit", StringComparison.OrdinalIgnoreCase) ||
                 block.Contains("Completed", StringComparison.OrdinalIgnoreCase) ||
                 block.Contains("Abgeschlossen", StringComparison.OrdinalIgnoreCase))
        {
            res.RemainingSeconds = 0;
            res.Status = "Ready";
        }

        // Kosten extrahieren
        var costMatch = CostRegex().Match(block);
        if (costMatch.Success)
        {
            string costStr = costMatch.Groups[1].Value.Replace(".", "").Replace(",", "");
            if (int.TryParse(costStr, out int parsedCost))
            {
                res.CostAuec = parsedCost;
            }
        }
        else
        {
            res.CostAuec = (int)Math.Round(res.ScuQuantity * 85);
        }

        // Standort bestimmen
        string loc = defaultLocation ?? "";
        foreach (var station in RefineryCatalog.AllStations)
        {
            if (block.Contains(station.Id, StringComparison.OrdinalIgnoreCase) ||
                block.Contains(station.Name, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(loc) && loc.Contains(station.Name, StringComparison.OrdinalIgnoreCase)))
            {
                loc = station.Name;
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(loc))
        {
            loc = "CRU-L1 Ambitious Dream Station";
        }
        res.RefineryLocation = loc;

        // Ausbeute berechnen
        var yieldMatch = YieldPercentRegex().Match(block);
        if (yieldMatch.Success)
        {
            string yStr = yieldMatch.Groups[1].Value.Replace(',', '.');
            if (double.TryParse(yStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double yVal))
            {
                res.YieldPercent = yVal;
            }
        }
        else
        {
            res.YieldPercent = RefineryCatalog.CalculateYield(res.MaterialName, res.Method, res.RefineryLocation);
        }

        return res;
    }

    private static string NormalizeMaterial(string raw)
    {
        if (raw.Equals("Quantainium", StringComparison.OrdinalIgnoreCase)) return "Quantanium";
        if (raw.Equals("Beryll", StringComparison.OrdinalIgnoreCase)) return "Beryl";
        if (raw.Equals("Korund", StringComparison.OrdinalIgnoreCase)) return "Corundum";
        if (raw.Equals("Quarz", StringComparison.OrdinalIgnoreCase)) return "Quartz";
        if (raw.Equals("Diamant", StringComparison.OrdinalIgnoreCase)) return "Diamond";
        if (raw.Equals("Kupfer", StringComparison.OrdinalIgnoreCase)) return "Copper";
        if (raw.Equals("Eisen", StringComparison.OrdinalIgnoreCase)) return "Iron";
        if (raw.Equals("Wolfram", StringComparison.OrdinalIgnoreCase)) return "Tungsten";
        if (raw.Equals("Titan", StringComparison.OrdinalIgnoreCase)) return "Titanium";
        if (raw.StartsWith("Inert", StringComparison.OrdinalIgnoreCase)) return "Inert Materials";
        return raw;
    }
}
