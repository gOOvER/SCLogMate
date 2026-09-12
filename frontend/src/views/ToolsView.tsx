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
  Sliders,
  Clock,
  CheckCircle,
} from 'lucide-react';
import { bridge, ToolsStatusDto, ConfigBackupItemDto, KeybindBackupItemDto } from '../services/photinoBridge';

/**
 * Intelligent non-destructive user.cfg merger:
 * Updates targeted CVARs while preserving 100% of existing comments (; # //),
 * custom cvars (FOV, sharpening, resolutions, custom graphics commands),
 * and custom formatting.
 */
function mergeCfgContent(
  currentText: string,
  updates: Record<string, string | number>
): string {
  if (!currentText.trim()) {
    const lines = [
      '; --- Star Citizen Konfiguration (SCLogMate) ---',
      ...Object.entries(updates).map(([k, v]) => `${k} = ${v}`),
    ];
    return lines.join('\n');
  }

  const lines = currentText.split(/\r?\n/);
  const remainingKeys = new Set(Object.keys(updates).map((k) => k.toLowerCase()));
  const updateMap: Record<string, { key: string; value: string | number }> = {};
  for (const [k, v] of Object.entries(updates)) {
    updateMap[k.toLowerCase()] = { key: k, value: v };
  }

  const resultLines: string[] = [];

  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith(';') || trimmed.startsWith('#') || trimmed.startsWith('//')) {
      resultLines.push(line);
      continue;
    }

    const eqIdx = line.indexOf('=');
    if (eqIdx > 0) {
      const key = line.substring(0, eqIdx).trim();
      const keyLower = key.toLowerCase();
      if (updateMap[keyLower]) {
        const leadingWhitespace = line.match(/^\s*/)?.[0] || '';
        resultLines.push(`${leadingWhitespace}${updateMap[keyLower].key} = ${updateMap[keyLower].value}`);
        remainingKeys.delete(keyLower);
        continue;
      }
    }

    resultLines.push(line);
  }

  if (remainingKeys.size > 0) {
    resultLines.push('');
    resultLines.push('; --- Zusätzliche Tuning-Parameter (SCLogMate) ---');
    for (const keyLower of remainingKeys) {
      const item = updateMap[keyLower];
      resultLines.push(`${item.key} = ${item.value}`);
    }
  }

  return resultLines.join('\n');
}

