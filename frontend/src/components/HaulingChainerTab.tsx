import React, { useState, useEffect } from 'react';
import {
  bridge,
  HaulingJobItemDto,
  HaulingChainedRouteResultDto,
} from '../services/photinoBridge';
import { CargoFitModal } from './CargoFitModal';
import {
  Truck,
  Package,
  Layers,
  Sparkles,
  AlertTriangle,
  CheckCircle2,
  Navigation,
  ArrowRight,
  Plus,
  Trash2,
  RotateCcw,
  Box,
} from 'lucide-react';

const SHIPS_CATALOG = [
  { name: 'Crusader C2 Hercules', scu: 696 },
  { name: 'Crusader M2 Hercules', scu: 522 },
  { name: 'Drake Caterpillar', scu: 576 },
  { name: 'Anvil Carrack', scu: 456 },
  { name: 'Aegis Reclaimer', scu: 420 },
  { name: 'RSI Constellation Taurus', scu: 174 },
  { name: 'RSI Zeus Mk II CL', scu: 128 },
  { name: 'MISC Freelancer MAX', scu: 120 },
  { name: 'Crusader Mercury Star Runner', scu: 114 },
  { name: 'RSI Constellation Andromeda', scu: 96 },
  { name: 'ARGO RAFT', scu: 96 },
  { name: 'Drake Corsair', scu: 72 },
  { name: 'MISC Freelancer', scu: 66 },
  { name: 'Crusader C1 Spirit', scu: 64 },
  { name: 'MISC Hull A', scu: 64 },
  { name: 'Drake Cutlass Black', scu: 46 },
  { name: 'Consolidated Outland Nomad', scu: 24 },
  { name: 'Drake Vulture', scu: 12 },
  { name: 'Aegis Avenger Titan', scu: 8 },
];

const PRESET_OPTIONS = [
  { key: 'hurston_express', label: 'Hurston Express (Lorville - Everus - Arial)' },
  { key: 'stanton_interplanetary', label: 'Stanton Interplanetary (Arial - Crusader - ArcCorp)' },
  { key: 'distribution_center_run', label: 'Verteilzentrum & DC-Logistik (Cassidy - Orinth)' },
];

