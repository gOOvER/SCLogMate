using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Photino.NET;
using SCLogMate.Models;

namespace SCLogMate.Core.Photino;

public class IpcMessage
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("payload")]
    public JsonElement? Payload { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class AppStatusDto
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0-rc2";

    [JsonPropertyName("isLiveWatching")]
    public bool IsLiveWatching { get; set; }

    [JsonPropertyName("logPath")]
    public string? LogPath { get; set; }

    [JsonPropertyName("activeSessionName")]
    public string? ActiveSessionName { get; set; }

    [JsonPropertyName("dbSessionCount")]
    public int DbSessionCount { get; set; }

    [JsonPropertyName("totalIncome")]
    public long TotalIncome { get; set; }

    [JsonPropertyName("totalSpend")]
    public long TotalSpend { get; set; }

    [JsonPropertyName("totalNet")]
    public long TotalNet { get; set; }

    [JsonPropertyName("lastEventTime")]
    public string? LastEventTime { get; set; }
}

public class SessionSummaryDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("startTime")]
    public string StartTime { get; set; } = "";

    [JsonPropertyName("endTime")]
    public string EndTime { get; set; } = "";

    [JsonPropertyName("duration")]
    public string Duration { get; set; } = "";

    [JsonPropertyName("income")]
    public long Income { get; set; }

    [JsonPropertyName("spend")]
    public long Spend { get; set; }

    [JsonPropertyName("net")]
    public long Net { get; set; }

    [JsonPropertyName("sales")]
    public long Sales { get; set; }

    [JsonPropertyName("trade")]
    public long Trade { get; set; }

    [JsonPropertyName("deaths")]
    public int Deaths { get; set; }

    [JsonPropertyName("missions")]
    public int Missions { get; set; }

    [JsonPropertyName("ships")]
    public List<string> Ships { get; set; } = new();

    [JsonPropertyName("lastLocation")]
    public string LastLocation { get; set; } = "";
}

public class LogEventDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.Now.ToString("HH:mm:ss");

    [JsonPropertyName("category")]
    public string Category { get; set; } = "system";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("amount")]
    public long? Amount { get; set; }

    [JsonPropertyName("ship")]
    public string? Ship { get; set; }

    [JsonPropertyName("rawText")]
    public string? RawText { get; set; }
}

public class WarehouseItemDto
{
    [JsonPropertyName("location")]
    public string Location { get; set; } = "";

    [JsonPropertyName("locationCode")]
    public string LocationCode { get; set; } = "";

    [JsonPropertyName("system")]
    public string System { get; set; } = "Stanton";

    [JsonPropertyName("parentBody")]
    public string ParentBody { get; set; } = "";

    [JsonPropertyName("itemClass")]
    public string ItemClass { get; set; } = "";

    [JsonPropertyName("itemName")]
    public string ItemName { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "Sonstiges";

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("lastUpdated")]
    public string LastUpdated { get; set; } = "";

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "📦";

    [JsonPropertyName("locationDisplay")]
    public string LocationDisplay { get; set; } = "";
}

public class WarehouseLocationDto
{
    [JsonPropertyName("locationName")]
    public string LocationName { get; set; } = "";

    [JsonPropertyName("locationCode")]
    public string LocationCode { get; set; } = "";

    [JsonPropertyName("system")]
    public string System { get; set; } = "Stanton";

    [JsonPropertyName("parentBody")]
    public string ParentBody { get; set; } = "";

    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }

    [JsonPropertyName("uniqueItemTypes")]
    public int UniqueItemTypes { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "🪐";
}

public class FinanceOverviewDto
{
    [JsonPropertyName("totalIncome")]
    public long TotalIncome { get; set; }

    [JsonPropertyName("totalSpend")]
    public long TotalSpend { get; set; }

    [JsonPropertyName("totalNet")]
    public long TotalNet { get; set; }

    [JsonPropertyName("sales")]
    public long Sales { get; set; }

    [JsonPropertyName("trade")]
    public long Trade { get; set; }

    [JsonPropertyName("missionsReward")]
    public long MissionsReward { get; set; }

    [JsonPropertyName("purchases")]
    public long Purchases { get; set; }

    [JsonPropertyName("transferIn")]
    public long TransferIn { get; set; }

    [JsonPropertyName("transferOut")]
    public long TransferOut { get; set; }

