import React, { useEffect, useState } from 'react';
import { bridge, FleetStatDto } from '../services/photinoBridge';
import {
  Layers,
  RefreshCw,
  Rocket,
  ShieldAlert,
  Zap,
} from 'lucide-react';

export const FleetView: React.FC = () => {
  const [fleet, setFleet] = useState<FleetStatDto[]>([]);
  const [loading, setLoading] = useState<boolean>(false);

  const fetchFleet = async () => {
    try {
      setLoading(true);
      const res = await bridge.sendRequest<FleetStatDto[]>('get_fleet');
      setFleet(res);
    } catch (err) {
      console.error('Failed to load fleet data:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchFleet();
  }, []);

  const totalFlights = fleet.reduce((acc, s) => acc + s.flights, 0);
  const totalQts = fleet.reduce((acc, s) => acc + s.quantumJumps, 0);
  const totalLosses = fleet.reduce((acc, s) => acc + s.losses, 0);

  return (
    <div className="flex flex-col h-full space-y-4">
      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="sc-glass rounded-lg p-4 border border-slate-800 sc-hud-corner">
          <div className="flex justify-between items-start">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Registrierte Schiffe
            </span>
            <Rocket className="w-4 h-4 text-cyan-400" />
          </div>
          <div className="mt-2 text-2xl font-bold font-mono text-cyan-300">
            {fleet.length} <span className="text-xs font-normal text-slate-400">Schiffstypen</span>
          </div>
          <div className="mt-1 text-xs text-slate-400">Aktiv im Star Citizen Live-Log erfasst</div>
        </div>

        <div className="sc-glass rounded-lg p-4 border border-slate-800 sc-hud-corner">
          <div className="flex justify-between items-start">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Flug-Einsätze
            </span>
            <Layers className="w-4 h-4 text-amber-400" />
          </div>
          <div className="mt-2 text-2xl font-bold font-mono text-amber-300">
            {totalFlights} <span className="text-xs font-normal text-slate-400">Sitzungen</span>
          </div>
          <div className="mt-1 text-xs text-slate-400">Gesamte Einsätze über alle Sessions</div>
        </div>

        <div className="sc-glass rounded-lg p-4 border border-slate-800 sc-hud-corner">
          <div className="flex justify-between items-start">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Quantum-Sprünge
            </span>
            <Zap className="w-4 h-4 text-indigo-400" />
          </div>
          <div className="mt-2 text-2xl font-bold font-mono text-indigo-300">
            {totalQts} <span className="text-xs font-normal text-slate-400">Sprünge</span>
          </div>
          <div className="mt-1 text-xs text-slate-400">Erfolgreich initiierte QT-Reisen</div>
        </div>

        <div className="sc-glass rounded-lg p-4 border border-slate-800 sc-hud-corner">
          <div className="flex justify-between items-start">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Schiffsverluste
            </span>
            <ShieldAlert className="w-4 h-4 text-rose-400" />
          </div>
          <div className="mt-2 text-2xl font-bold font-mono text-rose-400">
            {totalLosses} <span className="text-xs font-normal text-slate-400">Zerstörungen</span>
          </div>
          <div className="mt-1 text-xs text-slate-400">Totalverluste durch Feindfeuer / Unfall</div>
        </div>
      </div>

      {/* Fleet Table */}
      <div className="flex-1 sc-glass rounded-lg border border-slate-800 overflow-hidden flex flex-col">
        <div className="p-3.5 border-b border-slate-800 flex items-center justify-between bg-slate-900/60">
          <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-cyan-300">
            <Rocket className="w-4 h-4 text-cyan-400" /> Schiffsflotte & Einsatzstatistiken
          </div>
          <button
            onClick={fetchFleet}
            className="p-1.5 rounded bg-slate-900 border border-slate-800 text-slate-400 hover:text-slate-200 cursor-pointer"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
          </button>
        </div>

        <div className="flex-1 overflow-y-auto">
          <table className="w-full text-left text-xs border-collapse">
            <thead>
              <tr className="border-b border-slate-800 bg-slate-900/60 text-slate-400 font-mono uppercase tracking-wider sticky top-0 backdrop-blur-md">
                <th className="py-3 px-4">Schiff / Bezeichnung</th>
                <th className="py-3 px-4 text-center">Flugeinsätze</th>
                <th className="py-3 px-4 text-center">QT-Sprünge</th>
                <th className="py-3 px-4 text-center">Verluste</th>
                <th className="py-3 px-4">Zuletzt gesteuert</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/40">
              {fleet.length === 0 ? (
                <tr>
                  <td colSpan={5} className="py-16 text-center text-slate-500 font-mono">
                    Keine Schiffsdaten in den Logs aufgezeichnet.
                  </td>
                </tr>
              ) : (
                fleet.map((s) => (
                  <tr key={s.shipName} className="hover:bg-slate-900/40 transition">
                    <td className="py-3 px-4">
                      <div className="font-semibold text-slate-200">{s.shipName}</div>
                    </td>
                    <td className="py-3 px-4 text-center font-mono text-cyan-300 font-semibold">
                      {s.flights}
                    </td>
                    <td className="py-3 px-4 text-center font-mono text-indigo-300">
                      {s.quantumJumps}
                    </td>
                    <td className="py-3 px-4 text-center">
                      {s.losses > 0 ? (
                        <span className="sc-badge-red text-[10px]">{s.losses}</span>
                      ) : (
                        <span className="text-slate-600 font-mono">0</span>
                      )}
                    </td>
                    <td className="py-3 px-4 font-mono text-slate-400">{s.lastUsed}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