export const HaulingChainerTab: React.FC = () => {
  const [shipName, setShipName] = useState<string>('MISC Freelancer MAX');
  const [jobs, setJobs] = useState<HaulingJobItemDto[]>([]);
  const [optimizeOrder, setOptimizeOrder] = useState<boolean>(true);
  const [routeResult, setRouteResult] = useState<HaulingChainedRouteResultDto | null>(null);
  const [loading, setLoading] = useState<boolean>(false);
  const [isCargoFitOpen, setIsCargoFitOpen] = useState<boolean>(false);
  const [showAddModal, setShowAddModal] = useState<boolean>(false);

  // New job modal form state
  const [newTitle, setNewTitle] = useState('');
  const [newPickup, setNewPickup] = useState('Everus Harbor (Hurston)');
  const [newDelivery, setNewDelivery] = useState('Lorville Teasa (Hurston)');
  const [newScu, setNewScu] = useState(32);
  const [newCommodity, setNewCommodity] = useState('Beryl');
  const [newReward, setNewReward] = useState(45000);
  const [newContractor, setNewContractor] = useState('Red Wind Line Haul');

  // Load initial preset on mount
  useEffect(() => {
    loadPreset('hurston_express');
  }, []);

  const loadPreset = async (presetKey: string) => {
    setLoading(true);
    try {
      const presetJobs = await bridge.getHaulingPresets(presetKey);
      setJobs(presetJobs);
      recalculate(shipName, presetJobs, optimizeOrder);
    } catch (err) {
      console.error('Failed to load hauling preset:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadActiveJobs = async () => {
    setLoading(true);
    try {
      const activeJobs = await bridge.getActiveHaulingJobs();
      setJobs(activeJobs);
      recalculate(shipName, activeJobs, optimizeOrder);
    } catch (err) {
      console.error('Failed to import active hauling jobs:', err);
    } finally {
      setLoading(false);
    }
  };

  const recalculate = async (ship: string, jobList: HaulingJobItemDto[], optimize: boolean) => {
    try {
      const res = await bridge.calculateHaulingChain(ship, jobList, optimize);
      setRouteResult(res);
    } catch (err) {
      console.error('Error calculating hauling chain:', err);
    }
  };

  const handleShipChange = (newShip: string) => {
    setShipName(newShip);
    recalculate(newShip, jobs, optimizeOrder);
  };

  const handleToggleOptimize = () => {
    const nextVal = !optimizeOrder;
    setOptimizeOrder(nextVal);
    recalculate(shipName, jobs, nextVal);
  };

  const handleToggleJob = (id: string) => {
    const updated = jobs.map((j) => (j.id === id ? { ...j, isEnabled: !j.isEnabled } : j));
    setJobs(updated);
    recalculate(shipName, updated, optimizeOrder);
  };

  const handleDeleteJob = (id: string) => {
    const updated = jobs.filter((j) => j.id !== id);
    setJobs(updated);
    recalculate(shipName, updated, optimizeOrder);
  };

  const handleAddCustomJob = () => {
    if (!newTitle.trim() || !newPickup.trim() || !newDelivery.trim() || newScu <= 0) return;

    const newJob: HaulingJobItemDto = {
      id: `custom_${Date.now()}`,
      title: newTitle.trim(),
      pickupLocation: newPickup.trim(),
      deliveryLocation: newDelivery.trim(),
      scu: newScu,
      commodity: newCommodity.trim() || 'Allgemeine Fracht',
      rewardAuec: newReward,
      contractor: newContractor.trim() || 'Covalex Shipping',
      isEnabled: true,
    };

    const updated = [...jobs, newJob];
    setJobs(updated);
    recalculate(shipName, updated, optimizeOrder);
    setShowAddModal(false);
    setNewTitle('');
  };

  const formatAuec = (num: number) => num.toLocaleString('de-DE');

  return (
    <div className="flex flex-col space-y-4">
      {/* Top Controls Bar */}
      <div className="sc-glass rounded-lg p-4 border border-slate-800 flex flex-wrap items-center justify-between gap-4">
        {/* Ship Selector */}
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2 text-cyan-400">
            <Truck className="w-5 h-5 text-cyan-400" />
            <span className="text-xs font-semibold uppercase tracking-wider text-slate-300">
              Frachtschiff:
            </span>
          </div>
          <select
            value={shipName}
            onChange={(e) => handleShipChange(e.target.value)}
            className="bg-slate-900 border border-cyan-500/40 rounded px-3 py-1.5 text-xs text-cyan-200 font-mono focus:outline-none focus:border-cyan-400 cursor-pointer shadow-[0_0_10px_rgba(0,240,255,0.1)]"
          >
            {SHIPS_CATALOG.map((ship) => (
              <option key={ship.name} value={ship.name}>
                {ship.name} ({ship.scu} SCU)
              </option>
            ))}
          </select>
        </div>

        {/* Action Buttons */}
        <div className="flex flex-wrap items-center gap-2">
          {/* Preset Selector */}
          <select
            onChange={(e) => loadPreset(e.target.value)}
            defaultValue=""
            className="bg-slate-900 border border-slate-700 rounded px-2.5 py-1.5 text-xs text-slate-300 focus:outline-none focus:border-cyan-500 cursor-pointer"
          >
            <option value="" disabled>
              Preset-Routen laden...
            </option>
            {PRESET_OPTIONS.map((p) => (
              <option key={p.key} value={p.key}>
                {p.label}
              </option>
            ))}
          </select>

          {/* Import Active Contracts Button */}
          <button
            onClick={loadActiveJobs}
            disabled={loading}
            className="px-3 py-1.5 rounded bg-emerald-950/60 hover:bg-emerald-900/80 text-emerald-300 text-xs font-semibold border border-emerald-700/60 transition cursor-pointer flex items-center gap-1.5 shadow-sm"
            title="Importiert aktive Hauling- und Frachtaufträge direkt aus Game.log & SQLite"
          >
            <RotateCcw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
            <span>Aktive Aufträge laden</span>
          </button>

          {/* Add Custom Job */}
          <button
            onClick={() => setShowAddModal(true)}
            className="px-3 py-1.5 rounded bg-cyan-950/60 hover:bg-cyan-900/80 text-cyan-300 text-xs font-semibold border border-cyan-700/60 transition cursor-pointer flex items-center gap-1.5 shadow-sm"
          >
            <Plus className="w-3.5 h-3.5" />
            <span>Auftrag hinzufügen</span>
          </button>

          {/* Optimize Order Toggle */}
          <button
            onClick={handleToggleOptimize}
            className={`px-3 py-1.5 rounded text-xs font-semibold border transition cursor-pointer flex items-center gap-1.5 ${
              optimizeOrder
                ? 'bg-amber-950/50 text-amber-300 border-amber-600/70 shadow-[0_0_10px_rgba(245,158,11,0.2)]'
                : 'bg-slate-900 text-slate-400 border-slate-700 hover:text-slate-200'
            }`}
            title="Sortiert Abholungen und Lieferungen automatisch nach Planeten-Clustern"
          >
            <Sparkles className="w-3.5 h-3.5" />
            <span>{optimizeOrder ? 'Auto-Cluster aktiv' : 'Manuelle Reihenfolge'}</span>
          </button>

          {/* Direct Cargo-Fit Button */}
          <button
            onClick={() => setIsCargoFitOpen(true)}
            className="px-3.5 py-1.5 rounded bg-indigo-950/70 hover:bg-indigo-900 text-indigo-300 text-xs font-semibold border border-indigo-500/60 transition cursor-pointer flex items-center gap-1.5 shadow-[0_0_12px_rgba(99,102,241,0.2)]"
            title="Testet das Frachtgitter und die Kistenverteilung physikalisch im Cargo-Fit 3D-Packer"
          >
            <Layers className="w-3.5 h-3.5 text-indigo-400" />
            <span>Im Cargo-Grid Packer prüfen</span>
          </button>
        </div>
      </div>

      {/* KPI Cards Header */}
      {routeResult && (
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          {/* Total Reward */}
          <div className="sc-glass rounded-lg p-3.5 border border-slate-800 sc-hud-corner">
            <span className="text-[11px] font-semibold text-slate-400 uppercase tracking-wider">
              Gesamt-Auszahlung
            </span>
            <div className="mt-1 text-xl font-bold font-mono text-emerald-300">
              +{formatAuec(routeResult.totalRewardAuec)} <span className="text-xs font-normal text-slate-400">aUEC</span>
            </div>
            <div className="mt-1 text-[11px] text-slate-400 font-mono">
              {routeResult.totalJobs} Aufträge gebündelt
            </div>
          </div>

          {/* Total Cargo Moved */}
          <div className="sc-glass rounded-lg p-3.5 border border-slate-800 sc-hud-corner">
            <span className="text-[11px] font-semibold text-slate-400 uppercase tracking-wider">
              Frachtvolumen gesamt
            </span>
            <div className="mt-1 text-xl font-bold font-mono text-cyan-300">
              {routeResult.totalScuMoved} <span className="text-xs font-normal text-slate-400">SCU bewegt</span>
            </div>
            <div className="mt-1 text-[11px] text-slate-400 font-mono">
              Schiffskapazität: {routeResult.shipCapacityScu} SCU
            </div>
          </div>

          {/* Peak Load & Fill Projection */}
          <div
            className={`sc-glass rounded-lg p-3.5 border sc-hud-corner transition ${
              routeResult.isFeasible
                ? 'border-emerald-800/60 bg-emerald-950/10'
                : 'border-rose-700/80 bg-rose-950/30 ring-1 ring-rose-500/50'
            }`}
          >
            <div className="flex justify-between items-start">
              <span className="text-[11px] font-semibold text-slate-300 uppercase tracking-wider">
                Spitzen-Füllstand (Peak)
              </span>
              {routeResult.isFeasible ? (
                <CheckCircle2 className="w-4 h-4 text-emerald-400" />
              ) : (
                <AlertTriangle className="w-4 h-4 text-rose-400 animate-pulse" />
              )}
            </div>
            <div
              className={`mt-1 text-xl font-bold font-mono ${
                routeResult.isFeasible ? 'text-emerald-300' : 'text-rose-300'
              }`}
            >
              {routeResult.peakLoadScu} / {routeResult.shipCapacityScu} SCU
              <span className="text-xs font-normal ml-1.5 opacity-80">
                ({routeResult.peakFillPercentage}%)
              </span>
            </div>
            <div className="mt-1 text-[11px] font-mono">
              {routeResult.isFeasible ? (
                <span className="text-emerald-400">✓ Kein Überbuchen</span>
              ) : (
                <span className="text-rose-400 font-bold">
                  ⚠ Überbucht um +{routeResult.peakLoadScu - routeResult.shipCapacityScu} SCU!
                </span>
              )}
            </div>
          </div>

          {/* Stops and Distance */}
          <div className="sc-glass rounded-lg p-3.5 border border-slate-800 sc-hud-corner">
            <span className="text-[11px] font-semibold text-slate-400 uppercase tracking-wider">
              Route &amp; Reisedauer
            </span>
            <div className="mt-1 text-xl font-bold font-mono text-amber-300">
              {routeResult.totalWaypoints} <span className="text-xs font-normal text-slate-400">Stopps</span>
              <span className="text-sm font-normal text-slate-400 ml-2">~{routeResult.totalDistanceGm} Gm</span>
            </div>
            <div className="mt-1 text-[11px] text-slate-400 font-mono flex items-center gap-1">
              <Navigation className="w-3 h-3 text-amber-400" />
              <span>Geschätzte QT-Zeit: ~{routeResult.estimatedTotalQtMinutes} Min</span>
            </div>
          </div>
        </div>
      )}

      {/* Warnings & Feasibility Banner */}
      {routeResult && routeResult.warnings && routeResult.warnings.length > 0 && (
        <div
          className={`rounded-lg p-3.5 border text-xs font-mono flex items-start gap-3 ${
            routeResult.isFeasible
              ? 'bg-amber-950/40 border-amber-700/60 text-amber-200'
              : 'bg-rose-950/60 border-rose-700 text-rose-200 shadow-[0_0_15px_rgba(244,63,94,0.2)]'
          }`}
        >
          <AlertTriangle className="w-5 h-5 shrink-0 mt-0.5 text-amber-400" />
          <div className="flex-1 space-y-1">
            <div className="font-bold text-sm">
              {routeResult.isFeasible ? 'Routen-Hinweis' : 'Kritische Überbuchungswarnung!'}
            </div>
            {routeResult.warnings.map((w, idx) => (
              <div key={idx} className="opacity-95">
                • {w}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Main Section: Two Columns (Waypoints Timeline & Jobs List) */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-4">
        {/* Left Column: Visual Waypoint Timeline & Füllstandsprojektion (7 Cols) */}
        <div className="lg:col-span-7 space-y-3">
          <div className="flex items-center justify-between px-1">
            <div className="flex items-center gap-2">
              <Navigation className="w-4 h-4 text-cyan-400" />
              <h3 className="text-sm font-semibold text-slate-200 uppercase tracking-wider font-mono">
                Wegpunkt-Sequenz &amp; Füllstandsprojektion
              </h3>
            </div>
            <span className="text-xs text-slate-400 font-mono">
              {routeResult?.waypoints.length || 0} Wegpunkte
            </span>
          </div>

          {routeResult && routeResult.waypoints.length > 0 ? (
            <div className="space-y-3">
              {routeResult.waypoints.map((wp, idx) => {
                const isOver = wp.isOverloaded;
                const fillRatio = Math.min(100, wp.fillPercentage);

                return (
                  <div
                    key={idx}
                    className={`sc-glass rounded-lg p-3.5 border transition relative ${
                      isOver
                        ? 'border-rose-600 bg-rose-950/20'
                        : 'border-slate-800 hover:border-cyan-500/40'
                    }`}
                  >
                    {/* Header */}
                    <div className="flex items-center justify-between gap-2">
                      <div className="flex items-center gap-2.5">
                        <span className="w-6 h-6 rounded-full bg-slate-900 border border-slate-700 text-cyan-300 font-mono text-xs flex items-center justify-center font-bold">
                          {wp.stepIndex}
                        </span>

                        <div>
                          <div className="font-semibold text-slate-200 text-sm flex items-center gap-2">
                            <span>{wp.location}</span>
                            <span className="text-[10px] px-1.5 py-0.5 rounded bg-slate-800 text-slate-400 font-mono">
                              {wp.celestialBody}
                            </span>
                          </div>
                        </div>
                      </div>

                      {/* Action Badge */}
                      <div className="flex items-center gap-2">
                        {wp.action === 'Pickup' && (
                          <span className="px-2 py-0.5 rounded text-[10px] font-bold font-mono bg-emerald-950 text-emerald-300 border border-emerald-700/60">
                            ABHOLUNG (+{wp.deltaScu} SCU)
                          </span>
                        )}
                        {wp.action === 'Delivery' && (
                          <span className="px-2 py-0.5 rounded text-[10px] font-bold font-mono bg-cyan-950 text-cyan-300 border border-cyan-700/60">
                            LIEFERUNG ({wp.deltaScu} SCU)
                          </span>
                        )}
                        {wp.action === 'Combined' && (
                          <span className="px-2 py-0.5 rounded text-[10px] font-bold font-mono bg-purple-950 text-purple-300 border border-purple-700/60">
                            KOMBINIERT ({wp.deltaScu >= 0 ? `+${wp.deltaScu}` : wp.deltaScu} SCU)
                          </span>
                        )}
                      </div>
                    </div>

                    {/* Cargo Load Progress Bar */}
                    <div className="mt-3 bg-slate-950/60 rounded p-2.5 border border-slate-800/80">
                      <div className="flex justify-between items-center text-xs font-mono mb-1.5">
                        <span className="text-slate-400 flex items-center gap-1.5">
                          <Package className="w-3.5 h-3.5 text-cyan-400" />
                          Frachtraum-Belegung nach Stopp:
                        </span>
                        <span
                          className={`font-bold ${
                            isOver
                              ? 'text-rose-400'
                              : wp.fillPercentage > 85
                              ? 'text-amber-300'
                              : 'text-emerald-300'
                          }`}
                        >
                          {wp.currentLoadScu} / {wp.capacityScu} SCU ({wp.fillPercentage}%)
                          {isOver && ` [! +${wp.overloadAmountScu} SCU Überhang]`}
                        </span>
                      </div>

                      {/* Bar */}
                      <div className="w-full bg-slate-900 rounded-full h-2.5 overflow-hidden border border-slate-800">
                        <div
                          className={`h-full transition-all duration-300 rounded-full ${
                            isOver
                              ? 'bg-rose-500'
                              : wp.fillPercentage > 85
                              ? 'bg-amber-500'
                              : 'bg-emerald-500'
                          }`}
                          style={{ width: `${fillRatio}%` }}
                        />
                      </div>
                    </div>

                    {/* Handled Items Detail */}
                    <div className="mt-2.5 space-y-1">
                      {wp.cargoDetails.map((detail, dIdx) => (
                        <div
                          key={dIdx}
                          className="text-[11px] font-mono text-slate-400 flex items-center justify-between px-1"
                        >
                          <span className="truncate max-w-xs">
                            • {detail.jobTitle} ({detail.commodity})
                          </span>
                          <span
                            className={detail.scuDelta > 0 ? 'text-emerald-400 font-semibold' : 'text-cyan-400 font-semibold'}
                          >
                            {detail.scuDelta > 0 ? `+${detail.scuDelta}` : detail.scuDelta} SCU
                          </span>
                        </div>
                      ))}
                    </div>

                    {/* Distance / Transit to next stop */}
                    {idx < routeResult.waypoints.length - 1 && (
                      <div className="mt-2 pt-2 border-t border-slate-800/60 flex items-center justify-between text-[11px] font-mono text-slate-500">
                        <span className="flex items-center gap-1">
                          <ArrowRight className="w-3 h-3 text-cyan-500" />
                          Nächster Sprung nach {routeResult.waypoints[idx + 1].location}
                        </span>
                        <span>
                          ~{wp.distanceToNextGm} Gm (~{wp.estimatedQtMinutes} Min QT)
                        </span>
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          ) : (
            <div className="sc-glass rounded-lg p-10 text-center text-slate-500 font-mono">
              Keine Wegpunkte vorhanden. Wähle ein Preset oder füge Aufträge hinzu.
            </div>
          )}
        </div>

        {/* Right Column: Chained Jobs & Box Breakdown (5 Cols) */}
        <div className="lg:col-span-5 space-y-4">
          {/* Box Breakdown Card */}
          {routeResult && (
            <div className="sc-glass rounded-lg p-3.5 border border-slate-800 sc-hud-corner">
              <div className="flex items-center justify-between mb-2">
                <div className="flex items-center gap-2">
                  <Box className="w-4 h-4 text-indigo-400" />
                  <h4 className="text-xs font-semibold text-slate-200 uppercase tracking-wider font-mono">
                    Spitzen-Kistenaufteilung ({routeResult.peakLoadScu} SCU)
                  </h4>
                </div>
                <button
                  onClick={() => setIsCargoFitOpen(true)}
                  className="text-[10px] text-indigo-400 hover:text-indigo-300 font-mono underline cursor-pointer"
                >
                  3D-Packer öffnen &rarr;
                </button>
              </div>

              <div className="flex flex-wrap gap-1.5">
                {Object.entries(routeResult.peakBoxBreakdown).length > 0 ? (
                  Object.entries(routeResult.peakBoxBreakdown).map(([scuSize, count]) => (
                    <div
                      key={scuSize}
                      className="px-2 py-1 rounded bg-slate-900 border border-indigo-700/50 text-indigo-300 text-xs font-mono flex items-center gap-1.5"
                    >
                      <span className="font-bold">{count}x</span>
                      <span>{scuSize} SCU Box</span>
                    </div>
                  ))
                ) : (
                  <span className="text-xs text-slate-500 font-mono">0 SCU geladen</span>
                )}
              </div>
            </div>
          )}

          {/* Chained Jobs List */}
          <div className="sc-glass rounded-lg p-3.5 border border-slate-800">
            <div className="flex items-center justify-between mb-3">
              <div className="flex items-center gap-2">
                <Package className="w-4 h-4 text-cyan-400" />
                <h4 className="text-xs font-semibold text-slate-200 uppercase tracking-wider font-mono">
                  Gebündelte Frachtaufträge ({jobs.length})
                </h4>
              </div>
            </div>

            <div className="space-y-2.5 max-h-[500px] overflow-y-auto pr-1">
              {jobs.map((job) => (
                <div
                  key={job.id}
                  className={`p-2.5 rounded border transition text-xs font-mono ${
                    job.isEnabled
                      ? 'bg-slate-900/80 border-slate-700 text-slate-300'
                      : 'bg-slate-950/40 border-slate-900 text-slate-600 opacity-60'
                  }`}
                >
                  <div className="flex items-start justify-between gap-2">
                    <div className="flex items-center gap-2">
                      <input
                        type="checkbox"
                        checked={job.isEnabled}
                        onChange={() => handleToggleJob(job.id)}
                        className="cursor-pointer accent-cyan-500 rounded"
                        title="Auftrag aktivieren / deaktivieren"
                      />
                      <span className="font-semibold text-slate-200">{job.title}</span>
                    </div>
                    <button
                      onClick={() => handleDeleteJob(job.id)}
                      className="text-slate-500 hover:text-rose-400 p-0.5 rounded cursor-pointer transition"
                      title="Auftrag entfernen"
                    >
                      <Trash2 className="w-3.5 h-3.5" />
                    </button>
                  </div>

                  <div className="mt-2 grid grid-cols-2 gap-1 text-[11px] text-slate-400">
                    <div>
                      <span className="text-slate-500 block">Abholung:</span>
                      <span className="text-emerald-400">{job.pickupLocation}</span>
                    </div>
                    <div>
                      <span className="text-slate-500 block">Lieferung:</span>
                      <span className="text-cyan-400">{job.deliveryLocation}</span>
                    </div>
                  </div>

                  <div className="mt-2 pt-1.5 border-t border-slate-800/80 flex items-center justify-between text-[11px]">
                    <span className="px-1.5 py-0.5 rounded bg-slate-800 text-slate-300 font-bold">
                      {job.scu} SCU ({job.commodity})
                    </span>
                    <span className="text-emerald-400 font-bold">
                      +{formatAuec(job.rewardAuec)} aUEC
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* Add Custom Job Modal */}
      {showAddModal && (
        <div className="fixed inset-0 z-50 bg-black/70 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="sc-glass rounded-lg border border-cyan-500/50 p-6 max-w-md w-full shadow-2xl space-y-4">
            <div className="flex justify-between items-center border-b border-slate-800 pb-2">
              <h3 className="text-sm font-semibold uppercase tracking-wider text-cyan-300 font-mono flex items-center gap-2">
                <Plus className="w-4 h-4 text-cyan-400" />
                Frachtauftrag manuell hinzufügen
              </h3>
              <button
                onClick={() => setShowAddModal(false)}
                className="text-slate-500 hover:text-slate-200 cursor-pointer"
              >
                ✕
              </button>
            </div>

            <div className="space-y-3 text-xs font-mono">
              <div>
                <label className="text-slate-400 block mb-1">Auftrags-Titel:</label>
                <input
                  type="text"
                  value={newTitle}
                  onChange={(e) => setNewTitle(e.target.value)}
                  placeholder="z.B. Covalex Frachtflug: Beryll nach Orison"
                  className="w-full bg-slate-900 border border-slate-700 rounded px-2.5 py-1.5 text-slate-200 focus:outline-none focus:border-cyan-500"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-slate-400 block mb-1">Abhol-Ort:</label>
                  <input
                    type="text"
                    value={newPickup}
                    onChange={(e) => setNewPickup(e.target.value)}
                    className="w-full bg-slate-900 border border-slate-700 rounded px-2.5 py-1.5 text-slate-200 focus:outline-none focus:border-cyan-500"
                  />
                </div>
                <div>
                  <label className="text-slate-400 block mb-1">Liefer-Ziel:</label>
                  <input
                    type="text"
                    value={newDelivery}
                    onChange={(e) => setNewDelivery(e.target.value)}
                    className="w-full bg-slate-900 border border-slate-700 rounded px-2.5 py-1.5 text-slate-200 focus:outline-none focus:border-cyan-500"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-slate-400 block mb-1">Frachtmenge (SCU):</label>
                  <input
                    type="number"
                    min="1"
                    max="1000"
                    value={newScu}
                    onChange={(e) => setNewScu(parseInt(e.target.value) || 16)}
                    className="w-full bg-slate-900 border border-slate-700 rounded px-2.5 py-1.5 text-slate-200 focus:outline-none focus:border-cyan-500"
                  />
                </div>
                <div>
                  <label className="text-slate-400 block mb-1">Ware / Commodity:</label>
                  <input
                    type="text"
                    value={newCommodity}
                    onChange={(e) => setNewCommodity(e.target.value)}
                    className="w-full bg-slate-900 border border-slate-700 rounded px-2.5 py-1.5 text-slate-200 focus:outline-none focus:border-cyan-500"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-slate-400 block mb-1">Belohnung (aUEC):</label>
                  <input
                    type="number"
                    min="0"
                    step="1000"
                    value={newReward}
                    onChange={(e) => setNewReward(parseInt(e.target.value) || 0)}
                    className="w-full bg-slate-900 border border-slate-700 rounded px-2.5 py-1.5 text-slate-200 focus:outline-none focus:border-cyan-500"
                  />
                </div>
                <div>
                  <label className="text-slate-400 block mb-1">Auftraggeber:</label>
                  <input
                    type="text"
                    value={newContractor}
                    onChange={(e) => setNewContractor(e.target.value)}
                    className="w-full bg-slate-900 border border-slate-700 rounded px-2.5 py-1.5 text-slate-200 focus:outline-none focus:border-cyan-500"
                  />
                </div>
              </div>
            </div>

            <div className="flex justify-end gap-2 pt-2 border-t border-slate-800">
              <button
                onClick={() => setShowAddModal(false)}
                className="px-3 py-1.5 rounded bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold cursor-pointer"
              >
                Abbrechen
              </button>
              <button
                onClick={handleAddCustomJob}
                className="px-4 py-1.5 rounded bg-cyan-600 hover:bg-cyan-500 text-slate-950 font-bold text-xs cursor-pointer shadow-md"
              >
                Auftrag einfügen
              </button>
            </div>
          </div>
        </div>
      )}

      {/* CargoFit Physical Packer Modal */}
      {isCargoFitOpen && (
        <CargoFitModal
          isOpen={isCargoFitOpen}
          onClose={() => setIsCargoFitOpen(false)}
          initialShipName={shipName}
          initialCrates={routeResult?.peakBoxBreakdown || { 32: 2 }}
        />
      )}
    </div>
  );
};
