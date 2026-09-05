using CommunityToolkit.Mvvm.ComponentModel;

namespace SCLogMate.Models;

public partial class RsTargetItem : ObservableObject
{
    public string Name { get; set; } = "";
    public string Tier { get; set; } = "C";
    public string TierColor { get; set; } = "#94A3B8";
    public string Method { get; set; } = "ship";
    public int BaseRs { get; set; }

    [ObservableProperty]
    private bool isEnabled;

    partial void OnIsEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(CardBackground));
        OnPropertyChanged(nameof(CardBorderBrush));
        OnPropertyChanged(nameof(NameForeground));
    }

    public string MethodIcon => Method switch
    {
        "salvage" => "🏗️ Salvage",
        "fps" or "fps+vehicle" => "💎 Hand-Gem",
        "vehicle" => "🚗 ROC-Gem",
        _ => "🪨 Schiffs-Erz"
    };

    public string BaseRsDisplay => BaseRs > 0 ? $"{BaseRs:N0} RS" : "";

    public string CardBackground => IsEnabled ? "#0D223A" : "#08101E";
    public string CardBorderBrush => IsEnabled ? "#38BDF8" : "#1A2E44";
    public string NameForeground => IsEnabled ? "#FFFFFF" : "#CBD5E1";
}
