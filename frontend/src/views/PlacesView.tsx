import React, { useEffect, useState, useMemo } from 'react';
import { bridge, PlaceItemDto } from '../services/photinoBridge';
import {
  Compass,
  Search,
  RefreshCw,
  Copy,
  Check,
} from 'lucide-react';

export const PlacesView: React.FC = () => {
  const [places, setPlaces] = useState<PlaceItemDto[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [search, setSearch] = useState<string>('');
  const [systemFilter, setSystemFilter] = useState<string>('all');
  const [typeFilter, setTypeFilter] = useState<string>('all');
  const [copiedId, setCopiedId] = useState<string | null>(null);

  const fetchPlaces = async () => {
    try {
      setLoading(true);
      const res = await bridge.sendRequest<PlaceItemDto[]>('get_places');
      setPlaces(res || []);
    } catch (err) {
      console.error('Failed to load places:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPlaces();
  }, []);

  const types = [
    { id: 'all', label: 'Alle Orte' },
    { id: 'Planet', label: '🪐 Planeten' },
    { id: 'Moon', label: '🌑 Monde' },
    { id: 'LandingZone', label: '🏙️ Landezonen' },
    { id: 'SpaceStation', label: '🛰️ Raumstationen' },
    { id: 'LagrangeStation', label: '⛽ Lagrange (L1-L5)' },
    { id: 'Outpost', label: '🏭 Außenposten' },
  ];

  const filteredPlaces = useMemo(() => {
    return places.filter((p) => {
      if (systemFilter !== 'all' && p.system.toLowerCase() !== systemFilter.toLowerCase()) {
        return false;
      }
      if (typeFilter !== 'all' && p.type.toLowerCase() !== typeFilter.toLowerCase()) {
        return false;
      }
      if (search.trim()) {
        const query = search.toLowerCase();
        const matchesName = p.name.toLowerCase().includes(query);
        const matchesParent = p.parentBody?.toLowerCase().includes(query);
        const matchesDesc = p.description?.toLowerCase().includes(query);
        const matchesSpec = p.specialization?.toLowerCase().includes(query);
        return matchesName || matchesParent || matchesDesc || matchesSpec;
      }
      return true;
    });
  }, [places, systemFilter, typeFilter, search]);

  const handleCopy = (place: PlaceItemDto) => {
    const text = `${place.name} · ${place.parentBody ? `${place.parentBody}, ` : ''}${place.system}`;
    navigator.clipboard.writeText(text).then(() => {
      setCopiedId(place.id);
      setTimeout(() => setCopiedId(null), 2000);
    });
  };

  return (
    <div className="flex flex-col h-full space-y-4">
      {/* Top Filter & Search Bar */}
      <div className="sc-glass rounded-lg p-3 border border-slate-800 flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          {/* System filter */}
          <div className="flex items-center bg-slate-900/80 p-1 rounded-md border border-slate-800">
            <button
              onClick={() => setSystemFilter('all')}
              className={`px-3 py-1 text-xs font-semibold rounded cursor-pointer transition ${
                systemFilter === 'all'
                  ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40 shadow-[0_0_8px_rgba(0,240,255,0.2)]'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              Alle Systeme
            </button>
            <button
              onClick={() => setSystemFilter('Stanton')}
              className={`px-3 py-1 text-xs font-semibold rounded cursor-pointer transition ${
                systemFilter === 'Stanton'
                  ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40 shadow-[0_0_8px_rgba(0,240,255,0.2)]'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              Stanton
            </button>
            <button
              onClick={() => setSystemFilter('Pyro')}
              className={`px-3 py-1 text-xs font-semibold rounded cursor-pointer transition ${
                systemFilter === 'Pyro'
                  ? 'bg-amber-500/20 text-amber-300 border border-amber-500/40 shadow-[0_0_8px_rgba(245,158,11,0.2)]'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              Pyro
            </button>
          </div>

          {/* Type dropdown */}
          <select
            value={typeFilter}
            onChange={(e) => setTypeFilter(e.target.value)}
            className="bg-slate-900 border border-slate-800 rounded px-2.5 py-1.5 text-xs text-slate-200 focus:outline-none focus:border-cyan-500 cursor-pointer font-mono"
          >
            {types.map((t) => (
              <option key={t.id} value={t.id}>
                {t.label}
              </option>
            ))}
          </select>
        </div>

        <div className="flex items-center gap-2">
          <div className="relative">
            <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-500" />
            <input
              type="text"
              placeholder="Ort, Station oder Mond filtern..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="bg-slate-900 border border-slate-800 rounded pl-8 pr-3 py-1.5 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-cyan-500 w-56 lg:w-72 font-mono"
            />
          </div>

          <button
            onClick={fetchPlaces}
            title="Aktualisieren"
            className="p-1.5 rounded bg-slate-900 border border-slate-800 text-slate-400 hover:text-cyan-400 transition cursor-pointer"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin text-cyan-400' : ''}`} />
          </button>
        </div>
      </div>

      {/* Grid of Places */}
      <div className="flex-1 overflow-y-auto pr-1">
        {filteredPlaces.length === 0 ? (
          <div className="sc-glass rounded-lg p-12 border border-slate-800 text-center flex flex-col items-center justify-center space-y-2">
            <Compass className="w-10 h-10 text-slate-600" />
            <div className="text-slate-300 font-medium">Keine Orte gefunden</div>
            <div className="text-xs text-slate-500">Passe die Suchfilter an oder wähle ein anderes Sternensystem.</div>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3">
            {filteredPlaces.map((p) => {
              const isCopied = copiedId === p.id;

              return (
                <div
                  key={p.id}
                  className="sc-glass rounded-lg p-3.5 border border-slate-800 hover:border-cyan-500/40 transition-all duration-200 flex flex-col justify-between group sc-hud-corner"
                >
                  <div>
                    {/* Header */}
                    <div className="flex items-start justify-between gap-2">
                      <div className="flex items-center gap-2.5">
                        <span className="text-xl shrink-0 p-1.5 rounded-lg bg-slate-900/80 border border-slate-800">
                          {p.icon}
                        </span>
                        <div>
                          <h3 className="text-sm font-bold text-slate-100 group-hover:text-cyan-300 transition-colors">
                            {p.name}
                          </h3>
                          <div className="text-[10px] font-mono text-slate-500">
                            {p.system} {p.parentBody ? `· ${p.parentBody}` : ''}
                          </div>
                        </div>
                      </div>

                      <div className="flex items-center gap-1 shrink-0">
                        <span
                          className={`px-2 py-0.5 text-[9px] font-semibold rounded border uppercase tracking-wider ${
                            p.securityLevel?.toLowerCase() === 'high'
                              ? 'bg-emerald-500/15 text-emerald-300 border-emerald-500/30'
                              : p.securityLevel?.toLowerCase() === 'lawless'
                              ? 'bg-rose-500/15 text-rose-300 border-rose-500/30'
                              : 'bg-amber-500/15 text-amber-300 border-amber-500/30'
                          }`}
                        >
                          {p.securityLevel || 'Medium'}
                        </span>
                      </div>
                    </div>

                    {/* Description */}
                    <p className="text-xs text-slate-400 mt-2.5 leading-relaxed line-clamp-2">
                      {p.description}
                    </p>

                    {/* Specialization Pill */}
                    {p.specialization && (
                      <div className="mt-2.5 text-[11px] font-mono text-cyan-300 bg-cyan-950/20 border border-cyan-500/20 px-2 py-1 rounded">
                        {p.specialization}
                      </div>
                    )}
                  </div>

                  {/* Footer with Armistice indicator & Copy Action */}
                  <div className="mt-3 pt-2.5 border-t border-slate-800/80 flex items-center justify-between text-[11px] font-mono">
                    <span className={p.hasArmistice ? 'text-emerald-400' : 'text-rose-400'}>
                      {p.hasArmistice ? '🟢 Waffenruhe' : '🔴 Waffen scharf'}
                    </span>

                    <button
                      onClick={() => handleCopy(p)}
                      title="Name in Zwischenablage kopieren"
                      className="flex items-center gap-1 px-2 py-1 rounded bg-slate-900 border border-slate-800 text-slate-400 hover:text-cyan-300 hover:border-cyan-500/30 transition cursor-pointer"
                    >
                      {isCopied ? <Check className="w-3 h-3 text-emerald-400" /> : <Copy className="w-3 h-3" />}
                      <span className="text-[10px]">{isCopied ? 'Kopiert' : 'Kopieren'}</span>
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
};
