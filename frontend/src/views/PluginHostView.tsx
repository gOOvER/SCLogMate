import React, { useEffect, useRef, useState } from 'react';
import { RotateCw, ExternalLink, Copy, Check, Puzzle, Cpu, Globe } from 'lucide-react';
import { bridge, PluginDto } from '../services/photinoBridge';

interface PluginHostViewProps {
  plugin: PluginDto;
  serverPort: number;
}

export const PluginHostView: React.FC<PluginHostViewProps> = ({ plugin, serverPort }) => {
  const iframeRef = useRef<HTMLIFrameElement>(null);
  const [copiedUrl, setCopiedUrl] = useState(false);
  const [isIframeLoading, setIsIframeLoading] = useState(true);
  const [iframeKey, setIframeKey] = useState(0);

  // The local loopback URL for the plugin web entry
  const pluginUrl = `http://127.0.0.1:${serverPort}/plugins/${plugin.id}/${plugin.entry || 'index.html'}`;

  // Handle postMessage communication from the plugin iframe
  useEffect(() => {
    const handleMessage = async (event: MessageEvent) => {
      // Ensure the message comes from the local plugin server
      if (!event.origin.startsWith(`http://127.0.0.1:${serverPort}`) && !event.origin.startsWith(`http://localhost:${serverPort}`)) {
        return;
      }

      const data = event.data;
      if (!data || typeof data !== 'object') return;

      // Handle RPC requests initiated by the plugin client SDK (sclogmate.js)
      if (data.type === 'sclogmate_plugin_rpc') {
        const { id, action, payload } = data;
        try {
          const res = await bridge.pluginRpc(plugin.id, action, payload);
          if (iframeRef.current?.contentWindow) {
            iframeRef.current.contentWindow.postMessage(
              {
                type: 'sclogmate_plugin_rpc_result',
                id,
                success: res.success,
                result: res.result,
                error: res.error
              },
              '*'
            );
          }
        } catch (err: any) {
          if (iframeRef.current?.contentWindow) {
            iframeRef.current.contentWindow.postMessage(
              {
                type: 'sclogmate_plugin_rpc_result',
                id,
                success: false,
                error: err?.message || 'RPC execution failed'
              },
              '*'
            );
          }
        }
      }
    };

    window.addEventListener('message', handleMessage);
    return () => window.removeEventListener('message', handleMessage);
  }, [plugin.id, serverPort]);

  // Forward live game events into the plugin iframe
  useEffect(() => {
    const forwardEventToIframe = (eventName: string, payload: any) => {
      if (iframeRef.current?.contentWindow) {
        iframeRef.current.contentWindow.postMessage(
          {
            type: 'sclogmate_plugin_event',
            event: eventName,
            payload
          },
          '*'
        );
      }
    };

    const unbindHud = bridge.on('HUD_UPDATE', (telemetry) => forwardEventToIframe('telemetry_update', telemetry));
    const unbindLog = bridge.on('LOG_EVENT', (logEntry) => forwardEventToIframe('log_event', logEntry));
    const unbindCustom = bridge.on(`PLUGIN_EVENT:${plugin.id}`, (customPayload) => forwardEventToIframe('plugin_custom_event', customPayload));

    return () => {
      unbindHud();
      unbindLog();
      unbindCustom();
    };
  }, [plugin.id]);

  const handleCopyUrl = () => {
    navigator.clipboard.writeText(pluginUrl);
    setCopiedUrl(true);
    setTimeout(() => setCopiedUrl(false), 2000);
  };

  const handleOpenExternal = () => {
    bridge.openExternalUrl(pluginUrl);
  };

  const handleReload = () => {
    setIsIframeLoading(true);
    setIframeKey((prev) => prev + 1);
  };

  return (
    <div className="flex flex-col h-full w-full bg-slate-950 overflow-hidden select-none">
      {/* Top action & info bar */}
      <div className="flex items-center justify-between px-5 py-3 bg-slate-900/80 border-b border-slate-800/80 backdrop-blur-md z-10 shrink-0">
        <div className="flex items-center gap-3 min-w-0">
          <div className="w-8 h-8 rounded-lg bg-cyan-500/10 border border-cyan-500/30 flex items-center justify-center text-cyan-400 shrink-0 shadow-sm shadow-cyan-950">
            <Puzzle className="w-4 h-4" />
          </div>
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <h2 className="text-sm font-semibold text-slate-100 truncate tracking-wide">{plugin.name}</h2>
              <span className="text-[10px] px-2 py-0.5 rounded-full bg-slate-800 text-slate-400 border border-slate-700/60 font-mono">
                v{plugin.version}
              </span>
              {plugin.hasBackend ? (
                <span className="flex items-center gap-1 text-[10px] px-2 py-0.5 rounded-full bg-emerald-500/10 text-emerald-400 border border-emerald-500/30 font-medium">
                  <Cpu className="w-3 h-3" /> Native C#
                </span>
              ) : (
                <span className="flex items-center gap-1 text-[10px] px-2 py-0.5 rounded-full bg-cyan-500/10 text-cyan-400 border border-cyan-500/30 font-medium">
                  <Globe className="w-3 h-3" /> Web
                </span>
              )}
            </div>
            {plugin.description && (
              <p className="text-xs text-slate-400 truncate mt-0.5 max-w-xl">{plugin.description}</p>
            )}
          </div>
        </div>

        {/* Right side controls */}
        <div className="flex items-center gap-2 shrink-0">
          <button
            onClick={handleCopyUrl}
            title="Kopiert die URL für OBS Studio Browser-Quellen oder den Browser"
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-md bg-slate-800/80 hover:bg-slate-700/80 text-slate-300 hover:text-white border border-slate-700 transition-all shadow-sm"
          >
            {copiedUrl ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5 text-slate-400" />}
            <span>{copiedUrl ? 'Kopiert!' : 'OBS URL'}</span>
          </button>

          <button
            onClick={handleOpenExternal}
            title="Im externen Standard-Webbrowser öffnen"
            className="p-1.5 rounded-md bg-slate-800/80 hover:bg-slate-700/80 text-slate-400 hover:text-slate-100 border border-slate-700 transition-all shadow-sm"
          >
            <ExternalLink className="w-4 h-4" />
          </button>

          <button
            onClick={handleReload}
            title="Plugin neu laden"
            className="p-1.5 rounded-md bg-slate-800/80 hover:bg-slate-700/80 text-slate-400 hover:text-slate-100 border border-slate-700 transition-all shadow-sm"
          >
            <RotateCw className={`w-4 h-4 ${isIframeLoading ? 'animate-spin text-cyan-400' : ''}`} />
          </button>
        </div>
      </div>

      {/* Main Iframe Host */}
      <div className="relative flex-1 w-full h-full bg-[#080d19]">
        {isIframeLoading && (
          <div className="absolute inset-0 flex items-center justify-center bg-slate-950/60 backdrop-blur-sm z-20 pointer-events-none">
            <div className="flex flex-col items-center gap-3">
              <div className="w-7 h-7 border-2 border-cyan-500/20 border-t-cyan-400 rounded-full animate-spin" />
              <span className="text-xs text-slate-400 font-medium">Lade Plugin...</span>
            </div>
          </div>
        )}
        <iframe
          key={iframeKey}
          ref={iframeRef}
          src={pluginUrl}
          className="w-full h-full border-0 outline-none"
          title={plugin.name}
          onLoad={() => setIsIframeLoading(false)}
          sandbox="allow-scripts allow-same-origin allow-forms allow-popups allow-modals"
        />
      </div>
    </div>
  );
};
