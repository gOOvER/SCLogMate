import React, { useState } from 'react';
import {
  bridge,
  FleetShipDto,
  ScannedShipComponentDto,
} from '../services/photinoBridge';
import {
  X,
  Wrench,
  Shield,
  Zap,
  Crosshair,
  RotateCcw,
  Palette,
  Camera,
  CheckCircle2,
  Radio,
  Cpu,
  BookOpen,
  ExternalLink,
  Coins,
} from 'lucide-react';
import { PipsAnalyzerBadge } from './PipsAnalyzerBadge';

interface ShipLoadoutModalProps {
  isOpen: boolean;
  onClose: () => void;
  ship: FleetShipDto | null;
  onRefreshFleet: () => void;
}

export const ShipLoadoutModal: React.FC<ShipLoadoutModalProps> = ({
  isOpen,
  onClose,
  ship,
  onRefreshFleet,
}) => {
  const [isResetting, setIsResetting] = useState(false);
  const [isScanning, setIsScanning] = useState(false);
  const [feedback, setFeedback] = useState<string | null>(null);

  if (!isOpen || !ship) return null;

  const components: ScannedShipComponentDto[] = Array.isArray(ship.components) ? ship.components : [];
  const isCustomScanned = Boolean(ship.componentsUpdatedAt);

  const cleanComponentName = (rawName: string): string => {
    if (!rawName) return '';
    let name = rawName.trim();
    if (name.includes('(')) {
      name = name.split('(')[0].trim();
    }
    name = name.replace(/[•·*]/g, '').trim();
    name = name.replace(/['"]$/, '').trim();
    if (name.toLowerCase() === 'chili-max') return 'Chill-Max';
    if (name.toLowerCase() === 'gin-zel') return 'Ginzel';
    if (name.toLowerCase().startsWith('5ca')) return "5CA 'Akura'";
    return name;
  };

  const handleOpenComponentWiki = (compName: string, e?: React.MouseEvent) => {
    if (e) e.stopPropagation();
    const clean = cleanComponentName(compName);
    if (!clean) return;
    window.dispatchEvent(new CustomEvent('open-wiki-dossier', { detail: clean }));
  };

  const getSlotIcon = (slotType: string) => {
    switch (slotType.toLowerCase()) {
      case 'weapon':
        return <Crosshair className="w-3.5 h-3.5 text-amber-400" />;
      case 'turret':
        return <Crosshair className="w-3.5 h-3.5 text-orange-400" />;
      case 'quantumdrive':
      case 'qd':
      case 'jumpmodule':
        return <Radio className="w-3.5 h-3.5 text-cyan-400" />;
      case 'avionics':
      case 'radar':
        return <Radio className="w-3.5 h-3.5 text-indigo-400" />;
      case 'shield':
        return <Shield className="w-3.5 h-3.5 text-blue-400" />;
      case 'cooler':
        return <Cpu className="w-3.5 h-3.5 text-sky-400" />;
      case 'powerplant':
        return <Zap className="w-3.5 h-3.5 text-yellow-400" />;
      case 'utility':
        return <Wrench className="w-3.5 h-3.5 text-emerald-400" />;
      default:
        return <Wrench className="w-3.5 h-3.5 text-slate-400" />;
    }
  };

  const getSlotColor = (slotType: string) => {
    switch (slotType.toLowerCase()) {
      case 'weapon':
        return 'border-amber-500/30 bg-amber-950/20 text-amber-300';
      case 'turret':
        return 'border-orange-500/30 bg-orange-950/20 text-orange-300';
      case 'quantumdrive':
      case 'qd':
      case 'jumpmodule':
        return 'border-cyan-500/30 bg-cyan-950/20 text-cyan-300';
      case 'avionics':
      case 'radar':
        return 'border-indigo-500/30 bg-indigo-950/20 text-indigo-300';
      case 'shield':
        return 'border-blue-500/30 bg-blue-950/20 text-blue-300';
      case 'cooler':
        return 'border-sky-500/30 bg-sky-950/20 text-sky-300';
      case 'powerplant':
        return 'border-yellow-500/30 bg-yellow-950/20 text-yellow-300';
      case 'utility':
        return 'border-emerald-500/30 bg-emerald-950/20 text-emerald-300';
      default:
        return 'border-slate-700/50 bg-slate-900/30 text-slate-300';
    }
  };

  const handleResetStock = async () => {
    setIsResetting(true);
    setFeedback(null);
    try {
      await bridge.clearShipComponents(ship.name);
      setFeedback('✓ Auf Standard-Werkskonfiguration zurückgesetzt');
      onRefreshFleet();
    } catch (err: any) {
      setFeedback(`✕ Fehler: ${err?.message || 'Zurücksetzen fehlgeschlagen'}`);
    } finally {
      setIsResetting(false);
      setTimeout(() => setFeedback(null), 4000);
    }
  };

  const cleanShipName = ship.name.split(/\s*·\s*/)[0].trim();

  const handleScanScreenshot = async () => {
    setIsScanning(true);
    setFeedback('Scanne Screenshots (OCR läuft)...');
    try {
      const res = await bridge.scanScreenshotLoadout();
      if (res.success) {
        setFeedback(res.message || `✓ ${res.shipName || 'Schiff'} erkannt (${res.components?.length || 0} Komponenten)`);
        onRefreshFleet();
      } else {
        setFeedback(`✕ ${res.message || 'Kein VLM-Screenshot erkannt'}`);
      }
    } catch (err: any) {
      setFeedback(`✕ Fehler: ${err?.message || 'Scan fehlgeschlagen'}`);
    } finally {
      setIsScanning(false);
      setTimeout(() => setFeedback(null), 8000);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/75 backdrop-blur-sm animate-fade-in">
      <div
        className="bg-[#050C16] border border-[#142A45] rounded-xl shadow-2xl w-full max-w-2xl max-h-[90vh] flex flex-col overflow-hidden text-slate-200"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Modal Header */}
        <div className="px-5 py-3.5 border-b border-[#142A45] flex items-center justify-between bg-gradient-to-r from-[#071322] to-[#0A1A2F]">
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-lg bg-cyan-950/60 border border-cyan-800/60 text-cyan-400">
              <Wrench className="w-5 h-5" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h3 className="text-base font-bold text-slate-100">{cleanShipName}</h3>
                <span
                  className="px-1.5 py-0.5 rounded text-[10px] font-mono font-bold border"
                  style={{
                    borderColor: ship.manufacturerColor || '#38BDF8',
                    color: ship.manufacturerColor || '#38BDF8',
                    backgroundColor: '#071322',
                  }}
                >
                  {ship.manufacturerBadge || 'SHIP'}
                </span>
                {ship.isCurrent && (
                  <span className="px-2 py-0.5 rounded-full text-[9px] font-mono font-bold bg-emerald-950/80 border border-emerald-500/60 text-emerald-300">
                    AKTIV
                  </span>
                )}
              </div>
              <p className="text-xs text-slate-400 mt-0.5">
                {ship.manufacturer} · {ship.role}
              </p>
            </div>
          </div>

          <div className="flex items-center gap-2">
            {/* Quick Link: In-App Wiki Dossier */}
            <button
              onClick={() => {
                window.dispatchEvent(new CustomEvent('open-wiki-dossier', { detail: cleanShipName }));
              }}
              className="px-2.5 py-1.5 rounded-lg bg-[#071322] hover:bg-cyan-950/70 border border-[#142A45] hover:border-cyan-500/60 text-cyan-300 text-xs font-mono font-semibold flex items-center gap-1.5 transition cursor-pointer hover:shadow-[0_0_8px_rgba(6,182,212,0.3)]"
              title="Star Citizen Wiki Dossier für dieses Schiff öffnen"
            >
              <BookOpen className="w-3.5 h-3.5 text-cyan-400" />
              <span className="hidden sm:inline">SCWiki</span>
            </button>

            {/* Quick Link: Erkul Calculator */}
            <button
              onClick={() => {
                bridge.openExternalUrl('https://www.erkul.games/live/calculator');
              }}
              className="px-2.5 py-1.5 rounded-lg bg-[#071322] hover:bg-amber-950/70 border border-[#142A45] hover:border-amber-500/60 text-amber-300 text-xs font-mono font-semibold flex items-center gap-1.5 transition cursor-pointer hover:shadow-[0_0_8px_rgba(245,158,11,0.3)]"
              title="Erkul Games Loadout Calculator im Browser öffnen"
            >
              <Crosshair className="w-3.5 h-3.5 text-amber-400" />
              <span className="hidden sm:inline">Erkul</span>
            </button>

            {/* Quick Link: UEX Corp */}
            <button
              onClick={() => {
                bridge.openExternalUrl(`https://uexcorp.space/search?q=${encodeURIComponent(ship.name)}`);
              }}
              className="px-2.5 py-1.5 rounded-lg bg-[#071322] hover:bg-emerald-950/70 border border-[#142A45] hover:border-emerald-500/60 text-emerald-300 text-xs font-mono font-semibold flex items-center gap-1.5 transition cursor-pointer hover:shadow-[0_0_8px_rgba(16,185,129,0.3)]"
              title="UEX Markt- & Terminaldaten abrufen"
            >
              <Coins className="w-3.5 h-3.5 text-emerald-400" />
              <span className="hidden sm:inline">UEX</span>
            </button>

            <button
              onClick={onClose}
              className="p-1.5 rounded-lg text-slate-400 hover:text-slate-100 hover:bg-slate-800/70 transition cursor-pointer ml-1"
              title="Schließen"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Modal Body */}
        <div className="p-5 overflow-y-auto space-y-4 flex-1 custom-scrollbar">
          {/* Status & Livery Banner */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {/* Livery / Lackierung Card */}
            <div className="p-3 rounded-lg bg-[#071322] border border-[#142A45] flex items-center gap-3">
              <div className="p-2 rounded-md bg-purple-950/50 border border-purple-800/50 text-purple-400 shrink-0">
                <Palette className="w-4 h-4" />
              </div>
              <div className="min-w-0">
                <span className="text-[10px] font-mono uppercase text-slate-500 font-bold block">
                  Lackierung / Skin
                </span>
                <span className="text-xs font-semibold text-purple-200 truncate block">
                  {ship.livery || 'Standard-Werkslackierung'}
                </span>
              </div>
            </div>

            {/* Scan Origin Card */}
            <div className="p-3 rounded-lg bg-[#071322] border border-[#142A45] flex items-center gap-3">
              <div className={`p-2 rounded-md shrink-0 border ${
                isCustomScanned
                  ? 'bg-emerald-950/50 border-emerald-800/50 text-emerald-400'
                  : 'bg-slate-900/50 border-slate-700/50 text-slate-400'
              }`}>
                <Camera className="w-4 h-4" />
              </div>
              <div className="min-w-0">
                <span className="text-[10px] font-mono uppercase text-slate-500 font-bold block">
                  Erfassungsmethode
                </span>
                <span className="text-xs font-semibold text-slate-200 truncate block">
                  {isCustomScanned
                    ? `📷 VLM Screenshot (${ship.componentsUpdatedAt})`
                    : 'Werks-Katalog (Stock)'}
                </span>
              </div>
            </div>
          </div>

          {/* Pips & Weapon Synchronization Section */}
          <div className="p-3 rounded-lg bg-[#071322] border border-[#142A45] space-y-2">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Crosshair className="w-4 h-4 text-amber-400" />
                <span className="text-xs font-bold text-slate-200 uppercase tracking-wider font-mono">
                  Waffen & Vorhaltepunkte (Pips)
                </span>
              </div>
              <PipsAnalyzerBadge pipsResult={ship.pipsResult} />
            </div>

            {ship.pipsResult && (
              <div className="pt-2 border-t border-[#122236] text-xs text-slate-300 space-y-1">
                <div className="flex items-center justify-between text-[11px] font-mono">
                  <span className="text-slate-400">Geschwindigkeit:</span>
                  <span className="text-cyan-300 font-bold">
                    {Array.isArray(ship.pipsResult.speedsMps) && ship.pipsResult.speedsMps.length > 0
                      ? ship.pipsResult.speedsMps.join(' m/s, ') + ' m/s'
                      : '—'}
                  </span>
                </div>
                {ship.pipsResult.advice && (
                  <p className="text-[11px] text-slate-400 italic pt-1">
                    💡 {ship.pipsResult.advice}
                  </p>
                )}
              </div>
            )}
          </div>

          {/* Components Grid */}
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <div>
                <span className="text-xs font-bold text-slate-200 uppercase tracking-wider font-mono">
                  Installierte Ausrüstung ({components.length})
                </span>
                <span className="text-[10px] text-slate-400 block mt-0.5">
                  Klicke auf eine Komponente für Spezifikationen & Händlerstandorte im SCWiki
                </span>
              </div>
              {isCustomScanned && (
                <span className="text-[10px] font-mono text-emerald-400 flex items-center gap-1">
                  <CheckCircle2 className="w-3 h-3" /> Aus Spiel-Screenshot verifiziert
                </span>
              )}
            </div>

            {components.length === 0 ? (
              <div className="p-4 rounded-lg bg-[#071322] border border-[#142A45] text-center text-xs text-slate-500">
                Keine Komponenten erfasst. Nutzen Sie "Screenshot scannen", um Ihre Ausrüstung automatisch aus dem Spiel zu erfassen.
              </div>
            ) : (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                {components.map((c, idx) => {
                  const cleanName = cleanComponentName(c.componentName);
                  return (
                    <div
                      key={`${c.slotType}-${idx}`}
                      onClick={() => handleOpenComponentWiki(c.componentName)}
                      className={`group p-2.5 rounded-lg border flex items-center justify-between gap-2.5 transition cursor-pointer hover:border-cyan-400/60 hover:bg-cyan-950/20 hover:shadow-[0_0_12px_rgba(6,182,212,0.15)] ${getSlotColor(c.slotType)}`}
                      title={`"${cleanName}" anklicken für SCWiki-Dossier (Werte & Händler-Standorte)`}
                    >
                      <div className="flex items-center gap-2.5 min-w-0 flex-1">
                        <div className="p-1.5 rounded bg-black/40 shrink-0 group-hover:bg-cyan-950/60 group-hover:text-cyan-300 transition">
                          {getSlotIcon(c.slotType)}
                        </div>
                        <div className="min-w-0 flex-1">
                          <span className="text-[10px] font-mono text-slate-400 uppercase tracking-wide block truncate">
                            {c.slotLabel || c.slotType}
                          </span>
                          <span className="text-xs font-bold text-slate-100 truncate block group-hover:text-cyan-200 transition">
                            {c.componentName}
                          </span>
                        </div>
                      </div>

                      {/* Quick Action Buttons */}
                      <div className="flex items-center gap-1 shrink-0 opacity-80 group-hover:opacity-100 transition">
                        <button
                          onClick={(e) => handleOpenComponentWiki(c.componentName, e)}
                          className="p-1 rounded bg-black/40 hover:bg-cyan-900/70 text-slate-400 hover:text-cyan-300 border border-transparent hover:border-cyan-500/40 transition cursor-pointer"
                          title={`SCWiki-Dossier für "${cleanName}" öffnen`}
                        >
                          <BookOpen className="w-3.5 h-3.5" />
                        </button>

                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            if (cleanName) {
                              bridge.openExternalUrl(`https://star-citizen.wiki/${encodeURIComponent(cleanName)}`);
                            }
                          }}
                          className="p-1 rounded bg-black/40 hover:bg-blue-900/70 text-slate-400 hover:text-blue-300 border border-transparent hover:border-blue-500/40 transition cursor-pointer"
                          title={`"${cleanName}" im Star Citizen Wiki Browser aufrufen`}
                        >
                          <ExternalLink className="w-3.5 h-3.5" />
                        </button>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          {/* Feedback message banner */}
          {feedback && (
            <div className="p-2.5 rounded-lg bg-cyan-950/70 border border-cyan-700/60 text-cyan-200 text-xs font-mono animate-fade-in flex items-center justify-between">
              <span>{feedback}</span>
            </div>
          )}
        </div>

        {/* Modal Footer */}
        <div className="px-5 py-3 border-t border-[#142A45] flex flex-wrap items-center justify-between gap-2 bg-[#071322]">
          <div className="flex items-center gap-2">
            <button
              onClick={handleScanScreenshot}
              disabled={isScanning}
              className="px-3 py-1.5 rounded-md bg-emerald-950/80 hover:bg-emerald-900 border border-emerald-700 text-emerald-300 text-xs font-semibold flex items-center gap-1.5 transition cursor-pointer disabled:opacity-50"
              title="Aktuellen Star Citizen Screenshot nach Schiffsausrüstung scannen"
            >
              <Camera className={`w-3.5 h-3.5 ${isScanning ? 'animate-spin text-amber-400' : 'text-emerald-400'}`} />
              <span>{isScanning ? 'Scanne Screenshot...' : 'Screenshot scannen'}</span>
            </button>

            {isCustomScanned && (
              <button
                onClick={handleResetStock}
                disabled={isResetting}
                className="px-3 py-1.5 rounded-md bg-slate-900 hover:bg-slate-800 border border-slate-700 text-slate-300 text-xs font-semibold flex items-center gap-1.5 transition cursor-pointer disabled:opacity-50"
                title="Ausrüstung auf Star Citizen Standard-Werkskomponenten zurücksetzen"
              >
                <RotateCcw className={`w-3.5 h-3.5 ${isResetting ? 'animate-spin text-amber-400' : 'text-slate-400'}`} />
                <span>Auf Werkszustand</span>
              </button>
            )}
          </div>

          <button
            onClick={onClose}
            className="px-4 py-1.5 rounded-md bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-bold transition cursor-pointer"
          >
            Schließen
          </button>
        </div>
      </div>
    </div>
  );
};
