import React, { useEffect, useState, useMemo } from 'react';
import {
  bridge,
  RsResourceDto,
  RsMatchDto,
} from '../services/photinoBridge';
import {
  Search,
  Radar,
  Crop,
  Zap,
} from 'lucide-react';

export const OreScannerView: React.FC = () => {
  // Radar State
  const [resources, setResources] = useState<RsResourceDto[]>([]);
  const [inputRs, setInputRs] = useState<string>('2000');
  const [matches, setMatches] = useState<RsMatchDto[]>([]);
  const [search, setSearch] = useState<string>('');
  const [tierFilter, setTierFilter] = useState<string>('all');
  const [methodFilter, setMethodFilter] = useState<string>('all');
  const [isSelectingRegion, setIsSelectingRegion] = useState(false);
  const [isTestingScan, setIsTestingScan] = useState(false);
  const [toastMsg, setToastMsg] = useState<string | null>(null);

  const showToast = (msg: string) => {
    setToastMsg(msg);
    setTimeout(() => setToastMsg(null), 4000);
  };

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

  const handleSelectRegion = async () => {
    try {
      setIsSelectingRegion(true);
      showToast('Bildschirm-Auswahl: Ziehe mit der Maus ein Rechteck über dein HUD-Fadenkreuz / RS-Signal...');
      const res = await bridge.sendRequest<any>('select_ocr_region', { target: 'rs' });
      if (res?.success && res.region) {
        showToast(`✓ RS-Scanbereich gespeichert: ${res.region.width}×${res.region.height} @ (${res.region.x}, ${res.region.y})`);
      } else if (res?.cancelled) {
        showToast('Auswahl abgebrochen');
      }
    } catch (err) {
      console.error('Failed to select RS region:', err);
      showToast('Fehler bei der Bildschirmauswahl');
    } finally {
      setIsSelectingRegion(false);
    }
  };

  const handleTestScan = async () => {
    try {
      setIsTestingScan(true);
      const res = await bridge.sendRequest<any>('test_ocr_scan', { target: 'rs' });
      if (res?.success && res.extractedValue != null) {
        showToast(`✓ RS-Signatur erkannt: ${res.extractedValue.toLocaleString('de-DE')} RS (${res.durationMs}ms)`);
        handleInputChange(res.extractedValue.toString());
      } else if (res?.recognizedText && res.recognizedText !== '(Kein Text erkannt)') {
        showToast(`Text erfasst: '${res.recognizedText}' (keine RS-Ziffer gefunden)`);
      } else {
        showToast('⚠️ Keine RS-Signatur im Scanbereich erkannt');
      }
    } catch (err) {
      console.error('Test scan failed:', err);
      showToast('Fehler beim OCR Test-Scan');
    } finally {
      setIsTestingScan(false);
    }
  };

  const filteredResources = useMemo(() => {
    return resources.filter((r) => {
      if (tierFilter !== 'all' && r.tier.toLowerCase() !== tierFilter.toLowerCase()) return false;
      if (methodFilter !== 'all' && r.method.toLowerCase() !== methodFilter.toLowerCase()) return false;
      if (search.trim()) {
        const q = search.toLowerCase();
        return (
          r.name.toLowerCase().includes(q) ||
          r.tier.toLowerCase().includes(q) ||
          r.method.toLowerCase().includes(q) ||
          r.locations.some((l) => l.toLowerCase().includes(q))
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
      case 'C':
        return 'bg-emerald-500/20 text-emerald-300 border-emerald-500/40';
      default:
        return 'bg-slate-800 text-slate-400 border-slate-700';
    }
  };

  const quickPresets = [
    { label: '2.000 RS (Salvage Panels)', value: '2000' },
    { label: '3.000 RS (FPS Gems)', value: '3000' },
    { label: '14.000 RS (Mining Cluster)', value: '14000' },
    { label: '18.000 RS (Großes Vorkommen)', value: '18000' },
  ];

  return (
    <div className="flex flex-col min-h-full space-y-4">
      {/* Toast */}
      {toastMsg && (
        <div className="fixed top-4 right-4 z-50 bg-cyan-950/90 text-cyan-200 border border-cyan-500 px-4 py-2 rounded shadow-lg text-xs font-mono animate-in fade-in slide-in-from-top-2">
          {toastMsg}
        </div>
      )}

      {/* RADAR DECODER CONTENT */}
      <div className="flex-1 flex flex-col space-y-4 overflow-y-auto pr-1">
        {/* Top Decoder Bar */}
        <div className="sc-glass rounded-lg p-4 border border-slate-800 sc-hud-corner space-y-3">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div className="flex items-center gap-2">
              <Radar className="w-5 h-5 text-cyan-400" />
              <h2 className="text-sm font-bold text-slate-100 uppercase tracking-wider font-mono">
                Radar-Signatur Decoder (Ping-Erkennung)
              </h2>
            </div>

            {/* OCR Controls */}
            <div className="flex items-center gap-2">
              <button
                onClick={handleSelectRegion}
                disabled={isSelectingRegion}
                className="flex items-center gap-1.5 px-3 py-1.5 rounded bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-mono transition cursor-pointer border border-slate-700"
                title="Wähle den Bildschirmausschnitt deines Schiffs-HUDs mit dem RS-Signalwert"
              >
                <Crop className="w-3.5 h-3.5 text-cyan-400" />
                {isSelectingRegion ? 'Auswahl läuft...' : 'RS-Bereich markieren'}
              </button>

              <button
                onClick={handleTestScan}
                disabled={isTestingScan}
                className="flex items-center gap-1.5 px-3 py-1.5 rounded bg-cyan-600 hover:bg-cyan-500 text-white text-xs font-mono font-semibold transition cursor-pointer shadow-[0_0_12px_rgba(0,240,255,0.25)]"
                title="Testet den OCR-Scan im gewählten Bereich"
              >
                <Zap className="w-3.5 h-3.5" />
                {isTestingScan ? 'Scannt...' : 'HUD Scan'}
              </button>
            </div>
          </div>

          <p className="text-xs text-slate-400 font-mono">
            Gib die erkannte RS-Signatur aus deinem Schiffs-Ping ein (oder nutze OCR), um zu decodieren, ob es sich um lukratives Quantainium, Gold, FPS-Gems oder Trümmerteile handelt.
          </p>

          {/* Input & Presets */}
          <div className="flex flex-wrap items-center gap-3 pt-1">
            <div className="relative flex-1 min-w-[200px] max-w-sm">
              <input
                type="text"
                value={inputRs}
                onChange={(e) => handleInputChange(e.target.value)}
                placeholder="z.B. 2000 oder 14000"
                className="w-full bg-slate-900/90 border border-cyan-500/40 rounded px-3 py-2 text-sm font-mono text-cyan-300 placeholder-slate-600 focus:outline-none focus:border-cyan-400 focus:ring-1 focus:ring-cyan-400"
              />
              <span className="absolute right-3 top-2.5 text-xs font-mono text-slate-500">RS</span>
            </div>

            {/* Quick Presets */}
            <div className="flex items-center gap-1.5 flex-wrap">
              {quickPresets.map((p) => (
                <button
                  key={p.value}
                  onClick={() => handleInputChange(p.value)}
                  className={`px-2.5 py-1.5 rounded text-[11px] font-mono transition cursor-pointer border ${
                    inputRs === p.value
                      ? 'bg-cyan-500/20 text-cyan-300 border-cyan-500/50 shadow-[0_0_8px_rgba(0,240,255,0.2)]'
                      : 'bg-slate-900/60 text-slate-400 border-slate-800 hover:text-slate-200 hover:border-slate-700'
                  }`}
                >
                  {p.label}
                </button>
              ))}
            </div>
          </div>
        </div>

        {/* Decoder Matches Results */}
        {matches.length > 0 ? (
          <div className="space-y-2">
            <h3 className="text-xs font-bold text-slate-400 uppercase tracking-wider font-mono flex items-center gap-2">
              <span>Erkannte Signaturen & Wahrscheinlichkeiten</span>
              <span className="px-1.5 py-0.2 rounded bg-cyan-950 text-cyan-400 text-[10px] border border-cyan-800">
                {matches.length} Treffer
              </span>
            </h3>

            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3">
              {matches.map((m, idx) => (
                <div
                  key={idx}
                  className="sc-glass rounded-lg p-3.5 border border-slate-800 hover:border-cyan-500/40 transition flex flex-col justify-between sc-hud-corner group"
                >
                  <div>
                    {/* Header */}
                    <div className="flex items-start justify-between gap-2">
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-bold text-slate-100 text-sm group-hover:text-cyan-300 transition">
                            {m.resourceName}
                          </span>
                          <span
                            className={`px-1.5 py-0.5 text-[9px] font-mono font-bold rounded border uppercase ${getTierBadge(
                              m.tier
                            )}`}
                          >
                            Tier {m.tier}
                          </span>
                        </div>
                        <div className="text-[11px] font-mono text-slate-400 mt-0.5">{m.method} · {m.rarity}</div>
                      </div>

                      {/* Match Badge */}
                      <div className="text-right">
                        <span className={`text-xs font-mono font-bold ${m.isExact ? 'text-emerald-400' : 'text-amber-400'}`}>
                          {m.isExact ? '✓ Exakter Treffer' : `±${Math.round(m.errorPct)}% Abweichung`}
                        </span>
                        <div className="text-[10px] text-slate-500 font-mono">
                          Basis: {m.baseRs.toLocaleString('de-DE')} RS
                        </div>
                      </div>
                    </div>

                    {/* Nodes & Cluster Details */}
                    <div className="mt-3 p-2 rounded bg-slate-900/60 border border-slate-800/80 space-y-1 font-mono text-xs">
                      <div className="flex justify-between text-[11px]">
                        <span className="text-slate-400">Felsen-Anzahl:</span>
                        <span className="text-cyan-300 font-bold">{m.nodes}× Felsen / Cluster</span>
                      </div>
                      <div className="flex justify-between text-[11px]">
                        <span className="text-slate-400">Richtpreis / SCU:</span>
                        <span className="text-emerald-400 font-medium">
                          {m.estimatedPricePerScu > 0 ? `${m.estimatedPricePerScu.toLocaleString('de-DE')} aUEC` : '—'}
                        </span>
                      </div>
                    </div>

                    {/* Cluster Value / Footer */}
                    <div className="mt-3 pt-2 border-t border-slate-800/60 flex items-center justify-between text-[10px] font-mono text-slate-500">
                      <span>Geschätzter Cluster-Wert:</span>
                      <span className="text-emerald-300 font-bold">
                        {m.estimatedClusterValue > 0 ? `~${m.estimatedClusterValue.toLocaleString('de-DE')} aUEC` : '—'}
                      </span>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        ) : (
          inputRs && (
            <div className="sc-glass rounded-lg p-6 border border-slate-800 text-center text-xs font-mono text-slate-400">
              Kein direkter Treffer für <span className="text-cyan-300 font-bold">{inputRs} RS</span> gefunden. Bitte prüfe, ob die Zahl durch Cluster-Ping verdoppelt/vervielfacht ist (z.B. Vielfache von 2.000 oder 1.700).
            </div>
          )
        )}

        {/* RESOURCE DATABASE TABLE */}
        <div className="sc-glass rounded-lg border border-slate-800 sc-hud-corner overflow-hidden">
          <div className="p-3 bg-slate-900/80 border-b border-slate-800 flex flex-wrap items-center justify-between gap-3">
            <div className="flex items-center gap-2">
              <Search className="w-4 h-4 text-cyan-400" />
              <h3 className="text-xs font-bold text-slate-200 font-mono uppercase tracking-wider">
                Vollständige RS-Signatur-Referenz (Star Citizen 3.24+)
              </h3>
            </div>

            {/* Filters */}
            <div className="flex items-center gap-2 flex-wrap">
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Suche nach Erz / Material..."
                className="bg-slate-950 border border-slate-800 rounded px-2.5 py-1 text-xs font-mono text-slate-200 placeholder-slate-600 focus:outline-none focus:border-cyan-500"
              />

              <select
                value={tierFilter}
                onChange={(e) => setTierFilter(e.target.value)}
                className="bg-slate-950 border border-slate-800 rounded px-2 py-1 text-xs font-mono text-slate-200 focus:outline-none focus:border-cyan-500"
              >
                <option value="all">Alle Tiers</option>
                <option value="S">Tier S (Quantainium)</option>
                <option value="A">Tier A (Bexalite, Gold, Taranite)</option>
                <option value="B">Tier B (Agricium, Laranite)</option>
                <option value="C">Tier C (Beryl, Titanium)</option>
              </select>

              <select
                value={methodFilter}
                onChange={(e) => setMethodFilter(e.target.value)}
                className="bg-slate-950 border border-slate-800 rounded px-2 py-1 text-xs font-mono text-slate-200 focus:outline-none focus:border-cyan-500"
              >
                <option value="all">Alle Methoden</option>
                <option value="Mining (Ship)">Schiffs-Mining</option>
                <option value="Mining (Vehicle)">ROC Mining</option>
                <option value="Mining (FPS)">Hand-Mining (FPS)</option>
                <option value="Salvage (Scraping)">Salvage (Hull Scraping)</option>
              </select>
            </div>
          </div>

          <div className="overflow-x-auto max-h-96">
            <table className="w-full text-left font-mono text-xs">
              <thead className="bg-slate-950/90 text-slate-400 sticky top-0 border-b border-slate-800 text-[11px] uppercase">
                <tr>
                  <th className="py-2.5 px-3 font-semibold">Material / Erz</th>
                  <th className="py-2.5 px-3 font-semibold">Tier</th>
                  <th className="py-2.5 px-3 font-semibold text-right">Basis RS</th>
                  <th className="py-2.5 px-3 font-semibold">Typ</th>
                  <th className="py-2.5 px-3 font-semibold text-right">Richtpreis / SCU</th>
                  <th className="py-2.5 px-3 font-semibold">Haupt-Fundorte</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/60 text-slate-300">
                {filteredResources.map((res) => (
                  <tr key={res.name} className="hover:bg-cyan-950/20 transition">
                    <td className="py-2 px-3 font-bold text-slate-100 flex items-center gap-2">
                      <span className="text-cyan-400 font-mono">⬡</span>
                      {res.name}
                    </td>
                    <td className="py-2 px-3">
                      <span
                        className={`px-1.5 py-0.5 text-[9px] font-bold rounded border uppercase ${getTierBadge(
                          res.tier
                        )}`}
                      >
                        {res.tier}
                      </span>
                    </td>
                    <td className="py-2 px-3 text-right font-bold text-cyan-300">
                      {res.baseRs.toLocaleString('de-DE')} RS
                    </td>
                    <td className="py-2 px-3 text-slate-400 text-[11px]">
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
    </div>
  );
};
