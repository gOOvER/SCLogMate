import React, { useEffect, useState } from 'react';
import { bridge, FactionReputationDto } from '../services/photinoBridge';
import { RefreshCw } from 'lucide-react';

export const ReputationView: React.FC = () => {
  const [factions, setFactions] = useState<FactionReputationDto[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [loading, setLoading] = useState<boolean>(false);

  const fetchReputation = async () => {
    try {
      setLoading(true);
      const res = await bridge.sendRequest<FactionReputationDto[]>('get_reputation');
      setFactions(res);
    } catch (err) {
      console.error('Failed to load reputation:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchReputation();
  }, []);

  const categories = [
    { id: 'all', label: 'Alle Organisationen' },
    { id: 'Sicherheit', label: '🛡️ Sicherheit & Kopfgeld' },
    { id: 'Fracht', label: '📦 Fracht & Transport' },
    { id: 'Industrie', label: '⛏️ Industrie & Bergbau' },
    { id: 'Unterwelt', label: '☠️ Unterwelt & Syndikate' },
  ];

  const filtered = factions.filter(
    (f) => selectedCategory === 'all' || f.category === selectedCategory
  );

  return (
    <div className="flex flex-col h-full space-y-4">
      {/* Category Filter Toolbar */}
      <div className="sc-glass rounded-lg p-3 border border-slate-800 flex items-center justify-between">
        <div className="flex items-center gap-2 overflow-x-auto">
          {categories.map((c) => (
            <button
              key={c.id}
              onClick={() => setSelectedCategory(c.id)}
              className={`px-3 py-1.5 text-xs font-semibold rounded transition cursor-pointer shrink-0 ${
                selectedCategory === c.id
                  ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40 shadow-[0_0_10px_rgba(0,240,255,0.2)]'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900 border border-transparent'
              }`}
            >
              {c.label}
            </button>
          ))}
        </div>

        <button
          onClick={fetchReputation}
          title="Aktualisieren"
          className="p-1.5 rounded bg-slate-900 border border-slate-800 text-slate-400 hover:text-slate-200 cursor-pointer"
        >
          <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
        </button>
      </div>

      {/* Faction Cards Grid */}
      <div className="flex-1 overflow-y-auto grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 pr-1">
        {filtered.map((f) => (
          <div
            key={f.id}
            className="sc-glass rounded-lg p-4 border border-slate-800 flex flex-col justify-between sc-hud-corner hover:border-cyan-500/30 transition group"
          >
            <div>
              {/* Header */}
              <div className="flex items-start justify-between">
                <div className="flex items-center gap-2.5">
                  <div className="w-8 h-8 rounded-lg bg-cyan-950/40 border border-cyan-500/30 text-cyan-400 flex items-center justify-center text-sm shadow-[0_0_10px_rgba(0,240,255,0.15)]">
                    {f.icon || '🛡'}
                  </div>
                  <div>
                    <h3 className="font-semibold text-sm text-slate-200 group-hover:text-cyan-300 transition">
                      {f.name}
                    </h3>
                    <div className="text-[10px] text-slate-400 font-mono">
                      {f.system} · {f.category}
                    </div>
                  </div>
                </div>

                <span className="sc-badge text-[10px]">
                  Rang {f.currentLevel}
                </span>
              </div>

              {/* Description */}
              <p className="text-xs text-slate-400 mt-3 leading-relaxed line-clamp-2">
                {f.description}
              </p>
            </div>

            {/* Level & Progress */}
            <div className="mt-4 pt-3 border-t border-slate-800/80">
              <div className="flex justify-between items-center text-xs mb-1.5 font-mono">
                <span className="text-amber-300 font-semibold">{f.levelTitle}</span>
                <span className="text-slate-400 text-[11px]">{f.progressText}</span>
              </div>

              {/* Progress Bar */}
              <div className="w-full h-2 rounded-full bg-slate-900 border border-slate-800 overflow-hidden relative">
                <div
                  className="h-full rounded-full bg-gradient-to-r from-cyan-500 to-amber-400 transition-all duration-500 shadow-[0_0_8px_rgba(0,240,255,0.4)]"
                  style={{ width: `${Math.min(100, Math.max(0, f.progressPercent))}%` }}
                />
              </div>

              <div className="mt-2 flex items-center justify-between text-[11px] text-slate-500 font-mono">
                <span>{f.completedMissions} Aufträge abgeschlossen</span>
                <span>{f.currentXp.toLocaleString('de-DE')} XP gesamt</span>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
