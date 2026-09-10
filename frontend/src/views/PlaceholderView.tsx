import React from 'react';
import { NavTabId } from '../components/Sidebar';
import {
  Award,
  Compass,
  FileCode2,
  Info,
  MapPin,
  Pickaxe,
  Radar,
  Settings,
  Shield,
  ShoppingBag,
  Target,
  Wrench,
} from 'lucide-react';

interface PlaceholderViewProps {
  tab: NavTabId;
}

export const PlaceholderView: React.FC<PlaceholderViewProps> = ({ tab }) => {
  const meta: Record<
    string,
    { title: string; subtitle: string; icon: React.ElementType; phase: string; details: string[] }
  > = {
    missions: {
      title: 'Aufträge & Missions-Katalog',
      subtitle: 'Historische Missionsabschlüsse, OCR-Auftragsliste & CIG-Vertragskatalog',
      icon: Target,
      phase: 'Phase 2',
      details: [
        'Anzeige aller angenommenen, abgeschlossenen und fehlgeschlagenen Aufträge',
        'Live OCR-Scanning des Contract Managers',
        'Übersicht über Missionsketten und Fraktions-Belohnungen',
      ],
    },
    reputation: {
      title: 'Ansehen & Fraktionen (Reputation)',
      subtitle: 'Fortschrittsbalken und Ränge bei allen Organisationen des Stanton- & Pyro-Systems',
      icon: Award,
      phase: 'Phase 2',
      details: [
        'Bounty Hunters Guild, Crusader Security, Hurston Dynamics, MicroTech Protection',
        'Affinitätsstufen und Freischaltungen für höher dotierte Aufträge',
        'Historische XP-Entwicklung pro Fraktion',
      ],
    },
    blueprints: {
      title: 'Baupläne (Crafting & Blueprints)',
      subtitle: 'Verzeichnis aller erlernten Baupläne, Komponenten und Fertigungsrezepte',
      icon: FileCode2,
      phase: 'Phase 2',
      details: [
        'Automatische Erfassung aus dem Log beim Erlernen eines Rezepts',
        'Auflistung benötigter Materialien und Erze (Janalite, Bexalite, Gold, etc.)',
        'Filterung nach Waffen, Komponenten und Ausrüstung',
      ],
    },
    loadout: {
      title: 'Ausrüstung & Loadout',
      subtitle: 'Angelegte Rüstungen, Waffen, Multi-Tools und Schiffskomponenten',
      icon: Shield,
      phase: 'Phase 2',
      details: [
        'Protokollierung von Ausrüstungswechseln und Kitting-Vorgängen',
        'Übersicht über verlorene Ausrüstung nach Med-Bett-Respawn',
        'Schiffskomponenten-Tracking',
      ],
    },
    starmap: {
      title: 'Interaktive Sternenkarte (Starmap)',
      subtitle: 'Navigationskarte von Stanton & Pyro mit Sprungpunkten, Monden und Stationen',
      icon: Radar,
      phase: 'Phase 3',
      details: [
        'Interaktive 2D/3D-Darstellung der Himmelskörper und Lagrange-Punkte',
        'Distanz- & Quantum-Reisezeitberechnung',
        'Eigene POI-Markierungen und Koordinaten',
      ],
    },
    places: {
      title: 'Orte & Stationen',
      subtitle: 'Besuchsstatistik aller Städte, Raumstationen, Außenposten und Höhlen',
      icon: Compass,
      phase: 'Phase 3',
      details: [
        'Verweildauer und Aufenthaltszeiten pro Planet und Raumhafen',
        'Landungs- und Abflug-Historie',
        'Eigene Notizen zu Standorten und Ladezonen',
      ],
    },
    blackbox: {
      title: 'Flugschreiber (BlackBox)',
      subtitle: 'Detaillierte Telemetrie, Quantum-Timeline und Zerstörungsanalysen',
      icon: MapPin,
      phase: 'Phase 3',
      details: [
        'Genaue Chronik von QT-Sprüngen (Start, Interdiction, Ankunft)',
        'Sitz- und Einsteigevorgänge des Piloten',
        'Havarie- und Schadensberichte vor Schiffsverlust',
      ],
    },
    orescanner: {
      title: 'RS Signal Scanner & Gesteinsanalyse',
      subtitle: 'RS-Signaturen-Entschlüsselung und Gesteins-Zusammensetzungsrechner',
      icon: Pickaxe,
      phase: 'Phase 3',
      details: [
        'Live-Overlay oder In-App-Berechnung von Quantanium, Gold, Beryll & Taranit',
        'Optimaler Abbau- und Laserstärkenguide',
        'Signatur-Datenbank für schnelle Gesteinsidentifikation',
      ],
    },
    market: {
      title: 'Handelsmarkt (UEX Corp Integration)',
      subtitle: 'Live-Rohstoffpreise, lukrativste Handelsrouten und Terminal-Kapazitäten',
      icon: ShoppingBag,
      phase: 'Phase 3',
      details: [
        'Direkte Anbindung an die offizielle UEX Corp API',
        'Gewinnberechnung unter Berücksichtigung von Schiffsfrachtraum (SCU)',
        'Echtzeit-Meldungen über Preisfluktuationen und Nachfrage',
      ],
    },
    tools: {
      title: 'Werkzeuge & Star Citizen Wartung',
      subtitle: 'Shader-Cache Löschung, USER-Ordner Bereinigung und Keybind-Backups',
      icon: Wrench,
      phase: 'Phase 3',
      details: [
        '1-Klick-Löschung des Star Citizen Shader-Caches zur Performance-Wiederherstellung',
        'Sicherung und Wiederherstellung der Tastenbelegungen (actionmaps.xml)',
        'Archivierung und Komprimierung alter Logdateien',
      ],
    },
    settings: {
      title: 'SCLogMate Einstellungen',
      subtitle: 'Konfiguration von Pfaden, OCR-Screenreader, Overlays, Wipe-Filter & SQLite DB',
      icon: Settings,
      phase: 'Phase 4',
      details: [
        'Star Citizen Installationsverzeichnis & automatischer Kanal-Wechsel (LIVE/PTU)',
        'Wipe-Stichtag zur flexiblen Filterung historischer Finanz- und Missionsdaten',
        'mobiGlas OCR Kalibrierung und In-Game Achievement Toasts',
        'SQLite Datenbank-Diagnose, Re-Scan und Bereinigung',
      ],
    },
    about: {
      title: 'Über SCLogMate',
      subtitle: 'Star Citizen Live Log Companion & Analytics (Photino.NET Edition)',
      icon: Info,
      phase: 'Info',
      details: [
        'Entwickelt für Star Citizen Live Companion Analytics von gOOvER',
        'Technologie-Stack: .NET 10 (C#) · Photino.NET WebView2 · React 19 · Tailwind CSS v4',
        'Teil des SCVerse Ökosystems (zusammen mit MobiNexus & SCVerseWebsite)',
      ],
    },
  };

  const item = meta[tab] || {
    title: tab,
    subtitle: 'Modul in Vorbereitung',
    icon: Info,
    phase: 'Geplant',
    details: [],
  };
  const Icon = item.icon;

  return (
    <div className="flex items-center justify-center h-full p-8">
      <div className="max-w-xl w-full sc-glass rounded-xl p-8 border border-slate-800 sc-hud-corner text-center relative overflow-hidden">
        <div className="w-16 h-16 rounded-2xl bg-cyan-950/40 border border-cyan-500/40 text-cyan-400 mx-auto flex items-center justify-center mb-5 shadow-[0_0_25px_rgba(0,240,255,0.2)]">
          <Icon className="w-8 h-8" />
        </div>

        <div className="inline-block px-2.5 py-0.5 rounded-full text-[10px] font-mono uppercase tracking-wider bg-cyan-950 border border-cyan-500/30 text-cyan-300 mb-3">
          {item.phase} — In Vorbereitung
        </div>

        <h2 className="text-xl font-bold text-slate-100 tracking-wide">{item.title}</h2>
        <p className="text-xs text-slate-400 mt-1 max-w-md mx-auto">{item.subtitle}</p>

        <div className="mt-6 text-left p-4 rounded-lg bg-slate-900/60 border border-slate-800/80 space-y-2">
          <span className="text-[10px] font-mono uppercase tracking-wider text-slate-500 block">
            Geplante Funktionen:
          </span>
          <ul className="space-y-1.5 text-xs text-slate-300">
            {item.details.map((d, i) => (
              <li key={i} className="flex items-start gap-2">
                <span className="text-cyan-400 font-bold shrink-0">›</span>
                <span>{d}</span>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
};
