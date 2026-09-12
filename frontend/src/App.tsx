import React, { useEffect, useState } from 'react';
import { RefreshCw } from 'lucide-react';
import {
  bridge,
  AppStatus,
  SessionSummary,
  LogEventItem,
  HudTelemetry,
  ScanProgress,
} from './services/photinoBridge';
import { Sidebar, NavTabId } from './components/Sidebar';
import { MasterHeader } from './components/MasterHeader';
import { SessionBar } from './components/SessionBar';
import { HudBar } from './components/HudBar';
import { EventsView } from './views/EventsView';
import { ChatLogView } from './views/ChatLogView';
import { FinancesView } from './views/FinancesView';
import { WarehouseView } from './views/WarehouseView';
import { FleetView } from './views/FleetView';
import { MissionsView } from './views/MissionsView';
import { ReputationView } from './views/ReputationView';
import { BlueprintsView } from './views/BlueprintsView';
import { LoadoutView } from './views/LoadoutView';
import { StarmapView } from './views/StarmapView';
import { PlacesView } from './views/PlacesView';
import { BlackboxView } from './views/BlackboxView';
import { OreScannerView } from './views/OreScannerView';
import { MarketView } from './views/MarketView';
import { ToolsView } from './views/ToolsView';
import { SettingsView } from './views/SettingsView';
import { AboutView } from './views/AboutView';
import { WikiExplorerView } from './views/WikiExplorerView';
import { DbUpdateModal } from './components/DbUpdateModal';
import { UpdateModal } from './components/UpdateModal';
import { WikiDossierModal } from './components/WikiDossierModal';
import { HardDrive } from 'lucide-react';
import { UpdateInfoDto, WikiInfo } from './services/photinoBridge';

