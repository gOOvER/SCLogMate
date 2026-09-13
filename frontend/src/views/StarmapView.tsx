import React, { useEffect, useState, useMemo, useRef } from 'react';
import {
  bridge,
  StarmapObjectDto,
  QuantumDriveDto,
  QuantumRouteResultDto,
  StarmapResponseDto,
  HudTelemetry,
} from '../services/photinoBridge';
import {
  Navigation,
  Sparkles,
  ZoomIn,
  ZoomOut,
  Maximize2,
  Crosshair,
  Search,
  Compass,
} from 'lucide-react';

interface StarmapViewProps {
  telemetry?: HudTelemetry;
}

export const StarmapView: React.FC<StarmapViewProps> = ({ telemetry }) => {
  const [system, setSystem] = useState<'Stanton' | 'Pyro' | 'Nyx'>('Stanton');
  const [objects, setObjects] = useState<StarmapObjectDto[]>([]);
  const [drives, setDrives] = useState<QuantumDriveDto[]>([]);
  const [selectedId, setSelectedId] = useState<string>('hurston');
  const [fromId, setFromId] = useState<string>('hurston');
  const [toId, setToId] = useState<string>('crusader');
  const [selectedDrive, setSelectedDrive] = useState<string>('Atlas');
  const [routeResult, setRouteResult] = useState<QuantumRouteResultDto | null>(null);

  // Search & Filter
  const [searchQuery, setSearchQuery] = useState<string>('');

  // Follow-Me Mode
  const [followMe, setFollowMe] = useState<boolean>(true);

  // Layer filters
  const [showStations, setShowStations] = useState<boolean>(true);
  const [showMoons, setShowMoons] = useState<boolean>(true);
  const [showLandingZones, setShowLandingZones] = useState<boolean>(true);

  // Zoom & Pan state
  const [zoom, setZoom] = useState<number>(1);
  const [pan, setPan] = useState<{ x: number; y: number }>({ x: 0, y: 0 });
  const [isDragging, setIsDragging] = useState<boolean>(false);
  const dragStartRef = useRef<{ x: number; y: number }>({ x: 0, y: 0 });
  const hasDraggedRef = useRef<boolean>(false);
  const canvasRef = useRef<HTMLDivElement | null>(null);

  // Center coordinate reference
  const center = 300;
  const scale = 0.65 * zoom;

  // Auto-switch system based on player telemetry if followMe is enabled
  useEffect(() => {
    if (!followMe || !telemetry?.locationSystem) return;
    const sys = telemetry.locationSystem.trim().toLowerCase();
    if (sys === 'pyro') {
      if (system !== 'Pyro') setSystem('Pyro');
    } else if (sys === 'nyx') {
      if (system !== 'Nyx') setSystem('Nyx');
    } else if (sys === 'stanton') {
      if (system !== 'Stanton') setSystem('Stanton');
    }
  }, [telemetry?.locationSystem, followMe]);

  // Non-passive wheel listener for smooth zoom-to-cursor
  useEffect(() => {
    const el = canvasRef.current;
    if (!el) return;

    const onWheel = (e: WheelEvent) => {
      e.preventDefault();
      const rect = el.getBoundingClientRect();
      const mouseX = e.clientX - rect.left;
      const mouseY = e.clientY - rect.top;

      const zoomFactor = e.deltaY < 0 ? 1.15 : 0.87;
      setZoom((prevZoom) => {
        const nextZoom = Math.min(3.5, Math.max(0.3, prevZoom * zoomFactor));
        // Keep point under cursor invariant
        setPan((prevPan) => {
          const ratio = nextZoom / prevZoom;
          const newX = mouseX - (mouseX - prevPan.x) * ratio;
          const newY = mouseY - (mouseY - prevPan.y) * ratio;
          return { x: newX, y: newY };
        });
        return nextZoom;
      });
    };

    el.addEventListener('wheel', onWheel, { passive: false });
    return () => {
      el.removeEventListener('wheel', onWheel);
    };
  }, []);

  const fetchStarmap = async (sysName: string) => {
    try {
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

  // Resolve player's current location object in the starmap
  const playerLocationObject = useMemo(() => {
    if (!telemetry || !objects.length) return null;

    // Match by telemetry.locationId first
    if (telemetry.locationId) {
      const byId = objects.find((o) => o.id.toLowerCase() === telemetry.locationId!.toLowerCase());
      if (byId) return byId;
    }

    // Match by exact or partial name
    if (telemetry.locationName && telemetry.locationName !== '—' && telemetry.locationName !== 'Unbekannt') {
      const cleanName = telemetry.locationName.split('·')[0].trim().toLowerCase();
      const byName = objects.find(
        (o) =>
          o.name.toLowerCase() === cleanName ||
          o.name.toLowerCase().includes(cleanName) ||
          cleanName.includes(o.name.toLowerCase())
      );
      if (byName) return byName;
    }

    // Fallback to parent body
    if (telemetry.locationBody && telemetry.locationBody !== '—') {
      const cleanBody = telemetry.locationBody.toLowerCase();
      const byBody = objects.find((o) => o.name.toLowerCase() === cleanBody || o.id.toLowerCase() === cleanBody);
      if (byBody) return byBody;
    }

    return null;
  }, [telemetry, objects]);

  // Resolve player's destination during quantum travel
  const travelTargetObject = useMemo(() => {
    if (!telemetry?.isTravelling || !objects.length) return null;

    if (telemetry.travellingToId) {
      const byId = objects.find((o) => o.id.toLowerCase() === telemetry.travellingToId!.toLowerCase());
      if (byId) return byId;
    }

    if (telemetry.travellingToName) {
      const cleanTarget = telemetry.travellingToName.toLowerCase();
      return objects.find(
        (o) =>
          o.name.toLowerCase() === cleanTarget ||
          o.name.toLowerCase().includes(cleanTarget) ||
          cleanTarget.includes(o.name.toLowerCase())
      ) || null;
    }

    return null;
  }, [telemetry, objects]);

  // Center view on player position
  const centerOnPlayer = () => {
    if (!playerLocationObject) return;
    const targetX = center + playerLocationObject.relX * scale;
    const targetY = center + playerLocationObject.relY * scale;
    setPan({
      x: 300 - targetX,
      y: 300 - targetY,
    });
  };

  // Center on player when followMe is engaged and location changes
  useEffect(() => {
    if (followMe && playerLocationObject) {
      centerOnPlayer();
    }
  }, [playerLocationObject?.id, followMe]);

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

  // Search matches
  const searchMatches = useMemo(() => {
    if (!searchQuery.trim()) return new Set<string>();
    const q = searchQuery.toLowerCase().trim();
    const set = new Set<string>();
    objects.forEach((o) => {
      if (
        o.name.toLowerCase().includes(q) ||
        o.id.toLowerCase().includes(q) ||
        o.specialization?.toLowerCase().includes(q) ||
        o.resources?.toLowerCase().includes(q)
      ) {
        set.add(o.id.toLowerCase());
      }
    });
    return set;
  }, [searchQuery, objects]);

  // Mouse pan handlers
  const handleMouseDown = (e: React.MouseEvent) => {
    setIsDragging(true);
    hasDraggedRef.current = false;
    dragStartRef.current = { x: e.clientX - pan.x, y: e.clientY - pan.y };
  };

  const handleMouseMove = (e: React.MouseEvent) => {
    if (!isDragging) return;
    const dx = Math.abs(e.clientX - (dragStartRef.current.x + pan.x));
    const dy = Math.abs(e.clientY - (dragStartRef.current.y + pan.y));
    if (dx > 3 || dy > 3) {
      hasDraggedRef.current = true;
    }
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

  return (
    <div className="flex flex-col min-h-full space-y-4 select-none">
      {/* Top Toolbar: System Selector & Quick Filters */}
      <div className="sc-glass rounded-lg p-3 border border-slate-800 flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center flex-wrap gap-2">
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

          {/* Follow-Me Toggle */}
          <button
            onClick={() => {
              const next = !followMe;
              setFollowMe(next);
              if (next && playerLocationObject) centerOnPlayer();
            }}
            title="Automatisches Zentrieren auf Spielerstandort"
            className={`px-2.5 py-1 text-xs font-mono rounded border flex items-center gap-1.5 transition cursor-pointer ${
              followMe
                ? 'bg-emerald-500/20 border-emerald-500/40 text-emerald-300 shadow-[0_0_10px_rgba(16,185,129,0.2)]'
                : 'bg-slate-900/60 border-slate-800 text-slate-500 hover:text-slate-300'
            }`}
          >
            <Crosshair className={`w-3.5 h-3.5 ${followMe ? 'text-emerald-400 animate-spin-slow' : ''}`} />
            <span>Follow Me</span>
          </button>

          {/* Quick Search */}
          <div className="relative">
            <Search className="w-3.5 h-3.5 text-slate-500 absolute left-2.5 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Ort / Station / Ressource..."
              className="bg-slate-900/80 border border-slate-800 text-xs text-slate-200 pl-8 pr-2.5 py-1 rounded w-44 focus:w-56 transition-all focus:outline-none focus:border-cyan-500 font-mono"
            />
          </div>

          {/* Layer toggles */}
          <div className="flex items-center gap-1.5 ml-1">
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
          {playerLocationObject && (
            <button
              onClick={centerOnPlayer}
              title="Auf aktuellen Spielerstandort springen"
              className="px-2.5 py-1 text-xs font-mono rounded bg-cyan-950/40 border border-cyan-500/30 text-cyan-300 hover:bg-cyan-900/40 transition cursor-pointer flex items-center gap-1.5"
            >
              <Compass className="w-3.5 h-3.5 text-cyan-400" />
              <span>Hier</span>
            </button>
          )}
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
              <filter id="here-glow" x="-50%" y="-50%" width="200%" height="200%">
                <feGaussianBlur stdDeviation="4" result="blur" />
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

            {/* Live Quantum Travel Vector (drawTravel) */}
            {telemetry?.isTravelling && playerLocationObject && travelTargetObject && (
              <g className="map-live-travel pointer-events-none">
                {(() => {
                  const x1 = center + playerLocationObject.relX * scale;
                  const y1 = center + playerLocationObject.relY * scale;
                  const x2 = center + travelTargetObject.relX * scale;
                  const y2 = center + travelTargetObject.relY * scale;
                  return (
                    <>
                      {/* Animated vector travel line */}
                      <line
                        x1={x1}
                        y1={y1}
                        x2={x2}
                        y2={y2}
                        stroke="#F59E0B"
                        strokeWidth="2"
                        strokeDasharray="8 5"
                        filter="url(#glow)"
                        className="animate-pulse"
                      >
                        <animate
                          attributeName="stroke-dashoffset"
                          values="26;0"
                          dur="0.85s"
                          repeatCount="indefinite"
                        />
                      </line>

                      {/* Expanding target rings */}
                      <circle
                        cx={x2}
                        cy={y2}
                        r="14"
                        fill="none"
                        stroke="#F59E0B"
                        strokeWidth="1.5"
                      >
                        <animate
                          attributeName="r"
                          values="8;24"
                          dur="1.6s"
                          repeatCount="indefinite"
                        />
                        <animate
                          attributeName="opacity"
                          values="0.9;0"
                          dur="1.6s"
                          repeatCount="indefinite"
                        />
                      </circle>
                    </>
                  );
                })()}
              </g>
            )}

            {/* Manual Route Line if calculated */}
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
              const isSearchMatch = searchMatches.has(obj.id.toLowerCase());

              const r = Math.max(3, (obj.size / 2) * Math.min(1.5, Math.max(0.7, scale)));

              return (
                <g
                  key={obj.id}
                  className="cursor-pointer transition-transform"
                  onClick={(e) => {
                    e.stopPropagation();
                    if (hasDraggedRef.current) return;
                    setSelectedId(obj.id);
                  }}
                >
                  {/* Search Match Halo */}
                  {isSearchMatch && (
                    <circle
                      cx={cx}
                      cy={cy}
                      r={r + 12}
                      fill="none"
                      stroke="#FACC15"
                      strokeWidth="2"
                      strokeDasharray="2 2"
                      filter="url(#glow)"
                    />
                  )}

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

                  {/* Label (displayed for planets, stars, search matches and selected) */}
                  {(isSelected ||
                    isSearchMatch ||
                    obj.type.toLowerCase() === 'planet' ||
                    obj.type.toLowerCase() === 'star' ||
                    zoom >= 1.4) && (
                    <text
                      x={cx}
                      y={cy + r + 11}
                      textAnchor="middle"
                      fill={isSelected ? '#00F0FF' : isSearchMatch ? '#FACC15' : '#94A3B8'}
                      fontSize={isSelected || isSearchMatch ? '11px' : '9px'}
                      fontFamily="monospace"
                      fontWeight={isSelected || isSearchMatch ? 'bold' : 'normal'}
                      pointerEvents="none"
                    >
                      {obj.name}
                    </text>
                  )}
                </g>
              );
            })}

            {/* LIVE "YOU ARE HERE" PLAYER MARKER (QuantumWake drawHere port) */}
            {playerLocationObject && (
              <g className="map-player-here pointer-events-none" filter="url(#here-glow)">
                {(() => {
                  const px = center + playerLocationObject.relX * scale;
                  const py = center + playerLocationObject.relY * scale;
                  const ringR = 15;

                  return (
                    <>
                      {/* Constant Steady Ring */}
                      <circle
                        cx={px}
                        cy={py}
                        r={ringR}
                        fill="none"
                        stroke="#10B981"
                        strokeWidth="1.8"
                      />

                      {/* Expanding Pulse Ring */}
                      <circle
                        cx={px}
                        cy={py}
                        r={ringR}
                        fill="none"
                        stroke="#10B981"
                        strokeWidth="1.2"
                      >
                        <animate
                          attributeName="r"
                          values={`${ringR};${ringR + 22}`}
                          dur="2.2s"
                          repeatCount="indefinite"
                        />
                        <animate
                          attributeName="opacity"
                          values="0.8;0"
                          dur="2.2s"
                          repeatCount="indefinite"
                        />
                      </circle>

                      {/* Precision Reticle Crosshair Ticks */}
                      <line
                        x1={px - ringR - 5}
                        y1={py}
                        x2={px + ringR + 5}
                        y2={py}
                        stroke="#10B981"
                        strokeWidth="1.2"
                        opacity="0.8"
                      />
                      <line
                        x1={px}
                        y1={py - ringR - 5}
                        x2={px}
                        y2={py + ringR + 5}
                        stroke="#10B981"
                        strokeWidth="1.2"
                        opacity="0.8"
                      />

                      {/* Center Green Core Dot */}
                      <circle
                        cx={px}
                        cy={py}
                        r="4"
                        fill="#10B981"
                      />

                      {/* Angled Leader Line to Label */}
                      <line
                        x1={px + ringR * 0.7}
                        y1={py - ringR * 0.7}
                        x2={px + ringR + 12}
                        y2={py - ringR - 12}
                        stroke="#10B981"
                        strokeWidth="1.2"
                      />

                      {/* "YOU ARE HERE" Callout Box */}
                      <rect
                        x={px + ringR + 10}
                        y={py - ringR - 26}
                        width="92"
                        height="16"
                        rx="3"
                        fill="#064E3B"
                        stroke="#10B981"
                        strokeWidth="1"
                        opacity="0.9"
                      />
                      <text
                        x={px + ringR + 14}
                        y={py - ringR - 14}
                        fill="#34D399"
                        fontSize="9.5px"
                        fontFamily="monospace"
                        fontWeight="bold"
                        letterSpacing="0.5px"
                      >
                        YOU ARE HERE
                      </text>
                    </>
                  );
                })()}
              </g>
            )}
          </svg>

          {/* Quick HUD Overlay at bottom left of canvas */}
          <div className="absolute bottom-3 left-3 sc-glass px-3 py-1.5 rounded border border-slate-800 text-[11px] font-mono text-slate-400 pointer-events-none flex items-center gap-2">
            <span>System: <strong className="text-cyan-400">{system}</strong></span>
            <span>·</span>
            <span>Objekte: <strong className="text-slate-200">{visibleObjects.length}</strong></span>
            <span>·</span>
            <span>Zoom: <strong className="text-slate-200">{Math.round(zoom * 100)}%</strong></span>
            {playerLocationObject && (
              <>
                <span>·</span>
                <span className="text-emerald-400 flex items-center gap-1 font-semibold">
                  <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" />
                  {playerLocationObject.name}
                </span>
              </>
            )}
            {telemetry?.isTravelling && (
              <>
                <span>·</span>
                <span className="text-amber-400 font-semibold animate-pulse">
                  ➔ QT nach {telemetry.travellingToName || 'Ziel'}
                </span>
              </>
            )}
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
