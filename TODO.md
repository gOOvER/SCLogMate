# 📋 ToDo-Liste: Was fehlt noch von RC2 & Vollständige SCWiki-Integration

Dieses Dokument erfasst alle noch offenen Funktionen aus der **Avalonia RC2-Version**, die vollständige **Star Citizen Wiki (SCWiki)** Integration inklusive Bild-Caching sowie die geplante **Chat-Protokollierung per OCR**.

---

## 🌐 1. Vollständige Star Citizen Wiki (SCWiki) Integration

Ziel: Vollständige In-App Integration des Star Citizen Wikis ohne externe Browser-Tabs, mit lokalem Disk-Cache für HD-Bilder und strukturierter Speicherung technischer Datenblätter in SQLite für 100% Offline-Fähigkeit.

### 1.1 Lokaler HD-Bilder- & Asset-Cache
- [ ] **`Core/WikiImageCache.cs` implementieren:**
  - Automatischer Download aller Render-Bilder, Schemata und Thumbnails bei Abruf.
  - Persistente Speicherung unter `%APPDATA%\SCLogMate\cache\wiki\images\{hash}.webp`.
  - Bereitstellung lokaler Bilddaten für das Web-Frontend über Base64-Data-URIs oder lokale Pfade.
  - Offline-Garantie: Wenn offline oder im Flugbetrieb, werden gecachte Bilder blitzschnell von Disk geladen.
- [ ] **Automatischer Background-Prefetch:**
  - Sanftes Vorladen von Bildern für häufig geflogene Schiffe der aktuellen Flotte und gelagerte Gegenstände.

### 1.2 Erweiterter SQLite-Cache (`Database.cs`)
- [ ] **Schema-Version bumpen (v20):**
  - Neue Tabelle `wiki_vehicles_cache`:
    - `name`, `model_normalized`, `manufacturer`, `role`, `type`, `production_status`
    - `crew_min`, `crew_max`, `cargo_scu`, `quantum_fuel_capacity`
    - `speed_scm`, `speed_max`, `mass`, `length`, `beam`, `height`
    - `weapons_summary`, `shields_summary`, `quantum_drive_default`
    - `description_de`, `description_en`, `web_url`, `image_url`, `local_image_path`, `msrp`, `updated_at`
  - Bestehende Tabelle `wiki_items_cache` erweitern:
    - Technische Werte für Waffen (DPS, Feuerrate, Magazingröße)
    - Rüstungswerte (Schadensreduktion in %, Temperatur-Widerstandsbereich)
    - Item-Grade, Item-Klasse und Baugrößen (S1–S5)

### 1.3 IPC-Bridge & Backend-Handler (`PhotinoBridge.cs`)
- [ ] Handler `lookup_wiki`:
  - Nimmt Schiff- oder Item-Namen bzw. CIG-Klassennamen (`grin_utility_medium_helmet_01_01_04`) entgegen.
  - Prüft 1. Memory-Cache -> 2. SQLite-Cache -> 3. SCWiki API v2.
  - Lädt Bild im Hintergrund in Disk-Cache und gibt vollständiges Datenblatt zurück.
- [ ] Handler `search_wiki`:
  - Volltextsuche über Schiffe, Fahrzeuge, Waffen und Rüstungen.
- [ ] Handler `get_wiki_specs`:
  - Liefert strukturierte Hardpoint- und Komponentenübersichten für Schiffe.

### 1.4 In-App SCWiki Dossier Modal (`frontend/src/components/WikiDossierModal.tsx`)
- [ ] **Modernes SC-Glassmorphism Modal:**
  - **HD-Header:** Hochauflösendes 3D-Render des Schiffs/Gegenstands mit Hersteller-Crest.
  - **Spezifikationen-Grid:** Kacheln für Besatzung (Min/Max), Fracht (SCU), Schilde, Quantum Drive, Waffen/Türme.
  - **Lore & Beschreibung:** Priorisiert echte deutsche Übersetzung, umschaltbar auf Englisch.
  - **In-Game Händler & Preise:** Wo im Verse (z. B. *New Deal*, *Astro Armada*, *Cousin Crows*) für aUEC kaufbar oder mietbar.
  - **Web-Link:** Button zum Öffnen des offiziellen Eintrags auf `starcitizen.tools` / `star-citizen.wiki`.

