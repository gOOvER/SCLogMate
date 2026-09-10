import React, { useEffect, useState, useMemo } from 'react';
import { bridge, MarketCommodityDto } from '../services/photinoBridge';
import {
  ShoppingBag,
  Search,
  RefreshCw,
  TrendingUp,
  ArrowRight,
} from 'lucide-react';

export const MarketView: React.FC = () => {
  const [commodities, setCommodities] = useState<MarketCommodityDto[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [search, setSearch] = useState<string>('');
  const [selectedCommodityName, setSelectedCommodityName] = useState<string>('Laranite');
  const [cargoScu, setCargoScu] = useState<number>(696); // Default C2 Hercules

  const fetchMarket = async () => {
    try {
      setLoading(true);
      const res = await bridge.sendRequest<MarketCommodityDto[]>('get_market');
      setCommodities(res || []);
      if (res && res.length > 0 && !selectedCommodityName) {
        setSelectedCommodityName(res[0].name);
      }
    } catch (err) {
      console.error('Failed to load market commodities:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchMarket();
  }, []);

  const selectedCommodity = useMemo(() => {
    return commodities.find((c) => c.name === selectedCommodityName) || commodities[0] || null;
  }, [commodities, selectedCommodityName]);

  // Profit calculation
  const calculation = useMemo(() => {
    if (!selectedCommodity) {
      return { investment: 0, revenue: 0, profit: 0, roi: 0 };
    }
    const investment = selectedCommodity.avgBuyPrice * cargoScu * 100; // 1 SCU = 100 cSCU units in aUEC
    const revenue = selectedCommodity.avgSellPrice * cargoScu * 100;
    const profit = revenue - investment;
    const roi = investment > 0 ? (profit / investment) * 100 : 0;
    return {
      investment: Math.round(investment),
      revenue: Math.round(revenue),
      profit: Math.round(profit),
      roi: Math.round(roi * 10) / 10,
    };
  }, [selectedCommodity, cargoScu]);

  const filteredCommodities = useMemo(() => {
    return commodities.filter((c) => {
      if (search.trim()) {
        const q = search.toLowerCase();
        return (
          c.name.toLowerCase().includes(q) ||
          c.category.toLowerCase().includes(q) ||
          c.bestBuyLocation.toLowerCase().includes(q) ||
          c.bestSellLocation.toLowerCase().includes(q)
        );
      }
      return true;
    });
  }, [commodities, search]);

  const shipPresets = [
    { label: 'C2 Hercules (696 SCU)', scu: 696 },
    { label: 'Caterpillar (576 SCU)', scu: 576 },
    { label: 'Taurus (174 SCU)', scu: 174 },
    { label: 'Freelancer MAX (120 SCU)', scu: 120 },
    { label: 'Cutlass Black (46 SCU)', scu: 46 },
  ];

  return (
    <div className="flex flex-col h-full space-y-4">
      {/* Cargo Run Profit Calculator Header */}
      <div className="sc-glass rounded-lg p-4 border border-slate-800 sc-hud-corner space-y-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <ShoppingBag className="w-5 h-5 text-cyan-400" />
            <div>
              <h2 className="text-sm font-bold text-slate-100 uppercase tracking-wider font-mono">
                Handelsrouten & Frachtgewinn-Kalkulator
              </h2>
              <div className="text-[11px] text-slate-400">
                Kalkuliere Investition, Erlös und Reingewinn für deine Frachtrouten im Stanton & Pyro System
              </div>
            </div>
          </div>

          {/* Ship Presets */}
          <div className="flex items-center gap-1.5 overflow-x-auto">
            {shipPresets.map((s) => (
              <button
                key={s.label}
                onClick={() => setCargoScu(s.scu)}
                className={`px-2.5 py-1 text-[11px] font-mono rounded border transition cursor-pointer shrink-0 ${
                  cargoScu === s.scu
                    ? 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/40 shadow-[0_0_8px_rgba(0,240,255,0.2)]'
                    : 'bg-slate-900 border-slate-800 text-slate-400 hover:text-slate-200'
                }`}
              >
                {s.label}
              </button>
            ))}
          </div>
        </div>

        {/* Inputs & Calculation Output Grid */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-3 pt-1">
          {/* Commodity picker */}
          <div className="bg-slate-900/80 p-2.5 rounded border border-slate-800">
            <label className="text-[10px] font-mono text-slate-500 block mb-1">WARE / ROHSTOFF</label>
            <select
              value={selectedCommodityName}
              onChange={(e) => setSelectedCommodityName(e.target.value)}
              className="w-full bg-slate-950 border border-slate-800 rounded p-1.5 text-xs text-slate-200 focus:outline-none focus:border-cyan-500 cursor-pointer font-mono font-bold"
            >
              {commodities.map((c) => (
                <option key={c.name} value={c.name}>
                  {c.name} (+{c.margin.toFixed(2)} aUEC)
                </option>
              ))}
            </select>
          </div>

          {/* Cargo SCU input */}
          <div className="bg-slate-900/80 p-2.5 rounded border border-slate-800">
            <label className="text-[10px] font-mono text-slate-500 block mb-1">FRACHTKAPAZITÄT (SCU)</label>
            <input
              type="number"
              value={cargoScu}
              onChange={(e) => setCargoScu(Math.max(1, parseInt(e.target.value) || 1))}
              className="w-full bg-slate-950 border border-slate-800 rounded p-1.5 text-xs text-slate-200 font-mono font-bold focus:outline-none focus:border-cyan-500"
            />
          </div>

          {/* Investment required */}
          <div className="bg-slate-900/80 p-2.5 rounded border border-slate-800 flex flex-col justify-between">
            <span className="text-[10px] font-mono text-slate-500">BENÖTIGTES KAPITAL</span>
            <div className="text-base font-bold font-mono text-slate-200">
              {calculation.investment.toLocaleString('de-DE')}{' '}
              <span className="text-xs font-normal text-slate-400">aUEC</span>
            </div>
          </div>

          {/* Net profit per run */}
          <div className="bg-emerald-950/20 border border-emerald-500/30 p-2.5 rounded flex flex-col justify-between shadow-[0_0_12px_rgba(16,185,129,0.1)]">
            <div className="flex justify-between items-center">
              <span className="text-[10px] font-mono text-emerald-400">REINGEWINN / RUN</span>
              <span className="text-[10px] font-mono text-emerald-300 font-bold">+{calculation.roi}% ROI</span>
            </div>
            <div className="text-base font-bold font-mono text-emerald-300">
              +{calculation.profit.toLocaleString('de-DE')}{' '}
              <span className="text-xs font-normal text-emerald-400/80">aUEC</span>
            </div>
          </div>
        </div>

        {/* Selected Route Info */}
        {selectedCommodity && (
          <div className="pt-2 border-t border-slate-800/80 flex flex-wrap items-center justify-between text-xs font-mono text-slate-400 gap-2">
            <div className="flex items-center gap-2">
              <span className="text-slate-500">Kauf:</span>
              <span className="text-cyan-300 font-semibold">{selectedCommodity.bestBuyLocation}</span>
              <ArrowRight className="w-3 h-3 text-slate-600" />
              <span className="text-slate-500">Verkauf:</span>
              <span className="text-emerald-300 font-semibold">{selectedCommodity.bestSellLocation}</span>
            </div>
            <div className="text-slate-500">
              Marge: <strong className="text-slate-200">+{selectedCommodity.margin.toFixed(2)} aUEC / Einheit</strong>
            </div>
          </div>
        )}
      </div>

      {/* Market Commodities Table Filter Bar */}
      <div className="sc-glass rounded-lg p-3 border border-slate-800 flex flex-wrap items-center justify-between gap-3">
        <div className="text-xs font-bold font-mono text-slate-300 flex items-center gap-2">
          <TrendingUp className="w-4 h-4 text-cyan-400" />
          <span>ROHSTOFF-PREISE & MARKTMARGEN ({filteredCommodities.length})</span>
        </div>

        <div className="flex items-center gap-2">
          <div className="relative">
            <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-slate-500" />
            <input
              type="text"
              placeholder="Ware oder Handelsort filtern..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="bg-slate-900 border border-slate-800 rounded pl-8 pr-3 py-1.5 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-cyan-500 w-56 font-mono"
            />
          </div>

          <button
            onClick={fetchMarket}
            title="Aktualisieren"
            className="p-1.5 rounded bg-slate-900 border border-slate-800 text-slate-400 hover:text-cyan-400 transition cursor-pointer"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin text-cyan-400' : ''}`} />
          </button>
        </div>
      </div>

      {/* Table */}
      <div className="flex-1 overflow-y-auto pr-1">
        <div className="sc-glass rounded-lg border border-slate-800 overflow-hidden">
          <table className="w-full text-left border-collapse text-xs font-mono">
            <thead>
              <tr className="border-b border-slate-800 bg-slate-950/60 text-slate-400 text-[11px]">
                <th className="py-2.5 px-3">WARE / ROHSTOFF</th>
                <th className="py-2.5 px-3">KATEGORIE</th>
                <th className="py-2.5 px-3">TIER</th>
                <th className="py-2.5 px-3 text-right">Ø EINKAUF</th>
                <th className="py-2.5 px-3 text-right">Ø VERKAUF</th>
                <th className="py-2.5 px-3 text-right">MARGE / EINHEIT</th>
                <th className="py-2.5 px-3">BESTER EINKAUFSORT</th>
                <th className="py-2.5 px-3">BESTER VERKAUFSORT</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/60">
              {filteredCommodities.map((item) => {
                const isSelected = item.name === selectedCommodityName;

                return (
                  <tr
                    key={item.name}
                    onClick={() => setSelectedCommodityName(item.name)}
                    className={`transition cursor-pointer group ${
                      isSelected
                        ? 'bg-cyan-950/30 text-cyan-200 font-bold'
                        : 'hover:bg-slate-900/60 text-slate-300'
                    }`}
                  >
                    <td className="py-2.5 px-3 font-bold group-hover:text-cyan-300">
                      {item.name}
                    </td>
                    <td className="py-2.5 px-3 text-slate-400">
                      {item.category}
                    </td>
                    <td className="py-2.5 px-3">
                      <span className="px-2 py-0.5 text-[10px] font-bold rounded bg-slate-800 border border-slate-700 text-amber-300">
                        {item.tier}
                      </span>
                    </td>
                    <td className="py-2.5 px-3 text-right text-slate-300">
                      {item.avgBuyPrice.toFixed(2)} aUEC
                    </td>
                    <td className="py-2.5 px-3 text-right text-slate-300">
                      {item.avgSellPrice.toFixed(2)} aUEC
                    </td>
                    <td className="py-2.5 px-3 text-right text-emerald-400 font-bold">
                      +{item.margin.toFixed(2)} aUEC
                    </td>
                    <td className="py-2.5 px-3 text-slate-400 text-[11px] truncate max-w-xs">
                      {item.bestBuyLocation}
                    </td>
                    <td className="py-2.5 px-3 text-slate-400 text-[11px] truncate max-w-xs">
                      {item.bestSellLocation}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};
