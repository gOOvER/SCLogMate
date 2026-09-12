// Type definitions for Photino window.external
declare global {
  interface External {
    sendMessage?: (message: string) => void;
    receiveMessage?: (callback: (message: string) => void) => void;
  }
}

export interface IpcMessage<T = any> {
  id?: string;
  type: string;
  payload?: T;
  error?: string;
}

export interface AppStatus {
  version: string;
  isLiveWatching: boolean;
  logPath: string | null;
  activeSessionName: string | null;
  dbSessionCount: number;
  totalIncome: number;
  totalSpend: number;
  totalNet: number;
  lastEventTime: string | null;
  chatOcrEnabled?: boolean;
}

export interface ChatMessageDto {
  id: number;
  timestamp: string;
  sessionId?: string;
  channel: string;
  sender: string;
  recipient?: string;
  message: string;
  rawOcr?: string;
  isFlagged: boolean;
  createdAt: string;
}

export interface HudTelemetry {
  isGameRunning: boolean;
  pilotName: string;
  pilotAvatarUrl?: string;
  pilotTitle?: string;
  pilotOrgName?: string;
  citizenRecord?: string;
  pilotOrgSid?: string;
  pilotOrgRank?: string;
  pilotOrgLogoUrl?: string;
  pilotEnlisted?: string;
  pilotProfileUrl?: string;
  serverRegionCode: 'EU' | 'US' | 'AUS' | 'ASIA' | 'PU' | 'OTHER' | string;
  serverRegionName: string;
  serverRegionFlag?: string;
  serverShard: string;
  serverShardNumber?: string;
  serverVersion: string;
  serverPingMs?: number | null;
  locationName: string;
  locationSystem: string;
  locationBody: string;
  locationType: string;
  isArmistice: boolean;
  jurisdiction: string;
  shipName: string;
  shipFlightInfo: string;
  balance: number;
  sessionIncome: number;
  sessionSpend: number;
  sessionNet: number;
  autoOcrEnabled: boolean;
  activeMissionTitle: string;
  activeMissionGiver: string;
  activeMissionReward: number;
  activeMissionStatus: string;
  sessionSpanText: string;
  selectedSession: string;
}

export interface PilotProfile {
  handle: string;
  citizenRecord: string;
  title: string;
  avatarUrl?: string;
  enlisted: string;
  fluency: string;
  orgName?: string;
  orgSid?: string;
  orgRank?: string;
  orgLogoUrl?: string;
  profileUrl: string;
  website?: string;
  bio?: string;
  isVerified: boolean;
}

export interface SessionSummary {
  id: number;
  name: string;
  startTime: string;
  endTime: string;
  duration: string;
  income: number;
  spend: number;
  net: number;
  sales: number;
  trade: number;
  deaths: number;
  missions: number;
  ships: string[];
  lastLocation: string;
}

export interface LogEventItem {
  id: string;
  timestamp: string;
  category: string;
  kind?: string;
  kindText?: string;
  icon?: string;
  title: string;
  description: string;
  amount?: number;
  ship?: string;
  rawText?: string;
}

export interface FinanceChartPointDto {
  time: string;
  balance: number;
  income: number;
  spend: number;
  delta: number;
  label: string;
}

export interface WarehouseItemDto {
  location: string;
  locationCode: string;
  system: string;
  parentBody: string;
  itemClass: string;
  itemName: string;
  category: string;
  quantity: number;
  lastUpdated: string;
  icon: string;
  locationDisplay: string;
}

export interface WarehouseLocationDto {
  locationName: string;
  locationCode: string;
  system: string;
  parentBody: string;
  totalItems: number;
  uniqueItemTypes: number;
  icon: string;
}

export interface FinanceOverviewDto {
  scope?: 'all' | 'current';
  totalIncome: number;
  totalSpend: number;
  totalNet: number;
  liveBalance?: number;
  totalCargoAuec?: number;
  totalCargoScu?: number;
  profitMargin?: number;
  sales: number;
  trade: number;
  missionsReward: number;
  purchases: number;
  transferIn: number;
  transferOut: number;
  ledger: LogEventItem[];
  cargo: LogEventItem[];
  topExpenses: LogEventItem[];
  topIncome?: LogEventItem[];
  timelinePoints?: FinanceChartPointDto[];
}

export interface FleetStatDto {
  shipName: string;
  flights: number;
  quantumJumps: number;
  losses: number;
  lastUsed: string;
}

export interface FleetShipDto {
  name: string;
  rawCode: string;
  manufacturer: string;
  manufacturerBadge: string;
  manufacturerColor: string;
  role: string;
  estimatedValueAuec: number;
  flightCount: number;
  quantumJumps: number;
  lossCount: number;
  lastFlown: string;
  isCurrent: boolean;
  isInHangar: boolean;
  isPledgeBought: boolean;
  pledgeValueUsd: number;
  insuranceType: string;
  acquisitionType: string;
  customNotes: string;
}

export interface CatalogShipDto {
  name: string;
  manufacturer: string;
  role: string;
  valueAuec: number;
  pledgeUsd: number;
  defaultInsurance: string;
}

export interface FleetResponseDto {
  ships: FleetShipDto[];
  catalog: CatalogShipDto[];
  totalFleetValueAuec: number;
  totalFleetPledgeUsd: number;
  totalFlights: number;
  totalQuantumJumps: number;
  hangarCount: number;
  flownCount: number;
}

export interface MissionItemDto {
  id: string;
  title: string;
  contractor: string;
  faction: string;
  missionType: string;
  baseReward: number;
  reputationGain: number;
  isIllegal: boolean;
  starSystems: string;
  blueprints: string[];
  description: string;
  isActive?: boolean;
  isCompleted?: boolean;
  time?: string;
}

export interface MissionsResponseDto {
  active: MissionItemDto[];
  history: MissionItemDto[];
  catalog: MissionItemDto[];
}

export interface FactionReputationDto {
  id: string;
  name: string;
  shortName: string;
  category: string;
  icon: string;
  system: string;
  description: string;
  currentXp: number;
  completedMissions: number;
  currentLevel: number;
  levelTitle: string;
  progressPercent: number;
  progressText: string;
}

export interface BlueprintDto {
  id: string;
  name: string;
  category: string;
  subCategory: string;
  rarity: string;
  requiredMaterials: string;
  unlockInfo: string;
  isLearned: boolean;
  learnedDate?: string;
}

export interface LoadoutSlotDto {
  slotKey: string;
  slotName: string;
  icon: string;
  itemName: string;
  rawClass?: string;
  armorClass: string;
  damageReduction: number;
  tempRange: string;
  badgeColor: string;
  isEquipped: boolean;
  lastEquipped?: string;
}

export interface StarmapObjectDto {
  id: string;
  name: string;
  system: string;
  parentId?: string;
  type: string;
  orbitRadius: number;
  orbitAngleDeg: number;
  colorHex: string;
  size: number;
  hasArmistice: boolean;
  jurisdiction: string;
  securityLevel: string;
  specialization: string;
  resources: string;
  description: string;
  targetSystem?: string;
  relX: number;
  relY: number;
}

export interface QuantumDriveDto {
  name: string;
  sizeClass: string;
  topSpeedKmS: number;
  displayText: string;
}

export interface QuantumRouteResultDto {
  fromId: string;
  fromName: string;
  toId: string;
  toName: string;
  driveName: string;
  distKm: number;
  distGm: number;
  flightTimeSeconds: number;
  flightTimeFormatted: string;
}

export interface StarmapResponseDto {
  currentSystem: string;
  objects: StarmapObjectDto[];
  drives: QuantumDriveDto[];
}

export interface PlaceItemDto {
  id: string;
  name: string;
  system: string;
  parentBody: string;
  type: string;
  icon: string;
  securityLevel: string;
  hasArmistice: boolean;
  specialization: string;
  description: string;
}

export interface FlightTimelineItemDto {
  id: string;
  time: string;
  relativeTime: string;
  kind: string;
  title: string;
  subtitle: string;
  ship?: string;
  location?: string;
  isMajor: boolean;
}

export interface FlightShipStatDto {
  ship: string;
  sorties: number;
  flightMinutes: number;
  flightTimeText: string;
}

