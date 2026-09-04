using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SCLogMate.Core;

public sealed class UexCommodityTerminalPrice
{
    public string TerminalName { get; set; } = "";
    public decimal PriceBuy { get; set; }
    public decimal PriceSell { get; set; }
    public decimal StockScu { get; set; }
    public decimal DemandScu { get; set; }
    public long DateModified { get; set; }
}

public sealed class UexCommodityPriceInfo
{
    public string CommodityName { get; set; } = "";
    public decimal BestSell { get; set; }
    public string? BestSellTerminal { get; set; }
    public decimal BestBuy { get; set; }
    public string? BestBuyTerminal { get; set; }
    public decimal AvgSell { get; set; }
    public int TerminalsCount { get; set; }
    public int SellTerminalsCount { get; set; }
    public int BuyTerminalsCount { get; set; }
    public DateTimeOffset? LastReportedAt { get; set; }
    public List<UexCommodityTerminalPrice> Terminals { get; set; } = [];
}

public sealed class UexLocationInfo
{
    public string Name { get; set; } = "";
    public string StarSystemName { get; set; } = "";
    public string PlanetName { get; set; } = "";
    public string MoonName { get; set; } = "";
    public string JurisdictionName { get; set; } = "";
    public string FactionName { get; set; } = "";
    public bool HasRefuel { get; set; }
    public bool HasRepair { get; set; }
    public bool HasClinic { get; set; }
    public bool HasRefinery { get; set; }
    public bool HasCargoCenter { get; set; }
    public bool HasFreightElevator { get; set; }
    public bool HasTradeTerminal { get; set; }
    public bool HasHabitation { get; set; }
    public bool HasShops { get; set; }
    public bool IsArmistice { get; set; }
    public bool IsMonitored { get; set; }
    public bool IsJumpPoint { get; set; }
    public string WikiUrl { get; set; } = "";
    public string UexUrl => "https://uexcorp.space/trade/price_finder?location=" + Uri.EscapeDataString(Name);
    public string TerminalsUrl => "https://uexcorp.space/terminals";

    public bool HasAnyServices => HasRefuel || HasRepair || HasClinic || HasRefinery || HasCargoCenter || HasFreightElevator || HasTradeTerminal;
}

