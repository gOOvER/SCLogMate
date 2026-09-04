using System;

namespace SCLogMate.Models;

public sealed record LedgerRecord
{
    public DateTime Timestamp { get; init; }
    public string Kind { get; init; } = ""; // "Frachtverkauf", "Frachtkauf", "Item gekauft", "Strafe gezahlt", "Belohnung"
    public string What { get; init; } = ""; // e.g. "Laranite · 128 SCU" or "P4-AR Rifle"
    public string Where { get; init; } = "—"; // Back-tracked location
    public string Shop { get; init; } = "—"; // Shop / Terminal
    public decimal Amount { get; init; } // Positive for income, negative for spend
    public int Quantity { get; init; }
    public bool Confirmed { get; init; }
    public decimal RunningBalance { get; init; }

    public string TimeText => Timestamp.ToLocalTime().ToString("dd.MM.yy HH:mm");
    public string AmountText => Amount >= 0 ? $"+{Amount:N0} aUEC" : $"{Amount:N0} aUEC";
    public string RunningBalanceText => $"{RunningBalance:N0} aUEC";

    public bool IsPositive => Amount >= 0;
    public string AmountColor => IsPositive ? "#4ADE80" : "#F87171";
    public string AmountBg => IsPositive ? "#0B2615" : "#2E1114";
    public string AmountBorder => IsPositive ? "#1A5E33" : "#742429";

    public string KindBadgeColor => Kind switch
    {
        "Frachtverkauf" => "#4ADE80",
        "Frachtkauf" => "#F59E0B",
        "Item gekauft" => "#FB923C",
        "Strafe gezahlt" => "#F87171",
        "Belohnung" => "#38BDF8",
        _ => "#8B949E"
    };

    public string KindBadgeBg => Kind switch
    {
        "Frachtverkauf" => "#092212",
        "Frachtkauf" => "#271905",
        "Item gekauft" => "#2B1607",
        "Strafe gezahlt" => "#2E1114",
        "Belohnung" => "#0A2033",
        _ => "#161B22"
    };

    public string TimestampText => TimeText;
    public string BadgeColor => KindBadgeColor;
    public string KindBadge => Kind;
    public string Detail => What;
    public string AmountFormatted => AmountText;
    public string RunningBalanceFormatted => RunningBalanceText;
    public string Location => Where;
    public string Ship => "—";
}
