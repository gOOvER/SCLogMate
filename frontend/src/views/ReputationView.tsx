import React, { useEffect, useState, useMemo } from 'react';
import { bridge, FactionReputationDto } from '../services/photinoBridge';
import {
  Award,
  Shield,
  Package,
  Pickaxe,
  Skull,
  Search,
  Check,
  RotateCcw,
  Info,
  SlidersHorizontal,
  Sparkles,
  Database,
  ChevronUp,
} from 'lucide-react';

const LEVEL_THRESHOLDS = [0, 1000, 3000, 7500, 15000, 30000];

export const ReputationView: React.FC = () => {
  const [factions, setFactions] = useState<FactionReputationDto[]>([]);
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [search, setSearch] = useState<string>('');
  const [onlyActive, setOnlyActive] = useState<boolean>(false);
  const [showInfoBanner, setShowInfoBanner] = useState<boolean>(true);

  // Calibration Drawer/Modal state
  const [calibratingFaction, setCalibratingFaction] = useState<FactionReputationDto | null>(null);
  const [calibXp, setCalibXp] = useState<number>(0);
  const [calibMissions, setCalibMissions] = useState<number>(0);
  const [savedFeedback, setSavedFeedback] = useState<string | null>(null);

  const fetchReputation = async () => {
    try {
      const res = await bridge.sendRequest<FactionReputationDto[]>('get_reputation');
      if (Array.isArray(res)) {
        setFactions(res);
      }
    } catch (err) {
      console.error('Failed to load reputation:', err);
    }
  };

  useEffect(() => {
    fetchReputation();
    const unbind = bridge.on<FactionReputationDto[]>('reputation_response', (newData) => {
      if (Array.isArray(newData)) {
        setFactions(newData);
      }
    });
    return () => unbind();
  }, []);

  // Quick set level (Rang 1 - 6) directly on card
  const handleQuickSetLevel = async (faction: FactionReputationDto, level: number) => {
    try {
      const targetXp = LEVEL_THRESHOLDS[Math.max(0, Math.min(5, level - 1))];
      await bridge.sendRequest('set_reputation', {
        factionId: faction.id,
        level,
        xp: targetXp,
        missions: Math.max(faction.completedMissions, level - 1),
      });
      fetchReputation();
    } catch (err) {
      console.error('Failed to set reputation level:', err);
    }
  };

  // Open calibration dialog
  const openCalibration = (faction: FactionReputationDto) => {
    setCalibratingFaction(faction);
    setCalibXp(faction.currentXp);
    setCalibMissions(faction.completedMissions);
    setSavedFeedback(null);
  };

  // Adjust XP in calibration dialog
  const handleAdjustXp = (delta: number) => {
    setCalibXp((prev) => Math.max(0, prev + delta));
  };

  // Apply calibration save
  const handleSaveCalibration = async () => {
    if (!calibratingFaction) return;
    try {
      await bridge.sendRequest('set_reputation', {
        factionId: calibratingFaction.id,
        xp: Math.max(0, calibXp),
        missions: Math.max(0, calibMissions),
      });
      setSavedFeedback('In SQLite gespeichert!');
      setTimeout(() => {
        setSavedFeedback(null);
        setCalibratingFaction(null);
      }, 1200);
      fetchReputation();
    } catch (err) {
      console.error('Failed to save reputation calibration:', err);
    }
  };

  // Reset faction to 0 XP
  const handleResetFaction = async (factionId: string) => {
    try {
      await bridge.sendRequest('reset_reputation', { factionId });
      setCalibXp(0);
      setCalibMissions(0);
      setSavedFeedback('Auf 0 XP zurückgesetzt!');
      setTimeout(() => setSavedFeedback(null), 1500);
      fetchReputation();
    } catch (err) {
      console.error('Failed to reset faction reputation:', err);
    }
  };

  const categories = [
    { id: 'all', label: 'Alle Organisationen', icon: Award },
    { id: 'Sicherheit', label: 'Sicherheit & Kopfgeld', icon: Shield },
    { id: 'Fracht', label: 'Fracht & Transport', icon: Package },
    { id: 'Industrie', label: 'Industrie & Bergbau', icon: Pickaxe },
    { id: 'Unterwelt', label: 'Unterwelt & Syndikate', icon: Skull },
  ];

  // Filtering
  const filtered = useMemo(() => {
    return factions.filter((f) => {
      if (selectedCategory !== 'all' && f.category !== selectedCategory) return false;
      if (onlyActive && f.currentXp === 0 && f.completedMissions === 0) return false;
      if (search.trim()) {
        const q = search.toLowerCase();
        return (
          f.name.toLowerCase().includes(q) ||
          f.shortName.toLowerCase().includes(q) ||
          f.system.toLowerCase().includes(q) ||
          f.category.toLowerCase().includes(q) ||
          f.description.toLowerCase().includes(q)
        );
      }
      return true;
    });
  }, [factions, selectedCategory, search, onlyActive]);

  // Overall KPIs
  const totalXp = useMemo(() => factions.reduce((sum, f) => sum + (f.currentXp || 0), 0), [factions]);
  const totalMissions = useMemo(() => factions.reduce((sum, f) => sum + (f.completedMissions || 0), 0), [factions]);
  const activeFactionsCount = useMemo(() => factions.filter((f) => f.currentXp > 0 || f.completedMissions > 0).length, [factions]);
  const highestLevelFaction = useMemo(() => {
    if (factions.length === 0) return null;
    return [...factions].sort((a, b) => b.currentXp - a.currentXp)[0];
  }, [factions]);

  const getCategoryColor = (cat: string) => {
    switch (cat) {
      case 'Sicherheit':
        return 'text-sky-400 border-sky-500/40 bg-sky-950/40';
      case 'Fracht':
        return 'text-amber-400 border-amber-500/40 bg-amber-950/40';
      case 'Industrie':
        return 'text-emerald-400 border-emerald-500/40 bg-emerald-950/40';
      case 'Unterwelt':
        return 'text-rose-400 border-rose-500/40 bg-rose-950/40';
      default:
        return 'text-purple-400 border-purple-500/40 bg-purple-950/40';
    }
  };

  return (
    <div className="flex flex-col min-h-full space-y-3 font-sans select-none">
      {/* ══ 1. TELEMETRY KPI SUMMARY STRIP ══ */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-2.5 shrink-0">
        {/* Karte 1: Organisationen */}
        <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-lg p-2.5 flex flex-col justify-between shadow-sm">
          <div className="flex justify-between items-start">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
              Organisationen
            </span>
            <Award className="w-3.5 h-3.5 text-cyan-400" />
          </div>
          <div className="mt-1 text-lg font-bold font-mono text-cyan-300">
            {factions.length} <span className="text-[10px] font-normal text-slate-400">Gilden & Fraktionen</span>
          </div>
          <div className="text-[10px] font-mono text-slate-500 mt-0.5">
            {activeFactionsCount} mit aktivem Status
          </div>
        </div>

        {/* Karte 2: Höchste Rufstufe */}
        <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-lg p-2.5 flex flex-col justify-between shadow-sm">
          <div className="flex justify-between items-start">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
              Höchste Rufstufe
            </span>
            <Sparkles className="w-3.5 h-3.5 text-amber-400" />
          </div>
          <div className="mt-1 text-sm font-bold font-mono text-amber-300 truncate" title={highestLevelFaction?.levelTitle}>
            {highestLevelFaction && highestLevelFaction.currentXp > 0 ? (
              <span>Rang {highestLevelFaction.currentLevel} · {highestLevelFaction.shortName}</span>
            ) : (
              <span className="text-slate-500">Noch keine Beziehung</span>
            )}
          </div>
          <div className="text-[10px] font-mono text-amber-400/80 mt-0.5 truncate">
            {highestLevelFaction && highestLevelFaction.currentXp > 0 ? highestLevelFaction.levelTitle : 'Starte Missionen'}
          </div>
        </div>

        {/* Karte 3: Gesamt-XP */}
        <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-lg p-2.5 flex flex-col justify-between shadow-sm">
          <div className="flex justify-between items-start">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
              Gesamt-Erfahrung
            </span>
            <Database className="w-3.5 h-3.5 text-emerald-400" />
          </div>
          <div className="mt-1 text-lg font-bold font-mono text-emerald-300">
            {totalXp.toLocaleString('de-DE')} <span className="text-[10px] font-normal text-emerald-500">XP</span>
          </div>
          <div className="text-[10px] font-mono text-slate-500 mt-0.5">
            Persistent in SQLite gespeichert
          </div>
        </div>

        {/* Karte 4: Abgeschlossene Aufträge */}
        <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-lg p-2.5 flex flex-col justify-between shadow-sm">
          <div className="flex justify-between items-start">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
              Aufträge Erledigt
            </span>
            <Check className="w-3.5 h-3.5 text-purple-400" />
          </div>
          <div className="mt-1 text-lg font-bold font-mono text-purple-300">
            {totalMissions} <span className="text-[10px] font-normal text-purple-500">Missionserfolge</span>
          </div>
          <div className="text-[10px] font-mono text-slate-500 mt-0.5">
            Automatische XP-Gutschrift
          </div>
        </div>
      </div>

      {/* ══ 2. DELPHI EXPLANATION BANNER (Collapsible) ══ */}
      {showInfoBanner && (
        <div className="bg-[#061426]/90 border border-cyan-900/70 rounded-lg p-2.5 px-3 flex items-start justify-between gap-3 text-xs font-mono shadow-sm">
          <div className="flex items-start gap-2.5">
            <Info className="w-4 h-4 text-cyan-400 shrink-0 mt-0.5" />
            <div className="space-y-0.5">
              <span className="font-bold text-cyan-300">
                mobiGlas Delphi-Synchronisation & dauerhafte SQLite-Speicherung
              </span>
              <p className="text-slate-300 leading-relaxed text-[11px]">
                Star Citizen protokolliert in der <code className="text-amber-300 bg-black/40 px-1 py-0.2 rounded">Game.log</code> keinen globalen Rufverlauf. Bei Spiel-Crashes oder Log-Rotationen fehlen historische Einträge. Du kannst deinen Ruf hier mit deiner in-game <strong>Delphi-App</strong> abgleichen (per Klick auf <span className="text-emerald-400 font-bold">[R1]</span> bis <span className="text-emerald-400 font-bold">[R6]</span> oder Feinjustierung). Einmal kalibriert, bleibt dein Ruf dauerhaft in SCLogMate gesichert und wächst bei jeder live abgeschlossenen Mission automatisch weiter.
              </p>
            </div>
          </div>
          <button
            onClick={() => setShowInfoBanner(false)}
            className="text-slate-500 hover:text-slate-300 p-1 cursor-pointer shrink-0"
            title="Hinweis ausblenden"
          >
            <ChevronUp className="w-3.5 h-3.5" />
          </button>
        </div>
      )}

      {/* ══ 3. TOOLBAR: CATEGORIES & SEARCH & ACTIVE FILTER ══ */}
      <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between border-b border-cyan-950/80 bg-[#040914] px-3 py-1.5 shrink-0 rounded-t-lg gap-2">
        <div className="flex items-center gap-1.5 overflow-x-auto no-scrollbar">
          {categories.map((c) => {
            const Icon = c.icon;
            const count = c.id === 'all' ? factions.length : factions.filter((f) => f.category === c.id).length;
            return (
              <button
                key={c.id}
                onClick={() => setSelectedCategory(c.id)}
                className={`px-2.5 py-1 text-xs font-mono font-semibold rounded transition cursor-pointer shrink-0 flex items-center gap-1.5 ${
                  selectedCategory === c.id
                    ? 'bg-cyan-950/80 text-cyan-300 border border-cyan-500/70 shadow-[0_0_8px_rgba(6,182,212,0.25)]'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900/60 border border-transparent'
                }`}
              >
                <Icon className="w-3 h-3 text-cyan-400" />
                <span>{c.label}</span>
                <span className="text-[10px] text-slate-500">({count})</span>
              </button>
            );
          })}
        </div>

        <div className="flex items-center gap-2">
          {/* Only Active Filter */}
          <button
            onClick={() => setOnlyActive(!onlyActive)}
            className={`px-2 py-1 rounded text-xs font-mono transition cursor-pointer border flex items-center gap-1.5 ${
              onlyActive
                ? 'bg-emerald-950/80 border-emerald-600 text-emerald-300 font-semibold'
                : 'bg-[#071322] border-cyan-950 text-slate-400 hover:text-slate-200'
            }`}
            title="Nur Fraktionen mit bereits erfahrener Reputation (XP > 0) anzeigen"
          >
            <span className={`w-1.5 h-1.5 rounded-full ${onlyActive ? 'bg-emerald-400' : 'bg-slate-600'}`} />
            <span>Nur aktive</span>
          </button>

          {/* Search Box */}
          <div className="relative flex items-center min-w-[160px] sm:min-w-[200px]">
            <Search className="w-3.5 h-3.5 text-slate-500 absolute left-2.5 pointer-events-none" />
            <input
              type="text"
              placeholder="Fraktion, System..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full pl-8 pr-2.5 py-1 text-xs font-mono bg-[#030a16] border border-cyan-950 rounded text-slate-200 placeholder-slate-600 focus:outline-none focus:border-cyan-600"
            />
          </div>
        </div>
      </div>

      {/* ══ 4. FACTION CARDS GRID ══ */}
      <div className="flex-1 bg-[#040914]/90 rounded-b-lg border border-cyan-950/80 overflow-y-auto p-3 shadow-sm min-h-[400px]">
        {filtered.length === 0 ? (
          <div className="h-44 flex flex-col items-center justify-center text-xs font-mono text-slate-500 space-y-1">
            <Award className="w-6 h-6 text-slate-600 mb-1" />
            <div>Keine Organisationen für diesen Filter gefunden.</div>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3">
            {filtered.map((f) => {
              const catClass = getCategoryColor(f.category);

              return (
                <div
                  key={f.id}
                  className="bg-[#030c1a]/90 border border-cyan-950/90 rounded-lg p-3 flex flex-col justify-between hover:border-cyan-700/60 transition shadow-sm group"
                >
                  <div>
                    {/* Header: Icon, Name, Category & Level */}
                    <div className="flex items-start justify-between gap-2">
                      <div className="flex items-center gap-2.5 min-w-0">
                        <div className="w-8 h-8 rounded bg-[#061426] border border-cyan-900/60 text-cyan-300 flex items-center justify-center text-base shadow-sm shrink-0">
                          {f.icon || '🛡'}
                        </div>
                        <div className="min-w-0">
                          <h3 className="font-bold text-xs font-mono text-slate-100 group-hover:text-cyan-300 transition truncate" title={f.name}>
                            {f.name}
                          </h3>
                          <div className="text-[10px] text-slate-400 font-mono flex items-center gap-1.5 mt-0.5">
                            <span className={`px-1.5 py-0.2 rounded border text-[9px] ${catClass}`}>
                              {f.category}
                            </span>
                            <span className="text-slate-600">·</span>
                            <span className="truncate text-slate-400">{f.system}</span>
                          </div>
                        </div>
                      </div>

                      {/* Rank Badge */}
                      <div className="flex flex-col items-end shrink-0">
                        <span className="px-2 py-0.5 rounded font-mono font-bold text-[10px] bg-cyan-950/80 border border-cyan-700 text-cyan-300 shadow-sm">
                          Rang {f.currentLevel}
                        </span>
                      </div>
                    </div>

                    {/* Description */}
                    <p className="text-[11px] text-slate-400 mt-2 leading-relaxed line-clamp-2" title={f.description}>
                      {f.description}
                    </p>
                  </div>

                  {/* Level Progress & Telemetry */}
                  <div className="mt-3 pt-2.5 border-t border-cyan-950/80 space-y-2">
                    <div className="flex justify-between items-center text-xs font-mono">
                      <span className="text-amber-300 font-bold text-[11px] truncate" title={f.levelTitle}>
                        {f.levelTitle}
                      </span>
                      <span className="text-slate-400 text-[10px] shrink-0 font-semibold">
                        {f.progressText}
                      </span>
                    </div>

                    {/* Progress Bar */}
                    <div className="w-full h-2 rounded bg-[#020712] border border-cyan-950 overflow-hidden relative shadow-inner">
                      <div
                        className="h-full rounded bg-gradient-to-r from-cyan-500 via-sky-400 to-amber-400 transition-all duration-500 shadow-[0_0_8px_rgba(6,182,212,0.4)]"
                        style={{ width: `${Math.min(100, Math.max(0, f.progressPercent))}%` }}
                      />
                    </div>

                    {/* Stats strip */}
                    <div className="flex items-center justify-between text-[10px] text-slate-500 font-mono pt-0.5">
                      <span>{f.completedMissions} {f.completedMissions === 1 ? 'Auftrag' : 'Aufträge'}</span>
                      <span className="text-cyan-400 font-semibold">{f.currentXp.toLocaleString('de-DE')} XP</span>
                    </div>

                    {/* ══ MOBIGLAS DELPHI QUICK TIER SELECTOR ══ */}
                    <div className="pt-1.5 border-t border-cyan-950/60 flex items-center justify-between gap-1">
                      <div className="flex items-center gap-1">
                        <span className="text-[9px] font-mono text-slate-500 uppercase tracking-wider mr-1">
                          Delphi:
                        </span>
                        {[1, 2, 3, 4, 5, 6].map((lvl) => (
                          <button
                            key={lvl}
                            onClick={() => handleQuickSetLevel(f, lvl)}
                            title={`Direkt auf Rang ${lvl} (${LEVEL_THRESHOLDS[lvl - 1].toLocaleString('de-DE')} XP) stellen`}
                            className={`w-5 h-5 rounded text-[9px] font-mono font-bold flex items-center justify-center cursor-pointer transition ${
                              f.currentLevel === lvl
                                ? 'bg-cyan-500 text-black shadow-[0_0_8px_rgba(6,182,212,0.5)]'
                                : 'bg-[#061426] border border-cyan-950 text-slate-400 hover:text-cyan-300 hover:border-cyan-700'
                            }`}
                          >
                            {lvl}
                          </button>
                        ))}
                      </div>

                      <button
                        onClick={() => openCalibration(f)}
                        title="Detaillierte mobiGlas Delphi-Kalibrierung (XP Feinjustierung & Zähler)"
                        className="flex items-center gap-1 px-2 py-0.5 rounded bg-[#061426] border border-cyan-950 hover:border-cyan-700 text-[10px] font-mono text-cyan-300 transition cursor-pointer shrink-0"
                      >
                        <SlidersHorizontal className="w-3 h-3 text-cyan-400" />
                        <span>Kalibrieren</span>
                      </button>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* ══ 5. DELPHI CALIBRATION MODAL ══ */}
      {calibratingFaction && (
        <div className="fixed inset-0 z-50 bg-black/75 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-[#040e1e] border border-cyan-800/90 rounded-lg p-5 max-w-md w-full shadow-[0_0_30px_rgba(0,240,255,0.15)] space-y-4 font-mono text-xs">
            {/* Modal Header */}
            <div className="flex items-start justify-between border-b border-cyan-950 pb-3">
              <div className="flex items-center gap-2.5">
                <div className="w-9 h-9 rounded bg-[#061426] border border-cyan-700 text-cyan-300 flex items-center justify-center text-lg shadow-sm">
                  {calibratingFaction.icon || '🛡'}
                </div>
                <div>
                  <h3 className="font-bold text-sm text-cyan-300">{calibratingFaction.name}</h3>
                  <div className="text-[10px] text-slate-400">{calibratingFaction.system} · {calibratingFaction.category}</div>
                </div>
              </div>
              <button
                onClick={() => setCalibratingFaction(null)}
                className="text-slate-400 hover:text-white cursor-pointer px-2 py-1 text-sm font-bold"
              >
                ✕
              </button>
            </div>

            {/* Delphi Rank Presets */}
            <div className="space-y-1.5">
              <span className="text-[11px] font-bold text-slate-300 uppercase tracking-wider">
                1. Rufstufe (Delphi Delphi-Rang wählen)
              </span>
              <div className="grid grid-cols-3 gap-1.5">
                {[
                  { lvl: 1, xp: 0, title: 'Rang 1' },
                  { lvl: 2, xp: 1000, title: 'Rang 2' },
                  { lvl: 3, xp: 3000, title: 'Rang 3' },
                  { lvl: 4, xp: 7500, title: 'Rang 4' },
                  { lvl: 5, xp: 15000, title: 'Rang 5' },
                  { lvl: 6, xp: 30000, title: 'Rang 6' },
                ].map((item) => (
                  <button
                    key={item.lvl}
                    onClick={() => setCalibXp(item.xp)}
                    className={`px-2 py-1.5 rounded border text-left cursor-pointer transition ${
                      calibXp >= item.xp && (item.lvl === 6 || calibXp < LEVEL_THRESHOLDS[item.lvl])
                        ? 'bg-cyan-950 text-cyan-300 border-cyan-500 font-bold shadow-[0_0_8px_rgba(6,182,212,0.3)]'
                        : 'bg-[#061426] border-cyan-950 text-slate-400 hover:text-slate-200'
                    }`}
                  >
                    <div className="text-[11px]">{item.title}</div>
                    <div className="text-[9px] text-slate-500">{item.xp.toLocaleString('de-DE')} XP</div>
                  </button>
                ))}
              </div>
            </div>

            {/* Exact XP Input & Steppers */}
            <div className="space-y-1.5 pt-2 border-t border-cyan-950">
              <div className="flex justify-between items-center">
                <span className="text-[11px] font-bold text-slate-300 uppercase tracking-wider">
                  2. Exakte XP & Feinjustierung
                </span>
                <span className="text-cyan-400 font-bold">{calibXp.toLocaleString('de-DE')} XP</span>
              </div>

              {/* Stepper Buttons */}
              <div className="flex items-center gap-1.5">
                {[
                  { label: '-1.000', delta: -1000 },
                  { label: '-250', delta: -250 },
                  { label: '+250', delta: 250 },
                  { label: '+500', delta: 500 },
                  { label: '+1.000', delta: 1000 },
                ].map((b) => (
                  <button
                    key={b.label}
                    onClick={() => handleAdjustXp(b.delta)}
                    className="flex-1 py-1 rounded bg-[#061426] border border-cyan-950 hover:border-cyan-700 text-[10px] text-slate-300 hover:text-cyan-300 cursor-pointer transition"
                  >
                    {b.label}
                  </button>
                ))}
              </div>

              {/* Number Input */}
              <div className="grid grid-cols-2 gap-2 mt-2">
                <div>
                  <label className="text-[10px] text-slate-400 block mb-1">XP manuell eingeben:</label>
                  <input
                    type="number"
                    min="0"
                    step="50"
                    value={calibXp}
                    onChange={(e) => setCalibXp(Math.max(0, parseInt(e.target.value) || 0))}
                    className="w-full px-2.5 py-1 bg-[#020712] border border-cyan-950 rounded text-cyan-300 font-bold focus:outline-none focus:border-cyan-500 text-xs"
                  />
                </div>
                <div>
                  <label className="text-[10px] text-slate-400 block mb-1">Erledigte Aufträge:</label>
                  <input
                    type="number"
                    min="0"
                    value={calibMissions}
                    onChange={(e) => setCalibMissions(Math.max(0, parseInt(e.target.value) || 0))}
                    className="w-full px-2.5 py-1 bg-[#020712] border border-cyan-950 rounded text-slate-200 focus:outline-none focus:border-cyan-500 text-xs"
                  />
                </div>
              </div>
            </div>

            {/* Feedback message */}
            {savedFeedback && (
              <div className="p-2 rounded bg-emerald-950/80 border border-emerald-500/80 text-emerald-300 text-center font-bold animate-fade-in flex items-center justify-center gap-1.5">
                <Check className="w-3.5 h-3.5 text-emerald-400" />
                <span>{savedFeedback}</span>
              </div>
            )}

            {/* Actions */}
            <div className="flex items-center justify-between pt-3 border-t border-cyan-950">
              <button
                onClick={() => handleResetFaction(calibratingFaction.id)}
                className="flex items-center gap-1 px-2.5 py-1.5 rounded bg-rose-950/40 border border-rose-900/60 hover:border-rose-700 text-rose-300 text-xs transition cursor-pointer"
                title="Diesen Fraktionsruf auf 0 XP zurücksetzen"
              >
                <RotateCcw className="w-3.5 h-3.5 text-rose-400" />
                <span>Zurücksetzen</span>
              </button>

              <div className="flex items-center gap-2">
                <button
                  onClick={() => setCalibratingFaction(null)}
                  className="px-3 py-1.5 rounded border border-slate-800 text-slate-400 hover:text-slate-200 hover:bg-slate-900 cursor-pointer transition text-xs"
                >
                  Abbrechen
                </button>
                <button
                  onClick={handleSaveCalibration}
                  className="flex items-center gap-1.5 px-3 py-1.5 rounded bg-cyan-600 hover:bg-cyan-500 text-black font-bold cursor-pointer transition shadow-[0_0_10px_rgba(6,182,212,0.4)] text-xs"
                >
                  <Check className="w-3.5 h-3.5" />
                  <span>In SQLite sichern</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
