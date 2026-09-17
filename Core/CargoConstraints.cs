using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace SCLogMate.Core;

public sealed record LoadingDockDef(
    [property: JsonPropertyName("starmapId")] string StarmapId,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("system")] string System,
    [property: JsonPropertyName("body")] string Body,
    [property: JsonPropertyName("hasExternalCollar")] bool HasExternalCollar = true,
    [property: JsonPropertyName("supportsAutoLoad")] bool SupportsAutoLoad = true
);

public sealed record CargoShipDef(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("manufacturer")] string Manufacturer,
    [property: JsonPropertyName("totalScu")] int TotalScu,
    [property: JsonPropertyName("maxContainerScu")] int MaxContainerScu,
    [property: JsonPropertyName("padSize")] string PadSize, // XS, S, M, L, XL, Capital
    [property: JsonPropertyName("requiresDockingCollar")] bool RequiresDockingCollar,
    [property: JsonPropertyName("canLandPlanetside")] bool CanLandPlanetside,
    [property: JsonPropertyName("cargoAccessType")] string CargoAccessType,
    [property: JsonPropertyName("notes")] string Notes
);

public sealed record ShipConstraintEvaluationResult(
    [property: JsonPropertyName("isCompatible")] bool IsCompatible,
    [property: JsonPropertyName("originHasDock")] bool OriginHasDock,
    [property: JsonPropertyName("destinationHasDock")] bool DestinationHasDock,
    [property: JsonPropertyName("shipFound")] bool ShipFound,
    [property: JsonPropertyName("ship")] CargoShipDef? Ship,
    [property: JsonPropertyName("warnings")] IReadOnlyList<string> Warnings,
    [property: JsonPropertyName("notices")] IReadOnlyList<string> Notices
);

/// <summary>
/// Das Register aller 28 verifizierten Loading-Dock-Stationen (NexusApp / DataCore Referenz)
/// und Cargo-Schiff-Restriktionen (Hangar-Größen, Docking-Collars, Max-Container-Größen).
/// </summary>
public static class CargoConstraints
{
    #region 28 Loading Docks

    public static readonly IReadOnlyList<LoadingDockDef> LoadingDocks = new List<LoadingDockDef>
    {
        // Jump Points
        new("StarMapObject.JumpPoint_Stanton_Pyro", "rr_jp_stantonpyro", "Stanton – Pyro Gateway", "Stanton", "Jump Point", true, true),
        new("StarMapObject.JumpPoint_Pyro_Stanton", "rr_jp_pyrostanton", "Pyro – Stanton Gateway", "Pyro", "Jump Point", true, true),
        new("StarMapObject.JumpPoint_Stanton_Terra", "rr_jp_stantonterra", "Stanton – Terra Gateway", "Stanton", "Jump Point", true, true),
        new("StarMapObject.JumpPoint_Pyro_Nyx", "rr_jp_pyronyx", "Pyro – Nyx Gateway", "Pyro", "Jump Point", true, true),
        new("StarMapObject.JumpPoint_Nyx_Pyro", "rr_jp_nyxpyro", "Nyx – Pyro Gateway", "Nyx", "Jump Point", true, true),
        new("StarMapObject.RR_JP_StantonMagnus", "rr_jp_stantonmagnus", "Stanton – Magnus Gateway", "Stanton", "Jump Point", true, true),
        new("StarMapObject.RR_JP_NyxCastra", "rr_jp_nyxcastra", "Nyx – Castra Gateway", "Nyx", "Jump Point", true, true),

        // Stanton Planetary LEO Stations
        new("StarMapObject.RR_ARC_LEO", "rr_arc_leo", "Baijini Point", "Stanton", "ArcCorp", true, true),
        new("StarMapObject.RR_CRU_LEO", "rr_cru_leo", "Seraphim Station", "Stanton", "Crusader", true, true),
        new("StarMapObject.RR_HUR_LEO", "rr_hur_leo", "Everus Harbor", "Stanton", "Hurston", true, true),
        new("StarMapObject.RR_MIC_LEO", "rr_mic_leo", "Port Tressler", "Stanton", "microTech", true, true),

        // Pyro Planetary LEO & Rest Stop Docks
        new("StarMapObject.RR_P2_LEO", "rr_p2_leo", "Monox Station (LEO)", "Pyro", "Monox", true, true),
        new("StarMapObject.RR_P3_LEO", "rr_p3_leo", "Bloom Station (LEO)", "Pyro", "Bloom", true, true),
        new("StarMapObject.RR_P6_LEO", "rr_p6_leo", "Terminus Station (LEO)", "Pyro", "Terminus", true, true),

        // Pyro Lagrange Rest Stops
        new("StarMapObject.RR_P1_L2", "rr_p1_l2", "Pyro I L2 Rest Stop", "Pyro", "Pyro I", true, true),
        new("StarMapObject.RR_P1_L3", "rr_p1_l3", "Pyro I L3 Rest Stop", "Pyro", "Pyro I", true, true),
        new("StarMapObject.RR_P1_L4", "rr_p1_l4", "Pyro I L4 Rest Stop", "Pyro", "Pyro I", true, true),
        new("StarMapObject.RR_P1_L5", "rr_p1_l5", "Pyro I L5 Rest Stop", "Pyro", "Pyro I", true, true),
        new("StarMapObject.RR_P2_L3", "rr_p2_l3", "Monox L3 Rest Stop", "Pyro", "Monox", true, true),
        new("StarMapObject.RR_P2_L4", "rr_p2_l4", "Monox L4 Rest Stop", "Pyro", "Monox", true, true),
        new("StarMapObject.RR_P3_L2", "rr_p3_l2", "Bloom L2 Rest Stop", "Pyro", "Bloom", true, true),
        new("StarMapObject.RR_P3_L5", "rr_p3_l5", "Bloom L5 Rest Stop", "Pyro", "Bloom", true, true),
        new("StarMapObject.RR_P5_L1", "rr_p5_l1", "Pyro V L1 Rest Stop", "Pyro", "Pyro V", true, true),
        new("StarMapObject.RR_P5_L2", "rr_p5_l2", "Pyro V L2 Rest Stop", "Pyro", "Pyro V", true, true),
        new("StarMapObject.RR_P5_L3", "rr_p5_l3", "Pyro V L3 Rest Stop", "Pyro", "Pyro V", true, true),
        new("StarMapObject.RR_P6_L1", "rr_p6_l1", "Terminus L1 Rest Stop", "Pyro", "Terminus", true, true),
        new("StarMapObject.RR_P6_L2", "rr_p6_l2", "Terminus L2 Rest Stop", "Pyro", "Terminus", true, true),
        new("StarMapObject.RR_P6_L5", "rr_p6_l5", "Terminus L5 Rest Stop", "Pyro", "Terminus", true, true)
    };

