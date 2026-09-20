import React, { useEffect, useRef, useState, useMemo } from 'react';
import {
  bridge,
  FleetShipDto,
  FleetResponseDto,
} from '../services/photinoBridge';
import {
  Rocket,
  Plus,
  Star,
  Check,
  X,
  Edit2,
  ExternalLink,
  Search,
  Coins,
  Warehouse,
  Camera,
  Scale,
  Wrench,
  Palette,
  Clipboard,
  Eye,
  Zap,
  ChevronDown,
} from 'lucide-react';
import { ShipCompareModal } from '../components/ShipCompareModal';
import { ShipLoadoutModal } from '../components/ShipLoadoutModal';

export interface FleetViewProps {
  initialSearch?: string;
  initialTab?: 'hangar' | 'history';
}

export const FleetView: React.FC<FleetViewProps> = ({
  initialSearch,
  initialTab,
}) => {
  const [fleetData, setFleetData] = useState<FleetResponseDto | null>(null);
  const [activeTab, setActiveTab] = useState<'hangar' | 'history'>(initialTab || 'hangar');
  const [search, setSearch] = useState<string>(initialSearch || '');
  const [selectedAcquisition, setSelectedAcquisition] = useState<string>('Alle');
  const [selectedManufacturer, setSelectedManufacturer] = useState<string>('Alle');
  const [openAcqMenuShip, setOpenAcqMenuShip] = useState<string | null>(null);

  const autoSwitchedRef = useRef<string | null>(null);

  const handleTabClick = (tab: 'hangar' | 'history') => {
    autoSwitchedRef.current = initialSearch || '__user_selected__';
    setActiveTab(tab);
  };

  const handleClearSearch = () => {
    setSearch('');
    autoSwitchedRef.current = initialSearch || '__cleared__';
  };

  // Modal for "+ Schiff hinzufügen"
  const [isAddModalOpen, setIsAddModalOpen] = useState<boolean>(false);
  const [catalogSearch, setCatalogSearch] = useState<string>('');

  // Comparison modal state
  const [isCompareModalOpen, setIsCompareModalOpen] = useState<boolean>(false);
  const [compareShipA, setCompareShipA] = useState<string | undefined>(undefined);
  const [compareShipB, setCompareShipB] = useState<string | undefined>(undefined);

  // Loadout inspection modal state
  const [isLoadoutModalOpen, setIsLoadoutModalOpen] = useState<boolean>(false);
  const [selectedLoadoutShip, setSelectedLoadoutShip] = useState<FleetShipDto | null>(null);

  // Screenshot scan state
  const [isScanningScreenshot, setIsScanningScreenshot] = useState<boolean>(false);
  const [screenshotFeedback, setScreenshotFeedback] = useState<string | null>(null);

  // Inline pledge editing state
  const [editingPledgeShip, setEditingPledgeShip] = useState<string | null>(null);
  const [pledgeInputVal, setPledgeInputVal] = useState<string>('');

  // Notes editing state
  const [editingNotesShip, setEditingNotesShip] = useState<string | null>(null);
  const [notesInputVal, setNotesInputVal] = useState<string>('');

  const fetchFleet = async () => {
    try {
      const res = await bridge.sendRequest<FleetResponseDto>('get_fleet');
      setFleetData(res);
    } catch (err) {
      console.error('Failed to load fleet data:', err);
    }
  };

  useEffect(() => {
    fetchFleet();

    const unsub = bridge.on<FleetResponseDto>('FLEET_UPDATED', (data) => {
      if (data) setFleetData(data);
    });

    const unsubScreenshot = bridge.on<any>('SCREENSHOT_LOADOUT_DETECTED', (res) => {
      if (res?.success) {
        setScreenshotFeedback(`✓ Neuer Screenshot: ${res.shipName || 'Schiff'} (${res.components?.length || 0} Komponenten)`);
        fetchFleet();
        setTimeout(() => setScreenshotFeedback(null), 6000);
      }
    });

    return () => {
      unsub();
      unsubScreenshot();
    };
  }, []);

  useEffect(() => {
    if (selectedLoadoutShip && fleetData?.ships) {
      const updated = fleetData.ships.find(s => s.name === selectedLoadoutShip.name);
      if (updated) setSelectedLoadoutShip(updated);
    }
  }, [fleetData]);

  useEffect(() => {
    if (initialSearch !== undefined) {
      setSearch(initialSearch);
      if (initialTab) {
        setActiveTab(initialTab);
        autoSwitchedRef.current = initialSearch || '__explicit_tab__';
      } else if (initialSearch !== autoSwitchedRef.current) {
        autoSwitchedRef.current = null;
      }
    }
  }, [initialSearch, initialTab]);

  useEffect(() => {
    if (
      initialSearch &&
      !initialTab &&
      autoSwitchedRef.current !== initialSearch &&
      fleetData?.ships &&
      fleetData.ships.length > 0
    ) {
      const query = initialSearch.toLowerCase();
      const found = fleetData.ships.find((s) => s.name.toLowerCase().includes(query));
      if (found && !found.isInHangar) {
        setActiveTab('history');
      }
      autoSwitchedRef.current = initialSearch;
    }
  }, [fleetData, initialSearch, initialTab]);

  const handleScanScreenshot = async () => {
    setIsScanningScreenshot(true);
    setScreenshotFeedback('Scanne Screenshots (OCR läuft)...');
    try {
      const res = await bridge.scanScreenshotLoadout();
      if (res.success) {
        setScreenshotFeedback(res.message || `✓ ${res.shipName || 'Schiff'} erkannt (${res.components?.length || 0} Komponenten)`);
        fetchFleet();
      } else {
        setScreenshotFeedback(`✕ ${res.message || 'Kein VLM/Hangar-Screenshot erkannt'}`);
      }
    } catch (err: any) {
      setScreenshotFeedback(`✕ Fehler: ${err?.message || 'Scan fehlgeschlagen'}`);
    } finally {
      setIsScanningScreenshot(false);
      setTimeout(() => setScreenshotFeedback(null), 8000);
    }
  };

  const handleScanClipboard = async () => {
    setIsScanningScreenshot(true);
    setScreenshotFeedback('Scanne Zwischenablage (OCR läuft)...');
    try {
      const res = await bridge.scanClipboardLoadout();
      if (res.success) {
        setScreenshotFeedback(res.message || `✓ ${res.shipName || 'Schiff'} erkannt (${res.components?.length || 0} Komponenten)`);
        fetchFleet();
      } else {
        setScreenshotFeedback(`✕ ${res.message || 'Kein Ausrüstungsbild in der Zwischenablage erkannt'}`);
      }
    } catch (err: any) {
      setScreenshotFeedback(`✕ Fehler: ${err?.message || 'Scan fehlgeschlagen'}`);
    } finally {
      setIsScanningScreenshot(false);
      setTimeout(() => setScreenshotFeedback(null), 8000);
    }
  };

  useEffect(() => {
    const handlePaste = (e: ClipboardEvent) => {
      // Wenn der Fokus in einem Eingabefeld liegt, natives Pasten nicht abfangen
      const tag = (e.target as HTMLElement)?.tagName?.toLowerCase();
      if (tag === 'input' || tag === 'textarea') return;
      if (isScanningScreenshot) return;
      handleScanClipboard();
    };
    window.addEventListener('paste', handlePaste);
    return () => window.removeEventListener('paste', handlePaste);
  }, [isScanningScreenshot]);

  useEffect(() => {
    if (!openAcqMenuShip) return;
    const handleOutsideClick = () => setOpenAcqMenuShip(null);
    window.addEventListener('click', handleOutsideClick);
    return () => window.removeEventListener('click', handleOutsideClick);
  }, [openAcqMenuShip]);

  // Actions
  const handleToggleHangar = async (shipName: string) => {
    try {
      const res = await bridge.sendRequest<FleetResponseDto>('toggle_ship_hangar', { shipName });
      setFleetData(res);
    } catch (err) {
      console.error('Failed to toggle ship hangar:', err);
    }
  };

  const handleAddCatalogShip = async (shipName: string) => {
    try {
      const res = await bridge.sendRequest<FleetResponseDto>('add_catalog_ship_to_hangar', { shipName });
      setFleetData(res);
      setIsAddModalOpen(false);
    } catch (err) {
      console.error('Failed to add ship to hangar:', err);
    }
  };

  const handleSetAcquisition = async (shipName: string, acquisition: string) => {
    try {
      const res = await bridge.sendRequest<FleetResponseDto>('set_ship_acquisition', { shipName, acquisition });
      setFleetData(res);
    } catch (err) {
      console.error('Failed to set acquisition:', err);
    }
  };

  const handleCycleInsurance = async (shipName: string) => {
    try {
      const res = await bridge.sendRequest<FleetResponseDto>('cycle_ship_insurance', { shipName });
      setFleetData(res);
    } catch (err) {
      console.error('Failed to cycle insurance:', err);
    }
  };

  const handleStartEditPledge = (ship: FleetShipDto) => {
    setEditingPledgeShip(ship.name);
    setPledgeInputVal(String(ship.pledgeValueUsd || 0));
  };

  const handleSavePledge = async (shipName: string) => {
    try {
      const pUsd = parseInt(pledgeInputVal, 10) || 0;
      const res = await bridge.sendRequest<FleetResponseDto>('update_ship_pledge', { shipName, pledgeUsd: pUsd });
      setFleetData(res);
      setEditingPledgeShip(null);
    } catch (err) {
      console.error('Failed to save pledge value:', err);
    }
  };

  const handleSaveNotes = async (shipName: string) => {
    try {
      const res = await bridge.sendRequest<FleetResponseDto>('update_ship_notes', { shipName, notes: notesInputVal });
      setFleetData(res);
      setEditingNotesShip(null);
    } catch (err) {
      console.error('Failed to save notes:', err);
    }
  };

  const handleSetCurrentShip = async (shipName: string) => {
    try {
      await bridge.sendRequest('set_current_ship', { shipName });
      fetchFleet();
    } catch (err) {
      console.error('Failed to set current ship:', err);
    }
  };

  // Filter ships
  const ships = fleetData?.ships || [];

  const filteredShips = useMemo(() => {
    return ships.filter((ship) => {
      // Tab filter
      if (activeTab === 'hangar' && !ship.isInHangar) return false;

      // Search filter
      if (search.trim()) {
        const q = search.toLowerCase();
        const matchName = ship.name.toLowerCase().includes(q);
        const matchMfr = ship.manufacturer.toLowerCase().includes(q) || ship.manufacturerBadge.toLowerCase().includes(q);
        const matchRole = ship.role.toLowerCase().includes(q);
        const matchAcq = ship.acquisitionType.toLowerCase().includes(q);
        if (!matchName && !matchMfr && !matchRole && !matchAcq) return false;
      }

      // Acquisition filter
      if (selectedAcquisition !== 'Alle') {
        if (!ship.acquisitionType.toLowerCase().includes(selectedAcquisition.toLowerCase())) {
          return false;
        }
      }

      // Manufacturer filter
      if (selectedManufacturer !== 'Alle') {
        if (!ship.manufacturerBadge.toUpperCase().includes(selectedManufacturer.toUpperCase()) &&
            !ship.manufacturer.toUpperCase().includes(selectedManufacturer.toUpperCase())) {
          return false;
        }
      }

      return true;
    });
  }, [ships, activeTab, search, selectedAcquisition, selectedManufacturer]);

  const hangarCount = fleetData?.hangarCount ?? ships.filter((s) => s.isInHangar).length;
  const historyCount = fleetData?.flownCount ?? ships.length;

  // Catalog filtered for add modal
  const filteredCatalog = useMemo(() => {
    const catalog = fleetData?.catalog || [];
    if (!catalogSearch.trim()) return catalog;
    const q = catalogSearch.toLowerCase();
    return catalog.filter(
      (c) =>
        c.name.toLowerCase().includes(q) ||
        c.manufacturer.toLowerCase().includes(q) ||
        c.role.toLowerCase().includes(q)
    );
  }, [fleetData?.catalog, catalogSearch]);

  const manufacturers = ['Alle', 'DRAKE', 'AEGIS', 'CRUSADER', 'ANVIL', 'RSI', 'MISC', 'ORIGIN', 'ARGO', 'MIRAI'];
  const acquisitions = ['Alle', 'Pledge Store', 'In-Game (aUEC)', 'Miete (Rental)', 'Geliehen'];

  return (
    <div className="flex flex-col min-h-full space-y-3 font-sans select-none">
      {/* ══ TOP BANNER & SEGMENTED CONTROLS ══ */}
      <div className="bg-[#050C16] border border-[#14263B] rounded-lg p-3 shadow-md flex flex-col gap-3">
        {/* Row 1: Tab switcher, Add Ship Button & KPI Pills */}
        <div className="flex flex-wrap items-center justify-between gap-3">
          {/* Left: Mein Hangar vs Flug-Historie & + Schiff */}
          <div className="flex items-center gap-2">
            {/* Segmented Switcher */}
            <div className="bg-[#030810] border border-[#122236] rounded-lg p-1 flex items-center gap-1 shadow-inner">
              <button
                onClick={() => handleTabClick('hangar')}
                className={`flex items-center gap-2 px-3 py-1.5 rounded-md text-xs font-semibold transition-all cursor-pointer ${
                  activeTab === 'hangar'
                    ? 'bg-gradient-to-r from-cyan-950/80 to-[#07243B] text-cyan-300 border border-cyan-500/40 shadow-sm'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-[#071322]'
                }`}
                title="Zeigt nur Schiffe in deinem persönlichen Hangar-Besitz"
              >
                <Warehouse className="w-3.5 h-3.5 text-cyan-400" />
                <span>Mein Hangar</span>
                <span className="px-1.5 py-0.2 rounded-full text-[10px] font-mono font-bold bg-cyan-950/80 text-cyan-400 border border-cyan-800/60">
                  {hangarCount}
                </span>
              </button>

              <button
                onClick={() => handleTabClick('history')}
                className={`flex items-center gap-2 px-3 py-1.5 rounded-md text-xs font-semibold transition-all cursor-pointer ${
                  activeTab === 'history'
                    ? 'bg-gradient-to-r from-slate-900 to-[#101D2E] text-sky-300 border border-sky-500/40 shadow-sm'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-[#071322]'
                }`}
                title="Zeigt alle jemals in den Logs geflogenen Schiffe (inkl. Free Fly, Miete, geliehen)"
              >
                <Rocket className="w-3.5 h-3.5 text-sky-400" />
                <span>Flug-Historie</span>
                <span className="px-1.5 py-0.2 rounded-full text-[10px] font-mono font-bold bg-slate-800 text-slate-300 border border-slate-700">
                  {historyCount}
                </span>
              </button>
            </div>

            <div className="w-[1px] h-6 bg-[#16283C] mx-1" />

            {/* + Schiff hinzufügen */}
            <button
              onClick={() => setIsAddModalOpen(true)}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-md bg-[#091829] hover:bg-[#0d233c] text-cyan-300 border border-cyan-800/60 hover:border-cyan-500 text-xs font-semibold transition shadow-sm cursor-pointer"
              title="Schiff aus dem Gesamtkatalog zu deinem Hangar hinzufügen"
            >
              <div className="w-4 h-4 rounded bg-cyan-500/20 border border-cyan-400/40 flex items-center justify-center text-cyan-400 font-bold text-xs">
                +
              </div>
              <span>Schiff hinzufügen</span>
            </button>

            {/* ⚖️ Schiffe vergleichen */}
            <button
              onClick={() => {
                setCompareShipA(ships[0]?.name);
                setCompareShipB(ships[1]?.name || fleetData?.catalog[0]?.name);
                setIsCompareModalOpen(true);
              }}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-md bg-[#0d1b2a] hover:bg-[#13283f] text-indigo-300 border border-indigo-800/60 hover:border-indigo-500 text-xs font-semibold transition shadow-sm cursor-pointer"
              title="Zwei Schiffe im Side-by-Side Vergleich gegenüberstellen"
            >
              <Scale className="w-3.5 h-3.5 text-indigo-400" />
              <span>Vergleichen</span>
            </button>

            {/* 📷 Screenshot Loadout scannen */}
            <button
              onClick={handleScanScreenshot}
              disabled={isScanningScreenshot}
              className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md text-xs font-semibold transition shadow-sm cursor-pointer border ${
                isScanningScreenshot
                  ? 'bg-slate-800 text-slate-400 border-slate-700'
                  : 'bg-[#0f1f2e] hover:bg-[#172f44] text-emerald-300 border-emerald-800/60 hover:border-emerald-500'
              }`}
              title="Neuesten Screenshot scannen und Schiffs-Ausrüstung (VLM / ASOP) erkennen"
            >
              <Camera className={`w-3.5 h-3.5 ${isScanningScreenshot ? 'animate-spin text-amber-400' : 'text-emerald-400'}`} />
              <span>{isScanningScreenshot ? 'Scanne...' : 'Screenshot OCR'}</span>
            </button>

            {/* 📋 Screenshot aus Zwischenablage scannen */}
            <button
              onClick={handleScanClipboard}
              disabled={isScanningScreenshot}
              className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md text-xs font-semibold transition shadow-sm cursor-pointer border ${
                isScanningScreenshot
                  ? 'bg-slate-800 text-slate-400 border-slate-700'
                  : 'bg-[#091a2e] hover:bg-[#0f2947] text-cyan-300 border-cyan-800/60 hover:border-cyan-500'
              }`}
              title="Screenshot direkt aus der Zwischenablage einlesen (oder einfach Strg+V drücken)"
            >
              <Clipboard className="w-3.5 h-3.5 text-cyan-400" />
              <span>Zwischenablage (Strg+V)</span>
            </button>

            {screenshotFeedback && (
              <span className="text-[11px] font-mono px-2 py-0.5 rounded bg-slate-900 border border-slate-700 text-cyan-300 animate-fade-in">
                {screenshotFeedback}
              </span>
            )}
          </div>

          {/* Right: Telemetry Cluster (WERT, PLEDGE, FLÜGE, QUANTUM) */}
          <div className="flex flex-wrap items-center gap-2">
            {/* WERT */}
            <div
              className="bg-[#051C12]/90 border border-[#144C2E] rounded-lg px-2.5 py-1 flex items-center gap-2 shadow-sm"
              title="Geschätzter aUEC In-Game Marktwert"
            >
              <div className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
              <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-emerald-400">
                WERT
              </span>
              <span className="text-xs font-mono font-bold text-emerald-300">
                {(fleetData?.totalFleetValueAuec ?? 0).toLocaleString()} <span className="text-[10px] font-normal text-emerald-500">aUEC</span>
              </span>
            </div>

            {/* PLEDGE */}
            <div
              className="bg-[#1C1402]/90 border border-[#684507] rounded-lg px-2.5 py-1 flex items-center gap-2 shadow-sm"
              title="Echtgeld-Pledgewert aller Pledge-Store Schiffe"
            >
              <div className="w-1.5 h-1.5 rounded-full bg-amber-400" />
              <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-amber-400">
                PLEDGE
              </span>
              <span className="text-xs font-mono font-bold text-amber-300">
                ${(fleetData?.totalFleetPledgeUsd ?? 0).toLocaleString()} <span className="text-[10px] font-normal text-amber-500">USD</span>
              </span>
            </div>

            {/* FLÜGE */}
            <div
              className="bg-[#051524]/90 border border-[#123B60] rounded-lg px-2.5 py-1 flex items-center gap-2 shadow-sm"
              title="Gesamtzahl dokumentierter Flug-Starts"
            >
              <div className="w-1.5 h-1.5 rounded-full bg-sky-400" />
              <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-sky-400">
                FLÜGE
              </span>
              <span className="text-xs font-mono font-bold text-sky-300">
                {(fleetData?.totalFlights ?? 0).toLocaleString()}
              </span>
            </div>

            {/* QT-SPRÜNGE */}
            <div
              className="bg-[#140A28]/90 border border-[#3B1C70] rounded-lg px-2.5 py-1 flex items-center gap-2 shadow-sm"
              title="Quantum-Travel Überlicht-Sprünge (QT) durch das Stanton- und Pyro-System"
            >
              <div className="w-1.5 h-1.5 rounded-full bg-purple-400" />
              <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-purple-400 flex items-center gap-1">
                <Zap className="w-3 h-3" />
                QT-SPRÜNGE
              </span>
              <span className="text-xs font-mono font-bold text-purple-300">
                {(fleetData?.totalQuantumJumps ?? 0).toLocaleString()}
              </span>
            </div>
          </div>
        </div>

        {/* Row 2: Filter-Chips (Acquisition & Manufacturer) + Search */}
        <div className="flex flex-wrap items-center justify-between gap-2 pt-1 border-t border-[#122236]">
          {/* Filter-Chips */}
          <div className="flex flex-wrap items-center gap-1.5 overflow-x-auto py-0.5">
            {/* Acquisition types */}
            {acquisitions.map((acq) => {
              const isSelected = selectedAcquisition === acq;
              const label =
                acq === 'Alle'
                  ? 'Alle Herkünfte'
                  : acq === 'Pledge Store'
                  ? '💵 Pledge'
                  : acq === 'In-Game (aUEC)'
                  ? '🪙 In-Game'
                  : acq === 'Miete (Rental)'
                  ? '🎟 Miete'
                  : '👥 Geliehen';

              return (
                <button
                  key={acq}
                  onClick={() => setSelectedAcquisition(acq)}
                  className={`px-2 py-0.5 rounded text-[11px] font-medium transition cursor-pointer ${
                    isSelected
                      ? 'bg-cyan-950/90 text-cyan-300 border border-cyan-500/60 font-semibold'
                      : 'bg-[#071322] text-slate-400 hover:text-slate-200 border border-[#14263B]'
                  }`}
                >
                  {label}
                </button>
              );
            })}

            <div className="w-[1px] h-4 bg-[#16283C] mx-1" />

            {/* Manufacturers */}
            {manufacturers.map((mfr) => {
              const isSelected = selectedManufacturer === mfr;
              return (
                <button
                  key={mfr}
                  onClick={() => setSelectedManufacturer(mfr)}
                  className={`px-2 py-0.5 rounded text-[11px] font-mono transition cursor-pointer ${
                    isSelected
                      ? 'bg-cyan-950/90 text-cyan-300 border border-cyan-500/60 font-bold'
                      : 'bg-[#071322] text-slate-400 hover:text-slate-200 border border-[#14263B]'
                  }`}
                >
                  {mfr === 'Alle' ? 'Alle Marken' : mfr}
                </button>
              );
            })}
          </div>

          {/* Search Box */}
          <div className="relative w-64 shrink-0">
            <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-500" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Schiff, Rolle, Marke..."
              className="w-full bg-[#071322] border border-[#1C3D5E]/60 rounded-md pl-8 pr-7 py-1 text-xs font-mono text-slate-200 placeholder-slate-500 focus:outline-none focus:border-cyan-500"
            />
            {search && (
              <button
                onClick={handleClearSearch}
                className="absolute right-2 top-1/2 -translate-y-1/2 text-slate-500 hover:text-slate-300 text-xs"
                title="Suche zurücksetzen"
              >
                ✕
              </button>
            )}
          </div>
        </div>
      </div>

      {/* ══ MAIN TABLE / FLEET LIST ══ */}
      <div className="flex-1 bg-[#040914]/90 rounded-lg border border-[#14263B] flex flex-col overflow-hidden shadow-sm min-h-[400px]">
        <div className="flex-1 overflow-auto">
          <table className="w-full min-w-[950px] text-left text-xs border-collapse font-sans">
            <thead>
              <tr className="border-b border-cyan-950/80 bg-[#051122]/95 text-slate-400 text-[11px] font-mono font-bold uppercase tracking-wider sticky top-0 backdrop-blur-md z-10 shadow-sm">
                <th className="py-3 px-3.5 w-32">Status &amp; Hangar</th>
                <th className="py-3 px-3.5 min-w-[220px]">Schiff &amp; Hersteller</th>
                <th className="py-3 px-3.5 w-44">Herkunft / Pledge</th>
                <th className="py-3 px-3.5 w-44">Rolle / Marktwert</th>
                <th className="py-3 px-3.5 w-36">Versicherung</th>
                <th className="py-3 px-3.5 w-48">Flug-Einsätze</th>
                <th className="py-3 px-3.5 text-right w-36 font-mono">Aktionen</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#102235]/60">
              {filteredShips.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-20 text-center">
                    <div className="max-w-md mx-auto flex flex-col items-center gap-3">
                      <div className="w-12 h-12 rounded-full bg-[#07192C] border border-cyan-800/60 flex items-center justify-center text-cyan-400">
                        <Warehouse className="w-6 h-6" />
                      </div>
                      <div className="text-sm font-bold text-slate-300">
                        {activeTab === 'hangar' ? 'Keine Schiffe in deinem Hangar gefunden' : 'Keine Schiffe erfasst'}
                      </div>
                      <p className="text-xs text-slate-500 text-center">
                        {activeTab === 'hangar'
                          ? 'Füge Schiffe aus dem Gesamtkatalog hinzu oder markiere Schiffe in der Flug-Historie mit dem Stern ★ als dein Hangar-Eigentum.'
                          : 'Starte Star Citizen oder lade bestehende Game-Logs, um deine Flug-Historie zu synchronisieren.'}
                      </p>
                      {activeTab === 'hangar' && (
                        <button
                          onClick={() => setIsAddModalOpen(true)}
                          className="mt-1 px-4 py-1.5 rounded-md bg-cyan-600 hover:bg-cyan-500 text-slate-950 font-bold text-xs flex items-center gap-1.5 transition shadow cursor-pointer"
                        >
                          <Plus className="w-3.5 h-3.5" />
                          <span>Schiff jetzt hinzufügen</span>
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ) : (
                filteredShips.map((ship) => {
                  const isEditingPledge = editingPledgeShip === ship.name;
                  const isEditingNotes = editingNotesShip === ship.name;

                  // Insurance color
                  const insColor =
                    ship.insuranceType.includes('LTI')
                      ? 'text-purple-300 border-purple-500/60 bg-purple-950/50 shadow-[0_0_8px_rgba(168,85,247,0.2)] hover:border-purple-400'
                      : ship.insuranceType.includes('120')
                      ? 'text-cyan-300 border-cyan-500/60 bg-cyan-950/50 shadow-[0_0_8px_rgba(6,182,212,0.2)] hover:border-cyan-400'
                      : ship.insuranceType.includes('24') || ship.insuranceType.includes('12')
                      ? 'text-blue-300 border-blue-500/50 bg-blue-950/40 hover:border-blue-400'
                      : 'text-slate-300 border-slate-700 bg-slate-900/50 hover:border-slate-500';

                  // Acquisition color & label
                  const acqBadge =
                    ship.acquisitionType === 'Pledge Store'
                      ? { label: `$${ship.pledgeValueUsd} USD`, color: 'text-amber-300 border-amber-500/50 bg-gradient-to-r from-amber-950/70 to-amber-900/40 shadow-[0_0_8px_rgba(245,158,11,0.15)] hover:border-amber-400' }
                      : ship.acquisitionType === 'In-Game (aUEC)'
                      ? { label: '🪙 In-Game Kauf', color: 'text-emerald-300 border-emerald-500/50 bg-gradient-to-r from-emerald-950/70 to-emerald-900/40 shadow-[0_0_8px_rgba(16,185,129,0.15)] hover:border-emerald-400' }
                      : ship.acquisitionType === 'Miete (Rental)'
                      ? { label: '🎟 Gemietet', color: 'text-sky-300 border-sky-500/50 bg-gradient-to-r from-sky-950/70 to-sky-900/40 shadow-[0_0_8px_rgba(14,165,233,0.15)] hover:border-sky-400' }
                      : { label: '👥 Geliehen', color: 'text-slate-300 border-slate-700 bg-slate-900/60 hover:border-slate-500' };

                  return (
                    <tr
                      key={ship.name}
                      className={`transition-all duration-150 group border-l-[3px] ${
                        ship.isCurrent
                          ? 'border-l-emerald-400 bg-emerald-950/20 shadow-[inset_0_0_20px_rgba(16,185,129,0.06)]'
                          : 'border-l-transparent hover:border-l-cyan-400 hover:bg-[#071a30]/80'
                      }`}
                    >
                      {/* 1. Status & Hangar */}
                      <td className="py-3 px-3.5">
                        <div className="flex items-center gap-2">
                          {/* Active / Inactive checkmark button */}
                          {ship.isCurrent ? (
                            <span
                              className="p-1.5 rounded-lg bg-emerald-950/80 border border-emerald-500/60 text-emerald-400 flex items-center justify-center shadow-[0_0_10px_rgba(16,185,129,0.35)]"
                              data-tooltip="Aktives Schiff (für HUD & Live-Log)"
                            >
                              <Check className="w-3.5 h-3.5 stroke-[2.5]" />
                            </span>
                          ) : (
                            <button
                              onClick={() => handleSetCurrentShip(ship.name)}
                              className="p-1.5 rounded-lg border border-slate-800 bg-[#061224] hover:bg-emerald-950/40 hover:border-emerald-500/60 hover:text-emerald-300 text-slate-500 transition cursor-pointer"
                              data-tooltip="Als aktives Schiff für HUD und Live-Log setzen"
                            >
                              <Check className="w-3.5 h-3.5 opacity-40 hover:opacity-100 transition-opacity" />
                            </button>
                          )}

                          {/* Star Toggle Button */}
                          <button
                            onClick={() => handleToggleHangar(ship.name)}
                            className={`p-1.5 rounded-lg transition border cursor-pointer ${
                              ship.isInHangar
                                ? 'bg-amber-950/50 border-amber-500/60 text-amber-400 hover:bg-amber-900/60 shadow-[0_0_8px_rgba(245,158,11,0.25)]'
                                : 'bg-[#061224] border-slate-800 text-slate-500 hover:text-amber-400 hover:border-amber-600/50 hover:bg-amber-950/20'
                            }`}
                            data-tooltip={
                              ship.isInHangar
                                ? 'Im persönlichen Hangar (Klicken zum Entfernen)'
                                : 'Nicht im Hangar (Klicken zum Hinzufügen)'
                            }
                          >
                            <Star className={`w-3.5 h-3.5 ${ship.isInHangar ? 'fill-amber-400' : ''}`} />
                          </button>
                        </div>
                      </td>

                      {/* 2. Schiff & Hersteller */}
                      <td className="py-3 px-3.5">
                        <div className="flex flex-col">
                          <div className="relative group/shipname inline-block">
                            <div className="flex items-center gap-1.5 cursor-pointer">
                              <span className="font-bold text-slate-100 group-hover/shipname:text-cyan-300 transition-colors text-[13.5px] tracking-wide">
                                {ship.name.split(/\s*·\s*/)[0].trim()}
                              </span>
                              {(ship.imageUrl || ship.thumbnailUrl) && (
                                <Eye className="w-3 h-3 text-cyan-500/50 group-hover/shipname:text-cyan-400 transition-colors" />
                              )}
                            </div>

                            {/* 🌌 High-Tech Ship Preview Hover Card */}
                            {(ship.imageUrl || ship.thumbnailUrl) && (
                              <div className="absolute left-0 top-full mt-2 hidden group-hover/shipname:flex flex-col z-50 w-72 p-2.5 rounded-xl bg-[#040d1a]/95 border border-cyan-500/40 shadow-[0_10px_35px_rgba(0,0,0,0.8),0_0_15px_rgba(6,182,212,0.25)] backdrop-blur-md pointer-events-none transition-all animate-in fade-in duration-150">
                                <div className="relative w-full h-36 rounded-lg overflow-hidden bg-[#02060f] border border-cyan-900/60 flex items-center justify-center">
                                  <img
                                    src={ship.imageUrl || ship.thumbnailUrl || ''}
                                    alt={ship.name}
                                    className="w-full h-full object-cover object-center relative z-1"
                                    loading="lazy"
                                    onError={(e) => {
                                      (e.target as HTMLElement).style.display = 'none';
                                    }}
                                  />
                                  <div className="absolute inset-0 flex flex-col items-center justify-center text-slate-500 p-2 text-center bg-[#030914]">
                                    <Rocket className="w-7 h-7 text-cyan-500/40 mb-1" />
                                    <span className="text-[10px] font-mono text-cyan-300/70 truncate max-w-[90%]">{ship.name}</span>
                                  </div>
                                  <div className="absolute inset-0 bg-gradient-to-t from-[#040d1a] via-transparent to-transparent opacity-80" />
                                  <div className="absolute bottom-2 left-2 right-2 flex items-center justify-between">
                                    <span className="text-[10px] font-mono font-bold px-1.5 py-0.5 rounded bg-black/75 text-cyan-300 border border-cyan-500/30 backdrop-blur-sm">
                                      {ship.manufacturerBadge || 'SHIP'}
                                    </span>
                                    <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-black/75 text-slate-300 border border-slate-700 backdrop-blur-sm">
                                      {ship.role}
                                    </span>
                                  </div>
                                </div>
                                <div className="mt-2 flex items-center justify-between text-[11px]">
                                  <span className="font-bold text-slate-100 truncate">{ship.name}</span>
                                  <span className="text-[10px] font-mono text-cyan-400">
                                    {ship.flightCount} Einsätze
                                  </span>
                                </div>
                              </div>
                            )}
                          </div>
                          <div className="flex items-center gap-2 mt-0.5">
                            <span
                              className="px-1.5 py-0.2 rounded text-[9px] font-mono font-bold border shadow-xs"
                              style={{
                                borderColor: ship.manufacturerColor ? `${ship.manufacturerColor}80` : '#0284c7',
                                color: ship.manufacturerColor || '#38BDF8',
                                backgroundColor: '#051224',
                              }}
                            >
                              {ship.manufacturerBadge || 'SHIP'}
                            </span>
                            <span className="text-[11.5px] text-slate-400 truncate max-w-[200px] font-sans">
                              {(() => {
                                const b = (ship.manufacturerBadge || '').toUpperCase();
                                const m = ship.manufacturer || '';
                                if (b === 'RSI' || m === 'RSI') return 'Roberts Space Industries';
                                if (b === 'MISC' || m === 'MISC') return 'Musashi Industrial & Starflight Concern';
                                if (m.length > b.length) return m;
                                return m || b;
                              })()}
                            </span>
                          </div>

                          {/* Livery / Lackierung & Loadout Button */}
                          <div className="flex flex-wrap items-center gap-1.5 mt-1.5">
                            {ship.livery && (
                              <span
                                className="px-2 py-0.5 rounded text-[9.5px] font-mono border border-purple-500/40 bg-purple-950/40 text-purple-300 flex items-center gap-1 shadow-xs"
                                data-tooltip={`Lackierung: ${ship.livery}`}
                              >
                                <Palette className="w-2.5 h-2.5 text-purple-400" />
                                <span className="truncate max-w-[130px]">{ship.livery}</span>
                              </span>
                            )}

                            {Array.isArray(ship.components) && ship.components.length > 0 && (
                              <button
                                onClick={() => {
                                  setSelectedLoadoutShip(ship);
                                  setIsLoadoutModalOpen(true);
                                }}
                                className="px-2 py-0.5 rounded text-[9.5px] font-mono border border-cyan-800/60 bg-cyan-950/40 hover:bg-cyan-900/60 text-cyan-300 flex items-center gap-1.5 transition cursor-pointer shadow-xs hover:border-cyan-500 hover:shadow-[0_0_8px_rgba(6,182,212,0.25)]"
                                data-tooltip="Komponenten, Ausrüstung & VLM-Loadout im Detail ansehen"
                              >
                                <Wrench className="w-2.5 h-2.5 text-cyan-400" />
                                <span>{ship.components.length} Ausrüstung</span>
                                {ship.componentsUpdatedAt && (
                                  <span className="text-emerald-400 font-bold" data-tooltip="Erfolgreich via OCR/VLM erfasst">
                                    ✓
                                  </span>
                                )}
                              </button>
                            )}
                          </div>

                          {/* Custom Notes inline hint */}
                          {ship.customNotes && !isEditingNotes && (
                            <span
                              onClick={() => {
                                setEditingNotesShip(ship.name);
                                setNotesInputVal(ship.customNotes);
                              }}
                              className="text-[10.5px] text-slate-400 italic mt-1 hover:text-cyan-300 cursor-pointer truncate max-w-[220px] transition-colors"
                              data-tooltip={ship.customNotes}
                            >
                              📝 {ship.customNotes}
                            </span>
                          )}
                        </div>
                      </td>

                      {/* 3. Herkunft / Pledge */}
                      <td className="py-3 px-3.5">
                        <div className="flex items-center gap-2">
                          {isEditingPledge ? (
                            <div className="flex items-center gap-1 font-mono">
                              <span className="text-amber-400 font-bold">$</span>
                              <input
                                type="number"
                                value={pledgeInputVal}
                                onChange={(e) => setPledgeInputVal(e.target.value)}
                                className="w-16 bg-[#071322] border border-amber-500/60 rounded px-1.5 py-0.5 text-xs text-amber-200 focus:outline-none"
                                autoFocus
                              />
                              <button
                                onClick={() => handleSavePledge(ship.name)}
                                className="p-1 rounded bg-emerald-950 border border-emerald-600 text-emerald-400 hover:bg-emerald-900 cursor-pointer"
                                data-tooltip="Speichern"
                              >
                                <Check className="w-3 h-3" />
                              </button>
                              <button
                                onClick={() => setEditingPledgeShip(null)}
                                className="p-1 rounded bg-slate-800 border border-slate-700 text-slate-400 hover:bg-slate-700 cursor-pointer"
                                data-tooltip="Abbrechen"
                              >
                                <X className="w-3 h-3" />
                              </button>
                            </div>
                          ) : (
                            <div className="relative inline-flex items-center gap-1.5">
                              <button
                                onClick={(e) => {
                                  e.stopPropagation();
                                  setOpenAcqMenuShip(openAcqMenuShip === ship.name ? null : ship.name);
                                }}
                                className={`px-2.5 py-1 rounded-md text-[11px] font-mono font-semibold border transition cursor-pointer flex items-center gap-1.5 ${acqBadge.color}`}
                                data-tooltip="Klicken, um Herkunft zu ändern (Echtgeld / In-Game / Miete)"
                              >
                                <span>{acqBadge.label}</span>
                                <ChevronDown className="w-2.5 h-2.5 opacity-60" />
                              </button>

                              {/* Dropdown Menu for Herkunft */}
                              {openAcqMenuShip === ship.name && (
                                <div
                                  onClick={(e) => e.stopPropagation()}
                                  className="absolute left-0 top-full mt-1.5 z-50 w-56 py-1 rounded-xl bg-[#040e1d] border border-cyan-500/40 shadow-[0_10px_25px_rgba(0,0,0,0.85)] backdrop-blur-md animate-in fade-in zoom-in-95 duration-150"
                                >
                                  <div className="px-3 py-1.5 text-[10px] font-mono text-cyan-400 font-bold border-b border-slate-800/80 uppercase tracking-wider flex items-center justify-between">
                                    <span>Herkunft wählen</span>
                                    {ship.isInHangar && (
                                      <span className="text-[9px] text-amber-400 flex items-center gap-0.5">
                                        <Star className="w-2.5 h-2.5 fill-amber-400" /> Im Hangar
                                      </span>
                                    )}
                                  </div>
                                  {[
                                    {
                                      id: 'Pledge Store',
                                      label: '💵 Pledge Store (Echtgeld)',
                                      badge: `$${ship.pledgeValueUsd} USD`,
                                      desc: 'Im RSI Store gekauft',
                                      activeClass: 'text-amber-300 bg-amber-950/40',
                                    },
                                    {
                                      id: 'In-Game (aUEC)',
                                      label: '🪙 In-Game Kauf',
                                      badge: 'aUEC',
                                      desc: 'Mit Ingame-Credits gekauft',
                                      activeClass: 'text-emerald-300 bg-emerald-950/40',
                                    },
                                    {
                                      id: 'Miete (Rental)',
                                      label: '🎟 Gemietet',
                                      badge: 'Rental',
                                      desc: 'Temporär gemietet',
                                      activeClass: 'text-sky-300 bg-sky-950/40',
                                    },
                                    {
                                      id: 'Geliehen / Free Fly',
                                      label: '👥 Geliehen',
                                      badge: 'Free Fly',
                                      desc: 'Org / Event / Gast',
                                      activeClass: 'text-slate-300 bg-slate-800/40',
                                    },
                                  ].map((opt) => (
                                    <button
                                      key={opt.id}
                                      onClick={async (e) => {
                                        e.stopPropagation();
                                        setOpenAcqMenuShip(null);
                                        await handleSetAcquisition(ship.name, opt.id);
                                      }}
                                      className={`w-full px-3 py-2 text-left flex flex-col hover:bg-cyan-950/50 transition cursor-pointer border-b border-slate-900/50 last:border-b-0 ${
                                        ship.acquisitionType === opt.id ? opt.activeClass : 'text-slate-300'
                                      }`}
                                    >
                                      <span className="text-xs font-semibold flex items-center justify-between">
                                        <span>{opt.label}</span>
                                        {ship.acquisitionType === opt.id ? (
                                          <Check className="w-3.5 h-3.5 text-cyan-400" />
                                        ) : (
                                          <span className="text-[10px] font-mono text-slate-500">{opt.badge}</span>
                                        )}
                                      </span>
                                      <span className="text-[10px] text-slate-400">{opt.desc}</span>
                                    </button>
                                  ))}
                                </div>
                              )}

                              {ship.isPledgeBought && (
                                <button
                                  onClick={() => handleStartEditPledge(ship)}
                                  className="p-1.5 rounded-md bg-[#061224] border border-slate-800 text-slate-400 hover:text-amber-300 hover:border-amber-500/50 cursor-pointer transition opacity-60 group-hover:opacity-100"
                                  data-tooltip="Pledgewert ($) manuell anpassen"
                                >
                                  <Edit2 className="w-3 h-3" />
                                </button>
                              )}
                            </div>
                          )}
                        </div>
                      </td>

                      {/* 4. Rolle / Typ */}
                      <td className="py-3 px-3.5">
                        <span className="text-cyan-300 font-medium text-xs truncate block max-w-[180px]" data-tooltip={ship.role}>
                          {ship.role}
                        </span>
                        <span className="text-[10.5px] font-mono text-slate-400 flex items-center gap-1 mt-0.5">
                          {ship.estimatedValueAuec > 0
                            ? `~${(ship.estimatedValueAuec / 1000000).toFixed(1)}M aUEC`
                            : 'Kein UEX Preis'}
                        </span>
                      </td>

                      {/* 5. Versicherung */}
                      <td className="py-3 px-3.5">
                        <button
                          onClick={() => handleCycleInsurance(ship.name)}
                          className={`px-2.5 py-1 rounded-md text-[10.5px] font-mono font-semibold border transition cursor-pointer ${insColor}`}
                          data-tooltip="Klicken zum Durchschalten: LTI ➔ 120M (IAE) ➔ 24M ➔ 12M ➔ 6M"
                        >
                          {ship.insuranceType || 'LTI (Lifetime)'}
                        </button>
                      </td>

                      {/* 6. Flug-Einsätze */}
                      <td className="py-3 px-3.5 font-mono">
                        <div className="flex flex-col gap-1">
                          <div className="flex items-center gap-1.5">
                            <span
                              className="px-2 py-0.5 rounded bg-sky-950/60 border border-sky-800/50 text-sky-300 text-[11px] font-bold flex items-center gap-1 shadow-xs"
                              data-tooltip="Dokumentierte Flugeinsätze"
                            >
                              <Rocket className="w-3 h-3 text-sky-400" />
                              <span>{ship.flightCount} Flüge</span>
                            </span>
                            <span
                              className="px-2 py-0.5 rounded bg-purple-950/60 border border-purple-800/50 text-purple-300 text-[11px] font-bold flex items-center gap-1 shadow-xs"
                              data-tooltip="Quantum-Travel Überlicht-Sprünge (QT)"
                            >
                              <Zap className="w-3 h-3 text-purple-400" />
                              <span>{ship.quantumJumps} Sprünge</span>
                            </span>
                          </div>
                          <span className="text-[10.5px] text-slate-400 font-sans truncate" data-tooltip="Zuletzt geflogen">
                            {ship.lastFlown !== '—' ? `Zuletzt: ${ship.lastFlown}` : 'Noch nicht geflogen'}
                          </span>
                        </div>
                      </td>

                      {/* 7. Aktionen */}
                      <td className="py-3 px-3.5 text-right">
                        <div className="flex items-center justify-end gap-1.5">
                          {/* Ausrüstung & Komponenten */}
                          <button
                            onClick={() => {
                              setSelectedLoadoutShip(ship);
                              setIsLoadoutModalOpen(true);
                            }}
                            className="p-1.5 rounded-lg bg-[#061224] hover:bg-cyan-950/70 border border-[#14263B] hover:border-cyan-500 text-cyan-400 hover:text-cyan-200 hover:shadow-[0_0_10px_rgba(6,182,212,0.3)] transition cursor-pointer"
                            data-tooltip="Ausrüstung, Komponenten & VLM-Loadout ansehen"
                          >
                            <Wrench className="w-3.5 h-3.5" />
                          </button>

                          {/* Vergleichen */}
                          <button
                            onClick={() => {
                              setCompareShipA(ship.name);
                              setCompareShipB(ships.find((s) => s.name !== ship.name)?.name || fleetData?.catalog[0]?.name);
                              setIsCompareModalOpen(true);
                            }}
                            className="p-1.5 rounded-lg bg-[#061224] hover:bg-indigo-950/70 border border-[#14263B] hover:border-indigo-500 text-indigo-400 hover:text-indigo-200 hover:shadow-[0_0_10px_rgba(99,102,241,0.3)] transition cursor-pointer"
                            data-tooltip="Schiff im Side-by-Side Vergleich gegenüberstellen"
                          >
                            <Scale className="w-3.5 h-3.5" />
                          </button>

                          {/* Wiki */}
                          <button
                            onClick={() => {
                              window.dispatchEvent(new CustomEvent('open-wiki-dossier', { detail: ship.name }));
                            }}
                            className="p-1.5 rounded-lg bg-[#061224] hover:bg-sky-950/70 border border-[#14263B] hover:border-sky-500 text-sky-400 hover:text-sky-200 hover:shadow-[0_0_10px_rgba(14,165,233,0.3)] transition cursor-pointer"
                            data-tooltip="Star Citizen Wiki Dossier öffnen"
                          >
                            <ExternalLink className="w-3.5 h-3.5" />
                          </button>

                          {/* UEX */}
                          <button
                            onClick={() => {
                              bridge.openExternalUrl(`https://uexcorp.space/search?q=${encodeURIComponent(ship.name)}`);
                            }}
                            className="p-1.5 rounded-lg bg-[#061224] hover:bg-amber-950/70 border border-[#14263B] hover:border-amber-500 text-amber-400 hover:text-amber-200 hover:shadow-[0_0_10px_rgba(245,158,11,0.3)] transition cursor-pointer"
                            data-tooltip="UEX Marktpreise & Händlerstandorte abrufen"
                          >
                            <Coins className="w-3.5 h-3.5" />
                          </button>

                          {/* Notes */}
                          <button
                            onClick={() => {
                              setEditingNotesShip(ship.name);
                              setNotesInputVal(ship.customNotes || '');
                            }}
                            className="p-1.5 rounded-lg bg-[#061224] hover:bg-slate-800 border border-[#14263B] hover:border-slate-500 text-slate-400 hover:text-white transition cursor-pointer"
                            data-tooltip="Persönliche Notiz bearbeiten"
                          >
                            <Edit2 className="w-3.5 h-3.5" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* ══ MODAL: SCHIFF AUS KATALOG HINZUFÜGEN ══ */}
      {isAddModalOpen && (
        <div className="fixed inset-0 bg-black/75 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-[#071322] border border-cyan-800/80 rounded-xl shadow-2xl w-full max-w-xl flex flex-col max-h-[85vh] overflow-hidden animate-in fade-in zoom-in-95 duration-150">
            {/* Modal Header */}
            <div className="p-4 border-b border-[#14263B] bg-[#091B30] flex items-center justify-between">
              <div className="flex items-center gap-2 text-sm font-bold text-cyan-300">
                <Warehouse className="w-4 h-4 text-cyan-400" />
                <span>Schiff zu persönlichem Hangar hinzufügen</span>
              </div>
              <button
                onClick={() => setIsAddModalOpen(false)}
                className="text-slate-400 hover:text-slate-200 p-1 rounded hover:bg-slate-800"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            {/* Modal Search */}
            <div className="p-3 border-b border-[#14263B] bg-[#061224]">
              <div className="relative">
                <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-500" />
                <input
                  type="text"
                  value={catalogSearch}
                  onChange={(e) => setCatalogSearch(e.target.value)}
                  placeholder="Katalog nach Schiff, Hersteller oder Rolle durchsuchen..."
                  className="w-full bg-[#071322] border border-[#1C3D5E] rounded-md pl-9 pr-3 py-1.5 text-xs font-mono text-slate-200 placeholder-slate-500 focus:outline-none focus:border-cyan-500"
                  autoFocus
                />
              </div>
            </div>

            {/* Modal Ship List */}
            <div className="flex-1 overflow-y-auto p-2 divide-y divide-[#102235]">
              {filteredCatalog.length === 0 ? (
                <div className="py-12 text-center text-xs font-mono text-slate-500">
                  Kein Schiff mit &quot;{catalogSearch}&quot; im Gesamtkatalog gefunden.
                </div>
              ) : (
                filteredCatalog.map((catShip) => (
                  <div
                    key={catShip.name}
                    className="p-2.5 hover:bg-[#091D34] rounded-lg transition flex items-center justify-between gap-3 group"
                  >
                    <div className="flex flex-col">
                      <span className="font-bold text-slate-200 group-hover:text-cyan-300 text-xs">
                        {catShip.name}
                      </span>
                      <div className="flex items-center gap-2 mt-0.5 text-[11px] text-slate-400">
                        <span className="text-cyan-400 font-mono">{catShip.manufacturer}</span>
                        <span>·</span>
                        <span>{catShip.role}</span>
                      </div>
                      <div className="flex items-center gap-2 mt-1 text-[10.5px] font-mono">
                        {catShip.valueAuec > 0 && (
                          <span className="text-emerald-400">
                            ~{(catShip.valueAuec / 1000000).toFixed(2)}M aUEC
                          </span>
                        )}
                        {catShip.pledgeUsd > 0 && (
                          <span className="text-amber-400">
                            ${catShip.pledgeUsd} USD
                          </span>
                        )}
                      </div>
                    </div>

                    <button
                      onClick={() => handleAddCatalogShip(catShip.name)}
                      className="px-3 py-1.5 rounded-md bg-cyan-600 hover:bg-cyan-500 text-slate-950 font-bold text-xs flex items-center gap-1.5 transition shadow cursor-pointer shrink-0"
                    >
                      <Plus className="w-3.5 h-3.5" />
                      <span>In Hangar</span>
                    </button>
                  </div>
                ))
              )}
            </div>

            {/* Modal Footer */}
            <div className="p-3 border-t border-[#14263B] bg-[#091B30] flex justify-between items-center text-xs text-slate-400">
              <span>{filteredCatalog.length} Schiffe verfügbar</span>
              <button
                onClick={() => setIsAddModalOpen(false)}
                className="px-3 py-1 rounded bg-[#091829] border border-slate-700 hover:border-slate-500 text-slate-300 text-xs font-semibold cursor-pointer"
              >
                Schließen
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ══ MODAL / POPUP: NOTIZ BEARBEITEN ══ */}
      {editingNotesShip && (
        <div className="fixed inset-0 bg-black/75 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-[#071322] border border-cyan-800/80 rounded-xl shadow-2xl w-full max-w-md p-4 flex flex-col gap-3">
            <div className="flex items-center justify-between">
              <span className="text-xs font-bold font-mono text-cyan-300">
                Notiz für {editingNotesShip}
              </span>
              <button onClick={() => setEditingNotesShip(null)} className="text-slate-400 hover:text-slate-200">
                <X className="w-4 h-4" />
              </button>
            </div>
            <textarea
              value={notesInputVal}
              onChange={(e) => setNotesInputVal(e.target.value)}
              placeholder="Z. B. Loadout-Details, Waffenfitting, Versicherungshinweise, Kaufdatum..."
              className="w-full bg-[#061224] border border-[#1C3D5E] rounded-md p-2 text-xs font-sans text-slate-200 placeholder-slate-500 focus:outline-none focus:border-cyan-500 h-24 resize-none"
              autoFocus
            />
            <div className="flex justify-end gap-2">
              <button
                onClick={() => setEditingNotesShip(null)}
                className="px-3 py-1 rounded bg-[#091829] border border-slate-700 text-slate-300 text-xs"
              >
                Abbrechen
              </button>
              <button
                onClick={() => handleSaveNotes(editingNotesShip)}
                className="px-3 py-1 rounded bg-cyan-600 hover:bg-cyan-500 text-slate-950 font-bold text-xs"
              >
                Speichern
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ══ MODAL: SCHIFFS-VERGLEICH ══ */}
      {isCompareModalOpen && (
        <ShipCompareModal
          isOpen={isCompareModalOpen}
          onClose={() => setIsCompareModalOpen(false)}
          initialShipA={compareShipA}
          initialShipB={compareShipB}
          fleetShips={ships}
          catalog={fleetData?.catalog || []}
          onOpenWikiDossier={(sName) => {
            window.dispatchEvent(new CustomEvent('open-wiki-dossier', { detail: sName }));
          }}
        />
      )}

      {/* ══ MODAL: SCHIFFS-AUSRÜSTUNG & LOADOUT ══ */}
      {isLoadoutModalOpen && (
        <ShipLoadoutModal
          isOpen={isLoadoutModalOpen}
          onClose={() => setIsLoadoutModalOpen(false)}
          ship={selectedLoadoutShip}
          onRefreshFleet={fetchFleet}
        />
      )}
    </div>
  );
};
