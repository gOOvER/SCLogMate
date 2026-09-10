using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SCLogMate.Core;

/// <summary>
/// Katalog zur Übersetzung von internen CIG Item-Klassen in Klarschriftnamen und Kategorien.
/// Integriert automatischen Star Citizen Wiki API Cache, asynchrones Nachladen und zweisprachige Lokalisierung (DE/EN).
/// </summary>
public static class WarehouseCatalog
{
    private static readonly ConcurrentDictionary<string, (string Name, string Category)> _cachedWikiNames = new(StringComparer.OrdinalIgnoreCase);
    private static bool _isWikiCacheLoaded;
    private static readonly System.Threading.Lock _cacheLock = new();

    private static void EnsureWikiCacheLoaded()
    {
        if (_isWikiCacheLoaded) return;
        lock (_cacheLock)
        {
            if (_isWikiCacheLoaded) return;
            try
            {
                var dbItems = Database.GetAllCachedWikiItemNames();
                foreach (var kvp in dbItems)
                {
                    _cachedWikiNames[kvp.Key] = kvp.Value;
                }
            }
            catch { }

            WikiApiClient.ItemResolved += (className, info) =>
            {
                _cachedWikiNames[className] = (info.Name, info.Category);
            };
            _isWikiCacheLoaded = true;
        }
    }

    public static (string Name, string Category) Resolve(string itemClass, bool? isGerman = null)
    {
        if (string.IsNullOrWhiteSpace(itemClass))
            return ("Unbekannter Gegenstand", "Sonstiges");

        var raw = itemClass.Trim();
        bool de = isGerman ?? I18n.Instance.IsGerman;

        EnsureWikiCacheLoaded();

        // 1. Aus dem persistenten Star Citizen Wiki Cache
        if (_cachedWikiNames.TryGetValue(raw, out var wikiEntry) && !string.IsNullOrWhiteSpace(wikiEntry.Name))
        {
            return wikiEntry;
        }

        // 2. Statisches Wörterbuch bekannter Items
        if (KnownItems.TryGetValue(raw, out var entry))
        {
            return (de ? entry.NameDe : entry.NameEn, de ? entry.CatDe : entry.CatEn);
        }

        // 3. Asynchron im Hintergrund bei star-citizen.wiki nachschlagen (Non-Blocking)
        WikiApiClient.EnqueueClassPrefetch(raw);

        // 4. Regelbasierte intelligente Aufbereitung & Lokalisierung
        var lower = raw.ToLowerInvariant();

        // Edelsteine / Harvestables
        if (lower.Contains("mineral") || lower.Contains("janalite") || lower.Contains("aphorite") ||
            lower.Contains("dolivine") || lower.Contains("hadanite") || lower.Contains("glacosite") ||
            lower.Contains("feynmaline") || lower.Contains("beradom") ||
            lower.Contains("revenant") || lower.Contains("harvestable") || lower.Contains("kopion"))
        {
            return (PrettifyHarvestable(raw, de), de ? "Mineralien & Erze" : "Minerals & Ores");
        }

        // Schiffskomponenten & Schiffsausrüstung
        if (lower.StartsWith("qdrv_") || lower.StartsWith("shld_") || lower.StartsWith("cool_") ||
            lower.StartsWith("powr_") || lower.StartsWith("jdrv_") || lower.Contains("_scitem"))
        {
            return (PrettifyShipComponent(raw, de), de ? "Schiffsausrüstung" : "Ship Equipment");
        }

        // Werkzeuge & Module
        if (lower.Contains("multitool") || lower.Contains("tractor") || lower.Contains("mining") ||
            lower.Contains("salvage") || lower.Contains("repair") || lower.Contains("cambio") ||
            lower.Contains("fabricator"))
        {
            return (PrettifyTool(raw, de), de ? "Werkzeuge & Module" : "Tools & Modules");
        }

        // Waffen & Munition
        if (lower.Contains("rifle") || lower.Contains("pistol") || lower.Contains("lmg") ||
            lower.Contains("smg") || lower.Contains("shotgun") || lower.Contains("sniper") ||
            lower.Contains("gren") || lower.Contains("mag") || lower.Contains("melee") ||
            lower.Contains("knife") || lower.Contains("weapon") || lower.Contains("optics"))
        {
            return (PrettifyWeapon(raw, de), de ? "Waffen & Munition" : "Weapons & Ammo");
        }

        // Rüstung & Kleidung
        if (lower.Contains("helmet") || lower.Contains("undersuit") || lower.Contains("boots") ||
            lower.Contains("jacket") || lower.Contains("shirt") || lower.Contains("pants") ||
            lower.Contains("hat") || lower.Contains("glasses") || lower.Contains("torso") ||
            lower.Contains("arms") || lower.Contains("legs") || lower.Contains("backpack") ||
            lower.Contains("armor") || lower.StartsWith("alb_") || lower.StartsWith("ctl_") ||
            lower.StartsWith("drn_") || lower.StartsWith("scu_") || lower.StartsWith("r6p_") ||
            lower.StartsWith("cbd_") || lower.StartsWith("nvs_") || lower.StartsWith("cba_") ||
            lower.StartsWith("cds_") || lower.StartsWith("grin_") || lower.StartsWith("rrs_") ||
            lower.StartsWith("kap_") || lower.StartsWith("gys_") || lower.StartsWith("gsb_") ||
            lower.StartsWith("eld_") || lower.StartsWith("fio_") || lower.StartsWith("hdh_") ||
            lower.StartsWith("ops_") || lower.StartsWith("oct_") || lower.StartsWith("ccc_") ||
            lower.StartsWith("qrt_") || lower.StartsWith("dmc_") || lower.StartsWith("987_"))
        {
            return (PrettifyArmor(raw, de), de ? "Rüstung & Kleidung" : "Armor & Clothing");
        }

        // Medizin & Verpflegung
        if (lower.Contains("consumable") || lower.Contains("medpen") || lower.Contains("oxypen") ||
            lower.Contains("drink") || lower.Contains("bottle") || lower.Contains("food") ||
            lower.Contains("meal") || lower.Contains("pips") || lower.Contains("cruz") ||
            lower.Contains("can_") || lower.Contains("snack") || lower.Contains("tin_") ||
            lower.Contains("sachet_"))
        {
            return (PrettifyConsumable(raw, de), de ? "Verbrauchsgüter" : "Consumables");
        }

        // Quest, Utensilien, Frachtkisten, Wertsachen
        if (lower.Contains("fuse") || lower.Contains("harddrive") || lower.Contains("extinguisher") ||
            lower.Contains("cryptokey") || lower.Contains("carryable") || lower.Contains("currency_bar") ||
            lower.Contains("medal") || lower.Contains("blackbox") || lower.Contains("inventorycontainer"))
        {
            return (PrettifyUtility(raw, de), de ? "Quest & Wertsachen" : "Quest & Valuables");
        }

        // Fallback: Generische Bereinigung
        return (FormatGeneric(raw), de ? "Sonstiges" : "Miscellaneous");
    }

