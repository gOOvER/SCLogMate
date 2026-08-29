using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SCLogReader.Core;

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
        Timeout = TimeSpan.FromSeconds(8)
    };

    private static readonly ConcurrentDictionary<string, UexLocationInfo> LocationCache = new(StringComparer.OrdinalIgnoreCase);
    private static bool _hasLoadedStations = false;
    private static readonly object _initLock = new();
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
        lock (_initLock)
        {
            if (_hasLoadedStations) return;
            _hasLoadedStations = true;
        }

        try
        {
            await LoadSpaceStationsAsync();
            await LoadCitiesAsync();
        }
        catch (Exception ex)
        {
            Logger.Log($"UexApi Init Fehler: {ex.Message}");
        }
    }

    private static async Task LoadSpaceStationsAsync()
    {
        var response = await Http.GetAsync("space_stations");
        if (!response.IsSuccessStatusCode) return;

        using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) return;

        foreach (var item in data.EnumerateArray())
        {
            var info = ParseLocation(item);
            if (!string.IsNullOrEmpty(info.Name))
            {
                LocationCache[info.Name] = info;
            }
        }
    }

    private static async Task LoadCitiesAsync()
    {
        var response = await Http.GetAsync("cities");
        if (!response.IsSuccessStatusCode) return;

        using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) return;

        foreach (var item in data.EnumerateArray())
        {
            var info = ParseLocation(item);
            if (!string.IsNullOrEmpty(info.Name))
            {
                LocationCache[info.Name] = info;
            }
        }
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
}
