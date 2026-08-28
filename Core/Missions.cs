namespace SCLogReader.Core;

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
        var faction = Faction(generator);
        var both = generator + "_" + contract;
        return new Info(faction, Type(both), Difficulty(contract), SystemOf(contract));
    }

    /// <summary>Anzeige-/Speicherform: "RedWind · Fracht/Bergung · Schwer · Stanton".</summary>
    public static string Format(in Info i) => $"{i.Faction} · {i.Type} · {i.Difficulty} · {i.System}";

    /// <summary>Fraktion = Präfix vor dem ersten Unterstrich (bzw. der ganze Name).</summary>
    static string Faction(string generator)
    {
        var g = generator.Trim();
        var us = g.IndexOf('_');
        return us > 0 ? g[..us] : g;
    }

    static string Type(string s)
    {
        if (Has(s, "RecoverCargo") || Has(s, "HaulCargo") || Has(s, "Hauling")) return "Fracht/Bergung";
        if (Has(s, "FacilityDelve")) return "Facility Delve";
        if (Has(s, "Assassinat") || Has(s, "Eliminate") || Has(s, "KillShip") || Has(s, "HeadHunt")) return "Kampf/Kill";
        if (Has(s, "Patrol") || Has(s, "Defend")) return "Patrouille/Verteidigung";
        if (Has(s, "RecoverData") || Has(s, "DataDownload") || Has(s, "BlackBox")) return "Daten";
        if (Has(s, "MissingPerson") || Has(s, "RecoverItem") || Has(s, "Collector")) return "Person/Bergung";
        return "Sonstige";
    }

    static string Difficulty(string c)
    {
        if (Has(c, "VeryEasy")) return "Sehr leicht";
        if (Has(c, "Easy")) return "Leicht";
        if (Has(c, "Medium")) return "Mittel";
        if (Has(c, "Hard")) return "Schwer";
        return "k.A.";
    }

    static string SystemOf(string c)
    {
        if (Has(c, "Stanton")) return "Stanton";
        if (Has(c, "Nyx")) return "Nyx";
        if (Has(c, "Pyro")) return "Pyro";
        return "k.A.";
    }

    static bool Has(string hay, string needle) =>
        hay.Contains(needle, System.StringComparison.OrdinalIgnoreCase);
}
