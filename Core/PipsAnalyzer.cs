using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SCLogMate.Core;

/// <summary>
/// Ballistische Kenndaten einer Schiffswaffe (Mündungsgeschwindigkeit, DPS, Alpha).
/// </summary>
public sealed record GunBallistics(
    string Name,
    string Model,
    int Size,
    double AmmoSpeed,      // Projektilgeschwindigkeit in m/s (bestimmt den Vorhaltepunkt / Pip)
    double Dps,            // Kontinuierlicher Schaden pro Sekunde (Sustained)
    double Alpha,          // Einzelschussschaden / Alpha Strike
    double FireRateRpm,    // Schussrate pro Minute
    string DamageType      // Laser, Ballistic, Distortion, Plasma
);

/// <summary>
/// Gruppe von Waffen mit identischer Mündungsgeschwindigkeit.
/// </summary>
public sealed record SpeedPipGroup(
    double SpeedMps,
    IReadOnlyList<string> Guns,
    double CombinedDps,
    double CombinedAlpha
);

public sealed record GunVelocityInfo(
    string GunName,
    double SpeedMps,
    string AmmoType,
    string Category
);

/// <summary>
/// Analyse-Ergebnis der Vorhaltepunkte (Pips) für ein Schiffsloadout.
/// </summary>
public sealed record PipsEvaluationResult(
    int PipCount,
    bool IsSynchronized,
    IReadOnlyList<double> SpeedsMps,
    double SpeedSpreadMps,
    string Rating,
    string SummaryBadge,
    string Advice,
    IReadOnlyList<GunVelocityInfo> Guns,
    double TotalPilotDps = 0,
    double TotalAlphaDamage = 0,
    int PipsCount = 0,
    string Status = "",
    string StatusBadge = "",
    string Description = "",
    IReadOnlyList<SpeedPipGroup>? Groups = null
);

