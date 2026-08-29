using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SCLogReader.Models;

public enum AchievementToastType
{
    Blueprint,
    MissionReward,
    Loot,
    Milestone
}

public partial class AchievementToastData : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();
    public AchievementToastType Type { get; set; }
    public string HeaderText { get; set; } = "ERFOLG FREIGESCHALTET";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string IconText { get; set; } = "⬡";
    public string BadgeBg { get; set; } = "#261E08";
    public string BadgeBorder { get; set; } = "#F59E0B";
    public string BadgeColor { get; set; } = "#FBBF24";
    public string TextColor { get; set; } = "#F0F6FC";
    public string SubtitleColor { get; set; } = "#4ADE80";
    public string GlowBorderColor { get; set; } = "#F59E0B";

    [ObservableProperty] private double opacity = 1.0;

    public static AchievementToastData ForBlueprint(string blueprintName)
    {
        return new AchievementToastData
        {
            Type = AchievementToastType.Blueprint,
            HeaderText = "BAUPLAN ERLERNT",
            Title = blueprintName,
            Subtitle = "In persönlicher Crafting-Datenbank registriert",
            IconText = "⬡",
            BadgeBg = "#2E1F07",
            BadgeBorder = "#F59E0B",
            BadgeColor = "#FBBF24",
            TextColor = "#FDF4DC",
            SubtitleColor = "#FBBF24",
            GlowBorderColor = "#F59E0B"
        };
    }

    public static AchievementToastData ForMissionReward(string missionTitle, long reward)
    {
        return new AchievementToastData
        {
            Type = AchievementToastType.MissionReward,
            HeaderText = "AUFTRAG ABGESCHLOSSEN",
            Title = missionTitle,
            Subtitle = reward > 0 ? $"+{reward:N0} aUEC Belohnung" : "Erfolgreich abgeschlossen",
            IconText = "★",
            BadgeBg = "#062E1A",
            BadgeBorder = "#10B981",
            BadgeColor = "#34D399",
            TextColor = "#ECFDF5",
            SubtitleColor = "#34D399",
            GlowBorderColor = "#10B981"
        };
    }

    public static AchievementToastData ForLoot(string itemName)
    {
        return new AchievementToastData
        {
            Type = AchievementToastType.Loot,
            HeaderText = "SELTENER GEGENSTAND",
            Title = itemName,
            Subtitle = "Im Inventar gesichert",
            IconText = "◈",
            BadgeBg = "#0C2338",
            BadgeBorder = "#38BDF8",
            BadgeColor = "#38BDF8",
            TextColor = "#F0F9FF",
            SubtitleColor = "#38BDF8",
            GlowBorderColor = "#0284C7"
        };
    }
}
