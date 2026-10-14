import React, { useState } from 'react';
import { CampaignStatusDto } from '../services/photinoBridge';
import {
  Trophy,
  Shield,
  Truck,
  Pickaxe,
  CheckCircle2,
  Target,
  Zap,
  Award,
  Layers,
} from 'lucide-react';
import { useI18n } from '../i18n';

interface CampaignsTabProps {
  campaigns?: CampaignStatusDto[];
  onSelectMission?: (title: string) => void;
}

export const CampaignsTab: React.FC<CampaignsTabProps> = ({
  campaigns = [],
  onSelectMission,
}) => {
  const { locale } = useI18n();
  const [selectedCampaignId, setSelectedCampaignId] = useState<string>(
    campaigns.find((c) => c.isActive)?.id || campaigns[0]?.id || 'Iasi_ScenarioProgress'
  );
  const [selectedTrackFilter, setSelectedTrackFilter] = useState<string>('all');

  const activeCampaign =
    campaigns.find((c) => c.id === selectedCampaignId) || campaigns[0];

  const getTrackIcon = (trackId: string) => {
    switch (trackId.toLowerCase()) {
      case 'defense':
        return <Shield className="w-4 h-4 text-rose-400" />;
      case 'transport':
        return <Truck className="w-4 h-4 text-sky-400" />;
      case 'collection':
        return <Pickaxe className="w-4 h-4 text-emerald-400" />;
      case 'overall':
      default:
        return <Trophy className="w-4 h-4 text-amber-400" />;
    }
  };

  if (!activeCampaign) {
    return (
      <div className="sc-glass rounded-lg p-10 border border-slate-800 text-center text-slate-400">
        <Trophy className="w-12 h-12 mx-auto mb-3 text-slate-600 animate-pulse" />
        <p className="text-base font-semibold text-slate-300">
          {locale === 'en' ? 'No Campaign Data Available' : 'Keine Kampagnen-Daten verfügbar'}
        </p>
        <p className="text-xs text-slate-500 mt-1">
          {locale === 'en'
            ? 'Complete in-game event missions to record campaign progress.'
            : 'Schließe In-Game Event-Aufträge ab, um Kampagnen-Fortschritt zu erfassen.'}
        </p>
      </div>
    );
  }

  const overallTrack = activeCampaign.tracks.find((t) => t.overall) || activeCampaign.tracks[0];
  const categoryTracks = activeCampaign.tracks.filter((t) => !t.overall);

  const filteredContracts = activeCampaign.contracts.filter((c) => {
    if (selectedTrackFilter === 'all') return true;
    return c.trackId.toLowerCase() === selectedTrackFilter.toLowerCase();
  });

  return (
    <div className="space-y-5">
      {/* Campaign Selector Pills */}
      <div className="flex flex-wrap items-center gap-2">
        {campaigns.map((camp) => {
          const isSel = camp.id === selectedCampaignId;
          return (
            <button
              key={camp.id}
              onClick={() => {
                setSelectedCampaignId(camp.id);
                setSelectedTrackFilter('all');
              }}
              className={`px-4 py-2 rounded-lg text-xs font-semibold flex items-center gap-2 transition cursor-pointer border ${
                isSel
                  ? 'bg-gradient-to-r from-amber-500/20 via-cyan-500/20 to-indigo-500/20 text-cyan-200 border-cyan-500/50 shadow-[0_0_15px_rgba(0,240,255,0.25)]'
                  : 'bg-slate-900/60 text-slate-400 hover:text-slate-200 hover:bg-slate-800/80 border-slate-800'
              }`}
            >
              <span className="text-base">{camp.icon}</span>
              <span>{camp.title}</span>
              {camp.isActive && (
                <span className="px-1.5 py-0.5 text-[10px] font-bold uppercase rounded bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
                  {locale === 'en' ? 'Active' : 'Aktiv'}
                </span>
              )}
            </button>
          );
        })}
      </div>

      {/* Hero Card for Selected Campaign */}
      <div className="relative overflow-hidden sc-glass rounded-xl p-5 border border-slate-700/80 sc-hud-corner shadow-lg bg-gradient-to-br from-slate-900/90 via-slate-900/70 to-cyan-950/20">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="space-y-1">
            <div className="flex items-center gap-2">
              <span className="text-2xl">{activeCampaign.icon}</span>
              <h2 className="text-xl font-bold font-mono tracking-wider text-slate-100 uppercase">
                {activeCampaign.title}
              </h2>
              {activeCampaign.isActive && (
                <span className="flex items-center gap-1 px-2 py-0.5 text-[11px] font-semibold rounded-full bg-emerald-500/20 text-emerald-300 border border-emerald-500/40">
                  <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-ping" />
                  Live Event
                </span>
              )}
            </div>
            <p className="text-xs text-cyan-400 font-mono tracking-wide">{activeCampaign.tagline}</p>
            <p className="text-xs text-slate-400 max-w-2xl leading-relaxed pt-1">
              {activeCampaign.description}
            </p>
          </div>

          <div className="flex items-center gap-4 bg-slate-950/60 p-3 rounded-lg border border-slate-800">
            <div className="text-right">
              <div className="text-[10px] uppercase font-semibold text-slate-400">
                {locale === 'en' ? 'Event Points' : 'Event-Punkte'}
              </div>
              <div className="text-2xl font-black font-mono text-amber-300">
                {activeCampaign.totalPoints.toLocaleString('de-DE')}
                <span className="text-xs font-normal text-slate-400 ml-1">pts</span>
              </div>
            </div>
            <div className="w-[1px] h-8 bg-slate-800" />
            <div>
              <div className="text-[10px] uppercase font-semibold text-slate-400">
                {locale === 'en' ? 'Completed' : 'Erledigt'}
              </div>
              <div className="text-2xl font-black font-mono text-cyan-300">
                {activeCampaign.totalCompleted}
                <span className="text-xs font-normal text-slate-400 ml-1">runs</span>
              </div>
            </div>
          </div>
        </div>

        {/* Overall Track Progress Bar */}
        {overallTrack && (
          <div className="mt-5 pt-4 border-t border-slate-800/80">
            <div className="flex justify-between items-end mb-2">
              <div className="flex items-center gap-2">
                <Trophy className="w-4 h-4 text-amber-400" />
                <span className="text-xs font-bold text-slate-200 uppercase tracking-wide">
                  {overallTrack.name}
                </span>
                {overallTrack.currentTier > 0 && (
                  <span className="px-2 py-0.5 text-[10px] font-bold rounded bg-amber-500/20 text-amber-300 border border-amber-500/30">
                    Tier {overallTrack.currentTier} erreicht
                  </span>
                )}
              </div>
              <div className="text-xs font-mono text-slate-400">
                {overallTrack.nextTierPoints ? (
                  <>
                    <span className="text-cyan-300 font-semibold">
                      {overallTrack.points.toLocaleString('de-DE')}
                    </span>{' '}
                    /{' '}
                    <span>
                      {overallTrack.nextTierPoints.toLocaleString('de-DE')} pts bis Tier{' '}
                      {overallTrack.currentTier + 1}
                    </span>{' '}
                    <span className="text-amber-400 font-semibold">
                      ({overallTrack.pointsToNext?.toLocaleString('de-DE')} fehlen)
                    </span>
                  </>
                ) : (
                  <span className="text-emerald-400 font-semibold flex items-center gap-1">
                    <CheckCircle2 className="w-3.5 h-3.5" /> Alle Tiers gemeistert!
                  </span>
                )}
              </div>
            </div>

            {/* Custom Multi-Tier Progress Bar */}
            <div className="relative w-full h-3 bg-slate-950/80 rounded-full overflow-hidden border border-slate-800">
              <div
                className="h-full bg-gradient-to-r from-amber-500 via-yellow-400 to-amber-300 rounded-full transition-all duration-500 shadow-[0_0_12px_rgba(245,158,11,0.5)]"
                style={{
                  width: `${Math.min(
                    100,
                    Math.max(
                      5,
                      overallTrack.tiers.length > 0
                        ? (overallTrack.points /
                            overallTrack.tiers[overallTrack.tiers.length - 1].minPoints) *
                            100
                        : overallTrack.progressPercent
                    )
                  )}%`,
                }}
              />
            </div>

            {/* Tier Milestone Markers */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-2 mt-3">
              {overallTrack.tiers.map((tier) => (
                <div
                  key={tier.tierLevel}
                  className={`p-2.5 rounded-lg border text-xs transition ${
                    tier.reached
                      ? 'bg-emerald-950/20 border-emerald-500/40 text-emerald-300 shadow-[0_0_10px_rgba(16,185,129,0.15)]'
                      : 'bg-slate-950/40 border-slate-800 text-slate-400'
                  }`}
                >
                  <div className="flex justify-between items-center mb-1">
                    <span className="font-bold flex items-center gap-1">
                      {tier.reached ? (
                        <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400" />
                      ) : (
                        <Award className="w-3.5 h-3.5 text-slate-500" />
                      )}
                      Tier {tier.tierLevel}
                    </span>
                    <span className="font-mono text-[11px]">
                      {tier.minPoints.toLocaleString('de-DE')} pts
                    </span>
                  </div>
                  <div className="text-[11px] text-slate-300 line-clamp-2 leading-tight">
                    {tier.reward || 'Spezialbelohnung'}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Category Tracks Grid (Defense, Transport, Collection) */}
      {categoryTracks.length > 0 && (
        <div className="space-y-3">
          <div className="flex items-center gap-2">
            <Layers className="w-4 h-4 text-cyan-400" />
            <h3 className="text-xs font-bold text-slate-300 uppercase tracking-wider">
              {locale === 'en' ? 'Category Progress Tracks' : 'Kategorie-Fortschritt & Spezialisierungen'}
            </h3>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {categoryTracks.map((track) => (
              <div
                key={track.id}
                className="sc-glass rounded-xl p-4 border border-slate-800 sc-hud-corner flex flex-col justify-between space-y-3 hover:border-slate-700 transition"
              >
                <div>
                  <div className="flex justify-between items-start">
                    <div className="flex items-center gap-2">
                      <div className="p-1.5 rounded-md bg-slate-950 border border-slate-800">
                        {getTrackIcon(track.id)}
                      </div>
                      <div>
                        <h4 className="text-xs font-bold text-slate-200">{track.name}</h4>
                        <div className="text-[10px] text-slate-400">
                          {track.currentTier > 0
                            ? `Tier ${track.currentTier} erreicht`
                            : 'Noch kein Tier'}
                        </div>
                      </div>
                    </div>
                    <div className="text-right">
                      <div className="text-sm font-bold font-mono text-cyan-300">
                        {track.points.toLocaleString('de-DE')}
                        <span className="text-[10px] font-normal text-slate-400 ml-0.5">pts</span>
                      </div>
                    </div>
                  </div>

                  {/* Track Mini Progress */}
                  <div className="mt-3">
                    <div className="flex justify-between text-[10px] text-slate-400 font-mono mb-1">
                      <span>Fortschritt</span>
                      <span>
                        {track.nextTierPoints
                          ? `${track.pointsToNext?.toLocaleString('de-DE')} pts bis T${
                              track.currentTier + 1
                            }`
                          : 'Max Tier ✓'}
                      </span>
                    </div>
                    <div className="w-full h-2 bg-slate-950 rounded-full overflow-hidden border border-slate-800">
                      <div
                        className="h-full rounded-full transition-all duration-500"
                        style={{
                          backgroundColor: track.color,
                          width: `${Math.min(
                            100,
                            Math.max(
                              4,
                              track.tiers.length > 0
                                ? (track.points / track.tiers[track.tiers.length - 1].minPoints) *
                                    100
                                : track.progressPercent
                            )
                          )}%`,
                        }}
                      />
                    </div>
                  </div>

                  {/* Tiers List */}
                  <div className="mt-3 space-y-1.5">
                    {track.tiers.map((tier) => (
                      <div
                        key={tier.tierLevel}
                        className={`px-2 py-1 rounded text-[11px] flex justify-between items-center ${
                          tier.reached
                            ? 'bg-emerald-950/20 text-emerald-300 border border-emerald-500/30'
                            : 'bg-slate-950/50 text-slate-400 border border-slate-900'
                        }`}
                      >
                        <span className="flex items-center gap-1 font-semibold">
                          {tier.reached ? (
                            <CheckCircle2 className="w-3 h-3 text-emerald-400" />
                          ) : (
                            <span className="w-1.5 h-1.5 rounded-full bg-slate-600" />
                          )}
                          T{tier.tierLevel} ({tier.minPoints.toLocaleString('de-DE')} pts)
                        </span>
                        <span className="text-[10px] truncate max-w-[140px] text-slate-300">
                          {tier.reward || 'Badge'}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>

                {/* Fastest Path Suggestions for this Track */}
                {track.fastestSuggestions.length > 0 && (
                  <div className="pt-2 border-t border-slate-800/80">
                    <div className="flex items-center gap-1 text-[10px] font-bold text-amber-400 uppercase tracking-wide mb-1">
                      <Zap className="w-3 h-3 text-amber-400" />
                      Schnellster Weg zu Tier {track.currentTier + 1}
                    </div>
                    <div className="space-y-1">
                      {track.fastestSuggestions.slice(0, 2).map((sug) => (
                        <div
                          key={sug.contract}
                          onClick={() => onSelectMission && onSelectMission(sug.title)}
                          className="px-2 py-1 rounded bg-amber-950/20 border border-amber-500/20 text-[11px] flex justify-between items-center cursor-pointer hover:border-amber-500/40 transition"
                        >
                          <span className="truncate max-w-[170px] text-slate-300">
                            {sug.title}
                          </span>
                          <span className="font-mono text-amber-300 font-bold text-[10px]">
                            {sug.needed}x ({sug.points} pts)
                          </span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Contracts Table */}
      <div className="sc-glass rounded-xl border border-slate-800 overflow-hidden">
        <div className="p-3 bg-slate-950/70 border-b border-slate-800 flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <Target className="w-4 h-4 text-cyan-400" />
            <h3 className="text-xs font-bold text-slate-200 uppercase tracking-wide">
              {locale === 'en' ? 'Campaign Contracts & Points' : 'Event-Aufträge & Punkte-Auszahlung'}
            </h3>
            <span className="text-[11px] text-slate-400">({filteredContracts.length})</span>
          </div>

          <div className="flex items-center gap-1">
            <button
              onClick={() => setSelectedTrackFilter('all')}
              className={`px-2.5 py-1 text-xs rounded transition cursor-pointer ${
                selectedTrackFilter === 'all'
                  ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              Alle
            </button>
            {categoryTracks.map((tr) => (
              <button
                key={tr.id}
                onClick={() => setSelectedTrackFilter(tr.id)}
                className={`px-2.5 py-1 text-xs rounded transition cursor-pointer flex items-center gap-1 ${
                  selectedTrackFilter === tr.id
                    ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40'
                    : 'text-slate-400 hover:text-slate-200'
                }`}
              >
                {getTrackIcon(tr.id)}
                {tr.name.split(' ')[0]}
              </button>
            ))}
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs text-slate-300">
            <thead className="bg-slate-900/60 text-slate-400 text-[11px] uppercase font-mono border-b border-slate-800">
              <tr>
                <th className="py-2.5 px-3">Auftrag / Contract</th>
                <th className="py-2.5 px-3">Auftraggeber</th>
                <th className="py-2.5 px-3">Kategorie</th>
                <th className="py-2.5 px-3 text-right">Punkte</th>
                <th className="py-2.5 px-3 text-center">Abgeschlossen</th>
                <th className="py-2.5 px-3 text-right">Zuletzt erledigt</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/60 font-sans">
              {filteredContracts.length === 0 ? (
                <tr>
                  <td colSpan={6} className="text-center py-6 text-slate-500">
                    Keine Aufträge für diesen Filter gefunden.
                  </td>
                </tr>
              ) : (
                filteredContracts.map((c) => (
                  <tr
                    key={c.contractId}
                    className="hover:bg-slate-800/30 transition group"
                  >
                    <td className="py-2.5 px-3 font-medium text-slate-200">
                      <div className="flex items-center gap-1.5">
                        <span>{c.title}</span>
                        {c.openCount > 0 && (
                          <span className="px-1.5 py-0.2 rounded text-[10px] font-bold bg-sky-500/20 text-sky-400 border border-sky-500/30">
                            {c.openCount} aktiv
                          </span>
                        )}
                      </div>
                      <div className="text-[10px] font-mono text-slate-500">{c.contractId}</div>
                    </td>
                    <td className="py-2.5 px-3 text-slate-400">{c.issuer}</td>
                    <td className="py-2.5 px-3">
                      <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-medium bg-slate-900 border border-slate-800 text-slate-300">
                        {getTrackIcon(c.trackId)}
                        {c.trackName}
                      </span>
                    </td>
                    <td className="py-2.5 px-3 text-right font-mono font-bold text-amber-300">
                      +{c.points} pts
                    </td>
                    <td className="py-2.5 px-3 text-center">
                      <span
                        className={`inline-block px-2 py-0.5 rounded font-mono text-[11px] font-semibold ${
                          c.completedCount > 0
                            ? 'bg-emerald-500/20 text-emerald-400 border border-emerald-500/30'
                            : 'bg-slate-900 text-slate-500'
                        }`}
                      >
                        {c.completedCount}x
                      </span>
                    </td>
                    <td className="py-2.5 px-3 text-right font-mono text-slate-400">
                      {c.lastCompleted || '—'}
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
