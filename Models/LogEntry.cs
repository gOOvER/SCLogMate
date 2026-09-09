using System;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SCLogMate.Models;

public enum EventKind
{
    TransferIn,
    TransferOut,
    MissionReward,
    Purchase,
    Sale,
    Trade,
    Maintenance,
    Location,
    Inventory,
    Vehicle,
    Quantum,
    Mission,
    Jurisdiction,
    Party,
    MedBed,
    Hangar,
    Loadout,
    Offer,
    ShipLoss,
    Death,
    Impound,
    Friend,
    Entitlement,
    Blueprint,
    Gear,
    Kill,
    MissionDone,
    Fine,
    Crime,
    Refinery,
    Injury,
    Loot,
    MissionTaken,
    Crash,
    SessionChange,
    Info
}

public partial class LogEntry : ObservableObject
{
    public DateTime Time { get; init; }
    public EventKind Kind { get; init; }

    /// <summary>aUEC, signed: positive = rein, negative = raus, 0 = kein Geldwert.</summary>
    public long Amount { get; set; }

    /// <summary>itemClassGUID (nur bei Käufen) für die Namensauflösung über UEX.</summary>
    public string? ItemRef { get; init; }

    /// <summary>Zusatz hinter dem Namen (z.B. "×1 · Cargo Office"), bleibt bei Namensupdate erhalten.</summary>
    public string? Suffix { get; init; }

    /// <summary>Schiffsname (bei Vehicle/Quantum) für die Flotten-Liste.</summary>
    public string? Ship { get; init; }

    /// <summary>Erkanntes Reiseziel bei einer Quantum-Ankunft.</summary>
    public string? Location { get; init; }

    /// <summary>Anzeigetext – wird bei Käufen asynchron mit dem echten Item-Namen ersetzt.</summary>
    [ObservableProperty] private string detail = "";

    public string TimeText => Time.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

    public string KindText => Kind switch
    {
        EventKind.TransferIn => "Eingang",
        EventKind.TransferOut => "Ausgang",
        EventKind.MissionReward => "Belohnung",
        EventKind.Purchase => "Kauf",
        EventKind.Sale => "Verkauf",
        EventKind.Trade => "Handel",
        EventKind.Maintenance => "Wartung",
        EventKind.Location => "Standort",
        EventKind.Inventory => "Lager",
        EventKind.Vehicle => "Schiff",
        EventKind.Quantum => "Quantum",
        EventKind.Mission => "Mission",
        EventKind.Jurisdiction => "Gebiet",
        EventKind.Party => "Party",
        EventKind.MedBed => "Med-Bett",
        EventKind.Hangar => "Hangar",
        EventKind.Loadout => "Ausrüstung",
        EventKind.Offer => "Angebot",
        EventKind.ShipLoss => "Verlust",
        EventKind.Death => "Tod",
        EventKind.Impound => "Beschlagn.",
        EventKind.Friend => "Freund",
        EventKind.Entitlement => "Miete",
        EventKind.Blueprint => "Bauplan",
        EventKind.Gear => "Defekt",
        EventKind.Kill => "Kampf",
        EventKind.MissionDone => "Auftrag ✓",
        EventKind.Fine => "Strafe",
        EventKind.Crime => "Straftat",
        EventKind.Refinery => "Veredelung",
        EventKind.Injury => "Verletzung",
        EventKind.Loot => "Loot",
        EventKind.MissionTaken => "Auftraggeber",
        EventKind.Crash => "Crash",
        EventKind.SessionChange => "Session",
        _ => "Info"
    };

    public string AmountText => Amount != 0 ? $"{Amount:N0}" : "";

