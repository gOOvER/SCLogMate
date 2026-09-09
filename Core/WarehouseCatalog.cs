using System;
using System.Collections.Generic;

namespace SCLogMate.Core;

/// <summary>
/// Katalog zur Übersetzung von internen CIG Item-Klassen in Klarschriftnamen und Kategorien.
/// </summary>
public static class WarehouseCatalog
{
    public static (string Name, string Category) Resolve(string itemClass)
    {
        if (string.IsNullOrWhiteSpace(itemClass))
            return ("Unbekannter Gegenstand", "Sonstiges");

        var raw = itemClass.Trim();

        // 1. Statisches Wörterbuch bekannter Items
        if (KnownItems.TryGetValue(raw, out var entry))
            return entry;

        // 2. Regelbasierte Erkennung nach bekannten Mustern
        var lower = raw.ToLowerInvariant();

        // Edelsteine / Harvestables
        if (lower.Contains("mineral") || lower.Contains("janalite") || lower.Contains("aphorite") ||
            lower.Contains("dolivine") || lower.Contains("hadanite") || lower.Contains("glacosite") ||
            lower.Contains("feynmaline") || lower.Contains("beradom") ||
            lower.Contains("revenant") || lower.Contains("harvestable"))
        {
            return (PrettifyHarvestable(raw), "Mineralien & Erze");
        }

        // Schiffskomponenten & Schiffsausrüstung
        if (lower.StartsWith("qdrv_") || lower.StartsWith("shld_") || lower.StartsWith("cool_") ||
            lower.StartsWith("powr_") || lower.StartsWith("jdrv_") || lower.Contains("_scitem"))
        {
            return (PrettifyShipComponent(raw), "Schiffsausrüstung");
        }

        // Werkzeuge & Module
        if (lower.Contains("multitool") || lower.Contains("tractor") || lower.Contains("mining") ||
            lower.Contains("salvage") || lower.Contains("repair") || lower.Contains("cambio") ||
            lower.Contains("fabricator"))
        {
            return (PrettifyTool(raw), "Werkzeuge & Module");
        }

        // Waffen & Munition
        if (lower.Contains("rifle") || lower.Contains("pistol") || lower.Contains("lmg") ||
            lower.Contains("smg") || lower.Contains("shotgun") || lower.Contains("sniper") ||
            lower.Contains("gren") || lower.Contains("mag") || lower.Contains("melee") ||
            lower.Contains("knife") || lower.Contains("weapon") || lower.Contains("optics"))
        {
            return (PrettifyWeapon(raw), "Waffen & Munition");
        }

        // Rüstung & Kleidung
        if (lower.Contains("helmet") || lower.Contains("undersuit") || lower.Contains("boots") ||
            lower.Contains("jacket") || lower.Contains("shirt") || lower.Contains("pants") ||
            lower.Contains("hat") || lower.Contains("glasses") || lower.Contains("torso") ||
            lower.Contains("arms") || lower.Contains("legs") || lower.Contains("backpack") ||
            lower.Contains("armor") || lower.StartsWith("alb_") || lower.StartsWith("ctl_") ||
            lower.StartsWith("drn_") || lower.StartsWith("scu_") || lower.StartsWith("r6p_") ||
            lower.StartsWith("cbd_") || lower.StartsWith("nvs_"))
        {
            return (PrettifyArmor(raw), "Rüstung & Kleidung");
        }

        // Medizin & Verpflegung
        if (lower.Contains("consumable") || lower.Contains("medpen") || lower.Contains("oxypen") ||
            lower.Contains("drink") || lower.Contains("bottle") || lower.Contains("food") ||
            lower.Contains("meal") || lower.Contains("pips") || lower.Contains("cruz") ||
            lower.Contains("can_") || lower.Contains("snack") || lower.Contains("tin_") ||
            lower.Contains("sachet_"))
        {
            return (PrettifyConsumable(raw), "Verbrauchsgüter");
        }

        // Quest, Utensilien, Sicherungen, Wertsachen
        if (lower.Contains("fuse") || lower.Contains("harddrive") || lower.Contains("extinguisher") ||
            lower.Contains("cryptokey") || lower.Contains("carryable") || lower.Contains("currency_bar") ||
            lower.Contains("medal") || lower.Contains("blackbox"))
        {
            return (PrettifyUtility(raw), "Quest & Wertsachen");
        }

        // Fallback: Generische Bereinigung
        return (FormatGeneric(raw), "Sonstiges");
    }

