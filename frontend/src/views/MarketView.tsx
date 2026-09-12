import React, { useEffect, useState, useMemo } from 'react';
import {
  bridge,
  MarketCommodityDto,
  TradeRouteDto,
  SalvagePriceSummaryDto,
} from '../services/photinoBridge';
import {
  TrendingUp,
  ShoppingBag,
  Wrench,
  Search,
  ArrowRight,
  Ship,
} from 'lucide-react';

export const MarketView: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'routes' | 'salvage' | 'calculator'>('routes');
  const [commodities, setCommodities] = useState<MarketCommodityDto[]>([]);
  const [tradeRoutes, setTradeRoutes] = useState<TradeRouteDto[]>([]);
  const [salvagePrices, setSalvagePrices] = useState<SalvagePriceSummaryDto[]>([]);
  const [isLoadingRoutes, setIsLoadingRoutes] = useState<boolean>(false);

  const [search, setSearch] = useState<string>('');
  const [selectedCommodityName, setSelectedCommodityName] = useState<string>('Laranite');
  const [cargoScu, setCargoScu] = useState<number>(696); // Default: C2 Hercules
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

  // Profit calculation for single commodity in calculator tab
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
    { label: 'C2', scu: 696 },
    { label: 'Caterpillar', scu: 576 },
    { label: 'Taurus', scu: 174 },
    { label: 'Freelancer MAX', scu: 120 },
    { label: 'Cutlass Black', scu: 46 },
  ];

  return (
    <div className="flex flex-col min-h-full space-y-3 select-none">
      {/* ══ 1. Saubere Haupt-Tabs ══ */}
      <div className="flex items-center gap-2 border-b border-cyan-950/80 pb-2">
        <button
          onClick={() => setActiveTab('routes')}
          className={`flex items-center gap-2 px-3.5 py-1.5 text-xs font-mono font-bold rounded transition cursor-pointer ${
            activeTab === 'routes'
              ? 'bg-cyan-950/80 text-cyan-300 border border-cyan-500/70 shadow-[0_0_8px_rgba(6,182,212,0.25)]'
              : 'text-slate-400 hover:text-slate-200 border border-transparent'
          }`}
        >
          <TrendingUp className="w-3.5 h-3.5 text-emerald-400" />
          <span>Handelsrouten</span>
          {tradeRoutes.length > 0 && (
            <span className="ml-1 px-1.5 py-0.2 rounded text-[10px] bg-cyan-900/60 text-cyan-200">
              {tradeRoutes.length}
            </span>
          )}
        </button>

        <button
          onClick={() => setActiveTab('salvage')}
          className={`flex items-center gap-2 px-3.5 py-1.5 text-xs font-mono font-bold rounded transition cursor-pointer ${
            activeTab === 'salvage'
              ? 'bg-cyan-950/80 text-cyan-300 border border-cyan-500/70 shadow-[0_0_8px_rgba(6,182,212,0.25)]'
              : 'text-slate-400 hover:text-slate-200 border border-transparent'
          }`}
        >
          <Wrench className="w-3.5 h-3.5 text-amber-400" />
          <span>Salvage & Schrott-Preise</span>
          {salvagePrices.length > 0 && (
            <span className="ml-1 px-1.5 py-0.2 rounded text-[10px] bg-amber-900/60 text-amber-200">
              {salvagePrices.length}
            </span>
          )}
        </button>

        <button
          onClick={() => setActiveTab('calculator')}
          className={`flex items-center gap-2 px-3.5 py-1.5 text-xs font-mono font-bold rounded transition cursor-pointer ${
            activeTab === 'calculator'
              ? 'bg-cyan-950/80 text-cyan-300 border border-cyan-500/70 shadow-[0_0_8px_rgba(6,182,212,0.25)]'
              : 'text-slate-400 hover:text-slate-200 border border-transparent'
          }`}
        >
          <ShoppingBag className="w-3.5 h-3.5 text-cyan-400" />
          <span>Warenrechner</span>
        </button>
      </div>

      {/* ══ TAB 1: SMARTE HANDELSROUTEN ══ */}
      {activeTab === 'routes' && (
        <div className="flex-1 flex flex-col space-y-3 overflow-y-auto pr-1">
          {/* Konfigurations- & Filterleiste */}
          <div className="sc-glass rounded-lg p-3 border border-cyan-950/80 bg-[#040914]/90 flex flex-wrap items-center justify-between gap-3 text-xs font-mono">
            {/* System-Filter */}
            <div className="flex items-center gap-2">
              <span className="text-slate-500 text-[11px] uppercase tracking-wider">System:</span>
              <div className="flex items-center gap-1 bg-[#030814] p-0.5 rounded border border-cyan-950">
                {['all', 'Stanton', 'Pyro'].map((sys) => (
                  <button
                    key={sys}
                    onClick={() => setRouteSystemFilter(sys)}
                    className={`px-2.5 py-0.5 rounded text-[11px] font-semibold transition cursor-pointer ${
                      routeSystemFilter === sys
                        ? 'bg-cyan-900/70 text-cyan-300 border border-cyan-700/60'
                        : 'text-slate-400 hover:text-white'
                    }`}
                  >
                    {sys === 'all' ? 'Alle Systeme' : sys}
                  </button>
                ))}
              </div>
            </div>

            {/* Schiffs- & Laderaum Schnellwahl */}
            <div className="flex items-center gap-2 flex-wrap">
              <span className="text-slate-500 text-[11px] uppercase tracking-wider flex items-center gap-1">
                <Ship className="w-3.5 h-3.5 text-cyan-400" />
                Laderaum:
              </span>
              <div className="flex items-center gap-1">
                {shipPresets.map((s) => (
                  <button
                    key={s.label}
                    onClick={() => setCargoScu(s.scu)}
                    className={`px-2 py-0.5 text-[11px] rounded transition cursor-pointer border ${
                      cargoScu === s.scu
                        ? 'bg-cyan-500/20 text-cyan-300 border-cyan-500/60 font-bold'
                        : 'bg-[#030814] border-cyan-950 text-slate-400 hover:text-slate-200'
                    }`}
                  >
                    {s.label} ({s.scu})
                  </button>
                ))}
              </div>

              {/* Manuelle SCU Eingabe */}
              <div className="flex items-center gap-1 bg-[#030814] border border-cyan-950 rounded px-2 py-0.5">
                <input
                  type="number"
                  min="1"
                  max="5000"
                  value={cargoScu}
                  onChange={(e) => setCargoScu(Math.max(1, parseInt(e.target.value, 10) || 1))}
                  className="bg-transparent border-none text-slate-200 text-xs font-bold w-12 text-right focus:outline-none"
                />
                <span className="text-slate-500 text-[10px]">SCU</span>
              </div>
            </div>

            {/* Budget Limit */}
            <div className="flex items-center gap-2">
              <span className="text-slate-500 text-[11px] uppercase tracking-wider">Max. Budget:</span>
              <select
                value={maxBudgetAuec}
                onChange={(e) => setMaxBudgetAuec(parseInt(e.target.value, 10))}
                className="bg-[#030814] border border-cyan-950 rounded px-2 py-1 text-slate-200 text-xs focus:outline-none focus:border-cyan-500 cursor-pointer"
              >
                <option value={500000}>500.000 aUEC</option>
                <option value={2000000}>2.000.000 aUEC</option>
                <option value={5000000}>5.000.000 aUEC</option>
                <option value={15000000}>15.000.000 aUEC</option>
                <option value={50000000}>50.000.000 aUEC</option>
                <option value={999999999}>Unbegrenzt</option>
              </select>
            </div>
          </div>

          {/* Routen-Karten / Liste */}
          <div className="space-y-2">
            <div className="flex items-center justify-between text-xs font-mono text-slate-400">
              <span className="font-bold uppercase tracking-wider text-slate-300 flex items-center gap-1.5">
                <TrendingUp className="w-4 h-4 text-emerald-400" />
                Profitabelste Handelsrouten ({tradeRoutes.length})
              </span>
              <span>Sortiert nach absolutem Reingewinn für {cargoScu} SCU</span>
            </div>

            {isLoadingRoutes ? (
              <div className="sc-glass rounded-lg p-12 text-center text-slate-400 font-mono text-xs">
                Routen werden anhand aktueller UEXcorp Marktpreise kalkuliert...
              </div>
            ) : tradeRoutes.length === 0 ? (
              <div className="sc-glass rounded-lg p-12 text-center text-slate-400 font-mono text-xs border border-cyan-950">
                Keine profitablen Handelsrouten für das gewählte Budget ({maxBudgetAuec.toLocaleString('de-DE')} aUEC) und {cargoScu} SCU gefunden.
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                {tradeRoutes.map((r, idx) => (
                  <div
                    key={r.id + idx}
                    className="sc-glass rounded-lg p-3.5 border border-cyan-950/80 hover:border-emerald-500/40 transition flex flex-col justify-between sc-hud-corner bg-[#040914]/90 group"
                  >
                    <div>
                      {/* Header: Rang, Ware, System & Risiko */}
                      <div className="flex items-start justify-between gap-2">
                        <div>
                          <div className="flex items-center gap-2">
                            <span className="text-xs font-mono font-bold text-cyan-400">
                              #{idx + 1}
                            </span>
                            <span className="text-sm font-bold text-slate-100 font-mono">
                              {r.commodity}
                            </span>
                          </div>
                          <div className="text-[10px] font-mono text-slate-400 mt-0.5">
                            System: <strong className="text-slate-300">{r.system}</strong> · {r.maxScu} SCU Ladung
                          </div>
                        </div>

                        <span
                          className={`px-2 py-0.5 rounded text-[10px] font-mono font-bold border uppercase ${
                            r.riskLevel === 'Sicher'
                              ? 'bg-emerald-950/60 text-emerald-300 border-emerald-800/60'
                              : r.riskLevel === 'Mittel'
                              ? 'bg-amber-950/60 text-amber-300 border-amber-800/60'
                              : 'bg-rose-950/60 text-rose-300 border-rose-800/60'
                          }`}
                        >
                          {r.riskLevel}
                        </span>
                      </div>

                      {/* Stationen: Einkauf -> Verkauf */}
                      <div className="mt-2.5 p-2 rounded bg-[#030814] border border-cyan-950 flex items-center justify-between text-xs font-mono">
                        <div className="truncate max-w-[45%]">
                          <div className="text-[10px] text-slate-500 uppercase font-semibold">Einkauf:</div>
                          <div className="text-slate-200 font-bold truncate" title={r.origin}>{r.origin}</div>
                          <div className="text-[10px] text-cyan-400">{r.buyPricePerScu.toLocaleString('de-DE')} aUEC / SCU</div>
                        </div>

                        <ArrowRight className="w-4 h-4 text-cyan-400 shrink-0 mx-2" />

                        <div className="text-right truncate max-w-[45%]">
                          <div className="text-[10px] text-slate-500 uppercase font-semibold">Verkauf:</div>
                          <div className="text-slate-200 font-bold truncate" title={r.destination}>{r.destination}</div>
                          <div className="text-[10px] text-emerald-400">{r.sellPricePerScu.toLocaleString('de-DE')} aUEC / SCU</div>
                        </div>
                      </div>
                    </div>

                    {/* Finanz-Kennzahlen */}
                    <div className="mt-3 pt-2.5 border-t border-cyan-950/80 grid grid-cols-3 gap-2 text-xs font-mono">
                      <div>
                        <div className="text-[10px] text-slate-500">Investition:</div>
                        <div className="text-slate-300 font-semibold">
                          ~ {(r.investmentAuec).toLocaleString('de-DE')} aUEC
                        </div>
                      </div>

                      <div>
                        <div className="text-[10px] text-slate-500">Marge / SCU:</div>
                        <div className="text-cyan-400 font-semibold">
                          +{r.profitPerScu.toLocaleString('de-DE')} aUEC
                        </div>
                      </div>

                      <div className="text-right">
                        <div className="text-[10px] text-slate-500">Reingewinn (ROI):</div>
                        <div className="text-emerald-400 font-bold text-sm">
                          +{r.totalProfitAuec.toLocaleString('de-DE')} aUEC
                          <span className="text-[10px] text-emerald-500 font-normal ml-1">({r.roiPercent}%)</span>
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

      {/* ══ TAB 2: SALVAGE & SCHROTT-BESTPREISE ══ */}
      {activeTab === 'salvage' && (
        <div className="flex-1 flex flex-col space-y-3 overflow-y-auto pr-1">
          <div className="sc-glass rounded-lg p-3.5 border border-amber-500/30 bg-amber-950/10 sc-hud-corner font-mono text-xs">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2 text-amber-300 font-bold uppercase tracking-wider">
                <Wrench className="w-4 h-4 text-amber-400" />
                UEXcorp Salvage- & Rohstoff-Bestpreise
              </div>
              <span className="text-[10px] text-slate-400">Übersicht der lukrativsten Ankaufstationen</span>
            </div>
            <p className="text-slate-400 text-[11px] mt-1">
              Zeigt die aktuellen Höchstpreise für Bergungsgüter (RMC, Baustoffe, Schrott) und wertvolle Roherze in Stanton und Pyro.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3 font-mono">
            {salvagePrices.map((s) => (
              <div
                key={s.materialName}
                className="sc-glass rounded-lg p-3 border border-cyan-950/80 bg-[#040914]/90 flex flex-col justify-between hover:border-amber-500/40 transition sc-hud-corner"
              >
                <div>
                  <div className="flex items-center justify-between">
                    <span className="font-bold text-slate-100 text-xs">{s.materialName}</span>
                    <span className="text-[9px] px-1.5 py-0.5 rounded bg-cyan-950/60 border border-cyan-900 text-cyan-300 uppercase">
                      {s.system}
                    </span>
                  </div>
                  <div className="text-[10px] text-slate-500 mt-0.5">{s.category}</div>

                  <div className="mt-3 p-2 rounded bg-[#030814] border border-cyan-950">
                    <div className="text-[10px] text-slate-500 uppercase">Bester Verkaufsort:</div>
                    <div className="text-slate-200 font-bold text-xs truncate mt-0.5" title={s.bestSellLocation}>
                      {s.bestSellLocation}
                    </div>
                  </div>
                </div>

                <div className="mt-3 pt-2 border-t border-cyan-950 flex items-baseline justify-between">
                  <div>
                    <div className="text-[10px] text-slate-500">Ø Marktpreis:</div>
                    <div className="text-slate-400 text-xs">{s.avgSellPricePerScu.toLocaleString('de-DE')} aUEC</div>
                  </div>
                  <div className="text-right">
                    <div className="text-[10px] text-emerald-500 font-semibold">Höchstpreis:</div>
                    <div className="text-emerald-400 font-bold text-sm">
                      {s.bestSellPricePerScu.toLocaleString('de-DE')} aUEC <span className="text-[10px] text-slate-500 font-normal">/ SCU</span>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ══ TAB 3: WARENKATALOG & EINZELKALKULATOR ══ */}
      {activeTab === 'calculator' && (
        <div className="flex-1 flex flex-col space-y-3 overflow-y-auto pr-1">
          {/* Kalkulator Header */}
          <div className="sc-glass rounded-lg p-3.5 border border-cyan-950/80 bg-[#040914]/90 sc-hud-corner space-y-3">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="flex items-center gap-2">
                <ShoppingBag className="w-4 h-4 text-cyan-400" />
                <div>
                  <h2 className="text-xs font-bold text-slate-100 uppercase tracking-wider font-mono">
                    Einzelwaren-Kalkulator
                  </h2>
                  <div className="text-[10px] text-slate-400 font-mono">
                    Wähle ein Handelsgut zur manuellen Frachtberechnung für {cargoScu} SCU Laderaum
                  </div>
                </div>
              </div>

              {/* Schiffs-Preset Auswahl im Rechner */}
              <div className="flex items-center gap-1">
                {shipPresets.map((s) => (
                  <button
                    key={s.label}
                    onClick={() => setCargoScu(s.scu)}
                    className={`px-2 py-0.5 text-[10px] font-mono rounded transition cursor-pointer border ${
                      cargoScu === s.scu
                        ? 'bg-cyan-500/20 text-cyan-300 border-cyan-500/60 font-bold'
                        : 'bg-[#030814] border-cyan-950 text-slate-400 hover:text-slate-200'
                    }`}
                  >
                    {s.label}
                  </button>
                ))}
              </div>
            </div>

            {/* Ausgewählte Ware - Live Dashboard */}
            {selectedCommodity && (
              <div className="grid grid-cols-1 md:grid-cols-4 gap-3 pt-2 border-t border-cyan-950/80 font-mono text-xs">
                <div className="p-2.5 rounded bg-[#030814] border border-cyan-950">
                  <div className="text-slate-500 text-[10px] uppercase">WARE & PREISE</div>
                  <div className="text-sm font-bold text-slate-100 mt-0.5">{selectedCommodity.name}</div>
                  <div className="text-slate-400 text-[10px] mt-0.5">
                    Kauf: {selectedCommodity.avgBuyPrice * 100} · Verk: {selectedCommodity.avgSellPrice * 100} aUEC/SCU
                  </div>
                </div>

                <div className="p-2.5 rounded bg-[#030814] border border-cyan-950">
                  <div className="text-slate-500 text-[10px] uppercase">INVESTITION ({cargoScu} SCU)</div>
                  <div className="text-sm font-bold text-slate-200 mt-0.5">
                    {calculation.investment.toLocaleString('de-DE')} aUEC
                  </div>
                  <div className="text-slate-400 text-[10px] mt-0.5">Benötigtes Kapital</div>
                </div>

                <div className="p-2.5 rounded bg-[#030814] border border-cyan-950">
                  <div className="text-slate-500 text-[10px] uppercase">GESAMTERLÖS</div>
                  <div className="text-sm font-bold text-cyan-300 mt-0.5">
                    {calculation.revenue.toLocaleString('de-DE')} aUEC
                  </div>
                  <div className="text-slate-400 text-[10px] mt-0.5">Brutto-Verkaufserlös</div>
                </div>

                <div className="p-2.5 rounded bg-emerald-950/20 border border-emerald-500/40">
                  <div className="text-emerald-400 text-[10px] font-bold uppercase">REINGEWINN & RENDITE</div>
                  <div className="text-sm font-bold text-emerald-300 mt-0.5">
                    +{calculation.profit.toLocaleString('de-DE')} aUEC
                  </div>
                  <div className="text-emerald-400 text-[10px] mt-0.5 font-bold">{calculation.roi}% ROI</div>
                </div>
              </div>
            )}
          </div>

          {/* Suchleiste */}
          <div className="relative">
            <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-500" />
            <input
              type="text"
              placeholder="Handelsware nach Name, Kategorie oder Station filtern..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="bg-[#040914] border border-cyan-950 rounded pl-8 pr-3 py-1.5 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-cyan-500 w-full font-mono"
            />
          </div>

          {/* Waren-Kacheln */}
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3">
            {filteredCommodities.map((c) => (
              <div
                key={c.name}
                onClick={() => setSelectedCommodityName(c.name)}
                className={`sc-glass rounded-lg p-3 border transition cursor-pointer flex flex-col justify-between ${
                  selectedCommodityName === c.name
                    ? 'border-cyan-500 bg-cyan-950/30 shadow-[0_0_12px_rgba(0,240,255,0.15)]'
                    : 'border-cyan-950/80 bg-[#040914]/90 hover:border-cyan-800'
                }`}
              >
                <div>
                  <div className="flex items-center justify-between">
                    <span className="font-bold text-slate-100 text-xs font-mono">{c.name}</span>
                    <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-[#030814] border border-cyan-950 text-slate-400">
                      {c.category}
                    </span>
                  </div>

                  <div className="mt-2 text-xs font-mono space-y-1">
                    <div className="flex justify-between text-[11px]">
                      <span className="text-slate-500">Kaufort:</span>
                      <span className="text-slate-300 truncate max-w-[180px]" title={c.bestBuyLocation}>{c.bestBuyLocation}</span>
                    </div>
                    <div className="flex justify-between text-[11px]">
                      <span className="text-slate-500">Verkaufort:</span>
                      <span className="text-slate-300 truncate max-w-[180px]" title={c.bestSellLocation}>{c.bestSellLocation}</span>
                    </div>
                  </div>
                </div>

                <div className="mt-3 pt-2 border-t border-cyan-950/80 flex items-center justify-between text-xs font-mono">
                  <span className="text-slate-400 text-[10px]">Marge: +{(c.margin * 100).toLocaleString('de-DE')} / SCU</span>
                  <span className="text-emerald-400 font-bold text-xs">
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
export default MarketView;
