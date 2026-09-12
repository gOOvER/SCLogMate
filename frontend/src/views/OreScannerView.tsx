import React, { useEffect, useState, useMemo } from 'react';
import {
  bridge,
  RsResourceDto,
  RsMatchDto,
  MiningHaulDto,
} from '../services/photinoBridge';
import {
  Search,
  Radar,
  Crop,
  Zap,
  Clock,
  Plus,
  Trash2,
} from 'lucide-react';

export const OreScannerView: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'radar' | 'refinery'>('radar');

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

  // Refinery State
  const [hauls, setHauls] = useState<MiningHaulDto[]>([]);
  const [showAddModal, setShowAddModal] = useState<boolean>(false);
  const [newMaterial, setNewMaterial] = useState<string>('Quantainium');
  const [newScu, setNewScu] = useState<number>(32);
  const [newRefinery, setNewRefinery] = useState<string>('CRU-L1 Ambitious Dream');
  const [newMethod, setNewMethod] = useState<string>('Dinyx Dodecathetic');
  const [newDurationHours, setNewDurationHours] = useState<number>(2);

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

  const fetchHauls = async () => {
    try {
      const res = await bridge.sendRequest<MiningHaulDto[]>('get_mining_hauls');
      setHauls(res || []);
    } catch (err) {
      console.error('Failed to load mining hauls:', err);
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
    fetchHauls();
    decodeRs(inputRs);

    // Ticking timer for refinery jobs
    const interval = setInterval(() => {
      setHauls((prev) =>
        prev.map((h) => ({
          ...h,
          remainingSeconds: Math.max(0, h.remainingSeconds - 1),
          isTimerCompleted: h.remainingSeconds - 1 <= 0,
        }))
      );
    }, 1000);

    return () => clearInterval(interval);
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

  const handleCreateHaul = async (e: React.FormEvent) => {
    e.preventDefault();
    const yieldMap: Record<string, number> = {
      'Dinyx Dodecathetic': 93.0,
      'Cormack Method': 88.5,
      'Ferron Exchange': 85.0,
      'Pyroxeres': 82.0,
      'Gaskin-Kandah': 78.0,
    };
    const yieldPct = yieldMap[newMethod] || 88.0;
    const cost = Math.round(newScu * 85);

    try {
      await bridge.sendRequest('save_mining_haul', {
        materialName: newMaterial,
        scuQuantity: newScu,
        refineryLocation: newRefinery,
        method: newMethod,
        yieldPercent: yieldPct,
        costAuec: cost,
        durationSeconds: newDurationHours * 3600,
        status: 'Refining',
        soldAuec: 0,
      });
      setShowAddModal(false);
      fetchHauls();
      showToast(`✓ Raffinerie-Auftrag für ${newScu} SCU ${newMaterial} gestartet!`);
    } catch (err) {
      console.error('Failed to create mining haul:', err);
    }
  };

  const handleUpdateStatus = async (id: number, status: string, sold = 0) => {
    try {
      await bridge.sendRequest('update_mining_haul_status', { id, status, soldAuec: sold });
      fetchHauls();
    } catch (err) {
      console.error('Failed to update haul status:', err);
    }
  };

  const handleDeleteHaul = async (id: number) => {
    if (!window.confirm('Diesen Raffinerie-Auftrag löschen?')) return;
    try {
      await bridge.sendRequest('delete_mining_haul', { id });
      fetchHauls();
    } catch (err) {
      console.error('Failed to delete haul:', err);
    }
  };

  const formatSeconds = (sec: number) => {
    if (sec <= 0) return '00:00:00 (Fertig)';
    const h = Math.floor(sec / 3600);
    const m = Math.floor((sec % 3600) / 60);
    const s = sec % 60;
    return `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
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

      {/* Tabs */}
      <div className="flex items-center justify-between border-b border-slate-800 pb-2">
        <div className="flex items-center gap-2">
          <button
            onClick={() => setActiveTab('radar')}
            className={`flex items-center gap-2 px-4 py-2 text-xs font-mono font-semibold rounded-t cursor-pointer transition ${
              activeTab === 'radar'
                ? 'bg-slate-800 text-cyan-300 border-b-2 border-cyan-400 shadow-[0_2px_8px_rgba(0,240,255,0.15)]'
                : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            <Radar className="w-4 h-4 text-cyan-400" />
            RS-Radar & Signatur-Decoder
          </button>

          <button
            onClick={() => setActiveTab('refinery')}
            className={`flex items-center gap-2 px-4 py-2 text-xs font-mono font-semibold rounded-t cursor-pointer transition ${
              activeTab === 'refinery'
                ? 'bg-slate-800 text-amber-300 border-b-2 border-amber-400 shadow-[0_2px_8px_rgba(245,158,11,0.15)]'
                : 'text-slate-400 hover:text-slate-200'
            }`}
          >
            <Clock className="w-4 h-4 text-amber-400" />
            Raffinerie-Aufträge & Mining-Haul-Tracker ({hauls.length})
          </button>
        </div>

        {activeTab === 'refinery' && (
          <button
            onClick={() => setShowAddModal(true)}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded bg-amber-600 hover:bg-amber-500 text-white text-xs font-mono font-semibold transition cursor-pointer shadow-[0_0_12px_rgba(245,158,11,0.3)]"
          >
            <Plus className="w-4 h-4" />
            Neuer Raffinerie-Auftrag
          </button>
        )}
      </div>

      {/* TAB 1: RADAR DECODER */}
      {activeTab === 'radar' && (
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
                  className="flex items-center gap-1.5 px-3 py-1.5 rounded bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700 text-xs font-mono transition cursor-pointer"
                >
                  <Crop className="w-3.5 h-3.5 text-cyan-400" />
                  <span>Scanbereich wählen</span>
                </button>

                <button
                  onClick={handleTestScan}
                  disabled={isTestingScan}
                  className="flex items-center gap-1.5 px-3 py-1.5 rounded bg-cyan-600 hover:bg-cyan-500 text-white text-xs font-mono font-semibold transition cursor-pointer shadow-[0_0_10px_rgba(0,240,255,0.3)]"
                >
                  <Zap className="w-3.5 h-3.5" />
                  <span>{isTestingScan ? 'Scanne...' : 'Live-Scan testen'}</span>
                </button>
              </div>
            </div>

            {/* Input + Presets */}
            <div className="flex flex-wrap items-center gap-3 pt-1">
              <div className="flex items-center gap-2 bg-slate-900 border border-slate-800 rounded px-3 py-1.5 w-60">
                <span className="text-xs text-slate-500 font-mono">RS:</span>
                <input
                  type="text"
                  value={inputRs}
                  onChange={(e) => handleInputChange(e.target.value)}
                  placeholder="z.B. 2000"
                  className="bg-transparent border-none text-slate-100 text-sm font-bold font-mono focus:outline-none w-full"
                />
              </div>

              <div className="flex items-center gap-1.5 overflow-x-auto">
                {quickPresets.map((p) => (
                  <button
                    key={p.value}
                    onClick={() => handleInputChange(p.value)}
                    className="px-2.5 py-1 text-[11px] font-mono bg-slate-900/80 hover:bg-slate-800 border border-slate-800 rounded text-slate-300 hover:text-cyan-300 transition cursor-pointer shrink-0"
                  >
                    {p.label}
                  </button>
                ))}
              </div>
            </div>
          </div>

          {/* Results Grid */}
          {matches.length > 0 && (
            <div className="space-y-2">
              <h3 className="text-xs font-mono font-bold uppercase tracking-wider text-slate-300">
                Mögliche Ressourcen-Treffer ({matches.length})
              </h3>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
                {matches.map((m) => (
                  <div
                    key={m.resourceName + m.nodes}
                    className="sc-glass rounded-lg p-3 border border-slate-800 hover:border-cyan-500/40 transition flex flex-col justify-between sc-hud-corner"
                  >
                    <div>
                      <div className="flex items-start justify-between gap-2">
                        <div>
                          <h4 className="font-bold text-slate-100 text-sm">{m.resourceName}</h4>
                          <span className="text-[10px] text-slate-400 font-mono">
                            {m.nodes}x Vorkommen · {m.method}
                          </span>
                        </div>
                        <span className={`px-2 py-0.5 text-[10px] font-bold rounded border ${getTierBadge(m.tier)}`}>
                          Tier {m.tier}
                        </span>
                      </div>

                      <div className="mt-2 text-xs font-mono space-y-1">
                        <div className="flex justify-between text-[11px]">
                          <span className="text-slate-400">Basis-RS:</span>
                          <span className="text-slate-200">{m.baseRs.toLocaleString('de-DE')} RS</span>
                        </div>
                        <div className="flex justify-between text-[11px]">
                          <span className="text-slate-400">Genauigkeit:</span>
                          <span className={m.isExact ? 'text-emerald-400 font-bold' : 'text-amber-400'}>
                            {m.isExact ? 'Exakter Treffer' : `±${m.errorPct}% Abweichung`}
                          </span>
                        </div>
                      </div>
                    </div>

                    <div className="mt-3 pt-2 border-t border-slate-800/80 flex items-center justify-between text-xs font-mono">
                      <span className="text-slate-400 text-[10px]">Geschätzter Wert:</span>
                      <span className="text-emerald-400 font-bold">
                        ~ {m.estimatedClusterValue > 0 ? `${m.estimatedClusterValue.toLocaleString('de-DE')} aUEC` : '—'}
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Catalog Table */}
          <div className="flex-1 overflow-auto pr-1">
            <div className="flex flex-wrap items-center justify-between gap-3 mb-2">
              <span className="text-xs font-mono font-bold uppercase tracking-wider text-slate-300">
                Ressourcen-Katalog ({filteredResources.length})
              </span>
              <div className="flex items-center gap-2">
                <div className="flex items-center gap-1.5 bg-slate-900 border border-slate-800 rounded px-2.5 py-1 text-xs font-mono">
                  <Search className="w-3.5 h-3.5 text-slate-400" />
                  <input
                    type="text"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    placeholder="Material suchen..."
                    className="bg-transparent border-none text-slate-200 placeholder-slate-500 focus:outline-none w-32"
                  />
                </div>
                <select
                  value={tierFilter}
                  onChange={(e) => setTierFilter(e.target.value)}
                  className="bg-slate-900 border border-slate-800 rounded px-2 py-1 text-xs font-mono text-slate-300 focus:outline-none cursor-pointer"
                >
                  <option value="all">Alle Tiers</option>
                  <option value="S">Tier S</option>
                  <option value="A">Tier A</option>
                  <option value="B">Tier B</option>
                  <option value="C">Tier C</option>
                </select>
                <select
                  value={methodFilter}
                  onChange={(e) => setMethodFilter(e.target.value)}
                  className="bg-slate-900 border border-slate-800 rounded px-2 py-1 text-xs font-mono text-slate-300 focus:outline-none cursor-pointer"
                >
                  <option value="all">Alle Methoden</option>
                  <option value="mining">Mining</option>
                  <option value="salvage">Salvage</option>
                  <option value="fps">FPS Gems</option>
                </select>
              </div>
            </div>
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
      )}

      {/* TAB 2: REFINERY & MINING HAUL TRACKER */}
      {activeTab === 'refinery' && (
        <div className="flex-1 flex flex-col space-y-4 overflow-y-auto pr-1">
          {hauls.length === 0 ? (
            <div className="sc-glass rounded-lg p-12 border border-slate-800 text-center flex flex-col items-center justify-center space-y-3">
              <Clock className="w-12 h-12 text-amber-500/40" />
              <div className="text-slate-200 font-medium">Keine aktiven Raffinerie-Aufträge</div>
              <p className="text-xs text-slate-400 max-w-md">
                Erfasse deine Raffinerie-Jobs (z.B. Quantainium, Bexalite oder Gold), um Verarbeitungszeiten, Rest-Timer und Ausbeuten live im Blick zu behalten.
              </p>
              <button
                onClick={() => setShowAddModal(true)}
                className="px-4 py-2 rounded bg-amber-600 hover:bg-amber-500 text-white text-xs font-mono font-semibold transition cursor-pointer shadow-[0_0_12px_rgba(245,158,11,0.3)]"
              >
                ＋ Ersten Auftrag anlegen
              </button>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3">
              {hauls.map((h) => (
                <div
                  key={h.id}
                  className="sc-glass rounded-lg p-3.5 border border-slate-800 hover:border-amber-500/40 transition flex flex-col justify-between sc-hud-corner group"
                >
                  <div>
                    {/* Header */}
                    <div className="flex items-start justify-between gap-2">
                      <div>
                        <h3 className="font-bold text-slate-100 text-sm group-hover:text-amber-300 transition">
                          {h.scuQuantity} SCU {h.materialName}
                        </h3>
                        <div className="text-[10px] font-mono text-slate-400">
                          {h.refineryLocation} · {h.method}
                        </div>
                      </div>

                      <span
                        className={`px-2 py-0.5 text-[9px] font-mono font-bold rounded border uppercase ${
                          h.status === 'Sold'
                            ? 'bg-emerald-950/60 text-emerald-300 border-emerald-800'
                            : h.isTimerCompleted || h.status === 'Ready'
                            ? 'bg-amber-950/60 text-amber-300 border-amber-800'
                            : 'bg-cyan-950/60 text-cyan-300 border-cyan-800'
                        }`}
                      >
                        {h.status === 'Sold' ? 'Verkauft' : h.isTimerCompleted ? 'Bereit' : 'In Bearbeitung'}
                      </span>
                    </div>

                    {/* Timer / Progress Bar */}
                    <div className="mt-3 p-2 rounded bg-slate-900/80 border border-slate-800 space-y-1.5 font-mono text-xs">
                      <div className="flex justify-between text-[11px]">
                        <span className="text-slate-400 flex items-center gap-1">
                          <Clock className="w-3 h-3 text-cyan-400" />
                          Restzeit:
                        </span>
                        <span className={`font-bold ${h.isTimerCompleted ? 'text-emerald-400' : 'text-amber-400'}`}>
                          {formatSeconds(h.remainingSeconds)}
                        </span>
                      </div>

                      <div className="w-full bg-slate-950 h-2 rounded-full overflow-hidden">
                        <div
                          className={`h-full rounded-full transition-all duration-500 ${
                            h.isTimerCompleted ? 'bg-emerald-500 w-full' : 'bg-amber-500'
                          }`}
                          style={{
                            width: h.isTimerCompleted
                              ? '100%'
                              : `${Math.min(100, Math.max(5, ((h.durationSeconds - h.remainingSeconds) / h.durationSeconds) * 100))}%`,
                          }}
                        />
                      </div>

                      <div className="flex justify-between text-[10px] text-slate-500 pt-0.5">
                        <span>Start: {h.submittedAt}</span>
                        <span>Fertig: {h.readyAt}</span>
                      </div>
                    </div>

                    {/* Yield info */}
                    <div className="mt-2.5 grid grid-cols-2 gap-2 text-xs font-mono">
                      <div className="p-2 rounded bg-slate-900/40 border border-slate-800/60">
                        <div className="text-[10px] text-slate-500">ERTRAG ({h.yieldPercent}%):</div>
                        <div className="font-bold text-cyan-300 text-sm mt-0.5">{h.yieldScu} SCU</div>
                      </div>
                      <div className="p-2 rounded bg-slate-900/40 border border-slate-800/60">
                        <div className="text-[10px] text-slate-500">KOSTEN:</div>
                        <div className="font-bold text-slate-300 text-sm mt-0.5">{h.costAuec.toLocaleString('de-DE')} aUEC</div>
                      </div>
                    </div>
                  </div>

                  {/* Actions */}
                  <div className="mt-3 pt-2.5 border-t border-slate-800/80 flex items-center justify-between text-xs font-mono">
                    <div className="flex items-center gap-2">
                      {h.status !== 'Sold' && (
                        <button
                          onClick={() => handleUpdateStatus(h.id, 'Sold', Math.round(h.yieldScu * 88000))}
                          className="px-2.5 py-1 rounded bg-emerald-950/60 hover:bg-emerald-900/80 text-emerald-300 border border-emerald-800 text-[10px] font-bold cursor-pointer transition"
                        >
                          ✓ Als Verkauft markieren
                        </button>
                      )}
                    </div>

                    <button
                      onClick={() => handleDeleteHaul(h.id)}
                      className="p-1 rounded bg-slate-900 border border-slate-800 text-slate-500 hover:text-rose-400 transition cursor-pointer"
                      title="Auftrag löschen"
                    >
                      <Trash2 className="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* MODAL: NEW REFINERY HAUL */}
      {showAddModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4">
          <div className="sc-glass border border-amber-500/40 rounded-lg p-5 w-full max-w-md shadow-[0_0_30px_rgba(245,158,11,0.2)]">
            <h2 className="text-base font-bold text-slate-100 font-mono uppercase tracking-wider mb-4 flex items-center gap-2">
              <Clock className="w-5 h-5 text-amber-400" />
              Neuen Raffinerie-Auftrag starten
            </h2>

            <form onSubmit={handleCreateHaul} className="space-y-3 font-mono text-xs">
              <div>
                <label className="block text-slate-400 mb-1">Rohstoff / Erz</label>
                <select
                  value={newMaterial}
                  onChange={(e) => setNewMaterial(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-800 rounded p-2 text-slate-200 focus:border-amber-500 focus:outline-none"
                >
                  <option value="Quantainium">Quantainium (Hochexplosiv · Höchster Wert)</option>
                  <option value="Bexalite">Bexalite</option>
                  <option value="Gold">Gold</option>
                  <option value="Taranite">Taranite</option>
                  <option value="Laranite">Laranite</option>
                  <option value="Agricium">Agricium</option>
                  <option value="Hephaestanite">Hephaestanite</option>
                  <option value="Beryl">Beryl</option>
                </select>
              </div>

              <div className="grid grid-cols-2 gap-2">
                <div>
                  <label className="block text-slate-400 mb-1">Rohstoff SCU</label>
                  <input
                    type="number"
                    min={1}
                    value={newScu}
                    onChange={(e) => setNewScu(parseInt(e.target.value, 10) || 1)}
                    className="w-full bg-slate-900 border border-slate-800 rounded p-2 text-slate-200 focus:border-amber-500 focus:outline-none"
                  />
                </div>
                <div>
                  <label className="block text-slate-400 mb-1">Dauer (Stunden)</label>
                  <input
                    type="number"
                    min={1}
                    max={72}
                    value={newDurationHours}
                    onChange={(e) => setNewDurationHours(parseInt(e.target.value, 10) || 1)}
                    className="w-full bg-slate-900 border border-slate-800 rounded p-2 text-slate-200 focus:border-amber-500 focus:outline-none"
                  />
                </div>
              </div>

              <div>
                <label className="block text-slate-400 mb-1">Raffinerie-Station</label>
                <select
                  value={newRefinery}
                  onChange={(e) => setNewRefinery(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-800 rounded p-2 text-slate-200 focus:border-amber-500 focus:outline-none"
                >
                  <option value="CRU-L1 Ambitious Dream">CRU-L1 Ambitious Dream</option>
                  <option value="HUR-L1 Green Glade">HUR-L1 Green Glade</option>
                  <option value="HUR-L2 Faithful Dream">HUR-L2 Faithful Dream</option>
                  <option value="ARC-L1 Wide Forest">ARC-L1 Wide Forest</option>
                  <option value="MIC-L1 Shallow Frontier">MIC-L1 Shallow Frontier</option>
                  <option value="Checkmate Station (Pyro)">Checkmate Station (Pyro)</option>
                </select>
              </div>

              <div>
                <label className="block text-slate-400 mb-1">Verarbeitungsmethode</label>
                <select
                  value={newMethod}
                  onChange={(e) => setNewMethod(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-800 rounded p-2 text-slate-200 focus:border-amber-500 focus:outline-none"
                >
                  <option value="Dinyx Dodecathetic">Dinyx Dodecathetic (93% Ertrag · Langsam & Hochwertig)</option>
                  <option value="Cormack Method">Cormack Method (88.5% Ertrag · Ausgewogen)</option>
                  <option value="Ferron Exchange">Ferron Exchange (85% Ertrag · Schnell)</option>
                  <option value="Pyroxeres">Pyroxeres (82% Ertrag · Günstig)</option>
                  <option value="Gaskin-Kandah">Gaskin-Kandah (78% Ertrag · Extrem schnell)</option>
                </select>
              </div>

              <div className="flex items-center justify-end gap-2 pt-3 border-t border-slate-800">
                <button
                  type="button"
                  onClick={() => setShowAddModal(false)}
                  className="px-4 py-2 rounded bg-slate-800 hover:bg-slate-700 text-slate-300 transition cursor-pointer"
                >
                  Abbrechen
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 rounded bg-amber-600 hover:bg-amber-500 text-white font-semibold transition cursor-pointer shadow-[0_0_12px_rgba(245,158,11,0.3)]"
                >
                  Auftrag anlegen
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
