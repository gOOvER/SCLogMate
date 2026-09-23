# 🛰️ SCLogMate

[![Release](https://img.shields.io/github/v/release/gOOvER/SCLogMate?style=flat&color=38BDF8&label=Release)](https://github.com/gOOvER/SCLogMate/releases/latest)
[![Build & Release](https://github.com/gOOvER/SCLogMate/actions/workflows/release.yml/badge.svg)](https://github.com/gOOvER/SCLogMate/actions)
[![VirusTotal Clean](https://img.shields.io/badge/VirusTotal-Clean%20(0%2F70)-34D399?logo=virustotal&logoColor=white)](https://www.virustotal.com/gui/search/SCLogMate)
[![Platform](https://img.shields.io/badge/Platform-Windows%20x64-0078D6?logo=windows&logoColor=white)](https://github.com/gOOvER/SCLogMate/releases)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![React 19](https://img.shields.io/badge/React-19.3-61DAFB?logo=react&logoColor=black)](https://react.dev/)
[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL_3.0-blue.svg)](LICENSE)

[🇬🇧 English](#-english) &nbsp;|&nbsp; [🇩🇪 Deutsch](#-deutsch)

---

<a name="-english"></a>
# 🇬🇧 English

**The ultimate Star Citizen Live Log Companion, Industrial Suite & Analytics Assistant.**

**SCLogMate** is a standalone Windows desktop companion designed specifically for **Star Citizen 4.x**. Built upon high-performance .NET 10 and a modern React 19 glassmorphic interface (Photino.NET), SCLogMate continuously monitors and interprets your local `Game.log` in real-time without hooking game memory or violating AntiCheat policies.

From real-time financial ledgers and mobiGlas wallet OCR reconciliation to fleet management, ASOP terminal loadout scanning, mining crack calculators, 3D cargo packing, refinery tracking, and OBS streaming widgets—SCLogMate is the pilot's essential co-pilot for navigating the Verse.

---

> ℹ️ **Fork Notice & Acknowledgments**:  
> **SCLogMate** originated as a heavily modernized and expanded evolution of [**SCLogReader**](https://github.com/miwidot/SCLogReader) by [**miwidot**](https://github.com/miwidot). Sincere thanks to **miwidot** for creating the foundational log-parsing engine and base architecture!

> ⚠️ **Unofficial Community Tool**:  
> Not affiliated with, endorsed by, or authorized by Cloud Imperium Games (CIG) or Roberts Space Industries (RSI). SCLogMate reads **strictly read-only** from your local `Game.log` and Windows screen/clipboard capture APIs—**100% AntiCheat compliant and safe to use**.

---

## ✨ Features & Highlights (English)

### 🖥️ Native In-Game Overlays & HUD
- **Floating Mini-HUD (`Alt + H`)**:
  - Always-on-top, click-through, glassmorphic HUD overlay positioned directly over full-screen Star Citizen.
  - Live aUEC balance & session delta, current ship, active armistice/jurisdiction status, and server latency.
- **Achievement & Notification Toast Overlay**:
  - Unobtrusive, non-activating (`WS_EX_NOACTIVATE`) toast banners that queue cleanly on screen.
  - Triggers on: Learned Blueprints, Completed Contracts, Reputation Promotions, Refinery Batch Completion, Freight/Ship Elevator Readiness, Ship Destruction, and **Crimes Committed Against You** (e.g. Homicide, Vehicle Destruction by hostile players).
- **RS Radar Signature Decoder**:
  - Optical recognition of Star Citizen ping signatures (e.g., `1.800`, `7.200`, `8.000`) with zero-character recovery (`Ø`).
  - Distinguishes between valuable ship ores, ROC gems, hand gems, and salvage panels with market price estimations.
  - Synthesized 16-bit PCM Sci-Fi sonar alert chime (0 ms latency) and optional Windows TTS voice alerts.

### 🚀 Fleet Management & ASOP Loadout OCR
- **Hangar & Flight Chronicle**: Track sorties flown, quantum jumps, ship origins (Pledge Store, aUEC Purchase, Rental, Borrowed/Org), and personal pledge values.
- **1-Click ASOP Terminal Loadout Scanning**:
  - Captures complete 21-component loadouts directly from ASOP Fleet Manager "LOADOUT ESTIMATE" screens.
  - Supports Windows Clipboard scanning (`Ctrl + V`) to bypass CryEngine full-screen capture limitations.
- **Ship Loadout Inspector**:
  - Detailed component inspection modal: Power plants, coolers, quantum drives, shields, and weapons.
  - Calculates power draw headroom, thermal dissipation limits, quantum range, and weapon lead-pip convergence.
- **Offline Wiki Vehicle Caching**:
  - Local SQLite vehicle database with high-resolution ship renders served via an internal loopback HTTP server.

### 🗺️ Starmap & Jump Gate Navigation
- **Vector Route Calculator**: Dynamically calculates flight distance in Gigameters (GM) and estimates transit times based on your equipped Quantum Drive (S1 Atlas/VK-00, S2 Crossfield, S3 TS-2).
- **Inter-System Jump Gates**: Visual transit points connecting **Stanton**, **Pyro**, and **Nyx**.
- **Lagrange Network (L1 – L5)**: Complete coverage of all R&R rest stops, cargo hubs, refineries, and medical facilities.
- **Dynamic Armistice Resolver**: Real-time armistice zone detection (🟢 Armistice Safe / 🔴 Weapons Free).

### ⛏️ Industrial Suite: Mining, Salvage & Refining
- **Refinery Job Manager (`RefineryView`)**:
  - Live job tracking across all stations in Stanton, Pyro & Nyx with real-time countdown timers.
  - Yield and cost simulator comparing 7 refining methods (Dinyx, Ferron, Cormack, Electrostatic, Pyroxeres, Gaskin-Kandah, Thermite).
  - Terminal screenshot OCR scanner for instant batch capture.
  - Desktop toast alerts when batches finish refining.
- **Rock Breaking Calculator ("Can I Crack It?")**:
  - Calculates required laser power (MW) against rock mass, resistance, and instability for Prospector and MOLE mining lasers.
- **Cargo-Fit Grid Packer**:
  - 3D/2D visual cargo hold container optimizer for standard SCU crates (1, 2, 4, 8, 16, 24, 32 SCU) with ship bay height constraints.

### 📦 Planetary Warehouse & Inventory
- **Multi-Location Inventory**: Tracks items across planetary landing zones, stations, and cargo centers.
- **Freight Elevator Integration**: Quickly log item transfers, dismantling, and stock movements.

### 💰 Economy, Ledger & mobiGlas Wallet OCR
- **Live Running Balance**: Automatic transaction ledger capturing shop purchases, commodity trading, refinery fees, and mission payouts.
- **mobiGlas OCR Reconciliation**: Automatically detects hidden game expenses (repairs, fuel, re-arm, hospital fees, ship expedites) whenever you open mobiGlas (`F1`), with dual-read validation and modulus anti-truncation guards.
- **UEX Corp API 2.0 Integration**: Live commodity pricing, trading margins, and terminal data.

### 📋 Contracts, Blueprints & Reputation
- **Master Mission Database (`scunpacked-data`)**: Over 500+ SC 4.x missions with contractors, factions, and rewards.
- **Automatic Reputation Tracking**: Derives faction rankings and promotion levels based on completed contracts.
- **Crafting Blueprint Tracker**: SC 4.x blueprints categorized with learned progress indicators.

### 🧩 Hybrid Plugin System & OBS Studio Overlays
- **Embedded Loopback HTTP Server**: Serves widgets and web applications locally with full CORS headers.
- **JavaScript Plugin SDK (`/sclogmate.js`)**: Real-time bidirectional telemetry streaming for OBS Studio Browser Sources.
- **Native C# Extensions**: Supports modular C# assemblies (`ISCPlugin`) for custom logic.

### 🌐 Internationalization (i18n)
- Seamless live switching between **English (EN)** and **German (DE)** across all views, HUD elements, tables, and dialogs.

---

## 🏗️ Technical Specifications

- **Runtime**: .NET 10 (Win-x64, Native ReadyToRun single-file standalone executable)
- **GUI Framework**: Photino.NET 4.0 + React 19.3 / TypeScript 5.7 / Vite 6.4 / Tailwind CSS 4.3
- **Overlays**: Win32 Native Layered Windows (`WS_EX_LAYERED`, `WS_EX_TRANSPARENT`, `WS_EX_NOACTIVATE`)
- **Database**: SQLite WAL Mode (`%APPDATA%\SCLogMate\sessions.db`, Schema v39)
- **OCR Engine**: Windows.Media.Ocr (Native Windows 10/11 runtime)
- **Audio Engine**: Aurora AI voice announcements + synthesized 16-bit PCM procedural sonar audio

### Building from Source

Prerequisites:
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Node.js (LTS v22+)](https://nodejs.org/)

```powershell
# 1. Clone repository
git clone https://github.com/gOOvER/SCLogMate.git
cd SCLogMate

# 2. Build React frontend web assets
cd frontend
npm install
npm run build
cd ..

# 3. Publish single-file executable (CPU throttled with 4 cores)
dotnet publish -c Release -r win-x64 --self-contained true -m:4 `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false `
  -o ./publish
```

Or execute the included automated release script:
```powershell
.\release.ps1 -SkipSign
```

---

<br/>

---

<a name="-deutsch"></a>
# 🇩🇪 Deutsch

**Der ultimative Star Citizen Live Log Companion, Industrie-Suite & Analyse-Assistent.**

**SCLogMate** ist ein eigenständiger Windows-Desktop-Begleiter, der speziell für **Star Citizen 4.x** entwickelt wurde. Basierend auf hochperformantem .NET 10 und einer modernen React 19 Glassmorphism-Oberfläche (Photino.NET), überwacht und interpretiert SCLogMate deine lokale `Game.log` in Echtzeit—völlig ohne Spiel-Hooks und ohne Eingriff in den Speicher.

Vom mitlaufenden Finanzbuch über mobiGlas-Wallet-OCR-Abgleich bis hin zu Flottenverwaltung, ASOP-Terminal-Loadout-Scanning, Bergbau-Bruchrechner, 3D-Frachtraumplanung, Raffinerie-Tracking und OBS-Streaming-Widgets ist SCLogMate das unverzichtbare Co-Piloten-System für jeden Star Citizen Piloten.

---

> ℹ️ **Fork-Hinweis & Danksagung**:  
> **SCLogMate** entstand als umfassend modernisierte und stark erweiterte Weiterentwicklung von [**SCLogReader**](https://github.com/miwidot/SCLogReader) von [**miwidot**](https://github.com/miwidot). Herzlichen Dank an **miwidot** für das exzellente Fundament des Log-Parsings und der Basis-Architektur!

> ⚠️ **Inoffizielles Community-Tool**:  
> Nicht mit Cloud Imperium Games (CIG) oder Roberts Space Industries (RSI) verbunden. SCLogMate liest **ausschließlich lesend** deine lokale `Game.log` sowie Windows-Screen-/Zwischenablage-APIs—**100% AntiCheat-konform und sicher im Betrieb**.

---

## ✨ Features & Highlights (Deutsch)

### 🖥️ Native In-Game Overlays & HUD
- **Frei positionierbares Mini-HUD (`Alt + H`)**:
  - Always-On-Top, transparenter Glassmorphism-HUD direkt über dem Star Citizen Vollbildspiel.
  - Live aUEC-Saldo, Session-Delta, Schiffszustand, Sicherheitszonen-Status und Shard-Latenz.
- **Achievement & Belohnungs-Toast-Overlay**:
  - Nicht-fokussierende (`WS_EX_NOACTIVATE`) Toast-Banner, die sich ordentlich stapeln und nach wenigen Sekunden sanft ausblenden.
  - Erscheint bei: Erlernten Bauplänen, Missionsabschlüssen, Fraktions-Beförderungen, fertigen Raffinerie-Aufträgen, Fracht-/Schiffsaufzügen, Schiffszerstörung und **Verbrechen feindlicher Spieler gegen dich** (`⚔ Crime Against Player`).
- **RS Radar-Signatur-Decoder**:
  - Optische Signatur-Erkennung (z. B. `1.800`, `7.200`, `8.000`) inklusive automatischer Wiederherstellung von durchgestrichenen Nullen (`Ø`).
  - Schnelle Unterscheidung zwischen Schiffserzen, ROC-Gems, Hand-Gems und Salvage-Panels mit Echtzeit-Marktwertschätzung.
  - Generierter 16-Bit-PCM Sci-Fi Sonar-Sound (0 ms Latenz) und optionale Windows-TTS-Sprachausgabe.

### 🚀 Flottenverwaltung & ASOP Loadout OCR
- **Hangar & Flugchronik**: Erfassung von geflogenen Schiffen, Einsätzen, Quantum-Sprüngen, Erwerbsstatus (Pledge Store, In-Game aUEC, Gemietet, Geliehen/Org) und Pledge-Werten.
- **1-Klick ASOP Loadout-Scanner**:
  - Liest vollständige 21-Komponenten-Ausrüstungen direkt aus dem "LOADOUT ESTIMATE"-Bildschirm des ASOP-Terminals aus.
  - Unterstützt das Scannen direkt aus der Windows-Zwischenablage (`Strg + V`), um CryEngine-Vollbild-Screenshotsperren zu umgehen.
- **Schiffsausrüstungs-Inspektor**:
  - Detailliertes Komponenten-Modal: Generatoren, Kühler, Quantum Drives, Schilde und Waffen.
  - Analyse von Energie-Headroom, Kühlleistung, Sprungreichweite und Waffen-Lead-Pip-Konvergenz.
- **Lokaler Offline-Wiki-Fahrzeugcache**:
  - Lokale SQLite-Schiffsdatenbank mit HD-Bildern, die über einen internen Loopback-HTTP-Server blitzschnell und offline gerendert werden.

### 🗺️ Sternenkarte & Sprungtor-Navigation
- **Vektor-Routenrechner**: Berechnet Distanzen in Gigametern (GM) und prognostiziert Flugzeiten abhängig vom installierten Sprungantrieb (S1 Atlas/VK-00, S2 Crossfield, S3 TS-2).
- **Inter-System Sprungtore**: Visuelle Verbindungskorridore zwischen **Stanton**, **Pyro** und **Nyx**.
- **Lagrange-Netzwerk (L1 – L5)**: Vollständige Erfassung aller Raumstationen, Frachtzentren, Raffinerien und Kliniken.
- **Sicherheitszonen-Resolver**: Automatische Erkennung von UEE-Waffenstillstandszonen (🟢 Waffenruhe / 🔴 Gesetzlos).

### ⛏️ Industrie-Suite: Bergbau, Bergung & Veredelung
- **Raffinerie-Auftragsmanager (`RefineryView`)**:
  - Auftragsüberwachung über alle Raffinerien in Stanton, Pyro & Nyx mit Live-Sekunden-Countdowns.
  - Ertrags- und Kostensimulator für 7 Veredelungsmethoden (Dinyx, Ferron, Cormack, Electrostatic, Pyroxeres, Gaskin-Kandah, Thermite).
  - Terminal-Screenshot-OCR-Erfassung für blitzschnelles Buchen ohne Tipparbeit.
  - Desktop-Toast-Meldung bei Auftragsfertigstellung.
- **Gesteinsbruch-Rechner ("Can I Crack It?")**:
  - Berechnet die erforderliche Laserleistung (MW) anhand von Gesteinsmasse, Resistenz und Instabilität für Prospector- und MOLE-Laser.
- **Cargo-Fit Frachtraum-Planer**:
  - 3D/2D-Visualisierung zur optimalen Beladung von SCU-Containern (1, 2, 4, 8, 16, 24, 32 SCU) unter Berücksichtigung von Schiffsbuchthöhen.

### 📦 Planetenlager & Inventar (Warehouse)
- **Standortbezogene Bestandsübersicht**: Verwalte Gegenstände, Erze und Ausrüstung an allen Raumhäfen und Stationen.
- **Frachtaufzug-Aktionen**: Buchen von Frachtaufzug-Entnahmen, Zerlegungen und Lagerbereinigungen mit einem Klick.

### 💰 Finanzen, Transaktionsbuch & mobiGlas-Wallet-OCR
- **Laufendes Transaktionsbuch**: Automatische Erfassung aller Terminalkäufe, Warenverkäufe, Gebühren und Missionsbelohnungen.
- **mobiGlas-Wallet-Delta-Abgleich**: Erkennt ungeloggte Ausgaben (Reparaturen, Betankung, Munition, Krankenhausaufenthalte, Schiffs-Claims) beim Öffnen des mobiGlas (`F1`) mit Plausibilitätsprüfungen gegen Lesefehler.
- **UEX Corp API 2.0 Integration**: Live-Warenpreise, Handelsmargen und Terminal-Verfügbarkeiten.

### 📋 Aufträge, Baupläne & Ansehen (Reputation)
- **Master-Missionskatalog (`scunpacked-data`)**: Über 500+ SC 4.x PU-Aufträge mit Auftraggebern, Fraktionen und Belohnungen.
- **Automatisches Rufstufen-Tracking**: Berechnet Fraktionsstufen und Rangfortschritte anhand abgeschlossener Missionen.
- **Crafting-Bauplan-Datenbank**: Vollständiger SC 4.x Bauplan-Katalog mit Fortschrittsanzeige gelernter Rezepte.

### 🧩 Hybrides Plugin-System & OBS Studio Overlays
- **Integrierter Loopback-HTTP-Server**: Liefert Web-Widgets lokal mit vollständigen CORS-Headern aus.
- **JavaScript Plugin SDK (`/sclogmate.js`)**: Echtzeit-Telemetrie per Bidirektional-IPC für OBS Studio Browser Sources.
- **Native C#-Erweiterungen**: Unterstützt ladbare C#-Plugins (`ISCPlugin`) für individuelle Logik.

### 🌐 Zweisprachige Benutzeroberfläche (i18n)
- Nahtloses Umschalten zur Laufzeit zwischen **Deutsch (DE)** und **Englisch (EN)** für alle Ansichten, Tabellen, Menüs und Tooltips.

---

## 🏗️ Technische Spezifikationen

- **Runtime**: .NET 10 (Win-x64, Native ReadyToRun Single-File-Executable)
- **Frontend-Stack**: Photino.NET 4.0 + React 19.3 / TypeScript 5.7 / Vite 6.4 / Tailwind CSS 4.3
- **Overlays**: Native Win32 Layered Windows (`WS_EX_LAYERED`, `WS_EX_TRANSPARENT`, `WS_EX_NOACTIVATE`)
- **Datenbank**: SQLite WAL Mode (`%APPDATA%\SCLogMate\sessions.db`, Schema v39)
- **OCR Engine**: Windows.Media.Ocr (Natives Windows 10/11 Subsystem)
- **Audio Engine**: Aurora KI-Sprachansagen + prozedural generiertes 16-Bit-PCM Sonar-Audio

### Selbst kompilieren

Voraussetzungen:
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Node.js (LTS v22+)](https://nodejs.org/)

```powershell
# 1. Repository klonen
git clone https://github.com/gOOvER/SCLogMate.git
cd SCLogMate

# 2. React Frontend bauen
cd frontend
npm install
npm run build
cd ..

# 3. Single-File Exe veröffentlichen (CPU gedrosselt auf 4 Kerne)
dotnet publish -c Release -r win-x64 --self-contained true -m:4 `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false `
  -o ./publish
```

Oder das automatisierte Release-Skript ausführen:
```powershell
.\release.ps1 -SkipSign
```

---

## 📜 Disclaimer & Lizenz

Dies ist ein inoffizielles, von Fans erstelltes Community-Tool und steht in **keiner Verbindung** zu Cloud Imperium Games (CIG) oder Roberts Space Industries (RSI).

- Basiert historisch auf [SCLogReader](https://github.com/miwidot/SCLogReader) von **miwidot** (Versionen bis v1.0.0-rc2 unter MIT-Lizenz).
- Ab Version **v1.0.0** lizenziert unter der **[GNU Affero General Public License v3.0 (AGPLv3)](LICENSE)**.
- Spieldaten bereitgestellt durch [scunpacked-data](https://github.com/StarCitizenWiki/scunpacked-data) & [Star Citizen Wiki](https://star-citizen.wiki).
- Handels-Telemetrie bereitgestellt durch [UEX Corp](https://uexcorp.space).
