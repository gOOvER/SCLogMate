using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SCLogMate.Models;

/// <summary>
/// A single hauling contract / cargo delivery job item.
/// </summary>
public sealed record HaulingJobItem
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("title")] public string Title { get; init; } = "";
    [JsonPropertyName("pickupLocation")] public string PickupLocation { get; init; } = "";
    [JsonPropertyName("deliveryLocation")] public string DeliveryLocation { get; init; } = "";
    [JsonPropertyName("scu")] public int Scu { get; init; }
    [JsonPropertyName("commodity")] public string Commodity { get; init; } = "Allgemeine Fracht";
    [JsonPropertyName("rewardAuec")] public long RewardAuec { get; init; }
    [JsonPropertyName("contractor")] public string Contractor { get; init; } = "Covalex / Red Wind";
    [JsonPropertyName("isEnabled")] public bool IsEnabled { get; init; } = true;
}

/// <summary>
/// Detail item handled at a specific route stop/waypoint.
/// </summary>
public sealed record HaulingWaypointCargoDetail
{
    [JsonPropertyName("jobId")] public string JobId { get; init; } = "";
    [JsonPropertyName("jobTitle")] public string JobTitle { get; init; } = "";
    [JsonPropertyName("commodity")] public string Commodity { get; init; } = "";
    [JsonPropertyName("scuDelta")] public int ScuDelta { get; init; }
    [JsonPropertyName("actionType")] public string ActionType { get; init; } = "Pickup";
}

/// <summary>
/// A single waypoint/stop along the chained hauling route with fill-level projection.
/// </summary>
public sealed record HaulingWaypointDto
{
    [JsonPropertyName("stepIndex")] public int StepIndex { get; init; }
    [JsonPropertyName("location")] public string Location { get; init; } = "";
    [JsonPropertyName("celestialBody")] public string CelestialBody { get; init; } = "Stanton";
    [JsonPropertyName("system")] public string System { get; init; } = "Stanton";
    [JsonPropertyName("action")] public string Action { get; init; } = "Pickup"; // Pickup, Delivery, Combined
    [JsonPropertyName("deltaScu")] public int DeltaScu { get; init; }
    [JsonPropertyName("currentLoadScu")] public int CurrentLoadScu { get; init; }
    [JsonPropertyName("capacityScu")] public int CapacityScu { get; init; }
    [JsonPropertyName("fillPercentage")] public double FillPercentage { get; init; }
    [JsonPropertyName("isOverloaded")] public bool IsOverloaded { get; init; }
    [JsonPropertyName("overloadAmountScu")] public int OverloadAmountScu { get; init; }
    [JsonPropertyName("cargoDetails")] public IReadOnlyList<HaulingWaypointCargoDetail> CargoDetails { get; init; } = [];
    [JsonPropertyName("distanceToNextGm")] public double DistanceToNextGm { get; init; }
    [JsonPropertyName("estimatedQtMinutes")] public double EstimatedQtMinutes { get; init; }
}

/// <summary>
/// Result of the chained multi-contract hauling route with capacity projection and warnings.
/// </summary>
public sealed record HaulingChainedRouteResult
{
    [JsonPropertyName("shipName")] public string ShipName { get; init; } = "";
    [JsonPropertyName("shipCapacityScu")] public int ShipCapacityScu { get; init; }
    [JsonPropertyName("totalJobs")] public int TotalJobs { get; init; }
    [JsonPropertyName("totalWaypoints")] public int TotalWaypoints { get; init; }
    [JsonPropertyName("totalRewardAuec")] public long TotalRewardAuec { get; init; }
    [JsonPropertyName("totalScuMoved")] public int TotalScuMoved { get; init; }
    [JsonPropertyName("peakLoadScu")] public int PeakLoadScu { get; init; }
    [JsonPropertyName("peakFillPercentage")] public double PeakFillPercentage { get; init; }
    [JsonPropertyName("isFeasible")] public bool IsFeasible { get; init; }
    [JsonPropertyName("totalDistanceGm")] public double TotalDistanceGm { get; init; }
    [JsonPropertyName("estimatedTotalQtMinutes")] public double EstimatedTotalQtMinutes { get; init; }
    [JsonPropertyName("waypoints")] public IReadOnlyList<HaulingWaypointDto> Waypoints { get; init; } = [];
    [JsonPropertyName("warnings")] public IReadOnlyList<string> Warnings { get; init; } = [];
    [JsonPropertyName("peakBoxBreakdown")] public IReadOnlyDictionary<int, int> PeakBoxBreakdown { get; init; } = new Dictionary<int, int>();
}

/// <summary>
/// Request payload to chain and optimize hauling contracts.
/// </summary>
public sealed record HaulingChainRequest
{
    [JsonPropertyName("shipName")] public string ShipName { get; init; } = "Crusader C2 Hercules";
    [JsonPropertyName("jobs")] public List<HaulingJobItem> Jobs { get; init; } = [];
    [JsonPropertyName("optimizeOrder")] public bool OptimizeOrder { get; init; } = true;
}
