import { Locale } from '../i18n/types';

/**
 * Modular Translation Bundle definition.
 * Any language (e.g. en, de, fr, es, it, zh, etc.) can be added or dynamically registered.
 */
export interface EventTranslationBundle {
  /**
   * Exact description matches (full string match, case-insensitive lookup).
   */
  exact: Record<string, string>;

  /**
   * Prefix substitutions: if description starts with `from`, replaces prefix with `to`.
   */
  prefixes: Array<{ from: string; to: string }>;

  /**
   * Substring substitutions: replaces occurrences of `from` with `to`.
   */
  substrings: Array<{ from: string; to: string }>;

  /**
   * Regular expression pattern substitutions.
   */
  patterns: Array<{
    pattern: RegExp;
    replacement: string | ((match: string, ...groups: any[]) => string);
  }>;
}

/**
 * Multilingual Title & Kind Dictionary.
 * Maps canonical event titles to any supported target language.
 */
const TITLE_DICTIONARY: Record<string, Record<string, string>> = {
  income: { de: 'Eingang', en: 'Income', fr: 'Revenu', es: 'Ingreso' },
  expense: { de: 'Ausgang', en: 'Expense', fr: 'Dépense', es: 'Gasto' },
  reward: { de: 'Belohnung', en: 'Reward', fr: 'Récompense', es: 'Recompensa' },
  purchase: { de: 'Kauf', en: 'Purchase', fr: 'Achat', es: 'Compra' },
  sale: { de: 'Verkauf', en: 'Sale', fr: 'Vente', es: 'Venta' },
  trade: { de: 'Handel', en: 'Trade', fr: 'Commerce', es: 'Comercio' },
  maintenance: { de: 'Wartung', en: 'Maintenance', fr: 'Maintenance', es: 'Mantenimiento' },
  location: { de: 'Standort', en: 'Location', fr: 'Emplacement', es: 'Ubicación' },
  warehouse: { de: 'Lager', en: 'Warehouse', fr: 'Entrepôt', es: 'Almacén' },
  ship: { de: 'Schiff', en: 'Ship', fr: 'Vaisseau', es: 'Nave' },
  quantum: { de: 'Quantum', en: 'Quantum', fr: 'Quantique', es: 'Cuántico' },
  mission: { de: 'Mission', en: 'Mission', fr: 'Mission', es: 'Misión' },
  jurisdiction: { de: 'Gebiet', en: 'Jurisdiction', fr: 'Juridiction', es: 'Jurisdicción' },
  party: { de: 'Party', en: 'Party', fr: 'Groupe', es: 'Grupo' },
  medbed: { de: 'Med-Bett', en: 'Medbed', fr: 'Lit médical', es: 'Cama médica' },
  hangar: { de: 'Hangar', en: 'Hangar', fr: 'Hangar', es: 'Hangar' },
  equipment: { de: 'Ausrüstung', en: 'Equipment', fr: 'Équipement', es: 'Equipamiento' },
  offer: { de: 'Angebot', en: 'Offer', fr: 'Offre', es: 'Oferta' },
  shiploss: { de: 'Verlust', en: 'Ship Loss', fr: 'Perte de vaisseau', es: 'Pérdida de nave' },
  death: { de: 'Tod', en: 'Death', fr: 'Mort', es: 'Muerte' },
  impound: { de: 'Beschlagn.', en: 'Impound', fr: 'Fourrière', es: 'Embargo' },
  friend: { de: 'Freund', en: 'Friend', fr: 'Ami', es: 'Amigo' },
  rental: { de: 'Miete', en: 'Rental', fr: 'Location', es: 'Alquiler' },
  blueprint: { de: 'Bauplan', en: 'Blueprint', fr: 'Plan', es: 'Plano' },
  defect: { de: 'Defekt', en: 'Defect', fr: 'Défaut', es: 'Defecto' },
  combat: { de: 'Kampf', en: 'Combat', fr: 'Combat', es: 'Combate' },
  contract_done: { de: 'Auftrag ✓', en: 'Contract ✓', fr: 'Contrat ✓', es: 'Contrato ✓' },
  fine: { de: 'Strafe', en: 'Fine', fr: 'Amende', es: 'Multa' },
  crime: { de: 'Straftat', en: 'Crime', fr: 'Crime', es: 'Crimen' },
  refining: { de: 'Veredelung', en: 'Refining', fr: 'Raffinage', es: 'Refinado' },
  injury: { de: 'Verletzung', en: 'Injury', fr: 'Blessure', es: 'Lesión' },
  loot: { de: 'Loot', en: 'Loot', fr: 'Butin', es: 'Botín' },
  contractor: { de: 'Auftraggeber', en: 'Contractor', fr: 'Donneur d’ordre', es: 'Contratista' },
  crash: { de: 'Crash', en: 'Crash', fr: 'Crash', es: 'Crash' },
  server: { de: 'Server', en: 'Server', fr: 'Serveur', es: 'Servidor' },
  finances: { de: 'Finanzen', en: 'Finance', fr: 'Finances', es: 'Finanzas' },
  place: { de: 'Ort', en: 'Location', fr: 'Lieu', es: 'Lugar' },
  contract: { de: 'Auftrag', en: 'Mission', fr: 'Mission', es: 'Misión' },
  system: { de: 'System', en: 'System', fr: 'Système', es: 'Sistema' },
  info: { de: 'Info', en: 'Info', fr: 'Info', es: 'Información' },
};

