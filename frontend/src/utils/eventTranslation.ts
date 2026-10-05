import { Locale } from '../i18n/types';

/**
 * Maps known German event titles / kind texts to English and vice versa.
 */
const TITLE_DE_TO_EN: Record<string, string> = {
  Eingang: 'Income',
  Ausgang: 'Expense',
  Belohnung: 'Reward',
  Kauf: 'Purchase',
  Verkauf: 'Sale',
  Handel: 'Trade',
  Wartung: 'Maintenance',
  Standort: 'Location',
  Lager: 'Warehouse',
  Schiff: 'Ship',
  Quantum: 'Quantum',
  Mission: 'Mission',
  Gebiet: 'Jurisdiction',
  Party: 'Party',
  'Med-Bett': 'Medbed',
  Hangar: 'Hangar',
  Ausrüstung: 'Equipment',
  Angebot: 'Offer',
  Verlust: 'Ship Loss',
  Tod: 'Death',
  'Beschlagn.': 'Impound',
  Freund: 'Friend',
  Miete: 'Rental',
  Bauplan: 'Blueprint',
  Defekt: 'Defect',
  Kampf: 'Combat',
  'Auftrag ✓': 'Contract ✓',
  Strafe: 'Fine',
  Straftat: 'Crime',
  Veredelung: 'Refining',
  Verletzung: 'Injury',
  Loot: 'Loot',
  Auftraggeber: 'Contractor',
  Crash: 'Crash',
  Server: 'Server',
  Finanzen: 'Finance',
  Ort: 'Location',
  Auftrag: 'Mission',
  System: 'System',
  Info: 'Info',
};

const TITLE_EN_TO_DE: Record<string, string> = Object.entries(TITLE_DE_TO_EN).reduce(
  (acc, [de, en]) => {
    acc[en.toLowerCase()] = de;
    return acc;
  },
  {} as Record<string, string>
);

/**
 * Translates event titles / kinds according to the active locale.
 */
export function translateEventTitle(title?: string | null, locale: Locale = 'de'): string {
  if (!title) return '';
  const trimmed = title.trim();

  if (locale === 'en') {
    if (TITLE_DE_TO_EN[trimmed]) return TITLE_DE_TO_EN[trimmed];
    // Case-insensitive fallback
    const lower = trimmed.toLowerCase();
    for (const [de, en] of Object.entries(TITLE_DE_TO_EN)) {
      if (de.toLowerCase() === lower) return en;
    }
    return trimmed;
  }

  // locale === 'de'
  const lower = trimmed.toLowerCase();
  if (TITLE_EN_TO_DE[lower]) return TITLE_EN_TO_DE[lower];
  return trimmed;
}

/**
 * Translates event detail descriptions dynamically between German and English.
 */
