# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **Interactive Ship & Mission Cross-Linking across Live-Log and Finances (`frontend/src/views/EventsView.tsx`, `frontend/src/views/FinancesView.tsx`, `frontend/src/views/FleetView.tsx`, `frontend/src/views/MissionsView.tsx`, `frontend/src/views/WarehouseView.tsx`, `frontend/src/views/PlacesView.tsx`, `frontend/src/App.tsx`)**:
  - **Live-Log & Events Table Links (`EventsView.tsx`)**:
    - Made the **Ship** column clickable with a dedicated rocket icon badge. Clicking any ship (e.g. Drake Corsair, Drake Vulture) instantly navigates to the Fleet tab (`fleet`) and sets the search filter to that ship.
    - Added interactive category badges and hover quick-links (`Auftrag ↗`, `Schiff ↗`) in the Detail column to open associated missions or ships directly.
    - Added a dedicated "Direkt-Verknüpfungen" quick-action card in the side-drawer for selected events, allowing instant navigation to Fleet, Wiki dossier, Mission Manager, Finances ledger, or Warehouse.
    - Added contextual navigation options (`Schiff in Flotte anzeigen`, `Auftrag im Manager öffnen`, `In Buchhaltung anzeigen`, `Im Warenlager anzeigen`) to the right-click context menu.
  - **Finances & Cargo Ledger Links (`FinancesView.tsx`)**:
    - Made the **Ship** column in both the Buchhaltung (Ledger) and Fracht & Handel (Cargo) tables clickable, jumping directly to the ship in Fleet view.
  - **Deep-Link Navigation & Search Synchronization (`App.tsx`, `FleetView.tsx`, `MissionsView.tsx`, `WarehouseView.tsx`, `PlacesView.tsx`)**:
    - Added `NavTargetContext` in `App.tsx` allowing cross-tab navigation with pre-filtered search queries, locations, and subtabs.
    - Enhanced `FleetView` to automatically switch between Hangar and History tabs if a navigated ship was flown historically but is not currently in the player's active hangar.
    - Enhanced `MissionsView` to automatically switch to the History tab if a navigated contract was completed or logged in historical records.

### Fixed
- **Decoupled mobiGlas Wallet Balance Discrepancies from Mission Rewards (`Core/Photino/PhotinoBridge.cs`, `Core/Database.cs`)**:
  - **Eliminated Erroneous Mission Reward Inflation**: Removed the legacy reconciliation logic in `OnBalanceCaptured` that automatically tacked positive balance discrepancies onto recently completed missions. Missions maintain their authentic contract rewards (e.g. 200,000 aUEC), and unlogged balance gains (such as player-to-player transfers, trade, or unlogged sales) are now strictly recorded as independent `TransferIn` events (`Einnahme (mobiGlas)` / `Saldo-Abgleich`).
  - **Database Migration v32 & Reward Rectification**: Added SQLite schema migration `v32` to restore any artificially inflated mission rewards back to their authentic contract amounts, ensuring historical quest records and ledger statistics remain accurate.
- **mobiGlas Wallet Multi-Million Balance OCR Truncation & Spurious Maintenance Events Fix (`Core/Ocr/WalletOcrTrigger.cs`, `Core/Ocr/WalletCapture.cs`, `Core/Database.cs`)**:
  - **Fixed Truncated Balances on Multi-Million Values (`WalletOcrTrigger.cs`)**: Fixed an issue where Star Citizen mobiGlas wallet values with 7 or more digits (e.g. `¤ 4,038,230`) were rejected because OCR frequently missed or merged the first comma (`4038,230`). The group validator previously strictly required the leading group `parts[0]` to have between 1 and 3 digits, erroneously discarding valid numbers with fused thousands groups (e.g. `4038` having 4 digits) and leaving only transient 3-digit tail readings (`230`) to be accepted.
  - **Support Fused Thousands Groups up to 9 Digits**: Allowed `parts[0].Length` to range from 1 to 9 digits in `ExtractBalance`, and expanded `ThousandsSeparatorRegex` and `SpaceThousandsRegex` lookbehinds from `\d{1,3}` to `\d{1,9}` to accurately recognize and parse formatted, partially formatted, and unformatted balances up to 11 digits (e.g. `4038,230`, `12038,230`).
  - **Increased Fade-In Settle Delay (`WalletCapture.cs`)**: Increased `SettleDelay` from 300ms to 500ms to allow mobiGlas holographic unfold and HUD fade-in animations to fully stabilize before starting OCR captures, avoiding partial/garbled initial frames.
  - **Partial-Read Guard in Cross-Grab Confirmation (`WalletCapture.cs`)**: Added digit-length validation preventing candidate readings with fewer digits than the maximum digit length observed in the current burst from being confirmed (e.g. preventing a transient 3-digit tail `230` from being confirmed when `4,038,230` was already detected).
  - **Database Migration v31 & Data Cleanup (`Core/Database.cs`)**: Added SQLite schema migration `v31` to automatically purge erroneous `Maintenance` events created when OCR mistakenly parsed truncated balances (e.g. -3,896,000 aUEC) and rectified associated mission rewards.
