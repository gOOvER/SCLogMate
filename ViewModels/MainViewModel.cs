using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Media;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCLogReader.Core;
using SCLogReader.Core.Ocr;
using SCLogReader.Models;
using SCLogReader.Services;
using SCLogReader.Views;

namespace SCLogReader.ViewModels;

public partial class MainViewModel : ObservableObject
{
    LogTailer? _tailer;
    LogParser _parser = new();
    readonly OcrEngineService _ocrEngine = new();
    readonly WalletCapture _walletCapture;
    readonly ContractScanner _contractScanner;
    readonly ScanIndicatorWindow _scanIndicator = new();
    bool _initializing = true;
    bool _ready;          // Persistenz erst nach Konstruktor
    bool _suppressSave;   // Session-Wechsel nicht als Default speichern
    DateTime? _sessionStart, _sessionEnd;
    AppSettings _settings = Settings.Load();

    [ObservableProperty] private string logPath = "";
    [ObservableProperty] private string status = "bereit";
    [ObservableProperty] private string manualBalance = "";   // echter Kontostand (Eingabe)
    [ObservableProperty] private bool balanceSaved;            // kurzes „✓ Gespeichert"-Signal
    [ObservableProperty] private bool isEditingManualBalance;  // manuelle Eingabezeile einblenden
    [ObservableProperty] private bool autoOcrEnabled;          // automatischer mobiGlas-Scan (Standard: aus)
    [ObservableProperty] private bool showScanBox;            // Scan-Rahmen im Spiel anzeigen
    [ObservableProperty] private bool ocrAvailable;
    [ObservableProperty] private string ocrStatusText = "OCR bereit";
    [ObservableProperty] private string ocrRegionText = "Standard (Auto)";
    [ObservableProperty] private string contractRegionText = "Nicht kalibriert";
    [ObservableProperty] private string activeContractTitle = "— Kein aktiver Auftrag —";
    [ObservableProperty] private string activeContractRewardText = "—";
    [ObservableProperty] private string activeContractOrg = "";
    [ObservableProperty] private bool hasActiveContract;
    [ObservableProperty] private string contractStatusText = "Wartet auf mobiGlas Accepted-Tab…";
    public string ContractRegionTooltip => _settings.ContractRegion is { } r
        ? $"Auftrags-Bereich: {r.Width}x{r.Height} @ ({r.X},{r.Y})"
        : "Auftrags-Bereich nicht kalibriert (⊕ Bereich klicken)";

    [ObservableProperty] private bool isGameRunning;
    [ObservableProperty] private string gameStatusText = "⚪ SC STANDBY";
    [ObservableProperty] private string gameStatusColor = "#8B949E";
    [ObservableProperty] private string gameStatusBadgeBg = "#161B22";
    [ObservableProperty] private string gameStatusTooltip = "Star Citizen ist derzeit nicht gestartet · Parser wartet auf Spielstart";

    public ObservableCollection<ContractDetails> ActiveContracts { get; } = new();
    public bool HasActiveContracts => ActiveContracts.Count > 0;
    public string ActiveContractsCountText => ActiveContracts.Count == 1 ? "1 aktiver Auftrag" : $"{ActiveContracts.Count} aktive Aufträge";
    [ObservableProperty] private bool updateAvailable;
    [ObservableProperty] private string updateText = "";
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> _knownContracts = new(StringComparer.OrdinalIgnoreCase);
    Updater.Info? _update;
    Avalonia.Threading.DispatcherTimer? _updateTimer;
    Avalonia.Threading.DispatcherTimer? _pingTimer;
    Avalonia.Threading.DispatcherTimer? _processTimer;
    [ObservableProperty] private SessionInfo? selectedSession;
    [ObservableProperty] private string currentLocation = "—";
    [ObservableProperty] private string currentShip = "—";
    [ObservableProperty] private string lastInventory = "—";

    [ObservableProperty] private ResolvedLocation resolvedLocation = new();
    public string LocationSystemBadge => ResolvedLocation.SystemName.ToUpperInvariant();
    public string LocationBadgeColor => ResolvedLocation.SystemBadgeColor;
    public string LocationMainText => ResolvedLocation.DisplayName == "—" ? "—" : 
        string.IsNullOrEmpty(ResolvedLocation.ParentBody) || ResolvedLocation.ParentBody == "—" || ResolvedLocation.ParentBody == ResolvedLocation.DisplayName
            ? ResolvedLocation.DisplayName 
            : $"{ResolvedLocation.DisplayName} · {ResolvedLocation.ParentBody}";
    public string LocationStatusSubline => ResolvedLocation.DisplayName == "—" 
        ? "Warte auf Log-Daten..." 
        : $"{ResolvedLocation.ArmisticeStatusText} · {ResolvedLocation.SystemName}";

    // Starmap-Properties
    [ObservableProperty] private string selectedStarmapSystem = "Stanton";
    [ObservableProperty] private StarmapObject? selectedStarmapObject;
    [ObservableProperty] private string starmapSearchText = "";
    [ObservableProperty] private bool showStarmapStations = true;
    [ObservableProperty] private bool showStarmapMoons = true;
    [ObservableProperty] private bool showStarmapLandingZones = true;
    [ObservableProperty] private bool showStarmapJumpPoints = true;
    [ObservableProperty] private int selectedTabIndex = 0;

    // Star Citizen Wiki API Integration
    [ObservableProperty] private WikiInfo? currentShipWiki;
    [ObservableProperty] private WikiInfo? selectedWikiInfo;
    [ObservableProperty] private Avalonia.Media.Imaging.Bitmap? wikiImageBitmap;
    [ObservableProperty] private bool isWikiOverlayOpen;
    [ObservableProperty] private bool isWikiLoading;

    // UEX API 2.0 Integration
    [ObservableProperty] private UexLocationInfo? selectedStarmapUexInfo;
    [ObservableProperty] private string uexApiKeyInput = "";
    [ObservableProperty] private string uexStatusMessage = "";
    [ObservableProperty] private string uexStatusColor = "#4ADE80";

    // In-Game Floating HUD Overlay
    [ObservableProperty] private bool isOverlayActive;
    [ObservableProperty] private double overlayOpacity = 0.92;
    private Views.FloatingOverlayWindow? _overlayWindow;

    // WoW-Style In-Game Achievement & Reward Toast Banner (Völlig unabhängig vom Mini-HUD)
    [ObservableProperty] private bool toastOverlayEnabled = true;
    private Views.AchievementToastWindow? _toastWindow;

    // Fenster-Verhalten
    [ObservableProperty] private bool minimizeToTrayOnClose = true;

    // Bauplan-Datenbank (Crafting Blueprints)
    public ObservableCollection<BlueprintItem> BlueprintCatalogList { get; } = new();
    public DataGridCollectionView BlueprintsView { get; }
    [ObservableProperty] private string blueprintSearchText = "";
    [ObservableProperty] private string selectedBlueprintCategory = "Alle";

    public int LearnedBlueprintsCount => BlueprintCatalogList.Count(b => b.IsLearned);
    public int MissingBlueprintsCount => BlueprintCatalogList.Count(b => !b.IsLearned);
    public int TotalBlueprintsCount => BlueprintCatalogList.Count;
    public double BlueprintProgressPercent => TotalBlueprintsCount > 0 ? (double)LearnedBlueprintsCount / TotalBlueprintsCount * 100.0 : 0;
    public string BlueprintProgressText => $"{LearnedBlueprintsCount:N0} von {TotalBlueprintsCount:N0} Bauplänen erlernt ({BlueprintProgressPercent:F1}%)";

    // Missions-Datenbank (Master Catalog & In-Game Wiki)
    public ObservableCollection<MissionInfo> MissionCatalogList { get; } = new();
    public DataGridCollectionView MissionsView { get; }
    [ObservableProperty] private string missionSearchText = "";
    [ObservableProperty] private string selectedMissionType = "Alle";
    [ObservableProperty] private MissionInfo? selectedMission;
    public int TotalMissionsCount => MissionCatalogList.Count;

    public IReadOnlyList<StarmapObject> CurrentSystemObjects => StarmapData.GetSystemObjects(SelectedStarmapSystem);
    public IReadOnlyList<StarmapObject> SearchStarmapResults => string.IsNullOrWhiteSpace(StarmapSearchText)
        ? CurrentSystemObjects.Where(o => o.Type != StarmapObjectType.Star).Take(14).ToList()
        : StarmapData.GetSystemObjects(SelectedStarmapSystem)
            .Where(o => o.Name.Contains(StarmapSearchText, StringComparison.OrdinalIgnoreCase) || o.Description.Contains(StarmapSearchText, StringComparison.OrdinalIgnoreCase))
            .ToList();

    [ObservableProperty] private long totalIn;        // Transfers rein
    [ObservableProperty] private long totalReward;    // Missions-Belohnungen
    [ObservableProperty] private long totalOut;       // Transfers raus
    [ObservableProperty] private long totalPurchases; // Käufe
    [ObservableProperty] private long totalSales;     // Item-Verkäufe
    [ObservableProperty] private long totalTrade;     // Fracht/Waren-Verkäufe
    [ObservableProperty] private int missionsDone;    // abgeschlossene Aufträge (Belohnung nicht im Log)

    [ObservableProperty] private bool running;
    [ObservableProperty] private bool isDatabaseBusy;
    [ObservableProperty] private string databaseStatusMessage = "";
    [ObservableProperty] private double databaseProgressPercent;
    [ObservableProperty] private long? serverPingMs;
    [ObservableProperty] private bool isPingingServer;

    public Core.BulkObservableCollection<LogEntry> Events { get; } = new();
    public ObservableCollection<SessionInfo> Sessions { get; } = new();

    // Gefilterte Ansicht für das DataGrid (Filter-Chips + Spalten-Sortierung)
    public DataGridCollectionView EventsView { get; }

    [ObservableProperty] private string activeFilter = "Alle";
    HashSet<EventKind>? _activeKinds;

    static readonly Dictionary<string, HashSet<EventKind>?> FilterMap = new()
    {
        ["Alle"] = null,
        ["Geld"] = new() { EventKind.TransferIn, EventKind.TransferOut, EventKind.MissionReward,
                           EventKind.Purchase, EventKind.Sale, EventKind.Trade, EventKind.Offer, EventKind.Fine },
        ["Aufträge"] = new() { EventKind.Mission, EventKind.MissionDone, EventKind.MissionTaken },
        ["Baupläne"] = new() { EventKind.Blueprint },
        ["Schiffe"] = new() { EventKind.Vehicle, EventKind.Quantum, EventKind.ShipLoss },
        ["Orte"] = new() { EventKind.Location, EventKind.Jurisdiction, EventKind.Hangar },
        ["Crew"] = new() { EventKind.Party, EventKind.Friend },
        ["Loot"] = new() { EventKind.Loot },
        ["Sonst"] = new() { EventKind.MedBed, EventKind.Death, EventKind.Impound,
                            EventKind.Loadout, EventKind.Entitlement, EventKind.Inventory, EventKind.Gear, EventKind.Kill,
                            EventKind.Crime, EventKind.Refinery, EventKind.Injury, EventKind.Crash, EventKind.SessionChange },
    };

    [ObservableProperty] private LogEntry? selectedEntry;

