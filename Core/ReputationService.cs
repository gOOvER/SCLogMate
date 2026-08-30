using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SCLogReader.Core;

public partial class FactionReputation : ObservableObject
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string ShortName { get; init; } = "";
    public string Category { get; init; } = "Sicherheit"; // Sicherheit, Fracht, Industrie, Unterwelt
    public string Icon { get; init; } = "🛡";
    public string System { get; init; } = "Stanton";
    public string Description { get; init; } = "";

    [ObservableProperty] private int currentXp;
    [ObservableProperty] private int completedMissions;
    [ObservableProperty] private DateTime? lastMissionTime;

    public int CurrentLevel => CalculateLevel(CurrentXp);
    public string LevelTitle => GetLevelTitle(CurrentLevel, Category);
    public double LevelProgressPercent => CalculateProgressPercent(CurrentXp);
    public string ProgressText => $"{CurrentXp:N0} / {GetNextLevelXp(CurrentLevel):N0} XP";
    public string MissionsCountText => $"{CompletedMissions} {(CompletedMissions == 1 ? "Auftrag" : "Aufträge")}";

    public IBrush CategoryBrush => Category switch
    {
        "Sicherheit" => new SolidColorBrush(Color.Parse("#38BDF8")), // Ice Cyan
        "Fracht" => new SolidColorBrush(Color.Parse("#FFB23E")),     // Warm Amber
        "Industrie" => new SolidColorBrush(Color.Parse("#34D399")),  // Emerald Green
        "Unterwelt" => new SolidColorBrush(Color.Parse("#F87171")),  // Crimson Red
        _ => new SolidColorBrush(Color.Parse("#A371F7"))             // Purple
    };

    public IBrush CategoryBgBrush => Category switch
    {
        "Sicherheit" => new SolidColorBrush(Color.Parse("#1A1D6FA5")),
        "Fracht" => new SolidColorBrush(Color.Parse("#1AFFB23E")),
        "Industrie" => new SolidColorBrush(Color.Parse("#1A34D399")),
        "Unterwelt" => new SolidColorBrush(Color.Parse("#1AF87171")),
        _ => new SolidColorBrush(Color.Parse("#1AA371F7"))
    };

    public void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(CurrentLevel));
        OnPropertyChanged(nameof(LevelTitle));
        OnPropertyChanged(nameof(LevelProgressPercent));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(MissionsCountText));
    }

    private static readonly int[] LevelXpThresholds = { 0, 1000, 3000, 7500, 15000, 30000 };

    public static int CalculateLevel(int xp)
    {
        for (int i = LevelXpThresholds.Length - 1; i >= 0; i--)
        {
            if (xp >= LevelXpThresholds[i]) return i + 1;
        }
        return 1;
    }

    public static int GetNextLevelXp(int level)
    {
        if (level >= LevelXpThresholds.Length) return LevelXpThresholds[^1] * 2;
        return LevelXpThresholds[level];
    }

    public static double CalculateProgressPercent(int xp)
    {
        int lvl = CalculateLevel(xp);
        if (lvl >= LevelXpThresholds.Length) return 100.0;
        int currentBase = LevelXpThresholds[lvl - 1];
        int nextTarget = LevelXpThresholds[lvl];
        int delta = nextTarget - currentBase;
        if (delta <= 0) return 100.0;
        int progress = xp - currentBase;
        return Math.Clamp((double)progress / delta * 100.0, 0.0, 100.0);
    }

    public static string GetLevelTitle(int level, string category)
    {
        if (category == "Sicherheit")
        {
            return level switch
            {
                1 => "Rang 1: Rekrut / Trainee",
                2 => "Rang 2: Junior Hunter",
                3 => "Rang 3: Senior Officer",
                4 => "Rang 4: Veteran Specialist",
                5 => "Rang 5: Master Defender",
                _ => "Rang 6: Legendärer Kommandant"
            };
        }
        if (category == "Fracht")
        {
            return level switch
            {
                1 => "Rang 1: Fracht-Kurier",
                2 => "Rang 2: Zuverlässiger Spediteur",
                3 => "Rang 3: Senior Cargo Master",
                4 => "Rang 4: Logistik-Direktor",
                5 => "Rang 5: Flotten-Versorger",
                _ => "Rang 6: Handels-Baron"
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
        // Unterwelt
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
        // Sicherheit & Kopfgeld
        new FactionReputation
        {
            Id = "BHG",
            Name = "Bounty Hunters Guild",
            ShortName = "BHG",
            Category = "Sicherheit",
            Icon = "⚔",
            System = "Stanton & Pyro",
            Description = "Die offizielle Gilde aller lizenzierten Kopfgeldjäger der UEE."
        },
        new FactionReputation
        {
            Id = "NORTHROCK",
            Name = "Northrock Service Group",
            ShortName = "Northrock",
            Category = "Sicherheit",
            Icon = "🛡",
            System = "Stanton",
            Description = "Privates Sicherheitsunternehmen für Escort-, Abfang- und Patrouillen-Aufträge."
        },
        new FactionReputation
        {
            Id = "CRU_SEC",
            Name = "Crusader Security",
            ShortName = "Crusader Sec",
            Category = "Sicherheit",
            Icon = "⚡",
            System = "Stanton (Crusader)",
            Description = "Gesetzeshüter von Orison und den Monden Cellin, Daymar und Yela."
        },
        new FactionReputation
        {
            Id = "HUR_SEC",
            Name = "Hurston Dynamics Security",
            ShortName = "Hurston Sec",
            Category = "Sicherheit",
            Icon = "⚜",
            System = "Stanton (Hurston)",
            Description = "Die bewaffnete Sicherheitsabteilung des Hurston-Konzerns rund um Lorville."
        },
        new FactionReputation
        {
            Id = "MT_SEC",
            Name = "microTech Protection Services",
            ShortName = "microTech Sec",
            Category = "Sicherheit",
            Icon = "❄",
            System = "Stanton (microTech)",
            Description = "Schutzdienst von New Babbage und den Forschungs-Außenposten auf microTech."
        },
        new FactionReputation
        {
            Id = "BLACJAC",
            Name = "BlacJac Security",
            ShortName = "BlacJac",
            Category = "Sicherheit",
            Icon = "♠",
            System = "Stanton (ArcCorp)",
            Description = "Polizei- und Sicherheitsmacht für Area18, Wala und Lyria."
        },
        new FactionReputation
        {
            Id = "CDF",
            Name = "Civilian Defense Force",
            ShortName = "CDF",
            Category = "Sicherheit",
            Icon = "🎖",
            System = "UEE",
            Description = "Zivile Miliz und Flotte für Flotten-Events wie Siege of Orison & XenoThreat."
        },

        // Fracht & Logistik
        new FactionReputation
        {
            Id = "REDWIND",
            Name = "Red Wind Line",
            ShortName = "Red Wind",
            Category = "Fracht",
            Icon = "📦",
            System = "Stanton (Hurston)",
            Description = "Großer Express-Kurierdienst und Frachtlogistiker mit Sitz in Lorville."
        },
        new FactionReputation
        {
            Id = "UNITED_CARGO",
            Name = "United Cargo Guild",
            ShortName = "Cargo Guild",
            Category = "Fracht",
            Icon = "🚢",
            System = "Stanton & Pyro",
            Description = "Die gewerkschaftliche Vertretung aller Raumfrachter und Transportkapitäne."
        },
        new FactionReputation
        {
            Id = "COVALEX",
            Name = "Covalex Shipping",
            ShortName = "Covalex",
            Category = "Fracht",
            Icon = "🚚",
            System = "Stanton",
            Description = "Traditionsreicher Großspediteur für interplanetare Frachtlieferungen."
        },

        // Industrie & Bergung
        new FactionReputation
        {
            Id = "RECCO",
            Name = "Recco Battaglia",
            ShortName = "Recco",
            Category = "Industrie",
            Icon = "⛏",
            System = "Stanton (Levski / Delamar)",
            Description = "Chef-Disponentin für Schürf-, Bergbau- und Ressourcen-Expeditionen."
        },
        new FactionReputation
        {
            Id = "PYRO_SALVAGE",
            Name = "Pyro Salvage Syndicate",
            ShortName = "Pyro Salvage",
            Category = "Industrie",
            Icon = "🛠",
            System = "Pyro",
            Description = "Schrott- und Bergungsspezialisten für Schiffswracks und RMC in Pyro."
        },

        // Unterwelt & Kontakte
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
            CurrentXp = 0,
            CompletedMissions = 0
        }).ToList();
    }

    public static FactionReputation? MatchFaction(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var t = text.ToLowerInvariant();
        if (t.Contains("bounty hunter") || t.Contains("kopfgeld") || t.Contains("bhg") || t.Contains("vlrt") || t.Contains("lrt") || t.Contains("mrt") || t.Contains("hrt") || t.Contains("vhrt") || t.Contains("ert"))
            return AllFactions.FirstOrDefault(f => f.Id == "BHG");

        if (t.Contains("northrock"))
            return AllFactions.FirstOrDefault(f => f.Id == "NORTHROCK");

        if (t.Contains("crusader sec") || t.Contains("crusader") || t.Contains("orison"))
            return AllFactions.FirstOrDefault(f => f.Id == "CRU_SEC");

        if (t.Contains("hurston sec") || t.Contains("hurston dynamics") || t.Contains("lorville"))
            return AllFactions.FirstOrDefault(f => f.Id == "HUR_SEC");

        if (t.Contains("microtech") || t.Contains("new babbage"))
            return AllFactions.FirstOrDefault(f => f.Id == "MT_SEC");

        if (t.Contains("blacjac") || t.Contains("arccorp") || t.Contains("area18") || t.Contains("lyria"))
            return AllFactions.FirstOrDefault(f => f.Id == "BLACJAC");

        if (t.Contains("cdf") || t.Contains("civilian defense") || t.Contains("xenothreat") || t.Contains("siege of orison"))
            return AllFactions.FirstOrDefault(f => f.Id == "CDF");

        if (t.Contains("red wind"))
            return AllFactions.FirstOrDefault(f => f.Id == "REDWIND");

        if (t.Contains("cargo") || t.Contains("hauling") || t.Contains("fracht") || t.Contains("united cargo"))
            return AllFactions.FirstOrDefault(f => f.Id == "UNITED_CARGO");

        if (t.Contains("covalex"))
            return AllFactions.FirstOrDefault(f => f.Id == "COVALEX");

        if (t.Contains("recco") || t.Contains("delamar") || t.Contains("levski") || t.Contains("mining") || t.Contains("schürf"))
            return AllFactions.FirstOrDefault(f => f.Id == "RECCO");

        if (t.Contains("salvage") || t.Contains("pyro salvage") || t.Contains("bergung") || t.Contains("rmc"))
            return AllFactions.FirstOrDefault(f => f.Id == "PYRO_SALVAGE");

        if (t.Contains("twitch") || t.Contains("pacheco"))
            return AllFactions.FirstOrDefault(f => f.Id == "TWITCH");

        if (t.Contains("wallace") || t.Contains("klim") || t.Contains("labor"))
            return AllFactions.FirstOrDefault(f => f.Id == "WALLACE");

        if (t.Contains("clovus") || t.Contains("darneely"))
            return AllFactions.FirstOrDefault(f => f.Id == "CLOVUS");

        if (t.Contains("ruto"))
            return AllFactions.FirstOrDefault(f => f.Id == "RUTO");

        return null;
    }
}
