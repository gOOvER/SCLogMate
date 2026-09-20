using System;
using System.Text.Json;
using SCLogMate.Models;

namespace SCLogMate.Core.Plugins;

/// <summary>
/// Runtime context passed to native C# plugins during initialization.
/// Provides safe access to game events, telemetry, RPC routing, and logging.
/// </summary>
public interface IPluginContext
{
    /// <summary>
    /// Metadata loaded from the plugin's manifest.json.
    /// </summary>
    PluginManifest Manifest { get; }

    /// <summary>
    /// Absolute path to the plugin's root directory on disk.
    /// </summary>
    string PluginDirectory { get; }

    /// <summary>
    /// Subscribes to live parsed Star Citizen game log events.
    /// </summary>
    void SubscribeLogEvents(Action<LogEntry> callback);

    /// <summary>
    /// Registers a custom RPC action handler that the plugin's Web UI can call via scLogMate.call(action, payload).
    /// </summary>
    void RegisterRpcHandler(string action, Func<JsonElement, object?> handler);

    /// <summary>
    /// Broadcasts a custom event to the plugin's Web UI iframe or all connected clients.
    /// </summary>
    void BroadcastToUi(string eventName, object? payload);

    /// <summary>
    /// Logs an informational message tagged with the plugin identifier.
    /// </summary>
    void LogInfo(string message);

    /// <summary>
    /// Logs an error message or exception tagged with the plugin identifier.
    /// </summary>
    void LogError(string message, Exception? ex = null);
}

/// <summary>
/// Entrypoint interface for native C# plugins.
/// </summary>
public interface ISCPlugin
{
    /// <summary>
    /// Called when the plugin is loaded and initialized by SCLogMate.
    /// </summary>
    void Initialize(IPluginContext context);

    /// <summary>
    /// Called when the plugin is being unloaded or when SCLogMate is shutting down.
    /// Clean up timers, background threads, and external handles here.
    /// </summary>
    void Shutdown();
}
