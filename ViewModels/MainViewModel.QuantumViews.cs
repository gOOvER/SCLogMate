using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCLogMate.Core;
using SCLogMate.Models;

namespace SCLogMate.ViewModels;

public partial class MainViewModel
{
    // ══ SUB-TAB INDIZES ══
    [ObservableProperty] private int financeSubTabIndex = 0; // 0=Übersicht, 1=Ledger, 2=Ausgaben, 3=Fracht
    [ObservableProperty] private int missionSubTabIndex = 0; // 0=Aufträge, 1=Katalog

    // ══ CONTRACTS (AUFTRÄGE) ══
    public ObservableCollection<ContractRecord> ContractRecords { get; } = new();
    public ObservableCollection<FacetItem> ContractIssuers { get; } = new();
    public ObservableCollection<FacetItem> ContractTypes { get; } = new();

    [ObservableProperty] private string selectedContractIssuer = "Alle";
    [ObservableProperty] private string selectedContractType = "Alle";
    [ObservableProperty] private string selectedContractOutcome = "Alle"; // "Alle", "Abgeschlossen", "Abgebrochen", "Aktiv"
    public List<string> ContractOutcomes { get; } = new() { "Alle", "Abgeschlossen", "Abgebrochen", "Aktiv" };
    [ObservableProperty] private string contractSearchText = "";

    [ObservableProperty] private int contractsTotalCount;
    [ObservableProperty] private int contractsCompletedCount;
    [ObservableProperty] private int contractsAbandonedCount;
    [ObservableProperty] private string contractsCompletionRateText = "0 %";

    // ══ PLACES (ORTE) ══
    public ObservableCollection<LocationVisitTotal> TopLocations { get; } = new();
    public ObservableCollection<QuantumDestinationTotal> TopQuantumDestinations { get; } = new();

    [ObservableProperty] private string placesSearchText = "";
    [ObservableProperty] private string placesPeriod = "Alle"; // "Alle", "30 Tage", "7 Tage", "24 Stunden", "Session"
    [ObservableProperty] private int totalLocationsVisitedCount;
    [ObservableProperty] private int totalQuantumJumpsCount;

    // ══ SPENDING (AUSGABEN) ══
    public ObservableCollection<ConfirmedPurchaseRecord> ConfirmedPurchasesView { get; } = new();
    public ObservableCollection<StatItem> SpendByShop { get; } = new();
    public ObservableCollection<StatItem> SpendByItem { get; } = new();
    public ObservableCollection<StatItem> SpendByCategory { get; } = new();

    [ObservableProperty] private string spendingTotalAuecText = "0 aUEC";
    [ObservableProperty] private string spendingCountText = "0 Käufe";
    [ObservableProperty] private string spendingAverageText = "—";
    [ObservableProperty] private string spendingSearchText = "";

    // ══ LEDGER (BUCHUNGSJOURNAL) ══
    public ObservableCollection<LedgerRecord> LedgerView { get; } = new();

    [ObservableProperty] private string ledgerFilter = "Alle"; // "Alle", "Frachtverkauf", "Frachtkauf", "Item gekauft", "Überweisung", "Sonstige"
    [ObservableProperty] private string ledgerSearchText = "";
    [ObservableProperty] private string ledgerTotalIncomeText = "0 aUEC";
    [ObservableProperty] private string ledgerTotalSpendText = "0 aUEC";
    [ObservableProperty] private string ledgerNetBalanceText = "0 aUEC";

    // ══ CARGO (FRACHTHANDEL) ══
    public ObservableCollection<CargoTradeRecord> CargoTradesView { get; } = new();

    [ObservableProperty] private string cargoActionFilter = "Alle"; // "Alle", "Verkauf", "Einkauf"
    [ObservableProperty] private string cargoSearchText = "";
    [ObservableProperty] private string cargoTotalIncomeText = "0 aUEC";
    [ObservableProperty] private string cargoTotalSpendText = "0 aUEC";
    [ObservableProperty] private string cargoNetProfitText = "0 aUEC";
    [ObservableProperty] private string cargoTotalScuText = "0 SCU";
    [ObservableProperty] private string cargoTradesCountText = "0 Trades";

    // ══ MARKET & COMMODITY ══
    public ObservableCollection<MarketCommodityEntry> MarketEntriesView { get; } = new();

    [ObservableProperty] private string marketSearchText = "";
    [ObservableProperty] private string marketCategoryFilter = "Alle";
    [ObservableProperty] private bool isCommodityDetailOpen = false;
    [ObservableProperty] private MarketCommodityEntry? selectedMarketCommodity;

    public ObservableCollection<CommodityTerminalRow> SelectedCommodityTerminals { get; } = new();
    public ObservableCollection<CargoTradeRecord> SelectedCommodityReceipts { get; } = new();

    [ObservableProperty] private string selectedCommodityBestSellText = "—";
    [ObservableProperty] private string selectedCommodityBestBuyText = "—";
    [ObservableProperty] private string selectedCommodityMarginText = "—";
    [ObservableProperty] private string selectedCommoditySoldText = "—";
    [ObservableProperty] private string selectedCommodityRevenueText = "—";
    [ObservableProperty] private string uexMarketLastUpdatedText = "Nicht geladen";
    [ObservableProperty] private bool isFetchingUexMarket;

