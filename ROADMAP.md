# 🚀 SCLogMate — Feature Roadmap & ToDo-Liste

Dieses Dokument sammelt geplante Erweiterungen, Optimierungen und Feature-Konzepte für zukünftige Versionen von **SCLogMate**.

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
