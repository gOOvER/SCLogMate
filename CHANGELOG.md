# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- **Full Parity with SCLogMate 1.0.0-rc2 Structure & Subtabs**:
  - **Settings Subtabs (`SettingsView.tsx`, `PhotinoBridge.cs`)**:
    - Restored exact 8 subtabs in RC2 order: `📁 Allgemein`, `⏳ Wipe & Filter`, `🖥 Overlays & HUD`, `👁 mobiGlas & OCR`, `🌐 UEX Integration`, `🎙 VoiceAttack & Aurora`, `💾 Datenbank & Wartung`, and `🧪 Entwickler` (conditional on `debugMode`).
    - Added dedicated **Wipe & Persistence Filter** subtab with live toggle, target date input, quick presets ("★ 4.8 Wipe", "📅 Heute", "✕ Filter aus"), and modular category filters (Money, Contracts, Fleet, Blueprints).
    - Added **Developer & Debug Tools** subtab (`debugMode`) with live simulation triggers for armistice entry/exit, ship channel entry, blueprint discovered, quantum arrival, 30k server error, and emergency/death, plus state dump and log cleanup.
  - **Tools Subtabs (`ToolsView.tsx`)**:
    - Added subtab switching matching RC2: `System & Wartung` (Shader cache, crash dumps, system diagnostics, user.cfg live editor & tuning generator) and `Backups` (ActionMaps keybind archive, backup creation with notes, restore directory info).
  - **Missions & Finances Parity**:
    - Aligned Missions subtabs to `Missionsverlauf`, `Aktive OCR-Verträge`, and `Auftragskatalog`.
    - Aligned Finances subtabs to `Übersicht`, `Buchhaltung`, `Ausgaben`, and `Fracht` with interactive financial timeline chart.
    - Aligned Events filter chips to the exact 9 RC2 categories: `Alle`, `Geld`, `Aufträge`, `Baupläne`, `Schiffe`, `Orte`, `Crew`, `Loot`, `Sonst`.
  - **Full Parity for Fleet & Personal Hangar Management (`FleetView.tsx`, `PhotinoBridge.cs`)**:
    - Restored the exact Avalonia RC2 **Flotten- & Hangar-Verzeichnis** structure with segmented switcher: `🏠 Mein Hangar` (owned ships filter with live counter) vs `✈ Flug-Historie` (all ships documented in game logs).
    - Added catalog-backed **+ Schiff hinzufügen** modal with search across all `FleetCatalog` entries, enabling users to add ships into "Mein Hangar" even if not yet flown in logs.
    - Added 1-click star toggle (`★` / `☆`) to dynamically add/remove ships from the personal Hangar.
    - Added acquisition cycler chip (`💵 Pledge Store` ➔ `🪙 In-Game (aUEC)` ➔ `🎟 Miete (Rental)` ➔ `👥 Geliehen / Free Fly`).
    - Added inline Pledge USD amount editor with direct persistence to SQLite `fleet_user_ships`.
    - Added insurance cycler chip (`LTI (Lifetime)` ➔ `120 Monate (IAE)` ➔ `24 Monate` ➔ `12 Monate` ➔ `6 Monate`).
    - Added "Als aktiv" quick button to set any fleet ship as active for top HUD telemetry and log parsing.
    - Added complete Telemetry KPI Cluster: `WERT` (estimated aUEC market value), `PLEDGE` (total pledge USD), `FLÜGE` (total flight count), and `QUANTUM` (total quantum jumps).
    - Added manufacturer brand chips and acquisition filter chips, with Star Citizen Wiki and UEX price lookup action links.
  - **Aurora Voice Service Integration (`PhotinoBridge.cs`)**:
    - Wired `AuroraVoiceService` into live log tailing and simulation commands, honoring user volume and activation settings.

### Changed
- **Session Architecture & Live-Only Start Page (`SessionBar.tsx`, `App.tsx`, `EventsView.tsx`)**:
  - Removed global session dropdown from the master `SessionBar` header, transforming it into a clean, permanent Live-Stream companion status bar (`LIVE-STREAM`, `Game.log`, active time span, and HUD collapse toggle).
  - Ensured the start page (`EventsView`) is strictly dedicated to monitoring the active live session (`Game.log`), preventing accidental switching of global live telemetry to historical sessions.
  - Contextually relocated historical session selection to `EventsView` under `Sitzungsarchiv` mode, where users can select and inspect any of the archived sessions or all sessions on demand.
- **Finances Chart Overhaul & Modernization (`FinancesView.tsx`)**:
  - Removed misleading legacy "Quantum Timeline" badge and terminology, replacing it with an informative `SALDEN- & TRANSAKTIONSVERLAUF` header.
  - Eliminated dense static point circles that caused lumpy caterpillar-like stroke artifacts across dense ledger datasets.
  - Implemented 3 dedicated, clean chart modes: `Kumulativ (Saldo)`, `Einnahmen vs. Ausgaben`, and `Cashflow (Einzelposten)`.
  - Added formatted aUEC Y-axis grid lines (`+1.5M`, `+750K`, `0`, `-200K`) and X-axis timestamp ticks.
  - Added interactive cursor tracking with vertical crosshair, target reticles, and real-time live telemetry inspector strip showing timestamp, booking detail, transaction delta, and resulting balance.
- **Events View Modernization & Session Sync (`EventsView.tsx`, `App.tsx`)**:
  - Removed duplicate session selector dropdown from the `EventsView` filter bar, binding it directly to the global master HUD `SessionBar` (`telemetry.selectedSession`).
  - Streamlined the filter bar with a sleek active session status pill, giving full horizontal prominence to glowing category chips, search, and live tail controls.
  - Redesigned the events empty state with an interactive sci-fi HUD empty terminal display and a 1-click filter reset button.
- **Events Grid & Database Localization Parity (`EventsView.tsx`, `LogParser.cs`, `PhotinoBridge.cs`)**:
  - Restored crisp, separated table columns with vertical borders for `ZEIT`, `TYP`, `BETRAG`, `SCHIFF`, and `DETAIL`, resolving the merged "BETRAG SCHIFF" header and double-dash `-- --` artifacts.
  - Aligned empty cell rendering to Avalonia RC2: cells with zero amount or no ship now render clean and empty instead of placeholder dashes (`—`).
  - Removed redundant `{e.title}: ` prefix in the `DETAIL` column and added status color-coding matching Avalonia RC2's `StatusBrush`.
  - Linked `Localization.Hint` and `I18n.Instance.SetLanguage` to Photino startup and settings updates so game localization files (`global.ini`) are loaded for the active game path.
  - Integrated `WarehouseCatalog.Resolve` and `Localization.ItemName` into player attachment loadout and loot parsing to resolve raw internal class names (e.g. `Carryable_2H_..._blackbox` -> `Flugschreiber / Blackbox (Missionsgut)`).
  - Fixed Hangar elevator events erroneously setting the location name as `Ship`.
  - Added mission text localization for common English mission status lines into German when the app language is set to German.
- **Contract Tracking & OCR Parity with RC2 (`SettingsView.tsx`, `MissionsView.tsx`, `PhotinoBridge.cs`)**:
  - Confirmed and reflected that contract OCR scanning is permanently disabled (as in Avalonia RC2 `MainViewModel.cs:3297`) due to mobiGlas transparency and low-contrast font recognition failures, superseded by 100% reliable native `Game.log` notification parsing (`Contract Accepted`, `Contract Complete`, `Contract Failed`).
  - Replaced obsolete contract OCR test and snipping coordinates card in `SettingsView.tsx` with the official RC2 `❖ AKTIVE AUFTRÄGE & MISSIONS-TRACKING` card featuring `✕ Aufträge leeren` and `⚡ Auto-Sync aktiv` status badge.
  - Implemented `clear_contracts` RPC in `PhotinoBridge.cs` calling `Database.ClearActiveContracts()`.
  - Updated `MissionsView.tsx` labels from `Aktive OCR-Verträge` to `Aktive Aufträge (Live)`.
- **Navigation & Sidebar (`Sidebar.tsx`, `App.tsx`, `AboutView.tsx`)**:
  - Restored exact 16 tabs in RC2 order (`events`, `finances`, `missions`, `reputation`, `starmap`, `places`, `blackbox`, `orescanner`, `market`, `fleet`, `warehouse`, `blueprints`, `loadout`, `tools`, `settings`, `about`).
  - Default tab set to `events` (`Ereignisse`), matching Avalonia RC2 startup behavior.
  - Version aligned to `v1.0.0-rc2` matching `SCLogMate.csproj`.

### Removed
- **Redundant Manual Toolbar Refresh Buttons Across All Views**:
  - Removed obsolete circular refresh (`RefreshCw`) buttons from `FleetView`, `EventsView`, `WarehouseView`, `ReputationView`, `PlacesView`, `OreScannerView`, `MissionsView`, `MarketView`, `LoadoutView`, `FinancesView`, `BlueprintsView`, `BlackboxView`, `StarmapView`, and `MasterHeader`.
  - All views receive instantaneous real-time push updates via LogTailer, reactive SQLite queries, and bidirectional IPC broadcasts (`FLEET_UPDATED`, `HUD_UPDATE`, `WAREHOUSE_UPDATED`, etc.), rendering manual page-level refresh buttons completely unnecessary.
- **Unauthorized Scraping & Long-Removed Features**:
  - Removed unauthorized RSI citizen web scraper (`CitizenProfileService.cs`) and `PilotDossierModal.tsx`.
  - Removed obsolete standalone `DashboardView` and `SessionsView` tabs (dashboard is docked in HUD rows 1-2, and sessions are switched globally in the header).
  - Removed obsolete manual balance input card from settings.

### Added
- **Interactive OCR Region Selection & Calibration Subsystem (`NativeRegionSelector.cs`, `NativeScanIndicator.cs`, `SettingsView.tsx`, `PhotinoBridge.cs`, `photinoBridge.ts`)**:
  - Implemented interactive on-screen snipping tool (`NativeRegionSelector.cs`) using pure Win32 layered overlay with live drag-rectangle, real-time pixel dimensions badge, ESC cancellation, and multi-monitor switching (`Tab` or `M`).
  - Added click-through on-screen scan indicator frame (`NativeScanIndicator.cs`) rendering a persistent cyan border and label over Star Citizen or desktop with `FlashGreen` visual feedback upon successful OCR balance extraction.
  - Comprehensive OCR configuration view in `SettingsView.tsx` (`mobiGlas & OCR` tab):
    - Dedicated configuration cards for both **mobiGlas Wallet (aUEC)** and **Auftragsmanager (Contract Manager)**.
    - Quick 1-click resolution presets for 1080p, 1440p (WQHD), 4K (UHD), 3440×1440 (21:9 Ultrawide), and 5120×1440 (32:9 Super Ultrawide).
    - Manual coordinate fine-tuning inputs for X, Y, Width, and Height with instant save.
    - Interactive "⚡ Test-Scan" button that runs real-time OCR and returns recognized text, extracted aUEC amount, and millisecond latency.
    - "👁 Scan-Rahmen im Spiel anzeigen" toggle to preview bounding box directly over the game client.
    - Reset buttons to return to dynamic resolution-based auto-detection.
- **Database & Log Synchronization Modal (`DbUpdateModal.tsx`, `App.tsx`, `PhotinoBridge.cs`)**:
  - Created a dedicated Glassmorphism `DbUpdateModal` dialog displaying whenever database schema migrations or parser upgrades require a full re-scan, or when indexing newly detected log files.
  - Features real-time progress animation, processed/total session counters, percentage badge, current filename indicator, and an update reason badge (e.g. schema migration or parser version bump).
  - Automatically verifies logs against database fingerprints (`Length:LastWriteTimeUtc.Ticks`), skipping unmodified logs in under 10ms to keep the database up-to-date with minimal startup overhead.
- **Comprehensive Multi-Directory Log Discovery (`PhotinoBridge.cs`)**:
  - Implemented `DiscoverAllLogFiles()` to discover all Star Citizen logs across all mounted drives, release channels (`LIVE`, `PTU`, `EPTU`, `HOTFIX`, `TECH-PREVIEW`), `logbackups` subfolders, and internal archives.
- **RSI Citizen Dossier & Profile Service (`CitizenProfileService.cs`, `PilotDossierModal.tsx`, `MasterHeader.tsx`, `HudBar.tsx`, `PhotinoBridge.cs`)**:
  - Created `Core/CitizenProfileService.cs` which asynchronously fetches and caches Star Citizen dossiers directly from RSI (`robertsspaceindustries.com/citizens/<handle>`):
    - Parses UEE Citizen Record number (e.g. `#593923`), title (e.g. `High Admiral`), enlistment date, fluency languages, avatar image URL, primary organization name, org SID, org rank, org logo, and pilot bio.
    - Caches profile metadata in-memory with thread-safe `ConcurrentDictionary` to minimize web requests.
  - **Interactive Glassmorphism Pilot Dossier Modal (`PilotDossierModal.tsx`)**:
    - Designed an interactive, sci-fi glassmorphism pilot dossier with hologram avatar, UEE citizen record badge, org insignia, enlisted date, fluency badges, bio text, and quick link to the RSI citizen page.
  - **Pilot Identity Integration across Navigation & HUD (`MasterHeader.tsx`, `HudBar.tsx`, `SessionBar.tsx`)**:
    - Added pilot avatar thumbnail with fallback icon, player handle, and official RSI title in the `MasterHeader`.
    - Added avatar thumbnail and click-to-open dossier trigger to the Character/Pilot HUD card.
- **In-Memory Live Log Ring Buffer & Real-Time Live Feed (`PhotinoBridge.cs`, `App.tsx`, `EventsView.tsx`)**:
  - Implemented thread-safe `_liveEvents` ring buffer (up to 1,000 entries) in `PhotinoBridge.cs`.
  - Configured `LogTailer` to start with `fromStart: true` and stream historical lines from the active `Game.log` into the live buffer while monitoring file EOF for real-time live events.
  - Emitted `LIVE_EVENTS_LOADED` and synced live events with initial load and runtime updates so `DashboardView` and `EventsView` immediately show live log events.
- **Dedicated Live Session Pinned Dashboard (`SessionBar.tsx`, `App.tsx`, `EventsView.tsx`, `BlackboxView.tsx`, `SessionsView.tsx`)**:
  - Replaced the session selector dropdown on the main Dashboard view with a glowing `🔴 LIVE-SITZUNG: Game.log` indicator to keep the main view anchored to real-time gameplay.
  - Added session selector dropdowns to the specific telemetry and log views (`EventsView` and `BlackboxView`), allowing users to inspect historical sessions while the Dashboard stays on the live feed.
  - Added a direct **`Chronik`** shortcut button to session rows in `SessionsView.tsx` to jump directly into the event log of any chosen session.

### Fixed
- **Critical WebView2 Startup Crash & Native Access Violation (`PhotinoBridge.cs`, `photinoBridge.ts`)**:
  - Fixed startup crash (`0xc0000005` access violation in `Photino_SendWebMessage`) caused by IPC messages being sent from background threads before the Photino WebView2 control was fully initialized.
  - Implemented a robust two-way `client_ready` handshake: the frontend sends `client_ready` upon mounting, and the backend defers background tailers and sync operations until this message is received.
  - Thread-safe IPC dispatch: marshaled all `SendWebMessage` calls through `_window.Invoke` with a dedicated lock (`_sendLock`), preventing any cross-thread reentrancy or race conditions in native Photino code.
