using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SCLogMate.Core;

public partial class FactionReputation : ObservableObject
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string ShortName { get; init; } = "";
    public string Category { get; init; } = "Sicherheit"; // Sicherheit, Fracht, Industrie, Unterwelt
    public string Icon { get; init; } = "🛡";
    public string System { get; init; } = "Stanton";
    public string Description { get; init; } = "";

    public bool HasPrestige { get; init; }
    public string? PrestigeReward { get; init; }
    public int[] Thresholds { get; init; } = { 0, 1000, 3000, 7500, 15000, 30000 };
    public List<string> CustomTitles { get; init; } = new();

    [ObservableProperty] private int currentXp;
    [ObservableProperty] private int completedMissions;
    [ObservableProperty] private DateTime? lastMissionTime;

    public int MaxLevel => Thresholds.Length;

    public int CurrentLevel => CalculateLevel(CurrentXp, Thresholds);

    public string LevelTitle
    {
        get
        {
            int lvl = CurrentLevel;
            if (CustomTitles != null && CustomTitles.Count >= lvl)
            {
                return CustomTitles[lvl - 1];
            }
            return GetDefaultLevelTitle(lvl, Category);
        }
    }

    public double LevelProgressPercent => CalculateProgressPercent(CurrentXp, Thresholds);

    public int NextLevelXp => GetNextLevelXp(CurrentLevel, Thresholds);

    public string Standing
    {
        get
        {
            if (HasPrestige && CurrentLevel >= 4) return $"Prestige {CurrentLevel - 3}";
            return CurrentLevel switch
            {
                <= 1 => "Neutral",
                2 => "Associate",
                3 => "Trusted",
                4 => "Favorable",
                >= 5 => "Admired"
            };
        }
    }

    public string ProgressText
    {
        get
        {
            int lvl = CurrentLevel;
            if (lvl >= Thresholds.Length)
            {
                return $"{CurrentXp:N0} / {Thresholds[^1]:N0} XP (MAX)";
            }
            return $"{CurrentXp:N0} / {Thresholds[lvl]:N0} XP";
        }
    }

    public string MissionsCountText => $"{CompletedMissions} {(CompletedMissions == 1 ? "Auftrag" : "Aufträge")}";

    private static readonly IBrush BrushSicherheit = new SolidColorBrush(Color.Parse("#38BDF8"));
    private static readonly IBrush BrushFracht = new SolidColorBrush(Color.Parse("#FFB23E"));
    private static readonly IBrush BrushIndustrie = new SolidColorBrush(Color.Parse("#34D399"));
    private static readonly IBrush BrushUnterwelt = new SolidColorBrush(Color.Parse("#F87171"));
    private static readonly IBrush BrushDefault = new SolidColorBrush(Color.Parse("#A371F7"));

    private static readonly IBrush BgBrushSicherheit = new SolidColorBrush(Color.Parse("#1A1D6FA5"));
    private static readonly IBrush BgBrushFracht = new SolidColorBrush(Color.Parse("#1AFFB23E"));
    private static readonly IBrush BgBrushIndustrie = new SolidColorBrush(Color.Parse("#1A34D399"));
    private static readonly IBrush BgBrushUnterwelt = new SolidColorBrush(Color.Parse("#1AF87171"));
    private static readonly IBrush BgBrushDefault = new SolidColorBrush(Color.Parse("#1AA371F7"));

    public IBrush CategoryBrush => Category switch
    {
        "Sicherheit" => BrushSicherheit,
        "Fracht" => BrushFracht,
        "Industrie" => BrushIndustrie,
        "Unterwelt" => BrushUnterwelt,
        _ => BrushDefault
    };

    public IBrush CategoryBgBrush => Category switch
    {
        "Sicherheit" => BgBrushSicherheit,
        "Fracht" => BgBrushFracht,
        "Industrie" => BgBrushIndustrie,
        "Unterwelt" => BgBrushUnterwelt,
        _ => BgBrushDefault
    };

    public void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(CurrentLevel));
        OnPropertyChanged(nameof(LevelTitle));
        OnPropertyChanged(nameof(LevelProgressPercent));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(MissionsCountText));
    }

    public static int CalculateLevel(int xp, int[]? thresholds = null)
    {
        var t = thresholds ?? new[] { 0, 1000, 3000, 7500, 15000, 30000 };
        for (int i = t.Length - 1; i >= 0; i--)
        {
            if (xp >= t[i]) return i + 1;
        }
        return 1;
    }

    public static int GetNextLevelXp(int level, int[]? thresholds = null)
    {
        var t = thresholds ?? new[] { 0, 1000, 3000, 7500, 15000, 30000 };
        if (level >= t.Length) return t[^1];
        return t[level];
    }

    public static double CalculateProgressPercent(int xp, int[]? thresholds = null)
    {
        var t = thresholds ?? new[] { 0, 1000, 3000, 7500, 15000, 30000 };
        int lvl = CalculateLevel(xp, t);
        if (lvl >= t.Length) return 100.0;
        int currentBase = t[lvl - 1];
        int nextTarget = t[lvl];
        int delta = nextTarget - currentBase;
        if (delta <= 0) return 100.0;
        int progress = xp - currentBase;
        return Math.Clamp((double)progress / delta * 100.0, 0.0, 100.0);
    }

    public static string GetDefaultLevelTitle(int level, string category)
    {
        if (category == "Sicherheit")
        {
            return level switch
            {
                1 => "Rang 1: Junior Contractor",
                2 => "Rang 2: Security Contractor",
                3 => "Rang 3: Senior Contractor",
                4 => "Rang 4: Veteran Specialist",
                5 => "Rang 5: Master Defender",
                _ => "Rang 6: Security Director"
            };
        }
        if (category == "Fracht")
        {
            return level switch
            {
                1 => "Rang 1: Fracht-Kurier",
                2 => "Rang 2: Spediteur",
                3 => "Rang 3: Junior Hauler",
                4 => "Rang 4: Senior Cargo Master",
                5 => "Rang 5: Flotten-Versorger",
                6 => "Rang 6: Handels-Baron",
                _ => "Rang 7: Großmeister"
            };
        }
        if (category == "Industrie")
        {
            return level switch
            {
                1 => "Rang 1: Schürfer",
                2 => "Rang 2: Bergungs-Spezialist",
                3 => "Rang 3: Erfahrener Verwerter",
                4 => "Rang 4: Minen-Vorarbeiter",
                5 => "Rang 5: Industrie-Magnat",
                _ => "Rang 6: Meister der Ressourcen"
            };
        }
        return level switch
        {
            1 => "Rang 1: Straßen-Kontakt",
            2 => "Rang 2: Bekannter Runner",
            3 => "Rang 3: Geschätzter Insider",
            4 => "Rang 4: Syndikats-Vollstrecker",
            5 => "Rang 5: Schatten-Agent",
            _ => "Rang 6: Syndikats-Kopf"
        };
    }
}

