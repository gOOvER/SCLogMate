using System;
using System.Collections.Generic;
using System.Linq;

namespace SCLogMate.Core;

/// <summary>
/// Mining modifiers applied to a rock: percentages where -30 means 30% reduction.
/// Stacks additively across lasers, modules, and gadgets.
/// </summary>
public sealed record MiningModifiers(
    double Instability = 0,
    double WindowSize = 0,
    double Resistance = 0,
    double ShatterDamage = 0,
    double ClusterFactor = 0,
    double WindowRate = 0,
    double CatastrophicRate = 0)
{
    public static readonly MiningModifiers None = new();

    public bool IsEmpty => Instability == 0 && WindowSize == 0 && Resistance == 0 && ShatterDamage == 0
        && ClusterFactor == 0 && WindowRate == 0 && CatastrophicRate == 0;

    public MiningModifiers Plus(MiningModifiers other) => new(
        Instability + other.Instability,
        WindowSize + other.WindowSize,
        Resistance + other.Resistance,
        ShatterDamage + other.ShatterDamage,
        ClusterFactor + other.ClusterFactor,
        WindowRate + other.WindowRate,
        CatastrophicRate + other.CatastrophicRate);
}

/// <summary>
/// Specifications of a ship or vehicle mining laser head.
/// </summary>
public sealed record MiningLaserInfo(
    string Id,
    string Name,
    int Size,
    double Power,
    double ExtractionPower,
    double FilterModifier,
    double ThrottleMinimum,
    int Slots,
    string Manufacturer,
    MiningModifiers Modifiers,
    string Description = "");

/// <summary>
/// Passive or active module insertable into mining laser head slots.
/// </summary>
public sealed record MiningModuleInfo(
    string Id,
    string Name,
    string Type, // "passive" or "active"
    double PowerMultiplier,
    double ExtractionMultiplier,
    int Charges,
    double LifetimeSeconds,
    MiningModifiers Modifiers,
    string Description = "");

/// <summary>
/// Deployable mining gadget placed directly onto a deposit.
/// </summary>
public sealed record MiningGadgetInfo(
    string Id,
    string Name,
    MiningModifiers Modifiers,
    string Description = "");

/// <summary>
/// User input parameters for a scanned deposit.
/// </summary>
public sealed record RockScanInput(
    double MassKg,
    double ResistancePercent,
    double InstabilityPercent,
    string? MineralName = null);

/// <summary>
/// Selected head and module configuration.
/// </summary>
public sealed record LaserHeadSelection(
    string LaserId,
    List<string>? ModuleIds = null);

/// <summary>
/// Alternative laser head assessment for the same rock.
/// </summary>
public sealed record LaserAlternativeDto(
    string LaserId,
    string Name,
    double Power,
    double PowerDelivered,
    double Ratio,
    string Verdict,
    double MaxCrackableMassKg,
    double EffectiveResistancePercent);

/// <summary>
/// Comprehensive verdict evaluating whether the current equipment can crack the rock.
/// </summary>
public sealed record CrackVerdictResult(
    double PowerDelivered,
    double PowerRequired,
    double Ratio,
    string Verdict, // "solo", "gadget", "crew", "none"
    string VerdictTitle,
    string VerdictBadge,
    string VerdictColor,
    double EffectiveResistancePercent,
    double EffectiveInstabilityPercent,
    double WindowPercent,
    double MaxCrackableMassKg,
    double EnergyCapacity,
    double EnergyDecayPerSecond,
    IReadOnlyList<string> Notes,
    IReadOnlyList<LaserAlternativeDto> Alternatives);

/// <summary>
/// Rock-cracking calculator based on community formulas (scminer.rocks, 0.36 W/kg @ 0% res)
/// and official Star Citizen 4.x PU laser parameters.
/// </summary>
public static class RockCracking
{
    // Community golden constant: 0.36 W/kg required at zero resistance
    public const double RequiredWattsPerKg = 0.36;
    public const double SoloRatio = 1.15;
    public const double GadgetRatio = 0.70;

