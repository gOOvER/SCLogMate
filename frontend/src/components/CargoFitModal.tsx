import React, { useState, useEffect } from 'react';
import {
  bridge,
  CargoFitResultDto,
} from '../services/photinoBridge';
import {
  X,
  Package,
  CheckCircle,
  AlertTriangle,
  Layers,
  Box,
  Plus,
  Minus,
  RotateCcw,
  Sparkles,
} from 'lucide-react';

interface CargoFitModalProps {
  isOpen: boolean;
  onClose: () => void;
  initialShipName?: string;
  initialCrates?: Record<number, number>;
}

const SHIPS_LIST = [
  'Crusader C2 Hercules',
  'Crusader M2 Hercules',
  'Drake Caterpillar',
  'Anvil Carrack',
  'RSI Constellation Taurus',
  'RSI Constellation Andromeda',
  'MISC Freelancer MAX',
  'MISC Freelancer',
  'Crusader C1 Spirit',
  'Drake Corsair',
  'Drake Cutlass Black',
  'RSI Zeus Mk II CL',
  'ARGO RAFT',
  'MISC Hull A',
  'Crusader Mercury Star Runner',
  'Drake Vulture',
  'Consolidated Outland Nomad',
  'Aegis Avenger Titan',
];

const CRATE_SIZES = [32, 24, 16, 8, 4, 2, 1];

const CRATE_COLORS: Record<number, { bg: string; text: string; border: string }> = {
  32: { bg: 'bg-indigo-600/30', text: 'text-indigo-300', border: 'border-indigo-500/50' },
  24: { bg: 'bg-violet-600/30', text: 'text-violet-300', border: 'border-violet-500/50' },
  16: { bg: 'bg-cyan-600/30', text: 'text-cyan-300', border: 'border-cyan-500/50' },
  8:  { bg: 'bg-emerald-600/30', text: 'text-emerald-300', border: 'border-emerald-500/50' },
  4:  { bg: 'bg-amber-600/30', text: 'text-amber-300', border: 'border-amber-500/50' },
  2:  { bg: 'bg-orange-600/30', text: 'text-orange-300', border: 'border-orange-500/50' },
  1:  { bg: 'bg-slate-700/40', text: 'text-slate-300', border: 'border-slate-600' },
};