public static class ReputationCatalog
{
    private static readonly List<FactionReputation> AllFactions = new()
    {
        // ══════════════════════════════════════════════════════════════════
        // 1. BESONDERE PRESTIGE-MISSIONSGEBER & LEVSKI / NYX
        // ══════════════════════════════════════════════════════════════════
        new FactionReputation
        {
            Id = "RECCO",
            Name = "Recco Battaglia",
            ShortName = "Recco",
            Category = "Industrie",
            Icon = "⛏",
            System = "Nyx (Levski)",
            Description = "Chef-Disponentin für Schürf-, Bergungs- und Blackbox-Operationen in Nyx. Bietet exklusive Prestige-Schiffsbelohnungen.",
            HasPrestige = true,
            PrestigeReward = "P1: Drake Golem (10.8k XP) · P2: MISC Prospector (30k XP) · P3: ARGO MOLE (69.6k XP)",
            Thresholds = new[] { 0, 2400, 6000, 10800, 30000, 69600 },
            CustomTitles = new()
            {
                "Prospective Associate",
                "Associate",
                "Trusted Associate",
                "Prestige 1 (Drake Golem)",
                "Prestige 2 (MISC Prospector)",
                "Prestige 3 (ARGO MOLE)"
            }
        },
        new FactionReputation
        {
            Id = "PEOPLES_ALLIANCE",
            Name = "People's Alliance of Levski",
            ShortName = "People's Alliance",
            Category = "Industrie",
            Icon = "✊",
            System = "Nyx (Levski / Delamar)",
            Description = "Unabhängige Bürgerbewegung und Arbeiter-Miliz von Levski und Delamar.",
            Thresholds = new[] { 0, 1500, 4000, 10000, 25000, 50000 },
            CustomTitles = new()
            {
                "Rang 1: Freund von Levski",
                "Rang 2: Allianz-Unterstützer",
                "Rang 3: Vertrauter Bürger",
                "Rang 4: Minenwächter",
                "Rang 5: Allianz-Ratsmitglied",
                "Rang 6: Volksheld von Delamar"
            }
        },

        // ══════════════════════════════════════════════════════════════════
        // 2. FRACHT, TRANSPORT & HUMANITÄRE LOGISTIK
        // ══════════════════════════════════════════════════════════════════
        new FactionReputation
        {
            Id = "ORISON_RELIEF",
            Name = "Orison Relief Services",
            ShortName = "Orison Relief",
            Category = "Fracht",
            Icon = "🕊",
            System = "Stanton (Crusader)",
            Description = "Notfall- und Frachtversorgungslogistik für Crusader und die Cloudview-Plattformen von Orison.",
            Thresholds = new[] { 0, 500, 2000, 6000, 15000, 35000, 75000 },
            CustomTitles = new()
            {
                "Rang 1: Fracht-Kurier",
                "Rang 2: Zuverlässiger Spediteur",
                "Rang 3: Junior Hauler",
                "Rang 4: Relief Partner",
                "Rang 5: Senior Logistics Officer",
                "Rang 6: Relief Transport Director",
                "Rang 7: Großmeister der Versorgung"
            }
        },
        new FactionReputation
        {
            Id = "ALLIANCE_AID",
            Name = "Alliance Aid",
            ShortName = "Alliance Aid",
            Category = "Fracht",
            Icon = "🤝",
            System = "Stanton",
            Description = "Humanitäre Hilfstransporte, medizinische Güter und Krisenversorgung in Stanton.",
            Thresholds = new[] { 0, 500, 2000, 6000, 15000, 35000, 75000 },
            CustomTitles = new()
            {
                "Rang 1: Freiwilliger Helfer",
                "Rang 2: Feld-Kurier",
                "Rang 3: Versorgungs-Spezialist",
                "Rang 4: Einsatz-Koordinator",
                "Rang 5: Leitender Logistiker",
                "Rang 6: Krisen-Direktor",
                "Rang 7: Humanitär-Pionier"
            }
        },
        new FactionReputation
        {
            Id = "COVALEX",
            Name = "Covalex Shipping",
            ShortName = "Covalex",
            Category = "Fracht",
            Icon = "🚚",
            System = "Stanton",
            Description = "Traditionsreicher Großspediteur für interplanetare Frachtlieferungen und Cargo Hauling.",
            Thresholds = new[] { 0, 500, 2000, 6000, 15000, 35000, 75000 },
            CustomTitles = new()
            {
                "Rang 1: Trainee / Praktikant",
                "Rang 2: Rookie / Neuling",
                "Rang 3: Junior Hauler",
                "Rang 4: Member / Mitglied",
                "Rang 5: Experienced / Erfahrener Spediteur",
                "Rang 6: Senior Transport Specialist",
                "Rang 7: Master Hauler / Großmeister"
            }
        },
        new FactionReputation
        {
            Id = "REDWIND",
            Name = "Red Wind Linehaul",
            ShortName = "Red Wind",
            Category = "Fracht",
            Icon = "📦",
            System = "Stanton & Pyro",
            Description = "Großer Express-Kurierdienst und Frachtlogistiker mit Sitz in Lorville und starker Pyro-Präsenz.",
            Thresholds = new[] { 0, 500, 2000, 6000, 15000, 35000, 75000 },
            CustomTitles = new()
            {
                "Rang 1: Trainee / Praktikant",
                "Rang 2: Rookie / Neuling",
                "Rang 3: Junior Hauler",
                "Rang 4: Member / Mitglied",
                "Rang 5: Experienced / Erfahren",
                "Rang 6: Senior Linehauler",
                "Rang 7: Master Linehauler"
            }
        },
        new FactionReputation
        {
            Id = "LING_FAMILY",
            Name = "Ling Family Hauling",
            ShortName = "Ling Family",
            Category = "Fracht",
            Icon = "🧬",
            System = "Stanton & Nyx",
            Description = "Familiengeführtes Biotechnologie-, Pharma- und Ressourcen-Logistikunternehmen.",
            Thresholds = new[] { 0, 500, 2000, 6000, 15000, 35000, 75000 },
            CustomTitles = new()
            {
                "Rang 1: Trainee / Neuling",
                "Rang 2: Cargo Courier",
                "Rang 3: Junior Logistics Partner",
                "Rang 4: Trusted Associate",
                "Rang 5: Senior Logistics Director",
                "Rang 6: Family Fleet Contractor",
                "Rang 7: Master Supply Partner"
            }
        },
        new FactionReputation
        {
            Id = "UNITED_CARGO",
            Name = "United Cargo Guild",
            ShortName = "Cargo Guild",
            Category = "Fracht",
            Icon = "🚢",
            System = "Stanton & Pyro",
            Description = "Die gewerkschaftliche Dachorganisation aller Frachterpiloten und Transportkapitäne.",
            Thresholds = new[] { 0, 500, 2000, 6000, 15000, 35000, 75000 }
        },

        // ══════════════════════════════════════════════════════════════════
        // 3. SICHERHEIT, KOPFGELD & MILIZ
        // ══════════════════════════════════════════════════════════════════
        new FactionReputation
        {
            Id = "BHG",
            Name = "Bounty Hunters Guild",
            ShortName = "BHG",
            Category = "Sicherheit",
            Icon = "⚔",
            System = "Stanton & Pyro",
            Description = "Die offizielle Gilde aller lizenzierten Kopfgeldjäger der UEE (VLRT bis ERT Zertifizierungen).",
            Thresholds = new[] { 0, 1200, 3600, 9000, 18000, 35000 },
            CustomTitles = new()
            {
                "Rang 1: Tracker Trainee (VLRT)",
                "Rang 2: Novice Tracker (LRT)",
                "Rang 3: Experienced Tracker (MRT)",
                "Rang 4: Senior Tracker (HRT)",
                "Rang 5: Master Tracker (VHRT)",
                "Rang 6: Grandmaster Tracker (ERT)"
            }
        },
        new FactionReputation
        {
            Id = "INTERSEC",
            Name = "Intersec Defense Solutions",
            ShortName = "Intersec",
            Category = "Sicherheit",
            Icon = "🛡",
            System = "Stanton & Pyro",
            Description = "Privates Sicherheits- und Verteidigungsunternehmen für Geleitschutz, Patrouillen und VIP-Sicherheit.",
            Thresholds = new[] { 0, 1000, 3000, 8000, 20000, 45000 },
            CustomTitles = new()
            {
                "Rang 1: Trainee Guard / Rekrut",
                "Rang 2: Junior Security Officer",
                "Rang 3: Tactical Escort Specialist",
                "Rang 4: Patrol Commander",
                "Rang 5: Senior Defense Consultant",
                "Rang 6: Defense Operations Director"
            }
        },
        new FactionReputation
        {
            Id = "NORTHROCK",
            Name = "Northrock Service Group",
            ShortName = "Northrock",
            Category = "Sicherheit",
            Icon = "🛡",
            System = "Stanton",
            Description = "Privates Sicherheitsunternehmen für Escort-, Abfang- und schwere Gruppen-Patrouillenaufträge.",
            Thresholds = new[] { 0, 1500, 4500, 11000, 25000, 50000 }
        },
        new FactionReputation
        {
            Id = "CRU_SEC",
            Name = "Crusader Security",
            ShortName = "Crusader Sec",
            Category = "Sicherheit",
            Icon = "⚡",
            System = "Stanton (Crusader)",
            Description = "Gesetzeshüter von Orison und den Monden Cellin, Daymar und Yela.",
            Thresholds = new[] { 0, 1500, 4500, 11000, 25000, 50000 },
            CustomTitles = new()
            {
                "Rang 1: Junior Security Contractor",
                "Rang 2: Security Contractor",
                "Rang 3: Senior Security Contractor",
                "Rang 4: Veteran Security Contractor",
                "Rang 5: Master Security Contractor",
                "Rang 6: Security Director / Chefermittler"
            }
        },
        new FactionReputation
        {
            Id = "HUR_SEC",
            Name = "Hurston Dynamics Security",
            ShortName = "Hurston Sec",
            Category = "Sicherheit",
            Icon = "⚜",
            System = "Stanton (Hurston)",
            Description = "Die bewaffnete Sicherheitsabteilung des Hurston-Konzerns rund um Lorville.",
            Thresholds = new[] { 0, 1500, 4500, 11000, 25000, 50000 },
            CustomTitles = new()
            {
                "Rang 1: Junior Security Contractor",
                "Rang 2: Security Contractor",
                "Rang 3: Senior Security Contractor",
                "Rang 4: Veteran Security Contractor",
                "Rang 5: Master Security Contractor",
                "Rang 6: Director of Operations"
            }
        },
        new FactionReputation
        {
            Id = "MT_SEC",
            Name = "microTech Protection Services",
            ShortName = "microTech Sec",
            Category = "Sicherheit",
            Icon = "❄",
            System = "Stanton (microTech)",
            Description = "Schutzdienst von New Babbage und den Forschungs-Außenposten auf microTech.",
            Thresholds = new[] { 0, 1500, 4500, 11000, 25000, 50000 }
        },
        new FactionReputation
        {
            Id = "BLACJAC",
            Name = "BlacJac Security",
            ShortName = "BlacJac",
            Category = "Sicherheit",
            Icon = "♠",
            System = "Stanton (ArcCorp)",
            Description = "Polizei- und Sicherheitsmacht für Area18, Wala und Lyria.",
            Thresholds = new[] { 0, 1500, 4500, 11000, 25000, 50000 }
        },
        new FactionReputation
        {
            Id = "CDF",
            Name = "Civilian Defense Force",
            ShortName = "CDF",
            Category = "Sicherheit",
            Icon = "🎖",
            System = "UEE",
            Description = "Zivile Miliz und Hilfsflotte für Großevents wie Siege of Orison & XenoThreat.",
            Thresholds = new[] { 0, 2000, 6000, 15000, 30000, 60000 },
            CustomTitles = new()
            {
                "Rang 1: Freiwilliger / Volunteer",
                "Rang 2: Miliz-Rekrut / Militia Recruit",
                "Rang 3: Miliz-Spezialist / Militia Specialist",
                "Rang 4: Miliz-Veteran / Militia Veteran",
                "Rang 5: Flotten-Unterstützer / Fleet Auxiliary",
                "Rang 6: Flottenheld / Task Force Hero"
            }
        },
        new FactionReputation
        {
            Id = "FOXWELL",
            Name = "Foxwell Enforcement",
            ShortName = "Foxwell",
            Category = "Sicherheit",
            Icon = "🦊",
            System = "Pyro",
            Description = "Paramilitärische Sicherheitsfirma in Pyro für Geleitschutz und Gesetzeshüter-Verträge.",
            Thresholds = new[] { 0, 1500, 4500, 11000, 25000, 50000 },
            CustomTitles = new()
            {
                "Rang 1: Junior Guard",
                "Rang 2: Enforcement Officer",
                "Rang 3: Senior Operative",
                "Rang 4: Tactical Specialist",
                "Rang 5: Commander",
                "Rang 6: Chief of Operations"
            }
        },

        // ══════════════════════════════════════════════════════════════════
        // 4. INDUSTRIE, BERGBAU, BERGUNG & FORSCHUNG
        // ══════════════════════════════════════════════════════════════════
        new FactionReputation
        {
            Id = "ADAGIO",
            Name = "Adagio Holdings",
            ShortName = "Adagio",
            Category = "Industrie",
            Icon = "🧲",
            System = "Stanton & Pyro",
            Description = "Kommerzieller Schrott-, Wrack- und RMC-Bergungskonzern mit Industrie- und Werftaufträgen.",
            Thresholds = new[] { 0, 1000, 3000, 7500, 18000, 40000 },
            CustomTitles = new()
            {
                "Rang 1: Schrottsammler / Scrapper",
                "Rang 2: Bergungs-Spezialist",
                "Rang 3: Erfahrener RMC-Verwerter",
                "Rang 4: Wrack-Gutachter",
                "Rang 5: Bergungs-Meister",
                "Rang 6: Großflotten-Demontierer"
            }
        },
        new FactionReputation
        {
            Id = "SHUBIN",
            Name = "Shubin Interstellar",
            ShortName = "Shubin",
            Category = "Industrie",
            Icon = "⛏",
            System = "Stanton & Pyro",
            Description = "Mächtiger Bergbau-Konzern mit Förderanlagen und Raffineriestationen in Stanton und Pyro.",
            Thresholds = new[] { 0, 1000, 3000, 8000, 20000, 45000 },
            CustomTitles = new()
            {
                "Rang 1: Schürf-Hilfskraft",
                "Rang 2: Lizenzierter Miner",
                "Rang 3: Erfahrener Prospektor",
                "Rang 4: Abbauleiter",
                "Rang 5: Distrikt-Chefgeologe",
                "Rang 6: Bergbau-Magnat"
            }
        },
        new FactionReputation
        {
            Id = "WAYFARERS",
            Name = "United Wayfarers Club",
            ShortName = "Wayfarers",
            Category = "Industrie",
            Icon = "🧭",
            System = "Stanton & Pyro",
            Description = "Pionier- und Erkundungsgilde für Deep-Space-Scans, POI-Kartierung und Routenfindung.",
            Thresholds = new[] { 0, 1000, 3000, 8000, 20000, 45000 },
            CustomTitles = new()
            {
                "Rang 1: Wanderer / Wayfarer",
                "Rang 2: Pfadfinder / Scout",
                "Rang 3: Kartograf / Cartographer",
                "Rang 4: Navigator",
                "Rang 5: Sternen-Pionier / Pioneer",
                "Rang 6: Legendärer Navigator"
            }
        },
        new FactionReputation
        {
            Id = "CITIZENS_PROSPERITY",
            Name = "Citizens for Prosperity",
            ShortName = "Citizens Prosperity",
            Category = "Industrie",
            Icon = "🌾",
            System = "Pyro",
            Description = "Siedler- und Handelsföderation in Pyro für Ressourcen- und Versorgungsstabilität.",
            Thresholds = new[] { 0, 1000, 3000, 8000, 20000, 45000 }
        },
        new FactionReputation
        {
            Id = "PYRO_SALVAGE",
            Name = "Pyro Salvage Syndicate",
            ShortName = "Pyro Salvage",
            Category = "Industrie",
            Icon = "🛠",
            System = "Pyro",
            Description = "Schrott- und Bergungsspezialisten für Schiffswracks und RMC in Pyro.",
            Thresholds = new[] { 0, 1000, 3000, 7500, 18000, 40000 }
        },

        // ══════════════════════════════════════════════════════════════════
        // 5. UNTERWELT, DATEN & KONTAKTE
        // ══════════════════════════════════════════════════════════════════
        new FactionReputation
        {
            Id = "BITZEROS",
            Name = "BitZeros",
            ShortName = "BitZeros",
            Category = "Unterwelt",
            Icon = "💾",
            System = "Stanton",
            Description = "Underground-Kollektiv für Datenkuriere, verschlüsselte Pings und Zero-Day Datenübertragungen.",
            Thresholds = new[] { 0, 800, 2400, 6000, 15000, 35000 },
            CustomTitles = new()
            {
                "Rang 1: Byte-Runner",
                "Rang 2: Krypto-Kurier",
                "Rang 3: Netz-Infiltrator",
                "Rang 4: Zero-Day Broker",
                "Rang 5: Cyber-Architekt",
                "Rang 6: Master Hacker"
            }
        },
        new FactionReputation
        {
            Id = "TWITCH",
            Name = "Tecia 'Twitch' Pacheco",
            ShortName = "Twitch",
            Category = "Unterwelt",
            Icon = "🕶",
            System = "Stanton (Area18)",
            Description = "Ex-Militär-Kontakt in Area18 für heikle und inoffizielle Geheim-Operationen."
        },
        new FactionReputation
        {
            Id = "WALLACE",
            Name = "Wallace Klim",
            ShortName = "Wallace",
            Category = "Unterwelt",
            Icon = "🧪",
            System = "Stanton (GrimHEX)",
            Description = "Chemiker und Schmuggler-Disponent mit Labor in GrimHEX."
        },
        new FactionReputation
        {
            Id = "CLOVUS",
            Name = "Clovus Darneely",
            ShortName = "Clovus",
            Category = "Unterwelt",
            Icon = "🗝",
            System = "Stanton (Lorville)",
            Description = "Antiquitätenhändler und Bergungs-Auftraggeber im Reclamations-Distrikt."
        },
        new FactionReputation
        {
            Id = "HEADHUNTERS",
            Name = "Headhunters",
            ShortName = "Headhunters",
            Category = "Unterwelt",
            Icon = "☠",
            System = "Stanton & Pyro",
            Description = "Gefürchtetes Söldner- und Piratensyndikat für schwere Kampfaufträge."
        },
        new FactionReputation
        {
            Id = "ROUGH_ANIMALS",
            Name = "Rough Animals",
            ShortName = "Rough Animals",
            Category = "Unterwelt",
            Icon = "🐺",
            System = "Pyro",
            Description = "Skrupellose Pyro-Gang mit Fokus auf Territoriumskontrolle und Überfälle."
        },
        new FactionReputation
        {
            Id = "RUTO",
            Name = "Ruto",
            ShortName = "Ruto",
            Category = "Unterwelt",
            Icon = "👤",
            System = "GrimHEX / Pyro",
            Description = "Anonymer Hacker und Hologramm-Vermittler im Schmugglernetzwerk."
        }
    };