    [JsonPropertyName("ledger")]
    public List<LogEventDto> Ledger { get; set; } = new();

    [JsonPropertyName("cargo")]
    public List<LogEventDto> Cargo { get; set; } = new();

    [JsonPropertyName("topExpenses")]
    public List<LogEventDto> TopExpenses { get; set; } = new();
}

public class FleetStatDto
{
    [JsonPropertyName("shipName")]
    public string ShipName { get; set; } = "";

    [JsonPropertyName("flights")]
    public int Flights { get; set; }

    [JsonPropertyName("quantumJumps")]
    public int QuantumJumps { get; set; }

    [JsonPropertyName("losses")]
    public int Losses { get; set; }

    [JsonPropertyName("lastUsed")]
    public string LastUsed { get; set; } = "";
}

public class MissionItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("contractor")]
    public string Contractor { get; set; } = "";

    [JsonPropertyName("faction")]
    public string Faction { get; set; } = "";

    [JsonPropertyName("missionType")]
    public string MissionType { get; set; } = "";

    [JsonPropertyName("baseReward")]
    public int BaseReward { get; set; }

    [JsonPropertyName("reputationGain")]
    public int ReputationGain { get; set; }

    [JsonPropertyName("isIllegal")]
    public bool IsIllegal { get; set; }

    [JsonPropertyName("starSystems")]
    public string StarSystems { get; set; } = "Stanton";

    [JsonPropertyName("blueprints")]
    public string[] Blueprints { get; set; } = Array.Empty<string>();

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("isCompleted")]
    public bool IsCompleted { get; set; }

    [JsonPropertyName("time")]
    public string? Time { get; set; }
}

public class FactionReputationDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("shortName")]
    public string ShortName { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "Sicherheit";

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "🛡";

    [JsonPropertyName("system")]
    public string System { get; set; } = "Stanton";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("currentXp")]
    public int CurrentXp { get; set; }

    [JsonPropertyName("completedMissions")]
    public int CompletedMissions { get; set; }

    [JsonPropertyName("currentLevel")]
    public int CurrentLevel { get; set; }

    [JsonPropertyName("levelTitle")]
    public string LevelTitle { get; set; } = "";

    [JsonPropertyName("progressPercent")]
    public double ProgressPercent { get; set; }

    [JsonPropertyName("progressText")]
    public string ProgressText { get; set; } = "";
}

public class BlueprintDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("subCategory")]
    public string SubCategory { get; set; } = "";

    [JsonPropertyName("rarity")]
    public string Rarity { get; set; } = "";

    [JsonPropertyName("requiredMaterials")]
    public string RequiredMaterials { get; set; } = "";

    [JsonPropertyName("unlockInfo")]
    public string UnlockInfo { get; set; } = "";

    [JsonPropertyName("isLearned")]
    public bool IsLearned { get; set; }

    [JsonPropertyName("learnedDate")]
    public string? LearnedDate { get; set; }
}

public class LoadoutSlotDto
{
    [JsonPropertyName("slotKey")]
    public string SlotKey { get; set; } = "";

    [JsonPropertyName("slotName")]
    public string SlotName { get; set; } = "";

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "📦";

    [JsonPropertyName("itemName")]
    public string ItemName { get; set; } = "—";

    [JsonPropertyName("rawClass")]
    public string RawClass { get; set; } = "";

    [JsonPropertyName("armorClass")]
    public string ArmorClass { get; set; } = "";

    [JsonPropertyName("damageReduction")]
    public int DamageReduction { get; set; }

    [JsonPropertyName("tempRange")]
    public string TempRange { get; set; } = "";

    [JsonPropertyName("badgeColor")]
    public string BadgeColor { get; set; } = "#38BDF8";

    [JsonPropertyName("isEquipped")]
    public bool IsEquipped { get; set; }

    [JsonPropertyName("lastEquipped")]
    public string? LastEquipped { get; set; }
}

