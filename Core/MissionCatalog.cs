using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SCLogReader.Core;

public sealed class MissionInfo
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string Contractor { get; init; } = "";
    public string Faction { get; init; } = "";
    public string MissionType { get; init; } = "Sonstiges";
    public int BaseReward { get; init; }
    public int ReputationGain { get; init; }
    public bool IsIllegal { get; init; }
    public string StarSystems { get; init; } = "Stanton";
    public string[] Blueprints { get; init; } = Array.Empty<string>();
    public string Description { get; init; } = "";

    public string RewardText => BaseReward > 0 ? $"+{BaseReward:N0} aUEC" : "Variabel";
    public string ReputationText => ReputationGain > 0 ? $"+{ReputationGain} XP" : "–";
    public string LegalityText => IsIllegal ? "Illegal" : "Legal";
    public bool HasBlueprints => Blueprints.Length > 0;
    public string BlueprintsText => HasBlueprints ? string.Join(", ", Blueprints) : "Keine";
}

/// <summary>
/// Vollständige Master-Missionsdatenbank basierend auf Star Citizen Spieldaten (scunpacked-data & StarCitizenWiki).
/// Bietet exakte und fehlertolerante (Fuzzy) Suche für Logfile- und OCR-Missionsabgleiche.
/// </summary>
public static class MissionCatalog
{
    private static readonly List<MissionInfo> _catalog = new();
    private static readonly Dictionary<string, MissionInfo> _lookupByNormTitle = new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<MissionInfo> AllMissions => _catalog;

    static MissionCatalog()
    {
        InitializeCatalog();
    }

    public static MissionInfo? Lookup(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;
        var norm = Normalize(title);
        return _lookupByNormTitle.TryGetValue(norm, out var m) ? m : null;
    }

    public static MissionInfo? FuzzyLookup(string? rawTitle)
    {
        if (string.IsNullOrWhiteSpace(rawTitle)) return null;
        var exact = Lookup(rawTitle);
        if (exact != null) return exact;

        var rawNorm = Normalize(rawTitle);
        if (rawNorm.Length < 3) return null;

        // 1. Substring / Contains Match
        foreach (var m in _catalog)
        {
            var mNorm = Normalize(m.Title);
            if (mNorm.Length >= 5 && (rawNorm.Contains(mNorm) || mNorm.Contains(rawNorm)))
                return m;
        }

        // 2. Token Overlap Match (mindestens 2 gemeinsame Wörter mit >= 4 Zeichen)
        var rawTokens = rawNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => t.Length >= 4).ToHashSet();
        if (rawTokens.Count > 0)
        {
            MissionInfo? bestMatch = null;
            int maxOverlap = 0;

            foreach (var m in _catalog)
            {
                var mTokens = Normalize(m.Title).Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(t => t.Length >= 4).ToHashSet();
                int overlap = rawTokens.Intersect(mTokens).Count();
                if (overlap >= 2 && overlap > maxOverlap)
                {
                    maxOverlap = overlap;
                    bestMatch = m;
                }
            }

            if (bestMatch != null) return bestMatch;
        }