    public static List<FactionReputation> CreateFreshFactionList()
    {
        return AllFactions.Select(f => new FactionReputation
        {
            Id = f.Id,
            Name = f.Name,
            ShortName = f.ShortName,
            Category = f.Category,
            Icon = f.Icon,
            System = f.System,
            Description = f.Description,
            HasPrestige = f.HasPrestige,
            PrestigeReward = f.PrestigeReward,
            Thresholds = f.Thresholds,
            CustomTitles = f.CustomTitles,
            CurrentXp = 0,
            CompletedMissions = 0
        }).ToList();
    }

    public static IReadOnlyList<FactionReputation> GetAllFactions() => AllFactions;

    public static FactionReputation? GetFaction(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        return AllFactions.FirstOrDefault(f => f.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Matched einen Missions-Auftraggeber oder Titel hochpräzise auf die zugehörige Fraktion.
    /// Vermeidet generische Stichwortkollisionen (z. B. Orison relief vs. Crusader Security).
    /// </summary>
    public static FactionReputation? MatchFaction(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var t = text.ToLowerInvariant();

        // 1. Spezifische Multi-Wort-Matches zuerst (höchste Priorität)
        if (t.Contains("orison relief"))
            return AllFactions.FirstOrDefault(f => f.Id == "ORISON_RELIEF");

        if (t.Contains("alliance aid"))
            return AllFactions.FirstOrDefault(f => f.Id == "ALLIANCE_AID");

        if (t.Contains("adagio holdings") || t.Contains("adagio"))
            return AllFactions.FirstOrDefault(f => f.Id == "ADAGIO");

        if (t.Contains("recco battaglia") || t.Contains("recco"))
            return AllFactions.FirstOrDefault(f => f.Id == "RECCO");

        if (t.Contains("intersec defense solutions") || t.Contains("intersec defense") || t.Contains("intersec"))
            return AllFactions.FirstOrDefault(f => f.Id == "INTERSEC");

        if (t.Contains("ling family hauling") || t.Contains("ling family") || t.Contains("ling hauling") || t.Contains("ling biotechnology") || t.Contains("lingbio"))
            return AllFactions.FirstOrDefault(f => f.Id == "LING_FAMILY");

        if (t.Contains("covalex shipping") || t.Contains("covalex"))
            return AllFactions.FirstOrDefault(f => f.Id == "COVALEX");

        if (t.Contains("red wind line") || t.Contains("red wind") || t.Contains("redwind"))
            return AllFactions.FirstOrDefault(f => f.Id == "REDWIND");

        if (t.Contains("united wayfarers") || t.Contains("wayfarers") || t.Contains("wayfarer"))
            return AllFactions.FirstOrDefault(f => f.Id == "WAYFARERS");

        if (t.Contains("bitzeros") || t.Contains("bit zeros"))
            return AllFactions.FirstOrDefault(f => f.Id == "BITZEROS");

        if (t.Contains("shubin interstellar") || t.Contains("shubin"))
            return AllFactions.FirstOrDefault(f => f.Id == "SHUBIN");

        if (t.Contains("foxwell enforcement") || t.Contains("foxwell"))
            return AllFactions.FirstOrDefault(f => f.Id == "FOXWELL");

        if (t.Contains("people's alliance") || t.Contains("peoples alliance"))
            return AllFactions.FirstOrDefault(f => f.Id == "PEOPLES_ALLIANCE");

        if (t.Contains("citizens for prosperity") || t.Contains("citizens prosperity"))
            return AllFactions.FirstOrDefault(f => f.Id == "CITIZENS_PROSPERITY");

        if (t.Contains("northrock service group") || t.Contains("northrock"))
            return AllFactions.FirstOrDefault(f => f.Id == "NORTHROCK");

        if (t.Contains("crusader security") || t.Contains("crusader sec"))
            return AllFactions.FirstOrDefault(f => f.Id == "CRU_SEC");

        if (t.Contains("hurston dynamics security") || t.Contains("hurston security") || t.Contains("hurston sec"))
            return AllFactions.FirstOrDefault(f => f.Id == "HUR_SEC");

        if (t.Contains("microtech protection") || t.Contains("microtech sec"))
            return AllFactions.FirstOrDefault(f => f.Id == "MT_SEC");

        if (t.Contains("blacjac security") || t.Contains("blacjac"))
            return AllFactions.FirstOrDefault(f => f.Id == "BLACJAC");

        if (t.Contains("civilian defense force") || t.Contains("civilian defense") || t.Contains("cdf") || t.Contains("xenothreat") || t.Contains("siege of orison"))
            return AllFactions.FirstOrDefault(f => f.Id == "CDF");

        if (t.Contains("bounty hunters guild") || t.Contains("bounty hunter") || t.Contains("bhg"))
            return AllFactions.FirstOrDefault(f => f.Id == "BHG");

        if (t.Contains("tecia pacheco") || t.Contains("twitch"))
            return AllFactions.FirstOrDefault(f => f.Id == "TWITCH");

        if (t.Contains("wallace klim") || t.Contains("wallace"))
            return AllFactions.FirstOrDefault(f => f.Id == "WALLACE");

        if (t.Contains("clovus darneely") || t.Contains("clovus"))
            return AllFactions.FirstOrDefault(f => f.Id == "CLOVUS");

        if (t.Contains("ruto"))
            return AllFactions.FirstOrDefault(f => f.Id == "RUTO");

        if (t.Contains("headhunters") || t.Contains("head hunters"))
            return AllFactions.FirstOrDefault(f => f.Id == "HEADHUNTERS");

        if (t.Contains("rough animals"))
            return AllFactions.FirstOrDefault(f => f.Id == "ROUGH_ANIMALS");

        if (t.Contains("pyro salvage syndicate") || t.Contains("pyro salvage"))
            return AllFactions.FirstOrDefault(f => f.Id == "PYRO_SALVAGE");

        if (t.Contains("united cargo guild") || t.Contains("united cargo"))
            return AllFactions.FirstOrDefault(f => f.Id == "UNITED_CARGO");

        // 2. Spezifische Auftragstitel der Levski / Nyx Missionen von Recco Battaglia
        if (t.Contains("blackbox retrieval") || t.Contains("ship in distress") || t.Contains("missing mining team") || t.Contains("keeger belt") || t.Contains("glaciem ring"))
            return AllFactions.FirstOrDefault(f => f.Id == "RECCO");

        // 3. Kopfgeldjäger Risikostufen
        if (t.Contains("vlrt") || t.Contains("lrt") || t.Contains("mrt") || t.Contains("hrt") || t.Contains("vhrt") || t.Contains("ert"))
            return AllFactions.FirstOrDefault(f => f.Id == "BHG");

        return null;
    }

    /// <summary>
    /// Berechnet die realistischen Rufpunkte (XP) für einen Missionserfolg basierend auf
    /// Missionsskalierung, Schwierigkeit und Belohnung.
    /// </summary>
    public static int CalculateMissionXp(string title, long reward, string? difficulty = null, string? category = null)
    {
        var t = (title ?? "").ToLowerInvariant();
        var d = (difficulty ?? "").ToLowerInvariant();

        // 1. Extreme & Story-Aufträge
        if (t.Contains("very dangerous") || d.Contains("sehr schwer") || t.Contains("ert"))
            return 900;
        if (t.Contains("w/danger") || t.Contains("dangerous") || d.Contains("schwer") || t.Contains("vhrt"))
            return 650;
        if (t.Contains("hrt") || t.Contains("siege of orison") || t.Contains("xenothreat"))
            return 600;

        // 2. Bergung, Vermisste & Blackbox
        if (t.Contains("ship in distress") || t.Contains("schiff in not"))
            return 450;
        if (t.Contains("missing mining") || t.Contains("missing persons"))
            return 400;
        if (t.Contains("blackbox") || t.Contains("flight recorder"))
            return 500;

        // 3. Fracht- & Transportskalierung
        if (t.Contains("large") || t.Contains("groß") || t.Contains("interstellar") || t.Contains("senior"))
            return 650;
        if (t.Contains("medium") || t.Contains("mittel") || t.Contains("junior") || t.Contains("order") || t.Contains("direktroute"))
            return 350;
        if (t.Contains("small") || t.Contains("klein") || t.Contains("neuling") || t.Contains("rookie") || t.Contains("kurier"))
            return 175;

        // 4. Kampf / Kopfgeld
        if (t.Contains("mrt")) return 450;
        if (t.Contains("lrt")) return 250;
        if (t.Contains("vlrt")) return 100;

        // 5. Gestaffelter Fallback über Belohnungsbetrag
        if (reward >= 150000) return 500;
        if (reward >= 70000) return 350;
        if (reward >= 30000) return 250;
        return 175;
    }
}