- **Orison Relief Medium Materials Order Reward Correction (`Core/MissionCatalog.cs`, `Core/Database.cs`)**:
  - Corrected `BaseReward` for `Orison Relief: Medium Materials Order` (`orison_relief_med_order`) from outdated `58.000 aUEC` to the authentic Star Citizen 4.10 payout of `200.000 aUEC`.
  - Added SQLite schema migration `v30` in `Core/Database.cs` to update all existing historical and active `MissionTaken` and `MissionReward` log events from `58.000` to `200.000 aUEC`, and rectified OCR reconciliation deltas that had accumulated the difference.
- **Aurora ATC Hangar Clearance Infinite Voice Repetition Fix (`Core/AuroraVoiceService.cs`)**:
  - **Excluded Freight & Ship Elevators from Voice Triggers**: Fixed an issue where freight and ship elevator operations (`LoadingPlatformManager`, `Frachtaufzug bereit`, `Schiffsaufzug bereit`) continuously triggered `OnAtcLanding` voice announcements ("Landefreigabe erteilt", "Startfreigabe erteilt") whenever any elevator reached `OpenIdle` in spaceports (e.g. Orison).
  - **Granular Event Filtering in `ProcessLiveEvent`**: Verified that `EventKind.Hangar` events only trigger voice playback when `e.Detail` represents an actual ATC landing/takeoff clearance or hangar assignment, explicitly excluding cargo/ship elevators and ship retrieval spawns.
  - **Hardened Regex & Matchers in `ProcessLiveLine`**: Excluded background ATC comms bubbles (`AImodule_ATC`, `CSCCommsComponent`), hangar queue polling events (`Hangar Queue`, `Joined hangar queue`), and notification fading/removal updates (`UpdateNotificationItem`). Only actual HUD notifications (`Added notification ... Hangar Request Completed / Landefreigabe / Startfreigabe`) or granted ATC responses now trigger voice lines.
  - **Increased Cooldown**: Raised `atc_landing` playback cooldown from 15s to 45s to avoid duplicate voice lines when multiple log notifications fire for the same clearance.
- **Missions Navigation & Empty Filter State Fix (`frontend/src/views/EventsView.tsx`, `frontend/src/views/MissionsView.tsx`)**:
  - **Stripped Generic Query on Mission Navigation (`EventsView.tsx`)**: Fixed an issue where clicking the category badge "Auftrag" or mission detail links passed generic strings like `"Mission"` as search queries, filtering out valid active missions whose titles/contractors didn't match the word `"Mission"`. Added `cleanMissionSearch` to extract real contractor/mission names while stripping log prefixes, currency, and generic labels.
  - **Category Badge Navigation**: Made the "Auftrag" category badge navigate to `missions` directly without any filter query.
  - **Search Input Clear Button & State Reset (`MissionsView.tsx`)**: Added a 1-click clear button (`✕`) inside the search input.
  - **Empty-State Filter Reset**: Added an inline "Filter zurücksetzen" button when search or type filters result in zero matches, allowing users to instantly restore the full mission list.
  - **Expanded Filter Matching**: Broadened `getFilteredList` to search across mission type, description, and star systems in addition to title, contractor, and faction.
  - **Decoupled Search State**: Removed `data` from `useEffect` dependencies so search inputs are not continuously overwritten when telemetry updates arrive.