    private static string PrettifyHarvestable(string raw, bool de)
    {
        var clean = raw.Replace("Harvestable_Mineral_1H_", "")
                       .Replace("Harvestable_Trophy_1H_", "")
                       .Replace("Harvestable_", "")
                       .Replace("Trophy_1H_", "")
                       .Replace("_", " ");

        if (clean.Contains("KopionHorn", StringComparison.OrdinalIgnoreCase))
            return clean.Contains("Irradiated", StringComparison.OrdinalIgnoreCase)
                ? (de ? "Kopion-Horn (Verstrahlt)" : "Kopion Horn (Irradiated)")
                : (de ? "Kopion-Horn" : "Kopion Horn");

        if (clean.Contains("AmioshiPlague", StringComparison.OrdinalIgnoreCase))
            return de ? "Amioshi-Pest (Sporen)" : "Amioshi Plague (Spores)";

        if (clean.Contains("SunsetBerry", StringComparison.OrdinalIgnoreCase))
            return de ? "Sunset Berry (Frucht)" : "Sunset Berry (Fruit)";

        if (clean.Contains("Pitambu", StringComparison.OrdinalIgnoreCase))
            return de ? "Pitambu (Frucht)" : "Pitambu (Fruit)";

        if (clean.Contains("Stonebug", StringComparison.OrdinalIgnoreCase))
            return de ? "Stonebug (Insekt)" : "Stonebug (Insect)";

        if (clean.Contains("Janalite", StringComparison.OrdinalIgnoreCase))
            return de ? "Janalite (Edelstein)" : "Janalite (Gem)";

        if (clean.Contains("Aphorite", StringComparison.OrdinalIgnoreCase))
            return de ? "Aphorite (Edelstein)" : "Aphorite (Gem)";

        if (clean.Contains("Dolivine", StringComparison.OrdinalIgnoreCase))
            return de ? "Dolivine (Edelstein)" : "Dolivine (Gem)";

        if (clean.Contains("Hadanite", StringComparison.OrdinalIgnoreCase))
            return de ? "Hadanite (Edelstein)" : "Hadanite (Gem)";

        if (clean.Contains("Feynmaline", StringComparison.OrdinalIgnoreCase))
            return de ? "Feynmaline (Edelstein)" : "Feynmaline (Gem)";

        if (clean.Contains("Beradom", StringComparison.OrdinalIgnoreCase))
            return de ? "Beradom (Edelstein)" : "Beradom (Gem)";

        return CapitalizeWords(clean);
    }

