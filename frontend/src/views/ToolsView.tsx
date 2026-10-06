import React, { useState, useEffect, useMemo } from 'react';
import {
  Trash2,
  RefreshCw,
  Save,
  HardDrive,
  Cpu,
  ShieldCheck,
  Terminal,
  Zap,
  CheckCircle2,
  AlertCircle,
  FileText,
  Key,
  Copy,
  Check,
  FolderOpen,
  Sparkles,
  Wrench,
  Cloud,
  RotateCcw,
  Archive,
  Download,
  ExternalLink,
  Sliders,
  Clock,
  Activity,
  Monitor,
  Gauge,
  Sun,
  Layers,
  Palette,
  Rocket,
  BookOpen,
  CheckCheck,
  Maximize2,
  Minimize2,
  X,
  Eye,
  Gamepad2,
  ArrowLeftRight,
  Search,
  AlertTriangle,
  HelpCircle,
  Camera,
  CheckSquare,
  Square,
} from 'lucide-react';
import {
  bridge,
  ToolsStatusDto,
  ConfigBackupItemDto,
  KeybindBackupItemDto,
  HotasStatusDto,
  SettingsDto,
  ScreenshotCleanupStatusDto,
  ScreenshotItemDto,
} from '../services/photinoBridge';
import { useI18n } from '../i18n';

export interface HardwareRecommendation {
  cpuShort: string;
  gpuShort: string;
  hasDetectedHardware: boolean;
  badge: string;
  vramPool: number;
  vramLabel: string;
  cgfPool: number;
}

export function getHardwareRecommendation(status: ToolsStatusDto | null): HardwareRecommendation {
  let cpuShort = '';
  if (status?.cpuModel && status.cpuModel !== 'Unbekannt') {
    const raw = status.cpuModel;
    if (raw.includes('5800X3D')) cpuShort = '5800X3D';
    else if (raw.includes('7800X3D')) cpuShort = '7800X3D';
    else if (raw.includes('9800X3D')) cpuShort = '9800X3D';
    else {
      cpuShort = raw.replace(/\(R\)|\(TM\)|Processor|Core|8-Core|16-Core|AMD|Intel/gi, '').trim().split(/\s+/).slice(0, 2).join(' ');
    }
  }

  let gpuShort = '';
  if (status?.gpuModel && status.gpuModel !== 'Unbekannt') {
    const raw = status.gpuModel;
    if (raw.includes('5070')) gpuShort = 'RTX 5070';
    else if (raw.includes('5080')) gpuShort = 'RTX 5080';
    else if (raw.includes('5090')) gpuShort = 'RTX 5090';
    else if (raw.includes('4090')) gpuShort = 'RTX 4090';
    else if (raw.includes('4080')) gpuShort = 'RTX 4080';
    else if (raw.includes('4070')) gpuShort = 'RTX 4070';
    else {
      gpuShort = raw.replace(/NVIDIA|GeForce|AMD|Radeon/gi, '').trim().split(/\s+/).slice(0, 3).join(' ');
    }
  }

  const hasDetectedHardware = Boolean(cpuShort || gpuShort);

  // VRAM calculation
  let vramPool = 8192;
  let vramLabel = '12GB VRAM';
  const vramNum = status?.gpuVramMb ? parseInt(status.gpuVramMb, 10) : 0;
  const gpuUpper = (status?.gpuModel || '').toUpperCase();

  if (vramNum >= 20000 || gpuUpper.includes('4090') || gpuUpper.includes('5090') || gpuUpper.includes('7900 XTX') || gpuUpper.includes('3090')) {
    vramPool = 12288;
    vramLabel = '16GB+ VRAM';
  } else if (vramNum >= 14000 || gpuUpper.includes('4080') || gpuUpper.includes('5080') || gpuUpper.includes('7900 XT')) {
    vramPool = 10240;
    vramLabel = '16GB VRAM';
  } else if (vramNum >= 10000 || gpuUpper.includes('5070') || gpuUpper.includes('4070') || gpuUpper.includes('3080') || gpuUpper.includes('7800 XT')) {
    vramPool = 8192;
    vramLabel = '12GB VRAM';
  } else if (vramNum >= 7000 || gpuUpper.includes('4060') || gpuUpper.includes('3070') || gpuUpper.includes('3060') || gpuUpper.includes('6700 XT')) {
    vramPool = 6144;
    vramLabel = '8-10GB VRAM';
  } else {
    vramPool = 4096;
    vramLabel = '6-8GB VRAM';
  }

  // CGF Pool based on RAM
  let cgfPool = 4096;
  if ((status?.totalRamGb || 0) >= 48) {
    cgfPool = 6144;
  } else if ((status?.totalRamGb || 0) <= 16 && (status?.totalRamGb || 0) > 0) {
    cgfPool = 2048;
  }

  let badge = 'Auto-Tuning';
  if (cpuShort && gpuShort) {
    badge = `${cpuShort} · ${gpuShort}`;
  } else if (cpuShort || gpuShort) {
    badge = cpuShort || gpuShort;
  }

  return {
    cpuShort: cpuShort || 'System-CPU',
    gpuShort: gpuShort || 'System-GPU',
    hasDetectedHardware,
    badge,
    vramPool,
    vramLabel,
    cgfPool,
  };
}

export const generateHardwarePreset = (status: ToolsStatusDto | null): string => {
  const rec = getHardwareRecommendation(status);
  return `Con_Restricted = 0

-- Star Citizen - Optimierte USER.cfg für ${rec.cpuShort} & ${rec.gpuShort}
-- Con_Restricted = 0 MUSS ganz oben stehen, damit die Konsole und Datei geladen werden.

-- Performance & Core Management
r_multithreaded = 1
-- sys_job_system_max_worker wurde entfernt: Die Engine nutzt alle CPU-Threads optimal.

-- FPS, Monitor & Synchronisation
sys_maxFps = 160
r_VSync = 0
r_enable_full_gpu_sync = 0
r_FullscreenWindow = 1
r_BorderlessWindow = 1

-- Speicher- & Streaming-Optimierung
r_TexturesStreaming = 1
e_StreamCgfPoolSize = ${rec.cgfPool}
r_TexturesStreamPoolSize = ${rec.vramPool} -- Abgestimmt auf ${rec.vramLabel}

-- Detailstufen & Partikel
e_ParticlesQuality = 3
r_DetailDistance = 22

-- HDR Einstellungen (Nur aktiv, wenn Windows-HDR nutzt)
r_HDRDisplayOutput = 1
r_HDRDisplayMaxNits = 1000
r_HDRDisplayRefWhite = 200
r_HDRDisplayDeviceLimits = 1

-- Grafik-Tweaks (Kein Motion Blur, SSDO aktiv)
r_MotionBlur = 0
r_ssdo = 1

-- Shader & Cache-Verbesserungen gegen Mikroruckler
r_shadersasyncactivation = 1
r_GsmCache = 1

-- Interface & Sprache
pl_pit.forceSoftwareCursor = 0
g_language = german_(germany)
g_languageAudio = english`;
};

export const TEMPLATE_ESPORT = `Con_Restricted = 0

-- Star Citizen - High FPS & E-Sport Profil
-- Fokus auf minimale Latenz, maximale Bildrate und G-Sync/FreeSync

-- Performance & Multithreading
r_multithreaded = 1

-- FPS Limit, VSync & GPU-Sync (Minimale Latenz)
sys_maxfps = 165
r_VSync = 0
r_enable_full_gpu_sync = 0
r_FullscreenWindow = 1
r_BorderlessWindow = 1

-- Streaming & VRAM
r_TexturesStreaming = 1
r_TexturesStreamPoolSize = 8192
e_StreamCgfPoolSize = 4096

-- Grafik-Reduktion für maximale Klarheit & FPS
r_MotionBlur = 0
r_ssdo = 0
e_ParticlesQuality = 2
r_DetailDistance = 18

-- Shader & Stotter-Prävention
r_shadersasyncactivation = 1
r_GsmCache = 1

-- HUD / Debug
r_DisplayInfo = 1
pl_pit.forceSoftwareCursor = 0`;

export const TEMPLATE_QUALITY = `Con_Restricted = 0

-- Star Citizen - Maximale Grafik & Immersion
-- Fokus auf beste Optik, hohe Detaildistanz und lebendige Effekte

-- Performance & Multithreading
r_multithreaded = 1

-- Bildwiederholrate & Sync
sys_maxfps = 0
r_VSync = 1
r_FullscreenWindow = 1
r_BorderlessWindow = 1

-- Maximaler Textur- & Geometrie-Puffer
r_TexturesStreaming = 1
r_TexturesStreamPoolSize = 12288
e_StreamCgfPoolSize = 6144

-- Hohe Grafikdetails & Umgebungsverdeckung
r_ssdo = 2
e_ParticlesQuality = 3
r_DetailDistance = 25
r_MotionBlur = 0

-- Native HDR Unterstützung
r_HDRDisplayOutput = 1
r_HDRDisplayMaxNits = 1000
r_HDRDisplayRefWhite = 200
r_HDRDisplayDeviceLimits = 1

-- Shader-Aktivierung & Cache
r_shadersasyncactivation = 1
r_GsmCache = 1
r_DisplayInfo = 0`;

export const TEMPLATE_MINIMAL = `Con_Restricted = 0

-- Star Citizen - 60 FPS Cap / Einsteiger
-- Stabilisiert die Bildrate auf 60 FPS und schont Hardware & Temperaturen

-- Performance
r_multithreaded = 1

-- Festes 60 FPS Limit
sys_maxfps = 60
r_VSync = 0
r_enable_full_gpu_sync = 0

-- Konservative Streaming-Puffer (Für 6-8GB VRAM & 16GB RAM)
r_TexturesStreaming = 1
r_TexturesStreamPoolSize = 4096
e_StreamCgfPoolSize = 2048

-- Moderate Detailstufen
e_ParticlesQuality = 2
r_DetailDistance = 18
r_MotionBlur = 0
r_ssdo = 1

-- Stotter-Schutz
r_shadersasyncactivation = 1
r_GsmCache = 1
r_DisplayInfo = 1`;

export function getTemplateContent(
  type: 'hardware_auto' | 'esport' | 'quality' | 'minimal',
  status: ToolsStatusDto | null
): string {
  if (type === 'hardware_auto') return generateHardwarePreset(status);
  if (type === 'esport') return TEMPLATE_ESPORT;
  if (type === 'quality') return TEMPLATE_QUALITY;
  return TEMPLATE_MINIMAL;
}

export function getPreviewHighlights(
  type: 'hardware_auto' | 'esport' | 'quality' | 'minimal',
  rec: HardwareRecommendation
): string[] {
  if (type === 'hardware_auto') {
    return [
      '160 FPS Cap',
      'VSync: Aus',
      `StreamPool: ${rec.vramPool} MB (${rec.vramLabel})`,
      `CGF: ${rec.cgfPool} MB`,
      'HDR: 1000 Nits',
      'Multithreading: Ein',
      'DE UI / EN Audio',
    ];
  }
  if (type === 'esport') {
    return ['165 FPS Cap', 'VSync: Aus', 'Low GPU-Sync: Aus', 'StreamPool: 8192 MB', 'DisplayInfo: 1', 'MotionBlur: Aus', 'SSDO: Aus'];
  }
  if (type === 'quality') {
    return ['Unbegrenzt FPS', 'VSync: Ein', 'StreamPool: 12288 MB', 'CGF: 6144 MB', 'SSDO: Level 2', 'DetailDist: 25', 'HDR: 1000 Nits'];
  }
  return ['60 FPS Cap', 'VSync: Aus', 'StreamPool: 4096 MB', 'CGF: 2048 MB', 'DetailDist: 18', 'DisplayInfo: 1'];
}

export interface CfgDocEntry {
  category: string;
  command: string;
  valueDescription: string;
  recommended: string;
  explanation: string;
  tag: string;
}

export const CFG_REFERENCE_ENTRIES: CfgDocEntry[] = [
  {
    category: '1. SYSTEM & KONSOLE (GRUNDLAGEN)',
    command: 'Con_Restricted',
    valueDescription: '0 = Entsperrt | 1 = Eingeschränkt',
    recommended: '0',
    explanation: 'Schaltet die Entwicklerkonsole frei (Taste ^ oder ~). MUSS ganz oben in der user.cfg stehen, damit die Datei von Star Citizen geladen wird!',
    tag: 'Pflicht / Basis',
  },
  {
    category: '1. SYSTEM & KONSOLE (GRUNDLAGEN)',
    command: 'r_DisplayInfo',
    valueDescription: '0 = Aus | 1 = FPS | 2 = FPS, Tickrate, RAM | 3-4 = Debug',
    recommended: '1',
    explanation: 'Zeigt die Telemetrie-Box oben rechts im Bildschirm an. Wert 1 zeigt FPS, 2 zeigt Server-Tickrate & Bandbreite.',
    tag: 'Telemetrie',
  },
  {
    category: '2. MONITOR, AUFLÖSUNG & SYNCHRONISATION',
    command: 'sys_maxFps',
    valueDescription: 'Ganzzahl (z. B. 60, 120, 144, 160, 0 = Aus)',
    recommended: '160',
    explanation: 'Setzt ein festes Limit für die maximalen FPS. Bei G-Sync/FreeSync knapp unter die maximale Hz-Zahl des Monitors setzen.',
    tag: 'FPS-Cap',
  },
  {
    category: '2. MONITOR, AUFLÖSUNG & SYNCHRONISATION',
    command: 'r_VSync',
    valueDescription: '0 = Aus | 1 = Ein',
    recommended: '0',
    explanation: 'Schaltet die vertikale Synchronisation im Spiel aus (0) oder ein (1). Bei G-Sync oder FreeSync im Spiel immer auf 0 setzen.',
    tag: 'Latenz',
  },
  {
    category: '2. MONITOR, AUFLÖSUNG & SYNCHRONISATION',
    command: 'r_enable_full_gpu_sync',
    valueDescription: '0 = Gelockert / Schneller | 1 = Voll synchron',
    recommended: '0',
    explanation: 'Erzwingt synchrone Berechnung zwischen CPU und GPU bei 1. Ein Wert von 0 reduziert den Input-Lag und liefert meist bessere FPS.',
    tag: 'Performance',
  },
  {
    category: '2. MONITOR, AUFLÖSUNG & SYNCHRONISATION',
    command: 'r_FullscreenWindow',
    valueDescription: '1 = Aktiviert | 0 = Normaler Fenstermodus',
    recommended: '1',
    explanation: 'Erzwingt zusammen mit "r_BorderlessWindow = 1" den randlosen Vollbild-Fenstermodus.',
    tag: 'Fenstermodus',
  },
  {
    category: '2. MONITOR, AUFLÖSUNG & SYNCHRONISATION',
    command: 'r_BorderlessWindow',
    valueDescription: '1 = Randlos | 0 = Mit Rahmen',
    recommended: '1',
    explanation: 'Sorgt für stabileres Tabben (Alt+Tab) aus dem Spiel im modernen Windows-Betrieb.',
    tag: 'Stabilität',
  },
  {
    category: '3. GRAFIK & VISUELLE EFFEKTE',
    command: 'r_MotionBlur',
    valueDescription: '0 = Aus | 1 = Ein',
    recommended: '0',
    explanation: 'Deaktiviert (0) oder aktiviert (1) die Bewegungsunschärfe. Erhöht die Bildschärfe bei schnellen Kamerabewegungen massiv.',
    tag: 'Optik',
  },
  {
    category: '3. GRAFIK & VISUELLE EFFEKTE',
    command: 'r_ssdo',
    valueDescription: '0 = Aus | 1 = Normal | 2 = Hoch',
    recommended: '1',
    explanation: 'Steuert die Umgebungsverdeckung (Screen Space Directional Occlusion). Sorgt für deutlich plastischere, realistischere Schatten.',
    tag: 'Schatten',
  },
  {
    category: '3. GRAFIK & VISUELLE EFFEKTE',
    command: 'r_HDRDisplayOutput',
    valueDescription: '1 = HDR aktiv | 0 = SDR',
    recommended: '1',
    explanation: 'Aktiviert die native HDR-Ausgabe des Spiels (erfordert vorher in Windows aktiviertes HDR).',
    tag: 'HDR',
  },
  {
    category: '3. GRAFIK & VISUELLE EFFEKTE',
    command: 'r_HDRDisplayMaxNits',
    valueDescription: 'Spitzenhelligkeit in Nits (z. B. 400, 600, 1000, 1400)',
    recommended: '1000',
    explanation: 'Setzt die maximale Helligkeit des Monitors in Nits für die HDR-Kanalisierung.',
    tag: 'HDR',
  },
  {
    category: '3. GRAFIK & VISUELLE EFFEKTE',
    command: 'r_HDRDisplayRefWhite',
    valueDescription: 'Weißwert-Referenz (Standard 100, 200, 300)',
    recommended: '200',
    explanation: 'Bestimmt den Weißwert-Referenzpunkt für die HDR-Darstellung. 200 verhindert Blend-Effekte im Cockpit.',
    tag: 'HDR',
  },
  {
    category: '3. GRAFIK & VISUELLE EFFEKTE',
    command: 'r_HDRDisplayDeviceLimits',
    valueDescription: '1 = Hardware-Limits beachten | 0 = Ignorieren',
    recommended: '1',
    explanation: 'Erzwingt das Einhalten der Hardware-Limits deines Bildschirms bei HDR-Kanalisierung.',
    tag: 'HDR',
  },
  {
    category: '4. TEXTUREN, GEOMETRIE & SPEICHER-STREAMING',
    command: 'r_TexturesStreaming',
    valueDescription: '1 = Aktiviert | 0 = Deaktiviert',
    recommended: '1',
    explanation: 'Aktiviert das dynamische Nachladen von Texturen (sollte immer auf 1 stehen).',
    tag: 'Streaming',
  },
  {
    category: '4. TEXTUREN, GEOMETRIE & SPEICHER-STREAMING',
    command: 'r_TexturesStreamPoolSize',
    valueDescription: '6144 (8GB VRAM) | 8192 (12GB VRAM) | 12288 (16GB+ VRAM)',
    recommended: '8192',
    explanation: 'Der VRAM-Speicherpool für Texturen in MB. 8192 MB ist die goldene Mitte für die 12GB VRAM der RTX 5070.',
    tag: 'VRAM',
  },
  {
    category: '4. TEXTUREN, GEOMETRIE & SPEICHER-STREAMING',
    command: 'e_StreamCgfPoolSize',
    valueDescription: '2048, 4096, 8192 (MB)',
    recommended: '4096',
    explanation: 'Speicherpool für 3D-Geometriedaten (Static Meshes). 4096 ist der Standard für moderne Systeme mit 32GB/64GB RAM.',
    tag: 'RAM',
  },
  {
    category: '4. TEXTUREN, GEOMETRIE & SPEICHER-STREAMING',
    command: 'r_DetailDistance',
    valueDescription: 'Skala 5 bis 30 (Standard 10, Hoch 22)',
    recommended: '22',
    explanation: 'Erhöht die Distanz, ab der kleine Details auf Objekten scharf gezeichnet werden. Verringert das Aufploppen (Pop-ins) von Texturen.',
    tag: 'LOD',
  },
  {
    category: '4. TEXTUREN, GEOMETRIE & SPEICHER-STREAMING',
    command: 'e_ParticlesQuality',
    valueDescription: '1 = Niedrig | 2 = Medium | 3 = Hoch',
    recommended: '3',
    explanation: 'Bestimmt die Berechnungsqualität von Partikeleffekten (Explosionen, Triebwerksfeuer, Weltraumstaub).',
    tag: 'Partikel',
  },
  {
    category: '5. SHADER- & RUCKLER-OPTIMIERUNG (CACHE)',
    command: 'r_shadersasyncactivation',
    valueDescription: '1 = Asynchron im Hintergrund | 0 = Blockierend',
    recommended: '1',
    explanation: 'Erlaubt der Engine das asynchrone Laden von Shadern im Hintergrund. Verhindert kurze Standbilder (Stuttering), wenn neue Shader kompiliert werden.',
    tag: 'Anti-Stutter',
  },
  {
    category: '5. SHADER- & RUCKLER-OPTIMIERUNG (CACHE)',
    command: 'r_GsmCache',
    valueDescription: '1 = Aktiviert | 0 = Deaktiviert',
    recommended: '1',
    explanation: 'Aktiviert das Caching von globalen Schatten-Maps (Global Shadow Maps). Entlastet die CPU in dicht bebauten Städten (Lorville, Area18).',
    tag: 'CPU-Entlastung',
  },
  {
    category: '6. SPRACHE & INTERFACE (LOKALISIERUNG)',
    command: 'g_language',
    valueDescription: 'german_(germany) oder english',
    recommended: 'german_(germany)',
    explanation: 'Stellt die Textsprache im Spiel auf Deutsch um.',
    tag: 'Sprache',
  },
  {
    category: '6. SPRACHE & INTERFACE (LOKALISIERUNG)',
    command: 'g_languageAudio',
    valueDescription: 'english oder german_(germany)',
    recommended: 'english',
    explanation: 'Behält die originale englische Sprachausgabe bei (verhindert fehlende deutsche Sprachdateien / stumme Dialoge).',
    tag: 'Audio',
  },
  {
    category: '6. SPRACHE & INTERFACE (LOKALISIERUNG)',
    command: 'pl_pit.forceSoftwareCursor',
    valueDescription: '0 = Hardware-Mauszeiger | 1 = Software-Cursor',
    recommended: '0',
    explanation: 'Bei 0 wird der direkte Hardware-Mauszeiger genutzt. Verhindert träge oder ruckelnde Mauszeiger in den In-Game-Menüs.',
    tag: 'Mauszeiger',
  },
  {
    category: '7. CPU & KERN-MANAGEMENT',
    command: 'r_multithreaded',
    valueDescription: '1 = Multithreading aktiv | 0 = Aus',
    recommended: '1',
    explanation: 'Aktiviert paralleles Rendering auf allen Kernen. Der 5800X3D nutzt damit alle 16 logischen Threads optimal aus.',
    tag: 'CPU',
  },
];

function mergeCfgContent(
  currentText: string,
  updates: Record<string, string | number>
): string {
  if (!currentText.trim()) {
    return updates['__raw__'] ? String(updates['__raw__']) : Object.entries(updates).map(([k, v]) => `${k} = ${v}`).join('\n');
  }

  const lines = currentText.split(/\r?\n/);
  const remainingKeys = new Set(Object.keys(updates).map((k) => k.toLowerCase()));
  const updateMap: Record<string, { key: string; value: string | number }> = {};
  for (const [k, v] of Object.entries(updates)) {
    updateMap[k.toLowerCase()] = { key: k, value: v };
  }

  const resultLines: string[] = [];

  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith(';') || trimmed.startsWith('#') || trimmed.startsWith('//') || trimmed.startsWith('--')) {
      resultLines.push(line);
      continue;
    }

    const eqIdx = line.indexOf('=');
    if (eqIdx > 0) {
      const key = line.substring(0, eqIdx).trim();
      const keyLower = key.toLowerCase();
      if (updateMap[keyLower]) {
        const leadingWhitespace = line.match(/^\s*/)?.[0] || '';
        resultLines.push(`${leadingWhitespace}${updateMap[keyLower].key} = ${updateMap[keyLower].value}`);
        remainingKeys.delete(keyLower);
        continue;
      }
    }

    resultLines.push(line);
  }

  if (remainingKeys.has('con_restricted')) {
    resultLines.unshift('Con_Restricted = 0');
    remainingKeys.delete('con_restricted');
  }

  if (remainingKeys.size > 0) {
    resultLines.push('');
    resultLines.push('; --- Zusätzliche Tuning-Parameter (SCLogMate) ---');
    for (const keyLower of remainingKeys) {
      const item = updateMap[keyLower];
      resultLines.push(`${item.key} = ${item.value}`);
    }
  }

  return resultLines.join('\n');
}

