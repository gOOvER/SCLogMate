using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SCLogMate.Core.Plugins;

/// <summary>
/// Lightweight local loopback HTTP server (127.0.0.1) dedicated to hosting plugin web assets
/// and serving the client SDK (sclogmate.js). Eliminates file:// URI cross-origin restrictions in WebView2
/// and allows seamless integration with OBS Studio Browser Sources.
/// </summary>
public sealed class PluginHttpServer : IDisposable
{
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;
    private readonly string _pluginsRootDir;

    public int Port { get; private set; }
    public bool IsRunning => _listener?.IsListening ?? false;

    private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html; charset=utf-8",
        [".htm"] = "text/html; charset=utf-8",
        [".js"] = "application/javascript; charset=utf-8",
        [".mjs"] = "application/javascript; charset=utf-8",
        [".css"] = "text/css; charset=utf-8",
        [".json"] = "application/json; charset=utf-8",
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".svg"] = "image/svg+xml",
        [".ico"] = "image/x-icon",
        [".webp"] = "image/webp",
        [".woff"] = "font/woff",
        [".woff2"] = "font/woff2",
        [".ttf"] = "font/ttf",
        [".mp3"] = "audio/mpeg",
        [".wav"] = "audio/wav",
        [".ogg"] = "audio/ogg",
    };

    public PluginHttpServer(string pluginsRootDir)
    {
        _pluginsRootDir = pluginsRootDir;
    }

    /// <summary>
    /// Starts the loopback HTTP listener on an available local port.
    /// </summary>
    public void Start(int preferredPort = 38491)
    {
        if (IsRunning) return;

        int port = FindAvailablePort(preferredPort);
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");

        try
        {
            _listener.Start();
            Port = port;
            _cts = new CancellationTokenSource();
            _listenTask = Task.Run(() => ListenLoopAsync(_cts.Token));
            Logger.Log($"[PluginHttpServer] Running on http://127.0.0.1:{Port}/");
        }
        catch (Exception ex)
        {
            Logger.Error($"[PluginHttpServer] Failed to start on port {port}", ex);
            _listener.Close();
            _listener = null;
        }
    }

    private static int FindAvailablePort(int startingPort)
    {
        for (int p = startingPort; p < startingPort + 100; p++)
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Loopback, p));
                return p;
            }
            catch
            {
                // Port in use, try next
            }
        }
        // Fallback to OS assigned port
        using var fallbackSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        fallbackSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)fallbackSocket.LocalEndPoint!).Port;
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => ProcessRequestAsync(context), ct);
            }
            catch (HttpListenerException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested)
                {
                    Logger.Error("[PluginHttpServer] Error accepting HTTP connection", ex);
                }
            }
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        // Apply CORS headers for loopback development & iframes
        response.AddHeader("Access-Control-Allow-Origin", "*");
        response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

        if (request.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            response.StatusCode = 204;
            response.Close();
            return;
        }

        string rawUrl = request.Url?.AbsolutePath ?? "/";

        try
        {
            // 1. Serve embedded Client SDK: /sclogmate.js
            if (rawUrl.Equals("/sclogmate.js", StringComparison.OrdinalIgnoreCase))
            {
                byte[] sdkBytes = Encoding.UTF8.GetBytes(GetClientSdkScript());
                response.ContentType = "application/javascript; charset=utf-8";
                response.ContentLength64 = sdkBytes.Length;
                await response.OutputStream.WriteAsync(sdkBytes);
                response.Close();
                return;
            }

            // 2. Serve static plugin files: /plugins/{pluginId}/{relativePath...}
            if (rawUrl.StartsWith("/plugins/", StringComparison.OrdinalIgnoreCase))
            {
                var trimmed = rawUrl.Substring("/plugins/".Length).TrimStart('/');
                var parts = trimmed.Split('/', 2);
                var pluginId = parts[0];
                var subPath = parts.Length > 1 ? parts[1] : "index.html";
                if (string.IsNullOrWhiteSpace(subPath)) subPath = "index.html";

                var targetDir = Path.Combine(_pluginsRootDir, pluginId);
                var targetFile = Path.GetFullPath(Path.Combine(targetDir, subPath));

                // Path traversal protection
                if (!targetFile.StartsWith(Path.GetFullPath(targetDir), StringComparison.OrdinalIgnoreCase))
                {
                    response.StatusCode = 403;
                    response.Close();
                    return;
                }

                if (!File.Exists(targetFile))
                {
                    response.StatusCode = 404;
                    byte[] notFoundBytes = Encoding.UTF8.GetBytes($"404 Not Found: {subPath}");
                    await response.OutputStream.WriteAsync(notFoundBytes);
                    response.Close();
                    return;
                }

                var ext = Path.GetExtension(targetFile);
                response.ContentType = MimeTypes.TryGetValue(ext, out var mime) ? mime : "application/octet-stream";
                using var fs = File.OpenRead(targetFile);
                response.ContentLength64 = fs.Length;
                await fs.CopyToAsync(response.OutputStream);
                response.Close();
                return;
            }

            // Default fallback response
            response.StatusCode = 200;
            response.ContentType = "text/plain; charset=utf-8";
            byte[] okBytes = Encoding.UTF8.GetBytes("SCLogMate Plugin Server Active");
            await response.OutputStream.WriteAsync(okBytes);
            response.Close();
        }
        catch (Exception ex)
        {
            try
            {
                response.StatusCode = 500;
                response.Close();
            }
            catch { }
            Logger.Error($"[PluginHttpServer] Failed to handle request {rawUrl}", ex);
        }
    }

    /// <summary>
    /// Returns the bundled lightweight JavaScript SDK client script.
    /// This script is loaded by plugin pages via: &lt;script src="/sclogmate.js"&gt;&lt;/script&gt;
    /// </summary>
    private string GetClientSdkScript()
    {
        return @"
(function() {
  'use strict';
  if (window.scLogMate) return;

  const listeners = new Map();
  const pendingRequests = new Map();
  let requestIdCounter = 1;

  window.addEventListener('message', (event) => {
    const data = event.data;
    if (!data || typeof data !== 'object') return;

    // Handle incoming broadcast events (e.g. LOG_EVENT, HUD_TELEMETRY)
    if (data.type === 'SCLM_EVENT' && data.event) {
      const callbacks = listeners.get(data.event) || [];
      callbacks.forEach(cb => {
        try { cb(data.data); } catch (err) { console.error('[SCLogMate SDK] Event callback error:', err); }
      });
    }

    // Handle RPC response
    if (data.type === 'SCLM_RPC_RESPONSE' && data.requestId) {
      if (pendingRequests.has(data.requestId)) {
        const { resolve, reject } = pendingRequests.get(data.requestId);
        pendingRequests.delete(data.requestId);
        if (data.error) reject(new Error(data.error));
        else resolve(data.result);
      }
    }
  });

  window.scLogMate = {
    on: function(event, callback) {
      if (!listeners.has(event)) listeners.set(event, []);
      listeners.get(event).push(callback);
      return () => {
        const list = listeners.get(event) || [];
        const idx = list.indexOf(callback);
        if (idx !== -1) list.splice(idx, 1);
      };
    },

    call: function(action, payload) {
      return new Promise((resolve, reject) => {
        const reqId = 'req_' + (requestIdCounter++);
        pendingRequests.set(reqId, { resolve, reject });
        window.parent.postMessage({
          type: 'SCLM_RPC_REQUEST',
          requestId: reqId,
          action: action,
          payload: payload
        }, '*');

        // 10 second timeout guard
        setTimeout(() => {
          if (pendingRequests.has(reqId)) {
            pendingRequests.delete(reqId);
            reject(new Error('Request timeout: ' + action));
          }
        }, 10000);
      });
    },

    getTelemetry: function() {
      return this.call('get_telemetry');
    },

    getSessions: function() {
      return this.call('get_sessions');
    },

    getFleet: function() {
      return this.call('get_fleet');
    },

    notify: function(title, message) {
      return this.call('show_notification', { title, message });
    }
  };

  // Signal readiness to parent host
  window.parent.postMessage({ type: 'SCLM_PLUGIN_READY' }, '*');
})();
";
    }

    public void Dispose()
    {
        try
        {
            _cts?.Cancel();
            _listener?.Stop();
            _listener?.Close();
            _listener = null;
        }
        catch { }
    }
}
