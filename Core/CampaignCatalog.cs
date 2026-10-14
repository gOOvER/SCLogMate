using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SCLogMate.Models;

namespace SCLogMate.Core;

public sealed class CampaignTierDto
{
    public int TierLevel { get; set; }
    public int MinPoints { get; set; }
    public string Badge { get; set; } = "";
    public string Reward { get; set; } = "";
    public bool Reached { get; set; }
}

public sealed class CampaignSuggestionDto
{
    public string Contract { get; set; } = "";
    public string Title { get; set; } = "";
    public string Issuer { get; set; } = "";
    public int Points { get; set; }
    public int Needed { get; set; }
    public int Done { get; set; }
}

public sealed class CampaignTrackDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Overall { get; set; }
    public string Color { get; set; } = "#38BDF8";
    public int Points { get; set; }
    public int CurrentTier { get; set; }
    public int? NextTierPoints { get; set; }
    public int? PointsToNext { get; set; }
    public double ProgressPercent { get; set; }
    public List<CampaignTierDto> Tiers { get; set; } = new();
    public List<CampaignSuggestionDto> FastestSuggestions { get; set; } = new();
}

public sealed class CampaignContractStatusDto
{
    public string ContractId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Issuer { get; set; } = "";
    public int Points { get; set; }
    public string TrackId { get; set; } = "";
    public string TrackName { get; set; } = "";
    public int CompletedCount { get; set; }
    public int OpenCount { get; set; }
    public string? LastCompleted { get; set; }
}

public sealed class CampaignStatusDto
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Tagline { get; set; } = "";
    public string Icon { get; set; } = "🏆";
    public bool IsActive { get; set; }
    public int TotalCompleted { get; set; }
    public int TotalPoints { get; set; }
    public List<CampaignTrackDto> Tracks { get; set; } = new();
    public List<CampaignContractStatusDto> Contracts { get; set; } = new();
}

/// <summary>
/// Verwaltet und aggregiert In-Game Kampagnen & Events (ScenarioProgress)
/// wie den RSI Discovery Month, Orison Relief, Alliance Aid und Return of XenoThreat.
/// </summary>
public static class CampaignCatalog
{
    private sealed record ContractMeta(string Id, string Title, string Issuer, int Points, string TrackId, int Variants = 0);
    private sealed record TrackMeta(string Id, string Name, bool Overall, string Color, (int MinPoints, string Badge, string Reward)[] Tiers);
    private sealed record CampaignMeta(string Id, string Title, string Description, string Tagline, string Icon, bool IsActive, TrackMeta[] Tracks, ContractMeta[] Contracts);

