import React, { useEffect, useRef, useState } from 'react';
import { bridge, MissionItemDto, MissionsResponseDto } from '../services/photinoBridge';
import {
  CheckCircle2,
  Clock,
  Radio,
  Search,
  Target,
  Trash2,
  X,
} from 'lucide-react';
import { useI18n } from '../i18n';

export interface MissionsViewProps {
  initialSearch?: string;
  initialTab?: 'active' | 'history' | 'catalog';
}

export const MissionsView: React.FC<MissionsViewProps> = ({
  initialSearch,
  initialTab,
}) => {
  const { t, locale } = useI18n();
  const [data, setData] = useState<MissionsResponseDto>({
    active: [],
    history: [],
    catalog: [],
  });
  const [activeTab, setActiveTab] = useState<'active' | 'history' | 'catalog'>(initialTab || 'active');
  const [search, setSearch] = useState<string>(initialSearch || '');
  const [typeFilter, setTypeFilter] = useState<string>('all');
  const [toast, setToast] = useState<string | null>(null);

  const autoSwitchedRef = useRef<string | null>(null);

  const handleTabClick = (tab: 'active' | 'history' | 'catalog') => {
    autoSwitchedRef.current = initialSearch || '__user_selected__';
    setActiveTab(tab);
  };

  const handleClearSearch = () => {
    setSearch('');
    autoSwitchedRef.current = initialSearch || '__cleared__';
  };

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

  useEffect(() => {
    setSearch(initialSearch || '');
    if (initialTab) {
      setActiveTab(initialTab);
      autoSwitchedRef.current = initialSearch || '__explicit_tab__';
    } else if (initialSearch !== autoSwitchedRef.current) {
      autoSwitchedRef.current = null;
    }
  }, [initialSearch, initialTab]);

  useEffect(() => {
    if (
      initialSearch &&
      !initialTab &&
      autoSwitchedRef.current !== initialSearch &&
      (data.history.length > 0 || data.active.length > 0)
    ) {
      const q = initialSearch.toLowerCase();
      const inActive = data.active.some(
        (m) =>
          (m.title && m.title.toLowerCase().includes(q)) ||
          (m.contractor && m.contractor.toLowerCase().includes(q))
      );
      const inHistory = data.history.some(
        (m) =>
          (m.title && m.title.toLowerCase().includes(q)) ||
          (m.contractor && m.contractor.toLowerCase().includes(q))
      );
      if (!inActive && inHistory) {
        setActiveTab('history');
      }
      autoSwitchedRef.current = initialSearch;
    }
  }, [data, initialSearch, initialTab]);

  const formatNumber = (num?: number) => {
    if (num === undefined || num === null) return '0';
    return num.toLocaleString('de-DE');
  };

  const getFilteredList = (list: MissionItemDto[]) => {
    const q = search.trim().toLowerCase();
    return list.filter((m) => {
      const matchSearch =
        !q ||
        (m.title && m.title.toLowerCase().includes(q)) ||
        (m.contractor && m.contractor.toLowerCase().includes(q)) ||
        (m.faction && m.faction.toLowerCase().includes(q)) ||
        (m.missionType && m.missionType.toLowerCase().includes(q)) ||
        (m.description && m.description.toLowerCase().includes(q)) ||
        (m.starSystems && m.starSystems.toLowerCase().includes(q));

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
          onClick={() => handleTabClick('active')}
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
          onClick={() => handleTabClick('history')}
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
            {formatNumber(data.totalCompleted ?? data.history.length)} <span className="text-xs font-normal text-slate-400">im Log erfasst</span>
          </div>
          <div className="mt-1 text-xs text-slate-400">Historische Missionsabschlüsse &amp; Belohnungen</div>
        </div>

        <div
          onClick={() => handleTabClick('catalog')}
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
            onClick={() => handleTabClick('active')}
            className={`px-3 py-1.5 text-xs font-semibold rounded transition cursor-pointer flex items-center gap-1.5 ${
              activeTab === 'active'
                ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40 shadow-[0_0_10px_rgba(0,240,255,0.2)]'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900 border border-transparent'
            }`}
          >
            <Radio className="w-3.5 h-3.5" /> {t('missions.tabActive')} ({data.active.length})
          </button>

          <button
            onClick={() => handleTabClick('history')}
            className={`px-3 py-1.5 text-xs font-semibold rounded transition cursor-pointer flex items-center gap-1.5 ${
              activeTab === 'history'
                ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40 shadow-[0_0_10px_rgba(0,240,255,0.2)]'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900 border border-transparent'
            }`}
          >
            <Clock className="w-3.5 h-3.5" /> {t('missions.tabHistory')} ({formatNumber(data.totalCompleted ?? data.history.length)})
          </button>

          <button
            onClick={() => handleTabClick('catalog')}
            className={`px-3 py-1.5 text-xs font-semibold rounded transition cursor-pointer flex items-center gap-1.5 ${
              activeTab === 'catalog'
                ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40 shadow-[0_0_10px_rgba(0,240,255,0.2)]'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900 border border-transparent'
            }`}
          >
            <Target className="w-3.5 h-3.5" /> {t('missions.tabCatalog')} ({data.catalog.length})
          </button>
        </div>

        {/* Filter & Search */}
        <div className="flex items-center gap-2">
          <select
            value={typeFilter}
            onChange={(e) => setTypeFilter(e.target.value)}
            className="bg-slate-900/80 border border-slate-800 rounded px-2.5 py-1.5 text-xs text-slate-300 focus:outline-none focus:border-cyan-500/50 cursor-pointer font-mono"
          >
            <option value="all">{locale === 'en' ? 'All Types' : 'Alle Typen'}</option>
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
              placeholder={t('missions.searchPlaceholder')}
              className="bg-slate-900/80 border border-slate-800 rounded pl-8 pr-7 py-1.5 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-cyan-500/50 w-52"
            />
            {search && (
              <button
                type="button"
                onClick={handleClearSearch}
                className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-500 hover:text-slate-200 p-0.5 rounded cursor-pointer transition"
                title="Suche leeren"
              >
                <X className="w-3 h-3" />
              </button>
            )}
          </div>

          <button
            onClick={handleClearContracts}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded bg-rose-950/40 hover:bg-rose-900/60 text-rose-300 text-xs font-semibold border border-rose-800/80 transition cursor-pointer shrink-0 ml-1"
            title="Aktive Auftragsliste leeren (behebt feststeckende Aufträge)"
          >
            <Trash2 className="w-3.5 h-3.5 text-rose-400" />
            <span>✕ {t('missions.clearActive')}</span>
          </button>
        </div>
      </div>

      {/* Main Table Content */}
      <div className="flex-1 sc-glass rounded-lg border border-slate-800 overflow-y-auto">
        <table className="w-full text-left text-xs border-collapse">
          <thead>
            <tr className="border-b border-slate-800 bg-slate-900/60 text-slate-400 font-mono uppercase tracking-wider sticky top-0 backdrop-blur-md z-10">
              <th className="py-3 px-4">{locale === 'en' ? 'Mission / Name' : 'Auftrag / Bezeichnung'}</th>
              <th className="py-3 px-4">{locale === 'en' ? 'Contract Giver / Faction' : 'Auftraggeber / Fraktion'}</th>
              <th className="py-3 px-4">{locale === 'en' ? 'Type / System' : 'Typ / System'}</th>
              <th className="py-3 px-4 text-right">{t('hud.reward')}</th>
              <th className="py-3 px-4 text-center">{t('common.status')}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800/40">
            {currentList.length === 0 ? (
              <tr>
                <td colSpan={5} className="py-16 text-center text-slate-500 font-mono">
                  <div className="flex flex-col items-center justify-center gap-2">
                    <div>
                      {search || typeFilter !== 'all' ? (
                        <>
                          Keine Aufträge für den aktuellen Filter gefunden
                          {search && (
                            <span className="text-cyan-400 font-semibold ml-1">
                              "{search}"
                            </span>
                          )}
                          .
                        </>
                      ) : (
                        'Keine Aufträge in dieser Kategorie vorhanden.'
                      )}
                    </div>
                    {(search || typeFilter !== 'all') && (
                      <button
                        type="button"
                        onClick={() => {
                          setSearch('');
                          setTypeFilter('all');
                        }}
                        className="mt-1 px-3 py-1 bg-cyan-950/60 hover:bg-cyan-900/80 border border-cyan-700/60 hover:border-cyan-400 text-cyan-300 rounded text-xs transition cursor-pointer flex items-center gap-1.5 font-sans"
                      >
                        <X className="w-3.5 h-3.5" />
                        <span>Filter zurücksetzen</span>
                      </button>
                    )}
                  </div>
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
                        {m.stepsTotal && m.stepsTotal > 0 ? (
                          <span className="text-cyan-400 font-mono ml-2">
                            · {m.progressText || `Schritt ${m.stepsDone ?? 0} von ${m.stepsTotal}`}
                          </span>
                        ) : null}
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
                      <div className="flex flex-col items-center gap-1">
                        <span className="sc-badge-green text-[10px]">Aktiv</span>
                        {m.stepsTotal && m.stepsTotal > 0 ? (
                          <span className="text-[10px] font-mono text-cyan-300 bg-cyan-950/50 border border-cyan-800/60 px-1.5 py-0.5 rounded shadow-sm">
                            {m.progressText || `${m.stepsDone ?? 0}/${m.stepsTotal}`}
                          </span>
                        ) : null}
                      </div>
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