- **Warehouse & Places Location Deduplication and Canonicalization (`Core/Locations.cs`, `Core/LogParser.cs`, `Core/Database.cs`, `ViewModels/MainViewModel.QuantumViews.cs`)**:
  - **Fixed Duplicate Location Names (`ExtractLocationFromShop`)**: Fixed an issue where purchasing or selling items at shops attached redundant parent body suffixes (e.g. `Levski · Delamar`, `Area 18 · ArcCorp`, `New Babbage · microTech`), resulting in duplicate entries and split inventory item totals between shop purchases and freight elevator movements.
  - **Location Name Normalization (`Locations.NormalizeLocationName`)**: Added automatic normalization for formatted location strings (stripping compound celestial body separators such as ` · ` and ` (...)`) in `Locations.ResolveLocation` and database warehouse movement persistence.
  - **Defensive Summary Merging (`Database.GetWarehouseLocationsSummary`, `Database.GetWarehouseItems`)**: Grouped and merged warehouse locations by canonical name and aggregated total item quantities and distinct types across both C# backend and SQL queries.
  - **Database Migration v29 & Parser Version 39**: Implemented SQLite schema migration `v29` to atomically deduplicate and merge existing redundant location rows in `warehouse_items` into their canonical locations. Incremented `CurrentParserVersion` to 39 in `Core/Database.cs`.
  - **Top Locations Merging (`ViewModels/MainViewModel.QuantumViews.cs`)**: Grouped `TopLocations` in the Places overview by canonical location name so visit counts from differently formatted log entries are unified.
- **Windows Taskbar Application Icon Display (`Program.cs`)**:
  - Removed unmapped `SetCurrentProcessExplicitAppUserModelID("SCVerse.SCLogMate")` call which caused Windows Shell to decouple the process from `SCLogMate.exe`'s embedded icon resource and fall back to the default blank/generic application icon on the taskbar.
  - Implemented `SetClassLongPtr` (`GCLP_HICON` and `GCLP_HICONSM`) alongside `WM_SETICON` (`ICON_BIG` and `ICON_SMALL`) using exact system metrics (`SM_CXICON`, `SM_CXSMICON`) to ensure the window class and native handle retain the custom icon across minimizations and window switches.
  - Added `SetNotificationRegistrationId(Guid.NewGuid().ToString())` to prevent Photino/Windows from caching a missing or default icon for the webview process.
  - Automatically register a Start Menu shortcut (`SCLogMate.lnk`) in `%APPDATA%\Microsoft\Windows\Start Menu\Programs` pointing to `SCLogMate.exe` and `SCLogMate.ico` to ensure system-wide Windows Shell icon indexing.
  - Ensured `SCLogMate.ico` is extracted from embedded assembly resources into `%APPDATA%\SCLogMate\` on initial launch.
- **Salvage Claim & Mission Title Ship Collision Protection (`Core/MissionCatalog.cs`, `Core/LogParser.cs`, `ViewModels/MainViewModel.cs`, `Core/Ocr/ContractParser.cs`, `Core/Database.cs`)**:
  - **Ship Model Conflict Prevention in `MissionCatalog.FuzzyLookup`**: Added known ship model detection (`corsair`, `cutlass`, `vulture`, `caterpillar`, etc.) and manufacturer stop-words (`drake`, `aegis`, `anvil`, `rsi`, etc.) to prevent fuzzy matching across conflicting ship models. Fixed an issue where accepted Drake Corsair missions (e.g. `Claim #92872: Drake Corsair Salvage Rights`) were erroneously matched with `Legal Salvage Claim: Drake Cutlass`.
  - **In-Game Mission Title Preservation (`MissionCatalog.ResolveTitle`)**: Refactored `LogParser.cs` and `MainViewModel.cs` to preserve specific in-game mission titles from HUD notifications rather than overwriting them with generic or conflicting catalog titles.
  - **Added Drake Corsair & Modern Salvage Claims**: Added missing catalog entries for Drake Corsair, Drake Vulture, Drake Caterpillar, and Aegis Reclaimer salvage claims to `Core/MissionCatalog.cs`.
  - **Database Migration v28 & Parser Version 38**: Incremented `CurrentSchemaVersion` to 28 (purging active contract rows corrupted with `Drake Cutlass`) and `CurrentParserVersion` to 38 in `Core/Database.cs`.
