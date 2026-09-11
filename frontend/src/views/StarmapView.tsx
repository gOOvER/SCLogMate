import React, { useEffect, useState, useMemo, useRef } from 'react';
import {
  bridge,
  StarmapObjectDto,
  QuantumDriveDto,
  QuantumRouteResultDto,
  StarmapResponseDto,
} from '../services/photinoBridge';
import {
  Navigation,
  RefreshCw,
  Sparkles,
  ZoomIn,
  ZoomOut,
  Maximize2,
} from 'lucide-react';

export const StarmapView: React.FC = () => {
  const [system, setSystem] = useState<'Stanton' | 'Pyro' | 'Nyx'>('Stanton');
  const [objects, setObjects] = useState<StarmapObjectDto[]>([]);
  const [drives, setDrives] = useState<QuantumDriveDto[]>([]);
  const [selectedId, setSelectedId] = useState<string>('hurston');
  const [fromId, setFromId] = useState<string>('hurston');
  const [toId, setToId] = useState<string>('crusader');
  const [selectedDrive, setSelectedDrive] = useState<string>('Atlas');
  const [routeResult, setRouteResult] = useState<QuantumRouteResultDto | null>(null);
  const [loading, setLoading] = useState<boolean>(false);

  // Layer filters
  const [showStations, setShowStations] = useState<boolean>(true);
  const [showMoons, setShowMoons] = useState<boolean>(true);
  const [showLandingZones, setShowLandingZones] = useState<boolean>(true);

  // Zoom & Pan state
  const [zoom, setZoom] = useState<number>(1);
  const [pan, setPan] = useState<{ x: number; y: number }>({ x: 0, y: 0 });
  const [isDragging, setIsDragging] = useState<boolean>(false);
  const dragStartRef = useRef<{ x: number; y: number }>({ x: 0, y: 0 });
  const canvasRef = useRef<HTMLDivElement | null>(null);

  // Non-passive wheel listener for smooth zoom without page scroll
  useEffect(() => {
    const el = canvasRef.current;
    if (!el) return;

    const onWheel = (e: WheelEvent) => {
      e.preventDefault();
      const zoomFactor = e.deltaY < 0 ? 1.15 : 0.87;
      setZoom((z) => Math.min(3.5, Math.max(0.3, z * zoomFactor)));
    };

    el.addEventListener('wheel', onWheel, { passive: false });
    return () => {
      el.removeEventListener('wheel', onWheel);
    };
  }, []);

  const fetchStarmap = async (sysName: string) => {
    try {
      setLoading(true);
      const res = await bridge.sendRequest<StarmapResponseDto>('get_starmap', { system: sysName });
      if (res?.objects) {
        setObjects(res.objects);
        if (res.drives && res.drives.length > 0) {
          setDrives(res.drives);
          if (!selectedDrive) setSelectedDrive(res.drives[0].name);
        }

        // Auto-select valid objects when switching systems
        const currentExists = res.objects.some((o) => o.id.toLowerCase() === selectedId.toLowerCase());
        if (!currentExists && res.objects.length > 0) {
          const firstNonStar = res.objects.find((o) => o.type !== 'Star' && o.type !== 'Stern') || res.objects[0];
          setSelectedId(firstNonStar.id);
          setFromId(firstNonStar.id);
          const secondObj = res.objects.find((o) => o.id !== firstNonStar.id && o.type !== 'Star' && o.type !== 'Stern') || firstNonStar;
          setToId(secondObj.id);
        }
      }
    } catch (err) {
      console.error('Failed to load starmap:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchStarmap(system);
  }, [system]);

  // Route calculation
  const calculateRoute = async () => {
    if (!fromId || !toId) return;
    try {
      const res = await bridge.sendRequest<QuantumRouteResultDto>('calculate_route', {
        fromId,
        toId,
        driveName: selectedDrive,
      });
      setRouteResult(res);
    } catch (err) {
      console.error('Route calculation failed:', err);
    }
  };

  useEffect(() => {
    if (fromId && toId && objects.length > 0) {
      calculateRoute();
    }
  }, [fromId, toId, selectedDrive, objects]);

  const selectedObject = useMemo(() => {
    return objects.find((o) => o.id.toLowerCase() === selectedId.toLowerCase()) || objects[0] || null;
  }, [objects, selectedId]);

  // Filtered objects on canvas
  const visibleObjects = useMemo(() => {
    return objects.filter((o) => {
      const t = o.type.toLowerCase();
      if (!showStations && (t.includes('station') || t.includes('lagrange'))) return false;
      if (!showMoons && t.includes('moon')) return false;
      if (!showLandingZones && (t.includes('landing') || t.includes('outpost'))) return false;
      return true;
    });
  }, [objects, showStations, showMoons, showLandingZones]);

  // Mouse pan handlers
  const handleMouseDown = (e: React.MouseEvent) => {
    setIsDragging(true);
    dragStartRef.current = { x: e.clientX - pan.x, y: e.clientY - pan.y };
  };

  const handleMouseMove = (e: React.MouseEvent) => {
    if (!isDragging) return;
    setPan({
      x: e.clientX - dragStartRef.current.x,
      y: e.clientY - dragStartRef.current.y,
    });
  };

  const handleMouseUp = () => setIsDragging(false);

  const handleResetView = () => {
    setZoom(1);
    setPan({ x: 0, y: 0 });
  };

  // Center coordinate reference
  const center = 300;
  const scale = 0.65 * zoom;

  return (
    <div className="flex flex-col min-h-full space-y-4 select-none">
      {/* Top Toolbar: System Selector & Quick Filters */}
      <div className="sc-glass rounded-lg p-3 border border-slate-800 flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          {/* System switch pills */}
          <div className="flex items-center bg-slate-900/80 p-1 rounded-md border border-slate-800">
            <button
              onClick={() => setSystem('Stanton')}
              className={`px-3 py-1 text-xs font-semibold rounded cursor-pointer transition ${
                system === 'Stanton'
                  ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40 shadow-[0_0_10px_rgba(0,240,255,0.2)]'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              Stanton System (UEE)
            </button>
            <button
              onClick={() => setSystem('Pyro')}
              className={`px-3 py-1 text-xs font-semibold rounded cursor-pointer transition ${
                system === 'Pyro'
                  ? 'bg-amber-500/20 text-amber-300 border border-amber-500/40 shadow-[0_0_10px_rgba(245,158,11,0.2)]'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              Pyro System (Lawless)
            </button>
            <button
              onClick={() => setSystem('Nyx')}
              className={`px-3 py-1 text-xs font-semibold rounded cursor-pointer transition ${
                system === 'Nyx'
                  ? 'bg-purple-500/20 text-purple-300 border border-purple-500/40 shadow-[0_0_10px_rgba(168,85,247,0.2)]'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              Nyx System (Levski)
            </button>
          </div>

          {/* Layer toggles */}
          <div className="flex items-center gap-1.5 ml-2">
            <button
              onClick={() => setShowStations(!showStations)}
              className={`px-2.5 py-1 text-[11px] font-mono rounded border transition cursor-pointer ${
                showStations
                  ? 'bg-cyan-950/40 border-cyan-500/30 text-cyan-300'
                  : 'bg-slate-900/60 border-slate-800 text-slate-500'
              }`}
            >
              🛰️ Stationen
            </button>
            <button
              onClick={() => setShowMoons(!showMoons)}
              className={`px-2.5 py-1 text-[11px] font-mono rounded border transition cursor-pointer ${
                showMoons
                  ? 'bg-cyan-950/40 border-cyan-500/30 text-cyan-300'
                  : 'bg-slate-900/60 border-slate-800 text-slate-500'
              }`}
            >
              🌑 Monde
            </button>
            <button
              onClick={() => setShowLandingZones(!showLandingZones)}
              className={`px-2.5 py-1 text-[11px] font-mono rounded border transition cursor-pointer ${
                showLandingZones
                  ? 'bg-cyan-950/40 border-cyan-500/30 text-cyan-300'
                  : 'bg-slate-900/60 border-slate-800 text-slate-500'
              }`}
            >
              🏙️ Städte / Outposts
            </button>
          </div>
        </div>

        {/* View actions: Zoom, Reset, Refresh */}
        <div className="flex items-center gap-2">
          <button
            onClick={() => setZoom((z) => Math.min(3.5, z + 0.2))}
            title="Vergrößern (oder Mausrad)"
            className="p-1.5 rounded bg-slate-900 border border-slate-800 text-slate-400 hover:text-cyan-400 transition cursor-pointer"
          >
            <ZoomIn className="w-4 h-4" />
          </button>
          <button
            onClick={() => setZoom((z) => Math.max(0.3, z - 0.2))}
            title="Verkleinern (oder Mausrad)"
            className="p-1.5 rounded bg-slate-900 border border-slate-800 text-slate-400 hover:text-cyan-400 transition cursor-pointer"
          >
            <ZoomOut className="w-4 h-4" />
          </button>
          <button
            onClick={handleResetView}
            title="Ansicht zurücksetzen"
            className="p-1.5 rounded bg-slate-900 border border-slate-800 text-slate-400 hover:text-cyan-400 transition cursor-pointer"
          >
            <Maximize2 className="w-4 h-4" />
          </button>
          <button
            onClick={() => fetchStarmap(system)}
            title="Aktualisieren"
            className="p-1.5 rounded bg-slate-900 border border-slate-800 text-slate-400 hover:text-cyan-400 transition cursor-pointer"
          >
            <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin text-cyan-400' : ''}`} />
          </button>
        </div>
      </div>

      {/* Main Starmap Area: Radar Canvas (Left) + Inspector / Route Planner (Right) */}
      <div className="flex-1 grid grid-cols-1 lg:grid-cols-3 gap-4 min-h-[520px]">
        {/* SVG Interactive Canvas */}
        <div
          ref={canvasRef}
          className="lg:col-span-2 sc-glass rounded-lg border border-slate-800 relative overflow-hidden flex items-center justify-center cursor-grab active:cursor-grabbing sc-hud-corner"
          onMouseDown={handleMouseDown}
          onMouseMove={handleMouseMove}
          onMouseUp={handleMouseUp}
          onMouseLeave={handleMouseUp}
        >
          {/* Subtle Radar Background Grid */}
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_center,rgba(0,240,255,0.05)_0%,transparent_70%)] pointer-events-none" />

          {/* SVG Canvas */}
          <svg
            viewBox="0 0 600 600"
            className="w-full h-full max-h-[750px]"
            style={{ transform: `translate(${pan.x}px, ${pan.y}px)` }}
          >
            <defs>
              <filter id="glow" x="-20%" y="-20%" width="140%" height="140%">
                <feGaussianBlur stdDeviation="3" result="blur" />
                <feMerge>
                  <feMergeNode in="blur" />
                  <feMergeNode in="SourceGraphic" />
                </feMerge>
              </filter>
            </defs>

            {/* Concentric Radar Rings */}
            {[60, 120, 180, 240, 300].map((r) => (
              <circle
                key={r}
                cx={center}
                cy={center}
                r={r * scale}
                fill="none"
                stroke="#1e293b"
                strokeWidth="1"
                strokeDasharray="4 4"
                opacity="0.6"
              />
            ))}

            {/* Crosshair grid lines */}
            <line
              x1={center}
              y1={20}
              x2={center}
              y2={580}
              stroke="#1e293b"
              strokeWidth="1"
              strokeDasharray="2 6"
            />
            <line
              x1={20}
              y1={center}
              x2={580}
              y2={center}
              stroke="#1e293b"
              strokeWidth="1"
              strokeDasharray="2 6"
            />

            {/* Orbit circles for major planets */}
            {visibleObjects
              .filter((o) => o.type.toLowerCase() === 'planet')
              .map((p) => (
                <circle
                  key={`orbit-${p.id}`}
                  cx={center}
                  cy={center}
                  r={p.orbitRadius * scale}
                  fill="none"
                  stroke={`${p.colorHex}30`}
                  strokeWidth="1"
                />
              ))}

            {/* Route Line if calculated */}
            {routeResult &&
              (() => {
                const fromObj = objects.find((o) => o.id === fromId);
                const toObj = objects.find((o) => o.id === toId);
                if (!fromObj || !toObj) return null;
                const x1 = center + fromObj.relX * scale;
                const y1 = center + fromObj.relY * scale;
                const x2 = center + toObj.relX * scale;
                const y2 = center + toObj.relY * scale;
                return (
                  <line
                    x1={x1}
                    y1={y1}
                    x2={x2}
                    y2={y2}
                    stroke="#00F0FF"
                    strokeWidth="2"
                    strokeDasharray="6 4"
                    filter="url(#glow)"
                    className="animate-pulse"
                  />
                );
              })()}

            {/* Celestial Objects */}
            {visibleObjects.map((obj) => {
              const cx = center + obj.relX * scale;
              const cy = center + obj.relY * scale;
              const isSelected = obj.id.toLowerCase() === selectedId.toLowerCase();
              const isFrom = obj.id.toLowerCase() === fromId.toLowerCase();
              const isTo = obj.id.toLowerCase() === toId.toLowerCase();

              const r = Math.max(3, (obj.size / 2) * Math.min(1.5, Math.max(0.7, scale)));

              return (
                <g
                  key={obj.id}
                  className="cursor-pointer transition-transform"
                  onClick={(e) => {
                    e.stopPropagation();
                    setSelectedId(obj.id);
                  }}
                >
                  {/* Selection Ring */}
                  {isSelected && (
                    <circle
                      cx={cx}
                      cy={cy}
                      r={r + 6}
                      fill="none"
                      stroke="#00F0FF"
                      strokeWidth="1.5"
                      strokeDasharray="3 3"
                      filter="url(#glow)"
                    />
                  )}

                  {/* Start / Target Marker */}
                  {isFrom && (
                    <circle cx={cx} cy={cy} r={r + 9} fill="none" stroke="#10B981" strokeWidth="1.5" />
                  )}
                  {isTo && (
                    <circle cx={cx} cy={cy} r={r + 9} fill="none" stroke="#F43F5E" strokeWidth="1.5" />
                  )}

                  {/* Object Body */}
                  <circle
                    cx={cx}
                    cy={cy}
                    r={r}
                    fill={obj.colorHex}
                    filter={obj.type.toLowerCase() === 'star' ? 'url(#glow)' : undefined}
                    opacity="0.9"
                  />

                  {/* Label (displayed for planets, stars, and selected) */}
                  {(isSelected ||
                    obj.type.toLowerCase() === 'planet' ||
                    obj.type.toLowerCase() === 'star' ||
                    zoom >= 1.4) && (
                    <text
                      x={cx}
                      y={cy + r + 11}
                      textAnchor="middle"
                      fill={isSelected ? '#00F0FF' : '#94A3B8'}
                      fontSize={isSelected ? '11px' : '9px'}
                      fontFamily="monospace"
                      fontWeight={isSelected ? 'bold' : 'normal'}
                      pointerEvents="none"
                    >
                      {obj.name}
                    </text>
                  )}
                </g>
              );
            })}
          </svg>

          {/* Quick HUD Overlay at bottom left of canvas */}
          <div className="absolute bottom-3 left-3 sc-glass px-3 py-1.5 rounded border border-slate-800 text-[11px] font-mono text-slate-400 pointer-events-none">
            <span>System: <strong className="text-cyan-400">{system}</strong></span>
            <span className="mx-2">·</span>
            <span>Objekte: <strong className="text-slate-200">{visibleObjects.length}</strong></span>
            <span className="mx-2">·</span>
            <span>Zoom: <strong className="text-slate-200">{Math.round(zoom * 100)}%</strong></span>
          </div>
        </div>

        {/* Right Sidebar: Inspector Card & Quantum Route Planner */}
        <div className="flex flex-col space-y-4 overflow-y-auto pr-1">
          {/* Object Inspector Card */}
          {selectedObject ? (
            <div className="sc-glass rounded-lg p-4 border border-slate-800 flex flex-col justify-between sc-hud-corner">
              <div>
                <div className="flex items-start justify-between">
                  <div>
                    <span className="text-[10px] font-mono text-slate-500 uppercase tracking-wider">
                      {selectedObject.system} · {selectedObject.type}
                    </span>
                    <h2 className="text-lg font-bold text-slate-100 flex items-center gap-2 mt-0.5">
                      <span
                        className="w-3 h-3 rounded-full shrink-0"
                        style={{ backgroundColor: selectedObject.colorHex }}
                      />
                      {selectedObject.name}
                    </h2>
                  </div>

                  <span
                    className={`px-2 py-0.5 text-[10px] font-semibold rounded border ${
                      selectedObject.securityLevel?.toLowerCase() === 'high'
                        ? 'bg-emerald-500/20 text-emerald-300 border-emerald-500/40'
                        : selectedObject.securityLevel?.toLowerCase() === 'lawless'
                        ? 'bg-rose-500/20 text-rose-300 border-rose-500/40'
                        : 'bg-amber-500/20 text-amber-300 border-amber-500/40'
                    }`}
                  >
                    {selectedObject.securityLevel || 'Medium'}
                  </span>
                </div>

                {/* Description */}
                <p className="text-xs text-slate-300 mt-2.5 leading-relaxed">
                  {selectedObject.description}
                </p>

                {/* Attributes Grid */}
                <div className="grid grid-cols-2 gap-2 mt-3 text-xs font-mono">
                  <div className="bg-slate-900/60 p-2 rounded border border-slate-800">
                    <span className="text-[10px] text-slate-500 block">JURISDIKTION</span>
                    <span className="text-slate-200 font-semibold">{selectedObject.jurisdiction || 'UEE'}</span>
                  </div>
                  <div className="bg-slate-900/60 p-2 rounded border border-slate-800">
                    <span className="text-[10px] text-slate-500 block">SCHUTZZONE</span>
                    <span className={selectedObject.hasArmistice ? 'text-emerald-400' : 'text-rose-400'}>
                      {selectedObject.hasArmistice ? '🟢 Waffenruhe' : '🔴 Waffen scharf'}
                    </span>
                  </div>
                </div>

                {/* Specialization & Resources */}
                {selectedObject.specialization && (
                  <div className="mt-2 text-xs bg-slate-900/60 p-2 rounded border border-slate-800 flex items-center gap-1.5">
                    <Sparkles className="w-3.5 h-3.5 text-amber-400 shrink-0" />
                    <span className="text-slate-300">{selectedObject.specialization}</span>
                  </div>
                )}

                {selectedObject.resources && (
                  <div className="mt-2 text-xs bg-slate-900/60 p-2 rounded border border-slate-800">
                    <span className="text-[10px] text-slate-500 block font-mono">ROHSTOFFE & ERZE</span>
                    <span className="text-cyan-300 font-mono text-[11px]">{selectedObject.resources}</span>
                  </div>
                )}
              </div>

              {/* Set as Route Point Buttons */}
              <div className="mt-4 pt-3 border-t border-slate-800/80 flex items-center gap-2">
                <button
                  onClick={() => setFromId(selectedObject.id)}
                  className="flex-1 py-1.5 text-xs font-semibold rounded bg-emerald-950/40 border border-emerald-500/30 text-emerald-300 hover:bg-emerald-900/40 transition cursor-pointer text-center"
                >
                  Als Startpunkt
                </button>
                <button
                  onClick={() => setToId(selectedObject.id)}
                  className="flex-1 py-1.5 text-xs font-semibold rounded bg-rose-950/40 border border-rose-500/30 text-rose-300 hover:bg-rose-900/40 transition cursor-pointer text-center"
                >
                  Als Zielpunkt
                </button>
              </div>
            </div>
          ) : (
            <div className="sc-glass rounded-lg p-6 border border-slate-800 text-center text-xs text-slate-500">
              Klicke auf ein Objekt im Canvas zur Inspektion.
            </div>
          )}

          {/* Quantum Travel Route Planner */}
          <div className="sc-glass rounded-lg p-4 border border-slate-800 sc-hud-corner space-y-3">
            <div className="flex items-center gap-2">
              <Navigation className="w-4 h-4 text-cyan-400" />
              <h3 className="text-xs font-bold text-slate-200 uppercase tracking-wider font-mono">
                Quantum Routenplaner
              </h3>
            </div>

            {/* From & To Selectors */}
            <div className="space-y-2 text-xs">
              <div>
                <label className="text-[10px] font-mono text-slate-500 block mb-1">STARTPUNKT</label>
                <select
                  value={fromId}
                  onChange={(e) => setFromId(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-800 rounded p-1.5 text-xs text-slate-200 focus:outline-none focus:border-cyan-500 cursor-pointer font-mono"
                >
                  {objects.map((o) => (
                    <option key={`from-${o.id}`} value={o.id}>
                      {o.name} ({o.system})
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="text-[10px] font-mono text-slate-500 block mb-1">ZIELPUNKT</label>
                <select
                  value={toId}
                  onChange={(e) => setToId(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-800 rounded p-1.5 text-xs text-slate-200 focus:outline-none focus:border-cyan-500 cursor-pointer font-mono"
                >
                  {objects.map((o) => (
                    <option key={`to-${o.id}`} value={o.id}>
                      {o.name} ({o.system})
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="text-[10px] font-mono text-slate-500 block mb-1">QUANTUM DRIVE</label>
                <select
                  value={selectedDrive}
                  onChange={(e) => setSelectedDrive(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-800 rounded p-1.5 text-xs text-slate-200 focus:outline-none focus:border-cyan-500 cursor-pointer font-mono"
                >
                  {drives.map((d) => (
                    <option key={d.name} value={d.name}>
                      {d.displayText}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            {/* Calculation Output */}
            {routeResult && (
              <div className="mt-3 pt-3 border-t border-slate-800/80 space-y-2 font-mono">
                <div className="flex justify-between items-center text-xs">
                  <span className="text-slate-500">DISTANZ:</span>
                  <span className="text-cyan-300 font-bold">
                    {routeResult.distGm} GM <span className="text-slate-500 font-normal">({routeResult.distKm.toLocaleString('de-DE')} km)</span>
                  </span>
                </div>

                <div className="flex justify-between items-center text-xs">
                  <span className="text-slate-500">REISEDAUER:</span>
                  <span className="text-emerald-400 font-bold text-sm">{routeResult.flightTimeFormatted}</span>
                </div>

                <div className="flex justify-between items-center text-[11px]">
                  <span className="text-slate-500">STATUS:</span>
                  <span className="text-amber-400">Direkter Quantum-Vektor</span>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