    private static readonly CampaignMeta[] Definitions = new[]
    {
        // 1. RSI Discovery Month
        new CampaignMeta(
            Id: "Iasi_ScenarioProgress",
            Title: "RSI Discovery Month",
            Description: "Willkommen zum jährlichen RSI Discovery Event! Schließe Event-Aufträge rund um Stanton ab, um Punkte zu sammeln und exklusive Schiffskomponenten, Starwalker-Lackierungen und Abzeichen freizuschalten.",
            Tagline: "RSI Entdeckungs-Monat · Alpha 4.10+",
            Icon: "🚀",
            IsActive: true,
            Tracks: new[]
            {
                new TrackMeta("overall", "Gesamtfortschritt (Your Total)", true, "#F59E0B", new[]
                {
                    (4500, "R_PU_IASI_OP_1", "BriskAir IC-10 Kühler, Delphi RS-10 Radar & Sovereign IP-10 Kraftwerk"),
                    (9900, "R_PU_IASI_OP_2", "Ursa Rover Starwalker-Livery & Helios IP-10 Kraftwerk"),
                    (24000, "R_PU_IASI_OP_3", "Zeus Mk II PHB Flight Blades & Helios IP-20 Kraftwerk"),
                    (30000, "R_PU_IASI_OP_4", "Helios IP-30 Kraftwerk & Constellation Starwalker-Livery")
                }),
                new TrackMeta("defense", "Verteidigung & Flottenschutz", false, "#EF4444", new[]
                {
                    (2000, "R_PU_IASI_DEFENSE_1", "Scorpius Echo-Livery & Sovereign IP-20 Kraftwerk"),
                    (4000, "R_PU_IASI_DEFENSE_2", "Meteor PHB & TSB Flight Blades"),
                    (6500, "R_PU_IASI_DEFENSE_3", "Zeus Starwalker-Livery"),
                    (10000, "R_PU_IASI_DEFENSE_4", "P8-SC 'Echo' SMG & Helios IP-20 Kraftwerk")
                }),
                new TrackMeta("transport", "Transport & Logistik", false, "#38BDF8", new[]
                {
                    (2000, "R_PU_IASI_TRANSPORT_1", "RSI Discovery Transport Bronze Badge & Belohnungskiste"),
                    (4000, "R_PU_IASI_TRANSPORT_2", "RSI Discovery Transport Silber Badge & Belohnungskiste"),
                    (6500, "R_PU_IASI_TRANSPORT_3", "RSI Discovery Transport Gold Badge & Fracht-Setup"),
                    (10000, "R_PU_IASI_TRANSPORT_4", "RSI Discovery Transport Platin Badge & Exklusiv-Skin")
                }),
                new TrackMeta("collection", "Rohstoffe & Bergung", false, "#10B981", new[]
                {
                    (2000, "R_PU_IASI_COLLECTION_1", "RSI Discovery Collection Bronze Badge"),
                    (4000, "R_PU_IASI_COLLECTION_2", "RSI Discovery Collection Silber Badge"),
                    (6500, "R_PU_IASI_COLLECTION_3", "RSI Discovery Collection Gold Badge"),
                    (10000, "R_PU_IASI_COLLECTION_4", "RSI Discovery Collection Platin Badge & Ausrüstung")
                })
            },
            Contracts: new[]
            {
                new ContractMeta("Iasi_ResourceGathering_Mining_Quantanium", "RSI Disc. Month: Procure Refined Quantainium", "Shubin Interstellar", 1142, "collection"),
                new ContractMeta("Iasi_ResourceGathering_Mining_Laranite", "RSI Disc. Month: Procure Refined Laranite", "Shubin Interstellar", 850, "collection"),
                new ContractMeta("Iasi_ResourceGathering_Salvage_Large", "RSI Disc. Month: Large Salvage Recovery", "Shubin Interstellar", 720, "collection"),
                new ContractMeta("Iasi_ResourceGathering_Salvage_Medium", "RSI Disc. Month: Medium Salvage Recovery", "Shubin Interstellar", 450, "collection"),
                new ContractMeta("Iasi_HaulCargo_AtoB_Supply_SecuritySupplies", "RSI Disc. Month: Important Supply Haul", "Covalex Shipping", 625, "transport"),
                new ContractMeta("Iasi_HaulCargo_AtoB_Supply_Large", "RSI Disc. Month: Large Event Cargo Haul", "Covalex Shipping", 500, "transport"),
                new ContractMeta("Iasi_HaulCargo_AtoB_Supply_Medium", "RSI Disc. Month: Medium Event Cargo Haul", "Covalex Shipping", 350, "transport"),
                new ContractMeta("Iasi_HaulCargo_AtoB_Supply_Small", "RSI Disc. Month: Small Event Cargo Haul", "Covalex Shipping", 200, "transport"),
                new ContractMeta("Iasi_Courier_GoldItem_OnceOnly", "RSI Disc. Month: Special Discovery Plinth Box", "RSI Outreach", 450, "transport"),
                new ContractMeta("Iasi_2H_Box_Courier", "RSI Disc. Month: Urgent Courier Run", "Covalex Shipping", 250, "transport", Variants: 3),
                new ContractMeta("Iasi_Patrol_Hard", "RSI Disc. Month: Orange Lvl. - Neutralize Threats", "Foxwell Enforcement", 417, "defense", Variants: 3),
                new ContractMeta("Iasi_Patrol_Medium", "RSI Disc. Month: Yellow Lvl. - Patrol System", "Foxwell Enforcement", 280, "defense", Variants: 3),
                new ContractMeta("Iasi_Patrol_Easy", "RSI Disc. Month: Green Lvl. - Routine Patrol", "Foxwell Enforcement", 160, "defense", Variants: 3),
                new ContractMeta("Iasi_DefendShip_Hard", "RSI Disc. Month: Heavy Escort & Defense", "Foxwell Enforcement", 380, "defense", Variants: 3),
                new ContractMeta("Iasi_DefendShip_Medium", "RSI Disc. Month: Escort Support Flight", "Foxwell Enforcement", 260, "defense", Variants: 3),
                new ContractMeta("Iasi_DefendShip_Easy", "RSI Disc. Month: Support Ship in Distress", "Foxwell Enforcement", 146, "defense", Variants: 3),
                new ContractMeta("Iasi_EliminateAll_Hard", "RSI Disc. Month: High Value Outlaw Cell Sweep", "Northrock Service Group", 350, "defense", Variants: 3),
                new ContractMeta("Iasi_EliminateAll_Medium", "RSI Disc. Month: Outlaw Harassment Clearance", "Northrock Service Group", 240, "defense", Variants: 3),
                new ContractMeta("Iasi_EliminateSpecific_Hard", "RSI Disc. Month: Outlaw Strike Leader", "Northrock Service Group", 300, "defense", Variants: 3),
                new ContractMeta("Iasi_EliminateSpecific_Easy", "RSI Disc. Month: Intercept Outlaw Scout", "Northrock Service Group", 125, "defense", Variants: 3)
            }
        ),

        // 2. Orison Relief Services
        new CampaignMeta(
            Id: "ORS_ScenarioProgress",
            Title: "Orison Relief Services",
            Description: "Hilfsgüterlieferungen und Versorgungsketten zur Unterstützung der Plattformen rund um Orison (Crusader).",
            Tagline: "Crusader Relief · Alpha 4.9+",
            Icon: "🏙️",
            IsActive: false,
            Tracks: new[]
            {
                new TrackMeta("overall", "Gesamtfortschritt (Your Total)", true, "#EC4899", new[]
                {
                    (10800, "ORS_Pistol", "ORS Dienstpistole (Custom Skin)"),
                    (27000, "ORS_Suit", "ORS Rettungs-Raumanzug & MedPen-Holster"),
                    (45000, "ORS_Medium_Armor", "ORS Mittlere Spezialpanzerung"),
                    (72000, "ORS_Heavy_Armor", "ORS Schwere Spezialpanzerung (Titan)")
                })
            },
            Contracts: new[]
            {
                new ContractMeta("ORS_CA_Large", "Orison Relief: Large Materials Order", "Orison Relief Services", 3294, "overall"),
                new ContractMeta("ORS_CA_Medium", "Orison Relief: Medium Materials Order", "Orison Relief Services", 2208, "overall"),
                new ContractMeta("ORS_CA_Small", "Orison Relief: Small Materials Order", "Orison Relief Services", 1120, "overall"),
                new ContractMeta("ORS_MA_Large", "Orison Relief: Priority Supply Delivery", "Orison Relief Services", 2800, "overall"),
                new ContractMeta("ORS_MA_Medium", "Orison Relief: Standard Supply Delivery", "Orison Relief Services", 1850, "overall"),
                new ContractMeta("ORS_MA_Small", "Orison Relief: Emergency Medical Courier", "Orison Relief Services", 950, "overall")
            }
        ),

        // 3. Alliance Aid
        new CampaignMeta(
            Id: "CleanAir_ScenarioProgress",
            Title: "Alliance Aid (Clean Air)",
            Description: "Gemeinschaftsinitiative zur Sicherung und humanitären Unterstützung entlegener Außenposten im Stanton- und Nyx-System.",
            Tagline: "Alliance Aid · Civilian Defense",
            Icon: "🛡️",
            IsActive: false,
            Tracks: new[]
            {
                new TrackMeta("overall", "Gesamtfortschritt (Your Total)", true, "#10B981", new[]
                {
                    (5000, "CAA_Tier1", "Civilian Defense Utility Set"),
                    (12000, "CAA_Tier2", "Clean Air Recon Helmet & Uniform"),
                    (25000, "CAA_Tier3", "Alliance Tactical Core Armor"),
                    (40000, "CAA_Tier4", "Alliance Aid Defender Livery")
                })
            },
            Contracts: new[]
            {
                new ContractMeta("CleanAir_Supply_Large", "Alliance Aid: Heavy Cargo Run", "Civilian Defense", 1800, "overall"),
                new ContractMeta("CleanAir_Supply_Medium", "Alliance Aid: Medical Run", "Civilian Defense", 1200, "overall"),
                new ContractMeta("CleanAir_Patrol", "Alliance Aid: Outpost Patrol", "Civilian Defense", 850, "overall")
            }
        ),

        // 4. Return of XenoThreat
        new CampaignMeta(
            Id: "RoX_ScenarioProgress",
            Title: "Return of XenoThreat",
            Description: "Verteidigung der Stanton-Systemgrenzen gegen die XenoThreat-Invasion. Gemeinsame Operationen mit der UEE Navy und lokalen Sicherheitskräften.",
            Tagline: "System Defense · Navy Fleet Support",
            Icon: "⚔️",
            IsActive: false,
            Tracks: new[]
            {
                new TrackMeta("overall", "Gesamtfortschritt (Your Total)", true, "#F97316", new[]
                {
                    (10000, "RoX_Tier1", "CDF Bronze Medal & Navy Cap"),
                    (25000, "RoX_Tier2", "CDF Combat Flightsuit"),
                    (50000, "RoX_Tier3", "XenoThreat Veteran Heavy Armor"),
                    (100000, "RoX_Tier4", "UEE Navy Destroyer Escort Badge & Gladius Livery")
                })
            },
            Contracts: new[]
            {
                new ContractMeta("RoX_Defend_Fleet", "XenoThreat: Defend Fleet Convoy", "People's Alliance", 4500, "overall"),
                new ContractMeta("RoX_Supply_Javelin", "XenoThreat: Resupply Javelin Warship", "People's Alliance", 3500, "overall"),
                new ContractMeta("RoX_Engage_Idris", "XenoThreat: Engage Outlaw Capital Ships", "People's Alliance", 6000, "overall")
            }
        )
    };

