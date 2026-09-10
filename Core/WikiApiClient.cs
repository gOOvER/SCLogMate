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
    private static readonly ConcurrentQueue<string> _prefetchQueue = new();
    private static readonly ConcurrentDictionary<string, byte> _queuedOrFetched = new(StringComparer.OrdinalIgnoreCase);
    private static bool _isPrefetchRunning;
    private static readonly System.Threading.Lock _prefetchLock = new();

    public static event Action<string, WikiInfo>? ItemResolved;

    static WikiApiClient()
    {
        Http.DefaultRequestHeaders.Add("Accept", "application/json");
        Http.DefaultRequestHeaders.Add("User-Agent", "SCLogMate/1.0.0 (+https://github.com/gOOvER/SCLogMate)");
    }

    public static async Task<WikiInfo?> LookupAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query == "—") return null;

        var clean = CleanSearchTerm(query);
        if (Cache.TryGetValue(clean, out var cached) && cached != null) return cached;

        try
        {
            // 0. Wenn die Anfrage wie eine interne CIG-Item-Klasse aussieht (z.B. mit '_' oder Prefixes)
            if (clean.Contains('_') || clean.StartsWith("Carryable", StringComparison.OrdinalIgnoreCase) ||
                clean.StartsWith("Harvestable", StringComparison.OrdinalIgnoreCase))
            {
                var byClass = await LookupByClassNameAsync(clean);
                if (byClass != null)
                {
                    Cache[clean] = byClass;
                    return byClass;
                }
            }

            // 1. Bei Fahrzeugen / Schiffen suchen
            var vehicle = await SearchVehicleAsync(clean);
            if (vehicle != null)
            {
                Cache[clean] = vehicle;
                return vehicle;
            }

            // 2. Bei Items / Waffen / Komponenten nach Namen suchen
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

    public static async Task<WikiInfo?> LookupByClassNameAsync(string className)
    {
        if (string.IsNullOrWhiteSpace(className)) return null;
        var clean = className.Trim();

        // 1. In-Memory Cache
        if (Cache.TryGetValue(clean, out var cached) && cached != null)
            return cached;

        // 2. Persistent SQLite Cache
        var dbCached = Database.GetCachedWikiItem(clean);
        if (dbCached != null)
        {
            Cache[clean] = dbCached;
            return dbCached;
        }

        // 3. star-citizen.wiki API
        try
        {
            var url = $"items?filter[class_name]={Uri.EscapeDataString(clean)}";
            var response = await Http.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);
                var root = doc.RootElement;

                if (root.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
                {
                    var first = data[0];
                    var name = first.TryGetProperty("name", out var n) ? n.GetString() ?? clean : clean;
                    var classLabel = first.TryGetProperty("classification_label", out var cl) ? cl.GetString() : null;
                    var typeLabel = first.TryGetProperty("type_label", out var tl) ? tl.GetString() : null;
                    var category = MapClassificationToCategory(classLabel, typeLabel);

                    var mfgName = "";
                    if (first.TryGetProperty("manufacturer", out var mfgObj) && mfgObj.ValueKind == JsonValueKind.Object)
                    {
                        if (mfgObj.TryGetProperty("name", out var mn)) mfgName = mn.GetString() ?? "";
                    }

                    var info = new WikiInfo
                    {
                        Name = name,
                        Category = category,
                        Manufacturer = mfgName,
                        WebUrl = first.TryGetProperty("web_url", out var wu) ? wu.GetString() ?? "" : "",
                        Type = typeLabel ?? classLabel ?? ""
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

                    Database.SaveCachedWikiItem(clean, info);
                    Cache[clean] = info;
                    ItemResolved?.Invoke(clean, info);
                    return info;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"WikiApi Lookup Fehler ({clean}): {ex.Message}");
        }

        // Weder im Wiki noch lokal vorhanden -> Unbekanntes Item für spätere Pflege loggen!
        UnknownEventsLogger.LogUnknown("ItemClass", clean);
        return null;
    }

    public static void EnqueueClassPrefetch(string className)
    {
        if (string.IsNullOrWhiteSpace(className)) return;
        var clean = className.Trim();
        if (_queuedOrFetched.TryAdd(clean, 0))
        {
            _prefetchQueue.Enqueue(clean);
            StartPrefetchWorker();
        }
    }

    private static void StartPrefetchWorker()
    {
        lock (_prefetchLock)
        {
            if (_isPrefetchRunning) return;
            _isPrefetchRunning = true;
        }

        Task.Run(async () =>
        {
            while (_prefetchQueue.TryDequeue(out var itemClass))
            {
                try
                {
                    await LookupByClassNameAsync(itemClass);
                    await Task.Delay(250); // Sanfte Rate-Limiting-Pause
                }
                catch { }
            }
            lock (_prefetchLock)
            {
                _isPrefetchRunning = false;
            }
        });
    }

    public static string MapClassificationToCategory(string? classification, string? type)
    {
        var raw = $"{classification} {type}".ToLowerInvariant();
        if (raw.Contains("clothing") || raw.Contains("hat") || raw.Contains("jacket") || raw.Contains("shirt") ||
            raw.Contains("pants") || raw.Contains("shoe") || raw.Contains("boot") || raw.Contains("armor") ||
            raw.Contains("helmet") || raw.Contains("torso") || raw.Contains("arms") || raw.Contains("legs") ||
            raw.Contains("backpack") || raw.Contains("undersuit") || raw.Contains("glove"))
        {
            return I18n.Instance.IsGerman ? "Rüstung & Kleidung" : "Armor & Clothing";
        }
        if (raw.Contains("weapon") || raw.Contains("pistol") || raw.Contains("rifle") || raw.Contains("shotgun") ||
            raw.Contains("sniper") || raw.Contains("smg") || raw.Contains("lmg") || raw.Contains("knife") || raw.Contains("grenade"))
        {
            return I18n.Instance.IsGerman ? "Waffen & Munition" : "Weapons & Ammo";
        }
        if (raw.Contains("quantum") || raw.Contains("shield") || raw.Contains("cooler") || raw.Contains("power") ||
            raw.Contains("engine") || raw.Contains("jump") || raw.Contains("turret") || raw.Contains("missile") ||
            raw.Contains("qdrv") || raw.Contains("shld") || raw.Contains("cool") || raw.Contains("powr"))
        {
            return I18n.Instance.IsGerman ? "Schiffsausrüstung" : "Ship Equipment";
        }
        if (raw.Contains("tool") || raw.Contains("tractor") || raw.Contains("mining") || raw.Contains("salvage") || raw.Contains("fabricat"))
        {
            return I18n.Instance.IsGerman ? "Werkzeuge & Module" : "Tools & Modules";
        }
        if (raw.Contains("mineral") || raw.Contains("ore") || raw.Contains("harvestable") || raw.Contains("gem"))
        {
            return I18n.Instance.IsGerman ? "Mineralien & Erze" : "Minerals & Ores";
        }
        if (raw.Contains("consumable") || raw.Contains("medical") || raw.Contains("food") || raw.Contains("drink") || raw.Contains("medpen"))
        {
            return I18n.Instance.IsGerman ? "Verbrauchsgüter" : "Consumables";
        }
        if (raw.Contains("carryable") || raw.Contains("mission") || raw.Contains("valuable") || raw.Contains("container") || raw.Contains("medal"))
        {
            return I18n.Instance.IsGerman ? "Quest & Wertsachen" : "Quest & Valuables";
        }
        return I18n.Instance.IsGerman ? "Sonstiges" : "Miscellaneous";
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
