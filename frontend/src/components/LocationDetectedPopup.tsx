import React, { useState, useEffect, useRef } from 'react';
import { Compass, Crosshair, Copy, Check, X, BookmarkCheck, ExternalLink, Save, Edit2 } from 'lucide-react';
import { bridge, CopiedLocationReading, UserPoiDto } from '../services/photinoBridge';
import { useI18n } from '../i18n';
import { NavTabId } from './Sidebar';
import { NavTargetContext } from '../App';

interface LocationDetectedPopupProps {
  onNavigate?: (tab: NavTabId, context?: NavTargetContext) => void;
  onPoiSaved?: () => void;
}

const POI_CATEGORIES = [
  { id: 'Mining', label: '⛏️ Mining & Vorkommen', color: '#F59E0B' },
  { id: 'Salvage', label: '🧲 Salvage & Wracks', color: '#10B981' },
  { id: 'Bunker', label: '🛡️ Bunker & Außenposten', color: '#EF4444' },
  { id: 'Secret', label: '🤫 Geheim & Drogenlabore', color: '#8B5CF6' },
  { id: 'Trade', label: '📦 Handel & Terminals', color: '#06B6D4' },
  { id: 'Misc', label: '📍 Sonstige Wegpunkte', color: '#94A3B8' },
];

