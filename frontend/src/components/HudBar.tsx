import React from 'react';
import {
  Compass,
  CreditCard,
  ExternalLink,
  MapPin,
  Rocket,
  Scroll,
  Shield,
  ShieldAlert,
  Wifi,
  Sparkles,
  Zap,
} from 'lucide-react';
import { HudTelemetry } from '../services/photinoBridge';
import { NavTabId } from './Sidebar';

interface HudBarProps {
  telemetry: HudTelemetry;
  onNavigate: (tab: NavTabId) => void;
  onTriggerOcr?: () => void;
  onToggleAutoOcr?: () => void;
}

export const HudBar: React.FC<HudBarProps> = ({
  telemetry,
  onNavigate,
  onTriggerOcr,
  onToggleAutoOcr,
}) => {
  // Region Flag Helper
  const renderRegionFlag = (code: string) => {
    switch (code) {
      case 'EU':
        return (
          <span className="inline-flex items-center justify-center w-4 h-2.5 rounded-[1px] bg-[#003399] border border-blue-900 relative overflow-hidden" title="Europa (EU Shard)">
            <span className="w-1.5 h-1.5 rounded-full border border-yellow-400 border-dashed" />
          </span>
        );
      case 'US':
        return (
          <span className="inline-flex flex-col w-4 h-2.5 rounded-[1px] bg-white border border-slate-700 relative overflow-hidden" title="USA / Amerika (US Shard)">
            <span className="h-1 bg-[#B22234]" />
            <span className="h-0.5 bg-white" />
            <span className="h-1 bg-[#B22234]" />
            <span className="absolute top-0 left-0 w-2 h-1.5 bg-[#3C3B6E]" />
          </span>
        );
      case 'AUS':
        return (
          <span className="inline-flex items-center justify-center w-4 h-2.5 rounded-[1px] bg-[#00008B] border border-blue-900 relative overflow-hidden" title="Australien (AUS Shard)">
            <span className="text-[6px] text-white absolute top-0 left-0.5 font-bold">★</span>
          </span>
        );
      default:
        return <span className="text-xs" title="Global Shard">🌐</span>;
    }
  };

  const formatAuec = (val: number) => {
    return new Intl.NumberFormat('de-DE').format(val);
  };

  return (
    <div className="px-5 pt-2.5 pb-1 flex flex-col gap-2.5 shrink-0 bg-gradient-to-b from-[#030814]/90 to-[#020610]/95 border-b border-cyan-950/60 transition-all select-none">
      {/* ══ ZEILE 1: 3 KARTEN (Pilot/Server, Standort, Schiff) ══ */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-2.5">
        {/* KARTE 1: PILOT & SERVER */}
        <div className="bg-[#051122]/80 border border-cyan-950/80 hover:border-cyan-800/60 rounded-lg p-2.5 flex flex-col justify-between backdrop-blur-sm transition-all shadow-sm">
          {/* Header */}
          <div className="flex items-center justify-between gap-2 mb-1">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400 flex items-center gap-1.5">
              <span className="w-1.5 h-1.5 rounded-full bg-cyan-400" />
              Pilot & Server
            </span>

            {/* Region & Ping Badge */}
            <div className="flex items-center gap-1.5 bg-[#030814] px-2 py-0.5 rounded border border-cyan-950 text-[10px] font-mono">
              {renderRegionFlag(telemetry.serverRegionCode)}
              <span className="font-bold text-amber-300">{telemetry.serverRegionCode}</span>
              <span className="text-slate-600">·</span>
              <div className="flex items-center gap-1" title={`Latenz: ${telemetry.serverPingMs ?? '—'} ms`}>
                <Wifi className={`w-2.5 h-2.5 ${telemetry.serverPingMs ? 'text-emerald-400' : 'text-slate-500'}`} />
                <span className={telemetry.serverPingMs ? 'text-emerald-300 font-semibold' : 'text-slate-500'}>
                  {telemetry.serverPingMs ? `${telemetry.serverPingMs}ms` : '—'}
                </span>
              </div>
            </div>
          </div>

          {/* Main Pilot Name */}
          <div className="text-sm font-bold font-mono text-slate-100 truncate" title={telemetry.pilotName}>
            {telemetry.pilotName}
          </div>

          {/* Subline: Version & Shard */}
          <div className="text-[11px] font-mono text-slate-400 truncate flex items-center gap-1.5 mt-0.5">
            <span className="text-cyan-400">{telemetry.serverVersion}</span>
            <span className="text-slate-600">·</span>
            <span className="text-slate-400">{telemetry.serverShard}</span>
          </div>
        </div>

        {/* KARTE 2: STANDORT & JURISDIKTION */}
        <div className="bg-[#051122]/80 border border-cyan-950/80 hover:border-cyan-800/60 rounded-lg p-2.5 flex flex-col justify-between backdrop-blur-sm transition-all shadow-sm">
          {/* Header */}
          <div className="flex items-center justify-between gap-2 mb-1">
            <div className="flex items-center gap-1.5">
              <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400 flex items-center gap-1">
                <MapPin className="w-3 h-3 text-cyan-400" />
                Standort
              </span>

              {/* System Pill */}
              <span
                className={`text-[9px] font-mono font-bold px-1.5 py-0.5 rounded border uppercase ${
                  telemetry.locationSystem === 'Pyro'
                    ? 'bg-amber-950/40 border-amber-600/50 text-amber-300'
                    : 'bg-cyan-950/40 border-cyan-700/50 text-cyan-300'
                }`}
              >
                {telemetry.locationSystem}
              </span>

              {/* Armistice Pill */}
              <div
                className={`flex items-center gap-1 px-1.5 py-0.5 rounded text-[9px] font-mono font-semibold border ${
                  telemetry.isArmistice
                    ? 'bg-emerald-950/50 border-emerald-700/60 text-emerald-300'
                    : 'bg-rose-950/50 border-rose-700/60 text-rose-300'
                }`}
              >
                {telemetry.isArmistice ? (
                  <>
                    <Shield className="w-2.5 h-2.5 text-emerald-400" />
                    <span>Schutzzone</span>
                  </>
                ) : (
                  <>
                    <ShieldAlert className="w-2.5 h-2.5 text-rose-400" />
                    <span>Waffen aktiv</span>
                  </>
                )}
              </div>
            </div>

            {/* Quick Link Starmap */}
            <button
              onClick={() => onNavigate('starmap')}
              className="flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-mono font-bold text-cyan-300 hover:text-cyan-200 bg-cyan-950/40 hover:bg-cyan-900/60 border border-cyan-800/60 transition cursor-pointer"
              title="Auf der Sternenkarte anzeigen"
            >
              <Compass className="w-3 h-3" />
              <span>Karte</span>
            </button>
          </div>

          {/* Location Name & Type */}
          <div className="flex items-center justify-between gap-1">
            <div className="text-sm font-bold font-mono text-slate-100 truncate" title={telemetry.locationName}>
              {telemetry.locationName}
            </div>
            <span className="text-[9px] font-mono font-semibold px-1.5 py-0.5 rounded bg-slate-900 border border-slate-800 text-slate-400 shrink-0">
              {telemetry.locationType}
            </span>
          </div>

          {/* Subline: Jurisdiction & Body */}
          <div className="text-[11px] font-mono text-slate-400 truncate mt-0.5">
            <span>{telemetry.locationBody}</span>
            <span className="text-slate-600 mx-1.5">·</span>
            <span className="text-slate-400">{telemetry.jurisdiction}</span>
          </div>
        </div>

        {/* KARTE 3: SCHIFF & FLOTTE */}
        <div className="bg-[#051122]/80 border border-cyan-950/80 hover:border-cyan-800/60 rounded-lg p-2.5 flex flex-col justify-between backdrop-blur-sm transition-all shadow-sm">
          {/* Header */}
          <div className="flex items-center justify-between gap-2 mb-1">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400 flex items-center gap-1.5">
              <Rocket className="w-3 h-3 text-cyan-400" />
              Schiff & Flotte
            </span>

            {/* Quick Links Fleet & Wiki */}
            <div className="flex items-center gap-1">
              <button
                onClick={() => onNavigate('fleet')}
                className="flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-mono font-bold text-sky-300 hover:text-sky-200 bg-sky-950/40 hover:bg-sky-900/60 border border-sky-800/60 transition cursor-pointer"
                title="Flottenübersicht & Hangar öffnen"
              >
                <span>Flotte</span>
              </button>
              <a
                href={`https://star-citizen.wiki/${encodeURIComponent(telemetry.shipName)}`}
                target="_blank"
                rel="noreferrer"
                className="flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-mono font-bold text-cyan-300 hover:text-cyan-200 bg-cyan-950/40 hover:bg-cyan-900/60 border border-cyan-800/60 transition cursor-pointer"
                title="Star Citizen Wiki Datenblatt öffnen"
              >
                <span>Wiki</span>
                <ExternalLink className="w-2.5 h-2.5" />
              </a>
            </div>
          </div>

          {/* Ship Name */}
          <div className="text-sm font-bold font-mono text-slate-100 truncate" title={telemetry.shipName}>
            {telemetry.shipName}
          </div>

          {/* Subline: Flight Telemetry */}
          <div className="text-[11px] font-mono text-slate-400 truncate mt-0.5">
            {telemetry.shipFlightInfo}
          </div>
        </div>
      </div>

      {/* ══ ZEILE 2: 2 BREITE KARTEN (Kontostand & Saldo, Auftragsmanager) ══ */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-2.5">
        {/* KARTE 4: KONTOSTAND & SALDO */}
        <div className="bg-[#051122]/80 border border-cyan-950/80 hover:border-cyan-800/60 rounded-lg p-2.5 flex flex-col justify-between backdrop-blur-sm transition-all shadow-sm">
          {/* Header */}
          <div className="flex items-center justify-between gap-2 mb-1">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400 flex items-center gap-1.5">
              <CreditCard className="w-3 h-3 text-cyan-400" />
              Kontostand & Saldo
            </span>

            {/* Action Buttons: Auto-Sync & Scan */}
            <div className="flex items-center gap-1.5">
              <button
                onClick={onToggleAutoOcr}
                className={`flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-mono font-bold border transition cursor-pointer ${
                  telemetry.autoOcrEnabled
                    ? 'bg-emerald-950/50 border-emerald-700/60 text-emerald-300'
                    : 'bg-slate-900 border-slate-800 text-slate-400'
                }`}
                title="Automatischer mobiGlas Kontostand-Scan"
              >
                <Zap className="w-2.5 h-2.5" />
                <span>{telemetry.autoOcrEnabled ? 'Auto-Sync' : 'Manuell'}</span>
              </button>

              <button
                onClick={onTriggerOcr}
                className="flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-mono font-bold bg-cyan-950/40 hover:bg-cyan-900/60 border border-cyan-800/60 text-cyan-300 transition cursor-pointer"
                title="Kontostand per Test-Scan ablesen"
              >
                <Sparkles className="w-2.5 h-2.5" />
                <span>Scan</span>
              </button>
            </div>
          </div>

          {/* Kontostand Live & Pills */}
          <div className="flex flex-wrap items-baseline justify-between gap-2">
            <div className="text-lg font-bold font-mono text-cyan-300 tracking-tight flex items-baseline gap-1.5">
              <span>{formatAuec(telemetry.balance)}</span>
              <span className="text-xs font-semibold text-cyan-500 font-sans">aUEC</span>
            </div>

            {/* Session Delta Pills */}
            <div className="flex items-center gap-1.5 font-mono text-[10px]">
              {/* Einnahmen */}
              <div className="flex items-center gap-1 px-2 py-0.5 rounded bg-emerald-950/40 border border-emerald-800/50 text-emerald-400" title="Sitzungseinnahmen">
                <span>▲</span>
                <span>+{formatAuec(telemetry.sessionIncome)}</span>
              </div>

              {/* Ausgaben */}
              <div className="flex items-center gap-1 px-2 py-0.5 rounded bg-rose-950/40 border border-rose-800/50 text-rose-400" title="Sitzungsausgaben">
                <span>▼</span>
                <span>-{formatAuec(telemetry.sessionSpend)}</span>
              </div>

              {/* Netto */}
              <div
                className={`flex items-center gap-1 px-2 py-0.5 rounded border font-semibold ${
                  telemetry.sessionNet >= 0
                    ? 'bg-cyan-950/50 border-cyan-700/60 text-cyan-300'
                    : 'bg-amber-950/50 border-amber-700/60 text-amber-300'
                }`}
                title="Netto-Saldo der Sitzung"
              >
                <span>Netto:</span>
                <span>{telemetry.sessionNet >= 0 ? '+' : ''}{formatAuec(telemetry.sessionNet)}</span>
              </div>
            </div>
          </div>
        </div>

        {/* KARTE 5: AUFTRAGSMANAGER / AKTIVE MISSION */}
        <div className="bg-[#051122]/80 border border-cyan-950/80 hover:border-cyan-800/60 rounded-lg p-2.5 flex flex-col justify-between backdrop-blur-sm transition-all shadow-sm">
          {/* Header */}
          <div className="flex items-center justify-between gap-2 mb-1">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400 flex items-center gap-1.5">
              <Scroll className="w-3 h-3 text-cyan-400" />
              Auftragsmanager
            </span>

            <button
              onClick={() => onNavigate('missions')}
              className="flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-mono font-bold text-cyan-300 hover:text-cyan-200 bg-cyan-950/40 hover:bg-cyan-900/60 border border-cyan-800/60 transition cursor-pointer"
              title="Alle Aufträge & Verträge anzeigen"
            >
              <span>Aufträge</span>
            </button>
          </div>

          {/* Mission Title & Reward */}
          <div className="flex items-center justify-between gap-2">
            <div className="text-sm font-bold font-mono text-slate-100 truncate" title={telemetry.activeMissionTitle}>
              {telemetry.activeMissionTitle}
            </div>
            {telemetry.activeMissionReward > 0 && (
              <span className="text-xs font-mono font-bold text-emerald-400 shrink-0">
                +{formatAuec(telemetry.activeMissionReward)} aUEC
              </span>
            )}
          </div>

          {/* Subline: Giver & Status */}
          <div className="text-[11px] font-mono text-slate-400 truncate flex items-center justify-between mt-0.5">
            <span className="text-slate-300 truncate">{telemetry.activeMissionGiver}</span>
            <span className="text-[10px] text-cyan-400 font-semibold shrink-0 ml-2">
              ● {telemetry.activeMissionStatus}
            </span>
          </div>
        </div>
      </div>
    </div>
  );
};
