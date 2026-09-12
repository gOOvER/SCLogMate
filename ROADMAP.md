# 🚀 SCLogMate — Feature Roadmap & ToDo-Liste

Dieses Dokument sammelt geplante Erweiterungen, Optimierungen und Feature-Konzepte für zukünftige Versionen von **SCLogMate**.

---

## 🎯 Prioritäre ToDo: UI/UX Angleichung an SCLogMate RC2 (React + Photino)

Entscheidung getroffen: **Wir bleiben bei React + Photino.NET**. Die folgenden Schritte gleichen das React-Frontend Schritt für Schritt an das vertraute, hochdichte Star-Citizen-HUD-Design der Release-Candidate-Version (RC2) an:

- [x] **Schritt 1: Globaler Rahmen, Master-Header & 2-Zeilen-HUD (Shell)**
  - **Oberste Leiste:** Star Citizen Prozess-Status Badge (`● LIVE` / `○ Offline`), Overlay-Schnellstarter (`🖥 Mini-HUD`, `🛰 RS-Overlay`), Sprachwähler mit echten Länderflaggen (DE / EN) und Versions-Pill.
  - **mobiGlas Session-Strip:** Prominenter Session-Auswahlbalken direkt unter dem Header mit Dropdown aller erfassten Spielsitzungen und leuchtender Zeitspannen-Pille (`SessionSpanText`).
  - **Permanentes 2-Zeilen SC-HUD (oben, einklappbar):**
    - *Zeile 1:* Pilot & Server (Region, Shard-Nummer, SC-Version) · Standort & Jurisdiktion (mit leuchtendem Waffenruhe/Armistice-Badge) · Aktives Schiff (Hersteller, Typ, Status).
    - *Zeile 2:* Kontostand & Saldo (Live-aUEC, Session-Delta, OCR-Badge) · Aktiver Auftrag / Mission · Flugdaten & Telemetrie.
    - Einklapp-Funktion per Toggle für maximalen Platz bei Karten & Tabellen.
- [x] **Schritt 2: Chronik & Ereignisse (`EventsView.tsx`)**
  - Dichte, tabellarische Darstellung der Ereignisse mit farbigen Event-Pills (Finanzen, Kampf, Schiffe, Missionen, Orte).
  - Such- und Filterleiste wie in RC2 inkl. ausziehbarem Detail-Drawer mit Rohdaten.
- [x] **Schritt 3: Finanzen & Saldo (`FinancesView.tsx`)**
  - Angleichung der Bilanzkarten (Gesamtguthaben, Sitzungs-Delta, Handelsgewinne).
  - Zeitverlaufs-Chart und tabellarisches Transaktionsbuch für aUEC-Transfers und Rohstoffeinkäufe/-verkäufe.
- [x] **Schritt 4: Lagerbestand / Warehouse (`WarehouseView.tsx`)**
  - Exakte Zwei-Spalten-Struktur: Links Planeten & Raumstationen mit Item-Zählern, rechts Artikeltabelle.
  - Direkte In-Grid Schnellanpassungen (`+` / `-`) und Frachtaufzug-Aktionen (`📦 Per Frachtaufzug entnehmen`, `🔧 Zerlegt`, `Standort leeren`).
- [x] **Schritt 5: Flotte, Aufträge, Ansehen & weitere Detail-Views**
  - Schrittweise Angleichung von Typografie, Tabellenstilen und Informationsdichte an die Avalonia-Vorlage.
  - Automatischer Wechsel nach Datenbank-Synchronisation (`DbUpdateModal.tsx`): 3s-Countdown mit "Weiter (3s)"-Button, der nach Abschluss des Syncs selbsttätig in die App übergeht.

---

