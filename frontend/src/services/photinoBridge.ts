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
  category: 'wallet' | 'combat' | 'mission' | 'ship' | 'location' | 'system';
  title: string;
  description: string;
  amount?: number;
  ship?: string;
  rawText?: string;
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
  totalIncome: number;
  totalSpend: number;
  totalNet: number;
  sales: number;
  trade: number;
  missionsReward: number;
  purchases: number;
  transferIn: number;
  transferOut: number;
  ledger: LogEventItem[];
  cargo: LogEventItem[];
  topExpenses: LogEventItem[];
}

export interface FleetStatDto {
  shipName: string;
  flights: number;
  quantumJumps: number;
  losses: number;
  lastUsed: string;
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

  // Mock implementation for browser-only development
  private async handleMockRequest(type: string, payload?: any): Promise<any> {
    switch (type) {
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
        return [
          {
            shipName: 'Drake Vulture',
            flights: 14,
            quantumJumps: 38,
            losses: 0,
            lastUsed: '10.09.2026 18:45',
          },
          {
            shipName: 'Aegis Vanguard Warden',
            flights: 22,
            quantumJumps: 64,
            losses: 2,
            lastUsed: '08.09.2026 23:10',
          },
        ] as FleetStatDto[];

      case 'toggle_watcher':
        return { isLiveWatching: payload?.enable ?? true };

      case 'scan_logs':
        return { scannedCount: 24 };

      default:
        return { success: true };
    }
  }
}

export const bridge = new PhotinoBridge();
