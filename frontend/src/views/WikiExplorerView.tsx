import React, { useState, useEffect, useMemo } from 'react';
import {
  Rocket,
  Search,
  Box,
  Users,
  ExternalLink,
  Filter,
  Loader2,
  Info,
} from 'lucide-react';
import { WikiInfo, bridge } from '../services/photinoBridge';

interface WikiExplorerViewProps {
  onOpenDossier: (itemOrQuery: string | WikiInfo) => void;
}

const MANUFACTURERS = [
  'Alle',
  'Aegis Dynamics',
  'Anvil Aerospace',
  'Argo Astronautics',
  'Crusader Industries',
  'Drake Interplanetary',
  'MISC',
  'Origin Jumpworks',
  'Roberts Space Industries',
  'Consolidated Outland',
  'Esperia',
  'Banu',
  'Gatac Manufacture',
];

export const WikiExplorerView: React.FC<WikiExplorerViewProps> = ({ onOpenDossier }) => {
  const [query, setQuery] = useState<string>('');
  const [category, setCategory] = useState<'all' | 'ships' | 'items'>('ships');
  const [selectedManufacturer, setSelectedManufacturer] = useState<string>('Alle');
  const [results, setResults] = useState<WikiInfo[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);

  const executeSearch = (searchTerm: string, cat: string) => {
    setIsLoading(true);
    bridge.searchWiki(searchTerm, cat, 40)
      .then((data) => {
        setResults(data || []);
      })
      .catch((err) => {
        console.error('Wiki search error:', err);
      })
      .finally(() => {
        setIsLoading(false);
      });
  };

  useEffect(() => {
    // Initial load: Search top ships
    executeSearch(query || 'Cutlass', category);
  }, []);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    executeSearch(query, category);
  };

  const handleCategoryChange = (newCat: 'all' | 'ships' | 'items') => {
    setCategory(newCat);
    executeSearch(query, newCat);
  };

  // Filtered by selected manufacturer
  const filteredResults = useMemo(() => {
    if (selectedManufacturer === 'Alle') return results;
    return results.filter((item) =>
      item.manufacturer?.toLowerCase().includes(selectedManufacturer.toLowerCase())
    );
  }, [results, selectedManufacturer]);

  return (
    <div className="space-y-5 animate-in fade-in duration-200">
      {/* Top Banner / Hero */}
      <div className="sc-glass border border-cyan-500/30 rounded-2xl p-5 relative overflow-hidden shadow-xl shadow-cyan-950/40">
        <div className="absolute top-0 left-0 right-0 h-1 bg-gradient-to-r from-transparent via-cyan-400 to-transparent" />
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="flex items-center space-x-3.5">
            <div className="w-12 h-12 rounded-xl bg-cyan-500/10 border border-cyan-500/40 flex items-center justify-center text-cyan-400 shadow-[0_0_20px_rgba(6,182,212,0.25)] shrink-0">
              <Rocket className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <h1 className="text-lg font-bold text-white font-mono tracking-wide uppercase">
                  STAR CITIZEN WIKI EXPLORER
                </h1>
                <span className="px-2 py-0.5 rounded text-[10px] font-mono font-bold bg-cyan-950/80 border border-cyan-500/50 text-cyan-300">
                  OFFLINE CACHE
                </span>
              </div>
              <p className="text-xs text-slate-400 font-mono mt-0.5">
                Katalog aller Schiffe, Fahrzeuge, Waffen und Ausrüstung mit lokalem HD-Bild- & Spezifikationen-Cache.
              </p>
            </div>
          </div>

          {/* External Quick Link */}
          <button
            onClick={() => bridge.openExternalUrl('https://star-citizen.wiki')}
            className="px-3 py-1.5 rounded-xl text-xs font-mono font-bold bg-cyan-950/80 border border-cyan-500/40 hover:bg-cyan-900/60 text-cyan-300 transition flex items-center gap-1.5 self-start md:self-auto cursor-pointer"
          >
            <ExternalLink className="w-3.5 h-3.5" />
            <span>star-citizen.wiki öffnen</span>
          </button>
        </div>

        {/* Search & Category Filter Bar */}
        <div className="mt-5 pt-4 border-t border-cyan-950/80 flex flex-col md:flex-row gap-3">
          <form onSubmit={handleSearchSubmit} className="flex-1 flex gap-2">
            <div className="relative flex-1">
              <Search className="w-4 h-4 text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2" />
              <input
                type="text"
                placeholder="Schiff, Fahrzeug oder Ausrüstung suchen (z. B. Carrack, Cutlass, Gladius, P8-SC)..."
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                className="w-full bg-[#030914] border border-cyan-950/90 rounded-xl pl-10 pr-4 py-2.5 text-xs font-mono text-white placeholder-slate-500 focus:outline-none focus:border-cyan-500/60 transition"
              />
            </div>
            <button
              type="submit"
              className="px-4 py-2.5 rounded-xl text-xs font-mono font-bold bg-cyan-600 hover:bg-cyan-500 text-white transition flex items-center gap-2 cursor-pointer shadow-lg shadow-cyan-950/60"
            >
              <Search className="w-3.5 h-3.5" />
              <span>Suchen</span>
            </button>
          </form>

          {/* Category Toggle */}
          <div className="flex bg-[#030914] p-1 rounded-xl border border-cyan-950/90 gap-1 shrink-0 font-mono text-xs">
            <button
              onClick={() => handleCategoryChange('ships')}
              className={`px-3 py-1.5 rounded-lg font-bold transition cursor-pointer ${
                category === 'ships'
                  ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              Schiffe & Fahrzeuge
            </button>
            <button
              onClick={() => handleCategoryChange('items')}
              className={`px-3 py-1.5 rounded-lg font-bold transition cursor-pointer ${
                category === 'items'
                  ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              Items & Waffen
            </button>
            <button
              onClick={() => handleCategoryChange('all')}
              className={`px-3 py-1.5 rounded-lg font-bold transition cursor-pointer ${
                category === 'all'
                  ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40'
                  : 'text-slate-400 hover:text-white'
              }`}
            >
              Alle
            </button>
          </div>
        </div>

        {/* Manufacturer Chips */}
        <div className="mt-3 flex items-center gap-1.5 overflow-x-auto pb-1 text-xs font-mono">
          <span className="text-[10px] uppercase text-slate-500 flex items-center gap-1 mr-1 shrink-0">
            <Filter className="w-3 h-3 text-cyan-400" />
            Hersteller:
          </span>
          {MANUFACTURERS.map((mfg) => (
            <button
              key={mfg}
              onClick={() => setSelectedManufacturer(mfg)}
              className={`px-2.5 py-1 rounded-lg shrink-0 transition cursor-pointer text-[11px] ${
                selectedManufacturer === mfg
                  ? 'bg-cyan-500/25 border border-cyan-500/60 text-cyan-300 font-bold'
                  : 'bg-slate-900/60 border border-slate-800 text-slate-400 hover:text-slate-200 hover:border-slate-700'
              }`}
            >
              {mfg.replace(' Dynamics', '').replace(' Aerospace', '').replace(' Industries', '').replace(' Interplanetary', '')}
            </button>
          ))}
        </div>
      </div>

      {/* Grid Results */}
      {isLoading ? (
        <div className="sc-glass border border-cyan-950/80 rounded-2xl py-20 flex flex-col items-center justify-center space-y-3 font-mono text-cyan-400">
          <Loader2 className="w-8 h-8 animate-spin text-cyan-400" />
          <p className="text-xs uppercase tracking-wider text-slate-400">
            Durchsuche Star Citizen Wiki Datenbank...
          </p>
        </div>
      ) : filteredResults.length === 0 ? (
        <div className="sc-glass border border-cyan-950/80 rounded-2xl py-16 flex flex-col items-center justify-center space-y-3 font-mono text-center">
          <Info className="w-10 h-10 text-slate-600" />
          <p className="text-sm font-bold text-slate-300">
            Keine Einträge für deine Auswahl gefunden
          </p>
          <p className="text-xs text-slate-500 max-w-sm">
            Versuche einen anderen Suchbegriff oder setze den Hersteller-Filter auf &quot;Alle&quot;.
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filteredResults.map((item, idx) => {
            const imgSrc = item.localImageBase64 || item.imageUrl || item.thumbnailUrl;
            return (
              <div
                key={`${item.name}-${idx}`}
                onClick={() => onOpenDossier(item)}
                className="sc-glass border border-cyan-950/70 hover:border-cyan-500/50 rounded-xl p-4 transition-all duration-200 flex flex-col justify-between group cursor-pointer hover:shadow-xl hover:shadow-cyan-950/40 relative overflow-hidden"
              >
                {/* Glow bar on hover */}
                <div className="absolute top-0 left-0 right-0 h-0.5 bg-gradient-to-r from-transparent via-cyan-400 to-transparent opacity-0 group-hover:opacity-100 transition-opacity" />

                <div>
                  {/* Top card bar */}
                  <div className="flex items-center justify-between gap-2 mb-2 text-[10px] font-mono">
                    <span className="text-cyan-400 font-bold truncate">
                      {item.manufacturer || 'Star Citizen'}
                    </span>
                    {item.productionStatus && (
                      <span className={`px-1.5 py-0.5 rounded text-[9px] font-bold border ${
                        item.productionStatus.toLowerCase().includes('flight')
                          ? 'bg-emerald-950/80 border-emerald-500/40 text-emerald-300'
                          : 'bg-amber-950/80 border-amber-500/40 text-amber-300'
                      }`}>
                        {item.productionStatus}
                      </span>
                    )}
                  </div>

                  {/* Thumbnail Banner */}
                  <div className="w-full h-32 rounded-lg bg-[#020712] border border-cyan-950/80 overflow-hidden flex items-center justify-center p-2 mb-3 relative">
                    {imgSrc ? (
                      <img
                        src={imgSrc}
                        alt={item.name}
                        className="w-full h-full object-contain transition-transform duration-300 group-hover:scale-105"
                        onError={(e) => {
                          (e.target as HTMLElement).style.display = 'none';
                        }}
                      />
                    ) : (
                      <Rocket className="w-8 h-8 text-slate-700" />
                    )}
                  </div>

                  {/* Ship Title & Role */}
                  <h3 className="font-bold text-white text-sm font-mono tracking-wide group-hover:text-cyan-300 transition truncate">
                    {item.name}
                  </h3>
                  <div className="text-xs text-slate-400 font-mono mt-0.5 truncate">
                    {item.role || item.type || item.category}
                  </div>
                </div>

                {/* Bottom stats & CTA */}
                <div className="mt-3 pt-3 border-t border-cyan-950/60 flex items-center justify-between text-xs font-mono">
                  <div className="flex items-center space-x-3 text-slate-400 text-[11px]">
                    {item.cargoScu !== undefined && item.cargoScu !== null && item.cargoScu > 0 && (
                      <span className="flex items-center gap-1" title="Frachtkapazität">
                        <Box className="w-3 h-3 text-cyan-400" />
                        <span>{item.cargoScu} SCU</span>
                      </span>
                    )}
                    {item.crewMin !== undefined && item.crewMin !== null && (
                      <span className="flex items-center gap-1" title="Besatzung">
                        <Users className="w-3 h-3 text-cyan-400" />
                        <span>{item.crewMin} P.</span>
                      </span>
                    )}
                  </div>

                  <span className="text-[11px] font-bold text-cyan-400 group-hover:underline flex items-center gap-1">
                    <span>Dossier</span>
                    <span>→</span>
                  </span>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