## 🗺 1. Starmap & Navigation (Sternenkarte)
- [x] **Sprungreise- & Flugzeit-Rechner (Vector Route):**
  - Zeichnen einer dynamischen Fluglinie vom aktuellen Spieler-Standort zum ausgewählten Zielobjekt auf dem Radar.
  - Berechnung der echten Distanz in Gigametern (GM) und Kilometern.
  - Schätzung der Reisezeit basierend auf konfigurierbaren Sprungantrieben (z. B. S1 Atlas/VK-00, S2 Crossfield, S3 TS-2).
- [x] **Vollständiges Lagrange-Netzwerk (L1 – L5):**
  - Ergänzung aller Lagrange-Stationen rund um Hurston, Crusader, ArcCorp und microTech (*HUR-L1 bis L5, CRU-L1/L4/L5, ARC-L1 bis L4, MIC-L1 bis L5*).
  - Visuelle Badges für Stations-Spezialisierungen (*Raffinerie, Frachtzentrum, Rest Stop, Klinik*).
- [x] **Inter-System Sprungtor-Netzwerk (Jump Gates):**
  - Visuelle Tunnelsysteme zwischen Stanton, Pyro und Nyx.
  - 1-Klick Systemwechsel bei Klick auf ein Sprungtor.
  - Anzeige von Tor-Größen (*S / M / L*) und Transit-Details.
- [x] **Sicherheits- & Gefahrenzonen-Overlays:**
  - Farbcodierte visuelle Zonen für UEE-Waffenruhe (Grün/Cyan) vs. gesetzlose Piraten-Sektoren (Rot/Orange, z. B. GrimHEX, Ruin Station).
- [x] **Rohstoff- & Mining-Hotspots:**
  - Filterbare Markierung von Planeten und Asteroidengürteln mit Vorkommen (*Quantanium, Gold, Beryll, RMC-Wracks*).
- [x] **Pop-out Navigations-Großfenster & Immersives Vollbild:**
  - Separates, maximierbares Starmap-Fenster für Multimonitor-Setups sowie 1-Klick Ausblenden der oberen Dashboard-Karten für maximale Arbeitsfläche.

---

## 📜 2. Log-Parsing & Ereignis-Erkennung
- [x] **Erweiterte Fahrzeug- & Schiffs-Ereignisse:**
  - Präzise Erfassung von Schiffszerstörungen, Selbstzerstörung, Versicherungs-Claims, ATC-Landefreigaben und Fracht-/Schiffsaufzügen (SC 4.x Freight & Ship Elevators).
- [x] **Fraktionsruf & Rufstufen-Tracking:**
  - Automatische Berechnung von Rufstufen bei Auftraggebern (*Bounty Hunters Guild, Northrock, Crusader Security, Hurston Sec, microTech Sec, BlacJac, CDF, Red Wind, United Cargo, Covalex, Recco, Twitch, Wallace Klim, Clovus, Ruto*) anhand abgeschlossener Missionen inkl. Stufenfortschritt (Rang 1 bis 6) & SQLite-Persistenz.
- [x] **Gruppen- & Mehrspieler-Erweiterungen:**
  - Automatisches Erfassen von Gruppen-Belohnungen, Crew-Zusammensetzung und geteilten Missionen.
- [x] **Veredelungs- & Raffinerie-Tracking:**
  - Log-Erkennung von Veredelungsaufträgen und Statusmeldungen.

---

## 🥋 3. Piloten-Ausrüstung & Loadout
- [x] **Waffen-Aufsätze & Modifikationen:**
  - Erkennung von Visieren, Schalldämpfern, Kompensatoren und Magazingrößen an ausgerüsteten Primär- und Sekundärwaffen.
- [x] **Rüstungsklassen & Umgebungsschutz:**
  - Anzeige von Rüstungstypen (*Leicht, Mittel, Schwer, Hazmat, Fliegeranzug*) sowie Temperaturwiderständen (-225°C bis +225°C) und Schadenswiderständen (20% bis 40%) im Ausrüstungs-Tab.
- [x] **Loadout-Export & Sharing:**
  - 1-Klick Export der aktuellen Ausrüstung als Text für Discord / Zwischenablage sowie formatierter Markdown-Report (`.md`) für Org-Einsätze und Flotten-Briefings.

