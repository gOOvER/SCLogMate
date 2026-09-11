import React, { useEffect, useState } from 'react';
import {
  ExternalLink,
  Shield,
  User,
  Users,
  Calendar,
  Languages,
  Award,
  X,
  CheckCircle2,
  Globe,
} from 'lucide-react';
import { bridge, PilotProfile } from '../services/photinoBridge';

interface PilotDossierModalProps {
  isOpen: boolean;
  pilotName?: string;
  onClose: () => void;
}

export const PilotDossierModal: React.FC<PilotDossierModalProps> = ({
  isOpen,
  pilotName,
  onClose,
}) => {
  const [profile, setProfile] = useState<PilotProfile | null>(null);
  const [loading, setLoading] = useState<boolean>(false);

  useEffect(() => {
    if (!isOpen) return;

    let isMounted = true;
    setLoading(true);

    bridge
      .sendRequest<PilotProfile>('get_pilot_dossier', { handle: pilotName })
      .then((res) => {
        if (isMounted && res) {
          setProfile(res);
        }
      })
      .catch((err) => {
        console.error('Failed to load pilot dossier:', err);
      })
      .finally(() => {
        if (isMounted) setLoading(false);
      });

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', handleKeyDown);

    return () => {
      isMounted = false;
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, [isOpen, pilotName, onClose]);

  if (!isOpen) return null;

  const handleOpenRsi = () => {
    const url = profile?.profileUrl || `https://robertsspaceindustries.com/citizens/${encodeURIComponent(pilotName || '')}`;
    bridge.send('open_external', { url });
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-md animate-fadeIn">
      {/* Background click to close */}
      <div className="absolute inset-0" onClick={onClose} />

      {/* Sci-Fi Dossier Modal Container */}
      <div className="relative w-full max-w-xl bg-[#030914]/95 border border-cyan-500/40 rounded-xl shadow-[0_0_50px_rgba(6,182,212,0.25)] overflow-hidden flex flex-col font-sans z-10 border-t-cyan-400">
        {/* Hologram scanline accent bar */}
        <div className="h-1 bg-gradient-to-r from-transparent via-cyan-400 to-transparent shadow-[0_0_12px_rgba(6,182,212,0.8)]" />

        {/* Header */}
        <div className="flex items-center justify-between px-6 py-3.5 border-b border-cyan-950/80 bg-[#051122]">
          <div className="flex items-center gap-2.5">
            <div className="flex items-center justify-center w-7 h-7 rounded bg-cyan-950/80 border border-cyan-700/60 text-cyan-400">
              <Shield className="w-4 h-4" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="text-xs font-mono font-bold tracking-widest text-cyan-400 uppercase">
                  UEE CITIZEN DOSSIER
                </span>
                {profile?.citizenRecord && profile.citizenRecord !== '—' && (
                  <span className="px-1.5 py-0.2 rounded bg-cyan-950 border border-cyan-700/60 text-[10px] font-mono font-bold text-cyan-300">
                    {profile.citizenRecord}
                  </span>
                )}
              </div>
              <p className="text-[10.5px] font-mono text-slate-400">
                Offizielles RSI Spectrum Profil & Flotten-Zugehörigkeit
              </p>
            </div>
          </div>

          <button
            onClick={onClose}
            className="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-white/10 transition cursor-pointer"
            title="Schließen"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* Modal Body */}
        <div className="p-6 space-y-5 overflow-y-auto max-h-[80vh]">
          {loading && !profile ? (
            <div className="py-16 text-center space-y-3">
              <div className="w-10 h-10 border-2 border-cyan-500 border-t-transparent rounded-full animate-spin mx-auto shadow-[0_0_15px_rgba(6,182,212,0.4)]" />
              <p className="text-xs font-mono text-cyan-300">Lade Piloten-Dossier aus Spectrum Datenbank…</p>
            </div>
          ) : (
            <>
              {/* Pilot Card Top */}
              <div className="flex flex-col sm:flex-row items-center sm:items-start gap-4 p-4 rounded-lg bg-gradient-to-br from-[#061426] to-[#040d1a] border border-cyan-900/60">
                {/* Avatar */}
                <div className="relative shrink-0">
                  <div className="w-20 h-20 rounded-full overflow-hidden border-2 border-cyan-400 shadow-[0_0_20px_rgba(6,182,212,0.4)] bg-slate-900 flex items-center justify-center">
                    {profile?.avatarUrl ? (
                      <img
                        src={profile.avatarUrl}
                        alt={profile.handle}
                        className="w-full h-full object-cover"
                        onError={(e) => {
                          (e.target as HTMLElement).style.display = 'none';
                        }}
                      />
                    ) : (
                      <User className="w-10 h-10 text-cyan-400/60" />
                    )}
                  </div>
                  <span
                    className="absolute bottom-0.5 right-0.5 w-4 h-4 rounded-full bg-emerald-500 border-2 border-[#040d1a] shadow-[0_0_8px_rgba(16,185,129,0.8)]"
                    title="Aktiv im Dienst"
                  />
                </div>

                {/* Name & Titles */}
                <div className="flex-1 text-center sm:text-left min-w-0">
                  <div className="flex flex-wrap items-center justify-center sm:justify-start gap-2">
                    <h2 className="text-xl font-bold font-mono text-white tracking-wide truncate">
                      {profile?.handle || pilotName || '—'}
                    </h2>
                    {profile?.isVerified && (
                      <span className="flex items-center gap-1 px-2 py-0.5 rounded bg-cyan-950/80 border border-cyan-700/60 text-[10px] font-mono text-cyan-300">
                        <CheckCircle2 className="w-3 h-3 text-cyan-400" />
                        VERIFIED
                      </span>
                    )}
                  </div>

                  <div className="mt-1 flex items-center justify-center sm:justify-start gap-2">
                    <span className="text-xs font-mono font-semibold text-amber-400 flex items-center gap-1">
                      <Award className="w-3.5 h-3.5" />
                      {profile?.title || 'Civilian'}
                    </span>
                  </div>

                  {/* Badges / Enlistment & Fluency */}
                  <div className="mt-3 flex flex-wrap items-center justify-center sm:justify-start gap-2 text-[11px] font-mono text-slate-300">
                    {profile?.enlisted && profile.enlisted !== '—' && (
                      <span className="flex items-center gap-1.5 px-2 py-1 rounded bg-[#07172b] border border-cyan-900/60">
                        <Calendar className="w-3 h-3 text-cyan-400" />
                        Im Dienst seit: <strong className="text-cyan-200">{profile.enlisted}</strong>
                      </span>
                    )}
                    {profile?.fluency && profile.fluency !== '—' && (
                      <span className="flex items-center gap-1.5 px-2 py-1 rounded bg-[#07172b] border border-cyan-900/60">
                        <Languages className="w-3 h-3 text-cyan-400" />
                        Sprachen: <strong className="text-slate-200">{profile.fluency}</strong>
                      </span>
                    )}
                  </div>
                </div>
              </div>

              {/* Organization Section */}
              {profile?.orgName ? (
                <div className="p-4 rounded-lg bg-[#051122] border border-cyan-900/60 space-y-3">
                  <div className="flex items-center justify-between">
                    <span className="text-xs font-mono font-bold uppercase tracking-wider text-cyan-400 flex items-center gap-1.5">
                      <Users className="w-3.5 h-3.5" />
                      Haupt-Organisation
                    </span>
                    {profile.orgSid && (
                      <span className="px-2 py-0.5 rounded bg-indigo-950/80 border border-indigo-700/60 text-[10px] font-mono font-bold text-indigo-300">
                        SID: {profile.orgSid}
                      </span>
                    )}
                  </div>

                  <div className="flex items-center gap-3.5">
                    {profile.orgLogoUrl ? (
                      <div className="w-12 h-12 rounded bg-black/60 border border-cyan-800/60 p-1 shrink-0 overflow-hidden flex items-center justify-center">
                        <img
                          src={profile.orgLogoUrl}
                          alt={profile.orgName}
                          className="w-full h-full object-contain"
                        />
                      </div>
                    ) : (
                      <div className="w-12 h-12 rounded bg-indigo-950/60 border border-indigo-800/60 flex items-center justify-center text-indigo-400 shrink-0">
                        <Shield className="w-6 h-6" />
                      </div>
                    )}

                    <div className="flex-1 min-w-0">
                      <div className="text-sm font-bold font-mono text-white truncate">
                        {profile.orgName}
                      </div>
                      <div className="text-xs font-mono text-cyan-300 mt-0.5">
                        Rang: <span className="font-semibold text-amber-300">{profile.orgRank || 'Member'}</span>
                      </div>
                    </div>
                  </div>
                </div>
              ) : (
                <div className="p-3.5 rounded-lg bg-[#051122] border border-slate-800 text-xs font-mono text-slate-400 text-center">
                  Keine Haupt-Organisation im RSI-Profil hinterlegt.
                </div>
              )}

              {/* Bio / Description */}
              {profile?.bio && (
                <div className="p-4 rounded-lg bg-[#051122] border border-cyan-950 space-y-1.5">
                  <span className="text-[11px] font-mono font-bold uppercase tracking-wider text-slate-400">
                    Biografie & Notizen
                  </span>
                  <p className="text-xs text-slate-300 leading-relaxed font-sans italic">
                    "{profile.bio}"
                  </p>
                </div>
              )}
            </>
          )}
        </div>

        {/* Footer Actions */}
        <div className="flex items-center justify-between px-6 py-3.5 border-t border-cyan-950/80 bg-[#040c1a]">
          <button
            onClick={handleOpenRsi}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded bg-[#061528] hover:bg-cyan-950 border border-cyan-800/60 hover:border-cyan-500 text-xs font-mono text-cyan-300 hover:text-cyan-100 transition cursor-pointer shadow-sm"
          >
            <Globe className="w-3.5 h-3.5 text-cyan-400" />
            <span>RSI Profil im Browser</span>
            <ExternalLink className="w-3 h-3 ml-0.5 opacity-70" />
          </button>

          <button
            onClick={onClose}
            className="px-4 py-1.5 rounded bg-cyan-600 hover:bg-cyan-500 text-slate-950 font-mono font-bold text-xs transition cursor-pointer shadow-[0_0_12px_rgba(6,182,212,0.4)]"
          >
            Schließen
          </button>
        </div>
      </div>
    </div>
  );
};