// Fast case-insensitive reverse lookup table: lowercased string -> canonical translations Record<locale, string>
const titleReverseLookup = new Map<string, Record<string, string>>();

function rebuildTitleReverseLookup(): void {
  titleReverseLookup.clear();
  for (const item of Object.values(TITLE_DICTIONARY)) {
    for (const val of Object.values(item)) {
      if (val) {
        titleReverseLookup.set(val.toLowerCase().trim(), item);
      }
    }
  }
}
rebuildTitleReverseLookup();

/**
 * Registers additional title and kind translations into the global multilingual dictionary.
 */
export function registerTitleTranslations(entries: Record<string, Record<string, string>>): void {
  for (const [key, translations] of Object.entries(entries)) {
    if (!TITLE_DICTIONARY[key]) {
      TITLE_DICTIONARY[key] = { ...translations };
    } else {
      TITLE_DICTIONARY[key] = { ...TITLE_DICTIONARY[key], ...translations };
    }
  }
  rebuildTitleReverseLookup();
}

/**
 * English Event Detail Translation Bundle.
 * Translates German and raw StarEngine events into English.
 */
const englishBundle: EventTranslationBundle = {
  exact: {
    'Schutzzone aktiv (Waffen blockiert)': 'Armistice zone active (weapons locked)',
    'Schutzzone - Kampfhandlung untersagt!': 'Armistice zone - Combat prohibited!',
    'Keine Schutzzone (Waffen scharf)': 'Weapons free (no armistice)',
    'Schutzzone verlassen (Waffen scharf)': 'Left armistice zone (weapons armed)',
    'Sperrgebiet verlassen': 'Left restricted area',
    '⛔ Sperrzone: Zwangsumbettung (Relocated)': '⛔ Restricted area: Relocated',
    '🏴 Ungesetzlicher Sektor': '🏴 Unlawful sector',
    '📡 Überwachter Raum (Comm-Array aktiv)': '📡 Monitored space (Comm-Array active)',
    '📡 Unüberwachter Raum (Kein Comm-Array)': '📡 Unmonitored space (No Comm-Array)',
    'Server-Verbindung getrennt / Sitzung beendet (EndSession)': 'Server disconnected / session ended (EndSession)',
    'Server verbunden / Shard erkannt': 'Server connected / shard identified',
    'Verbindung getrennt (30000 Crash)': 'Connection lost (30000 Crash)',
    'Im Spiel gespawnt (Station / Hangar)': 'Spawned in game (Station / Hangar)',
    'Respawn an medizinischer Einrichtung': 'Respawned at medical facility',
    'Hangar-Anforderung bereit / Tor geöffnet': 'Hangar request ready / doors open',
    'In Hangar-Warteschlange eingereiht': 'Queued for hangar assignment',
    'Hangar-Zuweisung erhalten': 'Hangar assignment received',
    'Spieler-Ausrüstung': 'Player Loadout',
    'Missionsziel abgeschlossen': 'Mission objective completed',
    'Auftrag erfolgreich abgeschlossen': 'Contract successfully completed',
    'Auftrag abgeschlossen': 'Contract successfully completed',
    'Auftrag fehlgeschlagen': 'Contract failed',
    'Auftrag abgebrochen': 'Contract abandoned',
    'Auftrag storniert': 'Contract cancelled',
    'Auftrag zurückgezogen': 'Contract withdrawn',
    '☠ Gestorben': '☠ Died',
    'Kampfunfähig': 'Incapacitated',
    'Bitte warten, die lokalen Notfalldienste sind unterwegs': 'Standby, local emergency services are en route',
    'Wiederbelebt / Medizinische Behandlung': 'Resuscitated / Medical treatment',
    'Spieler eliminiert / gestorben': 'Player eliminated / died',
    'Material zur Veredelung abgegeben': 'Work order submitted for refining',
    'Entitlement/Miete gestartet': 'Entitlement/Rental started',
    '⛽ Betankung abgeschlossen': '⛽ Refueling complete',
    '⛽ Betankungsanfrage akzeptiert': '⛽ Refueling request accepted',
    '⛽ Andocken an Tanker': '⛽ Docking with tanker',
    '⛽ Vom Tanker abgedockt': '⛽ Undocked from tanker',
    '⛽ Betankungsvorgang aktiv': '⛽ Refueling active',
    '⚡ Bergbau: Bruch-Laser aktiv': '⚡ Mining: Fracture laser active',
    '🔍 Bergbau: Scan-Modus aktiv': '🔍 Mining: Scan mode active',
  },
  prefixes: [
    { from: '🏛 Rechtsgebiet:', to: '🏛 Jurisdiction:' },
    { from: 'Rechtsgebiet:', to: 'Jurisdiction:' },
    { from: 'Server beigetreten:', to: 'Joined server:' },
    { from: 'Server beigetreten', to: 'Joined server' },
    { from: 'Server verbunden:', to: 'Server connected:' },
    { from: 'Crash erkannt:', to: 'Crash detected:' },
    { from: 'Landefreigabe:', to: 'Landing clearance:' },
    { from: 'Eigenes Schiff betreten:', to: 'Boarded personal ship:' },
    { from: 'Versicherungs-Claim:', to: 'Insurance claim:' },
    { from: 'Strafe gezahlt:', to: 'Fine paid:' },
    { from: '⚔ Verbrechen gegen dich:', to: '⚔ Crime against you:' },
    { from: 'Angebot von ', to: 'Offer from ' },
    { from: 'Zielkoordinate erreicht', to: 'Target coordinate reached' },
    { from: 'Quantum Sprung:', to: 'Quantum Jump:' },
    { from: 'Ankunft bei ', to: 'Arrival at ' },
    { from: 'Missionsziel abgeschlossen:', to: 'Mission objective completed:' },
    { from: 'Auftrag erfolgreich abgeschlossen:', to: 'Contract completed:' },
    { from: 'Auftrag abgeschlossen:', to: 'Contract completed:' },
    { from: 'Auftrag fehlgeschlagen:', to: 'Contract failed:' },
    { from: 'Auftrag abgebrochen:', to: 'Contract abandoned:' },
    { from: 'Auftrag storniert:', to: 'Contract cancelled:' },
    { from: 'Auftrag zurückgezogen:', to: 'Contract withdrawn:' },
    { from: 'Missions-Belohnung:', to: 'Mission Reward:' },
    { from: 'Neuer Auftrag:', to: 'New Contract:' },
    { from: 'Auftrag angenommen:', to: 'Contract accepted:' },
    { from: 'QT-Ankunft ·', to: 'QT Arrival ·' },
    { from: 'QT nach ', to: 'QT to ' },
    { from: '☠ getötet von', to: '☠ Killed by' },
    { from: '☠ Gestorben · ', to: '☠ Died · ' },
    { from: 'Verletzung erlitten:', to: 'Injury sustained:' },
    { from: 'Veredelung fertig (', to: 'Refining complete (' },
    { from: 'von ', to: 'from ' },
    { from: 'an ', to: 'to ' },
  ],
  substrings: [
    { from: 'Server-Verbindung getrennt', to: 'Server disconnected' },
    { from: 'Landung eingeleitet', to: 'Landing initiated' },
    { from: 'Fahrwerk ausgefahren', to: 'Landing gear deployed' },
    { from: 'Fahrwerk eingefahren', to: 'Landing gear retracted' },
    { from: 'Schiff gelandet', to: 'Ship landed' },
    { from: 'Abflug eingeleitet', to: 'Takeoff initiated' },
    { from: '(Sortie beendet / Pilotensitz verlassen)', to: '(Sortie ended / exited pilot seat)' },
    { from: '(Pilotensitz eingenommen)', to: '(Entered pilot seat)' },
    { from: '(Sortie begonnen / Pilotensitz eingenommen)', to: '(Sortie started / entered pilot seat)' },
    { from: 'zerstört / Selbstzerstörung', to: 'destroyed / self-destruct' },
    { from: '– Kollision', to: '– Collision' },
    { from: '⚡ QT-Kalibrierung', to: '⚡ QT calibration' },
    { from: '(Missionsabgabe Frachtaufzug)', to: '(Cargo elevator mission delivery)' },
    { from: 'QT-Sprung initiiert', to: 'QT Jump initiated' },
    { from: 'QT-Sprung beendet', to: 'QT Jump completed' },
    { from: 'QT-Sprung abgebrochen', to: 'QT Jump aborted' },
    { from: 'Feuerlöscher', to: 'Fire Extinguisher' },
    { from: '(Kauf)', to: '(Buy)' },
    { from: '(Verkauf)', to: '(Sell)' },
    { from: 'unbrauchbar', to: 'unusable' },
  ],
  patterns: [
    // 🚀 Ship retrieval notification
    {
      pattern: /🚀 Schiff freigeschaltet:(.*?)\(Abholbar am Ship Kiosk\)/g,
      replacement: '🚀 Ship retrieved:$1(Ready at Ship Kiosk)',
    },
    // ASOP Fleet summary
    {
      pattern: /ASOP Flotte: (\d+) von (\d+) Schiffen einsatzbereit(.*?)/g,
      replacement: (_match: string, a: string, b: string, rest: string) => {
        let r = rest
          .replace('alle bereit', 'all ready')
          .replace('im Claim/Expedite', 'claimed/expedited')
          .replace('im Claim', 'claimed');
        return `ASOP Fleet: ${a} of ${b} ships ready${r}`;
      },
    },
    // Mission category and difficulty indicators
    {
      pattern: / · (Fracht\/Transport|Fracht & Transport|Fracht\/Bergung|Auftrag|k\.A\.|Mittel|Schwer|Sehr Schwer|Leicht|Einfach|Extrem) · /g,
      replacement: (_match: string, token: string) => {
        switch (token) {
          case 'Fracht/Transport':
            return ' · Cargo/Hauling · ';
          case 'Fracht & Transport':
            return ' · Cargo & Hauling · ';
          case 'Fracht/Bergung':
            return ' · Cargo/Salvage · ';
          case 'Auftrag':
            return ' · Contract · ';
          case 'k.A.':
            return ' · N/A · ';
          case 'Mittel':
            return ' · Medium · ';
          case 'Schwer':
            return ' · Hard · ';
          case 'Sehr Schwer':
            return ' · Very Hard · ';
          case 'Leicht':
          case 'Einfach':
            return ' · Easy · ';
          case 'Extrem':
            return ' · Extreme · ';
          default:
            return ` · ${token} · `;
        }
      },
    },
    // Journey and distance
    {
      pattern: /^Reise durch (.+?)-System · Distanz: (.+)$/,
      replacement: 'Journey through $1 system · Distance: $2',
    },
    // Objective / Teilziel
    {
      pattern: /^Teilziel:\s*(.+)$/,
      replacement: (_match: string, sub: string) => {
        let t = sub
          .replace(/^Fracht geliefert/i, 'Cargo delivered')
          .replace(/^Gegner neutralisiert/i, 'Targets neutralized')
          .replace(/^Paket geborgen/i, 'Package recovered')
          .replace(/^Paket abgeliefert/i, 'Package delivered');
        return `Objective: ${t}`;
      },
    },
    // New Objective / Neues Missionsziel
    {
      pattern: /^Neues Missionsziel:\s*(.+)$/,
      replacement: (_match: string, rest: string) => {
        let r = rest
          .replace(/^Flugschreiber abliefern bei/i, 'Deliver Flight Recorder To')
          .replace(/^Flugschreiber aus Wrack bergen in/i, 'Collect Flight Recorder From a wreck site in');
        return `New Objective: ${r}`;
      },
    },
    // Cargo Elevator request
    {
      pattern: /^Frachtaufzug:\s*(.*?)\s*angefordert$/i,
      replacement: 'Cargo elevator: $1 requested',
    },
    // Party membership
    {
      pattern: /^▸ (.*?) ist beigetreten$/,
      replacement: '▸ $1 joined',
    },
    {
      pattern: /^◂ (.*?) hat verlassen$/,
      replacement: '◂ $1 left',
    },
    // Reward with cargo elevator
    {
      pattern: /^🎁 Belohnung:(.*?)\(Frachtaufzug\)$/,
      replacement: '🎁 Reward:$1(Cargo elevator)',
    },
  ],
};

