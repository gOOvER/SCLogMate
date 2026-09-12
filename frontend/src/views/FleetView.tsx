import React, { useEffect, useState, useMemo } from 'react';
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
} from 'lucide-react';

export const FleetView: React.FC = () => {
  const [fleetData, setFleetData] = useState<FleetResponseDto | null>(null);
  const [activeTab, setActiveTab] = useState<'hangar' | 'history'>('hangar');
  const [search, setSearch] = useState<string>('');
  const [selectedAcquisition, setSelectedAcquisition] = useState<string>('Alle');
  const [selectedManufacturer, setSelectedManufacturer] = useState<string>('Alle');

  // Modal for "+ Schiff hinzufügen"
  const [isAddModalOpen, setIsAddModalOpen] = useState<boolean>(false);
  const [catalogSearch, setCatalogSearch] = useState<string>('');

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

    return () => unsub();
  }, []);

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

  const handleCycleAcquisition = async (shipName: string) => {
    try {
      const res = await bridge.sendRequest<FleetResponseDto>('cycle_ship_acquisition', { shipName });
      setFleetData(res);
    } catch (err) {
      console.error('Failed to cycle acquisition:', err);
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
                onClick={() => setActiveTab('hangar')}
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
                onClick={() => setActiveTab('history')}
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

            {/* QUANTUM */}
            <div
              className="bg-[#140A28]/90 border border-[#3B1C70] rounded-lg px-2.5 py-1 flex items-center gap-2 shadow-sm"
              title="Erfolgreich durchgeführte Quantum Travel Sprünge"
            >
              <div className="w-1.5 h-1.5 rounded-full bg-purple-400" />
              <span className="text-[10px] font-mono font-bold uppercase tracking-wider text-purple-400">
                QUANTUM
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
                onClick={() => setSearch('')}
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
              <tr className="border-b border-[#14263B] bg-[#061224] text-slate-400 text-[10.5px] font-mono font-bold uppercase tracking-wider sticky top-0 backdrop-blur-md z-10">
                <th className="py-2.5 px-3 w-36">Status &amp; Hangar</th>
                <th className="py-2.5 px-3 min-w-[220px]">Schiff &amp; Hersteller</th>
                <th className="py-2.5 px-3 w-48">Herkunft / Pledge</th>
                <th className="py-2.5 px-3 w-44">Rolle / Typ</th>
                <th className="py-2.5 px-3 w-36">Versicherung</th>
                <th className="py-2.5 px-3 w-36">Flug-Einsätze</th>
                <th className="py-2.5 px-3 text-right w-28 font-mono">Aktionen</th>
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
                          className="mt-1 px-4 py-1.5 rounded-md bg-cyan-600 hover:bg-cyan-500 text-slate-950 font-bold text-xs flex items-center gap-1.5 transition shadow"
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
                      ? 'text-purple-400 border-purple-500/50 bg-purple-950/40'
                      : ship.insuranceType.includes('120')
                      ? 'text-cyan-400 border-cyan-500/50 bg-cyan-950/40'
                      : ship.insuranceType.includes('24') || ship.insuranceType.includes('12')
                      ? 'text-blue-400 border-blue-500/50 bg-blue-950/40'
                      : 'text-slate-400 border-slate-700 bg-slate-900/40';

                  // Acquisition color & label
                  const acqBadge =
                    ship.acquisitionType === 'Pledge Store'
                      ? { label: `$${ship.pledgeValueUsd} USD`, color: 'text-amber-400 border-amber-600/50 bg-amber-950/40' }
                      : ship.acquisitionType === 'In-Game (aUEC)'
                      ? { label: '🪙 In-Game Kauf', color: 'text-emerald-400 border-emerald-600/50 bg-emerald-950/40' }
                      : ship.acquisitionType === 'Miete (Rental)'
                      ? { label: '🎟 Gemietet', color: 'text-sky-400 border-sky-600/50 bg-sky-950/40' }
                      : { label: '👥 Geliehen', color: 'text-slate-400 border-slate-700 bg-slate-900/40' };

                  return (
                    <tr
                      key={ship.name}
                      className={`hover:bg-[#07172A]/70 transition-colors group ${
                        ship.isCurrent ? 'bg-cyan-950/20' : ''
                      }`}
                    >
                      {/* 1. Status & Hangar */}
                      <td className="py-2.5 px-3">
                        <div className="flex items-center gap-2">
                          {/* Active / Idle pill */}
                          {ship.isCurrent ? (
                            <span className="px-2 py-0.5 rounded-full text-[10px] font-mono font-bold bg-emerald-950/80 border border-emerald-500/60 text-emerald-300 flex items-center gap-1 shadow-sm">
                              <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
                              AKTIV
                            </span>
                          ) : (
                            <button
                              onClick={() => handleSetCurrentShip(ship.name)}
                              className="opacity-40 group-hover:opacity-100 hover:text-cyan-300 text-slate-500 transition px-1.5 py-0.5 rounded text-[9.5px] font-mono border border-transparent hover:border-cyan-800/60 hover:bg-cyan-950/40 cursor-pointer"
                              title="Dieses Schiff als aktives Schiff für HUD und Log erfassen"
                            >
                              Als aktiv
                            </button>
                          )}

                          {/* Star Toggle Button */}
                          <button
                            onClick={() => handleToggleHangar(ship.name)}
                            className={`p-1 rounded transition border cursor-pointer ${
                              ship.isInHangar
                                ? 'bg-amber-950/40 border-amber-600/50 text-amber-400 hover:bg-amber-900/50'
                                : 'bg-[#091522] border-slate-700/60 text-slate-500 hover:text-amber-400 hover:border-amber-600/40'
                            }`}
                            title={
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
                      <td className="py-2.5 px-3">
                        <div className="flex flex-col">
                          <span className="font-bold text-slate-100 group-hover:text-cyan-300 transition text-xs">
                            {ship.name}
                          </span>
                          <div className="flex items-center gap-1.5 mt-0.5">
                            <span
                              className="px-1.5 py-0.2 rounded text-[9px] font-mono font-bold border"
                              style={{
                                borderColor: ship.manufacturerColor || '#38BDF8',
                                color: ship.manufacturerColor || '#38BDF8',
                                backgroundColor: '#071322',
                              }}
                            >
                              {ship.manufacturerBadge || 'SHIP'}
                            </span>
                            <span className="text-[11px] text-slate-400 truncate max-w-[150px]">
                              {ship.manufacturer}
                            </span>
                          </div>

                          {/* Custom Notes inline hint */}
                          {ship.customNotes && !isEditingNotes && (
                            <span
                              onClick={() => {
                                setEditingNotesShip(ship.name);
                                setNotesInputVal(ship.customNotes);
                              }}
                              className="text-[10px] text-slate-500 italic mt-0.5 hover:text-cyan-400 cursor-pointer truncate max-w-[220px]"
                              title={ship.customNotes}
                            >
                              📝 {ship.customNotes}
                            </span>
                          )}
                        </div>
                      </td>

                      {/* 3. Herkunft / Pledge */}
                      <td className="py-2.5 px-3">
                        <div className="flex items-center gap-1.5">
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
                                title="Speichern"
                              >
                                <Check className="w-3 h-3" />
                              </button>
                              <button
                                onClick={() => setEditingPledgeShip(null)}
                                className="p-1 rounded bg-slate-800 border border-slate-700 text-slate-400 hover:bg-slate-700 cursor-pointer"
                                title="Abbrechen"
                              >
                                <X className="w-3 h-3" />
                              </button>
                            </div>
                          ) : (
                            <>
                              <button
                                onClick={() => handleCycleAcquisition(ship.name)}
                                className={`px-2 py-0.5 rounded text-[11px] font-mono font-semibold border transition cursor-pointer ${acqBadge.color}`}
                                title="Klicken zum Durchschalten: 💵 Pledge Store ➔ 🪙 In-Game Kauf ➔ 🎟 Miete ➔ 👥 Geliehen"
                              >
                                {acqBadge.label}
                              </button>

                              {ship.isPledgeBought && (
                                <button
                                  onClick={() => handleStartEditPledge(ship)}
                                  className="p-1 rounded bg-[#091522] border border-slate-700/60 text-slate-400 hover:text-amber-400 hover:border-amber-600/40 cursor-pointer transition opacity-70 hover:opacity-100"
                                  title="Pledgewert ($) anpassen"
                                >
                                  <Edit2 className="w-2.5 h-2.5" />
                                </button>
                              )}
                            </>
                          )}
                        </div>
                      </td>

                      {/* 4. Rolle / Typ */}
                      <td className="py-2.5 px-3">
                        <span className="text-cyan-400/90 font-medium text-xs truncate block max-w-[170px]" title={ship.role}>
                          {ship.role}
                        </span>
                        <span className="text-[10px] font-mono text-slate-500">
                          {ship.estimatedValueAuec > 0
                            ? `~${(ship.estimatedValueAuec / 1000000).toFixed(1)}M aUEC`
                            : 'Kein UEX Preis'}
                        </span>
                      </td>

                      {/* 5. Versicherung */}
                      <td className="py-2.5 px-3">
                        <button
                          onClick={() => handleCycleInsurance(ship.name)}
                          className={`px-2 py-0.5 rounded text-[10.5px] font-mono font-semibold border transition cursor-pointer ${insColor}`}
                          title="Klicken zum Durchschalten: LTI ➔ 120M (IAE) ➔ 24M ➔ 12M ➔ 6M"
                        >
                          {ship.insuranceType || 'LTI (Lifetime)'}
                        </button>
                      </td>

                      {/* 6. Flug-Einsätze */}
                      <td className="py-2.5 px-3 font-mono">
                        <div className="flex flex-col">
                          <span className="font-bold text-slate-200 text-xs">
                            {ship.flightCount}× <span className="text-slate-500 font-normal">Flüge</span>
                            <span className="mx-1 text-slate-600">·</span>
                            <span className="text-sky-400">{ship.quantumJumps}</span> <span className="text-slate-500 font-normal">QT</span>
                          </span>
                          <span className="text-[10px] text-slate-500 mt-0.5">
                            {ship.lastFlown !== '—' ? ship.lastFlown : 'Noch nicht geflogen'}
                          </span>
                        </div>
                      </td>

                      {/* 7. Aktionen */}
                      <td className="py-2.5 px-3 text-right">
                        <div className="flex items-center justify-end gap-1.5">
                          {/* Wiki */}
                          <button
                            onClick={() => {
                              window.dispatchEvent(new CustomEvent('open-wiki-dossier', { detail: ship.name }));
                            }}
                            className="p-1.5 rounded bg-[#071322] hover:bg-cyan-950/60 border border-[#14263B] hover:border-cyan-700 text-cyan-400 transition cursor-pointer"
                            title="Star Citizen Wiki Dossier öffnen"
                          >
                            <ExternalLink className="w-3 h-3" />
                          </button>

                          {/* UEX */}
                          <a
                            href={`https://uexcorp.space/ships/?search=${encodeURIComponent(ship.name)}`}
                            target="_blank"
                            rel="noreferrer"
                            className="p-1.5 rounded bg-[#071322] hover:bg-amber-950/60 border border-[#14263B] hover:border-amber-700 text-amber-400 transition cursor-pointer"
                            title="UEX Händlerpreise & Standorte prüfen"
                          >
                            <Coins className="w-3 h-3" />
                          </a>

                          {/* Notes */}
                          <button
                            onClick={() => {
                              setEditingNotesShip(ship.name);
                              setNotesInputVal(ship.customNotes || '');
                            }}
                            className="p-1.5 rounded bg-[#071322] hover:bg-slate-800 border border-[#14263B] text-slate-400 hover:text-slate-200 transition cursor-pointer"
                            title="Notiz / Schiffsbeschreibung bearbeiten"
                          >
                            <Edit2 className="w-3 h-3" />
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
    </div>
  );
};
