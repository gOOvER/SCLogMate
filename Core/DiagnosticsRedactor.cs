using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SCLogMate.Core;

/// <summary>
/// Schwärzt und maskiert sensible Daten (lokale Windows-Pfade, Benutzernamen, Discord-Webhooks,
/// API-Keys, Auth-Tokens) vor dem Exportieren von Fehlerberichten und Support-Texten.
/// </summary>
public static partial class DiagnosticsRedactor
{
    [GeneratedRegex(@"[a-zA-Z]:\\Users\\[^\s\\/]+(?:\\[^\s""'\(\)]+)?", RegexOptions.IgnoreCase)]
    private static partial Regex WindowsUserPathRegex();

    [GeneratedRegex(@"https:\/\/discord(?:app)?\.com\/api\/webhooks\/[0-9]+\/[A-Za-z0-9_\-]+", RegexOptions.IgnoreCase)]
    private static partial Regex DiscordWebhookRegex();

    [GeneratedRegex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b")]
    private static partial Regex IpAddressRegex();

    [GeneratedRegex(@"(?:bearer|token|apikey|api_key|password|auth|secret)\s*[:=]\s*[""']?[A-Za-z0-9_\-\.]{8,}[""']?", RegexOptions.IgnoreCase)]
    private static partial Regex AuthTokenRegex();

    /// <summary>
    /// Bereinigt einen gegebenen Freitext oder Logauszug von privaten Pfaden und Kennungen.
    /// </summary>
    public static string Sanitize(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        var sanitized = text;

        // 1. Spezifische bekannte System-Verzeichnisse maskieren
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            sanitized = sanitized.Replace(userProfile, "<user_home>", StringComparison.OrdinalIgnoreCase);
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (!string.IsNullOrWhiteSpace(appData))
        {
            sanitized = sanitized.Replace(appData, "<appdata>", StringComparison.OrdinalIgnoreCase);
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            sanitized = sanitized.Replace(localAppData, "<localappdata>", StringComparison.OrdinalIgnoreCase);
        }

        // 2. Allgemeine Windows-Benutzerpfade maskieren (C:\Users\<Name>\...)
        sanitized = WindowsUserPathRegex().Replace(sanitized, "<path>");

        // 3. Discord Webhook URLs maskieren
        sanitized = DiscordWebhookRegex().Replace(sanitized, "https://discord.com/api/webhooks/<redacted_webhook>");

        // 4. IP-Adressen maskieren (außer Localhost 127.0.0.1)
        sanitized = IpAddressRegex().Replace(sanitized, m =>
        {
            if (m.Value == "127.0.0.1" || m.Value == "0.0.0.0") return m.Value;
            return "<ip_redacted>";
        });

        // 5. Auth-Tokens & Passwörter
        sanitized = AuthTokenRegex().Replace(sanitized, "auth=<redacted>");

        return sanitized;
    }

    /// <summary>
    /// Erstellt eine kompakte, datenschutzbereinigte System- und Diagnose-Zusammenfassung
    /// für Discord-Support und GitHub-Issues.
    /// </summary>
    public static string BuildSanitizedDiagnosticSummary(
        string appVersion,
        string dbSchemaVersion,
        string parserVersion,
        int sessionCount,
        int totalEvents,
        string? activeGameVersion,
        string? activeShard)
    {
        var osVersion = Environment.OSVersion.VersionString;
        var is64Bit = Environment.Is64BitOperatingSystem ? "64-Bit" : "32-Bit";
        var ramGb = Math.Round(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024.0 / 1024.0 / 1024.0, 1);
        var dateUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

        return $@"=== SCLogMate System- & Diagnose-Zusammenfassung ===
Zeitpunkt: {dateUtc}
SCLogMate Version: {appVersion} (Photino / React)
Betriebssystem: {osVersion} ({is64Bit})
Arbeitsspeicher: ca. {ramGb} GB RAM verfügbar
Datenbank Schema: v{dbSchemaVersion} | Parser Engine: v{parserVersion}
Erfasste Sessions: {sessionCount} | Gesamte Events: {totalEvents:N0}
Star Citizen Version: {Sanitize(activeGameVersion ?? "Unbekannt")}
Aktiver Shard: {Sanitize(activeShard ?? "—")}
Datenpfad: <appdata>\SCLogMate\sessions.db
Status: Operational ✓
====================================================";
    }

    /// <summary>
    /// Erstellt einen 1-Klick Sanitized Bug-Report (~1-3 KB) ohne Spieler-Handle oder private Pfade
    /// inklusive Systemumgebung, Datenbank-Status, Shard-Kontext und bereinigten letzten Log-Zeilen/Fehlern.
    /// </summary>
    public static string BuildSanitizedBugReport(
        string appVersion,
        string dbSchemaVersion,
        string parserVersion,
        int sessionCount,
        int totalEvents,
        string? activeGameVersion,
        string? activeShard,
        string? lastEndReason,
        System.Collections.Generic.IEnumerable<string>? recentErrorOrCrashLines = null)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== SCLogMate Sanitized Bug-Report ===");
        sb.AppendLine($"Zeitpunkt (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"SCLogMate: {appVersion} (.NET 10 / Photino / React 19)");
        sb.AppendLine($"OS: {Environment.OSVersion.VersionString} ({(Environment.Is64BitOperatingSystem ? "64-Bit" : "32-Bit")})");
        sb.AppendLine($"RAM: {Math.Round(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 1024.0 / 1024.0 / 1024.0, 1)} GB verfügbar");
        sb.AppendLine($"DB-Schema: v{dbSchemaVersion} | Parser Engine: v{parserVersion}");
        sb.AppendLine($"Sessions: {sessionCount} | Total Events: {totalEvents:N0}");
        sb.AppendLine($"Star Citizen: {Sanitize(activeGameVersion ?? "Unbekannt")}");
        sb.AppendLine($"Shard: {Sanitize(activeShard ?? "—")} | Status/Disconnect: {Sanitize(lastEndReason ?? "Normal")}");
        sb.AppendLine("----------------------------------------");
        sb.AppendLine("Recent Diagnostics / Error Trace (Sanitized):");

        var hasLines = false;
        if (recentErrorOrCrashLines != null)
        {
            foreach (var line in System.Linq.Enumerable.TakeLast(recentErrorOrCrashLines, 30))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                sb.AppendLine(Sanitize(line));
                hasLines = true;
            }
        }

        if (!hasLines)
        {
            sb.AppendLine("Keine kritischen Fehler oder Abstürze protokolliert (Clean Log State).");
        }

        sb.AppendLine("========================================");
        return sb.ToString();
    }
}
