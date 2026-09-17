import React, { useState } from 'react';
import { PipsEvaluationResult } from '../services/photinoBridge';

interface PipsAnalyzerBadgeProps {
  pipsResult?: PipsEvaluationResult | null;
  compact?: boolean;
  showTooltip?: boolean;
  className?: string;
}

export const PipsAnalyzerBadge: React.FC<PipsAnalyzerBadgeProps> = ({
  pipsResult,
  compact = false,
  showTooltip = true,
  className = '',
}) => {
  const [isHovered, setIsHovered] = useState(false);

  if (!pipsResult || pipsResult.rating === 'NoGuns' || (pipsResult.pipCount ?? 0) === 0) {
    return null;
  }

  const isSync = Boolean(pipsResult.isSynchronized || pipsResult.pipCount === 1);
  const pipCount = pipsResult.pipCount ?? 1;
  const speeds = Array.isArray(pipsResult.speedsMps) ? pipsResult.speedsMps : [];
  const guns = Array.isArray(pipsResult.guns) ? pipsResult.guns : [];

  // Colors & Badges based on projectile speed thresholds
  const bgClass = isSync
    ? 'bg-emerald-500/15 border-emerald-500/40 text-emerald-300 hover:bg-emerald-500/25'
    : pipCount === 2
    ? 'bg-amber-500/15 border-amber-500/40 text-amber-300 hover:bg-amber-500/25'
    : 'bg-rose-500/15 border-rose-500/40 text-rose-300 hover:bg-rose-500/25';

  const dotClass = isSync
    ? 'bg-emerald-400 shadow-[0_0_8px_rgba(52,211,153,0.8)]'
    : pipCount === 2
    ? 'bg-amber-400 shadow-[0_0_8px_rgba(251,191,36,0.8)]'
    : 'bg-rose-400 shadow-[0_0_8px_rgba(244,63,94,0.8)]';

  const label = isSync
    ? compact
      ? '1 Pip'
      : pipsResult.summaryBadge || '1 Pip · Synchr.'
    : compact
    ? `${pipCount} Pips`
    : pipsResult.summaryBadge || `${pipCount} Pips · Geteilt`;

  return (
    <div
      className={`relative inline-flex items-center ${className}`}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      <div
        className={`inline-flex items-center gap-1.5 px-2 py-0.5 rounded-md border text-[11px] font-mono font-medium transition-all duration-150 cursor-help ${bgClass}`}
        title={!showTooltip ? pipsResult.advice || pipsResult.summaryBadge : undefined}
      >
        <span className={`w-2 h-2 rounded-full shrink-0 animate-pulse ${dotClass}`} />
        <span>{label}</span>
      </div>

      {showTooltip && isHovered && (
        <div className="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-72 p-3 bg-slate-900/95 backdrop-blur-md border border-slate-700/80 rounded-lg shadow-2xl z-50 pointer-events-none text-left">
          <div className="flex items-center justify-between border-b border-slate-700/60 pb-1.5 mb-2">
            <span className="text-xs font-semibold text-slate-200 flex items-center gap-1.5">
              🎯 Lead-Pips Analyse
            </span>
            <span
              className={`text-[10px] font-mono px-1.5 py-0.2 rounded font-semibold ${
                isSync
                  ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/40'
                  : 'bg-amber-500/20 text-amber-300 border border-amber-500/40'
              }`}
            >
              {isSync ? 'Synchronisiert' : `${pipCount} Fadenkreuze`}
            </span>
          </div>

          {pipsResult.advice && (
            <p className="text-[11px] text-slate-300 leading-relaxed mb-2.5">
              {pipsResult.advice}
            </p>
          )}

          {speeds.length > 0 && (
            <div className="space-y-1 text-[10px] font-mono">
              <div className="text-slate-400 font-sans text-[10px] mb-0.5">Projektil-Geschwindigkeiten:</div>
              <div className="flex flex-wrap gap-1">
                {speeds.map((s, idx) => (
                  <span
                    key={idx}
                    className="px-1.5 py-0.5 rounded bg-slate-800 border border-slate-700 text-cyan-300"
                  >
                    {s.toLocaleString()} m/s
                  </span>
                ))}
              </div>
            </div>
          )}

          {guns.length > 0 && (
            <div className="mt-2 pt-2 border-t border-slate-800 space-y-1">
              <div className="text-slate-400 font-sans text-[10px]">Erkannte Bewaffnung:</div>
              {guns.map((g, idx) => (
                <div key={idx} className="flex items-center justify-between text-[10px] text-slate-300">
                  <span className="truncate max-w-[170px]" title={g.gunName}>
                    {g.gunName}
                  </span>
                  <span className="font-mono text-cyan-400">{g.speedMps} m/s</span>
                </div>
              ))}
            </div>
          )}

          <div className="mt-2 text-[9px] text-slate-500 italic">
            * Star Citizen berechnet separate Vorhaltepunkte (Pips) je nach Mündungsgeschwindigkeit.
          </div>
        </div>
      )}
    </div>
  );
};
