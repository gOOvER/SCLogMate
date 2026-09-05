using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using SCLogMate.Core;

namespace SCLogMate.Services;

/// <summary>
/// Lädt Bilder asynchron aus dem Web (mit In-Memory &amp; Disk-Caching) für die Wiki- und Schiffsanzeigen.
/// </summary>
public static class ImageLoaderService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };
    private static readonly ConcurrentDictionary<string, Bitmap> MemCache = new();
    private static readonly string CacheDir = Path.Combine(
        Settings.Dir, "image_cache");
    private const long MaxCacheBytes = 128L * 1024 * 1024;

    static ImageLoaderService()
    {
        try { Directory.CreateDirectory(CacheDir); } catch { /* ignore */ }
    }

    public static async Task<Bitmap?> LoadBitmapAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        if (MemCache.TryGetValue(url, out var cached)) return cached;

        var fileName = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(url))) + ".img";
        var localPath = Path.Combine(CacheDir, fileName);

        // 1. Aus Disk-Cache laden
        if (File.Exists(localPath))
        {
            try
            {
                using var fs = File.OpenRead(localPath);
                var bmp = new Bitmap(fs);
                File.SetLastAccessTimeUtc(localPath, DateTime.UtcNow);
                MemCache[url] = bmp;
                return bmp;
            }
            catch { /* defekte Datei */ }
        }

        // 2. Aus Web herunterladen
        try
        {
            var bytes = await Http.GetByteArrayAsync(url);
            try
            {
                await File.WriteAllBytesAsync(localPath, bytes);
                TrimDiskCache();
            }
            catch { /* ignore */ }

            using var ms = new MemoryStream(bytes);
            var bmp = new Bitmap(ms);
            MemCache[url] = bmp;
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    private static void TrimDiskCache()
    {
        try
        {
            var files = new DirectoryInfo(CacheDir).GetFiles("*.img").OrderBy(file => file.LastAccessTimeUtc).ToList();
            long size = files.Sum(file => file.Length);
            foreach (var file in files)
            {
                if (size <= MaxCacheBytes) break;
                size -= file.Length;
                file.Delete();
            }
        }
        catch { /* cache cleanup is best effort */ }
    }
}