public static class UexApiClient
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://api.uexcorp.uk/2.0/"),
        Timeout = TimeSpan.FromSeconds(15)
    };

    private static readonly ConcurrentDictionary<string, UexCommodityPriceInfo> CommodityPricesCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly SemaphoreSlim CommodityPricesLock = new(1, 1);
    public static DateTimeOffset? LastCommodityPricesFetch { get; private set; }
    public static readonly TimeSpan CommodityPricesCacheDuration = TimeSpan.FromHours(1);
    private static bool _diskCacheChecked = false;
    private static string CommodityCacheFilePath => System.IO.Path.Combine(Settings.Dir, "uex", "commodities_prices_cache.json");

    private static readonly ConcurrentDictionary<string, UexLocationInfo> LocationCache = new(StringComparer.OrdinalIgnoreCase);
    private static bool _hasLoadedStations = false;
    private static readonly SemaphoreSlim LocationsLoadLock = new(1, 1);
    private static string? _apiKey;

    static UexApiClient()
    {
        Http.DefaultRequestHeaders.Add("Accept", "application/json");
        Http.DefaultRequestHeaders.Add("User-Agent", "SCLogMate/1.0.0 (+https://github.com/gOOvER/SCLogMate)");
    }

    public static void SetApiKey(string? key)
    {
        _apiKey = string.IsNullOrWhiteSpace(key) ? null : key.Trim();
        Http.DefaultRequestHeaders.Remove("Authorization");
        Http.DefaultRequestHeaders.Remove("secret-key");
        if (!string.IsNullOrEmpty(_apiKey))
        {
            Http.DefaultRequestHeaders.Add("secret-key", _apiKey);
        }
    }

    public static async Task<(bool Success, string Message)> TestConnectionAsync(string? key = null)
    {
        if (key != null) SetApiKey(key);
        try
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                var resPub = await Http.GetAsync("commodities");
                return resPub.IsSuccessStatusCode
                    ? (true, "✓ UEX API 2.0 verbunden (Öffentlicher Modus)")
                    : (false, $"⚠ UEX API Fehler (HTTP {(int)resPub.StatusCode})");
            }

            // Mit persönlichem Schlüssel prüfen wir den /user Endpoint
            var res = await Http.GetAsync("user");
            var body = await res.Content.ReadAsStringAsync();
            if (res.IsSuccessStatusCode)
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Object)
                    {
                        var username = dataEl.TryGetProperty("username", out var un) ? un.GetString() : null;
                        var handle = dataEl.TryGetProperty("handle", out var hn) ? hn.GetString() : null;
                        var name = !string.IsNullOrEmpty(handle) ? handle : username;
                        if (!string.IsNullOrEmpty(name))
                        {
                            return (true, $"✓ UEX API 2.0 verbunden · Account: {name}");
                        }
                    }
                }
                catch { /* ignore */ }

                return (true, "✓ UEX API 2.0 erfolgreich verbunden & Schlüssel authentifiziert!");
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                if (root.TryGetProperty("status", out var stEl) && stEl.GetString() == "invalid_secret_key")
                {
                    return (false, "⚠ UEX: Geheimer Schlüssel ist ungültig (bitte neu aus UEX-Profil kopieren)");
                }
                if (root.TryGetProperty("message", out var msgEl) && !string.IsNullOrWhiteSpace(msgEl.GetString()))
                {
                    return (false, $"⚠ UEX: {msgEl.GetString()}");
                }
            }
            catch { /* ignore */ }

            return (false, $"⚠ UEX API HTTP {(int)res.StatusCode}: {res.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            return (false, $"⚠ Verbindungsfehler: {ex.Message}");
        }
    }

    public static async Task<UexLocationInfo?> LookupLocationAsync(string locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName) || locationName == "—") return null;

        var clean = CleanName(locationName);

        // 1. Zuerst im Cache prüfen
        if (LocationCache.TryGetValue(clean, out var found)) return found;

        // Teilübereinstimmung im Cache
        var match = LocationCache.Values.FirstOrDefault(v => 
            v.Name.Contains(clean, StringComparison.OrdinalIgnoreCase) || 
            clean.Contains(v.Name, StringComparison.OrdinalIgnoreCase));
        if (match != null) return match;

        // 2. Falls noch nicht geladen, Stationen & Städte initial laden
        if (!_hasLoadedStations)
        {
            await EnsureLocationsLoadedAsync();
            if (LocationCache.TryGetValue(clean, out var loaded)) return loaded;

            match = LocationCache.Values.FirstOrDefault(v => 
                v.Name.Contains(clean, StringComparison.OrdinalIgnoreCase) || 
                clean.Contains(v.Name, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;
        }

        return null;
    }

    private static string CleanName(string s)
    {
        var clean = s.Trim();
        if (clean.Contains(" · ")) clean = clean.Split(" · ")[0].Trim();
        if (clean.Contains(" - ")) clean = clean.Split(" - ")[0].Trim();
        if (clean.Contains('(')) clean = clean.Split('(')[0].Trim();
        return clean;
    }

    private static async Task EnsureLocationsLoadedAsync()
    {
        await LocationsLoadLock.WaitAsync();
        try
        {
            if (_hasLoadedStations) return;

            var stationsLoaded = await LoadSpaceStationsAsync();
            var citiesLoaded = await LoadCitiesAsync();
            _hasLoadedStations = stationsLoaded && citiesLoaded;
            if (!_hasLoadedStations)
            {
                Logger.Log("UEX API: Standortdaten konnten nicht vollständig geladen werden und werden später erneut versucht.");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"UexApi Init Fehler: {ex.Message}");
        }
        finally
        {
            LocationsLoadLock.Release();
        }
    }

    private static async Task<bool> LoadSpaceStationsAsync()
    {
        var response = await Http.GetAsync("space_stations");
        if (!response.IsSuccessStatusCode) return false;

        using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) return false;

        foreach (var item in data.EnumerateArray())
        {
            var info = ParseLocation(item);
            if (!string.IsNullOrEmpty(info.Name))
            {
                LocationCache[info.Name] = info;
            }
        }
        return true;
    }

    private static async Task<bool> LoadCitiesAsync()
    {
        var response = await Http.GetAsync("cities");
        if (!response.IsSuccessStatusCode) return false;

        using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) return false;

        foreach (var item in data.EnumerateArray())
        {
            var info = ParseLocation(item);
            if (!string.IsNullOrEmpty(info.Name))
            {
                LocationCache[info.Name] = info;
            }
        }
        return true;
    }

    private static UexLocationInfo ParseLocation(JsonElement item)
    {
        var info = new UexLocationInfo
        {
            Name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
            StarSystemName = item.TryGetProperty("star_system_name", out var s) ? s.GetString() ?? "" : "",
            PlanetName = item.TryGetProperty("planet_name", out var p) ? p.GetString() ?? "" : "",
            MoonName = item.TryGetProperty("moon_name", out var m) ? m.GetString() ?? "" : "",
            JurisdictionName = item.TryGetProperty("jurisdiction_name", out var j) ? j.GetString() ?? "" : "",
            FactionName = item.TryGetProperty("faction_name", out var f) ? f.GetString() ?? "" : "",
            HasRefuel = item.TryGetProperty("has_refuel", out var rf) && rf.GetInt32() == 1,
            HasRepair = item.TryGetProperty("has_repair", out var rp) && rp.GetInt32() == 1,
            HasClinic = item.TryGetProperty("has_clinic", out var cl) && cl.GetInt32() == 1,
            HasRefinery = item.TryGetProperty("has_refinery", out var rn) && rn.GetInt32() == 1,
            HasCargoCenter = item.TryGetProperty("has_cargo_center", out var cc) && cc.GetInt32() == 1,
            HasFreightElevator = item.TryGetProperty("has_freight_elevator", out var fe) && fe.GetInt32() == 1,
            HasTradeTerminal = item.TryGetProperty("has_trade_terminal", out var tt) && tt.GetInt32() == 1,
            HasHabitation = item.TryGetProperty("has_habitation", out var hb) && hb.GetInt32() == 1,
            HasShops = item.TryGetProperty("has_shops", out var sh) && sh.GetInt32() == 1,
            IsArmistice = item.TryGetProperty("is_armistice", out var ar) && ar.GetInt32() == 1,
            IsMonitored = item.TryGetProperty("is_monitored", out var mo) && mo.GetInt32() == 1,
            IsJumpPoint = item.TryGetProperty("is_jump_point", out var jp) && jp.GetInt32() == 1,
            WikiUrl = item.TryGetProperty("wiki", out var w) ? w.GetString() ?? "" : ""
        };

        return info;
    }

    public static void EnsureDiskCacheLoaded()
    {
        if (_diskCacheChecked) return;
        _diskCacheChecked = true;
        try
        {
            var path = CommodityCacheFilePath;
            if (System.IO.File.Exists(path))
            {
                var fi = new System.IO.FileInfo(path);
                var json = System.IO.File.ReadAllText(path);
                var list = JsonSerializer.Deserialize<List<UexCommodityPriceInfo>>(json);
                if (list != null && list.Count > 0)
                {
                    CommodityPricesCache.Clear();
                    foreach (var item in list)
                    {
                        CommodityPricesCache[item.CommodityName] = item;
                    }
                    LastCommodityPricesFetch = fi.LastWriteTimeUtc;
                    Logger.Log($"UEX API: {list.Count} Marktpreise aus lokalem Cache geladen (Stand: {fi.LastWriteTime:dd.MM.yy HH:mm}).");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"UEX API Disk-Cache Fehler: {ex.Message}");
        }
    }

    public static async Task<bool> FetchCommodityPricesAsync(bool force = false)
    {
        EnsureDiskCacheLoaded();

        if (!force && LastCommodityPricesFetch.HasValue &&
            (DateTimeOffset.UtcNow - LastCommodityPricesFetch.Value) < CommodityPricesCacheDuration &&
            !CommodityPricesCache.IsEmpty)
        {
            return true;
        }

        await CommodityPricesLock.WaitAsync();
        try
        {
            if (!force && LastCommodityPricesFetch.HasValue &&
                (DateTimeOffset.UtcNow - LastCommodityPricesFetch.Value) < CommodityPricesCacheDuration &&
                !CommodityPricesCache.IsEmpty)
            {
                return true;
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var response = await Http.GetAsync("commodities_prices_all", cts.Token);
            if (!response.IsSuccessStatusCode) return false;

            using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token);
            var root = doc.RootElement;

            if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                return false;

            var grouped = new Dictionary<string, List<UexCommodityTerminalPrice>>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in data.EnumerateArray())
            {
                var commName = item.TryGetProperty("commodity_name", out var cn) ? cn.GetString() : null;
                if (string.IsNullOrWhiteSpace(commName)) continue;

                var termName = item.TryGetProperty("terminal_name", out var tn) ? tn.GetString() ?? "Unbekannt" : "Unbekannt";
                decimal buy = item.TryGetProperty("price_buy", out var pb) && pb.TryGetDecimal(out var dBuy) ? dBuy : 0;
                decimal sell = item.TryGetProperty("price_sell", out var ps) && ps.TryGetDecimal(out var dSell) ? dSell : 0;
                decimal stock = item.TryGetProperty("scu_buy", out var sb) && sb.TryGetDecimal(out var dStock) ? dStock : 0;
                decimal demand = item.TryGetProperty("scu_sell_stock", out var ss) && ss.TryGetDecimal(out var dDemand) ? dDemand : 0;
                long dateMod = item.TryGetProperty("date_modified", out var dm) && dm.TryGetInt64(out var dMod) ? dMod : 0;

                if (!grouped.TryGetValue(commName, out var list))
                {
                    list = new List<UexCommodityTerminalPrice>();
                    grouped[commName] = list;
                }

                list.Add(new UexCommodityTerminalPrice
                {
                    TerminalName = termName,
                    PriceBuy = buy,
                    PriceSell = sell,
                    StockScu = stock,
                    DemandScu = demand,
                    DateModified = dateMod
                });
            }

            CommodityPricesCache.Clear();
            var resultList = new List<UexCommodityPriceInfo>();
            foreach (var (commName, rows) in grouped)
            {
                var bestSell = rows.Where(r => r.PriceSell > 0).OrderByDescending(r => r.PriceSell).FirstOrDefault();
                var bestBuy = rows.Where(r => r.PriceBuy > 0).OrderBy(r => r.PriceBuy).FirstOrDefault();
                var avgSell = rows.Where(r => r.PriceSell > 0).Select(r => r.PriceSell).DefaultIfEmpty(0).Average();
                long maxSeen = rows.Select(r => r.DateModified).DefaultIfEmpty(0).Max();

                var info = new UexCommodityPriceInfo
                {
                    CommodityName = commName,
                    BestSell = bestSell?.PriceSell ?? 0,
                    BestSellTerminal = bestSell?.TerminalName,
                    BestBuy = bestBuy?.PriceBuy ?? 0,
                    BestBuyTerminal = bestBuy?.TerminalName,
                    AvgSell = Math.Round(avgSell, 0),
                    TerminalsCount = rows.Count,
                    SellTerminalsCount = rows.Count(r => r.PriceSell > 0),
                    BuyTerminalsCount = rows.Count(r => r.PriceBuy > 0),
                    LastReportedAt = maxSeen > 0 ? DateTimeOffset.FromUnixTimeSeconds(maxSeen) : null,
                    Terminals = rows
                };

                CommodityPricesCache[commName] = info;
                resultList.Add(info);
            }

            LastCommodityPricesFetch = DateTimeOffset.UtcNow;

            // In Disk-Cache sichern
            try
            {
                var uexDir = System.IO.Path.Combine(Settings.Dir, "uex");
                System.IO.Directory.CreateDirectory(uexDir);
                var json = JsonSerializer.Serialize(resultList, new JsonSerializerOptions { WriteIndented = false });
                System.IO.File.WriteAllText(CommodityCacheFilePath, json);
            }
            catch (Exception ex)
            {
                Logger.Log($"Fehler beim Speichern des UEX Disk-Caches: {ex.Message}");
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Log($"Fehler beim Abrufen der UEX-Marktpreise: {ex.Message}");
            return false;
        }
        finally
        {
            CommodityPricesLock.Release();
        }
    }

    public static UexCommodityPriceInfo? GetCommodityPrice(string name)
    {
        EnsureDiskCacheLoaded();
        if (string.IsNullOrWhiteSpace(name)) return null;
        return CommodityPricesCache.TryGetValue(name, out var info) ? info : null;
    }

    public static IReadOnlyDictionary<string, UexCommodityPriceInfo> GetAllCommodityPrices()
    {
        EnsureDiskCacheLoaded();
        return CommodityPricesCache;
    }
}
