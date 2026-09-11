import React from 'react';
import { AppStatus, LogEventItem, SessionSummary } from '../services/photinoBridge';
import {
  Activity,
  Box,
  Coins,
  ExternalLink,
  Layers,
  Radio,
  Rocket,
  Scroll,
  TrendingDown,
  TrendingUp,
} from 'lucide-react';
import { useI18n } from '../i18n';

interface DashboardViewProps {
  status: AppStatus | null;
  sessions: SessionSummary[];
  events: LogEventItem[];
  onNavigate: (tab: any) => void;
  warehouseTotal?: number;
}

export const DashboardView: React.FC<DashboardViewProps> = ({
  status,
  sessions,
  events,
  onNavigate,
  warehouseTotal = 0,
}) => {
  const { t } = useI18n();

  const formatNumber = (num?: number) => {
    if (num === undefined || num === null) return '0';
    return num.toLocaleString('de-DE');
  };

  const uniqueShips = Array.from(new Set(sessions.flatMap((s) => s.ships || [])));

  const getCategoryBadge = (category: string) => {
    switch (category) {
      case 'wallet':
        return (
          <span className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-mono font-bold bg-emerald-950/60 border border-emerald-800/60 text-emerald-300">
            💰 Finanzen
          </span>
        );
      case 'combat':
        return (
          <span className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-mono font-bold bg-rose-950/60 border border-rose-800/60 text-rose-300">
            ⚔️ Kampf
          </span>
        );
      case 'mission':
        return (
          <span className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-mono font-bold bg-amber-950/60 border border-amber-800/60 text-amber-300">
            🎯 Auftrag
          </span>
        );
      case 'ship':
        return (
          <span className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-mono font-bold bg-sky-950/60 border border-sky-800/60 text-sky-300">
            🚀 Schiff
          </span>
        );
      case 'location':
        return (
          <span className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-mono font-bold bg-cyan-950/60 border border-cyan-800/60 text-cyan-300">
            📍 Ort
          </span>
        );
      default:
        return (
          <span className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-mono font-bold bg-slate-900 border border-slate-800 text-slate-400">
            ⚙️ System
          </span>
        );
    }
  };

  return (
    <div className="flex flex-col h-full space-y-3 font-sans select-none overflow-hidden">
      {/* ══ 4 METRIC TOP CARDS ══ */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-2.5 shrink-0">
        {/* Finanzsaldo Netto */}
        <div
          onClick={() => onNavigate('finances')}
          className="bg-[#051122]/90 border border-cyan-950/80 rounded-lg p-2.5 cursor-pointer hover:border-cyan-500/40 transition shadow-sm"
        >
          <div className="flex justify-between items-start">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
              {t('dashboard.financialNet')}
            </span>
            <Coins className="w-3.5 h-3.5 text-cyan-400" />
          </div>
          <div className="mt-1 text-xl font-bold font-mono text-cyan-300">
            {formatNumber(status?.totalNet)} <span className="text-[10px] font-normal text-cyan-500 font-sans">aUEC</span>
          </div>
          <div className="mt-1 flex items-center text-[10px] font-mono text-slate-400 gap-2">
            <span className="text-emerald-400 flex items-center gap-0.5">
              <TrendingUp className="w-2.5 h-2.5" /> +{formatNumber(status?.totalIncome)}
            </span>
            <span>·</span>
            <span className="text-rose-400 flex items-center gap-0.5">
              <TrendingDown className="w-2.5 h-2.5" /> -{formatNumber(status?.totalSpend)}
            </span>
          </div>
        </div>

        {/* Lagerbestand */}
        <div
          onClick={() => onNavigate('warehouse')}
          className="bg-[#051122]/90 border border-cyan-950/80 rounded-lg p-2.5 cursor-pointer hover:border-cyan-500/40 transition shadow-sm"
        >
          <div className="flex justify-between items-start">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
              {t('dashboard.warehouse')}
            </span>
            <Box className="w-3.5 h-3.5 text-cyan-400" />
          </div>
          <div className="mt-1 text-xl font-bold font-mono text-cyan-300">
            {warehouseTotal} <span className="text-[10px] font-normal text-slate-400 font-sans">Items</span>
          </div>
          <div className="text-[10px] font-mono text-slate-500 mt-1 truncate">
            {t('dashboard.itemsStored')}
          </div>
        </div>

        {/* Flotte & Hangar */}
        <div
          onClick={() => onNavigate('fleet')}
          className="bg-[#051122]/90 border border-cyan-950/80 rounded-lg p-2.5 cursor-pointer hover:border-indigo-500/40 transition shadow-sm"
        >
          <div className="flex justify-between items-start">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
              {t('dashboard.fleet')}
            </span>
            <Rocket className="w-3.5 h-3.5 text-indigo-400" />
          </div>
          <div className="mt-1 text-xl font-bold font-mono text-indigo-300">
            {uniqueShips.length} <span className="text-[10px] font-normal text-slate-400 font-sans">Schiffe</span>
          </div>
          <div className="text-[10px] font-mono text-slate-500 mt-1 truncate">
            {t('dashboard.shipsInService')}
          </div>
        </div>

        {/* Aktive Session Telemetrie */}
        <div
          onClick={() => onNavigate('sessions')}
          className="bg-[#051122]/90 border border-cyan-950/80 rounded-lg p-2.5 cursor-pointer hover:border-amber-500/40 transition shadow-sm"
        >
          <div className="flex justify-between items-start">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
              {t('dashboard.flightSessions')}
            </span>
            <Layers className="w-3.5 h-3.5 text-amber-400" />
          </div>
          <div className="mt-1 text-xl font-bold font-mono text-amber-300">
            {status?.dbSessionCount ?? sessions.length} <span className="text-[10px] font-normal text-slate-400 font-sans">Sessions</span>
          </div>
          <div className="text-[10px] font-mono text-slate-500 mt-1 truncate">
            {status?.activeSessionName ? `● ${status.activeSessionName}` : 'Keine aktive Session'}
          </div>
        </div>
      </div>

      {/* ══ FULL-WIDTH LIVE EVENT STREAM ══ */}
      <div className="flex-1 bg-[#040914]/90 rounded-lg border border-cyan-950/80 flex flex-col overflow-hidden shadow-sm min-h-0">
        <div className="p-2.5 border-b border-cyan-950 bg-[#061224] flex items-center justify-between shrink-0">
          <div className="flex items-center gap-2.5">
            <span className="text-xs font-mono font-bold uppercase tracking-wider text-amber-300 flex items-center gap-2">
              <Activity className="w-3.5 h-3.5 text-amber-400" /> {t('dashboard.liveEvents')}
            </span>
            <span className="flex items-center gap-1.5 px-2 py-0.5 rounded bg-emerald-950/40 border border-emerald-800/40 text-[10px] font-mono text-emerald-300">
              <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
              LIVE TAIL
            </span>
            <span className="text-[10.5px] font-mono text-slate-500 hidden sm:inline">
              ({events.length} Ereignisse in aktueller Session)
            </span>
          </div>

          <button
            onClick={() => onNavigate('events')}
            className="flex items-center gap-1.5 px-2.5 py-1 rounded bg-[#071322] hover:bg-cyan-950/60 border border-cyan-900/60 hover:border-cyan-700 text-xs font-mono text-cyan-300 hover:text-cyan-100 transition cursor-pointer"
          >
            <Scroll className="w-3 h-3 text-cyan-400" />
            <span>Zur Chronik</span>
            <ExternalLink className="w-2.5 h-2.5" />
          </button>
        </div>

        {/* Events Table */}
        <div className="flex-1 overflow-y-auto">
          {events.length === 0 ? (
            <div className="py-24 text-center text-slate-500 font-mono text-xs">
              <Radio className="w-6 h-6 text-cyan-500/40 mx-auto mb-2 animate-pulse" />
              {t('dashboard.noEventsYet')}
            </div>
          ) : (
            <table className="w-full text-left text-xs border-collapse font-mono">
              <thead>
                <tr className="border-b border-cyan-950 bg-[#061224] text-slate-400 text-[10px] font-bold uppercase tracking-wider sticky top-0 backdrop-blur-md z-10">
                  <th className="py-2 px-3 w-28">ZEIT</th>
                  <th className="py-2 px-3 w-28">TYP</th>
                  <th className="py-2 px-3 font-sans">DETAIL</th>
                  <th className="py-2 px-3 w-36 hidden md:table-cell">SCHIFF</th>
                  <th className="py-2 px-4 text-right w-36">BETRAG</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-cyan-950/40">
                {events.map((e) => (
                  <tr
                    key={e.id}
                    onClick={() => onNavigate('events')}
                    className="hover:bg-[#071628]/60 transition-colors cursor-pointer group"
                  >
                    <td className="py-2 px-3 text-slate-400 text-[11px] whitespace-nowrap">
                      {e.timestamp}
                    </td>
                    <td className="py-2 px-3 whitespace-nowrap">
                      {getCategoryBadge(e.category)}
                    </td>
                    <td className="py-2 px-3 font-sans text-xs">
                      <div className="font-semibold text-slate-200 group-hover:text-cyan-300 transition">
                        {e.title}
                      </div>
                      {e.description && (
                        <div className="text-[11px] text-slate-400 truncate max-w-xl">
                          {e.description}
                        </div>
                      )}
                    </td>
                    <td className="py-2 px-3 hidden md:table-cell text-slate-400 text-[11px] truncate max-w-[140px]">
                      {e.ship || '—'}
                    </td>
                    <td className="py-2 px-4 text-right whitespace-nowrap font-mono font-bold">
                      {e.amount !== undefined && e.amount !== null && e.amount !== 0 ? (
                        <span className={e.amount > 0 ? 'text-emerald-400' : 'text-rose-400'}>
                          {e.amount > 0 ? '+' : ''}
                          {formatNumber(e.amount)} aUEC
                        </span>
                      ) : (
                        <span className="text-slate-600">—</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  );
};

export default DashboardView;