- **Pilot Character Name, Server Shard, and Region Flag Resolution (`HudBar.tsx`, `PhotinoBridge.cs`, `LogParser.cs`, `Database.cs`)**:
  - **Pilot & Player Name Extraction**:
    - Resolved the missing pilot name in the HUD bar by actively scanning the active or selected log file header (up to 3,000 lines) on startup and session selection.
    - Added `NicknameRegex` support to `LogParser.cs` as a seamless fallback to `CharRegex` to capture pilot identity from `nickname="..."` network log entries.
    - Added database fallback query (`SELECT pilot FROM sessions ...`) to maintain pilot continuity when a fresh session starts before initial character login lines.
  - **Dynamic Shard Information & Subline Numbering**:
    - Fixed empty shard display by reading `<Join PU>` events and computing the human-readable shard number (e.g. `Shard #170`) alongside the full shard identifier (`pub_euw1b_12545750_170`).
    - Added tail scanning (`ScanLogTailForShard`) to ensure shard transitions and server-hops during prolonged gameplay sessions are accurately updated.
  - **Accurate Region Flags & Default Handling**:
    - Replaced the hardcoded EU default and inaccurate CSS pseudo-flag with crisp Unicode region flag emojis (`🇪🇺 EU`, `🇺🇸 US`, `🇦🇺 AUS`, `🌏 ASIA`, `🌐 PU / Global`).
    - Aligned region detection with RC2 `ServerRegionInfo` logic: properly displays `🌐 PU` or `🌐 —` when offline or not connected to a persistent shard instead of falsely showing the EU flag.
  - **Database Schema Upgrade v18 (`Database.cs`)**:
    - Bumped `CurrentSchemaVersion` to 18 and applied SQLite migration adding `pilot`, `shard`, and `version` columns to the `sessions` table so historical session telemetry is persisted and loaded with zero file latency.

### Added
- **Global & In-View Live Progress Banners for Database Operations & Scans (`App.tsx`, `SettingsView.tsx`, `EmbeddedAssets.cs`)**:
  - **Global Log-Scan & Re-Scan Progress Banner (`App.tsx`)**:
    - Embedded a persistent, glowing progress banner right below the master header whenever a scan or re-parse is active.
    - Displays the active log file counter `(X/Y)`, current log file name, percentage, and animated gradient progress bar across all views.
  - **Active Operation Banners in Database Maintenance (`SettingsView.tsx`)**:
    - Added dedicated live progress banners whenever a database task is executing:
      - **Deep Integrity Check**: Pulsing banner and button spinner indicating `PRAGMA integrity_check` is analyzing database pages and B-trees.
      - **VACUUM & Cleanup**: Real-time banner indicating orphaned records are being purged and database pages compacted.
      - **Structure & Index Repair**: Real-time banner indicating tables, columns, and SQLite indexes are being rebuilt.
      - **Batch Re-Scan**: Live gradient progress bar with file count and log filename.
    - **Detailed Completion Banners**: Added dismissible result banners displaying detailed outcome status, timestamps, and cleaned size metrics (`sizeBefore -> sizeAfter`).
    - Added active spinners and loading labels (`Prüfe Integrität...`, `Bereinige...`, `Repariere Struktur...`) to all maintenance buttons.
  - **Embedded Asset Freshness Guarantee (`EmbeddedAssets.cs`)**:
    - Ensured `index.html` is always cleanly overwritten on extraction to avoid stale browser cache bundles.
- **Full Batch Log Re-Reading & Comprehensive Database Maintenance ("Alle Logs neu einlesen") (`SettingsView.tsx`, `SessionsView.tsx`, `MasterHeader.tsx`, `PhotinoBridge.cs`)**:
  - Brought back the prominent 3-card maintenance center from RC2 in **Settings -> SQLite & Datenbank**:
    - **Kompletter Re-Scan Card**: Action button **`🔄 Alle Logs neu einlesen`** with live animated gradient progress bar (`SCAN_PROGRESS` with indexed file count, filename, and percent) and completion summary banner (`✓ Re-Scan abgeschlossen: X Sessions, Y Ereignisse neu indexiert`).
    - **Datenbank-Bereinigung Card**: Action button **`🧹 DB bereinigen & VACUUM`** to purge orphaned records and shrink the database file.
    - **Datenbank leeren Card**: Action button **`✕ Datenbank zurücksetzen`** with safety confirmation prompt to reset all tables and indexes.
    - Quick-launch tools: Deep integrity check, structure & index repair, open `sessions.db` in Windows Explorer, and open `SCLogMate.debug.log`.
  - **Exhaustive Multi-Drive & Channel Log Discovery (`PhotinoBridge.cs`)**:
    - Enhanced `ReparseAllLogs()` to scan all drives (`DriveInfo.GetDrives()`), known Star Citizen roots (`Program Files`, `Roberts Space Industries`, `Games`, `StarCitizen`), all channels (`LIVE`, `PTU`, `EPTU`, `HOTFIX`, `TECH-PREVIEW`), every `logbackups` directory (even if `Game.log` is missing or game is inactive), cloud storage paths, and local `LogArchive`, ensuring all historical logs (540+ files) are ingested.
  - **Prominent Re-Scan Access Across the Entire App**:
    - Renamed and highlighted the main action in `SessionsView.tsx` to **`🔄 Alle Logs neu einlesen`**.
    - Integrated **`🔄 Alle Logs neu einlesen (Kompletter Re-Scan)`** directly into the in-view DB maintenance modal.
    - Added a quick Re-Scan button next to Quick-Scan in `MasterHeader.tsx` accessible from any page.
- **Complete Log Parsing & Session Management System (`SessionsView.tsx`, `Core/Photino/PhotinoBridge.cs`, `NativeDialogs.cs`)**:
  - Implemented the complete log parsing and session management suite in `SCLogMate-Photino` matching and enhancing the RC2 functionality:
    - **Log Source & Channel Command Bar**: Real-time display of the active `Game.log` path, Star Citizen channel badge (`LIVE`, `PTU`, `EPTU`, `TECH-PREVIEW`, `CUSTOM`), live file size, last modified timestamp, and streaming watcher indicator.
    - **Channel Quick-Switcher**: Multi-channel pill bar displaying all installed Star Citizen environments detected on the machine for instant 1-click switching.
    - **Automated Path Detection & Native File Picker**: Integrated `PathFinder.FindBest()` (`detect_log_path`) and native Win32 `comdlg32.dll` open file dialog (`NativeDialogs.cs` / `browse_log_file`).
    - **Comprehensive Multi-Session Re-Scan**: Added full batch log ingestion across `LogArchive`, active `Game.log`, `logbackups` directories, and all detected channels via `Database.RescanAll`, with real-time IPC progress broadcasting (`SCAN_PROGRESS` with file counter, percent, and file name) and progress bar visualization.
    - **Interactive Sessions Management Table**: Multi-column DataGrid showing session name, start/end time, duration, income, expenses, net aUEC profit, ships flown, last location, and death/loss status. Includes per-row actions to activate as current session, re-parse individually, or delete from the database.
    - **Database Diagnostics & Maintenance Center**: Built-in modal providing full SQLite integrity status (`PRAGMA quick_check`), installed vs current schema/parser versions, table row counts, structure repair (`Database.RepairOrUpdateStructure`), VACUUM optimization (`Database.Cleanup`), and database reset.
    - **Unknown Events Log Viewer**: In-app viewer modal for `SCLogMate.unknown.log` with category statistics and shortcut to open directly in the system text editor.
    - **CSV & JSON Data Export**: Instant export functionality for all session transactions and event chronologies.
  - Connected real backend IPC handlers in `SettingsView.tsx` for log path auto-detection, file browsing, and SQLite database maintenance (VACUUM, deep integrity check, and structure repair).
- **Nyx Star System & Mouse Wheel Zoom in Starmap (`StarmapView.tsx`, `PlacesView.tsx`, `PhotinoBridge.cs`)**:
  - Integrated the complete **Nyx System** (Delamar, Levski free mining port, Keeger Belt, Glaciem Ring, Theta Station, and Jump Points) into the interactive Starmap radar and Places directory.
  - Implemented smooth, non-passive **mouse wheel zooming** on the Starmap SVG canvas, allowing seamless scroll zoom from 0.3x to 3.5x without scrolling the parent view.
- **Full Reactive i18n System & Multi-Language Support (`frontend/src/i18n/`)**:
  - Implemented a complete frontend internationalization engine supporting German (`de`) and English (`en`) with `localStorage` language persistence.
  - Connected the Master Header vector flags (DE 🇩🇪 / EN 🇬🇧) to dynamically translate all top navigation tabs, group headers, 2-line HUD telemetry cards, session selector, and dashboard widgets instantly.
- **Full-Width Live Event Stream on Dashboard (`DashboardView.tsx`)**:
  - Expanded the live event feed across the full width of the start page:
    - Displays high-density Star Citizen events with timestamp, categorized color-coded pills (Finances, Combat, Mission, Ship, Location, System), detailed description, ship context, signed aUEC amounts, and direct navigation to the Chronik view.
- **True Standalone Single-File Binary with Embedded Web Frontend (`EmbeddedAssets.cs`, `SCLogMate.csproj`, `Program.cs`)**:
  - Embedded all compiled web frontend assets (`wwwroot/**`) directly into the `SCLogMate.exe` binary via `<EmbeddedResource Include="wwwroot\**" LogicalName="wwwroot/%(RecursiveDir)%(Filename)%(Extension)" />`.
  - Implemented `EmbeddedAssets.EnsureIndexHtml()` (`Core/Photino/EmbeddedAssets.cs`):
    - Supports seamless local development overrides if a `wwwroot` directory exists adjacent to the source / binary.
    - If running standalone, automatically extracts embedded assets to `%APPDATA%/SCLogMate/wwwroot/` with size-matching caching to prevent redundant disk I/O.
  - Cleaned publish pipeline to remove the `publish/wwwroot` folder post-publish, delivering a 100% self-contained single `SCLogMate.exe` binary with zero external folders or dependencies.

### Fixed
- **Responsive Viewport Scrolling & Custom Sci-Fi Scrollbars (`App.tsx`, `index.css`, and all views)**:
  - Fixed content clipping and missing scrollbars on smaller windows across all 16 views:
    - Added `min-h-0 min-w-0 overflow-y-auto overflow-x-auto` to the main viewport container in `App.tsx` and resolved the flexbox height collapsing bug.
    - Updated all individual views (`DashboardView`, `EventsView`, `SessionsView`, `FinancesView`, `FleetView`, `WarehouseView`, `StarmapView`, `PlacesView`, `OreScannerView`, `MarketView`, `MissionsView`, `ReputationView`, `BlueprintsView`, `LoadoutView`, `BlackboxView`) from rigid `h-full overflow-hidden` to responsive `min-h-full` containers.
    - Implemented modern CSS standard `scrollbar-color: rgba(0, 240, 255, 0.4) rgba(3, 7, 18, 0.85)` and `scrollbar-width: thin` with high-contrast glowing WebKit fallbacks in `index.css`.
    - Added horizontal scroll handling and minimum table column widths (`min-w-[700px]` to `min-w-[920px]`) for dense multi-column DataGrids in `SessionsView`, `EventsView`, `FleetView`, `WarehouseView`, `OreScannerView`, and `MarketView`.
- **Single-File Release Web Asset Packaging & Launch Fix (`Program.cs`, `SCLogMate.csproj`)**:
  - Fixed application failing to start in release builds (`ERR_CONNECTION_REFUSED`):
    - Added an automatic MSBuild post-publish target (`EnsureWwwrootInPublish`) and `<CopyToPublishDirectory>Always</CopyToPublishDirectory>` in `SCLogMate.csproj` so all compiled web assets (`wwwroot/index.html` and `assets/*`) are guaranteed to be copied into the single-file publish directory (`publish/wwwroot/`).
    - Fixed path resolution in `Program.cs` by inspecting all candidate paths (`AppContext.BaseDirectory`, `AppDomain.CurrentDomain.BaseDirectory`, `Environment.CurrentDirectory`, parent relative directories).
    - Removed blind fallback to `http://localhost:5173` which triggered Edge WebView2 network error screens when no Vite dev server was running. If static assets are missing, a structured diagnostic error page is now displayed via `window.LoadRawString()`.
    - Synced `Environment.CurrentDirectory` to `BaseDirectory` on startup to ensure consistent relative file resolution across all execution methods.

### Changed
- **Master Header Cleanup (`MasterHeader.tsx`, `App.tsx`)**:
  - Removed manual Stop/Watcher toggle button from the top master bar to align with SCLogMate RC2 architecture, which relies on fully automated `StarCitizen.exe` process tracking and `Game.log` streaming.
- **RC2 UI/UX Alignment — Step 5: Fleet Registry & High-Density Ship Statistics (`FleetView.tsx`)**:
  - Refactored Fleet view (`FleetView.tsx`) with a high-density, multi-column Star Citizen hangar dashboard:
    - **Fleet KPI Summary Cards**: 4 top cards displaying total registered ship types, total flight sorties, successful Quantum Jumps, and estimated fleet net worth in aUEC.
    - **Interactive Fleet DataGrid**: High-density table featuring ship name, sortie counter, QT jump counter, loss/destruction badge, last flown session timestamp, and direct link to Star Citizen Wiki.
    - **Instant Filter & Search**: Client-side filtering by ship model name.
- **RC2 UI/UX Alignment — Step 4: Warehouse & Storage Management (`WarehouseView.tsx`)**:
  - Redesigned Warehouse inventory view (`WarehouseView.tsx`) to strictly match the RC2 two-column Star Citizen layout:
    - **Left Column — Location Selector**: Planetary hubs and space stations grouped with Star System pills (`STANTON`, `PYRO`, `NYX`), item counters, total stored value in aUEC, and 1-click filter.
    - **Right Column — High-Density Inventory DataGrid**: Direct in-grid quantity adjustments (`+` / `-`), color-coded item categories (`Commodity`, `Component`, `Weapon`, `Armor`, `Consumable`), unit prices, and total value.
    - **Freight Elevator Quick Actions**: Contextual quick action bar with `📦 Per Frachtaufzug entnehmen`, `🔧 Zerlegt`, and `Standort leeren` with confirmation dialogs.
- **RC2 UI/UX Alignment — Step 3: Financial Dashboard, Balance History & Ledger (`FinancesView.tsx`, `Core/Photino/PhotinoBridge.cs`)**:
  - Overhauled Finances view (`FinancesView.tsx`) with a comprehensive Star Citizen financial suite:
    - **Top Financial KPIs**: 5 cards displaying current live wallet balance, total session income, total session expenses, net session profit/loss, and trade transaction volume.
    - **Interactive Balance Trend**: Glowing Sci-Fi SVG balance progression chart tracking aUEC over time with interactive hover points.
    - **Categorized Sub-Tabs**: Multi-tab layout featuring Overview & Trends, Transaction Ledger, Cargo & Commodity Trade, Top Expenses breakdown, and Manual Bookings.
    - **Manual Booking Terminal**: Integrated custom financial entry modal and backend bridge endpoint `record_expense` (`PhotinoBridge.cs`) allowing pilots to record custom expenses (fuel, repair, re-arm, fines) directly into SQLite.
- **RC2 UI/UX Alignment — Step 2: High-Density Events DataGrid & Inspector (`EventsView.tsx`)**:
  - Rebuilt the Events log view (`EventsView.tsx`) into a high-density, multi-column Star Citizen event table matching RC2 while modernizing UX:
    - **Interactive Multi-Column DataGrid**: Cleanly aligned columns for timestamp (`ZEIT`), category badge (`TYP`), signed monetary delta (`BETRAG`), active ship name (`SCHIFF`), and detailed description (`DETAIL`).
    - **Interactive Column Sorting**: 1-click sorting on time, category, amount, ship, or title with directional arrows.
    - **Keyboard Navigation**: Native `Up` / `Down` arrow key navigation across events and `Escape` to close inspector.
    - **Categorized Filter Chips**: Quick filtering by All, Finances (`💰 Finanzen`), Combat (`⚔️ Kampf`), Contracts (`🎯 Aufträge`), Ships (`🚀 Schiffe`), Locations (`📍 Standorte`), and System (`⚙️ System`).
    - **Real-Time Search & Live Tail**: Search input with instant clear button `✕`, live hit counter, and glowing `● LIVE TAIL` indicator.
    - **Side Inspector Drawer**: Slide-out event detail card featuring structured fields, 1-click clipboard actions (`Copy Detail`, `Copy Raw Line` with checkmark feedback), Wiki link, and raw log syntax block.
