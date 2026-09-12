using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using SCLogMate.Models;

namespace SCLogMate.Core;

/// <summary>
/// Lokale SQLite-Datenbank als nachbaubarer Cache/Index der fertigen Sessions.
/// Quelle der Wahrheit bleiben die archivierten Roh-Logs (LogArchive).
/// - Schema-Version: PRAGMA user_version (Tabellenstruktur)
/// - Parser-Version: bei Erhöhung wird die DB aus dem Archiv NEU aufgebaut.
/// </summary>
public static class Database
{
    public const int CurrentSchemaVersion = 21; // Erhöhen bei Tabellen- oder Spalten-Änderungen
    public const int CurrentParserVersion = 34; // Erhöhen, wenn der LogParser neue Felder/Events liefert

    public static bool WasParserResetRequired { get; set; }
    public static bool WasMigrationApplied { get; set; }
    public static string? LastMigrationReason { get; set; }

    public static string DatabaseFilePath => DbPath;

    static string DbPath => Path.Combine(Settings.Dir, "sessions.db");

    static string Conn => $"Data Source={DbPath};Default Timeout=60;";

    private static readonly System.Threading.Lock _initLock = new();
    private static readonly System.Threading.Lock _writeLock = new();
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
        lock (_writeLock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
            using var db = new SqliteConnection(Conn);
            db.Open();

            Exec(db, @"PRAGMA journal_mode = WAL;
                       PRAGMA synchronous = NORMAL;
                       PRAGMA busy_timeout = 60000;");

            // 1. Schema-Migrationen anwenden (PRAGMA user_version)
            ApplySchemaMigrations(db);

            // 2. Parser-Version prüfen -> bei Änderung Cache leeren & neu indexieren
            CheckParserVersion(db);

            // 3. Sicherstellen, dass Indizes immer existieren (z.B. falls Re-Scan unterbrochen wurde)
            Exec(db, @"CREATE INDEX IF NOT EXISTS ix_events_session ON events(session);
                       CREATE INDEX IF NOT EXISTS ix_events_kind ON events(kind);
                       CREATE INDEX IF NOT EXISTS ix_events_time ON events(time);
                       CREATE INDEX IF NOT EXISTS ix_events_session_kind ON events(session, kind);
                       CREATE INDEX IF NOT EXISTS ix_events_kind_time ON events(kind, time);");
            _isInitialized = true;
        }
    }

    /// <summary>
    /// Führt inkrementelle Schema-Upgrades (Tabellen, Spalten, Indizes) strukturiert aus.
    /// </summary>
    private static void ApplySchemaMigrations(SqliteConnection db)
    {
        var versionObj = Scalar(db, "PRAGMA user_version;");
        int dbSchemaVersion = Convert.ToInt32(versionObj ?? 0);

        if (dbSchemaVersion < CurrentSchemaVersion)
        {
            WasMigrationApplied = true;
            LastMigrationReason = $"Datenbank-Schema Upgrade von v{dbSchemaVersion} auf v{CurrentSchemaVersion}";
        }

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

        if (dbSchemaVersion < 7)
        {
            Exec(db, "ALTER TABLE sessions ADD COLUMN fingerprint TEXT;");
            Exec(db, "PRAGMA user_version = 7;");
            dbSchemaVersion = 7;
            Logger.Log("DB Schema: Migration auf v7 (Session-Fingerprint) angewendet.");
        }

        if (dbSchemaVersion < 8)
        {
            Exec(db, @"
                CREATE INDEX IF NOT EXISTS ix_events_session_kind ON events(session, kind);
                CREATE INDEX IF NOT EXISTS ix_events_kind_time ON events(kind, time);
            ");
            Exec(db, "PRAGMA user_version = 8;");
            dbSchemaVersion = 8;
            Logger.Log("DB Schema: Migration auf v8 (Composite-Indizes für session/kind und kind/time) angewendet.");
        }

        if (dbSchemaVersion < 9)
        {
            Exec(db, @"
                DELETE FROM events WHERE rowid NOT IN (
                    SELECT MIN(rowid) FROM events GROUP BY session, time, kind, amount, detail
                );
                DELETE FROM sessions WHERE rowid NOT IN (
                    SELECT MIN(rowid) FROM sessions GROUP BY COALESCE(start, name), COALESCE(end, name)
                );
                DELETE FROM events WHERE session NOT IN (
                    SELECT name FROM sessions
                );
            ");
            Exec(db, "PRAGMA user_version = 9;");
            dbSchemaVersion = 9;
            Logger.Log("DB Schema: Migration auf v9 (Bereinigung doppelter Events & Sessions) erfolgreich angewendet.");
        }

        if (dbSchemaVersion < 10)
        {
            Exec(db, @"
                UPDATE events 
                SET kind = 'SessionChange' 
                WHERE kind = 'Location' 
                  AND (detail LIKE '%gespawnt%' OR detail LIKE '%Spawned%' OR detail LIKE '%login%');
            ");
            Exec(db, "PRAGMA user_version = 10;");
            dbSchemaVersion = 10;
            Logger.Log("DB Schema: Migration auf v10 (Bereinigung von Spawns/Logins aus Locations) erfolgreich angewendet.");
        }

        if (dbSchemaVersion < 11)
        {
            Exec(db, @"
                UPDATE contracts 
                SET contracted_by = '' 
                WHERE contracted_by = 'mobiGlas' OR contracted_by LIKE '%mobiGlas%';
            ");
            Exec(db, "PRAGMA user_version = 11;");
            dbSchemaVersion = 11;
            Logger.Log("DB Schema: Migration auf v11 (Bereinigung von 'mobiGlas' als Auftraggeber in contracts) erfolgreich angewendet.");
        }

        if (dbSchemaVersion < 12)
        {
            Exec(db, @"
                UPDATE contracts 
                SET contracted_by = 'Recco Battaglia' 
                WHERE (title LIKE '%Moraine%' OR title LIKE '%Battaglia%') 
                  AND (contracted_by = '' OR contracted_by IS NULL OR contracted_by = 'Unbekannt' OR contracted_by = 'Battaglia');
            ");
            Exec(db, "PRAGMA user_version = 12;");
            dbSchemaVersion = 12;
            Logger.Log("DB Schema: Migration auf v12 (Bereinigung von 'Recco Battaglia' Aufträgen in contracts) erfolgreich angewendet.");
        }

        if (dbSchemaVersion < 13)
        {
            Exec(db, @"
                UPDATE fleet_user_ships 
                SET in_hangar = 1 
                WHERE is_pledge = 1 OR acquisition = 'Pledge Store' OR acquisition = 'In-Game (aUEC)';
            ");
            Exec(db, "PRAGMA user_version = 13;");
            dbSchemaVersion = 13;
            Logger.Log("DB Schema: Migration auf v13 (Pledged und In-Game Schiffe automatisch im Hangar aktiv) erfolgreich angewendet.");
        }

        if (dbSchemaVersion < 14)
        {
            try
            {
                Exec(db, @"
                    UPDATE events 
                    SET ship = REPLACE(REPLACE(REPLACE(ship, ' Salvage', ''), ' Teach', ''), ' GS', '') 
                    WHERE ship LIKE '% Salvage%' OR ship LIKE '% Teach%' OR ship LIKE '% GS%';
                ");
                Exec(db, @"
                    UPDATE fleet_user_ships 
                    SET name = REPLACE(REPLACE(REPLACE(name, ' Salvage', ''), ' Teach', ''), ' GS', '') 
                    WHERE name LIKE '% Salvage%' OR name LIKE '% Teach%' OR name LIKE '% GS%';
                ");
            }
            catch { }
            Exec(db, "PRAGMA user_version = 14;");
            dbSchemaVersion = 14;
            Logger.Log("DB Schema: Migration auf v14 (Umfassende Bereinigung von Spawn-Archetypen wie Salvage, Teach, GS) erfolgreich angewendet.");
        }

        if (dbSchemaVersion < 15)
        {
            try
            {
                Exec(db, @"
                    UPDATE events 
                    SET ship = 'M80 · Origin' 
                    WHERE ship = 'm80 · Origin' OR ship = 'm80' OR ship = 'Origin M80' OR ship = 'Origin M80 · Origin';
                ");

                Exec(db, @"
                    INSERT INTO fleet_user_ships (name, in_hangar, is_pledge, pledge_usd, insurance, acquisition, notes)
                    SELECT 'M80 · Origin', in_hangar, is_pledge, pledge_usd, insurance, acquisition, notes
                    FROM fleet_user_ships 
                    WHERE name IN ('m80 · Origin', 'Origin M80 · Origin', 'm80', 'Origin M80')
                    ORDER BY is_pledge DESC, in_hangar DESC, pledge_usd DESC
                    LIMIT 1
                    ON CONFLICT(name) DO UPDATE SET
                        in_hangar = excluded.in_hangar,
                        is_pledge = excluded.is_pledge,
                        pledge_usd = CASE WHEN excluded.pledge_usd > 0 THEN excluded.pledge_usd ELSE fleet_user_ships.pledge_usd END,
                        insurance = CASE WHEN excluded.insurance != '' AND excluded.insurance != 'LTI (Lifetime)' THEN excluded.insurance ELSE fleet_user_ships.insurance END,
                        acquisition = excluded.acquisition;
                ");

                Exec(db, @"
                    DELETE FROM fleet_user_ships 
                    WHERE name IN ('m80 · Origin', 'Origin M80 · Origin', 'm80', 'Origin M80');
                ");
            }
            catch (Exception ex)
            {
                Logger.Error("Migration v15 (M80 Harmonisierung)", ex);
            }
            Exec(db, "PRAGMA user_version = 15;");
            dbSchemaVersion = 15;
            Logger.Log("DB Schema: Migration auf v15 (M80 · Origin Harmonisierung und Deduplizierung) erfolgreich angewendet.");
        }

        if (dbSchemaVersion < 16)
        {
            try
            {
                Exec(db, @"
                    CREATE TABLE IF NOT EXISTS warehouse_items (
                        location TEXT NOT NULL,
                        location_code TEXT NOT NULL,
                        system TEXT NOT NULL,
                        parent_body TEXT NOT NULL,
                        item_class TEXT NOT NULL,
                        item_name TEXT NOT NULL,
                        category TEXT NOT NULL,
                        quantity INTEGER NOT NULL,
                        last_updated TEXT NOT NULL,
                        PRIMARY KEY (location, item_class)
                    );
                    CREATE INDEX IF NOT EXISTS ix_warehouse_location ON warehouse_items(location);
                    CREATE INDEX IF NOT EXISTS ix_warehouse_category ON warehouse_items(category);
                ");
            }
            catch (Exception ex)
            {
                Logger.Error("Migration v16 (warehouse_items)", ex);
            }
            Exec(db, "PRAGMA user_version = 16;");
            dbSchemaVersion = 16;
            Logger.Log("DB Schema: Migration auf v16 (warehouse_items Tabelle & Indizes) erfolgreich angewendet.");
        }

        if (dbSchemaVersion < 17)
        {
            try
            {
                Exec(db, @"
                    CREATE TABLE IF NOT EXISTS wiki_items_cache (
                        class_name TEXT PRIMARY KEY,
                        name TEXT NOT NULL,
                        category TEXT,
                        manufacturer TEXT,
                        description_de TEXT,
                        description_en TEXT,
                        thumbnail_url TEXT,
                        image_url TEXT,
                        web_url TEXT,
                        updated_at TEXT NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS ix_wiki_items_name ON wiki_items_cache(name);
                ");
            }
            catch (Exception ex)
            {
                Logger.Error("Migration v17 (wiki_items_cache)", ex);
            }
            Exec(db, "PRAGMA user_version = 17;");
            dbSchemaVersion = 17;
            Logger.Log("DB Schema: Migration auf v17 (wiki_items_cache Tabelle & Indizes) erfolgreich angewendet.");
        }

        if (dbSchemaVersion < 18)
        {
            try { Exec(db, "ALTER TABLE sessions ADD COLUMN pilot TEXT;"); } catch { }
            try { Exec(db, "ALTER TABLE sessions ADD COLUMN shard TEXT;"); } catch { }
            try { Exec(db, "ALTER TABLE sessions ADD COLUMN version TEXT;"); } catch { }
            Exec(db, "PRAGMA user_version = 18;");
            dbSchemaVersion = 18;
            Logger.Log("DB Schema: Migration auf v18 (sessions pilot, shard, version Spalten) erfolgreich angewendet.");
        }

        if (dbSchemaVersion < 19)
        {
            try
            {
                Exec(db, @"
                    CREATE TABLE IF NOT EXISTS pilot_profiles (
                        handle TEXT PRIMARY KEY,
                        citizen_record TEXT,
                        title TEXT,
                        avatar_url TEXT,
                        enlisted TEXT,
                        fluency TEXT,
                        org_name TEXT,
                        org_sid TEXT,
                        org_rank TEXT,
                        org_logo_url TEXT,
                        website TEXT,
                        profile_url TEXT,
                        bio TEXT,
                        updated_at TEXT NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS ix_pilot_profiles_citizen_record ON pilot_profiles(citizen_record);
                ");
            }
            catch (Exception ex)
            {
                Logger.Error("Migration v19 (pilot_profiles)", ex);
            }
            Exec(db, "PRAGMA user_version = 19;");
            dbSchemaVersion = 19;
            Logger.Log("DB Schema: Migration auf v19 (pilot_profiles Tabelle & Indizes) erfolgreich angewendet.");
        }

        if (dbSchemaVersion < 20)
        {
            try
            {
                Exec(db, @"
                    CREATE TABLE IF NOT EXISTS wiki_vehicles_cache (
                        name TEXT PRIMARY KEY,
                        manufacturer TEXT,
                        role TEXT,
                        type TEXT,
                        focus TEXT,
                        size TEXT,
                        crew_min INTEGER,
                        crew_max INTEGER,
                        cargo_scu REAL,
                        quantum_fuel REAL,
                        length REAL,
                        beam REAL,
                        height REAL,
                        mass REAL,
                        msrp REAL,
                        production_status TEXT,
                        description_de TEXT,
                        description_en TEXT,
                        thumbnail_url TEXT,
                        image_url TEXT,
                        web_url TEXT,
                        pledge_url TEXT,
                        specs_json TEXT,
                        stores_json TEXT,
                        updated_at TEXT NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS ix_wiki_vehicles_manufacturer ON wiki_vehicles_cache(manufacturer);
                    CREATE INDEX IF NOT EXISTS ix_wiki_vehicles_role ON wiki_vehicles_cache(role);
                ");

                try { Exec(db, "ALTER TABLE wiki_items_cache ADD COLUMN specs_json TEXT;"); } catch { }
                try { Exec(db, "ALTER TABLE wiki_items_cache ADD COLUMN item_grade TEXT;"); } catch { }
                try { Exec(db, "ALTER TABLE wiki_items_cache ADD COLUMN item_type TEXT;"); } catch { }
            }
            catch (Exception ex)
            {
                Logger.Error("Migration v20 (wiki_vehicles_cache)", ex);
            }
            Exec(db, "PRAGMA user_version = 20;");
            dbSchemaVersion = 20;
            Logger.Log("DB Schema: Migration auf v20 (wiki_vehicles_cache & erweiterte Item-Attribute) erfolgreich angewendet.");
        }

        if (dbSchemaVersion < 21)
        {
            try
            {
                Exec(db, @"
                    CREATE TABLE IF NOT EXISTS chat_messages (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        timestamp TEXT NOT NULL,
                        session_id TEXT,
                        channel TEXT NOT NULL,
                        sender TEXT NOT NULL,
                        recipient TEXT,
                        message TEXT NOT NULL,
                        raw_ocr TEXT,
                        is_flagged INTEGER DEFAULT 0,
                        created_at TEXT NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS ix_chat_timestamp ON chat_messages(timestamp);
                    CREATE INDEX IF NOT EXISTS ix_chat_sender ON chat_messages(sender);
                    CREATE INDEX IF NOT EXISTS ix_chat_channel ON chat_messages(channel);
                    CREATE INDEX IF NOT EXISTS ix_chat_session ON chat_messages(session_id);
                ");
            }
            catch (Exception ex)
            {
                Logger.Error("Migration v21 (chat_messages)", ex);
            }
            Exec(db, "PRAGMA user_version = 21;");
            dbSchemaVersion = 21;
            Logger.Log("DB Schema: Migration auf v21 (chat_messages Tabelle & Indizes) erfolgreich angewendet.");
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
            Exec(db, "DELETE FROM events; DELETE FROM sessions; DELETE FROM warehouse_items;");
            SetMeta(db, "parserVersion", CurrentParserVersion.ToString(CultureInfo.InvariantCulture));
            WasParserResetRequired = true;
            LastMigrationReason = $"Parser-Update auf v{CurrentParserVersion} (Vollständige Neu-Indexierung aller Logs)";
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
        var uniqueFiles = logFiles
            .GroupBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First());
        foreach (var file in uniqueFiles)
        {
            var name = Path.GetFileName(file);
            var fingerprint = GetFileFingerprint(file);
            if (Scalar(db, "SELECT 1 FROM sessions WHERE name=$n AND fingerprint=$f LIMIT 1;", ("$n", name), ("$f", fingerprint)) == null)
            {
                unindexed++;
            }
        }
        return unindexed;
    }

    /// <summary>Parst und speichert alle Logs, die noch nicht in der DB sind. Liefert Anzahl neuer.</summary>
    public static int IndexNew(IEnumerable<string> logFiles, Action<int, int, string>? onProgress = null)
    {
        lock (_writeLock)
        {
            using var db = new SqliteConnection(Conn);
            db.Open();

            Exec(db, @"PRAGMA synchronous = OFF;
                       PRAGMA journal_mode = WAL;
                       PRAGMA cache_size = -64000;
                       PRAGMA temp_store = MEMORY;
                       PRAGMA busy_timeout = 60000;");

            int added = 0;
            var filesList = logFiles
                .GroupBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            // 1. Zuerst exakt ermitteln, welche Logs noch fehlen
            var pending = new List<(string file, string name, string fingerprint)>();
            foreach (var file in filesList)
            {
                var name = Path.GetFileName(file);
                var fingerprint = GetFileFingerprint(file);
                if (Scalar(db, "SELECT 1 FROM sessions WHERE name=$n AND fingerprint=$f LIMIT 1;", ("$n", name), ("$f", fingerprint)) == null)
                {
                    pending.Add((file, name, fingerprint));
                }
            }

            int total = pending.Count;
            if (total == 0)
            {
                Exec(db, "PRAGMA synchronous = NORMAL;");
                return 0;
            }

            // 2. Nur die tatsächlich fehlenden Logs indexieren
            for (int i = 0; i < total; i++)
            {
                var (file, name, fingerprint) = pending[i];
                onProgress?.Invoke(i + 1, total, name);

                try
                {
                    var parser = new LogParser();
                    DateTime? first = null, last = null;
                    using var tx = db.BeginTransaction();

                    using (var del = db.CreateCommand())
                    {
                        del.Transaction = tx;
                        del.CommandText = "DELETE FROM events WHERE session=$n; DELETE FROM sessions WHERE name=$n;";
                        del.Parameters.AddWithValue("$n", name);
                        del.ExecuteNonQuery();
                    }

                    using (var cmd = db.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "INSERT INTO events(session,time,kind,amount,detail,ship) VALUES($s,$t,$k,$a,$d,$sh)";
                        var ps = cmd.Parameters.Add("$s", SqliteType.Text); ps.Value = name;
                        var pt = cmd.Parameters.Add("$t", SqliteType.Text);
                        var pk = cmd.Parameters.Add("$k", SqliteType.Text);
                        var pa = cmd.Parameters.Add("$a", SqliteType.Integer);
                        var pd = cmd.Parameters.Add("$d", SqliteType.Text);
                        var psh = cmd.Parameters.Add("$sh", SqliteType.Text);

                        foreach (var line in LogEntryReader.ReadEntries(ReadShared(file)))
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
                    }

                    foreach (var wm in parser.WarehouseMovements)
                    {
                        RecordWarehouseMovementInternal(db, tx, wm.Time, wm.Location, wm.LocationCode, wm.System, wm.ParentBody, wm.ItemClass, wm.ItemName, wm.Category, wm.Delta);
                    }

                    using (var s = db.CreateCommand())
                    {
                        s.Transaction = tx;
                        s.CommandText = "INSERT OR REPLACE INTO sessions(name,start,end,fingerprint,pilot,shard,version) VALUES($n,$st,$en,$f,$pi,$sh,$v)";
                        s.Parameters.AddWithValue("$n", name);
                        s.Parameters.AddWithValue("$st", (object?)first?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
                        s.Parameters.AddWithValue("$en", (object?)last?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
                        s.Parameters.AddWithValue("$f", fingerprint);
                        s.Parameters.AddWithValue("$pi", (object?)parser.Meta.GetValueOrDefault("character") ?? DBNull.Value);
                        s.Parameters.AddWithValue("$sh", (object?)parser.Meta.GetValueOrDefault("shard") ?? DBNull.Value);
                        s.Parameters.AddWithValue("$v", (object?)parser.Meta.GetValueOrDefault("version") ?? DBNull.Value);
                        s.ExecuteNonQuery();
                    }
                    tx.Commit();
                    added++;
                }
                catch (Exception ex) { Logger.Error("Index " + name, ex); }
            }

            Exec(db, @"PRAGMA synchronous = NORMAL;
                       PRAGMA wal_checkpoint(PASSIVE);");
            return added;
        }
    }

    /// <summary>Leert die Datenbank vollständig (Events + Sessions).</summary>
    public static void ClearAll()
    {
        lock (_writeLock)
        {
            using var db = new SqliteConnection(Conn);
            db.Open();
            Exec(db, "DELETE FROM events; DELETE FROM sessions; DELETE FROM warehouse_items;");
            Exec(db, "PRAGMA wal_checkpoint(PASSIVE);");
            Logger.Log("DB: Alle Events, Sessions und Lagerbestände vollständig zurückgesetzt.");
        }
    }

    /// <summary>Führt eine vollständige Neu-Indexierung aller Logs durch (Re-Scan).</summary>
    public static (int indexedSessions, int totalEvents) RescanAll(IEnumerable<string> logFiles, Action<int, int, string>? onProgress = null)
    {
        lock (_writeLock)
        {
            ClearAll();
            var files = logFiles
                .GroupBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
            int totalFiles = files.Count;
            int sessionCount = 0;
            int eventCount = 0;

            using var db = new SqliteConnection(Conn);
            db.Open();

            // High-Speed Konfiguration für Bulk-Import
            Exec(db, @"PRAGMA synchronous = OFF;
                       PRAGMA journal_mode = WAL;
                       PRAGMA cache_size = -64000;
                       PRAGMA temp_store = MEMORY;
                       PRAGMA busy_timeout = 60000;");

            // Alle Indizes temporär entfernen für maximale sequentielle Schreibgeschwindigkeit
            Exec(db, @"DROP INDEX IF EXISTS ix_events_session;
                       DROP INDEX IF EXISTS ix_events_kind;
                       DROP INDEX IF EXISTS ix_events_time;
                       DROP INDEX IF EXISTS ix_events_session_kind;
                       DROP INDEX IF EXISTS ix_events_kind_time;");

            try
            {
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
                        foreach (var line in LogEntryReader.ReadEntries(ReadShared(file)))
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

                        foreach (var wm in parser.WarehouseMovements)
                        {
                            RecordWarehouseMovementInternal(db, tx, wm.Time, wm.Location, wm.LocationCode, wm.System, wm.ParentBody, wm.ItemClass, wm.ItemName, wm.Category, wm.Delta);
                        }

                        using (var s = db.CreateCommand())
                        {
                            s.Transaction = tx;
                            s.CommandText = "INSERT OR REPLACE INTO sessions(name,start,end,fingerprint,pilot,shard,version) VALUES($n,$st,$en,$f,$pi,$sh,$v)";
                            s.Parameters.AddWithValue("$n", name);
                            s.Parameters.AddWithValue("$st", (object?)first?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
                            s.Parameters.AddWithValue("$en", (object?)last?.ToString("o", CultureInfo.InvariantCulture) ?? DBNull.Value);
                            s.Parameters.AddWithValue("$f", GetFileFingerprint(file));
                            s.Parameters.AddWithValue("$pi", (object?)parser.Meta.GetValueOrDefault("character") ?? DBNull.Value);
                            s.Parameters.AddWithValue("$sh", (object?)parser.Meta.GetValueOrDefault("shard") ?? DBNull.Value);
                            s.Parameters.AddWithValue("$v", (object?)parser.Meta.GetValueOrDefault("version") ?? DBNull.Value);
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
            }
            finally
            {
                // Indizes neu aufbauen & Normalzustand wiederherstellen
                Exec(db, @"CREATE INDEX IF NOT EXISTS ix_events_session ON events(session);
                           CREATE INDEX IF NOT EXISTS ix_events_kind ON events(kind);
                           CREATE INDEX IF NOT EXISTS ix_events_time ON events(time);
                           CREATE INDEX IF NOT EXISTS ix_events_session_kind ON events(session, kind);
                           CREATE INDEX IF NOT EXISTS ix_events_kind_time ON events(kind, time);
                           PRAGMA synchronous = NORMAL;
                           PRAGMA wal_checkpoint(PASSIVE);");
            }

            Logger.Log($"DB: Re-Scan beendet: {sessionCount} Sessions, {eventCount} Events.");
            return (sessionCount, eventCount);
        }
    }

    /// <summary>Bereinigt verwaiste Einträge, optimiert Indizes und führt VACUUM aus.</summary>
    public static (int cleanedEvents, int cleanedSessions, long sizeBefore, long sizeAfter) Cleanup()
    {
        lock (_writeLock)
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

    /// <summary>
    /// Liefert eine umfassende Diagnose der SQLite-Datenbank:
    /// Schema- und Parser-Versionen, Tabellen- und Spaltenvalidierung, Indizes, Zeilenzahlen und Integritätsprüfung.
    /// </summary>
    public static DatabaseDiagnosticsInfo GetDiagnostics(bool runDeepCheck = true)
    {
        EnsureInitialized();
        var diag = new DatabaseDiagnosticsInfo
        {
            DatabasePath = DbPath,
            DatabaseSizeBytes = GetDatabaseSizeBytes(),
            CheckedAt = DateTime.Now
        };

        try
        {
            using var db = new SqliteConnection(Conn);
            db.Open();

            // 1. SQLite Version & Journal Mode
            var sqliteVer = Scalar(db, "SELECT sqlite_version();");
            diag.SqliteVersion = sqliteVer?.ToString() ?? "Unbekannt";

            var jMode = Scalar(db, "PRAGMA journal_mode;");
            diag.JournalMode = jMode?.ToString()?.ToUpperInvariant() ?? "WAL";

            // 2. Schema- & Parser-Versionen
            var userVer = Scalar(db, "PRAGMA user_version;");
            diag.InstalledSchemaVersion = Convert.ToInt32(userVer ?? 0);

            var parserVerStr = GetMeta(db, "parserVersion");
            if (int.TryParse(parserVerStr, out var pVer))
                diag.InstalledParserVersion = pVer;

            // 3. Tabellen-Existenz prüfen
            var expectedTables = new Dictionary<string, string[]>
            {
                ["meta"] = new[] { "key", "value" },
                ["sessions"] = new[] { "name", "start", "end", "fingerprint" },
                ["events"] = new[] { "session", "time", "kind", "amount", "detail", "ship" },
                ["contracts"] = new[] { "id", "title", "reward", "contracted_by", "scanned_at", "status" },
                ["user_pois"] = new[] { "id", "system", "body", "name", "notes", "category", "color", "created_at" },
                ["reputation"] = new[] { "faction_id", "xp", "completed_missions", "last_updated" },
                ["fleet_user_ships"] = new[] { "name", "in_hangar", "is_pledge", "pledge_usd", "insurance", "acquisition", "notes" },
                ["warehouse_items"] = new[] { "location", "location_code", "system", "parent_body", "item_class", "item_name", "category", "quantity", "last_updated" },
                ["wiki_items_cache"] = new[] { "class_name", "name", "category", "manufacturer", "description_de", "description_en", "thumbnail_url", "image_url", "web_url", "updated_at" },
                ["pilot_profiles"] = new[] { "handle", "citizen_record", "title", "avatar_url", "enlisted", "fluency", "org_name", "org_sid", "org_rank", "org_logo_url", "website", "profile_url", "bio", "updated_at" }
            };

            var existingTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = db.CreateCommand())
            {
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    existingTables.Add(r.GetString(0));
                }
            }
            diag.ExistingTables = existingTables.OrderBy(t => t).ToList();

            foreach (var kvp in expectedTables)
            {
                var tbl = kvp.Key;
                if (!existingTables.Contains(tbl))
                {
                    diag.MissingTables.Add(tbl);
                }
                else
                {
                    var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    using var colCmd = db.CreateCommand();
                    colCmd.CommandText = $"PRAGMA table_info({tbl});";
                    using var cr = colCmd.ExecuteReader();
                    while (cr.Read())
                    {
                        existingCols.Add(cr.GetString(1));
                    }

                    foreach (var col in kvp.Value)
                    {
                        if (!existingCols.Contains(col))
                            diag.MissingColumns.Add($"{tbl}.{col}");
                    }
                }
            }

            // 4. Indizes prüfen
            var expectedIndexes = new[]
            {
                "ix_events_session",
                "ix_events_kind",
                "ix_events_time",
                "ix_events_session_kind",
                "ix_events_kind_time",
                "ix_contracts_status",
                "ix_user_pois_system",
                "ix_warehouse_location",
                "ix_warehouse_category"
            };

            var existingIndexes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var idxCmd = db.CreateCommand())
            {
                idxCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='index' AND name NOT LIKE 'sqlite_%';";
                using var ir = idxCmd.ExecuteReader();
                while (ir.Read())
                {
                    existingIndexes.Add(ir.GetString(0));
                }
            }

            foreach (var idx in expectedIndexes)
            {
                if (!existingIndexes.Contains(idx))
                    diag.MissingIndexes.Add(idx);
            }

            // 5. Datensatz-Zahlen ermitteln
            int SafeCount(string tbl)
            {
                if (!existingTables.Contains(tbl)) return 0;
                try
                {
                    var count = Scalar(db, $"SELECT COUNT(*) FROM {tbl};");
                    return Convert.ToInt32(count ?? 0);
                }
                catch { return 0; }
            }

            diag.SessionCount = SafeCount("sessions");
            diag.EventCount = SafeCount("events");
            diag.ContractCount = SafeCount("contracts");
            diag.FleetShipCount = SafeCount("fleet_user_ships");
            diag.PoiCount = SafeCount("user_pois");
            diag.ReputationCount = SafeCount("reputation");
            diag.WarehouseItemCount = SafeCount("warehouse_items");
            diag.WikiItemCount = SafeCount("wiki_items_cache");
            diag.PilotProfileCount = SafeCount("pilot_profiles");

            // 6. Physische Integritätsprüfung
            if (runDeepCheck)
            {
                var check = Scalar(db, "PRAGMA quick_check;");
                var checkStr = check?.ToString() ?? "";
                if (string.Equals(checkStr, "ok", StringComparison.OrdinalIgnoreCase))
                {
                    diag.IntegrityCheckOk = true;
                    diag.IntegrityMessage = "Fehlerfrei (ok)";
                }
                else
                {
                    diag.IntegrityCheckOk = false;
                    diag.IntegrityMessage = string.IsNullOrWhiteSpace(checkStr) ? "Unbekannter Fehler" : checkStr;
                }
            }
            else
            {
                diag.IntegrityCheckOk = true;
                diag.IntegrityMessage = "Übersprungen (Schnellprüfung)";
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Database.GetDiagnostics", ex);
            diag.IntegrityCheckOk = false;
            diag.IntegrityMessage = $"Diagnosefehler: {ex.Message}";
        }

        return diag;
    }

    /// <summary>
    /// Führt gezielte Reparaturen und Schema-Upgrades an der SQLite-Datenbank aus:
    /// Wendet fehlende Migrationen an, repariert Tabellen, Spalten und fehlende Indizes.
    /// </summary>
    public static (bool success, string message) RepairOrUpdateStructure()
    {
        lock (_writeLock)
        {
            try
            {
                using var db = new SqliteConnection(Conn);
                db.Open();

                Exec(db, @"PRAGMA journal_mode = WAL;
                           PRAGMA synchronous = NORMAL;
                           PRAGMA busy_timeout = 60000;");

                // 1. Schema-Migrationen strukturiert anwenden
                ApplySchemaMigrations(db);

                // 2. Sicherstellen, dass alle Tabellen existieren (Idempotent)
                Exec(db, @"
                    CREATE TABLE IF NOT EXISTS meta(key TEXT PRIMARY KEY, value TEXT);
                    CREATE TABLE IF NOT EXISTS sessions(name TEXT PRIMARY KEY, start TEXT, end TEXT, fingerprint TEXT);
                    CREATE TABLE IF NOT EXISTS events(session TEXT, time TEXT, kind TEXT, amount INTEGER, detail TEXT, ship TEXT);
                    CREATE TABLE IF NOT EXISTS contracts(
                        id TEXT PRIMARY KEY,
                        title TEXT NOT NULL,
                        reward INTEGER NOT NULL,
                        contracted_by TEXT NOT NULL,
                        scanned_at TEXT NOT NULL,
                        status TEXT NOT NULL DEFAULT 'Active'
                    );
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
                    CREATE TABLE IF NOT EXISTS reputation(
                        faction_id TEXT PRIMARY KEY,
                        xp INTEGER NOT NULL DEFAULT 0,
                        completed_missions INTEGER NOT NULL DEFAULT 0,
                        last_updated TEXT NOT NULL
                    );
                    CREATE TABLE IF NOT EXISTS fleet_user_ships(
                        name TEXT PRIMARY KEY,
                        in_hangar INTEGER NOT NULL DEFAULT 1,
                        is_pledge INTEGER NOT NULL DEFAULT 1,
                        pledge_usd INTEGER NOT NULL DEFAULT 0,
                        insurance TEXT NOT NULL DEFAULT 'LTI (Lifetime)',
                        acquisition TEXT NOT NULL DEFAULT 'Pledge Store',
                        notes TEXT NOT NULL DEFAULT ''
                    );
                    CREATE TABLE IF NOT EXISTS warehouse_items (
                        location TEXT NOT NULL,
                        location_code TEXT NOT NULL,
                        system TEXT NOT NULL,
                        parent_body TEXT NOT NULL,
                        item_class TEXT NOT NULL,
                        item_name TEXT NOT NULL,
                        category TEXT NOT NULL,
                        quantity INTEGER NOT NULL,
                        last_updated TEXT NOT NULL,
                        PRIMARY KEY (location, item_class)
                    );
                    CREATE INDEX IF NOT EXISTS ix_warehouse_location ON warehouse_items(location);
                    CREATE INDEX IF NOT EXISTS ix_warehouse_category ON warehouse_items(category);
                    CREATE TABLE IF NOT EXISTS wiki_items_cache (
                        class_name TEXT PRIMARY KEY,
                        name TEXT NOT NULL,
                        category TEXT,
                        manufacturer TEXT,
                        description_de TEXT,
                        description_en TEXT,
                        thumbnail_url TEXT,
                        image_url TEXT,
                        web_url TEXT,
                        updated_at TEXT NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS ix_wiki_items_name ON wiki_items_cache(name);
                    CREATE TABLE IF NOT EXISTS pilot_profiles (
                        handle TEXT PRIMARY KEY,
                        citizen_record TEXT,
                        title TEXT,
                        avatar_url TEXT,
                        enlisted TEXT,
                        fluency TEXT,
                        org_name TEXT,
                        org_sid TEXT,
                        org_rank TEXT,
                        org_logo_url TEXT,
                        website TEXT,
                        profile_url TEXT,
                        bio TEXT,
                        updated_at TEXT NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS ix_pilot_profiles_citizen_record ON pilot_profiles(citizen_record);
                ");

                // 3. Kritische Spalten nachziehen (falls eine Tabelle älter war)
                try { Exec(db, "ALTER TABLE sessions ADD COLUMN fingerprint TEXT;"); } catch { }
                try { Exec(db, "ALTER TABLE fleet_user_ships ADD COLUMN in_hangar INTEGER NOT NULL DEFAULT 1;"); } catch { }
                Exec(db, "UPDATE fleet_user_ships SET in_hangar = 1 WHERE is_pledge = 1 OR acquisition = 'Pledge Store' OR acquisition = 'In-Game (aUEC)';");

                // 4. Alle Indizes herstellen
                Exec(db, @"CREATE INDEX IF NOT EXISTS ix_events_session ON events(session);
                           CREATE INDEX IF NOT EXISTS ix_events_kind ON events(kind);
                           CREATE INDEX IF NOT EXISTS ix_events_time ON events(time);
                           CREATE INDEX IF NOT EXISTS ix_events_session_kind ON events(session, kind);
                           CREATE INDEX IF NOT EXISTS ix_events_kind_time ON events(kind, time);
                           CREATE INDEX IF NOT EXISTS ix_contracts_status ON contracts(status);
                           CREATE INDEX IF NOT EXISTS ix_user_pois_system ON user_pois(system);");

                // 5. Metadaten synchronisieren
                Exec(db, $"PRAGMA user_version = {CurrentSchemaVersion};");
                SetMeta(db, "schemaVersion", CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture));
                if (string.IsNullOrEmpty(GetMeta(db, "parserVersion")))
                {
                    SetMeta(db, "parserVersion", CurrentParserVersion.ToString(CultureInfo.InvariantCulture));
                }

                Logger.Log($"DB: Struktur-Reparatur und Schema-Aktualisierung auf v{CurrentSchemaVersion} erfolgreich durchgeführt.");
                return (true, $"Struktur erfolgreich auf Schema v{CurrentSchemaVersion} aktualisiert und alle Indizes repariert.");
            }
            catch (Exception ex)
            {
                Logger.Error("Database.RepairOrUpdateStructure", ex);
                return (false, $"Fehler bei Struktur-Reparatur: {ex.Message}");
            }
        }
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

    /// <summary>Liefert die neuesten gespeicherten Events (neueste zuerst, bis maxEntries), chronologisch sortiert. Optional nach Session gefiltert.</summary>
    public static List<LogEntry> LoadRecentEvents(int maxEntries = 15000, string? session = null)
    {
        var list = new List<LogEntry>(Math.Min(maxEntries, 2000));
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var cmd = db.CreateCommand();
        if (!string.IsNullOrEmpty(session) && session != "__all__")
        {
            cmd.CommandText = $"SELECT time,kind,amount,detail,ship FROM events WHERE session = $sess ORDER BY time DESC LIMIT {maxEntries}";
            cmd.Parameters.AddWithValue("$sess", session);
        }
        else
        {
            cmd.CommandText = $"SELECT time,kind,amount,detail,ship FROM events ORDER BY time DESC LIMIT {maxEntries}";
        }
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            DateTime.TryParse(r.GetString(0), CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t);
            Enum.TryParse<EventKind>(r.GetString(1), out var kind);
            list.Add(new LogEntry
            {
                Time = t,
                Kind = kind,
                Amount = r.GetInt64(2),
                Detail = r.IsDBNull(3) ? "" : r.GetString(3),
                Ship = r.IsDBNull(4) ? null : r.GetString(4)
            });
        }
        list.Reverse(); // In chronologische Reihenfolge bringen
        return list;
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
                    case "Purchase": a.Purchases += -sum; break;
                    case "Maintenance": a.Purchases += -sum; break;
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
        c.CommandText = @"SELECT DISTINCT time,kind,amount,detail,ship FROM events
                          WHERE kind IN ('TransferIn','TransferOut','MissionReward','Purchase','Sale','Trade','Fine')
                          ORDER BY ABS(amount) DESC LIMIT $n";
        c.Parameters.AddWithValue("$n", n);
        using var r = c.ExecuteReader();
        while (r.Read())
        {
            DateTime.TryParse(r.GetString(0), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t);
            Enum.TryParse<EventKind>(r.GetString(1), out var kind);
            list.Add(new LogEntry
            {
                Time = t,
                Kind = kind,
                Amount = r.GetInt64(2),
                Detail = r.IsDBNull(3) ? "" : r.GetString(3),
                Ship = r.IsDBNull(4) ? null : r.GetString(4)
            });
        }
        return list;
    }

    /// <summary>Alle Geld-Events (chronologisch aufsteigend) für das Finanzdiagramm.</summary>
    public static List<LogEntry> AllFinanceEvents(DateTime? since = null)
    {
        var list = new List<LogEntry>();
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var c = db.CreateCommand();
        if (since.HasValue)
        {
            c.CommandText = @"SELECT time,kind,amount,detail,ship FROM events
                              WHERE kind IN ('TransferIn','TransferOut','MissionReward','Purchase','Sale','Trade','Fine')
                              AND time >= $since
                              ORDER BY time ASC";
            c.Parameters.AddWithValue("$since", since.Value.ToString("o", CultureInfo.InvariantCulture));
        }
        else
        {
            c.CommandText = @"SELECT time,kind,amount,detail,ship FROM events
                              WHERE kind IN ('TransferIn','TransferOut','MissionReward','Purchase','Sale','Trade','Fine')
                              ORDER BY time ASC";
        }
        using var r = c.ExecuteReader();
        while (r.Read())
        {
            DateTime.TryParse(r.GetString(0), CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t);
            Enum.TryParse<EventKind>(r.GetString(1), out var kind);
            list.Add(new LogEntry
            {
                Time = t,
                Kind = kind,
                Amount = r.GetInt64(2),
                Detail = r.IsDBNull(3) ? "" : r.GetString(3),
                Ship = r.IsDBNull(4) ? null : r.GetString(4)
            });
        }
        return list;
    }

    /// <summary>Neueste N Geld-Events (chronologisch absteigend, neueste zuerst).</summary>
    public static List<LogEntry> RecentMoneyEvents(int n)
    {
        var list = new List<LogEntry>();
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var c = db.CreateCommand();
        c.CommandText = @"SELECT DISTINCT time,kind,amount,detail,ship FROM events
                          WHERE kind IN ('TransferIn','TransferOut','MissionReward','Purchase','Sale','Trade','Fine')
                          ORDER BY time DESC LIMIT $n";
        c.Parameters.AddWithValue("$n", n);
        using var r = c.ExecuteReader();
        while (r.Read())
        {
            DateTime.TryParse(r.GetString(0), CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t);
            Enum.TryParse<EventKind>(r.GetString(1), out var kind);
            list.Add(new LogEntry
            {
                Time = t,
                Kind = kind,
                Amount = r.GetInt64(2),
                Detail = r.IsDBNull(3) ? "" : r.GetString(3),
                Ship = r.IsDBNull(4) ? null : r.GetString(4)
            });
        }
        return list;
    }

    /// <summary>Flugschreiber-Events für eine bestimmte Session (Dateiname) oder alle Sessions.</summary>
    public static List<LogEntry> GetTimelineEventsForSession(string? sessionName = null)
    {
        var list = new List<LogEntry>();
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var c = db.CreateCommand();
        if (!string.IsNullOrEmpty(sessionName))
        {
            c.CommandText = @"SELECT time,kind,amount,detail,ship FROM events
                              WHERE session = $sess
                              AND kind IN ('Vehicle','ShipLoss','Quantum','Location','Hangar','Crash','Death')
                              ORDER BY time ASC";
            c.Parameters.AddWithValue("$sess", sessionName);
        }
        else
        {
            c.CommandText = @"SELECT time,kind,amount,detail,ship FROM events
                              WHERE kind IN ('Vehicle','ShipLoss','Quantum','Location','Hangar','Crash','Death')
                              ORDER BY time ASC";
        }
        using var r = c.ExecuteReader();
        while (r.Read())
        {
            DateTime.TryParse(r.GetString(0), CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t);
            Enum.TryParse<EventKind>(r.GetString(1), out var kind);
            list.Add(new LogEntry
            {
                Time = t,
                Kind = kind,
                Amount = r.GetInt64(2),
                Detail = r.IsDBNull(3) ? "" : r.GetString(3),
                Ship = r.IsDBNull(4) ? null : r.GetString(4)
            });
        }
        return list;
    }

    /// <summary>Alle Flugschreiber-relevanten Events aus allen Sessions chronologisch aufsteigend.</summary>
    public static List<LogEntry> AllTimelineEvents(DateTime? since = null)
    {
        var list = new List<LogEntry>();
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var c = db.CreateCommand();
        if (since.HasValue)
        {
            c.CommandText = @"SELECT time,kind,amount,detail,ship FROM events
                              WHERE kind IN ('Vehicle','ShipLoss','Quantum','Location','Hangar','Crash','Death')
                              AND time >= $since
                              ORDER BY time ASC";
            c.Parameters.AddWithValue("$since", since.Value.ToString("o", CultureInfo.InvariantCulture));
        }
        else
        {
            c.CommandText = @"SELECT time,kind,amount,detail,ship FROM events
                              WHERE kind IN ('Vehicle','ShipLoss','Quantum','Location','Hangar','Crash','Death')
                              ORDER BY time ASC";
        }
        using var r = c.ExecuteReader();
        while (r.Read())
        {
            DateTime.TryParse(r.GetString(0), CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t);
            Enum.TryParse<EventKind>(r.GetString(1), out var kind);
            list.Add(new LogEntry
            {
                Time = t,
                Kind = kind,
                Amount = r.GetInt64(2),
                Detail = r.IsDBNull(3) ? "" : r.GetString(3),
                Ship = r.IsDBNull(4) ? null : r.GetString(4)
            });
        }
        return list;
    }

    /// <summary>Alle angenommenen Missionen aus allen Sessions chronologisch aufsteigend.</summary>
    public static List<LogEntry> AllMissionTakenEvents()
    {
        var list = new List<LogEntry>();
        using var db = new SqliteConnection(Conn);
        db.Open();
        using var c = db.CreateCommand();
        c.CommandText = "SELECT time,detail FROM events WHERE kind='MissionTaken' ORDER BY time ASC";
        using var r = c.ExecuteReader();
        while (r.Read())
        {
            DateTime.TryParse(r.GetString(0), CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t);
            list.Add(new LogEntry { Time = t, Kind = EventKind.MissionTaken, Detail = r.IsDBNull(1) ? "" : r.GetString(1) });
        }
        return list;
    }

    // ---- Helfer ----
    static IEnumerable<string> ReadShared(string file)
    {
        using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 65536);
        using var sr = new StreamReader(fs, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 65536);
        string? l;
        while ((l = sr.ReadLine()) != null) yield return l;
    }

    static void Exec(SqliteConnection db, string sql, params (string, object?)[] parameters)
    {
        using var c = db.CreateCommand();
        c.CommandText = sql;
        foreach (var (key, value) in parameters) c.Parameters.AddWithValue(key, value ?? DBNull.Value);
        c.ExecuteNonQuery();
    }

    static object? Scalar(SqliteConnection db, string sql, params (string, object?)[] ps)
    {
        using var c = db.CreateCommand();
        c.CommandText = sql;
        foreach (var (k, v) in ps) c.Parameters.AddWithValue(k, v ?? DBNull.Value);
        return c.ExecuteScalar();
    }

    static string? GetMeta(SqliteConnection db, string key) =>
        Scalar(db, "SELECT value FROM meta WHERE key=$k", ("$k", key)) as string;

    static string GetFileFingerprint(string file)
    {
        var info = new FileInfo(file);
        return $"{info.Length}:{info.LastWriteTimeUtc.Ticks}";
    }

    static void SetMeta(SqliteConnection db, string key, string value)
    {
        using var c = db.CreateCommand();
        c.CommandText = "INSERT OR REPLACE INTO meta(key,value) VALUES($k,$v)";
        c.Parameters.AddWithValue("$k", key);
        c.Parameters.AddWithValue("$v", value);
        c.ExecuteNonQuery();
    }

    public static string? GetMeta(string key)
    {
        EnsureInitialized();
        using var db = new SqliteConnection(Conn);
        db.Open();
        return GetMeta(db, key);
    }

    public static void SetMeta(string key, string value)
    {
        lock (_writeLock)
        {
            EnsureInitialized();
            using var db = new SqliteConnection(Conn);
            db.Open();
            SetMeta(db, key, value);
        }
    }

    /// <summary>
    /// Speichert ein manuelles oder per mobiGlas-Delta erfasstes Ausgabe-/Wartungs-Ereignis persistent in der events-Tabelle.
    /// </summary>
    public static void InsertCustomEvent(string session, DateTime time, Models.EventKind kind, long amount, string detail, string? ship = null)
    {
        lock (_writeLock)
        {
            try
            {
                EnsureInitialized();
                using var db = new SqliteConnection(Conn);
                db.Open();
                using var cmd = db.CreateCommand();
                cmd.CommandText = "INSERT INTO events(session,time,kind,amount,detail,ship) VALUES($s,$t,$k,$a,$d,$sh);";
                cmd.Parameters.AddWithValue("$s", session);
                cmd.Parameters.AddWithValue("$t", time.ToString("o", CultureInfo.InvariantCulture));
                cmd.Parameters.AddWithValue("$k", kind.ToString());
                cmd.Parameters.AddWithValue("$a", amount);
                cmd.Parameters.AddWithValue("$d", detail);
                cmd.Parameters.AddWithValue("$sh", (object?)ship ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.Error("Database.InsertCustomEvent", ex);
            }
        }
    }

    public static void SaveContract(Models.ContractDetails contract)
    {
        lock (_writeLock)
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
        lock (_writeLock)
        {
            EnsureInitialized();
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "DELETE FROM contracts;";
            cmd.ExecuteNonQuery();
        }
    }

    public static void RemoveContract(string title, int reward)
    {
        lock (_writeLock)
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
        lock (_writeLock)
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
    }

    public static void DeleteUserPoi(int id)
    {
        lock (_writeLock)
        {
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "DELETE FROM user_pois WHERE id=$id;";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
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
        lock (_writeLock)
        {
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
    }

    public static void SetFactionReputation(string factionId, int xp, int completedMissions, DateTime time)
    {
        if (string.IsNullOrWhiteSpace(factionId)) return;
        lock (_writeLock)
        {
            EnsureInitialized();
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO reputation (faction_id, xp, completed_missions, last_updated)
                VALUES ($fid, $xp, $missions, $last)
                ON CONFLICT(faction_id) DO UPDATE SET
                    xp = $xp,
                    completed_missions = $missions,
                    last_updated = $last;";
            cmd.Parameters.AddWithValue("$fid", factionId);
            cmd.Parameters.AddWithValue("$xp", Math.Max(0, xp));
            cmd.Parameters.AddWithValue("$missions", Math.Max(0, completedMissions));
            cmd.Parameters.AddWithValue("$last", time.ToString("o"));
            cmd.ExecuteNonQuery();
        }
    }

    public static void ResetFactionReputations()
    {
        lock (_writeLock)
        {
            EnsureInitialized();
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "DELETE FROM reputation;";
            cmd.ExecuteNonQuery();
        }
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
                var rawName = r.GetString(0);
                var inHangar = r.GetInt32(1) == 1;
                var isPledge = r.GetInt32(2) == 1;
                var pledgeUsd = r.GetInt32(3);
                var insurance = r.GetString(4);
                var acq = r.GetString(5);
                var notes = r.GetString(6);

                var cat = FleetCatalog.Lookup(rawName);
                var canonicalName = cat.NormalizedName != "Unbekannt" ? cat.NormalizedName : rawName;

                var data = new DbFleetCustomData(inHangar, isPledge, pledgeUsd, insurance, acq, notes);

                if (dict.TryGetValue(canonicalName, out var existing))
                {
                    bool mergedHangar = existing.InHangar || inHangar;
                    bool mergedPledge = existing.IsPledge || isPledge;
                    int mergedUsd = Math.Max(existing.PledgeUsd, pledgeUsd);
                    string mergedIns = !string.IsNullOrEmpty(insurance) && insurance != "LTI (Lifetime)" ? insurance : existing.Insurance;
                    string mergedAcq = mergedPledge ? "Pledge Store" : (!string.IsNullOrEmpty(acq) ? acq : existing.Acquisition);
                    string mergedNotes = !string.IsNullOrEmpty(existing.Notes) ? existing.Notes : notes;
                    dict[canonicalName] = new DbFleetCustomData(mergedHangar, mergedPledge, mergedUsd, mergedIns, mergedAcq, mergedNotes);
                }
                else
                {
                    dict[canonicalName] = data;
                }

                if (!dict.ContainsKey(rawName))
                {
                    dict[rawName] = dict[canonicalName];
                }
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
        var cat = FleetCatalog.Lookup(name);
        var canonicalName = cat.NormalizedName != "Unbekannt" ? cat.NormalizedName : name;

        if (isPledge || string.Equals(acquisition, "Pledge Store", StringComparison.OrdinalIgnoreCase) || string.Equals(acquisition, "In-Game (aUEC)", StringComparison.OrdinalIgnoreCase))
        {
            inHangar = true;
        }
        lock (_writeLock)
        {
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
                cmd.Parameters.AddWithValue("$n", canonicalName);
                cmd.Parameters.AddWithValue("$ih", inHangar ? 1 : 0);
                cmd.Parameters.AddWithValue("$p", isPledge ? 1 : 0);
                cmd.Parameters.AddWithValue("$u", pledgeUsd);
                cmd.Parameters.AddWithValue("$i", insurance);
                cmd.Parameters.AddWithValue("$a", acquisition);
                cmd.Parameters.AddWithValue("$nt", notes);
                cmd.ExecuteNonQuery();

                // Falls ein alternativer Legacy-Name existierte, diesen aufräumen
                if (!canonicalName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    using var delCmd = db.CreateCommand();
                    delCmd.CommandText = "DELETE FROM fleet_user_ships WHERE name = $old;";
                    delCmd.Parameters.AddWithValue("$old", name);
                    delCmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("SaveFleetShipCustomData", ex);
            }
        }
    }

    #endregion

    #region Warehouse Inventory

    public static void RecordWarehouseMovement(DateTime time, string location, string locationCode, string system, string parentBody, string itemClass, string itemName, string category, int deltaQty)
    {
        lock (_writeLock)
        {
            EnsureInitialized();
            try
            {
                using var db = new SqliteConnection(Conn);
                db.Open();
                RecordWarehouseMovementInternal(db, null, time, location, locationCode, system, parentBody, itemClass, itemName, category, deltaQty);
            }
            catch (Exception ex)
            {
                Logger.Error("RecordWarehouseMovement", ex);
            }
        }
    }

    public static void RecordWarehouseMovementInternal(SqliteConnection db, SqliteTransaction? tx, DateTime time, string location, string locationCode, string system, string parentBody, string itemClass, string itemName, string category, int deltaQty)
    {
        using var cmd = db.CreateCommand();
        if (tx != null) cmd.Transaction = tx;
        cmd.CommandText = @"
            INSERT INTO warehouse_items (location, location_code, system, parent_body, item_class, item_name, category, quantity, last_updated)
            VALUES ($loc, $code, $sys, $body, $cls, $name, $cat, $qty, $lu)
            ON CONFLICT(location, item_class) DO UPDATE SET
                quantity = warehouse_items.quantity + excluded.quantity,
                last_updated = CASE WHEN excluded.last_updated > warehouse_items.last_updated THEN excluded.last_updated ELSE warehouse_items.last_updated END;
        ";
        cmd.Parameters.AddWithValue("$loc", location);
        cmd.Parameters.AddWithValue("$code", locationCode);
        cmd.Parameters.AddWithValue("$sys", system);
        cmd.Parameters.AddWithValue("$body", parentBody);
        cmd.Parameters.AddWithValue("$cls", itemClass);
        cmd.Parameters.AddWithValue("$name", itemName);
        cmd.Parameters.AddWithValue("$cat", category);
        cmd.Parameters.AddWithValue("$qty", deltaQty);
        cmd.Parameters.AddWithValue("$lu", time.ToString("o", CultureInfo.InvariantCulture));
        cmd.ExecuteNonQuery();
    }

    public static void AdjustWarehouseItemQuantity(string location, string itemClass, int delta)
    {
        EnsureInitialized();
        lock (_writeLock)
        {
            try
            {
                using var db = new SqliteConnection(Conn);
                db.Open();
                using var cmd = db.CreateCommand();
                cmd.CommandText = @"
                    UPDATE warehouse_items
                    SET quantity = quantity + $delta,
                        last_updated = $lu
                    WHERE location = $loc AND item_class = $cls;

                    DELETE FROM warehouse_items
                    WHERE location = $loc AND item_class = $cls AND quantity <= 0;
                ";
                cmd.Parameters.AddWithValue("$delta", delta);
                cmd.Parameters.AddWithValue("$loc", location);
                cmd.Parameters.AddWithValue("$cls", itemClass);
                cmd.Parameters.AddWithValue("$lu", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.Error("AdjustWarehouseItemQuantity", ex);
            }
        }
    }

    public static void DeleteWarehouseItem(string location, string itemClass)
    {
        EnsureInitialized();
        lock (_writeLock)
        {
            try
            {
                using var db = new SqliteConnection(Conn);
                db.Open();
                using var cmd = db.CreateCommand();
                cmd.CommandText = "DELETE FROM warehouse_items WHERE location = $loc AND item_class = $cls;";
                cmd.Parameters.AddWithValue("$loc", location);
                cmd.Parameters.AddWithValue("$cls", itemClass);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.Error("DeleteWarehouseItem", ex);
            }
        }
    }

    public static void ClearWarehouseLocation(string location)
    {
        EnsureInitialized();
        lock (_writeLock)
        {
            try
            {
                using var db = new SqliteConnection(Conn);
                db.Open();
                using var cmd = db.CreateCommand();
                cmd.CommandText = "DELETE FROM warehouse_items WHERE location = $loc;";
                cmd.Parameters.AddWithValue("$loc", location);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Logger.Error("ClearWarehouseLocation", ex);
            }
        }
    }

    public static List<WarehouseItem> GetWarehouseItems(string? locationFilter = null, string? categoryFilter = null, string? search = null)
    {
        EnsureInitialized();
        var items = new List<WarehouseItem>();
        try
        {
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();

            var sql = "SELECT location, location_code, system, parent_body, item_class, item_name, category, quantity, last_updated FROM warehouse_items WHERE quantity > 0";

            if (!string.IsNullOrWhiteSpace(locationFilter) && locationFilter != "Alle Standorte")
            {
                sql += " AND location = $loc";
                cmd.Parameters.AddWithValue("$loc", locationFilter);
            }

            if (!string.IsNullOrWhiteSpace(categoryFilter) && categoryFilter != "Alle Kategorien")
            {
                sql += " AND (category = $cat OR category LIKE $catPrefix)";
                cmd.Parameters.AddWithValue("$cat", categoryFilter);
                cmd.Parameters.AddWithValue("$catPrefix", $"%{categoryFilter}%");
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += " AND (item_name LIKE $search OR item_class LIKE $search OR location LIKE $search OR category LIKE $search)";
                cmd.Parameters.AddWithValue("$search", $"%{search}%");
            }

            sql += " ORDER BY location ASC, category ASC, item_name ASC;";
            cmd.CommandText = sql;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                DateTime.TryParse(reader.GetString(8), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var lu);
                var itemClass = reader.GetString(4);
                var rawName = reader.GetString(5);
                var rawCat = reader.GetString(6);

                var (resolvedName, resolvedCat) = WarehouseCatalog.Resolve(itemClass);
                var finalName = !string.IsNullOrWhiteSpace(resolvedName) && resolvedName != itemClass ? resolvedName : rawName;
                var finalCat = !string.IsNullOrWhiteSpace(resolvedCat) && resolvedCat != "Sonstiges" ? resolvedCat : rawCat;

                items.Add(new WarehouseItem
                {
                    Location = reader.GetString(0),
                    LocationCode = reader.GetString(1),
                    System = reader.GetString(2),
                    ParentBody = reader.GetString(3),
                    ItemClass = itemClass,
                    ItemName = finalName,
                    Category = finalCat,
                    Quantity = reader.GetInt32(7),
                    LastUpdated = lu
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Error("GetWarehouseItems", ex);
        }
        return items;
    }

    public static List<WarehouseLocationGroup> GetWarehouseLocationsSummary()
    {
        EnsureInitialized();
        var list = new List<WarehouseLocationGroup>();
        try
        {
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = @"
                SELECT location, location_code, system, parent_body, SUM(quantity) as total_items, COUNT(DISTINCT item_class) as unique_types
                FROM warehouse_items
                WHERE quantity > 0
                GROUP BY location, location_code, system, parent_body
                ORDER BY total_items DESC, location ASC;
            ";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new WarehouseLocationGroup
                {
                    LocationName = reader.GetString(0),
                    LocationCode = reader.GetString(1),
                    System = reader.GetString(2),
                    ParentBody = reader.GetString(3),
                    TotalItems = Convert.ToInt32(reader.GetInt64(4)),
                    UniqueItemTypes = Convert.ToInt32(reader.GetInt64(5))
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Error("GetWarehouseLocationsSummary", ex);
        }
        return list;
    }

    public static Dictionary<string, (string Name, string Category)> GetAllCachedWikiItemNames()
    {
        EnsureInitialized();
        var dict = new Dictionary<string, (string Name, string Category)>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "SELECT class_name, name, category FROM wiki_items_cache WHERE name != '';";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                dict[reader.GetString(0)] = (reader.GetString(1), reader.IsDBNull(2) ? "Sonstiges" : reader.GetString(2));
            }
        }
        catch (Exception ex)
        {
            Logger.Error("GetAllCachedWikiItemNames", ex);
        }
        return dict;
    }

    public static WikiInfo? GetCachedWikiItem(string className)
    {
        EnsureInitialized();
        try
        {
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = @"
                SELECT name, category, manufacturer, description_de, description_en, thumbnail_url, image_url, web_url 
                FROM wiki_items_cache 
                WHERE class_name = $cls 
                LIMIT 1;
            ";
            cmd.Parameters.AddWithValue("$cls", className);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new WikiInfo
                {
                    Name = reader.GetString(0),
                    Category = reader.IsDBNull(1) ? "Item" : reader.GetString(1),
                    Manufacturer = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    DescriptionDe = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    DescriptionEn = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    ThumbnailUrl = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    ImageUrl = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    WebUrl = reader.IsDBNull(7) ? "" : reader.GetString(7)
                };
            }
        }
        catch (Exception ex)
        {
            Logger.Error("GetCachedWikiItem", ex);
        }
        return null;
    }

    public static void SaveCachedWikiItem(string className, WikiInfo info)
    {
        EnsureInitialized();
        try
        {
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO wiki_items_cache (class_name, name, category, manufacturer, description_de, description_en, thumbnail_url, image_url, web_url, updated_at)
                VALUES ($cls, $name, $cat, $mfg, $dde, $den, $thumb, $img, $web, $updated)
                ON CONFLICT(class_name) DO UPDATE SET
                    name = excluded.name,
                    category = excluded.category,
                    manufacturer = excluded.manufacturer,
                    description_de = excluded.description_de,
                    description_en = excluded.description_en,
                    thumbnail_url = excluded.thumbnail_url,
                    image_url = excluded.image_url,
                    web_url = excluded.web_url,
                    updated_at = excluded.updated_at;
            ";
            cmd.Parameters.AddWithValue("$cls", className);
            cmd.Parameters.AddWithValue("$name", info.Name);
            cmd.Parameters.AddWithValue("$cat", info.Category ?? "Sonstiges");
            cmd.Parameters.AddWithValue("$mfg", info.Manufacturer ?? "");
            cmd.Parameters.AddWithValue("$dde", info.DescriptionDe ?? "");
            cmd.Parameters.AddWithValue("$den", info.DescriptionEn ?? "");
            cmd.Parameters.AddWithValue("$thumb", info.ThumbnailUrl ?? "");
            cmd.Parameters.AddWithValue("$img", info.ImageUrl ?? "");
            cmd.Parameters.AddWithValue("$web", info.WebUrl ?? "");
            cmd.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();

            using var updateCmd = db.CreateCommand();
            updateCmd.CommandText = @"
                UPDATE warehouse_items 
                SET item_name = $name, category = $cat 
                WHERE item_class = $cls;
            ";
            updateCmd.Parameters.AddWithValue("$name", info.Name);
            updateCmd.Parameters.AddWithValue("$cat", info.Category ?? "Sonstiges");
            updateCmd.Parameters.AddWithValue("$cls", className);
            updateCmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Logger.Error("SaveCachedWikiItem", ex);
        }
    }

    public static WikiInfo? GetCachedWikiVehicle(string name)
    {
        EnsureInitialized();
        if (string.IsNullOrWhiteSpace(name)) return null;
        try
        {
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = @"
                SELECT name, manufacturer, role, type, focus, size, crew_min, crew_max, 
                       cargo_scu, quantum_fuel, length, beam, height, mass, msrp, 
                       production_status, description_de, description_en, thumbnail_url, 
                       image_url, web_url, pledge_url, specs_json, stores_json 
                FROM wiki_vehicles_cache 
                WHERE name = $name 
                LIMIT 1;
            ";
            cmd.Parameters.AddWithValue("$name", name.Trim());
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var info = new WikiInfo
                {
                    Name = reader.GetString(0),
                    Category = "Schiff & Fahrzeug",
                    Manufacturer = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Role = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Type = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Focus = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Size = reader.IsDBNull(5) ? "" : WikiApiClient.CleanLocalizedField(reader.GetString(5)),
                    CrewMin = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    CrewMax = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    CargoScu = reader.IsDBNull(8) ? null : reader.GetDouble(8),
                    QuantumFuel = reader.IsDBNull(9) ? null : reader.GetDouble(9),
                    Length = reader.IsDBNull(10) ? null : reader.GetDouble(10),
                    Beam = reader.IsDBNull(11) ? null : reader.GetDouble(11),
                    Height = reader.IsDBNull(12) ? null : reader.GetDouble(12),
                    Mass = reader.IsDBNull(13) ? null : reader.GetDouble(13),
                    Msrp = reader.IsDBNull(14) ? null : reader.GetDouble(14),
                    ProductionStatus = reader.IsDBNull(15) ? "" : WikiApiClient.CleanLocalizedField(reader.GetString(15)),
                    DescriptionDe = reader.IsDBNull(16) ? "" : reader.GetString(16),
                    DescriptionEn = reader.IsDBNull(17) ? "" : reader.GetString(17),
                    ThumbnailUrl = reader.IsDBNull(18) ? "" : reader.GetString(18),
                    ImageUrl = reader.IsDBNull(19) ? "" : reader.GetString(19),
                    WebUrl = reader.IsDBNull(20) ? "" : reader.GetString(20),
                    PledgeUrl = reader.IsDBNull(21) ? "" : reader.GetString(21),
                };

                if (!reader.IsDBNull(22))
                {
                    try
                    {
                        var json = reader.GetString(22);
                        var rawSpecs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                        var cleanSpecs = new Dictionary<string, string>();
                        foreach (var kv in rawSpecs)
                        {
                            var v = kv.Value;
                            if (v.Contains("{") && v.Contains("\""))
                            {
                                var cleanInner = WikiApiClient.CleanLocalizedField(v.Replace("Größe ", ""));
                                v = int.TryParse(cleanInner, out _) || cleanInner.Length == 1 ? $"Größe {cleanInner}" : cleanInner;
                            }
                            cleanSpecs[kv.Key] = v;
                        }
                        info.Specs = cleanSpecs;
                    }
                    catch { }
                }

                if (!reader.IsDBNull(23))
                {
                    try
                    {
                        var json = reader.GetString(23);
                        info.StoreLocations = System.Text.Json.JsonSerializer.Deserialize<List<WikiStoreLocationDto>>(json) ?? new();
                    }
                    catch { }
                }

                return info;
            }
        }
        catch (Exception ex)
        {
            Logger.Error("GetCachedWikiVehicle", ex);
        }
        return null;
    }

    public static void SaveCachedWikiVehicle(WikiInfo info)
    {
        EnsureInitialized();
        if (info == null || string.IsNullOrWhiteSpace(info.Name)) return;
        try
        {
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO wiki_vehicles_cache (
                    name, manufacturer, role, type, focus, size, crew_min, crew_max, 
                    cargo_scu, quantum_fuel, length, beam, height, mass, msrp, 
                    production_status, description_de, description_en, thumbnail_url, 
                    image_url, web_url, pledge_url, specs_json, stores_json, updated_at
                ) VALUES (
                    $name, $mfg, $role, $type, $focus, $size, $crewMin, $crewMax,
                    $cargoScu, $qf, $len, $beam, $hgt, $mass, $msrp,
                    $status, $dde, $den, $thumb, $img, $web, $pledge,
                    $specs, $stores, $updated
                )
                ON CONFLICT(name) DO UPDATE SET
                    manufacturer = excluded.manufacturer,
                    role = excluded.role,
                    type = excluded.type,
                    focus = excluded.focus,
                    size = excluded.size,
                    crew_min = excluded.crew_min,
                    crew_max = excluded.crew_max,
                    cargo_scu = excluded.cargo_scu,
                    quantum_fuel = excluded.quantum_fuel,
                    length = excluded.length,
                    beam = excluded.beam,
                    height = excluded.height,
                    mass = excluded.mass,
                    msrp = excluded.msrp,
                    production_status = excluded.production_status,
                    description_de = excluded.description_de,
                    description_en = excluded.description_en,
                    thumbnail_url = excluded.thumbnail_url,
                    image_url = excluded.image_url,
                    web_url = excluded.web_url,
                    pledge_url = excluded.pledge_url,
                    specs_json = excluded.specs_json,
                    stores_json = excluded.stores_json,
                    updated_at = excluded.updated_at;
            ";

            info.Size = WikiApiClient.CleanLocalizedField(info.Size);
            info.Role = WikiApiClient.CleanLocalizedField(info.Role);
            info.Type = WikiApiClient.CleanLocalizedField(info.Type);
            info.Focus = WikiApiClient.CleanLocalizedField(info.Focus);
            info.ProductionStatus = WikiApiClient.CleanLocalizedField(info.ProductionStatus);

            if (info.Specs != null && info.Specs.Count > 0)
            {
                var cleanedSpecs = new Dictionary<string, string>();
                foreach (var kv in info.Specs)
                {
                    var val = kv.Value;
                    if (val.Contains("{") && val.Contains("\""))
                    {
                        var cleanInner = WikiApiClient.CleanLocalizedField(val.Replace("Größe ", ""));
                        val = int.TryParse(cleanInner, out _) || cleanInner.Length == 1 ? $"Größe {cleanInner}" : cleanInner;
                    }
                    cleanedSpecs[kv.Key] = val;
                }
                info.Specs = cleanedSpecs;
            }

            cmd.Parameters.AddWithValue("$name", info.Name.Trim());
            cmd.Parameters.AddWithValue("$mfg", info.Manufacturer ?? "");
            cmd.Parameters.AddWithValue("$role", info.Role ?? "");
            cmd.Parameters.AddWithValue("$type", info.Type ?? "");
            cmd.Parameters.AddWithValue("$focus", info.Focus ?? "");
            cmd.Parameters.AddWithValue("$size", info.Size ?? "");
            cmd.Parameters.AddWithValue("$crewMin", (object?)info.CrewMin ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$crewMax", (object?)info.CrewMax ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$cargoScu", (object?)info.CargoScu ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$qf", (object?)info.QuantumFuel ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$len", (object?)info.Length ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$beam", (object?)info.Beam ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$hgt", (object?)info.Height ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$mass", (object?)info.Mass ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$msrp", (object?)info.Msrp ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$status", info.ProductionStatus ?? "");
            cmd.Parameters.AddWithValue("$dde", info.DescriptionDe ?? "");
            cmd.Parameters.AddWithValue("$den", info.DescriptionEn ?? "");
            cmd.Parameters.AddWithValue("$thumb", info.ThumbnailUrl ?? "");
            cmd.Parameters.AddWithValue("$img", info.ImageUrl ?? "");
            cmd.Parameters.AddWithValue("$web", info.WebUrl ?? "");
            cmd.Parameters.AddWithValue("$pledge", info.PledgeUrl ?? "");
            cmd.Parameters.AddWithValue("$specs", info.Specs != null && info.Specs.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(info.Specs) : DBNull.Value);
            cmd.Parameters.AddWithValue("$stores", info.StoreLocations != null && info.StoreLocations.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(info.StoreLocations) : DBNull.Value);
            cmd.Parameters.AddWithValue("$updated", DateTime.UtcNow.ToString("o"));

            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Logger.Error("SaveCachedWikiVehicle", ex);
        }
    }

    public static List<WikiInfo> GetAllCachedWikiVehicles()
    {
        EnsureInitialized();
        var list = new List<WikiInfo>();
        try
        {
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = @"
                SELECT name, manufacturer, role, type, focus, size, crew_min, crew_max, 
                       cargo_scu, msrp, production_status, thumbnail_url, image_url, web_url 
                FROM wiki_vehicles_cache 
                ORDER BY name ASC;
            ";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new WikiInfo
                {
                    Name = reader.GetString(0),
                    Category = "Schiff & Fahrzeug",
                    Manufacturer = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Role = reader.IsDBNull(2) ? "" : WikiApiClient.CleanLocalizedField(reader.GetString(2)),
                    Type = reader.IsDBNull(3) ? "" : WikiApiClient.CleanLocalizedField(reader.GetString(3)),
                    Focus = reader.IsDBNull(4) ? "" : WikiApiClient.CleanLocalizedField(reader.GetString(4)),
                    Size = reader.IsDBNull(5) ? "" : WikiApiClient.CleanLocalizedField(reader.GetString(5)),
                    CrewMin = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    CrewMax = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    CargoScu = reader.IsDBNull(8) ? null : reader.GetDouble(8),
                    Msrp = reader.IsDBNull(9) ? null : reader.GetDouble(9),
                    ProductionStatus = reader.IsDBNull(10) ? "" : WikiApiClient.CleanLocalizedField(reader.GetString(10)),
                    ThumbnailUrl = reader.IsDBNull(11) ? "" : reader.GetString(11),
                    ImageUrl = reader.IsDBNull(12) ? "" : reader.GetString(12),
                    WebUrl = reader.IsDBNull(13) ? "" : reader.GetString(13)
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Error("GetAllCachedWikiVehicles", ex);
        }
        return list;
    }

    public static (string? pilot, string? shard, string? version) GetSessionMeta(string sessionName)
    {
        EnsureInitialized();
        try
        {
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "SELECT pilot, shard, version FROM sessions WHERE name = @name LIMIT 1;";
            cmd.Parameters.AddWithValue("@name", sessionName);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string? p = reader.IsDBNull(0) ? null : reader.GetString(0);
                string? s = reader.IsDBNull(1) ? null : reader.GetString(1);
                string? v = reader.IsDBNull(2) ? null : reader.GetString(2);
                return (p, s, v);
            }
        }
        catch { }
        return (null, null, null);
    }

    public static string? GetLatestPilotName()
    {
        EnsureInitialized();
        try
        {
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "SELECT pilot FROM sessions WHERE pilot IS NOT NULL AND pilot != '' AND pilot != '—' ORDER BY start DESC LIMIT 1;";
            var res = cmd.ExecuteScalar()?.ToString();
            if (!string.IsNullOrWhiteSpace(res) && res != "—") return res;
        }
        catch { }
        return null;
    }

    public static void UpdateSessionMeta(string sessionName, string? pilot, string? shard, string? version)
    {
        EnsureInitialized();
        try
        {
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = @"
                UPDATE sessions 
                SET pilot = COALESCE(@p, pilot), 
                    shard = COALESCE(@s, shard), 
                    version = COALESCE(@v, version) 
                WHERE name = @name;";
            cmd.Parameters.AddWithValue("@name", sessionName);
            cmd.Parameters.AddWithValue("@p", (object?)pilot ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@s", (object?)shard ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@v", (object?)version ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }
        catch { }
    }

    public static PilotProfile? GetPilotProfile(string handle)
    {
        EnsureInitialized();
        try
        {
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = @"
                SELECT handle, citizen_record, title, avatar_url, enlisted, fluency, 
                       org_name, org_sid, org_rank, org_logo_url, website, profile_url, bio, updated_at
                FROM pilot_profiles 
                WHERE handle = @h 
                LIMIT 1;
            ";
            cmd.Parameters.AddWithValue("@h", handle);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                DateTime.TryParse(reader.GetString(13), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var upd);
                return new PilotProfile
                {
                    Handle = reader.GetString(0),
                    CitizenRecord = reader.IsDBNull(1) ? "" : reader.GetString(1),
                    Title = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    AvatarUrl = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Enlisted = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Fluency = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    OrgName = reader.IsDBNull(6) ? "" : reader.GetString(6),
                    OrgSid = reader.IsDBNull(7) ? "" : reader.GetString(7),
                    OrgRank = reader.IsDBNull(8) ? "" : reader.GetString(8),
                    OrgLogoUrl = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    Website = reader.IsDBNull(10) ? "" : reader.GetString(10),
                    ProfileUrl = reader.IsDBNull(11) ? "" : reader.GetString(11),
                    Bio = reader.IsDBNull(12) ? "" : reader.GetString(12),
                    UpdatedAt = upd
                };
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Database.GetPilotProfile({handle})", ex);
        }
        return null;
    }

    public static void SavePilotProfile(PilotProfile profile)
    {
        EnsureInitialized();
        try
        {
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO pilot_profiles (handle, citizen_record, title, avatar_url, enlisted, fluency, 
                                           org_name, org_sid, org_rank, org_logo_url, website, profile_url, bio, updated_at)
                VALUES (@h, @cr, @ti, @av, @enl, @flu, @on, @os, @or, @ol, @ws, @pu, @bio, @upd)
                ON CONFLICT(handle) DO UPDATE SET
                    citizen_record = excluded.citizen_record,
                    title = excluded.title,
                    avatar_url = excluded.avatar_url,
                    enlisted = excluded.enlisted,
                    fluency = excluded.fluency,
                    org_name = excluded.org_name,
                    org_sid = excluded.org_sid,
                    org_rank = excluded.org_rank,
                    org_logo_url = excluded.org_logo_url,
                    website = excluded.website,
                    profile_url = excluded.profile_url,
                    bio = excluded.bio,
                    updated_at = excluded.updated_at;
            ";
            cmd.Parameters.AddWithValue("@h", profile.Handle);
            cmd.Parameters.AddWithValue("@cr", (object?)profile.CitizenRecord ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ti", (object?)profile.Title ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@av", (object?)profile.AvatarUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@enl", (object?)profile.Enlisted ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@flu", (object?)profile.Fluency ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@on", (object?)profile.OrgName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@os", (object?)profile.OrgSid ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@or", (object?)profile.OrgRank ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ol", (object?)profile.OrgLogoUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ws", (object?)profile.Website ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pu", (object?)profile.ProfileUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@bio", (object?)profile.Bio ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@upd", profile.UpdatedAt.ToString("o"));
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Logger.Error($"Database.SavePilotProfile({profile.Handle})", ex);
        }
    }

    #endregion

    #region Chat Messages (OCR & Chronik)

    public static long InsertChatMessage(ChatMessageDto msg)
    {
        EnsureInitialized();
        if (msg == null || string.IsNullOrWhiteSpace(msg.Message)) return 0;
        try
        {
            using var db = new SqliteConnection(Conn);
            db.Open();

            // Deduplizierung: Selbe Nachricht vom selben Sender innerhalb von 15 Sekunden überspringen
            using var checkCmd = db.CreateCommand();
            checkCmd.CommandText = @"
                SELECT id FROM chat_messages 
                WHERE sender = @s AND message = @m 
                ORDER BY id DESC LIMIT 1;
            ";
            checkCmd.Parameters.AddWithValue("@s", msg.Sender.Trim());
            checkCmd.Parameters.AddWithValue("@m", msg.Message.Trim());
            var existing = checkCmd.ExecuteScalar();
            if (existing != null && existing != DBNull.Value)
            {
                return Convert.ToInt64(existing);
            }

            using var cmd = db.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO chat_messages (
                    timestamp, session_id, channel, sender, recipient, 
                    message, raw_ocr, is_flagged, created_at
                ) VALUES (
                    @ts, @sid, @ch, @s, @r, @m, @raw, @flag, @cr
                );
                SELECT last_insert_rowid();
            ";
            cmd.Parameters.AddWithValue("@ts", string.IsNullOrEmpty(msg.Timestamp) ? DateTime.UtcNow.ToString("o") : msg.Timestamp);
            cmd.Parameters.AddWithValue("@sid", (object?)msg.SessionId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ch", string.IsNullOrEmpty(msg.Channel) ? "Global" : msg.Channel);
            cmd.Parameters.AddWithValue("@s", msg.Sender.Trim());
            cmd.Parameters.AddWithValue("@r", (object?)msg.Recipient ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@m", msg.Message.Trim());
            cmd.Parameters.AddWithValue("@raw", (object?)msg.RawOcr ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@flag", msg.IsFlagged ? 1 : 0);
            cmd.Parameters.AddWithValue("@cr", string.IsNullOrEmpty(msg.CreatedAt) ? DateTime.UtcNow.ToString("o") : msg.CreatedAt);

            var idObj = cmd.ExecuteScalar();
            return idObj != null ? Convert.ToInt64(idObj) : 0;
        }
        catch (Exception ex)
        {
            Logger.Error("Database.InsertChatMessage", ex);
            return 0;
        }
    }

    public static List<ChatMessageDto> GetChatMessages(
        string? sessionId = null,
        string? channel = null,
        string? sender = null,
        string? query = null,
        bool flaggedOnly = false,
        int limit = 250,
        int offset = 0)
    {
        EnsureInitialized();
        var list = new List<ChatMessageDto>();
        try
        {
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();

            var whereClauses = new List<string>();
            if (!string.IsNullOrWhiteSpace(sessionId) && sessionId != "ALL")
            {
                whereClauses.Add("session_id = @sid");
                cmd.Parameters.AddWithValue("@sid", sessionId.Trim());
            }
            if (!string.IsNullOrWhiteSpace(channel) && channel != "ALL")
            {
                whereClauses.Add("LOWER(channel) = LOWER(@ch)");
                cmd.Parameters.AddWithValue("@ch", channel.Trim());
            }
            if (!string.IsNullOrWhiteSpace(sender))
            {
                whereClauses.Add("LOWER(sender) LIKE @sender");
                cmd.Parameters.AddWithValue("@sender", $"%{sender.Trim().ToLowerInvariant()}%");
            }
            if (!string.IsNullOrWhiteSpace(query))
            {
                whereClauses.Add("(LOWER(message) LIKE @q OR LOWER(sender) LIKE @q)");
                cmd.Parameters.AddWithValue("@q", $"%{query.Trim().ToLowerInvariant()}%");
            }
            if (flaggedOnly)
            {
                whereClauses.Add("is_flagged = 1");
            }

            var where = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";
            cmd.CommandText = $@"
                SELECT id, timestamp, session_id, channel, sender, recipient, 
                       message, raw_ocr, is_flagged, created_at
                FROM chat_messages
                {where}
                ORDER BY id DESC
                LIMIT @lim OFFSET @off;
            ";
            cmd.Parameters.AddWithValue("@lim", Math.Clamp(limit, 1, 1000));
            cmd.Parameters.AddWithValue("@off", Math.Max(0, offset));

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ChatMessageDto
                {
                    Id = reader.GetInt64(0),
                    Timestamp = reader.GetString(1),
                    SessionId = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Channel = reader.GetString(3),
                    Sender = reader.GetString(4),
                    Recipient = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Message = reader.GetString(6),
                    RawOcr = reader.IsDBNull(7) ? null : reader.GetString(7),
                    IsFlagged = reader.GetInt32(8) == 1,
                    CreatedAt = reader.GetString(9)
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Database.GetChatMessages", ex);
        }
        return list;
    }

    public static bool FlagChatMessage(long id, bool isFlagged)
    {
        EnsureInitialized();
        try
        {
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "UPDATE chat_messages SET is_flagged = @flag WHERE id = @id;";
            cmd.Parameters.AddWithValue("@flag", isFlagged ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (Exception ex)
        {
            Logger.Error("Database.FlagChatMessage", ex);
            return false;
        }
    }

    public static bool ClearChatMessages(string? sessionId = null)
    {
        EnsureInitialized();
        try
        {
            using var db = new SqliteConnection(Conn);
            db.Open();
            using var cmd = db.CreateCommand();
            if (!string.IsNullOrWhiteSpace(sessionId) && sessionId != "ALL")
            {
                cmd.CommandText = "DELETE FROM chat_messages WHERE session_id = @sid;";
                cmd.Parameters.AddWithValue("@sid", sessionId.Trim());
            }
            else
            {
                cmd.CommandText = "DELETE FROM chat_messages;";
            }
            return cmd.ExecuteNonQuery() >= 0;
        }
        catch (Exception ex)
        {
            Logger.Error("Database.ClearChatMessages", ex);
            return false;
        }
    }

    #endregion
}
