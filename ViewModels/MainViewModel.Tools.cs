using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCLogMate.Core;
using SCLogMate.Services;

namespace SCLogMate.ViewModels;

public partial class MainViewModel
{
    // ══ TOOLS PROPERTIES ══

    [ObservableProperty] private double _toolsShaderCacheMb;
    [ObservableProperty] private double _toolsCrashDumpsMb;
    [ObservableProperty] private string _toolsStatusMessage = "";
    [ObservableProperty] private bool _isToolsBusy;
    [ObservableProperty] private string? _toolsCloudStoragePath;

    [ObservableProperty] private ObservableCollection<KeybindBackupInfo> _toolsKeybindBackups = new();
    [ObservableProperty] private KeybindBackupInfo? _toolsSelectedKeybindBackup;
    [ObservableProperty] private string _toolsNewKeybindBackupNote = "";

    [ObservableProperty] private ObservableCollection<ConfigBackupInfo> _toolsConfigBackups = new();
    [ObservableProperty] private ConfigBackupInfo? _toolsSelectedConfigBackup;

    [ObservableProperty] private string _toolsUserCfgContent = "";
    [ObservableProperty] private bool _toolsUserCfgExists;
    [ObservableProperty] private string _toolsUserCfgPath = "";

    // user.cfg Quick-Presets
    [ObservableProperty] private bool _cfgConsoleUnlocked = true;
    [ObservableProperty] private int _cfgDisplayInfoLevel = 1;
    [ObservableProperty] private bool _cfgVSyncOff = true;
    [ObservableProperty] private bool _cfgMotionBlurOff = true;
    [ObservableProperty] private int _cfgMaxFps = 160;
    [ObservableProperty] private int _cfgStreamPoolSizeMb = 8192;
    [ObservableProperty] private int _cfgStreamPoolIndex = 1;
    [ObservableProperty] private string _cfgLanguage = "german_(germany)";
    [ObservableProperty] private string _cfgLanguageAudio = "english";

    public string UserCfgStatusText => ToolsUserCfgExists ? "Aktiv (Gefunden)" : "Nicht vorhanden";
    public string UserCfgStatusColor => ToolsUserCfgExists ? "#4ADE80" : "#F87171";
    public string RamStatusColor => ToolsSystemDiag?.RamOk == true ? "#4ADE80" : "#F87171";

    public string CfgVSyncText => CfgVSyncOff ? "Aus" : "Ein";
    public string CfgVSyncColor => CfgVSyncOff ? "#4ADE80" : "#F59E0B";
    public string CfgMotionBlurText => CfgMotionBlurOff ? "Aus" : "Ein";
    public string CfgMotionBlurColor => CfgMotionBlurOff ? "#4ADE80" : "#F59E0B";

    partial void OnCfgVSyncOffChanged(bool value)
    {
        OnPropertyChanged(nameof(CfgVSyncText));
        OnPropertyChanged(nameof(CfgVSyncColor));
    }

    partial void OnCfgMotionBlurOffChanged(bool value)
    {
        OnPropertyChanged(nameof(CfgMotionBlurText));
        OnPropertyChanged(nameof(CfgMotionBlurColor));
    }

    public string ToolsShaderStatusText => ToolsShaderCacheMb > 500 ? "Bereinigung empfohlen" : "Optimal (< 500 MB)";
    public string ToolsShaderStatusColor => ToolsShaderCacheMb > 500 ? "#F59E0B" : "#4ADE80";
    public string ToolsCrashDumpsStatusText => ToolsCrashDumpsMb > 0 ? $"{ToolsCrashDumpsMb:F1} MB belegt" : "0 MB (Sauber)";
    public string ToolsCrashDumpsStatusColor => ToolsCrashDumpsMb > 0 ? "#F87171" : "#4ADE80";

    [ObservableProperty] private int _toolsSubTabIndex;

    private Views.UserCfgEditorWindow? _userCfgWindow;
    private Views.KeybindBackupWindow? _keybindWindow;
    private Views.CloudBackupWindow? _cloudBackupWindow;

    partial void OnToolsShaderCacheMbChanged(double value)
    {
        OnPropertyChanged(nameof(ToolsShaderStatusText));
        OnPropertyChanged(nameof(ToolsShaderStatusColor));
    }

