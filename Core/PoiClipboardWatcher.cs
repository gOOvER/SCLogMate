using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using SCLogMate.Models;

namespace SCLogMate.Core;

public sealed record CopiedLocationReading(
    double X,
    double Y,
    double Z,
    DateTime Timestamp,
    string RawText,
    string DetectedSystem,
    List<PoiDistanceInfo> NearestPois
);

public sealed record PoiDistanceInfo(
    int Id,
    string Name,
    string Category,
    string Body,
    double DistanceMeters,
    string FormattedDistance
);

public static partial class PoiClipboardWatcher
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(IntPtr hMem);

    private const uint CF_UNICODETEXT = 13;

    private static Timer? _pollTimer;
    private static string? _lastClipboardText;
    private static bool _isEnabled = true;
    private static readonly Lock _lock = new();

    public static CopiedLocationReading? LastReading { get; private set; }
    public static event Action<CopiedLocationReading>? OnLocationDetected;

    public static bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            if (_isEnabled) Start();
            else Stop();
        }
    }

    public static void Start()
    {
        lock (_lock)
        {
            if (_pollTimer != null) return;
            _pollTimer = new Timer(CheckClipboard, null, TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(1500));
            Logger.Log("PoiClipboardWatcher: Auto-Clipboard POI-Tracker gestartet.");
        }
    }

    public static void Stop()
    {
        lock (_lock)
        {
            _pollTimer?.Dispose();
            _pollTimer = null;
        }
    }

    public static CopiedLocationReading? CheckNow()
    {
        var text = ReadClipboardText();
        if (string.IsNullOrWhiteSpace(text)) return LastReading;
        ProcessText(text);
        return LastReading;
    }

    private static void CheckClipboard(object? state)
    {
        if (!_isEnabled) return;
        try
        {
            var text = ReadClipboardText();
            if (string.IsNullOrWhiteSpace(text) || string.Equals(text, _lastClipboardText, StringComparison.Ordinal))
                return;

            _lastClipboardText = text;
            ProcessText(text);
        }
        catch (Exception ex)
        {
            Logger.Error("PoiClipboardWatcher CheckClipboard", ex);
        }
    }

    private static void ProcessText(string text)
    {
        var match = ShowLocationRegex().Match(text);
        if (!match.Success) return;

        if (!double.TryParse(match.Groups["x"].ValueSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
            !double.TryParse(match.Groups["y"].ValueSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
            !double.TryParse(match.Groups["z"].ValueSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out var z))
        {
            return;
        }

        var system = DetectCurrentSystem();
        var pois = Database.GetUserPois(system);
        var nearest = new List<PoiDistanceInfo>();

        foreach (var poi in pois)
        {
            if (poi.PosX.HasValue && poi.PosY.HasValue && poi.PosZ.HasValue)
            {
                var dx = x - poi.PosX.Value;
                var dy = y - poi.PosY.Value;
                var dz = z - poi.PosZ.Value;
                var distM = Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
                nearest.Add(new PoiDistanceInfo(
                    poi.Id,
                    poi.Name,
                    poi.Category,
                    poi.Body,
                    distM,
                    FormatDistance(distM)
                ));
            }
        }

        nearest = nearest.OrderBy(p => p.DistanceMeters).Take(5).ToList();

        var reading = new CopiedLocationReading(x, y, z, DateTime.UtcNow, text.Trim(), system, nearest);
        LastReading = reading;
        Logger.Log($"PoiClipboardWatcher: Neue Koordinaten aus Zwischenablage erfasst: X={x:F1}, Y={y:F1}, Z={z:F1} ({system})");

        OnLocationDetected?.Invoke(reading);
    }

    public static string FormatDistance(double meters)
    {
        if (meters < 1000) return $"{meters:F0} m";
        if (meters < 1_000_000) return $"{meters / 1000:F1} km";
        if (meters < 1_000_000_000) return $"{meters / 1_000_000:F1} Mm";
        return $"{meters / 1_000_000_000:F2} GM";
    }

    private static string DetectCurrentSystem()
    {
        // Aus LogParser oder Starmap
        try
        {
            var lastLoc = Database.LoadRecentEvents(50)
                .Where(e => e.Kind == EventKind.Location || e.Kind == EventKind.Quantum)
                .OrderByDescending(e => e.Time)
                .FirstOrDefault();

            if (lastLoc != null)
            {
                var resolved = Locations.ResolveLocation(lastLoc.Detail);
                if (!string.IsNullOrEmpty(resolved.SystemName) && resolved.SystemName != "—")
                {
                    return resolved.SystemName;
                }
            }
        }
        catch { }

        return "Stanton";
    }

    private static string? ReadClipboardText()
    {
        for (int i = 0; i < 3; i++)
        {
            if (OpenClipboard(IntPtr.Zero))
            {
                try
                {
                    var handle = GetClipboardData(CF_UNICODETEXT);
                    if (handle == IntPtr.Zero) return null;

                    var pointer = GlobalLock(handle);
                    if (pointer == IntPtr.Zero) return null;

                    try
                    {
                        return Marshal.PtrToStringUni(pointer);
                    }
                    finally
                    {
                        GlobalUnlock(handle);
                    }
                }
                finally
                {
                    CloseClipboard();
                }
            }
            Thread.Sleep(50);
        }
        return null;
    }

    [GeneratedRegex(
        @"x:\s*(?<x>-?\d+(?:\.\d+)?)\s*[,;]?\s*" +
        @"y:\s*(?<y>-?\d+(?:\.\d+)?)\s*[,;]?\s*" +
        @"z:\s*(?<z>-?\d+(?:\.\d+)?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ShowLocationRegex();
}
