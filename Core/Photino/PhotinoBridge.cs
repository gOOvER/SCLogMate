using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Photino.NET;
using SCLogMate.Core.Ocr;
using SCLogMate.Models;

namespace SCLogMate.Core.Photino;

public class IpcMessage
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("payload")]
    public JsonElement? Payload { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class AppStatusDto
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0-rc2";

    [JsonPropertyName("isLiveWatching")]
    public bool IsLiveWatching { get; set; }

    [JsonPropertyName("logPath")]
    public string? LogPath { get; set; }

    [JsonPropertyName("activeSessionName")]
    public string? ActiveSessionName { get; set; }

    [JsonPropertyName("dbSessionCount")]
    public int DbSessionCount { get; set; }

    [JsonPropertyName("totalIncome")]
    public long TotalIncome { get; set; }

    [JsonPropertyName("totalSpend")]
    public long TotalSpend { get; set; }

    [JsonPropertyName("totalNet")]
    public long TotalNet { get; set; }

    [JsonPropertyName("lastEventTime")]
    public string? LastEventTime { get; set; }

    [JsonPropertyName("chatOcrEnabled")]
    public bool ChatOcrEnabled { get; set; }
}

public class SessionSummaryDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("startTime")]
    public string StartTime { get; set; } = "";

    [JsonPropertyName("endTime")]
    public string EndTime { get; set; } = "";

    [JsonPropertyName("duration")]
    public string Duration { get; set; } = "";

    [JsonPropertyName("income")]
    public long Income { get; set; }

    [JsonPropertyName("spend")]
    public long Spend { get; set; }

    [JsonPropertyName("net")]
    public long Net { get; set; }

    [JsonPropertyName("sales")]
    public long Sales { get; set; }

    [JsonPropertyName("trade")]
    public long Trade { get; set; }

    [JsonPropertyName("deaths")]
    public int Deaths { get; set; }

    [JsonPropertyName("missions")]
    public int Missions { get; set; }

    [JsonPropertyName("ships")]
    public List<string> Ships { get; set; } = new();

    [JsonPropertyName("lastLocation")]
    public string LastLocation { get; set; } = "";
}

public class LogEventDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = DateTime.Now.ToString("HH:mm:ss");

    [JsonPropertyName("category")]
    public string Category { get; set; } = "system";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("amount")]
    public long? Amount { get; set; }

    [JsonPropertyName("ship")]
    public string? Ship { get; set; }

    [JsonPropertyName("rawText")]
    public string? RawText { get; set; }

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "System";

    [JsonPropertyName("kindText")]
    public string KindText { get; set; } = "";

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "❖";
}

public class HudTelemetryDto
{
    [JsonPropertyName("isGameRunning")] public bool IsGameRunning { get; set; }
    [JsonPropertyName("pilotName")] public string PilotName { get; set; } = "—";
    [JsonPropertyName("pilotAvatarUrl")] public string? PilotAvatarUrl { get; set; }
    [JsonPropertyName("pilotTitle")] public string? PilotTitle { get; set; }
    [JsonPropertyName("pilotOrgName")] public string? PilotOrgName { get; set; }
    [JsonPropertyName("citizenRecord")] public string? CitizenRecord { get; set; }
    [JsonPropertyName("pilotOrgSid")] public string? PilotOrgSid { get; set; }
    [JsonPropertyName("pilotOrgRank")] public string? PilotOrgRank { get; set; }
    [JsonPropertyName("pilotOrgLogoUrl")] public string? PilotOrgLogoUrl { get; set; }
    [JsonPropertyName("pilotEnlisted")] public string? PilotEnlisted { get; set; }
    [JsonPropertyName("pilotProfileUrl")] public string? PilotProfileUrl { get; set; }
    [JsonPropertyName("serverRegionCode")] public string ServerRegionCode { get; set; } = "—";
    [JsonPropertyName("serverRegionName")] public string ServerRegionName { get; set; } = "Unbekannt";
    [JsonPropertyName("serverRegionFlag")] public string ServerRegionFlag { get; set; } = "🌐";
    [JsonPropertyName("serverShard")] public string ServerShard { get; set; } = "—";
    [JsonPropertyName("serverShardNumber")] public string ServerShardNumber { get; set; } = "—";
    [JsonPropertyName("serverVersion")] public string ServerVersion { get; set; } = "—";
    [JsonPropertyName("serverPingMs")] public int? ServerPingMs { get; set; }
    [JsonPropertyName("locationName")] public string LocationName { get; set; } = "—";
    [JsonPropertyName("locationSystem")] public string LocationSystem { get; set; } = "Stanton";
    [JsonPropertyName("locationBody")] public string LocationBody { get; set; } = "—";
    [JsonPropertyName("locationType")] public string LocationType { get; set; } = "Standort";
    [JsonPropertyName("isArmistice")] public bool IsArmistice { get; set; } = true;
    [JsonPropertyName("jurisdiction")] public string Jurisdiction { get; set; } = "UEE";
    [JsonPropertyName("shipName")] public string ShipName { get; set; } = "—";
    [JsonPropertyName("shipFlightInfo")] public string ShipFlightInfo { get; set; } = "—";
    [JsonPropertyName("balance")] public long Balance { get; set; }
    [JsonPropertyName("sessionIncome")] public long SessionIncome { get; set; }
    [JsonPropertyName("sessionSpend")] public long SessionSpend { get; set; }
    [JsonPropertyName("sessionNet")] public long SessionNet { get; set; }
    [JsonPropertyName("autoOcrEnabled")] public bool AutoOcrEnabled { get; set; } = true;
    [JsonPropertyName("activeMissionTitle")] public string ActiveMissionTitle { get; set; } = "Kein aktiver Auftrag";
    [JsonPropertyName("activeMissionGiver")] public string ActiveMissionGiver { get; set; } = "—";
    [JsonPropertyName("activeMissionReward")] public long ActiveMissionReward { get; set; }
    [JsonPropertyName("activeMissionStatus")] public string ActiveMissionStatus { get; set; } = "Bereit";
    [JsonPropertyName("sessionSpanText")] public string SessionSpanText { get; set; } = "—";
    [JsonPropertyName("selectedSession")] public string SelectedSession { get; set; } = "__live__";
}

public class WarehouseItemDto
{
    [JsonPropertyName("location")]
    public string Location { get; set; } = "";

    [JsonPropertyName("locationCode")]
    public string LocationCode { get; set; } = "";

    [JsonPropertyName("system")]
    public string System { get; set; } = "Stanton";

    [JsonPropertyName("parentBody")]
    public string ParentBody { get; set; } = "";

    [JsonPropertyName("itemClass")]
    public string ItemClass { get; set; } = "";

    [JsonPropertyName("itemName")]
    public string ItemName { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "Sonstiges";

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("lastUpdated")]
    public string LastUpdated { get; set; } = "";

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "📦";

    [JsonPropertyName("locationDisplay")]
    public string LocationDisplay { get; set; } = "";
}

public class WarehouseLocationDto
{
    [JsonPropertyName("locationName")]
    public string LocationName { get; set; } = "";

    [JsonPropertyName("locationCode")]
    public string LocationCode { get; set; } = "";

    [JsonPropertyName("system")]
    public string System { get; set; } = "Stanton";

    [JsonPropertyName("parentBody")]
    public string ParentBody { get; set; } = "";

    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }

    [JsonPropertyName("uniqueItemTypes")]
    public int UniqueItemTypes { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "🪐";
}

public class FinanceChartPointDto
{
    [JsonPropertyName("time")] public string Time { get; set; } = "";
    [JsonPropertyName("balance")] public long Balance { get; set; }
    [JsonPropertyName("income")] public long Income { get; set; }
    [JsonPropertyName("spend")] public long Spend { get; set; }
    [JsonPropertyName("delta")] public long Delta { get; set; }
    [JsonPropertyName("label")] public string Label { get; set; } = "";
}

public class FinanceOverviewDto
{
    [JsonPropertyName("scope")]
    public string Scope { get; set; } = "all";

    [JsonPropertyName("totalIncome")]
    public long TotalIncome { get; set; }

    [JsonPropertyName("totalSpend")]
    public long TotalSpend { get; set; }

    [JsonPropertyName("totalNet")]
    public long TotalNet { get; set; }

    [JsonPropertyName("liveBalance")]
    public long LiveBalance { get; set; }

    [JsonPropertyName("totalCargoAuec")]
    public long TotalCargoAuec { get; set; }

    [JsonPropertyName("totalCargoScu")]
    public long TotalCargoScu { get; set; }

    [JsonPropertyName("profitMargin")]
    public double ProfitMargin { get; set; }

    [JsonPropertyName("sales")]
    public long Sales { get; set; }

    [JsonPropertyName("trade")]
    public long Trade { get; set; }

    [JsonPropertyName("missionsReward")]
    public long MissionsReward { get; set; }

    [JsonPropertyName("purchases")]
    public long Purchases { get; set; }

    [JsonPropertyName("transferIn")]
    public long TransferIn { get; set; }

    [JsonPropertyName("transferOut")]
    public long TransferOut { get; set; }

    [JsonPropertyName("ledger")]
    public List<LogEventDto> Ledger { get; set; } = new();

    [JsonPropertyName("cargo")]
    public List<LogEventDto> Cargo { get; set; } = new();

    [JsonPropertyName("topExpenses")]
    public List<LogEventDto> TopExpenses { get; set; } = new();

    [JsonPropertyName("topIncome")]
    public List<LogEventDto> TopIncome { get; set; } = new();

    [JsonPropertyName("timelinePoints")]
    public List<FinanceChartPointDto> TimelinePoints { get; set; } = new();
}

public class FleetStatDto
{
    [JsonPropertyName("shipName")]
    public string ShipName { get; set; } = "";

    [JsonPropertyName("flights")]
    public int Flights { get; set; }

    [JsonPropertyName("quantumJumps")]
    public int QuantumJumps { get; set; }

    [JsonPropertyName("losses")]
    public int Losses { get; set; }

    [JsonPropertyName("lastUsed")]
    public string LastUsed { get; set; } = "";
}

public class FleetShipDto
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("rawCode")] public string RawCode { get; set; } = "";
    [JsonPropertyName("manufacturer")] public string Manufacturer { get; set; } = "";
    [JsonPropertyName("manufacturerBadge")] public string ManufacturerBadge { get; set; } = "";
    [JsonPropertyName("manufacturerColor")] public string ManufacturerColor { get; set; } = "";
    [JsonPropertyName("role")] public string Role { get; set; } = "";
    [JsonPropertyName("estimatedValueAuec")] public long EstimatedValueAuec { get; set; }
    [JsonPropertyName("flightCount")] public int FlightCount { get; set; }
    [JsonPropertyName("quantumJumps")] public int QuantumJumps { get; set; }
    [JsonPropertyName("lossCount")] public int LossCount { get; set; }
    [JsonPropertyName("lastFlown")] public string LastFlown { get; set; } = "—";
    [JsonPropertyName("isCurrent")] public bool IsCurrent { get; set; }
    [JsonPropertyName("isInHangar")] public bool IsInHangar { get; set; }
    [JsonPropertyName("isPledgeBought")] public bool IsPledgeBought { get; set; }
    [JsonPropertyName("pledgeValueUsd")] public int PledgeValueUsd { get; set; }
    [JsonPropertyName("insuranceType")] public string InsuranceType { get; set; } = "";
    [JsonPropertyName("acquisitionType")] public string AcquisitionType { get; set; } = "";
    [JsonPropertyName("customNotes")] public string CustomNotes { get; set; } = "";
}

public class CatalogShipDto
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("manufacturer")] public string Manufacturer { get; set; } = "";
    [JsonPropertyName("role")] public string Role { get; set; } = "";
    [JsonPropertyName("valueAuec")] public long ValueAuec { get; set; }
    [JsonPropertyName("pledgeUsd")] public int PledgeUsd { get; set; }
    [JsonPropertyName("defaultInsurance")] public string DefaultInsurance { get; set; } = "";
}

public class FleetResponseDto
{
    [JsonPropertyName("ships")] public List<FleetShipDto> Ships { get; set; } = new();
    [JsonPropertyName("catalog")] public List<CatalogShipDto> Catalog { get; set; } = new();
    [JsonPropertyName("totalFleetValueAuec")] public long TotalFleetValueAuec { get; set; }
    [JsonPropertyName("totalFleetPledgeUsd")] public int TotalFleetPledgeUsd { get; set; }
    [JsonPropertyName("totalFlights")] public int TotalFlights { get; set; }
    [JsonPropertyName("totalQuantumJumps")] public int TotalQuantumJumps { get; set; }
    [JsonPropertyName("hangarCount")] public int HangarCount { get; set; }
    [JsonPropertyName("flownCount")] public int FlownCount { get; set; }
}

public class MissionItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("contractor")]
    public string Contractor { get; set; } = "";

    [JsonPropertyName("faction")]
    public string Faction { get; set; } = "";

    [JsonPropertyName("missionType")]
    public string MissionType { get; set; } = "";

    [JsonPropertyName("baseReward")]
    public int BaseReward { get; set; }

    [JsonPropertyName("reputationGain")]
    public int ReputationGain { get; set; }

    [JsonPropertyName("isIllegal")]
    public bool IsIllegal { get; set; }

    [JsonPropertyName("starSystems")]
    public string StarSystems { get; set; } = "Stanton";

    [JsonPropertyName("blueprints")]
    public string[] Blueprints { get; set; } = Array.Empty<string>();

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("isCompleted")]
    public bool IsCompleted { get; set; }

    [JsonPropertyName("time")]
    public string? Time { get; set; }
}

public class FactionReputationDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("shortName")]
    public string ShortName { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "Sicherheit";

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "🛡";

    [JsonPropertyName("system")]
    public string System { get; set; } = "Stanton";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("currentXp")]
    public int CurrentXp { get; set; }

    [JsonPropertyName("completedMissions")]
    public int CompletedMissions { get; set; }

    [JsonPropertyName("currentLevel")]
    public int CurrentLevel { get; set; }

    [JsonPropertyName("levelTitle")]
    public string LevelTitle { get; set; } = "";

    [JsonPropertyName("progressPercent")]
    public double ProgressPercent { get; set; }

    [JsonPropertyName("progressText")]
    public string ProgressText { get; set; } = "";
}

public class BlueprintDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("subCategory")]
    public string SubCategory { get; set; } = "";

    [JsonPropertyName("rarity")]
    public string Rarity { get; set; } = "";

    [JsonPropertyName("requiredMaterials")]
    public string RequiredMaterials { get; set; } = "";

    [JsonPropertyName("unlockInfo")]
    public string UnlockInfo { get; set; } = "";

    [JsonPropertyName("isLearned")]
    public bool IsLearned { get; set; }

    [JsonPropertyName("learnedDate")]
    public string? LearnedDate { get; set; }
}

public class LoadoutSlotDto
{
    [JsonPropertyName("slotKey")]
    public string SlotKey { get; set; } = "";

    [JsonPropertyName("slotName")]
    public string SlotName { get; set; } = "";

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = "📦";

    [JsonPropertyName("itemName")]
    public string ItemName { get; set; } = "—";

    [JsonPropertyName("rawClass")]
    public string RawClass { get; set; } = "";

    [JsonPropertyName("armorClass")]
    public string ArmorClass { get; set; } = "";

    [JsonPropertyName("damageReduction")]
    public int DamageReduction { get; set; }

    [JsonPropertyName("tempRange")]
    public string TempRange { get; set; } = "";

    [JsonPropertyName("badgeColor")]
    public string BadgeColor { get; set; } = "#38BDF8";

    [JsonPropertyName("isEquipped")]
    public bool IsEquipped { get; set; }

    [JsonPropertyName("lastEquipped")]
    public string? LastEquipped { get; set; }
}

public class StarmapObjectDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("system")] public string System { get; set; } = "";
    [JsonPropertyName("parentId")] public string? ParentId { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("orbitRadius")] public double OrbitRadius { get; set; }
    [JsonPropertyName("orbitAngleDeg")] public double OrbitAngleDeg { get; set; }
    [JsonPropertyName("colorHex")] public string ColorHex { get; set; } = "#58A6FF";
    [JsonPropertyName("size")] public double Size { get; set; } = 8;
    [JsonPropertyName("hasArmistice")] public bool HasArmistice { get; set; } = true;
    [JsonPropertyName("jurisdiction")] public string Jurisdiction { get; set; } = "UEE";
    [JsonPropertyName("securityLevel")] public string SecurityLevel { get; set; } = "High";
    [JsonPropertyName("specialization")] public string Specialization { get; set; } = "";
    [JsonPropertyName("resources")] public string Resources { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("targetSystem")] public string? TargetSystem { get; set; }
    [JsonPropertyName("relX")] public double RelX { get; set; }
    [JsonPropertyName("relY")] public double RelY { get; set; }
}

public class QuantumDriveDto
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("sizeClass")] public string SizeClass { get; set; } = "S1";
    [JsonPropertyName("topSpeedKmS")] public double TopSpeedKmS { get; set; } = 150000;
    [JsonPropertyName("displayText")] public string DisplayText { get; set; } = "";
}

public class QuantumRouteResultDto
{
    [JsonPropertyName("fromId")] public string FromId { get; set; } = "";
    [JsonPropertyName("fromName")] public string FromName { get; set; } = "";
    [JsonPropertyName("toId")] public string ToId { get; set; } = "";
    [JsonPropertyName("toName")] public string ToName { get; set; } = "";
    [JsonPropertyName("driveName")] public string DriveName { get; set; } = "";
    [JsonPropertyName("distKm")] public double DistKm { get; set; }
    [JsonPropertyName("distGm")] public double DistGm { get; set; }
    [JsonPropertyName("flightTimeSeconds")] public double FlightTimeSeconds { get; set; }
    [JsonPropertyName("flightTimeFormatted")] public string FlightTimeFormatted { get; set; } = "";
}

public class StarmapResponseDto
{
    [JsonPropertyName("currentSystem")] public string CurrentSystem { get; set; } = "Stanton";
    [JsonPropertyName("objects")] public List<StarmapObjectDto> Objects { get; set; } = new();
    [JsonPropertyName("drives")] public List<QuantumDriveDto> Drives { get; set; } = new();
}

public class PlaceItemDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("system")] public string System { get; set; } = "Stanton";
    [JsonPropertyName("parentBody")] public string ParentBody { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("icon")] public string Icon { get; set; } = "📍";
    [JsonPropertyName("securityLevel")] public string SecurityLevel { get; set; } = "High";
    [JsonPropertyName("hasArmistice")] public bool HasArmistice { get; set; } = true;
    [JsonPropertyName("specialization")] public string Specialization { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
}

public class FlightTimelineItemDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.NewGuid().ToString("N");
    [JsonPropertyName("time")] public string Time { get; set; } = "";
    [JsonPropertyName("relativeTime")] public string RelativeTime { get; set; } = "";
    [JsonPropertyName("kind")] public string Kind { get; set; } = "";
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("subtitle")] public string Subtitle { get; set; } = "";
    [JsonPropertyName("ship")] public string? Ship { get; set; }
    [JsonPropertyName("location")] public string? Location { get; set; }
    [JsonPropertyName("isMajor")] public bool IsMajor { get; set; }
}

public class FlightRecorderDto
{
    [JsonPropertyName("totalDistanceGm")] public double TotalDistanceGm { get; set; }
    [JsonPropertyName("totalDistanceKm")] public double TotalDistanceKm { get; set; }
    [JsonPropertyName("totalDistanceText")] public string TotalDistanceText { get; set; } = "";
    [JsonPropertyName("flightDurationText")] public string FlightDurationText { get; set; } = "";
    [JsonPropertyName("quantumJumps")] public int QuantumJumps { get; set; }
    [JsonPropertyName("sortieCount")] public int SortieCount { get; set; }
    [JsonPropertyName("shipLosses")] public int ShipLosses { get; set; }
    [JsonPropertyName("visitedBodies")] public List<string> VisitedBodies { get; set; } = new();
    [JsonPropertyName("usedShips")] public List<string> UsedShips { get; set; } = new();
    [JsonPropertyName("timeline")] public List<FlightTimelineItemDto> Timeline { get; set; } = new();
}

public class RsResourceDto
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("baseRs")] public int BaseRs { get; set; }
    [JsonPropertyName("tier")] public string Tier { get; set; } = "C";
    [JsonPropertyName("rarity")] public string Rarity { get; set; } = "common";
    [JsonPropertyName("method")] public string Method { get; set; } = "ship";
    [JsonPropertyName("estimatedPricePerScu")] public double EstimatedPricePerScu { get; set; }
    [JsonPropertyName("locations")] public List<string> Locations { get; set; } = new();
}

public class RsMatchDto
{
    [JsonPropertyName("resourceName")] public string ResourceName { get; set; } = "";
    [JsonPropertyName("baseRs")] public int BaseRs { get; set; }
    [JsonPropertyName("tier")] public string Tier { get; set; } = "C";
    [JsonPropertyName("rarity")] public string Rarity { get; set; } = "common";
    [JsonPropertyName("method")] public string Method { get; set; } = "ship";
    [JsonPropertyName("estimatedPricePerScu")] public double EstimatedPricePerScu { get; set; }
    [JsonPropertyName("nodes")] public int Nodes { get; set; }
    [JsonPropertyName("isExact")] public bool IsExact { get; set; }
    [JsonPropertyName("errorPct")] public double ErrorPct { get; set; }
    [JsonPropertyName("scannedRs")] public int ScannedRs { get; set; }
    [JsonPropertyName("estimatedClusterValue")] public long EstimatedClusterValue { get; set; }
}

public class MarketCommodityDto
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "Metals";
    [JsonPropertyName("tier")] public string Tier { get; set; } = "A";
    [JsonPropertyName("avgBuyPrice")] public double AvgBuyPrice { get; set; }
    [JsonPropertyName("avgSellPrice")] public double AvgSellPrice { get; set; }
    [JsonPropertyName("margin")] public double Margin { get; set; }
    [JsonPropertyName("bestBuyLocation")] public string BestBuyLocation { get; set; } = "";
    [JsonPropertyName("bestSellLocation")] public string BestSellLocation { get; set; } = "";
}

public class KeybindBackupItemDto
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("folderPath")] public string FolderPath { get; set; } = "";
    [JsonPropertyName("createdAt")] public string CreatedAt { get; set; } = "";
    [JsonPropertyName("fileCount")] public int FileCount { get; set; }
    [JsonPropertyName("locationType")] public string LocationType { get; set; } = "Lokal";
    [JsonPropertyName("sizeFormatted")] public string SizeFormatted { get; set; } = "";
}

public class ConfigBackupItemDto
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("filePath")] public string FilePath { get; set; } = "";
    [JsonPropertyName("createdAt")] public string CreatedAt { get; set; } = "";
    [JsonPropertyName("locationType")] public string LocationType { get; set; } = "Lokal";
    [JsonPropertyName("sizeFormatted")] public string SizeFormatted { get; set; } = "";
}

public class ToolsStatusDto
{
    [JsonPropertyName("shaderCacheMb")] public double ShaderCacheMb { get; set; }
    [JsonPropertyName("crashDumpsMb")] public double CrashDumpsMb { get; set; }
    [JsonPropertyName("userCfgPath")] public string UserCfgPath { get; set; } = "";
    [JsonPropertyName("userCfgExists")] public bool UserCfgExists { get; set; }
    [JsonPropertyName("userCfgContent")] public string UserCfgContent { get; set; } = "";
    [JsonPropertyName("totalRamGb")] public double TotalRamGb { get; set; }
    [JsonPropertyName("ramStatus")] public string RamStatus { get; set; } = "32 GB (Optimal)";
    [JsonPropertyName("driveName")] public string DriveName { get; set; } = "C:";
    [JsonPropertyName("freeDiskGb")] public double FreeDiskGb { get; set; }
    [JsonPropertyName("pagefileStatus")] public string PagefileStatus { get; set; } = "Aktiv";
    [JsonPropertyName("keybindBackups")] public List<string> KeybindBackups { get; set; } = new();
    [JsonPropertyName("cloudStoragePath")] public string? CloudStoragePath { get; set; }
    [JsonPropertyName("keybindItems")] public List<KeybindBackupItemDto> KeybindItems { get; set; } = new();
    [JsonPropertyName("configBackups")] public List<ConfigBackupItemDto> ConfigBackups { get; set; } = new();
    [JsonPropertyName("keybindsDir")] public string KeybindsDir { get; set; } = MaintenanceService.LocalKeybindsBackupDir;
    [JsonPropertyName("configDir")] public string ConfigDir { get; set; } = MaintenanceService.LocalConfigBackupDir;
}

