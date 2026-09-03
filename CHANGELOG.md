# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0-beta4] - 2026-09-03
### Added
- Interactive QuantumWake-inspired Sci-Fi vector financial timeline chart (`Views/FinanceTimelineChart.cs`) with hardware-accelerated Avalonia DrawingContext rendering, glowing neon dual curves (earned vs. spent with gradient fills), cumulative net profit trendline, and session cashflow histogram.
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

### Fixed
- Prevent inflated flight counters and contract duplicates caused by the fallback contractor.

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