    private static string PrettifyShipComponent(string raw)
    {
        var clean = raw.Replace("_SCItem", "", StringComparison.OrdinalIgnoreCase);
        var parts = clean.Split('_');
        if (parts.Length >= 4)
        {
            var type = parts[0].ToUpperInvariant() switch
            {
                "QDRV" => "Quantenantrieb",
                "SHLD" => "Schildgenerator",
                "COOL" => "Kühler",
                "POWR" => "Kraftwerk",
                "JDRV" => "Sprungantrieb",
                _ => parts[0]
            };
            var size = parts[2].ToUpperInvariant();
            var name = parts[3];
            return $"{type} {name} ({size})";
        }
        return FormatGeneric(clean);
    }

    private static readonly Dictionary<string, (string Name, string Category)> KnownItems = new(StringComparer.OrdinalIgnoreCase)
    {
        // Edelsteine / Mineralien
        { "Harvestable_Mineral_1H_Janalite", ("Janalite (Edelstein)", "Mineralien & Erze") },
        { "Harvestable_Mineral_1H_Aphorite", ("Aphorite (Edelstein)", "Mineralien & Erze") },
        { "Harvestable_Mineral_1H_Dolivine", ("Dolivine (Edelstein)", "Mineralien & Erze") },
        { "Harvestable_Mineral_1H_Hadanite", ("Hadanite (Edelstein)", "Mineralien & Erze") },
        { "Harvestable_Mineral_1H_Glacosite", ("Glacosite (Edelstein)", "Mineralien & Erze") },
        { "Harvestable_Revenant", ("Revenant Tree Pod (Pflanze)", "Mineralien & Erze") },

        // Werkzeuge & Aufsätze
        { "grin_multitool_01", ("Greycat Multi-Tool", "Werkzeuge") },
        { "grin_multitool_01_tractorbeam", ("TruHold Traktorstrahl-Aufsatz", "Werkzeuge") },
        { "grin_multitool_01_salvage_repair", ("Cambio-Lite SRT Aufsatz", "Werkzeuge") },
        { "grin_multitool_resource_salvage_repair_01_filled", ("SRT Kanister (Gefüllt)", "Werkzeuge") },
        { "grin_multitool_01_mining", ("OreBit Mining-Aufsatz", "Werkzeuge") },
        { "grin_multitool_01_healing", ("Lifecure Med-Aufsatz", "Werkzeuge") },
        { "grin_tractor_01_msn_rwd01", ("MaxLift Traktorstrahl (Gelb)", "Werkzeuge") },
        { "grin_tractor_01_msn_rwd02", ("MaxLift Traktorstrahl (Spezial)", "Werkzeuge") },

        // Waffen & Granaten
        { "behr_gren_frag_01", ("Behring Granate (Splitter)", "Waffen") },
        { "klwe_pistol_energy_01", ("Klaus & Werner Arclight Pistole", "Waffen") },
        { "klwe_lmg_energy_01", ("Klaus & Werner Demeco LMG", "Waffen") },
        { "klwe_lmg_energy_01_mag", ("Demeco Energiemagazin", "Waffen") },
        { "gmni_pistol_ballistic_01", ("Gemini LH86 Pistole", "Waffen") },
        { "gmni_rifle_ballistic_01_mag", ("Gemini S71 Magazin", "Waffen") },
        { "gmni_lmg_ballistic_01_mag", ("Gemini F55 Magazin", "Waffen") },
        { "ksar_smg_energy_01", ("Kastak Arms Custodian SMG", "Waffen") },
        { "ksar_rifle_energy_01_mag", ("Kastak Arms Energiemagazin", "Waffen") },
        { "ksar_pistol_ballistic_01_mag", ("Kastak Arms Pistolenmagazin", "Waffen") },
        { "volt_rifle_energy_01", ("Volt Parallax Energiegewehr", "Waffen") },
        { "volt_pistol_energy_01", ("Volt Pistole", "Waffen") },
        { "banu_melee_04", ("Banu Messer (Klinge)", "Waffen") },

        // Medizin & Nahrung
        { "crlf_consumable_healing_01", ("MedPen (Heilung)", "Verbrauchsgüter") },
        { "crlf_consumable_oxygen_01", ("OxyPen (Sauerstoff)", "Verbrauchsgüter") },
        { "crlf_consumable_adrenaline_01", ("AdrenaPen (Geschwindigkeit)", "Verbrauchsgüter") },
        { "crlf_consumable_steroid_01", ("SteroidPen (Stärke)", "Verbrauchsgüter") },
        { "Drink_bottle_cruz_01_a", ("Cruz Flow (Getränk)", "Verbrauchsgüter") },
        { "Drink_bottle_cruz_01_lux_a", ("Cruz Lux (Getränk)", "Verbrauchsgüter") },
        { "Drink_bottle_vestal_01_a", ("Vestal Wasser", "Verbrauchsgüter") },
        { "Drink_bottle_water_old_2_a", ("Wasserflasche", "Verbrauchsgüter") },
        { "Drink_can_pips_01_t17_a", ("Pips Dose", "Verbrauchsgüter") },
        { "Drink_can_pips_01_q66_a", ("Pips Q66 Dose", "Verbrauchsgüter") },
        { "Drink_bottle_gg_01_detox_a", ("Green Gel Detox Getränk", "Verbrauchsgüter") },
        { "Food_bar_onemeal_01_a", ("OneMeal Riegel", "Verbrauchsgüter") },
        { "Food_bar_com_01_a", ("Kompakt-Riegel", "Verbrauchsgüter") },

        // Quest & Utility
        { "Fuse_subItem_standard", ("Standard-Sicherung (Fuse)", "Quest & Utility") },
        { "FPS_Consumable_HardDrive_Generic", ("Datenlaufwerk (Festplatte)", "Quest & Utility") },
        { "kegr_fire_extinguisher_01", ("Feuerlöscher", "Quest & Utility") }
    };

