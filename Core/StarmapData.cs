using System;
using System.Collections.Generic;
using System.Linq;

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
    public double OrbitRadius { get; set; }    // Visueller Radius im System-Canvas (z.B. 70 bis 400)
    public double OrbitAngleDeg { get; set; }  // Winkel auf der Umlaufbahn (0 - 360)
    public string ColorHex { get; set; } = "#58A6FF";
    public double Size { get; set; } = 8;
    public bool HasArmistice { get; set; } = true;
    public string Jurisdiction { get; set; } = "UEE";
    public string SecurityLevel { get; set; } = "High"; // High, Medium, Low, Lawless
    public string Specialization { get; set; } = "";    // z. B. ⛏ Raffinerie, 📦 Frachtzentrum, 🏥 Klinik
    public string Resources { get; set; } = "";         // z. B. Quantanium, Gold, Beryll, RMC
    public string Description { get; set; } = "";
    public string? TargetSystem { get; set; }           // Bei Sprungtoren: Ziel-System (z. B. "Pyro")
    public List<StarmapObject> Children { get; set; } = new();

    // Berechnete Canvas-Position bezogen auf Mittelpunkt (0,0)
    public double RelX => OrbitRadius * Math.Cos(OrbitAngleDeg * Math.PI / 180.0);
    public double RelY => OrbitRadius * Math.Sin(OrbitAngleDeg * Math.PI / 180.0);
}

public sealed class QuantumDriveProfile
{
    public string Name { get; init; } = "";
    public string SizeClass { get; init; } = "S1"; // S1, S2, S3
    public double TopSpeedKmS { get; init; } = 150000;
    public double AccelRate { get; init; } = 1.0;

    public string DisplayText => $"{SizeClass} {Name} ({TopSpeedKmS:N0} km/s)";
}

public sealed class ResolvedLocation
{
    public string RawCode { get; set; } = "";
    public string DisplayName { get; set; } = "Unbekannt";
    public string SystemName { get; set; } = "Stanton";
    public string ParentBody { get; set; } = "—";
    public StarmapObjectType Type { get; set; } = StarmapObjectType.Outpost;
    public bool IsArmistice { get; set; }
    public string ArmisticeStatusText => IsArmistice ? "🟢 Schutzzone aktiv" : "🔴 Keine Schutzzone (Waffen scharf)";
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

