using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using SCLogMate.Models;

namespace SCLogMate.Core;

public class CombatAnalyticsDto
{
    [JsonPropertyName("totalKills")] public int TotalKills { get; set; }
    [JsonPropertyName("totalDeaths")] public int TotalDeaths { get; set; }
    [JsonPropertyName("kdRatio")] public double KdRatio { get; set; }
    [JsonPropertyName("shipLosses")] public int ShipLosses { get; set; }
    [JsonPropertyName("medRespawns")] public int MedRespawns { get; set; }
    [JsonPropertyName("estimatedKitLossAuec")] public long EstimatedKitLossAuec { get; set; }
    [JsonPropertyName("estimatedShipClaimLossAuec")] public long EstimatedShipClaimLossAuec { get; set; }
    [JsonPropertyName("estimatedTotalLossAuec")] public long EstimatedTotalLossAuec { get; set; }

    [JsonPropertyName("deathCauses")] public List<CombatCategoryStatDto> DeathCauses { get; set; } = new();
    [JsonPropertyName("dangerZones")] public List<DangerZoneDto> DangerZones { get; set; } = new();
    [JsonPropertyName("recentCasualties")] public List<CasualtyIncidentDto> RecentCasualties { get; set; } = new();
}

public class CombatCategoryStatDto
{
    [JsonPropertyName("label")] public string Label { get; set; } = "";
    [JsonPropertyName("count")] public int Count { get; set; }
    [JsonPropertyName("percent")] public double Percent { get; set; }
    [JsonPropertyName("color")] public string Color { get; set; } = "#EF4444";
}

public class DangerZoneDto
{
    [JsonPropertyName("location")] public string Location { get; set; } = "";
    [JsonPropertyName("system")] public string System { get; set; } = "Stanton";
    [JsonPropertyName("incidentCount")] public int IncidentCount { get; set; }
    [JsonPropertyName("deaths")] public int Deaths { get; set; }
    [JsonPropertyName("shipLosses")] public int ShipLosses { get; set; }
    [JsonPropertyName("threatLevel")] public string ThreatLevel { get; set; } = "Mittel"; // Hoch, Mittel, Niedrig
}

public class CasualtyIncidentDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("timestamp")] public string Timestamp { get; set; } = "";
    [JsonPropertyName("session")] public string Session { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "Death"; // Death, ShipLoss, Crash
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("detail")] public string Detail { get; set; } = "";
    [JsonPropertyName("ship")] public string? Ship { get; set; }
    [JsonPropertyName("location")] public string Location { get; set; } = "Unbekannt";
    [JsonPropertyName("estimatedCostAuec")] public int EstimatedCostAuec { get; set; }
}

public static class CombatAnalyticsService
{
    private const int AverageKitReequipCostAuec = 48000;
    private const int AverageShipExpediteFeeAuec = 22000;

