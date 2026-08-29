using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using SCLogReader.Models;

namespace SCLogReader.Core;

public partial class BlueprintItem : ObservableObject
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "Waffen"; // Waffen, Rüstung, Komponenten, Munition, Werkzeuge
    public string SubCategory { get; set; } = "";
    public string Rarity { get; set; } = "Gewöhnlich";
    public string Description { get; set; } = "";
    public string RequiredMaterials { get; set; } = "";
    public string UnlockInfo { get; set; } = "Loot / Mission";
    
    [ObservableProperty] private bool isLearned;
    [ObservableProperty] private DateTime? learnedAt;

    public string LearnedDateText => IsLearned && LearnedAt.HasValue 
        ? $"Erlernt am {LearnedAt.Value:dd.MM.yyyy HH:mm}" 
        : IsLearned ? "Erlernt" : "Noch nicht erlernt";

    public string LearnedDateShort => LearnedAt.HasValue ? LearnedAt.Value.ToString("dd.MM.yyyy") : "";

    public string StatusColor => IsLearned ? "#3FB950" : "#8B949E";
    public string StatusBgColor => IsLearned ? "#0F2818" : "#161B22";
    public string StatusBorderColor => IsLearned ? "#238636" : "#30363D";
    public string StatusTextShort => IsLearned ? "Erlernt" : "Fehlt";
    public string StatusBadge => IsLearned ? "✓ Erlernt" : "✕ Fehlt";
    
    public string CategoryBadge => !string.IsNullOrEmpty(SubCategory) 
        ? $"{CategoryIcon} {Category} · {SubCategory}" 
        : $"{CategoryIcon} {Category}";

    public string CategoryIcon => Category switch
    {
        "Waffen" => "⚔",
        "Rüstung" => "🛡",
        "Komponenten" => "⚙",
        "Munition" => "🔋",
        "Werkzeuge" => "🔧",
        "Medizin" => "💉",
        _ => "📦"
    };

    public string RarityColor => Rarity switch
    {
        "Legendär" => "#F59E0B",
        "Episch" => "#A855F7",
        "Selten" => "#38BDF8",
        _ => "#94A3B8"
    };
}

public static class BlueprintCatalog
{
    private static List<BlueprintItem>? _cachedCatalog;

