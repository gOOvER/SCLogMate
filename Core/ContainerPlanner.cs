using System;
using System.Collections.Generic;
using System.Linq;

namespace SCLogMate.Core;

/// <summary>
/// A single container size selection: count of crates of a given SCU capacity,
/// including their individual and aggregated auto-load fee and dispatch duration.
/// </summary>
public readonly record struct ContainerPick(
    int Scu,
    int Count,
    long UnitFee,
    long TotalFee,
    double UnitTimeSeconds,
    double TotalTimeSeconds
);

/// <summary>
/// Recommended container purchase plan to reach target SCU capacity.
/// Contains container pick counts, shortfall against requested target,
/// total crate counts, fee calculations, and auto-load duration estimates.
/// </summary>
public sealed record ContainerPlan(
    int TargetScu,
    int TotalScu,
    int ShortfallScu,
    IReadOnlyList<ContainerPick> Picks,
    int MinContainerScu,
    int MaxContainerScu,
    int TotalBoxCount,
    long TotalAutoLoadFee,
    int TotalEstimatedSeconds
)
{
    public bool HitsTarget => ShortfallScu == 0;
    public bool IsEmpty => Picks.Count == 0;
    public bool SingleSize => MinContainerScu == MaxContainerScu;
}

/// <summary>
/// Chooses optimal container crate sizes to purchase at cargo kiosks.
/// Uses bounded dynamic programming to maximize filled SCU capacity (&lt;= target),
/// minimize crate count (lower fees, faster handling), and avoid crates exceeding the ship's grid limits.
/// </summary>
public static class ContainerPlanner
{
    // Auto-load base dispatch time in seconds
    public const int BaseDispatchSeconds = 72;

    // Star Citizen auto-load fee ladder in aUEC per crate size
    public static long FeeForScu(int scu) => scu switch
    {
        1 => 30,
        2 => 55,
        4 => 100,
        8 => 190,
        16 => 360,
        24 => 495,
        32 => 680,
        _ => scu * 30 // Proportional fallback for exotic/custom crate sizes
    };

    // Empirical freight handling time in seconds per crate size
    public static double TimeForScu(int scu) => scu switch
    {
        1 => 1.2,
        2 => 1.8,
        4 => 3.0,
        8 => 4.8,
        16 => 7.2,
        24 => 9.0,
        32 => 10.8,
        _ => scu * 0.5 // Fallback
    };

    /// <summary>
    /// Computes the optimal crate purchase plan for a kiosk offering <paramref name="containerSizes"/>
    /// to a ship whose largest grid accommodates up to <paramref name="shipMaxContainerScu"/> SCU crates.
    /// </summary>
    public static ContainerPlan? Plan(string? containerSizes, int shipMaxContainerScu, int targetScu)
    {
        if (targetScu <= 0 || shipMaxContainerScu <= 0) return null;

        // Default to standard SC crate ladder if none specified
        var menu = string.IsNullOrWhiteSpace(containerSizes) ? "1,2,4,8,16,24,32" : containerSizes;
        var sizes = UsableSizes(menu, shipMaxContainerScu);
        if (sizes.Count == 0) return null;

        // boxes[i] = fewest crates that total exactly i SCU; -1 = unreachable.
        // pick[i] = a crate size used in that best solution, for reconstruction.
        var boxes = new int[targetScu + 1];
        var pick = new int[targetScu + 1];
        for (int i = 1; i <= targetScu; i++) boxes[i] = -1;

        for (int i = 1; i <= targetScu; i++)
        {
            foreach (var s in sizes)
            {
                if (s > i) continue;
                var prev = boxes[i - s];
                if (prev < 0) continue;
                if (boxes[i] < 0 || prev + 1 < boxes[i])
                {
                    boxes[i] = prev + 1;
                    pick[i] = s;
                }
            }
        }

        // Fullest reachable load at or under the target
        int best = targetScu;
        while (best > 0 && boxes[best] < 0) best--;

        var counts = new SortedDictionary<int, int>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
        for (int at = best; at > 0; at -= pick[at])
        {
            counts[pick[at]] = counts.TryGetValue(pick[at], out var n) ? n + 1 : 1;
        }

        var picks = new List<ContainerPick>(counts.Count);
        int totalBoxes = 0;
        long totalFee = 0;
        double sumBoxSeconds = 0;

        foreach (var kv in counts)
        {
            var scu = kv.Key;
            var count = kv.Value;
            var unitFee = FeeForScu(scu);
            var lotFee = unitFee * count;
            var unitTime = TimeForScu(scu);
            var lotTime = unitTime * count;

            picks.Add(new ContainerPick(scu, count, unitFee, lotFee, unitTime, lotTime));
            totalBoxes += count;
            totalFee += lotFee;
            sumBoxSeconds += lotTime;
        }

        int totalSeconds = totalBoxes > 0 ? BaseDispatchSeconds + (int)Math.Ceiling(sumBoxSeconds) : 0;
        int minSize = sizes[^1];
        int maxSize = sizes[0];

        return new ContainerPlan(
            TargetScu: targetScu,
            TotalScu: best,
            ShortfallScu: targetScu - best,
            Picks: picks,
            MinContainerScu: minSize,
            MaxContainerScu: maxSize,
            TotalBoxCount: totalBoxes,
            TotalAutoLoadFee: totalFee,
            TotalEstimatedSeconds: totalSeconds
        );
    }

    /// <summary>
    /// Returns the maximum achievable buyable SCU quantity at or under <paramref name="targetScu"/>.
    /// </summary>
    public static int BuyableScu(string? containerSizes, int shipMaxContainerScu, int targetScu) =>
        Plan(containerSizes, shipMaxContainerScu, targetScu)?.TotalScu ?? targetScu;

    /// <summary>
    /// Parses and filters kiosk crate sizes against the ship's grid capacity.
    /// </summary>
    public static List<int> UsableSizes(string containerSizes, int shipMaxContainerScu)
    {
        var sizes = new List<int>();
        if (string.IsNullOrWhiteSpace(containerSizes)) return sizes;

        foreach (var token in containerSizes.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(token, out var size) && size > 0 && size <= shipMaxContainerScu && !sizes.Contains(size))
            {
                sizes.Add(size);
            }
        }

        sizes.Sort((a, b) => b.CompareTo(a)); // Largest first for deterministic reconstruction
        return sizes;
    }
}
