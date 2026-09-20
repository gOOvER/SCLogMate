using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;

namespace SCLogMate.Core;

/// <summary>
/// Resolves accurate Star Citizen patch versions (e.g. 4.10.1 instead of 4.10.0)
/// by inspecting RSI Launcher logs, Windows PE FileVersion headers, and known CIG build mappings.
/// CIG often maintains the base branch name (e.g. 'sc-alpha-4.10.0') across point releases,
/// but records the true release version (e.g. '4.10.1') in the RSI launcher update logs and FileVersion.
/// </summary>
public static partial class GameVersionResolver
{
    private static readonly ConcurrentDictionary<string, string> BuildToVersionCache = new(StringComparer.OrdinalIgnoreCase)
    {
        // Known authoritative mappings for point releases where Git branch was not renamed by CIG
        ["12660092"] = "4.10.1",
        ["12122953"] = "4.8.3",
        ["12061511"] = "4.8.2",
        ["12030094"] = "4.8.2",
        ["11952564"] = "4.8.1",
        ["11875683"] = "4.8.1",
        ["11715810"] = "4.7.2",
        ["11674325"] = "4.7.2",
        ["11638371"] = "4.7.1",
        ["11617053"] = "4.7.1",
        ["11592622"] = "4.7.1",
        ["10591185"] = "4.3.2",
        ["10487514"] = "4.3.2",
        ["10452200"] = "4.3.2",
    };

    private static bool _launcherScanned;
    private static readonly object _scanLock = new();

    [GeneratedRegex(@"Star Citizen (?:LIVE|PTU|EPTU|HOTFIX) (?<ver>[0-9]+\.[0-9]+(?:\.[0-9]+)?)-[a-z]+\.(?<build>[0-9]+)", RegexOptions.IgnoreCase)]
    private static partial Regex LauncherVersionRegex();

    [GeneratedRegex(@"^4\.10\.193\b")]
    private static partial Regex Sc4101FileVersionRegex();

    /// <summary>
    /// Attempts to resolve an accurate patch version from build number.
    /// </summary>
    public static bool TryResolveVersion(string? build, out string version)
    {
        version = "";
        if (string.IsNullOrWhiteSpace(build)) return false;

        var cleanBuild = build.Trim();

        // 1. Check in-memory cache / known mappings
        if (BuildToVersionCache.TryGetValue(cleanBuild, out var cached))
        {
            version = cached;
            return true;
        }

        // 2. Dynamically scan RSI Launcher logs on demand
        EnsureLauncherLogsScanned();

        if (BuildToVersionCache.TryGetValue(cleanBuild, out var found))
        {
            version = found;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to resolve or refine patch version from Windows PE FileVersion header.
    /// E.g. '4.10.193.11644' -> '4.10.1'.
    /// </summary>
    public static bool TryResolveFromWindowsVersion(string? fileVersion, out string version)
    {
        version = "";
        if (string.IsNullOrWhiteSpace(fileVersion)) return false;

        var fv = fileVersion.Trim();
        if (Sc4101FileVersionRegex().IsMatch(fv))
        {
            version = "4.10.1";
            return true;
        }

        return false;
    }

    /// <summary>
    /// Scans %APPDATA%\rsilauncher\logs\log.log and log.old.log to harvest
    /// all build-to-version mappings directly from the official launcher.
    /// </summary>
    public static void EnsureLauncherLogsScanned()
    {
        if (_launcherScanned) return;
        lock (_scanLock)
        {
            if (_launcherScanned) return;
            _launcherScanned = true;
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var logsDir = Path.Combine(appData, "rsilauncher", "logs");
                if (Directory.Exists(logsDir))
                {
                    foreach (var fn in new[] { "log.log", "log.old.log" })
                    {
                        var p = Path.Combine(logsDir, fn);
                        if (!File.Exists(p)) continue;

                        using var fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        using var reader = new StreamReader(fs, System.Text.Encoding.UTF8);
                        string? line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            var m = LauncherVersionRegex().Match(line);
                            if (m.Success)
                            {
                                var bld = m.Groups["build"].Value;
                                var ver = m.Groups["ver"].Value;
                                BuildToVersionCache[bld] = ver;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GameVersionResolver.EnsureLauncherLogsScanned", ex);
            }
        }
    }
}
