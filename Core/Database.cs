using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using SCLogReader.Models;

namespace SCLogReader.Core;

/// <summary>
/// Lokale SQLite-Datenbank als nachbaubarer Cache/Index der fertigen Sessions.
/// Quelle der Wahrheit bleiben die archivierten Roh-Logs (LogArchive).
/// - Schema-Version: PRAGMA user_version (Tabellenstruktur)
/// - Parser-Version: bei Erhöhung wird die DB aus dem Archiv NEU aufgebaut.
/// </summary>
public static class Database
{
    public const int CurrentSchemaVersion = 6;  // Erhöhen bei Tabellen- oder Spalten-Änderungen
    public const int CurrentParserVersion = 23; // Erhöhen, wenn der LogParser neue Felder/Events liefert

    public static bool WasParserResetRequired { get; set; }

    static string DbPath => Path.Combine(Settings.Dir, "sessions.db");

    static string Conn => $"Data Source={DbPath}";

    private static readonly object _initLock = new();
    private static bool _isInitialized;

    public static void EnsureInitialized()
    {
        if (_isInitialized) return;
        lock (_initLock)
        {
            if (_isInitialized) return;
            Init();
            _isInitialized = true;
        }
    }

    public static void Init()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
        using var db = new SqliteConnection(Conn);
        db.Open();

        Exec(db, @"PRAGMA journal_mode = WAL;
                   PRAGMA synchronous = NORMAL;
                   PRAGMA busy_timeout = 5000;");

        // 1. Schema-Migrationen anwenden (PRAGMA user_version)
        ApplySchemaMigrations(db);

        // 2. Parser-Version prüfen -> bei Änderung Cache leeren & neu indexieren
        CheckParserVersion(db);
        _isInitialized = true;
    }

