using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SCLogMate.Core;

/// <summary>
/// SCMDB (scmdb.net) JSON Export Parser & Importer.
/// Parst { version, exportedAt, missions[], blueprints[] } Exporte,
/// validiert Grenzwerte (&lt;=5MB) und sortiert gelernte Einträge via Add-Only Plan ein.
/// </summary>
public static class ScmdbExportParser
{
    public const int MaxInputBytes = 5 * 1024 * 1024; // 5 MB

    private const string InvalidFormatError = "Diese Datei ist kein gültiger SCMDB-Export.";
    private const string TooLargeError = "Diese Datei ist zu groß für den Import (über 5 MB).";

    public sealed record Result(
        IReadOnlyList<string> CompletedNames,
        int SkippedNotCompleted,
        int MalformedEntries,
        int MissionCount,
        int Version,
        string? ExportedAt,
        bool NewerVersion,
        string? Error)
    {
        public bool Success => Error is null;
    }

    private static Result Failure(string error) =>
        new(Array.Empty<string>(), 0, 0, 0, 0, null, false, error);

    public static Result Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Failure(InvalidFormatError);
        if (Encoding.UTF8.GetByteCount(json) > MaxInputBytes) return Failure(TooLargeError);

        JsonDocument doc;
        try { doc = JsonDocument.Parse(json); }
        catch (JsonException) { return Failure(InvalidFormatError); }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return Failure(InvalidFormatError);
            if (!root.TryGetProperty("blueprints", out var blueprintsEl) || blueprintsEl.ValueKind != JsonValueKind.Array)
                return Failure(InvalidFormatError);

            int version = root.TryGetProperty("version", out var vEl) && vEl.ValueKind == JsonValueKind.Number
                && vEl.TryGetInt32(out var v) ? v : 0;
            string? exportedAt = root.TryGetProperty("exportedAt", out var eaEl) && eaEl.ValueKind == JsonValueKind.String
                ? eaEl.GetString() : null;
            int missionCount = root.TryGetProperty("missions", out var missionsEl) && missionsEl.ValueKind == JsonValueKind.Array
                ? missionsEl.GetArrayLength() : 0;

            var names = new List<string>();
            int skipped = 0, malformed = 0;
            foreach (var bp in blueprintsEl.EnumerateArray())
            {
                if (bp.ValueKind != JsonValueKind.Object) { malformed++; continue; }

                bool hasName = bp.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
                    && !string.IsNullOrEmpty(nameEl.GetString());
                bool hasCompleted = bp.TryGetProperty("completed", out var completedEl)
                    && (completedEl.ValueKind == JsonValueKind.True || completedEl.ValueKind == JsonValueKind.False);

                if (!hasName || !hasCompleted) { malformed++; continue; }
                if (completedEl.ValueKind == JsonValueKind.False) { skipped++; continue; }

                names.Add(nameEl.GetString()!);
            }

            return new Result(names, skipped, malformed, missionCount, version, exportedAt, version > 3, null);
        }
    }
}

/// <summary>
/// Headless Add-Only Bucketing für SCMDB Importe.
/// </summary>
public static class ScmdbImportPlan
{
    public sealed record Result(
        IReadOnlyList<string> ToImport,
        IReadOnlyList<string> AlreadyOwned,
        IReadOnlyList<string> Unrecognized);

    public static Result Build(
        IEnumerable<string> parsedNames,
        IReadOnlyCollection<string> ownedNames,
        Func<string, string?> resolveName)
    {
        var owned = new HashSet<string>(ownedNames, StringComparer.OrdinalIgnoreCase);
        var toImport = new List<string>();
        var alreadyOwned = new List<string>();
        var unrecognized = new List<string>();

        var seenResolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenUnrecognized = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in parsedNames)
        {
            var resolved = resolveName(raw);
            if (resolved is null)
            {
                if (seenUnrecognized.Add(raw)) unrecognized.Add(raw);
                continue;
            }
            if (!seenResolved.Add(resolved)) continue; // de-dupe
            (owned.Contains(resolved) ? alreadyOwned : toImport).Add(resolved);
        }

        return new Result(toImport, alreadyOwned, unrecognized);
    }
}

/// <summary>
/// Service für SCMDB Import/Export sowie Bauplan-Netzwerk- und Gap-Analyse.
/// </summary>
public static class ScmdbService
{
    private static readonly Dictionary<string, string> _canonicalLookup = new(StringComparer.OrdinalIgnoreCase);
    private static readonly System.Threading.Lock _lock = new();

