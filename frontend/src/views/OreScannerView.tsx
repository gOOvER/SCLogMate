import React, { useEffect, useState, useMemo } from 'react';
import { bridge, RsResourceDto, RsMatchDto } from '../services/photinoBridge';
import {
  Search,
  Radar,
  HelpCircle,
} from 'lucide-react';

export const OreScannerView: React.FC = () => {
  const [resources, setResources] = useState<RsResourceDto[]>([]);
  const [inputRs, setInputRs] = useState<string>('2000');
  const [matches, setMatches] = useState<RsMatchDto[]>([]);
  const [search, setSearch] = useState<string>('');
  const [tierFilter, setTierFilter] = useState<string>('all');
  const [methodFilter, setMethodFilter] = useState<string>('all');

  const fetchSignatures = async () => {
    try {
      const res = await bridge.sendRequest<RsResourceDto[]>('get_rs_signatures');
      setResources(res || []);
    } catch (err) {
      console.error('Failed to load RS signatures:', err);
    }
  };

  const decodeRs = async (valStr: string) => {
    const val = parseInt(valStr.replace(/\D/g, ''), 10);
    if (isNaN(val) || val <= 0) {
      setMatches([]);
      return;
    }
    try {
      const res = await bridge.sendRequest<RsMatchDto[]>('decode_rs', { rs: val });
      setMatches(res || []);
    } catch (err) {
      console.error('Failed to decode RS value:', err);
    }
  };

  useEffect(() => {
    fetchSignatures();
    decodeRs(inputRs);
  }, []);

  const handleInputChange = (val: string) => {
    setInputRs(val);
    decodeRs(val);
  };

  const quickPresets = [
    { label: '2.000 RS (Salvage Panels)', value: '2000' },
    { label: '3.000 RS (FPS Gems)', value: '3000' },
    { label: '3.400 RS (Lindinium)', value: '3400' },
    { label: '4.000 RS (ROC Gems)', value: '4000' },
    { label: '6.000 RS (Quantanium)', value: '6000' },
    { label: '7.200 RS (Gold / Bexalite)', value: '7200' },
    { label: '14.400 RS (2x Gold Cluster)', value: '14400' },
  ];

  const filteredResources = useMemo(() => {
    return resources.filter((r) => {
      if (tierFilter !== 'all' && r.tier.toUpperCase() !== tierFilter.toUpperCase()) return false;
      if (methodFilter !== 'all' && r.method.toLowerCase() !== methodFilter.toLowerCase()) return false;
      if (search.trim()) {
        const q = search.toLowerCase();
        return (
          r.name.toLowerCase().includes(q) ||
          r.locations?.some((loc) => loc.toLowerCase().includes(q))
        );
      }
      return true;
    });
  }, [resources, tierFilter, methodFilter, search]);

  const getTierBadge = (tier: string) => {
    switch (tier.toUpperCase()) {
      case 'S':
        return 'bg-amber-500/20 text-amber-300 border-amber-500/40 shadow-[0_0_8px_rgba(245,158,11,0.2)]';
      case 'A':
        return 'bg-purple-500/20 text-purple-300 border-purple-500/40';
      case 'B':
        return 'bg-cyan-500/20 text-cyan-300 border-cyan-500/40';
      default:
        return 'bg-slate-800 text-slate-400 border-slate-700';
    }
  };

  return (
    <div className="flex flex-col min-h-full space-y-4">
      {/* Top RS Decoder / Calculator Tool */}
      <div className="sc-glass rounded-lg p-4 border border-slate-800 sc-hud-corner space-y-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <Radar className="w-5 h-5 text-cyan-400" />
            <div>
              <h2 className="text-sm font-bold text-slate-100 uppercase tracking-wider font-mono">
                RS Signal Radar Decoder
              </h2>
              <div className="text-[11px] text-slate-400">
                Ermittle sofort Erz- und Salvage-Zusammensetzung anhand der gescannten Radarsignatur
              </div>
            </div>
          </div>

          {/* Quick preset chips */}
          <div className="flex items-center gap-1.5 overflow-x-auto">
            {quickPresets.map((p) => (
              <button
                key={p.value}
                onClick={() => handleInputChange(p.value)}
                className={`px-2.5 py-1 text-[11px] font-mono rounded border transition cursor-pointer shrink-0 ${
                  inputRs === p.value
                    ? 'bg-cyan-500/20 text-cyan-300 border-cyan-500/40 shadow-[0_0_8px_rgba(0,240,255,0.2)]'
                    : 'bg-slate-900 border-slate-800 text-slate-400 hover:text-slate-200'
                }`}
              >
                {p.label}
              </button>
            ))}
          </div>
        </div>

        {/* Decoder Input & Result Cards */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3 items-center">
          {/* Big Input Box */}
          <div className="sc-glass rounded-lg p-3 border border-slate-800">
            <span className="text-[10px] font-mono text-slate-500 uppercase tracking-wider block mb-1">
              Gescannter RS-Wert (Signalstärke)
            </span>
            <div className="flex items-center gap-2">
              <input
                type="text"
                value={inputRs}
                onChange={(e) => handleInputChange(e.target.value)}
                placeholder="z. B. 2000, 6000, 7200"
                className="bg-slate-900 border border-slate-700 rounded px-3 py-2 text-xl font-bold font-mono text-cyan-300 focus:outline-none focus:border-cyan-500 w-full"
              />
              <span className="text-sm font-mono text-slate-500 font-bold">RS</span>
            </div>
          </div>

          {/* Matches Output List */}
          <div className="md:col-span-2">
            {matches.length === 0 ? (
              <div className="sc-glass rounded-lg p-3 border border-slate-800 text-xs text-slate-500 flex items-center gap-2">
                <HelpCircle className="w-4 h-4 text-slate-600" />
                <span>Keine bekannte RS-Signatur gefunden. Versuche ein Vielfaches wie 2000, 3400 oder 6000.</span>
              </div>
            ) : (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                {matches.map((m, idx) => (
                  <div
                    key={`${m.resourceName}-${idx}`}
                    className="sc-glass rounded-lg p-3 border border-cyan-500/30 shadow-[0_0_10px_rgba(0,240,255,0.05)] flex items-center justify-between"
                  >
                    <div>
                      <div className="flex items-center gap-1.5">
                        <span className={`px-1.5 py-0.2 text-[10px] font-mono font-bold rounded border ${getTierBadge(m.tier)}`}>
                          Tier {m.tier}
                        </span>
                        <h4 className="text-xs font-bold text-slate-100">{m.resourceName}</h4>
                      </div>
                      <div className="text-[11px] text-slate-400 font-mono mt-0.5">
                        {m.nodes}x Knoten · Basis: {m.baseRs.toLocaleString('de-DE')} RS
                      </div>
                    </div>

                    <div className="text-right">
                      <div className="text-xs font-mono font-bold text-emerald-400">
                        {m.estimatedPricePerScu.toLocaleString('de-DE')} aUEC
                      </div>
                      <span className="text-[9px] font-mono text-slate-500">pro SCU</span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Catalog Table Filter Bar */}
      <div className="sc-glass rounded-lg p-3 border border-slate-800 flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          {/* Tier filter */}
          <select
            value={tierFilter}
            onChange={(e) => setTierFilter(e.target.value)}
            className="bg-slate-900 border border-slate-800 rounded px-2.5 py-1.5 text-xs text-slate-200 focus:outline-none focus:border-cyan-500 cursor-pointer font-mono"
          >
            <option value="all">Alle Tiers</option>
            <option value="S">Tier S (Top Wert)</option>
            <option value="A">Tier A (Hochwertig)</option>
            <option value="B">Tier B (Mittel)</option>
            <option value="C">Tier C (Standard)</option>
          </select>

          {/* Method filter */}
          <select
            value={methodFilter}
            onChange={(e) => setMethodFilter(e.target.value)}
            className="bg-slate-900 border border-slate-800 rounded px-2.5 py-1.5 text-xs text-slate-200 focus:outline-none focus:border-cyan-500 cursor-pointer font-mono"
          >
            <option value="all">Alle Methoden</option>
            <option value="ship">Schiff-Mining</option>
            <option value="fps">FPS / Handabbau</option>
            <option value="salvage">Salvage / Hülle</option>
          </select>
        </div>

        <div className="flex items-center gap-2">
          <div className="relative">
            <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-500" />
            <input
              type="text"
              placeholder="Ressource oder Fundort suchen..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="bg-slate-900 border border-slate-800 rounded pl-8 pr-3 py-1.5 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-cyan-500 w-56 font-mono"
            />
          </div>
        </div>
      </div>

      {/* Catalog Table */}
      <div className="flex-1 overflow-auto pr-1">
        <div className="sc-glass rounded-lg border border-slate-800 overflow-auto">
          <table className="w-full min-w-[650px] text-left border-collapse text-xs font-mono">
            <thead>
              <tr className="border-b border-slate-800 bg-slate-950/60 text-slate-400 text-[11px]">
                <th className="py-2.5 px-3">RESSOURCE</th>
                <th className="py-2.5 px-3">TIER</th>
                <th className="py-2.5 px-3">BASIS RS</th>
                <th className="py-2.5 px-3">METHODE</th>
                <th className="py-2.5 px-3 text-right">Ø PREIS / SCU</th>
                <th className="py-2.5 px-3">HAUPTFUNDORTE</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/60">
              {filteredResources.map((res) => (
                <tr
                  key={res.name}
                  onClick={() => handleInputChange(res.baseRs.toString())}
                  className="hover:bg-slate-900/60 transition cursor-pointer group"
                >
                  <td className="py-2 px-3 font-bold text-slate-200 group-hover:text-cyan-300">
                    {res.name}
                  </td>
                  <td className="py-2 px-3">
                    <span className={`px-2 py-0.5 text-[10px] font-bold rounded border ${getTierBadge(res.tier)}`}>
                      {res.tier}
                    </span>
                  </td>
                  <td className="py-2 px-3 text-cyan-400 font-bold">
                    {res.baseRs.toLocaleString('de-DE')} RS
                  </td>
                  <td className="py-2 px-3 text-slate-400 capitalize">
                    {res.method}
                  </td>
                  <td className="py-2 px-3 text-right text-emerald-400 font-bold">
                    {res.estimatedPricePerScu > 0 ? `${res.estimatedPricePerScu.toLocaleString('de-DE')} aUEC` : '—'}
                  </td>
                  <td className="py-2 px-3 text-slate-400 text-[11px] truncate max-w-xs">
                    {res.locations?.join(', ') || 'Stanton & Pyro Asteroiden'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
