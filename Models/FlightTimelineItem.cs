using System;

namespace SCLogMate.Models;

public enum TimelineItemType
{
    Spawn,
    Departure,
    QuantumTravel,
    CombatKill,
    CombatDeath,
    Trade,
    Mining,
    Refinery,
    Mission,
    Arrival,
    Generic
}

public sealed class FlightTimelineItem
{
    public DateTime Timestamp { get; set; }
    public string FormattedTime => Timestamp.ToLocalTime().Date == DateTime.Today
        ? Timestamp.ToLocalTime().ToString("HH:mm:ss")
        : Timestamp.ToLocalTime().ToString("dd.MM. HH:mm");
    public string RelativeTimeText { get; set; } = "+00:00";
    public TimelineItemType Type { get; set; } = TimelineItemType.Generic
    ;
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string Detail { get; set; } = "";
    public string LocationName { get; set; } = "";
    public string SystemName { get; set; } = "Stanton";
    public string ParentBody { get; set; } = "";
    public string ShipName { get; set; } = "";

    // Metriken
    public double DistanceGm { get; set; }
    public double DistanceKm { get; set; }
    public long DeltaAuec { get; set; }

    public string DistanceText => DistanceGm > 0.05
        ? $"{DistanceGm:F1} GM ({DistanceKm:N0} km)"
        : (DistanceKm > 0 ? $"{DistanceKm:N0} km" : "");

    public string DeltaAuecText => DeltaAuec != 0
        ? (DeltaAuec > 0 ? $"+{DeltaAuec:N0} aUEC" : $"{DeltaAuec:N0} aUEC")
        : "";

    public string DeltaAuecColor => DeltaAuec > 0 ? "#3FB950" : (DeltaAuec < 0 ? "#F85149" : "#8B949E");

    // UI Badges & Icons
    public string IconGlyph => Type switch
    {
        TimelineItemType.Spawn => "🚀",
        TimelineItemType.Departure => "🛫",
        TimelineItemType.QuantumTravel => "🌀",
        TimelineItemType.CombatKill => "⚔️",
        TimelineItemType.CombatDeath => "☠️",
        TimelineItemType.Trade => "💰",
        TimelineItemType.Mining => "⛏️",
        TimelineItemType.Refinery => "🏭",
        TimelineItemType.Mission => "📋",
        TimelineItemType.Arrival => "🛬",
        _ => "📍"
    };

    public string BadgeBackground => Type switch
    {
        TimelineItemType.Spawn => "#1E293B",
        TimelineItemType.Departure => "#1E3A8A",
        TimelineItemType.QuantumTravel => "#312E81",
        TimelineItemType.CombatKill => "#064E3B",
        TimelineItemType.CombatDeath => "#7F1D1D",
        TimelineItemType.Trade => "#78350F",
        TimelineItemType.Mining => "#713F12",
        TimelineItemType.Refinery => "#581C87",
        TimelineItemType.Mission => "#134E4A",
        TimelineItemType.Arrival => "#14532D",
        _ => "#1F2937"
    };

    public string BadgeBorder => Type switch
    {
        TimelineItemType.Spawn => "#38BDF8",
        TimelineItemType.Departure => "#60A5FA",
        TimelineItemType.QuantumTravel => "#818CF8",
        TimelineItemType.CombatKill => "#34D399",
        TimelineItemType.CombatDeath => "#F87171",
        TimelineItemType.Trade => "#FBBF24",
        TimelineItemType.Mining => "#F59E0B",
        TimelineItemType.Refinery => "#C084FC",
        TimelineItemType.Mission => "#2DD4BF",
        TimelineItemType.Arrival => "#4ADE80",
        _ => "#4B5563"
    };

    public string TypeLabel => Type switch
    {
        TimelineItemType.Spawn => "SCHIFF-SPAWN",
        TimelineItemType.Departure => "ABFLUG",
        TimelineItemType.QuantumTravel => "QUANTUM-SPRUNG",
        TimelineItemType.CombatKill => "ZIEL VERNICHTET",
        TimelineItemType.CombatDeath => "SCHIFF ZERSTÖRT",
        TimelineItemType.Trade => "HANDELSTRANSAKTION",
        TimelineItemType.Mining => "MINING & EXTRAKTION",
        TimelineItemType.Refinery => "RAFFINERIE-START",
        TimelineItemType.Mission => "AUFTRAGSEREIGNIS",
        TimelineItemType.Arrival => "LANDUNG & ANKUNFT",
        _ => "EREIGNIS"
    };
}