    private static void EnsureLookup()
    {
        lock (_lock)
        {
            if (_canonicalLookup.Count > 0) return;
            var catalog = BlueprintCatalog.CreateFreshCatalog();
            foreach (var b in catalog)
            {
                _canonicalLookup[b.Name] = b.Name;
                _canonicalLookup[b.Id] = b.Name;
                
                var norm = BlueprintCatalog.NormalizeBlueprintName(b.Name);
                if (!string.IsNullOrEmpty(norm)) _canonicalLookup[norm] = b.Name;

                var noQuotes = norm.Replace("\"", "").Trim();
                if (!string.IsNullOrEmpty(noQuotes)) _canonicalLookup[noQuotes] = b.Name;
            }
        }
    }

    /// <summary>
    /// Löst einen rohen SCMDB-Namen auf einen bekannten kanonischen Bauplannamen auf.
    /// </summary>
    public static string? ResolveBlueprintName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        EnsureLookup();

        var trimmed = raw.Trim();
        if (_canonicalLookup.TryGetValue(trimmed, out var canonical)) return canonical;

        var norm = BlueprintCatalog.NormalizeBlueprintName(trimmed);
        if (_canonicalLookup.TryGetValue(norm, out canonical)) return canonical;

        var noQuotes = norm.Replace("\"", "").Trim();
        if (_canonicalLookup.TryGetValue(noQuotes, out canonical)) return canonical;

        // Fallback: Suffixe entfernen (z. B. "Camo", "Pistol", "Rifle")
        var match = _canonicalLookup.FirstOrDefault(kvp =>
            kvp.Key.StartsWith(noQuotes, StringComparison.OrdinalIgnoreCase) ||
            noQuotes.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(match.Value)) return match.Value;

        return null;
    }

    /// <summary>
    /// Erzeugt ein SCMDB v3 kompatibles JSON mit dem aktuellen Stand aller Baupläne.
    /// </summary>
    public static string GenerateScmdbExport(IEnumerable<BlueprintItem> items)
    {
        var list = items.ToList();
        var bpList = new List<object>();

        foreach (var item in list)
        {
            bpList.Add(new
            {
                tag = $"BP_CRAFT_{item.Id}",
                name = item.Name,
                url = $"https://scmdb.net/?page=fab&fab=BP_CRAFT_{item.Id}",
                completed = item.IsLearned,
                favorite = item.Rarity == "Legendär" || item.Rarity == "Episch"
            });
        }

        var root = new
        {
            version = 3,
            exportedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            missions = Array.Empty<object>(),
            blueprints = bpList
        };

        return JsonSerializer.Serialize(root, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Berechnet die Netzwerk- und Org-Abdeckung sowie Prioritäts-Gaps für Baupläne.
    /// </summary>
    public static BlueprintCoverageReport AnalyzeCoverage(IReadOnlyList<BlueprintItem> items)
    {
        int total = items.Count;
        int learned = items.Count(x => x.IsLearned);
        int missing = total - learned;
        double percent = total > 0 ? Math.Round((double)learned / total * 100.0, 1) : 0;

        // Kategorien
        var catGroups = items.GroupBy(x => x.Category).OrderByDescending(g => g.Count());
        var categories = new List<CategoryCoverage>();
        foreach (var g in catGroups)
        {
            int cTot = g.Count();
            int cLrn = g.Count(x => x.IsLearned);
            double cPct = cTot > 0 ? Math.Round((double)cLrn / cTot * 100.0, 1) : 0;
            var sample = g.First();
            categories.Add(new CategoryCoverage(
                g.Key,
                sample.CategoryIcon,
                cTot,
                cLrn,
                cPct
            ));
        }

        // Raritäten
        var rarityOrder = new[] { "Legendär", "Episch", "Selten", "Gewöhnlich" };
        var rarities = new List<RarityCoverage>();
        foreach (var r in rarityOrder)
        {
            var match = items.Where(x => string.Equals(x.Rarity, r, StringComparison.OrdinalIgnoreCase)).ToList();
            if (match.Count == 0) continue;
            int rTot = match.Count;
            int rLrn = match.Count(x => x.IsLearned);
            double rPct = rTot > 0 ? Math.Round((double)rLrn / rTot * 100.0, 1) : 0;
            var color = match.First().RarityColor;
            rarities.Add(new RarityCoverage(r, color, rTot, rLrn, rPct));
        }

        // Top Gaps: Legendäre, epische oder Schlüssel-Komponenten, die noch fehlen
        var topGaps = items
            .Where(x => !x.IsLearned)
            .OrderByDescending(x => GetGapPriority(x))
            .Take(12)
            .Select(x => new BlueprintGapItem(
                x.Id,
                x.Name,
                x.Category,
                x.SubCategory,
                x.Rarity,
                x.RarityColor,
                x.UnlockInfo,
                x.RequiredMaterials
            ))
            .ToList();

        // Org Craft-Rollen / Spezialisierungen
        var specializations = CalculateSpecializations(items);

        return new BlueprintCoverageReport(
            total,
            learned,
            missing,
            percent,
            categories,
            rarities,
            topGaps,
            specializations
        );
    }

    private static int GetGapPriority(BlueprintItem item)
    {
        int score = 0;
        if (item.Rarity == "Legendär") score += 100;
        else if (item.Rarity == "Episch") score += 70;
        else if (item.Rarity == "Selten") score += 40;
        else score += 10;

        if (item.Category == "Komponenten") score += 25;
        if (item.Name.Contains("Atlas", StringComparison.OrdinalIgnoreCase) ||
            item.Name.Contains("FR-", StringComparison.OrdinalIgnoreCase) ||
            item.Name.Contains("Novikov", StringComparison.OrdinalIgnoreCase) ||
            item.Name.Contains("Pembroke", StringComparison.OrdinalIgnoreCase) ||
            item.Name.Contains("FS-9", StringComparison.OrdinalIgnoreCase) ||
            item.Name.Contains("P8-SC", StringComparison.OrdinalIgnoreCase))
        {
            score += 50;
        }

        return score;
    }

    private static List<SpecializationProgress> CalculateSpecializations(IReadOnlyList<BlueprintItem> items)
    {
        var result = new List<SpecializationProgress>();

        void AddSpec(string title, string role, string icon, Func<BlueprintItem, bool> predicate, string focus)
        {
            var group = items.Where(predicate).ToList();
            int total = group.Count;
            if (total == 0) return;
            int learned = group.Count(x => x.IsLearned);
            double pct = Math.Round((double)learned / total * 100.0, 1);
            result.Add(new SpecializationProgress(title, role, icon, total, learned, pct, focus));
        }

        AddSpec(
            "Waffenschmied",
            "Gunsmith",
            "⚔️",
            x => x.Category == "Waffen",
            "Handfeuerwaffen, Scharfschützengewehre & Bordwaffen"
        );

        AddSpec(
            "Rüstungsmeister",
            "Armorsmith",
            "🛡️",
            x => x.Category == "Rüstung",
            "Schwere Kampfanzüge, Helme & Rucksäcke"
        );

        AddSpec(
            "Schiffs-Ingenieur",
            "Ship Systems Engineer",
            "⚙️",
            x => x.Category == "Komponenten",
            "Quantum-Drives, Schilde & Kühler"
        );

        AddSpec(
            "Überlebens-Experte",
            "Extreme Hazard Outfitter",
            "🌋",
            x => x.Name.Contains("Novikov", StringComparison.OrdinalIgnoreCase) ||
                 x.Name.Contains("Pembroke", StringComparison.OrdinalIgnoreCase) ||
                 x.Name.Contains("Defiance", StringComparison.OrdinalIgnoreCase),
            "Extrem-Umgebungs- und Umwelt-Rüstungen"
        );

        AddSpec(
            "Feldversorger",
            "Field Munitions & Tools",
            "🔋",
            x => x.Category == "Munition" || x.Category == "Werkzeuge",
            "Magazine, Traktorstrahl- & Multi-Tools"
        );

        return result;
    }
}

public sealed record CategoryCoverage(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("icon")] string Icon,
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("learned")] int Learned,
    [property: JsonPropertyName("percent")] double Percent
);

