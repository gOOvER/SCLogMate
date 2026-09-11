import React, { useEffect, useState, useMemo } from 'react';
import { bridge, BlueprintDto } from '../services/photinoBridge';
import {
  Scroll,
  CheckCircle2,
  AlertCircle,
  Search,
  Hammer,
  Shield,
  Crosshair,
  Cpu,
  Layers,
  Sparkles,
} from 'lucide-react';

export const BlueprintsView: React.FC = () => {
  const [blueprints, setBlueprints] = useState<BlueprintDto[]>([]);
  const [search, setSearch] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'learned' | 'missing'>('all');
  const [categoryFilter, setCategoryFilter] = useState<string>('all');

  const fetchBlueprints = async () => {
    try {
      const res = await bridge.sendRequest<BlueprintDto[]>('get_blueprints');
      setBlueprints(res || []);
    } catch (err) {
      console.error('Failed to load blueprints:', err);
    }
  };

  useEffect(() => {
    fetchBlueprints();
  }, []);

  // Compute stats
  const totalCount = blueprints.length;
  const learnedCount = blueprints.filter((b) => b.isLearned).length;
  const missingCount = totalCount - learnedCount;
  const progressPercent = totalCount > 0 ? Math.round((learnedCount / totalCount) * 100) : 0;

  // Extract distinct categories
  const categories = useMemo(() => {
    const cats = new Set<string>();
    blueprints.forEach((b) => {
      if (b.category) cats.add(b.category);
    });
    return Array.from(cats).sort();
  }, [blueprints]);

  // Filtered blueprints
  const filteredBlueprints = useMemo(() => {
    return blueprints.filter((b) => {
      // Status filter
      if (statusFilter === 'learned' && !b.isLearned) return false;
      if (statusFilter === 'missing' && b.isLearned) return false;

      // Category filter
      if (categoryFilter !== 'all' && b.category !== categoryFilter) return false;

      // Search filter
      if (search.trim()) {
        const query = search.toLowerCase();
        const matchesName = b.name.toLowerCase().includes(query);
        const matchesSubCat = b.subCategory?.toLowerCase().includes(query);
        const matchesUnlock = b.unlockInfo?.toLowerCase().includes(query);
        const matchesMats = b.requiredMaterials?.toLowerCase().includes(query);
        return matchesName || matchesSubCat || matchesUnlock || matchesMats;
      }

      return true;
    });
  }, [blueprints, statusFilter, categoryFilter, search]);

  const getRarityBadge = (rarity: string) => {
    const r = (rarity || 'Common').toLowerCase();
    if (r.includes('legendary') || r.includes('legende')) {
      return 'bg-amber-500/20 text-amber-300 border-amber-500/40 shadow-[0_0_10px_rgba(245,158,11,0.2)]';
    }
    if (r.includes('epic') || r.includes('episch')) {
      return 'bg-purple-500/20 text-purple-300 border-purple-500/40 shadow-[0_0_10px_rgba(168,85,247,0.2)]';
    }
    if (r.includes('rare') || r.includes('selten')) {
      return 'bg-blue-500/20 text-blue-300 border-blue-500/40 shadow-[0_0_10px_rgba(59,130,246,0.2)]';
    }
    return 'bg-slate-800 text-slate-300 border-slate-700';
  };

  const getCategoryIcon = (category: string) => {
    const c = (category || '').toLowerCase();
    if (c.includes('waffe') || c.includes('weapon')) return <Crosshair className="w-4 h-4 text-rose-400" />;
    if (c.includes('rüstung') || c.includes('armor')) return <Shield className="w-4 h-4 text-cyan-400" />;
    if (c.includes('komponente') || c.includes('component')) return <Cpu className="w-4 h-4 text-amber-400" />;
    if (c.includes('werkzeug') || c.includes('tool')) return <Hammer className="w-4 h-4 text-emerald-400" />;
    return <Layers className="w-4 h-4 text-slate-400" />;
  };

  return (
    <div className="flex flex-col min-h-full space-y-4">
      {/* Top Stat Bar */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
        <div className="sc-glass rounded-lg p-3 border border-slate-800 flex items-center justify-between">
          <div>
            <div className="text-xs text-slate-400 font-mono tracking-wider">GESAMT BAUPLÄNE</div>
            <div className="text-xl font-bold text-slate-100 mt-0.5">{totalCount}</div>
          </div>
          <div className="p-2.5 rounded-lg bg-cyan-500/10 text-cyan-400 border border-cyan-500/20">
            <Scroll className="w-5 h-5" />
          </div>
        </div>

        <div className="sc-glass rounded-lg p-3 border border-slate-800 flex items-center justify-between">
          <div>
            <div className="text-xs text-slate-400 font-mono tracking-wider">ERLERNT / FREIGESCHALTET</div>
            <div className="text-xl font-bold text-emerald-400 mt-0.5">{learnedCount}</div>
          </div>
          <div className="p-2.5 rounded-lg bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
            <CheckCircle2 className="w-5 h-5" />
          </div>
        </div>

        <div className="sc-glass rounded-lg p-3 border border-slate-800 flex items-center justify-between">
          <div>
            <div className="text-xs text-slate-400 font-mono tracking-wider">FEHLEND</div>
            <div className="text-xl font-bold text-slate-400 mt-0.5">{missingCount}</div>
          </div>
          <div className="p-2.5 rounded-lg bg-slate-800 text-slate-400 border border-slate-700">
            <AlertCircle className="w-5 h-5" />
          </div>
        </div>

        <div className="sc-glass rounded-lg p-3 border border-slate-800 flex flex-col justify-between">
          <div className="flex items-center justify-between">
            <div className="text-xs text-slate-400 font-mono tracking-wider">FORTSCHRITT</div>
            <span className="text-sm font-mono font-bold text-cyan-400">{progressPercent}%</span>
          </div>
          <div className="w-full bg-slate-800/80 rounded-full h-2 mt-2 overflow-hidden border border-slate-700">
            <div
              className="bg-gradient-to-r from-cyan-500 to-emerald-400 h-full rounded-full transition-all duration-500 shadow-[0_0_10px_rgba(0,240,255,0.4)]"
              style={{ width: `${progressPercent}%` }}
            />
          </div>
        </div>
      </div>

      {/* Filter & Search Bar */}
      <div className="sc-glass rounded-lg p-3 border border-slate-800 flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          {/* Status buttons */}
          <div className="flex items-center bg-slate-900/80 p-1 rounded-md border border-slate-800">
            <button
              onClick={() => setStatusFilter('all')}
              className={`px-3 py-1 text-xs font-semibold rounded cursor-pointer transition ${
                statusFilter === 'all'
                  ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40 shadow-[0_0_8px_rgba(0,240,255,0.2)]'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              Alle ({totalCount})
            </button>
            <button
              onClick={() => setStatusFilter('learned')}
              className={`px-3 py-1 text-xs font-semibold rounded cursor-pointer transition ${
                statusFilter === 'learned'
                  ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/40 shadow-[0_0_8px_rgba(16,185,129,0.2)]'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              Erlernt ({learnedCount})
            </button>
            <button
              onClick={() => setStatusFilter('missing')}
              className={`px-3 py-1 text-xs font-semibold rounded cursor-pointer transition ${
                statusFilter === 'missing'
                  ? 'bg-slate-700 text-slate-200 border border-slate-600'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              Fehlend ({missingCount})
            </button>
          </div>

          {/* Category dropdown */}
          <select
            value={categoryFilter}
            onChange={(e) => setCategoryFilter(e.target.value)}
            className="bg-slate-900/90 border border-slate-700 rounded-md px-3 py-1.5 text-xs text-slate-200 focus:outline-none focus:border-cyan-500 font-mono cursor-pointer"
          >
            <option value="all">Alle Kategorien</option>
            {categories.map((cat) => (
              <option key={cat} value={cat}>
                {cat}
              </option>
            ))}
          </select>
        </div>

        <div className="flex items-center gap-2">
          {/* Search box */}
          <div className="relative">
            <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-500" />
            <input
              type="text"
              placeholder="Bauplan suchen..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="bg-slate-900/90 border border-slate-700 rounded-md pl-8 pr-3 py-1.5 text-xs text-slate-200 focus:outline-none focus:border-cyan-500 w-48 lg:w-64 placeholder:text-slate-600 font-mono"
            />
          </div>
        </div>
      </div>

      {/* Grid of Blueprints */}
      <div className="flex-1 overflow-y-auto pr-1">
        {filteredBlueprints.length === 0 ? (
          <div className="sc-glass rounded-lg p-12 border border-slate-800 text-center flex flex-col items-center justify-center space-y-3">
            <Scroll className="w-12 h-12 text-slate-600" />
            <div className="text-slate-300 font-medium">Keine Baupläne gefunden</div>
            <div className="text-xs text-slate-500 max-w-md">
              Es gibt keine Einträge, die den ausgewählten Kriterien entsprechen. Ändere die Filter oder starte einen Log-Scan.
            </div>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3">
            {filteredBlueprints.map((bp) => {
              return (
                <div
                  key={bp.id}
                  className={`sc-glass rounded-lg p-3.5 border transition-all duration-200 flex flex-col justify-between relative overflow-hidden group ${
                    bp.isLearned
                      ? 'border-emerald-500/30 hover:border-emerald-500/60 shadow-[0_0_15px_rgba(16,185,129,0.05)]'
                      : 'border-slate-800 hover:border-slate-700 opacity-85 hover:opacity-100'
                  }`}
                >
                  {/* Subtle Sci-Fi background indicator */}
                  <div
                    className={`absolute top-0 right-0 w-24 h-24 blur-2xl rounded-full pointer-events-none transition-opacity ${
                      bp.isLearned ? 'bg-emerald-500/10' : 'bg-slate-700/5'
                    }`}
                  />

                  <div>
                    {/* Header: Category + Badges */}
                    <div className="flex items-center justify-between gap-2 mb-2">
                      <div className="flex items-center gap-1.5 text-xs text-slate-400">
                        {getCategoryIcon(bp.category)}
                        <span className="font-semibold text-slate-300">{bp.category}</span>
                        {bp.subCategory && (
                          <>
                            <span className="text-slate-600">/</span>
                            <span className="text-slate-400">{bp.subCategory}</span>
                          </>
                        )}
                      </div>

                      <div className="flex items-center gap-1.5 shrink-0">
                        {bp.rarity && (
                          <span
                            className={`px-2 py-0.5 text-[10px] font-mono uppercase tracking-wider rounded border ${getRarityBadge(
                              bp.rarity
                            )}`}
                          >
                            {bp.rarity}
                          </span>
                        )}

                        {bp.isLearned ? (
                          <span className="flex items-center gap-1 px-2 py-0.5 text-[10px] font-semibold rounded bg-emerald-500/20 text-emerald-300 border border-emerald-500/40">
                            <CheckCircle2 className="w-3 h-3" />
                            Erlernt
                          </span>
                        ) : (
                          <span className="px-2 py-0.5 text-[10px] font-medium rounded bg-slate-800/80 text-slate-400 border border-slate-700">
                            Fehlend
                          </span>
                        )}
                      </div>
                    </div>

                    {/* Blueprint Title */}
                    <h3 className="text-sm font-bold text-slate-100 group-hover:text-cyan-300 transition-colors">
                      {bp.name}
                    </h3>

                    {/* Unlock Info / Source */}
                    {bp.unlockInfo && (
                      <div className="mt-2 text-xs text-slate-400 flex items-start gap-1.5 bg-slate-900/60 p-2 rounded border border-slate-800/80">
                        <Sparkles className="w-3.5 h-3.5 text-amber-400 shrink-0 mt-0.5" />
                        <span className="text-slate-300 leading-relaxed">{bp.unlockInfo}</span>
                      </div>
                    )}

                    {/* Required Materials */}
                    {bp.requiredMaterials && (
                      <div className="mt-2">
                        <div className="text-[10px] font-mono text-slate-500 uppercase tracking-wider mb-1">
                          Benötigte Materialien
                        </div>
                        <div className="text-xs text-slate-300 font-mono bg-slate-950/40 p-1.5 rounded border border-slate-800/50">
                          {bp.requiredMaterials}
                        </div>
                      </div>
                    )}
                  </div>

                  {/* Footer date if learned */}
                  {bp.isLearned && bp.learnedDate && (
                    <div className="mt-3 pt-2 border-t border-slate-800/80 flex items-center justify-between text-[11px] font-mono text-slate-500">
                      <span>Erlernt am:</span>
                      <span className="text-emerald-400/90">{bp.learnedDate}</span>
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
};
