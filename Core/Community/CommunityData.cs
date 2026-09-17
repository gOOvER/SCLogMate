using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SCLogMate.Core.Community;

#region Community Data Records

/// <summary>What the community dataset knows about one commodity.</summary>
/// <param name="Sold">Facility keys where kiosks accept it, e.g. DC_Stan_Hurston_S1_Farnesway.</param>
/// <param name="Bought">Facility keys where kiosks stock it.</param>
public sealed record CommodityInfo(
    string Name,
    IReadOnlyList<string> Groups,
    IReadOnlyList<string> Sold,
    IReadOnlyList<string> Bought);

/// <summary>Reference data for one ship or vehicle.</summary>
public sealed record ShipInfo(
    string Name,
    string? Career,
    string? Role,
    int Crew,
    bool IsSpaceship,
    decimal? ExpeditedCost,
    double? ExpeditedClaimTime,
    double? StandardClaimTime,
    double CargoScu = 0,
    double ScmSpeed = 0,
    double MaxSpeed = 0,
    double ShieldHp = 0,
    double Health = 0);

/// <summary>Reference data for one item: what kind of thing it is.</summary>
public sealed record ItemInfo(
    string? Type,
    string? SubType,
    int Size,
    int Grade,
    string? Manufacturer,
    string? Uuid = null,
    string? Name = null);

/// <summary>One port on a ship that the player is allowed to change.</summary>
public sealed record ShipSlot(
    string Port,
    string Hardpoint,
    string Kind,
    int Size,
    string? Fitted,
    int FittedGrade,
    string? FittedUuid);

/// <summary>A body's real position within its system, star at the origin.</summary>
public sealed record BodyPosition(double X, double Y);

/// <summary>One resource spawning at one named location.</summary>
public sealed record ResourceSpawn(
    string Resource,
    string? Deposit,
    string Kind,
    string Location,
    string? System,
    string Group,
    double GroupChance,
    double Share);

/// <summary>One crafting blueprint: what it makes, from what, and how it is obtained.</summary>
public sealed record BlueprintInfo(
    string Output,
    string? OutputUuid,
    string? Type,
    int Grade,
    string Kind,
    int CraftSeconds,
    IReadOnlyList<string> Materials,
    bool Default,
    IReadOnlyList<string> RewardPools);

public sealed record WeaponStats(
    double Dps,
    double SustainedDps,
    double Alpha,
    double RateOfFire,
    double Range,
    double AmmoSpeed,
    IReadOnlyDictionary<string, double> DpsByType);

public sealed record ShieldStats(
    double Hp,
    double Regen,
    double DownedDelay,
    double DamagedDelay,
    IReadOnlyDictionary<string, double> AbsorptionMax);

public sealed record QuantumStats(
    double Speed,
    double SpoolTime,
    double Cooldown,
    double FuelRate,
    double DisconnectRange,
    double StageOneAccel,
    double StageTwoAccel);

public sealed record MissileStats(
    double Damage,
    double Speed,
    double LockTime,
    double Range,
    string? TrackingSignal);

public sealed record ArmorSignals(double Em, double Ir, double CrossSection);

public sealed record FitPort(
    string PortId,
    string Hardpoint,
    string? Class,
    string? Type,
    bool Editable,
    int MinSize,
    int MaxSize,
    IReadOnlyList<string> Accepts,
    IReadOnlyList<FitPort> Children);

public sealed record Vec3(double X, double Y, double Z);

public sealed record FlightStats(double Scm, double Boost, double Max, double Pitch, double Yaw, double Roll);

public sealed record DatasetTotals(
    double EmShields,
    double EmQuantum,
    double IrShields,
    double IrQuantum,
    int PowerSegments,
    double CoolingSegments,
    double ShieldHp,
    double FixedDps,
    double TurretDps,
    double QuantumRange,
    double MassTotal);

public sealed record ShipBase(
    string Class,
    string Name,
    string? Manufacturer,
    string? Role,
    string? Career,
    int Size,
    int Crew,
    bool IsSpaceship,
    double HullMass,
    double LoadoutMass,
    double Health,
    Vec3 CrossSection,
    FlightStats Flight,
    double QuantumFuel,
    double HydrogenFuel,
    double CargoScu,
    IReadOnlyDictionary<string, int> PowerPools,
    DatasetTotals Dataset,
    IReadOnlyList<FitPort> Loadout);

public sealed record PartStats(
    string Class,
    string Type,
    int Size,
    int Grade,
    string Name,
    string? Manufacturer,
    string? Uuid,
    double Mass,
    double Em,
    double Ir,
    double Health,
    double PowerGen,
    double PowerUseMin,
    double PowerUseMax,
    double CoolantGen,
    double CoolantUseMin,
    double CoolantUseMax,
    WeaponStats? Weapon = null,
    ShieldStats? Shield = null,
    QuantumStats? Quantum = null,
    MissileStats? Missile = null,
    ArmorSignals? Armor = null,
    string? SubType = null,
    bool Networked = true,
    string? MakerCode = null);

#endregion

/// <summary>
/// The community dataset (StarCitizenWiki / scunpacked-data) service.
/// Downloads, digests and caches commodities, trade locations, ships, items, slots, blueprints, and positions.
/// </summary>
public sealed partial class CommunityData
{
    public static CommunityData Instance { get; } = new();

    public const string CommoditiesUrl =
        "https://raw.githubusercontent.com/StarCitizenWiki/scunpacked-data/master/resources/commodities.json";

    public const string TradeLocationsUrl =
        "https://raw.githubusercontent.com/StarCitizenWiki/scunpacked-data/master/resources/commodity_trade_locations.json";

    public const string ShipsUrl =
        "https://raw.githubusercontent.com/StarCitizenWiki/scunpacked-data/master/ships.json";

    public const string FpsItemsUrl =
        "https://raw.githubusercontent.com/StarCitizenWiki/scunpacked-data/master/fps-items.json";

    public const string ShipItemsUrl =
        "https://raw.githubusercontent.com/StarCitizenWiki/scunpacked-data/master/ship-items.json";

