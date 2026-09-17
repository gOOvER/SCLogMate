import React, { useState, useEffect, useMemo } from 'react';
import {
  bridge,
  FleetShipDto,
  CatalogShipDto,
  ShipComparisonDataDto,
  ShipComparisonSideDto,
} from '../services/photinoBridge';
import { PipsAnalyzerBadge } from './PipsAnalyzerBadge';

interface ShipCompareModalProps {
  isOpen: boolean;
  onClose: () => void;
  initialShipA?: string;
  initialShipB?: string;
  fleetShips: FleetShipDto[];
  catalog: CatalogShipDto[];
  onOpenWikiDossier?: (shipName: string) => void;
}

export const ShipCompareModal: React.FC<ShipCompareModalProps> = ({
  isOpen,
  onClose,
  initialShipA,
  initialShipB,
  fleetShips,
  catalog,
  onOpenWikiDossier,
}) => {
  // Available ship options (unique by normalized name)
  const shipOptions = useMemo(() => {
    const map = new Map<string, { name: string; manufacturer: string; inHangar: boolean }>();
    fleetShips.forEach((s) => {
      map.set(s.name.toLowerCase(), {
        name: s.name,
        manufacturer: s.manufacturer,
        inHangar: s.isInHangar,
      });
    });
    catalog.forEach((c) => {
      if (!map.has(c.name.toLowerCase())) {
        map.set(c.name.toLowerCase(), {
          name: c.name,
          manufacturer: c.manufacturer,
          inHangar: false,
        });
      }
    });
    return Array.from(map.values()).sort((a, b) => {
      if (a.inHangar && !b.inHangar) return -1;
      if (!a.inHangar && b.inHangar) return 1;
      return a.name.localeCompare(b.name);
    });
  }, [fleetShips, catalog]);

  const defaultShipA = initialShipA || fleetShips[0]?.name || catalog[0]?.name || 'Cutlass Black';
  const defaultShipB =
    initialShipB ||
    fleetShips.find((s) => s.name.toLowerCase() !== defaultShipA.toLowerCase())?.name ||
    catalog.find((c) => c.name.toLowerCase() !== defaultShipA.toLowerCase())?.name ||
    'Freelancer';

  const [shipA, setShipA] = useState<string>(defaultShipA);
  const [shipB, setShipB] = useState<string>(defaultShipB);
  const [compData, setCompData] = useState<ShipComparisonDataDto | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(false);

  // Sync initial props
  useEffect(() => {
    if (initialShipA) setShipA(initialShipA);
    if (initialShipB) setShipB(initialShipB);
  }, [initialShipA, initialShipB]);

  // Fetch comparison data from backend
  useEffect(() => {
    if (!isOpen || !shipA || !shipB) return;

    let isMounted = true;
    setIsLoading(true);

    bridge
      .getShipComparisonData(shipA, shipB)
      .then((data) => {
        if (isMounted) {
          setCompData(data);
          setIsLoading(false);
        }
      })
      .catch((err) => {
        console.error('Failed to get ship comparison data:', err);
        if (isMounted) setIsLoading(false);
      });

    return () => {
      isMounted = false;
    };
  }, [isOpen, shipA, shipB]);

  if (!isOpen) return null;

  const handleSwap = () => {
    setShipA(shipB);
    setShipB(shipA);
  };

  const sideA = compData?.shipA;
  const sideB = compData?.shipB;

  // Comparison helper functions for highlighting advantage
  const getScuDiff = () => {
    if (!sideA || !sideB) return null;
    const diff = sideA.totalScu - sideB.totalScu;
    return diff;
  };

  const scuDiff = getScuDiff();

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/75 backdrop-blur-md animate-fade-in">
      <div className="relative w-full max-w-5xl max-h-[92vh] flex flex-col bg-slate-900/95 border border-slate-700/80 rounded-2xl shadow-2xl overflow-hidden">
        {/* Header Bar */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-800 bg-slate-900/90 backdrop-blur shrink-0">
          <div className="flex items-center gap-3">
            <span className="text-2xl">⚖️</span>
            <div>
              <h2 className="text-lg font-bold text-white tracking-wide">
                Schiffs- & Flotten-Vergleich
              </h2>
              <p className="text-xs text-slate-400">
                Direkte Gegenüberstellung von Spezifikationen, Ballistik, Frachtraum & Händlerpreisen
              </p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            <button
              onClick={handleSwap}
              className="px-3 py-1.5 rounded-lg border border-slate-700 hover:border-cyan-500/50 bg-slate-800/80 text-xs text-cyan-300 font-medium flex items-center gap-1.5 transition"
              title="Seiten vertauschen"
            >
              <span>⇄</span>
              <span>Tauschen</span>
            </button>
            <button
              onClick={onClose}
              className="w-8 h-8 rounded-lg flex items-center justify-center bg-slate-800/80 hover:bg-rose-500/20 text-slate-400 hover:text-rose-300 border border-slate-700 transition"
              title="Schließen"
            >
              ✕
            </button>
          </div>
        </div>

        {/* Ship Selectors Bar */}
        <div className="grid grid-cols-2 gap-4 px-6 py-3 bg-slate-950/60 border-b border-slate-800/80 shrink-0">
          {/* Selector A */}
          <div className="flex items-center gap-2">
            <span className="text-xs font-mono text-cyan-400 font-bold px-2 py-0.5 rounded bg-cyan-500/10 border border-cyan-500/30">
              SCHIFF A
            </span>
            <select
              value={shipA}
              onChange={(e) => setShipA(e.target.value)}
              className="flex-1 bg-slate-800/90 border border-slate-700 text-slate-100 text-sm rounded-lg px-3 py-1.5 focus:outline-none focus:border-cyan-500 font-medium"
            >
              {shipOptions.map((opt) => (
                <option key={`a-${opt.name}`} value={opt.name}>
                  {opt.inHangar ? '★ ' : ''}
                  {opt.name} ({opt.manufacturer})
                </option>
              ))}
            </select>
          </div>

          {/* Selector B */}
          <div className="flex items-center gap-2">
            <span className="text-xs font-mono text-indigo-400 font-bold px-2 py-0.5 rounded bg-indigo-500/10 border border-indigo-500/30">
              SCHIFF B
            </span>
            <select
              value={shipB}
              onChange={(e) => setShipB(e.target.value)}
              className="flex-1 bg-slate-800/90 border border-slate-700 text-slate-100 text-sm rounded-lg px-3 py-1.5 focus:outline-none focus:border-indigo-500 font-medium"
            >
              {shipOptions.map((opt) => (
                <option key={`b-${opt.name}`} value={opt.name}>
                  {opt.inHangar ? '★ ' : ''}
                  {opt.name} ({opt.manufacturer})
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Modal Scrollable Content */}
        <div className="flex-1 overflow-y-auto px-6 py-4 space-y-6 custom-scrollbar">
          {isLoading ? (
            <div className="flex flex-col items-center justify-center py-24 text-slate-400 space-y-3">
              <div className="w-8 h-8 border-2 border-cyan-400 border-t-transparent rounded-full animate-spin" />
              <span className="text-sm">Lade Schiffsspezifikationen & Händlerdaten...</span>
            </div>
          ) : !sideA || !sideB ? (
            <div className="text-center py-16 text-slate-400">
              Keine ausreichenden Vergleichsdaten verfügbar.
            </div>
          ) : (
            <>
              {/* Ship Profile Cards (Top Hero) */}
              <div className="grid grid-cols-2 gap-4">
                <ShipHeroCard
                  side={sideA}
                  accentColor="cyan"
                  onOpenWiki={() => onOpenWikiDossier?.(sideA.name)}
                />
                <ShipHeroCard
                  side={sideB}
                  accentColor="indigo"
                  onOpenWiki={() => onOpenWikiDossier?.(sideB.name)}
                />
              </div>

              {/* Section 1: Waffen-Ballistik & Lead-Pips */}
              <ComparisonSection title="🎯 Bewaffnung & Lead-Pips Vorhaltepunkte">
                <div className="grid grid-cols-2 gap-4">
                  <div className="p-3.5 rounded-xl bg-slate-800/40 border border-slate-700/50 space-y-2">
                    <div className="flex items-center justify-between">
                      <span className="text-xs text-slate-400">Pips-Synchronisation:</span>
                      <PipsAnalyzerBadge pipsResult={sideA.pipsResult} />
                    </div>
                    {sideA.pipsResult?.advice && (
                      <p className="text-[11px] text-slate-300 italic leading-relaxed">
                        {sideA.pipsResult.advice}
                      </p>
                    )}
                  </div>
                  <div className="p-3.5 rounded-xl bg-slate-800/40 border border-slate-700/50 space-y-2">
                    <div className="flex items-center justify-between">
                      <span className="text-xs text-slate-400">Pips-Synchronisation:</span>
                      <PipsAnalyzerBadge pipsResult={sideB.pipsResult} />
                    </div>
                    {sideB.pipsResult?.advice && (
                      <p className="text-[11px] text-slate-300 italic leading-relaxed">
                        {sideB.pipsResult.advice}
                      </p>
                    )}
                  </div>
                </div>
              </ComparisonSection>

              {/* Section 2: Fracht, Pads & Zugang */}
              <ComparisonSection title="📦 Frachtkapazität & Landebedingungen">
                <div className="space-y-2">
                  <ComparisonRow
                    label="Frachtvolumen"
                    valA={`${sideA.totalScu} SCU`}
                    valB={`${sideB.totalScu} SCU`}
                    winner={scuDiff ? (scuDiff > 0 ? 'A' : 'B') : undefined}
                    deltaText={
                      scuDiff !== null && scuDiff !== 0
                        ? `${Math.abs(scuDiff)} SCU ${scuDiff > 0 ? 'Mehr bei A' : 'Mehr bei B'}`
                        : undefined
                    }
                  />
                  <ComparisonRow
                    label="Größter Container"
                    valA={sideA.maxContainerScu > 0 ? `${sideA.maxContainerScu} SCU Box` : '—'}
                    valB={sideB.maxContainerScu > 0 ? `${sideB.maxContainerScu} SCU Box` : '—'}
                  />
                  <ComparisonRow
                    label="Pad-Größe"
                    valA={sideA.padSize}
                    valB={sideB.padSize}
                    notice="Kleinere Pads ermöglichen Landung auf Außenposten."
                  />
                  <ComparisonRow
                    label="Fracht-Zugang"
                    valA={sideA.cargoAccessType}
                    valB={sideB.cargoAccessType}
                  />
                  <ComparisonRow
                    label="Planeten-Landung"
                    valA={sideA.canLandPlanetside ? 'Ja ✓' : 'Nein (Nur Raum)'}
                    valB={sideB.canLandPlanetside ? 'Ja ✓' : 'Nein (Nur Raum)'}
                  />
                  <ComparisonRow
                    label="Docking-Collar nötig"
                    valA={sideA.requiresDockingCollar ? 'Ja (Stationsturm)' : 'Nein (Landepad)'}
                    valB={sideB.requiresDockingCollar ? 'Ja (Stationsturm)' : 'Nein (Landepad)'}
                  />
                </div>
              </ComparisonSection>

              {/* Section 3: Abmessungen & Besatzung */}
              <ComparisonSection title="📐 Dimensionen & Besatzung">
                <div className="space-y-2">
                  <ComparisonRow
                    label="Länge × Breite × Höhe"
                    valA={
                      sideA.length
                        ? `${sideA.length}m × ${sideA.beam || '—'}m × ${sideA.height || '—'}m`
                        : '—'
                    }
                    valB={
                      sideB.length
                        ? `${sideB.length}m × ${sideB.beam || '—'}m × ${sideB.height || '—'}m`
                        : '—'
                    }
                  />
                  <ComparisonRow
                    label="Masse"
                    valA={sideA.mass ? `${(sideA.mass / 1000).toLocaleString()} t` : '—'}
                    valB={sideB.mass ? `${(sideB.mass / 1000).toLocaleString()} t` : '—'}
                  />
                  <ComparisonRow
                    label="Besatzung (Min / Max)"
                    valA={sideA.crewMin ? `${sideA.crewMin} – ${sideA.crewMax || sideA.crewMin}` : '—'}
                    valB={sideB.crewMin ? `${sideB.crewMin} – ${sideB.crewMax || sideB.crewMin}` : '—'}
                  />
                  <ComparisonRow
                    label="Quantum-Treibstoff"
                    valA={sideA.quantumFuel ? `${sideA.quantumFuel.toLocaleString()} L` : '—'}
                    valB={sideB.quantumFuel ? `${sideB.quantumFuel.toLocaleString()} L` : '—'}
                  />
                </div>
              </ComparisonSection>

              {/* Section 4: Wirtschaft & Händlerverzeichnis */}
              <ComparisonSection title="🛒 Händlerverzeichnis & Kaufpreise im Verse">
                <div className="grid grid-cols-2 gap-4">
                  {/* Stores A */}
                  <div className="p-3.5 rounded-xl bg-slate-800/40 border border-slate-700/50 space-y-2">
                    <div className="flex items-center justify-between text-xs pb-1 border-b border-slate-700/50">
                      <span className="text-slate-400">Kaufpreis Schätzung:</span>
                      <span className="font-mono font-bold text-amber-300">
                        {sideA.estimatedValueAuec > 0
                          ? `${sideA.estimatedValueAuec.toLocaleString()} aUEC`
                          : '—'}
                      </span>
                    </div>

                    <div className="space-y-1.5 pt-1">
                      <div className="text-[11px] font-semibold text-slate-300">
                        Verfügbare Stationen ({sideA.storeLocations.length}):
                      </div>
                      {sideA.storeLocations.length === 0 ? (
                        <div className="text-xs text-slate-500 italic">
                          Keine In-Game Händlerdaten hinterlegt.
                        </div>
                      ) : (
                        sideA.storeLocations.map((loc, idx) => (
                          <div
                            key={idx}
                            className="p-2 rounded-lg bg-slate-900/60 border border-slate-800 flex items-center justify-between text-[11px]"
                          >
                            <div>
                              <div className="font-medium text-slate-200">{loc.storeName}</div>
                              <div className="text-[10px] text-slate-400">{loc.location}</div>
                            </div>
                            <div className="text-right font-mono">
                              <div className="text-amber-400 font-bold">
                                {loc.priceAuec.toLocaleString()} aUEC
                              </div>
                              {loc.rentPrice1dAuec && (
                                <div className="text-[10px] text-slate-400">
                                  Miete: {loc.rentPrice1dAuec.toLocaleString()}/Tag
                                </div>
                              )}
                            </div>
                          </div>
                        ))
                      )}
                    </div>
                  </div>

                  {/* Stores B */}
                  <div className="p-3.5 rounded-xl bg-slate-800/40 border border-slate-700/50 space-y-2">
                    <div className="flex items-center justify-between text-xs pb-1 border-b border-slate-700/50">
                      <span className="text-slate-400">Kaufpreis Schätzung:</span>
                      <span className="font-mono font-bold text-amber-300">
                        {sideB.estimatedValueAuec > 0
                          ? `${sideB.estimatedValueAuec.toLocaleString()} aUEC`
                          : '—'}
                      </span>
                    </div>

                    <div className="space-y-1.5 pt-1">
                      <div className="text-[11px] font-semibold text-slate-300">
                        Verfügbare Stationen ({sideB.storeLocations.length}):
                      </div>
                      {sideB.storeLocations.length === 0 ? (
                        <div className="text-xs text-slate-500 italic">
                          Keine In-Game Händlerdaten hinterlegt.
                        </div>
                      ) : (
                        sideB.storeLocations.map((loc, idx) => (
                          <div
                            key={idx}
                            className="p-2 rounded-lg bg-slate-900/60 border border-slate-800 flex items-center justify-between text-[11px]"
                          >
                            <div>
                              <div className="font-medium text-slate-200">{loc.storeName}</div>
                              <div className="text-[10px] text-slate-400">{loc.location}</div>
                            </div>
                            <div className="text-right font-mono">
                              <div className="text-amber-400 font-bold">
                                {loc.priceAuec.toLocaleString()} aUEC
                              </div>
                              {loc.rentPrice1dAuec && (
                                <div className="text-[10px] text-slate-400">
                                  Miete: {loc.rentPrice1dAuec.toLocaleString()}/Tag
                                </div>
                              )}
                            </div>
                          </div>
                        ))
                      )}
                    </div>
                  </div>
                </div>
              </ComparisonSection>

              {/* Section 5: Persönliche Flugstatistik */}
              <ComparisonSection title="📊 Deine Flug- & Einsatzstatistik">
                <div className="space-y-2">
                  <ComparisonRow
                    label="Absolvierte Flüge"
                    valA={`${sideA.flightCount} Einsätze`}
                    valB={`${sideB.flightCount} Einsätze`}
                    winner={
                      sideA.flightCount !== sideB.flightCount
                        ? sideA.flightCount > sideB.flightCount
                          ? 'A'
                          : 'B'
                        : undefined
                    }
                  />
                  <ComparisonRow
                    label="Quantum-Sprünge"
                    valA={`${sideA.quantumJumps}`}
                    valB={`${sideB.quantumJumps}`}
                  />
                  <ComparisonRow
                    label="Schiffsverluste (Losses)"
                    valA={`${sideA.lossCount}`}
                    valB={`${sideB.lossCount}`}
                  />
                  <ComparisonRow
                    label="Zuletzt geflogen"
                    valA={sideA.lastFlown}
                    valB={sideB.lastFlown}
                  />
                </div>
              </ComparisonSection>
            </>
          )}
        </div>
      </div>
    </div>
  );
};

interface ShipHeroCardProps {
  side: ShipComparisonSideDto;
  accentColor: 'cyan' | 'indigo';
  onOpenWiki?: () => void;
}

const ShipHeroCard: React.FC<ShipHeroCardProps> = ({ side, accentColor, onOpenWiki }) => {
  const isCyan = accentColor === 'cyan';
  const borderClass = isCyan ? 'border-cyan-500/40' : 'border-indigo-500/40';
  const tagBg = isCyan ? 'bg-cyan-500/15 text-cyan-300' : 'bg-indigo-500/15 text-indigo-300';

  return (
    <div
      className={`p-4 rounded-xl bg-gradient-to-b from-slate-800/80 to-slate-900/90 border ${borderClass} shadow-lg relative overflow-hidden flex flex-col justify-between`}
    >
      <div>
        <div className="flex items-start justify-between gap-2 mb-2">
          <div>
            <div className="flex items-center gap-2">
              <span className={`text-[10px] font-mono font-bold px-1.5 py-0.5 rounded ${tagBg}`}>
                {side.manufacturerBadge || side.manufacturer}
              </span>
              {side.isInHangar && (
                <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-emerald-500/15 border border-emerald-500/40 text-emerald-300">
                  ★ Im Hangar
                </span>
              )}
            </div>
            <h3 className="text-lg font-bold text-white mt-1 leading-snug">{side.name}</h3>
            <p className="text-xs text-slate-400">{side.role}</p>
          </div>

          <button
            onClick={onOpenWiki}
            className="px-2.5 py-1 rounded-md text-[11px] bg-slate-800 hover:bg-slate-700 border border-slate-700 text-slate-300 hover:text-white transition shrink-0"
            title="Dossier öffnen"
          >
            📖 Dossier
          </button>
        </div>

        <div className="grid grid-cols-2 gap-2 mt-3 pt-3 border-t border-slate-800/80 text-xs">
          <div>
            <span className="text-[10px] text-slate-400 block">Pledge / Versicherung:</span>
            <span className="font-mono text-slate-200">
              {side.pledgeValueUsd > 0 ? `$${side.pledgeValueUsd}` : '—'} · {side.defaultInsurance}
            </span>
          </div>
          <div>
            <span className="text-[10px] text-slate-400 block">Status:</span>
            <span className="font-mono text-slate-200">{side.acquisitionType}</span>
          </div>
        </div>
      </div>
    </div>
  );
};

interface ComparisonSectionProps {
  title: string;
  children: React.ReactNode;
}

const ComparisonSection: React.FC<ComparisonSectionProps> = ({ title, children }) => (
  <div className="space-y-2">
    <h4 className="text-xs font-bold uppercase tracking-wider text-slate-400 flex items-center gap-2">
      <span>{title}</span>
      <div className="flex-1 h-px bg-slate-800" />
    </h4>
    {children}
  </div>
);

interface ComparisonRowProps {
  label: string;
  valA: React.ReactNode;
  valB: React.ReactNode;
  winner?: 'A' | 'B';
  deltaText?: string;
  notice?: string;
}

const ComparisonRow: React.FC<ComparisonRowProps> = ({
  label,
  valA,
  valB,
  winner,
  deltaText,
  notice,
}) => {
  return (
    <div className="grid grid-cols-12 gap-2 p-2.5 rounded-lg bg-slate-800/40 border border-slate-800/80 hover:bg-slate-800/60 transition items-center text-xs">
      <div className="col-span-4">
        <div className="font-medium text-slate-300">{label}</div>
        {notice && <div className="text-[10px] text-slate-500 italic">{notice}</div>}
      </div>

      <div
        className={`col-span-4 font-mono font-medium text-center ${
          winner === 'A'
            ? 'text-emerald-400 font-bold bg-emerald-500/10 py-1 rounded border border-emerald-500/30'
            : 'text-slate-200'
        }`}
      >
        {valA}
      </div>

      <div
        className={`col-span-4 font-mono font-medium text-center ${
          winner === 'B'
            ? 'text-emerald-400 font-bold bg-emerald-500/10 py-1 rounded border border-emerald-500/30'
            : 'text-slate-200'
        }`}
      >
        {valB}
      </div>

      {deltaText && (
        <div className="col-span-12 text-center text-[10px] font-mono text-cyan-400/80 pt-0.5">
          {deltaText}
        </div>
      )}
    </div>
  );
};
