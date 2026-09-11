import React, { useEffect, useState } from 'react';
import {
  bridge,
  WarehouseItemDto,
  WarehouseLocationDto,
} from '../services/photinoBridge';
import {
  Box,
  Download,
  MapPin,
  Minus,
  MoreVertical,
  Package,
  Plus,
  RefreshCw,
  Search,
  Trash2,
  Wrench,
  CheckCircle2,
} from 'lucide-react';

export const WarehouseView: React.FC = () => {
  const [locations, setLocations] = useState<WarehouseLocationDto[]>([]);
  const [items, setItems] = useState<WarehouseItemDto[]>([]);
  const [selectedLocation, setSelectedLocation] = useState<string>('all');
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [systemFilter, setSystemFilter] = useState<'all' | 'Stanton' | 'Pyro'>('all');
  const [search, setSearch] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(false);
  const [actionMenuOpenId, setActionMenuOpenId] = useState<string | null>(null);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  const fetchWarehouse = async () => {
    try {
      setLoading(true);
      const res = await bridge.sendRequest<{
        locations: WarehouseLocationDto[];
        items: WarehouseItemDto[];
      }>('get_warehouse', {
        location: selectedLocation === 'all' ? null : selectedLocation,
        category: selectedCategory === 'all' ? null : selectedCategory,
        search: search || null,
      });
      setLocations(res.locations);
      setItems(res.items);
    } catch (err) {
      console.error('Failed to load warehouse data:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchWarehouse();

    const unbind = bridge.on('WAREHOUSE_UPDATED', (data) => {
      if (data?.locations) setLocations(data.locations);
      if (data?.items) setItems(data.items);
    });

    return () => unbind();
  }, [selectedLocation, selectedCategory]);

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 3000);
  };

  const handleAdjustQty = async (item: WarehouseItemDto, delta: number) => {
    try {
      const res = await bridge.sendRequest<{
        locations: WarehouseLocationDto[];
        items: WarehouseItemDto[];
      }>('adjust_warehouse_qty', {
        location: item.location,
        itemClass: item.itemClass,
        delta,
      });
      if (res?.items) setItems(res.items);
      if (res?.locations) setLocations(res.locations);
      showToast(`${item.itemName}: ${delta > 0 ? '+1' : '-1'} verbucht`);
    } catch (err) {
      console.error('Failed to adjust quantity:', err);
    }
  };

  const handleDeleteItem = async (item: WarehouseItemDto) => {
    if (!confirm(`Soll der Gegenstand "${item.itemName}" wirklich aus dem Lager entfernt werden?`)) {
      return;
    }
    try {
      const res = await bridge.sendRequest<{
        locations: WarehouseLocationDto[];
        items: WarehouseItemDto[];
      }>('delete_warehouse_item', {
        location: item.location,
        itemClass: item.itemClass,
      });
      if (res?.items) setItems(res.items);
      if (res?.locations) setLocations(res.locations);
      showToast(`"${item.itemName}" gelöscht`);
    } catch (err) {
      console.error('Failed to delete item:', err);
    }
  };

  const handleClearLocation = async () => {
    if (selectedLocation === 'all') return;
    if (!confirm(`Möchtest du wirklich das gesamte Lager an "${selectedLocation}" leeren?`)) {
      return;
    }
    try {
      const res = await bridge.sendRequest<{
        locations: WarehouseLocationDto[];
        items: WarehouseItemDto[];
      }>('clear_warehouse_location', {
        location: selectedLocation,
      });
      if (res?.items) setItems(res.items);
      if (res?.locations) setLocations(res.locations);
      showToast(`Lager an "${selectedLocation}" geleert`);
    } catch (err) {
      console.error('Failed to clear location:', err);
    }
  };

  const handleExportMarkdown = () => {
    const locName = selectedLocation === 'all' ? 'Alle Standorte' : selectedLocation;
    let md = `# Star Citizen Lagerbestand — ${locName}\n\n`;
    md += `*Exportiert am: ${new Date().toLocaleString('de-DE')}*\n\n`;
    md += `| Icon | Gegenstand | CIG Klasse | Kategorie | Standort | Menge | Letztes Update |\n`;
    md += `| :---: | :--- | :--- | :--- | :--- | :---: | :--- |\n`;

    items.forEach((it) => {
      md += `| ${it.icon} | **${it.itemName}** | \`${it.itemClass}\` | ${it.category} | ${it.location} | **${it.quantity}×** | ${it.lastUpdated} |\n`;
    });

    navigator.clipboard.writeText(md);
    showToast('Lagerbestand als Markdown kopiert!');
  };

  const totalAllItems = locations.reduce((acc, l) => acc + l.totalItems, 0);

  const filteredLocations = locations.filter((loc) => {
    if (systemFilter === 'all') return true;
    return loc.system.toLowerCase() === systemFilter.toLowerCase();
  });

  const categories = [
    'all',
    'Rüstung',
    'Waffen',
    'Komponenten',
    'Rohstoffe',
    'Munition',
    'Sonstiges',
  ];

  return (
    <div className="flex min-h-full min-h-[500px] gap-3 font-sans select-none">
      {/* ══ LINKE SPALTE: STANDORTE & STATIONEN ══ */}
      <div className="w-72 bg-[#040914]/90 rounded-lg border border-cyan-950/80 flex flex-col overflow-hidden shrink-0 shadow-sm">
        {/* Header & System Filter */}
        <div className="p-3 border-b border-cyan-950 bg-[#061224] space-y-2">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-1.5 text-xs font-mono font-bold uppercase tracking-wider text-cyan-300">
              <MapPin className="w-3.5 h-3.5 text-cyan-400" /> Standorte ({locations.length})
            </div>
            <span className="px-2 py-0.5 rounded bg-cyan-950 border border-cyan-800 text-cyan-300 font-mono text-[10px] font-bold">
              {totalAllItems} Items
            </span>
          </div>

          {/* System Pills */}
          <div className="flex items-center gap-1">
            {(['all', 'Stanton', 'Pyro'] as const).map((sys) => (
              <button
                key={sys}
                onClick={() => setSystemFilter(sys)}
                className={`flex-1 py-1 rounded text-[10px] font-mono font-semibold transition cursor-pointer border ${
                  systemFilter === sys
                    ? 'bg-cyan-950/80 border-cyan-600/70 text-cyan-300 shadow-sm'
                    : 'bg-[#030914] border-cyan-950/60 text-slate-400 hover:text-slate-200'
                }`}
              >
                {sys === 'all' ? 'Alle' : sys}
              </button>
            ))}
          </div>
        </div>

        {/* Location List */}
        <div className="flex-1 overflow-y-auto p-2 space-y-1 divide-y divide-cyan-950/30">
          <button
            onClick={() => setSelectedLocation('all')}
            className={`w-full text-left px-2.5 py-2 rounded text-xs transition cursor-pointer flex items-center justify-between font-mono ${
              selectedLocation === 'all'
                ? 'bg-cyan-950/60 text-cyan-300 border border-cyan-700/60 font-semibold shadow-sm'
                : 'text-slate-300 hover:bg-[#061224]/80 border border-transparent'
            }`}
          >
            <div className="flex items-center gap-2">
              <span className="text-sm">🌌</span>
              <span className="font-sans font-medium">Alle Standorte</span>
            </div>
            <span className="px-1.5 py-0.5 rounded text-[10px] bg-[#020610] text-cyan-400 font-bold">
              {totalAllItems}
            </span>
          </button>

          {filteredLocations.map((loc) => (
            <button
              key={loc.locationName}
              onClick={() => setSelectedLocation(loc.locationName)}
              className={`w-full text-left px-2.5 py-2 rounded text-xs transition cursor-pointer flex items-center justify-between font-mono pt-2 ${
                selectedLocation === loc.locationName
                  ? 'bg-cyan-950/60 text-cyan-300 border border-cyan-700/60 font-semibold shadow-sm'
                  : 'text-slate-300 hover:bg-[#061224]/80 border border-transparent'
              }`}
            >
              <div className="flex items-center gap-2 truncate">
                <span className="text-sm shrink-0">{loc.icon}</span>
                <div className="truncate">
                  <div className="truncate font-sans font-medium text-slate-200">{loc.locationName}</div>
                  <div className="text-[10px] text-slate-500 font-mono truncate">
                    {loc.parentBody ? `${loc.parentBody} · ` : ''}{loc.system}
                  </div>
                </div>
              </div>
              <span className="px-1.5 py-0.5 rounded text-[10px] bg-[#020610] text-cyan-400 font-bold shrink-0">
                {loc.totalItems}
              </span>
            </button>
          ))}
        </div>
      </div>

      {/* ══ RECHTE SPALTE: ARTIKELTABELLE ══ */}
      <div className="flex-1 flex flex-col space-y-2.5 min-w-0">
        {/* Toolbar & Filterleiste */}
        <div className="bg-[#040914]/90 rounded-lg p-2.5 border border-cyan-950/80 space-y-2 shrink-0">
          <div className="flex items-center justify-between gap-3">
            {/* Search */}
            <div className="relative flex-1 max-w-md">
              <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-500" />
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && fetchWarehouse()}
                placeholder="Gegenstand, Klasse oder CIG ID suchen..."
                className="w-full bg-[#071322] border border-cyan-900/60 rounded pl-8 pr-3 py-1.5 text-xs font-mono text-slate-200 placeholder-slate-500 focus:outline-none focus:border-cyan-500"
              />
            </div>

            {/* Actions */}
            <div className="flex items-center gap-2">
              {toastMessage && (
                <div className="flex items-center gap-1.5 px-2.5 py-1 rounded bg-emerald-950/60 border border-emerald-700/60 text-emerald-300 text-xs font-mono">
                  <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400 shrink-0" />
                  <span>{toastMessage}</span>
                </div>
              )}

              {selectedLocation !== 'all' && (
                <button
                  onClick={handleClearLocation}
                  className="px-2.5 py-1.5 rounded text-xs font-mono font-semibold border border-rose-800/60 bg-rose-950/40 text-rose-300 hover:bg-rose-900/50 transition cursor-pointer flex items-center gap-1"
                >
                  <Trash2 className="w-3.5 h-3.5" /> Standort leeren
                </button>
              )}

              <button
                onClick={handleExportMarkdown}
                title="Lagerbestand als formatierte Markdown-Tabelle kopieren"
                className="px-2.5 py-1.5 rounded text-xs font-mono font-semibold border border-cyan-950 hover:border-cyan-800 bg-[#061224] text-slate-300 hover:text-cyan-300 transition cursor-pointer flex items-center gap-1.5"
              >
                <Download className="w-3.5 h-3.5" /> Export (.md)
              </button>

              <button
                onClick={fetchWarehouse}
                title="Aktualisieren"
                className="p-1.5 rounded bg-[#071322] border border-cyan-950 hover:border-cyan-800 text-slate-400 hover:text-cyan-300 cursor-pointer"
              >
                <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin text-cyan-400' : ''}`} />
              </button>
            </div>
          </div>

          {/* Category Filter Chips */}
          <div className="flex items-center gap-1.5 overflow-x-auto no-scrollbar pt-0.5">
            {categories.map((c) => (
              <button
                key={c}
                onClick={() => setSelectedCategory(c)}
                className={`px-2.5 py-1 text-[11px] font-mono font-semibold rounded transition cursor-pointer shrink-0 ${
                  selectedCategory === c
                    ? 'bg-cyan-950/70 text-cyan-300 border border-cyan-600/60 shadow-sm'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900/60 border border-transparent'
                }`}
              >
                {c === 'all' ? 'Alle Kategorien' : c}
              </button>
            ))}
          </div>
        </div>

        {/* Dichte Artikeltabelle */}
        <div className="flex-1 bg-[#040914]/90 rounded-lg border border-cyan-950/80 overflow-auto shadow-sm min-h-[300px]">
          <table className="w-full min-w-[700px] text-left text-xs border-collapse font-mono">
            <thead>
              <tr className="border-b border-cyan-950 bg-[#061224] text-slate-400 text-[10.5px] font-bold uppercase tracking-wider sticky top-0 backdrop-blur-md z-10">
                <th className="py-2.5 px-3 w-10 text-center">#</th>
                <th className="py-2.5 px-4 font-sans">Gegenstand / CIG Klasse</th>
                <th className="py-2.5 px-4 font-sans">Kategorie</th>
                <th className="py-2.5 px-4 font-sans">Standort</th>
                <th className="py-2.5 px-4 text-center w-36">Menge</th>
                <th className="py-2.5 px-4">Erfasst</th>
                <th className="py-2.5 px-3 text-right w-12 font-sans">Aktion</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-cyan-950/40">
              {items.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-20 text-center text-slate-500 font-mono">
                    <Package className="w-8 h-8 text-cyan-500/30 mx-auto mb-2" />
                    Keine Gegenstände an diesem Standort gefunden.
                  </td>
                </tr>
              ) : (
                items.map((it) => {
                  const menuKey = `${it.location}_${it.itemClass}`;
                  const isMenuOpen = actionMenuOpenId === menuKey;

                  return (
                    <tr key={menuKey} className="hover:bg-[#071628]/60 transition-colors group">
                      <td className="py-2.5 px-3 text-center text-sm">{it.icon}</td>
                      <td className="py-2.5 px-4">
                        <div className="font-semibold text-slate-200 group-hover:text-cyan-300 transition font-sans text-xs">
                          {it.itemName}
                        </div>
                        <div className="text-[10px] text-slate-500 font-mono truncate max-w-xs">
                          {it.itemClass}
                        </div>
                      </td>
                      <td className="py-2.5 px-4">
                        <span className="px-2 py-0.5 rounded bg-[#030914] border border-cyan-950 text-[10px] text-slate-300">
                          {it.category}
                        </span>
                      </td>
                      <td className="py-2.5 px-4 text-slate-300">
                        <div className="font-sans text-xs">{it.location}</div>
                        <div className="text-[10px] text-slate-500 font-mono">
                          {it.parentBody ? `${it.parentBody} · ` : ''}{it.system}
                        </div>
                      </td>
                      <td className="py-2.5 px-4">
                        {/* +/- In-Grid Schnellanpassung */}
                        <div className="flex items-center justify-center gap-1.5">
                          <button
                            onClick={() => handleAdjustQty(it, -1)}
                            title="Menge um 1 verringern (-1)"
                            className="w-6 h-6 rounded bg-[#071322] hover:bg-rose-950/60 border border-cyan-950 hover:border-rose-700/60 text-slate-300 hover:text-rose-300 flex items-center justify-center transition cursor-pointer"
                          >
                            <Minus className="w-3 h-3" />
                          </button>
                          <span className="font-mono font-bold text-cyan-300 min-w-[36px] text-center text-sm">
                            {it.quantity}×
                          </span>
                          <button
                            onClick={() => handleAdjustQty(it, 1)}
                            title="Menge um 1 erhöhen (+1)"
                            className="w-6 h-6 rounded bg-[#071322] hover:bg-emerald-950/60 border border-cyan-950 hover:border-emerald-700/60 text-slate-300 hover:text-emerald-300 flex items-center justify-center transition cursor-pointer"
                          >
                            <Plus className="w-3 h-3" />
                          </button>
                        </div>
                      </td>
                      <td className="py-2.5 px-4 font-mono text-slate-400 text-[11px]">
                        {it.lastUpdated}
                      </td>
                      <td className="py-2.5 px-3 text-right relative">
                        <button
                          onClick={() => setActionMenuOpenId(isMenuOpen ? null : menuKey)}
                          className="p-1 rounded text-slate-400 hover:text-slate-200 hover:bg-[#061224] cursor-pointer"
                          title="Aktionen"
                        >
                          <MoreVertical className="w-3.5 h-3.5" />
                        </button>

                        {/* Dropdown Action Menu */}
                        {isMenuOpen && (
                          <div className="absolute right-0 top-8 z-30 w-56 bg-[#040914] border border-cyan-800/80 rounded-md shadow-xl py-1 text-left font-sans text-xs">
                            <button
                              onClick={() => {
                                handleAdjustQty(it, -1);
                                setActionMenuOpenId(null);
                              }}
                              className="w-full text-left px-3 py-1.5 hover:bg-cyan-950/60 text-slate-200 hover:text-cyan-300 flex items-center gap-2 transition cursor-pointer"
                            >
                              <Box className="w-3.5 h-3.5 text-cyan-400" />
                              <span>📦 Per Frachtaufzug entnehmen (-1)</span>
                            </button>

                            <button
                              onClick={() => {
                                handleAdjustQty(it, -1);
                                setActionMenuOpenId(null);
                              }}
                              className="w-full text-left px-3 py-1.5 hover:bg-cyan-950/60 text-slate-200 hover:text-amber-300 flex items-center gap-2 transition cursor-pointer"
                            >
                              <Wrench className="w-3.5 h-3.5 text-amber-400" />
                              <span>🔧 Zerlegt / Modifiziert (-1)</span>
                            </button>

                            <div className="h-px bg-cyan-950 my-1" />

                            <button
                              onClick={() => {
                                handleDeleteItem(it);
                                setActionMenuOpenId(null);
                              }}
                              className="w-full text-left px-3 py-1.5 hover:bg-rose-950/60 text-rose-400 hover:text-rose-300 flex items-center gap-2 transition cursor-pointer"
                            >
                              <Trash2 className="w-3.5 h-3.5" />
                              <span>Eintrag löschen</span>
                            </button>
                          </div>
                        )}
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default WarehouseView;