/**
 * German Event Detail Translation Bundle.
 * Translates English StarEngine events into German when locale is 'de'.
 */
const germanBundle: EventTranslationBundle = {
  exact: {
    'objective complete': 'Missionsziel abgeschlossen',
    'contract complete': 'Auftrag erfolgreich abgeschlossen',
    'contract completed': 'Auftrag erfolgreich abgeschlossen',
    'contract failed': 'Auftrag fehlgeschlagen',
    'contract abandoned': 'Auftrag abgebrochen',
    'contract cancelled': 'Auftrag storniert',
    'contract withdrawn': 'Auftrag zurückgezogen',
    'incapacitated': 'Kampfunfähig',
    '☠ died': '☠ Gestorben',
    'player loadout': 'Spieler-Ausrüstung',
  },
  prefixes: [
    { from: 'Contract Complete:', to: 'Auftrag erfolgreich abgeschlossen:' },
    { from: 'Contract Completed:', to: 'Auftrag erfolgreich abgeschlossen:' },
    { from: 'Contract Failed:', to: 'Auftrag fehlgeschlagen:' },
    { from: 'Contract Abandoned:', to: 'Auftrag abgebrochen:' },
    { from: 'Contract Cancelled:', to: 'Auftrag storniert:' },
    { from: 'Contract Withdrawn:', to: 'Auftrag zurückgezogen:' },
    { from: 'Entering Armistice Zone', to: '🟢 Schutzzone aktiv (Waffen blockiert)' },
    { from: 'Entered Armistice Zone', to: '🟢 Schutzzone aktiv (Waffen blockiert)' },
    { from: 'Leaving Armistice Zone', to: '🔴 Schutzzone verlassen (Waffen scharf)' },
    { from: 'Left Armistice Zone', to: '🔴 Schutzzone verlassen (Waffen scharf)' },
    { from: '☠ Died · ', to: '☠ Gestorben · ' },
    { from: 'Boarded personal ship:', to: 'Eigenes Schiff betreten:' },
    { from: 'Entered personal ship:', to: 'Eigenes Schiff betreten:' },
    { from: 'Refining complete (', to: 'Veredelung fertig (' },
  ],
  substrings: [
    { from: 'armistice zone - combat prohibited!', to: 'Schutzzone - Kampfhandlung untersagt!' },
    { from: 'emergency services are en route', to: 'Bitte warten, die lokalen Notfalldienste sind unterwegs' },
  ],
  patterns: [
    {
      pattern: /^New Objective:\s*(.+)$/i,
      replacement: (_match: string, rest: string) => {
        let r = rest.trim();
        if (r.startsWith('Deliver Flight Recorder To')) {
          return 'Neues Missionsziel: Flugschreiber abliefern bei ' + r.substring('Deliver Flight Recorder To'.length).trim();
        }
        if (r.startsWith('Collect Flight Recorder From a wreck site in the')) {
          return 'Neues Missionsziel: Flugschreiber aus Wrack bergen in ' + r.substring('Collect Flight Recorder From a wreck site in the'.length).trim();
        }
        return 'Neues Missionsziel: ' + r;
      },
    },
  ],
};

