import React, { useEffect, useState } from 'react';
import {
  bridge,
  AppStatus,
  SessionSummary,
  LogEventItem,
} from './services/photinoBridge';
import { Sidebar, NavTabId } from './components/Sidebar';
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
import { PlaceholderView } from './views/PlaceholderView';
import {
  FolderSync,
  HardDrive,
  Play,
  RefreshCw,
  Square,
} from 'lucide-react';

export const App: React.FC = () => {
  const [activeTab, setActiveTab] = useState<NavTabId>('dashboard');
  const [sidebarCollapsed, setSidebarCollapsed] = useState<boolean>(false);
  const [status, setStatus] = useState<AppStatus | null>(null);
  const [sessions, setSessions] = useState<SessionSummary[]>([]);
  const [events, setEvents] = useState<LogEventItem[]>([]);
  const [warehouseTotal, setWarehouseTotal] = useState<number>(0);
  const [loading, setLoading] = useState<boolean>(true);
  const [isScanning, setIsScanning] = useState<boolean>(false);

  const loadData = async () => {
    try {
      setLoading(true);
      const [statusRes, sessionsRes, whRes] = await Promise.all([
        bridge.sendRequest<AppStatus>('get_status'),
        bridge.sendRequest<SessionSummary[]>('get_sessions'),
        bridge.sendRequest<{ locations: any[] }>('get_warehouse'),
      ]);
      setStatus(statusRes);
      setSessions(sessionsRes);
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

    const unbindWh = bridge.on<{ locations: any[] }>('WAREHOUSE_UPDATED', (data) => {
      if (data?.locations) {
        const total = data.locations.reduce((acc: number, l: any) => acc + (l.totalItems || 0), 0);
        setWarehouseTotal(total);
      }
    });

    return () => {
      unbindLog();
      unbindStatus();
      unbindWh();
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
        {/* Top HUD Header */}
        <header className="flex items-center justify-between px-6 py-2.5 border-b border-slate-800/80 bg-[#030712]/90 backdrop-blur-md z-10 shrink-0 h-14">
          <div className="flex items-center gap-3 truncate">
            <span className="font-mono text-xs text-slate-400 flex items-center gap-2 truncate">
              <HardDrive className="w-3.5 h-3.5 text-cyan-400 shrink-0" />
              <span className="truncate max-w-md" title={status?.logPath || 'Standardpfad'}>
                {status?.logPath || 'Star Citizen Live Log'}
              </span>
            </span>
          </div>

          {/* Action & Watcher Controls */}
          <div className="flex items-center gap-3 shrink-0">
            <div className="flex items-center gap-2 px-3 py-1.5 rounded-md bg-slate-900/80 border border-slate-800">
              <span
                className={`w-2.5 h-2.5 rounded-full ${
                  status?.isLiveWatching
                    ? 'bg-emerald-400 animate-pulse shadow-[0_0_8px_#34d399]'
                    : 'bg-amber-400/80'
                }`}
              />
              <span className="text-xs font-mono font-medium text-slate-300">
                {status?.isLiveWatching ? 'LIVE WATCHING' : 'STANDBY'}
              </span>
            </div>

            <button
              onClick={handleToggleWatcher}
              className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded border transition-all cursor-pointer ${
                status?.isLiveWatching
                  ? 'border-amber-500/40 bg-amber-950/30 text-amber-300 hover:bg-amber-900/40'
                  : 'border-emerald-500/40 bg-emerald-950/30 text-emerald-300 hover:bg-emerald-900/40'
              }`}
            >
              {status?.isLiveWatching ? (
                <>
                  <Square className="w-3.5 h-3.5 fill-current" /> Stopp
                </>
              ) : (
                <>
                  <Play className="w-3.5 h-3.5 fill-current" /> Start Watcher
                </>
              )}
            </button>

            <button
              onClick={handleTriggerScan}
              disabled={isScanning}
              className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded border border-cyan-500/40 bg-cyan-950/30 text-cyan-300 hover:bg-cyan-900/40 transition-all cursor-pointer disabled:opacity-50"
            >
              <FolderSync className={`w-3.5 h-3.5 ${isScanning ? 'animate-spin' : ''}`} />
              {isScanning ? 'Scanne...' : 'Logs scannen'}
            </button>

            <button
              onClick={loadData}
              title="Neu laden"
              className="p-1.5 rounded border border-slate-800 hover:border-slate-700 bg-slate-900/60 text-slate-400 hover:text-slate-200 transition cursor-pointer"
            >
              <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
            </button>
          </div>
        </header>

        {/* View Body */}
        <main className="flex-1 overflow-hidden p-5">
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

          {/* Placeholders for remaining tabs */}
          {['tools', 'settings', 'about'].includes(activeTab) && (
            <PlaceholderView tab={activeTab} />
          )}
        </main>

        {/* Bottom Statusbar */}
        <footer className="flex items-center justify-between px-6 py-2 border-t border-slate-800 bg-[#030712]/95 text-[11px] font-mono text-slate-500 shrink-0">
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
