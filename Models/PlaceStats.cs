using System;

namespace SCLogMate.Models;

public sealed record LocationVisitTotal
{
    public string Name { get; init; } = "";
    public string? System { get; init; }
    public string? Body { get; init; }
    public string Kind { get; init; } = "Standort";
    public int Visits { get; init; }
    public DateTime? LastVisit { get; init; }

    public string SystemText => !string.IsNullOrEmpty(System) ? System : "—";
    public string BodyText => !string.IsNullOrEmpty(Body) ? Body : "—";
    public string LastVisitText => LastVisit.HasValue ? LastVisit.Value.ToLocalTime().ToString("dd.MM.yy HH:mm") : "—";
    public string VisitsBadgeText => Visits == 1 ? "1 Besuch" : $"{Visits:N0} Besuche";
    public string VisitCountFormatted => VisitsBadgeText;
    public string LastVisitedText => LastVisitText;

    public string SystemColor => System?.ToUpperInvariant() switch
    {
        "STANTON" => "#38BDF8",
        "PYRO" => "#F97316",
        "NYX" => "#A855F7",
        _ => "#8B949E"
    };
}

public sealed record QuantumDestinationTotal
{
    public string Destination { get; init; } = "";
    public int Jumps { get; init; }
    public DateTime? LastJump { get; init; }

    public string JumpsBadgeText => Jumps == 1 ? "1 Sprung" : $"{Jumps:N0} Sprünge";
    public string JumpCountFormatted => JumpsBadgeText;
    public string LastJumpText => LastJump.HasValue ? LastJump.Value.ToLocalTime().ToString("dd.MM.yy HH:mm") : "—";
}
