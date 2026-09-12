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

public sealed class WikiStoreLocationDto
{
    [JsonPropertyName("storeName")] public string StoreName { get; set; } = "";
    [JsonPropertyName("location")] public string Location { get; set; } = "";
    [JsonPropertyName("priceAuec")] public long PriceAuec { get; set; }
    [JsonPropertyName("rentPrice1dAuec")] public long? RentPrice1dAuec { get; set; }
}

public sealed class WikiInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "Fahrzeug"; // Schiff & Fahrzeug, Waffe, Rüstung, Komponente, Item
    [JsonPropertyName("manufacturer")] public string Manufacturer { get; set; } = "";
    [JsonPropertyName("role")] public string Role { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("focus")] public string Focus { get; set; } = "";
    [JsonPropertyName("size")] public string Size { get; set; } = "";
    [JsonPropertyName("crewMin")] public int? CrewMin { get; set; }
    [JsonPropertyName("crewMax")] public int? CrewMax { get; set; }
    [JsonPropertyName("cargoScu")] public double? CargoScu { get; set; }
    [JsonPropertyName("quantumFuel")] public double? QuantumFuel { get; set; }
    [JsonPropertyName("length")] public double? Length { get; set; }
    [JsonPropertyName("beam")] public double? Beam { get; set; }
    [JsonPropertyName("height")] public double? Height { get; set; }
    [JsonPropertyName("mass")] public double? Mass { get; set; }
    [JsonPropertyName("descriptionDe")] public string DescriptionDe { get; set; } = "";
    [JsonPropertyName("descriptionEn")] public string DescriptionEn { get; set; } = "";
    [JsonPropertyName("bestDescription")] public string BestDescription => !string.IsNullOrWhiteSpace(DescriptionDe) ? DescriptionDe : DescriptionEn;
    [JsonPropertyName("descriptionHeader")] public string DescriptionHeader => !string.IsNullOrWhiteSpace(DescriptionDe) ? "📖  BESCHREIBUNG (DEUTSCH)" : "📖  BESCHREIBUNG (ENGLISCH)";
    [JsonPropertyName("imageUrl")] public string ImageUrl { get; set; } = "";
    [JsonPropertyName("thumbnailUrl")] public string ThumbnailUrl { get; set; } = "";
    [JsonPropertyName("localImageBase64")] public string LocalImageBase64 { get; set; } = "";
    [JsonPropertyName("webUrl")] public string WebUrl { get; set; } = "";
    [JsonPropertyName("pledgeUrl")] public string PledgeUrl { get; set; } = "";
    [JsonPropertyName("msrp")] public double? Msrp { get; set; }
    [JsonPropertyName("productionStatus")] public string ProductionStatus { get; set; } = "";
    [JsonPropertyName("specs")] public Dictionary<string, string> Specs { get; set; } = new();
    [JsonPropertyName("storeLocations")] public List<WikiStoreLocationDto> StoreLocations { get; set; } = new();
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

    public static async Task<WikiInfo?> LookupAsync(string query, bool enrichBase64Image = true)
    {
        if (string.IsNullOrWhiteSpace(query) || query == "—") return null;

        var clean = CleanSearchTerm(query);
        if (Cache.TryGetValue(clean, out var cached) && cached != null)
        {
            if (enrichBase64Image && string.IsNullOrEmpty(cached.LocalImageBase64))
            {
                await EnrichLocalImageAsync(cached);
            }
            return cached;
        }

        try
        {
            // 0. Wenn die Anfrage wie eine interne CIG-Item-Klasse aussieht (z.B. mit '_' oder Prefixes)
            if (clean.Contains('_') || clean.StartsWith("Carryable", StringComparison.OrdinalIgnoreCase) ||
                clean.StartsWith("Harvestable", StringComparison.OrdinalIgnoreCase))
            {
                var byClass = await LookupByClassNameAsync(clean);
                if (byClass != null)
                {
                    if (enrichBase64Image) await EnrichLocalImageAsync(byClass);
                    Cache[clean] = byClass;
                    return byClass;
                }
            }

            // 1. Bei Fahrzeugen / Schiffen suchen (prüft auch lokalen SQLite-Cache)
            var vehicle = await SearchVehicleAsync(clean);
            if (vehicle != null)
            {
                if (enrichBase64Image) await EnrichLocalImageAsync(vehicle);
                Cache[clean] = vehicle;
                return vehicle;
            }

            // 2. Bei Items / Waffen / Komponenten nach Namen suchen
            var item = await SearchItemAsync(clean);
            if (item != null)
            {
                if (enrichBase64Image) await EnrichLocalImageAsync(item);
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
            WikiImageCache.PrefetchImage(dbCached.ImageUrl ?? dbCached.ThumbnailUrl);
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
                    WikiImageCache.PrefetchImage(info.ImageUrl ?? info.ThumbnailUrl);
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

        UnknownEventsLogger.LogUnknown("ItemClass", clean);
        return null;
    }

    public static async Task<List<WikiInfo>> SearchWikiAsync(string query, string? category = null, int limit = 25)
    {
        var results = new List<WikiInfo>();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Lokale gecachte Schiffe prüfen
        var cachedVehicles = Database.GetAllCachedWikiVehicles();
        var q = query.Trim().ToLowerInvariant();

        foreach (var v in cachedVehicles)
        {
            if (string.IsNullOrWhiteSpace(q) || v.Name.ToLowerInvariant().Contains(q) || v.Manufacturer.ToLowerInvariant().Contains(q) || v.Role.ToLowerInvariant().Contains(q))
            {
                if (category == null || category == "all" || category == "ships")
                {
                    results.Add(v);
                    seenNames.Add(v.Name);
                    if (results.Count >= limit) return results;
                }
            }
        }

        // 2. Remote API Abfrage für Fahrzeuge
        if (category == null || category == "all" || category == "ships")
        {
            try
            {
                var url = $"vehicles?filter[name]={Uri.EscapeDataString(query)}&page[size]={limit}";
                var response = await Http.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var doc = await JsonDocument.ParseAsync(stream);
                    if (doc.RootElement.TryGetProperty("data", out var vData) && vData.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in vData.EnumerateArray())
                        {
                            var vName = item.TryGetProperty("name", out var np) ? np.GetString() ?? "" : "";
                            if (!string.IsNullOrWhiteSpace(vName) && seenNames.Add(vName))
                            {
                                var parsed = ParseVehicleFromJson(item, vName);
                                Database.SaveCachedWikiVehicle(parsed);
                                results.Add(parsed);
                                if (results.Count >= limit) break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"WikiApi Search Vehicles Fehler: {ex.Message}");
            }
        }

        // 3. Remote API Abfrage für Items / Ausrüstung
        if (results.Count < limit && (category == null || category == "all" || category != "ships"))
        {
            try
            {
                var url = $"items?filter[name]={Uri.EscapeDataString(query)}&page[size]={limit - results.Count}";
                var response = await Http.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var doc = await JsonDocument.ParseAsync(stream);
                    if (doc.RootElement.TryGetProperty("data", out var iData) && iData.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in iData.EnumerateArray())
                        {
                            var iName = item.TryGetProperty("name", out var np) ? np.GetString() ?? "" : "";
                            if (!string.IsNullOrWhiteSpace(iName) && seenNames.Add(iName))
                            {
                                var parsed = ParseItemFromJson(item, iName);
                                results.Add(parsed);
                                if (results.Count >= limit) break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"WikiApi Search Items Fehler: {ex.Message}");
            }
        }

        return results;
    }

    public static async Task EnrichLocalImageAsync(WikiInfo info)
    {
        if (info == null) return;
        var targetUrl = !string.IsNullOrEmpty(info.ImageUrl) ? info.ImageUrl : info.ThumbnailUrl;
        if (!string.IsNullOrEmpty(targetUrl))
        {
            info.LocalImageBase64 = await WikiImageCache.GetImageAsDataUriAsync(targetUrl);
        }
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
                    await Task.Delay(250);
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
        if (s.Contains(" · ")) s = s.Split(" · ")[0].Trim();
        if (s.Contains(" - ")) s = s.Split(" - ")[0].Trim();
        if (s.Contains('(')) s = s.Split('(')[0].Trim();
        return s;
    }

    private static async Task<WikiInfo?> SearchVehicleAsync(string name)
    {
        // 1. Lokalen SQLite-Cache zuerst prüfen!
        var localCached = Database.GetCachedWikiVehicle(name);
        if (localCached != null)
        {
            WikiImageCache.PrefetchImage(localCached.ImageUrl ?? localCached.ThumbnailUrl);
            return localCached;
        }

        var url = $"vehicles?filter[name]={Uri.EscapeDataString(name)}";
        var response = await Http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;

        using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        if (!root.TryGetProperty("data", out var data) || data.GetArrayLength() == 0) return null;

        var parsed = ParseVehicleFromJson(data[0], name);

        // In SQLite persistieren und Bild im Hintergrund vorhalten
        Database.SaveCachedWikiVehicle(parsed);
        WikiImageCache.PrefetchImage(parsed.ImageUrl ?? parsed.ThumbnailUrl);

        return parsed;
    }

    private static WikiInfo ParseVehicleFromJson(JsonElement first, string fallbackName)
    {
        var name = first.TryGetProperty("name", out var n) ? n.GetString() ?? fallbackName : fallbackName;
        var info = new WikiInfo
        {
            Name = name,
            Category = "Schiff & Fahrzeug",
            WebUrl = first.TryGetProperty("web_url", out var wu) ? wu.GetString() ?? "" : "",
            PledgeUrl = first.TryGetProperty("pledge_url", out var pu) ? pu.GetString() ?? "" : "",
            Role = ExtractLocalizedOrString(first, "role"),
            Msrp = first.TryGetProperty("msrp", out var ms) && ms.ValueKind == JsonValueKind.Number ? ms.GetDouble() : null,
            Size = ExtractLocalizedOrString(first, "size")
        };

        // Hersteller
        if (first.TryGetProperty("manufacturer", out var m) && m.ValueKind == JsonValueKind.Object)
        {
            if (m.TryGetProperty("name", out var mn)) info.Manufacturer = mn.GetString() ?? "";
        }

        // Typ
        info.Type = ExtractLocalizedOrString(first, "type");

        // Fokus
        if (first.TryGetProperty("foci", out var fociEl) && fociEl.ValueKind == JsonValueKind.Array && fociEl.GetArrayLength() > 0)
        {
            var firstFocus = fociEl[0];
            if (firstFocus.TryGetProperty("de_DE", out var fde)) info.Focus = fde.GetString() ?? "";
            else if (firstFocus.TryGetProperty("en_EN", out var fen)) info.Focus = fen.GetString() ?? "";
        }

        // Status
        info.ProductionStatus = ExtractLocalizedOrString(first, "production_status");

        // Crew
        if (first.TryGetProperty("crew", out var crewObj) && crewObj.ValueKind == JsonValueKind.Object)
        {
            if (crewObj.TryGetProperty("min", out var cmin) && cmin.ValueKind == JsonValueKind.Number) info.CrewMin = cmin.GetInt32();
            if (crewObj.TryGetProperty("max", out var cmax) && cmax.ValueKind == JsonValueKind.Number) info.CrewMax = cmax.GetInt32();
        }

        // Cargo SCU
        if (first.TryGetProperty("cargo_capacity", out var cc) && cc.ValueKind == JsonValueKind.Number)
        {
            info.CargoScu = cc.GetDouble();
        }
        else if (first.TryGetProperty("cargobay_size", out var cbs) && cbs.ValueKind == JsonValueKind.Number)
        {
            info.CargoScu = cbs.GetDouble();
        }

        // Quantum Fuel
        if (first.TryGetProperty("quantum_fuel_tank_size", out var qft) && qft.ValueKind == JsonValueKind.Number)
        {
            info.QuantumFuel = qft.GetDouble();
        }

        // Abmessungen (Length, Beam, Height, Mass)
        if (first.TryGetProperty("length", out var len) && len.ValueKind == JsonValueKind.Number) info.Length = len.GetDouble();
        if (first.TryGetProperty("beam", out var bm) && bm.ValueKind == JsonValueKind.Number) info.Beam = bm.GetDouble();
        if (first.TryGetProperty("height", out var hg) && hg.ValueKind == JsonValueKind.Number) info.Height = hg.GetDouble();
        if (first.TryGetProperty("mass", out var mass) && mass.ValueKind == JsonValueKind.Number) info.Mass = mass.GetDouble();

        // Beschreibung
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

        var cleanedGdde = CleanGermanDescription(gdde);
        if (!string.IsNullOrWhiteSpace(cleanedGdde) && !IsEnglishText(cleanedGdde))
            info.DescriptionDe = cleanedGdde;
        else if (!string.IsNullOrWhiteSpace(dde) && !IsEnglishText(dde))
            info.DescriptionDe = dde;
        else if (!string.IsNullOrWhiteSpace(cleanedGdde))
            info.DescriptionDe = cleanedGdde;
        else
            info.DescriptionDe = dde;

        // Bilder
        if (first.TryGetProperty("images", out var imgs) && imgs.ValueKind == JsonValueKind.Array && imgs.GetArrayLength() > 0)
        {
            var img = imgs[0];
            if (img.TryGetProperty("thumbnail_url", out var tu)) info.ThumbnailUrl = tu.GetString() ?? "";
            if (img.TryGetProperty("original_url", out var ou)) info.ImageUrl = ou.GetString() ?? "";
        }

        // Specs Dictionary zusammenstellen
        if (info.CargoScu.HasValue) info.Specs["Frachtkapazität"] = $"{info.CargoScu.Value:N0} SCU";
        if (info.CrewMin.HasValue || info.CrewMax.HasValue) info.Specs["Besatzung"] = $"{info.CrewMin ?? 1} - {info.CrewMax ?? info.CrewMin ?? 1} Personen";
        if (info.QuantumFuel.HasValue) info.Specs["Quantum Treibstoff"] = $"{info.QuantumFuel.Value:N0} l";
        if (info.Length.HasValue && info.Beam.HasValue && info.Height.HasValue)
            info.Specs["Abmessungen (L×B×H)"] = $"{info.Length.Value:N1} m × {info.Beam.Value:N1} m × {info.Height.Value:N1} m";
        if (!string.IsNullOrEmpty(info.Size))
        {
            var cleanSize = CleanLocalizedField(info.Size);
            info.Size = cleanSize;
            if (int.TryParse(cleanSize, out _) || cleanSize.Length == 1)
                info.Specs["Fahrzeuggröße"] = $"Größe {cleanSize}";
            else
                info.Specs["Fahrzeuggröße"] = cleanSize;
        }

        // Händlerorte anreichern (FleetCatalog & Standard Verse Händler)
        var catEntry = FleetCatalog.Lookup(info.Name);
        if (catEntry.EstimatedValueAuec > 0)
        {
            var storeLoc = DetermineVerseStore(info.Manufacturer);
            info.StoreLocations.Add(new WikiStoreLocationDto
            {
                StoreName = storeLoc.store,
                Location = storeLoc.location,
                PriceAuec = catEntry.EstimatedValueAuec,
                RentPrice1dAuec = (long)(catEntry.EstimatedValueAuec * 0.02)
            });
        }

        return info;
    }

    private static (string store, string location) DetermineVerseStore(string manufacturer)
    {
        var m = manufacturer.ToLowerInvariant();
        if (m.Contains("origin") || m.Contains("aegis") || m.Contains("misc") || m.Contains("esperia"))
            return ("Astro Armada", "Area 18, ArcCorp");
        if (m.Contains("crusader"))
            return ("Crusader Showroom", "Cloudview Center, Orison");
        return ("New Deal", "Teasa Spaceport, Lorville (Hurston)");
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

        return ParseItemFromJson(data[0], name);
    }

    private static WikiInfo ParseItemFromJson(JsonElement first, string fallbackName)
    {
        var name = first.TryGetProperty("name", out var n) ? n.GetString() ?? fallbackName : fallbackName;
        var info = new WikiInfo
        {
            Name = name,
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

    public static string ExtractLocalizedOrString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop)) return "";
        if (prop.ValueKind == JsonValueKind.String) return CleanLocalizedField(prop.GetString() ?? "");
        if (prop.ValueKind == JsonValueKind.Number) return prop.ToString();
        if (prop.ValueKind == JsonValueKind.Object)
        {
            if (prop.TryGetProperty("de_DE", out var de) && !string.IsNullOrWhiteSpace(de.GetString()))
                return de.GetString()!;
            if (prop.TryGetProperty("en_EN", out var en) && !string.IsNullOrWhiteSpace(en.GetString()))
                return en.GetString()!;
            foreach (var p in prop.EnumerateObject())
            {
                if (p.Value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(p.Value.GetString()))
                    return p.Value.GetString()!;
            }
        }
        return CleanLocalizedField(prop.ToString());
    }

    public static string CleanLocalizedField(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return "";
        var trimmed = val.Trim();
        if (trimmed.StartsWith("{") && trimmed.Contains("\""))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                if (doc.RootElement.TryGetProperty("de_DE", out var de) && !string.IsNullOrWhiteSpace(de.GetString()))
                    return de.GetString()!;
                if (doc.RootElement.TryGetProperty("en_EN", out var en) && !string.IsNullOrWhiteSpace(en.GetString()))
                    return en.GetString()!;
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(prop.Value.GetString()))
                        return prop.Value.GetString()!;
                }
            }
            catch { }
        }
        return trimmed;
    }
}
