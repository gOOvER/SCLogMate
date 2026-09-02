using System;

namespace SCLogMate.Models;

/// <summary>
/// Datenmodell für einen per OCR im mobiGlas Contract Manager erfassten Auftrag.
/// </summary>
public record ContractDetails
{
    public string Title { get; set; } = "";
    public int Reward { get; set; }
    public string ContractedBy { get; set; } = "";
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public bool IsCompleted { get; set; }

    public string RewardText => Reward > 0 ? $"+{Reward:N0} aUEC" : "—";
    public string TimeText => ScannedAt.ToLocalTime().ToString("HH:mm:ss");

    public string DisplayText => Reward > 0
        ? $"{Title} · {Reward:N0} aUEC" + (!string.IsNullOrEmpty(ContractedBy) ? $" ({ContractedBy})" : "")
        : Title;
}