export interface FlightRecorderDto {
  totalDistanceGm: number;
  totalDistanceKm: number;
  totalDistanceText: string;
  flightDurationText: string;
  seatFlightDurationText?: string;
  inGameDurationText?: string;
  menuDurationText?: string;
  quantumJumps: number;
  sortieCount: number;
  shipLosses: number;
  visitedBodies: string[];
  usedShips: string[];
  shipStats?: FlightShipStatDto[];
  timeline: FlightTimelineItemDto[];
}

export interface PoiDistanceInfo {
  id: number;
  name: string;
  category: string;
  body: string;
  distanceMeters: number;
  formattedDistance: string;
}

export interface CopiedLocationReading {
  x: number;
  y: number;
  z: number;
  timestamp: string;
  rawText: string;
  detectedSystem: string;
  nearestPois: PoiDistanceInfo[];
}

export interface UserPoiDto {
  id: number;
  system: string;
  body: string;
  name: string;
  notes: string;
  category: string;
  color: string;
  createdAt: string;
  posX?: number | null;
  posY?: number | null;
  posZ?: number | null;
  hasCoordinates: boolean;
  coordinatesFormatted: string;
  distanceFormatted?: string | null;
}

export interface MiningHaulDto {
  id: number;
  sessionId?: string | null;
  materialName: string;
  scuQuantity: number;
  refineryLocation: string;
  method: string;
  yieldPercent: number;
  costAuec: number;
  submittedAt: string;
  readyAt: string;
  durationSeconds: number;
  remainingSeconds: number;
  isTimerCompleted: boolean;
  status: string; // Refining, Ready, Collected, Sold
  yieldScu: number;
  soldAuec: number;
}

export interface TradeRouteDto {
  id: string;
  commodity: string;
  origin: string;
  destination: string;
  system: string;
  buyPricePerScu: number;
  sellPricePerScu: number;
  profitPerScu: number;
  roiPercent: number;
  maxScu: number;
  investmentAuec: number;
  totalProfitAuec: number;
  riskLevel: string;
}

export interface SalvagePriceSummaryDto {
  materialName: string;
  category: string;
  bestSellLocation: string;
  bestSellPricePerScu: number;
  avgSellPricePerScu: number;
  system: string;
}

export interface CombatCategoryStatDto {
  label: string;
  count: number;
  percent: number;
  color: string;
}

export interface DangerZoneDto {
  location: string;
  system: string;
  incidentCount: number;
  deaths: number;
  shipLosses: number;
  threatLevel: string;
}

export interface CasualtyIncidentDto {
  id: string;
  timestamp: string;
  session: string;
  type: string;
  title: string;
  detail: string;
  ship?: string;
  location: string;
  estimatedCostAuec: number;
}

export interface CombatAnalyticsDto {
  totalKills: number;
  totalDeaths: number;
  kdRatio: number;
  shipLosses: number;
  medRespawns: number;
  estimatedKitLossAuec: number;
  estimatedShipClaimLossAuec: number;
  estimatedTotalLossAuec: number;
  deathCauses: CombatCategoryStatDto[];
  dangerZones: DangerZoneDto[];
  recentCasualties: CasualtyIncidentDto[];
}

export interface RsResourceDto {
  name: string;
  baseRs: number;
  tier: string;
  rarity: string;
  method: string;
  estimatedPricePerScu: number;
  locations: string[];
}

export interface RsMatchDto {
  resourceName: string;
  baseRs: number;
  tier: string;
  rarity: string;
  method: string;
  estimatedPricePerScu: number;
  nodes: number;
  isExact: boolean;
  errorPct: number;
  scannedRs: number;
  estimatedClusterValue: number;
}

export interface MarketCommodityDto {
  name: string;
  category: string;
  tier: string;
  avgBuyPrice: number;
  avgSellPrice: number;
  margin: number;
  bestBuyLocation: string;
  bestSellLocation: string;
}

export interface KeybindBackupItemDto {
  name: string;
  folderPath: string;
  createdAt: string;
  fileCount: number;
  locationType: string;
  sizeFormatted: string;
}

export interface ConfigBackupItemDto {
  name: string;
  filePath: string;
  createdAt: string;
  locationType: string;
  sizeFormatted: string;
}

export interface ToolsStatusDto {
  shaderCacheMb: number;
  crashDumpsMb: number;
  userCfgPath: string;
  userCfgExists: boolean;
  userCfgContent: string;
  totalRamGb: number;
  ramStatus: string;
  driveName: string;
  freeDiskGb: number;
  pagefileStatus: string;
  keybindBackups: string[];
  cloudStoragePath?: string;
  keybindItems?: KeybindBackupItemDto[];
  configBackups?: ConfigBackupItemDto[];
  keybindsDir?: string;
  configDir?: string;
}

export interface ScanRegionDto {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface OcrRegionsConfig {
  walletRegion: ScanRegionDto | null;
  contractRegion: ScanRegionDto | null;
  rsScanRegion: ScanRegionDto | null;
  chatRegion?: ScanRegionDto | null;
  defaultWalletRegion: ScanRegionDto;
  defaultContractRegion: ScanRegionDto;
  defaultRsRegion: ScanRegionDto;
  defaultChatRegion?: ScanRegionDto;
  screenWidth: number;
  screenHeight: number;
  isWalletScanBoxVisible: boolean;
  isContractScanBoxVisible: boolean;
}

export interface OcrTestResult {
  success: boolean;
  target: string;
  recognizedText: string;
  extractedValue?: number | null;
  durationMs: number;
  region?: ScanRegionDto;
  error?: string;
}

export interface SettingsDto {
  logPath?: string;
  autoOcrEnabled: boolean;
  uexApiKey?: string;
  overlayEnabled: boolean;
  overlayOpacity: number;
  toastEnabled: boolean;
  toastBlueprintEnabled: boolean;
  toastMissionEnabled: boolean;
  toastReputationEnabled: boolean;
  toastRefineryEnabled: boolean;
  toastElevatorEnabled: boolean;
  toastShipDestructionEnabled: boolean;
  auroraIntegrationEnabled: boolean;
  auroraVolume: number;
  rsTargetAlertEnabled: boolean;
  rsTargetSoundEnabled: boolean;
  walletRegion?: ScanRegionDto | null;
  contractRegion?: ScanRegionDto | null;
  rsScanRegion?: ScanRegionDto | null;

  // Wipe-Filter Settings
  wipeFilterEnabled?: boolean;
  wipeDateString?: string;
  wipeFilterMoney?: boolean;
  wipeFilterContracts?: boolean;
  wipeFilterFleet?: boolean;
  wipeFilterBlueprints?: boolean;

  // General & System
  selectedFontFamily?: string;
  appLanguage?: string;
  minimizeToTrayOnClose?: boolean;
  autostartEnabled?: boolean;
  debugMode?: boolean;
}

export interface DetectedPath {
  path: string;
  channel: string;
  lastModified: string;
  sizeBytes: number;
  isCurrent: boolean;
}

export interface LogStatus {
  currentLogPath: string | null;
  channel: string;
  exists: boolean;
  sizeBytes: number;
  formattedSize: string;
  lastModified: string | null;
  isLiveWatching: boolean;
  activeSession: string;
  detectedPaths: DetectedPath[];
  backupsCount: number;
  archiveCount: number;
  parserVersion: number;
  schemaVersion: number;
}

export interface ScanProgress {
  current: number;
  total: number;
  percent: number;
  currentFileName: string;
  isCompleted: boolean;
  indexedSessions?: number;
  totalEvents?: number;
  isDbUpdate?: boolean;
  updateReason?: string;
}

export interface UpdateInfoDto {
  updateAvailable: boolean;
  currentVersion: string;
  newVersion: string;
  releaseNotes?: string;
  htmlUrl?: string;
}

export interface DbDiagnostics {
  databasePath: string;
  databaseSizeBytes: number;
  formattedSize: string;
  sqliteVersion: string;
  journalMode: string;
  installedSchemaVersion: number;
  currentSchemaVersion: number;
  installedParserVersion: number;
  currentParserVersion: number;
  sessionCount: number;
  eventCount: number;
  contractCount: number;
  fleetShipCount: number;
  poiCount: number;
  reputationCount: number;
  warehouseItemCount: number;
  integrityCheckOk: boolean;
  integrityMessage: string;
  checkedAt: string;
  isSynchronous: boolean;
}

export interface UnknownEventsData {
  path: string;
  lines: string[];
}

export interface WikiStoreLocation {
  storeName: string;
  location: string;
  priceAuec: number;
  rentPrice1dAuec?: number | null;
}

export interface WikiInfo {
  name: string;
  category: string;
  manufacturer: string;
  role: string;
  type: string;
  focus: string;
  size: string;
  crewMin?: number | null;
  crewMax?: number | null;
  cargoScu?: number | null;
  quantumFuel?: number | null;
  length?: number | null;
  beam?: number | null;
  height?: number | null;
  mass?: number | null;
  descriptionDe: string;
  descriptionEn: string;
  bestDescription: string;
  descriptionHeader: string;
  imageUrl: string;
  thumbnailUrl: string;
  localImageBase64?: string;
  webUrl: string;
  pledgeUrl: string;
  msrp?: number | null;
  productionStatus: string;
  specs: Record<string, string>;
  storeLocations: WikiStoreLocation[];
}

type EventListener = (payload: any) => void;

class PhotinoBridge {
  private isAvailable: boolean = false;
  private eventListeners: Map<string, Set<EventListener>> = new Map();
  private pendingRequests: Map<string, { resolve: (val: any) => void; reject: (err: any) => void; timeout: any }> = new Map();