- **RC2 UI/UX Alignment — Step 1: Global Shell, Master Header & 2-Line SC-HUD (`MasterHeader.tsx`, `SessionBar.tsx`, `HudBar.tsx`, `App.tsx`, `Core/Photino/PhotinoBridge.cs`)**:
  - Implemented high-density Star Citizen mobiGlas top shell aligned with SCLogMate RC2 while delivering a modern, clean glassmorphism aesthetic:
    - **Master Header (`MasterHeader.tsx`)**: Live `StarCitizen.exe` process detection badge (`● LIVE GAME` pulsing green vs `○ OFFLINE`), active Game.log path indicator, quick overlay launch controls (`🖥 Mini-HUD`, `🛰 RS-Overlay`), language toggle with vector flags (DE 🇩🇪 / EN 🇬🇧), live log watcher toggle, and manual log directory scan trigger.
    - **mobiGlas Session-Strip (`SessionBar.tsx`)**: Dedicated session selection bar placed directly below the header with quick options for live stream, cumulative history, and individual flight sessions, styled glowing `SessionSpanText` badge, and 1-click HUD collapse/expand toggle (with `Alt + H` shortcut).
    - **Collapsible 2-Line Star Citizen HUD (`HudBar.tsx`)**:
      - *Row 1 (3 modular telemetry cards)*: Pilot handle & server status (vector region flags for EU/US/AUS/World, SC version, shard ID, live ping latency), location & jurisdiction (system badges for Stanton/Pyro/Nyx, dynamic armistice safety badge, celestial body, quick jump to Starmap), and active ship & flight statistics (flown ship, flights, QT jumps, quick jump to Fleet & Wiki).
      - *Row 2 (2 wide cards)*: Wallet & balance ledger (prominent monospace aUEC balance, auto-sync and test scan controls, session delta pills for income, expenses, and net profit) and contract manager (active mission title, reward in aUEC, issuing faction/client, and current mission status).
  - Added backend IPC endpoints in `PhotinoBridge.cs`: `get_hud`, `select_session`, `trigger_ocr`, `toggle_auto_ocr`, `open_overlay`, `open_rs_overlay`, and automatic `HUD_UPDATE` event broadcast on incoming log lines.
- **Photino.NET & React Full Functional Migration — Phase 4 (Full Completion) (`frontend/src/views/`, `frontend/src/services/`, `Core/Photino/`)**:
  - Ported **Tools & Cache Cleaner View** (`ToolsView.tsx`): GPU Shader-Cache (DirectX/Vulkan) & crash dumps cleaner with live MB size counters, Quick Tuning presets (High FPS / E-Sport, Graphics & Immersion, Minimal PC), live `user.cfg` generator and code editor with line numbers and copy-to-clipboard, Keybind backup manager with custom notes and timestamped archives, and real-time hardware diagnostics (RAM status, disk free space, pagefile).
  - Ported **Settings View** (`SettingsView.tsx`): Categorized configuration dashboard with sub-tabs for General (Game.log auto-detection and file path, start balance), Overlays & HUD (floating mini-HUD toggle, opacity slider, in-game toast notifications for blueprints, missions, factions, refinery, freight elevator, ship destruction), mobiGlas & OCR (auto-OCR watcher toggle, dual-read & cross-grab protection info), Aurora & Audio (voice assistant integration, volume slider, radar sonar-ping and RS target alerts), UEX Corp API key integration, and SQLite database maintenance (schema version, VACUUM, deep integrity check).
  - Ported **About View** (`AboutView.tsx`): Modern hero branding card with SCLogMate Photino Edition info, runtime specifications (.NET 10, Photino.NET, React 19, Tailwind CSS v4, SQLite), Ko-fi community support banner, SCVerse ecosystem showcase (SCLogMate, MobiNexus Bot, SCVerse Web Portal), tech stack overview, and Roberts Space Industries / Cloud Imperium Games community disclaimer.
  - Fully eliminated `PlaceholderView` from `App.tsx`: All 16 primary navigation tabs across the entire Star Citizen live companion suite are now fully implemented and connected to C# backend IPC endpoints via `PhotinoBridge.cs`.
- **Photino.NET & React Full Functional Migration — Phase 3 (`frontend/src/views/`, `Core/Photino/`)**:
  - Ported **Sternenkarte / Starmap View** (`StarmapView.tsx`): Interactive 2D radar starmap visualization with Stanton and Pyro solar systems, concentric orbital rings, glowing celestial bodies, layer filters (Stations, Moons, Landing Zones), zoom/pan controls, celestial body inspector, and integrated Quantum Jump route planner calculating exact flight time and GM/km distance for various quantum drives.
  - Ported **Orte & Stationen View** (`PlacesView.tsx`): Searchable directory of Star Citizen locations across Stanton and Pyro, categorized by planets, moons, landing zones, orbital stations, Lagrange refineries (L1-L5), and outposts with security ratings and armistice status.
  - Ported **Flugschreiber / Blackbox View** (`BlackboxView.tsx`): Flight telemetry recorder tracking total traveled distance (GM / km), active flight duration, quantum jumps, ship losses, sortie counts, visited celestial bodies, and a chronological flight timeline with event badges and Markdown export.
  - Ported **RS Signal Scanner View** (`OreScannerView.tsx`): Radar Signature (RS) decoder for mining ores and salvage panels, featuring an interactive RS value calculator (calculating node clusters, mineral types, and estimated aUEC value) alongside a full RS signature catalog table.
  - Ported **Handelsmarkt & UEX Rohstoffe View** (`MarketView.tsx`): Commodity market browser with buy/sell prices, margins per SCU, optimal trading routes, and an interactive cargo hauler profit calculator with ship presets (C2 Hercules, Caterpillar, Constellation Taurus, Freelancer MAX, Cutlass Black).
  - Expanded `PhotinoBridge.cs` with backend IPC endpoints: `get_starmap`, `calculate_route`, `get_places`, `get_blackbox`, `get_rs_signatures`, `decode_rs`, and `get_market`.
  - Integrated all Phase 3 views into `frontend/src/App.tsx`.
- **Photino.NET & React Full Functional Migration — Phase 2 (`frontend/src/views/`)**:
  - Ported **Aufträge & Missionen View** (`MissionsView.tsx`): Multi-tab mission tracker supporting active OCR contracts from mobiGlas, parsed historical mission completions with aUEC rewards, and the global CIG mission catalog with type filtering (Bounty, Delivery, Mercenary, Salvage, etc.) and search.
  - Ported **Ansehen & Fraktionen View** (`ReputationView.tsx`): Faction reputation dashboard categorizing organizations (Security & Bounty Hunting, Cargo & Logistics, Industry & Mining, Underworld & Syndicates) with XP progression bars, current rank titles, and total completed contract counts.
  - Ported **Baupläne & Crafting View** (`BlueprintsView.tsx`): Comprehensive Star Citizen crafting blueprint browser with status filter pills (All, Learned, Missing), category dropdown (Weapons, Armor, Components, Tools), rarity badge highlights, ingredient requirements, unlock sources, and completion progress bars.
  - Ported **Pilot Ausrüstung & Loadout View** (`LoadoutView.tsx`): Visual gear and pilot inspector with dual-column layout (Armor & Undersuit vs Weapons & Utilities), overall damage reduction calculation (-%), temperature protection ranges, armor class determination (Heavy, Medium, Light), and single-click Markdown export to clipboard.
  - Integrated all Phase 2 views into `frontend/src/App.tsx` navigation and verified compilation.
- **Photino.NET & React Full Functional Migration — Phase 1 (`Core/Photino/`, `frontend/`)**:
  - Implemented 16-tab categorized sidebar navigation matching the entire functional landscape of the Avalonia application (Chronik, Finanzen, Lager, Flotte, Aufträge, Ansehen, Starmap, Orte, Flugschreiber, RS Scanner, Markt, Baupläne, Ausrüstung, Werkzeuge, Einstellungen, Über).
  - Ported **Chronik / Events View** (`EventsView.tsx`): Real-time and historical event browsing with filter pills (All, Wallet, Combat, Missions, Ships, Locations, System), search bar, and detailed entry inspector drawer.
  - Ported **Finanzen & Saldo View** (`FinancesView.tsx`): Financial overview cards, transaction ledger, cargo & commodity trading log, and top expense breakdowns.
  - Ported **Lager & Bestandsverwaltung View** (`WarehouseView.tsx`): Dual-column planetary & station inventory management, location filtering, instant `+`/`-` stock adjustment buttons, context action menu (Freight Elevator -1, Dismantled -1, Delete), and Markdown export.
  - Ported **Flotte & Schiffe View** (`FleetView.tsx`): Fleet summary cards, flight counts, quantum jumps, losses, and last used timestamps.
  - Added dashboard overview (`DashboardView.tsx`) with quick navigation and placeholders (`PlaceholderView.tsx`) for upcoming phases.
  - Expanded `PhotinoBridge.cs` backend IPC handlers (`get_events`, `get_finance`, `get_warehouse`, `adjust_warehouse_qty`, `delete_warehouse_item`, `clear_warehouse_location`, `get_fleet`).
- **Warehouse Item Management & Manual Stock Adjustments (`Core/Database.cs`, `ViewModels/MainViewModel.Warehouse.cs`, `Views/MainWindow.axaml`)**:
  - Added direct item adjustment controls (`-` and `+` buttons) directly in the Warehouse grid quantity column for instantaneous manual stock corrections (e.g. when retrieving items via the freight elevator or consuming gear).
  - Added dedicated warehouse row action buttons and context menu entries:
    - `📦 Per Frachtaufzug entnehmen (-1)`: Quickly deducts 1 count of the selected item from station or planetary storage.
    - `➕ Menge um 1 erhöhen (+1)`: Increments the item quantity.
    - `🔧 Zerlegt / Dismantled (-1)`: Records an item dismantling action and deducts 1 from warehouse inventory.
    - `🗑️ Gegenstand aus Lagerbestand löschen`: Completely removes obsolete or lingering items from local storage.
  - Added `🗑️ Standort leeren` button in the warehouse toolbar to purge all inventory for the currently filtered location.
  - Added `Database.AdjustWarehouseItemQuantity`, `Database.DeleteWarehouseItem`, and `Database.ClearWarehouseLocation` SQLite operations.
- **Freight Elevator & Ship Elevator Live Event Detection (`Core/LogParser.cs`, `ViewModels/MainViewModel.cs`)**:
  - Implemented parser recognition for `CEntityComponentFreightElevatorUIProvider::FillUnstowRequest`, logging an `EventKind.Inventory` entry when items are requested and brought up via the freight elevator (`Frachtaufzug: N Gegenstände angefordert / hochgeholt`).
  - Added freight and ship elevator readiness tracking via `CSCLoadingPlatformManager::OnLoadingPlatformStateChanged` transitioning to `OpenIdle`, firing live `EventKind.Hangar` ready events and triggering the `ElevatorReady` toast notification overlay.
  - Bumped `CurrentParserVersion` to `34` in `Core/Database.cs`.

### Removed
- **Duplicate Database Indexing Top Banner (`Views/MainWindow.axaml`)**:
  - Removed the redundant top progress bar banner during database scanning (`IsDatabaseBusy`), retaining the dedicated central modal progress overlay (`Star Citizen Datenbank-Indexierung`).

## [1.0.0-rc2] - 2026-09-09
### Added
- **Planetary & Station Warehouse Inventory System (`Models/WarehouseItem.cs`, `Core/WarehouseCatalog.cs`, `Core/LogParser.cs`, `Core/Database.cs`, `ViewModels/MainViewModel.Warehouse.cs`, `Views/MainWindow.axaml`, `Core/I18n.cs`)**:
  - Implemented a persistent planetary and station storage system tracking all items stored or retrieved at locations (cities, outposts, space stations).
  - Deliberately filters out volatile ship inventories and temporary containers to provide a reliable ledger of items resting on planets and stations.
  - Implemented `WarehouseCatalog` translating internal CIG item classes (minerals, gems like Janalite and Aphorite, multi-tools, tractor beams, weapons, armor, medpens, quest items) into human-readable titles and organized categories.
  - Added SQLite schema migration `v16` creating `warehouse_items` table and indexes (`ix_warehouse_location`, `ix_warehouse_category`).
  - Bumped `CurrentParserVersion` to `33` to automatically index historical warehouse movements across all archived sessions.
  - Added new primary tab **📦 Lager / Warehouse** with dual-column layout: Left sidebar listing planets/stations with item count badges and quick filtering; Right data grid detailing item icon, readable name, CIG class, category badge, location, quantity badge, and last movement timestamp.
  - Added category filter chips (All, Minerals & Ores, Tools, Weapons, Armor, Consumables, Quest & Utility, Miscellaneous) and live search bar.
  - Added Markdown export (`ExportWarehouseMarkdownCommand`) for saving full inventory reports by location.
- **Freight Elevator, Shop, Mission Cargo & Refinery Warehouse Ingestion (`Core/LogParser.cs`, `Core/WarehouseCatalog.cs`)**:
  - **Freight Elevator & Inventory Grid Tracking**: Added support for modern 3.24+ / 4.0 inventory movements (`Type[Store]`, `Type[Stack]`, `Type[Split]`, and `<Update Container Items Add New Item>`) with case-insensitive `request[` matching, resolving an issue where inventory movements in modern logs were skipped.
  - **Location ID Mapping Fix**: Fixed inventory location resolution by binding `:Location:<id>` from `<Query Inventory>`, `Freight Inventory Grid`, and `FreightElevator` requests (e.g. Levski warehouse location `3723364946`), properly attributing cargo elevator operations to their respective planetary locations.
  - **Shop Transactions Ledger Sync**: Integrated shop buy (`SShopBuyRequest`) and sell (`SShopSellRequest`) events into the warehouse ledger (+qty for purchased components, tools, and consumables; -qty for items sold at terminals).
  - **Mission Cargo Deductions via Freight Elevator**: Integrated automatic cargo elevator drop-off deduction when mission delivery objectives complete (`MISSION_OBJECTIVE_STATE_COMPLETED` matching `SMarkerHandler_Hauling::OnItemRegistered`), accurately deducting delivered crates and packages from station/planetary storage.
  - **Refinery Handover Deductions**: Added automatic deduction of raw minerals and unrefined ore delivered to refinery kiosks upon refinery job creation.
- **Star Citizen Wiki API Integration & Item Class Resolution (`Core/WikiApiClient.cs`, `Core/WarehouseCatalog.cs`, `Core/Database.cs`, `Core/UnknownEventsLogger.cs`, `ViewModels/MainViewModel.Warehouse.cs`, `Views/MainWindow.axaml`)**:
  - **Direct SCWiki API Integration**: Integrated `https://api.star-citizen.wiki/api/v2/items?filter[class_name]=...` to resolve cryptic internal CIG asset identifiers (e.g. `alb_hat_01_01_17` -> `Ketchum Beanie Aqua`, `grin_utility_medium_helmet_01_01_04` -> `Aril Helmet Hazard`) with official in-game store names, manufacturers, German descriptions, and render images.
  - **Persistent SQLite Cache (`wiki_items_cache`, Schema v17)**: Added SQLite schema migration `v17` (`Database.CurrentSchemaVersion = 17`) introducing `wiki_items_cache` table and indexes, ensuring all fetched item definitions are saved locally for instantaneous, zero-latency offline lookups.
  - **Non-Blocking Background Prefetching**: Engineered a non-blocking queue in `WikiApiClient` that asynchronously prefetches unknown item classes in the background without causing stalls in log parsing or UI thread rendering.
  - **Intelligent Bilingual Fallback & Apparel Formatting**: Upgraded `WarehouseCatalog` with culture-aware (DE/EN) heuristic parsers formatting apparel design & colorway codes (e.g. `Alb Mütze (Design 01 · Farbe 17)`) and mission cargo containers while awaiting Wiki data.
  - **Unknown Item Class Diagnostic Logging**: Added automatic tracking of unresolvable item classes to `%APPDATA%\SCLogMate\SCLogMate.unknown.log` via `UnknownEventsLogger.LogUnknown("ItemClass", ...)`, pinpointing missing items for continuous catalog refinement.
  - **Interactive Wiki Details in Warehouse Tab**: Added double-click navigation and a context menu entry ("🌐 Im Star Citizen Wiki nachschlagen") on warehouse items, opening the full in-app Wiki overlay with 3D photos, stats, German lore, and direct web links.
  - **Session Log Name Resolution**: Enhanced `LogParser` purchase, sale, and inventory event detail messages to present readable Wiki names in the main session event log.