/**
 * Registry of registered detail translation bundles keyed by target locale.
 * Developers, plugins, or community translators can add new bundles anytime.
 */
const BUNDLE_REGISTRY: Record<string, EventTranslationBundle> = {
  en: englishBundle,
  de: germanBundle,
};

/**
 * Registers or extends an event translation bundle for a target language.
 *
 * @param locale Target locale code (e.g. 'fr', 'es', 'it', 'zh', etc.)
 * @param bundle Partial translation bundle containing exact phrases, prefixes, substrings, and/or patterns.
 */
export function registerEventTranslationBundle(locale: string, bundle: Partial<EventTranslationBundle>): void {
  const normLocale = locale.toLowerCase().trim();
  const existing = BUNDLE_REGISTRY[normLocale] || {
    exact: {},
    prefixes: [],
    substrings: [],
    patterns: [],
  };

  BUNDLE_REGISTRY[normLocale] = {
    exact: { ...existing.exact, ...(bundle.exact || {}) },
    prefixes: [...(bundle.prefixes || []), ...existing.prefixes].sort((a, b) => b.from.length - a.from.length),
    substrings: [...existing.substrings, ...(bundle.substrings || [])],
    patterns: [...existing.patterns, ...(bundle.patterns || [])],
  };

  clearTranslationCache();
}