    public const string ManufacturersUrl =
        "https://raw.githubusercontent.com/StarCitizenWiki/scunpacked-data/master/manufacturers.json";

    public const string ResourcesUrl =
        "https://raw.githubusercontent.com/StarCitizenWiki/scunpacked-data/master/resources/resources.json";

    public const string ResourceLocationsUrl =
        "https://raw.githubusercontent.com/StarCitizenWiki/scunpacked-data/master/resources/locations.json";

    public const string BlueprintsUrl =
        "https://raw.githubusercontent.com/StarCitizenWiki/scunpacked-data/master/blueprints.json";

    public const string StarmapInfoUrl =
        "https://raw.githubusercontent.com/StarCitizenWiki/scunpacked-data/master/starmap.json";

    public const string StarmapUrl =
        "https://raw.githubusercontent.com/StarCitizenWiki/scunpacked-data/master/starmap_positions.json";

    public const string HistoryUrl =
        "https://api.github.com/repos/StarCitizenWiki/scunpacked-data/commits?per_page=20";

    private readonly string _directory;

    private Dictionary<string, CommodityInfo> _byId = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, ShipInfo> _ships = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, List<ShipSlot>> _shipSlots = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, ItemInfo> _items = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, Dictionary<string, BodyPosition>> _positions = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string> _manufacturers = new(StringComparer.OrdinalIgnoreCase);
    private List<ResourceSpawn> _resourceSpawns = [];
    private List<BlueprintInfo> _blueprints = [];
    private Dictionary<string, string> _placeLore = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, PartStats> _parts = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, ShipBase> _shipBases = new(StringComparer.OrdinalIgnoreCase);

    public CommunityData(string? directory = null)
    {
        _directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SCLogMate",
            "community");

