import React, { useEffect, useState } from 'react';
import { RefreshCw } from 'lucide-react';
import {
  bridge,
  AppStatus,
  SessionSummary,
  LogEventItem,
  HudTelemetry,
  ScanProgress,
  applyFontFamily,
} from './services/photinoBridge';
import { Sidebar, NavTabId } from './components/Sidebar';
import { MasterHeader } from './components/MasterHeader';
import { SessionBar } from './components/SessionBar';
import { HudBar } from './components/HudBar';
import { EventsView } from './views/EventsView';
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
import { RefineryView } from './views/RefineryView';
import { MarketView } from './views/MarketView';
import { ToolsView } from './views/ToolsView';
import { SettingsView } from './views/SettingsView';
import { AboutView } from './views/AboutView';
import { WikiExplorerView } from './views/WikiExplorerView';
import { DbUpdateModal } from './components/DbUpdateModal';
import { UpdateModal } from './components/UpdateModal';
import { WikiDossierModal } from './components/WikiDossierModal';
import { GlobalTooltip } from './components/GlobalTooltip';
import { HardDrive } from 'lucide-react';
import { UpdateInfoDto, WikiInfo, AutoLoadEntryDto, PluginDto } from './services/photinoBridge';
import { useI18n } from './i18n';
import { PluginHostView } from './views/PluginHostView';

export interface NavTargetContext {
  search?: string;
  subTab?: string;
  location?: string;
  tab?: string;
}

