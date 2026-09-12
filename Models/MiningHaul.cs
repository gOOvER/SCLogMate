using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SCLogMate.Models;

public partial class MiningHaul : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string? sessionId;
    [ObservableProperty] private string materialName = "Quantainium";
    [ObservableProperty] private double scuQuantity = 32.0;
    [ObservableProperty] private string refineryLocation = "CRU-L1 Ambitious Dream";
    [ObservableProperty] private string method = "Dinyx Dodecathetic";
    [ObservableProperty] private double yieldPercent = 93.0;
    [ObservableProperty] private int costAuec = 2500;
    [ObservableProperty] private DateTime submittedAt = DateTime.UtcNow;
    [ObservableProperty] private int durationSeconds = 7200; // 2h default
    [ObservableProperty] private string status = "Refining"; // Refining, Ready, Collected, Sold
    [ObservableProperty] private int soldAuec = 0;

    public DateTime ReadyAt => SubmittedAt.AddSeconds(DurationSeconds);
    public int RemainingSeconds => Math.Max(0, (int)(ReadyAt - DateTime.UtcNow).TotalSeconds);
    public bool IsTimerCompleted => RemainingSeconds <= 0;
    public double YieldScu => Math.Round(ScuQuantity * (YieldPercent / 100.0), 2);
    public string SubmittedAtFormatted => SubmittedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
    public string ReadyAtFormatted => ReadyAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
}
