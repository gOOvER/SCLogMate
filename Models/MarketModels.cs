using System;
using System.Collections.Generic;

namespace SCLogMate.Models;

public sealed record MarketCommodityEntry
{
    public string Name { get; init; } = "";
    public string Category { get; init; } = "Allgemein";
    public string? ResourceGuid { get; init; }
    public int TerminalsCount { get; init; }
    public int SellTerminalsCount { get; init; }
    public int BuyTerminalsCount { get; init; }

    // Eigene Handelszahlen
    public int MyScuSold { get; init; }
    public decimal MyRevenue { get; init; }
    public int MyTradeCount { get; init; }

    // UEX Marktpreise
    public decimal UexBestSell { get; init; }
    public string? UexBestSellTerminal { get; init; }
    public decimal UexBestBuy { get; init; }
    public string? UexBestBuyTerminal { get; init; }
    public decimal UexMargin => (UexBestSell > 0 && UexBestBuy > 0) ? UexBestSell - UexBestBuy : 0;
    public decimal UexAvgSell { get; init; }

    public string MyScuSoldText => MyScuSold > 0 ? $"{MyScuSold:N0} SCU" : "—";
    public string MyRevenueText => MyRevenue > 0 ? $"{MyRevenue:N0} aUEC" : "—";
    public string MyTradesBadgeText => MyTradeCount > 0 ? $"{MyTradeCount} Deals" : "—";

    public string UexBestSellText => UexBestSell > 0 ? $"{UexBestSell:N0} aUEC" : "—";
    public string UexBestBuyText => UexBestBuy > 0 ? $"{UexBestBuy:N0} aUEC" : "—";
    public string UexMarginText => UexMargin != 0 ? $"{UexMargin:+#,##0;-#,##0;0} aUEC" : "—";
    public string UexMarginColor => UexMargin > 0 ? "#4ADE80" : (UexMargin < 0 ? "#F87171" : "#8B949E");

    public string BestSellPriceFormatted => UexBestSellText;
    public string BestBuyPriceFormatted => UexBestBuyText;
    public string MarginFormatted => UexMarginText;
    public string UserTradesCountText => MyTradesBadgeText;
    public string UserRevenueFormatted => MyRevenueText;
}

public sealed record CommodityTerminalRow
{
    public string Terminal { get; init; } = "";
    public string System { get; init; } = "Stanton";
    public string Location { get; init; } = "";
    public bool IsLawless { get; init; }
    public decimal BuyPrice { get; init; } // What kiosk charges you
    public decimal SellPrice { get; init; } // What kiosk pays you
    public decimal StockScu { get; init; }
    public decimal DemandScu { get; init; }

    public string BuyPriceText => BuyPrice > 0 ? $"{BuyPrice:N0} aUEC" : "—";
    public string SellPriceText => SellPrice > 0 ? $"{SellPrice:N0} aUEC" : "—";
    public string StockText => StockScu > 0 ? $"{StockScu:N0} SCU" : "—";
    public string DemandText => DemandScu > 0 ? $"{DemandScu:N0} SCU" : "—";

    public string Type => SellPrice > 0 ? "Kauf" : "Verkauf";
    public string PriceFormatted => SellPrice > 0 ? SellPriceText : BuyPriceText;
    public string DiffFormatted => "Bestwert";
    public string SecurityStatus => JurisdictionText;
    public string SecurityBrush => JurisdictionColor;

    public string JurisdictionText => IsLawless ? "⚔ Gesetzlos (Unüberwacht)" : "🛡 Überwacht (Schutzzone)";
    public string JurisdictionColor => IsLawless ? "#F87171" : "#4ADE80";
    public string JurisdictionBg => IsLawless ? "#260F11" : "#092212";
}

public sealed record ConfirmedPurchaseRecord
{
    public DateTime Timestamp { get; init; }
    public string ItemName { get; init; } = "";
    public string Category { get; init; } = "Sonstige";
    public string Shop { get; init; } = "—";
    public string Location { get; init; } = "—";
    public decimal TotalPrice { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice => Quantity > 0 ? TotalPrice / Quantity : TotalPrice;
    public bool Confirmed { get; init; } = true;

    public string TimeText => Timestamp.ToLocalTime().ToString("dd.MM.yy HH:mm");
    public string TotalPriceText => $"{TotalPrice:N0} aUEC";
    public string UnitPriceText => $"{UnitPrice:N0} aUEC";
    public string QuantityText => $"×{Quantity}";

    public string ShopName => Shop;
    public string TimestampText => TimeText;
    public string TotalPriceFormatted => TotalPriceText;
}
