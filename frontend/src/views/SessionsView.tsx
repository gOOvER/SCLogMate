import React from 'react';
import { SessionSummary } from '../services/photinoBridge';
import { Compass, Layers } from 'lucide-react';

interface SessionsViewProps {
  sessions: SessionSummary[];
}

export const SessionsView: React.FC<SessionsViewProps> = ({ sessions }) => {
  const formatNumber = (num?: number) => {
    if (num === undefined || num === null) return '0';
    return num.toLocaleString('de-DE');
  };

  return (
    <div className="flex flex-col h-full space-y-4">
      <div className="sc-glass rounded-lg overflow-hidden border border-slate-800 flex-1 flex flex-col">
        <div className="p-3.5 border-b border-slate-800 bg-slate-900/60 flex items-center justify-between">
          <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-cyan-300">
            <Layers className="w-4 h-4 text-cyan-400" /> Aufgezeichnete Flugsitzungen ({sessions.length})
          </div>
          <span className="text-[11px] font-mono text-slate-400">
            Chronologisch aus Game.log & logbackups
          </span>
        </div>

        <div className="flex-1 overflow-y-auto">
          <table className="w-full text-left text-xs border-collapse">
            <thead>
              <tr className="border-b border-slate-800 bg-slate-900/60 text-slate-400 font-mono uppercase tracking-wider sticky top-0 backdrop-blur-md z-10">
                <th className="py-3 px-4">Session / Zeitfenster</th>
                <th className="py-3 px-4">Dauer</th>
                <th className="py-3 px-4 text-right">Einnahmen</th>
                <th className="py-3 px-4 text-right">Ausgaben</th>
                <th className="py-3 px-4 text-right">Netto</th>
                <th className="py-3 px-4">Schiffe</th>
                <th className="py-3 px-4">Letzter Standort</th>
                <th className="py-3 px-4 text-center">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/40 font-mono">
              {sessions.length === 0 ? (
                <tr>
                  <td colSpan={8} className="py-16 text-center text-slate-500">
                    Keine Sessions gefunden. Klicke oben auf »Logs scannen«.
                  </td>
                </tr>
              ) : (
                sessions.map((sess) => (
                  <tr
                    key={sess.id}
                    className="hover:bg-cyan-950/20 transition-colors group cursor-pointer"
                  >
                    <td className="py-3 px-4">
                      <div className="font-semibold text-slate-200 group-hover:text-cyan-300 transition font-sans">
                        {sess.name}
                      </div>
                      <div className="text-[10px] text-slate-400">
                        {sess.startTime} → {sess.endTime}
                      </div>
                    </td>
                    <td className="py-3 px-4 text-slate-300">{sess.duration}</td>
                    <td className="py-3 px-4 text-right text-emerald-400">
                      +{formatNumber(sess.income)}
                    </td>
                    <td className="py-3 px-4 text-right text-rose-400">
                      -{formatNumber(sess.spend)}
                    </td>
                    <td
                      className={`py-3 px-4 text-right font-bold ${
                        sess.net >= 0 ? 'text-cyan-300' : 'text-rose-400'
                      }`}
                    >
                      {formatNumber(sess.net)} aUEC
                    </td>
                    <td className="py-3 px-4 font-sans">
                      {sess.ships && sess.ships.length > 0 ? (
                        <div className="flex flex-wrap gap-1">
                          {sess.ships.map((sh, i) => (
                            <span
                              key={i}
                              className="px-1.5 py-0.5 rounded text-[10px] bg-cyan-950/60 border border-cyan-800/40 text-cyan-300 font-mono"
                            >
                              {sh}
                            </span>
                          ))}
                        </div>
                      ) : (
                        <span className="text-slate-600">—</span>
                      )}
                    </td>
                    <td className="py-3 px-4 text-slate-300 font-sans flex items-center gap-1.5">
                      <Compass className="w-3.5 h-3.5 text-cyan-400 shrink-0" />
                      <span className="truncate max-w-[180px]">{sess.lastLocation || '—'}</span>
                    </td>
                    <td className="py-3 px-4 text-center font-sans">
                      {sess.deaths > 0 ? (
                        <span className="sc-badge-red" title={`${sess.deaths}x Med-Bett`}>
                          {sess.deaths} Verlust
                        </span>
                      ) : (
                        <span className="sc-badge-green">Erfolgreich</span>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