    public static readonly List<QuantumDriveProfile> AvailableDrives = new()
    {
        new QuantumDriveProfile { Name = "VK-00", SizeClass = "S1", TopSpeedKmS = 283000 },
        new QuantumDriveProfile { Name = "Atlas", SizeClass = "S1", TopSpeedKmS = 152000 },
        new QuantumDriveProfile { Name = "Beacon", SizeClass = "S1", TopSpeedKmS = 126000 },
        new QuantumDriveProfile { Name = "Crossfield", SizeClass = "S2", TopSpeedKmS = 261000 },
        new QuantumDriveProfile { Name = "Yeager", SizeClass = "S2", TopSpeedKmS = 180000 },
        new QuantumDriveProfile { Name = "Bolon", SizeClass = "S2", TopSpeedKmS = 139000 },
        new QuantumDriveProfile { Name = "TS-2", SizeClass = "S3", TopSpeedKmS = 260000 },
        new QuantumDriveProfile { Name = "Pontes", SizeClass = "S3", TopSpeedKmS = 262000 },
        new QuantumDriveProfile { Name = "Agni", SizeClass = "S3", TopSpeedKmS = 218000 }
    };

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
        if (string.IsNullOrWhiteSpace(nameOrId)) return null;
        if (ObjectsById.TryGetValue(nameOrId, out var obj)) return obj;
        return ObjectsById.Values.FirstOrDefault(o => o.Name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase) || o.Id.Equals(nameOrId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Berechnet die echte Distanz und Flugdauer zwischen zwei Starmap-Objekten.
    /// Skalierungsfaktor: 1 Canvas-Einheit = ~150.000 km (Stanton Systemradius ≈ 60 Millionen km).
    /// </summary>
    public static (double DistKm, double DistGm, TimeSpan FlightTime) CalculateRoute(StarmapObject from, StarmapObject to, QuantumDriveProfile drive)
    {
        double dx = to.RelX - from.RelX;
        double dy = to.RelY - from.RelY;
        double canvasDist = Math.Sqrt(dx * dx + dy * dy);

        // 1 Canvas-Einheit entspricht 150.000 Kilometern
        double distKm = canvasDist * 150000.0;
        if (distKm < 50000.0) distKm = 50000.0; // Mindestdistanz für Orbit / Landezone
        double distGm = distKm / 1_000_000.0;

        // Beschleunigung & Endgeschwindigkeit einbeziehen
        double speed = drive.TopSpeedKmS > 0 ? drive.TopSpeedKmS : 150000.0;
        double travelSeconds = (distKm / speed) + 12.0; // 12s Spool-Up & Kalibrierung
        var flightTime = TimeSpan.FromSeconds(travelSeconds);

        return (distKm, distGm, flightTime);
    }

    public static ResolvedLocation Resolve(string raw)
    {
        return Locations.ResolveLocation(raw);
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
        Register(new StarmapObject { Id = "stanton_star", Name = "Stanton (Stern)", System = "Stanton", Type = StarmapObjectType.Star, OrbitRadius = 0, OrbitAngleDeg = 0, ColorHex = "#FBBF24", Size = 28, Description = "G-Typ Hauptreihenstern mit 4 bewohnten Planeten im UEE-Besitz." });

        // 1. Hurston
        var hurston = new StarmapObject { Id = "hurston", Name = "Hurston", System = "Stanton", ParentId = "stanton_star", Type = StarmapObjectType.Planet, OrbitRadius = 90, OrbitAngleDeg = 45, ColorHex = "#D97706", Size = 17, SecurityLevel = "High", Jurisdiction = "Hurston Dynamics", Resources = "Beryll, Titan, Wolfram", Description = "Industrieplanet im Besitz von Hurston Dynamics. Hauptstadt: Lorville." };
        Register(hurston);
        Register(new StarmapObject { Id = "lorville", Name = "Lorville", System = "Stanton", ParentId = "hurston", Type = StarmapObjectType.LandingZone, OrbitRadius = 90, OrbitAngleDeg = 48, ColorHex = "#F59E0B", Size = 9, HasArmistice = true, SecurityLevel = "High", Jurisdiction = "Hurston Dynamics", Specialization = "📦 Großhandelszentrum · 🏪 Schiffshändler", Description = "Hauptstadt von Hurston. Metro-Center, CBD, Raumhafen Teasa." });
        Register(new StarmapObject { Id = "everus", Name = "Everus Harbor", System = "Stanton", ParentId = "hurston", Type = StarmapObjectType.SpaceStation, OrbitRadius = 90, OrbitAngleDeg = 41, ColorHex = "#38BDF8", Size = 8, HasArmistice = true, SecurityLevel = "High", Specialization = "⛏ Raffinerie & Hangars", Description = "Orbitale Hauptstation über Hurston mit Raffinerie & Frachtdecks." });
        Register(new StarmapObject { Id = "orinth", Name = "Reclamation & Disposal Orinth", System = "Stanton", ParentId = "hurston", Type = StarmapObjectType.Outpost, OrbitRadius = 90, OrbitAngleDeg = 52, ColorHex = "#EAB308", Size = 5, HasArmistice = false, SecurityLevel = "Low", Specialization = "🧲 Schrott- & Bergungsplatz", Description = "Schrottplatz und Umschlagplatz für RMC und illegale Güter auf Hurston." });
        Register(new StarmapObject { Id = "hdpc_cassidy", Name = "HDPC-Cassidy Distribution Center", System = "Stanton", ParentId = "hurston", Type = StarmapObjectType.Outpost, OrbitRadius = 90, OrbitAngleDeg = 38, ColorHex = "#F97316", Size = 5.5, HasArmistice = true, Specialization = "🏢 Hurston Dynamics Verteilzentrum", Description = "Großes automatisiertes Logistik- und Frachtzentrum von Hurston Dynamics." });

        // Hurston Monde
        Register(new StarmapObject { Id = "arial", Name = "Arial", System = "Stanton", ParentId = "hurston", Type = StarmapObjectType.Moon, OrbitRadius = 106, OrbitAngleDeg = 55, ColorHex = "#FBBF24", Size = 6, Resources = "Laranit, Titan", Description = "Heißer, mineralreicher Mond von Hurston mit HDMS-Bezdek & Lathan." });
        Register(new StarmapObject { Id = "aberdeen", Name = "Aberdeen", System = "Stanton", ParentId = "hurston", Type = StarmapObjectType.Moon, OrbitRadius = 112, OrbitAngleDeg = 32, ColorHex = "#CA8A04", Size = 6, Resources = "Hadannit, Dolivin", Description = "Dichter Schwefelmond mit der Klescher Rehabilitation Facility." });
        Register(new StarmapObject { Id = "klescher", Name = "Klescher Automated Rehabilitation", System = "Stanton", ParentId = "aberdeen", Type = StarmapObjectType.Outpost, OrbitRadius = 112, OrbitAngleDeg = 30, ColorHex = "#EF4444", Size = 5, HasArmistice = false, SecurityLevel = "High", Specialization = "🔒 Gefängniskomplex & Bergbauminen", Description = "Automatisierte UEE-Strafkolonie mit Tiefbergbau-Tunneln auf Aberdeen." });
        Register(new StarmapObject { Id = "magda", Name = "Magda", System = "Stanton", ParentId = "hurston", Type = StarmapObjectType.Moon, OrbitRadius = 118, OrbitAngleDeg = 62, ColorHex = "#A16207", Size = 6, Resources = "Corundum, Diamant", Description = "Kraterreicher Mond von Hurston mit HDMS-Perlman." });
        Register(new StarmapObject { Id = "ita", Name = "Ita", System = "Stanton", ParentId = "hurston", Type = StarmapObjectType.Moon, OrbitRadius = 124, OrbitAngleDeg = 24, ColorHex = "#78350F", Size = 6, Resources = "Gold, Beryll", Description = "Felsmond von Hurston mit Außenposten HDMS-Ryder & HDMS-Woodward." });

        // Hurston Lagrange Points (HUR-L1 bis HUR-L5)
        Register(new StarmapObject { Id = "hur_l1", Name = "HUR-L1 Green Glade", System = "Stanton", ParentId = "hurston", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 90, OrbitAngleDeg = 105, ColorHex = "#34D399", Size = 8, HasArmistice = true, Specialization = "⛏ Raffinerie & Fracht", Description = "Große Raffinerie-Station im Lagrange-Punkt L1 von Hurston." });
        Register(new StarmapObject { Id = "hur_l2", Name = "HUR-L2 Faithful Dream", System = "Stanton", ParentId = "hurston", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 90, OrbitAngleDeg = -15, ColorHex = "#38BDF8", Size = 7, HasArmistice = true, Specialization = "📦 Fracht & Rest Stop", Description = "Logistik- & Versorgungsstation im L2-Punkt von Hurston." });
        Register(new StarmapObject { Id = "hur_l3", Name = "HUR-L3 Thundering Express", System = "Stanton", ParentId = "hurston", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 90, OrbitAngleDeg = 225, ColorHex = "#38BDF8", Size = 7, HasArmistice = true, Specialization = "🏪 Rest Stop & Klinik", Description = "Entlegene Versorgungsstation gegenüber von Hurston." });
        Register(new StarmapObject { Id = "hur_l4", Name = "HUR-L4 Melodic Fields", System = "Stanton", ParentId = "hurston", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 90, OrbitAngleDeg = 165, ColorHex = "#34D399", Size = 8, HasArmistice = true, Specialization = "⛏ Raffinerie", Description = "Bergbaustation im Asteroidenfeld des L4-Punkts." });
        Register(new StarmapObject { Id = "hur_l5", Name = "HUR-L5 High Course", System = "Stanton", ParentId = "hurston", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 90, OrbitAngleDeg = -75, ColorHex = "#34D399", Size = 8, HasArmistice = true, Specialization = "⛏ Raffinerie & Klinik", Description = "Erstrangige Raffinerie- und Reparaturstation." });

        // 2. Crusader
        var crusader = new StarmapObject { Id = "crusader", Name = "Crusader", System = "Stanton", ParentId = "stanton_star", Type = StarmapObjectType.Planet, OrbitRadius = 170, OrbitAngleDeg = 140, ColorHex = "#EC4899", Size = 21, SecurityLevel = "High", Jurisdiction = "Crusader Industries", Description = "Gasriese mit atembarer oberer Atmosphäre. Heimat der Wolkenstadt Orison." };
        Register(crusader);
        Register(new StarmapObject { Id = "orison", Name = "Orison", System = "Stanton", ParentId = "crusader", Type = StarmapObjectType.LandingZone, OrbitRadius = 170, OrbitAngleDeg = 143, ColorHex = "#F472B6", Size = 10, HasArmistice = true, SecurityLevel = "High", Jurisdiction = "Crusader Industries", Specialization = "🚢 Schiffswerft & Luxus-Showroom", Description = "Schwebende Stadtplattform in der Atmosphäre von Crusader. Showrooms von Crusader & Cousin Crow's." });
        Register(new StarmapObject { Id = "seraphim", Name = "Seraphim Station", System = "Stanton", ParentId = "crusader", Type = StarmapObjectType.SpaceStation, OrbitRadius = 170, OrbitAngleDeg = 136, ColorHex = "#38BDF8", Size = 8, HasArmistice = true, SecurityLevel = "High", Specialization = "📦 Frachtdecks & Hangars", Description = "Orbitale Raumstation über Crusader mit Fracht- und Hangardecks." });
        
        // Crusader Monde
        Register(new StarmapObject { Id = "cellin", Name = "Cellin", System = "Stanton", ParentId = "crusader", Type = StarmapObjectType.Moon, OrbitRadius = 186, OrbitAngleDeg = 125, ColorHex = "#E5E7EB", Size = 6, Resources = "Gold, Beryll, Diamant", Description = "Vulkanisch aktiver Mond von Crusader mit Tram & Myers Mining." });
        Register(new StarmapObject { Id = "spk", Name = "Security Post Kareah", System = "Stanton", ParentId = "cellin", Type = StarmapObjectType.SpaceStation, OrbitRadius = 188, OrbitAngleDeg = 122, ColorHex = "#EF4444", Size = 6.5, HasArmistice = false, SecurityLevel = "High", Specialization = "🛡️ UEE-Sicherheitsaußenposten & CrimeStat-Terminal", Description = "Orbitale UEE-Sicherheitsstation über Cellin. CrimeStat-Löschterminal und Evidence-Locker." });
        Register(new StarmapObject { Id = "daymar", Name = "Daymar", System = "Stanton", ParentId = "crusader", Type = StarmapObjectType.Moon, OrbitRadius = 192, OrbitAngleDeg = 152, ColorHex = "#FDE68A", Size = 7, Resources = "Hadannit, Beryll, Aphorit", Description = "Wüstenmond von Crusader. Bekannt für Shubin Mining SCD-1 & Brio's Breaker Yard." });
        Register(new StarmapObject { Id = "brios", Name = "Brio's Breaker Yard", System = "Stanton", ParentId = "daymar", Type = StarmapObjectType.Outpost, OrbitRadius = 192, OrbitAngleDeg = 155, ColorHex = "#F59E0B", Size = 5, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "🧲 RMC- & Drogen-Umschlagplatz", Description = "Berüchtigter Gesetzlosen-Schrottplatz auf Daymar. Ankauf von RMC und illegalen Waren." });
        Register(new StarmapObject { Id = "yela", Name = "Yela", System = "Stanton", ParentId = "crusader", Type = StarmapObjectType.Moon, OrbitRadius = 198, OrbitAngleDeg = 133, ColorHex = "#93C5FD", Size = 6, Resources = "Quantanium, Diamant, RMC", Description = "Eismond mit dichtem Asteroidengürtel. Versteck von Grim HEX." });
        Register(new StarmapObject { Id = "grimhex", Name = "Grim HEX", System = "Stanton", ParentId = "yela", Type = StarmapObjectType.SpaceStation, OrbitRadius = 202, OrbitAngleDeg = 131, ColorHex = "#EF4444", Size = 8, HasArmistice = false, SecurityLevel = "Lawless", Jurisdiction = "Outlaw Syndicates", Specialization = "🏴‍☠️ Schwarzmarkt & Schmuggel", Description = "Ehemalige Green-HEX-Bergbaubasis im Asteroidenring von Yela. Gesetzlose Station." });

        // Crusader Lagrange Points
        Register(new StarmapObject { Id = "cru_l1", Name = "CRU-L1 Ambitious Dream", System = "Stanton", ParentId = "crusader", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 170, OrbitAngleDeg = 200, ColorHex = "#34D399", Size = 8, HasArmistice = true, Specialization = "⛏ Raffinerie & Fracht", Description = "Raffinerie-Hub zwischen Crusader und ArcCorp." });
        Register(new StarmapObject { Id = "cru_l4", Name = "CRU-L4 Shallow Frontier", System = "Stanton", ParentId = "crusader", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 170, OrbitAngleDeg = 80, ColorHex = "#38BDF8", Size = 7, HasArmistice = true, Specialization = "🏪 Rest Stop", Description = "Versorgungsstation am Crusader-L4 Punkt." });
        Register(new StarmapObject { Id = "cru_l5", Name = "CRU-L5 Beautiful Glen", System = "Stanton", ParentId = "crusader", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 170, OrbitAngleDeg = 20, ColorHex = "#38BDF8", Size = 7, HasArmistice = true, Specialization = "📦 Fracht & Klinik", Description = "Wichtiger Zwischenstopp für Frachtrouten nach Hurston." });

        // 3. ArcCorp
        var arccorp = new StarmapObject { Id = "arccorp", Name = "ArcCorp", System = "Stanton", ParentId = "stanton_star", Type = StarmapObjectType.Planet, OrbitRadius = 250, OrbitAngleDeg = 235, ColorHex = "#F97316", Size = 17, SecurityLevel = "High", Jurisdiction = "ArcCorp", Description = "Vollständig urbanisierter Stadtplanet. Heimat von Area 18." };
        Register(arccorp);
        Register(new StarmapObject { Id = "area18", Name = "Area 18", System = "Stanton", ParentId = "arccorp", Type = StarmapObjectType.LandingZone, OrbitRadius = 250, OrbitAngleDeg = 238, ColorHex = "#FB923C", Size = 10, HasArmistice = true, SecurityLevel = "High", Jurisdiction = "ArcCorp", Specialization = "🏢 Mega-Shopping & IO-Tower", Description = "Hauptlandezone von ArcCorp mit Riker Memorial Spaceport, Astro Armada und Dumper's Depot." });
        Register(new StarmapObject { Id = "area061", Name = "Area 061", System = "Stanton", ParentId = "arccorp", Type = StarmapObjectType.Outpost, OrbitRadius = 250, OrbitAngleDeg = 242, ColorHex = "#EA580C", Size = 5.5, HasArmistice = true, Specialization = "🏭 Industrie- & Frachtzone", Description = "Industrieller Fracht- und Fabrikbezirk auf ArcCorp." });
        Register(new StarmapObject { Id = "baijini", Name = "Baijini Point", System = "Stanton", ParentId = "arccorp", Type = StarmapObjectType.SpaceStation, OrbitRadius = 250, OrbitAngleDeg = 231, ColorHex = "#38BDF8", Size = 8, HasArmistice = true, SecurityLevel = "High", Specialization = "📦 Frachtdecks & Waffen", Description = "Orbitale Raumstation über ArcCorp mit direkter Shuttle-Verbindung." });
        
        // ArcCorp Monde
        Register(new StarmapObject { Id = "lyria", Name = "Lyria", System = "Stanton", ParentId = "arccorp", Type = StarmapObjectType.Moon, OrbitRadius = 266, OrbitAngleDeg = 222, ColorHex = "#BAE6FD", Size = 6, Resources = "Quantanium, Laranit, Titan", Description = "Eis- und Cryomond von ArcCorp. Hauptherkunftsort von reinem Quantanium mit Shubin SAL-2 & SAL-5." });
        Register(new StarmapObject { Id = "wala", Name = "Wala", System = "Stanton", ParentId = "arccorp", Type = StarmapObjectType.Moon, OrbitRadius = 272, OrbitAngleDeg = 248, ColorHex = "#FEF08A", Size = 6, Resources = "Beryll, Gold, Diamant", Description = "Mineralmond von ArcCorp mit Samson & Son Scrap und ArcCorp Mining Area 045." });
        Register(new StarmapObject { Id = "samson", Name = "Samson & Son Salvage", System = "Stanton", ParentId = "wala", Type = StarmapObjectType.Outpost, OrbitRadius = 272, OrbitAngleDeg = 251, ColorHex = "#F59E0B", Size = 5, HasArmistice = false, SecurityLevel = "Low", Specialization = "🧲 Schrott- & Bergungsplatz", Description = "Schrottplatz auf Wala mit RMC- und Schwarzmarkt-Handel." });

        // ArcCorp Lagrange Points
        Register(new StarmapObject { Id = "arc_l1", Name = "ARC-L1 Wide Forest", System = "Stanton", ParentId = "arccorp", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 250, OrbitAngleDeg = 295, ColorHex = "#34D399", Size = 8, HasArmistice = true, Specialization = "⛏ Raffinerie & Quantanium-Hub", Description = "Große Raffinerie für das von Lyria abgebaute Quantanium." });
        Register(new StarmapObject { Id = "arc_l2", Name = "ARC-L2 Rest Stop", System = "Stanton", ParentId = "arccorp", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 250, OrbitAngleDeg = 175, ColorHex = "#38BDF8", Size = 7, HasArmistice = true, Specialization = "🏪 Rest Stop", Description = "Versorgungsstation am ArcCorp L2 Punkt." });
        Register(new StarmapObject { Id = "arc_l3", Name = "ARC-L3 Modern Icarus", System = "Stanton", ParentId = "arccorp", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 250, OrbitAngleDeg = 55, ColorHex = "#38BDF8", Size = 7, HasArmistice = true, Specialization = "🏪 Rest Stop & Klinik", Description = "Raststation im hinteren Orbit von ArcCorp." });
        Register(new StarmapObject { Id = "arc_l4", Name = "ARC-L4 Faint Glen", System = "Stanton", ParentId = "arccorp", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 250, OrbitAngleDeg = -5, ColorHex = "#34D399", Size = 8, HasArmistice = true, Specialization = "⛏ Raffinerie", Description = "Raffineriestation am L4-Punkt von ArcCorp." });

        // 4. microTech
        var microtech = new StarmapObject { Id = "microtech", Name = "microTech", System = "Stanton", ParentId = "stanton_star", Type = StarmapObjectType.Planet, OrbitRadius = 330, OrbitAngleDeg = 320, ColorHex = "#38BDF8", Size = 18, SecurityLevel = "High", Jurisdiction = "microTech", Description = "Kalter Tundra- und Schneeplanet. High-Tech-Zentrum und Sitz von microTech." };
        Register(microtech);
        Register(new StarmapObject { Id = "newbabbage", Name = "New Babbage", System = "Stanton", ParentId = "microtech", Type = StarmapObjectType.LandingZone, OrbitRadius = 330, OrbitAngleDeg = 323, ColorHex = "#67E8F9", Size = 10, HasArmistice = true, SecurityLevel = "High", Jurisdiction = "microTech", Specialization = "🏙 Tech-Kuppelstadt · 💻 The Commons", Description = "High-Tech-Kuppelstadt am Aspire Grand und Tobin Expo Center. The Commons & Centermass." });
        Register(new StarmapObject { Id = "porttressler", Name = "Port Tressler", System = "Stanton", ParentId = "microtech", Type = StarmapObjectType.SpaceStation, OrbitRadius = 330, OrbitAngleDeg = 316, ColorHex = "#38BDF8", Size = 8, HasArmistice = true, SecurityLevel = "High", Specialization = "📦 Frachtdecks & Großhangars", Description = "Orbitalstation über microTech mit Logistikzentrum und Hangar-Hub." });
        Register(new StarmapObject { Id = "sakura_sun", Name = "Sakura Sun Goldenrod Work Center", System = "Stanton", ParentId = "microtech", Type = StarmapObjectType.Outpost, OrbitRadius = 330, OrbitAngleDeg = 327, ColorHex = "#EC4899", Size = 5.5, HasArmistice = true, Specialization = "🏢 Sakura Sun Verteilzentrum", Description = "Hochmodernes Elektronik- und Biotech-Verteilzentrum von Sakura Sun auf microTech." });

        // microTech Monde
        Register(new StarmapObject { Id = "calliope", Name = "Calliope", System = "Stanton", ParentId = "microtech", Type = StarmapObjectType.Moon, OrbitRadius = 346, OrbitAngleDeg = 308, ColorHex = "#E0F2FE", Size = 6, Resources = "Beryll, Corundum", Description = "Stürmischer Eismond mit Forschungsposten Rayari Anvik & Kaltre." });
        Register(new StarmapObject { Id = "clio", Name = "Clio", System = "Stanton", ParentId = "microtech", Type = StarmapObjectType.Moon, OrbitRadius = 352, OrbitAngleDeg = 332, ColorHex = "#7DD3FC", Size = 6, Resources = "Titan, Diamant", Description = "Ozean- und Eismond mit flüssigen Meeren und Außenposten Rayari Cantwell." });
        Register(new StarmapObject { Id = "euterpe", Name = "Euterpe", System = "Stanton", ParentId = "microtech", Type = StarmapObjectType.Moon, OrbitRadius = 358, OrbitAngleDeg = 312, ColorHex = "#C7D2FE", Size = 5, Resources = "RMC-Wracks, Eisen", Description = "Glatteis-Mond von microTech mit Devlin Scrap & Salvage." });
        Register(new StarmapObject { Id = "devlin", Name = "Devlin Scrap & Salvage", System = "Stanton", ParentId = "euterpe", Type = StarmapObjectType.Outpost, OrbitRadius = 358, OrbitAngleDeg = 315, ColorHex = "#F59E0B", Size = 5, HasArmistice = false, SecurityLevel = "Low", Specialization = "🧲 Bergungs- & RMC-Depot", Description = "Schrott- und Bergungsplatz auf Euterpe." });

        // microTech Lagrange Points
        Register(new StarmapObject { Id = "mic_l1", Name = "MIC-L1 Shallow Frontier", System = "Stanton", ParentId = "microtech", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 330, OrbitAngleDeg = 20, ColorHex = "#34D399", Size = 8, HasArmistice = true, Specialization = "⛏ Raffinerie & Reparatur", Description = "Wichtigste Raffinerie- und Verarbeitungsstation im microTech-Gebiet." });
        Register(new StarmapObject { Id = "mic_l2", Name = "MIC-L2 Long Forest", System = "Stanton", ParentId = "microtech", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 330, OrbitAngleDeg = 260, ColorHex = "#38BDF8", Size = 7, HasArmistice = true, Specialization = "📦 Fracht & Rest Stop", Description = "Frachtterminal am L2-Punkt von microTech." });
        Register(new StarmapObject { Id = "mic_l3", Name = "MIC-L3 Rest Stop", System = "Stanton", ParentId = "microtech", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 330, OrbitAngleDeg = 140, ColorHex = "#38BDF8", Size = 7, HasArmistice = true, Specialization = "🏪 Rest Stop", Description = "Abgelegene Versorgungsstation gegenüber von microTech." });
        Register(new StarmapObject { Id = "mic_l4", Name = "MIC-L4 Rest Stop", System = "Stanton", ParentId = "microtech", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 330, OrbitAngleDeg = 80, ColorHex = "#38BDF8", Size = 7, HasArmistice = true, Specialization = "🏪 Rest Stop", Description = "Versorgungsstation am microTech L4 Punkt." });
        Register(new StarmapObject { Id = "mic_l5", Name = "MIC-L5 Modern Icarus", System = "Stanton", ParentId = "microtech", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 330, OrbitAngleDeg = 200, ColorHex = "#38BDF8", Size = 7, HasArmistice = true, Specialization = "🏪 Rest Stop & Klinik", Description = "Rast- und Klinikstation am L5-Punkt von microTech." });

        // Inter-System Jump Gates
        Register(new StarmapObject { Id = "jp_pyro", Name = "Pyro Jump Point (Sprungtor)", System = "Stanton", ParentId = "stanton_star", Type = StarmapObjectType.JumpPoint, OrbitRadius = 390, OrbitAngleDeg = 80, ColorHex = "#F43F5E", Size = 11, TargetSystem = "Pyro", Description = "Interstellares Sprungtor direkt in das gesetzlose Pyro-System. 1-Klick Sprung möglich." });
        Register(new StarmapObject { Id = "jp_nyx", Name = "Nyx Jump Point (Sprungtor)", System = "Stanton", ParentId = "stanton_star", Type = StarmapObjectType.JumpPoint, OrbitRadius = 390, OrbitAngleDeg = 190, ColorHex = "#A855F7", Size = 10, TargetSystem = "Nyx", Description = "Sprungtor in das Nyx-System (Delamar & Levski)." });
        Register(new StarmapObject { Id = "jp_terra", Name = "Terra Gateway", System = "Stanton", ParentId = "stanton_star", Type = StarmapObjectType.JumpPoint, OrbitRadius = 390, OrbitAngleDeg = 290, ColorHex = "#34D399", Size = 9, Description = "Sprungtor zum UEE-Hauptweltsystem Terra." });
    }

    private static void InitPyro()
    {
        Register(new StarmapObject { Id = "pyro_star", Name = "Pyro (Flare-Stern)", System = "Pyro", Type = StarmapObjectType.Star, OrbitRadius = 0, OrbitAngleDeg = 0, ColorHex = "#EF4444", Size = 30, Description = "Instabiler Roter Zwerg / Flare-Stern mit tödlichen Sonneneruptionen und extremer Strahlung." });

        // 1. Pyro I
        Register(new StarmapObject { Id = "pyro1", Name = "Pyro I", System = "Pyro", ParentId = "pyro_star", Type = StarmapObjectType.Planet, OrbitRadius = 65, OrbitAngleDeg = 20, ColorHex = "#7F1D1D", Size = 10, SecurityLevel = "Lawless", Description = "Glühender Lavaplanet nah am Stern mit extremen Oberflächentemperaturen." });
        Register(new StarmapObject { Id = "p1_l1", Name = "P1-L1 Station", System = "Pyro", ParentId = "pyro1", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 65, OrbitAngleDeg = 80, ColorHex = "#F87171", Size = 7, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "🏪 Gesetzlose Raststation", Description = "Versorgungsstation im Orbit von Pyro I." });
        Register(new StarmapObject { Id = "p1_l2", Name = "P1-L2 Station", System = "Pyro", ParentId = "pyro1", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 65, OrbitAngleDeg = -40, ColorHex = "#F87171", Size = 7, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "⛽ Treibstoff-Depot", Description = "Raststation im L2-Punkt von Pyro I." });

        // 2. Monox (Pyro II)
        var monox = new StarmapObject { Id = "monox", Name = "Monox (Pyro II)", System = "Pyro", ParentId = "pyro_star", Type = StarmapObjectType.Planet, OrbitRadius = 120, OrbitAngleDeg = 95, ColorHex = "#D97706", Size = 15, SecurityLevel = "Lawless", Description = "Dürre Wüstenwelt. Hauptstützpunkt der Rough & Ready Gang." };
        Register(monox);
        Register(new StarmapObject { Id = "checkmate", Name = "Checkmate Station", System = "Pyro", ParentId = "monox", Type = StarmapObjectType.SpaceStation, OrbitRadius = 120, OrbitAngleDeg = 98, ColorHex = "#F59E0B", Size = 9, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "🏴‍☠️ Rough & Ready HQ · Schwarzmarkt", Description = "Hauptumschlagplatz für Schmuggel- und Schrottgut. Hauptquartier der Rough & Ready." });
        Register(new StarmapObject { Id = "sunset_mesa", Name = "Sunset Mesa", System = "Pyro", ParentId = "monox", Type = StarmapObjectType.Outpost, OrbitRadius = 120, OrbitAngleDeg = 92, ColorHex = "#EA580C", Size = 6, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "⛏️ Schrottplatz & Außenposten", Resources = "RMC, Titan, Gold", Description = "Berüchtigter Schrottplatz und Außenposten auf Monox." });
        Register(new StarmapObject { Id = "monox_fuel", Name = "Monox Fuel Depot", System = "Pyro", ParentId = "monox", Type = StarmapObjectType.Outpost, OrbitRadius = 120, OrbitAngleDeg = 102, ColorHex = "#FBBF24", Size = 5, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "⛽ Gesetzloser Treibstoff-Umschlag" });
        Register(new StarmapObject { Id = "p2_l1", Name = "P2-L1 Station", System = "Pyro", ParentId = "monox", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 120, OrbitAngleDeg = 155, ColorHex = "#F59E0B", Size = 7, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "🏪 Rest Stop" });
        Register(new StarmapObject { Id = "p2_l2", Name = "P2-L2 Station", System = "Pyro", ParentId = "monox", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 120, OrbitAngleDeg = 35, ColorHex = "#F59E0B", Size = 7, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "⛽ Treibstoff & Reparatur" });
        Register(new StarmapObject { Id = "p2_l4", Name = "P2-L4 Rest Stop", System = "Pyro", ParentId = "monox", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 120, OrbitAngleDeg = 215, ColorHex = "#F59E0B", Size = 7, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "🏪 Rest Stop" });
        Register(new StarmapObject { Id = "p2_l5", Name = "P2-L5 Rest Stop", System = "Pyro", ParentId = "monox", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 120, OrbitAngleDeg = -25, ColorHex = "#F59E0B", Size = 7, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "📦 Fracht & Schmuggel" });

        // 3. Bloom (Pyro III)
        var bloom = new StarmapObject { Id = "bloom", Name = "Bloom (Pyro III)", System = "Pyro", ParentId = "pyro_star", Type = StarmapObjectType.Planet, OrbitRadius = 180, OrbitAngleDeg = 170, ColorHex = "#EA580C", Size = 14, SecurityLevel = "Lawless", Description = "Tektonisch aktive Welt mit Starlight Station und Orbituary." };
        Register(bloom);
        Register(new StarmapObject { Id = "orbituary", Name = "Orbituary", System = "Pyro", ParentId = "bloom", Type = StarmapObjectType.SpaceStation, OrbitRadius = 180, OrbitAngleDeg = 165, ColorHex = "#FB923C", Size = 8, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "🏪 Gesetzlose Tank- & Waffenstation", Description = "Orbitale Raumbasis über Bloom mit Waffen- und Treibstoffhandel." });
        Register(new StarmapObject { Id = "starlight", Name = "Starlight Station", System = "Pyro", ParentId = "bloom", Type = StarmapObjectType.SpaceStation, OrbitRadius = 180, OrbitAngleDeg = 175, ColorHex = "#F97316", Size = 7.5, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "📦 Schmuggel & Schwarzmarkt", Description = "Handelsstation im Orbit von Bloom." });
        Register(new StarmapObject { Id = "sacrens_reach", Name = "Sacren's Reach", System = "Pyro", ParentId = "bloom", Type = StarmapObjectType.Outpost, OrbitRadius = 180, OrbitAngleDeg = 168, ColorHex = "#C2410C", Size = 5.5, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "⛏️ Bergbau-Außenposten", Resources = "Kupfer, Beryll, Quarz" });
        Register(new StarmapObject { Id = "p3_l1", Name = "P3-L1 Station", System = "Pyro", ParentId = "bloom", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 180, OrbitAngleDeg = 230, ColorHex = "#FB923C", Size = 7, HasArmistice = false, SecurityLevel = "Lawless" });
        Register(new StarmapObject { Id = "p3_l2", Name = "P3-L2 Station", System = "Pyro", ParentId = "bloom", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 180, OrbitAngleDeg = 110, ColorHex = "#FB923C", Size = 7, HasArmistice = false, SecurityLevel = "Lawless" });
        Register(new StarmapObject { Id = "p3_l4", Name = "P3-L4 Station", System = "Pyro", ParentId = "bloom", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 180, OrbitAngleDeg = 290, ColorHex = "#FB923C", Size = 7, HasArmistice = false, SecurityLevel = "Lawless" });
        Register(new StarmapObject { Id = "p3_l5", Name = "P3-L5 Station", System = "Pyro", ParentId = "bloom", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 180, OrbitAngleDeg = 50, ColorHex = "#FB923C", Size = 7, HasArmistice = false, SecurityLevel = "Lawless" });

        // 4. Pyro IV (Shattered Protoplanet)
        var pyro4 = new StarmapObject { Id = "pyro4", Name = "Pyro IV", System = "Pyro", ParentId = "pyro_star", Type = StarmapObjectType.Planet, OrbitRadius = 240, OrbitAngleDeg = 250, ColorHex = "#A8A29E", Size = 12, SecurityLevel = "Lawless", Resources = "RMC, Schiffswracks", Description = "Zerstörter Protoplanet in Kollisionsnähe mit Pyro V. Reiches Trümmerfeld." };
        Register(pyro4);
        Register(new StarmapObject { Id = "pyro4_junkyard", Name = "The Junkyard", System = "Pyro", ParentId = "pyro4", Type = StarmapObjectType.Outpost, OrbitRadius = 240, OrbitAngleDeg = 254, ColorHex = "#F59E0B", Size = 6, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "🧲 Wrackbergung & Salvage-Hotspot", Description = "Dichtes Trümmer- und Schrottfeld für Salvage-Operationen." });
        Register(new StarmapObject { Id = "p4_l1", Name = "P4-L1 Station", System = "Pyro", ParentId = "pyro4", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 240, OrbitAngleDeg = 310, ColorHex = "#A8A29E", Size = 7, HasArmistice = false, SecurityLevel = "Lawless" });
        Register(new StarmapObject { Id = "p4_l2", Name = "P4-L2 Station", System = "Pyro", ParentId = "pyro4", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 240, OrbitAngleDeg = 190, ColorHex = "#A8A29E", Size = 7, HasArmistice = false, SecurityLevel = "Lawless" });
        Register(new StarmapObject { Id = "p4_l4", Name = "P4-L4 Station", System = "Pyro", ParentId = "pyro4", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 240, OrbitAngleDeg = 10, ColorHex = "#A8A29E", Size = 7, HasArmistice = false, SecurityLevel = "Lawless" });
        Register(new StarmapObject { Id = "p4_l5", Name = "P4-L5 Station", System = "Pyro", ParentId = "pyro4", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 240, OrbitAngleDeg = 130, ColorHex = "#A8A29E", Size = 7, HasArmistice = false, SecurityLevel = "Lawless" });

        // 5. Pyro V (Gas Giant) & Monde (Ignis, Vatra, Adir, Fairo, FTransit, Vuur)
        var pyro5 = new StarmapObject { Id = "pyro5", Name = "Pyro V", System = "Pyro", ParentId = "pyro_star", Type = StarmapObjectType.Planet, OrbitRadius = 305, OrbitAngleDeg = 310, ColorHex = "#0284C7", Size = 23, SecurityLevel = "Lawless", Description = "Riesiger Gasriese mit 6 Monden und berüchtigten Piratenstationen wie Gaslight & Megiddo." };
        Register(pyro5);
        Register(new StarmapObject { Id = "ignis", Name = "Ignis (Pyro 5a)", System = "Pyro", ParentId = "pyro5", Type = StarmapObjectType.Moon, OrbitRadius = 320, OrbitAngleDeg = 298, ColorHex = "#DC2626", Size = 6, Resources = "Quantanium, Schwefel, Eisen", Description = "Glühender vulkanischer Mond von Pyro V." });
        Register(new StarmapObject { Id = "vatra", Name = "Vatra (Pyro 5b)", System = "Pyro", ParentId = "pyro5", Type = StarmapObjectType.Moon, OrbitRadius = 324, OrbitAngleDeg = 318, ColorHex = "#D97706", Size = 5.5, Resources = "Laranit, Beryll", Description = "Schwefel- und Aschemond." });
        Register(new StarmapObject { Id = "adir", Name = "Adir (Pyro 5c)", System = "Pyro", ParentId = "pyro5", Type = StarmapObjectType.Moon, OrbitRadius = 328, OrbitAngleDeg = 302, ColorHex = "#94A3B8", Size = 6, Resources = "Titan, Wolfram", Description = "Kraterreicher Felsmond von Pyro V." });
        Register(new StarmapObject { Id = "fairo", Name = "Fairo (Pyro 5d)", System = "Pyro", ParentId = "pyro5", Type = StarmapObjectType.Moon, OrbitRadius = 332, OrbitAngleDeg = 324, ColorHex = "#38BDF8", Size = 5.5, Resources = "Diamant, Eis, Corundum", Description = "Eismond mit gefrorenen Ozeanen." });
        Register(new StarmapObject { Id = "ftransit", Name = "FTransit (Pyro 5e)", System = "Pyro", ParentId = "pyro5", Type = StarmapObjectType.Moon, OrbitRadius = 336, OrbitAngleDeg = 294, ColorHex = "#E2E8F0", Size = 5, Resources = "Silizium", Description = "Schnell rotierender Felsmond." });
        Register(new StarmapObject { Id = "vuur", Name = "Vuur (Pyro 5f)", System = "Pyro", ParentId = "pyro5", Type = StarmapObjectType.Moon, OrbitRadius = 340, OrbitAngleDeg = 330, ColorHex = "#78350F", Size = 5.5, Resources = "Agricium, Gold", Description = "Äußerer Erzmond von Pyro V." });

        // Pyro V Stationen & Gaslight
        Register(new StarmapObject { Id = "gaslight", Name = "P5-L2 Gaslight Station", System = "Pyro", ParentId = "pyro5", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 305, OrbitAngleDeg = 250, ColorHex = "#F59E0B", Size = 8.5, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "⛏️ Gaslight Piraten-Raffinerie & Treibstoff", Description = "Legendäre Piraten-Raffinerie im L2-Lagrange-Punkt von Pyro V." });
        Register(new StarmapObject { Id = "megiddo", Name = "Megiddo Station", System = "Pyro", ParentId = "pyro5", Type = StarmapObjectType.SpaceStation, OrbitRadius = 305, OrbitAngleDeg = 314, ColorHex = "#EF4444", Size = 8, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "🏴‍☠️ Megiddo Piratenfestung", Description = "Schwer befestigte Piratenbasis im Pyro V Orbit." });
        Register(new StarmapObject { Id = "rats_nest", Name = "The Rat's Nest", System = "Pyro", ParentId = "pyro5", Type = StarmapObjectType.SpaceStation, OrbitRadius = 305, OrbitAngleDeg = 306, ColorHex = "#F97316", Size = 7.5, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "☠ Schwarzmarkt & Versteck" });
        Register(new StarmapObject { Id = "p5_l1", Name = "P5-L1 Station", System = "Pyro", ParentId = "pyro5", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 305, OrbitAngleDeg = 10, ColorHex = "#0284C7", Size = 7, HasArmistice = false, SecurityLevel = "Lawless" });
        Register(new StarmapObject { Id = "p5_l3", Name = "P5-L3 Station", System = "Pyro", ParentId = "pyro5", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 305, OrbitAngleDeg = 130, ColorHex = "#0284C7", Size = 7, HasArmistice = false, SecurityLevel = "Lawless" });
        Register(new StarmapObject { Id = "p5_l4", Name = "P5-L4 Station", System = "Pyro", ParentId = "pyro5", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 305, OrbitAngleDeg = 70, ColorHex = "#0284C7", Size = 7, HasArmistice = false, SecurityLevel = "Lawless" });
        Register(new StarmapObject { Id = "p5_l5", Name = "P5-L5 Station", System = "Pyro", ParentId = "pyro5", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 305, OrbitAngleDeg = 190, ColorHex = "#0284C7", Size = 7, HasArmistice = false, SecurityLevel = "Lawless" });

        // 6. Terminus (Pyro VI) & Ruin Station
        var terminus = new StarmapObject { Id = "terminus", Name = "Terminus (Pyro VI)", System = "Pyro", ParentId = "pyro_star", Type = StarmapObjectType.Planet, OrbitRadius = 365, OrbitAngleDeg = 40, ColorHex = "#475569", Size = 16, SecurityLevel = "Lawless", Description = "Äußerer Eisplanet. Heimat der berüchtigten Ruin Station." };
        Register(terminus);
        Register(new StarmapObject { Id = "ruinstation", Name = "Ruin Station", System = "Pyro", ParentId = "terminus", Type = StarmapObjectType.SpaceStation, OrbitRadius = 365, OrbitAngleDeg = 44, ColorHex = "#EF4444", Size = 10, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "☠ Piraten-Megastation & Schwarzmarkt", Description = "Zentrum der Piratenaktivität und größter Gesetzlosen-Umschlagplatz in Pyro (ehemalige Gold Horizon Station)." });
        Register(new StarmapObject { Id = "rustville", Name = "Rustville Settlement", System = "Pyro", ParentId = "terminus", Type = StarmapObjectType.Outpost, OrbitRadius = 365, OrbitAngleDeg = 37, ColorHex = "#F59E0B", Size = 5.5, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "🏚️ Gesetzlose Kolonie & Handel" });
        Register(new StarmapObject { Id = "p6_l1", Name = "P6-L1 Station", System = "Pyro", ParentId = "terminus", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 365, OrbitAngleDeg = 100, ColorHex = "#475569", Size = 7, HasArmistice = false, SecurityLevel = "Lawless" });
        Register(new StarmapObject { Id = "p6_l2", Name = "P6-L2 Station", System = "Pyro", ParentId = "terminus", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 365, OrbitAngleDeg = -20, ColorHex = "#475569", Size = 7, HasArmistice = false, SecurityLevel = "Lawless" });
        Register(new StarmapObject { Id = "p6_l3", Name = "P6-L3 Station", System = "Pyro", ParentId = "terminus", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 365, OrbitAngleDeg = 220, ColorHex = "#475569", Size = 7, HasArmistice = false, SecurityLevel = "Lawless" });
        Register(new StarmapObject { Id = "p6_l4", Name = "P6-L4 Station", System = "Pyro", ParentId = "terminus", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 365, OrbitAngleDeg = 160, ColorHex = "#475569", Size = 7, HasArmistice = false, SecurityLevel = "Lawless" });
        Register(new StarmapObject { Id = "p6_l5", Name = "P6-L5 Station", System = "Pyro", ParentId = "terminus", Type = StarmapObjectType.LagrangeStation, OrbitRadius = 365, OrbitAngleDeg = -80, ColorHex = "#475569", Size = 7, HasArmistice = false, SecurityLevel = "Lawless" });

        // Sprungtore in Pyro
        Register(new StarmapObject { Id = "jp_stanton_from_pyro", Name = "Stanton Jump Point (Sprungtor)", System = "Pyro", ParentId = "pyro_star", Type = StarmapObjectType.JumpPoint, OrbitRadius = 395, OrbitAngleDeg = 260, ColorHex = "#38BDF8", Size = 11, TargetSystem = "Stanton", Description = "Sprungtor zurück in das sichere Stanton-System." });
        Register(new StarmapObject { Id = "jp_nyx_from_pyro", Name = "Nyx Jump Point (Sprungtor)", System = "Pyro", ParentId = "pyro_star", Type = StarmapObjectType.JumpPoint, OrbitRadius = 395, OrbitAngleDeg = 140, ColorHex = "#A855F7", Size = 10, TargetSystem = "Nyx", Description = "Sprungtor in das Nyx-System." });
        Register(new StarmapObject { Id = "jp_terra_from_pyro", Name = "Terra Jump Point (Sprungtor)", System = "Pyro", ParentId = "pyro_star", Type = StarmapObjectType.JumpPoint, OrbitRadius = 395, OrbitAngleDeg = 30, ColorHex = "#34D399", Size = 9, Description = "Sprungtor zum Terra-System." });
    }

    private static void InitNyx()
    {
        Register(new StarmapObject { Id = "nyx_star", Name = "Nyx (Stern)", System = "Nyx", Type = StarmapObjectType.Star, OrbitRadius = 0, OrbitAngleDeg = 0, ColorHex = "#60A5FA", Size = 26, Description = "Blau-Weißer Hauptreihenstern (F-Klasse)." });

        // 1. Nyx I
        Register(new StarmapObject { Id = "nyx1", Name = "Nyx I", System = "Nyx", ParentId = "nyx_star", Type = StarmapObjectType.Planet, OrbitRadius = 85, OrbitAngleDeg = 60, ColorHex = "#94A3B8", Size = 11, Description = "Felsiger Kernplanet in Sternennähe mit Bergbau-Stollen." });
        Register(new StarmapObject { Id = "nyx1_mine", Name = "Nyx I Surface Mine", System = "Nyx", ParentId = "nyx1", Type = StarmapObjectType.Outpost, OrbitRadius = 85, OrbitAngleDeg = 64, ColorHex = "#CBD5E1", Size = 5, Specialization = "⛏️ Erz-Abbau & Rohstoffe", Resources = "Titan, Wolfram, Eisen" });

        // 2. Delamar & Levski (im Asteroidengürtel Glaciem Ring)
        var delamar = new StarmapObject { Id = "delamar", Name = "Delamar", System = "Nyx", ParentId = "nyx_star", Type = StarmapObjectType.Moon, OrbitRadius = 175, OrbitAngleDeg = 150, ColorHex = "#64748B", Size = 16, SecurityLevel = "Medium", Jurisdiction = "People's Alliance", Resources = "Beryll, Wolfram, RMC", Description = "Großer Planetoid im Asteroidengürtel (Glaciem Ring). Sitz der freien Kolonie Levski." };
        Register(delamar);
        Register(new StarmapObject { Id = "levski", Name = "Levski", System = "Nyx", ParentId = "delamar", Type = StarmapObjectType.LandingZone, OrbitRadius = 175, OrbitAngleDeg = 154, ColorHex = "#38BDF8", Size = 10, HasArmistice = true, SecurityLevel = "Medium", Jurisdiction = "People's Alliance", Specialization = "⛏ Bergbaustadt · 🏴 Freie Handelszone & Grand Barter", Description = "Freie Bergbaustadt im Krater von Delamar. Gegründet von der People's Alliance (Recco Battaglia, Grand Barter, Cafe Musain)." });
        Register(new StarmapObject { Id = "theta_station", Name = "People's Service Station Theta", System = "Nyx", ParentId = "delamar", Type = StarmapObjectType.SpaceStation, OrbitRadius = 175, OrbitAngleDeg = 144, ColorHex = "#38BDF8", Size = 7.5, HasArmistice = true, SecurityLevel = "Medium", Specialization = "⛽ People's Alliance Versorgungsstation", Description = "Versorgungs- und Reparaturdock der People's Alliance im Delamar-Orbit." });
        Register(new StarmapObject { Id = "breaker_267", Name = "QV Breaker Station BRK-267", System = "Nyx", ParentId = "delamar", Type = StarmapObjectType.SpaceStation, OrbitRadius = 175, OrbitAngleDeg = 158, ColorHex = "#FB923C", Size = 7.5, HasArmistice = false, SecurityLevel = "Low", Specialization = "🧲 Breaker & Schrottverwertung", Description = "Schrott- und Erzbrecher-Station im Delamar-Asteroidenring." });
        Register(new StarmapObject { Id = "bore_hole_4", Name = "Bore Hole Site 4", System = "Nyx", ParentId = "delamar", Type = StarmapObjectType.Outpost, OrbitRadius = 175, OrbitAngleDeg = 148, ColorHex = "#94A3B8", Size = 5, Specialization = "⛏️ Tiefbohrung & Minenanlage", Resources = "Wolfram, Quarz" });
        Register(new StarmapObject { Id = "miners_rest", Name = "Miner's Rest", System = "Nyx", ParentId = "delamar", Type = StarmapObjectType.Outpost, OrbitRadius = 175, OrbitAngleDeg = 152, ColorHex = "#E2E8F0", Size = 5, Specialization = "🏪 Minen-Zwischenstopp & Barter" });

        // 3. Keeger Belt
        var keegerBelt = new StarmapObject { Id = "keeger_belt", Name = "Keeger Belt (Asteroidengürtel)", System = "Nyx", ParentId = "nyx_star", Type = StarmapObjectType.Planet, OrbitRadius = 235, OrbitAngleDeg = 90, ColorHex = "#78716C", Size = 12, SecurityLevel = "Lawless", Resources = "Beryll, Titan, Schrott", Description = "Dichter Asteroidengürtel mit Piratenverstecken und Schmuggelbasen." };
        Register(keegerBelt);
        Register(new StarmapObject { Id = "moraine_base", Name = "Moraine Base (Keeger Belt)", System = "Nyx", ParentId = "keeger_belt", Type = StarmapObjectType.SpaceStation, OrbitRadius = 235, OrbitAngleDeg = 85, ColorHex = "#EF4444", Size = 8, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "☠ Gesetzlose Basis im Keeger-Gürtel", Description = "Versteckte Piraten- und Schmuggelbasis tief im Keeger-Asteroidenfeld." });
        Register(new StarmapObject { Id = "keeger_depot", Name = "Keeger Asteroid Depot", System = "Nyx", ParentId = "keeger_belt", Type = StarmapObjectType.Outpost, OrbitRadius = 235, OrbitAngleDeg = 95, ColorHex = "#F59E0B", Size = 5.5, HasArmistice = false, SecurityLevel = "Lawless", Specialization = "📦 Schmuggel-Depot & Erzspeicher" });

        // 4. Glaciem Ring
        var glaciemRing = new StarmapObject { Id = "glaciem_ring", Name = "Glaciem Ring (Eisgürtel)", System = "Nyx", ParentId = "nyx_star", Type = StarmapObjectType.Planet, OrbitRadius = 285, OrbitAngleDeg = 300, ColorHex = "#93C5FD", Size = 13, Resources = "Reines Eis, Quantanium, Diamant", Description = "Riesiger äußerer Eis- und Asteroidenring um das Nyx-System." };
        Register(glaciemRing);
        Register(new StarmapObject { Id = "glaciem_rockcracker", Name = "Glaciem RockCracker 12", System = "Nyx", ParentId = "glaciem_ring", Type = StarmapObjectType.SpaceStation, OrbitRadius = 285, OrbitAngleDeg = 305, ColorHex = "#38BDF8", Size = 7.5, Specialization = "⛏️ Eis- und Erzabbau-Station" });
        Register(new StarmapObject { Id = "glaciem_outpost", Name = "Frostfall Outpost", System = "Nyx", ParentId = "glaciem_ring", Type = StarmapObjectType.Outpost, OrbitRadius = 285, OrbitAngleDeg = 295, ColorHex = "#E0F2FE", Size = 5, Specialization = "📦 Forschungs- & Schürfposten" });

        // 5. Nyx III (Eisgasriese)
        Register(new StarmapObject { Id = "nyx3", Name = "Nyx III", System = "Nyx", ParentId = "nyx_star", Type = StarmapObjectType.Planet, OrbitRadius = 335, OrbitAngleDeg = 230, ColorHex = "#0369A1", Size = 18, Description = "Eisiger Gasriese im äußeren Nyx-System." });
        Register(new StarmapObject { Id = "nyx3_station", Name = "Nyx III Sensor Array", System = "Nyx", ParentId = "nyx3", Type = StarmapObjectType.SpaceStation, OrbitRadius = 335, OrbitAngleDeg = 234, ColorHex = "#38BDF8", Size = 7, Specialization = "📡 Deep-Space Sensorstation" });

        // Sprungtore in Nyx
        Register(new StarmapObject { Id = "jp_stanton_from_nyx", Name = "Stanton Jump Point (Sprungtor)", System = "Nyx", ParentId = "nyx_star", Type = StarmapObjectType.JumpPoint, OrbitRadius = 390, OrbitAngleDeg = 200, ColorHex = "#38BDF8", Size = 10, TargetSystem = "Stanton", Description = "Sprungtor in das Stanton-System (Stanton Gateway Station)." });
        Register(new StarmapObject { Id = "jp_pyro_from_nyx", Name = "Pyro Jump Point (Sprungtor)", System = "Nyx", ParentId = "nyx_star", Type = StarmapObjectType.JumpPoint, OrbitRadius = 390, OrbitAngleDeg = 320, ColorHex = "#F97316", Size = 10, TargetSystem = "Pyro", Description = "Sprungtor in das Pyro-System (Pyro Gateway Station)." });
        Register(new StarmapObject { Id = "jp_castra", Name = "Castra Gateway", System = "Nyx", ParentId = "nyx_star", Type = StarmapObjectType.JumpPoint, OrbitRadius = 390, OrbitAngleDeg = 45, ColorHex = "#10B981", Size = 9, Description = "Sprungtor in das UEE-Grenzsystem Castra." });
        Register(new StarmapObject { Id = "jp_tohil", Name = "Tohil Gateway", System = "Nyx", ParentId = "nyx_star", Type = StarmapObjectType.JumpPoint, OrbitRadius = 390, OrbitAngleDeg = 110, ColorHex = "#6366F1", Size = 9, Description = "Sprungtor in das Schmugglersystem Tohil." });
    }
}