---

## 💰 4. Handel, Wirtschaft & Finanzen
- [x] **Raffinerie-Timer & Benachrichtigungen:**
  - Visueller Live-Countdown für aktive Veredelungsaufträge (*Station, Material, Methode, Ertrag, Status*) mit automatischer Toast-Benachrichtigung bei Fertigstellung.
- [x] **Handelsrouten-Empfehlungen & Profit-Kalkulator:**
  - Katalog profitabler Fracht- & Handelsrouten für Stanton & Pyro inkl. Echtzeit-Gewinnberechnung je nach Schiffsladekapazität (SCU).
- [x] **Flotten-Gesamtwert & Hangar-Verzeichnis:**
  - Schätzung des Gesamtwerts aller im Hangar geflogenen Schiffe in aUEC sowie Zählung individueller Flüge und QT-Sprünge.
- [x] **Loot- & Beute-Wert-Schätzer:**
  - Automatische Bepreisung erbeuteter Ausrüstung und Waffen anhand gängiger In-Game Händlerpreise.

---

## 🗔 5. In-Game Overlays & System
- [x] **Globaler Hotkey (`Alt + H`):**
  - Schnelles Ein-/Ausblenden des Mini-HUDs direkt im Vollbild-Spiel per systemweitem Tastenkürzel (`Alt + H`), ohne aus dem Spiel tappen zu müssen.
- [x] **HUD-Sperre (Click-Through & Position-Lock):**
  - Verriegeln des Mini-HUDs gegen versehentliches Verschieben sowie Click-Through Modus (`WS_EX_TRANSPARENT`).
- [x] **Modulare Toast-Kategorien & Soundeffekt:**
  - Granulare Checkboxen für alle Toast-Typen (*Baupläne, Missionen, Fraktions-Beförderungen, Raffinerie, Aufzüge, Schiffszerstörung*) inkl. dezentem Audio-Soundeffekt.
- [x] **Multi-Monitor Profiling:**
  - Automatisches Speichern und Wiederherstellen separater Overlay-Positionen.

---

## 🌐 6. Star Citizen Wiki (SCWiki) Vollintegration & Lokaler Cache

### 🎯 Das Ziel: 100% In-App SCWiki-Integration mit Offline-Cache & HD-Bildern

```mermaid
flowchart LR
    A[UI Klick: Schiff / Item / HUD] --> B[PhotinoBridge: lookup_wiki]
    B --> C{SQLite Cache?}
    C -- Ja --> D[Sofortige Rückgabe aus DB]
    E --> F[Lokaler HD-Bilder Cache auf Disk]
    E --> G[Speichern in SQLite]
    F --> H[WikiDossierModal in React]
    G --> H
    D --> H
```

### 📝 Aufgaben für die SCWiki-Vollintegration:
- [x] **Lokaler HD-Bilder- & Asset-Cache (`Core/WikiImageCache.cs`):**
  - Download und dauerhaftes Speichern aller Render-Bilder und Thumbnails unter `%APPDATA%\SCLogMate\cache\wiki\images\{hash}.webp`.
  - Bereitstellung der lokalen Cachedateien für das React-Frontend (über Base64 Data-URIs oder lokales Dateiprotokoll).
  - Automatischer Fallback: Falls offline oder noch nicht gecacht, wird das Remote-CDN geladen und parallel im Hintergrund gecacht.
- [x] **Erweiterter SQLite-Speicher (`Core/Database.cs`, Schema v20):**
  - **Neue Tabelle `wiki_vehicles_cache`:**
    - Speicherung vollständiger Schiffsspezifikationen: *Rolle, Typ, Fokus, Hersteller, Besatzung (Min/Max), Frachtkapazität (SCU), Quantum-Treibstoff-Kapazität, Triebwerke, Waffen-Hardpoints, Schilde, Kühler, Abmessungen (L×B×H, Masse), Pledge-Preis (MSRP) und deutscher Beschreibungstext*.
  - **Bestehende Tabelle `wiki_items_cache` erweitert um:**
    - Schadenswerte, Feuerrate, Rüstungsklasse, Temperaturwiderstände, Item-Grade und Typ.
