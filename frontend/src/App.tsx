import React, { useEffect, useState } from 'react';
import {
  bridge,
  AppStatus,
  SessionSummary,
  LogEventItem,
  HudTelemetry,
} from './services/photinoBridge';
import { Sidebar, NavTabId } from './components/Sidebar';
import { MasterHeader } from './components/MasterHeader';
import { SessionBar } from './components/SessionBar';
import { HudBar } from './components/HudBar';
import { DashboardView } from './views/DashboardView';
import { EventsView } from './views/EventsView';
import { SessionsView } from './views/SessionsView';
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
import { HardDrive } from 'lucide-react';

export const App: React.FC = () => {
  const [activeTab, setActiveTab] = useState<NavTabId>('dashboard');
  const [sidebarCollapsed, setSidebarCollapsed] = useState<boolean>(false);
  const [isHudCollapsed, setIsHudCollapsed] = useState<boolean>(false);
  const [status, setStatus] = useState<AppStatus | null>(null);
  const [sessions, setSessions] = useState<SessionSummary[]>([]);
  const [events, setEvents] = useState<LogEventItem[]>([]);
  const [warehouseTotal, setWarehouseTotal] = useState<number>(0);
  const [loading, setLoading] = useState<boolean>(true);
  const [isScanning, setIsScanning] = useState<boolean>(false);

  const [telemetry, setTelemetry] = useState<HudTelemetry>({
    isGameRunning: false,
    pilotName: '—',
    serverRegionCode: 'EU',
    serverRegionName: 'Europa',
    serverShard: '—',
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
      const [statusRes, sessionsRes, whRes, hudRes] = await Promise.all([
        bridge.sendRequest<AppStatus>('get_status'),
        bridge.sendRequest<SessionSummary[]>('get_sessions'),
        bridge.sendRequest<{ locations: any[] }>('get_warehouse'),
        bridge.sendRequest<HudTelemetry>('get_hud'),
      ]);
      setStatus(statusRes);
      setSessions(sessionsRes);
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

    // Keyboard shortcut Alt + H to toggle HUD collapse
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.altKey && (e.key === 'h' || e.key === 'H')) {
        e.preventDefault();
        setIsHudCollapsed((prev) => !prev);
      }
    };
    window.addEventListener('keydown', handleKeyDown);

    return () => {
      unbindLog();
      unbindStatus();
      unbindHud();
      unbindWh();
      window.removeEventListener('keydown', handleKeyDown);
    };
  }, []);

  const handleToggleWatcher = async () => {
    if (!status) return;
    try {
      const res = await bridge.sendRequest<{ isLiveWatching: boolean }>('toggle_watcher', {
        enable: !status.isLiveWatching,
      });
      setStatus((prev) => (prev ? { ...prev, isLiveWatching: res.isLiveWatching } : null));
    } catch (err) {
      console.error('Failed to toggle watcher:', err);
    }
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

  const handleSelectSession = async (sessionName: string) => {
    try {
      const res = await bridge.sendRequest<HudTelemetry>('select_session', { session: sessionName });
      if (res) setTelemetry(res);
    } catch (err) {
      console.error('Failed to select session:', err);
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
        onSelectTab={setActiveTab}
        collapsed={sidebarCollapsed}
        onToggleCollapsed={() => setSidebarCollapsed(!sidebarCollapsed)}
        warehouseCount={warehouseTotal > 0 ? warehouseTotal : undefined}
        liveEventCount={events.length > 0 ? events.length : undefined}
      />

      {/* Main App Container */}
      <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
        {/* Master Header: SC Prozess Status, Overlays, Sprache, Watcher */}
        <MasterHeader
          status={status}
          isGameRunning={telemetry.isGameRunning}
          isScanning={isScanning}
          loading={loading}
          onRefresh={loadData}
          onTriggerScan={handleTriggerScan}
          onToggleWatcher={handleToggleWatcher}
        />

        {/* mobiGlas Session-Strip: Dropdown, Zeitspanne & HUD-Toggle */}
        <SessionBar
          sessions={sessions}
          selectedSession={telemetry.selectedSession}
          sessionSpanText={telemetry.sessionSpanText}
          isHudCollapsed={isHudCollapsed}
          onSelectSession={handleSelectSession}
          onToggleHudCollapsed={() => setIsHudCollapsed(!isHudCollapsed)}
        />

        {/* Permanentes 2-Zeilen SC-HUD (Collapsible für mehr Arbeitsfläche) */}
        {!isHudCollapsed && (
          <HudBar
            telemetry={telemetry}
            onNavigate={setActiveTab}
            onTriggerOcr={handleTriggerOcr}
            onToggleAutoOcr={handleToggleAutoOcr}
          />
        )}

        {/* View Body */}
        <main className="flex-1 overflow-hidden p-4">
          {activeTab === 'dashboard' && (
            <DashboardView
              status={status}
              sessions={sessions}
              events={events}
              onNavigate={setActiveTab}
              warehouseTotal={warehouseTotal}
            />
          )}

          {activeTab === 'events' && <EventsView />}

          {activeTab === 'sessions' && <SessionsView sessions={sessions} />}

          {activeTab === 'finances' && <FinancesView />}

          {activeTab === 'warehouse' && <WarehouseView />}

          {activeTab === 'fleet' && <FleetView />}

          {activeTab === 'missions' && <MissionsView />}

          {activeTab === 'reputation' && <ReputationView />}

          {activeTab === 'blueprints' && <BlueprintsView />}

          {activeTab === 'loadout' && <LoadoutView />}

          {activeTab === 'starmap' && <StarmapView />}

          {activeTab === 'places' && <PlacesView />}

          {activeTab === 'blackbox' && <BlackboxView />}

          {activeTab === 'orescanner' && <OreScannerView />}

          {activeTab === 'market' && <MarketView />}

          {activeTab === 'tools' && <ToolsView />}

          {activeTab === 'settings' && <SettingsView />}

          {activeTab === 'about' && <AboutView />}
        </main>

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