export const CargoFitModal: React.FC<CargoFitModalProps> = ({
  isOpen,
  onClose,
  initialShipName = 'Crusader C2 Hercules',
  initialCrates = { 32: 2, 16: 2 },
}) => {
  const [shipName, setShipName] = useState<string>(initialShipName);
  const [crates, setCrates] = useState<Record<number, number>>(initialCrates);
  const [fitResult, setFitResult] = useState<CargoFitResultDto | null>(null);
  const [isCalculating, setIsCalculating] = useState(false);

  useEffect(() => {
    if (initialShipName) setShipName(initialShipName);
  }, [initialShipName]);

  const runCalculation = async () => {
    setIsCalculating(true);
    try {
      const res = await bridge.calculateCargoFit(shipName, crates);
      setFitResult(res);
    } catch (err) {
      console.error('Failed to calculate cargo fit:', err);
    } finally {
      setIsCalculating(false);
    }
  };

  useEffect(() => {
    if (isOpen) {
      runCalculation();
    }
  }, [isOpen, shipName, crates]);

  if (!isOpen) return null;

  const totalRequestedScu = Object.entries(crates).reduce(
    (sum, [scu, count]) => sum + Number(scu) * count,
    0
  );

  const updateCount = (scu: number, delta: number) => {
    setCrates((prev) => {
      const cur = prev[scu] || 0;
      const next = Math.max(0, cur + delta);
      const copy = { ...prev };
      if (next === 0) delete copy[scu];
      else copy[scu] = next;
      return copy;
    });
  };

  const applyPreset = (preset: Record<number, number>) => {
    setCrates(preset);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm animate-in fade-in">
      <div className="bg-slate-950 border border-cyan-500/40 rounded-xl shadow-2xl w-full max-w-5xl max-h-[92vh] flex flex-col overflow-hidden sc-hud-corner">
        {/* Header */}
        <div className="p-4 border-b border-slate-800 flex items-center justify-between bg-slate-900/60">
          <div className="flex items-center gap-2.5">
            <div className="p-2 rounded-lg bg-cyan-500/10 border border-cyan-500/30">
              <Package className="w-5 h-5 text-cyan-400" />
            </div>
            <div>
              <h2 className="text-base font-bold text-slate-100 font-mono flex items-center gap-2">
                Frachtraum-Planer (Cargo-Fit)
                <span className="text-[11px] px-2 py-0.5 rounded bg-cyan-950 text-cyan-400 border border-cyan-800 font-normal">
                  Star Citizen 4.x PU
                </span>
                {isCalculating && (
                  <span className="text-[10px] text-cyan-400 animate-pulse font-normal">
                    Berechne...
                  </span>
                )}
              </h2>
              <p className="text-xs text-slate-400 font-mono">
                Prüft physische Kisten-Stauung auf 1.25m Frachtgitter & MaxBox-Höhen
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Body Content */}
        <div className="flex-1 overflow-y-auto p-4 space-y-4">
          {/* Top Controls: Ship Selector & Presets */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <div className="md:col-span-2 sc-glass p-3 rounded-lg border border-slate-800 flex flex-col justify-between">
              <div>
                <label className="text-xs font-mono font-bold text-slate-300 block mb-1.5">
                  Ziel-Schiff für Laderaum-Prüfung:
                </label>
                <div className="flex items-center gap-2">
                  <select
                    value={shipName}
                    onChange={(e) => setShipName(e.target.value)}
                    className="flex-1 bg-slate-900 border border-slate-700 rounded px-3 py-2 text-sm font-mono text-cyan-300 focus:outline-none focus:border-cyan-400"
                  >
                    {SHIPS_LIST.map((s) => (
                      <option key={s} value={s}>
                        {s}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              {/* Quick Presets */}
              <div className="mt-3 flex items-center gap-1.5 flex-wrap pt-2 border-t border-slate-800/80">
                <span className="text-[10px] text-slate-500 font-mono uppercase mr-1">Presets:</span>
                <button
                  onClick={() => applyPreset({ 32: 2, 16: 2 })}
                  className="px-2 py-1 rounded bg-slate-800 hover:bg-slate-700 text-[11px] font-mono text-slate-300 border border-slate-700 transition"
                >
                  96 SCU (2×32 + 2×16)
                </button>
                <button
                  onClick={() => applyPreset({ 32: 4, 16: 2, 8: 2 })}
                  className="px-2 py-1 rounded bg-slate-800 hover:bg-slate-700 text-[11px] font-mono text-slate-300 border border-slate-700 transition"
                >
                  176 SCU (Großfracht)
                </button>
                <button
                  onClick={() => applyPreset({ 8: 4, 4: 4, 2: 4, 1: 4 })}
                  className="px-2 py-1 rounded bg-slate-800 hover:bg-slate-700 text-[11px] font-mono text-slate-300 border border-slate-700 transition"
                >
                  60 SCU (Gemischter Loot)
                </button>
                <button
                  onClick={() => setCrates({})}
                  className="px-2 py-1 rounded bg-rose-950/40 hover:bg-rose-900/60 text-[11px] font-mono text-rose-300 border border-rose-800/60 transition flex items-center gap-1"
                >
                  <RotateCcw className="w-3 h-3" />
                  Leeren
                </button>
              </div>
            </div>

            {/* Total Manifest Summary */}
            <div className="sc-glass p-3 rounded-lg border border-slate-800 flex flex-col justify-between">
              <div>
                <span className="text-xs font-mono text-slate-400 block mb-1">Gewähltes Frachtvolumen:</span>
                <div className="text-2xl font-bold font-mono text-cyan-300">
                  {totalRequestedScu.toLocaleString('de-DE')} <span className="text-sm font-normal text-slate-400">SCU</span>
                </div>
                <div className="text-xs font-mono text-slate-400 mt-1">
                  Kisten-Gesamtzahl: <span className="text-slate-200 font-bold">{Object.values(crates).reduce((a, b) => a + b, 0)} Stück</span>
                </div>
              </div>

              {fitResult && (
                <div className="mt-2 pt-2 border-t border-slate-800/80 flex items-center justify-between text-xs font-mono">
                  <span className="text-slate-500">Laderaum {fitResult.shipName}:</span>
                  <span className="text-slate-300 font-bold">{fitResult.totalCapacityScu} SCU</span>
                </div>
              )}
            </div>
          </div>

          {/* Crate Selector Steppers */}
          <div className="sc-glass p-3 rounded-lg border border-slate-800">
            <h3 className="text-xs font-mono font-bold text-slate-300 uppercase tracking-wider mb-2 flex items-center gap-2">
              <Box className="w-4 h-4 text-cyan-400" />
              Kisten-Zusammensetzung (Anzahl wählen)
            </h3>

            <div className="grid grid-cols-2 sm:grid-cols-4 md:grid-cols-7 gap-2">
              {CRATE_SIZES.map((scu) => {
                const count = crates[scu] || 0;
                const col = CRATE_COLORS[scu];
                return (
                  <div
                    key={scu}
                    className={`p-2.5 rounded border flex flex-col items-center justify-between transition ${
                      count > 0 ? `${col.bg} ${col.border}` : 'bg-slate-900/60 border-slate-800'
                    }`}
                  >
                    <div className="text-center">
                      <div className={`text-base font-bold font-mono ${count > 0 ? col.text : 'text-slate-300'}`}>
                        {scu} SCU
                      </div>
                      <div className="text-[10px] font-mono text-slate-500">
                        {scu === 32 ? '10×2.5m' : scu === 16 ? '5×2.5m' : scu === 8 ? '2.5×2.5m' : `${scu * 1.25}m`}
                      </div>
                    </div>

                    <div className="flex items-center gap-1.5 mt-2">
                      <button
                        onClick={() => updateCount(scu, -1)}
                        disabled={count <= 0}
                        className="p-1 rounded bg-slate-800 hover:bg-slate-700 disabled:opacity-30 text-slate-200"
                      >
                        <Minus className="w-3 h-3" />
                      </button>
                      <span className="w-6 text-center font-mono font-bold text-sm text-slate-100">
                        {count}
                      </span>
                      <button
                        onClick={() => updateCount(scu, 1)}
                        className="p-1 rounded bg-slate-800 hover:bg-slate-700 text-cyan-300"
                      >
                        <Plus className="w-3 h-3" />
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>

          {/* Live Outcome Banner */}
          {fitResult && (
            <div
              className={`p-4 rounded-xl border flex flex-col md:flex-row items-start md:items-center justify-between gap-3 ${
                fitResult.fits
                  ? 'bg-emerald-950/40 border-emerald-500/60 shadow-[0_0_20px_rgba(16,185,129,0.15)]'
                  : 'bg-rose-950/40 border-rose-500/60 shadow-[0_0_20px_rgba(239,68,68,0.15)]'
              }`}
            >
              <div className="flex items-start gap-3">
                {fitResult.fits ? (
                  <CheckCircle className="w-7 h-7 text-emerald-400 shrink-0 mt-0.5" />
                ) : (
                  <AlertTriangle className="w-7 h-7 text-rose-400 shrink-0 mt-0.5" />
                )}
                <div>
                  <div className="flex items-center gap-2">
                    <span
                      className={`text-sm font-bold font-mono uppercase px-2 py-0.5 rounded ${
                        fitResult.fits ? 'bg-emerald-500/20 text-emerald-300' : 'bg-rose-500/20 text-rose-300'
                      }`}
                    >
                      {fitResult.fits ? 'PASST PERFEKT AUFS GITTER ✓' : 'PASST NICHT VOLLSTÄNDIG ✖'}
                    </span>
                    <span className="text-xs font-mono text-slate-400">
                      in {fitResult.shipName}
                    </span>
                  </div>
                  <div className="text-xs font-mono text-slate-300 mt-1">
                    {fitResult.fits ? (
                      <span>
                        Alle {fitResult.requestedTotalScu} SCU finden unter Berücksichtigung von MaxBox-Höhen & physischem Stacking Platz.
                      </span>
                    ) : (
                      <span>
                        {fitResult.totalPlacedScu} von {fitResult.requestedTotalScu} SCU geladen. Restliche Kisten können physisch nicht untergebracht werden.
                      </span>
                    )}
                  </div>

                  {fitResult.rejectionReasons && fitResult.rejectionReasons.length > 0 && (
                    <div className="mt-2 text-xs font-mono text-rose-300 space-y-1">
                      {fitResult.rejectionReasons.map((r, i) => (
                        <div key={i} className="flex items-center gap-1.5">
                          <span>⚠️</span>
                          <span>{r}</span>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>

              {/* Quick metrics */}
              <div className="text-right shrink-0 font-mono">
                <div className="text-xs text-slate-400">Belegung:</div>
                <div className="text-lg font-bold text-slate-100">
                  {fitResult.totalPlacedScu} / {fitResult.totalCapacityScu} SCU
                </div>
                <div className="text-[11px] text-slate-500">
                  Verbleibend frei: <span className="text-emerald-400">{fitResult.remainingFreeScu} SCU</span>
                </div>
              </div>
            </div>
          )}

          {/* Grids Layout View */}
          {fitResult && fitResult.grids && fitResult.grids.length > 0 && (
            <div className="sc-glass p-3 rounded-lg border border-slate-800 space-y-3">
              <h3 className="text-xs font-mono font-bold text-slate-300 uppercase tracking-wider flex items-center gap-2">
                <Layers className="w-4 h-4 text-cyan-400" />
                Frachtgitter & Stauung ({fitResult.grids.length} Frachtbereiche)
              </h3>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                {fitResult.grids.map((grid, idx) => (
                  <div key={idx} className="p-3 rounded-lg bg-slate-900/70 border border-slate-800/80 space-y-2">
                    <div className="flex items-center justify-between">
                      <span className="font-bold text-xs text-cyan-300 font-mono">{grid.gridName}</span>
                      <span className="text-[11px] font-mono text-slate-400">
                        {grid.usedScu} / {grid.capacityScu} SCU ({Math.round((grid.usedScu / grid.capacityScu) * 100)}%)
                      </span>
                    </div>

                    {/* Capacity Bar */}
                    <div className="w-full h-2 bg-slate-800 rounded-full overflow-hidden">
                      <div
                        className={`h-full transition-all duration-300 ${
                          grid.usedScu > grid.capacityScu
                            ? 'bg-rose-500'
                            : grid.usedScu === grid.capacityScu
                            ? 'bg-emerald-500'
                            : 'bg-cyan-500'
                        }`}
                        style={{ width: `${Math.min(100, (grid.usedScu / grid.capacityScu) * 100)}%` }}
                      />
                    </div>

                    {/* Placed Crates Manifest */}
                    <div className="pt-2 border-t border-slate-800/60">
                      <span className="text-[10px] text-slate-500 font-mono uppercase block mb-1">
                        Platzierte Kisten in diesem Frachtbereich:
                      </span>
                      {grid.placedCrates.length > 0 ? (
                        <div className="flex flex-wrap gap-1.5 max-h-32 overflow-y-auto">
                          {grid.placedCrates.map((p, pIdx) => {
                            const col = CRATE_COLORS[p.scu] || CRATE_COLORS[1];
                            return (
                              <span
                                key={pIdx}
                                className={`px-2 py-0.5 rounded text-[10px] font-mono border ${col.bg} ${col.text} ${col.border}`}
                                title={`Gitter-Position: X=${p.x}, Y=${p.y}, Z=${p.z} (Größe: ${p.dimX}×${p.dimY}×${p.dimZ})`}
                              >
                                {p.scu} SCU @ [{p.x},{p.y},{p.z}]
                              </span>
                            );
                          })}
                        </div>
                      ) : (
                        <span className="text-xs font-mono text-slate-500 italic">Keine Kisten in diesem Bereich</span>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Fleet Compatibility Sweep */}
          {fitResult && fitResult.compatibleFleetShips && fitResult.compatibleFleetShips.length > 0 && (
            <div className="sc-glass p-3 rounded-lg border border-slate-800 space-y-2">
              <h3 className="text-xs font-mono font-bold text-slate-300 uppercase tracking-wider flex items-center gap-2">
                <Sparkles className="w-4 h-4 text-amber-400" />
                Flotten-Check: Welche Schiffe aus deinem Hangar können das laden?
              </h3>

              <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-2">
                {fitResult.compatibleFleetShips.map((fs, idx) => (
                  <div
                    key={idx}
                    className={`p-2.5 rounded border transition flex flex-col justify-between ${
                      fs.fits
                        ? 'bg-emerald-950/20 border-emerald-500/30'
                        : 'bg-slate-900/40 border-slate-800'
                    }`}
                  >
                    <div>
                      <div className="flex items-start justify-between gap-1">
                        <span className="text-xs font-bold text-slate-200 font-mono">{fs.shipName}</span>
                        <span
                          className={`text-[9px] font-mono font-bold px-1.5 py-0.5 rounded border uppercase ${
                            fs.fits
                              ? 'bg-emerald-500/20 text-emerald-300 border-emerald-500/40'
                              : 'bg-slate-800 text-slate-400 border-slate-700'
                          }`}
                        >
                          {fs.fits ? '✓ Passt' : '✖ Zu klein'}
                        </span>
                      </div>
                      <div className="text-[11px] font-mono text-slate-400 mt-1">
                        Kapazität: {fs.totalCapacityScu} SCU · Frei: {fs.freeScu} SCU
                      </div>
                    </div>

                    <button
                      onClick={() => setShipName(fs.shipName)}
                      className="mt-2 text-[10px] font-mono text-cyan-400 hover:text-cyan-200 transition text-left"
                    >
                      → Als Ziel-Schiff wählen
                    </button>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="p-3 border-t border-slate-800 bg-slate-900/80 flex items-center justify-between">
          <div className="text-xs font-mono text-slate-400">
            SCLogMate Laderaum-Geometrie & Kisten-Kollision · Stand Star Citizen 4.x
          </div>
          <button
            onClick={onClose}
            className="px-4 py-1.5 rounded bg-slate-800 hover:bg-slate-700 text-xs font-mono text-slate-200 transition cursor-pointer"
          >
            Schließen
          </button>
        </div>
      </div>
    </div>
  );
};