    private static string PrettifyShipComponent(string raw, bool de)
    {
        var clean = raw.Replace("_SCItem", "", StringComparison.OrdinalIgnoreCase);
        var parts = clean.Split('_');
        if (parts.Length >= 4)
        {
            var type = parts[0].ToUpperInvariant() switch
            {
                "QDRV" => de ? "Quantenantrieb" : "Quantum Drive",
                "SHLD" => de ? "Schildgenerator" : "Shield Generator",
                "COOL" => de ? "Kühler" : "Cooler",
                "POWR" => de ? "Kraftwerk" : "Power Plant",
                "JDRV" => de ? "Sprungantrieb" : "Jump Drive",
                _ => parts[0]
            };
            var size = parts[2].ToUpperInvariant();
            var name = parts[3];
            return $"{type} {name} ({size})";
        }
        return FormatGeneric(clean);
    }

    private static string PrettifyTool(string raw, bool de)
    {
        var s = raw.Replace("_SCItem", "")
                   .Replace("ItemFabricator_", de ? "Material-Fabrikator " : "Material Fabricator ")
                   .Replace("_", " ");

        return CapitalizeWords(s);
    }

    private static string PrettifyWeapon(string raw, bool de)
    {
        var s = raw.Replace("_SCItem", "")
                   .Replace("behr_", "Behring ")
                   .Replace("klwe_", "Klaus & Werner ")
                   .Replace("ksar_", "Kastak Arms ")
                   .Replace("gemini_", "Gemini ")
                   .Replace("volt_", "Volt ")
                   .Replace("_", " ");

        return CapitalizeWords(s);
    }