public class PhotinoBridge
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private PhotinoWindow? _window;
    private LogTailer? _tailer;
    private readonly LogParser _parser = new();
    private string? _currentLogPath;
    private string? _activeSessionName;
    private DateTime? _lastEventTime;

    public void Initialize(PhotinoWindow window)
    {
        _window = window;
        _currentLogPath = Settings.Load().LogPath ?? PathFinder.FindBest();

        // Register Web Message Handler
        _window.RegisterWebMessageReceivedHandler((sender, rawMessage) =>
        {
            Task.Run(() => HandleIncomingMessage(rawMessage));
        });

        // Initialize background watcher if log file exists
        if (!string.IsNullOrEmpty(_currentLogPath) && File.Exists(_currentLogPath))
        {
            StartLogTailer(_currentLogPath);
        }
    }

    public void SendResponse<T>(string? requestId, string type, T payload)
    {
        if (_window == null) return;
        var msg = new
        {
            id = requestId,
            type,
            payload,
        };
        SendRaw(JsonSerializer.Serialize(msg, JsonOpts));
    }

    public void SendError(string? requestId, string errorMessage)
    {
        if (_window == null) return;
        var msg = new
        {
            id = requestId,
            type = "error",
            error = errorMessage,
        };
        SendRaw(JsonSerializer.Serialize(msg, JsonOpts));
    }

    public void Broadcast<T>(string type, T payload)
    {
        if (_window == null) return;
        var msg = new
        {
            type,
            payload,
        };
        SendRaw(JsonSerializer.Serialize(msg, JsonOpts));
    }

    private void SendRaw(string json)
    {
        try
        {
            _window?.SendWebMessage(json);
        }
        catch (Exception ex)
        {
            Logger.Error("PhotinoBridge.SendRaw", ex);
        }
    }

    private void HandleIncomingMessage(string raw)
    {
        try
        {
            var req = JsonSerializer.Deserialize<IpcMessage>(raw, JsonOpts);
            if (req == null) return;

            switch (req.Type)
            {
                case "get_status":
                    SendResponse(req.Id, "status_response", GetAppStatus());
                    break;

                case "get_sessions":
                    SendResponse(req.Id, "sessions_response", GetSessions());
                    break;

                case "get_events":
                    string? category = null;
                    string? search = null;
                    int limit = 100;
                    int offset = 0;
                    if (req.Payload.HasValue)
                    {
                        if (req.Payload.Value.TryGetProperty("category", out var catProp)) category = catProp.GetString();
                        if (req.Payload.Value.TryGetProperty("search", out var sProp)) search = sProp.GetString();
                        if (req.Payload.Value.TryGetProperty("limit", out var limProp)) limit = limProp.GetInt32();
                        if (req.Payload.Value.TryGetProperty("offset", out var offProp)) offset = offProp.GetInt32();
                    }
                    SendResponse(req.Id, "events_response", GetEvents(category, search, limit, offset));
                    break;

                case "get_finance":
                    SendResponse(req.Id, "finance_response", GetFinanceOverview());
                    break;

                case "get_warehouse":
                    string? locFilter = null;
                    string? catFilter = null;
                    string? whSearch = null;
                    if (req.Payload.HasValue)
                    {
                        if (req.Payload.Value.TryGetProperty("location", out var lfProp)) locFilter = lfProp.GetString();
                        if (req.Payload.Value.TryGetProperty("category", out var cfProp)) catFilter = cfProp.GetString();
                        if (req.Payload.Value.TryGetProperty("search", out var wsProp)) whSearch = wsProp.GetString();
                    }
                    SendResponse(req.Id, "warehouse_response", GetWarehouseData(locFilter, catFilter, whSearch));
                    break;

                case "adjust_warehouse_qty":
                    if (req.Payload.HasValue)
                    {
                        string loc = req.Payload.Value.GetProperty("location").GetString() ?? "";
                        string itemClass = req.Payload.Value.GetProperty("itemClass").GetString() ?? "";
                        int delta = req.Payload.Value.GetProperty("delta").GetInt32();
                        Database.AdjustWarehouseItemQuantity(loc, itemClass, delta);
                        SendResponse(req.Id, "adjust_warehouse_qty_response", GetWarehouseData(loc, null, null));
                        Broadcast("WAREHOUSE_UPDATED", GetWarehouseData(null, null, null));
                    }
                    break;

                case "delete_warehouse_item":
                    if (req.Payload.HasValue)
                    {
                        string loc = req.Payload.Value.GetProperty("location").GetString() ?? "";
                        string itemClass = req.Payload.Value.GetProperty("itemClass").GetString() ?? "";
                        Database.DeleteWarehouseItem(loc, itemClass);
                        SendResponse(req.Id, "delete_warehouse_item_response", GetWarehouseData(loc, null, null));
                        Broadcast("WAREHOUSE_UPDATED", GetWarehouseData(null, null, null));
                    }
                    break;

                case "clear_warehouse_location":
                    if (req.Payload.HasValue)
                    {
                        string loc = req.Payload.Value.GetProperty("location").GetString() ?? "";
                        Database.ClearWarehouseLocation(loc);
                        SendResponse(req.Id, "clear_warehouse_location_response", GetWarehouseData(null, null, null));
                        Broadcast("WAREHOUSE_UPDATED", GetWarehouseData(null, null, null));
                    }
                    break;

                case "get_fleet":
                    SendResponse(req.Id, "fleet_response", GetFleetData());
                    break;

                case "get_missions":
                    SendResponse(req.Id, "missions_response", GetMissionsData());
                    break;

                case "get_reputation":
                    SendResponse(req.Id, "reputation_response", GetReputationData());
                    break;

                case "get_blueprints":
                    SendResponse(req.Id, "blueprints_response", GetBlueprintsData());
                    break;

                case "get_loadout":
                    SendResponse(req.Id, "loadout_response", GetLoadoutData());
                    break;

                case "toggle_watcher":
                    bool enable = true;
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("enable", out var enableProp))
                    {
                        enable = enableProp.GetBoolean();
                    }
                    else
                    {
                        enable = _tailer == null || !_tailer.IsLiveStreaming;
                    }

                    if (enable)
                    {
                        if (!string.IsNullOrEmpty(_currentLogPath) && File.Exists(_currentLogPath))
                        {
                            StartLogTailer(_currentLogPath);
                        }
                    }
                    else
                    {
                        _tailer?.Stop();
                        _tailer = null;
                    }

                    SendResponse(req.Id, "toggle_watcher_response", new { isLiveWatching = _tailer != null });
                    Broadcast("STATUS_UPDATE", GetAppStatus());
                    break;

                case "scan_logs":
                    int scanned = TriggerScan();
                    SendResponse(req.Id, "scan_logs_response", new { scannedCount = scanned });
                    Broadcast("STATUS_UPDATE", GetAppStatus());
                    Broadcast("sessions_response", GetSessions());
                    break;

                default:
                    SendResponse(req.Id, $"{req.Type}_ack", new { success = true });
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Error("PhotinoBridge.HandleIncomingMessage", ex);
        }
    }

    private AppStatusDto GetAppStatus()
    {
        Database.EnsureInitialized();
        var agg = Database.Aggregate(since: null, filterMoney: true, filterContracts: true, filterFleet: false);

        long income = agg.In + agg.Reward + agg.Sales + agg.Trade;
        long spend = agg.Out + agg.Purchases;

        return new AppStatusDto
        {
            Version = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "1.0.0-rc2",
            IsLiveWatching = _tailer != null,
            LogPath = _currentLogPath,
            ActiveSessionName = _activeSessionName ?? "Live Session",
            DbSessionCount = Database.GetSessionCount(),
            TotalIncome = income,
            TotalSpend = spend,
            TotalNet = income - spend,
            LastEventTime = _lastEventTime?.ToString("HH:mm:ss"),
        };
    }

    private List<SessionSummaryDto> GetSessions()
    {
        Database.EnsureInitialized();
        var list = new List<SessionSummaryDto>();

        try
        {
            using var db = new SqliteConnection($"Data Source={Database.DatabaseFilePath};Default Timeout=60;");
            db.Open();

            const string sql = @"
                SELECT s.name, s.start, s.end,
                       COALESCE(SUM(CASE WHEN e.kind IN ('TransferIn', 'MissionReward', 'Sale', 'Trade') THEN e.amount ELSE 0 END), 0) AS income,
                       COALESCE(SUM(CASE WHEN e.kind IN ('TransferOut', 'Purchase', 'Fine', 'Maintenance') THEN -e.amount ELSE 0 END), 0) AS spend,
                       COALESCE(SUM(CASE WHEN e.kind = 'Sale' THEN e.amount ELSE 0 END), 0) AS sales,
                       COALESCE(SUM(CASE WHEN e.kind = 'Trade' THEN e.amount ELSE 0 END), 0) AS trade,
                       COUNT(CASE WHEN e.kind = 'MedBed' THEN 1 END) AS deaths,
                       COUNT(CASE WHEN e.kind IN ('Mission', 'MissionDone') THEN 1 END) AS missions,
                       (SELECT e2.detail FROM events e2 WHERE e2.session = s.name AND e2.kind = 'Location' ORDER BY e2.time DESC LIMIT 1) AS last_loc,
                       GROUP_CONCAT(DISTINCT e.ship) AS ships
                FROM sessions s
                LEFT JOIN events e ON e.session = s.name
                GROUP BY s.name, s.start, s.end
                ORDER BY s.start DESC
                LIMIT 50;";

            using var cmd = db.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();

            int idx = 1;
            while (reader.Read())
            {
                string name = reader.GetString(0);
                string? startStr = reader.IsDBNull(1) ? null : reader.GetString(1);
                string? endStr = reader.IsDBNull(2) ? null : reader.GetString(2);
                long inc = reader.GetInt64(3);
                long spd = reader.GetInt64(4);
                long sal = reader.GetInt64(5);
                long trd = reader.GetInt64(6);
                int deaths = reader.GetInt32(7);
                int missions = reader.GetInt32(8);
                string lastLoc = reader.IsDBNull(9) ? "—" : reader.GetString(9);
                string? shipsRaw = reader.IsDBNull(10) ? null : reader.GetString(10);

                DateTime.TryParse(startStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var st);
                DateTime.TryParse(endStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var en);

                var dur = en > st ? (en - st) : TimeSpan.Zero;
                string durStr = dur.TotalHours >= 1
                    ? $"{(int)dur.TotalHours}h {dur.Minutes}m"
                    : $"{dur.Minutes}m";

                var shipsList = !string.IsNullOrEmpty(shipsRaw)
                    ? shipsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                    : new List<string>();

                list.Add(new SessionSummaryDto
                {
                    Id = idx++,
                    Name = name,
                    StartTime = st != DateTime.MinValue ? st.ToLocalTime().ToString("dd.MM. HH:mm") : "—",
                    EndTime = en != DateTime.MinValue ? en.ToLocalTime().ToString("HH:mm") : "—",
                    Duration = durStr,
                    Income = inc,
                    Spend = spd,
                    Net = inc - spd,
                    Sales = sal,
                    Trade = trd,
                    Deaths = deaths,
                    Missions = missions,
                    Ships = shipsList,
                    LastLocation = lastLoc,
                });
            }

            if (list.Count == 0 && !string.IsNullOrEmpty(_currentLogPath))
            {
                var scanned = SessionScanner.Scan(_currentLogPath);
                foreach (var s in scanned.Take(25))
                {
                    list.Add(new SessionSummaryDto
                    {
                        Id = idx++,
                        Name = Path.GetFileName(s.Path),
                        StartTime = s.Start.ToLocalTime().ToString("dd.MM. HH:mm"),
                        EndTime = "—",
                        Duration = "—",
                        Income = 0,
                        Spend = 0,
                        Net = 0,
                        Sales = 0,
                        Trade = 0,
                        Deaths = 0,
                        Missions = 0,
                        Ships = new List<string>(),
                        LastLocation = "—",
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("PhotinoBridge.GetSessions", ex);
        }

        return list;
    }

    private List<LogEventDto> GetEvents(string? categoryFilter, string? searchQuery, int limit, int offset)
    {
        Database.EnsureInitialized();
        var rawEvents = Database.LoadRecentEvents(2500);
        var query = rawEvents.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(categoryFilter) && categoryFilter != "all")
        {
            query = query.Where(e => MapCategory(e.Kind).Equals(categoryFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.Where(e =>
                (e.Detail != null && e.Detail.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                (e.Ship != null && e.Ship.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                e.KindText.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .Reverse()
            .Skip(offset)
            .Take(limit)
            .Select(e => new LogEventDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Timestamp = e.Time.ToLocalTime().ToString("dd.MM. HH:mm:ss"),
                Category = MapCategory(e.Kind),
                Title = e.KindText,
                Description = e.Detail ?? e.KindText,
                Amount = e.Amount != 0 ? e.Amount : null,
                Ship = e.Ship,
                RawText = e.Detail,
            })
            .ToList();
    }

    private FinanceOverviewDto GetFinanceOverview()
    {
        Database.EnsureInitialized();
        var agg = Database.Aggregate(since: null, filterMoney: true, filterContracts: true, filterFleet: false);

        var financeEvents = Database.AllFinanceEvents().Take(100).Select(e => new LogEventDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = e.Time.ToLocalTime().ToString("dd.MM. HH:mm"),
            Category = MapCategory(e.Kind),
            Title = e.KindText,
            Description = e.Detail ?? e.KindText,
            Amount = e.Amount,
            Ship = e.Ship,
        }).ToList();

        var cargoEvents = Database.AllTrades().Take(100).Select(e => new LogEventDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = e.Time.ToLocalTime().ToString("dd.MM. HH:mm"),
            Category = "wallet",
            Title = e.KindText,
            Description = e.Detail ?? e.KindText,
            Amount = e.Amount,
            Ship = e.Ship,
        }).ToList();

        var topMoney = Database.TopMoney(15).Select(e => new LogEventDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = e.Time.ToLocalTime().ToString("dd.MM. HH:mm"),
            Category = "wallet",
            Title = e.KindText,
            Description = e.Detail ?? e.KindText,
            Amount = e.Amount,
            Ship = e.Ship,
        }).ToList();

        return new FinanceOverviewDto
        {
            TotalIncome = agg.In + agg.Reward + agg.Sales + agg.Trade,
            TotalSpend = agg.Out + agg.Purchases,
            TotalNet = (agg.In + agg.Reward + agg.Sales + agg.Trade) - (agg.Out + agg.Purchases),
            Sales = agg.Sales,
            Trade = agg.Trade,
            MissionsReward = agg.Reward,
            Purchases = agg.Purchases,
            TransferIn = agg.In,
            TransferOut = agg.Out,
            Ledger = financeEvents,
            Cargo = cargoEvents,
            TopExpenses = topMoney,
        };
    }

    private object GetWarehouseData(string? locationFilter, string? categoryFilter, string? search)
    {
        Database.EnsureInitialized();
        var locationsRaw = Database.GetWarehouseLocationsSummary();
        var itemsRaw = Database.GetWarehouseItems(locationFilter, categoryFilter, search);

        var locations = locationsRaw.Select(l => new WarehouseLocationDto
        {
            LocationName = l.LocationName,
            LocationCode = l.LocationCode,
            System = l.System,
            ParentBody = l.ParentBody,
            TotalItems = l.TotalItems,
            UniqueItemTypes = l.UniqueItemTypes,
            Icon = l.Icon,
        }).ToList();

        var items = itemsRaw.Select(i => new WarehouseItemDto
        {
            Location = i.Location,
            LocationCode = i.LocationCode,
            System = i.System,
            ParentBody = i.ParentBody,
            ItemClass = i.ItemClass,
            ItemName = i.ItemName,
            Category = i.Category,
            Quantity = i.Quantity,
            LastUpdated = i.FormattedDate,
            Icon = i.Icon,
            LocationDisplay = i.LocationDisplay,
        }).ToList();

        return new
        {
            locations,
            items,
        };
    }

    private List<FleetStatDto> GetFleetData()
    {
        Database.EnsureInitialized();
        var stats = Database.GetFleetStats();
        return stats.Select(s => new FleetStatDto
        {
            ShipName = s.Ship,
            Flights = s.FlightCount,
            QuantumJumps = s.QtCount,
            Losses = s.LossCount,
            LastUsed = s.LastTime.HasValue ? s.LastTime.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm") : "—",
        }).ToList();
    }

    private object GetMissionsData()
    {
        Database.EnsureInitialized();
        var catalog = MissionCatalog.AllMissions.Take(250).Select(m => new MissionItemDto
        {
            Id = m.Id,
            Title = m.Title,
            Contractor = m.Contractor,
            Faction = m.Faction,
            MissionType = m.MissionType,
            BaseReward = m.BaseReward,
            ReputationGain = m.ReputationGain,
            IsIllegal = m.IsIllegal,
            StarSystems = m.StarSystems,
            Blueprints = m.Blueprints,
            Description = m.Description,
        }).ToList();

        var activeContracts = Database.GetActiveContracts().Select(c => new MissionItemDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = c.Title,
            Contractor = c.ContractedBy,
            Faction = c.ContractedBy,
            BaseReward = c.Reward,
            IsActive = true,
            Description = c.DisplayText,
            Time = c.ScannedAt.ToLocalTime().ToString("dd.MM. HH:mm"),
        }).ToList();

        var history = Database.LoadRecentEvents(2500)
            .Where(e => e.Kind is EventKind.Mission or EventKind.MissionDone or EventKind.MissionTaken)
            .Reverse()
            .Take(100)
            .Select(e => new MissionItemDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = e.Detail ?? e.KindText,
                Contractor = "Star Citizen Auftragsmanager",
                BaseReward = (int)e.Amount,
                IsCompleted = e.Kind == EventKind.MissionDone,
                Time = e.Time.ToLocalTime().ToString("dd.MM. HH:mm"),
            }).ToList();

        return new
        {
            active = activeContracts,
            history,
            catalog,
        };
    }

    private List<FactionReputationDto> GetReputationData()
    {
        Database.EnsureInitialized();
        var list = ReputationCatalog.CreateFreshFactionList();

        try
        {
            var missionEvents = Database.LoadRecentEvents(5000)
                .Where(e => e.Kind is EventKind.Mission or EventKind.MissionDone)
                .ToList();

            foreach (var ev in missionEvents)
            {
                var matched = ReputationCatalog.MatchFaction(ev.Detail);
                if (matched != null)
                {
                    var target = list.FirstOrDefault(f => f.Id == matched.Id);
                    if (target != null)
                    {
                        target.CompletedMissions++;
                        target.CurrentXp += 250;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("PhotinoBridge.GetReputationData", ex);
        }

        return list.Select(f => new FactionReputationDto
        {
            Id = f.Id,
            Name = f.Name,
            ShortName = f.ShortName,
            Category = f.Category,
            Icon = f.Icon,
            System = f.System,
            Description = f.Description,
            CurrentXp = f.CurrentXp,
            CompletedMissions = f.CompletedMissions,
            CurrentLevel = f.CurrentLevel,
            LevelTitle = f.LevelTitle,
            ProgressPercent = f.LevelProgressPercent,
            ProgressText = f.ProgressText,
        }).ToList();
    }

    private List<BlueprintDto> GetBlueprintsData()
    {
        Database.EnsureInitialized();
        var catalog = BlueprintCatalog.CreateFreshCatalog();
        var learnedDistinct = new HashSet<string>(Database.DistinctBlueprints(), StringComparer.OrdinalIgnoreCase);

        var events = Database.AllBlueprintEvents();
        var learnedDates = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        foreach (var ev in events)
        {
            if (!string.IsNullOrEmpty(ev.Detail) && !learnedDates.ContainsKey(ev.Detail))
            {
                learnedDates[ev.Detail] = ev.Time;
            }
        }

        var result = new List<BlueprintDto>();
        foreach (var b in catalog)
        {
            bool isLearned = learnedDistinct.Contains(b.Name) || learnedDates.ContainsKey(b.Name);
            string? dateStr = null;
            if (learnedDates.TryGetValue(b.Name, out var dt))
            {
                dateStr = dt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
            }

            result.Add(new BlueprintDto
            {
                Id = b.Id,
                Name = b.Name,
                Category = b.Category,
                SubCategory = b.SubCategory,
                Rarity = b.Rarity,
                RequiredMaterials = b.RequiredMaterials,
                UnlockInfo = b.UnlockInfo,
                IsLearned = isLearned,
                LearnedDate = dateStr,
            });
        }

        return result;
    }

    private List<LoadoutSlotDto> GetLoadoutData()
    {
        Database.EnsureInitialized();
        var slots = new List<LoadoutSlotDto>
        {
            new() { SlotKey = "Helmet", SlotName = "Helm", Icon = "🪖" },
            new() { SlotKey = "Torso", SlotName = "Torso / Core", Icon = "🥋" },
            new() { SlotKey = "Arms", SlotName = "Arme", Icon = "🦾" },
            new() { SlotKey = "Legs", SlotName = "Beine", Icon = "🦿" },
            new() { SlotKey = "Undersuit", SlotName = "Undersuit", Icon = "🩱" },
            new() { SlotKey = "Backpack", SlotName = "Rucksack", Icon = "🎒" },
            new() { SlotKey = "Primary1", SlotName = "Primärwaffe 1", Icon = "🎯" },
            new() { SlotKey = "Primary2", SlotName = "Primärwaffe 2", Icon = "🎯" },
            new() { SlotKey = "Sidearm", SlotName = "Seitenwaffe", Icon = "🔫" },
            new() { SlotKey = "MultiTool", SlotName = "Multi-Tool", Icon = "🔧" },
            new() { SlotKey = "MedItem", SlotName = "Med-Kit / Pen", Icon = "💉" },
        };

        try
        {
            var loadoutEvents = Database.LoadRecentEvents(2500)
                .Where(e => e.Kind == EventKind.Loadout && !string.IsNullOrWhiteSpace(e.Detail))
                .ToList();

            foreach (var ev in loadoutEvents)
            {
                var (slotType, slotName, _) = LogParser.ClassifyLoadoutSlot(ev.Detail);
                string slotKey = slotType.ToString();
                var slot = slots.FirstOrDefault(s => s.SlotKey.Equals(slotKey, StringComparison.OrdinalIgnoreCase));
                if (slot != null)
                {
                    string clean = ItemNames.CleanFallback(ev.Detail);
                    var (armorClass, dmgRed, tempRange, badgeColor, _, _) = LoadoutCatalog.GetItemMeta(slotType, ev.Detail, clean);
                    slot.ItemName = clean;
                    slot.RawClass = ev.Detail;
                    slot.ArmorClass = armorClass;
                    slot.DamageReduction = dmgRed;
                    slot.TempRange = tempRange;
                    slot.BadgeColor = badgeColor;
                    slot.IsEquipped = true;
                    slot.LastEquipped = ev.Time.ToLocalTime().ToString("dd.MM. HH:mm");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("PhotinoBridge.GetLoadoutData", ex);
        }

        return slots;
    }

    private int TriggerScan()
    {
        try
        {
            var targetDir = !string.IsNullOrEmpty(_currentLogPath)
                ? Path.GetDirectoryName(_currentLogPath)
                : null;

            if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir)) return 0;

            var files = Directory.GetFiles(targetDir, "*.log", SearchOption.AllDirectories);
            if (files.Length == 0) return 0;

            int added = Database.IndexNew(files);
            Logger.Log($"PhotinoBridge: Scan ausgeführt – {added} neue Sessions indexiert.");
            return added;
        }
        catch (Exception ex)
        {
            Logger.Error("PhotinoBridge.TriggerScan", ex);
            return 0;
        }
    }

    private void StartLogTailer(string path)
    {
        try
        {
            _tailer?.Stop();
            _tailer = new LogTailer(path);
            _tailer.Line += OnLogLineReceived;
            _tailer.Start(fromStart: false);
            _activeSessionName = Path.GetFileName(path);
            Logger.Log($"PhotinoBridge: LogTailer gestartet für {path}");
        }
        catch (Exception ex)
        {
            Logger.Error("PhotinoBridge.StartLogTailer", ex);
        }
    }

    private void OnLogLineReceived(string rawLine)
    {
        try
        {
            var entry = _parser.Feed(rawLine);
            if (entry == null) return;

            _lastEventTime = entry.Time;

            var dto = new LogEventDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Timestamp = entry.Time.ToLocalTime().ToString("HH:mm:ss"),
                Category = MapCategory(entry.Kind),
                Title = entry.KindText,
                Description = entry.Detail ?? entry.KindText,
                Amount = entry.Amount != 0 ? entry.Amount : null,
                Ship = entry.Ship,
                RawText = rawLine.Length > 120 ? rawLine[..120] + "…" : rawLine,
            };

            Broadcast("LOG_EVENT", dto);
        }
        catch (Exception ex)
        {
            Logger.Error("PhotinoBridge.OnLogLineReceived", ex);
        }
    }

    private static string MapCategory(EventKind kind) => kind switch
    {
        EventKind.TransferIn or EventKind.TransferOut or EventKind.Purchase or
        EventKind.Sale or EventKind.Trade or EventKind.MissionReward or EventKind.Fine => "wallet",
        EventKind.Death or EventKind.ShipLoss or EventKind.Kill or EventKind.Injury => "combat",
        EventKind.Mission or EventKind.MissionDone or EventKind.MissionTaken => "mission",
        EventKind.Vehicle or EventKind.Quantum or EventKind.Hangar => "ship",
        EventKind.Location or EventKind.Jurisdiction => "location",
        _ => "system"
    };
}
