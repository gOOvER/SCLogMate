namespace SCLogMate.Core;

/// <summary>
/// Leitet aus den rohen Missions-Feldern der Log-Zeile
/// <c>&lt;CLocalMissionPhaseMarker::CreateMarker&gt;</c> die brauchbaren Kategorien ab:
/// Auftraggeber/Fraktion, Auftrags-Typ, Schwierigkeit und System.
/// Quelle: <c>generator name [RedWind_RecoverCargo]</c> + <c>contract [RedWind_Stanton_Hard_RecoverCargo]</c>.
/// </summary>
public static class Missions
{
    public readonly record struct Info(string Faction, string Type, string Difficulty, string System);

    public static Info Derive(string generator, string contract)
    {
        var faction = Faction(generator, contract);
        var both = generator + "_" + contract;
        return new Info(faction, Type(both), Difficulty(contract), SystemOf(contract));
    }

    /// <summary>Anzeige-/Speicherform: "RedWind · Fracht/Bergung · Schwer · Stanton".</summary>
    public static string Format(in Info i) => $"{i.Faction} · {i.Type} · {i.Difficulty} · {i.System}";

    /// <summary>Fraktion = Präfix vor dem ersten Unterstrich (bzw. der ganze Name oder Generator-Zuordnung).</summary>
    static string Faction(string generator, string contract = "")
    {
        var g = generator.Trim();
        var us = g.IndexOf('_');
        var raw = us > 0 ? g[..us] : g;

        // Spezifische Behandlung für dynamische Missionsgeneratoren (CIG Engine Strings)
        if (raw.Equals("TheBackpocket", System.StringComparison.OrdinalIgnoreCase))
        {
            if (Has(contract, "ORS_") || Has(contract, "Orison")) return "Orison Relief Services";
            if (Has(contract, "RoX_")) return "People's Alliance";
            if (Has(contract, "HaulCargo_") || Has(contract, "RedWind")) return "Red Wind Line";
            if (Has(contract, "Covalex")) return "Covalex Shipping";
            if (Has(contract, "Ling")) return "Ling Family";
            return "Unbekannt";
        }

        if (raw.Equals("CleanAir", System.StringComparison.OrdinalIgnoreCase))
        {
            return "Civilian Defense";
        }

        return raw switch
        {
            "Battaglia" => "Recco Battaglia",
            "Eckhart" => "Miles Eckhart",
            "Darneely" => "Clovus Darneely",
            "Hurston" => "Constantine Hurston",
            "Pacheco" => "Tecia Pacheco",
            "Twitch" => "Tecia Pacheco",
            "Klim" => "Wallace Klim",
            "RedWind" => "Red Wind Line",
            "NorthRock" => "Northrock Service Group",
            "LingBiotechnology" => "Ling Biotechnology",
            "LingFamilyHauling" => "Ling Family",
            "LingFamily" => "Ling Family",
            "MicroTechLogistics" => "microTech Logistics",
            "CrusaderIndustries" => "Crusader Industries",
            "HurstonDynamics" => "Hurston Dynamics",
            "ArcCorp" => "ArcCorp",
            "BountyHuntersGuild" => "Bounty Hunters Guild",
            "InterSec" => "InterSec Security",
            "Adagio" => "Adagio Holdings",
            "Covalex" => "Covalex Shipping",
            "UnitedCargo" => "United Cargo",
            "Foxcor" => "Foxcor",
            "Dsl" => "Delamar Space Lines",
            "PeoplesAlliance" => "People's Alliance",
            "Headhunters" => "Headhunters",
            "RoughAnimals" => "Rough Animals",
            "CitizensForProsperity" => "Citizens for Prosperity",
            _ => raw
        };
    }

    static string Type(string s)
    {
        if (Has(s, "ORS_MA")) return "Lieferung";
        if (Has(s, "ORS_CA")) return "Fracht/Transport";
        if (Has(s, "HaulCargo") || Has(s, "Hauling") || Has(s, "RecoverCargo")) return "Fracht/Bergung";
        if (Has(s, "FacilityDelve")) return "Facility Delve";
        if (Has(s, "Certification")) return "Zertifizierung";
        if (Has(s, "Assassinat") || Has(s, "Eliminate") || Has(s, "KillShip") || Has(s, "HeadHunt") || Has(s, "ShipWaveAttack") || Has(s, "BoardShip")) return "Kampf/Kill";
        if (Has(s, "Patrol") || Has(s, "Defend")) return "Patrouille/Verteidigung";
        if (Has(s, "RecoverData") || Has(s, "DataDownload") || Has(s, "BlackBox") || Has(s, "Uplink") || Has(s, "DataDrive") || Has(s, "Data")) return "Daten";
        if (Has(s, "MissingPerson") || Has(s, "MissingPersons") || Has(s, "RecoverItem") || Has(s, "Collector")) return "Person/Bergung";
        if (Has(s, "Mining")) return "Bergbau";
        if (Has(s, "Salvage")) return "Bergung";
        if (Has(s, "Investigat")) return "Ermittlung";
        return "Auftrag";
    }

    static string Difficulty(string c)
    {
        if (Has(c, "VeryEasy")) return "Sehr leicht";
        if (Has(c, "Easy")) return "Leicht";
        if (Has(c, "Medium")) return "Mittel";
        if (Has(c, "VeryHard")) return "Sehr schwer";
        if (Has(c, "Hard")) return "Schwer";
        return "k.A.";
    }

    static string SystemOf(string c)
    {
        if (Has(c, "Stanton") || Has(c, "Hurston") || Has(c, "Crusader") || Has(c, "MicroTech") || Has(c, "ArcCorp") || Has(c, "ORS_") || Has(c, "Adaigo") || Has(c, "Adagio")) return "Stanton";
        if (Has(c, "Nyx") || Has(c, "Battaglia") || Has(c, "Levski") || Has(c, "Delamar") || Has(c, "RoX_")) return "Nyx";
        if (Has(c, "Pyro") || Has(c, "RoughAnimals") || Has(c, "Headhunters")) return "Pyro";
        return "k.A.";
    }

    static bool Has(string hay, string needle) =>
        hay.Contains(needle, System.StringComparison.OrdinalIgnoreCase);
}
