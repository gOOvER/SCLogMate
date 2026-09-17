import React, { useEffect, useState, useMemo } from 'react';
import {
  bridge,
  BlueprintDto,
  BlueprintCoverageReport,
  ScmdbImportResult,
  ScmdbExportResult,
} from '../services/photinoBridge';
import {
  Scroll,
  CheckCircle2,
  AlertCircle,
  Search,
  Hammer,
  Shield,
  Crosshair,
  Cpu,
  Layers,
  Sparkles,
  Upload,
  Download,
  Copy,
  Check,
  X,
  Network,
  BarChart3,
  Target,
  Award,
  RefreshCw,
  ExternalLink,
} from 'lucide-react';

export const BlueprintsView: React.FC = () => {
  const [blueprints, setBlueprints] = useState<BlueprintDto[]>([]);
  const [coverage, setCoverage] = useState<BlueprintCoverageReport | null>(null);
  const [activeTab, setActiveTab] = useState<'catalog' | 'coverage'>('catalog');
  const [search, setSearch] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'learned' | 'missing'>('all');
  const [categoryFilter, setCategoryFilter] = useState<string>('all');
  const [loading, setLoading] = useState<boolean>(false);

  // SCMDB Modal States
  const [showImportModal, setShowImportModal] = useState<boolean>(false);
  const [showExportModal, setShowExportModal] = useState<boolean>(false);
  const [importJsonText, setImportJsonText] = useState<string>('');
  const [importPreview, setImportPreview] = useState<ScmdbImportResult | null>(null);
  const [importError, setImportError] = useState<string | null>(null);
  const [isImporting, setIsImporting] = useState<boolean>(false);
  const [exportData, setExportData] = useState<ScmdbExportResult | null>(null);
  const [copiedExport, setCopiedExport] = useState<boolean>(false);
  const [togglingName, setTogglingName] = useState<string | null>(null);

  const fetchBlueprints = async () => {
    try {
      const res = await bridge.sendRequest<BlueprintDto[]>('get_blueprints');
      setBlueprints(res || []);
    } catch (err) {
      console.error('Failed to load blueprints:', err);
    }
  };

  const fetchCoverage = async () => {
    try {
      const res = await bridge.sendRequest<BlueprintCoverageReport>('get_blueprint_coverage');
      setCoverage(res);
    } catch (err) {
      console.error('Failed to load blueprint coverage:', err);
    }
  };

  const reloadAll = async () => {
    setLoading(true);
    await Promise.all([fetchBlueprints(), fetchCoverage()]);
    setLoading(false);
  };

  useEffect(() => {
    reloadAll();

    const unbind = bridge.on('blueprints_response', (data: any) => {
      if (Array.isArray(data)) {
        setBlueprints(data);
        fetchCoverage();
      }
    });

    return () => unbind();
  }, []);

  // Compute quick stats
  const totalCount = blueprints.length;
  const learnedCount = blueprints.filter((b) => b.isLearned).length;
  const missingCount = totalCount - learnedCount;
  const progressPercent = totalCount > 0 ? Math.round((learnedCount / totalCount) * 100) : 0;

  // Extract distinct categories
  const categories = useMemo(() => {
    const cats = new Set<string>();
    blueprints.forEach((b) => {
      if (b.category) cats.add(b.category);
    });
    return Array.from(cats).sort();
  }, [blueprints]);

  // Filtered blueprints
  const filteredBlueprints = useMemo(() => {
    return blueprints.filter((b) => {
      if (statusFilter === 'learned' && !b.isLearned) return false;
      if (statusFilter === 'missing' && b.isLearned) return false;
      if (categoryFilter !== 'all' && b.category !== categoryFilter) return false;

      if (search.trim()) {
        const query = search.toLowerCase();
        const matchesName = b.name.toLowerCase().includes(query);
        const matchesSubCat = b.subCategory?.toLowerCase().includes(query);
        const matchesUnlock = b.unlockInfo?.toLowerCase().includes(query);
        const matchesMats = b.requiredMaterials?.toLowerCase().includes(query);
        const matchesSource = b.source?.toLowerCase().includes(query);
        return matchesName || matchesSubCat || matchesUnlock || matchesMats || matchesSource;
      }

      return true;
    });
  }, [blueprints, statusFilter, categoryFilter, search]);

  // Direct toggle on card
  const handleToggleLearned = async (bp: BlueprintDto) => {
    try {
      setTogglingName(bp.name);
      const nextState = !bp.isLearned;
      await bridge.sendRequest('toggle_blueprint_learned', {
        name: bp.name,
        isLearned: nextState,
      });

      // Optimistic update
      setBlueprints((prev) =>
        prev.map((item) =>
          item.name === bp.name
            ? { ...item, isLearned: nextState, source: nextState ? 'Manuell' : undefined }
            : item
        )
      );
      fetchCoverage();
    } catch (err) {
      console.error('Failed to toggle blueprint:', err);
    } finally {
      setTogglingName(null);
    }
  };

  // SCMDB Import Handling
  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > 5 * 1024 * 1024) {
      setImportError('Datei überschreitet die maximale Dateigröße von 5 MB.');
      return;
    }

    const reader = new FileReader();
    reader.onload = (event) => {
      const text = event.target?.result as string;
      setImportJsonText(text);
      analyzeImport(text);
    };
    reader.readAsText(file);
  };

  const analyzeImport = async (text: string) => {
    setImportError(null);
    setImportPreview(null);
    if (!text.trim()) return;

    try {
      const res = await bridge.sendRequest<ScmdbImportResult>('import_scmdb_json', {
        json: text,
        apply: false,
      });

      if (!res.success) {
        setImportError(res.error || 'Ungültiges SCMDB JSON Format.');
      } else {
        setImportPreview(res);
      }
    } catch (err: any) {
      setImportError(err.message || 'Fehler beim Parsen der Datei.');
    }
  };

  const executeImport = async () => {
    if (!importJsonText) return;
    setIsImporting(true);
    setImportError(null);

    try {
      const res = await bridge.sendRequest<ScmdbImportResult>('import_scmdb_json', {
        json: importJsonText,
        apply: true,
      });

      if (!res.success) {
        setImportError(res.error || 'Import fehlgeschlagen.');
      } else {
        await reloadAll();
        setShowImportModal(false);
        setImportPreview(null);
        setImportJsonText('');
      }
    } catch (err: any) {
      setImportError(err.message || 'Fehler bei der Ausführung des Imports.');
    } finally {
      setIsImporting(false);
    }
  };

  // SCMDB Export Handling
  const handleOpenExport = async () => {
    try {
      const res = await bridge.sendRequest<ScmdbExportResult>('export_scmdb_json');
      setExportData(res);
      setShowExportModal(true);
      setCopiedExport(false);
    } catch (err) {
      console.error('Export failed:', err);
    }
  };

  const handleDownloadExport = () => {
    if (!exportData?.json) return;
    const blob = new Blob([exportData.json], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `scmdb-export-sclogmate-${new Date().toISOString().slice(0, 10)}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  const handleCopyExport = () => {
    if (!exportData?.json) return;
    navigator.clipboard.writeText(exportData.json);
    setCopiedExport(true);
    setTimeout(() => setCopiedExport(false), 3000);
  };

  const getRarityBadge = (rarity: string) => {
    const r = (rarity || 'Common').toLowerCase();
    if (r.includes('legendary') || r.includes('legende')) {
      return 'bg-amber-500/20 text-amber-300 border-amber-500/40 shadow-[0_0_10px_rgba(245,158,11,0.2)]';
    }
    if (r.includes('epic') || r.includes('episch')) {
      return 'bg-purple-500/20 text-purple-300 border-purple-500/40 shadow-[0_0_10px_rgba(168,85,247,0.2)]';
    }
    if (r.includes('rare') || r.includes('selten')) {
      return 'bg-blue-500/20 text-blue-300 border-blue-500/40 shadow-[0_0_10px_rgba(59,130,246,0.2)]';
    }
    return 'bg-slate-800 text-slate-300 border-slate-700';
  };

  const getCategoryIcon = (category: string) => {
    const c = (category || '').toLowerCase();
    if (c.includes('waffe') || c.includes('weapon')) return <Crosshair className="w-4 h-4 text-rose-400" />;
    if (c.includes('rüstung') || c.includes('armor')) return <Shield className="w-4 h-4 text-cyan-400" />;
    if (c.includes('komponente') || c.includes('component')) return <Cpu className="w-4 h-4 text-amber-400" />;
    if (c.includes('werkzeug') || c.includes('tool')) return <Hammer className="w-4 h-4 text-emerald-400" />;
    return <Layers className="w-4 h-4 text-slate-400" />;
  };

  return (
    <div className="flex flex-col min-h-full space-y-4">
      {/* Top Main Navigation & Quick Action Bar */}
      <div className="sc-glass rounded-lg p-3 border border-slate-800 flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          {/* Main Tab Switcher */}
          <div className="flex items-center bg-slate-900/90 p-1 rounded-md border border-slate-800">
            <button
              onClick={() => setActiveTab('catalog')}
              className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded cursor-pointer transition ${
                activeTab === 'catalog'
                  ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40 shadow-[0_0_8px_rgba(0,240,255,0.2)]'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              <Scroll className="w-3.5 h-3.5" />
              Bauplan-Katalog ({totalCount})
            </button>
            <button
              onClick={() => setActiveTab('coverage')}
              className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded cursor-pointer transition ${
                activeTab === 'coverage'
                  ? 'bg-gradient-to-r from-cyan-500/20 to-emerald-500/20 text-emerald-300 border border-emerald-500/40 shadow-[0_0_8px_rgba(16,185,129,0.2)]'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              <Network className="w-3.5 h-3.5 text-emerald-400" />
              Netzwerk & Org-Abdeckung
            </button>
          </div>

          <button
            onClick={reloadAll}
            disabled={loading}
            title="Aktualisieren"
            className="p-1.5 rounded bg-slate-900/90 border border-slate-700 text-slate-400 hover:text-slate-200 cursor-pointer transition"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin text-cyan-400' : ''}`} />
          </button>
        </div>

        {/* SCMDB Sync Actions */}
        <div className="flex items-center gap-2">
          <button
            onClick={() => setShowImportModal(true)}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-md bg-slate-900/90 hover:bg-slate-800 border border-slate-700 hover:border-cyan-500/50 text-xs font-semibold text-slate-200 hover:text-cyan-300 cursor-pointer transition shadow-sm"
          >
            <Upload className="w-3.5 h-3.5 text-cyan-400" />
            SCMDB Importieren
          </button>

          <button
            onClick={handleOpenExport}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-md bg-cyan-500/10 hover:bg-cyan-500/20 border border-cyan-500/30 hover:border-cyan-500/60 text-xs font-semibold text-cyan-300 cursor-pointer transition shadow-[0_0_10px_rgba(0,240,255,0.1)]"
          >
            <Download className="w-3.5 h-3.5 text-cyan-400" />
            SCMDB Exportieren
          </button>
        </div>
      </div>

      {/* Stats Quickbar */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
        <div className="sc-glass rounded-lg p-3 border border-slate-800 flex items-center justify-between">
          <div>
            <div className="text-xs text-slate-400 font-mono tracking-wider">GESAMT BAUPLÄNE</div>
            <div className="text-xl font-bold text-slate-100 mt-0.5">{totalCount}</div>
          </div>
          <div className="p-2.5 rounded-lg bg-cyan-500/10 text-cyan-400 border border-cyan-500/20">
            <Scroll className="w-5 h-5" />
          </div>
        </div>

        <div className="sc-glass rounded-lg p-3 border border-slate-800 flex items-center justify-between">
          <div>
            <div className="text-xs text-slate-400 font-mono tracking-wider">ERLERNT / FREIGESCHALTET</div>
            <div className="text-xl font-bold text-emerald-400 mt-0.5">{learnedCount}</div>
          </div>
          <div className="p-2.5 rounded-lg bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
            <CheckCircle2 className="w-5 h-5" />
          </div>
        </div>

        <div className="sc-glass rounded-lg p-3 border border-slate-800 flex items-center justify-between">
          <div>
            <div className="text-xs text-slate-400 font-mono tracking-wider">FEHLEND (GAPS)</div>
            <div className="text-xl font-bold text-slate-400 mt-0.5">{missingCount}</div>
          </div>
          <div className="p-2.5 rounded-lg bg-slate-800 text-slate-400 border border-slate-700">
            <AlertCircle className="w-5 h-5" />
          </div>
        </div>

        <div className="sc-glass rounded-lg p-3 border border-slate-800 flex flex-col justify-between">
          <div className="flex items-center justify-between">
            <div className="text-xs text-slate-400 font-mono tracking-wider">ABDECKUNG</div>
            <span className="text-sm font-mono font-bold text-cyan-400">{progressPercent}%</span>
          </div>
          <div className="w-full bg-slate-800/80 rounded-full h-2 mt-2 overflow-hidden border border-slate-700">
            <div
              className="bg-gradient-to-r from-cyan-500 to-emerald-400 h-full rounded-full transition-all duration-500 shadow-[0_0_10px_rgba(0,240,255,0.4)]"
              style={{ width: `${progressPercent}%` }}
            />
          </div>
        </div>
      </div>

      {/* Tab 1: Standard Catalog Grid */}
      {activeTab === 'catalog' && (
        <>
          {/* Filter & Search Bar */}
          <div className="sc-glass rounded-lg p-3 border border-slate-800 flex flex-wrap items-center justify-between gap-3">
            <div className="flex items-center gap-2">
              {/* Status buttons */}
              <div className="flex items-center bg-slate-900/80 p-1 rounded-md border border-slate-800">
                <button
                  onClick={() => setStatusFilter('all')}
                  className={`px-3 py-1 text-xs font-semibold rounded cursor-pointer transition ${
                    statusFilter === 'all'
                      ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40 shadow-[0_0_8px_rgba(0,240,255,0.2)]'
                      : 'text-slate-400 hover:text-slate-200'
                  }`}
                >
                  Alle ({totalCount})
                </button>
                <button
                  onClick={() => setStatusFilter('learned')}
                  className={`px-3 py-1 text-xs font-semibold rounded cursor-pointer transition ${
                    statusFilter === 'learned'
                      ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/40 shadow-[0_0_8px_rgba(16,185,129,0.2)]'
                      : 'text-slate-400 hover:text-slate-200'
                  }`}
                >
                  Erlernt ({learnedCount})
                </button>
                <button
                  onClick={() => setStatusFilter('missing')}
                  className={`px-3 py-1 text-xs font-semibold rounded cursor-pointer transition ${
                    statusFilter === 'missing'
                      ? 'bg-slate-700 text-slate-200 border border-slate-600'
                      : 'text-slate-400 hover:text-slate-200'
                  }`}
                >
                  Fehlend ({missingCount})
                </button>
              </div>

              {/* Category dropdown */}
              <select
                value={categoryFilter}
                onChange={(e) => setCategoryFilter(e.target.value)}
                className="bg-slate-900/90 border border-slate-700 rounded-md px-3 py-1.5 text-xs text-slate-200 focus:outline-none focus:border-cyan-500 font-mono cursor-pointer"
              >
                <option value="all">Alle Kategorien</option>
                {categories.map((cat) => (
                  <option key={cat} value={cat}>
                    {cat}
                  </option>
                ))}
              </select>
            </div>

            <div className="flex items-center gap-2">
              {/* Search box */}
              <div className="relative">
                <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-500" />
                <input
                  type="text"
                  placeholder="Bauplan oder Material suchen..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  className="bg-slate-900/90 border border-slate-700 rounded-md pl-8 pr-3 py-1.5 text-xs text-slate-200 focus:outline-none focus:border-cyan-500 w-48 lg:w-64 placeholder:text-slate-600 font-mono"
                />
              </div>
            </div>
          </div>

          {/* Grid of Blueprints */}
          <div className="flex-1 overflow-y-auto pr-1">
            {filteredBlueprints.length === 0 ? (
              <div className="sc-glass rounded-lg p-12 border border-slate-800 text-center flex flex-col items-center justify-center space-y-3">
                <Scroll className="w-12 h-12 text-slate-600" />
                <div className="text-slate-300 font-medium">Keine Baupläne gefunden</div>
                <div className="text-xs text-slate-500 max-w-md">
                  Es gibt keine Einträge, die den ausgewählten Kriterien entsprechen. Ändere die Filter oder starte einen SCMDB-Import.
                </div>
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3">
                {filteredBlueprints.map((bp) => {
                  const isBusy = togglingName === bp.name;

                  return (
                    <div
                      key={bp.id}
                      className={`sc-glass rounded-lg p-3.5 border transition-all duration-200 flex flex-col justify-between relative overflow-hidden group ${
                        bp.isLearned
                          ? 'border-emerald-500/30 hover:border-emerald-500/60 shadow-[0_0_15px_rgba(16,185,129,0.05)]'
                          : 'border-slate-800 hover:border-slate-700 opacity-85 hover:opacity-100'
                      }`}
                    >
                      {/* Subtle Sci-Fi background indicator */}
                      <div
                        className={`absolute top-0 right-0 w-24 h-24 blur-2xl rounded-full pointer-events-none transition-opacity ${
                          bp.isLearned ? 'bg-emerald-500/10' : 'bg-slate-700/5'
                        }`}
                      />

                      <div>
                        {/* Header: Category + Badges */}
                        <div className="flex items-center justify-between gap-2 mb-2">
                          <div className="flex items-center gap-1.5 text-xs text-slate-400">
                            {getCategoryIcon(bp.category)}
                            <span className="font-semibold text-slate-300">{bp.category}</span>
                            {bp.subCategory && (
                              <>
                                <span className="text-slate-600">/</span>
                                <span className="text-slate-400">{bp.subCategory}</span>
                              </>
                            )}
                          </div>

                          <div className="flex items-center gap-1.5 shrink-0">
                            {bp.rarity && (
                              <span
                                className={`px-2 py-0.5 text-[10px] font-mono uppercase tracking-wider rounded border ${getRarityBadge(
                                  bp.rarity
                                )}`}
                              >
                                {bp.rarity}
                              </span>
                            )}

                            {/* Clickable Status Toggle Badge */}
                            <button
                              onClick={() => handleToggleLearned(bp)}
                              disabled={isBusy}
                              title={bp.isLearned ? 'Klicken, um als fehlend zu markieren' : 'Klicken, um als erlernt zu markieren'}
                              className={`flex items-center gap-1 px-2 py-0.5 text-[10px] font-semibold rounded cursor-pointer transition border ${
                                bp.isLearned
                                  ? 'bg-emerald-500/20 hover:bg-rose-500/20 text-emerald-300 hover:text-rose-300 border-emerald-500/40 hover:border-rose-500/40'
                                  : 'bg-slate-800/80 hover:bg-emerald-500/20 text-slate-400 hover:text-emerald-300 border-slate-700 hover:border-emerald-500/40'
                              }`}
                            >
                              {bp.isLearned ? (
                                <>
                                  <CheckCircle2 className="w-3 h-3 text-emerald-400" />
                                  <span>Erlernt</span>
                                </>
                              ) : (
                                <>
                                  <span className="text-slate-500">○</span>
                                  <span>Fehlt</span>
                                </>
                              )}
                            </button>
                          </div>
                        </div>

                        {/* Blueprint Title */}
                        <div className="flex items-start justify-between gap-2">
                          <h3 className="text-sm font-bold text-slate-100 group-hover:text-cyan-300 transition-colors">
                            {bp.name}
                          </h3>
                          {bp.source && (
                            <span className="shrink-0 px-1.5 py-0.5 text-[9px] font-mono rounded bg-slate-900/80 text-slate-400 border border-slate-800">
                              {bp.source}
                            </span>
                          )}
                        </div>

                        {/* Unlock Info / Source */}
                        {bp.unlockInfo && (
                          <div className="mt-2 text-xs text-slate-400 flex items-start gap-1.5 bg-slate-900/60 p-2 rounded border border-slate-800/80">
                            <Sparkles className="w-3.5 h-3.5 text-amber-400 shrink-0 mt-0.5" />
                            <span className="text-slate-300 leading-relaxed">{bp.unlockInfo}</span>
                          </div>
                        )}

                        {/* Required Materials */}
                        {bp.requiredMaterials && (
                          <div className="mt-2">
                            <div className="text-[10px] font-mono text-slate-500 uppercase tracking-wider mb-1">
                              Benötigte Materialien
                            </div>
                            <div className="text-xs text-slate-300 font-mono bg-slate-950/40 p-1.5 rounded border border-slate-800/50">
                              {bp.requiredMaterials}
                            </div>
                          </div>
                        )}
                      </div>

                      {/* Footer date if learned */}
                      {bp.isLearned && bp.learnedDate && (
                        <div className="mt-3 pt-2 border-t border-slate-800/80 flex items-center justify-between text-[11px] font-mono text-slate-500">
                          <span>Freigeschaltet:</span>
                          <span className="text-emerald-400/90">{bp.learnedDate}</span>
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </>
      )}

      {/* Tab 2: Blueprint Network & Org Coverage Gap Analysis */}
      {activeTab === 'coverage' && coverage && (
        <div className="flex-1 overflow-y-auto space-y-4 pr-1">
          {/* Org Specialization Readiness Ladder */}
          <div className="sc-glass rounded-lg p-4 border border-slate-800">
            <div className="flex items-center justify-between mb-3">
              <div className="flex items-center gap-2">
                <Award className="w-5 h-5 text-amber-400" />
                <h2 className="text-base font-bold text-slate-100">Org-Craft Spezialisierungen & Rollen</h2>
              </div>
              <span className="text-xs font-mono text-slate-400">
                Basierend auf erlernten Rezepten
              </span>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
              {coverage.specializations.map((spec) => (
                <div
                  key={spec.title}
                  className="p-3.5 rounded-lg bg-slate-900/60 border border-slate-800/90 flex flex-col justify-between space-y-3 relative overflow-hidden"
                >
                  <div>
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <span className="text-xl">{spec.icon}</span>
                        <div>
                          <div className="text-sm font-bold text-slate-200">{spec.title}</div>
                          <div className="text-[10px] font-mono text-slate-400">{spec.role}</div>
                        </div>
                      </div>
                      <span className="text-sm font-mono font-bold text-cyan-400">{spec.percent}%</span>
                    </div>
                    <div className="text-xs text-slate-400 mt-2 leading-relaxed">{spec.focus}</div>
                  </div>

                  <div>
                    <div className="flex items-center justify-between text-[10px] font-mono text-slate-500 mb-1">
                      <span>Erlernt: {spec.learned} / {spec.total}</span>
                      <span>{spec.percent >= 75 ? 'Meister' : spec.percent >= 40 ? 'Geselle' : 'Lehrling'}</span>
                    </div>
                    <div className="w-full bg-slate-800 rounded-full h-1.5 overflow-hidden">
                      <div
                        className="bg-gradient-to-r from-cyan-500 to-emerald-400 h-full rounded-full transition-all duration-500"
                        style={{ width: `${spec.percent}%` }}
                      />
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Categories & Rarities Breakdown */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
            {/* Category Breakdown */}
            <div className="sc-glass rounded-lg p-4 border border-slate-800 space-y-3">
              <div className="flex items-center gap-2">
                <BarChart3 className="w-4 h-4 text-cyan-400" />
                <h3 className="text-sm font-bold text-slate-100">Abdeckung nach Kategorie</h3>
              </div>
              <div className="space-y-2.5">
                {coverage.categories.map((c) => (
                  <div key={c.name} className="p-2.5 rounded bg-slate-900/60 border border-slate-800/80">
                    <div className="flex items-center justify-between mb-1.5 text-xs">
                      <div className="flex items-center gap-2 font-medium text-slate-200">
                        <span>{c.icon}</span>
                        <span>{c.name}</span>
                      </div>
                      <div className="font-mono text-slate-400">
                        <span className="text-emerald-400 font-bold">{c.learned}</span> / {c.total} (
                        <span className="text-cyan-300 font-semibold">{c.percent}%</span>)
                      </div>
                    </div>
                    <div className="w-full bg-slate-800 rounded-full h-2 overflow-hidden">
                      <div
                        className="bg-gradient-to-r from-cyan-500 to-emerald-400 h-full rounded-full transition-all duration-500"
                        style={{ width: `${c.percent}%` }}
                      />
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* Rarities Breakdown */}
            <div className="sc-glass rounded-lg p-4 border border-slate-800 space-y-3">
              <div className="flex items-center gap-2">
                <Sparkles className="w-4 h-4 text-amber-400" />
                <h3 className="text-sm font-bold text-slate-100">Abdeckung nach Seltenheit</h3>
              </div>
              <div className="space-y-2.5">
                {coverage.rarities.map((r) => (
                  <div key={r.rarity} className="p-2.5 rounded bg-slate-900/60 border border-slate-800/80">
                    <div className="flex items-center justify-between mb-1.5 text-xs">
                      <div className="flex items-center gap-2 font-medium" style={{ color: r.color }}>
                        <span className="w-2 h-2 rounded-full" style={{ backgroundColor: r.color }} />
                        <span>{r.rarity}</span>
                      </div>
                      <div className="font-mono text-slate-400">
                        <span className="text-slate-200 font-bold">{r.learned}</span> / {r.total} (
                        <span className="font-semibold" style={{ color: r.color }}>
                          {r.percent}%
                        </span>
                        )
                      </div>
                    </div>
                    <div className="w-full bg-slate-800 rounded-full h-2 overflow-hidden">
                      <div
                        className="h-full rounded-full transition-all duration-500"
                        style={{ width: `${r.percent}%`, backgroundColor: r.color }}
                      />
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Top 12 Priority Gaps (Missing Key Blueprints) */}
          <div className="sc-glass rounded-lg p-4 border border-slate-800 space-y-3">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Target className="w-5 h-5 text-rose-400" />
                <h3 className="text-base font-bold text-slate-100">
                  Prioritäts-Lücken (Top {coverage.topGaps.length} fehlende Schlüssel-Baupläne)
                </h3>
              </div>
              <span className="text-xs font-mono text-slate-400">
                Höchster strategischer Nutzen für die Flotte
              </span>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3">
              {coverage.topGaps.map((gap) => (
                <div
                  key={gap.id}
                  className="p-3 rounded-lg bg-slate-900/80 border border-slate-800 hover:border-slate-700 flex flex-col justify-between space-y-3 relative overflow-hidden"
                >
                  <div>
                    <div className="flex items-center justify-between gap-2 mb-1.5">
                      <span className="text-xs font-semibold text-slate-300 flex items-center gap-1">
                        {getCategoryIcon(gap.category)}
                        {gap.category} {gap.subCategory && `· ${gap.subCategory}`}
                      </span>
                      <span
                        className="px-2 py-0.5 text-[9px] font-mono uppercase tracking-wider rounded border"
                        style={{
                          borderColor: `${gap.rarityColor}66`,
                          color: gap.rarityColor,
                          backgroundColor: `${gap.rarityColor}15`,
                        }}
                      >
                        {gap.rarity}
                      </span>
                    </div>

                    <h4 className="text-sm font-bold text-slate-100">{gap.name}</h4>

                    {gap.unlockInfo && (
                      <div className="mt-2 text-xs text-amber-300/90 bg-amber-500/10 p-2 rounded border border-amber-500/20 leading-relaxed flex items-start gap-1.5">
                        <Sparkles className="w-3.5 h-3.5 text-amber-400 shrink-0 mt-0.5" />
                        <span>{gap.unlockInfo}</span>
                      </div>
                    )}

                    {gap.requiredMaterials && (
                      <div className="mt-2 text-[11px] font-mono text-slate-400 bg-slate-950/60 p-1.5 rounded border border-slate-800/60">
                        {gap.requiredMaterials}
                      </div>
                    )}
                  </div>

                  <div className="pt-2 border-t border-slate-800 flex items-center justify-end">
                    <button
                      onClick={() =>
                        handleToggleLearned({
                          id: gap.id,
                          name: gap.name,
                          category: gap.category,
                          subCategory: gap.subCategory,
                          rarity: gap.rarity,
                          requiredMaterials: gap.requiredMaterials,
                          unlockInfo: gap.unlockInfo,
                          isLearned: false,
                        })
                      }
                      className="flex items-center gap-1.5 px-2.5 py-1 text-xs font-semibold rounded bg-emerald-500/10 hover:bg-emerald-500/20 text-emerald-300 border border-emerald-500/30 cursor-pointer transition"
                    >
                      <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400" />
                      Als erlernt abhaken
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* SCMDB Import Modal */}
      {showImportModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-sm p-4">
          <div className="sc-glass rounded-xl border border-slate-700 w-full max-w-2xl max-h-[90vh] flex flex-col overflow-hidden shadow-2xl animate-in fade-in zoom-in-95 duration-200">
            {/* Modal Header */}
            <div className="p-4 border-b border-slate-800 flex items-center justify-between bg-slate-900/60">
              <div className="flex items-center gap-2">
                <Upload className="w-5 h-5 text-cyan-400" />
                <h3 className="text-base font-bold text-slate-100">SCMDB Export importieren</h3>
              </div>
              <button
                onClick={() => {
                  setShowImportModal(false);
                  setImportPreview(null);
                  setImportError(null);
                }}
                className="p-1 rounded text-slate-400 hover:text-slate-200 hover:bg-slate-800 cursor-pointer transition"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Modal Body */}
            <div className="p-5 overflow-y-auto space-y-4 text-xs">
              <div className="text-slate-300 leading-relaxed">
                Importiere deine freigeschalteten Baupläne von{' '}
                <a
                  href="https://scmdb.net"
                  target="_blank"
                  rel="noreferrer"
                  className="text-cyan-400 underline hover:text-cyan-300 inline-flex items-center gap-1"
                >
                  scmdb.net <ExternalLink className="w-3 h-3 inline" />
                </a>
                . Lade die exportierte JSON-Datei hoch oder füge den Inhalt direkt ein.
              </div>

              {/* File Upload Zone */}
              <div className="border-2 border-dashed border-slate-700 hover:border-cyan-500/60 rounded-lg p-5 text-center transition bg-slate-900/40">
                <input
                  type="file"
                  accept=".json"
                  onChange={handleFileSelect}
                  className="hidden"
                  id="scmdb-file-input"
                />
                <label
                  htmlFor="scmdb-file-input"
                  className="cursor-pointer flex flex-col items-center justify-center space-y-2"
                >
                  <Upload className="w-8 h-8 text-cyan-400" />
                  <span className="text-slate-200 font-semibold text-sm">
                    SCMDB .json Datei auswählen oder hierher ziehen
                  </span>
                  <span className="text-[11px] text-slate-500">Maximal 5 MB</span>
                </label>
              </div>

              {/* Textarea for raw JSON */}
              <div className="space-y-1.5">
                <label className="text-[11px] font-mono text-slate-400 uppercase tracking-wider">
                  Oder JSON-Inhalt einfügen:
                </label>
                <textarea
                  value={importJsonText}
                  onChange={(e) => {
                    setImportJsonText(e.target.value);
                    analyzeImport(e.target.value);
                  }}
                  placeholder='{"version": 3, "blueprints": [{"name": "Yubarev Pistol", "completed": true}, ...]}'
                  className="w-full h-24 bg-slate-950/80 border border-slate-800 rounded-md p-2.5 font-mono text-xs text-slate-200 focus:outline-none focus:border-cyan-500 resize-none"
                />
              </div>

              {/* Error Box */}
              {importError && (
                <div className="p-3 rounded bg-rose-500/10 border border-rose-500/30 text-rose-300 flex items-start gap-2">
                  <AlertCircle className="w-4 h-4 text-rose-400 shrink-0 mt-0.5" />
                  <div>{importError}</div>
                </div>
              )}

              {/* Import Preview Plan */}
              {importPreview && (
                <div className="p-4 rounded-lg bg-slate-900/80 border border-slate-700 space-y-3">
                  <div className="flex items-center justify-between font-mono text-[11px] text-slate-400 pb-2 border-b border-slate-800">
                    <span>SCMDB Version: {importPreview.version}</span>
                    {importPreview.exportedAt && <span>Exportiert: {importPreview.exportedAt}</span>}
                  </div>

                  <div className="grid grid-cols-3 gap-2 text-center">
                    <div className="p-2 rounded bg-emerald-500/10 border border-emerald-500/20">
                      <div className="text-lg font-bold text-emerald-400">{importPreview.toImportCount}</div>
                      <div className="text-[10px] text-emerald-300/80">Neu zu importieren</div>
                    </div>
                    <div className="p-2 rounded bg-slate-800/80 border border-slate-700">
                      <div className="text-lg font-bold text-slate-300">{importPreview.alreadyOwnedCount}</div>
                      <div className="text-[10px] text-slate-400">Bereits erlernt</div>
                    </div>
                    <div className="p-2 rounded bg-amber-500/10 border border-amber-500/20">
                      <div className="text-lg font-bold text-amber-400">{importPreview.unrecognizedCount}</div>
                      <div className="text-[10px] text-amber-300/80">Nicht erkannt</div>
                    </div>
                  </div>

                  {importPreview.toImport.length > 0 && (
                    <div>
                      <div className="text-[11px] font-semibold text-emerald-300 mb-1">
                        Vorschau der neuen Baupläne ({importPreview.toImport.length}):
                      </div>
                      <div className="max-h-24 overflow-y-auto p-2 bg-slate-950/60 rounded border border-slate-800/80 font-mono text-[11px] text-slate-300 space-y-0.5">
                        {importPreview.toImport.map((name) => (
                          <div key={name} className="flex items-center gap-1.5">
                            <CheckCircle2 className="w-3 h-3 text-emerald-400 shrink-0" />
                            <span>{name}</span>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}

                  {importPreview.unrecognized.length > 0 && (
                    <div>
                      <div className="text-[11px] font-semibold text-amber-300 mb-1">
                        Nicht im Katalog gefunden ({importPreview.unrecognized.length}):
                      </div>
                      <div className="max-h-16 overflow-y-auto p-2 bg-slate-950/60 rounded border border-slate-800/80 font-mono text-[11px] text-slate-400">
                        {importPreview.unrecognized.join(', ')}
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>

            {/* Modal Footer */}
            <div className="p-4 border-t border-slate-800 bg-slate-900/60 flex items-center justify-between">
              <button
                onClick={() => {
                  setShowImportModal(false);
                  setImportPreview(null);
                }}
                className="px-4 py-1.5 rounded text-xs font-semibold text-slate-400 hover:text-slate-200 cursor-pointer"
              >
                Abbrechen
              </button>

              <button
                onClick={executeImport}
                disabled={!importPreview || importPreview.toImportCount === 0 || isImporting}
                className={`flex items-center gap-1.5 px-4 py-1.5 rounded text-xs font-semibold cursor-pointer transition ${
                  importPreview && importPreview.toImportCount > 0 && !isImporting
                    ? 'bg-emerald-500 hover:bg-emerald-400 text-slate-950 shadow-[0_0_12px_rgba(16,185,129,0.3)]'
                    : 'bg-slate-800 text-slate-500 cursor-not-allowed'
                }`}
              >
                {isImporting ? (
                  <>
                    <RefreshCw className="w-3.5 h-3.5 animate-spin" />
                    <span>Wird importiert...</span>
                  </>
                ) : (
                  <>
                    <Check className="w-3.5 h-3.5" />
                    <span>
                      {importPreview?.toImportCount
                        ? `${importPreview.toImportCount} Baupläne importieren`
                        : 'Importieren'}
                    </span>
                  </>
                )}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* SCMDB Export Modal */}
      {showExportModal && exportData && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 backdrop-blur-sm p-4">
          <div className="sc-glass rounded-xl border border-slate-700 w-full max-w-2xl max-h-[90vh] flex flex-col overflow-hidden shadow-2xl animate-in fade-in zoom-in-95 duration-200">
            {/* Modal Header */}
            <div className="p-4 border-b border-slate-800 flex items-center justify-between bg-slate-900/60">
              <div className="flex items-center gap-2">
                <Download className="w-5 h-5 text-cyan-400" />
                <h3 className="text-base font-bold text-slate-100">SCMDB v3 Export bereit</h3>
              </div>
              <button
                onClick={() => setShowExportModal(false)}
                className="p-1 rounded text-slate-400 hover:text-slate-200 hover:bg-slate-800 cursor-pointer transition"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Modal Body */}
            <div className="p-5 overflow-y-auto space-y-4 text-xs">
              <div className="text-slate-300 leading-relaxed">
                Der Export enthält alle {exportData.totalCount} Baupläne des Katalogs ({exportData.learnedCount} als erlernt markiert). Dieses Format kann direkt in{' '}
                <span className="text-cyan-400 font-mono">scmdb.net</span> importiert oder mit deiner Organisation geteilt werden.
              </div>

              <div className="relative">
                <textarea
                  readOnly
                  value={exportData.json}
                  className="w-full h-64 bg-slate-950/80 border border-slate-800 rounded-md p-3 font-mono text-[11px] text-slate-300 focus:outline-none resize-none leading-relaxed select-all"
                />
              </div>
            </div>

            {/* Modal Footer */}
            <div className="p-4 border-t border-slate-800 bg-slate-900/60 flex items-center justify-between">
              <button
                onClick={() => setShowExportModal(false)}
                className="px-4 py-1.5 rounded text-xs font-semibold text-slate-400 hover:text-slate-200 cursor-pointer"
              >
                Schließen
              </button>

              <div className="flex items-center gap-2">
                <button
                  onClick={handleCopyExport}
                  className="flex items-center gap-1.5 px-3 py-1.5 rounded bg-slate-800 hover:bg-slate-700 text-xs font-semibold text-slate-200 cursor-pointer transition"
                >
                  {copiedExport ? (
                    <>
                      <Check className="w-3.5 h-3.5 text-emerald-400" />
                      <span className="text-emerald-400">Kopiert!</span>
                    </>
                  ) : (
                    <>
                      <Copy className="w-3.5 h-3.5 text-slate-400" />
                      <span>In Zwischenablage</span>
                    </>
                  )}
                </button>

                <button
                  onClick={handleDownloadExport}
                  className="flex items-center gap-1.5 px-4 py-1.5 rounded bg-cyan-500 hover:bg-cyan-400 text-xs font-semibold text-slate-950 cursor-pointer transition shadow-[0_0_12px_rgba(0,240,255,0.3)]"
                >
                  <Download className="w-3.5 h-3.5" />
                  <span>Als .json herunterladen</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
