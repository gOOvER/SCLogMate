using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SCLogMate.Core;

public partial class RefineryJob : ObservableObject
{
    [ObservableProperty] private string id = Guid.NewGuid().ToString("N");
    [ObservableProperty] private string station = "HUR-L1 Green Glade Station";
    [ObservableProperty] private string material = "Quantanium";
    [ObservableProperty] private string method = "Dinyx Solventation";
    [ObservableProperty] private int inputUnits = 3200; // e.g. 32 SCU
    [ObservableProperty] private int outputScu = 30; // e.g. 30 SCU
    [ObservableProperty] private long costAuec = 14500;
    [ObservableProperty] private long estimatedValueAuec = 750000;
    [ObservableProperty] private DateTime startedAt = DateTime.UtcNow;
    [ObservableProperty] private TimeSpan duration = TimeSpan.FromHours(4.5);
    [ObservableProperty] private bool isCollected = false;
    [ObservableProperty] private bool hasNotifiedDone = false;

    public DateTime CompletedAt => StartedAt + Duration;

    public bool IsCompleted => DateTime.UtcNow >= CompletedAt;

    public TimeSpan RemainingTime
    {
        get
        {
            var left = CompletedAt - DateTime.UtcNow;
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }
    }

    public double ProgressPercent
    {
        get
        {
            if (Duration.TotalSeconds <= 0) return 100;
            var elapsed = (DateTime.UtcNow - StartedAt).TotalSeconds;
            var pct = (elapsed / Duration.TotalSeconds) * 100.0;
            return Math.Clamp(pct, 0.0, 100.0);
        }
    }

    public string StatusText
    {
        get
        {
            if (IsCollected) return "✓ Abgeholt";
            if (IsCompleted) return "★ Fertiggestellt (Abholbereit)";
            var rem = RemainingTime;
            return rem.TotalHours >= 1 
                ? $"⏳ In Arbeit (noch {rem.Hours}h {rem.Minutes}m)" 
                : $"⏳ In Arbeit (noch {rem.Minutes}m {rem.Seconds}s)";
        }
    }

    public string StatusColor => IsCollected ? "#7E97AD" : (IsCompleted ? "#3FB950" : "#F59E0B");

    public void RefreshTime()
    {
        OnPropertyChanged(nameof(RemainingTime));
        OnPropertyChanged(nameof(ProgressPercent));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusColor));
    }
}

public static class RefineryCatalog
{
    public static readonly List<string> Stations = new()
    {
        "HUR-L1 Green Glade Station",
        "HUR-L2 Support Station",
        "CRU-L1 Ambitious Dream Station",
        "ARC-L1 Wide Forest Station",
        "ARC-L2 Industrial Station",
        "MIC-L1 Shallow Frontier Station",
        "Pyro — Orbit Station"
    };

    public static readonly List<string> Materials = new()
    {
        "Quantanium",
        "Beryl",
        "Gold",
        "Laranite",
        "Bexalite",
        "Taranite",
        "Borase",
        "Agricium",
        "Hephaestanite",
        "Titanium",
        "Copper",
        "Corundum"
    };

    public static readonly List<string> Methods = new()
    {
        "Dinyx Solventation (95% Ertrag, Langsam, Günstig)",
        "Ferron Exchange (90% Ertrag, Ausgewogen)",
        "Gaskin Process (88% Ertrag, Schnell)",
        "Pyrometric Chromaglyphy (80% Ertrag, Sehr schnell)",
        "Cormack Method (85% Ertrag, Sehr günstig)"
    };
}