    private static string PrettifyArmor(string raw, bool de)
    {
        var lower = raw.ToLowerInvariant();

        // 1. Hersteller / Marke ermitteln
        var mfg = "Unbekannt";
        if (lower.StartsWith("alb_")) mfg = "Alb";
        else if (lower.StartsWith("ctl_")) mfg = "Castile";
        else if (lower.StartsWith("cba_")) mfg = "CBA";
        else if (lower.StartsWith("cds_")) mfg = "CDS";
        else if (lower.StartsWith("grin_")) mfg = "Greycat";
        else if (lower.StartsWith("rrs_")) mfg = "RRS";
        else if (lower.StartsWith("kap_")) mfg = "Kastak";
        else if (lower.StartsWith("gys_")) mfg = "G-2";
        else if (lower.StartsWith("gsb_")) mfg = "GSB";
        else if (lower.StartsWith("eld_")) mfg = "Elroy";
        else if (lower.StartsWith("fio_")) mfg = "Fiolina";
        else if (lower.StartsWith("hdh_")) mfg = "HDH";
        else if (lower.StartsWith("drn_")) mfg = "Derelict";
        else if (lower.StartsWith("ops_")) mfg = "OPS";
        else if (lower.StartsWith("oct_")) mfg = "Oct";
        else if (lower.StartsWith("r6p_")) mfg = "R6P";
        else if (lower.StartsWith("nvs_")) mfg = "Novikov";
        else if (lower.StartsWith("ccc_")) mfg = "Caldera";
        else if (lower.StartsWith("qrt_")) mfg = "Quirinus";
        else if (lower.StartsWith("dmc_")) mfg = "DMC";
        else if (lower.StartsWith("987_")) mfg = "987";
        else if (lower.StartsWith("rsi_")) mfg = "RSI";

        // 2. Kleidungsstück / Rüstungsteil ermitteln
        var itemType = "";
        if (lower.Contains("hat") || lower.Contains("beanie")) itemType = de ? "Mütze/Hut" : "Hat";
        else if (lower.Contains("jacket")) itemType = de ? "Jacke" : "Jacket";
        else if (lower.Contains("shirt")) itemType = de ? "Hemd" : "Shirt";
        else if (lower.Contains("pants")) itemType = de ? "Hose" : "Pants";
        else if (lower.Contains("boots") || lower.Contains("shoes")) itemType = de ? "Stiefel" : "Boots";
        else if (lower.Contains("glasses")) itemType = de ? "Brille" : "Glasses";
        else if (lower.Contains("helmet")) itemType = de ? "Helm" : "Helmet";
        else if (lower.Contains("core") || lower.Contains("torso")) itemType = de ? "Brustpanzer" : "Core Armor";
        else if (lower.Contains("arms")) itemType = de ? "Armschützer" : "Arms Armor";
        else if (lower.Contains("legs")) itemType = de ? "Beinschützer" : "Legs Armor";
        else if (lower.Contains("backpack")) itemType = de ? "Rucksack" : "Backpack";
        else if (lower.Contains("undersuit")) itemType = de ? "Unteranzug" : "Undersuit";
        else if (lower.Contains("jumpsuit")) itemType = de ? "Overall" : "Jumpsuit";
        else itemType = de ? "Kleidung" : "Apparel";

        // 3. Rüstungs-Klasse (Schwer, Mittel, Leicht)
        var weight = "";
        if (lower.Contains("heavy")) weight = de ? "Schwer" : "Heavy";
        else if (lower.Contains("medium")) weight = de ? "Mittel" : "Medium";
        else if (lower.Contains("light")) weight = de ? "Leicht" : "Light";

        // 4. Varianten & Farbcodes extrahieren (z.B. _01_01_17 oder _iae2022_01)
        var variantSuffix = "";
        if (lower.Contains("iae2022"))
        {
            variantSuffix = "(IAE 2952)";
        }
        else
        {
            var match = Regex.Match(raw, @"_(\d{2})_(\d{2})_(\d{2})$");
            if (match.Success)
            {
                var design = match.Groups[1].Value;
                var color = match.Groups[3].Value;
                variantSuffix = de ? $"(Design {design} · Farbe {color})" : $"(Design {design} · Color {color})";
            }
            else
            {
                var matchSimple = Regex.Match(raw, @"_(\d{2})_(\d{2})$");
                if (matchSimple.Success)
                {
                    var design = matchSimple.Groups[1].Value;
                    variantSuffix = de ? $"(Variante {design})" : $"(Variant {design})";
                }
            }
        }

        var parts = new List<string> { mfg };
        if (!string.IsNullOrEmpty(weight)) parts.Add(weight);
        parts.Add(itemType);
        if (!string.IsNullOrEmpty(variantSuffix)) parts.Add(variantSuffix);

        return string.Join(" ", parts).Trim();
    }

    private static string PrettifyConsumable(string raw, bool de)
    {
        var s = raw.Replace("crlf_consumable_", "")
                   .Replace("Drink_bottle_", "")
                   .Replace("Drink_can_", "")
                   .Replace("Food_bar_", "")
                   .Replace("_", " ");
        return CapitalizeWords(s);
    }

