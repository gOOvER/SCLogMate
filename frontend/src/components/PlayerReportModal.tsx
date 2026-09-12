import React, { useState, useEffect } from 'react';
import {
  ShieldAlert,
  X,
  Copy,
  Check,
  ExternalLink,
  MessageSquare,
  UserX,
  FileText,
  AlertTriangle,
} from 'lucide-react';
import { ChatMessageDto, bridge } from '../services/photinoBridge';

interface PlayerReportModalProps {
  isOpen: boolean;
  onClose: () => void;
  initialSuspect?: string;
  selectedMessages: ChatMessageDto[];
}

const VIOLATION_CATEGORIES = [
  'Griefing / Pad Ramming / Stream Sniping',
  'Harassment / Hate Speech / In-Game Chat Abuse',
  'Exploiting / Duping / Combat Logging',
  'Scamming / AUEC Fraud',
  'Other Gameplay Violation',
];

export const PlayerReportModal: React.FC<PlayerReportModalProps> = ({
  isOpen,
  onClose,
  initialSuspect = '',
  selectedMessages,
}) => {
  const [suspect, setSuspect] = useState(initialSuspect);
  const [category, setCategory] = useState(VIOLATION_CATEGORIES[0]);
  const [description, setDescription] = useState('');
  const [activeMessageIds, setActiveMessageIds] = useState<number[]>([]);
  const [reportMarkdown, setReportMarkdown] = useState('');
  const [copied, setCopied] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setSuspect(initialSuspect);
      setActiveMessageIds(selectedMessages.map((m) => m.id));
      setCopied(false);
    }
  }, [isOpen, initialSuspect, selectedMessages]);

  useEffect(() => {
    if (!isOpen) return;

    let isMounted = true;
    setIsGenerating(true);

    bridge
      .exportPlayerReport({
        suspect: suspect.trim(),
        category,
        description: description.trim(),
        messageIds: activeMessageIds,
      })
      .then((res) => {
        if (isMounted && res && res.markdown) {
          setReportMarkdown(res.markdown);
        }
      })
      .catch((err) => console.error('Failed to generate player report:', err))
      .finally(() => {
        if (isMounted) setIsGenerating(false);
      });

    return () => {
      isMounted = false;
    };
  }, [isOpen, suspect, category, description, activeMessageIds]);

  if (!isOpen) return null;

  const toggleMessageSelection = (id: number) => {
    setActiveMessageIds((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]
    );
  };

  const handleCopy = () => {
    if (!reportMarkdown) return;
    navigator.clipboard.writeText(reportMarkdown).then(() => {
      setCopied(true);
      setTimeout(() => setCopied(false), 2500);
    });
  };

  const handleOpenRsiSupport = () => {
    bridge.openExternalUrl('https://support.robertsspaceindustries.com/hc/en-us/requests/new');
  };

  return (
    <div className="fixed inset-0 z-[120] flex items-center justify-center bg-black/85 backdrop-blur-md p-4 animate-in fade-in duration-200">
      <div className="sc-glass rounded-2xl w-full max-w-3xl border border-rose-500/40 shadow-2xl shadow-rose-950/60 p-6 relative overflow-hidden flex flex-col max-h-[92vh]">
        {/* Glowing Header Accent Bar */}
        <div className="absolute top-0 left-0 right-0 h-1 bg-gradient-to-r from-transparent via-rose-500 to-transparent" />

        {/* Top Header */}
        <div className="flex items-start justify-between gap-4 mb-5 shrink-0">
          <div className="flex items-center space-x-3.5">
            <div className="w-11 h-11 rounded-xl bg-rose-500/10 border border-rose-500/40 flex items-center justify-center text-rose-400 shadow-[0_0_20px_rgba(244,63,94,0.3)] shrink-0">
              <ShieldAlert className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center space-x-2">
                <h2 className="text-base font-bold text-white tracking-wide uppercase font-mono">
                  CIG Player Incident Report
                </h2>
                <span className="px-2 py-0.5 rounded text-[10px] font-mono font-bold bg-rose-950/80 border border-rose-500/50 text-rose-300">
                  SUPPORT TICKET
                </span>
              </div>
              <p className="text-xs text-slate-400 mt-0.5">
                Generiert einen verifizierten Vorfallbericht mit OCR-Chat-Protokoll für das RSI Player Support Team.
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800/60 transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Scrollable Body */}
        <div className="flex-1 overflow-y-auto pr-1 space-y-4 text-xs font-mono">
          {/* Input Grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            {/* Suspect Handle */}
            <div className="bg-slate-900/60 border border-slate-700/60 rounded-xl p-3">
              <label className="block text-[11px] font-bold text-slate-300 uppercase mb-1.5 flex items-center space-x-1.5">
                <UserX className="w-3.5 h-3.5 text-rose-400" />
                <span>Gemeldeter Spieler (Handle)</span>
              </label>
              <input
                type="text"
                value={suspect}
                onChange={(e) => setSuspect(e.target.value)}
                placeholder="z.B. Pirate_Gamer"
                className="w-full bg-slate-950/80 border border-slate-700/80 rounded-lg px-3 py-2 text-rose-300 font-bold focus:outline-none focus:border-rose-500 transition-colors placeholder:text-slate-600"
              />
            </div>

            {/* Violation Category */}
            <div className="bg-slate-900/60 border border-slate-700/60 rounded-xl p-3">
              <label className="block text-[11px] font-bold text-slate-300 uppercase mb-1.5 flex items-center space-x-1.5">
                <AlertTriangle className="w-3.5 h-3.5 text-amber-400" />
                <span>Verstoß-Kategorie</span>
              </label>
              <select
                value={category}
                onChange={(e) => setCategory(e.target.value)}
                className="w-full bg-slate-950/80 border border-slate-700/80 rounded-lg px-3 py-2 text-slate-200 focus:outline-none focus:border-rose-500 transition-colors text-xs"
              >
                {VIOLATION_CATEGORIES.map((c) => (
                  <option key={c} value={c} className="bg-slate-900 text-slate-200">
                    {c}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* Situation Narrative Description */}
          <div className="bg-slate-900/60 border border-slate-700/60 rounded-xl p-3">
            <label className="block text-[11px] font-bold text-slate-300 uppercase mb-1.5 flex items-center space-x-1.5">
              <FileText className="w-3.5 h-3.5 text-cyan-400" />
              <span>Eigene Sachverhaltsdarstellung (Optional)</span>
            </label>
            <textarea
              rows={3}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Beschreibe kurz den Kontext des Vorfalls (z.B. Spieler hat wiederholt Schiffe auf dem Pad zerstört oder im Global Chat beleidigt)..."
              className="w-full bg-slate-950/80 border border-slate-700/80 rounded-lg p-2.5 text-slate-300 focus:outline-none focus:border-cyan-500 transition-colors placeholder:text-slate-600 resize-none font-sans text-xs"
            />
          </div>

          {/* Evidence Messages Selection */}
          <div className="bg-slate-900/60 border border-slate-700/60 rounded-xl p-3">
            <div className="flex items-center justify-between mb-2">
              <div className="flex items-center space-x-2">
                <MessageSquare className="w-3.5 h-3.5 text-purple-400" />
                <span className="text-[11px] font-bold text-slate-300 uppercase">
                  Beweismaterial aus Chat-Log ({activeMessageIds.length} von {selectedMessages.length} gewählt)
                </span>
              </div>
              <div className="flex space-x-2 text-[10px]">
                <button
                  type="button"
                  onClick={() => setActiveMessageIds(selectedMessages.map((m) => m.id))}
                  className="text-cyan-400 hover:underline"
                >
                  Alle wählen
                </button>
                <span className="text-slate-600">|</span>
                <button
                  type="button"
                  onClick={() => setActiveMessageIds([])}
                  className="text-slate-400 hover:underline"
                >
                  Keine
                </button>
              </div>
            </div>

            {selectedMessages.length === 0 ? (
              <p className="text-slate-500 text-[11px] italic py-2 text-center">
                Keine Chat-Zeilen im Chat-Protokoll markiert. Wähle im Chat-Log Zeilen aus, um sie dem Bericht beizufügen.
              </p>
            ) : (
              <div className="max-h-36 overflow-y-auto space-y-1 pr-1">
                {selectedMessages.map((msg) => {
                  const isSelected = activeMessageIds.includes(msg.id);
                  return (
                    <div
                      key={msg.id}
                      onClick={() => toggleMessageSelection(msg.id)}
                      className={`flex items-center justify-between p-2 rounded-lg border cursor-pointer transition-colors ${
                        isSelected
                          ? 'bg-rose-950/30 border-rose-500/40 text-slate-200'
                          : 'bg-slate-950/40 border-slate-800/80 text-slate-500 hover:border-slate-700'
                      }`}
                    >
                      <div className="flex items-center space-x-2.5 overflow-hidden">
                        <input
                          type="checkbox"
                          checked={isSelected}
                          onChange={() => {}}
                          className="rounded border-slate-700 text-rose-500 focus:ring-0 cursor-pointer"
                        />
                        <span className="text-[10px] text-slate-400 shrink-0">
                          {msg.timestamp?.substring(11, 19) || '—'}
                        </span>
                        <span className="text-[10px] px-1.5 py-0.2 rounded bg-slate-800 text-slate-300 shrink-0">
                          [{msg.channel}]
                        </span>
                        <span className="font-bold text-rose-400 shrink-0">{msg.sender}:</span>
                        <span className="truncate text-[11px]">{msg.message}</span>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          {/* Generated Ticket Preview */}
          <div className="bg-slate-950/90 border border-slate-800 rounded-xl p-3">
            <div className="flex items-center justify-between mb-1.5">
              <span className="text-[10px] uppercase font-bold text-slate-400 tracking-wider">
                Vorschau Ticket-Text (Markdown)
              </span>
              {isGenerating && <span className="text-[10px] text-cyan-400 animate-pulse">Wird aktualisiert...</span>}
            </div>
            <pre className="text-[11px] text-slate-300 font-mono whitespace-pre-wrap bg-slate-900/90 p-3 rounded-lg border border-slate-800 max-h-44 overflow-y-auto select-all leading-relaxed">
              {reportMarkdown || 'Erstelle Vorfallbericht...'}
            </pre>
          </div>
        </div>

        {/* Footer Actions */}
        <div className="flex items-center justify-between gap-3 pt-4 mt-2 border-t border-slate-800/80 shrink-0">
          <button
            onClick={handleOpenRsiSupport}
            className="flex items-center space-x-2 px-3.5 py-2 rounded-xl text-xs font-mono font-bold bg-slate-800 hover:bg-slate-700 border border-slate-600 text-slate-300 transition-colors"
          >
            <ExternalLink className="w-3.5 h-3.5 text-cyan-400" />
            <span>RSI Support öffnen</span>
          </button>

          <div className="flex items-center space-x-2.5">
            <button
              onClick={handleCopy}
              className={`flex items-center space-x-2 px-4 py-2 rounded-xl text-xs font-mono font-bold transition-all shadow-lg ${
                copied
                  ? 'bg-emerald-600 text-white shadow-emerald-900/50'
                  : 'bg-rose-600 hover:bg-rose-500 text-white shadow-rose-900/40 hover:shadow-rose-900/60'
              }`}
            >
              {copied ? (
                <>
                  <Check className="w-4 h-4 text-white" />
                  <span>Kopiert!</span>
                </>
              ) : (
                <>
                  <Copy className="w-4 h-4 text-rose-100" />
                  <span>Report kopieren</span>
                </>
              )}
            </button>
            <button
              onClick={onClose}
              className="px-4 py-2 rounded-xl text-xs font-mono text-slate-400 hover:text-white hover:bg-slate-800/60 transition-colors"
            >
              Schließen
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