- [x] **Backend IPC-Bridge (`Core/Photino/PhotinoBridge.cs`):**
  - Neuer Handler `lookup_wiki`: Sucht Schiffe oder Items anhand Name oder interner CIG-Klasse (`grin_utility_medium_helmet_01_01_04`).
  - Neuer Handler `search_wiki`: Volltextsuche über Schiffe, Fahrzeuge, Waffen, Rüstungen und Module.
  - Neuer Handler `get_wiki_specs`: Liefert strukturierte Datenblätter für Schiffe und Ausrüstung.
- [x] **Interaktives SCWiki-Dossier-Modal (`frontend/src/components/WikiDossierModal.tsx`):**
  - Modernes Star-Citizen-Glassmorphism-Modal direkt in der App:
    - **HD-Header:** Vollbild-Render des Schiffs/Items mit Hersteller-Logo und Kategorie-Pill.
    - **Lore & Beschreibung:** Deutsche Übersetzung priorisiert, mit Umschalter auf Original-Englisch.
    - **Spezifikationen-Grid:** Kacheln für Besatzung, Fracht (SCU), Schild-Größen, Quantum Drive, Waffen/Türme.
    - **Kauf- & Mietorte:** Wo im Verse (z. B. *New Deal*, *Astro Armada*, *Cousin Crows*) ist das Schiff/Item für aUEC erhältlich?
    - **Externer Link:** Direkter Button zum Öffnen des Eintrags im echten Wiki (`starcitizen.tools` / `star-citizen.wiki`).
- [x] **Nahtlose Einbindung an allen Berührungspunkten in der UI:**
  - **Startseite / HUD (Karte 3 - Aktives Schiff):** Klick auf „Wiki“ öffnet das Schiffsdossier-Modal.
  - **Flotten-Manager (`FleetView.tsx`):** Klick auf ein beliebiges Schiff öffnet das vollständige SCWiki-Datenblatt.
  - **Lagerbestand (`WarehouseView.tsx`):** Klick auf Gegenstand oder Aktionsmenü „Im Wiki anzeigen“ öffnet das Item-Dossier.
  - **Ereignis-Chronik (`EventsView.tsx`):** Kontextmenü / Button bei Log-Einträgen mit Schiffen oder Beute.
  - **Ausrüstung & Baupläne (`LoadoutView.tsx`, `BlueprintsView.tsx`):** Klick auf Waffen/Rüstungen/Komponenten öffnet das SCWiki-Dossier.
- [x] **Eigener SCWiki-Browser / Explorer (Neuer Reiter oder Untermenü):**
  - Katalog aller Star-Citizen-Schiffe, -Fahrzeuge und -Waffen mit Live-Suche, Herstellerfiltern (Anvil, Drake, RSI, Aegis, Origin etc.) und Direktansicht.

---

## 💬 7. In-Game Chat-Verlauf & OCR-Protokollierung (Player Reports) — [Vorübergehend pausiert / Deaktiviert]
- [ ] **OCR-basierte Chat-Erfassung (`Core/Ocr/ChatOcrScanner.cs`):** *(Pausiert für Rework, siehe Schritt 9 am Ende der Roadmap)*
  - Dedizierte, ressourcenschonende OCR-Erfassung des Star Citizen Chat-Fensters (Global, Party, Direct Message, Channel).
  - Robust gegen variierende Transparenzen, Chat-Schriftarten und Hintergründe.
