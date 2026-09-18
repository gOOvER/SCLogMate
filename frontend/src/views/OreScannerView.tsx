import React, { useEffect, useState, useMemo } from 'react';
import {
  bridge,
  RsResourceDto,
  RsMatchDto,
  MiningEquipmentResponseDto,
  CrackVerdictResultDto,
} from '../services/photinoBridge';
import {
  Search,
  Radar,
  Crop,
  Zap,
  Hammer,
  Sliders,
  Flame,
  CheckCircle,
  AlertTriangle,
  XCircle,
  Sparkles,
  ChevronRight,
  Info,
} from 'lucide-react';

export const OreScannerView: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'radar' | 'cracker'>('radar');

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

  // Rock-Cracking Calculator State
  const [miningEquip, setMiningEquip] = useState<MiningEquipmentResponseDto | null>(null);
  const [rockMass, setRockMass] = useState<number>(5200);
  const [rockRes, setRockRes] = useState<number>(72);
  const [rockInst, setRockInst] = useState<number>(78);
  const [mineralName, setMineralName] = useState<string>('Quantanium');
  const [shipType, setShipType] = useState<'prospector' | 'mole'>('prospector');

  // Head 1
  const [laser1, setLaser1] = useState<string>('helix_s1');
  const [mod1_1, setMod1_1] = useState<string>('mod_focus3');
  const [mod1_2, setMod1_2] = useState<string>('');

  // Head 2 & 3 (MOLE)
  const [laser2, setLaser2] = useState<string>('arbor_mh1');
  const [mod2_1, setMod2_1] = useState<string>('');
  const [laser3, setLaser3] = useState<string>('lancet_mh1');
  const [mod3_1, setMod3_1] = useState<string>('');

  // Gadget
  const [selectedGadget, setSelectedGadget] = useState<string>('');

  // Verdict Result
  const [verdict, setVerdict] = useState<CrackVerdictResultDto | null>(null);

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

  const fetchMiningEquipment = async () => {
    try {
      const equip = await bridge.getMiningEquipment();
      setMiningEquip(equip);
    } catch (err) {
      console.error('Failed to fetch mining equipment:', err);
    }
  };

  useEffect(() => {
    fetchSignatures();
    decodeRs(inputRs);
    fetchMiningEquipment();
  }, []);

  // Recalculate Crackability whenever rock or equipment changes
  useEffect(() => {
    const calculateCrack = async () => {
      const heads = [
        {
          laserId: laser1,
          moduleIds: [mod1_1, mod1_2].filter(Boolean),
        },
      ];
      if (shipType === 'mole') {
        heads.push({
          laserId: laser2,
          moduleIds: [mod2_1].filter(Boolean),
        });
        heads.push({
          laserId: laser3,
          moduleIds: [mod3_1].filter(Boolean),
        });
      }

      try {
        const res = await bridge.calculateRockCrack({
          massKg: rockMass,
          resistancePercent: rockRes,
          instabilityPercent: rockInst,
          mineralName: mineralName || undefined,
          heads,
          gadgetId: selectedGadget || undefined,
        });
        setVerdict(res);
      } catch (err) {
        console.error('Failed to calculate rock crack:', err);
      }
    };

    calculateCrack();
  }, [
    rockMass,
    rockRes,
    rockInst,
    mineralName,
    shipType,
    laser1,
    mod1_1,
    mod1_2,
    laser2,
    mod2_1,
    laser3,
    mod3_1,
    selectedGadget,
  ]);

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

  const transferToCracker = (resourceName: string) => {
    setMineralName(resourceName);
    if (resourceName.toLowerCase().includes('quant')) {
      setRockMass(5200);
      setRockRes(72);
      setRockInst(78);
    } else if (resourceName.toLowerCase().includes('beryl')) {
      setRockMass(8500);
      setRockRes(45);
      setRockInst(40);
    } else if (resourceName.toLowerCase().includes('gold')) {
      setRockMass(6000);
      setRockRes(55);
      setRockInst(50);
    } else {
      setRockMass(7000);
      setRockRes(50);
      setRockInst(45);
    }
    setActiveTab('cracker');
    showToast(`In Knack-Rechner übertragen: ${resourceName}`);
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

      {/* Main Tab Navigation */}
      <div className="flex items-center gap-2 border-b border-slate-800 pb-2">
        <button
          onClick={() => setActiveTab('radar')}
          className={`flex items-center gap-2 px-4 py-2 rounded-lg text-xs font-mono font-bold uppercase transition cursor-pointer border ${
            activeTab === 'radar'
              ? 'bg-cyan-500/20 text-cyan-300 border-cyan-500/60 shadow-[0_0_12px_rgba(0,240,255,0.2)]'
              : 'bg-slate-900/60 text-slate-400 border-slate-800 hover:text-slate-200 hover:border-slate-700'
          }`}
        >
          <Radar className="w-4 h-4 text-cyan-400" />
          <span>Radar-Signatur Decoder</span>
        </button>

        <button
          onClick={() => setActiveTab('cracker')}
          className={`flex items-center gap-2 px-4 py-2 rounded-lg text-xs font-mono font-bold uppercase transition cursor-pointer border ${
            activeTab === 'cracker'
              ? 'bg-amber-500/20 text-amber-300 border-amber-500/60 shadow-[0_0_12px_rgba(245,158,11,0.2)]'
              : 'bg-slate-900/60 text-slate-400 border-slate-800 hover:text-slate-200 hover:border-slate-700'
          }`}
        >
          <Hammer className="w-4 h-4 text-amber-400" />
          <span>Gesteins-Knack-Rechner (Can I Crack It?)</span>
        </button>
      </div>

      {/* ========================================================================= */}
      {/* TAB 1: RADAR DECODER                                                      */}
      {/* ========================================================================= */}
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

                    {/* Action button: Transfer to Cracker */}
                    <button
                      onClick={() => transferToCracker(m.resourceName)}
                      className="mt-3 w-full py-1.5 rounded bg-slate-800 hover:bg-slate-700 text-cyan-300 hover:text-cyan-100 text-xs font-mono transition flex items-center justify-center gap-1.5 border border-slate-700 cursor-pointer"
                    >
                      <Hammer className="w-3.5 h-3.5 text-amber-400" />
                      <span>Im Knack-Rechner prüfen</span>
                      <ChevronRight className="w-3 h-3 text-slate-500" />
                    </button>
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
      )}

      {/* ========================================================================= */}
      {/* TAB 2: GESTEINS-KNACK-RECHNER (ROCK CRACKER)                              */}
      {/* ========================================================================= */}
      {activeTab === 'cracker' && (
        <div className="flex-1 flex flex-col space-y-4 overflow-y-auto pr-1">
          {/* Top Quick Presets */}
          <div className="sc-glass p-3.5 rounded-lg border border-slate-800 sc-hud-corner space-y-2">
            <div className="flex items-center justify-between">
              <span className="text-xs font-mono font-bold text-slate-300 uppercase flex items-center gap-1.5">
                <Sparkles className="w-3.5 h-3.5 text-amber-400" />
                Schnell-Presets für typische Brocken:
              </span>
              <span className="text-[11px] font-mono text-slate-500">Star Citizen 4.x Formel: 0,36 W/kg</span>
            </div>

            <div className="flex items-center gap-2 flex-wrap">
              {miningEquip?.presets.map((p, idx) => (
                <button
                  key={idx}
                  onClick={() => {
                    setMineralName(p.mineral);
                    setRockMass(p.massKg);
                    setRockRes(p.resistance);
                    setRockInst(p.instability);
                    showToast(`Preset geladen: ${p.name}`);
                  }}
                  className="px-3 py-1.5 rounded bg-slate-900/80 hover:bg-slate-800 text-xs font-mono text-slate-300 border border-slate-700/80 hover:border-cyan-500/50 transition cursor-pointer flex items-center gap-1.5"
                >
                  <span className="text-cyan-400 font-bold">⬡</span>
                  <span>{p.name}</span>
                  <span className="text-[10px] text-slate-500">({p.massKg.toLocaleString('de-DE')} kg / {p.resistance}%)</span>
                </button>
              ))}
            </div>
          </div>

          {/* Configuration Grid: Rock Scan vs Equipment Setup */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
            {/* Rock Parameters Card */}
            <div className="sc-glass p-4 rounded-lg border border-slate-800 space-y-3 sc-hud-corner">
              <h3 className="text-xs font-mono font-bold text-slate-200 uppercase tracking-wider flex items-center gap-2">
                <Radar className="w-4 h-4 text-cyan-400" />
                Gesteins-Scan Parameter
              </h3>

              <div className="space-y-3">
                {/* Mineral Name */}
                <div>
                  <label className="text-xs font-mono text-slate-400 block mb-1">Mineral / Vorkommen:</label>
                  <input
                    type="text"
                    value={mineralName}
                    onChange={(e) => setMineralName(e.target.value)}
                    placeholder="z.B. Quantanium, Beryl, Gold..."
                    className="w-full bg-slate-900 border border-slate-700 rounded px-3 py-1.5 text-xs font-mono text-slate-200 focus:outline-none focus:border-cyan-400"
                  />
                </div>

                {/* Mass (kg) */}
                <div>
                  <div className="flex justify-between text-xs font-mono mb-1">
                    <span className="text-slate-400">Gesteins-Masse:</span>
                    <span className="text-cyan-300 font-bold">{rockMass.toLocaleString('de-DE')} kg</span>
                  </div>
                  <input
                    type="range"
                    min="500"
                    max="25000"
                    step="100"
                    value={rockMass}
                    onChange={(e) => setRockMass(Number(e.target.value))}
                    className="w-full accent-cyan-400 cursor-pointer"
                  />
                  <div className="flex justify-between text-[10px] font-mono text-slate-500">
                    <span>500 kg (FPS/ROC)</span>
                    <span>10.000 kg (Prospector Solo)</span>
                    <span>25.000 kg (MOLE)</span>
                  </div>
                </div>

                {/* Resistance (%) */}
                <div>
                  <div className="flex justify-between text-xs font-mono mb-1">
                    <span className="text-slate-400">Resistenz:</span>
                    <span className="text-amber-300 font-bold">{rockRes}%</span>
                  </div>
                  <input
                    type="range"
                    min="0"
                    max="95"
                    step="1"
                    value={rockRes}
                    onChange={(e) => setRockRes(Number(e.target.value))}
                    className="w-full accent-amber-400 cursor-pointer"
                  />
                  <div className="flex justify-between text-[10px] font-mono text-slate-500">
                    <span>0% (Weich)</span>
                    <span>50% (Standard)</span>
                    <span>95% (Extreme Härte)</span>
                  </div>
                </div>

                {/* Instability (%) */}
                <div>
                  <div className="flex justify-between text-xs font-mono mb-1">
                    <span className="text-slate-400">Instabilität:</span>
                    <span className="text-rose-300 font-bold">{rockInst}%</span>
                  </div>
                  <input
                    type="range"
                    min="0"
                    max="100"
                    step="1"
                    value={rockInst}
                    onChange={(e) => setRockInst(Number(e.target.value))}
                    className="w-full accent-rose-400 cursor-pointer"
                  />
                  <div className="flex justify-between text-[10px] font-mono text-slate-500">
                    <span>0% (Ruhig)</span>
                    <span>50% (Schwankend)</span>
                    <span>100% (Explosiv)</span>
                  </div>
                </div>
              </div>
            </div>

            {/* Equipment Setup Card */}
            <div className="sc-glass p-4 rounded-lg border border-slate-800 space-y-3 sc-hud-corner">
              <div className="flex items-center justify-between">
                <h3 className="text-xs font-mono font-bold text-slate-200 uppercase tracking-wider flex items-center gap-2">
                  <Sliders className="w-4 h-4 text-cyan-400" />
                  Mining-Laser & Modul-Setup
                </h3>

                {/* Ship Type Toggle */}
                <div className="flex items-center rounded bg-slate-900 p-0.5 border border-slate-800">
                  <button
                    onClick={() => setShipType('prospector')}
                    className={`px-2 py-1 rounded text-[11px] font-mono transition ${
                      shipType === 'prospector'
                        ? 'bg-cyan-500/20 text-cyan-300 font-bold'
                        : 'text-slate-400 hover:text-slate-200'
                    }`}
                  >
                    Prospector (1 Head)
                  </button>
                  <button
                    onClick={() => setShipType('mole')}
                    className={`px-2 py-1 rounded text-[11px] font-mono transition ${
                      shipType === 'mole'
                        ? 'bg-cyan-500/20 text-cyan-300 font-bold'
                        : 'text-slate-400 hover:text-slate-200'
                    }`}
                  >
                    MOLE (3 Heads)
                  </button>
                </div>
              </div>

              {/* Head 1 Config */}
              <div className="p-2.5 rounded bg-slate-900/80 border border-slate-800 space-y-2">
                <div className="flex items-center justify-between text-xs font-mono">
                  <span className="font-bold text-slate-200">
                    {shipType === 'prospector' ? 'Haupt-Laser (S1)' : 'Laser Turm 1 (Front/Zentral)'}:
                  </span>
                  <span className="text-[10px] text-cyan-400">
                    {miningEquip?.lasers.find((l) => l.id === laser1)?.power.toLocaleString('de-DE')} W
                  </span>
                </div>

                <select
                  value={laser1}
                  onChange={(e) => setLaser1(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-700 rounded px-2.5 py-1.5 text-xs font-mono text-cyan-300 focus:outline-none focus:border-cyan-400"
                >
                  {miningEquip?.lasers.map((l) => (
                    <option key={l.id} value={l.id}>
                      {l.name} ({l.power.toLocaleString('de-DE')} W · {l.slots} Slots · {l.manufacturer})
                    </option>
                  ))}
                </select>

                {/* Head 1 Module Slots */}
                <div className="grid grid-cols-2 gap-2 pt-1">
                  <div>
                    <label className="text-[10px] font-mono text-slate-500 block mb-0.5">Modul-Slot 1:</label>
                    <select
                      value={mod1_1}
                      onChange={(e) => setMod1_1(e.target.value)}
                      className="w-full bg-slate-950 border border-slate-800 rounded px-2 py-1 text-[11px] font-mono text-slate-200 focus:outline-none focus:border-cyan-500"
                    >
                      <option value="">(Kein Modul)</option>
                      {miningEquip?.modules.map((m) => (
                        <option key={m.id} value={m.id}>
                          {m.name} ({m.type})
                        </option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="text-[10px] font-mono text-slate-500 block mb-0.5">Modul-Slot 2:</label>
                    <select
                      value={mod1_2}
                      onChange={(e) => setMod1_2(e.target.value)}
                      className="w-full bg-slate-950 border border-slate-800 rounded px-2 py-1 text-[11px] font-mono text-slate-200 focus:outline-none focus:border-cyan-500"
                    >
                      <option value="">(Kein Modul)</option>
                      {miningEquip?.modules.map((m) => (
                        <option key={m.id} value={m.id}>
                          {m.name} ({m.type})
                        </option>
                      ))}
                    </select>
                  </div>
                </div>
              </div>

              {/* MOLE Heads 2 & 3 */}
              {shipType === 'mole' && (
                <div className="grid grid-cols-2 gap-2">
                  <div className="p-2 rounded bg-slate-900/60 border border-slate-800 space-y-1">
                    <span className="text-[11px] font-bold font-mono text-slate-300 block">Turm 2 (Links):</span>
                    <select
                      value={laser2}
                      onChange={(e) => setLaser2(e.target.value)}
                      className="w-full bg-slate-950 border border-slate-700 rounded px-2 py-1 text-xs font-mono text-cyan-300"
                    >
                      {miningEquip?.lasers.map((l) => (
                        <option key={l.id} value={l.id}>
                          {l.name} ({l.power} W)
                        </option>
                      ))}
                    </select>
                    <select
                      value={mod2_1}
                      onChange={(e) => setMod2_1(e.target.value)}
                      className="w-full bg-slate-950 border border-slate-800 rounded px-2 py-0.5 text-[10px] font-mono text-slate-300 mt-1"
                    >
                      <option value="">(Kein Modul)</option>
                      {miningEquip?.modules.map((m) => (
                        <option key={m.id} value={m.id}>
                          {m.name}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="p-2 rounded bg-slate-900/60 border border-slate-800 space-y-1">
                    <span className="text-[11px] font-bold font-mono text-slate-300 block">Turm 3 (Rechts):</span>
                    <select
                      value={laser3}
                      onChange={(e) => setLaser3(e.target.value)}
                      className="w-full bg-slate-950 border border-slate-700 rounded px-2 py-1 text-xs font-mono text-cyan-300"
                    >
                      {miningEquip?.lasers.map((l) => (
                        <option key={l.id} value={l.id}>
                          {l.name} ({l.power} W)
                        </option>
                      ))}
                    </select>
                    <select
                      value={mod3_1}
                      onChange={(e) => setMod3_1(e.target.value)}
                      className="w-full bg-slate-950 border border-slate-800 rounded px-2 py-0.5 text-[10px] font-mono text-slate-300 mt-1"
                    >
                      <option value="">(Kein Modul)</option>
                      {miningEquip?.modules.map((m) => (
                        <option key={m.id} value={m.id}>
                          {m.name}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>
              )}

              {/* Gadget Selector */}
              <div className="pt-1">
                <label className="text-xs font-mono text-slate-400 block mb-1 flex items-center justify-between">
                  <span>Aufgesetztes Mining-Gadget (Boden/Außen):</span>
                  <span className="text-[10px] text-amber-400 font-bold">Direkt am Brocken</span>
                </label>
                <select
                  value={selectedGadget}
                  onChange={(e) => setSelectedGadget(e.target.value)}
                  className="w-full bg-slate-900 border border-slate-700 rounded px-3 py-1.5 text-xs font-mono text-amber-300 focus:outline-none focus:border-amber-400"
                >
                  <option value="">(Kein Gadget angebracht)</option>
                  {miningEquip?.gadgets.map((g) => (
                    <option key={g.id} value={g.id}>
                      {g.name} — {g.description}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          </div>

          {/* VERDICT CARD */}
          {verdict && (
            <div
              className={`p-4 rounded-xl border flex flex-col md:flex-row items-start md:items-center justify-between gap-4 sc-hud-corner ${
                verdict.verdict === 'solo'
                  ? 'bg-emerald-950/40 border-emerald-500/60 shadow-[0_0_24px_rgba(16,185,129,0.2)]'
                  : verdict.verdict === 'gadget'
                  ? 'bg-amber-950/40 border-amber-500/60 shadow-[0_0_24px_rgba(245,158,11,0.2)]'
                  : 'bg-rose-950/40 border-rose-500/60 shadow-[0_0_24px_rgba(239,68,68,0.2)]'
              }`}
            >
              <div className="flex items-start gap-3.5">
                {verdict.verdict === 'solo' ? (
                  <CheckCircle className="w-8 h-8 text-emerald-400 shrink-0 mt-1" />
                ) : verdict.verdict === 'gadget' ? (
                  <AlertTriangle className="w-8 h-8 text-amber-400 shrink-0 mt-1" />
                ) : (
                  <XCircle className="w-8 h-8 text-rose-400 shrink-0 mt-1" />
                )}
                <div>
                  <div className="flex items-center gap-2 flex-wrap">
                    <span
                      className="px-2.5 py-0.5 text-xs font-mono font-bold rounded uppercase tracking-wider"
                      style={{ backgroundColor: `${verdict.verdictColor}20`, color: verdict.verdictColor }}
                    >
                      {verdict.verdictBadge}
                    </span>
                    <span className="text-xs font-mono text-slate-300">
                      {verdict.verdictTitle}
                    </span>
                  </div>

                  <p className="text-xs font-mono text-slate-300 mt-1.5">
                    Geliefert: <span className="text-cyan-300 font-bold">{verdict.powerDelivered.toLocaleString('de-DE')} W</span> · Benötigt: <span className="text-slate-400 font-bold">{verdict.powerRequired.toLocaleString('de-DE')} W</span> (Leistungsverhältnis: <span className="text-slate-100 font-bold">{Math.round(verdict.ratio * 100)}%</span>)
                  </p>

                  {/* Notes */}
                  {verdict.notes.length > 0 && (
                    <div className="mt-2 text-xs font-mono text-slate-300 space-y-0.5">
                      {verdict.notes.map((note, nIdx) => (
                        <div key={nIdx} className="flex items-center gap-1.5">
                          <Info className="w-3.5 h-3.5 text-cyan-400 shrink-0" />
                          <span>{note}</span>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>

              {/* Quick Metrics */}
              <div className="flex items-center gap-4 shrink-0 font-mono text-right border-t md:border-t-0 md:border-l border-slate-800/80 pt-2 md:pt-0 md:pl-4">
                <div>
                  <div className="text-[10px] text-slate-400 uppercase">Eff. Resistenz</div>
                  <div className="text-base font-bold text-amber-300">{verdict.effectiveResistancePercent}%</div>
                </div>
                <div>
                  <div className="text-[10px] text-slate-400 uppercase">Optimal-Fenster</div>
                  <div className="text-base font-bold text-cyan-300">{verdict.windowPercent}%</div>
                </div>
                <div>
                  <div className="text-[10px] text-slate-400 uppercase">Max. Solo-Masse</div>
                  <div className="text-base font-bold text-emerald-300">{verdict.maxCrackableMassKg.toLocaleString('de-DE')} kg</div>
                </div>
              </div>
            </div>
          )}

          {/* HEAD-TO-HEAD LASER COMPARISON TABLE */}
          {verdict && verdict.alternatives && verdict.alternatives.length > 0 && (
            <div className="sc-glass rounded-lg border border-slate-800 sc-hud-corner overflow-hidden">
              <div className="p-3 bg-slate-900/80 border-b border-slate-800 flex items-center justify-between">
                <h3 className="text-xs font-mono font-bold text-slate-200 uppercase tracking-wider flex items-center gap-2">
                  <Flame className="w-4 h-4 text-cyan-400" />
                  Head-to-Head Laser-Vergleich (Welcher Kopf knackt diesen Stein?)
                </h3>
                <span className="text-[10px] font-mono text-slate-500">Alle Size 1 Mining-Köpfe</span>
              </div>

              <div className="overflow-x-auto">
                <table className="w-full text-left font-mono text-xs">
                  <thead className="bg-slate-950/90 text-slate-400 border-b border-slate-800 text-[11px] uppercase">
                    <tr>
                      <th className="py-2 px-3">Laser-Kopf</th>
                      <th className="py-2 px-3 text-right">Basis-Power</th>
                      <th className="py-2 px-3 text-right">Gelieferte Watt</th>
                      <th className="py-2 px-3 text-right">Leistungsquote</th>
                      <th className="py-2 px-3">Urteil</th>
                      <th className="py-2 px-3 text-right">Max. knackbar</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-800/60 text-slate-300">
                    {verdict.alternatives.map((alt) => (
                      <tr
                        key={alt.laserId}
                        className={`transition ${alt.laserId === laser1 ? 'bg-cyan-950/30' : 'hover:bg-slate-900/40'}`}
                      >
                        <td className="py-2 px-3 font-bold text-slate-100 flex items-center gap-1.5">
                          {alt.laserId === laser1 && <span className="text-cyan-400 text-xs">►</span>}
                          <span>{alt.name}</span>
                        </td>
                        <td className="py-2 px-3 text-right text-slate-400">
                          {alt.power.toLocaleString('de-DE')} W
                        </td>
                        <td className="py-2 px-3 text-right font-bold text-cyan-300">
                          {alt.powerDelivered.toLocaleString('de-DE')} W
                        </td>
                        <td className="py-2 px-3 text-right font-bold">
                          <span
                            className={
                              alt.ratio >= 1.15
                                ? 'text-emerald-400'
                                : alt.ratio >= 0.7
                                ? 'text-amber-400'
                                : 'text-rose-400'
                            }
                          >
                            {Math.round(alt.ratio * 100)}%
                          </span>
                        </td>
                        <td className="py-2 px-3">
                          <span
                            className={`px-1.5 py-0.5 rounded text-[10px] font-bold border uppercase ${
                              alt.verdict === 'solo'
                                ? 'bg-emerald-500/20 text-emerald-300 border-emerald-500/40'
                                : alt.verdict === 'gadget'
                                ? 'bg-amber-500/20 text-amber-300 border-amber-500/40'
                                : 'bg-rose-500/20 text-rose-300 border-rose-500/40'
                            }`}
                          >
                            {alt.verdict === 'solo'
                              ? '✓ Solo'
                              : alt.verdict === 'gadget'
                              ? '⚡ Gadget'
                              : '✖ Zu schwer'}
                          </span>
                        </td>
                        <td className="py-2 px-3 text-right text-emerald-300 font-bold">
                          {alt.maxCrackableMassKg.toLocaleString('de-DE')} kg
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
