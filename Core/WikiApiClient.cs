using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SCLogMate.Core;

public sealed class WikiInfo
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "Fahrzeug"; // Schiff, Waffe, Rüstung, Komponente, Item
    public string Manufacturer { get; set; } = "";
    public string Role { get; set; } = "";
    public string Type { get; set; } = "";
    public string Size { get; set; } = "";
    public string DescriptionDe { get; set; } = "";
    public string DescriptionEn { get; set; } = "";
    public string BestDescription => !string.IsNullOrWhiteSpace(DescriptionDe) ? DescriptionDe : DescriptionEn;
    public string DescriptionHeader => !string.IsNullOrWhiteSpace(DescriptionDe) ? "📖  BESCHREIBUNG (DEUTSCH)" : "📖  BESCHREIBUNG (ENGLISCH)";
    public string ImageUrl { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
    public string WebUrl { get; set; } = "";
    public string PledgeUrl { get; set; } = "";
    public double? Msrp { get; set; }
    public string ProductionStatus { get; set; } = "";
    public Dictionary<string, string> Specs { get; set; } = new();
}

public static class WikiApiClient
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://api.star-citizen.wiki/api/v2/"),
        Timeout = TimeSpan.FromSeconds(10)
    };

    private static readonly ConcurrentDictionary<string, WikiInfo?> Cache = new(StringComparer.OrdinalIgnoreCase);

    static WikiApiClient()
    {
        Http.DefaultRequestHeaders.Add("Accept", "application/json");
        Http.DefaultRequestHeaders.Add("User-Agent", "SCLogMate/1.0.0 (+https://github.com/gOOvER/SCLogMate)");
    }

    public static async Task<WikiInfo?> LookupAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query == "—") return null;

        var clean = CleanSearchTerm(query);
        if (Cache.TryGetValue(clean, out var cached)) return cached;

        try
        {
            // 1. Zuerst bei Fahrzeugen / Schiffen suchen
            var vehicle = await SearchVehicleAsync(clean);
            if (vehicle != null)
            {
                Cache[clean] = vehicle;
                return vehicle;
            }

            // 2. Danach bei Items / Waffen / Komponenten suchen
            var item = await SearchItemAsync(clean);
            if (item != null)
            {
                Cache[clean] = item;
                return item;
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"WikiApi Lookup Fehler ({clean}): {ex.Message}");
        }

        Cache[clean] = null;
        return null;
    }

    private static string CleanSearchTerm(string term)
    {
        var s = term.Trim();
        // Entfernt Hersteller-Suffixe wie " · Drake" oder "(Kauf)"
        if (s.Contains(" · ")) s = s.Split(" · ")[0].Trim();
        if (s.Contains(" - ")) s = s.Split(" - ")[0].Trim();
        if (s.Contains('(')) s = s.Split('(')[0].Trim();
        return s;
    }

    private static async Task<WikiInfo?> SearchVehicleAsync(string name)
    {
        var url = $"vehicles?filter[name]={Uri.EscapeDataString(name)}";
        var response = await Http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;

        using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        if (!root.TryGetProperty("data", out var data) || data.GetArrayLength() == 0) return null;

        var first = data[0];
        var info = new WikiInfo
        {
            Name = first.TryGetProperty("name", out var n) ? n.GetString() ?? name : name,
            Category = "Schiff & Fahrzeug",
            WebUrl = first.TryGetProperty("web_url", out var wu) ? wu.GetString() ?? "" : "",
            PledgeUrl = first.TryGetProperty("pledge_url", out var pu) ? pu.GetString() ?? "" : "",
            Role = first.TryGetProperty("role", out var r) ? r.GetString() ?? "" : "",
            Msrp = first.TryGetProperty("msrp", out var ms) && ms.ValueKind == JsonValueKind.Number ? ms.GetDouble() : null
        };

        // Hersteller
        if (first.TryGetProperty("manufacturer", out var m) && m.ValueKind == JsonValueKind.Object)
        {
            if (m.TryGetProperty("name", out var mn)) info.Manufacturer = mn.GetString() ?? "";
        }

        // Typ / Fokus
        if (first.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.Object)
        {
            if (t.TryGetProperty("de_DE", out var tde)) info.Type = tde.GetString() ?? "";
            else if (t.TryGetProperty("en_EN", out var ten)) info.Type = ten.GetString() ?? "";
        }

        // Status
        if (first.TryGetProperty("production_status", out var ps) && ps.ValueKind == JsonValueKind.Object)
        {
            if (ps.TryGetProperty("de_DE", out var psde)) info.ProductionStatus = psde.GetString() ?? "";
            else if (ps.TryGetProperty("en_EN", out var psen)) info.ProductionStatus = psen.GetString() ?? "";
        }

        // Beschreibung (intelligente Auswahl der echten deutschen In-Game Übersetzung)
        string dde = "", den = "", gdde = "", gden = "";
        if (first.TryGetProperty("description", out var desc) && desc.ValueKind == JsonValueKind.Object)
        {
            if (desc.TryGetProperty("de_DE", out var ddeEl)) dde = ddeEl.GetString() ?? "";
            if (desc.TryGetProperty("en_EN", out var denEl)) den = denEl.GetString() ?? "";
        }

        if (first.TryGetProperty("game_description", out var gdesc) && gdesc.ValueKind == JsonValueKind.Object)
        {
            if (gdesc.TryGetProperty("de_DE", out var gddeEl)) gdde = gddeEl.GetString() ?? "";
            if (gdesc.TryGetProperty("en_EN", out var gdenEl)) gden = gdenEl.GetString() ?? "";
        }

        info.DescriptionEn = !string.IsNullOrWhiteSpace(gden) ? CleanGermanDescription(gden) : den;

        // Echte deutsche Übersetzung bevorzugen:
        var cleanedGdde = CleanGermanDescription(gdde);
        if (!string.IsNullOrWhiteSpace(cleanedGdde) && !IsEnglishText(cleanedGdde))
        {
            info.DescriptionDe = cleanedGdde;
        }
        else if (!string.IsNullOrWhiteSpace(dde) && !IsEnglishText(dde))
        {
            info.DescriptionDe = dde;
        }
        else if (!string.IsNullOrWhiteSpace(cleanedGdde))
        {
            info.DescriptionDe = cleanedGdde;
        }
        else
        {
            info.DescriptionDe = dde;
        }

        // Bilder
        if (first.TryGetProperty("images", out var imgs) && imgs.ValueKind == JsonValueKind.Array && imgs.GetArrayLength() > 0)
        {
            var img = imgs[0];
            if (img.TryGetProperty("thumbnail_url", out var tu)) info.ThumbnailUrl = tu.GetString() ?? "";
            if (img.TryGetProperty("original_url", out var ou)) info.ImageUrl = ou.GetString() ?? "";
        }

        return info;
    }

    private static async Task<WikiInfo?> SearchItemAsync(string name)
    {
        var url = $"items?filter[name]={Uri.EscapeDataString(name)}";
        var response = await Http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;

        using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        if (!root.TryGetProperty("data", out var data) || data.GetArrayLength() == 0) return null;

        var first = data[0];
        var info = new WikiInfo
        {
            Name = first.TryGetProperty("name", out var n) ? n.GetString() ?? name : name,
            Category = first.TryGetProperty("type_label", out var tl) ? tl.GetString() ?? "Item" : "Item",
            WebUrl = first.TryGetProperty("web_url", out var wu) ? wu.GetString() ?? "" : "",
            Type = first.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "",
            Manufacturer = first.TryGetProperty("manufacturer_description", out var md) ? md.GetString() ?? "" : ""
        };

        if (first.TryGetProperty("description", out var desc) && desc.ValueKind == JsonValueKind.Object)
        {
            if (desc.TryGetProperty("de_DE", out var dde)) info.DescriptionDe = dde.GetString() ?? "";
            if (desc.TryGetProperty("en_EN", out var den)) info.DescriptionEn = den.GetString() ?? "";
        }

        if (first.TryGetProperty("images", out var imgs) && imgs.ValueKind == JsonValueKind.Array && imgs.GetArrayLength() > 0)
        {
            var img = imgs[0];
            if (img.TryGetProperty("thumbnail_url", out var tu)) info.ThumbnailUrl = tu.GetString() ?? "";
            if (img.TryGetProperty("original_url", out var ou)) info.ImageUrl = ou.GetString() ?? "";
        }

        return info;
    }

    private static bool IsEnglishText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var lower = text.ToLowerInvariant();
        return lower.Contains(" the ") || lower.Contains(" with ") || lower.Contains(" and ") || 
               lower.Contains(" for ") || lower.Contains(" this ") || lower.Contains(" from ") ||
               lower.Contains(" ship ") || lower.Contains("built for") || lower.Contains("a solo-flyable");
    }

    private static string CleanGermanDescription(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var lines = raw.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var resultLines = new List<string>();
        bool pastHeader = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!pastHeader)
            {
                if (trimmed.StartsWith("Hersteller:", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("Fokus (En):", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("Fokus (De):", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("---", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrEmpty(trimmed))
                {
                    continue;
                }
                pastHeader = true;
            }
            resultLines.Add(line);
        }

        var cleaned = string.Join("\n", resultLines).Trim();
        return !string.IsNullOrEmpty(cleaned) ? cleaned : raw.Trim();
    }
}