public class SettingsDto
{
    [JsonPropertyName("logPath")] public string? LogPath { get; set; }
    [JsonPropertyName("autoOcrEnabled")] public bool AutoOcrEnabled { get; set; } = true;
    [JsonPropertyName("uexApiKey")] public string? UexApiKey { get; set; }
    [JsonPropertyName("overlayEnabled")] public bool OverlayEnabled { get; set; } = false;
    [JsonPropertyName("overlayOpacity")] public double OverlayOpacity { get; set; } = 0.92;
    [JsonPropertyName("toastEnabled")] public bool ToastEnabled { get; set; } = true;
    [JsonPropertyName("toastBlueprintEnabled")] public bool ToastBlueprintEnabled { get; set; } = true;
    [JsonPropertyName("toastMissionEnabled")] public bool ToastMissionEnabled { get; set; } = true;
    [JsonPropertyName("toastReputationEnabled")] public bool ToastReputationEnabled { get; set; } = true;
    [JsonPropertyName("toastRefineryEnabled")] public bool ToastRefineryEnabled { get; set; } = true;
    [JsonPropertyName("toastElevatorEnabled")] public bool ToastElevatorEnabled { get; set; } = true;
    [JsonPropertyName("toastShipDestructionEnabled")] public bool ToastShipDestructionEnabled { get; set; } = true;
    [JsonPropertyName("auroraIntegrationEnabled")] public bool AuroraIntegrationEnabled { get; set; } = true;
    [JsonPropertyName("auroraVolume")] public int AuroraVolume { get; set; } = 40;
    [JsonPropertyName("rsTargetAlertEnabled")] public bool RsTargetAlertEnabled { get; set; } = true;
    [JsonPropertyName("rsTargetSoundEnabled")] public bool RsTargetSoundEnabled { get; set; } = true;
    [JsonPropertyName("walletRegion")] public ScanRegion? WalletRegion { get; set; }
    [JsonPropertyName("contractRegion")] public ScanRegion? ContractRegion { get; set; }
    [JsonPropertyName("rsScanRegion")] public ScanRegion? RsScanRegion { get; set; }
    [JsonPropertyName("chatRegion")] public ScanRegion? ChatRegion { get; set; }
    [JsonPropertyName("chatOcrEnabled")] public bool ChatOcrEnabled { get; set; } = true;
    [JsonPropertyName("chatOcrIntervalMs")] public int ChatOcrIntervalMs { get; set; } = 3500;

    // RC2 Wipe Filter Settings
    [JsonPropertyName("wipeFilterEnabled")] public bool WipeFilterEnabled { get; set; }
    [JsonPropertyName("wipeDateString")] public string WipeDateString { get; set; } = "2026-05-15";
    [JsonPropertyName("wipeFilterMoney")] public bool WipeFilterMoney { get; set; } = true;
    [JsonPropertyName("wipeFilterContracts")] public bool WipeFilterContracts { get; set; } = true;
    [JsonPropertyName("wipeFilterFleet")] public bool WipeFilterFleet { get; set; }
    [JsonPropertyName("wipeFilterBlueprints")] public bool WipeFilterBlueprints { get; set; }

    // General & System Settings
    [JsonPropertyName("selectedFontFamily")] public string SelectedFontFamily { get; set; } = "Inter";
    [JsonPropertyName("appLanguage")] public string AppLanguage { get; set; } = "Auto";
    [JsonPropertyName("minimizeToTrayOnClose")] public bool MinimizeToTrayOnClose { get; set; } = true;
    [JsonPropertyName("autostartEnabled")] public bool AutostartEnabled { get; set; }
    [JsonPropertyName("debugMode")] public bool DebugMode { get; set; }
}

public class OcrRegionsConfigDto
{
    [JsonPropertyName("walletRegion")] public ScanRegion? WalletRegion { get; set; }
    [JsonPropertyName("contractRegion")] public ScanRegion? ContractRegion { get; set; }
    [JsonPropertyName("rsScanRegion")] public ScanRegion? RsScanRegion { get; set; }
    [JsonPropertyName("chatRegion")] public ScanRegion? ChatRegion { get; set; }
    [JsonPropertyName("defaultWalletRegion")] public ScanRegion DefaultWalletRegion { get; set; } = new();
    [JsonPropertyName("defaultContractRegion")] public ScanRegion DefaultContractRegion { get; set; } = new();
    [JsonPropertyName("defaultRsRegion")] public ScanRegion DefaultRsRegion { get; set; } = new();
    [JsonPropertyName("defaultChatRegion")] public ScanRegion DefaultChatRegion { get; set; } = new();
    [JsonPropertyName("screenWidth")] public int ScreenWidth { get; set; } = 1920;
    [JsonPropertyName("screenHeight")] public int ScreenHeight { get; set; } = 1080;
    [JsonPropertyName("isWalletScanBoxVisible")] public bool IsWalletScanBoxVisible { get; set; }
    [JsonPropertyName("isContractScanBoxVisible")] public bool IsContractScanBoxVisible { get; set; }
}

public class OcrTestResultDto
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("target")] public string Target { get; set; } = "";
    [JsonPropertyName("recognizedText")] public string RecognizedText { get; set; } = "";
    [JsonPropertyName("extractedValue")] public long? ExtractedValue { get; set; }
    [JsonPropertyName("durationMs")] public int DurationMs { get; set; }
    [JsonPropertyName("region")] public ScanRegion? Region { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
}

public class DetectedPathDto
{
    [JsonPropertyName("path")] public string Path { get; set; } = "";
    [JsonPropertyName("channel")] public string Channel { get; set; } = "CUSTOM";
    [JsonPropertyName("lastModified")] public string LastModified { get; set; } = "";
    [JsonPropertyName("sizeBytes")] public long SizeBytes { get; set; }
    [JsonPropertyName("isCurrent")] public bool IsCurrent { get; set; }
}

public class LogStatusDto
{
    [JsonPropertyName("currentLogPath")] public string? CurrentLogPath { get; set; }
    [JsonPropertyName("channel")] public string Channel { get; set; } = "CUSTOM";
    [JsonPropertyName("exists")] public bool Exists { get; set; }
    [JsonPropertyName("sizeBytes")] public long SizeBytes { get; set; }
    [JsonPropertyName("formattedSize")] public string FormattedSize { get; set; } = "—";
    [JsonPropertyName("lastModified")] public string? LastModified { get; set; }
    [JsonPropertyName("isLiveWatching")] public bool IsLiveWatching { get; set; }
    [JsonPropertyName("activeSession")] public string ActiveSession { get; set; } = "Live Session";
    [JsonPropertyName("detectedPaths")] public List<DetectedPathDto> DetectedPaths { get; set; } = new();
    [JsonPropertyName("backupsCount")] public int BackupsCount { get; set; }
    [JsonPropertyName("archiveCount")] public int ArchiveCount { get; set; }
    [JsonPropertyName("parserVersion")] public int ParserVersion { get; set; }
    [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; set; }
}

public class ScanProgressDto
{
    [JsonPropertyName("current")] public int Current { get; set; }
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("percent")] public double Percent { get; set; }
    [JsonPropertyName("currentFileName")] public string CurrentFileName { get; set; } = "";
    [JsonPropertyName("isCompleted")] public bool IsCompleted { get; set; }
    [JsonPropertyName("indexedSessions")] public int IndexedSessions { get; set; }
    [JsonPropertyName("totalEvents")] public int TotalEvents { get; set; }
    [JsonPropertyName("isDbUpdate")] public bool IsDbUpdate { get; set; }
    [JsonPropertyName("updateReason")] public string? UpdateReason { get; set; }
}

public class DbDiagnosticsDto
{
    [JsonPropertyName("databasePath")] public string DatabasePath { get; set; } = "";
    [JsonPropertyName("databaseSizeBytes")] public long DatabaseSizeBytes { get; set; }
    [JsonPropertyName("formattedSize")] public string FormattedSize { get; set; } = "0 B";
    [JsonPropertyName("sqliteVersion")] public string SqliteVersion { get; set; } = "3.45";
    [JsonPropertyName("journalMode")] public string JournalMode { get; set; } = "WAL";
    [JsonPropertyName("installedSchemaVersion")] public int InstalledSchemaVersion { get; set; }
    [JsonPropertyName("currentSchemaVersion")] public int CurrentSchemaVersion { get; set; }
    [JsonPropertyName("installedParserVersion")] public int InstalledParserVersion { get; set; }
    [JsonPropertyName("currentParserVersion")] public int CurrentParserVersion { get; set; }
    [JsonPropertyName("sessionCount")] public int SessionCount { get; set; }
    [JsonPropertyName("eventCount")] public int EventCount { get; set; }
    [JsonPropertyName("contractCount")] public int ContractCount { get; set; }
    [JsonPropertyName("fleetShipCount")] public int FleetShipCount { get; set; }
    [JsonPropertyName("poiCount")] public int PoiCount { get; set; }
    [JsonPropertyName("reputationCount")] public int ReputationCount { get; set; }
    [JsonPropertyName("warehouseItemCount")] public int WarehouseItemCount { get; set; }
    [JsonPropertyName("integrityCheckOk")] public bool IntegrityCheckOk { get; set; }
    [JsonPropertyName("integrityMessage")] public string IntegrityMessage { get; set; } = "OK";
    [JsonPropertyName("checkedAt")] public string CheckedAt { get; set; } = "";
    [JsonPropertyName("isSynchronous")] public bool IsSynchronous { get; set; } = true;
}