    /// <summary>
    /// Berechnet den aktuellen Status aller Kampagnen anhand der historischen DB-Events
    /// sowie der aktuell im laufenden LogParser erfassten Verträge.
    /// </summary>
    public static List<CampaignStatusDto> GetCampaignProgress(IEnumerable<ContractRecord>? liveContracts = null)
    {
        var result = new List<CampaignStatusDto>();
        var liveList = liveContracts?.ToList() ?? new List<ContractRecord>();

        // 1. Historische Missionen aus DB ermitteln
        var completedRuns = new Dictionary<string, (int Count, DateTime? Last)>(StringComparer.OrdinalIgnoreCase);
        var inProgressRuns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // a) Live-Verträge auswerten
        foreach (var c in liveList)
        {
            var code = NormalizeContractCode(c.ContractCode, c.Title);
            if (string.IsNullOrWhiteSpace(code)) continue;

            if (c.Outcome == ContractOutcome.Completed)
            {
                if (!completedRuns.TryGetValue(code, out var cur))
                    completedRuns[code] = (1, c.CompletedAt);
                else
                    completedRuns[code] = (cur.Count + 1, c.CompletedAt ?? cur.Last);
            }
            else if (c.Outcome == ContractOutcome.InProgress)
            {
                inProgressRuns[code] = inProgressRuns.GetValueOrDefault(code) + 1;
            }
        }

        // b) Aus SQLite DB einlesen (MissionTaken / MissionReward / MissionDone)
        try
        {
            Database.EnsureInitialized();
            var rawMissions = Database.AllMissionHistoryEvents(limit: 5000);
            foreach (var e in rawMissions)
            {
                var detail = e.Detail ?? "";
                var code = ExtractContractCodeFromText(detail);
                if (!string.IsNullOrWhiteSpace(code))
                {
                    if (!completedRuns.TryGetValue(code, out var cur))
                        completedRuns[code] = (1, e.Time);
                    else
                        completedRuns[code] = (cur.Count + 1, e.Time > (cur.Last ?? DateTime.MinValue) ? e.Time : cur.Last);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("CampaignCatalog.DbRead", ex);
        }

        // 2. Kampagnen-Stati aggregieren
        foreach (var meta in Definitions)
        {
            var status = new CampaignStatusDto
            {
                Id = meta.Id,
                Title = meta.Title,
                Description = meta.Description,
                Tagline = meta.Tagline,
                Icon = meta.Icon,
                IsActive = meta.IsActive
            };

            var contractStatusList = new List<CampaignContractStatusDto>();
            int campaignTotalPoints = 0;
            int campaignCompletedCount = 0;

            var pointsPerTrack = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var cm in meta.Contracts)
            {
                var run = completedRuns.GetValueOrDefault(cm.Id);
                var open = inProgressRuns.GetValueOrDefault(cm.Id);

                // Auch Subvarianten _0, _1, _2 prüfen
                for (int v = 0; v <= cm.Variants; v++)
                {
                    var varKey = $"{cm.Id}_{v}";
                    if (completedRuns.TryGetValue(varKey, out var varRun))
                    {
                        run = (run.Count + varRun.Count, varRun.Last > (run.Last ?? DateTime.MinValue) ? varRun.Last : run.Last);
                    }
                    if (inProgressRuns.TryGetValue(varKey, out var varOpen))
                    {
                        open += varOpen;
                    }
                }

                int earned = run.Count * cm.Points;
                campaignTotalPoints += earned;
                campaignCompletedCount += run.Count;

                // Punkte dem jeweiligen Track und dem Gesamttreck gutschreiben
                pointsPerTrack["overall"] = pointsPerTrack.GetValueOrDefault("overall") + earned;
                if (!cm.TrackId.Equals("overall", StringComparison.OrdinalIgnoreCase))
                {
                    pointsPerTrack[cm.TrackId] = pointsPerTrack.GetValueOrDefault(cm.TrackId) + earned;
                }

                var trackName = meta.Tracks.FirstOrDefault(t => t.Id.Equals(cm.TrackId, StringComparison.OrdinalIgnoreCase))?.Name ?? "Allgemein";

                contractStatusList.Add(new CampaignContractStatusDto
                {
                    ContractId = cm.Id,
                    Title = cm.Title,
                    Issuer = cm.Issuer,
                    Points = cm.Points,
                    TrackId = cm.TrackId,
                    TrackName = trackName,
                    CompletedCount = run.Count,
                    OpenCount = open,
                    LastCompleted = run.Last?.ToLocalTime().ToString("dd.MM.yy HH:mm")
                });
            }

            status.TotalPoints = pointsPerTrack.GetValueOrDefault("overall", 0);
            status.TotalCompleted = campaignCompletedCount;
            status.Contracts = contractStatusList.OrderByDescending(c => c.CompletedCount).ThenByDescending(c => c.Points).ToList();

            // Tracks & Tier-Fortschritte auswerten
            foreach (var tm in meta.Tracks)
            {
                int trackPoints = pointsPerTrack.GetValueOrDefault(tm.Id, 0);
                var tierDtos = new List<CampaignTierDto>();
                int currentTier = 0;
                int? nextTierPoints = null;

                for (int i = 0; i < tm.Tiers.Length; i++)
                {
                    var (minPts, badge, reward) = tm.Tiers[i];
                    bool reached = trackPoints >= minPts;
                    if (reached) currentTier = i + 1;
                    else if (nextTierPoints == null) nextTierPoints = minPts;

                    tierDtos.Add(new CampaignTierDto
                    {
                        TierLevel = i + 1,
                        MinPoints = minPts,
                        Badge = badge,
                        Reward = reward,
                        Reached = reached
                    });
                }

                int? pointsToNext = nextTierPoints.HasValue ? Math.Max(0, nextTierPoints.Value - trackPoints) : null;
                double progressPercent = 0.0;
                if (nextTierPoints.HasValue)
                {
                    int prevTierMin = currentTier > 0 ? tm.Tiers[currentTier - 1].MinPoints : 0;
                    int span = nextTierPoints.Value - prevTierMin;
                    progressPercent = span > 0 ? Math.Clamp(Math.Round(((double)(trackPoints - prevTierMin) / span) * 100.0, 1), 0.0, 100.0) : 100.0;
                }
                else
                {
                    progressPercent = 100.0;
                }

                // Schnellste Vorschläge berechnen (welche Aufträge schließen die Lücke am schnellsten)
                var suggestions = new List<CampaignSuggestionDto>();
                if (pointsToNext.HasValue && pointsToNext.Value > 0)
                {
                    var eligibleContracts = meta.Contracts
                        .Where(c => tm.Overall || c.TrackId.Equals(tm.Id, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(c => c.Points)
                        .Take(3);

                    foreach (var ec in eligibleContracts)
                    {
                        int needed = (int)Math.Ceiling((double)pointsToNext.Value / ec.Points);
                        int done = contractStatusList.FirstOrDefault(c => c.ContractId == ec.Id)?.CompletedCount ?? 0;
                        suggestions.Add(new CampaignSuggestionDto
                        {
                            Contract = ec.Id,
                            Title = ec.Title,
                            Issuer = ec.Issuer,
                            Points = ec.Points,
                            Needed = needed,
                            Done = done
                        });
                    }
                }

                status.Tracks.Add(new CampaignTrackDto
                {
                    Id = tm.Id,
                    Name = tm.Name,
                    Overall = tm.Overall,
                    Color = tm.Color,
                    Points = trackPoints,
                    CurrentTier = currentTier,
                    NextTierPoints = nextTierPoints,
                    PointsToNext = pointsToNext,
                    ProgressPercent = progressPercent,
                    Tiers = tierDtos,
                    FastestSuggestions = suggestions
                });
            }

            result.Add(status);
        }

        return result;
    }

    private static string NormalizeContractCode(string? code, string? title)
    {
        if (!string.IsNullOrWhiteSpace(code))
        {
            // Subvarianten wie _0, _1, _2 auf Basisnamen normalisieren
            return Regex.Replace(code.Trim(), @"_\d+$", "");
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            return ExtractContractCodeFromText(title);
        }

        return "";
    }

    private static string ExtractContractCodeFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";

        // Prüfen auf [con] Tag aus MissionTaken Detail
        var bracketMatch = Regex.Match(text, @"\[(?<con>Iasi_[^\]]+|ORS_[^\]]+|CleanAir_[^\]]+|RoX_[^\]]+)\]", RegexOptions.IgnoreCase);
        if (bracketMatch.Success)
            return Regex.Replace(bracketMatch.Groups["con"].Value, @"_\d+$", "");

        // Direkte Namensübereinstimmung
        var directMatch = Regex.Match(text, @"\b(?<con>Iasi_[A-Za-z0-9_]+|ORS_[A-Za-z0-9_]+|CleanAir_[A-Za-z0-9_]+|RoX_[A-Za-z0-9_]+)\b", RegexOptions.IgnoreCase);
        if (directMatch.Success)
            return Regex.Replace(directMatch.Groups["con"].Value, @"_\d+$", "");

        // Heuristik nach Missionstiteln (z.B. RSI Discovery Month Aufträge)
        if (text.Contains("Procure Refined Quantainium", StringComparison.OrdinalIgnoreCase)) return "Iasi_ResourceGathering_Mining_Quantanium";
        if (text.Contains("Important Supply Haul", StringComparison.OrdinalIgnoreCase)) return "Iasi_HaulCargo_AtoB_Supply_SecuritySupplies";
        if (text.Contains("Neutralize Threats", StringComparison.OrdinalIgnoreCase) || text.Contains("Orange Lvl", StringComparison.OrdinalIgnoreCase)) return "Iasi_Patrol_Hard";
        if (text.Contains("Support Ship in Distress", StringComparison.OrdinalIgnoreCase)) return "Iasi_DefendShip_Easy";
        if (text.Contains("Medium Materials Order", StringComparison.OrdinalIgnoreCase)) return "ORS_CA_Medium";
        if (text.Contains("Large Materials Order", StringComparison.OrdinalIgnoreCase)) return "ORS_CA_Large";

        return "";
    }
}