    partial void OnToolsCrashDumpsMbChanged(double value)
    {
        OnPropertyChanged(nameof(ToolsCrashDumpsStatusText));
        OnPropertyChanged(nameof(ToolsCrashDumpsStatusColor));
    }

    partial void OnToolsUserCfgExistsChanged(bool value)
    {
        OnPropertyChanged(nameof(UserCfgStatusText));
        OnPropertyChanged(nameof(UserCfgStatusColor));
    }

    partial void OnToolsSystemDiagChanged(SystemDiagnosticInfo value)
    {
        OnPropertyChanged(nameof(RamStatusColor));
    }

    partial void OnCfgStreamPoolIndexChanged(int value)
    {
        CfgStreamPoolSizeMb = value switch
        {
            0 => 4096,
            2 => 12288,
            _ => 8192
        };
    }

    [ObservableProperty] private SystemDiagnosticInfo _toolsSystemDiag = new();

    // ══ TOOLS INITIALIZATION ══

    public void InitializeTools()
    {
        ToolsCloudStoragePath = _settings?.CloudStoragePath ?? "";
        _ = RefreshToolsDataAsync();
    }

    [RelayCommand]
    public async Task RefreshToolsDataAsync()
    {
        if (IsToolsBusy) return;
        IsToolsBusy = true;

        await Task.Run(() =>
        {
            var shaders = MaintenanceService.GetShaderCacheSizeMb();
            var crashes = MaintenanceService.GetCrashDumpsSizeMb();
            var backups = MaintenanceService.ListKeybindBackups(ToolsCloudStoragePath);
            var cfgBackups = MaintenanceService.ListConfigBackups(ToolsCloudStoragePath);
            var (cfgExists, cfgContent) = MaintenanceService.ReadUserCfg(LogPath);
            var cfgPath = MaintenanceService.GetUserCfgPath(LogPath);
            var diag = MaintenanceService.GetSystemDiagnostics(LogPath);

            Dispatcher.UIThread.Post(() =>
            {
                ToolsShaderCacheMb = shaders;
                ToolsCrashDumpsMb = crashes;
                ToolsKeybindBackups = new ObservableCollection<KeybindBackupInfo>(backups);
                if (ToolsSelectedKeybindBackup == null && ToolsKeybindBackups.Count > 0)
                {
                    ToolsSelectedKeybindBackup = ToolsKeybindBackups[0];
                }

                ToolsConfigBackups = new ObservableCollection<ConfigBackupInfo>(cfgBackups);
                if (ToolsSelectedConfigBackup == null && ToolsConfigBackups.Count > 0)
                {
                    ToolsSelectedConfigBackup = ToolsConfigBackups[0];
                }

                ToolsUserCfgExists = cfgExists;
                ToolsUserCfgContent = cfgContent;
                ToolsUserCfgPath = cfgPath;
                ToolsSystemDiag = diag;

                ParseUserCfgPresets(cfgContent);

                IsToolsBusy = false;
            });
        });
    }

