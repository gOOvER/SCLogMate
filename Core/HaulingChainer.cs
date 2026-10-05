using System;
using System.Collections.Generic;
using System.Linq;
using SCLogMate.Models;

namespace SCLogMate.Core;

/// <summary>
/// Multi-Contract Hauling Chainer & Frachtraum-Füllstandsprojektion Engine.
/// Sequences multi-contract hauling jobs into optimal routes, prevents cargo hold overbooking,
/// and computes step-by-step fill-level projections.
/// </summary>
public static class HaulingChainer
{
    private static readonly int[] StandardCrateSizes = [32, 24, 16, 8, 4, 2, 1];

    /// <summary>
    /// Gets the standard cargo capacity in SCU for a known ship or defaults to 96 SCU.
    /// </summary>
    public static int GetShipCapacity(string shipName)
    {
        if (string.IsNullOrWhiteSpace(shipName)) return 96;

        var matchedKey = CargoFit.ShipGrids.Keys.FirstOrDefault(k =>
            k.Contains(shipName, StringComparison.OrdinalIgnoreCase) ||
            shipName.Contains(k, StringComparison.OrdinalIgnoreCase));

        if (matchedKey != null && CargoFit.ShipGrids.TryGetValue(matchedKey, out var grids))
        {
            return grids.Sum(g => g.Scu);
        }

        return 96; // Fallback capacity
    }

    /// <summary>
    /// Breaks down a total SCU volume into the fewest number of standard Star Citizen container crates.
    /// </summary>
    public static Dictionary<int, int> CalculateBoxBreakdown(int totalScu)
    {
        var result = new Dictionary<int, int>();
        int remainder = totalScu;

        foreach (var size in StandardCrateSizes)
        {
            if (remainder <= 0) break;
            int count = remainder / size;
            if (count > 0)
            {
                result[size] = count;
                remainder %= size;
            }
        }

        return result;
    }

    /// <summary>
    /// Identifies the parent planetary sphere or star system for routing and clustering.
    /// </summary>
    public static (string CelestialBody, string System) ResolveCelestialBody(string location)
    {
        if (string.IsNullOrWhiteSpace(location)) return ("Stanton", "Stanton");

        var loc = location.ToLowerInvariant();

        if (loc.Contains("pyro") || loc.Contains("ruin") || loc.Contains("checkmate") || loc.Contains("monox") || loc.Contains("bloom"))
            return ("Pyro", "Pyro");

        if (loc.Contains("hurston") || loc.Contains("lorville") || loc.Contains("everus") || loc.Contains("arial") ||
            loc.Contains("aberdeen") || loc.Contains("magda") || loc.Contains("ita") || loc.Contains("hur-l") || loc.Contains("hdpc") || loc.Contains("hdms"))
            return ("Hurston", "Stanton");

        if (loc.Contains("crusader") || loc.Contains("orison") || loc.Contains("seraphim") || loc.Contains("daymar") ||
            loc.Contains("cellin") || loc.Contains("yela") || loc.Contains("cru-l") || loc.Contains("grim hex") || loc.Contains("covalex"))
            return ("Crusader", "Stanton");

        if (loc.Contains("arccorp") || loc.Contains("area 18") || loc.Contains("area18") || loc.Contains("baijini") ||
            loc.Contains("lyria") || loc.Contains("wala") || loc.Contains("arc-l"))
            return ("ArcCorp", "Stanton");

        if (loc.Contains("microtech") || loc.Contains("new babbage") || loc.Contains("tressler") || loc.Contains("calliope") ||
            loc.Contains("clio") || loc.Contains("euterpe") || loc.Contains("mic-l"))
            return ("microTech", "Stanton");

        return ("Stanton", "Stanton");
    }

    /// <summary>
    /// Computes the approximate quantum travel distance in Gm between two locations.
    /// </summary>
    public static double EstimateDistanceGm(string origin, string destination)
    {
        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(destination)) return 1.0;
        if (string.Equals(origin.Trim(), destination.Trim(), StringComparison.OrdinalIgnoreCase)) return 0.0;