    // Default Star Citizen 4.x rock model constants
    public const double DefaultPowerCapacityPerMass = 10.0;
    public const double DefaultDecayPerMass = 0.20;
    public const double DefaultBaseWindowSize = 0.10;
    public const double DefaultMaxWindowSize = 0.50;

    public static readonly IReadOnlyList<MiningLaserInfo> Lasers =
    [
        new("helix_s1", "Helix I", 1, 3900, 480, 0, 0.15, 2, "Thermyte", new(Instability: -25, WindowSize: -20, Resistance: -30), "High-power laser capable of cracking high-mass solo deposits."),
        new("helix_s2", "Helix II", 2, 8450, 1050, 0, 0.15, 3, "Thermyte", new(Instability: -30, WindowSize: -25, Resistance: -35), "Heavy Size 2 MOLE laser designed for extreme deposits."),
        new("arbor_mh1", "Arbor MH1", 1, 2340, 390, 0, 0.10, 1, "Shubin Interstellar", new(Instability: 0, WindowSize: 0, Resistance: 0), "Standard balanced Prospector factory mining head."),
        new("klein_s1", "Klein-S1", 1, 3120, 440, 0, 0.10, 2, "Shubin Interstellar", new(Instability: 10, WindowSize: 20, Resistance: 10), "Higher wattage with an enlarged optimal charge window."),
        new("klein_s2", "Klein-S2", 2, 6760, 960, 0, 0.10, 2, "Shubin Interstellar", new(Instability: 15, WindowSize: 25, Resistance: 15), "Size 2 variant for heavy rock extraction."),
        new("hofstede_s1", "Hofstede-S1", 1, 2340, 390, 0, 0.10, 2, "Shubin Interstellar", new(Instability: -35, WindowSize: 10, Resistance: 0), "Specialized in subduing chaotic, high-instability deposits."),
        new("lancet_mh1", "Lancet MH1", 1, 1800, 320, 0, 0.05, 3, "Greycat Industrial", new(Instability: -75, WindowSize: 40, Resistance: -20, ShatterDamage: -50), "Precision laser with maximum stability for volatile Quantanium."),
        new("impact_s1", "Impact I", 1, 2860, 420, 0, 0.12, 1, "Greycat Industrial", new(Instability: 20, WindowSize: -15, Resistance: -15, ShatterDamage: 40), "Aggressive head with enhanced shatter impact.")
    ];

    public static readonly IReadOnlyList<MiningModuleInfo> Modules =
    [
        new("mod_focus3", "Focus III", "passive", 1.0, 1.0, 0, 0, new(Instability: -25, WindowSize: 20), "Significantly widens optimal charge window."),
        new("mod_surge", "Surge", "active", 1.5, 1.0, 5, 7.0, new(Instability: 20, Resistance: -10), "Active power burst: +50% fracture power for 7 seconds."),
        new("mod_brand", "Brand", "passive", 1.15, 1.0, 0, 0, new(Instability: 10), "Increases continuous fracture beam power by 15%."),
        new("mod_stampede", "Stampede", "active", 1.8, 1.0, 3, 5.0, new(Instability: 50, CatastrophicRate: 50), "Extreme emergency burst: +80% power for 5 seconds."),
        new("mod_lifeline", "Lifeline", "passive", 1.0, 1.0, 0, 0, new(CatastrophicRate: -50, ShatterDamage: -30), "Safety module reducing catastrophic explosion damage."),
        new("mod_rieger_c3", "Rieger-C3", "passive", 1.1, 1.0, 0, 0, new(Resistance: -15), "Reduces rock resistance by -15% passively."),
        new("mod_optimum", "Optimum", "passive", 1.0, 1.0, 0, 0, new(WindowSize: 30, WindowRate: 20), "Maximizes optimal charge window width."),
        new("mod_torpid", "Torpid", "passive", 1.0, 1.0, 0, 0, new(Instability: -40), "Aggressive instability suppression.")
    ];

