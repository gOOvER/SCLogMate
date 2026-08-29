# Changelog — SCLogMate

All notable changes to this project are documented in this file. Format based on
[Keep a Changelog](https://keepachangelog.com/en/1.0.0/), versions adhere to
[SemVer](https://semver.org/).

## [Unreleased]

## [1.0.0] - 2026-08-29 — *SCLogMate Initial Release*

### Project Rebranding & Architecture
- **Project Rebranded to SCLogMate**: Full rebranding from *SCLogReader* to **SCLogMate** (Executable: `SCLogMate.exe`).
- **Seamless Data Migration (%APPDATA%\SCLogMate)**: Automatic, non-destructive import of all previous settings, SQLite databases, and OCR calibrations.
- **.NET 10 & High-Performance Core**: Zero-allocation compiled Source Generator regular expressions (`[GeneratedRegex]`), SQLite WAL mode, and optimized DataGrid render pipelines.

### Gaming & HUD Overlays
- **WoW-Style In-Game Achievement & Reward Toast Overlay**:
  - Gaming-style achievement banners for new blueprints (`⬡ BLUEPRINT LEARNED`) and mission completions (`★ MISSION COMPLETE: +aUEC`).
  - Stacked list support: Multiple simultaneous or sequential rewards queue neatly underneath rather than overwriting each other.
  - Operates completely autonomously and independently from the Mini-HUD, smoothly fades out after ~5.5s, never steals game focus (`WS_EX_NOACTIVATE`), and can be freely repositioned anywhere on screen.
  - 1-click test button in the Settings menu for easy positioning and previewing.
- **In-Game Floating Mini-HUD Overlay**:
  - Freely repositionable, borderless Always-on-Top live overlay displaying real-time aUEC balance, current location, armistice zone state, focused mission, and server ping.

### Mission Tracking & Master Catalog
- **Built-in Master Mission Database (`scunpacked-data`)**:
  - Comprehensive catalog of all Star Citizen 4.x PU missions including contractors (*Recco Battaglia*, *Vaughn*, *Wallace Klim*, *Miles Eckhart*, *Twitch*, etc.), factions, default rewards, reputation XP, star systems, and crafting blueprint drops.
  - Interactive **Mission Browser** tab with instant text search and category filters.
- **Zero-OCR Log-Matching & Auto-Sync**:
  - Automatic extraction of contractor, faction, and reward details directly from `Game.log`.
  - **Comprehensive Status Tracking**: Detects `Accepted`, `Complete`, `Abandoned`, and `Failed` mission states in real-time.
  - **Automatic Mission Sync**: Completed, abandoned, or failed missions are immediately cleared from active contract tracking and the SQLite database.

### Blueprint Database (Crafting Blueprints)
- **New Tab: ⬡ Blueprints**:
  - Complete master database of all SC 4.x crafting blueprints (armor, weapons, multi-tools, ammunition, ship components, medical).
  - Learning progress tracker (`X of Y learned`, percentage display), filter chips, and acquisition timestamp history.

### mobiGlas Screenreader (Windows Native OCR)
- **Automated aUEC Balance Capture**:
  - Reads your genuine live balance whenever opening mobiGlas (`F1`) via native Windows OCR.
  - **Robust Multi-Monitor Synchronization**: Pixel-perfect calibration (`⊕ Area`) and a non-intrusive in-game indicator box (`▣ Scan-Box`) with hardware DPI scaling.

### Locations, Starmap & Economy
- **Starmap & Armistice Resolver**:
  - Location resolution for Stanton, Pyro, and Nyx (including *Keeger Depot*, *Wikelo Emporium*, hangars, caves, and contested zones).
  - Real-time armistice zone detection (🟢 Safe Zone / 🔴 Unprotected).
- **UEX Corp API 2.0 Integration & Star Citizen Wiki**:
  - Personal UEX Bearer Token configuration with 1-click connection testing.
  - In-game wiki modal with HD vehicle artwork, manufacturer specs, and lore.
- **Economy & Cargo Tracking**:
  - Real-time accounting for cargo buy/sell orders, shop purchases, player transfers, refinery jobs, and fines.

---

# Archive: Legacy SCLogReader Changelog (Base Fork by miwidot)

Historical changelog from the original *SCLogReader* foundation by **miwidot**:

## [1.2.0] - 2026-08-29
- Settings Page tab with grouped cards.
- Star Citizen crash & fatal error detection with automated contract reset.
- Bilingual English/German mobiGlas scanning.
- Multi-monitor area calibration.

## [1.1.19] - 2026-08-28
- Mission Reputation Proxy tab by contractor/faction.
- Database query performance optimization via SQL batching.

## [1.1.18] - 2026-08-18
- Cargo buy requests (`SShopCommodityBuyRequest`) included in balance calculations.
- Commodity market prices and extended loot tracking.

## [1.1.17] - 2026-07-19
- Balance calculations fixed relative to timestamp entry.

## [1.1.14] - 2026-07-14
- Loot item tracking and name resolution via localized `global.ini`.

## [1.1.0] - 2026-06-28
- SQLite index for sessions and raw log archiving.

## [1.0.0] - 2026-06-28
- Initial public release of SCLogReader by miwidot.