    private static string PrettifyUtility(string raw, bool de)
    {
        if (raw.Contains("blackbox", StringComparison.OrdinalIgnoreCase))
            return de ? "Flugschreiber / Blackbox (Missionsgut)" : "Flight Recorder / Blackbox (Mission Cargo)";

        if (raw.Contains("CY_medal_1_pristine", StringComparison.OrdinalIgnoreCase))
            return de ? "CY Bürgermedaille (Makellos)" : "CY Citizen Medal (Pristine)";

        if (raw.Contains("CY_medal", StringComparison.OrdinalIgnoreCase))
            return de ? "CY Bürgermedaille" : "CY Citizen Medal";

        if (raw.Contains("currency_bar", StringComparison.OrdinalIgnoreCase))
            return de ? "aUEC Währungsbarren (Makellos)" : "aUEC Currency Bar (Pristine)";

        if (raw.Contains("plushie_penguin", StringComparison.OrdinalIgnoreCase))
            return de ? "Pico der Pinguin (Plüschtier)" : "Pico the Penguin (Plushie)";

        if (raw.Contains("InventoryContainer_1SCU", StringComparison.OrdinalIgnoreCase))
            return de ? "Frachtcontainer (1 SCU)" : "Cargo Container (1 SCU)";

        if (raw.Contains("InventoryContainer_2SCU", StringComparison.OrdinalIgnoreCase))
            return de ? "Frachtcontainer (2 SCU)" : "Cargo Container (2 SCU)";

        if (raw.Contains("medical_samples", StringComparison.OrdinalIgnoreCase))
            return de ? "Medizinische Proben (Missionsgut)" : "Medical Samples (Mission Cargo)";

        if (raw.Contains("metal_oresamples", StringComparison.OrdinalIgnoreCase))
            return de ? "Erzproben (Missionsgut)" : "Ore Samples (Mission Cargo)";

        if (raw.Contains("Glowstick", StringComparison.OrdinalIgnoreCase))
        {
            var color = raw.Contains("Blue") ? (de ? "Blau" : "Blue") :
                        raw.Contains("Green") ? (de ? "Grün" : "Green") :
                        raw.Contains("Pink") ? (de ? "Pink" : "Pink") : (de ? "Standard" : "Standard");
            return de ? $"Knicklicht ({color})" : $"Glowstick ({color})";
        }

        var s = raw.Replace("subItem_", "")
                   .Replace("FPS_Consumable_", "")
                   .Replace("Carryable_1H_", "")
                   .Replace("Carryable_2H_", "")
                   .Replace("Carryable_", "")
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

    private static readonly Dictionary<string, (string NameDe, string NameEn, string CatDe, string CatEn)> KnownItems = new(StringComparer.OrdinalIgnoreCase)
    {
        // Edelsteine / Mineralien
        { "Harvestable_Mineral_1H_Janalite", ("Janalite (Edelstein)", "Janalite (Gem)", "Mineralien & Erze", "Minerals & Ores") },
        { "Harvestable_Mineral_1H_Aphorite", ("Aphorite (Edelstein)", "Aphorite (Gem)", "Mineralien & Erze", "Minerals & Ores") },
        { "Harvestable_Mineral_1H_Dolivine", ("Dolivine (Edelstein)", "Dolivine (Gem)", "Mineralien & Erze", "Minerals & Ores") },
        { "Harvestable_Mineral_1H_Hadanite", ("Hadanite (Edelstein)", "Hadanite (Gem)", "Mineralien & Erze", "Minerals & Ores") },
        { "Harvestable_Mineral_1H_Feynmaline", ("Feynmaline (Edelstein)", "Feynmaline (Gem)", "Mineralien & Erze", "Minerals & Ores") },
        { "Harvestable_Mineral_1H_Beradom", ("Beradom (Edelstein)", "Beradom (Gem)", "Mineralien & Erze", "Minerals & Ores") },
        { "Harvestable_Mineral_1H_Glacosite", ("Glacosite (Edelstein)", "Glacosite (Gem)", "Mineralien & Erze", "Minerals & Ores") },
        { "Harvestable_Revenant", ("Revenant Tree Pod (Pflanze)", "Revenant Tree Pod (Plant)", "Mineralien & Erze", "Minerals & Ores") },

        // Werkzeuge
        { "multitool_truhold", ("TruHold Traktorstrahl", "TruHold Tractor Beam", "Werkzeuge & Module", "Tools & Modules") },
        { "multitool_maxlift", ("MaxLift Traktorstrahl", "MaxLift Tractor Beam", "Werkzeuge & Module", "Tools & Modules") },
        { "multitool_cambio_lite", ("Cambio-Lite Erz-Aufsatz", "Cambio-Lite Mining Attachment", "Werkzeuge & Module", "Tools & Modules") },
        { "multitool_orebit", ("OreBit Bergbau-Aufsatz", "OreBit Mining Attachment", "Werkzeuge & Module", "Tools & Modules") },
        { "multitool_lifecure", ("LifeCure Med-Aufsatz", "LifeCure Med Attachment", "Werkzeuge & Module", "Tools & Modules") },

        // Quest & Utility
        { "Fuse_subItem_standard", ("Standard-Sicherung", "Standard Fuse", "Quest & Wertsachen", "Quest & Valuables") },
        { "FPS_Consumable_HardDrive_Generic", ("Datenlaufwerk (Festplatte)", "Data Drive (Hard Drive)", "Quest & Wertsachen", "Quest & Valuables") },
        { "kegr_fire_extinguisher_01", ("Feuerlöscher", "Fire Extinguisher", "Quest & Wertsachen", "Quest & Valuables") },
        { "cryptokey_tigerclaw", ("Tigerclaw Krypto-Stick", "Tigerclaw Cryptokey", "Quest & Wertsachen", "Quest & Valuables") },

        // Medizin & Verbrauchsgüter
        { "crlf_consumable_medpen", ("Hemozal MedPen", "Hemozal MedPen", "Verbrauchsgüter", "Consumables") },
        { "crlf_consumable_oxypen", ("Oxypen (Sauerstoff)", "Oxypen (Oxygen)", "Verbrauchsgüter", "Consumables") }
    };
}
