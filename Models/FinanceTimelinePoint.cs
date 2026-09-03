using System;
using Avalonia.Media;

namespace SCLogMate.Models;

/// <summary>
/// Repräsentiert einen Zeitreihen-Punkt für die interaktiven Sci-Fi Finanz-Diagramme.
/// Unterstützt kumulierte Kurven (Einnahmen vs. Ausgaben), Netto-Gewinnverlauf und Cashflow-Balken.
/// </summary>
public class FinanceTimelinePoint
{
    public DateTime Time { get; init; }
    public long Amount { get; init; }
    public long CumulativeIncome { get; init; }
    public long CumulativeSpend { get; init; }
    public long CumulativeNet { get; init; }
    public string Label { get; init; } = "";
    public EventKind Kind { get; init; }
    public string Detail { get; init; } = "";

    public bool IsIncome => Amount >= 0;
    public string AmountText => $"{(Amount >= 0 ? "+" : "")}{Amount:N0} aUEC";
    public string WhenText => Time == default ? "" : Time.ToLocalTime().ToString("dd.MM. HH:mm");
    public string DateOnlyText => Time == default ? "" : Time.ToLocalTime().ToString("dd.MM.yyyy");
}
