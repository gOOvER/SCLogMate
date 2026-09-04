using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SCLogMate.Core;

public class KeybindBackupInfo
{
    public string Name { get; set; } = "";
    public string FolderPath { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int FileCount { get; set; }
    public string LocationType { get; set; } = "Lokal"; // "Lokal", "Cloud", "Lokal + Cloud"
    public string SizeFormatted { get; set; } = "";
}

public class ConfigBackupInfo
{
    public string Name { get; set; } = "";
    public string FilePath { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string LocationType { get; set; } = "Lokal"; // "Lokal", "Cloud", "Lokal + Cloud"
    public string SizeFormatted { get; set; } = "";
}

public class SystemDiagnosticInfo
{
    public double TotalRamGb { get; set; }
    public string RamStatus { get; set; } = "Unbekannt";
    public bool RamOk { get; set; }
    public string DriveName { get; set; } = "";
    public string DriveType { get; set; } = "SSD / Festplatte";
    public double FreeDiskGb { get; set; }
    public string PagefileStatus { get; set; } = "Aktiv";
    public bool PagefileOk { get; set; } = true;
    public string StarCitizenVersion { get; set; } = "Unbekannt";
}

public static class MaintenanceService
{
    public static string LocalKeybindsBackupDir => Path.Combine(Settings.Dir, "keybind_backups");
    public static string LocalConfigBackupDir => Path.Combine(Settings.Dir, "config_backups");

    public static string GetStarCitizenLocalAppDataDir()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(local, "star citizen");
    }

    public static List<string> GetShaderDirectories()
    {
        var dirs = new List<string>();
        var scLocal = GetStarCitizenLocalAppDataDir();
        if (!Directory.Exists(scLocal)) return dirs;

        try
        {
            foreach (var sub in Directory.GetDirectories(scLocal))
            {
                var s1 = Path.Combine(sub, "Shaders");
                if (Directory.Exists(s1)) dirs.Add(s1);

                var s2 = Path.Combine(sub, "vulkanshadercache");
                if (Directory.Exists(s2)) dirs.Add(s2);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("GetShaderDirectories", ex);
        }
        return dirs;
    }

    public static string GetCrashDirectory()
    {
        return Path.Combine(GetStarCitizenLocalAppDataDir(), "crashes");
    }

    public static double GetShaderCacheSizeMb()
    {
        long totalBytes = 0;
        foreach (var dir in GetShaderDirectories())
        {
            try
            {
                var di = new DirectoryInfo(dir);
                totalBytes += di.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
            }
            catch { }
        }
        return Math.Round((double)totalBytes / (1024 * 1024), 1);
    }

    public static (bool success, double freedMb, string message) CleanShaderCache()
    {
        double initialMb = GetShaderCacheSizeMb();
        int deletedCount = 0;

        foreach (var dir in GetShaderDirectories())
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                    deletedCount++;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"CleanShaderCache for {dir}", ex);
            }
        }