- **Aurora Voice ATC & Hangar Assignment Audio Playback (`Core/Photino/PhotinoBridge.cs`, `Core/AuroraVoiceService.cs`, `Core/LogParser.cs`)**:
  - Connected `_auroraService.ProcessLiveEvent(entry)` inside `PhotinoBridge.OnLogLineReceived`, ensuring parsed live events (`EventKind.Hangar`, `EventKind.Maintenance`, `EventKind.Blueprint`, etc.) are delivered to the voice engine.
  - Expanded ATC and Hangar assignment matchers in `AuroraVoiceService.ProcessLiveLine` to support Star Citizen 4.10+ PU log formats (`Hangar Request Completed`, `Joined hangar queue`, `Hangar Queue`, `Hangaranforderung`, `Hangar-Zuweisung`, `AImodule_ATC`).
  - Added queue notifications (`Joined hangar queue`, `In Hangar-Warteschlange eingereiht`) to `LogParser`'s `EventKind.Hangar` detection.
  - Added diagnostic logging in `AuroraVoiceService.OnAtcLanding` showing available audio files and playback triggers.

## [1.0.0-rc3] - 2026-09-19
### Fixed
- **Inventory Item Class Resolution & Noise Filtering (`Core/WikiApiClient.cs`, `Core/WarehouseCatalog.cs`, `release.ps1`)**:
  - Automatically strip numeric entity instance IDs (e.g. `_776193770765`) from internal CIG item class identifiers before performing Star Citizen Wiki API lookups and caching.
  - Suppress character customization loadout noise (`necksock`, `fp_visor`, `hair_`, `brows_`, `eyedetail`, `eyelashes`, etc.) from being logged as unknown item classes in `SCLogMate.unknown.log`.
  - Normalized class name lookups in `WarehouseCatalog` to ensure clean item names and categories for inventory and loot items.
  - Hardened `release.ps1` git push target to `refs/heads/main` preventing ambiguous refspec conflicts.
- **Mission Detection, Contractor Resolution & Management Accuracy (`Core/MissionCatalog.cs`, `Core/Missions.cs`, `Core/LogParser.cs`, `ViewModels/MainViewModel.cs`, `ViewModels/MainViewModel.QuantumViews.cs`, `Core/Photino/PhotinoBridge.cs`, `Core/Database.cs`)**:
  - **FuzzyLookup Over-Matching Protection**: Enhanced `MissionCatalog.FuzzyLookup` with stop-word filtering (`claim`, `rights`, `small`, `medium`, `large`, `haul`, `salvage`, etc.) and strict length-ratio checks, preventing false-positive matches (e.g. salvage rights claims matching `Salvage Claim: Small` and corrupting contractor and faction data).
  - **Dynamic Engine Generator Resolution**: Upgraded `Missions.cs` to resolve Star Citizen 4.10+ dynamic generator identifiers (`TheBackpocket`, `CleanAir`) to real in-lore factions and contractors (`Orison Relief Services`, `People's Alliance`, `Red Wind Line`, `Covalex Shipping`, `Ling Family`, `Civilian Defense`).
  - **Marker vs. HUD Notification Synchronization**: Resolved race condition where `<CLocalMissionPhaseMarker::CreateMarker>` preceded HUD notifications (`Contract Accepted`), preventing real mission titles from being dropped or replaced by placeholder strings.
  - **Colon-Separated Title Parsing**: Refined contractor extraction from mission notifications, preventing claim reference numbers (e.g. `Claim #79277`) from being misinterpreted as contractor names.
  - **Active Contract Completion Fallback Safeguards**: Removed blind single-contract fallback in `HandleMissionCompleted` and abandoned contract handling when completion notifications have conflicting specific titles.
  - **QuantumViews Contract Deduplication**: Prevented merging unrelated concurrent contracts from the same contractor in the contracts overview.
  - **Missions History & Completion State**: Updated `PhotinoBridge.GetMissionsData()` to include `EventKind.MissionReward` in the mission history and correctly determine completion states for both rewarded contracts and fee-based salvage claims.
  - **Added Modern 4.10+ Contracts**: Added Orison Relief cargo hauling and medical supply contract definitions to `MissionCatalog.cs`.
  - **Parser Version Bump**: Incremented `CurrentParserVersion` to 37 in `Database.cs` to automatically re-index historical sessions with sanitized contractor and mission records.