    public static List<BlueprintItem> CreateFreshCatalog()
    {
        if (_cachedCatalog != null && _cachedCatalog.Count > 0)
        {
            return _cachedCatalog.Select(CloneItem).ToList();
        }

        var items = new List<BlueprintItem>();
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream("SCLogReader.Data.blueprints.json");
            if (stream != null)
            {
                using var doc = JsonDocument.Parse(stream);
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    var name = el.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var catRaw = el.TryGetProperty("category", out var c) ? c.GetString() ?? "Weapons" : "Weapons";
                    var subRaw = el.TryGetProperty("subCategory", out var sc) ? sc.GetString() ?? "" : "";
                    
                    var (catDe, subDe) = TranslateCategory(catRaw, subRaw, name);

                    var ingList = new List<string>();
                    if (el.TryGetProperty("ingredients", out var ings) && ings.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var ing in ings.EnumerateArray())
                        {
                            var res = ing.TryGetProperty("resourceName", out var rn) ? rn.GetString() ?? "" : "";
                            var qty = ing.TryGetProperty("quantity", out var q) ? q.GetDouble() : 0;
                            var unit = ing.TryGetProperty("unit", out var u) ? u.GetString() ?? "SCU" : "SCU";
                            if (!string.IsNullOrEmpty(res))
                            {
                                var qtyStr = qty < 1 ? qty.ToString("0.##") : qty.ToString("N0");
                                ingList.Add($"{qtyStr} {unit} {res}");
                            }
                        }
                    }

                    var unlockFaction = el.TryGetProperty("unlockFaction", out var uf) ? uf.GetString() : null;
                    var unlockMission = el.TryGetProperty("unlockMission", out var um) ? um.GetString() : null;
                    var unlockRep = el.TryGetProperty("unlockRep", out var ur) ? ur.GetString() : null;

                    string unlockInfo = "Loot / Mission";
                    if (!string.IsNullOrEmpty(unlockFaction))
                    {
                        unlockInfo = !string.IsNullOrEmpty(unlockRep) 
                            ? $"{unlockFaction} (Rang {unlockRep})" 
                            : unlockFaction;
                    }
                    else if (!string.IsNullOrEmpty(unlockMission))
                    {
                        unlockInfo = unlockMission;
                    }

                    items.Add(new BlueprintItem
                    {
                        Id = name.ToLowerInvariant().Replace(" ", "_"),
                        Name = name,
                        Category = catDe,
                        SubCategory = subDe,
                        RequiredMaterials = ingList.Count > 0 ? string.Join(" · ", ingList) : "—",
                        UnlockInfo = unlockInfo,
                        Rarity = DetermineRarity(name, catDe, ingList.Count),
                        IsLearned = false
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Fehler beim Laden von blueprints.json: {ex.Message}");
        }

        if (items.Count == 0)
        {
            items = FallbackCatalog();
        }

        _cachedCatalog = items;
        return _cachedCatalog.Select(CloneItem).ToList();
    }

    private static BlueprintItem CloneItem(BlueprintItem b) => new()
    {
        Id = b.Id,
        Name = b.Name,
        Category = b.Category,
        SubCategory = b.SubCategory,
        Rarity = b.Rarity,
        Description = b.Description,
        RequiredMaterials = b.RequiredMaterials,
        UnlockInfo = b.UnlockInfo,
        IsLearned = b.IsLearned,
        LearnedAt = b.LearnedAt
    };

    private static (string Category, string SubCategory) TranslateCategory(string cat, string sub, string name)
    {
        string catDe = cat switch
        {
            "Armor" => "Rüstung",
            "Weapons" => "Waffen",
            "Ship Components" => "Komponenten",
            "Ammo" => "Munition",
            _ => cat
        };

        string subDe = sub switch
        {
            "Cooler" => "Kühler",
            "Power Plant" => "Kraftwerk",
            "Quantum Drive" => "Quantum-Antrieb",
            "Radar" => "Radar",
            "Shield" => "Schildgenerator",
            "Mining Laser" => "Bergbau-Laser",
            "Salvage" => "Verwertung",
            "Ship Weapon" => "Bordwaffe",
            "Tractor Beam" => "Traktorstrahl",
            _ => sub
        };

        if (catDe == "Rüstung" && string.IsNullOrEmpty(subDe))
        {
            if (name.Contains("Helmet", StringComparison.OrdinalIgnoreCase)) subDe = "Helm";
            else if (name.Contains("Core", StringComparison.OrdinalIgnoreCase) || name.Contains("Torso", StringComparison.OrdinalIgnoreCase)) subDe = "Torso";
            else if (name.Contains("Arms", StringComparison.OrdinalIgnoreCase)) subDe = "Arme";
            else if (name.Contains("Legs", StringComparison.OrdinalIgnoreCase)) subDe = "Beine";
            else if (name.Contains("Backpack", StringComparison.OrdinalIgnoreCase)) subDe = "Rucksack";
            else if (name.Contains("Undersuit", StringComparison.OrdinalIgnoreCase)) subDe = "Unteranzug";
        }

        return (catDe, subDe);
    }

    private static string DetermineRarity(string name, string category, int ingredientCount)
    {
        if (ingredientCount >= 5 || name.Contains("Executive", StringComparison.OrdinalIgnoreCase) || name.Contains("Novikov", StringComparison.OrdinalIgnoreCase) || name.Contains("Pembroke", StringComparison.OrdinalIgnoreCase))
            return "Legendär";
        if (ingredientCount >= 4 || name.Contains("Citadel", StringComparison.OrdinalIgnoreCase) || name.Contains("Scalpel", StringComparison.OrdinalIgnoreCase) || name.Contains("Yubarev", StringComparison.OrdinalIgnoreCase))
            return "Episch";
        if (ingredientCount >= 3 || category == "Komponenten")
            return "Selten";
        return "Gewöhnlich";
    }

    public static void Sync(System.Collections.ObjectModel.ObservableCollection<BlueprintItem> catalog, IEnumerable<LogEntry> events)
    {
        var learnedMap = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        foreach (var ev in events)
        {
            if (ev.Kind == EventKind.Blueprint ||
                ev.Detail.Contains("Bauplan", StringComparison.OrdinalIgnoreCase) ||
                ev.Detail.Contains("Blueprint", StringComparison.OrdinalIgnoreCase))
            {
                var clean = NormalizeBlueprintName(ev.Detail);
                if (!string.IsNullOrEmpty(clean))
                {
                    if (!learnedMap.ContainsKey(clean) || ev.Time < learnedMap[clean])
                    {
                        learnedMap[clean] = ev.Time;
                    }
                }
            }
        }

        var matchedLogKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Phase 1: Exakte Namensübereinstimmung (strikt, damit Arrowhead Rifle nicht mit Arrowhead Battery verwechselt wird)
        foreach (var item in catalog)
        {
            var itemNorm = NormalizeBlueprintName(item.Name);
            var itemNoQuotes = itemNorm.Replace("\"", "").Trim();

            // 1.1 Exakter Name
            if (learnedMap.TryGetValue(item.Name, out var dt) ||
                learnedMap.TryGetValue(itemNorm, out dt))
            {
                item.IsLearned = true;
                item.LearnedAt = dt;
                matchedLogKeys.Add(item.Name);
                matchedLogKeys.Add(itemNorm);
                continue;
            }

            // 1.2 Exakt ohne Anführungszeichen (z. B. Skinnamen)
            var matchNoQuotes = learnedMap.FirstOrDefault(kvp => 
                string.Equals(kvp.Key.Replace("\"", "").Trim(), itemNoQuotes, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(matchNoQuotes.Key))
            {
                item.IsLearned = true;
                item.LearnedAt = matchNoQuotes.Value;
                matchedLogKeys.Add(matchNoQuotes.Key);
                continue;
            }
        }

        // Phase 2: Strukturierter Fallback für Varianten (Skins / Camo-Suffixe / Fraktions-Tags)
        foreach (var item in catalog)
        {
            if (item.IsLearned) continue;

            var itemNorm = NormalizeBlueprintName(item.Name);
            var itemNoQuotes = itemNorm.Replace("\"", "").Trim();

            var match = learnedMap.FirstOrDefault(kvp =>
            {
                var k = kvp.Key;
                // Camo in Anführungszeichen entfernen, z. B. 'BR-2 "Purgatory Camo" Shotgun' -> 'BR-2 Shotgun'
                var kStripped = System.Text.RegularExpressions.Regex.Replace(k, "\"([^\"]+)\"", "").Trim();
                kStripped = System.Text.RegularExpressions.Regex.Replace(kStripped, @"\s+", " ");
                if (string.Equals(kStripped, itemNorm, StringComparison.OrdinalIgnoreCase)) return true;

                // Rüstungs-Camo-Suffixe entfernen, z. B. 'Chiron Arms Purgatory Camo' -> 'Chiron Arms'
                var kArmorBase = System.Text.RegularExpressions.Regex.Replace(k, @"\s+(Purgatory Camo|Levski Edition|Shooting Star|Tactical|Grey|Black)$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
                if (string.Equals(kArmorBase, itemNorm, StringComparison.OrdinalIgnoreCase)) return true;

                return false;
            });

            if (!string.IsNullOrEmpty(match.Key))
            {
                item.IsLearned = true;
                item.LearnedAt = match.Value;
                matchedLogKeys.Add(match.Key);
            }
        }

        // Phase 3: Unbekannte Baupläne aus den Logs, die in keinem Katalog vorhanden sind, dynamisch mit abgeleiteten Ressourcen anlegen
        foreach (var kvp in learnedMap)
        {
            if (!matchedLogKeys.Contains(kvp.Key))
            {
                var cleanName = kvp.Key;
                // Prüfen ob bereits in der Liste
                if (catalog.Any(x => string.Equals(x.Name, cleanName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var (cat, sub) = InferCategoryFromName(cleanName);
                var mats = InferMaterialsFromName(cleanName, cat);

                catalog.Insert(0, new BlueprintItem
                {
                    Id = cleanName.ToLowerInvariant().Replace(" ", "_"),
                    Name = cleanName,
                    Category = cat,
                    SubCategory = sub,
                    RequiredMaterials = mats,
                    UnlockInfo = "Loot / Mission",
                    Rarity = "Selten",
                    IsLearned = true,
                    LearnedAt = kvp.Value
                });
            }
        }
    }

    private static string InferMaterialsFromName(string name, string category)
    {
        var lower = name.ToLowerInvariant();
        if (category == "Waffen" || lower.Contains("rifle") || lower.Contains("pistol") || lower.Contains("shotgun"))
            return "2 units Aphorite · 0.02 SCU Copper · 0.04 SCU Iron";
        if (category == "Rüstung" || lower.Contains("helmet") || lower.Contains("armor") || lower.Contains("suit"))
            return "0.02 SCU Aslarite · 0.04 SCU Laranite · 0.04 SCU Tungsten";
        if (category == "Komponenten" || lower.Contains("module") || lower.Contains("laser") || lower.Contains("scraper"))
            return "0.05 SCU Copper · 0.05 SCU Iron · 5 units Sadaryx";
        if (category == "Munition" || lower.Contains("battery") || lower.Contains("magazine"))
            return "0.02 SCU Hephestanite · 0.02 SCU Quartz";
        return "0.02 SCU Copper · 0.04 SCU Iron";
    }

    public static string NormalizeBlueprintName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var s = raw.Trim();
        if (s.StartsWith("Bauplan:", StringComparison.OrdinalIgnoreCase)) s = s["Bauplan:".Length..].Trim();
        if (s.StartsWith("Blueprint:", StringComparison.OrdinalIgnoreCase)) s = s["Blueprint:".Length..].Trim();
        if (s.StartsWith("Received Blueprint:", StringComparison.OrdinalIgnoreCase)) s = s["Received Blueprint:".Length..].Trim();
        if (s.StartsWith("Bauplan erhalten:", StringComparison.OrdinalIgnoreCase)) s = s["Bauplan erhalten:".Length..].Trim();
        
        // StarStrings prefix: "Ind/2/B Sedulity" -> "Sedulity"
        s = System.Text.RegularExpressions.Regex.Replace(s, @"^[A-Za-z]+/\d+/[A-Za-z]+\s+", "");
        
        // StarStrings/Component suffix: "Sedulity (Ind/2/B)" or "Citadel (S2 B Industrial)" -> "Sedulity", "Citadel"
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\s*\([A-Za-z0-9/\s]+\)", "");
        
        return s.Trim().TrimEnd(':').Trim();
    }

    private static (string Category, string SubCategory) InferCategoryFromName(string name)
    {
        var lower = name.ToLowerInvariant();
        if (lower.Contains("rifle") || lower.Contains("pistol") || lower.Contains("shotgun") || 
            lower.Contains("smg") || lower.Contains("knife") || lower.Contains("gewehr") || 
            lower.Contains("waffe") || lower.Contains("cannon") || lower.Contains("repeater"))
        {
            return ("Waffen", "Handfeuerwaffe");
        }
        if (lower.Contains("helmet") || lower.Contains("helm") || lower.Contains("core") || 
            lower.Contains("torso") || lower.Contains("arms") || lower.Contains("legs") || 
            lower.Contains("suit") || lower.Contains("armor") || lower.Contains("rüstung") || 
            lower.Contains("backpack") || lower.Contains("rucksack"))
        {
            return ("Rüstung", "Kampfpanzerung");
        }
        if (lower.Contains("magazine") || lower.Contains("battery") || lower.Contains("magazin") || lower.Contains("munition"))
        {
            return ("Munition", "Magazin");
        }
        if (lower.Contains("cooler") || lower.Contains("shield") || lower.Contains("drive") || 
            lower.Contains("quantum") || lower.Contains("power") || lower.Contains("kühler") || 
            lower.Contains("schild") || lower.Contains("kraftwerk"))
        {
            return ("Komponenten", "Schiffskomponente");
        }
        return ("Waffen", "");
    }

    private static List<BlueprintItem> FallbackCatalog() => new()
    {
        new() { Id = "bp_p8sc", Name = "P8-SC SMG", Category = "Waffen", Rarity = "Gewöhnlich", RequiredMaterials = "Titan · Wolfram · Verbundwerkstoff" },
        new() { Id = "bp_fs9", Name = "FS-9 LMG", Category = "Waffen", Rarity = "Selten", RequiredMaterials = "Titan · Wolfram · RMC" },
        new() { Id = "bp_defiance", Name = "Defiance Armor", Category = "Rüstung", SubCategory = "Torso", Rarity = "Selten", RequiredMaterials = "Titan · Wolfram · RMC" },
        new() { Id = "bp_atlas", Name = "Atlas Quantum Drive", Category = "Komponenten", SubCategory = "Quantum-Antrieb", Rarity = "Selten", RequiredMaterials = "Quantanium · Supraleiter" }
    };
}