    private void ParseUserCfgPresets(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;

        var mConsole = Regex.Match(content, @"^\s*Con_Restricted\s*=\s*(?<v>\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        if (mConsole.Success) CfgConsoleUnlocked = mConsole.Groups["v"].Value == "0";

        var mDisplay = Regex.Match(content, @"^\s*r_DisplayInfo\s*=\s*(?<v>\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        if (mDisplay.Success && int.TryParse(mDisplay.Groups["v"].Value, out var disp)) CfgDisplayInfoLevel = disp;

        var mVSync = Regex.Match(content, @"^\s*r_VSync\s*=\s*(?<v>\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        if (mVSync.Success) CfgVSyncOff = mVSync.Groups["v"].Value == "0";

        var mMotion = Regex.Match(content, @"^\s*r_MotionBlur\s*=\s*(?<v>\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        if (mMotion.Success) CfgMotionBlurOff = mMotion.Groups["v"].Value == "0";

        var mFps = Regex.Match(content, @"^\s*sys_maxFps\s*=\s*(?<v>\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        if (mFps.Success && int.TryParse(mFps.Groups["v"].Value, out var fps)) CfgMaxFps = fps;

        var mPool = Regex.Match(content, @"^\s*r_TexturesStreamPoolSize\s*=\s*(?<v>\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        if (mPool.Success && int.TryParse(mPool.Groups["v"].Value, out var pool))
        {
            CfgStreamPoolSizeMb = pool;
            CfgStreamPoolIndex = pool <= 4096 ? 0 : pool >= 12288 ? 2 : 1;
        }

        var mLang = Regex.Match(content, @"^\s*g_language\s*=\s*(?<v>[^\r\n]+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        if (mLang.Success) CfgLanguage = mLang.Groups["v"].Value.Trim();

        var mAudio = Regex.Match(content, @"^\s*g_languageAudio\s*=\s*(?<v>[^\r\n]+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        if (mAudio.Success) CfgLanguageAudio = mAudio.Groups["v"].Value.Trim();
    }

    // ══ TOOLS COMMANDS ══

    [RelayCommand]
    public async Task CleanShadersAsync()
    {
        if (IsToolsBusy) return;
        IsToolsBusy = true;
        ToolsStatusMessage = "Bereinige Shader-Cache...";

        await Task.Run(() =>
        {
            var res = MaintenanceService.CleanShaderCache();
            var newSize = MaintenanceService.GetShaderCacheSizeMb();

            Dispatcher.UIThread.Post(() =>
            {
                ToolsShaderCacheMb = newSize;
                ToolsStatusMessage = res.message;
                Status = res.message;
                IsToolsBusy = false;
            });
        });
    }

    [RelayCommand]
    public async Task CleanCrashDumpsAsync()
    {
        if (IsToolsBusy) return;
        IsToolsBusy = true;
        ToolsStatusMessage = "Bereinige Crash-Dumps...";

        await Task.Run(() =>
        {
            var res = MaintenanceService.CleanCrashDumps();
            var newSize = MaintenanceService.GetCrashDumpsSizeMb();

            Dispatcher.UIThread.Post(() =>
            {
                ToolsCrashDumpsMb = newSize;
                ToolsStatusMessage = res.message;
                Status = res.message;
                IsToolsBusy = false;
            });
        });
    }

    [RelayCommand]
    public async Task BackupKeybindsAsync()
    {
        if (IsToolsBusy) return;
        IsToolsBusy = true;
        ToolsStatusMessage = "Sichere Steuerungs-Profile...";

        var note = ToolsNewKeybindBackupNote;
        var cloud = ToolsCloudStoragePath;

        await Task.Run(() =>
        {
            var res = MaintenanceService.BackupKeybinds(LogPath, cloud, note);
            var backups = MaintenanceService.ListKeybindBackups(cloud);

            Dispatcher.UIThread.Post(() =>
            {
                ToolsKeybindBackups = new ObservableCollection<KeybindBackupInfo>(backups);
                if (ToolsKeybindBackups.Count > 0) ToolsSelectedKeybindBackup = ToolsKeybindBackups[0];
                ToolsNewKeybindBackupNote = "";
                ToolsStatusMessage = res.message;
                Status = res.message;
                IsToolsBusy = false;
            });
        });
    }

    [RelayCommand]
    public async Task RestoreSelectedKeybindAsync()
    {
        if (ToolsSelectedKeybindBackup == null)
        {
            ToolsStatusMessage = "Bitte wähle ein Steuerungs-Backup aus der Liste aus.";
            return;
        }

        if (IsToolsBusy) return;
        IsToolsBusy = true;
        var backup = ToolsSelectedKeybindBackup;
        ToolsStatusMessage = $"Stelle Backup '{backup.Name}' wieder her...";

        await Task.Run(() =>
        {
            var res = MaintenanceService.RestoreKeybinds(backup, LogPath);

            Dispatcher.UIThread.Post(() =>
            {
                ToolsStatusMessage = res.message;
                Status = res.message;
                IsToolsBusy = false;
            });
        });
    }

    [RelayCommand]
    public async Task BrowseCloudStoragePathAsync()
    {
        var tl = UiServices.TopLevel;
        if (tl is null) return;

        IStorageFolder? start = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(ToolsCloudStoragePath) && Directory.Exists(ToolsCloudStoragePath))
            {
                start = await tl.StorageProvider.TryGetFolderFromPathAsync(ToolsCloudStoragePath);
            }
            else
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (Directory.Exists(userProfile))
                {
                    start = await tl.StorageProvider.TryGetFolderFromPathAsync(userProfile);
                }
            }
        }
        catch { /* ignore */ }

        var folders = await tl.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Cloud-Stammverzeichnis auswählen (OneDrive, Dropbox, Nextcloud...)",
            AllowMultiple = false,
            SuggestedStartLocation = start
        });

        var picked = folders?.FirstOrDefault()?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(picked)) return;

        ToolsCloudStoragePath = picked;
        await SaveCloudStoragePathAsync();
    }

    [RelayCommand]
    public async Task SaveCloudStoragePathAsync()
    {
        _settings.CloudStoragePath = string.IsNullOrWhiteSpace(ToolsCloudStoragePath) ? null : ToolsCloudStoragePath.Trim();
        Settings.Save(_settings);
        ToolsStatusMessage = string.IsNullOrWhiteSpace(_settings.CloudStoragePath) 
            ? "Cloud-Speicherpfad deaktiviert." 
            : $"✓ Cloud-Speicherpfad gespeichert: {_settings.CloudStoragePath}";
        
        await RefreshToolsDataAsync();
    }

    [RelayCommand]
    public async Task ExportLogsZipAsync()
    {
        if (IsToolsBusy) return;
        IsToolsBusy = true;
        ToolsStatusMessage = "Erstelle Log-Archiv ZIP...";

        // Zielordner: bevorzugt Desktop oder Downloads
        var destFolder = !string.IsNullOrWhiteSpace(ToolsCloudStoragePath) && Directory.Exists(ToolsCloudStoragePath)
            ? Path.Combine(ToolsCloudStoragePath, "SCLogMate", "Logs")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));

        await Task.Run(() =>
        {
            var res = MaintenanceService.ExportLogsToZip(destFolder, LogPath);

            Dispatcher.UIThread.Post(() =>
            {
                ToolsStatusMessage = res.message;
                Status = res.message;
                IsToolsBusy = false;
            });
        });
    }