- [x] **Strukturierte Chat-Chronik mit Zeitstempel & User-ID (`Core/Database.cs`):**
  - Automatisches Parsen von:
    - **Präziser Zeitstempel** (`[YYYY-MM-DD HH:mm:ss]`).
    - **Sender / Spielername** (`User / Handle`) inkl. Farb-/Kanal-Zuordnung (Global / Party / Flüstern).
    - **Kanal-Kennung** (`Global`, `Party`, `Direct`).
    - **Vollständiger Nachrichten-Text**.
  - Speicherung in dedizierter SQLite-Tabelle `chat_messages` mit Indizes auf Zeitstempel und Spielername.
- [x] **Chat-Chronik & Such-Explorer in der UI (`frontend/src/views/ChatLogView.tsx`):**
  - Durchsuchbarer Chat-Verlauf mit Live-Volltextsuche und Spieler-Filter (z. B. alle Nachrichten eines bestimmten Spielers isolieren).
  - Schnellauswahl nach Zeitraum / Spielsitzung.
- [x] **1-Klick CIG Support-Report Export:**
  - Einfaches Zusammenstellen und Exportieren von Vorfällen (Griefing, Beleidigungen, Belästigung, Erpressung, Piraterie) für den RSI / CIG Player Support.
  - Formatierte Zusammenfassung mit exakten Zeitstempeln, Shard-ID, Server-Region, Spielernamen, wörtlichem Chatprotokoll und optionalem Screenshot-Auszug als saubere Text-/Markdown-Vorlage für das Support-Ticket.

---

## 🕹 8. Ausstehende RC2-Parität (Avalonia vs. Photino)

Im Vergleich zur Avalonia-Version (RC2) sind die meisten Kernbereiche (Chronik, Flotte, Finanzen, Starmap, Lager, Blackbox, Baupläne, Werkzeuge) bereits in React/Photino portiert. Folgende spezialisierte RC2-Features fehlen noch bzw. werden schrittweise überführt:

| Bereich | RC2 (Avalonia) | Photino (Aktueller Stand) | Status / ToDo |
|---|---|---|---|
| **SCWiki In-App Overlay** | Integriertes Modal mit Bild, Specs & deutscher Lore | Integriertes Glassmorphism-Modal mit HD-Render, Specs & Lore | 🟢 Erledigt (Abschnitt 6) |
| **Lokaler SCWiki Bild-Cache** | Nur Remote-URLs (kein permanenter Disk-Cache) | Vollständiger Disk-Cache unter %APPDATA%\cache\wiki\ | 🟢 Erledigt (Abschnitt 6) |
| **In-Game Chat-Chronik & Reports** | Nicht vorhanden | OCR-Chatlogger, SQLite-Speicherung & 1-Klick CIG Report | 🟢 Erledigt (Abschnitt 7) |
| **OCR Screen-Region-Selector** | Interaktiver Rahmen auf dem Desktop zum Zeichnen/Justieren der Scan-Region für Kontostand & Aufträge | Virtuelles Multi-Monitor Snipping-Tool über alle Displays mit Live-Pixelbadge & Tastaturkürzeln | 🟢 Erledigt (`NativeRegionSelector.cs`) |
| **Scan Indicator Window** | Grüner/Gelber visueller Flash-Indikator am Monitorrand bei OCR-Erfassung | Nativer transparenter Color-Key Rahmen mit grünem Bestätigungs-Flash | 🟢 Erledigt (`NativeScanIndicator.cs`) |
| **Floating Mini-HUD Overlay (`Alt+H`)** | Separates, transparentes, rahmenloses Always-on-Top Win32-Fenster mit Click-Through Modus direkt über dem Vollbild-Spiel | Web-Dashboard & Overlay-Schalter vorhanden | 🟡 Pop-out / Win32 Overlay Fenster finalisieren |
| **RS-Scan Overlay Window** | Separates transparentes Radar-/Signatur-Fenster über dem Spiel | Im Hauptfenster vorhanden (`OreScannerView.tsx`) | 🟡 Transparenter In-Game Always-on-Top Modus |
| **In-Game Desktop Toasts** | Transparente native Toasts über dem Spielfenster für Aufzüge, Schiffszerstörung, Missionsabschluss | Interne Web-Toasts innerhalb der App aktiv | 🟡 Native Win32 Always-On-Top Desktop-Toasts |
| **Rechtsklick-Kontextmenüs** | Kontextmenüs auf allen Tabellenzeilen (*Im Wiki nachschlagen, Zeile kopieren, Filter setzen*) | Meist nur Klick-Auswahl oder Detail-Drawer | 🟢 In React nachrüstbar |
| **DB-Diagnose & Schnell-Reparatur** | Visuelle Tabelle mit Prüfung aller Spalten, Tabellen, Indizes und `PRAGMA quick_check;` | Basis-Tools vorhanden | 🟢 UI-Angleichung an RC2-Diagnose |

