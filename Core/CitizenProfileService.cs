using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace SCLogMate.Core;

public class CitizenProfileDto
{
    [JsonPropertyName("handle")]
    public string Handle { get; set; } = "—";

    [JsonPropertyName("citizenRecord")]
    public string CitizenRecord { get; set; } = "—";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "Civilian";

    [JsonPropertyName("avatarUrl")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("enlisted")]
    public string Enlisted { get; set; } = "—";

    [JsonPropertyName("fluency")]
    public string Fluency { get; set; } = "—";

    [JsonPropertyName("orgName")]
    public string? OrgName { get; set; }

    [JsonPropertyName("orgSid")]
    public string? OrgSid { get; set; }

    [JsonPropertyName("orgRank")]
    public string? OrgRank { get; set; }

    [JsonPropertyName("orgLogoUrl")]
    public string? OrgLogoUrl { get; set; }

    [JsonPropertyName("profileUrl")]
    public string ProfileUrl { get; set; } = "https://robertsspaceindustries.com";

    [JsonPropertyName("bio")]
    public string? Bio { get; set; }

    [JsonPropertyName("isVerified")]
    public bool IsVerified { get; set; } = false;
}

public static class CitizenProfileService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10),
    };

    private static readonly ConcurrentDictionary<string, CitizenProfileDto> Cache = new(StringComparer.OrdinalIgnoreCase);

    static CitizenProfileService()
    {
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 (compatible; SCLogMate/1.0)");
    }

    public static async Task<CitizenProfileDto> GetProfileAsync(string handle)
    {
        if (string.IsNullOrWhiteSpace(handle) || handle == "—" || handle.Equals("Unbekannt", StringComparison.OrdinalIgnoreCase))
        {
            return new CitizenProfileDto { Handle = "—" };
        }

        var cleanHandle = handle.Trim();
        if (Cache.TryGetValue(cleanHandle, out var cached))
        {
            return cached;
        }

        var dto = await FetchRsiProfileAsync(cleanHandle);
        Cache[cleanHandle] = dto;
        return dto;
    }

    public static CitizenProfileDto? GetCached(string handle)
    {
        if (string.IsNullOrWhiteSpace(handle)) return null;
        return Cache.TryGetValue(handle.Trim(), out var dto) ? dto : null;
    }

    private static async Task<CitizenProfileDto> FetchRsiProfileAsync(string handle)
    {
        var profileUrl = $"https://robertsspaceindustries.com/citizens/{Uri.EscapeDataString(handle)}";
        var dto = new CitizenProfileDto
        {
            Handle = handle,
            ProfileUrl = profileUrl
        };

        try
        {
            var response = await HttpClient.GetAsync(profileUrl);
            if (!response.IsSuccessStatusCode)
            {
                return dto;
            }

            var html = await response.Content.ReadAsStringAsync();
            dto.IsVerified = true;

            // Citizen Record #
            var matchRecord = Regex.Match(html, @"UEE Citizen Record\s*</span>\s*<strong class=""value"">#?(\d+)</strong>", RegexOptions.IgnoreCase);
            if (matchRecord.Success)
            {
                dto.CitizenRecord = $"#{matchRecord.Groups[1].Value.Trim()}";
            }

            // Handle name
            var matchHandle = Regex.Match(html, @"Handle name\s*</span>\s*<strong class=""value"">([^<]+)</strong>", RegexOptions.IgnoreCase);
            if (matchHandle.Success)
            {
                dto.Handle = HttpUtility.HtmlDecode(matchHandle.Groups[1].Value.Trim());
            }

            // Title / Rank
            var matchTitle = Regex.Match(html, @"<span class=""value"">([A-Za-z0-9\s\-]+)</span>\s*</p>", RegexOptions.IgnoreCase);
            if (matchTitle.Success)
            {
                var t = HttpUtility.HtmlDecode(matchTitle.Groups[1].Value.Trim());
                if (!string.IsNullOrWhiteSpace(t)) dto.Title = t;
            }

            // Enlisted Date
            var matchEnlisted = Regex.Match(html, @"Enlisted\s*</span>\s*<strong class=""value"">([^<]+)</strong>", RegexOptions.IgnoreCase);
            if (matchEnlisted.Success)
            {
                dto.Enlisted = HttpUtility.HtmlDecode(matchEnlisted.Groups[1].Value.Trim());
            }

            // Fluency
            var matchFluency = Regex.Match(html, @"Fluency\s*</span>\s*<strong class=""value"">([^<]+)</strong>", RegexOptions.IgnoreCase);
            if (matchFluency.Success)
            {
                var f = HttpUtility.HtmlDecode(matchFluency.Groups[1].Value.Trim());
                dto.Fluency = Regex.Replace(f, @"\s+", " ").Trim();
            }

            // Avatar image URL
            var matchAvatar = Regex.Match(html, @"<div class=""thumb"">\s*<img src=""([^""]+)""", RegexOptions.IgnoreCase);
            if (matchAvatar.Success)
            {
                var a = matchAvatar.Groups[1].Value.Trim();
                if (a.StartsWith("/")) a = "https://robertsspaceindustries.com" + a;
                dto.AvatarUrl = a;
            }

            // Organization Name & SID & Rank & Logo
            var matchOrg = Regex.Match(html, @"<a href=""/orgs/([^""]+)"" class=""value data10""[^>]*>([^<]+)</a>", RegexOptions.IgnoreCase);
            if (matchOrg.Success)
            {
                dto.OrgSid = HttpUtility.HtmlDecode(matchOrg.Groups[1].Value.Trim());
                dto.OrgName = HttpUtility.HtmlDecode(matchOrg.Groups[2].Value.Trim());
            }

            var matchOrgRank = Regex.Match(html, @"Organization rank\s*</span>\s*<strong class=""value[^""]*"">([^<]+)</strong>", RegexOptions.IgnoreCase);
            if (matchOrgRank.Success)
            {
                dto.OrgRank = HttpUtility.HtmlDecode(matchOrgRank.Groups[1].Value.Trim());
            }

            var matchOrgLogo = Regex.Match(html, @"<a href=""/orgs/[^""]+"">\s*<img src=""([^""]+)""", RegexOptions.IgnoreCase);
            if (matchOrgLogo.Success)
            {
                var oLogo = matchOrgLogo.Groups[1].Value.Trim();
                if (oLogo.StartsWith("/")) oLogo = "https://robertsspaceindustries.com" + oLogo;
                dto.OrgLogoUrl = oLogo;
            }

            // Bio
            var matchBio = Regex.Match(html, @"<div class=""bio[^""]*"">[\s\S]*?<div class=""value"">([\s\S]*?)</div>", RegexOptions.IgnoreCase);
            if (matchBio.Success)
            {
                var rawBio = matchBio.Groups[1].Value;
                var cleanBio = Regex.Replace(rawBio, @"<[^>]+>", " ");
                dto.Bio = HttpUtility.HtmlDecode(Regex.Replace(cleanBio, @"\s+", " ").Trim());
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"CitizenProfileService.FetchRsiProfile({handle})", ex);
        }

        return dto;
    }
}
