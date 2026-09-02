using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SCLogMate.Core;

public partial class TradeRouteItem : ObservableObject
{
    [ObservableProperty] private string commodity = "";
    [ObservableProperty] private string buyLocation = "";
    [ObservableProperty] private string sellLocation = "";
    [ObservableProperty] private long buyPricePerScu = 0;
    [ObservableProperty] private long sellPricePerScu = 0;
    [ObservableProperty] private string riskLevel = "Niedrig";
    [ObservableProperty] private string recommendedShip = "";
    [ObservableProperty] private long estimatedRunProfit = 0;

    public long ProfitPerScu => SellPricePerScu - BuyPricePerScu;
    public double MarginPercent => BuyPricePerScu > 0 ? ((double)ProfitPerScu / BuyPricePerScu) * 100.0 : 0.0;

    public string ProfitPerScuText => $"+{ProfitPerScu:N0} aUEC";
    public string EstimatedRunProfitText => $"★ +{EstimatedRunProfit:N0} aUEC";

    partial void OnEstimatedRunProfitChanged(long value)
    {
        OnPropertyChanged(nameof(EstimatedRunProfitText));
    }
}

public static class TradingCatalog
{
    public static List<TradeRouteItem> CreatePopularRoutes(int capacity = 696)
    {
        var raw = new List<(string c, string buy, string sell, long bP, long sP, string risk, string ship)>
        {
            ("RMC (Recycled Material)", "Pickers Field / Rappel (Hurston)", "Area18 TDD / Orison TDD", 9800, 14200, "Mittel (PvP Risiko)", "C2 Hercules / Cutlass Black"),
            ("Beryl", "Kudre Ore / HDMS-Lathan (Hurston)", "Lorville TDD (Hurston)", 2700, 3150, "Niedrig (Sicher)", "C2 / Caterpillar / Freelancer"),
            ("Gold", "Smuggler Cache / Orphanage (Lyria)", "Brio's Breaker Yard / GrimHEX", 6800, 8950, "Hoch (Gesetzlos)", "Corsair / Taurus / Cutlass"),
            ("Laranite", "HDMS-Woodward / Bezdek (Arial)", "Lorville CBD (Hurston)", 2850, 3450, "Niedrig-Mittel", "C2 / Constellation Taurus"),
            ("Titanium", "HDMS-Edmond / HDMS-Stanhope", "Lorville Central TDD", 820, 1020, "Niedrig", "Hull-C / C2 / MAX"),
            ("Distilled Spirits", "Hickes Research (Cellin)", "GrimHEX / Port Tressler", 420, 580, "Niedrig", "Freelancer / Cutlass"),
            ("Medical Supplies", "Deakins Research (Yela)", "Port Olisar / CRU-L1", 1750, 2150, "Niedrig", "Cutlass / Freelancer"),
            ("Agricium", "ARC-L1 / Mining Stations", "Area18 TDD", 2450, 2900, "Niedrig", "Freelancer / Zeus Mk II"),
            ("Neon / Slam (Illegale Drogen)", "Jumptown (Yela) / Raven's Roost", "GrimHEX / Samson & Son", 8200, 13400, "Sehr Hoch (Piraterie)", "MSR / Cutlass Black")
        };

        var list = new List<TradeRouteItem>();
        foreach (var (c, buy, sell, bP, sP, risk, ship) in raw)
        {
            var item = new TradeRouteItem
            {
                Commodity = c,
                BuyLocation = buy,
                SellLocation = sell,
                BuyPricePerScu = bP,
                SellPricePerScu = sP,
                RiskLevel = risk,
                RecommendedShip = ship,
                EstimatedRunProfit = (sP - bP) * capacity
            };
            list.Add(item);
        }
        return list;
    }
}
