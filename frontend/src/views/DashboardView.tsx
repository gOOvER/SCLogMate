import React from 'react';
import { AppStatus, LogEventItem, SessionSummary } from '../services/photinoBridge';
import {
  Activity,
  Box,
  Coins,
  Layers,
  Radio,
  Rocket,
  TrendingDown,
  TrendingUp,
} from 'lucide-react';

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
  const formatNumber = (num?: number) => {
    if (num === undefined || num === null) return '0';
    return num.toLocaleString('de-DE');
  };

  return (
    <div className="flex flex-col h-full space-y-4 overflow-y-auto pr-1">
      {/* 4 Metric Top Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        {/* Net Earnings */}
        <div
          onClick={() => onNavigate('finances')}
          className="sc-glass rounded-lg p-4 relative overflow-hidden sc-hud-corner cursor-pointer hover:border-cyan-500/40 transition"
        >
          <div className="flex justify-between items-start">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Finanzsaldo Netto
            </span>
            <Coins className="w-4 h-4 text-cyan-400" />
          </div>
          <div className="mt-2 text-2xl font-bold font-mono text-cyan-300 drop-shadow-[0_0_8px_rgba(0,240,255,0.3)]">
            {formatNumber(status?.totalNet)} <span className="text-xs font-normal text-cyan-500">aUEC</span>
          </div>
          <div className="mt-1 flex items-center text-xs text-slate-400 gap-2">
            <span className="text-emerald-400 flex items-center gap-0.5">
              <TrendingUp className="w-3 h-3" /> +{formatNumber(status?.totalIncome)}
            </span>
            <span>·</span>
            <span className="text-rose-400 flex items-center gap-0.5">
              <TrendingDown className="w-3 h-3" /> -{formatNumber(status?.totalSpend)}
            </span>
          </div>
        </div>

        {/* Sessions */}
        <div
          onClick={() => onNavigate('sessions')}
          className="sc-glass rounded-lg p-4 relative overflow-hidden sc-hud-corner cursor-pointer hover:border-amber-500/40 transition"
        >
          <div className="flex justify-between items-start">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Flugsitzungen
            </span>
            <Layers className="w-4 h-4 text-amber-400" />
          </div>
          <div className="mt-2 text-2xl font-bold font-mono text-amber-300">
            {status?.dbSessionCount ?? sessions.length}{' '}
            <span className="text-xs font-normal text-slate-400">indexiert</span>
          </div>
          <div className="mt-1 text-xs text-slate-400 truncate">
            Aktiv: {status?.activeSessionName || 'Keine laufende Session'}
          </div>
        </div>

        {/* Warehouse Inventory */}
        <div
          onClick={() => onNavigate('warehouse')}
          className="sc-glass rounded-lg p-4 relative overflow-hidden sc-hud-corner cursor-pointer hover:border-cyan-500/40 transition"
        >
          <div className="flex justify-between items-start">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Lagerbestand (Warehouse)
            </span>
            <Box className="w-4 h-4 text-cyan-400" />
          </div>
          <div className="mt-2 text-2xl font-bold font-mono text-cyan-300">
            {warehouseTotal} <span className="text-xs font-normal text-slate-400">Gegenstände</span>
          </div>
          <div className="mt-1 text-xs text-slate-400">
            Persistent an Planeten & Stationen gelagert
          </div>
        </div>

        {/* Flotte */}
        <div
          onClick={() => onNavigate('fleet')}
          className="sc-glass rounded-lg p-4 relative overflow-hidden sc-hud-corner cursor-pointer hover:border-indigo-500/40 transition"
        >
          <div className="flex justify-between items-start">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Flotte & Schiffe
            </span>
            <Rocket className="w-4 h-4 text-indigo-400" />
          </div>
          <div className="mt-2 text-2xl font-bold font-mono text-indigo-300">
            {Array.from(new Set(sessions.flatMap((s) => s.ships || []))).length}{' '}
            <span className="text-xs font-normal text-slate-400">Schiffe im Einsatz</span>
          </div>
          <div className="mt-1 text-xs text-slate-400">
            QT-Sprünge, Einsätze & Schiffsverluste
          </div>
        </div>
      </div>

      {/* Dual Column Bottom: Recent Sessions & Live Feed */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 flex-1">
        {/* Recent Sessions */}
        <div className="sc-glass rounded-lg p-4 border border-slate-800 flex flex-col">
          <div className="flex items-center justify-between pb-3 border-b border-slate-800">
            <span className="text-xs font-semibold uppercase tracking-wider text-cyan-300 flex items-center gap-2">
              <Layers className="w-4 h-4 text-cyan-400" /> Letzte Sessions
            </span>
            <button
              onClick={() => onNavigate('sessions')}
              className="text-xs text-cyan-400 hover:text-cyan-300 cursor-pointer font-medium"
            >
              Alle anzeigen →
            </button>
          </div>

          <div className="flex-1 overflow-y-auto mt-3 space-y-2">
            {sessions.slice(0, 6).map((s) => (
              <div
                key={s.id}
                onClick={() => onNavigate('sessions')}
                className="p-2.5 rounded bg-slate-900/50 border border-slate-800/80 hover:border-cyan-500/30 transition cursor-pointer flex items-center justify-between"
              >
                <div>
                  <div className="font-semibold text-xs text-slate-200">{s.name}</div>
                  <div className="text-[10px] text-slate-400 font-mono">
                    {s.startTime} · Dauer {s.duration}
                  </div>
                </div>
                <div className="text-right font-mono text-xs">
                  <div
                    className={`font-bold ${
                      s.net >= 0 ? 'text-cyan-300' : 'text-rose-400'
                    }`}
                  >
                    {formatNumber(s.net)} aUEC
                  </div>
                  <div className="text-[10px] text-slate-500">{s.lastLocation || '—'}</div>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Live Event Activity */}
        <div className="sc-glass rounded-lg p-4 border border-slate-800 flex flex-col">
          <div className="flex items-center justify-between pb-3 border-b border-slate-800">
            <span className="text-xs font-semibold uppercase tracking-wider text-amber-300 flex items-center gap-2">
              <Activity className="w-4 h-4 text-amber-400" /> Live Ereignis-Stream
            </span>
            <button
              onClick={() => onNavigate('events')}
              className="text-xs text-cyan-400 hover:text-cyan-300 cursor-pointer font-medium"
            >
              Zur Chronik →
            </button>
          </div>

          <div className="flex-1 overflow-y-auto mt-3 space-y-2">
            {events.length === 0 ? (
              <div className="py-12 text-center text-slate-500 font-mono text-xs">
                <Radio className="w-6 h-6 text-cyan-500/40 mx-auto mb-2 animate-pulse" />
                Warte auf neue Live-Ereignisse...
              </div>
            ) : (
              events.slice(0, 6).map((e) => (
                <div
                  key={e.id}
                  onClick={() => onNavigate('events')}
                  className="p-2.5 rounded bg-slate-900/50 border border-slate-800/80 hover:border-cyan-500/30 transition cursor-pointer flex items-center justify-between"
                >
                  <div className="flex items-center gap-2.5">
                    <span className="text-[10px] font-mono text-slate-500">{e.timestamp}</span>
                    <div>
                      <div className="font-semibold text-xs text-slate-200">{e.title}</div>
                      <div className="text-[11px] text-slate-400 truncate max-w-[240px]">
                        {e.description}
                      </div>
                    </div>
                  </div>
                  {e.amount !== undefined && e.amount !== null && e.amount !== 0 && (
                    <span
                      className={`font-mono text-xs font-bold ${
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
