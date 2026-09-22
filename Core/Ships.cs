using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SCLogMate.Core;

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
        ["GLSN"] = "GLSN",
    };

    static readonly string[] BrandPrefixes =
    {
        "Drake", "Aegis", "Crusader", "Anvil", "RSI", "MISC", "Origin", "Argo", "Mirai",
        "Esperia", "Gatac", "Consolidated Outland", "Banu", "Tumbril", "Greycat", "Kruger", "GLSN"
    };

    // interne Variant-Tags, die niemanden interessieren (z. B. Missions-Archetypen, Wreck-Tags, Spawntemplates)
    static readonly HashSet<string> Noise = new(StringComparer.OrdinalIgnoreCase)
    {
        "Unmanned", "PU", "AI", "S42", "Template", "Modified",
        "Salvage", "Derelict", "Wreck", "Pirate", "Security", "Police",
        "Civilian", "Criminal", "Outlaw", "Bounty", "Escort", "Patrol",
        "Rental", "Loaner", "Test", "Preview", "Show", "FreeFly", "Mission", "Teach", "GS"
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

    [GeneratedRegex(@"\b(?:Salvage|Derelict|Wreck|Pirate|Civilian|Criminal|Outlaw|Bounty|Escort|Patrol|Rental|Loaner|Teach|GS)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ArchetypeNoiseRegex();

    public static string NormalizeModelName(string model)
    {
        if (string.IsNullOrWhiteSpace(model)) return model;

        var m = model.Trim().Replace('_', ' ');

        // Falsche Missions-/Spawn-Archetypen entfernen (z. B. "MOLE Salvage" -> "MOLE")
        m = ArchetypeNoiseRegex().Replace(m, "");

        // Römische Zahlen & Mk Normalisierung (Mk2 / Mk_2 -> Mk II)
        m = MkIIIRegex().Replace(m, "Mk III");
        m = MkIIRegex().Replace(m, "Mk II");
        m = Mk3Regex().Replace(m, "Mk III");
        m = Mk2Regex().Replace(m, "Mk II");
        m = Mk1Regex().Replace(m, "Mk I");
        m = MkIRegex().Replace(m, "Mk I");

        // CIG Localization artifacts (e.g. "NameDRAK Golem", "nameMISC Starlancer MAX")
        if (m.StartsWith("Name", StringComparison.OrdinalIgnoreCase) && m.Length > 4 && (char.IsUpper(m[4]) || char.IsDigit(m[4]))) m = m[4..].Trim();
        if (m.StartsWith("name", StringComparison.OrdinalIgnoreCase) && m.Length > 4 && (char.IsUpper(m[4]) || char.IsDigit(m[4]))) m = m[4..].Trim();

        // Spezifische Namensharmonisierungen
        if (m.Equals("Aurora", StringComparison.OrdinalIgnoreCase)) m = "Aurora Mk II";
        if (m.Equals("m80", StringComparison.OrdinalIgnoreCase)) m = "M80";
        if (m.Equals("m50", StringComparison.OrdinalIgnoreCase)) m = "M50";
        if (m.Equals("85x", StringComparison.OrdinalIgnoreCase)) m = "85X";
        if (m.Equals("Zeus CL", StringComparison.OrdinalIgnoreCase)) m = "Zeus Mk II CL";
        if (m.Equals("Zeus ES", StringComparison.OrdinalIgnoreCase)) m = "Zeus Mk II ES";
        if (m.Equals("Zeus MR", StringComparison.OrdinalIgnoreCase)) m = "Zeus Mk II MR";
        if (m.Equals("Starlifter C2", StringComparison.OrdinalIgnoreCase)) m = "C2 Hercules";
        if (m.Equals("Starlifter M2", StringComparison.OrdinalIgnoreCase)) m = "M2 Hercules";
        if (m.Equals("Starlifter A2", StringComparison.OrdinalIgnoreCase)) m = "A2 Hercules";
        if (m.Equals("Spirit A1", StringComparison.OrdinalIgnoreCase)) m = "A1 Spirit";
        if (m.Equals("Star Runner", StringComparison.OrdinalIgnoreCase)) m = "Mercury Star Runner";
        if (m.Equals("Starfighter Inferno", StringComparison.OrdinalIgnoreCase)) m = "Ares Inferno";
        if (m.Equals("Starfighter Ion", StringComparison.OrdinalIgnoreCase)) m = "Ares Ion";
        if (m.Equals("SanTokYai", StringComparison.OrdinalIgnoreCase) || m.Equals("San'tok.yāi", StringComparison.OrdinalIgnoreCase)) m = "San'tok.yāi";
        if (m.Equals("C8R Pisces Rescue", StringComparison.OrdinalIgnoreCase)) m = "C8R Pisces";
        if (m.Equals("C.O. Nomad", StringComparison.OrdinalIgnoreCase)) m = "Nomad";
        if (m.Equals("Grey's Basher", StringComparison.OrdinalIgnoreCase)) m = "Basher";
        if (m.Equals("Grey's Shiv", StringComparison.OrdinalIgnoreCase)) m = "Shiv";
        if (m.Equals("ARGO ATLS GEO", StringComparison.OrdinalIgnoreCase) || m.Equals("ATLS GEO", StringComparison.OrdinalIgnoreCase)) m = "ATLS GEO";

        return Regex.Replace(m, @"\s+", " ").Trim();
    }

    public static string Prettify(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;

        var name = TrailingIdRegex().Replace(raw, "").Trim();

        // Strip CIG internal localization prefixes like "@vehicle_" or "@vehicle"
        if (name.StartsWith("@vehicle_", StringComparison.OrdinalIgnoreCase))
            name = name["@vehicle_".Length..].Trim();
        if (name.StartsWith("@vehicle", StringComparison.OrdinalIgnoreCase))
            name = name["@vehicle".Length..].Trim();

        // 1. Bereits im Format "Model · Brand"
        if (name.Contains('·'))
        {
            var parts1 = name.Split('·', StringSplitOptions.TrimEntries);
            if (parts1.Length == 2)
            {
                var m = NormalizeModelName(parts1[0]);
                var brandPart = parts1[1];
                if (brandPart.Equals("@vehicle", StringComparison.OrdinalIgnoreCase))
                {
                    return Prettify(m);
                }
                return $"{m} · {brandPart}";
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

        // 3. Format: "DRAK_Clipper_795148066379", "RSI_Aurora_Mk2_...", "NameDRAK_Golem", "nameMISC_Starlancer_MAX"
        var parts = name.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return raw;

        string brandKey = parts[0];
        if (brandKey.StartsWith("name", StringComparison.OrdinalIgnoreCase) && brandKey.Length > 4)
            brandKey = brandKey[4..];

        string brand = Brands.TryGetValue(brandKey, out var b) ? b : parts[0];
        var rest = parts.Skip(1).Where(p => !Noise.Contains(p)).ToArray();

        var rawModel = string.Join(' ', rest);
        if (rawModel.Length == 0) return brand;

        return $"{NormalizeModelName(rawModel)} · {brand}";
    }
}
