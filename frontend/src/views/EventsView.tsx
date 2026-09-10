import React, { useEffect, useState } from 'react';
import { bridge, LogEventItem } from '../services/photinoBridge';
import {
  RefreshCw,
  Search,
} from 'lucide-react';

export const EventsView: React.FC = () => {
  const [events, setEvents] = useState<LogEventItem[]>([]);
  const [category, setCategory] = useState<string>('all');
  const [search, setSearch] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(false);
  const [selectedEvent, setSelectedEvent] = useState<LogEventItem | null>(null);

  const fetchEvents = async () => {
    try {
      setLoading(true);
      const res = await bridge.sendRequest<LogEventItem[]>('get_events', {
        category,
        search,
        limit: 150,
      });
      setEvents(res);
    } catch (err) {
      console.error('Failed to load events:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchEvents();
  }, [category]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    fetchEvents();
  };

  const getCategoryBadge = (cat: string) => {
    switch (cat) {
      case 'wallet':
        return <span className="sc-badge text-[10px]">Geldwert</span>;
      case 'combat':
        return <span className="sc-badge-red text-[10px]">Kampf</span>;
      case 'mission':
        return <span className="sc-badge-gold text-[10px]">Mission</span>;
      case 'ship':
        return <span className="sc-badge text-[10px] text-cyan-300">Schiff</span>;
      case 'location':
        return <span className="sc-badge-green text-[10px]">Standort</span>;
      default:
        return <span className="px-1.5 py-0.5 rounded text-[10px] bg-slate-800 text-slate-400">System</span>;
    }
  };

  return (
    <div className="flex flex-col h-full space-y-4">
      {/* Filter & Toolbar */}
      <div className="flex flex-wrap items-center justify-between gap-3 sc-glass p-3.5 rounded-lg border border-slate-800/80">
        <div className="flex items-center gap-1.5 overflow-x-auto">
          {[
            { id: 'all', label: 'Alle Ereignisse' },
            { id: 'wallet', label: '💰 Finanzen' },
            { id: 'combat', label: '⚔️ Kampf & Verluste' },
            { id: 'mission', label: '🎯 Aufträge' },
            { id: 'ship', label: '🚀 Flotte & QT' },
            { id: 'location', label: '📍 Standorte' },
            { id: 'system', label: '⚙️ System' },
          ].map((c) => (
            <button
              key={c.id}
              onClick={() => setCategory(c.id)}
              className={`px-3 py-1.5 text-xs font-semibold rounded transition cursor-pointer shrink-0 ${
                category === c.id
                  ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40 shadow-[0_0_10px_rgba(0,240,255,0.2)]'
                  : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900 border border-transparent'
              }`}
            >
              {c.label}
            </button>
          ))}
        </div>

        {/* Search Bar */}
        <form onSubmit={handleSearchSubmit} className="flex items-center gap-2">
          <div className="relative">
            <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-500" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Chronik durchsuchen..."
              className="bg-slate-900/80 border border-slate-800 rounded pl-8 pr-3 py-1.5 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-cyan-500/50 w-52"
            />
          </div>
          <button
            type="submit"
            className="px-2.5 py-1.5 rounded bg-slate-800 text-slate-300 hover:text-cyan-300 text-xs font-medium cursor-pointer"
          >
            Suchen
          </button>
          <button
            type="button"
            onClick={fetchEvents}
            className="p-1.5 rounded bg-slate-800 text-slate-400 hover:text-slate-200 cursor-pointer"
            title="Neu laden"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
          </button>
        </form>
      </div>

      {/* Events Stream Grid */}
      <div className="flex-1 flex gap-4 overflow-hidden">
        <div className="flex-1 sc-glass rounded-lg border border-slate-800 overflow-y-auto p-3 space-y-2">
          {events.length === 0 ? (
            <div className="py-20 text-center text-slate-500 font-mono text-xs">
              Keine Ereignisse für die gewählte Filterung gefunden.
            </div>
          ) : (
            events.map((e) => (
              <div
                key={e.id}
                onClick={() => setSelectedEvent(e)}
                className={`p-3 rounded border transition cursor-pointer flex items-center justify-between gap-4 ${
                  selectedEvent?.id === e.id
                    ? 'bg-cyan-950/40 border-cyan-500/50 shadow-[0_0_12px_rgba(0,240,255,0.1)]'
                    : 'bg-slate-900/40 border-slate-800 hover:border-cyan-500/30 hover:bg-slate-900/70'
                }`}
              >
                <div className="flex items-center gap-3 min-w-0">
                  <span className="font-mono text-[11px] text-slate-500 shrink-0">{e.timestamp}</span>
                  <div className="shrink-0">{getCategoryBadge(e.category)}</div>
                  <div className="truncate">
                    <span className="font-semibold text-xs text-slate-200 mr-2">{e.title}</span>
                    <span className="text-xs text-slate-400">{e.description}</span>
                  </div>
                </div>

                <div className="flex items-center gap-3 shrink-0">
                  {e.ship && (
                    <span className="px-2 py-0.5 rounded text-[10px] bg-slate-800/80 border border-slate-700 text-slate-300 font-mono">
                      {e.ship}
                    </span>
                  )}
                  {e.amount !== undefined && e.amount !== null && e.amount !== 0 && (
                    <span
                      className={`font-mono font-bold text-xs ${
                        e.amount > 0 ? 'text-emerald-400' : 'text-rose-400'
                      }`}
                    >
                      {e.amount > 0 ? '+' : ''}
                      {e.amount.toLocaleString('de-DE')} aUEC
                    </span>
                  )}
                </div>
              </div>
            ))
          )}
        </div>

        {/* Selected Event Detail Drawer */}
        {selectedEvent && (
          <div className="w-80 sc-glass rounded-lg border border-cyan-500/30 p-4 flex flex-col justify-between sc-hud-corner overflow-y-auto">
            <div>
              <div className="flex items-center justify-between pb-3 border-b border-slate-800">
                <span className="text-xs font-mono font-semibold uppercase tracking-wider text-cyan-300">
                  Ereignis-Details
                </span>
                <button
                  onClick={() => setSelectedEvent(null)}
                  className="text-slate-500 hover:text-slate-300 text-xs cursor-pointer"
                >
                  ✕
                </button>
              </div>

              <div className="mt-3 space-y-3 text-xs">
                <div>
                  <span className="text-slate-500 block text-[10px] uppercase font-mono">Zeitpunkt</span>
                  <span className="font-mono text-slate-200">{selectedEvent.timestamp}</span>
                </div>

                <div>
                  <span className="text-slate-500 block text-[10px] uppercase font-mono">Kategorie & Typ</span>
                  <div className="flex items-center gap-2 mt-1">
                    {getCategoryBadge(selectedEvent.category)}
                    <span className="text-slate-200 font-semibold">{selectedEvent.title}</span>
                  </div>
                </div>

                <div>
                  <span className="text-slate-500 block text-[10px] uppercase font-mono">Beschreibung</span>
                  <p className="text-slate-300 mt-0.5 leading-relaxed">{selectedEvent.description}</p>
                </div>

                {selectedEvent.ship && (
                  <div>
                    <span className="text-slate-500 block text-[10px] uppercase font-mono">Beteiligtes Schiff</span>
                    <span className="font-mono text-cyan-300">{selectedEvent.ship}</span>
                  </div>
                )}

                {selectedEvent.amount !== undefined && selectedEvent.amount !== null && (
                  <div>
                    <span className="text-slate-500 block text-[10px] uppercase font-mono">Betrag</span>
                    <span
                      className={`font-mono text-sm font-bold ${
                        selectedEvent.amount > 0 ? 'text-emerald-400' : 'text-rose-400'
                      }`}
                    >
                      {selectedEvent.amount > 0 ? '+' : ''}
                      {selectedEvent.amount.toLocaleString('de-DE')} aUEC
                    </span>
                  </div>
                )}

                {selectedEvent.rawText && (
                  <div>
                    <span className="text-slate-500 block text-[10px] uppercase font-mono">Rohdaten / Zeile</span>
                    <div className="p-2 rounded bg-black/50 border border-slate-800 text-[10px] font-mono text-slate-400 break-all mt-1">
                      {selectedEvent.rawText}
                    </div>
                  </div>
                )}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
