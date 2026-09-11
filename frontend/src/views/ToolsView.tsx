import React, { useState, useEffect } from 'react';
import {
  Trash2,
  RefreshCw,
  Save,
  HardDrive,
  Cpu,
  ShieldCheck,
  Terminal,
  Zap,
  CheckCircle2,
  AlertCircle,
  FileText,
  Key,
  Copy,
  Check,
  FolderOpen,
  Sparkles,
  Wrench,
  Cloud,
  RotateCcw,
  Archive,
  Download,
  ExternalLink,
} from 'lucide-react';
import { bridge, ToolsStatusDto } from '../services/photinoBridge';

export const ToolsView: React.FC = () => {
  const [activeSubTab, setActiveSubTab] = useState<'maintenance' | 'backups'>('maintenance');
  const [status, setStatus] = useState<ToolsStatusDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [cfgContent, setCfgContent] = useState('');
  const [copied, setCopied] = useState(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  // Backups & Tresor state
  const [selectedKeybindIndex, setSelectedKeybindIndex] = useState<number>(0);
  const [selectedConfigIndex, setSelectedConfigIndex] = useState<number>(0);
  const [cloudPath, setCloudPath] = useState('');
  const [keybindNote, setKeybindNote] = useState('');
  const [configNote, setConfigNote] = useState('');

  // Presets & Tuning controls
  const [vsyncOff, setVsyncOff] = useState(true);
  const [motionBlurOff, setMotionBlurOff] = useState(true);
  const [consoleUnlocked, setConsoleUnlocked] = useState(true);
  const [displayInfo, setDisplayInfo] = useState<number>(1);
  const [maxFps, setMaxFps] = useState<number>(120);
  const [streamPool, setStreamPool] = useState<number>(6144);
  const [langEnglish, setLangEnglish] = useState(true);

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3500);
  };

  const loadStatus = async () => {
    try {
      setLoading(true);
      const data = await bridge.send<ToolsStatusDto>('get_tools_status');
      setStatus(data);
      setCfgContent(data.userCfgContent || '');
      if (data.cloudStoragePath !== undefined) {
        setCloudPath(data.cloudStoragePath || '');
      }
      parseCfgContent(data.userCfgContent || '');
    } catch (err) {
      console.error('Failed to load tools status:', err);
    } finally {
      setLoading(false);
    }
  };

  const parseCfgContent = (content: string) => {
    const lines = content.split('\n');
    lines.forEach((line) => {
      const trimmed = line.trim();
      if (trimmed.startsWith('r_VSync')) {
        const val = trimmed.split('=')[1]?.trim();
        setVsyncOff(val === '0');
      } else if (trimmed.startsWith('r_MotionBlur')) {
        const val = trimmed.split('=')[1]?.trim();
        setMotionBlurOff(val === '0');
      } else if (trimmed.startsWith('Con_Restricted')) {
        const val = trimmed.split('=')[1]?.trim();
        setConsoleUnlocked(val === '0');
      } else if (trimmed.startsWith('r_DisplayInfo')) {
        const val = parseInt(trimmed.split('=')[1]?.trim() || '0', 10);
        if (!isNaN(val)) setDisplayInfo(val);
      } else if (trimmed.startsWith('sys_maxfps')) {
        const val = parseInt(trimmed.split('=')[1]?.trim() || '0', 10);
        if (!isNaN(val)) setMaxFps(val);
      } else if (trimmed.startsWith('r_TexturesStreamPoolSize')) {
        const val = parseInt(trimmed.split('=')[1]?.trim() || '0', 10);
        if (!isNaN(val)) setStreamPool(val);
      } else if (trimmed.startsWith('g_language')) {
        setLangEnglish(trimmed.includes('english'));
      }
    });
  };

  const generateCfgFromControls = (
    vsync: boolean,
    mb: boolean,
    consoleUnl: boolean,
    disp: number,
    fps: number,
    pool: number,
    en: boolean
  ) => {
    const lines = [
      `; --- Star Citizen Tuning (SCLogMate generated) ---`,
      `Con_Restricted = ${consoleUnl ? 0 : 1}`,
      `r_VSync = ${vsync ? 0 : 1}`,
      `r_MotionBlur = ${mb ? 0 : 1}`,
      `sys_maxfps = ${fps}`,
      `r_TexturesStreamPoolSize = ${pool}`,
      `r_DisplayInfo = ${disp}`,
      `g_language = ${en ? 'english' : 'german'}`,
      `r_Optane = 1`,
      `e_PrecacheResources = 1`,
    ];
    return lines.join('\n');
  };

  const applyPreset = (type: 'esport' | 'quality' | 'minimal') => {
    if (type === 'esport') {
      setVsyncOff(true);
      setMotionBlurOff(true);
      setConsoleUnlocked(true);
      setDisplayInfo(1);
      setMaxFps(165);
      setStreamPool(8192);
      setLangEnglish(true);
      const text = generateCfgFromControls(true, true, true, 1, 165, 8192, true);
      setCfgContent(text);
      showToast('Profil "High FPS / E-Sport" geladen');
    } else if (type === 'quality') {
      setVsyncOff(false);
      setMotionBlurOff(false);
      setConsoleUnlocked(true);
      setDisplayInfo(0);
      setMaxFps(0);
      setStreamPool(12288);
      setLangEnglish(true);
      const text = generateCfgFromControls(false, false, true, 0, 0, 12288, true);
      setCfgContent(text);
      showToast('Profil "Grafik & Immersion" geladen');
    } else {
      setVsyncOff(true);
      setMotionBlurOff(true);
      setConsoleUnlocked(true);
      setDisplayInfo(1);
      setMaxFps(60);
      setStreamPool(4096);
      setLangEnglish(true);
      const text = generateCfgFromControls(true, true, true, 1, 60, 4096, true);
      setCfgContent(text);
      showToast('Profil "Minimal / Einsteiger-PC" geladen');
    }
  };

  const handleClearShaderCache = async () => {
    setActionLoading('shaders');
    try {
      const res = await bridge.send<ToolsStatusDto>('clear_shader_cache');
      setStatus(res);
      showToast('Shader-Cache erfolgreich geleert!');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Bereinigen der Shader');
    } finally {
      setActionLoading(null);
    }
  };

  const handleClearCrashDumps = async () => {
    setActionLoading('dumps');
    try {
      const res = await bridge.send<ToolsStatusDto>('clear_crash_dumps');
      setStatus(res);
      showToast('Crash-Dumps erfolgreich gelöscht!');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Löschen der Crash-Dumps');
    } finally {
      setActionLoading(null);
    }
  };

  const handleSaveUserCfg = async () => {
    setActionLoading('saveCfg');
    try {
      const res = await bridge.send<ToolsStatusDto>('save_user_cfg', { cfgContent });
      setStatus(res);
      showToast('user.cfg erfolgreich gespeichert & angewendet!');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Speichern der user.cfg');
    } finally {
      setActionLoading(null);
    }
  };

  const handleCreateKeybindBackup = async () => {
    setActionLoading('backupKeybind');
    try {
      const res = await bridge.send<ToolsStatusDto>('backup_keybinds', { note: keybindNote });
      setStatus(res);
      setKeybindNote('');
      showToast('Keybind-Backup erfolgreich angelegt!');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Erstellen des Keybind-Backups');
    } finally {
      setActionLoading(null);
    }
  };

  const handleRestoreKeybind = async () => {
    const keybinds = status?.keybindItems || [];
    const item = keybinds[selectedKeybindIndex];
    if (!item) {
      showToast('Bitte wähle zuerst ein Keybind-Backup aus.');
      return;
    }

    setActionLoading('restoreKeybind');
    try {
      const res = await bridge.send<{ success: boolean; message: string; tools?: ToolsStatusDto }>('restore_keybinds', {
        name: item.name,
        folderPath: item.folderPath,
      });
      if (res.tools) setStatus(res.tools);
      showToast(res.message || 'Keybinds erfolgreich wiederhergestellt!');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Wiederherstellen der Keybinds');
    } finally {
      setActionLoading(null);
    }
  };

  const handleBackupUserCfgSnapshot = async () => {
    setActionLoading('backupCfg');
    try {
      const res = await bridge.send<{ success: boolean; message: string; tools?: ToolsStatusDto }>('backup_user_cfg', {
        note: configNote || 'Manuell',
      });
      if (res.tools) setStatus(res.tools);
      setConfigNote('');
      showToast(res.message || 'user.cfg Snapshot archiviert!');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Archivieren des user.cfg Snapshots');
    } finally {
      setActionLoading(null);
    }
  };

  const handleRestoreConfigSnapshot = async () => {
    const configs = status?.configBackups || [];
    const item = configs[selectedConfigIndex];
    if (!item) {
      showToast('Bitte wähle zuerst eine Version aus.');
      return;
    }

    setActionLoading('restoreCfg');
    try {
      const res = await bridge.send<{ success: boolean; message: string; tools?: ToolsStatusDto }>('restore_user_cfg', {
        name: item.name,
        filePath: item.filePath,
      });
      if (res.tools) {
        setStatus(res.tools);
        setCfgContent(res.tools.userCfgContent || '');
      }
      showToast(res.message || 'user.cfg Stand wiederhergestellt!');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Wiederherstellen der Version');
    } finally {
      setActionLoading(null);
    }
  };

  const handleSaveCloudPath = async () => {
    setActionLoading('saveCloud');
    try {
      const res = await bridge.send<{ success: boolean; tools?: ToolsStatusDto }>('save_cloud_storage_path', {
        path: cloudPath,
      });
      if (res.tools) setStatus(res.tools);
      showToast('Cloud-Speicherpfad gespeichert!');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Speichern des Cloud-Pfads');
    } finally {
      setActionLoading(null);
    }
  };

  const handleExportLogsZip = async () => {
    setActionLoading('exportZip');
    try {
      const res = await bridge.send<{ success: boolean; message: string; zipPath?: string }>('export_logs_zip');
      showToast(res.message || 'Logs erfolgreich als ZIP exportiert!');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Exportieren der Logs');
    } finally {
      setActionLoading(null);
    }
  };

  const handleSyncLogsCloud = async () => {
    setActionLoading('syncCloud');
    try {
      const res = await bridge.send<{ success: boolean; message: string }>('sync_logs_cloud');
      showToast(res.message || 'Logs erfolgreich in Cloud gesichert!');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Synchronisieren in die Cloud');
    } finally {
      setActionLoading(null);
    }
  };

  const handleOpenFolder = async (folderType: 'keybinds' | 'config' | 'cloud' | 'logbackups') => {
    try {
      const res = await bridge.send<{ success: boolean; error?: string }>('open_folder', { folderType });
      if (!res.success && res.error) {
        showToast(res.error);
      }
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Öffnen des Ordners');
    }
  };

  const copyToClipboard = () => {
    navigator.clipboard.writeText(cfgContent);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  useEffect(() => {
    loadStatus();
  }, []);

  if (loading && !status) {
    return (
      <div className="flex flex-col items-center justify-center h-96 space-y-4">
        <RefreshCw className="w-8 h-8 text-sky-400 animate-spin" />
        <p className="text-sm text-slate-400">Lese Star Citizen Konfiguration & Caches aus...</p>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6 max-w-7xl mx-auto">
      {/* Toast Alert */}
      {toastMessage && (
        <div className="fixed bottom-6 right-6 z-50 flex items-center space-x-3 px-4 py-3 rounded-lg bg-slate-900 border border-sky-500/50 shadow-xl shadow-sky-500/10 text-sky-200 animate-in fade-in slide-in-from-bottom-2">
          <CheckCircle2 className="w-5 h-5 text-sky-400 shrink-0" />
          <span className="text-sm font-medium">{toastMessage}</span>
        </div>
      )}

      {/* Header Banner */}
      <div className="p-5 rounded-xl bg-gradient-to-r from-slate-900/90 via-slate-900/60 to-slate-950/80 border border-slate-800 backdrop-blur shadow-lg flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div className="flex items-center space-x-4">
          <div className="w-12 h-12 rounded-lg bg-sky-500/10 border border-sky-500/30 flex items-center justify-center text-sky-400 shadow-inner">
            <Zap className="w-6 h-6" />
          </div>
          <div>
            <div className="flex items-center space-x-2">
              <h1 className="text-xl font-bold text-white tracking-wide">TOOLS & WARTUNGS-STUDIO</h1>
              <span className="px-2 py-0.5 text-xs font-semibold rounded bg-sky-950 text-sky-400 border border-sky-800">
                SC LIVE
              </span>
            </div>
            <p className="text-xs text-slate-400 mt-1 font-mono">
              Aktive user.cfg: {status?.userCfgPath || 'J:\\StarCitizen\\LIVE\\user.cfg'}
            </p>
          </div>
        </div>

        <div className="flex items-center space-x-3">
          <button
            onClick={loadStatus}
            disabled={actionLoading !== null}
            className="flex items-center space-x-2 px-3 py-2 rounded-lg bg-slate-800/80 hover:bg-slate-700 text-slate-200 text-xs font-medium border border-slate-700 transition cursor-pointer"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${actionLoading ? 'animate-spin' : ''}`} />
            <span>Neu laden</span>
          </button>
          {activeSubTab === 'maintenance' ? (
            <button
              onClick={handleSaveUserCfg}
              disabled={actionLoading !== null}
              className="flex items-center space-x-2 px-4 py-2 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold shadow-lg shadow-sky-600/25 border border-sky-400 transition cursor-pointer"
            >
              <Save className="w-4 h-4" />
              <span>user.cfg Speichern</span>
            </button>
          ) : (
            <button
              onClick={handleCreateKeybindBackup}
              disabled={actionLoading !== null}
              className="flex items-center space-x-2 px-4 py-2 rounded-lg bg-purple-600 hover:bg-purple-500 text-white text-xs font-bold shadow-lg shadow-purple-600/25 border border-purple-400 transition cursor-pointer"
            >
              <Archive className="w-4 h-4" />
              <span>✦ Keybinds sichern</span>
            </button>
          )}
        </div>
      </div>

      {/* Sub-Tab Navigation Bar */}
      <div className="flex gap-2 border-b border-slate-800 pb-3">
        <button
          onClick={() => setActiveSubTab('maintenance')}
          className={`flex items-center space-x-2 px-4 py-2 rounded-lg text-xs font-medium transition cursor-pointer ${
            activeSubTab === 'maintenance'
              ? 'bg-sky-500/20 text-sky-300 border border-sky-500/40 shadow-sm'
              : 'bg-slate-900/40 hover:bg-slate-800/60 text-slate-400 hover:text-slate-200 border border-transparent'
          }`}
        >
          <Wrench className="w-4 h-4" />
          <span>System & Wartung</span>
        </button>
        <button
          onClick={() => setActiveSubTab('backups')}
          className={`flex items-center space-x-2 px-4 py-2 rounded-lg text-xs font-medium transition cursor-pointer ${
            activeSubTab === 'backups'
              ? 'bg-purple-500/20 text-purple-300 border border-purple-500/40 shadow-sm'
              : 'bg-slate-900/40 hover:bg-slate-800/60 text-slate-400 hover:text-slate-200 border border-transparent'
          }`}
        >
          <Archive className="w-4 h-4" />
          <span>💾 Backups &amp; Tresor</span>
        </button>
      </div>

      {/* SubTab 1: System & Wartung */}
      {activeSubTab === 'maintenance' && (
        <div className="space-y-6">
          {/* Top Diagnostics & Cache Cards Grid */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
        {/* Shader Cache Card */}
        <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800/80 hover:border-slate-700 transition flex flex-col justify-between">
          <div className="flex items-center justify-between mb-3">
            <div className="flex items-center space-x-2">
              <HardDrive className="w-5 h-5 text-amber-400" />
              <h3 className="text-sm font-semibold text-white">DirectX / Vulkan Shader-Cache</h3>
            </div>
            <span className="text-xs px-2 py-0.5 rounded bg-amber-950/60 text-amber-400 border border-amber-800/60 font-mono">
              {status?.shaderCacheMb ? `${status.shaderCacheMb.toFixed(1)} MB` : '0 MB'}
            </span>
          </div>
          <p className="text-xs text-slate-400 mb-4 leading-relaxed">
            Behebt Shader-Stottern und Ruckler nach Patches. Die Pipeline kompiliert beim nächsten Spielstart sauber neu.
          </p>
          <button
            onClick={handleClearShaderCache}
            disabled={actionLoading === 'shaders' || (status?.shaderCacheMb || 0) === 0}
            className="w-full flex items-center justify-center space-x-2 py-2 px-3 rounded-lg bg-amber-500/10 hover:bg-amber-500/20 text-amber-300 text-xs font-semibold border border-amber-500/30 transition disabled:opacity-40"
          >
            <Trash2 className="w-4 h-4" />
            <span>{actionLoading === 'shaders' ? 'Bereinige...' : 'Shader-Cache leeren'}</span>
          </button>
        </div>

        {/* Crash Dumps Card */}
        <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800/80 hover:border-slate-700 transition flex flex-col justify-between">
          <div className="flex items-center justify-between mb-3">
            <div className="flex items-center space-x-2">
              <AlertCircle className="w-5 h-5 text-rose-400" />
              <h3 className="text-sm font-semibold text-white">Crash-Dumps & Fehlerprotokolle</h3>
            </div>
            <span className="text-xs px-2 py-0.5 rounded bg-rose-950/60 text-rose-400 border border-rose-800/60 font-mono">
              {status?.crashDumpsMb ? `${status.crashDumpsMb.toFixed(1)} MB` : '0 MB'}
            </span>
          </div>
          <p className="text-xs text-slate-400 mb-4 leading-relaxed">
            Alte Star Citizen Minidumps (.dmp) und Absturzberichte im AppData-Verzeichnis freigeben.
          </p>
          <button
            onClick={handleClearCrashDumps}
            disabled={actionLoading === 'dumps' || (status?.crashDumpsMb || 0) === 0}
            className="w-full flex items-center justify-center space-x-2 py-2 px-3 rounded-lg bg-rose-500/10 hover:bg-rose-500/20 text-rose-300 text-xs font-semibold border border-rose-500/30 transition disabled:opacity-40"
          >
            <Trash2 className="w-4 h-4" />
            <span>{actionLoading === 'dumps' ? 'Lösche...' : 'Crash-Dumps bereinigen'}</span>
          </button>
        </div>

        {/* System Diagnostics Card */}
        <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800/80 hover:border-slate-700 transition flex flex-col justify-between">
          <div className="flex items-center justify-between mb-3">
            <div className="flex items-center space-x-2">
              <Cpu className="w-5 h-5 text-emerald-400" />
              <h3 className="text-sm font-semibold text-white">System & Hardware-Check</h3>
            </div>
            <span className="text-xs px-2 py-0.5 rounded bg-emerald-950/60 text-emerald-400 border border-emerald-800/60 font-mono">
              Optimal
            </span>
          </div>
          <div className="space-y-2 text-xs text-slate-300 mb-2 font-mono">
            <div className="flex justify-between">
              <span className="text-slate-400">Arbeitsspeicher:</span>
              <span className="font-semibold text-emerald-300">{status?.ramStatus || '32 GB'}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-400">Laufwerk {status?.driveName || 'C:'}:</span>
              <span>{status?.freeDiskGb ? `${status.freeDiskGb.toFixed(1)} GB frei` : 'Frei'}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-400">Auslagerungsdatei:</span>
              <span>{status?.pagefileStatus || 'Aktiv auf NVMe SSD'}</span>
            </div>
          </div>
          <div className="pt-2 border-t border-slate-800/60 flex items-center space-x-2 text-[11px] text-slate-400">
            <ShieldCheck className="w-3.5 h-3.5 text-emerald-400" />
            <span>Systemvoraussetzungen für 4.0+ erfüllt</span>
          </div>
        </div>
      </div>

      {/* Main Studio: 2 Columns (Left: Presets & Parameters, Right: Live Editor & Backups) */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Left Column: Presets & Tuning Parameters (5 cols) */}
        <div className="lg:col-span-5 space-y-6">
          {/* Presets Box */}
          <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-4">
            <div className="flex items-center space-x-2 text-amber-400 font-semibold text-sm">
              <Sparkles className="w-4 h-4" />
              <span>SCHNELL-TUNING PROFILE</span>
            </div>

            <div className="space-y-2.5">
              <button
                onClick={() => applyPreset('esport')}
                className="w-full p-3 rounded-lg bg-slate-800/70 hover:bg-slate-800 text-left border border-slate-700/80 hover:border-sky-500/60 transition group flex items-center justify-between"
              >
                <div>
                  <div className="text-xs font-bold text-sky-400 group-hover:text-sky-300">
                    ⚡ High FPS / E-Sport Preset
                  </div>
                  <div className="text-[11px] text-slate-400 mt-0.5">
                    165 FPS Limit, VSync Aus, 8GB StreamPool, DisplayInfo=1
                  </div>
                </div>
                <span className="text-xs px-2 py-1 rounded bg-sky-950 text-sky-400 border border-sky-800">
                  Wählen
                </span>
              </button>

              <button
                onClick={() => applyPreset('quality')}
                className="w-full p-3 rounded-lg bg-slate-800/70 hover:bg-slate-800 text-left border border-slate-700/80 hover:border-purple-500/60 transition group flex items-center justify-between"
              >
                <div>
                  <div className="text-xs font-bold text-purple-400 group-hover:text-purple-300">
                    🎨 Grafik & Immersion Preset
                  </div>
                  <div className="text-[11px] text-slate-400 mt-0.5">
                    Unbegrenzt FPS, VSync Ein, 12GB StreamPool, DisplayInfo=0
                  </div>
                </div>
                <span className="text-xs px-2 py-1 rounded bg-purple-950 text-purple-400 border border-purple-800">
                  Wählen
                </span>
              </button>

              <button
                onClick={() => applyPreset('minimal')}
                className="w-full p-3 rounded-lg bg-slate-800/70 hover:bg-slate-800 text-left border border-slate-700/80 hover:border-slate-500/60 transition group flex items-center justify-between"
              >
                <div>
                  <div className="text-xs font-bold text-slate-300 group-hover:text-white">
                    💻 Minimal / Einsteiger-PC
                  </div>
                  <div className="text-[11px] text-slate-400 mt-0.5">
                    60 FPS Cap, 4GB StreamPool, maximale Stabilität
                  </div>
                </div>
                <span className="text-xs px-2 py-1 rounded bg-slate-800 text-slate-300 border border-slate-700">
                  Wählen
                </span>
              </button>
            </div>
          </div>

          {/* Detailed Tuning Controls */}
          <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-4">
            <div className="flex items-center space-x-2 text-sky-400 font-semibold text-sm">
              <Terminal className="w-4 h-4" />
              <span>TUNING-PARAMETER (LIVE GENERATOR)</span>
            </div>

            <div className="space-y-3 text-xs">
              <label className="flex items-center space-x-3 cursor-pointer p-2 rounded hover:bg-slate-800/40">
                <input
                  type="checkbox"
                  checked={consoleUnlocked}
                  onChange={(e) => {
                    setConsoleUnlocked(e.target.checked);
                    setCfgContent(
                      generateCfgFromControls(
                        vsyncOff,
                        motionBlurOff,
                        e.target.checked,
                        displayInfo,
                        maxFps,
                        streamPool,
                        langEnglish
                      )
                    );
                  }}
                  className="rounded border-slate-700 text-sky-600 focus:ring-sky-500 bg-slate-800"
                />
                <span className="text-slate-200">
                  Konsole freigeben <code className="text-slate-400">(Con_Restricted=0)</code>
                </span>
              </label>

              <label className="flex items-center space-x-3 cursor-pointer p-2 rounded hover:bg-slate-800/40">
                <input
                  type="checkbox"
                  checked={vsyncOff}
                  onChange={(e) => {
                    setVsyncOff(e.target.checked);
                    setCfgContent(
                      generateCfgFromControls(
                        e.target.checked,
                        motionBlurOff,
                        consoleUnlocked,
                        displayInfo,
                        maxFps,
                        streamPool,
                        langEnglish
                      )
                    );
                  }}
                  className="rounded border-slate-700 text-sky-600 focus:ring-sky-500 bg-slate-800"
                />
                <span className="text-slate-200">
                  VSync deaktivieren <code className="text-slate-400">(r_VSync=0)</code>
                </span>
              </label>

              <label className="flex items-center space-x-3 cursor-pointer p-2 rounded hover:bg-slate-800/40">
                <input
                  type="checkbox"
                  checked={motionBlurOff}
                  onChange={(e) => {
                    setMotionBlurOff(e.target.checked);
                    setCfgContent(
                      generateCfgFromControls(
                        vsyncOff,
                        e.target.checked,
                        consoleUnlocked,
                        displayInfo,
                        maxFps,
                        streamPool,
                        langEnglish
                      )
                    );
                  }}
                  className="rounded border-slate-700 text-sky-600 focus:ring-sky-500 bg-slate-800"
                />
                <span className="text-slate-200">
                  Bewegungsunschärfe aus <code className="text-slate-400">(r_MotionBlur=0)</code>
                </span>
              </label>

              <div className="pt-2 border-t border-slate-800 space-y-3">
                <div className="flex items-center justify-between">
                  <span className="text-slate-300">DisplayInfo Telemetrie:</span>
                  <select
                    value={displayInfo}
                    onChange={(e) => {
                      const val = parseInt(e.target.value, 10);
                      setDisplayInfo(val);
                      setCfgContent(
                        generateCfgFromControls(
                          vsyncOff,
                          motionBlurOff,
                          consoleUnlocked,
                          val,
                          maxFps,
                          streamPool,
                          langEnglish
                        )
                      );
                    }}
                    className="bg-slate-800 border border-slate-700 rounded px-2 py-1 text-slate-200 text-xs focus:outline-none focus:border-sky-500"
                  >
                    <option value={0}>0 (Aus)</option>
                    <option value={1}>1 (FPS / Server Tick)</option>
                    <option value={2}>2 (Erweitert / Render)</option>
                    <option value={3}>3 (Vollständige Debug-Info)</option>
                  </select>
                </div>

                <div className="flex items-center justify-between">
                  <span className="text-slate-300">Max FPS Begrenzung:</span>
                  <div className="flex items-center space-x-2">
                    <input
                      type="number"
                      min={0}
                      max={360}
                      step={10}
                      value={maxFps}
                      onChange={(e) => {
                        const val = parseInt(e.target.value, 10) || 0;
                        setMaxFps(val);
                        setCfgContent(
                          generateCfgFromControls(
                            vsyncOff,
                            motionBlurOff,
                            consoleUnlocked,
                            displayInfo,
                            val,
                            streamPool,
                            langEnglish
                          )
                        );
                      }}
                      className="w-20 bg-slate-800 border border-slate-700 rounded px-2 py-1 text-slate-200 text-xs font-mono text-right focus:outline-none focus:border-sky-500"
                    />
                    <span className="text-[11px] text-slate-400">FPS</span>
                  </div>
                </div>

                <div className="flex items-center justify-between">
                  <span className="text-slate-300">Textur-StreamPool:</span>
                  <select
                    value={streamPool}
                    onChange={(e) => {
                      const val = parseInt(e.target.value, 10);
                      setStreamPool(val);
                      setCfgContent(
                        generateCfgFromControls(
                          vsyncOff,
                          motionBlurOff,
                          consoleUnlocked,
                          displayInfo,
                          maxFps,
                          val,
                          langEnglish
                        )
                      );
                    }}
                    className="bg-slate-800 border border-slate-700 rounded px-2 py-1 text-slate-200 text-xs focus:outline-none focus:border-sky-500"
                  >
                    <option value={4096}>4096 MB (6-8 GB VRAM)</option>
                    <option value={6144}>6144 MB (10-12 GB VRAM)</option>
                    <option value={8192}>8192 MB (16 GB VRAM)</option>
                    <option value={12288}>12288 MB (24 GB VRAM / 4090)</option>
                  </select>
                </div>
              </div>
            </div>
          </div>

          {/* Keybinds Backup Section */}
          <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-2 text-sky-400 font-semibold text-sm">
                <Key className="w-4 h-4" />
                <span>KEYBINDS & PROFILE-ARCHIV</span>
              </div>
              <span className="text-xs text-slate-400">
                {status?.keybindBackups?.length || 0} Backups
              </span>
            </div>

            <div className="flex items-center space-x-2">
              <input
                type="text"
                placeholder="Optionale Notiz (z. B. HOSAS Dual-Stick 3.24)..."
                value={keybindNote}
                onChange={(e) => setKeybindNote(e.target.value)}
                className="flex-1 bg-slate-800/80 border border-slate-700 rounded-lg px-3 py-2 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-sky-500 font-mono"
              />
              <button
                onClick={handleCreateKeybindBackup}
                disabled={actionLoading === 'backupKeybind'}
                className="px-3 py-2 rounded-lg bg-sky-600/20 hover:bg-sky-600/30 text-sky-300 text-xs font-semibold border border-sky-500/40 transition shrink-0 cursor-pointer"
              >
                {actionLoading === 'backupKeybind' ? 'Sichere...' : '+ Backup'}
              </button>
            </div>

            <div className="space-y-2 max-h-44 overflow-y-auto pr-1">
              {status?.keybindBackups && status.keybindBackups.length > 0 ? (
                status.keybindBackups.map((b, idx) => (
                  <div
                    key={idx}
                    className="p-2.5 rounded-lg bg-slate-800/40 border border-slate-800 flex items-center justify-between text-xs"
                  >
                    <div className="flex items-center space-x-2">
                      <FileText className="w-3.5 h-3.5 text-slate-400 shrink-0" />
                      <span className="text-slate-200 font-mono">{b}</span>
                    </div>
                    <span className="text-[10px] px-2 py-0.5 rounded bg-emerald-950 text-emerald-400 border border-emerald-800 font-medium">
                      Gesichert
                    </span>
                  </div>
                ))
              ) : (
                <p className="text-xs text-slate-500 italic py-2">
                  Noch keine manuellen Keybind-Backups vorhanden. Klicke auf "+ Backup", um deine Belegungen vor Patches zu sichern.
                </p>
              )}
            </div>
          </div>
        </div>

        {/* Right Column: user.cfg Live Code Editor (7 cols) */}
        <div className="lg:col-span-7 flex flex-col space-y-4">
          <div className="flex-1 p-5 rounded-xl bg-slate-900/80 border border-slate-800 flex flex-col shadow-xl">
            {/* Editor Header Bar */}
            <div className="flex items-center justify-between pb-3 border-b border-slate-800 mb-3">
              <div className="flex items-center space-x-2">
                <FileText className="w-4 h-4 text-sky-400" />
                <span className="text-sm font-semibold text-white">user.cfg Editor</span>
                <span className="text-xs text-slate-500 font-mono">
                  ({cfgContent.split('\n').length} Zeilen)
                </span>
              </div>

              <div className="flex items-center space-x-2">
                <button
                  onClick={copyToClipboard}
                  className="flex items-center space-x-1 px-2.5 py-1 rounded bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs transition"
                  title="In Zwischenablage kopieren"
                >
                  {copied ? (
                    <>
                      <Check className="w-3 h-3 text-emerald-400" />
                      <span className="text-emerald-400">Kopiert</span>
                    </>
                  ) : (
                    <>
                      <Copy className="w-3 h-3" />
                      <span>Kopieren</span>
                    </>
                  )}
                </button>
                <button
                  onClick={handleSaveUserCfg}
                  disabled={actionLoading === 'saveCfg'}
                  className="flex items-center space-x-1.5 px-3 py-1 rounded bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold transition shadow-md shadow-sky-600/20"
                >
                  <Save className="w-3.5 h-3.5" />
                  <span>Speichern & Anwenden</span>
                </button>
              </div>
            </div>

            {/* Editor Textarea with Line Numbers Styling */}
            <div className="flex-1 relative rounded-lg bg-slate-950 border border-slate-800/80 font-mono text-xs overflow-hidden flex">
              <textarea
                value={cfgContent}
                onChange={(e) => setCfgContent(e.target.value)}
                spellCheck={false}
                className="w-full h-full min-h-[480px] p-4 bg-transparent text-sky-100 resize-none focus:outline-none font-mono text-xs leading-relaxed selection:bg-sky-800/50"
                placeholder="r_VSync = 0&#10;sys_maxfps = 120&#10;..."
              />
            </div>

            {/* Editor Footer Help */}
            <div className="pt-3 mt-3 border-t border-slate-800 flex items-center justify-between text-[11px] text-slate-400">
              <div className="flex items-center space-x-1">
                <FolderOpen className="w-3.5 h-3.5 text-slate-500" />
                <span>Wird direkt im Star Citizen LIVE Stammordner abgelegt</span>
              </div>
              <span className="text-slate-500 font-mono">UTF-8 No BOM</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  )}

      {/* SubTab 2: Backups & Tresor (RC2 Star Citizen Tresor & Backup-Zentrale) */}
      {activeSubTab === 'backups' && (
        <div className="space-y-6">
          {/* Top Banner */}
          <div className="p-5 rounded-xl bg-gradient-to-r from-purple-950/40 via-slate-900/60 to-sky-950/40 border border-purple-800/40 backdrop-blur shadow-lg flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div className="flex items-center space-x-4">
              <div className="w-12 h-12 rounded-lg bg-purple-500/10 border border-purple-500/30 flex items-center justify-center text-purple-400 shadow-inner shrink-0">
                <Archive className="w-6 h-6" />
              </div>
              <div>
                <div className="flex items-center space-x-2">
                  <h1 className="text-base font-bold text-white tracking-wide">
                    STAR CITIZEN TRESOR &amp; BACKUP-ZENTRALE
                  </h1>
                </div>
                <p className="text-xs text-slate-400 mt-1 font-mono">
                  Zentrale Verwaltung aller Steuerungsbelegungen, user.cfg-Snapshots und Cloud-Replikation.
                </p>
              </div>
            </div>

            <div className="flex flex-wrap items-center gap-2">
              <button
                onClick={() => handleOpenFolder('keybinds')}
                title="Öffnet den lokalen Keybind-Backup-Ordner"
                className="flex items-center space-x-1.5 px-3 py-1.5 rounded-lg bg-purple-950/50 hover:bg-purple-900/70 text-purple-300 text-xs font-semibold border border-purple-800/60 transition cursor-pointer"
              >
                <span>🎮 Keybinds-Ordner</span>
                <ExternalLink className="w-3 h-3" />
              </button>
              <button
                onClick={() => handleOpenFolder('config')}
                title="Öffnet den lokalen Config-Backup-Ordner"
                className="flex items-center space-x-1.5 px-3 py-1.5 rounded-lg bg-sky-950/50 hover:bg-sky-900/70 text-sky-300 text-xs font-semibold border border-sky-800/60 transition cursor-pointer"
              >
                <span>⚙️ Config-Ordner</span>
                <ExternalLink className="w-3 h-3" />
              </button>
              <button
                onClick={() => handleOpenFolder('cloud')}
                title="Öffnet den konfigurierten Cloud-Speicherordner"
                className="flex items-center space-x-1.5 px-3 py-1.5 rounded-lg bg-slate-900/80 hover:bg-slate-800 text-slate-300 text-xs font-semibold border border-slate-700 transition cursor-pointer"
              >
                <span>☁️ Cloud-Ordner</span>
                <ExternalLink className="w-3 h-3" />
              </button>
            </div>
          </div>

          {/* 2-Spalten Layout: Keybind-Tresor links, user.cfg & Cloud rechts */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            
            {/* Linke Spalte: Keybind-Tresor */}
            <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-4 flex flex-col justify-between">
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <div className="w-7 h-7 rounded-lg bg-purple-500/10 border border-purple-500/30 flex items-center justify-center text-purple-400">
                      <Key className="w-4 h-4" />
                    </div>
                    <h2 className="text-xs font-bold text-purple-300 tracking-wider">
                      KEYBIND-TRESOR (ACTIONMAPS.XML)
                    </h2>
                  </div>
                  <div className="flex items-center space-x-2">
                    <span className="text-[11px] px-2.5 py-0.5 rounded-full bg-purple-950 text-purple-300 border border-purple-800 font-mono font-semibold">
                      {status?.keybindItems?.length ?? status?.keybindBackups?.length ?? 0} Gesichert
                    </span>
                    <button
                      onClick={() => handleOpenFolder('keybinds')}
                      title="Keybind-Ordner im Windows Explorer öffnen"
                      className="p-1.5 rounded-md bg-purple-950/40 hover:bg-purple-900/60 text-purple-300 border border-purple-800/60 transition cursor-pointer"
                    >
                      <ExternalLink className="w-3 h-3" />
                    </button>
                  </div>
                </div>

                <p className="text-xs text-slate-400">
                  Sichert alle Steuerungs-Mappings &amp; Joystick-Profile vor Spiel-Updates – lokal und in deiner Cloud.
                </p>

                {/* Neues Keybind-Backup anlegen */}
                <div className="p-3 rounded-lg bg-slate-950/80 border border-slate-800/80 space-y-2.5">
                  <div className="text-xs font-semibold text-slate-300">
                    Neues Keybind-Backup erstellen
                  </div>
                  <div className="flex items-center space-x-2">
                    <input
                      type="text"
                      placeholder="Optionale Notiz (z. B. HOSAS VKB Patch 4.0)..."
                      value={keybindNote}
                      onChange={(e) => setKeybindNote(e.target.value)}
                      className="flex-1 bg-slate-900 border border-slate-700 rounded-lg px-3 py-2 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-purple-500 font-mono"
                    />
                    <button
                      onClick={handleCreateKeybindBackup}
                      disabled={actionLoading !== null}
                      className="flex items-center space-x-1.5 px-4 py-2 rounded-lg bg-purple-600 hover:bg-purple-500 text-white text-xs font-bold shadow-lg shadow-purple-600/25 border border-purple-400 transition cursor-pointer shrink-0 disabled:opacity-50"
                    >
                      <Archive className={`w-3.5 h-3.5 ${actionLoading === 'backupKeybind' ? 'animate-spin' : ''}`} />
                      <span>✦ Jetzt sichern</span>
                    </button>
                  </div>
                </div>

                {/* Backup-Liste */}
                <div className="space-y-2">
                  <div className="flex items-center justify-between text-xs font-semibold text-slate-300">
                    <span>Gespeicherte Profile</span>
                    <span className="text-[11px] text-slate-500 font-normal">Klicke zum Auswählen</span>
                  </div>

                  {status?.keybindItems && status.keybindItems.length > 0 ? (
                    <div className="space-y-2 overflow-y-auto max-h-[260px] pr-1">
                      {status.keybindItems.map((item, idx) => {
                        const isSelected = selectedKeybindIndex === idx;
                        return (
                          <div
                            key={idx}
                            onClick={() => setSelectedKeybindIndex(idx)}
                            className={`p-3 rounded-lg border transition cursor-pointer flex items-center justify-between gap-3 ${
                              isSelected
                                ? 'bg-purple-950/40 border-purple-500/60 shadow-[0_0_12px_rgba(168,85,247,0.15)]'
                                : 'bg-slate-950/80 border-slate-800/80 hover:border-slate-700'
                            }`}
                          >
                            <div className="flex items-center space-x-3 min-w-0">
                              <div
                                className={`w-8 h-8 rounded-lg flex items-center justify-center shrink-0 border ${
                                  isSelected
                                    ? 'bg-purple-500/20 border-purple-500/40 text-purple-300'
                                    : 'bg-slate-900 border-slate-800 text-slate-400'
                                }`}
                              >
                                <Key className="w-4 h-4" />
                              </div>
                              <div className="min-w-0">
                                <div className="text-xs font-bold text-slate-200 truncate font-mono">
                                  {item.name}
                                </div>
                                <div className="text-[10px] text-slate-500 mt-0.5">
                                  Erstellt: {item.createdAt}
                                </div>
                              </div>
                            </div>
                            <div className="flex items-center space-x-2 shrink-0">
                              <span
                                className={`text-[10px] px-2 py-0.5 rounded font-semibold border ${
                                  item.locationType.includes('Cloud')
                                    ? 'bg-sky-950/80 text-sky-400 border-sky-800'
                                    : 'bg-slate-900 text-slate-300 border-slate-700'
                                }`}
                              >
                                {item.locationType}
                              </span>
                              <span className="text-[10px] font-mono text-slate-400">
                                {item.sizeFormatted}
                              </span>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  ) : status?.keybindBackups && status.keybindBackups.length > 0 ? (
                    <div className="space-y-2 overflow-y-auto max-h-[260px] pr-1">
                      {status.keybindBackups.map((name, idx) => {
                        const isSelected = selectedKeybindIndex === idx;
                        return (
                          <div
                            key={idx}
                            onClick={() => setSelectedKeybindIndex(idx)}
                            className={`p-3 rounded-lg border transition cursor-pointer flex items-center justify-between gap-3 ${
                              isSelected
                                ? 'bg-purple-950/40 border-purple-500/60 shadow-[0_0_12px_rgba(168,85,247,0.15)]'
                                : 'bg-slate-950/80 border-slate-800/80 hover:border-slate-700'
                            }`}
                          >
                            <div className="flex items-center space-x-3 min-w-0">
                              <div className="w-8 h-8 rounded-lg bg-purple-500/20 border border-purple-500/40 flex items-center justify-center text-purple-300 shrink-0">
                                <Key className="w-4 h-4" />
                              </div>
                              <div className="min-w-0">
                                <div className="text-xs font-bold text-slate-200 truncate font-mono">
                                  {name}
                                </div>
                                <div className="text-[10px] text-slate-500 mt-0.5">
                                  actionmaps.xml Sicherung
                                </div>
                              </div>
                            </div>
                            <span className="text-[10px] px-2 py-0.5 rounded font-semibold border bg-slate-900 text-slate-300 border-slate-700">
                              Lokal
                            </span>
                          </div>
                        );
                      })}
                    </div>
                  ) : (
                    <div className="p-8 rounded-xl bg-slate-950 border border-slate-800/60 text-center text-xs text-slate-500 italic">
                      Noch keine Keybind-Backups vorhanden. Klicke auf "✦ Jetzt sichern", um deine Belegungen vor Patches zu sichern.
                    </div>
                  )}
                </div>
              </div>

              {/* Wiederherstellen Bar */}
              <div className="pt-3 border-t border-slate-800/80 space-y-3">
                <div className="p-3 rounded-lg bg-slate-950/90 border border-slate-800/80 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                  <div className="text-xs text-slate-400">
                    <span>Ausgewähltes Profil wiederherstellen:</span>
                    {status?.keybindItems && status.keybindItems[selectedKeybindIndex] && (
                      <span className="ml-1.5 text-purple-300 font-mono font-semibold">
                        {status.keybindItems[selectedKeybindIndex].name}
                      </span>
                    )}
                  </div>
                  <button
                    onClick={handleRestoreKeybind}
                    disabled={actionLoading !== null || !status?.keybindItems?.length}
                    className="flex items-center justify-center space-x-2 px-4 py-2 rounded-lg bg-purple-600 hover:bg-purple-500 text-white text-xs font-bold shadow-lg shadow-purple-600/25 border border-purple-400 transition cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed shrink-0"
                  >
                    <RotateCcw className={`w-3.5 h-3.5 ${actionLoading === 'restoreKeybind' ? 'animate-spin' : ''}`} />
                    <span>↺ Wiederherstellen</span>
                  </button>
                </div>

                <div className="p-3 rounded-lg bg-purple-950/20 border border-purple-900/30 flex items-start space-x-2.5 text-xs text-slate-400">
                  <Sparkles className="w-4 h-4 text-purple-400 shrink-0 mt-0.5" />
                  <div className="text-[11px] leading-relaxed">
                    Backups werden sicher in <code className="text-purple-300">USER\Client\0\Controls\Mappings</code> und im SCLogMate-Tresor verwahrt.
                  </div>
                </div>
              </div>
            </div>

            {/* Rechte Spalte: user.cfg Snapshots & Cloud-Speicher */}
            <div className="space-y-6">
              
              {/* Card 1: user.cfg Snapshots */}
              <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-4">
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <div className="w-7 h-7 rounded-lg bg-sky-500/10 border border-sky-500/30 flex items-center justify-center text-sky-400">
                      <FileText className="w-3.5 h-3.5" />
                    </div>
                    <h2 className="text-xs font-bold text-sky-400 tracking-wider">
                      USER.CFG VERSIONS-ARCHIV
                    </h2>
                  </div>
                  <div className="flex items-center space-x-2">
                    <span className="text-[11px] px-2.5 py-0.5 rounded-full bg-sky-950 text-sky-400 border border-sky-800 font-mono font-semibold">
                      {status?.configBackups?.length || 0} Versionen
                    </span>
                    <button
                      onClick={() => handleOpenFolder('config')}
                      title="Config-Ordner im Windows Explorer öffnen"
                      className="p-1.5 rounded-md bg-sky-950/40 hover:bg-sky-900/60 text-sky-300 border border-sky-800/60 transition cursor-pointer"
                    >
                      <ExternalLink className="w-3 h-3" />
                    </button>
                  </div>
                </div>

                <p className="text-xs text-slate-400">
                  Historische Versionen deiner Tuning-Konfiguration. Schneller Rollback auf bewährte Setups.
                </p>

                {/* Dropdown & Rollback */}
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-2">
                  <select
                    value={selectedConfigIndex}
                    onChange={(e) => setSelectedConfigIndex(parseInt(e.target.value, 10) || 0)}
                    className="sm:col-span-2 bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-xs font-mono text-slate-200 focus:outline-none focus:border-sky-500 cursor-pointer"
                  >
                    {status?.configBackups && status.configBackups.length > 0 ? (
                      status.configBackups.map((c, idx) => (
                        <option key={idx} value={idx}>
                          {c.name} ({c.createdAt}) [{c.locationType}]
                        </option>
                      ))
                    ) : (
                      <option value={0}>Keine Versionen vorhanden</option>
                    )}
                  </select>

                  <button
                    onClick={handleRestoreConfigSnapshot}
                    disabled={actionLoading !== null || !status?.configBackups?.length}
                    className="flex items-center justify-center space-x-1.5 px-3 py-2 rounded-lg bg-sky-600/90 hover:bg-sky-500 text-white text-xs font-bold border border-sky-400 shadow-md shadow-sky-600/20 transition cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <RotateCcw className={`w-3.5 h-3.5 ${actionLoading === 'restoreCfg' ? 'animate-spin' : ''}`} />
                    <span>↺ Rollback</span>
                  </button>
                </div>

                {/* Snapshot archivieren */}
                <div className="pt-2 border-t border-slate-800/80 flex flex-col sm:flex-row gap-2">
                  <input
                    type="text"
                    placeholder="Optionale Notiz (z. B. Vor 4.0 Patch)..."
                    value={configNote}
                    onChange={(e) => setConfigNote(e.target.value)}
                    className="flex-1 bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-sky-500 font-mono"
                  />
                  <button
                    onClick={handleBackupUserCfgSnapshot}
                    disabled={actionLoading !== null}
                    className="flex items-center justify-center space-x-1.5 px-3 py-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold border border-slate-700 transition cursor-pointer shrink-0 disabled:opacity-50"
                  >
                    <Download className="w-3.5 h-3.5 text-sky-400" />
                    <span>⤓ Stand archivieren</span>
                  </button>
                </div>
              </div>

              {/* Card 2: Cloud-Speicher & Log-Archiv */}
              <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-4">
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <div className="w-7 h-7 rounded-lg bg-sky-500/10 border border-sky-500/30 flex items-center justify-center text-sky-400">
                      <Cloud className="w-3.5 h-3.5" />
                    </div>
                    <h2 className="text-xs font-bold text-sky-400 tracking-wider">
                      CLOUD-SPEICHER &amp; LOG-ARCHIV
                    </h2>
                  </div>
                  <span className="text-[11px] px-2.5 py-0.5 rounded-full bg-slate-800 text-slate-300 border border-slate-700 font-mono">
                    OneDrive / Dropbox
                  </span>
                </div>

                <p className="text-xs text-slate-400">
                  Synchronisiere Sicherungen und exportiere Log-Historien direkt in deinen Cloud-Dienst.
                </p>

                {/* Cloud Path Input */}
                <div className="flex items-center space-x-2">
                  <input
                    type="text"
                    placeholder="Cloud-Pfad (z. B. C:\Users\...\OneDrive\StarCitizen)..."
                    value={cloudPath}
                    onChange={(e) => setCloudPath(e.target.value)}
                    className="flex-1 bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-xs font-mono text-slate-200 placeholder-slate-500 focus:outline-none focus:border-sky-500"
                  />
                  <button
                    onClick={handleSaveCloudPath}
                    disabled={actionLoading !== null}
                    title="Pfad in Einstellungen speichern"
                    className="px-3 py-2 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold border border-sky-400 transition cursor-pointer shrink-0 disabled:opacity-50"
                  >
                    Speichern
                  </button>
                  <button
                    onClick={() => handleOpenFolder('cloud')}
                    title="Im Windows Explorer öffnen"
                    className="p-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-sky-400 border border-slate-700 transition cursor-pointer shrink-0"
                  >
                    <FolderOpen className="w-4 h-4" />
                  </button>
                </div>

                {/* Action Buttons */}
                <div className="grid grid-cols-2 gap-3 pt-2 border-t border-slate-800/80">
                  <button
                    onClick={handleExportLogsZip}
                    disabled={actionLoading !== null}
                    className="flex items-center justify-center space-x-2 p-2.5 rounded-lg bg-slate-950 hover:bg-slate-850 text-slate-200 text-xs font-semibold border border-slate-800 hover:border-slate-700 transition cursor-pointer disabled:opacity-50"
                  >
                    <Download className="w-3.5 h-3.5 text-slate-400" />
                    <span>⤓ Logs als ZIP</span>
                  </button>
                  <button
                    onClick={handleSyncLogsCloud}
                    disabled={actionLoading !== null}
                    className="flex items-center justify-center space-x-2 p-2.5 rounded-lg bg-sky-950/40 hover:bg-sky-900/60 text-sky-300 text-xs font-bold border border-sky-800/60 transition cursor-pointer disabled:opacity-50"
                  >
                    <Cloud className="w-3.5 h-3.5 text-sky-400" />
                    <span>☁ In Cloud sichern</span>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