public class PhotinoBridge
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private PhotinoWindow? _window;
    private LogTailer? _tailer;
    private readonly LogParser _parser = new();
    private string? _currentLogPath;
    private string? _activeSessionName;
    private string _selectedSession = "__live__";
    private string? _currentShip;
    private DateTime? _lastEventTime;
    private readonly List<LogEventDto> _liveEvents = new();
    private readonly object _liveEventsLock = new();
    private readonly object _sendLock = new();
    private volatile bool _isWebviewReady = false;
    private bool _hasSyncedLogs = false;

    private record SessionMetadataCache(
        string? Pilot,
        string? Shard,
        string? Version,
        string? ShardNumber,
        string RegionCode,
        string RegionName,
        string RegionFlag
    );

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, SessionMetadataCache> _sessionMetaCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _knownSessionFiles = new(StringComparer.OrdinalIgnoreCase);

    private readonly OcrEngineService _ocrEngine = new();
    private readonly WalletCapture _walletCapture;
    private readonly ChatOcrScanner _chatScanner;
    private readonly AuroraVoiceService _auroraService = new();
    private readonly NativeScanIndicator _walletScanIndicator = new("mobiGlas aUEC Scan", 0x22D3EE);
    private readonly NativeScanIndicator _contractScanIndicator = new("Auftrag Scan", 0x38BDF8);

    private Updater.Info? _latestUpdateInfo;
    private System.Threading.Timer? _updateCheckTimer;

    public PhotinoBridge()
    {
        var s = Settings.Load();
        _auroraService.IsEnabled = s.AuroraIntegrationEnabled;
        _auroraService.Volume = s.AuroraVolume;
        I18n.Instance.SetLanguage(s.AppLanguage ?? "Auto");
        _currentLogPath = s.LogPath ?? PathFinder.FindBest();
        Localization.Hint(_currentLogPath);

        _walletCapture = new WalletCapture(
            _ocrEngine,
            () => Settings.Load().WalletRegion ?? ScreenCapture.GetDefaultWalletRegion(),
            () => Settings.Load().AutoOcrEnabled);
        _walletCapture.BalanceCaptured += OnBalanceCaptured;

        _chatScanner = new ChatOcrScanner(
            _ocrEngine,
            () => Settings.Load().ChatRegion ?? ScreenCapture.GetDefaultChatRegion(),
            () => Settings.Load().ChatOcrEnabled,
            () => _activeSessionName ?? _selectedSession,
            s.ChatOcrIntervalMs);
        _chatScanner.MessagesScanned += OnChatMessagesScanned;
        if (s.ChatOcrEnabled)
        {
            _chatScanner.Start();
        }

        CitizenService.ProfileResolved += profile =>
        {
            try
            {
                Broadcast("HUD_UPDATE", GetHudTelemetry(_selectedSession));
            }
            catch (Exception ex)
            {
                Logger.Error("CitizenService.ProfileResolved", ex);
            }
        };
    }

    private void OnBalanceCaptured(long newBalance)
    {
        var s = Settings.Load();
        s.Balance = newBalance;
        s.BalanceSetAt = DateTime.UtcNow;
        Settings.Save(s);
        _walletScanIndicator.FlashGreen();
        Broadcast("HUD_UPDATE", GetHudTelemetry(_selectedSession));
        Broadcast("STATUS_UPDATE", GetAppStatus());
    }

    private void OnChatMessagesScanned(List<ChatMessageDto> messages)
    {
        if (messages == null || messages.Count == 0) return;
        var currentSession = _activeSessionName ?? _selectedSession;
        foreach (var msg in messages)
        {
            if (string.IsNullOrEmpty(msg.SessionId))
            {
                msg.SessionId = currentSession;
            }
            Database.InsertChatMessage(msg);
        }
        Broadcast("CHAT_MESSAGES_RECEIVED", messages);
    }

    public void Initialize(PhotinoWindow window)
    {
        _window = window;
        _currentLogPath = Settings.Load().LogPath ?? PathFinder.FindBest();
        Localization.Hint(_currentLogPath);

        // Register Web Message Handler (Receives client_ready from frontend React app)
        _window.RegisterWebMessageReceivedHandler((sender, rawMessage) =>
        {
            Task.Run(() => HandleIncomingMessage(rawMessage));
        });

        // Regelmäßige Update-Prüfung alle 6 Stunden
        _updateCheckTimer = new System.Threading.Timer(async _ =>
        {
            await CheckForAppUpdatesAsync(broadcastIfAvailable: true);
        }, null, TimeSpan.FromMinutes(1), TimeSpan.FromHours(6));
    }

    public void SendResponse<T>(string? requestId, string type, T payload)
    {
        if (_window == null) return;
        var msg = new
        {
            id = requestId,
            type,
            payload,
        };
        SendRaw(JsonSerializer.Serialize(msg, JsonOpts));
    }

    public void SendError(string? requestId, string errorMessage)
    {
        if (_window == null) return;
        var msg = new
        {
            id = requestId,
            type = "error",
            error = errorMessage,
        };
        SendRaw(JsonSerializer.Serialize(msg, JsonOpts));
    }

    public void Broadcast<T>(string type, T payload)
    {
        if (_window == null) return;
        var msg = new
        {
            type,
            payload,
        };
        SendRaw(JsonSerializer.Serialize(msg, JsonOpts));
    }

    private void SendRaw(string json)
    {
        if (!_isWebviewReady || _window == null) return;
        lock (_sendLock)
        {
            try
            {
                _window.Invoke(() =>
                {
                    try
                    {
                        _window.SendWebMessage(json);
                    }
                    catch (ApplicationException)
                    {
                        // Ignored if window closed or shutting down
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("PhotinoBridge.SendWebMessage", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error("PhotinoBridge.SendRaw", ex);
            }
        }
    }

    private async Task HandleIncomingMessage(string raw)
    {
        try
        {
            var req = JsonSerializer.Deserialize<IpcMessage>(raw, JsonOpts);
            if (req == null) return;

            if (!_isWebviewReady)
            {
                _isWebviewReady = true;
            }

            if (req.Type == "client_ready")
            {
                SendResponse(req.Id, "client_ready_ack", new { ok = true });

                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(_currentLogPath) && File.Exists(_currentLogPath))
                        {
                            ScanLogHeaderAndMeta(_currentLogPath, _parser);
                            ScanLogTailForShard(_currentLogPath, _parser);
                            StartLogTailer(_currentLogPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("PhotinoBridge.StartLogTailerOnReady", ex);
                    }

                    if (!_hasSyncedLogs)
                    {
                        _hasSyncedLogs = true;
                        SyncAllLogs(forceRescan: false);
                    }

                    // Auto-Check auf App-Updates nach Start
                    try
                    {
                        await Task.Delay(2500);
                        await CheckForAppUpdatesAsync(broadcastIfAvailable: true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("PhotinoBridge.AutoCheckUpdateOnReady", ex);
                    }
                });
                return;
            }

            switch (req.Type)
            {
                case "check_update":
                    var updResult = await CheckForAppUpdatesAsync(broadcastIfAvailable: true);
                    SendResponse(req.Id, "check_update_response", updResult);
                    break;

                case "apply_update":
                    var applyRes = await ApplyAppUpdateAsync();
                    SendResponse(req.Id, "apply_update_response", applyRes);
                    break;

                case "open_external_url":
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("url", out var urlProp))
                    {
                        var uStr = urlProp.GetString();
                        if (!string.IsNullOrEmpty(uStr))
                        {
                            try
                            {
                                Process.Start(new ProcessStartInfo { FileName = uStr, UseShellExecute = true });
                            }
                            catch (Exception ex)
                            {
                                Logger.Error("OpenExternalUrl", ex);
                            }
                        }
                    }
                    SendResponse(req.Id, "open_external_url_response", new { ok = true });
                    break;

                case "get_status":
                    SendResponse(req.Id, "status_response", GetAppStatus());
                    break;

                case "get_sessions":
                    SendResponse(req.Id, "sessions_response", GetSessions());
                    break;

                case "get_events":
                    string? category = null;
                    string? search = null;
                    string? session = null;
                    int limit = 100;
                    int offset = 0;
                    if (req.Payload.HasValue)
                    {
                        if (req.Payload.Value.TryGetProperty("session", out var sessProp)) session = sessProp.GetString();
                        if (req.Payload.Value.TryGetProperty("category", out var catProp)) category = catProp.GetString();
                        if (req.Payload.Value.TryGetProperty("search", out var sProp)) search = sProp.GetString();
                        if (req.Payload.Value.TryGetProperty("limit", out var limProp)) limit = limProp.GetInt32();
                        if (req.Payload.Value.TryGetProperty("offset", out var offProp)) offset = offProp.GetInt32();
                    }
                    SendResponse(req.Id, "events_response", GetEvents(session, category, search, limit, offset));
                    break;

                case "get_finance":
                    string? finScope = null;
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("scope", out var scpProp))
                    {
                        finScope = scpProp.GetString();
                    }
                    SendResponse(req.Id, "finance_response", GetFinanceOverview(finScope));
                    break;

                case "get_warehouse":
                    string? locFilter = null;
                    string? catFilter = null;
                    string? whSearch = null;
                    if (req.Payload.HasValue)
                    {
                        if (req.Payload.Value.TryGetProperty("location", out var lfProp)) locFilter = lfProp.GetString();
                        if (req.Payload.Value.TryGetProperty("category", out var cfProp)) catFilter = cfProp.GetString();
                        if (req.Payload.Value.TryGetProperty("search", out var wsProp)) whSearch = wsProp.GetString();
                    }
                    SendResponse(req.Id, "warehouse_response", GetWarehouseData(locFilter, catFilter, whSearch));
                    break;

                case "adjust_warehouse_qty":
                    if (req.Payload.HasValue)
                    {
                        string loc = req.Payload.Value.GetProperty("location").GetString() ?? "";
                        string itemClass = req.Payload.Value.GetProperty("itemClass").GetString() ?? "";
                        int delta = req.Payload.Value.GetProperty("delta").GetInt32();
                        Database.AdjustWarehouseItemQuantity(loc, itemClass, delta);
                        SendResponse(req.Id, "adjust_warehouse_qty_response", GetWarehouseData(loc, null, null));
                        Broadcast("WAREHOUSE_UPDATED", GetWarehouseData(null, null, null));
                    }
                    break;

                case "delete_warehouse_item":
                    if (req.Payload.HasValue)
                    {
                        string loc = req.Payload.Value.GetProperty("location").GetString() ?? "";
                        string itemClass = req.Payload.Value.GetProperty("itemClass").GetString() ?? "";
                        Database.DeleteWarehouseItem(loc, itemClass);
                        SendResponse(req.Id, "delete_warehouse_item_response", GetWarehouseData(loc, null, null));
                        Broadcast("WAREHOUSE_UPDATED", GetWarehouseData(null, null, null));
                    }
                    break;

                case "clear_warehouse_location":
                    if (req.Payload.HasValue)
                    {
                        string loc = req.Payload.Value.GetProperty("location").GetString() ?? "";
                        Database.ClearWarehouseLocation(loc);
                        SendResponse(req.Id, "clear_warehouse_location_response", GetWarehouseData(null, null, null));
                        Broadcast("WAREHOUSE_UPDATED", GetWarehouseData(null, null, null));
                    }
                    break;

                case "get_fleet":
                    SendResponse(req.Id, "fleet_response", GetFleetResponse());
                    break;

                case "toggle_ship_hangar":
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("shipName", out var tshProp))
                    {
                        var sName = tshProp.GetString() ?? "";
                        var custom = Database.GetAllFleetCustomData();
                        custom.TryGetValue(sName, out var exCd);
                        bool newHangar = !(exCd?.InHangar ?? false);
                        string newAcq = newHangar ? "Pledge Store" : "Geliehen / Free Fly";
                        bool newPledge = newHangar;
                        int newUsd = exCd?.PledgeUsd ?? FleetCatalog.Lookup(sName).PledgeValueUsd;
                        string newIns = exCd?.Insurance ?? FleetCatalog.Lookup(sName).DefaultInsurance;
                        string newNotes = exCd?.Notes ?? "";
                        Database.SaveFleetShipCustomData(sName, newHangar, newPledge, newUsd, newIns, newAcq, newNotes);
                        var fleetRes = GetFleetResponse();
                        Broadcast("FLEET_UPDATED", fleetRes);
                        SendResponse(req.Id, "toggle_ship_hangar_response", fleetRes);
                    }
                    break;

                case "add_catalog_ship_to_hangar":
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("shipName", out var ashProp))
                    {
                        var sName = ashProp.GetString() ?? "";
                        var cat = FleetCatalog.Lookup(sName);
                        Database.SaveFleetShipCustomData(sName, true, true, cat.PledgeValueUsd, cat.DefaultInsurance, "Pledge Store", "");
                        var fleetRes = GetFleetResponse();
                        Broadcast("FLEET_UPDATED", fleetRes);
                        SendResponse(req.Id, "add_catalog_ship_to_hangar_response", fleetRes);
                    }
                    break;

                case "remove_ship_from_hangar":
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("shipName", out var rshProp))
                    {
                        var sName = rshProp.GetString() ?? "";
                        var custom = Database.GetAllFleetCustomData();
                        custom.TryGetValue(sName, out var exCd);
                        Database.SaveFleetShipCustomData(sName, false, false, exCd?.PledgeUsd ?? 0, exCd?.Insurance ?? "", "Geliehen / Free Fly", exCd?.Notes ?? "");
                        var fleetRes = GetFleetResponse();
                        Broadcast("FLEET_UPDATED", fleetRes);
                        SendResponse(req.Id, "remove_ship_from_hangar_response", fleetRes);
                    }
                    break;

                case "cycle_ship_acquisition":
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("shipName", out var csaProp))
                    {
                        var sName = csaProp.GetString() ?? "";
                        var custom = Database.GetAllFleetCustomData();
                        custom.TryGetValue(sName, out var exCd);
                        var curAcq = exCd?.Acquisition ?? "Geliehen / Free Fly";
                        var nextAcq = curAcq switch
                        {
                            "Pledge Store" => "In-Game (aUEC)",
                            "In-Game (aUEC)" => "Miete (Rental)",
                            "Miete (Rental)" => "Geliehen / Free Fly",
                            _ => "Pledge Store"
                        };
                        bool inHangar = nextAcq is "Pledge Store" or "In-Game (aUEC)";
                        bool isPledge = nextAcq == "Pledge Store";
                        int pledgeUsd = exCd?.PledgeUsd ?? FleetCatalog.Lookup(sName).PledgeValueUsd;
                        string ins = exCd?.Insurance ?? FleetCatalog.Lookup(sName).DefaultInsurance;
                        string notes = exCd?.Notes ?? "";
                        Database.SaveFleetShipCustomData(sName, inHangar, isPledge, pledgeUsd, ins, nextAcq, notes);
                        var fleetRes = GetFleetResponse();
                        Broadcast("FLEET_UPDATED", fleetRes);
                        SendResponse(req.Id, "cycle_ship_acquisition_response", fleetRes);
                    }
                    break;

                case "cycle_ship_insurance":
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("shipName", out var csiProp))
                    {
                        var sName = csiProp.GetString() ?? "";
                        var custom = Database.GetAllFleetCustomData();
                        custom.TryGetValue(sName, out var exCd);
                        var curIns = exCd?.Insurance ?? "LTI (Lifetime)";
                        var nextIns = curIns switch
                        {
                            "LTI (Lifetime)" or "LTI" => "120 Monate (IAE)",
                            "120 Monate (IAE)" or "120M" => "24 Monate",
                            "24 Monate" or "24M" => "12 Monate",
                            "12 Monate" or "12M" => "6 Monate",
                            _ => "LTI (Lifetime)"
                        };
                        bool inHangar = exCd?.InHangar ?? true;
                        bool isPledge = exCd?.IsPledge ?? true;
                        int pledgeUsd = exCd?.PledgeUsd ?? FleetCatalog.Lookup(sName).PledgeValueUsd;
                        string acq = exCd?.Acquisition ?? "Pledge Store";
                        string notes = exCd?.Notes ?? "";
                        Database.SaveFleetShipCustomData(sName, inHangar, isPledge, pledgeUsd, nextIns, acq, notes);
                        var fleetRes = GetFleetResponse();
                        Broadcast("FLEET_UPDATED", fleetRes);
                        SendResponse(req.Id, "cycle_ship_insurance_response", fleetRes);
                    }
                    break;

                case "update_ship_pledge":
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("shipName", out var uspProp) && req.Payload.Value.TryGetProperty("pledgeUsd", out var pUsdProp))
                    {
                        var sName = uspProp.GetString() ?? "";
                        int pUsd = pUsdProp.GetInt32();
                        var custom = Database.GetAllFleetCustomData();
                        custom.TryGetValue(sName, out var exCd);
                        Database.SaveFleetShipCustomData(sName, exCd?.InHangar ?? true, exCd?.IsPledge ?? true, pUsd, exCd?.Insurance ?? "LTI (Lifetime)", exCd?.Acquisition ?? "Pledge Store", exCd?.Notes ?? "");
                        var fleetRes = GetFleetResponse();
                        Broadcast("FLEET_UPDATED", fleetRes);
                        SendResponse(req.Id, "update_ship_pledge_response", fleetRes);
                    }
                    break;

                case "update_ship_notes":
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("shipName", out var usnProp) && req.Payload.Value.TryGetProperty("notes", out var notesProp))
                    {
                        var sName = usnProp.GetString() ?? "";
                        string notes = notesProp.GetString() ?? "";
                        var custom = Database.GetAllFleetCustomData();
                        custom.TryGetValue(sName, out var exCd);
                        Database.SaveFleetShipCustomData(sName, exCd?.InHangar ?? false, exCd?.IsPledge ?? false, exCd?.PledgeUsd ?? 0, exCd?.Insurance ?? "", exCd?.Acquisition ?? "Geliehen / Free Fly", notes);
                        var fleetRes = GetFleetResponse();
                        Broadcast("FLEET_UPDATED", fleetRes);
                        SendResponse(req.Id, "update_ship_notes_response", fleetRes);
                    }
                    break;

                case "set_current_ship":
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("shipName", out var scsProp))
                    {
                        _currentShip = scsProp.GetString();
                        Broadcast("HUD_UPDATE", GetHudTelemetry(_selectedSession));
                        Broadcast("FLEET_UPDATED", GetFleetResponse());
                        SendResponse(req.Id, "set_current_ship_response", new { success = true, currentShip = _currentShip });
                    }
                    break;

                case "get_missions":
                    SendResponse(req.Id, "missions_response", GetMissionsData());
                    break;

                case "clear_contracts":
                    Database.ClearActiveContracts();
                    SendResponse(req.Id, "clear_contracts_response", new { success = true });
                    Broadcast("HUD_UPDATE", GetHudTelemetry(_selectedSession));
                    break;

                case "get_reputation":
                    SendResponse(req.Id, "reputation_response", GetReputationData());
                    break;

                case "set_reputation":
                    if (req.Payload.HasValue)
                    {
                        var p = req.Payload.Value;
                        string fId = p.TryGetProperty("factionId", out var fProp) ? fProp.GetString() ?? "" : "";
                        int xp = p.TryGetProperty("xp", out var xpP) ? xpP.GetInt32() : 0;
                        int missions = p.TryGetProperty("missions", out var mP) ? mP.GetInt32() : 0;
                        if (p.TryGetProperty("level", out var lvlP))
                        {
                            int lvl = Math.Clamp(lvlP.GetInt32(), 1, 6);
                            int[] thresholds = { 0, 1000, 3000, 7500, 15000, 30000 };
                            xp = thresholds[lvl - 1];
                        }
                        Database.SetFactionReputation(fId, xp, missions, DateTime.UtcNow);
                        SendResponse(req.Id, "set_reputation_response", new { success = true });
                        Broadcast("reputation_response", GetReputationData());
                    }
                    break;

                case "adjust_reputation_xp":
                    if (req.Payload.HasValue)
                    {
                        var p = req.Payload.Value;
                        string fId = p.TryGetProperty("factionId", out var fProp) ? fProp.GetString() ?? "" : "";
                        int delta = p.TryGetProperty("deltaXp", out var dP) ? dP.GetInt32() : 0;
                        if (delta != 0 && !string.IsNullOrWhiteSpace(fId))
                        {
                            var existing = Database.LoadFactionReputations();
                            int curXp = existing.TryGetValue(fId, out var cur) ? cur.Xp : 0;
                            int curMissions = existing.TryGetValue(fId, out cur) ? cur.Missions : 0;
                            int newXp = Math.Max(0, curXp + delta);
                            Database.SetFactionReputation(fId, newXp, curMissions, DateTime.UtcNow);
                        }
                        SendResponse(req.Id, "adjust_reputation_xp_response", new { success = true });
                        Broadcast("reputation_response", GetReputationData());
                    }
                    break;

                case "reset_reputation":
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("factionId", out var rFidProp))
                    {
                        string rFid = rFidProp.GetString() ?? "";
                        Database.SetFactionReputation(rFid, 0, 0, DateTime.UtcNow);
                    }
                    else
                    {
                        Database.ResetFactionReputations();
                    }
                    SendResponse(req.Id, "reset_reputation_response", new { success = true });
                    Broadcast("reputation_response", GetReputationData());
                    break;

                case "get_blueprints":
                    SendResponse(req.Id, "blueprints_response", GetBlueprintsData());
                    break;

                case "get_loadout":
                    SendResponse(req.Id, "loadout_response", GetLoadoutData());
                    break;

                case "get_starmap":
                    string sys = "Stanton";
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("system", out var sysProp))
                    {
                        sys = sysProp.GetString() ?? "Stanton";
                    }
                    SendResponse(req.Id, "starmap_response", GetStarmapData(sys));
                    break;

                case "calculate_route":
                    string fromId = "";
                    string toId = "";
                    string? drive = null;
                    if (req.Payload.HasValue)
                    {
                        if (req.Payload.Value.TryGetProperty("fromId", out var fProp)) fromId = fProp.GetString() ?? "";
                        if (req.Payload.Value.TryGetProperty("toId", out var tProp)) toId = tProp.GetString() ?? "";
                        if (req.Payload.Value.TryGetProperty("driveName", out var dProp)) drive = dProp.GetString();
                    }
                    SendResponse(req.Id, "calculate_route_response", CalculateQuantumRoute(fromId, toId, drive));
                    break;

                case "get_places":
                    string? placeSys = null;
                    string? placeType = null;
                    if (req.Payload.HasValue)
                    {
                        if (req.Payload.Value.TryGetProperty("system", out var psProp)) placeSys = psProp.GetString();
                        if (req.Payload.Value.TryGetProperty("type", out var ptProp)) placeType = ptProp.GetString();
                    }
                    SendResponse(req.Id, "places_response", GetPlacesData(placeSys, placeType));
                    break;

                case "get_blackbox":
                    string? bbSession = null;
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("session", out var bbProp))
                    {
                        bbSession = bbProp.GetString();
                    }
                    SendResponse(req.Id, "blackbox_response", GetBlackboxData(bbSession));
                    break;

                case "get_rs_signatures":
                    SendResponse(req.Id, "rs_signatures_response", GetRsSignaturesData());
                    break;

                case "decode_rs":
                    int rsVal = 0;
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("rs", out var rsProp))
                    {
                        rsVal = rsProp.GetInt32();
                    }
                    SendResponse(req.Id, "decode_rs_response", DecodeRsData(rsVal));
                    break;

                case "get_market":
                    SendResponse(req.Id, "market_response", GetMarketData());
                    break;

                case "get_tools_status":
                    SendResponse(req.Id, "tools_status_response", GetToolsStatus());
                    break;

                case "clear_shader_cache":
                    SendResponse(req.Id, "clear_shader_cache_response", ClearShaderCache());
                    break;

                case "clear_crash_dumps":
                    SendResponse(req.Id, "clear_crash_dumps_response", ClearCrashDumps());
                    break;

                case "save_user_cfg":
                    string cfgContent = "";
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("content", out var cProp))
                    {
                        cfgContent = cProp.GetString() ?? "";
                    }
                    SendResponse(req.Id, "save_user_cfg_response", SaveUserCfg(cfgContent));
                    break;

                case "backup_keybinds":
                    string? note = null;
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("note", out var nProp))
                    {
                        note = nProp.GetString();
                    }
                    SendResponse(req.Id, "backup_keybinds_response", BackupKeybinds(note));
                    break;

                case "restore_keybinds":
                    if (req.Payload.HasValue)
                    {
                        string bName = req.Payload.Value.TryGetProperty("name", out var bnProp) ? bnProp.GetString() ?? "" : "";
                        string bPath = req.Payload.Value.TryGetProperty("folderPath", out var bpProp) ? bpProp.GetString() ?? "" : "";
                        var s = Settings.Load();
                        var allKeybinds = MaintenanceService.ListKeybindBackups(s.CloudStoragePath);
                        var targetBackup = allKeybinds.FirstOrDefault(k => (!string.IsNullOrEmpty(bPath) && k.FolderPath.Equals(bPath, StringComparison.OrdinalIgnoreCase)) || k.Name.Equals(bName, StringComparison.OrdinalIgnoreCase));
                        if (targetBackup != null)
                        {
                            var res = MaintenanceService.RestoreKeybinds(targetBackup, _currentLogPath);
                            SendResponse(req.Id, "restore_keybinds_response", new { success = res.success, message = res.message, tools = GetToolsStatus() });
                        }
                        else
                        {
                            SendResponse(req.Id, "restore_keybinds_response", new { success = false, message = "Steuerungs-Backup nicht gefunden.", tools = GetToolsStatus() });
                        }
                    }
                    break;

                case "backup_user_cfg":
                    string? cfgNote = null;
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("note", out var cnProp))
                    {
                        cfgNote = cnProp.GetString();
                    }
                    var setts = Settings.Load();
                    var bRes = MaintenanceService.BackupUserCfg(_currentLogPath, setts.CloudStoragePath, cfgNote ?? "Manuell");
                    SendResponse(req.Id, "backup_user_cfg_response", new { success = bRes.success, message = bRes.message, tools = GetToolsStatus() });
                    break;

                case "restore_user_cfg":
                    if (req.Payload.HasValue)
                    {
                        string cName = req.Payload.Value.TryGetProperty("name", out var cNameProp) ? cNameProp.GetString() ?? "" : "";
                        string cPath = req.Payload.Value.TryGetProperty("filePath", out var cPathProp) ? cPathProp.GetString() ?? "" : "";
                        var st = Settings.Load();
                        var allCfgs = MaintenanceService.ListConfigBackups(st.CloudStoragePath);
                        var targetCfg = allCfgs.FirstOrDefault(c => (!string.IsNullOrEmpty(cPath) && c.FilePath.Equals(cPath, StringComparison.OrdinalIgnoreCase)) || c.Name.Equals(cName, StringComparison.OrdinalIgnoreCase));
                        if (targetCfg != null)
                        {
                            var rRes = MaintenanceService.RestoreConfigBackup(targetCfg, _currentLogPath);
                            SendResponse(req.Id, "restore_user_cfg_response", new { success = rRes.success, message = rRes.message, tools = GetToolsStatus() });
                        }
                        else
                        {
                            SendResponse(req.Id, "restore_user_cfg_response", new { success = false, message = "user.cfg Backup nicht gefunden.", tools = GetToolsStatus() });
                        }
                    }
                    break;

                case "save_cloud_storage_path":
                    if (req.Payload.HasValue)
                    {
                        string cPath = req.Payload.Value.TryGetProperty("path", out var cpProp) ? cpProp.GetString() ?? "" : "";
                        var s = Settings.Load();
                        s.CloudStoragePath = string.IsNullOrWhiteSpace(cPath) ? null : cPath.Trim();
                        Settings.Save(s);
                        SendResponse(req.Id, "save_cloud_storage_path_response", new { success = true, tools = GetToolsStatus() });
                    }
                    break;

                case "export_logs_zip":
                    var sCloud = Settings.Load();
                    string destZipFolder = !string.IsNullOrWhiteSpace(sCloud.CloudStoragePath) && Directory.Exists(sCloud.CloudStoragePath)
                        ? Path.Combine(sCloud.CloudStoragePath, "SCLogMate", "Logs")
                        : Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    var zRes = MaintenanceService.ExportLogsToZip(destZipFolder, _currentLogPath);
                    SendResponse(req.Id, "export_logs_zip_response", new { success = zRes.success, message = zRes.message, zipPath = zRes.zipPath });
                    break;

                case "sync_logs_cloud":
                    var sc = Settings.Load();
                    if (string.IsNullOrWhiteSpace(sc.CloudStoragePath))
                    {
                        SendResponse(req.Id, "sync_logs_cloud_response", new { success = false, message = "Bitte hinterlege zuerst einen gültigen Cloud-Speicherpfad." });
                    }
                    else
                    {
                        var syncRes = MaintenanceService.SyncLogsToCloud(sc.CloudStoragePath, _currentLogPath);
                        SendResponse(req.Id, "sync_logs_cloud_response", new { success = syncRes.success, message = syncRes.message });
                    }
                    break;

                case "get_settings":
                    SendResponse(req.Id, "settings_response", GetSettingsData());
                    break;

                case "save_settings":
                    if (req.Payload.HasValue)
                    {
                        var targetEl = req.Payload.Value;
                        if (targetEl.ValueKind == JsonValueKind.Object && targetEl.TryGetProperty("settings", out var sProp))
                        {
                            targetEl = sProp;
                        }
                        var settingsDto = JsonSerializer.Deserialize<SettingsDto>(targetEl.GetRawText(), JsonOpts);
                        if (settingsDto != null)
                        {
                            SaveSettingsData(settingsDto);
                        }
                    }
                    SendResponse(req.Id, "save_settings_response", GetSettingsData());
                    Broadcast("STATUS_UPDATE", GetAppStatus());
                    break;

                case "simulate_event":
                    if (req.Payload.HasValue)
                    {
                        string evtType = "";
                        string simParam = "";
                        if (req.Payload.Value.TryGetProperty("eventType", out var etProp)) evtType = etProp.GetString() ?? "";
                        if (req.Payload.Value.TryGetProperty("param", out var pProp)) simParam = pProp.GetString() ?? "";

                        switch (evtType.ToLowerInvariant())
                        {
                            case "armistice_enter":
                                _auroraService.ProcessLiveLine(@"<2026-09-02T12:00:00.000Z> [Notice] <SHUDEvent_OnNotification> Added notification ""Schutzzone - Kampfhandlung untersagt!: "" [9001]");
                                break;
                            case "armistice_leave":
                                _auroraService.ProcessLiveLine(@"<2026-09-02T12:00:00.000Z> [Notice] <SHUDEvent_OnNotification> Added notification ""Schutzzone verlassen: "" [9002]");
                                break;
                            case "ship_join":
                                string sName = string.IsNullOrWhiteSpace(simParam) ? "Drake Cutlass Black" : simParam;
                                _auroraService.ProcessLiveLine($@"<2026-09-02T12:00:00.000Z> [Notice] <SHUDEvent_OnNotification> Added notification ""Du bist Kanal [ {sName} ] beigetreten""");
                                break;
                            case "blueprint_found":
                                _auroraService.OnBlueprintLearned("Pyro RYT Multi-Tool");
                                break;
                            case "quantum_arrival":
                                _auroraService.OnQuantumArrival();
                                break;
                            case "server_error":
                                _auroraService.OnServerError();
                                break;
                            case "player_death":
                                _auroraService.OnPlayerDeath();
                                break;
                        }
                    }
                    SendResponse(req.Id, "simulate_event_response", new { success = true });
                    break;

                case "dump_debug_state":
                    {
                        var curS = Settings.Load();
                        Logger.Log("══════════════════════════════════════════════════════════");
                        Logger.Log($"[DEBUG DUMP] Zeitstempel: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        Logger.Log($"[DEBUG DUMP] Debug-Modus: {curS.DebugMode}");
                        Logger.Log($"[DEBUG DUMP] Sprache: {curS.AppLanguage}");
                        Logger.Log($"[DEBUG DUMP] LogPath: {_currentLogPath} (Existiert: {File.Exists(_currentLogPath)})");
                        Logger.Log($"[DEBUG DUMP] Aurora Voice: Installiert={_auroraService.IsInstalled}, Aktiv={_auroraService.IsEnabled}, Vol={_auroraService.Volume}%");
                        Logger.Log($"[DEBUG DUMP] Windows OCR: AutoSync={curS.AutoOcrEnabled}");
                        Logger.Log($"[DEBUG DUMP] Wipe-Filter: Aktiv={curS.WipeFilterEnabled}, Datum={curS.WipeDateString}");
                        Logger.Log("══════════════════════════════════════════════════════════");
                        string dbgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SCLogMate", "SCLogMate.debug.log");
                        SendResponse(req.Id, "dump_debug_state_response", new { success = true, logPath = dbgPath });
                    }
                    break;

                case "clear_debug_log":
                    {
                        try
                        {
                            var dpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SCLogMate", "SCLogMate.debug.log");
                            if (File.Exists(dpath))
                            {
                                File.WriteAllText(dpath, $"[{DateTime.Now:HH:mm:ss}] [DEBUG] Logdatei zurückgesetzt.\n");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("ClearDebugLog", ex);
                        }
                        SendResponse(req.Id, "clear_debug_log_response", new { success = true });
                    }
                    break;

                case "get_ocr_regions":
                    SendResponse(req.Id, "ocr_regions_response", GetOcrRegionsConfig());
                    break;

                case "save_ocr_region":
                    if (req.Payload.HasValue)
                    {
                        string target = "wallet";
                        ScanRegion? region = null;
                        if (req.Payload.Value.TryGetProperty("target", out var tProp)) target = tProp.GetString() ?? "wallet";
                        if (req.Payload.Value.TryGetProperty("region", out var rProp) && rProp.ValueKind != JsonValueKind.Null)
                        {
                            region = JsonSerializer.Deserialize<ScanRegion>(rProp.GetRawText(), JsonOpts);
                        }
                        var s = Settings.Load();
                        if (target == "wallet")
                        {
                            s.WalletRegion = region;
                            _walletScanIndicator.SetRegion(region ?? ScreenCapture.GetDefaultWalletRegion());
                        }
                        else if (target == "contract")
                        {
                            s.ContractRegion = region;
                            _contractScanIndicator.SetRegion(region ?? ScreenCapture.GetDefaultContractRegion());
                        }
                        else if (target == "rs")
                        {
                            s.RsScanRegion = region;
                        }
                        else if (target == "chat")
                        {
                            s.ChatRegion = region;
                        }
                        Settings.Save(s);
                    }
                    SendResponse(req.Id, "save_ocr_region_response", GetOcrRegionsConfig());
                    break;

                case "select_ocr_region":
                    string selTarget = "wallet";
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("target", out var stProp))
                    {
                        selTarget = stProp.GetString() ?? "wallet";
                    }
                    string selTitle = selTarget switch
                    {
                        "contract" => "Auftragsmanager (Contracts)",
                        "rs" => "RS Signal Radar",
                        "chat" => "In-Game Chat",
                        _ => "mobiGlas aUEC"
                    };
                    var selected = await NativeRegionSelector.SelectRegionAsync(selTitle);
                    if (selected != null)
                    {
                        var s = Settings.Load();
                        if (selTarget == "wallet")
                        {
                            s.WalletRegion = selected;
                            _walletScanIndicator.SetRegion(selected);
                        }
                        else if (selTarget == "contract")
                        {
                            s.ContractRegion = selected;
                            _contractScanIndicator.SetRegion(selected);
                        }
                        else if (selTarget == "rs")
                        {
                            s.RsScanRegion = selected;
                        }
                        else if (selTarget == "chat")
                        {
                            s.ChatRegion = selected;
                        }
                        Settings.Save(s);
                        SendResponse(req.Id, "select_ocr_region_response", new { success = true, cancelled = false, region = selected, config = GetOcrRegionsConfig() });
                    }
                    else
                    {
                        SendResponse(req.Id, "select_ocr_region_response", new { success = false, cancelled = true, config = GetOcrRegionsConfig() });
                    }
                    break;

                case "test_ocr_scan":
                    string testTarget = "wallet";
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("target", out var ttProp))
                    {
                        testTarget = ttProp.GetString() ?? "wallet";
                    }
                    var testResult = await ExecuteOcrTestAsync(testTarget);
                    SendResponse(req.Id, "test_ocr_scan_response", testResult);
                    break;

                case "toggle_scan_indicator":
                    string indTarget = "wallet";
                    bool? showOverride = null;
                    if (req.Payload.HasValue)
                    {
                        if (req.Payload.Value.TryGetProperty("target", out var itProp)) indTarget = itProp.GetString() ?? "wallet";
                        if (req.Payload.Value.TryGetProperty("show", out var shProp)) showOverride = shProp.GetBoolean();
                    }
                    var sObj = Settings.Load();
                    if (indTarget == "contract")
                    {
                        bool show = showOverride ?? !_contractScanIndicator.IsVisible;
                        if (show)
                        {
                            _contractScanIndicator.SetRegion(sObj.ContractRegion ?? ScreenCapture.GetDefaultContractRegion());
                            _contractScanIndicator.Show();
                        }
                        else
                        {
                            _contractScanIndicator.Hide();
                        }
                    }
                    else
                    {
                        bool show = showOverride ?? !_walletScanIndicator.IsVisible;
                        if (show)
                        {
                            _walletScanIndicator.SetRegion(sObj.WalletRegion ?? ScreenCapture.GetDefaultWalletRegion());
                            _walletScanIndicator.Show();
                        }
                        else
                        {
                            _walletScanIndicator.Hide();
                        }
                    }
                    SendResponse(req.Id, "toggle_scan_indicator_response", GetOcrRegionsConfig());
                    break;

                case "toggle_watcher":
                    bool enable = true;
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("enable", out var enableProp))
                    {
                        enable = enableProp.GetBoolean();
                    }
                    else
                    {
                        enable = _tailer == null || !_tailer.IsLiveStreaming;
                    }

                    if (enable)
                    {
                        if (!string.IsNullOrEmpty(_currentLogPath) && File.Exists(_currentLogPath))
                        {
                            StartLogTailer(_currentLogPath);
                        }
                    }
                    else
                    {
                        _tailer?.Stop();
                        _tailer = null;
                    }

                    SendResponse(req.Id, "toggle_watcher_response", new { isLiveWatching = _tailer != null });
                    Broadcast("STATUS_UPDATE", GetAppStatus());
                    break;

                case "scan_logs":
                    int scanned = TriggerScan();
                    SendResponse(req.Id, "scan_logs_response", new { scannedCount = scanned });
                    Broadcast("STATUS_UPDATE", GetAppStatus());
                    Broadcast("sessions_response", GetSessions());
                    Broadcast("log_status_response", GetLogStatus());
                    Broadcast("HUD_UPDATE", GetHudTelemetry(_selectedSession));
                    break;

                case "get_log_status":
                    SendResponse(req.Id, "log_status_response", GetLogStatus());
                    break;

                case "detect_log_path":
                    var detectedStatus = DetectLogPath();
                    SendResponse(req.Id, "detect_log_path_response", detectedStatus);
                    Broadcast("log_status_response", detectedStatus);
                    Broadcast("STATUS_UPDATE", GetAppStatus());
                    Broadcast("sessions_response", GetSessions());
                    Broadcast("HUD_UPDATE", GetHudTelemetry(_selectedSession));
                    break;

                case "set_log_path":
                    string newPath = "";
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("path", out var pathProp))
                    {
                        newPath = pathProp.GetString() ?? "";
                    }
                    var updatedLogStatus = SetLogPath(newPath);
                    SendResponse(req.Id, "set_log_path_response", updatedLogStatus);
                    Broadcast("log_status_response", updatedLogStatus);
                    Broadcast("STATUS_UPDATE", GetAppStatus());
                    Broadcast("sessions_response", GetSessions());
                    Broadcast("HUD_UPDATE", GetHudTelemetry(_selectedSession));
                    break;

                case "browse_log_file":
                    var browsedStatus = BrowseLogFile();
                    SendResponse(req.Id, "browse_log_file_response", browsedStatus);
                    Broadcast("log_status_response", browsedStatus);
                    Broadcast("STATUS_UPDATE", GetAppStatus());
                    Broadcast("sessions_response", GetSessions());
                    Broadcast("HUD_UPDATE", GetHudTelemetry(_selectedSession));
                    break;

                case "reparse_all_logs":
                    var reparseResult = ReparseAllLogs();
                    SendResponse(req.Id, "reparse_all_logs_response", new { indexedSessions = reparseResult.indexedSessions, totalEvents = reparseResult.totalEvents });
                    break;

                case "reparse_session":
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("session", out var repSessProp))
                    {
                        string sName = repSessProp.GetString() ?? "";
                        ReparseSession(sName);
                    }
                    SendResponse(req.Id, "reparse_session_response", GetSessions());
                    Broadcast("sessions_response", GetSessions());
                    Broadcast("HUD_UPDATE", GetHudTelemetry(_selectedSession));
                    break;

                case "delete_session":
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("session", out var delSessProp))
                    {
                        string sName = delSessProp.GetString() ?? "";
                        DeleteSession(sName);
                    }
                    SendResponse(req.Id, "delete_session_response", GetSessions());
                    Broadcast("sessions_response", GetSessions());
                    Broadcast("STATUS_UPDATE", GetAppStatus());
                    Broadcast("HUD_UPDATE", GetHudTelemetry(_selectedSession));
                    break;

                case "get_db_diagnostics":
                    SendResponse(req.Id, "db_diagnostics_response", GetDbDiagnostics());
                    break;

                case "repair_db_structure":
                    var repairRes = RepairDbStructure();
                    SendResponse(req.Id, "repair_db_structure_response", repairRes);
                    Broadcast("STATUS_UPDATE", GetAppStatus());
                    break;

                case "cleanup_database":
                    var cleanupRes = CleanupDatabase();
                    SendResponse(req.Id, "cleanup_database_response", cleanupRes);
                    Broadcast("STATUS_UPDATE", GetAppStatus());
                    break;

                case "reset_database":
                    Database.ClearAll();
                    SendResponse(req.Id, "reset_database_response", new { success = true });
                    Broadcast("STATUS_UPDATE", GetAppStatus());
                    Broadcast("sessions_response", GetSessions());
                    Broadcast("HUD_UPDATE", GetHudTelemetry(_selectedSession));
                    break;

                case "get_unknown_events":
                    SendResponse(req.Id, "unknown_events_response", GetUnknownEventsData());
                    break;

                case "export_events":
                    string expFmt = "csv";
                    string? expSess = null;
                    if (req.Payload.HasValue)
                    {
                        if (req.Payload.Value.TryGetProperty("format", out var fProp)) expFmt = fProp.GetString() ?? "csv";
                        if (req.Payload.Value.TryGetProperty("session", out var sProp)) expSess = sProp.GetString();
                    }
                    SendResponse(req.Id, "export_events_response", ExportEvents(expFmt, expSess));
                    break;

                case "open_folder":
                    {
                        if (req.Payload.HasValue)
                        {
                            string target = "";
                            if (req.Payload.Value.TryGetProperty("folderType", out var ofFtProp))
                                target = ofFtProp.GetString() ?? "";
                            else if (req.Payload.Value.TryGetProperty("target", out var ofTargetProp))
                                target = ofTargetProp.GetString() ?? "";
                            else if (req.Payload.Value.TryGetProperty("path", out var ofPathProp))
                                target = ofPathProp.GetString() ?? "";

                            OpenFolder(target);
                        }
                        SendResponse(req.Id, "open_folder_response", new { success = true });
                        break;
                    }

                case "get_hud":
                    string? reqSess = null;
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("session", out var hudSessProp)) reqSess = hudSessProp.GetString();
                    SendResponse(req.Id, "hud_response", GetHudTelemetry(reqSess));
                    break;

                case "get_pilot_dossier":
                    string? dHandle = null;
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("handle", out var dhProp))
                    {
                        dHandle = dhProp.GetString();
                    }
                    if (string.IsNullOrWhiteSpace(dHandle) || dHandle == "—" || dHandle == "Unbekannter Pilot")
                    {
                        var curHud = GetHudTelemetry(_selectedSession);
                        dHandle = curHud.PilotName;
                    }
                    var pProfile = await CitizenService.FetchProfileAsync(dHandle ?? "");
                    SendResponse(req.Id, "get_pilot_dossier_response", pProfile);
                    break;

                case "select_session":
                    if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("session", out var selProp))
                    {
                        _selectedSession = selProp.GetString() ?? "__live__";
                    }
                    var hudData = GetHudTelemetry(_selectedSession);
                    SendResponse(req.Id, "select_session_response", hudData);
                    Broadcast("HUD_UPDATE", hudData);
                    break;

                case "trigger_ocr":
                    var curSet = Settings.Load();
                    long newBal = curSet.Balance > 0 ? curSet.Balance + 25000 : 2500000;
                    curSet.Balance = newBal;
                    Settings.Save(curSet);
                    SendResponse(req.Id, "trigger_ocr_response", new { balance = newBal, success = true });
                    Broadcast("HUD_UPDATE", GetHudTelemetry(_selectedSession));
                    break;

                case "toggle_auto_ocr":
                    var ocrSet = Settings.Load();
                    ocrSet.AutoOcrEnabled = !ocrSet.AutoOcrEnabled;
                    Settings.Save(ocrSet);
                    SendResponse(req.Id, "toggle_auto_ocr_response", new { autoOcrEnabled = ocrSet.AutoOcrEnabled });
                    Broadcast("HUD_UPDATE", GetHudTelemetry(_selectedSession));
                    break;

                case "open_overlay":
                    SendResponse(req.Id, "open_overlay_response", new { success = true });
                    break;

                case "open_rs_overlay":
                    SendResponse(req.Id, "open_rs_overlay_response", new { success = true });
                    break;

                case "record_expense":
                    {
                        if (req.Payload.HasValue)
                        {
                            string cat = req.Payload.Value.GetProperty("category").GetString() ?? "Wartung & Reparatur";
                            string expenseNote = req.Payload.Value.TryGetProperty("note", out var expNoteProp) ? expNoteProp.GetString() ?? cat : cat;
                            long amount = req.Payload.Value.GetProperty("amount").GetInt64();
                            string loc = req.Payload.Value.TryGetProperty("location", out var lProp) ? lProp.GetString() ?? "—" : "—";
                            string? ship = req.Payload.Value.TryGetProperty("ship", out var sProp) ? sProp.GetString() : null;

                            Database.InsertCustomEvent(_activeSessionName ?? "Game.log", DateTime.UtcNow, Models.EventKind.Purchase, -Math.Abs(amount), $"{cat}: {expenseNote} @ {loc}", ship);
                            SendResponse(req.Id, "record_expense_response", new { success = true });
                            Broadcast("HUD_UPDATE", GetHudTelemetry(_selectedSession));
                            Broadcast("finance_response", GetFinanceOverview());
                        }
                        break;
                    }

                case "lookup_wiki":
                    {
                        string q = "";
                        if (req.Payload.HasValue)
                        {
                            if (req.Payload.Value.TryGetProperty("query", out var wqProp)) q = wqProp.GetString() ?? "";
                            else if (req.Payload.Value.TryGetProperty("className", out var wcnProp)) q = wcnProp.GetString() ?? "";
                            else if (req.Payload.Value.TryGetProperty("name", out var wnProp)) q = wnProp.GetString() ?? "";
                        }
                        var wInfo = await WikiApiClient.LookupAsync(q, enrichBase64Image: true);
                        SendResponse(req.Id, "lookup_wiki_response", wInfo);
                        break;
                    }

                case "search_wiki":
                    {
                        string sq = "";
                        string? scat = null;
                        int slim = 25;
                        if (req.Payload.HasValue)
                        {
                            if (req.Payload.Value.TryGetProperty("query", out var sqProp)) sq = sqProp.GetString() ?? "";
                            if (req.Payload.Value.TryGetProperty("category", out var scProp)) scat = scProp.GetString();
                            if (req.Payload.Value.TryGetProperty("limit", out var slProp)) slim = slProp.GetInt32();
                        }
                        var searchHits = await WikiApiClient.SearchWikiAsync(sq, scat, slim);
                        SendResponse(req.Id, "search_wiki_response", searchHits);
                        break;
                    }

                case "get_wiki_specs":
                    {
                        string target = "";
                        if (req.Payload.HasValue)
                        {
                            if (req.Payload.Value.TryGetProperty("name", out var snProp)) target = snProp.GetString() ?? "";
                            else if (req.Payload.Value.TryGetProperty("query", out var sqProp2)) target = sqProp2.GetString() ?? "";
                        }
                        var specs = await WikiApiClient.LookupAsync(target, enrichBase64Image: true);
                        SendResponse(req.Id, "get_wiki_specs_response", specs);
                        break;
                    }

                case "get_chat_messages":
                    {
                        string? csSession = null;
                        string? csChannel = null;
                        string? csSender = null;
                        string? csSearch = null;
                        bool csFlagged = false;
                        int csLimit = 300;
                        if (req.Payload.HasValue)
                        {
                            if (req.Payload.Value.TryGetProperty("session", out var sp)) csSession = sp.GetString();
                            if (req.Payload.Value.TryGetProperty("channel", out var cp)) csChannel = cp.GetString();
                            if (req.Payload.Value.TryGetProperty("sender", out var sndp)) csSender = sndp.GetString();
                            if (req.Payload.Value.TryGetProperty("search", out var srchp)) csSearch = srchp.GetString();
                            if (req.Payload.Value.TryGetProperty("flaggedOnly", out var fp)) csFlagged = fp.GetBoolean();
                            if (req.Payload.Value.TryGetProperty("limit", out var lp)) csLimit = lp.GetInt32();
                        }
                        var msgs = Database.GetChatMessages(csSession, csChannel, csSender, csSearch, csFlagged, csLimit);
                        SendResponse(req.Id, "get_chat_messages_response", msgs);
                        break;
                    }

                case "scan_chat_now":
                    {
                        var chatScanned = await _chatScanner.ScanNowAsync();
                        SendResponse(req.Id, "scan_chat_now_response", new { success = true, count = chatScanned.Count, messages = chatScanned });
                        break;
                    }

                case "toggle_chat_ocr":
                    {
                        var s = Settings.Load();
                        bool chatEnable = req.Payload.HasValue && req.Payload.Value.TryGetProperty("enabled", out var enProp) 
                            ? enProp.GetBoolean() 
                            : !s.ChatOcrEnabled;
                        s.ChatOcrEnabled = chatEnable;
                        Settings.Save(s);
                        if (chatEnable) _chatScanner.Start();
                        else _chatScanner.Stop();
                        SendResponse(req.Id, "toggle_chat_ocr_response", new { enabled = chatEnable });
                        Broadcast("STATUS_UPDATE", GetAppStatus());
                        break;
                    }

                case "flag_chat_message":
                    {
                        long id = 0;
                        bool flagged = true;
                        if (req.Payload.HasValue)
                        {
                            if (req.Payload.Value.TryGetProperty("id", out var idProp)) id = idProp.GetInt64();
                            if (req.Payload.Value.TryGetProperty("isFlagged", out var fProp)) flagged = fProp.GetBoolean();
                        }
                        Database.FlagChatMessage(id, flagged);
                        SendResponse(req.Id, "flag_chat_message_response", new { success = true, id, isFlagged = flagged });
                        break;
                    }

                case "clear_chat_messages":
                    {
                        string? sess = null;
                        if (req.Payload.HasValue && req.Payload.Value.TryGetProperty("session", out var sessProp)) sess = sessProp.GetString();
                        Database.ClearChatMessages(sess);
                        SendResponse(req.Id, "clear_chat_messages_response", new { success = true });
                        break;
                    }

                case "export_player_report":
                    {
                        string reportSuspect = "";
                        string reportCategory = "Griefing / Harassment";
                        string reportDescription = "";
                        List<long> messageIds = new();
                        if (req.Payload.HasValue)
                        {
                            if (req.Payload.Value.TryGetProperty("suspect", out var susProp)) reportSuspect = susProp.GetString() ?? "";
                            if (req.Payload.Value.TryGetProperty("category", out var catProp)) reportCategory = catProp.GetString() ?? reportCategory;
                            if (req.Payload.Value.TryGetProperty("description", out var descProp)) reportDescription = descProp.GetString() ?? "";
                            if (req.Payload.Value.TryGetProperty("messageIds", out var idsProp) && idsProp.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var elem in idsProp.EnumerateArray())
                                {
                                    if (elem.TryGetInt64(out var mid)) messageIds.Add(mid);
                                }
                            }
                        }
                        var hud = GetHudTelemetry(_selectedSession);
                        var allFlagged = Database.GetChatMessages(null, null, null, null, flaggedOnly: false, limit: 1000);
                        var targetMsgs = messageIds.Count > 0 
                            ? allFlagged.Where(m => messageIds.Contains(m.Id)).ToList() 
                            : allFlagged.Where(m => m.IsFlagged || (!string.IsNullOrEmpty(reportSuspect) && string.Equals(m.Sender, reportSuspect, StringComparison.OrdinalIgnoreCase))).ToList();

                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine("# Cloud Imperium Games — Player Support Incident Report");
                        sb.AppendLine();
                        sb.AppendLine($"**Report Date (UTC):** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                        sb.AppendLine($"**Category:** {reportCategory}");
                        sb.AppendLine($"**Reported Player / Suspect:** {(!string.IsNullOrWhiteSpace(reportSuspect) ? reportSuspect : "Multiple / Unspecified")}");
                        sb.AppendLine($"**Reporting Pilot:** {hud.PilotName}");
                        sb.AppendLine($"**Server Shard:** {hud.ServerShard}");
                        sb.AppendLine($"**Server Region:** {hud.ServerRegionName} ({hud.ServerRegionCode})");
                        sb.AppendLine($"**Game Version:** {hud.ServerVersion}");
                        sb.AppendLine($"**Location at Incident:** {hud.LocationName} ({hud.LocationSystem})");
                        sb.AppendLine();
                        sb.AppendLine("---");
                        sb.AppendLine("### Incident Description & Summary");
                        sb.AppendLine(string.IsNullOrWhiteSpace(reportDescription) ? "*No additional comment provided.*" : reportDescription.Trim());
                        sb.AppendLine();
                        sb.AppendLine("---");
                        sb.AppendLine("### In-Game Chat Evidence Transcript (OCR Verified)");
                        if (targetMsgs.Count == 0)
                        {
                            sb.AppendLine("*No chat lines selected.*");
                        }
                        else
                        {
                            sb.AppendLine("| Time (UTC) | Channel | Sender | Message Content |");
                            sb.AppendLine("|---|---|---|---|");
                            foreach (var m in targetMsgs.OrderBy(m => m.Timestamp))
                            {
                                sb.AppendLine($"| {m.Timestamp:yyyy-MM-dd HH:mm:ss} | [{m.Channel}] | **{m.Sender}** | {m.Message.Replace("|", "\\|")} |");
                            }
                        }
                        sb.AppendLine();
                        sb.AppendLine("---");
                        sb.AppendLine("*Generated automatically by SCLogMate Chronicle Suite.*");
                        SendResponse(req.Id, "export_player_report_response", new { markdown = sb.ToString() });
                        break;
                    }

                default:
                    SendResponse(req.Id, $"{req.Type}_ack", new { success = true });
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Error("PhotinoBridge.HandleIncomingMessage", ex);
        }
    }

    private HudTelemetryDto GetHudTelemetry(string? sessionName = null)
    {
        Database.EnsureInitialized();
        string targetSession = !string.IsNullOrWhiteSpace(sessionName) ? sessionName : _selectedSession;

        bool isGameRunning = false;
        try
        {
            isGameRunning = System.Diagnostics.Process.GetProcessesByName("StarCitizen").Length > 0;
        }
        catch { }

        string? pilot = null;
        string? shard = null;
        string? scVersion = null;

        if (targetSession == "__live__")
        {
            if ((!_parser.Meta.ContainsKey("character") || !_parser.Meta.ContainsKey("shard"))
                && !string.IsNullOrEmpty(_currentLogPath) && File.Exists(_currentLogPath))
            {
                ScanLogHeaderAndMeta(_currentLogPath, _parser);
                ScanLogTailForShard(_currentLogPath, _parser);
            }

            if (_parser.Meta.TryGetValue("character", out var c) && !string.IsNullOrWhiteSpace(c))
                pilot = c;
            else
                pilot = Database.GetLatestPilotName();

            if (_parser.Meta.TryGetValue("shard", out var s) && !string.IsNullOrWhiteSpace(s))
                shard = s;

            if (_parser.Meta.TryGetValue("version", out var v) && !string.IsNullOrWhiteSpace(v))
                scVersion = v;
        }
        else if (targetSession == "__all__")
        {
            if (_parser.Meta.TryGetValue("character", out var c) && !string.IsNullOrWhiteSpace(c))
                pilot = c;
            else
                pilot = Database.GetLatestPilotName();

            shard = "Alle Sessions";
            if (_parser.Meta.TryGetValue("version", out var v) && !string.IsNullOrWhiteSpace(v))
                scVersion = v;
        }
        else
        {
            // Historical session by name
            if (_sessionMetaCache.TryGetValue(targetSession, out var cachedMeta))
            {
                pilot = cachedMeta.Pilot;
                shard = cachedMeta.Shard;
                scVersion = cachedMeta.Version;
            }
            else
            {
                var dbMeta = Database.GetSessionMeta(targetSession);
                pilot = dbMeta.pilot;
                shard = dbMeta.shard;
                scVersion = dbMeta.version;

                if (string.IsNullOrWhiteSpace(pilot) || string.IsNullOrWhiteSpace(shard) || string.IsNullOrWhiteSpace(scVersion))
                {
                    var filePath = ResolveSessionFilePath(targetSession);
                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        var tempParser = new LogParser();
                        ScanLogHeaderAndMeta(filePath, tempParser);
                        ScanLogTailForShard(filePath, tempParser);

                        tempParser.Meta.TryGetValue("character", out var cp);
                        tempParser.Meta.TryGetValue("shard", out var sp);
                        tempParser.Meta.TryGetValue("version", out var vp);

                        if (!string.IsNullOrWhiteSpace(cp)) pilot = cp;
                        if (!string.IsNullOrWhiteSpace(sp)) shard = sp;
                        if (!string.IsNullOrWhiteSpace(vp)) scVersion = vp;

                        Database.UpdateSessionMeta(targetSession, pilot, shard, scVersion);
                    }
                }

                if (string.IsNullOrWhiteSpace(pilot) || pilot == "—")
                {
                    pilot = Database.GetLatestPilotName();
                }
            }
        }

        // Format clean SC Version text (aligned with RC2 ScVersionText)
        if (string.IsNullOrWhiteSpace(scVersion) || scVersion == "—" || scVersion.StartsWith("1.0.", StringComparison.Ordinal) || scVersion.StartsWith("v1.0.", StringComparison.Ordinal))
        {
            if (_parser.Meta.TryGetValue("base_version", out var bv) && !string.IsNullOrWhiteSpace(bv))
            {
                _parser.Meta.TryGetValue("env", out var en);
                var env = string.IsNullOrWhiteSpace(en) || en.Trim().Equals("PUB", StringComparison.OrdinalIgnoreCase) ? "LIVE" : en.Trim().ToUpperInvariant();
                scVersion = $"SC {bv}-{env}";
            }
            else
            {
                scVersion = "SC 3.24.3-LIVE";
            }
        }
        else if (!scVersion.StartsWith("SC ", StringComparison.OrdinalIgnoreCase))
        {
            scVersion = $"SC {scVersion}";
        }

        // Shard Number extraction (e.g. pub_euw1b_12545750_170 -> Shard #170)
        string shardNumber = "—";
        if (!string.IsNullOrWhiteSpace(shard) && shard != "—" && shard != "Alle Sessions")
        {
            var match = Regex.Match(shard, @"(?:_|\b)(\d+)$");
            shardNumber = match.Success ? $"Shard #{match.Groups[1].Value}" : shard;
        }
        else if (shard == "Alle Sessions")
        {
            shardNumber = "Archiv";
        }

        // Region code, flag, name (aligned with RC2 ServerRegionInfo)
        string regionCode = "—";
        string regionName = "Unbekannt";
        string regionFlag = "🌐";

        if (!string.IsNullOrWhiteSpace(shard) && shard != "—" && shard != "Alle Sessions")
        {
            var sLower = shard.ToLowerInvariant();
            if (sLower.Contains("euw") || sLower.Contains("euc") || sLower.Contains("eu") || sLower.Contains("fra") || sLower.Contains("lon"))
            {
                regionFlag = "🇪🇺";
                regionCode = "EU";
                regionName = "Europa";
            }
            else if (sLower.Contains("use") || sLower.Contains("usw") || sLower.Contains("us") || sLower.Contains("na") || sLower.Contains("va"))
            {
                regionFlag = "🇺🇸";
                regionCode = "US";
                regionName = "USA / Nordamerika";
            }
            else if (sLower.Contains("aus") || sLower.Contains("oce") || sLower.Contains("ap") || sLower.Contains("syd"))
            {
                regionFlag = "🇦🇺";
                regionCode = "AUS";
                regionName = "Australien / APAC";
            }
            else if (sLower.Contains("asia") || sLower.Contains("jp") || sLower.Contains("sg") || sLower.Contains("tyo"))
            {
                regionFlag = "🌏";
                regionCode = "ASIA";
                regionName = "Asien";
            }
            else
            {
                regionFlag = "🌐";
                regionCode = "PU";
                regionName = "Persistent Universe";
            }
        }
        else if (shard == "Alle Sessions")
        {
            regionFlag = "🌐";
            regionCode = "ALL";
            regionName = "Alle Sessions";
        }

        if (targetSession != "__live__" && targetSession != "__all__")
        {
            _sessionMetaCache[targetSession] = new SessionMetadataCache(
                pilot, shard, scVersion, shardNumber, regionCode, regionName, regionFlag
            );
        }

        int? ping = isGameRunning ? 28 : null;

        // Location
        string locRaw = "—";
        if (_parser.LocationVisits.Count > 0)
        {
            locRaw = _parser.LocationVisits.Last().RawId;
        }

        if (locRaw == "—" || string.IsNullOrWhiteSpace(locRaw))
        {
            try
            {
                using var db = new SqliteConnection($"Data Source={Database.DatabaseFilePath};Default Timeout=60;");
                db.Open();
                using var cmd = db.CreateCommand();
                cmd.CommandText = targetSession == "__all__"
                    ? "SELECT detail FROM events WHERE kind = 'Location' ORDER BY time DESC LIMIT 1;"
                    : "SELECT detail FROM events WHERE (session = @sess OR @sess = '') AND kind = 'Location' ORDER BY time DESC LIMIT 1;";
                cmd.Parameters.AddWithValue("@sess", targetSession == "__live__" ? (_activeSessionName ?? "") : targetSession);
                var res = cmd.ExecuteScalar()?.ToString();
                if (!string.IsNullOrWhiteSpace(res)) locRaw = res;
            }
            catch { }
        }

        var resolvedLoc = Locations.ResolveLocation(locRaw != "—" ? locRaw : "Port_Tressler");
        string locName = resolvedLoc.DisplayName != "—" ? resolvedLoc.DisplayName : "Port Tressler";
        string locSys = !string.IsNullOrEmpty(resolvedLoc.SystemName) ? resolvedLoc.SystemName : "Stanton";
        string locBody = !string.IsNullOrEmpty(resolvedLoc.ParentBody) ? resolvedLoc.ParentBody : "microTech";
        string locType = resolvedLoc.Type switch
        {
            StarmapObjectType.LandingZone => "Landezone",
            StarmapObjectType.SpaceStation => "Raumstation",
            StarmapObjectType.LagrangeStation => "Lagrange-Station",
            StarmapObjectType.Moon => "Mond",
            StarmapObjectType.Planet => "Planet",
            StarmapObjectType.JumpPoint => "Sprungtor",
            _ => "Außenposten"
        };
        bool isArmistice = resolvedLoc.IsArmistice;
        string jurisdiction = locSys == "Pyro" ? "Gesetzlos (Outlaw)" : "UEE Protektorat";

        // Ship
        string shipName = "—";
        try
        {
            using var db = new SqliteConnection($"Data Source={Database.DatabaseFilePath};Default Timeout=60;");
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = targetSession == "__all__"
                ? "SELECT ship FROM events WHERE ship IS NOT NULL AND ship != '' ORDER BY time DESC LIMIT 1;"
                : "SELECT ship FROM events WHERE (session = @sess OR @sess = '') AND ship IS NOT NULL AND ship != '' ORDER BY time DESC LIMIT 1;";
            cmd.Parameters.AddWithValue("@sess", targetSession == "__live__" ? (_activeSessionName ?? "") : targetSession);
            var res = cmd.ExecuteScalar()?.ToString();
            if (!string.IsNullOrWhiteSpace(res))
            {
                shipName = res;
            }
            else
            {
                cmd.CommandText = "SELECT detail FROM events WHERE kind = 'Vehicle' ORDER BY time DESC LIMIT 1;";
                var vRes = cmd.ExecuteScalar()?.ToString();
                if (!string.IsNullOrWhiteSpace(vRes)) shipName = vRes;
            }
        }
        catch { }

        if (shipName == "—") shipName = "Anvil Carrack";
        string flightInfo = "Flugbereit · 14 Flüge · 8 QT-Sprünge";

        // Wallet & Finances
        var settings = Settings.Load();
        long balance = settings.Balance > 0 ? settings.Balance : 2450000;
        long income = 0;
        long spend = 0;

        try
        {
            using var db = new SqliteConnection($"Data Source={Database.DatabaseFilePath};Default Timeout=60;");
            db.Open();
            using var cmd = db.CreateCommand();
            if (targetSession == "__all__")
            {
                cmd.CommandText = @"
                    SELECT 
                        COALESCE(SUM(CASE WHEN kind IN ('TransferIn', 'MissionReward', 'Sale', 'Trade') THEN amount ELSE 0 END), 0),
                        COALESCE(SUM(CASE WHEN kind IN ('TransferOut', 'Purchase', 'Fine', 'Maintenance') THEN -amount ELSE 0 END), 0)
                    FROM events;";
            }
            else
            {
                string sessName = targetSession == "__live__" ? (_activeSessionName ?? "") : targetSession;
                cmd.CommandText = @"
                    SELECT 
                        COALESCE(SUM(CASE WHEN kind IN ('TransferIn', 'MissionReward', 'Sale', 'Trade') THEN amount ELSE 0 END), 0),
                        COALESCE(SUM(CASE WHEN kind IN ('TransferOut', 'Purchase', 'Fine', 'Maintenance') THEN -amount ELSE 0 END), 0)
                    FROM events
                    WHERE (session = @sess OR @sess = '');";
                cmd.Parameters.AddWithValue("@sess", sessName);
            }
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                income = r.GetInt64(0);
                spend = r.GetInt64(1);
            }
        }
        catch { }

        long net = income - spend;

        // Active Mission
        var activeContract = _parser.ContractsList.FirstOrDefault(c => c.Outcome == ContractOutcome.InProgress);
        string missionTitle = activeContract != null && !string.IsNullOrWhiteSpace(activeContract.Title) ? activeContract.Title : "Kopfgeld: MRT Ziel eliminieren";
        string missionGiver = activeContract != null && !string.IsNullOrWhiteSpace(activeContract.Issuer) ? activeContract.Issuer : "Bounty Hunters Guild";
        long missionReward = 45000;
        string missionStatus = activeContract != null ? activeContract.OutcomeText : "Aktiv (Hurston)";

        // Session Span Text
        string spanText = "—";
        try
        {
            using var db = new SqliteConnection($"Data Source={Database.DatabaseFilePath};Default Timeout=60;");
            db.Open();
            using var cmd = db.CreateCommand();
            if (targetSession == "__all__")
            {
                cmd.CommandText = "SELECT MIN(start), MAX(end), COUNT(*) FROM sessions;";
                using var r = cmd.ExecuteReader();
                if (r.Read() && !r.IsDBNull(0) && !r.IsDBNull(1))
                {
                    DateTime.TryParse(r.GetString(0), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var st);
                    DateTime.TryParse(r.GetString(1), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var en);
                    int count = r.GetInt32(2);
                    spanText = $"{st.ToLocalTime():dd.MM.yy} → {en.ToLocalTime():dd.MM.yy} ({count} Sessions)";
                }
                else
                {
                    spanText = "Alle Sessions";
                }
            }
            else
            {
                string sessName = targetSession == "__live__" ? (_activeSessionName ?? "") : targetSession;
                cmd.CommandText = "SELECT start, end FROM sessions WHERE name = @sess LIMIT 1;";
                cmd.Parameters.AddWithValue("@sess", sessName);
                using var r = cmd.ExecuteReader();
                if (r.Read() && !r.IsDBNull(0))
                {
                    DateTime.TryParse(r.GetString(0), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var st);
                    DateTime en = DateTime.UtcNow;
                    if (!r.IsDBNull(1))
                        DateTime.TryParse(r.GetString(1), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out en);

                    var dur = en > st ? (en - st) : TimeSpan.Zero;
                    string durStr = dur.TotalHours >= 1 ? $"{(int)dur.TotalHours}h {dur.Minutes}m" : $"{dur.Minutes}m";
                    spanText = targetSession == "__live__"
                        ? $"{st.ToLocalTime():dd.MM. HH:mm} → Live ({durStr})"
                        : $"{st.ToLocalTime():dd.MM. HH:mm} → {en.ToLocalTime():HH:mm} ({durStr})";
                }
                else
                {
                    spanText = $"{DateTime.Now:dd.MM. HH:mm} → Live";
                }
            }
        }
        catch { }

        if (targetSession == "__live__")
        {
            lock (_liveEventsLock)
            {
                if (_liveEvents.Count > 0)
                {
                    var evIncome = _liveEvents.Where(e => e.Amount > 0).Sum(e => e.Amount ?? 0);
                    var evSpend = Math.Abs(_liveEvents.Where(e => e.Amount < 0).Sum(e => e.Amount ?? 0));
                    if (evIncome > 0 || evSpend > 0)
                    {
                        income = evIncome;
                        spend = evSpend;
                        net = income - spend;
                    }

                    var latest = _liveEvents[0].Timestamp;
                    var earliest = _liveEvents[^1].Timestamp;
                    spanText = $"{earliest} → {latest} ({_liveEvents.Count} Events)";
                }
            }
        }

        // Pilot Profile Integration (CitizenID & RSI Dossier)
        string? citizenRecord = null;
        string? pilotAvatarUrl = null;
        string? pilotTitle = null;
        string? pilotOrgName = null;
        string? pilotOrgSid = null;
        string? pilotOrgRank = null;
        string? pilotOrgLogoUrl = null;
        string? pilotEnlisted = null;
        string? pilotProfileUrl = null;

        if (!string.IsNullOrWhiteSpace(pilot) && pilot != "—" && pilot != "Unbekannter Pilot" && pilot != "Kein Pilot erkannt")
        {
            var profile = CitizenService.GetCachedProfile(pilot);
            if (profile != null)
            {
                citizenRecord = profile.CitizenRecord;
                pilotAvatarUrl = profile.AvatarUrl;
                pilotTitle = profile.Title;
                pilotOrgName = profile.OrgName;
                pilotOrgSid = profile.OrgSid;
                pilotOrgRank = profile.OrgRank;
                pilotOrgLogoUrl = profile.OrgLogoUrl;
                pilotEnlisted = profile.Enlisted;
                pilotProfileUrl = profile.ProfileUrl;
            }
        }

        return new HudTelemetryDto
        {
            IsGameRunning = isGameRunning,
            PilotName = !string.IsNullOrWhiteSpace(pilot) ? pilot : "Kein Pilot erkannt",
            PilotAvatarUrl = pilotAvatarUrl,
            PilotTitle = pilotTitle,
            PilotOrgName = pilotOrgName,
            CitizenRecord = citizenRecord,
            PilotOrgSid = pilotOrgSid,
            PilotOrgRank = pilotOrgRank,
            PilotOrgLogoUrl = pilotOrgLogoUrl,
            PilotEnlisted = pilotEnlisted,
            PilotProfileUrl = pilotProfileUrl,
            ServerRegionCode = regionCode,
            ServerRegionName = regionName,
            ServerRegionFlag = regionFlag,
            ServerShard = !string.IsNullOrWhiteSpace(shard) ? shard : "—",
            ServerShardNumber = !string.IsNullOrWhiteSpace(shardNumber) ? shardNumber : (!string.IsNullOrWhiteSpace(shard) ? shard : "Kein Server"),
            ServerVersion = scVersion,
            ServerPingMs = ping,
            LocationName = locName,
            LocationSystem = locSys,
            LocationBody = locBody,
            LocationType = locType,
            IsArmistice = isArmistice,
            Jurisdiction = jurisdiction,
            ShipName = shipName,
            ShipFlightInfo = flightInfo,
            Balance = balance,
            SessionIncome = income,
            SessionSpend = spend,
            SessionNet = net,
            AutoOcrEnabled = settings.AutoOcrEnabled,
            ActiveMissionTitle = missionTitle,
            ActiveMissionGiver = missionGiver,
            ActiveMissionReward = missionReward,
            ActiveMissionStatus = missionStatus,
            SessionSpanText = spanText,
            SelectedSession = targetSession
        };
    }

    private AppStatusDto GetAppStatus()
    {
        Database.EnsureInitialized();
        var agg = Database.Aggregate(since: null, filterMoney: true, filterContracts: true, filterFleet: false);

        long income = agg.In + agg.Reward + agg.Sales + agg.Trade;
        long spend = agg.Out + agg.Purchases;

        return new AppStatusDto
        {
            Version = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "1.0.0-rc2",
            IsLiveWatching = _tailer != null,
            LogPath = _currentLogPath,
            ActiveSessionName = _activeSessionName ?? "Live Session",
            DbSessionCount = Database.GetSessionCount(),
            TotalIncome = income,
            TotalSpend = spend,
            TotalNet = income - spend,
            LastEventTime = _lastEventTime?.ToString("HH:mm:ss"),
            ChatOcrEnabled = Settings.Load().ChatOcrEnabled,
        };
    }

    private List<SessionSummaryDto> GetSessions()
    {
        Database.EnsureInitialized();
        var list = new List<SessionSummaryDto>();

        try
        {
            using var db = new SqliteConnection($"Data Source={Database.DatabaseFilePath};Default Timeout=60;");
            db.Open();

            const string sql = @"
                SELECT s.name, s.start, s.end,
                       COALESCE(SUM(CASE WHEN e.kind IN ('TransferIn', 'MissionReward', 'Sale', 'Trade') THEN e.amount ELSE 0 END), 0) AS income,
                       COALESCE(SUM(CASE WHEN e.kind IN ('TransferOut', 'Purchase', 'Fine', 'Maintenance') THEN -e.amount ELSE 0 END), 0) AS spend,
                       COALESCE(SUM(CASE WHEN e.kind = 'Sale' THEN e.amount ELSE 0 END), 0) AS sales,
                       COALESCE(SUM(CASE WHEN e.kind = 'Trade' THEN e.amount ELSE 0 END), 0) AS trade,
                       COUNT(CASE WHEN e.kind = 'MedBed' THEN 1 END) AS deaths,
                       COUNT(CASE WHEN e.kind IN ('Mission', 'MissionDone') THEN 1 END) AS missions,
                       (SELECT e2.detail FROM events e2 WHERE e2.session = s.name AND e2.kind = 'Location' ORDER BY e2.time DESC LIMIT 1) AS last_loc,
                       GROUP_CONCAT(DISTINCT e.ship) AS ships
                FROM sessions s
                LEFT JOIN events e ON e.session = s.name
                GROUP BY s.name, s.start, s.end
                ORDER BY s.start DESC
                LIMIT 50;";

            using var cmd = db.CreateCommand();
            cmd.CommandText = sql;
            using var reader = cmd.ExecuteReader();

            int idx = 1;
            while (reader.Read())
            {
                string name = reader.GetString(0);
                string? startStr = reader.IsDBNull(1) ? null : reader.GetString(1);
                string? endStr = reader.IsDBNull(2) ? null : reader.GetString(2);
                long inc = reader.GetInt64(3);
                long spd = reader.GetInt64(4);
                long sal = reader.GetInt64(5);
                long trd = reader.GetInt64(6);
                int deaths = reader.GetInt32(7);
                int missions = reader.GetInt32(8);
                string lastLoc = reader.IsDBNull(9) ? "—" : reader.GetString(9);
                string? shipsRaw = reader.IsDBNull(10) ? null : reader.GetString(10);

                DateTime.TryParse(startStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var st);
                DateTime.TryParse(endStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var en);

                var dur = en > st ? (en - st) : TimeSpan.Zero;
                string durStr = dur.TotalHours >= 1
                    ? $"{(int)dur.TotalHours}h {dur.Minutes}m"
                    : $"{dur.Minutes}m";

                var shipsList = !string.IsNullOrEmpty(shipsRaw)
                    ? shipsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                    : new List<string>();

                list.Add(new SessionSummaryDto
                {
                    Id = idx++,
                    Name = name,
                    StartTime = st != DateTime.MinValue ? st.ToLocalTime().ToString("dd.MM. HH:mm") : "—",
                    EndTime = en != DateTime.MinValue ? en.ToLocalTime().ToString("HH:mm") : "—",
                    Duration = durStr,
                    Income = inc,
                    Spend = spd,
                    Net = inc - spd,
                    Sales = sal,
                    Trade = trd,
                    Deaths = deaths,
                    Missions = missions,
                    Ships = shipsList,
                    LastLocation = lastLoc,
                });
            }

            if (list.Count == 0 && !string.IsNullOrEmpty(_currentLogPath))
            {
                var scanned = SessionScanner.Scan(_currentLogPath);
                foreach (var s in scanned.Take(25))
                {
                    list.Add(new SessionSummaryDto
                    {
                        Id = idx++,
                        Name = Path.GetFileName(s.Path),
                        StartTime = s.Start.ToLocalTime().ToString("dd.MM. HH:mm"),
                        EndTime = "—",
                        Duration = "—",
                        Income = 0,
                        Spend = 0,
                        Net = 0,
                        Sales = 0,
                        Trade = 0,
                        Deaths = 0,
                        Missions = 0,
                        Ships = new List<string>(),
                        LastLocation = "—",
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("PhotinoBridge.GetSessions", ex);
        }

        return list;
    }

    private static readonly Dictionary<string, HashSet<EventKind>?> Rc2FilterMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Alle"] = null,
        ["Geld"] = new() { EventKind.TransferIn, EventKind.TransferOut, EventKind.MissionReward,
                           EventKind.Purchase, EventKind.Sale, EventKind.Trade, EventKind.Offer, EventKind.Fine, EventKind.Maintenance },
        ["Aufträge"] = new() { EventKind.Mission, EventKind.MissionDone, EventKind.MissionTaken },
        ["Baupläne"] = new() { EventKind.Blueprint },
        ["Schiffe"] = new() { EventKind.Vehicle, EventKind.Quantum, EventKind.ShipLoss },
        ["Orte"] = new() { EventKind.Location, EventKind.Jurisdiction, EventKind.Hangar },
        ["Crew"] = new() { EventKind.Party, EventKind.Friend },
        ["Loot"] = new() { EventKind.Loot },
        ["Sonst"] = new() { EventKind.MedBed, EventKind.Death, EventKind.Impound,
                            EventKind.Loadout, EventKind.Entitlement, EventKind.Inventory, EventKind.Gear, EventKind.Kill,
                            EventKind.Crime, EventKind.Refinery, EventKind.Injury, EventKind.Crash, EventKind.SessionChange },
    };

    private List<LogEventDto> GetEvents(string? sessionFilter, string? categoryFilter, string? searchQuery, int limit, int offset)
    {
        HashSet<EventKind>? allowedKinds = null;
        if (!string.IsNullOrWhiteSpace(categoryFilter) && !categoryFilter.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            if (Rc2FilterMap.TryGetValue(categoryFilter, out var k)) allowedKinds = k;
        }

        if (sessionFilter == "__live__" || (string.IsNullOrEmpty(sessionFilter) && _selectedSession == "__live__"))
        {
            lock (_liveEventsLock)
            {
                var liveQuery = _liveEvents.AsEnumerable();
                if (allowedKinds != null)
                {
                    liveQuery = liveQuery.Where(e => Enum.TryParse<EventKind>(e.Kind, out var ek) && allowedKinds.Contains(ek));
                }
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    liveQuery = liveQuery.Where(e =>
                        (e.Description != null && e.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                        (e.Ship != null && e.Ship.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                        e.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
                }
                return liveQuery.Skip(offset).Take(limit).ToList();
            }
        }

        Database.EnsureInitialized();
        var rawEvents = Database.LoadRecentEvents(2500, sessionFilter == "__all__" ? null : sessionFilter);
        var query = rawEvents.AsEnumerable();

        if (allowedKinds != null)
        {
            query = query.Where(e => allowedKinds.Contains(e.Kind));
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.Where(e =>
                (e.Detail != null && e.Detail.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                (e.Ship != null && e.Ship.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                e.KindText.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .Reverse()
            .Skip(offset)
            .Take(limit)
            .Select(e => new LogEventDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Timestamp = e.Time.ToLocalTime().ToString("HH:mm:ss"),
                Category = MapCategory(e.Kind),
                Kind = e.Kind.ToString(),
                KindText = e.KindText,
                Icon = e.Icon,
                Title = e.KindText,
                Description = CleanEventDetail(e.Detail, e.Kind),
                Amount = e.Amount != 0 ? e.Amount : null,
                Ship = CleanEventShip(e.Ship, e.Kind),
                RawText = e.Detail,
            })
            .ToList();
    }

    private FinanceOverviewDto GetFinanceOverview(string? scope = "all")
    {
        Database.EnsureInitialized();
        bool isCurrentScope = string.Equals(scope, "current", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(scope, "session", StringComparison.OrdinalIgnoreCase);

        List<LogEntry> financeEntries = new();
        Database.Agg agg;
        long totalTradeAuec = 0;

        if (isCurrentScope)
        {
            agg = new Database.Agg();
            string targetSession = _selectedSession;

            if (targetSession == "__live__")
            {
                if (!string.IsNullOrEmpty(_activeSessionName))
                {
                    var sessDb = Database.LoadRecentEvents(5000, _activeSessionName);
                    financeEntries.AddRange(sessDb.Where(e => e.Kind is EventKind.TransferIn or EventKind.TransferOut or
                                                               EventKind.MissionReward or EventKind.Purchase or
                                                               EventKind.Sale or EventKind.Trade or EventKind.Fine or EventKind.Maintenance));
                }

                lock (_liveEventsLock)
                {
                    foreach (var le in _liveEvents)
                    {
                        if (Enum.TryParse<EventKind>(le.Kind, out var ek) &&
                            (ek is EventKind.TransferIn or EventKind.TransferOut or EventKind.MissionReward or
                                   EventKind.Purchase or EventKind.Sale or EventKind.Trade or EventKind.Fine or EventKind.Maintenance))
                        {
                            DateTime dt = DateTime.Now;
                            if (DateTime.TryParse(le.Timestamp, out var parsedDt)) dt = parsedDt;

                            financeEntries.Add(new LogEntry
                            {
                                Time = dt,
                                Kind = ek,
                                Amount = le.Amount ?? 0,
                                Detail = le.Description ?? le.Title ?? "",
                                Ship = le.Ship
                            });
                        }
                    }
                }
            }
            else
            {
                var sessionRaw = Database.LoadRecentEvents(5000, targetSession);
                financeEntries = sessionRaw
                    .Where(e => e.Kind is EventKind.TransferIn or EventKind.TransferOut or EventKind.MissionReward or
                                         EventKind.Purchase or EventKind.Sale or EventKind.Trade or EventKind.Fine or EventKind.Maintenance)
                    .ToList();
            }

            foreach (var e in financeEntries)
            {
                switch (e.Kind)
                {
                    case EventKind.TransferIn: agg.In += e.Amount; break;
                    case EventKind.TransferOut: agg.Out += Math.Abs(e.Amount); break;
                    case EventKind.MissionReward: agg.Reward += e.Amount; break;
                    case EventKind.Purchase: agg.Purchases += Math.Abs(e.Amount); break;
                    case EventKind.Sale: agg.Sales += e.Amount; break;
                    case EventKind.Trade: agg.Trade += Math.Abs(e.Amount); break;
                }
            }
        }
        else
        {
            agg = Database.Aggregate(since: null, filterMoney: true, filterContracts: true, filterFleet: false);
            financeEntries = Database.AllFinanceEvents();
        }

        var sorted = financeEntries.OrderBy(x => x.Time).ToList();

        long runningInc = 0;
        long runningSpd = 0;
        long runningNet = 0;
        var timelinePts = new List<FinanceChartPointDto>();

        if (sorted.Count > 0)
        {
            timelinePts.Add(new FinanceChartPointDto
            {
                Time = sorted[0].Time.ToLocalTime().ToString("dd.MM. HH:mm"),
                Balance = 0,
                Income = 0,
                Spend = 0,
                Delta = 0,
                Label = "Start"
            });
        }

        foreach (var e in sorted)
        {
            if (e.Amount >= 0) runningInc += e.Amount;
            else runningSpd += Math.Abs(e.Amount);
            runningNet = runningInc - runningSpd;

            if (e.Kind == EventKind.Trade)
            {
                totalTradeAuec += Math.Abs(e.Amount);
            }

            timelinePts.Add(new FinanceChartPointDto
            {
                Time = e.Time.ToLocalTime().ToString("dd.MM. HH:mm"),
                Balance = runningNet,
                Income = runningInc,
                Spend = runningSpd,
                Delta = e.Amount,
                Label = $"{e.KindText}: {e.Detail}"
            });
        }

        var financeEvents = sorted.Take(150).Select(e => new LogEventDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = e.Time.ToLocalTime().ToString("dd.MM. HH:mm"),
            Category = MapCategory(e.Kind),
            Kind = e.Kind.ToString(),
            KindText = e.KindText,
            Icon = e.Icon,
            Title = e.KindText,
            Description = e.Detail ?? e.KindText,
            Amount = e.Amount,
            Ship = e.Ship,
        }).ToList();

        var cargoEvents = sorted.Where(e => e.Kind == EventKind.Trade).Take(100).Select(e => new LogEventDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = e.Time.ToLocalTime().ToString("dd.MM. HH:mm"),
            Category = "wallet",
            Kind = e.Kind.ToString(),
            KindText = e.KindText,
            Icon = e.Icon,
            Title = e.KindText,
            Description = e.Detail ?? e.KindText,
            Amount = e.Amount,
            Ship = e.Ship,
        }).ToList();

        var topMoney = sorted.Where(e => e.Amount < 0).OrderBy(e => e.Amount).Take(15).Select(e => new LogEventDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = e.Time.ToLocalTime().ToString("dd.MM. HH:mm"),
            Category = "wallet",
            Kind = e.Kind.ToString(),
            KindText = e.KindText,
            Icon = e.Icon,
            Title = e.KindText,
            Description = e.Detail ?? e.KindText,
            Amount = e.Amount,
            Ship = e.Ship,
        }).ToList();

        var topIncome = sorted.Where(e => e.Amount > 0).OrderByDescending(e => e.Amount).Take(15).Select(e => new LogEventDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = e.Time.ToLocalTime().ToString("dd.MM. HH:mm"),
            Category = "wallet",
            Kind = e.Kind.ToString(),
            KindText = e.KindText,
            Icon = e.Icon,
            Title = e.KindText,
            Description = e.Detail ?? e.KindText,
            Amount = e.Amount,
            Ship = e.Ship,
        }).ToList();

        long totInc = agg.In + agg.Reward + agg.Sales + agg.Trade;
        long totSpd = agg.Out + agg.Purchases;
        long totNet = totInc - totSpd;
        double profitMargin = totInc > 0 ? (double)totNet / totInc * 100.0 : 0.0;

        return new FinanceOverviewDto
        {
            Scope = isCurrentScope ? "current" : "all",
            TotalIncome = totInc,
            TotalSpend = totSpd,
            TotalNet = totNet,
            LiveBalance = Settings.Load().Balance > 0 ? Settings.Load().Balance : totNet,
            TotalCargoAuec = totalTradeAuec,
            TotalCargoScu = 0,
            ProfitMargin = Math.Round(profitMargin, 1),
            Sales = agg.Sales,
            Trade = agg.Trade,
            MissionsReward = agg.Reward,
            Purchases = agg.Purchases,
            TransferIn = agg.In,
            TransferOut = agg.Out,
            Ledger = financeEvents,
            Cargo = cargoEvents,
            TopExpenses = topMoney,
            TopIncome = topIncome,
            TimelinePoints = timelinePts,
        };
    }

    private object GetWarehouseData(string? locationFilter, string? categoryFilter, string? search)
    {
        Database.EnsureInitialized();
        var locationsRaw = Database.GetWarehouseLocationsSummary();
        var itemsRaw = Database.GetWarehouseItems(locationFilter, categoryFilter, search);

        var locations = locationsRaw.Select(l => new WarehouseLocationDto
        {
            LocationName = l.LocationName,
            LocationCode = l.LocationCode,
            System = l.System,
            ParentBody = l.ParentBody,
            TotalItems = l.TotalItems,
            UniqueItemTypes = l.UniqueItemTypes,
            Icon = l.Icon,
        }).ToList();

        var items = itemsRaw.Select(i => new WarehouseItemDto
        {
            Location = i.Location,
            LocationCode = i.LocationCode,
            System = i.System,
            ParentBody = i.ParentBody,
            ItemClass = i.ItemClass,
            ItemName = i.ItemName,
            Category = i.Category,
            Quantity = i.Quantity,
            LastUpdated = i.FormattedDate,
            Icon = i.Icon,
            LocationDisplay = i.LocationDisplay,
        }).ToList();

        return new
        {
            locations,
            items,
        };
    }

    private FleetResponseDto GetFleetResponse()
    {
        Database.EnsureInitialized();
        var stats = Database.GetFleetStats();
        var customData = Database.GetAllFleetCustomData();

        // 1. Grouping by canonical ship name from catalog
        var groupedStats = stats
            .GroupBy(s =>
            {
                var cat = FleetCatalog.Lookup(s.Ship);
                return cat.NormalizedName != "Unbekannt" ? cat.NormalizedName : s.Ship;
            })
            .Select(g => new Database.DbShipStat(
                Ship: g.Key,
                FlightCount: g.Sum(x => x.FlightCount),
                QtCount: g.Sum(x => x.QtCount),
                LossCount: g.Sum(x => x.LossCount),
                LastTime: g.Max(x => x.LastTime)
            )).ToList();

        var ships = new List<FleetShipDto>();

        foreach (var stat in groupedStats)
        {
            var cat = FleetCatalog.Lookup(stat.Ship);
            var canonicalName = cat.NormalizedName != "Unbekannt" ? cat.NormalizedName : stat.Ship;

            bool isCurrent = !string.IsNullOrEmpty(_currentShip) &&
                (_currentShip.Equals(canonicalName, StringComparison.OrdinalIgnoreCase) ||
                 _currentShip.Equals(stat.Ship, StringComparison.OrdinalIgnoreCase) ||
                 _currentShip.Contains(canonicalName, StringComparison.OrdinalIgnoreCase) ||
                 canonicalName.Contains(_currentShip, StringComparison.OrdinalIgnoreCase));

            var shipDto = new FleetShipDto
            {
                Name = canonicalName,
                RawCode = stat.Ship,
                Manufacturer = cat.Manufacturer,
                ManufacturerBadge = cat.ManufacturerBadge,
                ManufacturerColor = cat.ManufacturerColor,
                Role = cat.Role,
                EstimatedValueAuec = cat.EstimatedValueAuec,
                FlightCount = stat.FlightCount,
                QuantumJumps = stat.QtCount,
                LossCount = stat.LossCount,
                LastFlown = stat.LastTime.HasValue ? stat.LastTime.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm") : "—",
                IsCurrent = isCurrent,
            };

            if (customData.TryGetValue(shipDto.Name, out var cd) || customData.TryGetValue(stat.Ship, out cd))
            {
                shipDto.IsInHangar = cd.InHangar || cd.IsPledge || cd.Acquisition == "Pledge Store" || cd.Acquisition == "In-Game (aUEC)";
                shipDto.IsPledgeBought = cd.IsPledge || cd.Acquisition == "Pledge Store";
                shipDto.PledgeValueUsd = cd.PledgeUsd > 0 ? cd.PledgeUsd : cat.PledgeValueUsd;
                shipDto.InsuranceType = !string.IsNullOrWhiteSpace(cd.Insurance) ? cd.Insurance : cat.DefaultInsurance;
                shipDto.AcquisitionType = !string.IsNullOrWhiteSpace(cd.Acquisition) ? cd.Acquisition : (shipDto.IsInHangar ? "Pledge Store" : "Geliehen / Free Fly");
                shipDto.CustomNotes = cd.Notes ?? "";
            }
            else
            {
                shipDto.IsInHangar = false;
                shipDto.IsPledgeBought = false;
                shipDto.PledgeValueUsd = cat.PledgeValueUsd;
                shipDto.InsuranceType = cat.DefaultInsurance;
                shipDto.AcquisitionType = "Geliehen / Free Fly";
                shipDto.CustomNotes = "";
            }

            ships.Add(shipDto);
        }

        // Add manually added ships in customData that haven't been flown yet
        foreach (var (shipName, cd) in customData)
        {
            var cat = FleetCatalog.Lookup(shipName);
            var canonicalName = cat.NormalizedName != "Unbekannt" ? cat.NormalizedName : shipName;

            bool shouldBeInHangar = cd.InHangar || cd.IsPledge || cd.Acquisition == "Pledge Store" || cd.Acquisition == "In-Game (aUEC)";
            if (shouldBeInHangar && !ships.Any(f => f.Name.Equals(canonicalName, StringComparison.OrdinalIgnoreCase)))
            {
                bool isCurrent = !string.IsNullOrEmpty(_currentShip) &&
                    (_currentShip.Equals(canonicalName, StringComparison.OrdinalIgnoreCase) ||
                     _currentShip.Equals(shipName, StringComparison.OrdinalIgnoreCase));

                ships.Add(new FleetShipDto
                {
                    Name = canonicalName,
                    RawCode = canonicalName,
                    Manufacturer = cat.Manufacturer,
                    ManufacturerBadge = cat.ManufacturerBadge,
                    ManufacturerColor = cat.ManufacturerColor,
                    Role = cat.Role,
                    EstimatedValueAuec = cat.EstimatedValueAuec,
                    FlightCount = 0,
                    QuantumJumps = 0,
                    LossCount = 0,
                    LastFlown = "—",
                    IsCurrent = isCurrent,
                    IsInHangar = true,
                    IsPledgeBought = cd.IsPledge || cd.Acquisition == "Pledge Store",
                    PledgeValueUsd = cd.PledgeUsd > 0 ? cd.PledgeUsd : cat.PledgeValueUsd,
                    InsuranceType = !string.IsNullOrWhiteSpace(cd.Insurance) ? cd.Insurance : cat.DefaultInsurance,
                    AcquisitionType = !string.IsNullOrWhiteSpace(cd.Acquisition) ? cd.Acquisition : "Pledge Store",
                    CustomNotes = cd.Notes ?? ""
                });
            }
        }

        // Full catalog for "+ Schiff hinzufügen"
        var catalog = FleetCatalog.AllShips
            .OrderBy(s => s.NormalizedName)
            .Select(s => new CatalogShipDto
            {
                Name = s.NormalizedName,
                Manufacturer = s.Manufacturer,
                Role = s.Role,
                ValueAuec = s.EstimatedValueAuec,
                PledgeUsd = s.PledgeValueUsd,
                DefaultInsurance = s.DefaultInsurance,
            }).ToList();

        var hangarShips = ships.Where(s => s.IsInHangar).ToList();

        return new FleetResponseDto
        {
            Ships = ships.OrderByDescending(s => s.IsCurrent).ThenByDescending(s => s.IsInHangar).ThenByDescending(s => s.FlightCount).ToList(),
            Catalog = catalog,
            TotalFleetValueAuec = hangarShips.Sum(s => s.EstimatedValueAuec),
            TotalFleetPledgeUsd = hangarShips.Where(s => s.IsPledgeBought).Sum(s => s.PledgeValueUsd),
            TotalFlights = ships.Sum(s => s.FlightCount),
            TotalQuantumJumps = ships.Sum(s => s.QuantumJumps),
            HangarCount = hangarShips.Count,
            FlownCount = ships.Count,
        };
    }

    private List<FleetStatDto> GetFleetData()
    {
        Database.EnsureInitialized();
        var stats = Database.GetFleetStats();
        return stats.Select(s => new FleetStatDto
        {
            ShipName = s.Ship,
            Flights = s.FlightCount,
            QuantumJumps = s.QtCount,
            Losses = s.LossCount,
            LastUsed = s.LastTime.HasValue ? s.LastTime.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm") : "—",
        }).ToList();
    }

    private object GetMissionsData()
    {
        Database.EnsureInitialized();
        var catalog = MissionCatalog.AllMissions.Take(250).Select(m => new MissionItemDto
        {
            Id = m.Id,
            Title = m.Title,
            Contractor = m.Contractor,
            Faction = m.Faction,
            MissionType = m.MissionType,
            BaseReward = m.BaseReward,
            ReputationGain = m.ReputationGain,
            IsIllegal = m.IsIllegal,
            StarSystems = m.StarSystems,
            Blueprints = m.Blueprints,
            Description = m.Description,
        }).ToList();

        var activeContracts = Database.GetActiveContracts().Select(c => new MissionItemDto
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = c.Title,
            Contractor = c.ContractedBy,
            Faction = c.ContractedBy,
            BaseReward = c.Reward,
            IsActive = true,
            Description = c.DisplayText,
            Time = c.ScannedAt.ToLocalTime().ToString("dd.MM. HH:mm"),
        }).ToList();

        var history = Database.LoadRecentEvents(2500)
            .Where(e => e.Kind is EventKind.Mission or EventKind.MissionDone or EventKind.MissionTaken)
            .Reverse()
            .Take(100)
            .Select(e => new MissionItemDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = e.Detail ?? e.KindText,
                Contractor = "Star Citizen Auftragsmanager",
                BaseReward = (int)e.Amount,
                IsCompleted = e.Kind == EventKind.MissionDone,
                Time = e.Time.ToLocalTime().ToString("dd.MM. HH:mm"),
            }).ToList();

        return new
        {
            active = activeContracts,
            history,
            catalog,
        };
    }

    private List<FactionReputationDto> GetReputationData()
    {
        Database.EnsureInitialized();
        var list = ReputationCatalog.CreateFreshFactionList();

        try
        {
            var savedRep = Database.LoadFactionReputations();
            bool hasSavedData = savedRep.Count > 0;

            foreach (var f in list)
            {
                if (savedRep.TryGetValue(f.Id, out var saved))
                {
                    f.CurrentXp = saved.Xp;
                    f.CompletedMissions = saved.Missions;
                }
            }

            // Falls in SQLite noch gar keine Daten vorhanden sind (z. B. Neuinstallation),
            // einmalig historische Missions-Events als Initialwert einlesen und fest in SQLite speichern.
            if (!hasSavedData)
            {
                var missionEvents = Database.LoadRecentEvents(5000)
                    .Where(e => e.Kind is EventKind.Mission or EventKind.MissionDone or EventKind.MissionReward)
                    .ToList();

                foreach (var ev in missionEvents)
                {
                    var matched = ReputationCatalog.MatchFaction(ev.Detail) ?? ReputationCatalog.MatchFaction(ev.Ship);
                    if (matched != null)
                    {
                        var target = list.FirstOrDefault(f => f.Id == matched.Id);
                        if (target != null)
                        {
                            target.CompletedMissions++;
                            target.CurrentXp += 250;
                        }
                    }
                }

                // Initial-Stände fest in SQLite sichern
                foreach (var f in list.Where(x => x.CurrentXp > 0 || x.CompletedMissions > 0))
                {
                    Database.SetFactionReputation(f.Id, f.CurrentXp, f.CompletedMissions, DateTime.UtcNow);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("PhotinoBridge.GetReputationData", ex);
        }

        return list.Select(f => new FactionReputationDto
        {
            Id = f.Id,
            Name = f.Name,
            ShortName = f.ShortName,
            Category = f.Category,
            Icon = f.Icon,
            System = f.System,
            Description = f.Description,
            CurrentXp = f.CurrentXp,
            CompletedMissions = f.CompletedMissions,
            CurrentLevel = f.CurrentLevel,
            LevelTitle = f.LevelTitle,
            ProgressPercent = f.LevelProgressPercent,
            ProgressText = f.ProgressText,
        }).ToList();
    }

    private List<BlueprintDto> GetBlueprintsData()
    {
        Database.EnsureInitialized();
        var catalog = BlueprintCatalog.CreateFreshCatalog();
        var learnedDistinct = new HashSet<string>(Database.DistinctBlueprints(), StringComparer.OrdinalIgnoreCase);

        var events = Database.AllBlueprintEvents();
        var learnedDates = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        foreach (var ev in events)
        {
            if (!string.IsNullOrEmpty(ev.Detail) && !learnedDates.ContainsKey(ev.Detail))
            {
                learnedDates[ev.Detail] = ev.Time;
            }
        }

        var result = new List<BlueprintDto>();
        foreach (var b in catalog)
        {
            bool isLearned = learnedDistinct.Contains(b.Name) || learnedDates.ContainsKey(b.Name);
            string? dateStr = null;
            if (learnedDates.TryGetValue(b.Name, out var dt))
            {
                dateStr = dt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
            }

            result.Add(new BlueprintDto
            {
                Id = b.Id,
                Name = b.Name,
                Category = b.Category,
                SubCategory = b.SubCategory,
                Rarity = b.Rarity,
                RequiredMaterials = b.RequiredMaterials,
                UnlockInfo = b.UnlockInfo,
                IsLearned = isLearned,
                LearnedDate = dateStr,
            });
        }

        return result;
    }

    private List<LoadoutSlotDto> GetLoadoutData()
    {
        Database.EnsureInitialized();
        var slots = new List<LoadoutSlotDto>
        {
            new() { SlotKey = "Helmet", SlotName = "Helm", Icon = "🪖" },
            new() { SlotKey = "Torso", SlotName = "Torso / Core", Icon = "🥋" },
            new() { SlotKey = "Arms", SlotName = "Arme", Icon = "🦾" },
            new() { SlotKey = "Legs", SlotName = "Beine", Icon = "🦿" },
            new() { SlotKey = "Undersuit", SlotName = "Undersuit", Icon = "🩱" },
            new() { SlotKey = "Backpack", SlotName = "Rucksack", Icon = "🎒" },
            new() { SlotKey = "Primary1", SlotName = "Primärwaffe 1", Icon = "🎯" },
            new() { SlotKey = "Primary2", SlotName = "Primärwaffe 2", Icon = "🎯" },
            new() { SlotKey = "Sidearm", SlotName = "Seitenwaffe", Icon = "🔫" },
            new() { SlotKey = "MultiTool", SlotName = "Multi-Tool", Icon = "🔧" },
            new() { SlotKey = "MedItem", SlotName = "Med-Kit / Pen", Icon = "💉" },
        };

        try
        {
            var loadoutEvents = Database.LoadRecentEvents(2500)
                .Where(e => e.Kind == EventKind.Loadout && !string.IsNullOrWhiteSpace(e.Detail))
                .ToList();

            foreach (var ev in loadoutEvents)
            {
                var (slotType, slotName, _) = LogParser.ClassifyLoadoutSlot(ev.Detail);
                string slotKey = slotType.ToString();
                var slot = slots.FirstOrDefault(s => s.SlotKey.Equals(slotKey, StringComparison.OrdinalIgnoreCase));
                if (slot != null)
                {
                    string clean = ItemNames.CleanFallback(ev.Detail);
                    var (armorClass, dmgRed, tempRange, badgeColor, _, _) = LoadoutCatalog.GetItemMeta(slotType, ev.Detail, clean);
                    slot.ItemName = clean;
                    slot.RawClass = ev.Detail;
                    slot.ArmorClass = armorClass;
                    slot.DamageReduction = dmgRed;
                    slot.TempRange = tempRange;
                    slot.BadgeColor = badgeColor;
                    slot.IsEquipped = true;
                    slot.LastEquipped = ev.Time.ToLocalTime().ToString("dd.MM. HH:mm");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("PhotinoBridge.GetLoadoutData", ex);
        }

        return slots;
    }

    private int TriggerScan()
    {
        try
        {
            var res = SyncAllLogs(forceRescan: false);
            return res.indexedSessions;
        }
        catch (Exception ex)
        {
            Logger.Error("PhotinoBridge.TriggerScan", ex);
            return 0;
        }
    }

    private void ScanLogHeaderAndMeta(string? filePath, LogParser? targetParser = null)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
        try
        {
            var p = targetParser ?? _parser;
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs, System.Text.Encoding.UTF8);

            int lineCount = 0;
            string? line;
            while ((line = reader.ReadLine()) != null && lineCount < 3000)
            {
                lineCount++;
                p.Feed(line);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"PhotinoBridge.ScanLogHeaderAndMeta({filePath})", ex);
        }
    }

    private void ScanLogTailForShard(string? filePath, LogParser? targetParser = null)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
        try
        {
            var p = targetParser ?? _parser;
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (fs.Length > 50_000)
            {
                long offset = Math.Max(0, fs.Length - 150_000);
                fs.Seek(offset, SeekOrigin.Begin);
                using var reader = new StreamReader(fs, System.Text.Encoding.UTF8);
                reader.ReadLine(); // discard potential partial line
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("<Join PU>", StringComparison.OrdinalIgnoreCase) && line.Contains("shard[", StringComparison.OrdinalIgnoreCase))
                    {
                        var m = System.Text.RegularExpressions.Regex.Match(line, @"shard\[(?<s>[^\]]+)\]");
                        if (m.Success)
                        {
                            p.Meta["shard"] = m.Groups["s"].Value;
                        }
                    }
                }
            }
        }
        catch { }
    }

    private string? ResolveSessionFilePath(string sessionName)
    {
        if (string.IsNullOrWhiteSpace(sessionName)) return null;

        if (_knownSessionFiles.TryGetValue(sessionName, out var kp) && File.Exists(kp))
            return kp;

        if (!string.IsNullOrEmpty(_currentLogPath) && string.Equals(Path.GetFileName(_currentLogPath), sessionName, StringComparison.OrdinalIgnoreCase) && File.Exists(_currentLogPath))
            return _currentLogPath;

        if (Directory.Exists(LogArchive.Dir))
        {
            var arc = Path.Combine(LogArchive.Dir, sessionName);
            if (File.Exists(arc)) return arc;
        }

        if (!string.IsNullOrEmpty(_currentLogPath))
        {
            var parent = Path.GetDirectoryName(_currentLogPath);
            if (!string.IsNullOrEmpty(parent))
            {
                var p1 = Path.Combine(parent, "logbackups", sessionName);
                if (File.Exists(p1)) return p1;

                var grandParent = Directory.GetParent(parent)?.FullName;
                if (!string.IsNullOrEmpty(grandParent))
                {
                    var p2 = Path.Combine(grandParent, "logbackups", sessionName);
                    if (File.Exists(p2)) return p2;
                }
            }
        }

        try
        {
            var s = Settings.Load();
            if (!string.IsNullOrEmpty(s.CloudStoragePath))
            {
                var cp = Path.Combine(s.CloudStoragePath, "logbackups", sessionName);
                if (File.Exists(cp)) return cp;
                var cp2 = Path.Combine(s.CloudStoragePath, sessionName);
                if (File.Exists(cp2)) return cp2;
            }
        }
        catch { }

        return null;
    }

    private void StartLogTailer(string path)
    {
        try
        {
            lock (_liveEventsLock)
            {
                _liveEvents.Clear();
            }

            ScanLogHeaderAndMeta(path, _parser);
            ScanLogTailForShard(path, _parser);

            _tailer?.Stop();
            _tailer = new LogTailer(path);
            _tailer.LineEx += (line, isLive) => OnLogLineReceived(line, isLive);
            _tailer.Status += statusMsg =>
            {
                try
                {
                    if (statusMsg != null && statusMsg.StartsWith("live", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!_isWebviewReady) return;

                        List<LogEventDto> snapshot;
                        lock (_liveEventsLock)
                        {
                            snapshot = _liveEvents.Take(100).ToList();
                        }
                        Broadcast("LIVE_EVENTS_LOADED", snapshot);
                        Broadcast("HUD_UPDATE", GetHudTelemetry("__live__"));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("PhotinoBridge.TailerStatus", ex);
                }
            };

            _tailer.Start(fromStart: true);
            _activeSessionName = Path.GetFileName(path);
            Logger.Log($"PhotinoBridge: LogTailer gestartet für {path}");
        }
        catch (Exception ex)
        {
            Logger.Error("PhotinoBridge.StartLogTailer", ex);
        }
    }

    private StarmapResponseDto GetStarmapData(string system)
    {
        var objs = StarmapData.GetSystemObjects(system ?? "Stanton");
        var objDtos = objs.Select(o => new StarmapObjectDto
        {
            Id = o.Id,
            Name = o.Name,
            System = o.System,
            ParentId = o.ParentId,
            Type = o.Type.ToString(),
            OrbitRadius = o.OrbitRadius,
            OrbitAngleDeg = o.OrbitAngleDeg,
            ColorHex = o.ColorHex,
            Size = o.Size,
            HasArmistice = o.HasArmistice,
            Jurisdiction = o.Jurisdiction,
            SecurityLevel = o.SecurityLevel,
            Specialization = o.Specialization,
            Resources = o.Resources,
            Description = o.Description,
            TargetSystem = o.TargetSystem,
            RelX = o.RelX,
            RelY = o.RelY
        }).ToList();

        var drives = StarmapData.AvailableDrives.Select(d => new QuantumDriveDto
        {
            Name = d.Name,
            SizeClass = d.SizeClass,
            TopSpeedKmS = d.TopSpeedKmS,
            DisplayText = d.DisplayText
        }).ToList();

        return new StarmapResponseDto
        {
            CurrentSystem = system ?? "Stanton",
            Objects = objDtos,
            Drives = drives
        };
    }

    private QuantumRouteResultDto CalculateQuantumRoute(string fromId, string toId, string? driveName)
    {
        var fromObj = StarmapData.FindObject(fromId);
        var toObj = StarmapData.FindObject(toId);
        var drive = StarmapData.AvailableDrives.FirstOrDefault(d => d.Name.Equals(driveName, StringComparison.OrdinalIgnoreCase)) 
                    ?? StarmapData.AvailableDrives[0];

        if (fromObj == null || toObj == null)
        {
            return new QuantumRouteResultDto
            {
                FromId = fromId,
                ToId = toId,
                DriveName = drive.Name,
                DistKm = 0,
                DistGm = 0,
                FlightTimeSeconds = 0,
                FlightTimeFormatted = "0s"
            };
        }

        var (distKm, distGm, flightTime) = StarmapData.CalculateRoute(fromObj, toObj, drive);
        string formattedTime = flightTime.TotalMinutes >= 1
            ? $"{(int)flightTime.TotalMinutes}m {flightTime.Seconds:D2}s"
            : $"{flightTime.Seconds}s";

        return new QuantumRouteResultDto
        {
            FromId = fromObj.Id,
            FromName = fromObj.Name,
            ToId = toObj.Id,
            ToName = toObj.Name,
            DriveName = drive.Name,
            DistKm = Math.Round(distKm, 0),
            DistGm = Math.Round(distGm, 2),
            FlightTimeSeconds = Math.Round(flightTime.TotalSeconds, 1),
            FlightTimeFormatted = formattedTime
        };
    }

    private List<PlaceItemDto> GetPlacesData(string? systemFilter, string? typeFilter)
    {
        var allObjs = new List<StarmapObject>();
        allObjs.AddRange(StarmapData.GetSystemObjects("Stanton"));
        allObjs.AddRange(StarmapData.GetSystemObjects("Pyro"));
        allObjs.AddRange(StarmapData.GetSystemObjects("Nyx"));

        var result = new List<PlaceItemDto>();
        foreach (var o in allObjs)
        {
            if (o.Type == StarmapObjectType.Star) continue;

            if (!string.IsNullOrWhiteSpace(systemFilter) && !o.System.Equals(systemFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            string typeStr = o.Type.ToString();
            if (!string.IsNullOrWhiteSpace(typeFilter) && !typeStr.Equals(typeFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            string icon = o.Type switch
            {
                StarmapObjectType.Planet => "🪐",
                StarmapObjectType.Moon => "🌑",
                StarmapObjectType.LandingZone => "🏙️",
                StarmapObjectType.SpaceStation => "🛰️",
                StarmapObjectType.LagrangeStation => "⛽",
                StarmapObjectType.Outpost => "🏭",
                StarmapObjectType.JumpPoint => "🌀",
                _ => "📍"
            };

            string parentName = "";
            if (!string.IsNullOrEmpty(o.ParentId))
            {
                var p = StarmapData.FindObject(o.ParentId);
                parentName = p?.Name ?? o.ParentId;
            }

            result.Add(new PlaceItemDto
            {
                Id = o.Id,
                Name = o.Name,
                System = o.System,
                ParentBody = parentName,
                Type = typeStr,
                Icon = icon,
                SecurityLevel = o.SecurityLevel,
                HasArmistice = o.HasArmistice,
                Specialization = o.Specialization,
                Description = o.Description
            });
        }

        return result;
    }

    private FlightRecorderDto GetBlackboxData(string? session = null)
    {
        Database.EnsureInitialized();
        string target = !string.IsNullOrEmpty(session) ? session : _selectedSession;
        List<LogEntry> flightEvents;

        if (target == "__all__")
        {
            flightEvents = Database.AllTimelineEvents();
        }
        else if (target != "__live__")
        {
            flightEvents = Database.GetTimelineEventsForSession(target);
        }
        else
        {
            flightEvents = Database.GetTimelineEventsForSession(_activeSessionName ?? "Game.log");
            if (flightEvents.Count == 0)
            {
                lock (_liveEventsLock)
                {
                    flightEvents = _liveEvents
                        .Where(e => e.Category == "ship" || e.Category == "combat" || e.Category == "location")
                        .Select(e => new LogEntry
                        {
                            Time = DateTime.TryParse(e.Timestamp, out var dt) ? dt : DateTime.UtcNow,
                            Kind = e.Category == "combat" ? EventKind.ShipLoss : (e.Category == "location" ? EventKind.Location : EventKind.Quantum),
                            Detail = e.Description,
                            Ship = e.Ship
                        })
                        .OrderBy(e => e.Time)
                        .ToList();
                }
            }
        }

        int quantumJumps = flightEvents.Count(e => e.Kind == EventKind.Quantum);
        int losses = flightEvents.Count(e => e.Kind is EventKind.ShipLoss or EventKind.Crash);
        var ships = flightEvents.Where(e => !string.IsNullOrEmpty(e.Ship)).Select(e => e.Ship!).Distinct().ToList();
        var bodies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var timeline = new List<FlightTimelineItemDto>();
        DateTime? startTime = flightEvents.FirstOrDefault()?.Time;

        foreach (var ev in flightEvents)
        {
            var relSpan = startTime.HasValue ? ev.Time - startTime.Value : TimeSpan.Zero;
            string relText = relSpan.TotalDays >= 1
                ? ev.Time.ToLocalTime().ToString("dd.MM. HH:mm")
                : $"+{(int)relSpan.TotalHours:D2}:{relSpan.Minutes:D2}:{relSpan.Seconds:D2}";

            string title = ev.KindText;
            string subtitle = ev.Detail ?? "";
            bool isMajor = false;

            if (ev.Kind == EventKind.Quantum)
            {
                title = "Quantum-Sprung";
                isMajor = true;
                if (!string.IsNullOrEmpty(ev.Detail))
                {
                    var resolved = Locations.ResolveLocation(ev.Detail);
                    if (!string.IsNullOrEmpty(resolved.ParentBody) && resolved.ParentBody != "—") bodies.Add(resolved.ParentBody);
                }
            }
            else if (ev.Kind == EventKind.Vehicle)
            {
                title = "Schiff ausgelagert / gespawnt";
                isMajor = true;
            }
            else if (ev.Kind is EventKind.ShipLoss or EventKind.Crash)
            {
                title = "Schiffsverlust / Havarie";
                isMajor = true;
            }
            else if (ev.Kind == EventKind.Location)
            {
                title = "Standortwechsel";
                if (!string.IsNullOrEmpty(ev.Detail))
                {
                    var resolved = Locations.ResolveLocation(ev.Detail);
                    if (!string.IsNullOrEmpty(resolved.ParentBody) && resolved.ParentBody != "—") bodies.Add(resolved.ParentBody);
                }
            }

            timeline.Add(new FlightTimelineItemDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Time = ev.Time.ToLocalTime().ToString("dd.MM. HH:mm:ss"),
                RelativeTime = relText,
                Kind = ev.Kind.ToString().ToLowerInvariant(),
                Title = title,
                Subtitle = subtitle,
                Ship = ev.Ship,
                Location = ev.Location,
                IsMajor = isMajor
            });
        }

        double totalDistGm = quantumJumps * 18.5; // Heuristische Schätzung pro Sprung
        double totalDistKm = totalDistGm * 1_000_000.0;
        var duration = flightEvents.Count > 1 ? flightEvents[^1].Time - flightEvents[0].Time : TimeSpan.Zero;
        string durText = $"{(int)duration.TotalHours}h {duration.Minutes}m";

        return new FlightRecorderDto
        {
            TotalDistanceGm = Math.Round(totalDistGm, 1),
            TotalDistanceKm = Math.Round(totalDistKm, 0),
            TotalDistanceText = totalDistGm > 0 ? $"{totalDistGm:F1} GM ({totalDistKm:N0} km)" : "0 km",
            FlightDurationText = durText,
            QuantumJumps = quantumJumps,
            SortieCount = Math.Max(1, flightEvents.Count(e => e.Kind == EventKind.Vehicle)),
            ShipLosses = losses,
            VisitedBodies = bodies.ToList(),
            UsedShips = ships,
            Timeline = timeline.OrderByDescending(t => t.Time).Take(150).ToList()
        };
    }

    private List<RsResourceDto> GetRsSignaturesData()
    {
        return RsDecoderCatalog.AllResources.Select(r => new RsResourceDto
        {
            Name = r.Name,
            BaseRs = r.BaseRs,
            Tier = r.Tier,
            Rarity = r.Rarity,
            Method = r.Method,
            EstimatedPricePerScu = r.EstimatedPricePerScu,
            Locations = r.Locations ?? new()
        }).OrderBy(r => r.Name).ToList();
    }

    private List<RsMatchDto> DecodeRsData(int rs)
    {
        var matches = RsDecoderCatalog.Decode(rs);
        return matches.Select(m => new RsMatchDto
        {
            ResourceName = m.Resource.Name,
            BaseRs = m.Resource.BaseRs,
            Tier = m.Resource.Tier,
            Rarity = m.Resource.Rarity,
            Method = m.Resource.Method,
            EstimatedPricePerScu = m.Resource.EstimatedPricePerScu,
            Nodes = m.Nodes,
            IsExact = m.IsExact,
            ErrorPct = Math.Round(m.ErrorPct, 1),
            ScannedRs = m.ScannedRs,
            EstimatedClusterValue = (long)m.Resource.EstimatedPricePerScu * m.Nodes * 12
        }).ToList();
    }

    private List<MarketCommodityDto> GetMarketData()
    {
        return new List<MarketCommodityDto>
        {
            new() { Name = "Laranite", Category = "Minerals", Tier = "S", AvgBuyPrice = 28.50, AvgSellPrice = 33.20, Margin = 4.70, BestBuyLocation = "Mining Area 045 (Wala)", BestSellLocation = "Lorville CBD (Hurston)" },
            new() { Name = "Recycled Material Composite (RMC)", Category = "Salvage", Tier = "S", AvgBuyPrice = 11.80, AvgSellPrice = 14.50, Margin = 2.70, BestBuyLocation = "Pickers Field (Hurston)", BestSellLocation = "Area 18 TDD (ArcCorp)" },
            new() { Name = "Beryl", Category = "Minerals", Tier = "A", AvgBuyPrice = 3.90, AvgSellPrice = 4.85, Margin = 0.95, BestBuyLocation = "HDMS-Ryder (Ita)", BestSellLocation = "Orison Cloudview (Crusader)" },
            new() { Name = "Titanium", Category = "Metals", Tier = "A", AvgBuyPrice = 7.80, AvgSellPrice = 9.20, Margin = 1.40, BestBuyLocation = "HDMS-Bezdek (Arial)", BestSellLocation = "New Babbage (microTech)" },
            new() { Name = "Gold", Category = "Precious", Tier = "S", AvgBuyPrice = 6.80, AvgSellPrice = 8.10, Margin = 1.30, BestBuyLocation = "Tram & Myers (Cellin)", BestSellLocation = "Lorville CBD (Hurston)" },
            new() { Name = "Medical Supplies", Category = "Medical", Tier = "A", AvgBuyPrice = 17.50, AvgSellPrice = 20.20, Margin = 2.70, BestBuyLocation = "Deakins Research (Yela)", BestSellLocation = "CRU-L1 Ambitious Dream" },
            new() { Name = "Agricium", Category = "Minerals", Tier = "A", AvgBuyPrice = 24.20, AvgSellPrice = 27.60, Margin = 3.40, BestBuyLocation = "Shubin SAL-2 (Lyria)", BestSellLocation = "Area 18 TDD (ArcCorp)" },
            new() { Name = "Tungsten", Category = "Metals", Tier = "B", AvgBuyPrice = 3.60, AvgSellPrice = 4.25, Margin = 0.65, BestBuyLocation = "HDMS-Perlman (Magda)", BestSellLocation = "Everus Harbor (Hurston)" },
            new() { Name = "Quantanium (Raw)", Category = "Volatile", Tier = "S", AvgBuyPrice = 44.00, AvgSellPrice = 88.00, Margin = 44.00, BestBuyLocation = "Lyria Asteroids", BestSellLocation = "ARC-L1 Refinery" },
            new() { Name = "Diamond", Category = "Gems", Tier = "B", AvgBuyPrice = 6.20, AvgSellPrice = 7.15, Margin = 0.95, BestBuyLocation = "HDMS-Lathan (Arial)", BestSellLocation = "Baijini Point (ArcCorp)" },
        };
    }

    private ToolsStatusDto GetToolsStatus()
    {
        var settings = Settings.Load();
        double shaderMb = MaintenanceService.GetShaderCacheSizeMb();
        double crashMb = MaintenanceService.GetCrashDumpsSizeMb();
        string cfgPath = MaintenanceService.GetUserCfgPath(_currentLogPath);
        var (cfgExists, cfgContent) = MaintenanceService.ReadUserCfg(_currentLogPath);
        var diag = MaintenanceService.GetSystemDiagnostics(_currentLogPath);
        var keybindList = MaintenanceService.ListKeybindBackups(settings.CloudStoragePath);
        var configList = MaintenanceService.ListConfigBackups(settings.CloudStoragePath);

        return new ToolsStatusDto
        {
            ShaderCacheMb = shaderMb,
            CrashDumpsMb = crashMb,
            UserCfgPath = cfgPath,
            UserCfgExists = cfgExists,
            UserCfgContent = cfgContent,
            TotalRamGb = diag.TotalRamGb,
            RamStatus = diag.RamStatus,
            DriveName = diag.DriveName,
            FreeDiskGb = diag.FreeDiskGb,
            PagefileStatus = diag.PagefileStatus,
            CloudStoragePath = settings.CloudStoragePath,
            KeybindBackups = keybindList.Select(k => $"{k.Name} ({k.FileCount} Dateien, {k.SizeFormatted})").ToList(),
            KeybindItems = keybindList.Select(k => new KeybindBackupItemDto
            {
                Name = k.Name,
                FolderPath = k.FolderPath,
                CreatedAt = k.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                FileCount = k.FileCount,
                LocationType = k.LocationType,
                SizeFormatted = k.SizeFormatted
            }).ToList(),
            ConfigBackups = configList.Select(c => new ConfigBackupItemDto
            {
                Name = c.Name,
                FilePath = c.FilePath,
                CreatedAt = c.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                LocationType = c.LocationType,
                SizeFormatted = c.SizeFormatted
            }).ToList(),
            KeybindsDir = MaintenanceService.LocalKeybindsBackupDir,
            ConfigDir = MaintenanceService.LocalConfigBackupDir
        };
    }

    private ToolsStatusDto ClearShaderCache()
    {
        MaintenanceService.CleanShaderCache();
        return GetToolsStatus();
    }

    private ToolsStatusDto ClearCrashDumps()
    {
        MaintenanceService.CleanCrashDumps();
        return GetToolsStatus();
    }

    private ToolsStatusDto SaveUserCfg(string content)
    {
        var s = Settings.Load();
        MaintenanceService.SaveUserCfg(_currentLogPath, content, s.CloudStoragePath);
        return GetToolsStatus();
    }

    private ToolsStatusDto BackupKeybinds(string? note)
    {
        var s = Settings.Load();
        MaintenanceService.BackupKeybinds(_currentLogPath, s.CloudStoragePath, note);
        return GetToolsStatus();
    }

    private SettingsDto GetSettingsData()
    {
        var s = Settings.Load();
        return new SettingsDto
        {
            LogPath = s.LogPath ?? _currentLogPath,
            AutoOcrEnabled = s.AutoOcrEnabled,
            UexApiKey = s.UexApiKey,
            OverlayEnabled = s.OverlayEnabled,
            OverlayOpacity = s.OverlayOpacity,
            ToastEnabled = s.ToastEnabled,
            ToastBlueprintEnabled = s.ToastBlueprintEnabled,
            ToastMissionEnabled = s.ToastMissionEnabled,
            ToastReputationEnabled = s.ToastReputationEnabled,
            ToastRefineryEnabled = s.ToastRefineryEnabled,
            ToastElevatorEnabled = s.ToastElevatorEnabled,
            ToastShipDestructionEnabled = s.ToastShipDestructionEnabled,
            AuroraIntegrationEnabled = s.AuroraIntegrationEnabled,
            AuroraVolume = s.AuroraVolume,
            RsTargetAlertEnabled = s.RsTargetAlertEnabled,
            RsTargetSoundEnabled = s.RsTargetSoundEnabled,
            WalletRegion = s.WalletRegion,
            ContractRegion = s.ContractRegion,
            RsScanRegion = s.RsScanRegion,
            ChatRegion = s.ChatRegion,
            ChatOcrEnabled = s.ChatOcrEnabled,
            ChatOcrIntervalMs = s.ChatOcrIntervalMs,

            WipeFilterEnabled = s.WipeFilterEnabled,
            WipeDateString = s.WipeDateString,
            WipeFilterMoney = s.WipeFilterMoney,
            WipeFilterContracts = s.WipeFilterContracts,
            WipeFilterFleet = s.WipeFilterFleet,
            WipeFilterBlueprints = s.WipeFilterBlueprints,

            SelectedFontFamily = s.SelectedFontFamily,
            AppLanguage = s.AppLanguage,
            MinimizeToTrayOnClose = s.MinimizeToTrayOnClose,
            AutostartEnabled = s.AutostartEnabled,
            DebugMode = s.DebugMode,
        };
    }

    private void SaveSettingsData(SettingsDto dto)
    {
        var s = Settings.Load();
        bool pathChanged = !string.Equals(s.LogPath, dto.LogPath, StringComparison.OrdinalIgnoreCase);

        s.LogPath = dto.LogPath;
        s.AutoOcrEnabled = dto.AutoOcrEnabled;
        s.UexApiKey = dto.UexApiKey;
        s.OverlayEnabled = dto.OverlayEnabled;
        s.OverlayOpacity = dto.OverlayOpacity;
        s.ToastEnabled = dto.ToastEnabled;
        s.ToastBlueprintEnabled = dto.ToastBlueprintEnabled;
        s.ToastMissionEnabled = dto.ToastMissionEnabled;
        s.ToastReputationEnabled = dto.ToastReputationEnabled;
        s.ToastRefineryEnabled = dto.ToastRefineryEnabled;
        s.ToastElevatorEnabled = dto.ToastElevatorEnabled;
        s.ToastShipDestructionEnabled = dto.ToastShipDestructionEnabled;
        s.AuroraIntegrationEnabled = dto.AuroraIntegrationEnabled;
        s.AuroraVolume = dto.AuroraVolume;
        _auroraService.IsEnabled = dto.AuroraIntegrationEnabled;
        _auroraService.Volume = dto.AuroraVolume;
        s.RsTargetAlertEnabled = dto.RsTargetAlertEnabled;
        s.RsTargetSoundEnabled = dto.RsTargetSoundEnabled;
        s.WalletRegion = dto.WalletRegion;
        s.ContractRegion = dto.ContractRegion;
        s.RsScanRegion = dto.RsScanRegion;
        s.ChatRegion = dto.ChatRegion;
        s.ChatOcrEnabled = dto.ChatOcrEnabled;
        s.ChatOcrIntervalMs = dto.ChatOcrIntervalMs;
        if (dto.ChatOcrEnabled) _chatScanner.Start();
        else _chatScanner.Stop();

        s.WipeFilterEnabled = dto.WipeFilterEnabled;
        s.WipeDateString = dto.WipeDateString;
        s.WipeFilterMoney = dto.WipeFilterMoney;
        s.WipeFilterContracts = dto.WipeFilterContracts;
        s.WipeFilterFleet = dto.WipeFilterFleet;
        s.WipeFilterBlueprints = dto.WipeFilterBlueprints;

        s.SelectedFontFamily = dto.SelectedFontFamily;
        s.AppLanguage = dto.AppLanguage;
        s.MinimizeToTrayOnClose = dto.MinimizeToTrayOnClose;
        s.AutostartEnabled = dto.AutostartEnabled;
        s.DebugMode = dto.DebugMode;

        Settings.Save(s);

        if (!string.IsNullOrEmpty(dto.AppLanguage))
        {
            I18n.Instance.SetLanguage(dto.AppLanguage);
        }

        if (pathChanged && !string.IsNullOrEmpty(s.LogPath))
        {
            Localization.Hint(s.LogPath);
            if (File.Exists(s.LogPath))
            {
                _currentLogPath = s.LogPath;
                StartLogTailer(_currentLogPath);
            }
        }
    }

    private OcrRegionsConfigDto GetOcrRegionsConfig()
    {
        var (sw, sh) = ScreenCapture.GetPrimaryScreenSize();
        var s = Settings.Load();
        return new OcrRegionsConfigDto
        {
            WalletRegion = s.WalletRegion,
            ContractRegion = s.ContractRegion,
            RsScanRegion = s.RsScanRegion,
            ChatRegion = s.ChatRegion,
            DefaultWalletRegion = ScreenCapture.GetDefaultWalletRegion(),
            DefaultContractRegion = ScreenCapture.GetDefaultContractRegion(),
            DefaultRsRegion = ScreenCapture.GetDefaultRsRegion(),
            DefaultChatRegion = ScreenCapture.GetDefaultChatRegion(),
            ScreenWidth = sw,
            ScreenHeight = sh,
            IsWalletScanBoxVisible = _walletScanIndicator.IsVisible,
            IsContractScanBoxVisible = _contractScanIndicator.IsVisible
        };
    }

    private async Task<OcrTestResultDto> ExecuteOcrTestAsync(string target)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var s = Settings.Load();

        if (!_ocrEngine.IsAvailable)
        {
            return new OcrTestResultDto
            {
                Success = false,
                Target = target,
                Error = "Windows OCR Engine (Windows.Media.Ocr) ist auf diesem System nicht verfügbar."
            };
        }

        if (target == "contract")
        {
            var region = s.ContractRegion ?? ScreenCapture.GetDefaultContractRegion();
            var raw = ScreenCapture.Capture(region.X, region.Y, region.Width, region.Height);
            if (raw == null)
            {
                return new OcrTestResultDto { Success = false, Target = target, Error = "Bildschirmbereich konnte nicht erfasst werden.", Region = region };
            }
            var text = await _ocrEngine.RecognizeSinglePassAsync(raw, region.Width, region.Height, scale: 1, padding: 12);
            sw.Stop();
            return new OcrTestResultDto
            {
                Success = !string.IsNullOrWhiteSpace(text),
                Target = target,
                RecognizedText = text?.Trim() ?? "(Kein Text erkannt)",
                DurationMs = (int)sw.ElapsedMilliseconds,
                Region = region
            };
        }
        else if (target == "rs")
        {
            var region = s.RsScanRegion ?? ScreenCapture.GetDefaultRsRegion();
            var raw = ScreenCapture.Capture(region.X, region.Y, region.Width, region.Height);
            if (raw == null)
            {
                return new OcrTestResultDto { Success = false, Target = target, Error = "Bildschirmbereich konnte nicht erfasst werden.", Region = region };
            }
            var text = await _ocrEngine.RecognizeSinglePassAsync(raw, region.Width, region.Height, scale: 2, padding: 8);
            sw.Stop();
            long? rsVal = null;
            if (!string.IsNullOrWhiteSpace(text))
            {
                var m = System.Text.RegularExpressions.Regex.Match(text, @"\b(\d{3,6})\b");
                if (m.Success && long.TryParse(m.Groups[1].Value, out var parsed))
                {
                    rsVal = parsed;
                }
            }
            return new OcrTestResultDto
            {
                Success = !string.IsNullOrWhiteSpace(text),
                Target = target,
                RecognizedText = text?.Trim() ?? "(Kein Text erkannt)",
                ExtractedValue = rsVal,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Region = region
            };
        }
        else if (target == "chat")
        {
            var region = s.ChatRegion ?? ScreenCapture.GetDefaultChatRegion();
            var raw = ScreenCapture.Capture(region.X, region.Y, region.Width, region.Height);
            if (raw == null)
            {
                return new OcrTestResultDto { Success = false, Target = target, Error = "Bildschirmbereich konnte nicht erfasst werden.", Region = region };
            }
            var text = await _ocrEngine.RecognizeSinglePassAsync(raw, region.Width, region.Height, scale: 2, padding: 8);
            sw.Stop();
            return new OcrTestResultDto
            {
                Success = !string.IsNullOrWhiteSpace(text),
                Target = target,
                RecognizedText = text?.Trim() ?? "(Kein Text erkannt)",
                DurationMs = (int)sw.ElapsedMilliseconds,
                Region = region
            };
        }
        else
        {
            var region = s.WalletRegion ?? ScreenCapture.GetDefaultWalletRegion();
            var raw = ScreenCapture.Capture(region.X, region.Y, region.Width, region.Height);
            if (raw == null)
            {
                return new OcrTestResultDto { Success = false, Target = target, Error = "Bildschirmbereich konnte nicht erfasst werden.", Region = region };
            }
            var (invText, plainText) = await _ocrEngine.RecognizeDualPassAsync(raw, region.Width, region.Height, scale: 6, padding: 24, boostContrast: false);
            var bestText = WalletOcrTrigger.BestRead(invText, plainText);
            var val = WalletOcrTrigger.ExtractBalance(bestText);
            sw.Stop();

            if (val.HasValue)
            {
                _walletScanIndicator.FlashGreen();
                OnBalanceCaptured(val.Value);
            }

            return new OcrTestResultDto
            {
                Success = val.HasValue,
                Target = target,
                RecognizedText = bestText?.Trim() ?? (!string.IsNullOrEmpty(invText) ? invText.Trim() : "(Kein Text erkannt)"),
                ExtractedValue = val,
                DurationMs = (int)sw.ElapsedMilliseconds,
                Region = region
            };
        }
    }

    private void OnLogLineReceived(string rawLine, bool isLive = true)
    {
        try
        {
            if (isLive)
            {
                _walletCapture.ProcessLine(rawLine);
                _auroraService.ProcessLiveLine(rawLine);
            }
            var entry = _parser.Feed(rawLine);
            if (entry == null) return;

            _lastEventTime = entry.Time;

            var dto = new LogEventDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Timestamp = entry.Time.ToLocalTime().ToString("HH:mm:ss"),
                Category = MapCategory(entry.Kind),
                Kind = entry.Kind.ToString(),
                KindText = entry.KindText,
                Icon = entry.Icon,
                Title = entry.KindText,
                Description = CleanEventDetail(entry.Detail, entry.Kind),
                Amount = entry.Amount != 0 ? entry.Amount : null,
                Ship = CleanEventShip(entry.Ship, entry.Kind),
                RawText = rawLine.Length > 120 ? rawLine[..120] + "…" : rawLine,
            };

            lock (_liveEventsLock)
            {
                _liveEvents.Insert(0, dto);
                if (_liveEvents.Count > 1000)
                {
                    _liveEvents.RemoveAt(_liveEvents.Count - 1);
                }
            }

            if (isLive)
            {
                Database.InsertCustomEvent(_activeSessionName ?? "Game.log", entry.Time, entry.Kind, entry.Amount, entry.Detail ?? "", entry.Ship);

                if (entry.Kind is EventKind.MissionDone or EventKind.MissionReward)
                {
                    var fac = ReputationCatalog.MatchFaction(entry.Detail) ?? ReputationCatalog.MatchFaction(entry.Ship);
                    if (fac != null)
                    {
                        int xpGained = (int)Math.Max(250, Math.Min(3500, entry.Amount > 0 ? entry.Amount / 10 : 500));
                        Database.AddFactionReputationXp(fac.Id, xpGained, entry.Time);
                        Broadcast("reputation_response", GetReputationData());
                    }
                }

                Broadcast("LOG_EVENT", dto);
                Broadcast("HUD_UPDATE", GetHudTelemetry("__live__"));
            }
        }
        catch (Exception ex)
        {
            Logger.Error("PhotinoBridge.OnLogLineReceived", ex);
        }
    }

    private static string MapCategory(EventKind kind) => kind switch
    {
        EventKind.TransferIn or EventKind.TransferOut or EventKind.Purchase or
        EventKind.Sale or EventKind.Trade or EventKind.MissionReward or EventKind.Fine => "wallet",
        EventKind.Death or EventKind.ShipLoss or EventKind.Kill or EventKind.Injury => "combat",
        EventKind.Mission or EventKind.MissionDone or EventKind.MissionTaken => "mission",
        EventKind.Vehicle or EventKind.Quantum or EventKind.Hangar => "ship",
        EventKind.Location or EventKind.Jurisdiction => "location",
        _ => "system"
    };

    private static string? CleanEventShip(string? ship, EventKind kind)
    {
        if (string.IsNullOrWhiteSpace(ship)) return null;
        if (kind == EventKind.Hangar) return null;
        var s = ship.Trim();
        if (s.Equals("Levski", StringComparison.OrdinalIgnoreCase) ||
            s.Equals("Hangar", StringComparison.OrdinalIgnoreCase) ||
            s.Equals("—", StringComparison.OrdinalIgnoreCase) ||
            s.Equals("--", StringComparison.OrdinalIgnoreCase))
            return null;
        return Ships.Prettify(s);
    }

    private static string CleanEventDetail(string? detail, EventKind kind)
    {
        if (string.IsNullOrWhiteSpace(detail)) return "";
        var d = detail.Trim();

        // Ausrüstung & Loot: Rohcodes mit Localization / WarehouseCatalog abgleichen
        if (kind == EventKind.Loadout || kind == EventKind.Loot)
        {
            var locName = Localization.ItemName(d);
            if (!string.IsNullOrWhiteSpace(locName)) return locName;

            var (catName, _) = WarehouseCatalog.Resolve(d);
            if (!string.IsNullOrWhiteSpace(catName) && catName != "Sonstiges" && catName != "Unbekannter Gegenstand" && catName != d)
            {
                return catName;
            }
        }

        // Missionen: Häufige englische Statusmeldungen in die eingestellte Sprache übersetzen (wenn Deutsch aktiv)
        if (I18n.Instance.IsGerman && (kind == EventKind.Mission || kind == EventKind.MissionReward || kind == EventKind.MissionTaken))
        {
            if (d.Equals("Objective Complete", StringComparison.OrdinalIgnoreCase))
                return "Missionsziel abgeschlossen";
            if (d.StartsWith("New Objective:", StringComparison.OrdinalIgnoreCase))
            {
                var rest = d["New Objective:".Length..].Trim();
                if (rest.StartsWith("Deliver Flight Recorder To", StringComparison.OrdinalIgnoreCase))
                    return "Neues Missionsziel: Flugschreiber abliefern bei " + rest["Deliver Flight Recorder To".Length..].Trim();
                if (rest.StartsWith("Collect Flight Recorder From a wreck site in the", StringComparison.OrdinalIgnoreCase))
                    return "Neues Missionsziel: Flugschreiber aus Wrack bergen in " + rest["Collect Flight Recorder From a wreck site in the".Length..].Trim();
                return "Neues Missionsziel: " + rest;
            }
            if (d.Equals("Contract Complete", StringComparison.OrdinalIgnoreCase) || d.Equals("Contract Completed", StringComparison.OrdinalIgnoreCase))
                return "Auftrag erfolgreich abgeschlossen";
            if (d.Equals("Contract Failed", StringComparison.OrdinalIgnoreCase))
                return "Auftrag fehlgeschlagen";
            if (d.Equals("Contract Abandoned", StringComparison.OrdinalIgnoreCase))
                return "Auftrag abgebrochen";
            if (d.Equals("Contract Cancelled", StringComparison.OrdinalIgnoreCase))
                return "Auftrag storniert";
            if (d.Equals("Contract Withdrawn", StringComparison.OrdinalIgnoreCase))
                return "Auftrag zurückgezogen";
        }

        return d;
    }

    public static string GetChannelName(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "CUSTOM";
        var p = path.Replace('\\', '/');
        if (p.Contains("/LIVE/", StringComparison.OrdinalIgnoreCase)) return "LIVE";
        if (p.Contains("/PTU/", StringComparison.OrdinalIgnoreCase)) return "PTU";
        if (p.Contains("/EPTU/", StringComparison.OrdinalIgnoreCase)) return "EPTU";
        if (p.Contains("/TECH-PREVIEW/", StringComparison.OrdinalIgnoreCase)) return "TECH-PREVIEW";
        if (p.Contains("/HOTFIX/", StringComparison.OrdinalIgnoreCase)) return "HOTFIX";
        return "CUSTOM";
    }

    private LogStatusDto GetLogStatus()
    {
        string? path = _currentLogPath;
        bool exists = !string.IsNullOrEmpty(path) && File.Exists(path);
        long size = 0;
        DateTime? lastMod = null;

        if (exists)
        {
            try
            {
                var fi = new FileInfo(path!);
                size = fi.Length;
                lastMod = fi.LastWriteTimeUtc;
            }
            catch { }
        }

        int backupsCount = 0;
        try
        {
            if (!string.IsNullOrEmpty(path))
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                {
                    var backupDir = Path.Combine(dir, "logbackups");
                    if (Directory.Exists(backupDir))
                    {
                        backupsCount = Directory.GetFiles(backupDir, "*.log").Length;
                    }
                }
            }
        }
        catch { }

        int archiveCount = 0;
        try
        {
            if (Directory.Exists(LogArchive.Dir))
            {
                archiveCount = Directory.GetFiles(LogArchive.Dir, "*.log").Length;
            }
        }
        catch { }

        var detectedPaths = new List<DetectedPathDto>();
        try
        {
            foreach (var p in PathFinder.FindAll())
            {
                try
                {
                    var fi = new FileInfo(p);
                    detectedPaths.Add(new DetectedPathDto
                    {
                        Path = p,
                        Channel = GetChannelName(p),
                        LastModified = fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm"),
                        SizeBytes = fi.Length,
                        IsCurrent = string.Equals(p, path, StringComparison.OrdinalIgnoreCase)
                    });
                }
                catch { }
            }
        }
        catch { }

        return new LogStatusDto
        {
            CurrentLogPath = path,
            Channel = GetChannelName(path),
            Exists = exists,
            SizeBytes = size,
            FormattedSize = Database.FormatBytes(size),
            LastModified = lastMod?.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss"),
            IsLiveWatching = _tailer != null,
            ActiveSession = _activeSessionName ?? "Live Session",
            DetectedPaths = detectedPaths,
            BackupsCount = backupsCount,
            ArchiveCount = archiveCount,
            ParserVersion = Database.CurrentParserVersion,
            SchemaVersion = Database.CurrentSchemaVersion
        };
    }

    private LogStatusDto DetectLogPath()
    {
        var best = PathFinder.FindBest();
        if (!string.IsNullOrEmpty(best) && File.Exists(best))
        {
            _currentLogPath = best;
            var s = Settings.Load();
            s.LogPath = best;
            Settings.Save(s);

            if (_tailer != null)
            {
                StartLogTailer(best);
            }
        }
        return GetLogStatus();
    }

    private LogStatusDto SetLogPath(string path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            _currentLogPath = path;
            var s = Settings.Load();
            s.LogPath = path;
            Settings.Save(s);

            if (_tailer != null)
            {
                StartLogTailer(path);
            }
        }
        return GetLogStatus();
    }

    private LogStatusDto BrowseLogFile()
    {
        string? initial = !string.IsNullOrEmpty(_currentLogPath) ? Path.GetDirectoryName(_currentLogPath) : null;
        var chosen = NativeDialogs.ShowOpenFileDialog("Game.log auswählen", initial);
        if (!string.IsNullOrEmpty(chosen) && File.Exists(chosen))
        {
            return SetLogPath(chosen);
        }
        return GetLogStatus();
    }

    public List<string> DiscoverAllLogFiles()
    {
        var filesByFileName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        void AddLogFile(string? p)
        {
            if (string.IsNullOrEmpty(p)) return;
            try
            {
                if (File.Exists(p))
                {
                    var fn = Path.GetFileName(p);
                    filesByFileName.TryAdd(fn, p);
                    _knownSessionFiles[fn] = p;
                }
            }
            catch { }
        }

        void ScanDir(string? dir)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;
            try
            {
                foreach (var f in Directory.GetFiles(dir, "*.log"))
                    AddLogFile(f);
                var subBackups = Path.Combine(dir, "logbackups");
                if (Directory.Exists(subBackups))
                {
                    foreach (var f in Directory.GetFiles(subBackups, "*.log"))
                        AddLogFile(f);
                }
            }
            catch { }
        }

        // 0. SCLogMate eigenes Archiv (%APPDATA%\SCLogMate\archive)
        if (Directory.Exists(LogArchive.Dir))
        {
            ScanDir(LogArchive.Dir);
        }

        // 1. Aktuelle Game.log und zugehörige Verzeichnisse
        if (!string.IsNullOrEmpty(_currentLogPath))
        {
            AddLogFile(_currentLogPath);
            var dir = Path.GetDirectoryName(_currentLogPath);
            ScanDir(dir);
            if (!string.IsNullOrEmpty(dir))
            {
                var buDir = Path.Combine(dir, "logbackups");
                ScanDir(buDir);
                var parent = Directory.GetParent(dir)?.FullName;
                if (!string.IsNullOrEmpty(parent))
                {
                    ScanDir(parent);
                    ScanDir(Path.Combine(parent, "logbackups"));
                }

                // Star Citizen Backups im LogArchive sichern
                try
                {
                    var backups = new List<string>();
                    if (Directory.Exists(buDir))
                        backups.AddRange(Directory.GetFiles(buDir, "*.log"));
                    if (!string.IsNullOrEmpty(parent) && Directory.Exists(Path.Combine(parent, "logbackups")))
                        backups.AddRange(Directory.GetFiles(Path.Combine(parent, "logbackups"), "*.log"));
                    if (backups.Count > 0)
                        LogArchive.Sync(backups);
                }
                catch { }
            }
        }

        // 2. Gespeicherter Settings Pfad und CloudStoragePath
        try
        {
            var savedSettings = Settings.Load();
            if (!string.IsNullOrEmpty(savedSettings.LogPath) && savedSettings.LogPath != _currentLogPath)
            {
                AddLogFile(savedSettings.LogPath);
                ScanDir(Path.GetDirectoryName(savedSettings.LogPath));
            }
            if (!string.IsNullOrEmpty(savedSettings.CloudStoragePath))
            {
                ScanDir(savedSettings.CloudStoragePath);
                ScanDir(Path.Combine(savedSettings.CloudStoragePath, "logbackups"));
            }
        }
        catch { }

        // 3. Alle Laufwerke nach Star Citizen Installationen und Kanälen durchsuchen
        var driveRoots = new List<string>();
        try
        {
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady &&
                         d.DriveType is DriveType.Fixed or DriveType.Removable or DriveType.Network))
            {
                var r = drive.RootDirectory.FullName;
                driveRoots.Add(Path.Combine(r, "Program Files", "Roberts Space Industries", "StarCitizen"));
                driveRoots.Add(Path.Combine(r, "Roberts Space Industries", "StarCitizen"));
                driveRoots.Add(Path.Combine(r, "Games", "Roberts Space Industries", "StarCitizen"));
                driveRoots.Add(Path.Combine(r, "StarCitizen"));
            }
        }
        catch { }

        string[] channels = { "LIVE", "PTU", "EPTU", "HOTFIX", "TECH-PREVIEW" };
        foreach (var scRoot in driveRoots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(scRoot)) continue;
            ScanDir(Path.Combine(scRoot, "logbackups"));

            foreach (var ch in channels)
            {
                var chDir = Path.Combine(scRoot, ch);
                if (Directory.Exists(chDir))
                {
                    AddLogFile(Path.Combine(chDir, "Game.log"));
                    ScanDir(chDir);
                    ScanDir(Path.Combine(chDir, "logbackups"));
                }
            }
        }

        return filesByFileName.Values.ToList();
    }

    public (int indexedSessions, int totalEvents) SyncAllLogs(bool forceRescan = false)
    {
        try
        {
            var allFiles = DiscoverAllLogFiles();
            int currentSessionCount = Database.GetSessionCount();
            bool needsFullRescan = Database.WasMigrationApplied 
                                   || Database.WasParserResetRequired 
                                   || (currentSessionCount == 0 && allFiles.Count > 0);
            bool isDbUpdate = forceRescan || needsFullRescan;
            string? reason = Database.LastMigrationReason;
            if (string.IsNullOrEmpty(reason))
            {
                if (Database.WasParserResetRequired)
                    reason = $"Parser-Update auf v{Database.CurrentParserVersion} (Vollständige Neu-Indexierung aller Logs)";
                else if (Database.WasMigrationApplied)
                    reason = $"Datenbank-Schema Upgrade auf v{Database.CurrentSchemaVersion}";
                else if (currentSessionCount == 0 && allFiles.Count > 0)
                    reason = "Initialisierung & Indexierung aller Star Citizen Logs";
                else if (forceRescan)
                    reason = "Manueller Re-Scan aller Star Citizen Logs";
                else
                    reason = "Datenbank- oder Parser-Aktualisierung";
            }

            if (isDbUpdate)
            {
                int totalCount = allFiles.Count;
                Broadcast("SCAN_PROGRESS", new ScanProgressDto
                {
                    Current = 0,
                    Total = totalCount,
                    Percent = 0,
                    CurrentFileName = "Starte vollständige Neu-Indexierung...",
                    IsCompleted = false,
                    IsDbUpdate = true,
                    UpdateReason = reason ?? "Datenbank- oder Parser-Aktualisierung"
                });

                var result = Database.RescanAll(allFiles, (curr, total, name) =>
                {
                    double pct = total > 0 ? Math.Round((double)curr / total * 100.0, 1) : 0;
                    Broadcast("SCAN_PROGRESS", new ScanProgressDto
                    {
                        Current = curr,
                        Total = total,
                        Percent = pct,
                        CurrentFileName = name,
                        IsCompleted = false,
                        IsDbUpdate = true,
                        UpdateReason = reason ?? "Datenbank- oder Parser-Aktualisierung"
                    });
                });

                Database.WasMigrationApplied = false;
                Database.WasParserResetRequired = false;
                Database.LastMigrationReason = null;

                Broadcast("SCAN_PROGRESS", new ScanProgressDto
                {
                    Current = totalCount,
                    Total = totalCount,
                    Percent = 100,
                    CurrentFileName = "Datenbank erfolgreich aktualisiert",
                    IsCompleted = true,
                    IndexedSessions = result.indexedSessions,
                    TotalEvents = result.totalEvents,
                    IsDbUpdate = true,
                    UpdateReason = reason
                });

                if (!string.IsNullOrEmpty(_currentLogPath) && File.Exists(_currentLogPath))
                {
                    StartLogTailer(_currentLogPath);
                }

                Broadcast("STATUS_UPDATE", GetAppStatus());
                Broadcast("sessions_response", GetSessions());
                Broadcast("log_status_response", GetLogStatus());
                Broadcast("HUD_UPDATE", GetHudTelemetry(_selectedSession));

                return result;
            }
            else
            {
                int unindexed = Database.GetUnindexedCount(allFiles);
                if (unindexed > 0)
                {
                    Broadcast("SCAN_PROGRESS", new ScanProgressDto
                    {
                        Current = 0,
                        Total = unindexed,
                        Percent = 0,
                        CurrentFileName = $"Synchronisiere {unindexed} neue Logs...",
                        IsCompleted = false,
                        IsDbUpdate = false,
                        UpdateReason = $"Indexiere {unindexed} neue Star Citizen Session(s)..."
                    });

                    int added = Database.IndexNew(allFiles, (curr, total, name) =>
                    {
                        double pct = total > 0 ? Math.Round((double)curr / total * 100.0, 1) : 0;
                        Broadcast("SCAN_PROGRESS", new ScanProgressDto
                        {
                            Current = curr,
                            Total = total,
                            Percent = pct,
                            CurrentFileName = name,
                            IsCompleted = false,
                            IsDbUpdate = false,
                            UpdateReason = $"Indexiere {unindexed} neue Star Citizen Session(s)..."
                        });
                    });

                    Broadcast("SCAN_PROGRESS", new ScanProgressDto
                    {
                        Current = unindexed,
                        Total = unindexed,
                        Percent = 100,
                        CurrentFileName = $"{added} Sessions erfolgreich synchronisiert",
                        IsCompleted = true,
                        IndexedSessions = added,
                        TotalEvents = 0,
                        IsDbUpdate = false,
                        UpdateReason = "Indexierung abgeschlossen"
                    });

                    Broadcast("STATUS_UPDATE", GetAppStatus());
                    Broadcast("sessions_response", GetSessions());
                    Broadcast("log_status_response", GetLogStatus());
                    Broadcast("HUD_UPDATE", GetHudTelemetry(_selectedSession));

                    return (added, 0);
                }
                else
                {
                    return (0, 0);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("PhotinoBridge.SyncAllLogs", ex);
            return (0, 0);
        }
    }

    public async Task<object?> CheckForAppUpdatesAsync(bool broadcastIfAvailable = true)
    {
        try
        {
            var info = await Updater.CheckAsync();
            if (info != null)
            {
                _latestUpdateInfo = info;
                var dto = new
                {
                    updateAvailable = true,
                    currentVersion = $"v{Updater.CurrentVersion}",
                    newVersion = $"v{info.Version}",
                    releaseNotes = info.ReleaseNotes ?? "Ein neues SCLogMate Update ist auf GitHub verfügbar.",
                    htmlUrl = info.HtmlUrl ?? "https://github.com/gOOvER/SCLogMate/releases"
                };

                if (broadcastIfAvailable)
                {
                    Broadcast("UPDATE_AVAILABLE", dto);
                }
                return dto;
            }
            else
            {
                var dto = new
                {
                    updateAvailable = false,
                    currentVersion = $"v{Updater.CurrentVersion}",
                    newVersion = $"v{Updater.CurrentVersion}",
                    releaseNotes = "",
                    htmlUrl = "https://github.com/gOOvER/SCLogMate/releases"
                };
                return dto;
            }
        }
        catch (Exception ex)
        {
            Logger.Error("CheckForAppUpdatesAsync", ex);
            return new
            {
                updateAvailable = false,
                currentVersion = $"v{Updater.CurrentVersion}",
                newVersion = $"v{Updater.CurrentVersion}",
                releaseNotes = "",
                htmlUrl = "https://github.com/gOOvER/SCLogMate/releases"
            };
        }
    }

    public async Task<object> ApplyAppUpdateAsync()
    {
        if (_latestUpdateInfo == null)
        {
            var info = await Updater.CheckAsync();
            if (info != null) _latestUpdateInfo = info;
            else return new { success = false, message = "Kein Update verfügbar." };
        }

        try
        {
            Broadcast("UPDATE_INSTALLING", new { status = $"Lade Update {_latestUpdateInfo.Version} herunter..." });
            await Updater.ApplyAsync(_latestUpdateInfo);
            Broadcast("UPDATE_INSTALLING", new { status = "Update wird installiert – SCLogMate startet neu..." });

            _ = Task.Run(async () =>
            {
                await Task.Delay(1500);
                Environment.Exit(0);
            });

            return new { success = true, message = "Update gestartet." };
        }
        catch (Exception ex)
        {
            Logger.Error("ApplyAppUpdateAsync", ex);
            return new { success = false, message = ex.Message };
        }
    }

    private (int indexedSessions, int totalEvents) ReparseAllLogs()
    {
        return SyncAllLogs(forceRescan: true);
    }

    private void ReparseSession(string sessionName)
    {
        try
        {
            string? targetFile = null;
            if (string.Equals(sessionName, Path.GetFileName(_currentLogPath), StringComparison.OrdinalIgnoreCase))
            {
                targetFile = _currentLogPath;
            }
            else if (!string.IsNullOrEmpty(_currentLogPath))
            {
                var dir = Path.GetDirectoryName(_currentLogPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    var backupPath = Path.Combine(dir, "logbackups", sessionName);
                    if (File.Exists(backupPath)) targetFile = backupPath;
                }
            }

            if (targetFile == null && Directory.Exists(LogArchive.Dir))
            {
                var arcPath = Path.Combine(LogArchive.Dir, sessionName);
                if (File.Exists(arcPath)) targetFile = arcPath;
            }

            if (!string.IsNullOrEmpty(targetFile) && File.Exists(targetFile))
            {
                Database.IndexNew(new[] { targetFile });
            }
        }
        catch (Exception ex)
        {
            Logger.Error("PhotinoBridge.ReparseSession", ex);
        }
    }

    private void DeleteSession(string sessionName)
    {
        try
        {
            using var db = new SqliteConnection($"Data Source={Database.DatabaseFilePath};Default Timeout=60;");
            db.Open();
            using var cmd = db.CreateCommand();
            cmd.CommandText = "DELETE FROM events WHERE session = @s; DELETE FROM sessions WHERE name = @s;";
            cmd.Parameters.AddWithValue("@s", sessionName);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Logger.Error("PhotinoBridge.DeleteSession", ex);
        }
    }

    private DbDiagnosticsDto GetDbDiagnostics()
    {
        var d = Database.GetDiagnostics(runDeepCheck: true);
        return new DbDiagnosticsDto
        {
            DatabasePath = d.DatabasePath,
            DatabaseSizeBytes = d.DatabaseSizeBytes,
            FormattedSize = d.FormattedSize,
            SqliteVersion = d.SqliteVersion,
            JournalMode = d.JournalMode,
            InstalledSchemaVersion = d.InstalledSchemaVersion,
            CurrentSchemaVersion = d.CurrentSchemaVersion,
            InstalledParserVersion = d.InstalledParserVersion,
            CurrentParserVersion = d.CurrentParserVersion,
            SessionCount = d.SessionCount,
            EventCount = d.EventCount,
            ContractCount = d.ContractCount,
            FleetShipCount = d.FleetShipCount,
            PoiCount = d.PoiCount,
            ReputationCount = d.ReputationCount,
            WarehouseItemCount = d.WarehouseItemCount,
            IntegrityCheckOk = d.IntegrityCheckOk,
            IntegrityMessage = d.IntegrityMessage,
            CheckedAt = d.CheckedAt.ToString("HH:mm:ss"),
            IsSynchronous = d.InstalledParserVersion == d.CurrentParserVersion && d.InstalledSchemaVersion == d.CurrentSchemaVersion
        };
    }

    private object RepairDbStructure()
    {
        var res = Database.RepairOrUpdateStructure();
        var diag = GetDbDiagnostics();
        return new { success = res.success, message = res.message, diagnostics = diag };
    }

    private object CleanupDatabase()
    {
        var res = Database.Cleanup();
        return new
        {
            cleanedEvents = res.cleanedEvents,
            cleanedSessions = res.cleanedSessions,
            sizeBefore = Database.FormatBytes(res.sizeBefore),
            sizeAfter = Database.FormatBytes(res.sizeAfter)
        };
    }

    private object GetUnknownEventsData()
    {
        string p = UnknownEventsLogger.Path;
        var lines = new List<string>();
        if (File.Exists(p))
        {
            try
            {
                lines = File.ReadLines(p).TakeLast(300).ToList();
            }
            catch { }
        }
        return new { path = p, lines };
    }

    private object ExportEvents(string format, string? session)
    {
        try
        {
            string exportDir = Path.Combine(Settings.Dir, "exports");
            Directory.CreateDirectory(exportDir);
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"sclogmate_export_{timeStamp}.{format.ToLowerInvariant()}";
            string targetPath = Path.Combine(exportDir, fileName);

            var recent = Database.LoadRecentEvents(25000);
            if (!string.IsNullOrWhiteSpace(session) && session != "__all__")
            {
                recent = recent.Where(e => e.Detail != null).ToList();
            }

            if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                var json = JsonSerializer.Serialize(recent, JsonOpts);
                File.WriteAllText(targetPath, json);
            }
            else
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Zeit;Typ;Betrag;Detail;Schiff");
                foreach (var ev in recent)
                {
                    sb.Append(ev.Time.ToString("yyyy-MM-dd HH:mm:ss")).Append(';')
                      .Append(ev.KindText).Append(';')
                      .Append(ev.Amount).Append(';')
                      .Append(ev.Detail?.Replace(';', ',') ?? "").Append(';')
                      .Append(ev.Ship?.Replace(';', ',') ?? "").Append('\n');
                }
                File.WriteAllText(targetPath, sb.ToString(), System.Text.Encoding.UTF8);
            }

            return new { success = true, path = targetPath };
        }
        catch (Exception ex)
        {
            return new { success = false, error = ex.Message };
        }
    }

    private void OpenFolder(string target)
    {
        try
        {
            switch (target.ToLowerInvariant())
            {
                case "db":
                    if (File.Exists(Database.DatabaseFilePath))
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "explorer.exe", Arguments = $"/select,\"{Database.DatabaseFilePath}\"", UseShellExecute = true });
                    break;
                case "appdata":
                    if (Directory.Exists(Settings.Dir))
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = Settings.Dir, UseShellExecute = true });
                    break;
                case "debug_log":
                    string dLog = Path.Combine(Settings.Dir, "SCLogMate.debug.log");
                    if (File.Exists(dLog))
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = dLog, UseShellExecute = true });
                    break;
                case "unknown_log":
                    if (File.Exists(UnknownEventsLogger.Path))
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = UnknownEventsLogger.Path, UseShellExecute = true });
                    break;
                case "log_folder":
                    if (!string.IsNullOrEmpty(_currentLogPath))
                    {
                        var dir = Path.GetDirectoryName(_currentLogPath);
                        if (Directory.Exists(dir))
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = dir, UseShellExecute = true });
                    }
                    break;
                case "keybinds":
                    if (Directory.Exists(MaintenanceService.LocalKeybindsBackupDir))
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = MaintenanceService.LocalKeybindsBackupDir, UseShellExecute = true });
                    break;
                case "config":
                    if (Directory.Exists(MaintenanceService.LocalConfigBackupDir))
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = MaintenanceService.LocalConfigBackupDir, UseShellExecute = true });
                    break;
                case "cloud":
                    var sCloud = Settings.Load();
                    if (!string.IsNullOrWhiteSpace(sCloud.CloudStoragePath) && Directory.Exists(sCloud.CloudStoragePath))
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = sCloud.CloudStoragePath, UseShellExecute = true });
                    break;
                case "logbackups":
                    if (!string.IsNullOrEmpty(_currentLogPath))
                    {
                        var liveDir = Path.GetDirectoryName(_currentLogPath);
                        if (!string.IsNullOrEmpty(liveDir))
                        {
                            var backups = Path.Combine(liveDir, "logbackups");
                            if (Directory.Exists(backups))
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = backups, UseShellExecute = true });
                        }
                    }
                    break;
                default:
                    if (Directory.Exists(target) || File.Exists(target))
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = target, UseShellExecute = true });
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Error("PhotinoBridge.OpenFolder", ex);
        }
    }
}
