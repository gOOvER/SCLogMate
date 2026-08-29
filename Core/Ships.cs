using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SCLogReader.Core;

/// <summary>
/// Macht aus internen Schiffs-Codes (CRUS_Starlifter_A2_Unmanned_Salvage)
/// lesbare Namen ("Starlifter A2 Salvage · Crusader").
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

    // interne Variant-Tags, die niemanden interessieren
    static readonly HashSet<string> Noise = new(StringComparer.OrdinalIgnoreCase)
    {
        "Unmanned", "PU", "AI", "S42", "Template", "Modified"
    };

    [GeneratedRegex(@"_\d{4,}$")]
    private static partial Regex TrailingIdRegex();

    public static string Prettify(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;

        var name = TrailingIdRegex().Replace(raw, "");
        var parts = name.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return raw;

        string brand = Brands.TryGetValue(parts[0], out var b) ? b : parts[0];
        var rest = parts.Skip(1).Where(p => !Noise.Contains(p)).ToArray();

        var model = string.Join(' ', rest);
        return model.Length == 0 ? brand : $"{model} · {brand}";
    }
}
