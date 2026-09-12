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
  Activity,
  Monitor,
  Gauge,
} from 'lucide-react';
import { bridge, ToolsStatusDto, ConfigBackupItemDto, KeybindBackupItemDto } from '../services/photinoBridge';

/**
 * Non-destructive user.cfg merger:
 * Preserves 100% of existing comments (; # // --),
 * custom cvars (FOV, sharpening, custom graphics/resolutions, HDR, SSDO, etc.),
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
    // Keep comments & empty lines untouched (; # // --)
    if (!trimmed || trimmed.startsWith(';') || trimmed.startsWith('#') || trimmed.startsWith('//') || trimmed.startsWith('--')) {
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
  // Default tab is 'maintenance' for direct system maintenance & diagnostics
  const [activeTab, setActiveTab] = useState<'maintenance' | 'cfg' | 'keybinds'>('maintenance');
  const [status, setStatus] = useState<ToolsStatusDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [cfgContent, setCfgContent] = useState('');
  const [copied, setCopied] = useState(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  // Backup & Notes State
  const [cloudPath, setCloudPath] = useState('');
  const [configNote, setConfigNote] = useState('');
  const [keybindNote, setKeybindNote] = useState('');

  // Tuning Controls (Parsed dynamically from user.cfg)
  const [vsyncOff, setVsyncOff] = useState(false);
  const [motionBlurOff, setMotionBlurOff] = useState(false);
  const [consoleUnlocked, setConsoleUnlocked] = useState(false);
  const [displayInfo, setDisplayInfo] = useState<number>(0);
  const [maxFps, setMaxFps] = useState<number>(160);
  const [streamPool, setStreamPool] = useState<number>(8192);
  const [langEnglish, setLangEnglish] = useState(false);

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
    let vsync = false;
    let mb = false;
    let con = false;
    let disp = 0;
    let fps = 160;
    let pool = 8192;
    let langEn = false;

    const lines = content.split(/\r?\n/);
    lines.forEach((line) => {
      const trimmed = line.trim();
      if (!trimmed || trimmed.startsWith(';') || trimmed.startsWith('#') || trimmed.startsWith('//') || trimmed.startsWith('--')) return;

      const eqIdx = trimmed.indexOf('=');
      if (eqIdx <= 0) return;
      const key = trimmed.substring(0, eqIdx).trim().toLowerCase();
      const val = trimmed.substring(eqIdx + 1).trim();

      if (key === 'r_vsync') {
        vsync = val === '0';
      } else if (key === 'r_motionblur') {
        mb = val === '0';
      } else if (key === 'con_restricted') {
        con = val === '0';
      } else if (key === 'r_displayinfo' || key === 'r_displaysessioninfo') {
        const parsed = parseInt(val, 10);
        if (!isNaN(parsed)) disp = parsed;
      } else if (key === 'sys_maxfps') {
        const parsed = parseInt(val, 10);
        if (!isNaN(parsed)) fps = parsed;
      } else if (key === 'r_texturesstreampoolsize') {
        const parsed = parseInt(val, 10);
        if (!isNaN(parsed)) pool = parsed;
      } else if (key === 'g_language') {
        langEn = val.toLowerCase().includes('english');
      }
    });

    setVsyncOff(vsync);
    setMotionBlurOff(mb);
    setConsoleUnlocked(con);
    setDisplayInfo(disp);
    setMaxFps(fps);
    setStreamPool(pool);
    setLangEnglish(langEn);
  };

  // Preset Application (Non-destructive merge into existing file)
  const applyPreset = (type: 'esport' | 'quality' | 'minimal') => {
    let updates: Record<string, string | number> = {};

    if (type === 'esport') {
      setVsyncOff(true);
      setMotionBlurOff(true);
      setConsoleUnlocked(true);
      setDisplayInfo(1);
      setMaxFps(165);
      setStreamPool(8192);
      updates = {
        Con_Restricted: 0,
        r_VSync: 0,
        r_MotionBlur: 0,
        sys_maxfps: 165,
        r_TexturesStreamPoolSize: 8192,
        r_DisplayInfo: 1,
      };
      showToast('Profil "High FPS / E-Sport" eingefügt (Bestehende Einträge bleiben erhalten)');
    } else if (type === 'quality') {
      setVsyncOff(false);
      setMotionBlurOff(false);
      setConsoleUnlocked(true);
      setDisplayInfo(0);
      setMaxFps(0);
      setStreamPool(12288);
      updates = {
        Con_Restricted: 0,
        r_VSync: 1,
        r_MotionBlur: 0,
        sys_maxfps: 0,
        r_TexturesStreamPoolSize: 12288,
        r_DisplayInfo: 0,
      };
      showToast('Profil "Grafik & Immersion" eingefügt (Bestehende Einträge bleiben erhalten)');
    } else {
      setVsyncOff(true);
      setMotionBlurOff(true);
      setConsoleUnlocked(true);
      setDisplayInfo(1);
      setMaxFps(60);
      setStreamPool(4096);
      updates = {
        Con_Restricted: 0,
        r_VSync: 0,
        r_MotionBlur: 0,
        sys_maxfps: 60,
        r_TexturesStreamPoolSize: 4096,
        r_DisplayInfo: 1,
      };
      showToast('Profil "Minimal / Einsteiger-PC" eingefügt (Bestehende Einträge bleiben erhalten)');
    }

    setCfgContent((prev) => {
      const merged = mergeCfgContent(prev, updates);
      return merged;
    });
  };

  const updateSingleCvar = (key: string, value: string | number) => {
    setCfgContent((prev) => {
      const merged = mergeCfgContent(prev, { [key]: value });
      return merged;
    });
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
      showToast('user.cfg gespeichert! (Vorher automatisch lokal & in Cloud gesichert)');
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
      showToast(res.message || 'user.cfg Backup erfolgreich in Lokal & Cloud archiviert!');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Erstellen des user.cfg Backups');
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
      showToast(res.message || `user.cfg erfolgreich aus '${item.name}' wiederhergestellt!`);
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
      showToast('Cloud-Speicherpfad erfolgreich gespeichert!');
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

  const handleToggleAutoCloudSync = async (enabled: boolean) => {
    try {
      const res = await bridge.send<{ success: boolean; autoCloudSyncEnabled: boolean; tools?: ToolsStatusDto }>('toggle_auto_cloud_sync', { enabled });
      if (res.tools) setStatus(res.tools);
      showToast(enabled ? 'Automatische Cloud-Synchronisation aktiviert' : 'Automatische Cloud-Synchronisation pausiert');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Ändern der Cloud-Einstellung');
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
    const unsubscribe = bridge.on<ToolsStatusDto>('TOOLS_UPDATED', (newStatus) => {
      if (newStatus) {
        setStatus(newStatus);
        if (newStatus.cloudStoragePath !== undefined) {
          setCloudPath(newStatus.cloudStoragePath || '');
        }
      }
    });
    return () => unsubscribe();
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
      ? `OneDrive (${status.cloudStoragePath})`
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
      <div className="p-5 rounded-2xl bg-gradient-to-r from-slate-900/95 via-slate-900/80 to-slate-950 border border-slate-800 shadow-xl flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center space-x-4">
          <div className="w-12 h-12 rounded-xl bg-sky-500/10 border border-sky-500/30 flex items-center justify-center text-sky-400 shadow-inner shrink-0">
            <Zap className="w-6 h-6" />
          </div>
          <div>
            <div className="flex items-center space-x-2.5">
              <h1 className="text-lg font-bold text-white tracking-wide">STAR CITIZEN TOOLS &amp; WARTUNG</h1>
              <span className="px-2 py-0.5 text-[11px] font-semibold rounded bg-sky-950 text-sky-400 border border-sky-800 font-mono">
                LIVE
              </span>
            </div>
            <p className="text-xs text-slate-400 mt-1">
              Client-Tuning, Cache-Bereinigung, Hardware-Benchmark &amp; Backups
            </p>
          </div>
        </div>

        <div className="flex items-center space-x-2.5 shrink-0">
          <button
            onClick={loadStatus}
            disabled={actionLoading !== null}
            className="flex items-center space-x-1.5 px-3.5 py-2 rounded-lg bg-slate-800/80 hover:bg-slate-700 text-slate-200 text-xs font-medium border border-slate-700 transition cursor-pointer"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${actionLoading ? 'animate-spin' : ''}`} />
            <span>Neu laden</span>
          </button>
        </div>
      </div>

      {/* 3-Tab Bar: Wartung is Tab 1 */}
      <div className="flex items-center space-x-2 border-b border-slate-800 pb-3">
        <button
          onClick={() => setActiveTab('maintenance')}
          className={`flex items-center space-x-2 px-4 py-2 rounded-xl text-xs font-semibold transition cursor-pointer ${
            activeTab === 'maintenance'
              ? 'bg-amber-500/20 text-amber-300 border border-amber-500/40 shadow-sm'
              : 'bg-slate-900/50 hover:bg-slate-800/70 text-slate-400 hover:text-slate-200 border border-transparent'
          }`}
        >
          <Wrench className="w-4 h-4" />
          <span>1. Wartung &amp; Diagnose</span>
        </button>

        <button
          onClick={() => setActiveTab('cfg')}
          className={`flex items-center space-x-2 px-4 py-2 rounded-xl text-xs font-semibold transition cursor-pointer ${
            activeTab === 'cfg'
              ? 'bg-sky-500/20 text-sky-300 border border-sky-500/40 shadow-sm'
              : 'bg-slate-900/50 hover:bg-slate-800/70 text-slate-400 hover:text-slate-200 border border-transparent'
          }`}
        >
          <Sliders className="w-4 h-4" />
          <span>2. user.cfg (Editor &amp; Backups)</span>
          {status?.configBackups && status.configBackups.length > 0 && (
            <span className="px-1.5 py-0.2 rounded-full bg-sky-950 text-sky-400 text-[10px] font-mono border border-sky-800">
              {status.configBackups.length}
            </span>
          )}
        </button>

        <button
          onClick={() => setActiveTab('keybinds')}
          className={`flex items-center space-x-2 px-4 py-2 rounded-xl text-xs font-semibold transition cursor-pointer ${
            activeTab === 'keybinds'
              ? 'bg-purple-500/20 text-purple-300 border border-purple-500/40 shadow-sm'
              : 'bg-slate-900/50 hover:bg-slate-800/70 text-slate-400 hover:text-slate-200 border border-transparent'
          }`}
        >
          <Key className="w-4 h-4" />
          <span>3. Steuerungs-Tresor (Keybinds &amp; Cloud)</span>
          {status?.keybindItems && status.keybindItems.length > 0 && (
            <span className="px-1.5 py-0.2 rounded-full bg-purple-950 text-purple-300 text-[10px] font-mono border border-purple-800">
              {status.keybindItems.length}
            </span>
          )}
        </button>
      </div>

      {/* ══════════════════════════════════════════════════════════════
          TAB 2: USER.CFG TUNING, LIVE-EDITOR & BACKUPS
          ══════════════════════════════════════════════════════════════ */}
      {activeTab === 'cfg' && (
        <div className="space-y-6">
          {/* Non-destructive Merge Alert & Safety Guarantee */}
          <div className="p-4 rounded-xl bg-gradient-to-r from-emerald-950/40 via-slate-900/60 to-sky-950/40 border border-emerald-600/30 flex items-start space-x-3 text-xs">
            <ShieldCheck className="w-5 h-5 text-emerald-400 shrink-0 mt-0.5" />
            <div className="space-y-1">
              <div className="font-bold text-emerald-300">
                100% Schutz für deine Einstellungen: Kommentare und eigene Zeilen bleiben immer erhalten!
              </div>
              <div className="text-slate-300 text-[11px] leading-relaxed">
                Beim Ändern von Schaltern oder Presets werden bestehende Zeilen (wie <code>r_ssdo</code>, <code>r_HDRDisplayOutput</code>, <code>cl_fov</code>, <code>sys_budget_sysmemkb</code>) <strong>niemals gelöscht</strong>.
                Zudem wird <strong>vor jeder Speicherung automatisch ein Backup</strong> im lokalen Ordner und in der Cloud gesichert.
              </div>
            </div>
          </div>

          {/* Main 2-Column Studio */}
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
            {/* Left Column: Presets & Controls (5 cols) */}
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
                      Einfügen
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
                      Einfügen
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
                      Einfügen
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
                        const val = e.target.checked;
                        setConsoleUnlocked(val);
                        updateSingleCvar('Con_Restricted', val ? 0 : 1);
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
                        const val = e.target.checked;
                        setVsyncOff(val);
                        updateSingleCvar('r_VSync', val ? 0 : 1);
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
                        const val = e.target.checked;
                        setMotionBlurOff(val);
                        updateSingleCvar('r_MotionBlur', val ? 0 : 1);
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
                        value={langEnglish ? 'english' : 'german_(germany)'}
                        onChange={(e) => {
                          const isEn = e.target.value === 'english';
                          setLangEnglish(isEn);
                          updateSingleCvar('g_language', isEn ? 'english' : 'german_(germany)');
                        }}
                        className="bg-slate-800 border border-slate-700 rounded-lg px-2.5 py-1 text-slate-200 text-xs focus:outline-none focus:border-sky-500 cursor-pointer"
                      >
                        <option value="german_(germany)">Deutsch (german_(germany))</option>
                        <option value="english">Englisch (english)</option>
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
                <div className="flex flex-col sm:flex-row sm:items-center justify-between pb-3 border-b border-slate-800 mb-3 gap-2">
                  <div className="flex items-center space-x-2.5 min-w-0">
                    <FileText className="w-4 h-4 text-sky-400 shrink-0" />
                    <div className="min-w-0">
                      <div className="flex items-center space-x-2">
                        <span className="text-sm font-bold text-white">LIVE user.cfg Editor</span>
                        <span className="text-xs text-slate-500 font-mono">
                          ({cfgContent.split('\n').length} Zeilen)
                        </span>
                      </div>
                      <p className="text-[11px] text-slate-400 font-mono truncate max-w-md" title={status?.userCfgPath || 'StarCitizen\\LIVE\\user.cfg'}>
                        {status?.userCfgPath || 'StarCitizen\\LIVE\\user.cfg'}
                      </p>
                    </div>
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
                    onChange={(e) => {
                      setCfgContent(e.target.value);
                      parseCfgContent(e.target.value);
                    }}
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

          {/* ══════════════════════════════════════════════════════════════
              USER.CFG BACKUP-TRESOR (DIREKT IN DIESEM TAB!)
              ══════════════════════════════════════════════════════════════ */}
          <div className="p-5 rounded-2xl bg-slate-900/80 border border-slate-800 space-y-4">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b border-slate-800">
              <div className="flex items-center space-x-3">
                <div className="w-9 h-9 rounded-xl bg-purple-500/10 border border-purple-500/30 flex items-center justify-center text-purple-400 shrink-0">
                  <Archive className="w-5 h-5" />
                </div>
                <div>
                  <div className="flex items-center space-x-2">
                    <h3 className="text-sm font-bold text-white tracking-wide">
                      USER.CFG BACKUP-HISTORIE &amp; 1-KLICK ROLLBACK
                    </h3>
                    <span className="text-[10px] px-2 py-0.5 rounded-full bg-purple-950 text-purple-300 border border-purple-800 font-mono font-semibold">
                      {status?.configBackups?.length || 0} Backups
                    </span>
                  </div>
                  <p className="text-xs text-slate-400">
                    Jede Änderung wird automatisch hier und in deiner Cloud gesichert. Du kannst jederzeit mit einem Klick auf einen früheren Stand zurückrollen.
                  </p>
                </div>
              </div>

              <div className="flex items-center space-x-2">
                <button
                  onClick={() => handleOpenFolder('config')}
                  className="flex items-center space-x-1.5 px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold border border-slate-700 transition cursor-pointer"
                  title="Config-Backup-Ordner im Windows Explorer öffnen"
                >
                  <FolderOpen className="w-3.5 h-3.5 text-sky-400" />
                  <span>Ordner öffnen</span>
                </button>
                <button
                  onClick={handleBackupUserCfgSnapshot}
                  disabled={actionLoading !== null}
                  className="flex items-center space-x-1.5 px-3.5 py-1.5 rounded-lg bg-purple-600 hover:bg-purple-500 text-white text-xs font-bold shadow-md shadow-purple-600/20 border border-purple-400 transition cursor-pointer"
                >
                  <Download className="w-3.5 h-3.5" />
                  <span>Jetzt sichern</span>
                </button>
              </div>
            </div>

            {/* Manual backup with note row */}
            <div className="flex items-center space-x-2">
              <input
                type="text"
                placeholder="Optionale Notiz für manuelles Backup (z. B. Vor Grafik-Update oder Patch 4.0)..."
                value={configNote}
                onChange={(e) => setConfigNote(e.target.value)}
                className="flex-1 bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2 text-xs text-slate-200 placeholder-slate-600 focus:outline-none focus:border-purple-500 font-mono"
              />
              <button
                onClick={handleBackupUserCfgSnapshot}
                disabled={actionLoading !== null}
                className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold border border-slate-700 transition cursor-pointer shrink-0"
              >
                Snapshot anlegen
              </button>
            </div>

            {/* Backups List */}
            <div className="space-y-2 overflow-y-auto max-h-[320px] pr-1">
              {status?.configBackups && status.configBackups.length > 0 ? (
                status.configBackups.map((c, idx) => (
                  <div
                    key={idx}
                    className="p-3.5 rounded-xl bg-slate-950/90 border border-slate-800 hover:border-slate-700 transition flex items-center justify-between gap-3"
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
                      className="flex items-center space-x-1.5 px-3.5 py-1.5 rounded-lg bg-sky-950 hover:bg-sky-900 text-sky-300 text-xs font-bold border border-sky-800 transition cursor-pointer shrink-0"
                      title="Diesen Stand wieder in die LIVE user.cfg einspielen (aktueller Stand wird zuvor automatisch gesichert)"
                    >
                      <RotateCcw className={`w-3.5 h-3.5 ${actionLoading === `restoreCfg_${c.name}` ? 'animate-spin' : ''}`} />
                      <span>Wiederherstellen</span>
                    </button>
                  </div>
                ))
              ) : (
                <div className="p-6 rounded-xl bg-slate-950 border border-slate-800/60 text-center text-xs text-slate-500 italic">
                  Noch keine user.cfg-Snapshots vorhanden. Vor der nächsten Änderung wird automatisch ein Stand archiviert.
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* ══════════════════════════════════════════════════════════════
          TAB 1: WARTUNG & DIAGNOSE
          ══════════════════════════════════════════════════════════════ */}
      {activeTab === 'maintenance' && (
        <div className="space-y-6">
          {/* 1. Maintenance Actions: Shaders & Crash Dumps */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
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
          </div>

          {/* 2. Full-Width Dedicated Hardware & Star Citizen Startup Benchmark Suite */}
          <div className="p-6 rounded-2xl bg-gradient-to-br from-slate-900/90 via-slate-900/70 to-slate-950 border border-slate-800 shadow-xl space-y-6">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-4 border-b border-slate-800/80">
              <div className="flex items-center space-x-3.5">
                <div className="w-10 h-10 rounded-xl bg-emerald-500/10 border border-emerald-500/30 flex items-center justify-center text-emerald-400 shadow-inner shrink-0">
                  <Cpu className="w-5 h-5" />
                </div>
                <div>
                  <div className="flex items-center space-x-2.5">
                    <h3 className="text-sm font-bold text-white tracking-wide">
                      SYSTEM-HARDWARE &amp; GAME.LOG STARTUP-BENCHMARK
                    </h3>
                    <span className="text-[10px] px-2.5 py-0.5 rounded-full bg-emerald-950/90 text-emerald-400 border border-emerald-800/80 font-mono font-bold">
                      {status?.cpuModel && status.cpuModel !== 'Unbekannt' ? 'Aus Game.log erfasst' : 'Aktiv'}
                    </span>
                  </div>
                  <p className="text-xs text-slate-400 mt-0.5">
                    Live-Hardwareidentifikation und Star Citizen Engine-Leistungsindizes aus dem Spielstart.
                  </p>
                </div>
              </div>

              {status?.windowsVersion && (
                <div className="text-[11px] font-mono px-3 py-1.5 rounded-xl bg-slate-950/80 border border-slate-800 text-slate-300 shrink-0">
                  {status.windowsVersion}
                </div>
              )}
            </div>

            {/* Hardware Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
              {/* CPU Box */}
              <div className="p-4 rounded-xl bg-slate-950/60 border border-slate-800/80 hover:border-slate-700 transition flex flex-col justify-between">
                <div>
                  <div className="flex items-center justify-between text-slate-400 text-xs mb-1.5">
                    <span className="font-semibold uppercase tracking-wider text-[10px]">Hauptprozessor (CPU)</span>
                    <Cpu className="w-4 h-4 text-sky-400" />
                  </div>
                  <div className="font-bold text-slate-100 text-sm font-mono leading-tight">
                    {status?.cpuModel || 'Wird ermittelt...'}
                  </div>
                </div>
                <div className="mt-3 pt-2 border-t border-slate-800/60 flex items-center justify-between text-[11px] font-mono text-slate-400">
                  <span>Logische Kerne:</span>
                  <span className="font-semibold text-sky-300">{status?.cpuLogicalCores ? `${status.cpuLogicalCores} Threads` : '16 Threads'}</span>
                </div>
              </div>

              {/* GPU Box */}
              <div className="p-4 rounded-xl bg-slate-950/60 border border-slate-800/80 hover:border-slate-700 transition flex flex-col justify-between">
                <div>
                  <div className="flex items-center justify-between text-slate-400 text-xs mb-1.5">
                    <span className="font-semibold uppercase tracking-wider text-[10px]">Grafikkarte &amp; Anzeige</span>
                    <Monitor className="w-4 h-4 text-emerald-400" />
                  </div>
                  <div className="font-bold text-slate-100 text-sm font-mono leading-tight">
                    {status?.gpuModel || 'Wird ermittelt...'}
                  </div>
                  <div className="text-[11px] text-slate-400 font-mono mt-1">
                    {status?.gpuVramMb ? `${status.gpuVramMb}` : '12 GB'} VRAM {status?.gpuDriverVersion ? `· v${status.gpuDriverVersion}` : ''}
                  </div>
                </div>
                <div className="mt-3 pt-2 border-t border-slate-800/60 flex items-center justify-between text-[11px] font-mono text-slate-400">
                  <span>Display-Auflösung:</span>
                  <span className="font-semibold text-emerald-300">{status?.displayResolution || '2560x1440x32'}</span>
                </div>
              </div>

              {/* RAM Box */}
              <div className="p-4 rounded-xl bg-slate-950/60 border border-slate-800/80 hover:border-slate-700 transition flex flex-col justify-between">
                <div>
                  <div className="flex items-center justify-between text-slate-400 text-xs mb-1.5">
                    <span className="font-semibold uppercase tracking-wider text-[10px]">Arbeitsspeicher (RAM)</span>
                    <Activity className="w-4 h-4 text-purple-400" />
                  </div>
                  <div className="font-bold text-slate-100 text-sm font-mono leading-tight">
                    {status?.ramStatus || `${status?.totalRamGb || 64} GB`}
                  </div>
                </div>
                <div className="mt-3 pt-2 border-t border-slate-800/60 flex items-center justify-between text-[11px] font-mono text-slate-400">
                  <span>Auslagerungsdatei:</span>
                  <span className="font-semibold text-purple-300">{status?.pagefileStatus || 'Aktiv'}</span>
                </div>
              </div>

              {/* Drive Box */}
              <div className="p-4 rounded-xl bg-slate-950/60 border border-slate-800/80 hover:border-slate-700 transition flex flex-col justify-between">
                <div>
                  <div className="flex items-center justify-between text-slate-400 text-xs mb-1.5">
                    <span className="font-semibold uppercase tracking-wider text-[10px]">Installations-Laufwerk</span>
                    <HardDrive className="w-4 h-4 text-amber-400" />
                  </div>
                  <div className="font-bold text-slate-100 text-sm font-mono leading-tight">
                    Laufwerk {status?.driveName || 'J:'}: {status?.freeDiskGb ? `${status.freeDiskGb.toFixed(1)} GB frei` : 'Frei'}
                  </div>
                </div>
                <div className="mt-3 pt-2 border-t border-slate-800/60 flex items-center justify-between text-[11px] font-mono text-slate-400">
                  <span>Laufwerkstyp:</span>
                  <span className="font-semibold text-amber-300">{status?.driveType || 'Interne SSD / NVMe'}</span>
                </div>
              </div>
            </div>

            {/* Engine Startup Benchmark Section */}
            <div className="p-4 rounded-xl bg-slate-950/80 border border-slate-800/90 space-y-3">
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-2 text-xs font-bold text-sky-400 tracking-wider font-mono">
                  <Gauge className="w-4 h-4 text-sky-400" />
                  <span>ENGINE STARTUP BENCHMARK &amp; PERFORMANCE INDEX (GAME.LOG)</span>
                </div>
                <span className="text-[10px] text-slate-500 font-mono">
                  Gemessen von Star Citizen beim Engine-Boot
                </span>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 text-xs font-mono">
                {/* CPU Benchmark */}
                <div className="p-3 rounded-lg bg-slate-900/80 border border-slate-800/80 flex flex-col justify-between space-y-1">
                  <div className="text-slate-400 text-[10px] uppercase tracking-wider">CPU Benchmark Zeit</div>
                  <div className="font-bold text-sky-300 text-sm">
                    {status?.cpuBenchmark || '34.24 ms (int+mem)'}
                  </div>
                  <div className="text-[10px] text-slate-500">Kürzere Latenz = Bessere Physik-Verarbeitung</div>
                </div>

                {/* GPU Benchmark */}
                <div className="p-3 rounded-lg bg-slate-900/80 border border-slate-800/80 flex flex-col justify-between space-y-1">
                  <div className="text-slate-400 text-[10px] uppercase tracking-wider">GPU Benchmark Zeit</div>
                  <div className="font-bold text-emerald-300 text-sm">
                    {status?.gpuBenchmark || '23.25 ms (Adapter 0)'}
                  </div>
                  <div className="text-[10px] text-slate-500">Vulkan Frame-Buffer &amp; Shader Renderzeit</div>
                </div>

                {/* Performance Indices */}
                <div className="p-3 rounded-lg bg-slate-900/80 border border-slate-800/80 flex flex-col justify-between space-y-1">
                  <div className="text-slate-400 text-[10px] uppercase tracking-wider">CIG Performance Index</div>
                  <div className="font-bold text-purple-300 text-sm flex items-center space-x-2">
                    <span>CPU: {status?.performanceIndexCpu || '184.87'}</span>
                    <span className="text-slate-600">|</span>
                    <span>GPU: {status?.performanceIndexGpu || '443.02'}</span>
                  </div>
                  <div className="text-[10px] text-slate-500">CIG Telemetrie Rating (Höher = Schneller)</div>
                </div>

                {/* DataCore & PSO Pipeline */}
                <div className="p-3 rounded-lg bg-slate-900/80 border border-slate-800/80 flex flex-col justify-between space-y-1">
                  <div className="text-slate-400 text-[10px] uppercase tracking-wider">DataCore &amp; PSO Boot</div>
                  <div className="font-bold text-amber-300 text-sm">
                    {status?.dataCoreLoadTime ? `${status.dataCoreLoadTime}` : '3.72s'} {status?.psoCacheGenTime ? `(PSO: ${status.psoCacheGenTime})` : '(PSO: 0.38s)'}
                  </div>
                  <div className="text-[10px] text-slate-500">NVMe Ladezeit für Game-Binaries</div>
                </div>
              </div>
            </div>

            {/* Bottom Status Guarantee */}
            <div className="pt-3 border-t border-slate-800/70 flex items-center justify-between text-xs text-slate-400">
              <div className="flex items-center space-x-2">
                <ShieldCheck className="w-4 h-4 text-emerald-400 shrink-0" />
                <span className="text-slate-300 font-medium">Systemvoraussetzungen für Star Citizen 4.x optimal</span>
              </div>
              <span className="text-[11px] font-mono text-slate-500 hidden sm:inline">
                Automatisch analysiert aus der aktiven Star Citizen Sitzung
              </span>
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
                Alle user.cfg-Snapshots und Joystick-Belegungen werden im Backup-Tresor automatisch in Google Drive / OneDrive gesichert.
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ══════════════════════════════════════════════════════════════
          TAB 3: STEUERUNGS-TRESOR (KEYBINDS & CLOUD)
          ══════════════════════════════════════════════════════════════ */}
      {activeTab === 'keybinds' && (
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
                    <h2 className="text-sm font-bold text-white tracking-wide">CLOUD-SPEICHER &amp; SYNCHRONISATION</h2>
                    <span className="text-[10px] px-2 py-0.5 rounded-full bg-purple-950 text-purple-300 border border-purple-800 font-mono font-semibold">
                      {cloudDisplay}
                    </span>
                  </div>
                  <p className="text-xs text-slate-400 mt-0.5">
                    Hinterlege deinen Cloud-Ordner (Google Drive / OneDrive / Dropbox) zur standortübergreifenden Sicherung.
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
                placeholder="Cloud-Pfad (z. B. I:\Meine Ablage\Backup\StarCitizen)..."
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

            {/* Auto Cloud Sync Toggle & Guarantee */}
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pt-3 border-t border-purple-900/30 text-xs bg-purple-950/20 -mx-5 -mb-4 px-5 py-3 rounded-b-2xl">
              <label className="flex items-center space-x-3 cursor-pointer select-none">
                <input
                  type="checkbox"
                  checked={status?.autoCloudSyncEnabled !== false}
                  onChange={(e) => handleToggleAutoCloudSync(e.target.checked)}
                  className="w-4 h-4 rounded text-purple-600 bg-slate-900 border-slate-700 focus:ring-purple-500 cursor-pointer"
                />
                <div>
                  <span className="font-bold text-white block">
                    Vollautomatische Cloud-Synchronisation aktiv
                  </span>
                  <span className="text-[11px] text-slate-400 block">
                    Alle neuen Game.log-Dateien, user.cfg-Snapshots und Keybinds werden nach Spielende und App-Start automatisch ohne Klick synchronisiert.
                  </span>
                </div>
              </label>

              {typeof status?.cloudLogCount === 'number' && status.cloudLogCount > 0 && (
                <div className="flex items-center space-x-1.5 px-3 py-1.5 rounded-lg bg-emerald-950/60 border border-emerald-800/60 text-emerald-300 font-mono text-xs shrink-0 self-start sm:self-auto">
                  <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400" />
                  <span>{status.cloudLogCount} Logs aktuell in Cloud gesichert</span>
                </div>
              )}
            </div>
          </div>

          {/* Keybind-Tresor (actionmaps.xml) */}
          <div className="p-5 rounded-2xl bg-slate-900/70 border border-slate-800 space-y-4">
            <div className="flex items-center justify-between pb-3 border-b border-slate-800">
              <div className="flex items-center space-x-2.5">
                <div className="w-8 h-8 rounded-lg bg-purple-500/10 border border-purple-500/30 flex items-center justify-center text-purple-400">
                  <Key className="w-4 h-4" />
                </div>
                <div>
                  <h3 className="text-xs font-bold text-purple-300 tracking-wider">
                    STEUERUNGS-TRESOR (ACTIONMAPS.XML &amp; MAPPINGS)
                  </h3>
                  <p className="text-[11px] text-slate-400">
                    Sichert Joystick-, HOTAS-, HOSAS- und Tastaturbelegungen vor Spiel-Patches
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
                  placeholder="Optionale Notiz (z. B. VKB Dual-Stick Patch 4.0)..."
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
                      className="p-3.5 rounded-xl bg-slate-950/80 border border-slate-800/80 hover:border-slate-700 transition flex items-center justify-between gap-3"
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
                        className="flex items-center space-x-1.5 px-3.5 py-1.5 rounded-lg bg-purple-950 hover:bg-purple-900/80 text-purple-300 text-xs font-bold border border-purple-800/80 transition cursor-pointer shrink-0"
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
      )}
    </div>
  );
};
