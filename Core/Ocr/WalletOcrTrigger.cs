using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SCLogReader.Core.Ocr;

/// <summary>
/// Erkennt mobiGlas-Triggerzeilen im Log und extrahiert aUEC-Guthaben aus OCR-Text.
/// </summary>
public static partial class WalletOcrTrigger
{
    public const long MaxPlausibleBalance = 99_999_999_999;
    public const long MinPlausibleBalance = 0;

    // Unterstützt alle Tausendertrennzeichen (Komma, Punkt, Leerzeichen wie "2 463 039", "12.714.118", "2,463,039" oder "2463039")
    [GeneratedRegex(@"(?:\b|(?<=\s|[^\w.]))(?>[0-9]{1,3}(?:[.,\s][0-9]{3})+|[0-9]{3,11})(?:\b|(?=\s|[^\w.]))")]
    private static partial Regex StrictBalanceRegex();

    [GeneratedRegex(@"[0-9]{3,12}")]
    private static partial Regex FallbackDigitsRegex();

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

    /// <summary>Wählt den zuverlässigeren Wert aus zwei OCR-Durchläufen (bevorzugt Invertiert für mobiGlas-Cyan).</summary>
    public static string? BestRead(string? a, string? b)
    {
        var va = a is null ? null : ExtractBalance(a);
        if (va is not null) return a;

        var vb = b is null ? null : ExtractBalance(b);
        if (vb is not null) return b;

        return null;
    }

    /// <summary>Extrahiert den Kontostand aus dem OCR-Text mit strengen Plausibilitätsprüfungen.</summary>
    public static long? ExtractBalance(string? ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText)) return null;

        // Vorverarbeitung: Trennt Buchstaben/Präfixe sauber von Ziffern (z.B. "aUEC12.714.118" -> "aUEC 12.714.118")
        var normalized = ocrText;
        normalized = Regex.Replace(normalized, @"([a-zA-Z\u00A4\u00A7\u00A9\u00AE\$€£¥])([0-9])", "$1 $2");
        normalized = Regex.Replace(normalized, @"([0-9])([a-zA-Z\u00A4\u00A7\u00A9\u00AE\$€£¥])", "$1 $2");
        // Ersetzt führende OCR-Artefakte wie '|' vor Zahlen
        normalized = Regex.Replace(normalized, @"[\|\/\(\)\[\]\{\}]", " ");

        // 1. Strikte Suche nach Tausender-Gruppierungen oder >= 3-stelligen Zahlen
        string? best = null;
        var bestDigits = 0;

        foreach (Match m in StrictBalanceRegex().Matches(normalized))
        {
            var digits = 0;
            foreach (var c in m.Value)
            {
                if (c is >= '0' and <= '9') digits++;
            }

            if (digits >= 3 && digits > bestDigits)
            {
                bestDigits = digits;
                best = m.Value;
            }
        }

        // 2. Fallback nur wenn aUEC im Text steht
        if (best is null && (normalized.Contains("aUEC", StringComparison.OrdinalIgnoreCase) || normalized.Contains("UEC", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (Match m in FallbackDigitsRegex().Matches(normalized))
            {
                var digits = m.Value.Length;
                if (digits > bestDigits)
                {
                    bestDigits = digits;
                    best = m.Value;
                }
            }
        }

        if (best is null || bestDigits == 0) return null;

        var cleaned = best.Replace(",", "").Replace(".", "").Replace(" ", "").Trim();
        if (!long.TryParse(cleaned, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            return null;

        if (value < MinPlausibleBalance || value > MaxPlausibleBalance)
            return null;

        return value;
    }
}
