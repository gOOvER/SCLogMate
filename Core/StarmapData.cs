using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SCLogReader.Core;

public enum StarmapObjectType
{
    Star,
    Planet,
    Moon,
    LandingZone,
    SpaceStation,
    LagrangeStation,
    Outpost,
    JumpPoint
}

public sealed class StarmapObject
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string System { get; set; } = "";
    public string? ParentId { get; set; }
    public StarmapObjectType Type { get; set; }
    public double OrbitRadius { get; set; }    // Visueller Radius im System-Canvas (z.B. 70 bis 350)
    public double OrbitAngleDeg { get; set; }  // Winkel auf der Umlaufbahn (0 - 360)
    public string ColorHex { get; set; } = "#58A6FF";
    public double Size { get; set; } = 8;
    public bool HasArmistice { get; set; }
    public string Jurisdiction { get; set; } = "UEE";
    public string Description { get; set; } = "";
    public List<StarmapObject> Children { get; set; } = new();

    // Berechnete Canvas-Position bezogen auf Mittelpunkt (0,0)
    public double RelX => OrbitRadius * Math.Cos(OrbitAngleDeg * Math.PI / 180.0);
    public double RelY => OrbitRadius * Math.Sin(OrbitAngleDeg * Math.PI / 180.0);
}

public sealed class ResolvedLocation
{
    public string RawCode { get; set; } = "";
    public string DisplayName { get; set; } = "Unbekannt";
    public string SystemName { get; set; } = "Stanton";
    public string ParentBody { get; set; } = "—";
    public StarmapObjectType Type { get; set; } = StarmapObjectType.Outpost;
    public bool IsArmistice { get; set; }
    public string ArmisticeStatusText => IsArmistice ? "🟢 Schutzzone aktiv" : "🟡 Keine Schutzzone";
    public string FullBreadcrumb => string.IsNullOrEmpty(ParentBody) || ParentBody == "—" 
        ? $"{DisplayName} ({SystemName})" 
        : $"{DisplayName} · {ParentBody} ({SystemName})";
    public string SystemBadgeColor => SystemName.ToUpperInvariant() switch
    {
        "STANTON" => "#38BDF8", // Cyan
        "PYRO" => "#F97316",    // Orange
        "NYX" => "#A855F7",     // Lila
        _ => "#58A6FF"
    };
}

public static class StarmapData
{
    private static readonly Dictionary<string, List<StarmapObject>> Systems = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, StarmapObject> ObjectsById = new(StringComparer.OrdinalIgnoreCase);

    static StarmapData()
    {
        InitStanton();
        InitPyro();
        InitNyx();
    }

    public static IReadOnlyList<string> SystemNames => new[] { "Stanton", "Pyro", "Nyx" };

    public static IReadOnlyList<StarmapObject> GetSystemObjects(string systemName)
    {
        return Systems.TryGetValue(systemName, out var list) ? list : Array.Empty<StarmapObject>();
    }

