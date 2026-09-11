import React from 'react';
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
} from 'lucide-react';
import { PilotProfile, bridge } from '../services/photinoBridge';

interface PilotDossierModalProps {
  isOpen: boolean;
  profile: PilotProfile | null;
  onClose: () => void;
}

export const PilotDossierModal: React.FC<PilotDossierModalProps> = ({
  isOpen,
  profile,
  onClose,
}) => {
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

  return (
    <div className="fixed inset-0 z-[120] flex items-center justify-center bg-black/85 backdrop-blur-md p-4 animate-in fade-in duration-200">
      <div className="sc-glass rounded-2xl w-full max-w-xl border border-cyan-500/40 shadow-2xl shadow-cyan-950/60 p-6 relative overflow-hidden flex flex-col max-h-[90vh]">
        {/* Glowing Header Accent Bar */}
        <div className="absolute top-0 left-0 right-0 h-1 bg-gradient-to-r from-transparent via-cyan-400 to-transparent" />

        {/* Top Header */}
        <div className="flex items-start justify-between gap-4 mb-5 shrink-0">
          <div className="flex items-center space-x-3.5">
            <div className="w-12 h-12 rounded-xl bg-cyan-500/10 border border-cyan-500/40 flex items-center justify-center text-cyan-400 shadow-[0_0_20px_rgba(6,182,212,0.3)] shrink-0 overflow-hidden">
              {profile.avatarUrl ? (
                <img
                  src={profile.avatarUrl}
                  alt={profile.handle}
                  className="w-full h-full object-cover"
                  onError={(e) => {
                    // Fallback to User icon on image load error
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
                  CITIZEN DOSSIER
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

        {/* Content Body */}
        <div className="space-y-4 overflow-y-auto pr-1 select-text">
          {/* Pilot Info Grid */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {/* Primary Details Card */}
            <div className="bg-[#051122]/90 border border-cyan-950 rounded-xl p-3.5 flex flex-col justify-between">
              <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400 mb-2 flex items-center gap-1.5">
                <Shield className="w-3.5 h-3.5 text-cyan-400" />
                PILOTENSTATUS
              </span>

              <div className="space-y-2 text-xs font-mono">
                <div className="flex items-center justify-between border-b border-cyan-950/60 pb-1.5">
                  <span className="text-slate-400 flex items-center gap-1">
                    <User className="w-3 h-3 text-slate-500" />
                    Handle Name:
                  </span>
                  <span className="font-bold text-white tracking-wide">
                    {profile.handle}
                  </span>
                </div>

                <div className="flex items-center justify-between border-b border-cyan-950/60 pb-1.5">
                  <span className="text-slate-400 flex items-center gap-1">
                    <Sparkles className="w-3 h-3 text-amber-500" />
                    Rang & Ehrentitel:
                  </span>
                  <span className="font-semibold text-amber-300">
                    {profile.title || 'Civilian'}
                  </span>
                </div>

                <div className="flex items-center justify-between border-b border-cyan-950/60 pb-1.5">
                  <span className="text-slate-400 flex items-center gap-1">
                    <Calendar className="w-3 h-3 text-slate-500" />
                    Enlisted (Registriert):
                  </span>
                  <span className="text-slate-200">
                    {profile.enlisted || '—'}
                  </span>
                </div>

                <div className="flex items-center justify-between">
                  <span className="text-slate-400 flex items-center gap-1">
                    <Languages className="w-3 h-3 text-slate-500" />
                    Sprachen (Fluency):
                  </span>
                  <span className="text-cyan-300">
                    {profile.fluency || 'English'}
                  </span>
                </div>
              </div>
            </div>

            {/* Organization Card */}
            <div className="bg-[#051122]/90 border border-cyan-950 rounded-xl p-3.5 flex flex-col justify-between">
              <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400 mb-2 flex items-center gap-1.5">
                <Building2 className="w-3.5 h-3.5 text-cyan-400" />
                HAUPTORGANISATION
              </span>

              {profile.orgName ? (
                <div className="flex items-center gap-3 my-auto">
                  {profile.orgLogoUrl && (
                    <div className="w-14 h-14 rounded-lg bg-black/60 border border-cyan-800/50 p-1 flex items-center justify-center shrink-0">
                      <img
                        src={profile.orgLogoUrl}
                        alt={profile.orgName}
                        className="max-h-full max-w-full object-contain"
                        onError={(e) => {
                          (e.target as HTMLElement).style.display = 'none';
                        }}
                      />
                    </div>
                  )}

                  <div className="min-w-0 flex-1">
                    <div className="text-sm font-bold text-slate-100 font-mono truncate">
                      {profile.orgName}
                    </div>
                    <div className="flex items-center gap-2 mt-1 text-[11px] font-mono">
                      {profile.orgSid && (
                        <span className="px-1.5 py-0.5 rounded bg-cyan-950/70 border border-cyan-700/60 text-cyan-300 font-bold text-[10px]">
                          [{profile.orgSid}]
                        </span>
                      )}
                      {profile.orgRank && (
                        <span className="text-slate-400">
                          {profile.orgRank}
                        </span>
                      )}
                    </div>
                  </div>
                </div>
              ) : (
                <div className="flex items-center justify-center py-4 text-xs font-mono text-slate-500 italic">
                  Keine Hauptorganisation hinterlegt
                </div>
              )}
            </div>
          </div>

          {/* Website Link (if present) */}
          {profile.website && (
            <div className="bg-[#051122]/90 border border-cyan-950 rounded-xl p-3 flex items-center justify-between">
              <div className="flex items-center gap-2 text-xs font-mono">
                <Globe className="w-4 h-4 text-cyan-400" />
                <span className="text-slate-400">Webseite:</span>
                <span className="text-cyan-300 font-medium truncate max-w-[280px]">
                  {profile.website}
                </span>
              </div>
              <button
                onClick={handleOpenWebsite}
                className="flex items-center gap-1 px-2 py-1 rounded text-[11px] font-mono font-bold text-cyan-300 bg-cyan-950/60 hover:bg-cyan-900 border border-cyan-800/60 transition cursor-pointer"
              >
                <span>Öffnen</span>
                <ExternalLink className="w-3 h-3" />
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

        {/* Footer Actions */}
        <div className="flex items-center justify-between gap-3 mt-5 pt-3 border-t border-cyan-950/80 shrink-0">
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
