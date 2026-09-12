using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SCLogMate.Core;

/// <summary>
/// Kopiert fertige Backup-Logs einmalig in ein eigenes Archiv
/// (%AppData%\SCLogMate\archive). Damit bleiben sie erhalten, auch wenn
/// SC seine Backups löscht – Grundlage zum späteren Neu-Parsen.
/// </summary>
public static class LogArchive
{
    public static string Dir => Path.Combine(Settings.Dir, "archive");

    /// <summary>Kopiert neue Backups ins Archiv. Gibt ALLE Archiv-Logpfade zurück.</summary>
    public static List<string> Sync(IEnumerable<string> backupFiles)
    {
        Directory.CreateDirectory(Dir);
        string? cloudTargetDir = null;
        try
        {
            var s = Settings.Load();
            if (s.AutoCloudSyncEnabled)
            {
                var effectiveCloud = MaintenanceService.GetEffectiveCloudPath(s.CloudStoragePath);
                if (!string.IsNullOrWhiteSpace(effectiveCloud) && Directory.Exists(effectiveCloud))
                {
                    cloudTargetDir = Path.Combine(effectiveCloud, "SCLogMate", "Logs");
                    Directory.CreateDirectory(cloudTargetDir);
                }
            }
        }
        catch { }

        foreach (var f in backupFiles)
        {
            try
            {
                var dest = Path.Combine(Dir, Path.GetFileName(f));
                bool copiedOrUpdated = false;

                if (File.Exists(dest))
                {
                    var sourceInfo = new FileInfo(f);
                    var destInfo = new FileInfo(dest);
                    if (sourceInfo.Length > destInfo.Length)
                    {
                        File.Copy(f, dest, overwrite: true);
                        copiedOrUpdated = true;
                    }
                }
                else
                {
                    File.Copy(f, dest);
                    copiedOrUpdated = true;
                }

                // Automatische Cloud-Replikation
                if (copiedOrUpdated && !string.IsNullOrEmpty(cloudTargetDir))
                {
                    try
                    {
                        var cloudDest = Path.Combine(cloudTargetDir, Path.GetFileName(f));
                        File.Copy(f, cloudDest, overwrite: true);
                    }
                    catch (Exception exCloud)
                    {
                        Logger.Error("Cloud auto replicate " + f, exCloud);
                    }
                }
            }
            catch (Exception ex) { Logger.Error("Archive copy " + f, ex); }
        }
        return Directory.GetFiles(Dir, "*.log").ToList();
    }
}

