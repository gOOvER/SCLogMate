using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCLogMate.Core;
using SCLogMate.Models;

namespace SCLogMate.ViewModels;

public partial class MainViewModel
{
    // ══ DATENBANK DIAGNOSE & STRUKTURPRÜFUNG ══

    [ObservableProperty]
    private DatabaseDiagnosticsInfo? _dbDiagnostics;

    [ObservableProperty]
    private bool _isCheckingDbStructure;

    [ObservableProperty]
    private string _dbStructureCheckMessage = "";

    [ObservableProperty]
    private bool _hasDbStructureCheckRun;

    partial void OnSettingsSubTabIndexChanged(int value)
    {
        // Sub-Tab 6: Datenbank & Wartung
        if (value == 6 && DbDiagnostics == null && !IsCheckingDbStructure)
        {
            _ = CheckDbStructureAsync();
        }
    }

    /// <summary>
    /// Führt eine detaillierte Integritäts- und Strukturprüfung der lokalen SQLite-Datenbank aus.
    /// </summary>
    [RelayCommand]
    public async Task CheckDbStructureAsync()
    {
        if (IsCheckingDbStructure) return;
        IsCheckingDbStructure = true;
        DbStructureCheckMessage = "Prüfe SQLite-Integrität, Tabellenstruktur, Spalten und Indizes...";

        try
        {
            var diag = await Task.Run(() => Database.GetDiagnostics(runDeepCheck: true));

            Dispatcher.UIThread.Post(() =>
            {
                DbDiagnostics = diag;
                HasDbStructureCheckRun = true;
                DbStructureCheckMessage = diag.StructureSummaryText;
                OnPropertyChanged(nameof(DatabaseSummaryText));
            });
        }
        catch (Exception ex)
        {
            Logger.Error("CheckDbStructureAsync", ex);
            Dispatcher.UIThread.Post(() =>
            {
                DbStructureCheckMessage = $"Fehler bei der Strukturprüfung: {ex.Message}";
            });
        }
        finally
        {
            Dispatcher.UIThread.Post(() =>
            {
                IsCheckingDbStructure = false;
            });
        }
    }

    /// <summary>
    /// Repariert fehlende Strukturen, Tabellen, Spalten oder Indizes und führt ggf. ausstehende Migrationen aus.
    /// </summary>
    [RelayCommand]
    public async Task RepairDbStructureAsync()
    {
        if (IsCheckingDbStructure || IsDatabaseBusy) return;
        IsCheckingDbStructure = true;
        DbStructureCheckMessage = "Repariere Datenbank-Struktur und aktualisiere Schema...";

        try
        {
            var result = await Task.Run(() => Database.RepairOrUpdateStructure());

            var diag = await Task.Run(() => Database.GetDiagnostics(runDeepCheck: true));

            Dispatcher.UIThread.Post(() =>
            {
                DbDiagnostics = diag;
                HasDbStructureCheckRun = true;
                DbStructureCheckMessage = $"{result.message} · {diag.StructureSummaryText}";
                Status = result.success ? "✓ Datenbank-Struktur erfolgreich aktualisiert." : "⚠️ Reparaturhinweis beachten.";
                OnPropertyChanged(nameof(DatabaseSummaryText));
            });
        }
        catch (Exception ex)
        {
            Logger.Error("RepairDbStructureAsync", ex);
            Dispatcher.UIThread.Post(() =>
            {
                DbStructureCheckMessage = $"Fehler bei der Reparatur: {ex.Message}";
            });
        }
        finally
        {
            Dispatcher.UIThread.Post(() =>
            {
                IsCheckingDbStructure = false;
            });
        }
    }

    /// <summary>
    /// Öffnet den Ordner von sessions.db im Windows Explorer und wählt die Datei aus.
    /// </summary>
    [RelayCommand]
    public void OpenDatabaseFileLocation()
    {
        try
        {
            var dbPath = Database.DatabaseFilePath;
            if (File.Exists(dbPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{dbPath}\"",
                    UseShellExecute = true
                });
            }
            else if (Directory.Exists(Settings.Dir))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Settings.Dir,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Error("OpenDatabaseFileLocation", ex);
        }
    }

    /// <summary>
    /// Aktualisiert die Diagnose im Hintergrund (z. B. nach Rescan, Cleanup oder Reset).
    /// </summary>
    public void RefreshDbDiagnosticsInBackground()
    {
        Task.Run(() =>
        {
            try
            {
                var diag = Database.GetDiagnostics(runDeepCheck: false);
                Dispatcher.UIThread.Post(() =>
                {
                    DbDiagnostics = diag;
                    OnPropertyChanged(nameof(DatabaseSummaryText));
                });
            }
            catch (Exception ex)
            {
                Logger.Error("RefreshDbDiagnosticsInBackground", ex);
            }
        });
    }
}
