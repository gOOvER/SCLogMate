import React from 'react';
import { ChevronDown, ChevronUp, Clock, Zap } from 'lucide-react';
import { SessionSummary } from '../services/photinoBridge';
import { useI18n } from '../i18n';

interface SessionBarProps {
  sessions: SessionSummary[];
  selectedSession: string;
  sessionSpanText: string;
  isHudCollapsed: boolean;
  isDashboard?: boolean;
  activeSessionName?: string;
  onSelectSession: (sessionName: string) => void;
  onToggleHudCollapsed: () => void;
}

export const SessionBar: React.FC<SessionBarProps> = ({
  sessions,
  selectedSession,
  sessionSpanText,
  isHudCollapsed,
  isDashboard = false,
  activeSessionName,
  onSelectSession,
  onToggleHudCollapsed,
}) => {
  const { t } = useI18n();

  return (
    <div className="flex items-center justify-between px-5 py-2 border-b border-cyan-950/50 bg-[#030814]/90 z-15 shrink-0 gap-3 text-xs">
      {/* Session Label & Dropdown or Live Badge on Dashboard */}
      <div className="flex items-center gap-2.5 flex-1 min-w-0">
        {isDashboard ? (
          <div className="flex items-center gap-2.5 min-w-0">
            <div className="flex items-center gap-2 px-2.5 py-1 rounded bg-[#061426] border border-cyan-800/60 shadow-[0_0_10px_rgba(6,182,212,0.15)]">
              <span className="relative flex h-2 w-2">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
                <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
              </span>
              <span className="text-emerald-400 font-bold font-mono tracking-wider text-xs">
                LIVE-SITZUNG
              </span>
            </div>
            <span className="text-slate-300 font-mono text-xs truncate">
              {activeSessionName || 'Game.log'} <span className="text-slate-500 hidden sm:inline">(Aktuelle Star Citizen Spielsitzung)</span>
            </span>
          </div>
        ) : (
          <>
            <div className="flex items-center gap-1.5 text-cyan-400 font-bold font-mono tracking-wider shrink-0">
              <Zap className="w-3.5 h-3.5 text-cyan-400 fill-cyan-400/20" />
              <span>{t('sessionBar.activeSession').toUpperCase()}:</span>
            </div>

            {/* Dropdown */}
            <div className="relative flex-1 max-w-xl">
              <select
                value={selectedSession}
                onChange={(e) => onSelectSession(e.target.value)}
                className="w-full appearance-none bg-[#071322] border border-cyan-900/60 hover:border-cyan-700/80 rounded px-3 py-1.5 pr-8 text-xs font-mono text-slate-200 focus:outline-none focus:border-cyan-500 transition cursor-pointer"
              >
                <option value="__live__">{t('sessionBar.liveStream')}</option>
                <option value="__all__">{t('sessionBar.allSessions')}</option>
                {sessions.map((s) => (
                  <option key={s.id || s.name} value={s.name}>
                    📁 {s.name} ({s.startTime} · {s.duration})
                  </option>
                ))}
              </select>
              <ChevronDown className="w-3.5 h-3.5 text-slate-400 absolute right-2.5 top-1/2 -translate-y-1/2 pointer-events-none" />
            </div>
          </>
        )}
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