export const App: React.FC = () => {
  const [activeTab, setActiveTab] = useState<NavTabId>('events');
  const [sidebarCollapsed, setSidebarCollapsed] = useState<boolean>(false);
  const [isHudCollapsed, setIsHudCollapsed] = useState<boolean>(false);
  const [status, setStatus] = useState<AppStatus | null>(null);
  const [updateInfo, setUpdateInfo] = useState<UpdateInfoDto | null>(null);
  const [isUpdateModalOpen, setIsUpdateModalOpen] = useState<boolean>(false);
  const [scanProgress, setScanProgress] = useState<ScanProgress | null>(null);
  const [sessions, setSessions] = useState<SessionSummary[]>([]);
  const [events, setEvents] = useState<LogEventItem[]>([]);
  const [warehouseTotal, setWarehouseTotal] = useState<number>(0);
  const [loading, setLoading] = useState<boolean>(true);
  const [isScanning, setIsScanning] = useState<boolean>(false);

  // Wiki Dossier Modal State
  const [wikiModalOpen, setWikiModalOpen] = useState<boolean>(false);
  const [wikiModalItem, setWikiModalItem] = useState<WikiInfo | null>(null);
  const [wikiModalQuery, setWikiModalQuery] = useState<string | null>(null);

  const handleOpenWiki = (target: string | WikiInfo) => {
    if (typeof target === 'string') {
      setWikiModalQuery(target);
      setWikiModalItem(null);
    } else {
      setWikiModalItem(target);
      setWikiModalQuery(target.name);
    }
    setWikiModalOpen(true);
  };

  const [telemetry, setTelemetry] = useState<HudTelemetry>({
    isGameRunning: false,
    pilotName: '—',
    serverRegionCode: 'EU',
    serverRegionName: 'Europa',
    serverShard: '—',
    serverShardNumber: '—',
    serverVersion: '—',
    serverPingMs: null,
    locationName: '—',
    locationSystem: 'Stanton',
    locationBody: '—',
    locationType: 'Standort',
    isArmistice: true,
    jurisdiction: 'UEE Protektorat',
    shipName: '—',
    shipFlightInfo: '—',
    balance: 0,
    sessionIncome: 0,
    sessionSpend: 0,
    sessionNet: 0,
    autoOcrEnabled: true,
    activeMissionTitle: 'Kein aktiver Auftrag',
    activeMissionGiver: '—',
    activeMissionReward: 0,
    activeMissionStatus: 'Bereit',
    sessionSpanText: '—',
    selectedSession: '__live__',
  });

  const loadData = async () => {
    try {
      setLoading(true);
      const [statusRes, sessionsRes, whRes, hudRes, eventsRes] = await Promise.all([
        bridge.sendRequest<AppStatus>('get_status'),
        bridge.sendRequest<SessionSummary[]>('get_sessions'),
        bridge.sendRequest<{ locations: any[] }>('get_warehouse'),
        bridge.sendRequest<HudTelemetry>('get_hud'),
        bridge.sendRequest<LogEventItem[]>('get_events', { session: '__live__', limit: 100 }),
      ]);
      setStatus(statusRes);
      setSessions(sessionsRes);
      if (eventsRes) setEvents(eventsRes);
      if (hudRes) setTelemetry(hudRes);
      if (whRes?.locations) {
        const total = whRes.locations.reduce((acc: number, l: any) => acc + (l.totalItems || 0), 0);
        setWarehouseTotal(total);
      }
    } catch (err) {
      console.error('Failed to load initial data:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();

    // Subscribe to live events from C# backend
    const unbindLog = bridge.on<LogEventItem>('LOG_EVENT', (newEvent) => {
      setEvents((prev) => [newEvent, ...prev.slice(0, 99)]);
    });

    const unbindLiveLoaded = bridge.on<LogEventItem[]>('LIVE_EVENTS_LOADED', (loadedEvents) => {
      if (Array.isArray(loadedEvents)) {
        setEvents(loadedEvents);
      }
    });

    const unbindStatus = bridge.on<AppStatus>('STATUS_UPDATE', (newStatus) => {
      setStatus(newStatus);
    });

    const unbindHud = bridge.on<HudTelemetry>('HUD_UPDATE', (newTelemetry) => {
      setTelemetry(newTelemetry);
    });

    const unbindWh = bridge.on<{ locations: any[] }>('WAREHOUSE_UPDATED', (data) => {
      if (data?.locations) {
        const total = data.locations.reduce((acc: number, l: any) => acc + (l.totalItems || 0), 0);
        setWarehouseTotal(total);
      }
    });

    const unbindScan = bridge.on<any>('SCAN_PROGRESS', (progress) => {
      setScanProgress(progress);
      if (progress.isCompleted) {
        setIsScanning(false);
        loadData();
      } else {
        setIsScanning(true);
      }
    });

    const unbindUpdate = bridge.on<UpdateInfoDto>('UPDATE_AVAILABLE', (info) => {
      if (info && info.updateAvailable) {
        setUpdateInfo(info);
        setIsUpdateModalOpen(true);
      }
    });

    // Keyboard shortcut Alt + H to toggle HUD collapse
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.altKey && (e.key === 'h' || e.key === 'H')) {
        e.preventDefault();
        setIsHudCollapsed((prev) => !prev);
      }
    };
    window.addEventListener('keydown', handleKeyDown);

    const onOpenWikiEvent = (e: any) => {
      if (e.detail) handleOpenWiki(e.detail);
    };
    window.addEventListener('open-wiki-dossier', onOpenWikiEvent);

    return () => {
      unbindLog();
      unbindLiveLoaded();
      unbindStatus();
      unbindHud();
      unbindWh();
      unbindScan();
      unbindUpdate();
      window.removeEventListener('keydown', handleKeyDown);
      window.removeEventListener('open-wiki-dossier', onOpenWikiEvent);
    };
  }, []);

  const handleSelectTab = (tab: NavTabId) => {
    setActiveTab(tab);
  };

  const handleTriggerScan = async () => {
    try {
      setIsScanning(true);
      await bridge.sendRequest('scan_logs');
      await loadData();
    } catch (err) {
      console.error('Scan failed:', err);
    } finally {
      setIsScanning(false);
    }
  };

  const handleReparseAll = async () => {
    try {
      setIsScanning(true);
      await bridge.sendRequest('reparse_all_logs');
      await loadData();
    } catch (err) {
      console.error('Reparse all failed:', err);
    } finally {
      setIsScanning(false);
    }
  };


  const handleTriggerOcr = async () => {
    try {
      await bridge.sendRequest('trigger_ocr');
    } catch (err) {
      console.error('Failed to trigger OCR:', err);
    }
  };

  const handleToggleAutoOcr = async () => {
    try {
      await bridge.sendRequest('toggle_auto_ocr');
    } catch (err) {
      console.error('Failed to toggle auto OCR:', err);
    }
  };

  return (
    <div className="flex h-screen w-screen sc-grid bg-[#030712] text-slate-100 font-sans overflow-hidden">
      {/* 16-Tab Navigation Sidebar */}
      <Sidebar
        activeTab={activeTab}
        onSelectTab={handleSelectTab}
        collapsed={sidebarCollapsed}
        onToggleCollapsed={() => setSidebarCollapsed(!sidebarCollapsed)}
        warehouseCount={warehouseTotal > 0 ? warehouseTotal : undefined}
        liveEventCount={events.length > 0 ? events.length : undefined}
      />

      {/* Main App Container */}
      <div className="flex-1 flex flex-col min-w-0 min-h-0 overflow-hidden">
        {/* Master Header: SC Prozess Status, Overlays, Sprache, Watcher, Pilot Dossier */}
        {/* Master Header: SC Prozess Status, Overlays, Sprache, Watcher */}
        <MasterHeader
          status={status}
          isGameRunning={telemetry.isGameRunning}
          isScanning={isScanning}
          loading={loading}
          updateInfo={updateInfo}
          onOpenUpdateModal={() => setIsUpdateModalOpen(true)}
          onRefresh={loadData}
          onTriggerScan={handleTriggerScan}
          onReparseAll={handleReparseAll}
        />

        {/* Globaler Re-Scan / Log-Scan Fortschritts-Banner */}
        {isScanning && (
          <div className="bg-gradient-to-r from-cyan-950 via-slate-950 to-cyan-950 border-b border-cyan-500/60 px-4 py-2 flex flex-col sm:flex-row sm:items-center justify-between gap-2 text-xs font-mono text-cyan-200 shadow-xl shadow-cyan-950/50 z-30 animate-in slide-in-from-top-1">
            <div className="flex items-center space-x-2.5 min-w-0">
              <RefreshCw className="w-4 h-4 text-cyan-400 animate-spin shrink-0" />
              <div className="flex items-center space-x-2 truncate">
                <span className="font-bold text-white uppercase tracking-wider shrink-0">
                  {scanProgress && scanProgress.total > 0
                    ? `Log-Scan (${scanProgress.current}/${scanProgress.total}):`
                    : 'Log-Scan läuft:'}
                </span>
                <span className="text-cyan-300 truncate font-sans">
                  {scanProgress?.currentFileName || 'Lese Star Citizen Log-Dateien ein...'}
                </span>
              </div>
            </div>
            <div className="flex items-center space-x-3 shrink-0 self-end sm:self-auto">
              <span className="font-bold text-cyan-400">
                {scanProgress && scanProgress.total > 0 ? `${scanProgress.percent}%` : 'Aktiv...'}
              </span>
              <div className="w-36 bg-slate-900 rounded-full h-2 overflow-hidden border border-cyan-900/60">
                <div
                  className="bg-gradient-to-r from-cyan-500 to-sky-400 h-full transition-all duration-300 shadow-[0_0_8px_rgba(6,182,212,0.8)]"
                  style={{
                    width: scanProgress && scanProgress.total > 0 ? `${Math.max(4, scanProgress.percent)}%` : '50%',
                  }}
                />
              </div>
            </div>
          </div>
        )}

        {/* Live-Status Strip: Aktive Sitzung, Zeitspanne & HUD-Toggle */}
        <SessionBar
          sessionSpanText={telemetry.sessionSpanText}
          isHudCollapsed={isHudCollapsed}
          activeSessionName={status?.activeSessionName || undefined}
          onToggleHudCollapsed={() => setIsHudCollapsed(!isHudCollapsed)}
        />

        {/* Permanentes 2-Zeilen SC-HUD (Collapsible für mehr Arbeitsfläche) */}
        {!isHudCollapsed && (
          <HudBar
            telemetry={telemetry}
            onNavigate={handleSelectTab}
            onTriggerOcr={handleTriggerOcr}
            onToggleAutoOcr={handleToggleAutoOcr}
            onOpenWiki={handleOpenWiki}
          />
        )}

        {/* View Body: 16 Tabs exact matching Avalonia RC2 */}
        <main className="flex-1 min-h-0 min-w-0 overflow-y-auto overflow-x-auto p-4">
          {activeTab === 'events' && (
            <EventsView
              sessions={sessions}
            />
          )}

          {activeTab === 'chat' && <ChatLogView />}

          {activeTab === 'finances' && <FinancesView />}

          {activeTab === 'missions' && <MissionsView />}

          {activeTab === 'reputation' && <ReputationView />}

          {activeTab === 'starmap' && <StarmapView />}

          {activeTab === 'places' && <PlacesView />}

          {activeTab === 'blackbox' && <BlackboxView />}

          {activeTab === 'orescanner' && <OreScannerView />}

          {activeTab === 'market' && <MarketView />}

          {activeTab === 'fleet' && <FleetView />}

          {activeTab === 'wiki' && <WikiExplorerView onOpenDossier={handleOpenWiki} />}

          {activeTab === 'warehouse' && <WarehouseView />}

          {activeTab === 'blueprints' && <BlueprintsView />}

          {activeTab === 'loadout' && <LoadoutView />}

          {activeTab === 'tools' && <ToolsView />}

          {activeTab === 'settings' && <SettingsView />}

          {activeTab === 'about' && <AboutView />}
        </main>

        {/* Database Migration & Scan Progress Modal */}
        <DbUpdateModal
          progress={scanProgress}
          onDismiss={() => setScanProgress(null)}
        />

        {/* Application Release Update Modal */}
        <UpdateModal
          isOpen={isUpdateModalOpen}
          updateInfo={updateInfo}
          onClose={() => setIsUpdateModalOpen(false)}
        />

        {/* Star Citizen Wiki Dossier Modal */}
        <WikiDossierModal
          isOpen={wikiModalOpen}
          item={wikiModalItem}
          query={wikiModalQuery}
          onClose={() => setWikiModalOpen(false)}
        />

        {/* Bottom Statusbar */}
        <footer className="flex items-center justify-between px-5 py-1.5 border-t border-cyan-950/60 bg-[#020610]/95 text-[11px] font-mono text-slate-500 shrink-0">
          <div className="flex items-center gap-4">
            <span className="flex items-center gap-1.5">
              <HardDrive className="w-3 h-3 text-cyan-400" /> SQLite: sessions.db
            </span>
            <span>·</span>
            <span>{status?.dbSessionCount ?? sessions.length} Sessions indexiert</span>
            <span>·</span>
            <span>{warehouseTotal} Lagerartikel</span>
          </div>
          <div className="flex items-center gap-3">
            <span
              className={`font-semibold ${
                bridge.isConnected ? 'text-emerald-400' : 'text-cyan-400'
              }`}
            >
              {bridge.isConnected ? '● PHOTINO NATIVE' : '○ DEV BROWSER'}
            </span>
            <span>·</span>
            <span className="text-cyan-400 font-semibold">{status?.version || 'v1.0.0-rc2'}</span>
          </div>
        </footer>
      </div>
    </div>
  );
};

export default App;
