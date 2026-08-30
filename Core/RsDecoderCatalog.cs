using System;
using System.Collections.Generic;
using System.Linq;
using SCLogReader.Models;

namespace SCLogReader.Core;

public static class RsDecoderCatalog
{
    private static readonly List<RsResource> _resources = new();
    public static IReadOnlyList<RsResource> AllResources => _resources;

    static RsDecoderCatalog()
    {
        InitCatalog();
    }

    private static void InitCatalog()
    {
        _resources.Clear();

        // 1. Tier S — Legendär / Selten
        _resources.Add(new RsResource
        {
            Name = "Quantanium",
            BaseRs = 3170,
            Tier = "S",
            Rarity = "legendary",
            Method = "ship",
            EstimatedPricePerScu = 88000,
            Refineries = new()
            {
                new("Levski", "Nyx", 5),
                new("ARC-L1 Wide Forest Station", "Stanton", 3),
                new("ARC-L2 Lively Pathway Station", "Stanton", 3),
                new("HUR-L1 Green Glade Station", "Stanton", 2),
                new("MIC-L2 Long Forest Station", "Stanton", 1)
            }
        });

        _resources.Add(new RsResource
        {
            Name = "Bexalite",
            BaseRs = 3600,
            Tier = "S",
            Rarity = "rare",
            Method = "ship",
            EstimatedPricePerScu = 44000,
            Refineries = new()
            {
                new("MIC-L5 Modern Icarus Station", "Stanton", 12),
                new("MIC-L2 Long Forest Station", "Stanton", 9),
                new("Levski", "Nyx", 8),
                new("ARC-L2 Lively Pathway Station", "Stanton", 2)
            }
        });

        // 2. Tier A — Hoher Wert / Selten
        _resources.Add(new RsResource
        {
            Name = "Laranite",
            BaseRs = 3825,
            Tier = "A",
            Rarity = "uncommon",
            Method = "ship",
            EstimatedPricePerScu = 31000,
            Refineries = new()
            {
                new("Levski", "Nyx", 7),
                new("HUR-L1 Green Glade Station", "Stanton", 2),
                new("MIC-L1 Shallow Frontier Station", "Stanton", 2),
                new("Terra Gateway (Stanton)", "Stanton", 2)
            }
        });

        _resources.Add(new RsResource
        {
            Name = "Gold",
            BaseRs = 3585,
            Tier = "A",
            Rarity = "rare",
            Method = "ship",
            EstimatedPricePerScu = 24000,
            Refineries = new()
            {
                new("MIC-L2 Long Forest Station", "Stanton", 9),
                new("ARC-L2 Lively Pathway Station", "Stanton", 7),
                new("Levski", "Nyx", 5),
                new("MIC-L1 Shallow Frontier Station", "Stanton", 1)
            }
        });

        _resources.Add(new RsResource
        {
            Name = "Taranite",
            BaseRs = 3555,
            Tier = "A",
            Rarity = "rare",
            Method = "ship",
            EstimatedPricePerScu = 34000,
            Refineries = new()
            {
                new("Levski", "Nyx", 8),
                new("ARC-L4 Faint Glen Station", "Stanton", 5)
            }
        });

        _resources.Add(new RsResource
        {
            Name = "Beryl",
            BaseRs = 3540,
            Tier = "A",
            Rarity = "rare",
            Method = "ship",
            EstimatedPricePerScu = 16000,
            Refineries = new()
            {
                new("Levski", "Nyx", 8),
                new("ARC-L1 Wide Forest Station", "Stanton", 7),
                new("CRU-L1 Ambitious Dream Station", "Stanton", 7),
                new("MIC-L5 Modern Icarus Station", "Stanton", 7)
            }
        });

        _resources.Add(new RsResource
        {
            Name = "Agricium",
            BaseRs = 3885,
            Tier = "A",
            Rarity = "uncommon",
            Method = "ship",
            EstimatedPricePerScu = 27500,
            Refineries = new()
            {
                new("MIC-L1 Shallow Frontier Station", "Stanton", 8),
                new("Levski", "Nyx", 8),
                new("Terra Gateway (Stanton)", "Stanton", 8)
            }
        });

        _resources.Add(new RsResource
        {
            Name = "Ouratite",
            BaseRs = 3370,
            Tier = "A",
            Rarity = "epic",
            Method = "ship",
            EstimatedPricePerScu = 29000,
            Refineries = new()
        });

        _resources.Add(new RsResource
        {
            Name = "Savrilium",
            BaseRs = 3200,
            Tier = "A",
            Rarity = "legendary",
            Method = "ship",
            EstimatedPricePerScu = 38000,
            Refineries = new()
            {
                new("Levski", "Nyx", 1)
            }
        });

        _resources.Add(new RsResource
        {
            Name = "Stileron",
            BaseRs = 3185,
            Tier = "A",
            Rarity = "legendary",
            Method = "ship",
            EstimatedPricePerScu = 36000,
            Refineries = new()
        });

        // 3. Tier B — Mittlerer Ertrag
        _resources.Add(new RsResource
        {
            Name = "Borase",
            BaseRs = 3570,
            Tier = "B",
            Rarity = "rare",
            Method = "ship",
            EstimatedPricePerScu = 19500,
            Refineries = new()
            {
                new("MIC-L5 Modern Icarus Station", "Stanton", 9),
                new("Levski", "Nyx", 8),
                new("ARC-L2 Lively Pathway Station", "Stanton", 2)
            }
        });

        _resources.Add(new RsResource
        {
            Name = "Titanium",
            BaseRs = 3855,
            Tier = "B",
            Rarity = "uncommon",
            Method = "ship",
            EstimatedPricePerScu = 11000,
            Refineries = new()
            {
                new("MIC-L5 Modern Icarus Station", "Stanton", 13),
                new("Levski", "Nyx", 8),
                new("MIC-L2 Long Forest Station", "Stanton", 6),
                new("ARC-L1 Wide Forest Station", "Stanton", 5)
            }
        });

        _resources.Add(new RsResource
        {
            Name = "Tungsten",
            BaseRs = 3870,
            Tier = "B",
            Rarity = "uncommon",
            Method = "ship",
            EstimatedPricePerScu = 12500,
            Refineries = new()
            {
                new("MIC-L2 Long Forest Station", "Stanton", 9),
                new("Levski", "Nyx", 8),
                new("HUR-L1 Green Glade Station", "Stanton", 4)
            }
        });

        _resources.Add(new RsResource
        {
            Name = "Aslarite",
            BaseRs = 3840,
            Tier = "B",
            Rarity = "uncommon",
            Method = "ship",
            EstimatedPricePerScu = 14000,
            Refineries = new()
        });

        _resources.Add(new RsResource
        {
            Name = "Torite",
            BaseRs = 3900,
            Tier = "B",
            Rarity = "uncommon",
            Method = "ship",
            EstimatedPricePerScu = 13000,
            Refineries = new()
            {
                new("Levski", "Nyx", 1)
            }
        });

        _resources.Add(new RsResource
        {
            Name = "Lindinium",
            BaseRs = 3400,
            Tier = "B",
            Rarity = "epic",
            Method = "ship",
            EstimatedPricePerScu = 22000,
            Refineries = new()
            {
                new("Levski", "Nyx", 4)
            }
        });

        _resources.Add(new RsResource
        {
            Name = "Riccite",
            BaseRs = 3385,
            Tier = "B",
            Rarity = "epic",
            Method = "ship",
            EstimatedPricePerScu = 20000,
            Refineries = new()
        });

        _resources.Add(new RsResource
        {
            Name = "Hephestanite",
            BaseRs = 4180,
            Tier = "B",
            Rarity = "common",
            Method = "ship",
            EstimatedPricePerScu = 9500,
            Refineries = new()
            {
                new("MIC-L5 Modern Icarus Station", "Stanton", 8),
                new("Levski", "Nyx", 8)
            }
        });

        // 4. Tier C — Standard Erze & Metalle
        _resources.Add(new RsResource { Name = "Tin", BaseRs = 4195, Tier = "C", Rarity = "common", Method = "ship", EstimatedPricePerScu = 7500, Refineries = new() { new("Levski", "Nyx", 9) } });
        _resources.Add(new RsResource { Name = "Quartz", BaseRs = 4210, Tier = "C", Rarity = "common", Method = "ship", EstimatedPricePerScu = 5800, Refineries = new() { new("ARC-L1 Wide Forest Station", "Stanton", 11), new("Levski", "Nyx", 8) } });
        _resources.Add(new RsResource { Name = "Corundum", BaseRs = 4225, Tier = "C", Rarity = "common", Method = "ship", EstimatedPricePerScu = 6400, Refineries = new() { new("Levski", "Nyx", 8), new("CRU-L1 Ambitious Dream Station", "Stanton", 7) } });
        _resources.Add(new RsResource { Name = "Copper", BaseRs = 4240, Tier = "C", Rarity = "common", Method = "ship", EstimatedPricePerScu = 7200, Refineries = new() { new("MIC-L5 Modern Icarus Station", "Stanton", 9), new("Levski", "Nyx", 8) } });
        _resources.Add(new RsResource { Name = "Silicon", BaseRs = 4255, Tier = "C", Rarity = "common", Method = "ship", EstimatedPricePerScu = 5200 });
        _resources.Add(new RsResource { Name = "Iron", BaseRs = 4270, Tier = "C", Rarity = "common", Method = "ship", EstimatedPricePerScu = 4800, Refineries = new() { new("MIC-L5 Modern Icarus Station", "Stanton", 8), new("Levski", "Nyx", 8) } });
        _resources.Add(new RsResource { Name = "Aluminium", BaseRs = 4285, Tier = "C", Rarity = "common", Method = "ship", EstimatedPricePerScu = 4200 });
        _resources.Add(new RsResource { Name = "Ice", BaseRs = 4300, Tier = "C", Rarity = "common", Method = "ship", EstimatedPricePerScu = 2800 });

        // 5. Salvage Panels (Wrackteile) — 2.000 RS / Panel
        _resources.Add(new RsResource
        {
            Name = "Salvage (Panels)",
            BaseRs = 2000,
            Tier = "A",
            Rarity = "uncommon",
            Method = "salvage",
            EstimatedPricePerScu = 14500, // RMC / Construction Material
            Refineries = new()
        });
    }

    /// <summary>
    /// Decodiert ein RS-Signal (z. B. 7200, 14400, 3170, 6000) in passende Erze/Salvage-Treffer.
    /// </summary>
    public static List<RsMatch> Decode(int rs)
    {
        var matches = new List<RsMatch>();
        if (rs < 1000 || rs > 300000) return matches;

        foreach (var res in _resources)
        {
            var (isMatch, nodes, isExact, errorPct) = res.CheckRs(rs);
            if (isMatch)
            {
                matches.Add(new RsMatch
                {
                    Resource = res,
                    Nodes = nodes,
                    IsExact = isExact,
                    ErrorPct = errorPct,
                    ScannedRs = rs
                });
            }
        }

        // Sortierung: Exakte Matches zuerst, dann nach Tier (S vor A vor B), dann geringster Fehler
        return matches
            .OrderByDescending(m => m.IsExact)
            .ThenBy(m => m.Resource.Tier switch { "S" => 0, "A" => 1, "B" => 2, _ => 3 })
            .ThenBy(m => m.ErrorPct)
            .ToList();
    }
}
