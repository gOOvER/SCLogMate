using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SCLogMate.Models;

public enum LoadoutSlotType
{
    Helmet,
    Torso,
    Arms,
    Legs,
    Undersuit,
    Backpack,
    Primary1,
    Primary2,
    Sidearm,
    MultiTool,
    MedItem
}

public partial class LoadoutItem : ObservableObject
{
    [ObservableProperty] private LoadoutSlotType slot;
    [ObservableProperty] private string slotName = "";
    [ObservableProperty] private string itemName = "—";
    [ObservableProperty] private string rawClass = "";
    [ObservableProperty] private string icon = "🛡️";
    [ObservableProperty] private DateTime? lastObserved;

    [ObservableProperty] private string armorClass = "";
    [ObservableProperty] private int damageReductionPercent = 0;
    [ObservableProperty] private string damageReductionText = "";
    [ObservableProperty] private string temperatureMinMaxText = "";
    [ObservableProperty] private string attachmentsText = "";
    [ObservableProperty] private string backpackCapacityText = "";
    [ObservableProperty] private string badgeColor = "#38BDF8";

    public string LastObservedFormatted => LastObserved.HasValue 
        ? LastObserved.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss")
        : "Nie gesehen";

    public string LastObservedText => LastObservedFormatted;

    public bool IsEquipped => !string.IsNullOrWhiteSpace(ItemName) && ItemName != "—";
}