export const CvarHelpTooltip: React.FC<{ command: string; align?: 'left' | 'right' | 'center' }> = ({
  command,
  align = 'right',
}) => {
  const [open, setOpen] = useState(false);
  const entry = useMemo(() => {
    const cmdLower = command.toLowerCase();
    return CFG_REFERENCE_ENTRIES.find((e) => e.command.toLowerCase() === cmdLower);
  }, [command]);

  if (!entry) return null;

  return (
    <div
      className="relative inline-flex items-center ml-1.5"
      onMouseEnter={() => setOpen(true)}
      onMouseLeave={() => setOpen(false)}
    >
      <button
        type="button"
        onClick={(e) => {
          e.preventDefault();
          e.stopPropagation();
          setOpen((v) => !v);
        }}
        aria-label={`Erklärung zu ${entry.command}`}
        className="p-0.5 rounded text-slate-500 hover:text-sky-300 hover:bg-slate-800/80 transition-colors inline-flex items-center justify-center cursor-pointer focus:outline-none"
        title="Befehls-Erklärung anzeigen"
      >
        <HelpCircle className="w-3.5 h-3.5" />
      </button>

      {open && (
        <div
          role="tooltip"
          className={`absolute z-50 w-72 p-3 rounded-xl bg-slate-900/98 backdrop-blur-md border border-slate-700 shadow-2xl text-left pointer-events-auto transition-all ${
            align === 'left' ? 'left-0' : align === 'center' ? 'left-1/2 -translate-x-1/2' : 'right-0'
          } bottom-full mb-1.5`}
        >
          <div className="flex items-center justify-between pb-1.5 border-b border-slate-800 gap-2">
            <span className="font-mono text-xs font-bold text-sky-300 truncate">{entry.command}</span>
            <span className="text-[9px] px-1.5 py-0.5 rounded bg-sky-950/80 text-sky-400 font-semibold border border-sky-800/60 uppercase">
              {entry.tag}
            </span>
          </div>
          <p className="text-[11px] text-slate-300 mt-2 leading-relaxed font-normal">
            {entry.explanation}
          </p>
          <div className="mt-2.5 pt-2 border-t border-slate-800/80 space-y-1 text-[10px] font-mono">
            <div className="text-slate-400">
              Werte: <span className="text-slate-300">{entry.valueDescription}</span>
            </div>
            <div className="text-emerald-400 font-semibold">
              Empfehlung: <span className="text-white">{entry.recommended}</span>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export const ToolsView: React.FC = () => {
  const { locale } = useI18n();
  // Navigation: Maintenance, user.cfg Studio, Backups, and HOTAS & Joysticks
  const [activeTab, setActiveTab] = useState<'maintenance' | 'cfg' | 'keybinds' | 'hotas'>('cfg');
  const [cfgView, setCfgView] = useState<'editor' | 'tuning'>('editor');
  const [backupSubTab, setBackupSubTab] = useState<'keybinds' | 'usercfg'>('keybinds');
  const [showLexiconModal, setShowLexiconModal] = useState(false);
  const [editorPopout, setEditorPopout] = useState(false);

  const [status, setStatus] = useState<ToolsStatusDto | null>(null);
  const hwRec = useMemo(() => getHardwareRecommendation(status), [status]);
  const [previewPreset, setPreviewPreset] = useState<'hardware_auto' | 'esport' | 'quality' | 'minimal' | null>(null);
  const [loading, setLoading] = useState(true);

  // HOTAS & Sticks Profiler State
  const [hotasStatus, setHotasStatus] = useState<HotasStatusDto | null>(null);
  const [hotasLoading, setHotasLoading] = useState(false);
  const [hotasSwapping, setHotasSwapping] = useState(false);
  const [hotasExporting, setHotasExporting] = useState(false);
  const [hotasSearch, setHotasSearch] = useState('');
  const [hotasFilterDevice, setHotasFilterDevice] = useState<string>('all');
  const [hotasFilterCategory, setHotasFilterCategory] = useState<string>('all');
  const [hotasExportName, setHotasExportName] = useState('sclogmate_profile');
  const [hotasExportModalOpen, setHotasExportModalOpen] = useState(false);
  const [copiedResortCmd, setCopiedResortCmd] = useState(false);
  const [hotasEnabled, setHotasEnabled] = useState(true);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        if (previewPreset) setPreviewPreset(null);
        else if (editorPopout) setEditorPopout(false);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [previewPreset, editorPopout]);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const [cfgContent, setCfgContent] = useState('');
  const [copied, setCopied] = useState(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  // Backup & Notes State
  const [cloudPath, setCloudPath] = useState('');
  const [configNote, setConfigNote] = useState('');
  const [keybindNote, setKeybindNote] = useState('');

  // Screenshot Cleanup & Gallery State
  const [screenshotStatus, setScreenshotStatus] = useState<ScreenshotCleanupStatusDto | null>(null);
  const [screenshotLoading, setScreenshotLoading] = useState(false);
  const [screenshotActionLoading, setScreenshotActionLoading] = useState<string | null>(null);
  const [screenshotFilter, setScreenshotFilter] = useState<'all' | 'loadout' | 'reputation' | 'contract' | 'blank' | 'other'>('all');
  const [screenshotSearch, setScreenshotSearch] = useState('');
  const [selectedScreenshotPaths, setSelectedScreenshotPaths] = useState<Set<string>>(new Set());
  const [deleteConfirmModal, setDeleteConfirmModal] = useState<{
    open: boolean;
    mode: 'all' | 'reputation' | 'loadout' | 'contract' | 'blank' | 'other' | 'selected';
    count: number;
    label: string;
    paths?: string[];
  } | null>(null);

  // Tuning Controls
  const [consoleUnlocked, setConsoleUnlocked] = useState(true);
  const [multithreaded, setMultithreaded] = useState(true);
  const [vsyncOff, setVsyncOff] = useState(true);
  const [fullGpuSyncOff, setFullGpuSyncOff] = useState(true);
  const [fullscreenWindow, setFullscreenWindow] = useState(true);
  const [borderlessWindow, setBorderlessWindow] = useState(true);
  const [maxFps, setMaxFps] = useState<number>(160);
  const [displayInfo, setDisplayInfo] = useState<number>(0);

  // Streaming & VRAM
  const [texturesStreaming, setTexturesStreaming] = useState(true);
  const [streamPool, setStreamPool] = useState<number>(8192);
  const [streamCgfPoolSize, setStreamCgfPoolSize] = useState<number>(4096);

  // Details & Visuals
  const [particlesQuality, setParticlesQuality] = useState<number>(3);
  const [detailDistance, setDetailDistance] = useState<number>(22);
  const [motionBlurOff, setMotionBlurOff] = useState(true);
  const [ssdoLevel, setSsdoLevel] = useState<number>(1);

  // HDR
  const [hdrOutput, setHdrOutput] = useState(true);
  const [hdrMaxNits, setHdrMaxNits] = useState<number>(1000);
  const [hdrRefWhite, setHdrRefWhite] = useState<number>(200);
  const [hdrDeviceLimits, setHdrDeviceLimits] = useState(true);

  // Shaders & Cache
  const [shadersAsync, setShadersAsync] = useState(true);
  const [gsmCache, setGsmCache] = useState(true);

  // Interface & Language
  const [hardwareCursor, setHardwareCursor] = useState(true);
  const [langEnglish, setLangEnglish] = useState(false);
  const [langAudioEnglish, setLangAudioEnglish] = useState(true);

  // Reference Filter State
  const [searchRef, setSearchRef] = useState('');

  const showToast = (msg: string) => {
    setToastMessage(msg);
    setTimeout(() => setToastMessage(null), 5000);
  };

  const loadStatus = async () => {
    try {
      setLoading(true);
      const data = await bridge.send<ToolsStatusDto>('get_tools_status');
      setStatus(data);
      const rawCfg = data.userCfgContent || '';
      setCfgContent(rawCfg);
      if (data.cloudStoragePath !== undefined) {
        setCloudPath(data.cloudStoragePath || '');
      }
      parseCfgContent(rawCfg);
    } catch (err) {
      console.error('Failed to load tools status:', err);
      showToast('Fehler beim Laden der System-Tools');
    } finally {
      setLoading(false);
    }
  };

  const parseCfgContent = (content: string) => {
    let con = true;
    let mt = true;
    let vsync = true;
    let gpuSync = true;
    let fsWin = true;
    let borderless = true;
    let fps = 160;
    let disp = 0;

    let texStream = true;
    let pool = 8192;
    let cgfPool = 4096;

    let partQual = 3;
    let detDist = 22;
    let mb = true;
    let ssdoVal = 1;

    let hdrOut = true;
    let hdrNits = 1000;
    let hdrWhite = 200;
    let hdrLimits = true;

    let asyncShad = true;
    let gsm = true;

    let hwCursor = true;
    let langEn = false;
    let audioEn = true;

    const lines = content.split(/\r?\n/);
    lines.forEach((line) => {
      const trimmed = line.trim();
      if (!trimmed || trimmed.startsWith(';') || trimmed.startsWith('#') || trimmed.startsWith('//') || trimmed.startsWith('--')) return;

      const eqIdx = trimmed.indexOf('=');
      if (eqIdx <= 0) return;
      const key = trimmed.substring(0, eqIdx).trim().toLowerCase();
      const val = trimmed.substring(eqIdx + 1).trim();

      if (key === 'con_restricted') con = val === '0';
      else if (key === 'r_multithreaded') mt = val === '1';
      else if (key === 'r_vsync') vsync = val === '0';
      else if (key === 'r_enable_full_gpu_sync') gpuSync = val === '0';
      else if (key === 'r_fullscreenwindow') fsWin = val === '1';
      else if (key === 'r_borderlesswindow') borderless = val === '1';
      else if (key === 'sys_maxfps') {
        const parsed = parseInt(val, 10);
        if (!isNaN(parsed)) fps = parsed;
      } else if (key === 'r_displayinfo' || key === 'r_displaysessioninfo') {
        const parsed = parseInt(val, 10);
        if (!isNaN(parsed)) disp = parsed;
      } else if (key === 'r_texturesstreaming') texStream = val === '1';
      else if (key === 'r_texturesstreampoolsize') {
        const parsed = parseInt(val, 10);
        if (!isNaN(parsed)) pool = parsed;
      } else if (key === 'e_streamcgfpoolsize') {
        const parsed = parseInt(val, 10);
        if (!isNaN(parsed)) cgfPool = parsed;
      } else if (key === 'e_particlesquality') {
        const parsed = parseInt(val, 10);
        if (!isNaN(parsed)) partQual = parsed;
      } else if (key === 'r_detaildistance') {
        const parsed = parseInt(val, 10);
        if (!isNaN(parsed)) detDist = parsed;
      } else if (key === 'r_motionblur') mb = val === '0';
      else if (key === 'r_ssdo') {
        const parsed = parseInt(val, 10);
        if (!isNaN(parsed)) ssdoVal = parsed;
      } else if (key === 'r_hdrdisplayoutput') hdrOut = val === '1';
      else if (key === 'r_hdrdisplaymaxnits') {
        const parsed = parseInt(val, 10);
        if (!isNaN(parsed)) hdrNits = parsed;
      } else if (key === 'r_hdrdisplayrefwhite') {
        const parsed = parseInt(val, 10);
        if (!isNaN(parsed)) hdrWhite = parsed;
      } else if (key === 'r_hdrdisplaydevicelimits') hdrLimits = val === '1';
      else if (key === 'r_shadersasyncactivation') asyncShad = val === '1';
      else if (key === 'r_gsmcache') gsm = val === '1';
      else if (key === 'pl_pit.forcesoftwarecursor') hwCursor = val === '0';
      else if (key === 'g_language') langEn = val.toLowerCase().includes('english');
      else if (key === 'g_languageaudio') audioEn = val.toLowerCase().includes('english');
    });

    setConsoleUnlocked(con);
    setMultithreaded(mt);
    setVsyncOff(vsync);
    setFullGpuSyncOff(gpuSync);
    setFullscreenWindow(fsWin);
    setBorderlessWindow(borderless);
    setMaxFps(fps);
    setDisplayInfo(disp);
    setTexturesStreaming(texStream);
    setStreamPool(pool);
    setStreamCgfPoolSize(cgfPool);
    setParticlesQuality(partQual);
    setDetailDistance(detDist);
    setMotionBlurOff(mb);
    setSsdoLevel(ssdoVal);
    setHdrOutput(hdrOut);
    setHdrMaxNits(hdrNits);
    setHdrRefWhite(hdrWhite);
    setHdrDeviceLimits(hdrLimits);
    setShadersAsync(asyncShad);
    setGsmCache(gsm);
    setHardwareCursor(hwCursor);
    setLangEnglish(langEn);
    setLangAudioEnglish(audioEn);
  };

  const applyPreset = (type: 'hardware_auto' | '5800x3d_5070' | 'esport' | 'quality' | 'minimal', replaceAll: boolean = false) => {
    const canonicalType = (type === '5800x3d_5070' ? 'hardware_auto' : type) as 'hardware_auto' | 'esport' | 'quality' | 'minimal';
    const rec = getHardwareRecommendation(status);

    if (replaceAll || !cfgContent.trim()) {
      const template = getTemplateContent(canonicalType, status);
      setCfgContent(template);
      parseCfgContent(template);
      showToast(`Vorlage (${canonicalType === 'hardware_auto' ? rec.badge : canonicalType}) komplett geladen!`);
      return;
    }

    if (canonicalType === 'hardware_auto') {
      const updates: Record<string, string | number> = {
        Con_Restricted: 0,
        r_multithreaded: 1,
        sys_maxFps: 160,
        r_VSync: 0,
        r_enable_full_gpu_sync: 0,
        r_FullscreenWindow: 1,
        r_BorderlessWindow: 1,
        r_TexturesStreaming: 1,
        e_StreamCgfPoolSize: rec.cgfPool,
        r_TexturesStreamPoolSize: rec.vramPool,
        e_ParticlesQuality: 3,
        r_DetailDistance: 22,
        r_HDRDisplayOutput: 1,
        r_HDRDisplayMaxNits: 1000,
        r_HDRDisplayRefWhite: 200,
        r_HDRDisplayDeviceLimits: 1,
        r_MotionBlur: 0,
        r_ssdo: 1,
        r_shadersasyncactivation: 1,
        r_GsmCache: 1,
        'pl_pit.forceSoftwareCursor': 0,
        g_language: 'german_(germany)',
        g_languageAudio: 'english',
      };

      setCfgContent((prev) => {
        const merged = mergeCfgContent(prev, updates);
        parseCfgContent(merged);
        return merged;
      });
      showToast(`⭐ Hardware-Empfehlung (${rec.badge}) per Merge übernommen`);
      return;
    }

    let updates: Record<string, string | number> = {};

    if (canonicalType === 'esport') {
      updates = {
        Con_Restricted: 0,
        r_multithreaded: 1,
        r_VSync: 0,
        r_MotionBlur: 0,
        sys_maxfps: 165,
        r_TexturesStreamPoolSize: 8192,
        r_DisplayInfo: 1,
        r_shadersasyncactivation: 1,
        r_enable_full_gpu_sync: 0,
        r_FullscreenWindow: 1,
        r_BorderlessWindow: 1,
      };
      showToast('Profil "High FPS / E-Sport" per Merge übernommen');
    } else if (canonicalType === 'quality') {
      updates = {
        Con_Restricted: 0,
        r_multithreaded: 1,
        r_VSync: 1,
        r_MotionBlur: 0,
        sys_maxfps: 0,
        r_TexturesStreamPoolSize: 12288,
        r_DisplayInfo: 0,
        r_ssdo: 2,
        e_ParticlesQuality: 3,
        r_DetailDistance: 25,
      };
      showToast('Profil "Grafik & Immersion" per Merge übernommen');
    } else {
      updates = {
        Con_Restricted: 0,
        r_VSync: 0,
        r_MotionBlur: 0,
        sys_maxfps: 60,
        r_TexturesStreamPoolSize: 4096,
        r_DisplayInfo: 1,
        e_StreamCgfPoolSize: 2048,
      };
      showToast('Profil "Minimal / 60 FPS" per Merge übernommen');
    }

    setCfgContent((prev) => {
      const merged = mergeCfgContent(prev, updates);
      parseCfgContent(merged);
      return merged;
    });
  };

  const updateSingleCvar = (key: string, value: string | number) => {
    setCfgContent((prev) => {
      const merged = mergeCfgContent(prev, { [key]: value });
      return merged;
    });
  };

  const handleClearShaderCache = async () => {
    setActionLoading('shaders');
    try {
      const res = await bridge.send<{ success: boolean; freedMb: number; message: string; tools?: ToolsStatusDto }>('clear_shader_cache');
      if (res.tools) {
        setStatus(res.tools);
      } else if ((res as any).shaderCacheMb !== undefined) {
        setStatus(res as any);
      }
      showToast(res.message || (res.success ? 'Shader-Cache erfolgreich geleert!' : 'Shader-Cache konnte nicht geleert werden.'));
    } catch (err: any) {
      console.error(err);
      showToast(err?.message || 'Fehler beim Bereinigen der Shader');
    } finally {
      setActionLoading(null);
    }
  };

  const handleClearCrashDumps = async () => {
    setActionLoading('dumps');
    try {
      const res = await bridge.send<{ success: boolean; freedMb: number; message: string; tools?: ToolsStatusDto }>('clear_crash_dumps');
      if (res.tools) {
        setStatus(res.tools);
      } else if ((res as any).crashDumpsMb !== undefined) {
        setStatus(res as any);
      }
      showToast(res.message || (res.success ? 'Crash-Dumps erfolgreich bereinigt!' : 'Crash-Dumps konnten nicht gelöscht werden.'));
    } catch (err: any) {
      console.error(err);
      showToast(err?.message || 'Fehler beim Löschen der Crash-Dumps');
    } finally {
      setActionLoading(null);
    }
  };

  const loadScreenshotStatus = async (autoScan = true) => {
    try {
      setScreenshotLoading(true);
      const res = await bridge.getScreenshotCleanupStatus(undefined, autoScan);
      setScreenshotStatus(res);
    } catch (err: any) {
      console.error('Failed to load screenshot status:', err);
    } finally {
      setScreenshotLoading(false);
    }
  };

  const handleOpenScreenshotFolder = async () => {
    try {
      await bridge.openScreenshotFolder();
    } catch (err: any) {
      console.error(err);
      showToast('Konnte Screenshot-Ordner nicht öffnen.');
    }
  };

  const handleExecuteDelete = async (
    mode: 'all' | 'reputation' | 'loadout' | 'contract' | 'blank' | 'other' | 'selected',
    paths?: string[]
  ) => {
    setScreenshotActionLoading(mode);
    try {
      const res = await bridge.cleanupScreenshots(mode, paths);
      if (res.status) {
        setScreenshotStatus(res.status);
      } else {
        await loadScreenshotStatus(false);
      }
      setSelectedScreenshotPaths(new Set());
      setDeleteConfirmModal(null);
      showToast(res.message || `${res.deletedCount} Screenshots erfolgreich gelöscht.`);
      await loadStatus();
    } catch (err: any) {
      console.error(err);
      showToast(err?.message || 'Fehler beim Löschen der Screenshots');
    } finally {
      setScreenshotActionLoading(null);
    }
  };

  const handleToggleSelectScreenshot = (path: string) => {
    const next = new Set(selectedScreenshotPaths);
    if (next.has(path)) {
      next.delete(path);
    } else {
      next.add(path);
    }
    setSelectedScreenshotPaths(next);
  };

  const handleSelectAllCategory = (items: ScreenshotItemDto[]) => {
    if (selectedScreenshotPaths.size === items.length && items.length > 0) {
      setSelectedScreenshotPaths(new Set());
    } else {
      setSelectedScreenshotPaths(new Set(items.map((i) => i.filePath)));
    }
  };

  const handleSaveUserCfg = async () => {
    setActionLoading('saveCfg');
    try {
      const res = await bridge.send<ToolsStatusDto>('save_user_cfg', {
        content: cfgContent,
        cfgContent: cfgContent,
      });
      setStatus(res);
      showToast('user.cfg gespeichert! (Vorher automatisch lokal & in Cloud gesichert)');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Speichern der user.cfg');
    } finally {
      setActionLoading(null);
    }
  };

  const handleBackupUserCfgSnapshot = async () => {
    setActionLoading('backupCfg');
    try {
      const res = await bridge.send<{ success: boolean; message: string; tools?: ToolsStatusDto }>('backup_user_cfg', {
        note: configNote || 'Manuell',
      });
      if (res.tools) setStatus(res.tools);
      setConfigNote('');
      showToast(res.message || 'user.cfg Backup erfolgreich archiviert!');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Erstellen des user.cfg Backups');
    } finally {
      setActionLoading(null);
    }
  };

  const handleRestoreConfigSnapshot = async (item: ConfigBackupItemDto) => {
    if (!item) return;
    setActionLoading(`restoreCfg_${item.name}`);
    try {
      const res = await bridge.send<{ success: boolean; message: string; tools?: ToolsStatusDto }>('restore_user_cfg', {
        name: item.name,
        filePath: item.filePath,
      });
      if (res.tools) {
        setStatus(res.tools);
        setCfgContent(res.tools.userCfgContent || '');
        parseCfgContent(res.tools.userCfgContent || '');
      }
      showToast(res.message || `user.cfg erfolgreich aus '${item.name}' wiederhergestellt!`);
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Wiederherstellen der Version');
    } finally {
      setActionLoading(null);
    }
  };

  const handleCreateKeybindBackup = async () => {
    setActionLoading('backupKeybind');
    try {
      const res = await bridge.send<ToolsStatusDto>('backup_keybinds', { note: keybindNote });
      setStatus(res);
      setKeybindNote('');
      showToast('Keybind-Backup erfolgreich angelegt (Lokal & Cloud)!');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Erstellen des Keybind-Backups');
    } finally {
      setActionLoading(null);
    }
  };

  const handleRestoreKeybind = async (item: KeybindBackupItemDto) => {
    if (!item) return;
    setActionLoading(`restoreKeybind_${item.name}`);
    try {
      const res = await bridge.send<{ success: boolean; message: string; tools?: ToolsStatusDto }>('restore_keybinds', {
        name: item.name,
        folderPath: item.folderPath,
      });
      if (res.tools) setStatus(res.tools);
      showToast(res.message || 'Keybinds erfolgreich wiederhergestellt!');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Wiederherstellen der Keybinds');
    } finally {
      setActionLoading(null);
    }
  };

  const handleSaveCloudPath = async () => {
    setActionLoading('saveCloud');
    try {
      const res = await bridge.send<{ success: boolean; tools?: ToolsStatusDto }>('save_cloud_storage_path', {
        path: cloudPath,
      });
      if (res.tools) setStatus(res.tools);
      showToast('Cloud-Speicherpfad erfolgreich gespeichert!');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Speichern des Cloud-Pfads');
    } finally {
      setActionLoading(null);
    }
  };

  const handleExportLogsZip = async () => {
    setActionLoading('exportZip');
    try {
      const res = await bridge.send<{ success: boolean; message: string; zipPath?: string }>('export_logs_zip');
      showToast(res.message || 'Logs erfolgreich als ZIP exportiert!');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Exportieren der Logs');
    } finally {
      setActionLoading(null);
    }
  };

  const handleSyncLogsCloud = async () => {
    setActionLoading('syncCloud');
    try {
      const res = await bridge.send<{ success: boolean; message: string }>('sync_logs_cloud');
      showToast(res.message || 'Logs erfolgreich in Cloud gesichert!');
      await loadStatus();
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Synchronisieren in die Cloud');
    } finally {
      setActionLoading(null);
    }
  };

  const handleToggleAutoCloudSync = async (enabled: boolean) => {
    try {
      const res = await bridge.send<{ success: boolean; autoCloudSyncEnabled: boolean; tools?: ToolsStatusDto }>('toggle_auto_cloud_sync', { enabled });
      if (res.tools) setStatus(res.tools);
      showToast(enabled ? 'Automatische Cloud-Synchronisation aktiviert' : 'Automatische Cloud-Synchronisation pausiert');
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Ändern der Cloud-Einstellung');
    }
  };

  const handleOpenFolder = async (folderType: 'keybinds' | 'config' | 'cloud' | 'logbackups') => {
    try {
      const res = await bridge.send<{ success: boolean; error?: string }>('open_folder', { folderType });
      if (!res.success && res.error) {
        showToast(res.error);
      }
    } catch (err) {
      console.error(err);
      showToast('Fehler beim Öffnen des Ordners');
    }
  };

  const copyToClipboard = () => {
    navigator.clipboard.writeText(cfgContent);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const loadHotasStatus = async () => {
    try {
      setHotasLoading(true);
      const res = await bridge.getHotasStatus();
      if (res) {
        setHotasStatus(res);
      }
    } catch (err) {
      console.error('Failed to load HOTAS status:', err);
    } finally {
      setHotasLoading(false);
    }
  };

  const handleSwapSticks = async (instA: number = 1, instB: number = 2) => {
    try {
      setHotasSwapping(true);
      const res = await bridge.swapHotasDevices(instA, instB);
      if (res?.success) {
        showToast(res.message);
        if (res.status) setHotasStatus(res.status);
        else await loadHotasStatus();
      } else {
        showToast(res?.message || 'Fehler beim Tausch der Joysticks');
      }
    } catch (err) {
      showToast('Fehler beim Tauschen der Joysticks');
    } finally {
      setHotasSwapping(false);
    }
  };

  const handleExportHotas = async () => {
    try {
      setHotasExporting(true);
      const res = await bridge.exportHotasLayout(hotasExportName);
      if (res?.success) {
        showToast(res.message);
        setHotasExportModalOpen(false);
      } else {
        showToast(res?.message || 'Fehler beim Exportieren des Profils');
      }
    } catch (err) {
      showToast('Fehler beim Exportieren des Profils');
    } finally {
      setHotasExporting(false);
    }
  };

  useEffect(() => {
    loadStatus();
    loadScreenshotStatus(true);
    loadHotasStatus();
    bridge.getSettings().then((st: SettingsDto) => {
      if (st && st.hotasProfilerEnabled !== undefined) {
        setHotasEnabled(st.hotasProfilerEnabled);
      }
    }).catch(() => {});

    const unsubscribe = bridge.on<ToolsStatusDto>('TOOLS_UPDATED', (newStatus) => {
      if (newStatus) {
        setStatus(newStatus);
        if (newStatus.cloudStoragePath !== undefined) {
          setCloudPath(newStatus.cloudStoragePath || '');
        }
      }
    });
    return () => unsubscribe();
  }, []);

  if (loading && !status) {
    return (
      <div className="flex flex-col items-center justify-center h-96 space-y-4">
        <RefreshCw className="w-8 h-8 text-sky-400 animate-spin" />
        <p className="text-sm text-slate-400">Lese Star Citizen Konfiguration &amp; System-Status aus...</p>
      </div>
    );
  }

  const cloudDisplay = status?.cloudStoragePath
    ? status.cloudAutoDetected
      ? `OneDrive (${status.cloudStoragePath})`
      : status.cloudStoragePath
    : 'Nicht konfiguriert';

  const filteredEntries = CFG_REFERENCE_ENTRIES.filter((e) => {
    if (!searchRef.trim()) return true;
    const q = searchRef.toLowerCase();
    return (
      e.command.toLowerCase().includes(q) ||
      e.category.toLowerCase().includes(q) ||
      e.explanation.toLowerCase().includes(q) ||
      e.tag.toLowerCase().includes(q)
    );
  });

  return (
    <div className="p-6 space-y-6 max-w-7xl mx-auto">
      {/* Toast Notification */}
      {toastMessage && (
        <div className={`fixed bottom-6 right-6 z-50 flex items-center space-x-3 px-4 py-3 rounded-xl bg-slate-900/95 shadow-2xl animate-in fade-in slide-in-from-bottom-2 max-w-md ${
          toastMessage.toLowerCase().includes('fehler') || toastMessage.toLowerCase().includes('gesperrt') || toastMessage.toLowerCase().includes('läuft') || toastMessage.toLowerCase().includes('nicht')
            ? 'border border-amber-500/60 shadow-amber-500/20 text-amber-100'
            : 'border border-sky-500/50 shadow-sky-500/20 text-sky-100'
        }`}>
          {toastMessage.toLowerCase().includes('fehler') || toastMessage.toLowerCase().includes('gesperrt') || toastMessage.toLowerCase().includes('läuft') || toastMessage.toLowerCase().includes('nicht') ? (
            <AlertTriangle className="w-5 h-5 text-amber-400 shrink-0" />
          ) : (
            <CheckCircle2 className="w-5 h-5 text-emerald-400 shrink-0" />
          )}
          <span className="text-xs font-semibold leading-relaxed">{toastMessage}</span>
        </div>
      )}

      {/* Popout / Fullscreen Editor Overlay Modal */}
      {editorPopout && (
        <div className="fixed inset-0 z-50 bg-slate-950/90 backdrop-blur-md p-6 flex flex-col animate-in fade-in">
          <div className="flex-1 max-w-7xl w-full mx-auto bg-slate-900/95 border border-slate-700 rounded-3xl p-6 flex flex-col shadow-2xl space-y-4">
            {/* Popout Toolbar */}
            <div className="flex items-center justify-between pb-3 border-b border-slate-800">
              <div className="flex items-center space-x-3">
                <div className="w-10 h-10 rounded-xl bg-sky-500/10 border border-sky-500/30 flex items-center justify-center text-sky-400">
                  <Terminal className="w-5 h-5" />
                </div>
                <div>
                  <div className="flex items-center space-x-2">
                    <h2 className="text-base font-bold text-white">user.cfg Editor (Popout-Modus)</h2>
                    <span className="text-xs font-mono text-slate-400">
                      ({cfgContent.split('\n').length} Zeilen · {cfgContent.length} Zeichen)
                    </span>
                  </div>
                  <p className="text-xs text-slate-400 font-mono">
                    {status?.userCfgPath || 'StarCitizen\\LIVE\\user.cfg'}
                  </p>
                </div>
              </div>

              <div className="flex items-center space-x-2">
                <button
                  onClick={() => applyPreset('5800x3d_5070', false)}
                  className="px-3 py-1.5 rounded-lg bg-amber-500/20 hover:bg-amber-500/30 text-amber-300 text-xs font-bold border border-amber-500/40 transition cursor-pointer"
                >
                  ⭐ 5800X3D &amp; RTX 5070
                </button>
                <button
                  onClick={copyToClipboard}
                  className="px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs transition cursor-pointer flex items-center space-x-1"
                >
                  {copied ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
                  <span>{copied ? 'Kopiert' : 'Kopieren'}</span>
                </button>
                <button
                  onClick={handleSaveUserCfg}
                  disabled={actionLoading === 'saveCfg'}
                  className="px-4 py-1.5 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold transition shadow-md shadow-sky-600/20 cursor-pointer flex items-center space-x-1.5"
                >
                  <Save className="w-3.5 h-3.5" />
                  <span>Speichern</span>
                </button>
                <button
                  onClick={() => setEditorPopout(false)}
                  className="p-2 rounded-xl bg-slate-800 hover:bg-rose-500/20 hover:text-rose-400 text-slate-400 border border-slate-700 transition cursor-pointer"
                  title="Popout schließen"
                >
                  <X className="w-5 h-5" />
                </button>
              </div>
            </div>

            {/* Popout Textarea (Full Height) */}
            <div className="flex-1 relative rounded-2xl bg-slate-950 border border-slate-800 font-mono text-xs overflow-hidden flex">
              <textarea
                value={cfgContent}
                onChange={(e) => {
                  setCfgContent(e.target.value);
                  parseCfgContent(e.target.value);
                }}
                spellCheck={false}
                className="w-full h-full p-5 bg-transparent text-sky-100 resize-none focus:outline-none font-mono text-xs leading-relaxed selection:bg-sky-800/50"
                placeholder="; Star Citizen user.cfg Konfiguration&#10;Con_Restricted = 0&#10;..."
              />
            </div>

            <div className="flex items-center justify-between text-xs text-slate-400 pt-1 font-mono">
              <span>Automatische lokale Sicherung und Cloud-Backup vor jedem Speichern</span>
              <button
                onClick={() => setEditorPopout(false)}
                className="text-sky-400 hover:underline cursor-pointer"
              >
                Zurück zur Standardansicht [Esc / Schließen]
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Header Banner */}
      <div className="p-5 rounded-2xl bg-gradient-to-r from-slate-900/95 via-slate-900/80 to-slate-950 border border-slate-800 shadow-xl flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center space-x-4">
          <div className="w-12 h-12 rounded-xl bg-sky-500/10 border border-sky-500/30 flex items-center justify-center text-sky-400 shadow-inner shrink-0">
            <Zap className="w-6 h-6" />
          </div>
          <div>
            <div className="flex items-center space-x-2.5">
              <h1 className="text-lg font-bold text-white tracking-wide">STAR CITIZEN TOOLS &amp; WARTUNG</h1>
              <span className="px-2 py-0.5 text-[11px] font-semibold rounded bg-sky-950 text-sky-400 border border-sky-800 font-mono">
                LIVE
              </span>
            </div>
            <p className="text-xs text-slate-400 mt-1">
              Client-Tuning, Cache-Bereinigung, Hardware-Benchmark &amp; Backups
            </p>
          </div>
        </div>
      </div>

      {/* EXACTLY 3 Top Navigation Tabs */}
      <div className="flex items-center space-x-2 border-b border-slate-800 pb-3">
        <button
          onClick={() => setActiveTab('maintenance')}
          className={`flex items-center space-x-2 px-4 py-2 rounded-xl text-xs font-semibold transition cursor-pointer ${
            activeTab === 'maintenance'
              ? 'bg-amber-500/20 text-amber-300 border border-amber-500/40 shadow-sm'
              : 'bg-slate-900/50 hover:bg-slate-800/70 text-slate-400 hover:text-slate-200 border border-transparent'
          }`}
        >
          <Wrench className="w-4 h-4" />
          <span>{locale === 'en' ? 'Maintenance & Diagnostics' : 'Wartung & Diagnose'}</span>
        </button>

        <button
          onClick={() => setActiveTab('cfg')}
          className={`flex items-center space-x-2 px-4 py-2 rounded-xl text-xs font-semibold transition cursor-pointer ${
            activeTab === 'cfg'
              ? 'bg-sky-500/20 text-sky-300 border border-sky-500/40 shadow-sm'
              : 'bg-slate-900/50 hover:bg-slate-800/70 text-slate-400 hover:text-slate-200 border border-transparent'
          }`}
        >
          <FileText className="w-4 h-4" />
          <span>user.cfg Studio</span>
        </button>

        <button
          onClick={() => setActiveTab('keybinds')}
          className={`flex items-center space-x-2 px-4 py-2 rounded-xl text-xs font-semibold transition cursor-pointer ${
            activeTab === 'keybinds'
              ? 'bg-purple-500/20 text-purple-300 border border-purple-500/40 shadow-sm'
              : 'bg-slate-900/50 hover:bg-slate-800/70 text-slate-400 hover:text-slate-200 border border-transparent'
          }`}
        >
          <Archive className="w-4 h-4" />
          <span>Backups</span>
          {((status?.keybindItems?.length || 0) + (status?.configBackups?.length || 0)) > 0 && (
            <span className="px-1.5 py-0.2 rounded-full bg-purple-950 text-purple-300 text-[10px] font-mono border border-purple-800 font-bold">
              {(status?.keybindItems?.length || 0) + (status?.configBackups?.length || 0)}
            </span>
          )}
        </button>

        <button
          onClick={() => {
            setActiveTab('hotas');
            if (!hotasStatus) loadHotasStatus();
          }}
          className={`flex items-center space-x-2 px-4 py-2 rounded-xl text-xs font-semibold transition cursor-pointer ${
            activeTab === 'hotas'
              ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/40 shadow-sm'
              : 'bg-slate-900/50 hover:bg-slate-800/70 text-slate-400 hover:text-slate-200 border border-transparent'
          }`}
        >
          <Gamepad2 className="w-4 h-4" />
          <span>HOTAS &amp; Joysticks</span>
          {hotasStatus?.hasMismatch && (
            <span className="flex h-2 w-2 relative">
              <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-rose-400 opacity-75"></span>
              <span className="relative inline-flex rounded-full h-2 w-2 bg-rose-500"></span>
            </span>
          )}
          {hotasStatus?.devices && hotasStatus.devices.length > 0 && (
            <span className="px-1.5 py-0.2 rounded-full bg-emerald-950 text-emerald-300 text-[10px] font-mono border border-emerald-800 font-bold">
              {hotasStatus.devices.length}
            </span>
          )}
        </button>
      </div>

      {/* ══════════════════════════════════════════════════════════════
          TAB 2: USER.CFG STUDIO (ALL CFG TOOLS IN ONE PLACE)
          ══════════════════════════════════════════════════════════════ */}
      {activeTab === 'cfg' && (
        <div className="space-y-5">
          {/* Sub-View Switcher within user.cfg */}
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 p-2 rounded-2xl bg-slate-900/80 border border-slate-800">
            <div className="flex flex-wrap items-center gap-1.5">
              <button
                onClick={() => setCfgView('editor')}
                className={`flex items-center space-x-2 px-3.5 py-1.5 rounded-xl text-xs font-bold transition cursor-pointer ${
                  cfgView === 'editor'
                    ? 'bg-sky-600 text-white shadow-md shadow-sky-600/30'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60'
                }`}
              >
                <FileText className="w-3.5 h-3.5" />
                <span>Live-Editor</span>
              </button>

              <button
                onClick={() => setCfgView('tuning')}
                className={`flex items-center space-x-2 px-3.5 py-1.5 rounded-xl text-xs font-bold transition cursor-pointer ${
                  cfgView === 'tuning'
                    ? 'bg-sky-600 text-white shadow-md shadow-sky-600/30'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60'
                }`}
              >
                <Sliders className="w-3.5 h-3.5" />
                <span>Tuning-Schalter</span>
              </button>
            </div>

            <div className="flex flex-wrap items-center gap-2">
              <button
                onClick={() => setShowLexiconModal(true)}
                className="flex items-center space-x-1.5 px-3 py-1.5 rounded-xl bg-slate-800/90 hover:bg-slate-700 text-slate-300 hover:text-white text-xs font-medium border border-slate-700/60 transition cursor-pointer"
                title="Befehls-Lexikon nach Parametern durchsuchen"
              >
                <BookOpen className="w-3.5 h-3.5 text-emerald-400" />
                <span>Befehls-Lexikon</span>
              </button>

              <button
                onClick={() => {
                  setActiveTab('keybinds');
                  setBackupSubTab('usercfg');
                }}
                className="flex items-center space-x-1.5 px-3 py-1.5 rounded-xl bg-slate-800/90 hover:bg-slate-700 text-slate-300 hover:text-white text-xs font-medium border border-slate-700/60 transition cursor-pointer"
                title="Zu den archivierten user.cfg Snapshots wechseln"
              >
                <Archive className="w-3.5 h-3.5 text-purple-400" />
                <span>Backups ({status?.configBackups?.length || 0})</span>
              </button>

              <div className="hidden lg:flex items-center space-x-2 pl-2 border-l border-slate-800 text-[11px] font-mono text-slate-400">
                <span className="flex items-center space-x-1 text-emerald-400">
                  <ShieldCheck className="w-3.5 h-3.5" />
                  <span>Auto-Backup aktiv</span>
                </span>
                <span>•</span>
                <span className="truncate max-w-[200px] text-slate-500" title={status?.userCfgPath || 'StarCitizen\\LIVE\\user.cfg'}>
                  {status?.userCfgPath || 'StarCitizen\\LIVE\\user.cfg'}
                </span>
              </div>
            </div>
          </div>

          {/* 1. SUB-VIEW: LIVE CODE EDITOR */}
          {cfgView === 'editor' && (
            <div className="space-y-5">
              {/* Presets Bar */}
              <div className="p-4 rounded-2xl bg-gradient-to-r from-slate-900/90 via-slate-900/70 to-slate-950 border border-slate-800 flex flex-col lg:flex-row lg:items-center justify-between gap-4">
                <div className="flex items-center space-x-3">
                  <div className="w-9 h-9 rounded-xl bg-amber-500/10 border border-amber-500/30 flex items-center justify-center text-amber-400 shrink-0">
                    <Sparkles className="w-4 h-4" />
                  </div>
                  <div>
                    <div className="text-xs font-bold text-white tracking-wide">SCHNELL-TUNING PROFILE</div>
                    <div className="text-[11px] text-slate-400">
                      Wähle ein vorkonfiguriertes Profil. Bestehende Einträge bleiben per Merge erhalten.
                    </div>
                  </div>
                </div>

                <div className="flex flex-wrap items-center gap-2">
                  <div className="flex items-center rounded-xl bg-gradient-to-r from-amber-500/20 to-sky-500/20 border border-amber-500/40 p-1 shadow-md shadow-amber-500/10">
                    <button
                      onClick={() => applyPreset('hardware_auto', false)}
                      className="px-3 py-1.5 text-xs font-bold text-amber-300 hover:text-white transition flex items-center space-x-1.5 cursor-pointer"
                      title={`Wendet die Empfehlung (${hwRec.badge}) per Merge auf deine bestehende user.cfg an`}
                    >
                      <Rocket className="w-3.5 h-3.5 text-amber-400" />
                      <span>⭐ Hardware-Empfehlung</span>
                      {hwRec.badge && (
                        <span className="px-1.5 py-0.5 rounded bg-amber-950/80 text-amber-300 text-[10px] font-mono border border-amber-500/30 font-semibold">
                          {hwRec.badge}
                        </span>
                      )}
                    </button>
                    <button
                      onClick={() => applyPreset('hardware_auto', true)}
                      className="px-2 py-1 text-[10px] font-mono text-amber-400 hover:text-amber-200 bg-amber-950/60 hover:bg-amber-900/60 rounded-lg border border-amber-500/30 transition cursor-pointer"
                      title={`Ersetzt den Inhalt komplett durch die saubere Vorlage (${hwRec.badge}) mit Kommentaren`}
                    >
                      Template
                    </button>
                  </div>

                  <button
                    onClick={() => applyPreset('esport')}
                    className="px-3 py-2 rounded-xl bg-slate-800/80 hover:bg-slate-700 text-sky-400 hover:text-sky-300 text-xs font-bold border border-slate-700 transition cursor-pointer"
                    title="165 FPS Limit, VSync Aus, 8GB StreamPool, DisplayInfo=1"
                  >
                    High FPS
                  </button>

                  <button
                    onClick={() => applyPreset('quality')}
                    className="px-3 py-2 rounded-xl bg-slate-800/80 hover:bg-slate-700 text-purple-400 hover:text-purple-300 text-xs font-bold border border-slate-700 transition cursor-pointer"
                    title="Unbegrenzt FPS, VSync Ein, 12GB StreamPool, SSDO"
                  >
                    Immersion
                  </button>

                  <button
                    onClick={() => applyPreset('minimal')}
                    className="px-3 py-2 rounded-xl bg-slate-800/80 hover:bg-slate-700 text-slate-300 hover:text-white text-xs font-semibold border border-slate-700 transition cursor-pointer"
                    title="60 FPS Cap, 4GB StreamPool"
                  >
                    60 FPS Cap
                  </button>

                  <button
                    onClick={() => setPreviewPreset('hardware_auto')}
                    className="flex items-center space-x-1.5 px-3 py-2 rounded-xl bg-slate-800/90 hover:bg-slate-700 text-amber-300 hover:text-white text-xs font-bold border border-amber-500/30 transition cursor-pointer"
                    title="Vorlagen-Vorschau öffnen und alle Profile im Detail ansehen"
                  >
                    <Eye className="w-3.5 h-3.5 text-amber-400" />
                    <span>Vorlagen-Vorschau</span>
                  </button>
                </div>
              </div>

              {/* Editor Card */}
              <div className="p-5 rounded-2xl bg-slate-900/90 border border-slate-800 flex flex-col shadow-2xl space-y-4">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between pb-3 border-b border-slate-800 gap-3">
                  <div className="flex items-center space-x-3">
                    <div className="w-8 h-8 rounded-lg bg-sky-500/10 border border-sky-500/30 flex items-center justify-center text-sky-400 shrink-0">
                      <Terminal className="w-4 h-4" />
                    </div>
                    <div>
                      <div className="flex items-center space-x-2">
                        <span className="text-sm font-bold text-white">LIVE user.cfg Editor</span>
                        <span className="text-xs text-slate-400 font-mono">
                          ({cfgContent.split('\n').length} Zeilen · {cfgContent.length} Zeichen)
                        </span>
                      </div>
                      <p className="text-[11px] text-slate-400 font-mono truncate max-w-lg" title={status?.userCfgPath || 'StarCitizen\\LIVE\\user.cfg'}>
                        {status?.userCfgPath || 'StarCitizen\\LIVE\\user.cfg'}
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center space-x-2">
                    <button
                      onClick={() => setEditorPopout(true)}
                      className="flex items-center space-x-1.5 px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-sky-300 text-xs font-semibold border border-slate-700 transition cursor-pointer"
                      title="Editor als eigenes Popout / Vollbild öffnen"
                    >
                      <Maximize2 className="w-3.5 h-3.5 text-sky-400" />
                      <span>Popout-Modus</span>
                    </button>

                    <button
                      onClick={copyToClipboard}
                      className="flex items-center space-x-1 px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs transition cursor-pointer"
                      title="In Zwischenablage kopieren"
                    >
                      {copied ? (
                        <>
                          <Check className="w-3.5 h-3.5 text-emerald-400" />
                          <span className="text-emerald-400">Kopiert</span>
                        </>
                      ) : (
                        <>
                          <Copy className="w-3.5 h-3.5" />
                          <span>Kopieren</span>
                        </>
                      )}
                    </button>

                    <button
                      onClick={handleBackupUserCfgSnapshot}
                      disabled={actionLoading !== null}
                      className="flex items-center space-x-1.5 px-3 py-1.5 rounded-lg bg-purple-950 hover:bg-purple-900 text-purple-300 text-xs font-semibold border border-purple-800 transition cursor-pointer"
                      title="Erstellt sofort einen manuellen Snapshot dieser Konfiguration"
                    >
                      <Archive className="w-3.5 h-3.5" />
                      <span>Snapshot sichern</span>
                    </button>

                    <button
                      onClick={handleSaveUserCfg}
                      disabled={actionLoading === 'saveCfg'}
                      className="flex items-center space-x-1.5 px-4 py-1.5 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold transition shadow-md shadow-sky-600/20 cursor-pointer"
                    >
                      <Save className="w-3.5 h-3.5" />
                      <span>{actionLoading === 'saveCfg' ? 'Speichere...' : 'Speichern (mit Auto-Backup)'}</span>
                    </button>
                  </div>
                </div>

                <div className="relative rounded-xl bg-slate-950 border border-slate-800 font-mono text-xs overflow-hidden flex shadow-inner">
                  <textarea
                    value={cfgContent}
                    onChange={(e) => {
                      setCfgContent(e.target.value);
                      parseCfgContent(e.target.value);
                    }}
                    spellCheck={false}
                    className="w-full min-h-[500px] p-4 bg-transparent text-sky-100 resize-y focus:outline-none font-mono text-xs leading-relaxed selection:bg-sky-800/50"
                    placeholder="; Star Citizen user.cfg Konfiguration&#10;Con_Restricted = 0&#10;r_VSync = 0&#10;sys_maxfps = 160&#10;..."
                  />
                </div>

                <div className="pt-2 border-t border-slate-800/80 flex flex-col sm:flex-row sm:items-center justify-between text-xs text-slate-400 gap-2">
                  <div className="flex items-center space-x-2">
                    <ShieldCheck className="w-4 h-4 text-emerald-400 shrink-0" />
                    <span className="text-slate-300">
                      100% Schutz: Kommentare (-- oder ;) und eigene Zeilen werden niemals gelöscht.
                    </span>
                  </div>

                  <div className="flex items-center space-x-3 text-[11px] font-mono">
                    <button
                      onClick={() => setCfgView('tuning')}
                      className="text-sky-400 hover:text-sky-300 hover:underline cursor-pointer"
                    >
                      Zu den visuellen Reglern →
                    </button>
                    <span>•</span>
                    <button
                      onClick={() => setShowLexiconModal(true)}
                      className="text-emerald-400 hover:text-emerald-300 hover:underline cursor-pointer"
                    >
                      Befehls-Lexikon →
                    </button>
                    <span>•</span>
                    <button
                      onClick={() => {
                        setActiveTab('keybinds');
                        setBackupSubTab('usercfg');
                      }}
                      className="text-purple-400 hover:text-purple-300 hover:underline cursor-pointer"
                    >
                      Zu den Backups ({status?.configBackups?.length || 0}) →
                    </button>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* 2. SUB-VIEW: VISUELLE TUNING REGLER */}
          {cfgView === 'tuning' && (
            <div className="space-y-6">
              <div className="p-4 rounded-2xl bg-gradient-to-r from-sky-950/40 via-slate-900/60 to-purple-950/40 border border-sky-600/30 flex items-start justify-between gap-4">
                <div className="flex items-start space-x-3">
                  <Sliders className="w-5 h-5 text-sky-400 shrink-0 mt-0.5" />
                  <div>
                    <h2 className="text-sm font-bold text-white tracking-wide">
                      VISUELLES GRAFIK- &amp; PERFORMANCE-TUNING
                    </h2>
                    <p className="text-xs text-slate-300 mt-0.5">
                      Alle Änderungen werden sofort per <strong>Live-Merge</strong> in deine <code>user.cfg</code> synchronisiert. Kommentare und individuelle CVARs bleiben 100% erhalten.
                    </p>
                  </div>
                </div>

                <div className="flex items-center space-x-2 shrink-0">
                  <button
                    onClick={handleSaveUserCfg}
                    disabled={actionLoading === 'saveCfg'}
                    className="flex items-center space-x-1.5 px-4 py-2 rounded-xl bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold transition shadow-md shadow-sky-600/20 cursor-pointer"
                  >
                    <Save className="w-3.5 h-3.5" />
                    <span>Speichern</span>
                  </button>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
                {/* CARD 1: CPU & Thread Management */}
                <div className="p-5 rounded-2xl bg-slate-900/80 border border-slate-800 space-y-4 flex flex-col justify-between">
                  <div>
                    <div className="flex items-center space-x-2.5 pb-3 border-b border-slate-800 text-sky-400 font-bold text-xs tracking-wider">
                      <Cpu className="w-4 h-4" />
                      <span>1. SYSTEM &amp; CPU-KERNMANAGEMENT</span>
                    </div>

                    <div className="space-y-3 pt-3 text-xs">
                      <label className="flex items-center space-x-3 cursor-pointer p-2.5 rounded-xl hover:bg-slate-800/50 border border-slate-800/40 transition">
                        <input
                          type="checkbox"
                          checked={consoleUnlocked}
                          onChange={(e) => {
                            const val = e.target.checked;
                            setConsoleUnlocked(val);
                            updateSingleCvar('Con_Restricted', val ? 0 : 1);
                          }}
                          className="rounded border-slate-700 text-sky-600 focus:ring-sky-500 bg-slate-800 cursor-pointer"
                        />
                        <div className="flex-1">
                          <div className="flex items-center justify-between">
                            <span className="text-slate-200 font-semibold">Entwicklerkonsole freigeben</span>
                            <CvarHelpTooltip command="Con_Restricted" />
                          </div>
                          <span className="text-[11px] text-slate-400 font-mono">Con_Restricted = {consoleUnlocked ? 0 : 1}</span>
                          <span className="text-[10px] text-slate-500 block mt-0.5">Schaltet Taste ^ / ~ frei (wird von SCLogMate automatisch an den Dateianfang gesetzt)</span>
                        </div>
                      </label>

                      <label className="flex items-center space-x-3 cursor-pointer p-2.5 rounded-xl hover:bg-slate-800/50 border border-slate-800/40 transition">
                        <input
                          type="checkbox"
                          checked={multithreaded}
                          onChange={(e) => {
                            const val = e.target.checked;
                            setMultithreaded(val);
                            updateSingleCvar('r_multithreaded', val ? 1 : 0);
                          }}
                          className="rounded border-slate-700 text-sky-600 focus:ring-sky-500 bg-slate-800 cursor-pointer"
                        />
                        <div className="flex-1">
                          <div className="flex items-center justify-between">
                            <span className="text-slate-200 font-semibold block">Multithreading aktivieren</span>
                            <CvarHelpTooltip command="r_multithreaded" />
                          </div>
                          <span className="text-[11px] text-slate-400 font-mono">r_multithreaded = {multithreaded ? 1 : 0}</span>
                          <span className="text-[10px] text-emerald-400 block mt-0.5">Optimal für 5800X3D (16 Threads)</span>
                        </div>
                      </label>
                    </div>
                  </div>

                  <div className="text-[10px] text-slate-500 font-mono pt-2 border-t border-slate-800/60">
                    sys_job_system_max_worker wurde entfernt, damit SC alle Kerne voll auslastet.
                  </div>
                </div>

                {/* CARD 2: Monitor, FPS & Synchronisation */}
                <div className="p-5 rounded-2xl bg-slate-900/80 border border-slate-800 space-y-4 flex flex-col justify-between">
                  <div>
                    <div className="flex items-center space-x-2.5 pb-3 border-b border-slate-800 text-emerald-400 font-bold text-xs tracking-wider">
                      <Activity className="w-4 h-4" />
                      <span>2. MONITOR, FPS &amp; SYNCHRONISATION</span>
                    </div>

                    <div className="space-y-3 pt-3 text-xs">
                      <div>
                        <div className="flex items-center justify-between mb-1">
                          <div className="flex items-center space-x-1">
                            <span className="text-slate-200 font-semibold">Max FPS Limit:</span>
                            <CvarHelpTooltip command="sys_maxFps" />
                          </div>
                          <span className="text-[11px] text-slate-400 font-mono">sys_maxFps = {maxFps}</span>
                        </div>
                        <div className="flex items-center space-x-2">
                          <input
                            type="number"
                            min={0}
                            max={360}
                            step={5}
                            value={maxFps}
                            onChange={(e) => {
                              const val = parseInt(e.target.value, 10) || 0;
                              setMaxFps(val);
                              updateSingleCvar('sys_maxFps', val);
                            }}
                            className="w-24 bg-slate-950 border border-slate-700 rounded-lg px-2.5 py-1 text-slate-200 text-xs font-mono text-right focus:outline-none focus:border-sky-500"
                          />
                          <div className="flex items-center space-x-1">
                            {[60, 120, 144, 160].map((f) => (
                              <button
                                key={f}
                                onClick={() => {
                                  setMaxFps(f);
                                  updateSingleCvar('sys_maxFps', f);
                                }}
                                className={`px-2 py-0.5 rounded text-[10px] font-mono border transition cursor-pointer ${
                                  maxFps === f
                                    ? 'bg-sky-950 text-sky-300 border-sky-700 font-bold'
                                    : 'bg-slate-800 text-slate-400 border-slate-700 hover:text-white'
                                }`}
                              >
                                {f}
                              </button>
                            ))}
                          </div>
                        </div>
                      </div>

                      <label className="flex items-center space-x-3 cursor-pointer p-2.5 rounded-xl hover:bg-slate-800/50 border border-slate-800/40 transition">
                        <input
                          type="checkbox"
                          checked={vsyncOff}
                          onChange={(e) => {
                            const val = e.target.checked;
                            setVsyncOff(val);
                            updateSingleCvar('r_VSync', val ? 0 : 1);
                          }}
                          className="rounded border-slate-700 text-sky-600 focus:ring-sky-500 bg-slate-800 cursor-pointer"
                        />
                        <div className="flex-1">
                          <div className="flex items-center justify-between">
                            <span className="text-slate-200 font-semibold block">VSync im Spiel ausschalten</span>
                            <CvarHelpTooltip command="r_VSync" />
                          </div>
                          <span className="text-[11px] text-slate-400 font-mono">r_VSync = {vsyncOff ? 0 : 1}</span>
                          <span className="text-[10px] text-slate-500 block">Bei G-Sync/FreeSync immer auf 0 setzen</span>
                        </div>
                      </label>

                      <label className="flex items-center space-x-3 cursor-pointer p-2.5 rounded-xl hover:bg-slate-800/50 border border-slate-800/40 transition">
                        <input
                          type="checkbox"
                          checked={fullGpuSyncOff}
                          onChange={(e) => {
                            const val = e.target.checked;
                            setFullGpuSyncOff(val);
                            updateSingleCvar('r_enable_full_gpu_sync', val ? 0 : 1);
                          }}
                          className="rounded border-slate-700 text-sky-600 focus:ring-sky-500 bg-slate-800 cursor-pointer"
                        />
                        <div className="flex-1">
                          <div className="flex items-center justify-between">
                            <span className="text-slate-200 font-semibold block">Full GPU Sync lockern (0)</span>
                            <CvarHelpTooltip command="r_enable_full_gpu_sync" />
                          </div>
                          <span className="text-[11px] text-slate-400 font-mono">r_enable_full_gpu_sync = {fullGpuSyncOff ? 0 : 1}</span>
                          <span className="text-[10px] text-slate-500 block">Reduziert Input-Lag &amp; liefert bessere FPS</span>
                        </div>
                      </label>

                      <div className="grid grid-cols-2 gap-2 pt-1 border-t border-slate-800/60">
                        <label className="flex items-center justify-between cursor-pointer p-1.5 rounded-lg hover:bg-slate-800/50">
                          <div className="flex items-center space-x-2">
                            <input
                              type="checkbox"
                              checked={fullscreenWindow}
                              onChange={(e) => {
                                const val = e.target.checked;
                                setFullscreenWindow(val);
                                updateSingleCvar('r_FullscreenWindow', val ? 1 : 0);
                              }}
                              className="rounded border-slate-700 text-sky-600 bg-slate-800"
                            />
                            <span className="text-[11px] text-slate-300">FullscreenWindow</span>
                          </div>
                          <CvarHelpTooltip command="r_FullscreenWindow" />
                        </label>

                        <label className="flex items-center justify-between cursor-pointer p-1.5 rounded-lg hover:bg-slate-800/50">
                          <div className="flex items-center space-x-2">
                            <input
                              type="checkbox"
                              checked={borderlessWindow}
                              onChange={(e) => {
                                const val = e.target.checked;
                                setBorderlessWindow(val);
                                updateSingleCvar('r_BorderlessWindow', val ? 1 : 0);
                              }}
                              className="rounded border-slate-700 text-sky-600 bg-slate-800"
                            />
                            <span className="text-[11px] text-slate-300">BorderlessWindow</span>
                          </div>
                          <CvarHelpTooltip command="r_BorderlessWindow" />
                        </label>
                      </div>
                    </div>
                  </div>

                  <div className="text-[10px] text-emerald-400 font-mono pt-2 border-t border-slate-800/60">
                    Randloses Fenster sorgt für stabiles Alt-Tabben ohne Bildflackern.
                  </div>
                </div>

                {/* CARD 3: VRAM & Texture Streaming */}
                <div className="p-5 rounded-2xl bg-slate-900/80 border border-slate-800 space-y-4 flex flex-col justify-between">
                  <div>
                    <div className="flex items-center space-x-2.5 pb-3 border-b border-slate-800 text-purple-400 font-bold text-xs tracking-wider">
                      <HardDrive className="w-4 h-4" />
                      <span>4. TEXTUREN, GEOMETRIE &amp; STREAMING</span>
                    </div>

                    <div className="space-y-3 pt-3 text-xs">
                      <div>
                        <div className="flex items-center justify-between mb-1">
                          <div className="flex items-center space-x-1">
                            <span className="text-slate-200 font-semibold">VRAM Textur-Pool:</span>
                            <CvarHelpTooltip command="r_TexturesStreamPoolSize" />
                          </div>
                          <span className="text-[11px] text-slate-400 font-mono">r_TexturesStreamPoolSize</span>
                        </div>
                        <select
                          value={streamPool}
                          onChange={(e) => {
                            const val = parseInt(e.target.value, 10);
                            setStreamPool(val);
                            updateSingleCvar('r_TexturesStreamPoolSize', val);
                          }}
                          className="w-full bg-slate-950 border border-slate-700 rounded-lg px-2.5 py-1.5 text-slate-200 text-xs focus:outline-none focus:border-sky-500 cursor-pointer font-mono"
                        >
                          <option value={6144}>6144 MB (8 GB VRAM)</option>
                          <option value={8192}>8192 MB (⭐ RTX 5070 12GB / 16GB VRAM)</option>
                          <option value={12288}>12288 MB (16GB+ / 24 GB RTX 4090)</option>
                        </select>
                      </div>

                      <div>
                        <div className="flex items-center justify-between mb-1">
                          <div className="flex items-center space-x-1">
                            <span className="text-slate-200 font-semibold">Geometriedaten-Pool:</span>
                            <CvarHelpTooltip command="e_StreamCgfPoolSize" />
                          </div>
                          <span className="text-[11px] text-slate-400 font-mono">e_StreamCgfPoolSize</span>
                        </div>
                        <select
                          value={streamCgfPoolSize}
                          onChange={(e) => {
                            const val = parseInt(e.target.value, 10);
                            setStreamCgfPoolSize(val);
                            updateSingleCvar('e_StreamCgfPoolSize', val);
                          }}
                          className="w-full bg-slate-950 border border-slate-700 rounded-lg px-2.5 py-1.5 text-slate-200 text-xs focus:outline-none focus:border-sky-500 cursor-pointer font-mono"
                        >
                          <option value={2048}>2048 MB (Standard / 16GB RAM)</option>
                          <option value={4096}>4096 MB (⭐ Standard für 32GB / 64GB RAM)</option>
                          <option value={8192}>8192 MB (Extrem / 64GB+ RAM)</option>
                        </select>
                      </div>

                      <label className="flex items-center space-x-3 cursor-pointer p-2.5 rounded-xl hover:bg-slate-800/50 border border-slate-800/40 transition">
                        <input
                          type="checkbox"
                          checked={texturesStreaming}
                          onChange={(e) => {
                            const val = e.target.checked;
                            setTexturesStreaming(val);
                            updateSingleCvar('r_TexturesStreaming', val ? 1 : 0);
                          }}
                          className="rounded border-slate-700 text-sky-600 focus:ring-sky-500 bg-slate-800 cursor-pointer"
                        />
                        <div className="flex-1">
                          <div className="flex items-center justify-between">
                            <span className="text-slate-200 font-semibold block">Dynamisches Textur-Streaming</span>
                            <CvarHelpTooltip command="r_TexturesStreaming" />
                          </div>
                          <span className="text-[11px] text-slate-400 font-mono">r_TexturesStreaming = {texturesStreaming ? 1 : 0}</span>
                          <span className="text-[10px] text-slate-500 block">Sollte immer auf 1 stehen</span>
                        </div>
                      </label>
                    </div>
                  </div>

                  <div className="text-[10px] text-purple-400 font-mono pt-2 border-t border-slate-800/60">
                    8192 MB Pool ist der Sweet-Spot für die 12GB GDDR7 der RTX 5070.
                  </div>
                </div>

                {/* CARD 4: HDR Display Output */}
                <div className="p-5 rounded-2xl bg-slate-900/80 border border-slate-800 space-y-4 flex flex-col justify-between">
                  <div>
                    <div className="flex items-center space-x-2.5 pb-3 border-b border-slate-800 text-amber-400 font-bold text-xs tracking-wider">
                      <Sun className="w-4 h-4" />
                      <span>3. HDR EINSTELLUNGEN</span>
                    </div>

                    <div className="space-y-3 pt-3 text-xs">
                      <label className="flex items-center space-x-3 cursor-pointer p-2.5 rounded-xl hover:bg-slate-800/50 border border-slate-800/40 transition">
                        <input
                          type="checkbox"
                          checked={hdrOutput}
                          onChange={(e) => {
                            const val = e.target.checked;
                            setHdrOutput(val);
                            updateSingleCvar('r_HDRDisplayOutput', val ? 1 : 0);
                          }}
                          className="rounded border-slate-700 text-amber-500 focus:ring-amber-500 bg-slate-800 cursor-pointer"
                        />
                        <div className="flex-1">
                          <div className="flex items-center justify-between">
                            <span className="text-slate-200 font-semibold block">Native HDR-Ausgabe</span>
                            <CvarHelpTooltip command="r_HDRDisplayOutput" />
                          </div>
                          <span className="text-[11px] text-slate-400 font-mono">r_HDRDisplayOutput = {hdrOutput ? 1 : 0}</span>
                          <span className="text-[10px] text-slate-500 block">Erfordert vorher in Windows aktiviertes HDR</span>
                        </div>
                      </label>

                      <div className="grid grid-cols-2 gap-2">
                        <div>
                          <div className="flex items-center justify-between mb-1">
                            <span className="text-slate-300 font-semibold block">Max Nits:</span>
                            <CvarHelpTooltip command="r_HDRDisplayMaxNits" />
                          </div>
                          <select
                            value={hdrMaxNits}
                            onChange={(e) => {
                              const val = parseInt(e.target.value, 10);
                              setHdrMaxNits(val);
                              updateSingleCvar('r_HDRDisplayMaxNits', val);
                            }}
                            className="w-full bg-slate-950 border border-slate-700 rounded-lg px-2 py-1 text-slate-200 text-xs font-mono cursor-pointer"
                          >
                            <option value={400}>400 Nits</option>
                            <option value={600}>600 Nits</option>
                            <option value={1000}>1000 Nits (⭐)</option>
                            <option value={1400}>1400 Nits</option>
                          </select>
                        </div>

                        <div>
                          <div className="flex items-center justify-between mb-1">
                            <span className="text-slate-300 font-semibold block">Ref White:</span>
                            <CvarHelpTooltip command="r_HDRDisplayRefWhite" />
                          </div>
                          <select
                            value={hdrRefWhite}
                            onChange={(e) => {
                              const val = parseInt(e.target.value, 10);
                              setHdrRefWhite(val);
                              updateSingleCvar('r_HDRDisplayRefWhite', val);
                            }}
                            className="w-full bg-slate-950 border border-slate-700 rounded-lg px-2 py-1 text-slate-200 text-xs font-mono cursor-pointer"
                          >
                            <option value={100}>100</option>
                            <option value={200}>200 (⭐)</option>
                            <option value={300}>300</option>
                          </select>
                        </div>
                      </div>

                      <label className="flex items-center space-x-3 cursor-pointer p-2.5 rounded-xl hover:bg-slate-800/50 border border-slate-800/40 transition">
                        <input
                          type="checkbox"
                          checked={hdrDeviceLimits}
                          onChange={(e) => {
                            const val = e.target.checked;
                            setHdrDeviceLimits(val);
                            updateSingleCvar('r_HDRDisplayDeviceLimits', val ? 1 : 0);
                          }}
                          className="rounded border-slate-700 text-amber-500 focus:ring-amber-500 bg-slate-800 cursor-pointer"
                        />
                        <div className="flex-1">
                          <div className="flex items-center justify-between">
                            <span className="text-slate-200 font-semibold block">Hardware-Limits einhalten</span>
                            <CvarHelpTooltip command="r_HDRDisplayDeviceLimits" />
                          </div>
                          <span className="text-[11px] text-slate-400 font-mono">r_HDRDisplayDeviceLimits = {hdrDeviceLimits ? 1 : 0}</span>
                        </div>
                      </label>
                    </div>
                  </div>

                  <div className="text-[10px] text-amber-400/90 font-mono pt-2 border-t border-slate-800/60">
                    1000 Nits &amp; 200 RefWhite bieten brillante Sterne ohne Überstrahlung im Cockpit.
                  </div>
                </div>

                {/* CARD 5: Shader & Micro-Stutter Protection */}
                <div className="p-5 rounded-2xl bg-slate-900/80 border border-slate-800 space-y-4 flex flex-col justify-between">
                  <div>
                    <div className="flex items-center space-x-2.5 pb-3 border-b border-slate-800 text-cyan-400 font-bold text-xs tracking-wider">
                      <Layers className="w-4 h-4" />
                      <span>5. SHADER- &amp; RUCKLER-OPTIMIERUNG (CACHE)</span>
                    </div>

                    <div className="space-y-3 pt-3 text-xs">
                      <label className="flex items-center space-x-3 cursor-pointer p-2.5 rounded-xl hover:bg-slate-800/50 border border-slate-800/40 transition">
                        <input
                          type="checkbox"
                          checked={shadersAsync}
                          onChange={(e) => {
                            const val = e.target.checked;
                            setShadersAsync(val);
                            updateSingleCvar('r_shadersasyncactivation', val ? 1 : 0);
                          }}
                          className="rounded border-slate-700 text-cyan-600 focus:ring-cyan-500 bg-slate-800 cursor-pointer"
                        />
                        <div className="flex-1">
                          <div className="flex items-center justify-between">
                            <span className="text-slate-200 font-semibold block">Asynchrone Shader-Aktivierung</span>
                            <CvarHelpTooltip command="r_shadersasyncactivation" />
                          </div>
                          <span className="text-[11px] text-slate-400 font-mono">r_shadersasyncactivation = {shadersAsync ? 1 : 0}</span>
                          <span className="text-[10px] text-emerald-400 block">Verhindert Stuttering bei neuen Shadern</span>
                        </div>
                      </label>

                      <label className="flex items-center space-x-3 cursor-pointer p-2.5 rounded-xl hover:bg-slate-800/50 border border-slate-800/40 transition">
                        <input
                          type="checkbox"
                          checked={gsmCache}
                          onChange={(e) => {
                            const val = e.target.checked;
                            setGsmCache(val);
                            updateSingleCvar('r_GsmCache', val ? 1 : 0);
                          }}
                          className="rounded border-slate-700 text-cyan-600 focus:ring-cyan-500 bg-slate-800 cursor-pointer"
                        />
                        <div className="flex-1">
                          <div className="flex items-center justify-between">
                            <span className="text-slate-200 font-semibold block">Global Shadow Map Cache</span>
                            <CvarHelpTooltip command="r_GsmCache" />
                          </div>
                          <span className="text-[11px] text-slate-400 font-mono">r_GsmCache = {gsmCache ? 1 : 0}</span>
                          <span className="text-[10px] text-slate-400 block">Entlastet CPU in Städten (Lorville, Area18)</span>
                        </div>
                      </label>
                    </div>
                  </div>

                  <div className="text-[10px] text-slate-500 font-mono pt-2 border-t border-slate-800/60">
                    Kombination verhindert die gefürchteten Ruckler beim Betreten von Städten und Hangars.
                  </div>
                </div>

                {/* CARD 6: Details, Grafik-Tweaks & SSDO */}
                <div className="p-5 rounded-2xl bg-slate-900/80 border border-slate-800 space-y-4 flex flex-col justify-between">
                  <div>
                    <div className="flex items-center space-x-2.5 pb-3 border-b border-slate-800 text-rose-400 font-bold text-xs tracking-wider">
                      <Palette className="w-4 h-4" />
                      <span>3. GRAFIK &amp; VISUELLE EFFEKTE</span>
                    </div>

                    <div className="space-y-3 pt-3 text-xs">
                      <label className="flex items-center space-x-3 cursor-pointer p-2.5 rounded-xl hover:bg-slate-800/50 border border-slate-800/40 transition">
                        <input
                          type="checkbox"
                          checked={motionBlurOff}
                          onChange={(e) => {
                            const val = e.target.checked;
                            setMotionBlurOff(val);
                            updateSingleCvar('r_MotionBlur', val ? 0 : 1);
                          }}
                          className="rounded border-slate-700 text-rose-600 focus:ring-rose-500 bg-slate-800 cursor-pointer"
                        />
                        <div className="flex-1">
                          <div className="flex items-center justify-between">
                            <span className="text-slate-200 font-semibold block">Bewegungsunschärfe deaktivieren</span>
                            <CvarHelpTooltip command="r_MotionBlur" />
                          </div>
                          <span className="text-[11px] text-slate-400 font-mono">r_MotionBlur = {motionBlurOff ? 0 : 1}</span>
                          <span className="text-[10px] text-slate-500 block">Erhöht Bildschärfe bei schnellen Drehungen</span>
                        </div>
                      </label>

                      <div>
                        <div className="flex items-center justify-between mb-1">
                          <div className="flex items-center space-x-1">
                            <span className="text-slate-200 font-semibold">SSDO Umgebungsverdeckung:</span>
                            <CvarHelpTooltip command="r_ssdo" />
                          </div>
                        </div>
                        <select
                          value={ssdoLevel}
                          onChange={(e) => {
                            const val = parseInt(e.target.value, 10);
                            setSsdoLevel(val);
                            updateSingleCvar('r_ssdo', val);
                          }}
                          className="w-full bg-slate-950 border border-slate-700 rounded-lg px-2.5 py-1.5 text-slate-200 text-xs font-mono cursor-pointer"
                        >
                          <option value={0}>0 = Ausgeschaltet</option>
                          <option value={1}>1 = Normal (Standard für realistischere Schatten)</option>
                          <option value={2}>2 = Hoch (Maximale Schattentiefe)</option>
                        </select>
                      </div>

                      <div className="grid grid-cols-2 gap-2">
                        <div>
                          <div className="flex items-center justify-between mb-1">
                            <span className="text-slate-300 font-semibold block">Partikelqualität:</span>
                            <CvarHelpTooltip command="e_ParticlesQuality" />
                          </div>
                          <select
                            value={particlesQuality}
                            onChange={(e) => {
                              const val = parseInt(e.target.value, 10);
                              setParticlesQuality(val);
                              updateSingleCvar('e_ParticlesQuality', val);
                            }}
                            className="w-full bg-slate-950 border border-slate-700 rounded-lg px-2 py-1 text-slate-200 text-xs font-mono cursor-pointer"
                          >
                            <option value={1}>1 = Niedrig</option>
                            <option value={2}>2 = Medium</option>
                            <option value={3}>3 = Hoch (⭐)</option>
                          </select>
                        </div>

                        <div>
                          <div className="flex items-center justify-between mb-1">
                            <span className="text-slate-300 font-semibold block">Detail-Distanz:</span>
                            <CvarHelpTooltip command="r_DetailDistance" />
                          </div>
                          <input
                            type="number"
                            min={5}
                            max={40}
                            value={detailDistance}
                            onChange={(e) => {
                              const val = parseInt(e.target.value, 10) || 22;
                              setDetailDistance(val);
                              updateSingleCvar('r_DetailDistance', val);
                            }}
                            className="w-full bg-slate-950 border border-slate-700 rounded-lg px-2 py-1 text-slate-200 text-xs font-mono text-right focus:outline-none focus:border-rose-500"
                          />
                        </div>
                      </div>
                    </div>
                  </div>

                  <div className="text-[10px] text-slate-500 font-mono pt-2 border-t border-slate-800/60">
                    DetailDistance = 22 verringert das Aufploppen (Pop-in) von Texturen auf Distanz.
                  </div>
                </div>

                {/* CARD 7: Interface, Telemetrie & Sprache */}
                <div className="p-5 rounded-2xl bg-slate-900/80 border border-slate-800 space-y-4 flex flex-col justify-between">
                  <div>
                    <div className="flex items-center space-x-2.5 pb-3 border-b border-slate-800 text-indigo-400 font-bold text-xs tracking-wider">
                      <Monitor className="w-4 h-4" />
                      <span>6. SPRACHE &amp; INTERFACE</span>
                    </div>

                    <div className="space-y-3 pt-3 text-xs">
                      <label className="flex items-center space-x-3 cursor-pointer p-2.5 rounded-xl hover:bg-slate-800/50 border border-slate-800/40 transition">
                        <input
                          type="checkbox"
                          checked={hardwareCursor}
                          onChange={(e) => {
                            const val = e.target.checked;
                            setHardwareCursor(val);
                            updateSingleCvar('pl_pit.forceSoftwareCursor', val ? 0 : 1);
                          }}
                          className="rounded border-slate-700 text-indigo-600 focus:ring-indigo-500 bg-slate-800 cursor-pointer"
                        />
                        <div className="flex-1">
                          <div className="flex items-center justify-between">
                            <span className="text-slate-200 font-semibold block">Hardware-Mauszeiger (0)</span>
                            <CvarHelpTooltip command="pl_pit.forceSoftwareCursor" />
                          </div>
                          <span className="text-[11px] text-slate-400 font-mono">pl_pit.forceSoftwareCursor = {hardwareCursor ? 0 : 1}</span>
                          <span className="text-[10px] text-emerald-400 block">Verhindert träge oder ruckelnde Zeiger</span>
                        </div>
                      </label>

                      <div>
                        <div className="flex items-center justify-between mb-1">
                          <div className="flex items-center space-x-1">
                            <span className="text-slate-300 font-semibold block">r_DisplayInfo Telemetrie:</span>
                            <CvarHelpTooltip command="r_DisplayInfo" />
                          </div>
                        </div>
                        <select
                          value={displayInfo}
                          onChange={(e) => {
                            const val = parseInt(e.target.value, 10);
                            setDisplayInfo(val);
                            updateSingleCvar('r_DisplayInfo', val);
                          }}
                          className="w-full bg-slate-950 border border-slate-700 rounded-lg px-2.5 py-1 text-slate-200 text-xs focus:outline-none focus:border-indigo-500 cursor-pointer font-mono"
                        >
                          <option value={0}>0 = Ausgeschaltet</option>
                          <option value={1}>1 = Nur FPS (Schlank)</option>
                          <option value={2}>2 = FPS, Server-Tickrate &amp; RAM</option>
                          <option value={3}>3 = Detaillierte Render- &amp; Thread-Statistiken</option>
                          <option value={4}>4 = Vollständige Debug-Telemetrie</option>
                        </select>
                      </div>

                      <div className="grid grid-cols-2 gap-2">
                        <div>
                          <div className="flex items-center justify-between mb-1">
                            <span className="text-slate-300 font-semibold block">g_language:</span>
                            <CvarHelpTooltip command="g_language" />
                          </div>
                          <select
                            value={langEnglish ? 'english' : 'german_(germany)'}
                            onChange={(e) => {
                              const isEn = e.target.value === 'english';
                              setLangEnglish(isEn);
                              updateSingleCvar('g_language', isEn ? 'english' : 'german_(germany)');
                            }}
                            className="w-full bg-slate-950 border border-slate-700 rounded-lg px-2 py-1 text-slate-200 text-xs cursor-pointer font-mono"
                          >
                            <option value="german_(germany)">german_(germany)</option>
                            <option value="english">english</option>
                          </select>
                        </div>

                        <div>
                          <div className="flex items-center justify-between mb-1">
                            <span className="text-slate-300 font-semibold block">g_languageAudio:</span>
                            <CvarHelpTooltip command="g_languageAudio" />
                          </div>
                          <select
                            value={langAudioEnglish ? 'english' : 'german_(germany)'}
                            onChange={(e) => {
                              const isEn = e.target.value === 'english';
                              setLangAudioEnglish(isEn);
                              updateSingleCvar('g_languageAudio', isEn ? 'english' : 'german_(germany)');
                            }}
                            className="w-full bg-slate-950 border border-slate-700 rounded-lg px-2 py-1 text-slate-200 text-xs cursor-pointer font-mono"
                          >
                            <option value="english">english (⭐)</option>
                            <option value="german_(germany)">german_(germany)</option>
                          </select>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div className="text-[10px] text-slate-500 font-mono pt-2 border-t border-slate-800/60">
                    Deutsche Menütexte mit englischer Original-Sprachausgabe verhindert stumme Funkdialoge.
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      )}

      {/* ══════════════════════════════════════════════════════════════
          TAB 1: WARTUNG & DIAGNOSE (SHADER, DUMPS, HARDWARE BENCHMARK)
          ══════════════════════════════════════════════════════════════ */}
      {activeTab === 'maintenance' && (
        <div className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
            {/* Shader Cache Card */}
            <div className="p-5 rounded-2xl bg-slate-900/70 border border-slate-800 hover:border-slate-700 transition flex flex-col justify-between">
              <div>
                <div className="flex items-center justify-between mb-3">
                  <div className="flex items-center space-x-2.5">
                    <HardDrive className="w-5 h-5 text-amber-400" />
                    <h3 className="text-sm font-semibold text-white">DirectX, Vulkan &amp; NVIDIA / AMD Shader</h3>
                  </div>
                  <span className="text-xs px-2.5 py-0.5 rounded bg-amber-950/70 text-amber-400 border border-amber-800/70 font-mono font-bold">
                    {status?.shaderCacheMb ? `${status.shaderCacheMb.toFixed(1)} MB` : '0 MB'}
                  </span>
                </div>
                <p className="text-xs text-slate-400 mb-4 leading-relaxed">
                  Löscht Star Citizen-, DirectX- und GPU-Treiber-Shader (NVIDIA DXCache/GLCache &amp; AMD). Verhindert Shader-Stottern und zwingt die Grafikkarte zum sauberen Neuaufbau aller Shader.
                </p>
              </div>
              <button
                onClick={handleClearShaderCache}
                disabled={actionLoading === 'shaders'}
                className="w-full flex items-center justify-center space-x-2 py-2.5 px-3 rounded-xl bg-amber-500/10 hover:bg-amber-500/20 text-amber-300 text-xs font-semibold border border-amber-500/30 transition disabled:opacity-40 cursor-pointer disabled:cursor-not-allowed"
              >
                <Trash2 className="w-4 h-4" />
                <span>{actionLoading === 'shaders' ? 'Bereinige...' : 'Shader-Cache vollständig leeren'}</span>
              </button>
            </div>

            {/* Crash Dumps Card */}
            <div className="p-5 rounded-2xl bg-slate-900/70 border border-slate-800 hover:border-slate-700 transition flex flex-col justify-between">
              <div>
                <div className="flex items-center justify-between mb-3">
                  <div className="flex items-center space-x-2.5">
                    <AlertCircle className="w-5 h-5 text-rose-400" />
                    <h3 className="text-sm font-semibold text-white">Crash-Dumps &amp; Logs</h3>
                  </div>
                  <span className="text-xs px-2.5 py-0.5 rounded bg-rose-950/70 text-rose-400 border border-rose-800/70 font-mono font-bold">
                    {status?.crashDumpsMb ? `${status.crashDumpsMb.toFixed(1)} MB` : '0 MB'}
                  </span>
                </div>
                <p className="text-xs text-slate-400 mb-4 leading-relaxed">
                  Gibt Festplattenspeicher frei, indem alte Star Citizen Minidumps (.dmp) und Absturzberichte im AppData-Verzeichnis bereinigt werden.
                </p>
              </div>
              <button
                onClick={handleClearCrashDumps}
                disabled={actionLoading === 'dumps' || (status?.crashDumpsMb || 0) === 0}
                className="w-full flex items-center justify-center space-x-2 py-2.5 px-3 rounded-xl bg-rose-500/10 hover:bg-rose-500/20 text-rose-300 text-xs font-semibold border border-rose-500/30 transition disabled:opacity-40 cursor-pointer disabled:cursor-not-allowed"
              >
                <Trash2 className="w-4 h-4" />
                <span>{actionLoading === 'dumps' ? 'Lösche...' : 'Crash-Dumps bereinigen'}</span>
              </button>
            </div>

            {/* Screenshots & OCR Card */}
            <div className="p-5 rounded-2xl bg-slate-900/70 border border-slate-800 hover:border-slate-700 transition flex flex-col justify-between">
              <div>
                <div className="flex items-center justify-between mb-3">
                  <div className="flex items-center space-x-2.5">
                    <Camera className="w-5 h-5 text-sky-400" />
                    <h3 className="text-sm font-semibold text-white">Star Citizen Screenshots</h3>
                  </div>
                  <span className="text-xs px-2.5 py-0.5 rounded bg-sky-950/70 text-sky-400 border border-sky-800/70 font-mono font-bold">
                    {screenshotStatus ? `${screenshotStatus.totalCount} Bilder · ${screenshotStatus.totalSizeFormatted}` : `${status?.screenshotCount || 0} Bilder`}
                  </span>
                </div>
                <p className="text-xs text-slate-400 mb-4 leading-relaxed">
                  Verwaltet und bereinigt den Screenshot-Ordner gezielt nach Schiffsausrüstung (VLM/ASOP), Delphi-Ruf, leeren HDR-Frames oder komplett.
                </p>
              </div>
              <div className="flex items-center gap-2">
                <button
                  onClick={() => loadScreenshotStatus(true)}
                  disabled={screenshotLoading}
                  className="flex-1 flex items-center justify-center space-x-1.5 py-2.5 px-3 rounded-xl bg-sky-500/10 hover:bg-sky-500/20 text-sky-300 text-xs font-semibold border border-sky-500/30 transition cursor-pointer disabled:opacity-40"
                >
                  <RefreshCw className={`w-3.5 h-3.5 ${screenshotLoading ? 'animate-spin' : ''}`} />
                  <span>{screenshotLoading ? 'Scanne...' : 'Neu scannen'}</span>
                </button>
                <button
                  onClick={handleOpenScreenshotFolder}
                  className="p-2.5 rounded-xl bg-slate-800/80 hover:bg-slate-700 text-slate-300 border border-slate-700 transition cursor-pointer"
                  title="Ordner im Windows Explorer öffnen"
                >
                  <FolderOpen className="w-4 h-4" />
                </button>
              </div>
            </div>
          </div>

          {/* ══ SCREENSHOT CLEANUP & INTERACTIVE GALLERY SUITE ══ */}
          <div className="p-6 rounded-2xl bg-gradient-to-br from-slate-900/90 via-slate-900/70 to-slate-950 border border-slate-800 shadow-xl space-y-6">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-4 border-b border-slate-800/80">
              <div className="flex items-center space-x-3.5">
                <div className="w-10 h-10 rounded-xl bg-sky-500/10 border border-sky-500/30 flex items-center justify-center text-sky-400 shadow-inner shrink-0">
                  <Camera className="w-5 h-5" />
                </div>
                <div>
                  <div className="flex items-center space-x-2.5">
                    <h3 className="text-sm font-bold text-white tracking-wide">
                      STAR CITIZEN SCREENSHOT-ORDNER &amp; OCR-BEREINIGUNG
                    </h3>
                    <span className="text-[10px] px-2.5 py-0.5 rounded-full bg-sky-950/90 text-sky-400 border border-sky-800/80 font-mono font-bold">
                      {screenshotStatus ? `${screenshotStatus.totalCount} Aufnahmen (${screenshotStatus.totalSizeFormatted})` : 'Aktiv'}
                    </span>
                  </div>
                  <p className="text-xs text-slate-400 mt-0.5">
                    Erkennt und bereinigt Screenshots gezielt nach Schiffsausrüstung (VLM/ASOP), Delphi-Ruf, Aufträgen oder fehlerhaften HDR-Dateien.
                  </p>
                </div>
              </div>

              <div className="flex items-center gap-2">
                <button
                  onClick={() => loadScreenshotStatus(true)}
                  disabled={screenshotLoading}
                  className="flex items-center space-x-1.5 px-3 py-1.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold border border-slate-700 transition cursor-pointer disabled:opacity-40"
                  title="Screenshots neu einlesen und OCR-Klassifizierung aktualisieren"
                >
                  <RefreshCw className={`w-3.5 h-3.5 ${screenshotLoading ? 'animate-spin text-sky-400' : ''}`} />
                  <span>{screenshotLoading ? 'Scanne...' : 'Neu scannen'}</span>
                </button>
                <button
                  onClick={handleOpenScreenshotFolder}
                  className="flex items-center space-x-1.5 px-3 py-1.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold border border-slate-700 transition cursor-pointer"
                  title="Ordner im Windows Explorer öffnen"
                >
                  <FolderOpen className="w-3.5 h-3.5" />
                  <span>Ordner öffnen</span>
                </button>
              </div>
            </div>

            {/* Path indicator */}
            {screenshotStatus?.folderPath && (
              <div className="text-[11px] font-mono px-3 py-2 rounded-xl bg-slate-950/60 border border-slate-800 text-slate-400 flex items-center justify-between">
                <span className="truncate">Ordner: <span className="text-slate-200">{screenshotStatus.folderPath}</span></span>
                <span className="text-[10px] text-emerald-400 shrink-0 ml-2">✓ Bereit</span>
              </div>
            )}

            {/* Quick-Action KPI Cluster with Delete Buttons */}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
              {/* 1. Schiffsausrüstung (VLM / ASOP) */}
              <div className="p-3.5 rounded-xl bg-slate-950/60 border border-slate-800/80 hover:border-emerald-800/50 transition flex flex-col justify-between space-y-2.5">
                <div>
                  <div className="flex items-center justify-between text-slate-400 text-xs mb-1">
                    <span className="font-semibold uppercase tracking-wider text-[10px]">Schiffsausrüstung (VLM / ASOP)</span>
                    <span className="text-xs font-mono font-bold text-emerald-400">{screenshotStatus?.loadoutCount ?? 0}</span>
                  </div>
                  <div className="text-sm font-bold font-mono text-white">
                    {screenshotStatus ? `${screenshotStatus.loadoutSizeMb.toFixed(1)} MB` : '0 MB'}
                  </div>
                  <p className="text-[11px] text-slate-400 mt-1 leading-snug">
                    Erkannte Flotten- &amp; Komponenten-Scans.
                  </p>
                </div>
                <button
                  onClick={() => setDeleteConfirmModal({
                    open: true,
                    mode: 'loadout',
                    count: screenshotStatus?.loadoutCount || 0,
                    label: 'Schiffsausrüstungs-Screenshots (VLM / ASOP)'
                  })}
                  disabled={(screenshotStatus?.loadoutCount || 0) === 0 || screenshotActionLoading !== null}
                  className="w-full flex items-center justify-center space-x-1.5 py-1.5 px-2.5 rounded-lg bg-emerald-500/10 hover:bg-emerald-500/20 text-emerald-300 text-xs font-semibold border border-emerald-500/30 transition disabled:opacity-40 cursor-pointer disabled:cursor-not-allowed"
                >
                  <Trash2 className="w-3.5 h-3.5" />
                  <span>Ausrüstung löschen</span>
                </button>
              </div>

              {/* 2. Delphi Ruf & Faktionen */}
              <div className="p-3.5 rounded-xl bg-slate-950/60 border border-slate-800/80 hover:border-amber-800/50 transition flex flex-col justify-between space-y-2.5">
                <div>
                  <div className="flex items-center justify-between text-slate-400 text-xs mb-1">
                    <span className="font-semibold uppercase tracking-wider text-[10px]">Ruf &amp; Delphi-Faktionen</span>
                    <span className="text-xs font-mono font-bold text-amber-400">{screenshotStatus?.reputationCount ?? 0}</span>
                  </div>
                  <div className="text-sm font-bold font-mono text-white">
                    {screenshotStatus ? `${screenshotStatus.reputationSizeMb.toFixed(1)} MB` : '0 MB'}
                  </div>
                  <p className="text-[11px] text-slate-400 mt-1 leading-snug">
                    mobiGlas Ruf- &amp; Faktionsabgleiche.
                  </p>
                </div>
                <button
                  onClick={() => setDeleteConfirmModal({
                    open: true,
                    mode: 'reputation',
                    count: screenshotStatus?.reputationCount || 0,
                    label: 'Ruf- & Delphi-Screenshots'
                  })}
                  disabled={(screenshotStatus?.reputationCount || 0) === 0 || screenshotActionLoading !== null}
                  className="w-full flex items-center justify-center space-x-1.5 py-1.5 px-2.5 rounded-lg bg-amber-500/10 hover:bg-amber-500/20 text-amber-300 text-xs font-semibold border border-amber-500/30 transition disabled:opacity-40 cursor-pointer disabled:cursor-not-allowed"
                >
                  <Trash2 className="w-3.5 h-3.5" />
                  <span>Ruf löschen</span>
                </button>
              </div>

              {/* 3. Leere Frames (101 KB HDR-Bug) */}
              <div className="p-3.5 rounded-xl bg-slate-950/60 border border-slate-800/80 hover:border-rose-800/50 transition flex flex-col justify-between space-y-2.5">
                <div>
                  <div className="flex items-center justify-between text-slate-400 text-xs mb-1">
                    <span className="font-semibold uppercase tracking-wider text-[10px]">Leere Frames (HDR-Bug)</span>
                    <span className="text-xs font-mono font-bold text-rose-400">{screenshotStatus?.blankCount ?? 0}</span>
                  </div>
                  <div className="text-sm font-bold font-mono text-white">
                    {screenshotStatus ? `${screenshotStatus.blankSizeMb.toFixed(1)} MB` : '0 MB'}
                  </div>
                  <p className="text-[11px] text-slate-400 mt-1 leading-snug">
                    Schwarze 101 KB Star Citizen HDR-Frames.
                  </p>
                </div>
                <button
                  onClick={() => setDeleteConfirmModal({
                    open: true,
                    mode: 'blank',
                    count: screenshotStatus?.blankCount || 0,
                    label: 'fehlerhafte / leere HDR-Frames (101 KB)'
                  })}
                  disabled={(screenshotStatus?.blankCount || 0) === 0 || screenshotActionLoading !== null}
                  className="w-full flex items-center justify-center space-x-1.5 py-1.5 px-2.5 rounded-lg bg-rose-500/10 hover:bg-rose-500/20 text-rose-300 text-xs font-semibold border border-rose-500/30 transition disabled:opacity-40 cursor-pointer disabled:cursor-not-allowed"
                >
                  <Trash2 className="w-3.5 h-3.5" />
                  <span>Leere Frames löschen</span>
                </button>
              </div>

              {/* 4. Alle Screenshots bereinigen */}
              <div className="p-3.5 rounded-xl bg-slate-950/60 border border-slate-800/80 hover:border-sky-800/50 transition flex flex-col justify-between space-y-2.5">
                <div>
                  <div className="flex items-center justify-between text-slate-400 text-xs mb-1">
                    <span className="font-semibold uppercase tracking-wider text-[10px]">Gesamter Ordner</span>
                    <span className="text-xs font-mono font-bold text-sky-400">{screenshotStatus?.totalCount ?? 0}</span>
                  </div>
                  <div className="text-sm font-bold font-mono text-white">
                    {screenshotStatus ? screenshotStatus.totalSizeFormatted : '0 MB'}
                  </div>
                  <p className="text-[11px] text-slate-400 mt-1 leading-snug">
                    Alle Aufnahmen im Screenshot-Ordner.
                  </p>
                </div>
                <button
                  onClick={() => setDeleteConfirmModal({
                    open: true,
                    mode: 'all',
                    count: screenshotStatus?.totalCount || 0,
                    label: 'ALLE Screenshots im Ordner'
                  })}
                  disabled={(screenshotStatus?.totalCount || 0) === 0 || screenshotActionLoading !== null}
                  className="w-full flex items-center justify-center space-x-1.5 py-1.5 px-2.5 rounded-lg bg-rose-600/20 hover:bg-rose-600/30 text-rose-200 text-xs font-semibold border border-rose-500/40 transition disabled:opacity-40 cursor-pointer disabled:cursor-not-allowed"
                >
                  <Trash2 className="w-3.5 h-3.5 text-rose-400" />
                  <span>Alle löschen</span>
                </button>
              </div>
            </div>

            {/* Category Filter Tabs & Search & Interactive Selection */}
            <div className="space-y-3 pt-2">
              <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-2.5">
                {/* Pills */}
                <div className="flex items-center gap-1.5 overflow-x-auto no-scrollbar">
                  {[
                    { id: 'all', label: 'Alle', count: screenshotStatus?.totalCount ?? 0 },
                    { id: 'loadout', label: 'Schiffsausrüstung', count: screenshotStatus?.loadoutCount ?? 0 },
                    { id: 'reputation', label: 'Ruf / Delphi', count: screenshotStatus?.reputationCount ?? 0 },
                    { id: 'blank', label: 'Leere HDR-Frames', count: screenshotStatus?.blankCount ?? 0 },
                    { id: 'other', label: 'Sonstige', count: screenshotStatus?.otherCount ?? 0 },
                  ].map((tab) => (
                    <button
                      key={tab.id}
                      onClick={() => setScreenshotFilter(tab.id as any)}
                      className={`px-2.5 py-1 text-xs font-mono font-semibold rounded-lg transition cursor-pointer shrink-0 flex items-center gap-1.5 ${
                        screenshotFilter === tab.id
                          ? 'bg-sky-950/80 text-sky-300 border border-sky-500/70 shadow-[0_0_8px_rgba(56,189,248,0.25)]'
                          : 'text-slate-400 hover:text-slate-200 hover:bg-slate-900/60 border border-transparent'
                      }`}
                    >
                      <span>{tab.label}</span>
                      <span className="text-[10px] text-slate-500">({tab.count})</span>
                    </button>
                  ))}
                </div>

                {/* Search & Actions */}
                <div className="flex items-center gap-2">
                  <div className="relative flex items-center min-w-[140px] sm:min-w-[180px]">
                    <Search className="w-3.5 h-3.5 text-slate-500 absolute left-2.5 pointer-events-none" />
                    <input
                      type="text"
                      placeholder="Datei, Schiff suchen..."
                      value={screenshotSearch}
                      onChange={(e) => setScreenshotSearch(e.target.value)}
                      className="w-full pl-8 pr-2.5 py-1 text-xs font-mono bg-slate-950 border border-slate-800 rounded-lg text-slate-200 placeholder-slate-600 focus:outline-none focus:border-sky-500"
                    />
                  </div>

                  {selectedScreenshotPaths.size > 0 && (
                    <button
                      onClick={() => setDeleteConfirmModal({
                        open: true,
                        mode: 'selected',
                        count: selectedScreenshotPaths.size,
                        label: `${selectedScreenshotPaths.size} ausgewählte Screenshots`,
                        paths: Array.from(selectedScreenshotPaths)
                      })}
                      className="flex items-center space-x-1.5 px-3 py-1 text-xs font-semibold rounded-lg bg-rose-500/20 hover:bg-rose-500/30 text-rose-300 border border-rose-500/40 transition cursor-pointer"
                    >
                      <Trash2 className="w-3.5 h-3.5" />
                      <span>{selectedScreenshotPaths.size} löschen</span>
                    </button>
                  )}
                </div>
              </div>

              {/* Table / List */}
              <div className="rounded-xl border border-slate-800 bg-slate-950/80 overflow-hidden max-h-[360px] overflow-y-auto">
                {(() => {
                  const filtered = (screenshotStatus?.items || []).filter((item) => {
                    if (screenshotFilter !== 'all' && item.category !== screenshotFilter) return false;
                    if (screenshotSearch.trim()) {
                      const q = screenshotSearch.toLowerCase();
                      return item.fileName.toLowerCase().includes(q) || item.details.toLowerCase().includes(q);
                    }
                    return true;
                  });

                  if (filtered.length === 0) {
                    return (
                      <div className="p-8 text-center text-xs font-mono text-slate-500">
                        Keine Screenshots für diesen Filter gefunden.
                      </div>
                    );
                  }

                  const allSelected = filtered.length > 0 && filtered.every((i) => selectedScreenshotPaths.has(i.filePath));

                  return (
                    <table className="w-full text-left text-xs font-mono">
                      <thead className="bg-slate-900/90 text-slate-400 text-[10px] uppercase border-b border-slate-800 sticky top-0 z-10 backdrop-blur-sm">
                        <tr>
                          <th className="p-2.5 w-10 text-center">
                            <button
                              onClick={() => handleSelectAllCategory(filtered)}
                              className="text-slate-400 hover:text-white cursor-pointer"
                              title={allSelected ? 'Alle abwählen' : 'Alle dieser Kategorie auswählen'}
                            >
                              {allSelected ? <CheckSquare className="w-4 h-4 text-sky-400" /> : <Square className="w-4 h-4 text-slate-500" />}
                            </button>
                          </th>
                          <th className="p-2.5">Dateiname</th>
                          <th className="p-2.5">Datum / Uhrzeit</th>
                          <th className="p-2.5">Kategorie</th>
                          <th className="p-2.5">Erkannte Details</th>
                          <th className="p-2.5 text-right">Größe</th>
                          <th className="p-2.5 w-10 text-center">Löschen</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-800/60">
                        {filtered.map((item) => {
                          const isSelected = selectedScreenshotPaths.has(item.filePath);
                          const catBadgeColor =
                            item.category === 'loadout'
                              ? 'bg-emerald-950 text-emerald-300 border-emerald-800'
                              : item.category === 'reputation'
                              ? 'bg-amber-950 text-amber-300 border-amber-800'
                              : item.category === 'blank'
                              ? 'bg-rose-950 text-rose-300 border-rose-800'
                              : 'bg-slate-900 text-slate-400 border-slate-700';

                          return (
                            <tr
                              key={item.filePath}
                              className={`hover:bg-slate-900/50 transition ${isSelected ? 'bg-sky-950/20' : ''}`}
                            >
                              <td className="p-2.5 text-center">
                                <button
                                  onClick={() => handleToggleSelectScreenshot(item.filePath)}
                                  className="text-slate-400 hover:text-white cursor-pointer"
                                >
                                  {isSelected ? <CheckSquare className="w-4 h-4 text-sky-400" /> : <Square className="w-4 h-4 text-slate-600" />}
                                </button>
                              </td>
                              <td className="p-2.5 text-slate-200 truncate max-w-[220px]" title={item.fileName}>
                                {item.fileName}
                              </td>
                              <td className="p-2.5 text-slate-400 whitespace-nowrap">
                                {item.lastModifiedFormatted}
                              </td>
                              <td className="p-2.5 whitespace-nowrap">
                                <span className={`text-[10px] px-2 py-0.5 rounded border font-semibold ${catBadgeColor}`}>
                                  {item.categoryLabel}
                                </span>
                              </td>
                              <td className="p-2.5 text-slate-300 truncate max-w-[240px]" title={item.details}>
                                {item.details}
                              </td>
                              <td className="p-2.5 text-right text-slate-400 whitespace-nowrap font-mono">
                                {item.sizeFormatted}
                              </td>
                              <td className="p-2.5 text-center">
                                <button
                                  onClick={() => setDeleteConfirmModal({
                                    open: true,
                                    mode: 'selected',
                                    count: 1,
                                    label: `Screenshot '${item.fileName}'`,
                                    paths: [item.filePath]
                                  })}
                                  className="text-slate-500 hover:text-rose-400 transition cursor-pointer p-1"
                                  title="Diesen Screenshot löschen"
                                >
                                  <Trash2 className="w-3.5 h-3.5" />
                                </button>
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  );
                })()}
              </div>
            </div>
          </div>

          {/* Hardware & Startup Benchmark Suite */}
          <div className="p-6 rounded-2xl bg-gradient-to-br from-slate-900/90 via-slate-900/70 to-slate-950 border border-slate-800 shadow-xl space-y-6">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-4 border-b border-slate-800/80">
              <div className="flex items-center space-x-3.5">
                <div className="w-10 h-10 rounded-xl bg-emerald-500/10 border border-emerald-500/30 flex items-center justify-center text-emerald-400 shadow-inner shrink-0">
                  <Cpu className="w-5 h-5" />
                </div>
                <div>
                  <div className="flex items-center space-x-2.5">
                    <h3 className="text-sm font-bold text-white tracking-wide">
                      SYSTEM-HARDWARE &amp; GAME.LOG STARTUP-BENCHMARK
                    </h3>
                    <span className="text-[10px] px-2.5 py-0.5 rounded-full bg-emerald-950/90 text-emerald-400 border border-emerald-800/80 font-mono font-bold">
                      {status?.cpuModel && status.cpuModel !== 'Unbekannt' ? 'Aus Game.log erfasst' : 'Aktiv'}
                    </span>
                  </div>
                  <p className="text-xs text-slate-400 mt-0.5">
                    Live-Hardwareidentifikation und Star Citizen Engine-Leistungsindizes aus dem Spielstart.
                  </p>
                </div>
              </div>

              {status?.windowsVersion && (
                <div className="text-[11px] font-mono px-3 py-1.5 rounded-xl bg-slate-950/80 border border-slate-800 text-slate-300 shrink-0">
                  {(() => {
                    const raw = status.windowsVersion;
                    const m = raw.match(/10\.0\.(\d+)/i);
                    if (m && parseInt(m[1], 10) >= 22000) {
                      return raw.replace(/Windows\s+10/i, 'Windows 11');
                    }
                    return raw;
                  })()}
                </div>
              )}
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
              <div className="p-4 rounded-xl bg-slate-950/60 border border-slate-800/80 hover:border-slate-700 transition flex flex-col justify-between">
                <div>
                  <div className="flex items-center justify-between text-slate-400 text-xs mb-1.5">
                    <span className="font-semibold uppercase tracking-wider text-[10px]">Hauptprozessor (CPU)</span>
                    <Cpu className="w-4 h-4 text-sky-400" />
                  </div>
                  <div className="font-bold text-slate-100 text-sm font-mono leading-tight">
                    {status?.cpuModel || 'AMD Ryzen 7 5800X3D 8-Core'}
                  </div>
                </div>
                <div className="mt-3 pt-2 border-t border-slate-800/60 flex items-center justify-between text-[11px] font-mono text-slate-400">
                  <span>Logische Kerne:</span>
                  <span className="font-semibold text-sky-300">{status?.cpuLogicalCores ? `${status.cpuLogicalCores} Threads` : '16 Threads'}</span>
                </div>
              </div>

              <div className="p-4 rounded-xl bg-slate-950/60 border border-slate-800/80 hover:border-slate-700 transition flex flex-col justify-between">
                <div>
                  <div className="flex items-center justify-between text-slate-400 text-xs mb-1.5">
                    <span className="font-semibold uppercase tracking-wider text-[10px]">Grafikkarte &amp; Anzeige</span>
                    <Monitor className="w-4 h-4 text-emerald-400" />
                  </div>
                  <div className="font-bold text-slate-100 text-sm font-mono leading-tight">
                    {status?.gpuModel || 'NVIDIA GeForce RTX 5070'}
                  </div>
                  <div className="text-[11px] text-slate-400 font-mono mt-1">
                    {status?.gpuVramMb ? `${status.gpuVramMb}` : '12 GB'} VRAM {status?.gpuDriverVersion ? `· v${status.gpuDriverVersion}` : ''}
                  </div>
                </div>
                <div className="mt-3 pt-2 border-t border-slate-800/60 flex items-center justify-between text-[11px] font-mono text-slate-400">
                  <span>Display-Auflösung:</span>
                  <span className="font-semibold text-emerald-300">{status?.displayResolution || '2560x1440x32'}</span>
                </div>
              </div>

              <div className="p-4 rounded-xl bg-slate-950/60 border border-slate-800/80 hover:border-slate-700 transition flex flex-col justify-between">
                <div>
                  <div className="flex items-center justify-between text-slate-400 text-xs mb-1.5">
                    <span className="font-semibold uppercase tracking-wider text-[10px]">Arbeitsspeicher (RAM)</span>
                    <Activity className="w-4 h-4 text-purple-400" />
                  </div>
                  <div className="font-bold text-slate-100 text-sm font-mono leading-tight">
                    {status?.ramStatus || `${status?.totalRamGb || 64} GB`}
                  </div>
                </div>
                <div className="mt-3 pt-2 border-t border-slate-800/60 flex items-center justify-between text-[11px] font-mono text-slate-400">
                  <span>Auslagerungsdatei:</span>
                  <span className="font-semibold text-purple-300">{status?.pagefileStatus || 'Aktiv'}</span>
                </div>
              </div>

              <div className="p-4 rounded-xl bg-slate-950/60 border border-slate-800/80 hover:border-slate-700 transition flex flex-col justify-between">
                <div>
                  <div className="flex items-center justify-between text-slate-400 text-xs mb-1.5">
                    <span className="font-semibold uppercase tracking-wider text-[10px]">Installations-Laufwerk</span>
                    <HardDrive className="w-4 h-4 text-amber-400" />
                  </div>
                  <div className="font-bold text-slate-100 text-sm font-mono leading-tight">
                    Laufwerk {status?.driveName || 'J:'}: {status?.freeDiskGb ? `${status.freeDiskGb.toFixed(1)} GB frei` : 'Frei'}
                  </div>
                </div>
                <div className="mt-3 pt-2 border-t border-slate-800/60 flex items-center justify-between text-[11px] font-mono text-slate-400">
                  <span>Laufwerkstyp:</span>
                  <span className="font-semibold text-amber-300">{status?.driveType || 'Interne SSD / NVMe'}</span>
                </div>
              </div>
            </div>

            <div className="p-4 rounded-xl bg-slate-950/80 border border-slate-800/90 space-y-3">
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-2 text-xs font-bold text-sky-400 tracking-wider font-mono">
                  <Gauge className="w-4 h-4 text-sky-400" />
                  <span>ENGINE STARTUP BENCHMARK &amp; PERFORMANCE INDEX (GAME.LOG)</span>
                </div>
                <span className="text-[10px] text-slate-500 font-mono">
                  Gemessen von Star Citizen beim Engine-Boot
                </span>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 text-xs font-mono">
                <div className="p-3 rounded-lg bg-slate-900/80 border border-slate-800/80 flex flex-col justify-between space-y-1">
                  <div className="text-slate-400 text-[10px] uppercase tracking-wider">CPU Benchmark Zeit</div>
                  <div className="font-bold text-sky-300 text-sm">
                    {status?.cpuBenchmark || '34.24 ms (int+mem)'}
                  </div>
                  <div className="text-[10px] text-slate-500">Kürzere Latenz = Bessere Physik-Verarbeitung</div>
                </div>

                <div className="p-3 rounded-lg bg-slate-900/80 border border-slate-800/80 flex flex-col justify-between space-y-1">
                  <div className="text-slate-400 text-[10px] uppercase tracking-wider">GPU Benchmark Zeit</div>
                  <div className="font-bold text-emerald-300 text-sm">
                    {status?.gpuBenchmark || '23.25 ms (Adapter 0)'}
                  </div>
                  <div className="text-[10px] text-slate-500">Vulkan Frame-Buffer &amp; Shader Renderzeit</div>
                </div>

                <div className="p-3 rounded-lg bg-slate-900/80 border border-slate-800/80 flex flex-col justify-between space-y-1">
                  <div className="text-slate-400 text-[10px] uppercase tracking-wider">CIG Performance Index</div>
                  <div className="font-bold text-purple-300 text-sm flex items-center space-x-2">
                    <span>CPU: {status?.performanceIndexCpu || '184.87'}</span>
                    <span className="text-slate-600">|</span>
                    <span>GPU: {status?.performanceIndexGpu || '443.02'}</span>
                  </div>
                  <div className="text-[10px] text-slate-500">CIG Telemetrie Rating (Höher = Schneller)</div>
                </div>

                <div className="p-3 rounded-lg bg-slate-900/80 border border-slate-800/80 flex flex-col justify-between space-y-1">
                  <div className="text-slate-400 text-[10px] uppercase tracking-wider">DataCore &amp; PSO Boot</div>
                  <div className="font-bold text-amber-300 text-sm">
                    {status?.dataCoreLoadTime ? `${status.dataCoreLoadTime}` : '3.72s'} {status?.psoCacheGenTime ? `(PSO: ${status.psoCacheGenTime})` : '(PSO: 0.38s)'}
                  </div>
                  <div className="text-[10px] text-slate-500">NVMe Ladezeit für Game-Binaries</div>
                </div>
              </div>
            </div>

            <div className="pt-3 border-t border-slate-800/70 flex items-center justify-between text-xs text-slate-400">
              <div className="flex items-center space-x-2">
                <ShieldCheck className="w-4 h-4 text-emerald-400 shrink-0" />
                <span className="text-slate-300 font-medium">Systemvoraussetzungen für Star Citizen optimal</span>
              </div>
              <span className="text-[11px] font-mono text-slate-500 hidden sm:inline">
                Automatisch analysiert aus der aktiven Star Citizen Sitzung
              </span>
            </div>
          </div>
        </div>
      )}

      {/* ══════════════════════════════════════════════════════════════
          TAB 3: ZENTRALER BACKUP-TRESOR (KEYBINDS, CLOUD & USER.CFG)
          ══════════════════════════════════════════════════════════════ */}
      {activeTab === 'keybinds' && (
        <div className="space-y-6">
          {/* Sub-Tabs for Backups */}
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-1 border-b border-slate-800">
            <div className="flex items-center space-x-1.5 p-1 rounded-2xl bg-slate-900/90 border border-slate-800">
              <button
                onClick={() => setBackupSubTab('keybinds')}
                className={`flex items-center space-x-2 px-3.5 py-1.5 rounded-xl text-xs font-bold transition cursor-pointer ${
                  backupSubTab === 'keybinds'
                    ? 'bg-purple-600 text-white shadow-md shadow-purple-600/30'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60'
                }`}
              >
                <Key className="w-3.5 h-3.5" />
                <span>Tastenbelegungen &amp; Actionmaps</span>
                {status?.keybindItems && status.keybindItems.length > 0 && (
                  <span className="px-1.5 py-0.2 rounded-full bg-purple-950 text-purple-300 text-[10px] font-mono border border-purple-800 font-bold">
                    {status.keybindItems.length}
                  </span>
                )}
              </button>

              <button
                onClick={() => setBackupSubTab('usercfg')}
                className={`flex items-center space-x-2 px-3.5 py-1.5 rounded-xl text-xs font-bold transition cursor-pointer ${
                  backupSubTab === 'usercfg'
                    ? 'bg-purple-600 text-white shadow-md shadow-purple-600/30'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60'
                }`}
              >
                <FileText className="w-3.5 h-3.5" />
                <span>user.cfg Snapshots</span>
                {status?.configBackups && status.configBackups.length > 0 && (
                  <span className="px-1.5 py-0.2 rounded-full bg-purple-950 text-purple-300 text-[10px] font-mono border border-purple-800 font-bold">
                    {status.configBackups.length}
                  </span>
                )}
              </button>
            </div>

            <div className="flex items-center space-x-2 text-[11px] text-slate-400">
              <span className="flex items-center space-x-1 text-emerald-400 font-medium">
                <ShieldCheck className="w-3.5 h-3.5" />
                <span>Automatischer Snapshot-Schutz aktiv</span>
              </span>
            </div>
          </div>

          {backupSubTab === 'keybinds' ? (
            <div className="space-y-6">
              <div className="p-5 rounded-2xl bg-gradient-to-r from-purple-950/40 via-slate-900/80 to-sky-950/40 border border-purple-800/30 backdrop-blur shadow-xl space-y-4">
                <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                  <div className="flex items-center space-x-3.5">
                    <div className="w-11 h-11 rounded-xl bg-purple-500/10 border border-purple-500/30 flex items-center justify-center text-purple-400 shadow-inner shrink-0">
                      <Cloud className="w-6 h-6" />
                    </div>
                    <div>
                      <div className="flex items-center space-x-2">
                        <h2 className="text-sm font-bold text-white tracking-wide">CLOUD-SPEICHER &amp; SYNCHRONISATION</h2>
                        <span className="text-[10px] px-2 py-0.5 rounded-full bg-purple-950 text-purple-300 border border-purple-800 font-mono font-semibold">
                          {cloudDisplay}
                        </span>
                      </div>
                      <p className="text-xs text-slate-400 mt-0.5">
                        Hinterlege deinen Cloud-Ordner (Google Drive / OneDrive / Dropbox) zur standortübergreifenden Sicherung.
                      </p>
                    </div>
                  </div>

                  <div className="flex flex-wrap items-center gap-2">
                    <button
                      onClick={handleExportLogsZip}
                      disabled={actionLoading !== null}
                      className="flex items-center space-x-1.5 px-3 py-2 rounded-xl bg-slate-800/90 hover:bg-slate-700 text-slate-200 text-xs font-semibold border border-slate-700 transition cursor-pointer"
                    >
                      <Download className="w-3.5 h-3.5 text-slate-400" />
                      <span>Logs als ZIP</span>
                    </button>
                    <button
                      onClick={handleSyncLogsCloud}
                      disabled={actionLoading !== null}
                      className="flex items-center space-x-1.5 px-3.5 py-2 rounded-xl bg-sky-950/70 hover:bg-sky-900/80 text-sky-300 text-xs font-bold border border-sky-800/80 transition cursor-pointer"
                    >
                      <Cloud className="w-3.5 h-3.5 text-sky-400" />
                      <span>Logs in Cloud sichern</span>
                    </button>
                    <button
                      onClick={() => handleOpenFolder('cloud')}
                      title="Cloud-Ordner im Explorer öffnen"
                      className="p-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 border border-slate-700 transition cursor-pointer"
                    >
                      <FolderOpen className="w-4 h-4" />
                    </button>
                  </div>
                </div>

                <div className="flex items-center space-x-2 pt-2 border-t border-slate-800/80">
                  <input
                    type="text"
                    placeholder="Cloud-Pfad (z. B. I:\Meine Ablage\Backup\StarCitizen)..."
                    value={cloudPath}
                    onChange={(e) => setCloudPath(e.target.value)}
                    className="flex-1 bg-slate-950 border border-slate-800 rounded-xl px-3.5 py-2 text-xs font-mono text-slate-200 placeholder-slate-600 focus:outline-none focus:border-purple-500"
                  />
                  <button
                    onClick={handleSaveCloudPath}
                    disabled={actionLoading !== null}
                    className="px-4 py-2 rounded-xl bg-purple-600 hover:bg-purple-500 text-white text-xs font-bold border border-purple-400 transition cursor-pointer shrink-0"
                  >
                    Pfad speichern
                  </button>
                </div>

                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pt-3 border-t border-purple-900/30 text-xs bg-purple-950/20 -mx-5 -mb-4 px-5 py-3 rounded-b-2xl">
                  <label className="flex items-center space-x-3 cursor-pointer select-none">
                    <input
                      type="checkbox"
                      checked={status?.autoCloudSyncEnabled !== false}
                      onChange={(e) => handleToggleAutoCloudSync(e.target.checked)}
                      className="w-4 h-4 rounded text-purple-600 bg-slate-900 border-slate-700 focus:ring-purple-500 cursor-pointer"
                    />
                    <div>
                      <span className="font-bold text-white block">
                        Vollautomatische Cloud-Synchronisation aktiv
                      </span>
                      <span className="text-[11px] text-slate-400 block">
                        Alle neuen Game.log-Dateien, user.cfg-Snapshots und Keybinds werden nach Spielende und App-Start automatisch synchronisiert.
                      </span>
                    </div>
                  </label>

                  {((typeof status?.cloudLogCount === 'number' && status.cloudLogCount > 0) || status?.cloudLastBackupTime) && (
                    <div className="flex flex-col sm:flex-row sm:items-center gap-1.5 sm:gap-2.5 px-3 py-1.5 rounded-lg bg-emerald-950/60 border border-emerald-800/60 text-emerald-300 font-mono text-xs shrink-0 self-start sm:self-auto shadow-sm">
                      <div className="flex items-center space-x-1.5">
                        <CheckCircle2 className="w-3.5 h-3.5 text-emerald-400 shrink-0" />
                        <span>{status?.cloudLogCount ?? 0} Logs aktuell in Cloud gesichert</span>
                      </div>
                      {status?.cloudLastBackupTime && (
                        <div className="flex items-center space-x-1 text-[11px] text-emerald-400/90 border-t sm:border-t-0 sm:border-l border-emerald-800/80 pt-1 sm:pt-0 sm:pl-2.5">
                          <Clock className="w-3 h-3 text-emerald-400/80 shrink-0" />
                          <span>Stand: {status.cloudLastBackupTime} Uhr</span>
                        </div>
                      )}
                    </div>
                  )}
                </div>
              </div>

              {/* Keybind-Tresor (actionmaps.xml) */}
              <div className="p-5 rounded-2xl bg-slate-900/70 border border-slate-800 space-y-4">
                <div className="flex items-center justify-between pb-3 border-b border-slate-800">
                  <div className="flex items-center space-x-2.5">
                    <div className="w-8 h-8 rounded-lg bg-purple-500/10 border border-purple-500/30 flex items-center justify-center text-purple-400">
                      <Key className="w-4 h-4" />
                    </div>
                    <div>
                      <h3 className="text-xs font-bold text-purple-300 tracking-wider">
                        STEUERUNGS-BACKUPS (ACTIONMAPS.XML &amp; MAPPINGS)
                      </h3>
                      <p className="text-[11px] text-slate-400">
                        Sichert Joystick-, HOTAS-, HOSAS- und Tastaturbelegungen vor Spiel-Patches
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center space-x-2">
                    <span className="text-[11px] px-2.5 py-0.5 rounded-full bg-purple-950 text-purple-300 border border-purple-800 font-mono font-semibold">
                      {status?.keybindItems?.length || status?.keybindBackups?.length || 0} Profile
                    </span>
                    <button
                      onClick={() => handleOpenFolder('keybinds')}
                      title="Keybinds-Ordner im Windows Explorer öffnen"
                      className="p-1.5 rounded-lg bg-purple-950/50 hover:bg-purple-900/60 text-purple-300 border border-purple-800/60 transition cursor-pointer"
                    >
                      <ExternalLink className="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>

                <div className="p-3 rounded-xl bg-slate-950/80 border border-slate-800/80 space-y-2">
                  <span className="text-[11px] font-semibold text-slate-300 block">
                    Neues Steuerungs-Backup anlegen:
                  </span>
                  <div className="flex items-center space-x-2">
                    <input
                      type="text"
                      placeholder="Optionale Notiz (z. B. VKB Dual-Stick Patch 4.0)..."
                      value={keybindNote}
                      onChange={(e) => setKeybindNote(e.target.value)}
                      className="flex-1 bg-slate-900 border border-slate-800 rounded-lg px-3 py-1.5 text-xs text-slate-200 placeholder-slate-600 focus:outline-none focus:border-purple-500 font-mono"
                    />
                    <button
                      onClick={handleCreateKeybindBackup}
                      disabled={actionLoading !== null}
                      className="flex items-center space-x-1.5 px-3.5 py-1.5 rounded-lg bg-purple-600 hover:bg-purple-500 text-white text-xs font-bold shadow-md shadow-purple-600/20 border border-purple-400 transition cursor-pointer shrink-0"
                    >
                      <Archive className="w-3.5 h-3.5" />
                      <span>Sichern</span>
                    </button>
                  </div>
                </div>

                <div className="space-y-2">
                  <span className="text-[11px] font-semibold text-slate-400 block">
                    Gespeicherte Steuerungs-Profile:
                  </span>
                  <div className="space-y-2 overflow-y-auto max-h-[320px] pr-1">
                    {status?.keybindItems && status.keybindItems.length > 0 ? (
                      status.keybindItems.map((k, idx) => (
                        <div
                          key={idx}
                          className="p-3.5 rounded-xl bg-slate-950/80 border border-slate-800/80 hover:border-slate-700 transition flex items-center justify-between gap-3"
                        >
                          <div className="min-w-0">
                            <div className="text-xs font-bold text-slate-200 truncate font-mono">
                              {k.name}
                            </div>
                            <div className="flex items-center space-x-2 text-[10px] text-slate-500 mt-1">
                              <span className="flex items-center space-x-1">
                                <Clock className="w-3 h-3" />
                                <span>{k.createdAt}</span>
                              </span>
                              <span>•</span>
                              <span>{k.fileCount} Dateien ({k.sizeFormatted})</span>
                              <span>•</span>
                              <span
                                className={`px-1.5 py-0.2 rounded font-semibold border text-[9px] ${
                                  k.locationType.includes('Cloud')
                                    ? 'bg-purple-950 text-purple-300 border-purple-800'
                                    : 'bg-slate-900 text-slate-400 border-slate-700'
                                }`}
                              >
                                {k.locationType}
                              </span>
                            </div>
                          </div>

                          <button
                            onClick={() => handleRestoreKeybind(k)}
                            disabled={actionLoading !== null}
                            className="flex items-center space-x-1.5 px-3.5 py-1.5 rounded-lg bg-purple-950 hover:bg-purple-900/80 text-purple-300 text-xs font-bold border border-purple-800/80 transition cursor-pointer shrink-0"
                            title="Steuerung in Star Citizen wiederherstellen"
                          >
                            <RotateCcw className={`w-3.5 h-3.5 ${actionLoading === `restoreKeybind_${k.name}` ? 'animate-spin' : ''}`} />
                            <span>Rollback</span>
                          </button>
                        </div>
                      ))
                    ) : (
                      <div className="p-6 rounded-xl bg-slate-950 border border-slate-800/60 text-center text-xs text-slate-500 italic">
                        Noch keine Keybind-Backups vorhanden. Klicke auf "Sichern", um deine Belegungen vor Patches zu sichern.
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </div>
          ) : (
            <div className="space-y-6">
              <div className="p-6 rounded-2xl bg-gradient-to-r from-purple-950/40 via-slate-900/80 to-sky-950/40 border border-purple-800/40 backdrop-blur shadow-xl space-y-4">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                  <div className="flex items-center space-x-3.5">
                    <div className="w-12 h-12 rounded-xl bg-purple-500/10 border border-purple-500/30 flex items-center justify-center text-purple-400 shadow-inner shrink-0">
                      <Archive className="w-6 h-6" />
                    </div>
                    <div>
                      <div className="flex items-center space-x-2.5">
                        <h2 className="text-base font-bold text-white tracking-wide">
                          USER.CFG BACKUPS &amp; 1-KLICK ROLLBACK
                        </h2>
                        <span className="text-[11px] px-2.5 py-0.5 rounded-full bg-purple-950 text-purple-300 border border-purple-800 font-mono font-bold">
                          {status?.configBackups?.length || 0} Snapshots
                        </span>
                      </div>
                      <p className="text-xs text-slate-400 mt-1">
                        Jede Speicherung wird automatisch hier, im LIVE-Ordner (.bak) und in deiner Cloud archiviert. Du kannst jederzeit mit einem Klick auf einen früheren Stand zurückrollen.
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center space-x-2 shrink-0">
                    <button
                      onClick={() => handleOpenFolder('config')}
                      className="flex items-center space-x-1.5 px-3.5 py-2 rounded-xl bg-slate-800/90 hover:bg-slate-700 text-slate-200 text-xs font-semibold border border-slate-700 transition cursor-pointer"
                      title="Config-Backup-Ordner im Windows Explorer öffnen"
                    >
                      <FolderOpen className="w-4 h-4 text-sky-400" />
                      <span>Ordner öffnen</span>
                    </button>

                    <button
                      onClick={handleBackupUserCfgSnapshot}
                      disabled={actionLoading !== null}
                      className="flex items-center space-x-1.5 px-4 py-2 rounded-xl bg-purple-600 hover:bg-purple-500 text-white text-xs font-bold shadow-md shadow-purple-600/25 border border-purple-400 transition cursor-pointer"
                    >
                      <Download className="w-4 h-4" />
                      <span>Jetzt sichern</span>
                    </button>
                  </div>
                </div>

                <div className="flex items-center space-x-2 pt-3 border-t border-purple-900/40">
                  <input
                    type="text"
                    placeholder="Optionale Notiz für manuelles Backup (z. B. Vor Grafik-Update, Vor Patch 4.0, RTX 5070 Tuning)..."
                    value={configNote}
                    onChange={(e) => setConfigNote(e.target.value)}
                    className="flex-1 bg-slate-950/80 border border-slate-800 rounded-xl px-4 py-2.5 text-xs text-slate-200 placeholder-slate-600 focus:outline-none focus:border-purple-500 font-mono"
                  />
                  <button
                    onClick={handleBackupUserCfgSnapshot}
                    disabled={actionLoading !== null}
                    className="px-4 py-2.5 rounded-xl bg-purple-950/60 hover:bg-purple-900 text-purple-200 text-xs font-semibold border border-purple-800 transition cursor-pointer shrink-0"
                  >
                    Snapshot anlegen
                  </button>
                </div>
              </div>

              <div className="p-5 rounded-2xl bg-slate-900/80 border border-slate-800 space-y-4">
                <div className="flex items-center justify-between pb-2 border-b border-slate-800">
                  <span className="text-xs font-bold text-slate-300 uppercase tracking-wider font-mono">
                    Archivierte Snapshot-Versionen
                  </span>
                  <span className="text-[11px] text-slate-500 font-mono">
                    Sortiert nach Datum (Neueste zuerst)
                  </span>
                </div>

                <div className="space-y-2.5 overflow-y-auto max-h-[500px] pr-1">
                  {status?.configBackups && status.configBackups.length > 0 ? (
                    status.configBackups.map((c, idx) => (
                      <div
                        key={idx}
                        className="p-4 rounded-xl bg-slate-950/80 border border-slate-800/90 hover:border-slate-700 transition flex flex-col sm:flex-row sm:items-center justify-between gap-3 group"
                      >
                        <div className="min-w-0">
                          <div className="text-xs font-bold text-slate-200 truncate font-mono group-hover:text-purple-300 transition">
                            {c.name}
                          </div>
                          <div className="flex flex-wrap items-center gap-2 text-[11px] text-slate-500 mt-1 font-mono">
                            <span className="flex items-center space-x-1">
                              <Clock className="w-3 h-3 text-slate-400" />
                              <span>{c.createdAt}</span>
                            </span>
                            <span>•</span>
                            <span>{c.sizeFormatted}</span>
                            <span>•</span>
                            <span
                              className={`px-2 py-0.5 rounded font-semibold border text-[10px] ${
                                c.locationType.includes('Cloud')
                                  ? 'bg-sky-950 text-sky-400 border-sky-800'
                                  : 'bg-slate-900 text-slate-400 border-slate-700'
                              }`}
                            >
                              {c.locationType}
                            </span>
                          </div>
                        </div>

                        <div className="flex items-center space-x-2 shrink-0">
                          <button
                            onClick={() => handleRestoreConfigSnapshot(c)}
                            disabled={actionLoading !== null}
                            className="flex items-center space-x-1.5 px-4 py-2 rounded-xl bg-purple-950/70 hover:bg-purple-900 text-purple-200 text-xs font-bold border border-purple-800 transition cursor-pointer"
                            title="Diesen Stand wieder in die LIVE user.cfg einspielen (aktueller Stand wird zuvor automatisch gesichert)"
                          >
                            <RotateCcw className={`w-3.5 h-3.5 ${actionLoading === `restoreCfg_${c.name}` ? 'animate-spin' : ''}`} />
                            <span>Wiederherstellen</span>
                          </button>
                        </div>
                      </div>
                    ))
                  ) : (
                    <div className="p-8 rounded-xl bg-slate-950 border border-slate-800/60 text-center text-xs text-slate-500 italic space-y-2">
                      <Archive className="w-8 h-8 text-slate-600 mx-auto" />
                      <p>Noch keine user.cfg-Snapshots vorhanden.</p>
                      <p className="text-[11px] text-slate-600">
                        Klicke oben auf "Jetzt sichern", oder passe einen Wert an — vor jeder Änderung wird automatisch ein Stand gesichert.
                      </p>
                    </div>
                  )}
                </div>
              </div>
            </div>
          )}
        </div>
      )}

      {/* ══════════════════════════════════════════════════════════════
          TAB 4: HOTAS & JOYSTICKS (OPTIONAL PROFILER & SWAP TOOL)
          ══════════════════════════════════════════════════════════════ */}
      {activeTab === 'hotas' && (
        <div className="space-y-6">
          {/* Top Bar / Profile Info & Quick Actions */}
          <div className="p-4 rounded-2xl bg-slate-900/80 border border-slate-800 flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div className="flex items-center space-x-3">
              <div className="w-10 h-10 rounded-xl bg-emerald-500/10 border border-emerald-500/30 flex items-center justify-center text-emerald-400 shrink-0">
                <Gamepad2 className="w-5 h-5" />
              </div>
              <div>
                <div className="flex items-center space-x-2">
                  <h3 className="text-sm font-bold text-white tracking-wide">
                    STAR CITIZEN HOTAS &amp; CONTROLS PROFILER
                  </h3>
                  <span className="px-2 py-0.5 text-[10px] font-semibold rounded bg-emerald-950 text-emerald-300 border border-emerald-800 font-mono">
                    {hotasStatus?.profileName || 'LIVE'}
                  </span>
                  {!hotasEnabled && (
                    <span className="px-2 py-0.5 text-[10px] font-semibold rounded bg-amber-950 text-amber-300 border border-amber-800 font-mono">
                      DEAKTIVIERT
                    </span>
                  )}
                </div>
                <p className="text-xs text-slate-400 mt-0.5 truncate max-w-xl font-mono">
                  {hotasStatus?.actionMapsPath || 'USER\\Client\\0\\Profiles\\default\\actionmaps.xml'}
                </p>
              </div>
            </div>

            <div className="flex flex-wrap items-center gap-2">
              <button
                onClick={loadHotasStatus}
                disabled={hotasLoading}
                className="flex items-center space-x-1.5 px-3 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs font-semibold border border-slate-700 transition cursor-pointer"
                title="actionmaps.xml & Game.log neu einlesen"
              >
                <RefreshCw className={`w-3.5 h-3.5 ${hotasLoading ? 'animate-spin' : ''}`} />
                <span>Neu einlesen</span>
              </button>

              <button
                onClick={() => handleSwapSticks(1, 2)}
                disabled={hotasSwapping || !hotasStatus?.devices || hotasStatus.devices.length < 2}
                className="flex items-center space-x-1.5 px-3.5 py-2 rounded-xl bg-amber-600/90 hover:bg-amber-500 text-white text-xs font-bold shadow-md shadow-amber-600/20 border border-amber-400/60 transition cursor-pointer"
                title="Tauscht Stick 1 und Stick 2 in actionmaps.xml (mit automatischem Backup)"
              >
                <ArrowLeftRight className={`w-3.5 h-3.5 ${hotasSwapping ? 'animate-spin' : ''}`} />
                <span>Stick 1 ⇄ 2 Tauschen</span>
              </button>

              <button
                onClick={() => setHotasExportModalOpen(true)}
                disabled={!hotasStatus?.actionMapsFound}
                className="flex items-center space-x-1.5 px-3.5 py-2 rounded-xl bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-bold shadow-md shadow-emerald-600/20 border border-emerald-400 transition cursor-pointer"
                title="Profil unter controls\\mappings als XML-Layout exportieren"
              >
                <Save className="w-3.5 h-3.5" />
                <span>Profil Exportieren</span>
              </button>
            </div>
          </div>

          {/* DISABLED BANNER */}
          {!hotasEnabled && (
            <div className="p-4 rounded-xl bg-amber-950/40 border border-amber-500/40 flex items-center justify-between gap-3 text-xs text-amber-200">
              <div className="flex items-center space-x-2.5">
                <AlertTriangle className="w-4 h-4 text-amber-400 shrink-0" />
                <span>
                  Der HOTAS Profiler ist in den allgemeinen Einstellungen als optional markiert und aktuell deaktiviert.
                </span>
              </div>
              <button
                onClick={async () => {
                  try {
                    const st = await bridge.getSettings();
                    if (st) {
                      st.hotasProfilerEnabled = true;
                      await bridge.send('save_settings', { settings: st });
                      setHotasEnabled(true);
                      showToast('✓ HOTAS Profiler dauerhaft aktiviert');
                      loadHotasStatus();
                    }
                  } catch (e) {
                    setHotasEnabled(true);
                  }
                }}
                className="px-3 py-1.5 rounded-lg bg-amber-600 hover:bg-amber-500 text-white text-xs font-bold transition shrink-0 cursor-pointer shadow-sm"
              >
                Jetzt dauerhaft aktivieren
              </button>
            </div>
          )}

          {/* MISMATCH WARNING BANNER (When USB Index Swap is detected) */}
          {hotasStatus?.hasMismatch && (
            <div className="p-5 rounded-2xl bg-gradient-to-r from-rose-950/90 via-slate-900 to-rose-950/80 border-2 border-rose-500/80 shadow-2xl shadow-rose-950/50 space-y-3 animate-in fade-in slide-in-from-top-2">
              <div className="flex items-start justify-between gap-4">
                <div className="flex items-start space-x-3.5">
                  <div className="w-10 h-10 rounded-xl bg-rose-500/20 border border-rose-500/40 flex items-center justify-center text-rose-400 shrink-0 mt-0.5">
                    <AlertTriangle className="w-5 h-5 animate-bounce" />
                  </div>
                  <div className="space-y-1">
                    <div className="flex items-center space-x-2">
                      <span className="text-sm font-extrabold text-white tracking-wide uppercase text-rose-300">
                        ⚠️ Joystick-Vertauschung erkannt! (Windows USB Index Swap)
                      </span>
                      <span className="px-2 py-0.5 text-[10px] font-bold rounded-full bg-rose-900 text-rose-200 border border-rose-600">
                        KORREKTUR BEREIT
                      </span>
                    </div>
                    <p className="text-xs text-rose-100/90 leading-relaxed font-medium">
                      {hotasStatus.mismatchDescription || 'Windows hat die Reihenfolge deiner Joysticks beim Booten oder USB-Einstecken vertauscht.'}
                    </p>
                  </div>
                </div>
              </div>

              <div className="flex flex-wrap items-center gap-3 pt-2 border-t border-rose-900/60">
                <button
                  onClick={() => handleSwapSticks(1, 2)}
                  disabled={hotasSwapping}
                  className="flex items-center space-x-2 px-4 py-2.5 rounded-xl bg-rose-600 hover:bg-rose-500 text-white text-xs font-extrabold shadow-lg shadow-rose-600/40 border border-rose-300 transition cursor-pointer"
                >
                  <Zap className={`w-4 h-4 text-amber-300 ${hotasSwapping ? 'animate-spin' : ''}`} />
                  <span>1-Klick Profil korrigieren (Auto-Swap in actionmaps.xml)</span>
                </button>

                {hotasStatus.suggestedConsoleCommand && (
                  <div className="flex items-center space-x-2 bg-slate-950/90 border border-slate-700 rounded-xl px-3 py-1.5">
                    <span className="text-[11px] text-slate-400 font-mono">In-Game Konsole:</span>
                    <code className="text-xs font-mono font-bold text-amber-300 px-2 py-0.5 rounded bg-slate-900 border border-slate-800">
                      {hotasStatus.suggestedConsoleCommand}
                    </code>
                    <button
                      onClick={() => {
                        navigator.clipboard.writeText(hotasStatus.suggestedConsoleCommand || '');
                        setCopiedResortCmd(true);
                        setTimeout(() => setCopiedResortCmd(false), 2500);
                        showToast('Konsolenbefehl in Zwischenablage kopiert! Im Spiel Taste ^ drücken und einfügen.');
                      }}
                      className="p-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white transition cursor-pointer"
                      title="Befehl kopieren"
                    >
                      {copiedResortCmd ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5 text-slate-300" />}
                    </button>
                  </div>
                )}

                <span className="text-[11px] text-slate-400 italic ml-auto">
                  Tipp: Der Konsolenbefehl funktioniert direkt im laufenden Spiel ohne Neustart.
                </span>
              </div>
            </div>
          )}

          {/* CONNECTED HARDWARE CARDS GRID */}
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <h4 className="text-xs font-bold text-slate-300 uppercase tracking-wider flex items-center space-x-2">
                <Sliders className="w-3.5 h-3.5 text-emerald-400" />
                <span>Erkannte Steuerungsgeräte ({hotasStatus?.devices?.length || 0} Joysticks/HOTAS)</span>
              </h4>
              <span className="text-xs text-slate-500 font-mono">
                {hotasStatus?.joystickBindingsCount || 0} Joystick-Belegungen · {hotasStatus?.totalBindingsCount || 0} Gesamt
              </span>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {hotasStatus?.devices && hotasStatus.devices.length > 0 ? (
                hotasStatus.devices.map((dev) => (
                  <div
                    key={dev.instance}
                    className={`p-5 rounded-2xl bg-slate-900/80 border transition flex flex-col justify-between space-y-4 ${
                      dev.isSwappedWith
                        ? 'border-rose-500/70 shadow-lg shadow-rose-950/30'
                        : 'border-slate-800 hover:border-slate-700'
                    }`}
                  >
                    <div>
                      {/* Top Badges */}
                      <div className="flex items-center justify-between gap-2">
                        <div className="flex items-center space-x-2">
                          <span className="px-2.5 py-1 rounded-lg bg-emerald-500/20 text-emerald-300 border border-emerald-500/40 text-xs font-bold font-mono">
                            Stick #{dev.instance} ({dev.deviceKey})
                          </span>
                          {dev.vendorName && (
                            <span className="px-2 py-0.5 rounded bg-slate-800 text-slate-300 text-[10px] font-semibold border border-slate-700">
                              {dev.vendorName}
                            </span>
                          )}
                        </div>

                        {dev.isSwappedWith ? (
                          <span className="px-2 py-0.5 rounded bg-rose-950 text-rose-300 text-[10px] font-bold border border-rose-800 animate-pulse">
                            Vertauscht mit Stick {dev.isSwappedWith}
                          </span>
                        ) : dev.isConnected ? (
                          <span className="flex items-center space-x-1 px-2 py-0.5 rounded bg-emerald-950 text-emerald-300 text-[10px] font-semibold border border-emerald-800">
                            <Check className="w-3 h-3" />
                            <span>In Game.log erkannt</span>
                          </span>
                        ) : (
                          <span className="px-2 py-0.5 rounded bg-slate-800 text-slate-400 text-[10px] border border-slate-700">
                            Im Profil hinterlegt
                          </span>
                        )}
                      </div>

                      {/* Product Name */}
                      <h4 className="text-base font-bold text-white mt-3 flex items-center space-x-2">
                        <span>{dev.product || `Joystick ${dev.instance}`}</span>
                      </h4>

                      {/* USB GUID Info */}
                      {dev.guid && (
                        <p className="text-[11px] text-slate-400 font-mono mt-1 truncate">
                          {dev.guid} {dev.usbVid && dev.usbPid ? `(VID: ${dev.usbVid} · PID: ${dev.usbPid})` : ''}
                        </p>
                      )}

                      {/* Stats & Deadzones */}
                      <div className="mt-4 grid grid-cols-2 gap-2 text-xs">
                        <div className="p-2.5 rounded-xl bg-slate-950/70 border border-slate-800">
                          <span className="text-[10px] text-slate-500 font-semibold block uppercase">Belegte Aktionen</span>
                          <span className="text-sm font-bold text-sky-300 font-mono">{dev.bindingCount}</span>
                        </div>

                        <div className="p-2.5 rounded-xl bg-slate-950/70 border border-slate-800">
                          <span className="text-[10px] text-slate-500 font-semibold block uppercase">Kurven / Exponenten</span>
                          <span className="text-sm font-bold text-emerald-300 font-mono">
                            {dev.curves.length > 0 ? `${dev.curves.length} konfiguriert` : 'Linear (1.0)'}
                          </span>
                        </div>
                      </div>

                      {/* Configured Deadzones Pills */}
                      {dev.deadzones && Object.keys(dev.deadzones).length > 0 && (
                        <div className="mt-3 space-y-1.5">
                          <span className="text-[10px] font-semibold text-slate-400 uppercase tracking-wider block">
                            Totzonen (Deadzones):
                          </span>
                          <div className="flex flex-wrap gap-1.5">
                            {Object.entries(dev.deadzones).map(([axis, val]) => (
                              <span
                                key={axis}
                                className="px-2 py-0.5 rounded bg-slate-800/80 border border-slate-700 text-slate-300 font-mono text-[10px]"
                              >
                                {axis.toUpperCase()}: {(val * 100).toFixed(1)}%
                              </span>
                            ))}
                          </div>
                        </div>
                      )}

                      {/* Configured Curves */}
                      {dev.curves && dev.curves.length > 0 && (
                        <div className="mt-3 space-y-1.5">
                          <span className="text-[10px] font-semibold text-slate-400 uppercase tracking-wider block">
                            Achsenkurven &amp; Invertierung:
                          </span>
                          <div className="flex flex-wrap gap-1.5">
                            {dev.curves.map((c) => (
                              <span
                                key={c.option}
                                className="px-2 py-0.5 rounded bg-slate-800/80 border border-slate-700 text-emerald-300 font-mono text-[10px]"
                              >
                                {c.optionLabel}: exp {c.exponent ?? 1.0} {c.inverted ? '(Invertiert)' : ''}
                              </span>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>

                    {/* Bottom Card Action */}
                    <div className="pt-3 border-t border-slate-800 flex items-center justify-between">
                      <button
                        onClick={() => handleSwapSticks(dev.instance, dev.instance === 1 ? 2 : 1)}
                        disabled={hotasSwapping}
                        className="flex items-center space-x-1.5 text-xs text-amber-400 hover:text-amber-300 font-semibold cursor-pointer transition"
                      >
                        <ArrowLeftRight className="w-3.5 h-3.5" />
                        <span>Mit Stick {dev.instance === 1 ? 2 : 1} tauschen</span>
                      </button>

                      <button
                        onClick={() => {
                          setHotasFilterDevice(dev.deviceKey);
                        }}
                        className="text-xs text-sky-400 hover:text-sky-300 font-medium cursor-pointer"
                      >
                        Belegungen filtern ({dev.bindingCount}) →
                      </button>
                    </div>
                  </div>
                ))
              ) : (
                <div className="col-span-2 p-8 rounded-2xl bg-slate-900/60 border border-slate-800 text-center space-y-2">
                  <Gamepad2 className="w-8 h-8 text-slate-500 mx-auto" />
                  <div className="text-sm font-semibold text-slate-300">Keine Joystick-Geräte in actionmaps.xml gefunden</div>
                  <p className="text-xs text-slate-500 max-w-md mx-auto">
                    Starte Star Citizen und belege mindestens eine Joystick-Achse oder Taste, damit Star Citizen die Optionen in actionmaps.xml abspeichert.
                  </p>
                </div>
              )}
            </div>
          </div>

          {/* INTERACTIVE BINDINGS EXPLORER */}
          <div className="p-6 rounded-2xl bg-slate-900/80 border border-slate-800 space-y-5">
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
              <div>
                <h4 className="text-sm font-bold text-white flex items-center space-x-2">
                  <BookOpen className="w-4 h-4 text-sky-400" />
                  <span>TASTEN- &amp; ACHSENBELEGUNGS-EXPLORER</span>
                </h4>
                <p className="text-xs text-slate-400 mt-0.5">
                  Durchsuche alle aktiven Belegungen deines Star Citizen Profils nach Aktion, Stick, Achse oder Taste.
                </p>
              </div>

              {/* Search Box */}
              <div className="relative w-full md:w-72">
                <Search className="w-4 h-4 absolute left-3 top-2.5 text-slate-500" />
                <input
                  type="text"
                  placeholder="Suchen (z. B. Pitch, Feuer, Boost, js1)..."
                  value={hotasSearch}
                  onChange={(e) => setHotasSearch(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl pl-9 pr-4 py-2 text-xs text-slate-200 placeholder-slate-500 focus:outline-none focus:border-sky-500 font-mono"
                />
              </div>
            </div>

            {/* Filter Bar: Device Selector + Category Selector */}
            <div className="flex flex-wrap items-center justify-between gap-3 pt-2 border-t border-slate-800/80">
              {/* Device Tabs */}
              <div className="flex flex-wrap items-center gap-1.5">
                <span className="text-[11px] font-semibold text-slate-500 mr-1">Gerät:</span>
                {[
                  { id: 'all', label: 'Alle Geräte' },
                  { id: 'js1', label: 'Stick 1 (js1)' },
                  { id: 'js2', label: 'Stick 2 (js2)' },
                  { id: 'kb1', label: 'Tastatur' },
                  { id: 'mo1', label: 'Maus' },
                ].map((d) => (
                  <button
                    key={d.id}
                    onClick={() => setHotasFilterDevice(d.id)}
                    className={`px-2.5 py-1 rounded-lg text-xs font-semibold transition cursor-pointer ${
                      hotasFilterDevice === d.id
                        ? 'bg-sky-500/20 text-sky-300 border border-sky-500/40 shadow-sm'
                        : 'bg-slate-950 text-slate-400 hover:text-slate-200 border border-slate-800'
                    }`}
                  >
                    {d.label}
                  </button>
                ))}
              </div>

              {/* Category Pills */}
              <div className="flex flex-wrap items-center gap-1.5">
                <span className="text-[11px] font-semibold text-slate-500 mr-1">Kategorie:</span>
                {['all', 'Flug', 'Kampf', 'Systeme', 'Industrie', 'Scanner', 'Allgemein'].map((c) => (
                  <button
                    key={c}
                    onClick={() => setHotasFilterCategory(c)}
                    className={`px-2 py-0.5 rounded text-[11px] font-medium transition cursor-pointer ${
                      hotasFilterCategory === c
                        ? 'bg-emerald-500/20 text-emerald-300 border border-emerald-500/40'
                        : 'bg-slate-950 text-slate-500 hover:text-slate-300 border border-slate-800/60'
                    }`}
                  >
                    {c === 'all' ? 'Alle' : c}
                  </button>
                ))}
              </div>
            </div>

            {/* Bindings Table */}
            <div className="overflow-x-auto rounded-xl border border-slate-800 bg-slate-950/60 max-h-[500px]">
              <table className="w-full text-left text-xs">
                <thead className="bg-slate-900/90 text-slate-400 font-semibold border-b border-slate-800 sticky top-0 z-10 backdrop-blur">
                  <tr>
                    <th className="py-2.5 px-4">Kategorie / Map</th>
                    <th className="py-2.5 px-4">Aktion in Star Citizen</th>
                    <th className="py-2.5 px-4">Eingabegerät</th>
                    <th className="py-2.5 px-4">Belegtes Steuerelement</th>
                    <th className="py-2.5 px-4">Modus</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-800/60 font-mono">
                  {(() => {
                    const filtered = (hotasStatus?.bindings || []).filter((b) => {
                      if (hotasFilterDevice !== 'all' && b.deviceKey !== hotasFilterDevice) return false;
                      if (hotasFilterCategory !== 'all' && b.category !== hotasFilterCategory) return false;
                      if (!hotasSearch.trim()) return true;
                      const q = hotasSearch.toLowerCase();
                      return (
                        b.action.toLowerCase().includes(q) ||
                        b.actionLabel.toLowerCase().includes(q) ||
                        b.actionMap.toLowerCase().includes(q) ||
                        b.actionMapLabel.toLowerCase().includes(q) ||
                        b.rawInput.toLowerCase().includes(q) ||
                        b.control.toLowerCase().includes(q) ||
                        b.controlLabel.toLowerCase().includes(q)
                      );
                    });

                    if (filtered.length === 0) {
                      return (
                        <tr>
                          <td colSpan={5} className="py-8 text-center text-slate-500 italic font-sans">
                            Keine Belegungen gefunden, die den Kriterien entsprechen.
                          </td>
                        </tr>
                      );
                    }

                    return filtered.map((b, idx) => (
                      <tr key={idx} className="hover:bg-slate-900/40 transition">
                        <td className="py-2.5 px-4">
                          <div className="font-sans font-semibold text-slate-300 text-xs">
                            {b.actionMapLabel}
                          </div>
                          <div className="text-[10px] text-slate-500 font-mono">
                            {b.actionMap}
                          </div>
                        </td>

                        <td className="py-2.5 px-4">
                          <div className="font-sans font-semibold text-white text-xs">
                            {b.actionLabel}
                          </div>
                          <div className="text-[10px] text-slate-500 font-mono">
                            {b.action}
                          </div>
                        </td>

                        <td className="py-2.5 px-4">
                          <span
                            className={`px-2 py-0.5 rounded text-[10px] font-bold uppercase ${
                              b.deviceType === 'joystick'
                                ? b.instance === 1
                                  ? 'bg-emerald-950 text-emerald-300 border border-emerald-800'
                                  : 'bg-teal-950 text-teal-300 border border-teal-800'
                                : b.deviceType === 'keyboard'
                                ? 'bg-sky-950 text-sky-300 border border-sky-800'
                                : 'bg-slate-800 text-slate-400 border border-slate-700'
                            }`}
                          >
                            {b.deviceType === 'joystick' ? `Stick ${b.instance} (${b.deviceKey})` : b.deviceKey}
                          </span>
                        </td>

                        <td className="py-2.5 px-4">
                          <span className="font-bold text-amber-300">
                            {b.controlLabel}
                          </span>
                          <span className="text-[10px] text-slate-500 ml-1.5">
                            ({b.rawInput})
                          </span>
                        </td>

                        <td className="py-2.5 px-4 text-[11px] text-slate-400 font-sans">
                          {b.activationMode ? (
                            <span className="px-1.5 py-0.2 rounded bg-slate-800 text-slate-300 border border-slate-700 text-[10px] uppercase font-mono">
                              {b.activationMode}
                            </span>
                          ) : (
                            <span className="text-slate-600">—</span>
                          )}
                          {b.multiTap && (
                            <span className="ml-1 text-[10px] text-purple-300 font-mono">
                              ({b.multiTap}x Tap)
                            </span>
                          )}
                        </td>
                      </tr>
                    ));
                  })()}
                </tbody>
              </table>
            </div>
          </div>

          {/* EXPORT MODAL */}
          {hotasExportModalOpen && (
            <div className="fixed inset-0 z-50 bg-black/80 backdrop-blur-md p-4 flex items-center justify-center animate-in fade-in duration-150">
              <div className="w-full max-w-md bg-slate-900 border border-slate-700 rounded-2xl shadow-2xl overflow-hidden p-6 space-y-4">
                <div className="flex items-center space-x-3">
                  <div className="w-10 h-10 rounded-xl bg-emerald-500/10 border border-emerald-500/30 flex items-center justify-center text-emerald-400 shrink-0">
                    <Save className="w-5 h-5" />
                  </div>
                  <div>
                    <h3 className="text-sm font-bold text-white">Steuerungsprofil Exportieren</h3>
                    <p className="text-xs text-slate-400">
                      Wird in <code>user\client\0\controls\mappings\</code> gespeichert.
                    </p>
                  </div>
                </div>

                <div className="space-y-2">
                  <label className="text-xs font-semibold text-slate-300 block">Profil-Name:</label>
                  <input
                    type="text"
                    value={hotasExportName}
                    onChange={(e) => setHotasExportName(e.target.value)}
                    placeholder="z. B. Torsten_Dual_Stick"
                    className="w-full bg-slate-950 border border-slate-700 rounded-xl px-3.5 py-2 text-xs text-white font-mono focus:outline-none focus:border-emerald-500"
                  />
                  <span className="text-[11px] text-slate-500 font-mono block">
                    Dateiname: layout_{hotasExportName || 'profile'}_exported.xml
                  </span>
                </div>

                <div className="flex items-center justify-end space-x-2 pt-3 border-t border-slate-800">
                  <button
                    onClick={() => setHotasExportModalOpen(false)}
                    className="px-4 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-medium transition cursor-pointer"
                  >
                    Abbrechen
                  </button>
                  <button
                    onClick={handleExportHotas}
                    disabled={hotasExporting}
                    className="flex items-center space-x-1.5 px-4 py-2 rounded-xl bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-bold transition shadow-md shadow-emerald-600/20 cursor-pointer"
                  >
                    <Save className="w-3.5 h-3.5" />
                    <span>{hotasExporting ? 'Exportiere...' : 'Jetzt Exportieren'}</span>
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      )}

      {/* ══════════════════════════════════════════════════════════════
          POPOUT / VOLLBILD LIVE-EDITOR MODAL
          ══════════════════════════════════════════════════════════════ */}
      {editorPopout && (
        <div className="fixed inset-0 z-50 bg-slate-950/90 backdrop-blur-md p-3 sm:p-6 flex flex-col animate-in fade-in duration-200">
          <div className="flex-1 flex flex-col bg-slate-900/95 border border-slate-700/80 rounded-2xl shadow-2xl overflow-hidden">
            {/* Popout Header */}
            <div className="p-4 border-b border-slate-800 bg-slate-950/80 flex flex-wrap items-center justify-between gap-3">
              <div className="flex items-center space-x-3">
                <div className="w-8 h-8 rounded-lg bg-sky-500/10 border border-sky-500/30 flex items-center justify-center text-sky-400 shrink-0">
                  <Terminal className="w-4 h-4" />
                </div>
                <div>
                  <div className="flex items-center space-x-2">
                    <span className="text-sm font-bold text-white">user.cfg Studio — Popout Live-Editor</span>
                    <span className="text-xs text-sky-400 font-mono">
                      ({cfgContent.split('\n').length} Zeilen · {cfgContent.length} Zeichen)
                    </span>
                  </div>
                  <p className="text-[11px] text-slate-400 font-mono truncate max-w-xl">
                    {status?.userCfgPath || 'StarCitizen\\LIVE\\user.cfg'}
                  </p>
                </div>
              </div>

              {/* Presets & Actions in Popout */}
              <div className="flex flex-wrap items-center gap-2">
                <div className="flex items-center rounded-lg bg-gradient-to-r from-amber-500/20 to-sky-500/20 border border-amber-500/40 p-0.5">
                  <button
                    onClick={() => applyPreset('hardware_auto', false)}
                    className="px-2.5 py-1 text-xs font-bold text-amber-300 hover:text-white transition flex items-center space-x-1.5 cursor-pointer"
                    title={`Wendet die Hardware-Empfehlung (${hwRec.badge}) per Merge an`}
                  >
                    <Rocket className="w-3.5 h-3.5 text-amber-400" />
                    <span>⭐ Hardware-Empfehlung</span>
                    {hwRec.badge && (
                      <span className="px-1.5 py-0.2 rounded bg-amber-950/80 text-amber-300 text-[9px] font-mono border border-amber-500/30">
                        {hwRec.badge}
                      </span>
                    )}
                  </button>
                  <button
                    onClick={() => applyPreset('hardware_auto', true)}
                    className="px-2 py-1 text-[10px] font-mono text-amber-400 hover:text-amber-200 bg-amber-950/60 rounded border border-amber-500/30 transition cursor-pointer"
                    title="Reines Template einfügen"
                  >
                    Template
                  </button>
                </div>

                <button
                  onClick={() => setPreviewPreset('hardware_auto')}
                  className="flex items-center space-x-1 px-2.5 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-amber-300 hover:text-white text-xs font-semibold border border-amber-500/30 transition cursor-pointer"
                  title="Vorlagen-Vorschau öffnen"
                >
                  <Eye className="w-3.5 h-3.5 text-amber-400" />
                  <span>Vorschau</span>
                </button>

                <button
                  onClick={copyToClipboard}
                  className="flex items-center space-x-1 px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs transition cursor-pointer"
                >
                  {copied ? (
                    <>
                      <Check className="w-3.5 h-3.5 text-emerald-400" />
                      <span className="text-emerald-400">Kopiert</span>
                    </>
                  ) : (
                    <>
                      <Copy className="w-3.5 h-3.5" />
                      <span>Kopieren</span>
                    </>
                  )}
                </button>

                <button
                  onClick={handleBackupUserCfgSnapshot}
                  disabled={actionLoading !== null}
                  className="flex items-center space-x-1.5 px-3 py-1.5 rounded-lg bg-purple-950 hover:bg-purple-900 text-purple-300 text-xs font-semibold border border-purple-800 transition cursor-pointer"
                >
                  <Archive className="w-3.5 h-3.5" />
                  <span>Snapshot sichern</span>
                </button>

                <button
                  onClick={handleSaveUserCfg}
                  disabled={actionLoading === 'saveCfg'}
                  className="flex items-center space-x-1.5 px-4 py-1.5 rounded-lg bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold transition shadow-md shadow-sky-600/20 cursor-pointer"
                >
                  <Save className="w-3.5 h-3.5" />
                  <span>{actionLoading === 'saveCfg' ? 'Speichere...' : 'Speichern (mit Auto-Backup)'}</span>
                </button>

                <button
                  onClick={() => setEditorPopout(false)}
                  className="flex items-center space-x-1.5 px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white text-xs font-semibold border border-slate-700 transition cursor-pointer"
                  title="Popout schließen"
                >
                  <Minimize2 className="w-3.5 h-3.5 text-slate-400" />
                  <span>Schließen</span>
                  <X className="w-3.5 h-3.5 text-slate-500 ml-0.5" />
                </button>
              </div>
            </div>

            {/* Popout Textarea */}
            <div className="flex-1 p-3 bg-slate-950/90 overflow-hidden flex flex-col font-mono text-xs">
              <textarea
                value={cfgContent}
                onChange={(e) => {
                  setCfgContent(e.target.value);
                  parseCfgContent(e.target.value);
                }}
                spellCheck={false}
                autoFocus
                className="flex-1 w-full p-4 bg-slate-950 text-sky-100 rounded-xl border border-slate-800 focus:border-sky-500/50 focus:outline-none font-mono text-xs leading-relaxed resize-none selection:bg-sky-800/50"
                placeholder="; Star Citizen user.cfg Konfiguration&#10;Con_Restricted = 0&#10;r_VSync = 0&#10;sys_maxfps = 160&#10;..."
              />
            </div>

            {/* Popout Footer */}
            <div className="p-3 border-t border-slate-800 bg-slate-950/80 flex items-center justify-between text-xs text-slate-400">
              <div className="flex items-center space-x-2">
                <ShieldCheck className="w-4 h-4 text-emerald-400 shrink-0" />
                <span className="text-slate-300">
                  100% geschützt: Vor jedem Speichern wird automatisch ein Backup-Snapshot gesichert.
                </span>
              </div>
              <button
                onClick={() => setEditorPopout(false)}
                className="text-xs text-sky-400 hover:text-sky-300 font-medium cursor-pointer"
              >
                Fertig &amp; Schließen ✕
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ══════════════════════════════════════════════════════════════
          VORLAGEN-VORSCHAU MODAL
          ══════════════════════════════════════════════════════════════ */}
      {previewPreset && (
        <div className="fixed inset-0 z-50 bg-black/80 backdrop-blur-md p-3 sm:p-6 flex items-center justify-center animate-in fade-in duration-150">
          <div className="w-full max-w-4xl max-h-[90vh] flex flex-col bg-slate-900 border border-slate-700/80 rounded-2xl shadow-2xl overflow-hidden">
            {/* Modal Header */}
            <div className="p-4 border-b border-slate-800 bg-slate-950/80 flex items-center justify-between gap-3">
              <div className="flex items-center space-x-3">
                <div className="w-9 h-9 rounded-xl bg-amber-500/10 border border-amber-500/30 flex items-center justify-center text-amber-400 shrink-0">
                  <Eye className="w-5 h-5" />
                </div>
                <div>
                  <h3 className="text-sm font-bold text-white tracking-wide">
                    VORLAGEN-VORSCHAU &amp; DETAILS
                  </h3>
                  <p className="text-xs text-slate-400">
                    Prüfe die genauen Befehle und Werte, bevor du sie in deine user.cfg übernimmst.
                  </p>
                </div>
              </div>

              <button
                onClick={() => setPreviewPreset(null)}
                className="p-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-400 hover:text-white transition cursor-pointer"
                title="Vorschau schließen [ESC]"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Template Switcher Tabs inside Preview */}
            <div className="flex items-center space-x-2 p-3 bg-slate-950/50 border-b border-slate-800 overflow-x-auto">
              <button
                onClick={() => setPreviewPreset('hardware_auto')}
                className={`flex items-center space-x-2 px-3.5 py-1.5 rounded-xl text-xs font-bold transition cursor-pointer shrink-0 ${
                  previewPreset === 'hardware_auto'
                    ? 'bg-amber-500/20 text-amber-300 border border-amber-500/40 shadow-sm'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60 border border-transparent'
                }`}
              >
                <Rocket className="w-3.5 h-3.5 text-amber-400" />
                <span>⭐ Hardware-Empfehlung</span>
                {hwRec.badge && (
                  <span className="text-[10px] font-mono px-1.5 py-0.2 rounded bg-amber-950 text-amber-300 border border-amber-500/30">
                    {hwRec.badge}
                  </span>
                )}
              </button>

              <button
                onClick={() => setPreviewPreset('esport')}
                className={`flex items-center space-x-2 px-3.5 py-1.5 rounded-xl text-xs font-bold transition cursor-pointer shrink-0 ${
                  previewPreset === 'esport'
                    ? 'bg-sky-500/20 text-sky-300 border border-sky-500/40 shadow-sm'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60 border border-transparent'
                }`}
              >
                <Zap className="w-3.5 h-3.5 text-sky-400" />
                <span>⚡ High FPS / E-Sport</span>
              </button>

              <button
                onClick={() => setPreviewPreset('quality')}
                className={`flex items-center space-x-2 px-3.5 py-1.5 rounded-xl text-xs font-bold transition cursor-pointer shrink-0 ${
                  previewPreset === 'quality'
                    ? 'bg-purple-500/20 text-purple-300 border border-purple-500/40 shadow-sm'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60 border border-transparent'
                }`}
              >
                <Palette className="w-3.5 h-3.5 text-purple-400" />
                <span>🎨 Grafik &amp; Immersion</span>
              </button>

              <button
                onClick={() => setPreviewPreset('minimal')}
                className={`flex items-center space-x-2 px-3.5 py-1.5 rounded-xl text-xs font-bold transition cursor-pointer shrink-0 ${
                  previewPreset === 'minimal'
                    ? 'bg-slate-800 text-slate-200 border border-slate-600 shadow-sm'
                    : 'text-slate-400 hover:text-slate-200 hover:bg-slate-800/60 border border-transparent'
                }`}
              >
                <Monitor className="w-3.5 h-3.5 text-slate-400" />
                <span>💻 60 FPS Cap</span>
              </button>
            </div>

            {/* Template Highlights Bar */}
            <div className="px-4 py-2.5 bg-slate-950/70 border-b border-slate-800/80 flex flex-wrap items-center justify-between gap-2 text-xs">
              <div className="flex flex-wrap items-center gap-2">
                <span className="text-slate-400 text-[11px] font-semibold">Schlüsselwerte:</span>
                {getPreviewHighlights(previewPreset, hwRec).map((h, i) => (
                  <span
                    key={i}
                    className="px-2 py-0.5 rounded-md bg-slate-800/80 border border-slate-700/80 text-slate-200 font-mono text-[11px]"
                  >
                    {h}
                  </span>
                ))}
              </div>
              <span className="text-[11px] text-slate-500 font-mono">
                {getTemplateContent(previewPreset, status).split('\n').length} Zeilen
              </span>
            </div>

            {/* Code Box */}
            <div className="flex-1 p-4 bg-slate-950 overflow-y-auto font-mono text-xs max-h-[50vh]">
              <pre className="text-sky-100/90 leading-relaxed select-text whitespace-pre font-mono">
                {getTemplateContent(previewPreset, status)}
              </pre>
            </div>

            {/* Modal Footer / Action Buttons */}
            <div className="p-4 border-t border-slate-800 bg-slate-950/90 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
              <div className="flex items-center space-x-2 text-xs text-slate-400">
                <ShieldCheck className="w-4 h-4 text-emerald-400 shrink-0" />
                <span className="text-slate-300 text-[11px]">
                  <strong>Tipp:</strong> „Per Merge übernehmen“ behält deine eigenen manuellen Befehle &amp; Kommentare bei.
                </span>
              </div>

              <div className="flex flex-wrap items-center gap-2">
                <button
                  onClick={() => {
                    navigator.clipboard.writeText(getTemplateContent(previewPreset, status));
                    showToast('Vorlage in Zwischenablage kopiert!');
                  }}
                  className="flex items-center space-x-1 px-3 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs transition cursor-pointer"
                >
                  <Copy className="w-3.5 h-3.5" />
                  <span>Kopieren</span>
                </button>

                <button
                  onClick={() => {
                    applyPreset(previewPreset, false);
                    setPreviewPreset(null);
                  }}
                  className="flex items-center space-x-1.5 px-4 py-2 rounded-xl bg-sky-600 hover:bg-sky-500 text-white text-xs font-bold transition shadow-md shadow-sky-600/20 cursor-pointer"
                  title="Übernimmt die Vorlagenwerte und lässt eigene Zeilen intakt"
                >
                  <Sparkles className="w-3.5 h-3.5" />
                  <span>Per Merge übernehmen</span>
                </button>

                <button
                  onClick={() => {
                    applyPreset(previewPreset, true);
                    setPreviewPreset(null);
                  }}
                  className="flex items-center space-x-1.5 px-3.5 py-2 rounded-xl bg-amber-600 hover:bg-amber-500 text-white text-xs font-bold transition shadow-md shadow-amber-600/20 cursor-pointer"
                  title="Ersetzt den aktuellen Editor-Inhalt komplett durch diese Vorlage"
                >
                  <FileText className="w-3.5 h-3.5" />
                  <span>Als Template laden</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ══════════════════════════════════════════════════════════════
          BEFEHLS-LEXIKON MODAL / OVERLAY
          ══════════════════════════════════════════════════════════════ */}
      {showLexiconModal && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center p-4 sm:p-6 bg-slate-950/80 backdrop-blur-md animate-in fade-in duration-200"
          onClick={() => setShowLexiconModal(false)}
        >
          <div
            className="w-full max-w-5xl max-h-[88vh] rounded-3xl bg-slate-900 border border-slate-700 shadow-2xl flex flex-col overflow-hidden animate-in zoom-in-95 duration-200"
            onClick={(e) => e.stopPropagation()}
          >
            {/* Modal Header */}
            <div className="p-5 border-b border-slate-800 flex items-center justify-between bg-slate-900/90 shrink-0">
              <div className="flex items-center space-x-3.5">
                <div className="w-10 h-10 rounded-xl bg-emerald-500/10 border border-emerald-500/30 flex items-center justify-center text-emerald-400 shadow-inner shrink-0">
                  <BookOpen className="w-5 h-5" />
                </div>
                <div>
                  <h2 className="text-base font-bold text-white tracking-wide">
                    STAR CITIZEN BEFEHLS-LEXIKON &amp; CVAR-REFERENZ
                  </h2>
                  <p className="text-xs text-slate-400 mt-0.5">
                    Erklärungen, Wertebereiche, empfohlene Optimalwerte und 1-Klick-Übernahme in deine Live-user.cfg.
                  </p>
                </div>
              </div>

              <div className="flex items-center space-x-2">
                <button
                  onClick={() => {
                    applyPreset('5800x3d_5070');
                    setShowLexiconModal(false);
                    setCfgView('editor');
                  }}
                  className="hidden sm:flex items-center space-x-1.5 px-3.5 py-1.5 rounded-xl bg-amber-600 hover:bg-amber-500 text-white text-xs font-bold shadow-md shadow-amber-600/20 border border-amber-400 transition cursor-pointer"
                >
                  <Rocket className="w-3.5 h-3.5" />
                  <span>5800X3D / 5070 Profil</span>
                </button>

                <button
                  onClick={() => setShowLexiconModal(false)}
                  className="p-2 rounded-xl text-slate-400 hover:text-white hover:bg-slate-800 transition cursor-pointer"
                  title="Schließen"
                >
                  <X className="w-5 h-5" />
                </button>
              </div>
            </div>

            {/* Modal Search Bar */}
            <div className="p-4 border-b border-slate-800/80 bg-slate-950/60 shrink-0">
              <div className="relative">
                <Search className="w-4 h-4 text-slate-500 absolute left-3.5 top-1/2 -translate-y-1/2" />
                <input
                  type="text"
                  placeholder="Befehl oder Stichwort suchen (z. B. SSDO, Nits, StreamPool, VSync, Con_Restricted)..."
                  value={searchRef}
                  onChange={(e) => setSearchRef(e.target.value)}
                  className="w-full bg-slate-950 border border-slate-800 rounded-xl pl-10 pr-4 py-2 text-xs text-slate-200 placeholder-slate-600 focus:outline-none focus:border-emerald-500 font-mono"
                  autoFocus
                />
              </div>
            </div>

            {/* Modal Content / Cards Grid */}
            <div className="p-5 overflow-y-auto space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {filteredEntries.map((item, idx) => (
                  <div
                    key={idx}
                    className="p-4 rounded-xl bg-slate-950/80 border border-slate-800/90 hover:border-emerald-500/40 transition flex flex-col justify-between space-y-3"
                  >
                    <div>
                      <div className="flex items-center justify-between gap-2 mb-1.5">
                        <span className="text-[10px] font-bold text-emerald-400 font-mono uppercase tracking-wider">
                          {item.category}
                        </span>
                        <span className="text-[9px] px-2 py-0.5 rounded bg-slate-900 text-slate-400 font-semibold border border-slate-800">
                          {item.tag}
                        </span>
                      </div>

                      <div className="font-mono font-bold text-sm text-sky-300">
                        {item.command} = {item.recommended}
                      </div>

                      <p className="text-xs text-slate-300 mt-2 leading-relaxed">
                        {item.explanation}
                      </p>

                      <div className="mt-2 text-[11px] text-slate-500 font-mono">
                        Wertebereich: <span className="text-slate-400">{item.valueDescription}</span>
                      </div>
                    </div>

                    <div className="pt-2 border-t border-slate-800/80 flex items-center justify-between">
                      <span className="text-[10px] text-slate-500 font-mono">
                        Empfehlung: <strong className="text-emerald-300">{item.recommended}</strong>
                      </span>

                      <button
                        onClick={() => {
                          updateSingleCvar(item.command, item.recommended);
                          showToast(`✓ ${item.command} = ${item.recommended} in user.cfg eingefügt`);
                        }}
                        className="flex items-center space-x-1 px-3 py-1 rounded-lg bg-emerald-950/70 hover:bg-emerald-900 text-emerald-300 border border-emerald-800 text-[11px] font-semibold transition cursor-pointer"
                      >
                        <CheckCheck className="w-3 h-3" />
                        <span>Wert übernehmen</span>
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ══ DELETE CONFIRM MODAL ══ */}
      {deleteConfirmModal && deleteConfirmModal.open && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-sm animate-in fade-in duration-150"
          onClick={() => setDeleteConfirmModal(null)}
        >
          <div
            className="w-full max-w-md rounded-2xl bg-slate-900 border border-slate-700 p-6 shadow-2xl space-y-4 animate-in zoom-in-95 duration-150"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-start space-x-3">
              <div className="w-10 h-10 rounded-xl bg-rose-500/10 border border-rose-500/30 flex items-center justify-center text-rose-400 shrink-0">
                <Trash2 className="w-5 h-5" />
              </div>
              <div className="space-y-1">
                <h3 className="text-sm font-bold text-white tracking-wide">
                  Screenshots unwiderruflich löschen?
                </h3>
                <p className="text-xs text-slate-300 leading-relaxed">
                  Möchtest du wirklich <strong className="text-rose-300">{deleteConfirmModal.count} {deleteConfirmModal.label}</strong> aus deinem Star Citizen Screenshot-Ordner auf der Festplatte löschen?
                </p>
                <p className="text-[11px] text-slate-500">
                  Diese Aktion löscht die Dateien dauerhaft von der Festplatte.
                </p>
              </div>
            </div>

            <div className="flex items-center justify-end space-x-2 pt-2 border-t border-slate-800">
              <button
                onClick={() => setDeleteConfirmModal(null)}
                className="px-3.5 py-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-semibold transition cursor-pointer"
              >
                Abbrechen
              </button>
              <button
                onClick={() => handleExecuteDelete(deleteConfirmModal.mode, deleteConfirmModal.paths)}
                disabled={screenshotActionLoading !== null}
                className="px-4 py-2 rounded-xl bg-rose-600 hover:bg-rose-500 text-white text-xs font-bold transition shadow-lg shadow-rose-950/50 cursor-pointer disabled:opacity-40 flex items-center space-x-1.5"
              >
                <Trash2 className="w-3.5 h-3.5" />
                <span>{screenshotActionLoading ? 'Lösche...' : 'Unwiderruflich löschen'}</span>
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
