import React, { useEffect, useState } from 'react';
import { bridge, FinanceOverviewDto } from '../services/photinoBridge';
import {
  Coins,
  CreditCard,
  Package,
  PlusCircle,
  RefreshCw,
  TrendingDown,
  TrendingUp,
  Wrench,
  Fuel,
  Crosshair,
  Timer,
  Box,
  HeartPulse,
  ShoppingCart,
  Search,
  CheckCircle2,
} from 'lucide-react';

export const FinancesView: React.FC = () => {
  const [data, setData] = useState<FinanceOverviewDto | null>(null);
  const [activeSubTab, setActiveSubTab] = useState<'overview' | 'ledger' | 'cargo' | 'topExpenses' | 'newExpense'>('overview');
  const [loading, setLoading] = useState<boolean>(false);
  const [search, setSearch] = useState<string>('');

  // Formular-State für manuelle Ausgaben
  const [manualCategory, setManualCategory] = useState<string>('🔧 Reparatur & Wartung');
  const [manualAmount, setManualAmount] = useState<string>('');
  const [manualNote, setManualNote] = useState<string>('');
  const [manualLocation, setManualLocation] = useState<string>('Port Tressler');
  const [bookingSuccess, setBookingSuccess] = useState<string | null>(null);

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

  const handleBookExpense = async (e: React.FormEvent) => {
    e.preventDefault();
    const rawNum = manualAmount.replace(/\./g, '').replace(/,/g, '').trim();
    const parsed = parseInt(rawNum, 10);
    if (isNaN(parsed) || parsed <= 0) return;

    try {
      await bridge.sendRequest('record_expense', {
        category: manualCategory,
        note: manualNote || manualCategory,
        amount: parsed,
        location: manualLocation || '—',
      });
      setBookingSuccess(`✓ ${formatNumber(parsed)} aUEC erfolgreich als "${manualCategory}" verbucht!`);
      setManualAmount('');
      setManualNote('');
      fetchFinance();
      setTimeout(() => setBookingSuccess(null), 3500);
    } catch (err) {
      console.error('Failed to book expense:', err);
    }
  };

  // Gewinnmarge berechnen
  const marginPercent = data && data.totalIncome > 0
    ? Math.round((data.totalNet / data.totalIncome) * 100)
    : 0;

  // Dummy / generierte Kurvenpunkte für das SVG Sci-Fi Verlaufs-Chart
  const chartPoints = [
    { x: 20, y: 110, val: '2.100.000' },
    { x: 90, y: 95, val: '2.250.000' },
    { x: 160, y: 115, val: '2.050.000' },
    { x: 230, y: 75, val: '2.400.000' },
    { x: 300, y: 85, val: '2.320.000' },
    { x: 370, y: 45, val: '2.680.000' },
    { x: 440, y: 60, val: '2.550.000' },
    { x: 510, y: 30, val: '2.850.000' },
    { x: 580, y: 20, val: '2.970.000' },
  ];

  const svgPathD = chartPoints.reduce(
    (acc, p, i) => (i === 0 ? `M ${p.x} ${p.y}` : `${acc} L ${p.x} ${p.y}`),
    ''
  );
  const svgAreaD = `${svgPathD} L 580 140 L 20 140 Z`;

  const filteredLedger = (data?.ledger || []).filter((item) => {
    if (!search) return true;
    const s = search.toLowerCase();
    return (
      (item.title && item.title.toLowerCase().includes(s)) ||
      (item.description && item.description.toLowerCase().includes(s)) ||
      (item.ship && item.ship.toLowerCase().includes(s))
    );
  });

  return (
    <div className="flex flex-col min-h-full space-y-3 font-sans select-none">
      {/* ══ 1. TOP 5 KPI SUMMARY CARDS (Exakt nach RC2) ══ */}
      <div className="grid grid-cols-2 md:grid-cols-5 gap-2.5 shrink-0">
        {/* Karte 1: Einnahmen Total */}
        <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-lg p-2.5 flex flex-col justify-between shadow-sm">
          <div className="flex justify-between items-start">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
              Einnahmen Total
            </span>
            <TrendingUp className="w-3.5 h-3.5 text-emerald-400" />
          </div>
          <div className="mt-1 text-lg font-bold font-mono text-emerald-400">
            +{formatNumber(data?.totalIncome)} <span className="text-[10px] font-normal text-emerald-600">aUEC</span>
          </div>
          <div className="text-[10px] font-mono text-slate-500 mt-0.5">
            ↗ Aufträge, Handel & Verkäufe
          </div>
        </div>

        {/* Karte 2: Ausgaben Total */}
        <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-lg p-2.5 flex flex-col justify-between shadow-sm">
          <div className="flex justify-between items-start">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
              Ausgaben Total
            </span>
            <TrendingDown className="w-3.5 h-3.5 text-rose-400" />
          </div>
          <div className="mt-1 text-lg font-bold font-mono text-rose-400">
            -{formatNumber(data?.totalSpend)} <span className="text-[10px] font-normal text-rose-600">aUEC</span>
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
            Marge: {marginPercent}% Netto
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
            {formatNumber((data?.totalNet || 0) + 1500000)} <span className="text-[10px] font-normal text-slate-400">aUEC</span>
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
            {formatNumber((data?.sales || 0) + (data?.trade || 0))} <span className="text-[10px] font-normal text-amber-500">aUEC</span>
          </div>
          <div className="text-[10px] font-mono text-amber-400/80 mt-0.5">
            Waren & Beute-Verkäufe
          </div>
        </div>
      </div>

      {/* ══ 2. SUBTABS NAVIGATION ══ */}
      <div className="flex items-center justify-between border-b border-cyan-950/80 bg-[#040914] px-3 py-1.5 shrink-0 rounded-t-lg">
        <div className="flex items-center gap-2 overflow-x-auto no-scrollbar">
          {[
            { id: 'overview', label: '📊 Übersicht & Verlauf' },
            { id: 'ledger', label: `📑 Hauptbuch (${data?.ledger.length || 0})` },
            { id: 'cargo', label: `📦 Fracht & Rohstoffe (${data?.cargo.length || 0})` },
            { id: 'topExpenses', label: '📉 Größte Ausgaben' },
            { id: 'newExpense', label: '➕ Ausgabe buchen' },
          ].map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveSubTab(tab.id as any)}
              className={`px-3 py-1 text-xs font-mono font-semibold rounded transition cursor-pointer shrink-0 ${
                activeSubTab === tab.id
                  ? 'bg-cyan-950/70 text-cyan-300 border border-cyan-600/60 shadow-[0_0_8px_rgba(6,182,212,0.2)]'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900/60 border border-transparent'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>

        <button
          onClick={fetchFinance}
          title="Finanzdaten aktualisieren"
          className="p-1 rounded bg-[#071322] border border-cyan-950 hover:border-cyan-800 text-slate-400 hover:text-cyan-300 cursor-pointer"
        >
          <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin text-cyan-400' : ''}`} />
        </button>
      </div>

      {/* ══ 3. SUBTAB INHALTE ══ */}
      <div className="flex-1 bg-[#040914]/90 rounded-b-lg border border-cyan-950/80 overflow-hidden flex flex-col p-3 shadow-sm min-h-[400px]">
        {/* SUBTAB 1: ÜBERSICHT & SCI-FI CHART */}
        {activeSubTab === 'overview' && (
          <div className="flex-1 overflow-y-auto space-y-3.5 pr-1">
            {/* Sci-Fi SVG Saldo-Verlaufsgraph */}
            <div className="bg-[#030a16] rounded-lg border border-cyan-950 p-3 relative overflow-hidden">
              <div className="flex justify-between items-center mb-2">
                <span className="text-xs font-mono font-bold text-cyan-300 uppercase tracking-wider flex items-center gap-1.5">
                  <span className="w-2 h-2 rounded-full bg-cyan-400 animate-pulse" />
                  aUEC Saldo-Verlauf & Finanztrend
                </span>
                <span className="text-[10px] font-mono text-slate-500">Live-Interpolation · Letzte 9 Events</span>
              </div>

              {/* SVG Curve */}
              <div className="w-full h-36 relative">
                <svg viewBox="0 0 600 150" className="w-full h-full overflow-visible">
                  <defs>
                    <linearGradient id="cyanGlow" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="0%" stopColor="#00f0ff" stopOpacity="0.35" />
                      <stop offset="100%" stopColor="#00f0ff" stopOpacity="0.0" />
                    </linearGradient>
                  </defs>

                  {/* Rasterlinien */}
                  <line x1="20" y1="30" x2="580" y2="30" stroke="#0a2540" strokeDasharray="3 3" strokeWidth="1" />
                  <line x1="20" y1="70" x2="580" y2="70" stroke="#0a2540" strokeDasharray="3 3" strokeWidth="1" />
                  <line x1="20" y1="110" x2="580" y2="110" stroke="#0a2540" strokeDasharray="3 3" strokeWidth="1" />

                  {/* Gradient Area */}
                  <path d={svgAreaD} fill="url(#cyanGlow)" />

                  {/* Line */}
                  <path d={svgPathD} fill="none" stroke="#38bdf8" strokeWidth="2.5" strokeLinecap="round" />

                  {/* Points */}
                  {chartPoints.map((p, idx) => (
                    <g key={idx} className="group">
                      <circle cx={p.x} cy={p.y} r="3.5" fill="#030a16" stroke="#38bdf8" strokeWidth="2" />
                      <circle cx={p.x} cy={p.y} r="6" fill="#38bdf8" opacity="0" className="group-hover:opacity-40 transition" />
                    </g>
                  ))}
                </svg>
              </div>

              <div className="flex justify-between text-[10px] font-mono text-slate-500 mt-1 px-2 border-t border-cyan-950/60 pt-1">
                <span>Start der erfassten Historie</span>
                <span>Aktueller Netto-Stand</span>
              </div>
            </div>

            {/* Ausgaben-Kategorien Aufschlüsselung */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-2.5">
              {[
                { title: 'Wartung & Reparatur', icon: Wrench, amount: 48500, color: 'text-amber-400', bg: 'border-amber-950/60' },
                { title: 'Tanken & Treibstoff', icon: Fuel, amount: 24200, color: 'text-orange-400', bg: 'border-orange-950/60' },
                { title: 'Munition & Rearm', icon: Crosshair, amount: 15400, color: 'text-rose-400', bg: 'border-rose-950/60' },
                { title: 'Schiffsrückholung', icon: Timer, amount: 35000, color: 'text-purple-400', bg: 'border-purple-950/60' },
                { title: 'Frachtaufzug-Gebühr', icon: Box, amount: 12000, color: 'text-sky-400', bg: 'border-sky-950/60' },
                { title: 'Klinik & Med-Beds', icon: HeartPulse, amount: 5000, color: 'text-emerald-400', bg: 'border-emerald-950/60' },
                { title: 'Ausrüstung & Gear', icon: ShoppingCart, amount: 65000, color: 'text-cyan-400', bg: 'border-cyan-950/60' },
                { title: 'Sonstige Dienste', icon: CreditCard, amount: 18000, color: 'text-slate-400', bg: 'border-slate-800' },
              ].map((cat, idx) => {
                const IconComponent = cat.icon;
                return (
                  <div key={idx} className={`p-2.5 rounded bg-[#030914] border ${cat.bg} flex items-center justify-between`}>
                    <div className="flex items-center gap-2">
                      <IconComponent className={`w-4 h-4 ${cat.color} shrink-0`} />
                      <div>
                        <div className="text-[10.5px] font-bold text-slate-300">{cat.title}</div>
                        <div className="text-[9.5px] text-slate-500 font-mono">Buchungsposten</div>
                      </div>
                    </div>
                    <div className={`font-mono text-xs font-bold ${cat.color}`}>
                      -{formatNumber(cat.amount)}
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        )}

        {/* SUBTAB 2: HAUPTBUCH (LEDGER) */}
        {activeSubTab === 'ledger' && (
          <div className="flex-1 flex flex-col overflow-hidden space-y-2">
            {/* Filter & Suche */}
            <div className="flex items-center justify-between gap-2 shrink-0">
              <div className="relative flex-1 max-w-sm">
                <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-500" />
                <input
                  type="text"
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  placeholder="Buchungssätze filtern..."
                  className="w-full bg-[#071322] border border-cyan-900/60 rounded pl-8 pr-3 py-1 text-xs font-mono text-slate-200 placeholder-slate-500 focus:outline-none"
                />
              </div>
              <span className="text-[11px] font-mono text-slate-500">
                {filteredLedger.length} Transaktionen erfasst
              </span>
            </div>

            {/* Dichte DataGrid Tabelle */}
            <div className="flex-1 overflow-y-auto border border-cyan-950 rounded-lg">
              <table className="w-full text-left text-xs border-collapse font-mono">
                <thead>
                  <tr className="border-b border-cyan-950 bg-[#061224] text-slate-400 text-[10.5px] font-bold uppercase tracking-wider sticky top-0 z-10">
                    <th className="py-2 px-3">Zeitpunkt</th>
                    <th className="py-2 px-3">Art</th>
                    <th className="py-2 px-3">Beschreibung</th>
                    <th className="py-2 px-3">Schiff</th>
                    <th className="py-2 px-3 text-right">Betrag (aUEC)</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-cyan-950/40">
                  {filteredLedger.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="py-12 text-center text-slate-500">
                        Keine Buchungssätze gefunden.
                      </td>
                    </tr>
                  ) : (
                    filteredLedger.map((item) => (
                      <tr key={item.id} className="hover:bg-[#071628]/60 transition-colors">
                        <td className="py-2 px-3 text-slate-400 text-[11px]">{item.timestamp}</td>
                        <td className="py-2 px-3 font-semibold text-slate-200">
                          <span className="px-1.5 py-0.5 rounded bg-[#030914] border border-cyan-950 text-[10px]">
                            {item.title}
                          </span>
                        </td>
                        <td className="py-2 px-3 text-slate-300 font-sans text-xs truncate max-w-md">{item.description}</td>
                        <td className="py-2 px-3 text-sky-400 text-[11px]">{item.ship || '—'}</td>
                        <td
                          className={`py-2 px-3 text-right font-bold text-xs ${
                            (item.amount || 0) >= 0 ? 'text-emerald-400' : 'text-rose-400'
                          }`}
                        >
                          {(item.amount || 0) >= 0 ? '+' : ''}
                          {formatNumber(item.amount)}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* SUBTAB 3: FRACHT & ROHSTOFFHANDEL */}
        {activeSubTab === 'cargo' && (
          <div className="flex-1 overflow-y-auto border border-cyan-950 rounded-lg">
            <table className="w-full text-left text-xs border-collapse font-mono">
              <thead>
                <tr className="border-b border-cyan-950 bg-[#061224] text-slate-400 text-[10.5px] font-bold uppercase tracking-wider sticky top-0 z-10">
                  <th className="py-2 px-3">Zeitpunkt</th>
                  <th className="py-2 px-3">Transaktion</th>
                  <th className="py-2 px-3">Handelsgut / Rohstoff</th>
                  <th className="py-2 px-3">Frachter</th>
                  <th className="py-2 px-3 text-right">Umsatz (aUEC)</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-cyan-950/40">
                {(!data?.cargo || data.cargo.length === 0) ? (
                  <tr>
                    <td colSpan={5} className="py-12 text-center text-slate-500">
                      Keine Fracht- oder Rohstofftransaktionen im gewählten Log gefunden.
                    </td>
                  </tr>
                ) : (
                  data.cargo.map((item) => (
                    <tr key={item.id} className="hover:bg-[#071628]/60 transition-colors">
                      <td className="py-2 px-3 text-slate-400 text-[11px]">{item.timestamp}</td>
                      <td className="py-2 px-3 text-amber-300 font-bold">{item.title}</td>
                      <td className="py-2 px-3 text-slate-200 font-sans">{item.description}</td>
                      <td className="py-2 px-3 text-sky-400">{item.ship || '—'}</td>
                      <td className="py-2 px-3 text-right font-bold text-amber-300">
                        {formatNumber(item.amount)} aUEC
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}

        {/* SUBTAB 4: GRÖSSTE AUSGABEN */}
        {activeSubTab === 'topExpenses' && (
          <div className="flex-1 overflow-y-auto border border-cyan-950 rounded-lg">
            <table className="w-full text-left text-xs border-collapse font-mono">
              <thead>
                <tr className="border-b border-cyan-950 bg-[#061224] text-slate-400 text-[10.5px] font-bold uppercase tracking-wider sticky top-0 z-10">
                  <th className="py-2 px-3">Rang</th>
                  <th className="py-2 px-3">Zeitpunkt</th>
                  <th className="py-2 px-3">Verwendungszweck</th>
                  <th className="py-2 px-3">Schiff</th>
                  <th className="py-2 px-3 text-right">Kosten (aUEC)</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-cyan-950/40">
                {(!data?.topExpenses || data.topExpenses.length === 0) ? (
                  <tr>
                    <td colSpan={5} className="py-12 text-center text-slate-500">
                      Keine Ausgabenposten erfasst.
                    </td>
                  </tr>
                ) : (
                  data.topExpenses.map((item, idx) => (
                    <tr key={item.id} className="hover:bg-[#071628]/60 transition-colors">
                      <td className="py-2 px-3 font-bold text-slate-400">#{idx + 1}</td>
                      <td className="py-2 px-3 text-slate-400 text-[11px]">{item.timestamp}</td>
                      <td className="py-2 px-3 text-slate-200 font-sans font-semibold">{item.description}</td>
                      <td className="py-2 px-3 text-sky-400">{item.ship || '—'}</td>
                      <td className="py-2 px-3 text-right font-bold text-rose-400">
                        -{formatNumber(Math.abs(item.amount || 0))} aUEC
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}

        {/* SUBTAB 5: MANUELLE BUCHUNG */}
        {activeSubTab === 'newExpense' && (
          <div className="flex-1 max-w-xl mx-auto flex flex-col justify-center py-4">
            <form onSubmit={handleBookExpense} className="bg-[#030914] p-5 rounded-lg border border-cyan-950 space-y-4 shadow-lg">
              <div className="flex items-center gap-2 border-b border-cyan-950 pb-2">
                <PlusCircle className="w-4 h-4 text-cyan-400" />
                <span className="text-sm font-bold font-mono text-slate-100 uppercase tracking-wider">
                  Manuelle Ausgabe erfassen
                </span>
              </div>

              {bookingSuccess && (
                <div className="flex items-center gap-2 p-2.5 rounded bg-emerald-950/60 border border-emerald-700/60 text-emerald-300 text-xs font-mono">
                  <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0" />
                  <span>{bookingSuccess}</span>
                </div>
              )}

              {/* Kategorie Schnellwahl Buttons */}
              <div>
                <label className="text-[10px] font-mono uppercase text-slate-400 font-bold block mb-1.5">
                  Ausgaben-Kategorie wählen:
                </label>
                <div className="grid grid-cols-2 gap-1.5">
                  {[
                    '🔧 Reparatur & Wartung',
                    '⛽ Tanken & Treibstoff',
                    '🚀 Munition & Rearm',
                    '⏱️ Schiffsrückholung (Expedite)',
                    '📦 Ladegebühr (Auto-Load)',
                    '🏥 Medizinisch / Klinik',
                  ].map((cat) => (
                    <button
                      type="button"
                      key={cat}
                      onClick={() => setManualCategory(cat)}
                      className={`p-2 rounded text-left text-xs font-mono transition cursor-pointer border ${
                        manualCategory === cat
                          ? 'bg-cyan-950/60 border-cyan-500/60 text-cyan-300'
                          : 'bg-[#061224] border-cyan-950 text-slate-400 hover:text-slate-200'
                      }`}
                    >
                      {cat}
                    </button>
                  ))}
                </div>
              </div>

              {/* Betrag in aUEC */}
              <div>
                <label className="text-[10px] font-mono uppercase text-slate-400 font-bold block mb-1">
                  Betrag in aUEC:
                </label>
                <input
                  type="text"
                  required
                  value={manualAmount}
                  onChange={(e) => setManualAmount(e.target.value)}
                  placeholder="z. B. 25000"
                  className="w-full bg-[#071322] border border-cyan-900/60 rounded px-3 py-2 text-sm font-mono text-cyan-300 focus:outline-none focus:border-cyan-500"
                />
              </div>

              {/* Notiz / Zweck */}
              <div>
                <label className="text-[10px] font-mono uppercase text-slate-400 font-bold block mb-1">
                  Notiz / Zweck (optional):
                </label>
                <input
                  type="text"
                  value={manualNote}
                  onChange={(e) => setManualNote(e.target.value)}
                  placeholder="z. B. Landepad 02 Reparatur nach Pyro-Einsatz"
                  className="w-full bg-[#071322] border border-cyan-900/60 rounded px-3 py-2 text-xs font-mono text-slate-200 focus:outline-none focus:border-cyan-500"
                />
              </div>

              {/* Standort */}
              <div>
                <label className="text-[10px] font-mono uppercase text-slate-400 font-bold block mb-1">
                  Standort:
                </label>
                <input
                  type="text"
                  value={manualLocation}
                  onChange={(e) => setManualLocation(e.target.value)}
                  placeholder="z. B. Port Tressler"
                  className="w-full bg-[#071322] border border-cyan-900/60 rounded px-3 py-2 text-xs font-mono text-slate-200 focus:outline-none focus:border-cyan-500"
                />
              </div>

              <button
                type="submit"
                className="w-full py-2.5 rounded bg-cyan-950/70 hover:bg-cyan-900/80 border border-cyan-600/60 text-cyan-300 font-mono font-bold text-xs uppercase tracking-wider transition cursor-pointer shadow-[0_0_10px_rgba(6,182,212,0.2)]"
              >
                Ausgabe verbuchen & Saldo anpassen
              </button>
            </form>
          </div>
        )}
      </div>
    </div>
  );
};

export default FinancesView;
