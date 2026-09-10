import React, { useEffect, useState } from 'react';
import {
  bridge,
  AppStatus,
  SessionSummary,
  LogEventItem
} from './services/photinoBridge';
import {
  Activity,
  Coins,
  Compass,
  FolderSync,
  HardDrive,
  Layers,
  Play,
  Radio,
  RefreshCw,
  Rocket,
  Square,
  TrendingDown,
  TrendingUp,
  Workflow
} from 'lucide-react';

export const App: React.FC = () => {
  const [status, setStatus] = useState<AppStatus | null>(null);
  const [sessions, setSessions] = useState<SessionSummary[]>([]);
  const [events, setEvents] = useState<LogEventItem[]>([]);
  const [selectedTab, setSelectedTab] = useState<'sessions' | 'live' | 'ships'>('sessions');
  const [loading, setLoading] = useState<boolean>(true);
  const [isScanning, setIsScanning] = useState<boolean>(false);

  // Load initial data
  const loadData = async () => {
    try {
      setLoading(true);
      const [statusRes, sessionsRes] = await Promise.all([
        bridge.sendRequest<AppStatus>('get_status'),
        bridge.sendRequest<SessionSummary[]>('get_sessions'),
      ]);
      setStatus(statusRes);
      setSessions(sessionsRes);
    } catch (err) {
      console.error('Failed to load data:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();

    // Subscribe to real-time events from C# backend
    const unbindLog = bridge.on<LogEventItem>('LOG_EVENT', (newEvent) => {
      setEvents((prev) => [newEvent, ...prev.slice(0, 99)]);
    });

    const unbindStatus = bridge.on<AppStatus>('STATUS_UPDATE', (newStatus) => {
      setStatus(newStatus);
    });

    const unbindSession = bridge.on<SessionSummary>('SESSION_UPDATE', (updatedSession) => {
      setSessions((prev) => {
        const idx = prev.findIndex((s) => s.id === updatedSession.id);
        if (idx >= 0) {
          const next = [...prev];
          next[idx] = updatedSession;
          return next;
        }
        return [updatedSession, ...prev];
      });
    });

    return () => {
      unbindLog();
      unbindStatus();
      unbindSession();
    };
  }, []);

  const handleToggleWatcher = async () => {
    if (!status) return;
    try {
      const res = await bridge.sendRequest<{ isLiveWatching: boolean }>('toggle_watcher', {
        enable: !status.isLiveWatching,
      });
      setStatus((prev) => prev ? { ...prev, isLiveWatching: res.isLiveWatching } : null);
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

  const formatNumber = (num?: number) => {
    if (num === undefined || num === null) return '0';
    return num.toLocaleString('de-DE');
  };

  return (
    <div className="flex flex-col h-screen w-screen sc-grid bg-[#030712] text-slate-100 font-sans selection:bg-cyan-500 selection:text-black">
      {/* Top HUD Header */}
      <header className="flex items-center justify-between px-6 py-3 border-b border-cyan-950/60 bg-[#030712]/90 backdrop-blur-md z-10">
        <div className="flex items-center gap-4">
          <div className="relative flex items-center justify-center w-9 h-9 rounded bg-cyan-950/40 border border-cyan-500/30 text-cyan-400 sc-hud-corner shadow-[0_0_15px_rgba(0,240,255,0.15)]">
            <Rocket className="w-5 h-5 text-cyan-400" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-lg font-bold tracking-wider uppercase text-cyan-300">
                SCLog<span className="text-amber-400 font-extrabold">Mate</span>
              </h1>
              <span className="sc-badge">PHOTINO PROTOTYPE</span>
            </div>
            <p className="text-xs text-slate-400 font-mono flex items-center gap-2">
              <HardDrive className="w-3 h-3 text-cyan-500" />
              <span className="truncate max-w-[380px]" title={status?.logPath || 'Standardpfad'}>
                {status?.logPath || 'Star Citizen Live Log'}
              </span>
            </p>
          </div>
        </div>

        {/* Live Watcher & Actions Controls */}
        <div className="flex items-center gap-3">
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

      {/* Metric Cards Banner */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 px-6 py-4">
        {/* Net Earnings */}
        <div className="sc-glass rounded-lg p-4 relative overflow-hidden sc-hud-corner">
          <div className="flex justify-between items-start">
            <span className="text-xs font-medium text-slate-400 uppercase tracking-wider">
              Netto aUEC
            </span>
            <Coins className="w-4 h-4 text-cyan-400" />
          </div>
          <div className="mt-2 text-2xl font-bold font-mono tracking-tight text-cyan-300 drop-shadow-[0_0_8px_rgba(0,240,255,0.3)]">
            {formatNumber(status?.totalNet)} <span className="text-xs font-normal text-cyan-500">aUEC</span>
          </div>
          <div className="mt-1 flex items-center text-xs text-slate-400 gap-2">
            <span className="text-emerald-400 flex items-center gap-0.5">
              <TrendingUp className="w-3 h-3" /> +{formatNumber(status?.totalIncome)}
            </span>
            <span>·</span>
            <span className="text-rose-400 flex items-center gap-0.5">
              <TrendingDown className="w-3 h-3" /> -{formatNumber(status?.totalSpend)}
            </span>
          </div>
        </div>

        {/* Sessions Recorded */}
        <div className="sc-glass rounded-lg p-4 relative overflow-hidden sc-hud-corner">
          <div className="flex justify-between items-start">
            <span className="text-xs font-medium text-slate-400 uppercase tracking-wider">
              Sessions
            </span>
            <Layers className="w-4 h-4 text-amber-400" />
          </div>
          <div className="mt-2 text-2xl font-bold font-mono tracking-tight text-amber-300">
            {status?.dbSessionCount ?? sessions.length}{' '}
            <span className="text-xs font-normal text-slate-400">gespeichert</span>
          </div>
          <div className="mt-1 text-xs text-slate-400 truncate">
            Aktiv: {status?.activeSessionName || 'Keine laufende Session'}
          </div>
        </div>

        {/* Trade & Sales */}
        <div className="sc-glass rounded-lg p-4 relative overflow-hidden sc-hud-corner">
          <div className="flex justify-between items-start">
            <span className="text-xs font-medium text-slate-400 uppercase tracking-wider">
              Handel & Beute
            </span>
            <Workflow className="w-4 h-4 text-indigo-400" />
          </div>
          <div className="mt-2 text-2xl font-bold font-mono tracking-tight text-indigo-300">
            {formatNumber(sessions.reduce((acc, s) => acc + s.sales + s.trade, 0))}{' '}
            <span className="text-xs font-normal text-slate-400">aUEC</span>
          </div>
          <div className="mt-1 text-xs text-slate-400">
            Aus Güterverkauf & Auftragsbelohnungen
          </div>
        </div>

        {/* Engine / Bridge Status */}
        <div className="sc-glass rounded-lg p-4 relative overflow-hidden sc-hud-corner">
          <div className="flex justify-between items-start">
            <span className="text-xs font-medium text-slate-400 uppercase tracking-wider">
              Subsystem IPC
            </span>
            <Radio className="w-4 h-4 text-emerald-400" />
          </div>
          <div className="mt-2 flex items-center gap-2">
            <span
              className={`text-sm font-semibold font-mono ${
                bridge.isConnected ? 'text-emerald-400' : 'text-cyan-400'
              }`}
            >
              {bridge.isConnected ? 'PHOTINO NATIVE' : 'DEV STANDALONE'}
            </span>
          </div>
          <div className="mt-1 text-xs text-slate-400 font-mono">
            {status?.version || 'v1.0.0-rc2'} · .NET 10 Bridge
          </div>
        </div>
      </div>

      {/* Tabs Navigation */}
      <div className="flex items-center gap-2 px-6 border-b border-slate-800 bg-[#030712]/50">
        <button
          onClick={() => setSelectedTab('sessions')}
          className={`flex items-center gap-2 px-4 py-2.5 text-xs font-semibold uppercase tracking-wider border-b-2 transition cursor-pointer ${
            selectedTab === 'sessions'
              ? 'border-cyan-400 text-cyan-300 bg-cyan-950/20'
              : 'border-transparent text-slate-400 hover:text-slate-200'
          }`}
        >
          <Layers className="w-3.5 h-3.5" /> Sessions ({sessions.length})
        </button>

        <button
          onClick={() => setSelectedTab('live')}
          className={`flex items-center gap-2 px-4 py-2.5 text-xs font-semibold uppercase tracking-wider border-b-2 transition cursor-pointer ${
            selectedTab === 'live'
              ? 'border-cyan-400 text-cyan-300 bg-cyan-950/20'
              : 'border-transparent text-slate-400 hover:text-slate-200'
          }`}
        >
          <Activity className="w-3.5 h-3.5" /> Live Log Feed ({events.length})
        </button>

        <button
          onClick={() => setSelectedTab('ships')}
          className={`flex items-center gap-2 px-4 py-2.5 text-xs font-semibold uppercase tracking-wider border-b-2 transition cursor-pointer ${
            selectedTab === 'ships'
              ? 'border-cyan-400 text-cyan-300 bg-cyan-950/20'
              : 'border-transparent text-slate-400 hover:text-slate-200'
          }`}
        >
          <Rocket className="w-3.5 h-3.5" /> Schiffe & Standorte
        </button>
      </div>

      {/* Tab Content Area */}
      <div className="flex-1 overflow-auto p-6">
        {selectedTab === 'sessions' && (
          <div className="sc-glass rounded-lg overflow-hidden border border-slate-800/80">
            <table className="w-full text-left text-xs border-collapse">
              <thead>
                <tr className="border-b border-slate-800 bg-slate-900/60 text-slate-400 font-mono uppercase tracking-wider">
                  <th className="py-3 px-4">Session / Datum</th>
                  <th className="py-3 px-4">Dauer</th>
                  <th className="py-3 px-4 text-right">Einnahmen</th>
                  <th className="py-3 px-4 text-right">Ausgaben</th>
                  <th className="py-3 px-4 text-right">Netto</th>
                  <th className="py-3 px-4">Schiffe</th>
                  <th className="py-3 px-4">Letzter Standort</th>
                  <th className="py-3 px-4 text-center">Status</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/50">
                {sessions.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="py-12 text-center text-slate-500 font-mono">
                      Keine Sessions gefunden. Klicke oben auf »Logs scannen«.
                    </td>
                  </tr>
                ) : (
                  sessions.map((sess) => (
                    <tr
                      key={sess.id}
                      className="hover:bg-cyan-950/20 transition-colors group cursor-pointer"
                    >
                      <td className="py-3 px-4">
                        <div className="font-semibold text-slate-200 group-hover:text-cyan-300 transition">
                          {sess.name}
                        </div>
                        <div className="text-[10px] text-slate-400 font-mono">
                          {sess.startTime} → {sess.endTime}
                        </div>
                      </td>
                      <td className="py-3 px-4 font-mono text-slate-300">{sess.duration}</td>
                      <td className="py-3 px-4 text-right font-mono text-emerald-400">
                        +{formatNumber(sess.income)}
                      </td>
                      <td className="py-3 px-4 text-right font-mono text-rose-400">
                        -{formatNumber(sess.spend)}
                      </td>
                      <td
                        className={`py-3 px-4 text-right font-mono font-bold ${
                          sess.net >= 0 ? 'text-cyan-300' : 'text-rose-400'
                        }`}
                      >
                        {formatNumber(sess.net)} aUEC
                      </td>
                      <td className="py-3 px-4">
                        {sess.ships && sess.ships.length > 0 ? (
                          <div className="flex flex-wrap gap-1">
                            {sess.ships.map((sh, i) => (
                              <span
                                key={i}
                                className="px-1.5 py-0.5 rounded text-[10px] bg-cyan-950/60 border border-cyan-800/40 text-cyan-300"
                              >
                                {sh}
                              </span>
                            ))}
                          </div>
                        ) : (
                          <span className="text-slate-600">—</span>
                        )}
                      </td>
                      <td className="py-3 px-4 text-slate-300 flex items-center gap-1.5">
                        <Compass className="w-3.5 h-3.5 text-cyan-400 shrink-0" />
                        <span className="truncate max-w-[180px]">{sess.lastLocation || '—'}</span>
                      </td>
                      <td className="py-3 px-4 text-center">
                        {sess.deaths > 0 ? (
                          <span className="sc-badge-red" title={`${sess.deaths}x Med-Bett`}>
                            {sess.deaths} Verlust
                          </span>
                        ) : (
                          <span className="sc-badge-green">Erfolgreich</span>
                        )}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}

        {selectedTab === 'live' && (
          <div className="sc-glass rounded-lg p-4 border border-slate-800/80 font-mono text-xs">
            <div className="flex items-center justify-between mb-3 pb-2 border-b border-slate-800">
              <span className="text-slate-400 font-semibold uppercase tracking-wider">
                Echtzeit-Log Stream
              </span>
              <span className="text-slate-500 text-[11px]">
                {events.length} Events in dieser Sitzung aufgezeichnet
              </span>
            </div>

            {events.length === 0 ? (
              <div className="py-16 text-center text-slate-500">
                <Radio className="w-8 h-8 text-cyan-500/40 mx-auto mb-2 animate-pulse" />
                <p>Warte auf neue Events aus dem Star Citizen Live-Log...</p>
                <p className="text-[11px] text-slate-600 mt-1">
                  Sobald im Spiel Transaktionen, Schiffssitzwechsel oder Standorte protokolliert werden, erscheinen sie hier live.
                </p>
              </div>
            ) : (
              <div className="space-y-2 max-h-[600px] overflow-y-auto pr-1">
                {events.map((ev) => (
                  <div
                    key={ev.id}
                    className="p-2.5 rounded bg-slate-900/60 border border-slate-800 hover:border-cyan-500/30 transition flex items-start justify-between"
                  >
                    <div className="flex items-start gap-3">
                      <span className="text-[11px] text-slate-500">{ev.timestamp}</span>
                      <div>
                        <div className="font-semibold text-slate-200">{ev.title}</div>
                        <div className="text-[11px] text-slate-400">{ev.description}</div>
                        {ev.rawText && (
                          <div className="text-[10px] text-slate-600 mt-1 font-mono">{ev.rawText}</div>
                        )}
                      </div>
                    </div>
                    {ev.amount !== undefined && (
                      <span
                        className={`font-mono font-bold ${
                          ev.amount >= 0 ? 'text-emerald-400' : 'text-rose-400'
                        }`}
                      >
                        {ev.amount >= 0 ? '+' : ''}
                        {formatNumber(ev.amount)} aUEC
                      </span>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {selectedTab === 'ships' && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="sc-glass rounded-lg p-4 border border-slate-800">
              <h3 className="text-sm font-semibold uppercase tracking-wider text-cyan-300 mb-3 flex items-center gap-2">
                <Rocket className="w-4 h-4 text-cyan-400" /> Registrierte Schiffe & Fahrzeuge
              </h3>
              <div className="space-y-2">
                {Array.from(new Set(sessions.flatMap((s) => s.ships || []))).map((ship, i) => (
                  <div
                    key={i}
                    className="flex items-center justify-between p-2.5 rounded bg-slate-900/50 border border-slate-800/80"
                  >
                    <span className="font-mono text-xs text-slate-200">{ship}</span>
                    <span className="sc-badge">Erfasst</span>
                  </div>
                ))}
              </div>
            </div>

            <div className="sc-glass rounded-lg p-4 border border-slate-800">
              <h3 className="text-sm font-semibold uppercase tracking-wider text-amber-300 mb-3 flex items-center gap-2">
                <Compass className="w-4 h-4 text-amber-400" /> Zuletzt besuchte Standorte
              </h3>
              <div className="space-y-2">
                {Array.from(new Set(sessions.map((s) => s.lastLocation).filter(Boolean))).map(
                  (loc, i) => (
                    <div
                      key={i}
                      className="flex items-center justify-between p-2.5 rounded bg-slate-900/50 border border-slate-800/80"
                    >
                      <span className="font-mono text-xs text-slate-200">{loc}</span>
                      <span className="sc-badge-gold">Station / Planet</span>
                    </div>
                  )
                )}
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Bottom Statusbar */}
      <footer className="flex items-center justify-between px-6 py-2 border-t border-slate-800 bg-[#030712]/90 text-[11px] font-mono text-slate-500">
        <div className="flex items-center gap-4">
          <span className="flex items-center gap-1.5">
            <HardDrive className="w-3 h-3 text-cyan-400" /> SQLite: SCLogMate.db
          </span>
          <span>·</span>
          <span>{sessions.length} Sessions indexiert</span>
        </div>
        <div className="flex items-center gap-3">
          <span>Photino.NET + React + Vite + Tailwind CSS</span>
          <span>·</span>
          <span className="text-cyan-400 font-semibold">{status?.version || 'v1.0.0-rc2'}</span>
        </div>
      </footer>
    </div>
  );
};

export default App;
