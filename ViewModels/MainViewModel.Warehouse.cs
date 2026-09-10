using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCLogMate.Core;
using SCLogMate.Models;

namespace SCLogMate.ViewModels;

public partial class MainViewModel
{
    // ═════════════════════════════════════════════════════════════════════════
    // 📦 LAGER & PLANETEN-INVENTAR (WAREHOUSE INVENTORY)
    // ═════════════════════════════════════════════════════════════════════════

    [ObservableProperty]
    private ObservableCollection<WarehouseLocationGroup> _warehouseLocations = new();

    [ObservableProperty]
    private ObservableCollection<WarehouseItem> _filteredWarehouseItems = new();

    [ObservableProperty]
    private WarehouseItem? _selectedWarehouseItem;

    private List<WarehouseItem> _allWarehouseItems = new();

    [ObservableProperty]
    private WarehouseLocationGroup? _selectedWarehouseLocation;

    [ObservableProperty]
    private string _selectedWarehouseCategory = "Alle Kategorien";

    [ObservableProperty]
    private string _warehouseSearchText = "";

    [ObservableProperty]
    private int _totalWarehouseQuantity;

    [ObservableProperty]
    private int _totalWarehouseUniqueItems;

    [ObservableProperty]
    private int _totalWarehouseLocationsCount;

    [ObservableProperty]
    private bool _isLoadingWarehouse;

    partial void OnWarehouseSearchTextChanged(string value) => ApplyWarehouseFilter();
    partial void OnSelectedWarehouseCategoryChanged(string value) => ApplyWarehouseFilter();
    partial void OnSelectedWarehouseLocationChanged(WarehouseLocationGroup? value) => ApplyWarehouseFilter();

    private bool _wikiItemResolvedSubscribed;