- **Salvage Claim Contract Reward Fix & Cleanup (`Core/LogParser.cs`, `Core/Database.cs`, `Core/MissionCatalog.cs`)**:
  - Fixed an issue where completing Salvage Rights / Salvage Claim contracts incorrectly awarded an artificial aUEC mission completion reward. In Star Citizen, salvage claims require paying an upfront fee and do not award completion payouts (revenue is generated solely through scraping/selling RMC and Construction Materials).
  - Updated `LogParser` to identify salvage claims and suppress completion reward amounts (`reward = 0`, logged as `EventKind.Mission` instead of `EventKind.MissionReward`).
  - Added `ContractFee` property to `MissionCatalog` and set `BaseReward = 0` across all legal and unverified salvage claim definitions.
  - Implemented database schema migration v27 (`CurrentSchemaVersion = 27`) to clean up legacy false-positive `MissionReward` events from past salvage claim completions.
- **Aurora Voice False-Positive Docking Output (`Core/AuroraVoiceService.cs`)**:
  - Resolved an issue where approaching any station, rest stop, or armistice zone triggered the "Andocken" (Docking) voice output.
  - Removed overly broad substring matchers (`DockingTube`, `Docking collar`) that were false-positively triggered by Star Citizen's background entity streaming (such as `Station_DockingTube_Reststop-arm` and fuel port attachment components).
  - Restricted docking voice alerts to explicit player docking actions and ATC assignments (`RequestDocking`, `Docking Request`, `Assigned to Docking`, `Docking complete`, `Docking granted`, `Andockfreigabe`).

### Added
- **Extended Aurora Voice Category Controls (`Core/AuroraVoiceService.cs`, `Core/Settings.cs`, `PhotinoBridge.cs`, `SettingsView.tsx`)**:
  - Added granular category toggles in the Settings view and backend configuration for ATC & Landing (`auroraAtcAndLanding`), Maintenance & Refuel (`auroraMaintenance`), Stations & Moons (`auroraDestinations`), and Onboard Ship Systems (`auroraShipSystems`).
  - Integrated active category filtering into `AuroraVoiceService` event playback, allowing users to customize voice notifications per gameplay subsystem.


### Added
- **Rock-Cracking Mining Calculator ("Can I Crack It?") (`Core/RockCracking.cs`, `OreScannerView.tsx`, `PhotinoBridge.cs`)**:
  - Implemented Star Citizen 4.x fracture power physics solver ($P_{\text{req}} = 0.36 \times \text{Mass} \times (1 + \text{Instability} \times 0.20)$ vs. delivered laser power $P_{\text{del}} = P_{\text{laser}} \times (1 - \text{Resistance}_{\text{eff}})$).
  - Comprehensive laser & outfitting catalog: Helix, Arbor, Klein, Lancet, Hofstede, Impact, modules (Focus III, Surge, Brand, Torpid, Vaux, Stampede), and gadgets (BoreMax, OptiMax, WaveShift, Sabir, Stalwart).
  - Support for Prospector (1 Head) and MOLE (3 Heads) multi-crew configurations with live power gauges, solo/gadget/crew verdict badges, and laser head-to-head comparison matrices.
  - 1-click preset transfer directly from scanned RS radar pings into rock parameters.
- **Physical Cargo-Fit 3D/2D Grid Packing Engine ("Frachtraum-Planer") (`Core/CargoFit.cs`, `CargoFitModal.tsx`, `MarketView.tsx`)**:
  - Dynamic 3D/2D cargo hold packing calculation using standard 1.25m cell coordinates for 1, 2, 4, 8, 16, 24, and 32 SCU containers.
  - Hull-specific MaxBox height restrictions (e.g. Corsair, Cutlass Black, Spirit C1, Freelancer) preventing oversized containers from being planned into restricted bays.
  - Interactive crate stepper, presets (96 SCU bulk, loot containers), visual deck placement manifests, and automated personal hangar fleet compatibility checks.
  - 1-click integration on trade route cards verifying cargo hold fit before purchase.