export const ToolsView: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'maintenance' | 'tuning' | 'backups'>('maintenance');
  const [status, setStatus] = useState<ToolsStatusDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [cfgContent, setCfgContent] = useState('');
  const [copied, setCopied] = useState(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  // Backup & Vault State
  const [cloudPath, setCloudPath] = useState('');
  const [configNote, setConfigNote] = useState('');
  const [keybindNote, setKeybindNote] = useState('');

  // Tuning Controls (Parsed from user.cfg)
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
      const rawCfg = data.userCfgContent || '';
      setCfgContent(rawCfg);
      if (data.cloudStoragePath !== undefined) {
        setCloudPath(data.cloudStoragePath || '');
      }
      parseCfgContent(rawCfg);
    } catch (err) {
      console.error('Failed to load tools status:', err);
      showToast('Fehler beim Laden der System-Tools');
    } finally {
      setLoading(false);
    }
  };

  const parseCfgContent = (content: string) => {
    const lines = content.split('\n');
    lines.forEach((line) => {
      const trimmed = line.trim();
      if (trimmed.startsWith(';') || trimmed.startsWith('#') || trimmed.startsWith('//')) return;

      const eqIdx = trimmed.indexOf('=');
      if (eqIdx <= 0) return;
      const key = trimmed.substring(0, eqIdx).trim();
      const val = trimmed.substring(eqIdx + 1).trim();

      if (key.toLowerCase() === 'r_vsync') {
        setVsyncOff(val === '0');
      } else if (key.toLowerCase() === 'r_motionblur') {
        setMotionBlurOff(val === '0');
      } else if (key.toLowerCase() === 'con_restricted') {
        setConsoleUnlocked(val === '0');
      } else if (key.toLowerCase() === 'r_displayinfo') {
        const parsed = parseInt(val, 10);
        if (!isNaN(parsed)) setDisplayInfo(parsed);
      } else if (key.toLowerCase() === 'sys_maxfps') {
        const parsed = parseInt(val, 10);
        if (!isNaN(parsed)) setMaxFps(parsed);
      } else if (key.toLowerCase() === 'r_texturesstreampoolsize') {
        const parsed = parseInt(val, 10);
        if (!isNaN(parsed)) setStreamPool(parsed);
      } else if (key.toLowerCase() === 'g_language') {
        setLangEnglish(val.toLowerCase().includes('english'));
      }
    });
  };

  // Preset Application (Non-destructive merge!)
  const applyPreset = (type: 'esport' | 'quality' | 'minimal') => {
    let updates: Record<string, string | number> = {};

    if (type === 'esport') {
      setVsyncOff(true);
      setMotionBlurOff(true);
      setConsoleUnlocked(true);
      setDisplayInfo(1);
      setMaxFps(165);
      setStreamPool(8192);
      setLangEnglish(true);
      updates = {
        Con_Restricted: 0,
        r_VSync: 0,
        r_MotionBlur: 0,
        sys_maxfps: 165,
        r_TexturesStreamPoolSize: 8192,
        r_DisplayInfo: 1,
        g_language: 'english',
        r_Optane: 1,
        e_PrecacheResources: 1,
      };
      showToast('Profil "High FPS / E-Sport" in user.cfg eingefügt (Bestehende Werte erhalten)');
    } else if (type === 'quality') {
      setVsyncOff(false);
      setMotionBlurOff(false);
      setConsoleUnlocked(true);
      setDisplayInfo(0);
      setMaxFps(0);
      setStreamPool(12288);
      setLangEnglish(true);
      updates = {
        Con_Restricted: 0,
        r_VSync: 1,
        r_MotionBlur: 0,
        sys_maxfps: 0,
        r_TexturesStreamPoolSize: 12288,
        r_DisplayInfo: 0,
        g_language: 'english',
        r_Optane: 1,
        e_PrecacheResources: 1,
      };
      showToast('Profil "Grafik & Immersion" in user.cfg eingefügt (Bestehende Werte erhalten)');
    } else {
      setVsyncOff(true);
      setMotionBlurOff(true);
      setConsoleUnlocked(true);
      setDisplayInfo(1);
      setMaxFps(60);
      setStreamPool(4096);
      setLangEnglish(true);
      updates = {
        Con_Restricted: 0,
        r_VSync: 0,
        r_MotionBlur: 0,
        sys_maxfps: 60,
        r_TexturesStreamPoolSize: 4096,
        r_DisplayInfo: 1,
        g_language: 'english',
        r_Optane: 1,
        e_PrecacheResources: 1,
      };
      showToast('Profil "Minimal / Einsteiger-PC" in user.cfg eingefügt (Bestehende Werte erhalten)');
    }

    setCfgContent((prev) => mergeCfgContent(prev, updates));
  };

  const updateSingleCvar = (key: string, value: string | number) => {
    setCfgContent((prev) => mergeCfgContent(prev, { [key]: value }));
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
      const res = await bridge.send<ToolsStatusDto>('save_user_cfg', {
        content: cfgContent,
        cfgContent: cfgContent,
      });
      setStatus(res);
      showToast('user.cfg gespeichert! (Automatisches Backup lokal & Cloud gesichert)');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Speichern der user.cfg');
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
      showToast(res.message || 'user.cfg Sicherung erfolgreich angelegt!');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Erstellen der user.cfg-Sicherung');
    } finally {
      setActionLoading(null);
    }
  };

  const handleRestoreConfigSnapshot = async (item: ConfigBackupItemDto) => {
    if (!item) return;
    setActionLoading(`restoreCfg_${item.name}`);
    try {
      const res = await bridge.send<{ success: boolean; message: string; tools?: ToolsStatusDto }>('restore_user_cfg', {
        name: item.name,
        filePath: item.filePath,
      });
      if (res.tools) {
        setStatus(res.tools);
        setCfgContent(res.tools.userCfgContent || '');
        parseCfgContent(res.tools.userCfgContent || '');
      }
      showToast(res.message || `Version '${item.name}' wiederhergestellt!`);
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Wiederherstellen der Version');
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
      showToast('Keybind-Backup erfolgreich angelegt (Lokal & Cloud)!');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Erstellen des Keybind-Backups');
    } finally {
      setActionLoading(null);
    }
  };

  const handleRestoreKeybind = async (item: KeybindBackupItemDto) => {
    if (!item) return;
    setActionLoading(`restoreKeybind_${item.name}`);
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

  const handleSaveCloudPath = async () => {
    setActionLoading('saveCloud');
    try {
      const res = await bridge.send<{ success: boolean; tools?: ToolsStatusDto }>('save_cloud_storage_path', {
        path: cloudPath,
      });
      if (res.tools) setStatus(res.tools);
      showToast('Cloud-Speicherpfad erfolgreich hinterlegt!');
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
        <p className="text-sm text-slate-400">Lese Star Citizen Konfiguration & System-Status aus...</p>
      </div>
    );
  }

  const cloudDisplay = status?.cloudStoragePath
    ? status.cloudAutoDetected
      ? `OneDrive (automatisch aktiv)`
      : status.cloudStoragePath
    : 'Nicht konfiguriert';

  return (
    <div className="p-6 space-y-6 max-w-7xl mx-auto">
      {/* Toast Notification */}
      {toastMessage && (
        <div className="fixed bottom-6 right-6 z-50 flex items-center space-x-3 px-4 py-3 rounded-xl bg-slate-900/95 border border-sky-500/50 shadow-2xl shadow-sky-500/20 text-sky-100 animate-in fade-in slide-in-from-bottom-2">
          <CheckCircle2 className="w-5 h-5 text-emerald-400 shrink-0" />
          <span className="text-xs font-semibold">{toastMessage}</span>
        </div>
      )}

      {/* Header Banner */}
      <div className="p-5 rounded-2xl bg-gradient-to-r from-slate-900/95 via-slate-900/80 to-slate-950 border border-slate-800 shadow-xl flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div className="flex items-center space-x-4">
          <div className="w-12 h-12 rounded-xl bg-sky-500/10 border border-sky-500/30 flex items-center justify-center text-sky-400 shadow-inner shrink-0">
            <Zap className="w-6 h-6" />
          </div>
          <div>
            <div className="flex items-center space-x-2.5">
              <h1 className="text-lg font-bold text-white tracking-wide">TOOLS & WARTUNGS-STUDIO</h1>
              <span className="px-2 py-0.5 text-[11px] font-semibold rounded bg-sky-950 text-sky-400 border border-sky-800 font-mono">
                SC LIVE
              </span>
              {status?.cloudStoragePath && (
                <span className="px-2 py-0.5 text-[11px] font-semibold rounded bg-emerald-950/80 text-emerald-400 border border-emerald-800/80 flex items-center space-x-1">
                  <Cloud className="w-3 h-3" />
                  <span>Cloud-Backup aktiv</span>
                </span>
              )}
            </div>
            <p className="text-xs text-slate-400 mt-1 font-mono truncate max-w-xl">
              user.cfg: {status?.userCfgPath || 'StarCitizen\\LIVE\\user.cfg'}
            </p>
          </div>
        </div>

        <div className="flex items-center space-x-2.5">
          <button
            onClick={loadStatus}
            disabled={actionLoading !== null}
            className="flex items-center space-x-1.5 px-3 py-2 rounded-lg bg-slate-800/80 hover:bg-slate-700 text-slate-200 text-xs font-medium border border-slate-700 transition cursor-pointer"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${actionLoading ? 'animate-spin' : ''}`} />
            <span>Aktualisieren</span>
          </button>

          {activeTab === 'tuning' && (
            <button
              onClick={handleSaveUserCfg}
              disabled={actionLoading !== null}
              className="flex items-center space-x-2 px-4 py-2 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold shadow-lg shadow-sky-600/25 border border-sky-400 transition cursor-pointer"
            >
              <Save className="w-4 h-4" />
              <span>Speichern &amp; Anwenden</span>
            </button>
          )}

          {activeTab === 'backups' && (
            <button
              onClick={handleBackupUserCfgSnapshot}
              disabled={actionLoading !== null}
              className="flex items-center space-x-2 px-4 py-2 rounded-lg bg-purple-600 hover:bg-purple-500 text-white text-xs font-bold shadow-lg shadow-purple-600/25 border border-purple-400 transition cursor-pointer"
            >
              <Archive className="w-4 h-4" />
              <span>user.cfg sichern</span>
            </button>
          )}
        </div>
      </div>

      {/* Clean 3-Tab Bar */}
      <div className="flex items-center space-x-2 border-b border-slate-800 pb-3">
        <button
          onClick={() => setActiveTab('maintenance')}
          className={`flex items-center space-x-2 px-4 py-2 rounded-xl text-xs font-semibold transition cursor-pointer ${
            activeTab === 'maintenance'
              ? 'bg-amber-500/15 text-amber-300 border border-amber-500/40 shadow-sm'
              : 'bg-slate-900/50 hover:bg-slate-800/70 text-slate-400 hover:text-slate-200 border border-transparent'
          }`}
        >
          <Wrench className="w-4 h-4" />
          <span>1. Wartung &amp; Diagnose</span>
        </button>

        <button
          onClick={() => setActiveTab('tuning')}
          className={`flex items-center space-x-2 px-4 py-2 rounded-xl text-xs font-semibold transition cursor-pointer ${
            activeTab === 'tuning'
              ? 'bg-sky-500/15 text-sky-300 border border-sky-500/40 shadow-sm'
              : 'bg-slate-900/50 hover:bg-slate-800/70 text-slate-400 hover:text-slate-200 border border-transparent'
          }`}
        >
          <Sliders className="w-4 h-4" />
          <span>2. user.cfg Tuning &amp; Live-Editor</span>
        </button>

        <button
          onClick={() => setActiveTab('backups')}
          className={`flex items-center space-x-2 px-4 py-2 rounded-xl text-xs font-semibold transition cursor-pointer ${
            activeTab === 'backups'
              ? 'bg-purple-500/15 text-purple-300 border border-purple-500/40 shadow-sm'
              : 'bg-slate-900/50 hover:bg-slate-800/70 text-slate-400 hover:text-slate-200 border border-transparent'
          }`}
        >
          <Archive className="w-4 h-4" />
          <span>3. Backup-Tresor (user.cfg &amp; Keybinds)</span>
          {status?.configBackups && status.configBackups.length > 0 && (
            <span className="px-1.5 py-0.2 rounded-full bg-purple-950 text-purple-300 text-[10px] font-mono border border-purple-800">
              {status.configBackups.length}
            </span>
          )}
        </button>
      </div>

      {/* ══════════════════════════════════════════════════════════════
          TAB 1: WARTUNG & DIAGNOSE
          ══════════════════════════════════════════════════════════════ */}
      {activeTab === 'maintenance' && (
        <div className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
            {/* Shader Cache Card */}
            <div className="p-5 rounded-2xl bg-slate-900/70 border border-slate-800 hover:border-slate-700 transition flex flex-col justify-between">
              <div>
                <div className="flex items-center justify-between mb-3">
                  <div className="flex items-center space-x-2.5">
                    <HardDrive className="w-5 h-5 text-amber-400" />
                    <h3 className="text-sm font-semibold text-white">DirectX / Vulkan Shader-Cache</h3>
                  </div>
                  <span className="text-xs px-2.5 py-0.5 rounded bg-amber-950/70 text-amber-400 border border-amber-800/70 font-mono font-bold">
                    {status?.shaderCacheMb ? `${status.shaderCacheMb.toFixed(1)} MB` : '0 MB'}
                  </span>
                </div>
                <p className="text-xs text-slate-400 mb-4 leading-relaxed">
                  Löscht vorkompilierte Shader. Behebt Shader-Stottern und Ruckler nach Patches. Die Pipeline baut sich beim nächsten Start sauber neu auf.
                </p>
              </div>
              <button
                onClick={handleClearShaderCache}
                disabled={actionLoading === 'shaders' || (status?.shaderCacheMb || 0) === 0}
                className="w-full flex items-center justify-center space-x-2 py-2.5 px-3 rounded-xl bg-amber-500/10 hover:bg-amber-500/20 text-amber-300 text-xs font-semibold border border-amber-500/30 transition disabled:opacity-40 cursor-pointer disabled:cursor-not-allowed"
              >
                <Trash2 className="w-4 h-4" />
                <span>{actionLoading === 'shaders' ? 'Bereinige...' : 'Shader-Cache leeren'}</span>
              </button>
            </div>

            {/* Crash Dumps Card */}
            <div className="p-5 rounded-2xl bg-slate-900/70 border border-slate-800 hover:border-slate-700 transition flex flex-col justify-between">
              <div>
                <div className="flex items-center justify-between mb-3">
                  <div className="flex items-center space-x-2.5">
                    <AlertCircle className="w-5 h-5 text-rose-400" />
                    <h3 className="text-sm font-semibold text-white">Crash-Dumps &amp; Logs</h3>
                  </div>
                  <span className="text-xs px-2.5 py-0.5 rounded bg-rose-950/70 text-rose-400 border border-rose-800/70 font-mono font-bold">
                    {status?.crashDumpsMb ? `${status.crashDumpsMb.toFixed(1)} MB` : '0 MB'}
                  </span>
                </div>
                <p className="text-xs text-slate-400 mb-4 leading-relaxed">
                  Gibt Festplattenspeicher frei, indem alte Star Citizen Minidumps (.dmp) und Absturzberichte im AppData-Verzeichnis bereinigt werden.
                </p>
              </div>
              <button
                onClick={handleClearCrashDumps}
                disabled={actionLoading === 'dumps' || (status?.crashDumpsMb || 0) === 0}
                className="w-full flex items-center justify-center space-x-2 py-2.5 px-3 rounded-xl bg-rose-500/10 hover:bg-rose-500/20 text-rose-300 text-xs font-semibold border border-rose-500/30 transition disabled:opacity-40 cursor-pointer disabled:cursor-not-allowed"
              >
                <Trash2 className="w-4 h-4" />
                <span>{actionLoading === 'dumps' ? 'Lösche...' : 'Crash-Dumps bereinigen'}</span>
              </button>
            </div>

            {/* Hardware Diagnostics Card */}
            <div className="p-5 rounded-2xl bg-slate-900/70 border border-slate-800 hover:border-slate-700 transition flex flex-col justify-between">
              <div>
                <div className="flex items-center justify-between mb-3">
                  <div className="flex items-center space-x-2.5">
                    <Cpu className="w-5 h-5 text-emerald-400" />
                    <h3 className="text-sm font-semibold text-white">System &amp; Hardware-Status</h3>
                  </div>
                  <span className="text-xs px-2.5 py-0.5 rounded bg-emerald-950/70 text-emerald-400 border border-emerald-800/70 font-mono font-bold">
                    Aktiv
                  </span>
                </div>
                <div className="space-y-2 text-xs text-slate-300 mb-3 font-mono">
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
                    <span>{status?.pagefileStatus || 'NVMe SSD'}</span>
                  </div>
                </div>
              </div>
              <div className="pt-2.5 border-t border-slate-800 flex items-center space-x-2 text-[11px] text-slate-400">
                <ShieldCheck className="w-4 h-4 text-emerald-400 shrink-0" />
                <span>Systemvoraussetzungen für Star Citizen 4.x optimal</span>
              </div>
            </div>
          </div>

          {/* Quick Performance Recommendations */}
          <div className="p-5 rounded-2xl bg-slate-900/50 border border-slate-800 space-y-3">
            <div className="flex items-center space-x-2 text-sky-400 font-semibold text-sm">
              <Sparkles className="w-4 h-4" />
              <span>EMPFEHLUNGEN FÜR MAXIMALE STABILITÄT &amp; FPS</span>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-xs text-slate-400 leading-relaxed">
              <div className="p-3 rounded-xl bg-slate-950/60 border border-slate-800/80">
                <span className="font-bold text-slate-200 block mb-1">1. Nach jedem Patch Shaders leeren</span>
                CIG aktualisiert regelmäßig Shader-Pipelines. Ein Bereinigen vor dem ersten Spielstart verhindert Mikroruckler in Städten.
              </div>
              <div className="p-3 rounded-xl bg-slate-950/60 border border-slate-800/80">
                <span className="font-bold text-slate-200 block mb-1">2. Feste Auslagerungsdatei auf NVMe</span>
                Star Citizen benötigt inklusive VRAM-Spitzen bis zu 32 GB virtuellen Speicher. Lege die Pagefile immer auf deine schnellste NVMe-SSD.
              </div>
              <div className="p-3 rounded-xl bg-slate-950/60 border border-slate-800/80">
                <span className="font-bold text-slate-200 block mb-1">3. Automatische Cloud-Sicherungen</span>
                Alle user.cfg-Snapshots und Joystick-Belegungen werden im Backup-Tresor automatisch in OneDrive gesichert.
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ══════════════════════════════════════════════════════════════
          TAB 2: USER.CFG TUNING & LIVE-EDITOR
          ══════════════════════════════════════════════════════════════ */}
      {activeTab === 'tuning' && (
        <div className="space-y-6">
          {/* Non-destructive Merge Alert & Safety Guarantee */}
          <div className="p-4 rounded-xl bg-gradient-to-r from-emerald-950/40 via-slate-900/60 to-sky-950/40 border border-emerald-600/30 flex items-start space-x-3 text-xs">
            <ShieldCheck className="w-5 h-5 text-emerald-400 shrink-0 mt-0.5" />
            <div className="space-y-1">
              <div className="font-bold text-emerald-300">
                Intelligenter Schutz: Bestehende Einträge werden niemals überschrieben!
              </div>
              <div className="text-slate-300 text-[11px] leading-relaxed">
                Beim Wechseln von Schaltern oder Profilen werden ausschließlich die gezielten Einstellungen aktualisiert.
                Deine individuellen Kommentare (<code>;</code>), benutzerdefiniertes Sichtfeld (<code>cl_fov</code>), Auflösungen oder Grafik-Tweaks bleiben 100% erhalten.
                Zudem wird <strong>vor jeder Änderung automatisch ein Backup lokal und in der Cloud</strong> archiviert.
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
            {/* Left Column: Presets & Quick Controls (5 cols) */}
            <div className="lg:col-span-5 space-y-5">
              {/* Presets Box */}
              <div className="p-5 rounded-2xl bg-slate-900/70 border border-slate-800 space-y-4">
                <div className="flex items-center space-x-2 text-amber-400 font-semibold text-sm">
                  <Sparkles className="w-4 h-4" />
                  <span>SCHNELL-TUNING PROFILE</span>
                </div>

                <div className="space-y-2.5">
                  <button
                    onClick={() => applyPreset('esport')}
                    className="w-full p-3 rounded-xl bg-slate-800/70 hover:bg-slate-800 text-left border border-slate-700/80 hover:border-sky-500/60 transition group flex items-center justify-between cursor-pointer"
                  >
                    <div>
                      <div className="text-xs font-bold text-sky-400 group-hover:text-sky-300">
                        ⚡ High FPS / E-Sport Profil
                      </div>
                      <div className="text-[11px] text-slate-400 mt-0.5">
                        165 FPS Limit, VSync Aus, 8GB StreamPool, DisplayInfo=1
                      </div>
                    </div>
                    <span className="text-[11px] px-2.5 py-1 rounded-lg bg-sky-950 text-sky-400 border border-sky-800 font-semibold">
                      Übernehmen
                    </span>
                  </button>

                  <button
                    onClick={() => applyPreset('quality')}
                    className="w-full p-3 rounded-xl bg-slate-800/70 hover:bg-slate-800 text-left border border-slate-700/80 hover:border-purple-500/60 transition group flex items-center justify-between cursor-pointer"
                  >
                    <div>
                      <div className="text-xs font-bold text-purple-400 group-hover:text-purple-300">
                        🎨 Grafik &amp; Immersion Profil
                      </div>
                      <div className="text-[11px] text-slate-400 mt-0.5">
                        Unbegrenzt FPS, VSync Ein, 12GB StreamPool, DisplayInfo=0
                      </div>
                    </div>
                    <span className="text-[11px] px-2.5 py-1 rounded-lg bg-purple-950 text-purple-400 border border-purple-800 font-semibold">
                      Übernehmen
                    </span>
                  </button>

                  <button
                    onClick={() => applyPreset('minimal')}
                    className="w-full p-3 rounded-xl bg-slate-800/70 hover:bg-slate-800 text-left border border-slate-700/80 hover:border-slate-500/60 transition group flex items-center justify-between cursor-pointer"
                  >
                    <div>
                      <div className="text-xs font-bold text-slate-300 group-hover:text-white">
                        💻 Minimal / Einsteiger-PC
                      </div>
                      <div className="text-[11px] text-slate-400 mt-0.5">
                        60 FPS Cap, 4GB StreamPool, maximale Bildstabilität
                      </div>
                    </div>
                    <span className="text-[11px] px-2.5 py-1 rounded-lg bg-slate-800 text-slate-300 border border-slate-700 font-semibold">
                      Übernehmen
                    </span>
                  </button>
                </div>
              </div>

              {/* Detailed Tuning Controls */}
              <div className="p-5 rounded-2xl bg-slate-900/70 border border-slate-800 space-y-4">
                <div className="flex items-center space-x-2 text-sky-400 font-semibold text-sm">
                  <Terminal className="w-4 h-4" />
                  <span>FEINABSTIMMUNG (LIVE-MERGE)</span>
                </div>

                <div className="space-y-3 text-xs">
                  <label className="flex items-center space-x-3 cursor-pointer p-2.5 rounded-xl hover:bg-slate-800/40 border border-transparent hover:border-slate-800 transition">
                    <input
                      type="checkbox"
                      checked={consoleUnlocked}
                      onChange={(e) => {
                        setConsoleUnlocked(e.target.checked);
                        updateSingleCvar('Con_Restricted', e.target.checked ? 0 : 1);
                      }}
                      className="rounded border-slate-700 text-sky-600 focus:ring-sky-500 bg-slate-800 cursor-pointer"
                    />
                    <div className="flex-1">
                      <span className="text-slate-200 font-semibold">Konsole freigeben</span>
                      <span className="text-[11px] text-slate-400 block font-mono">Con_Restricted = {consoleUnlocked ? 0 : 1}</span>
                    </div>
                  </label>

                  <label className="flex items-center space-x-3 cursor-pointer p-2.5 rounded-xl hover:bg-slate-800/40 border border-transparent hover:border-slate-800 transition">
                    <input
                      type="checkbox"
                      checked={vsyncOff}
                      onChange={(e) => {
                        setVsyncOff(e.target.checked);
                        updateSingleCvar('r_VSync', e.target.checked ? 0 : 1);
                      }}
                      className="rounded border-slate-700 text-sky-600 focus:ring-sky-500 bg-slate-800 cursor-pointer"
                    />
                    <div className="flex-1">
                      <span className="text-slate-200 font-semibold">VSync deaktivieren</span>
                      <span className="text-[11px] text-slate-400 block font-mono">r_VSync = {vsyncOff ? 0 : 1}</span>
                    </div>
                  </label>

                  <label className="flex items-center space-x-3 cursor-pointer p-2.5 rounded-xl hover:bg-slate-800/40 border border-transparent hover:border-slate-800 transition">
                    <input
                      type="checkbox"
                      checked={motionBlurOff}
                      onChange={(e) => {
                        setMotionBlurOff(e.target.checked);
                        updateSingleCvar('r_MotionBlur', e.target.checked ? 0 : 1);
                      }}
                      className="rounded border-slate-700 text-sky-600 focus:ring-sky-500 bg-slate-800 cursor-pointer"
                    />
                    <div className="flex-1">
                      <span className="text-slate-200 font-semibold">Bewegungsunschärfe aus</span>
                      <span className="text-[11px] text-slate-400 block font-mono">r_MotionBlur = {motionBlurOff ? 0 : 1}</span>
                    </div>
                  </label>

                  <div className="pt-3 border-t border-slate-800 space-y-3">
                    <div className="flex items-center justify-between">
                      <div>
                        <span className="text-slate-200 font-semibold block">DisplayInfo Telemetrie:</span>
                        <span className="text-[11px] text-slate-400 font-mono">r_DisplayInfo</span>
                      </div>
                      <select
                        value={displayInfo}
                        onChange={(e) => {
                          const val = parseInt(e.target.value, 10);
                          setDisplayInfo(val);
                          updateSingleCvar('r_DisplayInfo', val);
                        }}
                        className="bg-slate-800 border border-slate-700 rounded-lg px-2.5 py-1 text-slate-200 text-xs focus:outline-none focus:border-sky-500 cursor-pointer"
                      >
                        <option value={0}>0 (Aus)</option>
                        <option value={1}>1 (FPS / Server Tick)</option>
                        <option value={2}>2 (Erweitert / Render)</option>
                        <option value={3}>3 (Komplette Debug-Info)</option>
                      </select>
                    </div>

                    <div className="flex items-center justify-between">
                      <div>
                        <span className="text-slate-200 font-semibold block">Max FPS Begrenzung:</span>
                        <span className="text-[11px] text-slate-400 font-mono">sys_maxfps</span>
                      </div>
                      <div className="flex items-center space-x-2">
                        <input
                          type="number"
                          min={0}
                          max={360}
                          step={5}
                          value={maxFps}
                          onChange={(e) => {
                            const val = parseInt(e.target.value, 10) || 0;
                            setMaxFps(val);
                            updateSingleCvar('sys_maxfps', val);
                          }}
                          className="w-20 bg-slate-800 border border-slate-700 rounded-lg px-2 py-1 text-slate-200 text-xs font-mono text-right focus:outline-none focus:border-sky-500"
                        />
                        <span className="text-[11px] text-slate-400">FPS</span>
                      </div>
                    </div>

                    <div className="flex items-center justify-between">
                      <div>
                        <span className="text-slate-200 font-semibold block">Textur-StreamPool:</span>
                        <span className="text-[11px] text-slate-400 font-mono">r_TexturesStreamPoolSize</span>
                      </div>
                      <select
                        value={streamPool}
                        onChange={(e) => {
                          const val = parseInt(e.target.value, 10);
                          setStreamPool(val);
                          updateSingleCvar('r_TexturesStreamPoolSize', val);
                        }}
                        className="bg-slate-800 border border-slate-700 rounded-lg px-2.5 py-1 text-slate-200 text-xs focus:outline-none focus:border-sky-500 cursor-pointer"
                      >
                        <option value={4096}>4096 MB (6-8 GB VRAM)</option>
                        <option value={6144}>6144 MB (10-12 GB VRAM)</option>
                        <option value={8192}>8192 MB (16 GB VRAM)</option>
                        <option value={12288}>12288 MB (24 GB / RTX 4090)</option>
                      </select>
                    </div>

                    <div className="flex items-center justify-between">
                      <div>
                        <span className="text-slate-200 font-semibold block">Spiel-Sprache:</span>
                        <span className="text-[11px] text-slate-400 font-mono">g_language</span>
                      </div>
                      <select
                        value={langEnglish ? 'english' : 'german'}
                        onChange={(e) => {
                          const isEn = e.target.value === 'english';
                          setLangEnglish(isEn);
                          updateSingleCvar('g_language', isEn ? 'english' : 'german');
                        }}
                        className="bg-slate-800 border border-slate-700 rounded-lg px-2.5 py-1 text-slate-200 text-xs focus:outline-none focus:border-sky-500 cursor-pointer"
                      >
                        <option value="english">Englisch (Original CIG)</option>
                        <option value="german">Deutsch</option>
                      </select>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Right Column: user.cfg Live Code Editor (7 cols) */}
            <div className="lg:col-span-7 flex flex-col space-y-4">
              <div className="flex-1 p-5 rounded-2xl bg-slate-900/90 border border-slate-800 flex flex-col shadow-2xl">
                {/* Editor Header Bar */}
                <div className="flex items-center justify-between pb-3 border-b border-slate-800 mb-3">
                  <div className="flex items-center space-x-2">
                    <FileText className="w-4 h-4 text-sky-400" />
                    <span className="text-sm font-bold text-white">LIVE user.cfg Editor</span>
                    <span className="text-xs text-slate-500 font-mono">
                      ({cfgContent.split('\n').length} Zeilen)
                    </span>
                  </div>

                  <div className="flex items-center space-x-2">
                    <button
                      onClick={copyToClipboard}
                      className="flex items-center space-x-1 px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs transition cursor-pointer"
                      title="In Zwischenablage kopieren"
                    >
                      {copied ? (
                        <>
                          <Check className="w-3.5 h-3.5 text-emerald-400" />
                          <span className="text-emerald-400">Kopiert</span>
                        </>
                      ) : (
                        <>
                          <Copy className="w-3.5 h-3.5" />
                          <span>Kopieren</span>
                        </>
                      )}
                    </button>
                    <button
                      onClick={handleSaveUserCfg}
                      disabled={actionLoading === 'saveCfg'}
                      className="flex items-center space-x-1.5 px-4 py-1.5 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold transition shadow-md shadow-sky-600/20 cursor-pointer"
                    >
                      <Save className="w-3.5 h-3.5" />
                      <span>Speichern</span>
                    </button>
                  </div>
                </div>

                {/* Editor Textarea with Syntax/Line feeling */}
                <div className="flex-1 relative rounded-xl bg-slate-950 border border-slate-800 font-mono text-xs overflow-hidden flex">
                  <textarea
                    value={cfgContent}
                    onChange={(e) => setCfgContent(e.target.value)}
                    spellCheck={false}
                    className="w-full h-full min-h-[460px] p-4 bg-transparent text-sky-100 resize-none focus:outline-none font-mono text-xs leading-relaxed selection:bg-sky-800/50"
                    placeholder="; Star Citizen user.cfg Konfiguration&#10;r_VSync = 0&#10;sys_maxfps = 120&#10;..."
                  />
                </div>

                {/* Editor Footer Notice */}
                <div className="pt-3 mt-3 border-t border-slate-800 flex items-center justify-between text-[11px] text-slate-400 font-mono">
                  <div className="flex items-center space-x-1.5">
                    <FolderOpen className="w-3.5 h-3.5 text-slate-500" />
                    <span className="text-slate-400">Direkt verknüpft mit Star Citizen LIVE Stammordner</span>
                  </div>
                  <span className="text-emerald-400 font-semibold flex items-center space-x-1">
                    <CheckCircle className="w-3 h-3" />
                    <span>Auto-Backup aktiv (Lokal &amp; Cloud)</span>
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ══════════════════════════════════════════════════════════════
          TAB 3: BACKUP-TRESOR (USER.CFG & KEYBINDS & CLOUD)
          ══════════════════════════════════════════════════════════════ */}
      {activeTab === 'backups' && (
        <div className="space-y-6">
          {/* Cloud Storage & Status Bar */}
          <div className="p-5 rounded-2xl bg-gradient-to-r from-purple-950/40 via-slate-900/80 to-sky-950/40 border border-purple-800/30 backdrop-blur shadow-xl space-y-4">
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
              <div className="flex items-center space-x-3.5">
                <div className="w-11 h-11 rounded-xl bg-purple-500/10 border border-purple-500/30 flex items-center justify-center text-purple-400 shadow-inner shrink-0">
                  <Cloud className="w-6 h-6" />
                </div>
                <div>
                  <div className="flex items-center space-x-2">
                    <h2 className="text-sm font-bold text-white tracking-wide">CLOUD-SPEICHER &amp; REPLIKATION</h2>
                    <span className="text-[10px] px-2 py-0.5 rounded-full bg-purple-950 text-purple-300 border border-purple-800 font-mono font-semibold">
                      {cloudDisplay}
                    </span>
                  </div>
                  <p className="text-xs text-slate-400 mt-0.5">
                    user.cfg-Snapshots und Joystick-Belegungen werden bei jeder Sicherung lokal und im Cloud-Ordner archiviert.
                  </p>
                </div>
              </div>

              <div className="flex flex-wrap items-center gap-2">
                <button
                  onClick={handleExportLogsZip}
                  disabled={actionLoading !== null}
                  className="flex items-center space-x-1.5 px-3 py-2 rounded-xl bg-slate-800/90 hover:bg-slate-700 text-slate-200 text-xs font-semibold border border-slate-700 transition cursor-pointer"
                >
                  <Download className="w-3.5 h-3.5 text-slate-400" />
                  <span>Logs als ZIP</span>
                </button>
                <button
                  onClick={handleSyncLogsCloud}
                  disabled={actionLoading !== null}
                  className="flex items-center space-x-1.5 px-3.5 py-2 rounded-xl bg-sky-950/70 hover:bg-sky-900/80 text-sky-300 text-xs font-bold border border-sky-800/80 transition cursor-pointer"
                >
                  <Cloud className="w-3.5 h-3.5 text-sky-400" />
                  <span>Logs in Cloud sichern</span>
                </button>
                <button
                  onClick={() => handleOpenFolder('cloud')}
                  title="Cloud-Ordner im Explorer öffnen"
                  className="p-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700 transition cursor-pointer"
                >
                  <FolderOpen className="w-4 h-4" />
                </button>
              </div>
            </div>

            {/* Cloud Path Config Row */}
            <div className="flex items-center space-x-2 pt-2 border-t border-slate-800/80">
              <input
                type="text"
                placeholder="Cloud-Pfad (z. B. C:\Users\...\OneDrive\StarCitizen)..."
                value={cloudPath}
                onChange={(e) => setCloudPath(e.target.value)}
                className="flex-1 bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2 text-xs font-mono text-slate-200 placeholder-slate-600 focus:outline-none focus:border-purple-500"
              />
              <button
                onClick={handleSaveCloudPath}
                disabled={actionLoading !== null}
                className="px-4 py-2 rounded-xl bg-purple-600 hover:bg-purple-500 text-white text-xs font-bold border border-purple-400 transition cursor-pointer shrink-0"
              >
                Pfad speichern
              </button>
            </div>
          </div>

          {/* 2-Column Vault Grid */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* Left Column: user.cfg Versionsarchiv */}
            <div className="p-5 rounded-2xl bg-slate-900/70 border border-slate-800 flex flex-col justify-between space-y-4">
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2.5">
                    <div className="w-8 h-8 rounded-lg bg-sky-500/10 border border-sky-500/30 flex items-center justify-center text-sky-400">
                      <FileText className="w-4 h-4" />
                    </div>
                    <div>
                      <h3 className="text-xs font-bold text-sky-300 tracking-wider">
                        USER.CFG VERSIONS-ARCHIV
                      </h3>
                      <p className="text-[11px] text-slate-400">
                        Automatische &amp; manuelle Snapshots mit 1-Klick Rollback
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center space-x-2">
                    <span className="text-[11px] px-2.5 py-0.5 rounded-full bg-sky-950 text-sky-400 border border-sky-800 font-mono font-semibold">
                      {status?.configBackups?.length || 0} Snapshots
                    </span>
                    <button
                      onClick={() => handleOpenFolder('config')}
                      title="Config-Backup-Ordner im Windows Explorer öffnen"
                      className="p-1.5 rounded-lg bg-sky-950/50 hover:bg-sky-900/60 text-sky-300 border border-sky-800/60 transition cursor-pointer"
                    >
                      <ExternalLink className="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>

                {/* Manuelles Backup anlegen */}
                <div className="p-3 rounded-xl bg-slate-950/80 border border-slate-800/80 space-y-2">
                  <span className="text-[11px] font-semibold text-slate-300 block">
                    Manuelles user.cfg Backup erstellen:
                  </span>
                  <div className="flex items-center space-x-2">
                    <input
                      type="text"
                      placeholder="Optionale Notiz (z. B. Vor Grafik-Update)..."
                      value={configNote}
                      onChange={(e) => setConfigNote(e.target.value)}
                      className="flex-1 bg-slate-900 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-slate-200 placeholder-slate-600 focus:outline-none focus:border-sky-500 font-mono"
                    />
                    <button
                      onClick={handleBackupUserCfgSnapshot}
                      disabled={actionLoading !== null}
                      className="flex items-center space-x-1.5 px-3.5 py-1.5 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold shadow-md shadow-sky-600/20 border border-sky-400 transition cursor-pointer shrink-0"
                    >
                      <Download className="w-3.5 h-3.5" />
                      <span>Sichern</span>
                    </button>
                  </div>
                </div>

                {/* Liste aller user.cfg Backups */}
                <div className="space-y-2">
                  <span className="text-[11px] font-semibold text-slate-400 block">
                    Gespeicherte Stände (Chronologisch):
                  </span>
                  <div className="space-y-2 overflow-y-auto max-h-[320px] pr-1">
                    {status?.configBackups && status.configBackups.length > 0 ? (
                      status.configBackups.map((c, idx) => (
                        <div
                          key={idx}
                          className="p-3 rounded-xl bg-slate-950/80 border border-slate-800/80 hover:border-slate-700 transition flex items-center justify-between gap-3"
                        >
                          <div className="min-w-0">
                            <div className="text-xs font-bold text-slate-200 truncate font-mono">
                              {c.name}
                            </div>
                            <div className="flex items-center space-x-2 text-[10px] text-slate-500 mt-1">
                              <span className="flex items-center space-x-1">
                                <Clock className="w-3 h-3" />
                                <span>{c.createdAt}</span>
                              </span>
                              <span>•</span>
                              <span className="font-mono">{c.sizeFormatted}</span>
                              <span>•</span>
                              <span
                                className={`px-1.5 py-0.2 rounded font-semibold border text-[9px] ${
                                  c.locationType.includes('Cloud')
                                    ? 'bg-sky-950 text-sky-400 border-sky-800'
                                    : 'bg-slate-900 text-slate-400 border-slate-700'
                                }`}
                              >
                                {c.locationType}
                              </span>
                            </div>
                          </div>

                          <button
                            onClick={() => handleRestoreConfigSnapshot(c)}
                            disabled={actionLoading !== null}
                            className="flex items-center space-x-1.5 px-3 py-1.5 rounded-lg bg-sky-950 hover:bg-sky-900/80 text-sky-300 text-xs font-bold border border-sky-800/80 transition cursor-pointer shrink-0"
                            title="Wiederherstellen (Vorher wird automatisch ein Backup angelegt)"
                          >
                            <RotateCcw className={`w-3.5 h-3.5 ${actionLoading === `restoreCfg_${c.name}` ? 'animate-spin' : ''}`} />
                            <span>Rollback</span>
                          </button>
                        </div>
                      ))
                    ) : (
                      <div className="p-6 rounded-xl bg-slate-950 border border-slate-800/60 text-center text-xs text-slate-500 italic">
                        Noch keine user.cfg-Snapshots vorhanden. Vor der ersten Änderung wird automatisch ein Stand angelegt.
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </div>

            {/* Right Column: Keybind-Tresor (actionmaps.xml) */}
            <div className="p-5 rounded-2xl bg-slate-900/70 border border-slate-800 flex flex-col justify-between space-y-4">
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2.5">
                    <div className="w-8 h-8 rounded-lg bg-purple-500/10 border border-purple-500/30 flex items-center justify-center text-purple-400">
                      <Key className="w-4 h-4" />
                    </div>
                    <div>
                      <h3 className="text-xs font-bold text-purple-300 tracking-wider">
                        KEYBIND-TRESOR (ACTIONMAPS.XML)
                      </h3>
                      <p className="text-[11px] text-slate-400">
                        Sichert HOSAS / Joystick / Mappings vor Patches
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center space-x-2">
                    <span className="text-[11px] px-2.5 py-0.5 rounded-full bg-purple-950 text-purple-300 border border-purple-800 font-mono font-semibold">
                      {status?.keybindItems?.length || status?.keybindBackups?.length || 0} Profile
                    </span>
                    <button
                      onClick={() => handleOpenFolder('keybinds')}
                      title="Keybinds-Ordner im Windows Explorer öffnen"
                      className="p-1.5 rounded-lg bg-purple-950/50 hover:bg-purple-900/60 text-purple-300 border border-purple-800/60 transition cursor-pointer"
                    >
                      <ExternalLink className="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>

                {/* Neues Keybind Backup anlegen */}
                <div className="p-3 rounded-xl bg-slate-950/80 border border-slate-800/80 space-y-2">
                  <span className="text-[11px] font-semibold text-slate-300 block">
                    Neues Steuerungs-Backup anlegen:
                  </span>
                  <div className="flex items-center space-x-2">
                    <input
                      type="text"
                      placeholder="Optionale Notiz (z. B. VKB HOSAS 4.0)..."
                      value={keybindNote}
                      onChange={(e) => setKeybindNote(e.target.value)}
                      className="flex-1 bg-slate-900 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-slate-200 placeholder-slate-600 focus:outline-none focus:border-purple-500 font-mono"
                    />
                    <button
                      onClick={handleCreateKeybindBackup}
                      disabled={actionLoading !== null}
                      className="flex items-center space-x-1.5 px-3.5 py-1.5 rounded-lg bg-purple-600 hover:bg-purple-500 text-white text-xs font-bold shadow-md shadow-purple-600/20 border border-purple-400 transition cursor-pointer shrink-0"
                    >
                      <Archive className="w-3.5 h-3.5" />
                      <span>Sichern</span>
                    </button>
                  </div>
                </div>

                {/* Liste aller Keybind Backups */}
                <div className="space-y-2">
                  <span className="text-[11px] font-semibold text-slate-400 block">
                    Gespeicherte Steuerungs-Profile:
                  </span>
                  <div className="space-y-2 overflow-y-auto max-h-[320px] pr-1">
                    {status?.keybindItems && status.keybindItems.length > 0 ? (
                      status.keybindItems.map((k, idx) => (
                        <div
                          key={idx}
                          className="p-3 rounded-xl bg-slate-950/80 border border-slate-800/80 hover:border-slate-700 transition flex items-center justify-between gap-3"
                        >
                          <div className="min-w-0">
                            <div className="text-xs font-bold text-slate-200 truncate font-mono">
                              {k.name}
                            </div>
                            <div className="flex items-center space-x-2 text-[10px] text-slate-500 mt-1">
                              <span className="flex items-center space-x-1">
                                <Clock className="w-3 h-3" />
                                <span>{k.createdAt}</span>
                              </span>
                              <span>•</span>
                              <span>{k.fileCount} Dateien ({k.sizeFormatted})</span>
                              <span>•</span>
                              <span
                                className={`px-1.5 py-0.2 rounded font-semibold border text-[9px] ${
                                  k.locationType.includes('Cloud')
                                    ? 'bg-purple-950 text-purple-300 border-purple-800'
                                    : 'bg-slate-900 text-slate-400 border-slate-700'
                                }`}
                              >
                                {k.locationType}
                              </span>
                            </div>
                          </div>

                          <button
                            onClick={() => handleRestoreKeybind(k)}
                            disabled={actionLoading !== null}
                            className="flex items-center space-x-1.5 px-3 py-1.5 rounded-lg bg-purple-950 hover:bg-purple-900/80 text-purple-300 text-xs font-bold border border-purple-800/80 transition cursor-pointer shrink-0"
                            title="Steuerung in Star Citizen wiederherstellen"
                          >
                            <RotateCcw className={`w-3.5 h-3.5 ${actionLoading === `restoreKeybind_${k.name}` ? 'animate-spin' : ''}`} />
                            <span>Rollback</span>
                          </button>
                        </div>
                      ))
                    ) : (
                      <div className="p-6 rounded-xl bg-slate-950 border border-slate-800/60 text-center text-xs text-slate-500 italic">
                        Noch keine Keybind-Backups vorhanden. Klicke auf "Sichern", um deine Belegungen vor Patches zu sichern.
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