    private static string PrettifyHarvestable(string raw)
    {
        var clean = raw.Replace("Harvestable_Mineral_1H_", "")
                       .Replace("Harvestable_", "")
                       .Replace("_", " ");
        return $"{clean} (Edelstein)";
    }

    private static string PrettifyTool(string raw)
    {
        var clean = raw.Replace("grin_multitool_01_", "")
                       .Replace("grin_", "")
                       .Replace("_", " ");
        return $"Greycat {CapitalizeWords(clean)}";
    }

    private static string PrettifyWeapon(string raw)
    {
        var s = raw.Replace("klwe_", "Klaus & Werner ")
                   .Replace("behr_", "Behring ")
                   .Replace("gmni_", "Gemini ")
                   .Replace("ksar_", "Kastak Arms ")
                   .Replace("volt_", "Volt ")
                   .Replace("_mag", " Magazin")
                   .Replace("_", " ");
        return CapitalizeWords(s);
    }

    private static string PrettifyArmor(string raw)
    {
        var s = raw.Replace("rsi_", "RSI ")
                   .Replace("rrs_", "RRS ")
                   .Replace("gys_", "Geys ")
                   .Replace("gsb_", "GSB ")
                   .Replace("ctl_", "Castile ")
                   .Replace("_", " ");
        return CapitalizeWords(s);
    }

    private static string PrettifyConsumable(string raw)
    {
        var s = raw.Replace("crlf_consumable_", "")
                   .Replace("Drink_bottle_", "")
                   .Replace("Drink_can_", "")
                   .Replace("Food_bar_", "")
                   .Replace("_", " ");
        return CapitalizeWords(s);
    }

    private static string PrettifyUtility(string raw)
    {
        var s = raw.Replace("subItem_", "")
                   .Replace("FPS_Consumable_", "")
                   .Replace("_", " ");
        return CapitalizeWords(s);
    }

    private static string FormatGeneric(string raw)
    {
        var s = raw.Replace("_", " ");
        return CapitalizeWords(s);
    }

    private static string CapitalizeWords(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        var words = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpperInvariant(words[i][0]) + (words[i].Length > 1 ? words[i][1..] : "");
            }
        }
        return string.Join(" ", words);
    }
}