    public static StarmapObject? FindObject(string nameOrId)
    {
        if (ObjectsById.TryGetValue(nameOrId, out var obj)) return obj;
        return ObjectsById.Values.FirstOrDefault(o => o.Name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase));
    }

    public static ResolvedLocation Resolve(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw == "—")
        {
            return new ResolvedLocation { DisplayName = "—", SystemName = "Stanton", ParentBody = "—" };
        }

        var clean = raw.Trim();

        // 1. Spezifische Codes & Muster
        if (clean.Contains("Keeger", StringComparison.OrdinalIgnoreCase))
        {
            return new ResolvedLocation
            {
                RawCode = clean,
                DisplayName = "Keeger Depot",
                SystemName = "Nyx",
                ParentBody = "Nyx Asteroiden",
                Type = StarmapObjectType.Outpost,
                IsArmistice = true
            };
        }

        if (clean.Contains("Levski", StringComparison.OrdinalIgnoreCase) || clean.StartsWith("Nyx", StringComparison.OrdinalIgnoreCase))
        {
            return new ResolvedLocation
            {
                RawCode = clean,
                DisplayName = "Levski",
                SystemName = "Nyx",
                ParentBody = "Delamar",
                Type = StarmapObjectType.LandingZone,
                IsArmistice = true
            };
        }

        if (clean.Contains("Lorville", StringComparison.OrdinalIgnoreCase) || clean.Contains("Stanton1", StringComparison.OrdinalIgnoreCase) || clean.Contains("Hurston", StringComparison.OrdinalIgnoreCase) || clean.Contains("Everus", StringComparison.OrdinalIgnoreCase))
        {
            string name = clean.Contains("Lorville", StringComparison.OrdinalIgnoreCase) ? "Lorville" :
                          clean.Contains("Everus", StringComparison.OrdinalIgnoreCase) ? "Everus Harbor" :
                          clean.Contains("Oparei", StringComparison.OrdinalIgnoreCase) ? "HDMS-Oparei" :
                          clean.Contains("Cassillo", StringComparison.OrdinalIgnoreCase) ? "Verteilzentrum Cassillo" :
                          clean.Contains("Arial", StringComparison.OrdinalIgnoreCase) ? "Arial" :
                          clean.Contains("Aberdeen", StringComparison.OrdinalIgnoreCase) ? "Aberdeen" :
                          clean.Contains("Magda", StringComparison.OrdinalIgnoreCase) ? "Magda" :
                          clean.Contains("Ita", StringComparison.OrdinalIgnoreCase) ? "Ita" : "Hurston";
            return new ResolvedLocation
            {
                RawCode = clean,
                DisplayName = name,
                SystemName = "Stanton",
                ParentBody = "Hurston",
                Type = name == "Lorville" ? StarmapObjectType.LandingZone : name.Contains("Harbor") ? StarmapObjectType.SpaceStation : StarmapObjectType.Outpost,
                IsArmistice = true
            };
        }

        if (clean.Contains("Orison", StringComparison.OrdinalIgnoreCase) || clean.Contains("Stanton2", StringComparison.OrdinalIgnoreCase) || clean.Contains("Crusader", StringComparison.OrdinalIgnoreCase) || clean.Contains("Seraphim", StringComparison.OrdinalIgnoreCase) || clean.Contains("GrimHex", StringComparison.OrdinalIgnoreCase) || clean.Contains("Grim HEX", StringComparison.OrdinalIgnoreCase))
        {
            string name = clean.Contains("Orison", StringComparison.OrdinalIgnoreCase) ? "Orison" :
                          clean.Contains("Seraphim", StringComparison.OrdinalIgnoreCase) ? "Seraphim Station" :
                          clean.Contains("Grim", StringComparison.OrdinalIgnoreCase) ? "Grim HEX" :
                          clean.Contains("Yela", StringComparison.OrdinalIgnoreCase) || clean.Contains("Deakins", StringComparison.OrdinalIgnoreCase) ? "Rayari Deakins" :
                          clean.Contains("Daymar", StringComparison.OrdinalIgnoreCase) ? "Daymar" :
                          clean.Contains("Cellin", StringComparison.OrdinalIgnoreCase) ? "Cellin" : "Crusader";
            return new ResolvedLocation
            {
                RawCode = clean,
                DisplayName = name,
                SystemName = "Stanton",
                ParentBody = name.Contains("Deakins") ? "Yela" : name == "Grim HEX" ? "Yela Orbit" : "Crusader",
                Type = name == "Orison" ? StarmapObjectType.LandingZone : name.Contains("Station") || name == "Grim HEX" ? StarmapObjectType.SpaceStation : StarmapObjectType.Outpost,
                IsArmistice = name != "Grim HEX"
            };
        }

        if (clean.Contains("Area18", StringComparison.OrdinalIgnoreCase) || clean.Contains("Area 18", StringComparison.OrdinalIgnoreCase) || clean.Contains("Stanton3", StringComparison.OrdinalIgnoreCase) || clean.Contains("ArcCorp", StringComparison.OrdinalIgnoreCase) || clean.Contains("Baijini", StringComparison.OrdinalIgnoreCase))
        {
            string name = clean.Contains("Area", StringComparison.OrdinalIgnoreCase) ? "Area 18" :
                          clean.Contains("Baijini", StringComparison.OrdinalIgnoreCase) ? "Baijini Point" :
                          clean.Contains("Lyria", StringComparison.OrdinalIgnoreCase) ? "Lyria" :
                          clean.Contains("Wala", StringComparison.OrdinalIgnoreCase) ? "Wala" : "ArcCorp";
            return new ResolvedLocation
            {
                RawCode = clean,
                DisplayName = name,
                SystemName = "Stanton",
                ParentBody = "ArcCorp",
                Type = name == "Area 18" ? StarmapObjectType.LandingZone : name.Contains("Baijini") ? StarmapObjectType.SpaceStation : StarmapObjectType.Outpost,
                IsArmistice = true
            };
        }

        if (clean.Contains("NewBabbage", StringComparison.OrdinalIgnoreCase) || clean.Contains("New Babbage", StringComparison.OrdinalIgnoreCase) || clean.Contains("Stanton4", StringComparison.OrdinalIgnoreCase) || clean.Contains("microTech", StringComparison.OrdinalIgnoreCase) || clean.Contains("Tressler", StringComparison.OrdinalIgnoreCase))
        {
            string name = clean.Contains("Babbage", StringComparison.OrdinalIgnoreCase) ? "New Babbage" :
                          clean.Contains("Tressler", StringComparison.OrdinalIgnoreCase) ? "Port Tressler" :
                          clean.Contains("Calliope", StringComparison.OrdinalIgnoreCase) || clean.Contains("Anvik", StringComparison.OrdinalIgnoreCase) ? "Rayari Anvik" :
                          clean.Contains("Clio", StringComparison.OrdinalIgnoreCase) ? "Clio" :
                          clean.Contains("Euterpe", StringComparison.OrdinalIgnoreCase) ? "Euterpe" : "microTech";
            return new ResolvedLocation
            {
                RawCode = clean,
                DisplayName = name,
                SystemName = "Stanton",
                ParentBody = name.Contains("Anvik") ? "Calliope" : "microTech",
                Type = name == "New Babbage" ? StarmapObjectType.LandingZone : name.Contains("Tressler") ? StarmapObjectType.SpaceStation : StarmapObjectType.Outpost,
                IsArmistice = true
            };
        }

        if (clean.StartsWith("RR_", StringComparison.OrdinalIgnoreCase) || clean.Contains("Lagrange", StringComparison.OrdinalIgnoreCase) || clean.Contains("-L", StringComparison.OrdinalIgnoreCase))
        {
            string sys = clean.Contains("HUR") || clean.Contains("ARC") || clean.Contains("CRU") || clean.Contains("MIC") ? "Stanton" :
                         clean.Contains("_P") ? "Pyro" : "Stanton";
            string resolvedName = Locations.Resolve(clean);
            return new ResolvedLocation
            {
                RawCode = clean,
                DisplayName = resolvedName,
                SystemName = sys,
                ParentBody = "Lagrange",
                Type = StarmapObjectType.LagrangeStation,
                IsArmistice = true
            };
        }

        if (clean.Contains("Pyro", StringComparison.OrdinalIgnoreCase) || clean.Contains("Checkmate", StringComparison.OrdinalIgnoreCase) || clean.Contains("Ruin Station", StringComparison.OrdinalIgnoreCase) || clean.Contains("Orbituary", StringComparison.OrdinalIgnoreCase))
        {
            return new ResolvedLocation
            {
                RawCode = clean,
                DisplayName = clean,
                SystemName = "Pyro",
                ParentBody = "Pyro System",
                Type = StarmapObjectType.SpaceStation,
                IsArmistice = false
            };
        }

        // Standard-Auflösung über Locations.Resolve
        var fallback = Locations.Resolve(clean);
        return new ResolvedLocation
        {
            RawCode = clean,
            DisplayName = fallback,
            SystemName = "Stanton",
            ParentBody = "Stanton",
            Type = StarmapObjectType.Outpost,
            IsArmistice = true
        };
    }

    private static void Register(StarmapObject obj)
    {
        if (!Systems.TryGetValue(obj.System, out var list))
        {
            list = new List<StarmapObject>();
            Systems[obj.System] = list;
        }
        list.Add(obj);
        ObjectsById[obj.Id] = obj;
    }

    private static void InitStanton()
    {
        // Zentralgestirn
        Register(new StarmapObject { Id = "stanton_star", Name = "Stanton (Stern)", System = "Stanton", Type = StarmapObjectType.Star, OrbitRadius = 0, OrbitAngleDeg = 0, ColorHex = "#FBBF24", Size = 26, Description = "G-Typ Hauptreihenstern" });

        // 1. Hurston
        var hurston = new StarmapObject { Id = "hurston", Name = "Hurston", System = "Stanton", ParentId = "stanton_star", Type = StarmapObjectType.Planet, OrbitRadius = 90, OrbitAngleDeg = 45, ColorHex = "#D97706", Size = 16, Description = "Industrieplanet im Besitz von Hurston Dynamics. Hauptstadt: Lorville." };
        Register(hurston);
        Register(new StarmapObject { Id = "lorville", Name = "Lorville", System = "Stanton", ParentId = "hurston", Type = StarmapObjectType.LandingZone, OrbitRadius = 90, OrbitAngleDeg = 48, ColorHex = "#F59E0B", Size = 9, HasArmistice = true, Description = "Hauptstadt von Hurston. Metro-Center, CBD, Raumhafen Teasa." });
        Register(new StarmapObject { Id = "everus", Name = "Everus Harbor", System = "Stanton", ParentId = "hurston", Type = StarmapObjectType.SpaceStation, OrbitRadius = 90, OrbitAngleDeg = 41, ColorHex = "#38BDF8", Size = 8, HasArmistice = true, Description = "Orbitale Hauptstation über Hurston mit Raffinerie & Hangar." });
        Register(new StarmapObject { Id = "arial", Name = "Arial", System = "Stanton", ParentId = "hurston", Type = StarmapObjectType.Moon, OrbitRadius = 106, OrbitAngleDeg = 55, ColorHex = "#FBBF24", Size = 6, Description = "Heißer, mineralreicher Mond von Hurston." });
        Register(new StarmapObject { Id = "aberdeen", Name = "Aberdeen", System = "Stanton", ParentId = "hurston", Type = StarmapObjectType.Moon, OrbitRadius = 112, OrbitAngleDeg = 32, ColorHex = "#CA8A04", Size = 6, Description = "Gefängnismond mit der Klescher Rehabilitation Facility." });
        Register(new StarmapObject { Id = "magda", Name = "Magda", System = "Stanton", ParentId = "hurston", Type = StarmapObjectType.Moon, OrbitRadius = 118, OrbitAngleDeg = 62, ColorHex = "#A16207", Size = 6, Description = "Kraterreicher Mond von Hurston." });
        Register(new StarmapObject { Id = "ita", Name = "Ita", System = "Stanton", ParentId = "hurston", Type = StarmapObjectType.Moon, OrbitRadius = 124, OrbitAngleDeg = 24, ColorHex = "#78350F", Size = 6, Description = "Mond von Hurston mit Außenposten HDMS-Ryder." });

        // 2. Crusader
        var crusader = new StarmapObject { Id = "crusader", Name = "Crusader", System = "Stanton", ParentId = "stanton_star", Type = StarmapObjectType.Planet, OrbitRadius = 160, OrbitAngleDeg = 140, ColorHex = "#EC4899", Size = 20, Description = "Gasriese mit atembarer oberer Atmosphäre. Heimat der Wolkenstadt Orison." };
        Register(crusader);
        Register(new StarmapObject { Id = "orison", Name = "Orison", System = "Stanton", ParentId = "crusader", Type = StarmapObjectType.LandingZone, OrbitRadius = 160, OrbitAngleDeg = 143, ColorHex = "#F472B6", Size = 10, HasArmistice = true, Description = "Schwebende Stadtplattform in der Atmosphäre von Crusader." });
        Register(new StarmapObject { Id = "seraphim", Name = "Seraphim Station", System = "Stanton", ParentId = "crusader", Type = StarmapObjectType.SpaceStation, OrbitRadius = 160, OrbitAngleDeg = 136, ColorHex = "#38BDF8", Size = 8, HasArmistice = true, Description = "Orbitale Raumstation über Crusader mit Fracht- und Hangardecks." });
        Register(new StarmapObject { Id = "cellin", Name = "Cellin", System = "Stanton", ParentId = "crusader", Type = StarmapObjectType.Moon, OrbitRadius = 176, OrbitAngleDeg = 125, ColorHex = "#E5E7EB", Size = 6, Description = "Vulkanisch aktiver Mond von Crusader." });
        Register(new StarmapObject { Id = "daymar", Name = "Daymar", System = "Stanton", ParentId = "crusader", Type = StarmapObjectType.Moon, OrbitRadius = 182, OrbitAngleDeg = 152, ColorHex = "#FDE68A", Size = 7, Description = "Wüstenmond von Crusader. Bekannt für Shubin Mining & Brio's." });
        Register(new StarmapObject { Id = "yela", Name = "Yela", System = "Stanton", ParentId = "crusader", Type = StarmapObjectType.Moon, OrbitRadius = 188, OrbitAngleDeg = 133, ColorHex = "#93C5FD", Size = 6, Description = "Eismond mit Asteroidengürtel. Versteck von Grim HEX." });
        Register(new StarmapObject { Id = "grimhex", Name = "Grim HEX", System = "Stanton", ParentId = "yela", Type = StarmapObjectType.SpaceStation, OrbitRadius = 192, OrbitAngleDeg = 131, ColorHex = "#EF4444", Size = 7, HasArmistice = false, Description = "Ehemalige Green-HEX-Bergbaubasis im Asteroidenring von Yela. Gesetzlose Station." });

        // 3. ArcCorp
        var arccorp = new StarmapObject { Id = "arccorp", Name = "ArcCorp", System = "Stanton", ParentId = "stanton_star", Type = StarmapObjectType.Planet, OrbitRadius = 230, OrbitAngleDeg = 235, ColorHex = "#F97316", Size = 16, Description = "Vollständig urbanisierter Stadtplanet. Heimat von Area 18." };
        Register(arccorp);
        Register(new StarmapObject { Id = "area18", Name = "Area 18", System = "Stanton", ParentId = "arccorp", Type = StarmapObjectType.LandingZone, OrbitRadius = 230, OrbitAngleDeg = 238, ColorHex = "#FB923C", Size = 9, HasArmistice = true, Description = "Hauptlandezone von ArcCorp mit Riker Memorial Spaceport und IO-Tower." });
        Register(new StarmapObject { Id = "baijini", Name = "Baijini Point", System = "Stanton", ParentId = "arccorp", Type = StarmapObjectType.SpaceStation, OrbitRadius = 230, OrbitAngleDeg = 231, ColorHex = "#38BDF8", Size = 8, HasArmistice = true, Description = "Orbitale Raumstation über ArcCorp." });
        Register(new StarmapObject { Id = "lyria", Name = "Lyria", System = "Stanton", ParentId = "arccorp", Type = StarmapObjectType.Moon, OrbitRadius = 246, OrbitAngleDeg = 222, ColorHex = "#BAE6FD", Size = 6, Description = "Eis- und Cryomond von ArcCorp. Reich an Quantainium." });
        Register(new StarmapObject { Id = "wala", Name = "Wala", System = "Stanton", ParentId = "arccorp", Type = StarmapObjectType.Moon, OrbitRadius = 252, OrbitAngleDeg = 248, ColorHex = "#FEF08A", Size = 6, Description = "Mineralmond von ArcCorp mit Shady Glen Farms." });

        // 4. microTech
        var microtech = new StarmapObject { Id = "microtech", Name = "microTech", System = "Stanton", ParentId = "stanton_star", Type = StarmapObjectType.Planet, OrbitRadius = 300, OrbitAngleDeg = 320, ColorHex = "#38BDF8", Size = 17, Description = "Kalter Tundra- und Schneeplanet. High-Tech-Zentrum und Sitz von microTech." };
        Register(microtech);
        Register(new StarmapObject { Id = "newbabbage", Name = "New Babbage", System = "Stanton", ParentId = "microtech", Type = StarmapObjectType.LandingZone, OrbitRadius = 300, OrbitAngleDeg = 323, ColorHex = "#67E8F9", Size = 10, HasArmistice = true, Description = "High-Tech-Kuppelstadt am Aspire Grand und Tobin Expo Center." });
        Register(new StarmapObject { Id = "porttressler", Name = "Port Tressler", System = "Stanton", ParentId = "microtech", Type = StarmapObjectType.SpaceStation, OrbitRadius = 300, OrbitAngleDeg = 316, ColorHex = "#38BDF8", Size = 8, HasArmistice = true, Description = "Orbitalstation über microTech mit Logistikzentrum und Hangar-Hub." });
        Register(new StarmapObject { Id = "calliope", Name = "Calliope", System = "Stanton", ParentId = "microtech", Type = StarmapObjectType.Moon, OrbitRadius = 316, OrbitAngleDeg = 308, ColorHex = "#E0F2FE", Size = 6, Description = "Stürmischer Eismond mit Forschungsposten Rayari Anvik." });
        Register(new StarmapObject { Id = "clio", Name = "Clio", System = "Stanton", ParentId = "microtech", Type = StarmapObjectType.Moon, OrbitRadius = 322, OrbitAngleDeg = 332, ColorHex = "#7DD3FC", Size = 6, Description = "Ozean- und Eismond mit flüssigen Meeren." });
        Register(new StarmapObject { Id = "euterpe", Name = "Euterpe", System = "Stanton", ParentId = "microtech", Type = StarmapObjectType.Moon, OrbitRadius = 328, OrbitAngleDeg = 312, ColorHex = "#C7D2FE", Size = 5, Description = "Glatteis-Mond von microTech. Schauplatz von Devlin Scrap & Salvage." });

        // Jumppoints & Gateways
        Register(new StarmapObject { Id = "jp_pyro", Name = "Pyro Gateway (Jumppoint)", System = "Stanton", ParentId = "stanton_star", Type = StarmapObjectType.JumpPoint, OrbitRadius = 360, OrbitAngleDeg = 80, ColorHex = "#F43F5E", Size = 10, Description = "Sprungtor in das Gesetzlosen-System Pyro." });
        Register(new StarmapObject { Id = "jp_magnus", Name = "Magnus Gateway", System = "Stanton", ParentId = "stanton_star", Type = StarmapObjectType.JumpPoint, OrbitRadius = 360, OrbitAngleDeg = 200, ColorHex = "#818CF8", Size = 9, Description = "Sprungtor in das Magnus-System." });
        Register(new StarmapObject { Id = "jp_terra", Name = "Terra Gateway", System = "Stanton", ParentId = "stanton_star", Type = StarmapObjectType.JumpPoint, OrbitRadius = 360, OrbitAngleDeg = 290, ColorHex = "#34D399", Size = 9, Description = "Sprungtor zum UEE-Hauptweltsystem Terra." });
    }

    private static void InitPyro()
    {
        Register(new StarmapObject { Id = "pyro_star", Name = "Pyro (Flare-Stern)", System = "Pyro", Type = StarmapObjectType.Star, OrbitRadius = 0, OrbitAngleDeg = 0, ColorHex = "#EF4444", Size = 28, Description = "Instabiler Roter Zwerg / Flare-Stern." });

        Register(new StarmapObject { Id = "pyro1", Name = "Pyro I", System = "Pyro", ParentId = "pyro_star", Type = StarmapObjectType.Planet, OrbitRadius = 70, OrbitAngleDeg = 20, ColorHex = "#7F1D1D", Size = 10, Description = "Glühender Lavaplanet nah am Stern." });
        Register(new StarmapObject { Id = "monox", Name = "Monox (Pyro II)", System = "Pyro", ParentId = "pyro_star", Type = StarmapObjectType.Planet, OrbitRadius = 120, OrbitAngleDeg = 95, ColorHex = "#D97706", Size = 15, Description = "Dürre Wüstenwelt mit Checkmate Station." });
        Register(new StarmapObject { Id = "checkmate", Name = "Checkmate Station", System = "Pyro", ParentId = "monox", Type = StarmapObjectType.SpaceStation, OrbitRadius = 120, OrbitAngleDeg = 98, ColorHex = "#F59E0B", Size = 8, HasArmistice = false, Description = "Hauptumschlagplatz der Rough & Ready Gang." });
        Register(new StarmapObject { Id = "bloom", Name = "Bloom (Pyro III)", System = "Pyro", ParentId = "pyro_star", Type = StarmapObjectType.Planet, OrbitRadius = 175, OrbitAngleDeg = 170, ColorHex = "#EA580C", Size = 14, Description = "Tektonisch aktive Welt mit Starlight Station und Orbituary." });
        Register(new StarmapObject { Id = "orbituary", Name = "Orbituary", System = "Pyro", ParentId = "bloom", Type = StarmapObjectType.SpaceStation, OrbitRadius = 175, OrbitAngleDeg = 165, ColorHex = "#FB923C", Size = 7, HasArmistice = false, Description = "Orbitale Raumbasis über Bloom." });
        Register(new StarmapObject { Id = "pyro4", Name = "Pyro IV", System = "Pyro", ParentId = "pyro_star", Type = StarmapObjectType.Planet, OrbitRadius = 230, OrbitAngleDeg = 250, ColorHex = "#A8A29E", Size = 12, Description = "Zerstörter Protoplanet in Kollision mit Pyro V." });
        Register(new StarmapObject { Id = "pyro5", Name = "Pyro V", System = "Pyro", ParentId = "pyro_star", Type = StarmapObjectType.Planet, OrbitRadius = 285, OrbitAngleDeg = 310, ColorHex = "#0284C7", Size = 22, Description = "Riesiger Gasriese mit vielen Monden (Ignis, Adir, Fairo, FTransit)." });
        Register(new StarmapObject { Id = "terminus", Name = "Terminus (Pyro VI)", System = "Pyro", ParentId = "pyro_star", Type = StarmapObjectType.Planet, OrbitRadius = 335, OrbitAngleDeg = 40, ColorHex = "#475569", Size = 15, Description = "Äußerer Eisplanet. Heimat der berüchtigten Ruin Station." });
        Register(new StarmapObject { Id = "ruinstation", Name = "Ruin Station", System = "Pyro", ParentId = "terminus", Type = StarmapObjectType.SpaceStation, OrbitRadius = 335, OrbitAngleDeg = 44, ColorHex = "#EF4444", Size = 9, HasArmistice = false, Description = "Zentrum der Piratenaktivität und Handelsstützpunkt von Pyro VI." });

        Register(new StarmapObject { Id = "jp_stanton_from_pyro", Name = "Stanton Gateway", System = "Pyro", ParentId = "pyro_star", Type = StarmapObjectType.JumpPoint, OrbitRadius = 360, OrbitAngleDeg = 260, ColorHex = "#38BDF8", Size = 10, Description = "Sprungtor zurück in das Stanton-System." });
        Register(new StarmapObject { Id = "jp_nyx_from_pyro", Name = "Nyx Gateway", System = "Pyro", ParentId = "pyro_star", Type = StarmapObjectType.JumpPoint, OrbitRadius = 360, OrbitAngleDeg = 140, ColorHex = "#A855F7", Size = 9, Description = "Sprungtor in das Nyx-System." });
    }

    private static void InitNyx()
    {
        Register(new StarmapObject { Id = "nyx_star", Name = "Nyx (Stern)", System = "Nyx", Type = StarmapObjectType.Star, OrbitRadius = 0, OrbitAngleDeg = 0, ColorHex = "#60A5FA", Size = 24, Description = "Blau-Weißer Hauptreihenstern (F-Klasse)." });

        Register(new StarmapObject { Id = "nyx1", Name = "Nyx I", System = "Nyx", ParentId = "nyx_star", Type = StarmapObjectType.Planet, OrbitRadius = 90, OrbitAngleDeg = 60, ColorHex = "#94A3B8", Size = 11, Description = "Felsiger Kernplanet in Sternennähe." });
        
        // Delamar (Glaciem Ring / Levski)
        var delamar = new StarmapObject { Id = "delamar", Name = "Delamar", System = "Nyx", ParentId = "nyx_star", Type = StarmapObjectType.Moon, OrbitRadius = 180, OrbitAngleDeg = 150, ColorHex = "#64748B", Size = 15, Description = "Großer Planetoid im Asteroidengürtel (Glaciem Ring). Sitz der freien Kolonie Levski." };
        Register(delamar);
        Register(new StarmapObject { Id = "levski", Name = "Levski", System = "Nyx", ParentId = "delamar", Type = StarmapObjectType.LandingZone, OrbitRadius = 180, OrbitAngleDeg = 154, ColorHex = "#38BDF8", Size = 10, HasArmistice = true, Description = "Freie Bergbaustadt im Krater von Delamar. Gegründet von der People's Alliance (Recco Battaglia & Co.)." });
        Register(new StarmapObject { Id = "keeger", Name = "Asteroidenbasis Keeger", System = "Nyx", ParentId = "delamar", Type = StarmapObjectType.Outpost, OrbitRadius = 195, OrbitAngleDeg = 142, ColorHex = "#E2E8F0", Size = 6, Description = "Außenposten im Asteroidenfeld nahe Delamar." });

        Register(new StarmapObject { Id = "nyx3", Name = "Nyx III", System = "Nyx", ParentId = "nyx_star", Type = StarmapObjectType.Planet, OrbitRadius = 270, OrbitAngleDeg = 240, ColorHex = "#0369A1", Size = 18, Description = "Eisiger Gasriese im äußeren Nyx-System." });

        Register(new StarmapObject { Id = "jp_pyro_from_nyx", Name = "Pyro Gateway", System = "Nyx", ParentId = "nyx_star", Type = StarmapObjectType.JumpPoint, OrbitRadius = 350, OrbitAngleDeg = 320, ColorHex = "#F97316", Size = 10, Description = "Sprungtor in das Pyro-System." });
        Register(new StarmapObject { Id = "jp_castra", Name = "Castra Gateway", System = "Nyx", ParentId = "nyx_star", Type = StarmapObjectType.JumpPoint, OrbitRadius = 350, OrbitAngleDeg = 45, ColorHex = "#10B981", Size = 9, Description = "Sprungtor in das UEE-Grenzsystem Castra." });
    }
}