        var (oBody, oSys) = ResolveCelestialBody(origin);
        var (dBody, dSys) = ResolveCelestialBody(destination);

        if (oSys != dSys) return 580.0; // Jump point transit distance
        if (string.Equals(oBody, dBody, StringComparison.OrdinalIgnoreCase)) return 0.25; // Intra-planetary / moon distance

        return (oBody, dBody) switch
        {
            ("Hurston", "Crusader") or ("Crusader", "Hurston") => 31.8,
            ("Hurston", "ArcCorp") or ("ArcCorp", "Hurston") => 22.4,
            ("Hurston", "microTech") or ("microTech", "Hurston") => 45.1,
            ("Crusader", "ArcCorp") or ("ArcCorp", "Crusader") => 42.6,
            ("Crusader", "microTech") or ("microTech", "Crusader") => 57.9,
            ("ArcCorp", "microTech") or ("microTech", "ArcCorp") => 38.2,
            _ => 30.0
        };
    }

    /// <summary>
    /// Estimates quantum travel and spooling/transit time in minutes for a given distance in Gm.
    /// </summary>
    public static double EstimateQtMinutes(double distanceGm)
    {
        if (distanceGm <= 0.01) return 0.0;
        if (distanceGm < 1.0) return 0.75; // Sub-orbital / local hop
        // Average medium/large ship QT speed ~ 12 Gm/min + spool/cooldown
        return Math.Round(0.8 + (distanceGm / 12.5), 1);
    }

    /// <summary>
    /// Optimizes and projects cargo capacity across multiple hauling contracts.
    /// </summary>
    public static HaulingChainedRouteResult CalculateHaulingChain(HaulingChainRequest request)
    {
        var shipName = !string.IsNullOrWhiteSpace(request.ShipName) ? request.ShipName : "Crusader C2 Hercules";
        int capacity = GetShipCapacity(shipName);

        var activeJobs = (request.Jobs ?? [])
            .Where(j => j.IsEnabled && j.Scu > 0 && !string.IsNullOrWhiteSpace(j.PickupLocation) && !string.IsNullOrWhiteSpace(j.DeliveryLocation))
            .ToList();

        if (activeJobs.Count == 0)
        {
            return new HaulingChainedRouteResult
            {
                ShipName = shipName,
                ShipCapacityScu = capacity,
                TotalJobs = 0,
                TotalWaypoints = 0,
                TotalRewardAuec = 0,
                TotalScuMoved = 0,
                PeakLoadScu = 0,
                PeakFillPercentage = 0,
                IsFeasible = true,
                TotalDistanceGm = 0,
                EstimatedTotalQtMinutes = 0,
                Waypoints = [],
                Warnings = ["Keine aktiven Frachtaufträge ausgewählt."],
                PeakBoxBreakdown = new Dictionary<int, int>()
            };
        }

        var warnings = new List<string>();

        // Step 1: Sequence the stops
        List<HaulingWaypointDto> waypoints;
        if (request.OptimizeOrder)
        {
            waypoints = OptimizeWaypoints(activeJobs, capacity, warnings);
        }
        else
        {
            waypoints = BuildSequentialWaypoints(activeJobs, capacity, warnings);
        }

        // Step 2: Project capacity and evaluate feasibility
        int peakLoad = 0;
        bool isFeasible = true;
        double totalDistance = 0;
        double totalQtMinutes = 0;

        for (int i = 0; i < waypoints.Count; i++)
        {
            var wp = waypoints[i];
            if (wp.CurrentLoadScu > peakLoad) peakLoad = wp.CurrentLoadScu;
            if (wp.IsOverloaded) isFeasible = false;

            if (i < waypoints.Count - 1)
            {
                totalDistance += wp.DistanceToNextGm;
                totalQtMinutes += wp.EstimatedQtMinutes;
            }
        }

        if (peakLoad > capacity)
        {
            warnings.Insert(0, $"Achtung: Spitzen-Frachtaufkommen ({peakLoad} SCU) übersteigt die Frachtkapazität ({capacity} SCU) um {peakLoad - capacity} SCU!");
        }

        long totalReward = activeJobs.Sum(j => j.RewardAuec);
        int totalScuMoved = activeJobs.Sum(j => j.Scu);
        double peakPercent = capacity > 0 ? Math.Round((double)peakLoad / capacity * 100, 1) : 0;
        var boxBreakdown = CalculateBoxBreakdown(peakLoad);

        return new HaulingChainedRouteResult
        {
            ShipName = shipName,
            ShipCapacityScu = capacity,
            TotalJobs = activeJobs.Count,
            TotalWaypoints = waypoints.Count,
            TotalRewardAuec = totalReward,
            TotalScuMoved = totalScuMoved,
            PeakLoadScu = peakLoad,
            PeakFillPercentage = peakPercent,
            IsFeasible = isFeasible,
            TotalDistanceGm = Math.Round(totalDistance, 1),
            EstimatedTotalQtMinutes = Math.Round(totalQtMinutes, 1),
            Waypoints = waypoints,
            Warnings = warnings,
            PeakBoxBreakdown = boxBreakdown
        };
    }

    private sealed record RawAction(HaulingJobItem Job, string Location, bool IsPickup, int ScuDelta);

    /// <summary>
    /// Greedy cluster-aware topological sequencer for multi-contract hauling.
    /// Ensures all pickups occur before corresponding deliveries, clusters stops by celestial body,
    /// and groups combined pickups/deliveries at the same location.
    /// </summary>
    private static List<HaulingWaypointDto> OptimizeWaypoints(
        List<HaulingJobItem> jobs,
        int shipCapacity,
        List<string> warnings)
    {
        var pickedUp = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var delivered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var rawStops = new List<RawAction>();
        string currentLocation = jobs[0].PickupLocation;

        int safetyMaxSteps = jobs.Count * 4;
        int stepCount = 0;

        while (delivered.Count < jobs.Count && stepCount++ < safetyMaxSteps)
        {
            // Collect all candidate operations currently available
            var availablePickups = jobs
                .Where(j => !pickedUp.Contains(j.Id))
                .Select(j => new RawAction(j, j.PickupLocation, true, j.Scu))
                .ToList();

            var availableDeliveries = jobs
                .Where(j => pickedUp.Contains(j.Id) && !delivered.Contains(j.Id))
                .Select(j => new RawAction(j, j.DeliveryLocation, false, -j.Scu))
                .ToList();

            var candidates = availablePickups.Concat(availableDeliveries).ToList();
            if (candidates.Count == 0) break;

            var (currentBody, _) = ResolveCelestialBody(currentLocation);

            // Score candidate by location affinity, celestial cluster, and cargo room
            var bestCandidate = candidates
                .OrderByDescending(c =>
                {
                    double score = 0;
                    bool isSameLoc = string.Equals(c.Location.Trim(), currentLocation.Trim(), StringComparison.OrdinalIgnoreCase);
                    var (candBody, _) = ResolveCelestialBody(c.Location);
                    bool isSameBody = string.Equals(candBody, currentBody, StringComparison.OrdinalIgnoreCase);

                    if (isSameLoc) score += 1000;
                    else if (isSameBody) score += 200;

                    // Prefer deliveries if at same body to free up space
                    if (!c.IsPickup && isSameBody) score += 50;
                    // Otherwise prefer pickups to consolidate payload
                    else if (c.IsPickup) score += 20;

                    return score;
                })
                .First();

            // Find all other immediate candidates that can be performed at this EXACT same location
            var sameLocBatch = candidates
                .Where(c => string.Equals(c.Location.Trim(), bestCandidate.Location.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var act in sameLocBatch)
            {
                rawStops.Add(act);
                if (act.IsPickup) pickedUp.Add(act.Job.Id);
                else delivered.Add(act.Job.Id);
            }

            currentLocation = bestCandidate.Location;
        }

        return ProjectStopsToWaypoints(rawStops, shipCapacity, warnings);
    }

    /// <summary>
    /// Sequential fallback ordering (Pickups in order, then Deliveries).
    /// </summary>
    private static List<HaulingWaypointDto> BuildSequentialWaypoints(
        List<HaulingJobItem> jobs,
        int shipCapacity,
        List<string> warnings)
    {
        var rawStops = new List<RawAction>();
        foreach (var j in jobs)
        {
            rawStops.Add(new RawAction(j, j.PickupLocation, true, j.Scu));
        }
        foreach (var j in jobs)
        {
            rawStops.Add(new RawAction(j, j.DeliveryLocation, false, -j.Scu));
        }

        return ProjectStopsToWaypoints(rawStops, shipCapacity, warnings);
    }

    /// <summary>
    /// Merges consecutive operations at the same station/outpost into structured Waypoints
    /// with running SCU load calculations and overload detection.
    /// </summary>
    private static List<HaulingWaypointDto> ProjectStopsToWaypoints(
        List<RawAction> rawStops,
        int shipCapacity,
        List<string> warnings)
    {
        var groupedWaypoints = new List<(string Location, List<RawAction> Actions)>();

        foreach (var act in rawStops)
        {
            if (groupedWaypoints.Count > 0 &&
                string.Equals(groupedWaypoints[^1].Location.Trim(), act.Location.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                groupedWaypoints[^1].Actions.Add(act);
            }
            else
            {
                groupedWaypoints.Add((act.Location.Trim(), [act]));
            }
        }

        var result = new List<HaulingWaypointDto>();
        int runningLoad = 0;

        for (int i = 0; i < groupedWaypoints.Count; i++)
        {
            var (location, acts) = groupedWaypoints[i];
            var (cBody, sys) = ResolveCelestialBody(location);

            int netDelta = acts.Sum(a => a.ScuDelta);
            runningLoad += netDelta;
            if (runningLoad < 0) runningLoad = 0; // Guard against negative transient

            bool isOverloaded = runningLoad > shipCapacity;
            int overloadAmount = Math.Max(0, runningLoad - shipCapacity);

            if (isOverloaded)
            {
                warnings.Add($"Überbuchung bei Halt #{i + 1} ({location}): {runningLoad}/{shipCapacity} SCU (+{overloadAmount} SCU Überschuss)!");
            }

            bool hasPickup = acts.Any(a => a.IsPickup);
            bool hasDelivery = acts.Any(a => !a.IsPickup);
            string actionType = (hasPickup && hasDelivery) ? "Combined" : (hasPickup ? "Pickup" : "Delivery");

            var cargoDetails = acts.Select(a => new HaulingWaypointCargoDetail
            {
                JobId = a.Job.Id,
                JobTitle = a.Job.Title,
                Commodity = a.Job.Commodity,
                ScuDelta = a.ScuDelta,
                ActionType = a.IsPickup ? "Pickup" : "Delivery"
            }).ToList();

            double distToNext = 0;
            double qtToNext = 0;

            if (i < groupedWaypoints.Count - 1)
            {
                distToNext = EstimateDistanceGm(location, groupedWaypoints[i + 1].Location);
                qtToNext = EstimateQtMinutes(distToNext);
            }

            double fillPercent = shipCapacity > 0 ? Math.Round((double)runningLoad / shipCapacity * 100, 1) : 0;

            result.Add(new HaulingWaypointDto
            {
                StepIndex = i + 1,
                Location = location,
                CelestialBody = cBody,
                System = sys,
                Action = actionType,
                DeltaScu = netDelta,
                CurrentLoadScu = runningLoad,
                CapacityScu = shipCapacity,
                FillPercentage = fillPercent,
                IsOverloaded = isOverloaded,
                OverloadAmountScu = overloadAmount,
                CargoDetails = cargoDetails,
                DistanceToNextGm = distToNext,
                EstimatedQtMinutes = qtToNext
            });
        }

        return result;
    }

    /// <summary>
    /// Returns authentic pre-configured multi-contract hauling scenarios for instant testing and demonstration.
    /// </summary>
    public static List<HaulingJobItem> GetPresetJobs(string presetKey = "hurston_express")
    {
        return presetKey.ToLowerInvariant() switch
        {
            "stanton_interplanetary" => [
                new() { Id = "haul_01", Title = "Covalex Linehaul: Beryl nach Orison", PickupLocation = "HDMS-Lathan (Arial)", DeliveryLocation = "Orison Cloudview (Crusader)", Scu = 48, Commodity = "Beryl", RewardAuec = 68500, Contractor = "Covalex Shipping" },
                new() { Id = "haul_02", Title = "Red Wind: Titan nach Baijini Point", PickupLocation = "HDMS-Bezdek (Arial)", DeliveryLocation = "Baijini Point (ArcCorp)", Scu = 32, Commodity = "Titanium", RewardAuec = 44200, Contractor = "Red Wind Line Haul" },
                new() { Id = "haul_03", Title = "Hurston Dynamics: Laranit nach Everus", PickupLocation = "HDMS-Perlman (Magda)", DeliveryLocation = "Everus Harbor (Hurston)", Scu = 32, Commodity = "Laranite", RewardAuec = 52000, Contractor = "Hurston Dynamics" },
                new() { Id = "haul_04", Title = "Ling: Medizinische Vorräte nach Orison", PickupLocation = "Everus Harbor (Hurston)", DeliveryLocation = "Orison Cloudview (Crusader)", Scu = 16, Commodity = "Medical Supplies", RewardAuec = 29500, Contractor = "Ling Family Hauling" }
            ],
            "distribution_center_run" => [
                new() { Id = "haul_10", Title = "HDPC-Cassidy Nachschub: Bauteile", PickupLocation = "HDPC-Cassidy (Hurston)", DeliveryLocation = "Lorville Teasa (Hurston)", Scu = 32, Commodity = "Processed Goods", RewardAuec = 38000, Contractor = "Hurston Dynamics Logistics" },
                new() { Id = "haul_11", Title = "Covalex Gundo: Abholung Everus", PickupLocation = "Everus Harbor (Hurston)", DeliveryLocation = "Lorville CBD (Hurston)", Scu = 24, Commodity = "Electronics", RewardAuec = 27500, Contractor = "Covalex Shipping" },
                new() { Id = "haul_12", Title = "Schrotttransport: Orinth nach Lorville", PickupLocation = "Reclamation Orinth (Hurston)", DeliveryLocation = "Lorville CBD (Hurston)", Scu = 16, Commodity = "Recycled Material Composite", RewardAuec = 21000, Contractor = "Reclamation & Disposal" }
            ],
            _ => [
                // Default: hurston_express
                new() { Id = "haul_h1", Title = "Everus Express: Beryll-Lieferung", PickupLocation = "HDMS-Lathan (Arial)", DeliveryLocation = "Everus Harbor (Hurston)", Scu = 32, Commodity = "Beryl", RewardAuec = 42000, Contractor = "Red Wind Line Haul" },
                new() { Id = "haul_h2", Title = "Lorville Industrie: Titan-Eilfracht", PickupLocation = "HDMS-Bezdek (Arial)", DeliveryLocation = "Lorville Teasa (Hurston)", Scu = 48, Commodity = "Titanium", RewardAuec = 59000, Contractor = "Hurston Dynamics" },
                new() { Id = "haul_h3", Title = "Everus Orbit: Laranit-Kontingent", PickupLocation = "HDMS-Woodward (Ita)", DeliveryLocation = "Everus Harbor (Hurston)", Scu = 16, Commodity = "Laranite", RewardAuec = 34500, Contractor = "Covalex Shipping" }
            ]
        };
    }
}
