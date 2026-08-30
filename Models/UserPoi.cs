using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SCLogReader.Models;

public partial class UserPoi : ObservableObject
{
    [ObservableProperty] private int id;
    [ObservableProperty] private string system = "Stanton"; // Stanton, Pyro, Nyx
    [ObservableProperty] private string body = "";          // Planet / Moon / Station
    [ObservableProperty] private string name = "";          // e.g. "Geheimes Drogenlabor", "Quantainium Vorkommen"
    [ObservableProperty] private string notes = "";         // Freitext-Beschreibung / Koordinaten
    [ObservableProperty] private string category = "Mining"; // Mining, Salvage, Outpost, Secret, Bunker, Trade, Misc
    [ObservableProperty] private string color = "#F59E0B";  // Amber default
    [ObservableProperty] private DateTime createdAt = DateTime.UtcNow;

    public string CreatedAtFormatted => CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
}
