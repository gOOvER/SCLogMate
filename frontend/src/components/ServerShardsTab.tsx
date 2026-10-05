import React, { useEffect, useState, useMemo } from 'react';
import { bridge, ServerShardDto } from '../services/photinoBridge';
import {
  Server,
  Star,
  AlertTriangle,
  ClipboardCopy,
  CheckCircle2,
  Clock,
  Search,
  RotateCcw,
  Edit2,
  Check,
  X,
} from 'lucide-react';
import { useI18n } from '../i18n';

interface ServerShardsTabProps {
  currentShardId?: string | null;
}

export const ServerShardsTab: React.FC<ServerShardsTabProps> = ({ currentShardId }) => {
  const { locale } = useI18n();
  const [serverShards, setServerShards] = useState<ServerShardDto[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [search, setSearch] = useState<string>('');
  const [ratingFilter, setRatingFilter] = useState<string>('all');
  const [regionFilter, setRegionFilter] = useState<string>('all');
  const [editingNotesShardId, setEditingNotesShardId] = useState<string | null>(null);
  const [editingNotesText, setEditingNotesText] = useState<string>('');
  const [copiedCigShardId, setCopiedCigShardId] = useState<string | null>(null);

  const fetchServerShards = async () => {
    setLoading(true);
    try {
      const data = await bridge.sendRequest<ServerShardDto[]>('get_server_shards');
      setServerShards(data || []);
    } catch (err) {
      console.error('Failed to load server shards:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchServerShards();
    const unsub = bridge.on('SERVER_SHARDS_UPDATED', (payload: any) => {
      if (Array.isArray(payload)) {
        setServerShards(payload);
      } else {
        fetchServerShards();
      }
    });
    return () => unsub();
  }, []);

  const handleSetRating = async (shardId: string, rating: 'Good' | 'Avoid' | 'Neutral') => {
    try {
      const current = serverShards.find((s) => s.shardId === shardId);
      const notes = current?.notes || '';
      await bridge.sendRequest('update_shard_rating_and_notes', {
        shardId,
        rating,
        notes,
      });
      setServerShards((prev) =>
        prev.map((s) => (s.shardId === shardId ? { ...s, rating } : s))
      );
    } catch (err) {
      console.error('Failed to update shard rating:', err);
    }
  };

  const handleSaveNotes = async (shardId: string) => {
    try {
      const current = serverShards.find((s) => s.shardId === shardId);
      const rating = current?.rating || 'Neutral';
      const cleanNotes = editingNotesText.trim();
      await bridge.sendRequest('update_shard_rating_and_notes', {
        shardId,
        rating,
        notes: cleanNotes,
      });
      setServerShards((prev) =>
        prev.map((s) =>
          s.shardId === shardId ? { ...s, notes: cleanNotes } : s
        )
      );
      setEditingNotesShardId(null);
      setEditingNotesText('');
    } catch (err) {
      console.error('Failed to save shard notes:', err);
    }
  };

  const handleCopyCigSupport = (shard: ServerShardDto) => {
    const textToCopy = `Shard: ${shard.shardId} | Region: ${shard.region} | Playtime: ${Math.round(
      shard.totalSeconds / 60
    )}m | Disconnect/Exit: ${shard.lastEndReason || 'Normal Quit'}`;
    navigator.clipboard.writeText(textToCopy);
    setCopiedCigShardId(shard.shardId);
    setTimeout(() => {
      setCopiedCigShardId(null);
    }, 2500);
  };

  const formatShardDuration = (seconds: number) => {
    if (seconds <= 0) return '0 min';
    const hrs = Math.floor(seconds / 3600);
    const mins = Math.floor((seconds % 3600) / 60);
    if (hrs > 0) return `${hrs} Std ${mins} Min`;
    return `${mins} Min`;
  };

  const filteredShards = useMemo(() => {
    return serverShards.filter((shard) => {
      if (ratingFilter !== 'all' && shard.rating !== ratingFilter) return false;
      if (regionFilter !== 'all' && shard.region !== regionFilter) return false;
      if (search.trim()) {
        const q = search.toLowerCase();
        return (
          shard.shardId.toLowerCase().includes(q) ||
          shard.shardNumber.toLowerCase().includes(q) ||
          shard.region.toLowerCase().includes(q) ||
          (shard.notes && shard.notes.toLowerCase().includes(q))
        );
      }
      return true;
    });
  }, [serverShards, ratingFilter, regionFilter, search]);

  const ratingOptions = [
    { id: 'all', label: locale === 'en' ? 'All Ratings' : 'Alle Bewertungen' },
    { id: 'Good', label: locale === 'en' ? '⭐ Good Shards' : '⭐ Gute Shards' },
    { id: 'Avoid', label: locale === 'en' ? '⚠️ Avoid Shards' : '⚠️ Zu meiden' },
    { id: 'Neutral', label: locale === 'en' ? '⚪ Neutral' : '⚪ Neutral' },
  ];

  const regionOptions = [
    { id: 'all', label: locale === 'en' ? 'All Regions' : 'Alle Regionen' },
    { id: 'EU-Central', label: '🇪🇺 EU-Central' },
    { id: 'US-East', label: '🇺🇸 US-East' },
    { id: 'AUS', label: '🇦🇺 AUS' },
    { id: 'Asia', label: '🌏 Asia' },
  ];

  return (
    <div className="flex flex-col space-y-4">
      {/* Filter & Search Bar */}
      <div className="sc-glass rounded-lg p-3 border border-slate-800 flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-2">
          {/* Rating Filter */}
          <select
            value={ratingFilter}
            onChange={(e) => setRatingFilter(e.target.value)}
            className="bg-slate-900/80 border border-slate-800 rounded px-2.5 py-1.5 text-xs text-slate-300 focus:outline-none focus:border-cyan-500/50 cursor-pointer font-mono"
          >
            {ratingOptions.map((opt) => (
              <option key={opt.id} value={opt.id}>
                {opt.label}
              </option>
            ))}
          </select>

          {/* Region Filter */}
          <select
            value={regionFilter}
            onChange={(e) => setRegionFilter(e.target.value)}
            className="bg-slate-900/80 border border-slate-800 rounded px-2.5 py-1.5 text-xs text-slate-300 focus:outline-none focus:border-cyan-500/50 cursor-pointer font-mono"
          >
            {regionOptions.map((opt) => (
              <option key={opt.id} value={opt.id}>
                {opt.label}
              </option>
            ))}
          </select>
        </div>

        {/* Search & Reload */}
        <div className="flex items-center gap-2">
          <div className="relative">
            <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-500" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Shard-ID / Notiz suchen..."
              className="bg-slate-900/80 border border-slate-800 rounded pl-8 pr-7 py-1.5 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-cyan-500/50 w-52 font-mono"
            />
            {search && (
              <button
                type="button"
                onClick={() => setSearch('')}
                className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-500 hover:text-slate-200 p-0.5 rounded cursor-pointer"
              >
                <X className="w-3 h-3" />
              </button>
            )}
          </div>

          <button
            onClick={fetchServerShards}
            disabled={loading}
            className="p-1.5 rounded bg-slate-900 hover:bg-slate-800 text-slate-400 hover:text-cyan-400 border border-slate-800 transition cursor-pointer"
            title="Aktualisieren"
          >
            <RotateCcw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
          </button>
        </div>
      </div>

      {/* KPI HUD */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
        <div className="sc-glass p-3 rounded-lg border border-cyan-950/80 bg-[#040914]/90 sc-hud-corner">
          <div className="flex items-center justify-between text-[11px] text-slate-400 font-semibold uppercase tracking-wider">
            <span>Besuchte Shards</span>
            <Server className="w-4 h-4 text-cyan-400" />
          </div>
          <div className="mt-1 text-2xl font-black font-mono text-cyan-300">
            {serverShards.length}
          </div>
          <div className="text-[10px] text-slate-500 mt-0.5">Automatisch aus Star Citizen Logs erfasst</div>
        </div>

        <div className="sc-glass p-3 rounded-lg border border-cyan-950/80 bg-[#040914]/90 sc-hud-corner">
          <div className="flex items-center justify-between text-[11px] text-slate-400 font-semibold uppercase tracking-wider">
            <span>Gute Server (⭐)</span>
            <Star className="w-4 h-4 fill-emerald-400 text-emerald-400" />
          </div>
          <div className="mt-1 text-2xl font-black font-mono text-emerald-300">
            {serverShards.filter((s) => s.rating === 'Good').length}
          </div>
          <div className="text-[10px] text-slate-500 mt-0.5">Hohe Server-FPS, stabile Sessions</div>
        </div>

        <div className="sc-glass p-3 rounded-lg border border-cyan-950/80 bg-[#040914]/90 sc-hud-corner">
          <div className="flex items-center justify-between text-[11px] text-slate-400 font-semibold uppercase tracking-wider">
            <span>Zu meiden (⚠️)</span>
            <AlertTriangle className="w-4 h-4 text-rose-400" />
          </div>
          <div className="mt-1 text-2xl font-black font-mono text-rose-300">
            {serverShards.filter((s) => s.rating === 'Avoid').length}
          </div>
          <div className="text-[10px] text-slate-500 mt-0.5">30k Crashes, Lags oder Griefing</div>
        </div>

        <div className="sc-glass p-3 rounded-lg border border-cyan-950/80 bg-[#040914]/90 sc-hud-corner">
          <div className="flex items-center justify-between text-[11px] text-slate-400 font-semibold uppercase tracking-wider">
            <span>Erfasste Spielzeit</span>
            <Clock className="w-4 h-4 text-amber-400" />
          </div>
          <div className="mt-1 text-2xl font-black font-mono text-amber-300">
            {formatShardDuration(serverShards.reduce((acc, s) => acc + s.totalSeconds, 0))}
          </div>
          <div className="text-[10px] text-slate-500 mt-0.5">Gesamte aktive Zeit auf Servern</div>
        </div>
      </div>

      {/* Shards Cards Grid */}
      {filteredShards.length === 0 ? (
        <div className="sc-glass p-8 rounded-lg border border-slate-800 text-center text-slate-400 font-mono text-xs">
          Keine Server-Shards gefunden, die den Kriterien entsprechen.
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3 max-h-[55vh] overflow-y-auto pr-1">
          {filteredShards.map((shard) => {
            const isGood = shard.rating === 'Good';
            const isAvoid = shard.rating === 'Avoid';
            const isCopied = copiedCigShardId === shard.shardId;
            const isEditingNotes = editingNotesShardId === shard.shardId;
            const isCurrent = currentShardId && currentShardId.includes(shard.shardId);

            return (
              <div
                key={shard.shardId}
                className={`sc-glass rounded-lg p-3.5 border transition sc-hud-corner flex flex-col justify-between space-y-3 ${
                  isCurrent
                    ? 'ring-1 ring-cyan-400 border-cyan-500/70 bg-cyan-950/25 shadow-[0_0_12px_rgba(0,240,255,0.15)]'
                    : isGood
                    ? 'border-emerald-500/40 bg-emerald-950/10'
                    : isAvoid
                    ? 'border-rose-500/40 bg-rose-950/10'
                    : 'border-slate-800 bg-[#040914]/90'
                }`}
              >
                {/* Header */}
                <div>
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex items-center gap-2.5">
                      <span className="text-xl" title={shard.region}>
                        {shard.regionFlag || '🌐'}
                      </span>
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-bold text-white font-mono">
                            {shard.shardNumber ? `Shard ${shard.shardNumber}` : shard.shardId}
                          </span>
                          <span className="px-2 py-0.5 rounded text-[10px] font-bold font-mono bg-cyan-950/80 text-cyan-300 border border-cyan-800/60">
                            {shard.region}
                          </span>
                          {isCurrent && (
                            <span className="px-1.5 py-0.2 rounded text-[9px] font-bold font-mono bg-emerald-950 text-emerald-300 border border-emerald-500/60 animate-pulse">
                              AKTUELL AKTIV
                            </span>
                          )}
                        </div>
                        <div className="text-[11px] text-slate-400 font-mono select-all mt-0.5">
                          {shard.shardId}
                        </div>
                      </div>
                    </div>

                    {/* Rating Badge */}
                    <div>
                      {isGood && (
                        <span className="flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-bold bg-emerald-950/80 text-emerald-300 border border-emerald-500/60 shadow-[0_0_8px_rgba(16,185,129,0.25)]">
                          <Star className="w-3 h-3 fill-emerald-400 text-emerald-400" />
                          Gut
                        </span>
                      )}
                      {isAvoid && (
                        <span className="flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-bold bg-rose-950/80 text-rose-300 border border-rose-500/60 shadow-[0_0_8px_rgba(244,63,94,0.25)]">
                          <AlertTriangle className="w-3 h-3 text-rose-400" />
                          Meiden
                        </span>
                      )}
                      {!isGood && !isAvoid && (
                        <span className="px-2 py-0.5 rounded text-[10px] bg-slate-800/80 text-slate-400 font-mono">
                          Neutral
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Metadata Row */}
                  <div className="mt-2.5 pt-2 border-t border-slate-800/60 grid grid-cols-3 gap-2 text-[11px] font-mono text-slate-400">
                    <div>
                      <span className="text-slate-500 block text-[10px]">Besuche:</span>
                      <span className="text-slate-200 font-semibold">{shard.visitCount}x</span>
                    </div>
                    <div>
                      <span className="text-slate-500 block text-[10px]">Spielzeit:</span>
                      <span className="text-cyan-300 font-semibold">
                        {formatShardDuration(shard.totalSeconds)}
                      </span>
                    </div>
                    <div>
                      <span className="text-slate-500 block text-[10px]">Letzter Exit:</span>
                      <span className="text-amber-300 truncate block" title={shard.lastEndReason || 'Normal Quit'}>
                        {shard.lastEndReason || 'Normal Quit'}
                      </span>
                    </div>
                  </div>

                  {/* Notes Section */}
                  <div className="mt-2.5 bg-slate-950/60 rounded p-2 border border-slate-800/80 text-xs font-mono">
                    {isEditingNotes ? (
                      <div className="space-y-1.5">
                        <textarea
                          value={editingNotesText}
                          onChange={(e) => setEditingNotesText(e.target.value)}
                          placeholder="Eigene Notizen zu diesem Shard eingeben (z. B. 'Sehr stabil', 'Griefing an Seraphim', 'Lags')..."
                          className="w-full bg-slate-900 border border-cyan-500/50 rounded p-1.5 text-xs text-slate-200 focus:outline-none resize-none h-16"
                        />
                        <div className="flex justify-end gap-1.5">
                          <button
                            onClick={() => {
                              setEditingNotesShardId(null);
                              setEditingNotesText('');
                            }}
                            className="px-2 py-0.5 rounded bg-slate-800 hover:bg-slate-700 text-slate-300 text-[10px]"
                          >
                            Abbrechen
                          </button>
                          <button
                            onClick={() => handleSaveNotes(shard.shardId)}
                            className="px-2 py-0.5 rounded bg-cyan-600 hover:bg-cyan-500 text-slate-950 font-bold text-[10px] flex items-center gap-1"
                          >
                            <Check className="w-3 h-3" /> Speichern
                          </button>
                        </div>
                      </div>
                    ) : (
                      <div className="flex items-start justify-between gap-2">
                        <div className="text-slate-300 text-[11px] italic flex-1">
                          {shard.notes ? (
                            <span>„{shard.notes}“</span>
                          ) : (
                            <span className="text-slate-500 not-italic">Keine Notiz vorhanden.</span>
                          )}
                        </div>
                        <button
                          onClick={() => {
                            setEditingNotesShardId(shard.shardId);
                            setEditingNotesText(shard.notes || '');
                          }}
                          className="text-slate-500 hover:text-cyan-400 p-0.5 rounded cursor-pointer transition shrink-0"
                          title="Notiz bearbeiten"
                        >
                          <Edit2 className="w-3 h-3" />
                        </button>
                      </div>
                    )}
                  </div>
                </div>

                {/* Footer Controls: Rating Toggles & CIG Support String */}
                <div className="flex items-center justify-between gap-2 pt-1 border-t border-slate-800/80">
                  {/* Rating Selector */}
                  <div className="flex items-center gap-1 text-[11px] font-mono">
                    <span className="text-slate-500 text-[10px] mr-1">Bewertung:</span>
                    <button
                      onClick={() => handleSetRating(shard.shardId, isGood ? 'Neutral' : 'Good')}
                      className={`px-1.5 py-0.5 rounded text-[10px] font-semibold border transition cursor-pointer flex items-center gap-1 ${
                        isGood
                          ? 'bg-emerald-950 text-emerald-300 border-emerald-500/80'
                          : 'bg-slate-900 text-slate-400 border-slate-800 hover:text-emerald-400 hover:border-emerald-700'
                      }`}
                      title="Als stabilen / guten Shard markieren"
                    >
                      <Star className="w-2.5 h-2.5" />
                      <span>Gut</span>
                    </button>

                    <button
                      onClick={() => handleSetRating(shard.shardId, isAvoid ? 'Neutral' : 'Avoid')}
                      className={`px-1.5 py-0.5 rounded text-[10px] font-semibold border transition cursor-pointer flex items-center gap-1 ${
                        isAvoid
                          ? 'bg-rose-950 text-rose-300 border-rose-500/80'
                          : 'bg-slate-900 text-slate-400 border-slate-800 hover:text-rose-400 hover:border-rose-700'
                      }`}
                      title="Als instabilen / fehlerhaften Shard markieren"
                    >
                      <AlertTriangle className="w-2.5 h-2.5" />
                      <span>Meiden</span>
                    </button>
                  </div>

                  {/* 1-Click CIG Support Copy */}
                  <button
                    onClick={() => handleCopyCigSupport(shard)}
                    className="flex items-center gap-1 px-2 py-1 rounded bg-slate-900 hover:bg-slate-800 border border-slate-700 hover:border-cyan-500/50 text-slate-300 text-[10px] font-mono transition cursor-pointer shadow-xs"
                    title="Kopiert standardisierten Diagnosestring für CIG Support & Issue Council Reports"
                  >
                    {isCopied ? (
                      <>
                        <CheckCircle2 className="w-3 h-3 text-emerald-400" />
                        <span className="text-emerald-400 font-bold">Kopiert!</span>
                      </>
                    ) : (
                      <>
                        <ClipboardCopy className="w-3 h-3 text-cyan-400" />
                        <span>CIG-String kopieren</span>
                      </>
                    )}
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