    private void EnsureWikiResolvedHook()
    {
        if (_wikiItemResolvedSubscribed) return;
        _wikiItemResolvedSubscribed = true;
        WikiApiClient.ItemResolved += (className, info) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                bool updated = false;
                foreach (var item in _allWarehouseItems)
                {
                    if (string.Equals(item.ItemClass, className, StringComparison.OrdinalIgnoreCase))
                    {
                        item.ItemName = info.Name;
                        item.Category = info.Category;
                        updated = true;
                    }
                }
                if (updated)
                {
                    ApplyWarehouseFilter();
                }
            });
        };
    }

    [RelayCommand]
    public async Task OpenWikiForWarehouseItem(WarehouseItem? item)
    {
        if (item == null) return;
        var query = !string.IsNullOrWhiteSpace(item.ItemClass) ? item.ItemClass : item.ItemName;
        await OpenWiki(query);
    }

    public void LoadWarehouseData()
    {
        EnsureWikiResolvedHook();
        IsLoadingWarehouse = true;
        Task.Run(() =>
        {
            try
            {
                var locations = Database.GetWarehouseLocationsSummary();
                var items = Database.GetWarehouseItems();

                Dispatcher.UIThread.Post(() =>
                {
                    _allWarehouseItems = items;
                    WarehouseLocations = new ObservableCollection<WarehouseLocationGroup>(locations);
                    TotalWarehouseQuantity = items.Sum(i => i.Quantity);
                    TotalWarehouseUniqueItems = items.Select(i => i.ItemClass).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                    TotalWarehouseLocationsCount = locations.Count;
                    ApplyWarehouseFilter();
                    IsLoadingWarehouse = false;
                });
            }
            catch (Exception ex)
            {
                Logger.Error("LoadWarehouseData", ex);
                Dispatcher.UIThread.Post(() => IsLoadingWarehouse = false);
            }
        });
    }

    public void ApplyWarehouseFilter()
    {
        var query = _allWarehouseItems.AsEnumerable();

        if (SelectedWarehouseLocation != null && !string.IsNullOrWhiteSpace(SelectedWarehouseLocation.LocationName))
        {
            query = query.Where(i => string.Equals(i.Location, SelectedWarehouseLocation.LocationName, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SelectedWarehouseCategory) && SelectedWarehouseCategory != "Alle Kategorien")
        {
            query = query.Where(i => string.Equals(i.Category, SelectedWarehouseCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(WarehouseSearchText))
        {
            var s = WarehouseSearchText.Trim();
            query = query.Where(i =>
                i.ItemName.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                i.ItemClass.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                i.Location.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                i.Category.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                i.ParentBody.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                i.System.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        FilteredWarehouseItems = new ObservableCollection<WarehouseItem>(
            query.OrderBy(i => i.Location)
                 .ThenBy(i => i.Category)
                 .ThenBy(i => i.ItemName));
    }

    [RelayCommand]
    public void SelectWarehouseLocation(WarehouseLocationGroup? group)
    {
        if (SelectedWarehouseLocation == group)
        {
            SelectedWarehouseLocation = null; // Toggle/Abwählen
        }
        else
        {
            SelectedWarehouseLocation = group;
        }
    }

    [RelayCommand]
    public void SelectWarehouseCategory(string category)
    {
        SelectedWarehouseCategory = category;
    }

    [RelayCommand]
    public void ClearWarehouseFilter()
    {
        SelectedWarehouseLocation = null;
        SelectedWarehouseCategory = "Alle Kategorien";
        WarehouseSearchText = "";
    }

    [RelayCommand]
    public void DecreaseWarehouseItem(WarehouseItem? item)
    {
        if (item == null) return;
        Database.AdjustWarehouseItemQuantity(item.Location, item.ItemClass, -1);
        Status = $"📦 {item.ItemName} in {item.Location}: Menge um 1 verringert";
        LoadWarehouseData();
    }

    [RelayCommand]
    public void IncreaseWarehouseItem(WarehouseItem? item)
    {
        if (item == null) return;
        Database.AdjustWarehouseItemQuantity(item.Location, item.ItemClass, +1);
        Status = $"📦 {item.ItemName} in {item.Location}: Menge um 1 erhöht";
        LoadWarehouseData();
    }

    [RelayCommand]
    public void DismantleWarehouseItem(WarehouseItem? item)
    {
        if (item == null) return;
        Database.AdjustWarehouseItemQuantity(item.Location, item.ItemClass, -1);
        Status = $"🔧 {item.ItemName} in {item.Location} als zerlegt (Dismantled) verbucht (-1)";
        LoadWarehouseData();
    }

    [RelayCommand]
    public void DeleteWarehouseItem(WarehouseItem? item)
    {
        if (item == null) return;
        Database.DeleteWarehouseItem(item.Location, item.ItemClass);
        Status = $"🗑️ {item.ItemName} aus dem Lagerbestand von {item.Location} entfernt";
        LoadWarehouseData();
    }

    [RelayCommand]
    public void ClearSelectedLocationWarehouse()
    {
        if (SelectedWarehouseLocation == null || string.IsNullOrWhiteSpace(SelectedWarehouseLocation.LocationName) || SelectedWarehouseLocation.LocationName == "Alle Standorte")
        {
            Status = "ℹ Bitte zuerst einen bestimmten Standort in der Liste links auswählen.";
            return;
        }
        var locName = SelectedWarehouseLocation.LocationName;
        Database.ClearWarehouseLocation(locName);
        Status = $"🗑️ Lagerbestand für Standort '{locName}' vollständig geleert";
        LoadWarehouseData();
    }

    [RelayCommand]
    public void RefreshWarehouse()
    {
        LoadWarehouseData();
        Status = "✓ Lagerbestände der Planeten & Stationen neu geladen";
    }

    [RelayCommand]
    public async Task ExportWarehouseMarkdown()
    {
        try
        {
            var defaultFileName = $"Lagerbestand_{DateTime.Now:yyyy-MM-dd_HHmm}.md";
            var path = await PickSaveAsync(defaultFileName, "Markdown Dokument", "md");
            if (path == null) return;

            var sb = new StringBuilder();
            sb.AppendLine("# 📦 Star Citizen Lagerbestand (Planeten & Stationen)");
            sb.AppendLine();
            sb.AppendLine($"- **Exportiert am:** {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine($"- **Gesamte Gegenstände:** {TotalWarehouseQuantity:N0}");
            sb.AppendLine($"- **Verschiedene Item-Typen:** {TotalWarehouseUniqueItems}");
            sb.AppendLine($"- **Bekannte Standorte:** {TotalWarehouseLocationsCount}");
            sb.AppendLine();

            var grouped = FilteredWarehouseItems
                .GroupBy(i => i.LocationDisplay)
                .OrderBy(g => g.Key);

            foreach (var group in grouped)
            {
                sb.AppendLine($"## 🪐 {group.Key}");
                sb.AppendLine();
                sb.AppendLine("| Kategorie | Gegenstand | Menge | Letzte Ein-/Auslagerung |");
                sb.AppendLine("|---|---|:---:|---|");

                foreach (var item in group.OrderBy(i => i.Category).ThenBy(i => i.ItemName))
                {
                    sb.AppendLine($"| {item.Icon} {item.Category} | **{item.ItemName}** (`{item.ItemClass}`) | **{item.Quantity}** | {item.FormattedDate} |");
                }
                sb.AppendLine();
            }

            await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(false));
            Status = "✓ Lagerbestand als Markdown gespeichert: " + path;
        }
        catch (Exception ex)
        {
            Logger.Error("ExportWarehouseMarkdown", ex);
            Status = "Fehler beim Speichern des Lagerbestands";
        }
    }
}
