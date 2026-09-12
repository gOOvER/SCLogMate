import React, { useEffect, useState } from 'react';
import { bridge, MissionItemDto, MissionsResponseDto } from '../services/photinoBridge';
import {
  CheckCircle2,
  Clock,
  Radio,
  Search,
  Target,
  Trash2,
} from 'lucide-react';

export const MissionsView: React.FC = () => {
  const [data, setData] = useState<MissionsResponseDto>({
    active: [],
    history: [],
    catalog: [],
  });
  const [activeTab, setActiveTab] = useState<'active' | 'history' | 'catalog'>('active');
  const [search, setSearch] = useState<string>('');
  const [typeFilter, setTypeFilter] = useState<string>('all');
  const [toast, setToast] = useState<string | null>(null);

  const showToast = (msg: string) => {
    setToast(msg);
    setTimeout(() => setToast(null), 3000);
  };

  const fetchMissions = async () => {
    try {
      const res = await bridge.sendRequest<MissionsResponseDto>('get_missions');
      setData(res);
    } catch (err) {
      console.error('Failed to load missions:', err);
    }
  };

  const handleClearContracts = async () => {
    try {
      await bridge.sendRequest('clear_contracts');
      await fetchMissions();
      showToast('Aktive Auftragsliste geleert.');
    } catch (err) {
      console.error('Failed to clear contracts:', err);
      showToast('Fehler beim Leeren der Aufträge');
    }
  };

  useEffect(() => {
    fetchMissions();

    const unsubHud = bridge.on('HUD_UPDATE', () => {
      fetchMissions();
    });

    const unsubMissions = bridge.on('MISSIONS_UPDATED', (payload: any) => {
      if (payload && payload.active) {
        setData(payload);
      } else {
        fetchMissions();
      }
    });

    return () => {
      unsubHud();
      unsubMissions();
    };
  }, []);

  const formatNumber = (num?: number) => {
    if (num === undefined || num === null) return '0';
    return num.toLocaleString('de-DE');
  };

  const getFilteredList = (list: MissionItemDto[]) => {
    return list.filter((m) => {
      const matchSearch =
        !search ||
        m.title.toLowerCase().includes(search.toLowerCase()) ||
        m.contractor.toLowerCase().includes(search.toLowerCase()) ||
        m.faction.toLowerCase().includes(search.toLowerCase());

      const matchType =
        typeFilter === 'all' ||
        (m.missionType && m.missionType.toLowerCase() === typeFilter.toLowerCase());

      return matchSearch && matchType;
    });
  };

  const currentList =
    activeTab === 'active'
      ? getFilteredList(data.active)
      : activeTab === 'history'
      ? getFilteredList(data.history)
      : getFilteredList(data.catalog);

  return (
    <div className="flex flex-col min-h-full space-y-4">
      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div
          onClick={() => setActiveTab('active')}
          className={`sc-glass rounded-lg p-4 border border-slate-800 sc-hud-corner cursor-pointer transition hover:border-emerald-500/50 ${
            activeTab === 'active' ? 'ring-1 ring-emerald-500/40 bg-emerald-950/15' : ''
          }`}
        >
          <div className="flex justify-between items-start">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Aktive Aufträge (Live)
            </span>
            <Radio className="w-4 h-4 text-emerald-400" />
          </div>
          <div className="mt-2 text-2xl font-bold font-mono text-emerald-300">
            {data.active.length} <span className="text-xs font-normal text-slate-400">angenommen</span>
          </div>
          <div className="mt-1 text-xs text-slate-400">Aus Game.log &amp; SQLite Master-DB</div>
        </div>

        <div
          onClick={() => setActiveTab('history')}
          className={`sc-glass rounded-lg p-4 border border-slate-800 sc-hud-corner cursor-pointer transition hover:border-cyan-500/50 ${
            activeTab === 'history' ? 'ring-1 ring-cyan-500/40 bg-cyan-950/15' : ''
          }`}
        >
          <div className="flex justify-between items-start">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Abgeschlossene Missionen
            </span>
            <CheckCircle2 className="w-4 h-4 text-cyan-400" />
          </div>
          <div className="mt-2 text-2xl font-bold font-mono text-cyan-300">
            {data.history.length} <span className="text-xs font-normal text-slate-400">im Log erfasst</span>
          </div>
          <div className="mt-1 text-xs text-slate-400">Historische Missionsabschlüsse &amp; Belohnungen</div>
        </div>

        <div
          onClick={() => setActiveTab('catalog')}
          className={`sc-glass rounded-lg p-4 border border-slate-800 sc-hud-corner cursor-pointer transition hover:border-amber-500/50 ${
            activeTab === 'catalog' ? 'ring-1 ring-amber-500/40 bg-amber-950/15' : ''
          }`}
        >
          <div className="flex justify-between items-start">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              SC Auftrags-Katalog
            </span>
            <Target className="w-4 h-4 text-amber-400" />
          </div>
          <div className="mt-2 text-2xl font-bold font-mono text-amber-300">
            {data.catalog.length} <span className="text-xs font-normal text-slate-400">Missionstypen</span>
          </div>
          <div className="mt-1 text-xs text-slate-400">CIG Spieldatenbank (Stanton &amp; Pyro)</div>
        </div>
      </div>

      {/* Subtabs Bar & Filter */}
      <div className="sc-glass rounded-lg p-3 border border-slate-800 flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          <button
            onClick={() => setActiveTab('active')}
            className={`px-3 py-1.5 text-xs font-semibold rounded transition cursor-pointer flex items-center gap-1.5 ${
              activeTab === 'active'
                ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40 shadow-[0_0_10px_rgba(0,240,255,0.2)]'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900 border border-transparent'
            }`}
          >
            <Radio className="w-3.5 h-3.5" /> Aktive Aufträge ({data.active.length})
          </button>

          <button
            onClick={() => setActiveTab('history')}
            className={`px-3 py-1.5 text-xs font-semibold rounded transition cursor-pointer flex items-center gap-1.5 ${
              activeTab === 'history'
                ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40 shadow-[0_0_10px_rgba(0,240,255,0.2)]'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900 border border-transparent'
            }`}
          >
            <Clock className="w-3.5 h-3.5" /> Verlauf ({data.history.length})
          </button>

          <button
            onClick={() => setActiveTab('catalog')}
            className={`px-3 py-1.5 text-xs font-semibold rounded transition cursor-pointer flex items-center gap-1.5 ${
              activeTab === 'catalog'
                ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40 shadow-[0_0_10px_rgba(0,240,255,0.2)]'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900 border border-transparent'
            }`}
          >
            <Target className="w-3.5 h-3.5" /> Auftragskatalog ({data.catalog.length})
          </button>
        </div>

        {/* Filter & Search */}
        <div className="flex items-center gap-2">
          <select
            value={typeFilter}
            onChange={(e) => setTypeFilter(e.target.value)}
            className="bg-slate-900/80 border border-slate-800 rounded px-2.5 py-1.5 text-xs text-slate-300 focus:outline-none focus:border-cyan-500/50 cursor-pointer font-mono"
          >
            <option value="all">Alle Typen</option>
            <option value="Bounty">Bounty</option>
            <option value="Delivery">Delivery</option>
            <option value="Mercenary">Mercenary</option>
            <option value="Salvage">Salvage</option>
            <option value="Investigation">Investigation</option>
            <option value="Mining">Mining</option>
          </select>

          <div className="relative">
            <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-500" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Auftrag filtern..."
              className="bg-slate-900/80 border border-slate-800 rounded pl-8 pr-3 py-1.5 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-cyan-500/50 w-48"
            />
          </div>

          <button
            onClick={handleClearContracts}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded bg-rose-950/40 hover:bg-rose-900/60 text-rose-300 text-xs font-semibold border border-rose-800/80 transition cursor-pointer shrink-0 ml-1"
            title="Aktive Auftragsliste leeren (behebt feststeckende Aufträge)"
          >
            <Trash2 className="w-3.5 h-3.5 text-rose-400" />
            <span>✕ Aufträge leeren</span>
          </button>
        </div>
      </div>

      {/* Main Table Content */}
      <div className="flex-1 sc-glass rounded-lg border border-slate-800 overflow-y-auto">
        <table className="w-full text-left text-xs border-collapse">
          <thead>
            <tr className="border-b border-slate-800 bg-slate-900/60 text-slate-400 font-mono uppercase tracking-wider sticky top-0 backdrop-blur-md z-10">
              <th className="py-3 px-4">Auftrag / Bezeichnung</th>
              <th className="py-3 px-4">Auftraggeber / Fraktion</th>
              <th className="py-3 px-4">Typ / System</th>
              <th className="py-3 px-4 text-right">Belohnung</th>
              <th className="py-3 px-4 text-center">Status</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800/40">
            {currentList.length === 0 ? (
              <tr>
                <td colSpan={5} className="py-16 text-center text-slate-500 font-mono">
                  Keine Aufträge in dieser Kategorie gefunden.
                </td>
              </tr>
            ) : (
              currentList.map((m) => (
                <tr key={m.id} className="hover:bg-slate-900/40 transition">
                  <td className="py-3 px-4">
                    <div className="font-semibold text-slate-200">{m.title}</div>
                    {m.description && (
                      <div className="text-[11px] text-slate-400 truncate max-w-md">
                        {m.description}
                      </div>
                    )}
                  </td>
                  <td className="py-3 px-4">
                    <div className="text-slate-300">{m.contractor || m.faction || '—'}</div>
                  </td>
                  <td className="py-3 px-4 font-mono text-[11px]">
                    <span className="text-slate-300">{m.missionType || 'Standard'}</span>
                    <span className="text-slate-500 block">{m.starSystems}</span>
                  </td>
                  <td className="py-3 px-4 text-right font-mono font-bold">
                    {m.baseReward > 0 ? (
                      <span className="text-emerald-400">+{formatNumber(m.baseReward)} aUEC</span>
                    ) : (
                      <span className="text-slate-500">Variabel</span>
                    )}
                  </td>
                  <td className="py-3 px-4 text-center">
                    {m.isActive ? (
                      <span className="sc-badge-green text-[10px]">Aktiv</span>
                    ) : m.isCompleted ? (
                      <span className="sc-badge text-[10px]">Erledigt</span>
                    ) : m.isIllegal ? (
                      <span className="sc-badge-red text-[10px]">Illegal</span>
                    ) : (
                      <span className="px-1.5 py-0.5 rounded text-[10px] bg-slate-800 text-slate-400">
                        Katalog
                      </span>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Toast Notification */}
      {toast && (
        <div className="fixed bottom-6 right-6 z-50 bg-slate-900 border border-cyan-500/60 text-cyan-300 px-4 py-2.5 rounded-lg shadow-xl text-xs font-mono flex items-center gap-2">
          <span>{toast}</span>
        </div>
      )}
    </div>
  );
};
