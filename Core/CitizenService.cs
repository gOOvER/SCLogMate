using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SCLogMate.Models;

namespace SCLogMate.Core;

public static class CitizenService
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(12)
    };

    private static readonly ConcurrentDictionary<string, PilotProfile?> MemoryCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentQueue<string> PrefetchQueue = new();
    private static readonly ConcurrentDictionary<string, byte> QueuedOrFetched = new(StringComparer.OrdinalIgnoreCase);
    private static bool _isWorkerRunning;
    private static readonly System.Threading.Lock WorkerLock = new();

    public static event Action<PilotProfile>? ProfileResolved;

    static CitizenService()
    {
        Http.DefaultRequestHeaders.Add("User-Agent", "SCLogMate/1.0.0 (+https://github.com/gOOvER/SCLogMate)");
        Http.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/json");
    }

    public static PilotProfile? GetCachedProfile(string? handle)
    {
        if (string.IsNullOrWhiteSpace(handle) || handle == "—" || handle == "Unbekannter Pilot" || handle == "Kein Pilot erkannt")
            return null;

        var clean = handle.Trim();
        if (MemoryCache.TryGetValue(clean, out var mem) && mem != null)
            return mem;

        var dbProfile = Database.GetPilotProfile(clean);
        if (dbProfile != null)
        {
            MemoryCache[clean] = dbProfile;
            // Wenn älter als 7 Tage, im Hintergrund auffrischen
            if (DateTime.UtcNow - dbProfile.UpdatedAt > TimeSpan.FromDays(7))
            {
                EnqueueProfileFetch(clean);
            }
            return dbProfile;
        }

        EnqueueProfileFetch(clean);
        return null;
    }

    public static void EnqueueProfileFetch(string? handle)
    {
        if (string.IsNullOrWhiteSpace(handle) || handle == "—" || handle == "Unbekannter Pilot" || handle == "Kein Pilot erkannt")
            return;

        var clean = handle.Trim();
        if (QueuedOrFetched.TryAdd(clean, 0))
        {
            PrefetchQueue.Enqueue(clean);
            StartWorker();
        }
    }

    private static void StartWorker()
    {
        lock (WorkerLock)
        {
            if (_isWorkerRunning) return;
            _isWorkerRunning = true;
        }

        Task.Run(async () =>
        {
            while (PrefetchQueue.TryDequeue(out var handle))
            {
                try
                {
                    await FetchProfileAsync(handle);
                    await Task.Delay(300); // Höfliches Rate-Limiting
                }
                catch (Exception ex)
                {
                    Logger.Error($"CitizenService: Fehler bei Profile-Fetch für '{handle}'", ex);
                }
            }

            lock (WorkerLock)
            {
                _isWorkerRunning = false;
            }
        });
    }

    public static async Task<PilotProfile?> FetchProfileAsync(string handle)
    {
        if (string.IsNullOrWhiteSpace(handle) || handle == "—" || handle == "Unbekannter Pilot")
            return null;

        var clean = handle.Trim();

        // 1. Zuerst frisches SQLite Cache prüfen (jünger als 7 Tage)
        var cached = Database.GetPilotProfile(clean);
        if (cached != null && (DateTime.UtcNow - cached.UpdatedAt) < TimeSpan.FromDays(7))
        {
            MemoryCache[clean] = cached;
            return cached;
        }

        // 2. Roberts Space Industries Citizen Dossier abrufen
        try
        {
            var url = $"https://robertsspaceindustries.com/citizens/{Uri.EscapeDataString(clean)}";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await Http.SendAsync(req);

            if (response.IsSuccessStatusCode)
            {
                var html = await response.Content.ReadAsStringAsync();
                var profile = ParseRsiDossierHtml(clean, html);

                if (profile != null)
                {
                    Database.SavePilotProfile(profile);
                    MemoryCache[clean] = profile;
                    ProfileResolved?.Invoke(profile);
                    Logger.Log($"CitizenService: RSI Dossier geladen für '{clean}' (#{profile.CitizenRecord})");
                    return profile;
                }
            }
            else
            {
                Logger.Log($"CitizenService: RSI Dossier HTTP Status {(int)response.StatusCode} für '{clean}'");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"CitizenService: Fehler beim Abruf des RSI Dossiers für '{clean}'", ex);
        }

        // 3. Fallback: CitizenID Space API prüfen
        try
        {
            var fallbackProfile = await FetchCitizenIdSpaceAsync(clean);
            if (fallbackProfile != null)
            {
                Database.SavePilotProfile(fallbackProfile);
                MemoryCache[clean] = fallbackProfile;
                ProfileResolved?.Invoke(fallbackProfile);
                Logger.Log($"CitizenService: CitizenID.space Profil geladen für '{clean}'");
                return fallbackProfile;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"CitizenService: Fallback citizenid.space Fehler für '{clean}'", ex);
        }

        // Falls wir alte DB-Daten hatten, diese wenigstens zurückgeben
        if (cached != null)
        {
            MemoryCache[clean] = cached;
            return cached;
        }

        return null;
    }

    private static PilotProfile? ParseRsiDossierHtml(string requestedHandle, string html)
    {
        if (string.IsNullOrWhiteSpace(html) || html.Contains("404 Not Found") || html.Contains("citizen-not-found"))
            return null;

        var profile = new PilotProfile
        {
            Handle = requestedHandle,
            ProfileUrl = $"https://robertsspaceindustries.com/citizens/{requestedHandle}",
            UpdatedAt = DateTime.UtcNow
        };

        // 1. Citizen Record: <p class="entry citizen-record">...<strong class="value">#593923</strong>
        var recordMatch = Regex.Match(html, @"UEE Citizen Record[\s\S]*?<strong class=""value"">([^<]+)</strong>", RegexOptions.IgnoreCase);
        if (recordMatch.Success)
        {
            profile.CitizenRecord = recordMatch.Groups[1].Value.Trim();
        }

        // 2. Handle Name
        var handleMatch = Regex.Match(html, @"Handle name[\s\S]*?<strong class=""value"">([^<]+)</strong>", RegexOptions.IgnoreCase);
        if (handleMatch.Success)
        {
            profile.Handle = handleMatch.Groups[1].Value.Trim();
        }

        // 3. Title (z. B. High Admiral, Wing Commander, Civilian)
        var titleMatch = Regex.Match(html, @"<div class=""profile left-col"">[\s\S]*?<p class=""entry"">\s*(?:<span class=""icon"">[\s\S]*?</span>\s*)?<span class=""value"">([^<]+)</span>", RegexOptions.IgnoreCase);
        if (titleMatch.Success)
        {
            profile.Title = titleMatch.Groups[1].Value.Trim();
        }

        // 4. Avatar URL: <div class="thumb">\s*<img src="/media/..." />
        var avatarMatch = Regex.Match(html, @"<div class=""profile left-col"">[\s\S]*?<div class=""thumb"">\s*<img src=""([^""]+)""", RegexOptions.IgnoreCase);
        if (avatarMatch.Success)
        {
            var av = avatarMatch.Groups[1].Value.Trim();
            profile.AvatarUrl = av.StartsWith('/') ? $"https://robertsspaceindustries.com{av}" : av;
        }
        else
        {
            // Fallback auf CitizenID avatar redirect
            profile.AvatarUrl = $"https://citizenid.space/api/v1/profile/by-rsi/{Uri.EscapeDataString(profile.Handle)}/avatar";
        }

        // 5. Enlisted Date
        var enlistedMatch = Regex.Match(html, @"<span class=""label"">Enlisted</span>[\s\S]*?<strong class=""value"">([^<]+)</strong>", RegexOptions.IgnoreCase);
        if (enlistedMatch.Success)
        {
            profile.Enlisted = enlistedMatch.Groups[1].Value.Trim();
        }

        // 6. Fluency
        var fluencyMatch = Regex.Match(html, @"<span class=""label"">Fluency</span>[\s\S]*?<strong class=""value"">([\s\S]*?)</strong>", RegexOptions.IgnoreCase);
        if (fluencyMatch.Success)
        {
            var rawFluency = fluencyMatch.Groups[1].Value;
            var cleaned = Regex.Replace(rawFluency, @"\s+", " ").Trim();
            profile.Fluency = cleaned;
        }

        // 7. Website
        var websiteMatch = Regex.Match(html, @"<p class=""entry website"">[\s\S]*?<a[^>]*href=""([^""]+)""", RegexOptions.IgnoreCase);
        if (websiteMatch.Success)
        {
            profile.Website = websiteMatch.Groups[1].Value.Trim();
        }

        // 8. Main Organization
        var orgBoxMatch = Regex.Match(html, @"<div class=""main-org[^""]*"">([\s\S]*?)</div>\s*<span class=""deco-separator", RegexOptions.IgnoreCase);
        if (orgBoxMatch.Success)
        {
            var orgHtml = orgBoxMatch.Groups[1].Value;

            // Org Name: <a href="/orgs/SNPX" class="value data8" ...>Stellanebula Project</a>
            var orgNameMatch = Regex.Match(orgHtml, @"<a href=""/orgs/[^""]*""[^>]*>([^<]+)</a>", RegexOptions.IgnoreCase);
            if (orgNameMatch.Success)
            {
                profile.OrgName = orgNameMatch.Groups[1].Value.Trim();
            }

            // Org SID: Spectrum Identification (SID) ... <strong class="value data14">SNPX</strong>
            var orgSidMatch = Regex.Match(orgHtml, @"Spectrum Identification \(SID\)[\s\S]*?<strong class=""value[^""]*"">([^<]+)</strong>", RegexOptions.IgnoreCase);
            if (orgSidMatch.Success)
            {
                profile.OrgSid = orgSidMatch.Groups[1].Value.Trim();
            }

            // Org Rank: Organization rank ... <strong class="value data15">Recruit</strong>
            var orgRankMatch = Regex.Match(orgHtml, @"Organization rank[\s\S]*?<strong class=""value[^""]*"">([^<]+)</strong>", RegexOptions.IgnoreCase);
            if (orgRankMatch.Success)
            {
                profile.OrgRank = orgRankMatch.Groups[1].Value.Trim();
            }

            // Org Logo: <div class="thumb">...<img src="/media/..." />
            var orgLogoMatch = Regex.Match(orgHtml, @"<div class=""thumb"">[\s\S]*?<img src=""([^""]+)""", RegexOptions.IgnoreCase);
            if (orgLogoMatch.Success)
            {
                var oLogo = orgLogoMatch.Groups[1].Value.Trim();
                profile.OrgLogoUrl = oLogo.StartsWith('/') ? $"https://robertsspaceindustries.com{oLogo}" : oLogo;
            }
        }

        return profile;
    }

    private static async Task<PilotProfile?> FetchCitizenIdSpaceAsync(string handle)
    {
        var url = $"https://citizenid.space/api/v1/profile/by-rsi/{Uri.EscapeDataString(handle)}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("Accept", "application/json");

        var res = await Http.SendAsync(req);
        if (!res.IsSuccessStatusCode) return null;

        using var stream = await res.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;

        var profile = new PilotProfile
        {
            Handle = handle,
            ProfileUrl = $"https://robertsspaceindustries.com/citizens/{handle}",
            AvatarUrl = $"https://citizenid.space/api/v1/profile/by-rsi/{Uri.EscapeDataString(handle)}/avatar",
            UpdatedAt = DateTime.UtcNow
        };

        if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.Object)
        {
            if (dataEl.TryGetProperty("citizen_record", out var cr)) profile.CitizenRecord = cr.GetString() ?? "";
            if (dataEl.TryGetProperty("handle", out var h)) profile.Handle = h.GetString() ?? handle;
            if (dataEl.TryGetProperty("title", out var tit)) profile.Title = tit.GetString() ?? "";
            if (dataEl.TryGetProperty("enlisted", out var enl)) profile.Enlisted = enl.GetString() ?? "";
            if (dataEl.TryGetProperty("fluency", out var flu)) profile.Fluency = flu.GetString() ?? "";
            if (dataEl.TryGetProperty("bio", out var b)) profile.Bio = b.GetString() ?? "";

            if (dataEl.TryGetProperty("org", out var orgEl) && orgEl.ValueKind == JsonValueKind.Object)
            {
                if (orgEl.TryGetProperty("name", out var on)) profile.OrgName = on.GetString() ?? "";
                if (orgEl.TryGetProperty("sid", out var os)) profile.OrgSid = os.GetString() ?? "";
                if (orgEl.TryGetProperty("rank", out var or)) profile.OrgRank = or.GetString() ?? "";
                if (orgEl.TryGetProperty("logo", out var ol)) profile.OrgLogoUrl = ol.GetString() ?? "";
            }
        }

        return profile;
    }
}