    // Zeile online nachschlagen (Schiff/Bauplan/Item/Ware) – öffnet eine gezielte Web-Suche.
    [RelayCommand]
    private void Lookup()
    {
        var e = SelectedEntry;
        if (e is null || string.IsNullOrWhiteSpace(e.Detail)) return;

        // Detail von Zusätzen befreien (×1 · Shop, (bei …), – Kollision …)
        var term = e.Detail;
        foreach (var sep in new[] { "  ·", "  ", " ·", " (", " – ", " - " })
        {
            var i = term.IndexOf(sep, System.StringComparison.Ordinal);
            if (i > 2) term = term[..i];
        }
        term = term.Trim();

        // Missionen in der CitizenHQ-Missions-DB nachschlagen. Nur den Status-Präfix
        // ("Auftrag abgeschlossen: " …) strippen – der Name selbst kann "-" und ":" enthalten,
        // daher NICHT die generische Trennung von oben verwenden.
        if (e.Kind == EventKind.Mission)
        {
            var name = System.Text.RegularExpressions.Regex.Replace(
                e.Detail ?? "",
                @"^(Auftrag (?:abgeschlossen|angenommen|geteilt|zurückgezogen)|Neuer Auftrag|New Objective|Contract (?:Accepted|Complete)):?\s*",
                "").Trim();
            foreach (var sep in new[] { " | ", " Rang:", " Direktroute:", "  ·" })   // Rang/Route-Ballast abschneiden
            {
                var i = name.IndexOf(sep, System.StringComparison.Ordinal);
                if (i > 2) name = name[..i];
            }
            name = name.Trim(' ', ':');
            var enName = Localization.ToEnglish(name, LogPath);   // DE→EN für die englische DB
            if (enName == name && LooksGerman(name))
            {
                // Nicht übersetzbar (z.B. Ziel-Name mit Variable wie "Onyx-Facility S3B7 aufsuchen")
                // → breite Suche statt leerer englischer CitizenHQ-Trefferliste.
                OpenUrl("https://www.google.com/search?q=" + System.Uri.EscapeDataString("Star Citizen " + name));
                return;
            }
            // CitizenHQ speichert variable Schiff-/Ziel-Namen als Platzhalter (z.B. "CRITICAL REFUEL
            // REQUEST: Ship") → nur den Basis-Namen vor dem ersten ":" senden, sonst 0 Treffer.
            // Ausnahme: zu generischer Ein-Wort-Basis (z.B. "Target") → vollen Namen lassen.
            var mq = enName;
            int c = mq.IndexOf(':');
            if (c > 2)
            {
                var basePart = mq[..c].Trim();
                if (basePart.Split(' ', System.StringSplitOptions.RemoveEmptyEntries).Length >= 2) mq = basePart;
            }
            if (mq.Length > 1)
                OpenUrl("https://citizenhq.space/missions?q=" + System.Uri.EscapeDataString(mq));
            return;
        }

        // Loot/Items in der CitizenHQ-Item-DB nachschlagen (Preis dort, sobald verfügbar).
        if (e.Kind == EventKind.Loot)
        {
            OpenUrl("https://citizenhq.space/items?q=" + System.Uri.EscapeDataString(term));
            return;
        }

        // Baupläne direkt in der CitizenHQ-Bauplan-DB nachschlagen (?q= treibt die Suche).
        // Die Suche macht Substring-Match auf den vollen Namen – daher nur die ersten
        // 2 Wörter senden, sonst liefern abweichende Farb-/Variantennamen 0 Treffer.
        if (e.Kind == EventKind.Blueprint)
        {
            var en = Localization.ToEnglish(term, LogPath);   // DE→EN, dann erst kürzen
            var words = en.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            var query = string.Join(" ", words.Length > 2 ? words[..2] : words);
            OpenUrl("https://citizenhq.space/blueprints?q=" + System.Uri.EscapeDataString(query));
            return;
        }

        string q = e.Kind switch
        {
            EventKind.Vehicle or EventKind.ShipLoss or EventKind.Quantum => $"Star Citizen {term} ship",
            EventKind.Sale or EventKind.Purchase => $"Star Citizen {term} item",
            EventKind.Trade => $"Star Citizen {term} commodity",
            EventKind.Mission => $"Star Citizen {term} mission",
            EventKind.Location => $"Star Citizen {term} location",
            _ => $"Star Citizen {term}"
        };

        OpenUrl("https://www.google.com/search?q=" + System.Uri.EscapeDataString(q));
    }

