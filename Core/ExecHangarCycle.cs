using System;
using System.Collections.Generic;

namespace SCLogMate.Core;

/// <summary>
/// Computed state of the Executive Hangar Contested Zone (Pyro PYAM-EXHANG-0-1) at a given instant.
/// </summary>
public sealed record ExecHangarSnapshot(
    bool IsOpen,
    int GreensLit,
    bool IsFinalActiveTail,
    TimeSpan TimeToTransition,
    DateTime NextOpenUtc,
    DateTime NextCloseUtc,
    IReadOnlyList<DateTime> UpcomingOpensUtc,
    string CalibrationLabel,
    bool IsCustomAnchor
);

/// <summary>
/// Deterministic offline Executive Hangar cycle calculation for Star Citizen's Pyro system.
/// The PYAM contested-zone hangar rotation is globally synchronized across all game shards:
/// 65m 0.093s open, followed by 120m 0.173s closed.
/// Durations are community-calibrated and patch-stable; the anchor is calibrated per game patch.
/// </summary>
public static class ExecHangarCycle
{
    // 4.9.0-LIVE calibration anchor (server 12269732). The instant an open phase began.
    private static readonly DateTime AnchorOpenUtc = new(2026, 7, 22, 23, 37, 42, 439, DateTimeKind.Utc);
    private const long OpenMs = 3_900_093;   // 65m 0.093s (final 5 min active tail)
    private const long ClosedMs = 7_200_173; // 120m 0.173s
    private const long CycleMs = OpenMs + ClosedMs; // 185m 0.266s

    public const string CalibrationLabel = "4.9.0-LIVE";

    public static ExecHangarSnapshot At(DateTime utcNow, DateTime? anchorOverrideUtc = null)
    {
        var isCustom = anchorOverrideUtc.HasValue;
        var anchor = anchorOverrideUtc is { } o
            ? (o.Kind == DateTimeKind.Local ? o.ToUniversalTime() : DateTime.SpecifyKind(o, DateTimeKind.Utc))
            : AnchorOpenUtc;

        var deltaMs = (utcNow.Ticks - anchor.Ticks) / TimeSpan.TicksPerMillisecond;
        var pos = ((deltaMs % CycleMs) + CycleMs) % CycleMs;

        var isOpen = pos < OpenMs;
        int greens;
        bool tail = false;

        if (isOpen)
        {
            var openMin = pos / 60_000;
            greens = (int)Math.Max(0, 5 - openMin / 12);
            tail = openMin >= 60; // Last 5 minutes still active inside hangar
        }
        else
        {
            var closedMin = (pos - OpenMs) / 60_000;
            greens = (int)Math.Min(4, closedMin / 24);
        }

        var msToTransition = isOpen ? OpenMs - pos : CycleMs - pos;
        var msToNextOpen = CycleMs - pos;
        var nextOpen = utcNow.AddMilliseconds(msToNextOpen);
        var nextClose = isOpen
            ? utcNow.AddMilliseconds(msToTransition)
            : nextOpen.AddMilliseconds(OpenMs);

        var upcoming = new List<DateTime>(3);
        for (var i = 0; i < 3; i++)
        {
            upcoming.Add(nextOpen.AddMilliseconds(CycleMs * (long)i));
        }

        return new ExecHangarSnapshot(
            IsOpen: isOpen,
            GreensLit: greens,
            IsFinalActiveTail: tail,
            TimeToTransition: TimeSpan.FromMilliseconds(msToTransition),
            NextOpenUtc: nextOpen,
            NextCloseUtc: nextClose,
            UpcomingOpensUtc: upcoming,
            CalibrationLabel: CalibrationLabel,
            IsCustomAnchor: isCustom
        );
    }

    public static string FormatCountdown(TimeSpan t)
    {
        if (t < TimeSpan.Zero) t = TimeSpan.Zero;
        return t.TotalHours >= 1
            ? $"{(int)t.TotalHours}h {t.Minutes:00}m {t.Seconds:00}s"
            : $"{t.Minutes}m {t.Seconds:00}s";
    }
}