    [RelayCommand]
    public async Task SyncLogsCloudAsync()
    {
        if (string.IsNullOrWhiteSpace(ToolsCloudStoragePath))
        {
            ToolsStatusMessage = "Bitte gib zuerst einen gültigen Cloud-Speicherpfad an.";
            return;
        }

        if (IsToolsBusy) return;
        IsToolsBusy = true;
        ToolsStatusMessage = "Synchronisiere Logs mit der Cloud...";

        await Task.Run(() =>
        {
            var res = MaintenanceService.SyncLogsToCloud(ToolsCloudStoragePath, LogPath);

            Dispatcher.UIThread.Post(() =>
            {
                ToolsStatusMessage = res.message;
                Status = res.message;
                IsToolsBusy = false;
            });
        });
    }

    [RelayCommand]
    public void ApplyCfgPresetsToText()
    {
        var sb = new StringBuilder();
        if (CfgConsoleUnlocked) sb.AppendLine("Con_Restricted = 0");
        sb.AppendLine($"r_DisplayInfo = {CfgDisplayInfoLevel}");
        sb.AppendLine($"r_VSync = {(CfgVSyncOff ? 0 : 1)}");
        sb.AppendLine($"r_MotionBlur = {(CfgMotionBlurOff ? 0 : 1)}");
        if (CfgMaxFps > 0) sb.AppendLine($"sys_maxFps = {CfgMaxFps}");
        if (CfgStreamPoolSizeMb > 0) sb.AppendLine($"r_TexturesStreamPoolSize = {CfgStreamPoolSizeMb}");
        if (!string.IsNullOrWhiteSpace(CfgLanguage)) sb.AppendLine($"g_language = {CfgLanguage.Trim()}");
        if (!string.IsNullOrWhiteSpace(CfgLanguageAudio)) sb.AppendLine($"g_languageAudio = {CfgLanguageAudio.Trim()}");

        ToolsUserCfgContent = sb.ToString();
        ToolsStatusMessage = "Vorlagen in Editor übernommen. Klicke auf 'user.cfg speichern', um sie anzuwenden.";
    }

