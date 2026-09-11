import React, { useEffect, useState, useMemo } from 'react';
import { bridge, FlightRecorderDto, SessionSummary } from '../services/photinoBridge';
import {
  MapPin,
  Rocket,
  Compass,
  AlertTriangle,
  Copy,
  Check,
  Radio,
  Navigation,
  ChevronDown,
  Zap,
} from 'lucide-react';

export const BlackboxView: React.FC = () => {
  const [data, setData] = useState<FlightRecorderDto | null>(null);
  const [filterKind, setFilterKind] = useState<string>('all');
  const [copied, setCopied] = useState<boolean>(false);
  const [sessions, setSessions] = useState<SessionSummary[]>([]);
  const [selectedSession, setSelectedSession] = useState<string>('__live__');

  useEffect(() => {
    bridge.sendRequest<SessionSummary[]>('get_sessions').then((res) => {
      if (res) setSessions(res);
    }).catch(() => {});
  }, []);

  const fetchBlackbox = async (targetSession = selectedSession) => {
    try {
      const res = await bridge.sendRequest<FlightRecorderDto>('get_blackbox', { session: targetSession });
      setData(res);
    } catch (err) {
      console.error('Failed to load flight recorder data:', err);
    }
  };

  useEffect(() => {
    fetchBlackbox(selectedSession);
  }, [selectedSession]);

  const timeline = data?.timeline || [];

  const filteredTimeline = useMemo(() => {
    return timeline.filter((item) => {
      if (filterKind === 'all') return true;
      if (filterKind === 'quantum' && item.kind === 'quantum') return true;
      if (filterKind === 'vehicle' && item.kind === 'vehicle') return true;
      if (filterKind === 'loss' && (item.kind === 'shiploss' || item.kind === 'crash')) return true;
      if (filterKind === 'location' && item.kind === 'location') return true;
      return false;
    });
  }, [timeline, filterKind]);

  const handleCopyReport = () => {
    if (!data) return;
    const lines = [
      '# Star Citizen Flugschreiber Telemetrie-Bericht',
      `*Stand: ${new Date().toLocaleString('de-DE')}*`,
      '',
      `**Gesamtdistanz:** ${data.totalDistanceText}`,
      `**Flugdauer:** ${data.flightDurationText}`,
      `**Quantum-Sprünge:** ${data.quantumJumps}`,
      `**Schiffseinsätze:** ${data.sortieCount}`,
      `**Schiffsverluste:** ${data.shipLosses}`,
      `**Eingesetzte Schiffe:** ${data.usedShips.join(', ') || 'Keine'}`,
      `**Besuchte Himmelskörper:** ${data.visitedBodies.join(', ') || 'Keine'}`,
      '',
      '### Chronologischer Flugverlauf',
      ...timeline.map(
        (t) => `- [${t.time}] ${t.title}: ${t.subtitle}${t.ship ? ` (${t.ship})` : ''}`
      ),
    ];

    navigator.clipboard.writeText(lines.join('\n')).then(() => {
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    });
  };

  const getEventIcon = (kind: string) => {
    switch (kind) {
      case 'quantum':
        return <Navigation className="w-4 h-4 text-cyan-400" />;
      case 'vehicle':
        return <Rocket className="w-4 h-4 text-emerald-400" />;
      case 'shiploss':
      case 'crash':
        return <AlertTriangle className="w-4 h-4 text-rose-400" />;
      case 'location':
        return <MapPin className="w-4 h-4 text-amber-400" />;
      default:
        return <Radio className="w-4 h-4 text-slate-400" />;
    }
  };

  return (
    <div className="flex flex-col min-h-full space-y-4 select-none">
      {/* Session Filter Bar */}
      <div className="flex flex-wrap items-center justify-between gap-3 px-3 py-2 rounded-lg bg-[#040914]/90 border border-cyan-950/80 backdrop-blur-md">
        <div className="flex items-center gap-2.5">
          <span className="text-xs font-mono text-cyan-400 font-bold uppercase flex items-center gap-1.5">
            <Zap className="w-3.5 h-3.5 text-cyan-400" />
            Flugschreiber Sitzung:
          </span>
          <div className="relative">
            <select
              value={selectedSession}
              onChange={(e) => setSelectedSession(e.target.value)}
              className="appearance-none bg-[#071322] border border-cyan-900/70 hover:border-cyan-500 rounded px-2.5 py-1 pr-7 text-xs font-mono text-slate-200 focus:outline-none focus:border-cyan-400 transition cursor-pointer max-w-xs"
            >
              <option value="__live__">🔴 Live-Sitzung (Game.log)</option>
              <option value="__all__">🌐 Alle Flüge (Gesamthistorie)</option>
              {sessions.map((s) => (
                <option key={s.id || s.name} value={s.name}>
                  📁 {s.name} ({s.startTime})
                </option>
              ))}
            </select>
            <ChevronDown className="w-3.5 h-3.5 text-slate-400 absolute right-2 top-1/2 -translate-y-1/2 pointer-events-none" />
          </div>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={handleCopyReport}
            title="Bericht als Markdown kopieren"
            className="flex items-center gap-1.5 px-2.5 py-1 rounded bg-[#071322] border border-slate-800 hover:border-cyan-500 text-xs font-mono text-slate-300 hover:text-cyan-300 transition cursor-pointer"
          >
            {copied ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
            <span className="hidden sm:inline">Kopieren</span>
          </button>
        </div>
      </div>

      {/* Top Telemetry KPIs */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
        <div className="sc-glass rounded-lg p-3 border border-slate-800 flex items-center justify-between">
          <div>
            <div className="text-xs text-slate-400 font-mono tracking-wider">GESAMTDISTANZ</div>
            <div className="text-lg font-bold text-cyan-400 mt-0.5">
              {data?.totalDistanceText || '0 GM'}
            </div>
          </div>
          <div className="p-2.5 rounded-lg bg-cyan-500/10 text-cyan-400 border border-cyan-500/20">
            <Compass className="w-5 h-5" />
          </div>
        </div>

        <div className="sc-glass rounded-lg p-3 border border-slate-800 flex items-center justify-between">
          <div>
            <div className="text-xs text-slate-400 font-mono tracking-wider">QUANTUM-SPRÜNGE</div>
            <div className="text-lg font-bold text-emerald-400 mt-0.5">
              {data?.quantumJumps ?? 0}
            </div>
          </div>
          <div className="p-2.5 rounded-lg bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
            <Navigation className="w-5 h-5" />
          </div>
        </div>

        <div className="sc-glass rounded-lg p-3 border border-slate-800 flex items-center justify-between">
          <div>
            <div className="text-xs text-slate-400 font-mono tracking-wider">SCHIFFSVERLUSTE</div>
            <div className="text-lg font-bold text-rose-400 mt-0.5">
              {data?.shipLosses ?? 0}
            </div>
          </div>
          <div className="p-2.5 rounded-lg bg-rose-500/10 text-rose-400 border border-rose-500/20">
            <AlertTriangle className="w-5 h-5" />
          </div>
        </div>

        <div className="sc-glass rounded-lg p-3 border border-slate-800 flex items-center justify-between">
          <div>
            <div className="text-xs text-slate-400 font-mono tracking-wider">FLUGDAUER</div>
            <div className="text-lg font-bold text-amber-400 mt-0.5">
              {data?.flightDurationText || '0h 0m'}
            </div>
          </div>
          <div className="flex items-center gap-1.5">
            <button
              onClick={handleCopyReport}
              title="Bericht als Markdown kopieren"
              className="p-2 rounded-md border border-slate-700 hover:border-cyan-500/50 hover:bg-cyan-500/10 text-slate-400 hover:text-cyan-400 transition cursor-pointer"
            >
              {copied ? <Check className="w-4 h-4 text-emerald-400" /> : <Copy className="w-4 h-4" />}
            </button>
          </div>
        </div>
      </div>

      {/* Meta Chips: Visited Bodies & Used Ships */}
      <div className="sc-glass rounded-lg p-3 border border-slate-800 flex flex-wrap items-center justify-between gap-3 text-xs">
        <div className="flex items-center gap-3 flex-wrap">
          <div className="flex items-center gap-1.5">
            <span className="text-slate-500 font-mono">Himmelskörper:</span>
            {data?.visitedBodies && data.visitedBodies.length > 0 ? (
              data.visitedBodies.map((b) => (
                <span key={b} className="px-2 py-0.5 rounded bg-cyan-950/40 border border-cyan-500/30 text-cyan-300 font-mono text-[11px]">
                  {b}
                </span>
              ))
            ) : (
              <span className="text-slate-500">Keine</span>
            )}
          </div>

          <div className="flex items-center gap-1.5">
            <span className="text-slate-500 font-mono">Schiffe:</span>
            {data?.usedShips && data.usedShips.length > 0 ? (
              data.usedShips.map((s) => (
                <span key={s} className="px-2 py-0.5 rounded bg-amber-950/40 border border-amber-500/30 text-amber-300 font-mono text-[11px]">
                  {s}
                </span>
              ))
            ) : (
              <span className="text-slate-500">Keine</span>
            )}
          </div>
        </div>

        {/* Filter Pills */}
        <div className="flex items-center bg-slate-900/80 p-1 rounded-md border border-slate-800">
          <button
            onClick={() => setFilterKind('all')}
            className={`px-2.5 py-1 text-xs font-semibold rounded cursor-pointer transition ${
              filterKind === 'all'
                ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40'
                : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            Alle ({timeline.length})
          </button>
          <button
            onClick={() => setFilterKind('quantum')}
            className={`px-2.5 py-1 text-xs font-semibold rounded cursor-pointer transition ${
              filterKind === 'quantum'
                ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40'
                : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            Quantum
          </button>
          <button
            onClick={() => setFilterKind('vehicle')}
            className={`px-2.5 py-1 text-xs font-semibold rounded cursor-pointer transition ${
              filterKind === 'vehicle'
                ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/40'
                : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            Spawns
          </button>
          <button
            onClick={() => setFilterKind('loss')}
            className={`px-2.5 py-1 text-xs font-semibold rounded cursor-pointer transition ${
              filterKind === 'loss'
                ? 'bg-rose-500/20 text-rose-300 border border-rose-500/40'
                : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            Verluste
          </button>
        </div>
      </div>

      {/* Flight Timeline List */}
      <div className="flex-1 overflow-y-auto pr-1">
        {filteredTimeline.length === 0 ? (
          <div className="sc-glass rounded-lg p-12 border border-slate-800 text-center flex flex-col items-center justify-center space-y-2">
            <Compass className="w-10 h-10 text-slate-600" />
            <div className="text-slate-300 font-medium">Keine Flugereignisse erfasst</div>
            <div className="text-xs text-slate-500">Starte das Spiel und fliege Quantum-Routen, um Telemetriedaten aufzuzeichnen.</div>
          </div>
        ) : (
          <div className="space-y-2">
            {filteredTimeline.map((item) => {
              return (
                <div
                  key={item.id}
                  className={`sc-glass rounded-lg p-3 border transition-all duration-200 flex items-start justify-between gap-3 ${
                    item.isMajor
                      ? 'border-cyan-500/30 hover:border-cyan-500/60 shadow-[0_0_10px_rgba(0,240,255,0.05)]'
                      : 'border-slate-800 hover:border-slate-700'
                  }`}
                >
                  <div className="flex items-start gap-3">
                    <div className="p-2 rounded-lg bg-slate-900 border border-slate-800 shrink-0 mt-0.5">
                      {getEventIcon(item.kind)}
                    </div>

                    <div>
                      <div className="flex items-center gap-2">
                        <span className="text-xs font-bold text-slate-100">{item.title}</span>
                        {item.ship && (
                          <span className="px-2 py-0.2 text-[10px] font-mono rounded bg-slate-800 text-cyan-300 border border-slate-700">
                            {item.ship}
                          </span>
                        )}
                      </div>

                      <div className="text-xs text-slate-400 mt-1 leading-relaxed">
                        {item.subtitle}
                      </div>

                      {item.location && (
                        <div className="text-[10px] font-mono text-slate-500 mt-1 flex items-center gap-1">
                          <MapPin className="w-3 h-3 text-cyan-400" />
                          <span>{item.location}</span>
                        </div>
                      )}
                    </div>
                  </div>

                  <div className="text-right shrink-0">
                    <div className="text-xs font-mono font-bold text-slate-300">{item.time}</div>
                    <div className="text-[10px] font-mono text-slate-500 mt-0.5">{item.relativeTime}</div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
};
