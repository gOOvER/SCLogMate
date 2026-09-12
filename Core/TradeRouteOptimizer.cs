using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SCLogMate.Core;

public class TradeRouteDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("commodity")] public string Commodity { get; set; } = "";
    [JsonPropertyName("origin")] public string Origin { get; set; } = "";
    [JsonPropertyName("destination")] public string Destination { get; set; } = "";
    [JsonPropertyName("system")] public string System { get; set; } = "Stanton";
    [JsonPropertyName("buyPricePerScu")] public double BuyPricePerScu { get; set; }
    [JsonPropertyName("sellPricePerScu")] public double SellPricePerScu { get; set; }
    [JsonPropertyName("profitPerScu")] public double ProfitPerScu { get; set; }
    [JsonPropertyName("roiPercent")] public double RoiPercent { get; set; }
    [JsonPropertyName("maxScu")] public int MaxScu { get; set; }
    [JsonPropertyName("investmentAuec")] public long InvestmentAuec { get; set; }
    [JsonPropertyName("totalProfitAuec")] public long TotalProfitAuec { get; set; }
    [JsonPropertyName("riskLevel")] public string RiskLevel { get; set; } = "Sicher"; // Sicher, Mittel, Hoch
}

public class SalvagePriceSummaryDto
{
    [JsonPropertyName("materialName")] public string MaterialName { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "Salvage";
    [JsonPropertyName("bestSellLocation")] public string BestSellLocation { get; set; } = "";
    [JsonPropertyName("bestSellPricePerScu")] public double BestSellPricePerScu { get; set; }
    [JsonPropertyName("avgSellPricePerScu")] public double AvgSellPricePerScu { get; set; }
    [JsonPropertyName("system")] public string System { get; set; } = "Stanton";
}

public static class TradeRouteOptimizer
{
    public static async Task<List<TradeRouteDto>> CalculateBestRoutesAsync(
        int cargoHoldScu = 696,
        long maxCapitalAuec = 20000000,
        string? filterSystem = null)
    {
        await UexApiClient.FetchCommodityPricesAsync();
        var prices = UexApiClient.GetAllCommodityPrices();
        var routes = new List<TradeRouteDto>();

        foreach (var p in prices.Values)
        {
            if (p.BestBuy <= 0 || p.BestSell <= 0) continue;
            var marginPerScu = p.BestSell - p.BestBuy;
            if (marginPerScu <= 0) continue;

            // SCU Cap nach Kapital beschränken
            int affordableScu = (int)Math.Min(cargoHoldScu, maxCapitalAuec / p.BestBuy);
            if (affordableScu <= 0) continue;

            long investment = (long)p.BestBuy * affordableScu;
            long profit = (long)marginPerScu * affordableScu;
            double roi = investment > 0 ? ((double)profit / investment) * 100 : 0;

            string origin = !string.IsNullOrEmpty(p.BestBuyTerminal) ? p.BestBuyTerminal : "Stanton Außenposten";
            string destination = !string.IsNullOrEmpty(p.BestSellTerminal) ? p.BestSellTerminal : "TDD / Handelszentrum";

            string sys = "Stanton";
            if (origin.Contains("Pyro", StringComparison.OrdinalIgnoreCase) || destination.Contains("Pyro", StringComparison.OrdinalIgnoreCase))
            {
                sys = "Pyro";
            }

            if (!string.IsNullOrEmpty(filterSystem) && filterSystem != "all" && !sys.Equals(filterSystem, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string risk = "Sicher";
            if (sys == "Pyro" || origin.Contains("Grim", StringComparison.OrdinalIgnoreCase) || destination.Contains("Grim", StringComparison.OrdinalIgnoreCase) || origin.Contains("Jumptown", StringComparison.OrdinalIgnoreCase))
            {
                risk = "Hoch";
            }
            else if (origin.Contains("Mining", StringComparison.OrdinalIgnoreCase) || destination.Contains("Scrap", StringComparison.OrdinalIgnoreCase))
            {
                risk = "Mittel";
            }

            routes.Add(new TradeRouteDto
            {
                Id = $"{p.CommodityName}_{origin}_{destination}".Replace(" ", "_"),
                Commodity = p.CommodityName,
                Origin = origin,
                Destination = destination,
                System = sys,
                BuyPricePerScu = (double)Math.Round(p.BestBuy, 0),
                SellPricePerScu = (double)Math.Round(p.BestSell, 0),
                ProfitPerScu = (double)Math.Round(marginPerScu, 0),
                RoiPercent = Math.Round(roi, 1),
                MaxScu = affordableScu,
                InvestmentAuec = investment,
                TotalProfitAuec = profit,
                RiskLevel = risk
            });
        }

        return routes.OrderByDescending(r => r.TotalProfitAuec).Take(25).ToList();
    }

    public static async Task<List<SalvagePriceSummaryDto>> GetSalvagePricesAsync()
    {
        await UexApiClient.FetchCommodityPricesAsync();
        var prices = UexApiClient.GetAllCommodityPrices();
        var salvageMaterials = new[]
        {
            "Recycled Material Composite",
            "Construction Materials",
            "Scrap",
            "Gold",
            "Quantainium",
            "Beryl",
            "Laranite",
            "Bexalite"
        };

        var list = new List<SalvagePriceSummaryDto>();
        foreach (var mat in salvageMaterials)
        {
            if (prices.TryGetValue(mat, out var p))
            {
                list.Add(new SalvagePriceSummaryDto
                {
                    MaterialName = p.CommodityName,
                    Category = (p.CommodityName.Contains("Material") || p.CommodityName == "Scrap") ? "Salvage / Scrapper" : "Mining / Erz",
                    BestSellLocation = !string.IsNullOrEmpty(p.BestSellTerminal) ? p.BestSellTerminal : "TDD / City Trade Center",
                    BestSellPricePerScu = (double)Math.Round(p.BestSell, 0),
                    AvgSellPricePerScu = (double)(p.AvgSell > 0 ? p.AvgSell : Math.Round(p.BestSell * 0.95m, 0)),
                    System = p.BestSellTerminal?.Contains("Pyro") == true ? "Pyro" : "Stanton"
                });
            }
        }

        return list;
    }
}
