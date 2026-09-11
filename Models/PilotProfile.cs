using System;
using System.Text.Json.Serialization;

namespace SCLogMate.Models;

public sealed class PilotProfile
{
    [JsonPropertyName("handle")]
    public string Handle { get; set; } = "";

    [JsonPropertyName("citizenRecord")]
    public string CitizenRecord { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("avatarUrl")]
    public string AvatarUrl { get; set; } = "";

    [JsonPropertyName("enlisted")]
    public string Enlisted { get; set; } = "";

    [JsonPropertyName("fluency")]
    public string Fluency { get; set; } = "";

    [JsonPropertyName("orgName")]
    public string OrgName { get; set; } = "";

    [JsonPropertyName("orgSid")]
    public string OrgSid { get; set; } = "";

    [JsonPropertyName("orgRank")]
    public string OrgRank { get; set; } = "";

    [JsonPropertyName("orgLogoUrl")]
    public string OrgLogoUrl { get; set; } = "";

    [JsonPropertyName("website")]
    public string Website { get; set; } = "";

    [JsonPropertyName("profileUrl")]
    public string ProfileUrl { get; set; } = "";

    [JsonPropertyName("bio")]
    public string Bio { get; set; } = "";

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
