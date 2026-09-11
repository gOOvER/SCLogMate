import React, { useEffect, useState, useMemo } from 'react';
import { bridge, LoadoutSlotDto } from '../services/photinoBridge';
import {
  Shield,
  Crosshair,
  Thermometer,
  Sparkles,
  RefreshCw,
  Copy,
  Check,
  Zap,
  Info,
} from 'lucide-react';

export const LoadoutView: React.FC = () => {
  const [slots, setSlots] = useState<LoadoutSlotDto[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [copied, setCopied] = useState<boolean>(false);

  const fetchLoadout = async () => {
    try {
      setLoading(true);
      const res = await bridge.sendRequest<LoadoutSlotDto[]>('get_loadout');
      setSlots(res || []);
    } catch (err) {
      console.error('Failed to load loadout:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchLoadout();
  }, []);

  // Partition slots into Armor/Clothing vs Weapons/Equipment
  const armorSlots = useMemo(() => {
    const keys = ['Helmet', 'Torso', 'Arms', 'Legs', 'Undersuit', 'Backpack'];
    return keys.map((k) => slots.find((s) => s.slotKey.toLowerCase() === k.toLowerCase()) || {
      slotKey: k,
      slotName: k,
      icon: '🛡️',
      itemName: '—',
      armorClass: '',
      damageReduction: 0,
      tempRange: '',
      badgeColor: '#64748B',
      isEquipped: false,
    });
  }, [slots]);

  const gearSlots = useMemo(() => {
    const keys = ['Primary1', 'Primary2', 'Sidearm', 'MultiTool', 'MedItem'];
    return keys.map((k) => slots.find((s) => s.slotKey.toLowerCase() === k.toLowerCase()) || {
      slotKey: k,
      slotName: k,
      icon: '🎯',
      itemName: '—',
      armorClass: '',
      damageReduction: 0,
      tempRange: '',
      badgeColor: '#64748B',
      isEquipped: false,
    });
  }, [slots]);

  // Overall loadout stats
  const stats = useMemo(() => {
    const equippedArmor = armorSlots.filter((s) => s.isEquipped && s.damageReduction > 0);
    const avgDmgRed =
      equippedArmor.length > 0
        ? Math.round(equippedArmor.reduce((acc, cur) => acc + cur.damageReduction, 0) / equippedArmor.length)
        : 0;

    const maxDmgRed =
      equippedArmor.length > 0
        ? Math.max(...equippedArmor.map((s) => s.damageReduction))
        : 0;

    const equippedCount = slots.filter((s) => s.isEquipped).length;

    // Detect general armor type
    const armorClasses = armorSlots
      .map((s) => s.armorClass?.toLowerCase())
      .filter((c) => Boolean(c) && c !== 'keine' && c !== 'undersuit');

    let dominantClass = 'Zivil / Flight Suit';
    if (armorClasses.some((c) => c?.includes('heavy') || c?.includes('schwer'))) {
      dominantClass = 'Schwere Panzerung';
    } else if (armorClasses.some((c) => c?.includes('medium') || c?.includes('mittel'))) {
      dominantClass = 'Mittlere Panzerung';
    } else if (armorClasses.some((c) => c?.includes('light') || c?.includes('leicht'))) {
      dominantClass = 'Leichte Panzerung';
    }

    return {
      equippedCount,
      avgDmgRed,
      maxDmgRed,
      dominantClass,
    };
  }, [slots, armorSlots]);

  const handleCopyMarkdown = () => {
    const lines = [
      '# Star Citizen Pilot Loadout',
      `*Stand: ${new Date().toLocaleString('de-DE')}*`,
      '',
      `**Panzerung:** ${stats.dominantClass} (Max. Schutz: ${stats.maxDmgRed}%, Schnitt: ${stats.avgDmgRed}%)`,
      '',
      '### Panzerung & Kleidung',
      ...armorSlots.map(
        (s) => `- **${s.slotName}:** ${s.itemName}${s.armorClass ? ` (${s.armorClass}, -${s.damageReduction}%)` : ''}`
      ),
      '',
      '### Bewaffnung & Werkzeuge',
      ...gearSlots.map((s) => `- **${s.slotName}:** ${s.itemName}`),
    ];

    navigator.clipboard.writeText(lines.join('\n')).then(() => {
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    });
  };

  const getSlotIconComponent = (key: string) => {
    switch (key.toLowerCase()) {
      case 'helmet':
      case 'torso':
      case 'arms':
      case 'legs':
        return <Shield className="w-4 h-4 text-cyan-400" />;
      case 'undersuit':
      case 'backpack':
        return <Sparkles className="w-4 h-4 text-emerald-400" />;
      case 'primary1':
      case 'primary2':
      case 'sidearm':
        return <Crosshair className="w-4 h-4 text-rose-400" />;
      default:
        return <Zap className="w-4 h-4 text-amber-400" />;
    }
  };

  return (
    <div className="flex flex-col min-h-full space-y-4">
      {/* Top Header & Summary KPIs */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
        <div className="sc-glass rounded-lg p-3 border border-slate-800 flex items-center justify-between">
          <div>
            <div className="text-xs text-slate-400 font-mono tracking-wider">PANZERUNGS-KLASSE</div>
            <div className="text-lg font-bold text-cyan-400 mt-0.5">{stats.dominantClass}</div>
          </div>
          <div className="p-2.5 rounded-lg bg-cyan-500/10 text-cyan-400 border border-cyan-500/20">
            <Shield className="w-5 h-5" />
          </div>
        </div>

        <div className="sc-glass rounded-lg p-3 border border-slate-800 flex items-center justify-between">
          <div>
            <div className="text-xs text-slate-400 font-mono tracking-wider">MAX. SCHADENS-SCHUTZ</div>
            <div className="text-lg font-bold text-emerald-400 mt-0.5">-{stats.maxDmgRed}%</div>
          </div>
          <div className="p-2.5 rounded-lg bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
            <Shield className="w-5 h-5" />
          </div>
        </div>

        <div className="sc-glass rounded-lg p-3 border border-slate-800 flex items-center justify-between">
          <div>
            <div className="text-xs text-slate-400 font-mono tracking-wider">DURCHSCHNITT REDUKTION</div>
            <div className="text-lg font-bold text-slate-200 mt-0.5">-{stats.avgDmgRed}%</div>
          </div>
          <div className="p-2.5 rounded-lg bg-slate-800 text-slate-400 border border-slate-700">
            <Thermometer className="w-5 h-5" />
          </div>
        </div>

        <div className="sc-glass rounded-lg p-3 border border-slate-800 flex items-center justify-between">
          <div>
            <div className="text-xs text-slate-400 font-mono tracking-wider">AUSGERÜSTETE SLOTS</div>
            <div className="text-lg font-bold text-amber-400 mt-0.5">
              {stats.equippedCount} <span className="text-xs font-normal text-slate-500">/ {slots.length}</span>
            </div>
          </div>
          <div className="flex items-center gap-1.5">
            <button
              onClick={handleCopyMarkdown}
              title="Als Markdown kopieren"
              className="p-2 rounded-md border border-slate-700 hover:border-cyan-500/50 hover:bg-cyan-500/10 text-slate-400 hover:text-cyan-400 transition cursor-pointer"
            >
              {copied ? <Check className="w-4 h-4 text-emerald-400" /> : <Copy className="w-4 h-4" />}
            </button>
            <button
              onClick={fetchLoadout}
              title="Aktualisieren"
              className="p-2 rounded-md border border-slate-700 hover:border-cyan-500/50 hover:bg-cyan-500/10 text-slate-400 hover:text-cyan-400 transition cursor-pointer"
            >
              <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin text-cyan-400' : ''}`} />
            </button>
          </div>
        </div>
      </div>

      {/* Main Loadout Layout: 2 Columns (Armor vs Weapons) */}
      <div className="flex-1 grid grid-cols-1 lg:grid-cols-2 gap-4 overflow-y-auto pr-1">
        {/* Armor & Protection Column */}
        <div className="space-y-3">
          <div className="flex items-center justify-between px-1">
            <div className="flex items-center gap-2">
              <Shield className="w-4 h-4 text-cyan-400" />
              <h2 className="text-sm font-bold text-slate-200 tracking-wide font-mono uppercase">
                Rüstung & Schutzanzug
              </h2>
            </div>
            <span className="text-xs font-mono text-slate-500">6 Slots</span>
          </div>

          <div className="space-y-2.5">
            {armorSlots.map((slot) => {
              return (
                <div
                  key={slot.slotKey}
                  className={`sc-glass rounded-lg p-3 border transition-all duration-200 ${
                    slot.isEquipped
                      ? 'border-cyan-500/30 hover:border-cyan-500/60 shadow-[0_0_12px_rgba(0,240,255,0.05)]'
                      : 'border-slate-800 opacity-60'
                  }`}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex items-start gap-3">
                      <div className="p-2 rounded-lg bg-slate-900 border border-slate-800 text-lg shrink-0">
                        {slot.icon || getSlotIconComponent(slot.slotKey)}
                      </div>

                      <div>
                        <div className="flex items-center gap-2">
                          <span className="text-xs font-mono text-slate-400 uppercase tracking-wider">
                            {slot.slotName}
                          </span>
                          {slot.armorClass && (
                            <span
                              className="px-2 py-0.2 text-[10px] font-mono rounded border uppercase"
                              style={{
                                color: slot.badgeColor || '#38BDF8',
                                borderColor: `${slot.badgeColor}40` || '#38BDF840',
                                backgroundColor: `${slot.badgeColor}15` || '#38BDF815',
                              }}
                            >
                              {slot.armorClass}
                            </span>
                          )}
                        </div>

                        <div className="text-sm font-bold text-slate-100 mt-0.5">
                          {slot.itemName || '— Nicht ausgerüstet —'}
                        </div>

                        {/* Temp Range & Meta */}
                        {slot.tempRange && (
                          <div className="flex items-center gap-1 text-[11px] text-slate-400 font-mono mt-1">
                            <Thermometer className="w-3 h-3 text-cyan-400" />
                            <span>{slot.tempRange}</span>
                          </div>
                        )}

                        {slot.lastEquipped && (
                          <div className="text-[10px] text-slate-500 font-mono mt-0.5">
                            Erkannt: {slot.lastEquipped}
                          </div>
                        )}
                      </div>
                    </div>

                    {/* Damage reduction badge */}
                    {slot.damageReduction > 0 && (
                      <div className="flex flex-col items-end shrink-0">
                        <div className="text-xs font-mono font-bold text-emerald-400 bg-emerald-500/10 px-2 py-1 rounded border border-emerald-500/20 shadow-[0_0_8px_rgba(16,185,129,0.2)]">
                          -{slot.damageReduction}%
                        </div>
                        <span className="text-[9px] font-mono text-slate-500 mt-0.5">SCHADENS-RED.</span>
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* Weapons & Equipment Column */}
        <div className="space-y-3">
          <div className="flex items-center justify-between px-1">
            <div className="flex items-center gap-2">
              <Crosshair className="w-4 h-4 text-rose-400" />
              <h2 className="text-sm font-bold text-slate-200 tracking-wide font-mono uppercase">
                Bewaffnung & Ausrüstung
              </h2>
            </div>
            <span className="text-xs font-mono text-slate-500">5 Slots</span>
          </div>

          <div className="space-y-2.5">
            {gearSlots.map((slot) => {
              return (
                <div
                  key={slot.slotKey}
                  className={`sc-glass rounded-lg p-3 border transition-all duration-200 ${
                    slot.isEquipped
                      ? 'border-rose-500/30 hover:border-rose-500/60 shadow-[0_0_12px_rgba(244,63,94,0.05)]'
                      : 'border-slate-800 opacity-60'
                  }`}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex items-start gap-3">
                      <div className="p-2 rounded-lg bg-slate-900 border border-slate-800 text-lg shrink-0">
                        {slot.icon || getSlotIconComponent(slot.slotKey)}
                      </div>

                      <div>
                        <div className="flex items-center gap-2">
                          <span className="text-xs font-mono text-slate-400 uppercase tracking-wider">
                            {slot.slotName}
                          </span>
                        </div>

                        <div className="text-sm font-bold text-slate-100 mt-0.5">
                          {slot.itemName || '— Nicht ausgerüstet —'}
                        </div>

                        {slot.rawClass && (
                          <div className="text-[10px] text-slate-500 font-mono mt-1 truncate max-w-xs">
                            {slot.rawClass}
                          </div>
                        )}

                        {slot.lastEquipped && (
                          <div className="text-[10px] text-slate-500 font-mono mt-0.5">
                            Erkannt: {slot.lastEquipped}
                          </div>
                        )}
                      </div>
                    </div>

                    <div className="shrink-0">
                      {slot.isEquipped ? (
                        <span className="px-2 py-0.5 text-[10px] font-semibold rounded bg-rose-500/20 text-rose-300 border border-rose-500/30">
                          Aktiv
                        </span>
                      ) : (
                        <span className="px-2 py-0.5 text-[10px] font-medium rounded bg-slate-800/80 text-slate-500 border border-slate-700">
                          Leer
                        </span>
                      )}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>

          {/* Quick Info Box */}
          <div className="sc-glass rounded-lg p-3 border border-slate-800 text-xs text-slate-400 flex items-start gap-2 mt-4">
            <Info className="w-4 h-4 text-cyan-400 shrink-0 mt-0.5" />
            <div className="leading-relaxed">
              Die Ausrüstung wird kontinuierlich live aus den Star Citizen Game-Logs und den erkannten Loadout-Events extrahiert. Bei einem Rüstungswechsel im Spiel wird das Profil automatisch synchronisiert.
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
