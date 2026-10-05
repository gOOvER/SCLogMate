using System;
using System.Text.Json.Serialization;

namespace SCLogMate.Models;

/// <summary>
/// Repräsentiert einen Star Citizen Server-Shard im Tagebuch.
/// Speichert Spielzeit, Besuche, Bewertung (Good/Avoid) und Notizen sowie
/// den 1-Klick CIG Support-String für Issue-Council Tickets.
/// </summary>
public sealed record ServerShardRecord
{
    [JsonPropertyName("shardId")]
    public string ShardId { get; init; } = "";

    [JsonPropertyName("shardNumber")]
    public string ShardNumber { get; init; } = "";

    [JsonPropertyName("region")]
    public string Region { get; init; } = "PU";

    [JsonPropertyName("regionFlag")]
    public string RegionFlag { get; init; } = "🌐";

    [JsonPropertyName("firstSeen")]
    public DateTime? FirstSeen { get; init; }

    [JsonPropertyName("lastSeen")]
    public DateTime? LastSeen { get; init; }

    [JsonPropertyName("visitCount")]
    public int VisitCount { get; init; } = 1;

    [JsonPropertyName("totalSeconds")]
    public long TotalSeconds { get; init; }

    [JsonPropertyName("lastEndReason")]
    public string LastEndReason { get; init; } = "Normal Quit";

    /// <summary>
    /// "Good", "Avoid" oder "Neutral"
    /// </summary>
    [JsonPropertyName("rating")]
    public string Rating { get; init; } = "Neutral";

    [JsonPropertyName("notes")]
    public string Notes { get; init; } = "";

    [JsonIgnore]
    public string TotalDurationText
    {
        get
        {
            if (TotalSeconds <= 0) return "—";
            var ts = TimeSpan.FromSeconds(TotalSeconds);
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            return $"{ts.Minutes}m {ts.Seconds}s";
        }
    }

    [JsonIgnore]
    public string LastSeenText => LastSeen.HasValue ? LastSeen.Value.ToLocalTime().ToString("dd.MM.yy HH:mm") : "—";

    [JsonIgnore]
    public string FirstSeenText => FirstSeen.HasValue ? FirstSeen.Value.ToLocalTime().ToString("dd.MM.yy HH:mm") : "—";

    [JsonIgnore]
    public string VisitsBadgeText => VisitCount == 1 ? "1 Besuch" : $"{VisitCount:N0} Besuche";

    /// <summary>
    /// Standardisierter Support-String für CIG Issue Council und Support-Tickets.
    /// Format ist datenschutzkonform: Keine Spielernamen, keine Windows-Pfade, keine IP-Adressen.
    /// </summary>
    [JsonIgnore]
    public string CigSupportString
    {
        get
        {
            var endReasonStr = string.IsNullOrWhiteSpace(LastEndReason) ? "Normal Quit" : LastEndReason;
            var durStr = TotalDurationText != "—" ? TotalDurationText : "unbekannt";
            return $"Shard: {ShardId} | Region: {Region} | Total Session Playtime: {durStr} | Disconnect/Exit: {endReasonStr}";
        }
    }

    [JsonIgnore]
    public string RatingColor => Rating switch
    {
        "Good" => "#10B981",    // Emerald / Green
        "Avoid" => "#EF4444",   // Red / Alert
        _ => "#94A3B8"          // Slate / Neutral
    };

    [JsonIgnore]
    public string RatingBadgeText => Rating switch
    {
        "Good" => "Gut (Empfohlen)",
        "Avoid" => "Meiden (Problematisch)",
        _ => "Neutral"
    };
}