        double finalMb = GetShaderCacheSizeMb();
        double freed = Math.Max(0, initialMb - finalMb);
        return (true, freed, $"✓ Shader-Cache bereinigt: {freed:F1} MB freigegeben ({deletedCount} Cache-Ordner geleert).");
    }

    public static double GetCrashDumpsSizeMb()
    {
        var crashDir = GetCrashDirectory();
        if (!Directory.Exists(crashDir)) return 0;

        try
        {
            var di = new DirectoryInfo(crashDir);
            long totalBytes = di.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
            return Math.Round((double)totalBytes / (1024 * 1024), 1);
        }
        catch
        {
            return 0;
        }
    }

    public static (bool success, double freedMb, string message) CleanCrashDumps()
    {
        double initialMb = GetCrashDumpsSizeMb();
        var crashDir = GetCrashDirectory();
        if (!Directory.Exists(crashDir)) return (true, 0, "Keine Crash-Dumps vorhanden.");

        int deletedFiles = 0;
        try
        {
            var di = new DirectoryInfo(crashDir);
            foreach (var f in di.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                try
                {
                    f.Delete();
                    deletedFiles++;
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("CleanCrashDumps", ex);
        }

        double finalMb = GetCrashDumpsSizeMb();
        double freed = Math.Max(0, initialMb - finalMb);
        return (true, freed, $"✓ Crash-Dumps bereinigt: {freed:F1} MB freigegeben ({deletedFiles} Dateien entfernt).");
    }

    // ══ KEYBINDS BACKUP & RESTORE ══

    public static (string? liveDir, string? userClientDir, string? mappingsDir, string? actionMapsFile) ResolveKeybindPaths(string? logPath)
    {
        if (string.IsNullOrEmpty(logPath)) return (null, null, null, null);
        var liveDir = Path.GetDirectoryName(logPath);
        if (string.IsNullOrEmpty(liveDir)) return (null, null, null, null);

        var userClient = Path.Combine(liveDir, "user", "client", "0");
        var mappings = Path.Combine(userClient, "controls", "mappings");
        var actionMaps = Path.Combine(userClient, "Profiles", "default", "actionmaps.xml");

        return (liveDir, userClient, mappings, actionMaps);
    }

    public static (bool success, string message, int filesCount) BackupKeybinds(string? logPath, string? cloudPath = null, string? customNote = null)
    {
        var (_, _, mappingsDir, actionMapsFile) = ResolveKeybindPaths(logPath);
        var filesToBackup = new List<string>();

        if (!string.IsNullOrEmpty(actionMapsFile) && File.Exists(actionMapsFile))
        {
            filesToBackup.Add(actionMapsFile);
        }

        if (!string.IsNullOrEmpty(mappingsDir) && Directory.Exists(mappingsDir))
        {
            filesToBackup.AddRange(Directory.GetFiles(mappingsDir, "*.xml"));
        }

        if (filesToBackup.Count == 0)
        {
            return (false, "Keine Steuerungs-Dateien (actionmaps.xml / Mappings) im Spielordner gefunden.", 0);
        }

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var folderName = string.IsNullOrWhiteSpace(customNote) 
            ? $"Keybinds_{timestamp}" 
            : $"Keybinds_{timestamp}_{SanitizeFileName(customNote)}";

        var localTarget = Path.Combine(LocalKeybindsBackupDir, folderName);
        Directory.CreateDirectory(localTarget);

        foreach (var f in filesToBackup)
        {
            var isActionMaps = Path.GetFileName(f).Equals("actionmaps.xml", StringComparison.OrdinalIgnoreCase);
            var destName = isActionMaps ? "actionmaps.xml" : Path.GetFileName(f);
            File.Copy(f, Path.Combine(localTarget, destName), overwrite: true);
        }

        bool cloudSuccess = false;
        if (!string.IsNullOrWhiteSpace(cloudPath) && Directory.Exists(cloudPath))
        {
            try
            {
                var cloudTarget = Path.Combine(cloudPath, "SCLogMate", "Keybinds", folderName);
                Directory.CreateDirectory(cloudTarget);
                foreach (var f in Directory.GetFiles(localTarget))
                {
                    File.Copy(f, Path.Combine(cloudTarget, Path.GetFileName(f)), overwrite: true);
                }
                cloudSuccess = true;
            }
            catch (Exception ex)
            {
                Logger.Error("Cloud keybind backup copy", ex);
            }
        }

        string locText = cloudSuccess ? "Lokal & in der Cloud" : "Lokal";
        return (true, $"✓ {filesToBackup.Count} Steuerungsdateien gesichert ({locText}): {folderName}", filesToBackup.Count);
    }

    public static List<KeybindBackupInfo> ListKeybindBackups(string? cloudPath = null)
    {
        var list = new Dictionary<string, KeybindBackupInfo>(StringComparer.OrdinalIgnoreCase);

        // 1. Lokale Backups
        if (Directory.Exists(LocalKeybindsBackupDir))
        {
            foreach (var dir in Directory.GetDirectories(LocalKeybindsBackupDir))
            {
                var name = Path.GetFileName(dir);
                var di = new DirectoryInfo(dir);
                var files = di.GetFiles("*.xml");
                long size = files.Sum(f => f.Length);

                list[name] = new KeybindBackupInfo
                {
                    Name = name,
                    FolderPath = dir,
                    CreatedAt = di.CreationTime,
                    FileCount = files.Length,
                    LocationType = "Lokal",
                    SizeFormatted = FormatFileSize(size)
                };
            }
        }

        // 2. Cloud Backups
        if (!string.IsNullOrWhiteSpace(cloudPath) && Directory.Exists(cloudPath))
        {
            var cloudKeybinds = Path.Combine(cloudPath, "SCLogMate", "Keybinds");
            if (Directory.Exists(cloudKeybinds))
            {
                foreach (var dir in Directory.GetDirectories(cloudKeybinds))
                {
                    var name = Path.GetFileName(dir);
                    var di = new DirectoryInfo(dir);
                    var files = di.GetFiles("*.xml");
                    long size = files.Sum(f => f.Length);

                    if (list.TryGetValue(name, out var existing))
                    {
                        existing.LocationType = "Lokal + Cloud";
                    }
                    else
                    {
                        list[name] = new KeybindBackupInfo
                        {
                            Name = name,
                            FolderPath = dir,
                            CreatedAt = di.CreationTime,
                            FileCount = files.Length,
                            LocationType = "Cloud",
                            SizeFormatted = FormatFileSize(size)
                        };
                    }
                }
            }
        }

        return list.Values.OrderByDescending(b => b.CreatedAt).ToList();
    }

    public static (bool success, string message) RestoreKeybinds(KeybindBackupInfo backup, string? logPath)
    {
        if (!Directory.Exists(backup.FolderPath))
        {
            return (false, "Der Sicherungsordner existiert nicht mehr.");
        }

        var (_, userClientDir, mappingsDir, actionMapsFile) = ResolveKeybindPaths(logPath);
        if (string.IsNullOrEmpty(userClientDir) || string.IsNullOrEmpty(mappingsDir) || string.IsNullOrEmpty(actionMapsFile))
        {
            return (false, "Star Citizen LIVE/USER Verzeichnis konnte nicht ermittelt werden.");
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(actionMapsFile)!);
            Directory.CreateDirectory(mappingsDir);

            int restoredCount = 0;
            foreach (var f in Directory.GetFiles(backup.FolderPath, "*.xml"))
            {
                var fn = Path.GetFileName(f);
                if (fn.Equals("actionmaps.xml", StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(f, actionMapsFile, overwrite: true);
                    restoredCount++;
                }
                else
                {
                    File.Copy(f, Path.Combine(mappingsDir, fn), overwrite: true);
                    restoredCount++;
                }
            }

            return (true, $"✓ {restoredCount} Steuerungsdateien aus '{backup.Name}' erfolgreich in Star Citizen wiederhergestellt!");
        }
        catch (Exception ex)
        {
            Logger.Error("RestoreKeybinds", ex);
            return (false, $"Fehler bei der Wiederherstellung: {ex.Message}");
        }
    }

    // ══ LOG BACKUPS & CLOUD SYNC ══

    public static (bool success, string message, string? zipPath) ExportLogsToZip(string destinationFolder, string? logPath = null)
    {
        try
        {
            Directory.CreateDirectory(destinationFolder);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var zipName = $"StarCitizen_Logs_{timestamp}.zip";
            var zipPath = Path.Combine(destinationFolder, zipName);

            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Aus SCLogMate Archiv
            if (Directory.Exists(LogArchive.Dir))
            {
                foreach (var f in Directory.GetFiles(LogArchive.Dir, "*.log")) files.Add(f);
            }

            // Aus aktuellem logbackups Ordner
            if (!string.IsNullOrEmpty(logPath))
            {
                var liveDir = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(liveDir))
                {
                    var backups = Path.Combine(liveDir, "logbackups");
                    if (Directory.Exists(backups))
                    {
                        foreach (var f in Directory.GetFiles(backups, "*.log")) files.Add(f);
                    }
                }
            }

            if (files.Count == 0)
            {
                return (false, "Keine Logdateien zum Exportieren gefunden.", null);
            }

            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var f in files)
                {
                    archive.CreateEntryFromFile(f, Path.GetFileName(f), CompressionLevel.Optimal);
                }
            }

            return (true, $"✓ {files.Count} Logdateien erfolgreich in ZIP exportiert: {zipName}", zipPath);
        }
        catch (Exception ex)
        {
            Logger.Error("ExportLogsToZip", ex);
            return (false, $"Fehler beim ZIP-Export: {ex.Message}", null);
        }
    }

    public static (bool success, string message, int syncedCount) SyncLogsToCloud(string cloudPath, string? logPath = null)
    {
        if (string.IsNullOrWhiteSpace(cloudPath) || !Directory.Exists(cloudPath))
        {
            return (false, "Ungültiger oder nicht erreichbarer Cloud-Speicherpfad.", 0);
        }

        try
        {
            var targetDir = Path.Combine(cloudPath, "SCLogMate", "Logs");
            Directory.CreateDirectory(targetDir);

            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(LogArchive.Dir))
            {
                foreach (var f in Directory.GetFiles(LogArchive.Dir, "*.log")) files.Add(f);
            }

            if (!string.IsNullOrEmpty(logPath))
            {
                var liveDir = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(liveDir))
                {
                    var backups = Path.Combine(liveDir, "logbackups");
                    if (Directory.Exists(backups))
                    {
                        foreach (var f in Directory.GetFiles(backups, "*.log")) files.Add(f);
                    }
                }
            }

            int copied = 0;
            foreach (var f in files)
            {
                var dest = Path.Combine(targetDir, Path.GetFileName(f));
                if (!File.Exists(dest) || new FileInfo(f).Length > new FileInfo(dest).Length)
                {
                    File.Copy(f, dest, overwrite: true);
                    copied++;
                }
            }

            return (true, $"✓ Cloud-Sync abgeschlossen: {copied} neue/aktualisierte Logs nach '{targetDir}' synchronisiert.", copied);
        }
        catch (Exception ex)
        {
            Logger.Error("SyncLogsToCloud", ex);
            return (false, $"Fehler beim Cloud-Sync: {ex.Message}", 0);
        }
    }

    // ══ USER.CFG TUNING ══

    public static string GetUserCfgPath(string? logPath)
    {
        if (string.IsNullOrEmpty(logPath)) return "";
        var liveDir = Path.GetDirectoryName(logPath);
        return string.IsNullOrEmpty(liveDir) ? "" : Path.Combine(liveDir, "user.cfg");
    }

    public static (bool exists, string content) ReadUserCfg(string? logPath)
    {
        var path = GetUserCfgPath(logPath);
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return (false, "");
        }

        try
        {
            return (true, File.ReadAllText(path, Encoding.UTF8));
        }
        catch (Exception ex)
        {
            Logger.Error("ReadUserCfg", ex);
            return (false, "");
        }
    }

    public static (bool success, string message, string? backupFile) BackupUserCfg(string? logPath, string? cloudPath = null, string? customNote = null)
    {
        var path = GetUserCfgPath(logPath);
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return (false, "Keine user.cfg im Spielordner zum Archivieren vorhanden.", null);
        }

        try
        {
            Directory.CreateDirectory(LocalConfigBackupDir);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var name = string.IsNullOrWhiteSpace(customNote)
                ? $"user_cfg_{timestamp}.cfg"
                : $"user_cfg_{timestamp}_{SanitizeFileName(customNote)}.cfg";

            var localDest = Path.Combine(LocalConfigBackupDir, name);
            File.Copy(path, localDest, overwrite: true);

            // Auch direkt im LIVE als .bak vorhalten
            File.Copy(path, path + ".bak", overwrite: true);

            bool cloudOk = false;
            if (!string.IsNullOrWhiteSpace(cloudPath) && Directory.Exists(cloudPath))
            {
                try
                {
                    var cloudDir = Path.Combine(cloudPath, "SCLogMate", "Config");
                    Directory.CreateDirectory(cloudDir);
                    File.Copy(path, Path.Combine(cloudDir, name), overwrite: true);
                    cloudOk = true;
                }
                catch (Exception ex)
                {
                    Logger.Error("Cloud config backup", ex);
                }
            }

            var loc = cloudOk ? "Lokal & Cloud" : "Lokal";
            return (true, $"✓ user.cfg archiviert ({loc}): {name}", localDest);
        }
        catch (Exception ex)
        {
            Logger.Error("BackupUserCfg", ex);
            return (false, $"Fehler bei der Archivierung: {ex.Message}", null);
        }
    }

    public static List<ConfigBackupInfo> ListConfigBackups(string? cloudPath = null)
    {
        var list = new Dictionary<string, ConfigBackupInfo>(StringComparer.OrdinalIgnoreCase);

        // 1. Lokal
        if (Directory.Exists(LocalConfigBackupDir))
        {
            foreach (var f in Directory.GetFiles(LocalConfigBackupDir, "*.cfg"))
            {
                var name = Path.GetFileName(f);
                var fi = new FileInfo(f);
                list[name] = new ConfigBackupInfo
                {
                    Name = name,
                    FilePath = f,
                    CreatedAt = fi.CreationTime,
                    LocationType = "Lokal",
                    SizeFormatted = FormatFileSize(fi.Length)
                };
            }
        }

        // 2. Cloud
        if (!string.IsNullOrWhiteSpace(cloudPath) && Directory.Exists(cloudPath))
        {
            var cloudDir = Path.Combine(cloudPath, "SCLogMate", "Config");
            if (Directory.Exists(cloudDir))
            {
                foreach (var f in Directory.GetFiles(cloudDir, "*.cfg"))
                {
                    var name = Path.GetFileName(f);
                    var fi = new FileInfo(f);
                    if (list.TryGetValue(name, out var existing))
                    {
                        existing.LocationType = "Lokal + Cloud";
                    }
                    else
                    {
                        list[name] = new ConfigBackupInfo
                        {
                            Name = name,
                            FilePath = f,
                            CreatedAt = fi.CreationTime,
                            LocationType = "Cloud",
                            SizeFormatted = FormatFileSize(fi.Length)
                        };
                    }
                }
            }
        }

        return list.Values.OrderByDescending(b => b.CreatedAt).ToList();
    }

    public static (bool success, string message) RestoreConfigBackup(ConfigBackupInfo backup, string? logPath)
    {
        if (!File.Exists(backup.FilePath))
        {
            return (false, "Die ausgewählte Backup-Datei existiert nicht mehr.");
        }

        var path = GetUserCfgPath(logPath);
        if (string.IsNullOrEmpty(path))
        {
            return (false, "Star Citizen LIVE Ordner nicht gefunden.");
        }

        try
        {
            // Vor dem Wiederherstellen den aktuellen Stand sichern, falls vorhanden
            if (File.Exists(path))
            {
                File.Copy(path, path + ".bak", overwrite: true);
            }

            File.Copy(backup.FilePath, path, overwrite: true);
            return (true, $"✓ user.cfg aus Archiv '{backup.Name}' erfolgreich wiederhergestellt!");
        }
        catch (Exception ex)
        {
            Logger.Error("RestoreConfigBackup", ex);
            return (false, $"Fehler beim Wiederherstellen: {ex.Message}");
        }
    }

    public static (bool success, string message) SaveUserCfg(string? logPath, string content, string? cloudPath = null)
    {
        var path = GetUserCfgPath(logPath);
        if (string.IsNullOrEmpty(path))
        {
            return (false, "Pfad zur user.cfg konnte nicht ermittelt werden (Game.log Pfad prüfen).");
        }

        try
        {
            // Vor jeder Änderung ein vollständiges Archiv-Backup (lokal + optional Cloud) anlegen!
            if (File.Exists(path))
            {
                BackupUserCfg(logPath, cloudPath, "AutoBackup");
            }

            File.WriteAllText(path, content, new UTF8Encoding(false));
            return (true, "✓ user.cfg erfolgreich gespeichert (Archiv-Backup lokal & in Cloud gesichert).");
        }
        catch (Exception ex)
        {
            Logger.Error("SaveUserCfg", ex);
            return (false, $"Fehler beim Speichern der user.cfg: {ex.Message}");
        }
    }

    // ══ SYSTEM-DIAGNOSE ══

    public static SystemDiagnosticInfo GetSystemDiagnostics(string? logPath)
    {
        var info = new SystemDiagnosticInfo();

        // 1. RAM Ermittlung
        try
        {
            var gcMem = GC.GetGCMemoryInfo();
            info.TotalRamGb = Math.Round((double)gcMem.TotalAvailableMemoryBytes / (1024 * 1024 * 1024), 1);
            if (info.TotalRamGb >= 30)
            {
                info.RamStatus = $"{info.TotalRamGb:F0} GB (Optimal für Star Citizen)";
                info.RamOk = true;
            }
            else if (info.TotalRamGb >= 15)
            {
                info.RamStatus = $"{info.TotalRamGb:F0} GB (Minimum – 32 GB empfohlen)";
                info.RamOk = true;
            }
            else
            {
                info.RamStatus = $"{info.TotalRamGb:F0} GB (Kritisch zu wenig!)";
                info.RamOk = false;
            }
        }
        catch
        {
            info.RamStatus = "Nicht ermittelbar";
        }

        // 2. Laufwerk der Installation
        if (!string.IsNullOrEmpty(logPath))
        {
            try
            {
                var root = Path.GetPathRoot(logPath);
                if (!string.IsNullOrEmpty(root))
                {
                    var drive = new DriveInfo(root);
                    info.DriveName = drive.Name;
                    info.FreeDiskGb = Math.Round((double)drive.AvailableFreeSpace / (1024 * 1024 * 1024), 1);
                    info.DriveType = drive.DriveType == DriveType.Fixed ? "Interne SSD / NVMe" : drive.DriveType.ToString();
                }
            }
            catch { }
        }

        return info;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
}