### 1.5 Universelle Trigger in der Benutzeroberfläche
- [ ] **HUD (Karte 3 - Aktives Schiff):** Klick auf „Wiki“ öffnet das interne Schiffsdossier-Modal.
- [ ] **Flotten-Manager (`FleetView.tsx`):** Klick auf eine Schiffskarte öffnet das vollständige SCWiki-Datenblatt.
- [ ] **Lagerbestand (`WarehouseView.tsx`):** Klick auf Gegenstand oder Kontext-Aktion „Im Wiki anzeigen“ öffnet das Item-Dossier.
- [ ] **Chronik (`EventsView.tsx`):** Direktes Nachschlagen von Schiffen und Beute-Items aus dem Ereignis-Log.
- [ ] **Baupläne & Ausrüstung (`BlueprintsView.tsx`, `LoadoutView.tsx`):** Klick auf Komponenten/Waffen öffnet das Dossier.

---

## 🕹 2. Was fehlt noch von RC2 (Avalonia vs. Photino)

Im direkten Vergleich zur Avalonia Release-Candidate-Version (RC2) sind folgende Features noch zu migrieren oder anzupassen:

### 2.1 Native Desktop-Overlays & Pop-out Fenster
- [ ] **Floating Mini-HUD Overlay (`Alt + H`):**
  - In RC2: Ein transparentes, rahmenloses Always-on-Top Fenster mit Click-Through Modus (`WS_EX_TRANSPARENT`), das direkt über dem Vollbild-Spiel schwebt.
  - In Photino: Bisher nur im Hauptfenster integriert. Erfordert separates transparentes Win32-Overlay-Fenster oder Pop-out für echte Multimonitor- & In-Game-Nutzung.
- [ ] **RS-Scan Overlay Window:**
  - In RC2: Separates transparentes Radar- und Signatur-Overlay über Star Citizen zur Erkennung von Asteroiden & Ressourcen.
  - In Photino: Als Tab vorhanden (`OreScannerView.tsx`), transparenter In-Game-Modus steht noch aus.
- [ ] **Desktop OCR Region-Selector:**
  - In RC2: Interaktiver Bildschirm-Rahmen (`RegionSelectorWindow.axaml`), mit dem man direkt auf dem Desktop ein Rechteck über den Kontostand oder Auftrag zieht.
  - In Photino: Bisher nur numerische Koordinaten-Eingabe in den Einstellungen.
- [ ] **OCR Scan-Indicator:**
  - In RC2: Kurzer visueller grüner/gelber Blitz am Bildschirmrand, wenn die Geldbörse per OCR erfolgreich erfasst wurde.
- [ ] **In-Game Desktop Toasts:**
  - In RC2: Native Desktop-Toasts außerhalb des App-Fensters über dem Spiel (für Frachtaufzüge, Schiffszerstörung, Missionsabschluss).

### 2.2 Tabellen-Interaktionen & Kontextmenüs
- [ ] **Vollständige Rechtsklick-Kontextmenüs nachrüsten:**
  - *Events-Tabelle:* „Im Wiki nachschlagen“, „Zeile kopieren“, „Als Schnellfilter setzen“, „Ereignis exportieren“.
  - *Flotten-Tabelle:* „Schiffsdossier öffnen“, „Flugstatistik kopieren“.
  - *Finanzbuch:* „Buchung filtern“, „Transaktions-ID kopieren“.
  - *Lagerbestand:* „Im Wiki nachschlagen“, „Standort filtern“, „Menge manuell anpassen“.