export function translateEventDetail(text?: string | null, locale: Locale = 'de'): string {
  if (!text) return '';
  let d = text.trim();

  if (locale === 'en') {
    // ══ GERMAN -> ENGLISH TRANSLATION ══

    // 1. Armistice / Schutzzone & Jurisdiction
    if (d.includes('Schutzzone aktiv (Waffen blockiert)')) {
      return d.replace('Schutzzone aktiv (Waffen blockiert)', 'Armistice zone active (weapons locked)');
    }
    if (d.includes('Keine Schutzzone (Waffen scharf)')) {
      return d.replace('Keine Schutzzone (Waffen scharf)', 'Weapons free (no armistice)');
    }
    if (d.includes('Schutzzone verlassen (Waffen scharf)')) {
      return d.replace('Schutzzone verlassen (Waffen scharf)', 'Left armistice zone (weapons armed)');
    }
    if (d.includes('Sperrgebiet verlassen')) {
      return d.replace('Sperrgebiet verlassen', 'Left restricted area');
    }
    if (d.includes('⛔ Sperrzone: Zwangsumbettung (Relocated)')) {
      return '⛔ Restricted area: Relocated';
    }
    if (d.startsWith('🏛 Rechtsgebiet:')) {
      return '🏛 Jurisdiction:' + d.substring('🏛 Rechtsgebiet:'.length);
    }
    if (d.startsWith('Rechtsgebiet:')) {
      return 'Jurisdiction:' + d.substring('Rechtsgebiet:'.length);
    }
    if (d.includes('🏴 Ungesetzlicher Sektor')) {
      return d.replace('🏴 Ungesetzlicher Sektor', '🏴 Unlawful sector');
    }
    if (d.includes('📡 Überwachter Raum (Comm-Array aktiv)')) {
      return '📡 Monitored space (Comm-Array active)';
    }
    if (d.includes('📡 Unüberwachter Raum (Kein Comm-Array)')) {
      return '📡 Unmonitored space (No Comm-Array)';
    }

    // 2. Server & Session Connection
    if (d.includes('Server-Verbindung getrennt / Sitzung beendet (EndSession)')) {
      return 'Server disconnected / session ended (EndSession)';
    }
    if (d.includes('Server-Verbindung getrennt')) {
      return d.replace('Server-Verbindung getrennt', 'Server disconnected');
    }
    if (d.includes('Server verbunden / Shard erkannt')) {
      return 'Server connected / shard identified';
    }
    if (d.startsWith('Server beigetreten:')) {
      return 'Joined server:' + d.substring('Server beigetreten:'.length);
    }
    if (d.startsWith('Server beigetreten')) {
      return 'Joined server' + d.substring('Server beigetreten'.length);
    }
    if (d.startsWith('Server verbunden:')) {
      return 'Server connected:' + d.substring('Server verbunden:'.length);
    }
    if (d.includes('Verbindung getrennt (30000 Crash)')) {
      return 'Connection lost (30000 Crash)';
    }
    if (d.startsWith('Crash erkannt:')) {
      return 'Crash detected:' + d.substring('Crash erkannt:'.length);
    }
    if (d.includes('Im Spiel gespawnt (Station / Hangar)')) {
      return 'Spawned in game (Station / Hangar)';
    }
    if (d.includes('Respawn an medizinischer Einrichtung')) {
      return 'Respawned at medical facility';
    }

    // 3. Hangar, Landing & Takeoff
    if (d.includes('Hangar-Anforderung bereit / Tor geöffnet')) {
      return 'Hangar request ready / doors open';
    }
    if (d.includes('In Hangar-Warteschlange eingereiht')) {
      return 'Queued for hangar assignment';
    }
    if (d.includes('Hangar-Zuweisung erhalten')) {
      return 'Hangar assignment received';
    }
    if (d.startsWith('Landefreigabe:')) {
      return 'Landing clearance:' + d.substring('Landefreigabe:'.length);
    }
    if (d.includes('Landung eingeleitet')) {
      return d.replace('Landung eingeleitet', 'Landing initiated');
    }
    if (d.includes('Fahrwerk ausgefahren')) {
      return d.replace('Fahrwerk ausgefahren', 'Landing gear deployed');
    }
    if (d.includes('Fahrwerk eingefahren')) {
      return d.replace('Fahrwerk eingefahren', 'Landing gear retracted');
    }
    if (d.includes('Schiff gelandet')) {
      return d.replace('Schiff gelandet', 'Ship landed');
    }
    if (d.includes('Abflug eingeleitet')) {
      return d.replace('Abflug eingeleitet', 'Takeoff initiated');
    }

    // 4. Sorties, Pilot Seat & Fleet Status
    if (d.includes('(Sortie beendet / Pilotensitz verlassen)')) {
      return d.replace('(Sortie beendet / Pilotensitz verlassen)', '(Sortie ended / exited pilot seat)');
    }
    if (d.includes('(Pilotensitz eingenommen)')) {
      return d.replace('(Pilotensitz eingenommen)', '(Entered pilot seat)');
    }
    if (d.includes('(Sortie begonnen / Pilotensitz eingenommen)')) {
      return d.replace('(Sortie begonnen / Pilotensitz eingenommen)', '(Sortie started / entered pilot seat)');
    }
    if (d.includes('zerstört / Selbstzerstörung')) {
      return d.replace('zerstört / Selbstzerstörung', 'destroyed / self-destruct');
    }
    if (d.includes('– Kollision')) {
      return d.replace('– Kollision', '– Collision');
    }
    if (d.startsWith('Versicherungs-Claim:')) {
      return 'Insurance claim:' + d.substring('Versicherungs-Claim:'.length);
    }
    if (d.startsWith('🚀 Schiff freigeschaltet:')) {
      return d
        .replace('🚀 Schiff freigeschaltet:', '🚀 Ship retrieved:')
        .replace('(Abholbar am Ship Kiosk)', '(Ready at Ship Kiosk)');
    }
    if (d.startsWith('ASOP Flotte:')) {
      return d
        .replace('ASOP Flotte:', 'ASOP Fleet:')
        .replace('von', 'of')
        .replace('Schiffen einsatzbereit', 'ships ready')
        .replace('im Claim', 'claimed');
    }

    // 5. Missions & Objectives
    if (d.startsWith('Teilziel:')) {
      let sub = d.substring('Teilziel:'.length).trim();
      sub = sub.replace(/^Fracht geliefert/i, 'Cargo delivered');
      sub = sub.replace(/^Gegner neutralisiert/i, 'Targets neutralized');
      sub = sub.replace(/^Paket geborgen/i, 'Package recovered');
      sub = sub.replace(/^Paket abgeliefert/i, 'Package delivered');
      return 'Objective: ' + sub;
    }
    if (d === 'Missionsziel abgeschlossen') {
      return 'Mission objective completed';
    }
    if (d.startsWith('Missionsziel abgeschlossen:')) {
      return 'Mission objective completed:' + d.substring('Missionsziel abgeschlossen:'.length);
    }
    if (d === 'Auftrag erfolgreich abgeschlossen' || d === 'Auftrag abgeschlossen') {
      return 'Contract successfully completed';
    }
    if (d.startsWith('Auftrag erfolgreich abgeschlossen:')) {
      return 'Contract completed:' + d.substring('Auftrag erfolgreich abgeschlossen:'.length);
    }
    if (d.startsWith('Auftrag abgeschlossen:')) {
      return 'Contract completed:' + d.substring('Auftrag abgeschlossen:'.length);
    }
    if (d === 'Auftrag fehlgeschlagen') {
      return 'Contract failed';
    }
    if (d.startsWith('Auftrag fehlgeschlagen:')) {
      return 'Contract failed:' + d.substring('Auftrag fehlgeschlagen:'.length);
    }
    if (d === 'Auftrag abgebrochen') {
      return 'Contract abandoned';
    }
    if (d.startsWith('Auftrag abgebrochen:')) {
      return 'Contract abandoned:' + d.substring('Auftrag abgebrochen:'.length);
    }
    if (d === 'Auftrag storniert') {
      return 'Contract cancelled';
    }
    if (d.startsWith('Auftrag storniert:')) {
      return 'Contract cancelled:' + d.substring('Auftrag storniert:'.length);
    }
    if (d === 'Auftrag zurückgezogen') {
      return 'Contract withdrawn';
    }
    if (d.startsWith('Auftrag zurückgezogen:')) {
      return 'Contract withdrawn:' + d.substring('Auftrag zurückgezogen:'.length);
    }
    if (d.startsWith('Neues Missionsziel:')) {
      let rest = d.substring('Neues Missionsziel:'.length).trim();
      rest = rest.replace(/^Flugschreiber abliefern bei/i, 'Deliver Flight Recorder To');
      rest = rest.replace(/^Flugschreiber aus Wrack bergen in/i, 'Collect Flight Recorder From a wreck site in');
      return 'New Objective: ' + rest;
    }
    if (d.startsWith('Missions-Belohnung:')) {
      return 'Mission Reward:' + d.substring('Missions-Belohnung:'.length);
    }
    if (d.startsWith('Neuer Auftrag:')) {
      return 'New Contract:' + d.substring('Neuer Auftrag:'.length);
    }
    if (d.startsWith('Auftrag angenommen:')) {
      return 'Contract accepted:' + d.substring('Auftrag angenommen:'.length);
    }
    if (d.includes('(Missionsabgabe Frachtaufzug)')) {
      return d.replace('(Missionsabgabe Frachtaufzug)', '(Cargo elevator mission delivery)');
    }

    // 6. Quantum & Navigation
    if (d.startsWith('QT-Ankunft ·')) {
      return 'QT Arrival ·' + d.substring('QT-Ankunft ·'.length);
    }
    if (d.includes('QT-Sprung initiiert')) {
      return d.replace('QT-Sprung initiiert', 'QT Jump initiated');
    }
    if (d.includes('QT-Sprung beendet')) {
      return d.replace('QT-Sprung beendet', 'QT Jump completed');
    }
    if (d.includes('QT-Sprung abgebrochen')) {
      return d.replace('QT-Sprung abgebrochen', 'QT Jump aborted');
    }
    if (d.startsWith('QT nach ')) {
      return 'QT to ' + d.substring('QT nach '.length);
    }

    // 7. Combat, Damage & Deaths
    if (d.startsWith('☠ getötet von')) {
      return d.replace('☠ getötet von', '☠ Killed by');
    }
    if (d.startsWith('Kill:')) {
      return d;
    }
    if (d.includes('Wiederbelebt / Medizinische Behandlung')) {
      return 'Resuscitated / Medical treatment';
    }
    if (d.startsWith('Verletzung erlitten:')) {
      return 'Injury sustained:' + d.substring('Verletzung erlitten:'.length);
    }
    if (d.includes('Spieler eliminiert / gestorben')) {
      return 'Player eliminated / died';
    }

    // 8. Refinery & Cargo
    if (d.includes('Material zur Veredelung abgegeben')) {
      return d.replace('Material zur Veredelung abgegeben', 'Work order submitted for refining');
    }
    if (d.startsWith('Frachtaufzug:') && d.includes('angefordert')) {
      return d.replace('Frachtaufzug:', 'Cargo elevator:').replace('angefordert', 'requested');
    }
    if (d.includes('(Kauf)')) {
      return d.replace('(Kauf)', '(Buy)');
    }
    if (d.includes('(Verkauf)')) {
      return d.replace('(Verkauf)', '(Sell)');
    }
    if (d.endsWith('unbrauchbar')) {
      return d.replace(/unbrauchbar$/, 'unusable');
    }
    if (d.includes('Entitlement/Miete gestartet')) {
      return 'Entitlement/Rental started';
    }

    // 9. Party & Social
    if (d.startsWith('▸ ') && d.includes('ist beigetreten')) {
      return d.replace('ist beigetreten', 'joined');
    }
    if (d.startsWith('◂ ') && d.includes('hat verlassen')) {
      return d.replace('hat verlassen', 'left');
    }

    // 10. Finances (Transfers)
    if (d.startsWith('von ') && d.length < 50) {
      return 'from ' + d.substring('von '.length);
    }
    if (d.startsWith('an ') && d.length < 50) {
      return 'to ' + d.substring('an '.length);
    }

    // 11. Refueling & Mining
    if (d.includes('⛽ Betankung abgeschlossen')) {
      return '⛽ Refueling complete';
    }
    if (d.includes('⛽ Betankungsanfrage akzeptiert')) {
      return '⛽ Refueling request accepted';
    }
    if (d.includes('⛽ Andocken an Tanker')) {
      return '⛽ Docking with tanker';
    }
    if (d.includes('⛽ Vom Tanker abgedockt')) {
      return '⛽ Undocked from tanker';
    }
    if (d.includes('⛽ Betankungsvorgang aktiv')) {
      return '⛽ Refueling active';
    }
    if (d.includes('⚡ Bergbau: Bruch-Laser aktiv')) {
      return '⚡ Mining: Fracture laser active';
    }
    if (d.includes('🔍 Bergbau: Scan-Modus aktiv')) {
      return '🔍 Mining: Scan mode active';
    }

    return d;
  }

  // ══ ENGLISH -> GERMAN TRANSLATION (when locale === 'de') ══
  if (d.toLowerCase() === 'objective complete') {
    return 'Missionsziel abgeschlossen';
  }
  if (d.startsWith('Contract Complete:') || d.startsWith('Contract Completed:')) {
    const colonIdx = d.indexOf(':');
    return 'Auftrag erfolgreich abgeschlossen:' + d.substring(colonIdx + 1);
  }
  if (d.toLowerCase() === 'contract complete' || d.toLowerCase() === 'contract completed') {
    return 'Auftrag erfolgreich abgeschlossen';
  }
  if (d.startsWith('Contract Failed:')) {
    return 'Auftrag fehlgeschlagen:' + d.substring('Contract Failed:'.length);
  }
  if (d.toLowerCase() === 'contract failed') {
    return 'Auftrag fehlgeschlagen';
  }
  if (d.startsWith('Contract Abandoned:')) {
    return 'Auftrag abgebrochen:' + d.substring('Contract Abandoned:'.length);
  }
  if (d.toLowerCase() === 'contract abandoned') {
    return 'Auftrag abgebrochen';
  }
  if (d.startsWith('Contract Cancelled:')) {
    return 'Auftrag storniert:' + d.substring('Contract Cancelled:'.length);
  }
  if (d.toLowerCase() === 'contract cancelled') {
    return 'Auftrag storniert';
  }
  if (d.startsWith('Contract Withdrawn:')) {
    return 'Auftrag zurückgezogen:' + d.substring('Contract Withdrawn:'.length);
  }
  if (d.toLowerCase() === 'contract withdrawn') {
    return 'Auftrag zurückgezogen';
  }
  if (d.startsWith('New Objective:')) {
    let rest = d.substring('New Objective:'.length).trim();
    if (rest.startsWith('Deliver Flight Recorder To')) {
      return 'Neues Missionsziel: Flugschreiber abliefern bei ' + rest.substring('Deliver Flight Recorder To'.length).trim();
    }
    if (rest.startsWith('Collect Flight Recorder From a wreck site in the')) {
      return 'Neues Missionsziel: Flugschreiber aus Wrack bergen in ' + rest.substring('Collect Flight Recorder From a wreck site in the'.length).trim();
    }
    return 'Neues Missionsziel: ' + rest;
  }

  // Common armistice strings in game log
  if (d.startsWith('Entering Armistice Zone') || d.startsWith('Entered Armistice Zone')) {
    return '🟢 Schutzzone aktiv (Waffen blockiert)';
  }
  if (d.startsWith('Leaving Armistice Zone') || d.startsWith('Left Armistice Zone')) {
    return '🔴 Schutzzone verlassen (Waffen scharf)';
  }

  return d;
}