### Fixed
- **Timeline Re-Analysis Feedback & Global Refresh (`ViewModels/MainViewModel.cs`)**:
  - Fixed `RebuildTimeline` command to refresh timeline events from SQLite when in global scope (`TimelineScope == 0`) respecting fleet wipe filters.
  - Added real-time user feedback in the status bar indicating the total number of re-analyzed timeline events and distance traveled upon clicking "Neu analysieren".

## [1.0.0-rc1] - 2026-09-09
### Added
- **Database Version Overview & Structure Diagnostics (`Models/DatabaseDiagnosticsInfo.cs`, `Core/Database.cs`, `ViewModels/MainViewModel.Database.cs`, `Views/MainWindow.axaml`)**:
  - Implemented comprehensive database version tracking and integrity verification diagnostics for the local SQLite database (`sessions.db`).
  - Added real-time version comparison cards displaying installed vs. target Schema Version (`v11`), Parser Version (`v28`), SQLite engine version, and journal mode (WAL).
  - Added structural validation checking the presence and schema of all 7 SQLite tables (`sessions`, `events`, `contracts`, `user_pois`, `reputation`, `fleet_user_ships`, `meta`), critical columns, and 7 performance/composite indexes.
  - Added native SQLite physical integrity check (`PRAGMA quick_check;`) with detailed health status reporting.
  - Added metrics overview displaying record counts across all database entities (Sessions, Events, Contracts, Fleet Ships, Custom Waypoints, Reputation Factions).
  - Added interactive diagnostic actions: `CheckDbStructureCommand` to trigger live deep structural inspection, `RepairDbStructureCommand` to automatically restore missing tables, columns, indexes, and apply pending migrations, and `OpenDatabaseFileLocationCommand` to locate `sessions.db` in Windows Explorer.
  - Automatically runs diagnostic checks upon switching to the Database settings sub-tab and refreshes diagnostics following Rescan, Cleanup, or Reset operations.
- **Dedicated Unknown Events Logger & Diagnostic Actions (`Core/UnknownEventsLogger.cs`, `ViewModels/MainViewModel.cs`, `Views/MainWindow.axaml`, `App.axaml.cs`)**:
  - Implemented dedicated unknown event logger recording unhandled HUD notifications, uncataloged ship models, unresolved factions, and unmapped blueprints into `%APPDATA%\SCLogMate\SCLogMate.unknown.log`.
  - Added formatted frequency summary flusher (`UnknownEventsLogger.FlushSummary()`) triggered on application shutdown, post-rescan, and via UI diagnostics.
  - Added `OpenUnknownEventsLogCommand` and corresponding action buttons in the Database diagnostics header and the Storage & Diagnostics settings card in `MainWindow.axaml`.
- **Game Data Synchronization & Automation Tools (`tools/sync-fleet-catalog.ps1`, `tools/gen-missions.ps1`)**:
  - Implemented `tools/sync-fleet-catalog.ps1` to query `scunpacked-data/ships.json`, verify missing ships/vehicles against `FleetCatalog.cs`, and automate catalog maintenance for future Star Citizen patches.
  - Implemented `tools/gen-missions.ps1` to inspect and sync contract templates, factions, and rewards from `scunpacked-data` repository trees.
  - Expanded `FleetCatalog.cs` with the complete Argo Astronautics lineup (`Argo ATLS`, `MOLE`, `RAFT`, `SRV`, `MPUV`), ground vehicles (`Anvil Ballista`, `Centurion`, `Greycat ROC`, `ROC-DS`, `PTV`, `STV`, `Tumbril Cyclone` variants, `Nova Tank`, `Storm`, `RSI Ursa`, `Ursa Medivac`, `Lynx`), and modern combat ships (`Mirai Guardian`, `Guardian MX`, `Anvil Paladin`).
  - Added new modern Star Citizen 3.24+ / 4.0 reputation factions in `ReputationService.cs` (`Alliance Aid`, `Ling Family`, `Headhunters`, `Rough Animals`) with dedicated XP thresholds and faction matching logic.
  - Added modern contract profiles to `MissionCatalog.cs` for Cargo Hauling, Wikelo Collector introductions, and Pyro underworld operations.

### Changed
- **Ship Model Normalization & Archetype Noise Filtering (`Core/Ships.cs`, `Core/FleetCatalog.cs`, `ViewModels/MainViewModel.cs`, `Core/Database.cs`)**:
  - Fixed a bug where internal CIG mission archetype tags and spawn suffixes (such as `Salvage`, `Derelict`, `Wreck`, `Pirate`, `Civilian`, `Bounty`) were retained in prettified ship names (e.g. `MOLE Salvage · Argo` instead of `MOLE · Argo`), causing mining ships spawned for salvage missions to appear with corrupted model names.
  - Added archetype noise filtering in `Ships.NormalizeModelName` and expanded the internal noise tag set in `Ships.cs`.
  - Updated `MainViewModel.RebuildFleet` to group flight statistics by canonical catalog model name (`cat.NormalizedName`), consolidating split archetype entries into single clean fleet records.
  - Bumped SQLite database schema to `v14` (`Database.CurrentSchemaVersion = 14`) with automatic migration query to sanitize existing `MOLE Salvage` records in `events` and `fleet_user_ships` to `MOLE · Argo`.
- **Mission Ingestion Pipeline & Faction Statistics (`Core/LogParser.cs`, `ViewModels/MainViewModel.cs`, `Core/Database.cs`)**:
  - Fixed a critical bug where accepted contracts (`Contract Accepted: ...`) in HUD notifications were emitted with `EventKind.Mission` instead of `EventKind.MissionTaken`. Because `RebuildMissions` and SQLite database queries strictly filter for `EventKind.MissionTaken`, accepted contracts were omitted from the mission overview and faction reputation statistics.
  - Implemented automatic issuer/contractor extraction from notification titles (e.g. `Alliance Aid: ...`, `Red Wind: ...`, `Ling Family: ...`) when no static catalog match exists.
  - Added smart mission type derivation (`Fracht/Transport`, `Lieferung`, `Kopfgeld`, `Söldner`, `Bergung`, `Bergbau`, `Person/Bergung`) from contract titles.
  - Enhanced `RebuildMissions` in `MainViewModel` to gracefully support both dot-separated and colon-separated mission details.
  - Bumped SQLite database `CurrentParserVersion` from `30` to `31` to automatically re-index historical sessions from the archive and populate all accepted mission events.
- **Fleet Catalog O(1) Lookup & Substring Match Prioritization (`Core/FleetCatalog.cs`)**:
  - Optimized catalog lookup to execute an O(1) exact dictionary match before substring matching.
  - Pre-sorted catalog entries by key length descending (`SortedCatalog`) to prevent shorter model names (e.g. `Cutter`, `Cutlass`) from mistakenly matching before longer specific variants (e.g. `Cutter Rambler`, `Cutlass Black`).
- **LogParser Performance Optimization & Thread Safety (`Core/LogParser.cs`)**:
  - Implemented fast pre-filtering for HUD notification patterns (`isNotif`) and engine log prefixes (`SShop...`, `CActor::Kill`, `CSCItemNavigation`, etc.), bypassing up to 15 regex evaluations per log line.
  - Replaced ad-hoc runtime dynamic regexes (`CleanMissionTitlePrefixRegex`, `CamelCaseSplitRegex`) with compile-time source-generated regexes (`[GeneratedRegex]`).
  - Optimized timestamp parsing in `ParseTs()` to reuse universal timestamp cached in `_lastSeenTime` during `Feed()`, avoiding repetitive regex parsing and datetime string evaluations.
  - Consolidated duplicate location resolution and visit tracking across 5 separate code paths into `RecordLocationVisit()`.
  - Added `_stateLock` thread-safety guards for `ContractsList` snapshot generation and concurrent dictionary access.
- **Service & OCR Subsystem Performance Optimizations (`Core/AuroraVoiceService.cs`, `Core/ReputationService.cs`, `Core/Ocr/WalletOcrTrigger.cs`, `Core/FlightRecorderService.cs`, `Models/LogEntry.cs`)**:
  - Added fast notification pre-checks and compile-time `[GeneratedRegex]` source generators for ship channel detection in `AuroraVoiceService.ProcessLiveLine`.
  - Converted runtime dynamic regexes in `WalletOcrTrigger.ExtractBalance` to compile-time source-generated regexes (`OcrNoiseCharsRegex`, `SpaceThousandsRegex`).
  - Cached static frozen Avalonia `SolidColorBrush` instances in `FactionReputation` and `LogEntry` (`KindBadgeBg`, `KindBadgeFg`), eliminating hundreds of thousands of heap allocations when scrolling through large historical log tables.
  - Replaced LINQ calls with direct indexed list operations in `FlightRecorderService.BuildTimeline`.
- **UI Rendering Performance & Resource Optimization (`Views/FinanceTimelineChart.cs`, `Views/StarmapCanvas.cs`, `Views/ScanIndicatorWindow.cs`, `Views/RegionSelectorWindow.axaml.cs`, `App.axaml.cs`)**:
  - Cached static frozen Avalonia brushes, pens, and typefaces in `FinanceTimelineChart`, eliminating per-frame allocations during hover and pan interactions.
  - Implemented cached sorted data points in `FinanceTimelineChart` to avoid repeated LINQ sorting allocations on mouse movement.
  - Pre-allocated immutable starfield brushes and cached static drawing pens/brushes in `StarmapCanvas`, avoiding thousands of brush allocations per second during rendering.
  - Tied `StarmapCanvas._pulseTimer` to visual tree attachment and effective visibility, stopping unnecessary background rendering and timer ticks when the Starmap tab is inactive.
  - Replaced ad-hoc timer allocations in `ScanIndicatorWindow.FlashGreen` with a reusable timer instance and removed dead `MoveWindow` P/Invoke declarations in `RegionSelectorWindow` and `ScanIndicatorWindow`.
  - Added clean desktop application exit hook in `App.axaml.cs` to ensure `GlobalHotkey.Stop()` is invoked upon application termination.
- **Feature Freeze & Dependency Alignment (`SCLogMate.csproj`, `Core/Settings.cs`, `Core/Logger.cs`)**:
  - Declared Feature Freeze for the 1.0.0 Release Candidate phase with focus on stability, resilience, and clean resource management.
  - Upgraded dependencies to native .NET 10 runtime packages: `Microsoft.Data.Sqlite 10.0.11`, `System.Security.Cryptography.ProtectedData 10.0.11`, `CommunityToolkit.Mvvm 8.4.2`, and `Tmds.DBus.Protocol 0.95.1`.
  - Enabled `<PublishReadyToRun>true</PublishReadyToRun>` in `SCLogMate.csproj` for faster cold startup times in the self-contained single-file publish artifact.
  - Adopted .NET 10 / C# 13 `System.Threading.Lock` synchronization primitives across `Database`, `LogParser`, `MainViewModel`, `AuroraVoiceService`, `RsAudioAlertService`, `Settings`, and `Logger`, replacing all legacy object monitor locks and debouncing repetitive settings persistence log messages.

### Fixed
- **Star Citizen Game Version Detection & Status Header (`Core/LogParser.cs`, `ViewModels/MainViewModel.cs`)**:
  - Fixed an issue where the game version in the status header and tooltips was extracted from internal PE binary `FileVersion:` (e.g. displaying `v1.0.191.55227`) or with raw branch suffix noise (e.g. `v4.10.0-hotfix`).
  - Implemented extraction of official full Star Citizen release version strings (`<BaseVersion>-<Channel>.<BuildNumber>`, e.g. `4.10.0-LIVE.12572603`) by parsing numeric version from `Branch:`, mapping environment `PUB` to `LIVE`, and appending changelist build numbers.
  - Enhanced `ScVersionText`, `ScChannel`, `ServerSublineText`, and `ServerTooltipText` in `MainViewModel` to present the clean, official full game version without leading "v" or internal branch noise.
- **M80 / m80 Casing Normalization & Fleet Duplicate Resolution (`Core/Ships.cs`, `Core/FleetCatalog.cs`, `Core/Database.cs`, `ViewModels/MainViewModel.cs`)**:
  - Corrected lowercase `m80` model casing to uppercase `M80` across parser normalization and catalog aliases (`M80 · Origin`).
  - Added SQLite schema migration `v15` (`Database.CurrentSchemaVersion = 15`) consolidating split `m80 · Origin`, `Origin M80 · Origin`, and `M80 · Origin` entries in `events` and `fleet_user_ships`, preserving custom user pledge data ($300, 24 Monate insurance).
  - Enhanced `Database.GetAllFleetCustomData` and `Database.SaveFleetShipCustomData` to resolve canonical model names and deduplicate custom hangar entries while cleaning up obsolete legacy keys.
  - Guarded `MainViewModel.RebuildFleet` against injecting duplicate un-flown custom hangar ships by checking against canonical model names.
