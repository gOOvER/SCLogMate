import React, { useState, useEffect } from 'react';
import {
  FolderSync,
  HardDrive,
  RefreshCw,
  Monitor,
  Radio,
  Sparkles,
  ArrowUpCircle,
} from 'lucide-react';
import { bridge, AppStatus, UpdateInfoDto } from '../services/photinoBridge';
import { useI18n } from '../i18n';

interface MasterHeaderProps {
  status: AppStatus | null;
  isGameRunning?: boolean;
  isScanning: boolean;
  loading?: boolean;
  updateInfo?: UpdateInfoDto | null;
  onOpenUpdateModal?: () => void;
  onRefresh?: () => void;
  onTriggerScan: () => void;
  onReparseAll?: () => void;
}

export const MasterHeader: React.FC<MasterHeaderProps> = ({
  status,
  isGameRunning = false,
  isScanning,
  updateInfo,
  onOpenUpdateModal,
  onTriggerScan,
  onReparseAll,
}) => {
  const { locale, setLocale, t } = useI18n();
  const [overlayFeedback, setOverlayFeedback] = useState<string | null>(null);
  const [isMiniHudActive, setIsMiniHudActive] = useState(false);
  const [isRsOverlayActive, setIsRsOverlayActive] = useState(false);

  useEffect(() => {
    const unsub1 = bridge.on('OVERLAY_STATE', (data: any) => {
      if (typeof data?.isOverlayActive === 'boolean') {
        setIsMiniHudActive(data.isOverlayActive);
      }
    });
    const unsub2 = bridge.on('RS_OVERLAY_STATE', (data: any) => {
      if (typeof data?.isRsOverlayActive === 'boolean') {
        setIsRsOverlayActive(data.isRsOverlayActive);
      }
    });
    return () => {
      unsub1();
      unsub2();
    };
  }, []);

  const handleOpenOverlay = async (type: 'mini' | 'rs') => {
    try {
      if (type === 'mini') {
        const res = await bridge.sendRequest<{ success?: boolean; isOverlayActive?: boolean }>('open_overlay');
        const active = res?.isOverlayActive ?? !isMiniHudActive;
        setIsMiniHudActive(active);
        showToast(active ? 'Mini-HUD aktiviert (Alt+H)' : 'Mini-HUD ausgeblendet');
      } else {
        const res = await bridge.sendRequest<{ success?: boolean; isRsOverlayActive?: boolean }>('open_rs_overlay');
        const active = res?.isRsOverlayActive ?? !isRsOverlayActive;
        setIsRsOverlayActive(active);
        showToast(active ? 'RS-Radar aktiviert' : 'RS-Radar ausgeblendet');
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
      {/* Linke Seite: Logo / Version + Star Citizen Status + Logpfad */}
      <div className="flex items-center gap-3 min-w-0">
        {/* Brand & Version Badge */}
        <div className="flex items-center gap-2 shrink-0">
          <div className="flex items-center justify-center w-7 h-7 rounded bg-cyan-950/60 border border-cyan-500/40 text-cyan-400 font-bold text-sm shadow-[0_0_10px_rgba(0,240,255,0.2)]">
            ❖
          </div>
          <div className="flex items-center font-bold text-base tracking-wide">
            <span className="text-slate-100">SC</span>
            <span className="text-cyan-400">LOG</span>
            <span className="text-amber-400">MATE</span>
          </div>
          <span className="px-2 py-0.5 rounded bg-cyan-950/60 border border-cyan-800/60 text-cyan-300 font-mono text-[11px] font-bold">
            {status?.version || 'v1.0.0-rc2'}
          </span>
          {updateInfo?.updateAvailable && (
            <button
              onClick={onOpenUpdateModal}
              className="flex items-center gap-1.5 px-2.5 py-0.5 rounded bg-emerald-600 hover:bg-emerald-500 text-white font-mono text-[11px] font-bold shadow-[0_0_12px_rgba(16,185,129,0.5)] border border-emerald-400 animate-pulse transition cursor-pointer shrink-0"
              title="Klicken für Update-Details &amp; Installation"
            >
              <ArrowUpCircle className="w-3.5 h-3.5" />
              <span>Update {updateInfo.newVersion}</span>
            </button>
          )}
        </div>

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
            className={`flex items-center gap-1.5 px-2 py-1 rounded text-xs font-semibold transition cursor-pointer ${
              isMiniHudActive
                ? 'bg-cyan-950/80 text-cyan-300 border border-cyan-500/50 shadow-[0_0_10px_rgba(6,182,212,0.25)]'
                : 'text-slate-300 hover:text-cyan-300 hover:bg-cyan-950/40 border border-transparent'
            }`}
            title="In-Game Mini-HUD Overlay ein-/ausblenden (Globaler Hotkey: Alt+H)"
          >
            <Monitor className={`w-3.5 h-3.5 ${isMiniHudActive ? 'text-cyan-300 animate-pulse' : 'text-cyan-400'}`} />
            <span className="hidden xl:inline text-[11px]">{t('header.miniHud')}</span>
            {isMiniHudActive && <span className="w-1.5 h-1.5 rounded-full bg-cyan-400 animate-ping" />}
          </button>
          <button
            onClick={() => handleOpenOverlay('rs')}
            className={`flex items-center gap-1.5 px-2 py-1 rounded text-xs font-semibold transition cursor-pointer ${
              isRsOverlayActive
                ? 'bg-amber-950/80 text-amber-300 border border-amber-500/50 shadow-[0_0_10px_rgba(245,158,11,0.25)]'
                : 'text-slate-300 hover:text-amber-300 hover:bg-amber-950/40 border border-transparent'
            }`}
            title="RS Signal Decoder HUD Overlay ein-/ausblenden"
          >
            <Radio className={`w-3.5 h-3.5 ${isRsOverlayActive ? 'text-amber-300 animate-pulse' : 'text-amber-400'}`} />
            <span className="hidden xl:inline text-[11px]">{t('header.rsOverlay')}</span>
            {isRsOverlayActive && <span className="w-1.5 h-1.5 rounded-full bg-amber-400 animate-ping" />}
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
      </div>
    </header>
  );
};
