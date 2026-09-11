import React from 'react';
import { ChevronDown, ChevronUp, Clock } from 'lucide-react';

interface SessionBarProps {
  sessionSpanText: string;
  isHudCollapsed: boolean;
  activeSessionName?: string;
  onToggleHudCollapsed: () => void;
}

export const SessionBar: React.FC<SessionBarProps> = ({
  sessionSpanText,
  isHudCollapsed,
  activeSessionName,
  onToggleHudCollapsed,
}) => {
  return (
    <div className="flex items-center justify-between px-5 py-2 border-b border-cyan-950/50 bg-[#030814]/90 z-15 shrink-0 gap-3 text-xs">
      {/* Aktive Live-Sitzung Status */}
      <div className="flex items-center gap-2.5 flex-1 min-w-0">
        <div className="flex items-center gap-2 px-2.5 py-1 rounded bg-[#061426] border border-cyan-800/60 shadow-[0_0_10px_rgba(6,182,212,0.15)]">
          <span className="relative flex h-2 w-2">
            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
            <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
          </span>
          <span className="text-emerald-400 font-bold font-mono tracking-wider text-xs">
            LIVE-STREAM
          </span>
        </div>
        <span className="text-slate-300 font-mono text-xs truncate">
          {activeSessionName || 'Game.log'}{' '}
          <span className="text-slate-500 hidden sm:inline">(Aktuelle Star Citizen Spielsitzung)</span>
        </span>
      </div>

      {/* Rechte Seite: Session Span Pill & HUD Collapse Toggle */}
      <div className="flex items-center gap-2.5 shrink-0">
        {/* Leuchtende Zeitspannen-Pille */}
        <div
          className="flex items-center gap-1.5 px-3 py-1 rounded bg-[#061426] border border-cyan-800/60 text-cyan-300 font-mono text-[11px] font-semibold shadow-[0_0_8px_rgba(6,182,212,0.15)]"
          title="Zeitspanne der aktuellen Sitzung"
        >
          <Clock className="w-3 h-3 text-cyan-400 shrink-0" />
          <span>{sessionSpanText || '—'}</span>
        </div>

        {/* HUD Collapse/Expand Button */}
        <button
          onClick={onToggleHudCollapsed}
          className={`flex items-center gap-1.5 px-2.5 py-1 rounded border text-xs font-mono font-medium transition cursor-pointer ${
            isHudCollapsed
              ? 'bg-cyan-950/40 border-cyan-700/60 text-cyan-300 hover:bg-cyan-900/50 shadow-sm'
              : 'bg-[#06101e] border-slate-800 text-slate-400 hover:text-slate-200 hover:border-slate-700'
          }`}
          title={isHudCollapsed ? '2-Zeilen HUD einblenden' : '2-Zeilen HUD einklappen (für mehr Arbeitsfläche)'}
        >
          {isHudCollapsed ? (
            <>
              <ChevronDown className="w-3.5 h-3.5 text-cyan-400" />
              <span>HUD einblenden</span>
            </>
          ) : (
            <>
              <ChevronUp className="w-3.5 h-3.5" />
              <span className="hidden sm:inline">HUD minimieren</span>
            </>
          )}
        </button>
      </div>
    </div>
  );
};
