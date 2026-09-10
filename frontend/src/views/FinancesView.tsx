import React, { useEffect, useState } from 'react';
import { bridge, FinanceOverviewDto } from '../services/photinoBridge';
import {
  Coins,
  CreditCard,
  FileSpreadsheet,
  Package,
  RefreshCw,
  TrendingDown,
  TrendingUp,
  Workflow,
} from 'lucide-react';

export const FinancesView: React.FC = () => {
  const [data, setData] = useState<FinanceOverviewDto | null>(null);
  const [activeSubTab, setActiveSubTab] = useState<'ledger' | 'cargo' | 'topExpenses'>('ledger');
  const [loading, setLoading] = useState<boolean>(false);

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
  }, []);

  const formatNumber = (num?: number) => {
    if (num === undefined || num === null) return '0';
    return num.toLocaleString('de-DE');
  };

  return (
    <div className="flex flex-col h-full space-y-4">
      {/* Finance KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        {/* Net Balance */}
        <div className="sc-glass rounded-lg p-4 border border-slate-800 sc-hud-corner">
          <div className="flex justify-between items-start">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Finanzsaldo Netto
            </span>
            <Coins className="w-4 h-4 text-cyan-400" />
          </div>
          <div className="mt-2 text-2xl font-bold font-mono text-cyan-300 drop-shadow-[0_0_8px_rgba(0,240,255,0.3)]">
            {formatNumber(data?.totalNet)} <span className="text-xs font-normal text-cyan-500">aUEC</span>
          </div>
          <div className="mt-1 flex items-center gap-2 text-xs text-slate-400">
            <span className="text-emerald-400 flex items-center gap-0.5">
              <TrendingUp className="w-3 h-3" /> +{formatNumber(data?.totalIncome)}
            </span>
            <span>·</span>
            <span className="text-rose-400 flex items-center gap-0.5">
              <TrendingDown className="w-3 h-3" /> -{formatNumber(data?.totalSpend)}
            </span>
          </div>
        </div>

        {/* Cargo & Trade */}
        <div className="sc-glass rounded-lg p-4 border border-slate-800 sc-hud-corner">
          <div className="flex justify-between items-start">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Handel & Güterverkauf
            </span>
            <Package className="w-4 h-4 text-amber-400" />
          </div>
          <div className="mt-2 text-2xl font-bold font-mono text-amber-300">
            {formatNumber((data?.sales || 0) + (data?.trade || 0))}{' '}
            <span className="text-xs font-normal text-amber-500">aUEC</span>
          </div>
          <div className="mt-1 text-xs text-slate-400">
            Item-Verkäufe: {formatNumber(data?.sales)} · Fracht: {formatNumber(data?.trade)}
          </div>
        </div>

        {/* Mission Rewards */}
        <div className="sc-glass rounded-lg p-4 border border-slate-800 sc-hud-corner">
          <div className="flex justify-between items-start">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Auftrags-Belohnungen
            </span>
            <Workflow className="w-4 h-4 text-indigo-400" />
          </div>
          <div className="mt-2 text-2xl font-bold font-mono text-indigo-300">
            {formatNumber(data?.missionsReward)}{' '}
            <span className="text-xs font-normal text-indigo-500">aUEC</span>
          </div>
          <div className="mt-1 text-xs text-slate-400">
            Aus abgeschlossenen Verträgen & Kopfgeldern
          </div>
        </div>

        {/* Spending & Purchases */}
        <div className="sc-glass rounded-lg p-4 border border-slate-800 sc-hud-corner">
          <div className="flex justify-between items-start">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Käufe & Ausgaben
            </span>
            <CreditCard className="w-4 h-4 text-rose-400" />
          </div>
          <div className="mt-2 text-2xl font-bold font-mono text-rose-300">
            -{formatNumber(data?.purchases)}{' '}
            <span className="text-xs font-normal text-rose-500">aUEC</span>
          </div>
          <div className="mt-1 text-xs text-slate-400">
            Warenkäufe, Ausrüstung, Reparatur & Treibstoff
          </div>
        </div>
      </div>

      {/* Subtabs Bar */}
      <div className="flex items-center justify-between border-b border-slate-800 bg-[#030712]/70 px-4">
        <div className="flex items-center gap-4">
          <button
            onClick={() => setActiveSubTab('ledger')}
            className={`py-3 text-xs font-semibold uppercase tracking-wider border-b-2 transition cursor-pointer flex items-center gap-1.5 ${
              activeSubTab === 'ledger'
                ? 'border-cyan-400 text-cyan-300'
                : 'border-transparent text-slate-400 hover:text-slate-200'
            }`}
          >
            <FileSpreadsheet className="w-3.5 h-3.5" /> Hauptbuch (Ledger) ({data?.ledger.length || 0})
          </button>

          <button
            onClick={() => setActiveSubTab('cargo')}
            className={`py-3 text-xs font-semibold uppercase tracking-wider border-b-2 transition cursor-pointer flex items-center gap-1.5 ${
              activeSubTab === 'cargo'
                ? 'border-cyan-400 text-cyan-300'
                : 'border-transparent text-slate-400 hover:text-slate-200'
            }`}
          >
            <Package className="w-3.5 h-3.5" /> Fracht & Rohstoffhandel ({data?.cargo.length || 0})
          </button>

          <button
            onClick={() => setActiveSubTab('topExpenses')}
            className={`py-3 text-xs font-semibold uppercase tracking-wider border-b-2 transition cursor-pointer flex items-center gap-1.5 ${
              activeSubTab === 'topExpenses'
                ? 'border-cyan-400 text-cyan-300'
                : 'border-transparent text-slate-400 hover:text-slate-200'
            }`}
          >
            <TrendingDown className="w-3.5 h-3.5" /> Größte Einzelposten
          </button>
        </div>

        <button
          onClick={fetchFinance}
          title="Finanzdaten aktualisieren"
          className="p-1.5 rounded bg-slate-900 border border-slate-800 text-slate-400 hover:text-slate-200 cursor-pointer"
        >
          <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
        </button>
      </div>

      {/* Table Content Area */}
      <div className="flex-1 sc-glass rounded-lg border border-slate-800 overflow-y-auto">
        {activeSubTab === 'ledger' && (
          <table className="w-full text-left text-xs border-collapse">
            <thead>
              <tr className="border-b border-slate-800 bg-slate-900/60 text-slate-400 font-mono uppercase tracking-wider">
                <th className="py-3 px-4">Zeitpunkt</th>
                <th className="py-3 px-4">Buchungsart</th>
                <th className="py-3 px-4">Details / Beschreibung</th>
                <th className="py-3 px-4">Schiff</th>
                <th className="py-3 px-4 text-right">Betrag (aUEC)</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/40 font-mono">
              {(!data?.ledger || data.ledger.length === 0) ? (
                <tr>
                  <td colSpan={5} className="py-12 text-center text-slate-500">
                    Keine Buchungssätze in der Datenbank vorhanden.
                  </td>
                </tr>
              ) : (
                data.ledger.map((item) => (
                  <tr key={item.id} className="hover:bg-slate-900/40 transition">
                    <td className="py-2.5 px-4 text-slate-400">{item.timestamp}</td>
                    <td className="py-2.5 px-4 font-sans font-semibold text-slate-300">
                      {item.title}
                    </td>
                    <td className="py-2.5 px-4 font-sans text-slate-400">{item.description}</td>
                    <td className="py-2.5 px-4 text-slate-400">{item.ship || '—'}</td>
                    <td
                      className={`py-2.5 px-4 text-right font-bold ${
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
        )}

        {activeSubTab === 'cargo' && (
          <table className="w-full text-left text-xs border-collapse">
            <thead>
              <tr className="border-b border-slate-800 bg-slate-900/60 text-slate-400 font-mono uppercase tracking-wider">
                <th className="py-3 px-4">Datum</th>
                <th className="py-3 px-4">Ware / Ladung</th>
                <th className="py-3 px-4">Schiff</th>
                <th className="py-3 px-4 text-right">Erlös (aUEC)</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/40 font-mono">
              {(!data?.cargo || data.cargo.length === 0) ? (
                <tr>
                  <td colSpan={4} className="py-12 text-center text-slate-500">
                    Keine Frachttransaktionen verzeichnet.
                  </td>
                </tr>
              ) : (
                data.cargo.map((item) => (
                  <tr key={item.id} className="hover:bg-slate-900/40 transition">
                    <td className="py-2.5 px-4 text-slate-400">{item.timestamp}</td>
                    <td className="py-2.5 px-4 font-sans font-semibold text-amber-300">
                      {item.description}
                    </td>
                    <td className="py-2.5 px-4 text-slate-400">{item.ship || '—'}</td>
                    <td className="py-2.5 px-4 text-right font-bold text-emerald-400">
                      +{formatNumber(item.amount)}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        )}

        {activeSubTab === 'topExpenses' && (
          <table className="w-full text-left text-xs border-collapse">
            <thead>
              <tr className="border-b border-slate-800 bg-slate-900/60 text-slate-400 font-mono uppercase tracking-wider">
                <th className="py-3 px-4">Datum</th>
                <th className="py-3 px-4">Posten / Gegenstand</th>
                <th className="py-3 px-4">Schiff</th>
                <th className="py-3 px-4 text-right">Aufwand (aUEC)</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/40 font-mono">
              {(!data?.topExpenses || data.topExpenses.length === 0) ? (
                <tr>
                  <td colSpan={4} className="py-12 text-center text-slate-500">
                    Keine Ausgaben verzeichnet.
                  </td>
                </tr>
              ) : (
                data.topExpenses.map((item) => (
                  <tr key={item.id} className="hover:bg-slate-900/40 transition">
                    <td className="py-2.5 px-4 text-slate-400">{item.timestamp}</td>
                    <td className="py-2.5 px-4 font-sans font-semibold text-slate-300">
                      {item.description}
                    </td>
                    <td className="py-2.5 px-4 text-slate-400">{item.ship || '—'}</td>
                    <td className="py-2.5 px-4 text-right font-bold text-rose-400">
                      -{formatNumber(Math.abs(item.amount || 0))}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};