    [RelayCommand]
    public async Task SaveUserCfgAsync()
    {
        if (IsToolsBusy) return;
        IsToolsBusy = true;
        ToolsStatusMessage = "Speichere user.cfg...";

        var content = ToolsUserCfgContent;
        var cloud = ToolsCloudStoragePath;
        await Task.Run(() =>
        {
            var res = MaintenanceService.SaveUserCfg(LogPath, content, cloud);
            var (exists, updatedContent) = MaintenanceService.ReadUserCfg(LogPath);
            var cfgBackups = MaintenanceService.ListConfigBackups(cloud);

            Dispatcher.UIThread.Post(() =>
            {
                ToolsUserCfgExists = exists;
                ToolsUserCfgContent = updatedContent;
                ToolsConfigBackups = new ObservableCollection<ConfigBackupInfo>(cfgBackups);
                if (ToolsConfigBackups.Count > 0) ToolsSelectedConfigBackup = ToolsConfigBackups[0];
                ToolsStatusMessage = res.message;
                Status = res.message;
                IsToolsBusy = false;
            });
        });
    }

    [RelayCommand]
    public async Task BackupUserCfgAsync()
    {
        if (IsToolsBusy) return;
        IsToolsBusy = true;
        ToolsStatusMessage = "Archiviere user.cfg...";

        var cloud = ToolsCloudStoragePath;
        await Task.Run(() =>
        {
            var res = MaintenanceService.BackupUserCfg(LogPath, cloud, "Manuell");
            var cfgBackups = MaintenanceService.ListConfigBackups(cloud);

            Dispatcher.UIThread.Post(() =>
            {
                ToolsConfigBackups = new ObservableCollection<ConfigBackupInfo>(cfgBackups);
                if (ToolsConfigBackups.Count > 0) ToolsSelectedConfigBackup = ToolsConfigBackups[0];
                ToolsStatusMessage = res.message;
                Status = res.message;
                IsToolsBusy = false;
            });
        });
    }

    [RelayCommand]
    public async Task RestoreSelectedConfigBackupAsync()
    {
        if (ToolsSelectedConfigBackup == null)
        {
            ToolsStatusMessage = "Bitte wähle ein user.cfg-Backup aus der Liste aus.";
            return;
        }

        if (IsToolsBusy) return;
        IsToolsBusy = true;
        var backup = ToolsSelectedConfigBackup;
        ToolsStatusMessage = $"Stelle user.cfg aus '{backup.Name}' wieder her...";

        await Task.Run(() =>
        {
            var res = MaintenanceService.RestoreConfigBackup(backup, LogPath);
            var (exists, updatedContent) = MaintenanceService.ReadUserCfg(LogPath);

            Dispatcher.UIThread.Post(() =>
            {
                ToolsUserCfgExists = exists;
                ToolsUserCfgContent = updatedContent;
                ParseUserCfgPresets(updatedContent);
                ToolsStatusMessage = res.message;
                Status = res.message;
                IsToolsBusy = false;
            });
        });
    }

    [RelayCommand]
    public void OpenUserCfgPopout()
    {
        if (_userCfgWindow != null && _userCfgWindow.IsVisible)
        {
            _userCfgWindow.Activate();
            return;
        }

        _userCfgWindow = new Views.UserCfgEditorWindow(this);
        _userCfgWindow.Closed += (_, _) => _userCfgWindow = null;
        _userCfgWindow.Show();
    }

    [RelayCommand]
    public void OpenKeybindPopout()
    {
        if (_keybindWindow != null && _keybindWindow.IsVisible)
        {
            _keybindWindow.Activate();
            return;
        }

        _keybindWindow = new Views.KeybindBackupWindow(this);
        _keybindWindow.Closed += (_, _) => _keybindWindow = null;
        _keybindWindow.Show();
    }

    [RelayCommand]
    public void OpenCloudPopout()
    {
        if (_cloudBackupWindow != null && _cloudBackupWindow.IsVisible)
        {
            _cloudBackupWindow.Activate();
            return;
        }

        _cloudBackupWindow = new Views.CloudBackupWindow(this);
        _cloudBackupWindow.Closed += (_, _) => _cloudBackupWindow = null;
        _cloudBackupWindow.Show();
    }

    [RelayCommand]
    public void ApplyPresetHighFps()
    {
        CfgConsoleUnlocked = true;
        CfgVSyncOff = true;
        CfgMotionBlurOff = true;
        CfgDisplayInfoLevel = 1;
        CfgMaxFps = 165;
        CfgStreamPoolIndex = 1; // 8192 MB
        ApplyCfgPresetsToText();
        ToolsStatusMessage = "⚡ Profil 'High FPS / E-Sport' angewendet (in Texteditor übertragen).";
    }

