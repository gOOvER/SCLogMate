using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SCLogMate.Models;

/// <summary>
/// Ein an einem persistenten Planeten- oder Stationsstandort eingelagerter Gegenstand.
/// </summary>
public partial class WarehouseItem : ObservableObject
{
    [ObservableProperty]
    private string _location = "";

    [ObservableProperty]
    private string _locationCode = "";

    [ObservableProperty]
    private string _system = "Stanton";

    [ObservableProperty]
    private string _parentBody = "";

    [ObservableProperty]
    private string _itemClass = "";

    [ObservableProperty]
    private string _itemName = "";

    [ObservableProperty]
    private string _category = "Sonstiges";

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private DateTime _lastUpdated;

    public string FormattedDate => LastUpdated == DateTime.MinValue 
        ? "—" 
        : LastUpdated.ToString("dd.MM.yyyy HH:mm");

    public string Icon => Category switch
    {
        "Waffen" => "🔫",
        "Rüstung" => "🛡️",
        "Werkzeuge" => "🔧",
        "Mineralien & Erze" => "⛏️",
        "Verbrauchsgüter" => "💊",
        "Quest & Utility" => "💾",
        _ => "📦"
    };

    public string LocationDisplay => string.IsNullOrWhiteSpace(ParentBody)
        ? $"{Location} · {System}"
        : $"{Location} · {ParentBody} ({System})";

    public string QuantityBadge => $"{Quantity}×";
}

/// <summary>
/// Zusammenfassung eines Standorts mit Gesamtanzahl der dort eingelagerten Gegenstände.
/// </summary>
public partial class WarehouseLocationGroup : ObservableObject
{
    [ObservableProperty]
    private string _locationName = "";

    [ObservableProperty]
    private string _locationCode = "";

    [ObservableProperty]
    private string _system = "Stanton";

    [ObservableProperty]
    private string _parentBody = "";

    [ObservableProperty]
    private int _totalItems;

    [ObservableProperty]
    private int _uniqueItemTypes;

    public string Icon => LocationName.Contains("Station", StringComparison.OrdinalIgnoreCase) ||
                          LocationName.Contains("LEO", StringComparison.OrdinalIgnoreCase) ||
                          LocationName.Contains("RR_", StringComparison.OrdinalIgnoreCase) ||
                          LocationName.StartsWith("HUR-", StringComparison.OrdinalIgnoreCase) ||
                          LocationName.StartsWith("ARC-", StringComparison.OrdinalIgnoreCase) ||
                          LocationName.StartsWith("MIC-", StringComparison.OrdinalIgnoreCase) ||
                          LocationName.StartsWith("CRU-", StringComparison.OrdinalIgnoreCase)
        ? "🛰️"
        : LocationName.Contains("Mining", StringComparison.OrdinalIgnoreCase) ||
          LocationName.Contains("HDMS", StringComparison.OrdinalIgnoreCase) ||
          LocationName.Contains("Outpost", StringComparison.OrdinalIgnoreCase) ||
          LocationName.Contains("Farm", StringComparison.OrdinalIgnoreCase) ||
          LocationName.Contains("Area0", StringComparison.OrdinalIgnoreCase)
        ? "🏕️"
        : "🪐";

    public string DisplaySubtitle => string.IsNullOrWhiteSpace(ParentBody)
        ? System
        : $"{ParentBody} · {System}";
}