### 2.3 Datenbank-Diagnose & Tiefenprüfung
- [ ] **Detaillierte Struktur-Validierung in der UI:**
  - Prüfung aller 8 Tabellen und 12 Indizes auf physische Integrität (`PRAGMA quick_check;`).
  - Detaillierte Spalten- und Typenvalidierung direkt in der Diagnosetabelle.
  - 1-Klick „Datenbank reparieren & optimieren“ (Vacuum, Reindex, Migrationen nachziehen).

---

## 💬 3. In-Game Chat-Verlauf & OCR-Protokollierung (Player Reports)

Ziel: Automatisches Protokollieren des In-Game Chats per OCR zur Beweissicherung und extrem einfachen Erstellung von CIG-Support-Tickets bei Griefing, Beleidigungen, Piraterie oder Belästigung.

### 3.1 OCR Chat-Scanner (`Core/Ocr/ChatOcrScanner.cs`)
- [ ] **Hintergrund-Scanner für das Chatfenster:**
  - OCR-Erfassung des Star Citizen Chat-Bereichs (konfigurierbare Bildschirm-Region).
  - Multi-Kanal-Erkennung: *Global Chat*, *Party Chat*, *Direct Message (Whisper)*, *System/Channel*.
  - Schrifterkennung optimiert für Star-Citizen-Chatfont und wechselnde Schiffshintergründe.

### 3.2 Strukturierte Persistenz (`Core/Database.cs`)
- [ ] **Tabelle `chat_messages`:**
  - `id` (INTEGER PRIMARY KEY)
  - `timestamp` (TEXT, ISO-8601 & lokales Format)
  - `channel` (TEXT: Global / Party / Direct)
  - `sender_handle` (TEXT: Spielername / Absender)
  - `message_text` (TEXT: Vollständiger Wortlaut)
  - `session_id` (INTEGER: Zugeordnete Spielsitzung)
  - `server_shard` (TEXT: Shard-Nummer zum Zeitpunkt der Nachricht)
  - Indizes auf `timestamp`, `sender_handle` und `session_id`.

### 3.3 Chat-Chronik & Report-Center in der UI (`ChatLogView.tsx`)
- [ ] **Durchsuchbarer Chat-Viewer:**
  - Übersichtlicher Chatverlauf mit farblichen Kennzeichnungen je nach Kanal.
  - Volltext-Suche nach Begriffen, Beleidigungen oder Handelsangeboten.
  - Spieler-Filter: Mit 1 Klick alle Chatbeiträge eines bestimmten Handles isolieren.
- [ ] **1-Klick CIG Support-Report Export:**
  - Vorfall markieren (Mehrfachauswahl betroffener Zeilen).
  - Button **„CIG Support Report generieren“**:
    - Erstellt einen fertig formatierten Report für das RSI-Support-Ticket:
      - Exakte Tatzeitpunkte (UTC + Lokalzeit)
      - Shard-ID und Server-Region (z. B. `pub_euw1b_12545750_050 · EU`)
      - Beteiligter Spielername / Handle
      - Wörtlicher Chatverlauf als lückenloser Beweis
      - Optionaler Screenshot-Ausschnitt des Chatfensters
    - 1-Klick Kopieren in die Zwischenablage für das Zendesk-Ticket.

---

## 🚀 Prioritäten & Umsetzungsreihenfolge

1. **Phase 1 (Sofort):** SCWiki Vollintegration (Lokaler Bild-Cache auf Disk, SQLite-Spezifikationen, `WikiDossierModal.tsx`, Trigger im HUD & Flotte).
2. **Phase 2:** Rechtsklick-Kontextmenüs in den Tabellen & tiefe DB-Diagnose (`quick_check`).
3. **Phase 3:** Chat-Verlauf mit OCR & 1-Klick Support-Report Generator.
4. **Phase 4:** Native Always-on-Top Desktop-Overlays (Mini-HUD `Alt+H`, RS-Scan & Region-Selector).
