using System;
using System.Collections.Generic;
using System.Linq;
using SCLogMate.Models;

namespace SCLogMate.Core;

public sealed class FlightSummary
{
    public double TotalDistanceGm { get; set; }
    public double TotalDistanceKm { get; set; }
    public TimeSpan TotalFlightDuration { get; set; }
    public TimeSpan TotalSessionDuration { get; set; }
    public int QuantumJumps { get; set; }
    public int SortieCount { get; set; }
    public int ShipLosses { get; set; }
    public List<string> VisitedBodies { get; set; } = new();
    public List<string> UsedShips { get; set; } = new();

    public string TotalDistanceText => TotalDistanceGm > 0.05
        ? $"{TotalDistanceGm:F1} GM ({TotalDistanceKm:N0} km)"
        : $"{TotalDistanceKm:N0} km";

    public string ShipsUsedText => UsedShips.Count == 1 ? "1 Schiff geflogen" : $"{UsedShips.Count} Schiffe eingesetzt";
    public string VisitedBodiesText => VisitedBodies.Count == 1 ? "1 Himmelskörper" : $"{VisitedBodies.Count} Himmelskörper besucht";
    public string ShipLossesText => ShipLosses == 0 ? "Keine Verluste" : $"{ShipLosses} Schiffsverlust(e)";
}

public static class FlightRecorderService
{
    public static (List<FlightTimelineItem> Items, FlightSummary Summary) BuildTimeline(IEnumerable<LogEntry> sessionEntries)
    {
        var entries = sessionEntries.OrderBy(e => e.Time).ToList();
        var items = new List<FlightTimelineItem>();
        var summary = new FlightSummary();

        if (!entries.Any()) return (items, summary);

        var sessionStart = entries.First().Time;
        var sessionEnd = entries.Last().Time;
        summary.TotalSessionDuration = sessionEnd - sessionStart;

        string currentShip = "";
        ResolvedLocation? lastResolvedLoc = null;

        var visitedBodiesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var usedShipsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var relSpan = entry.Time - sessionStart;
            var relText = (entry.Time - sessionStart).TotalDays >= 1
                ? entry.Time.ToLocalTime().ToString("dd.MM.yyyy")
                : $"+{(int)relSpan.TotalHours:D2}:{relSpan.Minutes:D2}:{relSpan.Seconds:D2}";

            // 1. Schiffs-Spawn / Auslagerung / Zerstörung / Kollision
            if (entry.Kind == EventKind.Vehicle || entry.Kind == EventKind.ShipLoss || entry.Kind == EventKind.Crash)
            {
                var shipName = !string.IsNullOrEmpty(entry.Ship) ? entry.Ship : entry.Detail;
                if (!string.IsNullOrEmpty(shipName) && shipName != "Fahrzeug" && shipName != "Schiff")
                {
                    currentShip = shipName;
                    usedShipsSet.Add(currentShip);
                }

                if (entry.Kind == EventKind.Vehicle)
                {
                    summary.SortieCount++;
                    items.Add(new FlightTimelineItem
                    {
                        Timestamp = entry.Time,
                        RelativeTimeText = relText,
                        Type = TimelineItemType.Spawn,
                        Title = $"Schiff bereitgestellt: {currentShip}",
                        Subtitle = !string.IsNullOrEmpty(entry.Detail) ? entry.Detail : "Flugbereites Schiff am Hangar/Pad bereitgestellt",
                        ShipName = currentShip,
                        LocationName = lastResolvedLoc?.DisplayName ?? "Raumhafen",
                        SystemName = lastResolvedLoc?.SystemName ?? "Stanton"
                    });
                }
                else
                {
                    summary.ShipLosses++;
                    items.Add(new FlightTimelineItem
                    {
                        Timestamp = entry.Time,
                        RelativeTimeText = relText,
                        Type = TimelineItemType.CombatDeath,
                        Title = entry.Kind == EventKind.Crash ? $"Kollision / Absturz: {currentShip}" : $"Schiffsverlust: {currentShip}",
                        Subtitle = entry.Detail,
                        ShipName = currentShip,
                        LocationName = lastResolvedLoc?.DisplayName ?? "Unbekannter Sektor",
                        SystemName = lastResolvedLoc?.SystemName ?? "Stanton"
                    });
                }
            }
            // 2. Ortswechsel, Hangar & Quantum Travel
            else if (entry.Kind == EventKind.Location || entry.Kind == EventKind.Quantum || entry.Kind == EventKind.Hangar)
            {
                var locName = entry.Kind == EventKind.Quantum && !string.IsNullOrEmpty(entry.Detail) ? entry.Detail : entry.Detail;
                var resolved = Locations.ResolveLocation(locName);
                if (resolved.DisplayName != "—" && resolved.DisplayName != lastResolvedLoc?.DisplayName)
                {
                    if (!string.IsNullOrEmpty(resolved.ParentBody) && resolved.ParentBody != "—")
                        visitedBodiesSet.Add(resolved.ParentBody);

                    // Quantum Sprung Distanz berechnen
                    double distKm = 0;
                    double distGm = 0;
                    if (lastResolvedLoc != null && !string.IsNullOrEmpty(lastResolvedLoc.DisplayName))
                    {
                        var fromObj = StarmapData.FindObject(lastResolvedLoc.DisplayName);
                        var toObj = StarmapData.FindObject(resolved.DisplayName);
                        if (fromObj != null && toObj != null)
                        {
                            var route = StarmapData.CalculateRoute(fromObj, toObj, StarmapData.AvailableDrives[0]);
                            distKm = route.DistKm;
                            distGm = route.DistGm;
                            summary.TotalDistanceKm += distKm;
                            summary.TotalDistanceGm += distGm;
                            summary.TotalFlightDuration += route.FlightTime;
                            summary.QuantumJumps++;

                            items.Add(new FlightTimelineItem
                            {
                                Timestamp = entry.Time,
                                RelativeTimeText = relText,
                                Type = TimelineItemType.QuantumTravel,
                                Title = $"Quantum Sprung: {lastResolvedLoc.DisplayName} ➔ {resolved.DisplayName}",
                                Subtitle = $"Reise durch {resolved.SystemName}-System · Distanz: {distGm:F1} GM",
                                Detail = $"Zielkoordinate erreicht. {resolved.ArmisticeStatusText}",
                                LocationName = resolved.DisplayName,
                                SystemName = resolved.SystemName,
                                ParentBody = resolved.ParentBody,
                                ShipName = currentShip,
                                DistanceGm = distGm,
                                DistanceKm = distKm
                            });
                        }
                    }

                    // Ankunft an Station/LandingZone/Hangar
                    items.Add(new FlightTimelineItem
                    {
                        Timestamp = entry.Time,
                        RelativeTimeText = relText,
                        Type = resolved.Type == StarmapObjectType.LandingZone || resolved.Type == StarmapObjectType.SpaceStation 
                            ? TimelineItemType.Arrival 
                            : TimelineItemType.Generic,
                        Title = entry.Kind == EventKind.Hangar ? $"Hangar: {entry.Detail}" : $"Ankunft bei {resolved.DisplayName}",
                        Subtitle = resolved.FullBreadcrumb,
                        Detail = resolved.ArmisticeStatusText,
                        LocationName = resolved.DisplayName,
                        SystemName = resolved.SystemName,
                        ParentBody = resolved.ParentBody,
                        ShipName = currentShip
                    });

                    lastResolvedLoc = resolved;
                }
            }
            // 3. Respawn / Pilot Tod
            else if (entry.Kind == EventKind.Death)
            {
                items.Add(new FlightTimelineItem
                {
                    Timestamp = entry.Time,
                    RelativeTimeText = relText,
                    Type = TimelineItemType.CombatDeath,
                    Title = "Pilot eliminiert / Respawn",
                    Subtitle = entry.Detail,
                    LocationName = lastResolvedLoc?.DisplayName ?? "MedBed",
                    SystemName = lastResolvedLoc?.SystemName ?? "Stanton",
                    ShipName = currentShip
                });
            }
        }

