using System;
using System.Collections.Generic;

namespace SCLogMate.Models;

public record RefineryBonus(string Station, string System, int ModifierPct);

public class RsResource
{
    public string Name { get; set; } = "";
    public int BaseRs { get; set; }
    public string Tier { get; set; } = "C";      // S, A, B, C
    public string Rarity { get; set; } = "common"; // common, uncommon, rare, epic, legendary
    public string Method { get; set; } = "ship";   // ship, salvage, fps, vehicle, fps+vehicle
    public double EstimatedPricePerScu { get; set; }
    public List<string> Locations { get; set; } = new();
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
        if (BaseRs <= 0) return (false, 0, false, 0);

        double ratio = (double)rs / BaseRs;
        int nearest = (int)Math.Round(ratio);
        if (nearest < 1) return (false, 0, false, 0);

        // Cluster Limits: Bis zu 16 Asteroiden in großen Clustern und Missionen
        int maxCap = Method switch
        {
            "salvage" => 100, // Wrackfeld-Panels
            "fps" or "fps+vehicle" => 6,
            "vehicle" => 8,
            _ => 16 // Alle Ship-Mining Erze (auch Legendary/Epic/Rare in großen Clustern bis 16 Nodes)
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

    public string DisplayTitle => Resource.Method switch
    {
        "salvage" => Nodes == 1 ? "1x Salvage Panel" : $"{Nodes}x Salvage Panels",
        "fps" or "fps+vehicle" => $"{Nodes}x {Resource.Name} (Hand-Gem)",
        "vehicle" => $"{Nodes}x {Resource.Name} (Fahrzeug-Gem)",
        _ => $"{Nodes}x {Resource.Name}"
    };

    public string Subtitle => Resource.Method switch
    {
        "salvage" => $"Rumpf-Wrackteile (2.000 RS / Panel) · {(IsExact ? "Exakte Signatur" : $"Signal ±{ErrorPct:F1}%")}",
        "fps" or "fps+vehicle" => $"Edelstein / FPS-Mining · Base-RS {Resource.BaseRs:N0} · {(IsExact ? "Exakte Signatur" : $"Signal ±{ErrorPct:F1}%")}",
        "vehicle" => $"Edelstein / ROC-Mining · Base-RS {Resource.BaseRs:N0} · {(IsExact ? "Exakte Signatur" : $"Signal ±{ErrorPct:F1}%")}",
        _ => $"Tier {Resource.Tier} ({Resource.Rarity}) · Base-RS {Resource.BaseRs:N0} · {(IsExact ? "Exakte Signatur" : $"Signal ±{ErrorPct:F1}%")}"
    };

    public string BestRefineryText
    {
        get
        {
            if (Resource.Refineries.Count == 0) return "Keine Veredelung";
            var best = Resource.Refineries[0];
            return $"{best.Station} (+{best.ModifierPct}%)";
        }
    }

    public bool HasRefinery => Resource.Refineries.Count > 0;

    public string BestRefineryCompact => Resource.Refineries.Count > 0
        ? $"{Resource.Refineries[0].Station.Split(' ')[0]} (+{Resource.Refineries[0].ModifierPct}%)"
        : "";

    public double EstimatedYieldAuec => Resource.EstimatedPricePerScu > 0 
        ? (Resource.Method == "salvage" ? Nodes * 25000.0 : Resource.EstimatedPricePerScu)
        : 0;

    public string EstimatedValueText
    {
        get
        {
            if (Resource.EstimatedPricePerScu <= 0) return "—";

            if (Resource.Method == "salvage")
            {
                long panelVal = Nodes * 25000L;
                return $"ca. {panelVal:N0} aUEC";
            }

            return $"{Resource.EstimatedPricePerScu:N0} aUEC / SCU";
        }
    }

    public string CompactValueText => Resource.Method == "salvage"
        ? $"ca. {Nodes * 25000:N0} aUEC"
        : $"{Resource.EstimatedPricePerScu:N0} aUEC/SCU";

    public string BadgeColor => Resource.TierColor;
    public bool IsTargetMatch { get; set; }
    public string TargetAlertBorder => IsTargetMatch ? "#F59E0B" : "#0284C7";
    public string TargetAlertTitleColor => IsTargetMatch ? "#FBBF24" : "#38BDF8";
}
