import React, { useEffect, useState, useMemo, useRef } from 'react';
import {
  MessageSquare,
  ShieldAlert,
  Search,
  RefreshCw,
  Trash2,
  Bookmark,
  BookmarkCheck,
  Radio,
  User,
  ChevronDown,
  Crop,
  Zap,
} from 'lucide-react';
import { ChatMessageDto, PilotProfile, bridge } from '../services/photinoBridge';
import { PlayerReportModal } from '../components/PlayerReportModal';
import { PilotDossierModal } from '../components/PilotDossierModal';

interface ChatLogViewProps {
  initialSession?: string;
}

export const ChatLogView: React.FC<ChatLogViewProps> = ({ initialSession }) => {
  const [messages, setMessages] = useState<ChatMessageDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [scanningNow, setScanningNow] = useState(false);
  const [ocrEnabled, setOcrEnabled] = useState(false);
  const [selectingRegion, setSelectingRegion] = useState(false);
  const [testingScan, setTestingScan] = useState(false);
  const [toastMsg, setToastMsg] = useState<string | null>(null);

  const showToast = (msg: string) => {
    setToastMsg(msg);
    setTimeout(() => setToastMsg(null), 4000);
  };

  // Filters
  const [selectedChannel, setSelectedChannel] = useState<string>('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [flaggedOnly, setFlaggedOnly] = useState(false);

  // Modals state
  const [reportModalOpen, setReportModalOpen] = useState(false);
  const [reportSuspect, setReportSuspect] = useState('');
  const [reportSelectedMsgs, setReportSelectedMsgs] = useState<ChatMessageDto[]>([]);

  const [dossierModalOpen, setDossierModalOpen] = useState(false);
  const [selectedPilot, setSelectedPilot] = useState<PilotProfile | null>(null);

  const tableContainerRef = useRef<HTMLDivElement>(null);
  const [autoScroll, setAutoScroll] = useState(true);

  // Fetch messages from backend
  const fetchMessages = async () => {
    try {
      setLoading(true);
      const res = await bridge.getChatMessages({
        session: initialSession || '__live__',
        limit: 500,
      });
      if (res && Array.isArray(res)) {
        setMessages(res);
      }
    } catch (err) {
      console.error('Failed to load chat messages:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchMessages();

    // Fetch initial OCR active state
    bridge.sendRequest<any>('get_settings').then((st: any) => {
      if (st && typeof st.chatOcrEnabled === 'boolean') {
        setOcrEnabled(st.chatOcrEnabled);
      }
    }).catch(() => {});

    // Listen for backend status updates
    const unbindStatus = bridge.on<any>('STATUS_UPDATE', (st) => {
      if (st && typeof st.chatOcrEnabled === 'boolean') {
        setOcrEnabled(st.chatOcrEnabled);
      }
    });

    // Subscribe to live scanned messages from backend
    const unbindMsgs = bridge.on<ChatMessageDto[]>('CHAT_MESSAGES_RECEIVED', (newMsgs) => {
      if (newMsgs && newMsgs.length > 0) {
        setMessages((prev) => {
          const existingIds = new Set(prev.map((m) => m.id));
          const toAdd = newMsgs.filter((m) => !existingIds.has(m.id));
          if (toAdd.length === 0) return prev;
          return [...prev, ...toAdd];
        });
      }
    });

    return () => {
      unbindStatus();
      unbindMsgs();
    };
  }, [initialSession]);

  useEffect(() => {
    if (autoScroll && tableContainerRef.current) {
      tableContainerRef.current.scrollTo({
        top: tableContainerRef.current.scrollHeight,
        behavior: 'smooth',
      });
    }
  }, [messages, autoScroll]);

  const handleTriggerScan = async () => {
    if (scanningNow) return;
    try {
      setScanningNow(true);
      const res = await bridge.scanChatNow();
      if (res && res.messages && res.messages.length > 0) {
        setMessages((prev) => {
          const existingIds = new Set(prev.map((m) => m.id));
          const toAdd = res.messages.filter((m) => !existingIds.has(m.id));
          return [...prev, ...toAdd];
        });
        showToast(`✓ ${res.messages.length} Chat-Nachricht(en) erfasst`);
      } else {
        showToast('Keine neuen Chat-Nachrichten im Scan-Bereich gefunden.');
      }
    } catch (err) {
      console.error('Scan now failed:', err);
      showToast('Fehler beim Chat-Scan');
    } finally {
      setTimeout(() => setScanningNow(false), 500);
    }
  };

  const handleToggleOcr = async () => {
    try {
      const res = await bridge.toggleChatOcr(!ocrEnabled);
      setOcrEnabled(res.enabled);
    } catch (err) {
      console.error('Failed to toggle chat OCR:', err);
    }
  };

  const handleSelectRegion = async () => {
    try {
      setSelectingRegion(true);
      showToast('Bildschirm-Auswahl: Ziehe mit der Maus ein Rechteck über dein Chatfenster...');
      const res = await bridge.sendRequest<any>('select_ocr_region', { target: 'chat' });
      if (res?.success && res.region) {
        showToast(`✓ Chat-Bereich gespeichert: ${res.region.width}×${res.region.height} @ (${res.region.x}, ${res.region.y})`);
      } else if (res?.cancelled) {
        showToast('Auswahl abgebrochen');
      }
    } catch (err) {
      console.error('Failed to select chat region:', err);
      showToast('Fehler bei der Bildschirmauswahl');
    } finally {
      setSelectingRegion(false);
    }
  };

  const handleTestScan = async () => {
    try {
      setTestingScan(true);
      const res = await bridge.sendRequest<any>('test_ocr_scan', { target: 'chat' });
      if (res?.success) {
        showToast(`✓ Text erkannt: ${res.recognizedText?.slice(0, 35) || 'leer'} (${res.durationMs}ms)`);
        await fetchMessages();
      } else {
        showToast('⚠️ Kein Chat-Text erkannt. Prüfe den Scan-Bereich.');
      }
    } catch (err) {
      console.error('Test scan failed:', err);
      showToast('Fehler beim OCR Test-Scan');
    } finally {
      setTestingScan(false);
    }
  };

  const handleToggleFlag = async (msg: ChatMessageDto, e: React.MouseEvent) => {
    e.stopPropagation();
    const newFlagState = !msg.isFlagged;
    try {
      await bridge.flagChatMessage(msg.id, newFlagState);
      setMessages((prev) =>
        prev.map((m) => (m.id === msg.id ? { ...m, isFlagged: newFlagState } : m))
      );
    } catch (err) {
      console.error('Failed to flag message:', err);
    }
  };

  const handleClearChat = async () => {
    if (!window.confirm('Möchtest du das gesamte Chat-Protokoll dieser Sitzung wirklich leeren?')) {
      return;
    }
    try {
      await bridge.clearChatMessages(initialSession || '__live__');
      setMessages([]);
    } catch (err) {
      console.error('Failed to clear chat:', err);
    }
  };

  const handleOpenDossier = async (sender: string, e: React.MouseEvent) => {
    e.stopPropagation();
    if (!sender || sender === '—' || sender.toLowerCase() === 'system') return;
    try {
      const dossier = await bridge.getPilotDossier(sender);
      setSelectedPilot(dossier);
      setDossierModalOpen(true);
    } catch (err) {
      console.error('Failed to load pilot dossier:', err);
    }
  };

  const handleOpenReportModal = (specificSuspect?: string, specificMsg?: ChatMessageDto) => {
    if (specificMsg) {
      setReportSuspect(specificSuspect || specificMsg.sender);
      setReportSelectedMsgs([specificMsg]);
    } else {
      const flagged = messages.filter((m) => m.isFlagged);
      setReportSuspect(specificSuspect || (flagged.length > 0 ? flagged[0].sender : ''));
      setReportSelectedMsgs(flagged.length > 0 ? flagged : messages.slice(-5));
    }
    setReportModalOpen(true);
  };

  // Dynamic custom/org channels detected in messages
  const distinctOrgChannels = useMemo(() => {
    const set = new Set<string>();
    for (const m of messages) {
      const lower = m.channel.toLowerCase();
      if (lower !== 'global' && lower !== 'party' && lower !== 'direct' && lower !== 'whisper') {
        set.add(m.channel);
      }
    }
    return Array.from(set);
  }, [messages]);

  // Filter messages
  const filteredMessages = useMemo(() => {
    return messages.filter((m) => {
      // Channel filter
      if (selectedChannel !== 'all') {
        if (selectedChannel === 'Global' && m.channel.toLowerCase() !== 'global') return false;
        if (selectedChannel === 'Party' && !['party', 'gruppe'].includes(m.channel.toLowerCase())) return false;
        if (selectedChannel === 'Direct' && !['direct', 'whisper', 'dm', 'privat'].includes(m.channel.toLowerCase())) return false;
        if (!['Global', 'Party', 'Direct'].includes(selectedChannel)) {
          if (m.channel.toLowerCase() !== selectedChannel.toLowerCase()) return false;
        }
      }

      // Flagged filter
      if (flaggedOnly && !m.isFlagged) return false;

      // Search query
      if (searchQuery.trim()) {
        const q = searchQuery.toLowerCase();
        const matchSender = m.sender.toLowerCase().includes(q);
        const matchMsg = m.message.toLowerCase().includes(q);
        const matchChannel = m.channel.toLowerCase().includes(q);
        if (!matchSender && !matchMsg && !matchChannel) return false;
      }

      return true;
    });
  }, [messages, selectedChannel, flaggedOnly, searchQuery]);

  const flaggedCount = useMemo(() => messages.filter((m) => m.isFlagged).length, [messages]);

  return (
    <div className="flex flex-col min-h-full space-y-4 font-mono">
      {/* KPI & OCR Status Header Bar */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {/* Total Scanned Messages Card */}
        <div className="sc-glass rounded-lg p-4 border border-cyan-500/30 sc-hud-corner relative overflow-hidden">
          <div className="flex justify-between items-start">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Erfasste Nachrichten (OCR)
            </span>
            <MessageSquare className="w-4 h-4 text-cyan-400" />
          </div>
          <div className="mt-2 flex items-baseline space-x-2">
            <span className="text-2xl font-bold font-mono text-cyan-300">
              {messages.length}
            </span>
            <span className="text-xs font-normal text-slate-400">Zeilen</span>
          </div>
        </div>

        {/* Flagged Incidents Card */}
        <div className="sc-glass rounded-lg p-4 border border-rose-500/30 sc-hud-corner relative overflow-hidden">
          <div className="flex justify-between items-start">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Markiert für CIG Support-Report
            </span>
            <ShieldAlert className="w-4 h-4 text-rose-400" />
          </div>
          <div className="mt-2 flex items-baseline justify-between">
            <div className="flex items-baseline space-x-2">
              <span className="text-2xl font-bold font-mono text-rose-300">
                {flaggedCount}
              </span>
              <span className="text-xs font-normal text-slate-400">Vorfälle</span>
            </div>
            {flaggedCount > 0 && (
              <button
                onClick={() => handleOpenReportModal()}
                className="px-2.5 py-1 rounded bg-rose-600 hover:bg-rose-500 text-white text-[11px] font-bold shadow-lg shadow-rose-950/50 transition-colors flex items-center space-x-1"
              >
                <ShieldAlert className="w-3 h-3" />
                <span>Ticket erstellen</span>
              </button>
            )}
          </div>
        </div>

        {/* OCR Scanner Engine Card */}
        <div className="sc-glass rounded-lg p-4 border border-slate-800 sc-hud-corner relative overflow-hidden">
          <div className="flex justify-between items-start">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Live Chat-Scanner (3,5s Takt)
            </span>
            <Radio
              className={`w-4 h-4 ${
                ocrEnabled ? 'text-emerald-400 animate-pulse' : 'text-slate-500'
              }`}
            />
          </div>
          <div className="mt-2 flex items-center justify-between">
            <div className="flex items-center space-x-2">
              <span
                className={`w-2.5 h-2.5 rounded-full ${
                  ocrEnabled ? 'bg-emerald-400 shadow-[0_0_8px_#34d399]' : 'bg-slate-600'
                }`}
              />
              <span className="text-sm font-bold text-slate-200">
                {ocrEnabled ? 'AKTIV' : 'PAUSIERT'}
              </span>
            </div>
            <div className="flex items-center space-x-1.5">
              <button
                onClick={handleSelectRegion}
                disabled={selectingRegion}
                className="px-2 py-1 rounded bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white text-xs border border-slate-700 flex items-center space-x-1 transition-colors disabled:opacity-50"
                title="Chat-Scanbereich interaktiv am Bildschirm markieren"
              >
                <Crop className={`w-3 h-3 text-cyan-400 ${selectingRegion ? 'animate-spin' : ''}`} />
                <span>Bereich</span>
              </button>
              <button
                onClick={handleTestScan}
                disabled={testingScan}
                className="px-2 py-1 rounded bg-slate-800 hover:bg-slate-700 text-amber-300 hover:text-amber-200 text-xs border border-slate-700 flex items-center space-x-1 transition-colors disabled:opacity-50"
                title="OCR-Test auf aktuellem Chat-Bereich ausführen"
              >
                <Zap className={`w-3 h-3 text-amber-400 ${testingScan ? 'animate-spin' : ''}`} />
                <span>Test</span>
              </button>
              <button
                onClick={handleToggleOcr}
                className={`px-2 py-1 rounded text-xs border transition-colors ${
                  ocrEnabled
                    ? 'border-slate-700 bg-slate-800 text-slate-300 hover:bg-slate-700'
                    : 'border-emerald-600/50 bg-emerald-950/40 text-emerald-300 hover:bg-emerald-900/50'
                }`}
              >
                {ocrEnabled ? 'Pause' : 'Start'}
              </button>
              <button
                onClick={handleTriggerScan}
                disabled={scanningNow}
                className="px-2 py-1 rounded bg-cyan-600/80 hover:bg-cyan-500 text-white text-xs border border-cyan-500/50 flex items-center space-x-1 transition-colors disabled:opacity-50"
                title="Manuellen Chat-Scan sofort auslösen"
              >
                <RefreshCw className={`w-3 h-3 ${scanningNow ? 'animate-spin' : ''}`} />
                <span>Scan</span>
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Toast Message Banner */}
      {toastMsg && (
        <div className="p-2.5 rounded-lg bg-cyan-950/80 border border-cyan-500/50 text-cyan-200 text-xs flex items-center justify-between animate-in fade-in">
          <span>{toastMsg}</span>
          <button onClick={() => setToastMsg(null)} className="text-cyan-400 hover:text-white text-xs font-bold px-1.5 cursor-pointer">✕</button>
        </div>
      )}

      {/* Filter and Action Toolbar */}
      <div className="sc-glass rounded-xl p-3 border border-slate-800/80 space-y-2.5 text-xs">
        {/* Tier 1: Channel Pills + Scanner Controls */}
        <div className="flex flex-wrap items-center justify-between gap-2">
          {/* Channel Pills */}
          <div className="flex items-center space-x-1.5 flex-wrap gap-y-1">
            <button
              onClick={() => setSelectedChannel('all')}
              className={`px-3 py-1.5 rounded-lg border font-bold transition-all ${
                selectedChannel === 'all'
                  ? 'bg-cyan-500/20 border-cyan-500/60 text-cyan-300 shadow-[0_0_12px_rgba(6,182,212,0.25)]'
                  : 'bg-slate-900/60 border-slate-800 text-slate-400 hover:text-slate-200'
              }`}
            >
              Alle Kanäle
            </button>
            <button
              onClick={() => setSelectedChannel('Global')}
              className={`px-3 py-1.5 rounded-lg border font-bold transition-all ${
                selectedChannel === 'Global'
                  ? 'bg-cyan-500/20 border-cyan-500/60 text-cyan-300 shadow-[0_0_12px_rgba(6,182,212,0.25)]'
                  : 'bg-slate-900/60 border-slate-800 text-slate-400 hover:text-slate-200'
              }`}
            >
              [Global]
            </button>
            <button
              onClick={() => setSelectedChannel('Party')}
              className={`px-3 py-1.5 rounded-lg border font-bold transition-all ${
                selectedChannel === 'Party'
                  ? 'bg-blue-500/20 border-blue-500/60 text-blue-300 shadow-[0_0_12px_rgba(59,130,246,0.25)]'
                  : 'bg-slate-900/60 border-slate-800 text-slate-400 hover:text-slate-200'
              }`}
            >
              [Party]
            </button>
            <button
              onClick={() => setSelectedChannel('Direct')}
              className={`px-3 py-1.5 rounded-lg border font-bold transition-all ${
                selectedChannel === 'Direct'
                  ? 'bg-purple-500/20 border-purple-500/60 text-purple-300 shadow-[0_0_12px_rgba(168,85,247,0.25)]'
                  : 'bg-slate-900/60 border-slate-800 text-slate-400 hover:text-slate-200'
              }`}
            >
              [Direct / Whisper]
            </button>
            {distinctOrgChannels.map((orgCh) => (
              <button
                key={orgCh}
                onClick={() => setSelectedChannel(orgCh)}
                className={`px-3 py-1.5 rounded-lg border font-bold transition-all ${
                  selectedChannel.toLowerCase() === orgCh.toLowerCase()
                    ? 'bg-amber-500/20 border-amber-500/60 text-amber-300 shadow-[0_0_12px_rgba(245,158,11,0.25)]'
                    : 'bg-slate-900/60 border-slate-800 text-amber-400/80 hover:text-amber-300'
                }`}
              >
                [{orgCh}]
              </button>
            ))}
          </div>

          {/* Scanner Controls Pod */}
          <div className="flex items-center space-x-1.5 bg-slate-950/60 border border-slate-800/80 rounded-lg p-1 shrink-0">
            <button
              onClick={handleTriggerScan}
              disabled={scanningNow}
              className="px-2.5 py-1 rounded bg-cyan-600 hover:bg-cyan-500 text-white font-bold text-[11px] flex items-center space-x-1 shadow-sm transition-colors disabled:opacity-50 cursor-pointer"
              title="In-Game Chatbereich jetzt sofort scannen"
            >
              <RefreshCw className={`w-3.5 h-3.5 ${scanningNow ? 'animate-spin' : ''}`} />
              <span>Scan</span>
            </button>

            <button
              onClick={handleSelectRegion}
              disabled={selectingRegion}
              className="px-2 py-1 rounded bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white text-[11px] border border-slate-700/60 flex items-center space-x-1 transition-colors disabled:opacity-50 cursor-pointer"
              title="Chatfenster-Scanbereich am Bildschirm markieren"
            >
              <Crop className={`w-3.5 h-3.5 text-cyan-400 ${selectingRegion ? 'animate-spin' : ''}`} />
              <span>Bereich</span>
            </button>

            <button
              onClick={handleToggleOcr}
              className={`px-2 py-1 rounded text-[11px] border font-bold transition-colors cursor-pointer ${
                ocrEnabled
                  ? 'border-emerald-600/50 bg-emerald-950/50 text-emerald-300 hover:bg-emerald-900/60'
                  : 'border-slate-800 bg-slate-900 text-slate-400 hover:bg-slate-800'
              }`}
              title="Automatischen OCR-Hintergrundscanner (3,5s) starten oder pausieren"
            >
              <span>{ocrEnabled ? 'Live: Ein' : 'Live: Aus'}</span>
            </button>
          </div>
        </div>

        {/* Tier 2: Search & Flags on Left, Table Controls on Right */}
        <div className="flex flex-wrap items-center justify-between gap-3 pt-2 border-t border-slate-800/60">
          {/* Search & Flagged Filter */}
          <div className="flex items-center space-x-3 flex-1 min-w-[240px] max-w-md">
            <div className="relative flex-1">
              <Search className="w-3.5 h-3.5 absolute left-3 top-1/2 -translate-y-1/2 text-slate-500" />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Spieler oder Chat-Nachricht suchen..."
                className="w-full bg-slate-950/80 border border-slate-800 rounded-lg pl-8 pr-3 py-1 text-slate-200 text-xs focus:outline-none focus:border-cyan-500 transition-colors"
              />
            </div>

            <label className="flex items-center space-x-1.5 text-slate-300 cursor-pointer select-none shrink-0">
              <input
                type="checkbox"
                checked={flaggedOnly}
                onChange={(e) => setFlaggedOnly(e.target.checked)}
                className="rounded border-slate-700 text-rose-500 focus:ring-0 cursor-pointer"
              />
              <span className="text-[11px]">Nur Markierte</span>
            </label>
          </div>

          {/* Table Action Buttons */}
          <div className="flex items-center space-x-2 shrink-0">
            <button
              onClick={() => setAutoScroll(!autoScroll)}
              className={`px-2.5 py-1.5 rounded-lg border text-[11px] transition-colors flex items-center space-x-1.5 cursor-pointer ${
                autoScroll
                  ? 'bg-cyan-950/40 border-cyan-500/40 text-cyan-300'
                  : 'bg-slate-900/60 border-slate-800 text-slate-400 hover:text-slate-200'
              }`}
              title="Automatisches Scrollen bei neuen OCR-Nachrichten"
            >
              <ChevronDown className={`w-3.5 h-3.5 ${autoScroll ? 'text-cyan-400' : ''}`} />
              <span>Auto-Scroll</span>
            </button>

            <button
              onClick={() => handleOpenReportModal()}
              className="px-3 py-1.5 rounded-lg bg-rose-600/80 hover:bg-rose-500 border border-rose-500 text-white font-bold transition-colors flex items-center space-x-1.5 shadow-lg shadow-rose-950/40 cursor-pointer"
            >
              <ShieldAlert className="w-3.5 h-3.5" />
              <span>Report erstellen</span>
            </button>

            <button
              onClick={handleClearChat}
              className="p-1.5 rounded-lg bg-slate-900 border border-slate-800 text-slate-400 hover:text-rose-400 hover:border-rose-900 transition-colors cursor-pointer"
              title="Chat-Protokoll leeren"
            >
              <Trash2 className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>

      {/* Main Chat Chronicle Container */}
      <div className="sc-glass rounded-xl border border-slate-800 flex-1 flex flex-col min-h-[420px] max-h-[64vh] overflow-hidden">
        {/* Table Header */}
        <div className="flex items-center gap-2.5 px-3 py-1.5 bg-slate-950/90 border-b border-slate-800 text-[10px] font-bold text-slate-400 uppercase tracking-wider shrink-0 select-none">
          <div className="w-16 shrink-0">Zeit</div>
          <div className="w-20 shrink-0">Kanal</div>
          <div className="w-36 shrink-0">Sender (Pilot)</div>
          <div className="flex-1 min-w-0">Nachricht</div>
          <div className="w-14 text-right shrink-0">Aktion</div>
        </div>

        {/* Message Chronicle Rows */}
        <div ref={tableContainerRef} className="flex-1 overflow-y-auto divide-y divide-slate-800/30 p-1">
          {loading && messages.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-20 text-slate-500 space-y-3">
              <RefreshCw className="w-6 h-6 animate-spin text-cyan-400" />
              <p className="text-xs">Lade In-Game Chat-Protokoll...</p>
            </div>
          ) : filteredMessages.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-16 text-slate-500 space-y-3">
              <MessageSquare className="w-8 h-8 text-slate-700" />
              <p className="text-xs font-semibold text-slate-400">
                {messages.length === 0
                  ? 'Noch keine Chat-Nachrichten erfasst.'
                  : 'Keine Nachrichten für den aktuellen Filter gefunden.'}
              </p>
              <p className="text-[11px] text-slate-600 max-w-md text-center">
                {messages.length === 0
                  ? 'Der optische Chat-Scanner überwacht den Star Citizen Chat-Bereich links oben im Spiel. Drücke "Scan", wähle den Bereich oder starte den Scanner.'
                  : 'Passe die Suchkriterien oder Kanäle an.'}
              </p>
              {messages.length === 0 && (
                <div className="flex items-center space-x-2 pt-2">
                  <button
                    onClick={handleTriggerScan}
                    disabled={scanningNow}
                    className="px-3 py-1.5 rounded-lg bg-cyan-600 hover:bg-cyan-500 text-white font-bold text-xs flex items-center space-x-1.5 shadow-lg shadow-cyan-950/50 cursor-pointer disabled:opacity-50"
                  >
                    <RefreshCw className={`w-3.5 h-3.5 ${scanningNow ? 'animate-spin' : ''}`} />
                    <span>Jetzt Chat scannen</span>
                  </button>
                  <button
                    onClick={handleSelectRegion}
                    disabled={selectingRegion}
                    className="px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 text-xs border border-slate-700 flex items-center space-x-1.5 cursor-pointer disabled:opacity-50"
                  >
                    <Crop className="w-3.5 h-3.5 text-cyan-400" />
                    <span>Chat-Bereich auswählen</span>
                  </button>
                </div>
              )}
            </div>
          ) : (
            filteredMessages.map((msg) => {
              const channelUpper = msg.channel.toUpperCase();
              let channelBadgeClass = 'bg-cyan-950/60 border-cyan-500/40 text-cyan-300';
              if (channelUpper.includes('PARTY') || channelUpper.includes('GRUPPE')) {
                channelBadgeClass = 'bg-blue-950/60 border-blue-500/40 text-blue-300';
              } else if (
                channelUpper.includes('DIRECT') ||
                channelUpper.includes('WHISPER') ||
                channelUpper.includes('DM')
              ) {
                channelBadgeClass = 'bg-purple-950/60 border-purple-500/40 text-purple-300';
              } else if (!channelUpper.includes('GLOBAL')) {
                channelBadgeClass = 'bg-amber-950/60 border-amber-500/40 text-amber-300';
              }

              const timeFormatted = msg.timestamp
                ? msg.timestamp.length >= 19
                  ? msg.timestamp.substring(11, 19)
                  : msg.timestamp
                : '—';

              return (
                <div
                  key={msg.id}
                  className={`flex items-baseline gap-2.5 px-3 py-1 rounded text-xs transition-colors group ${
                    msg.isFlagged
                      ? 'bg-rose-950/25 border border-rose-500/30 text-rose-100 hover:bg-rose-950/40'
                      : 'hover:bg-slate-900/60 text-slate-300'
                  }`}
                >
                  {/* Timestamp */}
                  <div
                    className="w-16 shrink-0 text-[11px] text-slate-500 font-mono"
                    title={msg.timestamp}
                  >
                    {timeFormatted}
                  </div>

                  {/* Channel Badge */}
                  <div className="w-20 shrink-0">
                    <span
                      className={`inline-block px-1.5 py-0.5 rounded text-[10px] font-bold border truncate max-w-full ${channelBadgeClass}`}
                      title={msg.channel}
                    >
                      [{msg.channel}]
                    </span>
                  </div>

                  {/* Sender */}
                  <div className="w-36 shrink-0 flex items-center overflow-hidden">
                    <button
                      onClick={(e) => handleOpenDossier(msg.sender, e)}
                      className="font-bold text-cyan-400 hover:text-cyan-300 hover:underline truncate flex items-center space-x-1 text-left text-xs"
                      title="Citizen Dossier aufrufen"
                    >
                      <User className="w-3 h-3 shrink-0 opacity-70" />
                      <span className="truncate">{msg.sender}</span>
                    </button>
                  </div>

                  {/* Message Body */}
                  <div className="flex-1 min-w-0 break-words font-sans select-text text-xs leading-snug text-slate-200">
                    {msg.message}
                  </div>

                  {/* Actions (Flag / Incident Report) */}
                  <div className="w-14 shrink-0 flex items-center justify-end space-x-1">
                    <button
                      onClick={(e) => handleToggleFlag(msg, e)}
                      className={`p-1 rounded transition-colors cursor-pointer ${
                        msg.isFlagged
                          ? 'text-rose-400 hover:text-rose-300'
                          : 'text-slate-600 hover:text-rose-400 opacity-0 group-hover:opacity-100'
                      }`}
                      title={
                        msg.isFlagged
                          ? 'Markierung für Support-Report aufheben'
                          : 'Als Vorfall / Beweis markieren'
                      }
                    >
                      {msg.isFlagged ? (
                        <BookmarkCheck className="w-3.5 h-3.5 fill-rose-500/20" />
                      ) : (
                        <Bookmark className="w-3.5 h-3.5" />
                      )}
                    </button>

                    <button
                      onClick={() => handleOpenReportModal(msg.sender, msg)}
                      className="p-1 rounded text-slate-500 hover:text-rose-400 opacity-0 group-hover:opacity-100 transition-opacity cursor-pointer"
                      title="Diesen Vorfall direkt an CIG melden"
                    >
                      <ShieldAlert className="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>
              );
            })
          )}
        </div>
      </div>

      {/* Modals */}
      <PlayerReportModal
        isOpen={reportModalOpen}
        onClose={() => setReportModalOpen(false)}
        initialSuspect={reportSuspect}
        selectedMessages={reportSelectedMsgs}
      />

      <PilotDossierModal
        isOpen={dossierModalOpen}
        profile={selectedPilot}
        onClose={() => setDossierModalOpen(false)}
      />
    </div>
  );
};
