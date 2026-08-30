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
            else if (char.IsWhiteSpace(ch) && (sb.Length == 0 || sb[^1] != ' ')) sb.Append(' ');
        }
        return sb.ToString().Trim();
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
        // ── BOUNTY HUNTER GUILD & SICHERHEITSKRÄFTE (KOPFGELDJAGD) ──────────────────────────
        Add(new MissionInfo
        {
            Id = "bhg_tracker_cert",
            Title = "Tracker Training Permit Certification",
            Contractor = "Bounty Hunters Guild",
            Faction = "Bounty Hunters Guild",
            MissionType = "Kopfgeldjagd",
            BaseReward = 4000,
            ReputationGain = 100,
            StarSystems = "Stanton",
            Description = "Einstiegstest der Kopfgeldjäger-Gilde. Eliminiere das ausgewiesene Übungsziel zur Freischaltung regulärer Kopfgelder."
        });
        Add(new MissionInfo
        {
            Id = "bhg_vlrt_stanton",
            Title = "Very Low Risk Target (VLRT)",
            Contractor = "Bounty Hunters Guild",
            Faction = "Bounty Hunters Guild",
            MissionType = "Kopfgeldjagd",
            BaseReward = 7500,
            ReputationGain = 150,
            StarSystems = "Stanton",
            Description = "Leichtes Kopfgeldziel (meist Aurora, Mustang, Buccaneer). Aufspüren und neutralisieren."
        });
        Add(new MissionInfo
        {
            Id = "bhg_lrt_stanton",
            Title = "Low Risk Target (LRT)",
            Contractor = "Bounty Hunters Guild",
            Faction = "Bounty Hunters Guild",
            MissionType = "Kopfgeldjagd",
            BaseReward = 11500,
            ReputationGain = 250,
            StarSystems = "Stanton",
            Description = "Niedriges Risikoziel mit leichter Begleiteskorte (Cutlass Black, Gladius)."
        });
        Add(new MissionInfo
        {
            Id = "bhg_mrt_stanton",
            Title = "Medium Risk Target (MRT)",
            Contractor = "Bounty Hunters Guild",
            Faction = "Bounty Hunters Guild",
            MissionType = "Kopfgeldjagd",
            BaseReward = 19500,
            ReputationGain = 400,
            StarSystems = "Stanton",
            Blueprints = new[] { "Piecemeal Armor Core" },
            Description = "Mittleres Risikoziel mit bewaffneter Eskorte (Vanguard, Freelancer MIS)."
        });
        Add(new MissionInfo
        {
            Id = "bhg_hrt_stanton",
            Title = "High Risk Target (HRT)",
            Contractor = "Bounty Hunters Guild",
            Faction = "Bounty Hunters Guild",
            MissionType = "Kopfgeldjagd",
            BaseReward = 32000,
            ReputationGain = 650,
            StarSystems = "Stanton",
            Blueprints = new[] { "Killshot Rifle", "Badami Helmet" },
            Description = "Schweres Kopfgeldziel (Andromeda, Eclipse, Hurricane) in Asteroidenfeldern oder auf Mondoberflächen."
        });
        Add(new MissionInfo
        {
            Id = "bhg_vhrt_stanton",
            Title = "Very High Risk Target (VHRT)",
            Contractor = "Bounty Hunters Guild",
            Faction = "Bounty Hunters Guild",
            MissionType = "Kopfgeldjagd",
            BaseReward = 48500,
            ReputationGain = 950,
            StarSystems = "Stanton",
            Blueprints = new[] { "Overlord Helmet Supernova", "Killshot Rifle" },
            Description = "Sehr gefährliche Zielperson mit schwerer Geleitschutzflotte (Retaliator, Eclipse, Starfarer)."
        });
        Add(new MissionInfo
        {
            Id = "bhg_ert_stanton",
            Title = "Extreme Risk Target (ERT)",
            Contractor = "Bounty Hunters Guild",
            Faction = "Bounty Hunters Guild",
            MissionType = "Kopfgeldjagd",
            BaseReward = 75000,
            ReputationGain = 1500,
            StarSystems = "Stanton",
            Blueprints = new[] { "Overlord Core Supernova", "Overlord Arms Supernova", "Deadrig Shotgun" },
            Description = "Höchste Gefahrenstufe: Großschiffe wie Hammerhead, Reclaimer und C2 Gunships mit voller Piratenbesatzung."
        });

        // ── NORTHROCK SERVICE GROUP (GRUPPEN-KOPFGELDER) ───────────────────
        Add(new MissionInfo
        {
            Id = "northrock_group_mrt",
            Title = "Group Medium Risk Target Warrant",
            Contractor = "Northrock Service Group",
            Faction = "Northrock Service Group",
            MissionType = "Kopfgeldjagd",
            BaseReward = 44000,
            ReputationGain = 600,
            StarSystems = "Stanton",
            Description = "Neutralisiere drei koordinierte Piraten-Ziele in unterschiedlichen Bereichen von Stanton."
        });
        Add(new MissionInfo
        {
            Id = "northrock_group_hrt",
            Title = "Group High Risk Target Warrant",
            Contractor = "Northrock Service Group",
            Faction = "Northrock Service Group",
            MissionType = "Kopfgeldjagd",
            BaseReward = 68000,
            ReputationGain = 900,
            StarSystems = "Stanton",
            Blueprints = new[] { "Killshot Rifle", "Badami Arms" },
            Description = "Drei schwere Ziele mit Eskorten über Crusader, Hurston und ArcCorp ausschalten."
        });
        Add(new MissionInfo
        {
            Id = "northrock_group_vhrt",
            Title = "Group Very High Risk Target Warrant",
            Contractor = "Northrock Service Group",
            Faction = "Northrock Service Group",
            MissionType = "Kopfgeldjagd",
            BaseReward = 95000,
            ReputationGain = 1400,
            StarSystems = "Stanton",
            Blueprints = new[] { "Overlord Helmet Supernova", "Overlord Core Supernova" },
            Description = "Drei hochgradig bewaffnete Flottenverbände vor Ort neutralisieren."
        });
        Add(new MissionInfo
        {
            Id = "northrock_group_ert",
            Title = "Group Extreme Risk Target Warrant",
            Contractor = "Northrock Service Group",
            Faction = "Northrock Service Group",
            MissionType = "Kopfgeldjagd",
            BaseReward = 135000,
            ReputationGain = 2200,
            StarSystems = "Stanton",
            Blueprints = new[] { "Overlord Core Supernova", "Deadrig Shotgun", "Killshot Rifle Magazine" },
            Description = "Drei feindliche Hammerhead-Großschiffe mit vollem Geleitschutz eliminieren."
        });

        // ── SÖLDNER & SICHERHEITSEINSÄTZE (BUNKER / VERTEIDIGUNG) ────────────
        Add(new MissionInfo
        {
            Id = "merc_defend_occupants_t1",
            Title = "Defend Occupants from Outlaws",
            Contractor = "Crusader Security",
            Faction = "Crusader Industries",
            MissionType = "Söldner",
            BaseReward = 20000,
            ReputationGain = 250,
            StarSystems = "Stanton",
            Description = "Verteidige das lokale Untergrund-Sicherheitszentrum gegen drei Angriffswellen bewaffneter Gesetzloser."
        });
        Add(new MissionInfo
        {
            Id = "merc_defend_occupants_t2",
            Title = "Protect Civilian Research Facility",
            Contractor = "microTech Protection",
            Faction = "microTech",
            MissionType = "Söldner",
            BaseReward = 35000,
            ReputationGain = 400,
            StarSystems = "Stanton",
            Blueprints = new[] { "Badami Helmet" },
            Description = "Schütze das Personal einer abgelegenen Forschungskuppel vor Söldner-Infiltratoren."
        });
        Add(new MissionInfo
        {
            Id = "merc_defend_occupants_t3",
            Title = "Provide Backup at Distribution Center",
            Contractor = "Hurston Security",
            Faction = "Hurston Dynamics",
            MissionType = "Söldner",
            BaseReward = 55000,
            ReputationGain = 600,
            StarSystems = "Stanton",
            Blueprints = new[] { "Killshot Rifle", "Piecemeal Armor Core" },
            Description = "Unterstütze die Wachmannschaft im Verteilzentrum Cassillo/Covalex gegen schwer bewaffnete Piratentrupps."
        });
        Add(new MissionInfo
        {
            Id = "merc_evict_illegal_occupants",
            Title = "Evict Illegal Occupants",
            Contractor = "Hurston Security",
            Faction = "Hurston Dynamics",
            MissionType = "Söldner",
            BaseReward = 65000,
            ReputationGain = 700,
            StarSystems = "Stanton",
            Blueprints = new[] { "Overlord Core Supernova", "Deadrig Shotgun" },
            Description = "Dringe in die besetzte Untergrund-Anlage ein und schalte alle feindlichen Besatzer aus."
        });
        Add(new MissionInfo
        {
            Id = "merc_890_jump_hijack",
            Title = "Boarding Action in Progress",
            Contractor = "microTech Protection",
            Faction = "microTech",
            MissionType = "Söldner",
            BaseReward = 65000,
            ReputationGain = 800,
            StarSystems = "Stanton",
            Blueprints = new[] { "Killshot Rifle", "Overlord Helmet Supernova" },
            Description = "Eine 890 Jump Luxusyacht wurde von Outlaws gekapert. Docke an, rette die Besatzung und schalte alle Angreifer aus."
        });
        Add(new MissionInfo
        {
            Id = "merc_seize_data",
            Title = "Seize the Data",
            Contractor = "BlacJac Security",
            Faction = "BlacJac Security",
            MissionType = "Söldner",
            BaseReward = 45000,
            ReputationGain = 450,
            StarSystems = "Stanton",
            Description = "Entermannschaft auf eine feindliche Drake Herald schicken und den Daten-Upload an kriminelle Hacker stoppen."
        });
        Add(new MissionInfo
        {
            Id = "merc_destroy_surveillance",
            Title = "Unauthorized Surveillance Detected",
            Contractor = "Crusader Security",
            Faction = "Crusader Industries",
            MissionType = "Söldner",
            BaseReward = 25000,
            ReputationGain = 300,
            StarSystems = "Stanton",
            Description = "Finde und zerstöre 3 illegale Überwachungssonden im Orbit des Comm Arrays innerhalb des Zeitlimits."
        });
        Add(new MissionInfo
        {
            Id = "merc_spk_clearing",
            Title = "Security Post Kareah Defense",
            Contractor = "Crusader Security",
            Faction = "Crusader Industries",
            MissionType = "Söldner",
            BaseReward = 70000,
            ReputationGain = 850,
            StarSystems = "Stanton",
            Blueprints = new[] { "Overlord Arms Supernova", "Killshot Rifle" },
            Description = "Verteidige Security Post Kareah vor Kriminellen, die ihre CrimeStat-Einträge hacken wollen."
        });
        Add(new MissionInfo
        {
            Id = "merc_black_kite",
            Title = "Black Kite Infiltration",
            Contractor = "BlacJac Security",
            Faction = "BlacJac Security",
            MissionType = "Söldner",
            BaseReward = 50000,
            ReputationGain = 550,
            StarSystems = "Stanton",
            Description = "Infiltriere ein abgeriegeltes Daten-Versteck und vernichte den feindlichen Anführer."
        });

        // ── FRACHT, LIEFERUNG & LOGISTIK (COURIER & HAULING) ────────────────
        Add(new MissionInfo
        {
            Id = "redwind_local_parcel",
            Title = "Local Parcel Delivery (Multi-Drop)",
            Contractor = "Red Wind Linehaul",
            Faction = "Red Wind Linehaul",
            MissionType = "Fracht & Transport",
            BaseReward = 9500,
            ReputationGain = 120,
            StarSystems = "Stanton",
            Description = "Hole 3 Frachtkisten an Außenposten auf Hurston ab und liefere sie am CBD Lorville ab."
        });
        Add(new MissionInfo
        {
            Id = "redwind_inter_system",
            Title = "Inter-System Express Haul",
            Contractor = "Red Wind Linehaul",
            Faction = "Red Wind Linehaul",
            MissionType = "Fracht & Transport",
            BaseReward = 28000,
            ReputationGain = 350,
            StarSystems = "Stanton",
            Description = "Transportiere Eilsendungen von Everus Harbor nach New Babbage (microTech)."
        });
        Add(new MissionInfo
        {
            Id = "covalex_hub_distribution",
            Title = "Covalex Hub Freight Relocation",
            Contractor = "Covalex Shipping",
            Faction = "Covalex Shipping",
            MissionType = "Fracht & Transport",
            BaseReward = 22500,
            ReputationGain = 280,
            StarSystems = "Stanton",
            Description = "Großfracht-Transport zwischen L1-Lagrange-Stationen und Port Tressler."
        });
        Add(new MissionInfo
        {
            Id = "covalex_hazardous_cargo",
            Title = "Hazardous Material Transit",
            Contractor = "Covalex Shipping",
            Faction = "Covalex Shipping",
            MissionType = "Fracht & Transport",
            BaseReward = 38000,
            ReputationGain = 450,
            StarSystems = "Stanton",
            Description = "Gefahrgut-Transport unter Zeitdruck: Keine Erschütterungen oder Quantumsprung-Fehler erlaubt."
        });
        Add(new MissionInfo
        {
            Id = "united_cargo_supply_run",
            Title = "Cold Chain Bio-Supply Run",
            Contractor = "United Cargo",
            Faction = "United Cargo",
            MissionType = "Fracht & Transport",
            BaseReward = 18500,
            ReputationGain = 220,
            StarSystems = "Stanton",
            Description = "Lieferung gekühlter medizinischer Vorräte an Forschungsposten Rayari auf Calliope."
        });
        Add(new MissionInfo
        {
            Id = "freight_elevator_bulk_haul",
            Title = "Freight Elevator Bulk Transfer (SC 4.x)",
            Contractor = "Red Wind Linehaul",
            Faction = "Red Wind Linehaul",
            MissionType = "Fracht & Transport",
            BaseReward = 72000,
            ReputationGain = 800,
            StarSystems = "Stanton",
            Description = "Verlade 96 SCU Frachtkisten über den Frachtaufzug am Hangar und liefere sie am Ziel-Verteilzentrum ab."
        });

        // ── BERGUNG, SALVAGE & WRACK-VERWERTUNG ──────────────────────────────
        Add(new MissionInfo
        {
            Id = "salvage_hull_scraping_cutlass",
            Title = "Legal Salvage Claim: Drake Cutlass",
            Contractor = "Crusader Security",
            Faction = "Crusader Industries",
            MissionType = "Bergung & Salvage",
            BaseReward = 15000,
            ReputationGain = 180,
            StarSystems = "Stanton",
            Description = "Exklusive Bergbaurechte für ein verlassenes Cutlass-Wrack. Schabe die Hülle ab und sichere RMC."
        });
        Add(new MissionInfo
        {
            Id = "salvage_constellation_recovery",
            Title = "Heavy Salvage Rights: RSI Constellation",
            Contractor = "Hurston Dynamics",
            Faction = "Hurston Dynamics",
            MissionType = "Bergung & Salvage",
            BaseReward = 45000,
            ReputationGain = 450,
            StarSystems = "Stanton",
            Description = "Großes Wrack im Asteroidenring von Yela. Hülle schaben und Strukturteile bergen."
        });
        Add(new MissionInfo
        {
            Id = "salvage_hammerhead_clean_up",
            Title = "Unsanctioned Clean-up: Aegis Hammerhead",
            Contractor = "Duenas Syndicate",
            Faction = "Outlaw Syndicates",
            MissionType = "Bergung & Salvage",
            BaseReward = 85000,
            ReputationGain = 600,
            IsIllegal = true,
            StarSystems = "Stanton",
            Blueprints = new[] { "Deadrig Shotgun", "Overlord Arms Supernova" },
            Description = "Zerstöre und verwertet die Überreste eines illegalen Kriegsschiffs, bevor Sicherheitskräfte eintreffen."
        });
        Add(new MissionInfo
        {
            Id = "salvage_c2_cargo_recovery",
            Title = "Derelict C2 Hercules Cargo Recovery",
            Contractor = "Covalex Shipping",
            Faction = "Covalex Shipping",
            MissionType = "Bergung & Salvage",
            BaseReward = 52000,
            ReputationGain = 500,
            StarSystems = "Stanton",
            Description = "Finde das auf Daymar abgestürzte C2-Wrack und berge alle intakten Frachtkisten."
        });

        // ── ERMITTLUNG, VERSCHIEDENE PERSONEN & BLACK BOX ───────────────────
        Add(new MissionInfo
        {
            Id = "investigation_cave_search",
            Title = "Search and Recovery: Missing Cave Explorer",
            Contractor = "United Cargo",
            Faction = "Civilian Protection",
            MissionType = "Ermittlung",
            BaseReward = 18000,
            ReputationGain = 200,
            StarSystems = "Stanton",
            Description = "Erkunde das Höhlensystem auf Daymar / Aberdeen und finde den vermissten Höhlenforscher."
        });
        Add(new MissionInfo
        {
            Id = "investigation_covalex_gundo",
            Title = "Covalex Hub Gundo Investigation",
            Contractor = "Covalex Shipping",
            Faction = "Covalex Shipping",
            MissionType = "Ermittlung",
            BaseReward = 22000,
            ReputationGain = 250,
            StarSystems = "Stanton",
            Description = "Untersuche die zerstörte Covalex-Raumstation Gundo und finde die Ursache der fatalen Explosion."
        });
        Add(new MissionInfo
        {
            Id = "investigation_black_box_freelancer",
            Title = "Flight Recorder (Black Box) Retrieval",
            Contractor = "Crusader Security",
            Faction = "Crusader Industries",
            MissionType = "Ermittlung",
            BaseReward = 14500,
            ReputationGain = 160,
            StarSystems = "Stanton",
            Description = "Lokalisiere das Flugschreiber-Signal des abgestürzten Schiffs und bringe die Black Box zur Station."
        });

        // ── WARTUNG & COMM-ARRAYS ───────────────────────────────────────────
        Add(new MissionInfo
        {
            Id = "aciedo_comm_array_repair",
            Title = "Comm Array Repair & Reboot",
            Contractor = "Aciedo Communications",
            Faction = "Aciedo Communications",
            MissionType = "Wartung",
            BaseReward = 10000,
            ReputationGain = 1000,
            StarSystems = "Stanton",
            Description = "Reaktiviere ein von Kriminellen abgeschaltetes Kommunikations-Array im Orbit."
        });
        Add(new MissionInfo
        {
            Id = "maintenance_waste_disposal",
            Title = "Orbital Debris & Waste Disposal",
            Contractor = "Hurston Dynamics",
            Faction = "Hurston Dynamics",
            MissionType = "Wartung",
            BaseReward = 12500,
            ReputationGain = 150,
            StarSystems = "Stanton",
            Description = "Beseitige kontaminierte Abfallbehälter an einem der Außenposten auf Arial."
        });

        // ── RECCO BATTAGLIA & NYX SYSTEM (DELAMAR / LEVSKI) ─────────────────
        Add(new MissionInfo
        {
            Id = "recco_missing_mining_team",
            Title = "Missing Mining Team",
            Contractor = "Recco Battaglia",
            Faction = "People's Alliance",
            MissionType = "Ermittlung",
            BaseReward = 26750,
            ReputationGain = 200,
            StarSystems = "Nyx",
            Blueprints = new[] { "Piecemeal Armor Core", "Killshot Rifle" },
            Description = "Eines der Bergbau-Teams ist verschollen. Finde heraus, was mit der Crew im Glaciem Ring geschehen ist."
        });
        Add(new MissionInfo
        {
            Id = "recco_moraine_data_retrieval",
            Title = "Moraine Data Retrieval",
            Contractor = "Recco Battaglia",
            Faction = "People's Alliance",
            MissionType = "Bergung & Salvage",
            BaseReward = 30500,
            ReputationGain = 250,
            StarSystems = "Nyx",
            Blueprints = new[] { "Deadrig Shotgun", "Overlord Helmet Supernova" },
            Description = "Besorge vertrauliche Forschungs- und Scandaten von der verlassenen Moraine-Basis auf Delamar."
        });
        Add(new MissionInfo
        {
            Id = "recco_ship_in_distress",
            Title = "Ship In Distress",
            Contractor = "Recco Battaglia",
            Faction = "People's Alliance",
            MissionType = "Söldner",
            BaseReward = 58000,
            ReputationGain = 450,
            StarSystems = "Nyx",
            Blueprints = new[] { "Overlord Arms Supernova", "Overlord Core Supernova" },
            Description = "Ein Transportschiff der People's Alliance wird von Piraten attackiert. Vernichte die Angreifer."
        });
        Add(new MissionInfo
        {
            Id = "recco_claim_dispute",
            Title = "Mining Claim Dispute",
            Contractor = "Recco Battaglia",
            Faction = "People's Alliance",
            MissionType = "Söldner",
            BaseReward = 45000,
            ReputationGain = 350,
            StarSystems = "Nyx",
            Blueprints = new[] { "Overlord Legs Supernova" },
            Description = "Illegale Schürfer verletzen Delamars Bergbaurechte. Vertreibe die unbefugten Schiffe."
        });

        // ── PYRO SYSTEM (ROUGH & READY, OVERLORDS, RUIN STATION) ────────────
        Add(new MissionInfo
        {
            Id = "pyro_rough_ready_escort",
            Title = "Convoy Defense across Monox",
            Contractor = "Rough & Ready",
            Faction = "Rough & Ready",
            MissionType = "Söldner",
            BaseReward = 62000,
            ReputationGain = 500,
            IsIllegal = true,
            StarSystems = "Pyro",
            Blueprints = new[] { "Killshot Rifle", "Badami Helmet" },
            Description = "Schütze einen illegalen Treibstoff-Konvoi auf Pyro II vor Überfällen rivalisierender Banden."
        });
        Add(new MissionInfo
        {
            Id = "pyro_ruin_station_bounty",
            Title = "Ruin Station Elimination Contract",
            Contractor = "Citizens for Pyro",
            Faction = "Citizens for Pyro",
            MissionType = "Kopfgeldjagd",
            BaseReward = 78000,
            ReputationGain = 750,
            StarSystems = "Pyro",
            Blueprints = new[] { "Overlord Helmet Supernova", "Deadrig Shotgun" },
            Description = "Spüre einen berüchtigten Piratenkapitän im Orbit von Terminus (Pyro VI) auf und eliminiere ihn."
        });
        Add(new MissionInfo
        {
            Id = "pyro_bloom_salvage",
            Title = "Hazardous Volcanic Salvage: Bloom",
            Contractor = "Pyro Salvage Union",
            Faction = "Pyro Salvage Union",
            MissionType = "Bergung & Salvage",
            BaseReward = 88000,
            ReputationGain = 900,
            StarSystems = "Pyro",
            Blueprints = new[] { "Overlord Core Supernova", "Overlord Arms Supernova" },
            Description = "Berge wertvolle Triebwerkskomponenten eines verunglückten Transporters in der Nähe von Lavafeldern auf Pyro III."
        });
        Add(new MissionInfo
        {
            Id = "pyro_checkmate_raid",
            Title = "Repel Outpost Assault at Checkmate",
            Contractor = "Rough & Ready",
            Faction = "Rough & Ready",
            MissionType = "Söldner",
            BaseReward = 92000,
            ReputationGain = 1100,
            IsIllegal = true,
            StarSystems = "Pyro",
            Blueprints = new[] { "Killshot Rifle", "Overlord Legs Supernova" },
            Description = "Verteidige die Checkmate-Station vor feindlichen Kampfschiffen und Entermannschaften."
        });

        // ── UNTERWELT & SPEZIALAUFTRAGGEBER (TWITCH, CLOVUS, RUTO, VAUGHN) ──
        Add(new MissionInfo
        {
            Id = "twitch_price_of_freedom",
            Title = "The Price of Freedom",
            Contractor = "Twitch",
            Faction = "Headhunters",
            MissionType = "Söldner",
            BaseReward = 80000,
            ReputationGain = 700,
            IsIllegal = true,
            StarSystems = "Stanton",
            Blueprints = new[] { "Killshot Rifle", "Overlord Helmet Supernova" },
            Description = "Kapere einen Gefangenentransporter der Sicherheitskräfte und befreie inhaftierte Crewmitglieder."
        });
        Add(new MissionInfo
        {
            Id = "clovus_recovery_operation",
            Title = "Reclamation and Cover-Up",
            Contractor = "Clovus Darneely",
            Faction = "Lorville Underground",
            MissionType = "Bergung & Salvage",
            BaseReward = 42000,
            ReputationGain = 400,
            IsIllegal = true,
            StarSystems = "Stanton",
            Blueprints = new[] { "Piecemeal Armor Core" },
            Description = "Finde das geheime Daten-Relay an einem verlassenen Satelliten auf Hurston und vernichte alle Beweise."
        });
        Add(new MissionInfo
        {
            Id = "ruto_covert_assassination",
            Title = "Covert Contract: Silent Execution",
            Contractor = "Ruto",
            Faction = "GrimHEX Syndicate",
            MissionType = "Kopfgeldjagd",
            BaseReward = 85000,
            ReputationGain = 800,
            IsIllegal = true,
            StarSystems = "Stanton",
            Blueprints = new[] { "Killshot Rifle", "Deadrig Shotgun" },
            Description = "Finde und eliminiere das Ziel diskret, ohne den Alarm an benachbarten Außenposten auszulösen."
        });
        Add(new MissionInfo
        {
            Id = "vaughn_challenging_contract",
            Title = "A Challenging Contract",
            Contractor = "Vaughn",
            Faction = "Vaughn Syndicate",
            MissionType = "Kopfgeldjagd",
            BaseReward = 95000,
            ReputationGain = 900,
            IsIllegal = true,
            StarSystems = "Stanton, Nyx",
            Blueprints = new[] { "Killshot Rifle", "Deadrig Shotgun", "Overlord Core Supernova" },
            Description = "Hochbezahlter Attentatsauftrag im Asteroidenfeld. Schalte die Zielperson und ihre Leibwache aus."
        });
    }
}
