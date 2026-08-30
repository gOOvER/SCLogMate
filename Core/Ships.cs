using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SCLogReader.Core;

/// <summary>
/// Macht aus internen Schiffs-Codes (DRAK_Clipper_795148066379) und Kanal-Namen (Drake Clipper)
/// einheitliche lesbare Namen ("Clipper · Drake").
/// Normalisiert Versions- und Modellbezeichnungen (z. B. Mk2 -> Mk II).
/// </summary>
public static partial class Ships
{
    static readonly Dictionary<string, string> Brands = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RSI"] = "RSI",
        ["AEGS"] = "Aegis",
        ["ANVL"] = "Anvil",
        ["DRAK"] = "Drake",
        ["CRUS"] = "Crusader",
        ["MISC"] = "MISC",
        ["ORIG"] = "Origin",
        ["CNOU"] = "Consolidated Outland",
        ["BANU"] = "Banu",
        ["ARGO"] = "Argo",
        ["MRAI"] = "Mirai",
        ["GAMA"] = "Gatac",
        ["GATS"] = "Gatac",
        ["XIAN"] = "Xi'an",
        ["XNAA"] = "Xi'an",
        ["KRIG"] = "Kruger",
        ["GRIN"] = "Greycat",
        ["ESPR"] = "Esperia",
        ["TMBL"] = "Tumbril",
        ["VNCL"] = "Vanduul",
        ["RSIB"] = "RSI",
    };

    static readonly string[] BrandPrefixes =
    {
        "Drake", "Aegis", "Crusader", "Anvil", "RSI", "MISC", "Origin", "Argo", "Mirai",
        "Esperia", "Gatac", "Consolidated Outland", "Banu", "Tumbril", "Greycat", "Kruger"
    };

    // interne Variant-Tags, die niemanden interessieren
    static readonly HashSet<string> Noise = new(StringComparer.OrdinalIgnoreCase)
    {
        "Unmanned", "PU", "AI", "S42", "Template", "Modified"
    };

    [GeneratedRegex(@"_\d{4,}$")]
    private static partial Regex TrailingIdRegex();

    [GeneratedRegex(@"\b(?:Mk|MK|mk)[\s_-]*2\b")]
    private static partial Regex Mk2Regex();

    [GeneratedRegex(@"\b(?:Mk|MK|mk)[\s_-]*1\b")]
    private static partial Regex Mk1Regex();

    [GeneratedRegex(@"\b(?:Mk|MK|mk)[\s_-]*3\b")]
    private static partial Regex Mk3Regex();

    [GeneratedRegex(@"\b(?:Mk|MK|mk)[\s_-]*4\b")]
    private static partial Regex Mk4Regex();

    [GeneratedRegex(@"\b(?:Mk|MK|mk)[\s_-]*II\b", RegexOptions.IgnoreCase)]
    private static partial Regex MkIIRegex();

    [GeneratedRegex(@"\b(?:Mk|MK|mk)[\s_-]*III\b", RegexOptions.IgnoreCase)]
    private static partial Regex MkIIIRegex();

    [GeneratedRegex(@"\b(?:Mk|MK|mk)[\s_-]*I\b", RegexOptions.IgnoreCase)]
    private static partial Regex MkIRegex();

    public static string NormalizeModelName(string model)
    {
        if (string.IsNullOrWhiteSpace(model)) return model;

        var m = model.Trim().Replace('_', ' ');

        // Römische Zahlen & Mk Normalisierung (Mk2 / Mk_2 -> Mk II)
        m = MkIIIRegex().Replace(m, "Mk III");
        m = MkIIRegex().Replace(m, "Mk II");
        m = Mk3Regex().Replace(m, "Mk III");
        m = Mk2Regex().Replace(m, "Mk II");
        m = Mk1Regex().Replace(m, "Mk I");
        m = MkIRegex().Replace(m, "Mk I");

        // Spezifische Namensharmonisierungen
        if (m.Equals("Aurora", StringComparison.OrdinalIgnoreCase)) m = "Aurora Mk II";

        return Regex.Replace(m, @"\s+", " ").Trim();
    }

    public static string Prettify(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;

        var name = TrailingIdRegex().Replace(raw, "").Trim();

        // 1. Bereits im Format "Model · Brand"
        if (name.Contains('·'))
        {
            var parts1 = name.Split('·', StringSplitOptions.TrimEntries);
            if (parts1.Length == 2)
            {
                return $"{NormalizeModelName(parts1[0])} · {parts1[1]}";
            }
            return name;
        }

        // 2. Format: "Drake Clipper", "Aegis Gladius", "Crusader C2 Hercules", "RSI Aurora Mk2"
        foreach (var prefix in BrandPrefixes)
        {
            if (name.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase))
            {
                var model = name[(prefix.Length + 1)..].Trim();
                return $"{NormalizeModelName(model)} · {prefix}";
            }
        }

        // 3. Format: "DRAK_Clipper_795148066379", "RSI_Aurora_Mk2_..."
        var parts = name.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return raw;

        string brand = Brands.TryGetValue(parts[0], out var b) ? b : parts[0];
        var rest = parts.Skip(1).Where(p => !Noise.Contains(p)).ToArray();

        var rawModel = string.Join(' ', rest);
        if (rawModel.Length == 0) return brand;

        return $"{NormalizeModelName(rawModel)} · {brand}";
    }
}