---

## ⛏ 9. Industrial Mining & Refinery Suite (Bergbau, Veredelung & Warchest)

Umfassendes Industriemodul für Solo- und Gruppen-Bergbau, Veredelungsaufträge, Lagerverwaltung und Handelsoptimierung:

### 9.1 Refinery Job Tracking & OCR-Kiosk Logging
- [ ] **Vollständiges Auftrags-Management für Veredelungsaufträge (`RefineryView.tsx`, `Core/Database.cs`):**
  - Übersicht aller aktiven, verarbeiteten und abholbereiten Veredelungsaufträge über alle Raffineriestationen (*ARC-L1, CRU-L1, HUR-L1, HUR-L2, MIC-L1, Pyro-Raffinerien*).
  - Status-Phasen: *In Warteschlange*, *Wird veredelt (Live-Countdown)*, *Fertig zur Abholung*, *Eingelagert / Verkauft*.
  - Berechnung der Veredelungszeiten, Kosten und Materialerträge je nach gewählter Methode (*Cormack, Dinyx, Electrostatic, Ferron, Gencore, Pyroxeres, Thermite*).
  - Automatische Benachrichtigung (Desktop-Toast & Ton) bei Fertigstellung eines Auftrags.
- [ ] **OCR Screenshot- & Snipping-Erfassung von Raffinerie-Aufträgen (`RefineryOcrScanner.cs`):**
  - 1-Klick Bildschirmaufnahme oder interaktives Desktop-Snipping des In-Game Raffinerie-Terminals (*Refinery Kiosk*).
  - Automatisches Extrahieren von Ausgangs-Erzen, Mengen (cSCU / SCU), gewählter Methode, Endertrag, Gebühren und Fertigstellungsdatum direkt in die Datenbank.

### 9.2 Warchest Management & Qualitätsbewertung
- [ ] **Mineralien- & Rohstoff-Tresor (Warchest):**
  - Dedizierte Bestandsübersicht aller abgebauten Rohmineralien und veredelten Barren getrennt nach Standorten und Frachtbehältern.
  - **Qualitäts- & Reinheitsbewertung (Quality Ratings):** Erfassung von prozentualem Reinheitsgrad, Verunreinigungen (Inert Materials) und SCU-Dichte zur gezielten Veredelungs- und Verkaufsplanung.
  - Historische Gesamtertrags-Statistik und Entwicklung des Warchest-Wertes über Spielsitzungen hinweg.

### 9.3 Live UEX Corp Preis-Integration für Bergbau & Warchest
- [ ] **Echtzeit-Bewertung des Warchest-Bestands:**
  - Automatische Neuberechnung des Marktwertes aller gelagerten Rohstoffe und veredelten Güter basierend auf Live-Preisen der UEX Corp API.
  - Historische Preistrends und Margenwarnungen für Quantanium, Bexalite, Gold, Taranite, Larinite und RMC.

### 9.4 Rock Breaking & Laser-Power Rechner
- [ ] **Gesteinsbruch- & Überlastungs-Kalkulator:**
  - Physikalischer Bruch-Rechner: Eingabe von Gesteinsmasse (kg / t), Resistenz (%) und Instabilität.
  - Berechnung der benötigten Laserleistung (Watt / MW) zum Erreichen des optimalen Ladefensters (*Green Zone*).
  - Warnung vor Gesteins-Überladung, Instabilitäts-Spitzen und Explosions-/Shatter-Gefahr.

