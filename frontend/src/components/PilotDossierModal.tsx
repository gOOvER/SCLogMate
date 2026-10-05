import React, { useState, useEffect } from 'react';
import {
  User,
  Shield,
  Calendar,
  Languages,
  ExternalLink,
  Globe,
  Building2,
  X,
  Sparkles,
  Server,
  Wifi,
  ClipboardCopy,
  CheckCircle2,
  ArrowRight,
} from 'lucide-react';
import { PilotProfile, HudTelemetry, bridge } from '../services/photinoBridge';
import { RegionFlag } from './RegionFlag';
import { ServerShardsTab } from './ServerShardsTab';

interface PilotDossierModalProps {
  isOpen: boolean;
  profile: PilotProfile | null;
  telemetry?: HudTelemetry | null;
  initialTab?: 'pilot' | 'shards';
  onClose: () => void;
}

export const PilotDossierModal: React.FC<PilotDossierModalProps> = ({
  isOpen,
  profile,
  telemetry,
  initialTab = 'pilot',
  onClose,
}) => {
  const [activeTab, setActiveTab] = useState<'pilot' | 'shards'>(initialTab);
  const [copiedCigString, setCopiedCigString] = useState<boolean>(false);

  useEffect(() => {
    if (isOpen && initialTab) {
      setActiveTab(initialTab);
    }
  }, [isOpen, initialTab]);

  if (!isOpen || !profile) return null;

  const handleOpenRsi = () => {
    const url = profile.profileUrl || `https://robertsspaceindustries.com/citizens/${encodeURIComponent(profile.handle)}`;
    bridge.openExternalUrl(url);
  };

  const handleOpenWebsite = () => {
    if (profile.website) {
      bridge.openExternalUrl(profile.website);
    }
  };

  const handleCopyCurrentCigSupport = () => {
    if (!telemetry?.serverShard) return;
    const str = `Shard: ${telemetry.serverShard} | Region: ${telemetry.serverRegionName || telemetry.serverRegionCode || 'EU'} | Ping: ${telemetry.serverPingMs ?? '—'}ms | Build: ${telemetry.serverVersion || 'SC'}`;
    navigator.clipboard.writeText(str);
    setCopiedCigString(true);
    setTimeout(() => setCopiedCigString(false), 2500);
  };

  return (
    <div className="fixed inset-0 z-[120] flex items-center justify-center bg-black/85 backdrop-blur-md p-4 animate-in fade-in duration-200">
      <div className="sc-glass rounded-2xl w-full max-w-3xl border border-cyan-500/40 shadow-2xl shadow-cyan-950/60 p-6 relative overflow-hidden flex flex-col max-h-[92vh]">
        {/* Glowing Header Accent Bar */}
        <div className="absolute top-0 left-0 right-0 h-1 bg-gradient-to-r from-transparent via-cyan-400 to-transparent" />

        {/* Top Header */}
        <div className="flex items-start justify-between gap-4 mb-4 shrink-0">
          <div className="flex items-center space-x-3.5">
            <div className="w-12 h-12 rounded-xl bg-cyan-500/10 border border-cyan-500/40 flex items-center justify-center text-cyan-400 shadow-[0_0_20px_rgba(6,182,212,0.3)] shrink-0 overflow-hidden">
              {profile.avatarUrl ? (
                <img
                  src={profile.avatarUrl}
                  alt={profile.handle}
                  className="w-full h-full object-cover"
                  onError={(e) => {
                    (e.target as HTMLElement).style.display = 'none';
                  }}
                />
              ) : (
                <User className="w-6 h-6" />
              )}
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <h2 className="text-base font-bold text-white tracking-wide uppercase font-mono">
                  {profile.handle || 'PILOT'} · DOSSIER &amp; SERVER
                </h2>
                {profile.citizenRecord && (
                  <span className="px-2 py-0.5 rounded text-[10px] font-mono font-bold bg-amber-950/80 border border-amber-500/50 text-amber-300">
                    {profile.citizenRecord}
                  </span>
                )}
              </div>
              <div className="flex items-center space-x-2 mt-1 font-mono text-xs text-slate-400">
                <span className="text-cyan-400 font-bold text-sm tracking-wider">
                  {profile.handle}
                </span>
                {profile.title && (
                  <>
                    <span className="text-slate-600">·</span>
                    <span className="text-amber-400 font-medium">
                      {profile.title}
                    </span>
                  </>
                )}
                {telemetry?.serverShardNumber && telemetry.serverShardNumber !== '—' && (
                  <>
                    <span className="text-slate-600">·</span>
                    <span className="text-emerald-400 font-semibold">
                      {telemetry.serverShardNumber}
                    </span>
                  </>
                )}
              </div>
            </div>
          </div>

          <button
            onClick={onClose}
            className="text-slate-400 hover:text-white p-1.5 rounded-lg hover:bg-slate-800/60 transition cursor-pointer"
            title="Schließen"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Subtabs Navigation Bar */}
        <div className="flex items-center gap-2 border-b border-cyan-950/80 pb-3 mb-4 shrink-0">
          <button
            onClick={() => setActiveTab('pilot')}
            className={`px-3.5 py-1.5 rounded-lg text-xs font-mono font-bold flex items-center gap-2 transition cursor-pointer ${
              activeTab === 'pilot'
                ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/50 shadow-[0_0_10px_rgba(0,240,255,0.2)]'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900 border border-transparent'
            }`}
          >
            <User className="w-3.5 h-3.5 text-cyan-400" />
            <span>Citizen Dossier</span>
          </button>

          <button
            onClick={() => setActiveTab('shards')}
            className={`px-3.5 py-1.5 rounded-lg text-xs font-mono font-bold flex items-center gap-2 transition cursor-pointer ${
              activeTab === 'shards'
                ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/50 shadow-[0_0_10px_rgba(0,240,255,0.2)]'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900 border border-transparent'
            }`}
          >
            <Server className="w-3.5 h-3.5 text-emerald-400" />
            <span>Server- &amp; Shard-Tagebuch</span>
          </button>
        </div>

        {/* Content Body */}
        {activeTab === 'pilot' ? (
          <div className="space-y-4 overflow-y-auto pr-1 select-text flex-1">
            {/* Pilot Info Grid */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {/* Primary Details Card */}
              <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-xl p-4 flex flex-col justify-between shadow-inner">
                <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400 mb-2 flex items-center gap-1.5 pb-1.5 border-b border-cyan-950/80">
                  <Shield className="w-3.5 h-3.5 text-cyan-400" />
                  PILOTENSTATUS
                </span>

                <div className="space-y-1 text-xs font-mono">
                  <div className="grid grid-cols-[115px_1fr] items-center gap-2 py-1.5 border-b border-cyan-950/40">
                    <span className="text-slate-400 flex items-center gap-1.5 whitespace-nowrap">
                      <User className="w-3.5 h-3.5 text-cyan-500/70 shrink-0" />
                      Handle:
                    </span>
                    <span className="font-bold text-white tracking-wide truncate" title={profile.handle}>
                      {profile.handle}
                    </span>
                  </div>

                  <div className="grid grid-cols-[115px_1fr] items-center gap-2 py-1.5 border-b border-cyan-950/40">
                    <span className="text-slate-400 flex items-center gap-1.5 whitespace-nowrap">
                      <Sparkles className="w-3.5 h-3.5 text-amber-500/80 shrink-0" />
                      Rang &amp; Titel:
                    </span>
                    <span className="font-semibold text-amber-300 truncate" title={profile.title || 'Civilian'}>
                      {profile.title || 'Civilian'}
                    </span>
                  </div>

                  <div className="grid grid-cols-[115px_1fr] items-center gap-2 py-1.5 border-b border-cyan-950/40">
                    <span className="text-slate-400 flex items-center gap-1.5 whitespace-nowrap">
                      <Calendar className="w-3.5 h-3.5 text-slate-500 shrink-0" />
                      Registriert:
                    </span>
                    <span className="text-slate-200 whitespace-nowrap">
                      {profile.enlisted || '—'}
                    </span>
                  </div>

                  <div className="grid grid-cols-[115px_1fr] items-center gap-2 py-1.5">
                    <span className="text-slate-400 flex items-center gap-1.5 whitespace-nowrap">
                      <Languages className="w-3.5 h-3.5 text-slate-500 shrink-0" />
                      Sprachen:
                    </span>
                    <span className="text-cyan-300 truncate" title={profile.fluency || 'English'}>
                      {profile.fluency || 'English'}
                    </span>
                  </div>
                </div>
              </div>

              {/* Organization Card */}
              <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-xl p-4 flex flex-col justify-between shadow-inner">
                <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400 mb-2 flex items-center gap-1.5 pb-1.5 border-b border-cyan-950/80">
                  <Building2 className="w-3.5 h-3.5 text-cyan-400" />
                  HAUPTORGANISATION
                </span>

                {profile.orgName ? (
                  <div className="flex items-center gap-3.5 my-auto py-1">
                    {profile.orgLogoUrl ? (
                      <div className="w-14 h-14 rounded-xl bg-black/70 border border-cyan-800/50 p-1.5 flex items-center justify-center shrink-0 shadow-sm shadow-cyan-950/60">
                        <img
                          src={profile.orgLogoUrl}
                          alt={profile.orgName}
                          className="max-h-full max-w-full object-contain"
                          onError={(e) => {
                            (e.target as HTMLElement).style.display = 'none';
                          }}
                        />
                      </div>
                    ) : (
                      <div className="w-14 h-14 rounded-xl bg-cyan-950/30 border border-cyan-800/40 flex items-center justify-center text-cyan-500/60 shrink-0">
                        <Building2 className="w-6 h-6" />
                      </div>
                    )}

                    <div className="min-w-0 flex-1 space-y-1.5 font-mono">
                      <div className="text-sm font-bold text-slate-100 truncate tracking-wide" title={profile.orgName}>
                        {profile.orgName}
                      </div>
                      <div className="flex items-center gap-2 text-xs flex-wrap">
                        {profile.orgSid && (
                          <span className="px-1.5 py-0.5 rounded bg-cyan-950/80 border border-cyan-700/60 text-cyan-300 font-bold text-[10px] tracking-wider">
                            [{profile.orgSid}]
                          </span>
                        )}
                        {profile.orgRank && (
                          <span className="text-slate-300 text-xs truncate" title={profile.orgRank}>
                            {profile.orgRank}
                          </span>
                        )}
                      </div>
                    </div>
                  </div>
                ) : (
                  <div className="flex items-center justify-center py-6 text-xs font-mono text-slate-500 italic">
                    Keine Hauptorganisation hinterlegt
                  </div>
                )}
              </div>
            </div>

            {/* Active Server & Shard Card */}
            {telemetry && (
              <div className="bg-[#040c1a]/95 border border-cyan-800/60 rounded-xl p-4 shadow-inner">
                <div className="flex items-center justify-between pb-2 mb-2 border-b border-cyan-950/80">
                  <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-cyan-300 flex items-center gap-1.5">
                    <Server className="w-3.5 h-3.5 text-cyan-400" />
                    AKTIVER SERVER &amp; SHARD
                  </span>
                  <div className="flex items-center gap-1.5 text-xs font-mono">
                    <RegionFlag regionCode={telemetry.serverRegionCode} />
                    <span className="text-amber-300 font-bold">
                      {telemetry.serverRegionName || telemetry.serverRegionCode || 'LIVE'}
                    </span>
                    <span className="text-slate-600">·</span>
                    <div className="flex items-center gap-1 text-emerald-400">
                      <Wifi className="w-3 h-3" />
                      <span>{telemetry.serverPingMs != null ? `${telemetry.serverPingMs} ms` : '—'}</span>
                    </div>
                  </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-xs font-mono">
                  <div>
                    <span className="text-slate-500 text-[10px] block">Vollständiger Shard:</span>
                    <span className="text-white font-bold select-all break-all">
                      {telemetry.serverShard && telemetry.serverShard !== '—'
                        ? telemetry.serverShard
                        : 'Kein Server erkannt'}
                    </span>
                    {telemetry.serverShardNumber && telemetry.serverShardNumber !== '—' && (
                      <span className="ml-2 text-cyan-400 font-semibold">
                        ({telemetry.serverShardNumber})
                      </span>
                    )}
                  </div>

                  <div>
                    <span className="text-slate-500 text-[10px] block">Star Citizen Version:</span>
                    <span className="text-slate-200">
                      {telemetry.serverVersion && telemetry.serverVersion !== '—'
                        ? telemetry.serverVersion
                        : '—'}
                    </span>
                  </div>
                </div>

                <div className="flex flex-wrap items-center justify-between gap-2 mt-3 pt-2.5 border-t border-cyan-950/60">
                  <button
                    onClick={handleCopyCurrentCigSupport}
                    className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-900 hover:bg-slate-800 border border-slate-700 hover:border-cyan-500 text-slate-300 text-xs font-mono transition cursor-pointer"
                  >
                    {copiedCigString ? (
                      <>
                        <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400" />
                        <span className="text-emerald-400 font-bold">CIG-String kopiert!</span>
                      </>
                    ) : (
                      <>
                        <ClipboardCopy className="w-3.5 h-3.5 text-cyan-400" />
                        <span>CIG-Support-String kopieren</span>
                      </>
                    )}
                  </button>

                  <button
                    onClick={() => setActiveTab('shards')}
                    className="flex items-center gap-1 text-xs font-mono text-cyan-400 hover:text-cyan-300 font-semibold cursor-pointer underline"
                  >
                    <span>Server-Tagebuch aller Shards öffnen</span>
                    <ArrowRight className="w-3 h-3" />
                  </button>
                </div>
              </div>
            )}

            {/* Website Link (if present) */}
            {profile.website && (
              <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-xl p-3 flex items-center justify-between shadow-inner">
                <div className="flex items-center gap-2.5 text-xs font-mono min-w-0 mr-3">
                  <Globe className="w-4 h-4 text-cyan-400 shrink-0" />
                  <span className="text-slate-400 shrink-0">Webseite:</span>
                  <span className="text-cyan-300 font-medium truncate" title={profile.website}>
                    {profile.website}
                  </span>
                </div>
                <button
                  onClick={handleOpenWebsite}
                  className="flex items-center gap-1.5 px-3 py-1 rounded-lg text-xs font-mono font-bold text-cyan-300 bg-cyan-950/80 hover:bg-cyan-900 border border-cyan-700/60 transition cursor-pointer shrink-0"
                >
                  <span>Öffnen</span>
                  <ExternalLink className="w-3.5 h-3.5" />
                </button>
              </div>
            )}

            {/* Bio (if present) */}
            {profile.bio && (
              <div className="bg-[#051122]/70 border border-cyan-950 rounded-xl p-3 text-xs text-slate-300 font-mono">
                <span className="text-[10px] text-slate-500 uppercase block mb-1">
                  Biografie
                </span>
                <p className="leading-relaxed whitespace-pre-wrap">{profile.bio}</p>
              </div>
            )}
          </div>
        ) : (
          <div className="overflow-y-auto pr-1 select-text flex-1">
            <ServerShardsTab currentShardId={telemetry?.serverShard} />
          </div>
        )}

        {/* Footer Actions */}
        <div className="flex items-center justify-between gap-3 mt-4 pt-3 border-t border-cyan-950/80 shrink-0">
          <button
            onClick={handleOpenRsi}
            className="flex items-center gap-1.5 px-3 py-2 rounded-xl text-xs font-mono font-bold text-cyan-200 bg-cyan-950/80 hover:bg-cyan-900/90 border border-cyan-700/60 shadow-sm transition cursor-pointer"
          >
            <span>RSI Dossier im Browser öffnen</span>
            <ExternalLink className="w-3.5 h-3.5" />
          </button>

          <button
            onClick={onClose}
            className="px-4 py-2 rounded-xl text-xs font-mono font-bold text-slate-300 bg-slate-900 hover:bg-slate-800 border border-slate-700 transition cursor-pointer"
          >
            Schließen
          </button>
        </div>
      </div>
    </div>
  );
};
