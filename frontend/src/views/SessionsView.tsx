import React, { useState, useEffect } from 'react';
import {
  SessionSummary,
  LogStatus,
  ScanProgress,
  DbDiagnostics,
  UnknownEventsData,
  bridge,
} from '../services/photinoBridge';
import {
  Layers,
  Compass,
  RefreshCw,
  Search,
  FileText,
  Trash2,
  CheckCircle2,
  Radio,
  Folder,
  Database,
  ExternalLink,
  HelpCircle,
  Sparkles,
  Clock,
  Download,
} from 'lucide-react';

interface SessionsViewProps {
  sessions: SessionSummary[];
  onSelectSession?: (sessionName: string) => void;
  selectedSession?: string;
  onRefreshData?: () => void;
}

export const SessionsView: React.FC<SessionsViewProps> = ({
  sessions: initialSessions,
  onSelectSession,
  selectedSession = '__live__',
  onRefreshData,
}) => {

  // Local state
  const [sessionsList, setSessionsList] = useState<SessionSummary[]>(initialSessions);
  const [logStatus, setLogStatus] = useState<LogStatus | null>(null);
  const [scanProgress, setScanProgress] = useState<ScanProgress | null>(null);
  const [isScanning, setIsScanning] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [activeFilter, setActiveFilter] = useState<'all' | 'profit' | 'deaths' | 'missions'>('all');
  const [sortOrder, setSortOrder] = useState<'newest' | 'oldest' | 'profit' | 'duration'>('newest');

  // Modals
  const [isMaintenanceOpen, setIsMaintenanceOpen] = useState(false);
  const [dbDiag, setDbDiag] = useState<DbDiagnostics | null>(null);
  const [isCheckingDb, setIsCheckingDb] = useState(false);
  const [diagMessage, setDiagMessage] = useState<string | null>(null);

  const [isUnknownEventsOpen, setIsUnknownEventsOpen] = useState(false);
  const [unknownEvents, setUnknownEvents] = useState<UnknownEventsData | null>(null);

  const [toastMessage, setToastMessage] = useState<string | null>(null);

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3500);
  };

  const formatNumber = (num?: number) => {
    if (num === undefined || num === null) return '0';
    return num.toLocaleString('de-DE');
  };

  const loadLogStatus = async () => {
    try {
      const status = await bridge.sendRequest<LogStatus>('get_log_status');
      if (status) setLogStatus(status);
    } catch (err) {
      console.error('Failed to load log status:', err);
    }
  };

  const loadSessions = async () => {
    try {
      const data = await bridge.sendRequest<SessionSummary[]>('get_sessions');
      if (data) setSessionsList(data);
    } catch (err) {
      console.error('Failed to load sessions:', err);
    }
  };

  useEffect(() => {
    setSessionsList(initialSessions);
  }, [initialSessions]);

  useEffect(() => {
    loadLogStatus();
    loadSessions();

    const unbindScanProgress = bridge.on<ScanProgress>('SCAN_PROGRESS', (progress) => {
      setScanProgress(progress);
      if (progress.isCompleted) {
        setIsScanning(false);
        showToast(
          `Re-Scan abgeschlossen: ${progress.indexedSessions ?? 0} Sessions, ${formatNumber(progress.totalEvents)} Events indexiert!`
        );
        loadSessions();
        loadLogStatus();
        if (onRefreshData) onRefreshData();
      }
    });

    const unbindLogStatus = bridge.on<LogStatus>('log_status_response', (status) => {
      setLogStatus(status);
    });

    const unbindSessions = bridge.on<SessionSummary[]>('sessions_response', (sess) => {
      setSessionsList(sess);
    });

    return () => {
      unbindScanProgress();
      unbindLogStatus();
      unbindSessions();
    };
  }, []);

  // Actions
  const handleAutoDetect = async () => {
    try {
      showToast('Suche nach Star Citizen Installationen...');
      const res = await bridge.sendRequest<LogStatus>('detect_log_path');
      if (res) {
        setLogStatus(res);
        showToast(`Erkannt: ${res.channel} (${res.currentLogPath})`);
        loadSessions();
        if (onRefreshData) onRefreshData();
      }
    } catch (err) {
      console.error('Auto detect failed:', err);
      showToast('Keine Game.log gefunden');
    }
  };

  const handleBrowseFile = async () => {
    try {
      const res = await bridge.sendRequest<LogStatus>('browse_log_file');
      if (res && res.currentLogPath) {
        setLogStatus(res);
        showToast(`Ausgewählt: ${res.currentLogPath}`);
        loadSessions();
        if (onRefreshData) onRefreshData();
      }
    } catch (err) {
      console.error('Browse failed:', err);
    }
  };

  const handleSetPath = async (path: string) => {
    try {
      const res = await bridge.sendRequest<LogStatus>('set_log_path', { path });
      if (res) {
        setLogStatus(res);
        showToast(`Kanal gewechselt zu: ${res.channel}`);
        loadSessions();
        if (onRefreshData) onRefreshData();
      }
    } catch (err) {
      console.error('Set path failed:', err);
    }
  };

  const handleReparseAll = async () => {
    if (isScanning) return;
    try {
      setIsScanning(true);
      setScanProgress({
        current: 0,
        total: 10,
        percent: 0,
        currentFileName: 'Sammle Logs...',
        isCompleted: false,
      });
      await bridge.sendRequest('reparse_all_logs');
    } catch (err) {
      console.error('Reparse all failed:', err);
      setIsScanning(false);
      showToast('Fehler beim Re-Scan');
    }
  };

  const handleReparseSingle = async (sessionName: string) => {
    try {
      showToast(`Lese ${sessionName} neu ein...`);
      await bridge.sendRequest('reparse_session', { session: sessionName });
      loadSessions();
      showToast(`Session ${sessionName} erfolgreich aktualisiert.`);
    } catch (err) {
      console.error('Reparse session failed:', err);
    }
  };

  const handleDeleteSingle = async (sessionName: string) => {
    if (!window.confirm(`Möchtest du die Session "${sessionName}" wirklich aus der Datenbank löschen?`)) {
      return;
    }
    try {
      await bridge.sendRequest('delete_session', { session: sessionName });
      loadSessions();
      showToast(`Session "${sessionName}" gelöscht.`);
    } catch (err) {
      console.error('Delete session failed:', err);
    }
  };

  const handleOpenMaintenance = async () => {
    setIsMaintenanceOpen(true);
    try {
      setIsCheckingDb(true);
      const diag = await bridge.sendRequest<DbDiagnostics>('get_db_diagnostics');
      if (diag) setDbDiag(diag);
    } catch (err) {
      console.error('Get db diagnostics failed:', err);
    } finally {
      setIsCheckingDb(false);
    }
  };

  const handleRepairStructure = async () => {
    try {
      setIsCheckingDb(true);
      const res = await bridge.sendRequest<{ success: boolean; message: string; diagnostics: DbDiagnostics }>(
        'repair_db_structure'
      );
      if (res?.diagnostics) setDbDiag(res.diagnostics);
      setDiagMessage(res?.message || 'Strukturprüfung abgeschlossen');
      showToast('Datenbank-Struktur und Indizes erfolgreich aktualisiert');
    } catch (err) {
      console.error('Repair db structure failed:', err);
    } finally {
      setIsCheckingDb(false);
    }
  };

  const handleCleanupVacuum = async () => {
    try {
      setIsCheckingDb(true);
      const res = await bridge.sendRequest<{
        cleanedEvents: number;
        cleanedSessions: number;
        sizeBefore: string;
        sizeAfter: string;
      }>('cleanup_database');
      showToast(`VACUUM abgeschlossen! Bereinigt: ${res.cleanedEvents} Events. Größe: ${res.sizeBefore} → ${res.sizeAfter}`);
      const diag = await bridge.sendRequest<DbDiagnostics>('get_db_diagnostics');
      if (diag) setDbDiag(diag);
      loadLogStatus();
    } catch (err) {
      console.error('Cleanup failed:', err);
    } finally {
      setIsCheckingDb(false);
    }
  };

  const handleResetDatabase = async () => {
    if (!window.confirm('WARNUNG: Möchtest du wirklich die gesamte SQLite-Datenbank leeren? Alle indexierten Sessions und Transaktionen werden zurückgesetzt.')) {
      return;
    }
    try {
      await bridge.sendRequest('reset_database');
      showToast('Datenbank wurde vollständig geleert.');
      loadSessions();
      loadLogStatus();
      if (onRefreshData) onRefreshData();
      setIsMaintenanceOpen(false);
    } catch (err) {
      console.error('Reset database failed:', err);
    }
  };

  const handleOpenFolder = async (target: 'db' | 'appdata' | 'debug_log' | 'unknown_log' | 'log_folder') => {
    try {
      await bridge.sendRequest('open_folder', { target });
    } catch (err) {
      console.error('Open folder failed:', err);
    }
  };

  const handleExport = async (format: 'csv' | 'json') => {
    try {
      const res = await bridge.sendRequest<{ success: boolean; path?: string }>('export_events', {
        format,
        session: selectedSession,
      });
      if (res?.success && res.path) {
        showToast(`Erfolgreich exportiert: ${res.path}`);
      } else {
        showToast('Export abgeschlossen');
      }
    } catch (err) {
      console.error('Export failed:', err);
      showToast('Fehler beim Export');
    }
  };

  const handleOpenUnknownEvents = async () => {
    try {
      const data = await bridge.sendRequest<UnknownEventsData>('get_unknown_events');
      if (data) setUnknownEvents(data);
      setIsUnknownEventsOpen(true);
    } catch (err) {
      console.error('Get unknown events failed:', err);
    }
  };

  // Filtered & Sorted Sessions
  const filteredSessions = sessionsList
    .filter((s) => {
      if (activeFilter === 'profit') return s.net > 0;
      if (activeFilter === 'deaths') return s.deaths > 0;
      if (activeFilter === 'missions') return s.missions > 0;
      return true;
    })
    .filter((s) => {
      if (!searchQuery.trim()) return true;
      const q = searchQuery.toLowerCase();
      return (
        s.name.toLowerCase().includes(q) ||
        (s.lastLocation && s.lastLocation.toLowerCase().includes(q)) ||
        (s.ships && s.ships.some((sh) => sh.toLowerCase().includes(q)))
      );
    })
    .sort((a, b) => {
      if (sortOrder === 'profit') return b.net - a.net;
      if (sortOrder === 'duration') return b.duration.localeCompare(a.duration);
      if (sortOrder === 'oldest') return a.id - b.id;
      return b.id - a.id;
    });

  return (
    <div className="flex flex-col min-h-full space-y-4 font-sans">
      {/* Toast Notification */}
      {toastMessage && (
        <div className="fixed bottom-6 right-6 z-50 flex items-center space-x-3 px-4 py-3 rounded-lg bg-slate-900 border border-cyan-500/50 shadow-xl shadow-cyan-500/10 text-cyan-200 animate-in fade-in slide-in-from-bottom-2 text-xs">
          <CheckCircle2 className="w-4 h-4 text-cyan-400 shrink-0" />
          <span className="font-medium">{toastMessage}</span>
        </div>
      )}

      {/* ══ HEADER BAR: LOG-QUELLEN & BATCH-STEUERUNG ══ */}
      <div className="sc-glass rounded-xl p-4 border border-slate-800 flex flex-col lg:flex-row lg:items-center justify-between gap-4">
        {/* Linke Seite: Titel & Aktiver Pfad */}
        <div className="flex items-center space-x-4 min-w-0">
          <div className="w-11 h-11 rounded-lg bg-cyan-500/10 border border-cyan-500/30 flex items-center justify-center text-cyan-400 shadow-inner shrink-0">
            <Layers className="w-5 h-5" />
          </div>
          <div className="min-w-0">
            <div className="flex items-center space-x-2.5 flex-wrap">
              <h1 className="text-base font-bold text-white tracking-wide uppercase">
                LOGPARSING &amp; SESSION-VERWALTUNG
              </h1>
              {/* Channel Pill */}
              <span
                className={`px-2 py-0.5 text-[10px] font-mono font-bold rounded border uppercase ${
                  logStatus?.channel === 'LIVE'
                    ? 'bg-emerald-950/70 border-emerald-500/50 text-emerald-300'
                    : logStatus?.channel === 'PTU' || logStatus?.channel === 'EPTU'
                    ? 'bg-amber-950/70 border-amber-500/50 text-amber-300'
                    : 'bg-cyan-950/70 border-cyan-500/50 text-cyan-300'
                }`}
              >
                ● {logStatus?.channel || 'LIVE'}
              </span>
              {/* Live Streaming Badge */}
              <span className="flex items-center gap-1.5 px-2 py-0.5 rounded bg-slate-950 border border-slate-800 text-[10px] font-mono text-slate-300">
                <Radio
                  className={`w-3 h-3 ${
                    logStatus?.isLiveWatching ? 'text-emerald-400 animate-pulse' : 'text-slate-500'
                  }`}
                />
                {logStatus?.isLiveWatching ? 'Live Stream aktiv' : 'Stream angehalten'}
              </span>
            </div>
            <p
              className="text-xs text-slate-400 mt-1 font-mono truncate max-w-xl"
              title={logStatus?.currentLogPath || 'Kein Pfad konfiguriert'}
            >
              Pfad: {logStatus?.currentLogPath || 'Keine Game.log ausgewählt'}
            </p>
          </div>
        </div>

        {/* Rechte Seite: Schnellauswahl-Aktionen */}
        <div className="flex items-center gap-2 flex-wrap">
          {/* Auto-Erkennung */}
          <button
            onClick={handleAutoDetect}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-900/80 hover:bg-cyan-950/40 text-cyan-300 border border-cyan-800/60 hover:border-cyan-500 text-xs font-semibold transition"
            title="Sucht auf allen Laufwerken nach Star Citizen Game.log"
          >
            <Sparkles className="w-3.5 h-3.5 text-cyan-400" />
            <span>⚡ Auto-Erkennung</span>
          </button>

          {/* Datei wählen */}
          <button
            onClick={handleBrowseFile}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-900/80 hover:bg-slate-800 text-slate-200 border border-slate-700 text-xs font-medium transition"
            title="Game.log oder Backup-Datei manuell wählen"
          >
            <Folder className="w-3.5 h-3.5 text-slate-400" />
            <span>📁 Datei wählen</span>
          </button>

          {/* Kompletter Re-Scan */}
          <button
            onClick={handleReparseAll}
            disabled={isScanning}
            className="flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg bg-cyan-600 hover:bg-cyan-500 text-white font-bold text-xs shadow-lg shadow-cyan-600/20 border border-cyan-400 transition disabled:opacity-50 cursor-pointer"
            title="Liest alle Logs frisch mit neuen Parser-Regeln ein (Kompletter Re-Scan)"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isScanning ? 'animate-spin' : ''}`} />
            <span>{isScanning ? 'Lese Logs ein...' : '🔄 Alle Logs neu einlesen'}</span>
          </button>

          {/* DB-Wartung & Diagnose */}
          <button
            onClick={handleOpenMaintenance}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-900/80 hover:bg-slate-800 text-slate-300 border border-slate-700 text-xs font-medium transition"
            title="SQLite Strukturprüfung, VACUUM und Reparaturen"
          >
            <Database className="w-3.5 h-3.5 text-amber-400" />
            <span>🛠️ DB-Wartung</span>
          </button>
        </div>
      </div>

      {/* ══ SCHNELLAUSWAHL-KANÄLE (FALLS MEHRERE INSTALLATIONEN VORHANDEN) ══ */}
      {logStatus?.detectedPaths && logStatus.detectedPaths.length > 1 && (
        <div className="flex items-center gap-2 px-1 text-xs overflow-x-auto pb-1">
          <span className="text-[11px] font-mono text-slate-400 shrink-0">Erkannte Kanäle:</span>
          {logStatus.detectedPaths.map((p, idx) => (
            <button
              key={idx}
              onClick={() => handleSetPath(p.path)}
              className={`flex items-center gap-1.5 px-2.5 py-1 rounded-md text-[11px] font-mono transition border ${
                p.isCurrent
                  ? 'bg-cyan-950/80 border-cyan-500 text-cyan-300 font-bold shadow-sm'
                  : 'bg-slate-900/50 border-slate-800 text-slate-400 hover:text-slate-200 hover:border-slate-700'
              }`}
              title={`${p.path} (${p.lastModified})`}
            >
              <span>● {p.channel}</span>
              <span className="text-[10px] text-slate-500">
                {(p.sizeBytes / (1024 * 1024)).toFixed(1)} MB
              </span>
            </button>
          ))}
        </div>
      )}

      {/* ══ 4 KPI DIAGNOSTIK-KARTEN ══ */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
        {/* KPI 1: Aktiver Kanal & Loggröße */}
        <div className="sc-glass rounded-lg p-3.5 border border-slate-800/80 space-y-1">
          <div className="text-[10px] font-mono uppercase tracking-wider text-slate-400">
            Log-Quelle &amp; Kanal
          </div>
          <div className="flex items-center justify-between">
            <span className="text-sm font-bold text-white font-mono">
              {logStatus?.channel || 'CUSTOM'}
            </span>
            <span className="text-xs font-mono font-semibold text-cyan-300">
              {logStatus?.formattedSize || '0 B'}
            </span>
          </div>
          <div className="text-[10px] text-slate-400 font-mono truncate">
            {logStatus?.lastModified ? `Geändert: ${logStatus.lastModified}` : 'Nicht gefunden'}
          </div>
        </div>

        {/* KPI 2: Parser- & Schema-Version */}
        <div className="sc-glass rounded-lg p-3.5 border border-slate-800/80 space-y-1">
          <div className="text-[10px] font-mono uppercase tracking-wider text-slate-400">
            Parser &amp; Schema Engine
          </div>
          <div className="flex items-center justify-between">
            <span className="text-sm font-bold text-white font-mono">
              v{logStatus?.parserVersion ?? 28} · Schema v{logStatus?.schemaVersion ?? 17}
            </span>
            <span className="px-1.5 py-0.5 rounded text-[9px] font-bold bg-emerald-950 text-emerald-300 border border-emerald-800">
              ✓ Synchron
            </span>
          </div>
          <div className="text-[10px] text-slate-400 font-mono">
            LogParser v{logStatus?.parserVersion ?? 28} aktiv
          </div>
        </div>

        {/* KPI 3: Indexierte Sessions */}
        <div className="sc-glass rounded-lg p-3.5 border border-slate-800/80 space-y-1">
          <div className="text-[10px] font-mono uppercase tracking-wider text-slate-400">
            Indexierte Sessions
          </div>
          <div className="flex items-center justify-between">
            <span className="text-base font-bold text-cyan-300 font-mono">
              {sessionsList.length} Sessions
            </span>
            <span className="text-[11px] font-mono text-slate-400">
              Archiviert
            </span>
          </div>
          <div className="text-[10px] text-slate-400 font-mono">
            {sessionsList.filter((s) => s.net > 0).length} mit Gewinn ·{' '}
            {sessionsList.filter((s) => s.deaths > 0).length} mit Verlusten
          </div>
        </div>

        {/* KPI 4: Log-Backups & Archiv */}
        <div className="sc-glass rounded-lg p-3.5 border border-slate-800/80 space-y-1">
          <div className="text-[10px] font-mono uppercase tracking-wider text-slate-400">
            Log-Backups &amp; Archiv
          </div>
          <div className="flex items-center justify-between">
            <span className="text-sm font-bold text-white font-mono">
              {logStatus?.backupsCount ?? 0} Backups
            </span>
            <span className="text-[11px] font-mono text-amber-300">
              {logStatus?.archiveCount ?? 0} im Safe-Archiv
            </span>
          </div>
          <div className="text-[10px] text-slate-400 font-mono">
            Ordner: logbackups / archive
          </div>
        </div>
      </div>

      {/* ══ RE-SCAN FORTSCHRITTS-BANNER (NUR BEI AKTIVEM SCAN) ══ */}
      {isScanning && scanProgress && (
        <div className="sc-glass rounded-lg p-4 border border-cyan-500/50 bg-cyan-950/20 space-y-2 animate-in fade-in">
          <div className="flex items-center justify-between text-xs font-mono">
            <div className="flex items-center space-x-2 text-cyan-300">
              <RefreshCw className="w-4 h-4 animate-spin text-cyan-400" />
              <span className="font-bold">
                Indexiere Log-Dateien ({scanProgress.current}/{scanProgress.total}):
              </span>
              <span className="text-slate-300 truncate max-w-md">{scanProgress.currentFileName}</span>
            </div>
            <span className="font-bold text-cyan-400">{scanProgress.percent}%</span>
          </div>
          <div className="w-full bg-slate-900 rounded-full h-2 overflow-hidden border border-cyan-900/60">
            <div
              className="bg-gradient-to-r from-cyan-500 to-sky-400 h-full transition-all duration-300 shadow-[0_0_8px_rgba(6,182,212,0.6)]"
              style={{ width: `${Math.max(5, scanProgress.percent)}%` }}
            />
          </div>
        </div>
      )}

      {/* ══ FILTER-, SUCHE- & EXPORT-LEISTE ══ */}
      <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 text-xs">
        {/* Suche & Filter-Pills */}
        <div className="flex items-center gap-2 flex-wrap flex-1">
          {/* Suche */}
          <div className="relative min-w-[200px] flex-1 sm:max-w-xs">
            <Search className="w-3.5 h-3.5 text-slate-400 absolute left-2.5 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Session, Schiff oder Ort suchen..."
              className="w-full bg-[#071322] border border-slate-700 focus:border-cyan-500 rounded-lg pl-8 pr-3 py-1.5 text-xs text-slate-200 placeholder-slate-500 font-mono focus:outline-none transition"
            />
          </div>

          {/* Filter Chips */}
          <div className="flex items-center gap-1">
            <button
              onClick={() => setActiveFilter('all')}
              className={`px-2.5 py-1 rounded text-xs font-mono font-medium transition ${
                activeFilter === 'all'
                  ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40 font-bold'
                  : 'bg-slate-900/40 text-slate-400 hover:text-slate-200'
              }`}
            >
              Alle ({sessionsList.length})
            </button>
            <button
              onClick={() => setActiveFilter('profit')}
              className={`px-2.5 py-1 rounded text-xs font-mono font-medium transition ${
                activeFilter === 'profit'
                  ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/40 font-bold'
                  : 'bg-slate-900/40 text-slate-400 hover:text-slate-200'
              }`}
            >
              💰 Mit Gewinn
            </button>
            <button
              onClick={() => setActiveFilter('deaths')}
              className={`px-2.5 py-1 rounded text-xs font-mono font-medium transition ${
                activeFilter === 'deaths'
                  ? 'bg-rose-500/20 text-rose-300 border border-rose-500/40 font-bold'
                  : 'bg-slate-900/40 text-slate-400 hover:text-slate-200'
              }`}
            >
              💀 Verluste
            </button>
            <button
              onClick={() => setActiveFilter('missions')}
              className={`px-2.5 py-1 rounded text-xs font-mono font-medium transition ${
                activeFilter === 'missions'
                  ? 'bg-amber-500/20 text-amber-300 border border-amber-500/40 font-bold'
                  : 'bg-slate-900/40 text-slate-400 hover:text-slate-200'
              }`}
            >
              ★ Missionen
            </button>
          </div>
        </div>

        {/* Sortierung & Exporte */}
        <div className="flex items-center gap-2 shrink-0">
          <select
            value={sortOrder}
            onChange={(e) => setSortOrder(e.target.value as any)}
            className="bg-[#071322] border border-slate-700 rounded-lg px-2.5 py-1.5 text-xs text-slate-300 font-mono focus:outline-none focus:border-cyan-500 cursor-pointer"
          >
            <option value="newest">Neueste zuerst</option>
            <option value="oldest">Älteste zuerst</option>
            <option value="profit">Höchster Gewinn</option>
            <option value="duration">Längste Dauer</option>
          </select>

          <button
            onClick={() => handleExport('csv')}
            className="flex items-center gap-1 px-2.5 py-1.5 rounded-lg bg-slate-900/70 hover:bg-slate-800 text-slate-300 border border-slate-700 text-xs transition"
            title="Transaktions-Chronik als CSV exportieren"
          >
            <Download className="w-3 h-3 text-cyan-400" />
            <span>CSV</span>
          </button>

          <button
            onClick={() => handleExport('json')}
            className="flex items-center gap-1 px-2.5 py-1.5 rounded-lg bg-slate-900/70 hover:bg-slate-800 text-slate-300 border border-slate-700 text-xs transition"
            title="Vollständigen Datenbericht als JSON exportieren"
          >
            <Download className="w-3 h-3 text-amber-400" />
            <span>JSON</span>
          </button>

          <button
            onClick={handleOpenUnknownEvents}
            className="flex items-center gap-1 px-2.5 py-1.5 rounded-lg bg-slate-900/70 hover:bg-amber-950/40 text-amber-300 border border-amber-800/60 text-xs transition"
            title="Protokoll unbekannter Events öffnen"
          >
            <HelpCircle className="w-3 h-3 text-amber-400" />
            <span>❓ Unbekannte</span>
          </button>
        </div>
      </div>

      {/* ══ SESSION TABELLE ══ */}
      <div className="sc-glass rounded-xl overflow-hidden border border-slate-800 flex-1 flex flex-col min-h-[360px]">
        <div className="flex-1 overflow-auto">
          <table className="w-full min-w-[920px] text-left text-xs border-collapse">
            <thead>
              <tr className="border-b border-slate-800 bg-slate-900/80 text-slate-400 font-mono uppercase tracking-wider sticky top-0 backdrop-blur-md z-10">
                <th className="py-3 px-4">Session / Zeitraum</th>
                <th className="py-3 px-4">Dauer</th>
                <th className="py-3 px-4 text-right">Einnahmen</th>
                <th className="py-3 px-4 text-right">Ausgaben</th>
                <th className="py-3 px-4 text-right">Netto-Saldo</th>
                <th className="py-3 px-4">Schiffe</th>
                <th className="py-3 px-4">Letzter Standort</th>
                <th className="py-3 px-4 text-center">Status</th>
                <th className="py-3 px-4 text-right">Aktionen</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/40 font-mono">
              {filteredSessions.length === 0 ? (
                <tr>
                  <td colSpan={9} className="py-16 text-center text-slate-500 font-sans">
                    Keine Sitzungen gefunden. Klicke oben auf »🔄 Kompletter Re-Scan« oder »⚡ Auto-Erkennung«.
                  </td>
                </tr>
              ) : (
                filteredSessions.map((sess) => {
                  const isActive = selectedSession === sess.name;
                  return (
                    <tr
                      key={sess.id || sess.name}
                      className={`transition-colors group ${
                        isActive
                          ? 'bg-cyan-950/30 border-l-2 border-l-cyan-400'
                          : 'hover:bg-slate-900/50'
                      }`}
                    >
                      {/* Name & Zeitfenster */}
                      <td className="py-3 px-4">
                        <div className="flex items-center gap-2">
                          <span className="font-semibold text-slate-200 group-hover:text-cyan-300 transition font-sans">
                            {sess.name}
                          </span>
                          {isActive && (
                            <span className="px-1.5 py-0.5 rounded text-[9px] font-bold bg-cyan-950 text-cyan-300 border border-cyan-800">
                              AKTIV
                            </span>
                          )}
                        </div>
                        <div className="text-[10px] text-slate-400 mt-0.5">
                          {sess.startTime} → {sess.endTime}
                        </div>
                      </td>

                      {/* Dauer */}
                      <td className="py-3 px-4 text-slate-300 font-mono flex items-center gap-1 pt-4">
                        <Clock className="w-3 h-3 text-slate-500 shrink-0" />
                        <span>{sess.duration || '—'}</span>
                      </td>

                      {/* Einnahmen */}
                      <td className="py-3 px-4 text-right text-emerald-400 font-bold">
                        +{formatNumber(sess.income)}
                      </td>

                      {/* Ausgaben */}
                      <td className="py-3 px-4 text-right text-rose-400 font-bold">
                        -{formatNumber(sess.spend)}
                      </td>

                      {/* Netto Saldo */}
                      <td
                        className={`py-3 px-4 text-right font-bold ${
                          sess.net >= 0 ? 'text-cyan-300' : 'text-rose-400'
                        }`}
                      >
                        {formatNumber(sess.net)} aUEC
                      </td>

                      {/* Schiffe */}
                      <td className="py-3 px-4 font-sans">
                        {sess.ships && sess.ships.length > 0 ? (
                          <div className="flex flex-wrap gap-1">
                            {sess.ships.map((sh, i) => (
                              <span
                                key={i}
                                className="px-1.5 py-0.5 rounded text-[10px] bg-slate-900 border border-cyan-900/40 text-cyan-300 font-mono"
                              >
                                {sh}
                              </span>
                            ))}
                          </div>
                        ) : (
                          <span className="text-slate-600">—</span>
                        )}
                      </td>

                      {/* Letzter Standort */}
                      <td className="py-3 px-4 text-slate-300 font-sans">
                        <div className="flex items-center gap-1.5">
                          <Compass className="w-3.5 h-3.5 text-cyan-400 shrink-0" />
                          <span className="truncate max-w-[160px]">
                            {sess.lastLocation || '—'}
                          </span>
                        </div>
                      </td>

                      {/* Status / Tode */}
                      <td className="py-3 px-4 text-center font-sans">
                        {sess.deaths > 0 ? (
                          <span className="sc-badge-red" title={`${sess.deaths}x Med-Bett Verlust`}>
                            {sess.deaths}x Verlust
                          </span>
                        ) : (
                          <span className="sc-badge-green">Erfolgreich</span>
                        )}
                      </td>

                      {/* Aktionen */}
                      <td className="py-3 px-4 text-right font-sans">
                        <div className="flex items-center justify-end gap-1">
                          {/* Als aktiv wählen */}
                          <button
                            onClick={() => onSelectSession && onSelectSession(sess.name)}
                            className={`px-2 py-1 rounded text-[11px] font-mono font-semibold transition ${
                              isActive
                                ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40'
                                : 'bg-slate-900 text-slate-300 hover:bg-cyan-950/60 hover:text-cyan-300 border border-slate-800'
                            }`}
                            title="Diese Session als aktuellen Fokus auswählen"
                          >
                            {isActive ? '✓ Aktiv' : 'Aktivieren'}
                          </button>

                          {/* Neu parsen */}
                          <button
                            onClick={() => handleReparseSingle(sess.name)}
                            className="p-1.5 rounded hover:bg-slate-800 text-slate-400 hover:text-cyan-300 transition"
                            title="Nur diese Session neu einlesen"
                          >
                            <RefreshCw className="w-3.5 h-3.5" />
                          </button>

                          {/* Löschen */}
                          <button
                            onClick={() => handleDeleteSingle(sess.name)}
                            className="p-1.5 rounded hover:bg-rose-950/40 text-slate-500 hover:text-rose-400 transition"
                            title="Session aus Datenbank entfernen"
                          >
                            <Trash2 className="w-3.5 h-3.5" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* ══ MODAL: DATENBANK-WARTUNG & DIAGNOSE ══ */}
      {isMaintenanceOpen && (
        <div className="fixed inset-0 z-50 bg-black/80 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="sc-glass rounded-xl border border-slate-700 bg-slate-950/95 max-w-2xl w-full p-6 space-y-5 shadow-2xl animate-in zoom-in-95 font-sans">
            <div className="flex items-center justify-between border-b border-slate-800 pb-3">
              <div className="flex items-center space-x-2.5">
                <Database className="w-5 h-5 text-amber-400" />
                <h2 className="text-base font-bold text-white tracking-wide uppercase">
                  DATENBANK-DIAGNOSE &amp; WARTUNG
                </h2>
              </div>
              <button
                onClick={() => setIsMaintenanceOpen(false)}
                className="text-slate-400 hover:text-white text-sm"
              >
                ✕
              </button>
            </div>

            {/* KPI Diagnostik Grid */}
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 font-mono text-xs">
              <div className="p-3 rounded-lg bg-slate-900 border border-slate-800">
                <div className="text-[10px] text-slate-400 uppercase">Schema</div>
                <div className="text-sm font-bold text-cyan-400 mt-0.5">
                  v{dbDiag?.installedSchemaVersion ?? 17}
                </div>
                <div className="text-[10px] text-slate-500">App: v17</div>
              </div>
              <div className="p-3 rounded-lg bg-slate-900 border border-slate-800">
                <div className="text-[10px] text-slate-400 uppercase">Parser</div>
                <div className="text-sm font-bold text-emerald-400 mt-0.5">
                  v{dbDiag?.installedParserVersion ?? 28}
                </div>
                <div className="text-[10px] text-slate-500">Engine: v28</div>
              </div>
              <div className="p-3 rounded-lg bg-slate-900 border border-slate-800">
                <div className="text-[10px] text-slate-400 uppercase">Integrität</div>
                <div className="text-sm font-bold text-emerald-400 mt-0.5">
                  {dbDiag?.integrityCheckOk ? 'OK' : 'Fehler'}
                </div>
                <div className="text-[10px] text-slate-500 truncate" title={dbDiag?.integrityMessage}>
                  {dbDiag?.integrityMessage || 'quick_check ok'}
                </div>
              </div>
              <div className="p-3 rounded-lg bg-slate-900 border border-slate-800">
                <div className="text-[10px] text-slate-400 uppercase">Größe</div>
                <div className="text-sm font-bold text-cyan-400 mt-0.5">
                  {dbDiag?.formattedSize || '0 B'}
                </div>
                <div className="text-[10px] text-slate-500">Modus: WAL</div>
              </div>
            </div>

            {/* Datensätze Zähler */}
            <div className="p-3 rounded-lg bg-slate-900/60 border border-slate-800 grid grid-cols-3 sm:grid-cols-6 gap-2 text-center font-mono text-xs">
              <div>
                <div className="text-[10px] text-slate-400">Sessions</div>
                <div className="text-sm font-bold text-white">{dbDiag?.sessionCount ?? 0}</div>
              </div>
              <div>
                <div className="text-[10px] text-slate-400">Events</div>
                <div className="text-sm font-bold text-white">{formatNumber(dbDiag?.eventCount)}</div>
              </div>
              <div>
                <div className="text-[10px] text-slate-400">Aufträge</div>
                <div className="text-sm font-bold text-white">{dbDiag?.contractCount ?? 0}</div>
              </div>
              <div>
                <div className="text-[10px] text-slate-400">Flottenschiffe</div>
                <div className="text-sm font-bold text-white">{dbDiag?.fleetShipCount ?? 0}</div>
              </div>
              <div>
                <div className="text-[10px] text-slate-400">Lagerartikel</div>
                <div className="text-sm font-bold text-white">{dbDiag?.warehouseItemCount ?? 0}</div>
              </div>
              <div>
                <div className="text-[10px] text-slate-400">Wegpunkte</div>
                <div className="text-sm font-bold text-white">{dbDiag?.poiCount ?? 0}</div>
              </div>
            </div>

            {diagMessage && (
              <div className="p-3 rounded-lg bg-cyan-950/40 border border-cyan-500/40 text-cyan-300 text-xs">
                {diagMessage}
              </div>
            )}

            {/* Aktionsbuttons */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5 pt-2">
              <button
                onClick={() => {
                  setIsMaintenanceOpen(false);
                  handleReparseAll();
                }}
                disabled={isScanning || isCheckingDb}
                className="col-span-1 sm:col-span-2 flex items-center justify-center space-x-2 px-4 py-2.5 rounded-lg bg-cyan-600 hover:bg-cyan-500 text-white border border-cyan-400 text-xs font-bold shadow-lg shadow-cyan-600/20 transition cursor-pointer disabled:opacity-50"
              >
                <RefreshCw className={`w-4 h-4 ${isScanning ? 'animate-spin' : ''}`} />
                <span>🔄 Alle Logs neu einlesen (Kompletter Re-Scan)</span>
              </button>

              <button
                onClick={handleRepairStructure}
                disabled={isCheckingDb}
                className="flex items-center justify-center space-x-2 px-4 py-2.5 rounded-lg bg-cyan-950/60 hover:bg-cyan-900/80 text-cyan-300 border border-cyan-700/60 text-xs font-semibold transition cursor-pointer"
              >
                <RefreshCw className={`w-4 h-4 ${isCheckingDb ? 'animate-spin' : ''}`} />
                <span>⚡ Struktur &amp; Indizes reparieren</span>
              </button>

              <button
                onClick={handleCleanupVacuum}
                disabled={isCheckingDb}
                className="flex items-center justify-center space-x-2 px-4 py-2.5 rounded-lg bg-slate-900 hover:bg-slate-800 text-slate-200 border border-slate-700 text-xs font-semibold transition"
              >
                <span>🧹 DB bereinigen &amp; VACUUM</span>
              </button>

              <button
                onClick={() => handleOpenFolder('db')}
                className="flex items-center justify-center space-x-2 px-4 py-2.5 rounded-lg bg-slate-900 hover:bg-slate-800 text-slate-200 border border-slate-700 text-xs font-semibold transition"
              >
                <Folder className="w-4 h-4 text-amber-400" />
                <span>📂 sessions.db im Explorer</span>
              </button>

              <button
                onClick={() => handleOpenFolder('debug_log')}
                className="flex items-center justify-center space-x-2 px-4 py-2.5 rounded-lg bg-slate-900 hover:bg-slate-800 text-slate-200 border border-slate-700 text-xs font-semibold transition"
              >
                <FileText className="w-4 h-4 text-slate-400" />
                <span>📄 Debug-Log öffnen</span>
              </button>
            </div>

            {/* Gefahrenzone */}
            <div className="pt-3 border-t border-slate-800 flex items-center justify-between">
              <div className="text-[11px] text-slate-500 font-mono">
                Datenbankdatei: {dbDiag?.databasePath || 'sessions.db'}
              </div>
              <button
                onClick={handleResetDatabase}
                className="px-3 py-1.5 rounded-lg bg-rose-950/40 hover:bg-rose-900/60 text-rose-300 border border-rose-800 text-xs font-bold transition"
              >
                ✕ Datenbank leeren
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ══ MODAL: UNBEKANNTE EVENTS (UNKNOWN LOG) ══ */}
      {isUnknownEventsOpen && unknownEvents && (
        <div className="fixed inset-0 z-50 bg-black/80 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="sc-glass rounded-xl border border-slate-700 bg-slate-950/95 max-w-3xl w-full p-6 space-y-4 shadow-2xl animate-in zoom-in-95 font-sans">
            <div className="flex items-center justify-between border-b border-slate-800 pb-3">
              <div className="flex items-center space-x-2.5">
                <HelpCircle className="w-5 h-5 text-amber-400" />
                <div>
                  <h2 className="text-base font-bold text-white tracking-wide uppercase">
                    PROTOKOLL UNBEKANNTER EVENTS &amp; SCHIFFE
                  </h2>
                  <p className="text-[11px] text-slate-400 font-mono">
                    Datei: {unknownEvents.path}
                  </p>
                </div>
              </div>
              <button
                onClick={() => setIsUnknownEventsOpen(false)}
                className="text-slate-400 hover:text-white text-sm"
              >
                ✕
              </button>
            </div>

            <div className="bg-slate-950 rounded-lg p-3.5 border border-slate-800 h-80 overflow-y-auto font-mono text-[11px] text-slate-300 space-y-1">
              {unknownEvents.lines.length === 0 ? (
                <div className="text-slate-500 py-12 text-center">
                  Keine unbekannten Events protokolliert. Der Parser erkennt alle aktuellen Ereignisse fehlerfrei!
                </div>
              ) : (
                unknownEvents.lines.map((line, idx) => (
                  <div key={idx} className="hover:bg-slate-900/60 py-0.5 px-1 rounded truncate">
                    {line}
                  </div>
                ))
              )}
            </div>

            <div className="flex items-center justify-between pt-2">
              <button
                onClick={() => handleOpenFolder('unknown_log')}
                className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-900 hover:bg-slate-800 text-slate-300 border border-slate-700 text-xs"
              >
                <ExternalLink className="w-3.5 h-3.5" />
                <span>Im Texteditor öffnen</span>
              </button>

              <button
                onClick={() => setIsUnknownEventsOpen(false)}
                className="px-4 py-1.5 rounded-lg bg-cyan-600 hover:bg-cyan-500 text-white font-bold text-xs"
              >
                Schließen
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
