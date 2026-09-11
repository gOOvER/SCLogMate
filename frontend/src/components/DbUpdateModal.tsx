import React, { useState, useEffect } from 'react';
import { Database, CheckCircle2, RefreshCw, ShieldCheck } from 'lucide-react';
import { ScanProgress } from '../services/photinoBridge';

interface DbUpdateModalProps {
  progress: ScanProgress | null;
  onDismiss?: () => void;
}

export const DbUpdateModal: React.FC<DbUpdateModalProps> = ({ progress, onDismiss }) => {
  const [dismissed, setDismissed] = useState(false);
  const [countdown, setCountdown] = useState<number>(3);

  useEffect(() => {
    if (progress && !progress.isCompleted) {
      setDismissed(false);
      setCountdown(3);
    } else if (progress?.isCompleted) {
      setCountdown(3);
      const interval = setInterval(() => {
        setCountdown((prev) => {
          if (prev <= 1) {
            clearInterval(interval);
            setDismissed(true);
            if (onDismiss) onDismiss();
            return 0;
          }
          return prev - 1;
        });
      }, 1000);
      return () => clearInterval(interval);
    }
  }, [progress?.isCompleted]);

  if (!progress || dismissed) return null;

  const handleClose = () => {
    setDismissed(true);
    if (onDismiss) onDismiss();
  };

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center bg-black/80 backdrop-blur-md p-4 animate-in fade-in duration-200">
      <div className="sc-glass rounded-2xl w-full max-w-lg border border-cyan-500/40 shadow-2xl shadow-cyan-950/60 p-6 relative overflow-hidden">
        {/* Glow Header Accent */}
        <div className="absolute top-0 left-0 right-0 h-1 bg-gradient-to-r from-transparent via-cyan-400 to-transparent" />

        {/* Title & Icon */}
        <div className="flex items-center space-x-3 mb-4">
          <div className="w-12 h-12 rounded-xl bg-cyan-500/10 border border-cyan-500/40 flex items-center justify-center text-cyan-400 shadow-[0_0_15px_rgba(6,182,212,0.3)] shrink-0">
            {progress.isCompleted ? (
              <CheckCircle2 className="w-6 h-6 text-emerald-400" />
            ) : (
              <Database className="w-6 h-6 animate-pulse" />
            )}
          </div>
          <div>
            <div className="flex items-center space-x-2">
              <h2 className="text-base font-bold text-white tracking-wide uppercase">
                {progress.isCompleted ? 'Synchronisation abgeschlossen' : 'Datenbank-Aktualisierung'}
              </h2>
              <span className="px-2 py-0.5 rounded text-[10px] font-mono font-bold bg-cyan-950/80 border border-cyan-500/50 text-cyan-300">
                AUTO-SYNC
              </span>
            </div>
            <p className="text-xs text-cyan-300/80 font-mono mt-0.5">
              {progress.updateReason || 'Star Citizen Logs werden eingelesen'}
            </p>
          </div>
        </div>

        {/* Info Card */}
        <div className="p-3 rounded-lg bg-slate-900/60 border border-slate-800 text-xs text-slate-300 space-y-1.5 mb-5 font-sans">
          <div className="flex items-center space-x-2 text-cyan-300 font-medium">
            <ShieldCheck className="w-4 h-4 text-cyan-400 shrink-0" />
            <span>Intelligente Hash-Indexierung</span>
          </div>
          <p className="text-[11px] text-slate-400 leading-relaxed font-mono">
            Bereits indexierte Logdateien werden anhand ihres Fingerprints blitzschnell übersprungen. Nur neue oder veränderte Sitzungen werden neu eingelesen.
          </p>
        </div>

        {/* Progress Display */}
        <div className="space-y-3 mb-5">
          <div className="flex items-center justify-between text-xs font-mono">
            <span className="text-slate-400">
              {progress.total > 0
                ? `Datei ${progress.current} von ${progress.total}`
                : 'Analysiere Verzeichnisse...'}
            </span>
            <span className="font-bold text-cyan-400">
              {progress.total > 0 ? `${progress.percent}%` : 'Wird vorbereitet...'}
            </span>
          </div>

          {/* Progress Bar */}
          <div className="w-full bg-slate-950 rounded-full h-3 overflow-hidden border border-cyan-900/60 p-0.5 shadow-inner">
            <div
              className={`h-full rounded-full transition-all duration-300 ${
                progress.isCompleted
                  ? 'bg-emerald-500 shadow-[0_0_12px_rgba(16,185,129,0.8)]'
                  : 'bg-gradient-to-r from-cyan-600 via-cyan-400 to-sky-400 shadow-[0_0_12px_rgba(6,182,212,0.8)]'
              }`}
              style={{
                width: progress.total > 0 ? `${Math.max(4, progress.percent)}%` : '20%',
              }}
            />
          </div>

          {/* Current File */}
          <div className="flex items-center space-x-2 text-[11px] font-mono text-slate-400 truncate">
            {!progress.isCompleted && (
              <RefreshCw className="w-3 h-3 text-cyan-400 animate-spin shrink-0" />
            )}
            <span className="truncate">
              {progress.currentFileName || 'Suche Star Citizen Log-Dateien...'}
            </span>
          </div>
        </div>

        {/* Completed Stats or Action */}
        {progress.isCompleted ? (
          <div className="flex items-center justify-between pt-2 border-t border-slate-800">
            <div className="text-[11px] font-mono text-emerald-400 flex items-center space-x-1.5">
              <CheckCircle2 className="w-4 h-4 shrink-0" />
              <span>
                {progress.indexedSessions ?? progress.total} Sessions erfolgreich synchronisiert
              </span>
            </div>
            <button
              onClick={handleClose}
              className="px-4 py-1.5 rounded-lg bg-cyan-600 hover:bg-cyan-500 text-white font-bold text-xs shadow-lg shadow-cyan-600/30 transition cursor-pointer flex items-center space-x-1.5"
            >
              <span>Weiter</span>
              <span className="text-cyan-200 text-[10px]">({countdown}s)</span>
            </button>
          </div>
        ) : (
          <div className="text-right">
            <span className="text-[11px] font-mono text-slate-500">
              Bitte kurz warten...
            </span>
          </div>
        )}
      </div>
    </div>
  );
};
