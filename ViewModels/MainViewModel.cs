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
using SCLogMate.Core;
using SCLogMate.Core.Ocr;
using SCLogMate.Models;
using SCLogMate.Services;
using SCLogMate.Views;

namespace SCLogMate.ViewModels;

public partial class MainViewModel : ObservableObject
{
    LogTailer? _tailer;
    LogParser _parser = new();
    readonly OcrEngineService _ocrEngine = new();
    readonly WalletCapture _walletCapture;
    readonly ContractScanner _contractScanner;
    readonly RsOcrScanner _rsScanner;
    readonly AuroraVoiceService _auroraService;
    readonly ScanIndicatorWindow _scanIndicator = new();
    private Views.RsScanOverlayWindow? _rsOverlayWindow;
    bool _initializing = true;
    bool _ready;          // Persistenz erst nach Konstruktor
    bool _suppressSave;   // Session-Wechsel nicht als Default speichern
    DateTime? _sessionStart, _sessionEnd;
    AppSettings _settings = Settings.Load();

    [ObservableProperty] private bool isAuroraInstalled;
    [ObservableProperty] private string auroraPath = "Nicht installiert";
    [ObservableProperty] private bool isAuroraEnabled;
    [ObservableProperty] private int auroraVolume = 40;
    [ObservableProperty] private bool isAuroraShipGreetingsEnabled = true;
    [ObservableProperty] private bool isAuroraBlueprintsEnabled = true;
    [ObservableProperty] private bool isAuroraSafetyZonesEnabled = true;
    [ObservableProperty] private bool isAuroraRestrictedZonesEnabled = true;
    [ObservableProperty] private bool isAuroraMonitoredSpaceEnabled = true;
    [ObservableProperty] private bool isAuroraJurisdictionsEnabled = true;
    [ObservableProperty] private bool isAuroraQuantumArrivalEnabled = true;
    [ObservableProperty] private bool isAuroraPlayerDeathEnabled = true;
    [ObservableProperty] private bool isAuroraServerErrorsEnabled = true;
    [ObservableProperty] private bool simulateAuroraNotInstalled;

    /// <summary>Gibt an, ob der Entwickler- & Debug-Modus aktiv ist (über settings.json konfiguriert oder lokaler Debug-Build).</summary>
    public bool IsDebugModeActive => _settings.DebugMode;

    /// <summary>Kauf-Banner nur anzeigen, wenn Aurora nicht installiert ist (oder im Debug-Modus simuliert wird) UND Deutsch als Sprache aktiv ist.</summary>
    public bool ShowAuroraPurchaseBanner => (!IsAuroraInstalled || (IsDebugModeActive && SimulateAuroraNotInstalled)) && IsGermanLanguageActive;

    partial void OnIsAuroraInstalledChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowAuroraPurchaseBanner));
    }

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
    [ObservableProperty] private bool isUpdateModalOpen;
    [ObservableProperty] private string updateNewVersion = "";
    [ObservableProperty] private string updateCurrentVersion = "";
    [ObservableProperty] private string updateReleaseNotes = "";
    [ObservableProperty] private string updateStatusText = "";
    [ObservableProperty] private bool isInstallingUpdate;
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> _knownContracts = new(StringComparer.OrdinalIgnoreCase);
    Updater.Info? _update;
    Avalonia.Threading.DispatcherTimer? _updateTimer;
    Avalonia.Threading.DispatcherTimer? _pingTimer;
    Avalonia.Threading.DispatcherTimer? _processTimer;
    readonly DispatcherTimer _eventSearchTimer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    readonly object _initialEventQueueLock = new();
    readonly List<(LogEntry Entry, bool IsLive)> _initialEventQueue = new();
    bool _initialEventDrainScheduled;
    [ObservableProperty] private SessionInfo? selectedSession;
    [ObservableProperty] private string currentLocation = "—";
    [ObservableProperty] private string currentShip = "—";
    [ObservableProperty] private string lastInventory = "—";

    [ObservableProperty] private ResolvedLocation resolvedLocation = new();

    partial void OnCurrentLocationChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "—")
        {
            ResolvedLocation = new ResolvedLocation { DisplayName = "—", SystemName = "Stanton", ParentBody = "—" };
        }
        else
        {
            ResolvedLocation = StarmapData.Resolve(value);
        }

        if (!string.IsNullOrEmpty(ResolvedLocation.SystemName) && StarmapData.SystemNames.Contains(ResolvedLocation.SystemName))
        {
            SelectedStarmapSystem = ResolvedLocation.SystemName;
        }

        OnPropertyChanged(nameof(SearchStarmapResults));
    }

    partial void OnResolvedLocationChanged(ResolvedLocation value)
    {
        if (_auroraService != null)
        {
            _auroraService.IsAtStation = value.Type is StarmapObjectType.SpaceStation 
                or StarmapObjectType.LagrangeStation 
                or StarmapObjectType.LandingZone 
                or StarmapObjectType.JumpPoint 
                or StarmapObjectType.Outpost
                || value.DisplayName.Contains("Station", StringComparison.OrdinalIgnoreCase)
                || value.DisplayName.Contains("Port", StringComparison.OrdinalIgnoreCase)
                || value.DisplayName.Contains("Harbor", StringComparison.OrdinalIgnoreCase)
                || value.DisplayName.Contains("Point", StringComparison.OrdinalIgnoreCase)
                || value.DisplayName.Contains("Hangar", StringComparison.OrdinalIgnoreCase)
                || value.DisplayName.Contains("Rest Stop", StringComparison.OrdinalIgnoreCase);
        }

        OnPropertyChanged(nameof(LocationSystemBadge));
        OnPropertyChanged(nameof(LocationBadgeColor));
        OnPropertyChanged(nameof(LocationMainText));
        OnPropertyChanged(nameof(LocationStatusSubline));
        OnPropertyChanged(nameof(LocationTypeBadge));
        OnPropertyChanged(nameof(LocationParentBadge));
        OnPropertyChanged(nameof(LocationArmisticePillText));
        OnPropertyChanged(nameof(LocationArmisticePillColor));
        OnPropertyChanged(nameof(LocationArmisticePillBg));
        OnPropertyChanged(nameof(LocationArmisticePillBorder));
        OnPropertyChanged(nameof(ArmisticeBorderColor));
        OnPropertyChanged(nameof(LocationIsKnown));
    }


    public bool LocationIsKnown => ResolvedLocation.DisplayName != "—" && ResolvedLocation.DisplayName != "Unbekannt";
    public string LocationSystemBadge => ResolvedLocation.SystemName.ToUpperInvariant();
    public string LocationBadgeColor => ResolvedLocation.SystemBadgeColor;
    public string ArmisticeBorderColor => ResolvedLocation.IsArmistice == false ? "#EF4444" : "#38BDF8";
    public string LocationMainText => ResolvedLocation.DisplayName == "—" ? "—" : 
        string.IsNullOrEmpty(ResolvedLocation.ParentBody) || ResolvedLocation.ParentBody == "—" || ResolvedLocation.ParentBody == ResolvedLocation.DisplayName
            ? ResolvedLocation.DisplayName 
            : $"{ResolvedLocation.DisplayName} · {ResolvedLocation.ParentBody}";
    public string LocationStatusSubline => ResolvedLocation.DisplayName == "—" 
        ? I18n["Dash_Waiting_Logs"] 
        : $"{ResolvedLocation.ArmisticeStatusText} · {ResolvedLocation.SystemName}";

    public string LocationTypeBadge => ResolvedLocation.Type switch
    {
        StarmapObjectType.LandingZone => I18n["Dash_Type_LandingZone"],
        StarmapObjectType.SpaceStation => I18n["Dash_Type_SpaceStation"],
        StarmapObjectType.LagrangeStation => I18n["Dash_Type_RestStop"],
        StarmapObjectType.Moon => I18n["Dash_Type_Moon"],
        StarmapObjectType.Planet => I18n["Dash_Type_Planet"],
        StarmapObjectType.JumpPoint => I18n["Dash_Type_JumpPoint"],
        _ => I18n["Dash_Type_Default"]
    };

    public string LocationParentBadge => !string.IsNullOrEmpty(ResolvedLocation.ParentBody) && ResolvedLocation.ParentBody != "—" && ResolvedLocation.ParentBody != ResolvedLocation.DisplayName
        ? ResolvedLocation.ParentBody
        : ResolvedLocation.SystemName;

    public string LocationArmisticePillText => ResolvedLocation.IsArmistice ? I18n["Dash_Armistice_Safe"] : I18n["Dash_Armistice_Free"];
    public string LocationArmisticePillColor => ResolvedLocation.IsArmistice ? "#4ADE80" : "#F87171";
    public string LocationArmisticePillBg => ResolvedLocation.IsArmistice ? "#0A2416" : "#240A0E";
    public string LocationArmisticePillBorder => ResolvedLocation.IsArmistice ? "#1E5E3A" : "#5E1E28";

    // Starmap-Properties
    [ObservableProperty] private string selectedStarmapSystem = "Stanton";
    [ObservableProperty] private StarmapObject? selectedStarmapObject;
    [ObservableProperty] private int starmapFocusRequest;
    [ObservableProperty] private string starmapSearchText = "";
    [ObservableProperty] private bool showStarmapStations = true;
    [ObservableProperty] private bool showStarmapMoons = true;
    [ObservableProperty] private bool showStarmapLandingZones = true;
    [ObservableProperty] private bool showStarmapJumpPoints = true;
    [ObservableProperty] private int selectedTabIndex = 0;

    public IReadOnlyList<QuantumDriveProfile> AvailableQuantumDrives => StarmapData.AvailableDrives;
    [ObservableProperty] private QuantumDriveProfile selectedQuantumDrive = StarmapData.AvailableDrives[0];
    [ObservableProperty] private bool isStarmapFullscreen = false;
    public bool ShowDashboardCards => SelectedTabIndex != 4 || !IsStarmapFullscreen;

    partial void OnSelectedTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(ShowDashboardCards));
    }

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

    // In-Game Achievement & Reward Toast Banner (Völlig unabhängig vom Mini-HUD)
    [ObservableProperty] private bool toastOverlayEnabled = true;
    [ObservableProperty] private bool toastBlueprintEnabled = true;
    [ObservableProperty] private bool toastMissionEnabled = true;
    [ObservableProperty] private bool toastReputationEnabled = true;
    [ObservableProperty] private bool toastRefineryEnabled = true;
    [ObservableProperty] private bool toastElevatorEnabled = true;
    [ObservableProperty] private bool toastShipDestructionEnabled = true;
    [ObservableProperty] private bool toastSoundEnabled = false;
    private Views.AchievementToastWindow? _toastWindow;

    // HUD-Sperre, Click-Through & Hotkey
    [ObservableProperty] private bool overlayLocked = false;
    [ObservableProperty] private bool overlayClickThrough = false;
    [ObservableProperty] private bool globalHotkeyEnabled = true;

    // Raffinerie-Tracking & Handelsrouten
    public ObservableCollection<RefineryJob> RefineryJobs { get; } = new();
    public ObservableCollection<TradeRouteItem> TradeRoutes { get; } = new();

    // RS Signal Scanner & Mining/Salvage Decoder
    [ObservableProperty] private string rsInputText = "";
    [ObservableProperty] private int? currentRsValue;
    [ObservableProperty] private RsMatch? bestRsMatch;
    [ObservableProperty] private bool isRsOverlayActive;
    [ObservableProperty] private bool isRsAutoScanEnabled;
    [ObservableProperty] private string rsOcrStatusText = "OCR bereit";
    public ObservableCollection<RsMatch> CurrentRsMatches { get; } = new();

    public string CurrentRsDisplayValue => CurrentRsValue.HasValue ? $"RS {CurrentRsValue.Value:N0}" : "—";
    public string RsAutoScanStatusText => IsRsAutoScanEnabled ? "⚡ AUTO" : "MANUELL";
    public string RsAutoScanBadgeBg => IsRsAutoScanEnabled ? "#064E3B" : "#1E293B";
    public string RsAutoScanBadgeBorder => IsRsAutoScanEnabled ? "#34D399" : "#475569";
    public string RsAutoScanBadgeFg => IsRsAutoScanEnabled ? "#34D399" : "#94A3B8";
    public string SecondaryRsMatchesText => CurrentRsMatches.Count > 1 
        ? string.Join(" · ", CurrentRsMatches.Skip(1).Take(2).Select(m => $"{m.Nodes}x {m.Resource.Name}")) 
        : "Keine weiteren Übereinstimmungen";

    public List<string> AvailableRefineryStations => RefineryCatalog.Stations;
    public List<string> AvailableRefineryMaterials => RefineryCatalog.Materials;
    public List<string> AvailableRefineryMethods => RefineryCatalog.Methods;
    [ObservableProperty] private string newRefineryStation = RefineryCatalog.Stations[0];
    [ObservableProperty] private string newRefineryMaterial = RefineryCatalog.Materials[0];
    [ObservableProperty] private string newRefineryMethod = RefineryCatalog.Methods[0];
    [ObservableProperty] private int newRefineryScu = 32;
    [ObservableProperty] private int selectedTradeShipCapacity = 696;
    private DispatcherTimer? _refineryTimer;

    // Fenster- & System-Verhalten & Schriftart
    [ObservableProperty] private bool minimizeToTrayOnClose = true;
    [ObservableProperty] private bool autostartEnabled = false;

    public ObservableCollection<string> AvailableFonts { get; } = new()
    {
        "Inter (Modern & Klar)",
        "Segoe UI (Windows Standard)",
        "Cascadia Code (Monospace)",
        "Consolas (Classic Code)",
        "Bahnschrift (Clean Tech)",
        "Arial (Universal Sans)",
        "Verdana (Groß & Lesbar)"
    };

    [ObservableProperty] private string selectedFontOption = "Inter (Modern & Klar)";

    public string SelectedFontFamilyName => SelectedFontOption switch
    {
        "Segoe UI (Windows Standard)" => "Segoe UI",
        "Cascadia Code (Monospace)" => "Cascadia Code",
        "Consolas (Classic Code)" => "Consolas",
        "Bahnschrift (Clean Tech)" => "Bahnschrift",
        "Arial (Universal Sans)" => "Arial",
        "Verdana (Groß & Lesbar)" => "Verdana",
        _ => "Inter"
    };

    public Avalonia.Media.FontFamily CurrentFontFamily => new Avalonia.Media.FontFamily(SelectedFontFamilyName);

    partial void OnSelectedFontOptionChanged(string value)
    {
        if (_initializing) return;
        _settings.SelectedFontFamily = SelectedFontFamilyName;
        Settings.Save(_settings);
        OnPropertyChanged(nameof(SelectedFontFamilyName));
        OnPropertyChanged(nameof(CurrentFontFamily));
    }

    // Sprach-Umschalter / Language Switcher
    public ObservableCollection<Core.LanguageOption> AvailableLanguages { get; } = new()
    {
        new("Auto", "Automatisch (System)"),
        new("de", "Deutsch (German)"),
        new("en", "English (Englisch)")
    };

    [ObservableProperty] private Core.LanguageOption? selectedLanguageOption;

    public bool IsGermanLanguageActive => SelectedLanguageOption?.Code switch
    {
        "de" => true,
        "en" => false,
        _ => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("de", StringComparison.OrdinalIgnoreCase) ||
             System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("de", StringComparison.OrdinalIgnoreCase)
    };

    public string ActiveLanguageBadgeText => IsGermanLanguageActive ? "Deutsch" : "English";

    public string LanguageDescriptionText => SelectedLanguageOption?.Code switch
    {
        "de" => "Feste Sprache: Deutsch. Deutsche Zusatzmodule wie VoiceAttack Aurora sind aktiv.",
        "en" => "Fixed language: English. German-only third party companion prompts (like Aurora) are hidden.",
        _ => $"Automatische Systemerkennung (Aktuell: {(IsGermanLanguageActive ? "Deutsch" : "English")})."
    };

    public Core.I18n I18n => Core.I18n.Instance;

    partial void OnSelectedLanguageOptionChanged(Core.LanguageOption? value)
    {
        if (value == null) return;
        var code = value.Code;

        if (!_initializing)
        {
            _settings.AppLanguage = code;
            Settings.Save(_settings);
        }

        I18n.SetLanguage(code);
        RefreshGameStatusTexts();

        OnPropertyChanged(nameof(I18n));
        OnPropertyChanged(nameof(IsGermanLanguageActive));
        OnPropertyChanged(nameof(ShowAuroraPurchaseBanner));
        OnPropertyChanged(nameof(ActiveLanguageBadgeText));
        OnPropertyChanged(nameof(LanguageDescriptionText));
        OnPropertyChanged(nameof(LocationTypeBadge));
        OnPropertyChanged(nameof(LocationStatusSubline));
        OnPropertyChanged(nameof(LocationArmisticePillText));
        OnPropertyChanged(nameof(GameStatusText));
        OnPropertyChanged(nameof(GameStatusTooltip));
    }


    // Settings Sub-Tab Navigation
    [ObservableProperty] private int settingsSubTabIndex = 0;

    // Wipe- & Persistenz-Filter
    [ObservableProperty] private bool wipeFilterEnabled;
    [ObservableProperty] private string wipeDateString = "2026-05-15";
    [ObservableProperty] private bool wipeFilterMoney = true;
    [ObservableProperty] private bool wipeFilterContracts = true;
    [ObservableProperty] private bool wipeFilterFleet = false;
    [ObservableProperty] private bool wipeFilterBlueprints = false;

    // Piloten-Ausrüstung (Visual Loadout Slots)
    public ObservableCollection<LoadoutItem> PilotLoadoutSlots { get; } = new();

    // Flugschreiber & Session-Timeline (Black Box)
    public ObservableCollection<FlightTimelineItem> SessionTimeline { get; } = new();
    public DataGridCollectionView TimelineView { get; }
    [ObservableProperty] private FlightSummary flightSummary = new();
    [ObservableProperty] private FlightTimelineItem? selectedTimelineItem;
    [ObservableProperty] private string timelineSearchText = "";
    [ObservableProperty] private string selectedTimelineFilter = "Alle";

    // Starmap Custom POIs (Persönliche Wegpunkte & Notizen)
    public ObservableCollection<UserPoi> UserPois { get; } = new();
    [ObservableProperty] private UserPoi? selectedUserPoi;
    [ObservableProperty] private string newPoiName = "";
    [ObservableProperty] private string newPoiBody = "";
    [ObservableProperty] private string newPoiNotes = "";
    [ObservableProperty] private string newPoiCategory = "Mining";
    [ObservableProperty] private string newPoiColor = "#F59E0B";

    // Flotten-Sorties Entprellung
    private readonly HashSet<string> _sessionFlownShips = new(StringComparer.OrdinalIgnoreCase);

    // Ereignis-Volltextsuche
    [ObservableProperty] private string eventSearchText = "";

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

    // Fraktionsruf & Reputations-System
    public ObservableCollection<FactionReputation> Factions { get; } = new();
    public DataGridCollectionView FactionsView { get; }
    [ObservableProperty] private string selectedFactionCategory = "Alle";

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
        Status = AutoOcrEnabled ? "⚡ mobiGlas Kontostand Auto-Sync aktiviert (F1)" : "Kontostand Auto-Sync deaktiviert";
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

                // 0. SCLogMate eigenes Archiv (%APPDATA%\SCLogMate\archive)
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

    /// <summary>
    /// Synchronisiert beim Programmstart oder nach Schema/Parser-Updates automatisch alle Backup-Logs
    /// und indexiert fehlende Sessions geräuschlos im Hintergrund. Der Anwender muss nie manuell scannen.
    /// </summary>
    private async Task AutoSyncAndIndexDatabaseAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                var liveLog = LogPath;
                var dir = Path.GetDirectoryName(liveLog) ?? ".";
                var backupDir = Path.Combine(dir, "logbackups");
                var backups = Directory.Exists(backupDir)
                    ? Directory.GetFiles(backupDir, "*.log")
                    : System.Array.Empty<string>();

                var allArchived = LogArchive.Sync(backups);
                Database.Init();

                bool needsFullRescan = Database.WasParserResetRequired;
                int currentSessionCount = Database.GetSessionCount();
                int unindexedCount = Database.GetUnindexedCount(allArchived);

                if (needsFullRescan || (currentSessionCount == 0 && allArchived.Count > 0))
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        IsDatabaseBusy = true;
                        DatabaseStatusMessage = "⚡ Auto-Scan: Aktualisiere Datenbank nach Update...";
                        DatabaseProgressPercent = 0;
                    });

                    var result = Database.RescanAll(allArchived, (curr, total, name) =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            DatabaseProgressPercent = (double)curr / total * 100.0;
                            DatabaseStatusMessage = $"⚡ Auto-Scan ({curr}/{total}): {name}";
                        });
                    });

                    Dispatcher.UIThread.Post(() =>
                    {
                        DatabaseStatusMessage = $"✓ Auto-Scan fertig: {result.indexedSessions} Sessions ({result.totalEvents:N0} Events) indexiert.";
                        IsDatabaseBusy = false;
                        OnPropertyChanged(nameof(DatabaseSummaryText));
                        RefreshSessions(selectCurrent: false);
                        var wipeSince = GetEffectiveWipeDate();
                        RebuildFleet(Database.GetFleetStats(WipeFilterFleet ? wipeSince : null));
                        LoadIndependentFinancesAsync();
                    });
                }
                else if (unindexedCount > 0)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        IsDatabaseBusy = true;
                        DatabaseStatusMessage = $"⚡ Indexiere {unindexedCount} neue Session(s)...";
                        DatabaseProgressPercent = 0;
                    });

                    int added = Database.IndexNew(allArchived, (curr, total, name) =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            DatabaseProgressPercent = (double)curr / total * 100.0;
                            DatabaseStatusMessage = $"⚡ Indexiere ({curr}/{total}): {name}";
                        });
                    });

                    Dispatcher.UIThread.Post(() =>
                    {
                        IsDatabaseBusy = false;
                        OnPropertyChanged(nameof(DatabaseSummaryText));
                        RefreshSessions(selectCurrent: false);
                        var wipeSince = GetEffectiveWipeDate();
                        RebuildFleet(Database.GetFleetStats(WipeFilterFleet ? wipeSince : null));
                        LoadIndependentFinancesAsync();
                        if (added > 0)
                        {
                            Status = $"✓ {added} neue Session(s) automatisch im Hintergrund indexiert.";
                        }
                    });
                }
                else
                {
                    Dispatcher.UIThread.Post(LoadIndependentFinancesAsync);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("AutoSyncAndIndexDatabaseAsync", ex);
            }
        });
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
                // Bestehenden Auftrag im Speicher & Datenbank aktualisieren (kein Duplikat anlegen!)
                if (d.Reward > 0 && existing.Reward <= 0) existing.Reward = d.Reward;
                if (!string.IsNullOrEmpty(d.ContractedBy) && string.IsNullOrEmpty(existing.ContractedBy)) existing.ContractedBy = d.ContractedBy;
                if (title.Length > existing.Title.Length) existing.Title = title;
                existing.ScannedAt = d.ScannedAt;
                Database.SaveContract(existing);

                ActiveContractRewardText = existing.RewardText;
                ActiveContractOrg = existing.ContractedBy;
                ActiveContractTitle = existing.Title;
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
    public ObservableCollection<ShipFleetItem> FleetItems { get; } = new();
    public DataGridCollectionView FleetView { get; private set; }

    [ObservableProperty] private string selectedFleetViewMode = "Hangar"; // "Hangar" oder "Alle" (Flug-Historie)
    [ObservableProperty] private string selectedFleetAcquisition = "Alle";
    [ObservableProperty] private string selectedFleetManufacturer = "Alle";
    [ObservableProperty] private string fleetSearchText = "";

    public int HangarShipCount => FleetItems.Count(s => s.IsInHangar);
    public int AllFlownShipCount => FleetItems.Count;

    public long TotalFleetValue => FleetItems.Where(s => SelectedFleetViewMode != "Hangar" || s.IsInHangar).Sum(s => s.EstimatedValueAuec);
    public string TotalFleetValueText => TotalFleetValue > 0 ? $"{TotalFleetValue:N0} aUEC" : "0 aUEC";
    public int TotalFleetPledgeUsd => FleetItems.Where(s => s.IsPledgeBought && (SelectedFleetViewMode != "Hangar" || s.IsInHangar)).Sum(s => s.PledgeValueUsd);
    public string TotalFleetPledgeUsdText => TotalFleetPledgeUsd > 0 ? $"${TotalFleetPledgeUsd:N0} USD" : "$0 USD";
    public int TotalFleetFlights => FleetItems.Sum(s => s.FlightCount);
    public int TotalFleetQuantumJumps => FleetItems.Sum(s => s.QuantumJumps);

    [ObservableProperty] private Core.ShipCatalogEntry? selectedCatalogShipToAdd;
    public System.Collections.Generic.List<Core.ShipCatalogEntry> AllCatalogShips => Core.FleetCatalog.AllShips.ToList();

    public System.Collections.Generic.List<string> InsuranceOptions { get; } = new()
    {
        "LTI (Lifetime)",
        "120 Monate (IAE)",
        "24 Monate",
        "12 Monate",
        "6 Monate"
    };

    public System.Collections.Generic.List<string> AcquisitionOptions { get; } = new()
    {
        "Pledge Store",
        "In-Game (aUEC)",
        "Miete (Rental)",
        "Geliehen / Free Fly"
    };

    public string CurrentShipFlightInfo
    {
        get
        {
            if (string.IsNullOrWhiteSpace(CurrentShip) || CurrentShip == "—")
            {
                return HangarShipCount == 0 ? "Kein Schiff registriert" : $"{HangarShipCount} Schiffe im Hangar";
            }
            var item = FleetItems.FirstOrDefault(s => s.Name.Equals(CurrentShip, StringComparison.OrdinalIgnoreCase) || CurrentShip.Contains(s.Name, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                return $"Aktiv · {item.FlightCount}× geflogen · {item.QuantumJumps} QT-Sprünge (Hangar: {HangarShipCount})";
            }
            return $"{HangarShipCount} Schiffe im Hangar";
        }
    }

    public string FleetText => CurrentShipFlightInfo;
    public string ShipsSeenText => FleetItems.Count == 0 ? "—" : string.Join("\n", FleetItems.Select(f => $"{f.Name} ({f.FlightCount}× geflogen, {f.QuantumJumps} QT)"));

    public void RebuildFleet(IEnumerable<Database.DbShipStat> stats)
    {
        FleetItems.Clear();
        var customData = Database.GetAllFleetCustomData();

        foreach (var stat in stats)
        {
            var cat = FleetCatalog.Lookup(stat.Ship);
            var item = new ShipFleetItem
            {
                Name = stat.Ship,
                RawCode = stat.Ship,
                Manufacturer = cat.Manufacturer,
                ManufacturerBadge = cat.ManufacturerBadge,
                ManufacturerColor = cat.ManufacturerColor,
                Role = cat.Role,
                EstimatedValueAuec = cat.EstimatedValueAuec,
                FlightCount = stat.FlightCount,
                QuantumJumps = stat.QtCount,
                LossCount = stat.LossCount,
                LastFlown = stat.LastTime,
                IsCurrent = stat.Ship.Equals(CurrentShip, StringComparison.OrdinalIgnoreCase) || (!string.IsNullOrEmpty(CurrentShip) && CurrentShip.Contains(stat.Ship, StringComparison.OrdinalIgnoreCase))
            };

            if (customData.TryGetValue(stat.Ship, out var cd))
            {
                item.IsInHangar = cd.InHangar;
                item.IsPledgeBought = cd.IsPledge;
                item.PledgeValueUsd = cd.PledgeUsd;
                item.InsuranceType = cd.Insurance;
                item.AcquisitionType = cd.Acquisition;
                item.CustomNotes = cd.Notes;
            }
            else
            {
                // Schiffe aus Logs gehören zur Flug-Historie, nicht automatisch zum persönlichen Hangar
                item.IsInHangar = false;
                item.IsPledgeBought = false;
                item.PledgeValueUsd = cat.PledgeValueUsd;
                item.InsuranceType = cat.DefaultInsurance;
                item.AcquisitionType = "Geliehen / Free Fly";
            }

            FleetItems.Add(item);
        }

        // Falls der Nutzer Schiffe manuell im Hangar gespeichert hat, die noch nicht geflogen wurden:
        foreach (var (shipName, cd) in customData)
        {
            if (cd.InHangar && !FleetItems.Any(f => f.Name.Equals(shipName, StringComparison.OrdinalIgnoreCase)))
            {
                var cat = FleetCatalog.Lookup(shipName);
                FleetItems.Add(new ShipFleetItem
                {
                    Name = shipName,
                    RawCode = shipName,
                    Manufacturer = cat.Manufacturer,
                    ManufacturerBadge = cat.ManufacturerBadge,
                    ManufacturerColor = cat.ManufacturerColor,
                    Role = cat.Role,
                    EstimatedValueAuec = cat.EstimatedValueAuec,
                    FlightCount = 0,
                    QuantumJumps = 0,
                    LossCount = 0,
                    IsInHangar = true,
                    IsPledgeBought = cd.IsPledge,
                    PledgeValueUsd = cd.PledgeUsd > 0 ? cd.PledgeUsd : cat.PledgeValueUsd,
                    InsuranceType = cd.Insurance,
                    AcquisitionType = cd.Acquisition,
                    CustomNotes = cd.Notes
                });
            }
        }

        OnPropertyChanged(nameof(HangarShipCount));
        OnPropertyChanged(nameof(AllFlownShipCount));
        OnPropertyChanged(nameof(TotalFleetValue));
        OnPropertyChanged(nameof(TotalFleetValueText));
        OnPropertyChanged(nameof(TotalFleetPledgeUsd));
        OnPropertyChanged(nameof(TotalFleetPledgeUsdText));
        OnPropertyChanged(nameof(TotalFleetFlights));
        OnPropertyChanged(nameof(TotalFleetQuantumJumps));
        OnPropertyChanged(nameof(CurrentShipFlightInfo));
        OnPropertyChanged(nameof(FleetText));
        OnPropertyChanged(nameof(ShipsSeenText));
        FleetView?.Refresh();
    }

    public void RegisterOrUpdateShip(string shipName, bool isFlight = false, bool isQt = false, bool isLoss = false, DateTime? time = null, string? location = null)
    {
        if (string.IsNullOrWhiteSpace(shipName) || shipName == "—" || shipName == "Fahrzeug") return;

        var existing = FleetItems.FirstOrDefault(s => s.Name.Equals(shipName, StringComparison.OrdinalIgnoreCase) || shipName.Contains(s.Name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            if (isFlight) existing.FlightCount++;
            if (isQt) existing.QuantumJumps++;
            if (isLoss) existing.LossCount++;
            if (time.HasValue) existing.LastFlown = time.Value;
            if (!string.IsNullOrEmpty(location)) existing.LastLocation = location;
            existing.NotifyPropertiesChanged();
        }
        else
        {
            var cat = FleetCatalog.Lookup(shipName);
            var customData = Database.GetAllFleetCustomData();
            var item = new ShipFleetItem
            {
                Name = shipName,
                RawCode = shipName,
                Manufacturer = cat.Manufacturer,
                ManufacturerBadge = cat.ManufacturerBadge,
                ManufacturerColor = cat.ManufacturerColor,
                Role = cat.Role,
                EstimatedValueAuec = cat.EstimatedValueAuec,
                FlightCount = isFlight ? 1 : 1,
                QuantumJumps = isQt ? 1 : 0,
                LossCount = isLoss ? 1 : 0,
                LastFlown = time ?? DateTime.UtcNow,
                LastLocation = location ?? "—",
                IsCurrent = shipName.Equals(CurrentShip, StringComparison.OrdinalIgnoreCase) || (!string.IsNullOrEmpty(CurrentShip) && CurrentShip.Contains(shipName, StringComparison.OrdinalIgnoreCase))
            };

            if (customData.TryGetValue(shipName, out var cd))
            {
                item.IsInHangar = cd.InHangar;
                item.IsPledgeBought = cd.IsPledge;
                item.PledgeValueUsd = cd.PledgeUsd;
                item.InsuranceType = cd.Insurance;
                item.AcquisitionType = cd.Acquisition;
                item.CustomNotes = cd.Notes;
            }
            else
            {
                item.IsInHangar = false;
                item.IsPledgeBought = false;
                item.PledgeValueUsd = cat.PledgeValueUsd;
                item.InsuranceType = cat.DefaultInsurance;
                item.AcquisitionType = "Geliehen / Free Fly";
            }

            FleetItems.Add(item);
        }

        if (_shipSet.Add(shipName)) ShipsSeen.Add(shipName);

        OnPropertyChanged(nameof(HangarShipCount));
        OnPropertyChanged(nameof(AllFlownShipCount));
        OnPropertyChanged(nameof(TotalFleetValue));
        OnPropertyChanged(nameof(TotalFleetValueText));
        OnPropertyChanged(nameof(TotalFleetPledgeUsd));
        OnPropertyChanged(nameof(TotalFleetPledgeUsdText));
        OnPropertyChanged(nameof(TotalFleetFlights));
        OnPropertyChanged(nameof(TotalFleetQuantumJumps));
        OnPropertyChanged(nameof(CurrentShipFlightInfo));
        OnPropertyChanged(nameof(FleetText));
        OnPropertyChanged(nameof(ShipsSeenText));
        FleetView?.Refresh();
    }

    partial void OnFleetSearchTextChanged(string value) => FleetView?.Refresh();

    [RelayCommand]
    private void SwitchToFleetTab() => SelectedTabIndex = 7;

    [RelayCommand]
    private void SelectFleetViewMode(string mode)
    {
        SelectedFleetViewMode = mode;
        OnPropertyChanged(nameof(TotalFleetValue));
        OnPropertyChanged(nameof(TotalFleetValueText));
        OnPropertyChanged(nameof(TotalFleetPledgeUsd));
        OnPropertyChanged(nameof(TotalFleetPledgeUsdText));
        FleetView?.Refresh();
    }

    [RelayCommand]
    private void SelectFleetAcquisition(string acq)
    {
        SelectedFleetAcquisition = acq;
        FleetView?.Refresh();
    }

    [RelayCommand]
    private void SelectFleetManufacturer(string mfg)
    {
        SelectedFleetManufacturer = mfg;
        FleetView?.Refresh();
    }

    [RelayCommand]
    private void SelectShipAsCurrent(ShipFleetItem ship)
    {
        if (ship == null) return;
        CurrentShip = ship.Name;
        foreach (var s in FleetItems)
        {
            s.IsCurrent = (s == ship);
        }
        OnPropertyChanged(nameof(CurrentShip));
        OnPropertyChanged(nameof(CurrentShipFlightInfo));
        OnPropertyChanged(nameof(FleetText));
    }

    [RelayCommand]
    private void ToggleShipHangar(ShipFleetItem ship)
    {
        if (ship == null) return;
        ship.IsInHangar = !ship.IsInHangar;
        if (ship.IsInHangar && (ship.AcquisitionType == "Geliehen / Free Fly" || string.IsNullOrWhiteSpace(ship.AcquisitionType)))
        {
            ship.AcquisitionType = "Pledge Store";
            ship.IsPledgeBought = true;
        }
        ship.NotifyPropertiesChanged();
        Database.SaveFleetShipCustomData(ship.Name, ship.IsInHangar, ship.IsPledgeBought, ship.PledgeValueUsd, ship.InsuranceType, ship.AcquisitionType, ship.CustomNotes);
        OnPropertyChanged(nameof(HangarShipCount));
        OnPropertyChanged(nameof(TotalFleetValue));
        OnPropertyChanged(nameof(TotalFleetValueText));
        OnPropertyChanged(nameof(TotalFleetPledgeUsd));
        OnPropertyChanged(nameof(TotalFleetPledgeUsdText));
        OnPropertyChanged(nameof(CurrentShipFlightInfo));
        FleetView?.Refresh();
        Status = ship.IsInHangar ? $"✓ {ship.Name} zu 'Mein Hangar' hinzugefügt" : $"— {ship.Name} aus 'Mein Hangar' entfernt (bleibt in Flug-Historie)";
    }

    [RelayCommand]
    private void AddShipToHangar(ShipFleetItem ship)
    {
        if (ship == null) return;
        ship.IsInHangar = true;
        if (ship.AcquisitionType == "Geliehen / Free Fly" || string.IsNullOrWhiteSpace(ship.AcquisitionType))
        {
            ship.AcquisitionType = "Pledge Store";
            ship.IsPledgeBought = true;
        }
        ship.NotifyPropertiesChanged();
        Database.SaveFleetShipCustomData(ship.Name, ship.IsInHangar, ship.IsPledgeBought, ship.PledgeValueUsd, ship.InsuranceType, ship.AcquisitionType, ship.CustomNotes);
        OnPropertyChanged(nameof(HangarShipCount));
        OnPropertyChanged(nameof(TotalFleetValue));
        OnPropertyChanged(nameof(TotalFleetValueText));
        OnPropertyChanged(nameof(TotalFleetPledgeUsd));
        OnPropertyChanged(nameof(TotalFleetPledgeUsdText));
        OnPropertyChanged(nameof(CurrentShipFlightInfo));
        FleetView?.Refresh();
        Status = $"✓ {ship.Name} zu 'Mein Hangar' hinzugefügt";
    }

    [RelayCommand]
    private void RemoveShipFromHangar(ShipFleetItem ship)
    {
        if (ship == null) return;
        ship.IsInHangar = false;
        ship.NotifyPropertiesChanged();
        Database.SaveFleetShipCustomData(ship.Name, ship.IsInHangar, ship.IsPledgeBought, ship.PledgeValueUsd, ship.InsuranceType, ship.AcquisitionType, ship.CustomNotes);
        OnPropertyChanged(nameof(HangarShipCount));
        OnPropertyChanged(nameof(TotalFleetValue));
        OnPropertyChanged(nameof(TotalFleetValueText));
        OnPropertyChanged(nameof(TotalFleetPledgeUsd));
        OnPropertyChanged(nameof(TotalFleetPledgeUsdText));
        OnPropertyChanged(nameof(CurrentShipFlightInfo));
        FleetView?.Refresh();
        Status = $"— {ship.Name} aus 'Mein Hangar' entfernt (bleibt in Flug-Historie)";
    }

    [RelayCommand]
    private void CycleShipAcquisition(ShipFleetItem ship)
    {
        if (ship == null) return;
        var next = ship.AcquisitionType switch
        {
            "Pledge Store" => "In-Game (aUEC)",
            "In-Game (aUEC)" => "Miete (Rental)",
            "Miete (Rental)" => "Geliehen / Free Fly",
            _ => "Pledge Store"
        };
        ship.AcquisitionType = next;
        ship.IsPledgeBought = (next == "Pledge Store");
        ship.NotifyPropertiesChanged();
        Database.SaveFleetShipCustomData(ship.Name, ship.IsInHangar, ship.IsPledgeBought, ship.PledgeValueUsd, ship.InsuranceType, ship.AcquisitionType, ship.CustomNotes);
        OnPropertyChanged(nameof(TotalFleetPledgeUsd));
        OnPropertyChanged(nameof(TotalFleetPledgeUsdText));
        Status = $"{ship.Name}: {ship.AcquisitionType}";
    }

    [RelayCommand]
    private void CycleShipInsurance(ShipFleetItem ship)
    {
        if (ship == null) return;
        if (!ship.IsPledgeBought || ship.AcquisitionType != "Pledge Store")
        {
            Status = $"ℹ Versicherung gilt nur für Pledge-Store Käufe (Echtgeld). Aktuell: {ship.AcquisitionType}";
            return;
        }

        var next = ship.InsuranceType switch
        {
            "LTI (Lifetime)" => "120 Monate (IAE)",
            "120 Monate (IAE)" => "24 Monate",
            "24 Monate" => "12 Monate",
            "12 Monate" => "6 Monate",
            _ => "LTI (Lifetime)"
        };
        ship.InsuranceType = next;
        ship.NotifyPropertiesChanged();
        Database.SaveFleetShipCustomData(ship.Name, ship.IsInHangar, ship.IsPledgeBought, ship.PledgeValueUsd, ship.InsuranceType, ship.AcquisitionType, ship.CustomNotes);
        Status = $"Versicherung für {ship.Name}: {ship.InsuranceType}";
    }

    [RelayCommand]
    private void StartEditShipPledge(ShipFleetItem? ship)
    {
        if (ship == null) return;
        foreach (var s in FleetItems) s.IsEditingPledge = false;
        ship.PledgeInputText = ship.PledgeValueUsd.ToString();
        ship.IsEditingPledge = true;
    }

    [RelayCommand]
    private void SaveShipPledge(ShipFleetItem? ship)
    {
        if (ship == null) return;
        var digits = new string((ship.PledgeInputText ?? "").Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, out var val) && val >= 0)
        {
            ship.PledgeValueUsd = val;
        }
        ship.IsEditingPledge = false;
        ship.NotifyPropertiesChanged();
        Database.SaveFleetShipCustomData(ship.Name, ship.IsInHangar, ship.IsPledgeBought, ship.PledgeValueUsd, ship.InsuranceType, ship.AcquisitionType, ship.CustomNotes);
        OnPropertyChanged(nameof(TotalFleetPledgeUsd));
        OnPropertyChanged(nameof(TotalFleetPledgeUsdText));
        Status = $"💵 Pledge-Wert für {ship.Name} auf ${ship.PledgeValueUsd} gespeichert";
    }

    [RelayCommand]
    private void CancelEditShipPledge(ShipFleetItem? ship)
    {
        if (ship == null) return;
        ship.IsEditingPledge = false;
    }

    public void AdjustShipPledge(ShipFleetItem? ship, int delta)
    {
        if (ship == null) return;
        ship.PledgeValueUsd = Math.Max(0, ship.PledgeValueUsd + delta);
        ship.NotifyPropertiesChanged();
        Database.SaveFleetShipCustomData(ship.Name, ship.IsInHangar, ship.IsPledgeBought, ship.PledgeValueUsd, ship.InsuranceType, ship.AcquisitionType, ship.CustomNotes);
        OnPropertyChanged(nameof(TotalFleetPledgeUsd));
        OnPropertyChanged(nameof(TotalFleetPledgeUsdText));
    }

    [RelayCommand]
    private void AddSelectedCatalogShipToHangar()
    {
        if (SelectedCatalogShipToAdd == null) return;
        var cat = SelectedCatalogShipToAdd;
        var shipName = cat.NormalizedName;
        var existing = FleetItems.FirstOrDefault(s => s.Name.Equals(shipName, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.IsInHangar = true;
            if (existing.AcquisitionType == "Geliehen / Free Fly" || string.IsNullOrWhiteSpace(existing.AcquisitionType))
            {
                existing.AcquisitionType = "Pledge Store";
                existing.IsPledgeBought = true;
            }
            existing.NotifyPropertiesChanged();
            Database.SaveFleetShipCustomData(existing.Name, existing.IsInHangar, existing.IsPledgeBought, existing.PledgeValueUsd, existing.InsuranceType, existing.AcquisitionType, existing.CustomNotes);
        }
        else
        {
            var item = new ShipFleetItem
            {
                Name = cat.NormalizedName,
                RawCode = cat.NormalizedName,
                Manufacturer = cat.Manufacturer,
                ManufacturerBadge = cat.ManufacturerBadge,
                ManufacturerColor = cat.ManufacturerColor,
                Role = cat.Role,
                EstimatedValueAuec = cat.EstimatedValueAuec,
                FlightCount = 0,
                QuantumJumps = 0,
                LossCount = 0,
                IsInHangar = true,
                IsPledgeBought = true,
                PledgeValueUsd = cat.PledgeValueUsd,
                InsuranceType = cat.DefaultInsurance,
                AcquisitionType = "Pledge Store",
                IsCurrent = false
            };
            FleetItems.Add(item);
            Database.SaveFleetShipCustomData(item.Name, item.IsInHangar, item.IsPledgeBought, item.PledgeValueUsd, item.InsuranceType, item.AcquisitionType, item.CustomNotes);
        }

        OnPropertyChanged(nameof(HangarShipCount));
        OnPropertyChanged(nameof(AllFlownShipCount));
        OnPropertyChanged(nameof(TotalFleetValue));
        OnPropertyChanged(nameof(TotalFleetValueText));
        OnPropertyChanged(nameof(TotalFleetPledgeUsd));
        OnPropertyChanged(nameof(TotalFleetPledgeUsdText));
        SelectedFleetViewMode = "Hangar";
        FleetView?.Refresh();
        Status = $"✓ {shipName} zu 'Mein Hangar' hinzugefügt";
    }

    [RelayCommand]
    private void ToggleShipPledge(ShipFleetItem ship)
    {
        CycleShipAcquisition(ship);
    }

    [RelayCommand]
    private void OpenShipWiki(ShipFleetItem ship)
    {
        if (ship == null) return;
        OpenWikiCommand.Execute(ship.Name);
    }

    [RelayCommand]
    private void OpenShipUex(ShipFleetItem ship)
    {
        if (ship == null) return;
        try
        {
            var query = Uri.EscapeDataString(ship.Name.Split('·')[0].Trim());
            var url = $"https://uexcorp.space/vehicles?name={query}";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch { }
    }

    public long IncomeAll => TotalIn + TotalReward + TotalSales + TotalTrade;
    public long SpendAll => TotalOut + TotalPurchases;
    public long NetAll => IncomeAll - SpendAll;

    public long LiveSessionIncome => _allMode
        ? IncomeAll
        : Events.Where(e => IsMoney(e.Kind) && e.Amount > 0).Sum(e => e.Amount);

    public long LiveSessionSpend => _allMode
        ? SpendAll
        : Events.Where(e => IsMoney(e.Kind) && e.Amount < 0).Sum(e => -e.Amount);

    public long LiveSessionNet => LiveSessionIncome - LiveSessionSpend;

    public string SessionIncomeText => $"+{LiveSessionIncome:N0} aUEC";
    public string SessionSpendText => $"-{LiveSessionSpend:N0} aUEC";
    public string SessionNetText => $"{(LiveSessionNet >= 0 ? "+" : "")}{LiveSessionNet:N0} aUEC";
    public long SessionNetSign => LiveSessionNet;
    public string LiveBalanceText => ExpectedBalance > 0 ? $"{ExpectedBalance:N0} aUEC" : StartBalance() > 0 ? $"{StartBalance():N0} aUEC" : "— Nicht gesetzt —";

    // Geld-Statistik & Sci-Fi Vektor-Diagramme (eigener Tab)
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

    // ---- Unabhängiges Daten-Subsystem (Standard: Alle Sessions / Historie) ----
    private List<LogEntry> _allDbFinanceEvents = new();
    private List<LogEntry> _allDbTrades = new();
    private List<LogEntry> _allDbTopMoney = new();
    private List<LogEntry> _allDbRecentMoney = new();
    private List<LogEntry> _allDbTimelineEvents = new();
    private List<LogEntry> _allDbMissionEvents = new();
    private Database.Agg _allDbFinanceAgg = new();
    private bool _historyLoaded = false;

    private List<LogEntry> _liveTimelineEvents = new();
    private List<LogEntry> _liveMissionEvents = new();

    // Starmap-Integration für Flugschreiber
    private string _flightRecorderSelectedSystem = "Stanton";
    public string FlightRecorderSelectedSystem
    {
        get => _flightRecorderSelectedSystem;
        set => SetProperty(ref _flightRecorderSelectedSystem, value);
    }

    private StarmapObject? _flightRecorderSelectedObject;
    public StarmapObject? FlightRecorderSelectedObject
    {
        get => _flightRecorderSelectedObject;
        set => SetProperty(ref _flightRecorderSelectedObject, value);
    }

    public ObservableCollection<string> FlightRouteLocations { get; } = new();

    public ObservableCollection<SessionInfo> FlightSessions { get; } = new();
    private SessionInfo? _selectedFlightSession;
    public SessionInfo? SelectedFlightSession
    {
        get => _selectedFlightSession;
        set
        {
            if (SetProperty(ref _selectedFlightSession, value))
            {
                OnFlightSessionChanged();
            }
        }
    }

    [RelayCommand]
    public void SetFlightRecorderSystem(string system)
    {
        if (!string.IsNullOrEmpty(system) && StarmapData.SystemNames.Contains(system))
        {
            FlightRecorderSelectedSystem = system;
            var sysStar = StarmapData.GetSystemObjects(system).FirstOrDefault(o => o.Type == StarmapObjectType.Star);
            if (sysStar != null) FlightRecorderSelectedObject = sysStar;
        }
    }

    private void OnFlightSessionChanged()
    {
        var targetScope = (SelectedFlightSession == null || SelectedFlightSession.IsAll) ? 0
                        : (SelectedFlightSession.IsCurrent ? 1 : 2);

        if (_timelineScope != targetScope)
        {
            TimelineScope = targetScope;
        }
        else
        {
            RebuildIndependentTimeline();
        }
    }

    public void RefreshFlightSessions()
    {
        var prev = SelectedFlightSession?.Path;
        FlightSessions.Clear();
        foreach (var s in Sessions)
        {
            FlightSessions.Add(s);
        }
        SelectedFlightSession = FlightSessions.FirstOrDefault(x => x.Path == prev)
                                ?? FlightSessions.FirstOrDefault(x => x.IsAll)
                                ?? FlightSessions.FirstOrDefault();
    }

    private int _timelineScope = 0; // 0 = Alle Sessions (Standard), 1 = Aktuelle Session, 2 = Spezifisch
    public int TimelineScope
    {
        get => _timelineScope;
        set
        {
            if (SetProperty(ref _timelineScope, value))
            {
                RebuildIndependentTimeline();
                OnPropertyChanged(nameof(IsTimelineScopeAll));
                OnPropertyChanged(nameof(IsTimelineScopeCurrent));
            }
        }
    }
    public bool IsTimelineScopeAll => TimelineScope == 0;
    public bool IsTimelineScopeCurrent => TimelineScope == 1;

    [RelayCommand]
    public void SetTimelineScope(string scopeStr)
    {
        if (int.TryParse(scopeStr, out var s))
            TimelineScope = s;
    }

    private int _financeScope = 0; // 0 = Alle Sessions (Standard), 1 = Aktuelle Session
    public int FinanceScope
    {
        get => _financeScope;
        set
        {
            if (SetProperty(ref _financeScope, value))
            {
                RebuildIndependentFinances();
                OnPropertyChanged(nameof(IsFinanceScopeAll));
                OnPropertyChanged(nameof(IsFinanceScopeCurrent));
            }
        }
    }
    public bool IsFinanceScopeAll => FinanceScope == 0;
    public bool IsFinanceScopeCurrent => FinanceScope == 1;

    [RelayCommand]
    public void SetFinanceScope(string scopeStr)
    {
        if (int.TryParse(scopeStr, out var s))
            FinanceScope = s;
    }

    public long FinanceIncomeAll => FinanceScope == 0
        ? (_allDbFinanceAgg.In + _allDbFinanceAgg.Reward + _allDbFinanceAgg.Sales + _allDbFinanceAgg.Trade + _liveMoney.Where(e => e.Amount > 0).Sum(e => e.Amount))
        : Events.Where(e => IsMoney(e.Kind) && e.Amount > 0).Sum(e => e.Amount);

    public long FinanceSpendAll => FinanceScope == 0
        ? (_allDbFinanceAgg.Out + _allDbFinanceAgg.Purchases + _liveMoney.Where(e => e.Amount < 0).Sum(e => System.Math.Abs(e.Amount)))
        : Events.Where(e => IsMoney(e.Kind) && e.Amount < 0).Sum(e => System.Math.Abs(e.Amount));

    public long FinanceNetAll => FinanceIncomeAll - FinanceSpendAll;

    public string FinanceIncomeTotalText => $"{FinanceIncomeAll:N0} aUEC";
    public string FinanceSpendTotalText => $"{FinanceSpendAll:N0} aUEC";
    public string FinanceNetBalanceText => $"{(FinanceNetAll >= 0 ? "+" : "")}{FinanceNetAll:N0} aUEC";
    public long FinanceNetSign => FinanceNetAll;

    public string IncomeTotalText => FinanceIncomeTotalText;
    public string SpendTotalText => FinanceSpendTotalText;

    // Interaktives Sci-Fi Finanz-Diagramm (QuantumWake-Stil)
    public ObservableCollection<FinanceTimelinePoint> FinanceChartPoints { get; } = new();

    int _selectedFinanceChartMode = 0;
    public int SelectedFinanceChartMode
    {
        get => _selectedFinanceChartMode;
        set
        {
            if (SetProperty(ref _selectedFinanceChartMode, value))
            {
                RebuildFinanceChart();
                OnPropertyChanged(nameof(IsChartModeCumulative));
                OnPropertyChanged(nameof(IsChartModeNetTrend));
                OnPropertyChanged(nameof(IsChartModeCashflow));
            }
        }
    }

    public bool IsChartModeCumulative => SelectedFinanceChartMode == 0;
    public bool IsChartModeNetTrend => SelectedFinanceChartMode == 1;
    public bool IsChartModeCashflow => SelectedFinanceChartMode == 2;

    int _selectedFinanceChartFilter = 0;
    public int SelectedFinanceChartFilter
    {
        get => _selectedFinanceChartFilter;
        set
        {
            if (SetProperty(ref _selectedFinanceChartFilter, value))
            {
                RebuildFinanceChart();
                OnPropertyChanged(nameof(IsChartFilterAll));
                OnPropertyChanged(nameof(IsChartFilterTrade));
                OnPropertyChanged(nameof(IsChartFilterMissions));
            }
        }
    }

    public bool IsChartFilterAll => SelectedFinanceChartFilter == 0;
    public bool IsChartFilterTrade => SelectedFinanceChartFilter == 1;
    public bool IsChartFilterMissions => SelectedFinanceChartFilter == 2;

    public string ProfitMarginText { get; private set; } = "0.0%";
    public string TotalCargoAuecText { get; private set; } = "0 aUEC";
    public string TotalCargoScuText { get; private set; } = "0 SCU";

    [RelayCommand]
    public void SetFinanceChartMode(string modeStr)
    {
        if (int.TryParse(modeStr, out var m))
            SelectedFinanceChartMode = m;
    }

    [RelayCommand]
    public void SetFinanceChartFilter(string filterStr)
    {
        if (int.TryParse(filterStr, out var f))
            SelectedFinanceChartFilter = f;
    }

    const double BarMax = 280.0;

    public void LoadGlobalDataAsync()
    {
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

                if (backups.Length > 0)
                {
                    var archived = LogArchive.Sync(backups);
                    Database.Init();
                    Database.IndexNew(archived);
                }
                else
                {
                    Database.Init();
                }

                var wipeSince = GetEffectiveWipeDate();
                var agg = Database.Aggregate(wipeSince, WipeFilterMoney, WipeFilterContracts, WipeFilterFleet);
                var financeEvents = Database.AllFinanceEvents(WipeFilterMoney ? wipeSince : null);
                var trades = Database.AllTrades();
                var topMoney = Database.TopMoney(40);
                var recentMoney = Database.RecentMoneyEvents(30);
                var timelineEvents = Database.AllTimelineEvents(WipeFilterFleet ? wipeSince : null);
                var missionEvents = Database.AllMissionTakenEvents();

                Dispatcher.UIThread.Post(() =>
                {
                    _allDbFinanceAgg = agg;
                    _allDbFinanceEvents = financeEvents;
                    _allDbTrades = trades;
                    _allDbTopMoney = topMoney;
                    _allDbRecentMoney = recentMoney;
                    _allDbTimelineEvents = timelineEvents;
                    _allDbMissionEvents = missionEvents;
                    _historyLoaded = true;

                    RebuildIndependentFinances();
                    RebuildIndependentTimeline();
                    RebuildIndependentMissions();
                });
            }
            catch (Exception ex)
            {
                Logger.Error("LoadGlobalDataAsync", ex);
            }
        });
    }

    public void LoadIndependentFinancesAsync() => LoadGlobalDataAsync();

    public void RebuildIndependentFinances()
    {
        RebuildBars();

        if (FinanceScope == 0)
        {
            var trades = _allDbTrades.Concat(_liveMoney.Where(x => x.Kind == EventKind.Trade));
            RebuildCommodityTrades(trades);
            RebuildMarketPrices(trades);

            var top = _allDbTopMoney.Concat(_liveMoney.Where(x => IsMoney(x.Kind)))
                                    .OrderByDescending(e => System.Math.Abs(e.Amount))
                                    .Take(8)
                                    .OrderBy(e => System.Math.Abs(e.Amount));
            SetTopTransactions(top);

            var recent = _liveMoney.Where(x => IsMoney(x.Kind))
                                   .Concat(_allDbRecentMoney)
                                   .Take(20);
            RecentMoney.Clear();
            foreach (var e in recent)
            {
                RecentMoney.Add(new StatItem
                {
                    Label = e.Detail ?? e.KindText,
                    Value = e.Amount,
                    Time = e.Time,
                    Color = Brush(e.Amount >= 0 ? "#4ADE80" : "#F87171")
                });
            }
        }
        else
        {
            var sessionTrades = Events.Where(e => e.Kind == EventKind.Trade);
            RebuildCommodityTrades(sessionTrades);
            RebuildMarketPrices(sessionTrades);

            var sessionTop = Events.Where(e => IsMoney(e.Kind))
                                   .OrderByDescending(e => System.Math.Abs(e.Amount))
                                   .Take(8)
                                   .OrderBy(e => System.Math.Abs(e.Amount));
            SetTopTransactions(sessionTop);

            RecentMoney.Clear();
            foreach (var e in Events.Where(e => IsMoney(e.Kind)).Take(20))
            {
                RecentMoney.Add(new StatItem
                {
                    Label = e.Detail ?? e.KindText,
                    Value = e.Amount,
                    Time = e.Time,
                    Color = Brush(e.Amount >= 0 ? "#4ADE80" : "#F87171")
                });
            }
        }

        RebuildFinanceChart();
    }

    public void RebuildIndependentTimeline()
    {
        IEnumerable<LogEntry> source;
        if (TimelineScope == 0)
        {
            source = _allDbTimelineEvents.Concat(_liveTimelineEvents);
        }
        else if (TimelineScope == 1)
        {
            source = Events.Where(IsTimelineRelevant);
        }
        else if (SelectedFlightSession != null && !string.IsNullOrEmpty(SelectedFlightSession.Path))
        {
            var fname = Path.GetFileName(SelectedFlightSession.Path);
            var sessionEvents = Database.GetTimelineEventsForSession(fname);
            source = sessionEvents.Count > 0 ? sessionEvents : Events.Where(IsTimelineRelevant);
        }
        else
        {
            source = _allDbTimelineEvents.Concat(_liveTimelineEvents);
        }

        var (items, summary) = FlightRecorderService.BuildTimeline(source);
        SessionTimeline.Clear();
        FlightRouteLocations.Clear();
        foreach (var item in items)
        {
            SessionTimeline.Add(item);
            if (!string.IsNullOrWhiteSpace(item.LocationName) && item.LocationName != "—")
            {
                FlightRouteLocations.Add(item.LocationName);
            }
        }

        // Automatische System-Erkennung für Starmap im Flugschreiber
        var lastLocItem = items.LastOrDefault(i => !string.IsNullOrEmpty(i.SystemName) && StarmapData.SystemNames.Contains(i.SystemName));
        if (lastLocItem != null)
        {
            FlightRecorderSelectedSystem = lastLocItem.SystemName;
            var obj = StarmapData.FindObject(lastLocItem.LocationName);
            if (obj != null) FlightRecorderSelectedObject = obj;
        }

        FlightSummary = summary;
        TimelineView?.Refresh();
        OnPropertyChanged(nameof(FlightSummary));
    }

    static bool IsTimelineRelevant(LogEntry e) =>
        e.Kind is EventKind.Vehicle or EventKind.ShipLoss or EventKind.Quantum or EventKind.Location or EventKind.Hangar or EventKind.Crash or EventKind.Death;

    public void RebuildIndependentMissions()
    {
        var source = _allDbMissionEvents.Concat(_liveMissionEvents);
        RebuildMissions(source);
    }

    void RebuildStats()
    {
        RebuildIndependentFinances();
        RebuildIndependentTimeline();
        RebuildIndependentMissions();
    }

    void SetTopTransactions(System.Collections.Generic.IEnumerable<LogEntry> events)
    {
        var list = events.ToList();
        long max = 1;
        foreach (var e in list) max = System.Math.Max(max, System.Math.Abs(e.Amount));
        TopTransactions.Clear();
        foreach (var e in list)
        {
            TopTransactions.Add(new StatItem
            {
                Label = e.Detail ?? e.KindText,
                Value = e.Amount,
                Time = e.Time,
                BarWidth = System.Math.Abs(e.Amount) / (double)max * BarMax,
                Color = Brush(e.Amount >= 0 ? "#4ADE80" : "#F87171")
            });
        }
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

    void RebuildFinanceChart()
    {
        IEnumerable<LogEntry> source = FinanceScope == 0
            ? _allDbFinanceEvents.Concat(_liveMoney.Where(x => IsMoney(x.Kind)))
            : Events.Where(x => IsMoney(x.Kind));

        if (SelectedFinanceChartFilter == 1) // Nur Fracht
        {
            source = source.Where(x => x.Kind == EventKind.Trade);
        }
        else if (SelectedFinanceChartFilter == 2) // Nur Belohnungen & Transfers
        {
            source = source.Where(x => x.Kind is EventKind.MissionReward or EventKind.TransferIn or EventKind.TransferOut);
        }

        var sorted = source.OrderBy(x => x.Time).ToList();

        long runningInc = 0;
        long runningSpd = 0;
        long runningNet = 0;

        var pts = new List<FinanceTimelinePoint>(sorted.Count + 1);

        if (sorted.Count > 0)
        {
            var firstTime = sorted[0].Time;
            pts.Add(new FinanceTimelinePoint
            {
                Time = firstTime.AddSeconds(-1),
                Amount = 0,
                CumulativeIncome = 0,
                CumulativeSpend = 0,
                CumulativeNet = 0,
                Label = "Start",
                Kind = EventKind.Info,
                Detail = "Startpunkt"
            });
        }

        long totalCargoAuec = 0;
        long totalCargoScu = 0;

        foreach (var e in sorted)
        {
            if (e.Amount >= 0)
            {
                runningInc += e.Amount;
            }
            else
            {
                runningSpd += System.Math.Abs(e.Amount);
            }
            runningNet = runningInc - runningSpd;

            if (e.Kind == EventKind.Trade)
            {
                totalCargoAuec += System.Math.Abs(e.Amount);
                var m = TradeDetailRegex().Match(e.Detail ?? "");
                if (m.Success && long.TryParse(m.Groups["scu"].Value, out var scu))
                {
                    totalCargoScu += scu;
                }
            }

            pts.Add(new FinanceTimelinePoint
            {
                Time = e.Time,
                Amount = e.Amount,
                CumulativeIncome = runningInc,
                CumulativeSpend = runningSpd,
                CumulativeNet = runningNet,
                Label = $"{e.KindText}: {e.Detail}",
                Kind = e.Kind,
                Detail = e.Detail ?? e.KindText
            });
        }

        FinanceChartPoints.Clear();
        foreach (var p in pts)
        {
            FinanceChartPoints.Add(p);
        }

        // Gewinnspanne & Metriken
        long spend = FinanceSpendAll;
        long inc = FinanceIncomeAll;
        if (spend > 0)
        {
            double margin = ((double)(inc - spend) / spend) * 100.0;
            ProfitMarginText = $"{(margin >= 0 ? "+" : "")}{margin:F1}%";
        }
        else if (inc > 0) ProfitMarginText = "+100%";
        else ProfitMarginText = "0.0%";

        TotalCargoAuecText = $"{totalCargoAuec:N0} aUEC";
        TotalCargoScuText = $"{totalCargoScu:N0} SCU";

        OnPropertyChanged(nameof(ProfitMarginText));
        OnPropertyChanged(nameof(TotalCargoAuecText));
        OnPropertyChanged(nameof(TotalCargoScuText));
        OnPropertyChanged(nameof(FinanceIncomeTotalText));
        OnPropertyChanged(nameof(FinanceSpendTotalText));
        OnPropertyChanged(nameof(FinanceNetBalanceText));
        OnPropertyChanged(nameof(FinanceNetSign));
        OnPropertyChanged(nameof(IncomeTotalText));
        OnPropertyChanged(nameof(SpendTotalText));
    }

    void RebuildBars()
    {
        long tIn, tRew, tSales, tTrade, tOut, tPurch;
        if (FinanceScope == 0)
        {
            long liveIn = _liveMoney.Where(e => e.Kind == EventKind.TransferIn).Sum(e => e.Amount);
            long liveRew = _liveMoney.Where(e => e.Kind == EventKind.MissionReward).Sum(e => e.Amount);
            long liveSales = _liveMoney.Where(e => e.Kind == EventKind.Sale).Sum(e => e.Amount);
            long liveTrade = _liveMoney.Where(e => e.Kind == EventKind.Trade).Sum(e => e.Amount);
            long liveOut = _liveMoney.Where(e => e.Kind == EventKind.TransferOut).Sum(e => System.Math.Abs(e.Amount));
            long livePurch = _liveMoney.Where(e => e.Kind == EventKind.Purchase).Sum(e => System.Math.Abs(e.Amount));

            tIn = _allDbFinanceAgg.In + liveIn;
            tRew = _allDbFinanceAgg.Reward + liveRew;
            tSales = _allDbFinanceAgg.Sales + liveSales;
            tTrade = _allDbFinanceAgg.Trade + liveTrade;
            tOut = _allDbFinanceAgg.Out + liveOut;
            tPurch = _allDbFinanceAgg.Purchases + livePurch;
        }
        else
        {
            tIn = TotalIn;
            tRew = TotalReward;
            tSales = TotalSales;
            tTrade = TotalTrade;
            tOut = TotalOut;
            tPurch = TotalPurchases;
        }

        var inc = new (string L, long V, string C)[]
        {
            ("Transfers rein", tIn,    "#4ADE80"),
            ("Belohnungen",    tRew,   "#FBBF24"),
            ("Item-Verkäufe",  tSales, "#34D399"),
            ("Fracht-Handel",  tTrade, "#22D3EE"),
        };
        var spd = new (string L, long V, string C)[]
        {
            ("Transfers raus", tOut,   "#F87171"),
            ("Käufe",          tPurch, "#FB923C"),
        };

        long max = 1;
        foreach (var x in inc) max = System.Math.Max(max, x.V);
        foreach (var x in spd) max = System.Math.Max(max, x.V);

        long totalInc = inc.Sum(i => i.V);
        long totalSpd = spd.Sum(i => i.V);

        IncomeStats.Clear();
        foreach (var x in inc.Where(i => i.V > 0).OrderByDescending(i => i.V))
        {
            double pct = totalInc > 0 ? ((double)x.V / totalInc * 100.0) : 0;
            IncomeStats.Add(new StatItem
            {
                Label = x.L,
                Value = x.V,
                Sub = $"{pct:F1}%",
                BarWidth = x.V / (double)max * BarMax,
                Color = Brush(x.C)
            });
        }

        SpendStats.Clear();
        foreach (var x in spd.Where(i => i.V > 0).OrderByDescending(i => i.V))
        {
            double pct = totalSpd > 0 ? ((double)x.V / totalSpd * 100.0) : 0;
            SpendStats.Add(new StatItem
            {
                Label = x.L,
                Value = x.V,
                Sub = $"{pct:F1}%",
                BarWidth = x.V / (double)max * BarMax,
                Color = Brush(x.C)
            });
        }

        OnPropertyChanged(nameof(IncomeTotalText));
        OnPropertyChanged(nameof(SpendTotalText));
        OnPropertyChanged(nameof(FinanceIncomeTotalText));
        OnPropertyChanged(nameof(FinanceSpendTotalText));
        OnPropertyChanged(nameof(FinanceNetBalanceText));
        OnPropertyChanged(nameof(FinanceNetSign));
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
            var b = StartBalance();
            return b > 0 ? $"{b:N0} aUEC" : "— eintragen —";
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

    public long ExpectedBalance => StartBalance() > 0 
        ? (_settings.BalanceSetAt.HasValue ? StartBalance() + NetSinceBalance : StartBalance())
        : 0;

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
        _eventSearchTimer.Tick += (_, _) =>
        {
            _eventSearchTimer.Stop();
            EventsView?.Refresh();
        };
        _settings = Settings.Load();
        if (_settings.Balance > 0)
        {
            manualBalance = _settings.Balance.ToString("N0");
        }

        EventsView = new DataGridCollectionView(Events)
        {
            Filter = o =>
            {
                if (o is not LogEntry e) return true;
                if (_activeKinds != null && !_activeKinds.Contains(e.Kind)) return false;
                if (!string.IsNullOrWhiteSpace(EventSearchText))
                {
                    if (!e.Detail.Contains(EventSearchText, StringComparison.OrdinalIgnoreCase) &&
                        !e.KindText.Contains(EventSearchText, StringComparison.OrdinalIgnoreCase) &&
                        !e.TimeText.Contains(EventSearchText, StringComparison.OrdinalIgnoreCase) &&
                        !e.AmountText.Contains(EventSearchText, StringComparison.OrdinalIgnoreCase) &&
                        !(e.Ship?.Contains(EventSearchText, StringComparison.OrdinalIgnoreCase) == true))
                    {
                        return false;
                    }
                }
                return true;
            }
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

        var facList = ReputationCatalog.CreateFreshFactionList();
        var savedRep = Database.LoadFactionReputations();
        foreach (var f in facList)
        {
            if (savedRep.TryGetValue(f.Id, out var stat))
            {
                f.CurrentXp = stat.Xp;
                f.CompletedMissions = stat.Missions;
                f.LastMissionTime = stat.LastUpdated;
                f.NotifyStateChanged();
            }
            Factions.Add(f);
        }

        FactionsView = new DataGridCollectionView(Factions)
        {
            Filter = o =>
            {
                if (o is not FactionReputation fac) return true;
                if (SelectedFactionCategory != "Alle" && fac.Category != SelectedFactionCategory) return false;
                return true;
            }
        };

        FleetView = new DataGridCollectionView(FleetItems)
        {
            Filter = o =>
            {
                if (o is not ShipFleetItem ship) return true;
                if (SelectedFleetViewMode == "Hangar" && !ship.IsInHangar) return false;
                if (SelectedFleetAcquisition != "Alle" && !ship.AcquisitionType.StartsWith(SelectedFleetAcquisition, StringComparison.OrdinalIgnoreCase)) return false;
                if (SelectedFleetManufacturer != "Alle" && !ship.ManufacturerBadge.Equals(SelectedFleetManufacturer, StringComparison.OrdinalIgnoreCase) && !ship.Manufacturer.Contains(SelectedFleetManufacturer, StringComparison.OrdinalIgnoreCase)) return false;
                if (!string.IsNullOrWhiteSpace(FleetSearchText) &&
                    !ship.Name.Contains(FleetSearchText, StringComparison.OrdinalIgnoreCase) &&
                    !ship.Manufacturer.Contains(FleetSearchText, StringComparison.OrdinalIgnoreCase) &&
                    !ship.Role.Contains(FleetSearchText, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                return true;
            }
        };

        TimelineView = new DataGridCollectionView(SessionTimeline)
        {
            Filter = o =>
            {
                if (o is not FlightTimelineItem item) return true;
                if (SelectedTimelineFilter == "Quantum" && item.Type != TimelineItemType.QuantumTravel) return false;
                if (SelectedTimelineFilter == "Schiffe" && item.Type != TimelineItemType.Spawn && item.Type != TimelineItemType.Departure) return false;
                if (SelectedTimelineFilter == "Orte" && item.Type != TimelineItemType.Arrival && item.Type != TimelineItemType.Generic) return false;
                if (SelectedTimelineFilter == "Verluste" && item.Type != TimelineItemType.CombatDeath) return false;
                if (!string.IsNullOrWhiteSpace(TimelineSearchText))
                {
                    if (!item.Title.Contains(TimelineSearchText, StringComparison.OrdinalIgnoreCase) &&
                        !item.Subtitle.Contains(TimelineSearchText, StringComparison.OrdinalIgnoreCase) &&
                        !item.LocationName.Contains(TimelineSearchText, StringComparison.OrdinalIgnoreCase) &&
                        !item.ShipName.Contains(TimelineSearchText, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                return true;
            }
        };
        AutoOcrEnabled = _settings.AutoOcrEnabled;
        OcrAvailable = _ocrEngine.IsAvailable;
        UpdateOcrRegionText();

        UexApiKeyInput = "";
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
        AutostartEnabled = AutostartHelper.IsAutostartEnabled();
        var langCode = _settings.AppLanguage ?? "Auto";
        SelectedLanguageOption = AvailableLanguages.FirstOrDefault(l => l.Code.Equals(langCode, StringComparison.OrdinalIgnoreCase))
                                 ?? AvailableLanguages[0];

        _walletCapture = new WalletCapture(_ocrEngine, () => _settings.WalletRegion ?? ScreenCapture.GetDefaultWalletRegion(), () => AutoOcrEnabled);
        _walletCapture.BalanceCaptured += OnBalanceCaptured;

        _contractScanner = new ContractScanner(
            _ocrEngine,
            () => _settings.ContractRegion ?? ScreenCapture.GetDefaultContractRegion(),
            () => false); // Mission OCR dauerhaft deaktiviert (Log-basiertes Tracking ist aktiv)

        _rsScanner = new RsOcrScanner(_ocrEngine, () => _settings.RsScanRegion ?? ScreenCapture.GetDefaultRsRegion(), () => IsRsAutoScanEnabled);
        _rsScanner.RsValueDetected += val => Dispatcher.UIThread.Post(() => OnRsDetected(val));
        _rsScanner.StatusChanged += s => Dispatcher.UIThread.Post(() => RsOcrStatusText = s);

        IsRsAutoScanEnabled = _settings.RsAutoScanEnabled;
        if (IsRsAutoScanEnabled)
        {
            _rsScanner.Start();
        }

        if (_settings.RsOverlayEnabled)
        {
            IsRsOverlayActive = true;
        }

        _auroraService = new AuroraVoiceService(_settings.AuroraCustomPath);
        IsAuroraInstalled = _auroraService.IsInstalled;
        AuroraPath = _auroraService.AuroraDirectory ?? "Nicht installiert";
        IsAuroraEnabled = _settings.AuroraIntegrationEnabled;
        AuroraVolume = _settings.AuroraVolume;
        IsAuroraShipGreetingsEnabled = _settings.AuroraShipGreetings;
        IsAuroraBlueprintsEnabled = _settings.AuroraBlueprints;
        IsAuroraSafetyZonesEnabled = _settings.AuroraSafetyZones;
        IsAuroraRestrictedZonesEnabled = _settings.AuroraRestrictedZones;
        IsAuroraMonitoredSpaceEnabled = _settings.AuroraMonitoredSpace;
        IsAuroraJurisdictionsEnabled = _settings.AuroraJurisdictions;
        IsAuroraQuantumArrivalEnabled = _settings.AuroraQuantumArrival;
        IsAuroraPlayerDeathEnabled = _settings.AuroraPlayerDeath;
        IsAuroraServerErrorsEnabled = _settings.AuroraServerErrors;

        _auroraService.IsEnabled = IsAuroraEnabled;
        _auroraService.Volume = AuroraVolume;
        _auroraService.ShipGreetingsEnabled = IsAuroraShipGreetingsEnabled;
        _auroraService.BlueprintsEnabled = IsAuroraBlueprintsEnabled;
        _auroraService.SafetyZonesEnabled = IsAuroraSafetyZonesEnabled;
        _auroraService.RestrictedZonesEnabled = IsAuroraRestrictedZonesEnabled;
        _auroraService.MonitoredSpaceEnabled = IsAuroraMonitoredSpaceEnabled;
        _auroraService.JurisdictionsEnabled = IsAuroraJurisdictionsEnabled;
        _auroraService.QuantumArrivalEnabled = IsAuroraQuantumArrivalEnabled;
        _auroraService.PlayerDeathEnabled = IsAuroraPlayerDeathEnabled;
        _auroraService.ServerErrorsEnabled = IsAuroraServerErrorsEnabled;
        _auroraService.IsAtStation = ResolvedLocation.Type is StarmapObjectType.SpaceStation 
            or StarmapObjectType.LagrangeStation 
            or StarmapObjectType.LandingZone 
            or StarmapObjectType.JumpPoint 
            or StarmapObjectType.Outpost
            || ResolvedLocation.DisplayName.Contains("Station", StringComparison.OrdinalIgnoreCase)
            || ResolvedLocation.DisplayName.Contains("Port", StringComparison.OrdinalIgnoreCase)
            || ResolvedLocation.DisplayName.Contains("Harbor", StringComparison.OrdinalIgnoreCase)
            || ResolvedLocation.DisplayName.Contains("Point", StringComparison.OrdinalIgnoreCase)
            || ResolvedLocation.DisplayName.Contains("Hangar", StringComparison.OrdinalIgnoreCase)
            || ResolvedLocation.DisplayName.Contains("Rest Stop", StringComparison.OrdinalIgnoreCase);


        // Gespeicherte aktive Aufträge aus der SQLite-Datenbank laden (Bereinigung von evtl. OCR-Fehlern)
        try
        {
            var savedContracts = Database.GetActiveContracts();
            foreach (var c in savedContracts)
            {
                var cleanTitle = c.Title?.Trim() ?? "";
                if (cleanTitle.Length < 3 || 
                    cleanTitle.Equals("Reward", StringComparison.OrdinalIgnoreCase) ||
                    cleanTitle.Contains("OCR", StringComparison.OrdinalIgnoreCase) ||
                    cleanTitle.StartsWith("ch SQn", StringComparison.OrdinalIgnoreCase))
                {
                    Database.RemoveContract(cleanTitle, c.Reward);
                    continue;
                }

                ActiveContracts.Add(c);
                var norm = ContractParser.NormalizeTitle(cleanTitle);
                if (norm.Length > 0) _knownContracts[norm] = c.Reward;
            }
            if (ActiveContracts.Count > 0)
            {
                var first = ActiveContracts[0];
                HasActiveContract = true;
                ActiveContractTitle = first.Title;
                ActiveContractRewardText = first.RewardText;
                ActiveContractOrg = first.ContractedBy;
                ContractStatusText = $"✓ {ActiveContracts.Count} aktive(r) Auftrag/Aufträge";
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
        UexApiKeyInput = "";
        if (!string.IsNullOrEmpty(_settings.UexApiKey))
        {
            UexApiClient.SetApiKey(_settings.UexApiKey);
            UexStatusMessage = "✓ API-Key gespeichert & aktiv";
            UexStatusColor = "#4ADE80";
            _ = CheckUexConnectionOnStartupAsync(_settings.UexApiKey);
        }
        OverlayOpacity = _settings.OverlayOpacity > 0 ? _settings.OverlayOpacity : 0.92;
        AutostartEnabled = _settings.AutostartEnabled;
        MinimizeToTrayOnClose = _settings.MinimizeToTrayOnClose;

        // Schriftart initialisieren
        var savedFont = _settings.SelectedFontFamily ?? "Inter";
        SelectedFontOption = savedFont switch
        {
            var f when f.Contains("Segoe UI") => "Segoe UI (Windows Standard)",
            var f when f.Contains("Cascadia") => "Cascadia Code (Monospace)",
            var f when f.Contains("Consolas") => "Consolas (Classic Code)",
            var f when f.Contains("Bahnschrift") => "Bahnschrift (Clean Tech)",
            var f when f.Contains("Arial") => "Arial (Universal Sans)",
            var f when f.Contains("Verdana") => "Verdana (Groß & Lesbar)",
            _ => "Inter (Modern & Klar)"
        };

        // Wipe- & Persistenz-Filter initialisieren
        WipeFilterEnabled = _settings.WipeFilterEnabled;
        WipeDateString = _settings.WipeDateString ?? "2026-05-15";
        WipeFilterMoney = _settings.WipeFilterMoney;
        WipeFilterContracts = _settings.WipeFilterContracts;
        WipeFilterFleet = _settings.WipeFilterFleet;
        WipeFilterBlueprints = _settings.WipeFilterBlueprints;

        // Toast-Kategorien initialisieren
        ToastBlueprintEnabled = _settings.ToastBlueprintEnabled;
        ToastMissionEnabled = _settings.ToastMissionEnabled;
        ToastReputationEnabled = _settings.ToastReputationEnabled;
        ToastRefineryEnabled = _settings.ToastRefineryEnabled;
        ToastElevatorEnabled = _settings.ToastElevatorEnabled;
        ToastShipDestructionEnabled = _settings.ToastShipDestructionEnabled;
        ToastSoundEnabled = _settings.ToastSoundEnabled;
        OverlayLocked = _settings.OverlayLocked;
        OverlayClickThrough = _settings.OverlayClickThrough;
        GlobalHotkeyEnabled = _settings.GlobalHotkeyEnabled;

        // Handelsrouten laden
        TradeRoutes.Clear();
        foreach (var r in TradingCatalog.CreatePopularRoutes(SelectedTradeShipCapacity)) TradeRoutes.Add(r);

        // Globaler Hotkey (Alt+H)
        if (GlobalHotkeyEnabled)
        {
            GlobalHotkey.HotkeyPressed += () => Dispatcher.UIThread.Post(() => ToggleOverlay());
            GlobalHotkey.Start();
        }

        // Raffinerie Live-Timer
        _refineryTimer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _refineryTimer.Tick += (_, _) => CheckRefineryJobs();
        _refineryTimer.Start();

        // Piloten-Ausrüstung & POIs laden
        InitPilotLoadoutSlots();
        LoadUserPois();

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

        // Automatische DB-Synchronisation & Hintergrund-Indexierung
        _ = AutoSyncAndIndexDatabaseAsync();

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

    public void RefreshGameStatusTexts()
    {
        if (IsGameRunning)
        {
            GameStatusText = I18n["Header_Status_Running"];
            GameStatusColor = "#3FB950";
            GameStatusBadgeBg = "#0E2818";
            GameStatusTooltip = I18n["Header_Status_Tooltip_Running"];
        }
        else
        {
            GameStatusText = I18n["Header_Status_Standby"];
            GameStatusColor = "#8B949E";
            GameStatusBadgeBg = "#161B22";
            GameStatusTooltip = I18n["Header_Status_Tooltip_Standby"];
        }
        OnPropertyChanged(nameof(GameStatusText));
        OnPropertyChanged(nameof(GameStatusColor));
        OnPropertyChanged(nameof(GameStatusBadgeBg));
        OnPropertyChanged(nameof(GameStatusTooltip));
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
                RefreshGameStatusTexts();
                if (running)
                {
                    Status = "★ Star Citizen gestartet – Live-Erfassung aktiv";
                    if (!Running)
                    {
                        LoadSession();
                    }
                }
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
            UpdateNewVersion = $"v{info.Version}";
            UpdateCurrentVersion = $"v{Updater.CurrentVersion}";
            UpdateReleaseNotes = !string.IsNullOrWhiteSpace(info.ReleaseNotes) ? info.ReleaseNotes : "Ein neues Update für SCLogMate ist auf GitHub verfügbar.";
            IsUpdateModalOpen = true;
            Status = $"Update {info.Version} verfügbar!";
        }
        else
        {
            Status = $"SCLogMate ist auf dem neuesten Stand (v{Updater.CurrentVersion}).";
        }
    }

    async void CheckForUpdate(bool showModal = true)
    {
        var info = await Updater.CheckAsync();
        if (info is null) return;
        Dispatcher.UIThread.Post(() =>
        {
            _update = info;
            UpdateAvailable = true;
            UpdateText = $"⬆ Update {info.Version}";
            UpdateNewVersion = $"v{info.Version}";
            UpdateCurrentVersion = $"v{Updater.CurrentVersion}";
            UpdateReleaseNotes = !string.IsNullOrWhiteSpace(info.ReleaseNotes) ? info.ReleaseNotes : "Ein neues Update für SCLogMate ist auf GitHub verfügbar.";
            if (showModal)
            {
                IsUpdateModalOpen = true;
            }
        });
    }

    [RelayCommand]
    private void OpenUpdateModal()
    {
        if (_update != null)
        {
            IsUpdateModalOpen = true;
        }
        else
        {
            _ = CheckForUpdateManual();
        }
    }

    [RelayCommand]
    private void CloseUpdateModal()
    {
        IsUpdateModalOpen = false;
    }

    [RelayCommand]
    private void OpenReleaseInBrowser()
    {
        try
        {
            var url = _update?.HtmlUrl ?? "https://github.com/gOOvER/SCLogMate/releases";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch { }
    }

    [RelayCommand]
    private async Task Update()
    {
        if (_update is null || IsInstallingUpdate) return;
        IsInstallingUpdate = true;
        UpdateStatusText = $"Lade Update {_update.Version} herunter...";
        Status = $"Lade Update {_update.Version}…";
        try
        {
            await Updater.ApplyAsync(_update);
            UpdateStatusText = "Update wird installiert – SCLogMate startet neu…";
            Status = "Update wird installiert – Neustart…";
            await Task.Delay(500);
            (Avalonia.Application.Current?.ApplicationLifetime
                as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.Shutdown();
        }
        catch (Exception ex)
        {
            IsInstallingUpdate = false;
            UpdateStatusText = $"Fehler beim Aktualisieren: {ex.Message}";
        }
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
            SelectedSession = found.FirstOrDefault() ?? Sessions.FirstOrDefault();   // Default: Aktuelle Session
        _initializing = false;

        RefreshFlightSessions();
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

        var wipeSince = GetEffectiveWipeDate();
        var fleetStats = Database.GetFleetStats(WipeFilterFleet ? wipeSince : null);
        RebuildFleet(fleetStats);

        // Globale unabhängige Subsysteme sicherstellen (Finanzen, Flugschreiber, Missionen laden alle historischen Sessions)
        if (_historyLoaded)
        {
            RebuildIndependentFinances();
            RebuildIndependentTimeline();
            RebuildIndependentMissions();
        }
        else
        {
            LoadGlobalDataAsync();
        }

        if (SelectedSession?.IsAll == true) { LoadAllSessions(); return; }

        _tailer = new LogTailer(LogPath);
        _tailer.LineEx += (line, isLive) => OnLine(line, isLive);
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

                // 4) Summen/Top per SQL + ALLE Events für die Tabelle (virtualisiert) mit Wipe-Filter
                var wipeSince = GetEffectiveWipeDate();
                var agg = Database.Aggregate(wipeSince, WipeFilterMoney, WipeFilterContracts, WipeFilterFleet);
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

        // Piloten-Loadout aus historischen Events initialisieren
        foreach (var ev in recent.Where(x => x.Kind == EventKind.Loadout))
        {
            UpdatePilotLoadout(ev.Detail, ev.Time);
        }

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

        var wipeSince = GetEffectiveWipeDate();
        var fleetStats = Database.GetFleetStats(WipeFilterFleet ? wipeSince : null);
        RebuildFleet(fleetStats);

        _sessionStart = agg.Start;
        _sessionEnd = agg.End;
        _running = ExpectedBalance;   // Stand vom Eintrag + Bewegungen danach (nicht die ganze Historie)

        foreach (var n in new[] { nameof(IncomeAll), nameof(SpendAll), nameof(NetAll), nameof(NetBalanceText),
                 nameof(NetSign), nameof(FlowText), nameof(TradeText), nameof(ExpectedText), nameof(ExpectedBalance),
                 nameof(SessionSpanText), nameof(FleetText), nameof(ShipsSeenText), nameof(MissionsText),
                 nameof(TotalFleetValue), nameof(TotalFleetValueText), nameof(TotalFleetFlights), nameof(TotalFleetQuantumJumps), nameof(CurrentShipFlightInfo) })
            OnPropertyChanged(n);

        SyncBlueprints();
        RebuildTimeline();
        RebuildFinanceChart();

        Status = $"alle Sessions (DB: {agg.Sessions}) – laufende live…";

        // laufende Game.log LIVE dazu tailen (zählt einmal oben drauf)
        _tailer?.Stop();
        _tailer = new LogTailer(liveLog);
        _tailer.LineEx += (line, isLive) => OnLine(line, isLive);
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
        SessionTimeline.Clear();
        FlightSummary = new();
        ShipsSeen.Clear();
        FleetItems.Clear();
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
        _liveTimelineEvents.Clear();
        _liveMissionEvents.Clear();
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

    void OnLine(string line, bool isLive = false)
    {
        _walletCapture.ProcessLine(line);
        if (isLive)
        {
            _auroraService.ProcessLiveLine(line);
        }

        var e = _parser.Feed(line);
        if (e == null) return;

        if (isLive)
        {
            Dispatcher.UIThread.Post(() => Apply(e, true));
            return;
        }

        lock (_initialEventQueueLock)
        {
            _initialEventQueue.Add((e, false));
            if (_initialEventDrainScheduled) return;
            _initialEventDrainScheduled = true;
        }

        Dispatcher.UIThread.Post(DrainInitialEventQueue);
    }

    void DrainInitialEventQueue()
    {
        List<(LogEntry Entry, bool IsLive)> batch;
        lock (_initialEventQueueLock)
        {
            var count = Math.Min(250, _initialEventQueue.Count);
            batch = _initialEventQueue.GetRange(0, count);
            _initialEventQueue.RemoveRange(0, count);
            _initialEventDrainScheduled = _initialEventQueue.Count > 0;
        }

        foreach (var (entry, isLive) in batch)
            Apply(entry, isLive);

        lock (_initialEventQueueLock)
        {
            if (_initialEventDrainScheduled)
                Dispatcher.UIThread.Post(DrainInitialEventQueue);
        }
    }

    // Ein erkanntes Ereignis verarbeiten (Totals, Saldo, Flotte, Liste). Immer auf UI-Thread.
    void Apply(LogEntry e, bool isLive = false)
    {
        if (isLive)
        {
            _auroraService.ProcessLiveEvent(e);
        }

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
                    if (e.Amount <= 0)
                    {
                        var cleanTitle = Regex.Replace(e.Detail, @"^(Contract|Mission|Objective)\s+(Complete|Completed|Accepted|Finished|Erfolgreich):\s*", "", RegexOptions.IgnoreCase).Trim();
                        var cat = MissionCatalog.FuzzyLookup(cleanTitle) ?? MissionCatalog.FuzzyLookup(e.Detail);
                        e.Amount = cat != null && cat.BaseReward > 0 ? cat.BaseReward : 25000;
                    }
                    TotalReward += e.Amount;
                    HandleMissionCompleted(e.Detail, e.Amount);
                    if (isLive && e.Amount > 0)
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
                    bool isNewSortie = _sessionFlownShips.Add(e.Detail);
                    RegisterOrUpdateShip(e.Detail, isFlight: isNewSortie, isQt: false, isLoss: false, time: e.Time, location: CurrentLocation);
                    break;
                case EventKind.Quantum:
                    var qShip = !string.IsNullOrEmpty(e.Ship) ? e.Ship : CurrentShip;
                    RegisterOrUpdateShip(qShip, isFlight: false, isQt: true, isLoss: false, time: e.Time, location: CurrentLocation);
                    if (!string.IsNullOrEmpty(e.Location))
                    {
                        CurrentLocation = e.Location;
                    }
                    break;
                case EventKind.ShipLoss:
                    var lShip = !string.IsNullOrEmpty(e.Ship) ? e.Ship : CurrentShip;
                    RegisterOrUpdateShip(lShip, isFlight: false, isQt: false, isLoss: true, time: e.Time, location: CurrentLocation);
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
                    if (isLive)
                    {
                        TriggerAchievementToast(AchievementToastData.ForBlueprint(e.Detail));
                    }
                    break;
                case EventKind.Loadout:
                    UpdatePilotLoadout(e.Detail, e.Time);
                    break;
                case EventKind.Loot:
                    if (isLive)
                    {
                        TriggerAchievementToast(AchievementToastData.ForLoot(e.Detail));
                    }
                    break;
                case EventKind.MissionTaken:
                    RebuildMissions(Events.Where(x => x.Kind == EventKind.MissionTaken));
                    break;
                case EventKind.Crash:
                    if (isLive)
                    {
                        ClearContracts();
                        ContractStatusText = "⚠ Spiel abgestürzt – Aufträge zurückgesetzt";
                        Status = "⚠ Star Citizen Absturz erkannt! Aktive Aufträge wurden zurückgesetzt.";
                    }
                    break;
                case EventKind.SessionChange:
                    Status = e.Detail ?? "Server-/Session-Wechsel";
                    break;
            }

            // Mitlaufender Kontostand bei Geld-Ereignissen
            if (IsMoney(e.Kind))
            {
                var since = _settings.BalanceSetAt;
                if (since.HasValue && e.Time > since.Value)
                {
                    _running += e.Amount;
                    e.BalanceAfter = _running;
                    e.HasBalance = true;
                }
                else
                {
                    e.HasBalance = false;
                }

                _liveMoney.Add(e);
                RebuildIndependentFinances();
            }

            if (IsTimelineRelevant(e))
            {
                _liveTimelineEvents.Add(e);
                RebuildIndependentTimeline();
            }

            if (e.Kind == EventKind.MissionTaken)
            {
                _liveMissionEvents.Add(e);
                RebuildIndependentMissions();
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

    private void HandleMissionCompleted(string? detail, long passedReward = 0)
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

        long reward = matchContract != null && matchContract.Reward > 0 ? matchContract.Reward : passedReward;
        var completedTitle = matchContract?.Title ?? missionTitle;

        if (matchContract != null)
        {
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
        }

        if (reward > 0)
        {
            Status = $"★ Auftrag abgeschlossen & Belohnung verbucht: {completedTitle} · +{reward:N0} aUEC";
        }
        else
        {
            Status = $"★ Auftrag abgeschlossen: {completedTitle}";
        }

        // Fraktionsruf & XP automatisch verbuchen
        var fac = ReputationCatalog.MatchFaction(matchContract?.ContractedBy) ?? ReputationCatalog.MatchFaction(completedTitle);
        if (fac != null)
        {
            var target = Factions.FirstOrDefault(f => f.Id == fac.Id);
            if (target != null)
            {
                int xpGained = (int)Math.Max(500, Math.Min(3500, reward > 0 ? reward / 10 : 750));
                target.CurrentXp += xpGained;
                target.CompletedMissions += 1;
                target.LastMissionTime = DateTime.UtcNow;
                target.NotifyStateChanged();
                Database.AddFactionReputationXp(target.Id, xpGained, DateTime.UtcNow);
                FactionsView?.Refresh();
                Status += $" · +{xpGained:N0} XP ({target.ShortName})";
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
        string org = cat != null && !string.IsNullOrEmpty(cat.Contractor) ? cat.Contractor : (cat?.Faction ?? "");

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
            if (finalReward > 0 && existing.Reward <= 0) existing.Reward = finalReward;
            if (!string.IsNullOrEmpty(org) && string.IsNullOrEmpty(existing.ContractedBy)) existing.ContractedBy = org;
            if (contract.Title.Length > existing.Title.Length) existing.Title = contract.Title;
            existing.ScannedAt = DateTime.UtcNow;
            Database.SaveContract(existing);

            ActiveContractRewardText = existing.RewardText;
            ActiveContractOrg = existing.ContractedBy;
            ActiveContractTitle = existing.Title;
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
    public void RemoveContract(ContractDetails contract)
    {
        if (contract == null) return;
        ActiveContracts.Remove(contract);
        Database.RemoveContract(contract.Title, contract.Reward);
        HasActiveContract = ActiveContracts.Count > 0;
        ActiveContractTitle = ActiveContracts.FirstOrDefault()?.Title ?? "Kein Auftrag";
        ActiveContractRewardText = ActiveContracts.FirstOrDefault()?.RewardText ?? "";
        ActiveContractOrg = ActiveContracts.FirstOrDefault()?.ContractedBy ?? "";
        ContractStatusText = ActiveContracts.Count == 0 ? "Kein aktiver Auftrag" : ActiveContracts.Count == 1 ? "1 aktiver Auftrag" : $"{ActiveContracts.Count} aktive Aufträge";
        OnPropertyChanged(nameof(HasActiveContracts));
        OnPropertyChanged(nameof(ActiveContractsCountText));
    }



    [RelayCommand]
    public void SetMissionAsActiveContract(MissionInfo mission)
    {
        if (mission == null) return;
        var contract = new ContractDetails
        {
            Title = mission.Title,
            Reward = (int)mission.BaseReward,
            ContractedBy = !string.IsNullOrEmpty(mission.Contractor) ? mission.Contractor : (!string.IsNullOrEmpty(mission.Faction) ? mission.Faction : "mobiGlas"),
            ScannedAt = DateTime.UtcNow
        };

        var existing = ActiveContracts.FirstOrDefault(c => ContractParser.AreSameContract(c, contract));
        if (existing == null)
        {
            ActiveContracts.Insert(0, contract);
            Database.SaveContract(contract);
        }

        HasActiveContract = true;
        ActiveContractTitle = contract.Title;
        ActiveContractRewardText = contract.RewardText;
        ActiveContractOrg = contract.ContractedBy;
        ContractStatusText = ActiveContracts.Count == 1 ? "1 aktiver Auftrag" : $"{ActiveContracts.Count} aktive Aufträge";
        OnPropertyChanged(nameof(HasActiveContracts));
        OnPropertyChanged(nameof(ActiveContractsCountText));
        Status = $"★ Auftrag gesetzt: {contract.Title}";
    }

    [RelayCommand]
    public void OpenMissionsTab()
    {
        SelectedTabIndex = 2; // Tab '❖ Missionen'
    }

    [RelayCommand]
    public void OpenReputationTab()
    {
        SelectedTabIndex = 3; // Tab '🎖 Ruf'
    }

    [RelayCommand]
    public void SelectFactionCategory(string category)
    {
        SelectedFactionCategory = category;
        FactionsView?.Refresh();
    }

    [RelayCommand]
    public void ToggleStarmapFullscreen()
    {
        IsStarmapFullscreen = !IsStarmapFullscreen;
        OnPropertyChanged(nameof(ShowDashboardCards));
    }

    [RelayCommand]
    public void OpenStarmapPopoutWindow()
    {
        var win = new Views.StarmapWindow(this);
        win.Show();
    }

    [RelayCommand]
    public void JumpToTargetSystem()
    {
        if (SelectedStarmapObject != null && !string.IsNullOrEmpty(SelectedStarmapObject.TargetSystem))
        {
            SelectedStarmapSystem = SelectedStarmapObject.TargetSystem;
            SelectedStarmapObject = null;
            Status = $"🌀 Sprungtor durchquert: System gewechselt nach {SelectedStarmapSystem}!";
        }
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
    private void ResetStarmapView()
    {
        SelectedStarmapObject = null;
    }

    [RelayCommand]
    private void OpenStarmapForCurrentLocation()
    {
        if (!string.IsNullOrEmpty(ResolvedLocation.SystemName) && StarmapData.SystemNames.Contains(ResolvedLocation.SystemName))
        {
            SelectedStarmapSystem = ResolvedLocation.SystemName;
        }
        SelectedStarmapObject = StarmapData.FindObject(ResolvedLocation.DisplayName) ?? StarmapData.FindObject(ResolvedLocation.ParentBody);
        StarmapFocusRequest++;
        SelectedTabIndex = 4; // Tab '🗺 Karte'
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
    }

    private async Task CheckUexConnectionOnStartupAsync(string key)
    {
        try
        {
            var (success, msg) = await UexApiClient.TestConnectionAsync(key);
            if (success)
            {
                UexStatusMessage = msg;
                UexStatusColor = "#4ADE80";
            }
        }
        catch { /* ignore */ }
    }

    partial void OnMinimizeToTrayOnCloseChanged(bool value)
    {
        if (_initializing || !_ready) return;
        _settings.MinimizeToTrayOnClose = value;
        Settings.Save(_settings);
    }

    partial void OnAutostartEnabledChanged(bool value)
    {
        if (_initializing || !_ready) return;
        AutostartHelper.SetAutostart(value);
        _settings.AutostartEnabled = value;
        Settings.Save(_settings);
        Status = value ? "✓ Windows-Autostart aktiviert (minimiert ins Tray)" : "Windows-Autostart deaktiviert";
    }

    partial void OnEventSearchTextChanged(string value)
    {
        _eventSearchTimer.Stop();
        _eventSearchTimer.Start();
    }

    [RelayCommand]
    public void ClearEventSearch()
    {
        EventSearchText = "";
    }

    [RelayCommand]
    private async Task CopyEntryDetail(LogEntry? entry)
    {
        var e = entry ?? SelectedEntry;
        if (e == null || string.IsNullOrWhiteSpace(e.Detail)) return;
        if (UiServices.TopLevel?.Clipboard is { } clip)
        {
            await clip.SetTextAsync(e.Detail);
            Status = $"📋 Text in Zwischenablage kopiert: \"{e.Detail}\"";
        }
    }

    [RelayCommand]
    private async Task CopyEntryAmount(LogEntry? entry)
    {
        var e = entry ?? SelectedEntry;
        if (e == null || e.Amount == 0) return;
        if (UiServices.TopLevel?.Clipboard is { } clip)
        {
            await clip.SetTextAsync(e.Amount.ToString());
            Status = $"💰 Betrag kopiert: {e.Amount:N0} aUEC";
        }
    }

    [RelayCommand]
    private async Task CopyEntryLine(LogEntry? entry)
    {
        var e = entry ?? SelectedEntry;
        if (e == null) return;
        if (UiServices.TopLevel?.Clipboard is { } clip)
        {
            var line = $"{e.TimeText}\t{e.KindText}\t{e.AmountText}\t{e.BalanceAfterText}\t{e.Detail}";
            await clip.SetTextAsync(line);
            Status = "📋 Tabellenzeile in Zwischenablage kopiert";
        }
    }

    [RelayCommand]
    public void JumpToMissionsTab()
    {
        SelectedTabIndex = 2; // ❖ Missionen
    }

    [RelayCommand]
    public void JumpToReputationTab()
    {
        SelectedTabIndex = 3; // 🎖 Ruf
    }

    [RelayCommand]
    public void JumpToStarmapTab()
    {
        SelectedTabIndex = 4; // 🗺 Karte
    }

    [RelayCommand]
    public void JumpToTimelineTab()
    {
        SelectedTabIndex = 5; // ⏱ Flugschreiber
    }

    [RelayCommand]
    public void JumpToRsScannerTab()
    {
        SelectedTabIndex = 6; // ⛏ Mining
    }

    [RelayCommand]
    public void JumpToBlueprintsTab()
    {
        SelectedTabIndex = 8; // 🛠 Baupläne
    }

    public IReadOnlyList<RsResource> AllCatalogResources => RsDecoderCatalog.AllResources;

    [RelayCommand]
    public async Task SaveAndTestUexApiKey()
    {
        var cleanKey = string.IsNullOrWhiteSpace(UexApiKeyInput) ? null : UexApiKeyInput.Trim();
        _settings.UexApiKey = cleanKey;
        Settings.SaveUexApiKey(cleanKey);
        Settings.Save(_settings);
        UexApiKeyInput = "";
        UexApiClient.SetApiKey(cleanKey);

        if (cleanKey == null)
        {
            UexStatusMessage = "✓ API-Key entfernt (Öffentlicher Modus aktiv)";
            UexStatusColor = "#8B949E";
            Status = "UEX API-Key entfernt";
            return;
        }

        UexStatusMessage = "Prüfe UEX Corp API-Verbindung...";
        UexStatusColor = "#58A6FF";

        var (success, msg) = await UexApiClient.TestConnectionAsync(cleanKey);
        UexStatusMessage = success ? msg : $"⚠ {msg}";
        UexStatusColor = success ? "#4ADE80" : "#F87171";
        Status = $"UEX API: {msg}";
    }

    partial void OnSimulateAuroraNotInstalledChanged(bool value)
    {
        if (!IsDebugModeActive)
        {
            if (value)
            {
                Dispatcher.UIThread.Post(() => SimulateAuroraNotInstalled = false);
            }
            return;
        }

        if (value)
        {
            IsAuroraInstalled = false;
            AuroraPath = "Nicht installiert (Simulation aktiv)";
            if (_auroraService != null) _auroraService.IsEnabled = false;
        }
        else
        {
            IsAuroraInstalled = _auroraService?.IsInstalled ?? false;
            AuroraPath = _auroraService?.AuroraDirectory ?? "Nicht installiert";
            if (_auroraService != null) _auroraService.IsEnabled = IsAuroraEnabled;
        }
        OnPropertyChanged(nameof(ShowAuroraPurchaseBanner));
    }

    partial void OnIsAuroraEnabledChanged(bool value)
    {
        _auroraService.IsEnabled = value;
        _settings.AuroraIntegrationEnabled = value;
        Settings.Save(_settings);
    }

    partial void OnAuroraVolumeChanged(int value)
    {
        _auroraService.Volume = value;
        _settings.AuroraVolume = value;
        Settings.Save(_settings);
    }

    partial void OnIsAuroraShipGreetingsEnabledChanged(bool value)
    {
        _auroraService.ShipGreetingsEnabled = value;
        _settings.AuroraShipGreetings = value;
        Settings.Save(_settings);
    }

    partial void OnIsAuroraBlueprintsEnabledChanged(bool value)
    {
        _auroraService.BlueprintsEnabled = value;
        _settings.AuroraBlueprints = value;
        Settings.Save(_settings);
    }

    partial void OnIsAuroraSafetyZonesEnabledChanged(bool value)
    {
        _auroraService.SafetyZonesEnabled = value;
        _settings.AuroraSafetyZones = value;
        Settings.Save(_settings);
    }

    partial void OnIsAuroraRestrictedZonesEnabledChanged(bool value)
    {
        _auroraService.RestrictedZonesEnabled = value;
        _settings.AuroraRestrictedZones = value;
        Settings.Save(_settings);
    }

    partial void OnIsAuroraMonitoredSpaceEnabledChanged(bool value)
    {
        _auroraService.MonitoredSpaceEnabled = value;
        _settings.AuroraMonitoredSpace = value;
        Settings.Save(_settings);
    }

    partial void OnIsAuroraJurisdictionsEnabledChanged(bool value)
    {
        _auroraService.JurisdictionsEnabled = value;
        _settings.AuroraJurisdictions = value;
        Settings.Save(_settings);
    }

    partial void OnIsAuroraQuantumArrivalEnabledChanged(bool value)
    {
        _auroraService.QuantumArrivalEnabled = value;
        _settings.AuroraQuantumArrival = value;
        Settings.Save(_settings);
    }

    partial void OnIsAuroraPlayerDeathEnabledChanged(bool value)
    {
        _auroraService.PlayerDeathEnabled = value;
        _settings.AuroraPlayerDeath = value;
        Settings.Save(_settings);
    }

    partial void OnIsAuroraServerErrorsEnabledChanged(bool value)
    {
        _auroraService.ServerErrorsEnabled = value;
        _settings.AuroraServerErrors = value;
        Settings.Save(_settings);
    }

    [RelayCommand]
    private void PlayAuroraTestSound()
    {
        _auroraService.PlayTestSound();
    }

    async partial void OnCurrentShipChanged(string value)

    {
        foreach (var s in FleetItems)
        {
            s.IsCurrent = s.Name.Equals(value, StringComparison.OrdinalIgnoreCase) || (!string.IsNullOrEmpty(value) && value.Contains(s.Name, StringComparison.OrdinalIgnoreCase));
        }
        OnPropertyChanged(nameof(CurrentShipFlightInfo));
        OnPropertyChanged(nameof(FleetText));

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
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                if (_overlayWindow == null)
                {
                    _overlayWindow = new Views.FloatingOverlayWindow { DataContext = this };
                    _overlayWindow.InitSettings(_settings);
                }

                if (!_overlayWindow.IsVisible)
                {
                    _overlayWindow.Show();
                    IsOverlayActive = true;
                    _settings.OverlayEnabled = true;
                    Settings.Save(_settings);
                    Status = "🖥 In-Game Mini-HUD Overlay eingeblendet";
                }
                else
                {
                    _overlayWindow.Hide();
                    IsOverlayActive = false;
                    _settings.OverlayEnabled = false;
                    Settings.Save(_settings);
                    Status = "In-Game Mini-HUD Overlay ausgeblendet";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ToggleOverlay", ex);
                Status = "Fehler beim Öffnen des Mini-HUD: " + ex.Message;
                _overlayWindow = null;
                IsOverlayActive = false;
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

    partial void OnToastBlueprintEnabledChanged(bool value) { _settings.ToastBlueprintEnabled = value; Settings.Save(_settings); }
    partial void OnToastMissionEnabledChanged(bool value) { _settings.ToastMissionEnabled = value; Settings.Save(_settings); }
    partial void OnToastReputationEnabledChanged(bool value) { _settings.ToastReputationEnabled = value; Settings.Save(_settings); }
    partial void OnToastRefineryEnabledChanged(bool value) { _settings.ToastRefineryEnabled = value; Settings.Save(_settings); }
    partial void OnToastElevatorEnabledChanged(bool value) { _settings.ToastElevatorEnabled = value; Settings.Save(_settings); }
    partial void OnToastShipDestructionEnabledChanged(bool value) { _settings.ToastShipDestructionEnabled = value; Settings.Save(_settings); }
    partial void OnToastSoundEnabledChanged(bool value) { _settings.ToastSoundEnabled = value; Settings.Save(_settings); }
    partial void OnOverlayLockedChanged(bool value) { _settings.OverlayLocked = value; Settings.Save(_settings); _overlayWindow?.ApplyWindowStyles(); }
    partial void OnOverlayClickThroughChanged(bool value) { _settings.OverlayClickThrough = value; Settings.Save(_settings); _overlayWindow?.ApplyWindowStyles(); }
    partial void OnGlobalHotkeyEnabledChanged(bool value) { _settings.GlobalHotkeyEnabled = value; Settings.Save(_settings); }

    public void TriggerAchievementToast(AchievementToastData toast)
    {
        if (!ToastOverlayEnabled) return;
        if (toast.Type == AchievementToastType.Blueprint && !ToastBlueprintEnabled) return;
        if (toast.Type == AchievementToastType.MissionReward && !ToastMissionEnabled) return;
        if (toast.Type == AchievementToastType.ReputationPromotion && !ToastReputationEnabled) return;
        if (toast.Type == AchievementToastType.RefineryCompleted && !ToastRefineryEnabled) return;
        if (toast.Type == AchievementToastType.ElevatorReady && !ToastElevatorEnabled) return;
        if (toast.Type == AchievementToastType.ShipDestroyed && !ToastShipDestructionEnabled) return;

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
        switch (_testToastCounter % 6)
        {
            case 1:
                TriggerAchievementToast(AchievementToastData.ForBlueprint("Strata Helmet Levski Edition"));
                break;
            case 2:
                TriggerAchievementToast(AchievementToastData.ForMissionReward("Missing Mining Team", 26750));
                break;
            case 3:
                TriggerAchievementToast(AchievementToastData.ForReputationPromotion("Crusader Security", "Senior Deputy", 3));
                break;
            case 4:
                TriggerAchievementToast(AchievementToastData.ForRefineryCompleted("Quantanium", 32, "HUR-L1 Green Glade"));
                break;
            case 5:
                TriggerAchievementToast(AchievementToastData.ForElevatorReady("Frachtaufzug 02", "Area18 Riker Spaceport"));
                break;
            case 0:
                TriggerAchievementToast(AchievementToastData.ForShipDestroyed("Drake Cutlass Black", claimAvailable: true));
                break;
        }
        Status = "✦ Toast-Banner Test ausgelöst – klicke und ziehe das Banner zum Verschieben!";
    }

    [RelayCommand]
    public void ToggleToastOverlay()
    {
        ToastOverlayEnabled = !ToastOverlayEnabled;
        _settings.ToastEnabled = ToastOverlayEnabled;
        Settings.Save(_settings);
        Status = ToastOverlayEnabled ? "✦ Achievement-Banner aktiviert" : "Achievement-Banner deaktiviert";
    }

    // ==========================================
    // Raffinerie-Tracking & Countdown
    // ==========================================
    private void CheckRefineryJobs()
    {
        foreach (var job in RefineryJobs)
        {
            job.RefreshTime();

            if (job.IsCompleted && !job.HasNotifiedDone && !job.IsCollected)
            {
                job.HasNotifiedDone = true;
                TriggerAchievementToast(AchievementToastData.ForRefineryCompleted(job.Material, job.OutputScu, job.Station));
            }
        }
    }

    [RelayCommand]
    public void AddRefineryJob()
    {
        var durHours = NewRefineryMethod.Contains("Sehr schnell") ? 1.5 : (NewRefineryMethod.Contains("Schnell") ? 2.5 : 4.5);
        var cost = NewRefineryScu * (NewRefineryMaterial == "Quantanium" ? 450 : 180);
        var estVal = NewRefineryScu * (NewRefineryMaterial == "Quantanium" ? 24500 : 4200);

        var job = new RefineryJob
        {
            Station = NewRefineryStation,
            Material = NewRefineryMaterial,
            Method = NewRefineryMethod,
            InputUnits = NewRefineryScu * 100,
            OutputScu = (int)(NewRefineryScu * 0.92),
            CostAuec = cost,
            EstimatedValueAuec = estVal,
            StartedAt = DateTime.UtcNow,
            Duration = TimeSpan.FromHours(durHours)
        };
        RefineryJobs.Insert(0, job);
        Status = $"✓ Veredelungsauftrag {job.OutputScu} SCU {job.Material} auf {job.Station} gestartet";
    }

    [RelayCommand]
    public void CollectRefineryJob(RefineryJob? job)
    {
        if (job == null) return;
        job.IsCollected = true;
        Status = $"✓ Veredelung {job.OutputScu} SCU {job.Material} als abgeholt markiert";
    }

    [RelayCommand]
    public void DeleteRefineryJob(RefineryJob? job)
    {
        if (job == null) return;
        RefineryJobs.Remove(job);
        Status = "Veredelungsauftrag entfernt";
    }

    partial void OnSelectedTradeShipCapacityChanged(int value) => UpdateTradeRouteProfits();

    private void UpdateTradeRouteProfits()
    {
        foreach (var r in TradeRoutes)
        {
            r.EstimatedRunProfit = r.ProfitPerScu * Math.Max(1, SelectedTradeShipCapacity);
        }
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

    // ==========================================
    // Piloten-Ausrüstung (Visual Loadout Slots)
    // ==========================================
    private void InitPilotLoadoutSlots()
    {
        PilotLoadoutSlots.Clear();
        PilotLoadoutSlots.Add(new LoadoutItem { Slot = LoadoutSlotType.Helmet, SlotName = "Helm", Icon = "🪖" });
        PilotLoadoutSlots.Add(new LoadoutItem { Slot = LoadoutSlotType.Torso, SlotName = "Torso / Core", Icon = "🥋" });
        PilotLoadoutSlots.Add(new LoadoutItem { Slot = LoadoutSlotType.Arms, SlotName = "Arme", Icon = "🦾" });
        PilotLoadoutSlots.Add(new LoadoutItem { Slot = LoadoutSlotType.Legs, SlotName = "Beine", Icon = "🦿" });
        PilotLoadoutSlots.Add(new LoadoutItem { Slot = LoadoutSlotType.Undersuit, SlotName = "Undersuit", Icon = "🩱" });
        PilotLoadoutSlots.Add(new LoadoutItem { Slot = LoadoutSlotType.Backpack, SlotName = "Rucksack", Icon = "🎒" });
        PilotLoadoutSlots.Add(new LoadoutItem { Slot = LoadoutSlotType.Primary1, SlotName = "Primärwaffe 1", Icon = "🎯" });
        PilotLoadoutSlots.Add(new LoadoutItem { Slot = LoadoutSlotType.Primary2, SlotName = "Primärwaffe 2", Icon = "🎯" });
        PilotLoadoutSlots.Add(new LoadoutItem { Slot = LoadoutSlotType.Sidearm, SlotName = "Seitenwaffe", Icon = "🔫" });
        PilotLoadoutSlots.Add(new LoadoutItem { Slot = LoadoutSlotType.MultiTool, SlotName = "Multi-Tool", Icon = "🔧" });
        PilotLoadoutSlots.Add(new LoadoutItem { Slot = LoadoutSlotType.MedItem, SlotName = "Med-Kit / Pen", Icon = "💉" });
    }

    public string AverageArmorReductionText
    {
        get
        {
            var armorPieces = PilotLoadoutSlots.Where(s => (s.Slot is LoadoutSlotType.Helmet or LoadoutSlotType.Torso or LoadoutSlotType.Arms or LoadoutSlotType.Legs) && s.IsEquipped).ToList();
            if (armorPieces.Count == 0) return "Keine Panzerung";
            var avg = (int)armorPieces.Average(s => s.DamageReductionPercent);
            return $"{avg}% Reduktion ({armorPieces.Count}/4 Teile)";
        }
    }

    public string CombinedTempResistText
    {
        get
        {
            var suit = PilotLoadoutSlots.FirstOrDefault(s => s.Slot == LoadoutSlotType.Undersuit && s.IsEquipped);
            if (suit != null && !string.IsNullOrEmpty(suit.TemperatureMinMaxText)) return suit.TemperatureMinMaxText;
            var torso = PilotLoadoutSlots.FirstOrDefault(s => s.Slot == LoadoutSlotType.Torso && s.IsEquipped);
            if (torso != null && !string.IsNullOrEmpty(torso.TemperatureMinMaxText)) return torso.TemperatureMinMaxText;
            return "-40°C bis +65°C";
        }
    }

    public string TotalBackpackCapacityText
    {
        get
        {
            var bp = PilotLoadoutSlots.FirstOrDefault(s => s.Slot == LoadoutSlotType.Backpack && s.IsEquipped);
            if (bp != null && !string.IsNullOrEmpty(bp.BackpackCapacityText)) return bp.BackpackCapacityText;
            return "Standard (25k µSCU)";
        }
    }

    public string EquippedWeaponsSummaryText
    {
        get
        {
            var weaps = PilotLoadoutSlots.Where(s => (s.Slot is LoadoutSlotType.Primary1 or LoadoutSlotType.Primary2 or LoadoutSlotType.Sidearm) && s.IsEquipped).ToList();
            if (weaps.Count == 0) return "Keine Waffen";
            return string.Join(" · ", weaps.Select(w => w.ItemName));
        }
    }

    public void UpdatePilotLoadout(string rawItemName, DateTime timestamp)
    {
        if (string.IsNullOrWhiteSpace(rawItemName)) return;
        var (slotType, slotName, icon) = LogParser.ClassifyLoadoutSlot(rawItemName);
        var clean = Localization.ItemName(rawItemName) ?? rawItemName;

        var slot = PilotLoadoutSlots.FirstOrDefault(s => s.Slot == slotType);
        if (slot != null)
        {
            slot.ItemName = clean;
            slot.RawClass = rawItemName;
            slot.LastObserved = timestamp;
            slot.Icon = icon;

            var (armorClass, dmgRed, tempRange, badgeColor, attachments, capacity) = LoadoutCatalog.GetItemMeta(slotType, rawItemName, clean);
            slot.ArmorClass = armorClass;
            slot.DamageReductionPercent = dmgRed;
            slot.DamageReductionText = dmgRed > 0 ? $"🛡 {dmgRed}% Reduktion" : "";
            slot.TemperatureMinMaxText = tempRange;
            slot.BadgeColor = badgeColor;
            slot.AttachmentsText = attachments;
            slot.BackpackCapacityText = capacity;

            OnPropertyChanged(nameof(PilotLoadoutSlots));
            OnPropertyChanged(nameof(AverageArmorReductionText));
            OnPropertyChanged(nameof(CombinedTempResistText));
            OnPropertyChanged(nameof(TotalBackpackCapacityText));
            OnPropertyChanged(nameof(EquippedWeaponsSummaryText));
        }
    }

    [RelayCommand]
    public async Task CopyLoadoutToClipboard()
    {
        var sb = new StringBuilder();
        sb.AppendLine("📋 **SCLogMate — Piloten Loadout**");
        sb.AppendLine($"🛡 Rüstungsschutz: {AverageArmorReductionText} | 🌡 Temperatur: {CombinedTempResistText} | 🎒 {TotalBackpackCapacityText}");
        sb.AppendLine("---");
        foreach (var slot in PilotLoadoutSlots)
        {
            if (slot.IsEquipped)
            {
                sb.Append($"{slot.Icon} **{slot.SlotName}**: {slot.ItemName}");
                if (!string.IsNullOrEmpty(slot.ArmorClass)) sb.Append($" [{slot.ArmorClass}]");
                if (!string.IsNullOrEmpty(slot.AttachmentsText)) sb.Append($" ({slot.AttachmentsText})");
                sb.AppendLine();
            }
        }
        var text = sb.ToString();
        var clipboard = UiServices.TopLevel?.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(text);
            Status = "✓ Aktuelles Piloten-Loadout in die Zwischenablage kopiert!";
        }
    }

    [RelayCommand]
    public async Task ExportLoadoutMarkdown()
    {
        var path = await PickSaveAsync("pilot-loadout.md", "Markdown", "md");
        if (path == null) return;

        var sb = new StringBuilder();
        sb.AppendLine("# 🥋 SCLogMate — Pilot Loadout Report");
        sb.AppendLine();
        sb.AppendLine($"*Erstellt am: {DateTime.Now:dd.MM.yyyy HH:mm:ss}*");
        sb.AppendLine();
        sb.AppendLine("## 📊 Status & Schutzwerte");
        sb.AppendLine($"- **Schadensreduktion:** {AverageArmorReductionText}");
        sb.AppendLine($"- **Umgebungsschutz:** {CombinedTempResistText}");
        sb.AppendLine($"- **Tragekapazität:** {TotalBackpackCapacityText}");
        sb.AppendLine($"- **Waffen:** {EquippedWeaponsSummaryText}");
        sb.AppendLine();
        sb.AppendLine("## 🪖 Ausgerüstete Slots");
        sb.AppendLine("| Slot | Gegenstand | Typ / Klasse | Aufsätze & Details | Letzte Sichtung |");
        sb.AppendLine("| :--- | :--- | :--- | :--- | :--- |");
        foreach (var slot in PilotLoadoutSlots)
        {
            var item = slot.IsEquipped ? slot.ItemName : "*Leer*";
            var cls = !string.IsNullOrEmpty(slot.ArmorClass) ? slot.ArmorClass : "—";
            var att = !string.IsNullOrEmpty(slot.AttachmentsText) ? slot.AttachmentsText : (!string.IsNullOrEmpty(slot.BackpackCapacityText) ? slot.BackpackCapacityText : "—");
            sb.AppendLine($"| {slot.Icon} {slot.SlotName} | {item} | {cls} | {att} | {slot.LastObservedText} |");
        }

        await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(false));
        Status = "✓ Loadout-Report gespeichert: " + path;
    }

    // ==========================================
    // Starmap Custom POIs (Persönliche Wegpunkte)
    // ==========================================
    public void LoadUserPois()
    {
        UserPois.Clear();
        try
        {
            var pois = Database.GetUserPois();
            foreach (var p in pois) UserPois.Add(p);
        }
        catch (Exception ex)
        {
            Logger.Error("LoadUserPois", ex);
        }
    }

    [RelayCommand]
    public void AddUserPoi()
    {
        if (string.IsNullOrWhiteSpace(NewPoiName)) return;
        var bodyName = !string.IsNullOrWhiteSpace(NewPoiBody) ? NewPoiBody : (SelectedStarmapObject?.Name ?? ResolvedLocation.DisplayName);
        if (string.IsNullOrWhiteSpace(bodyName) || bodyName == "—") bodyName = SelectedStarmapSystem;

        var poi = new UserPoi
        {
            System = SelectedStarmapSystem,
            Body = bodyName,
            Name = NewPoiName.Trim(),
            Notes = NewPoiNotes.Trim(),
            Category = NewPoiCategory,
            Color = NewPoiColor,
            CreatedAt = DateTime.UtcNow
        };
        Database.SaveUserPoi(poi);
        UserPois.Insert(0, poi);
        NewPoiName = "";
        NewPoiNotes = "";
        Status = $"✓ POI \"{poi.Name}\" auf {poi.Body} ({poi.System}) gespeichert";
        OnPropertyChanged(nameof(UserPois));
    }

    [RelayCommand]
    public void DeleteUserPoi(UserPoi? poi)
    {
        var target = poi ?? SelectedUserPoi;
        if (target == null) return;
        Database.DeleteUserPoi(target.Id);
        UserPois.Remove(target);
        Status = $"POI \"{target.Name}\" gelöscht";
        OnPropertyChanged(nameof(UserPois));
    }

    // ==========================================
    // Wipe- & Persistenz-Filter
    // ==========================================
    public DateTime? GetEffectiveWipeDate()
    {
        if (!WipeFilterEnabled) return null;
        if (DateTime.TryParse(WipeDateString, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt))
        {
            return dt;
        }
        return null;
    }

    partial void OnWipeFilterEnabledChanged(bool value)
    {
        if (_initializing || !_ready) return;
        _settings.WipeFilterEnabled = value;
        Settings.Save(_settings);
        ApplyWipeFilter();
    }

    partial void OnWipeDateStringChanged(string value)
    {
        if (_initializing || !_ready) return;
        _settings.WipeDateString = value;
        Settings.Save(_settings);
        if (WipeFilterEnabled) ApplyWipeFilter();
    }

    partial void OnWipeFilterMoneyChanged(bool value)
    {
        if (_initializing || !_ready) return;
        _settings.WipeFilterMoney = value;
        Settings.Save(_settings);
        if (WipeFilterEnabled) ApplyWipeFilter();
    }

    partial void OnWipeFilterContractsChanged(bool value)
    {
        if (_initializing || !_ready) return;
        _settings.WipeFilterContracts = value;
        Settings.Save(_settings);
        if (WipeFilterEnabled) ApplyWipeFilter();
    }

    partial void OnWipeFilterFleetChanged(bool value)
    {
        if (_initializing || !_ready) return;
        _settings.WipeFilterFleet = value;
        Settings.Save(_settings);
        if (WipeFilterEnabled) ApplyWipeFilter();
    }

    partial void OnWipeFilterBlueprintsChanged(bool value)
    {
        if (_initializing || !_ready) return;
        _settings.WipeFilterBlueprints = value;
        Settings.Save(_settings);
        if (WipeFilterEnabled) ApplyWipeFilter();
    }

    [RelayCommand]
    public void SetWipePreset48()
    {
        WipeDateString = "2026-05-15";
        WipeFilterEnabled = true;
        Status = "Wipe-Filter auf Alpha 4.8 (15. Mai 2026) gesetzt.";
    }

    [RelayCommand]
    public void SetWipePresetToday()
    {
        WipeDateString = DateTime.UtcNow.ToString("yyyy-MM-dd");
        WipeFilterEnabled = true;
        Status = "Wipe-Filter auf heute gesetzt.";
    }

    [RelayCommand]
    public void ClearWipeFilter()
    {
        WipeFilterEnabled = false;
        Status = "Wipe-Filter deaktiviert (alle historischen Daten aktiv).";
    }

    public void ApplyWipeFilter()
    {
        if (SelectedSession?.IsAll == true)
        {
            LoadAllSessions();
        }
        else
        {
            RecomputeBalances();
        }
        OnPropertyChanged(nameof(SessionSpanText));
    }

    #region Flight Recorder & Session Timeline

    partial void OnTimelineSearchTextChanged(string value) => TimelineView?.Refresh();
    partial void OnSelectedTimelineFilterChanged(string value) => TimelineView?.Refresh();

    partial void OnSelectedTimelineItemChanged(FlightTimelineItem? value)
    {
        if (value != null && !string.IsNullOrWhiteSpace(value.LocationName) && value.LocationName != "—")
        {
            if (!string.IsNullOrEmpty(value.SystemName) && StarmapData.SystemNames.Contains(value.SystemName))
            {
                FlightRecorderSelectedSystem = value.SystemName;
            }
            var obj = StarmapData.FindObject(value.LocationName);
            if (obj != null)
            {
                FlightRecorderSelectedObject = obj;
            }
        }
    }

    [RelayCommand]
    public void SelectTimelineFilter(string filter)
    {
        SelectedTimelineFilter = filter;
        TimelineView?.Refresh();
    }

    [RelayCommand]
    public void RebuildTimeline()
    {
        RebuildIndependentTimeline();
    }

    [RelayCommand]
    public void JumpToTimelineLocation(FlightTimelineItem? item)
    {
        var target = item ?? SelectedTimelineItem;
        if (target == null || string.IsNullOrWhiteSpace(target.LocationName) || target.LocationName == "—") return;
        var sys = target.SystemName;
        if (!string.IsNullOrEmpty(sys) && StarmapData.SystemNames.Contains(sys))
        {
            SelectedStarmapSystem = sys;
        }
        var obj = StarmapData.FindObject(target.LocationName);
        if (obj != null)
        {
            SelectedStarmapObject = obj;
        }
        SelectedTabIndex = 4; // Tab '🗺 Karte'
        Status = $"🗺 Starmap fokussiert: {target.LocationName} ({target.SystemName})";
    }

    [RelayCommand]
    public async Task ExportFlightLog()
    {
        var defaultFileName = $"Flugbericht_{DateTime.Now:yyyy-MM-dd_HHmm}.md";
        var path = await PickSaveAsync(defaultFileName, "Markdown Dokument", "md");
        if (path == null) return;

        var label = SelectedSession?.Label ?? "Aktuelle Session";
        var md = FlightRecorderService.ExportToMarkdown(label, SessionTimeline.ToList(), FlightSummary);
        await File.WriteAllTextAsync(path, md, new UTF8Encoding(false));
        Status = "✓ Flugbericht gespeichert: " + path;
    }

    #endregion

    #region RS Signal Scanner & Mining/Salvage Decoder

    public void OnRsDetected(int rs)
    {
        CurrentRsValue = rs;
        RsInputText = rs.ToString();
        var matches = RsDecoderCatalog.Decode(rs);
        CurrentRsMatches.Clear();
        foreach (var m in matches) CurrentRsMatches.Add(m);
        BestRsMatch = matches.FirstOrDefault();

        OnPropertyChanged(nameof(CurrentRsDisplayValue));
        OnPropertyChanged(nameof(SecondaryRsMatchesText));
        Status = BestRsMatch != null
            ? $"🛰 RS Signal {rs:N0} erkannt: {BestRsMatch.DisplayTitle} ({BestRsMatch.Subtitle})"
            : $"🛰 RS Signal {rs:N0} erkannt (Keine bekannte Signatur)";
    }

    [RelayCommand]
    public void DecodeRs(string? input = null)
    {
        var raw = input ?? RsInputText;
        if (string.IsNullOrWhiteSpace(raw)) return;

        var val = RsOcrScanner.ExtractRsValue(raw);
        if (val.HasValue)
        {
            OnRsDetected(val.Value);
        }
        else if (int.TryParse(raw.Replace(".", "").Replace(",", "").Trim(), out int num) && num >= 1000)
        {
            OnRsDetected(num);
        }
        else
        {
            Status = "Ungültiger RS-Wert (z.B. 7200, 14400, 3170 eingeben)";
        }
    }

    [RelayCommand]
    public void SetRsPreset(int value)
    {
        OnRsDetected(value);
    }

    [RelayCommand]
    public void ToggleRsOverlay()
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                if (_rsOverlayWindow == null)
                {
                    _rsOverlayWindow = new Views.RsScanOverlayWindow { DataContext = this };
                    _rsOverlayWindow.InitSettings(_settings);
                }

                if (!_rsOverlayWindow.IsVisible)
                {
                    _rsOverlayWindow.Show();
                    IsRsOverlayActive = true;
                    _settings.RsOverlayEnabled = true;
                    Settings.Save(_settings);
                    Status = "🛰 RS Signal Scanner Overlay eingeblendet";
                }
                else
                {
                    _rsOverlayWindow.Hide();
                    IsRsOverlayActive = false;
                    _settings.RsOverlayEnabled = false;
                    Settings.Save(_settings);
                    Status = "RS Signal Scanner Overlay ausgeblendet";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ToggleRsOverlay", ex);
                Status = "Fehler beim Öffnen des RS-Overlays: " + ex.Message;
                _rsOverlayWindow = null;
                IsRsOverlayActive = false;
            }
        });
    }

    [RelayCommand]
    public void ToggleRsAutoScan()
    {
        IsRsAutoScanEnabled = !IsRsAutoScanEnabled;
        _settings.RsAutoScanEnabled = IsRsAutoScanEnabled;
        Settings.Save(_settings);

        if (IsRsAutoScanEnabled)
        {
            _rsScanner.Start();
        }
        else
        {
            _rsScanner.Stop();
        }
        OnPropertyChanged(nameof(RsAutoScanStatusText));
        OnPropertyChanged(nameof(RsAutoScanBadgeBg));
        OnPropertyChanged(nameof(RsAutoScanBadgeBorder));
        OnPropertyChanged(nameof(RsAutoScanBadgeFg));
    }

    [RelayCommand]
    public async Task TriggerRsOcrScan()
    {
        if (!_ocrEngine.IsAvailable)
        {
            Status = "Windows OCR ist auf diesem System nicht verfügbar";
            return;
        }

        Status = "Scanne RS-Signal vom Bildschirm…";
        var val = await _rsScanner.ScanOnceAsync();
        if (val.HasValue)
        {
            OnRsDetected(val.Value);
            RsOcrStatusText = $"✓ Erkannt: {val.Value:N0} RS ({DateTime.Now:HH:mm:ss})";
        }
        else
        {
            Status = "Kein RS-Signal auf dem Bildschirm erkannt";
            RsOcrStatusText = "Kein Signal gefunden";
        }
    }

    [RelayCommand]
    public void CalibrateRsScanRegion()
    {
        var win = new Views.RegionSelectorWindow();
        win.RegionSelected += r =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _settings.RsScanRegion = r;
                Settings.Save(_settings);
                Status = $"RS Scan-Bereich kalibriert: {r.Width}x{r.Height} @ ({r.X},{r.Y})";
            });
        };
        win.Show();
    }

    [RelayCommand]
    public void ResetRsScanRegion()
    {
        _settings.RsScanRegion = null;
        Settings.Save(_settings);
        Status = "RS Scan-Bereich auf Standard (Mitte) zurückgesetzt";
    }

    #endregion

    #region Entwickler & Debug Tools

    [RelayCommand]
    public void SimulateArmisticeEnter()
    {
        if (!IsDebugModeActive) return;
        Status = "🧪 [DEBUG] Simuliere: Schutzzone betreten...";
        _auroraService.ProcessLiveLine(@"<2026-09-02T12:00:00.000Z> [Notice] <SHUDEvent_OnNotification> Added notification ""Schutzzone - Kampfhandlung untersagt!: "" [9001]");
    }

    [RelayCommand]
    public void SimulateArmisticeLeave()
    {
        if (!IsDebugModeActive) return;
        Status = "🧪 [DEBUG] Simuliere: Schutzzone verlassen...";
        _auroraService.ProcessLiveLine(@"<2026-09-02T12:00:00.000Z> [Notice] <SHUDEvent_OnNotification> Added notification ""Schutzzone verlassen: "" [9002]");
    }

    [RelayCommand]
    public void SimulateShipJoin(string? shipName)
    {
        if (!IsDebugModeActive) return;
        var name = string.IsNullOrWhiteSpace(shipName) ? "Drake Cutlass Black" : shipName;
        Status = $"🧪 [DEBUG] Simuliere: Schiffskanal beigetreten ({name})...";
        _auroraService.ProcessLiveLine($@"<2026-09-02T12:00:00.000Z> [Notice] <SHUDEvent_OnNotification> Added notification ""Du bist Kanal [ {name} ] beigetreten""");
    }

    [RelayCommand]
    public void SimulateBlueprintFound()
    {
        if (!IsDebugModeActive) return;
        Status = "🧪 [DEBUG] Simuliere: Bauplan erlernt...";
        _auroraService.OnBlueprintLearned("Pyro RYT Multi-Tool");
        TriggerAchievementToast(AchievementToastData.ForBlueprint("Pyro RYT Multi-Tool (Experimental)"));
    }

    [RelayCommand]
    public void SimulateQuantumArrival()
    {
        if (!IsDebugModeActive) return;
        Status = "🧪 [DEBUG] Simuliere: Quantensprung Ankunft...";
        _auroraService.OnQuantumArrival();
    }

    [RelayCommand]
    public void SimulateServerError()
    {
        if (!IsDebugModeActive) return;
        Status = "🧪 [DEBUG] Simuliere: 30k Serverfehler...";
        _auroraService.OnServerError();
    }

    [RelayCommand]
    public void SimulatePlayerDeath()
    {
        if (!IsDebugModeActive) return;
        Status = "🧪 [DEBUG] Simuliere: Notfall / Spielertod...";
        _auroraService.OnPlayerDeath();
    }

    [RelayCommand]
    public void DumpStateToLog()
    {
        if (!IsDebugModeActive) return;
        Logger.Log("══════════════════════════════════════════════════════════");
        Logger.Log($"[DEBUG DUMP] Zeitstempel: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Logger.Log($"[DEBUG DUMP] Debug-Modus aktiv: {IsDebugModeActive}");
        Logger.Log($"[DEBUG DUMP] Gewählte Sprache: {SelectedLanguageOption?.Title} (Code: {SelectedLanguageOption?.Code}, Deutsch aktiv: {IsGermanLanguageActive})");
        Logger.Log($"[DEBUG DUMP] LogPath: {LogPath} (Existiert: {File.Exists(LogPath)})");
        Logger.Log($"[DEBUG DUMP] Standort: '{CurrentLocation}' -> Resolved: '{ResolvedLocation.DisplayName}' (Typ: {ResolvedLocation.Type}, IsArmistice: {ResolvedLocation.IsArmistice})");
        Logger.Log($"[DEBUG DUMP] Aktuelles Schiff: '{CurrentShip}'");
        Logger.Log($"[DEBUG DUMP] Kontostand: {LiveBalanceText} (Income: {SessionIncomeText}, Spend: {SessionSpendText}, Net: {SessionNetText})");
        Logger.Log($"[DEBUG DUMP] Aurora Voice: Installiert={IsAuroraInstalled} (Real={_auroraService.IsInstalled}), Aktiv={IsAuroraEnabled}, Vol={AuroraVolume}%, IsAtStation={_auroraService.IsAtStation}");
        Logger.Log($"[DEBUG DUMP] Windows OCR: Verfügbar={OcrAvailable}, AutoSync={AutoOcrEnabled}, Region={OcrRegionText}");
        Logger.Log($"[DEBUG DUMP] Aktive Aufträge: {ActiveContracts.Count}");
        Logger.Log("══════════════════════════════════════════════════════════");
        Status = "✓ [DEBUG] Detaillierter Systemstatus in SCLogMate.debug.log geschrieben.";
    }

    [RelayCommand]
    public void ClearDebugLog()
    {
        if (!IsDebugModeActive) return;
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SCLogMate", "SCLogMate.debug.log");
            if (File.Exists(path))
            {
                File.WriteAllText(path, $"[{DateTime.Now:HH:mm:ss}] [DEBUG] Logdatei zurückgesetzt.\n");
                Status = "✓ [DEBUG] Debug-Logdatei geleert.";
            }
        }
        catch (Exception ex)
        {
            Logger.Error("ClearDebugLog", ex);
            Status = $"Fehler beim Leeren des Debug-Logs: {ex.Message}";
        }
    }

    [RelayCommand]
    public void ResetOverlayPositions()
    {
        if (!IsDebugModeActive) return;
        _settings.OverlayPositionX = 50;
        _settings.OverlayPositionY = 50;
        _settings.RsOverlayPositionX = 400;
        _settings.RsOverlayPositionY = 50;
        Settings.Save(_settings);
        Status = "✓ [DEBUG] Overlay-Positionen auf Standard (50,50 / 400,50) zurückgesetzt.";
    }

    #endregion
}