    public static CombatAnalyticsDto GetAnalytics(string? session = null)
    {
        Database.EnsureInitialized();
        var result = new CombatAnalyticsDto();

        List<LogEntry> allEvents;
        if (!string.IsNullOrEmpty(session) && session != "__all__" && session != "__live__")
        {
            allEvents = Database.GetTimelineEventsForSession(session);
        }
        else
        {
            allEvents = Database.AllTimelineEvents();
        }

        var combatEvents = allEvents
            .Where(e => e.Kind is EventKind.Death or EventKind.Kill or EventKind.ShipLoss or EventKind.Crash or EventKind.MedBed)
            .OrderBy(e => e.Time)
            .ToList();

        result.TotalKills = combatEvents.Count(e => e.Kind == EventKind.Kill);
        result.TotalDeaths = combatEvents.Count(e => e.Kind == EventKind.Death);
        result.ShipLosses = combatEvents.Count(e => e.Kind is EventKind.ShipLoss or EventKind.Crash);
        result.MedRespawns = combatEvents.Count(e => e.Kind == EventKind.MedBed);

        result.KdRatio = result.TotalDeaths > 0
            ? Math.Round((double)result.TotalKills / result.TotalDeaths, 2)
            : result.TotalKills;

        result.EstimatedKitLossAuec = (long)result.TotalDeaths * AverageKitReequipCostAuec;
        result.EstimatedShipClaimLossAuec = (long)result.ShipLosses * AverageShipExpediteFeeAuec;
        result.EstimatedTotalLossAuec = result.EstimatedKitLossAuec + result.EstimatedShipClaimLossAuec;

        // Ursachenanalyse der Tode
        int pvpDeaths = 0;
        int pveDeaths = 0;
        int collisionDeaths = 0;
        int suicideOrAccident = 0;

        foreach (var d in combatEvents.Where(e => e.Kind == EventKind.Death))
        {
            var text = d.Detail ?? "";
            if (text.Contains("Kollision", StringComparison.OrdinalIgnoreCase) || text.Contains("Crash", StringComparison.OrdinalIgnoreCase))
            {
                collisionDeaths++;
            }
            else if (text.Contains("Selbstzerstörung", StringComparison.OrdinalIgnoreCase) || text.Contains("Suicide", StringComparison.OrdinalIgnoreCase))
            {
                suicideOrAccident++;
            }
            else if (text.Contains("NineTails", StringComparison.OrdinalIgnoreCase) || text.Contains("XenoThreat", StringComparison.OrdinalIgnoreCase) || text.Contains("Pirate", StringComparison.OrdinalIgnoreCase) || text.Contains("Security", StringComparison.OrdinalIgnoreCase))
            {
                pveDeaths++;
            }
            else if (text.Contains("getötet von", StringComparison.OrdinalIgnoreCase))
            {
                pvpDeaths++;
            }
            else
            {
                suicideOrAccident++;
            }
        }

        int deathTotal = Math.Max(1, result.TotalDeaths);
        result.DeathCauses = new List<CombatCategoryStatDto>
        {
            new() { Label = "PvP (Gegnerische Spieler)", Count = pvpDeaths, Percent = Math.Round((double)pvpDeaths / deathTotal * 100, 1), Color = "#EF4444" },
            new() { Label = "PvE / Gesetzlose (NPCs)", Count = pveDeaths, Percent = Math.Round((double)pveDeaths / deathTotal * 100, 1), Color = "#F59E0B" },
            new() { Label = "Schiffskollision / Crash", Count = collisionDeaths, Percent = Math.Round((double)collisionDeaths / deathTotal * 100, 1), Color = "#EC4899" },
            new() { Label = "Umwelt, Sturz & Unfall", Count = suicideOrAccident, Percent = Math.Round((double)suicideOrAccident / deathTotal * 100, 1), Color = "#8B5CF6" }
        };

        // Gefahrenzonen / Hotspots Heatmap
        var zoneMap = new Dictionary<string, (int Total, int Deaths, int ShipLosses)>(StringComparer.OrdinalIgnoreCase);
        string currentLoc = "Unbekannter Sektor";

        foreach (var ev in allEvents.OrderBy(e => e.Time))
        {
            if (ev.Kind == EventKind.Location && !string.IsNullOrEmpty(ev.Detail))
            {
                var resolved = Locations.ResolveLocation(ev.Detail);
                if (resolved.DisplayName != "—") currentLoc = resolved.DisplayName;
            }
            else if (ev.Kind is EventKind.Death or EventKind.ShipLoss or EventKind.Crash)
            {
                var loc = !string.IsNullOrEmpty(ev.Location) ? ev.Location : currentLoc;
                if (!zoneMap.ContainsKey(loc)) zoneMap[loc] = (0, 0, 0);

                var (tot, dth, sl) = zoneMap[loc];
                if (ev.Kind == EventKind.Death) dth++;
                else sl++;
                zoneMap[loc] = (tot + 1, dth, sl);
            }
        }

        result.DangerZones = zoneMap
            .OrderByDescending(kv => kv.Value.Total)
            .Take(8)
            .Select(kv =>
            {
                var loc = kv.Key;
                var resolved = Locations.ResolveLocation(loc);
                var total = kv.Value.Total;
                string threat = total >= 4 ? "Kritisch" : (total >= 2 ? "Hoch" : "Mäßig");
                return new DangerZoneDto
                {
                    Location = loc,
                    System = resolved.SystemName != "—" ? resolved.SystemName : "Stanton",
                    IncidentCount = total,
                    Deaths = kv.Value.Deaths,
                    ShipLosses = kv.Value.ShipLosses,
                    ThreatLevel = threat
                };
            })
            .ToList();

        // Letzte Vorfälle / Zwischenfälle
        result.RecentCasualties = combatEvents
            .Where(e => e.Kind is EventKind.Death or EventKind.ShipLoss or EventKind.Crash)
            .OrderByDescending(e => e.Time)
            .Take(25)
            .Select(e =>
            {
                int estCost = e.Kind == EventKind.Death ? AverageKitReequipCostAuec : AverageShipExpediteFeeAuec;
                string typeLabel = e.Kind == EventKind.Death ? "Pilotentod" : (e.Kind == EventKind.Crash ? "Kollision" : "Schiffsverlust");
                return new CasualtyIncidentDto
                {
                    Id = Guid.NewGuid().ToString("N")[..8],
                    Timestamp = e.Time.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss"),
                    Session = e.Location ?? "",
                    Type = typeLabel,
                    Title = $"{typeLabel}: {(!string.IsNullOrEmpty(e.Ship) ? e.Ship : "Ausrüstung verloren")}",
                    Detail = e.Detail ?? "",
                    Ship = e.Ship,
                    Location = !string.IsNullOrEmpty(e.Location) ? e.Location : "Im Einsatzgebiet",
                    EstimatedCostAuec = estCost
                };
            })
            .ToList();

        return result;
    }
}
