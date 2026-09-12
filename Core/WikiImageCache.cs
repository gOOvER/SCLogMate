using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SCLogMate.Core;

/// <summary>
/// Lokaler Disk-Cache für Star-Citizen-Wiki HD-Render und Thumbnails.
/// Pfad: %APPDATA%\SCLogMate\cache\wiki\images\{hash}.webp (oder Original-Endung)
/// Stellt Bilder auch als Base64 Data-URI für die verzögerungsfreie Offline-Darstellung im WebView2 bereit.
/// </summary>
public static class WikiImageCache
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private static string CacheDir => Path.Combine(Settings.Dir, "cache", "wiki", "images");

    static WikiImageCache()
    {
        Http.DefaultRequestHeaders.Add("User-Agent", "SCLogMate/1.0.0 (+https://github.com/gOOvER/SCLogMate)");
    }

    /// <summary>
    /// Ermittelt den lokalen Cache-Dateipfad für eine gegebene Remote-URL.
    /// </summary>
    public static string GetLocalCachePath(string remoteUrl)
    {
        if (string.IsNullOrWhiteSpace(remoteUrl)) return "";
        var hash = ComputeHash(remoteUrl.Trim());
        var ext = Path.GetExtension(new Uri(remoteUrl).AbsolutePath);
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 5) ext = ".webp";
        return Path.Combine(CacheDir, $"{hash}{ext}");
    }

    /// <summary>
    /// Lädt ein Bild bei Bedarf herunter, speichert es im lokalen Disk-Cache
    /// und gibt eine Base64 Data-URI ("data:image/...;base64,...") zurück.
    /// Falls offline oder Download fehlschlägt, wird die Remote-URL als Fallback zurückgegeben.
    /// </summary>
    public static async Task<string> GetImageAsDataUriAsync(string? remoteUrl)
    {
        if (string.IsNullOrWhiteSpace(remoteUrl)) return "";

        try
        {
            var localPath = GetLocalCachePath(remoteUrl);
            if (File.Exists(localPath))
            {
                var bytes = await File.ReadAllBytesAsync(localPath);
                if (bytes.Length > 0)
                {
                    var mime = GetMimeType(localPath);
                    return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
                }
            }

            // Wenn noch nicht auf Disk vorhanden: Im Hintergrund oder synchron herunterladen
            Directory.CreateDirectory(CacheDir);
            var downloadedBytes = await Http.GetByteArrayAsync(remoteUrl);
            if (downloadedBytes.Length > 0)
            {
                await File.WriteAllBytesAsync(localPath, downloadedBytes);
                var mime = GetMimeType(localPath);
                return $"data:{mime};base64,{Convert.ToBase64String(downloadedBytes)}";
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"WikiImageCache: Bild-Download fehlgeschlagen ({remoteUrl}): {ex.Message}");
        }

        // Fallback auf Remote URL
        return remoteUrl;
    }

    /// <summary>
    /// Lädt ein Bild asynchron in den Disk-Cache ohne UI-Blockade.
    /// </summary>
    public static void PrefetchImage(string? remoteUrl)
    {
        if (string.IsNullOrWhiteSpace(remoteUrl)) return;
        _ = Task.Run(async () =>
        {
            try
            {
                var localPath = GetLocalCachePath(remoteUrl);
                if (!File.Exists(localPath))
                {
                    Directory.CreateDirectory(CacheDir);
                    var bytes = await Http.GetByteArrayAsync(remoteUrl);
                    if (bytes.Length > 0)
                    {
                        await File.WriteAllBytesAsync(localPath, bytes);
                    }
                }
            }
            catch { }
        });
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString()[..24]; // 24 Zeichen Hash reicht vollkommen
    }

    private static string GetMimeType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            _ => "image/webp"
        };
    }
}