    [RelayCommand]
    private void OpenChangelog()
    {
        try
        {
            var exeDir = AppContext.BaseDirectory;
            var localMd = Path.Combine(exeDir, "CHANGELOG.md");
            if (File.Exists(localMd))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = localMd, UseShellExecute = true });
                return;
            }
        }
        catch { }
        OpenUrl("https://github.com/gOOvER/SCLogMate/releases");
    }

    // Grobe Heuristik: deutscher (nicht übersetzter) Missionsname? Dann taugt die englische DB nicht.
    static bool LooksGerman(string s)
    {
        if (s.IndexOfAny(new[] { 'ä', 'ö', 'ü', 'ß', 'Ä', 'Ö', 'Ü' }) >= 0) return true;
        foreach (var w in new[] { "aufsuchen", "besiegen", "ausschalten", "zerstör", "sammeln", "bergen",
                                  "liefern", "abholen", "Rückkehr", "verteidig", "eskortier", "Trümmer",
                                  "erledigen", "vernichten", "einsammeln", "beschützen", "untersuchen" })
            if (s.Contains(w, System.StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    static void OpenUrl(string url)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (System.Exception ex) { Logger.Error("OpenUrl", ex); }
    }

    [RelayCommand]
    private void StartEditBalance()
    {
        IsEditingManualBalance = true;
    }

    [RelayCommand]
    private void CancelEditBalance()
    {
        IsEditingManualBalance = false;
    }

    [RelayCommand]
    private void SetBalance()
    {
        var v = StartBalance();
        ManualBalance = v > 0 ? v.ToString("N0") : "";   // mit Tausenderpunkten anzeigen
        _settings.Balance = v;
        // Zeitpunkt festhalten: der Wert gilt ab JETZT. Nur spätere Bewegungen werden
        // angerechnet (sonst würde die lückenhafte Historie draufaddiert → falsche Erwartung).
        _settings.BalanceSetAt = v > 0 ? DateTime.UtcNow : null;
        Settings.Save(_settings);
        RecomputeBalances();
        IsEditingManualBalance = false;
        OnPropertyChanged(nameof(AccountText));
        OnPropertyChanged(nameof(LiveBalanceText));
        Status = v > 0 ? $"Kontostand gesetzt: {v:N0} aUEC (ab jetzt)" : "Kontostand geleert";
        BalanceSaved = true;   // grünes OK-Signal am Button, blendet nach 2s aus
        Avalonia.Threading.DispatcherTimer.RunOnce(() => BalanceSaved = false, System.TimeSpan.FromSeconds(2));
    }

    public string ManualBalanceTooltip => AutoOcrEnabled
        ? "Deaktiviert, da ⚡ Auto-Sync aktiv ist (der Kontostand wird automatisch per mobiGlas gelesen).\nSchalte 'Auto' oben aus, um den Kontostand manuell einzutragen."
        : "Kontostand manuell überschreiben / anpassen";

    [RelayCommand]
    private void ToggleAutoOcr()
    {
        AutoOcrEnabled = !AutoOcrEnabled;
        _settings.AutoOcrEnabled = AutoOcrEnabled;
        Settings.Save(_settings);
        OnPropertyChanged(nameof(ManualBalanceTooltip));
        Status = AutoOcrEnabled ? "⚡ Auto-Sync aktiviert (mobiGlas F1)" : "Auto-Sync deaktiviert (manuell)";

        if (AutoOcrEnabled)
        {
            _contractScanner.Start();
        }
        else
        {
            _contractScanner.Stop();
        }
    }

    [RelayCommand]
    private void SelectWalletRegion()
    {
        var win = new RegionSelectorWindow();
        win.RegionSelected += r =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _settings.WalletRegion = r;
                Settings.Save(_settings);
                UpdateOcrRegionText();
                if (ShowScanBox) _scanIndicator.SetRegion(r);
                Status = $"mobiGlas-Bereich gespeichert: {r.Width}x{r.Height} @ ({r.X},{r.Y})";
            });
        };
        win.Show();
    }

    [RelayCommand]
    private void ToggleScanBox()
    {
        ShowScanBox = !ShowScanBox;
        if (ShowScanBox)
        {
            var region = _settings.WalletRegion ?? ScreenCapture.GetDefaultWalletRegion();
            _scanIndicator.SetRegion(region);
            Status = "Scan-Rahmen im Spiel eingeblendet";
        }
        else
        {
            _scanIndicator.Hide();
            Status = "Scan-Rahmen ausgeblendet";
        }
    }

    [RelayCommand]
    private void ResetWalletRegion()
    {
        _settings.WalletRegion = null;
        Settings.Save(_settings);
        UpdateOcrRegionText();
        _scanIndicator.Hide();
        Status = "mobiGlas-Bereich auf Standard (Auto-Erkennung) zurückgesetzt";
    }

    [RelayCommand]
    private async Task TriggerOcr()
    {
        if (!OcrAvailable)
        {
            Status = "Windows OCR ist auf diesem System nicht verfügbar";
            return;
        }

        Status = "Kontostand wird gescannt…";
        var val = await _walletCapture.ScanDirectAsync();
        if (val.HasValue)
        {
            OnBalanceCaptured(val.Value);
            Status = $"Kontostand erkannt: {val.Value:N0} aUEC";
        }
        else
        {
            _walletCapture.Trigger();
            Status = "OCR-Erfassung läuft… (mobiGlas F1 geöffnet halten)";
        }
    }

    void UpdateOcrRegionText()
    {
        OcrRegionText = _settings.WalletRegion is { } r
            ? $"Bereich: {r.Width}x{r.Height} @ ({r.X},{r.Y})"
            : "Standard (Auto-Erkennung)";
        OnPropertyChanged(nameof(WalletRegionSummaryText));
        OnPropertyChanged(nameof(OcrRegionText));
    }

    [RelayCommand]
    private void SelectContractRegion()
    {
        var win = new RegionSelectorWindow();
        win.RegionSelected += r =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _settings.ContractRegion = r;
                Settings.Save(_settings);
                UpdateContractRegionText();
                OnPropertyChanged(nameof(ContractRegionTooltip));
                Status = $"Auftrags-Bereich gespeichert: {r.Width}x{r.Height} @ ({r.X},{r.Y})";
            });
        };
        win.Show();
    }

    [RelayCommand]
    public async Task ScanContract()
    {
        if (!OcrAvailable)
        {
            Status = "Windows OCR ist auf diesem System nicht verfügbar";
            return;
        }

        var region = _settings.ContractRegion ?? ScreenCapture.GetDefaultContractRegion();
        if (region == null || !region.IsValid)
        {
            region = ScreenCapture.GetDefaultContractRegion();
        }

        Status = "Auftragsmanager wird gescannt…";
        var raw = ScreenCapture.Capture(region.X, region.Y, region.Width, region.Height);
        if (raw == null)
        {
            ContractStatusText = "Bildschirm-Erfassung fehlgeschlagen";
            return;
        }

        var text = await _ocrEngine.RecognizeSinglePassAsync(raw, region.Width, region.Height);
        if (string.IsNullOrWhiteSpace(text))
        {
            ContractStatusText = "Kein Text im Auftrags-Bereich erkannt";
            return;
        }

        // Streng prüfen: Nur Aufträge aus dem ACCEPTED-Tab übernehmen!
        var details = ContractParser.Parse(text, requireAccepted: true);
        if (details != null && details.Reward > 0 && details.Title.Length >= 3)
        {
            OnContractScanned(details);
        }
        else
        {
            if (ContractParser.IsAcceptedContract(text))
            {
                ContractStatusText = "Im Accepted-Tab, aber kein Betrag lesbar";
            }
            else
            {
                ContractStatusText = "Nicht im Accepted-Tab (nur angenommene Aufträge)";
                Status = "Hinweis: Bitte im mobiGlas auf den Tab 'ACCEPTED' wechseln.";
            }
        }
    }

    void UpdateContractRegionText()
    {
        ContractRegionText = _settings.ContractRegion is { } r
            ? $"Bereich: {r.Width}x{r.Height} @ ({r.X},{r.Y})"
            : "Nicht kalibriert (⊕ Bereich klicken)";
        OnPropertyChanged(nameof(ContractRegionSummaryText));
    }

    public string DebugLogPath => Path.Combine(Settings.Dir, "SCLogMate.debug.log");
    public string DatabaseSummaryText => $"SQLite WAL · {Sessions.Count} Sessions · {Database.FormatBytes(Database.GetDatabaseSizeBytes())}";
    public string RuntimeInfoText => $".NET 10.0 (Win-x64) · Avalonia UI · Windows.Media.Ocr";
    public string WalletRegionSummaryText => _settings.WalletRegion is { } r ? $"{r.Width}x{r.Height} @ ({r.X}, {r.Y})" : "Standard (Auto-Erkennung)";
    public string ContractRegionSummaryText => _settings.ContractRegion is { } r ? $"{r.Width}x{r.Height} @ ({r.X}, {r.Y})" : "Nicht kalibriert (⊕ Bereich wählen)";

    [RelayCommand]
    private async Task RescanAllLogs()
    {
        if (IsDatabaseBusy) return;
        IsDatabaseBusy = true;
        DatabaseStatusMessage = "Sammle Log-Dateien für kompletten Re-Scan...";
        DatabaseProgressPercent = 0;

        try
        {
            await Task.Run(() =>
            {
                var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 0. SCLogReader eigenes Archiv (%APPDATA%\SCLogReader\archive)
                if (Directory.Exists(LogArchive.Dir))
                {
                    foreach (var f in Directory.GetFiles(LogArchive.Dir, "*.log"))
                        files.Add(f);
                }

                // 1. Aktuelle Game.log
                if (!string.IsNullOrEmpty(LogPath) && File.Exists(LogPath))
                {
                    files.Add(LogPath);
                    var dir = Path.GetDirectoryName(LogPath);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        var backups = Path.Combine(dir, "logbackups");
                        if (Directory.Exists(backups))
                        {
                            foreach (var f in Directory.GetFiles(backups, "*.log"))
                                files.Add(f);
                        }
                    }
                }

                // 2. Automatische Suche über alle Star Citizen Kanäle
                foreach (var log in PathFinder.FindAll())
                {
                    files.Add(log);
                    var dir = Path.GetDirectoryName(log);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        var backups = Path.Combine(dir, "logbackups");
                        if (Directory.Exists(backups))
                        {
                            foreach (var f in Directory.GetFiles(backups, "*.log"))
                                files.Add(f);
                        }
                    }
                }

                // 3. Re-Scan ausführen
                var result = Database.RescanAll(files, (curr, total, name) =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        DatabaseProgressPercent = (double)curr / total * 100.0;
                        DatabaseStatusMessage = $"Scanne ({curr}/{total}): {name}";
                    });
                });

                Dispatcher.UIThread.Post(() =>
                {
                    DatabaseStatusMessage = $"✓ Re-Scan abgeschlossen: {result.indexedSessions} Sessions, {result.totalEvents:N0} Ereignisse neu indexiert.";
                    Status = $"✓ Re-Scan abgeschlossen ({result.indexedSessions} Sessions / {result.totalEvents:N0} Events)";
                });
            });

            // UI-Daten neu laden
            if (!string.IsNullOrEmpty(LogPath) && File.Exists(LogPath))
            {
                RefreshSessions(selectCurrent: true);
                LoadSession();
            }
            else
            {
                OnPropertyChanged(nameof(DatabaseSummaryText));
            }
            SyncBlueprints();
        }
        catch (Exception ex)
        {
            DatabaseStatusMessage = $"Fehler beim Re-Scan: {ex.Message}";
            Logger.Error("RescanAllLogs", ex);
        }
        finally
        {
            IsDatabaseBusy = false;
            OnPropertyChanged(nameof(DatabaseSummaryText));
        }
    }

    [RelayCommand]
    private async Task CleanupDatabase()
    {
        if (IsDatabaseBusy) return;
        IsDatabaseBusy = true;
        DatabaseStatusMessage = "Optimiere und bereinige SQLite-Datenbank...";

        try
        {
            await Task.Run(() =>
            {
                var result = Database.Cleanup();
                Dispatcher.UIThread.Post(() =>
                {
                    DatabaseStatusMessage = $"✓ Bereinigung fertig! {result.cleanedEvents} leere Events / {result.cleanedSessions} verwaiste Sessions bereinigt. Größe: {Database.FormatBytes(result.sizeAfter)}.";
                    Status = $"✓ Datenbank bereinigt ({Database.FormatBytes(result.sizeAfter)})";
                });
            });
        }
        catch (Exception ex)
        {
            DatabaseStatusMessage = $"Fehler beim Cleanup: {ex.Message}";
            Logger.Error("CleanupDatabase", ex);
        }
        finally
        {
            IsDatabaseBusy = false;
            OnPropertyChanged(nameof(DatabaseSummaryText));
        }
    }

    [RelayCommand]
    private async Task ResetDatabase()
    {
        if (IsDatabaseBusy) return;
        IsDatabaseBusy = true;
        DatabaseStatusMessage = "Leere SQLite-Datenbank...";

        try
        {
            await Task.Run(() =>
            {
                Database.ClearAll();
            });

            Events.Clear();
            ShipsSeen.Clear();
            _shipSet.Clear();
            _liveMoney.Clear();
            TotalIn = TotalReward = TotalOut = TotalPurchases = TotalSales = TotalTrade = 0;
            MissionsDone = 0;
            CurrentLocation = CurrentShip = LastInventory = "—";
            RefreshSessions(selectCurrent: true);
            LoadSession();
            DatabaseStatusMessage = "✓ Datenbank wurde vollständig geleert.";
            Status = "✓ Datenbank geleert (0 Sessions, 0 Events)";
        }
        catch (Exception ex)
        {
            DatabaseStatusMessage = $"Fehler beim Zurücksetzen: {ex.Message}";
            Logger.Error("ResetDatabase", ex);
        }
        finally
        {
            IsDatabaseBusy = false;
            OnPropertyChanged(nameof(DatabaseSummaryText));
        }
    }

    [RelayCommand]
    private void OpenDebugLog()
    {
        try
        {
            if (File.Exists(DebugLogPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = DebugLogPath, UseShellExecute = true });
            }
            else
            {
                Status = "Debug-Log noch nicht vorhanden: " + DebugLogPath;
            }
        }
        catch (Exception ex)
        {
            Status = "Fehler beim Öffnen des Debug-Logs: " + ex.Message;
        }
    }

    [RelayCommand]
    private void OpenAppDataFolder()
    {
        try
        {
            var dir = Settings.Dir;
            Directory.CreateDirectory(dir);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = dir, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Status = "Fehler beim Öffnen des AppData-Ordners: " + ex.Message;
        }
    }

    [RelayCommand]
    public void ClearContracts()
    {
        ActiveContracts.Clear();
        _knownContracts.Clear();
        HasActiveContract = false;
        ActiveContractTitle = "— Kein aktiver Auftrag —";
        ActiveContractRewardText = "—";
        ActiveContractOrg = "";
        ContractStatusText = "Keine aktiven Aufträge";
        OnPropertyChanged(nameof(HasActiveContracts));
        OnPropertyChanged(nameof(ActiveContractsCountText));
        Database.ClearActiveContracts();
        Status = "Auftragsliste geleert.";
    }

    void OnContractScanned(ContractDetails d)
    {
        if (d == null || d.Reward <= 0 || string.IsNullOrWhiteSpace(d.Title)) return;

        Dispatcher.UIThread.Post(() =>
        {
            var title = d.Title.Trim();
            var norm = ContractParser.NormalizeTitle(title);
            if (norm.Length < 3) return;

            // Robuste Deduplizierung: Erkennt denselben Auftrag (Titel, Betrag, Kernwörter, Organisation)
            var existing = ActiveContracts.FirstOrDefault(c => ContractParser.AreSameContract(c, d));

            if (existing != null)
            {
                // Nur bestehenden Auftrag im Speicher aktualisieren
                if (title.Length > existing.Title.Length || string.IsNullOrEmpty(existing.ContractedBy))
                {
                    existing.Title = title;
                    existing.ContractedBy = !string.IsNullOrEmpty(d.ContractedBy) ? d.ContractedBy : existing.ContractedBy;
                    existing.Reward = d.Reward;
                    existing.ScannedAt = d.ScannedAt;
                    Database.SaveContract(existing);
                }
                return;
            }

            ActiveContracts.Insert(0, d);
            if (ActiveContracts.Count > 15) ActiveContracts.RemoveAt(ActiveContracts.Count - 1);

            // In SQLite-Datenbank persistieren
            Database.SaveContract(d);

            _knownContracts[norm] = d.Reward;
            HasActiveContract = true;
            ActiveContractTitle = title;
            ActiveContractRewardText = d.RewardText;
            ActiveContractOrg = d.ContractedBy;
            ContractStatusText = $"✓ {ActiveContracts.Count} Auftrag/Aufträge ({DateTime.Now:HH:mm:ss})";

            OnPropertyChanged(nameof(HasActiveContracts));
            OnPropertyChanged(nameof(ActiveContractsCountText));
            Status = $"★ Aktiver Auftrag erfasst: {title} · {d.RewardText}";
        });
    }

    void OnBalanceCaptured(long balance)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _settings.Balance = balance;
            _settings.BalanceSetAt = DateTime.UtcNow;
            Settings.Save(_settings);

            ManualBalance = balance.ToString("N0");
            RecomputeBalances();
            OnPropertyChanged(nameof(LiveBalanceText));
            OnPropertyChanged(nameof(AccountText));
            _scanIndicator.FlashGreen();
            Status = $"⚡ Kontostand per mobiGlas synchronisiert: {balance:N0} aUEC";
            OcrStatusText = $"✓ Zuletzt: {balance:N0} aUEC ({DateTime.Now:HH:mm:ss})";
        });
    }

    [RelayCommand]
    private void SetFilter(string name)
    {
        ActiveFilter = name;
        _activeKinds = FilterMap.TryGetValue(name, out var k) ? k : null;
        EventsView.Refresh();
    }

    public string SessionSpanText =>
        _sessionStart is { } a && _sessionEnd is { } b
            ? _allMode
                // „Alle Sessions": Datumsbereich + ECHTE Spielzeit (Summe der Session-Dauern),
                // nicht die Kalender-Spanne (die bei alten Logs riesig wird).
                ? $"{a.ToLocalTime():dd.MM.yy} → {b.ToLocalTime():dd.MM.yy}   (Σ {Dur(_allPlaytime)} gespielt)"
                : $"{a.ToLocalTime():dd.MM. HH:mm} → {b.ToLocalTime():HH:mm}   ({Dur(b - a)})"
            : "—";

    static string Dur(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes}m" : $"{t.Minutes}m";

    readonly System.Collections.Generic.SortedSet<string> _blueprints = new();
    public bool HasBlueprints => _blueprints.Count > 0;
    public string BlueprintsLine => _blueprints.Count == 0 ? "" : $"⬡  Baupläne ({_blueprints.Count}):  {string.Join("   ·   ", _blueprints)}";

    void AddBlueprint(string name)
    {
        if (_blueprints.Add(name))
        {
            OnPropertyChanged(nameof(HasBlueprints));
            OnPropertyChanged(nameof(BlueprintsLine));
        }
    }

    readonly System.Collections.Generic.HashSet<string> _shipSet = new();
    public ObservableCollection<string> ShipsSeen { get; } = new();
    public string FleetText => ShipsSeen.Count == 0 ? "" : $"{ShipsSeen.Count}× geflogen";
    public string ShipsSeenText => ShipsSeen.Count == 0 ? "—" : string.Join("\n", ShipsSeen);

    public long IncomeAll => TotalIn + TotalReward + TotalSales + TotalTrade;
    public long SpendAll => TotalOut + TotalPurchases;
    public long NetAll => IncomeAll - SpendAll;

    public long LiveSessionIncome => _allMode
        ? _liveMoney.Where(e => IsMoney(e.Kind) && e.Amount > 0).Sum(e => e.Amount)
        : Events.Where(e => IsMoney(e.Kind) && e.Amount > 0).Sum(e => e.Amount);

    public long LiveSessionSpend => _allMode
        ? _liveMoney.Where(e => IsMoney(e.Kind) && e.Amount < 0).Sum(e => -e.Amount)
        : Events.Where(e => IsMoney(e.Kind) && e.Amount < 0).Sum(e => -e.Amount);

    public long LiveSessionNet => LiveSessionIncome - LiveSessionSpend;

    public string SessionIncomeText => $"+{LiveSessionIncome:N0} aUEC";
    public string SessionSpendText => $"-{LiveSessionSpend:N0} aUEC";
    public string SessionNetText => $"{(LiveSessionNet >= 0 ? "+" : "")}{LiveSessionNet:N0} aUEC";
    public long SessionNetSign => LiveSessionNet;
    public string LiveBalanceText => ExpectedBalance > 0 ? $"{ExpectedBalance:N0} aUEC" : StartBalance() > 0 ? $"{StartBalance():N0} aUEC" : "— Nicht gesetzt —";

    // Geld-Statistik (eigener Tab)
    public ObservableCollection<StatItem> IncomeStats { get; } = new();
    public ObservableCollection<StatItem> SpendStats { get; } = new();
    public ObservableCollection<StatItem> TopTransactions { get; } = new();
    public ObservableCollection<StatItem> RecentMoney { get; } = new();
    public ObservableCollection<StatItem> CommodityTrades { get; } = new();
    public bool HasTrades => CommodityTrades.Count > 0;

    public ObservableCollection<MarketPrice> MarketPrices { get; } = new();
    public bool HasMarket => MarketPrices.Count > 0;

    public ObservableCollection<MissionStat> MissionFactions { get; } = new();
    public bool HasMissionFactions => MissionFactions.Count > 0;
    public string MissionsTotalText => $"{_missionTotal:N0} Missionen · {MissionFactions.Count} Auftraggeber";
    int _missionTotal;
    public string IncomeTotalText => $"{IncomeAll:N0} aUEC";
    public string SpendTotalText => $"{SpendAll:N0} aUEC";

    const double BarMax = 280.0;

    void RebuildStats()
    {
        RebuildBars();
        // Top aus der aktuellen Events-Liste (Live-/Einzelsession)
        var top = Events.Where(e => IsMoney(e.Kind))
                        .OrderByDescending(e => System.Math.Abs(e.Amount))
                        .Take(8)
                        .OrderBy(e => System.Math.Abs(e.Amount))
                        .ToList();
        SetTopTransactions(top);
        RebuildCommodityTrades(Events.Where(e => e.Kind == EventKind.Trade));
        RebuildMarketPrices(Events.Where(e => e.Kind == EventKind.Trade));
        RebuildMissions(Events.Where(e => e.Kind == EventKind.MissionTaken));
    }

    void SetTopTransactions(System.Collections.Generic.IEnumerable<LogEntry> events)
    {
        var list = events.ToList();
        long max = 1;
        foreach (var e in list) max = System.Math.Max(max, System.Math.Abs(e.Amount));
        TopTransactions.Clear();
        foreach (var e in list)
            TopTransactions.Add(new StatItem
            {
                Label = $"{e.KindText}: {e.Detail}",
                Value = e.Amount,
                Time = e.Time,
                BarWidth = System.Math.Abs(e.Amount) / (double)max * BarMax,
                Color = Brush(e.Amount >= 0 ? "#4ADE80" : "#F87171")
            });
        RebuildRecentMoney();
    }

    // Letzte Geld-Bewegungen, neueste zuerst (mit Datum).
    void RebuildRecentMoney()
    {
        RecentMoney.Clear();
        foreach (var e in Events.Where(x => IsMoney(x.Kind)).OrderByDescending(x => x.Time).Take(30))
            RecentMoney.Add(new StatItem
            {
                Label = $"{e.KindText}: {e.Detail}",
                Value = e.Amount,
                Time = e.Time,
                Color = Brush(e.Amount >= 0 ? "#4ADE80" : "#F87171")
            });
    }

    void RebuildBars()
    {
        var inc = new (string L, long V, string C)[]
        {
            ("Transfers rein", TotalIn,    "#4ADE80"),
            ("Belohnungen",    TotalReward, "#FBBF24"),
            ("Item-Verkäufe",  TotalSales,  "#34D399"),
            ("Fracht-Handel",  TotalTrade,  "#22D3EE"),
        };
        var spd = new (string L, long V, string C)[]
        {
            ("Transfers raus", TotalOut,       "#F87171"),
            ("Käufe",          TotalPurchases, "#FB923C"),
        };

        long max = 1;
        foreach (var x in inc) max = System.Math.Max(max, x.V);
        foreach (var x in spd) max = System.Math.Max(max, x.V);

        IncomeStats.Clear();
        foreach (var x in inc.Where(i => i.V > 0).OrderByDescending(i => i.V))
            IncomeStats.Add(new StatItem { Label = x.L, Value = x.V, BarWidth = x.V / (double)max * BarMax, Color = Brush(x.C) });

        SpendStats.Clear();
        foreach (var x in spd.Where(i => i.V > 0).OrderByDescending(i => i.V))
            SpendStats.Add(new StatItem { Label = x.L, Value = x.V, BarWidth = x.V / (double)max * BarMax, Color = Brush(x.C) });

        OnPropertyChanged(nameof(IncomeTotalText));
        OnPropertyChanged(nameof(SpendTotalText));
    }

    [GeneratedRegex(@"^(?<ware>.+?) ×(?<scu>\d+) SCU")]
    private static partial Regex TradeDetailRegex();

    // „Handel je Ware": SCU + Ø Preis/SCU + Erlös, pro Commodity zusammengefasst.
    void RebuildCommodityTrades(System.Collections.Generic.IEnumerable<LogEntry> trades)
    {
        var agg = new System.Collections.Generic.Dictionary<string, (long scu, long erloes)>();
        foreach (var e in trades)
        {
            if (e.Amount <= 0) continue;   // nur Verkäufe → Ø Preis/SCU sauber (Käufe verfälschen nicht)
            var m = TradeDetailRegex().Match(e.Detail ?? "");
            if (!m.Success) continue;
            var ware = m.Groups["ware"].Value.Trim();
            long scu = long.TryParse(m.Groups["scu"].Value, out var s) ? s : 0;
            var cur = agg.TryGetValue(ware, out var v) ? v : (scu: 0L, erloes: 0L);
            agg[ware] = (cur.scu + scu, cur.erloes + e.Amount);
        }

        long maxErloes = 1;
        foreach (var v in agg.Values) maxErloes = System.Math.Max(maxErloes, v.erloes);

        CommodityTrades.Clear();
        foreach (var kv in agg.OrderByDescending(k => k.Value.erloes))
        {
            var perScu = kv.Value.scu > 0 ? kv.Value.erloes / kv.Value.scu : 0;
            CommodityTrades.Add(new StatItem
            {
                Label = kv.Key,
                Sub = $"{kv.Value.scu:N0} SCU · Ø {perScu:N0}/SCU",
                Value = kv.Value.erloes,
                BarWidth = kv.Value.erloes / (double)maxErloes * BarMax,
                Color = Brush("#22D3EE")
            });
        }
        OnPropertyChanged(nameof(HasTrades));
    }

    [GeneratedRegex(@"^(?<ware>.+?) ×(?<scu>\d+) SCU\s*·\s*(?<shop>.+?)(?<kauf>\s*\(Kauf\))?\s*$")]
    private static partial Regex MarketDetailRegex();

    // „Marktpreise": pro Ware bester Verkaufs- und günstigster Kaufpreis (pro SCU) + Terminal + Marge.
    void RebuildMarketPrices(System.Collections.Generic.IEnumerable<LogEntry> trades)
    {
        var m = new System.Collections.Generic.Dictionary<string,
            (long sell, string sShop, long buy, string bShop)>();
        foreach (var e in trades)
        {
            var mm = MarketDetailRegex().Match(e.Detail ?? "");
            if (!mm.Success) continue;
            var ware = mm.Groups["ware"].Value.Trim();
            long scu = long.TryParse(mm.Groups["scu"].Value, out var s) ? s : 0;
            if (scu <= 0) continue;
            var shop = mm.Groups["shop"].Value.Trim();
            bool isBuy = mm.Groups["kauf"].Success || e.Amount < 0;
            long perScu = System.Math.Abs(e.Amount) / scu;
            var cur = m.TryGetValue(ware, out var v) ? v : (sell: 0L, sShop: "", buy: 0L, bShop: "");
            if (isBuy) { if (cur.buy == 0 || perScu < cur.buy) { cur.buy = perScu; cur.bShop = shop; } }
            else { if (perScu > cur.sell) { cur.sell = perScu; cur.sShop = shop; } }
            m[ware] = cur;
        }

        MarketPrices.Clear();
        foreach (var kv in m.OrderByDescending(x => x.Value.sell))
        {
            var d = kv.Value;
            long margin = d.sell > 0 && d.buy > 0 ? d.sell - d.buy : 0;
            MarketPrices.Add(new MarketPrice
            {
                Commodity = kv.Key,
                SellText = d.sell > 0 ? $"{d.sell:N0}/SCU · {d.sShop}" : "—",
                BuyText = d.buy > 0 ? $"{d.buy:N0}/SCU · {d.bShop}" : "—",
                MarginText = margin != 0 ? $"{margin:+#,##0;-#,##0}/SCU" : "",
                MarginValue = margin
            });
        }
        OnPropertyChanged(nameof(HasMarket));
    }

    // Missionen je Auftraggeber/Fraktion (Ruf-Proxy) aus den MissionTaken-Events.
    // Detail-Form: "Faction · Type · Difficulty · System" – Fraktion ist das erste Segment.
    void RebuildMissions(System.Collections.Generic.IEnumerable<LogEntry> missions)
    {
        var byFaction = new System.Collections.Generic.Dictionary<string,
            (int count, System.Collections.Generic.Dictionary<string,int> types)>();
        int total = 0;
        foreach (var e in missions)
        {
            var parts = (e.Detail ?? "").Split(" · ");
            if (parts.Length < 2) continue;
            var faction = parts[0].Trim();
            if (faction.Length == 0) continue;
            var type = parts[1].Trim();
            total++;
            if (!byFaction.TryGetValue(faction, out var cur))
                cur = (0, new System.Collections.Generic.Dictionary<string,int>());
            cur.count++;
            cur.types[type] = cur.types.TryGetValue(type, out var tc) ? tc + 1 : 1;
            byFaction[faction] = cur;
        }

        _missionTotal = total;
        int max = 1;
        foreach (var v in byFaction.Values) max = System.Math.Max(max, v.count);

        MissionFactions.Clear();
        foreach (var kv in byFaction.OrderByDescending(x => x.Value.count))
        {
            var topType = kv.Value.types.OrderByDescending(t => t.Value).First().Key;
            MissionFactions.Add(new MissionStat
            {
                Faction = kv.Key,
                Count = kv.Value.count,
                BarWidth = kv.Value.count / (double)max * BarMax,
                SubText = topType
            });
        }
        OnPropertyChanged(nameof(HasMissionFactions));
        OnPropertyChanged(nameof(MissionsTotalText));
    }

    static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));

    public string VersionText => "v" + Updater.CurrentVersion;

    public string ScPlayerName => _parser.Meta.TryGetValue("character", out var c) && !string.IsNullOrWhiteSpace(c) ? c : "—";

    public string ScVersionText
    {
        get
        {
            if (_parser.Meta.TryGetValue("version", out var v) && !string.IsNullOrWhiteSpace(v))
            {
                var ver = v.Trim();
                return ver.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? ver : "v" + ver;
            }
            return "—";
        }
    }

    public string ScChannel => _parser.Meta.TryGetValue("env", out var e) && !string.IsNullOrWhiteSpace(e) ? e.ToUpperInvariant() : "LIVE";

    public string ServerShardName => _parser.Meta.TryGetValue("shard", out var s) && !string.IsNullOrWhiteSpace(s) ? s : "—";

    public string ServerShardNumber
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ServerShardName) || ServerShardName == "—")
                return "Kein Server";

            var match = Regex.Match(ServerShardName, @"(?:_|\b)(\d+)$");
            if (match.Success)
            {
                return $"Shard #{match.Groups[1].Value}";
            }
            return ServerShardName;
        }
    }

    public (string flag, string code, string name) ServerRegionInfo
    {
        get
        {
            var shard = ServerShardName.ToLowerInvariant();
            if (shard.Contains("euw") || shard.Contains("euc") || shard.Contains("eu") || shard.Contains("fra") || shard.Contains("lon"))
                return ("🇪🇺", "EU", "Europa");
            if (shard.Contains("use") || shard.Contains("usw") || shard.Contains("us") || shard.Contains("na") || shard.Contains("va"))
                return ("🇺🇸", "US", "USA / Nordamerika");
            if (shard.Contains("aus") || shard.Contains("oce") || shard.Contains("ap") || shard.Contains("syd"))
                return ("🇦🇺", "AUS", "Australien / APAC");
            if (shard.Contains("asia") || shard.Contains("jp") || shard.Contains("sg") || shard.Contains("tyo"))
                return ("🌏", "ASIA", "Asien");
            if (ServerShardName != "—")
                return ("🌐", "PU", "Persistent Universe");
            return ("🌐", "—", "Unbekannt");
        }
    }

    public string ServerRegionFlag => ServerRegionInfo.flag;
    public string ServerRegionCode => ServerRegionInfo.code;
    public string ServerRegionName => ServerRegionInfo.name;
    public bool IsRegionEu => ServerRegionCode == "EU";
    public bool IsRegionUs => ServerRegionCode == "US";
    public bool IsRegionAus => ServerRegionCode == "AUS";
    public bool IsRegionAsia => ServerRegionCode == "ASIA";
    public bool IsRegionOther => !IsRegionEu && !IsRegionUs && !IsRegionAus && !IsRegionAsia;

    public string ServerBadgeText => ServerRegionCode == "—" ? ScChannel : $"{ServerRegionCode} · {ScChannel}";

    public string ServerMainText => ScPlayerName != "—" ? ScPlayerName : "Kein Pilot erkannt";

    public string ServerSublineText
    {
        get
        {
            var parts = new List<string>();
            if (ScVersionText != "—") parts.Add($"SC {ScVersionText}");
            if (ServerShardNumber != "—" && ServerShardNumber != "Kein Server") parts.Add(ServerShardNumber);
            return parts.Count > 0 ? string.Join("  ·  ", parts) : "Warte auf Server-Verbindung...";
        }
    }

    public string ServerPingText => ServerPingMs is { } p ? $"{p} ms" : IsPingingServer ? "…" : "—";
    public string ServerPingColor => ServerPingMs switch
    {
        null => "#8B949E",
        <= 45 => "#4ADE80",   // Grün (ausgezeichnet)
        <= 120 => "#FBBF24",  // Gelb (gut)
        _ => "#F87171"        // Rot (hoch)
    };

    public string ServerTooltipText => ServerShardName == "—"
        ? "Keine Serververbindung im aktuellen Log gefunden."
        : $"Vollständiger Shard-Name:\n{ServerShardName}\n\nRegion: {ServerRegionName} ({ServerRegionCode})\nLatenz (RTT): {(ServerPingMs is { } p ? $"{p} ms" : "Wird gemessen...")}\nKanal: {ScChannel}\nSpieler: {ScPlayerName}\nStar Citizen Build: {ScVersionText}";

    public async Task PingCurrentServerAsync()
    {
        if (IsPingingServer) return;
        IsPingingServer = true;
        try
        {
            var shard = ServerShardName;
            var latency = await ServerPingService.MeasureLatencyAsync(shard);
            Dispatcher.UIThread.Post(() =>
            {
                ServerPingMs = latency;
                OnPropertyChanged(nameof(ServerPingText));
                OnPropertyChanged(nameof(ServerPingColor));
                OnPropertyChanged(nameof(ServerTooltipText));
            });
        }
        catch { /* ignore */ }
        finally
        {
            IsPingingServer = false;
        }
    }

    public string MetaSummary
    {
        get
        {
            var parts = new List<string>();
            if (ScPlayerName != "—") parts.Add(ScPlayerName);
            if (ScVersionText != "—") parts.Add(ScVersionText);
            if (ServerShardName != "—") parts.Add($"{ServerRegionFlag} {ServerShardNumber}");
            return parts.Count == 0 ? "Star Citizen · Live-Auswertung" : string.Join("  ·  ", parts);
        }
    }

    // Log-Bewegung mit Vorzeichen (grün/rot via Converter im XAML)
    // Bezieht sich auf den Kontostand: nur Bewegungen SEIT dem Eintrag erklären den erwarteten Stand.
    public string NetBalanceText => _settings.BalanceSetAt is null
        ? "Kontostand setzen für Verlauf"
        : $"seit Eintrag {NetSinceBalance:+#,##0;-#,##0;0} aUEC";
    public long NetSign => NetSinceBalance;
    public string FlowText => $"▼ Ein {IncomeAll:N0}    ▲ Aus {SpendAll:N0}";
    public string TradeText => $"⇄ Handel {TotalSales + TotalTrade:N0}    ↧ Käufe {TotalPurchases:N0}";
    public bool HasMissions => MissionsDone > 0;
    public string MissionsText => MissionsDone > 0
        ? $"✔ {MissionsDone} Aufträge abgeschlossen — die Belohnung steht NICHT im Log (vom Server direkt aufs Konto). Differenz zu deinem echten Kontostand kommt v.a. daher."
        : "";

    partial void OnMissionsDoneChanged(int value)
    {
        OnPropertyChanged(nameof(HasMissions));
        OnPropertyChanged(nameof(MissionsText));
    }
    public string ToggleText => Running ? "Stop" : "Start";

    // Echter Kontostand (Eingabe) -> formatiert
    public string AccountText
    {
        get
        {
            var digits = new string(ManualBalance.Where(char.IsDigit).ToArray());
            return long.TryParse(digits, out var v) && v > 0 ? $"{v:N0} aUEC" : "— eintragen —";
        }
    }

    long _running;
    bool _allMode;     // „Alle Sessions": DB-Basis + Live-Session oben drauf
    TimeSpan _allPlaytime;   // echte Spielzeit (Summe der Session-Dauern) für „Alle Sessions"
    System.Collections.Generic.List<LogEntry> _dbTop = new();
    System.Collections.Generic.List<LogEntry> _dbTrades = new();
    readonly System.Collections.Generic.List<LogEntry> _liveMoney = new();

    long StartBalance()
    {
        var digits = new string((ManualBalance ?? "").Where(char.IsDigit).ToArray());
        if (long.TryParse(digits, out var v) && v > 0) return v;
        return _settings.Balance;
    }

    /// <summary>Summe der Geld-Bewegungen NACH dem Kontostand-Eintrag. Nur die zählen —
    /// der eingetragene Wert ist der Stand von damals, nicht der Start der ganzen Historie.</summary>
    public long NetSinceBalance
    {
        get
        {
            if (_settings.BalanceSetAt is not { } since) return 0;
            long sum = 0;
            foreach (var e in Events)
                if (IsMoney(e.Kind) && e.Time > since) sum += e.Amount;
            return sum;
        }
    }

    public long ExpectedBalance => StartBalance() + NetSinceBalance;
    public string ExpectedText =>
        StartBalance() <= 0 ? "Kontostand eintragen"
        : _settings.BalanceSetAt is null ? "≈ " + StartBalance().ToString("N0") + " aUEC (neu setzen für Verlauf)"
        : $"≈ {ExpectedBalance:N0} aUEC";

    static bool IsMoney(EventKind k) => k is EventKind.TransferIn or EventKind.TransferOut
        or EventKind.MissionReward or EventKind.Purchase or EventKind.Sale or EventKind.Trade or EventKind.Fine;

    // Saldo-Verlauf neu berechnen. Saldo gibt es nur für Ereignisse NACH dem Kontostand-Eintrag —
    // davor ist er unbekannt (Historie lückenhaft), daher bewusst leer statt falsch.
    void RecomputeBalances()
    {
        var since = _settings.BalanceSetAt;
        long run = StartBalance();
        for (int i = Events.Count - 1; i >= 0; i--)   // ältestes zuerst
        {
            var e = Events[i];
            if (!IsMoney(e.Kind)) continue;
            if (since is null || e.Time <= since.Value) { e.HasBalance = false; continue; }
            run += e.Amount;
            e.BalanceAfter = run;
            e.HasBalance = true;
        }
        _running = run;
        OnPropertyChanged(nameof(NetSinceBalance));
        OnPropertyChanged(nameof(NetBalanceText));
        OnPropertyChanged(nameof(NetSign));
        OnPropertyChanged(nameof(ExpectedText));
        OnPropertyChanged(nameof(ExpectedBalance));
        OnPropertyChanged(nameof(LiveBalanceText));
    }

    partial void OnManualBalanceChanged(string value)
    {
        OnPropertyChanged(nameof(AccountText));
        OnPropertyChanged(nameof(LiveBalanceText));
        RecomputeBalances();
        if (!_ready) return;
        var b = StartBalance();
        if (b > 0)
        {
            _settings.Balance = b;
            Settings.Save(_settings);
        }
    }

    public MainViewModel()
    {
        _initializing = true;
        _settings = Settings.Load();

        EventsView = new DataGridCollectionView(Events)
        {
            Filter = o => _activeKinds == null || (o is LogEntry e && _activeKinds.Contains(e.Kind))
        };

        foreach (var bp in BlueprintCatalog.CreateFreshCatalog())
        {
            BlueprintCatalogList.Add(bp);
        }

        BlueprintsView = new DataGridCollectionView(BlueprintCatalogList)
        {
            Filter = o =>
            {
                if (o is not BlueprintItem bp) return true;
                if (SelectedBlueprintCategory == "Erlernt" && !bp.IsLearned) return false;
                if (SelectedBlueprintCategory == "Fehlend" && bp.IsLearned) return false;
                if (SelectedBlueprintCategory != "Alle" && SelectedBlueprintCategory != "Erlernt" && SelectedBlueprintCategory != "Fehlend" && bp.Category != SelectedBlueprintCategory) return false;
                if (!string.IsNullOrWhiteSpace(BlueprintSearchText) &&
                    !bp.Name.Contains(BlueprintSearchText, StringComparison.OrdinalIgnoreCase) &&
                    !bp.SubCategory.Contains(BlueprintSearchText, StringComparison.OrdinalIgnoreCase) &&
                    !bp.UnlockInfo.Contains(BlueprintSearchText, StringComparison.OrdinalIgnoreCase) &&
                    !bp.RequiredMaterials.Contains(BlueprintSearchText, StringComparison.OrdinalIgnoreCase))
                    return false;
                return true;
            }
        };

        foreach (var m in MissionCatalog.AllMissions)
        {
            MissionCatalogList.Add(m);
        }

        MissionsView = new DataGridCollectionView(MissionCatalogList)
        {
            Filter = o =>
            {
                if (o is not MissionInfo m) return true;
                if (SelectedMissionType != "Alle" && m.MissionType != SelectedMissionType) return false;
                if (!string.IsNullOrWhiteSpace(MissionSearchText) &&
                    !m.Title.Contains(MissionSearchText, StringComparison.OrdinalIgnoreCase) &&
                    !m.Contractor.Contains(MissionSearchText, StringComparison.OrdinalIgnoreCase) &&
                    !m.Faction.Contains(MissionSearchText, StringComparison.OrdinalIgnoreCase) &&
                    !m.StarSystems.Contains(MissionSearchText, StringComparison.OrdinalIgnoreCase) &&
                    !m.Description.Contains(MissionSearchText, StringComparison.OrdinalIgnoreCase) &&
                    !m.BlueprintsText.Contains(MissionSearchText, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                return true;
            }
        };
        AutoOcrEnabled = _settings.AutoOcrEnabled;
        OcrAvailable = _ocrEngine.IsAvailable;
        UpdateOcrRegionText();

        UexApiKeyInput = _settings.UexApiKey ?? "";
        if (!string.IsNullOrEmpty(_settings.UexApiKey))
        {
            UexApiClient.SetApiKey(_settings.UexApiKey);
        }

        OverlayOpacity = _settings.OverlayOpacity > 0 ? _settings.OverlayOpacity : 0.92;
        if (_settings.OverlayEnabled)
        {
            IsOverlayActive = true;
        }
        MinimizeToTrayOnClose = _settings.MinimizeToTrayOnClose;

        _walletCapture = new WalletCapture(_ocrEngine, () => _settings.WalletRegion ?? ScreenCapture.GetDefaultWalletRegion(), () => AutoOcrEnabled);
        _walletCapture.BalanceCaptured += OnBalanceCaptured;

        _contractScanner = new ContractScanner(
            _ocrEngine,
            () => _settings.ContractRegion ?? ScreenCapture.GetDefaultContractRegion(),
            () => AutoOcrEnabled);
        _contractScanner.ContractScanned += OnContractScanned;
        _contractScanner.StageChanged += stage =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (stage == "not_accepted_tab" && HasActiveContract)
                {
                    // alter Auftrag bleibt stehen
                }
                else if (stage == "not_accepted_tab")
                {
                    ContractStatusText = "Wartet auf mobiGlas Accepted-Tab…";
                }
                else if (stage == "parsed")
                {
                    ContractStatusText = $"✓ {ActiveContracts.Count} Auftrag/Aufträge ({DateTime.Now:HH:mm:ss})";
                }
            });
        };

        if (AutoOcrEnabled)
        {
            _contractScanner.Start();
        }

        // Gespeicherte aktive Aufträge aus der SQLite-Datenbank laden
        try
        {
            var savedContracts = Database.GetActiveContracts();
            foreach (var c in savedContracts)
            {
                ActiveContracts.Add(c);
                var norm = ContractParser.NormalizeTitle(c.Title);
                if (norm.Length > 0) _knownContracts[norm] = c.Reward;
            }
            if (ActiveContracts.Count > 0)
            {
                var first = ActiveContracts[0];
                HasActiveContract = true;
                ActiveContractTitle = first.Title;
                ActiveContractRewardText = first.RewardText;
                ActiveContractOrg = first.ContractedBy;
                ContractStatusText = $"✓ {ActiveContracts.Count} gespeicherte(r) Auftrag/Aufträge geladen";
                OnPropertyChanged(nameof(HasActiveContracts));
                OnPropertyChanged(nameof(ActiveContractsCountText));
            }
        }
        catch (Exception ex)
        {
            Logger.Error("LoadActiveContracts", ex);
        }

        var saved = _settings.LogPath;
        var start = !string.IsNullOrWhiteSpace(saved) && File.Exists(saved)
            ? saved!
            : PathFinder.FindBest() ?? @"C:\Program Files\Roberts Space Industries\StarCitizen\LIVE\Game.log";

        LogPath = start;
        Localization.Hint(LogPath);   // Spiel-Wurzel für Item-/Text-Auflösung merken (vor DB-Aufbau)
        if (_settings.Balance > 0) ManualBalance = _settings.Balance.ToString("N0");

        // UEX & Overlay Einstellungen initialisieren
        UexApiKeyInput = _settings.UexApiKey ?? "";
        if (!string.IsNullOrEmpty(_settings.UexApiKey))
        {
            UexApiClient.SetApiKey(_settings.UexApiKey);
        }
        OverlayOpacity = _settings.OverlayOpacity > 0 ? _settings.OverlayOpacity : 0.92;

        RefreshSessions(selectCurrent: true);
        Status = saved != null && start == saved
            ? "gemerkte Einstellungen geladen"
            : Sessions.Count > 0 ? $"{Sessions.Count} Sessions gefunden" : "keine Game.log gefunden";
        _initializing = false;
        _ready = true;
        OnPropertyChanged(nameof(LiveBalanceText));
        OnPropertyChanged(nameof(AccountText));

        _settings.LogPath = LogPath;
        Settings.Save(_settings);

        // Standard: gleich alle Sessions laden
        if (SelectedSession?.IsAll == true) LoadSession();

        // Update-Prüfung: einmal beim Start + danach alle 6 Stunden
        CheckForUpdate();
        _updateTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromHours(6) };
        _updateTimer.Tick += (_, _) => CheckForUpdate();
        _updateTimer.Start();

        // Server-Latenz (Ping): Einmal sofort + alle 12 Sekunden im Hintergrund
        _ = PingCurrentServerAsync();
        _pingTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(12) };
        _pingTimer.Tick += (_, _) => _ = PingCurrentServerAsync();
        _pingTimer.Start();

        // Star Citizen Prozess-Erkennung & Auto-Resume
        CheckGameProcess();
        _processTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
        _processTimer.Tick += (_, _) => CheckGameProcess();
        _processTimer.Start();
    }

    void CheckGameProcess()
    {
        try
        {
            var procs = System.Diagnostics.Process.GetProcessesByName("StarCitizen");
            bool running = procs.Length > 0;
            if (running != IsGameRunning)
            {
                IsGameRunning = running;
                if (running)
                {
                    GameStatusText = "🟢 SC LÄUFT";
                    GameStatusColor = "#3FB950";
                    GameStatusBadgeBg = "#0E2818";
                    GameStatusTooltip = "Star Citizen (StarCitizen.exe) ist aktiv · Live-Logparser läuft";
                    Status = "★ Star Citizen gestartet – Live-Erfassung aktiv";
                    if (!Running)
                    {
                        LoadSession();
                    }
                }
                else
                {
                    GameStatusText = "⚪ SC STANDBY";
                    GameStatusColor = "#8B949E";
                    GameStatusBadgeBg = "#161B22";
                    GameStatusTooltip = "Star Citizen ist derzeit nicht gestartet · Parser wartet auf Spielstart";
                }
                OnPropertyChanged(nameof(GameStatusText));
                OnPropertyChanged(nameof(GameStatusColor));
                OnPropertyChanged(nameof(GameStatusBadgeBg));
                OnPropertyChanged(nameof(GameStatusTooltip));
            }
        }
        catch { /* ignore process lookup errors */ }
    }

    [RelayCommand]
    public async Task CheckForUpdateManual()
    {
        Status = "Prüfe auf Updates...";
        var info = await Updater.CheckAsync();
        if (info != null)
        {
            _update = info;
            UpdateAvailable = true;
            UpdateText = $"⬆ Update {info.Version}";
            Status = $"Update {info.Version} verfügbar!";
        }
        else
        {
            Status = $"SCLogMate ist auf dem neuesten Stand (v{Updater.CurrentVersion}).";
        }
    }

    async void CheckForUpdate()
    {
        var info = await Updater.CheckAsync();
        if (info is null) return;
        Dispatcher.UIThread.Post(() =>
        {
            _update = info;
            UpdateAvailable = true;
            UpdateText = $"⬆ Update {info.Version}";
        });
    }

    [RelayCommand]
    private async Task Update()
    {
        if (_update is null) return;
        Status = $"lade Update {_update.Version}…";
        await Updater.ApplyAsync(_update);
        Status = "Update wird installiert – Neustart…";
        await Task.Delay(400);
        (Avalonia.Application.Current?.ApplicationLifetime
            as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.Shutdown();
    }

    partial void OnRunningChanged(bool value) => OnPropertyChanged(nameof(ToggleText));

    // Pfad wird gemerkt – aber nicht bei reinen Session-Wechseln.
    partial void OnLogPathChanged(string value)
    {
        if (!_ready || _suppressSave) return;
        _settings.LogPath = value;
        Settings.Save(_settings);
    }

    partial void OnSelectedSessionChanged(SessionInfo? value)
    {
        if (_initializing || value is null) return;
        if (!value.IsAll)
        {
            _suppressSave = true;
            LogPath = value.Path;
            _suppressSave = false;
        }
        LoadSession();   // Session wechseln: zurücksetzen + einlesen
    }

    void RefreshSessions(bool selectCurrent)
    {
        var found = SessionScanner.Scan(LogPath);
        _initializing = true;
        Sessions.Clear();
        Sessions.Add(new SessionInfo { IsAll = true, Label = "★ Alle Sessions (zusammen)" });
        foreach (var s in found) Sessions.Add(s);
        if (selectCurrent)
            SelectedSession = Sessions.FirstOrDefault();   // Default: „Alle Sessions"
        _initializing = false;
    }

    [RelayCommand]
    private void Detect()
    {
        var found = PathFinder.FindBest();
        if (found != null) { LogPath = found; RefreshSessions(selectCurrent: true); Status = "erkannt: " + ChannelOf(found); }
        else Status = "keine Game.log gefunden";
    }

    // Game.log manuell wählen (z.B. wenn SC auf einer anderen Platte liegt).
    [RelayCommand]
    private async Task Browse()
    {
        var tl = UiServices.TopLevel;
        if (tl is null) return;

        IStorageFolder? start = null;
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (dir != null && Directory.Exists(dir))
                start = await tl.StorageProvider.TryGetFolderFromPathAsync(dir);
        }
        catch { /* ignore */ }

        var files = await tl.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Game.log auswählen",
            AllowMultiple = false,
            SuggestedStartLocation = start,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Star Citizen Log") { Patterns = new[] { "Game.log", "*.log" } },
                new FilePickerFileType("Alle Dateien") { Patterns = new[] { "*" } }
            }
        });

        var picked = files.FirstOrDefault()?.TryGetLocalPath();
        if (picked is null) return;

        LogPath = picked;
        RefreshSessions(selectCurrent: true);
        Status = "gewählt: " + picked;
    }

    [RelayCommand]
    private void Toggle()
    {
        if (Running)
        {
            _tailer?.Stop();
            Running = false;
            Status = "gestoppt";
            return;
        }
        LoadSession();
    }

    // Session (neu) laden: Tailer stoppen, alles zurücksetzen, von vorne einlesen.
    void LoadSession()
    {
        _tailer?.Stop();
        Reset();

        if (SelectedSession?.IsAll == true) { LoadAllSessions(); return; }

        _tailer = new LogTailer(LogPath);
        _tailer.Line += OnLine;
        _tailer.Status += s => Dispatcher.UIThread.Post(() => Status = s);
        _tailer.Start(fromStart: true);
        Running = true;
        SyncBlueprints();
    }

    // Alle Sessions: fertige aus der DB (einmal indexiert), laufende live dazu.
    void LoadAllSessions()
    {
        Running = true;
        Status = "lade Archiv…";
        var liveLog = LogPath;
        var dir = Path.GetDirectoryName(liveLog) ?? ".";
        var backupDir = Path.Combine(dir, "logbackups");

        Task.Run(() =>
        {
            try
            {
                var backups = Directory.Exists(backupDir)
                    ? Directory.GetFiles(backupDir, "*.log")
                    : System.Array.Empty<string>();

                // 1) Roh-Logs archivieren, 2) DB öffnen, 3) nur Neues indexieren
                var archived = LogArchive.Sync(backups);
                Database.Init();
                int added = Database.IndexNew(archived);

                // 4) Summen/Top per SQL + ALLE Events für die Tabelle (virtualisiert)
                var agg = Database.Aggregate();
                var topDb = Database.TopMoney(40);
                var recent = Database.LoadAllEvents().ToList();

                Logger.Log($"Alle Sessions: {agg.Sessions} in DB ({added} neu indexiert).");
                FlushUnknownNotifications();

                // 5) Basis aus DB setzen, dann laufende Game.log LIVE oben drauf tailen
                Dispatcher.UIThread.Post(() => ApplyAggregate(agg, topDb, recent, liveLog));
            }
            catch (System.Exception ex)
            {
                Logger.Error("LoadAllSessions", ex);
                Dispatcher.UIThread.Post(() => Status = "Fehler beim Laden – siehe SCLogMate.debug.log");
            }
        });
    }

    // Basis für „Alle": Summen aus DB setzen, dann laufende Game.log live drauf tailen.
    void ApplyAggregate(Database.Agg agg, System.Collections.Generic.List<LogEntry> topDb,
                        System.Collections.Generic.List<LogEntry> recent, string liveLog)
    {
        _allMode = true;
        _dbTop = topDb;
        _dbTrades = Database.AllTrades();
        _liveMoney.Clear();
        RebuildCommodityTrades(_dbTrades);
        RebuildMarketPrices(_dbTrades);
        RebuildMissions(recent.Where(e => e.Kind == EventKind.MissionTaken));

        // Basis-Summen = nur fertige Sessions (DB). Live kommt per Tailer oben drauf.
        TotalIn = agg.In; TotalReward = agg.Reward; TotalSales = agg.Sales;
        TotalTrade = agg.Trade; TotalOut = agg.Out; TotalPurchases = agg.Purchases;
        MissionsDone = agg.MissionsDone;
        _allPlaytime = TimeSpan.FromSeconds(agg.PlaytimeSeconds);
        OnPropertyChanged(nameof(SessionSpanText));
        foreach (var bp in Database.DistinctBlueprints()) _blueprints.Add(bp);
        OnPropertyChanged(nameof(HasBlueprints));
        OnPropertyChanged(nameof(BlueprintsLine));

        RebuildBars();
        SetTopTransactions(topDb.OrderByDescending(e => System.Math.Abs(e.Amount)).Take(8)
                                 .OrderBy(e => System.Math.Abs(e.Amount)));

        foreach (var sh in agg.Ships)
            if (_shipSet.Add(sh)) ShipsSeen.Add(sh);

        // Tabelle: ALLE Events aus DB (neueste zuerst). WICHTIG: in EINEM Rutsch via
        // ReplaceAll → genau EIN Reset an die EventsView, statt ≈60k Einzel-Adds die die
        // View jedes Mal neu filtern/sortieren (das war die „ewig lange"-Bremse).
        Events.ReplaceAll(recent.AsEnumerable().Reverse());

        CurrentLocation = Events.FirstOrDefault(e => e.Kind == EventKind.Location)?.Detail ?? "—";
        CurrentShip = Events.FirstOrDefault(e => e.Kind == EventKind.Vehicle)?.Detail ?? "—";
        LastInventory = Events.FirstOrDefault(e => e.Kind == EventKind.Inventory)?.Detail ?? "—";

        _sessionStart = agg.Start;
        _sessionEnd = agg.End;
        _running = ExpectedBalance;   // Stand vom Eintrag + Bewegungen danach (nicht die ganze Historie)

        foreach (var n in new[] { nameof(IncomeAll), nameof(SpendAll), nameof(NetAll), nameof(NetBalanceText),
                 nameof(NetSign), nameof(FlowText), nameof(TradeText), nameof(ExpectedText), nameof(ExpectedBalance),
                 nameof(SessionSpanText), nameof(FleetText), nameof(ShipsSeenText), nameof(MissionsText) })
            OnPropertyChanged(n);

        SyncBlueprints();

        Status = $"alle Sessions (DB: {agg.Sessions}) – laufende live…";

        // laufende Game.log LIVE dazu tailen (zählt einmal oben drauf)
        _tailer?.Stop();
        _tailer = new LogTailer(liveLog);
        _tailer.Line += OnLine;
        _tailer.Start(fromStart: true);
    }

    static void FlushUnknownNotifications()
    {
        if (LogParser.Unknown.IsEmpty) return;
        Logger.Log($"--- Unbekannte Notification-Typen ({LogParser.Unknown.Count}) – nach Häufigkeit ---");
        foreach (var kv in LogParser.Unknown.OrderByDescending(k => k.Value))
            Logger.Log($"  {kv.Value,5}x  {kv.Key}");
    }

    static System.Collections.Generic.IEnumerable<string> ReadSharedLines(string file)
    {
        FileStream fs;
        try { fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete); }
        catch { yield break; }
        using (fs)
        using (var sr = new StreamReader(fs))
        {
            string? l;
            while ((l = sr.ReadLine()) != null) yield return l;
        }
    }

    void Reset()
    {
        _parser = new LogParser();
        Events.Clear();
        ShipsSeen.Clear();
        _shipSet.Clear();
        _blueprints.Clear();
        OnPropertyChanged(nameof(HasBlueprints));
        OnPropertyChanged(nameof(BlueprintsLine));
        IncomeStats.Clear();
        SpendStats.Clear();
        TopTransactions.Clear();
        RecentMoney.Clear();
        CommodityTrades.Clear();
        MarketPrices.Clear();
        OnPropertyChanged(nameof(HasMarket));
        MissionFactions.Clear();
        _missionTotal = 0;
        OnPropertyChanged(nameof(HasMissionFactions));
        OnPropertyChanged(nameof(MissionsTotalText));
        _dbTrades = new System.Collections.Generic.List<LogEntry>();
        TotalIn = TotalReward = TotalOut = TotalPurchases = TotalSales = TotalTrade = 0;
        MissionsDone = 0;
        CurrentLocation = CurrentShip = "—";
        LastInventory = "—";
        _sessionStart = _sessionEnd = null;
        _allMode = false;
        _dbTop = new System.Collections.Generic.List<LogEntry>();
        _liveMoney.Clear();
        _running = StartBalance();
        OnPropertyChanged(nameof(NetBalanceText));
        OnPropertyChanged(nameof(NetSign));
        OnPropertyChanged(nameof(ExpectedText));
        OnPropertyChanged(nameof(ExpectedBalance));
        OnPropertyChanged(nameof(FlowText));
        OnPropertyChanged(nameof(TradeText));
        OnPropertyChanged(nameof(FleetText));
        OnPropertyChanged(nameof(ShipsSeenText));
        OnPropertyChanged(nameof(SessionSpanText));
    }

    [RelayCommand]
    private async Task ExportCsv()
    {
        var path = await PickSaveAsync("sc-transaktionen.csv", "CSV", "csv");
        if (path == null) return;

        var sb = new StringBuilder();
        sb.AppendLine("Zeit;Typ;Betrag;Detail");
        foreach (var e in Events.Reverse()) // chronologisch (Liste ist neueste zuerst)
            sb.Append(e.TimeText).Append(';')
              .Append(e.KindText).Append(';')
              .Append(e.Amount).Append(';')
              .Append(CsvEscape(e.Detail)).Append('\n');

        await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(true));
        Status = "CSV gespeichert: " + path;
    }

    [RelayCommand]
    private async Task ExportJson()
    {
        var path = await PickSaveAsync("sc-transaktionen.json", "JSON", "json");
        if (path == null) return;

        var report = new
        {
            system = _parser.Meta,
            session = new
            {
                start = _sessionStart,
                end = _sessionEnd,
                source = LogPath
            },
            totals = new
            {
                einnahmen = IncomeAll,
                ausgaben = SpendAll,
                netto = NetAll,
                transfersIn = TotalIn,
                transfersOut = TotalOut,
                verkaeufeItem = TotalSales,
                handelFracht = TotalTrade,
                kaeufe = TotalPurchases
            },
            standort = CurrentLocation,
            schiffAktuell = CurrentShip,
            flotte = ShipsSeen.ToArray(),
            ausruestung = Events.Where(e => e.Kind == EventKind.Loadout).Select(e => e.Detail).ToArray(),
            events = Events.Reverse().Select(e => new
            {
                time = e.Time,
                kind = e.Kind.ToString(),
                amount = e.Amount,
                detail = e.Detail
            })
        };

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        await File.WriteAllTextAsync(path, json, new UTF8Encoding(false));
        Status = "JSON gespeichert: " + path;
    }

    void OnLine(string line)
    {
        _walletCapture.ProcessLine(line);
        var e = _parser.Feed(line);
        if (e == null) return;
        Dispatcher.UIThread.Post(() => Apply(e));
    }

    // Ein erkanntes Ereignis verarbeiten (Totals, Saldo, Flotte, Liste). Immer auf UI-Thread.
    void Apply(LogEntry e)
    {
        {
            if (_sessionStart is null || e.Time < _sessionStart) _sessionStart = e.Time;
            if (_sessionEnd is null || e.Time > _sessionEnd) _sessionEnd = e.Time;

            Events.Insert(0, e);
            if (Events.Count > 100000) Events.RemoveAt(Events.Count - 1);

            switch (e.Kind)
            {
                case EventKind.TransferIn:
                    TotalIn += e.Amount;
                    break;
                case EventKind.MissionReward:
                    TotalReward += e.Amount;
                    HandleMissionCompleted(e.Detail);
                    if (!_initializing && e.Amount > 0)
                    {
                        TriggerAchievementToast(AchievementToastData.ForMissionReward(e.Detail, e.Amount));
                    }
                    break;
                case EventKind.TransferOut:
                    TotalOut += -e.Amount;       // Amount ist negativ
                    break;
                case EventKind.Fine:
                    TotalOut += -e.Amount;       // Bußgeld = aUEC raus
                    break;
                case EventKind.Purchase:
                    TotalPurchases += -e.Amount;
                    ResolveItemName(e);
                    break;
                case EventKind.Sale:
                    TotalSales += e.Amount;
                    ResolveItemName(e);
                    break;
                case EventKind.Trade:
                    TotalTrade += e.Amount;
                    break;
                case EventKind.Location:
                    CurrentLocation = e.Detail;
                    ResolvedLocation = StarmapData.Resolve(e.Detail);
                    OnPropertyChanged(nameof(LocationSystemBadge));
                    OnPropertyChanged(nameof(LocationBadgeColor));
                    OnPropertyChanged(nameof(LocationMainText));
                    OnPropertyChanged(nameof(LocationStatusSubline));
                    break;
                case EventKind.Jurisdiction:
                    if (e.Detail.Contains("🟢") || e.Detail.Contains("Schutzzone aktiv"))
                    {
                        ResolvedLocation.IsArmistice = true;
                    }
                    else if (e.Detail.Contains("🔴") || e.Detail.Contains("Schutzzone verlassen"))
                    {
                        ResolvedLocation.IsArmistice = false;
                    }
                    OnPropertyChanged(nameof(LocationStatusSubline));
                    break;
                case EventKind.Vehicle:
                    CurrentShip = e.Detail;
                    break;
                case EventKind.Inventory:
                    LastInventory = e.Detail;
                    break;
                case EventKind.Mission:
                    // Wenn eine Notification wie "Contract Complete: ..." oder "Auftrag abgeschlossen: ..." reinkommt
                    if (e.Detail.Contains("Complete", StringComparison.OrdinalIgnoreCase) ||
                        e.Detail.Contains("abgeschlossen", StringComparison.OrdinalIgnoreCase) ||
                        e.Detail.Contains("Erfolgreich", StringComparison.OrdinalIgnoreCase))
                    {
                        HandleMissionCompleted(e.Detail);
                    }
                    else if (e.Detail.Contains("Abandoned", StringComparison.OrdinalIgnoreCase) ||
                             e.Detail.Contains("Failed", StringComparison.OrdinalIgnoreCase) ||
                             e.Detail.Contains("Withdrawn", StringComparison.OrdinalIgnoreCase) ||
                             e.Detail.Contains("Cancelled", StringComparison.OrdinalIgnoreCase) ||
                             e.Detail.Contains("abgebrochen", StringComparison.OrdinalIgnoreCase) ||
                             e.Detail.Contains("aufgegeben", StringComparison.OrdinalIgnoreCase) ||
                             e.Detail.Contains("fehlgeschlagen", StringComparison.OrdinalIgnoreCase) ||
                             e.Detail.Contains("zurückgezogen", StringComparison.OrdinalIgnoreCase))
                    {
                        HandleMissionCancelled(e.Detail);
                    }
                    else if (e.Detail.Contains("Accepted", StringComparison.OrdinalIgnoreCase) ||
                             e.Detail.Contains("angenommen", StringComparison.OrdinalIgnoreCase) ||
                             e.Detail.Contains("Shared", StringComparison.OrdinalIgnoreCase) ||
                             e.Detail.Contains("geteilt", StringComparison.OrdinalIgnoreCase))
                    {
                        HandleMissionAccepted(e.Detail, e.Amount);
                    }
                    break;
                case EventKind.MissionDone:
                    HandleMissionCompleted(e.Detail);
                    break;
                case EventKind.Blueprint:
                    AddBlueprint(e.Detail);
                    SyncBlueprints();
                    if (!_initializing)
                    {
                        TriggerAchievementToast(AchievementToastData.ForBlueprint(e.Detail));
                    }
                    break;
                case EventKind.MissionTaken:
                    RebuildMissions(Events.Where(x => x.Kind == EventKind.MissionTaken));
                    break;
                case EventKind.Crash:
                    ClearContracts();
                    ContractStatusText = "⚠ Spiel abgestürzt – Aufträge zurückgesetzt";
                    Status = "⚠ Star Citizen Absturz erkannt! Aktive Aufträge wurden zurückgesetzt.";
                    break;
                case EventKind.SessionChange:
                    Status = e.Detail ?? "Server-/Session-Wechsel";
                    break;
            }

            // Mitlaufender Kontostand bei Geld-Ereignissen
            if (IsMoney(e.Kind))
            {
                _running += e.Amount;
                e.BalanceAfter = _running;
                e.HasBalance = true;

                // Live-Kontostand direkt mitführen, wenn ein Betrag gesetzt ist
                if (!_initializing && StartBalance() > 0)
                {
                    long current = StartBalance();
                    long newBal = Math.Max(0, current + e.Amount);
                    ManualBalance = newBal.ToString("N0");
                    _settings.Balance = newBal;
                    _settings.BalanceSetAt = e.Time;
                    Settings.Save(_settings);
                }

                if (_allMode)
                {
                    _liveMoney.Add(e);
                    RebuildBars();
                    SetTopTransactions(_dbTop.Concat(_liveMoney)
                        .OrderByDescending(x => System.Math.Abs(x.Amount)).Take(8)
                        .OrderBy(x => System.Math.Abs(x.Amount)));
                    if (e.Kind == EventKind.Trade)
                    {
                        RebuildCommodityTrades(_dbTrades.Concat(_liveMoney.Where(x => x.Kind == EventKind.Trade)));
                        RebuildMarketPrices(_dbTrades.Concat(_liveMoney.Where(x => x.Kind == EventKind.Trade)));
                    }
                }
                else RebuildStats();
            }

            // Flotte: Schiffe aus Vehicle- UND Quantum-Events sammeln
            if (!string.IsNullOrEmpty(e.Ship) && _shipSet.Add(e.Ship))
            {
                ShipsSeen.Add(e.Ship);
                OnPropertyChanged(nameof(FleetText));
                OnPropertyChanged(nameof(ShipsSeenText));
            }

            OnPropertyChanged(nameof(NetBalanceText));
            OnPropertyChanged(nameof(NetSign));
            OnPropertyChanged(nameof(FlowText));
            OnPropertyChanged(nameof(TradeText));
            OnPropertyChanged(nameof(SessionIncomeText));
            OnPropertyChanged(nameof(SessionSpendText));
            OnPropertyChanged(nameof(SessionNetText));
            OnPropertyChanged(nameof(SessionNetSign));
            OnPropertyChanged(nameof(LiveBalanceText));
            OnPropertyChanged(nameof(ExpectedText));
            OnPropertyChanged(nameof(ExpectedBalance));
            OnPropertyChanged(nameof(SessionSpanText));
            OnPropertyChanged(nameof(ServerShardName));
            OnPropertyChanged(nameof(ServerShardNumber));
            OnPropertyChanged(nameof(ServerRegionFlag));
            OnPropertyChanged(nameof(ServerRegionName));
            OnPropertyChanged(nameof(ServerRegionCode));
            OnPropertyChanged(nameof(IsRegionEu));
            OnPropertyChanged(nameof(IsRegionUs));
            OnPropertyChanged(nameof(IsRegionAus));
            OnPropertyChanged(nameof(IsRegionAsia));
            OnPropertyChanged(nameof(IsRegionOther));
            OnPropertyChanged(nameof(ScVersionText));
            OnPropertyChanged(nameof(ScPlayerName));
            OnPropertyChanged(nameof(ServerBadgeText));
            OnPropertyChanged(nameof(ServerMainText));
            OnPropertyChanged(nameof(ServerSublineText));
            OnPropertyChanged(nameof(ServerTooltipText));
            OnPropertyChanged(nameof(MetaSummary));
        }
    }

    private void HandleMissionCompleted(string? detail)
    {
        MissionsDone++;
        OnPropertyChanged(nameof(MissionsText));

        // Titel aus Notification extrahieren (z. B. "Contract Complete: Ship In Distress" -> "Ship In Distress")
        var missionTitle = detail ?? "";
        foreach (var prefix in new[] { "Contract Complete: ", "Contract Complete:", "Contract Completed: ", "Mission Complete: ", "Mission Completed: ", "Auftrag abgeschlossen: ", "Auftrag abgeschlossen", "Mission completed: ", "Mission completed", "Belohnung: ", "Belohnung:" })
        {
            if (missionTitle.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                missionTitle = missionTitle[prefix.Length..].Trim();
                break;
            }
        }

        ContractDetails? matchContract = null;
        if (!string.IsNullOrWhiteSpace(missionTitle) && !missionTitle.StartsWith("(Belohnung", StringComparison.OrdinalIgnoreCase))
        {
            var dummy = new ContractDetails { Title = missionTitle };
            matchContract = ActiveContracts.FirstOrDefault(c => ContractParser.AreSameContract(c, dummy));
            if (matchContract == null)
            {
                var norm = ContractParser.NormalizeTitle(missionTitle);
                if (norm.Length >= 4)
                {
                    matchContract = ActiveContracts.FirstOrDefault(c =>
                    {
                        var cNorm = ContractParser.NormalizeTitle(c.Title);
                        return cNorm.Contains(norm) || norm.Contains(cNorm);
                    });
                }
            }
        }

        // NUR entfernen, wenn tatsächlich ein passender aktiver Auftrag gefunden wurde!
        if (matchContract != null)
        {
            var reward = matchContract.Reward;
            var completedTitle = matchContract.Title;

            ActiveContracts.Remove(matchContract);
            Database.RemoveContract(matchContract.Title, matchContract.Reward);
            OnPropertyChanged(nameof(HasActiveContracts));
            OnPropertyChanged(nameof(ActiveContractsCountText));

            if (ActiveContracts.Count > 0)
            {
                var next = ActiveContracts[0];
                ActiveContractTitle = next.Title;
                ActiveContractRewardText = next.RewardText;
                ActiveContractOrg = next.ContractedBy;
                HasActiveContract = true;
                ContractStatusText = $"✓ {ActiveContracts.Count} Auftrag/Aufträge";
            }
            else
            {
                HasActiveContract = false;
                ActiveContractTitle = "— Kein aktiver Auftrag —";
                ActiveContractRewardText = "—";
                ActiveContractOrg = "";
                ContractStatusText = "Alle Aufträge abgeschlossen";
            }

            if (reward > 0)
            {
                Status = $"★ Auftrag abgeschlossen & Belohnung verbucht: {completedTitle} · +{reward:N0} aUEC";
            }
            else
            {
                Status = $"★ Auftrag abgeschlossen: {completedTitle}";
            }
        }
    }

    private void HandleMissionCancelled(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail)) return;

        var missionTitle = detail;
        foreach (var prefix in new[]
        {
            "Contract Abandoned: ", "Contract Abandoned:",
            "Contract Failed: ", "Contract Failed:",
            "Contract Withdrawn: ", "Contract Withdrawn:",
            "Contract Cancelled: ", "Contract Cancelled:",
            "Auftrag abgebrochen: ", "Auftrag abgebrochen",
            "Auftrag aufgegeben: ", "Auftrag aufgegeben",
            "Auftrag fehlgeschlagen: ", "Auftrag fehlgeschlagen",
            "Auftrag zurückgezogen: ", "Auftrag zurückgezogen"
        })
        {
            if (missionTitle.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                missionTitle = missionTitle[prefix.Length..].Trim();
                break;
            }
        }

        ContractDetails? matchContract = null;
        if (!string.IsNullOrWhiteSpace(missionTitle))
        {
            var dummy = new ContractDetails { Title = missionTitle };
            matchContract = ActiveContracts.FirstOrDefault(c => ContractParser.AreSameContract(c, dummy));
            if (matchContract == null)
            {
                var norm = ContractParser.NormalizeTitle(missionTitle);
                if (norm.Length >= 4)
                {
                    matchContract = ActiveContracts.FirstOrDefault(c =>
                    {
                        var cNorm = ContractParser.NormalizeTitle(c.Title);
                        return cNorm.Contains(norm) || norm.Contains(cNorm);
                    });
                }
            }
        }

        if (matchContract != null)
        {
            var title = matchContract.Title;
            ActiveContracts.Remove(matchContract);
            Database.RemoveContract(matchContract.Title, matchContract.Reward);
            OnPropertyChanged(nameof(HasActiveContracts));
            OnPropertyChanged(nameof(ActiveContractsCountText));

            if (ActiveContracts.Count > 0)
            {
                var next = ActiveContracts[0];
                ActiveContractTitle = next.Title;
                ActiveContractRewardText = next.RewardText;
                ActiveContractOrg = next.ContractedBy;
                HasActiveContract = true;
                ContractStatusText = $"✓ {ActiveContracts.Count} Auftrag/Aufträge";
            }
            else
            {
                HasActiveContract = false;
                ActiveContractTitle = "— Kein aktiver Auftrag —";
                ActiveContractRewardText = "—";
                ActiveContractOrg = "";
                ContractStatusText = "Keine aktiven Aufträge";
            }

            Status = $"✕ Auftrag abgebrochen/aufgegeben: {title}";
        }
        else
        {
            Status = $"✕ {detail}";
        }
    }

    private void HandleMissionAccepted(string? detail, long reward = 0)
    {
        if (string.IsNullOrWhiteSpace(detail)) return;

        var prefixes = new[] {
            "Contract Accepted: ", "Contract Accepted:",
            "Contract Shared: ", "Contract Shared:",
            "Auftrag angenommen: ", "Auftrag angenommen",
            "Auftrag geteilt: ", "Auftrag geteilt"
        };

        string? matchedPrefix = null;
        foreach (var prefix in prefixes)
        {
            if (detail.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                matchedPrefix = prefix;
                break;
            }
        }

        // NUR Meldungen mit explizitem Contract-Prefix verarbeiten (z. B. "Contract Accepted: ...")
        if (matchedPrefix == null) return;

        var missionTitle = detail[matchedPrefix.Length..].Trim();
        var norm = ContractParser.NormalizeTitle(missionTitle);
        if (norm.Length < 3) return;

        // Aus Missionskatalog Details & Belohnung nachschlagen
        var cat = MissionCatalog.FuzzyLookup(missionTitle);
        int finalReward = (int)(reward > 0 ? reward : (cat?.BaseReward ?? 0));
        string org = cat != null && !string.IsNullOrEmpty(cat.Contractor) ? cat.Contractor : (cat?.Faction ?? "Recco Battaglia");

        var contract = new ContractDetails
        {
            Title = cat?.Title ?? missionTitle,
            Reward = finalReward,
            ContractedBy = org,
            ScannedAt = DateTime.UtcNow
        };

        // Deduplizierung: Gibt es den Auftrag schon in der aktiven Liste?
        var existing = ActiveContracts.FirstOrDefault(c => ContractParser.AreSameContract(c, contract));
        if (existing != null)
        {
            if (finalReward > 0 && existing.Reward <= 0)
            {
                existing.Reward = finalReward;
                Database.SaveContract(existing);
            }
            return;
        }

        ActiveContracts.Insert(0, contract);
        if (ActiveContracts.Count > 25) ActiveContracts.RemoveAt(ActiveContracts.Count - 1);

        Database.SaveContract(contract);
        _knownContracts[norm] = finalReward;

        HasActiveContract = true;
        ActiveContractTitle = contract.Title;
        ActiveContractRewardText = contract.RewardText;
        ActiveContractOrg = contract.ContractedBy;
        ContractStatusText = ActiveContracts.Count == 1 ? "1 aktiver Auftrag" : $"{ActiveContracts.Count} aktive Aufträge";

        OnPropertyChanged(nameof(HasActiveContracts));
        OnPropertyChanged(nameof(ActiveContractsCountText));
        Status = $"★ Auftrag angenommen: {contract.Title} · {contract.RewardText}";
    }

    [RelayCommand]
    public void OpenMissionsTab()
    {
        SelectedTabIndex = 2; // Tab '❖ Missionen'
    }

    // Item-Namen live über UEX nachladen und den Eintrag aktualisieren.
    async void ResolveItemName(LogEntry e)
    {
        var name = await ItemNames.ResolveAsync(e.ItemRef);
        if (name is null) return;
        Dispatcher.UIThread.Post(() => e.Detail = $"{name}  {e.Suffix}");
    }

    static async Task<string?> PickSaveAsync(string suggested, string typeName, string ext)
    {
        var tl = UiServices.TopLevel;
        if (tl is null) return null;

        var file = await tl.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = suggested,
            DefaultExtension = ext,
            FileTypeChoices = new[]
            {
                new FilePickerFileType(typeName) { Patterns = new[] { "*." + ext } }
            }
        });

        return file?.TryGetLocalPath();
    }

    static string CsvEscape(string s) =>
        s.Contains(';') || s.Contains('"') || s.Contains('\n')
            ? "\"" + s.Replace("\"", "\"\"") + "\""
            : s;

    static string ChannelOf(string path)
    {
        var dir = Path.GetDirectoryName(path);
        return dir is null ? path : Path.GetFileName(dir);
    }

    partial void OnCurrentLocationChanged(string value)
    {
        ResolvedLocation = StarmapData.Resolve(value);
        if (!string.IsNullOrEmpty(ResolvedLocation.SystemName) && StarmapData.SystemNames.Contains(ResolvedLocation.SystemName))
        {
            SelectedStarmapSystem = ResolvedLocation.SystemName;
        }
        OnPropertyChanged(nameof(LocationSystemBadge));
        OnPropertyChanged(nameof(LocationBadgeColor));
        OnPropertyChanged(nameof(LocationMainText));
        OnPropertyChanged(nameof(LocationStatusSubline));
        OnPropertyChanged(nameof(SearchStarmapResults));
    }

    partial void OnSelectedStarmapSystemChanged(string value)
    {
        SelectedStarmapObject = null;
        OnPropertyChanged(nameof(CurrentSystemObjects));
        OnPropertyChanged(nameof(SearchStarmapResults));
    }

    partial void OnStarmapSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(SearchStarmapResults));
    }

    async partial void OnSelectedStarmapObjectChanged(StarmapObject? value)
    {
        if (value == null)
        {
            SelectedStarmapUexInfo = null;
            return;
        }
        SelectedStarmapUexInfo = await UexApiClient.LookupLocationAsync(value.Name);
    }

    [RelayCommand]
    private void SelectStarmapSystem(string system)
    {
        SelectedStarmapSystem = system;
    }

    [RelayCommand]
    private void SelectStarmapObject(StarmapObject? obj)
    {
        SelectedStarmapObject = obj;
    }

    [RelayCommand]
    private void OpenStarmapForCurrentLocation()
    {
        if (!string.IsNullOrEmpty(ResolvedLocation.SystemName) && StarmapData.SystemNames.Contains(ResolvedLocation.SystemName))
        {
            SelectedStarmapSystem = ResolvedLocation.SystemName;
        }
        SelectedStarmapObject = StarmapData.FindObject(ResolvedLocation.DisplayName) ?? StarmapData.FindObject(ResolvedLocation.ParentBody);
        SelectedTabIndex = 3; // Neuer Tab '🗺 Karte'
    }

    [RelayCommand]
    private void LookupStarmapOnUex()
    {
        var target = SelectedStarmapObject?.Name ?? ResolvedLocation.DisplayName;
        if (string.IsNullOrWhiteSpace(target) || target == "—") return;
        var url = "https://uexcorp.space/trade/price_finder?location=" + Uri.EscapeDataString(target);
        OpenUrl(url);
    }

    partial void OnUexApiKeyInputChanged(string value)
    {
        if (_initializing || !_ready) return;
        var cleanKey = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        _settings.UexApiKey = cleanKey;
        Settings.Save(_settings);
        UexApiClient.SetApiKey(cleanKey);
    }

    partial void OnMinimizeToTrayOnCloseChanged(bool value)
    {
        if (_initializing || !_ready) return;
        _settings.MinimizeToTrayOnClose = value;
        Settings.Save(_settings);
    }

    [RelayCommand]
    public async Task SaveAndTestUexApiKey()
    {
        var cleanKey = string.IsNullOrWhiteSpace(UexApiKeyInput) ? null : UexApiKeyInput.Trim();
        _settings.UexApiKey = cleanKey;
        Settings.Save(_settings);
        UexApiKeyInput = cleanKey ?? "";
        UexApiClient.SetApiKey(cleanKey);

        UexStatusMessage = "Prüfe UEX Corp API-Verbindung...";
        UexStatusColor = "#58A6FF";

        var (success, msg) = await UexApiClient.TestConnectionAsync(cleanKey);
        UexStatusMessage = msg;
        UexStatusColor = success ? "#4ADE80" : "#F87171";
        Status = $"UEX API: {msg}";
    }

    async partial void OnCurrentShipChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "—")
        {
            CurrentShipWiki = null;
            return;
        }
        CurrentShipWiki = await WikiApiClient.LookupAsync(value);
    }

    [RelayCommand]
    public async Task OpenWiki(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query == "—") return;
        IsWikiLoading = true;
        IsWikiOverlayOpen = true;
        WikiImageBitmap = null;
        try
        {
            SelectedWikiInfo = await WikiApiClient.LookupAsync(query);
            if (SelectedWikiInfo != null)
            {
                var imgUrl = !string.IsNullOrEmpty(SelectedWikiInfo.ImageUrl) ? SelectedWikiInfo.ImageUrl : SelectedWikiInfo.ThumbnailUrl;
                if (!string.IsNullOrEmpty(imgUrl))
                {
                    WikiImageBitmap = await ImageLoaderService.LoadBitmapAsync(imgUrl);
                }
            }
        }
        catch { /* ignore */ }
        finally
        {
            IsWikiLoading = false;
        }
    }

    [RelayCommand]
    private async Task ShowShipWiki()
    {
        var target = CurrentShip != "—" ? CurrentShip : SelectedWikiInfo?.Name ?? "";
        if (!string.IsNullOrWhiteSpace(target))
        {
            await OpenWiki(target);
        }
    }

    [RelayCommand]
    private async Task LookupWikiForEntry(LogEntry? entry)
    {
        var target = entry?.Detail ?? SelectedEntry?.Detail;
        if (!string.IsNullOrWhiteSpace(target))
        {
            await OpenWiki(target);
        }
    }

    [RelayCommand]
    private void CloseWikiOverlay()
    {
        IsWikiOverlayOpen = false;
        WikiImageBitmap = null;
    }

    [RelayCommand]
    private void OpenWikiWebUrl(string? url)
    {
        var target = url ?? SelectedWikiInfo?.WebUrl;
        if (!string.IsNullOrWhiteSpace(target))
        {
            OpenUrl(target);
        }
    }

    [RelayCommand]
    public void ToggleOverlay()
    {
        IsOverlayActive = !IsOverlayActive;
    }

    partial void OnIsOverlayActiveChanged(bool value)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (value)
            {
                if (_overlayWindow == null)
                {
                    _overlayWindow = new Views.FloatingOverlayWindow { DataContext = this };
                    _overlayWindow.InitSettings(_settings);
                }
                _overlayWindow.Show();
                _settings.OverlayEnabled = true;
                Settings.Save(_settings);
            }
            else
            {
                _overlayWindow?.Hide();
                _settings.OverlayEnabled = false;
                Settings.Save(_settings);
            }
        });
    }

    partial void OnOverlayOpacityChanged(double value)
    {
        if (_overlayWindow != null)
        {
            _overlayWindow.Opacity = Math.Clamp(value, 0.3, 1.0);
            _settings.OverlayOpacity = value;
            Settings.Save(_settings);
        }
    }

    public void TriggerAchievementToast(AchievementToastData toast)
    {
        if (!ToastOverlayEnabled) return;
        Dispatcher.UIThread.Post(() =>
        {
            if (_toastWindow == null)
            {
                _toastWindow = new Views.AchievementToastWindow();
                _toastWindow.InitSettings(_settings);
            }
            _toastWindow.ShowToast(toast, _settings.ToastDurationSeconds);
        });
    }

    private int _testToastCounter;

    [RelayCommand]
    public void TestAchievementToast()
    {
        _testToastCounter++;
        switch (_testToastCounter % 3)
        {
            case 1:
                TriggerAchievementToast(AchievementToastData.ForBlueprint("Strata Helmet Levski Edition"));
                break;
            case 2:
                TriggerAchievementToast(AchievementToastData.ForMissionReward("Missing Mining Team", 26750));
                break;
            case 0:
                TriggerAchievementToast(AchievementToastData.ForLoot("Pyro RYT Multi-Tool (Ghost Edition)"));
                break;
        }
        Status = "✦ Achievement-Banner Test ausgelöst – klicke und ziehe das Banner zum Verschieben!";
    }

    [RelayCommand]
    public void ToggleToastOverlay()
    {
        ToastOverlayEnabled = !ToastOverlayEnabled;
        _settings.ToastEnabled = ToastOverlayEnabled;
        Settings.Save(_settings);
        Status = ToastOverlayEnabled ? "✦ Achievement-Banner aktiviert" : "Achievement-Banner deaktiviert";
    }


    partial void OnBlueprintSearchTextChanged(string value) => RefreshBlueprintFilter();
    partial void OnSelectedBlueprintCategoryChanged(string value) => RefreshBlueprintFilter();

    public void RefreshBlueprintFilter()
    {
        BlueprintsView?.Refresh();
    }

    [RelayCommand]
    public void SelectBlueprintCategory(string category)
    {
        SelectedBlueprintCategory = category;
    }

    public void SyncBlueprints()
    {
        var dbBps = Database.AllBlueprintEvents();
        var liveBps = Events.Where(e => e.Kind == EventKind.Blueprint);
        var allBps = dbBps.Concat(liveBps).ToList();

        BlueprintCatalog.Sync(BlueprintCatalogList, allBps);
        OnPropertyChanged(nameof(LearnedBlueprintsCount));
        OnPropertyChanged(nameof(MissingBlueprintsCount));
        OnPropertyChanged(nameof(TotalBlueprintsCount));
        OnPropertyChanged(nameof(BlueprintProgressPercent));
        OnPropertyChanged(nameof(BlueprintProgressText));
        BlueprintsView?.Refresh();
    }

    partial void OnMissionSearchTextChanged(string value) => RefreshMissionFilter();
    partial void OnSelectedMissionTypeChanged(string value) => RefreshMissionFilter();

    public void RefreshMissionFilter()
    {
        MissionsView?.Refresh();
    }

    [RelayCommand]
    public void SelectMissionType(string type)
    {
        SelectedMissionType = type;
    }
}
