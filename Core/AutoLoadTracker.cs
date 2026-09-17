using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SCLogMate.Core;

public sealed record CargoBoxGroup(decimal BoxSize, int UnitAmount);

public sealed record AutoLoadEntry(
    string Id,
    DateTime StartUtc,
    string Kind, // "Kauf" oder "Verkauf"
    string ShopName,
    string CommodityName,
    int TotalScu,
    IReadOnlyList<CargoBoxGroup> Boxes,
    int? PredictedSeconds
)
{
    public double ElapsedSeconds(DateTime now) => Math.Max(0, (now - StartUtc).TotalSeconds);
    public double RemainingSeconds(DateTime now) => PredictedSeconds.HasValue
        ? Math.Max(0, PredictedSeconds.Value - ElapsedSeconds(now))
        : 0;
    public bool IsCompleted(DateTime now) => RemainingSeconds(now) <= 0;
}

public sealed class AutoLoadTracker
{
    private static readonly Lazy<AutoLoadTracker> _instance = new(() => new AutoLoadTracker());
    public static AutoLoadTracker Instance => _instance.Value;

    private static readonly Dictionary<decimal, double> BoxSeconds = new()
    {
        [0.125m] = 0,
        [0.25m] = 0,
        [0.5m] = 0,
        [1m] = 1.2,
        [2m] = 1.8,
        [4m] = 3.0,
        [8m] = 4.8,
        [16m] = 7.2,
        [24m] = 9.0,
        [32m] = 10.8
    };

    public const double BaseLoadingSeconds = 72.0;
    public const double BaseUnloadingSeconds = 72.0;
    public static readonly TimeSpan FreshWindow = TimeSpan.FromSeconds(120);
    public static readonly TimeSpan AbandonAfter = TimeSpan.FromHours(2);

    private readonly object _lock = new();
    private readonly List<AutoLoadEntry> _entries = new();
    private readonly string _storagePath;

    public event Action? EntriesChanged;
    public event Action<AutoLoadEntry>? AutoLoadCompleted;

    public AutoLoadTracker()
    {
        _storagePath = Path.Combine(Settings.Dir, "autoload_active.json");
        RestoreActiveEntries();
    }

    public IReadOnlyList<AutoLoadEntry> GetActiveEntries()
    {
        lock (_lock)
        {
            ExpireStaleUnlocked();
            return _entries.ToList();
        }
    }

    public static int? PredictSeconds(string kind, IReadOnlyList<CargoBoxGroup> boxes)
    {
        if (boxes.Count == 0) return null;
        double total = kind.Equals("Kauf", StringComparison.OrdinalIgnoreCase) || kind.Equals("Buy", StringComparison.OrdinalIgnoreCase)
            ? BaseLoadingSeconds
            : BaseUnloadingSeconds;

        foreach (var b in boxes)
        {
            if (BoxSeconds.TryGetValue(b.BoxSize, out var per))
            {
                total += b.UnitAmount * per;
            }
            else
            {
                // Fallback für ungerade Größen: 1.2s pro SCU
                total += b.UnitAmount * (double)b.BoxSize * 1.2;
            }
        }
        return (int)Math.Round(total);
    }

    public static List<CargoBoxGroup> ParseCargoBoxData(string raw)
    {
        var list = new List<CargoBoxGroup>();
        var matches = Regex.Matches(raw, @"boxSize\[(?<size>[0-9.]+)\]\s*\|\s*unitAmount\[(?<count>\d+)\]");
        foreach (Match m in matches)
        {
            if (decimal.TryParse(m.Groups["size"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var s) &&
                int.TryParse(m.Groups["count"].Value, out var c))
            {
                list.Add(new CargoBoxGroup(s, c));
            }
        }
        return list;
    }

    public void RegisterAutoLoad(DateTime timestampUtc, string kind, string shopName, string commodityName, int totalScu, IReadOnlyList<CargoBoxGroup> boxes)
    {
        if (DateTime.UtcNow - timestampUtc > FreshWindow) return;

        var predicted = PredictSeconds(kind, boxes);
        var entry = new AutoLoadEntry(
            Id: Guid.NewGuid().ToString("N"),
            StartUtc: timestampUtc,
            Kind: kind,
            ShopName: shopName,
            CommodityName: commodityName,
            TotalScu: totalScu,
            Boxes: boxes,
            PredictedSeconds: predicted
        );

        lock (_lock)
        {
            // Deduplizierung: Selbe Startzeit, selbe Station und selbe Ware
            if (_entries.Any(e => Math.Abs((e.StartUtc - entry.StartUtc).TotalSeconds) < 2 &&
                                 e.ShopName == entry.ShopName &&
                                 e.CommodityName == entry.CommodityName))
            {
                return;
            }

            _entries.Add(entry);
            PersistUnlocked();
        }

        Logger.Log($"[AUTOLOAD] Start: {entry.TotalScu} SCU {entry.CommodityName} ({entry.Kind}) at {entry.ShopName}, Dauer: {entry.PredictedSeconds}s");
        EntriesChanged?.Invoke();
    }

    public void DiscardEntry(string id)
    {
        lock (_lock)
        {
            int removed = _entries.RemoveAll(e => e.Id == id);
            if (removed > 0)
            {
                PersistUnlocked();
            }
        }
        EntriesChanged?.Invoke();
    }

    public void CheckCompletion()
    {
        List<AutoLoadEntry> finished = new();
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var e = _entries[i];
                if (e.IsCompleted(now))
                {
                    finished.Add(e);
                    _entries.RemoveAt(i);
                }
            }
            if (finished.Count > 0)
            {
                PersistUnlocked();
            }
        }

        foreach (var item in finished)
        {
            Logger.Log($"[AUTOLOAD] Abgeschlossen: {item.TotalScu} SCU {item.CommodityName} an {item.ShopName}");
            AutoLoadCompleted?.Invoke(item);
        }

        if (finished.Count > 0)
        {
            EntriesChanged?.Invoke();
        }
    }

    private void ExpireStaleUnlocked()
    {
        var now = DateTime.UtcNow;
        _entries.RemoveAll(e => now - e.StartUtc >= AbandonAfter);
    }

    private void PersistUnlocked()
    {
        try
        {
            var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_storagePath, json);
        }
        catch (Exception ex)
        {
            Logger.Error("AutoLoadTracker.Persist", ex);
        }
    }

    private void RestoreActiveEntries()
    {
        try
        {
            if (!File.Exists(_storagePath)) return;
            var json = File.ReadAllText(_storagePath);
            var loaded = JsonSerializer.Deserialize<List<AutoLoadEntry>>(json);
            if (loaded != null)
            {
                var now = DateTime.UtcNow;
                lock (_lock)
                {
                    _entries.Clear();
                    foreach (var e in loaded)
                    {
                        if (now - e.StartUtc < AbandonAfter && !e.IsCompleted(now))
                        {
                            _entries.Add(e);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("AutoLoadTracker.Restore", ex);
        }
    }
}
