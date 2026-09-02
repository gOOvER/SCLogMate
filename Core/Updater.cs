using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace SCLogMate.Core;

/// <summary>
/// Auto-Updater über GitHub Releases: prüft die neueste Version,
/// lädt die .exe und ersetzt sich selbst (über ein kleines Helfer-Batch).
/// </summary>
public static class Updater
{
    const string Repo = "gOOvER/SCLogMate";

    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

    public record Info(string Version, string Url, string ChecksumUrl, string? ReleaseNotes = null, string? HtmlUrl = null);

    public static string CurrentVersion
    {
        get
        {
            var asm = typeof(Updater).Assembly;
            var infoVer = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(infoVer))
            {
                var clean = infoVer.Split('+')[0].Trim().TrimStart('v', 'V');
                if (!string.IsNullOrEmpty(clean)) return clean;
            }

            var v = asm.GetName().Version;
            return v != null ? $"{v.Major}.{v.Minor}.{v.Build}" : "1.0.0";
        }
    }

    /// <summary>Liefert Update-Info, falls eine neuere Version vorliegt; sonst null.</summary>
    public static async Task<Info?> CheckAsync()
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.github.com/repos/{Repo}/releases?per_page=5");
            req.Headers.UserAgent.ParseAdd("SCLogMate");
            req.Headers.Accept.ParseAdd("application/vnd.github+json");

            using var resp = await Http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0) return null;

            // Erstes Release (neuestes) prüfen
            var rel = doc.RootElement[0];
            var tag = rel.GetProperty("tag_name").GetString()?.TrimStart('v', 'V');
            var body = rel.TryGetProperty("body", out var b) ? b.GetString() : null;
            var htmlUrl = rel.TryGetProperty("html_url", out var h) ? h.GetString() : $"https://github.com/{Repo}/releases";

            string? url = null;
            string? checksumUrl = null;
            if (rel.TryGetProperty("assets", out var assets))
            {
                foreach (var a in assets.EnumerateArray())
                {
                    var assetName = a.GetProperty("name").GetString() ?? "";
                    if (assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        url = a.GetProperty("browser_download_url").GetString();
                    }
                    else if (assetName.Equals("SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase))
                    {
                        checksumUrl = a.GetProperty("browser_download_url").GetString();
                    }
                }
            }

            if (tag != null && url != null && checksumUrl != null && IsNewer(tag, CurrentVersion))
                return new Info(tag, url, checksumUrl, body, htmlUrl);
        }
        catch { /* offline / kein Release -> kein Update */ }
        return null;
    }

    /// <summary>Lädt das Update und startet das Ersetzen (App muss danach beenden).</summary>
    public static async Task ApplyAsync(Info info)
    {
        var cur = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule!.FileName;
        var tmp = cur + ".new";

        var bytes = await Http.GetByteArrayAsync(info.Url);
        var checksumFile = await Http.GetStringAsync(info.ChecksumUrl);
        var expectedHash = GetExpectedHash(checksumFile, Path.GetFileName(new Uri(info.Url).LocalPath));
        var actualHash = Convert.ToHexString(SHA256.HashData(bytes));
        if (expectedHash is null || !actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Die Prüfsumme des Updates stimmt nicht mit dem Release überein.");

        await File.WriteAllBytesAsync(tmp, bytes);

        // Batch: warten bis App zu ist, alte exe ersetzen, neu starten, sich selbst löschen.
        var bat = Path.Combine(Path.GetTempPath(), "sclm_update.bat");
        await File.WriteAllTextAsync(bat,
            "@echo off\r\n" +
            "timeout /t 2 /nobreak >nul\r\n" +
            $"move /y \"{tmp}\" \"{cur}\" >nul\r\n" +
            $"start \"\" \"{cur}\"\r\n" +
            "del \"%~f0\"\r\n");

        Process.Start(new ProcessStartInfo
        {
            FileName = bat,
            UseShellExecute = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });
    }

    private static string? GetExpectedHash(string checksumFile, string assetName)
    {
        foreach (var line in checksumFile.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && parts[^1].Equals(assetName, StringComparison.OrdinalIgnoreCase))
                return parts[0];
        }
        return null;
    }

    static bool IsNewer(string remote, string local)
    {
        static (int, int, int) P(string s)
        {
            var p = s.Split('.', '-', '+').Where(x => int.TryParse(x, out _)).Select(int.Parse).ToArray();
            return (p.ElementAtOrDefault(0), p.ElementAtOrDefault(1), p.ElementAtOrDefault(2));
        }
        return P(remote).CompareTo(P(local)) > 0;
    }
}