  constructor() {
    this.init();
  }

  private init() {
    if (typeof window !== 'undefined' && window.external && typeof window.external.sendMessage === 'function') {
      this.isAvailable = true;

      // Register receiver
      if (typeof window.external.receiveMessage === 'function') {
        window.external.receiveMessage((rawMessage: string) => {
          this.handleIncomingMessage(rawMessage);
        });
      }

      // Send client_ready handshake signal to C# backend
      try {
        window.external.sendMessage(JSON.stringify({ type: 'client_ready' }));
      } catch (err) {
        console.error('Failed to send client_ready signal:', err);
      }
    } else {
      console.warn('[PhotinoBridge] Not running inside Photino WebView. Operating in standalone/mock mode.');
    }
  }

  private handleIncomingMessage(raw: string) {
    try {
      const msg: IpcMessage = JSON.parse(raw);

      // Handle response to a pending request
      if (msg.id && this.pendingRequests.has(msg.id)) {
        const req = this.pendingRequests.get(msg.id)!;
        clearTimeout(req.timeout);
        this.pendingRequests.delete(msg.id);

        if (msg.error) {
          req.reject(new Error(msg.error));
        } else {
          req.resolve(msg.payload);
        }
        return;
      }

      // Handle event broadcast
      const listeners = this.eventListeners.get(msg.type);
      if (listeners) {
        listeners.forEach((fn) => fn(msg.payload));
      }
    } catch (e) {
      console.error('[PhotinoBridge] Failed to parse incoming message:', e, raw);
    }
  }

  public get isConnected(): boolean {
    return this.isAvailable;
  }

  /** Send a command to C# and await a response */
  public sendRequest<TResponse = any, TPayload = any>(type: string, payload?: TPayload, timeoutMs = 12000): Promise<TResponse> {
    return new Promise((resolve, reject) => {
      const id = `${Date.now()}_${Math.random().toString(36).substring(2, 9)}`;

      if (!this.isAvailable) {
        // Fallback / mock data when opened directly in browser without C# backend
        return this.handleMockRequest(type, payload).then(resolve).catch(reject);
      }

      const timeout = setTimeout(() => {
        if (this.pendingRequests.has(id)) {
          this.pendingRequests.delete(id);
          reject(new Error(`Photino IPC timeout after ${timeoutMs}ms for ${type}`));
        }
      }, timeoutMs);

      this.pendingRequests.set(id, { resolve, reject, timeout });

      const msg: IpcMessage<TPayload> = { id, type, payload };
      window.external.sendMessage!(JSON.stringify(msg));
    });
  }

  /** Shorthand for sendRequest */
  public send<TResult = any, TPayload = any>(type: string, payload?: TPayload): Promise<TResult> {
    return this.sendRequest<TResult, TPayload>(type, payload);
  }

  /** Fire-and-forget notification to C# */
  public postMessage<TPayload = any>(type: string, payload?: TPayload) {
    if (!this.isAvailable) {
      console.log(`[Photino Mock Send] ${type}:`, payload);
      return;
    }
    const msg: IpcMessage<TPayload> = { type, payload };
    window.external.sendMessage!(JSON.stringify(msg));
  }

  /** Subscribe to events broadcast from C# */
  public on<T = any>(type: string, listener: (payload: T) => void): () => void {
    if (!this.eventListeners.has(type)) {
      this.eventListeners.set(type, new Set());
    }
    this.eventListeners.get(type)!.add(listener);

    // Return unbind function
    return () => {
      this.eventListeners.get(type)?.delete(listener);
    };
  }

  public checkUpdate(): Promise<UpdateInfoDto> {
    return this.sendRequest<UpdateInfoDto>('check_update');
  }

  public applyUpdate(): Promise<{ success: boolean; message?: string }> {
    return this.sendRequest<{ success: boolean; message?: string }>('apply_update');
  }

  public openExternalUrl(url: string): void {
    this.send('open_external_url', { url });
  }

  public getPilotDossier(handle?: string): Promise<PilotProfile> {
    return this.sendRequest<PilotProfile>('get_pilot_dossier', { handle });
  }

  public lookupWiki(query: string): Promise<WikiInfo | null> {
    return this.sendRequest<WikiInfo | null>('lookup_wiki', { query });
  }

  public searchWiki(query: string, category?: string, limit = 25): Promise<WikiInfo[]> {
    return this.sendRequest<WikiInfo[]>('search_wiki', { query, category, limit });
  }

  public getWikiSpecs(name: string): Promise<WikiInfo | null> {
    return this.sendRequest<WikiInfo | null>('get_wiki_specs', { name });
  }

  public getChatMessages(filters?: {
    session?: string;
    channel?: string;
    sender?: string;
    search?: string;
    flaggedOnly?: boolean;
    limit?: number;
  }): Promise<ChatMessageDto[]> {
    return this.sendRequest<ChatMessageDto[]>('get_chat_messages', filters);
  }

  public scanChatNow(): Promise<{ success: boolean; count: number; messages: ChatMessageDto[] }> {
    return this.sendRequest<{ success: boolean; count: number; messages: ChatMessageDto[] }>('scan_chat_now');
  }

  public toggleChatOcr(enabled?: boolean): Promise<{ enabled: boolean }> {
    return this.sendRequest<{ enabled: boolean }>('toggle_chat_ocr', { enabled });
  }

  public flagChatMessage(id: number, isFlagged: boolean): Promise<{ success: boolean; id: number; isFlagged: boolean }> {
    return this.sendRequest<{ success: boolean; id: number; isFlagged: boolean }>('flag_chat_message', { id, isFlagged });
  }

  public clearChatMessages(session?: string): Promise<{ success: boolean }> {
    return this.sendRequest<{ success: boolean }>('clear_chat_messages', { session });
  }

  public exportPlayerReport(params: {
    suspect?: string;
    category?: string;
    description?: string;
    messageIds?: number[];
  }): Promise<{ markdown: string }> {
    return this.sendRequest<{ markdown: string }>('export_player_report', params);
  }