### 9.5 Multi-Crew & Team Mining Operations
- [ ] **Mehrspieler- & Flotten-Bergbau-Planer:**
  - Planung kooperativer Bergbau-Einsätze mit mehreren Schiffen und Lasern (z. B. Prospector-Paare, RSI MOLE mit bis zu 3 Geschütztürmen, Unterstützung durch Handlaser).
  - Berechnung kombinierter Laserleistungen, überlappender Modul-Boni und synchronisierter Hitzeregulierung.
  - Transparente Aufteilung von Ladekapazitäten, Kosten und Verkaufserlösen auf Crewmitglieder.

### 9.6 Optimal Loadout Finder & Mining Equipment Database
- [ ] **Optimal Loadout Finder (Intelligente Laser-Empfehlung):**
  - Algorithmus zur Ermittlung der optimalen Ausrüstungskombination (Laser-Kopf + aktive/passive Sub-Module + Gadgets) für gewünschte Zielerze und Gesteinsgrößen.
- [ ] **Mining Equipment Database:**
  - Vollständiger interaktiver Katalog aller Star-Citizen-Mining-Laser (*S1 & S2: Helix, Lancet, Impact, Hofstede, Klein, Arbor*), Sub-Module (*Surge, Focus, Brandt, Torrent, Stampede, Lifeline etc.*) und platzierbarer Gadgets (*BoreMax, Sabir, Optimax*).
  - Technische Kennzahlen: Laserleistung, Green Zone Multiplikator, Resistenz-Reduktion, Instabilitäts-Dämpfung, Shatter Damage und Modul-Slots sowie Kaufstationen und Preise.

### 9.7 Rock Knowledge & Pinnbare Favoriten
- [ ] **Erweiterte Gesteins- & Mineralien-Enzyklopädie:**
  - Detailliertes Nachschlagewerk zu allen Erzen, Edelmetallen und Gasen im Star-Citizen-Universum.
  - **Pinnbare Favoriten:** 1-Klick Favorisieren von Zielmineralien (z. B. Quantanium, Gold, Beryl) für priorisierte Hervorhebung im HUD, Decoder und Radar-Scanner.
  - Spezifische Kennzahlen zu Materialdichte, Explosionsgefahr, Verfallszeit (z. B. instabiles Quantanium) und typischer Gesteinszusammensetzung.

### 9.8 Standort- & Vorkommen-Atlas (Location Data)
- [ ] **Präziser Rohstoff- & Spawn-Atlas:**
  - Detaillierte Vorkommen-Datenbank: Wo spawnen bestimmte Erze und Gesteinstypen am häufigsten?
  - Filterung nach planetaren Oberflächen (*Daymar, Lyria, Magda, Aberdeen, Calliope, Euterpe, Wala*) vs. Asteroidengürteln (*Aaron Halo Bänder 5–10, Yela-Ring, Lagrange-Cluster*).
  - Anzeige von Cluster-Wahrscheinlichkeiten, Abbau-Schwierigkeit und Umgebungsbedingungen (Gravitation, Atmosphäre, extreme Temperaturen).

### 9.9 Mineral Tracking & Blueprint-Bedarfsliste (Shopping List)
- [ ] **Bedarfs-Tracker für Crafting & Baupläne:**
  - Verknüpfung der erlernten Baupläne (`BlueprintsView.tsx`) mit dem Rohstoffbedarf: Markieren benötigter Mineralien für geplante Crafting-Rezepte als Live-Einkaufsliste.
  - Dynamischer Soll/Ist-Abgleich mit dem aktuellen Lagerbestand (`Warehouse`) und dem Warchest-Tresor.
  - Direktanzeige der besten Fundorte für aktuell noch fehlende Komponenten.

