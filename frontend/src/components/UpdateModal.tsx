import React, { useState, useEffect } from 'react';
import { Rocket, ExternalLink, RefreshCw, X, ArrowRight } from 'lucide-react';
import { UpdateInfoDto, bridge } from '../services/photinoBridge';

interface UpdateModalProps {
  isOpen: boolean;
  updateInfo: UpdateInfoDto | null;
  onClose: () => void;
}

export const UpdateModal: React.FC<UpdateModalProps> = ({ isOpen, updateInfo, onClose }) => {
  const [isInstalling, setIsInstalling] = useState(false);
  const [installStatus, setInstallStatus] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    // Listen for progress updates during install
    const unbind = bridge.on<{ status: string }>('UPDATE_INSTALLING', (data) => {
      setIsInstalling(true);
      if (data?.status) {
        setInstallStatus(data.status);
      }
    });

    return () => {
      unbind();
    };
  }, []);

  if (!isOpen || !updateInfo || !updateInfo.updateAvailable) {
    return null;
  }

  const handleApply = async () => {
    try {
      setIsInstalling(true);
      setErrorMessage(null);
      setInstallStatus(`Lade Update ${updateInfo.newVersion} herunter...`);
      const res = await bridge.applyUpdate();
      if (!res.success && res.message) {
        setErrorMessage(res.message);
        setIsInstalling(false);
      }
    } catch (err: any) {
      console.error('Failed to apply update:', err);
      setErrorMessage(err?.message || 'Fehler beim Installieren des Updates.');
      setIsInstalling(false);
    }
  };

  const handleOpenGitHub = () => {
    const url = updateInfo.htmlUrl || 'https://github.com/gOOvER/SCLogMate/releases';
    bridge.openExternalUrl(url);
  };

  return (
    <div className="fixed inset-0 z-[110] flex items-center justify-center bg-black/85 backdrop-blur-md p-4 animate-in fade-in duration-200">
      <div className="sc-glass rounded-2xl w-full max-w-xl border border-emerald-500/40 shadow-2xl shadow-emerald-950/60 p-6 relative overflow-hidden flex flex-col max-h-[90vh]">
        {/* Glowing Header Accent Bar */}
        <div className="absolute top-0 left-0 right-0 h-1 bg-gradient-to-r from-transparent via-emerald-400 to-transparent" />

        {/* Top Header */}
        <div className="flex items-start justify-between gap-4 mb-5 shrink-0">
          <div className="flex items-center space-x-3.5">
            <div className="w-12 h-12 rounded-xl bg-emerald-500/10 border border-emerald-500/40 flex items-center justify-center text-emerald-400 shadow-[0_0_20px_rgba(16,185,129,0.3)] shrink-0">
              <Rocket className="w-6 h-6 animate-pulse" />
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <h2 className="text-base font-bold text-white tracking-wide uppercase">
                  Neues Update verfügbar!
                </h2>
                <span className="px-2 py-0.5 rounded text-[10px] font-mono font-bold bg-emerald-950/80 border border-emerald-500/50 text-emerald-300">
                  RELEASE
                </span>
              </div>
              <div className="flex items-center space-x-2 mt-1 font-mono text-xs">
                <span className="px-2 py-0.5 rounded bg-slate-900/90 border border-slate-800 text-slate-400 text-[11px]">
                  Aktuell: {updateInfo.currentVersion}
                </span>
                <ArrowRight className="w-3.5 h-3.5 text-slate-500" />
                <span className="px-2 py-0.5 rounded bg-emerald-950/90 border border-emerald-500/60 text-emerald-300 font-bold text-[11px] shadow-[0_0_10px_rgba(16,185,129,0.25)]">
                  Neu: {updateInfo.newVersion}
                </span>
              </div>
            </div>
          </div>

          <button
            onClick={onClose}
            disabled={isInstalling}
            className="text-slate-400 hover:text-white p-1.5 rounded-lg hover:bg-slate-800/60 transition cursor-pointer disabled:opacity-30"
            title="Später erinnern"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Release Notes / Changelog */}
        <div className="space-y-2 mb-5 flex-1 min-h-0 flex flex-col">
          <div className="text-[11px] font-mono font-bold text-slate-400 uppercase tracking-wider flex items-center justify-between shrink-0">
            <span>Changelog &amp; Neuerungen:</span>
          </div>

          <div className="flex-1 overflow-y-auto bg-slate-950/80 border border-slate-800/80 rounded-xl p-4 text-xs font-mono text-slate-300 leading-relaxed whitespace-pre-wrap select-text custom-scrollbar">
            {updateInfo.releaseNotes || 'Für dieses Release sind auf GitHub Neuerungen und Verbesserungen verfügbar.'}
          </div>
        </div>

        {/* Status / Installation Banner */}
        {isInstalling && (
          <div className="rounded-xl p-3.5 bg-emerald-950/50 border border-emerald-500/60 flex items-center space-x-3 text-emerald-300 text-xs font-mono animate-pulse shadow-lg mb-4 shrink-0">
            <RefreshCw className="w-4 h-4 animate-spin text-emerald-400 shrink-0" />
            <span className="font-semibold">
              {installStatus || `Lade Update ${updateInfo.newVersion} herunter...`}
            </span>
          </div>
        )}

        {/* Error Message */}
        {errorMessage && (
          <div className="rounded-xl p-3 bg-rose-950/50 border border-rose-500/60 text-rose-300 text-xs font-mono mb-4 shrink-0">
            ⚠️ {errorMessage}
          </div>
        )}

        {/* Action Buttons */}
        <div className="flex items-center justify-between gap-3 pt-3 border-t border-slate-800/80 shrink-0">
          <button
            onClick={handleOpenGitHub}
            className="flex items-center space-x-1.5 px-3.5 py-2 rounded-lg bg-slate-900 hover:bg-slate-800 text-slate-300 hover:text-white text-xs font-medium border border-slate-700 transition cursor-pointer"
          >
            <ExternalLink className="w-3.5 h-3.5" />
            <span>Auf GitHub ansehen</span>
          </button>

          <div className="flex items-center space-x-2.5">
            <button
              onClick={onClose}
              disabled={isInstalling}
              className="px-4 py-2 rounded-lg text-slate-400 hover:text-slate-200 text-xs font-medium transition cursor-pointer hover:bg-slate-800/40 disabled:opacity-40"
            >
              Später erinnern
            </button>

            <button
              onClick={handleApply}
              disabled={isInstalling}
              className="flex items-center space-x-2 px-4 py-2 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-white font-bold text-xs shadow-lg shadow-emerald-600/30 border border-emerald-400 transition cursor-pointer disabled:opacity-50"
            >
              {isInstalling ? (
                <>
                  <RefreshCw className="w-3.5 h-3.5 animate-spin" />
                  <span>Wird aktualisiert...</span>
                </>
              ) : (
                <>
                  <Rocket className="w-3.5 h-3.5" />
                  <span>Jetzt aktualisieren &amp; neu starten</span>
                </>
              )}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
