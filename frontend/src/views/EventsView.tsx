import React, { useEffect, useMemo, useState } from 'react';
import { bridge, LogEventItem } from '../services/photinoBridge';
import {
  Check,
  ChevronDown,
  ChevronUp,
  Coins,
  Copy,
  ExternalLink,
  MapPin,
  RefreshCw,
  Rocket,
  Search,
  Swords,
  Target,
  Terminal,
  X,
} from 'lucide-react';

type SortColumn = 'timestamp' | 'category' | 'amount' | 'ship' | 'title';
type SortDirection = 'asc' | 'desc';

export const EventsView: React.FC = () => {
  const [events, setEvents] = useState<LogEventItem[]>([]);
  const [category, setCategory] = useState<string>('all');
  const [search, setSearch] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(false);
  const [selectedEvent, setSelectedEvent] = useState<LogEventItem | null>(null);
  const [limit, setLimit] = useState<number>(200);
  const [sortCol, setSortCol] = useState<SortColumn>('timestamp');
  const [sortDir, setSortDir] = useState<SortDirection>('desc');
  const [copiedField, setCopiedField] = useState<string | null>(null);

  const fetchEvents = async (count = limit) => {
    try {
      setLoading(true);
      const res = await bridge.sendRequest<LogEventItem[]>('get_events', {
        category,
        search,
        limit: count,
        offset: 0,
      });
      setEvents(res);
      if (res.length > 0 && !selectedEvent) {
        setSelectedEvent(res[0]);
      }
    } catch (err) {
      console.error('Failed to load events:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchEvents();
  }, [category]);

  // Live log subscription
  useEffect(() => {
    const unbind = bridge.on<LogEventItem>('LOG_EVENT', (newEvent) => {
      setEvents((prev) => [newEvent, ...prev.slice(0, limit - 1)]);
    });
    return () => unbind();
  }, [limit]);

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

  return (
    <div className="flex flex-col min-h-full space-y-2.5 select-none">
      {/* ══ 1. Filter-Bar & Schnellsuche (im RC2-Look) ══ */}
      <div className="flex flex-wrap items-center justify-between gap-2.5 px-3 py-2 rounded-lg bg-[#040914]/90 border border-cyan-950/80 backdrop-blur-md shrink-0">
        {/* Kategorien Chips */}
        <div className="flex items-center gap-1.5 overflow-x-auto no-scrollbar">
          {[
            { id: 'all', label: 'Alle' },
            { id: 'wallet', label: '💰 Finanzen' },
            { id: 'combat', label: '⚔️ Kampf' },
            { id: 'mission', label: '🎯 Aufträge' },
            { id: 'ship', label: '🚀 Schiffe & QT' },
            { id: 'location', label: '📍 Standorte' },
            { id: 'system', label: '⚙️ System' },
          ].map((c) => (
            <button
              key={c.id}
              onClick={() => setCategory(c.id)}
              className={`px-2.5 py-1 text-xs font-mono font-semibold rounded transition cursor-pointer shrink-0 ${
                category === c.id
                  ? 'bg-cyan-950/70 text-cyan-300 border border-cyan-500/60 shadow-[0_0_8px_rgba(6,182,212,0.25)]'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900/80 border border-transparent'
              }`}
            >
              {c.label}
            </button>
          ))}
        </div>

        {/* Live Tail Indicator + Search Input */}
        <div className="flex items-center gap-2.5 shrink-0">
          <div
            className="flex items-center gap-1.5 px-2 py-1 rounded-full bg-emerald-950/40 border border-emerald-800/50 text-emerald-300 font-mono text-[10px] font-bold tracking-wider shrink-0"
            title="Echtzeit-Log-Überwachung ist aktiv"
          >
            <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse shadow-[0_0_6px_#34d399]" />
            <span>LIVE TAIL</span>
          </div>

          <form onSubmit={handleSearchSubmit} className="flex items-center gap-1.5">
            <div className="relative">
              <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-500" />
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Chronik durchsuchen..."
                className="bg-[#071322] border border-cyan-900/60 focus:border-cyan-500/80 rounded pl-8 pr-7 py-1 text-xs font-mono text-slate-200 placeholder-slate-500 focus:outline-none w-56 transition"
              />
              {search && (
                <button
                  type="button"
                  onClick={() => {
                    setSearch('');
                    fetchEvents();
                  }}
                  className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-500 hover:text-slate-300"
                >
                  <X className="w-3.5 h-3.5" />
                </button>
              )}
            </div>

            <button
              type="button"
              onClick={() => fetchEvents()}
              className="p-1.5 rounded bg-cyan-950/60 border border-cyan-800/60 text-cyan-300 hover:bg-cyan-900/80 transition cursor-pointer"
              title="Aktualisieren"
            >
              <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
            </button>
          </form>
        </div>
      </div>

      {/* ══ 2. Hauptbereich: DataGrid Tabelle + ausziehbarer Detail-Drawer ══ */}
      <div className="flex-1 flex gap-3 min-h-[350px]">
        {/* DataGrid Container */}
        <div className="flex-1 flex flex-col bg-[#040914]/90 rounded-lg border border-cyan-950/80 overflow-hidden shadow-sm min-w-0">
          {/* DataGrid Header */}
          <div className="grid grid-cols-[135px_115px_120px_140px_1fr] px-3 py-2 bg-[#061224] border-b border-cyan-950 text-[10.5px] font-mono font-bold text-slate-400 uppercase tracking-wider shrink-0 select-none">
            <div
              onClick={() => handleSort('timestamp')}
              className="flex items-center gap-1 cursor-pointer hover:text-cyan-300"
            >
              <span>ZEIT</span>
              {sortCol === 'timestamp' && (sortDir === 'asc' ? <ChevronUp className="w-3 h-3" /> : <ChevronDown className="w-3 h-3" />)}
            </div>

            <div
              onClick={() => handleSort('category')}
              className="flex items-center gap-1 cursor-pointer hover:text-cyan-300"
            >
              <span>TYP</span>
              {sortCol === 'category' && (sortDir === 'asc' ? <ChevronUp className="w-3 h-3" /> : <ChevronDown className="w-3 h-3" />)}
            </div>

            <div
              onClick={() => handleSort('amount')}
              className="flex items-center justify-end gap-1 cursor-pointer hover:text-cyan-300 pr-2"
            >
              <span>BETRAG</span>
              {sortCol === 'amount' && (sortDir === 'asc' ? <ChevronUp className="w-3 h-3" /> : <ChevronDown className="w-3 h-3" />)}
            </div>

            <div
              onClick={() => handleSort('ship')}
              className="flex items-center gap-1 cursor-pointer hover:text-cyan-300"
            >
              <span>SCHIFF</span>
              {sortCol === 'ship' && (sortDir === 'asc' ? <ChevronUp className="w-3 h-3" /> : <ChevronDown className="w-3 h-3" />)}
            </div>

            <div
              onClick={() => handleSort('title')}
              className="flex items-center gap-1 cursor-pointer hover:text-cyan-300"
            >
              <span>DETAIL</span>
              {sortCol === 'title' && (sortDir === 'asc' ? <ChevronUp className="w-3 h-3" /> : <ChevronDown className="w-3 h-3" />)}
            </div>
          </div>

          {/* DataGrid Rows (Virtual Scrollable) */}
          <div className="flex-1 overflow-y-auto divide-y divide-cyan-950/40 font-mono text-xs">
            {displayEvents.length === 0 ? (
              <div className="py-24 text-center text-slate-500 text-xs">
                Keine Ereignisse für die gewählte Filterung gefunden.
              </div>
            ) : (
              displayEvents.map((e) => {
                const isSelected = selectedEvent?.id === e.id;
                return (
                  <div
                    key={e.id}
                    onClick={() => setSelectedEvent(e)}
                    className={`grid grid-cols-[135px_115px_120px_140px_1fr] px-3 py-1.5 items-center cursor-pointer transition-colors ${
                      isSelected
                        ? 'bg-cyan-950/50 text-slate-100 border-l-2 border-l-cyan-400'
                        : 'hover:bg-[#071526]/60 text-slate-300'
                    }`}
                  >
                    {/* Zeit */}
                    <div className="text-[11px] text-slate-400 truncate">{e.timestamp}</div>

                    {/* Typ Badge */}
                    <div>{getCategoryBadge(e.category)}</div>

                    {/* Betrag */}
                    <div className="text-right pr-2 font-bold text-xs">
                      {e.amount !== undefined && e.amount !== null && e.amount !== 0 ? (
                        <span className={e.amount > 0 ? 'text-emerald-400' : 'text-rose-400'}>
                          {e.amount > 0 ? '+' : ''}
                          {formatNumber(e.amount)}
                        </span>
                      ) : (
                        <span className="text-slate-600">—</span>
                      )}
                    </div>

                    {/* Schiff */}
                    <div className="truncate text-sky-400 font-medium text-[11px]" title={e.ship || ''}>
                      {e.ship || <span className="text-slate-600">—</span>}
                    </div>

                    {/* Detail Text */}
                    <div className="truncate text-slate-300 text-xs pr-2" title={e.description || e.title}>
                      <span className="font-semibold text-slate-200 mr-2">{e.title}:</span>
                      <span className="text-slate-400">{e.description}</span>
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
                <a
                  href={`https://star-citizen.wiki/${encodeURIComponent(selectedEvent.ship)}`}
                  target="_blank"
                  rel="noreferrer"
                  className="flex items-center justify-center gap-1 px-2.5 py-1.5 rounded bg-[#061224] hover:bg-cyan-950/60 border border-cyan-950 hover:border-cyan-800 text-xs text-cyan-400 hover:text-cyan-200 transition cursor-pointer"
                  title="Im Star Citizen Wiki nachschlagen"
                >
                  <ExternalLink className="w-3 h-3" />
                  <span>Wiki</span>
                </a>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default EventsView;
