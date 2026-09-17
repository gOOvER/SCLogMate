import React, { useEffect, useState, useMemo } from 'react';
import {
  bridge,
  MarketCommodityDto,
  TradeRouteDto,
  SalvagePriceSummaryDto,
  AutoLoadEntryDto,
  ContainerPlanDto,
} from '../services/photinoBridge';
import {
  TrendingUp,
  ShoppingBag,
  Wrench,
  Search,
  ArrowRight,
  Ship,
  Clock,
  PackageCheck,
  Trash2,
  Box,
  Layers,
  AlertTriangle,
  CheckCircle2,
  Info,
} from 'lucide-react';

export const MarketView: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'routes' | 'containers' | 'salvage' | 'calculator'>('routes');
  const [commodities, setCommodities] = useState<MarketCommodityDto[]>([]);
  const [tradeRoutes, setTradeRoutes] = useState<TradeRouteDto[]>([]);
  const [salvagePrices, setSalvagePrices] = useState<SalvagePriceSummaryDto[]>([]);
  const [autoLoads, setAutoLoads] = useState<AutoLoadEntryDto[]>([]);
  const [isLoadingRoutes, setIsLoadingRoutes] = useState<boolean>(false);

  // Container Planner State
  const [containerTargetScu, setContainerTargetScu] = useState<number>(696);
  const [shipMaxGridScu, setShipMaxGridScu] = useState<number>(32);
  const [kioskPreset, setKioskPreset] = useState<string>('all');
  const [customSizes, setCustomSizes] = useState<string>('1,2,4,8,16,24,32');
  const [containerPlan, setContainerPlan] = useState<ContainerPlanDto | null>(null);
  const [isPlanningContainers, setIsPlanningContainers] = useState<boolean>(false);

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

  const fetchAutoLoads = async () => {
    try {
      const res = await bridge.sendRequest<AutoLoadEntryDto[]>('get_autoload_entries');
      setAutoLoads(res || []);
    } catch (err) {
      console.error('Failed to load autoload entries:', err);
    }
  };

  const handleDiscardAutoLoad = async (id: string) => {
    try {
      const res = await bridge.sendRequest<AutoLoadEntryDto[]>('discard_autoload_entry', { id });
      setAutoLoads(res || []);
    } catch (err) {
      console.error('Failed to discard autoload entry:', err);
    }
  };

  const formatTimeRemaining = (seconds: number) => {
    if (seconds <= 0) return 'Fertig verladen!';
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
  };

  const fetchContainerPlan = async (target: number, maxScu: number, sizes: string) => {
    try {
      setIsPlanningContainers(true);
      const res = await bridge.sendRequest<ContainerPlanDto>('plan_containers', {
        targetScu: target,
        shipMaxContainerScu: maxScu,
        containerSizes: sizes,
      });
      setContainerPlan(res || null);
    } catch (err) {
      console.error('Failed to plan containers:', err);
    } finally {
      setIsPlanningContainers(false);
    }
  };

  const applyKioskPreset = (preset: string) => {
    setKioskPreset(preset);
    let sizes = '1,2,4,8,16,24,32';
    if (preset === 'mining') sizes = '1,2,4,8';
    else if (preset === 'scrap') sizes = '8,16,24,32';
    else if (preset === 'distro') sizes = '16,24,32';
    else if (preset === 'heavy') sizes = '24,32';
    setCustomSizes(sizes);
  };

  useEffect(() => {
    fetchMarket();
    fetchSmartRoutes();
    fetchAutoLoads();

    const unbindAutoLoad = bridge.on<AutoLoadEntryDto[]>('AUTOLOAD_UPDATED', (entries) => {
      if (Array.isArray(entries)) {
        setAutoLoads(entries);
      }
    });

    const unbindAutoLoadCompleted = bridge.on('AUTOLOAD_COMPLETED', () => {
      fetchAutoLoads();
    });

    const interval = setInterval(() => {
      setAutoLoads((prev) => {
        if (prev.length === 0) return prev;
        return prev.map((e) => {
          const rem = Math.max(0, e.remainingSeconds - 1);
          const el = e.elapsedSeconds + 1;
          const pred = e.predictedSeconds || 1;
          const pct = Math.min(100, Math.round((el / pred) * 100));
          return {
            ...e,
            remainingSeconds: rem,
            elapsedSeconds: el,
            progressPercent: pct,
          };
        });
      });
    }, 1000);

    return () => {
      unbindAutoLoad();
      unbindAutoLoadCompleted();
      clearInterval(interval);
    };
  }, []);

  useEffect(() => {
    fetchSmartRoutes();
  }, [cargoScu, maxBudgetAuec, routeSystemFilter]);

  useEffect(() => {
    fetchContainerPlan(containerTargetScu, shipMaxGridScu, customSizes);
  }, [containerTargetScu, shipMaxGridScu, customSizes]);

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
      {/* ══ Auto-Load Frachtaufzug Live-Timer ══ */}
      {autoLoads.length > 0 && (
        <div className="flex flex-col space-y-2 p-3 rounded-lg border border-cyan-500/40 bg-[#030914]/90 shadow-[0_0_15px_rgba(6,182,212,0.15)] animate-in slide-in-from-top-2">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2 text-xs font-mono font-bold text-cyan-300">
              <Clock className="w-4 h-4 text-cyan-400 animate-pulse" />
              <span>Laufende Frachtaufzug-Verladungen (Auto-Load)</span>
              <span className="px-1.5 py-0.5 rounded text-[10px] bg-cyan-900/60 text-cyan-200 border border-cyan-800/60">
                {autoLoads.length} aktiv
              </span>
            </div>
            <span className="text-[11px] font-mono text-slate-400">
              Star Citizen Frachtaufzug Timer
            </span>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-2.5 pt-1">
            {autoLoads.map((al) => {
              const isFinished = al.remainingSeconds <= 0;
              return (
                <div
                  key={al.id}
                  className="flex flex-col justify-between p-2.5 rounded bg-[#02050c] border border-cyan-900/50 hover:border-cyan-700/60 transition space-y-2"
                >
                  <div className="flex items-start justify-between gap-2">
                    <div className="min-w-0">
                      <div className="flex items-center gap-1.5 text-xs font-mono font-bold text-white truncate">
                        <Box className="w-3.5 h-3.5 text-amber-400 shrink-0" />
                        <span className="truncate">{al.commodityName}</span>
                        <span className="text-[10px] px-1.5 py-0.2 rounded bg-slate-800 text-slate-300 font-normal">
                          {al.kind}
                        </span>
                      </div>
                      <div className="text-[11px] font-mono text-slate-400 truncate mt-0.5">
                        {al.shopName} · {al.totalScu} SCU ({al.boxCount} Kisten)
                      </div>
                    </div>
                    <button
                      onClick={() => handleDiscardAutoLoad(al.id)}
                      title="Timer verwerfen"
                      className="text-slate-500 hover:text-rose-400 p-1 rounded transition cursor-pointer"
                    >
                      <Trash2 className="w-3.5 h-3.5" />
                    </button>
                  </div>

                  {/* Progress Bar */}
                  <div className="space-y-1">
                    <div className="flex items-center justify-between text-[11px] font-mono">
                      <span className={isFinished ? 'text-emerald-400 font-bold flex items-center gap-1' : 'text-cyan-300'}>
                        {isFinished ? (
                          <>
                            <PackageCheck className="w-3 h-3 text-emerald-400" />
                            Abholbereit!
                          </>
                        ) : (
                          `${formatTimeRemaining(al.remainingSeconds)} verbleibend`
                        )}
                      </span>
                      <span className="text-slate-400">{Math.round(al.progressPercent)}%</span>
                    </div>
                    <div className="w-full h-1.5 bg-slate-900 rounded-full overflow-hidden border border-cyan-950">
                      <div
                        className={`h-full transition-all duration-500 ${
                          isFinished
                            ? 'bg-emerald-400 shadow-[0_0_8px_#34d399]'
                            : 'bg-gradient-to-r from-cyan-500 to-emerald-400 shadow-[0_0_8px_rgba(6,182,212,0.5)]'
                        }`}
                        style={{ width: `${Math.min(100, Math.max(0, al.progressPercent))}%` }}
                      />
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}

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
          onClick={() => setActiveTab('containers')}
          className={`flex items-center gap-2 px-3.5 py-1.5 text-xs font-mono font-bold rounded transition cursor-pointer ${
            activeTab === 'containers'
              ? 'bg-cyan-950/80 text-cyan-300 border border-cyan-500/70 shadow-[0_0_8px_rgba(6,182,212,0.25)]'
              : 'text-slate-400 hover:text-slate-200 border border-transparent'
          }`}
        >
          <Layers className="w-3.5 h-3.5 text-cyan-400" />
          <span>Container-Planer</span>
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

      {/* ══ TAB 2: CONTAINER PLANER & KISTENOPTIMIERUNG ══ */}
      {activeTab === 'containers' && (
        <div className="flex-1 flex flex-col space-y-3 overflow-y-auto pr-1 font-mono">
          {/* Header Banner */}
          <div className="sc-glass rounded-lg p-3.5 border border-cyan-950/80 bg-[#040914]/90 sc-hud-corner space-y-3">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="flex items-center gap-2">
                <Layers className="w-4 h-4 text-cyan-400" />
                <div>
                  <div className="flex items-center gap-2">
                    <h2 className="text-xs font-bold text-slate-100 uppercase tracking-wider">
                      Container-Planer & Kistengrößen-Optimierung
                    </h2>
                    {isPlanningContainers && (
                      <span className="text-[10px] text-cyan-400 animate-pulse font-normal">
                        Berechne...
                      </span>
                    )}
                  </div>
                  <div className="text-[10px] text-slate-400">
                    Berechnet die ideale Kistenaufteilung, minimiert Ladezeiten und ermittelt Star Citizen Frachtaufzug-Gebühren (Bounded DP)
                  </div>
                </div>
              </div>

              {/* Schnellauswahl Schiffe */}
              <div className="flex items-center gap-1 flex-wrap">
                <span className="text-[10px] text-slate-500 mr-1 flex items-center gap-1">
                  <Ship className="w-3 h-3 text-cyan-400" /> Schiff:
                </span>
                {[
                  { label: 'C2', scu: 696, maxBox: 32 },
                  { label: 'Cat', scu: 576, maxBox: 32 },
                  { label: 'Taurus', scu: 174, maxBox: 32 },
                  { label: 'MAX', scu: 120, maxBox: 32 },
                  { label: 'Cutlass', scu: 46, maxBox: 16 },
                  { label: 'Hull A', scu: 64, maxBox: 16 },
                  { label: 'Hull C', scu: 4608, maxBox: 32 },
                ].map((s) => (
                  <button
                    key={s.label}
                    onClick={() => {
                      setContainerTargetScu(s.scu);
                      setShipMaxGridScu(s.maxBox);
                    }}
                    className={`px-2 py-0.5 text-[10px] rounded transition cursor-pointer border ${
                      containerTargetScu === s.scu && shipMaxGridScu === s.maxBox
                        ? 'bg-cyan-500/20 text-cyan-300 border-cyan-500/60 font-bold shadow-[0_0_8px_rgba(6,182,212,0.2)]'
                        : 'bg-[#030814] border-cyan-950 text-slate-400 hover:text-slate-200'
                    }`}
                  >
                    {s.label} ({s.scu})
                  </button>
                ))}
              </div>
            </div>

            {/* Steuerung & Filter */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-3 pt-2 border-t border-cyan-950/80 text-xs">
              {/* 1. Zielkapazität (Target SCU) */}
              <div className="p-2.5 rounded bg-[#030814] border border-cyan-950 space-y-1.5">
                <div className="flex items-center justify-between">
                  <span className="text-slate-500 text-[10px] uppercase font-semibold">Zielvolumen (SCU)</span>
                  <span className="text-cyan-400 font-bold text-xs">{containerTargetScu} SCU</span>
                </div>
                <div className="flex items-center gap-2">
                  <input
                    type="number"
                    min="1"
                    max="10000"
                    value={containerTargetScu}
                    onChange={(e) => setContainerTargetScu(Math.max(1, parseInt(e.target.value, 10) || 1))}
                    className="bg-[#02050c] border border-cyan-900/60 rounded px-2 py-1 text-slate-100 text-xs w-24 font-bold focus:outline-none focus:border-cyan-400"
                  />
                  <input
                    type="range"
                    min="1"
                    max="1000"
                    value={Math.min(1000, containerTargetScu)}
                    onChange={(e) => setContainerTargetScu(parseInt(e.target.value, 10))}
                    className="w-full accent-cyan-400 cursor-pointer"
                  />
                </div>
              </div>

              {/* 2. Maximales Kistengitter des Schiffs */}
              <div className="p-2.5 rounded bg-[#030814] border border-cyan-950 space-y-1.5">
                <div className="flex items-center justify-between">
                  <span className="text-slate-500 text-[10px] uppercase font-semibold">Schiffs-Gitterlimit</span>
                  <span className="text-slate-300 font-bold text-xs">Max. {shipMaxGridScu} SCU Kiste</span>
                </div>
                <div className="flex items-center gap-1">
                  {[2, 8, 16, 24, 32].map((sz) => (
                    <button
                      key={sz}
                      onClick={() => setShipMaxGridScu(sz)}
                      className={`flex-1 py-1 text-[10px] rounded transition cursor-pointer border ${
                        shipMaxGridScu === sz
                          ? 'bg-cyan-900/50 text-cyan-300 border-cyan-500 font-bold'
                          : 'bg-[#02050c] border-cyan-950 text-slate-400 hover:text-white'
                      }`}
                    >
                      {sz} SCU
                    </button>
                  ))}
                </div>
              </div>

              {/* 3. Kiosk Terminal Crate Menu */}
              <div className="p-2.5 rounded bg-[#030814] border border-cyan-950 space-y-1.5">
                <div className="flex items-center justify-between">
                  <span className="text-slate-500 text-[10px] uppercase font-semibold">Terminal-Angebot</span>
                  <span className="text-cyan-400 text-[10px] truncate max-w-[120px]" title={customSizes}>
                    [{customSizes}]
                  </span>
                </div>
                <div className="flex items-center gap-1 flex-wrap">
                  {[
                    { id: 'all', label: 'Alle (1–32)' },
                    { id: 'mining', label: 'Minen (1–8)' },
                    { id: 'scrap', label: 'Schrott (8–32)' },
                    { id: 'distro', label: 'Distro (16–32)' },
                  ].map((kp) => (
                    <button
                      key={kp.id}
                      onClick={() => applyKioskPreset(kp.id)}
                      className={`px-2 py-0.5 text-[10px] rounded transition cursor-pointer border ${
                        kioskPreset === kp.id
                          ? 'bg-cyan-900/50 text-cyan-300 border-cyan-500 font-bold'
                          : 'bg-[#02050c] border-cyan-950 text-slate-400 hover:text-white'
                      }`}
                    >
                      {kp.label}
                    </button>
                  ))}
                </div>
              </div>
            </div>
          </div>

          {/* ══ KPI / Ergebnis-Karten ══ */}
          {containerPlan && (
            <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
              {/* Ladevolumen & Erfüllungsgrad */}
              <div className="sc-glass rounded-lg p-3 border border-cyan-950/80 bg-[#040914]/90 sc-hud-corner space-y-1">
                <div className="flex items-center justify-between text-slate-500 text-[10px] uppercase">
                  <span>Geladenes Volumen</span>
                  {containerPlan.hitsTarget ? (
                    <span className="text-emerald-400 flex items-center gap-1 text-[9px] font-bold">
                      <CheckCircle2 className="w-3 h-3" /> 100% Exakt
                    </span>
                  ) : (
                    <span className="text-amber-400 flex items-center gap-1 text-[9px] font-bold">
                      <AlertTriangle className="w-3 h-3" /> Shortfall: {containerPlan.shortfallScu} SCU
                    </span>
                  )}
                </div>
                <div className="text-lg font-bold text-slate-100 flex items-baseline gap-1">
                  <span>{containerPlan.totalScu}</span>
                  <span className="text-xs text-slate-400 font-normal">/ {containerPlan.targetScu} SCU</span>
                </div>
                <div className="w-full h-1 bg-slate-900 rounded-full overflow-hidden">
                  <div
                    className={`h-full ${
                      containerPlan.hitsTarget ? 'bg-emerald-400' : 'bg-amber-400'
                    }`}
                    style={{ width: `${Math.min(100, (containerPlan.totalScu / containerPlan.targetScu) * 100)}%` }}
                  />
                </div>
              </div>

              {/* Minimale Kistenanzahl */}
              <div className="sc-glass rounded-lg p-3 border border-cyan-950/80 bg-[#040914]/90 sc-hud-corner space-y-1">
                <div className="text-slate-500 text-[10px] uppercase">Optimale Kistenanzahl</div>
                <div className="text-lg font-bold text-cyan-300 flex items-baseline gap-1.5">
                  <Box className="w-4 h-4 text-cyan-400 inline" />
                  <span>{containerPlan.totalBoxCount} Kisten</span>
                </div>
                <div className="text-[10px] text-slate-400">
                  Minimal mögliche Anzahl (reduziert Gebühr & Handling)
                </div>
              </div>

              {/* Frachtaufzug-Gebühr */}
              <div className="sc-glass rounded-lg p-3 border border-cyan-950/80 bg-[#040914]/90 sc-hud-corner space-y-1">
                <div className="text-slate-500 text-[10px] uppercase">Auto-Load Gebühr</div>
                <div className="text-lg font-bold text-amber-300">
                  {containerPlan.totalAutoLoadFee.toLocaleString('de-DE')} aUEC
                </div>
                <div className="text-[10px] text-slate-400">
                  Staffelpreise: 1 SCU (30) bis 32 SCU (680 aUEC)
                </div>
              </div>

              {/* Geschätzte Verladezeit */}
              <div className="sc-glass rounded-lg p-3 border border-cyan-950/80 bg-[#040914]/90 sc-hud-corner space-y-1">
                <div className="text-slate-500 text-[10px] uppercase">Geschätzte Verladezeit</div>
                <div className="text-lg font-bold text-emerald-300 flex items-baseline gap-1.5">
                  <Clock className="w-4 h-4 text-emerald-400 inline" />
                  <span>
                    {Math.floor(containerPlan.totalEstimatedSeconds / 60)}m {containerPlan.totalEstimatedSeconds % 60}s
                  </span>
                </div>
                <div className="text-[10px] text-slate-400">
                  72s Dispatch + ~{Math.round(containerPlan.totalEstimatedSeconds - 72)}s Kistenhandling
                </div>
              </div>
            </div>
          )}

          {/* ══ Hinweise & Warnungen ══ */}
          {containerPlan && containerPlan.shortfallScu > 0 && (
            <div className="p-3 rounded bg-amber-950/20 border border-amber-500/40 flex items-start gap-2 text-xs">
              <AlertTriangle className="w-4 h-4 text-amber-400 shrink-0 mt-0.5" />
              <div>
                <div className="font-bold text-amber-300">
                  Kapazitätslücke (Shortfall): {containerPlan.shortfallScu} SCU können mit diesem Terminal-Menü nicht geladen werden
                </div>
                <div className="text-[11px] text-slate-300 mt-0.5">
                  Die kleinste am Terminal verfügbare Kiste beträgt {containerPlan.minContainerScu} SCU. Um Überfüllung zu vermeiden, werden exakt {containerPlan.totalScu} SCU gekauft und {containerPlan.shortfallScu} SCU Frachtraum verbleiben leer.
                </div>
              </div>
            </div>
          )}

          {containerPlan && containerPlan.maxContainerScu < shipMaxGridScu && (
            <div className="p-3 rounded bg-cyan-950/20 border border-cyan-500/40 flex items-start gap-2 text-xs">
              <Info className="w-4 h-4 text-cyan-400 shrink-0 mt-0.5" />
              <div>
                <div className="font-bold text-cyan-300">
                  Terminal-Limitierung: Kiosk bietet maximal {containerPlan.maxContainerScu} SCU Kisten
                </div>
                <div className="text-[11px] text-slate-300 mt-0.5">
                  Dein Schiff unterstützt Kistengrößen bis zu {shipMaxGridScu} SCU. Da das Terminal nur bis zu {containerPlan.maxContainerScu} SCU anbietet, sind mehr Einzelkisten erforderlich, was die Verladezeit und Gebühr leicht erhöht.
                </div>
              </div>
            </div>
          )}

          {/* ══ Kisten-Aufschlüsselung (Crate Picks) ══ */}
          {containerPlan && containerPlan.picks.length > 0 ? (
            <div className="space-y-2">
              <div className="flex items-center justify-between text-xs text-slate-300">
                <span className="font-bold uppercase tracking-wider flex items-center gap-1.5">
                  <Box className="w-4 h-4 text-cyan-400" />
                  Optimale Kistenauswahl ({containerPlan.picks.length} Posten)
                </span>
                <span className="text-[11px] text-slate-500">
                  Kiosk-Einkaufsliste für Frachtterminals
                </span>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
                {containerPlan.picks.map((pick) => {
                  const pct = Math.round(((pick.scu * pick.count) / containerPlan.totalScu) * 100);
                  return (
                    <div
                      key={pick.scu}
                      className="sc-glass rounded-lg p-3.5 border border-cyan-900/60 bg-[#040914]/90 hover:border-cyan-500/60 transition sc-hud-corner flex flex-col justify-between space-y-2.5"
                    >
                      <div className="flex items-start justify-between">
                        <div className="flex items-center gap-2">
                          <div className="w-10 h-10 rounded bg-cyan-950/80 border border-cyan-500/40 flex flex-col items-center justify-center font-bold text-cyan-300">
                            <span className="text-sm leading-none">{pick.scu}</span>
                            <span className="text-[8px] text-slate-400 leading-none">SCU</span>
                          </div>
                          <div>
                            <div className="text-sm font-bold text-white">
                              {pick.count} × {pick.scu} SCU Container
                            </div>
                            <div className="text-[10px] text-cyan-400 font-semibold">
                              = {pick.scu * pick.count} SCU ({pct}% der Ladung)
                            </div>
                          </div>
                        </div>

                        <span className="px-2 py-0.5 rounded text-[10px] bg-slate-800/80 border border-slate-700 text-slate-300 font-bold">
                          {pick.count} Stück
                        </span>
                      </div>

                      {/* Details: Gebühr und Dauer */}
                      <div className="p-2 rounded bg-[#02050c] border border-cyan-950 grid grid-cols-2 gap-2 text-xs">
                        <div>
                          <div className="text-[9px] text-slate-500 uppercase">Auto-Load Gebühr</div>
                          <div className="text-slate-200 font-bold mt-0.5">
                            {pick.totalFee.toLocaleString('de-DE')} aUEC
                          </div>
                          <div className="text-[9px] text-slate-400">
                            {pick.unitFee} aUEC / Kiste
                          </div>
                        </div>

                        <div className="text-right">
                          <div className="text-[9px] text-slate-500 uppercase">Handlingdauer</div>
                          <div className="text-emerald-400 font-bold mt-0.5">
                            {pick.totalTimeSeconds.toFixed(1)} s
                          </div>
                          <div className="text-[9px] text-slate-400">
                            {pick.unitTimeSeconds}s / Kiste
                          </div>
                        </div>
                      </div>

                      {/* Visual Load Share Bar */}
                      <div className="w-full h-1 bg-slate-900 rounded-full overflow-hidden">
                        <div
                          className="h-full bg-gradient-to-r from-cyan-500 to-emerald-400"
                          style={{ width: `${pct}%` }}
                        />
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          ) : (
            <div className="sc-glass rounded-lg p-10 text-center text-slate-400 text-xs border border-cyan-950">
              Keine Kistenkombination für die gewählten Parameter möglich.
            </div>
          )}
        </div>
      )}

      {/* ══ TAB 3: SALVAGE & SCHROTT-BESTPREISE ══ */}
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