- **StarCitizenWiki / scunpacked-data Dynamic Community Data Engine (`Core/Community/CommunityData.cs`, `SettingsView.tsx`, `PhotinoBridge.cs`)**:
  - Automated ingestion pipeline importing 11 core data files from [StarCitizenWiki/scunpacked-data](https://github.com/StarCitizenWiki/scunpacked-data) into local binary/JSON digests under `%APPDATA%\SCLogMate\community\`.
  - Enables sub-100ms offline cold startup loading across 750+ commodities, 180+ ships, 5,000+ weapons/armor items, and blueprints.
  - Real-time game build and patch version tracking against active `Game.log` with 1-click community dataset synchronization.
- **Interactive Ship Loadout & Lead-Pips Ballistics Analyzer (`Core/PipsAnalyzer.cs`, `ShipLoadoutModal.tsx`, `FleetView.tsx`)**:
  - Detailed component outfitting inspector for hardpoints, shields, quantum drives, power plants, coolers, and active paints.
  - Real-time weapon muzzle velocity analyzer computing HUD targeting lead pips: distinguishes synchronized fire (`1 Pip`) from divergent trajectories (`2+ Split Pips`) with tactical recommendations.
  - Background screenshot OCR watcher automatically scanning mobiGlas VLM and ASOP terminal captures to sync fleet loadouts.
- **Side-by-Side Ship Comparison Studio (`ShipCompareModal.tsx`, `FleetView.tsx`)**:
  - Interactive side-by-side benchmark studio comparing any two ships from hangar or universe catalog across cargo volume, container box clearance, dimensions, mass, quantum fuel, crew, and pad sizes.
  - Integrated in-game dealership pricing, rental rates (New Deal, Astro Armada), and flight telemetry history.
- **SC Trade Tools (SCT) Dual-Source Market Integration (`Core/SctMarketService.cs`, `MarketView.tsx`)**:
  - Crowdsourced commodity price integration reconciled against UEXcorp with statistical median outlier filtering (MinSamples=4, 4.0× median cutoff).
  - Dual-source verification badges (`✓ Corroborated`, `± Disagreement`) on trade route cards and salvage market tables.
- **Multi-Variable Route Optimizer & Cargo Ship Constraints (`Core/TradeRouteOptimizer.cs`, `Core/CargoConstraints.cs`, `MarketView.tsx`)**:
  - Route ranking across 4 modes: Max Net Profit, Margin/SCU, Profit per Gigameter travel distance (`ProfitPerGm`), and ROI %.
  - Complete directory of 28 Star Citizen Loading Dock amenity sites with terminal elevator and docking collar validation.
  - Container Planner with bounded Dynamic Programming solving optimal kiosk crate assortments (minimizing fees and loading times).
  - Live Auto-Load dispatch estimator and freight elevator countdown banners matching empirical CIG mechanics.
- **Executive Hangar Contested Zone Tracker (`Core/ExecHangarCycle.cs`, `PlacesView.tsx`)**:
  - Deterministic offline cycle tracker for Pyro's Executive Hangar (`PYAM-EXHANG-0-1`) simulating the 5-light door sequence with next-openings schedule and tactical guide.
- **Live Starmap Vector Tracking & "YOU ARE HERE" Beacon (`StarmapView.tsx`)**:
  - Real-time pulsing player beacon, animated quantum travel vectors with moving dash trajectories, Follow-Me canvas tracking, and seamless system switching (Stanton / Pyro / Nyx).
- **Industrial Refinery & Processing Suite with OCR Kiosk Scanning (`RefineryView.tsx`, `Core/Ocr/RefineryParser.cs`)**:
  - 1-click & timed OCR scanning for refinery kiosks extracting materials, batch quantities, yields, costs, and remaining durations.
  - Method & Yield Simulator comparing all refining processes across Stanton, Pyro, and Nyx station bonuses.
- **Star Citizen `user.cfg` Tuning Studio & Cloud Backup Vault (`ToolsView.tsx`, `Core/MaintenanceService.cs`)**:
  - Live code editor with comment preservation (`--`, `;`, `#`), hardware-tailored presets (CPU/GPU detection), visual cvar sliders, and 1-click rollbacks.
  - Automated pre-save backups to local storage, `.bak` Live fallback, and cloud replication (OneDrive, Google Drive, Dropbox).
- **SCMDB Blueprint Synchronization & Org Coverage Analysis (`Core/ScmdbService.cs`, `BlueprintsView.tsx`)**:
  - Headless SCMDB v3 import/export parser, persistent `learned_blueprints` SQLite storage (migration v25), and interactive org-wide crafting gap analysis.
- **Multi-Crew & Ship Channel Tracking (`Core/ShipChannel.cs`, `Core/LogParser.cs`, `BlackboxView.tsx`)**:
  - Extracts pilot boarding, ship ownership, and aboard crew members across sorties (`ChannelMoment.YouBoarded`, `TheyBoarded`, `YouLeft`).
- **Privacy Masking & Sanitized Diagnostics Export (`Core/DiagnosticsRedactor.cs`, `SettingsView.tsx`)**:
  - Automatic scrubbing of usernames, file paths, webhooks, auth tokens, and public IPs for safe Discord and GitHub support reporting.

### Changed
- **License Migration to GNU Affero General Public License v3.0 (AGPL-3.0-or-later) (`LICENSE`, `README.md`, `SCLogMate.csproj`)**:
  - Re-licensed SCLogMate under the AGPLv3 for v1.0.0-rc3 and future releases, preserving MIT attribution for historical versions up to v1.0.0-rc2.
- **Native .NET 10 & Photino Desktop Migration**:
  - Re-architected desktop engine to native .NET 10 and Photino.NET paired with React 19, TypeScript, and Vite, delivering faster startup, lower memory consumption, and native windowing.
- **100% Self-Hosted, GDPR/DSGVO-Compliant Typography (`@fontsource`)**:
  - Bundled local offline font assets (Inter, Orbitron, Rajdhani, Roboto, JetBrains Mono), eliminating external CDN dependencies.
- **True In-Game vs. Menu/Queue Playtime Separation (`Core/LogParser.cs`, `Core/Database.cs`)**:
  - Separates `SC_Default` from `SC_Frontend` to calculate genuine cockpit flight hours and true net profit-per-hour metrics.

### Fixed
- **Star Citizen 4.10 PU Log Compatibility & Ship Channel Parser (`Core/ShipChannel.cs`, `Core/LogParser.cs`, `Core/Database.cs`)**:
  - Resolved prefix matching on raw log lines to capture all ship channel boarding and exit events (`ChannelMoment.YouLeft`).
  - Added support for 4.10 refinery order completion logs, planetary jurisdictions, and Comm-Array status transitions.
- **Hangar & Fleet False Ship Entries (`Core/FleetCatalog.cs`, `Core/Database.cs`)**:
  - Prevented inventory movements, refinery stations, and raw parameter tokens (e.g. `entitlementURN`) from appearing as vehicles. Cleaned historical records via SQLite schema migration v24.
- **Dynamic Wallet Balance Synchronization & Delta Tracking (`Core/Photino/PhotinoBridge.cs`, `Core/Ocr/WalletOcrTrigger.cs`)**:
  - Dynamic balance updates across financial events and robust mobiGlas aUEC OCR parsing with thousands-separator normalization (`ThousandsSeparatorRegex`).
- **High-DPI Multi-Monitor OCR Accuracy (`Core/Ocr/NativeRegionSelector.cs`, `Core/Ocr/OcrEngineService.cs`)**:
  - Added per-monitor DPI awareness and multi-monitor virtual desktop spanning for snipping tools and OCR scanners.
- **Contract Completion Reward Accuracy (`Core/MissionCatalog.cs`, `Core/LogParser.cs`)**:
  - Added tier-specific salvage/clean-up mission payouts and intelligent OCR wallet delta reconciliation for completed contracts.

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