- **Notification Parser Robustness & Real-World Log Coverage (`Core/LogParser.cs`, `Core/FleetCatalog.cs`)**:
  - Fixed notification categorization failing on `Entered/Exited Monitored Space`, `Entered ... Jurisdiction` (People's Alliance, Ungoverned, microTech, Hurston, UEE), and `Private Property` due to rigid `StartsWith` checks; converted to flexible substring matching (`Contains`).
  - Added recognition for medical emergency services (`Emergency Services`), vehicle retrieval (`Retrieve`), money transfer offers (`has sent you`), hangar queue positions (`Hangar-Warteschlange`), journal entries, and refinery work order completions.
  - Filtered out chat channel join/leave notifications (`left the channel`) to prevent pollution of unknown event diagnostics.
  - Corrected `Origin M80` catalog entry to Origin Jumpworks (was mistakenly attributed to Aegis).
  - Validated parser against 50 real-world game sessions (35+ hours of gameplay), reducing unhandled notification occurrences from 129 down to 0.
- **Fleet & Hangar Pledge Synchronization & Data Persistence (`Models/ShipFleetItem.cs`, `Core/Database.cs`, `ViewModels/MainViewModel.cs`, `Views/MainWindow.axaml`)**:
  - Fixed a critical bug where ships marked as pledged or with acquisition cycled to "Pledge Store" or "In-Game (aUEC)" did not have `IsInHangar = true` set, causing them to be hidden from "Mein Hangar".
  - Bumped SQLite database schema to `v13` (`Database.CurrentSchemaVersion = 13`) with automatic migration query to ensure all existing records with `is_pledge = 1` or `acquisition IN ('Pledge Store', 'In-Game (aUEC)')` are marked with `in_hangar = 1`.
  - Added guard in `Database.SaveFleetShipCustomData` to automatically enforce `in_hangar = 1` whenever `is_pledge = true` or `acquisition` is "Pledge Store" or "In-Game (aUEC)".
  - Added reactive property change notifications (`OnIsInHangarChanged`, `OnAcquisitionTypeChanged`, `OnIsPledgeBoughtChanged`, `OnPledgeValueUsdChanged`, `OnInsuranceTypeChanged`) in `ShipFleetItem.cs`.
  - Unified telemetry and statistics updates via `NotifyFleetStats()` across all acquisition cycling, pledge editing, catalog additions, and view mode toggling in `MainViewModel.cs`.
  - Wired up `CycleShipInsuranceCommand` in `MainWindow.axaml` allowing users to cycle insurance levels (LTI ➔ 120M ➔ 24M ➔ 12M ➔ 6M) by clicking the badge, and removed an obsolete duplicate read-only "HERKUNFT & PLEDGE" column.
- **Auto-Updater SemVer Version Comparison (`Core/Updater.cs`)**:
  - Fixed a critical version comparison bug where pre-release suffixes (e.g. `-beta7`, `-rc1`) were ignored by the naive 3-int parser, causing the updater to treat `1.0.0-beta7`, `1.0.0-rc1`, and `1.0.0` as identical versions (`1.0.0.CompareTo(1.0.0) == 0`).
  - Implemented full SemVer 2.0.0 precedence rules recognizing pre-release ranks (`alpha < beta < rc < release`) and sub-versions (`rc1 < rc2`), ensuring users seamlessly receive updates to release candidates and the final 1.0.0 release.
- **Global Hotkey Message Loop Termination & Dynamic Toggling (`Core/GlobalHotkey.cs`, `ViewModels/MainViewModel.cs`)**:
  - Fixed an issue where stopping the global hotkey listener left the background thread blocked indefinitely inside Win32 `GetMessage()`. Added `PostThreadMessage(threadId, WM_QUIT)` to unblock the thread and ensure `UnregisterHotKey` is reliably called.
  - Connected `OnGlobalHotkeyEnabledChanged` in `MainViewModel` to immediately start or stop the listener when toggled in the UI settings.
- **Single-Instance Mutex Permission Resilience (`Program.cs`)**:
  - Added exception handling with fallback to local session namespace for `Global\SCLogMate_SingleInstance` mutex creation, preventing crashes in restricted or non-elevated user environments.
- **Atomic Settings Persistence (`Core/Settings.cs`)**:
  - Made `Settings.Save()` thread-safe with an internal lock and implemented atomic file replacement via temporary file swap to eliminate the risk of settings file corruption during sudden terminations.
- **HTML Flight Report Export Sanitization & Accuracy (`Core/HtmlReportGenerator.cs`, `Core/Refinery.cs`)**:
  - Corrected spelling error in report title ("FLUGSCHREIBER" instead of "FLUSCHSCHREIBER") and made version string dynamically reference `Updater.CurrentVersion`.
  - Added HTML entity encoding for all interpolated log titles, locations, and ship names to prevent broken formatting.
  - Fixed remaining time hour formatting in `RefineryJob.StatusText` for jobs taking 24 hours or longer (`(int)TotalHours`).
- **Starmap Player System Indicator Formatting (`Views/StarmapCanvas.cs`, `Core/StarmapData.cs`)**:
  - Fixed an issue where viewing a starmap for a different star system displayed an unformatted fallback `(Unbekannt)` location banner (`SPIELER IST IM NYX-SYSTEM (Unbekannt)`).
  - Omitted the location detail parentheses when the specific outpost or station within the star system has not yet been resolved (`SPIELER IST IM NYX-SYSTEM`).
  - Corrected `ResolvedLocation.DisplayName` default property value from `"Unbekannt"` to `"—"`, preventing premature fallback resolution before location events are parsed.
- **Shard Region Detection in Server Ping Service (`Services/ServerPingService.cs`)**:
  - Fixed an issue where Australian/Oceanic game shards (e.g. `pub-aus-...`) were incorrectly matched as US West due to `s.Contains("us")` being evaluated before Australia. Reordered regional detection and refined regional identifiers to prevent false routing.
- **Audio Playback Event Leak & Resource Cleanup (`Core/AuroraVoiceService.cs`, `Core/RsAudioAlertService.cs`)**:
  - Fixed an issue in `AuroraVoiceService` where `MediaEnded` and `MediaFailed` event handlers remained permanently subscribed if playback timed out after 8 seconds, causing orphaned handlers to fire and prematurely terminate subsequent audio cues.
  - Ensured `Windows.Media.Core.MediaSource` instances are reliably disposed upon playback completion.
  - Fixed TTS speech cut-offs and WinRT resource leaks in `RsAudioAlertService.SpeakTargetNameAsync` by introducing a persistent synthesizer and player lifecycle with full playback await and cleanup.
- **Flight Duration Calculation in Flight Recorder (`Core/FlightRecorderService.cs`)**:
  - Fixed total flight and session duration formatting in markdown export (`summary.TotalSessionDuration.TotalHours` instead of `Hours`) to correctly display durations exceeding 24 hours.
- **Contract Contractor Resolution & Deduplication (`Core/Missions.cs`, `Core/MissionCatalog.cs`, `Core/LogParser.cs`, `Core/Database.cs`, `ViewModels/MainViewModel.cs`, `ViewModels/MainViewModel.QuantumViews.cs`)**:
  - Fixed an issue where accepting missions from mission givers (e.g. `Battaglia_Generator` / `Battaglia_CreateUplink`) created duplicate entries with "Unbekannt" contractor and generic marker placeholder titles in the active contracts list.
  - Added explicit generator contractor mapping in `Missions.Faction` and `LogParser.FormatMissionGiver` to resolve `Battaglia` to `Recco Battaglia` and recognize Nyx / Levski mission systems and mission types (`Daten`, `Bergbau`, `Bergung`).
  - Added missing "Moraine Movements" contract definition to `MissionCatalog` under Recco Battaglia / People's Alliance in the Nyx system.
  - Updated `LogParser` to prefer HUD notification mission titles over marker fallback strings and keep existing contractor details when processing subsequent contract log lines.
  - Enhanced contract deduplication in `HandleMissionAccepted` and `SyncQuantumViewsFromParser` to seamlessly merge marker placeholders with live accepted contracts and update SQLite database entries.
- **False Ship Welcome & Vehicle Event on Leaving Pilot Seat (`Core/LogParser.cs`, `Core/AuroraVoiceService.cs`, `Core/Database.cs`)**:
  - Fixed an issue where standing up from the pilot seat in a ship (`CVehicleMovementBase::ClearDriver: releasing control token`) emitted a false-positive `EventKind.Vehicle` event, triggering the Aurora Voice welcome announcement inside the ship and inflating the flight recorder's sortie count.
  - Updated `LogParser` so `ClearDriver` log lines only update `_lastShip` without creating a vehicle spawn/entry log entry.
  - Added additional safeguards in `AuroraVoiceService` (`ProcessRawLine` and `ProcessLogEntry`) to ignore seat exit keywords (`ClearDriver`, `verlassen`, `Pilotensitz`, `releasing control token`) for ship greetings.
  - Bumped `CurrentParserVersion` to `30`.
- **Settings Sub-Tab Navigation Hook (`ViewModels/MainViewModel.Database.cs`)**:
  - Connected `OnSettingsSubTabIndexChanged` hook in `MainViewModel.Database.cs` to trigger automatic database structure diagnostics whenever navigating to the Database settings sub-tab (`SettingsSubTabIndex = 6`).
- **Win32 GDI Resource Leak Prevention (`Core/Ocr/ScreenCapture.cs`)**:
  - Encapsulated GDI device context, bitmap creation, and blitting in `ScreenCapture.Capture()` inside a robust `try-finally` block to guarantee strict cleanup order (`SelectObject(old) -> DeleteObject -> DeleteDC -> ReleaseDC`) even when memory allocations fail or exceptions occur.
- **CancellationTokenSource Resource Cleanup (`Core/Ocr/WalletCapture.cs`)**:
  - Disposed previous `CancellationTokenSource` instances prior to allocating new tokens when re-triggering mobiGlas wallet OCR capture bursts.

## [1.0.0-beta7] - 2026-09-06
### Fixed
- **Contract Abandonment & Cancellation Recognition (`Core/LogParser.cs`, `ViewModels/MainViewModel.cs`, `Core/Database.cs`)**:
  - Implemented parsing for Star Citizen engine `<EndMission>` log events (`CompletionType[Abandon]`, `CompletionType[Fail]`, `CompletionType[Deactivate]`, `CompletionType[Complete]`). Previously, abandoned missions were not removed because the parser expected non-existent HUD notifications rather than the actual `<EndMission>` engine messages.
  - Abandoned and failed missions now immediately resolve their contract title from active contracts or the mission catalog, trigger `HandleMissionCancelled`, remove the contract from the active contracts widget (`ActiveContracts`), purge it from the persistent SQLite `contracts` table, and update the mission history (`_rawContracts`) with status `ContractOutcome.Abandoned`.
  - Added robust multi-token and single-contract fallback resolution in `HandleMissionCancelled` and `HandleMissionCompleted`, ensuring active contracts are reliably cleared even if slight OCR or title phrasing discrepancies occur.
  - Bumped `CurrentParserVersion` from 27 to 28 in `Core/Database.cs` to re-index historical session contracts from the archive.
- **Startup Indexing & Database Index Resilience (`Core/Database.cs`, `Core/LogParser.cs`, `ViewModels/MainViewModel.cs`)**:
  - Ensured all composite indexes on `events` are automatically restored during `Database.Init()` and inside a `finally` block in `RescanAll()`, preventing unindexed table scans if a rescan was interrupted.
  - Added fast string pre-check guards in `LogParser.Feed()` for `<MissionEnded>` and `<EndMission>` to avoid executing regex matches on millions of irrelevant lines during bulk indexing.
  - Guarded `LoadGlobalDataAsync()` during startup to prevent competing background database queries while a background rescan/indexing operation is active.
  - Ensured visual progress percentage reflects active processing immediately (`Math.Max(1.0, ...)`) rather than displaying a static `0%` during initial file chunks.

## [1.0.0-beta6] - 2026-09-05
### Added
- **RS Radar Target Watchlist & Audio/Visual Alert System (`Models/RsTargetItem.cs`, `Core/RsAudioAlertService.cs`, `Core/Settings.cs`, `Views/RsScanOverlayWindow.axaml`, `Views/MainWindow.axaml`)**:
  - Implemented configurable material watchlist allowing mining and salvage pilots to select specific ores, gems, and salvage panels to hunt for.
  - Added acoustic Sci-Fi sonar chime alert (synthesized in RAM via 16-bit PCM WAV, 0ms latency, zero external sound files required) and optional native Windows TTS speech announcements.
  - Added intelligent contact-tracking and repeat-suppression logic: alerts fire once on initial target discovery and remain silent while the pilot maintains active radar pings or approaches the same target/cluster (45s contact hold, 60s same-resource area cooldown).
  - Added live full-text search (`RsTargetSearchText`) and category filter tabs (`Alle`, `⭐ High-Value`, `🪨 Schiffserze`, `🚗 ROC-Gems`, `💎 Hand-Gems`, `🏗️ Salvage`, `🎯 Nur Aktive`) to the target watchlist card.
  - Redesigned target watchlist items into modern interactive cards displaying tier badges, method icons, and base RS signatures, with active count indicator (`RsTargetCountSummary`) and filter-aware batch selection.
  - Added visual alert cues to the in-game overlay, including dynamic amber-gold border glow, target badge, and quick-toggle overlay header control.
- **Ship Maintenance, Refuel, Repair & Unlogged Expense Tracking (`Core/Database.cs`, `Core/Settings.cs`, `Models/LogEntry.cs`, `Models/LedgerRecord.cs`, `ViewModels/MainViewModel.cs`, `Views/MainWindow.axaml`)**:
  - Implemented automatic detection and booking of unlogged in-game expenses (ship repairs, hydrogen/quantum refuel, rearm, clinic visits, auto-load fees) via mobiGlas wallet OCR balance delta reconciliation.
  - Added dedicated quick expense booking card ("➕ Ausgabe erfassen") in the Spending tab with quick presets (`🔧 Reparatur`, `⛽ Tanken`, `🚀 Rearm`, `⏱️ Schiffsclaim`, `📦 Ladegebühr`, `🏥 Klinik`), location prefill, and persistent SQLite database recording.
  - Added `EventKind.Maintenance` with distinct badge styling and icons across event streams, ledger journals, and financial breakdown charts.
  - Enhanced `Database.Totals()` to include maintenance expenses in overall expenditure calculations.
- **Dedicated Tools & Backup Popouts (`Views/KeybindBackupWindow.axaml`, `Views/CloudBackupWindow.axaml`, `ViewModels/MainViewModel.Tools.cs`)**:
  - Created standalone Sci-Fi popout window `KeybindBackupWindow` for dedicated management of Star Citizen control mappings and `actionmaps.xml` backups (create with version note, inspect timestamps/sizes, restore, dual local and cloud replication).
  - Created standalone Sci-Fi popout window `CloudBackupWindow` for cloud storage configuration (OneDrive, Dropbox, Nextcloud), ZIP log bundle export, and automatic cloud replication.
  - Added `OpenKeybindPopoutCommand` and `OpenCloudPopoutCommand` to `MainViewModel`.

### Changed
- **Tools & Maintenance Tab Restructuring (`Views/MainWindow.axaml`, `Core/I18n.cs`, `ViewModels/MainViewModel.Tools.cs`)**:
  - Reorganized the cluttered single-page Tools tab into two distinct, structured sub-tabs:
    1. `🧹 Wartung & System` (`SubTab_ToolsMaintenance`): Focused strictly on system health, cache cleaning (DirectX & Vulkan shaders), crash dump clearance, hardware telemetry (RAM, SSD, SC version), Explorer folder shortcuts, and quick launcher cards for tool studios.
    2. `💾 Backups & Tresor` (`SubTab_ToolsBackups`): Dedicated vault hub bringing all backup operations into one organized place (Keybind vault with actionmaps.xml history and restore, user.cfg version snapshots and rollbacks, cloud synchronization and ZIP log exports).
  - Added popout buttons to each tool card so users can seamlessly switch between the tab view and standalone popups.
- **RS Radar Auto-Scan Instant Catalog Emission & Slashed-Zero OCR Recovery (`Core/Settings.cs`, `ViewModels/MainViewModel.cs`, `Core/Ocr/RsOcrScanner.cs`)**:
  - Implemented immediate single-tick emission for verified catalog ore signatures (`RsDecoderCatalog`), providing instant 0-delay display upon radar ping without requiring multi-frame confirmation on transient pulses.
  - Optimized the background OCR loop interval to 200 ms, giving radar pings 7+ scanning opportunities while keeping CPU footprint under 1%.
  - Added robust recovery for Star Citizen slashed-zero (`Ø`) HUD characters where Windows OCR produces `e` / `E` (e.g. `7*2ee` -> `7.200`, `2.eee` -> `2.000`, `Breøø` -> `8.000`), converting character context seamlessly to zero.
  - Implemented non-catalog signature debouncing with a 3-tick grace period to avoid flicker while ensuring uncataloged pings remain stable.
- **RS Radar Overlay Modernization & Realistic Yield Pricing (`Views/RsScanOverlayWindow.axaml`, `Models/RsSignalMatch.cs`)**:
  - Completely redesigned the in-game RS Radar Overlay into a compact, modernized Sci-Fi HUD widget, preventing viewport obstruction during flight while rendering a large, prominent resource title (22px bold cyan).
  - Replaced oversized text buttons with streamlined micro-controls (`🎯` region calibrate, `👁` box indicator, `⚡` auto-scan toggle pill, `✕` close).
  - Fixed layout clipping and right-border overflow by adopting a constrained 3-column grid (`Auto,*,Auto`), automatically hiding the refinery chip when no refinery applies (e.g. salvage panels), and shortening value labels (`ca. 25.000 aUEC`).
  - Fixed unrealistic profit/yield calculation in `RsSignalMatch.cs` that previously multiplied every ore by a full 32 SCU of 100% pure material.
  - Display now transparently shows realistic standard market rates (e.g. `16.000 aUEC / SCU` for Beryl, `88.000 aUEC / SCU` for Quantanium) and realistic panel yield estimates for salvage (`ca. 25.000 aUEC per panel`).

### Removed
- **Unused Core files & dead code cleanup**:
  - Removed unreferenced `Core/LootValuation.cs` item valuation helper.
  - Removed redundant `Database.SessionCount()` method (duplicate of `GetSessionCount()`) and obsolete `Database.RecentEvents(int n)` query in `Core/Database.cs`.
  - Removed unreferenced `RefineryMethod` enum in `Core/Refinery.cs`.
  - Removed unused static `CyanBrush` field in `Views/ScanIndicatorWindow.cs`.
  - Documented `Tmds.DBus.Protocol` in `SCLogMate.csproj` with explanatory comment as transitive security vulnerability override for `Avalonia.Desktop` (GHSA-xrw6-gwf8-vvr9 / NU1903).
  - Removed redundant unused `Window.Resources` converter instances and unused `xmlns:conv` namespaces in `Views/UserCfgEditorWindow.axaml`, `Views/RsScanOverlayWindow.axaml`, and `Views/StarmapWindow.axaml`.

### Fixed
- **Voice Audio Playback & Integration Reliability (`Core/AuroraVoiceService.cs`)**:
  - Fixed false-positive death/crash detection where entering pilot seats (`ClearDriver`), physics engine collisions, or UI entity destruction falsely triggered a 120-second mute cooldown on ship greetings.
  - Eliminated persistent 15-minute `IsAtStation` lock-out that previously suppressed safety zone (armistice), jurisdiction, monitored space, and restricted zone voice announcements.
  - Separated cooldown keys for safety zone entry (`safety_enter`) and exit (`safety_leave`) with 30s intervals, ensuring exit announcements are never suppressed by preceding entry announcements.
  - Implemented asynchronous sequential audio queue (`Channel<(string, int)>`) with 200 ms natural inter-announcement spacing, eliminating race conditions and audio cut-offs when concurrent events occur.
  - Added robust wildcard directory discovery for sound catalog folders and detailed startup diagnostic logging.
- **RS Decoder OCR Pipeline & Instant Display Fix (`Core/Ocr/RsOcrScanner.cs`, `Core/Ocr/OcrEngineService.cs`, `Core/Ocr/ScreenCapture.cs`)**:
  - Resolved recognition failures and multi-second detection delays in the RS Decoder / Mining Radar Scanner.
  - Fixed calibrated scan regions being shifted and distorted by artificial safety margins; `RsOcrScanner` now captures the exact calibrated boundary directly.
  - Replaced aggressive contrast binarization `Math.Clamp(255 - (maxColor - 45) * 3)` in `OcrEngineService` with a smooth 1.4x contrast multiplier (`Math.Min(255, (255 - bgra[src]) * 14 / 10)`), preserving font anti-aliasing required by Windows OCR to distinguish digits like 6, 8, 9, and 0.
  - Standardized image scaling to 6× with 24px padding on single-pass inverted HUD captures, ensuring small 10-14px HUD fonts scale to ~70px without slowing down OCR (~25 ms per tick on calibrated regions).
  - Implemented continuous digit-run parsing (`SplitThousandsRegex` and 4–6 digit extraction across 2,000–200,000 RS) with fallback Priority 5, preventing non-cataloged signatures, salvage hulls, or ping echoes from being silently discarded with `return null`.
  - Fixed Star Citizen slashed zero (`Ø`) misinterpretation where OCR read trailing zeroes as `8`, `6`, or `B` (e.g. `3,400` Lindinium read as `3.480`, `3.488`, `3.468`, or `3.466`), adding automatic slashed-zero candidate recovery and glyph sanitization (`Ø`, `ø`, `*`, `'`) that cleanly resolves to exact ore catalog signatures.
  - Replaced the timer-based debounce with a 2-consecutive-tick confirmation mechanism, ensuring immediate ~150 ms display upon ping and instant reset when the signal vanishes.
  - Enforced concurrency invariants in `RsOcrScanner` with an `Interlocked`-guarded `_busy` flag preventing overlapping timer ticks.
  - Added diagnostic logging for raw OCR output so undetected text is visible in `%APPDATA%\SCLogMate\SCLogMate.debug.log`.
- **App Startup Freeze & Spinning Wait Cursor Fix (`ViewModels/MainViewModel.cs`, `Core/Database.cs`)**:
  - Fixed application startup hang and spinning hourglass cursor when starting with "Alle Sessions" selected.
  - Eliminated duplicate unmetered archiving and indexing (`LogArchive.Sync` and `Database.IndexNew`) inside `LoadAllSessions()`, which competed with `AutoSyncAndIndexDatabaseAsync()`, held SQLite write locks silently, and caused the UI to freeze without progress feedback.
  - Replaced full event table dump (`Database.LoadAllEvents().ToList()`) in `LoadAllSessions()` with `Database.LoadRecentEvents(15000)`, preventing Avalonia UI dispatcher thread freezes when rendering archives containing hundreds of thousands of events.
  - Deferred the initial All-Sessions event load in the `MainViewModel` constructor until `AutoSyncAndIndexDatabaseAsync()` finishes its check and background index, ensuring smooth startup with live progress bar feedback.
- **Database Lock Crash & Rescan Performance Fixes (`Core/Database.cs`, `ViewModels/MainViewModel.cs`, `Core/LogParser.cs`, `Core/Logger.cs`)**:
  - Fixed fatal crash (`SqliteException: SQLite Error 5: 'database is locked'`) caused by concurrent writes and competing background tasks on startup (`LoadGlobalDataAsync` running `IndexNew` while `AutoSyncAndIndexDatabaseAsync` was executing `RescanAll`).
  - Added process-wide `_writeLock` synchronization to all SQLite mutating operations (`Init`, `RescanAll`, `IndexNew`, `ClearAll`, `SetMeta`, `SaveContract`, `RemoveContract`, `ClearActiveContracts`, `SaveUserPoi`, `DeleteUserPoi`, `AddFactionReputationXp`, `SaveFleetShipCustomData`, `Cleanup`).
  - Configured `Default Timeout=60;` in SQLite connection string and 60-second busy timeout so concurrent reads/writes queue gracefully.
  - Eliminated duplicate log sync and indexing from `LoadGlobalDataAsync()`, delegating all background indexing exclusively to `AutoSyncAndIndexDatabaseAsync()`.
  - Dramatically accelerated bulk log indexing (`RescanAll`) by dropping all composite indexes on `events` (`ix_events_session`, `ix_events_kind`, `ix_events_time`, `ix_events_session_kind`, `ix_events_kind_time`) before import and recreating them in batch afterwards.
  - Removed slow blocking `VACUUM` from `ClearAll()`, preserving it only for manual user cleanup.
  - Optimized file I/O buffer to 64KB in `ReadShared` and added early length/bracket guard in `LogParser.Feed` to skip millions of unnecessary regex matches on non-timestamped lines.
  - Ensured `Database.WasParserResetRequired = false` resets after rescan completes, and added stack trace logging to `Logger.Error`.

- **Contractor / Mission Giver Recognition & Removal of "mobiGlas" Fallback (`Core/LogParser.cs`, `Core/MissionCatalog.cs`, `ViewModels/MainViewModel.cs`, `ViewModels/MainViewModel.QuantumViews.cs`, `Core/Database.cs`)**:
  - Eliminated "mobiGlas" as a fallback issuer in the Contracts table and Active Contracts HUD. mobiGlas is the in-game wearable interface, not the contractor.
  - Added parsing for modern Star Citizen 4.x comms notification log lines (`<CommsNotifications> SendCommsNotification +Missions.Organization... Mission: [...]`) to extract the exact mission giver (e.g., `Recco Battaglia`) and faction (e.g., `People's Alliance`).
  - Added catalog definitions for Recco Battaglia missions at Levski / Nyx (`Extra Special Job`, `Missing Persons`, `Missing Mining Team`, `Minor Mining Job`, `Blackbox Retrieval`, `Salvage Job`, etc.) to `Core/MissionCatalog.cs`.
  - Added helper methods `FormatMissionGiver`, `FormatFaction`, `ResolveIssuer`, and `ResolveMissionSystem` in `Core/LogParser.cs` for clean formatting, star system determination, and fallback resolution ("Unbekannt" instead of "mobiGlas").
  - Added SQLite schema migration v11 in `Core/Database.cs` to purge legacy "mobiGlas" contractor entries from the `contracts` table, and bumped `CurrentParserVersion` from 26 to 27 to re-index historical session contracts from the archive.

- **Ingame Scan-Box Toggle (`Views/ScanIndicatorWindow.cs`, `ViewModels/MainViewModel.cs`)**:
  - Fixed `ToggleScanBoxCommand` not showing the visual scan box on screen by adding the missing `_scanIndicator.Show()` invocation.
  - Updated `ScanIndicatorWindow` to override `Show()`, immediately reapplying physical Win32 bounds (`SetWindowPos`) upon becoming visible.
  - Added a `Closing` cancellation guard so closing the indicator window safely hides it without disposing the underlying window instance.
  - Ensured manual region calibration in `RegionSelected` refreshes and shows the active scan box if `ShowScanBox` is enabled.
- **mobiGlas aUEC OCR Balance Recognition (`Core/Ocr/WalletCapture.cs`, `Core/Ocr/OcrEngineService.cs`, `Core/Ocr/WalletOcrTrigger.cs`)**:
  - Fixed false/truncated balance recognition (`2.349 aUEC` instead of `2.349.289 aUEC`) caused by artificial -25px/+50px region padding that grabbed outer UI artifacts and borders.
  - Upgraded OCR scaling to 6× (`scale: 6, padding: 24`) and introduced unboosted invert/plain preprocessing (`boostContrast: false`) calibrated against real mobiGlas text rendering, preventing contrast saturation from mangling small trailing digits (e.g. `289` turning into `æ` or `2"`).
  - Hardened `WalletOcrTrigger.ExtractBalance` with atomic candidate matching (`CandidateNumberRegex`) to discard contaminated or letter-touched runs whole rather than backtracking into truncated numbers.
  - Added strict thousands separator validation (groups after the first must have exactly 3 digits; trailing or leading separators reject the candidate).
  - Filtered out clock timestamps (`14:02`), normalized glued currency tokens (`aUEC2.349.289`), and supported space-separated thousands groups (`2 463 039`).
- **Missions Tab Population & Contract Notification Parsing**:
  - **`Core/LogParser.cs`**: Added parsing for modern Star Citizen contract notification log lines (`Contract Accepted`, `Contract Complete`, `Contract Failed`, `New Objective`, etc.) with `MissionId: [...]` attribute extraction. Cleaned titles, enriched mission metadata from `MissionCatalog`, and populated `_contracts` to display missions in the Contracts table.
  - **`ViewModels/MainViewModel.QuantumViews.cs`**: Enhanced `SyncQuantumViewsFromParser()` to merge active contracts from `ActiveContracts` (e.g. from OCR or database) into `_rawContracts` and update facet views.
  - **`ViewModels/MainViewModel.cs`**: Synchronized live contract events (`HandleMissionAccepted`, `HandleMissionCompleted`, `HandleMissionCancelled`, and `RemoveContract`) directly with `_rawContracts` and `UpdateContractsView()`.
  - **`Core/Database.cs`**: Bumped `CurrentParserVersion` from 25 to 26 to trigger re-indexing of historical logs from the archive.
- **Persistent & Session-Independent Account Balance**:
  - **`ViewModels/MainViewModel.cs`**: Decoupled `LiveBalanceText` from temporary session event scoping, making the primary **KONTOSTAND (aUEC)** card display the player's true current balance across all sessions.
  - Automatically persisted real-time financial progression (mission rewards, trades, purchases, fines) to `_settings.Balance` and SQLite `meta` table (`current_wallet`).
  - Initialized `LiveBalanceText` and `ManualBalance` immediately upon startup from saved settings or database meta.
  - Preserved the current account balance across session switches, app restarts, and log rotations, while keeping session-specific indicators (Income, Spend, Net) scoped to the selected session.
- **XML Documentation comments (CS1570)**:
  - Fixed unescaped `&` characters in XML doc comments across `Core/MissionCatalog.cs`, `Core/Settings.cs`, `Services/ImageLoaderService.cs`, and `ViewModels/MainViewModel.cs`.
- **Starmap & System Detection (Stuck in Nyx)**:
  - **`Core/Locations.cs`**: Fixed Stanton delving facilities (`Onyx Facility S1A3`, `S3B6`, etc.) being falsely matched by `TryNyx` due to substring matching on "nyx", which permanently poisoned the global `ActiveSystem` to Nyx.
  - **`Core/Locations.cs`**: Corrected `NyxGatewayRegex` so gateway stations on the Stanton side (`Stanton_Nyx_JPStation`) remain in Stanton.
  - **`Core/Locations.cs`**: Added spacing variants in `Cities` dictionary and `TryWellKnownCity` (`New Babbage`, `Area 18`, `Area 061`) for instant parent planet resolution.
  - **`Core/LogParser.cs`**: Quantum route system detection (`QuantumRouteRegex` & `_pendingQtDestination`) now uses full location resolution via `Locations.ResolveLocation()`, reliably detecting Stanton for landing zones (`New Babbage`, `Lorville`, `Orison`, `Area 18`) and stations that omit the word "Stanton".
  - **`ViewModels/MainViewModel.cs`**: Fixed biased jurisdiction lookup in `ApplyAggregate` that filtered out Stanton/UEE jurisdiction events and jumped backwards into historical sessions to find old Nyx/Pyro events.
  - **`Views/StarmapCanvas.cs`**: Player radar beacon ("📍 DU BIST HIER") now validates star system affinity and resolves sub-locations hierarchically to their parent celestial body. If the player is in another system, an unobtrusive indicator is displayed in the HUD chrome.


## [1.0.0-beta5] - 2026-09-04
### Added
- **Star Citizen Tools & Maintenance Suite (`🛠 Tools`)**:
  - **Shader-Cache & Crash-Dump Cleaner**:
    - Calculates and displays total occupied disk space for Star Citizen DirectX and Vulkan shader caches (`%LOCALAPPDATA%\Star Citizen\*\Shaders` and `vulkanshadercache`).
    - 1-click safe cleaning of shader caches to eliminate stuttering and graphic glitches after game updates without touching user settings.
    - 1-click cleaning of crash dumps (`.dmp` files in `%LOCALAPPDATA%\Star Citizen\crashes`).
  - **Keybind-Tresor (actionmaps.xml Backup & Restore with Cloud Sync)**:
    - 1-click backup of current active `actionmaps.xml` and custom mappings from `USER\Client\0\Controls\Mappings\*.xml` with optional custom note and timestamp.
    - Dual storage: saves backups locally and automatically replicates them to the configured Cloud storage path (`<CloudPath>\SCLogMate\Keybinds\`).
    - Historical backup manager: lists all local and cloud backups with creation dates, file counts, and storage locations; allows 1-click restore directly into the Star Citizen LIVE directory.
  - **Universal Cloud Storage & Log Backup**:
    - Configurable Cloud sync directory (OneDrive, Nextcloud, Google Drive, Dropbox, or custom network drive) with native Windows folder picker dialog (`[Durchsuchen...]`) and auto-save.
    - 1-click export of all historical game logs and SCLogMate archive logs into a compressed ZIP archive.
    - 1-click synchronization of all log backups to `<CloudPath>\SCLogMate\Logs\`.
  - **`user.cfg` Tuning Studio (Dedicated Popout Window)**:
    - Standalone popout window (`UserCfgEditorWindow`) with an ergonomic 3-column studio layout for undisturbed tuning with maximum screen space.
    - **1-Click Quick Presets**:
      - *High FPS / E-Sport* (165 FPS limit, 8 GB VRAM stream pool, DisplayInfo level 1, VSync Off, Motion Blur Off).
      - *Graphics & Immersion* (Unlimited FPS, 12 GB VRAM stream pool, DisplayInfo Off, VSync On).
      - *Minimal / Safe* (60 FPS limit, 4 GB VRAM stream pool, DisplayInfo level 1, VSync Off).
    - Full-text syntax editor with monospace font (`Cascadia Code`), UTF-8 encoding support, and helpful engine tuning hints.
    - **Automatic & Manual Version Archive**:
      - Automatically creates a timestamped archive snapshot in the local archive (`%APPDATA%\SCLogMate\config_backups\`) and immediately replicates it to the configured Cloud storage (`<CloudPath>\SCLogMate\Config\`) before each save operation.
      - Manual `[💾 Archive Current State]` button for snapshots prior to patch days or experimental tweaks.
      - Version history drawer with creation timestamps, storage location badges (Local / Cloud), file sizes, and 1-click in-editor inspection (`[📄 Inspect in Editor]`).
      - 1-click rollback/restore (`[↺ Restore to LIVE]`) of any archived configuration snapshot directly into the Star Citizen LIVE directory (with automatic `.bak` safety backup of the active file).
  - **Redesigned Tools & Maintenance UI (`🛠 Tools`)**:
    - Futuristic Sci-Fi dashboard featuring a telemetry hero header and 4 live KPI metric cards (Shader Cache MB, Crash Dumps MB, Keybind Backups, user.cfg status & popout trigger).
    - Balanced 2-column layout eliminating crammed fields: System Maintenance, Cloud Sync, and Hardware Telemetry on the left; Engine Tuning Hub, Keybind Vault, and Windows Explorer Quick-Access on the right.
  - **System-Check & Star Citizen Diagnostics**:
    - Real-time RAM memory status and adequacy check (optimal 32 GB vs minimum 16 GB).
    - Star Citizen installation drive type detection (internal SSD / NVMe vs HDD) and free disk space tracking.
    - Quick-access buttons to open Star Citizen LIVE, logbackups, LocalAppData, and SCLogMate directories directly in Windows Explorer.
- **Contracts View (`🎯 Aufträge`)**:
  - Comprehensive mission lifecycle tracking: Accepted → Completed or Abandoned/Failed, with progress steps (`StepsDone/StepsTotal`), rewards in aUEC, mission duration, and status color badges.
  - Interactive multi-dimensional facet filtering: filter by mission issuer (faction/corporation with counts) and mission type (Bounty, Delivery, Mercenary, Salvage, etc.) using facet pills, plus outcome status (All, Completed, Abandoned, Active) and real-time search.
  - KPI summary header showing total accepted contracts, completed count, abandoned count, and completion rate %.
- **Places & Quantum Destinations (`📍 Orte`)**:
  - Dedicated Locations tab featuring top most-visited stations, cities, outposts, and planets with visit counts, star system classification, location type, last visit timestamp, and quick-jump button to locate on the Starmap.
  - Quantum destination analytics tracking top quantum travel jump targets with jump frequencies and last jump timestamps.
  - Time period filtering (All time, 30 days, 7 days, 24 hours, current session) and keyword search.
- **Spending Analytics (`💳 Ausgaben`)**:
  - Confirmed purchases breakdown by shop, purchased item, and category with visual horizontal comparison bars.
  - Dedicated table of confirmed purchases parsed from `<CEntityComponentShoppingProvider::RmShopFlowResponse>` (`result[Success]`) with unit and total prices in aUEC, quantities, shop terminal names, and back-tracked locations.
  - KPI cards for total spend, purchase counts, and average spend per purchase.
- **Ledger Journal (`📑 Ledger`)**:
  - Exhaustive financial ledger detailing every money flow (cargo sales, cargo purchases, shop buys, player transfers, mission rewards, fines) back-tracked to the exact location and terminal where the transaction occurred.
  - Color-coded transaction badges, chronological running balance calculation, and quick filters by transaction kind.
- **Cargo Trades (`📦 Fracht`)**:
  - Dedicated commodity trading log tracking purchases and sales in SCU and aUEC, with unit price per SCU, profit/loss badges, terminal and ship tracking.
  - Direct deep-linking button to inspect any traded commodity in the Market tab.
  - KPI cards summarizing net trade profit, traded SCU volume, trade count, total buy spend, and total sell revenue.
- **Market & Commodity Catalog (`📊 Markt`)**:
  - Commodity catalog joined with the player's personal trading history, tracking best sell and best buy prices, profit margins per SCU, user trade volume, and total revenue.
  - Category filters (Metals, Minerals, Salvage, Gases, Medical, Vice/Drugs, Agricultural) and search.
  - **UEX Corp Live Market Data & Last Updated Indicator**:
    - Integrated live UEX Corp API endpoint (`commodities_prices_all`) to fetch real community commodity prices, best buy/sell counters, and price averages across all tradeable goods.
    - Added local disk cache (`%APPDATA%\SCLogMate\uex\commodities_prices_cache.json`) with a 1-hour expiration period to respect API rate limits and support offline usage.
    - Added visible status badge in the Market tab and Commodity Detail header showing the exact timestamp when market data was last updated (`UEX Stand: dd.MM.yy HH:mm`).
    - Added interactive `[ 🔄 Aktualisieren ]` button in the Market header to fetch fresh UEX data on demand.
    - Commodity Detail view now displays genuine live UEX terminal listings with real buy/sell prices, stock, and demand across Stanton and Pyro.
- **Deep-Linkable Commodity Detail View**:
  - Detailed single-commodity view opened from the Market or Cargo tabs: best sell price, best buy price, maximum margin, total player SCU sold, and revenue.
  - Complete list of trade terminals across Stanton and Pyro with buy/sell rates, price difference against the best rate, stock/demand, and policed vs. lawless jurisdiction badges.
  - Player's personal receipts history for the selected commodity.
- **Fleet Directory Modernization (`Card Grid`)**:
  - View switcher allowing pilots to toggle seamlessly between a modern holographic ship card grid (`🗂 Cards`) and the classic detailed DataGrid (`📑 Table`).
  - Sci-Fi ship cards with dynamic manufacturer signature colors (Drake, Aegis, Crusader, Anvil, RSI, MISC, Origin, Argo, Mirai), manufacturer badge, ship role classification, hangar favorite star, sortie count, quantum jumps, and loss ratio.
  - Fleet Role HUD breakdown displaying real-time distribution across `Combat / Gunships`, `Cargo / Transport`, `Industrial / Mining / Salvage`, and `Exploration / Recon`.
- **Event Feed Sci-Fi Upgrade**:
  - `● LIVE TAIL` real-time status pill in the toolbar indicating live Star Citizen log file monitoring.
- **Discord Quick-Share Snapshots**:
  - `📸 Discord Copy` action buttons added to the **Fleet** and **Finances** tabs, formatting fleet metrics or financial reports as sleek YAML/Markdown codeblocks copied straight to the clipboard for instant sharing in Discord and web communities.
- **Interactive Standalone HTML Flight Report**:
  - New `🌐 HTML-Report` export button in the Flight Recorder generates an independent, responsive HTML5 document styled with embedded dark Sci-Fi CSS, KPI cards, and flight chronology via `Core/HtmlReportGenerator.cs`, opening automatically in the default browser.
- **Flight Recorder Vector Timeline**:
  - Continuous vertical glowing vector waypoint path connecting hangar takeoffs, quantum departures, station arrivals, and incidents into a unified navigation route.
- **About Tab Redesign & Ko-fi Community Support**:
  - Prominent Ko-fi donation button (`☕ Ko-fi Spende`) embedded directly in the hero header linking to `https://ko-fi.com/goover`.
  - Streamlined, clean, and uncluttered layout removing redundant proforma changelog grids in favor of a direct CHANGELOG viewer button and system diagnostic links.

### Fixed
- **Star System & Location Detection in Nyx / Pyro / Stanton (`Core/Locations.cs`, `Core/LogParser.cs`)**:
  - Fixed an issue where players in the Nyx system were incorrectly detected and displayed as being in Stanton (e.g. at Levski, Delamar, or Nyx mission beacons).
  - Implemented dynamic active system tracking (`Locations.ActiveSystem`) and route origin extraction from `<Calculate Route> ... Projected Start Location is <System>`.
  - Added jurisdiction-based system detection for `People's Alliance (Nyx)` and `Ungoverned Jurisdiction`.
  - Fixed Landing Zone resolution for Levski on Delamar: `levski_all-001`, `levski_v2`, and other variants now resolve cleanly to `Levski`, `Nyx`, `Delamar` with Landing Zone classification and active armistice zone.
  - Prevented transient mission beacons (`MISSION_QT_Quantum_Beacon_LongRange_*`, `NavPoint_*`) from overwriting real player locations or falsely defaulting to Stanton.
  - Bumped `Database.CurrentParserVersion` to 25 to ensure historical sessions are re-parsed with accurate system and location tags.
- **Ore Radar & RS Signal Scanner OCR Recognition (`Core/Ocr/RsOcrScanner.cs`, `Models/RsSignalMatch.cs`, `Views/RsScanOverlayWindow.axaml`, `Views/ScanIndicatorWindow.cs`)**:
  - High-value cluster signature recognition (up to 16 nodes / ~52.000 RS): expanded the ship mining cluster ceiling (`CheckRs` in `Models/RsSignalMatch.cs`) from 2-6 nodes to up to 16 nodes. Asteroid clusters containing 3x-12x ores (e.g. 10.200 RS for 3x Lindinium, 21.350 RS for 5x Savrilium + Lindinium, 9.510 / 12.680 RS for 3x/4x Quantanium, up to 28.800+ RS for Bexalite clusters) are now fully recognized and decoded instead of being rejected.
  - HUD noise & bogus number suppression: removed raw candidate fallback in `ExtractRsValue` so uncataloged random numbers from ship crosshairs, coordinates, velocities, or roll angles are no longer falsely emitted as valid RS signatures.
  - Distance & velocity token filtering: added HUD readout filtering in `SanitizeOcrText` (`km`, `m/s`, `bn`, `deg`, etc.) preventing target range markers (e.g. `2.0km`, `16.5km`) from polluting OCR numbers or triggering phantom Salvage Panel detections.
  - Fixed OCR trailing HUD bracket noise and zero recovery replacement syntax (`${1}${2}0`).
  - Overlay UI redesign & text overflow fix (`RsScanOverlayWindow.axaml`):
    - Completely resolved text overflow where long ore names or location subtitles ran outside the overlay boundaries by replacing unconstrained horizontal StackPanels with responsive, strict-width Grid rows and functioning `CharacterEllipsis`.
    - Modernized glassmorphic HUD styling with a widened viewport (450px x 192px), deep navy glass canvas (`#F2081120`), glowing cyan telemetry header, and isolated telemetry badges for refinery bonus station and estimated market yield in aUEC.
  - Integrated complete resource database: embedded and dynamically loaded all 39 Star Citizen harvestable resources (`seed_data.json`), including Lindinium (Base-RS 3400), Quantanium (3170), Savrilium (3200), Bexalite (3600), Salvage Panels (2000), all FPS hand-mining gems (3000 RS) and ROC vehicle gems (4000 RS), complete planetary/belt spawn locations, and refinery bonuses across all stations in Stanton, Pyro, and Nyx.
  - OCR digit confusion recovery (8xxx -> 3xxx): fixed recognition of Lindinium (3400 RS) and other 3xxx signatures when Windows OCR confuses the Star Citizen font's rounded '3' with '8' (e.g. 8400 -> 3400), resolving to the exact ore signature.
  - OCR thousands comma recovery (71.200 -> 7.200): resolved an issue where the Star Citizen HUD thousands comma `,` was read by Windows OCR as `1.` (producing e.g. 71.200 instead of 7.200 or 31.400 instead of 3.400), automatically recovering the intended 7.200 RS (2x Bexalite) or 3.400 RS (Lindinium) reading.
  - Fixed overlay window pointer drag interception: removed `BeginMoveDrag` from the root container in `RsScanOverlayWindow.axaml` which previously swallowed mouse clicks on overlay buttons (`[⚡ Auto-Scan]`, `[👁 Box]`, `[🎯 Bereich]`, `[⚡ Scan]`) and prevented Auto-Scan from being toggled. Window dragging is now isolated to the dedicated title bar handle (`⋮⋮ 🛰 RS DECODER`).
  - Auto-Scan auto-activation: opening the RS Scan Overlay window or completing an on-screen region calibration now automatically starts Auto-Scan. Auto-Scan is also enabled by default in application settings.
  - Interactive Region & Box Controls: Added dedicated `[🎯 Bereich]` (interactive mouse drag-to-select region), `[👁 Box]` (manual toggle for on-screen indicator frame), `[↺]` (reset to default), and clickable Auto-Scan button directly in the `RsScanOverlayWindow` header.
  - Added `[👁 Box]` toggle button to the main window RS Scanner card alongside the calibration button.
  - Removed on-screen indicator frame popup on scan: the pink scan indicator box is no longer displayed or flashed on screen during background or manual scanning, leaving the in-game HUD completely unobstructed. The indicator box is now only visible when explicitly turned on via the `[👁 Box]` toggle button.
  - ScanIndicatorWindow color restoration: fixed an issue where `FlashGreen()` could inadvertently reveal a hidden indicator frame or overwrite custom accent colors and labels with hardcoded magenta.
  - Fixed Windows OCR dimension limit (2600px): `OcrEngineService.Preprocess` now automatically caps image dimensions and adapts scaling so large regions and high-resolution monitors never trigger Windows OCR argument or buffer overflow exceptions.
  - Optimized HUD color preprocessing: enhanced `Preprocess` with maximum color channel intensity detection and contrast stretching, converting amber/orange, cyan, green, and white HUD text into crisp, high-contrast black-on-white characters for maximal OCR accuracy.
  - Instant radar ping detection: refined `ConfirmDebounce` to trigger immediately on the first valid RS detection without discarding brief 1-tick radar pings, backed by a 2-second cooldown against duplicate spam.
  - Fixed Auto-Scan ToggleButton event race in `MainWindow.axaml` and `MainViewModel.cs` where two-way binding combined with Command double-toggled and immediately stopped Auto-Scan.
  - Upgraded OCR multi-scale scaling: added Scale 6 (24px padding) as primary pass for tiny Star Citizen 10-14px HUD fonts, with Scale 4 and Scale 2 fallbacks.
  - Reduced scan timer interval to 150ms.
  - Added missing ore `Savrillium` (BaseRs 3200) to `RsDecoderCatalog.cs`.
  - Added comprehensive diagnostics logging (`[RsOcr]`) to `SCLogMate.debug.log` recording capture dimensions, raw OCR strings, and parsed RS values.
  - Implemented capture safety margins (+30px horizontal, +15px vertical padding) around calibrated scan regions in `RsOcrScanner.ScanOnceAsync` to prevent ship/ping drift from truncating digits at the boundary.
  - Fixed digit parsing and thousands normalization in `ExtractRsValue` to reliably support spaced thousands (`7, 200`, `7. 200`, `14 400`, `14, 400`, `108 000`), RS prefix (`RS: 7,200`) and suffix (`7,200 RS`), and OCR character confusion (`O`/`o` -> `0`, `l`/`I` -> `1`).
  - Adjusted default uncalibrated RS scan region in `ScreenCapture.GetDefaultRsRegion` to vertically span 25% to 70% of screen height directly centered over the Star Citizen crosshair.
- **Exclusion of Logins / Spawns from Places & Locations (`📍 Orte`)**:
  - Filtered out `Im Spiel gespawnt (Station / Hangar)` and client spawn/login events from appearing in the Orte (Places) list and Starmap locations.
  - Reclassified player spawn events in `LogParser.cs` from `EventKind.Location` to `EventKind.SessionChange`.
  - Added database schema migration v10 (`PRAGMA user_version = 10`) in `Core/Database.cs` to reclassify all historical spawn events in SQLite from `Location` to `SessionChange`.
  - Added guards in `Locations.IsAmbiguous` and `MainViewModel.QuantumViews.cs` (`RebuildPlacesFromDatabase`) to prevent non-geographic spawn entries from counting as location visits.
- **Duplicate & Triplicate Event Ingestion and Display**:
  - Fixed an issue where financial transactions, cargo trades, contracts, and session events appeared duplicated or triplicated in "Größte Einzel-Posten", "Letzte Geld-Bewegungen", and financial calculations.
  - Resolved root cause where `RescanDatabaseCommand` and `LogArchive.Sync` collected redundant log paths across `%APPDATA%\SCLogMate\archive` and `logbackups` into database re-scans.
  - Eliminated rogue SHA-suffixed archive duplicates in `LogArchive.Sync` by overwriting existing archive files when newer/larger rather than creating divergent file copies.
  - Fixed SQLite multi-statement syntax error in `Database.IndexNew` that previously prevented cleaning old session events prior to re-indexing.
  - Added database schema migration v9 (`PRAGMA user_version = 9`) to automatically purge all duplicate events and duplicate session rows from existing SQLite databases upon launch.
  - Added `DISTINCT` queries in `Database.TopMoney`, `Database.RecentMoneyEvents`, and LINQ `DistinctBy` guards in `RebuildIndependentFinances` to ensure completely deduplicated presentations.
- **Places / Orte Re-Scan and Historical Sync**:
  - Fixed an issue where the "Orte" (Places & Quantum Destinations) tab was not updated upon re-scanning the database because location visits and quantum travel events were previously only populated from the single active log session.
  - Implemented `RebuildPlacesFromDatabase` to aggregate all historical `Location` and `Quantum` events across all database sessions and merge them with live events.
  - Added support for filtering by current session in `FilterPlaces` when "Session" is selected.
- **Database Re-Scan Progress Display**:
  - Removed duplicate inline progress bar and green status box from the Database settings tab that previously showed concurrently in the background behind the modal indexer overlay.
  - Completion status message now only appears when the database operation is finished (`ShowDatabaseCompletionStatus`).
- **Database Indexing Performance & Live Progress Banner (`Core/Database.cs`, `Views/MainWindow.axaml`)**:
  - Added a dedicated Sci-Fi live progress banner directly below the header bar in `MainWindow.axaml`, displaying the currently parsed log file name, live progress bar, and percentage whenever background indexing is active (`IsDatabaseBusy`).
  - Greatly improved database indexing and rescan performance by introducing in-memory batch caching (`PRAGMA cache_size = -64000`, `PRAGMA synchronous = OFF`), deferred index creation during bulk imports, and pre-filtering to only process genuinely unindexed files in `IndexNew`.
  - Progress percentages and file counts now report reliably without freezing or skipping.
- **Dashboard Account Card Session Metrics Display**:
  - Fixed an issue where literal `{0}` format string placeholders were visible in the dashboard Kontostand card for Einnahmen, Ausgaben, and Saldo.
  - Redesigned the session metrics row into sleek, color-coded cockpit HUD pills (emerald for income, crimson for expenses, deep navy for net balance) with clean typography and spacing.

### Changed
- **Modernized Fleet Deck Header & Controls (`Views/MainWindow.axaml`)**:
  - Replaced the harsh solid blue buttons with a high-end cockpit MFD segmented view switcher (`🏠 Mein Hangar` vs. `✈ Flug-Historie`) with active glow borders and count badges.
  - Replaced the bulky, inline ComboBox and blue "+ In Hangar" button with a sleek, discrete `[+ Schiff hinzufügen]` action button featuring a dark mobiGlas Sci-Fi popover flyout.
  - Upgraded telemetry KPI metrics into glowing cockpit HUD clusters with distinct color schemes (aUEC Marktwert in Emerald, Pledge in Amber/Gold, Flights in Sky Cyan, Quantum Jumps in Violet) and high-legibility monospaced values.
  - Elevated acquisition and manufacturer filter chips with dynamic border and foreground converters (`FilterActiveConverter`, `FilterActiveBorderConverter`, `FilterActiveForegroundConverter`) for refined active/idle states without harsh solid blues.
  - Modernized fleet search bar with integrated `🔎` indicator and `✕` clear action (`ClearFleetSearchCommand`).
- **Cockpit-Themed Controls & Button Redesign in Tools & Maintenance Suite**:
  - Replaced jarring solid neon/cyan and saturated blue button blocks across the `🛠 Tools` tab and `user.cfg` Tuning Studio popout with sleek, dark sci-fi cockpit controls (`Classes="ghost"`).
  - Designed dark navy and slate button backgrounds (`#0C1B2E`, `#081422`, `#141026`) with crisp, subtle borders (`#1C4B78`, `#162E48`, `#3B256B`) and refined hover transitions.
  - Centered all button contents (`HorizontalContentAlignment="Center"` and `VerticalContentAlignment="Center"`) across all styles and buttons, eliminating left-shifted text and icons.
  - Upgraded Windows Explorer quick-access bar to evenly stretched toolbar buttons (`HorizontalAlignment="Stretch"`) with crisp vector `Path` geometry icons (folders and cloud) in uniform cyan `#38BDF8`, restoring the dedicated `SCLogMate` application directory button.
- **Maintainer & Attribution**:
  - Standardized project maintainer branding and attribution uniformly to `gOOvER`.
- Bumped application version in `SCLogMate.csproj` to `1.0.0-beta5`.

### Removed
- Removed obsolete green informational banner ("Automatische Missions- & Reputations-Erfassung aktiv") from the Aufträge / Katalog sub-tab in `Views/MainWindow.axaml`.

## [1.0.0-beta4] - 2026-09-03
### Added
- Interactive Sci-Fi vector financial timeline chart (`Views/FinanceTimelineChart.cs`) with hardware-accelerated Avalonia DrawingContext rendering, glowing neon dual curves (earned vs. spent with gradient fills), cumulative net profit trendline, and session cashflow histogram.
- Real-time interactive crosshair and floating mobiGlas HUD hover tooltip displaying exact timestamps, event names, transaction amounts, and running balances.
- Comprehensive financial KPI HUD header (Income, Expenses, Net Profit with margin %, live mobiGlas OCR wallet balance, and total traded cargo volume in aUEC & SCU).
- Chart mode switchers (Cumulative In vs. Out, Net Profit Trend, Cashflow Bars) and category filters (All, Cargo Only, Rewards/Transfers Only).
- Dynamic & extensible UI localization service (`Core/I18n.cs`) enabling instant, live switching between German and English across the entire UI without restarting; seamlessly updates all 12 main tabs, 8 settings sub-tabs, dashboard KPI cards, filter chips, location badges, status pills, and system messages.
- Application Language switcher: located globally in the main application Sci-Fi header bar (accessible from any tab) and in general settings; select between Automatic (System), German, and English with instant reactivity and settings persistence; controls language-specific features and conditionally hides German-only companion prompts.
- Aurora VoiceAttack test simulation toggle: test the uninstalled/unpurchased state with a single click in the UI.
- Direct Gumroad store purchase banner for the Aurora Log-Wächter package (`https://3415383443272.gumroad.com/l/yzpmoa`) displayed exclusively on German systems when Aurora is not installed or simulated.
- Native VoiceAttack & Aurora Log-Wächter integration: auto-detects installation in user documents, plays audio alerts (ship greetings for 73 ship classes, armistice/safety zones, restricted areas, monitored space, jurisdictions, quantum arrival, blueprints, player death, and server 30k errors) strictly read-only, and automatically grays out all controls if Aurora is not found.
- Parser diagnostics in `--scan` output for unmatched notifications and transfer headers that expire without an amount.
- Live Starmap location tracking after completed Quantum jumps.
- High-resolution multi-size application icon with Star Citizen holographic radar & quantum compass design embedded in the executable and taskbar.
- Integrated Developer & Debug Mode: configured strictly via `%APPDATA%\SCLogMate\settings.json` (defaults to active in local debug builds and strictly inactive in release/production builds).
- Developer tools sub-tab in settings (`🧪 Entwickler`) providing live Star Citizen log event simulation (Armistice enter/leave, ship boarding greetings, blueprint learned, quantum arrival, 30k error, death), state dumps to debug log, log clearing, and overlay reset.
- Strict security guard locking Aurora VoiceAttack simulation exclusively behind active debug mode (never exposed or active in production).


### Removed
- Removed misplaced finance and combat kill indicators from the Flight Recorder (`Flugschreiber`): purged finances (aUEC delta, trade transactions, purchases) and unlogged combat killboard stats from flight telemetry, keeping finances exclusively in the dedicated 'Finanzen' tab.

### Changed
- SQLite Schema v8 upgrade: added composite multi-column database indexes (`ix_events_session_kind` and `ix_events_kind_time`) for ultra-fast session querying and wipe-filtered event loading across tens of thousands of telemetry records.
- Optimized Flight Recorder session switching in `MainViewModel`: eliminated redundant timeline re-computations when selecting archived flights.
- Refined flight duration heuristic in `FlightRecorderService`: fallback cockpit duration estimation is only applied when actual sorties took place (`SortieCount > 0`), ensuring pure hangar/menu idle sessions report zero flight hours accurately.
- Corrected combat kill parsing in `LogParser.cs`: player victim lines are now accurately classified as `EventKind.Death` rather than generic kill events.
- Refocused Flight Recorder (`Flugschreiber`) strictly on pure aerospace telemetry and navigation: header now displays dedicated flight KPIs (`FLUGDISTANZ (QUANTUM)`, `FLUGZEIT & COCKPIT`, `SCHIFFSEINSÄTZE / SORTIES`, `NAVIGATION & ZONEN`), with streamlined flight event filters (`Alle`, `🌀 Quantum`, `🛸 Schiffe`, `📍 Orte & Landung`, `💥 Vorfälle & Verluste`).
- Comprehensive Flight Recorder rework: introduced a dedicated 'Flug-Explorer' dropdown (`FlightSessions`) allowing pilots to inspect either their full career timeline or specific individual flights with ship, duration, and system data, with direct 1-click waypoint focusing in the full Starmap tab.
- Decoupled Flight Recorder (`Flugschreiber`), Missions, and Finances from the top session dropdown: all analysis and telemetry tabs now display the complete career history across all sessions from the database by default, leaving the top session selector exclusively for filtering the main event log (`📜 Ereignisse`).
- Renamed the 'Erz-Scanner' tab to '⛏ Mining', integrating radar RS scanning, full 4.x resource catalog, and refinery jobs/live timers in one central place.
- Completely redesigned the Finances tab (`Views/MainWindow.axaml`) into a futuristic Sci-Fi financial analytics center featuring a 5-card HUD header, interactive vector timeline chart, dual-column category breakdown with dynamic percentage shares, and commodity trading margin intelligence.
- Join multi-line HUD notifications before parsing in archived sessions and live tailing.
- Index archived sessions by file size and last-write fingerprint so changed backups are parsed again.
- Save overlay positions after movement settles, debounce event searches, and render initial log events in batches.
- Migrate UEX API keys from `settings.json` to protected storage.
- Use stable SHA-256 keys and a 128 MB limit for the wiki image cache.

### Fixed
- Fixed overlapping horizontal scrollbar on Windows that obscured filter buttons in the Flight Recorder toolbar by restructuring the header into clean, non-overflowing rows.
- Restrict ship boarding greetings strictly to the initial boarding in a hangar/pad on a station: completely suppress greetings after ship crashes, collisions, destruction, player death, respawns, insurance claims, or quantum jumps, and only allow one greeting per ship family during a station stay.
- Fix country flags not displaying on Windows by replacing unrendered Unicode regional indicator emojis with dedicated, crisp XAML vector flags (German Black-Red-Gold and British Union Jack) across language pickers and badges.
- Completely eliminate false voice announcements on stations and during login/spawning: initialize IsAtStation from startup location, detect habs/spawns/hospitals, add a 60-second login grace period, and suppress Safety Zone, Monitored Space, Restricted Zone, and Jurisdiction voice alerts whenever the player is on a station or logging in.
- Upgrade Aurora voice playback engine to Windows Media Foundation player with volume control and live debug logging.
- Fix settings window bottom cutoff by compacting VoiceAttack control cards (single-row volume & test-sound, streamlined category tiles), adding a 160px bottom scroll margin, and reducing default window dimensions to 1400x780 (MinHeight 520) for optimal 1080p DPI scaled display support.
- Keep game focus when showing the Mini-HUD or RS overlay.
- Discard unfinished live entries on log rotation instead of carrying them into the next session.
- Stop a toast fade timer immediately when its toast is dismissed.
- Prevent stale tailer lines after a session change, serialize OCR work, and release native bitmap resources deterministically.
- Fall back to standard centered HUD scan region for RS OCR scanner when no custom region is configured.
- Prevent wallet OCR misreads by adding dual-read disparity rejection and requiring cross-grab confirmation (seen 2×).
- Improve OCR thread-safety in ContractScanner, timer disposal guards in RsOcrScanner, and lock synchronization on OcrEngineService disposal.
- Fix multi-size Windows application icon binary encoding to prevent startup bitmap loader errors.
- Allow retrying failed UEX location-data requests.

### Security
- Store UEX API keys with Windows DPAPI and mask the input until explicitly confirmed.
- Verify downloaded updates against the `SHA256SUMS.txt` release asset before replacement.

## [1.0.0-beta3] - 2026-08-30

### Added
- RS signal decoder and in-game scanner overlay for mining and salvage HUD pings.
- Windows OCR scanning with configurable capture regions and image preprocessing.
- Flight recorder timeline, session KPIs, and Markdown flight-report export.
- Pyro, Nyx, and extended Stanton locations in the Starmap.
- Reactive location and jurisdiction indicators.

### Fixed
- Aggregate flights by real sessions and deduplicate live sortie events.

## [1.0.0-beta2] - 2026-08-30

### Added
- Update dialog with automatic GitHub release detection and self-replacement.
- Progress modal for database indexing and re-scans.

### Changed
- Improve fleet sortie counts, contract merging, player detection, and database query batching.
- **RS Signal Scanner Overlay (`RsScanOverlayWindow`)**: Modernized sci-fi HUD design with widened layout (450x192px), vibrant cyan glass border, and dedicated rows for ore titles and rarity subtitles. Eliminates horizontal text clipping/overflow by placing title and subtitle in individual bounded grid rows with reliable character ellipsis, and splits refinery recommendation and aUEC value into distinct high-contrast telemetry pills.

### Fixed
- Prevent inflated flight counters and contract duplicates caused by the fallback contractor.
- **RS Signal Scanner (10.200 RS, 21.350 RS & Multi-Digit Recognition)**: Robust OCR detection for 5-digit signatures (e.g. 10.200 RS for 3x Lindinium, 21.350 RS for 5x Iron). Handles OCR misreads where commas are read as '1' in 6-digit sequences (101.200 -> 10200, 211.350 -> 21350) or '.1' (10.1200 / 21.1350), resolves 3<->8 middle-digit confusions (21.850 -> 21.350), 7<->2 prefix confusions, Z/z<->2 leading-digit confusions (Z1.350 -> 21.350), SO/So<->50 suffix confusions (21.3SO -> 21.350), truncated trailing zeros (21.35 -> 21350), converts letter confusions (IO / lO / |O -> 10), strips leading/trailing HUD bracket noise from 5- and 6-digit values, and adds diagnostic debug logging for unmatched OCR text.

## [1.0.0-beta1] - 2026-08-30

### Added
- Starmap navigation, Quantum route calculation, Lagrange stations, and jump-gate support.
- Fleet and hangar management, ship history, insurance tracking, and flight statistics.
- Faction reputation tracking, freight and ship-elevator parsing, and pilot loadout analysis.
- SQLite session indexing, raw-log archiving, and migration from SCLogMate to SCLogMate.
- Toast notifications, global hotkey support, click-through HUD mode, refinery orders, trade routes, and loot estimates.
- Mission catalog and log matching, wipe-date filtering, custom Starmap points, and UI search and clipboard tools.
- Windows autostart, minimize-to-tray, crash handling, OCR calibration, and bilingual mobiGlas scanning.

### Changed
- Move background synchronization and database rebuilds off the UI thread.

### Fixed
- Correct OCR leading-digit truncation and live balance recomputation.
- Prevent tray lifecycle disposal failures.

## [1.2.0] - 2026-08-29

### Added
- Settings page, crash and fatal-error detection, bilingual mobiGlas scanning, and multi-monitor OCR calibration.

## [1.1.19] - 2026-08-28

### Added
- Mission reputation view grouped by contractor and faction.

### Changed
- Batch database queries.

## [1.1.18] - 2026-08-18

### Changed
- Include cargo-buy requests in balance calculations and expand commodity prices and loot tracking.

## [1.1.17] - 2026-07-19

### Fixed
- Calculate balances relative to the recorded balance timestamp.

## [1.1.14] - 2026-07-14

### Added
- Loot-item tracking and localized `global.ini` name resolution.

## [1.1.0] - 2026-06-28

### Added
- SQLite session index and raw-log archive.

## [1.0.0] - 2026-06-28

### Added
- Initial public release of SCLogReader by miwidot.