    /// <summary>
    /// Führt inkrementelle Schema-Upgrades (Tabellen, Spalten, Indizes) strukturiert aus.
    /// </summary>
    private static void ApplySchemaMigrations(SqliteConnection db)
    {
        var versionObj = Scalar(db, "PRAGMA user_version;");
        int dbSchemaVersion = Convert.ToInt32(versionObj ?? 0);

        if (dbSchemaVersion < 1)
        {
            // Initial-Schema v1
            Exec(db, @"
                CREATE TABLE IF NOT EXISTS meta(key TEXT PRIMARY KEY, value TEXT);
                CREATE TABLE IF NOT EXISTS sessions(name TEXT PRIMARY KEY, start TEXT, end TEXT);
                CREATE TABLE IF NOT EXISTS events(session TEXT, time TEXT, kind TEXT, amount INTEGER, detail TEXT, ship TEXT);
                CREATE INDEX IF NOT EXISTS ix_events_session ON events(session);
                CREATE INDEX IF NOT EXISTS ix_events_kind ON events(kind);
                CREATE INDEX IF NOT EXISTS ix_events_time ON events(time);
            ");
            Exec(db, "PRAGMA user_version = 1;");
            dbSchemaVersion = 1;
            Logger.Log("DB Schema: Initialversion 1 angewendet.");
        }

        if (dbSchemaVersion < 2)
        {
            Exec(db, @"
                CREATE TABLE IF NOT EXISTS contracts(
                    id TEXT PRIMARY KEY,
                    title TEXT NOT NULL,
                    reward INTEGER NOT NULL,
                    contracted_by TEXT NOT NULL,
                    scanned_at TEXT NOT NULL,
                    status TEXT NOT NULL DEFAULT 'Active'
                );
                CREATE INDEX IF NOT EXISTS ix_contracts_status ON contracts(status);
            ");
            Exec(db, "PRAGMA user_version = 2;");
            dbSchemaVersion = 2;
            Logger.Log("DB Schema: Migration auf v2 (contracts Tabelle) erfolgreich angewendet.");
        }

        if (dbSchemaVersion < 3)
        {
            Exec(db, @"
                CREATE TABLE IF NOT EXISTS user_pois(
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    system TEXT NOT NULL,
                    body TEXT NOT NULL,
                    name TEXT NOT NULL,
                    notes TEXT NOT NULL,
                    category TEXT NOT NULL,
                    color TEXT NOT NULL,
                    created_at TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ix_user_pois_system ON user_pois(system);
            ");
            Exec(db, "PRAGMA user_version = 3;");
            dbSchemaVersion = 3;
            Logger.Log("DB Schema: Migration auf v3 (user_pois Tabelle) erfolgreich angewendet.");
        }

        if (dbSchemaVersion < 4)
        {
            Exec(db, @"
                CREATE TABLE IF NOT EXISTS reputation(
                    faction_id TEXT PRIMARY KEY,
                    xp INTEGER NOT NULL DEFAULT 0,
                    completed_missions INTEGER NOT NULL DEFAULT 0,
                    last_updated TEXT NOT NULL
                );
            ");
            Exec(db, "PRAGMA user_version = 4;");
            dbSchemaVersion = 4;
            Logger.Log("DB Schema: Migration auf v4 (reputation Tabelle) erfolgreich angewendet.");
        }

        if (dbSchemaVersion < 5)
        {
            Exec(db, @"
                CREATE TABLE IF NOT EXISTS fleet_user_ships(
                    name TEXT PRIMARY KEY,
                    in_hangar INTEGER NOT NULL DEFAULT 1,
                    is_pledge INTEGER NOT NULL DEFAULT 1,
                    pledge_usd INTEGER NOT NULL DEFAULT 0,
                    insurance TEXT NOT NULL DEFAULT 'LTI (Lifetime)',
                    acquisition TEXT NOT NULL DEFAULT 'Pledge Store',
                    notes TEXT NOT NULL DEFAULT ''
                );
            ");
            Exec(db, "PRAGMA user_version = 5;");
            dbSchemaVersion = 5;
            Logger.Log("DB Schema: Migration auf v5 (fleet_user_ships Tabelle) erfolgreich angewendet.");
        }

        if (dbSchemaVersion < 6)
        {
            try { Exec(db, "ALTER TABLE fleet_user_ships ADD COLUMN in_hangar INTEGER NOT NULL DEFAULT 1;"); } catch { }
            Exec(db, "PRAGMA user_version = 6;");
            dbSchemaVersion = 6;
            Logger.Log("DB Schema: Migration auf v6 (fleet_user_ships mit in_hangar Spalte) angewendet.");
        }

        SetMeta(db, "schemaVersion", CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Prüft, ob der Log-Parser aktualisiert wurde. Wenn ja, werden Rohlogs neu indexiert.
    /// </summary>
    private static void CheckParserVersion(SqliteConnection db)
    {
        var stored = GetMeta(db, "parserVersion");
        if (stored != CurrentParserVersion.ToString(CultureInfo.InvariantCulture))
        {
            Exec(db, "DELETE FROM events; DELETE FROM sessions;");
            SetMeta(db, "parserVersion", CurrentParserVersion.ToString(CultureInfo.InvariantCulture));
            WasParserResetRequired = true;
            Logger.Log($"DB: Parser-Version auf v{CurrentParserVersion} aktualisiert -> Cache für Re-Indexierung geleert.");
        }
    }

    /// <summary>Liefert die Anzahl der bereits in der DB indexierten Sessions.</summary>
    public static int GetSessionCount()
    {
        using var db = new SqliteConnection(Conn);
        db.Open();
        var count = Scalar(db, "SELECT COUNT(*) FROM sessions;");
        return Convert.ToInt32(count ?? 0);
    }

    /// <summary>Prüft, wie viele der übergebenen Logdateien noch nicht in der DB indexiert sind.</summary>
    public static int GetUnindexedCount(IEnumerable<string> logFiles)
    {
        using var db = new SqliteConnection(Conn);
        db.Open();
        int unindexed = 0;
        foreach (var file in logFiles)
        {
            var name = Path.GetFileName(file);
            if (Scalar(db, "SELECT 1 FROM sessions WHERE name=$n LIMIT 1;", ("$n", name)) == null)
            {
                unindexed++;
            }
        }
        return unindexed;
    }

    /// <summary>Parst und speichert alle Logs, die noch nicht in der DB sind. Liefert Anzahl neuer.</summary>
    public static int IndexNew(IEnumerable<string> logFiles, Action<int, int, string>? onProgress = null)
    {
        using var db = new SqliteConnection(Conn);
        db.Open();
        int added = 0;
        var filesList = new List<string>(logFiles);
        int total = filesList.Count;

        for (int i = 0; i < total; i++)
        {
            var file = filesList[i];
            var name = Path.GetFileName(file);
            if (Scalar(db, "SELECT 1 FROM sessions WHERE name=$n", ("$n", name)) != null) continue;

            onProgress?.Invoke(i + 1, total, name);

            try
            {
                var parser = new LogParser();
                DateTime? first = null, last = null;
                using var tx = db.BeginTransaction();
                using var cmd = db.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT INTO events(session,time,kind,amount,detail,ship) VALUES($s,$t,$k,$a,$d,$sh)";
                var ps = cmd.Parameters.Add("$s", SqliteType.Text); ps.Value = name;
                var pt = cmd.Parameters.Add("$t", SqliteType.Text);
                var pk = cmd.Parameters.Add("$k", SqliteType.Text);
                var pa = cmd.Parameters.Add("$a", SqliteType.Integer);
                var pd = cmd.Parameters.Add("$d", SqliteType.Text);
                var psh = cmd.Parameters.Add("$sh", SqliteType.Text);

                foreach (var line in ReadShared(file))
                {
                    var e = parser.Feed(line);
                    if (e == null) continue;
                    if (first == null || e.Time < first) first = e.Time;
                    if (last == null || e.Time > last) last = e.Time;
                    pt.Value = e.Time.ToString("o", CultureInfo.InvariantCulture);
                    pk.Value = e.Kind.ToString();
                    pa.Value = e.Amount;
                    pd.Value = e.Detail ?? "";
                    psh.Value = (object?)e.Ship ?? DBNull.Value;
                    cmd.ExecuteNonQuery();
                }

                using (var s = db.CreateCommand())
                {
                    s.Transaction = tx;
                    s.CommandText = "INSERT OR REPLACE INTO sessions(name,start,end) VALUES($n,$st,$en)";
                    s.Parameters.AddWithValue("$n", name);
                    s.Parameters.AddWithValue("$st", (object?)first?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
                    s.Parameters.AddWithValue("$en", (object?)last?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
                    s.ExecuteNonQuery();
                }
                tx.Commit();
                added++;
            }
            catch (Exception ex) { Logger.Error("Index " + name, ex); }
        }
        return added;
    }

    /// <summary>Leert die Datenbank vollständig (Events + Sessions) und komprimiert per VACUUM.</summary>
    public static void ClearAll()
    {
        using var db = new SqliteConnection(Conn);
        db.Open();
        Exec(db, "DELETE FROM events; DELETE FROM sessions;");
        Exec(db, "PRAGMA wal_checkpoint(TRUNCATE); VACUUM;");
        Logger.Log("DB: Alle Events und Sessions vollständig zurückgesetzt.");
    }

    /// <summary>Führt eine vollständige Neu-Indexierung aller Logs durch (Re-Scan).</summary>
    public static (int indexedSessions, int totalEvents) RescanAll(IEnumerable<string> logFiles, Action<int, int, string>? onProgress = null)
    {
        ClearAll();
        var files = new List<string>(logFiles);
        int totalFiles = files.Count;
        int sessionCount = 0;
        int eventCount = 0;

        using var db = new SqliteConnection(Conn);
        db.Open();

        for (int i = 0; i < totalFiles; i++)
        {
            var file = files[i];
            var name = Path.GetFileName(file);
            onProgress?.Invoke(i + 1, totalFiles, name);

            try
            {
                var parser = new LogParser();
                DateTime? first = null, last = null;
                using var tx = db.BeginTransaction();
                using var cmd = db.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "INSERT INTO events(session,time,kind,amount,detail,ship) VALUES($s,$t,$k,$a,$d,$sh)";
                var ps = cmd.Parameters.Add("$s", SqliteType.Text); ps.Value = name;
                var pt = cmd.Parameters.Add("$t", SqliteType.Text);
                var pk = cmd.Parameters.Add("$k", SqliteType.Text);
                var pa = cmd.Parameters.Add("$a", SqliteType.Integer);
                var pd = cmd.Parameters.Add("$d", SqliteType.Text);
                var psh = cmd.Parameters.Add("$sh", SqliteType.Text);

                int sessionEvents = 0;
                foreach (var line in ReadShared(file))
                {
                    var e = parser.Feed(line);
                    if (e == null) continue;
                    if (first == null || e.Time < first) first = e.Time;
                    if (last == null || e.Time > last) last = e.Time;
                    pt.Value = e.Time.ToString("o", CultureInfo.InvariantCulture);
                    pk.Value = e.Kind.ToString();
                    pa.Value = e.Amount;
                    pd.Value = e.Detail ?? "";
                    psh.Value = (object?)e.Ship ?? DBNull.Value;
                    cmd.ExecuteNonQuery();
                    sessionEvents++;
                }

                using (var s = db.CreateCommand())
                {
                    s.Transaction = tx;
                    s.CommandText = "INSERT OR REPLACE INTO sessions(name,start,end) VALUES($n,$st,$en)";
                    s.Parameters.AddWithValue("$n", name);
                    s.Parameters.AddWithValue("$st", (object?)first?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
                    s.Parameters.AddWithValue("$en", (object?)last?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
                    s.ExecuteNonQuery();
                }
                tx.Commit();
                sessionCount++;
                eventCount += sessionEvents;
            }
            catch (Exception ex)
            {
                Logger.Error("Rescan " + name, ex);
            }
        }

        Exec(db, "PRAGMA wal_checkpoint(TRUNCATE); VACUUM;");
        Logger.Log($"DB: Re-Scan beendet: {sessionCount} Sessions, {eventCount} Events.");
        return (sessionCount, eventCount);
    }

    /// <summary>Bereinigt verwaiste Einträge, optimiert Indizes und führt VACUUM aus.</summary>
    public static (int cleanedEvents, int cleanedSessions, long sizeBefore, long sizeAfter) Cleanup()
    {
        long sizeBefore = GetDatabaseSizeBytes();
        int cleanedEvents = 0;
        int cleanedSessions = 0;

        using (var db = new SqliteConnection(Conn))
        {
            db.Open();
            using (var tx = db.BeginTransaction())
            {
                // Ungültige/leere Datensätze entfernen
                using var c1 = db.CreateCommand();
                c1.Transaction = tx;
                c1.CommandText = "DELETE FROM events WHERE time IS NULL OR kind IS NULL OR trim(time) = ''";
                cleanedEvents += c1.ExecuteNonQuery();

                // Verwaiste Sessions ohne Events entfernen
                using var c2 = db.CreateCommand();
                c2.Transaction = tx;
                c2.CommandText = "DELETE FROM sessions WHERE name NOT IN (SELECT DISTINCT session FROM events WHERE session IS NOT NULL)";
                cleanedSessions += c2.ExecuteNonQuery();

                tx.Commit();
            }

            Exec(db, "PRAGMA optimize;");
            Exec(db, "PRAGMA wal_checkpoint(TRUNCATE);");
            Exec(db, "VACUUM;");
        }

        long sizeAfter = GetDatabaseSizeBytes();
        Logger.Log($"DB: Cleanup abgeschlossen. Vorher: {FormatBytes(sizeBefore)}, Nachher: {FormatBytes(sizeAfter)}.");
        return (cleanedEvents, cleanedSessions, sizeBefore, sizeAfter);
    }

    public static long GetDatabaseSizeBytes()
    {
        try
        {
            long total = 0;
            if (File.Exists(DbPath)) total += new FileInfo(DbPath).Length;
            var wal = DbPath + "-wal";
            if (File.Exists(wal)) total += new FileInfo(wal).Length;
            var shm = DbPath + "-shm";
            if (File.Exists(shm)) total += new FileInfo(shm).Length;
            return total;
        }
        catch { return 0; }
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }

    /// <summary>Alle gespeicherten Events chronologisch (älteste zuerst).</summary>
    public static IEnumerable<LogEntry> LoadAllEvents()
    {
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT time,kind,amount,detail,ship FROM events ORDER BY time";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            DateTime.TryParse(r.GetString(0), CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t);
            Enum.TryParse<EventKind>(r.GetString(1), out var kind);
            yield return new LogEntry
            {
                Time = t,
                Kind = kind,
                Amount = r.GetInt64(2),
                Detail = r.IsDBNull(3) ? "" : r.GetString(3),
                Ship = r.IsDBNull(4) ? null : r.GetString(4)
            };
        }
    }

    /// <summary>Alle Fracht-Verkäufe (Kind=Trade) für die „Handel je Ware"-Übersicht.</summary>
    public static List<LogEntry> AllTrades()
    {
        var list = new List<LogEntry>();
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var c = db.CreateCommand();
        c.CommandText = "SELECT time,amount,detail FROM events WHERE kind='Trade'";
        using var r = c.ExecuteReader();
        while (r.Read())
        {
            DateTime.TryParse(r.GetString(0), CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t);
            list.Add(new LogEntry { Time = t, Kind = EventKind.Trade, Amount = r.GetInt64(1), Detail = r.IsDBNull(2) ? "" : r.GetString(2) });
        }
        return list;
    }

    /// <summary>Eindeutige erhaltene Baupläne über alle Sessions.</summary>
    public static List<string> DistinctBlueprints()
    {
        var list = new List<string>();
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var c = db.CreateCommand();
        c.CommandText = "SELECT DISTINCT detail FROM events WHERE kind='Blueprint' ORDER BY detail";
        using var r = c.ExecuteReader();
        while (r.Read()) if (!r.IsDBNull(0)) list.Add(r.GetString(0));
        return list;
    }

    /// <summary>Alle erhaltenen Bauplan-Events mit Zeitstempel über alle Sessions.</summary>
    public static List<LogEntry> AllBlueprintEvents()
    {
        var list = new List<LogEntry>();
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var c = db.CreateCommand();
        c.CommandText = "SELECT time, detail FROM events WHERE kind='Blueprint' ORDER BY time ASC";
        using var r = c.ExecuteReader();
        while (r.Read())
        {
            var tStr = r.GetString(0);
            DateTime.TryParse(tStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var t);
            var detail = r.IsDBNull(1) ? "" : r.GetString(1);
            list.Add(new LogEntry { Time = t, Kind = EventKind.Blueprint, Detail = detail });
        }
        return list;
    }

    public static int SessionCount()
    {
        using var db = new SqliteConnection(Conn);
        db.Open();
        return Convert.ToInt32(Scalar(db, "SELECT COUNT(*) FROM sessions") ?? 0);
    }

    public class Agg
    {
        public long In, Reward, Out, Purchases, Sales, Trade;
        public DateTime? Start, End;
        public int Sessions;
        public double PlaytimeSeconds;   // Summe der Session-Dauern (echte Spielzeit, nicht Kalender-Spanne)
        public int MissionsDone;
        public List<string> Ships = new();
    }

    /// <summary>Summen per SQL (kein Vollladen der Events), mit optionalem Wipe-Filter.</summary>
    public static Agg Aggregate(DateTime? since = null, bool filterMoney = true, bool filterContracts = true, bool filterFleet = false)
    {
        var a = new Agg();
        using var db = new SqliteConnection(Conn);
        db.Open();

        var sinceIso = since?.ToString("o", CultureInfo.InvariantCulture);

        using (var c = db.CreateCommand())
        {
            if (since != null && filterMoney)
            {
                c.CommandText = "SELECT kind, COALESCE(SUM(amount),0) FROM events WHERE time >= $since GROUP BY kind";
                c.Parameters.AddWithValue("$since", sinceIso);
            }
            else
            {
                c.CommandText = "SELECT kind, COALESCE(SUM(amount),0) FROM events GROUP BY kind";
            }

            using var r = c.ExecuteReader();
            while (r.Read())
            {
                var kind = r.GetString(0);
                var sum = r.GetInt64(1);
                switch (kind)
                {
                    case "TransferIn": a.In = sum; break;
                    case "MissionReward": a.Reward = sum; break;
                    case "Sale": a.Sales = sum; break;
                    case "Trade": a.Trade = sum; break;
                    case "TransferOut": a.Out += -sum; break;  // Beträge negativ -> positiv
                    case "Fine": a.Out += -sum; break;         // Bußgelder = aUEC raus
                    case "Purchase": a.Purchases = -sum; break;
                }
            }
        }

        a.Sessions = Convert.ToInt32(Scalar(db, since != null 
            ? "SELECT COUNT(*) FROM sessions WHERE end >= $since" 
            : "SELECT COUNT(*) FROM sessions", ("$since", sinceIso ?? "")) ?? 0);

        // Echte Spielzeit = Summe der Session-Dauern. In C# rechnen (julianday verträgt das
        // 7-stellige "o"-Zeitformat nicht zuverlässig).
        using (var pc = db.CreateCommand())
        {
            pc.CommandText = since != null 
                ? "SELECT start, end FROM sessions WHERE start IS NOT NULL AND end IS NOT NULL AND end >= $since"
                : "SELECT start, end FROM sessions WHERE start IS NOT NULL AND end IS NOT NULL";
            if (since != null) pc.Parameters.AddWithValue("$since", sinceIso);
            using var pr = pc.ExecuteReader();
            while (pr.Read())
            {
                if (DateTime.TryParse(pr.GetString(0), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var st) &&
                    DateTime.TryParse(pr.GetString(1), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var en) &&
                    en > st)
                {
                    var dur = (en - st).TotalSeconds;
                    // Plausibilitätsprüfung: Sessions über 36h ignorieren/kappen
                    if (dur > 0 && dur < 36 * 3600)
                    {
                        a.PlaytimeSeconds += dur;
                    }
                }
            }
        }

        a.MissionsDone = Convert.ToInt32(Scalar(db, since != null && filterContracts
            ? "SELECT COUNT(*) FROM events WHERE kind='MissionDone' AND time >= $since"
            : "SELECT COUNT(*) FROM events WHERE kind='MissionDone'", ("$since", sinceIso ?? "")) ?? 0);

        if (Scalar(db, "SELECT MIN(time) FROM events") is string mn && DateTime.TryParse(mn, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var s)) a.Start = s;
        if (Scalar(db, "SELECT MAX(time) FROM events") is string mx && DateTime.TryParse(mx, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var e)) a.End = e;

        using (var c = db.CreateCommand())
        {
            if (since != null && filterFleet)
            {
                c.CommandText = "SELECT DISTINCT ship FROM events WHERE ship IS NOT NULL AND time >= $since ORDER BY ship";
                c.Parameters.AddWithValue("$since", sinceIso);
            }
            else
            {
                c.CommandText = "SELECT DISTINCT ship FROM events WHERE ship IS NOT NULL ORDER BY ship";
            }
            using var r = c.ExecuteReader();
            while (r.Read()) a.Ships.Add(r.GetString(0));
        }
        return a;
    }

    public record DbShipStat(
        string Ship,
        int FlightCount,
        int QtCount,
        int LossCount,
        DateTime? LastTime
    );

    /// <summary>
    /// Aggregiert echte Flugstatistiken (Spawns, QT-Sprünge, Verluste, letzter Einsatz) je Schiff.
    /// </summary>
    public static List<DbShipStat> GetFleetStats(DateTime? since = null)
    {
        var list = new List<DbShipStat>();
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var c = db.CreateCommand();

        if (since.HasValue)
        {
            c.CommandText = @"
                SELECT 
                    ship,
                    COUNT(DISTINCT session) as flights,
                    COUNT(CASE WHEN kind = 'Quantum' THEN 1 END) as qts,
                    COUNT(CASE WHEN kind = 'ShipLoss' THEN 1 END) as losses,
                    MAX(time) as last_time
                FROM events 
                WHERE ship IS NOT NULL AND trim(ship) != '' AND ship != '—' AND time >= $since
                GROUP BY ship
                ORDER BY MAX(time) DESC";
            c.Parameters.AddWithValue("$since", since.Value.ToString("o", CultureInfo.InvariantCulture));
        }
        else
        {
            c.CommandText = @"
                SELECT 
                    ship,
                    COUNT(DISTINCT session) as flights,
                    COUNT(CASE WHEN kind = 'Quantum' THEN 1 END) as qts,
                    COUNT(CASE WHEN kind = 'ShipLoss' THEN 1 END) as losses,
                    MAX(time) as last_time
                FROM events 
                WHERE ship IS NOT NULL AND trim(ship) != '' AND ship != '—'
                GROUP BY ship
                ORDER BY MAX(time) DESC";
        }

        using var r = c.ExecuteReader();
        while (r.Read())
        {
            var ship = r.GetString(0);
            var flights = r.GetInt32(1);
            var qts = r.GetInt32(2);
            var losses = r.GetInt32(3);
            DateTime? lastTime = null;
            if (!r.IsDBNull(4) && DateTime.TryParse(r.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
            {
                lastTime = dt;
            }

            // Wenn keine expliziten 'Vehicle'-Events geloggt wurden, aber QT-Sprünge vorliegen, mindestens 1 Flug annehmen
            if (flights == 0) flights = Math.Max(1, qts);

            list.Add(new DbShipStat(ship, flights, qts, losses, lastTime));
        }

        return list;
    }

    /// <summary>Größte Geld-Posten per SQL.</summary>
    public static List<LogEntry> TopMoney(int n)
    {
        var list = new List<LogEntry>();
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var c = db.CreateCommand();
        c.CommandText = @"SELECT kind,amount,detail FROM events
                          WHERE kind IN ('TransferIn','TransferOut','MissionReward','Purchase','Sale','Trade','Fine')
                          ORDER BY ABS(amount) DESC LIMIT $n";
        c.Parameters.AddWithValue("$n", n);
        using var r = c.ExecuteReader();
        while (r.Read())
        {
            Enum.TryParse<EventKind>(r.GetString(0), out var kind);
            list.Add(new LogEntry { Kind = kind, Amount = r.GetInt64(1), Detail = r.IsDBNull(2) ? "" : r.GetString(2) });
        }
        return list;
    }

    /// <summary>Neueste N Events (für die Tabelle), chronologisch.</summary>
    public static List<LogEntry> RecentEvents(int n)
    {
        var list = new List<LogEntry>();
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var c = db.CreateCommand();
        c.CommandText = "SELECT time,kind,amount,detail,ship FROM events ORDER BY time DESC LIMIT $n";
        c.Parameters.AddWithValue("$n", n);
        using var r = c.ExecuteReader();
        while (r.Read())
        {
            DateTime.TryParse(r.GetString(0), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t);
            Enum.TryParse<EventKind>(r.GetString(1), out var kind);
            list.Add(new LogEntry { Time = t, Kind = kind, Amount = r.GetInt64(2), Detail = r.IsDBNull(3) ? "" : r.GetString(3), Ship = r.IsDBNull(4) ? null : r.GetString(4) });
        }
        list.Reverse();
        return list;
    }

    // ---- Helfer ----
    static IEnumerable<string> ReadShared(string file)
    {
        using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var sr = new StreamReader(fs);
        string? l;
        while ((l = sr.ReadLine()) != null) yield return l;
    }

    static void Exec(SqliteConnection db, string sql)
    {
        using var c = db.CreateCommand();
        c.CommandText = sql;
        c.ExecuteNonQuery();
    }

    static object? Scalar(SqliteConnection db, string sql, params (string, object)[] ps)
    {
        using var c = db.CreateCommand();
        c.CommandText = sql;
        foreach (var (k, v) in ps) c.Parameters.AddWithValue(k, v);
        return c.ExecuteScalar();
    }

    static string? GetMeta(SqliteConnection db, string key) =>
        Scalar(db, "SELECT value FROM meta WHERE key=$k", ("$k", key)) as string;

    static void SetMeta(SqliteConnection db, string key, string value)
    {
        using var c = db.CreateCommand();
        c.CommandText = "INSERT OR REPLACE INTO meta(key,value) VALUES($k,$v)";
        c.Parameters.AddWithValue("$k", key);
        c.Parameters.AddWithValue("$v", value);
        c.ExecuteNonQuery();
    }

    public static void SaveContract(Models.ContractDetails contract)
    {
        EnsureInitialized();
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO contracts (id, title, reward, contracted_by, scanned_at, status)
            VALUES ($id, $title, $reward, $org, $time, 'Active');";
        cmd.Parameters.AddWithValue("$id", $"{contract.Title.Trim()}:{contract.Reward}");
        cmd.Parameters.AddWithValue("$title", contract.Title);
        cmd.Parameters.AddWithValue("$reward", contract.Reward);
        cmd.Parameters.AddWithValue("$org", contract.ContractedBy ?? "");
        cmd.Parameters.AddWithValue("$time", contract.ScannedAt.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public static List<Models.ContractDetails> GetActiveContracts()
    {
        EnsureInitialized();
        var list = new List<Models.ContractDetails>();
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT title, reward, contracted_by, scanned_at FROM contracts WHERE status='Active' ORDER BY scanned_at DESC;";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            DateTime.TryParse(r.GetString(3), out var dt);
            list.Add(new Models.ContractDetails
            {
                Title = r.GetString(0),
                Reward = r.GetInt32(1),
                ContractedBy = r.GetString(2),
                ScannedAt = dt != default ? dt : DateTime.UtcNow
            });
        }
        return list;
    }

    public static void ClearActiveContracts()
    {
        EnsureInitialized();
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM contracts;";
        cmd.ExecuteNonQuery();
    }

    public static void RemoveContract(string title, int reward)
    {
        EnsureInitialized();
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM contracts WHERE title=$t OR (reward=$r AND $r > 0 AND instr(lower($t), lower(title)) > 0) OR instr(lower(title), lower($t)) > 0;";
        cmd.Parameters.AddWithValue("$t", title);
        cmd.Parameters.AddWithValue("$r", reward);
        cmd.ExecuteNonQuery();
    }

    public static List<UserPoi> GetUserPois(string? system = null)
    {
        EnsureInitialized();
        var list = new List<UserPoi>();
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var cmd = db.CreateCommand();
        if (!string.IsNullOrEmpty(system))
        {
            cmd.CommandText = "SELECT id, system, body, name, notes, category, color, created_at FROM user_pois WHERE system=$sys ORDER BY id DESC;";
            cmd.Parameters.AddWithValue("$sys", system);
        }
        else
        {
            cmd.CommandText = "SELECT id, system, body, name, notes, category, color, created_at FROM user_pois ORDER BY id DESC;";
        }
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            DateTime.TryParse(r.GetString(7), out var dt);
            list.Add(new UserPoi
            {
                Id = r.GetInt32(0),
                System = r.GetString(1),
                Body = r.GetString(2),
                Name = r.GetString(3),
                Notes = r.GetString(4),
                Category = r.GetString(5),
                Color = r.GetString(6),
                CreatedAt = dt != default ? dt : DateTime.UtcNow
            });
        }
        return list;
    }

    public static int SaveUserPoi(UserPoi poi)
    {
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var cmd = db.CreateCommand();
        if (poi.Id > 0)
        {
            cmd.CommandText = @"
                UPDATE user_pois SET system=$sys, body=$body, name=$name, notes=$notes, category=$cat, color=$col
                WHERE id=$id;";
            cmd.Parameters.AddWithValue("$id", poi.Id);
        }
        else
        {
            cmd.CommandText = @"
                INSERT INTO user_pois (system, body, name, notes, category, color, created_at)
                VALUES ($sys, $body, $name, $notes, $cat, $col, $created);
                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$created", poi.CreatedAt.ToString("o"));
        }
        cmd.Parameters.AddWithValue("$sys", poi.System);
        cmd.Parameters.AddWithValue("$body", poi.Body);
        cmd.Parameters.AddWithValue("$name", poi.Name);
        cmd.Parameters.AddWithValue("$notes", poi.Notes);
        cmd.Parameters.AddWithValue("$cat", poi.Category);
        cmd.Parameters.AddWithValue("$col", poi.Color);

        if (poi.Id > 0)
        {
            cmd.ExecuteNonQuery();
            return poi.Id;
        }
        else
        {
            var newId = Convert.ToInt32(cmd.ExecuteScalar());
            poi.Id = newId;
            return newId;
        }
    }

    public static void DeleteUserPoi(int id)
    {
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM user_pois WHERE id=$id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    #region Faction Reputation Tracking

    public static Dictionary<string, (int Xp, int Missions, DateTime LastUpdated)> LoadFactionReputations()
    {
        EnsureInitialized();
        var result = new Dictionary<string, (int Xp, int Missions, DateTime LastUpdated)>(StringComparer.OrdinalIgnoreCase);
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT faction_id, xp, completed_missions, last_updated FROM reputation;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var fid = reader.GetString(0);
            var xp = reader.GetInt32(1);
            var missions = reader.GetInt32(2);
            var lastStr = reader.GetString(3);
            DateTime.TryParse(lastStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var last);
            result[fid] = (xp, missions, last);
        }
        return result;
    }

    public static void AddFactionReputationXp(string factionId, int xpDelta, DateTime time)
    {
        if (string.IsNullOrWhiteSpace(factionId) || xpDelta <= 0) return;
        EnsureInitialized();
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO reputation (faction_id, xp, completed_missions, last_updated)
            VALUES ($fid, $xp, 1, $last)
            ON CONFLICT(faction_id) DO UPDATE SET
                xp = xp + $xp,
                completed_missions = completed_missions + 1,
                last_updated = $last;";
        cmd.Parameters.AddWithValue("$fid", factionId);
        cmd.Parameters.AddWithValue("$xp", xpDelta);
        cmd.Parameters.AddWithValue("$last", time.ToString("o"));
        cmd.ExecuteNonQuery();
    }

    public static void ResetFactionReputations()
    {
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM reputation;";
        cmd.ExecuteNonQuery();
    }

    #endregion

    #region Fleet Custom User Ships (Pledge, Insurance, Notes)

    public record DbFleetCustomData(bool InHangar, bool IsPledge, int PledgeUsd, string Insurance, string Acquisition, string Notes);

    public static Dictionary<string, DbFleetCustomData> GetAllFleetCustomData()
    {
        EnsureInitialized();
        var dict = new Dictionary<string, DbFleetCustomData>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "SELECT name, COALESCE(in_hangar, 1), is_pledge, pledge_usd, insurance, acquisition, notes FROM fleet_user_ships;";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var name = r.GetString(0);
                var inHangar = r.GetInt32(1) == 1;
                var isPledge = r.GetInt32(2) == 1;
                var pledgeUsd = r.GetInt32(3);
                var insurance = r.GetString(4);
                var acq = r.GetString(5);
                var notes = r.GetString(6);
                dict[name] = new DbFleetCustomData(inHangar, isPledge, pledgeUsd, insurance, acq, notes);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("GetAllFleetCustomData", ex);
        }
        return dict;
    }

    public static void SaveFleetShipCustomData(string name, bool inHangar, bool isPledge, int pledgeUsd, string insurance, string acquisition, string notes)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        EnsureInitialized();
        try
        {
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO fleet_user_ships(name, in_hangar, is_pledge, pledge_usd, insurance, acquisition, notes)
                VALUES($n, $ih, $p, $u, $i, $a, $nt)
                ON CONFLICT(name) DO UPDATE SET
                    in_hangar = excluded.in_hangar,
                    is_pledge = excluded.is_pledge,
                    pledge_usd = excluded.pledge_usd,
                    insurance = excluded.insurance,
                    acquisition = excluded.acquisition,
                    notes = excluded.notes;";
            cmd.Parameters.AddWithValue("$n", name);
            cmd.Parameters.AddWithValue("$ih", inHangar ? 1 : 0);
            cmd.Parameters.AddWithValue("$p", isPledge ? 1 : 0);
            cmd.Parameters.AddWithValue("$u", pledgeUsd);
            cmd.Parameters.AddWithValue("$i", insurance);
            cmd.Parameters.AddWithValue("$a", acquisition);
            cmd.Parameters.AddWithValue("$nt", notes);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Logger.Error("SaveFleetShipCustomData", ex);
        }
    }

    #endregion
}
