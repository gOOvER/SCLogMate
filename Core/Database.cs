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
    public const int CurrentSchemaVersion = 2;  // Erhöhen bei Tabellen- oder Spalten-Änderungen
    public const int CurrentParserVersion = 15; // Erhöhen, wenn der LogParser neue Felder/Events liefert

    static string DbPath => Path.Combine(Settings.Dir, "sessions.db");

    static string Conn => $"Data Source={DbPath}";

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
            Logger.Log($"DB: Parser-Version auf v{CurrentParserVersion} aktualisiert -> Cache für Re-Indexierung geleert.");
        }
    }

    /// <summary>Parst und speichert alle Logs, die noch nicht in der DB sind. Liefert Anzahl neuer.</summary>
    public static int IndexNew(IEnumerable<string> logFiles)
    {
        using var db = new SqliteConnection(Conn);
        db.Open();
        int added = 0;

        foreach (var file in logFiles)
        {
            var name = Path.GetFileName(file);
            if (Scalar(db, "SELECT 1 FROM sessions WHERE name=$n", ("$n", name)) != null) continue;

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

    /// <summary>Summen per SQL (kein Vollladen der Events).</summary>
    public static Agg Aggregate()
    {
        var a = new Agg();
        using var db = new SqliteConnection(Conn);
        db.Open();

        using (var c = db.CreateCommand())
        {
            c.CommandText = "SELECT kind, COALESCE(SUM(amount),0) FROM events GROUP BY kind";
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

        a.Sessions = Convert.ToInt32(Scalar(db, "SELECT COUNT(*) FROM sessions") ?? 0);
        // Echte Spielzeit = Summe der Session-Dauern. In C# rechnen (julianday verträgt das
        // 7-stellige "o"-Zeitformat nicht zuverlässig).
        using (var pc = db.CreateCommand())
        {
            pc.CommandText = "SELECT start, end FROM sessions WHERE start IS NOT NULL AND end IS NOT NULL";
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
        a.MissionsDone = Convert.ToInt32(Scalar(db, "SELECT COUNT(*) FROM events WHERE kind='MissionDone'") ?? 0);
        if (Scalar(db, "SELECT MIN(time) FROM events") is string mn && DateTime.TryParse(mn, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var s)) a.Start = s;
        if (Scalar(db, "SELECT MAX(time) FROM events") is string mx && DateTime.TryParse(mx, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var e)) a.End = e;

        using (var c = db.CreateCommand())
        {
            c.CommandText = "SELECT DISTINCT ship FROM events WHERE ship IS NOT NULL ORDER BY ship";
            using var r = c.ExecuteReader();
            while (r.Read()) a.Ships.Add(r.GetString(0));
        }
        return a;
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
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM contracts;";
        cmd.ExecuteNonQuery();
    }

    public static void RemoveContract(string title, int reward)
    {
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "DELETE FROM contracts WHERE title=$t AND reward=$r;";
        cmd.Parameters.AddWithValue("$t", title);
        cmd.Parameters.AddWithValue("$r", reward);
        cmd.ExecuteNonQuery();
    }
}