        // Falls keine Flugdauer ermittelt werden konnte, nähern wir anhand der Sessiondauer an (nur wenn tatsächlich Flüge stattfanden)
        if (summary.TotalFlightDuration == TimeSpan.Zero && summary.TotalSessionDuration > TimeSpan.Zero && summary.SortieCount > 0)
        {
            summary.TotalFlightDuration = TimeSpan.FromMinutes(summary.TotalSessionDuration.TotalMinutes * 0.5);
        }

        summary.VisitedBodies = visitedBodiesSet.ToList();
        summary.UsedShips = usedShipsSet.ToList();

        return (items, summary);
    }

    /// <summary>
    /// Exportiert die Timeline als übersichtlichen Markdown-Flugbericht.
    /// </summary>
    public static string ExportToMarkdown(string sessionLabel, List<FlightTimelineItem> items, FlightSummary summary)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"# 🚀 Star Citizen Flugbericht — {sessionLabel}");
        sb.AppendLine($"*Erstellt von SCLogMate am {DateTime.Now:dd.MM.yyyy HH:mm}*\n");

        sb.AppendLine("## 📊 Missions-Zusammenfassung (Black Box KPIs)");
        sb.AppendLine($"- **Gesamte Flugdistanz**: {summary.TotalDistanceText}");
        sb.AppendLine($"- **Reine Flugzeit**: {summary.TotalFlightDuration.Hours}h {summary.TotalFlightDuration.Minutes}m");
        sb.AppendLine($"- **Session-Dauer**: {summary.TotalSessionDuration.Hours}h {summary.TotalSessionDuration.Minutes}m");
        sb.AppendLine($"- **Schiffseinsätze**: {summary.SortieCount} Starts ({summary.ShipsUsedText})");
        sb.AppendLine($"- **Schiffsverluste**: {summary.ShipLossesText}");
        sb.AppendLine($"- **Quantum Sprünge**: {summary.QuantumJumps}");
        sb.AppendLine($"- **Besuchte Himmelskörper**: {(summary.VisitedBodies.Any() ? string.Join(", ", summary.VisitedBodies) : "Keine")}");
        sb.AppendLine($"- **Eingesetzte Schiffe**: {(summary.UsedShips.Any() ? string.Join(", ", summary.UsedShips) : "Keine")}\n");

        sb.AppendLine("## ⏱ Chronologischer Flugschreiber");
        sb.AppendLine("| Zeit | Typ | Ereignis | Ort | Delta |");
        sb.AppendLine("| :--- | :--- | :--- | :--- | :--- |");

        foreach (var it in items)
        {
            var delta = !string.IsNullOrEmpty(it.DeltaAuecText) ? it.DeltaAuecText : (!string.IsNullOrEmpty(it.DistanceText) ? it.DistanceText : "—");
            sb.AppendLine($"| `{it.FormattedTime}` | {it.IconGlyph} {it.TypeLabel} | **{it.Title}**<br>*{it.Subtitle}* | {it.LocationName} | {delta} |");
        }

        return sb.ToString();
    }
}