        return null;
    }

    public static string Normalize(string s)
    {
        var noTags = Regex.Replace(s, @"\[[^\]]*\]", " ");
        var lower = noTags.ToLowerInvariant();
        var sb = new System.Text.StringBuilder(lower.Length);
        foreach (var ch in lower)
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else sb.Append(' ');
        }
        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }

    private static void Add(MissionInfo info)
    {
        _catalog.Add(info);
        var norm = Normalize(info.Title);
        if (!_lookupByNormTitle.ContainsKey(norm))
            _lookupByNormTitle[norm] = info;
    }

    private static void InitializeCatalog()
    {
        // ── RECCO BATTAGLIA (Levski / Delamar / Nyx) ──────────────────────────
        Add(new MissionInfo
        {
            Id = "recco_missing_mining_team",
            Title = "Missing Mining Team",
            Contractor = "Recco Battaglia",
            Faction = "People's Alliance",
            MissionType = "Ermittlung",
            BaseReward = 26750,
            ReputationGain = 160,
            IsIllegal = false,
            StarSystems = "Nyx",
            Blueprints = new[] { "Piecemeal Armor Core", "Killshot Rifle" },
            Description = "Eines der lokalen Bergbau-Teams ist verschollen. Suche den letzten bekannten Standort auf und finde heraus, was mit der Crew geschehen ist."
        });
        Add(new MissionInfo
        {
            Id = "recco_moraine_data_retrieval",
            Title = "Moraine Data Retrieval",
            Contractor = "Recco Battaglia",
            Faction = "People's Alliance",
            MissionType = "Bergung & Salvage",
            BaseReward = 30500,
            ReputationGain = 200,
            IsIllegal = false,
            StarSystems = "Nyx",
            Blueprints = new[] { "Deadrig Shotgun", "Overlord Helmet Supernova" },
            Description = "Besorge vertrauliche Forschungs- und Scandaten von der verlassenen Moraine-Basis."
        });
        Add(new MissionInfo
        {
            Id = "recco_crew_hasnt_checked_in",
            Title = "Crew Hasn't Checked In",
            Contractor = "Recco Battaglia",
            Faction = "People's Alliance",
            MissionType = "Ermittlung",
            BaseReward = 21250,
            ReputationGain = 160,
            IsIllegal = false,
            StarSystems = "Nyx",
            Blueprints = new[] { "Badami Helmet", "Killshot Rifle Magazine" },
            Description = "Ein Bergbau-Team meldet sich seit Stunden nicht. Begib dich zur letzten bekannten Position und identifiziere die Crewmitglieder."
        });
        Add(new MissionInfo
        {
            Id = "recco_ship_in_distress",
            Title = "Ship In Distress",
            Contractor = "Recco Battaglia",
            Faction = "People's Alliance",
            MissionType = "Söldner",
            BaseReward = 58000,
            ReputationGain = 350,
            IsIllegal = false,
            StarSystems = "Nyx",
            Blueprints = new[] { "Overlord Arms Supernova", "Overlord Core Supernova" },
            Description = "Ein Transportschiff der People's Alliance wird von Piraten attackiert. Vernichte die Angreifer und sichere das Wrack."
        });
        Add(new MissionInfo
        {
            Id = "recco_claim_dispute",
            Title = "Mining Claim Dispute",
            Contractor = "Recco Battaglia",
            Faction = "People's Alliance",
            MissionType = "Söldner",
            BaseReward = 45000,
            ReputationGain = 280,
            IsIllegal = false,
            StarSystems = "Nyx",
            Blueprints = new[] { "Overlord Legs Supernova" },
            Description = "Illegale Schürfer verletzen Delamars Bergbaurechte. Vertreibe die unbefugten Schiffe."
        });

        // ── VAUGHN (Attentate & Unterwelt) ───────────────────────────────────
        Add(new MissionInfo
        {
            Id = "vaughn_a_challenging_contract",
            Title = "A Challenging Contract",
            Contractor = "Vaughn",
            Faction = "Vaughn Syndicate",
            MissionType = "Söldner",
            BaseReward = 65000,
            ReputationGain = 250,
            IsIllegal = true,
            StarSystems = "Stanton, Nyx",
            Blueprints = new[] { "Killshot Rifle", "Deadrig Shotgun", "Overlord Core Supernova" },
            Description = "Schwer bewachtes Ziel in einer Forschungsstation neutralisieren."
        });
        Add(new MissionInfo
        {
            Id = "vaughn_a_chance_to_impress",
            Title = "A Chance to Impress",
            Contractor = "Vaughn",
            Faction = "Vaughn Syndicate",
            MissionType = "Söldner",
            BaseReward = 27500,
            ReputationGain = 100,
            IsIllegal = true,
            StarSystems = "Stanton, Nyx",
            Blueprints = new[] { "Badami Helmet" },
            Description = "Demonstration deiner Fähigkeiten: Diskrete Eliminierung eines Ziels an angegebener Adresse."
        });
        Add(new MissionInfo
        {
            Id = "vaughn_an_eye_for_an_eye",
            Title = "An Eye for an Eye",
            Contractor = "Vaughn",
            Faction = "Vaughn Syndicate",
            MissionType = "Söldner",
            BaseReward = 48000,
            ReputationGain = 200,
            IsIllegal = true,
            StarSystems = "Stanton, Nyx",
            Blueprints = new[] { "Overlord Helmet Supernova" },
            Description = "Vergeltungsauftrag gegen einen verräterischen Geschäftspartner."
        });
        Add(new MissionInfo
        {
            Id = "vaughn_high_profile_target",
            Title = "High Profile Target",
            Contractor = "Vaughn",
            Faction = "Vaughn Syndicate",
            MissionType = "Söldner",
            BaseReward = 85000,
            ReputationGain = 400,
            IsIllegal = true,
            StarSystems = "Stanton, Nyx",
            Blueprints = new[] { "Killshot Rifle", "Overlord Core Supernova" },
            Description = "Eliminierung eines hochrangigen UEE-Sicherheitsbeamten im Tiefraum."
        });

        // ── WALLACE KLIM (GrimHEX / Drogen & Schmuggel) ─────────────────────
        Add(new MissionInfo
        {
            Id = "klim_a_batch_from_scratch",
            Title = "A Batch from Scratch",
            Contractor = "Wallace Klim",
            Faction = "Wallace Klim",
            MissionType = "Fracht & Transport",
            BaseReward = 32000,
            ReputationGain = 250,
            IsIllegal = true,
            StarSystems = "Stanton",
            Blueprints = new[] { "MedPen Refill Pack" },
            Description = "Überwache und transportiere die Rohstoffkette und Endprodukte für Wallace Klims Labor."
        });
        Add(new MissionInfo
        {
            Id = "klim_distribution_run",
            Title = "Distribution Run",
            Contractor = "Wallace Klim",
            Faction = "Wallace Klim",
            MissionType = "Fracht & Transport",
            BaseReward = 45000,
            ReputationGain = 300,
            IsIllegal = true,
            StarSystems = "Stanton",
            Blueprints = new[] { "Piecemeal Armor Core" },
            Description = "Lieferung von veredeltem SLAM an Abnehmerstationen im Asteroidengürtel."
        });

        // ── CDF & BOUNTY HUNTERS GUILD (Kopfgeldjagd) ───────────────────────
        Add(new MissionInfo
        {
            Id = "cdf_call_to_arms",
            Title = "A Call to Arms",
            Contractor = "Civilian Defense Force",
            Faction = "UEE / CDF",
            MissionType = "Kopfgeldjagd",
            BaseReward = 0,
            ReputationGain = 50,
            IsIllegal = false,
            StarSystems = "Stanton, Nyx, Pyro",
            Description = "Dauerhafter Vertrag: Prämie für jeden neutralisierten Kriminellen mit aktivem CrimeStat im UEE-Raum."
        });
        Add(new MissionInfo
        {
            Id = "bhg_evaluation",
            Title = "Bounty Hunter Evaluation",
            Contractor = "Bounty Hunters Guild",
            Faction = "Bounty Hunters Guild",
            MissionType = "Kopfgeldjagd",
            BaseReward = 5000,
            ReputationGain = 250,
            IsIllegal = false,
            StarSystems = "Stanton",
            Description = "Zertifizierungsmission zur Aufnahme in die Kopfgeldjäger-Gilde."
        });
        Add(new MissionInfo
        {
            Id = "bhg_vlrt",
            Title = "Very Low Risk Target (VLRT)",
            Contractor = "Bounty Hunters Guild",
            Faction = "Bounty Hunters Guild",
            MissionType = "Kopfgeldjagd",
            BaseReward = 4000,
            ReputationGain = 200,
            IsIllegal = false,
            StarSystems = "Stanton",
            Description = "Kopfgeldauftrag gegen Ziel mit minimaler Bewaffnung (Aurora, Mustang)."
        });
        Add(new MissionInfo
        {
            Id = "bhg_lrt",
            Title = "Low Risk Target (LRT)",
            Contractor = "Bounty Hunters Guild",
            Faction = "Bounty Hunters Guild",
            MissionType = "Kopfgeldjagd",
            BaseReward = 8500,
            ReputationGain = 350,
            IsIllegal = false,
            StarSystems = "Stanton",
            Description = "Kopfgeldauftrag gegen Ziel mit leichter Kampfeskorte (Avenger, Buccaneer)."
        });
        Add(new MissionInfo
        {
            Id = "bhg_mrt",
            Title = "Moderate Risk Target (MRT)",
            Contractor = "Bounty Hunters Guild",
            Faction = "Bounty Hunters Guild",
            MissionType = "Kopfgeldjagd",
            BaseReward = 15000,
            ReputationGain = 600,
            IsIllegal = false,
            StarSystems = "Stanton",
            Description = "Kopfgeldauftrag gegen Ziel im Mehrpersonen-Schiff (Cutlass, Freelancer)."
        });
        Add(new MissionInfo
        {
            Id = "bhg_hrt",
            Title = "High Risk Target (HRT)",
            Contractor = "Bounty Hunters Guild",
            Faction = "Bounty Hunters Guild",
            MissionType = "Kopfgeldjagd",
            BaseReward = 22500,
            ReputationGain = 1200,
            IsIllegal = false,
            StarSystems = "Stanton",
            Description = "Kopfgeldauftrag gegen schweres Ziel mit Eskorte (Vanguard, Hurricane, Connie)."
        });
        Add(new MissionInfo
        {
            Id = "bhg_vhrt",
            Title = "Very High Risk Target (VHRT)",
            Contractor = "Bounty Hunters Guild",
            Faction = "Bounty Hunters Guild",
            MissionType = "Kopfgeldjagd",
            BaseReward = 35000,
            ReputationGain = 2400,
            IsIllegal = false,
            StarSystems = "Stanton",
            Description = "Gefährliche Zielgruppe mit mehreren schweren Jagdschiffen und Gunships."
        });
        Add(new MissionInfo
        {
            Id = "bhg_ert",
            Title = "Extreme Risk Target (ERT)",
            Contractor = "Bounty Hunters Guild",
            Faction = "Bounty Hunters Guild",
            MissionType = "Kopfgeldjagd",
            BaseReward = 55000,
            ReputationGain = 5000,
            IsIllegal = false,
            StarSystems = "Stanton",
            Blueprints = new[] { "Killshot Rifle", "Overlord Core Supernova" },
            Description = "Höchste Bedrohungsstufe: Großschiffe (Hammerhead, Reclaimer, Caterpillar) mit Elite-Eskorte."
        });

        // ── MILES ECKHART & SECURITY ─────────────────────────────────────────
        Add(new MissionInfo
        {
            Id = "eckhart_arlington_gang",
            Title = "The Arlington Gang",
            Contractor = "Miles Eckhart",
            Faction = "Eckhart Security",
            MissionType = "Söldner",
            BaseReward = 95000,
            ReputationGain = 450,
            IsIllegal = false,
            StarSystems = "Stanton",
            Blueprints = new[] { "Deadrig Shotgun", "Overlord Arms Supernova" },
            Description = "Missionskette zur Zerschlagung der berüchtigten Arlington-Gang inklusive ihrer Idris-Fregatte."
        });
        Add(new MissionInfo
        {
            Id = "eckhart_illegal_occupants",
            Title = "Clear Outposts of Illegal Occupants",
            Contractor = "Miles Eckhart",
            Faction = "Eckhart Security",
            MissionType = "Söldner",
            BaseReward = 38000,
            ReputationGain = 300,
            IsIllegal = false,
            StarSystems = "Stanton",
            Blueprints = new[] { "Badami Helmet", "Killshot Rifle Magazine" },
            Description = "Säubere eine besetzte Bergbaustation am Boden von bewaffneten Kriminellen."
        });

        // ── REDWIND & COVALEX & HAULING ───────────────────────────────────────
        Add(new MissionInfo
        {
            Id = "redwind_local_freight",
            Title = "Local Freight Delivery",
            Contractor = "RedWind Linehaul",
            Faction = "RedWind Linehaul",
            MissionType = "Fracht & Transport",
            BaseReward = 14500,
            ReputationGain = 200,
            IsIllegal = false,
            StarSystems = "Stanton",
            Description = "Frachttransport zwischen Orbitalstationen und Oberflächen-Außenposten."
        });
        Add(new MissionInfo
        {
            Id = "redwind_planetary_haul",
            Title = "Planetary Distribution Haul",
            Contractor = "RedWind Linehaul",
            Faction = "RedWind Linehaul",
            MissionType = "Fracht & Transport",
            BaseReward = 48000,
            ReputationGain = 500,
            IsIllegal = false,
            StarSystems = "Stanton",
            Description = "Großraum-Gütertransport von Industrie-Raffinerien zu Verteilzentren (32+ SCU)."
        });
        Add(new MissionInfo
        {
            Id = "covalex_gundo_investigation",
            Title = "Covalex Hub Gundo Investigation",
            Contractor = "Covalex Shipping",
            Faction = "Covalex Shipping",
            MissionType = "Ermittlung",
            BaseReward = 18000,
            ReputationGain = 250,
            IsIllegal = false,
            StarSystems = "Stanton",
            Description = "Untersuche die zerstörte Covalex Hub Gundo Raumstation und finde die Ursache der Explosion heraus."
        });

        // ── INVESTIGATION & SALVAGE (Bergung & Suche) ─────────────────────────
        Add(new MissionInfo
        {
            Id = "salvage_scrap_retrieval",
            Title = "Scrap & Hull Scraping Rights",
            Contractor = "Dumper's Depot",
            Faction = "Dumper's Depot",
            MissionType = "Bergung & Salvage",
            BaseReward = 25000,
            ReputationGain = 300,
            IsIllegal = false,
            StarSystems = "Stanton, Nyx",
            Description = "Kaufe exklusive Bergungsrechte an einem kürzlich verunglückten Frachtschiff."
        });
        Add(new MissionInfo
        {
            Id = "investigation_missing_person",
            Title = "Missing Person Search",
            Contractor = "InterSec Investigations",
            Faction = "InterSec",
            MissionType = "Ermittlung",
            BaseReward = 16500,
            ReputationGain = 180,
            IsIllegal = false,
            StarSystems = "Stanton, Nyx",
            Description = "Finde eine in den Höhlen oder Asteroidenfeldern verschollene Person."
        });
        Add(new MissionInfo
        {
            Id = "investigation_emergency_beacon",
            Title = "Emergency Beacon Investigation",
            Contractor = "InterSec Investigations",
            Faction = "InterSec",
            MissionType = "Ermittlung",
            BaseReward = 15000,
            ReputationGain = 150,
            IsIllegal = false,
            StarSystems = "Stanton, Nyx",
            Description = "Untersuche das Notsignal eines havarierten Schiffes im Asteroidenfeld."
        });

        // ── BUNKER & FACILITY DEFENSE (Sicherheitsdienste) ─────────────────────
        Add(new MissionInfo
        {
            Id = "bunker_defend_occupants",
            Title = "Defend Facility from Attackers",
            Contractor = "Crusader Security",
            Faction = "Crusader Industries",
            MissionType = "Söldner",
            BaseReward = 60000,
            ReputationGain = 500,
            IsIllegal = false,
            StarSystems = "Stanton",
            Blueprints = new[] { "Overlord Arms Supernova", "Killshot Rifle" },
            Description = "Verteidige das unterirdische Sicherheitszentrum vor mehreren Angriffswellen feindlicher Söldner."
        });
        Add(new MissionInfo
        {
            Id = "bunker_evict_illegal_occupants",
            Title = "Evict Illegal Occupants",
            Contractor = "Hurston Security",
            Faction = "Hurston Dynamics",
            MissionType = "Söldner",
            BaseReward = 75000,
            ReputationGain = 650,
            IsIllegal = false,
            StarSystems = "Stanton",
            Blueprints = new[] { "Overlord Core Supernova", "Deadrig Shotgun" },
            Description = "Dringe in die besetzte Untergrund-Anlage ein und schalte alle feindlichen Eindringlinge aus."
        });
        Add(new MissionInfo
        {
            Id = "aciedo_comm_array_repair",
            Title = "Comm Array Repair",
            Contractor = "Aciedo Communications",
            Faction = "Aciedo Communications",
            MissionType = "Wartung",
            BaseReward = 10000,
            ReputationGain = 1000,
            IsIllegal = false,
            StarSystems = "Stanton",
            Description = "Reaktiviere ein von Kriminellen deaktiviertes Kommunikations-Array im Orbit."
        });
        Add(new MissionInfo
        {
            Id = "twitch_the_price_of_freedom",
            Title = "The Price of Freedom",
            Contractor = "Twitch",
            Faction = "Headhunters",
            MissionType = "Söldner",
            BaseReward = 80000,
            ReputationGain = 500,
            IsIllegal = true,
            StarSystems = "Stanton",
            Blueprints = new[] { "Killshot Rifle", "Overlord Helmet Supernova" },
            Description = "Kapere einen Gefangenentransporter der Sicherheitskräfte und befreie inhaftierte Crewmitglieder."
        });
    }
}
