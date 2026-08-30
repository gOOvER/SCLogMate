using System;
using System.Collections.Generic;

namespace SCLogReader.Models;

public record RefineryBonus(string Station, string System, int ModifierPct);

public class RsResource
{
    public string Name { get; set; } = "";
    public int BaseRs { get; set; }
    public string Tier { get; set; } = "C";      // S, A, B, C
    public string Rarity { get; set; } = "common"; // common, uncommon, rare, epic, legendary
    public string Method { get; set; } = "ship";   // ship, salvage, fps, vehicle
    public double EstimatedPricePerScu { get; set; }
    public List<RefineryBonus> Refineries { get; set; } = new();

    public string TierColor => Tier switch
    {
        "S" => "#EC4899", // Magenta / Ultra rare
        "A" => "#F59E0B", // Gold / High value
        "B" => "#38BDF8", // Cyan / Medium value
        _ => "#94A3B8"    // Slate / Standard
    };

    public (bool Matches, int Nodes, bool IsExact, double ErrorPct) CheckRs(int rs)
    {
        if (BaseRs <= 0 || (Method != "ship" && Method != "salvage")) return (false, 0, false, 0);

        double ratio = (double)rs / BaseRs;
        int nearest = (int)Math.Round(ratio);
        if (nearest < 1) return (false, 0, false, 0);

        // Cluster Limits: Verhindert unmögliche Node-Multiplikatoren
        int maxCap = Rarity switch
        {
            "legendary" => 4,
            "epic" => 6,
            "rare" => 8,
            "uncommon" => 12,
            _ => 16
        };

        if (nearest > maxCap) return (false, 0, false, 0);

        if (rs % BaseRs == 0)
            return (true, nearest, true, 0.0);

        double errorPct = Math.Abs(ratio - nearest) / nearest * 100;
        if (errorPct <= 0.6)
            return (true, nearest, false, errorPct);

        return (false, 0, false, 0);
    }
}

public class RsMatch
{
    public RsResource Resource { get; set; } = new();
    public int Nodes { get; set; }
    public bool IsExact { get; set; }
    public double ErrorPct { get; set; }
    public int ScannedRs { get; set; }

    public string DisplayTitle => Resource.Method == "salvage"
        ? $"{Nodes}x Salvage Panel(s)"
        : $"{Nodes}x {Resource.Name}";

    public string Subtitle => Resource.Method == "salvage"
        ? $"Rumpf-Wrackteile (2.000 RS / Panel) · {(IsExact ? "Exakte Signatur" : $"Signal ±{ErrorPct:F1}%")}"
        : $"Tier {Resource.Tier} ({Resource.Rarity}) · Base-RS {Resource.BaseRs:N0} · {(IsExact ? "Exakte Signatur" : $"Signal ±{ErrorPct:F1}%")}";

    public string BestRefineryText
    {
        get
        {
            if (Resource.Refineries.Count == 0) return "Keine Veredelung";
            var best = Resource.Refineries[0];
            return $"{best.Station} (+{best.ModifierPct}%)";
        }
    }

    public double EstimatedYieldAuec => Resource.EstimatedPricePerScu > 0 
        ? Resource.EstimatedPricePerScu * Nodes * 32.0 
        : 0;

    public string EstimatedValueText => EstimatedYieldAuec > 0
        ? $"ca. {EstimatedYieldAuec:N0} aUEC"
        : "—";

    public string BadgeColor => Resource.TierColor;
}