/// <summary>
/// Berechnet Mündungsgeschwindigkeiten und die Anzahl resultierender Vorhaltepunkte (Pips)
/// auf dem HUD des Piloten. Verhindert Treffer-Asynchronitäten im Dogfighting.
/// </summary>
public static partial class PipsAnalyzer
{
    // Standard-Mündungsgeschwindigkeiten in Star Citizen (m/s)
    // Laser Repeater (CF-Serie, Attrition) operieren typischerweise bei 1.480 m/s
    // Ballistic Gatlings (Mantis, Scorpion, Yellowjacket) bei 1.332 m/s
    // Laser Cannons (M-Serie, Omnisky) bei 1.400 m/s
    // Ballistic Cannons (Deadbolt, Tarantula) bei 1.150 m/s
    private static readonly List<GunBallistics> KnownGuns = new()
    {
        // === Laser Repeater (1.480 m/s) ===
        new("CF-117 Bulldog", "Bulldog", 1, 1480, 160, 24, 400, "Laser"),
        new("CF-227 Badger", "Badger", 2, 1480, 240, 36, 400, "Laser"),
        new("CF-337 Panther", "Panther", 3, 1480, 360, 54, 400, "Laser"),
        new("CF-447 Rhino", "Rhino", 4, 1480, 540, 81, 400, "Laser"),
        new("CF-557 Galdiseen", "Galdiseen", 5, 1480, 810, 122, 400, "Laser"),
        new("Attrition-1", "Attrition-1", 1, 1480, 175, 26, 400, "Laser"),
        new("Attrition-2", "Attrition-2", 2, 1480, 260, 39, 400, "Laser"),
        new("Attrition-3", "Attrition-3", 3, 1480, 390, 58, 400, "Laser"),
        new("Attrition-4", "Attrition-4", 4, 1480, 585, 88, 400, "Laser"),
        new("Attrition-5", "Attrition-5", 5, 1480, 875, 131, 400, "Laser"),
        new("NBD-28 Neutron Repeater", "NBD-28", 2, 1250, 270, 45, 360, "Laser"),
        new("NBD-30 Neutron Repeater", "NBD-30", 3, 1250, 410, 68, 360, "Laser"),

        // === Laser Cannons (1.400 m/s) ===
        new("M3A Laser Cannon", "M3A", 1, 1400, 180, 72, 150, "Laser"),
        new("M4A Laser Cannon", "M4A", 2, 1400, 270, 108, 150, "Laser"),
        new("M5A Laser Cannon", "M5A", 3, 1400, 405, 162, 150, "Laser"),
        new("M6A Laser Cannon", "M6A", 4, 1400, 610, 244, 150, "Laser"),
        new("M7A Laser Cannon", "M7A", 5, 1400, 915, 366, 150, "Laser"),
        new("Omnisky III", "Omnisky III", 1, 1400, 175, 70, 150, "Laser"),
        new("Omnisky VI", "Omnisky VI", 2, 1400, 265, 106, 150, "Laser"),
        new("Omnisky IX", "Omnisky IX", 3, 1400, 395, 158, 150, "Laser"),
        new("Omnisky XII", "Omnisky XII", 4, 1400, 595, 238, 150, "Laser"),
        new("FL-11 Laser Cannon", "FL-11", 1, 1400, 170, 68, 150, "Laser"),
        new("FL-22 Laser Cannon", "FL-22", 2, 1400, 255, 102, 150, "Laser"),
        new("FL-33 Laser Cannon", "FL-33", 3, 1400, 385, 154, 150, "Laser"),
        new("Lightstrike I", "Lightstrike I", 1, 1400, 185, 74, 150, "Laser"),
        new("Lightstrike II", "Lightstrike II", 2, 1400, 275, 110, 150, "Laser"),
        new("Lightstrike III", "Lightstrike III", 3, 1400, 415, 166, 150, "Laser"),

        // === Ballistic Gatlings & Repeaters (1.332 m/s) ===
        new("Yellowjacket GT-210", "Yellowjacket", 1, 1332, 220, 18, 730, "Ballistic"),
        new("Scorpion GT-215", "Scorpion", 2, 1332, 330, 27, 730, "Ballistic"),
        new("Mantis GT-220", "Mantis", 3, 1332, 495, 41, 730, "Ballistic"),
        new("Revenant", "Revenant", 4, 1332, 740, 61, 730, "Ballistic"),
        new("AD4B Ballistic Gatling", "AD4B", 4, 1332, 790, 65, 730, "Ballistic"),
        new("AD5B Ballistic Gatling", "AD5B", 5, 1332, 1180, 97, 730, "Ballistic"),
        new("SW16BR1 Ballistic Repeater", "SW16BR1", 1, 1332, 210, 21, 600, "Ballistic"),
        new("SW16BR2 Ballistic Repeater", "SW16BR2", 2, 1332, 315, 31, 600, "Ballistic"),
        new("SW16BR3 Ballistic Repeater", "SW16BR3", 3, 1332, 470, 47, 600, "Ballistic"),

        // === Ballistic Cannons (1.150 m/s) ===
        new("Tarantula GT-870", "Tarantula", 3, 1150, 440, 147, 180, "Ballistic"),
        new("Deadbolt I", "Deadbolt I", 1, 1150, 230, 92, 150, "Ballistic"),
        new("Deadbolt II", "Deadbolt II", 2, 1150, 345, 138, 150, "Ballistic"),
        new("Deadbolt III", "Deadbolt III", 3, 1150, 520, 208, 150, "Ballistic"),
        new("Deadbolt IV", "Deadbolt IV", 4, 1150, 780, 312, 150, "Ballistic"),
        new("Deadbolt V", "Deadbolt V", 5, 1150, 1170, 468, 150, "Ballistic"),

        // === Distortion Weapons (1.480 m/s / 1.400 m/s) ===
        new("DR-X2 Distortion Repeater", "DR-X2", 2, 1480, 220, 33, 400, "Distortion"),
        new("DR-X3 Distortion Repeater", "DR-X3", 3, 1480, 330, 50, 400, "Distortion"),
        new("ATLS-1 Distortion Cannon", "ATLS-1", 1, 1400, 160, 64, 150, "Distortion"),
        new("ATLS-2 Distortion Cannon", "ATLS-2", 2, 1400, 240, 96, 150, "Distortion"),
        new("ATLS-3 Distortion Cannon", "ATLS-3", 3, 1400, 360, 144, 150, "Distortion"),

        // === Scatterguns (1.200 m/s) ===
        new("Dominance-1", "Dominance-1", 1, 1200, 210, 105, 120, "Laser"),
        new("Dominance-2", "Dominance-2", 2, 1200, 315, 158, 120, "Laser"),
        new("Dominance-3", "Dominance-3", 3, 1200, 470, 235, 120, "Laser"),
        new("Havoc Scattergun", "Havoc", 1, 1200, 230, 115, 120, "Ballistic"),
        new("Hellion Scattergun", "Hellion", 2, 1200, 345, 172, 120, "Ballistic"),
        new("Predator Scattergun", "Predator", 3, 1200, 515, 258, 120, "Ballistic")
    };

