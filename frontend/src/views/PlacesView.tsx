import React, { useEffect, useState, useMemo } from 'react';
import {
  bridge,
  PlaceItemDto,
  UserPoiDto,
  CopiedLocationReading,
  ExecHangarSnapshotDto,
} from '../services/photinoBridge';
import {
  Compass,
  Search,
  Copy,
  Check,
  MapPin,
  Plus,
  Trash2,
  Navigation,
  Crosshair,
  Radio,
  ShieldAlert,
  Shield,
  RotateCcw,
  Zap,
} from 'lucide-react';
import { useI18n } from '../i18n';

export interface PlacesViewProps {
  initialSearch?: string;
}

export const PlacesView: React.FC<PlacesViewProps> = ({ initialSearch }) => {
  const { t, locale } = useI18n();
  const [activeTab, setActiveTab] = useState<'starmap' | 'pois' | 'contested'>('pois');
  const [places, setPlaces] = useState<PlaceItemDto[]>([]);
  const [userPois, setUserPois] = useState<UserPoiDto[]>([]);
  const [lastCopiedLoc, setLastCopiedLoc] = useState<CopiedLocationReading | null>(null);
  const [execHangar, setExecHangar] = useState<ExecHangarSnapshotDto | null>(null);
  const [search, setSearch] = useState<string>(initialSearch || '');
  const [systemFilter, setSystemFilter] = useState<string>('all');
  const [typeFilter, setTypeFilter] = useState<string>('all');
  const [poiCategoryFilter, setPoiCategoryFilter] = useState<string>('all');
  const [copiedId, setCopiedId] = useState<string | number | null>(null);

  // New POI modal
  const [showAddModal, setShowAddModal] = useState<boolean>(false);
  const [newPoiName, setNewPoiName] = useState<string>('');
  const [newPoiSystem, setNewPoiSystem] = useState<string>('Stanton');
  const [newPoiBody, setNewPoiBody] = useState<string>('');
  const [newPoiCategory, setNewPoiCategory] = useState<string>('Mining');
  const [newPoiNotes, setNewPoiNotes] = useState<string>('');
  const [newPoiX, setNewPoiX] = useState<string>('');
  const [newPoiY, setNewPoiY] = useState<string>('');
  const [newPoiZ, setNewPoiZ] = useState<string>('');

  const fetchPlaces = async () => {
    try {
      const res = await bridge.sendRequest<PlaceItemDto[]>('get_places');
      setPlaces(res || []);
    } catch (err) {
      console.error('Failed to load places:', err);
    }
  };

  const fetchUserPois = async () => {
    try {
      const res = await bridge.sendRequest<UserPoiDto[]>('get_user_pois');
      setUserPois(res || []);
    } catch (err) {
      console.error('Failed to load user POIs:', err);
    }
  };

  const fetchLastCopied = async () => {
    try {
      const res = await bridge.sendRequest<CopiedLocationReading | null>('get_last_copied_location');
      if (res) setLastCopiedLoc(res);
    } catch { }
  };

  const fetchExecHangar = async () => {
    try {
      const res = await bridge.sendRequest<ExecHangarSnapshotDto>('get_exec_hangar_status');
      if (res) setExecHangar(res);
    } catch (err) {
      console.error('Failed to load exec hangar status:', err);
    }
  };

  const handleReanchor = async () => {
    if (!window.confirm('Executive Hangar jetzt auf diesen Moment neu kalibrieren?\n\nKlicke nur auf Bestätigen im exakten Moment, in dem der Hangar im Spiel öffnet!')) return;
    try {
      const res = await bridge.sendRequest<ExecHangarSnapshotDto>('reanchor_exec_hangar');
      if (res) setExecHangar(res);
    } catch (err) {
      console.error('Failed to reanchor:', err);
    }
  };

  const handleResetAnchor = async () => {
    try {
      const res = await bridge.sendRequest<ExecHangarSnapshotDto>('reset_exec_hangar_anchor');
      if (res) setExecHangar(res);
    } catch (err) {
      console.error('Failed to reset anchor:', err);
    }
  };

  const formatClockTime = (iso: string) => {
    try {
      const d = new Date(iso);
      return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    } catch {
      return iso;
    }
  };

  useEffect(() => {
    fetchPlaces();
    fetchUserPois();
    fetchLastCopied();
    fetchExecHangar();

    // Listen to live /showlocation clipboard events
    const unsub = bridge.on<CopiedLocationReading>('LOCATION_COPIED', (data: CopiedLocationReading) => {
      setLastCopiedLoc(data);
      fetchUserPois(); // Refresh distances to POIs
    });

    const unsubHangar = bridge.on<ExecHangarSnapshotDto>('EXEC_HANGAR_UPDATED', (data) => {
      if (data) setExecHangar(data);
    });

    const timer = setInterval(() => {
      setExecHangar((prev) => {
        if (!prev) return prev;
        const newSec = Math.max(0, prev.timeToTransitionSeconds - 1);
        const mins = Math.floor(newSec / 60);
        const secs = Math.floor(newSec % 60);
        const hrs = Math.floor(mins / 60);
        const remMins = mins % 60;
        const fmt = hrs >= 1
          ? `${hrs}h ${remMins.toString().padStart(2, '0')}m ${secs.toString().padStart(2, '0')}s`
          : `${remMins}m ${secs.toString().padStart(2, '0')}s`;
        return {
          ...prev,
          timeToTransitionSeconds: newSec,
          formattedCountdown: fmt,
        };
      });
    }, 1000);

    const handlePoiSavedEvent = () => {
      fetchUserPois();
    };
    window.addEventListener('user-poi-saved', handlePoiSavedEvent);

    return () => {
      unsub();
      unsubHangar();
      window.removeEventListener('user-poi-saved', handlePoiSavedEvent);
      clearInterval(timer);
    };
  }, []);

  useEffect(() => {
    if (initialSearch !== undefined) setSearch(initialSearch);
  }, [initialSearch]);

  const types = [
    { id: 'all', label: 'Alle Orte' },
    { id: 'Planet', label: '🪐 Planeten' },
    { id: 'Moon', label: '🌑 Monde' },
    { id: 'LandingZone', label: '🏙️ Landezonen' },
    { id: 'SpaceStation', label: '🛰️ Raumstationen' },
    { id: 'LagrangeStation', label: '⛽ Lagrange (L1-L5)' },
    { id: 'Outpost', label: '🏭 Außenposten' },
  ];

  const poiCategories = [
    { id: 'all', label: 'Alle Kategorien' },
    { id: 'Mining', label: '⛏️ Mining & Vorkommen' },
    { id: 'Salvage', label: '🧲 Salvage & Wracks' },
    { id: 'Bunker', label: '🛡️ Bunker & Außenposten' },
    { id: 'Secret', label: '🤫 Geheim & Drogenlabore' },
    { id: 'Trade', label: '📦 Handel & Terminals' },
    { id: 'Misc', label: '📍 Sonstige Wegpunkte' },
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
        return (
          p.name.toLowerCase().includes(query) ||
          p.parentBody?.toLowerCase().includes(query) ||
          p.description?.toLowerCase().includes(query) ||
          p.specialization?.toLowerCase().includes(query)
        );
      }
      return true;
    });
  }, [places, systemFilter, typeFilter, search]);

  const filteredPois = useMemo(() => {
    return userPois.filter((poi) => {
      if (systemFilter !== 'all' && poi.system.toLowerCase() !== systemFilter.toLowerCase()) {
        return false;
      }
      if (poiCategoryFilter !== 'all' && poi.category.toLowerCase() !== poiCategoryFilter.toLowerCase()) {
        return false;
      }
      if (search.trim()) {
        const query = search.toLowerCase();
        return (
          poi.name.toLowerCase().includes(query) ||
          poi.body.toLowerCase().includes(query) ||
          poi.notes.toLowerCase().includes(query)
        );
      }
      return true;
    });
  }, [userPois, systemFilter, poiCategoryFilter, search]);

  const handleCopyPlace = (place: PlaceItemDto) => {
    const text = `${place.name} · ${place.parentBody ? `${place.parentBody}, ` : ''}${place.system}`;
    navigator.clipboard.writeText(text).then(() => {
      setCopiedId(place.id);
      setTimeout(() => setCopiedId(null), 2000);
    });
  };

  const handleCopyPoiCoords = (poi: UserPoiDto) => {
    const text = poi.hasCoordinates
      ? `Coordinates: x:${poi.posX} y:${poi.posY} z:${poi.posZ}`
      : `${poi.name} (${poi.system} - ${poi.body})`;
    navigator.clipboard.writeText(text).then(() => {
      setCopiedId(poi.id);
      setTimeout(() => setCopiedId(null), 2000);
    });
  };

  const openAddWithCurrentLocation = () => {
    if (lastCopiedLoc) {
      setNewPoiX(lastCopiedLoc.x.toString());
      setNewPoiY(lastCopiedLoc.y.toString());
      setNewPoiZ(lastCopiedLoc.z.toString());
      setNewPoiSystem(lastCopiedLoc.detectedSystem || 'Stanton');
    }
    setNewPoiName('');
    setNewPoiBody('');
    setNewPoiNotes('');
    setNewPoiCategory('Mining');
    setShowAddModal(true);
  };

  const handleSaveNewPoi = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newPoiName.trim()) return;

    const px = parseFloat(newPoiX);
    const py = parseFloat(newPoiY);
    const pz = parseFloat(newPoiZ);

    const payload: Partial<UserPoiDto> = {
      name: newPoiName.trim(),
      system: newPoiSystem,
      body: newPoiBody.trim(),
      category: newPoiCategory,
      notes: newPoiNotes.trim(),
      posX: !isNaN(px) ? px : null,
      posY: !isNaN(py) ? py : null,
      posZ: !isNaN(pz) ? pz : null,
      color:
        newPoiCategory === 'Mining'
          ? '#F59E0B'
          : newPoiCategory === 'Salvage'
          ? '#10B981'
          : newPoiCategory === 'Bunker'
          ? '#EF4444'
          : newPoiCategory === 'Secret'
          ? '#8B5CF6'
          : '#06B6D4',
    };

    try {
      await bridge.sendRequest('save_user_poi', payload);
      setShowAddModal(false);
      fetchUserPois();
    } catch (err) {
      console.error('Failed to save POI:', err);
    }
  };

  const handleDeletePoi = async (id: number) => {
    if (!window.confirm(locale === 'en' ? 'Are you sure you want to delete this POI?' : 'Diesen POI wirklich löschen?')) return;
    try {
      await bridge.sendRequest('delete_user_poi', { id });
      fetchUserPois();
    } catch (err) {
      console.error('Failed to delete POI:', err);
    }
  };

  return (
    <div className="flex flex-col min-h-full space-y-4">
      {/* Top Banner: Auto-Clipboard Location Indicator */}
      <div className="sc-glass rounded-lg p-3.5 border border-cyan-500/30 bg-cyan-950/20 sc-hud-corner flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <div className="p-2 rounded-lg bg-cyan-500/10 border border-cyan-500/30 text-cyan-400 shrink-0 animate-pulse">
            <Radio className="w-5 h-5" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <span className="text-xs font-mono font-bold uppercase tracking-wider text-cyan-300">
                {t('places.autoWatcher')}
              </span>
              <span className="text-[10px] px-2 py-0.5 rounded-full bg-emerald-500/20 text-emerald-300 border border-emerald-500/40">
                {t('places.active')}
              </span>
            </div>
            {lastCopiedLoc ? (
              <div className="text-xs font-mono text-slate-300 mt-1 flex flex-wrap items-center gap-2">
                <span>System: <strong className="text-amber-300">{lastCopiedLoc.detectedSystem}</strong></span>
                <span className="text-slate-500">|</span>
                <span>X: <span className="text-cyan-400">{lastCopiedLoc.x.toLocaleString('de-DE')}</span></span>
                <span>Y: <span className="text-cyan-400">{lastCopiedLoc.y.toLocaleString('de-DE')}</span></span>
                <span>Z: <span className="text-cyan-400">{lastCopiedLoc.z.toLocaleString('de-DE')}</span></span>
                {lastCopiedLoc.nearestPois && lastCopiedLoc.nearestPois.length > 0 && (
                  <>
                    <span className="text-slate-500">|</span>
                    <span className="text-emerald-400 flex items-center gap-1">
                      <Crosshair className="w-3 h-3" />
                      Nächster POI: <strong>{lastCopiedLoc.nearestPois[0].name}</strong> ({lastCopiedLoc.nearestPois[0].formattedDistance})
                    </span>
                  </>
                )}
              </div>
            ) : (
              <div className="text-[11px] text-slate-400 mt-0.5">
                Tippe im Spiel-Chat <code className="bg-slate-900 px-1.5 py-0.5 rounded text-cyan-300 border border-slate-800">/showlocation</code> — SCLogMate liest deine GPS-Koordinaten automatisch aus der Zwischenablage.
              </div>
            )}
          </div>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={openAddWithCurrentLocation}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded bg-cyan-600 hover:bg-cyan-500 text-white text-xs font-mono font-semibold transition cursor-pointer shadow-[0_0_12px_rgba(0,240,255,0.3)]"
          >
            <Plus className="w-4 h-4" />
            {locale === 'en' ? 'Pin as POI' : 'Als POI pinnen'}
          </button>
        </div>
      </div>

      {/* Navigation Tabs */}
      <div className="flex items-center justify-between border-b border-slate-800 pb-2">
        <div className="flex items-center gap-2">
          <button
            onClick={() => setActiveTab('pois')}
            className={`flex items-center gap-2 px-4 py-2 text-xs font-mono font-semibold rounded-t cursor-pointer transition ${
              activeTab === 'pois'
                ? 'bg-slate-800 text-cyan-300 border-b-2 border-cyan-400 shadow-[0_2px_8px_rgba(0,240,255,0.15)]'
                : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            <MapPin className="w-4 h-4 text-amber-400" />
            {locale === 'en' ? 'Pinned POIs & GPS Waypoints' : 'Gepinnte POIs & GPS-Wegpunkte'} ({userPois.length})
          </button>
          <button
            onClick={() => setActiveTab('starmap')}
            className={`flex items-center gap-2 px-4 py-2 text-xs font-mono font-semibold rounded-t cursor-pointer transition ${
              activeTab === 'starmap'
                ? 'bg-slate-800 text-cyan-300 border-b-2 border-cyan-400 shadow-[0_2px_8px_rgba(0,240,255,0.15)]'
                : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            <Compass className="w-4 h-4 text-cyan-400" />
            {locale === 'en' ? 'Starmap Places & Stations' : 'Starmap Orte & Stationen'} ({places.length})
          </button>
          <button
            onClick={() => setActiveTab('contested')}
            className={`flex items-center gap-2 px-4 py-2 text-xs font-mono font-semibold rounded-t cursor-pointer transition ${
              activeTab === 'contested'
                ? 'bg-slate-800 text-rose-300 border-b-2 border-rose-400 shadow-[0_2px_8px_rgba(244,63,94,0.2)]'
                : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            <ShieldAlert className="w-4 h-4 text-rose-400" />
            <span>Contested Zones & Exec Hangar</span>
            {execHangar && (
              <span
                className={`ml-1 px-1.5 py-0.2 rounded text-[10px] font-mono font-bold ${
                  execHangar.isOpen
                    ? 'bg-emerald-950/80 text-emerald-300 border border-emerald-500/50 shadow-[0_0_6px_#34d399]'
                    : 'bg-slate-900 text-slate-400 border border-slate-700'
                }`}
              >
                {execHangar.isOpen ? 'OPEN' : 'CLOSED'}
              </span>
            )}
          </button>
        </div>

        {/* Global System Filter */}
        <div className="flex items-center bg-slate-900/80 p-1 rounded-md border border-slate-800">
          {['all', 'Stanton', 'Pyro', 'Nyx'].map((sys) => (
            <button
              key={sys}
              onClick={() => setSystemFilter(sys)}
              className={`px-3 py-1 text-[11px] font-semibold rounded cursor-pointer transition ${
                systemFilter === sys
                  ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              {sys === 'all' ? (locale === 'en' ? 'All Systems' : 'Alle Systeme') : sys}
            </button>
          ))}
        </div>
      </div>

      {/* Filter & Search Bar */}
      <div className="sc-glass rounded-lg p-3 border border-slate-800 flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          {activeTab === 'pois' ? (
            <select
              value={poiCategoryFilter}
              onChange={(e) => setPoiCategoryFilter(e.target.value)}
              className="bg-slate-900 border border-slate-800 rounded px-2.5 py-1.5 text-xs text-slate-200 focus:outline-none focus:border-cyan-500 cursor-pointer font-mono"
            >
              {poiCategories.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.label}
                </option>
              ))}
            </select>
          ) : (
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
          )}
        </div>

        <div className="relative">
          <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-500" />
          <input
            type="text"
            placeholder={activeTab === 'pois' ? 'POI Name, Notiz, Himmelskörper filtern...' : 'Ort oder Station filtern...'}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="bg-slate-900 border border-slate-800 rounded pl-8 pr-3 py-1.5 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-cyan-500 w-64 lg:w-80 font-mono"
          />
        </div>
      </div>

      {/* TAB 1: PINNED POIs & GPS TRACKER */}
      {activeTab === 'pois' && (
        <div className="flex-1 overflow-y-auto pr-1">
          {filteredPois.length === 0 ? (
            <div className="sc-glass rounded-lg p-12 border border-slate-800 text-center flex flex-col items-center justify-center space-y-3">
              <MapPin className="w-12 h-12 text-amber-500/40" />
              <div className="text-slate-200 font-medium">Noch keine POIs gepinnt</div>
              <p className="text-xs text-slate-400 max-w-md">
                Kopiere im Spiel per <code className="text-cyan-300">/showlocation</code> deine Koordinaten oder klicke auf &quot;Als POI pinnen&quot;, um Bunkereingänge, Mining-Fundorte oder geheime Treffpunkte dauerhaft zu speichern.
              </p>
              <button
                onClick={openAddWithCurrentLocation}
                className="px-4 py-2 rounded bg-cyan-600 hover:bg-cyan-500 text-white text-xs font-mono font-semibold transition cursor-pointer shadow-[0_0_12px_rgba(0,240,255,0.3)]"
              >
                ＋ Ersten POI anlegen
              </button>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3">
              {filteredPois.map((poi) => {
                const isCopied = copiedId === poi.id;
                return (
                  <div
                    key={poi.id}
                    className="sc-glass rounded-lg p-3.5 border border-slate-800 hover:border-amber-500/40 transition-all duration-200 flex flex-col justify-between group sc-hud-corner"
                  >
                    <div>
                      {/* Header */}
                      <div className="flex items-start justify-between gap-2">
                        <div className="flex items-center gap-2.5">
                          <span
                            className="text-lg p-1.5 rounded-lg bg-slate-900 border"
                            style={{ borderColor: `${poi.color}40`, color: poi.color }}
                          >
                            <MapPin className="w-5 h-5" />
                          </span>
                          <div>
                            <h3 className="text-sm font-bold text-slate-100 group-hover:text-amber-300 transition-colors">
                              {poi.name}
                            </h3>
                            <div className="text-[10px] font-mono text-slate-400">
                              {poi.system} {poi.body ? `· ${poi.body}` : ''}
                            </div>
                          </div>
                        </div>

                        <span
                          className="px-2 py-0.5 text-[9px] font-semibold rounded border uppercase tracking-wider shrink-0"
                          style={{
                            backgroundColor: `${poi.color}15`,
                            color: poi.color,
                            borderColor: `${poi.color}30`,
                          }}
                        >
                          {poi.category}
                        </span>
                      </div>

                      {/* Notes / Description */}
                      {poi.notes && (
                        <p className="text-xs text-slate-300 mt-2 leading-relaxed bg-slate-900/60 p-2 rounded border border-slate-800/80">
                          {poi.notes}
                        </p>
                      )}

                      {/* Coordinates Pill */}
                      {poi.hasCoordinates && (
                        <div className="mt-2.5 flex items-center justify-between text-[11px] font-mono bg-slate-950/80 p-1.5 rounded border border-slate-800">
                          <span className="text-slate-400 truncate max-w-[200px]" title={poi.coordinatesFormatted}>
                            {poi.coordinatesFormatted}
                          </span>
                          {poi.distanceFormatted && (
                            <span className="text-emerald-400 font-bold shrink-0 flex items-center gap-1">
                              <Navigation className="w-3 h-3" />
                              {poi.distanceFormatted}
                            </span>
                          )}
                        </div>
                      )}
                    </div>

                    {/* Footer Actions */}
                    <div className="mt-3 pt-2.5 border-t border-slate-800/80 flex items-center justify-between text-[11px] font-mono">
                      <span className="text-[10px] text-slate-500">
                        {poi.createdAt}
                      </span>

                      <div className="flex items-center gap-1.5">
                        <button
                          onClick={() => handleCopyPoiCoords(poi)}
                          title="Koordinaten als /showlocation kopieren"
                          className="flex items-center gap-1 px-2 py-1 rounded bg-slate-900 border border-slate-800 text-slate-400 hover:text-cyan-300 hover:border-cyan-500/30 transition cursor-pointer"
                        >
                          {isCopied ? <Check className="w-3 h-3 text-emerald-400" /> : <Copy className="w-3 h-3" />}
                          <span className="text-[10px]">{isCopied ? 'Kopiert' : 'Kopieren'}</span>
                        </button>

                        <button
                          onClick={() => handleDeletePoi(poi.id)}
                          title="POI löschen"
                          className="p-1 rounded bg-slate-900 border border-slate-800 text-slate-500 hover:text-rose-400 hover:border-rose-500/30 transition cursor-pointer"
                        >
                          <Trash2 className="w-3.5 h-3.5" />
                        </button>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}

      {/* TAB 2: STARMAP PLACES & STATIONS */}
      {activeTab === 'starmap' && (
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

                      <p className="text-xs text-slate-400 mt-2.5 leading-relaxed line-clamp-2">
                        {p.description}
                      </p>

                      {p.specialization && (
                        <div className="mt-2.5 text-[11px] font-mono text-cyan-300 bg-cyan-950/20 border border-cyan-500/20 px-2 py-1 rounded">
                          {p.specialization}
                        </div>
                      )}
                    </div>

                    <div className="mt-3 pt-2.5 border-t border-slate-800/80 flex items-center justify-between text-[11px] font-mono">
                      <span className={p.hasArmistice ? 'text-emerald-400' : 'text-rose-400'}>
                        {p.hasArmistice ? '🟢 Waffenruhe' : '🔴 Waffen scharf'}
                      </span>

                      <button
                        onClick={() => handleCopyPlace(p)}
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
      )}

      {/* ══ TAB 3: CONTESTED ZONES & EXECUTIVE HANGAR (PYRO) ══ */}
      {activeTab === 'contested' && (
        <div className="flex-1 flex flex-col space-y-4 font-mono">
          {/* Executive Hangar Live Card */}
          <div className="sc-glass rounded-lg p-4 border border-rose-900/50 bg-[#040814]/90 sc-hud-corner space-y-4 shadow-[0_0_20px_rgba(244,63,94,0.1)]">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="flex items-center gap-2.5">
                <div className={`p-2 rounded-lg border ${
                  execHangar?.isOpen
                    ? 'bg-emerald-500/10 border-emerald-500/40 text-emerald-400 animate-pulse'
                    : 'bg-cyan-500/10 border-cyan-500/30 text-cyan-400'
                }`}>
                  <ShieldAlert className="w-5 h-5" />
                </div>
                <div>
                  <div className="flex items-center gap-2">
                    <h2 className="text-sm font-bold text-white uppercase tracking-wider">
                      Pyro Executive Hangar (PYAM-EXHANG-0-1)
                    </h2>
                    {execHangar && (
                      <span className={`px-2 py-0.5 rounded text-[10px] font-bold border uppercase ${
                        execHangar.isOpen
                          ? 'bg-emerald-950/80 text-emerald-300 border-emerald-500/60 shadow-[0_0_10px_#34d399]'
                          : 'bg-slate-900 text-slate-400 border-slate-700'
                      }`}>
                        {execHangar.isOpen ? 'Hangar Geöffnet (OPEN)' : 'Hangar Geschlossen (CLOSED)'}
                      </span>
                    )}
                  </div>
                  <div className="text-[11px] text-slate-400 mt-0.5">
                    Global synchronisierter Pyro Contested Zone Zyklus · 65m Öffnung / 120m Schließung (185m Vollzyklus)
                  </div>
                </div>
              </div>

              {/* Re-Anchor & Reset Controls */}
              <div className="flex items-center gap-2">
                {execHangar?.isCustomAnchor && (
                  <button
                    onClick={handleResetAnchor}
                    className="flex items-center gap-1 px-2.5 py-1 rounded bg-slate-900 hover:bg-slate-800 text-slate-300 border border-slate-700 text-xs transition cursor-pointer"
                    title="Auf offizielle Patch-Kalibrierung zurücksetzen"
                  >
                    <RotateCcw className="w-3.5 h-3.5" />
                    Reset
                  </button>
                )}
                <button
                  onClick={handleReanchor}
                  className="flex items-center gap-1 px-3 py-1 rounded bg-rose-950/70 hover:bg-rose-900/90 text-rose-200 border border-rose-600/60 text-xs font-semibold transition cursor-pointer shadow-[0_0_8px_rgba(244,63,94,0.25)]"
                  title="Zyklus ab genau diesem Moment neu kalibrieren (nur bei sichtbarem Hangar-Öffnen anklicken!)"
                >
                  <Zap className="w-3.5 h-3.5 text-rose-400" />
                  Neu kalibrieren (Re-Anchor)
                </button>
              </div>
            </div>

            {/* Status-Ampel (5 Phasen-Lichter) & Countdown */}
            {execHangar && (
              <div className="grid grid-cols-1 md:grid-cols-12 gap-3 pt-3 border-t border-cyan-950/80 items-center">
                {/* 5 Phasen-Lichter */}
                <div className="md:col-span-5 p-3 rounded bg-[#02050c] border border-cyan-950/80 space-y-2">
                  <div className="flex items-center justify-between text-[11px]">
                    <span className="text-slate-500 uppercase font-semibold">Phasen-Lichter (In-Game Ampel)</span>
                    <span className={execHangar.isOpen ? 'text-emerald-400 font-bold' : 'text-cyan-400 font-bold'}>
                      {execHangar.greensLit} von 5 aktiv
                    </span>
                  </div>

                  {/* 5 Indicator Dots */}
                  <div className="flex items-center gap-2.5 py-1">
                    {[0, 1, 2, 3, 4].map((i) => {
                      const lit = i < execHangar.greensLit;
                      return (
                        <div
                          key={i}
                          className={`w-5 h-5 rounded-full border transition-all duration-500 flex items-center justify-center ${
                            lit
                              ? execHangar.isOpen
                                ? 'bg-emerald-400 border-emerald-300 shadow-[0_0_10px_#34d399]'
                                : 'bg-cyan-400 border-cyan-300 shadow-[0_0_8px_#22d3ee]'
                              : 'bg-slate-950 border-slate-800'
                          }`}
                        >
                          {lit && <div className="w-1.5 h-1.5 rounded-full bg-white/80" />}
                        </div>
                      );
                    })}
                  </div>

                  <div className="text-[10px] text-slate-400">
                    {execHangar.isOpen
                      ? execHangar.isFinalActiveTail
                        ? '⚠️ Letzte 5 Minuten! Lichter erloschen, Hangar ist innen noch geöffnet.'
                        : 'Lichter erlöschen alle 12 Minuten. Ein Licht = ca. 12 Minuten verbleibend.'
                      : 'Lichter laden alle 24 Minuten auf. 4 Lichter = kurz vor Öffnung.'}
                  </div>
                </div>

                {/* Countdown & Zustand */}
                <div className="md:col-span-4 p-3 rounded bg-[#02050c] border border-cyan-950/80 space-y-1">
                  <div className="text-slate-500 text-[11px] uppercase">
                    {execHangar.isOpen ? 'Schließt in:' : 'Öffnet in:'}
                  </div>
                  <div className={`text-2xl font-bold tracking-wider ${
                    execHangar.isOpen ? 'text-emerald-300' : 'text-cyan-300'
                  }`}>
                    {execHangar.formattedCountdown}
                  </div>
                  <div className="text-[10px] text-slate-400">
                    {execHangar.isOpen
                      ? `Schließt um ca. ${formatClockTime(execHangar.nextCloseUtc)} Uhr`
                      : `Öffnet um ca. ${formatClockTime(execHangar.nextOpenUtc)} Uhr`}
                  </div>
                </div>

                {/* Nächste Öffnungen & Kalibrierung */}
                <div className="md:col-span-3 p-3 rounded bg-[#02050c] border border-cyan-950/80 space-y-1.5 text-xs">
                  <div className="text-slate-500 text-[10px] uppercase">Nächste Öffnungen:</div>
                  <div className="space-y-1">
                    {execHangar.upcomingOpensUtc.map((t, idx) => (
                      <div key={idx} className="flex items-center justify-between text-[11px]">
                        <span className="text-slate-400">Zyklus #{idx + 1}:</span>
                        <span className="text-slate-200 font-bold">{formatClockTime(t)} Uhr</span>
                      </div>
                    ))}
                  </div>
                  <div className="text-[9px] text-slate-500 pt-1 border-t border-slate-900">
                    {execHangar.isCustomAnchor ? 'Benutzer-Anker aktiv' : `Kalibrierung: ${execHangar.calibrationLabel}`}
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Tactical Contested Zones Directory Cards */}
          <div className="space-y-2">
            <div className="flex items-center justify-between text-xs text-slate-300">
              <span className="font-bold uppercase tracking-wider flex items-center gap-1.5">
                <Shield className="w-4 h-4 text-rose-400" />
                Pyro Contested Zones & Asteroiden-Objekte
              </span>
              <span className="text-[11px] text-slate-500">
                Gefährliche PvP/PvE-Hochsicherheitsgebiete in Pyro
              </span>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              {/* Card 1: PYAM-EXHANG-0-1 */}
              <div className="sc-glass rounded-lg p-3.5 border border-cyan-950/80 bg-[#040914]/90 sc-hud-corner flex flex-col justify-between space-y-3">
                <div>
                  <div className="flex items-start justify-between">
                    <div>
                      <span className="text-xs font-bold text-white uppercase">PYAM-EXHANG-0-1</span>
                      <div className="text-[10px] text-cyan-400">Exchange Asteroid Hangar · Pyro Gürtel</div>
                    </div>
                    <span className="px-2 py-0.5 rounded text-[9px] font-bold bg-rose-950/60 text-rose-300 border border-rose-800/60">
                      EXTREME THREAT
                    </span>
                  </div>

                  <p className="text-slate-400 text-xs mt-2 leading-relaxed">
                    Haupt-Executive-Hangar mit rotierender Blast-Tür. Beherbergt seltene Waffen, ballistische Munition und High-Tier Tresor-Loot. Stark umkämpft von Spielern und schwer bewaffneten Fraktionen.
                  </p>
                </div>

                <div className="pt-2 border-t border-cyan-950/80 flex items-center justify-between text-xs">
                  <span className="text-slate-500 text-[11px]">Quantum-Marker: PYAM-EXHANG</span>
                  <button
                    onClick={() => {
                      navigator.clipboard.writeText('PYAM-EXHANG-0-1');
                      setCopiedId('PYAM-EXHANG-0-1');
                      setTimeout(() => setCopiedId(null), 2000);
                    }}
                    className="flex items-center gap-1 px-2 py-1 rounded bg-slate-800 hover:bg-slate-700 text-cyan-300 text-[10px] transition cursor-pointer"
                  >
                    {copiedId === 'PYAM-EXHANG-0-1' ? <Check className="w-3 h-3 text-emerald-400" /> : <Copy className="w-3 h-3" />}
                    {copiedId === 'PYAM-EXHANG-0-1' ? 'Kopiert!' : 'Marker kopieren'}
                  </button>
                </div>
              </div>

              {/* Card 2: PYAM-SUPVISR-3-4/5 */}
              <div className="sc-glass rounded-lg p-3.5 border border-cyan-950/80 bg-[#040914]/90 sc-hud-corner flex flex-col justify-between space-y-3">
                <div>
                  <div className="flex items-start justify-between">
                    <div>
                      <span className="text-xs font-bold text-white uppercase">PYAM-SUPVISR-3-4 / 3-5</span>
                      <div className="text-[10px] text-cyan-400">Supervisor Station & Data Center · Pyro Gürtel</div>
                    </div>
                    <span className="px-2 py-0.5 rounded text-[9px] font-bold bg-amber-950/60 text-amber-300 border border-amber-800/60">
                      HIGH THREAT
                    </span>
                  </div>

                  <p className="text-slate-400 text-xs mt-2 leading-relaxed">
                    Aufsichts- und Datenknotenpunkt mit zwei verbundenen Asteroiden (3-4 & 3-5). Enthält Hacking-Konsolen, Überwachungsterminals und Fraktionsmissionen.
                  </p>
                </div>

                <div className="pt-2 border-t border-cyan-950/80 flex items-center justify-between text-xs">
                  <span className="text-slate-500 text-[11px]">Quantum-Marker: PYAM-SUPVISR</span>
                  <button
                    onClick={() => {
                      navigator.clipboard.writeText('PYAM-SUPVISR-3-4');
                      setCopiedId('PYAM-SUPVISR-3-4');
                      setTimeout(() => setCopiedId(null), 2000);
                    }}
                    className="flex items-center gap-1 px-2 py-1 rounded bg-slate-800 hover:bg-slate-700 text-cyan-300 text-[10px] transition cursor-pointer"
                  >
                    {copiedId === 'PYAM-SUPVISR-3-4' ? <Check className="w-3 h-3 text-emerald-400" /> : <Copy className="w-3 h-3" />}
                    {copiedId === 'PYAM-SUPVISR-3-4' ? 'Kopiert!' : 'Marker kopieren'}
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* MODAL: ADD NEW POI */}
      {showAddModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4">
          <div className="sc-glass border border-cyan-500/40 rounded-lg p-5 w-full max-w-lg shadow-[0_0_30px_rgba(0,240,255,0.2)]">
            <h2 className="text-base font-bold text-slate-100 font-mono uppercase tracking-wider mb-4 flex items-center gap-2">
              <MapPin className="w-5 h-5 text-amber-400" />
              Neuen POI / Wegpunkt anlegen
            </h2>

            <form onSubmit={handleSaveNewPoi} className="space-y-3 font-mono text-xs">
              <div>
                <label className="block text-slate-400 mb-1">Name des POI / Ortes *</label>
                <input
                  type="text"
                  required
                  placeholder="z.B. Quantainium Hotspot Alpha, Drogenlabor..."
                  value={newPoiName}
                  onChange={(e) => setNewPoiName(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-800 rounded p-2 text-slate-200 focus:border-cyan-500 focus:outline-none"
                />
              </div>

              <div className="grid grid-cols-3 gap-2">
                <div>
                  <label className="block text-slate-400 mb-1">System</label>
                  <select
                    value={newPoiSystem}
                    onChange={(e) => setNewPoiSystem(e.target.value)}
                    className="w-full bg-slate-900 border border-slate-800 rounded p-2 text-slate-200 focus:border-cyan-500 focus:outline-none"
                  >
                    <option value="Stanton">Stanton</option>
                    <option value="Pyro">Pyro</option>
                    <option value="Nyx">Nyx</option>
                  </select>
                </div>
                <div>
                  <label className="block text-slate-400 mb-1">Körper / Mond</label>
                  <input
                    type="text"
                    placeholder="z.B. Daymar, Lyria..."
                    value={newPoiBody}
                    onChange={(e) => setNewPoiBody(e.target.value)}
                    className="w-full bg-slate-900 border border-slate-800 rounded p-2 text-slate-200 focus:border-cyan-500 focus:outline-none"
                  />
                </div>
                <div>
                  <label className="block text-slate-400 mb-1">Kategorie</label>
                  <select
                    value={newPoiCategory}
                    onChange={(e) => setNewPoiCategory(e.target.value)}
                    className="w-full bg-slate-900 border border-slate-800 rounded p-2 text-slate-200 focus:border-cyan-500 focus:outline-none"
                  >
                    <option value="Mining">Mining</option>
                    <option value="Salvage">Salvage</option>
                    <option value="Bunker">Bunker</option>
                    <option value="Secret">Secret</option>
                    <option value="Trade">Trade</option>
                    <option value="Misc">Misc</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-slate-400 mb-1">Notizen / Beschreibung</label>
                <textarea
                  rows={2}
                  placeholder="Details, Erzvorkommen, Gegner..."
                  value={newPoiNotes}
                  onChange={(e) => setNewPoiNotes(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-800 rounded p-2 text-slate-200 focus:border-cyan-500 focus:outline-none"
                />
              </div>

              <div>
                <label className="block text-slate-400 mb-1">3D Koordinaten (/showlocation)</label>
                <div className="grid grid-cols-3 gap-2">
                  <input
                    type="text"
                    placeholder="X-Achse"
                    value={newPoiX}
                    onChange={(e) => setNewPoiX(e.target.value)}
                    className="bg-slate-900 border border-slate-800 rounded p-2 text-slate-200 focus:border-cyan-500 focus:outline-none"
                  />
                  <input
                    type="text"
                    placeholder="Y-Achse"
                    value={newPoiY}
                    onChange={(e) => setNewPoiY(e.target.value)}
                    className="bg-slate-900 border border-slate-800 rounded p-2 text-slate-200 focus:border-cyan-500 focus:outline-none"
                  />
                  <input
                    type="text"
                    placeholder="Z-Achse"
                    value={newPoiZ}
                    onChange={(e) => setNewPoiZ(e.target.value)}
                    className="bg-slate-900 border border-slate-800 rounded p-2 text-slate-200 focus:border-cyan-500 focus:outline-none"
                  />
                </div>
              </div>

              <div className="flex items-center justify-end gap-2 pt-3 border-t border-slate-800">
                <button
                  type="button"
                  onClick={() => setShowAddModal(false)}
                  className="px-4 py-2 rounded bg-slate-800 hover:bg-slate-700 text-slate-300 transition cursor-pointer"
                >
                  Abbrechen
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 rounded bg-cyan-600 hover:bg-cyan-500 text-white font-semibold transition cursor-pointer shadow-[0_0_12px_rgba(0,240,255,0.3)]"
                >
                  Speichern
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