    [RelayCommand]
    public void ApplyPresetQuality()
    {
        CfgConsoleUnlocked = true;
        CfgVSyncOff = false;
        CfgMotionBlurOff = true;
        CfgDisplayInfoLevel = 0;
        CfgMaxFps = 0; // unbegrenzt
        CfgStreamPoolIndex = 2; // 12288 MB
        ApplyCfgPresetsToText();
        ToolsStatusMessage = "🎨 Profil 'Grafik & Immersion' angewendet (in Texteditor übertragen).";
    }

    [RelayCommand]
    public void ApplyPresetLowEnd()
    {
        CfgConsoleUnlocked = true;
        CfgVSyncOff = true;
        CfgMotionBlurOff = true;
        CfgDisplayInfoLevel = 1;
        CfgMaxFps = 60;
        CfgStreamPoolIndex = 0; // 4096 MB
        ApplyCfgPresetsToText();
        ToolsStatusMessage = "💻 Profil 'Minimal / Einsteiger-PC' angewendet (in Texteditor übertragen).";
    }

    [RelayCommand]
    public void LoadSelectedConfigBackupIntoEditor()
    {
        if (ToolsSelectedConfigBackup == null || !File.Exists(ToolsSelectedConfigBackup.FilePath))
        {
            ToolsStatusMessage = "Bitte zuerst ein gültiges Backup aus der Liste auswählen.";
            return;
        }

        try
        {
            var text = File.ReadAllText(ToolsSelectedConfigBackup.FilePath, Encoding.UTF8);
            ToolsUserCfgContent = text;
            ParseUserCfgPresets(text);
            ToolsStatusMessage = $"✓ Backup '{ToolsSelectedConfigBackup.Name}' zur Vorschau/Bearbeitung in Editor geladen.";
        }
        catch (Exception ex)
        {
            Logger.Error("LoadSelectedConfigBackupIntoEditor", ex);
            ToolsStatusMessage = $"Fehler beim Laden des Backups: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task ReloadUserCfgFromDiskAsync()
    {
        if (IsToolsBusy) return;
        IsToolsBusy = true;
        ToolsStatusMessage = "Lade aktuelle user.cfg von Festplatte neu...";

        await Task.Run(() =>
        {
            var (exists, updatedContent) = MaintenanceService.ReadUserCfg(LogPath);

            Dispatcher.UIThread.Post(() =>
            {
                ToolsUserCfgExists = exists;
                ToolsUserCfgContent = updatedContent;
                ParseUserCfgPresets(updatedContent);
                ToolsStatusMessage = exists 
                    ? "✓ user.cfg erfolgreich von Festplatte neu eingelesen."
                    : "Keine user.cfg im Star Citizen LIVE Ordner gefunden.";
                IsToolsBusy = false;
            });
        });
    }

    // ══ ORDNER-SCHNELLZUGRIFF ══

    [RelayCommand]
    public void OpenLiveFolder()
    {
        if (!string.IsNullOrEmpty(LogPath))
        {
            var dir = Path.GetDirectoryName(LogPath);
            OpenFolderInExplorer(dir);
        }
    }

    [RelayCommand]
    public void OpenLogbackupsFolder()
    {
        if (!string.IsNullOrEmpty(LogPath))
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrEmpty(dir))
            {
                var backups = Path.Combine(dir, "logbackups");
                OpenFolderInExplorer(backups);
            }
        }
    }

    [RelayCommand]
    public void OpenStarCitizenLocalAppDataFolder()
    {
        OpenFolderInExplorer(MaintenanceService.GetStarCitizenLocalAppDataDir());
    }

    [RelayCommand]
    public void OpenSCLogMateAppDataFolder()
    {
        OpenFolderInExplorer(Settings.Dir);
    }

    [RelayCommand]
    public void OpenCloudFolder()
    {
        if (!string.IsNullOrWhiteSpace(ToolsCloudStoragePath))
        {
            OpenFolderInExplorer(ToolsCloudStoragePath);
        }
    }

    private static void OpenFolderInExplorer(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"OpenFolderInExplorer {path}", ex);
        }
    }
}
