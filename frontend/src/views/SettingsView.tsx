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
  Sliders,
  Radio,
  FileCheck,
  Shield,
  FileText,
} from 'lucide-react';
import { bridge, SettingsDto } from '../services/photinoBridge';

export const SettingsView: React.FC = () => {
  const [activeSubTab, setActiveSubTab] = useState<
    'general' | 'hud' | 'ocr' | 'audio' | 'uex' | 'database'
  >('general');
  const [settings, setSettings] = useState<SettingsDto>({
    logPath: 'J:\\StarCitizen\\LIVE\\logbackups\\game.log',
    balance: 15420800,
    autoOcrEnabled: true,
    uexApiKey: '',
    overlayEnabled: true,
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
  });
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  const [dbDiag, setDbDiag] = useState<any>(null);
  const [isCheckingDb, setIsCheckingDb] = useState(false);
  const [isRescanning, setIsRescanning] = useState(false);
  const [rescanProgress, setRescanProgress] = useState<any>(null);
  const [rescanStatusMessage, setRescanStatusMessage] = useState<string | null>(null);

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3500);
  };

  const handleRescanAll = async () => {
    if (isRescanning || isCheckingDb) return;
    try {
      setIsRescanning(true);
      setRescanStatusMessage('Sammle Log-Dateien für kompletten Re-Scan...');
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
      await bridge.sendRequest('reset_database');
      showToast('Datenbank erfolgreich zurückgesetzt.');
      loadDbDiag();
    } catch (err) {
      showToast('Fehler beim Zurücksetzen der Datenbank');
    } finally {
      setIsCheckingDb(false);
    }
  };

  const handleOpenFolder = (target: string) => {
    bridge.send('open_folder', { target });
  };

  const loadSettings = async () => {
    try {
      setLoading(true);
      const data = await bridge.send<SettingsDto>('get_settings');
      if (data) {
        setSettings(data);
      }
    } catch (err) {
      console.error('Failed to load settings:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadDbDiag = async () => {
    try {
      setIsCheckingDb(true);
      const diag = await bridge.sendRequest<any>('get_db_diagnostics');
      if (diag) setDbDiag(diag);
    } catch (err) {
      console.error('Failed to load db diagnostics:', err);
    } finally {
      setIsCheckingDb(false);
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

  const handleVacuum = async () => {
    try {
      setIsCheckingDb(true);
      const res = await bridge.sendRequest<any>('cleanup_database');
      showToast(`SQLite VACUUM abgeschlossen! ${res?.cleanedEvents ?? 0} Events bereinigt (${res?.sizeBefore} → ${res?.sizeAfter})`);
      loadDbDiag();
    } catch (err) {
      showToast('Fehler beim VACUUM');
    } finally {
      setIsCheckingDb(false);
    }
  };

  const handleRepairDb = async () => {
    try {
      setIsCheckingDb(true);
      const res = await bridge.sendRequest<any>('repair_db_structure');
      if (res?.diagnostics) setDbDiag(res.diagnostics);
      showToast('Datenbank-Struktur und Indizes erfolgreich aktualisiert');
    } catch (err) {
      showToast('Fehler bei der Reparatur');
    } finally {
      setIsCheckingDb(false);
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
  }, []);

  useEffect(() => {
    const unbind = bridge.on<any>('SCAN_PROGRESS', (progress) => {
      setRescanProgress(progress);
      if (progress.isCompleted) {
        setIsRescanning(false);
        const msg = `Re-Scan abgeschlossen: ${progress.indexedSessions ?? 0} Sessions, ${(progress.totalEvents ?? 0).toLocaleString()} Ereignisse neu indexiert.`;
        setRescanStatusMessage(msg);
        showToast(`✓ ${msg}`);
        loadDbDiag();
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
          { id: 'general', label: '📁 Allgemein & Pfade', icon: Folder },
          { id: 'hud', label: '🖥 Overlays & HUD', icon: Eye },
          { id: 'ocr', label: '👁 mobiGlas & OCR', icon: Radio },
          { id: 'audio', label: '🎙 Aurora & Audio', icon: Volume2 },
          { id: 'uex', label: '🌐 UEX Integration', icon: Globe },
          { id: 'database', label: '💾 SQLite & Wartung', icon: Database },
        ].map((tab) => {
          const Icon = tab.icon;
          const isActive = activeSubTab === tab.id;
          return (
            <button
              key={tab.id}
              onClick={() => setActiveSubTab(tab.id as any)}
              className={`flex items-center space-x-2 px-4 py-2 rounded-lg text-xs font-medium transition ${
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
          <div className="p-6 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-5">
            <div>
              <h2 className="text-sm font-bold text-sky-400 flex items-center space-x-2">
                <Folder className="w-4 h-4" />
                <span>STAR CITIZEN GAME.LOG DATEIPFAD</span>
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
                  className="px-3 py-2.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-medium border border-slate-700 transition shrink-0"
                  title="Game.log manuell auswählen"
                >
                  📁 Durchsuchen
                </button>
                <button
                  onClick={handleAutoDetect}
                  className="px-3 py-2.5 rounded-lg bg-sky-900/60 hover:bg-sky-800/80 text-sky-300 text-xs font-medium border border-sky-700/60 transition shrink-0"
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

          <div className="p-6 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-5">
            <div>
              <h2 className="text-sm font-bold text-sky-400 flex items-center space-x-2">
                <Sliders className="w-4 h-4" />
                <span>START-KONTOSTAND (aUEC)</span>
              </h2>
              <p className="text-xs text-slate-400 mt-1">
                Referenzkontostand für die Saldo-Berechnung und OCR-Deltas.
              </p>
            </div>

            <div className="max-w-md space-y-2">
              <div className="flex items-center space-x-3">
                <input
                  type="number"
                  value={settings.balance}
                  onChange={(e) =>
                    setSettings({ ...settings, balance: parseInt(e.target.value, 10) || 0 })
                  }
                  className="flex-1 bg-slate-950 border border-slate-700 rounded-lg px-4 py-2 text-xs text-emerald-400 font-mono font-bold focus:outline-none focus:border-sky-500"
                />
                <span className="text-xs text-slate-400 font-bold">aUEC</span>
              </div>
              <p className="text-[11px] text-slate-500">
                Wird bei aktivem Auto-OCR automatisch abgeglichen, wenn der mobiGlas Kontostand erkannt wird.
              </p>
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
          <div className="p-6 rounded-xl bg-slate-900/60 border border-slate-800/80 space-y-5">
            <div>
              <h2 className="text-sm font-bold text-sky-400 flex items-center space-x-2">
                <Radio className="w-4 h-4" />
                <span>MOBIGLAS WALLET & KONTOSTAND-OCR</span>
              </h2>
              <p className="text-xs text-slate-400 mt-1">
                Liest den aUEC-Kontostand aus dem geöffneten mobiGlas über Win32 GDI & Tesseract Engine 5.
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
                    Prüft bei mobiGlas-Events automatisch den Bildschirmbereich und aktualisiert den Saldo
                  </div>
                </div>
              </label>

              <div className="p-4 rounded-lg bg-slate-950/60 border border-slate-800 space-y-3">
                <div className="text-xs font-semibold text-slate-200">
                  Dual-Read & Cross-Grab Schutzmechanismus
                </div>
                <div className="text-xs text-slate-400 leading-relaxed space-y-1.5">
                  <div className="flex items-center space-x-2 text-emerald-400">
                    <Shield className="w-4 h-4 shrink-0" />
                    <span>Cross-Grab Bestätigung: Doppelter Abgleich desselben Betrags verhindert Fehlscans</span>
                  </div>
                  <div className="flex items-center space-x-2 text-sky-400">
                    <FileCheck className="w-4 h-4 shrink-0" />
                    <span>Adaptive Schwellenwert-Invertierung & Kontrast-Filter für hohe Erkennungsgenauigkeit</span>
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
                  v{dbDiag?.installedSchemaVersion ?? 17} (App: v{dbDiag?.currentSchemaVersion ?? 17})
                </div>
              </div>
              <div className="p-3.5 rounded-lg bg-slate-950 border border-slate-800">
                <div className="text-slate-400">Parser Version:</div>
                <div className="text-sm font-bold text-emerald-400 mt-0.5">
                  v{dbDiag?.installedParserVersion ?? 28} (Engine: v{dbDiag?.currentParserVersion ?? 28})
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

            {/* Re-Scan Fortschritts-Banner */}
            {isRescanning && rescanProgress && (
              <div className="rounded-lg p-4 border border-cyan-500/50 bg-cyan-950/30 space-y-2 animate-in fade-in font-sans">
                <div className="flex items-center justify-between text-xs font-mono">
                  <div className="flex items-center space-x-2 text-cyan-300 min-w-0">
                    <RefreshCw className="w-4 h-4 animate-spin text-cyan-400 shrink-0" />
                    <span className="font-bold shrink-0">
                      Indexiere Log-Dateien ({rescanProgress.current}/{rescanProgress.total}):
                    </span>
                    <span className="text-slate-300 truncate">{rescanProgress.currentFileName}</span>
                  </div>
                  <span className="font-bold text-cyan-400 shrink-0 ml-2">{rescanProgress.percent}%</span>
                </div>
                <div className="w-full bg-slate-900 rounded-full h-2.5 overflow-hidden border border-cyan-900/60">
                  <div
                    className="bg-gradient-to-r from-cyan-500 to-sky-400 h-full transition-all duration-300 shadow-[0_0_10px_rgba(6,182,212,0.8)]"
                    style={{ width: `${Math.max(3, rescanProgress.percent)}%` }}
                  />
                </div>
              </div>
            )}

            {/* Erfolgsmeldung */}
            {rescanStatusMessage && !isRescanning && (
              <div className="p-3.5 rounded-lg bg-emerald-950/40 border border-emerald-500/40 text-emerald-300 text-xs flex items-center space-x-2.5 animate-in fade-in">
                <CheckCircle2 className="w-4 h-4 text-emerald-400 shrink-0" />
                <span className="font-semibold">{rescanStatusMessage}</span>
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
                  <span>🧹 DB bereinigen &amp; VACUUM</span>
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
                onClick={loadDbDiag}
                disabled={isCheckingDb || isRescanning}
                className="flex items-center gap-1.5 px-3.5 py-2 rounded-lg bg-slate-900 hover:bg-slate-800 text-slate-300 text-xs font-medium border border-slate-700 transition cursor-pointer"
              >
                <span>🔍 Tiefe Integritätsprüfung</span>
              </button>
              <button
                onClick={handleRepairDb}
                disabled={isCheckingDb || isRescanning}
                className="flex items-center gap-1.5 px-3.5 py-2 rounded-lg bg-cyan-950/60 hover:bg-cyan-900/80 text-cyan-300 text-xs font-medium border border-cyan-800 transition cursor-pointer"
              >
                <span>⚡ Struktur &amp; Indizes reparieren</span>
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
    </div>
  );
};