export const LocationDetectedPopup: React.FC<LocationDetectedPopupProps> = ({ onNavigate, onPoiSaved }) => {
  const { locale } = useI18n();
  const [reading, setReading] = useState<CopiedLocationReading | null>(null);
  const [isVisible, setIsVisible] = useState<boolean>(false);
  const [isCopied, setIsCopied] = useState<boolean>(false);
  const [isFormOpen, setIsFormOpen] = useState<boolean>(false);
  const [isPaused, setIsPaused] = useState<boolean>(false);
  const [timeLeft, setTimeLeft] = useState<number>(15);
  const [isSaving, setIsSaving] = useState<boolean>(false);
  const [savedSuccessName, setSavedSuccessName] = useState<string | null>(null);

  // Form State
  const [poiName, setPoiName] = useState<string>('');
  const [poiCategory, setPoiCategory] = useState<string>('Misc');
  const [poiBody, setPoiBody] = useState<string>('');
  const [poiNotes, setPoiNotes] = useState<string>('');

  const nameInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const unsub = bridge.on<CopiedLocationReading>('LOCATION_COPIED', (data: CopiedLocationReading) => {
      if (!data) return;
      setReading(data);
      setIsVisible(true);
      setIsFormOpen(false);
      setIsCopied(false);
      setSavedSuccessName(null);
      setTimeLeft(15);

      // Pre-fill defaults based on saved POI or nearest POI if available
      const nearest = data.nearestPois && data.nearestPois.length > 0 ? data.nearestPois[0] : null;
      setPoiBody(nearest?.body || '');
      setPoiCategory('Misc');
      setPoiName(data.savedPoiName || `GPS ${new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' })}`);
      setPoiNotes('Automatisch via /showlocation erfasst');
    });

    return () => {
      unsub();
    };
  }, []);

  // Countdown timer for auto-dismiss (only active when form is not open and not hovered)
  useEffect(() => {
    if (!isVisible || isFormOpen || isPaused || savedSuccessName) return;

    const timer = setInterval(() => {
      setTimeLeft((prev) => {
        if (prev <= 1) {
          setIsVisible(false);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(timer);
  }, [isVisible, isFormOpen, isPaused, savedSuccessName]);

  // Focus name input when form opens
  useEffect(() => {
    if (isFormOpen) {
      setTimeout(() => {
        nameInputRef.current?.focus();
      }, 80);
    }
  }, [isFormOpen]);

  if (!isVisible || !reading) return null;

  const handleCopyCoords = () => {
    const text = `Coordinates: x:${reading.x} y:${reading.y} z:${reading.z}`;
    navigator.clipboard.writeText(text).then(() => {
      setIsCopied(true);
      setTimeout(() => setIsCopied(false), 2500);
    });
  };

  const handleOpenForm = () => {
    setIsFormOpen(true);
  };

  const handleClose = () => {
    setIsVisible(false);
    setIsFormOpen(false);
    setSavedSuccessName(null);
  };

  const handleSavePoi = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!poiName.trim() || isSaving) return;

    setIsSaving(true);
    const catObj = POI_CATEGORIES.find((c) => c.id === poiCategory) || POI_CATEGORIES[0];

    const payload: Partial<UserPoiDto> = {
      name: poiName.trim(),
      system: reading.detectedSystem || 'Stanton',
      body: poiBody.trim(),
      category: poiCategory,
      notes: poiNotes.trim(),
      posX: reading.x,
      posY: reading.y,
      posZ: reading.z,
      color: catObj.color,
    };

    try {
      await bridge.sendRequest('save_user_poi', payload);
      setSavedSuccessName(poiName.trim());
      setIsSaving(false);
      setIsFormOpen(false);

      // Trigger global event so other views refresh POIs instantly
      window.dispatchEvent(new CustomEvent('user-poi-saved', { detail: payload }));
      if (onPoiSaved) onPoiSaved();

      // Close popup after brief success display
      setTimeout(() => {
        handleClose();
      }, 1800);
    } catch (err) {
      console.error('Failed to save POI from popup:', err);
      setIsSaving(false);
    }
  };

  const nearestPoi = reading.nearestPois && reading.nearestPois.length > 0 ? reading.nearestPois[0] : null;

  return (
    <div
      onMouseEnter={() => setIsPaused(true)}
      onMouseLeave={() => setIsPaused(false)}
      style={{ position: 'fixed', bottom: '2.5rem', right: '1.5rem', zIndex: 9999 }}
      className="w-[390px] max-w-[calc(100vw-3rem)] rounded-xl border border-amber-500/50 bg-[#030712]/95 backdrop-blur-md shadow-[0_0_35px_rgba(245,158,11,0.3)] text-slate-200 font-sans overflow-hidden animate-in slide-in-from-bottom-5 fade-in duration-300"
    >
      {/* Top Header Bar */}
      <div className="flex items-center justify-between px-3.5 py-2.5 bg-gradient-to-r from-amber-950/80 via-slate-900 to-amber-950/40 border-b border-amber-500/30">
        <div className="flex items-center gap-2 min-w-0">
          <div className="p-1 rounded bg-amber-500/20 text-amber-400 border border-amber-500/40 animate-pulse">
            <Compass className="w-4 h-4" />
          </div>
          <span className="text-xs font-mono font-bold uppercase tracking-wider text-amber-300 truncate">
            {locale === 'en' ? 'GPS Waypoint Added' : 'GPS-Wegpunkt hinzugefügt'}
          </span>
          <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-cyan-950/80 text-cyan-300 border border-cyan-800">
            /showlocation
          </span>
        </div>

        <button
          onClick={handleClose}
          className="p-1 rounded text-slate-400 hover:text-white hover:bg-slate-800/60 transition cursor-pointer"
          title={locale === 'en' ? 'Close' : 'Schließen'}
        >
          <X className="w-4 h-4" />
        </button>
      </div>

      {/* Card Content */}
      <div className="p-3.5 space-y-3 text-xs">
        {/* Success Message Banner */}
        {savedSuccessName ? (
          <div className="p-3 rounded-lg bg-emerald-950/60 border border-emerald-500/50 text-emerald-300 flex items-center gap-2 font-mono text-xs animate-in zoom-in-95">
            <BookmarkCheck className="w-5 h-5 text-emerald-400 shrink-0" />
            <div>
              <div className="font-bold">{locale === 'en' ? 'Waypoint Updated!' : 'Wegpunkt aktualisiert!'}</div>
              <div className="text-[11px] text-emerald-200/80">"{savedSuccessName}"</div>
            </div>
          </div>
        ) : (
          <>
            {/* Saved POI Name Badge */}
            <div className="flex items-center justify-between text-xs px-2.5 py-1.5 rounded bg-cyan-950/40 border border-cyan-800/60 font-mono">
              <span className="text-slate-400">{locale === 'en' ? 'Pinned as:' : 'Gespeichert als:'}</span>
              <span className="font-bold text-cyan-300 truncate max-w-[240px]">
                {reading.savedPoiName || `GPS ${reading.x.toFixed(0)}`}
              </span>
            </div>

            {/* System & Nearest POI */}
            <div className="flex items-center justify-between text-xs">
              <div className="flex items-center gap-1.5 font-mono">
                <span className="text-slate-400">{locale === 'en' ? 'System:' : 'Sternensystem:'}</span>
                <span
                  className={`font-bold px-2 py-0.5 rounded border text-[11px] ${
                    reading.detectedSystem?.toLowerCase() === 'pyro'
                      ? 'bg-rose-950/60 text-rose-300 border-rose-700/60'
                      : reading.detectedSystem?.toLowerCase() === 'nyx'
                      ? 'bg-purple-950/60 text-purple-300 border-purple-700/60'
                      : 'bg-amber-950/60 text-amber-300 border-amber-700/60'
                  }`}
                >
                  {reading.detectedSystem || 'Stanton'}
                </span>
              </div>

              {nearestPoi && (
                <div
                  className="flex items-center gap-1 text-[11px] font-mono text-emerald-400 truncate max-w-[200px]"
                  title={`${nearestPoi.name} (${nearestPoi.formattedDistance})`}
                >
                  <Crosshair className="w-3 h-3 shrink-0" />
                  <span className="truncate">{nearestPoi.name}</span>
                  <span className="text-slate-400 text-[10px]">({nearestPoi.formattedDistance})</span>
                </div>
              )}
            </div>

            {/* Coordinates Box */}
            <div className="p-2 rounded bg-[#02050e] border border-cyan-950/80 font-mono text-[11px] grid grid-cols-3 gap-2">
              <div>
                <span className="text-slate-500 text-[9px] block">X-ACHSE</span>
                <span className="text-cyan-300 font-semibold">{reading.x.toLocaleString('de-DE')}</span>
              </div>
              <div>
                <span className="text-slate-500 text-[9px] block">Y-ACHSE</span>
                <span className="text-cyan-300 font-semibold">{reading.y.toLocaleString('de-DE')}</span>
              </div>
              <div>
                <span className="text-slate-500 text-[9px] block">Z-ACHSE</span>
                <span className="text-cyan-300 font-semibold">{reading.z.toLocaleString('de-DE')}</span>
              </div>
            </div>

            {/* Inline Quick-Pin Form */}
            {isFormOpen ? (
              <form onSubmit={handleSavePoi} className="space-y-2.5 pt-1 border-t border-slate-800 animate-in fade-in">
                <div>
                  <label className="block text-[10px] font-mono text-slate-400 uppercase tracking-wider mb-1">
                    {locale === 'en' ? 'POI Name *' : 'POI Name / Bezeichnung *'}
                  </label>
                  <input
                    ref={nameInputRef}
                    type="text"
                    required
                    value={poiName}
                    onChange={(e) => setPoiName(e.target.value)}
                    placeholder={locale === 'en' ? 'e.g. Quantainium Hotspot, Bunker Entry...' : 'z.B. Quantainium Fundort, Bunker Eingang...'}
                    className="w-full bg-slate-900 border border-slate-700 rounded px-2.5 py-1.5 text-xs text-slate-100 placeholder-slate-500 focus:outline-none focus:border-amber-400 font-mono"
                  />
                </div>

                <div className="grid grid-cols-2 gap-2">
                  <div>
                    <label className="block text-[10px] font-mono text-slate-400 uppercase tracking-wider mb-1">
                      {locale === 'en' ? 'Category' : 'Kategorie'}
                    </label>
                    <select
                      value={poiCategory}
                      onChange={(e) => setPoiCategory(e.target.value)}
                      className="w-full bg-slate-900 border border-slate-700 rounded px-2 py-1.5 text-xs text-slate-200 focus:outline-none focus:border-amber-400 font-mono"
                    >
                      {POI_CATEGORIES.map((cat) => (
                        <option key={cat.id} value={cat.id}>
                          {cat.label}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label className="block text-[10px] font-mono text-slate-400 uppercase tracking-wider mb-1">
                      {locale === 'en' ? 'Body / Moon' : 'Himmelskörper / Mond'}
                    </label>
                    <input
                      type="text"
                      value={poiBody}
                      onChange={(e) => setPoiBody(e.target.value)}
                      placeholder={locale === 'en' ? 'e.g. Daymar, Lyria...' : 'z.B. Daymar, Lyria...'}
                      className="w-full bg-slate-900 border border-slate-700 rounded px-2 py-1.5 text-xs text-slate-100 placeholder-slate-500 focus:outline-none focus:border-amber-400 font-mono"
                    />
                  </div>
                </div>

                <div>
                  <input
                    type="text"
                    value={poiNotes}
                    onChange={(e) => setPoiNotes(e.target.value)}
                    placeholder={locale === 'en' ? 'Notes / Details (optional)...' : 'Notizen / Details (optional)...'}
                    className="w-full bg-slate-900 border border-slate-700 rounded px-2.5 py-1 text-[11px] text-slate-300 placeholder-slate-500 focus:outline-none focus:border-amber-400 font-mono"
                  />
                </div>

                <div className="flex items-center justify-end gap-2 pt-1">
                  <button
                    type="button"
                    onClick={() => setIsFormOpen(false)}
                    className="px-3 py-1 rounded bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-mono transition cursor-pointer"
                  >
                    {locale === 'en' ? 'Cancel' : 'Abbrechen'}
                  </button>
                  <button
                    type="submit"
                    disabled={isSaving || !poiName.trim()}
                    className="flex items-center gap-1.5 px-3 py-1 rounded bg-amber-600 hover:bg-amber-500 disabled:opacity-50 text-white font-mono font-semibold text-xs transition cursor-pointer shadow-[0_0_10px_rgba(245,158,11,0.3)]"
                  >
                    <Save className="w-3.5 h-3.5" />
                    {isSaving ? (locale === 'en' ? 'Saving...' : 'Speichert...') : (locale === 'en' ? 'Update POI' : 'Aktualisieren')}
                  </button>
                </div>
              </form>
            ) : (
              /* Action Buttons (Collapsed View) */
              <div className="flex items-center justify-between gap-2 pt-1 border-t border-slate-800/80">
                <button
                  onClick={handleOpenForm}
                  className="flex items-center gap-1.5 px-3 py-1.5 rounded bg-gradient-to-r from-amber-600 to-amber-500 hover:from-amber-500 hover:to-amber-400 text-slate-950 font-mono font-bold text-xs transition cursor-pointer shadow-[0_0_14px_rgba(245,158,11,0.35)] shrink-0"
                >
                  <Edit2 className="w-3.5 h-3.5" />
                  {locale === 'en' ? 'Rename / Edit' : 'Umbenennen / Anpassen'}
                </button>

                <div className="flex items-center gap-1.5">
                  <button
                    onClick={handleCopyCoords}
                    className="flex items-center gap-1 px-2.5 py-1.5 rounded bg-slate-900 border border-slate-700 hover:border-cyan-500/40 text-slate-300 hover:text-cyan-300 font-mono text-xs transition cursor-pointer"
                    title={locale === 'en' ? 'Copy coordinates text' : 'Koordinaten als Text kopieren'}
                  >
                    {isCopied ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
                    <span>{isCopied ? (locale === 'en' ? 'Copied' : 'Kopiert') : (locale === 'en' ? 'Copy' : 'Kopieren')}</span>
                  </button>

                  {onNavigate && (
                    <button
                      onClick={() => {
                        onNavigate('places');
                        handleClose();
                      }}
                      className="p-1.5 rounded bg-slate-900 border border-slate-700 hover:border-cyan-500/40 text-slate-400 hover:text-cyan-300 transition cursor-pointer"
                      title={locale === 'en' ? 'Open in Places & Starmap' : 'In Orte & Starmap öffnen'}
                    >
                      <ExternalLink className="w-3.5 h-3.5" />
                    </button>
                  )}
                </div>
              </div>
            )}
          </>
        )}
      </div>

      {/* Auto-Dismiss Progress Bar (Bottom) */}
      {!isFormOpen && !savedSuccessName && (
        <div className="w-full bg-slate-950 h-1 overflow-hidden">
          <div
            className="h-full bg-gradient-to-r from-amber-500 to-cyan-400 transition-all duration-1000 ease-linear"
            style={{ width: `${(timeLeft / 15) * 100}%` }}
          />
        </div>
      )}
    </div>
  );
};
