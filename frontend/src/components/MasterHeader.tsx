import React, { useState } from 'react';
import {
  FolderSync,
  HardDrive,
  RefreshCw,
  Monitor,
  Radio,
  Sparkles,
  User,
} from 'lucide-react';
import { bridge, AppStatus } from '../services/photinoBridge';
import { useI18n } from '../i18n';

interface MasterHeaderProps {
  status: AppStatus | null;
  pilotName?: string;
  pilotAvatarUrl?: string;
  pilotTitle?: string;
  isGameRunning?: boolean;
  isScanning: boolean;
  loading: boolean;
  onRefresh: () => void;
  onTriggerScan: () => void;
  onReparseAll?: () => void;
  onOpenPilotDossier?: () => void;
}

export const MasterHeader: React.FC<MasterHeaderProps> = ({
  status,
  pilotName,
  pilotAvatarUrl,
  pilotTitle,
  isGameRunning = false,
  isScanning,
  loading,
  onRefresh,
  onTriggerScan,
  onReparseAll,
  onOpenPilotDossier,
}) => {
  const { locale, setLocale, t } = useI18n();
  const [overlayFeedback, setOverlayFeedback] = useState<string | null>(null);

  const handleOpenOverlay = async (type: 'mini' | 'rs') => {
    try {
      if (type === 'mini') {
        await bridge.sendRequest('open_overlay');
        showToast('Mini-HUD aktiviert');
      } else {
        await bridge.sendRequest('open_rs_overlay');
        showToast('RS-Decoder aktiviert');
      }
    } catch (e) {
      console.error('Failed to trigger overlay:', e);
    }
  };

  const showToast = (msg: string) => {
    setOverlayFeedback(msg);
    setTimeout(() => setOverlayFeedback(null), 2500);
  };

  return (
    <header className="flex items-center justify-between px-5 py-2.5 border-b border-cyan-950/60 bg-[#040914]/95 backdrop-blur-md z-20 shrink-0 h-14 select-none">
      {/* Linke Seite: Logo / Version + Pilot Dossier + Star Citizen Status + Logpfad */}
      <div className="flex items-center gap-3 min-w-0">
        {/* App Title & Version Pill */}
        <div className="flex items-center gap-2 shrink-0">
          <div className="flex items-center gap-1.5 px-2 py-0.5 rounded bg-cyan-950/50 border border-cyan-800/50 text-cyan-300 font-mono text-[11px] font-bold tracking-wider">
            <span className="w-1.5 h-1.5 rounded-full bg-cyan-400 animate-ping" />
            <span>SCLM</span>
            <span className="text-cyan-500 font-normal">|</span>
            <span className="text-slate-300">{status?.version || 'v1.0.0-rc2'}</span>
          </div>
        </div>

        {/* Pilot Dossier Trigger Pill */}
        <button
          onClick={onOpenPilotDossier}
          className="flex items-center gap-2 px-2.5 py-1 rounded-md border border-cyan-800/70 bg-[#061426] hover:bg-cyan-950/70 hover:border-cyan-400 text-xs font-mono transition-all cursor-pointer shadow-sm group shrink-0"
          title={`Piloten-Dossier & RSI Profil öffnen: ${pilotName || 'Pilot'}`}
        >
          <div className="relative w-5 h-5 rounded-full overflow-hidden border border-cyan-400/80 bg-slate-900 shrink-0 flex items-center justify-center">
            {pilotAvatarUrl ? (
              <img src={pilotAvatarUrl} alt={pilotName} className="w-full h-full object-cover" />
            ) : (
              <User className="w-3 h-3 text-cyan-400" />
            )}
            <span className="absolute bottom-0 right-0 w-1.5 h-1.5 rounded-full bg-emerald-400" />
          </div>
          <div className="flex flex-col text-left leading-none">
            <span className="text-[11px] font-bold text-slate-200 group-hover:text-cyan-300 transition truncate max-w-[110px]">
              {pilotName && pilotName !== '—' ? pilotName : 'Commander'}
            </span>
            {pilotTitle && (
              <span className="text-[9px] text-amber-400 font-semibold truncate max-w-[110px]">
                {pilotTitle}
              </span>
            )}
          </div>
        </button>

        {/* Star Citizen Prozess Status Badge */}
        <div
          className={`flex items-center gap-2 px-2.5 py-1 rounded-md border text-xs font-mono font-semibold transition-all shrink-0 ${
            isGameRunning
              ? 'bg-emerald-950/40 border-emerald-500/50 text-emerald-300 shadow-[0_0_12px_rgba(16,185,129,0.25)]'
              : 'bg-slate-900/60 border-slate-800 text-slate-400'
          }`}
          title={isGameRunning ? 'Star Citizen Prozess erkannt (StarCitizen.exe)' : 'Star Citizen läuft momentan nicht'}
        >
          <span
            className={`w-2 h-2 rounded-full ${
              isGameRunning
                ? 'bg-emerald-400 animate-pulse shadow-[0_0_8px_#34d399]'
                : 'bg-slate-500'
            }`}
          />
          <span className="hidden sm:inline">{isGameRunning ? t('header.liveGame') : t('header.offline')}</span>
        </div>

        {/* Trenner */}
        <span className="text-slate-700 hidden lg:inline">|</span>

        {/* Aktiver Logpfad */}
        <div className="hidden xl:flex items-center gap-1.5 font-mono text-[11px] text-slate-400 truncate max-w-xs" title={status?.logPath || t('header.noLogPath')}>
          <HardDrive className="w-3.5 h-3.5 text-cyan-400/80 shrink-0" />
          <span className="truncate">{status?.logPath || t('header.noLogPath')}</span>
        </div>
      </div>

      {/* Mitte: Benachrichtigungs-Pill bei Aktionen */}
      {overlayFeedback && (
        <div className="hidden lg:flex items-center gap-1.5 px-3 py-1 rounded bg-cyan-500/10 border border-cyan-500/40 text-cyan-300 text-xs font-mono animate-fade-in">
          <Sparkles className="w-3 h-3 text-cyan-400 animate-spin" />
          <span>{overlayFeedback}</span>
        </div>
      )}

      {/* Rechte Seite: Overlays, Sprache & Aktionen */}
      <div className="flex items-center gap-2.5 shrink-0">
        {/* Overlay Schnellstarter */}
        <div className="flex items-center gap-1 bg-[#06101e] p-1 rounded-md border border-cyan-950/80">
          <button
            onClick={() => handleOpenOverlay('mini')}
            className="flex items-center gap-1.5 px-2 py-1 rounded text-xs font-semibold text-slate-300 hover:text-cyan-300 hover:bg-cyan-950/40 transition cursor-pointer"
            title="In-Game Mini-HUD Overlay ein-/ausblenden"
          >
            <Monitor className="w-3.5 h-3.5 text-cyan-400" />
            <span className="hidden xl:inline text-[11px]">{t('header.miniHud')}</span>
          </button>
          <button
            onClick={() => handleOpenOverlay('rs')}
            className="flex items-center gap-1.5 px-2 py-1 rounded text-xs font-semibold text-slate-300 hover:text-amber-300 hover:bg-amber-950/40 transition cursor-pointer"
            title="RS Signal Decoder HUD Overlay ein-/ausblenden"
          >
            <Radio className="w-3.5 h-3.5 text-amber-400" />
            <span className="hidden xl:inline text-[11px]">{t('header.rsOverlay')}</span>
          </button>
        </div>

        {/* Globaler Sprachwähler mit Vektorflaggen */}
        <div className="flex items-center bg-[#06101e] border border-cyan-950/80 rounded-md p-0.5">
          <button
            onClick={() => setLocale('de')}
            className={`flex items-center gap-1.5 px-2 py-1 rounded text-xs font-medium transition cursor-pointer ${
              locale === 'de'
                ? 'bg-cyan-950/60 text-cyan-300 border border-cyan-700/50 shadow-sm'
                : 'text-slate-400 hover:text-slate-200'
            }`}
            title="Deutsch"
          >
            {/* 🇩🇪 Vektor-Flagge */}
            <span className="inline-flex flex-col w-3.5 h-2.5 overflow-hidden rounded-[1px] border border-slate-700/80 shrink-0">
              <span className="h-1/3 bg-black" />
              <span className="h-1/3 bg-[#DD0000]" />
              <span className="h-1/3 bg-[#FFCE00]" />
            </span>
            <span className="text-[11px] font-mono">DE</span>
          </button>

          <button
            onClick={() => setLocale('en')}
            className={`flex items-center gap-1.5 px-2 py-1 rounded text-xs font-medium transition cursor-pointer ${
              locale === 'en'
                ? 'bg-cyan-950/60 text-cyan-300 border border-cyan-700/50 shadow-sm'
                : 'text-slate-400 hover:text-slate-200'
            }`}
            title="English"
          >
            {/* 🇬🇧 Vektor-Flagge */}
            <span className="inline-flex items-center justify-center w-3.5 h-2.5 overflow-hidden rounded-[1px] bg-[#00247D] border border-slate-700/80 relative shrink-0">
              <span className="absolute w-full h-[1px] bg-white transform rotate-[32deg]" />
              <span className="absolute w-full h-[1px] bg-white transform -rotate-[32deg]" />
              <span className="absolute w-full h-[1.5px] bg-white" />
              <span className="absolute h-full w-[1.5px] bg-white" />
              <span className="absolute w-full h-[0.8px] bg-[#CF142B]" />
              <span className="absolute h-full w-[0.8px] bg-[#CF142B]" />
            </span>
            <span className="text-[11px] font-mono">EN</span>
          </button>
        </div>

        {/* Scan Button Group */}
        <div className="flex items-center">
          <button
            onClick={onTriggerScan}
            disabled={isScanning}
            className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-mono font-semibold border border-cyan-500/50 bg-cyan-950/30 text-cyan-300 hover:bg-cyan-900/40 transition-all cursor-pointer disabled:opacity-50 ${
              onReparseAll ? 'rounded-l' : 'rounded'
            }`}
            title="Schnell-Scan (auf neue Events prüfen)"
          >
            <FolderSync className={`w-3.5 h-3.5 ${isScanning ? 'animate-spin' : ''}`} />
            <span className="hidden sm:inline">{isScanning ? t('common.scanning') : t('common.scan')}</span>
          </button>
          {onReparseAll && (
            <button
              onClick={onReparseAll}
              disabled={isScanning}
              className="px-2 py-1.5 text-xs font-mono font-semibold rounded-r border-t border-r border-b border-cyan-500/50 bg-cyan-950/40 text-cyan-300 hover:bg-cyan-800/50 transition-all cursor-pointer disabled:opacity-50"
              title="Kompletter Re-Scan: Alle Logs neu einlesen"
            >
              <RefreshCw className={`w-3.5 h-3.5 ${isScanning ? 'animate-spin text-cyan-400' : ''}`} />
            </button>
          )}
        </div>

        {/* Refresh Button */}
        <button
          onClick={onRefresh}
          title={t('header.refreshTooltip')}
          className="p-1.5 rounded border border-slate-800 hover:border-cyan-800 bg-[#06101e] text-slate-400 hover:text-cyan-300 transition cursor-pointer"
        >
          <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin text-cyan-400' : ''}`} />
        </button>
      </div>
    </header>
  );
};
