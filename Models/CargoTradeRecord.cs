using System;

namespace SCLogMate.Models;

public sealed record CargoTradeRecord
{
    public DateTime Timestamp { get; init; }
    public bool IsSell { get; init; }
    public string Commodity { get; init; } = "Fracht";
    public string? ResourceGuid { get; init; }
    public int QuantityScu { get; init; }
    public decimal TotalAuec { get; init; }
    public decimal PricePerScu => QuantityScu > 0 ? TotalAuec / QuantityScu : 0;
    public string Shop { get; init; } = "—";
    public string Where { get; init; } = "—"; // Back-tracked location

    public string TimeText => Timestamp.ToLocalTime().ToString("dd.MM.yy HH:mm");
    public string ActionText => IsSell ? "VERKAUF" : "EINKAUF";
    public string ActionColor => IsSell ? "#4ADE80" : "#F59E0B";
    public string ActionBg => IsSell ? "#092212" : "#291804";
    public string ActionBorder => IsSell ? "#17522B" : "#6E400B";

    public string TotalAuecText => IsSell ? $"+{TotalAuec:N0} aUEC" : $"-{TotalAuec:N0} aUEC";
    public string PricePerScuText => $"{PricePerScu:N0} aUEC/SCU";
    public string ScuText => $"{QuantityScu:N0} SCU";

    public string TimestampText => TimeText;
    public string TotalPriceFormatted => TotalAuecText;
    public string PricePerScuFormatted => PricePerScuText;
    public string ScuFormatted => ScuText;
    public string Ship => "—";
}
