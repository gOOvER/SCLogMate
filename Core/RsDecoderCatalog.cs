using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using SCLogMate.Models;

namespace SCLogMate.Core;

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

        try
        {
            byte[]? bytes = null;

            // 1. Embedded Resource aus dem Assembly laden
            var asm = typeof(RsDecoderCatalog).Assembly;
            using (var stream = asm.GetManifestResourceStream("SCLogMate.Data.seed_data.json"))
            {
                if (stream != null)
                {
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    bytes = ms.ToArray();
                }
            }

            // 2. Fallback: Direkt aus Datei lesen falls nicht embedded
            if (bytes == null || bytes.Length == 0)
            {
                var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "seed_data.json");
                if (File.Exists(localPath))
                {
                    bytes = File.ReadAllBytes(localPath);
                }
                else if (File.Exists("Data/seed_data.json"))
                {
                    bytes = File.ReadAllBytes("Data/seed_data.json");
                }
            }

            if (bytes != null && bytes.Length > 0)
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var seed = JsonSerializer.Deserialize<SeedDataJson>(bytes, opts);

                if (seed?.Resources != null)
                {
                    foreach (var r in seed.Resources)
                    {
                        if (r.BaseRs <= 0 && r.Method != "salvage") continue;

                        var res = new RsResource
                        {
                            Name = r.Name,
                            BaseRs = r.BaseRs,
                            Tier = string.IsNullOrWhiteSpace(r.Tier) ? "C" : r.Tier,
                            Rarity = string.IsNullOrWhiteSpace(r.Rarity) ? "common" : r.Rarity,
                            Method = string.IsNullOrWhiteSpace(r.Method) ? "ship" : r.Method,
                            Locations = r.Locations ?? new(),
                            EstimatedPricePerScu = GetEstimatedPrice(r.Name, r.Tier),
                            Refineries = r.Refineries?
                                .OrderByDescending(rf => rf.ModifierPct)
                                .Select(rf => new RefineryBonus(rf.Station, rf.System, rf.ModifierPct))
                                .ToList() ?? new()
                        };
                        _resources.Add(res);
                    }
                    Logger.Log($"[RsDecoder] {seed.Resources.Count} Ressourcen erfolgreich aus seed_data.json geladen.");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("RsDecoderCatalog.InitCatalog", ex);
        }

        // 3. Fallback: Sicherstellen, dass die wichtigsten Erze und Salvage Panels immer existieren
        EnsureFallbackEntries();
    }

    private static void EnsureFallbackEntries()
    {
        // Salvage Panels (2.000 RS)
        if (!_resources.Any(r => r.BaseRs == 2000))
        {
            _resources.Add(new RsResource
            {
                Name = "Salvage (Panels)",
                BaseRs = 2000,
                Tier = "A",
                Rarity = "uncommon",
                Method = "salvage",
                EstimatedPricePerScu = 14500,
                Refineries = new()
            });
        }

        // Lindinium (3.400 RS)
        if (!_resources.Any(r => r.Name.Equals("Lindinium", StringComparison.OrdinalIgnoreCase)))
        {
            _resources.Add(new RsResource
            {
                Name = "Lindinium",
                BaseRs = 3400,
                Tier = "B",
                Rarity = "epic",
                Method = "ship",
                EstimatedPricePerScu = 22000,
                Locations = new() { "Glaciem Ring", "Keeger Belt" },
                Refineries = new() { new("Levski", "Nyx", 4) }
            });
        }

        // Quantanium (3.170 RS)
        if (!_resources.Any(r => r.Name.Equals("Quantanium", StringComparison.OrdinalIgnoreCase)))
        {
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
                    new("HUR-L1 Green Glade Station", "Stanton", 2)
                }
            });
        }

        // Bexalite (3.600 RS)
        if (!_resources.Any(r => r.Name.Equals("Bexalite", StringComparison.OrdinalIgnoreCase)))
        {
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
                    new("Levski", "Nyx", 8)
                }
            });
        }
    }

    private static double GetEstimatedPrice(string name, string tier) => name switch
    {
        "Quantanium" => 88000,
        "Bexalite" => 44000,
        "Savrilium" or "Savrillium" => 38000,
        "Stileron" => 36000,
        "Taranite" => 34000,
        "Hadanite" => 32500,
        "Laranite" => 31000,
        "Ouratite" => 29000,
        "Agricium" => 27500,
        "Gold" => 24000,
        "Carinite" or "Carinite (Pure)" => 24000,
        "Lindinium" => 22000,
        "Riccite" => 20000,
        "Borase" => 19500,
        "Beryl" => 16000,
        "Salvage" or "Salvage (Panels)" => 14500,
        "Aslarite" => 14000,
        "Torite" => 13000,
        "Tungsten" => 12500,
        "Titanium" => 11000,
        "Hephestanite" => 9500,
        "Tin" => 7500,
        "Copper" => 7200,
        "Corundum" => 6400,
        "Quartz" => 5800,
        "Silicon" => 5200,
        "Iron" => 4800,
        "Aluminium" => 4200,
        "Ice" => 2800,
        "Janalite" => 250000,
        "Aphorite" => 18000,
        "Dolivine" => 15000,
        "Jaclium" => 28000,
        "Saldynium" => 26000,
        "Sadaryx" => 22000,
        "Beradom" => 25000,
        "Feynmaline" => 30000,
        "Glacosite" => 20000,
        _ => tier switch
        {
            "S" => 50000,
            "A" => 25000,
            "B" => 15000,
            _ => 5000
        }
    };

    /// <summary>
    /// Decodiert ein RS-Signal (z. B. 3400, 6800, 7200, 14400, 3170, 2000) in passende Erze/Salvage-Treffer.
    /// </summary>
    public static List<RsMatch> Decode(int rs)
    {
        var matches = new List<RsMatch>();
        if (rs < 1000 || rs > 300000) return matches;

        // 1. Primär: Ship-Mining Erze und Salvage Panels
        foreach (var res in _resources.Where(r => r.Method is "ship" or "salvage"))
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

        // 2. Sekundär: Falls keine Ship-Matches gefunden oder explizit Gem-RS (3000 FPS / 4000 Vehicle)
        if (matches.Count == 0 || rs == 3000 || rs == 4000)
        {
            foreach (var res in _resources.Where(r => r.Method is "fps" or "vehicle" or "fps+vehicle"))
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
        }

        // Sortierung: Exakte Matches zuerst, dann nach Tier (S vor A vor B vor C), dann geringster Fehler
        return matches
            .OrderByDescending(m => m.IsExact)
            .ThenBy(m => m.Resource.Tier switch { "S" => 0, "A" => 1, "B" => 2, _ => 3 })
            .ThenBy(m => m.ErrorPct)
            .ToList();
    }

    private record SeedDataJson(
        [property: JsonPropertyName("miningDataVersion")] string? MiningDataVersion,
        [property: JsonPropertyName("resources")] List<SeedResourceJson>? Resources
    );

    private record SeedResourceJson(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("baseRs")] int BaseRs,
        [property: JsonPropertyName("tier")] string Tier,
        [property: JsonPropertyName("rarity")] string Rarity,
        [property: JsonPropertyName("method")] string Method,
        [property: JsonPropertyName("locations")] List<string>? Locations,
        [property: JsonPropertyName("refineries")] List<SeedRefineryJson>? Refineries
    );

    private record SeedRefineryJson(
        [property: JsonPropertyName("station")] string Station,
        [property: JsonPropertyName("system")] string System,
        [property: JsonPropertyName("modifierPct")] int ModifierPct
    );
}