    // Farbe der DETAIL-Zelle: Missions-Status farbig (grün=fertig, rot=fehlgeschlagen,
    // blau=angenommen/neu, gelb=zurückgezogen), sonst Standard.
    static readonly IBrush _detailDefault = new SolidColorBrush(Color.Parse("#E6EDF3"));
    static readonly IBrush _missGreen = new SolidColorBrush(Color.Parse("#3FB950"));
    static readonly IBrush _missRed = new SolidColorBrush(Color.Parse("#F85149"));
    static readonly IBrush _missBlue = new SolidColorBrush(Color.Parse("#58A6FF"));
    static readonly IBrush _missAmber = new SolidColorBrush(Color.Parse("#D29922"));

    public IBrush StatusBrush
    {
        get
        {
            if (Kind == EventKind.Crash) return _missRed;
            if (Kind == EventKind.SessionChange) return _missAmber;
            if (Kind == EventKind.MissionDone) return _missGreen;
            if (Kind != EventKind.Mission) return _detailDefault;
            var d = Detail ?? "";
            if (d.StartsWith("Auftrag abgeschlossen", StringComparison.OrdinalIgnoreCase) || d.StartsWith("Contract Complete", StringComparison.OrdinalIgnoreCase)) return _missGreen;
            if (d.StartsWith("Auftrag fehlgeschlagen", StringComparison.OrdinalIgnoreCase) || d.StartsWith("Contract Failed", StringComparison.OrdinalIgnoreCase) ||
                d.StartsWith("Auftrag abgebrochen", StringComparison.OrdinalIgnoreCase) || d.StartsWith("Contract Abandoned", StringComparison.OrdinalIgnoreCase) ||
                d.StartsWith("Auftrag aufgegeben", StringComparison.OrdinalIgnoreCase) || d.StartsWith("Contract Cancelled", StringComparison.OrdinalIgnoreCase)) return _missRed;
            if (d.StartsWith("Auftrag zurückgezogen", StringComparison.OrdinalIgnoreCase) || d.StartsWith("Contract Withdrawn", StringComparison.OrdinalIgnoreCase)) return _missAmber;
            return _missBlue;   // angenommen / Neuer Auftrag / New Objective
        }
    }

    // Mitlaufender Kontostand nach diesem Ereignis (nur Geld-Events)
    [ObservableProperty] private long balanceAfter;
    [ObservableProperty] private bool hasBalance;
    public string BalanceAfterText => HasBalance ? $"{BalanceAfter:N0}" : "";
    partial void OnBalanceAfterChanged(long value) => OnPropertyChanged(nameof(BalanceAfterText));
    partial void OnHasBalanceChanged(bool value) => OnPropertyChanged(nameof(BalanceAfterText));

    public string Icon => Kind switch
    {
        EventKind.TransferIn => "▼",
        EventKind.TransferOut => "▲",
        EventKind.MissionReward => "★",
        EventKind.Purchase => "↧",
        EventKind.Sale => "↥",
        EventKind.Trade => "⇄",
        EventKind.Maintenance => "🔧",
        EventKind.Location => "◉",
        EventKind.Inventory => "▣",
        EventKind.Vehicle => "✈",
        EventKind.Quantum => "✦",
        EventKind.Mission => "✓",
        EventKind.Jurisdiction => "⬢",
        EventKind.Party => "♟",
        EventKind.MedBed => "✚",
        EventKind.Hangar => "⌂",
        EventKind.Loadout => "⛨",
        EventKind.Offer => "◇",
        EventKind.ShipLoss => "✸",
        EventKind.Death => "☠",
        EventKind.Impound => "⊠",
        EventKind.Friend => "♥",
        EventKind.Entitlement => "⧉",
        EventKind.Blueprint => "⬡",
        EventKind.Gear => "✖",
        EventKind.Kill => "⚔",
        EventKind.MissionDone => "✔",
        EventKind.Fine => "⚖",
        EventKind.Crime => "⚠",
        EventKind.Refinery => "⚗",
        EventKind.Injury => "⚕",
        EventKind.Loot => "◈",
        EventKind.MissionTaken => "❖",
        EventKind.Crash => "💥",
        EventKind.SessionChange => "⚡",
        _ => "·"
    };

