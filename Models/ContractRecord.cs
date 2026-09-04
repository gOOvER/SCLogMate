using System;

namespace SCLogMate.Models;

public enum ContractOutcome
{
    InProgress,
    Completed,
    Abandoned,
    Unknown
}

public sealed record ContractRecord
{
    public string MissionId { get; init; } = "";
    public DateTime AcceptedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string Title { get; init; } = "";
    public string Issuer { get; init; } = "Unbekannt";
    public string Type { get; init; } = "Sonstige";
    public string Difficulty { get; init; } = "k.A.";
    public string System { get; init; } = "k.A.";
    public int StepsTotal { get; init; }
    public int StepsDone { get; init; }
    public ContractOutcome Outcome { get; init; } = ContractOutcome.InProgress;
    public TimeSpan? Duration => CompletedAt.HasValue && CompletedAt.Value >= AcceptedAt
        ? CompletedAt.Value - AcceptedAt
        : null;

    public string AcceptedAtText => AcceptedAt.ToLocalTime().ToString("dd.MM.yy HH:mm");
    public string StepsText => StepsTotal > 0 ? $"{StepsDone}/{StepsTotal}" : "—";
    public string DurationText => Duration.HasValue
        ? Duration.Value.TotalMinutes < 1 ? "< 1 min" : $"~{Duration.Value.TotalMinutes:F0} min"
        : "—";

    public string OutcomeText => Outcome switch
    {
        ContractOutcome.Completed => "Abgeschlossen",
        ContractOutcome.Abandoned => "Abgebrochen",
        ContractOutcome.InProgress => "Aktiv",
        _ => "Unbekannt"
    };

    public string OutcomeColor => Outcome switch
    {
        ContractOutcome.Completed => "#4ADE80", // Green
        ContractOutcome.Abandoned => "#F87171", // Red
        ContractOutcome.InProgress => "#38BDF8", // Sky Blue
        _ => "#8B949E"
    };

    public long Reward { get; init; }
    public string RewardFormatted => Reward > 0 ? $"{Reward:N0} aUEC" : "—";
    public string ProgressText => StepsText;
    public string OutcomeBrush => OutcomeColor;

    public string OutcomeBg => Outcome switch
    {
        ContractOutcome.Completed => "#0E2A18",
        ContractOutcome.Abandoned => "#2D1214",
        ContractOutcome.InProgress => "#0B2238",
        _ => "#161B22"
    };

    public string OutcomeBorder => Outcome switch
    {
        ContractOutcome.Completed => "#1E6B37",
        ContractOutcome.Abandoned => "#7A272B",
        ContractOutcome.InProgress => "#1C4E78",
        _ => "#30363D"
    };
}

public sealed record FacetItem(string Name, int Count)
{
    public string DisplayName => $"{Name} ({Count})";
}
