using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SCLogMate.Core.Ocr;

/// <summary>
/// Erkennt mobiGlas-Triggerzeilen im Log und extrahiert aUEC-Guthaben aus OCR-Text.
/// </summary>
public static partial class WalletOcrTrigger
{
    public const long MaxPlausibleBalance = 99_999_999_999;
    public const long MinPlausibleBalance = 0;

    // Atomare Gruppe: bricht komplett ab, wenn Ziffern von Buchstaben oder Quotes berührt werden (PL4Y3R, 5,101.94B, 2.349.æ).
    // Kein Backtracking in Teilzahlen!
    [GeneratedRegex(@"(?<![\p{L}0-9""'])(?>[0-9][0-9.,]*)(?![\p{L}0-9""'])")]
    private static partial Regex CandidateNumberRegex();

    [GeneratedRegex(@"\b\d{1,2}:\d{2}(?::\d{2})?\b")]
    private static partial Regex ClockRegex();

    [GeneratedRegex(@"(?i)aUEC|(?i)UEC|[\u00A4\$€£¥]")]
    private static partial Regex CurrencyLabelRegex();

    /// <summary>Prüft, ob die Logzeile das Öffnen des mobiGlas oder Inventorys signalisiert.</summary>
    public static bool IsMobiGlasOpenSignal(string raw)
    {
        if (raw.Contains("<VehicleListQuery>", StringComparison.Ordinal) &&
            raw.Contains("Fetching vehicle list", StringComparison.Ordinal))
            return true;

        if (raw.Contains("RequestLocationInventory", StringComparison.Ordinal))
            return true;

        if (raw.Contains("mobiGlas", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    /// <summary>Wählt den zuverlässigeren Wert aus zwei OCR-Durchläufen.
    /// Wenn beide Passes divergierende Werte lesen → Misread-Signal, Grab verwerfen.</summary>
    public static string? BestRead(string? a, string? b)
    {
        var va = a is null ? null : ExtractBalance(a);
        var vb = b is null ? null : ExtractBalance(b);

        if (va is not null && vb is not null) return va == vb ? a : null;
        if (va is not null) return a;
        if (vb is not null) return b;
        return null;
    }

    /// <summary>Extrahiert den Kontostand aus dem OCR-Text mit strengen Plausibilitätsprüfungen.</summary>
    public static long? ExtractBalance(string? ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText)) return null;

        // 1. Uhrzeiten entfernen (z.B. "14:02 1,067,200 aUEC" -> "  1,067,200 aUEC")
        var normalized = ClockRegex().Replace(ocrText, " ");

        // 2. Explizite Währungskennungen sauber entfernen (auch wenn direkt an Zahl geklebt: "aUEC2.349.289")
        normalized = CurrencyLabelRegex().Replace(normalized, " ");

        // 3. Führende Vorzeichen / OCR-Störzeichen entfernen
        normalized = Regex.Replace(normalized, @"[+*~|/\\()\[\]{}]", " ");

        // 4. Leerzeichen als Tausendertrennzeichen zwischen Zifferngruppen normalisieren ("2 463 039" -> "2.463.039")
        normalized = Regex.Replace(normalized, @"(?<=\b\d{1,3})\s+(?=\d{3}(?:\s+\d{3})*\b)", ".");

        long? bestValue = null;
        var bestDigits = 0;

        foreach (Match m in CandidateNumberRegex().Matches(normalized))
        {
            var raw = m.Value.Trim();
            if (raw.Length == 0) continue;

            // Darf nicht mit Trennzeichen beginnen oder enden (z.B. "2.349." ist abgeschnitten)
            if (raw.StartsWith('.') || raw.StartsWith(',') || raw.EndsWith('.') || raw.EndsWith(','))
                continue;

            var parts = raw.Split(new[] { '.', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            if (parts.Length > 1)
            {
                // Tausender-Gruppierung: erste Gruppe 1-3 Ziffern, alle folgenden MÜSSEN genau 3 Ziffern haben
                if (parts[0].Length < 1 || parts[0].Length > 3) continue;

                bool validGrouping = true;
                for (int i = 1; i < parts.Length; i++)
                {
                    if (parts[i].Length != 3)
                    {
                        validGrouping = false;
                        break;
                    }
                }

                if (!validGrouping) continue;
            }
            else
            {
                // Unformatierte Ziffernfolge (z.B. "0", "846", "5105256")
                if (parts[0].Length < 1 || parts[0].Length > 11) continue;
            }

            var digits = 0;
            foreach (var c in raw)
            {
                if (c is >= '0' and <= '9') digits++;
            }

            var cleaned = raw.Replace(".", "").Replace(",", "").Replace(" ", "");
            if (!long.TryParse(cleaned, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
                continue;

            if (value < MinPlausibleBalance || value > MaxPlausibleBalance)
                continue;

            if (digits > bestDigits)
            {
                bestDigits = digits;
                bestValue = value;
            }
        }

        return bestValue;
    }
}
