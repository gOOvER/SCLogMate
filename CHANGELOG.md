# Changelog — SCLogMate

All notable changes to this project are documented in this file. Format based on
[Keep a Changelog](https://keepachangelog.com/en/1.0.0/), versions adhere to
[SemVer](https://semver.org/).

## [1.0.0-beta1] - 2026-08-30 — *SCLogMate Pre-Release Beta 1*

### 🗺 Starmap & Navigation (QuantumWake-Style)
- **Vector Route & Flight Time Calculator**:
  - Live cyan glowing flight vector line connecting player's current location (`YOU ARE HERE`) to any selected waypoint or destination.
  - Accurate distance calculation in Gigameters (GM) and kilometers (km).
  - Estimated quantum travel flight time based on selectable Quantum Drive profiles (**S1**: *Atlas*, *VK-00*, *Beacon*; **S2**: *Crossfield*, *Yeager*, *Bolon*; **S3**: *TS-2*, *Pontes*, *Agni*).
- **Full Lagrange Network (L1 – L5)**:
  - Added all Lagrange stations across Stanton (*HUR-L1 to L5, CRU-L1/L4/L5, ARC-L1 to L4, MIC-L1 to L5*) with station specializations (⛏ *Refinery*, 📦 *Cargo Hub*, 🏥 *Clinic*, 🏪 *Rest Stop*).
- **Inter-System Jump Gate Network**:
  - Full support for jump gates between Stanton, Pyro, and Nyx with 1-click system jumping.
- **Security & Danger Halos & Mining Hotspots**:
  - Glowing red halos for lawless / piracy zones (GrimHEX, Ruin Station, Checkmate) vs UEE armistice zones.
  - Resource tags for planets and moons (*Quantanium, Gold, Beryl, RMC wrecks, Bexalite*).
- **Pop-out Starmap Window & Immersive Layout**:
  - Dedicated multi-monitor Starmap window (`StarmapWindow.axaml`) for second screens and sim-pits.
  - Collapsible dashboard HUD cards toggle in main window for full-height Starmap display.

### 🛸 Flotten- & Hangar-Verzeichnis (Fleet Management)
- **Trennung zwischen `🏠 Mein Hangar` und `✈ Flug-Historie`**:
  - **`🏠 Mein Hangar`**: Zeigt ausschließlich Schiffe im persönlichen Besitz (Pledge Store oder in-game mit aUEC gekauft).
  - **`✈ Flug-Historie`**: Vollständiges Logbuch aller jemals geflogenen Schiffe (inklusive Free Fly Events, Leihschiffe von Freunden oder temporäre Mieten).
  - 1-Klick Stern-Schaltfläche (`★` / `☆`) zum Hinzufügen oder Entfernen von Schiffen aus dem persönlichen Hangar.
- **Erweiterte Erwerbsarten (Acquisition Tracking)**:
  - Jedes Schiff kann durchgeschaltet werden: `💵 Pledge Store ($)`, `🪙 In-Game Kauf (aUEC)`, `🎟 Miete (Rental)` oder `👥 Geliehen / Free Fly`.
  - Berechnet den Echtgeld-Gesamtwert der Flotte (`$ USD`) ausschließlich anhand der echten Pledge-Store Schiffe.
- **Bedingte Versicherungsverwaltung (Insurance Rules)**:
  - Versicherungen (`♾ LTI`, `🛡 10 Jahre (IAE)`, `24M`, `12M`, `6M`) sind nur für **Pledge Store** Schiffe aktivierbar.
  - In-game gekaufte Schiffe werden als `Keine (In-Game)` gekennzeichnet.
- **Schiffserkennung via Funkkanäle & Namens-Harmonisierung**:
  - Unterstützung für `You have joined channel '<Ship> : <Player>'`, `ClearDriver` und `ItemNavigation`-Routen zur lückenlosen Erfassung aller Flüge.
  - Automatische Namens-Normalisierung und Versionierungs-Harmonisierung (z. B. `Aurora Mk2`, `Aurora Mk II` und `Aurora` einheitlich zu **`Aurora Mk II · RSI`**; `Hornet Mk2` zu `Hornet Mk II`).
- **Individuelle Flugzähler & QT-Statistiken je Schiff**:
  - Jedes registrierte Schiff besitzt seinen eigenen Zähler für tatsächliche Flüge (`X× geflogen`), absolvierte QT-Sprünge (`Y QT-Sprünge`) und Verlusthistorie (`Z Verluste`).
- **Hersteller- & Erwerbs-Filter**:
  - Schnelle Filterung nach Typ (*Pledge, In-Game, Miete*) und Hersteller (*Drake, Aegis, Crusader, Anvil, RSI, MISC, Origin, Argo, Mirai*).

### 📜 Log-Parsing & Faction Reputation Tracking
- **Faction Reputation & Rank Progression**:
  - Full catalog of Star Citizen factions with XP progression thresholds and rank titles (Tiers 1 to 6).
  - Automatic XP accumulation on completed missions with SQLite persistence.
- **SC 4.x Freight & Ship Elevators & Vessel Destruction**:
  - Event parsing for cargo elevator operations, ship hangar calls, destruction, and insurance claims (passive elevator idle noise filtered out).

### 🥋 Piloten-Ausrüstung & Loadout-Intelligence
- **Rüstungsklassen & Schadensreduktion**:
  - Automatische Berechnung von Rüstungstypen (*Leicht (20%), Mittel (30%), Schwer (40%), Spezial-/Hazmat-Anzüge*) sowie Berechnung des durchschnittlichen Gesamt-Panzerschutzes aller angelegten Teile.
- **Umgebungsschutz & Temperatur-Resistenzen**:
  - Live-Analyse der minimalen und maximalen Temperaturverträglichkeit (-225°C bis +225°C bei Hazmat, -50°C bis +75°C bei Kampfpanzerungen).
- **Waffen-Aufsätze & Modifikationen**:
  - Erkennung und Anzeige von Visieren (Optiken/Scopes), Schalldämpfern, Kompensatoren, Laser-Modulen und Magazingrößen an ausgerüsteten Waffen.
- **1-Klick Loadout-Export & Discord-Sharing**:
  - Schneller Export der gesamten Ausrüstung in die Zwischenablage für Orgs/Discord sowie Erstellung formatierter Markdown-Reports (`.md`).

### 🎨 UI & Layout Fixes
- **Search Box Alignment**: Fixed shifted watermark placeholder text in search boxes with centered vertical content alignment and adjusted padding.

### 💾 Automatische SQLite-Indexierung & Auto-Sync
- **Geräuschloser Hintergrund-Sync beim Start**:
  - SCLogMate gleicht beim Anwendungsstart automatisch das Backup- und Archivverzeichnis ab und indexiert neue Sessions geräuschlos im Hintergrund.
- **Automatischer Re-Scan bei DB- & Parser-Updates**:
  - Bei Änderungen an der Schema- oder Parser-Version (z. B. neuen Heuristiken oder Event-Typen) wird die SQLite-Datenbank automatisch im Hintergrund neu aufgebaut, ohne dass der Anwender manuell einen Re-Scan anstoßen muss.

### Project Rebranding & Architecture
- **Project Rebranded to SCLogMate**: Full rebranding from *SCLogReader* to **SCLogMate** (Executable: `SCLogMate.exe`).
- **Seamless Data Migration (%APPDATA%\SCLogMate)**: Automatic, non-destructive import of all previous settings, SQLite databases, and OCR calibrations.
- **.NET 10 & High-Performance Core**: Zero-allocation compiled Source Generator regular expressions (`[GeneratedRegex]`), SQLite WAL mode, and optimized DataGrid render pipelines.

### 🗔 In-Game Overlays, Hotkeys & Modulare Toasts
- **Modulare Toast-Kategorien & Soundeffekt**:
  - Konfigurierbare Checkboxen für alle Toast-Benachrichtigungstypen (*Baupläne, Missions-Belohnungen, Fraktions-Beförderungen, Veredelungsabschluss, Frachtaufzüge, Schiffszerstörung/Claims*).
  - Optionaler, subtiler Windows-Soundeffekt bei eingehenden Erfolgsbannern.
- **Globaler System-Hotkey (`Alt + H`)**:
  - Blitzschnelles Ein- und Ausblenden des Mini-HUDs direkt im Spiel per Windows-Tastenkombination `Alt + H`, ohne das Vollbild-Spiel verlassen zu müssen.
- **HUD-Sperre & Click-Through Modus**:
  - Sperrung der Overlay-Position (`🔒 Position sperren`) gegen versehentliches Verschieben sowie `🖱️ Click-Through` (`WS_EX_TRANSPARENT`), um Klicks transparent an das Raumschiff-Cockpit durchzureichen.

### 💰 Handel, Raffinerie & Wirtschaft
- **Raffinerie-Aufträge & Live-Countdown-Timer**:
  - Erfassung und Verwaltung aktiver Veredelungen inkl. Methoden (*Dinyx, Ferron, Gaskin, Cormack*), Restzeit-Countdown und automatischem Benachrichtigungs-Toast bei Abholbereitschaft.
- **Profitable Handelsrouten & Profit-Kalkulator**:
  - Katalog profitabler Fracht- & Handelsrouten für Stanton & Pyro inkl. Echtzeit-Gewinnberechnung je nach Schiffsladekapazität (SCU).
- **Loot- & Beute-Wert-Schätzer**:
  - Automatische Bepreisung erbeuteter FPS-Ausrüstung und Waffen anhand typischer In-Game Shop- und Händlerpreise.

### SC 4.x PU Log Parsing & Refinements
- **SC 4.x Vehicle Retrieval Request Support**: Added support for ship spawn tracking via `Vehicle Retrieval Request` lines (replacing legacy pad delivery logs dropped in SC 4.x build 12519617+).
- **Vehicle Comms Channel & Party Notification Parsing**: Automatically tracks personal ship boarding and crew members via `Vehicle Comms Channel` (`[ <Ship> : <Owner> ]`), along with party member join (`New Member Joined`) and leave (`Member Left`) notifications.
- **cSCU/SCU Normalization**: Robust commodity handling for centi-SCU (`cSCU`) reported in `SShopCommodityBuyRequest` and `SShopCommoditySellRequest`.

### Wipe- & Persistenz-Filter
- **Granular Wipe-Datum Filtering**: Filter historical balances, completed missions, flown fleets, and blueprint unlocks starting from a specified wipe date (e.g. Star Citizen Alpha 4.8 / 15. Mai 2026 or Today).
- **Settings & Presets**: Dedicated Wipe-Filter card in Settings with 1-click presets (*Alpha 4.8 Wipe*, *Heute*, *Aus*) and modular checkboxes for Money, Missions, Fleet, and Blueprints.

### Piloten-Ausrüstung & Starmap Notizen
- **11-Slot Piloten-Ausrüstung (Visual Loadout Tab)**: Live tracking of all 11 pilot slots (Helm, Torso/Core, Arme, Beine, Undersuit, Rucksack, Primärwaffen, Seitenwaffe, Multi-Tool, Med-Kit) updated automatically on gear changes and loot.
- **Persönliche Starmap-Wegpunkte & Notizen (Custom POIs)**: Add custom coordinates and notes (Mining, Salvage, Trade, Bunker, Secret) directly to the 2D Starmap, with persistent SQLite storage (`user_pois`) and amber diamond radar markers on planets, moons, and stations.

### Mission Tracking & Master Catalog
- **Built-in Master Mission Database (`scunpacked-data`)**:
  - Comprehensive catalog of all Star Citizen 4.x PU missions including contractors (*Recco Battaglia*, *Vaughn*, *Wallace Klim*, *Miles Eckhart*, *Twitch*, etc.), factions, default rewards, reputation XP, star systems, and crafting blueprint drops.
  - Interactive **Mission Browser** tab with instant text search and category filters.
- **Zero-OCR Log-Matching & Auto-Sync**:
  - Automatic extraction of contractor, faction, and reward details directly from `Game.log`.
  - **Comprehensive Status Tracking**: Detects `Accepted`, `Complete`, `Abandoned`, and `Failed` mission states in real-time.
  - **Automatic Mission Sync**: Completed, abandoned, or failed missions are immediately cleared from active contract tracking and the SQLite database.

### UI & Usability Innovations
- **Visual Blueprint Progress Bar**:
  - Emerald glowing progress bar in the ⬡ Blueprints tab reflecting crafting progression (`X of Y learned (%)`).
- **Live Event Full-Text Search**:
  - Instant search filter in the Events tab to filter events by ship, item, location, amount, or time, with a 1-click clear button.
- **Glowing Pill Badges in Event Log**:
  - High-contrast categorized pill badges with icons for every event type (Gold for Rewards, Emerald for Blueprints, Cyan for Ships, Crimson for Combat/Kills, Purple for Locations).
- **Clipboard Copy Context Menu**:
  - Right-click any event entry to copy detail text, amount, or the entire tab-separated row to clipboard.
- **Dedicated "ℹ Über" Tab**:
  - Complete brand overview, update checking center, diagnostics, and external API attributions.

### System Integration & Window Management
- **Windows Autostart (Minimized to Tray)**:
  - Option in Settings to launch SCLogMate automatically on Windows login directly into the System Tray.
- **Minimize-to-Tray on Close (X)**:
  - Toggle in Settings to keep background tracking, OCR, and overlays running in the Tray when clicking close, or exit completely.
- **Crash Prevention & Tray Lifecycle**:
  - Intercepted Avalonia window disposal on close to prevent `ObjectDisposedException` on Tray restore.
- **Streamlined Header Bar**:
  - Removed redundant manual Start/Stop button; monitoring runs 100% automatically.

### OCR & Data Synchronization Fixes
- **OCR Leading Digit Truncation Fix**:
  - Added token and symbol pre-processing to eliminate truncated leading digits on high balance amounts (`aUEC`, `¤`, `|`).
  - Widened default capture scan bounds (500x80) with safety margins.
  - Live session balance and saldo recomputation immediately synced on mobiGlas scan.
- **UEX Corp API Key Persistence**:
  - Full two-way data-binding and verified storage in `%APPDATA%\SCLogMate\settings.json`.

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