### 9.10 Best Sales Locations (Routen-Optimierer für Warchest-Verkäufe)
- [ ] **Verkaufs- & Routen-Optimierer:**
  - Berechnet anhand des aktuellen Warchest-Inventars und der UEX-Echtzeitpreise den profitabelsten Verkaufsort im gesamten Verse (TDDs, Admin-Büros, Rohstoffhändler).
  - Berücksichtigung von maximalen Ankaufsmengen (Demand Caps), Terminal-Limits und Reisedistanzen für maximalen Netto-Stundengewinn.

---

## 🚀 Empfohlene nächste Umsetzungsschritte

- [x] **Schritt 1 (SCWiki Backend & Cache):**
  - Implementierung von `Core/WikiImageCache.cs` (Disk-Cache für Render-Bilder unter `%APPDATA%\SCLogMate\cache\wiki\images\`).
  - Tabelle `wiki_vehicles_cache` in `Database.cs` (Schema v20) für strukturierte Fahrzeug-Spezifikationen.
  - Erweiterung von `WikiApiClient.cs` & `PhotinoBridge.cs` um `lookup_wiki`.
- [x] **Schritt 2 (SCWiki UI Modal & Trigger):**
  - Erstellung des React-Modals `WikiDossierModal.tsx` mit HD-Bildern, deutscher Übersetzung und Spezifikationen-Kacheln.
  - Anbindung an das HUD (Schiffskarte), die Flotte (`FleetView.tsx`) und das Lager (`WarehouseView.tsx`).
- [x] **Schritt 3 (SCWiki Explorer / Suche):**
  - Eine Suchmaske zum Durchstöbern aller Schiffe, Fahrzeuge und Gegenstände des Star Citizen Wikis direkt in SCLogMate (`WikiExplorerView.tsx`).
- [x] **Schritt 4 (In-Game Chat-Verlauf & Player Reports - Abschnitt 7):**
  - OCR-basierte Chat-Erfassung, SQLite-Speicherung in `chat_messages` und 1-Klick CIG Support-Report Export.
- [x] **Schritt 5 (OCR Multi-Monitor & Universal-Kalibrierung):**
  - Multi-Monitor virtueller Desktop-Support für mobiGlas, RS Radar & Chat OCR ohne Abbruch auf Zweitmonitoren.
  - Universelle Kalibrierung und interaktive Bereichsauswahl in Einstellungen, Radar-Decoder und Chat-Chronik.
- [ ] **Schritt 6 (Refinery Job Tracking & OCR-Kiosk Logging):**
  - Erstellung von `RefineryView.tsx` mit Auftrags-Management, Countdown-Timern und OCR-Screenshot-Erfassung von Kiosk-Terminals.
- [ ] **Schritt 7 (Warchest Management & Best Sales Locations):**
  - Erfassung von Mineralienbeständen mit Reinheitsstufen (Quality Ratings) und automatischer Ermittlung der besten Verkaufsorte via UEX Corp API.
- [ ] **Schritt 8 (Rock Breaking Calculator & Optimal Loadout Finder):**
  - Laser-Power vs. Gesteinsmasse/Resistenz Rechner, Ausrüstungs-Datenbank und Multi-Crew Bergbau-Planer.
- [ ] **Schritt 9 (In-Game Chat-Protokoll & OCR-Rework — Backlog):**
  - **Komplette Überarbeitung der Chat-Texterkennung & Protokollierung:**
    - Trennung des Spielernamens / Senders in eine separate Tabellenspalte und UI-Spalte zur besseren visuellen Gliederung.
    - Zuverlässigere Textextraktion bei wechselnden Spielhintergründen und Visor-Transparenzen (Adaptive Thresholding / Hintergrund-Maskierung).
    - Verbesserte Vermeidung von Mehrfachlesungen stehender Nachrichten bei inaktivem Chatverlauf.
    - Reaktivierung des Navigationstabs und der Hintergrund-Erfassung nach erfolgreicher Fertigstellung.



