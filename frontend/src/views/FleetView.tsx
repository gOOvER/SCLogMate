import React, { useEffect, useState } from 'react';
import { bridge, FleetStatDto } from '../services/photinoBridge';
import {
  Coins,
  ExternalLink,
  Layers,
  RefreshCw,
  Rocket,
  Search,
  Zap,
} from 'lucide-react';

export const FleetView: React.FC = () => {
  const [fleet, setFleet] = useState<FleetStatDto[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [search, setSearch] = useState<string>('');

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

  // Geschätzter aUEC Flottenwert (ca. 8.5 Mio pro registriertem Schiffstyp)
  const estimatedFleetValue = fleet.length * 8500000;

  const filteredFleet = fleet.filter((s) => {
    if (!search) return true;
    return s.shipName.toLowerCase().includes(search.toLowerCase());
  });

  return (
    <div className="flex flex-col min-h-full space-y-3 font-sans select-none">
      {/* ══ KPI SUMMARY CARDS ══ */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-2.5 shrink-0">
        {/* Schiffe */}
        <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-lg p-2.5 flex flex-col justify-between shadow-sm">
          <div className="flex justify-between items-start">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
              Registrierte Schiffe
            </span>
            <Rocket className="w-3.5 h-3.5 text-cyan-400" />
          </div>
          <div className="mt-1 text-xl font-bold font-mono text-cyan-300">
            {fleet.length} <span className="text-[10px] font-normal text-cyan-500 font-sans">Schiffstypen</span>
          </div>
          <div className="text-[10px] font-mono text-slate-500 mt-0.5">Aktiv in den Live-Logs erfasst</div>
        </div>

        {/* Flug-Einsätze */}
        <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-lg p-2.5 flex flex-col justify-between shadow-sm">
          <div className="flex justify-between items-start">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
              Flug-Einsätze
            </span>
            <Layers className="w-3.5 h-3.5 text-amber-400" />
          </div>
          <div className="mt-1 text-xl font-bold font-mono text-amber-300">
            {totalFlights} <span className="text-[10px] font-normal text-amber-500 font-sans">Einsätze</span>
          </div>
          <div className="text-[10px] font-mono text-slate-500 mt-0.5">Summe über alle Sessions</div>
        </div>

        {/* Quantum-Sprünge */}
        <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-lg p-2.5 flex flex-col justify-between shadow-sm">
          <div className="flex justify-between items-start">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
              Quantum-Sprünge
            </span>
            <Zap className="w-3.5 h-3.5 text-sky-400" />
          </div>
          <div className="mt-1 text-xl font-bold font-mono text-sky-300">
            {totalQts} <span className="text-[10px] font-normal text-sky-500 font-sans">QT-Sprünge</span>
          </div>
          <div className="text-[10px] font-mono text-slate-500 mt-0.5">Erfolgreich initiierte Routen</div>
        </div>

        {/* Flottenwert Schätzung */}
        <div className="bg-[#051122]/90 border border-cyan-950/80 rounded-lg p-2.5 flex flex-col justify-between shadow-sm">
          <div className="flex justify-between items-start">
            <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-slate-400">
              Flotten-Gesamtwert
            </span>
            <Coins className="w-3.5 h-3.5 text-emerald-400" />
          </div>
          <div className="mt-1 text-xl font-bold font-mono text-emerald-400">
            ~{Math.round(estimatedFleetValue / 1000000)}M <span className="text-[10px] font-normal text-emerald-600 font-sans">aUEC</span>
          </div>
          <div className="text-[10px] font-mono text-slate-500 mt-0.5">
            {totalLosses} Verluste erfasst
          </div>
        </div>
      </div>

      {/* ══ TOOLBAR & FLEET TABLE ══ */}
      <div className="flex-1 bg-[#040914]/90 rounded-lg border border-cyan-950/80 flex flex-col overflow-hidden shadow-sm min-h-[350px]">
        <div className="p-2.5 border-b border-cyan-950 bg-[#061224] flex items-center justify-between gap-3 shrink-0">
          <div className="flex items-center gap-2 text-xs font-mono font-bold uppercase tracking-wider text-cyan-300">
            <Rocket className="w-3.5 h-3.5 text-cyan-400" /> Schiffsflotte & Hangar ({filteredFleet.length})
          </div>

          <div className="flex items-center gap-2">
            {/* Search */}
            <div className="relative w-56">
              <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-500" />
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Schiffsmodell filtern..."
                className="w-full bg-[#071322] border border-cyan-900/60 rounded pl-8 pr-3 py-1 text-xs font-mono text-slate-200 placeholder-slate-500 focus:outline-none focus:border-cyan-500"
              />
            </div>

            <button
              onClick={fetchFleet}
              title="Flotte aktualisieren"
              className="p-1 rounded bg-[#071322] border border-cyan-950 hover:border-cyan-800 text-slate-400 hover:text-cyan-300 cursor-pointer"
            >
              <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin text-cyan-400' : ''}`} />
            </button>
          </div>
        </div>

        {/* DataGrid */}
        <div className="flex-1 overflow-auto">
          <table className="w-full min-w-[700px] text-left text-xs border-collapse font-mono">
            <thead>
              <tr className="border-b border-cyan-950 bg-[#061224] text-slate-400 text-[10.5px] font-bold uppercase tracking-wider sticky top-0 backdrop-blur-md z-10">
                <th className="py-2.5 px-4 font-sans">Schiff / Modell</th>
                <th className="py-2.5 px-4 text-center">Flugeinsätze</th>
                <th className="py-2.5 px-4 text-center">QT-Sprünge</th>
                <th className="py-2.5 px-4 text-center">Verluste</th>
                <th className="py-2.5 px-4">Zuletzt geflogen</th>
                <th className="py-2.5 px-4 text-right font-sans">Aktion</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-cyan-950/40">
              {filteredFleet.length === 0 ? (
                <tr>
                  <td colSpan={6} className="py-16 text-center text-slate-500 font-mono">
                    Keine Schiffe für die Suche gefunden.
                  </td>
                </tr>
              ) : (
                filteredFleet.map((s) => (
                  <tr key={s.shipName} className="hover:bg-[#071628]/60 transition-colors group">
                    <td className="py-2.5 px-4">
                      <div className="font-semibold text-slate-200 group-hover:text-cyan-300 transition font-sans text-xs flex items-center gap-1.5">
                        <Rocket className="w-3.5 h-3.5 text-cyan-400 shrink-0" />
                        <span>{s.shipName}</span>
                      </div>
                    </td>
                    <td className="py-2.5 px-4 text-center font-bold text-cyan-300">
                      {s.flights}×
                    </td>
                    <td className="py-2.5 px-4 text-center text-sky-400">
                      {s.quantumJumps}
                    </td>
                    <td className="py-2.5 px-4 text-center">
                      {s.losses > 0 ? (
                        <span className="px-1.5 py-0.5 rounded bg-rose-950/60 border border-rose-800/60 text-rose-300 text-[10px] font-bold">
                          {s.losses}
                        </span>
                      ) : (
                        <span className="text-slate-600">0</span>
                      )}
                    </td>
                    <td className="py-2.5 px-4 text-slate-400 text-[11px]">{s.lastUsed}</td>
                    <td className="py-2.5 px-4 text-right">
                      <a
                        href={`https://star-citizen.wiki/${encodeURIComponent(s.shipName)}`}
                        target="_blank"
                        rel="noreferrer"
                        className="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-[#071322] hover:bg-cyan-950/60 border border-cyan-950 hover:border-cyan-800 text-[11px] text-cyan-400 hover:text-cyan-200 transition"
                      >
                        <span>Wiki</span>
                        <ExternalLink className="w-2.5 h-2.5" />
                      </a>
                    </td>
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

export default FleetView;
