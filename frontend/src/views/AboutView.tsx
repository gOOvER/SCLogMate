import React from 'react';
import {
  Sparkles,
  ExternalLink,
  Heart,
  Coffee,
  Code2,
  Shield,
  Bot,
  Globe,
} from 'lucide-react';

export const AboutView: React.FC = () => {
  return (
    <div className="p-6 space-y-6 max-w-5xl mx-auto">
      {/* 1. Hero Brand Header */}
      <div className="p-6 rounded-2xl bg-gradient-to-r from-slate-900/95 via-slate-900/80 to-slate-950/90 border border-slate-800 shadow-2xl relative overflow-hidden">
        <div className="absolute top-0 right-0 -mt-8 -mr-8 w-64 h-64 bg-sky-500/10 rounded-full blur-3xl pointer-events-none" />
        
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-6 relative z-10">
          <div className="flex items-center space-x-5">
            <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-sky-500 to-blue-600 flex items-center justify-center text-white shadow-xl shadow-sky-500/20 font-black text-2xl tracking-tighter">
              ✦
            </div>
            <div>
              <div className="flex items-center space-x-3">
                <h1 className="text-2xl font-black text-white tracking-wider">SCLogMate</h1>
                <span className="px-2.5 py-0.5 text-xs font-bold rounded-full bg-sky-950 text-sky-400 border border-sky-800">
                  v1.3.1 Photino Edition
                </span>
                <span className="px-2.5 py-0.5 text-xs font-bold rounded-full bg-emerald-950 text-emerald-400 border border-emerald-800">
                  LIVE COMPANION
                </span>
              </div>
              <p className="text-sm text-slate-400 mt-1">
                Star Citizen Live Log Companion & Hochleistungs-Telemetrie Suite
              </p>
              <div className="text-xs text-slate-500 font-mono mt-1">
                .NET 10 · Photino.NET · React 19 · Tailwind CSS v4 · SQLite
              </div>
            </div>
          </div>

          <div className="flex items-center space-x-3">
            <a
              href="https://github.com/gOOvER/SCLogMate"
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center space-x-2 px-4 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold border border-slate-700 transition"
            >
              <svg className="w-4 h-4 fill-current" viewBox="0 0 24 24">
                <path d="M12 0C5.37 0 0 5.37 0 12c0 5.31 3.435 9.795 8.205 11.385.6.105.825-.255.825-.57 0-.285-.015-1.23-.015-2.235-3.015.555-3.795-.735-4.035-1.41-.135-.345-.72-1.41-1.23-1.695-.42-.225-1.02-.78-.015-.795.945-.015 1.62.87 1.845 1.23 1.08 1.815 2.805 1.305 3.495.99.105-.78.42-1.305.765-1.605-2.67-.3-5.46-1.335-5.46-5.925 0-1.305.465-2.385 1.23-3.225-.12-.3-.54-1.53.12-3.18 0 0 1.005-.315 3.3 1.23.96-.27 1.98-.405 3-.405s2.04.135 3 .405c2.295-1.56 3.3-1.23 3.3-1.23.66 1.65.24 2.88.12 3.18.765.84 1.23 1.905 1.23 3.225 0 4.605-2.805 5.625-5.475 5.925.435.375.81 1.095.81 2.22 0 1.605-.015 2.895-.015 3.3 0 .315.225.69.825.57A12.02 12.02 0 0024 12c0-6.63-5.37-12-12-12z" />
              </svg>
              <span>GitHub</span>
              <ExternalLink className="w-3 h-3 text-slate-400" />
            </a>
            <a
              href="https://ko-fi.com/goover"
              target="_blank"
              rel="noopener noreferrer"
              className="flex items-center space-x-2 px-4 py-2.5 rounded-xl bg-rose-600 hover:bg-rose-500 text-white text-xs font-bold shadow-lg shadow-rose-600/25 border border-rose-400 transition"
            >
              <Coffee className="w-4 h-4" />
              <span>Kaffee spendieren</span>
            </a>
          </div>
        </div>
      </div>

      {/* 2. Ko-fi Community Support Card */}
      <div className="p-5 rounded-xl bg-gradient-to-r from-rose-950/30 via-slate-900/60 to-slate-900/40 border border-rose-900/40 flex items-center justify-between gap-4">
        <div className="flex items-center space-x-4">
          <div className="w-12 h-12 rounded-xl bg-rose-500/10 border border-rose-500/30 flex items-center justify-center text-rose-400 shrink-0">
            <Heart className="w-6 h-6 fill-rose-500/20 text-rose-400" />
          </div>
          <div>
            <div className="flex items-center space-x-2">
              <h3 className="text-sm font-bold text-white">Unterstütze die Weiterentwicklung</h3>
              <span className="text-[10px] px-2 py-0.5 rounded bg-rose-950 text-rose-400 border border-rose-800 font-bold">
                COMMUNITY DRIVEN
              </span>
            </div>
            <p className="text-xs text-slate-400 mt-0.5">
              SCLogMate ist ein 100% freies und quelloffenes Community-Projekt für Star Citizen Piloten.
              Wenn dir das Tool gefällt und deine Flüge erleichtert, freuen wir uns über jede Unterstützung!
            </p>
          </div>
        </div>

        <a
          href="https://ko-fi.com/goover"
          target="_blank"
          rel="noopener noreferrer"
          className="px-4 py-2 rounded-lg bg-rose-500/20 hover:bg-rose-500/30 text-rose-300 text-xs font-semibold border border-rose-500/40 transition shrink-0"
        >
          Spenden via Ko-fi ☕
        </a>
      </div>

      {/* 3. SCVerse Ökosystem */}
      <div className="space-y-3">
        <div className="flex items-center space-x-2 text-sky-400 font-bold text-xs tracking-wider uppercase">
          <Sparkles className="w-4 h-4" />
          <span>Das SCVerse Ökosystem</span>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {/* Card 1: SCLogMate */}
          <div className="p-4 rounded-xl bg-slate-900/60 border border-sky-500/30 space-y-2">
            <div className="flex items-center space-x-2 text-sky-400">
              <Shield className="w-5 h-5" />
              <h4 className="text-xs font-bold text-white">SCLogMate Desktop</h4>
            </div>
            <p className="text-xs text-slate-400 leading-relaxed">
              Lokaler Live-Companion mit Game.log Parser, OCR Kontostand-Erkennung, Offline-Starmap und Hardware-Optimierer.
            </p>
            <div className="text-[10px] font-mono text-sky-300 font-semibold pt-1">
              Photino.NET + React UI
            </div>
          </div>

          {/* Card 2: MobiNexus */}
          <div className="p-4 rounded-xl bg-slate-900/60 border border-purple-500/30 space-y-2">
            <div className="flex items-center space-x-2 text-purple-400">
              <Bot className="w-5 h-5" />
              <h4 className="text-xs font-bold text-white">MobiNexus Bot</h4>
            </div>
            <p className="text-xs text-slate-400 leading-relaxed">
              Leistungsstarker Discord-Bot für Flottenorganisation, Event-Planung, Verifizierung und automatische Synchronisation.
            </p>
            <div className="text-[10px] font-mono text-purple-300 font-semibold pt-1">
              Sapphire Framework / Node 24
            </div>
          </div>

          {/* Card 3: SCVerse Web Portal */}
          <div className="p-4 rounded-xl bg-slate-900/60 border border-emerald-500/30 space-y-2">
            <div className="flex items-center space-x-2 text-emerald-400">
              <Globe className="w-5 h-5" />
              <h4 className="text-xs font-bold text-white">SCVerse Website</h4>
            </div>
            <p className="text-xs text-slate-400 leading-relaxed">
              Zentrales Community-Portal mit interaktiver Starmap, Handelsrouten-Rechner, Bauplan-Archiv und Flottenübersicht.
            </p>
            <div className="text-[10px] font-mono text-emerald-300 font-semibold pt-1">
              Next.js / Tailwind CSS
            </div>
          </div>
        </div>
      </div>

      {/* 4. Tech-Stack & Architektur */}
      <div className="p-5 rounded-xl bg-slate-900/60 border border-slate-800 space-y-4">
        <div className="flex items-center space-x-2 text-white font-bold text-xs tracking-wider uppercase">
          <Code2 className="w-4 h-4 text-sky-400" />
          <span>Technologie & Frameworks</span>
        </div>

        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 text-xs">
          <div className="p-3 rounded-lg bg-slate-950 border border-slate-800/80">
            <div className="text-slate-400">Backend Core</div>
            <div className="font-bold text-white mt-1">.NET 10 / C#</div>
          </div>
          <div className="p-3 rounded-lg bg-slate-950 border border-slate-800/80">
            <div className="text-slate-400">Desktop Webview</div>
            <div className="font-bold text-sky-400 mt-1">Photino.NET</div>
          </div>
          <div className="p-3 rounded-lg bg-slate-950 border border-slate-800/80">
            <div className="text-slate-400">Frontend UI</div>
            <div className="font-bold text-emerald-400 mt-1">React 19 + Vite 6</div>
          </div>
          <div className="p-3 rounded-lg bg-slate-950 border border-slate-800/80">
            <div className="text-slate-400">Design System</div>
            <div className="font-bold text-purple-400 mt-1">Tailwind CSS v4</div>
          </div>
          <div className="p-3 rounded-lg bg-slate-950 border border-slate-800/80">
            <div className="text-slate-400">Datenbank</div>
            <div className="font-bold text-white mt-1">SQLite 3 (v17 Schema)</div>
          </div>
          <div className="p-3 rounded-lg bg-slate-950 border border-slate-800/80">
            <div className="text-slate-400">Optische OCR</div>
            <div className="font-bold text-amber-400 mt-1">Tesseract 5 + Win32 GDI</div>
          </div>
          <div className="p-3 rounded-lg bg-slate-950 border border-slate-800/80">
            <div className="text-slate-400">Sprachausgabe</div>
            <div className="font-bold text-rose-400 mt-1">Aurora / System.Speech</div>
          </div>
          <div className="p-3 rounded-lg bg-slate-950 border border-slate-800/80">
            <div className="text-slate-400">Lizenz</div>
            <div className="font-bold text-white mt-1">GPL-3.0 / Freie Software</div>
          </div>
        </div>
      </div>

      {/* 5. CIG Community Disclaimer */}
      <div className="p-4 rounded-xl bg-slate-950 border border-slate-800/60 text-[11px] text-slate-500 leading-relaxed space-y-2">
        <p className="font-semibold text-slate-400">
          Rechtlicher Hinweis / Roberts Space Industries Community Disclaimer:
        </p>
        <p>
          Dieses Projekt ist kein offizielles Produkt von Cloud Imperium Games oder Roberts Space Industries.
          Star Citizen®, Squadron 42®, Roberts Space Industries® und Cloud Imperium Games® sind eingetragene Warenzeichen
          der Cloud Imperium Rights LLC. Alle Spielinhalte, Grafiken und Bezeichnungen sind geistiges Eigentum der jeweiligen Rechteinhaber.
        </p>
      </div>
    </div>
  );
};
