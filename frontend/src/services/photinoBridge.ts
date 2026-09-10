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
  rawText?: string;
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
  public sendRequest<TResponse = any, TPayload = any>(type: string, payload?: TPayload, timeoutMs = 8000): Promise<TResponse> {
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

  // Mock implementation for browser-only development (npm run dev in Chrome/Edge)
  private async handleMockRequest(type: string, payload?: any): Promise<any> {
    console.log(`[PhotinoBridge Mock] Handled request: ${type}`, payload);
    switch (type) {
      case 'get_status':
        return {
          version: '1.0.0-photino-preview',
          isLiveWatching: true,
          logPath: 'C:\\Program Files\\Roberts Space Industries\\StarCitizen\\LIVE\\logbackups',
          activeSessionName: 'Session 2026-09-10 18:30',
          dbSessionCount: 14,
          totalIncome: 1450000,
          totalSpend: 320000,
          totalNet: 1130000,
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
            name: 'Stanton Bounty Hunting',
            startTime: '09.09. 20:00',
            endTime: '09.09. 22:15',
            duration: '2h 15m',
            income: 600000,
            spend: 200000,
            net: 400000,
            sales: 0,
            trade: 0,
            deaths: 1,
            missions: 8,
            ships: ['Aegis Vanguard Warden'],
            lastLocation: 'Port Tressler',
          },
        ] as SessionSummary[];

      case 'toggle_watcher':
        return { isLiveWatching: payload?.enable ?? true };

      case 'scan_logs':
        return { scannedCount: 12, newEvents: 48 };

      default:
        return { success: true };
    }
  }
}

export const bridge = new PhotinoBridge();