    // ══ INTERNE ROHDATEN-LISTEN ══
    private List<ContractRecord> _rawContracts = new();
    private List<ConfirmedPurchaseRecord> _rawPurchases = new();
    private List<CargoTradeRecord> _rawCargoTrades = new();
    private List<LedgerRecord> _rawLedgerRecords = new();
    private List<(DateTime Time, string RawId, string Name, string? System, string? Body, string Kind)> _rawLocations = new();
    private List<(DateTime Time, string Destination)> _rawQuantumDestinations = new();
    private List<MarketCommodityEntry> _rawMarketCatalog = new();

    private DispatcherTimer? _quantumSyncTimer;

    public void RequestQuantumViewsSync()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(RequestQuantumViewsSync);
            return;
        }

        if (_quantumSyncTimer == null)
        {
            _quantumSyncTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _quantumSyncTimer.Tick += (s, e) =>
            {
                _quantumSyncTimer.Stop();
                SyncQuantumViewsFromParser();
            };
        }
        _quantumSyncTimer.Stop();
        _quantumSyncTimer.Start();
    }

    public void ResetQuantumViews()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(ResetQuantumViews);
            return;
        }

        _rawContracts.Clear();
        _rawPurchases.Clear();
        _rawCargoTrades.Clear();
        _rawLedgerRecords.Clear();
        _rawLocations.Clear();
        _rawQuantumDestinations.Clear();

        ContractRecords.Clear();
        ContractIssuers.Clear();
        ContractTypes.Clear();
        TopLocations.Clear();
        TopQuantumDestinations.Clear();
        ConfirmedPurchasesView.Clear();
        SpendByShop.Clear();
        SpendByItem.Clear();
        SpendByCategory.Clear();
        LedgerView.Clear();
        CargoTradesView.Clear();
        MarketEntriesView.Clear();
        SelectedCommodityTerminals.Clear();
        SelectedCommodityReceipts.Clear();

        IsCommodityDetailOpen = false;
        SelectedMarketCommodity = null;

        ContractsTotalCount = 0;
        ContractsCompletedCount = 0;
        ContractsAbandonedCount = 0;
        ContractsCompletionRateText = "0 %";

        TotalLocationsVisitedCount = 0;
        TotalQuantumJumpsCount = 0;

        SpendingTotalAuecText = "0 aUEC";
        SpendingCountText = "0 Käufe";
        SpendingAverageText = "—";

        LedgerTotalIncomeText = "0 aUEC";
        LedgerTotalSpendText = "0 aUEC";
        LedgerNetBalanceText = "0 aUEC";

        CargoTotalIncomeText = "0 aUEC";
        CargoTotalSpendText = "0 aUEC";
        CargoNetProfitText = "0 aUEC";
        CargoTotalScuText = "0 SCU";
        CargoTradesCountText = "0 Trades";
    }

    public void SyncQuantumViewsFromParser()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(SyncQuantumViewsFromParser);
            return;
        }

        if (_parser == null) return;

        // 1. Rohdaten aus dem Parser übernehmen
        _rawContracts = _parser.ContractsList.OrderByDescending(c => c.AcceptedAt).ToList();
        _rawPurchases = _parser.ConfirmedPurchases.OrderByDescending(p => p.Timestamp).ToList();
        _rawCargoTrades = _parser.CargoTrades.OrderByDescending(t => t.Timestamp).ToList();
        _rawLedgerRecords = _parser.LedgerRecords.OrderBy(l => l.Timestamp).ToList(); // chronologisch für Saldo

        if (_allDbTimelineEvents != null && _allDbTimelineEvents.Count > 0)
        {
            RebuildPlacesFromDatabase(_allDbTimelineEvents);
        }
        else
        {
            _rawLocations = _parser.LocationVisits.ToList();
            _rawQuantumDestinations = _parser.QuantumDestinations.ToList();
            UpdatePlacesView();
        }

        // 2. Ansichten aktualisieren
        UpdateContractsView();
        UpdateSpendingView();
        UpdateLedgerView();
        UpdateCargoView();
        BuildMarketCatalog();
    }

    // ══ CONTRACTS METHODEN ══

    private void UpdateContractsView()
    {
        ContractsTotalCount = _rawContracts.Count;
        ContractsCompletedCount = _rawContracts.Count(c => c.Outcome == ContractOutcome.Completed);
        ContractsAbandonedCount = _rawContracts.Count(c => c.Outcome == ContractOutcome.Abandoned);

        double rate = ContractsTotalCount > 0 ? ((double)ContractsCompletedCount / ContractsTotalCount) * 100.0 : 0.0;
        ContractsCompletionRateText = $"{rate:F0} %";

        // Facetten aufbauen
        var issuers = _rawContracts
            .GroupBy(c => c.Issuer, StringComparer.OrdinalIgnoreCase)
            .Select(g => new FacetItem(g.Key, g.Count()))
            .OrderByDescending(f => f.Count)
            .ToList();
        ContractIssuers.Clear();
        ContractIssuers.Add(new FacetItem("Alle", ContractsTotalCount));
        foreach (var iss in issuers) ContractIssuers.Add(iss);

        var types = _rawContracts
            .GroupBy(c => c.Type, StringComparer.OrdinalIgnoreCase)
            .Select(g => new FacetItem(g.Key, g.Count()))
            .OrderByDescending(f => f.Count)
            .ToList();
        ContractTypes.Clear();
        ContractTypes.Add(new FacetItem("Alle", ContractsTotalCount));
        foreach (var ty in types) ContractTypes.Add(ty);

        FilterContracts();
    }

    private void FilterContracts()
    {
        var query = _rawContracts.AsEnumerable();

        if (!string.IsNullOrEmpty(SelectedContractIssuer) && SelectedContractIssuer != "Alle")
            query = query.Where(c => c.Issuer.Equals(SelectedContractIssuer, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(SelectedContractType) && SelectedContractType != "Alle")
            query = query.Where(c => c.Type.Equals(SelectedContractType, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(SelectedContractOutcome) && SelectedContractOutcome != "Alle")
        {
            query = SelectedContractOutcome switch
            {
                "Abgeschlossen" => query.Where(c => c.Outcome == ContractOutcome.Completed),
                "Abgebrochen" => query.Where(c => c.Outcome == ContractOutcome.Abandoned),
                "Aktiv" => query.Where(c => c.Outcome == ContractOutcome.InProgress),
                _ => query
            };
        }

        if (!string.IsNullOrWhiteSpace(ContractSearchText))
        {
            var search = ContractSearchText.Trim();
            query = query.Where(c =>
                c.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                c.Issuer.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                c.Type.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                c.System.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        ContractRecords.Clear();
        foreach (var c in query) ContractRecords.Add(c);
    }

    partial void OnSelectedContractIssuerChanged(string value) => FilterContracts();
    partial void OnSelectedContractTypeChanged(string value) => FilterContracts();
    partial void OnSelectedContractOutcomeChanged(string value) => FilterContracts();
    partial void OnContractSearchTextChanged(string value) => FilterContracts();

    [RelayCommand]
    public void SetContractIssuer(string issuer) => SelectedContractIssuer = issuer;

    [RelayCommand]
    public void SetContractType(string type) => SelectedContractType = type;

    [RelayCommand]
    public void SetContractOutcome(string outcome) => SelectedContractOutcome = outcome;

    // ══ PLACES METHODEN ══

    public void RebuildPlacesFromDatabase(IEnumerable<LogEntry>? timelineEvents = null)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => RebuildPlacesFromDatabase(timelineEvents));
            return;
        }

        var events = (timelineEvents ?? _allDbTimelineEvents)?.ToList();
        if (events == null || events.Count == 0)
        {
            if (_parser != null)
            {
                _rawLocations = _parser.LocationVisits.ToList();
                _rawQuantumDestinations = _parser.QuantumDestinations.ToList();
            }
            UpdatePlacesView();
            return;
        }

        var newLocs = new List<(DateTime Time, string RawId, string Name, string? System, string? Body, string Kind)>();
        var newQt = new List<(DateTime Time, string Destination)>();

        foreach (var e in events)
        {
            if (e.Kind == EventKind.Location && !string.IsNullOrWhiteSpace(e.Detail))
            {
                if (e.Detail.Contains("gespawnt", StringComparison.OrdinalIgnoreCase) ||
                    e.Detail.Contains("spawn", StringComparison.OrdinalIgnoreCase) ||
                    e.Detail.Contains("login", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var locRes = Locations.ResolveLocation(e.Detail);
                if (locRes.DisplayName != "—" && !locRes.DisplayName.StartsWith("Im Transit", StringComparison.OrdinalIgnoreCase))
                {
                    newLocs.Add((e.Time, locRes.RawCode, locRes.DisplayName, locRes.SystemName, locRes.ParentBody, locRes.Type.ToString()));
                }
                else if (!locRes.DisplayName.StartsWith("Im Transit", StringComparison.OrdinalIgnoreCase) &&
                         !e.Detail.StartsWith("Im Transit", StringComparison.OrdinalIgnoreCase))
                {
                    newLocs.Add((e.Time, e.Detail, e.Detail, "Stanton", "—", "Location"));
                }
            }
            else if (e.Kind == EventKind.Quantum && !string.IsNullOrWhiteSpace(e.Detail))
            {
                newQt.Add((e.Time, e.Detail));
            }
        }

        // Live-Events des aktuellen Parsers mit einbeziehen
        if (_parser != null)
        {
            foreach (var l in _parser.LocationVisits)
            {
                if (!newLocs.Any(x => x.Time == l.Time && x.Name.Equals(l.Name, StringComparison.OrdinalIgnoreCase)))
                    newLocs.Add(l);
            }
            foreach (var q in _parser.QuantumDestinations)
            {
                if (!newQt.Any(x => x.Time == q.Time && x.Destination.Equals(q.Destination, StringComparison.OrdinalIgnoreCase)))
                    newQt.Add(q);
            }
        }

        _rawLocations = newLocs;
        _rawQuantumDestinations = newQt;

        UpdatePlacesView();
    }

    private void UpdatePlacesView()
    {
        TotalLocationsVisitedCount = _rawLocations.Select(l => l.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        TotalQuantumJumpsCount = _rawQuantumDestinations.Count;

        FilterPlaces();
    }

    private void FilterPlaces()
    {
        var locQuery = _rawLocations.AsEnumerable();
        var qtQuery = _rawQuantumDestinations.AsEnumerable();

        if (PlacesPeriod != "Alle")
        {
            var cutoff = PlacesPeriod switch
            {
                "24 Stunden" => DateTime.UtcNow.AddHours(-24),
                "7 Tage" => DateTime.UtcNow.AddDays(-7),
                "30 Tage" => DateTime.UtcNow.AddDays(-30),
                "Session" => _sessionStart ?? DateTime.MinValue,
                _ => DateTime.MinValue
            };
            if (cutoff > DateTime.MinValue)
            {
                locQuery = locQuery.Where(l => l.Time >= cutoff);
                qtQuery = qtQuery.Where(q => q.Time >= cutoff);
            }
        }

        if (!string.IsNullOrWhiteSpace(PlacesSearchText))
        {
            var search = PlacesSearchText.Trim();
            locQuery = locQuery.Where(l =>
                l.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (l.System != null && l.System.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (l.Body != null && l.Body.Contains(search, StringComparison.OrdinalIgnoreCase)));

            qtQuery = qtQuery.Where(q => q.Destination.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        // Top-Standorte berechnen
        var locGrouped = locQuery
            .GroupBy(l => l.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => new LocationVisitTotal
            {
                Name = g.Key,
                System = g.First().System,
                Body = g.First().Body,
                Kind = g.First().Kind,
                Visits = g.Count(),
                LastVisit = g.Max(l => l.Time)
            })
            .OrderByDescending(l => l.Visits)
            .ToList();

        TopLocations.Clear();
        foreach (var l in locGrouped) TopLocations.Add(l);

        // Top Quantum-Ziele berechnen
        var qtGrouped = qtQuery
            .GroupBy(q => q.Destination, StringComparer.OrdinalIgnoreCase)
            .Select(g => new QuantumDestinationTotal
            {
                Destination = g.Key,
                Jumps = g.Count(),
                LastJump = g.Max(q => q.Time)
            })
            .OrderByDescending(q => q.Jumps)
            .ToList();

        TopQuantumDestinations.Clear();
        foreach (var q in qtGrouped) TopQuantumDestinations.Add(q);
    }

    partial void OnPlacesPeriodChanged(string value) => FilterPlaces();
    partial void OnPlacesSearchTextChanged(string value) => FilterPlaces();

    [RelayCommand]
    public void SetPlacesPeriod(string period) => PlacesPeriod = period;

    [RelayCommand]
    public void OpenStarmapForPlace(string locationName)
    {
        if (string.IsNullOrWhiteSpace(locationName)) return;
        StarmapSearchText = locationName;
        SelectedTabIndex = 4; // Tab '🗺 Karte'
    }

    // ══ SPENDING METHODEN ══

    private void UpdateSpendingView()
    {
        decimal totalSpend = _rawPurchases.Sum(p => p.TotalPrice);
        int count = _rawPurchases.Count;
        decimal avg = count > 0 ? totalSpend / count : 0;

        SpendingTotalAuecText = $"{totalSpend:N0} aUEC";
        SpendingCountText = $"{count:N0} Käufe";
        SpendingAverageText = count > 0 ? $"{avg:N0} aUEC" : "—";

        // Aufschlüsselung nach Shop
        var shops = _rawPurchases
            .GroupBy(p => p.Shop, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Shop = g.Key, Total = g.Sum(p => p.TotalPrice), Count = g.Count() })
            .OrderByDescending(x => x.Total)
            .Take(12)
            .ToList();

        decimal maxShop = shops.FirstOrDefault()?.Total ?? 1;
        SpendByShop.Clear();
        foreach (var s in shops)
        {
            double bar = maxShop > 0 ? Math.Max(12, (double)(s.Total / maxShop) * 220) : 12;
            SpendByShop.Add(new StatItem
            {
                Label = s.Shop,
                Sub = $"{s.Count} Käufe",
                Value = (long)s.Total,
                BarWidth = bar,
                Color = Brush("#F87171")
            });
        }

        // Aufschlüsselung nach Item
        var items = _rawPurchases
            .GroupBy(p => p.ItemName, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Item = g.Key, Total = g.Sum(p => p.TotalPrice), Qty = g.Sum(p => p.Quantity) })
            .OrderByDescending(x => x.Total)
            .Take(12)
            .ToList();

        decimal maxItem = items.FirstOrDefault()?.Total ?? 1;
        SpendByItem.Clear();
        foreach (var it in items)
        {
            double bar = maxItem > 0 ? Math.Max(12, (double)(it.Total / maxItem) * 220) : 12;
            SpendByItem.Add(new StatItem
            {
                Label = it.Item,
                Sub = $"×{it.Qty}",
                Value = (long)it.Total,
                BarWidth = bar,
                Color = Brush("#FB923C")
            });
        }

        // Aufschlüsselung nach Kategorie
        var categories = _rawPurchases
            .GroupBy(p => p.Category, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Category = g.Key, Total = g.Sum(p => p.TotalPrice), Count = g.Count() })
            .OrderByDescending(x => x.Total)
            .ToList();

        decimal maxCat = categories.FirstOrDefault()?.Total ?? 1;
        SpendByCategory.Clear();
        foreach (var cat in categories)
        {
            double bar = maxCat > 0 ? Math.Max(12, (double)(cat.Total / maxCat) * 220) : 12;
            SpendByCategory.Add(new StatItem
            {
                Label = cat.Category,
                Sub = $"{cat.Count} Posten",
                Value = (long)cat.Total,
                BarWidth = bar,
                Color = Brush("#FBBF24")
            });
        }

        FilterSpending();
    }

    private void FilterSpending()
    {
        var query = _rawPurchases.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SpendingSearchText))
        {
            var search = SpendingSearchText.Trim();
            query = query.Where(p =>
                p.ItemName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Shop.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Location.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        ConfirmedPurchasesView.Clear();
        foreach (var p in query) ConfirmedPurchasesView.Add(p);
    }

    partial void OnSpendingSearchTextChanged(string value) => FilterSpending();

    // ══ LEDGER METHODEN ══

    private void UpdateLedgerView()
    {
        // Laufenden Saldo von alt nach neu aufbauen
        decimal running = 0;
        var withRunning = new List<LedgerRecord>(_rawLedgerRecords.Count);

        foreach (var r in _rawLedgerRecords)
        {
            running += r.Amount;
            withRunning.Add(r with { RunningBalance = running });
        }

        // Neueste zuerst für die Anzeige
        withRunning.Reverse();

        decimal totalInc = withRunning.Where(l => l.Amount > 0).Sum(l => l.Amount);
        decimal totalSpend = withRunning.Where(l => l.Amount < 0).Sum(l => -l.Amount);
        decimal net = totalInc - totalSpend;

        LedgerTotalIncomeText = $"+{totalInc:N0} aUEC";
        LedgerTotalSpendText = $"-{totalSpend:N0} aUEC";
        LedgerNetBalanceText = net >= 0 ? $"+{net:N0} aUEC" : $"{net:N0} aUEC";

        FilterLedger(withRunning);
    }

    private void FilterLedger(List<LedgerRecord>? source = null)
    {
        var query = (source ?? _rawLedgerRecords.AsEnumerable().Reverse()).AsEnumerable();

        if (LedgerFilter != "Alle")
        {
            query = LedgerFilter switch
            {
                "Frachtverkauf" => query.Where(l => l.Kind == "Frachtverkauf"),
                "Frachtkauf" => query.Where(l => l.Kind == "Frachtkauf"),
                "Item gekauft" => query.Where(l => l.Kind == "Item gekauft"),
                "Überweisung" => query.Where(l => l.Kind.Contains("Überweisung")),
                "Sonstige" => query.Where(l => l.Kind != "Frachtverkauf" && l.Kind != "Frachtkauf" && l.Kind != "Item gekauft"),
                _ => query
            };
        }

        if (!string.IsNullOrWhiteSpace(LedgerSearchText))
        {
            var search = LedgerSearchText.Trim();
            query = query.Where(l =>
                l.What.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                l.Where.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                l.Shop.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                l.Kind.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        LedgerView.Clear();
        foreach (var l in query) LedgerView.Add(l);
    }

    partial void OnLedgerFilterChanged(string value) => FilterLedger();
    partial void OnLedgerSearchTextChanged(string value) => FilterLedger();

    [RelayCommand]
    public void SetLedgerFilter(string filter) => LedgerFilter = filter;

    // ══ CARGO METHODEN ══

    private void UpdateCargoView()
    {
        decimal totalInc = _rawCargoTrades.Where(t => t.IsSell).Sum(t => t.TotalAuec);
        decimal totalSpend = _rawCargoTrades.Where(t => !t.IsSell).Sum(t => t.TotalAuec);
        decimal net = totalInc - totalSpend;
        int totalScu = _rawCargoTrades.Sum(t => t.QuantityScu);
        int count = _rawCargoTrades.Count;

        CargoTotalIncomeText = $"+{totalInc:N0} aUEC";
        CargoTotalSpendText = $"-{totalSpend:N0} aUEC";
        CargoNetProfitText = net >= 0 ? $"+{net:N0} aUEC" : $"{net:N0} aUEC";
        CargoTotalScuText = $"{totalScu:N0} SCU";
        CargoTradesCountText = $"{count:N0} Trades";

        FilterCargo();
    }

    private void FilterCargo()
    {
        var query = _rawCargoTrades.AsEnumerable();

        if (CargoActionFilter != "Alle")
        {
            query = CargoActionFilter switch
            {
                "Verkauf" => query.Where(t => t.IsSell),
                "Einkauf" => query.Where(t => !t.IsSell),
                _ => query
            };
        }

        if (!string.IsNullOrWhiteSpace(CargoSearchText))
        {
            var search = CargoSearchText.Trim();
            query = query.Where(t =>
                t.Commodity.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                t.Shop.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                t.Where.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        CargoTradesView.Clear();
        foreach (var t in query) CargoTradesView.Add(t);
    }

    partial void OnCargoActionFilterChanged(string value) => FilterCargo();
    partial void OnCargoSearchTextChanged(string value) => FilterCargo();

    [RelayCommand]
    public void SetCargoActionFilter(string filter) => CargoActionFilter = filter;

    // ══ MARKET & COMMODITY DETAIL METHODEN ══

    private void BuildMarketCatalog()
    {
        // 1. UEX Cache prüfen & Zeitstempel aktualisieren
        UexApiClient.EnsureDiskCacheLoaded();
        if (UexApiClient.LastCommodityPricesFetch.HasValue)
        {
            var localTime = UexApiClient.LastCommodityPricesFetch.Value.ToLocalTime();
            UexMarketLastUpdatedText = $"{localTime:dd.MM.yy HH:mm}";
        }
        else
        {
            UexMarketLastUpdatedText = "Offline (Basispreise)";
        }

        var uexPrices = UexApiClient.GetAllCommodityPrices();

        // 2. Gruppierte Trades des Spielers ermitteln
        var myTradesByComm = _rawCargoTrades
            .GroupBy(t => t.Commodity, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        // 3. Basiskatalog bekannter Handelswaren
        var knownNames = new List<(string Name, string Cat, decimal bSell, decimal bBuy)>
        {
            ("Laranite", "Metalle", 3450, 2850),
            ("Beryl", "Mineralien", 3150, 2700),
            ("Gold", "Metalle", 8950, 6800),
            ("Titanium", "Metalle", 1020, 820),
            ("Copper", "Metalle", 1250, 950),
            ("RMC (Recycled Material)", "Bergung", 14200, 9800),
            ("Construction Materials", "Bergung", 6400, 4800),
            ("Medical Supplies", "Medizin", 2150, 1750),
            ("Agricium", "Metalle", 2900, 2450),
            ("Diamond", "Mineralien", 3350, 2750),
            ("Helium", "Gase", 410, 320),
            ("Hydrogen", "Gase", 380, 290),
            ("Fluorine", "Gase", 480, 360),
            ("Distilled Spirits", "Agrar & Nahrung", 580, 420),
            ("Processed Food", "Agrar & Nahrung", 410, 290),
            ("Stims", "Medizin", 1450, 1050),
            ("WiDoW", "Suchtmittel", 16800, 11500),
            ("Slam", "Suchtmittel", 13400, 8200),
            ("Neon", "Suchtmittel", 12100, 7800),
            ("E'tam", "Suchtmittel", 14200, 9400),
            ("Quantanium", "Mineralien", 27500, 21000),
            ("Taranite", "Metalle", 4200, 3400),
            ("Bexalite", "Mineralien", 4800, 3900),
            ("Hephaestanite", "Metalle", 1650, 1300),
            ("Borase", "Mineralien", 3850, 3100),
            ("Scrap", "Bergung", 190, 120),
            ("Waste", "Bergung", 60, 20)
        };

        // Ergänze um alle Waren, die der Spieler jemals in seinen Logs gehandelt hat
        foreach (var traded in myTradesByComm.Keys)
        {
            if (!knownNames.Any(k => k.Name.Equals(traded, StringComparison.OrdinalIgnoreCase)))
            {
                knownNames.Add((traded, "Allgemein", 0, 0));
            }
        }

        // Ergänze um weitere Waren aus dem UEX-Preiskatalog
        foreach (var uexKvp in uexPrices)
        {
            if (!knownNames.Any(k => k.Name.Equals(uexKvp.Key, StringComparison.OrdinalIgnoreCase)))
            {
                string cat = "Handelsware";
                var lower = uexKvp.Key.ToLowerInvariant();
                if (lower.Contains("gas") || lower.Contains("hydrogen") || lower.Contains("helium") || lower.Contains("iodine") || lower.Contains("chlorine")) cat = "Gase";
                else if (lower.Contains("metal") || lower.Contains("iron") || lower.Contains("copper") || lower.Contains("tungsten") || lower.Contains("titanium")) cat = "Metalle";
                else if (lower.Contains("ore") || lower.Contains("quartz") || lower.Contains("diamond") || lower.Contains("beryl")) cat = "Mineralien";
                else if (lower.Contains("scrap") || lower.Contains("salvage") || lower.Contains("rmc")) cat = "Bergung";
                else if (lower.Contains("med") || lower.Contains("stim")) cat = "Medizin";
                else if (lower.Contains("drug") || lower.Contains("widow") || lower.Contains("slam") || lower.Contains("neon") || lower.Contains("etim")) cat = "Suchtmittel";

                knownNames.Add((uexKvp.Key, cat, uexKvp.Value.BestSell, uexKvp.Value.BestBuy));
            }
        }

        _rawMarketCatalog = knownNames.Select(k =>
        {
            var my = myTradesByComm.GetValueOrDefault(k.Name);
            int soldScu = my?.Where(t => t.IsSell).Sum(t => t.QuantityScu) ?? 0;
            decimal rev = my?.Where(t => t.IsSell).Sum(t => t.TotalAuec) ?? 0;
            int count = my?.Count ?? 0;

            decimal bestSell = k.bSell;
            decimal bestBuy = k.bBuy;
            decimal avgSell = k.bSell > 0 ? k.bSell * 0.94m : 0;
            int termCount = k.bSell > 0 ? 8 : 4;
            int sellTermCount = k.bSell > 0 ? 5 : 2;
            int buyTermCount = k.bBuy > 0 ? 3 : 2;
            string? bestSellTerm = null;
            string? bestBuyTerm = null;

            if (uexPrices.TryGetValue(k.Name, out var uex))
            {
                if (uex.BestSell > 0) bestSell = uex.BestSell;
                if (uex.BestBuy > 0) bestBuy = uex.BestBuy;
                if (uex.AvgSell > 0) avgSell = uex.AvgSell;
                termCount = uex.TerminalsCount;
                sellTermCount = uex.SellTerminalsCount;
                buyTermCount = uex.BuyTerminalsCount;
                bestSellTerm = uex.BestSellTerminal;
                bestBuyTerm = uex.BestBuyTerminal;
            }

            return new MarketCommodityEntry
            {
                Name = k.Name,
                Category = k.Cat,
                TerminalsCount = termCount,
                SellTerminalsCount = sellTermCount,
                BuyTerminalsCount = buyTermCount,
                MyScuSold = soldScu,
                MyRevenue = rev,
                MyTradeCount = count,
                UexBestSell = bestSell,
                UexBestSellTerminal = bestSellTerm,
                UexBestBuy = bestBuy,
                UexBestBuyTerminal = bestBuyTerm,
                UexAvgSell = avgSell
            };
        })
        .OrderByDescending(m => m.MyRevenue)
        .ThenBy(m => m.Name)
        .ToList();

        FilterMarket();
    }

    [RelayCommand]
    public Task RefreshUexMarketData() => RefreshUexMarketDataInternal(force: true);

    public async Task RefreshUexMarketDataInternal(bool force = true)
    {
        if (IsFetchingUexMarket) return;
        IsFetchingUexMarket = true;
        UexMarketLastUpdatedText = "Wird aktualisiert…";

        try
        {
            bool success = await Task.Run(() => UexApiClient.FetchCommodityPricesAsync(force));
            if (UexApiClient.LastCommodityPricesFetch.HasValue)
            {
                var localTime = UexApiClient.LastCommodityPricesFetch.Value.ToLocalTime();
                UexMarketLastUpdatedText = $"{localTime:dd.MM.yy HH:mm}";
            }
            else
            {
                UexMarketLastUpdatedText = success ? "Aktuell" : "Offline (Basisdaten)";
            }

            BuildMarketCatalog();

            if (IsCommodityDetailOpen && SelectedMarketCommodity != null)
            {
                OpenCommodityDetail(SelectedMarketCommodity.Name);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Fehler bei RefreshUexMarketData: {ex.Message}");
            UexMarketLastUpdatedText = "Fehler beim Laden";
        }
        finally
        {
            IsFetchingUexMarket = false;
        }
    }

    private void FilterMarket()
    {
        var query = _rawMarketCatalog.AsEnumerable();

        if (MarketCategoryFilter != "Alle")
        {
            query = query.Where(m => m.Category.Contains(MarketCategoryFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(MarketSearchText))
        {
            var search = MarketSearchText.Trim();
            query = query.Where(m =>
                m.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                m.Category.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        MarketEntriesView.Clear();
        foreach (var m in query) MarketEntriesView.Add(m);
    }

    partial void OnMarketCategoryFilterChanged(string value) => FilterMarket();
    partial void OnMarketSearchTextChanged(string value) => FilterMarket();

    [RelayCommand]
    public void SetMarketCategoryFilter(string cat) => MarketCategoryFilter = cat;

    [RelayCommand]
    public void OpenCommodityDetail(object? param)
    {
        string? name = param switch
        {
            MarketCommodityEntry entry => entry.Name,
            CargoTradeRecord trade => trade.Commodity,
            string s => s,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(name)) return;

        var entryMatch = _rawMarketCatalog.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? new MarketCommodityEntry { Name = name, Category = "Handelsware" };

        SelectedMarketCommodity = entryMatch;
        IsCommodityDetailOpen = true;

        SelectedCommodityBestSellText = entryMatch.UexBestSell > 0 ? $"{entryMatch.UexBestSell:N0} aUEC" : "—";
        SelectedCommodityBestBuyText = entryMatch.UexBestBuy > 0 ? $"{entryMatch.UexBestBuy:N0} aUEC" : "—";
        SelectedCommodityMarginText = entryMatch.UexMargin != 0 ? $"{entryMatch.UexMargin:+#,##0;-#,##0;0} aUEC" : "—";
        SelectedCommoditySoldText = entryMatch.MyScuSold > 0 ? $"{entryMatch.MyScuSold:N0} SCU" : "0 SCU";
        SelectedCommodityRevenueText = entryMatch.MyRevenue > 0 ? $"{entryMatch.MyRevenue:N0} aUEC" : "0 aUEC";

        // Eigene Belege für diese Ware filtern
        SelectedCommodityReceipts.Clear();
        var receipts = _rawCargoTrades
            .Where(t => t.Commodity.Equals(name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.Timestamp);
        foreach (var r in receipts) SelectedCommodityReceipts.Add(r);

        // Terminals aufbauen (Echte UEX-Terminals oder Fallback)
        SelectedCommodityTerminals.Clear();
        var terms = GenerateTerminalsForCommodity(entryMatch);
        foreach (var t in terms) SelectedCommodityTerminals.Add(t);

        // Wenn wir nicht im Markt-Tab sind, dorthin wechseln
        SelectedTabIndex = 8; // Tab '📊 Markt'
    }

    [RelayCommand]
    public void CloseCommodityDetail()
    {
        IsCommodityDetailOpen = false;
        SelectedMarketCommodity = null;
    }

    private List<CommodityTerminalRow> GenerateTerminalsForCommodity(MarketCommodityEntry comm)
    {
        var uex = UexApiClient.GetCommodityPrice(comm.Name);
        if (uex != null && uex.Terminals.Count > 0)
        {
            return uex.Terminals
                .OrderByDescending(t => t.PriceSell)
                .ThenBy(t => t.PriceBuy)
                .Select(t =>
                {
                    bool isLawless = t.TerminalName.Contains("Grim", StringComparison.OrdinalIgnoreCase) ||
                                     t.TerminalName.Contains("Brio", StringComparison.OrdinalIgnoreCase) ||
                                     t.TerminalName.Contains("Reclamation", StringComparison.OrdinalIgnoreCase) ||
                                     t.TerminalName.Contains("Samson", StringComparison.OrdinalIgnoreCase) ||
                                     t.TerminalName.Contains("Pyro", StringComparison.OrdinalIgnoreCase) ||
                                     t.TerminalName.Contains("Checkmate", StringComparison.OrdinalIgnoreCase) ||
                                     t.TerminalName.Contains("Orbituary", StringComparison.OrdinalIgnoreCase) ||
                                     t.TerminalName.Contains("Ruin", StringComparison.OrdinalIgnoreCase);

                    string system = (t.TerminalName.Contains("Pyro", StringComparison.OrdinalIgnoreCase) ||
                                     t.TerminalName.Contains("Checkmate", StringComparison.OrdinalIgnoreCase) ||
                                     t.TerminalName.Contains("Orbituary", StringComparison.OrdinalIgnoreCase) ||
                                     t.TerminalName.Contains("Ruin", StringComparison.OrdinalIgnoreCase) ||
                                     t.TerminalName.Contains("PatchCity", StringComparison.OrdinalIgnoreCase))
                                     ? "Pyro" : "Stanton";

                    return new CommodityTerminalRow
                    {
                        Terminal = t.TerminalName,
                        System = system,
                        Location = t.TerminalName,
                        IsLawless = isLawless,
                        BuyPrice = t.PriceBuy,
                        SellPrice = t.PriceSell,
                        StockScu = t.StockScu,
                        DemandScu = t.DemandScu
                    };
                })
                .ToList();
        }

        // Fallback wenn offline oder keine Daten
        decimal baseSell = comm.UexBestSell > 0 ? comm.UexBestSell : 1000;
        decimal baseBuy = comm.UexBestBuy > 0 ? comm.UexBestBuy : 800;

        return new List<CommodityTerminalRow>
        {
            new() { Terminal = "Lorville TDD (Admin)", System = "Stanton", Location = "Lorville · Hurston", IsLawless = false, BuyPrice = 0, SellPrice = baseSell, StockScu = 0, DemandScu = 24000 },
            new() { Terminal = "Area 18 TDD", System = "Stanton", Location = "Area 18 · ArcCorp", IsLawless = false, BuyPrice = 0, SellPrice = Math.Round(baseSell * 0.98m), StockScu = 0, DemandScu = 18500 },
            new() { Terminal = "Orison Commerce Center", System = "Stanton", Location = "Orison · Crusader", IsLawless = false, BuyPrice = 0, SellPrice = Math.Round(baseSell * 0.97m), StockScu = 0, DemandScu = 16000 },
            new() { Terminal = "Brio's Breaker Yard", System = "Stanton", Location = "Daymar · Crusader", IsLawless = true, BuyPrice = 0, SellPrice = Math.Round(baseSell * 1.05m), StockScu = 0, DemandScu = 8000 },
            new() { Terminal = "Samson & Son Salvage", System = "Stanton", Location = "Wala · ArcCorp", IsLawless = true, BuyPrice = 0, SellPrice = Math.Round(baseSell * 1.03m), StockScu = 0, DemandScu = 6500 },
            new() { Terminal = "HDMS-Lathan", System = "Stanton", Location = "Arial · Hurston", IsLawless = false, BuyPrice = baseBuy, SellPrice = 0, StockScu = 14500, DemandScu = 0 },
            new() { Terminal = "HDMS-Woodward", System = "Stanton", Location = "Arial · Hurston", IsLawless = false, BuyPrice = Math.Round(baseBuy * 1.02m), SellPrice = 0, StockScu = 12000, DemandScu = 0 },
            new() { Terminal = "Checkmate Station Trade Kiosk", System = "Pyro", Location = "Checkmate · Monox", IsLawless = true, BuyPrice = Math.Round(baseBuy * 1.08m), SellPrice = Math.Round(baseSell * 1.06m), StockScu = 7500, DemandScu = 9000 },
            new() { Terminal = "Orbituary Admin Kiosk", System = "Pyro", Location = "Orbituary · Bloom", IsLawless = true, BuyPrice = 0, SellPrice = Math.Round(baseSell * 1.08m), StockScu = 0, DemandScu = 11000 }
        };
    }
}
