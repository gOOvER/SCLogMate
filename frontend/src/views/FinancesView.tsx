import React, { useEffect, useState, useMemo } from 'react';
import { bridge, FinanceOverviewDto } from '../services/photinoBridge';
import {
  Coins,
  CreditCard,
  Package,
  RefreshCw,
  TrendingDown,
  TrendingUp,
  Search,
  Copy,
  Check,
  Fuel,
  Wrench,
  Rocket,
  Shield,
  ShoppingCart,
  Scale,
  Send,
  HeartPulse,
} from 'lucide-react';

export const FinancesView: React.FC = () => {
  const [data, setData] = useState<FinanceOverviewDto | null>(null);
  const [activeSubTab, setActiveSubTab] = useState<'overview' | 'ledger' | 'spending' | 'cargo'>('overview');
  const [loading, setLoading] = useState<boolean>(false);
  const [search, setSearch] = useState<string>('');
  const [copiedDiscord, setCopiedDiscord] = useState<boolean>(false);

  // Chart Controls
  const [chartMode, setChartMode] = useState<'cumulative' | 'delta'>('cumulative');

  const fetchFinance = async () => {
    try {
      setLoading(true);
      const res = await bridge.sendRequest<FinanceOverviewDto>('get_finance');
      setData(res);
    } catch (err) {
      console.error('Failed to load finance data:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchFinance();
    const unbind = bridge.on<FinanceOverviewDto>('finance_response', (newData) => {
      setData(newData);
    });
    return () => unbind();
  }, []);

  const formatNumber = (num?: number | null) => {
    if (num === undefined || num === null) return '0';
    return new Intl.NumberFormat('de-DE').format(num);
  };

  const handleCopyDiscord = () => {
    if (!data) return;
    const income = formatNumber(data.totalIncome);
    const spend = formatNumber(data.totalSpend);
    const net = formatNumber(data.totalNet);
    const sign = data.totalNet >= 0 ? '+' : '';
    const text = `**📊 SCLogMate Finanzbericht**
> ↗ Einnahmen: \`${income} aUEC\`
> ↘ Ausgaben: \`${spend} aUEC\`
> 💰 Bilanz: \`${sign}${net} aUEC\` (${data.profitMargin}% Marge)
> 📦 Handelsvolumen: \`${formatNumber(data.totalCargoAuec)} aUEC\``;

    navigator.clipboard.writeText(text);
    setCopiedDiscord(true);
    setTimeout(() => setCopiedDiscord(false), 2500);
  };

  // Filtered ledger
  const filteredLedger = useMemo(() => {
    return (data?.ledger || []).filter((item) => {
      if (!search) return true;
      const s = search.toLowerCase();
      return (
        (item.title && item.title.toLowerCase().includes(s)) ||
        (item.description && item.description.toLowerCase().includes(s)) ||
        (item.ship && item.ship.toLowerCase().includes(s))
      );
    });
  }, [data?.ledger, search]);

  // Spending categories aggregation
  const spendingCategories = useMemo(() => {
    if (!data?.ledger) return [];
    const catMap: Record<string, { label: string; icon: any; amount: number; count: number; color: string }> = {
      fuel: { label: 'Treibstoff (Hydrogen/QT)', icon: Fuel, amount: 0, count: 0, color: 'text-amber-400 bg-amber-500' },
      repair: { label: 'Reparatur & Service', icon: Wrench, amount: 0, count: 0, color: 'text-orange-400 bg-orange-500' },
      ship: { label: 'Schiffe (Kauf/Miete/Claim)', icon: Rocket, amount: 0, count: 0, color: 'text-sky-400 bg-sky-500' },
      gear: { label: 'Waffen & Ausrüstung', icon: Shield, amount: 0, count: 0, color: 'text-purple-400 bg-purple-500' },
      cargo: { label: 'Fracht- & Rohstoffeinkauf', icon: ShoppingCart, amount: 0, count: 0, color: 'text-emerald-400 bg-emerald-500' },
      fine: { label: 'Strafen & Bußgelder', icon: Scale, amount: 0, count: 0, color: 'text-rose-400 bg-rose-500' },
      transfer: { label: 'Überweisungen', icon: Send, amount: 0, count: 0, color: 'text-cyan-400 bg-cyan-500' },
      med: { label: 'MedBed & Behandlung', icon: HeartPulse, amount: 0, count: 0, color: 'text-red-400 bg-red-500' },
    };

    let totalNegative = 0;
    for (const item of data.ledger) {
      if (item.amount && item.amount < 0) {
        const amt = Math.abs(item.amount);
        totalNegative += amt;
        const text = (item.title + ' ' + item.description).toLowerCase();
        if (text.includes('fuel') || text.includes('tanken') || text.includes('treibstoff')) {
          catMap.fuel.amount += amt;
          catMap.fuel.count++;
        } else if (text.includes('repair') || text.includes('reparatur') || text.includes('restock') || text.includes('service')) {
          catMap.repair.amount += amt;
          catMap.repair.count++;
        } else if (text.includes('claim') || text.includes('insurance') || text.includes('versicherung') || text.includes('ship')) {
          catMap.ship.amount += amt;
          catMap.ship.count++;
        } else if (text.includes('armor') || text.includes('weapon') || text.includes('waffe') || text.includes('rüst')) {
          catMap.gear.amount += amt;
          catMap.gear.count++;
        } else if (text.includes('trade') || text.includes('kauf') || text.includes('waren')) {
          catMap.cargo.amount += amt;
          catMap.cargo.count++;
        } else if (text.includes('fine') || text.includes('strafe')) {
          catMap.fine.amount += amt;
          catMap.fine.count++;
        } else if (text.includes('transfer') || text.includes('überweisung')) {
          catMap.transfer.amount += amt;
          catMap.transfer.count++;
        } else {
          catMap.cargo.amount += amt;
          catMap.cargo.count++;
        }
      }
    }

    return Object.values(catMap)
      .filter((c) => c.amount > 0)
      .map((c) => ({
        ...c,
        percent: totalNegative > 0 ? Math.round((c.amount / totalNegative) * 100) : 0,
      }))
      .sort((a, b) => b.amount - a.amount);
  }, [data?.ledger]);

  // Render SVG Quantum Timeline from real timelinePoints
  const timelinePoints = data?.timelinePoints || [];
  const svgWidth = 800;
  const svgHeight = 160;
  const padding = 30;

  const chartCoords = useMemo(() => {
    if (timelinePoints.length === 0) return [];
    const values = timelinePoints.map((p) => (chartMode === 'cumulative' ? p.balance : p.delta));
    const minVal = Math.min(0, ...values);
    const maxVal = Math.max(1, ...values);
    const range = maxVal - minVal || 1;

    return timelinePoints.map((p, idx) => {
      const val = chartMode === 'cumulative' ? p.balance : p.delta;
      const x = padding + (idx / Math.max(1, timelinePoints.length - 1)) * (svgWidth - padding * 2);
      const y = svgHeight - padding - ((val - minVal) / range) * (svgHeight - padding * 2);
      return { x, y, val, label: p.label, time: p.time };
    });
  }, [timelinePoints, chartMode]);

  const svgPathD = useMemo(() => {
    if (chartCoords.length === 0) return '';
    return chartCoords.reduce((acc, p, i) => (i === 0 ? `M ${p.x} ${p.y}` : `${acc} L ${p.x} ${p.y}`), '');
  }, [chartCoords]);

  const svgAreaD = useMemo(() => {
    if (chartCoords.length === 0) return '';
    const last = chartCoords[chartCoords.length - 1];
    const first = chartCoords[0];
    return `${svgPathD} L ${last.x} ${svgHeight - padding} L ${first.x} ${svgHeight - padding} Z`;
  }, [chartCoords, svgPathD]);

  return (
    <div className="flex flex-col min-h-full space-y-3 font-sans select-none">
      {/* ══ 1. TOP 5 KPI SUMMARY CARDS (Exakt nach RC2) ══ */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-2.5 shrink-0">
        {/* Karte 1: Einnahmen */}
        <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-lg p-2.5 flex flex-col justify-between shadow-sm">
          <div className="flex justify-between items-start">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
              Einnahmen
            </span>
            <TrendingUp className="w-3.5 h-3.5 text-emerald-400" />
          </div>
          <div className="mt-1 text-lg font-bold font-mono text-emerald-300">
            +{formatNumber(data?.totalIncome)} <span className="text-[10px] font-normal text-emerald-500">aUEC</span>
          </div>
          <div className="text-[10px] font-mono text-slate-500 mt-0.5">
            ↗ Missionen, Handel & Erlöse
          </div>
        </div>

        {/* Karte 2: Ausgaben */}
        <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-lg p-2.5 flex flex-col justify-between shadow-sm">
          <div className="flex justify-between items-start">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
              Ausgaben
            </span>
            <TrendingDown className="w-3.5 h-3.5 text-rose-400" />
          </div>
          <div className="mt-1 text-lg font-bold font-mono text-rose-300">
            -{formatNumber(data?.totalSpend)} <span className="text-[10px] font-normal text-rose-500">aUEC</span>
          </div>
          <div className="text-[10px] font-mono text-slate-500 mt-0.5">
            ↘ Wartung, Treibstoff & Käufe
          </div>
        </div>

        {/* Karte 3: Netto-Saldo mit Gewinnmarge */}
        <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-lg p-2.5 flex flex-col justify-between shadow-sm">
          <div className="flex justify-between items-start">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
              Netto-Saldo
            </span>
            <Coins className="w-3.5 h-3.5 text-cyan-400" />
          </div>
          <div className={`mt-1 text-lg font-bold font-mono ${((data?.totalNet || 0) >= 0 ? 'text-cyan-300' : 'text-amber-400')}`}>
            {((data?.totalNet || 0) >= 0 ? '+' : '')}{formatNumber(data?.totalNet)} <span className="text-[10px] font-normal text-cyan-500">aUEC</span>
          </div>
          <div className="text-[10px] font-mono text-cyan-400 font-bold mt-0.5">
            Marge: {data?.profitMargin || 0}% Netto
          </div>
        </div>

        {/* Karte 4: Live Saldo (mobiGlas) */}
        <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-lg p-2.5 flex flex-col justify-between shadow-sm">
          <div className="flex justify-between items-start">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
              Live Saldo
            </span>
            <CreditCard className="w-3.5 h-3.5 text-cyan-400" />
          </div>
          <div className="mt-1 text-lg font-bold font-mono text-slate-100 drop-shadow-[0_0_8px_rgba(6,182,212,0.3)]">
            {formatNumber(data?.liveBalance || data?.totalNet)} <span className="text-[10px] font-normal text-slate-400">aUEC</span>
          </div>
          <div className="text-[10px] font-mono text-slate-500 mt-0.5">
            mobiGlas Konto-Erfassung
          </div>
        </div>

        {/* Karte 5: Handelsvolumen */}
        <div className="col-span-2 md:col-span-1 bg-[#051122]/90 border border-cyan-950/80 rounded-lg p-2.5 flex flex-col justify-between shadow-sm">
          <div className="flex justify-between items-start">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
              Handelsvolumen
            </span>
            <Package className="w-3.5 h-3.5 text-amber-400" />
          </div>
          <div className="mt-1 text-lg font-bold font-mono text-amber-300">
            {formatNumber(data?.totalCargoAuec || (data?.sales || 0) + (data?.trade || 0))} <span className="text-[10px] font-normal text-amber-500">aUEC</span>
          </div>
          <div className="text-[10px] font-mono text-amber-400/80 mt-0.5">
            Waren & Beute-Verkäufe
          </div>
        </div>
      </div>

      {/* ══ 2. SUBTABS NAVIGATION (Exakt 4 RC2 Subtabs) ══ */}
      <div className="flex items-center justify-between border-b border-cyan-950/80 bg-[#040914] px-3 py-1.5 shrink-0 rounded-t-lg">
        <div className="flex items-center gap-2 overflow-x-auto no-scrollbar">
          {[
            { id: 'overview', label: '📊 Übersicht & Verlauf' },
            { id: 'ledger', label: `📑 Buchhaltung (${data?.ledger.length || 0})` },
            { id: 'spending', label: `📉 Ausgaben-Analyse` },
            { id: 'cargo', label: `📦 Fracht & Handel (${data?.cargo.length || 0})` },
          ].map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveSubTab(tab.id as any)}
              className={`px-3 py-1 text-xs font-mono font-semibold rounded transition cursor-pointer shrink-0 ${
                activeSubTab === tab.id
                  ? 'bg-cyan-950/80 text-cyan-300 border border-cyan-500/70 shadow-[0_0_8px_rgba(6,182,212,0.25)]'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900/60 border border-transparent'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={handleCopyDiscord}
            title="Finanzbericht für Discord kopieren"
            className="flex items-center gap-1.5 px-2.5 py-1 rounded bg-[#071322] border border-cyan-950 hover:border-cyan-700 text-xs font-mono text-cyan-300 transition cursor-pointer"
          >
            {copiedDiscord ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5 text-cyan-400" />}
            <span>{copiedDiscord ? 'Kopiert!' : 'Discord Copy'}</span>
          </button>

          <button
            onClick={fetchFinance}
            title="Finanzdaten aktualisieren"
            className="p-1 rounded bg-[#071322] border border-cyan-950 hover:border-cyan-800 text-slate-400 hover:text-cyan-300 cursor-pointer"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin text-cyan-400' : ''}`} />
          </button>
        </div>
      </div>

      {/* ══ 3. SUBTAB INHALTE ══ */}
      <div className="flex-1 bg-[#040914]/90 rounded-b-lg border border-cyan-950/80 overflow-hidden flex flex-col p-3 shadow-sm min-h-[400px]">
        {/* SUBTAB 1: ÜBERSICHT & REAL QUANTUM TIMELINE */}
        {activeSubTab === 'overview' && (
          <div className="flex-1 overflow-y-auto space-y-3.5 pr-1">
            {/* Echte Quantum Timeline SVG Grafik */}
            <div className="bg-[#030a16] rounded-lg border border-cyan-950 p-3 relative overflow-hidden">
              <div className="flex justify-between items-center mb-2">
                <div className="flex items-center gap-2">
                  <span className="px-1.5 py-0.5 rounded text-[9px] font-mono font-bold bg-cyan-950 text-cyan-300 border border-cyan-800">
                    ❖ QUANTUM TIMELINE
                  </span>
                  <span className="text-xs font-mono font-bold text-slate-200">
                    Finanzverlauf & Kontostand-Historie
                  </span>
                </div>

                {/* Mode Selector */}
                <div className="flex items-center gap-1 bg-[#051122] p-0.5 rounded border border-cyan-950 text-[10px] font-mono">
                  <button
                    onClick={() => setChartMode('cumulative')}
                    className={`px-2 py-0.5 rounded cursor-pointer transition ${
                      chartMode === 'cumulative' ? 'bg-cyan-950 text-cyan-300 border border-cyan-700' : 'text-slate-400 hover:text-white'
                    }`}
                  >
                    Kumulativ
                  </button>
                  <button
                    onClick={() => setChartMode('delta')}
                    className={`px-2 py-0.5 rounded cursor-pointer transition ${
                      chartMode === 'delta' ? 'bg-cyan-950 text-cyan-300 border border-cyan-700' : 'text-slate-400 hover:text-white'
                    }`}
                  >
                    Einzelposten
                  </button>
                </div>
              </div>

              {chartCoords.length > 0 ? (
                <div className="relative w-full overflow-x-auto">
                  <svg viewBox={`0 0 ${svgWidth} ${svgHeight}`} className="w-full h-40 overflow-visible">
                    <defs>
                      <linearGradient id="quantGrad" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" stopColor="#06b6d4" stopOpacity="0.35" />
                        <stop offset="100%" stopColor="#06b6d4" stopOpacity="0.0" />
                      </linearGradient>
                    </defs>

                    {/* Raster-Linien */}
                    <line x1={padding} y1={svgHeight - padding} x2={svgWidth - padding} y2={svgHeight - padding} stroke="#1e293b" strokeDasharray="3,3" />
                    <line x1={padding} y1={svgHeight / 2} x2={svgWidth - padding} y2={svgHeight / 2} stroke="#0f172a" strokeDasharray="2,2" />

                    {/* Gradient Area Fill */}
                    {svgAreaD && <path d={svgAreaD} fill="url(#quantGrad)" />}

                    {/* Main Line */}
                    {svgPathD && <path d={svgPathD} fill="none" stroke="#38bdf8" strokeWidth="2.5" strokeLinecap="round" />}

                    {/* Data Points */}
                    {chartCoords.map((p, i) => (
                      <g key={i} className="group cursor-pointer">
                        <circle cx={p.x} cy={p.y} r="3.5" fill="#082f49" stroke="#38bdf8" strokeWidth="2" className="group-hover:r-5 group-hover:fill-cyan-400 transition-all" />
                        <title>{`${p.time}\n${p.label}\n${formatNumber(p.val)} aUEC`}</title>
                      </g>
                    ))}
                  </svg>
                </div>
              ) : (
                <div className="h-32 flex items-center justify-center text-xs font-mono text-slate-500">
                  Keine historischen Transaktionen in dieser Auswahl vorhanden.
                </div>
              )}
            </div>

            {/* Top Einnahmen & Top Ausgaben 2-Spalten */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              {/* Größte Einnahmen */}
              <div className="bg-[#030a16] rounded-lg border border-cyan-950 p-3">
                <div className="text-xs font-mono font-bold text-emerald-400 uppercase tracking-wider mb-2 flex items-center gap-1.5">
                  <TrendingUp className="w-3.5 h-3.5" />
                  Größte Einnahmen-Posten
                </div>
                <div className="space-y-1.5 max-h-56 overflow-y-auto">
                  {(data?.topIncome || []).map((e, idx) => (
                    <div key={idx} className="flex justify-between items-center text-xs font-mono bg-[#051122]/60 p-1.5 rounded border border-cyan-950/60">
                      <div className="truncate max-w-[240px]">
                        <span className="text-slate-200 font-semibold">{e.title}</span>
                        <div className="text-[10px] text-slate-500 truncate">{e.description}</div>
                      </div>
                      <span className="text-emerald-400 font-bold shrink-0">+{formatNumber(e.amount)} aUEC</span>
                    </div>
                  ))}
                  {(!data?.topIncome || data.topIncome.length === 0) && (
                    <div className="text-xs text-slate-500 py-2">Keine Einnahmen erfasst.</div>
                  )}
                </div>
              </div>

              {/* Größte Ausgaben */}
              <div className="bg-[#030a16] rounded-lg border border-cyan-950 p-3">
                <div className="text-xs font-mono font-bold text-rose-400 uppercase tracking-wider mb-2 flex items-center gap-1.5">
                  <TrendingDown className="w-3.5 h-3.5" />
                  Größte Ausgaben-Posten
                </div>
                <div className="space-y-1.5 max-h-56 overflow-y-auto">
                  {(data?.topExpenses || []).map((e, idx) => (
                    <div key={idx} className="flex justify-between items-center text-xs font-mono bg-[#051122]/60 p-1.5 rounded border border-cyan-950/60">
                      <div className="truncate max-w-[240px]">
                        <span className="text-slate-200 font-semibold">{e.title}</span>
                        <div className="text-[10px] text-slate-500 truncate">{e.description}</div>
                      </div>
                      <span className="text-rose-400 font-bold shrink-0">{formatNumber(e.amount)} aUEC</span>
                    </div>
                  ))}
                  {(!data?.topExpenses || data.topExpenses.length === 0) && (
                    <div className="text-xs text-slate-500 py-2">Keine Ausgaben erfasst.</div>
                  )}
                </div>
              </div>
            </div>
          </div>
        )}

        {/* SUBTAB 2: BUCHHALTUNG (LEDGER DATAGRID) */}
        {activeSubTab === 'ledger' && (
          <div className="flex-1 flex flex-col space-y-2 overflow-hidden">
            <div className="flex items-center justify-between gap-2 shrink-0">
              <div className="relative w-72">
                <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-500" />
                <input
                  type="text"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  placeholder="Buchungsjournal durchsuchen..."
                  className="w-full bg-[#071322] border border-cyan-900/60 rounded pl-8 pr-3 py-1 text-xs font-mono text-slate-200 placeholder-slate-500 focus:outline-none focus:border-cyan-500"
                />
              </div>
              <span className="text-xs font-mono text-slate-400">{filteredLedger.length} Buchungen erfasst</span>
            </div>

            <div className="flex-1 overflow-auto border border-cyan-950 rounded-lg">
              <table className="w-full text-left font-mono text-xs border-collapse">
                <thead className="bg-[#030914] text-slate-400 text-[11px] uppercase tracking-wider sticky top-0 border-b border-cyan-950 z-10">
                  <tr>
                    <th className="py-2 px-3">Zeit</th>
                    <th className="py-2 px-3">Typ</th>
                    <th className="py-2 px-3">Betrag</th>
                    <th className="py-2 px-3">Schiff</th>
                    <th className="py-2 px-3">Buchungsdetail</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-cyan-950/40">
                  {filteredLedger.map((item) => (
                    <tr key={item.id} className="hover:bg-[#071322]/80 transition">
                      <td className="py-2 px-3 text-slate-400 whitespace-nowrap">{item.timestamp}</td>
                      <td className="py-2 px-3">
                        <span className="px-1.5 py-0.5 rounded text-[10px] font-bold bg-slate-900 border border-cyan-900/50 text-cyan-300">
                          {item.kindText || item.title}
                        </span>
                      </td>
                      <td className="py-2 px-3 whitespace-nowrap font-bold">
                        <span className={(item.amount || 0) >= 0 ? 'text-emerald-400' : 'text-rose-400'}>
                          {((item.amount || 0) >= 0 ? '+' : '')}{formatNumber(item.amount)} aUEC
                        </span>
                      </td>
                      <td className="py-2 px-3 text-cyan-300 font-semibold truncate max-w-[140px]">{item.ship || '—'}</td>
                      <td className="py-2 px-3 text-slate-300 truncate max-w-md" title={item.description}>{item.description}</td>
                    </tr>
                  ))}
                  {filteredLedger.length === 0 && (
                    <tr>
                      <td colSpan={5} className="py-8 text-center text-slate-500">
                        Keine Buchungen gefunden.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* SUBTAB 3: AUSGABEN-ANALYSE */}
        {activeSubTab === 'spending' && (
          <div className="flex-1 overflow-y-auto space-y-4 pr-1">
            <div className="text-xs font-mono text-slate-400">
              Kategorisierte Aufschlüsselung aller erfassten Ausgabenposten:
            </div>

            <div className="space-y-3">
              {spendingCategories.map((c, i) => {
                const IconComponent = c.icon;
                return (
                  <div key={i} className="bg-[#030a16] p-3 rounded-lg border border-cyan-950 space-y-2">
                    <div className="flex justify-between items-center text-xs font-mono">
                      <div className="flex items-center gap-2">
                        <IconComponent className="w-4 h-4 text-cyan-400" />
                        <span className="font-bold text-slate-200">{c.label}</span>
                        <span className="text-slate-500">({c.count}× gebucht)</span>
                      </div>
                      <div className="flex items-center gap-3">
                        <span className="font-bold text-rose-400">-{formatNumber(c.amount)} aUEC</span>
                        <span className="text-cyan-300 font-semibold">{c.percent}%</span>
                      </div>
                    </div>
                    <div className="w-full bg-slate-900 h-2 rounded-full overflow-hidden border border-cyan-950">
                      <div className={`h-full ${c.color}`} style={{ width: `${c.percent}%` }} />
                    </div>
                  </div>
                );
              })}
              {spendingCategories.length === 0 && (
                <div className="text-xs text-slate-500 py-8 text-center">Keine Ausgaben zur Kategorisierung vorhanden.</div>
              )}
            </div>
          </div>
        )}

        {/* SUBTAB 4: FRACHT & HANDEL */}
        {activeSubTab === 'cargo' && (
          <div className="flex-1 flex flex-col space-y-2 overflow-hidden">
            <div className="text-xs font-mono text-slate-400 flex items-center justify-between">
              <span>Fracht- & Rohstoffhandel (Trade Logs)</span>
              <span>{data?.cargo.length || 0} Handelsposten erfasst</span>
            </div>

            <div className="flex-1 overflow-auto border border-cyan-950 rounded-lg">
              <table className="w-full text-left font-mono text-xs border-collapse">
                <thead className="bg-[#030914] text-slate-400 text-[11px] uppercase tracking-wider sticky top-0 border-b border-cyan-950 z-10">
                  <tr>
                    <th className="py-2 px-3">Zeit</th>
                    <th className="py-2 px-3">Handelsaktion</th>
                    <th className="py-2 px-3">Umsatz / Wert</th>
                    <th className="py-2 px-3">Schiff</th>
                    <th className="py-2 px-3">Detail</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-cyan-950/40">
                  {(data?.cargo || []).map((c) => (
                    <tr key={c.id} className="hover:bg-[#071322]/80 transition">
                      <td className="py-2 px-3 text-slate-400 whitespace-nowrap">{c.timestamp}</td>
                      <td className="py-2 px-3 text-amber-300 font-semibold">{c.title}</td>
                      <td className="py-2 px-3 font-bold text-emerald-400 whitespace-nowrap">
                        {formatNumber(c.amount)} aUEC
                      </td>
                      <td className="py-2 px-3 text-cyan-300 truncate max-w-[130px]">{c.ship || '—'}</td>
                      <td className="py-2 px-3 text-slate-300 truncate max-w-md">{c.description}</td>
                    </tr>
                  ))}
                  {(!data?.cargo || data.cargo.length === 0) && (
                    <tr>
                      <td colSpan={5} className="py-8 text-center text-slate-500">
                        Keine Fracht- oder Handelsbewegungen verzeichnet.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