public sealed record RarityCoverage(
    [property: JsonPropertyName("rarity")] string Rarity,
    [property: JsonPropertyName("color")] string Color,
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("learned")] int Learned,
    [property: JsonPropertyName("percent")] double Percent
);

public sealed record BlueprintGapItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("subCategory")] string SubCategory,
    [property: JsonPropertyName("rarity")] string Rarity,
    [property: JsonPropertyName("rarityColor")] string RarityColor,
    [property: JsonPropertyName("unlockInfo")] string UnlockInfo,
    [property: JsonPropertyName("requiredMaterials")] string RequiredMaterials
);

public sealed record SpecializationProgress(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("icon")] string Icon,
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("learned")] int Learned,
    [property: JsonPropertyName("percent")] double Percent,
    [property: JsonPropertyName("focus")] string Focus
);

public sealed record BlueprintCoverageReport(
    [property: JsonPropertyName("total")] int Total,
    [property: JsonPropertyName("learned")] int Learned,
    [property: JsonPropertyName("missing")] int Missing,
    [property: JsonPropertyName("percent")] double Percent,
    [property: JsonPropertyName("categories")] IReadOnlyList<CategoryCoverage> Categories,
    [property: JsonPropertyName("rarities")] IReadOnlyList<RarityCoverage> Rarities,
    [property: JsonPropertyName("topGaps")] IReadOnlyList<BlueprintGapItem> TopGaps,
    [property: JsonPropertyName("specializations")] IReadOnlyList<SpecializationProgress> Specializations
);
