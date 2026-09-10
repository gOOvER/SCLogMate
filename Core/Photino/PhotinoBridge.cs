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

    [JsonPropertyName("rawText")]
    public string? RawText { get; set; }
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

            // Fallback: If DB had no sessions yet, scan files on disk
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
