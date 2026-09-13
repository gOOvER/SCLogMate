import React, { useEffect, useState, useMemo } from 'react';
import {
  bridge,
  MiningHaulDto,
  RefineryCatalogDto,
  WikiInfo,
} from '../services/photinoBridge';
import {
  Flame,
  Clock,
  CheckCircle2,
  Camera,
  Crop,
  Plus,
  Trash2,
  RefreshCw,
  Search,
  Calculator,
  Compass,
  BookOpen,
  TrendingUp,
  Coins,
  Box,
  Sparkles,
  Zap,
  HelpCircle,
  Timer,
  Copy,
} from 'lucide-react';

interface RefineryViewProps {
  onOpenWiki?: (target: string | WikiInfo) => void;
}

// Fallback Richtpreise für Erze (aUEC / SCU)
const ORE_BASE_PRICES: Record<string, number> = {
  Quantanium: 88000,
  Quantainium: 88000,
  Bexalite: 44000,
  Taranite: 32000,
  Larinite: 31000,
  Laranite: 31000,
  Lindinium: 22000,
  Agricium: 27500,
  Hephaestanite: 15000,
  Gold: 9200,
  Diamond: 6800,
  Diamant: 6800,
  Beryl: 4200,
  Beryll: 4200,
  Tungsten: 1400,
  Wolfram: 1400,
  Titanium: 1200,
  Titan: 1200,
  Copper: 800,
  Kupfer: 800,
  Corundum: 700,
  Korund: 700,
  Quartz: 500,
  Quarz: 500,
  Iron: 400,
  Eisen: 400,
  RMC: 14500,
};

