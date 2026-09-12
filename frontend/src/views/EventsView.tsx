import React, { useEffect, useMemo, useState } from 'react';
import { bridge, LogEventItem, SessionSummary, CombatAnalyticsDto, HudTelemetry } from '../services/photinoBridge';
import {
  Archive,
  BookOpen,
  Check,
  ChevronDown,
  ChevronUp,
  Code2,
  Coins,
  Copy,
  ExternalLink,
  Filter,
  MapPin,
  Rocket,
  Search,
  Swords,
  Target,
  Terminal,
  X,
  Skull,
  ShieldAlert,
  AlertOctagon,
  Activity,
} from 'lucide-react';
import { ContextMenu } from '../components/ContextMenu';

type SortColumn = 'timestamp' | 'category' | 'amount' | 'ship' | 'title';
type SortDirection = 'asc' | 'desc';

interface EventsViewProps {
  sessions?: SessionSummary[];
  initialEvents?: LogEventItem[];
}

export const EventsView: React.FC<EventsViewProps> = ({
  sessions = [],
  initialEvents = [],
}) => {
  const [events, setEvents] = useState<LogEventItem[]>(initialEvents);
  const [viewMode, setViewMode] = useState<'live' | 'archive' | 'combat'>('live');
  const [archiveSession, setArchiveSession] = useState<string>('__all__');
  const [combatData, setCombatData] = useState<CombatAnalyticsDto | null>(null);
  const [category, setCategory] = useState<string>('Alle');
  const [search, setSearch] = useState<string>('');
  const [selectedEvent, setSelectedEvent] = useState<LogEventItem | null>(null);
  const [limit, setLimit] = useState<number>(200);
  const [sortCol, setSortCol] = useState<SortColumn>('timestamp');
  const [sortDir, setSortDir] = useState<SortDirection>('desc');
  const [copiedField, setCopiedField] = useState<string | null>(null);
  const [contextMenu, setContextMenu] = useState<{ x: number; y: number; event: LogEventItem } | null>(null);

  const activeSession = viewMode === 'live' ? '__live__' : archiveSession;

  useEffect(() => {
    if (initialEvents && initialEvents.length > 0 && events.length === 0 && activeSession === '__live__') {
      setEvents(initialEvents);
    }
  }, [initialEvents]);

  const fetchCombatAnalytics = async (sessionTarget = activeSession) => {
    try {
      const res = await bridge.sendRequest<CombatAnalyticsDto>('get_combat_analytics', { session: sessionTarget });
      setCombatData(res);
    } catch (err) {
      console.error('Failed to load combat analytics:', err);
    }
  };

  useEffect(() => {
    if (viewMode === 'combat') {
      fetchCombatAnalytics(activeSession);
    }
  }, [viewMode, activeSession]);

  const fetchEvents = async (count = limit, sessionTarget = activeSession) => {
    try {
      const res = await bridge.sendRequest<LogEventItem[]>('get_events', {
        session: sessionTarget,
        category,
        search: search.trim() || undefined,
        limit: count,
      });
      setEvents(res);
      if (res.length > 0 && !selectedEvent) {
        setSelectedEvent(res[0]);
      }
    } catch (err) {
      console.error('Failed to load events:', err);
    }
  };

  useEffect(() => {
    fetchEvents(limit, activeSession);
  }, [category, activeSession]);

  // Live log subscription (active only in live stream mode)
  useEffect(() => {
    if (activeSession !== '__live__') return;

    const unbindLog = bridge.on<LogEventItem>('LOG_EVENT', (newEvent) => {
      setEvents((prev) => [newEvent, ...prev.slice(0, limit - 1)]);
    });

    const unbindLiveLoaded = bridge.on<LogEventItem[]>('LIVE_EVENTS_LOADED', (loadedEvents) => {
      if (Array.isArray(loadedEvents) && loadedEvents.length > 0) {
        setEvents(loadedEvents);
      } else {
        fetchEvents(limit, '__live__');
      }
    });

    const unbindHud = bridge.on<HudTelemetry>('HUD_UPDATE', () => {
      setEvents((prev) => {
        if (prev.length === 0) {
          fetchEvents(limit, '__live__');
        }
        return prev;
      });
    });

    return () => {
      unbindLog();
      unbindLiveLoaded();
      unbindHud();
    };
  }, [activeSession, limit]);

  // Keyboard navigation
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (!selectedEvent || events.length === 0) return;
      const idx = events.findIndex((ev) => ev.id === selectedEvent.id);
      if (idx === -1) return;

      if (e.key === 'ArrowDown') {
        e.preventDefault();
        const next = events[Math.min(idx + 1, events.length - 1)];
        setSelectedEvent(next);
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        const prev = events[Math.max(idx - 1, 0)];
        setSelectedEvent(prev);
      } else if (e.key === 'Escape') {
        setSelectedEvent(null);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [selectedEvent, events]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    fetchEvents();
  };

  const handleSort = (col: SortColumn) => {
    if (sortCol === col) {
      setSortDir(sortDir === 'asc' ? 'desc' : 'asc');
    } else {
      setSortCol(col);
      setSortDir('desc');
    }
  };

  const handleCopy = (text: string, fieldName: string) => {
    navigator.clipboard.writeText(text);
    setCopiedField(fieldName);
    setTimeout(() => setCopiedField(null), 2000);
  };

  const handleLoadMore = () => {
    const newLimit = limit + 150;
    setLimit(newLimit);
    fetchEvents(newLimit);
  };

  const formatNumber = (val?: number | null) => {
    if (val === undefined || val === null) return '';
    return new Intl.NumberFormat('de-DE').format(val);
  };

  // Sorted and filtered events
  const displayEvents = useMemo(() => {
    return [...events].sort((a, b) => {
      let cmp = 0;
      switch (sortCol) {
        case 'timestamp':
          cmp = (a.timestamp || '').localeCompare(b.timestamp || '');
          break;
        case 'category':
          cmp = (a.category || '').localeCompare(b.category || '');
          break;
        case 'amount':
          cmp = (a.amount || 0) - (b.amount || 0);
          break;
        case 'ship':
          cmp = (a.ship || '').localeCompare(b.ship || '');
          break;
        case 'title':
          cmp = (a.title || '').localeCompare(b.title || '');
          break;
      }
      return sortDir === 'asc' ? cmp : -cmp;
    });
  }, [events, sortCol, sortDir]);

  const getCategoryBadge = (cat: string) => {
    switch (cat) {
      case 'wallet':
        return (
          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-mono font-bold bg-emerald-950/60 text-emerald-300 border border-emerald-800/60 shrink-0">
            <Coins className="w-2.5 h-2.5 text-emerald-400" />
            <span>Finanzen</span>
          </span>
        );
      case 'combat':
        return (
          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-mono font-bold bg-rose-950/60 text-rose-300 border border-rose-800/60 shrink-0">
            <Swords className="w-2.5 h-2.5 text-rose-400" />
            <span>Kampf</span>
          </span>
        );
      case 'mission':
        return (
          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-mono font-bold bg-amber-950/60 text-amber-300 border border-amber-800/60 shrink-0">
            <Target className="w-2.5 h-2.5 text-amber-400" />
            <span>Auftrag</span>
          </span>
        );
      case 'ship':
        return (
          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-mono font-bold bg-sky-950/60 text-sky-300 border border-sky-800/60 shrink-0">
            <Rocket className="w-2.5 h-2.5 text-sky-400" />
            <span>Schiff</span>
          </span>
        );
      case 'location':
        return (
          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-mono font-bold bg-cyan-950/60 text-cyan-300 border border-cyan-800/60 shrink-0">
            <MapPin className="w-2.5 h-2.5 text-cyan-400" />
            <span>Ort</span>
          </span>
        );
      default:
        return (
          <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-mono font-medium bg-slate-900 text-slate-400 border border-slate-800 shrink-0">
            <Terminal className="w-2.5 h-2.5 text-slate-500" />
            <span>System</span>
          </span>
        );
    }
  };

  const getDetailColor = (e: LogEventItem) => {
    const d = e.description || e.title || '';
    if (e.category === 'combat' || d.includes('fehlgeschlagen') || d.includes('Failed') || d.includes('Tod') || d.includes('Crash') || d.includes('abgebrochen') || d.includes('Abandoned') || d.includes('storniert')) {
      return 'text-rose-400 font-medium';
    }
    if (d.includes('abgeschlossen') || d.includes('Complete') || d.includes('Erfolgreich') || (e.category === 'wallet' && (e.amount || 0) > 0)) {
      return 'text-emerald-300 font-medium';
    }
    if (d.includes('zurückgezogen') || d.includes('Withdrawn') || d.includes('Session') || d.includes('Warnung') || d.includes('Waffen scharf') || d.includes('Ungesetzlich')) {
      return 'text-amber-300';
    }
    return 'text-slate-200';
  };

  return (
    <div className="flex flex-col min-h-full space-y-2.5 select-none">
      {/* ══ 1. Filter-Bar & Schnellsuche (ohne doppelte Sitzungsauswahl) ══ */}
      <div className="flex flex-wrap items-center justify-between gap-3 px-3 py-2 rounded-lg bg-[#040914]/90 border border-cyan-950/80 backdrop-blur-md shrink-0">
        {/* Left: Active Mode Pill & Filter Chips */}
        <div className="flex items-center gap-3 min-w-0 flex-1 overflow-x-auto no-scrollbar">
          {/* Mode Switch: Live-Stream vs Sitzungsarchiv */}
          <div className="flex items-center gap-1.5 shrink-0 bg-[#030814] p-0.5 rounded border border-cyan-950">
            <button
              type="button"
              onClick={() => {
                setViewMode('live');
                fetchEvents(limit, '__live__');
              }}
              className={`px-2.5 py-1 rounded text-xs font-mono font-bold flex items-center gap-1.5 transition cursor-pointer ${
                viewMode === 'live'
                  ? 'bg-emerald-950/80 text-emerald-300 border border-emerald-500/70 shadow-[0_0_8px_rgba(16,185,129,0.25)]'
                  : 'text-slate-400 hover:text-white border border-transparent'
              }`}
              title="Startseite: Live-Stream der aktuellen Star Citizen Sitzung"
            >
              <span className="relative flex h-2 w-2">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
                <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
              </span>
              <span>Live-Stream</span>
            </button>

            <button
              type="button"
              onClick={() => {
                setViewMode('archive');
                fetchEvents(limit, archiveSession);
              }}
              className={`px-2.5 py-1 rounded text-xs font-mono font-bold flex items-center gap-1.5 transition cursor-pointer ${
                viewMode === 'archive'
                  ? 'bg-cyan-950/80 text-cyan-300 border border-cyan-500/70 shadow-[0_0_8px_rgba(6,182,212,0.25)]'
                  : 'text-slate-400 hover:text-white border border-transparent'
              }`}
              title="Sitzungsarchiv: Frühere Logdateien auswählen und durchsuchen"
            >
              <Archive className="w-3.5 h-3.5 text-cyan-400" />
              <span>Sitzungsarchiv</span>
            </button>

            <button
              type="button"
              onClick={() => setViewMode('combat')}
              className={`px-2.5 py-1 rounded text-xs font-mono font-bold flex items-center gap-1.5 transition cursor-pointer ${
                viewMode === 'combat'
                  ? 'bg-rose-950/80 text-rose-300 border border-rose-500/70 shadow-[0_0_8px_rgba(244,63,94,0.25)]'
                  : 'text-slate-400 hover:text-white border border-transparent'
              }`}
              title="Combat- & Gefahren-Analytics: Kit-Verluste, Ursachen, Gefahrenzonen und K/D"
            >
              <Swords className="w-3.5 h-3.5 text-rose-400" />
              <span>Combat-Analytics</span>
            </button>
          </div>

          {/* Wenn Archiv oder Combat gewählt ist: Dropdown für archivierte Sitzungen */}
          {(viewMode === 'archive' || viewMode === 'combat') && (
            <div className="relative shrink-0 animate-in fade-in duration-200">
              <select
                value={archiveSession}
                onChange={(e) => setArchiveSession(e.target.value)}
                className="appearance-none bg-[#071322] border border-cyan-900/80 hover:border-cyan-500 rounded px-2.5 py-1 pr-7 text-xs font-mono text-slate-200 focus:outline-none focus:border-cyan-400 transition cursor-pointer max-w-[220px] sm:max-w-xs truncate"
              >
                <option value="__all__">🌐 Alle Sitzungen (Gesamthistorie)</option>
                {sessions.map((s) => (
                  <option key={s.id || s.name} value={s.name}>
                    📁 {s.name} ({s.startTime} · {s.duration})
                  </option>
                ))}
              </select>
              <ChevronDown className="w-3.5 h-3.5 text-slate-400 absolute right-2 top-1/2 -translate-y-1/2 pointer-events-none" />
            </div>
          )}

          <div className="h-4 w-px bg-cyan-950/80 hidden sm:block shrink-0" />

          {/* Filter-Chips */}
          <div className="flex items-center gap-1.5 overflow-x-auto no-scrollbar">
            {[
              { id: 'Alle', label: 'Alle' },
              { id: 'Geld', label: '💰 Geld' },
              { id: 'Aufträge', label: '❖ Aufträge' },
              { id: 'Baupläne', label: '⬡ Baupläne' },
              { id: 'Schiffe', label: '🚀 Schiffe' },
              { id: 'Orte', label: '📍 Orte' },
              { id: 'Crew', label: '👥 Crew' },
              { id: 'Loot', label: '📦 Loot' },
              { id: 'Sonst', label: '⚙️ Sonst' },
            ].map((c) => (
              <button
                key={c.id}
                onClick={() => setCategory(c.id)}
                className={`px-2.5 py-1 text-xs font-mono font-semibold rounded transition cursor-pointer shrink-0 ${
                  category === c.id
                    ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-400/60 shadow-[0_0_10px_rgba(6,182,212,0.25)]'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900/80 border border-slate-800/80'
                }`}
              >
                {c.label}
              </button>
            ))}
          </div>
        </div>

        {/* Right: Search Input + Refresh */}
        <div className="flex items-center gap-2 shrink-0">
          <form onSubmit={handleSearchSubmit} className="flex items-center gap-1.5">
            <div className="relative">
              <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-500" />
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Ereignisse filtern..."
                className="bg-[#071322] border border-cyan-900/60 focus:border-cyan-500/80 rounded pl-8 pr-7 py-1 text-xs font-mono text-slate-200 placeholder-slate-500 focus:outline-none w-48 sm:w-56 transition"
              />
              {search && (
                <button
                  type="button"
                  onClick={() => {
                    setSearch('');
                    fetchEvents();
                  }}
                  className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-500 hover:text-slate-300 cursor-pointer"
                >
                  <X className="w-3.5 h-3.5" />
                </button>
              )}
            </div>
          </form>
        </div>
      </div>

      {/* ══ COMBAT & HAZARD ANALYTICS VIEW ══ */}
      {viewMode === 'combat' && (
        <div className="flex-1 flex flex-col space-y-3 overflow-y-auto pr-1">
          {/* Top KPI Cards */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
            <div className="sc-glass rounded-lg p-3.5 border border-slate-800 flex items-center justify-between">
              <div>
                <div className="text-xs text-slate-400 font-mono">KILLS / TOTESVERHÄLTNIS</div>
                <div className="text-xl font-bold text-emerald-400 mt-0.5">
                  {combatData?.totalKills ?? 0} <span className="text-xs text-slate-400 font-normal">Kills</span> · <span className="text-cyan-300">K/D {combatData?.kdRatio ?? 0}</span>
                </div>
              </div>
              <div className="p-2 rounded-lg bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
                <Target className="w-5 h-5" />
              </div>
            </div>

            <div className="sc-glass rounded-lg p-3.5 border border-slate-800 flex items-center justify-between">
              <div>
                <div className="text-xs text-slate-400 font-mono">TODE & SCHIFFSVERLUSTE</div>
                <div className="text-xl font-bold text-rose-400 mt-0.5">
                  {combatData?.totalDeaths ?? 0} <span className="text-xs text-slate-400 font-normal">Tode</span> · <span className="text-rose-300">{combatData?.shipLosses ?? 0} Schiffe</span>
                </div>
              </div>
              <div className="p-2 rounded-lg bg-rose-500/10 text-rose-400 border border-rose-500/20">
                <Skull className="w-5 h-5" />
              </div>
            </div>

            <div className="sc-glass rounded-lg p-3.5 border border-slate-800 flex items-center justify-between">
              <div>
                <div className="text-xs text-slate-400 font-mono">KIT-AUSRÜSTUNGSVERLUST</div>
                <div className="text-xl font-bold text-amber-400 mt-0.5">
                  ~ {((combatData?.estimatedKitLossAuec ?? 0) / 1000).toLocaleString('de-DE')}k <span className="text-xs font-normal text-slate-400">aUEC</span>
                </div>
              </div>
              <div className="p-2 rounded-lg bg-amber-500/10 text-amber-400 border border-amber-500/20">
                <ShieldAlert className="w-5 h-5" />
              </div>
            </div>

            <div className="sc-glass rounded-lg p-3.5 border border-slate-800 flex items-center justify-between">
              <div>
                <div className="text-xs text-slate-400 font-mono">GESAMTSCHADEN AUSFÄLLE</div>
                <div className="text-xl font-bold text-rose-400 mt-0.5">
                  ~ {((combatData?.estimatedTotalLossAuec ?? 0) / 1000).toLocaleString('de-DE')}k <span className="text-xs font-normal text-slate-400">aUEC</span>
                </div>
              </div>
              <div className="p-2 rounded-lg bg-rose-500/10 text-rose-400 border border-rose-500/20">
                <AlertOctagon className="w-5 h-5" />
              </div>
            </div>
          </div>

          {/* Analysis Split View */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-3 flex-1 min-h-[400px]">
            {/* Left: Death Causes Breakdown & Hazard Zones */}
            <div className="space-y-3 flex flex-col">
              {/* Causes */}
              <div className="sc-glass rounded-lg p-3.5 border border-slate-800 space-y-3">
                <h3 className="text-xs font-mono font-bold uppercase tracking-wider text-slate-300 flex items-center gap-2">
                  <Activity className="w-4 h-4 text-rose-400" />
                  Todesursachen-Verteilung
                </h3>
                <div className="space-y-2 font-mono text-xs">
                  {combatData?.deathCauses.map((c) => (
                    <div key={c.label} className="space-y-1">
                      <div className="flex items-center justify-between text-[11px]">
                        <span className="text-slate-300">{c.label}</span>
                        <span className="text-slate-400">{c.count} Vorfälle ({c.percent}%)</span>
                      </div>
                      <div className="w-full bg-slate-900 h-2 rounded-full overflow-hidden">
                        <div
                          className="h-full rounded-full transition-all duration-300"
                          style={{ width: `${Math.max(4, c.percent)}%`, backgroundColor: c.color }}
                        />
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* Hotspots */}
              <div className="sc-glass rounded-lg p-3.5 border border-slate-800 space-y-3 flex-1">
                <h3 className="text-xs font-mono font-bold uppercase tracking-wider text-slate-300 flex items-center gap-2">
                  <ShieldAlert className="w-4 h-4 text-amber-400" />
                  Gefahrenzonen & Hotspots (Verlustschwerpunkte)
                </h3>
                <div className="space-y-1.5 font-mono text-xs">
                  {combatData?.dangerZones && combatData.dangerZones.length > 0 ? (
                    combatData.dangerZones.map((z) => (
                      <div
                        key={z.location}
                        className="flex items-center justify-between p-2 rounded bg-slate-900/60 border border-slate-800"
                      >
                        <div className="flex items-center gap-2">
                          <MapPin className="w-3.5 h-3.5 text-rose-400 shrink-0" />
                          <div>
                            <div className="font-bold text-slate-200">{z.location}</div>
                            <div className="text-[10px] text-slate-400">{z.system}</div>
                          </div>
                        </div>

                        <div className="flex items-center gap-3 text-[11px]">
                          <span className="text-slate-400">
                            ☠ {z.deaths} Tode · 💥 {z.shipLosses} Schiffe
                          </span>
                          <span
                            className={`px-2 py-0.5 rounded text-[10px] font-bold border ${
                              z.threatLevel === 'Kritisch'
                                ? 'bg-rose-950/60 text-rose-400 border-rose-800'
                                : 'bg-amber-950/60 text-amber-400 border-amber-800'
                            }`}
                          >
                            {z.threatLevel}
                          </span>
                        </div>
                      </div>
                    ))
                  ) : (
                    <div className="text-slate-500 text-xs text-center py-6">Keine Gefahrenzonen verzeichnet.</div>
                  )}
                </div>
              </div>
            </div>

            {/* Right: Casualty Incidents Log */}
            <div className="sc-glass rounded-lg p-3.5 border border-slate-800 flex flex-col">
              <h3 className="text-xs font-mono font-bold uppercase tracking-wider text-slate-300 mb-3 flex items-center gap-2 shrink-0">
                <Skull className="w-4 h-4 text-rose-400" />
                Letzte Verlust- & Gefechtsvorfälle ({combatData?.recentCasualties.length ?? 0})
              </h3>
              <div className="flex-1 overflow-y-auto space-y-2 font-mono text-xs pr-1 max-h-[500px]">
                {combatData?.recentCasualties && combatData.recentCasualties.length > 0 ? (
                  combatData.recentCasualties.map((inc) => (
                    <div
                      key={inc.id}
                      className="p-2.5 rounded bg-slate-900/60 border border-slate-800 hover:border-rose-500/40 transition"
                    >
                      <div className="flex items-center justify-between text-[10px] text-slate-500 mb-1">
                        <span>{inc.timestamp}</span>
                        <span className="text-rose-400 font-bold">~ {inc.estimatedCostAuec.toLocaleString('de-DE')} aUEC Kit-Kosten</span>
                      </div>
                      <div className="font-bold text-slate-200 text-[11px]">{inc.title}</div>
                      <div className="text-slate-400 text-[10px] mt-0.5 truncate">{inc.detail}</div>
                      <div className="text-[10px] text-slate-400 mt-1 flex items-center gap-1">
                        <MapPin className="w-3 h-3 text-cyan-400" />
                        {inc.location}
                      </div>
                    </div>
                  ))
                ) : (
                  <div className="text-slate-500 text-xs text-center py-12">Keine Verluste in dieser Sitzung.</div>
                )}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ══ 2. Hauptbereich: DataGrid Tabelle + ausziehbarer Detail-Drawer ══ */}
      {viewMode !== 'combat' && (
      <div className="flex-1 flex gap-3 min-h-[350px]">
        {/* DataGrid Container */}
        <div className="flex-1 flex flex-col bg-[#040914]/90 rounded-lg border border-cyan-950/80 overflow-hidden shadow-sm min-w-0">
          {/* DataGrid Header */}
          <div className="grid grid-cols-[105px_120px_125px_150px_1fr] bg-[#061224] border-b border-cyan-950 text-[10.5px] font-mono font-bold text-slate-400 uppercase tracking-wider shrink-0 select-none">
            <div
              onClick={() => handleSort('timestamp')}
              className="flex items-center gap-1 cursor-pointer hover:text-cyan-300 px-3 py-2 border-r border-cyan-950/80"
            >
              <span>ZEIT</span>
              {sortCol === 'timestamp' && (sortDir === 'asc' ? <ChevronUp className="w-3 h-3" /> : <ChevronDown className="w-3 h-3" />)}
            </div>

            <div
              onClick={() => handleSort('category')}
              className="flex items-center gap-1 cursor-pointer hover:text-cyan-300 px-3 py-2 border-r border-cyan-950/80"
            >
              <span>TYP</span>
              {sortCol === 'category' && (sortDir === 'asc' ? <ChevronUp className="w-3 h-3" /> : <ChevronDown className="w-3 h-3" />)}
            </div>

            <div
              onClick={() => handleSort('amount')}
              className="flex items-center justify-end gap-1 cursor-pointer hover:text-cyan-300 px-3 py-2 border-r border-cyan-950/80 text-right"
            >
              <span>BETRAG</span>
              {sortCol === 'amount' && (sortDir === 'asc' ? <ChevronUp className="w-3 h-3" /> : <ChevronDown className="w-3 h-3" />)}
            </div>

            <div
              onClick={() => handleSort('ship')}
              className="flex items-center gap-1 cursor-pointer hover:text-cyan-300 px-3 py-2 border-r border-cyan-950/80"
            >
              <span>SCHIFF</span>
              {sortCol === 'ship' && (sortDir === 'asc' ? <ChevronUp className="w-3 h-3" /> : <ChevronDown className="w-3 h-3" />)}
            </div>

            <div
              onClick={() => handleSort('title')}
              className="flex items-center gap-1 cursor-pointer hover:text-cyan-300 px-3 py-2"
            >
              <span>DETAIL</span>
              {sortCol === 'title' && (sortDir === 'asc' ? <ChevronUp className="w-3 h-3" /> : <ChevronDown className="w-3 h-3" />)}
            </div>
          </div>

          {/* DataGrid Rows (Virtual Scrollable) */}
          <div className="flex-1 overflow-y-auto divide-y divide-cyan-950/40 font-mono text-xs">
            {displayEvents.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-20 px-4 text-center">
                <div className="w-12 h-12 rounded-full bg-cyan-950/40 border border-cyan-800/40 flex items-center justify-center mb-3 shadow-[0_0_15px_rgba(6,182,212,0.15)]">
                  <Terminal className="w-6 h-6 text-cyan-400 opacity-60" />
                </div>
                <div className="text-sm font-semibold text-slate-300 font-mono mb-1">
                  Keine Ereignisse gefunden
                </div>
                <div className="text-xs text-slate-500 max-w-sm mb-3">
                  {search
                    ? `Keine Treffer für die Suche "${search}".`
                    : category !== 'Alle'
                    ? `In der Kategorie "${category}" sind für diese Sitzung keine Einträge vorhanden.`
                    : 'Für die ausgewählte Sitzung wurden noch keine Ereignisse erfasst oder synchronisiert.'}
                </div>
                {(search || category !== 'Alle') && (
                  <button
                    onClick={() => {
                      setSearch('');
                      setCategory('Alle');
                    }}
                    className="px-3 py-1 text-xs font-mono rounded bg-cyan-950/60 hover:bg-cyan-900/60 border border-cyan-700/60 text-cyan-300 transition cursor-pointer"
                  >
                    Filter zurücksetzen
                  </button>
                )}
              </div>
            ) : (
              displayEvents.map((e) => {
                const isSelected = selectedEvent?.id === e.id;
                return (
                  <div
                    key={e.id}
                    onClick={() => setSelectedEvent(e)}
                    onContextMenu={(ev) => {
                      ev.preventDefault();
                      setSelectedEvent(e);
                      setContextMenu({ x: ev.clientX, y: ev.clientY, event: e });
                    }}
                    className={`grid grid-cols-[105px_120px_125px_150px_1fr] items-center cursor-pointer transition-colors ${
                      isSelected
                        ? 'bg-cyan-950/50 text-slate-100 border-l-2 border-l-cyan-400'
                        : 'hover:bg-[#071526]/60 text-slate-300'
                    }`}
                  >
                    {/* Zeit */}
                    <div className="text-[11px] text-slate-400 truncate px-3 py-1.5 border-r border-cyan-950/40">
                      {e.timestamp}
                    </div>

                    {/* Typ Badge */}
                    <div className="px-3 py-1.5 border-r border-cyan-950/40 flex items-center">
                      {getCategoryBadge(e.category)}
                    </div>

                    {/* Betrag */}
                    <div className="text-right px-3 py-1.5 border-r border-cyan-950/40 font-bold text-xs">
                      {e.amount !== undefined && e.amount !== null && e.amount !== 0 ? (
                        <span className={e.amount > 0 ? 'text-emerald-400' : 'text-rose-400'}>
                          {e.amount > 0 ? '+' : ''}
                          {formatNumber(e.amount)}
                        </span>
                      ) : null}
                    </div>

                    {/* Schiff */}
                    <div className="truncate text-sky-400 font-medium text-[11px] px-3 py-1.5 border-r border-cyan-950/40" title={e.ship || ''}>
                      {e.ship || null}
                    </div>

                    {/* Detail Text */}
                    <div className={`truncate text-xs px-3 py-1.5 ${getDetailColor(e)}`} title={e.description || e.title}>
                      {e.description || e.title}
                    </div>
                  </div>
                );
              })
            )}
          </div>

          {/* Footer Bar: Item Counter & Load More */}
          <div className="flex items-center justify-between px-3 py-1.5 bg-[#030814] border-t border-cyan-950 text-[11px] font-mono text-slate-500 shrink-0">
            <div>
              Zeige <span className="text-cyan-400 font-semibold">{displayEvents.length}</span> von{' '}
              <span className="text-slate-300">{events.length}</span> geladenen Ereignissen
            </div>

            <button
              onClick={handleLoadMore}
              className="px-2 py-0.5 rounded bg-cyan-950/40 hover:bg-cyan-900/60 border border-cyan-800/60 text-cyan-300 hover:text-cyan-200 transition cursor-pointer font-medium"
            >
              + 150 weitere laden
            </button>
          </div>
        </div>

        {/* ══ 3. Ausziehbarer Detail-Drawer (Side Inspector wie in RC2) ══ */}
        {selectedEvent && (
          <div className="w-84 bg-[#040914]/95 rounded-lg border border-cyan-800/60 p-3.5 flex flex-col justify-between overflow-y-auto shrink-0 shadow-lg animate-fade-in font-mono">
            <div className="space-y-3.5">
              {/* Drawer Header */}
              <div className="flex items-center justify-between pb-2 border-b border-cyan-950">
                <div className="flex items-center gap-2">
                  {getCategoryBadge(selectedEvent.category)}
                  <span className="text-xs font-bold text-slate-200 uppercase tracking-wider">
                    Ereignis-Detail
                  </span>
                </div>
                <button
                  onClick={() => setSelectedEvent(null)}
                  className="p-1 rounded text-slate-400 hover:text-slate-200 hover:bg-slate-800 transition cursor-pointer"
                  title="Schließen (Esc)"
                >
                  <X className="w-3.5 h-3.5" />
                </button>
              </div>

              {/* Title & Description */}
              <div>
                <div className="text-sm font-bold text-slate-100 leading-snug">{selectedEvent.title}</div>
                <div className="mt-1 text-xs text-slate-300 font-sans leading-relaxed">
                  {selectedEvent.description}
                </div>
              </div>

              {/* Telemetrie Badges Grid */}
              <div className="grid grid-cols-2 gap-2 text-xs">
                {/* Zeit */}
                <div className="p-2 rounded bg-[#061224] border border-cyan-950">
                  <span className="text-[10px] text-slate-500 block uppercase font-bold">Zeitpunkt</span>
                  <span className="text-slate-200 text-xs mt-0.5 block">{selectedEvent.timestamp}</span>
                </div>

                {/* Betrag */}
                <div className="p-2 rounded bg-[#061224] border border-cyan-950">
                  <span className="text-[10px] text-slate-500 block uppercase font-bold">Finanzen</span>
                  {selectedEvent.amount !== undefined && selectedEvent.amount !== null && selectedEvent.amount !== 0 ? (
                    <span
                      className={`text-xs font-bold mt-0.5 block ${
                        selectedEvent.amount > 0 ? 'text-emerald-400' : 'text-rose-400'
                      }`}
                    >
                      {selectedEvent.amount > 0 ? '+' : ''}
                      {formatNumber(selectedEvent.amount)} aUEC
                    </span>
                  ) : (
                    <span className="text-slate-500 text-xs mt-0.5 block">—</span>
                  )}
                </div>

                {/* Schiff */}
                {selectedEvent.ship && (
                  <div className="col-span-2 p-2 rounded bg-[#061224] border border-cyan-950">
                    <span className="text-[10px] text-slate-500 block uppercase font-bold">
                      Beteiligtes Schiff
                    </span>
                    <span className="text-sky-300 font-bold text-xs mt-0.5 block">
                      {selectedEvent.ship}
                    </span>
                  </div>
                )}
              </div>

              {/* Rohdaten / Logzeile Box */}
              <div>
                <div className="flex items-center justify-between mb-1">
                  <span className="text-[10px] uppercase font-bold text-slate-500">Rohdaten (Log-Zeile)</span>
                  <button
                    onClick={() => handleCopy(selectedEvent.rawText || selectedEvent.description, 'raw')}
                    className="flex items-center gap-1 text-[10px] text-cyan-400 hover:text-cyan-300 cursor-pointer"
                  >
                    {copiedField === 'raw' ? (
                      <>
                        <Check className="w-2.5 h-2.5 text-emerald-400" />
                        <span className="text-emerald-400 font-bold">Kopiert!</span>
                      </>
                    ) : (
                      <>
                        <Copy className="w-2.5 h-2.5" />
                        <span>Kopieren</span>
                      </>
                    )}
                  </button>
                </div>
                <div className="p-2 rounded bg-black/60 border border-slate-800 text-[10px] text-slate-400 break-all leading-relaxed max-h-36 overflow-y-auto select-text font-mono">
                  {selectedEvent.rawText || selectedEvent.description}
                </div>
              </div>
            </div>

            {/* Aktionen am Fuß des Drawers */}
            <div className="pt-3 border-t border-cyan-950 flex items-center gap-2">
              <button
                onClick={() => handleCopy(selectedEvent.description, 'desc')}
                className="flex-1 flex items-center justify-center gap-1.5 px-2.5 py-1.5 rounded bg-[#061224] hover:bg-cyan-950/60 border border-cyan-950 hover:border-cyan-800 text-xs text-slate-300 hover:text-cyan-300 transition cursor-pointer"
              >
                {copiedField === 'desc' ? (
                  <>
                    <Check className="w-3 h-3 text-emerald-400" />
                    <span className="text-emerald-400 font-semibold">Kopiert!</span>
                  </>
                ) : (
                  <>
                    <Copy className="w-3 h-3" />
                    <span>Detail kopieren</span>
                  </>
                )}
              </button>

              {selectedEvent.ship && (
                <button
                  onClick={() => {
                    window.dispatchEvent(new CustomEvent('open-wiki-dossier', { detail: selectedEvent.ship }));
                  }}
                  className="flex items-center justify-center gap-1 px-2.5 py-1.5 rounded bg-[#061224] hover:bg-cyan-950/60 border border-cyan-950 hover:border-cyan-800 text-xs text-cyan-400 hover:text-cyan-200 transition cursor-pointer"
                  title="Star Citizen Wiki Dossier öffnen"
                >
                  <ExternalLink className="w-3 h-3" />
                  <span>Wiki</span>
                </button>
              )}
            </div>
          </div>
        )}
      </div>
      )}

      {contextMenu && (
        <ContextMenu
          x={contextMenu.x}
          y={contextMenu.y}
          onClose={() => setContextMenu(null)}
          items={[
            ...(contextMenu.event.ship && contextMenu.event.ship !== '—'
              ? [
                  {
                    label: `Im SCWiki öffnen: ${contextMenu.event.ship}`,
                    icon: BookOpen,
                    onClick: () => {
                      window.dispatchEvent(
                        new CustomEvent('open-wiki-dossier', { detail: contextMenu.event.ship })
                      );
                    },
                  },
                  {
                    label: `Filter auf Schiff: ${contextMenu.event.ship}`,
                    icon: Filter,
                    onClick: () => setSearch(contextMenu.event.ship || ''),
                  },
                ]
              : []),
            {
              label: `Kategorie filtern: ${contextMenu.event.category}`,
              icon: Filter,
              onClick: () => setCategory(contextMenu.event.category),
            },
            { divider: true, label: '', onClick: () => {} },
            {
              label: 'Zeilen-Inhalt kopieren',
              icon: Copy,
              onClick: () => {
                const e = contextMenu.event;
                const amt = e.amount ? ` (${e.amount > 0 ? '+' : ''}${e.amount} aUEC)` : '';
                const shp = e.ship ? ` [${e.ship}]` : '';
                const text = `[${e.timestamp}] [${e.kindText}] ${e.title} - ${e.description || ''}${amt}${shp}`.trim();
                handleCopy(text, 'row');
              },
            },
            {
              label: 'JSON Rohdaten kopieren',
              icon: Code2,
              onClick: () => {
                handleCopy(JSON.stringify(contextMenu.event, null, 2), 'json');
              },
            },
          ]}
        />
      )}
    </div>
  );
};

export default EventsView;
