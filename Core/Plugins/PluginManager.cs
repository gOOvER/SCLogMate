using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using SCLogMate.Models;

namespace SCLogMate.Core.Plugins;

/// <summary>
/// Central manager for discovering, loading, hosting, and executing SCLogMate plugins.
/// Supports hybrid plugins combining Web UI (HTML/JS/CSS) and native C# assemblies.
/// </summary>
public sealed class PluginManager : IDisposable
{
    private static readonly Lazy<PluginManager> _instance = new(() => new PluginManager());
    public static PluginManager Instance => _instance.Value;

    private readonly string _pluginsDirectory;
    private readonly PluginHttpServer _httpServer;
    private readonly ConcurrentDictionary<string, PluginManifest> _plugins = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, NativePluginInstance> _nativePlugins = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Func<JsonElement, object?>> _rpcHandlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Action<LogEntry>> _logSubscribers = new();
    private readonly object _lock = new();

    public int ServerPort => _httpServer.Port;
    public string PluginsDirectory => _pluginsDirectory;

    public PluginManager()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _pluginsDirectory = Path.Combine(appData, "SCLogMate", "Plugins");
        _httpServer = new PluginHttpServer(_pluginsDirectory);
    }

    /// <summary>
    /// Initializes the plugin subsystem: ensures folders, provisions sample plugin if empty,
    /// starts loopback HTTP server, and loads all valid manifests and native assemblies.
    /// </summary>
    public void Initialize()
    {
        lock (_lock)
        {
            try
            {
                if (!Directory.Exists(_pluginsDirectory))
                {
                    Directory.CreateDirectory(_pluginsDirectory);
                }

                EnsureSamplePlugin();
                _httpServer.Start();
                ReloadPlugins();
            }
            catch (Exception ex)
            {
                Logger.Error("[PluginManager] Failed to initialize plugin subsystem", ex);
            }
        }
    }

    /// <summary>
    /// Reloads all plugins from the plugins directory.
    /// </summary>
    public void ReloadPlugins()
    {
        lock (_lock)
        {
            // Shutdown existing native plugins
            foreach (var (id, native) in _nativePlugins)
            {
                try
                {
                    native.Plugin.Shutdown();
                    native.Context.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Error($"[PluginManager] Error shutting down plugin '{id}'", ex);
                }
            }
            _nativePlugins.Clear();
            _plugins.Clear();
            _rpcHandlers.Clear();

            var settings = Settings.Load();
            var disabledSet = new HashSet<string>(settings.DisabledPluginIds ?? new(), StringComparer.OrdinalIgnoreCase);

            if (!Directory.Exists(_pluginsDirectory)) return;

            foreach (var dir in Directory.GetDirectories(_pluginsDirectory))
            {
                var manifestPath = Path.Combine(dir, "manifest.json");
                if (!File.Exists(manifestPath)) continue;

                try
                {
                    var json = File.ReadAllText(manifestPath);
                    var manifest = JsonSerializer.Deserialize<PluginManifest>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (manifest == null || string.IsNullOrWhiteSpace(manifest.Id))
                    {
                        Logger.Log($"[PluginManager] Skipping invalid manifest in {dir}");
                        continue;
                    }

                    manifest.PluginDirectory = dir;
                    manifest.Enabled = !disabledSet.Contains(manifest.Id);

                    _plugins[manifest.Id] = manifest;

                    if (manifest.Enabled && manifest.HasBackend)
                    {
                        LoadNativeBackend(manifest);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"[PluginManager] Failed to parse manifest in {dir}", ex);
                }
            }

            Logger.Log($"[PluginManager] Discovered {_plugins.Count} plugins ({_plugins.Values.Count(p => p.Enabled)} enabled, {_nativePlugins.Count} native C# instances).");
        }
    }

    private void LoadNativeBackend(PluginManifest manifest)
    {
        var dllPath = Path.Combine(manifest.PluginDirectory, manifest.BackendDll!);
        if (!File.Exists(dllPath))
        {
            Logger.Log($"[PluginManager] Backend DLL not found for plugin '{manifest.Id}': {dllPath}");
            return;
        }

        try
        {
            var loadContext = new PluginAssemblyLoadContext(dllPath);
            var assembly = loadContext.LoadFromAssemblyPath(dllPath);

            var pluginType = assembly.GetTypes().FirstOrDefault(t => typeof(ISCPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
            if (pluginType == null)
            {
                Logger.Log($"[PluginManager] No ISCPlugin implementation found in {dllPath}");
                return;
            }

            var pluginInstance = (ISCPlugin)Activator.CreateInstance(pluginType)!;
            var context = new PluginContext(manifest, this);

            pluginInstance.Initialize(context);
            _nativePlugins[manifest.Id] = new NativePluginInstance(pluginInstance, loadContext);

            Logger.Log($"[PluginManager] Native C# backend loaded for plugin '{manifest.Id}' ({pluginType.FullName})");
        }
        catch (Exception ex)
        {
            Logger.Error($"[PluginManager] Failed to load native plugin '{manifest.Id}' from {dllPath}", ex);
        }
    }

    /// <summary>
    /// Returns the list of all installed plugins with runtime metadata.
    /// </summary>
    public IReadOnlyList<PluginManifest> GetPlugins()
    {
        lock (_lock)
        {
            return _plugins.Values.ToList();
        }
    }

    /// <summary>
    /// Enables or disables a plugin by ID and updates user settings.
    /// </summary>
    public void TogglePlugin(string pluginId, bool enabled)
    {
        lock (_lock)
        {
            if (!_plugins.TryGetValue(pluginId, out var manifest)) return;

            manifest.Enabled = enabled;
            var settings = Settings.Load();
            settings.DisabledPluginIds ??= new();

            if (enabled)
            {
                settings.DisabledPluginIds.RemoveAll(id => id.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
                if (manifest.HasBackend && !_nativePlugins.ContainsKey(pluginId))
                {
                    LoadNativeBackend(manifest);
                }
            }
            else
            {
                if (!settings.DisabledPluginIds.Contains(pluginId, StringComparer.OrdinalIgnoreCase))
                {
                    settings.DisabledPluginIds.Add(pluginId);
                }
                if (_nativePlugins.TryRemove(pluginId, out var native))
                {
                    try
                    {
                        native.Plugin.Shutdown();
                        native.Context.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[PluginManager] Error shutting down disabled plugin '{pluginId}'", ex);
                    }
                }
            }

            Settings.Save(settings);
            Logger.Log($"[PluginManager] Plugin '{pluginId}' state changed to: {(enabled ? "Enabled" : "Disabled")}");
        }
    }

    /// <summary>
    /// Opens the operating system file explorer at the plugins directory.
    /// </summary>
    public void OpenPluginsFolder()
    {
        try
        {
            if (!Directory.Exists(_pluginsDirectory))
            {
                Directory.CreateDirectory(_pluginsDirectory);
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = _pluginsDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Logger.Error("[PluginManager] Failed to open plugins directory in Explorer", ex);
        }
    }

    /// <summary>
    /// Forwards parsed Star Citizen log entries to registered native plugins.
    /// </summary>
    public void BroadcastLogEvent(LogEntry entry)
    {
        lock (_lock)
        {
            foreach (var sub in _logSubscribers)
            {
                try { sub(entry); }
                catch (Exception ex) { Logger.Error("[PluginManager] Error in plugin log subscriber", ex); }
            }
        }
    }

    /// <summary>
    /// Registers an RPC handler for a specific plugin action.
    /// </summary>
    public void RegisterRpcHandler(string action, Func<JsonElement, object?> handler)
    {
        _rpcHandlers[action] = handler;
    }

    /// <summary>
    /// Executes an RPC action invoked by a plugin Web UI iframe.
    /// </summary>
    public object? HandleRpcAction(string action, JsonElement payload)
    {
        if (_rpcHandlers.TryGetValue(action, out var handler))
        {
            return handler(payload);
        }
        return null;
    }

    internal void AddLogSubscriber(Action<LogEntry> callback)
    {
        lock (_lock)
        {
            _logSubscribers.Add(callback);
        }
    }

    /// <summary>
    /// Deploys the pre-bundled starter sample plugin if the directory is empty.
    /// </summary>
    private void EnsureSamplePlugin()
    {
        var sampleDir = Path.Combine(_pluginsDirectory, "sample-telemetry-widget");
        if (Directory.Exists(sampleDir) && File.Exists(Path.Combine(sampleDir, "manifest.json")))
            return;

        try
        {
            Directory.CreateDirectory(sampleDir);

            // 1. Write manifest.json
            var manifestContent = @"{
  ""id"": ""sample-telemetry-widget"",
  ""name"": ""Live Telemetry Widget"",
  ""version"": ""1.0.0"",
  ""author"": ""SCLogMate Team"",
  ""description"": ""Demonstration plugin showing how to build custom dashboards using the SCLogMate JavaScript SDK."",
  ""entry"": ""index.html"",
  ""sidebar"": {
    ""label"": ""Telemetry Widget"",
    ""group"": ""Erweiterungen"",
    ""icon"": ""Activity"",
    ""order"": 10
  }
}";
            File.WriteAllText(Path.Combine(sampleDir, "manifest.json"), manifestContent);

            // 2. Write index.html
            var htmlContent = @"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>SCLogMate Plugin - Live Telemetry</title>
  <script src=""/sclogmate.js""></script>
  <style>
    body {
      margin: 0;
      padding: 24px;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
      background: #030712;
      color: #e2e8f0;
      box-sizing: border-box;
    }
    .card {
      background: rgba(15, 23, 42, 0.75);
      border: 1px solid rgba(6, 182, 212, 0.3);
      border-radius: 8px;
      padding: 20px;
      margin-bottom: 20px;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.5);
    }
    h1 {
      font-size: 20px;
      margin-top: 0;
      color: #38bdf8;
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
      margin-top: 16px;
    }
    .kpi {
      background: #091322;
      border: 1px solid #1e293b;
      border-radius: 6px;
      padding: 12px 16px;
    }
    .kpi-title {
      font-size: 11px;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: #94a3b8;
    }
    .kpi-value {
      font-size: 20px;
      font-weight: 700;
      color: #22d3ee;
      margin-top: 4px;
      font-family: monospace;
    }
    .log-feed {
      background: #020617;
      border: 1px solid #1e293b;
      border-radius: 6px;
      height: 240px;
      overflow-y: auto;
      padding: 12px;
      font-family: monospace;
      font-size: 12px;
    }
    .log-item {
      padding: 4px 0;
      border-bottom: 1px solid #0f172a;
      display: flex;
      gap: 10px;
    }
    .log-time { color: #64748b; }
    .log-kind { color: #f59e0b; font-weight: bold; }
    .log-detail { color: #cbd5e1; }
    .btn {
      background: #0284c7;
      color: white;
      border: none;
      padding: 8px 16px;
      border-radius: 4px;
      font-weight: 600;
      cursor: pointer;
      margin-top: 12px;
    }
    .btn:hover { background: #0369a1; }
  </style>
</head>
<body>
  <div class=""card"">
    <h1>🛸 Star Citizen Live Telemetry Widget</h1>
    <p style=""color: #94a3b8; font-size: 13px;"">
      This is a working demonstration of an SCLogMate Web Plugin hosted via the local loopback server.
    </p>

    <div class=""grid"">
      <div class=""kpi"">
        <div class=""kpi-title"">Pilot</div>
        <div id=""pilot-name"" class=""kpi-value"">—</div>
      </div>
      <div class=""kpi"">
        <div class=""kpi-title"">Current Location</div>
        <div id=""pilot-location"" class=""kpi-value"">—</div>
      </div>
      <div class=""kpi"">
        <div class=""kpi-title"">Active Ship</div>
        <div id=""ship-name"" class=""kpi-value"">—</div>
      </div>
      <div class=""kpi"">
        <div class=""kpi-title"">Wallet Balance</div>
        <div id=""wallet-balance"" class=""kpi-value"">0 aUEC</div>
      </div>
    </div>

    <button class=""btn"" onclick=""testNotification()"">Trigger Desktop Toast Notification</button>
  </div>

  <div class=""card"">
    <h2 style=""font-size: 16px; color: #38bdf8; margin-top: 0;"">📜 Real-time Game Log Stream</h2>
    <div id=""logs"" class=""log-feed"">
      <div style=""color: #64748b;"">Listening for real-time Star Citizen events...</div>
    </div>
  </div>

  <script>
    // 1. Listen for Telemetry updates
    window.scLogMate.on('HUD_TELEMETRY', (telemetry) => {
      if (!telemetry) return;
      document.getElementById('pilot-name').innerText = telemetry.pilotName || '—';
      document.getElementById('pilot-location').innerText = telemetry.locationName || '—';
      document.getElementById('ship-name').innerText = telemetry.shipName || '—';
      document.getElementById('wallet-balance').innerText = (telemetry.balance || 0).toLocaleString() + ' aUEC';
    });

    // 2. Listen for parsed Log Events
    const logFeed = document.getElementById('logs');
    window.scLogMate.on('LOG_EVENT', (event) => {
      if (!event) return;
      const row = document.createElement('div');
      row.className = 'log-item';
      row.innerHTML = `<span class=""log-time"">${new Date(event.time).toLocaleTimeString()}</span>` +
                      `<span class=""log-kind"">[${event.kind}]</span>` +
                      `<span class=""log-detail"">${event.detail || ''}</span>`;
      logFeed.prepend(row);
      if (logFeed.children.length > 50) logFeed.removeChild(logFeed.lastChild);
    });

    // 3. Initial load of telemetry
    window.scLogMate.getTelemetry().then(t => {
      if (t) {
        document.getElementById('pilot-name').innerText = t.pilotName || '—';
        document.getElementById('pilot-location').innerText = t.locationName || '—';
        document.getElementById('ship-name').innerText = t.shipName || '—';
        document.getElementById('wallet-balance').innerText = (t.balance || 0).toLocaleString() + ' aUEC';
      }
    });

    function testNotification() {
      window.scLogMate.notify('Plugin Alert', 'Hello from the sample telemetry widget!');
    }
  </script>
</body>
</html>";
            File.WriteAllText(Path.Combine(sampleDir, "index.html"), htmlContent);
            Logger.Log($"[PluginManager] Pre-installed sample plugin to {sampleDir}");
        }
        catch (Exception ex)
        {
            Logger.Error("[PluginManager] Failed to create sample plugin", ex);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var (_, native) in _nativePlugins)
            {
                try
                {
                    native.Plugin.Shutdown();
                    native.Context.Dispose();
                }
                catch { }
            }
            _nativePlugins.Clear();
            _httpServer.Dispose();
        }
    }

    private sealed class NativePluginInstance
    {
        public ISCPlugin Plugin { get; }
        public PluginAssemblyLoadContext Context { get; }

        public NativePluginInstance(ISCPlugin plugin, PluginAssemblyLoadContext context)
        {
            Plugin = plugin;
            Context = context;
        }
    }

    private sealed class PluginContext : IPluginContext
    {
        public PluginManifest Manifest { get; }
        public string PluginDirectory => Manifest.PluginDirectory;
        private readonly PluginManager _manager;

        public PluginContext(PluginManifest manifest, PluginManager manager)
        {
            Manifest = manifest;
            _manager = manager;
        }

        public void SubscribeLogEvents(Action<LogEntry> callback)
        {
            _manager.AddLogSubscriber(callback);
        }

        public void RegisterRpcHandler(string action, Func<JsonElement, object?> handler)
        {
            _manager.RegisterRpcHandler($"{Manifest.Id}:{action}", handler);
        }

        public void BroadcastToUi(string eventName, object? payload)
        {
            // Forward via Photino bridge
            Photino.PhotinoBridge.Current?.Broadcast($"PLUGIN_EVENT:{Manifest.Id}:{eventName}", payload);
        }

        public void LogInfo(string message)
        {
            Logger.Log($"[{Manifest.Name}] {message}");
        }

        public void LogError(string message, Exception? ex = null)
        {
            if (ex != null)
                Logger.Error($"[{Manifest.Name}] {message}", ex);
            else
                Logger.Log($"[{Manifest.Name}] ERROR: {message}");
        }
    }

    /// <summary>
    /// Custom collectible AssemblyLoadContext to allow isolated loading of native plugin assemblies.
    /// </summary>
    private sealed class PluginAssemblyLoadContext : AssemblyLoadContext, IDisposable
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginAssemblyLoadContext(string pluginPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }
            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            Unload();
        }
    }
}