export const RefineryView: React.FC<RefineryViewProps> = ({ onOpenWiki }) => {
  const [activeTab, setActiveTab] = useState<'orders' | 'ocr' | 'calculator' | 'stations'>('orders');
  const [hauls, setHauls] = useState<MiningHaulDto[]>([]);
  const [catalog, setCatalog] = useState<RefineryCatalogDto | null>(null);
  const [toastMsg, setToastMsg] = useState<string | null>(null);

  // Filter & Suche für Aufträge
  const [search, setSearch] = useState('');
  const [systemFilter, setSystemFilter] = useState<'all' | 'Stanton' | 'Pyro' | 'Nyx'>('all');
  const [statusFilter, setStatusFilter] = useState<'all' | 'Refining' | 'Ready' | 'Collected'>('all');

  // OCR Kiosk Status
  const [isScanning, setIsScanning] = useState(false);
  const [countdownSec, setCountdownSec] = useState<number | null>(null);
  const [lastOcrText, setLastOcrText] = useState<string | null>(null);
  const [lastScanResult, setLastScanResult] = useState<any>(null);

  // Manueller Auftrag Modal / State
  const [showAddModal, setShowAddModal] = useState(false);
  const [formSystem, setFormSystem] = useState<'Stanton' | 'Pyro' | 'Nyx'>('Stanton');
  const [formStation, setFormStation] = useState('ARC-L1 Wide Forest Station');
  const [formMaterial, setFormMaterial] = useState('Quantanium');
  const [formScu, setFormScu] = useState(32);
  const [formMethod, setFormMethod] = useState('Dinyx Dodecathetic');
  const [formDurationHours, setFormDurationHours] = useState(4.0);

  // Ertrags-Rechner State
  const [calcMaterial, setCalcMaterial] = useState('Quantanium');
  const [calcScu, setCalcScu] = useState(32);
  const [calcStation, setCalcStation] = useState('ARC-L1 Wide Forest Station');

  const showToast = (msg: string) => {
    setToastMsg(msg);
    setTimeout(() => setToastMsg(null), 4000);
  };

  const fetchHauls = async () => {
    try {
      const res = await bridge.sendRequest<MiningHaulDto[]>('get_mining_hauls');
      setHauls(res || []);
    } catch (err) {
      console.error('Failed to load mining hauls:', err);
    }
  };

  const fetchCatalog = async () => {
    try {
      const res = await bridge.sendRequest<RefineryCatalogDto>('get_refinery_catalog');
      if (res) {
        setCatalog(res);
        if (res.stations && res.stations.length > 0) {
          setFormStation(res.stations[0].name);
          setCalcStation(res.stations[0].name);
        }
      }
    } catch (err) {
      console.error('Failed to load refinery catalog:', err);
    }
  };

  useEffect(() => {
    fetchHauls();
    fetchCatalog();

    // Live Ticker für Restzeiten (sekündliche Aktualisierung)
    const interval = setInterval(() => {
      setHauls((prev) =>
        prev.map((h) => {
          if (h.status !== 'Refining') return h;
          const nextSec = Math.max(0, h.remainingSeconds - 1);
          return {
            ...h,
            remainingSeconds: nextSec,
            isTimerCompleted: nextSec <= 0,
            status: nextSec <= 0 ? 'Ready' : h.status,
          };
        })
      );
    }, 1000);

    return () => clearInterval(interval);
  }, []);

  // Stationen gefiltert nach System für Formular
  const formStationsForSystem = useMemo(() => {
    if (!catalog) return [];
    return catalog.stations.filter((s) => s.system === formSystem);
  }, [catalog, formSystem]);

  // System automatisch bei Station anpassen
  const detectSystemFromStation = (stationName: string): 'Stanton' | 'Pyro' | 'Nyx' => {
    if (
      stationName.includes('Delamar') ||
      stationName.includes('Levski') ||
      stationName.includes('Glaciem') ||
      stationName.includes('Nyx')
    ) {
      return 'Nyx';
    }
    if (
      stationName.includes('Pyro') ||
      stationName.includes('Ruin') ||
      stationName.includes('Checkpoint')
    ) {
      return 'Pyro';
    }
    return 'Stanton';
  };

  // 1. OCR Kiosk Sofort-Scan
  const handleScanInstant = async () => {
    try {
      setIsScanning(true);
      showToast('Kiosk-Scan läuft...');
      const res = await bridge.sendRequest<any>('scan_refinery_kiosk');
      if (res?.success) {
        setLastScanResult(res);
        setLastOcrText(res.recognizedText || '');
        fetchHauls();
        showToast(`✓ Scan erfolgreich: ${res.ordersFound} Auftrag/Aufträge erkannt (${res.ordersSaved} neu gespeichert)!`);
      } else {
        showToast(res?.error ? `Fehler: ${res.error}` : 'Keine Aufträge auf dem Bildschirm erkannt');
      }
    } catch (err) {
      console.error('OCR scan failed:', err);
      showToast('Fehler bei der OCR-Kiosk-Erkennung');
    } finally {
      setIsScanning(false);
    }
  };

  // 2. OCR Kiosk Vorlauf-Scan mit Countdown (3s zum Reintappen ins Spiel)
  const handleScanWithCountdown = () => {
    if (isScanning || countdownSec !== null) return;
    setCountdownSec(3);
    const cdInterval = setInterval(() => {
      setCountdownSec((prev) => {
        if (prev === null || prev <= 1) {
          clearInterval(cdInterval);
          setTimeout(() => {
            setCountdownSec(null);
            handleScanInstant();
          }, 300);
          return null;
        }
        return prev - 1;
      });
    }, 1000);
  };

  // 3. Scan-Bereich anpassen (Snipping)
  const handleSelectRegion = async () => {
    try {
      showToast('Bildschirm-Auswahl: Ziehe mit der Maus einen Rahmen über das Raffinerie-Terminal...');
      const res = await bridge.sendRequest<any>('select_ocr_region', { target: 'refinery' });
      if (res?.success && res.region) {
        showToast(`✓ Raffinerie-Scanbereich gespeichert: ${res.region.width}×${res.region.height}`);
      } else if (res?.cancelled) {
        showToast('Auswahl abgebrochen');
      }
    } catch (err) {
      console.error('Failed to select refinery region:', err);
      showToast('Fehler bei der Bereichsauswahl');
    }
  };

  // 4. Test-Scan
  const handleTestScan = async () => {
    try {
      setIsScanning(true);
      const res = await bridge.sendRequest<any>('test_ocr_scan', { target: 'refinery' });
      if (res?.success) {
        setLastOcrText(res.recognizedText);
        setLastScanResult(res);
        showToast(`✓ Test-Scan abgeschlossen in ${res.durationMs}ms`);
      } else {
        showToast('Kein verwertbarer Kiosk-Text erfasst');
      }
    } catch (err) {
      console.error('Test scan error:', err);
      showToast('Fehler beim OCR Test-Scan');
    } finally {
      setIsScanning(false);
    }
  };

  // Auftrag manuell speichern
  const handleSaveManualHaul = async (e: React.FormEvent) => {
    e.preventDefault();
    const yieldPct = catalog
      ? catalog.methods.find((m) => m.name === formMethod)?.baseYield || 90.0
      : 90.0;
    const cost = Math.round(formScu * 95);

    try {
      await bridge.sendRequest('save_mining_haul', {
        materialName: formMaterial,
        scuQuantity: formScu,
        refineryLocation: formStation,
        method: formMethod,
        yieldPercent: yieldPct,
        costAuec: cost,
        durationSeconds: Math.round(formDurationHours * 3600),
        status: 'Refining',
        soldAuec: 0,
      });
      setShowAddModal(false);
      fetchHauls();
      showToast(`✓ Auftrag gespeichert: ${formScu} SCU ${formMaterial} @ ${formStation}`);
    } catch (err) {
      console.error('Failed to save manual haul:', err);
      showToast('Fehler beim Speichern des Auftrags');
    }
  };

  // Status-Update (z. B. Abgeholt)
  const handleUpdateStatus = async (id: number, newStatus: string) => {
    try {
      await bridge.sendRequest('update_mining_haul_status', { id, status: newStatus });
      fetchHauls();
      showToast(`✓ Status aktualisiert auf: ${newStatus}`);
    } catch (err) {
      console.error('Failed to update haul status:', err);
    }
  };

  // Löschen
  const handleDeleteHaul = async (id: number) => {
    if (!window.confirm('Möchtest du diesen Veredelungsauftrag wirklich löschen?')) return;
    try {
      await bridge.sendRequest('delete_mining_haul', { id });
      fetchHauls();
      showToast('Auftrag gelöscht');
    } catch (err) {
      console.error('Failed to delete haul:', err);
    }
  };

  // Ins Warenlager übertragen
  const handleTransferToWarehouse = async (haul: MiningHaulDto) => {
    try {
      await bridge.sendRequest('save_warehouse_item', {
        name: haul.materialName,
        category: 'Erze & Veredelte Metalle',
        location: haul.refineryLocation,
        quantity: Math.round(haul.yieldScu),
        unit: 'SCU',
      });
      await handleUpdateStatus(haul.id, 'Collected');
      showToast(`✓ ${haul.yieldScu} SCU ${haul.materialName} ins Warenlager (${haul.refineryLocation}) gebucht!`);
    } catch (err) {
      console.error('Failed to transfer to warehouse:', err);
      showToast('Fehler bei der Übertragung ins Warenlager');
    }
  };

  // Formatierung von Sekunden zu HH:MM:SS
  const formatSeconds = (sec: number) => {
    if (sec <= 0) return 'Fertig!';
    const h = Math.floor(sec / 3600);
    const m = Math.floor((sec % 3600) / 60);
    const s = sec % 60;
    return `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  };

  // Statistische Zusammenfassung
  const stats = useMemo(() => {
    const active = hauls.filter((h) => h.status === 'Refining');
    const ready = hauls.filter((h) => h.status === 'Ready');
    const totalYieldScu = hauls.reduce((acc, h) => acc + (h.yieldScu || 0), 0);

    let totalEstValue = 0;
    hauls.forEach((h) => {
      const price = ORE_BASE_PRICES[h.materialName] || 15000;
      totalEstValue += (h.yieldScu || 0) * price;
    });

    let nextReadySec: number | null = null;
    active.forEach((h) => {
      if (nextReadySec === null || h.remainingSeconds < nextReadySec) {
        nextReadySec = h.remainingSeconds;
      }
    });

    return {
      activeCount: active.length,
      readyCount: ready.length,
      totalYieldScu: Math.round(totalYieldScu * 10) / 10,
      totalEstValue: Math.round(totalEstValue),
      nextReadySec,
    };
  }, [hauls]);

  // Gefilterte Aufträge
  const filteredHauls = useMemo(() => {
    return hauls.filter((h) => {
      // System Filter
      if (systemFilter !== 'all') {
        const sys = detectSystemFromStation(h.refineryLocation);
        if (sys !== systemFilter) return false;
      }
      // Status Filter
      if (statusFilter !== 'all' && h.status !== statusFilter) return false;
      // Textsuche
      if (search.trim()) {
        const q = search.toLowerCase();
        const m = h.materialName.toLowerCase().includes(q);
        const l = h.refineryLocation.toLowerCase().includes(q);
        const met = h.method.toLowerCase().includes(q);
        return m || l || met;
      }
      return true;
    });
  }, [hauls, systemFilter, statusFilter, search]);

  // Rechner-Vergleich aller Methoden
  const calculatorRows = useMemo(() => {
    if (!catalog) return [];
    const basePrice = ORE_BASE_PRICES[calcMaterial] || 15000;
    const baseInputScu = calcScu;

    // Finde Station Boni
    const stObj = catalog.stations.find((s) => s.name === calcStation);
    const stationBonus = stObj?.materialYieldBonuses[calcMaterial] || 0;

    return catalog.methods.map((method) => {
      const effYieldPct = Math.min(98.0, method.baseYield + stationBonus * 100);
      const yieldScu = Math.round(baseInputScu * (effYieldPct / 100.0) * 10) / 10;
      const hours = Math.round(baseInputScu * 0.08 * method.timeMultiplier * 10) / 10;
      const costAuec = Math.round(baseInputScu * 85 * method.costMultiplier);
      const revenueAuec = Math.round(yieldScu * basePrice);
      const profitAuec = revenueAuec - costAuec;

      return {
        method,
        effYieldPct,
        yieldScu,
        hours,
        costAuec,
        revenueAuec,
        profitAuec,
      };
    });
  }, [catalog, calcMaterial, calcScu, calcStation]);

  const bestProfitMethod = useMemo(() => {
    if (!calculatorRows.length) return null;
    return [...calculatorRows].sort((a, b) => b.profitAuec - a.profitAuec)[0];
  }, [calculatorRows]);

  const fastestMethod = useMemo(() => {
    if (!calculatorRows.length) return null;
    return [...calculatorRows].sort((a, b) => a.hours - b.hours)[0];
  }, [calculatorRows]);

  return (
    <div className="flex flex-col min-h-full space-y-4">
      {/* Toast Notification */}
      {toastMsg && (
        <div className="fixed bottom-6 right-6 z-50 flex items-center gap-2.5 px-4 py-3 bg-slate-900/95 border border-amber-500/40 rounded-xl shadow-[0_0_20px_rgba(245,158,11,0.25)] backdrop-blur text-sm text-amber-200 animate-in fade-in slide-in-from-bottom-2">
          <Sparkles className="w-4 h-4 text-amber-400 shrink-0" />
          <span>{toastMsg}</span>
        </div>
      )}

      {/* Top Header Card */}
      <div className="bg-slate-900/80 border border-slate-800/80 rounded-xl p-4 backdrop-blur shadow-lg">
        <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="w-11 h-11 rounded-xl bg-amber-500/10 border border-amber-500/30 flex items-center justify-center text-amber-400 shadow-[0_0_15px_rgba(245,158,11,0.15)]">
              <Flame className="w-6 h-6" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-white tracking-wide flex items-center gap-2">
                Raffinerie & Veredelung
                <span className="text-xs px-2 py-0.5 rounded font-mono font-medium bg-amber-500/15 border border-amber-500/30 text-amber-300">
                  Stanton · Pyro · Nyx
                </span>
              </h1>
              <p className="text-xs text-slate-400">
                In-Game Kiosk OCR-Erfassung, Live-Auftragstracking, SCWiki & UEX Corp Ertrags-Kalkulator
              </p>
            </div>
          </div>

          {/* Stat Badges */}
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-2">
            <div className="bg-slate-950/60 border border-slate-800/60 rounded-lg p-2.5 text-center min-w-[110px]">
              <div className="text-[11px] text-slate-400">Laufend</div>
              <div className="text-base font-bold text-amber-400 font-mono">
                {stats.activeCount} {stats.activeCount === 1 ? 'Job' : 'Jobs'}
              </div>
            </div>
            <div className="bg-slate-950/60 border border-slate-800/60 rounded-lg p-2.5 text-center min-w-[110px]">
              <div className="text-[11px] text-slate-400">Abholbereit</div>
              <div className="text-base font-bold text-emerald-400 font-mono">
                {stats.readyCount} {stats.readyCount === 1 ? 'Fertig' : 'Fertig'}
              </div>
            </div>
            <div className="bg-slate-950/60 border border-slate-800/60 rounded-lg p-2.5 text-center min-w-[120px]">
              <div className="text-[11px] text-slate-400">Gesamt-Ertrag</div>
              <div className="text-base font-bold text-cyan-300 font-mono">
                {stats.totalYieldScu.toLocaleString('de-DE')} SCU
              </div>
            </div>
            <div className="bg-slate-950/60 border border-slate-800/60 rounded-lg p-2.5 text-center min-w-[130px]">
              <div className="text-[11px] text-slate-400">UEX Marktwert</div>
              <div className="text-base font-bold text-yellow-400 font-mono">
                ~{(stats.totalEstValue / 1000).toFixed(0)}k <span className="text-[10px] text-slate-500 font-sans">aUEC</span>
              </div>
            </div>
          </div>
        </div>

        {/* Tab Navigation */}
        <div className="flex flex-wrap items-center gap-2 mt-4 pt-3 border-t border-slate-800/60">
          <button
            onClick={() => setActiveTab('orders')}
            className={`flex items-center gap-2 px-3.5 py-1.5 rounded-lg text-xs font-medium transition ${
              activeTab === 'orders'
                ? 'bg-amber-500/20 text-amber-300 border border-amber-500/40 shadow-[0_0_10px_rgba(245,158,11,0.2)]'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/50'
            }`}
          >
            <Clock className="w-3.5 h-3.5" />
            1. Aktive Aufträge ({hauls.length})
          </button>
          <button
            onClick={() => setActiveTab('ocr')}
            className={`flex items-center gap-2 px-3.5 py-1.5 rounded-lg text-xs font-medium transition ${
              activeTab === 'ocr'
                ? 'bg-amber-500/20 text-amber-300 border border-amber-500/40 shadow-[0_0_10px_rgba(245,158,11,0.2)]'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/50'
            }`}
          >
            <Camera className="w-3.5 h-3.5" />
            2. Kiosk OCR-Scanner & Erfassung
          </button>
          <button
            onClick={() => setActiveTab('calculator')}
            className={`flex items-center gap-2 px-3.5 py-1.5 rounded-lg text-xs font-medium transition ${
              activeTab === 'calculator'
                ? 'bg-amber-500/20 text-amber-300 border border-amber-500/40 shadow-[0_0_10px_rgba(245,158,11,0.2)]'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/50'
            }`}
          >
            <Calculator className="w-3.5 h-3.5" />
            3. Methoden- & Ertrags-Rechner (UEX)
          </button>
          <button
            onClick={() => setActiveTab('stations')}
            className={`flex items-center gap-2 px-3.5 py-1.5 rounded-lg text-xs font-medium transition ${
              activeTab === 'stations'
                ? 'bg-amber-500/20 text-amber-300 border border-amber-500/40 shadow-[0_0_10px_rgba(245,158,11,0.2)]'
                : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/50'
            }`}
          >
            <Compass className="w-3.5 h-3.5" />
            4. Stations-Atlas (Stanton, Pyro & Nyx)
          </button>

          <div className="ml-auto flex items-center gap-2">
            <button
              onClick={fetchHauls}
              className="p-1.5 text-slate-400 hover:text-slate-200 hover:bg-slate-800/70 rounded-lg transition"
              title="Aktualisieren"
            >
              <RefreshCw className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>

      {/* ========================================================================= */}
      {/* TAB 1: AKTIVE AUFTRÄGE (LIVE WORK ORDERS) */}
      {/* ========================================================================= */}
      {activeTab === 'orders' && (
        <div className="space-y-4">
          {/* Filter & Suche Bar */}
          <div className="flex flex-col sm:flex-row gap-3 items-center justify-between bg-slate-900/60 border border-slate-800/60 rounded-xl p-3">
            <div className="flex flex-wrap items-center gap-2 w-full sm:w-auto">
              <div className="relative flex-1 sm:w-60">
                <Search className="w-4 h-4 text-slate-500 absolute left-3 top-2.5" />
                <input
                  type="text"
                  placeholder="Material, Station oder Methode..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  className="w-full bg-slate-950/80 border border-slate-800 rounded-lg pl-9 pr-3 py-1.5 text-xs text-slate-200 focus:outline-none focus:border-amber-500/50"
                />
              </div>

              {/* System Filter */}
              <div className="flex items-center bg-slate-950/70 border border-slate-800/80 rounded-lg p-0.5">
                {(['all', 'Stanton', 'Pyro', 'Nyx'] as const).map((sys) => (
                  <button
                    key={sys}
                    onClick={() => setSystemFilter(sys)}
                    className={`px-2.5 py-1 text-[11px] rounded transition ${
                      systemFilter === sys
                        ? sys === 'Stanton'
                          ? 'bg-cyan-500/20 text-cyan-300 font-semibold'
                          : sys === 'Pyro'
                          ? 'bg-orange-500/20 text-orange-300 font-semibold'
                          : sys === 'Nyx'
                          ? 'bg-purple-500/20 text-purple-300 font-semibold'
                          : 'bg-amber-500/20 text-amber-300 font-semibold'
                        : 'text-slate-400 hover:text-slate-200'
                    }`}
                  >
                    {sys === 'all' ? 'Alle Systeme' : sys}
                  </button>
                ))}
              </div>

              {/* Status Filter */}
              <div className="flex items-center bg-slate-950/70 border border-slate-800/80 rounded-lg p-0.5">
                {(['all', 'Refining', 'Ready', 'Collected'] as const).map((st) => (
                  <button
                    key={st}
                    onClick={() => setStatusFilter(st)}
                    className={`px-2.5 py-1 text-[11px] rounded transition ${
                      statusFilter === st
                        ? 'bg-slate-800 text-amber-300 font-semibold'
                        : 'text-slate-400 hover:text-slate-200'
                    }`}
                  >
                    {st === 'all'
                      ? 'Alle Status'
                      : st === 'Refining'
                      ? '⏳ Läuft'
                      : st === 'Ready'
                      ? '✓ Bereit'
                      : '📦 Abgeholt'}
                  </button>
                ))}
              </div>
            </div>

            <div className="flex items-center gap-2 w-full sm:w-auto justify-end">
              <button
                onClick={() => setShowAddModal(true)}
                className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium bg-amber-500/20 border border-amber-500/40 text-amber-300 hover:bg-amber-500/30 transition shadow-[0_0_10px_rgba(245,158,11,0.15)]"
              >
                <Plus className="w-3.5 h-3.5" />
                Manuell anlegen
              </button>
            </div>
          </div>

          {/* Orders Cards Grid */}
          {filteredHauls.length === 0 ? (
            <div className="bg-slate-900/40 border border-slate-800/60 rounded-xl p-10 text-center text-slate-500">
              <Flame className="w-10 h-10 mx-auto text-slate-600 mb-2 opacity-60" />
              <div className="text-sm text-slate-300 font-medium">Keine Veredelungsaufträge gefunden</div>
              <p className="text-xs text-slate-500 mt-1 max-w-md mx-auto">
                Scanne dein In-Game Raffinerie-Kiosk im Tab „Kiosk OCR-Scanner“ oder lege manuell einen Auftrag an.
              </p>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3">
              {filteredHauls.map((haul) => {
                const sys = detectSystemFromStation(haul.refineryLocation);
                const estPrice = ORE_BASE_PRICES[haul.materialName] || 15000;
                const estValue = Math.round(haul.yieldScu * estPrice);
                const isReady = haul.status === 'Ready' || haul.isTimerCompleted;
                const isCollected = haul.status === 'Collected';

                // Fortschrittsbalken berechnen
                const progressPct =
                  haul.durationSeconds > 0
                    ? Math.min(
                        100,
                        Math.max(
                          0,
                          Math.round(((haul.durationSeconds - haul.remainingSeconds) / haul.durationSeconds) * 100)
                        )
                      )
                    : 100;

                return (
                  <div
                    key={haul.id}
                    className={`bg-slate-900/80 border rounded-xl p-4 flex flex-col justify-between transition relative overflow-hidden backdrop-blur ${
                      isCollected
                        ? 'border-slate-800/60 opacity-60'
                        : isReady
                        ? 'border-emerald-500/50 shadow-[0_0_15px_rgba(16,185,129,0.15)]'
                        : 'border-slate-800/90 hover:border-amber-500/40'
                    }`}
                  >
                    {/* Header: System Pill & Status */}
                    <div>
                      <div className="flex items-center justify-between gap-2 mb-2">
                        <span
                          className={`text-[10px] font-mono px-2 py-0.5 rounded font-bold ${
                            sys === 'Stanton'
                              ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40'
                              : sys === 'Pyro'
                              ? 'bg-orange-500/20 text-orange-300 border border-orange-500/40'
                              : 'bg-purple-500/20 text-purple-300 border border-purple-500/40'
                          }`}
                        >
                          {sys.toUpperCase()}
                        </span>

                        <span
                          className={`text-xs px-2.5 py-0.5 rounded-full font-medium flex items-center gap-1 ${
                            isCollected
                              ? 'bg-slate-800 text-slate-400'
                              : isReady
                              ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/40 shadow-[0_0_10px_rgba(16,185,129,0.2)] animate-pulse'
                              : 'bg-amber-500/15 text-amber-300 border border-amber-500/30'
                          }`}
                        >
                          {isCollected ? (
                            '📦 Abgeholt'
                          ) : isReady ? (
                            <>
                              <CheckCircle2 className="w-3 h-3 text-emerald-400" />
                              Abholbereit!
                            </>
                          ) : (
                            <>
                              <Clock className="w-3 h-3 text-amber-400" />
                              Wird veredelt
                            </>
                          )}
                        </span>
                      </div>

                      {/* Material & SCU */}
                      <div className="flex items-start justify-between gap-2 mt-1">
                        <div>
                          <div className="text-base font-bold text-white tracking-wide flex items-center gap-2">
                            {haul.materialName}
                            {onOpenWiki && (
                              <button
                                onClick={() => onOpenWiki(haul.materialName)}
                                className="text-[10px] px-1.5 py-0.5 rounded bg-cyan-500/15 border border-cyan-500/30 text-cyan-300 hover:bg-cyan-500/25 transition"
                                title="Im SCWiki nachschlagen"
                              >
                                Wiki
                              </button>
                            )}
                          </div>
                          <div className="text-xs text-slate-400 font-mono mt-0.5 flex items-center gap-1.5">
                            <span>{haul.scuQuantity} SCU Input</span>
                            <span>→</span>
                            <span className="text-cyan-300 font-bold">{haul.yieldScu} SCU Ertrag</span>
                            <span className="text-[10px] text-slate-500">({haul.yieldPercent}%)</span>
                          </div>
                        </div>

                        {/* UEX Price Tag */}
                        <div className="text-right font-mono">
                          <div className="text-xs text-yellow-400 font-bold">
                            ~{estValue.toLocaleString('de-DE')} aUEC
                          </div>
                          <div className="text-[10px] text-slate-500">UEX Marktwert</div>
                        </div>
                      </div>

                      {/* Station & Method Info */}
                      <div className="mt-3 bg-slate-950/60 border border-slate-800/60 rounded-lg p-2.5 text-xs space-y-1">
                        <div className="flex items-center justify-between text-slate-400">
                          <span>Standort:</span>
                          <span className="text-slate-200 font-medium truncate max-w-[200px]" title={haul.refineryLocation}>
                            {haul.refineryLocation}
                          </span>
                        </div>
                        <div className="flex items-center justify-between text-slate-400">
                          <span>Methode:</span>
                          <span className="text-amber-300/90 font-medium truncate max-w-[200px]" title={haul.method}>
                            {haul.method}
                          </span>
                        </div>
                        <div className="flex items-center justify-between text-slate-400">
                          <span>Prozesskosten:</span>
                          <span className="text-slate-300 font-mono">{haul.costAuec.toLocaleString('de-DE')} aUEC</span>
                        </div>
                      </div>

                      {/* Live Timer & Progress Bar */}
                      {!isCollected && (
                        <div className="mt-3 space-y-1.5">
                          <div className="flex items-center justify-between text-xs font-mono">
                            <span className="text-slate-400 flex items-center gap-1">
                              <Timer className="w-3 h-3 text-slate-500" />
                              Restzeit:
                            </span>
                            <span className={`font-bold ${isReady ? 'text-emerald-400' : 'text-amber-300'}`}>
                              {isReady ? '✓ Bereit zur Abholung' : formatSeconds(haul.remainingSeconds)}
                            </span>
                          </div>

                          <div className="w-full bg-slate-950 rounded-full h-1.5 overflow-hidden border border-slate-800">
                            <div
                              className={`h-full transition-all duration-1000 ${
                                isReady ? 'bg-emerald-500' : 'bg-gradient-to-r from-amber-500 to-yellow-400'
                              }`}
                              style={{ width: `${progressPct}%` }}
                            />
                          </div>

                          <div className="flex items-center justify-between text-[10px] text-slate-500 font-mono">
                            <span>Gestartet: {haul.submittedAt || '—'}</span>
                            <span>{progressPct}%</span>
                          </div>
                        </div>
                      )}
                    </div>

                    {/* Actions Bottom Bar */}
                    <div className="mt-4 pt-3 border-t border-slate-800/80 flex items-center justify-between gap-2">
                      <button
                        onClick={() => handleDeleteHaul(haul.id)}
                        className="p-1.5 text-slate-500 hover:text-rose-400 hover:bg-rose-500/10 rounded transition"
                        title="Auftrag löschen"
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>

                      <div className="flex items-center gap-1.5">
                        {isReady && !isCollected && (
                          <button
                            onClick={() => handleTransferToWarehouse(haul)}
                            className="flex items-center gap-1 px-2.5 py-1 rounded text-xs font-medium bg-cyan-500/20 text-cyan-300 border border-cyan-500/40 hover:bg-cyan-500/30 transition shadow-[0_0_10px_rgba(6,182,212,0.2)]"
                            title="Ins Warenlager der Station übertragen"
                          >
                            <Box className="w-3 h-3" />
                            Ins Lager
                          </button>
                        )}

                        {!isCollected && (
                          <button
                            onClick={() => handleUpdateStatus(haul.id, isReady ? 'Collected' : 'Ready')}
                            className="flex items-center gap-1 px-2.5 py-1 rounded text-xs font-medium bg-slate-800 text-slate-300 hover:bg-slate-700 hover:text-white transition"
                          >
                            <CheckCircle2 className="w-3 h-3 text-emerald-400" />
                            {isReady ? 'Als abgeholt' : 'Sofort bereit'}
                          </button>
                        )}
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}

      {/* ========================================================================= */}
      {/* TAB 2: KIOSK OCR-SCANNER & SCHNELL-ERFASSUNG */}
      {/* ========================================================================= */}
      {activeTab === 'ocr' && (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
          {/* Left Column: Actions & Guidance */}
          <div className="lg:col-span-1 space-y-4">
            <div className="bg-slate-900/80 border border-slate-800/80 rounded-xl p-4 backdrop-blur space-y-3">
              <h2 className="text-sm font-bold text-white flex items-center gap-2">
                <Camera className="w-4 h-4 text-amber-400" />
                In-Game Terminal erfassen
              </h2>
              <p className="text-xs text-slate-400 leading-relaxed">
                Stelle dich in Star Citizen vor das Raffinerie-Terminal (*Refinery Kiosk*). Ein Klick liest
                Auftragsnummer, Rohstoffe, Restzeiten und Methoden vollautomatisch aus dem Bildschirm!
              </p>

              {/* Action Buttons */}
              <div className="space-y-2 pt-2">
                <button
                  onClick={handleScanInstant}
                  disabled={isScanning || countdownSec !== null}
                  className="w-full flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl text-xs font-bold bg-gradient-to-r from-amber-500 to-yellow-500 hover:from-amber-400 hover:to-yellow-400 text-slate-950 shadow-[0_0_15px_rgba(245,158,11,0.3)] transition disabled:opacity-50"
                >
                  <Camera className="w-4 h-4" />
                  {isScanning ? 'Scan läuft...' : 'Kiosk scannen (Sofort)'}
                </button>

                <button
                  onClick={handleScanWithCountdown}
                  disabled={isScanning || countdownSec !== null}
                  className="w-full flex items-center justify-center gap-2 px-4 py-2 rounded-xl text-xs font-semibold bg-slate-800/90 hover:bg-slate-800 text-amber-300 border border-amber-500/30 transition disabled:opacity-50"
                >
                  <Clock className="w-4 h-4 text-amber-400" />
                  {countdownSec !== null ? `Reintappen ins Spiel... (${countdownSec}s)` : 'Scan in 3s (Vorlaufzeit)'}
                </button>

                <button
                  onClick={handleSelectRegion}
                  className="w-full flex items-center justify-center gap-2 px-4 py-2 rounded-xl text-xs font-medium bg-slate-950/80 hover:bg-slate-800 text-slate-300 border border-slate-800 transition"
                >
                  <Crop className="w-4 h-4 text-cyan-400" />
                  Scanbereich anpassen (Snipping)
                </button>

                <button
                  onClick={handleTestScan}
                  disabled={isScanning}
                  className="w-full flex items-center justify-center gap-2 px-4 py-1.5 rounded-lg text-xs text-slate-400 hover:text-slate-200 hover:bg-slate-800/60 transition"
                >
                  <RefreshCw className="w-3.5 h-3.5" />
                  Test-Scan (Debug Text)
                </button>
              </div>
            </div>

            {/* Conflict-Free Hotkey Hint */}
            <div className="bg-slate-900/60 border border-slate-800/60 rounded-xl p-3.5 text-xs text-slate-400 space-y-1.5">
              <div className="flex items-center gap-1.5 text-amber-300 font-semibold">
                <HelpCircle className="w-3.5 h-3.5 text-amber-400" />
                Sichere Bedienung im Spiel
              </div>
              <p className="text-[11px] leading-relaxed">
                Da Star Citizen viele Hotkeys belegt, ist der globale Tastatur-Hotkey standardmäßig deaktiviert. Du
                kannst den Vorlauf-Button nutzen, das Mini-HUD verwenden oder in den Einstellungen eine freie Taste (z.
                B. <span className="text-white font-mono">F10</span> oder <span className="text-white font-mono">Pause</span>)
                belegen.
              </p>
            </div>
          </div>

          {/* Right Column: Scan Preview & Parsed Orders */}
          <div className="lg:col-span-2 space-y-4">
            <div className="bg-slate-900/80 border border-slate-800/80 rounded-xl p-4 backdrop-blur min-h-[380px] flex flex-col">
              <h3 className="text-xs font-bold text-slate-300 uppercase tracking-wider mb-2 flex items-center gap-2">
                <Sparkles className="w-3.5 h-3.5 text-amber-400" />
                Erkannte Kiosk-Daten & Rohdaten
              </h3>

              {lastScanResult?.orders && lastScanResult.orders.length > 0 ? (
                <div className="space-y-3">
                  <div className="text-xs text-emerald-400 font-medium">
                    ✓ {lastScanResult.orders.length} Auftrag/Aufträge erfolgreich erkannt und gespeichert:
                  </div>

                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
                    {lastScanResult.orders.map((o: any, idx: number) => (
                      <div
                        key={idx}
                        className="bg-slate-950/80 border border-slate-800 rounded-lg p-3 text-xs space-y-1 font-mono"
                      >
                        <div className="text-amber-300 font-bold text-sm font-sans flex items-center justify-between">
                          <span>{o.materialName}</span>
                          <span className="text-cyan-300 text-xs">{o.scuQuantity} SCU</span>
                        </div>
                        <div className="text-slate-400">Methode: {o.method}</div>
                        <div className="text-slate-400">Station: {o.refineryLocation}</div>
                        <div className="text-emerald-400">
                          Restzeit: {o.remainingSeconds > 0 ? formatSeconds(o.remainingSeconds) : 'Bereit!'}
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              ) : (
                <div className="flex-1 flex flex-col items-center justify-center text-center p-8 text-slate-500 border border-dashed border-slate-800 rounded-lg">
                  <Camera className="w-8 h-8 text-slate-600 mb-2 opacity-60" />
                  <div className="text-sm text-slate-400">Noch kein Kiosk gescannt</div>
                  <p className="text-xs text-slate-500 mt-1 max-w-sm">
                    Klicke links auf „Kiosk scannen (3s Vorlauf)“, wechsle ins Spiel und blicke auf den Bildschirm des
                    Raffinerie-Terminals.
                  </p>
                </div>
              )}

              {/* Raw OCR Text Box */}
              {lastOcrText && (
                <div className="mt-4 pt-3 border-t border-slate-800">
                  <div className="flex items-center justify-between mb-1">
                    <span className="text-[11px] text-slate-400 font-mono">Erfasster Rohtext:</span>
                    <button
                      type="button"
                      onClick={() => {
                        navigator.clipboard.writeText(lastOcrText);
                        showToast('✓ Rohtext in Zwischenablage kopiert!');
                      }}
                      className="px-2 py-0.5 text-[10px] font-mono bg-slate-800 hover:bg-slate-700 text-slate-300 rounded border border-slate-700/80 transition flex items-center gap-1 cursor-pointer"
                    >
                      <Copy className="w-3 h-3" />
                      Kopieren
                    </button>
                  </div>
                  <pre className="text-[11px] bg-slate-950 p-2.5 rounded border border-slate-800/80 text-slate-300 font-mono max-h-36 overflow-y-auto whitespace-pre-wrap select-text cursor-text">
                    {lastOcrText}
                  </pre>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* ========================================================================= */}
      {/* TAB 3: METHODEN- & ERTRAGS-RECHNER (UEX CORP) */}
      {/* ========================================================================= */}
      {activeTab === 'calculator' && (
        <div className="space-y-4">
          {/* Controls Bar */}
          <div className="bg-slate-900/80 border border-slate-800/80 rounded-xl p-4 backdrop-blur space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              {/* Material Dropdown */}
              <div>
                <label className="text-xs text-slate-400 mb-1 block">Zu veredelndes Erz / Mineral:</label>
                <select
                  value={calcMaterial}
                  onChange={(e) => setCalcMaterial(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-xs text-slate-200 focus:outline-none focus:border-amber-500/50"
                >
                  {catalog?.materials.map((m) => (
                    <option key={m} value={m}>
                      {m} (~{(ORE_BASE_PRICES[m] || 15000).toLocaleString('de-DE')} aUEC / SCU)
                    </option>
                  )) || (
                    <option value="Quantanium">Quantanium</option>
                  )}
                </select>
              </div>

              {/* Quantity SCU */}
              <div>
                <label className="text-xs text-slate-400 mb-1 block">Rohstoff-Menge (SCU):</label>
                <div className="flex items-center gap-2">
                  <input
                    type="number"
                    min="1"
                    max="10000"
                    value={calcScu}
                    onChange={(e) => setCalcScu(Math.max(1, parseInt(e.target.value) || 1))}
                    className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-xs text-slate-200 focus:outline-none focus:border-amber-500/50 font-mono"
                  />
                  <div className="flex gap-1">
                    {[32, 64, 96, 128].map((preset) => (
                      <button
                        key={preset}
                        onClick={() => setCalcScu(preset)}
                        className={`px-2 py-1.5 text-[10px] rounded border transition font-mono ${
                          calcScu === preset
                            ? 'bg-amber-500/20 text-amber-300 border-amber-500/40'
                            : 'bg-slate-950 border-slate-800 text-slate-400 hover:text-slate-200'
                        }`}
                      >
                        {preset}
                      </button>
                    ))}
                  </div>
                </div>
              </div>

              {/* Station Dropdown */}
              <div>
                <label className="text-xs text-slate-400 mb-1 block">Raffinerie-Station (mit Boni):</label>
                <select
                  value={calcStation}
                  onChange={(e) => setCalcStation(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-xs text-slate-200 focus:outline-none focus:border-amber-500/50"
                >
                  {catalog?.stations.map((st) => {
                    const bonus = st.materialYieldBonuses[calcMaterial];
                    return (
                      <option key={st.id} value={st.name}>
                        [{st.system}] {st.name} {bonus ? `(+${Math.round(bonus * 100)}% Bonus!)` : ''}
                      </option>
                    );
                  }) || <option value="ARC-L1">ARC-L1</option>}
                </select>
              </div>
            </div>
          </div>

          {/* Winner Badges */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {bestProfitMethod && (
              <div className="bg-amber-500/10 border border-amber-500/30 rounded-xl p-3.5 flex items-center gap-3">
                <div className="w-10 h-10 rounded-lg bg-amber-500/20 border border-amber-500/40 flex items-center justify-center text-amber-400 shrink-0">
                  <Coins className="w-5 h-5" />
                </div>
                <div>
                  <div className="text-[11px] text-amber-300 font-semibold uppercase tracking-wider">
                    ★ Maximaler Netto-Gewinn
                  </div>
                  <div className="text-sm font-bold text-white mt-0.5">
                    {bestProfitMethod.method.displayName}
                  </div>
                  <div className="text-xs text-slate-400 font-mono mt-0.5">
                    {bestProfitMethod.yieldScu} SCU · Netto-Gewinn: ~
                    <span className="text-emerald-400 font-bold">
                      {bestProfitMethod.profitAuec.toLocaleString('de-DE')} aUEC
                    </span>
                  </div>
                </div>
              </div>
            )}

            {fastestMethod && (
              <div className="bg-cyan-500/10 border border-cyan-500/30 rounded-xl p-3.5 flex items-center gap-3">
                <div className="w-10 h-10 rounded-lg bg-cyan-500/20 border border-cyan-500/40 flex items-center justify-center text-cyan-400 shrink-0">
                  <Zap className="w-5 h-5" />
                </div>
                <div>
                  <div className="text-[11px] text-cyan-300 font-semibold uppercase tracking-wider">
                    ⚡ Schnellste Fertigstellung
                  </div>
                  <div className="text-sm font-bold text-white mt-0.5">
                    {fastestMethod.method.displayName}
                  </div>
                  <div className="text-xs text-slate-400 font-mono mt-0.5">
                    Fertig in nur <span className="text-cyan-300 font-bold">{fastestMethod.hours}h</span> · Ertrag:{' '}
                    {fastestMethod.yieldScu} SCU
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Methods Comparison Table */}
          <div className="bg-slate-900/80 border border-slate-800/80 rounded-xl overflow-hidden shadow-lg">
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead>
                  <tr className="bg-slate-950/80 border-b border-slate-800 text-slate-400 font-mono text-[11px]">
                    <th className="py-3 px-4">Veredelungsmethode</th>
                    <th className="py-3 px-3">Ausbeute</th>
                    <th className="py-3 px-3">Ertrag</th>
                    <th className="py-3 px-3">Geschwindigkeit</th>
                    <th className="py-3 px-3">Dauer</th>
                    <th className="py-3 px-3">Kosten</th>
                    <th className="py-3 px-4 text-right">Netto-Gewinn (UEX)</th>
                    <th className="py-3 px-3 text-center">Aktion</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-800/60">
                  {calculatorRows.map((row) => {
                    const isBest = row.method.name === bestProfitMethod?.method.name;
                    return (
                      <tr
                        key={row.method.name}
                        className={`hover:bg-slate-800/40 transition ${
                          isBest ? 'bg-amber-500/5' : ''
                        }`}
                      >
                        <td className="py-3 px-4">
                          <div className="font-bold text-white flex items-center gap-2">
                            {row.method.displayName}
                            {isBest && (
                              <span className="text-[10px] px-1.5 py-0.2 rounded bg-amber-500/20 text-amber-300 border border-amber-500/40 font-mono">
                                Top-Profit
                              </span>
                            )}
                          </div>
                          <div className="text-[10px] text-slate-400 mt-0.5 max-w-sm line-clamp-1">
                            {row.method.description}
                          </div>
                        </td>
                        <td className="py-3 px-3 font-mono font-bold text-cyan-300">
                          {row.effYieldPct}%
                        </td>
                        <td className="py-3 px-3 font-mono text-slate-200">
                          {row.yieldScu} SCU
                        </td>
                        <td className="py-3 px-3 text-slate-300">
                          <span
                            className={`px-2 py-0.5 rounded text-[10px] font-mono ${
                              row.method.speedRating.includes('Schnell')
                                ? 'bg-cyan-500/15 text-cyan-300'
                                : row.method.speedRating.includes('Langsam')
                                ? 'bg-rose-500/15 text-rose-300'
                                : 'bg-slate-800 text-slate-300'
                            }`}
                          >
                            {row.method.speedRating}
                          </span>
                        </td>
                        <td className="py-3 px-3 font-mono text-slate-300">
                          ~{row.hours} Std.
                        </td>
                        <td className="py-3 px-3 font-mono text-slate-400">
                          {row.costAuec.toLocaleString('de-DE')} aUEC
                        </td>
                        <td className="py-3 px-4 font-mono text-right">
                          <div className="font-bold text-emerald-400 text-sm">
                            +{row.profitAuec.toLocaleString('de-DE')} aUEC
                          </div>
                          <div className="text-[10px] text-slate-500">
                            Brutto: ~{row.revenueAuec.toLocaleString('de-DE')}
                          </div>
                        </td>
                        <td className="py-3 px-3 text-center">
                          <button
                            onClick={async () => {
                              try {
                                await bridge.sendRequest('save_mining_haul', {
                                  materialName: calcMaterial,
                                  scuQuantity: calcScu,
                                  refineryLocation: calcStation,
                                  method: row.method.name,
                                  yieldPercent: row.effYieldPct,
                                  costAuec: row.costAuec,
                                  durationSeconds: Math.round(row.hours * 3600),
                                  status: 'Refining',
                                  soldAuec: 0,
                                });
                                fetchHauls();
                                setActiveTab('orders');
                                showToast(`✓ Auftrag gestartet: ${calcScu} SCU ${calcMaterial} mit ${row.method.name}!`);
                              } catch (err) {
                                console.error('Failed to create order from calculator:', err);
                              }
                            }}
                            className="px-2.5 py-1 rounded text-[11px] font-medium bg-amber-500/20 text-amber-300 border border-amber-500/30 hover:bg-amber-500/30 transition whitespace-nowrap"
                          >
                            Als Auftrag
                          </button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* ========================================================================= */}
      {/* TAB 4: STATIONS-ATLAS (STANTON, PYRO & NYX) */}
      {/* ========================================================================= */}
      {activeTab === 'stations' && (
        <div className="space-y-4">
          {/* System Filters */}
          <div className="flex items-center gap-2 bg-slate-900/60 border border-slate-800/60 rounded-xl p-3">
            <span className="text-xs text-slate-400 font-medium mr-2">System filtern:</span>
            {(['all', 'Stanton', 'Pyro', 'Nyx'] as const).map((sys) => (
              <button
                key={sys}
                onClick={() => setSystemFilter(sys)}
                className={`px-3 py-1.5 text-xs rounded-lg transition font-medium ${
                  systemFilter === sys
                    ? sys === 'Stanton'
                      ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40'
                      : sys === 'Pyro'
                      ? 'bg-orange-500/20 text-orange-300 border border-orange-500/40'
                      : sys === 'Nyx'
                      ? 'bg-purple-500/20 text-purple-300 border border-purple-500/40'
                      : 'bg-amber-500/20 text-amber-300 border border-amber-500/40'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/50'
                }`}
              >
                {sys === 'all' ? 'Alle Systeme (Stanton, Pyro & Nyx)' : sys}
              </button>
            ))}
          </div>

          {/* Stations Grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3">
            {(catalog?.stations || [])
              .filter((st) => systemFilter === 'all' || st.system === systemFilter)
              .map((st) => (
                <div
                  key={st.id}
                  className="bg-slate-900/80 border border-slate-800/80 rounded-xl p-4 backdrop-blur flex flex-col justify-between space-y-3 hover:border-amber-500/30 transition"
                >
                  <div>
                    {/* Header: System & Armistice */}
                    <div className="flex items-center justify-between gap-2 mb-1.5">
                      <span
                        className={`text-[10px] font-mono px-2 py-0.5 rounded font-bold ${
                          st.system === 'Stanton'
                            ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40'
                            : st.system === 'Pyro'
                            ? 'bg-orange-500/20 text-orange-300 border border-orange-500/40'
                            : 'bg-purple-500/20 text-purple-300 border border-purple-500/40'
                        }`}
                      >
                        {st.system.toUpperCase()}
                      </span>

                      <span
                        className={`text-[10px] px-2 py-0.5 rounded font-medium ${
                          st.hasArmistice
                            ? 'bg-emerald-500/15 text-emerald-300 border border-emerald-500/30'
                            : 'bg-rose-500/15 text-rose-300 border border-rose-500/30'
                        }`}
                      >
                        {st.hasArmistice ? 'Waffenruhe (Armistice)' : 'Gesetzlos / Kein Schutz'}
                      </span>
                    </div>

                    <h3 className="text-sm font-bold text-white tracking-wide">{st.name}</h3>
                    <div className="text-xs text-slate-400 font-mono mt-0.5">{st.locationType}</div>
                    <p className="text-xs text-slate-300 mt-2 leading-relaxed">{st.description}</p>

                    {/* Material Bonuses */}
                    {Object.keys(st.materialYieldBonuses).length > 0 && (
                      <div className="mt-3 pt-2.5 border-t border-slate-800/70">
                        <div className="text-[11px] text-amber-300 font-semibold mb-1.5 flex items-center gap-1">
                          <TrendingUp className="w-3 h-3 text-amber-400" />
                          Stations-Spezialisierungen & Boni:
                        </div>
                        <div className="flex flex-wrap gap-1.5">
                          {Object.entries(st.materialYieldBonuses).map(([mat, b]) => (
                            <span
                              key={mat}
                              className="text-[11px] px-2 py-0.5 rounded bg-amber-500/15 border border-amber-500/30 text-amber-200 font-mono"
                            >
                              {mat} <span className="font-bold text-emerald-400">+{Math.round(b * 100)}%</span>
                            </span>
                          ))}
                        </div>
                      </div>
                    )}
                  </div>

                  {/* Preferred Methods & Wiki Link */}
                  <div className="pt-2.5 border-t border-slate-800/70 flex items-center justify-between gap-2">
                    <div className="text-[11px] text-slate-400 truncate max-w-[180px]">
                      Empfohlen: {st.preferredMethods.join(', ')}
                    </div>

                    {onOpenWiki && (
                      <button
                        onClick={() => onOpenWiki(st.name)}
                        className="flex items-center gap-1 px-2.5 py-1 rounded text-xs font-medium bg-cyan-500/15 text-cyan-300 border border-cyan-500/30 hover:bg-cyan-500/25 transition"
                      >
                        <BookOpen className="w-3 h-3" />
                        Wiki
                      </button>
                    )}
                  </div>
                </div>
              ))}
          </div>
        </div>
      )}

      {/* ========================================================================= */}
      {/* MODAL: MANUELLEN AUFTRAG ANLEGEN */}
      {/* ========================================================================= */}
      {showAddModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/75 backdrop-blur-sm animate-in fade-in">
          <div className="bg-slate-900 border border-slate-800 rounded-xl p-5 max-w-md w-full shadow-2xl space-y-4">
            <div className="flex items-center justify-between border-b border-slate-800 pb-3">
              <h3 className="text-sm font-bold text-white flex items-center gap-2">
                <Plus className="w-4 h-4 text-amber-400" />
                Veredelungsauftrag manuell anlegen
              </h3>
              <button
                onClick={() => setShowAddModal(false)}
                className="text-slate-500 hover:text-slate-300 text-sm"
              >
                ✕
              </button>
            </div>

            <form onSubmit={handleSaveManualHaul} className="space-y-3 text-xs">
              {/* System Switcher */}
              <div>
                <label className="text-slate-400 mb-1 block">Sternensystem:</label>
                <div className="grid grid-cols-3 gap-1.5">
                  {(['Stanton', 'Pyro', 'Nyx'] as const).map((sys) => (
                    <button
                      key={sys}
                      type="button"
                      onClick={() => {
                        setFormSystem(sys);
                        const firstForSys = catalog?.stations.find((s) => s.system === sys);
                        if (firstForSys) setFormStation(firstForSys.name);
                      }}
                      className={`py-1.5 text-xs rounded font-medium border transition ${
                        formSystem === sys
                          ? sys === 'Stanton'
                            ? 'bg-cyan-500/20 text-cyan-300 border-cyan-500/40'
                            : sys === 'Pyro'
                            ? 'bg-orange-500/20 text-orange-300 border-orange-500/40'
                            : 'bg-purple-500/20 text-purple-300 border-purple-500/40'
                          : 'bg-slate-950 border-slate-800 text-slate-400'
                      }`}
                    >
                      {sys}
                    </button>
                  ))}
                </div>
              </div>

              {/* Station Dropdown */}
              <div>
                <label className="text-slate-400 mb-1 block">Raffinerie-Station:</label>
                <select
                  value={formStation}
                  onChange={(e) => setFormStation(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-slate-200 focus:outline-none focus:border-amber-500/50"
                >
                  {formStationsForSystem.map((st) => (
                    <option key={st.id} value={st.name}>
                      {st.name}
                    </option>
                  ))}
                </select>
              </div>

              {/* Material Dropdown */}
              <div>
                <label className="text-slate-400 mb-1 block">Rohstoff / Erz:</label>
                <select
                  value={formMaterial}
                  onChange={(e) => setFormMaterial(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-slate-200 focus:outline-none focus:border-amber-500/50"
                >
                  {catalog?.materials.map((m) => (
                    <option key={m} value={m}>
                      {m}
                    </option>
                  )) || <option value="Quantanium">Quantanium</option>}
                </select>
              </div>

              {/* Quantity SCU */}
              <div>
                <label className="text-slate-400 mb-1 block">Menge (SCU):</label>
                <input
                  type="number"
                  min="1"
                  max="10000"
                  value={formScu}
                  onChange={(e) => setFormScu(Math.max(1, parseInt(e.target.value) || 1))}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-slate-200 focus:outline-none focus:border-amber-500/50 font-mono"
                />
              </div>

              {/* Method Dropdown */}
              <div>
                <label className="text-slate-400 mb-1 block">Veredelungsmethode:</label>
                <select
                  value={formMethod}
                  onChange={(e) => setFormMethod(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-slate-200 focus:outline-none focus:border-amber-500/50"
                >
                  {catalog?.methods.map((m) => (
                    <option key={m.name} value={m.name}>
                      {m.displayName} ({m.baseYield}% Ertrag)
                    </option>
                  )) || <option value="Dinyx Dodecathetic">Dinyx Dodecathetic</option>}
                </select>
              </div>

              {/* Duration Hours */}
              <div>
                <label className="text-slate-400 mb-1 block">Geschätzte Verarbeitungsdauer (Stunden):</label>
                <input
                  type="number"
                  step="0.5"
                  min="0.1"
                  max="48"
                  value={formDurationHours}
                  onChange={(e) => setFormDurationHours(parseFloat(e.target.value) || 2.0)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2 text-slate-200 focus:outline-none focus:border-amber-500/50 font-mono"
                />
              </div>

              <div className="pt-3 border-t border-slate-800 flex items-center justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setShowAddModal(false)}
                  className="px-3.5 py-1.5 rounded-lg text-slate-400 hover:text-slate-200 hover:bg-slate-800 transition"
                >
                  Abbrechen
                </button>
                <button
                  type="submit"
                  className="px-4 py-1.5 rounded-lg text-xs font-bold bg-amber-500 text-slate-950 hover:bg-amber-400 transition"
                >
                  Auftrag starten
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
