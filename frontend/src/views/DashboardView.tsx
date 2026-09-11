import React from 'react';
import { AppStatus, LogEventItem, SessionSummary } from '../services/photinoBridge';
import {
  Activity,
  Box,
  Coins,
  Compass,
  Layers,
  Radio,
  Rocket,
  ShieldCheck,
  Target,
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

  const activeSession = sessions.find((s) => s.name === status?.activeSessionName) || sessions[0];
  const uniqueShips = Array.from(new Set(sessions.flatMap((s) => s.ships || [])));

  return (
    <div className="flex flex-col h-full space-y-3 font-sans select-none overflow-y-auto pr-1">
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

      {/* ══ DUAL COLUMN: FLIGHT DECK TELEMETRIE & LIVE FEED ══ */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-3 flex-1 min-h-0">
        {/* Linke Spalte: Flight Deck & Telemetrie */}
        <div className="bg-[#040914]/90 rounded-lg border border-cyan-950/80 flex flex-col overflow-hidden shadow-sm">
          <div className="p-2.5 border-b border-cyan-950 bg-[#061224] flex items-center justify-between shrink-0">
            <div className="flex items-center gap-2 text-xs font-mono font-bold uppercase tracking-wider text-cyan-300">
              <Compass className="w-3.5 h-3.5 text-cyan-400" /> {t('dashboard.flightDeck')}
            </div>
            <span className="text-[10px] font-mono text-slate-500">
              {activeSession?.lastLocation || 'Stanton'} · Live
            </span>
          </div>

          <div className="p-3.5 flex-1 overflow-y-auto space-y-3">
            {/* Schiff & Status */}
            <div className="p-3 rounded-lg bg-[#071322] border border-cyan-950/80 flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="w-9 h-9 rounded bg-cyan-950/50 border border-cyan-800/40 flex items-center justify-center text-cyan-400">
                  <Rocket className="w-4 h-4" />
                </div>
                <div>
                  <div className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
                    {t('dashboard.currentShip')}
                  </div>
                  <div className="text-sm font-bold text-slate-100 font-sans">
                    {uniqueShips[0] || 'Kein Schiff aktiv'}
                  </div>
                </div>
              </div>
              <button
                onClick={() => onNavigate('fleet')}
                className="px-2.5 py-1 rounded bg-[#0a1b33] hover:bg-cyan-950/80 border border-cyan-800/40 text-[11px] font-mono text-cyan-300 transition cursor-pointer"
              >
                Hangar →
              </button>
            </div>

            {/* Standort & Sicherheitszone */}
            <div className="p-3 rounded-lg bg-[#071322] border border-cyan-950/80 flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="w-9 h-9 rounded bg-emerald-950/50 border border-emerald-800/40 flex items-center justify-center text-emerald-400">
                  <ShieldCheck className="w-4 h-4" />
                </div>
                <div>
                  <div className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
                    {t('dashboard.currentLocation')}
                  </div>
                  <div className="text-sm font-bold text-slate-100 font-sans">
                    {activeSession?.lastLocation || 'Stanton Orbit'}
                  </div>
                  <div className="text-[10px] font-mono text-emerald-400 mt-0.5 flex items-center gap-1">
                    <span className="w-1.5 h-1.5 rounded-full bg-emerald-400" />
                    <span>{t('dashboard.safeZone')}</span>
                  </div>
                </div>
              </div>
              <button
                onClick={() => onNavigate('starmap')}
                className="px-2.5 py-1 rounded bg-[#0a1b33] hover:bg-cyan-950/80 border border-cyan-800/40 text-[11px] font-mono text-cyan-300 transition cursor-pointer"
              >
                Starmap →
              </button>
            </div>

            {/* Aktiver Auftrag */}
            <div className="p-3 rounded-lg bg-[#071322] border border-cyan-950/80 flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="w-9 h-9 rounded bg-amber-950/50 border border-amber-800/40 flex items-center justify-center text-amber-400">
                  <Target className="w-4 h-4" />
                </div>
                <div>
                  <div className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
                    {t('dashboard.activeContract')}
                  </div>
                  <div className="text-xs font-semibold text-slate-200 font-sans">
                    {events.find((e) => e.category === 'mission')?.title || 'Bereit für neue Aufträge'}
                  </div>
                  <div className="text-[10px] font-mono text-slate-500 mt-0.5">
                    mobiGlas Contract Manager
                  </div>
                </div>
              </div>
              <button
                onClick={() => onNavigate('missions')}
                className="px-2.5 py-1 rounded bg-[#0a1b33] hover:bg-amber-950/80 border border-amber-800/40 text-[11px] font-mono text-amber-300 transition cursor-pointer"
              >
                Aufträge →
              </button>
            </div>
          </div>
        </div>

        {/* Rechte Spalte: Live Event Feed */}
        <div className="bg-[#040914]/90 rounded-lg border border-cyan-950/80 flex flex-col overflow-hidden shadow-sm">
          <div className="p-2.5 border-b border-cyan-950 bg-[#061224] flex items-center justify-between shrink-0">
            <div className="flex items-center gap-2 text-xs font-mono font-bold uppercase tracking-wider text-amber-300">
              <Activity className="w-3.5 h-3.5 text-amber-400" /> {t('dashboard.liveEvents')}
            </div>
            <button
              onClick={() => onNavigate('events')}
              className="text-[11px] font-mono text-cyan-400 hover:text-cyan-200 cursor-pointer transition"
            >
              {t('common.showAll')}
            </button>
          </div>

          <div className="flex-1 overflow-y-auto p-2.5 space-y-1.5">
            {events.length === 0 ? (
              <div className="py-16 text-center text-slate-500 font-mono text-xs">
                <Radio className="w-5 h-5 text-cyan-500/40 mx-auto mb-2 animate-pulse" />
                {t('dashboard.noEventsYet')}
              </div>
            ) : (
              events.slice(0, 8).map((e) => (
                <div
                  key={e.id}
                  onClick={() => onNavigate('events')}
                  className="p-2 rounded bg-[#071322] border border-cyan-950/60 hover:border-cyan-700/60 transition cursor-pointer flex items-center justify-between group"
                >
                  <div className="flex items-center gap-2.5 min-w-0">
                    <span className="text-[10px] font-mono text-slate-500 shrink-0">{e.timestamp}</span>
                    <div className="min-w-0">
                      <div className="font-semibold text-xs text-slate-200 group-hover:text-cyan-300 transition truncate">
                        {e.title}
                      </div>
                      <div className="text-[10.5px] font-sans text-slate-400 truncate max-w-sm">
                        {e.description}
                      </div>
                    </div>
                  </div>
                  {e.amount !== undefined && e.amount !== null && e.amount !== 0 && (
                    <span
                      className={`font-mono text-xs font-bold shrink-0 ml-2 ${
                        e.amount > 0 ? 'text-emerald-400' : 'text-rose-400'
                      }`}
                    >
                      {e.amount > 0 ? '+' : ''}
                      {formatNumber(e.amount)}
                    </span>
                  )}
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default DashboardView;

