using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SCLogMate.Models;

public partial class UserPoi : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string system = "Stanton"; // Stanton, Pyro, Nyx
    [ObservableProperty] private string body = "";          // Planet / Moon / Station
    [ObservableProperty] private string name = "";          // e.g. "Geheimes Drogenlabor", "Quantainium Vorkommen"
    [ObservableProperty] private string notes = "";         // Freitext-Beschreibung / Koordinaten
    [ObservableProperty] private string category = "Mining"; // Mining, Salvage, Outpost, Secret, Bunker, Trade, Misc
    [ObservableProperty] private string color = "#F59E0B";  // Amber default
    [ObservableProperty] private double? posX;
    [ObservableProperty] private double? posY;
    [ObservableProperty] private double? posZ;
    [ObservableProperty] private DateTime createdAt = DateTime.UtcNow;

    public bool HasCoordinates => PosX.HasValue && PosY.HasValue && PosZ.HasValue;
    public string CoordinatesFormatted => HasCoordinates ? $"X: {PosX:F1}, Y: {PosY:F1}, Z: {PosZ:F1}" : "—";
    public string CreatedAtFormatted => CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
}