    // Gecachte Brushes für KindBadges (verhindert zehntausende Brush-Allokationen beim DataGrid-Scrolling)
    private static readonly IBrush _bgMissionReward = new SolidColorBrush(Color.Parse("#332608"));
    private static readonly IBrush _bgBlueprint = new SolidColorBrush(Color.Parse("#092918"));
    private static readonly IBrush _bgSaleTrade = new SolidColorBrush(Color.Parse("#0B2B1B"));
    private static readonly IBrush _bgExpense = new SolidColorBrush(Color.Parse("#381317"));
    private static readonly IBrush _bgFlight = new SolidColorBrush(Color.Parse("#0B233F"));
    private static readonly IBrush _bgLocation = new SolidColorBrush(Color.Parse("#26143D"));
    private static readonly IBrush _bgCombatLoss = new SolidColorBrush(Color.Parse("#3D1016"));
    private static readonly IBrush _bgInventory = new SolidColorBrush(Color.Parse("#0F2836"));
    private static readonly IBrush _bgMission = new SolidColorBrush(Color.Parse("#1A202C"));
    private static readonly IBrush _bgDefault = new SolidColorBrush(Color.Parse("#161B22"));

    private static readonly IBrush _fgMissionReward = new SolidColorBrush(Color.Parse("#FBBF24"));
    private static readonly IBrush _fgBlueprint = new SolidColorBrush(Color.Parse("#34D399"));
    private static readonly IBrush _fgSaleTrade = new SolidColorBrush(Color.Parse("#4ADE80"));
    private static readonly IBrush _fgExpense = new SolidColorBrush(Color.Parse("#FB923C"));
    private static readonly IBrush _fgFlight = new SolidColorBrush(Color.Parse("#38BDF8"));
    private static readonly IBrush _fgLocation = new SolidColorBrush(Color.Parse("#C084FC"));
    private static readonly IBrush _fgCombatLoss = new SolidColorBrush(Color.Parse("#FB7185"));
    private static readonly IBrush _fgInventory = new SolidColorBrush(Color.Parse("#67E8F9"));
    private static readonly IBrush _fgMission = new SolidColorBrush(Color.Parse("#E2E8F0"));
    private static readonly IBrush _fgDefault = new SolidColorBrush(Color.Parse("#8B949E"));

    public IBrush KindBadgeBg => Kind switch
    {
        EventKind.MissionReward => _bgMissionReward,
        EventKind.Blueprint => _bgBlueprint,
        EventKind.Sale or EventKind.Trade => _bgSaleTrade,
        EventKind.Purchase or EventKind.Fine or EventKind.TransferOut or EventKind.Maintenance => _bgExpense,
        EventKind.Vehicle or EventKind.Quantum => _bgFlight,
        EventKind.Location or EventKind.Jurisdiction => _bgLocation,
        EventKind.Kill or EventKind.Death or EventKind.ShipLoss or EventKind.Crash => _bgCombatLoss,
        EventKind.Loot or EventKind.Loadout => _bgInventory,
        EventKind.Mission or EventKind.MissionTaken or EventKind.MissionDone => _bgMission,
        _ => _bgDefault
    };

    public IBrush KindBadgeFg => Kind switch
    {
        EventKind.MissionReward => _fgMissionReward,
        EventKind.Blueprint => _fgBlueprint,
        EventKind.Sale or EventKind.Trade => _fgSaleTrade,
        EventKind.Purchase or EventKind.Fine or EventKind.TransferOut or EventKind.Maintenance => _fgExpense,
        EventKind.Vehicle or EventKind.Quantum => _fgFlight,
        EventKind.Location or EventKind.Jurisdiction => _fgLocation,
        EventKind.Kill or EventKind.Death or EventKind.ShipLoss or EventKind.Crash => _fgCombatLoss,
        EventKind.Loot or EventKind.Loadout => _fgInventory,
        EventKind.Mission or EventKind.MissionTaken or EventKind.MissionDone => _fgMission,
        _ => _fgDefault
    };
}