    /// <summary>
    /// Ermittelt die ballistischen Werte einer Waffe anhand des Namens oder Modellkürzels.
    /// </summary>
    public static GunBallistics? FindGun(string? nameOrModel)
    {
        if (string.IsNullOrWhiteSpace(nameOrModel)) return null;
        var s = nameOrModel.Trim();

        // 1. Exakter Namens- oder Modell-Treffer
        var direct = KnownGuns.FirstOrDefault(g =>
            string.Equals(g.Name, s, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(g.Model, s, StringComparison.OrdinalIgnoreCase));
        if (direct != null) return direct;

        // 2. Enthält Modellname (z. B. "Panther" in "CF-337 Panther Laser Repeater")
        return KnownGuns.FirstOrDefault(g =>
            s.Contains(g.Model, StringComparison.OrdinalIgnoreCase) ||
            g.Name.Contains(s, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Standard-Waffenbestückung bekannter Schiffsrümpfe (Pilotengeschütze).
    /// </summary>
    private static readonly Dictionary<string, string[]> StockShipWeapons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Gladius"] = new[] { "Panther", "Panther", "Mantis" },                       // 2 Pips! (1480 + 1332)
        ["Arrow"] = new[] { "Badger", "Badger", "Bulldog", "Bulldog" },              // 1 Pip (1480)
        ["Hornet Mk II"] = new[] { "Rhino", "Rhino", "Panther", "Panther" },          // 1 Pip (1480)
        ["Sabre"] = new[] { "Panther", "Panther", "Panther", "Panther" },            // 1 Pip (1480)
        ["Cutlass Black"] = new[] { "Badger", "Badger", "Badger", "Badger" },         // 1 Pip (1480)
        ["C1 Spirit"] = new[] { "Panther", "Panther", "Panther", "Panther" },         // 1 Pip (1480)
        ["Avenger Titan"] = new[] { "Tigerstreik", "Badger", "Badger" },              // 2 Pips (1332 + 1480)
        ["Hermes"] = new[] { "Rhino", "Rhino", "Rhino", "Rhino" },                    // 1 Pip (1480)
        ["Hull B"] = new[] { "Badger", "Badger" },                                    // 1 Pip (1480)
        ["Freelancer"] = new[] { "Tarantula", "Tarantula", "Tarantula", "Tarantula" },// 1 Pip (1150)
        ["Constellation Taurus"] = new[] { "Rhino", "Rhino", "Rhino", "Rhino" },     // 1 Pip (1480)
        ["Constellation Andromeda"] = new[] { "Rhino", "Rhino", "Rhino", "Rhino" },  // 1 Pip (1480)
        ["Vanguard Warden"] = new[] { "M7A", "MVSA", "MVSA", "MVSA", "MVSA" },        // 1 Pip (1400)
    };

    /// <summary>
    /// Analysiert eine Liste von montierten Waffen und berechnet die Vorhaltepunkte (Pips).
    /// </summary>
    public static PipsEvaluationResult EvaluateGuns(IEnumerable<string>? gunNames)
    {
        if (gunNames == null)
            return EmptyResult();

        var resolvedGuns = new List<GunBallistics>();
        foreach (var rawName in gunNames)
        {
            var found = FindGun(rawName);
            if (found != null)
            {
                resolvedGuns.Add(found);
            }
            else
            {
                // Fallback: Wenn Name "Repeater" oder "Cannon" enthält, plausible Standardwerte annehmen
                resolvedGuns.Add(GuessGunFallback(rawName));
            }
        }

        if (resolvedGuns.Count == 0)
            return EmptyResult();

        // Gruppierung nach Mündungsgeschwindigkeit gerundet auf ganze m/s
        var groups = resolvedGuns
            .GroupBy(g => Math.Round(g.AmmoSpeed))
            .OrderByDescending(g => g.Key)
            .Select(grp => new SpeedPipGroup(
                SpeedMps: grp.Key,
                Guns: grp.Select(g => g.Name).ToList(),
                CombinedDps: Math.Round(grp.Sum(g => g.Dps), 1),
                CombinedAlpha: Math.Round(grp.Sum(g => g.Alpha), 1)
            ))
            .ToList();

        int pipsCount = groups.Count;
        double totalDps = Math.Round(resolvedGuns.Sum(g => g.Dps), 1);
        double totalAlpha = Math.Round(resolvedGuns.Sum(g => g.Alpha), 1);

        string status;
        string badge;
        string description;

        if (pipsCount == 1)
        {
            var speed = groups[0].SpeedMps;
            status = "Synchronized";
            badge = "1 Pip · Synchronisiert 🟢";
            description = $"Alle {resolvedGuns.Count} Pilotengeschütze feuern mit identischer Geschwindigkeit ({speed:N0} m/s). Perfekter einheitlicher Vorhaltepunkt auf dem Visier.";
        }
        else if (pipsCount == 2)
        {
            status = "Mismatched";
            badge = "2 Pips · Geteilter Vorhaltepunkt 🟡";
            var sp1 = groups[0].SpeedMps;
            var sp2 = groups[1].SpeedMps;
            description = $"Warnung: {resolvedGuns.Count} Waffen feuern mit 2 unterschiedlichen Geschwindigkeiten ({sp1:N0} m/s & {sp2:N0} m/s). Es erscheinen 2 Vorhaltepunkte auf dem HUD – Zielen auf einen Pip lässt die andere Waffengruppe verfehlen!";
        }
        else
        {
            status = "Scattered";
            badge = $"{pipsCount} Pips · Stark gestreut 🔴";
            description = $"Kritisch: Waffen sind über {pipsCount} unterschiedliche Geschwindigkeiten verteilt. Zielgenaues Feuern im Dogfight ist kaum möglich.";
        }

        var speedsList = groups.Select(g => g.SpeedMps).ToList();
        double speedSpread = speedsList.Count > 1 ? (speedsList.Max() - speedsList.Min()) : 0;
        bool isSync = pipsCount == 1;
        string rating = isSync ? "Perfect" : (pipsCount == 2 ? "Compatible" : "SplitPips");
        string summaryBadge = isSync ? $"1 Pip ({speedsList[0]:N0} m/s)" : $"{pipsCount} Pips ({string.Join(" / ", speedsList.Select(s => $"{s:N0}"))} m/s)";
        var gunVelocities = resolvedGuns.Select(g => new GunVelocityInfo(g.Name, g.AmmoSpeed, g.DamageType, g.DamageType)).ToList();

        return new PipsEvaluationResult(
            PipCount: pipsCount,
            IsSynchronized: isSync,
            SpeedsMps: speedsList,
            SpeedSpreadMps: speedSpread,
            Rating: rating,
            SummaryBadge: summaryBadge,
            Advice: description,
            Guns: gunVelocities,
            TotalPilotDps: totalDps,
            TotalAlphaDamage: totalAlpha,
            PipsCount: pipsCount,
            Status: status,
            StatusBadge: badge,
            Description: description,
            Groups: groups
        );
    }

    /// <summary>
    /// Analysiert die Vorhaltepunkte für ein bekanntes Schiff anhand seines Namens / Rumpfes.
    /// </summary>
    public static PipsEvaluationResult EvaluateShip(string? shipName)
    {
        if (string.IsNullOrWhiteSpace(shipName))
            return EmptyResult();

        // 1. Schiffsname bereinigen (z. B. "Gladius · Aegis" -> "Gladius")
        var clean = Ships.NormalizeModelName(shipName.Split('·')[0].Trim());

        // 2. Nach bekannten Standardwaffen suchen
        foreach (var (shipKey, guns) in StockShipWeapons)
        {
            if (clean.Contains(shipKey, StringComparison.OrdinalIgnoreCase) ||
                shipKey.Contains(clean, StringComparison.OrdinalIgnoreCase))
            {
                return EvaluateGuns(guns);
            }
        }

        // 3. Fallback: Wenn Schiff nicht im Dictionary, Standard Laser Repeater Fit (1.480 m/s) annehmen
        return EvaluateGuns(new[] { "CF-337 Panther", "CF-337 Panther" });
    }

    /// <summary>
    /// Prüft, was ein Waffentausch mit den Vorhaltepunkten macht (+1 Pip, 0 Pip, -1 Pip).
    /// </summary>
    public static (int PipDelta, string ChipText, string ChipColor) EvaluateCandidateSwap(
        IEnumerable<string> currentGuns,
        string oldGun,
        string newGun)
    {
        var currentRes = EvaluateGuns(currentGuns);
        var modifiedList = currentGuns.ToList();
        
        int idx = modifiedList.FindIndex(g => g.Contains(oldGun, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0)
        {
            modifiedList[idx] = newGun;
        }
        else
        {
            modifiedList.Add(newGun);
        }

        var newRes = EvaluateGuns(modifiedList);
        int delta = newRes.PipsCount - currentRes.PipsCount;

        if (delta > 0)
            return (delta, $"+{delta} Pip 🟡", "#F59E0B");
        if (delta < 0)
            return (delta, $"{delta} Pip 🟢", "#10B981");

        return (0, "0 Pip 🟢", "#38BDF8");
    }

    private static PipsEvaluationResult EmptyResult() =>
        new(
            PipCount: 0,
            IsSynchronized: false,
            SpeedsMps: Array.Empty<double>(),
            SpeedSpreadMps: 0,
            Rating: "NoGuns",
            SummaryBadge: "Keine Waffen",
            Advice: "Keine Pilotengeschütze für die Ballistik-Analyse erfasst.",
            Guns: Array.Empty<GunVelocityInfo>(),
            TotalPilotDps: 0,
            TotalAlphaDamage: 0,
            PipsCount: 0,
            Status: "NoGuns",
            StatusBadge: "Keine Waffen",
            Description: "Keine Pilotengeschütze für die Ballistik-Analyse erfasst.",
            Groups: Array.Empty<SpeedPipGroup>()
        );

    private static GunBallistics GuessGunFallback(string raw)
    {
        var l = raw.ToLowerInvariant();
        if (l.Contains("gatling") || l.Contains("ballistic") || l.Contains("mantis") || l.Contains("scorpion"))
            return new(raw, raw, 3, 1332, 450, 40, 700, "Ballistic");
        if (l.Contains("cannon") || l.Contains("laser cannon") || l.Contains("m4a") || l.Contains("m5a"))
            return new(raw, raw, 3, 1400, 400, 160, 150, "Laser");
        if (l.Contains("deadbolt") || l.Contains("tarantula"))
            return new(raw, raw, 3, 1150, 450, 180, 150, "Ballistic");

        // Standard Laser Repeater
        return new(raw, raw, 3, 1480, 360, 54, 400, "Laser");
    }
}
