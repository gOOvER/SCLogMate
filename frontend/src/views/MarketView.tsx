import React, { useEffect, useState, useMemo } from 'react';
import {
  bridge,
  MarketCommodityDto,
  TradeRouteDto,
  SalvagePriceSummaryDto,
} from '../services/photinoBridge';
import {
  ShoppingBag,
  Search,
  TrendingUp,
  ArrowRight,
  Wrench,
} from 'lucide-react';

export const MarketView: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'routes' | 'calculator'>('routes');
  const [commodities, setCommodities] = useState<MarketCommodityDto[]>([]);
  const [tradeRoutes, setTradeRoutes] = useState<TradeRouteDto[]>([]);
  const [salvagePrices, setSalvagePrices] = useState<SalvagePriceSummaryDto[]>([]);
  const [isLoadingRoutes, setIsLoadingRoutes] = useState<boolean>(false);

  const [search, setSearch] = useState<string>('');
  const [selectedCommodityName, setSelectedCommodityName] = useState<string>('Laranite');
  const [cargoScu, setCargoScu] = useState<number>(696); // Default C2 Hercules
  const [maxBudgetAuec, setMaxBudgetAuec] = useState<number>(15000000);
  const [routeSystemFilter, setRouteSystemFilter] = useState<string>('all');

  const fetchMarket = async () => {
    try {
      const res = await bridge.sendRequest<MarketCommodityDto[]>('get_market');
      setCommodities(res || []);
      if (res && res.length > 0 && !selectedCommodityName) {
        setSelectedCommodityName(res[0].name);
      }
    } catch (err) {
      console.error('Failed to load market commodities:', err);
    }
  };

  const fetchSmartRoutes = async () => {
    try {
      setIsLoadingRoutes(true);
      const res = await bridge.sendRequest<TradeRouteDto[]>('get_smart_trade_routes', {
        cargoHoldScu: cargoScu,
        maxCapitalAuec: maxBudgetAuec,
        system: routeSystemFilter,
      });
      setTradeRoutes(res || []);

      const salvRes = await bridge.sendRequest<SalvagePriceSummaryDto[]>('get_salvage_prices');
      setSalvagePrices(salvRes || []);
    } catch (err) {
      console.error('Failed to load trade routes:', err);
    } finally {
      setIsLoadingRoutes(false);
    }
  };

  useEffect(() => {
    fetchMarket();
    fetchSmartRoutes();
  }, []);

  useEffect(() => {
    fetchSmartRoutes();
  }, [cargoScu, maxBudgetAuec, routeSystemFilter]);

  const selectedCommodity = useMemo(() => {
    return commodities.find((c) => c.name === selectedCommodityName) || commodities[0] || null;
  }, [commodities, selectedCommodityName]);

  // Profit calculation for single commodity
  const calculation = useMemo(() => {
    if (!selectedCommodity) {
      return { investment: 0, revenue: 0, profit: 0, roi: 0 };
    }
    const investment = selectedCommodity.avgBuyPrice * cargoScu * 100;
    const revenue = selectedCommodity.avgSellPrice * cargoScu * 100;
    const profit = revenue - investment;
    const roi = investment > 0 ? (profit / investment) * 100 : 0;
    return {
      investment: Math.round(investment),
      revenue: Math.round(revenue),
      profit: Math.round(profit),
      roi: Math.round(roi * 10) / 10,
    };
  }, [selectedCommodity, cargoScu]);

  const filteredCommodities = useMemo(() => {
    return commodities.filter((c) => {
      if (search.trim()) {
        const q = search.toLowerCase();
        return (
          c.name.toLowerCase().includes(q) ||
          c.category.toLowerCase().includes(q) ||
          c.bestBuyLocation.toLowerCase().includes(q) ||
          c.bestSellLocation.toLowerCase().includes(q)
        );
      }
      return true;
    });
  }, [commodities, search]);

  const shipPresets = [
    { label: 'C2 (696 SCU)', scu: 696 },
    { label: 'Caterpillar (576 SCU)', scu: 576 },
    { label: 'Taurus (174 SCU)', scu: 174 },
    { label: 'Freelancer MAX (120 SCU)', scu: 120 },
    { label: 'Cutlass (46 SCU)', scu: 46 },
  ];

  return (
    <div className="flex flex-col min-h-full space-y-4">
      {/* Navigation Tabs */}
      <div className="flex items-center justify-between border-b border-slate-800 pb-2">
        <div className="flex items-center gap-2">
          <button
            onClick={() => setActiveTab('routes')}
            className={`flex items-center gap-2 px-4 py-2 text-xs font-mono font-semibold rounded-t cursor-pointer transition ${
              activeTab === 'routes'
                ? 'bg-slate-800 text-cyan-300 border-b-2 border-cyan-400 shadow-[0_2px_8px_rgba(0,240,255,0.15)]'
                : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            <TrendingUp className="w-4 h-4 text-emerald-400" />
            Smarte UEXcorp Handelsrouten & Salvage ({tradeRoutes.length})
          </button>
          <button
            onClick={() => setActiveTab('calculator')}
            className={`flex items-center gap-2 px-4 py-2 text-xs font-mono font-semibold rounded-t cursor-pointer transition ${
              activeTab === 'calculator'
                ? 'bg-slate-800 text-cyan-300 border-b-2 border-cyan-400 shadow-[0_2px_8px_rgba(0,240,255,0.15)]'
                : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            <ShoppingBag className="w-4 h-4 text-cyan-400" />
            Warenkatalog & Einzelkalkulator ({commodities.length})
          </button>
        </div>

        {/* Global Ship Presets */}
        <div className="flex items-center gap-1.5 overflow-x-auto">
          {shipPresets.map((s) => (
            <button
              key={s.label}
              onClick={() => setCargoScu(s.scu)}
              className={`px-2.5 py-1 text-[11px] font-mono rounded border transition cursor-pointer shrink-0 ${
                cargoScu === s.scu
                  ? 'bg-cyan-500/20 text-cyan-300 border-cyan-500/50 shadow-[0_0_8px_rgba(0,240,255,0.2)]'
                  : 'bg-slate-900 border-slate-800 text-slate-400 hover:text-slate-200'
              }`}
            >
              {s.label}
            </button>
          ))}
        </div>
      </div>

      {/* TAB 1: SMART TRADE ROUTES & SCRAPPER PRICES */}
      {activeTab === 'routes' && (
        <div className="flex-1 flex flex-col space-y-4 overflow-y-auto pr-1">
          {/* Controls bar */}
          <div className="sc-glass rounded-lg p-3 border border-slate-800 flex flex-wrap items-center justify-between gap-3 text-xs font-mono">
            <div className="flex items-center gap-3">
              <div className="flex items-center gap-1 bg-slate-900 p-1 rounded border border-slate-800">
                {['all', 'Stanton', 'Pyro'].map((sys) => (
                  <button
                    key={sys}
                    onClick={() => setRouteSystemFilter(sys)}
                    className={`px-3 py-1 rounded cursor-pointer transition ${
                      routeSystemFilter === sys
                        ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40'
                        : 'text-slate-400 hover:text-slate-200'
                    }`}
                  >
                    {sys === 'all' ? 'Alle Systeme' : sys}
                  </button>
                ))}
              </div>

              <div className="flex items-center gap-1.5">
                <span className="text-slate-400">Max. Budget:</span>
                <select
                  value={maxBudgetAuec}
                  onChange={(e) => setMaxBudgetAuec(parseInt(e.target.value, 10))}
                  className="bg-slate-900 border border-slate-800 rounded px-2.5 py-1 text-slate-200 focus:outline-none focus:border-cyan-500"
                >
                  <option value={500000}>500k aUEC</option>
                  <option value={2000000}>2 Mio aUEC</option>
                  <option value={5000000}>5 Mio aUEC</option>
                  <option value={15000000}>15 Mio aUEC</option>
                  <option value={50000000}>50 Mio aUEC</option>
                </select>
              </div>
            </div>

            <div className="text-slate-400 flex items-center gap-2">
              <span>Ladekapazität: <strong className="text-cyan-300">{cargoScu} SCU</strong></span>
            </div>
          </div>

          {/* Salvage & Mining Quick Pricer Banner */}
          {salvagePrices.length > 0 && (
            <div className="sc-glass rounded-lg p-3 border border-emerald-500/30 bg-emerald-950/15 sc-hud-corner space-y-2">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2 text-xs font-mono font-bold uppercase tracking-wider text-emerald-300">
                  <Wrench className="w-4 h-4 text-emerald-400" />
                  Salvage- & Erz-Bestpreise (Wo verkaufe ich am besten?)
                </div>
                <span className="text-[10px] font-mono text-slate-400">Live UEXcorp Marktdaten</span>
              </div>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-2 text-xs font-mono">
                {salvagePrices.map((s) => (
                  <div key={s.materialName} className="p-2 rounded bg-slate-900/80 border border-slate-800/80 flex flex-col justify-between">
                    <div>
                      <div className="font-bold text-slate-200 truncate">{s.materialName}</div>
                      <div className="text-[10px] text-slate-400 truncate">{s.bestSellLocation}</div>
                    </div>
                    <div className="text-emerald-400 font-bold mt-1 text-right">
                      {s.bestSellPricePerScu.toLocaleString('de-DE')} aUEC / SCU
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Trade Routes Grid */}
          <div className="space-y-2">
            <h3 className="text-xs font-mono font-bold uppercase tracking-wider text-slate-300 flex items-center gap-2">
              <TrendingUp className="w-4 h-4 text-emerald-400" />
              Top Profitabelste Frachtrouten (sortiert nach Reingewinn)
            </h3>

            {isLoadingRoutes ? (
              <div className="text-center py-12 text-slate-400 font-mono text-xs">Routen werden kalkuliert...</div>
            ) : tradeRoutes.length === 0 ? (
              <div className="sc-glass rounded-lg p-8 text-center text-slate-400 font-mono text-xs">
                Keine profitablen Routen für das gewählte Budget und System gefunden.
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                {tradeRoutes.map((r, idx) => (
                  <div
                    key={r.id + idx}
                    className="sc-glass rounded-lg p-3.5 border border-slate-800 hover:border-emerald-500/40 transition flex flex-col justify-between sc-hud-corner group"
                  >
                    <div>
                      <div className="flex items-start justify-between gap-2">
                        <div>
                          <span className="text-xs font-mono text-cyan-400 uppercase font-bold">
                            #{idx + 1} · {r.commodity}
                          </span>
                          <div className="text-[10px] font-mono text-slate-400">
                            System: {r.system} · {r.maxScu} SCU Ladung
                          </div>
                        </div>

                        <span
                          className={`px-2 py-0.5 rounded text-[9px] font-mono font-bold border uppercase ${
                            r.riskLevel === 'Sicher'
                              ? 'bg-emerald-950/60 text-emerald-300 border-emerald-800'
                              : r.riskLevel === 'Mittel'
                              ? 'bg-amber-950/60 text-amber-300 border-amber-800'
                              : 'bg-rose-950/60 text-rose-300 border-rose-800'
                          }`}
                        >
                          {r.riskLevel}
                        </span>
                      </div>

                      {/* Origin -> Destination */}
                      <div className="mt-2.5 p-2 rounded bg-slate-900/80 border border-slate-800/80 flex items-center justify-between text-xs font-mono">
                        <div className="truncate max-w-[45%]">
                          <div className="text-[10px] text-slate-500">Kauf:</div>
                          <div className="text-slate-200 font-bold truncate">{r.origin}</div>
                          <div className="text-[10px] text-slate-400">{r.buyPricePerScu.toLocaleString('de-DE')} aUEC/SCU</div>
                        </div>
                        <ArrowRight className="w-4 h-4 text-cyan-400 shrink-0" />
                        <div className="text-right truncate max-w-[45%]">
                          <div className="text-[10px] text-slate-500">Verkauf:</div>
                          <div className="text-slate-200 font-bold truncate">{r.destination}</div>
                          <div className="text-[10px] text-slate-400">{r.sellPricePerScu.toLocaleString('de-DE')} aUEC/SCU</div>
                        </div>
                      </div>
                    </div>

                    {/* Financial Summary */}
                    <div className="mt-3 pt-2.5 border-t border-slate-800/80 grid grid-cols-3 gap-2 text-xs font-mono">
                      <div>
                        <div className="text-[10px] text-slate-500">Investition:</div>
                        <div className="text-slate-300">~ {(r.investmentAuec / 1000).toLocaleString('de-DE')}k</div>
                      </div>
                      <div>
                        <div className="text-[10px] text-slate-500">Marge / SCU:</div>
                        <div className="text-cyan-400">+{r.profitPerScu.toLocaleString('de-DE')}</div>
                      </div>
                      <div className="text-right">
                        <div className="text-[10px] text-slate-500">Reingewinn:</div>
                        <div className="text-emerald-400 font-bold text-sm">
                          +{(r.totalProfitAuec / 1000).toLocaleString('de-DE')}k <span className="text-[10px] text-emerald-500">({r.roiPercent}%)</span>
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {/* TAB 2: COMMODITY CATALOG & SINGLE CALCULATOR */}
      {activeTab === 'calculator' && (
        <div className="flex-1 flex flex-col space-y-4 overflow-y-auto pr-1">
          {/* Cargo Run Profit Calculator Header */}
          <div className="sc-glass rounded-lg p-4 border border-slate-800 sc-hud-corner space-y-3">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="flex items-center gap-2">
                <ShoppingBag className="w-5 h-5 text-cyan-400" />
                <div>
                  <h2 className="text-sm font-bold text-slate-100 uppercase tracking-wider font-mono">
                    Einzelwaren-Kalkulator
                  </h2>
                  <div className="text-[11px] text-slate-400">
                    Wähle ein Handelsgut aus dem UEXcorp Katalog zur manuellen Frachtberechnung
                  </div>
                </div>
              </div>
            </div>

            {/* Selected Commodity Dashboard */}
            {selectedCommodity && (
              <div className="grid grid-cols-1 md:grid-cols-4 gap-3 pt-2 border-t border-slate-800/80 font-mono text-xs">
                <div className="p-2.5 rounded bg-slate-900/60 border border-slate-800">
                  <div className="text-slate-500 text-[10px]">WARE & PREIS</div>
                  <div className="text-sm font-bold text-slate-100 mt-0.5">{selectedCommodity.name}</div>
                  <div className="text-slate-400 text-[10px]">
                    Kauf: {selectedCommodity.avgBuyPrice * 100} aUEC · Verk: {selectedCommodity.avgSellPrice * 100} aUEC / SCU
                  </div>
                </div>

                <div className="p-2.5 rounded bg-slate-900/60 border border-slate-800">
                  <div className="text-slate-500 text-[10px]">INVESTITION</div>
                  <div className="text-sm font-bold text-slate-200 mt-0.5">
                    {calculation.investment.toLocaleString('de-DE')} aUEC
                  </div>
                  <div className="text-slate-400 text-[10px]">für {cargoScu} SCU</div>
                </div>

                <div className="p-2.5 rounded bg-slate-900/60 border border-slate-800">
                  <div className="text-slate-500 text-[10px]">ERLÖS</div>
                  <div className="text-sm font-bold text-cyan-300 mt-0.5">
                    {calculation.revenue.toLocaleString('de-DE')} aUEC
                  </div>
                  <div className="text-slate-400 text-[10px]">Bruttoverkauf</div>
                </div>

                <div className="p-2.5 rounded bg-emerald-950/20 border border-emerald-500/30">
                  <div className="text-emerald-400 text-[10px] font-bold">REINGEWINN & ROI</div>
                  <div className="text-sm font-bold text-emerald-300 mt-0.5">
                    +{calculation.profit.toLocaleString('de-DE')} aUEC
                  </div>
                  <div className="text-emerald-400 text-[10px]">{calculation.roi}% Rendite</div>
                </div>
              </div>
            )}
          </div>

          {/* Search bar */}
          <div className="relative">
            <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-500" />
            <input
              type="text"
              placeholder="Handelsware nach Name, Kategorie oder Station filtern..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="bg-slate-900 border border-slate-800 rounded pl-8 pr-3 py-1.5 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-cyan-500 w-full font-mono"
            />
          </div>

          {/* Commodity Cards Grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3">
            {filteredCommodities.map((c) => (
              <div
                key={c.name}
                onClick={() => setSelectedCommodityName(c.name)}
                className={`sc-glass rounded-lg p-3 border transition cursor-pointer flex flex-col justify-between ${
                  selectedCommodityName === c.name
                    ? 'border-cyan-500 bg-cyan-950/20 shadow-[0_0_12px_rgba(0,240,255,0.15)]'
                    : 'border-slate-800 hover:border-slate-700'
                }`}
              >
                <div>
                  <div className="flex items-center justify-between">
                    <span className="font-bold text-slate-100 text-xs font-mono">{c.name}</span>
                    <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-slate-900 border border-slate-800 text-slate-400">
                      {c.category}
                    </span>
                  </div>

                  <div className="mt-2 text-xs font-mono space-y-1">
                    <div className="flex justify-between text-[11px]">
                      <span className="text-slate-500">Kaufort:</span>
                      <span className="text-slate-300 truncate max-w-[170px]">{c.bestBuyLocation}</span>
                    </div>
                    <div className="flex justify-between text-[11px]">
                      <span className="text-slate-500">Verkaufort:</span>
                      <span className="text-slate-300 truncate max-w-[170px]">{c.bestSellLocation}</span>
                    </div>
                  </div>
                </div>

                <div className="mt-3 pt-2 border-t border-slate-800/80 flex items-center justify-between text-xs font-mono">
                  <span className="text-slate-400 text-[10px]">Marge: +{(c.margin * 100).toLocaleString('de-DE')} / SCU</span>
                  <span className="text-emerald-400 font-bold text-[11px]">
                    +{(c.margin * cargoScu * 100).toLocaleString('de-DE')} aUEC
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};