  // Mock implementation for browser-only development
  private async handleMockRequest(type: string, payload?: any): Promise<any> {
    switch (type) {
      case 'get_chat_messages':
        return [
          {
            id: 1,
            timestamp: new Date(Date.now() - 1000 * 60 * 12).toISOString(),
            sessionId: '__live__',
            channel: 'Global',
            sender: 'Cpt_Starhawk',
            message: 'Need escort from Seraphim to GrimHEX, paying 50k aUEC.',
            isFlagged: false,
            createdAt: new Date().toISOString()
          },
          {
            id: 2,
            timestamp: new Date(Date.now() - 1000 * 60 * 8).toISOString(),
            sessionId: '__live__',
            channel: 'Global',
            sender: 'Shadow_Viper',
            message: 'Incoming hostile mantis at OM-1! Snare active, beware traders!',
            isFlagged: false,
            createdAt: new Date().toISOString()
          },
          {
            id: 3,
            timestamp: new Date(Date.now() - 1000 * 60 * 4).toISOString(),
            sessionId: '__live__',
            channel: 'Party',
            sender: 'Wingman_Fox',
            message: 'Forming up on your wing, quantum drive spooled.',
            isFlagged: false,
            createdAt: new Date().toISOString()
          },
          {
            id: 4,
            timestamp: new Date(Date.now() - 1000 * 60 * 2).toISOString(),
            sessionId: '__live__',
            channel: 'Global',
            sender: 'GriefMaster_99',
            message: 'Pad ramming everyone at Port Tressler, try and stop me losers',
            isFlagged: true,
            createdAt: new Date().toISOString()
          }
        ] as ChatMessageDto[];

      case 'scan_chat_now':
        return {
          success: true,
          count: 1,
          messages: [
            {
              id: Date.now(),
              timestamp: new Date().toISOString(),
              sessionId: '__live__',
              channel: 'Global',
              sender: 'Reclaimer_Chief',
              message: 'Selling 120 SCU RMC at Area18 TDD.',
              isFlagged: false,
              createdAt: new Date().toISOString()
            }
          ]
        };

      case 'toggle_chat_ocr':
        return { enabled: payload?.enabled ?? true };

      case 'flag_chat_message':
        return { success: true, id: payload?.id, isFlagged: payload?.isFlagged };

      case 'clear_chat_messages':
        return { success: true };

      case 'export_player_report':
        return {
          markdown: `# Cloud Imperium Games — Player Support Incident Report\n\n**Report Date (UTC):** ${new Date().toISOString()}\n**Category:** ${payload?.category || 'Griefing'}\n**Reported Player:** ${payload?.suspect || 'GriefMaster_99'}\n\n### Incident Description & Summary\n${payload?.description || 'Pad ramming at Port Tressler'}\n\n### In-Game Chat Evidence Transcript\n| Time | Channel | Sender | Message |\n|---|---|---|---|\n| ${new Date().toLocaleTimeString()} | [Global] | GriefMaster_99 | Pad ramming everyone at Port Tressler |`
        };

      case 'check_update':
        return {
          updateAvailable: false,
          currentVersion: 'v1.0.0-rc2',
          newVersion: 'v1.0.0-rc2',
          releaseNotes: '',
          htmlUrl: 'https://github.com/gOOvER/SCLogMate/releases',
        } as UpdateInfoDto;

      case 'apply_update':
        return { success: true, message: 'Update gestartet' };

      case 'open_external_url':
        if (payload?.url) window.open(payload.url, '_blank');
        return { ok: true };

      case 'get_pilot_dossier':
        return {
          handle: payload?.handle || 'gOOvER',
          citizenRecord: '#593923',
          title: 'High Admiral',
          avatarUrl: 'https://robertsspaceindustries.com/media/000zndy8xaqxjr/heap_infobox/OldNoob.jpg',
          enlisted: 'Sep 13, 2014',
          fluency: 'English, German',
          orgName: 'Stellanebula Project',
          orgSid: 'SNPX',
          orgRank: 'Recruit',
          orgLogoUrl: 'https://robertsspaceindustries.com/media/5txttytjzckkzr/heap_infobox/SNPX-Logo.png',
          profileUrl: 'https://robertsspaceindustries.com/citizens/gOOvER',
          bio: 'Star Citizen Enthusiast & Space Commander',
          isVerified: true,
        } as PilotProfile;

      case 'get_hud':
      case 'select_session':
      case 'trigger_ocr':
      case 'toggle_auto_ocr':
        return {
          isGameRunning: true,
          pilotName: 'Commander Torsten',
          pilotAvatarUrl: 'https://robertsspaceindustries.com/media/000zndy8xaqxjr/heap_infobox/OldNoob.jpg',
          pilotTitle: 'High Admiral',
          pilotOrgName: 'Stellanebula Project',
          serverRegionCode: 'EU',
          serverRegionName: 'Europa',
          serverShard: '#1042-EU',
          serverVersion: 'SC 3.24.3-LIVE',
          serverPingMs: 28,
          locationName: 'Port Tressler · microTech',
          locationSystem: 'Stanton',
          locationBody: 'microTech',
          locationType: 'Raumstation',
          isArmistice: true,
          jurisdiction: 'UEE Protektorat',
          shipName: 'Anvil Carrack',
          shipFlightInfo: 'Flugbereit · 14 Flüge · 8 QT-Sprünge',
          balance: type === 'trigger_ocr' ? 2525000 : 2500000,
          sessionIncome: 145000,
          sessionSpend: 32500,
          sessionNet: 112500,
          autoOcrEnabled: type === 'toggle_auto_ocr' ? false : true,
          activeMissionTitle: 'Kein aktiver Auftrag',
          activeMissionGiver: '—',
          activeMissionReward: 0,
          activeMissionStatus: 'Bereit',
          sessionSpanText: '11.09. 14:20 → 16:45 (2h 25m)',
          selectedSession: payload?.session || '__live__',
        } as HudTelemetry;

      case 'get_status':
        return {
          version: '1.0.0-photino-preview',
          isLiveWatching: true,
          logPath: 'C:\\Games\\Roberts Space Industries\\StarCitizen\\LIVE\\Game.log',
          activeSessionName: 'Game.log (Aktuell)',
          dbSessionCount: 24,
          totalIncome: 4250000,
          totalSpend: 980000,
          totalNet: 3270000,
          lastEventTime: new Date().toLocaleTimeString(),
        } as AppStatus;

      case 'get_sessions':
        return [
          {
            id: 1,
            name: 'Pyro Mining & Salvage Run',
            startTime: '10.09. 17:15',
            endTime: '10.09. 18:45',
            duration: '1h 30m',
            income: 850000,
            spend: 120000,
            net: 730000,
            sales: 850000,
            trade: 0,
            deaths: 0,
            missions: 3,
            ships: ['Drake Vulture', 'Cutlass Black'],
            lastLocation: 'Checkmate Station (Pyro)',
          },
          {
            id: 2,
            name: 'Stanton Cargo Hauling',
            startTime: '09.09. 20:00',
            endTime: '09.09. 22:15',
            duration: '2h 15m',
            income: 1400000,
            spend: 400000,
            net: 1000000,
            sales: 0,
            trade: 1400000,
            deaths: 0,
            missions: 6,
            ships: ['Crusader C2 Hercules'],
            lastLocation: 'Lorville (Hurston)',
          },
        ] as SessionSummary[];

      case 'get_events':
        return [
          {
            id: 'e1',
            timestamp: '10.09. 18:42:15',
            category: 'wallet',
            title: 'Verkauf',
            description: 'RMC (Recycled Material Composite) ×24 SCU',
            amount: 345600,
            ship: 'Drake Vulture',
          },
          {
            id: 'e2',
            timestamp: '10.09. 18:25:00',
            category: 'ship',
            title: 'Quantum-Sprung',
            description: 'Sprung nach OM-1 (Aberdeen)',
            ship: 'Drake Vulture',
          },
          {
            id: 'e3',
            timestamp: '10.09. 18:10:20',
            category: 'mission',
            title: 'Auftrag abgeschlossen',
            description: 'Unverified Salvage Claim',
            amount: 65000,
          },
        ] as LogEventItem[];

      case 'get_finance':
        return {
          totalIncome: 4250000,
          totalSpend: 980000,
          totalNet: 3270000,
          sales: 1850000,
          trade: 1600000,
          missionsReward: 800000,
          purchases: 750000,
          transferIn: 0,
          transferOut: 230000,
          ledger: [],
          cargo: [],
          topExpenses: [],
        } as FinanceOverviewDto;

      case 'get_warehouse':
        return {
          locations: [
            {
              locationName: 'Everus Harbor',
              locationCode: 'EH_HUR',
              system: 'Stanton',
              parentBody: 'Hurston',
              totalItems: 42,
              uniqueItemTypes: 8,
              icon: '🛰️',
            },
            {
              locationName: 'Lorville',
              locationCode: 'LOR_HUR',
              system: 'Stanton',
              parentBody: 'Hurston',
              totalItems: 115,
              uniqueItemTypes: 24,
              icon: '🪐',
            },
          ] as WarehouseLocationDto[],
          items: [
            {
              location: 'Everus Harbor',
              locationCode: 'EH_HUR',
              system: 'Stanton',
              parentBody: 'Hurston',
              itemClass: 'scitem_multitool',
              itemName: 'Pyro RYT Multi-Tool',
              category: 'Werkzeuge',
              quantity: 4,
              lastUpdated: '10.09.2026 17:30',
              icon: '🔧',
              locationDisplay: 'Everus Harbor · Hurston (Stanton)',
            },
            {
              location: 'Everus Harbor',
              locationCode: 'EH_HUR',
              system: 'Stanton',
              parentBody: 'Hurston',
              itemClass: 'scitem_medpen',
              itemName: 'Hemozal MedPen',
              category: 'Verbrauchsgüter',
              quantity: 24,
              lastUpdated: '10.09.2026 17:35',
              icon: '💊',
              locationDisplay: 'Everus Harbor · Hurston (Stanton)',
            },
          ] as WarehouseItemDto[],
        };

      case 'adjust_warehouse_qty':
      case 'delete_warehouse_item':
      case 'clear_warehouse_location':
        return this.handleMockRequest('get_warehouse', payload);

      case 'get_fleet':
      case 'toggle_ship_hangar':
      case 'add_catalog_ship_to_hangar':
      case 'remove_ship_from_hangar':
      case 'cycle_ship_acquisition':
      case 'cycle_ship_insurance':
      case 'update_ship_pledge':
      case 'update_ship_notes':
        return {
          ships: [
            {
              name: 'Drake Vulture',
              rawCode: 'DRAK_Vulture',
              manufacturer: 'Drake Interplanetary',
              manufacturerBadge: 'DRAKE',
              manufacturerColor: '#4ADE80',
              role: 'Bergung & Salvage',
              estimatedValueAuec: 2450000,
              flightCount: 18,
              quantumJumps: 42,
              lossCount: 0,
              lastFlown: '11.09.2026 16:30',
              isCurrent: true,
              isInHangar: true,
              isPledgeBought: true,
              pledgeValueUsd: 175,
              insuranceType: 'LTI (Lifetime)',
              acquisitionType: 'Pledge Store',
              customNotes: 'Main Solo Salvage Ship mit Dual-Scraper',
            },
            {
              name: 'Aegis Vanguard Warden',
              rawCode: 'AEGS_Vanguard',
              manufacturer: 'Aegis Dynamics',
              manufacturerBadge: 'AEGIS',
              manufacturerColor: '#38BDF8',
              role: 'Schwerer Langstrecken-Jäger',
              estimatedValueAuec: 3800000,
              flightCount: 26,
              quantumJumps: 68,
              lossCount: 2,
              lastFlown: '10.09.2026 21:15',
              isCurrent: false,
              isInHangar: true,
              isPledgeBought: true,
              pledgeValueUsd: 260,
              insuranceType: '120 Monate (IAE)',
              acquisitionType: 'Pledge Store',
              customNotes: 'Bounty Hunter Setup (ERT/VHRT)',
            },
            {
              name: 'Crusader C1 Spirit',
              rawCode: 'CRUS_Spirit_C1',
              manufacturer: 'Crusader Industries',
              manufacturerBadge: 'CRUSADER',
              manufacturerColor: '#F59E0B',
              role: 'Mittlerer Frachter / Allrounder',
              estimatedValueAuec: 3100000,
              flightCount: 9,
              quantumJumps: 22,
              lossCount: 1,
              lastFlown: '08.09.2026 14:05',
              isCurrent: false,
              isInHangar: true,
              isPledgeBought: false,
              pledgeValueUsd: 0,
              insuranceType: 'Standard In-Game',
              acquisitionType: 'In-Game (aUEC)',
              customNotes: 'Gekauft in New Babbage Astro Armada',
            },
            {
              name: 'Anvil Arrow',
              rawCode: 'ANVL_Arrow',
              manufacturer: 'Anvil Aerospace',
              manufacturerBadge: 'ANVIL',
              manufacturerColor: '#EF4444',
              role: 'Leichter Abfangjäger',
              estimatedValueAuec: 975000,
              flightCount: 12,
              quantumJumps: 18,
              lossCount: 3,
              lastFlown: '05.09.2026 19:40',
              isCurrent: false,
              isInHangar: false,
              isPledgeBought: false,
              pledgeValueUsd: 0,
              insuranceType: 'Miet-Versicherung',
              acquisitionType: 'Miete (Rental)',
              customNotes: 'Gemietet für PvP Training',
            },
          ],
          catalog: [
            { name: 'Aegis Avenger Titan', manufacturer: 'Aegis Dynamics', role: 'Leichter Frachter / Starter', valueAuec: 785600, pledgeUsd: 60, defaultInsurance: '6 Monate' },
            { name: 'Aegis Gladius', manufacturer: 'Aegis Dynamics', role: 'Leichter Jäger', valueAuec: 1169900, pledgeUsd: 90, defaultInsurance: '6 Monate' },
            { name: 'Aegis Reclaimer', manufacturer: 'Aegis Dynamics', role: 'Schweres Bergungsschiff', valueAuec: 15120000, pledgeUsd: 400, defaultInsurance: 'LTI (Lifetime)' },
            { name: 'Aegis Sabre', manufacturer: 'Aegis Dynamics', role: 'Tarnkappenjäger', valueAuec: 2194000, pledgeUsd: 170, defaultInsurance: '6 Monate' },
            { name: 'Aegis Vanguard Warden', manufacturer: 'Aegis Dynamics', role: 'Schwerer Jäger', valueAuec: 3800000, pledgeUsd: 260, defaultInsurance: 'LTI (Lifetime)' },
            { name: 'Anvil Arrow', manufacturer: 'Anvil Aerospace', role: 'Leichter Jäger', valueAuec: 975000, pledgeUsd: 75, defaultInsurance: '6 Monate' },
            { name: 'Anvil Carrack', manufacturer: 'Anvil Aerospace', role: 'Schwere Erkundung', valueAuec: 26700000, pledgeUsd: 600, defaultInsurance: 'LTI (Lifetime)' },
            { name: 'Anvil F7C Hornet Mk II', manufacturer: 'Anvil Aerospace', role: 'Mittlerer Raumüberlegenheitsjäger', valueAuec: 2450000, pledgeUsd: 175, defaultInsurance: '120 Monate (IAE)' },
            { name: 'Crusader C1 Spirit', manufacturer: 'Crusader Industries', role: 'Mittlerer Frachter', valueAuec: 3100000, pledgeUsd: 125, defaultInsurance: '120 Monate (IAE)' },
            { name: 'Crusader Mercury Star Runner', manufacturer: 'Crusader Industries', role: 'Daten- & Frachttransport', valueAuec: 5600000, pledgeUsd: 260, defaultInsurance: 'LTI (Lifetime)' },
            { name: 'Drake Corsair', manufacturer: 'Drake Interplanetary', role: 'Schwere Erkundung & Gunship', valueAuec: 6500000, pledgeUsd: 250, defaultInsurance: '120 Monate (IAE)' },
            { name: 'Drake Cutlass Black', manufacturer: 'Drake Interplanetary', role: 'Mittlerer Multirole-Frachter', valueAuec: 2100000, pledgeUsd: 110, defaultInsurance: '6 Monate' },
            { name: 'Drake Vulture', manufacturer: 'Drake Interplanetary', role: 'Bergung & Salvage', valueAuec: 2450000, pledgeUsd: 175, defaultInsurance: 'LTI (Lifetime)' },
            { name: 'MISC Prospector', manufacturer: 'Musashi Industrial & Starflight Concern', role: 'Solo-Erzabbau (Mining)', valueAuec: 2850000, pledgeUsd: 155, defaultInsurance: '6 Monate' },
            { name: 'Origin 400i', manufacturer: 'Origin Jumpworks', role: 'Luxus-Erkundung', valueAuec: 6800000, pledgeUsd: 250, defaultInsurance: '120 Monate (IAE)' },
            { name: 'RSI Constellation Andromeda', manufacturer: 'Roberts Space Industries', role: 'Kanonenboot / Frachter', valueAuec: 7200000, pledgeUsd: 240, defaultInsurance: '6 Monate' },
            { name: 'RSI Zeus Mk II CL', manufacturer: 'Roberts Space Industries', role: 'Mittlerer Frachter', valueAuec: 3600000, pledgeUsd: 150, defaultInsurance: '120 Monate (IAE)' },
          ],
          totalFleetValueAuec: 9350000,
          totalFleetPledgeUsd: 435,
          totalFlights: 65,
          totalQuantumJumps: 150,
          hangarCount: 3,
          flownCount: 4,
        } as FleetResponseDto;

      case 'clear_contracts':
        return { success: true };

      case 'get_missions':
        return {
          active: [],
          history: [
            {
              id: 'm1',
              title: 'Covalex Delivery - Stanton Route',
              contractor: 'Covalex Shipping',
              faction: 'Covalex',
              missionType: 'Delivery',
              baseReward: 25000,
              reputationGain: 150,
              isIllegal: false,
              starSystems: 'Stanton',
              blueprints: [],
              description: 'Lieferung von 3 Frachtkisten nach MicroTech',
              isCompleted: true,
              time: '10.09. 16:30',
            },
          ],
          catalog: [],
        } as MissionsResponseDto;

      case 'get_reputation':
      case 'set_reputation':
      case 'adjust_reputation_xp':
      case 'reset_reputation':
        return [
          {
            id: 'rep_hurston',
            name: 'Hurston Dynamics Security',
            shortName: 'Hurston Sec',
            category: 'Sicherheit',
            icon: '🛡️',
            system: 'Stanton (Hurston)',
            description: 'Sicherheits- und Kopfgeldverträge rund um Hurston.',
            currentXp: 4800,
            completedMissions: 18,
            currentLevel: 3,
            levelTitle: 'Senior Deputy',
            progressPercent: 65,
            progressText: '4.800 / 7.000 XP',
          },
          {
            id: 'rep_covalex',
            name: 'Covalex Shipping',
            shortName: 'Covalex',
            category: 'Fracht',
            icon: '📦',
            system: 'Stanton',
            description: 'Offizielle Transport- und Kurieraufträge.',
            currentXp: 8200,
            completedMissions: 32,
            currentLevel: 4,
            levelTitle: 'Fleet Courier',
            progressPercent: 82,
            progressText: '8.200 / 10.000 XP',
          },
        ] as FactionReputationDto[];

      case 'get_blueprints':
        return [
          {
            id: 'bp_1',
            name: 'P8-SC SMG Silencer',
            category: 'Waffen',
            subCategory: 'Aufsatz',
            rarity: 'Rare',
            requiredMaterials: '2x RMC, 1x Titanium',
            unlockInfo: 'Pyro Cargo Wreck Salvage',
            isLearned: true,
            learnedDate: '10.09.2026',
          },
          {
            id: 'bp_2',
            name: 'Defiance Core Heavy Armor',
            category: 'Rüstung',
            subCategory: 'Torso',
            rarity: 'Epic',
            requiredMaterials: '6x RMC, 4x Tungsten',
            unlockInfo: 'Bunker Security Mission T4',
            isLearned: false,
          },
        ] as BlueprintDto[];

      case 'get_loadout':
        return [
          {
            slotKey: 'Helmet',
            slotName: 'Helm',
            icon: '🪖',
            itemName: 'Defiance Helmet Firestarter',
            armorClass: 'Heavy',
            damageReduction: 40,
            tempRange: '-100°C bis +140°C',
            badgeColor: '#F59E0B',
            isEquipped: true,
            lastEquipped: '10.09. 18:30',
          },
          {
            slotKey: 'Torso',
            slotName: 'Torso / Core',
            icon: '🥋',
            itemName: 'Defiance Core Firestarter',
            armorClass: 'Heavy',
            damageReduction: 40,
            tempRange: '-100°C bis +140°C',
            badgeColor: '#F59E0B',
            isEquipped: true,
            lastEquipped: '10.09. 18:30',
          },
          {
            slotKey: 'Primary1',
            slotName: 'Primärwaffe 1',
            icon: '🎯',
            itemName: 'FS-9 LMG',
            armorClass: '',
            damageReduction: 0,
            tempRange: '',
            badgeColor: '#38BDF8',
            isEquipped: true,
            lastEquipped: '10.09. 18:25',
          },
        ] as LoadoutSlotDto[];

      case 'get_starmap':
        return {
          currentSystem: payload?.system || 'Stanton',
          objects: [
            {
              id: 'stanton_star',
              name: 'Stanton (Stern)',
              system: 'Stanton',
              type: 'Star',
              orbitRadius: 0,
              orbitAngleDeg: 0,
              colorHex: '#FBBF24',
              size: 28,
              hasArmistice: true,
              jurisdiction: 'UEE',
              securityLevel: 'High',
              specialization: 'Sonnensystem-Zentrum',
              resources: 'Solarenergie',
              description: 'G-Typ Hauptreihenstern mit 4 bewohnten Planeten im UEE-Besitz.',
              relX: 0,
              relY: 0,
            },
            {
              id: 'hurston',
              name: 'Hurston',
              system: 'Stanton',
              parentId: 'stanton_star',
              type: 'Planet',
              orbitRadius: 90,
              orbitAngleDeg: 45,
              colorHex: '#D97706',
              size: 17,
              hasArmistice: true,
              jurisdiction: 'Hurston Dynamics',
              securityLevel: 'High',
              specialization: 'Industrie & Waffenbau',
              resources: 'Beryll, Titan, Wolfram',
              description: 'Industrieplanet im Besitz von Hurston Dynamics. Hauptstadt: Lorville.',
              relX: 63.6,
              relY: 63.6,
            },
            {
              id: 'crusader',
              name: 'Crusader',
              system: 'Stanton',
              parentId: 'stanton_star',
              type: 'Planet',
              orbitRadius: 170,
              orbitAngleDeg: 140,
              colorHex: '#EC4899',
              size: 21,
              hasArmistice: true,
              jurisdiction: 'Crusader Industries',
              securityLevel: 'High',
              specialization: 'Schiffbau & Luxus',
              resources: 'Gase, Wasserstoff',
              description: 'Gasriese mit atembarer oberer Atmosphäre. Wolkenstadt Orison.',
              relX: -130.2,
              relY: 109.3,
            },
            {
              id: 'arccorp',
              name: 'ArcCorp',
              system: 'Stanton',
              parentId: 'stanton_star',
              type: 'Planet',
              orbitRadius: 250,
              orbitAngleDeg: 235,
              colorHex: '#F97316',
              size: 17,
              hasArmistice: true,
              jurisdiction: 'ArcCorp',
              securityLevel: 'High',
              specialization: 'Megacity & Fusion Engines',
              resources: 'Komponenten, Technologie',
              description: 'Vollständig urbanisierter Stadtplanet. Heimat von Area 18.',
              relX: -143.4,
              relY: -204.8,
            },
            {
              id: 'microtech',
              name: 'microTech',
              system: 'Stanton',
              parentId: 'stanton_star',
              type: 'Planet',
              orbitRadius: 330,
              orbitAngleDeg: 310,
              colorHex: '#38BDF8',
              size: 18,
              hasArmistice: true,
              jurisdiction: 'microTech',
              securityLevel: 'High',
              specialization: 'High-Tech, mobiGlas & Software',
              resources: 'Cryo-Mineralien, Titan',
              description: 'Eisiger Planet mit hochentwickelten Forschungs- & Tech-Kuppeln. Hauptstadt: New Babbage.',
              relX: 212.1,
              relY: -252.8,
            },
            {
              id: 'everus',
              name: 'Everus Harbor',
              system: 'Stanton',
              parentId: 'hurston',
              type: 'SpaceStation',
              orbitRadius: 90,
              orbitAngleDeg: 41,
              colorHex: '#38BDF8',
              size: 8,
              hasArmistice: true,
              jurisdiction: 'Hurston Dynamics',
              securityLevel: 'High',
              specialization: 'Raffinerie & Hangars',
              resources: '',
              description: 'Orbitale Hauptstation über Hurston mit Raffinerie & Frachtdecks.',
              relX: 67.9,
              relY: 59.0,
            },
            {
              id: 'seraphim',
              name: 'Seraphim Station',
              system: 'Stanton',
              parentId: 'crusader',
              type: 'SpaceStation',
              orbitRadius: 170,
              orbitAngleDeg: 136,
              colorHex: '#38BDF8',
              size: 8,
              hasArmistice: true,
              jurisdiction: 'Crusader Industries',
              securityLevel: 'High',
              specialization: 'Frachtdecks & Hangars',
              resources: '',
              description: 'Orbitale Raumstation über Crusader mit Fracht- und Hangardecks.',
              relX: -122.3,
              relY: 118.1,
            },
          ],
          drives: [
            { name: 'VK-00', sizeClass: 'S1', topSpeedKmS: 283000, displayText: 'S1 VK-00 (283.000 km/s)' },
            { name: 'Atlas', sizeClass: 'S1', topSpeedKmS: 152000, displayText: 'S1 Atlas (152.000 km/s)' },
            { name: 'Crossfield', sizeClass: 'S2', topSpeedKmS: 261000, displayText: 'S2 Crossfield (261.000 km/s)' },
            { name: 'TS-2', sizeClass: 'S3', topSpeedKmS: 260000, displayText: 'S3 TS-2 (260.000 km/s)' },
          ],
        } as StarmapResponseDto;

      case 'calculate_route':
        return {
          fromId: payload?.fromId || 'hurston',
          fromName: 'Hurston',
          toId: payload?.toId || 'crusader',
          toName: 'Crusader',
          driveName: payload?.driveName || 'Atlas',
          distKm: 29850000,
          distGm: 29.85,
          flightTimeSeconds: 208.3,
          flightTimeFormatted: '3m 28s',
        } as QuantumRouteResultDto;

      case 'get_places':
        return [
          {
            id: 'hurston',
            name: 'Hurston',
            system: 'Stanton',
            parentBody: 'Stanton (Stern)',
            type: 'Planet',
            icon: '🪐',
            securityLevel: 'High',
            hasArmistice: true,
            specialization: 'Industrie & Waffen',
            description: 'Industrieplanet von Hurston Dynamics mit Metropole Lorville.',
          },
          {
            id: 'lorville',
            name: 'Lorville',
            system: 'Stanton',
            parentBody: 'Hurston',
            type: 'LandingZone',
            icon: '🏙️',
            securityLevel: 'High',
            hasArmistice: true,
            specialization: 'Großhandelszentrum & CBD',
            description: 'Hauptstadt von Hurston mit Teasa Spaceport und New Deal Shipyard.',
          },
          {
            id: 'everus',
            name: 'Everus Harbor',
            system: 'Stanton',
            parentBody: 'Hurston',
            type: 'SpaceStation',
            icon: '🛰️',
            securityLevel: 'High',
            hasArmistice: true,
            specialization: 'Raffinerie & Frachtdecks',
            description: 'Orbitale Raumstation über Hurston mit Hangars und Raffineriedeck.',
          },
          {
            id: 'area18',
            name: 'Area 18',
            system: 'Stanton',
            parentBody: 'ArcCorp',
            type: 'LandingZone',
            icon: '🏙️',
            securityLevel: 'High',
            hasArmistice: true,
            specialization: 'Astro Armada & TDD',
            description: 'Zentrale Landezone auf ArcCorp mit Riker Spaceport.',
          },
          {
            id: 'grimhex',
            name: 'Grim HEX',
            system: 'Stanton',
            parentBody: 'Yela (Crusader)',
            type: 'SpaceStation',
            icon: '☠️',
            securityLevel: 'Lawless',
            hasArmistice: false,
            specialization: 'Schwarzmarkt & Schmuggel',
            description: 'Ehemalige Green-HEX-Bergbaubasis im Asteroidenring von Yela.',
          },
        ] as PlaceItemDto[];

      case 'get_blackbox':
        return {
          totalDistanceGm: 129.5,
          totalDistanceKm: 129500000,
          totalDistanceText: '129.5 GM (129.500.000 km)',
          flightDurationText: '4h 18m',
          quantumJumps: 14,
          sortieCount: 3,
          shipLosses: 0,
          visitedBodies: ['Hurston', 'Crusader', 'Daymar', 'Arial'],
          usedShips: ['Drake Vulture', 'Aegis Vanguard Warden'],
          timeline: [
            {
              id: 'bb1',
              time: '10.09. 18:45:10',
              relativeTime: '+03:45:10',
              kind: 'quantum',
              title: 'Quantum-Sprung',
              subtitle: 'Sprungziel: Everus Harbor (Hurston)',
              ship: 'Drake Vulture',
              location: 'Everus Harbor',
              isMajor: true,
            },
            {
              id: 'bb2',
              time: '10.09. 18:15:30',
              relativeTime: '+03:15:30',
              kind: 'vehicle',
              title: 'Schiff ausgelagert',
              subtitle: 'Drake Vulture auf Hangar 04 bereitgestellt',
              ship: 'Drake Vulture',
              location: 'Lorville',
              isMajor: true,
            },
          ],
        } as FlightRecorderDto;

      case 'get_rs_signatures':
        return [
          {
            name: 'Salvage (Panels)',
            baseRs: 2000,
            tier: 'A',
            rarity: 'uncommon',
            method: 'salvage',
            estimatedPricePerScu: 14500,
            locations: ['Yela Ring', 'Hurston L1 Asteroiden'],
          },
          {
            name: 'Lindinium',
            baseRs: 3400,
            tier: 'B',
            rarity: 'common',
            method: 'ship',
            estimatedPricePerScu: 22000,
            locations: ['Pyro Asteroiden', 'Stanton'],
          },
          {
            name: 'Quantanium',
            baseRs: 6000,
            tier: 'S',
            rarity: 'rare',
            method: 'ship',
            estimatedPricePerScu: 88000,
            locations: ['Lyria', 'Yela Asteroid Ring'],
          },
          {
            name: 'Gold',
            baseRs: 7200,
            tier: 'S',
            rarity: 'uncommon',
            method: 'ship',
            estimatedPricePerScu: 44000,
            locations: ['Daymar', 'Cellin', 'Magda'],
          },
        ] as RsResourceDto[];

      case 'decode_rs':
        const rs = payload?.rs || 2000;
        return [
          {
            resourceName: rs === 2000 ? 'Salvage (Panels)' : 'Erzknoten / Erzcluster',
            baseRs: rs === 2000 ? 2000 : rs,
            tier: 'A',
            rarity: 'uncommon',
            method: rs === 2000 ? 'salvage' : 'ship',
            estimatedPricePerScu: 14500,
            nodes: 1,
            isExact: true,
            errorPct: 0,
            scannedRs: rs,
            estimatedClusterValue: 174000,
          },
        ] as RsMatchDto[];

      case 'get_market':
        return [
          {
            name: 'Laranite',
            category: 'Minerals',
            tier: 'S',
            avgBuyPrice: 28.5,
            avgSellPrice: 33.2,
            margin: 4.7,
            bestBuyLocation: 'Mining Area 045 (Wala)',
            bestSellLocation: 'Lorville CBD (Hurston)',
          },
          {
            name: 'Recycled Material Composite (RMC)',
            category: 'Salvage',
            tier: 'S',
            avgBuyPrice: 11.8,
            avgSellPrice: 14.5,
            margin: 2.7,
            bestBuyLocation: 'Pickers Field (Hurston)',
            bestSellLocation: 'Area 18 TDD (ArcCorp)',
          },
          {
            name: 'Beryl',
            category: 'Minerals',
            tier: 'A',
            avgBuyPrice: 3.9,
            avgSellPrice: 4.85,
            margin: 0.95,
            bestBuyLocation: 'HDMS-Ryder (Ita)',
            bestSellLocation: 'Orison Cloudview (Crusader)',
          },
          {
            name: 'Titanium',
            category: 'Metals',
            tier: 'A',
            avgBuyPrice: 7.8,
            avgSellPrice: 9.2,
            margin: 1.4,
            bestBuyLocation: 'HDMS-Bezdek (Arial)',
            bestSellLocation: 'New Babbage (microTech)',
          },
        ] as MarketCommodityDto[];

      case 'get_tools_status':
      case 'clear_shader_cache':
      case 'clear_crash_dumps':
      case 'save_user_cfg':
      case 'backup_keybinds':
      case 'restore_keybinds':
      case 'backup_user_cfg':
      case 'restore_user_cfg':
      case 'save_cloud_storage_path':
      case 'export_logs_zip':
      case 'sync_logs_cloud':
      case 'open_folder':
        return {
          shaderCacheMb: type === 'clear_shader_cache' ? 0 : 342.5,
          crashDumpsMb: type === 'clear_crash_dumps' ? 0 : 85.2,
          userCfgPath: 'J:\\StarCitizen\\LIVE\\user.cfg',
          userCfgExists: true,
          userCfgContent: payload?.cfgContent || 'r_VSync = 0\nr_MotionBlur = 0\nsys_maxfps = 120\nr_TexturesStreamPoolSize = 6144\nr_DisplayInfo = 1\ng_language = english',
          totalRamGb: 64,
          ramStatus: '64 GB (Optimal)',
          driveName: 'J:',
          freeDiskGb: 485.6,
          pagefileStatus: 'Aktiv (NVMe SSD)',
          cloudStoragePath: 'C:\\Users\\Pilot\\OneDrive\\StarCitizen',
          keybindBackups: [
            'backup_2026-03-01_dualstick (5 Dateien, 1.2 MB)',
            'backup_2026-02-15_flight (4 Dateien, 980 KB)',
          ],
          keybindItems: [
            {
              name: 'backup_2026-03-01_dualstick',
              folderPath: 'C:\\Users\\Pilot\\AppData\\Roaming\\SCLogMate\\keybind_backups\\backup_2026-03-01_dualstick',
              createdAt: '01.03.2026 18:30',
              fileCount: 5,
              locationType: 'Lokal + Cloud',
              sizeFormatted: '1.2 MB',
            },
            {
              name: 'backup_2026-02-15_flight',
              folderPath: 'C:\\Users\\Pilot\\AppData\\Roaming\\SCLogMate\\keybind_backups\\backup_2026-02-15_flight',
              createdAt: '15.02.2026 14:15',
              fileCount: 4,
              locationType: 'Lokal',
              sizeFormatted: '980 KB',
            },
          ],
          configBackups: [
            {
              name: 'user_2026-03-01_18-30-00.cfg',
              filePath: 'C:\\Users\\Pilot\\AppData\\Roaming\\SCLogMate\\config_backups\\user_2026-03-01_18-30-00.cfg',
              createdAt: '01.03.2026 18:30',
              locationType: 'Lokal + Cloud',
              sizeFormatted: '1.4 KB',
            },
            {
              name: 'user_2026-02-15_14-15-00.cfg',
              filePath: 'C:\\Users\\Pilot\\AppData\\Roaming\\SCLogMate\\config_backups\\user_2026-02-15_14-15-00.cfg',
              createdAt: '15.02.2026 14:15',
              locationType: 'Lokal',
              sizeFormatted: '1.2 KB',
            },
          ],
          keybindsDir: 'C:\\Users\\Pilot\\AppData\\Roaming\\SCLogMate\\keybind_backups',
          configDir: 'C:\\Users\\Pilot\\AppData\\Roaming\\SCLogMate\\config_backups',
        } as ToolsStatusDto;

      case 'get_settings':
      case 'save_settings':
        return {
          logPath: payload?.settings?.logPath || 'J:\\StarCitizen\\LIVE\\logbackups\\game.log',
          balance: payload?.settings?.balance ?? 15420800,
          autoOcrEnabled: payload?.settings?.autoOcrEnabled ?? true,
          uexApiKey: payload?.settings?.uexApiKey || '',
          overlayEnabled: payload?.settings?.overlayEnabled ?? true,
          overlayOpacity: payload?.settings?.overlayOpacity ?? 0.92,
          toastEnabled: payload?.settings?.toastEnabled ?? true,
          toastBlueprintEnabled: payload?.settings?.toastBlueprintEnabled ?? true,
          toastMissionEnabled: payload?.settings?.toastMissionEnabled ?? true,
          toastReputationEnabled: payload?.settings?.toastReputationEnabled ?? true,
          toastRefineryEnabled: payload?.settings?.toastRefineryEnabled ?? true,
          toastElevatorEnabled: payload?.settings?.toastElevatorEnabled ?? true,
          toastShipDestructionEnabled: payload?.settings?.toastShipDestructionEnabled ?? true,
          auroraIntegrationEnabled: payload?.settings?.auroraIntegrationEnabled ?? true,
          auroraVolume: payload?.settings?.auroraVolume ?? 40,
          rsTargetAlertEnabled: payload?.settings?.rsTargetAlertEnabled ?? true,
          rsTargetSoundEnabled: payload?.settings?.rsTargetSoundEnabled ?? true,
        } as SettingsDto;

      case 'lookup_wiki':
      case 'get_wiki_specs':
        return {
          name: payload?.query || payload?.name || 'Cutlass Black',
          category: 'Schiff & Fahrzeug',
          manufacturer: 'Drake Interplanetary',
          role: 'Mittlerer Frachter / Gunship',
          type: 'Medium Freight / Combat',
          focus: 'Allrounder, Fracht & Kampf',
          size: '3',
          crewMin: 1,
          crewMax: 2,
          cargoScu: 46,
          quantumFuel: 2500,
          length: 29.0,
          beam: 26.5,
          height: 10.0,
          mass: 226000,
          descriptionDe: 'Die Drake Cutlass Black ist das bekannteste und vielseitigste Schiff im Verse. Mit großem Frachtraum, Side-Doors, Traktorstrahl-Aufhängung und starker Bewaffnung ist sie die erste Wahl für Händler, Söldner und Entdecker.',
          descriptionEn: 'The Drake Cutlass Black is a low-cost, easy-to-maintain medium fighter and freighter. Boasting a larger-than-average cargo hold and tractor beam mount.',
          bestDescription: 'Die Drake Cutlass Black ist das bekannteste und vielseitigste Schiff im Verse. Mit großem Frachtraum, Side-Doors, Traktorstrahl-Aufhängung und starker Bewaffnung ist sie die erste Wahl für Händler, Söldner und Entdecker.',
          descriptionHeader: '📖  BESCHREIBUNG (DEUTSCH)',
          imageUrl: 'https://media.starcitizen.tools/images/thumb/7/7b/Cutlass_Black_in_flight.png/1200px-Cutlass_Black_in_flight.png',
          thumbnailUrl: 'https://media.starcitizen.tools/images/thumb/7/7b/Cutlass_Black_in_flight.png/320px-Cutlass_Black_in_flight.png',
          webUrl: 'https://star-citizen.wiki/Cutlass_Black',
          pledgeUrl: 'https://robertsspaceindustries.com/pledge/ships/drake-cutlass/Cutlass-Black',
          msrp: 110,
          productionStatus: 'Flight-Ready',
          specs: {
            'Frachtkapazität': '46 SCU',
            'Besatzung': '1 - 2 Personen',
            'Quantum Treibstoff': '2.500 l',
            'Abmessungen (L×B×H)': '29.0 m × 26.5 m × 10.0 m',
            'Masse': '226.000 kg',
            'Fahrzeuggröße': 'Größe 3 (Medium)',
            'Waffen': '4× Size 3 Hardpoints (Gimbal S2/Fixed S3)',
            'Türme': '1× Bemanntes Dach-Geschütz (2× S3)',
            'Schilde': '1× S2 Schildgenerator',
          },
          storeLocations: [
            {
              storeName: 'New Deal',
              location: 'Teasa Spaceport, Lorville (Hurston)',
              priceAuec: 2100000,
              rentPrice1dAuec: 42000,
            }
          ]
        } as WikiInfo;

      case 'search_wiki':
        return [
          {
            name: 'Cutlass Black',
            category: 'Schiff & Fahrzeug',
            manufacturer: 'Drake Interplanetary',
            role: 'Mittlerer Frachter',
            type: 'Medium Freight',
            cargoScu: 46,
            crewMin: 1,
            crewMax: 2,
            productionStatus: 'Flight-Ready',
            thumbnailUrl: 'https://media.starcitizen.tools/images/thumb/7/7b/Cutlass_Black_in_flight.png/320px-Cutlass_Black_in_flight.png',
            webUrl: 'https://star-citizen.wiki/Cutlass_Black',
            msrp: 110,
          },
          {
            name: 'Gladius',
            category: 'Schiff & Fahrzeug',
            manufacturer: 'Aegis Dynamics',
            role: 'Leichter Jäger',
            type: 'Light Fighter',
            cargoScu: 0,
            crewMin: 1,
            crewMax: 1,
            productionStatus: 'Flight-Ready',
            thumbnailUrl: 'https://media.starcitizen.tools/images/thumb/8/87/Gladius_flying_in_space.jpg/320px-Gladius_flying_in_space.jpg',
            webUrl: 'https://star-citizen.wiki/Gladius',
            msrp: 90,
          },
          {
            name: 'Carrack',
            category: 'Schiff & Fahrzeug',
            manufacturer: 'Anvil Aerospace',
            role: 'Expedition / Deep Space',
            type: 'Large Explorer',
            cargoScu: 456,
            crewMin: 4,
            crewMax: 6,
            productionStatus: 'Flight-Ready',
            thumbnailUrl: 'https://media.starcitizen.tools/images/thumb/e/e6/Carrack_in_space.jpg/320px-Carrack_in_space.jpg',
            webUrl: 'https://star-citizen.wiki/Carrack',
            msrp: 600,
          }
        ] as WikiInfo[];
    }
  }
}

export const bridge = new PhotinoBridge();
