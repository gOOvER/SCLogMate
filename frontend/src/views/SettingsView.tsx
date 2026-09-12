import React, { useState, useEffect } from 'react';
import {
  Settings,
  Save,
  RefreshCw,
  Folder,
  Volume2,
  Bell,
  Eye,
  Database,
  Globe,
  CheckCircle2,
  Radio,
  Shield,
  FileText,
  Crop,
  RotateCcw,
  Zap,
  Monitor,
  Sliders,
  Check,
  AlertTriangle,
  Sparkles,
  Clock,
  Radar,
  MessageSquare,
} from 'lucide-react';
import {
  bridge,
  SettingsDto,
  ScanRegionDto,
  OcrRegionsConfig,
  OcrTestResult,
} from '../services/photinoBridge';

export const SettingsView: React.FC = () => {
  const [activeSubTab, setActiveSubTab] = useState<
    'general' | 'wipe' | 'hud' | 'ocr' | 'uex' | 'audio' | 'database' | 'developer'
  >('general');
  const [settings, setSettings] = useState<SettingsDto>({
    logPath: 'J:\\StarCitizen\\LIVE\\logbackups\\game.log',
    autoOcrEnabled: true,
    uexApiKey: '',
    overlayEnabled: false,
    overlayOpacity: 0.92,
    toastEnabled: true,
    toastBlueprintEnabled: true,
    toastMissionEnabled: true,
    toastReputationEnabled: true,
    toastRefineryEnabled: true,
    toastElevatorEnabled: true,
    toastShipDestructionEnabled: true,
    auroraIntegrationEnabled: true,
    auroraVolume: 40,
    rsTargetAlertEnabled: true,
    rsTargetSoundEnabled: true,
    wipeFilterEnabled: false,
    wipeDateString: '2026-05-15',
    wipeFilterMoney: true,
    wipeFilterContracts: true,
    wipeFilterFleet: false,
    wipeFilterBlueprints: false,
    selectedFontFamily: 'Inter',
    appLanguage: 'Auto',
    minimizeToTrayOnClose: true,
    autostartEnabled: false,
    debugMode: false,
  });
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  const [dbDiag, setDbDiag] = useState<any>(null);
  const [isCheckingDb, setIsCheckingDb] = useState(false);
  const [isRescanning, setIsRescanning] = useState(false);
  const [rescanProgress, setRescanProgress] = useState<any>(null);

  const [activeDbOp, setActiveDbOp] = useState<
    'none' | 'integrity' | 'vacuum' | 'repair' | 'rescan'
  >('none');
  const [dbOpMessage, setDbOpMessage] = useState<string | null>(null);
  const [dbCompletionBanner, setDbCompletionBanner] = useState<{
    success: boolean;
    title: string;
    details: string;
  } | null>(null);

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3500);
  };

  const handleRescanAll = async () => {
    if (isRescanning || isCheckingDb) return;
    try {
      setIsRescanning(true);
      setActiveDbOp('rescan');
      setDbCompletionBanner(null);
      setRescanProgress({
        current: 0,
        total: 10,
        percent: 0,
        currentFileName: 'Sammle Logs...',
        isCompleted: false,
      });
      await bridge.sendRequest('reparse_all_logs');
    } catch (err) {
      console.error('Reparse all failed:', err);
      setIsRescanning(false);
      setActiveDbOp('none');
      showToast('Fehler beim Re-Scan');
    }
  };

  const handleResetDb = async () => {
    if (
      !window.confirm(
        'Möchtest du die SQLite-Datenbank wirklich komplett zurücksetzen? Alle indexierten Sessions und Ereignisse werden gelöscht!'
      )
    ) {
      return;
    }
    try {
      setIsCheckingDb(true);
      setActiveDbOp('none');
      setDbCompletionBanner(null);
      await bridge.sendRequest('reset_database');
      showToast('Datenbank erfolgreich zurückgesetzt.');
      setDbCompletionBanner({
        success: true,
        title: 'Datenbank erfolgreich geleert!',
        details: 'Alle Sessions, Ereignisse und Lagerbestände wurden auf den Anfangszustand zurückgesetzt.',
      });
      loadDbDiag(false);
    } catch (err) {
      showToast('Fehler beim Zurücksetzen der Datenbank');
    } finally {
      setIsCheckingDb(false);
    }
  };

  const handleOpenFolder = (target: string) => {
    bridge.send('open_folder', { target });
  };

  const [ocrConfig, setOcrConfig] = useState<OcrRegionsConfig | null>(null);
  const [isSelectingRegion, setIsSelectingRegion] = useState<string | null>(null);
  const [isTestingScan, setIsTestingScan] = useState<string | null>(null);
  const [testScanResult, setTestScanResult] = useState<OcrTestResult | null>(null);
  const [manualWallet, setManualWallet] = useState<ScanRegionDto>({ x: 1300, y: 415, width: 500, height: 80 });
  const [manualRs, setManualRs] = useState<ScanRegionDto>({ x: 720, y: 464, width: 480, height: 160 });
  const [manualChat, setManualChat] = useState<ScanRegionDto>({ x: 20, y: 400, width: 620, height: 450 });

  const loadOcrConfig = async () => {
    try {
      const cfg = await bridge.sendRequest<OcrRegionsConfig>('get_ocr_regions');
      if (cfg) {
        setOcrConfig(cfg);
        const curW = cfg.walletRegion || cfg.defaultWalletRegion;
        if (curW) setManualWallet(curW);
        const curRs = cfg.rsScanRegion || cfg.defaultRsRegion;
        if (curRs) setManualRs(curRs);
        const curChat = cfg.chatRegion || cfg.defaultChatRegion;
        if (curChat) setManualChat(curChat);
      }
    } catch (err) {
      console.error('Failed to load OCR config:', err);
    }
  };

  const handleSelectRegion = async (target: 'wallet' | 'contract' | 'rs' | 'chat') => {
    try {
      setIsSelectingRegion(target);
      showToast('Bildschirm-Auswahl gestartet: Ziehe mit der Maus ein Rechteck...');
      const res = await bridge.sendRequest<any>('select_ocr_region', { target });
      if (res?.success && res.region) {
        showToast(`Scan-Bereich gespeichert: ${res.region.width}×${res.region.height} @ (${res.region.x}, ${res.region.y})`);
        if (res.config) {
          setOcrConfig(res.config);
          if (target === 'wallet') setManualWallet(res.region);
          if (target === 'rs') setManualRs(res.region);
          if (target === 'chat') setManualChat(res.region);
        } else {
          loadOcrConfig();
        }
      } else if (res?.cancelled) {
        showToast('Auswahl abgebrochen');
      }
    } catch (err) {
      console.error('Region select failed:', err);
      showToast('Fehler bei der Bildschirmauswahl');
    } finally {
      setIsSelectingRegion(null);
    }
  };

  const handleTestScan = async (target: 'wallet' | 'contract' | 'rs' | 'chat') => {
    try {
      setIsTestingScan(target);
      setTestScanResult(null);
      const res = await bridge.sendRequest<OcrTestResult>('test_ocr_scan', { target });
      if (res) {
        setTestScanResult(res);
        if (res.success) {
          if (target === 'wallet') {
            showToast(res.extractedValue != null
              ? `✓ Kontostand erkannt: ${res.extractedValue.toLocaleString('de-DE')} aUEC (${res.durationMs}ms)`
              : `✓ Text erkannt: ${res.recognizedText.slice(0, 35)}... (${res.durationMs}ms)`);
          } else if (target === 'rs') {
            showToast(res.extractedValue != null
              ? `✓ RS Signatur erkannt: ${res.extractedValue.toLocaleString('de-DE')} RS (${res.durationMs}ms)`
              : `✓ Text erkannt: ${res.recognizedText.slice(0, 35)}... (${res.durationMs}ms)`);
          } else {
            showToast(`✓ Text erkannt: ${res.recognizedText.slice(0, 35)}... (${res.durationMs}ms)`);
          }
        } else {
          showToast(`⚠️ OCR Test: Kein gültiger Wert erkannt (${res.recognizedText || 'leer'})`);
        }
      }
    } catch (err) {
      console.error('Test scan failed:', err);
      showToast('Fehler beim OCR Test-Scan');
    } finally {
      setIsTestingScan(null);
    }
  };

  const handleResetRegion = async (target: 'wallet' | 'contract' | 'rs' | 'chat') => {
    try {
      const cfg = await bridge.sendRequest<OcrRegionsConfig>('save_ocr_region', { target, region: null });
      if (cfg) {
        setOcrConfig(cfg);
        if (target === 'wallet') setManualWallet(cfg.defaultWalletRegion);
        if (target === 'rs') setManualRs(cfg.defaultRsRegion);
        if (target === 'chat' && cfg.defaultChatRegion) setManualChat(cfg.defaultChatRegion);
      }
      showToast('Bereich auf Standard (Auto-Erkennung) zurückgesetzt');
    } catch (err) {
      showToast('Fehler beim Zurücksetzen');
    }
  };

  const handleToggleScanBox = async (target: 'wallet' | 'contract') => {
    try {
      const cfg = await bridge.sendRequest<OcrRegionsConfig>('toggle_scan_indicator', { target });
      if (cfg) setOcrConfig(cfg);
      showToast('Scan-Rahmen Anzeige umgeschaltet');
    } catch (err) {
      showToast('Fehler beim Umschalten des Scan-Rahmens');
    }
  };

  const handleApplyPreset = async (target: 'wallet' | 'contract', preset: ScanRegionDto) => {
    try {
      const cfg = await bridge.sendRequest<OcrRegionsConfig>('save_ocr_region', { target, region: preset });
      if (cfg) {
        setOcrConfig(cfg);
        if (target === 'wallet') setManualWallet(preset);
      }
      showToast(`Preset angewendet: ${preset.width}×${preset.height} @ (${preset.x}, ${preset.y})`);
    } catch (err) {
      showToast('Fehler beim Anwenden des Presets');
    }
  };

  const handleApplyManualCoords = async (target: 'wallet' | 'contract' | 'rs' | 'chat', coords: ScanRegionDto) => {
    try {
      const cfg = await bridge.sendRequest<OcrRegionsConfig>('save_ocr_region', { target, region: coords });
      if (cfg) setOcrConfig(cfg);
      showToast(`Koordinaten gespeichert: ${coords.width}×${coords.height} @ (${coords.x}, ${coords.y})`);
    } catch (err) {
      showToast('Fehler beim Speichern der Koordinaten');
    }
  };

  const loadSettings = async () => {
    try {
      setLoading(true);
      const data = await bridge.send<SettingsDto>('get_settings');
      if (data) {
        setSettings(data);
      }
      await loadOcrConfig();
    } catch (err) {
      console.error('Failed to load settings:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadDbDiag = async (isManual = false) => {
    try {
      setIsCheckingDb(true);
      if (isManual) {
        setActiveDbOp('integrity');
        setDbOpMessage('Führe tiefe SQLite-Integritätsprüfung durch (PRAGMA integrity_check)... Bitte warten.');
        setDbCompletionBanner(null);
      }
      const diag = await bridge.sendRequest<any>('get_db_diagnostics');
      if (diag) {
        setDbDiag(diag);
        if (isManual) {
          setDbCompletionBanner({
            success: diag.integrityCheckOk,
            title: diag.integrityCheckOk
              ? '✓ Tiefe Integritätsprüfung: FEHLERFREI'
              : '⚠️ Integritätsprüfung: Fehler festgestellt',
            details: `Ergebnis: ${diag.integrityMessage || 'quick_check ok'} · Datenbankgröße: ${diag.formattedSize} · Geprüft um ${new Date().toLocaleTimeString()}`,
          });
          showToast(diag.integrityCheckOk ? '✓ Integritätsprüfung: OK' : '⚠️ Integritätsfehler');
        }
      }
    } catch (err) {
      console.error('Failed to load db diagnostics:', err);
      if (isManual) {
        setDbCompletionBanner({
          success: false,
          title: 'Fehler bei der Integritätsprüfung',
          details: String(err),
        });
      }
    } finally {
      setIsCheckingDb(false);
      setActiveDbOp('none');
      setDbOpMessage(null);
    }
  };

  const handleAutoDetect = async () => {
    try {
      const res = await bridge.sendRequest<any>('detect_log_path');
      if (res?.currentLogPath) {
        setSettings((prev) => ({ ...prev, logPath: res.currentLogPath }));
        showToast(`Star Citizen ${res.channel} erkannt: ${res.currentLogPath}`);
      } else {
        showToast('Keine Game.log gefunden');
      }
    } catch (err) {
      showToast('Fehler bei der Auto-Erkennung');
    }
  };

  const handleBrowse = async () => {
    try {
      const res = await bridge.sendRequest<any>('browse_log_file');
      if (res?.currentLogPath) {
        setSettings((prev) => ({ ...prev, logPath: res.currentLogPath }));
        showToast(`Ausgewählt: ${res.currentLogPath}`);
      }
    } catch (err) {
      console.error('Browse failed:', err);
    }
  };

  const handleSetWipe48 = () => {
    setSettings((prev) => ({
      ...prev,
      wipeFilterEnabled: true,
      wipeDateString: '2026-05-15',
    }));
    showToast('Wipe-Filter auf Alpha 4.8 (15.05.2026) gesetzt');
  };

  const handleSetWipeToday = () => {
    const today = new Date().toISOString().split('T')[0];
    setSettings((prev) => ({
      ...prev,
      wipeFilterEnabled: true,
      wipeDateString: today,
    }));
    showToast(`Wipe-Filter auf heutigen Tag (${today}) gesetzt`);
  };

  const handleClearWipe = () => {
    setSettings((prev) => ({
      ...prev,
      wipeFilterEnabled: false,
    }));
    showToast('Wipe-Filter deaktiviert');
  };

  const handleSimulate = async (eventType: string, param?: string) => {
    try {
      await bridge.sendRequest('simulate_event', { eventType, param });
      showToast(`🧪 [DEBUG] Simuliert: ${eventType}`);
    } catch (err) {
      console.error('Simulation failed:', err);
      showToast('Fehler bei der Simulation');
    }
  };

  const handleDumpDebugState = async () => {
    try {
      await bridge.sendRequest('dump_debug_state');
      showToast('✓ [DEBUG] Detaillierter Status in SCLogMate.debug.log geschrieben.');
    } catch (err) {
      showToast('Fehler beim Dump');
    }
  };

  const handleClearDebugLog = async () => {
    try {
      await bridge.sendRequest('clear_debug_log');
      showToast('✓ [DEBUG] Debug-Logdatei geleert.');
    } catch (err) {
      showToast('Fehler beim Leeren des Logs');
    }
  };

  const handleClearContracts = async () => {
    try {
      await bridge.sendRequest('clear_contracts');
      showToast('Aktive Auftragsliste geleert.');
    } catch (err) {
      showToast('Fehler beim Leeren der Aufträge');
    }
  };

  const handleVacuum = async () => {
    try {
      setIsCheckingDb(true);
      setActiveDbOp('vacuum');
      setDbOpMessage('Führe SQLite VACUUM & Bereinigung durch... Bitte warten.');
      setDbCompletionBanner(null);
      const res = await bridge.sendRequest<any>('cleanup_database');
      const diag = await bridge.sendRequest<any>('get_db_diagnostics');
      if (diag) setDbDiag(diag);
      setDbCompletionBanner({
        success: true,
        title: 'Datenbank-Bereinigung & VACUUM erfolgreich abgeschlossen!',
        details: `${res?.cleanedEvents ?? 0} verwaiste Events bereinigt · Dateigröße: ${res?.sizeBefore} → ${res?.sizeAfter}`,
      });
      showToast(`SQLite VACUUM abgeschlossen! ${res?.sizeBefore} → ${res?.sizeAfter}`);
    } catch (err) {
      showToast('Fehler beim VACUUM');
      setDbCompletionBanner({
        success: false,
        title: 'Fehler beim VACUUM',
        details: String(err),
      });
    } finally {
      setIsCheckingDb(false);
      setActiveDbOp('none');
      setDbOpMessage(null);
    }
  };

  const handleRepairDb = async () => {
    try {
      setIsCheckingDb(true);
      setActiveDbOp('repair');
      setDbOpMessage('Repariere SQLite-Tabellenstruktur und aktualisiere Indizes... Bitte warten.');
      setDbCompletionBanner(null);
      const res = await bridge.sendRequest<any>('repair_db_structure');
      if (res?.diagnostics) setDbDiag(res.diagnostics);
      setDbCompletionBanner({
        success: res?.success ?? true,
        title: 'Struktur- und Index-Reparatur abgeschlossen!',
        details: res?.message || 'Alle Tabellen und Schema-Indizes wurden erfolgreich überprüft und repariert.',
      });
      showToast('Datenbank-Struktur und Indizes erfolgreich aktualisiert');
    } catch (err) {
      showToast('Fehler bei der Reparatur');
      setDbCompletionBanner({
        success: false,
        title: 'Fehler bei der Reparatur',
        details: String(err),
      });
    } finally {
      setIsCheckingDb(false);
      setActiveDbOp('none');
      setDbOpMessage(null);
    }
  };

  const handleSave = async () => {
    try {
      setSaving(true);
      await bridge.send('save_settings', { settings });
      showToast('Einstellungen erfolgreich in settings.json gespeichert!');
    } catch (err) {
      console.error('Failed to save settings:', err);
      showToast('Fehler beim Speichern der Einstellungen');
    } finally {
      setSaving(false);
    }
  };

  useEffect(() => {
    loadSettings();
    loadDbDiag(false);
  }, []);

  useEffect(() => {
    const unbind = bridge.on<any>('SCAN_PROGRESS', (progress) => {
      setRescanProgress(progress);
      if (progress.isCompleted) {
        setIsRescanning(false);
        setActiveDbOp('none');
        setDbCompletionBanner({
          success: true,
          title: 'Kompletter Re-Scan erfolgreich abgeschlossen!',
          details: `${progress.indexedSessions ?? 0} Sessions und ${(progress.totalEvents ?? 0).toLocaleString()} Ereignisse wurden frisch mit neuen Parser-Regeln indexiert.`,
        });
        showToast(`✓ Re-Scan abgeschlossen (${progress.indexedSessions ?? 0} Sessions)`);
        loadDbDiag(false);
      } else {
        setIsRescanning(true);
        setActiveDbOp('rescan');
      }
    });
    return () => unbind();
  }, []);

  useEffect(() => {
    if (activeSubTab === 'database' && !dbDiag) {
      loadDbDiag();
    }
  }, [activeSubTab]);

  if (loading) {
    return (
      <div className="flex flex-col items-center justify-center h-96 space-y-4">
        <RefreshCw className="w-8 h-8 text-sky-400 animate-spin" />
        <p className="text-sm text-slate-400">Lade Einstellungen...</p>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6 max-w-7xl mx-auto">
      {/* Toast Notification */}
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
            <Settings className="w-6 h-6" />
          </div>
          <div>
            <div className="flex items-center space-x-2">
              <h1 className="text-xl font-bold text-white tracking-wide">EINSTELLUNGEN & OPTIONEN</h1>
              <span className="px-2 py-0.5 text-xs font-semibold rounded bg-sky-950 text-sky-400 border border-sky-800">
                SCLogMate 1.3
              </span>
            </div>
            <p className="text-xs text-slate-400 mt-1 font-mono">
              Konfiguration für Game.log Parser, HUD-Overlays, Tesseract OCR, Aurora und Externe APIs
            </p>
          </div>
        </div>

        <button
          onClick={handleSave}
          disabled={saving}
          className="flex items-center space-x-2 px-5 py-2.5 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold shadow-lg shadow-sky-600/25 border border-sky-400 transition"
        >
          <Save className="w-4 h-4" />
          <span>{saving ? 'Speichere...' : 'Einstellungen speichern'}</span>
        </button>
      </div>

      {/* Sub-Tab Navigation Bar */}
      <div className="flex flex-wrap gap-2 border-b border-slate-800 pb-3">
        {[
          { id: 'general', label: '📁 Allgemein', icon: Folder },
          { id: 'wipe', label: '⏳ Wipe & Filter', icon: Clock },
          { id: 'hud', label: '🖥 Overlays & HUD', icon: Eye },
          { id: 'ocr', label: '👁 mobiGlas & OCR', icon: Radio },
          { id: 'uex', label: '🌐 UEX Integration', icon: Globe },
          { id: 'audio', label: '🎙 VoiceAttack & Aurora', icon: Volume2 },
          { id: 'database', label: '💾 Datenbank & Wartung', icon: Database },
          ...(settings.debugMode ? [{ id: 'developer', label: '🧪 Entwickler', icon: Sparkles }] : []),
        ].map((tab) => {
          const Icon = tab.icon;
          const isActive = activeSubTab === tab.id;
          return (
            <button
              key={tab.id}
              onClick={() => setActiveSubTab(tab.id as any)}
              className={`flex items-center space-x-2 px-4 py-2 rounded-lg text-xs font-medium transition cursor-pointer ${
                isActive
                  ? 'bg-sky-500/20 text-sky-300 border border-sky-500/40 shadow-sm'
                  : 'bg-slate-900/40 hover:bg-slate-800/60 text-slate-400 hover:text-slate-200 border border-transparent'
              }`}
            >
              <Icon className="w-4 h-4" />
              <span>{tab.label}</span>
            </button>
          );
        })}
      </div>

      {/* Tab 1: Allgemein */}
      {activeSubTab === 'general' && (
        <div className="space-y-6">
          {/* 1. Star Citizen Logdatei */}
          <div className="p-6 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-5">
            <div>
              <h2 className="text-sm font-bold text-sky-400 flex items-center space-x-2">
                <Folder className="w-4 h-4" />
                <span>STAR CITIZEN & LOGDATEI</span>
              </h2>
              <p className="text-xs text-slate-400 mt-1">
                Wähle deine Game.log Datei aus dem LIVE- oder PTU-Installationsverzeichnis von Star Citizen.
              </p>
            </div>

            <div className="space-y-2">
              <div className="flex items-center space-x-3">
                <input
                  type="text"
                  value={settings.logPath || ''}
                  onChange={(e) => setSettings({ ...settings, logPath: e.target.value })}
                  placeholder="z. B. J:\StarCitizen\LIVE\logbackups\game.log"
                  className="flex-1 bg-slate-950 border border-slate-700 rounded-lg px-4 py-2.5 text-xs text-slate-200 font-mono focus:outline-none focus:border-sky-500"
                />
                <button
                  onClick={handleBrowse}
                  className="px-3 py-2.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-medium border border-slate-700 transition shrink-0 cursor-pointer"
                  title="Game.log manuell auswählen"
                >
                  📁 Durchsuchen
                </button>
                <button
                  onClick={handleAutoDetect}
                  className="px-3 py-2.5 rounded-lg bg-sky-900/60 hover:bg-sky-800/80 text-sky-300 text-xs font-medium border border-sky-700/60 transition shrink-0 cursor-pointer"
                  title="Automatisch auf allen Laufwerken nach Game.log suchen"
                >
                  ⚡ Auto-Erkennung
                </button>
              </div>
              <div className="text-[11px] text-slate-500">
                Hinweis: SCLogMate liest die Datei im Non-Locking Shared Stream und tailt Live-Events verzögerungsfrei.
              </div>
            </div>
          </div>

          {/* 2. Erscheinungsbild & Schriftart (Font Chooser) */}
          <div className="p-6 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-4">
            <div>
              <h2 className="text-sm font-bold text-sky-400 flex items-center space-x-2">
                <FileText className="w-4 h-4" />
                <span>ERSCHEINUNGSBILD & SCHRIFTART (FONT CHOOSER)</span>
              </h2>
              <p className="text-xs text-slate-400 mt-1">
                Passe die primäre Schriftart der Benutzeroberfläche an deine Vorlieben an.
              </p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 items-center">
              <select
                value={settings.selectedFontFamily || 'Inter'}
                onChange={(e) => setSettings({ ...settings, selectedFontFamily: e.target.value })}
                className="bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-xs text-slate-200 focus:outline-none focus:border-sky-500"
              >
                <option value="Inter">Inter (Standard Modern)</option>
                <option value="Orbitron">Orbitron (Sci-Fi Cockpit)</option>
                <option value="Rajdhani">Rajdhani (Cyber Tech)</option>
                <option value="Segoe UI">Segoe UI (Windows Native)</option>
                <option value="Roboto">Roboto (Clean Sans)</option>
                <option value="Consolas">Consolas (Monospace)</option>
              </select>
              <div className="p-3 rounded-lg bg-slate-950/80 border border-slate-800 text-xs text-slate-300 truncate" style={{ fontFamily: settings.selectedFontFamily || 'Inter' }}>
                Vorschau: Star Citizen Live Log Companion 0123456789 (aUEC · Saldo · ⬡ Baupläne)
              </div>
            </div>
          </div>

          {/* 3. Sprache & Regional-Einstellungen */}
          <div className="p-6 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-sm font-bold text-sky-400 flex items-center space-x-2">
                  <Globe className="w-4 h-4" />
                  <span>SPRACHE & REGIONAL-EINSTELLUNGEN / LANGUAGE</span>
                </h2>
                <p className="text-xs text-slate-400 mt-1">
                  Wähle die Sprache der Anwendung und für sprachabhängige Zusatzmodule wie VoiceAttack & Aurora.
                </p>
              </div>
              <div className="flex items-center space-x-2 px-3 py-1 rounded bg-sky-950 border border-sky-800 text-xs text-sky-300 font-bold">
                <span>{settings.appLanguage === 'en-US' ? '🇬🇧 English' : '🇩🇪 Deutsch'}</span>
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 items-center">
              <select
                value={settings.appLanguage || 'Auto'}
                onChange={(e) => setSettings({ ...settings, appLanguage: e.target.value })}
                className="bg-slate-950 border border-slate-700 rounded-lg px-3 py-2 text-xs text-slate-200 focus:outline-none focus:border-sky-500"
              >
                <option value="Auto">🌐 Auto (Systemstandard)</option>
                <option value="de-DE">🇩🇪 Deutsch (Standard)</option>
                <option value="en-US">🇬🇧 English (US / UK)</option>
              </select>
              <div className="text-xs text-slate-400">
                {settings.appLanguage === 'en-US'
                  ? 'Application strings in English. Voice prompts English.'
                  : 'Benutzeroberfläche auf Deutsch. Deutsche Sprachdateien aktiv.'}
              </div>
            </div>
          </div>

          {/* 4. Fenster-, Tray- & System-Verhalten */}
          <div className="p-6 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-4">
            <h2 className="text-sm font-bold text-sky-400 flex items-center space-x-2">
              <Monitor className="w-4 h-4" />
              <span>FENSTER-, TRAY- & SYSTEM-VERHALTEN</span>
            </h2>

            <div className="space-y-3">
              <label className="flex items-start space-x-3 cursor-pointer p-2 rounded hover:bg-slate-800/40">
                <input
                  type="checkbox"
                  checked={settings.minimizeToTrayOnClose ?? true}
                  onChange={(e) => setSettings({ ...settings, minimizeToTrayOnClose: e.target.checked })}
                  className="rounded border-slate-700 text-sky-600 focus:ring-sky-500 bg-slate-800 mt-0.5"
                />
                <div>
                  <div className="text-xs font-semibold text-slate-200">
                    Beim Klick auf das Schließen-Kreuz (X) ins System-Tray minimieren
                  </div>
                  <div className="text-[11px] text-slate-400">
                    Die App läuft im Hintergrund im Infobereich weiter (Tracking, OCR & Overlays bleiben aktiv).
                  </div>
                </div>
              </label>

              <label className="flex items-start space-x-3 cursor-pointer p-2 rounded hover:bg-slate-800/40">
                <input
                  type="checkbox"
                  checked={settings.autostartEnabled ?? false}
                  onChange={(e) => setSettings({ ...settings, autostartEnabled: e.target.checked })}
                  className="rounded border-slate-700 text-sky-600 focus:ring-sky-500 bg-slate-800 mt-0.5"
                />
                <div>
                  <div className="text-xs font-semibold text-slate-200">
                    Mit Windows automatisch im Hintergrund starten (System-Tray)
                  </div>
                  <div className="text-[11px] text-slate-400">
                    Startet SCLogMate bei der Windows-Anmeldung minimiert in den Infobereich. Erkennt Star Citizen bei Spielstart sofort vollautomatisch.
                  </div>
                </div>
              </label>

              <div className="pt-2 border-t border-slate-800">
                <label className="flex items-start space-x-3 cursor-pointer p-2 rounded hover:bg-slate-800/40">
                  <input
                    type="checkbox"
                    checked={settings.debugMode ?? false}
                    onChange={(e) => setSettings({ ...settings, debugMode: e.target.checked })}
                    className="rounded border-slate-700 text-amber-500 focus:ring-amber-500 bg-slate-800 mt-0.5"
                  />
                  <div>
                    <div className="text-xs font-semibold text-amber-300 flex items-center space-x-2">
                      <Sparkles className="w-3.5 h-3.5" />
                      <span>Entwickler- & Debug-Modus aktivieren</span>
                    </div>
                    <div className="text-[11px] text-slate-400">
                      Schaltet den Sub-Tab "🧪 Entwickler" mit Test-Eventsimulatoren (Schutzzone, Schiff, Bauplan, 30k) und Diagnose-Dumps frei.
                    </div>
                  </div>
                </label>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Tab 2: Wipe & Filter */}
      {activeSubTab === 'wipe' && (
        <div className="space-y-6">
          <div className="p-6 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-5">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-sm font-bold text-amber-400 flex items-center space-x-2">
                  <Clock className="w-4 h-4" />
                  <span>WIPE- & PERSISTENZ-FILTER</span>
                </h2>
                <p className="text-xs text-slate-400 mt-1">
                  Kappt historische Log-Daten vor einem bestimmten Patch-Stichtag (z.B. Alpha 4.8 Wipe), damit Kontosalden, Missionen und Statistiken nur für den aktuellen Zyklus berechnet werden.
                </p>
              </div>
              <label className="flex items-center space-x-2 cursor-pointer px-3 py-1.5 rounded-lg bg-amber-950/60 border border-amber-800/60 text-amber-300 text-xs font-bold">
                <input
                  type="checkbox"
                  checked={settings.wipeFilterEnabled ?? false}
                  onChange={(e) => setSettings({ ...settings, wipeFilterEnabled: e.target.checked })}
                  className="rounded border-amber-700 text-amber-500 focus:ring-amber-400 bg-slate-900"
                />
                <span>Wipe-Filter aktiv</span>
              </label>
            </div>

            {/* Stichtag festlegen */}
            <div className="p-4 rounded-xl bg-slate-950/80 border border-slate-800/80 space-y-3">
              <div className="text-xs font-semibold text-slate-200">
                Stichtag festlegen (Wipe-Datum)
              </div>
              <div className="flex flex-wrap items-center gap-2">
                <input
                  type="text"
                  value={settings.wipeDateString || '2026-05-15'}
                  onChange={(e) => setSettings({ ...settings, wipeDateString: e.target.value })}
                  placeholder="JJJJ-MM-TT (z. B. 2026-05-15)"
                  className="flex-1 min-w-[200px] bg-slate-900 border border-slate-700 rounded-lg px-4 py-2 text-xs text-slate-200 font-mono focus:outline-none focus:border-amber-500"
                />
                <button
                  onClick={handleSetWipe48}
                  className="px-3 py-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-amber-300 text-xs font-medium border border-slate-700 transition cursor-pointer"
                  title="Auf Alpha 4.8 (15.05.2026) setzen"
                >
                  ★ 4.8 Wipe
                </button>
                <button
                  onClick={handleSetWipeToday}
                  className="px-3 py-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-medium border border-slate-700 transition cursor-pointer"
                  title="Auf heutigen Tag setzen"
                >
                  📅 Heute
                </button>
                <button
                  onClick={handleClearWipe}
                  className="px-3 py-2 rounded-lg bg-rose-950/40 hover:bg-rose-900/60 text-rose-300 text-xs font-medium border border-rose-800/60 transition cursor-pointer"
                  title="Filter deaktivieren"
                >
                  ✕ Filter aus
                </button>
              </div>
            </div>

            {/* Modular filterbar */}
            <div className="p-4 rounded-xl bg-slate-950/80 border border-slate-800/80 space-y-3">
              <div className="text-xs font-semibold text-slate-200">
                Betroffene Datenbereiche (Modular filterbar)
              </div>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                <label className="flex items-center space-x-2.5 p-2.5 rounded-lg bg-slate-900/60 border border-slate-800 hover:border-slate-700 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={settings.wipeFilterMoney ?? true}
                    onChange={(e) => setSettings({ ...settings, wipeFilterMoney: e.target.checked })}
                    className="rounded border-slate-700 text-amber-500 focus:ring-amber-400 bg-slate-800"
                  />
                  <span className="text-xs text-slate-200">💰 Geld & Saldo</span>
                </label>

                <label className="flex items-center space-x-2.5 p-2.5 rounded-lg bg-slate-900/60 border border-slate-800 hover:border-slate-700 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={settings.wipeFilterContracts ?? true}
                    onChange={(e) => setSettings({ ...settings, wipeFilterContracts: e.target.checked })}
                    className="rounded border-slate-700 text-amber-500 focus:ring-amber-400 bg-slate-800"
                  />
                  <span className="text-xs text-slate-200">❖ Missionen</span>
                </label>

                <label className="flex items-center space-x-2.5 p-2.5 rounded-lg bg-slate-900/60 border border-slate-800 hover:border-slate-700 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={settings.wipeFilterFleet ?? false}
                    onChange={(e) => setSettings({ ...settings, wipeFilterFleet: e.target.checked })}
                    className="rounded border-slate-700 text-amber-500 focus:ring-amber-400 bg-slate-800"
                  />
                  <span className="text-xs text-slate-200">✈ Flotte</span>
                </label>

                <label className="flex items-center space-x-2.5 p-2.5 rounded-lg bg-slate-900/60 border border-slate-800 hover:border-slate-700 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={settings.wipeFilterBlueprints ?? false}
                    onChange={(e) => setSettings({ ...settings, wipeFilterBlueprints: e.target.checked })}
                    className="rounded border-slate-700 text-amber-500 focus:ring-amber-400 bg-slate-800"
                  />
                  <span className="text-xs text-slate-200">🛠 Baupläne</span>
                </label>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Tab 2: Overlays & HUD */}
      {activeSubTab === 'hud' && (
        <div className="space-y-6">
          <div className="p-6 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-5">
            <div>
              <h2 className="text-sm font-bold text-sky-400 flex items-center space-x-2">
                <Eye className="w-4 h-4" />
                <span>IN-GAME FLOATING MINI-HUD OVERLAY</span>
              </h2>
              <p className="text-xs text-slate-400 mt-1">
                Immer im Vordergrund schwebendes Mini-Cockpit-Fenster für aUEC-Saldo, letzte Missionen und RS Scan-Werte.
              </p>
            </div>

            <div className="space-y-4">
              <label className="flex items-center space-x-3 cursor-pointer p-3 rounded-lg bg-slate-950/60 border border-slate-800">
                <input
                  type="checkbox"
                  checked={settings.overlayEnabled}
                  onChange={(e) => setSettings({ ...settings, overlayEnabled: e.target.checked })}
                  className="w-4 h-4 rounded border-slate-700 text-sky-600 focus:ring-sky-500 bg-slate-800"
                />
                <div>
                  <div className="text-xs font-semibold text-white">Floating Mini-HUD aktivieren</div>
                  <div className="text-[11px] text-slate-400">
                    Kompaktes HUD-Fenster beim Spielstart automatisch einblenden
                  </div>
                </div>
              </label>

              <div className="p-4 rounded-lg bg-slate-950/60 border border-slate-800 space-y-2">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-semibold text-slate-300">HUD-Deckkraft (Opacity):</span>
                  <span className="text-xs font-mono text-sky-400 font-bold">
                    {Math.round(settings.overlayOpacity * 100)}%
                  </span>
                </div>
                <input
                  type="range"
                  min="0.2"
                  max="1"
                  step="0.05"
                  value={settings.overlayOpacity}
                  onChange={(e) =>
                    setSettings({ ...settings, overlayOpacity: parseFloat(e.target.value) })
                  }
                  className="w-full accent-sky-500 cursor-pointer"
                />
              </div>
            </div>
          </div>

          <div className="p-6 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-5">
            <div>
              <h2 className="text-sm font-bold text-sky-400 flex items-center space-x-2">
                <Bell className="w-4 h-4" />
                <span>IN-GAME TOAST-BENACHRICHTIGUNGEN</span>
              </h2>
              <p className="text-xs text-slate-400 mt-1">
                Wähle, welche Ereignisse als dezent animiertes Overlay-Banner eingeblendet werden sollen.
              </p>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-xs">
              {[
                {
                  key: 'toastBlueprintEnabled',
                  title: '⬡ Neue Baupläne (Blueprints)',
                  sub: 'Erlernte Crafting-Rezepte sofort anzeigen',
                },
                {
                  key: 'toastMissionEnabled',
                  title: '📜 Missionsabschluss & Belohnungen',
                  sub: 'aUEC-Auszahlungen & Missions-Status',
                },
                {
                  key: 'toastReputationEnabled',
                  title: '⭐ Ruf- & Fraktionsaufstiege',
                  sub: 'Veränderungen der Affinity & Rang-Boni',
                },
                {
                  key: 'toastRefineryEnabled',
                  title: '🏭 Veredelungsaufträge fertig',
                  sub: 'Benachrichtigung bei abgeschlossener Raffinerie',
                },
                {
                  key: 'toastElevatorEnabled',
                  title: '🛗 Frachtaufzug-Warnungen',
                  sub: 'Statusänderungen am Cargo Elevator',
                },
                {
                  key: 'toastShipDestructionEnabled',
                  title: '💥 Schiffsverlust & Zerstörung',
                  sub: 'Versicherungs- & Bergungsinformationen',
                },
              ].map((item) => (
                <label
                  key={item.key}
                  className="flex items-start space-x-3 p-3 rounded-lg bg-slate-950/60 border border-slate-800 cursor-pointer hover:border-slate-700 transition"
                >
                  <input
                    type="checkbox"
                    checked={(settings as any)[item.key]}
                    onChange={(e) =>
                      setSettings({ ...settings, [item.key]: e.target.checked })
                    }
                    className="mt-0.5 w-4 h-4 rounded border-slate-700 text-sky-600 focus:ring-sky-500 bg-slate-800"
                  />
                  <div>
                    <div className="font-semibold text-white">{item.title}</div>
                    <div className="text-[11px] text-slate-400 mt-0.5">{item.sub}</div>
                  </div>
                </label>
              ))}
            </div>
          </div>
        </div>
      )}

      {/* Tab 3: mobiGlas & OCR */}
      {activeSubTab === 'ocr' && (
        <div className="space-y-6">
          {/* Resolution & System Banner */}
          <div className="p-4 rounded-xl bg-slate-900/80 border border-slate-800 flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-inner">
            <div className="flex items-center space-x-3">
              <div className="w-9 h-9 rounded-lg bg-sky-500/10 border border-sky-500/30 flex items-center justify-center text-sky-400 shrink-0">
                <Monitor className="w-5 h-5" />
              </div>
              <div>
                <div className="text-xs font-bold text-white flex items-center space-x-2">
                  <span>ERKANNTES SYSTEM-DISPLAY:</span>
                  <span className="font-mono text-sky-400">
                    {ocrConfig ? `${ocrConfig.screenWidth} × ${ocrConfig.screenHeight}` : '1920 × 1080'}
                  </span>
                  <span className="text-[10px] px-1.5 py-0.5 rounded bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 font-mono">
                    ONLINE
                  </span>
                </div>
                <div className="text-[11px] text-slate-400 mt-0.5">
                  Windows.Media.Ocr Engine aktiv · Multi-Monitor & DPI-Skalierung unterstützt
                </div>
              </div>
            </div>

            <button
              onClick={() => loadOcrConfig()}
              className="flex items-center space-x-1.5 px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-medium border border-slate-700 transition shrink-0"
              title="Aktuelle Regionen und Displaymaße neu abfragen"
            >
              <RefreshCw className="w-3.5 h-3.5" />
              <span>Neu laden</span>
            </button>
          </div>

          {/* Sektion 1: mobiGlas aUEC Kontostand Scan-Bereich */}
          <div className="p-6 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-5">
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-3">
              <div>
                <div className="flex items-center space-x-2">
                  <h2 className="text-sm font-bold text-sky-400 flex items-center space-x-2">
                    <Crop className="w-4 h-4" />
                    <span>MOBIGLAS WALLET SCAN-BEREICH (aUEC)</span>
                  </h2>
                  {ocrConfig?.walletRegion ? (
                    <span className="px-2 py-0.5 text-[10px] font-bold rounded bg-sky-950 text-sky-300 border border-sky-800">
                      BENUTZERDEFINIERT: {ocrConfig.walletRegion.width}×{ocrConfig.walletRegion.height} @ ({ocrConfig.walletRegion.x},{ocrConfig.walletRegion.y})
                    </span>
                  ) : (
                    <span className="px-2 py-0.5 text-[10px] font-bold rounded bg-emerald-950/80 text-emerald-300 border border-emerald-800">
                      STANDARD AUTO-ERKENNUNG ({ocrConfig?.defaultWalletRegion ? `${ocrConfig.defaultWalletRegion.width}×${ocrConfig.defaultWalletRegion.height}` : '500×80'})
                    </span>
                  )}
                </div>
                <p className="text-xs text-slate-400 mt-1">
                  Definiert das Bildschirm-Rechteck, in dem dein Kontostand beim Drücken von F1 im mobiGlas ausgelesen wird.
                </p>
              </div>

              {ocrConfig?.isWalletScanBoxVisible && (
                <span className="flex items-center space-x-1.5 px-2.5 py-1 rounded bg-emerald-500/10 text-emerald-400 border border-emerald-500/30 text-xs font-semibold animate-pulse shrink-0">
                  <span className="w-2 h-2 rounded-full bg-emerald-400"></span>
                  <span>Scan-Rahmen aktiv</span>
                </span>
              )}
            </div>

            {/* Haupt-Aktionsleiste */}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
              <button
                onClick={() => handleSelectRegion('wallet')}
                disabled={isSelectingRegion !== null}
                className="flex items-center justify-center space-x-2 px-4 py-3 rounded-lg bg-sky-600 hover:bg-sky-500 disabled:opacity-50 text-white text-xs font-bold shadow-lg shadow-sky-600/20 border border-sky-400 transition"
              >
                {isSelectingRegion === 'wallet' ? (
                  <>
                    <RefreshCw className="w-4 h-4 animate-spin" />
                    <span>Overlay aktiv...</span>
                  </>
                ) : (
                  <>
                    <Crop className="w-4 h-4" />
                    <span>Bereich am Bildschirm markieren</span>
                  </>
                )}
              </button>

              <button
                onClick={() => handleTestScan('wallet')}
                disabled={isTestingScan !== null}
                className="flex items-center justify-center space-x-2 px-4 py-3 rounded-lg bg-slate-800 hover:bg-slate-700 disabled:opacity-50 text-sky-300 text-xs font-bold border border-sky-500/30 transition shadow-sm"
              >
                {isTestingScan === 'wallet' ? (
                  <>
                    <RefreshCw className="w-4 h-4 animate-spin text-sky-400" />
                    <span>Scanne Bildschirm...</span>
                  </>
                ) : (
                  <>
                    <Zap className="w-4 h-4 text-amber-400" />
                    <span>Test-Scan ausführen</span>
                  </>
                )}
              </button>

              <button
                onClick={() => handleToggleScanBox('wallet')}
                className={`flex items-center justify-center space-x-2 px-4 py-3 rounded-lg text-xs font-bold border transition shadow-sm ${
                  ocrConfig?.isWalletScanBoxVisible
                    ? 'bg-emerald-950/60 hover:bg-emerald-900/80 text-emerald-300 border-emerald-600'
                    : 'bg-slate-800 hover:bg-slate-700 text-slate-200 border-slate-700'
                }`}
              >
                <Eye className="w-4 h-4" />
                <span>
                  {ocrConfig?.isWalletScanBoxVisible
                    ? 'Scan-Rahmen ausblenden'
                    : 'Scan-Rahmen im Spiel anzeigen'}
                </span>
              </button>

              <button
                onClick={() => handleResetRegion('wallet')}
                className="flex items-center justify-center space-x-2 px-4 py-3 rounded-lg bg-slate-800/80 hover:bg-slate-700 text-slate-300 hover:text-white text-xs font-bold border border-slate-700 transition"
              >
                <RotateCcw className="w-4 h-4 text-slate-400" />
                <span>Standard (Auto)</span>
              </button>
            </div>

            {/* Test-Scan Feedback Box */}
            {testScanResult && testScanResult.target === 'wallet' && (
              <div
                className={`p-4 rounded-lg border text-xs space-y-2 animate-in fade-in slide-in-from-top-2 ${
                  testScanResult.success
                    ? 'bg-emerald-950/40 border-emerald-500/40 text-emerald-200'
                    : 'bg-amber-950/40 border-amber-500/40 text-amber-200'
                }`}
              >
                <div className="flex items-center justify-between font-bold">
                  <div className="flex items-center space-x-2">
                    {testScanResult.success ? (
                      <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                    ) : (
                      <AlertTriangle className="w-4 h-4 text-amber-400" />
                    )}
                    <span>
                      {testScanResult.success
                        ? 'Test-Scan erfolgreich abgeschlossen'
                        : 'Test-Scan: Kontostand konnte nicht verifiziert werden'}
                    </span>
                  </div>
                  <span className="font-mono text-[11px] opacity-80">
                    {testScanResult.durationMs} ms
                  </span>
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 text-[11px] font-mono mt-1">
                  <div className="bg-slate-950/60 p-2 rounded border border-slate-800/60">
                    <span className="text-slate-400 block text-[10px]">ERKANNTES ERGEBNIS:</span>
                    <span className="font-bold text-sm text-sky-400">
                      {testScanResult.extractedValue != null
                        ? `${testScanResult.extractedValue.toLocaleString('de-DE')} aUEC`
                        : 'Kein Betrag extrahierbar'}
                    </span>
                  </div>
                  <div className="bg-slate-950/60 p-2 rounded border border-slate-800/60">
                    <span className="text-slate-400 block text-[10px]">ROHTEXT (OCR):</span>
                    <span className="text-slate-200 truncate block">
                      {testScanResult.recognizedText || '—'}
                    </span>
                  </div>
                </div>
                {testScanResult.region && (
                  <div className="text-[10px] text-slate-400">
                    Gescannter Bereich: {testScanResult.region.width}×{testScanResult.region.height} Pixel bei X:{testScanResult.region.x}, Y:{testScanResult.region.y}
                  </div>
                )}
              </div>
            )}

            {/* Schnellauswahl: Bildschirm-Presets */}
            <div className="p-4 rounded-lg bg-slate-950/60 border border-slate-800 space-y-2.5">
              <div className="flex items-center justify-between">
                <span className="text-xs font-bold text-slate-300 flex items-center space-x-1.5">
                  <Sparkles className="w-3.5 h-3.5 text-sky-400" />
                  <span>Auflösungs-Voreinstellungen (Schnellauswahl):</span>
                </span>
                <span className="text-[11px] text-slate-500 font-mono">1-Klick Optimierung</span>
              </div>
              <div className="flex flex-wrap gap-2">
                {[
                  { label: '1080p (Full HD)', x: 1300, y: 415, width: 500, height: 80 },
                  { label: '1440p (WQHD)', x: 1733, y: 553, width: 667, height: 107 },
                  { label: '4K (UHD 2160p)', x: 2600, y: 830, width: 1000, height: 160 },
                  { label: '3440×1440 (21:9 UW)', x: 2173, y: 553, width: 667, height: 107 },
                  { label: '5120×1440 (32:9 SUW)', x: 3013, y: 553, width: 667, height: 107 },
                ].map((preset) => (
                  <button
                    key={preset.label}
                    onClick={() => handleApplyPreset('wallet', preset)}
                    className="px-3 py-1.5 rounded-lg bg-slate-900 hover:bg-slate-800 text-slate-300 hover:text-sky-300 text-xs font-medium border border-slate-700/80 hover:border-sky-500/50 transition"
                  >
                    {preset.label}
                  </button>
                ))}
              </div>
            </div>

            {/* Pixel-Feinjustierung manuell */}
            <div className="p-4 rounded-lg bg-slate-950/40 border border-slate-800/80 space-y-3">
              <div className="text-xs font-bold text-slate-300 flex items-center space-x-1.5">
                <Sliders className="w-3.5 h-3.5 text-slate-400" />
                <span>Pixel-Feinabstimmung (Manuelle Koordinaten):</span>
              </div>
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                <div>
                  <label className="text-[10px] text-slate-400 font-mono block mb-1">X-Position (Links):</label>
                  <input
                    type="number"
                    value={manualWallet.x}
                    onChange={(e) => setManualWallet({ ...manualWallet, x: parseInt(e.target.value) || 0 })}
                    className="w-full bg-slate-900 border border-slate-700 rounded px-3 py-1.5 text-xs text-slate-200 font-mono focus:outline-none focus:border-sky-500"
                  />
                </div>
                <div>
                  <label className="text-[10px] text-slate-400 font-mono block mb-1">Y-Position (Oben):</label>
                  <input
                    type="number"
                    value={manualWallet.y}
                    onChange={(e) => setManualWallet({ ...manualWallet, y: parseInt(e.target.value) || 0 })}
                    className="w-full bg-slate-900 border border-slate-700 rounded px-3 py-1.5 text-xs text-slate-200 font-mono focus:outline-none focus:border-sky-500"
                  />
                </div>
                <div>
                  <label className="text-[10px] text-slate-400 font-mono block mb-1">Breite (Pixel):</label>
                  <input
                    type="number"
                    value={manualWallet.width}
                    onChange={(e) => setManualWallet({ ...manualWallet, width: Math.max(10, parseInt(e.target.value) || 10) })}
                    className="w-full bg-slate-900 border border-slate-700 rounded px-3 py-1.5 text-xs text-slate-200 font-mono focus:outline-none focus:border-sky-500"
                  />
                </div>
                <div>
                  <label className="text-[10px] text-slate-400 font-mono block mb-1">Höhe (Pixel):</label>
                  <input
                    type="number"
                    value={manualWallet.height}
                    onChange={(e) => setManualWallet({ ...manualWallet, height: Math.max(10, parseInt(e.target.value) || 10) })}
                    className="w-full bg-slate-900 border border-slate-700 rounded px-3 py-1.5 text-xs text-slate-200 font-mono focus:outline-none focus:border-sky-500"
                  />
                </div>
              </div>
              <div className="flex justify-end pt-1">
                <button
                  onClick={() => handleApplyManualCoords('wallet', manualWallet)}
                  className="px-4 py-1.5 rounded bg-sky-700 hover:bg-sky-600 text-white text-xs font-semibold shadow border border-sky-500 transition"
                >
                  Koordinaten speichern & anwenden
                </button>
              </div>
            </div>
          </div>

          {/* Sektion 2: RS Signal Radar Scan-Bereich (HUD-Ping) */}
          <div className="p-6 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-5">
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-3">
              <div>
                <div className="flex items-center space-x-2">
                  <h2 className="text-sm font-bold text-cyan-400 flex items-center space-x-2">
                    <Radar className="w-4 h-4" />
                    <span>RS SIGNAL RADAR SCAN-BEREICH (HUD)</span>
                  </h2>
                  {ocrConfig?.rsScanRegion ? (
                    <span className="px-2 py-0.5 text-[10px] font-bold rounded bg-cyan-950 text-cyan-300 border border-cyan-800">
                      BENUTZERDEFINIERT: {ocrConfig.rsScanRegion.width}×{ocrConfig.rsScanRegion.height} @ ({ocrConfig.rsScanRegion.x},{ocrConfig.rsScanRegion.y})
                    </span>
                  ) : (
                    <span className="px-2 py-0.5 text-[10px] font-bold rounded bg-emerald-950/80 text-emerald-300 border border-emerald-800">
                      STANDARD AUTO-ERKENNUNG ({ocrConfig?.defaultRsRegion ? `${ocrConfig.defaultRsRegion.width}×${ocrConfig.defaultRsRegion.height}` : '480×160'})
                    </span>
                  )}
                </div>
                <p className="text-xs text-slate-400 mt-1">
                  Definiert das Bildschirm-Rechteck um das HUD-Fadenkreuz zur optischen Erfassung der RS-Signatur beim Radar-Ping.
                </p>
              </div>
            </div>

            {/* RS Aktionsleiste */}
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
              <button
                onClick={() => handleSelectRegion('rs')}
                disabled={isSelectingRegion !== null}
                className="flex items-center justify-center space-x-2 px-4 py-3 rounded-lg bg-cyan-600 hover:bg-cyan-500 disabled:opacity-50 text-white text-xs font-bold shadow-lg shadow-cyan-600/20 border border-cyan-400 transition"
              >
                {isSelectingRegion === 'rs' ? (
                  <>
                    <RefreshCw className="w-4 h-4 animate-spin" />
                    <span>Overlay aktiv...</span>
                  </>
                ) : (
                  <>
                    <Crop className="w-4 h-4" />
                    <span>Bereich am Bildschirm markieren</span>
                  </>
                )}
              </button>

              <button
                onClick={() => handleTestScan('rs')}
                disabled={isTestingScan !== null}
                className="flex items-center justify-center space-x-2 px-4 py-3 rounded-lg bg-slate-800 hover:bg-slate-700 disabled:opacity-50 text-cyan-300 text-xs font-bold border border-cyan-500/30 transition shadow-sm"
              >
                {isTestingScan === 'rs' ? (
                  <>
                    <RefreshCw className="w-4 h-4 animate-spin text-cyan-400" />
                    <span>Scanne Bildschirm...</span>
                  </>
                ) : (
                  <>
                    <Zap className="w-4 h-4 text-amber-400" />
                    <span>Test-Scan ausführen</span>
                  </>
                )}
              </button>

              <button
                onClick={() => handleResetRegion('rs')}
                className="flex items-center justify-center space-x-2 px-4 py-3 rounded-lg bg-slate-800/80 hover:bg-slate-700 text-slate-300 hover:text-white text-xs font-bold border border-slate-700 transition"
              >
                <RotateCcw className="w-4 h-4 text-slate-400" />
                <span>Standard (Auto)</span>
              </button>
            </div>

            {/* Test-Scan Feedback Box für RS */}
            {testScanResult && testScanResult.target === 'rs' && (
              <div
                className={`p-4 rounded-lg border text-xs space-y-2 animate-in fade-in slide-in-from-top-2 ${
                  testScanResult.success
                    ? 'bg-emerald-950/40 border-emerald-500/40 text-emerald-200'
                    : 'bg-amber-950/40 border-amber-500/40 text-amber-200'
                }`}
              >
                <div className="flex items-center justify-between font-bold">
                  <div className="flex items-center space-x-2">
                    {testScanResult.success ? (
                      <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                    ) : (
                      <AlertTriangle className="w-4 h-4 text-amber-400" />
                    )}
                    <span>
                      {testScanResult.success
                        ? 'Test-Scan erfolgreich abgeschlossen'
                        : 'Test-Scan: RS Signatur konnte nicht verifiziert werden'}
                    </span>
                  </div>
                  <span className="font-mono text-[11px] opacity-80">
                    {testScanResult.durationMs} ms
                  </span>
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 text-[11px] font-mono mt-1">
                  <div className="bg-slate-950/60 p-2 rounded border border-slate-800/60">
                    <span className="text-slate-400 block text-[10px]">ERKANNTES ERGEBNIS:</span>
                    <span className="font-bold text-sm text-cyan-400">
                      {testScanResult.extractedValue != null
                        ? `${testScanResult.extractedValue.toLocaleString('de-DE')} RS`
                        : 'Keine RS-Ziffer extrahierbar'}
                    </span>
                  </div>
                  <div className="bg-slate-950/60 p-2 rounded border border-slate-800/60">
                    <span className="text-slate-400 block text-[10px]">ROHTEXT (OCR):</span>
                    <span className="text-slate-200 truncate block">
                      {testScanResult.recognizedText || '—'}
                    </span>
                  </div>
                </div>
                {testScanResult.region && (
                  <div className="text-[10px] text-slate-400">
                    Gescannter Bereich: {testScanResult.region.width}×{testScanResult.region.height} Pixel bei X:{testScanResult.region.x}, Y:{testScanResult.region.y}
                  </div>
                )}
              </div>
            )}

            {/* Pixel-Feinjustierung manuell RS */}
            <div className="p-4 rounded-lg bg-slate-950/40 border border-slate-800/80 space-y-3">
              <div className="text-xs font-bold text-slate-300 flex items-center space-x-1.5">
                <Sliders className="w-3.5 h-3.5 text-slate-400" />
                <span>Pixel-Feinabstimmung RS (Manuelle Koordinaten):</span>
              </div>
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                <div>
                  <label className="text-[10px] text-slate-400 font-mono block mb-1">X-Position (Links):</label>
                  <input
                    type="number"
                    value={manualRs.x}
                    onChange={(e) => setManualRs({ ...manualRs, x: parseInt(e.target.value) || 0 })}
                    className="w-full bg-slate-900 border border-slate-700 rounded px-3 py-1.5 text-xs text-slate-200 font-mono focus:outline-none focus:border-cyan-500"
                  />
                </div>
                <div>
                  <label className="text-[10px] text-slate-400 font-mono block mb-1">Y-Position (Oben):</label>
                  <input
                    type="number"
                    value={manualRs.y}
                    onChange={(e) => setManualRs({ ...manualRs, y: parseInt(e.target.value) || 0 })}
                    className="w-full bg-slate-900 border border-slate-700 rounded px-3 py-1.5 text-xs text-slate-200 font-mono focus:outline-none focus:border-cyan-500"
                  />
                </div>
                <div>
                  <label className="text-[10px] text-slate-400 font-mono block mb-1">Breite (Pixel):</label>
                  <input
                    type="number"
                    value={manualRs.width}
                    onChange={(e) => setManualRs({ ...manualRs, width: Math.max(10, parseInt(e.target.value) || 10) })}
                    className="w-full bg-slate-900 border border-slate-700 rounded px-3 py-1.5 text-xs text-slate-200 font-mono focus:outline-none focus:border-cyan-500"
                  />
                </div>
                <div>
                  <label className="text-[10px] text-slate-400 font-mono block mb-1">Höhe (Pixel):</label>
                  <input
                    type="number"
                    value={manualRs.height}
                    onChange={(e) => setManualRs({ ...manualRs, height: Math.max(10, parseInt(e.target.value) || 10) })}
                    className="w-full bg-slate-900 border border-slate-700 rounded px-3 py-1.5 text-xs text-slate-200 font-mono focus:outline-none focus:border-cyan-500"
                  />
                </div>
              </div>
              <div className="flex justify-end pt-1">
                <button
                  onClick={() => handleApplyManualCoords('rs', manualRs)}
                  className="px-4 py-1.5 rounded bg-cyan-700 hover:bg-cyan-600 text-white text-xs font-semibold shadow border border-cyan-500 transition"
                >
                  RS-Koordinaten speichern & anwenden
                </button>
              </div>
            </div>
          </div>

          {/* Sektion 3: In-Game Chat Scan-Bereich (F12) */}
          <div className="p-6 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-5">
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-3">
              <div>
                <div className="flex items-center space-x-2">
                  <h2 className="text-sm font-bold text-sky-400 flex items-center space-x-2">
                    <MessageSquare className="w-4 h-4" />
                    <span>IN-GAME CHAT SCAN-BEREICH (F12)</span>
                  </h2>
                  {ocrConfig?.chatRegion ? (
                    <span className="px-2 py-0.5 text-[10px] font-bold rounded bg-sky-950 text-sky-300 border border-sky-800">
                      BENUTZERDEFINIERT: {ocrConfig.chatRegion.width}×{ocrConfig.chatRegion.height} @ ({ocrConfig.chatRegion.x},{ocrConfig.chatRegion.y})
                    </span>
                  ) : (
                    <span className="px-2 py-0.5 text-[10px] font-bold rounded bg-emerald-950/80 text-emerald-300 border border-emerald-800">
                      STANDARD AUTO-ERKENNUNG ({ocrConfig?.defaultChatRegion ? `${ocrConfig.defaultChatRegion.width}×${ocrConfig.defaultChatRegion.height}` : '620×450'})
                    </span>
                  )}
                </div>
                <p className="text-xs text-slate-400 mt-1">
                  Definiert das Bildschirm-Rechteck des Chatfensters (Standard links oben), das der Live-Chat-Scanner erfasst.
                </p>
              </div>
            </div>

            {/* Chat Aktionsleiste */}
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
              <button
                onClick={() => handleSelectRegion('chat')}
                disabled={isSelectingRegion !== null}
                className="flex items-center justify-center space-x-2 px-4 py-3 rounded-lg bg-sky-600 hover:bg-sky-500 disabled:opacity-50 text-white text-xs font-bold shadow-lg shadow-sky-600/20 border border-sky-400 transition"
              >
                {isSelectingRegion === 'chat' ? (
                  <>
                    <RefreshCw className="w-4 h-4 animate-spin" />
                    <span>Overlay aktiv...</span>
                  </>
                ) : (
                  <>
                    <Crop className="w-4 h-4" />
                    <span>Bereich am Bildschirm markieren</span>
                  </>
                )}
              </button>

              <button
                onClick={() => handleTestScan('chat')}
                disabled={isTestingScan !== null}
                className="flex items-center justify-center space-x-2 px-4 py-3 rounded-lg bg-slate-800 hover:bg-slate-700 disabled:opacity-50 text-sky-300 text-xs font-bold border border-sky-500/30 transition shadow-sm"
              >
                {isTestingScan === 'chat' ? (
                  <>
                    <RefreshCw className="w-4 h-4 animate-spin text-sky-400" />
                    <span>Scanne Bildschirm...</span>
                  </>
                ) : (
                  <>
                    <Zap className="w-4 h-4 text-amber-400" />
                    <span>Test-Scan ausführen</span>
                  </>
                )}
              </button>

              <button
                onClick={() => handleResetRegion('chat')}
                className="flex items-center justify-center space-x-2 px-4 py-3 rounded-lg bg-slate-800/80 hover:bg-slate-700 text-slate-300 hover:text-white text-xs font-bold border border-slate-700 transition"
              >
                <RotateCcw className="w-4 h-4 text-slate-400" />
                <span>Standard (Auto)</span>
              </button>
            </div>

            {/* Test-Scan Feedback Box für Chat */}
            {testScanResult && testScanResult.target === 'chat' && (
              <div
                className={`p-4 rounded-lg border text-xs space-y-2 animate-in fade-in slide-in-from-top-2 ${
                  testScanResult.success
                    ? 'bg-emerald-950/40 border-emerald-500/40 text-emerald-200'
                    : 'bg-amber-950/40 border-amber-500/40 text-amber-200'
                }`}
              >
                <div className="flex items-center justify-between font-bold">
                  <div className="flex items-center space-x-2">
                    {testScanResult.success ? (
                      <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                    ) : (
                      <AlertTriangle className="w-4 h-4 text-amber-400" />
                    )}
                    <span>
                      {testScanResult.success
                        ? 'Test-Scan erfolgreich abgeschlossen'
                        : 'Test-Scan: Kein Chat-Text erkannt'}
                    </span>
                  </div>
                  <span className="font-mono text-[11px] opacity-80">
                    {testScanResult.durationMs} ms
                  </span>
                </div>
                <div className="bg-slate-950/60 p-2.5 rounded border border-slate-800/60 font-mono text-[11px]">
                  <span className="text-slate-400 block text-[10px]">ERKANNTES TEXTFRAGMENT:</span>
                  <div className="text-slate-200 max-h-24 overflow-y-auto whitespace-pre-wrap mt-0.5">
                    {testScanResult.recognizedText || '—'}
                  </div>
                </div>
                {testScanResult.region && (
                  <div className="text-[10px] text-slate-400">
                    Gescannter Bereich: {testScanResult.region.width}×{testScanResult.region.height} Pixel bei X:{testScanResult.region.x}, Y:{testScanResult.region.y}
                  </div>
                )}
              </div>
            )}

            {/* Pixel-Feinjustierung manuell Chat */}
            <div className="p-4 rounded-lg bg-slate-950/40 border border-slate-800/80 space-y-3">
              <div className="text-xs font-bold text-slate-300 flex items-center space-x-1.5">
                <Sliders className="w-3.5 h-3.5 text-slate-400" />
                <span>Pixel-Feinabstimmung Chat (Manuelle Koordinaten):</span>
              </div>
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                <div>
                  <label className="text-[10px] text-slate-400 font-mono block mb-1">X-Position (Links):</label>
                  <input
                    type="number"
                    value={manualChat.x}
                    onChange={(e) => setManualChat({ ...manualChat, x: parseInt(e.target.value) || 0 })}
                    className="w-full bg-slate-900 border border-slate-700 rounded px-3 py-1.5 text-xs text-slate-200 font-mono focus:outline-none focus:border-sky-500"
                  />
                </div>
                <div>
                  <label className="text-[10px] text-slate-400 font-mono block mb-1">Y-Position (Oben):</label>
                  <input
                    type="number"
                    value={manualChat.y}
                    onChange={(e) => setManualChat({ ...manualChat, y: parseInt(e.target.value) || 0 })}
                    className="w-full bg-slate-900 border border-slate-700 rounded px-3 py-1.5 text-xs text-slate-200 font-mono focus:outline-none focus:border-sky-500"
                  />
                </div>
                <div>
                  <label className="text-[10px] text-slate-400 font-mono block mb-1">Breite (Pixel):</label>
                  <input
                    type="number"
                    value={manualChat.width}
                    onChange={(e) => setManualChat({ ...manualChat, width: Math.max(10, parseInt(e.target.value) || 10) })}
                    className="w-full bg-slate-900 border border-slate-700 rounded px-3 py-1.5 text-xs text-slate-200 font-mono focus:outline-none focus:border-sky-500"
                  />
                </div>
                <div>
                  <label className="text-[10px] text-slate-400 font-mono block mb-1">Höhe (Pixel):</label>
                  <input
                    type="number"
                    value={manualChat.height}
                    onChange={(e) => setManualChat({ ...manualChat, height: Math.max(10, parseInt(e.target.value) || 10) })}
                    className="w-full bg-slate-900 border border-slate-700 rounded px-3 py-1.5 text-xs text-slate-200 font-mono focus:outline-none focus:border-sky-500"
                  />
                </div>
              </div>
              <div className="flex justify-end pt-1">
                <button
                  onClick={() => handleApplyManualCoords('chat', manualChat)}
                  className="px-4 py-1.5 rounded bg-sky-700 hover:bg-sky-600 text-white text-xs font-semibold shadow border border-sky-500 transition"
                >
                  Chat-Koordinaten speichern & anwenden
                </button>
              </div>
            </div>
          </div>

          {/* Sektion 4: Aktive Aufträge & Missions-Tracking (100% Game.log & SQLite) */}
          <div className="p-6 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-5">
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-3">
              <div>
                <h2 className="text-sm font-bold text-sky-400 flex items-center space-x-2">
                  <FileText className="w-4 h-4" />
                  <span>❖  AKTIVE AUFTRÄGE & MISSIONS-TRACKING</span>
                </h2>
                <p className="text-xs text-slate-400 mt-1">
                  Missionsdaten, Belohnungen und Baupläne werden 100% automatisch aus dem Game.log und der SQLite-Master-Datenbank abgeglichen (Auftrags-OCR wurde dauerhaft deaktiviert, da Star Citizen alle Vertragsstatus nativ im Log protokolliert).
                </p>
              </div>

              <button
                onClick={handleClearContracts}
                className="px-3.5 py-2 rounded-lg bg-rose-950/40 hover:bg-rose-900/60 text-rose-300 text-xs font-semibold border border-rose-800/80 transition cursor-pointer shrink-0"
                title="Aktive Auftragsliste leeren"
              >
                ✕ Aufträge leeren
              </button>
            </div>

            <div className="p-4 rounded-xl bg-slate-950 border border-slate-800/80 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
              <div className="space-y-1">
                <div className="text-xs font-bold text-slate-200">
                  Game.log Notification Parser & Master-DB
                </div>
                <div className="text-[11px] text-slate-400">
                  Erkennt automatisch <code className="text-sky-300">Contract Accepted</code>, <code className="text-emerald-300">Contract Complete</code> und <code className="text-rose-300">Contract Failed</code> mit MissionId direkt aus Star Citizen.
                </div>
              </div>
              <span className="px-3 py-1 rounded-full bg-emerald-950 text-emerald-300 border border-emerald-800 text-xs font-bold flex items-center space-x-1.5 shrink-0">
                <span className="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
                <span>⚡ Auto-Sync aktiv</span>
              </span>
            </div>
          </div>

          {/* Sektion 3: Auto-OCR Wächter & Schutzmechanismen */}
          <div className="p-6 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-5">
            <div>
              <h2 className="text-sm font-bold text-sky-400 flex items-center space-x-2">
                <Shield className="w-4 h-4" />
                <span>AUTO-OCR WÄCHTER & SCHUTZMECHANISMEN</span>
              </h2>
              <p className="text-xs text-slate-400 mt-1">
                Hintergrund-Synchronisation und NexusApp-Sicherheitslayer gegen Fehlscans.
              </p>
            </div>

            <div className="space-y-4">
              <label className="flex items-center space-x-3 cursor-pointer p-3 rounded-lg bg-slate-950/60 border border-slate-800">
                <input
                  type="checkbox"
                  checked={settings.autoOcrEnabled}
                  onChange={(e) => setSettings({ ...settings, autoOcrEnabled: e.target.checked })}
                  className="w-4 h-4 rounded border-slate-700 text-sky-600 focus:ring-sky-500 bg-slate-800"
                />
                <div>
                  <div className="text-xs font-semibold text-white">Auto-OCR Wächter aktiv</div>
                  <div className="text-[11px] text-slate-400">
                    Scannt beim Öffnen des mobiGlas (F1) vollautomatisch den Kontostand und aktualisiert deinen Saldo
                  </div>
                </div>
              </label>

              <div className="p-4 rounded-lg bg-slate-950/60 border border-slate-800 space-y-3">
                <div className="text-xs font-semibold text-slate-200">
                  Dual-Read & Cross-Grab Schutzmechanismus
                </div>
                <div className="text-xs text-slate-400 leading-relaxed space-y-1.5">
                  <div className="flex items-center space-x-2 text-emerald-400">
                    <Check className="w-4 h-4 shrink-0" />
                    <span>Cross-Grab Bestätigung: Derselbe Wert muss mindestens 2× in einer Burst-Sequenz übereinstimmen</span>
                  </div>
                  <div className="flex items-center space-x-2 text-sky-400">
                    <Check className="w-4 h-4 shrink-0" />
                    <span>Adaptive Schwellenwert-Invertierung & Kontrast-Filter für kristallklare Textextraktion</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Tab 4: Aurora & Audio */}
      {activeSubTab === 'audio' && (
        <div className="space-y-6">
          <div className="p-6 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-5">
            <div>
              <h2 className="text-sm font-bold text-sky-400 flex items-center space-x-2">
                <Volume2 className="w-4 h-4" />
                <span>AURORA SPRACHASSISTENT & VOICE INTEGRATION</span>
              </h2>
              <p className="text-xs text-slate-400 mt-1">
                Akustisches Co-Piloten-Feedback für Quantensprünge, Missionsbelohnungen und Gefahren.
              </p>
            </div>

            <div className="space-y-4">
              <label className="flex items-center space-x-3 cursor-pointer p-3 rounded-lg bg-slate-950/60 border border-slate-800">
                <input
                  type="checkbox"
                  checked={settings.auroraIntegrationEnabled}
                  onChange={(e) =>
                    setSettings({ ...settings, auroraIntegrationEnabled: e.target.checked })
                  }
                  className="w-4 h-4 rounded border-slate-700 text-sky-600 focus:ring-sky-500 bg-slate-800"
                />
                <div>
                  <div className="text-xs font-semibold text-white">Aurora Sprachausgabe aktivieren</div>
                  <div className="text-[11px] text-slate-400">
                    Sprachmeldungen über Windows SAPI / Neural TTS abspielen
                  </div>
                </div>
              </label>

              <div className="p-4 rounded-lg bg-slate-950/60 border border-slate-800 space-y-2">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-semibold text-slate-300">Aurora Lautstärke:</span>
                  <span className="text-xs font-mono text-sky-400 font-bold">
                    {settings.auroraVolume}%
                  </span>
                </div>
                <input
                  type="range"
                  min="0"
                  max="100"
                  step="5"
                  value={settings.auroraVolume}
                  onChange={(e) =>
                    setSettings({ ...settings, auroraVolume: parseInt(e.target.value, 10) })
                  }
                  className="w-full accent-sky-500 cursor-pointer"
                />
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-xs">
                <label className="flex items-start space-x-3 p-3 rounded-lg bg-slate-950/60 border border-slate-800 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={settings.rsTargetAlertEnabled}
                    onChange={(e) =>
                      setSettings({ ...settings, rsTargetAlertEnabled: e.target.checked })
                    }
                    className="mt-0.5 w-4 h-4 rounded border-slate-700 text-sky-600 focus:ring-sky-500 bg-slate-800"
                  />
                  <div>
                    <div className="font-semibold text-white">RS Target Alert</div>
                    <div className="text-[11px] text-slate-400">
                      Warnung bei Identifikation wertvoller Erz-Cluster (Quantainium, Bexalite)
                    </div>
                  </div>
                </label>

                <label className="flex items-start space-x-3 p-3 rounded-lg bg-slate-950/60 border border-slate-800 cursor-pointer">
                  <input
                    type="checkbox"
                    checked={settings.rsTargetSoundEnabled}
                    onChange={(e) =>
                      setSettings({ ...settings, rsTargetSoundEnabled: e.target.checked })
                    }
                    className="mt-0.5 w-4 h-4 rounded border-slate-700 text-sky-600 focus:ring-sky-500 bg-slate-800"
                  />
                  <div>
                    <div className="font-semibold text-white">Radar Sonar-Ping Sound</div>
                    <div className="text-[11px] text-slate-400">
                      Akustischer Ping-Ton bei erfolgreichem Radar-Scan
                    </div>
                  </div>
                </label>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Tab 5: UEX Corp */}
      {activeSubTab === 'uex' && (
        <div className="space-y-6">
          <div className="p-6 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-5">
            <div>
              <h2 className="text-sm font-bold text-sky-400 flex items-center space-x-2">
                <Globe className="w-4 h-4" />
                <span>UEX CORP API-SCHLÜSSEL & INTEGRATION</span>
              </h2>
              <p className="text-xs text-slate-400 mt-1">
                Ermöglicht Live-Warenpreise, Handelsrouten und Marktvolumina von UEXCorp.space abzurufen.
              </p>
            </div>

            <div className="space-y-3 max-w-xl">
              <label className="text-xs font-semibold text-slate-300">Dein UEX Corp API-Token:</label>
              <input
                type="password"
                value={settings.uexApiKey || ''}
                onChange={(e) => setSettings({ ...settings, uexApiKey: e.target.value })}
                placeholder="uex_token_..."
                className="w-full bg-slate-950 border border-slate-700 rounded-lg px-4 py-2 text-xs text-slate-200 font-mono focus:outline-none focus:border-sky-500"
              />
              <p className="text-[11px] text-slate-500">
                Erhältlich in deinen UEX Corp Kontoeinstellungen unter "API Keys". Ohne Key werden gecachte Community-Preise verwendet.
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Tab 6: SQLite & Datenbank */}
      {activeSubTab === 'database' && (
        <div className="space-y-6">
          <div className="p-6 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-5">
            <div>
              <h2 className="text-sm font-bold text-sky-400 flex items-center space-x-2">
                <Database className="w-4 h-4" />
                <span>SQLITE DATENBANK-VERWALTUNG &amp; WARTUNG</span>
              </h2>
              <p className="text-xs text-slate-400 mt-1">
                Verwalte das lokale SQLite-Archiv, starte einen vollständigen Re-Scan aller Logs oder bereinige die Datenbank.
              </p>
            </div>

            {/* Status-Cards */}
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4 text-xs font-mono">
              <div className="p-3.5 rounded-lg bg-slate-950 border border-slate-800">
                <div className="text-slate-400">Schema Version:</div>
                <div className="text-sm font-bold text-sky-400 mt-0.5">
                  v{dbDiag?.installedSchemaVersion ?? 18} (App: v{dbDiag?.currentSchemaVersion ?? 18})
                </div>
              </div>
              <div className="p-3.5 rounded-lg bg-slate-950 border border-slate-800">
                <div className="text-slate-400">Parser Version:</div>
                <div className="text-sm font-bold text-emerald-400 mt-0.5">
                  v{dbDiag?.installedParserVersion ?? 34} (Engine: v{dbDiag?.currentParserVersion ?? 34})
                </div>
              </div>
              <div className="p-3.5 rounded-lg bg-slate-950 border border-slate-800">
                <div className="text-slate-400">Integrität:</div>
                <div className="text-sm font-bold text-emerald-400 mt-0.5">
                  {dbDiag?.integrityCheckOk ? 'OK (quick_check)' : 'Fehler'}
                </div>
              </div>
              <div className="p-3.5 rounded-lg bg-slate-950 border border-slate-800">
                <div className="text-slate-400">Dateigröße:</div>
                <div className="text-sm font-bold text-sky-300 mt-0.5">
                  {dbDiag?.formattedSize || 'sessions.db'}
                </div>
              </div>
            </div>

            {/* Versions-Hinweis falls Schema oder Parser nicht synchron */}
            {dbDiag && !dbDiag.isSynchronous && (
              <div className="rounded-xl p-4 border border-amber-500/60 bg-amber-950/40 text-amber-200 flex items-center justify-between shadow-xl shadow-amber-950/50 animate-in fade-in">
                <div className="flex items-center space-x-3">
                  <span className="text-xl">⚠️</span>
                  <div>
                    <div className="font-bold text-xs uppercase tracking-wider text-amber-300">
                      Datenbank-Aktualisierung empfohlen
                    </div>
                    <div className="text-xs text-slate-300 mt-0.5 font-mono">
                      Schema (v{dbDiag.installedSchemaVersion} vs v{dbDiag.currentSchemaVersion}) oder Parser (v{dbDiag.installedParserVersion} vs v{dbDiag.currentParserVersion}) weicht ab. Ein Re-Scan aktualisiert alle Tabellen.
                    </div>
                  </div>
                </div>
                <button
                  onClick={handleRescanAll}
                  disabled={isRescanning || isCheckingDb}
                  className="px-3.5 py-1.5 rounded-lg bg-amber-600 hover:bg-amber-500 text-white text-xs font-bold transition shrink-0 cursor-pointer disabled:opacity-50"
                >
                  Jetzt neu einlesen
                </button>
              </div>
            )}

            {/* Laufender Vorgang Banner (Integritätsprüfung, VACUUM, Reparatur) */}
            {isCheckingDb && activeDbOp !== 'none' && activeDbOp !== 'rescan' && (
              <div className="rounded-xl p-4 border border-cyan-500/60 bg-cyan-950/40 text-cyan-200 flex items-center space-x-3.5 shadow-xl shadow-cyan-950/50 animate-pulse font-sans">
                <RefreshCw className="w-5 h-5 text-cyan-400 animate-spin shrink-0" />
                <div className="flex-1 min-w-0">
                  <div className="font-bold text-xs uppercase tracking-wider text-cyan-300">
                    {activeDbOp === 'integrity'
                      ? 'SQLite Integritätsprüfung läuft...'
                      : activeDbOp === 'vacuum'
                      ? 'SQLite VACUUM & Bereinigung läuft...'
                      : 'Datenbank-Struktur Reparatur läuft...'}
                  </div>
                  <div className="text-xs text-slate-300 mt-0.5 font-mono">
                    {dbOpMessage || 'Bitte warten, die Operation wird auf der SQLite-Datenbank ausgeführt...'}
                  </div>
                </div>
              </div>
            )}

            {/* Re-Scan Fortschritts-Banner */}
            {isRescanning && (
              <div className="rounded-xl p-4 border border-cyan-500/60 bg-cyan-950/40 space-y-2.5 animate-in fade-in font-sans shadow-xl shadow-cyan-950/50">
                <div className="flex items-center justify-between text-xs font-mono">
                  <div className="flex items-center space-x-2.5 text-cyan-300 min-w-0">
                    <RefreshCw className="w-4 h-4 animate-spin text-cyan-400 shrink-0" />
                    <span className="font-bold text-white shrink-0 uppercase tracking-wider">
                      {rescanProgress && rescanProgress.total > 0
                        ? `Indexiere Logs (${rescanProgress.current}/${rescanProgress.total}):`
                        : 'Starte Re-Scan:'}
                    </span>
                    <span className="text-cyan-300 truncate font-sans">
                      {rescanProgress?.currentFileName || 'Sammle Log-Dateien für Re-Scan...'}
                    </span>
                  </div>
                  <span className="font-bold text-cyan-400 shrink-0 ml-2">
                    {rescanProgress && rescanProgress.total > 0 ? `${rescanProgress.percent}%` : 'Sammle...'}
                  </span>
                </div>
                <div className="w-full bg-slate-900 rounded-full h-2.5 overflow-hidden border border-cyan-900/60">
                  <div
                    className="bg-gradient-to-r from-cyan-500 to-sky-400 h-full transition-all duration-300 shadow-[0_0_10px_rgba(6,182,212,0.8)]"
                    style={{
                      width: rescanProgress && rescanProgress.total > 0 ? `${Math.max(3, rescanProgress.percent)}%` : '20%',
                    }}
                  />
                </div>
              </div>
            )}

            {/* Abschluss- / Ergebnis-Banner */}
            {dbCompletionBanner && !isCheckingDb && !isRescanning && (
              <div
                className={`p-4 rounded-xl border flex items-center justify-between space-x-3 shadow-lg animate-in fade-in font-sans ${
                  dbCompletionBanner.success
                    ? 'bg-emerald-950/40 border-emerald-500/50 text-emerald-200'
                    : 'bg-rose-950/40 border-rose-500/50 text-rose-200'
                }`}
              >
                <div className="flex items-center space-x-3 min-w-0">
                  <CheckCircle2
                    className={`w-5 h-5 shrink-0 ${
                      dbCompletionBanner.success ? 'text-emerald-400' : 'text-rose-400'
                    }`}
                  />
                  <div>
                    <div
                      className={`font-bold text-xs uppercase tracking-wider ${
                        dbCompletionBanner.success ? 'text-emerald-300' : 'text-rose-300'
                      }`}
                    >
                      {dbCompletionBanner.title}
                    </div>
                    <div className="text-xs text-slate-300 font-mono mt-0.5">{dbCompletionBanner.details}</div>
                  </div>
                </div>
                <button
                  onClick={() => setDbCompletionBanner(null)}
                  className="text-slate-400 hover:text-white text-xs px-2.5 py-1 rounded bg-slate-900/60 border border-slate-800 cursor-pointer"
                >
                  ✕
                </button>
              </div>
            )}

            {/* 3 Haupt-Wartungskarten (wie in RC2) */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 pt-2">
              {/* Karte 1: Re-Scan */}
              <div className="p-4 rounded-xl bg-slate-950 border border-cyan-900/40 flex flex-col justify-between space-y-3 shadow-md">
                <div className="space-y-1.5">
                  <div className="text-xs font-bold text-white uppercase tracking-wider flex items-center gap-1.5">
                    <RefreshCw className="w-3.5 h-3.5 text-cyan-400" />
                    <span>Kompletter Re-Scan</span>
                  </div>
                  <p className="text-[11px] text-slate-400 leading-relaxed">
                    Liest alle Logs frisch mit neuen Parser-Regeln ein. Re-indexiert alle Session-Archive und Backups.
                  </p>
                </div>
                <button
                  onClick={handleRescanAll}
                  disabled={isRescanning || isCheckingDb}
                  className="w-full flex items-center justify-center space-x-2 px-4 py-2.5 rounded-lg bg-cyan-600 hover:bg-cyan-500 text-white text-xs font-bold shadow-lg shadow-cyan-600/20 border border-cyan-400 transition disabled:opacity-50 cursor-pointer"
                >
                  <RefreshCw className={`w-3.5 h-3.5 ${isRescanning ? 'animate-spin' : ''}`} />
                  <span>{isRescanning ? 'Lese Logs ein...' : '🔄 Alle Logs neu einlesen'}</span>
                </button>
              </div>

              {/* Karte 2: Cleanup & VACUUM */}
              <div className="p-4 rounded-xl bg-slate-950 border border-slate-800 flex flex-col justify-between space-y-3 shadow-md">
                <div className="space-y-1.5">
                  <div className="text-xs font-bold text-white uppercase tracking-wider flex items-center gap-1.5">
                    <span>🧹 Datenbank-Bereinigung</span>
                  </div>
                  <p className="text-[11px] text-slate-400 leading-relaxed">
                    Entfernt verwaiste Datensätze, bereinigt doppelte Einträge und führt SQLite VACUUM aus.
                  </p>
                </div>
                <button
                  onClick={handleVacuum}
                  disabled={isRescanning || isCheckingDb}
                  className="w-full flex items-center justify-center space-x-2 px-4 py-2.5 rounded-lg bg-slate-900 hover:bg-slate-800 text-slate-200 text-xs font-semibold border border-slate-700 transition disabled:opacity-50 cursor-pointer"
                >
                  <RefreshCw className={`w-3.5 h-3.5 ${activeDbOp === 'vacuum' ? 'animate-spin text-cyan-400' : ''}`} />
                  <span>{activeDbOp === 'vacuum' ? 'Bereinige...' : '🧹 DB bereinigen & VACUUM'}</span>
                </button>
              </div>

              {/* Karte 3: Reset */}
              <div className="p-4 rounded-xl bg-slate-950 border border-rose-900/30 flex flex-col justify-between space-y-3 shadow-md">
                <div className="space-y-1.5">
                  <div className="text-xs font-bold text-rose-400 uppercase tracking-wider flex items-center gap-1.5">
                    <span>✕ Datenbank leeren</span>
                  </div>
                  <p className="text-[11px] text-slate-400 leading-relaxed">
                    Setzt alle indexierten Sessions, Finanzdaten und Lagerbestände komplett zurück.
                  </p>
                </div>
                <button
                  onClick={handleResetDb}
                  disabled={isRescanning || isCheckingDb}
                  className="w-full flex items-center justify-center space-x-2 px-4 py-2.5 rounded-lg bg-rose-950/40 hover:bg-rose-900/60 text-rose-300 text-xs font-semibold border border-rose-800/80 transition disabled:opacity-50 cursor-pointer"
                >
                  <span>✕ Datenbank zurücksetzen</span>
                </button>
              </div>
            </div>

            {/* Diagnose- & Ordner-Aktionen */}
            <div className="flex flex-wrap gap-2.5 pt-3 border-t border-slate-800/80">
              <button
                onClick={() => loadDbDiag(true)}
                disabled={isCheckingDb || isRescanning}
                className="flex items-center gap-1.5 px-3.5 py-2 rounded-lg bg-slate-900 hover:bg-slate-800 text-slate-300 text-xs font-medium border border-slate-700 transition cursor-pointer disabled:opacity-50"
              >
                <RefreshCw className={`w-3.5 h-3.5 ${activeDbOp === 'integrity' ? 'animate-spin text-cyan-400' : ''}`} />
                <span>{activeDbOp === 'integrity' ? 'Prüfe Integrität...' : '🔍 Tiefe Integritätsprüfung'}</span>
              </button>
              <button
                onClick={handleRepairDb}
                disabled={isCheckingDb || isRescanning}
                className="flex items-center gap-1.5 px-3.5 py-2 rounded-lg bg-cyan-950/60 hover:bg-cyan-900/80 text-cyan-300 text-xs font-medium border border-cyan-800 transition cursor-pointer disabled:opacity-50"
              >
                <RefreshCw className={`w-3.5 h-3.5 ${activeDbOp === 'repair' ? 'animate-spin text-cyan-400' : ''}`} />
                <span>{activeDbOp === 'repair' ? 'Repariere Struktur...' : '⚡ Struktur & Indizes reparieren'}</span>
              </button>
              <button
                onClick={() => handleOpenFolder('db')}
                className="flex items-center gap-1.5 px-3.5 py-2 rounded-lg bg-slate-900 hover:bg-slate-800 text-slate-300 text-xs font-medium border border-slate-700 transition cursor-pointer"
              >
                <Folder className="w-3.5 h-3.5 text-amber-400" />
                <span>📂 sessions.db im Explorer</span>
              </button>
              <button
                onClick={() => handleOpenFolder('debug_log')}
                className="flex items-center gap-1.5 px-3.5 py-2 rounded-lg bg-slate-900 hover:bg-slate-800 text-slate-300 text-xs font-medium border border-slate-700 transition cursor-pointer"
              >
                <FileText className="w-3.5 h-3.5 text-slate-400" />
                <span>📄 Debug-Log öffnen</span>
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Tab 8: Entwickler & Debug Tools (Nur sichtbar, wenn debugMode aktiv) */}
      {activeSubTab === 'developer' && settings.debugMode && (
        <div className="space-y-6">
          <div className="p-6 rounded-xl bg-slate-900/60 border border-amber-900/40 space-y-5">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-sm font-bold text-amber-400 flex items-center space-x-2">
                  <Sparkles className="w-4 h-4" />
                  <span>STAR CITIZEN LIVE-EVENTS SIMULIEREN</span>
                </h2>
                <p className="text-xs text-slate-400 mt-1">
                  Injiziert simulierte Star Citizen Game.log-Events zur Prüfung von Audioausgaben, Muting, Cooldowns und Benachrichtigungen.
                </p>
              </div>
              <span className="px-2.5 py-1 rounded bg-amber-950 text-amber-400 border border-amber-800 text-[10px] font-bold">
                DEBUG ONLY
              </span>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-3">
              <button
                onClick={() => handleSimulate('armistice_enter')}
                className="p-3 rounded-lg bg-slate-800/80 hover:bg-slate-700/80 border border-slate-700 text-left transition flex items-center space-x-2.5 text-xs text-slate-200 hover:text-white cursor-pointer"
              >
                <span className="text-base">🛡️</span>
                <span>Schutzzone betreten</span>
              </button>

              <button
                onClick={() => handleSimulate('armistice_leave')}
                className="p-3 rounded-lg bg-slate-800/80 hover:bg-slate-700/80 border border-slate-700 text-left transition flex items-center space-x-2.5 text-xs text-slate-200 hover:text-white cursor-pointer"
              >
                <span className="text-base">⚔️</span>
                <span>Schutzzone verlassen</span>
              </button>

              <button
                onClick={() => handleSimulate('ship_join', 'Drake Cutlass Black')}
                className="p-3 rounded-lg bg-slate-800/80 hover:bg-slate-700/80 border border-slate-700 text-left transition flex items-center space-x-2.5 text-xs text-slate-200 hover:text-white cursor-pointer"
              >
                <span className="text-base">🚀</span>
                <span>Schiffseinstieg (Cutlass)</span>
              </button>

              <button
                onClick={() => handleSimulate('blueprint_found')}
                className="p-3 rounded-lg bg-slate-800/80 hover:bg-slate-700/80 border border-slate-700 text-left transition flex items-center space-x-2.5 text-xs text-slate-200 hover:text-white cursor-pointer"
              >
                <span className="text-base">📜</span>
                <span>Bauplan erlernt (Toast)</span>
              </button>

              <button
                onClick={() => handleSimulate('quantum_arrival')}
                className="p-3 rounded-lg bg-slate-800/80 hover:bg-slate-700/80 border border-slate-700 text-left transition flex items-center space-x-2.5 text-xs text-slate-200 hover:text-white cursor-pointer"
              >
                <span className="text-base">🌌</span>
                <span>Quantensprung Ankunft</span>
              </button>

              <button
                onClick={() => handleSimulate('server_error')}
                className="p-3 rounded-lg bg-slate-800/80 hover:bg-slate-700/80 border border-slate-700 text-left transition flex items-center space-x-2.5 text-xs text-rose-300 hover:text-rose-200 cursor-pointer"
              >
                <span className="text-base">⚠️</span>
                <span>30k Serverfehler</span>
              </button>

              <button
                onClick={() => handleSimulate('player_death')}
                className="p-3 rounded-lg bg-slate-800/80 hover:bg-slate-700/80 border border-slate-700 text-left transition flex items-center space-x-2.5 text-xs text-rose-300 hover:text-rose-200 cursor-pointer"
              >
                <span className="text-base">💀</span>
                <span>Notfall / Spielertod</span>
              </button>
            </div>
          </div>

          {/* System-Diagnose & Logging */}
          <div className="p-6 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-4">
            <h2 className="text-sm font-bold text-sky-400 flex items-center space-x-2">
              <FileText className="w-4 h-4" />
              <span>SYSTEM-DIAGNOSE & PROTOKOLLE</span>
            </h2>
            <p className="text-xs text-slate-400">
              Schreibe den vollständigen internen Telemetrie- und Modul-Status in <code>%APPDATA%\SCLogMate\SCLogMate.debug.log</code> oder setze die Datei zurück.
            </p>

            <div className="flex items-center space-x-3 pt-2">
              <button
                onClick={handleDumpDebugState}
                className="px-4 py-2.5 rounded-lg bg-sky-600/20 hover:bg-sky-600/30 text-sky-300 text-xs font-semibold border border-sky-500/40 transition flex items-center space-x-2 cursor-pointer"
              >
                <FileText className="w-3.5 h-3.5" />
                <span>Debug-Status in Log schreiben</span>
              </button>
              <button
                onClick={handleClearDebugLog}
                className="px-4 py-2.5 rounded-lg bg-rose-950/40 hover:bg-rose-900/60 text-rose-300 text-xs font-semibold border border-rose-800/60 transition flex items-center space-x-2 cursor-pointer"
              >
                <RotateCcw className="w-3.5 h-3.5" />
                <span>Debug-Logdatei leeren</span>
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
