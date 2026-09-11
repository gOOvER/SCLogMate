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
- [ ] **Lokaler HD-Bilder- & Asset-Cache (`Core/WikiImageCache.cs`):**
  - Automatischer Download und permanentes Caching aller Schiffs-Render, Item-Grafiken und Thumbnails unter `%APPDATA%\SCLogMate\cache\wiki\images\`.
  - 100% Offline-Verfügbarkeit und verzögerungsfreie Darstellung in allen Modals und Listen ohne externe Bild-Ladezeiten.
- [ ] **Erweiterter SQLite-Cache (`wiki_vehicles_cache` & `wiki_items_cache`, Schema v20):**
  - Persistente Speicherung vollständiger Schiffsspezifikationen: *Rolle, Typ, Fokus, Hersteller, Besatzung (Min/Max), Frachtkapazität (SCU), Quantum-Treibstoff, Triebwerke, Waffen-Hardpoints, Schilde, Abmessungen (L×B×H, Masse), Pledge-Preis (MSRP) und deutscher Beschreibungstext*.
  - Speicherung detaillierter Item-Werte (Waffen-DPS, Magazingröße, Rüstungsklasse, Temperatur- und Schadenswiderstände).
- [ ] **In-App SCWiki Dossier Modal (`frontend/src/components/WikiDossierModal.tsx`):**
  - Modernes In-App Glassmorphism-Dossier anstelle externer Browser-Links:
    - HD-Render mit Hersteller-Badge und Kategorisierung.
    - Vollständige Spezifikations-Kacheln (Crew, SCU, Schilde, Quantum Drive, Waffen).
    - Lokalisierte Lore & Beschreibung (Deutsch priorisiert, Umschalter auf Original-Englisch).
    - In-Game Kauf- & Mietorte mit aUEC-Preisen (z. B. *New Deal*, *Astro Armada*).
- [ ] **Universelle Trigger in der gesamten App:**
  - **HUD (Karte 3 - Aktives Schiff):** 1-Klick auf „Wiki“ öffnet direkt das interne Schiffsdossier.
  - **Flotte (`FleetView.tsx`):** Klick auf eine Schiffskarte öffnet das vollständige SCWiki-Datenblatt.
  - **Lager / Warehouse (`WarehouseView.tsx`):** Klick auf einen Gegenstand oder Menüaktion öffnet das Item-Dossier.
  - **Chronik (`EventsView.tsx`):** Direktes Nachschlagen von Schiffen und Beute aus dem Ereignis-Log.

---

## 💬 7. In-Game Chat-Verlauf & OCR-Protokollierung (Player Reports)
- [ ] **OCR-basierte Chat-Erfassung (`Core/Ocr/ChatOcrScanner.cs`):**
  - Dedizierte, ressourcenschonende OCR-Erfassung des Star Citizen Chat-Fensters (Global, Party, Direct Message, Channel).
  - Robust gegen variierende Transparenzen, Chat-Schriftarten und Hintergründe.
- [ ] **Strukturierte Chat-Chronik mit Zeitstempel & User-ID (`Core/Database.cs`):**
  - Automatisches Parsen von:
    - **Präziser Zeitstempel** (`[YYYY-MM-DD HH:mm:ss]`).
    - **Sender / Spielername** (`User / Handle`) inkl. Farb-/Kanal-Zuordnung (Global / Party / Flüstern).
    - **Kanal-Kennung** (`Global`, `Party`, `Direct`).
    - **Vollständiger Nachrichten-Text**.
  - Speicherung in dedizierter SQLite-Tabelle `chat_messages` mit Indizes auf Zeitstempel und Spielername.
- [ ] **Chat-Chronik & Such-Explorer in der UI (`frontend/src/views/ChatLogView.tsx`):**
  - Durchsuchbarer Chat-Verlauf mit Live-Volltextsuche und Spieler-Filter (z. B. alle Nachrichten eines bestimmten Spielers isolieren).
  - Schnellauswahl nach Zeitraum / Spielsitzung.
- [ ] **1-Klick CIG Support-Report Export:**
  - Einfaches Zusammenstellen und Exportieren von Vorfällen (Griefing, Beleidigungen, Belästigung, Erpressung, Piraterie) für den RSI / CIG Player Support.
  - Formatierte Zusammenfassung mit exakten Zeitstempeln, Shard-ID, Server-Region, Spielernamen, wörtlichem Chatprotokoll und optionalem Screenshot-Auszug als saubere Text-/Markdown-Vorlage für das Support-Ticket.

---

## 🕹 8. Ausstehende RC2-Parität (Avalonia vs. Photino)
- [ ] **Native Desktop-Overlays:**
  - *Floating Mini-HUD Overlay (`Alt + H`):* Separates, transparentes Always-on-Top Fenster mit Click-Through Modus (`WS_EX_TRANSPARENT`) über dem Vollbild-Spiel.
  - *RS-Scan Overlay Window:* Transparente Signatur- und Radaranzeige direkt über Star Citizen.
  - *Desktop OCR Region-Selector:* Interaktiver Bildschirm-Rahmen auf dem Desktop zum Auswählen der Scan-Region per Maus.
  - *Scan-Indicator:* Kurzer optischer Blitz am Monitorrand bei erfolgreichem Wallet-OCR-Scan.
  - *In-Game Desktop Toasts:* Native Desktop-Toasts außerhalb des App-Fensters über dem Spiel (Frachtaufzüge, Schiff zerstört).
- [ ] **Rechtsklick-Kontextmenüs in Tabellen:**
  - Zeilenaktionen für Chronik-, Flotten-, Finanz- und Lagertabellen (Wiki aufrufen, Werte in Zwischenablage kopieren, Schnellfilter setzen).
- [ ] **Tiefenprüfung der Datenbank:**
  - Tabellen- und Spalten-Integritätsprüfung (`PRAGMA quick_check;`) und 1-Klick Reparatur in der UI.