    public static readonly IReadOnlyList<MiningGadgetInfo> Gadgets =
    [
        new("gadget_boremax", "BoreMax", new(Resistance: -25, WindowSize: 15, Instability: 10), "Attaches to rock to reduce resistance by -25%."),
        new("gadget_optimax", "OptiMax", new(WindowSize: 35, Instability: -20, Resistance: -10), "Widens the optimal charge zone and calms instability."),
        new("gadget_waveshift", "WaveShift", new(Resistance: -35, ShatterDamage: -20), "Massive resistance reduction (-35%) for ultra-dense rocks."),
        new("gadget_sabir", "Sabir 21", new(Instability: -50, WindowSize: 20), "Specialized in stabilizing hyper-volatile deposits."),
        new("gadget_stalwart", "Stalwart", new(CatastrophicRate: -60, ShatterDamage: -40), "Maximizes containment safety against catastrophic detonations.")
    ];

    /// <summary>
    /// Calculates the crackability of a rock with a specific laser and module setup.
    /// </summary>
    public static CrackVerdictResult Assess(
        RockScanInput rock,
        IReadOnlyList<LaserHeadSelection> headSelections,
        string? gadgetSelectionId = null)
    {
        var notes = new List<string>();
        var gadget = !string.IsNullOrEmpty(gadgetSelectionId)
            ? Gadgets.FirstOrDefault(g => g.Id.Equals(gadgetSelectionId, StringComparison.OrdinalIgnoreCase))
            : null;

        var selectedHeads = new List<(MiningLaserInfo laser, List<MiningModuleInfo> mods)>();
        foreach (var hs in headSelections)
        {
            var l = Lasers.FirstOrDefault(x => x.Id.Equals(hs.LaserId, StringComparison.OrdinalIgnoreCase))
                 ?? Lasers[0]; // fallback to Arbor
            var mods = new List<MiningModuleInfo>();
            if (hs.ModuleIds != null)
            {
                foreach (var mid in hs.ModuleIds.Take(l.Slots))
                {
                    var m = Modules.FirstOrDefault(mod => mod.Id.Equals(mid, StringComparison.OrdinalIgnoreCase));
                    if (m != null) mods.Add(m);
                }
            }
            selectedHeads.Add((l, mods));
        }

        if (selectedHeads.Count == 0)
        {
            selectedHeads.Add((Lasers[0], []));
        }

        // 1. Calculate Power & Stacking Modifiers
        double totalPower = 0.0;
        var modifiers = MiningModifiers.None;

        foreach (var (laser, mods) in selectedHeads)
        {
            double headPowerMultiplier = mods.Aggregate(1.0, (acc, m) => acc * m.PowerMultiplier);
            totalPower += laser.Power * headPowerMultiplier;
            foreach (var mod in mods)
            {
                modifiers = modifiers.Plus(mod.Modifiers);
            }
        }

        // Average laser modifiers across all active heads (e.g. MOLE with 3 heads)
        var laserAveraged = selectedHeads.Aggregate(MiningModifiers.None, (acc, h) => acc.Plus(h.laser.Modifiers));
        modifiers = modifiers.Plus(new MiningModifiers(
            laserAveraged.Instability / selectedHeads.Count,
            laserAveraged.WindowSize / selectedHeads.Count,
            laserAveraged.Resistance / selectedHeads.Count,
            laserAveraged.ShatterDamage / selectedHeads.Count,
            laserAveraged.ClusterFactor / selectedHeads.Count,
            laserAveraged.WindowRate / selectedHeads.Count,
            laserAveraged.CatastrophicRate / selectedHeads.Count
        ));

        if (gadget != null)
        {
            modifiers = modifiers.Plus(gadget.Modifiers);
        }

        // 2. Compute Effective Rock Values
        double effResistance = Math.Clamp(rock.ResistancePercent * (1.0 + modifiers.Resistance / 100.0), 0.0, 98.0);
        double effInstability = Math.Clamp(rock.InstabilityPercent * (1.0 + modifiers.Instability / 100.0), 0.0, 100.0);

        // 3. Compute Power Balance & Verdict
        double requiredPower = RequiredWattsPerKg * Math.Max(1.0, rock.MassKg);
        double deliveredPower = totalPower * (1.0 - effResistance / 100.0);
        double ratio = requiredPower > 0 ? deliveredPower / requiredPower : 0;

        string verdict;
        string verdictTitle;
        string verdictBadge;
        string verdictColor;

        if (ratio >= SoloRatio)
        {
            verdict = "solo";
            verdictTitle = "Solo machbar";
            verdictBadge = "SOLO BRECHBAR ✓";
            verdictColor = "#10B981"; // emerald
        }
        else if (ratio >= GadgetRatio)
        {
            verdict = "gadget";
            verdictTitle = "Gadget / Modul nötig";
            verdictBadge = "GADGET EMPFOHLEN ⚡";
            verdictColor = "#F59E0B"; // amber
        }
        else
        {
            verdict = "crew";
            verdictTitle = "Multi-Crew erforderlich";
            verdictBadge = "ZU SCHWER FÜR SOLO ✖";
            verdictColor = "#EF4444"; // rose/red
        }

        // 4. Window, Capacity & Max Crackable Mass
        double window = Math.Clamp(DefaultBaseWindowSize * (1.0 + modifiers.WindowSize / 100.0), 0.0, DefaultMaxWindowSize) * 100.0;
        double maxCrackableMass = Math.Round((totalPower * (1.0 - effResistance / 100.0)) / (RequiredWattsPerKg * SoloRatio));
        double energyCapacity = DefaultPowerCapacityPerMass * rock.MassKg;
        double energyDecay = DefaultDecayPerMass * rock.MassKg;

        // 5. Notes & Tactical Advice
        if (verdict == "solo")
        {
            notes.Add($"Ausreichende Laser-Leistung: Überschuss von {(ratio - 1.0) * 100:0.0}% über der Abbruchschwelle.");
        }
        else if (verdict == "gadget")
        {
            notes.Add($"Leistungsdefizit von {(1.0 - ratio) * 100:0.0}%: Ein Gadget (-25% Resistenz) oder aktives Surge-Modul schließt die Lücke.");
        }
        else
        {
            notes.Add($"Laser liefert {deliveredPower:N0} W von benötigten {requiredPower:N0} W. Zweites Schiff (Prospector) oder Multi-Crew (MOLE) erforderlich.");
        }

        if (effInstability > 50)
        {
            notes.Add($"Hohe Rest-Instabilität ({effInstability:0.0}%): Die Ladung schwankt stark. Dämpfende Module (Focus/Torpid/Sabir) dringend empfohlen!");
        }

        // 6. Laser Head-to-Head Comparison
        var alternatives = new List<LaserAlternativeDto>();
        foreach (var candidate in Lasers.Where(l => l.Size == 1))
        {
            double candidateEffRes = Math.Clamp(rock.ResistancePercent * (1.0 + (candidate.Modifiers.Resistance + (gadget?.Modifiers.Resistance ?? 0)) / 100.0), 0.0, 98.0);
            double candDelivered = candidate.Power * (1.0 - candidateEffRes / 100.0);
            double candRatio = requiredPower > 0 ? candDelivered / requiredPower : 0;
            string candVerdict = candRatio >= SoloRatio ? "solo" : candRatio >= GadgetRatio ? "gadget" : "crew";
            double candMaxMass = Math.Round((candidate.Power * (1.0 - candidateEffRes / 100.0)) / (RequiredWattsPerKg * SoloRatio));

            alternatives.Add(new(
                candidate.Id,
                candidate.Name,
                candidate.Power,
                Math.Round(candDelivered),
                Math.Round(candRatio, 3),
                candVerdict,
                candMaxMass,
                Math.Round(candidateEffRes, 1)
            ));
        }

        return new CrackVerdictResult(
            Math.Round(deliveredPower),
            Math.Round(requiredPower),
            Math.Round(ratio, 3),
            verdict,
            verdictTitle,
            verdictBadge,
            verdictColor,
            Math.Round(effResistance, 1),
            Math.Round(effInstability, 1),
            Math.Round(window, 1),
            maxCrackableMass,
            Math.Round(energyCapacity),
            Math.Round(energyDecay, 1),
            notes,
            alternatives
        );
    }
}
