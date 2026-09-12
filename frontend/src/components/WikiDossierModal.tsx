import React, { useState, useEffect } from 'react';
import {
  Rocket,
  Box,
  Users,
  Fuel,
  Ruler,
  DollarSign,
  ExternalLink,
  X,
  Languages,
  Store,
  Loader2,
} from 'lucide-react';
import { WikiInfo, bridge } from '../services/photinoBridge';

interface WikiDossierModalProps {
  isOpen: boolean;
  item: WikiInfo | null;
  query?: string | null;
  onClose: () => void;
}

export const WikiDossierModal: React.FC<WikiDossierModalProps> = ({
  isOpen,
  item: initialItem,
  query,
  onClose,
}) => {
  const [data, setData] = useState<WikiInfo | null>(initialItem);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [lang, setLang] = useState<'de' | 'en'>('de');
  const [activeTab, setActiveTab] = useState<'overview' | 'specs' | 'stores'>('overview');

  useEffect(() => {
    if (!isOpen) return;

    if (initialItem) {
      setData(initialItem);
      setIsLoading(false);
      return;
    }

    if (query) {
      setIsLoading(true);
      setData(null);
      bridge.lookupWiki(query)
        .then((res) => {
          setData(res);
        })
        .catch((err) => {
          console.error('Wiki lookup failed:', err);
        })
        .finally(() => {
          setIsLoading(false);
        });
    }
  }, [isOpen, initialItem, query]);

  if (!isOpen) return null;

  const handleOpenWikiWeb = () => {
    if (data?.webUrl) {
      bridge.openExternalUrl(data.webUrl);
    } else if (data?.name || query) {
      bridge.openExternalUrl(`https://star-citizen.wiki/${encodeURIComponent(data?.name || query || '')}`);
    }
  };

  const handleOpenPledge = () => {
    if (data?.pledgeUrl) {
      bridge.openExternalUrl(data.pledgeUrl);
    }
  };

  const imageSrc = data?.localImageBase64 || data?.imageUrl || data?.thumbnailUrl;
  const descriptionText = lang === 'de'
    ? (data?.descriptionDe || data?.descriptionEn || 'Keine Beschreibung verfügbar.')
    : (data?.descriptionEn || data?.descriptionDe || 'No description available.');

  return (
    <div className="fixed inset-0 z-[130] flex items-center justify-center bg-black/85 backdrop-blur-md p-3 sm:p-5 animate-in fade-in duration-200">
      <div className="sc-glass rounded-2xl w-full max-w-3xl border border-cyan-500/40 shadow-2xl shadow-cyan-950/70 p-5 sm:p-6 relative overflow-hidden flex flex-col max-h-[92vh]">
        {/* Glowing Top Accent */}
        <div className="absolute top-0 left-0 right-0 h-1 bg-gradient-to-r from-transparent via-cyan-400 to-transparent" />

        {/* Modal Top Bar */}
        <div className="flex items-start justify-between gap-4 mb-4 shrink-0">
          <div className="flex items-center space-x-3">
            <div className="w-11 h-11 rounded-xl bg-cyan-500/10 border border-cyan-500/40 flex items-center justify-center text-cyan-400 shadow-[0_0_20px_rgba(6,182,212,0.3)] shrink-0">
              <Rocket className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <span className="text-[11px] font-mono font-bold tracking-widest text-cyan-400 uppercase">
                  STAR CITIZEN WIKI DOSSIER
                </span>
                {data?.category && (
                  <span className="px-2 py-0.5 rounded text-[10px] font-mono font-bold bg-slate-800/80 border border-slate-700 text-slate-300">
                    {data.category}
                  </span>
                )}
                {data?.productionStatus && (
                  <span className={`px-2 py-0.5 rounded text-[10px] font-mono font-bold border ${
                    data.productionStatus.toLowerCase().includes('flight')
                      ? 'bg-emerald-950/80 border-emerald-500/50 text-emerald-300'
                      : 'bg-amber-950/80 border-amber-500/50 text-amber-300'
                  }`}>
                    {data.productionStatus}
                  </span>
                )}
              </div>
              <h2 className="text-lg sm:text-xl font-bold text-white tracking-wide font-mono mt-0.5 flex items-center gap-2">
                <span>{data?.name || query || 'Schiff / Item'}</span>
                {data?.manufacturer && (
                  <span className="text-slate-400 text-xs font-normal">
                    · {data.manufacturer}
                  </span>
                )}
              </h2>
            </div>
          </div>

          <div className="flex items-center space-x-1.5">
            {/* Language Toggle */}
            <button
              onClick={() => setLang(lang === 'de' ? 'en' : 'de')}
              className="flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-xs font-mono font-bold border border-cyan-500/30 bg-cyan-950/40 hover:bg-cyan-900/50 text-cyan-300 transition cursor-pointer"
              title="Sprache umschalten"
            >
              <Languages className="w-3.5 h-3.5" />
              <span>{lang.toUpperCase()}</span>
            </button>

            {/* Close Button */}
            <button
              onClick={onClose}
              className="text-slate-400 hover:text-white p-1.5 rounded-lg hover:bg-slate-800/60 transition cursor-pointer"
              title="Schließen"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Loading State */}
        {isLoading ? (
          <div className="flex-1 flex flex-col items-center justify-center py-20 space-y-3 font-mono text-cyan-400">
            <Loader2 className="w-8 h-8 animate-spin text-cyan-400" />
            <p className="text-xs uppercase tracking-wider text-slate-400">
              Lade Star Citizen Wiki Dossier für &quot;{query}&quot;...
            </p>
          </div>
        ) : !data ? (
          <div className="flex-1 flex flex-col items-center justify-center py-16 space-y-4 font-mono text-center">
            <Rocket className="w-12 h-12 text-slate-600" />
            <div>
              <p className="text-sm font-bold text-slate-300">
                Kein Wiki-Eintrag für &quot;{query}&quot; gefunden
              </p>
              <p className="text-xs text-slate-500 mt-1">
                Überprüfe die Schreibweise oder öffne das Wiki direkt im Browser.
              </p>
            </div>
            <button
              onClick={handleOpenWikiWeb}
              className="px-4 py-2 rounded-xl text-xs font-mono font-bold bg-cyan-600 hover:bg-cyan-500 text-white transition flex items-center gap-2 cursor-pointer shadow-lg shadow-cyan-950/60"
            >
              <ExternalLink className="w-3.5 h-3.5" />
              <span>Im Web-Wiki suchen</span>
            </button>
          </div>
        ) : (
          /* Modal Content */
          <div className="flex-1 overflow-y-auto pr-1 space-y-4 select-text">
            {/* Hero Render Banner */}
            {imageSrc && (
              <div className="relative w-full h-48 sm:h-64 rounded-xl overflow-hidden border border-cyan-950/80 bg-gradient-to-b from-[#061224] to-[#020712] flex items-center justify-center group">
                <img
                  src={imageSrc}
                  alt={data.name}
                  className="w-full h-full object-contain p-2 transition-transform duration-300 group-hover:scale-105"
                  onError={(e) => {
                    (e.target as HTMLElement).style.display = 'none';
                  }}
                />
                <div className="absolute inset-0 bg-gradient-to-t from-[#020712] via-transparent to-transparent opacity-80 pointer-events-none" />
                <div className="absolute bottom-3 left-3 right-3 flex items-center justify-between text-xs font-mono text-slate-300 pointer-events-none">
                  <div className="flex items-center gap-2">
                    {data.role && (
                      <span className="px-2 py-0.5 rounded bg-black/60 border border-slate-700 text-cyan-300">
                        {data.role}
                      </span>
                    )}
                    {data.type && (
                      <span className="px-2 py-0.5 rounded bg-black/60 border border-slate-700 text-slate-300">
                        {data.type}
                      </span>
                    )}
                  </div>
                  {data.msrp && (
                    <span className="px-2.5 py-0.5 rounded font-bold bg-emerald-950/80 border border-emerald-500/50 text-emerald-300">
                      ${data.msrp} MSRP
                    </span>
                  )}
                </div>
              </div>
            )}

            {/* Quick KPI Bar */}
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 text-xs font-mono">
              <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-xl p-2.5 flex items-center space-x-2.5">
                <Box className="w-4 h-4 text-cyan-400 shrink-0" />
                <div>
                  <div className="text-[10px] text-slate-500 uppercase">Fracht</div>
                  <div className="font-bold text-white">
                    {data.cargoScu !== undefined && data.cargoScu !== null ? `${data.cargoScu} SCU` : '—'}
                  </div>
                </div>
              </div>

              <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-xl p-2.5 flex items-center space-x-2.5">
                <Users className="w-4 h-4 text-cyan-400 shrink-0" />
                <div>
                  <div className="text-[10px] text-slate-500 uppercase">Besatzung</div>
                  <div className="font-bold text-white">
                    {data.crewMin || data.crewMax
                      ? `${data.crewMin ?? 1} - ${data.crewMax ?? data.crewMin ?? 1}`
                      : '1 Person'}
                  </div>
                </div>
              </div>

              <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-xl p-2.5 flex items-center space-x-2.5">
                <Fuel className="w-4 h-4 text-cyan-400 shrink-0" />
                <div>
                  <div className="text-[10px] text-slate-500 uppercase">Quantum Tank</div>
                  <div className="font-bold text-white">
                    {data.quantumFuel ? `${data.quantumFuel.toLocaleString()} l` : '—'}
                  </div>
                </div>
              </div>

              <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-xl p-2.5 flex items-center space-x-2.5">
                <Ruler className="w-4 h-4 text-cyan-400 shrink-0" />
                <div>
                  <div className="text-[10px] text-slate-500 uppercase">Länge × Breite</div>
                  <div className="font-bold text-white truncate">
                    {data.length && data.beam ? `${data.length.toFixed(1)}m × ${data.beam.toFixed(1)}m` : '—'}
                  </div>
                </div>
              </div>
            </div>

            {/* Navigation Tabs */}
            <div className="flex border-b border-cyan-950 gap-2">
              <button
                onClick={() => setActiveTab('overview')}
                className={`pb-2 px-3 text-xs font-mono font-bold transition border-b-2 cursor-pointer ${
                  activeTab === 'overview'
                    ? 'border-cyan-400 text-cyan-300'
                    : 'border-transparent text-slate-400 hover:text-slate-200'
                }`}
              >
                Übersicht & Lore
              </button>
              <button
                onClick={() => setActiveTab('specs')}
                className={`pb-2 px-3 text-xs font-mono font-bold transition border-b-2 cursor-pointer ${
                  activeTab === 'specs'
                    ? 'border-cyan-400 text-cyan-300'
                    : 'border-transparent text-slate-400 hover:text-slate-200'
                }`}
              >
                Spezifikationen ({Object.keys(data.specs || {}).length})
              </button>
              <button
                onClick={() => setActiveTab('stores')}
                className={`pb-2 px-3 text-xs font-mono font-bold transition border-b-2 cursor-pointer ${
                  activeTab === 'stores'
                    ? 'border-cyan-400 text-cyan-300'
                    : 'border-transparent text-slate-400 hover:text-slate-200'
                }`}
              >
                Kauf- & Mietorte ({data.storeLocations?.length || 0})
              </button>
            </div>

            {/* Tab: Overview */}
            {activeTab === 'overview' && (
              <div className="space-y-3 text-xs font-mono">
                <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-xl p-3.5">
                  <div className="text-[10px] font-bold uppercase tracking-wider text-slate-400 mb-2 flex items-center justify-between">
                    <span>{lang === 'de' ? '📖 BESCHREIBUNG' : '📖 DESCRIPTION'}</span>
                    <span className="text-[10px] text-cyan-400">
                      {lang === 'de' ? 'Offizielle Deutsche Übersetzung' : 'English Original Lore'}
                    </span>
                  </div>
                  <p className="text-slate-300 leading-relaxed whitespace-pre-line text-[11px] sm:text-xs">
                    {descriptionText}
                  </p>
                </div>

                {data.focus && (
                  <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-xl p-3 flex items-center justify-between">
                    <span className="text-slate-400">Einsatzfokus:</span>
                    <span className="font-bold text-cyan-300">{data.focus}</span>
                  </div>
                )}
              </div>
            )}

            {/* Tab: Specs */}
            {activeTab === 'specs' && (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 text-xs font-mono">
                {Object.entries(data.specs || {}).map(([key, val]) => (
                  <div
                    key={key}
                    className="bg-[#051122]/90 border border-cyan-950/80 rounded-xl p-2.5 flex items-center justify-between"
                  >
                    <span className="text-slate-400">{key}:</span>
                    <span className="font-bold text-white text-right ml-2">{val}</span>
                  </div>
                ))}
                {Object.keys(data.specs || {}).length === 0 && (
                  <div className="col-span-2 py-8 text-center text-slate-500">
                    Keine erweiterten Spezifikationen in der Datenbank hinterlegt.
                  </div>
                )}
              </div>
            )}

            {/* Tab: Stores */}
            {activeTab === 'stores' && (
              <div className="space-y-2 text-xs font-mono">
                {data.storeLocations && data.storeLocations.length > 0 ? (
                  data.storeLocations.map((loc, idx) => (
                    <div
                      key={idx}
                      className="bg-[#051122]/90 border border-cyan-950/80 rounded-xl p-3 flex items-center justify-between"
                    >
                      <div className="flex items-center space-x-3">
                        <Store className="w-5 h-5 text-cyan-400" />
                        <div>
                          <div className="font-bold text-white">{loc.storeName}</div>
                          <div className="text-[10px] text-slate-400">{loc.location}</div>
                        </div>
                      </div>
                      <div className="text-right">
                        <div className="font-bold text-emerald-400">
                          {loc.priceAuec.toLocaleString()} aUEC
                        </div>
                        {loc.rentPrice1dAuec && (
                          <div className="text-[10px] text-slate-400">
                            Miete: {loc.rentPrice1dAuec.toLocaleString()} aUEC / Tag
                          </div>
                        )}
                      </div>
                    </div>
                  ))
                ) : (
                  <div className="py-8 text-center text-slate-500">
                    Keine Händlerdaten im Verse erfasst oder exklusives Pledge-Fahrzeug.
                  </div>
                )}
              </div>
            )}
          </div>
        )}

        {/* Modal Footer Actions */}
        <div className="mt-4 pt-3 border-t border-cyan-950/80 flex flex-wrap items-center justify-between gap-3 shrink-0">
          <div className="flex items-center space-x-2">
            <button
              onClick={handleOpenWikiWeb}
              className="px-3 py-1.5 rounded-xl text-xs font-mono font-bold bg-cyan-950/80 border border-cyan-500/40 hover:bg-cyan-900/60 text-cyan-300 transition flex items-center gap-1.5 cursor-pointer"
              title="Vollständigen Eintrag auf star-citizen.wiki öffnen"
            >
              <ExternalLink className="w-3.5 h-3.5" />
              <span>star-citizen.wiki</span>
            </button>

            {data?.pledgeUrl && (
              <button
                onClick={handleOpenPledge}
                className="px-3 py-1.5 rounded-xl text-xs font-mono font-bold bg-amber-950/80 border border-amber-500/40 hover:bg-amber-900/60 text-amber-300 transition flex items-center gap-1.5 cursor-pointer"
                title="Im RSI Pledge Store öffnen"
              >
                <DollarSign className="w-3.5 h-3.5" />
                <span>Pledge Store</span>
              </button>
            )}
          </div>

          <button
            onClick={onClose}
            className="px-4 py-1.5 rounded-xl text-xs font-mono font-bold bg-slate-800/80 hover:bg-slate-700 text-slate-200 transition cursor-pointer border border-slate-700"
          >
            Schließen
          </button>
        </div>
      </div>
    </div>
  );
};
