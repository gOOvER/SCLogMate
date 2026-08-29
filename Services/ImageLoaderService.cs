using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using SCLogReader.Core;

namespace SCLogReader.Services;

/// <summary>
/// Lädt Bilder asynchron aus dem Web (mit In-Memory & Disk-Caching) für die Wiki- und Schiffsanzeigen.
/// </summary>
public static class ImageLoaderService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };
    private static readonly ConcurrentDictionary<string, Bitmap> MemCache = new();
    private static readonly string CacheDir = Path.Combine(
        Settings.Dir, "image_cache");

    static ImageLoaderService()
    {
        try { Directory.CreateDirectory(CacheDir); } catch { /* ignore */ }
    }

    public static async Task<Bitmap?> LoadBitmapAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        if (MemCache.TryGetValue(url, out var cached)) return cached;

        var fileName = Math.Abs(url.GetHashCode()).ToString("X") + ".img";
        var localPath = Path.Combine(CacheDir, fileName);

        // 1. Aus Disk-Cache laden
        if (File.Exists(localPath))
        {
            try
            {
                using var fs = File.OpenRead(localPath);
                var bmp = new Bitmap(fs);
                MemCache[url] = bmp;
                return bmp;
            }
            catch { /* defekte Datei */ }
        }

        // 2. Aus Web herunterladen
        try
        {
            var bytes = await Http.GetByteArrayAsync(url);
            try { await File.WriteAllBytesAsync(localPath, bytes); } catch { /* ignore */ }

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
}
