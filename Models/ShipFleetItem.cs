using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SCLogReader.Models;

/// <summary>
/// Repräsentiert ein Schiff im Flotten- und Hangar-Verzeichnis.
/// Unterscheidet zwischen persönlichem Hangar-Besitz (Pledge / In-Game Kauf)
/// und Flug-Historie (geliehene Schiffe, Free Fly, etc.).
/// </summary>
public partial class ShipFleetItem : ObservableObject
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private string _rawCode = "";

    [ObservableProperty]
    private string _manufacturer = "Unbekannt";

    [ObservableProperty]
    private string _manufacturerBadge = "SC";

    [ObservableProperty]
    private string _manufacturerColor = "#58A6FF";

    [ObservableProperty]
    private string _role = "Mehrzweck / Multi-Role";

    [ObservableProperty]
    private int _flightCount = 1;

    [ObservableProperty]
    private int _quantumJumps = 0;

    [ObservableProperty]
    private int _lossCount = 0;

    [ObservableProperty]
    private long _estimatedValueAuec = 0;

    [ObservableProperty]
    private bool _isInHangar = true;

    [ObservableProperty]
    private bool _isPledgeBought = true;

    [ObservableProperty]
    private int _pledgeValueUsd = 65;

    [ObservableProperty]
    private string _insuranceType = "LTI (Lifetime)";

    [ObservableProperty]
    private string _acquisitionType = "Pledge Store"; // "Pledge Store", "In-Game (aUEC)", "Miete (Rental)", "Geliehen / Free Fly"

    [ObservableProperty]
    private string _customNotes = "";

    [ObservableProperty]
    private DateTime? _lastFlown;

    [ObservableProperty]
    private string _lastLocation = "—";

    [ObservableProperty]
    private bool _isCurrent;

    // Formatierte UI-Eigenschaften
    public string FlightCountText => $"{FlightCount}× geflogen";
    public string QuantumJumpsText => $"{QuantumJumps} QT-Sprünge";
    public string LossCountText => LossCount == 0 ? "0 Verluste" : $"{LossCount}× verloren";
    public string LossCountColor => LossCount == 0 ? "#4ADE80" : "#F87171";

    public string ValueText => EstimatedValueAuec > 0 ? $"{EstimatedValueAuec:N0} aUEC" : "— Nicht gelistet —";

    public string PledgeValueText => AcquisitionType switch
    {
        "Pledge Store" => $"💵 Pledge (${PledgeValueUsd})",
        "In-Game (aUEC)" => "🪙 In-Game Kauf",
        "Miete (Rental)" => "🎟 Miete",
        "Geliehen / Free Fly" => "👥 Geliehen / Free Fly",
        _ => IsPledgeBought ? $"💵 Pledge (${PledgeValueUsd})" : "🪙 In-Game"
    };

    public string PledgeBadgeColor => AcquisitionType switch
    {
        "Pledge Store" => "#F59E0B",
        "In-Game (aUEC)" => "#3FB950",
        "Miete (Rental)" => "#A78BFA",
        _ => "#8B949E"
    };

    public string InsuranceShortBadge
    {
        get
        {
            if (!IsPledgeBought || AcquisitionType != "Pledge Store")
            {
                return "Keine (In-Game)";
            }
            return InsuranceType switch
            {
                "LTI (Lifetime)" or "LTI" => "♾ LTI",
                "120 Monate (IAE)" or "120M" => "🛡 10 Jahre",
                "24 Monate" or "24M" => "24 Monate",
                "12 Monate" or "12M" => "12 Monate",
                _ => "6 Monate"
            };
        }
    }

    public string InsuranceBadgeColor
    {
        get
        {
            if (!IsPledgeBought || AcquisitionType != "Pledge Store")
            {
                return "#374151"; // Dezent dunkelgrau für In-Game Schiffe
            }
            return InsuranceType switch
            {
                "LTI (Lifetime)" or "LTI" => "#F59E0B",      // Gold / Orange
                "120 Monate (IAE)" or "120M" => "#38BDF8",  // Cyan
                "24 Monate" or "24M" => "#A78BFA",          // Lila
                "12 Monate" or "12M" => "#60A5FA",          // Blau
                _ => "#8B949E"                              // Grau (6M Standard)
            };
        }
    }

    public string HangarStatusText => IsInHangar ? "HANGAR" : "GAST";
    public string HangarStarText => IsInHangar ? "★" : "☆";
    public string HangarStarColor => IsInHangar ? "#38BDF8" : "#6E7681";
    public string HangarTooltip => IsInHangar ? "Schiff ist in 'Mein Hangar' (Klicken zum Entfernen)" : "Schiff ist nur in Flug-Historie (Klicken um zu 'Mein Hangar' hinzuzufügen)";

    public string LastFlownText => LastFlown.HasValue
        ? LastFlown.Value.ToString("dd.MM.yyyy HH:mm")
        : "—";

    public string LastFlownShort => LastFlown.HasValue
        ? (DateTime.UtcNow - LastFlown.Value).TotalHours < 24
            ? $"Heute {LastFlown.Value:HH:mm}"
            : LastFlown.Value.ToString("dd.MM. HH:mm")
        : "—";

    public void NotifyPropertiesChanged()
    {
        OnPropertyChanged(nameof(FlightCountText));
        OnPropertyChanged(nameof(QuantumJumpsText));
        OnPropertyChanged(nameof(LossCountText));
        OnPropertyChanged(nameof(LossCountColor));
        OnPropertyChanged(nameof(ValueText));
        OnPropertyChanged(nameof(PledgeValueText));
        OnPropertyChanged(nameof(PledgeBadgeColor));
        OnPropertyChanged(nameof(InsuranceBadgeColor));
        OnPropertyChanged(nameof(InsuranceShortBadge));
        OnPropertyChanged(nameof(HangarStatusText));
        OnPropertyChanged(nameof(HangarStarText));
        OnPropertyChanged(nameof(HangarStarColor));
        OnPropertyChanged(nameof(HangarTooltip));
        OnPropertyChanged(nameof(LastFlownText));
        OnPropertyChanged(nameof(LastFlownShort));
        OnPropertyChanged(nameof(IsInHangar));
    }
}
