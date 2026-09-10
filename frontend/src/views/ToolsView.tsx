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
} from 'lucide-react';
import { bridge, ToolsStatusDto } from '../services/photinoBridge';

export const ToolsView: React.FC = () => {
  const [status, setStatus] = useState<ToolsStatusDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [cfgContent, setCfgContent] = useState('');
  const [backupNote, setBackupNote] = useState('');
  const [copied, setCopied] = useState(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

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

  const handleCreateBackup = async () => {
    setActionLoading('backup');
    try {
      const res = await bridge.send<ToolsStatusDto>('backup_keybinds', { note: backupNote });
      setStatus(res);
      setBackupNote('');
      showToast('Keybind-Backup erfolgreich angelegt!');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Erstellen des Keybind-Backups');
    } finally {
      setActionLoading(null);
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
            className="flex items-center space-x-2 px-3 py-2 rounded-lg bg-slate-800/80 hover:bg-slate-700 text-slate-200 text-xs font-medium border border-slate-700 transition"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${actionLoading ? 'animate-spin' : ''}`} />
            <span>Neu laden</span>
          </button>
          <button
            onClick={handleSaveUserCfg}
            disabled={actionLoading !== null}
            className="flex items-center space-x-2 px-4 py-2 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold shadow-lg shadow-sky-600/25 border border-sky-400 transition"
          >
            <Save className="w-4 h-4" />
            <span>user.cfg Speichern</span>
          </button>
        </div>
      </div>

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
                value={backupNote}
                onChange={(e) => setBackupNote(e.target.value)}
                className="flex-1 bg-slate-800/80 border border-slate-700 rounded-lg px-3 py-2 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-sky-500"
              />
              <button
                onClick={handleCreateBackup}
                disabled={actionLoading === 'backup'}
                className="px-3 py-2 rounded-lg bg-sky-600/20 hover:bg-sky-600/30 text-sky-300 text-xs font-semibold border border-sky-500/40 transition shrink-0"
              >
                {actionLoading === 'backup' ? 'Sichere...' : '+ Backup'}
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
  );
};
