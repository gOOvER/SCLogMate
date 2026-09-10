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
} from 'lucide-react';

export const WarehouseView: React.FC = () => {
  const [locations, setLocations] = useState<WarehouseLocationDto[]>([]);
  const [items, setItems] = useState<WarehouseItemDto[]>([]);
  const [selectedLocation, setSelectedLocation] = useState<string>('all');
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [search, setSearch] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(false);
  const [actionMenuOpenId, setActionMenuOpenId] = useState<string | null>(null);

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
    } catch (err) {
      console.error('Failed to clear location:', err);
    }
  };

  const handleExportMarkdown = () => {
    const locName = selectedLocation === 'all' ? 'Alle Standorte' : selectedLocation;
    let md = `# Star Citizen Lagerbestand — ${locName}\n\n`;
    md += `*Exportiert am: ${new Date().toLocaleString('de-DE')}*\n\n`;
    md += `| Icon | Gegenstand | CIG Klasse | Kategorie | Standort | Menge | Letztes Update |\n`;
    md += `| :--: | :--------- | :--------- | :-------- | :------- | :---: | :------------- |\n`;

    items.forEach((it) => {
      md += `| ${it.icon} | ${it.itemName} | \`${it.itemClass}\` | ${it.category} | ${it.location} | **${it.quantity}×** | ${it.lastUpdated} |\n`;
    });

    const blob = new Blob([md], { type: 'text/markdown;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `Lagerbestand_${locName.replace(/[^a-zA-Z0-9_-]/g, '_')}.md`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const categories = [
    'all',
    'Mineralien & Erze',
    'Werkzeuge',
    'Waffen',
    'Rüstung',
    'Verbrauchsgüter',
    'Quest & Utility',
    'Sonstiges',
  ];

  const totalAllItems = locations.reduce((acc, l) => acc + l.totalItems, 0);

  return (
    <div className="flex h-full gap-4 overflow-hidden">
      {/* Left Sidebar: Planetary & Station Locations */}
      <div className="w-64 sc-glass rounded-lg border border-slate-800 flex flex-col overflow-hidden shrink-0">
        <div className="p-3 border-b border-slate-800 flex items-center justify-between bg-slate-900/60">
          <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-cyan-300">
            <MapPin className="w-4 h-4 text-cyan-400" /> Standorte ({locations.length})
          </div>
          <span className="sc-badge text-[10px]">{totalAllItems} Items</span>
        </div>

        <div className="flex-1 overflow-y-auto p-2 space-y-1">
          <button
            onClick={() => setSelectedLocation('all')}
            className={`w-full text-left px-3 py-2 rounded text-xs transition cursor-pointer flex items-center justify-between ${
              selectedLocation === 'all'
                ? 'bg-cyan-950/40 text-cyan-300 border border-cyan-500/40 font-semibold'
                : 'text-slate-300 hover:bg-slate-900/60 border border-transparent'
            }`}
          >
            <div className="flex items-center gap-2">
              <span className="text-sm">🌌</span>
              <span>Alle Standorte</span>
            </div>
            <span className="px-1.5 py-0.5 rounded text-[10px] bg-slate-800 font-mono text-slate-400">
              {totalAllItems}
            </span>
          </button>

          {locations.map((loc) => (
            <button
              key={loc.locationName}
              onClick={() => setSelectedLocation(loc.locationName)}
              className={`w-full text-left px-3 py-2 rounded text-xs transition cursor-pointer flex items-center justify-between ${
                selectedLocation === loc.locationName
                  ? 'bg-cyan-950/40 text-cyan-300 border border-cyan-500/40 font-semibold'
                  : 'text-slate-300 hover:bg-slate-900/60 border border-transparent'
              }`}
            >
              <div className="flex items-center gap-2 truncate">
                <span className="text-sm shrink-0">{loc.icon}</span>
                <div className="truncate">
                  <div className="truncate text-slate-200">{loc.locationName}</div>
                  <div className="text-[10px] text-slate-500 font-mono truncate">
                    {loc.parentBody ? `${loc.parentBody} · ` : ''}{loc.system}
                  </div>
                </div>
              </div>
              <span className="px-1.5 py-0.5 rounded text-[10px] bg-slate-800 font-mono text-slate-400 shrink-0">
                {loc.totalItems}
              </span>
            </button>
          ))}
        </div>
      </div>

      {/* Right Main Area: Warehouse Items Grid */}
      <div className="flex-1 flex flex-col space-y-3 overflow-hidden">
        {/* Toolbar & Category Chips */}
        <div className="sc-glass rounded-lg p-3 border border-slate-800 space-y-2.5">
          <div className="flex items-center justify-between gap-3">
            {/* Search */}
            <div className="relative flex-1 max-w-sm">
              <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-500" />
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && fetchWarehouse()}
                placeholder="Gegenstand oder Klasse filtern..."
                className="w-full bg-slate-900/80 border border-slate-800 rounded pl-8 pr-3 py-1.5 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-cyan-500/50"
              />
            </div>

            {/* Actions */}
            <div className="flex items-center gap-2">
              {selectedLocation !== 'all' && (
                <button
                  onClick={handleClearLocation}
                  className="px-2.5 py-1.5 rounded text-xs font-semibold border border-rose-500/30 bg-rose-950/30 text-rose-300 hover:bg-rose-900/40 transition cursor-pointer flex items-center gap-1"
                >
                  <Trash2 className="w-3.5 h-3.5" /> Standort leeren
                </button>
              )}

              <button
                onClick={handleExportMarkdown}
                title="Lagerbestand als Markdown speichern"
                className="px-2.5 py-1.5 rounded text-xs font-semibold border border-slate-800 hover:border-slate-700 bg-slate-900 text-slate-300 hover:text-cyan-300 transition cursor-pointer flex items-center gap-1.5"
              >
                <Download className="w-3.5 h-3.5" /> Exportieren (.md)
              </button>

              <button
                onClick={fetchWarehouse}
                title="Neu laden"
                className="p-1.5 rounded bg-slate-900 border border-slate-800 text-slate-400 hover:text-slate-200 cursor-pointer"
              >
                <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
              </button>
            </div>
          </div>

          {/* Category Filter Chips */}
          <div className="flex items-center gap-1.5 overflow-x-auto pt-1">
            {categories.map((c) => (
              <button
                key={c}
                onClick={() => setSelectedCategory(c)}
                className={`px-2.5 py-1 text-[11px] font-medium rounded transition cursor-pointer shrink-0 ${
                  selectedCategory === c
                    ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40 font-semibold'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900 border border-transparent'
                }`}
              >
                {c === 'all' ? 'Alle Kategorien' : c}
              </button>
            ))}
          </div>
        </div>

        {/* Data Grid */}
        <div className="flex-1 sc-glass rounded-lg border border-slate-800 overflow-y-auto">
          <table className="w-full text-left text-xs border-collapse">
            <thead>
              <tr className="border-b border-slate-800 bg-slate-900/60 text-slate-400 font-mono uppercase tracking-wider sticky top-0 backdrop-blur-md z-10">
                <th className="py-3 px-3 w-10 text-center">#</th>
                <th className="py-3 px-4">Gegenstand / CIG Klasse</th>
                <th className="py-3 px-4">Kategorie</th>
                <th className="py-3 px-4">Standort</th>
                <th className="py-3 px-4 text-center w-36">Menge</th>
                <th className="py-3 px-4">Zuletzt erfasst</th>
                <th className="py-3 px-3 text-right w-12">Aktion</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/40">
              {items.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-16 text-center text-slate-500 font-mono">
                    <Package className="w-8 h-8 text-cyan-500/30 mx-auto mb-2" />
                    Keine Gegenstände an diesem Standort gefunden.
                  </td>
                </tr>
              ) : (
                items.map((it) => {
                  const menuKey = `${it.location}_${it.itemClass}`;
                  const isMenuOpen = actionMenuOpenId === menuKey;

                  return (
                    <tr
                      key={menuKey}
                      className="hover:bg-slate-900/40 transition group"
                    >
                      <td className="py-2.5 px-3 text-center text-sm">{it.icon}</td>
                      <td className="py-2.5 px-4">
                        <div className="font-semibold text-slate-200 group-hover:text-cyan-300 transition">
                          {it.itemName}
                        </div>
                        <div className="text-[10px] text-slate-500 font-mono truncate max-w-xs">
                          {it.itemClass}
                        </div>
                      </td>
                      <td className="py-2.5 px-4">
                        <span className="sc-badge text-[10px]">{it.category}</span>
                      </td>
                      <td className="py-2.5 px-4 text-slate-300">
                        <div>{it.location}</div>
                        <div className="text-[10px] text-slate-500 font-mono">
                          {it.parentBody ? `${it.parentBody} · ` : ''}{it.system}
                        </div>
                      </td>
                      <td className="py-2.5 px-4">
                        {/* +/- Adjustment Controls */}
                        <div className="flex items-center justify-center gap-1.5">
                          <button
                            onClick={() => handleAdjustQty(it, -1)}
                            title="Menge um 1 verringern (-1)"
                            className="w-6 h-6 rounded bg-slate-800/80 hover:bg-rose-950/40 border border-slate-700 hover:border-rose-500/40 text-slate-300 hover:text-rose-300 flex items-center justify-center transition cursor-pointer"
                          >
                            <Minus className="w-3 h-3" />
                          </button>
                          <span className="font-mono font-bold text-cyan-300 min-w-[36px] text-center text-sm">
                            {it.quantity}×
                          </span>
                          <button
                            onClick={() => handleAdjustQty(it, 1)}
                            title="Menge um 1 erhöhen (+1)"
                            className="w-6 h-6 rounded bg-slate-800/80 hover:bg-emerald-950/40 border border-slate-700 hover:border-emerald-500/40 text-slate-300 hover:text-emerald-300 flex items-center justify-center transition cursor-pointer"
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
                          onClick={() =>
                            setActionMenuOpenId(isMenuOpen ? null : menuKey)
                          }
                          className="p-1 rounded text-slate-400 hover:text-slate-200 hover:bg-slate-800 cursor-pointer"
                        >
                          <MoreVertical className="w-3.5 h-3.5" />
                        </button>

                        {isMenuOpen && (
                          <div
                            onMouseLeave={() => setActionMenuOpenId(null)}
                            className="absolute right-3 top-8 z-30 w-52 rounded-md bg-slate-950 border border-slate-800 shadow-xl py-1 text-left"
                          >
                            <button
                              onClick={() => {
                                handleAdjustQty(it, -1);
                                setActionMenuOpenId(null);
                              }}
                              className="w-full px-3 py-1.5 text-xs text-slate-300 hover:bg-cyan-950/40 hover:text-cyan-300 text-left flex items-center gap-2 cursor-pointer"
                            >
                              <Box className="w-3.5 h-3.5 text-cyan-400" />
                              <span>Per Frachtaufzug entnehmen (-1)</span>
                            </button>
                            <button
                              onClick={() => {
                                handleAdjustQty(it, -1);
                                setActionMenuOpenId(null);
                              }}
                              className="w-full px-3 py-1.5 text-xs text-slate-300 hover:bg-amber-950/40 hover:text-amber-300 text-left flex items-center gap-2 cursor-pointer"
                            >
                              <Wrench className="w-3.5 h-3.5 text-amber-400" />
                              <span>Zerlegt / Dismantled (-1)</span>
                            </button>
                            <div className="my-1 border-t border-slate-800" />
                            <button
                              onClick={() => {
                                handleDeleteItem(it);
                                setActionMenuOpenId(null);
                              }}
                              className="w-full px-3 py-1.5 text-xs text-rose-400 hover:bg-rose-950/40 text-left flex items-center gap-2 cursor-pointer"
                            >
                              <Trash2 className="w-3.5 h-3.5" />
                              <span>Aus Lagerbestand löschen</span>
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
