import React, { useState } from 'react';
import {
  Puzzle,
  FolderOpen,
  RotateCw,
  ExternalLink,
  Cpu,
  Globe,
  Copy,
  Check,
  Info,
  Layers
} from 'lucide-react';
import { bridge, PluginDto } from '../services/photinoBridge';

interface SettingsPluginsTabProps {
  plugins: PluginDto[];
  serverPort: number;
  pluginsDirectory: string;
  onRefresh: () => void;
}

export const SettingsPluginsTab: React.FC<SettingsPluginsTabProps> = ({
  plugins,
  serverPort,
  pluginsDirectory,
  onRefresh
}) => {
  const [isReloading, setIsReloading] = useState(false);
  const [copiedId, setCopiedId] = useState<string | null>(null);

  const handleToggle = async (plugin: PluginDto) => {
    try {
      await bridge.togglePlugin(plugin.id, !plugin.enabled);
      onRefresh();
    } catch (err) {
      console.error('Failed to toggle plugin:', err);
    }
  };

  const handleReload = async () => {
    setIsReloading(true);
    try {
      await bridge.reloadPlugins();
      onRefresh();
    } catch (err) {
      console.error('Failed to reload plugins:', err);
    } finally {
      setTimeout(() => setIsReloading(false), 500);
    }
  };

  const handleOpenFolder = () => {
    bridge.openPluginsFolder();
  };

  const handleCopyObsUrl = (plugin: PluginDto) => {
    const url = `http://127.0.0.1:${serverPort}/plugins/${plugin.id}/${plugin.entry || 'index.html'}`;
    navigator.clipboard.writeText(url);
    setCopiedId(plugin.id);
    setTimeout(() => setCopiedId(null), 2000);
  };

  return (
    <div className="space-y-6">
      {/* Top Banner & Folder Action Bar */}
      <div className="p-4 rounded-xl bg-slate-900/60 border border-slate-800 flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        <div className="flex items-start gap-3">
          <div className="w-10 h-10 rounded-xl bg-cyan-500/10 border border-cyan-500/30 flex items-center justify-center text-cyan-400 shrink-0">
            <Puzzle className="w-5 h-5" />
          </div>
          <div>
            <h3 className="text-sm font-semibold text-slate-100 flex items-center gap-2">
              SCLogMate Plugin-System
              <span className="text-[10px] px-2 py-0.5 rounded-full bg-cyan-500/20 text-cyan-300 font-mono">
                Port {serverPort}
              </span>
            </h3>
            <p className="text-xs text-slate-400 mt-1 max-w-xl leading-relaxed">
              Erweitere SCLogMate mit benutzerdefinierten Web-Dashboards, HUD-Widgets, OBS-Overlays oder nativen C#-Erweiterungen.
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2.5 shrink-0 self-end md:self-auto">
          <button
            onClick={handleReload}
            disabled={isReloading}
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700/80 transition-all disabled:opacity-50"
          >
            <RotateCw className={`w-3.5 h-3.5 ${isReloading ? 'animate-spin text-cyan-400' : 'text-slate-400'}`} />
            <span>Neu laden</span>
          </button>

          <button
            onClick={handleOpenFolder}
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium rounded-lg bg-cyan-600/20 hover:bg-cyan-600/30 text-cyan-300 border border-cyan-500/40 transition-all"
          >
            <FolderOpen className="w-3.5 h-3.5 text-cyan-400" />
            <span>Plugin-Ordner öffnen</span>
          </button>
        </div>
      </div>

      {/* Installed Plugins List */}
      <div className="space-y-3">
        <div className="flex items-center justify-between px-1">
          <h4 className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
            Installierte Erweiterungen ({plugins.length})
          </h4>
          <span className="text-[11px] text-slate-500 font-mono truncate max-w-md" title={pluginsDirectory}>
            {pluginsDirectory}
          </span>
        </div>

        {plugins.length === 0 ? (
          <div className="p-8 rounded-xl bg-slate-900/30 border border-dashed border-slate-800 text-center flex flex-col items-center justify-center gap-2">
            <Puzzle className="w-8 h-8 text-slate-600 mb-1" />
            <p className="text-sm font-medium text-slate-300">Keine Plugins gefunden</p>
            <p className="text-xs text-slate-500 max-w-md leading-relaxed">
              Lege einen neuen Ordner mit einer <code className="text-cyan-400">manifest.json</code> in das Plugin-Verzeichnis, um eigene Dashboards oder Erweiterungen hinzuzufügen.
            </p>
            <button
              onClick={handleOpenFolder}
              className="mt-3 px-3 py-1.5 text-xs font-medium rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 transition-all"
            >
              Plugin-Ordner im Explorer anzeigen
            </button>
          </div>
        ) : (
          <div className="grid gap-3">
            {plugins.map((plugin) => (
              <div
                key={plugin.id}
                className={`p-4 rounded-xl border transition-all ${
                  plugin.enabled
                    ? 'bg-slate-900/70 border-slate-800 hover:border-slate-700/80 shadow-sm'
                    : 'bg-slate-950/40 border-slate-800/50 opacity-60'
                }`}
              >
                <div className="flex items-start justify-between gap-4">
                  <div className="flex items-start gap-3 min-w-0">
                    <div
                      className={`w-9 h-9 rounded-xl flex items-center justify-center shrink-0 border ${
                        plugin.enabled
                          ? 'bg-cyan-500/10 border-cyan-500/30 text-cyan-400'
                          : 'bg-slate-800 border-slate-700 text-slate-500'
                      }`}
                    >
                      <Puzzle className="w-4 h-4" />
                    </div>

                    <div className="min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <h4 className="text-sm font-semibold text-slate-100">{plugin.name}</h4>
                        <span className="text-[10px] px-2 py-0.5 rounded-full bg-slate-800 text-slate-400 border border-slate-700 font-mono">
                          v{plugin.version}
                        </span>
                        {plugin.hasBackend && (
                          <span className="flex items-center gap-1 text-[10px] px-2 py-0.5 rounded-full bg-emerald-500/10 text-emerald-400 border border-emerald-500/30 font-medium">
                            <Cpu className="w-3 h-3" /> Native C#
                          </span>
                        )}
                        <span className="flex items-center gap-1 text-[10px] px-2 py-0.5 rounded-full bg-cyan-500/10 text-cyan-400 border border-cyan-500/30 font-medium">
                          <Globe className="w-3 h-3" /> Web
                        </span>
                        {plugin.sidebar && (
                          <span className="flex items-center gap-1 text-[10px] px-2 py-0.5 rounded-full bg-indigo-500/10 text-indigo-400 border border-indigo-500/30 font-medium">
                            <Layers className="w-3 h-3" /> Sidebar: {plugin.sidebar.label}
                          </span>
                        )}
                      </div>

                      {plugin.description && (
                        <p className="text-xs text-slate-400 mt-1 leading-relaxed">{plugin.description}</p>
                      )}

                      <div className="flex items-center gap-4 mt-2 text-[11px] text-slate-500">
                        {plugin.author && <span>Autor: <strong className="text-slate-400">{plugin.author}</strong></span>}
                        {plugin.entry && <span>Entry: <code className="text-slate-400">{plugin.entry}</code></span>}
                        {plugin.backendDll && <span>Backend: <code className="text-emerald-400">{plugin.backendDll}</code></span>}
                      </div>
                    </div>
                  </div>

                  {/* Actions & Toggle Switch */}
                  <div className="flex items-center gap-3 shrink-0">
                    <button
                      onClick={() => handleCopyObsUrl(plugin)}
                      title="OBS Browser-Source URL kopieren"
                      className="p-1.5 rounded-lg bg-slate-800/80 hover:bg-slate-700 text-slate-400 hover:text-slate-200 border border-slate-700/80 transition-all"
                    >
                      {copiedId === plugin.id ? <Check className="w-4 h-4 text-emerald-400" /> : <Copy className="w-4 h-4" />}
                    </button>

                    <button
                      onClick={() => bridge.openExternalUrl(`http://127.0.0.1:${serverPort}/plugins/${plugin.id}/${plugin.entry || 'index.html'}`)}
                      title="Im externen Browser öffnen"
                      className="p-1.5 rounded-lg bg-slate-800/80 hover:bg-slate-700 text-slate-400 hover:text-slate-200 border border-slate-700/80 transition-all"
                    >
                      <ExternalLink className="w-4 h-4" />
                    </button>

                    {/* Toggle Switch */}
                    <button
                      onClick={() => handleToggle(plugin)}
                      role="switch"
                      aria-checked={plugin.enabled}
                      className={`relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none ${
                        plugin.enabled ? 'bg-cyan-600' : 'bg-slate-800'
                      }`}
                    >
                      <span
                        className={`pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out ${
                          plugin.enabled ? 'translate-x-5' : 'translate-x-0'
                        }`}
                      />
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Developer Guide / Information Card */}
      <div className="p-4 rounded-xl bg-slate-900/40 border border-slate-800/80 flex items-start gap-3">
        <Info className="w-4 h-4 text-cyan-400 mt-0.5 shrink-0" />
        <div className="text-xs text-slate-400 leading-relaxed space-y-1">
          <p className="font-semibold text-slate-300">Entwickler-Tipp: Eigene Plugins erstellen</p>
          <p>
            Ein Plugin besteht aus einem Ordner in <code className="text-slate-300 font-mono">{pluginsDirectory}</code> mit einer <code className="text-cyan-400 font-mono">manifest.json</code> und einer <code className="text-cyan-400 font-mono">index.html</code>.
            Binde einfach <code className="text-cyan-400 font-mono">&lt;script src="/sclogmate.js"&gt;&lt;/script&gt;</code> ein, um Zugriff auf Telemetrie, Logs, Hangar-Snapshots und System-Events zu erhalten.
          </p>
        </div>
      </div>
    </div>
  );
};
