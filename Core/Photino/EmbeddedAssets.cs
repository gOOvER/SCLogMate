using System;
using System.IO;
using System.Reflection;
using SCLogMate.Core;

namespace SCLogMate.Core.Photino;

public static class EmbeddedAssets
{
    private static readonly string AppDataWwwroot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SCLogMate",
        "wwwroot"
    );

    /// <summary>
    /// Ermittelt den Pfad zur index.html für WebView2/Photino.
    /// 1. Prüft zuerst, ob ein lokaler wwwroot-Entwicklungsordner existiert (Dev-Override).
    /// 2. Falls nicht vorhanden, werden die im Executable eingebetteten Frontend-Assets
    ///    automatisch nach %APPDATA%/SCLogMate/wwwroot/ extrahiert und von dort geladen.
    /// Dadurch funktioniert SCLogMate als 100% autarke Single-File-EXE ohne wwwroot-Begleitordner.
    /// </summary>
    public static string EnsureIndexHtml()
    {
        // 1. Lokale Entwicklungs-/Override-Pfade prüfen
        var localCandidates = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "index.html"),
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html"),
            Path.Combine(Environment.CurrentDirectory, "wwwroot", "index.html"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "wwwroot", "index.html"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "wwwroot", "index.html")
        };

        foreach (var candidate in localCandidates)
        {
            if (File.Exists(candidate))
            {
                Logger.Log($"EmbeddedAssets: Lokales Frontend gefunden unter {candidate}");
                return Path.GetFullPath(candidate);
            }
        }

        // 2. Aus Embedded Resources nach %APPDATA%/SCLogMate/wwwroot extrahieren
        try
        {
            ExtractEmbeddedAssets(AppDataWwwroot);
            var extractedIndex = Path.Combine(AppDataWwwroot, "index.html");
            if (File.Exists(extractedIndex))
            {
                Logger.Log($"EmbeddedAssets: Eingebettetes Frontend bereitgestellt unter {extractedIndex}");
                return extractedIndex;
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"EmbeddedAssets: Fehler beim Extrahieren der Frontend-Assets: {ex.Message}");
        }

        return Path.Combine(AppDataWwwroot, "index.html");
    }

    private static void ExtractEmbeddedAssets(string targetDir)
    {
        var asm = Assembly.GetExecutingAssembly();
        var resourceNames = asm.GetManifestResourceNames();

        Directory.CreateDirectory(targetDir);

        foreach (var resName in resourceNames)
        {
            string? relativePath = null;
            var normalizedName = resName.Replace('\\', '/');

            if (normalizedName.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase))
            {
                relativePath = normalizedName.Substring("wwwroot/".Length);
            }
            else if (resName.StartsWith("SCLogMate.wwwroot.", StringComparison.OrdinalIgnoreCase))
            {
                relativePath = MapDefaultResourceNameToPath(resName);
            }

            if (string.IsNullOrWhiteSpace(relativePath))
                continue;

            var destPath = Path.Combine(targetDir, relativePath.Replace('/', Path.DirectorySeparatorChar));
            var destFolder = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destFolder))
            {
                Directory.CreateDirectory(destFolder);
            }

            using var stream = asm.GetManifestResourceStream(resName);
            if (stream == null) continue;

            // index.html immer frisch überschreiben, um veraltete Bundle-Hashes zu vermeiden
            if (!relativePath.Equals("index.html", StringComparison.OrdinalIgnoreCase) &&
                File.Exists(destPath) && new FileInfo(destPath).Length == stream.Length)
            {
                continue;
            }

            using var fileStream = File.Create(destPath);
            stream.CopyTo(fileStream);
        }
    }

    private static string MapDefaultResourceNameToPath(string resName)
    {
        // Beispiel: "SCLogMate.wwwroot.assets.index-BQFsUAeT.js" -> "assets/index-BQFsUAeT.js"
        // Beispiel: "SCLogMate.wwwroot.index.html" -> "index.html"
        var stripped = resName.Substring("SCLogMate.wwwroot.".Length);
        var lastDot = stripped.LastIndexOf('.');
        if (lastDot <= 0) return stripped;

        var ext = stripped.Substring(lastDot);
        var namePart = stripped.Substring(0, lastDot);

        if (namePart.StartsWith("assets.", StringComparison.OrdinalIgnoreCase))
        {
            return "assets/" + namePart.Substring("assets.".Length) + ext;
        }

        return namePart + ext;
    }
}