/**
 * Returns a list of all locales that currently have registered translation bundles.
 */
export function getRegisteredTranslationLocales(): string[] {
  return Object.keys(BUNDLE_REGISTRY);
}

// High-speed memoization cache to keep UI rendering silky smooth (capped at 5,000 entries)
const detailCache = new Map<string, string>();
const MAX_CACHE_SIZE = 5000;

export function clearTranslationCache(): void {
  detailCache.clear();
}

/**
 * Translates event titles / kinds according to the active or specified locale.
 * Supports any language code registered in `TITLE_DICTIONARY` or via `registerTitleTranslations`.
 */
export function translateEventTitle(title?: string | null, locale: Locale | string = 'de'): string {
  if (!title) return '';
  const trimmed = title.trim();
  const target = String(locale).toLowerCase().trim();

  const entry = titleReverseLookup.get(trimmed.toLowerCase());
  if (entry && entry[target]) {
    return entry[target];
  }

  return trimmed;
}

/**
 * Translates event detail descriptions dynamically using the registered language bundle.
 * Fully extensible for any locale (German, English, French, Spanish, etc.).
 */
export function translateEventDetail(text?: string | null, locale: Locale | string = 'de'): string {
  if (!text) return '';
  const trimmed = text.trim();
  const target = String(locale).toLowerCase().trim();

  // Fast path: check memoization cache
  const cacheKey = `${target}::${trimmed}`;
  const cached = detailCache.get(cacheKey);
  if (cached !== undefined) return cached;

  const bundle = BUNDLE_REGISTRY[target];
  if (!bundle) {
    return trimmed;
  }

  let result = trimmed;

  // 1. Exact phrase lookup (O(1))
  const lower = result.toLowerCase();
  if (bundle.exact[result]) {
    result = bundle.exact[result];
  } else if (bundle.exact[lower]) {
    result = bundle.exact[lower];
  } else {
    // 2. Prefix replacements (sorted by descending length)
    for (const { from, to } of bundle.prefixes) {
      if (result.startsWith(from)) {
        result = to + result.substring(from.length);
        break;
      }
    }

    // 3. Substring substitutions
    for (const { from, to } of bundle.substrings) {
      if (result.includes(from)) {
        result = result.replaceAll(from, to);
      }
    }

    // 4. Pattern / Regex rules
    for (const { pattern, replacement } of bundle.patterns) {
      if (pattern.test(result)) {
        // Reset lastIndex for stateful global regexes
        pattern.lastIndex = 0;
        result = result.replace(pattern, replacement as any);
      }
    }
  }

  // Cache management
  if (detailCache.size >= MAX_CACHE_SIZE) {
    detailCache.clear();
  }
  detailCache.set(cacheKey, result);

  return result;
}