export const App: React.FC = () => {
  const { t } = useI18n();
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
  const [refineryCount, setRefineryCount] = useState<number>(0);
  const [autoLoadCount, setAutoLoadCount] = useState<number>(0);
  const [plugins, setPlugins] = useState<PluginDto[]>([]);
  const [pluginsServerPort, setPluginsServerPort] = useState<number>(48123);
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
      const [statusRes, sessionsRes, whRes, hudRes, eventsRes, haulsRes, settingsRes, autoLoadsRes] = await Promise.all([
        bridge.sendRequest<AppStatus>('get_status'),
        bridge.sendRequest<SessionSummary[]>('get_sessions'),
        bridge.sendRequest<{ locations: any[] }>('get_warehouse'),
        bridge.sendRequest<HudTelemetry>('get_hud'),
        bridge.sendRequest<LogEventItem[]>('get_events', { session: '__live__', limit: 100 }),
        bridge.sendRequest<any[]>('get_mining_hauls'),
        bridge.sendRequest<any>('get_settings').catch(() => null),
        bridge.sendRequest<AutoLoadEntryDto[]>('get_autoload_entries').catch(() => []),
      ]);
      if (settingsRes?.selectedFontFamily) {
        applyFontFamily(settingsRes.selectedFontFamily);
      }
      setStatus(statusRes);
      setSessions(sessionsRes);
      if (eventsRes) setEvents(eventsRes);
      if (hudRes) setTelemetry(hudRes);
      if (whRes?.locations) {
        const total = whRes.locations.reduce((acc: number, l: any) => acc + (l.totalItems || 0), 0);
        setWarehouseTotal(total);
      }
      if (haulsRes && Array.isArray(haulsRes)) {
        setRefineryCount(haulsRes.filter((h: any) => h.status === 'Refining').length);
      }
      if (autoLoadsRes && Array.isArray(autoLoadsRes)) {
        setAutoLoadCount(autoLoadsRes.length);
      }
      try {
        const pRes = await bridge.getPlugins();
        if (pRes) {
          setPlugins(pRes.plugins || []);
          if (pRes.serverPort) setPluginsServerPort(pRes.serverPort);
        }
      } catch (pErr) {
        console.error('Failed to load plugins:', pErr);
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
      setEvents((prev) => {
        if (
          prev.some(
            (x) =>
              x.id === newEvent.id ||
              (x.timestamp === newEvent.timestamp &&
                (x.kind || x.category) === (newEvent.kind || newEvent.category) &&
                x.description === newEvent.description &&
                x.amount === newEvent.amount)
          )
        ) {
          return prev;
        }
        return [newEvent, ...prev.slice(0, 99)];
      });
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

    const unbindMining = bridge.on<any[]>('mining_hauls_response', (hauls) => {
      if (Array.isArray(hauls)) {
        setRefineryCount(hauls.filter((h: any) => h.status === 'Refining').length);
      }
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

    const unbindAutoLoad = bridge.on<AutoLoadEntryDto[]>('AUTOLOAD_UPDATED', (entries) => {
      if (Array.isArray(entries)) {
        setAutoLoadCount(entries.length);
      }
    });

    const unbindAutoLoadDone = bridge.on('AUTOLOAD_COMPLETED', () => {
      bridge
        .sendRequest<AutoLoadEntryDto[]>('get_autoload_entries')
        .then((res) => {
          setAutoLoadCount(res?.length || 0);
        })
        .catch(() => {});
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

    const unbindPlugins = bridge.on('PLUGINS_CHANGED', (res: any) => {
      if (res?.plugins) setPlugins(res.plugins);
    });

    return () => {
      unbindLog();
      unbindLiveLoaded();
      unbindStatus();
      unbindHud();
      unbindMining();
      unbindWh();
      unbindScan();
      unbindUpdate();
      unbindAutoLoad();
      unbindAutoLoadDone();
      unbindPlugins();
      window.removeEventListener('keydown', handleKeyDown);
      window.removeEventListener('open-wiki-dossier', onOpenWikiEvent);
    };
  }, []);

  const [navContext, setNavContext] = useState<NavTargetContext | null>(null);

  const handleSelectTab = (tab: NavTabId, context?: NavTargetContext) => {
    setNavContext(context || null);
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
        refineryCount={refineryCount > 0 ? refineryCount : undefined}
        autoLoadCount={autoLoadCount > 0 ? autoLoadCount : undefined}
        plugins={plugins}
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
              initialEvents={events}
              onNavigate={handleSelectTab}
              onOpenWiki={handleOpenWiki}
            />
          )}

          {activeTab === 'finances' && (
            <FinancesView
              onNavigate={handleSelectTab}
              initialSearch={navContext?.search}
              initialSubTab={navContext?.subTab as any}
            />
          )}

          {activeTab === 'missions' && (
            <MissionsView
              initialSearch={navContext?.search}
              initialTab={navContext?.subTab as any}
            />
          )}

          {activeTab === 'reputation' && <ReputationView />}

          {activeTab === 'starmap' && <StarmapView telemetry={telemetry} />}

          {activeTab === 'places' && <PlacesView initialSearch={navContext?.search} />}

          {activeTab === 'blackbox' && <BlackboxView />}

          {activeTab === 'orescanner' && <OreScannerView />}

          {activeTab === 'refinery' && <RefineryView onOpenWiki={handleOpenWiki} />}

          {activeTab === 'market' && <MarketView />}

          {activeTab === 'fleet' && (
            <FleetView
              initialSearch={navContext?.search}
              initialTab={navContext?.subTab as any}
            />
          )}

          {activeTab === 'wiki' && <WikiExplorerView onOpenDossier={handleOpenWiki} />}

          {activeTab === 'warehouse' && (
            <WarehouseView
              initialSearch={navContext?.search}
              initialLocation={navContext?.location}
            />
          )}

          {activeTab === 'blueprints' && <BlueprintsView />}

          {activeTab === 'loadout' && <LoadoutView />}

          {activeTab === 'tools' && <ToolsView />}

          {activeTab === 'settings' && <SettingsView />}

          {activeTab === 'about' && <AboutView />}

          {activeTab.startsWith('plugin:') && (() => {
            const pId = activeTab.slice(7);
            const currentPlugin = plugins.find((p) => p.id === pId);
            if (!currentPlugin) {
              return (
                <div className="flex flex-col items-center justify-center h-96 text-slate-400 space-y-3">
                  <p className="text-sm">Plugin "{pId}" nicht gefunden oder deaktiviert.</p>
                  <button
                    onClick={() => handleSelectTab('settings')}
                    className="px-4 py-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-medium transition"
                  >
                    Zu den Plugin-Einstellungen
                  </button>
                </div>
              );
            }
            return <PluginHostView plugin={currentPlugin} serverPort={pluginsServerPort} />;
          })()}
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

        <GlobalTooltip />

        {/* Bottom Statusbar */}
        <footer className="flex items-center justify-between px-5 py-1.5 border-t border-cyan-950/60 bg-[#020610]/95 text-[11px] font-mono text-slate-500 shrink-0">
          <div className="flex items-center gap-4">
            <span className="flex items-center gap-1.5">
              <HardDrive className="w-3 h-3 text-cyan-400" /> SQLite: sessions.db
            </span>
            <span>·</span>
            <span>{t('statusBar.sessionsIndexed', { count: status?.dbSessionCount ?? sessions.length })}</span>
            <span>·</span>
            <span>{t('statusBar.warehouseItems', { count: warehouseTotal })}</span>
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
