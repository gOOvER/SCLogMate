# 🛰️ SCLogMate

[![Build & Release](https://github.com/gOOvER/SCLogMate/actions/workflows/release.yml/badge.svg)](https://github.com/gOOvER/SCLogMate/actions)
[![Latest Release](https://img.shields.io/github/v/release/gOOvER/SCLogMate?include_prereleases&style=flat&color=38BDF8&label=Release)](https://github.com/gOOvER/SCLogMate/releases/latest)
[![VirusTotal Clean](https://img.shields.io/badge/VirusTotal-Clean%20(0%2F70)-34D399?logo=virustotal&logoColor=white)](https://www.virustotal.com/gui/search/SCLogMate)
[![Platform](https://img.shields.io/badge/Platform-Windows%20x64-0078D6?logo=windows&logoColor=white)](https://github.com/gOOvER/SCLogMate/releases)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Star Citizen](https://img.shields.io/badge/Star%20Citizen-4.x%20PU%20Ready-F59E0B)](https://robertsspaceindustries.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-6E7681.svg)](LICENSE)

[🇬🇧 English](#-english) &nbsp;|&nbsp; [🇩🇪 Deutsch](#-deutsch)

---

<a name="-english"></a>
# 🇬🇧 English

**The modern, all-in-one Star Citizen Live Log Companion & Analytics Assistant.**

SCLogMate analyzes your Star Citizen `Game.log` in real-time: Finances, contracts, fleet, cargo trading, crafting blueprints, party/crew, combat kills, locations, and more. Features an In-Game Mini-HUD, animated achievement toast banners, native Windows mobiGlas OCR, and persistent SQLite archiving.

Single standalone Windows `.exe`, no installation required, no .NET runtime setup needed.

---

> ℹ️ **Fork Notice & Acknowledgments**:  
> **SCLogMate** is an extensively expanded and modernized fork of the original project [**SCLogReader**](https://github.com/miwidot/SCLogReader) by [**miwidot**](https://github.com/miwidot).  
> Huge thanks to **miwidot** for providing the rock-solid foundation of log parsing and base architecture!

---

> ⚠️ **Unofficial Community Project.** Not affiliated with, endorsed by, or authorized by Cloud Imperium Games (CIG) or Roberts Space Industries (RSI). Reads exclusively your local `Game.log` file (**read-only**) — no game process hooks, no memory access, **100% AntiCheat compliant**.

---

## ✨ Features & Highlights (English)

### ✦ In-Game Overlays (Gaming-Style)
- **Achievement & Reward Toast Banner**:
  - Pops up on newly learned blueprints (`⬡ BLUEPRINT LEARNED`) and mission completions (`★ MISSION COMPLETE: +aUEC`).
  - Dynamic stacked list: Multiple sequential rewards queue neatly underneath rather than overwriting each other.
  - Operates completely autonomously and independently from the Mini-HUD, smoothly fades out after ~5.5 seconds.
  - Freely draggable anywhere on screen via Drag & Drop (position saved persistently).
  - Never steals focus from gameplay (`WS_EX_NOACTIVATE` — no input lag while flying or shooting).
- **Floating Mini-HUD Overlay**:
  - Freely repositionable in-game HUD displaying live balance, location, armistice status, focused contract, and server ping.

### 📋 Contract & Mission Manager
- **Built-in Master Mission Catalog (`scunpacked-data`)**:
  - Complete SC 4.x PU mission database featuring contractors (*Recco Battaglia*, *Vaughn*, *Wallace Klim*, *Miles Eckhart*, *Twitch*, etc.), factions, standard rewards, reputation XP, star systems, and crafting blueprint drops.
  - Searchable mission browser tab with instant category filtering.
- **Zero-OCR Log-Matching & Auto-Sync**:
  - Automatically identifies `Accepted`, `Complete`, `Abandoned`, and `Failed` missions from game telemetry.
  - Completed or abandoned missions are immediately cleared from active contract tracking and the SQLite database in real-time.

### ⬡ Crafting Blueprint Catalog
- **Complete SC 4.x Blueprint Database**:
  - Weapons, armor, multi-tools, ammunition, ship components, and medical gear.
  - Progress tracker (`X of Y learned`, percentage overview), category filters, and acquisition date tracking.

### 👁 mobiGlas Screenreader (Windows Native OCR)
- **Automated aUEC Balance Capture**:
  - Reads your genuine live aUEC balance via native Windows OCR whenever opening mobiGlas (`F1`).
  - Multi-monitor area calibration (`⊕ Area`) with DPI-aware hardware pixel scaling and in-game indicator box.

### 💰 Economy & Cargo Tracking
- Running balance calculations for every store purchase, sale, commodity run, reward, or fine.
- In-depth financial analytics, profit margins, and commodity market prices.
- **UEX Corp API 2.0 Integration**: Connect your personal UEX Bearer Token for live trade terminal and pricing data.

### 🚀 Fleet, Starmap & Navigation
- Fleets flown, Quantum travel arrivals, and collision vessel losses.
- **Star Citizen Wiki Integration**: In-app vehicle inspection modal with HD imagery, manufacturer specifications, and lore.
- **Starmap & Armistice Resolver**: Full support for Stanton, Pyro, and Nyx (including *Keeger Depot*, *Wikelo Emporium*, hangars, caves, and contested zones) with live armistice zone state detection (🟢 Safe / 🔴 Unprotected).

---

## ⬇️ Download & Quick Start (English)

1. Download the latest **[`SCLogMate.exe` from Releases](https://github.com/gOOvER/SCLogMate/releases/latest)**.
2. Run the `.exe` (portable, no installation).
3. Your Star Citizen log path will be detected automatically.
4. Check the **[CHANGELOG](CHANGELOG.md)** for release notes.

---

## 🏗️ Technical Specifications

- **Framework**: .NET 10 (Win-x64, Self-Contained Single-File)
- **UI**: Avalonia UI 11.2 (Fluent Dark Theme)
- **OCR**: Windows.Media.Ocr (Native Windows 10/11 Engine)
- **Storage & Index**: SQLite WAL Mode (`%APPDATA%\SCLogMate\sessions.db`)
- **RegEx Core**: Zero-Allocation C# Source Generator Expressions (`[GeneratedRegex]`)

Build via PowerShell / .NET CLI:
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish
```

---

<br/>

---

<a name="-deutsch"></a>
# 🇩🇪 Deutsch

**Der moderne, all-in-one Star Citizen Live Log Companion & Analytics Assistant.**

SCLogMate wertet deine Star Citizen `Game.log` in Echtzeit aus: Finanzen, Aufträge, Flotte, Frachthandel, Crafting-Baupläne, Crew, Kills, Standorte und vieles mehr. Mit In-Game Mini-HUD, animierten Achievement-Toast-Bannern, nativer mobiGlas-OCR und persistentem SQLite-Archiv.

Eine einzige Windows-`.exe`, keine Installation nötig, kein .NET-Setup erforderlich.

---

> ℹ️ **Fork-Hinweis & Danksagung**:  
> **SCLogMate** ist ein umfassend erweiterter und modernisierter Fork des ursprünglichen Projekts [**SCLogReader**](https://github.com/miwidot/SCLogReader) von [**miwidot**](https://github.com/miwidot).  
> Ein großes Dankeschön an **miwidot** für das exzellente Fundament des Log-Parsings und der Basis-Architektur!

---

> ⚠️ **Inoffizielles Community-Projekt.** Nicht mit Cloud Imperium Games (CIG) oder Roberts Space Industries (RSI) verbunden oder autorisiert. Liest ausschließlich lokal deine `Game.log` (**read-only**) — kein Eingriff in den Spielprozess, kein Memory-Hook, **100% AntiCheat-konform**.

---

## ✨ Features & Highlights (Deutsch)

### ✦ In-Game Overlays (Gaming-Style)
- **Achievement & Reward Toast Banner**:
  - Plopt bei neuen Bauplänen (`⬡ BAUPLAN ERLERNT`) und Missionsbelohnungen (`★ AUFTRAG ABGESCHLOSSEN: +aUEC`) auf.
  - Dynamisch gestapelte Liste: Mehrere aufeinanderfolgende Erfolge überschreiben sich nicht, sondern reihen sich sauber untereinander ein.
  - Läuft völlig autonom und unabhängig vom Mini-HUD, fadet nach ~5,5 Sekunden sanft aus.
  - Frei per Drag & Drop auf dem Bildschirm verschiebbar (Position wird dauerhaft gespeichert).
  - Stört das Gameplay nicht (`WS_EX_NOACTIVATE` — kein Fokusverlust beim Fliegen oder Kämpfen).
- **Floating Mini-HUD Overlay**:
  - Frei positionierbares In-Game Overlay mit Kontostand, Standort, Schutzzonen-Status, fokussiertem Auftrag und Live-Server-Ping.

### 📋 Auftrags- & Missions-Manager
- **Integrierter Master-Missionskatalog (`scunpacked-data`)**:
  - Vollständige SC 4.x PU-Missionsdatenbank mit Auftraggebern (*Recco Battaglia*, *Vaughn*, *Wallace Klim*, *Miles Eckhart*, *Twitch* etc.), Fraktionen, Standard-Belohnungen, Ruf-XP, Sonnensystemen und Bauplan-Drops.
  - Durchsuchbarer Missions-Browser im Tab *❖ Missionen*.
- **Zero-OCR Log-Matching & Auto-Sync**:
  - Erkennt angenommene (`Accepted`), abgeschlossene (`Complete`), abgebrochene (`Abandoned`) und fehlgeschlagene (`Failed`) Aufträge automatisch.
  - Abgeschlossene oder abgebrochene Missionen werden in Echtzeit aus der Liste aktiver Aufträge ausgetragen.

### ⬡ Bauplan-Datenbank (Crafting Blueprints)
- **Vollständiger SC 4.x Bauplan-Katalog**:
  - Rüstung, Waffen, Werkzeuge, Munition, Komponenten und Medizin.
  - Fortschrittsanzeige (`X von Y erlernt`, Prozentanzeige), Kategorie-Filter und Datums-Tracking gefundener Baupläne.

### 👁 mobiGlas Screenreader (Windows Native OCR)
- **Automatischer aUEC Kontostand-Scan**:
  - Liest beim Öffnen des mobiGlas (`F1`) den echten Kontostand per Windows Native OCR ab.
  - Multi-Monitor Bereichsauswahl (`⊕ Bereich`) mit pixelgenauer DPI-Synchronisation und In-Game Scan-Box.

### 💰 Finanzen & Wirtschaft
- Mitlaufender Kontostand bei jedem Kauf, Verkauf, Handel, Belohnungseingang oder Bußgeld.
- Detaillierte Finanz-Statistiken, Margen-Rechner und Marktpreise je Ware.
- **UEX Corp API 2.0 Integration**: Hinterlegung des persönlichen UEX Bearer Tokens für Live-Handelsdaten und Terminals.

### 🚀 Flotte, Starmap & Standorte
- Geflogene Schiffe, Quantum-Ankünfte und Schiffsverluste.
- **Star Citizen Wiki Integration**: Detailanzeigen mit HD-Schiffsbildern, Herstellern und technischen Daten.
- **Starmap & Schutzzonen-Resolver**: Stanton, Pyro und Nyx (inkl. *Keeger Depot*, *Wikelo Emporium*, Hangars, Höhlen und Contested Zones) mit Live-Erkennung von Waffenstillstandszonen (🟢 Grün / 🔴 Rot).

---

## ⬇️ Download & Installation (Deutsch)

1. Lade die neueste **[`SCLogMate.exe` aus den Releases](https://github.com/gOOvER/SCLogMate/releases/latest)** herunter.
2. Starte die `.exe` (keine Installation nötig).
3. Dein Star Citizen Installationspfad wird automatisch erkannt.
4. Alle Änderungen und Updates findest du im **[CHANGELOG](CHANGELOG.md)**.

---

## 📜 Disclaimer & Lizenz

Dies ist ein inoffizielles, von Fans erstelltes Community-Tool und steht in **keiner Verbindung** zu Cloud Imperium Games (CIG) oder Roberts Space Industries (RSI).

- Basiert auf dem Originalprojekt [SCLogReader](https://github.com/miwidot/SCLogReader) von **miwidot**.
- Externe Spieldaten via [scunpacked-data](https://github.com/StarCitizenWiki/scunpacked-data) & [Star Citizen Wiki](https://star-citizen.wiki).
- Externe Handelsdaten via [UEX Corp](https://uexcorp.space).

Lizenziert unter der [MIT-Lizenz](LICENSE).
