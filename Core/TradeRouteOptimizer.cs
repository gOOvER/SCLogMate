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
    [JsonPropertyName("distanceGm")] public double DistanceGm { get; set; }
    [JsonPropertyName("profitPerGm")] public double ProfitPerGm { get; set; }
    [JsonPropertyName("boxCount")] public int BoxCount { get; set; }
    [JsonPropertyName("autoLoadFee")] public long AutoLoadFee { get; set; }
    [JsonPropertyName("autoLoadSeconds")] public int AutoLoadSeconds { get; set; }
    [JsonPropertyName("boxBreakdown")] public string BoxBreakdown { get; set; } = "";
    [JsonPropertyName("originHasDock")] public bool OriginHasDock { get; set; }
    [JsonPropertyName("destinationHasDock")] public bool DestinationHasDock { get; set; }
    [JsonPropertyName("dockWarning")] public string? DockWarning { get; set; }
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
        string? filterSystem = null,
        string? originLocation = null,
        string? rankMode = "Profit",
        string? demandFilter = "Any",
        int shipMaxBoxScu = 32)
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

            string origin = !string.IsNullOrEmpty(p.BestBuyTerminal) ? p.BestBuyTerminal : "Stanton Außenposten";
            string destination = !string.IsNullOrEmpty(p.BestSellTerminal) ? p.BestSellTerminal : "TDD / Handelszentrum";

            // Origin Anchor Filter (z.B. "FROM HERE" bzw. spezifischer Ort)
            if (!string.IsNullOrWhiteSpace(originLocation) && !originLocation.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                if (!origin.Contains(originLocation, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            string sys = "Stanton";
            if (origin.Contains("Pyro", StringComparison.OrdinalIgnoreCase) || destination.Contains("Pyro", StringComparison.OrdinalIgnoreCase))
            {
                sys = "Pyro";
            }

            if (!string.IsNullOrEmpty(filterSystem) && filterSystem != "all" && !sys.Equals(filterSystem, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Distanz & Profit/Gm ermitteln
            double distGm = EstimateDistanceGm(origin, destination);

            // Container Planning für diese Route berechnen
            var plan = ContainerPlanner.Plan(null, shipMaxBoxScu, affordableScu);
            int actualScu = plan?.TotalScu ?? affordableScu;
            long investment = (long)p.BestBuy * actualScu;
            long profit = (long)marginPerScu * actualScu;
            double roi = investment > 0 ? ((double)profit / investment) * 100 : 0;
            double profitPerGm = distGm > 0 ? Math.Round((double)profit / distGm, 0) : profit;

            string risk = "Sicher";
            if (sys == "Pyro" || origin.Contains("Grim", StringComparison.OrdinalIgnoreCase) || destination.Contains("Grim", StringComparison.OrdinalIgnoreCase) || origin.Contains("Jumptown", StringComparison.OrdinalIgnoreCase))
            {
                risk = "Hoch";
            }
            else if (origin.Contains("Mining", StringComparison.OrdinalIgnoreCase) || destination.Contains("Scrap", StringComparison.OrdinalIgnoreCase) || origin.Contains("Brio", StringComparison.OrdinalIgnoreCase))
            {
                risk = "Mittel";
            }

            string boxBreakdown = plan != null
                ? string.Join(", ", plan.Picks.Select(pk => $"{pk.Count}× {pk.Scu} SCU"))
                : $"{actualScu} SCU";

            bool originHasDock = CargoConstraints.HasLoadingDock(origin);
            bool destHasDock = CargoConstraints.HasLoadingDock(destination);
            string? dockWarning = null;
            if (!originHasDock && !destHasDock)
            {
                dockWarning = "Kein Loading Dock (nur Standard-Lift / manuelle Verladung)";
            }
            else if (!originHasDock)
            {
                dockWarning = "Startort ohne Loading Dock (kein Auto-Load am Außenposten)";
            }
            else if (!destHasDock)
            {
                dockWarning = "Zielort ohne Loading Dock (kein Auto-Load am Außenposten)";
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
                MaxScu = actualScu,
                InvestmentAuec = investment,
                TotalProfitAuec = profit,
                RiskLevel = risk,
                DistanceGm = distGm,
                ProfitPerGm = profitPerGm,
                BoxCount = plan?.TotalBoxCount ?? 0,
                AutoLoadFee = plan?.TotalAutoLoadFee ?? 0,
                AutoLoadSeconds = plan?.TotalEstimatedSeconds ?? 0,
                BoxBreakdown = boxBreakdown,
                OriginHasDock = originHasDock,
                DestinationHasDock = destHasDock,
                DockWarning = dockWarning
            });
        }

        // Multi-Variable Ranking (Profit, ProfitPerScu, ProfitPerGm, Roi)
        var ordered = (rankMode?.ToLowerInvariant()) switch
        {
            "profitperscu" => routes.OrderByDescending(r => r.ProfitPerScu).ThenByDescending(r => r.TotalProfitAuec),
            "profitpergm" => routes.OrderByDescending(r => r.ProfitPerGm).ThenByDescending(r => r.TotalProfitAuec),
            "roi" => routes.OrderByDescending(r => r.RoiPercent).ThenByDescending(r => r.TotalProfitAuec),
            _ => routes.OrderByDescending(r => r.TotalProfitAuec)
        };

        return ordered.Take(25).ToList();
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

        var result = new List<SalvagePriceSummaryDto>();
        foreach (var mat in salvageMaterials)
        {
            if (prices.TryGetValue(mat, out var p))
            {
                string sys = "Stanton";
                if (!string.IsNullOrEmpty(p.BestSellTerminal) && p.BestSellTerminal.Contains("Pyro", StringComparison.OrdinalIgnoreCase))
                {
                    sys = "Pyro";
                }

                result.Add(new SalvagePriceSummaryDto
                {
                    MaterialName = mat,
                    Category = mat.Contains("Material") || mat == "Scrap" ? "Salvage" : "Erz",
                    BestSellLocation = !string.IsNullOrEmpty(p.BestSellTerminal) ? p.BestSellTerminal : "TDD",
                    BestSellPricePerScu = (double)Math.Round(p.BestSell, 0),
                    AvgSellPricePerScu = (double)Math.Round(p.AvgSell, 0),
                    System = sys
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Schätzt die gerade astronomische Distanz zwischen Start- und Zielterminal in Gigametern (Gm).
    /// Berücksichtigt Himmelskörper, Stationen und intersystemische Sprungtore.
    /// </summary>
    private static double EstimateDistanceGm(string origin, string destination)
    {
        if (string.Equals(origin, destination, StringComparison.OrdinalIgnoreCase))
            return 0.0;

        string oBody = ResolveBody(origin);
        string dBody = ResolveBody(destination);

        // Identischer Himmelskörper / Mond / Orbit (z.B. Daymar -> Orison / Seraphim)
        if (string.Equals(oBody, dBody, StringComparison.OrdinalIgnoreCase))
            return 0.2;

        // Intersystemisch (Stanton <-> Pyro)
        bool oPyro = origin.Contains("Pyro", StringComparison.OrdinalIgnoreCase);
        bool dPyro = destination.Contains("Pyro", StringComparison.OrdinalIgnoreCase);
        if (oPyro != dPyro)
            return 580.0; // Jump Point Transit Distanz

        if (oPyro && dPyro)
            return 35.0; // Durchschnitt Pyro intern

        // Stanton Planet-zu-Planet Distanzmatrix (in Gm)
        return (oBody, dBody) switch
        {
            ("Hurston", "Crusader") or ("Crusader", "Hurston") => 31.8,
            ("Hurston", "ArcCorp") or ("ArcCorp", "Hurston") => 22.4,
            ("Hurston", "microTech") or ("microTech", "Hurston") => 45.1,
            ("Crusader", "ArcCorp") or ("ArcCorp", "Crusader") => 42.6,
            ("Crusader", "microTech") or ("microTech", "Crusader") => 57.9,
            ("ArcCorp", "microTech") or ("microTech", "ArcCorp") => 38.2,
            _ => 28.5 // Fallback Durchschnitt
        };
    }

    private static string ResolveBody(string location)
    {
        var s = location.ToLowerInvariant();
        if (s.Contains("hurston") || s.Contains("lorville") || s.Contains("everus") || s.Contains("arial") || s.Contains("aberdeen") || s.Contains("magda") || s.Contains("ita") || s.Contains("hur-l"))
            return "Hurston";
        if (s.Contains("crusader") || s.Contains("orison") || s.Contains("seraphim") || s.Contains("daymar") || s.Contains("cellin") || s.Contains("yela") || s.Contains("cru-l"))
            return "Crusader";
        if (s.Contains("arccorp") || s.Contains("area 18") || s.Contains("area18") || s.Contains("baijini") || s.Contains("lyria") || s.Contains("wala") || s.Contains("arc-l"))
            return "ArcCorp";
        if (s.Contains("microtech") || s.Contains("new babbage") || s.Contains("tressler") || s.Contains("calliope") || s.Contains("clio") || s.Contains("euterpe") || s.Contains("mic-l"))
            return "microTech";
        if (s.Contains("pyro") || s.Contains("ruin") || s.Contains("pyam") || s.Contains("checkmate") || s.Contains("monox"))
            return "Pyro";
        return "Stanton";
    }
}
