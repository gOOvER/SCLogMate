using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace SCLogReader.Core;

/// <summary>
/// Kopiert fertige Backup-Logs einmalig in ein eigenes Archiv
/// (%AppData%\SCLogReader\archive). Damit bleiben sie erhalten, auch wenn
/// SC seine Backups löscht – Grundlage zum späteren Neu-Parsen.
/// </summary>
public static class LogArchive
{
    public static string Dir => Path.Combine(Settings.Dir, "archive");

    /// <summary>Kopiert neue Backups ins Archiv. Gibt ALLE Archiv-Logpfade zurück.</summary>
    public static List<string> Sync(IEnumerable<string> backupFiles)
    {
        Directory.CreateDirectory(Dir);
        foreach (var f in backupFiles)
        {
            try
            {
                var dest = Path.Combine(Dir, Path.GetFileName(f));
                if (File.Exists(dest) && !FilesMatch(f, dest))
                {
                    var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(f)))[..16];
                    dest = Path.Combine(Dir, $"{Path.GetFileNameWithoutExtension(f)}_{hash}{Path.GetExtension(f)}");
                }
                if (!File.Exists(dest)) File.Copy(f, dest);
            }
            catch (Exception ex) { Logger.Error("Archive copy " + f, ex); }
        }
        return Directory.GetFiles(Dir, "*.log").ToList();
    }

    private static bool FilesMatch(string source, string destination)
    {
        var sourceInfo = new FileInfo(source);
        var destinationInfo = new FileInfo(destination);
        if (sourceInfo.Length != destinationInfo.Length) return false;

        return SHA256.HashData(File.ReadAllBytes(source))
            .SequenceEqual(SHA256.HashData(File.ReadAllBytes(destination)));
    }
}
