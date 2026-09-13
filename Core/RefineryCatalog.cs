using System;
using System.Collections.Generic;
using System.Linq;

namespace SCLogMate.Core;

public record RefineryStation(
    string Id,
    string Name,
    string System,
    string LocationType,
    string Description,
    Dictionary<string, double> MaterialYieldBonuses, // z. B. "Quantanium" => +0.05 (+5%)
    List<string> PreferredMethods,                   // z. B. ["Dinyx", "Ferron"]
    bool HasArmistice = true
);

public record RefineryMethod(
    string Name,
    string DisplayName,
    double BaseYield,       // Basis-Ausbeute in % (z. B. 93.0 für Dinyx)
    double TimeMultiplier,   // Zeitfaktor (1.0 = normal, 2.5 = sehr langsam, 0.4 = blitzschnell)
    double CostMultiplier,   // Kostenfaktor pro cSCU
    string SpeedRating,     // "Sehr Langsam", "Mittel", "Sehr Schnell"
    string CostRating,      // "Niedrig", "Mittel", "Hoch"
    string Description
);

public static class RefineryCatalog
{
    private static readonly List<RefineryStation> _stations = new();
    private static readonly List<RefineryMethod> _methods = new();

    static RefineryCatalog()
    {
        InitMethods();
        InitStations();
    }

    public static IReadOnlyList<RefineryStation> AllStations => _stations;
    public static IReadOnlyList<RefineryMethod> AllMethods => _methods;

    public static List<string> Stations => _stations.Select(s => s.Name).ToList();
    public static List<string> Materials => new()
    {
        "Quantanium", "Bexalite", "Gold", "Taranite", "Larinite", "Agricium",
        "Hephaestanite", "Beryl", "Diamond", "Titanium", "Tungsten",
        "Corundum", "Quartz", "Copper", "Iron", "Lindinium"
    };
    public static List<string> Methods => _methods.Select(m => m.DisplayName).ToList();

    private static void InitMethods()
    {
        _methods.AddRange(new[]
        {
            new RefineryMethod(
                "Dinyx Dodecathetic",
                "Dinyx Dodecathetic",
                93.0,
                2.6,
                0.9,
                "Sehr Langsam",
                "Niedrig",
                "Extrem hohe Materialausbeute durch schonende Lösemittelverfahren. Dauert am längsten, minimiert jedoch Rohstoffverluste."
            ),
            new RefineryMethod(
                "Ferron Exchange",
                "Ferron Exchange",
                88.5,
                1.3,
                1.4,
                "Ausgewogen",
                "Mittel",
                "Beliebtes Gleichgewichtsverfahren. Gute Ausbeute bei moderater Verarbeitungsdauer und vertretbaren Kosten."
            ),
            new RefineryMethod(
                "Cormack Method",
                "Cormack Method",
                86.0,
                0.85,
                0.75,
                "Schnell",
                "Sehr Günstig",
                "Effizientes, kostengünstiges chemisches Abscheideverfahren. Etwas geringere Ausbeute, aber sehr schnelle Fertigstellung."
            ),
            new RefineryMethod(
                "Electrostatic Purification",
                "Electrostatic Purification",
                90.5,
                0.65,
                2.2,
                "Sehr Schnell",
                "Sehr Hoch",
                "Hochmoderne elektromagnetische Trennung. Nahezu maximale Ausbeute in Rekordzeit, aber mit sehr hohen Prozesskosten verbunden."
            ),
            new RefineryMethod(
                "Pyroxeres",
                "Pyroxeres",
                82.0,
                0.9,
                0.6,
                "Schnell",
                "Günstig",
                "Traditionelles thermisches Schmelzverfahren. Niedrigste Veredelungskosten, allerdings mit spürbaren Materialverlusten."
            ),
            new RefineryMethod(
                "Gaskin-Kandah",
                "Gaskin-Kandah",
                78.0,
                0.7,
                1.1,
                "Schnell",
                "Mittel",
                "Spezialisiertes Gasphasen-Abscheideverfahren für flüchtige Erze."
            ),
            new RefineryMethod(
                "Thermite Processing",
                "Thermite Processing",
                80.0,
                0.55,
                1.8,
                "Extrem Schnell",
                "Hoch",
                "Hochenergetischer thermischer Schnellaufschluss für Notfälle und zeitkritische Lieferungen."
            )
        });
    }

    private static void InitStations()
    {
        // -------------------------------------------------------------
        // 1. STANTON SYSTEM
        // -------------------------------------------------------------
        _stations.Add(new RefineryStation(
            "arc_l1",
            "ARC-L1 Wide Forest Station",
            "Stanton",
            "Lagrange-Station (ArcCorp)",
            "Großes Erz-Umschlagszentrum mit Spezialisierung auf Quantanium und Bexalite.",
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                { "Quantanium", 0.05 },
                { "Quantainium", 0.05 },
                { "Bexalite", 0.03 },
                { "Gold", 0.02 }
            },
            new List<string> { "Dinyx Dodecathetic", "Ferron Exchange" }
        ));