        TryLoad();
    }

    private string DigestPath => Path.Combine(_directory, "digest.json");
    private string MetaPath => Path.Combine(_directory, "meta.json");
    private string ShipsDigestPath => Path.Combine(_directory, "digest-ships.json");
    private string SlotsDigestPath => Path.Combine(_directory, "digest-ship-slots.json");
    private string ItemsDigestPath => Path.Combine(_directory, "digest-items.json");
    private string PositionsDigestPath => Path.Combine(_directory, "digest-positions.json");
    private string ManufacturersDigestPath => Path.Combine(_directory, "digest-manufacturers.json");
    private string ResourceSpawnsDigestPath => Path.Combine(_directory, "digest-resource-spawns.json");
    private string BlueprintsDigestPath => Path.Combine(_directory, "digest-blueprints.json");
    private string PlaceLoreDigestPath => Path.Combine(_directory, "digest-place-lore.json");
    private string PartStatsDigestPath => Path.Combine(_directory, "digest-part-stats.json");
    private string ShipStatsDigestPath => Path.Combine(_directory, "digest-ship-stats.json");

    public bool IsEnabled => _byId.Count > 0;
    public int CommoditiesCount => _byId.Count;
    public int ShipsCount => _ships.Count;
    public int ItemsCount => _items.Count;
    public int BlueprintsCount => _blueprints.Count;
    public int PartsCount => _parts.Count;
    public DateTimeOffset? FetchedAt { get; private set; }
    public string? Dump { get; private set; }
    public string? DumpBuild => BuildIn(Dump);

    public static string? BuildIn(string? stamp)
    {
        if (string.IsNullOrWhiteSpace(stamp))
            return null;

        var m = BuildNumberRegex().Match(stamp);
        return m.Success ? m.Groups["build"].Value : null;
    }

    [GeneratedRegex(@"(?<build>\d{6,})", RegexOptions.Compiled)]
    private static partial Regex BuildNumberRegex();

    [GeneratedRegex(@"^\d+\.\d+(\.\d+)?-[A-Za-z]+\.\d{6,}$", RegexOptions.Compiled)]
    private static partial Regex DumpStampRegex();

    private static readonly HashSet<string> Shoppable = new(StringComparer.OrdinalIgnoreCase)
    {
        "QuantumDrive", "Shield", "PowerPlant", "Cooler",
        "WeaponGun", "Turret", "MissileLauncher", "Missile",
        "Radar", "EMP", "QuantumInterdictionGenerator", "MiningArm",
    };

    public string? Commodity(string? resourceId) =>
        resourceId is not null && _byId.TryGetValue(resourceId, out var info) ? info.Name : null;

    public IReadOnlyDictionary<string, CommodityInfo> AllCommodities => _byId;
    public IReadOnlyDictionary<string, ShipInfo> Ships => _ships;
    public IReadOnlyDictionary<string, ItemInfo> Items => _items;
    public IReadOnlyDictionary<string, PartStats> Parts => _parts;
    public IReadOnlyDictionary<string, ShipBase> ShipBases => _shipBases;
    public IReadOnlyList<BlueprintInfo> Blueprints => _blueprints;
    public IReadOnlyDictionary<string, string> Manufacturers => _manufacturers;
    public IReadOnlyList<ResourceSpawn> ResourceSpawns => _resourceSpawns;

    public ShipInfo? Ship(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName) || _ships.Count == 0)
            return null;

        var key = displayName.Trim().Replace(' ', '_');
        if (_ships.TryGetValue(key, out var exact))
            return exact;

        return _ships
            .Where(p => p.Key.StartsWith(key + "_", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Key.Length)
            .Select(p => p.Value)
            .FirstOrDefault();
    }

    public PartStats? FindPart(string? query)
    {
        if (string.IsNullOrWhiteSpace(query) || _parts.Count == 0)
            return null;

        var clean = query.Trim();
        if (_parts.TryGetValue(clean, out var exact))
            return exact;

        var byName = _parts.Values.FirstOrDefault(p => string.Equals(p.Name, clean, StringComparison.OrdinalIgnoreCase));
        if (byName is not null)
            return byName;

        return _parts.Values.FirstOrDefault(p =>
            p.Class.Contains(clean, StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains(clean, StringComparison.OrdinalIgnoreCase));
    }

    public double? FindWeaponSpeed(string gunNameOrClass)
    {
        var part = FindPart(gunNameOrClass);
        if (part?.Weapon is not null && part.Weapon.AmmoSpeed > 0)
            return part.Weapon.AmmoSpeed;

        return null;
    }

    public async Task<int> EnableAsync(HttpClient? http = null, CancellationToken token = default)
    {
        using var client = http ?? new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(60);
        if (!client.DefaultRequestHeaders.Contains("User-Agent"))
            client.DefaultRequestHeaders.Add("User-Agent", "SCLogMate");

        var commoditiesJson = await client.GetStringAsync(CommoditiesUrl, token);
        var tradesJson = await client.GetStringAsync(TradeLocationsUrl, token);
        var shipsJson = await client.GetStringAsync(ShipsUrl, token);
        var fpsItemsJson = await client.GetStringAsync(FpsItemsUrl, token);
        var shipItemsJson = await client.GetStringAsync(ShipItemsUrl, token);
        var starmapJson = await client.GetStringAsync(StarmapUrl, token);
        var manufacturersJson = await client.GetStringAsync(ManufacturersUrl, token);
        var resourcesJson = await client.GetStringAsync(ResourcesUrl, token);
        var resourceLocationsJson = await client.GetStringAsync(ResourceLocationsUrl, token);
        var blueprintsJson = await client.GetStringAsync(BlueprintsUrl, token);
        var starmapInfoJson = await client.GetStringAsync(StarmapInfoUrl, token);

        var digest = Digest(commoditiesJson, tradesJson);
        if (digest.Count == 0)
            throw new InvalidDataException("The community dataset parsed to zero commodities.");

        var ships = DigestShips(shipsJson);
        var slots = DigestShipSlots(shipsJson);
        var items = DigestItems(fpsItemsJson, shipItemsJson);
        var positions = DigestPositions(starmapJson);
        var manufacturers = DigestManufacturers(manufacturersJson);
        var spawns = DigestResourceSpawns(resourcesJson, resourceLocationsJson);
        var blueprints = DigestBlueprints(blueprintsJson);
        var lore = DigestPlaceLore(starmapInfoJson);
        var partStats = DigestPartStats(shipItemsJson);
        var shipStats = DigestShipStats(shipsJson, partStats);

        Directory.CreateDirectory(_directory);
        File.WriteAllText(DigestPath, JsonSerializer.Serialize(digest));
        File.WriteAllText(ShipsDigestPath, JsonSerializer.Serialize(ships));
        File.WriteAllText(SlotsDigestPath, JsonSerializer.Serialize(slots));
        File.WriteAllText(ItemsDigestPath, JsonSerializer.Serialize(items));
        File.WriteAllText(PositionsDigestPath, JsonSerializer.Serialize(positions));
        File.WriteAllText(ManufacturersDigestPath, JsonSerializer.Serialize(manufacturers));
        File.WriteAllText(ResourceSpawnsDigestPath, JsonSerializer.Serialize(spawns));
        File.WriteAllText(BlueprintsDigestPath, JsonSerializer.Serialize(blueprints));
        File.WriteAllText(PlaceLoreDigestPath, JsonSerializer.Serialize(lore));
        File.WriteAllText(PartStatsDigestPath, JsonSerializer.Serialize(partStats));
        File.WriteAllText(ShipStatsDigestPath, JsonSerializer.Serialize(shipStats));

        var dump = await ReadDumpAsync(client, token);
        File.WriteAllText(MetaPath, JsonSerializer.Serialize(new Meta(DateTimeOffset.UtcNow, dump)));

        _byId = digest;
        _ships = ships;
        _shipSlots = slots;
        _items = items;
        _positions = positions;
        _manufacturers = manufacturers;
        _resourceSpawns = spawns;
        _blueprints = blueprints;
        _placeLore = lore;
        _parts = partStats;
        _shipBases = shipStats;
        FetchedAt = DateTimeOffset.UtcNow;
        Dump = dump;

        return _byId.Count;
    }

    private static async Task<string?> ReadDumpAsync(HttpClient http, CancellationToken token)
    {
        try
        {
            using var document = JsonDocument.Parse(await http.GetStringAsync(HistoryUrl, token));
            foreach (var entry in document.RootElement.EnumerateArray())
            {
                if (!entry.TryGetProperty("commit", out var commit)
                    || !commit.TryGetProperty("message", out var message))
                    continue;

                var subject = (message.GetString() ?? string.Empty).Split('\n')[0].Trim();
                if (DumpStampRegex().IsMatch(subject))
                    return subject;
            }
        }
        catch
        {
        }

        return null;
    }

    public void Disable()
    {
        if (Directory.Exists(_directory))
        {
            try { Directory.Delete(_directory, recursive: true); } catch { }
        }

        _byId = new Dictionary<string, CommodityInfo>(StringComparer.OrdinalIgnoreCase);
        _ships = new Dictionary<string, ShipInfo>(StringComparer.OrdinalIgnoreCase);
        _shipSlots = new Dictionary<string, List<ShipSlot>>(StringComparer.OrdinalIgnoreCase);
        _items = new Dictionary<string, ItemInfo>(StringComparer.OrdinalIgnoreCase);
        _positions = new Dictionary<string, Dictionary<string, BodyPosition>>(StringComparer.OrdinalIgnoreCase);
        _manufacturers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _resourceSpawns = [];
        _blueprints = [];
        _placeLore = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _parts = new Dictionary<string, PartStats>(StringComparer.OrdinalIgnoreCase);
        _shipBases = new Dictionary<string, ShipBase>(StringComparer.OrdinalIgnoreCase);
        FetchedAt = null;
        Dump = null;
    }

    public void TryLoad()
    {
        try
        {
            if (!File.Exists(DigestPath))
                return;

            _byId = Load<CommodityInfo>(DigestPath);
            _ships = Load<ShipInfo>(ShipsDigestPath);

            if (File.Exists(SlotsDigestPath))
                _shipSlots = JsonSerializer.Deserialize<Dictionary<string, List<ShipSlot>>>(File.ReadAllText(SlotsDigestPath))
                    is { } s ? new Dictionary<string, List<ShipSlot>>(s, StringComparer.OrdinalIgnoreCase)
                             : new Dictionary<string, List<ShipSlot>>(StringComparer.OrdinalIgnoreCase);

            _items = Load<ItemInfo>(ItemsDigestPath);

            if (File.Exists(PositionsDigestPath))
                _positions = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, BodyPosition>>>(File.ReadAllText(PositionsDigestPath))
                    ?? new Dictionary<string, Dictionary<string, BodyPosition>>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(ManufacturersDigestPath))
                _manufacturers = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(ManufacturersDigestPath))
                    is { } m ? new Dictionary<string, string>(m, StringComparer.OrdinalIgnoreCase)
                             : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(ResourceSpawnsDigestPath))
                _resourceSpawns = JsonSerializer.Deserialize<List<ResourceSpawn>>(File.ReadAllText(ResourceSpawnsDigestPath)) ?? [];

            if (File.Exists(BlueprintsDigestPath))
                _blueprints = JsonSerializer.Deserialize<List<BlueprintInfo>>(File.ReadAllText(BlueprintsDigestPath)) ?? [];

            if (File.Exists(PlaceLoreDigestPath))
                _placeLore = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(PlaceLoreDigestPath))
                    is { } lore ? new Dictionary<string, string>(lore, StringComparer.OrdinalIgnoreCase)
                                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(PartStatsDigestPath))
                _parts = JsonSerializer.Deserialize<Dictionary<string, PartStats>>(File.ReadAllText(PartStatsDigestPath))
                    is { } p ? new Dictionary<string, PartStats>(p, StringComparer.OrdinalIgnoreCase)
                             : new Dictionary<string, PartStats>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(ShipStatsDigestPath))
                _shipBases = JsonSerializer.Deserialize<Dictionary<string, ShipBase>>(File.ReadAllText(ShipStatsDigestPath))
                    is { } sb ? new Dictionary<string, ShipBase>(sb, StringComparer.OrdinalIgnoreCase)
                              : new Dictionary<string, ShipBase>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(MetaPath))
            {
                var meta = JsonSerializer.Deserialize<Meta>(File.ReadAllText(MetaPath));
                FetchedAt = meta?.FetchedAt;
                Dump = meta?.Dump;
            }
        }
        catch
        {
            _byId = new Dictionary<string, CommodityInfo>(StringComparer.OrdinalIgnoreCase);
            _ships = new Dictionary<string, ShipInfo>(StringComparer.OrdinalIgnoreCase);
            _shipSlots = new Dictionary<string, List<ShipSlot>>(StringComparer.OrdinalIgnoreCase);
            _items = new Dictionary<string, ItemInfo>(StringComparer.OrdinalIgnoreCase);
            _parts = new Dictionary<string, PartStats>(StringComparer.OrdinalIgnoreCase);
            _shipBases = new Dictionary<string, ShipBase>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static Dictionary<string, T> Load<T>(string path)
    {
        if (!File.Exists(path))
            return new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);

        var parsed = JsonSerializer.Deserialize<Dictionary<string, T>>(File.ReadAllText(path));
        return parsed is null
            ? new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, T>(parsed, StringComparer.OrdinalIgnoreCase);
    }

    public static Dictionary<string, CommodityInfo> Digest(string commoditiesJson, string tradeLocationsJson)
    {
        var result = new Dictionary<string, CommodityInfo>(StringComparer.OrdinalIgnoreCase);
        var sold = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        var bought = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        using (var trades = JsonDocument.Parse(tradeLocationsJson))
        {
            if (trades.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in trades.RootElement.EnumerateArray())
                {
                    if (!entry.TryGetProperty("CommodityUUID", out var uuid) || uuid.GetString() is not { Length: 36 } id)
                        continue;

                    sold[id] = Facilities(entry, "SoldAt");
                    bought[id] = Facilities(entry, "BoughtAt");
                }
            }
        }

        using var commodities = JsonDocument.Parse(commoditiesJson);
        if (commodities.RootElement.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var entry in commodities.RootElement.EnumerateArray())
        {
            if (!entry.TryGetProperty("UUID", out var uuid) || uuid.GetString() is not { Length: 36 } id)
                continue;

            var name = entry.TryGetProperty("Name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : null;
            name ??= entry.TryGetProperty("Key", out var k) && k.ValueKind == JsonValueKind.String ? k.GetString() : null;

            if (string.IsNullOrWhiteSpace(name) || name.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
                continue;

            var groups = entry.TryGetProperty("CommodityGroups", out var g) && g.ValueKind == JsonValueKind.Array
                ? g.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToList()
                : [];

            result[id] = new CommodityInfo(name!, groups, sold.GetValueOrDefault(id, []), bought.GetValueOrDefault(id, []));
        }

        return result;
    }

    private static List<string> Facilities(JsonElement entry, string side)
    {
        if (!entry.TryGetProperty(side, out var rows) || rows.ValueKind != JsonValueKind.Array)
            return [];

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows.EnumerateArray())
        {
            if (!row.TryGetProperty("TradeLocationClassName", out var c) || c.GetString() is not { Length: > 0 } className)
                continue;

            var parts = className.Split('_');
            keys.Add(string.Join('_', parts.Take(Math.Min(5, parts.Length))));
        }

        return [.. keys.Order(StringComparer.OrdinalIgnoreCase)];
    }

    public static Dictionary<string, ShipInfo> DigestShips(string shipsJson)
    {
        var result = new Dictionary<string, ShipInfo>(StringComparer.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(shipsJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var entry in doc.RootElement.EnumerateArray())
        {
            var className = Str(entry, "ClassName");
            var name = Str(entry, "Name");
            if (className is null || name is null || name.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
                continue;

            decimal? expeditedCost = null;
            double? expedited = null;
            double? standard = null;

            if (entry.TryGetProperty("Insurance", out var insurance) && insurance.ValueKind == JsonValueKind.Object)
            {
                expeditedCost = Num(insurance, "ExpeditedCost") is { } cost ? (decimal)cost : null;
                expedited = Num(insurance, "ExpeditedClaimTime");
                standard = Num(insurance, "StandardClaimTime");
            }

            double scm = 0;
            double max = 0;
            if (entry.TryGetProperty("FlightCharacteristics", out var flight)
                && flight.ValueKind == JsonValueKind.Object
                && flight.TryGetProperty("Speeds", out var speeds)
                && speeds.ValueKind == JsonValueKind.Object)
            {
                scm = Num(speeds, "Scm") ?? 0;
                max = Num(speeds, "Max") ?? 0;
            }

            result[className] = new ShipInfo(
                name,
                Str(entry, "Career"),
                Str(entry, "Role"),
                (int)(Num(entry, "Crew") ?? 0),
                entry.TryGetProperty("IsSpaceship", out var s) && s.ValueKind == JsonValueKind.True,
                expeditedCost,
                expedited,
                standard,
                Num(entry, "Cargo") ?? 0,
                scm,
                max,
                Num(entry, "ShieldHp") ?? 0,
                Num(entry, "Health") ?? 0);
        }

        return result;
    }

    public static Dictionary<string, List<ShipSlot>> DigestShipSlots(string shipsJson)
    {
        var result = new Dictionary<string, List<ShipSlot>>(StringComparer.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(shipsJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var entry in doc.RootElement.EnumerateArray())
        {
            var className = Str(entry, "ClassName");
            if (className is null || !entry.TryGetProperty("Loadout", out var loadout))
                continue;

            var slots = new List<ShipSlot>();
            Walk(loadout, slots);
            if (slots.Count > 0)
                result[className] = slots;
        }

        return result;

        static void Walk(JsonElement ports, List<ShipSlot> into)
        {
            if (ports.ValueKind != JsonValueKind.Array)
                return;

            foreach (var port in ports.EnumerateArray())
            {
                Keep(port, into);
                if (port.TryGetProperty("Loadout", out var children))
                    Walk(children, into);
            }
        }

        static void Keep(JsonElement port, List<ShipSlot> into)
        {
            if (!port.TryGetProperty("Editable", out var editable) || editable.ValueKind != JsonValueKind.True)
                return;

            if (!port.TryGetProperty("CompatibleTypes", out var types) || types.ValueKind != JsonValueKind.Array)
                return;

            var hardpoint = Str(port, "HardpointName") ?? "?";
            var min = (int)(Num(port, "MinSize") ?? 0);
            var max = (int)(Num(port, "MaxSize") ?? min);

            var fitted = Str(port, "Name");
            if (fitted is not null && fitted.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
                fitted = null;

            foreach (var accepted in types.EnumerateArray())
            {
                var kind = Str(accepted, "Type");
                if (kind is null || !Shoppable.Contains(kind))
                    continue;

                for (var size = min; size <= max && size <= 12; size++)
                    into.Add(new ShipSlot(
                        Str(port, "PortId") ?? hardpoint,
                        hardpoint,
                        kind,
                        size,
                        fitted,
                        (int)(Num(port, "Grade") ?? 0),
                        Str(port, "UUID")));
            }
        }
    }

    public static List<ResourceSpawn> DigestResourceSpawns(string resourcesJson, string locationsJson)
    {
        var byUuid = new Dictionary<string, (string Name, string Kind)>(StringComparer.OrdinalIgnoreCase);
        using (var resourceDoc = JsonDocument.Parse(resourcesJson))
        {
            if (resourceDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in resourceDoc.RootElement.EnumerateArray())
                {
                    var uuid = Str(entry, "UUID");
                    var kind = Str(entry, "Kind");
                    var name = Str(entry, "Name");
                    if (name is null || name.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
                        name = Str(entry, "Key");

                    if (uuid is null || kind is null || name is null)
                        continue;

                    if (name.Contains("Test", StringComparison.OrdinalIgnoreCase)
                        || name.Contains("template", StringComparison.OrdinalIgnoreCase)
                        || name.Contains("Lootbox", StringComparison.OrdinalIgnoreCase)
                        || name.Contains("Placeholder", StringComparison.OrdinalIgnoreCase)
                        || name.Contains("Blocker", StringComparison.OrdinalIgnoreCase)
                        || name.Contains("Obstacle", StringComparison.OrdinalIgnoreCase))
                        continue;

                    byUuid[uuid] = (name, kind);
                }
            }
        }

        var spawns = new List<ResourceSpawn>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var locationDoc = JsonDocument.Parse(locationsJson);
        if (locationDoc.RootElement.ValueKind != JsonValueKind.Array)
            return spawns;

        foreach (var provider in locationDoc.RootElement.EnumerateArray())
        {
            if (!provider.TryGetProperty("Locations", out var locations)
                || locations.ValueKind != JsonValueKind.Array
                || !provider.TryGetProperty("Groups", out var groups)
                || groups.ValueKind != JsonValueKind.Array)
                continue;

            var places = locations.EnumerateArray()
                .Select(l => (Name: Str(l, "Name"), System: Str(l, "System")))
                .Where(l => l.Name is { Length: > 0 })
                .Distinct()
                .ToList();

            if (places.Count == 0)
                continue;

            foreach (var group in groups.EnumerateArray())
            {
                var groupName = Str(group, "GroupName") ?? "?";
                var groupChance = Num(group, "GroupProbability") ?? 0;

                if (!group.TryGetProperty("Deposits", out var deposits) || deposits.ValueKind != JsonValueKind.Array)
                    continue;

                var rows = deposits.EnumerateArray()
                    .Select(d => (Uuid: Str(d, "ResourceUUID"), Weight: Num(d, "RelativeProbability") ?? 0))
                    .Where(d => d.Uuid is not null && byUuid.ContainsKey(d.Uuid!))
                    .ToList();

                var totalWeight = rows.Sum(d => d.Weight);
                if (totalWeight <= 0)
                    continue;

                foreach (var (uuid, weight) in rows)
                {
                    var (rawName, kind) = byUuid[uuid!];
                    var (resource, deposit) = SplitResource(rawName);

                    foreach (var (placeName, system) in places)
                    {
                        if (!seen.Add($"{resource}|{deposit}|{placeName}|{groupName}"))
                            continue;

                        spawns.Add(new ResourceSpawn(
                            resource, deposit, kind, placeName!, system,
                            groupName.Replace('_', ' '),
                            Math.Round(groupChance, 4),
                            Math.Round(weight / totalWeight, 4)));
                    }
                }
            }
        }

        return spawns;
    }

    private static (string Resource, string? Deposit) SplitResource(string name)
    {
        var parts = name.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && parts[^1].Length >= 3 && parts[^1].All(char.IsLetter))
            return (PrettyWords(parts[^1]), PrettyWords(string.Join(' ', parts[..^1])));

        return (PrettyWords(name), null);
    }

    private static string PrettyWords(string value) =>
        Regex.Replace(value.Replace('_', ' '), "(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");

    public static List<BlueprintInfo> DigestBlueprints(string blueprintsJson)
    {
        var result = new List<BlueprintInfo>();
        using var doc = JsonDocument.Parse(blueprintsJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var entry in doc.RootElement.EnumerateArray())
        {
            if (!entry.TryGetProperty("Output", out var output) || output.ValueKind != JsonValueKind.Object)
                continue;

            var name = Str(output, "Name");
            if (name is null || name.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
                continue;

            var isDefault = false;
            var pools = new List<string>();

            if (entry.TryGetProperty("Availability", out var availability) && availability.ValueKind == JsonValueKind.Object)
            {
                isDefault = availability.TryGetProperty("Default", out var d) && d.ValueKind == JsonValueKind.True;
                if (availability.TryGetProperty("RewardPools", out var rewardPools) && rewardPools.ValueKind == JsonValueKind.Array)
                {
                    foreach (var pool in rewardPools.EnumerateArray())
                    {
                        var key = Str(pool, "Key");
                        if (key is not null)
                            pools.Add(PrettyWords(key.Replace("BP_REWARDS_", "")));
                    }
                }
            }

            var craftSeconds = 0;
            var materials = new List<string>();
            if (entry.TryGetProperty("Tiers", out var tiers) && tiers.ValueKind == JsonValueKind.Array && tiers.GetArrayLength() > 0)
            {
                var tier = tiers[0];
                craftSeconds = (int)(Num(tier, "CraftTimeSeconds") ?? 0);
                if (tier.TryGetProperty("Requirements", out var requirements))
                    CollectMaterials(requirements, materials);
            }

            result.Add(new BlueprintInfo(
                name,
                Str(output, "UUID"),
                Str(output, "Type"),
                int.TryParse(Str(output, "Grade"), out var grade) ? grade : 0,
                Str(entry, "Kind") ?? "creation",
                craftSeconds,
                materials.Distinct().ToList(),
                isDefault,
                pools.Distinct().ToList()));
        }

        return result;
    }

    private static void CollectMaterials(JsonElement node, List<string> materials)
    {
        if (node.ValueKind != JsonValueKind.Object)
            return;

        var kind = Str(node, "Kind");
        var name = Str(node, "Name");

        if (name is not null && !name.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
        {
            if (kind == "resource")
            {
                var scu = Num(node, "QuantityScu") ?? 0;
                materials.Add(scu > 0 ? $"{name} {scu:0.##} SCU" : name);
            }
            else if (kind == "item")
            {
                var quantity = Num(node, "Quantity") ?? 0;
                materials.Add(quantity > 1 ? $"{name} ×{quantity:0}" : name);
            }
        }

        if (node.TryGetProperty("Children", out var children) && children.ValueKind == JsonValueKind.Array)
            foreach (var child in children.EnumerateArray())
                CollectMaterials(child, materials);
    }

    public static Dictionary<string, string> DigestPlaceLore(string starmapJson)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(starmapJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var entry in doc.RootElement.EnumerateArray())
        {
            var name = Str(entry, "Name");
            var description = Str(entry, "Description");
            if (name is null || description is null
                || name.Contains("UNINITIALIZED") || name.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase)
                || description.Contains("UNINITIALIZED")
                || description.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase)
                || description.Trim().Length < 30)
                continue;

            result.TryAdd(name.Trim(), description.Trim());
        }

        return result;
    }

    public static Dictionary<string, string> DigestManufacturers(string manufacturersJson)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(manufacturersJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var entry in doc.RootElement.EnumerateArray())
        {
            var code = Str(entry, "Code");
            var name = Str(entry, "Name");
            if (code is { Length: > 0 } && name is { Length: > 0 } && !name.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
                result[code] = name;
        }

        return result;
    }

    public static Dictionary<string, ItemInfo> DigestItems(params string[] jsonFiles)
    {
        var result = new Dictionary<string, ItemInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var json in jsonFiles)
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var entry in doc.RootElement.EnumerateArray())
            {
                var className = Str(entry, "className");
                if (className is null)
                    continue;

                string? manufacturer = null;
                if (entry.TryGetProperty("stdItem", out var std) && std.ValueKind == JsonValueKind.Object
                    && std.TryGetProperty("Manufacturer", out var maker) && maker.ValueKind == JsonValueKind.Object)
                {
                    manufacturer = Str(maker, "Name");
                    if (manufacturer is "Unknown Manufacturer")
                        manufacturer = null;
                }

                var name = Str(entry, "name");
                if (name is null || name.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
                    name = null;

                result[className] = new ItemInfo(
                    Str(entry, "type"),
                    Str(entry, "subType") is "UNDEFINED" or null ? null : Str(entry, "subType"),
                    (int)(Num(entry, "size") ?? 0),
                    (int)(Num(entry, "grade") ?? 0),
                    manufacturer,
                    Str(entry, "reference"),
                    name);
            }
        }

        return result;
    }

    public static Dictionary<string, Dictionary<string, BodyPosition>> DigestPositions(string starmapJson)
    {
        var result = new Dictionary<string, Dictionary<string, BodyPosition>>(StringComparer.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(starmapJson);
        if (!doc.RootElement.TryGetProperty("entities", out var entities) || entities.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var entry in entities.EnumerateArray())
        {
            var type = Str(entry, "type");
            if (type is not ("Planet" or "Moon"))
                continue;

            var system = Str(entry, "system");
            var name = Str(entry, "name");
            var x = Num(entry, "x");
            var y = Num(entry, "y");
            if (system is null || name is null || x is null || y is null)
                continue;

            if (!result.TryGetValue(system, out var bodies))
                result[system] = bodies = new Dictionary<string, BodyPosition>(StringComparer.OrdinalIgnoreCase);

            bodies.TryAdd(name, new BodyPosition(x.Value, y.Value));
        }

        return result;
    }

    public static Dictionary<string, PartStats> DigestPartStats(string shipItemsJson)
    {
        var result = new Dictionary<string, PartStats>(StringComparer.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(shipItemsJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var entry in doc.RootElement.EnumerateArray())
        {
            var className = Str(entry, "className");
            var type = Str(entry, "type");
            if (className is null || type is null || type.StartsWith("Flair", StringComparison.Ordinal) || type == "Paints")
                continue;

            if (!entry.TryGetProperty("stdItem", out var std) || std.ValueKind != JsonValueKind.Object)
                continue;

            var name = Str(std, "Name") ?? Str(entry, "name") ?? className;
            if (name.Contains("PLACEHOLDER", StringComparison.OrdinalIgnoreCase))
                name = className;

            var networked = std.TryGetProperty("ResourceNetwork", out var rn) && rn.ValueKind == JsonValueKind.Object;

            result[className] = new PartStats(
                className,
                type,
                (int)(Num(entry, "size") ?? Num(std, "Size") ?? 0),
                (int)(Num(entry, "grade") ?? Num(std, "Grade") ?? 0),
                name,
                At(std, "Manufacturer", "Name") is { ValueKind: JsonValueKind.String } m ? m.GetString() : null,
                Str(std, "UUID"),
                Num(std, "Mass") ?? 0,
                Number(std, "Emission", "Em", "Maximum"),
                Number(std, "Emission", "Ir") is var ir && ir > 0 ? ir : Number(std, "Emission", "IR"),
                Number(std, "Durability", "Health"),
                Number(std, "ResourceNetwork", "Generation", "Power"),
                Number(std, "ResourceNetwork", "Usage", "Power", "Minimum"),
                Number(std, "ResourceNetwork", "Usage", "Power", "Maximum"),
                Number(std, "ResourceNetwork", "Generation", "Coolant"),
                Number(std, "ResourceNetwork", "Usage", "Coolant", "Minimum"),
                Number(std, "ResourceNetwork", "Usage", "Coolant", "Maximum"),
                Weapon(std),
                Shield(std),
                Quantum(std),
                Missile(std),
                Armor(std),
                Str(entry, "subType"),
                networked,
                At(std, "Manufacturer", "Code") is { ValueKind: JsonValueKind.String } mc && mc.GetString() is { Length: > 0 } code && code != "UNKN" ? code : null);
        }

        return result;

        static WeaponStats? Weapon(JsonElement std)
        {
            if (At(std, "Weapon", "Damage") is not { ValueKind: JsonValueKind.Object } damage)
                return null;

            var byType = new Dictionary<string, double>(StringComparer.Ordinal);
            if (damage.TryGetProperty("Dps", out var dps) && dps.ValueKind == JsonValueKind.Object)
                foreach (var kind in dps.EnumerateObject())
                    if (kind.Value.ValueKind == JsonValueKind.Number && kind.Value.GetDouble() > 0)
                        byType[kind.Name] = kind.Value.GetDouble();

            return new WeaponStats(
                Num(damage, "DpsTotal") ?? 0,
                Num(damage, "Sustained") ?? 0,
                Num(damage, "AlphaTotal") ?? 0,
                Number(std, "Weapon", "RateOfFire"),
                Number(std, "Weapon", "EffectiveRange"),
                Number(std, "Ammunition", "Speed"),
                byType);
        }

        static ShieldStats? Shield(JsonElement std)
        {
            if (At(std, "Shield") is not { ValueKind: JsonValueKind.Object } shield)
                return null;

            var absorb = new Dictionary<string, double>(StringComparer.Ordinal);
            if (shield.TryGetProperty("Absorption", out var abs) && abs.ValueKind == JsonValueKind.Object)
                foreach (var kind in abs.EnumerateObject())
                    absorb[kind.Name] = Num(kind.Value, "Maximum") ?? 0;

            return new ShieldStats(
                Num(shield, "MaxShieldHealth") ?? 0,
                Num(shield, "MaxShieldRegen") ?? 0,
                Num(shield, "DownedDelay") ?? 0,
                Num(shield, "DamagedDelay") ?? 0,
                absorb);
        }

        static QuantumStats? Quantum(JsonElement std)
        {
            if (At(std, "QuantumDrive") is not { ValueKind: JsonValueKind.Object } qd)
                return null;

            return new QuantumStats(
                Number(qd, "StandardJump", "DriveSpeed"),
                Number(qd, "StandardJump", "SpoolUpTime"),
                Number(qd, "StandardJump", "CooldownTime"),
                Num(qd, "FuelRate") ?? 0,
                Num(qd, "DisconnectRange") ?? 0,
                Number(qd, "StandardJump", "StageOneAccelRate"),
                Number(qd, "StandardJump", "StageTwoAccelRate"));
        }

        static MissileStats? Missile(JsonElement std)
        {
            if (At(std, "Missile") is not { ValueKind: JsonValueKind.Object } ms)
                return null;

            var damage = 0.0;
            if (ms.TryGetProperty("Damage", out var dmg) && dmg.ValueKind == JsonValueKind.Object)
                foreach (var kind in dmg.EnumerateObject())
                    if (kind.Value.ValueKind == JsonValueKind.Number) damage += kind.Value.GetDouble();

            return new MissileStats(
                damage,
                Number(ms, "GCS", "LinearSpeed"),
                Number(ms, "Targeting", "LockTime"),
                Num(ms, "Distance") ?? 0,
                At(ms, "Targeting", "TrackingSignalType") is { ValueKind: JsonValueKind.String } sig ? sig.GetString() : null);
        }

        static ArmorSignals? Armor(JsonElement std)
        {
            if (At(std, "Armor", "SignalMultipliers") is not { ValueKind: JsonValueKind.Object } sm)
                return null;

            return new ArmorSignals(
                Num(sm, "Electromagnetic") ?? 1,
                Num(sm, "Infrared") ?? 1,
                Num(sm, "CrossSection") ?? 1);
        }
    }

    public static Dictionary<string, ShipBase> DigestShipStats(string shipsJson, IReadOnlyDictionary<string, PartStats> parts)
    {
        var result = new Dictionary<string, ShipBase>(StringComparer.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(shipsJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var s in doc.RootElement.EnumerateArray())
        {
            var className = Str(s, "ClassName");
            if (className is null || !s.TryGetProperty("Loadout", out var loadout))
                continue;

            var pools = new Dictionary<string, int>(StringComparer.Ordinal);
            if (s.TryGetProperty("PowerPools", out var pp) && pp.ValueKind == JsonValueKind.Object)
                foreach (var pool in pp.EnumerateObject())
                    if (Num(pool.Value, "Size") is { } size && size >= 0)
                        pools[pool.Name] = (int)size;

            var ifcs = At(s, "FlightCharacteristics", "IFCS");
            var agility = At(s, "Agility");

            result[className] = new ShipBase(
                className,
                Str(s, "Name") ?? className,
                At(s, "Manufacturer", "Name") is { ValueKind: JsonValueKind.String } m ? m.GetString() : null,
                Str(s, "Role"),
                Str(s, "Career"),
                (int)(Num(s, "Size") ?? 0),
                (int)(Num(s, "Crew") ?? 0),
                s.TryGetProperty("IsSpaceship", out var sp) && sp.ValueKind == JsonValueKind.True,
                Num(s, "Mass") ?? 0,
                Num(s, "MassLoadout") ?? 0,
                Num(s, "Health") ?? 0,
                new Vec3(Number(s, "CrossSection", "X"), Number(s, "CrossSection", "Y"), Number(s, "CrossSection", "Z")),
                new FlightStats(
                    Number(ifcs, "ScmSpeed"), Number(ifcs, "BoostSpeedForward"), Number(ifcs, "MaxSpeed"),
                    Number(agility, "Pitch"), Number(agility, "Yaw"), Number(agility, "Roll")),
                Number(s, "QuantumTravel", "FuelCapacity"),
                Number(s, "Propulsion", "FuelCapacity"),
                Num(s, "Cargo") ?? 0,
                pools,
                new DatasetTotals(
                    Number(s, "Emission", "EmShields"),
                    Number(s, "Emission", "EmQuantum"),
                    Number(s, "Emission", "IrShields"),
                    Number(s, "Emission", "IrQuantum"),
                    (int)Number(s, "Power", "GenerationSegments"),
                    Number(s, "Cooling", "GenerationSegments"),
                    Num(s, "ShieldHp") ?? 0,
                    Number(s, "Weaponry", "FixedWeapons", "DpsTotal"),
                    Number(s, "Weaponry", "TurretDps"),
                    Number(s, "QuantumTravel", "Range"),
                    Num(s, "MassTotal") ?? 0),
                Ports(loadout, parts));
        }

        return result;

        static IReadOnlyList<FitPort> Ports(JsonElement ports, IReadOnlyDictionary<string, PartStats> parts)
        {
            if (ports.ValueKind != JsonValueKind.Array)
                return [];

            var list = new List<FitPort>();
            foreach (var port in ports.EnumerateArray())
            {
                var cls = Str(port, "ClassName");
                var editable = port.TryGetProperty("Editable", out var ed) && ed.ValueKind == JsonValueKind.True;
                var children = port.TryGetProperty("Loadout", out var sub) ? Ports(sub, parts) : [];
                var described = cls is not null && parts.ContainsKey(cls);
                var type = Str(port, "Type");

                var accepts = new List<string>();
                if (port.TryGetProperty("CompatibleTypes", out var types) && types.ValueKind == JsonValueKind.Array)
                    foreach (var t in types.EnumerateArray())
                        if (Str(t, "Type") is { } kind) accepts.Add(kind);

                var turretish = (type is not null && type.StartsWith("Turret", StringComparison.Ordinal))
                    || accepts.Any(a => a.StartsWith("Turret", StringComparison.Ordinal));
                var bench = editable && accepts.Any(Shoppable.Contains);

                if (!bench && !described && children.Count == 0 && !turretish)
                    continue;

                if (!bench && !turretish)
                    accepts.Clear();

                var hardpoint = Str(port, "HardpointName") ?? "?";
                list.Add(new FitPort(
                    Str(port, "PortId") ?? hardpoint,
                    hardpoint,
                    cls,
                    type is null ? null : type.Split('.')[0],
                    bench,
                    (int)(Num(port, "MinSize") ?? 0),
                    (int)(Num(port, "MaxSize") ?? Num(port, "MinSize") ?? 0),
                    accepts,
                    children));
            }

            return list;
        }
    }

    private static string? Str(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static double? Num(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDouble()
            : null;

    private static JsonElement At(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var step in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(step, out var next))
                return default;
            current = next;
        }

        return current;
    }

    private static double Number(JsonElement element, params string[] path)
    {
        var found = At(element, path);
        return found.ValueKind == JsonValueKind.Number ? found.GetDouble() : 0;
    }

    private sealed record Meta(DateTimeOffset FetchedAt, string? Dump = null);
}