    private static readonly HashSet<string> DockLookupTokens = new(StringComparer.OrdinalIgnoreCase);

    static CargoConstraints()
    {
        foreach (var dock in LoadingDocks)
        {
            DockLookupTokens.Add(dock.StarmapId);
            DockLookupTokens.Add(dock.Code);
            DockLookupTokens.Add(dock.DisplayName);

            // Schlüsselwörter für Namenserkennung
            if (dock.DisplayName.Contains("Baijini", StringComparison.OrdinalIgnoreCase)) DockLookupTokens.Add("Baijini");
            if (dock.DisplayName.Contains("Seraphim", StringComparison.OrdinalIgnoreCase)) DockLookupTokens.Add("Seraphim");
            if (dock.DisplayName.Contains("Everus", StringComparison.OrdinalIgnoreCase)) DockLookupTokens.Add("Everus");
            if (dock.DisplayName.Contains("Tressler", StringComparison.OrdinalIgnoreCase)) DockLookupTokens.Add("Tressler");
        }
    }

    /// <summary>
    /// Prüft, ob ein Standort über einen ausgewiesenen großen Loading Dock mit Docking-Armen
    /// und Fracht-Aufzügen verfügt (wichtig für Hull C und Auto-Load).
    /// </summary>
    public static bool HasLoadingDock(string? locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName)) return false;
        var s = locationName.Trim();

        // Direkte Erkennung
        if (DockLookupTokens.Contains(s)) return true;

        // Teilstring-Match für Stationen
        if (s.Contains("Everus Harbor", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("Port Tressler", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("Baijini Point", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("Seraphim Station", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("Gateway", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("Jump Point", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("Loading Dock", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Lagrange-Prüfung (z. B. "HUR-L1", "CRU-L1", "P1-L2", "P6-L1")
        if (s.Contains("-L", StringComparison.OrdinalIgnoreCase) &&
            (s.Contains("Rest Stop", StringComparison.OrdinalIgnoreCase) || s.Contains("Station", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    #endregion

    #region Cargo Ship Catalog

    public static readonly IReadOnlyList<CargoShipDef> Ships = new List<CargoShipDef>
    {
        // 32 SCU Clearances (Große Frachter)
        new("misc-hull-c", "Hull C", "MISC", 4608, 32, "Capital", true, false, "Externes Spindel-Grid", "Erfordert zwingend Station Docking Collar; keine Planetenlandung im beladenen Zustand"),
        new("crus-c2-hercules", "C2 Hercules", "Crusader Industries", 696, 32, "L", false, true, "Front- & Heckrampe (Drive-Through)", "Nimmt alle Containergrößen von 1 bis 32 SCU auf"),
        new("drak-caterpillar", "Caterpillar", "Drake Interplanetary", 576, 32, "L", false, true, "4x Seiten-Frachttore & Frontrampe", "Modulares Frachtdeck; volle 32-SCU-Fähigkeit"),
        new("crus-m2-hercules", "M2 Hercules", "Crusader Industries", 522, 32, "L", false, true, "Front- & Heckrampe", "Gepanzerter Frachter mit voller 32-SCU-Unterstützung"),
        new("rsi-constellation-taurus", "Constellation Taurus", "Roberts Space Industries", 174, 32, "M", false, true, "Großer Frachtlift & Heck-Schmuggelfach", "Nimmt bis zu 32 SCU Crate im Hauptlift auf"),
        new("misc-freelancer-max", "Freelancer MAX", "MISC", 120, 32, "M", false, true, "Breite Heckrampe", "Verbreitertes Frachtdeck; akzeptiert bis zu 32 SCU Boxen"),
        new("rsi-zeus-mk-ii-cl", "Zeus Mk II CL", "Roberts Space Industries", 128, 32, "M", false, true, "Heckrampe & Frachtlift", "Dedizierter Medium-Frachter mit Traktorstrahl"),
        new("argo-raft", "RAFT", "Argo Astronautics", 96, 32, "M", false, true, "3x Externe Frachtcontainer-Klemmen", "Nimmt 3x standardisierte 32 SCU CCT-Container auf"),
        new("misc-hull-a", "Hull A", "MISC", 64, 32, "M", false, true, "Externe Ausfahr-Spindeln", "Kompakter Frachter mit externen Klemmen bis 32 SCU"),

        // 24 SCU Clearance
        new("orig-400i", "400i", "Origin Jumpworks", 42, 24, "M", false, true, "Bauch-Frachtlift", "Maximal 24 SCU Containerhöhe im Bauchaufzug"),

        // 16 SCU Clearance (Medium-Frachter & Allrounder)
        new("rsi-constellation-andromeda", "Constellation Andromeda", "Roberts Space Industries", 96, 16, "M", false, true, "Bauchlift", "Maximal 16 SCU Container wegen Deckenhöhe des Aufzugs"),
        new("drak-corsair", "Corsair", "Drake Interplanetary", 72, 16, "M", false, true, "Heckrampe", "Breite Heckrampe für Fahrzeuge und bis zu 16 SCU Kisten"),
        new("misc-freelancer", "Freelancer Base", "MISC", 66, 16, "M", false, true, "Heckrampe & Luftschleuse", "Standard-Heckrampe fasst bis zu 16 SCU"),
        new("crus-c1-spirit", "C1 Spirit", "Crusader Industries", 64, 16, "M", false, true, "Lange Heckrampe", "Nimmt Container bis zu 16 SCU auf; Hecktraktorstrahl"),
        new("drak-cutlass-black", "Cutlass Black", "Drake Interplanetary", 46, 16, "M", false, true, "Heckrampe & 2x Seitentüren", "Maximal 16 SCU Kistenhöhe am Deckenüberhang"),
        new("orig-600i-touring", "600i Explorer", "Origin Jumpworks", 40, 16, "L", false, true, "Fracht-Lift", "Luxus-Lift für Fracht bis 16 SCU"),

        // 8 SCU Clearance (Kompakte Frachter & Transporter)
        new("cnou-nomad", "Nomad", "Consolidated Outland", 24, 8, "S", false, true, "Offenes Pickup-Frachtbett (Hover)", "Offenes Heckdeck; optimal für 1, 2, 4 und 8 SCU CCTs"),
        new("aegs-avenger-titan", "Avenger Titan", "Aegis Dynamics", 8, 8, "S", false, true, "Heckrampe", "Sehr beliebter Starter-Frachter für bis zu 8 SCU"),
        new("drak-clipper", "Clipper", "Drake Interplanetary", 12, 8, "S", false, true, "Heckrampe", "Leichter Starter-Frachter"),

        // 4 SCU Clearance
        new("orig-315p", "315p Explorer", "Origin Jumpworks", 12, 4, "S", false, true, "2x Externe Bauchklappen", "Getrennte Ladebuchten; maximal 4 SCU Boxen"),
        new("orig-300i", "300i", "Origin Jumpworks", 8, 4, "S", false, true, "Externe Bauchklappe", "Kompakter Lift für Kisten bis 4 SCU"),
        new("drak-cutter", "Cutter", "Drake Interplanetary", 4, 4, "S", false, true, "Heckrampe", "Kompakter Laderaum für 1x 4 SCU oder 4x 1 SCU"),

        // 2 SCU Clearance (Snubs & Klein-Transporter)
        new("anvl-c8x-pisces", "C8X Pisces Expedition", "Anvil Aerospace", 4, 2, "XS", false, true, "Heckrampe", "Begehbarer Innenraum für maximal 2 SCU Kisten"),
        new("orig-135c", "135c", "Origin Jumpworks", 4, 2, "XS", false, true, "Heck-Frachtluke", "Separater Frachtraum für bis zu 2 SCU"),
        new("rsi-aurora-cl", "Aurora CL", "Roberts Space Industries", 6, 2, "XS", false, true, "Externer Bauch-Frachtcontainer", "Externe Befestigung; optimal für kleine Kisten")
    };

    public static CargoShipDef? FindShip(string? shipIdOrName)
    {
        if (string.IsNullOrWhiteSpace(shipIdOrName)) return null;
        var s = shipIdOrName.Trim();

        return Ships.FirstOrDefault(x =>
            string.Equals(x.Id, s, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.Name, s, StringComparison.OrdinalIgnoreCase) ||
            x.Name.Contains(s, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Evaluation

    /// <summary>
    /// Bewertet, ob ein Schiff für eine bestimmte Frachtroute geeignet ist,
    /// und meldet Restriktionen bzgl. Docking-Collar, Auto-Load, Containergröße und Pad-Größe.
    /// </summary>
    public static ShipConstraintEvaluationResult EvaluateRoute(
        string? shipIdOrName,
        string originTerminal,
        string destinationTerminal,
        int requiredScu = 0,
        int containerScu = 0)
    {
        var ship = FindShip(shipIdOrName);
        bool originHasDock = HasLoadingDock(originTerminal);
        bool destHasDock = HasLoadingDock(destinationTerminal);

        var warnings = new List<string>();
        var notices = new List<string>();

        if (ship == null)
        {
            return new ShipConstraintEvaluationResult(
                IsCompatible: true,
                OriginHasDock: originHasDock,
                DestinationHasDock: destHasDock,
                ShipFound: false,
                Ship: null,
                Warnings: warnings,
                Notices: notices
            );
        }

        bool isCompatible = true;

        // 1. Hull C / Spindel Docking-Collar Check
        if (ship.RequiresDockingCollar)
        {
            if (!originHasDock)
            {
                isCompatible = false;
                warnings.Add($"Docking-Fehler: {ship.Name} erfordert einen Station Docking Collar. Startort '{originTerminal}' bietet kein passendes Loading Dock!");
            }
            if (!destHasDock)
            {
                isCompatible = false;
                warnings.Add($"Docking-Fehler: {ship.Name} erfordert einen Station Docking Collar. Zielort '{destinationTerminal}' bietet kein passendes Loading Dock!");
            }
        }

        // 2. Frachtkapazitäts-Check
        if (requiredScu > 0 && requiredScu > ship.TotalScu)
        {
            isCompatible = false;
            warnings.Add($"Überladung: Frachtvolumen ({requiredScu:N0} SCU) überschreitet Schiffskapazität von {ship.Name} ({ship.TotalScu:N0} SCU).");
        }

        // 3. Container-Tür-/Spindel-Clearance
        if (containerScu > 0 && containerScu > ship.MaxContainerScu)
        {
            isCompatible = false;
            warnings.Add($"Container-Größe nicht ladbar: {containerScu} SCU Kiste passt nicht durch Luken/Tore von {ship.Name} (Max: {ship.MaxContainerScu} SCU).");
        }

        // 4. Auto-Load Hinweise
        if (!originHasDock || !destHasDock)
        {
            notices.Add("Hinweis: An Außenposten / Nicht-Dock-Stationen ist kein automatischer Frachtaufzug (Auto-Load) verfügbar. Manuelle Verladung erforderlich.");
        }
        else
        {
            notices.Add("Optimal: Beide Stationen verfügen über verifizierte Loading Docks mit Auto-Load-Unterstützung.");
        }

        // 5. Pad-Größe bei großen Schiffen
        if ((ship.PadSize == "L" || ship.PadSize == "XL" || ship.PadSize == "Capital") &&
            (!originTerminal.Contains("Station", StringComparison.OrdinalIgnoreCase) && !originTerminal.Contains("Harbor", StringComparison.OrdinalIgnoreCase) && !originTerminal.Contains("Tressler", StringComparison.OrdinalIgnoreCase) && !originTerminal.Contains("Point", StringComparison.OrdinalIgnoreCase)))
        {
            notices.Add($"Großes Schiff ({ship.PadSize}): Bitte Pad- bzw. Hangarverfügbarkeit vor Ort beachten.");
        }

        return new ShipConstraintEvaluationResult(
            IsCompatible: isCompatible,
            OriginHasDock: originHasDock,
            DestinationHasDock: destHasDock,
            ShipFound: true,
            Ship: ship,
            Warnings: warnings,
            Notices: notices
        );
    }

    #endregion
}