        _stations.Add(new RefineryStation(
            "arc_l2",
            "ARC-L2 Lively Pathway Station",
            "Stanton",
            "Lagrange-Station (ArcCorp)",
            "Raffinerie- und Handelsstützpunkt auf der Route zu den Industrieaußenposten von ArcCorp.",
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                { "Titanium", 0.04 },
                { "Tungsten", 0.04 },
                { "Titan", 0.04 },
                { "Wolfram", 0.04 }
            },
            new List<string> { "Cormack Method", "Ferron Exchange" }
        ));

        _stations.Add(new RefineryStation(
            "cru_l1",
            "CRU-L1 Ambitious Dream Station",
            "Stanton",
            "Lagrange-Station (Crusader)",
            "Stark frequentiertes Bergbau-Zentrum nahe Crusader mit exzellenter Dinyx-Infrastruktur.",
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                { "Quantanium", 0.04 },
                { "Quantainium", 0.04 },
                { "Agricium", 0.05 },
                { "Hephaestanite", 0.03 }
            },
            new List<string> { "Dinyx Dodecathetic", "Electrostatic Purification" }
        ));

        _stations.Add(new RefineryStation(
            "hur_l1",
            "HUR-L1 Green Glade Station",
            "Stanton",
            "Lagrange-Station (Hurston)",
            "Zentrale Hurston Dynamics Raffinerie für schwere Industriemetalle und Edelmetalle.",
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                { "Gold", 0.05 },
                { "Beryl", 0.06 },
                { "Beryll", 0.06 },
                { "Taranite", 0.04 }
            },
            new List<string> { "Ferron Exchange", "Pyroxeres" }
        ));

        _stations.Add(new RefineryStation(
            "hur_l2",
            "HUR-L2 Support Station",
            "Stanton",
            "Lagrange-Station (Hurston)",
            "Versorgungs- und Veredelungswerk für mineralische Massenschüttgüter.",
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                { "Corundum", 0.07 },
                { "Quartz", 0.08 },
                { "Quarz", 0.08 },
                { "Copper", 0.05 }
            },
            new List<string> { "Cormack Method", "Pyroxeres" }
        ));

        _stations.Add(new RefineryStation(
            "mic_l1",
            "MIC-L1 Shallow Frontier Station",
            "Stanton",
            "Lagrange-Station (microTech)",
            "Hochmoderne microTech-Forschungs- und Veredelungsstation mit besten elektrostatischen Anlagen.",
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                { "Quantanium", 0.03 },
                { "Quantainium", 0.03 },
                { "Bexalite", 0.05 },
                { "Larinite", 0.04 }
            },
            new List<string> { "Electrostatic Purification", "Dinyx Dodecathetic" }
        ));

        _stations.Add(new RefineryStation(
            "mic_l2",
            "MIC-L2 Long Forest Station",
            "Stanton",
            "Lagrange-Station (microTech)",
            "Raffinerie für Asteroiden-Erze aus den äußeren Stanton-Gürteln.",
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                { "Bexalite", 0.04 },
                { "Gold", 0.03 }
            },
            new List<string> { "Ferron Exchange", "Cormack Method" }
        ));

        _stations.Add(new RefineryStation(
            "mic_l5",
            "MIC-L5 Modern Icarus Station",
            "Stanton",
            "Lagrange-Station (microTech)",
            "Beliebter Umschlagplatz für Solo-Prospector-Piloten im tiefen microTech-Raum.",
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                { "Bexalite", 0.06 },
                { "Agricium", 0.03 }
            },
            new List<string> { "Dinyx Dodecathetic", "Cormack Method" }
        ));

        // -------------------------------------------------------------
        // 2. PYRO SYSTEM
        // -------------------------------------------------------------
        _stations.Add(new RefineryStation(
            "pyro_gateway",
            "Pyro Gateway Station",
            "Pyro",
            "Sprungtor-Station",
            "Grenznahe Veredelungsstation am Stanton-Sprungtor mit hohen Sicherheitsvorkehrungen.",
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                { "Quantanium", 0.02 },
                { "Quantainium", 0.02 },
                { "Titanium", 0.03 }
            },
            new List<string> { "Ferron Exchange" }
        ));

        _stations.Add(new RefineryStation(
            "ruin_station",
            "Ruin Station",
            "Pyro",
            "Asteroidenbasis (Pyro VI)",
            "Gesetzloses Piraten-Industriezentrum. Akzeptiert alle Erze ohne lästige UEE-Bürokratie.",
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                { "Gold", 0.04 },
                { "Taranite", 0.05 },
                { "Bexalite", 0.03 }
            },
            new List<string> { "Pyroxeres", "Ferron Exchange" },
            HasArmistice: false
        ));

        _stations.Add(new RefineryStation(
            "pyro_checkpoint_1",
            "Pyro Checkpoint Station Alpha",
            "Pyro",
            "Frontier-Station",
            "Befestigter Schrott- und Erzbrecher-Posten im Pyro-System.",
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                { "Titanium", 0.05 },
                { "Iron", 0.05 }
            },
            new List<string> { "Cormack Method", "Pyroxeres" },
            HasArmistice: false
        ));

        // -------------------------------------------------------------
        // 3. NYX SYSTEM
        // -------------------------------------------------------------
        _stations.Add(new RefineryStation(
            "levski",
            "Levski Mining Center (Delamar)",
            "Nyx",
            "Planetare Bergbaustadt",
            "Freie Bergbaustadt der People's Alliance in Delamar. Traditionell spezialisiert auf seltene Asteroiden-Erze und Lindinium.",
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                { "Lindinium", 0.08 },
                { "Bexalite", 0.06 },
                { "Quantanium", 0.04 },
                { "Quantainium", 0.04 },
                { "Beryl", 0.05 },
                { "Beryll", 0.05 }
            },
            new List<string> { "Dinyx Dodecathetic", "Ferron Exchange", "Cormack Method" }
        ));

        _stations.Add(new RefineryStation(
            "glaciem_rockcracker",
            "Glaciem RockCracker 12",
            "Nyx",
            "Eisring-Industriestation",
            "Schwimmende Industriestation im Glaciem-Eisgürtel mit Hochdruck-Spaltanlagen.",
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                { "Quantanium", 0.06 },
                { "Quantainium", 0.06 },
                { "Diamond", 0.07 },
                { "Lindinium", 0.05 }
            },
            new List<string> { "Electrostatic Purification", "Dinyx Dodecathetic" }
        ));

        _stations.Add(new RefineryStation(
            "breaker_267",
            "QV Breaker Station BRK-267",
            "Nyx",
            "Schrott- & Brecherstation",
            "Schrott- und Erzbrecher-Station im Delamar-Asteroidenring mit schneller Grobveredelung.",
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                { "RMC", 0.05 },
                { "Titanium", 0.05 },
                { "Tungsten", 0.04 }
            },
            new List<string> { "Pyroxeres", "Cormack Method" },
            HasArmistice: false
        ));
    }

    /// <summary>
    /// Ermittelt das Sternensystem einer Station.
    /// </summary>
    public static string DetectSystem(string stationName)
    {
        if (string.IsNullOrWhiteSpace(stationName)) return "Stanton";
        var match = _stations.FirstOrDefault(s => s.Name.Contains(stationName, StringComparison.OrdinalIgnoreCase)
                                              || stationName.Contains(s.Name, StringComparison.OrdinalIgnoreCase)
                                              || stationName.Contains(s.Id, StringComparison.OrdinalIgnoreCase));
        if (match != null) return match.System;

        if (stationName.Contains("Delamar", StringComparison.OrdinalIgnoreCase) ||
            stationName.Contains("Levski", StringComparison.OrdinalIgnoreCase) ||
            stationName.Contains("Glaciem", StringComparison.OrdinalIgnoreCase) ||
            stationName.Contains("Nyx", StringComparison.OrdinalIgnoreCase))
            return "Nyx";

        if (stationName.Contains("Pyro", StringComparison.OrdinalIgnoreCase) ||
            stationName.Contains("Ruin", StringComparison.OrdinalIgnoreCase) ||
            stationName.Contains("Monox", StringComparison.OrdinalIgnoreCase))
            return "Pyro";

        return "Stanton";
    }

    /// <summary>
    /// Berechnet die effektive Ausbeute für ein Erz an einer bestimmten Station mit der gewählten Methode.
    /// </summary>
    public static double CalculateYield(string material, string method, string station)
    {
        var meth = _methods.FirstOrDefault(m => m.Name.Equals(method, StringComparison.OrdinalIgnoreCase) || m.DisplayName.Equals(method, StringComparison.OrdinalIgnoreCase))
                   ?? _methods.First();

        double yield = meth.BaseYield;
        var st = _stations.FirstOrDefault(s => s.Name.Equals(station, StringComparison.OrdinalIgnoreCase) || station.Contains(s.Name, StringComparison.OrdinalIgnoreCase));
        if (st != null && st.MaterialYieldBonuses.TryGetValue(material, out var bonus))
        {
            yield += (bonus * 100.0);
        }

        return Math.Clamp(Math.Round(yield, 1), 50.0, 98.0);
    }
}
