using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SCLogMate.Core;

/// <summary>
/// Protokolliert unbekannte Logevents, Schiffe, Benachrichtigungen und unaufgelöste Einträge
/// in %APPDATA%\SCLogMate\SCLogMate.unknown.log zur Diagnose und kontinuierlichen Parser-Verbesserung.
/// </summary>
public static class UnknownEventsLogger
{
    private static readonly object Lock = new();
    public static string Path { get; }

    // Deduplizierte Zähler für die Zusammenfassung: Category -> (Key -> Count)
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, int>> Categories = new(StringComparer.OrdinalIgnoreCase);

    static UnknownEventsLogger()
    {
        string dir;
        try
        {
            dir = Settings.Dir;
            Directory.CreateDirectory(dir);
        }
        catch
        {
            try { dir = System.IO.Path.GetDirectoryName(Environment.ProcessPath ?? AppContext.BaseDirectory) ?? "."; }
            catch { dir = "."; }
        }
        Path = System.IO.Path.Combine(dir, "SCLogMate.unknown.log");
    }

    /// <summary>
    /// Registriert ein unbekanntes Event in der angegebenen Kategorie.
    /// Schreibt es beim ersten Auftreten und periodisch in die Log-Datei.
    /// </summary>
    public static void LogUnknown(string category, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var clean = text.Trim();

        var catDict = Categories.GetOrAdd(category, _ => new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase));
        int count = catDict.AddOrUpdate(clean, 1, (_, c) => c + 1);

        // Bei erstmaligem Auftreten oder alle 50 Wiederholungen direkt in die Datei loggen
        if (count == 1 || count % 50 == 0)
        {
            try
            {
                lock (Lock)
                {
                    File.AppendAllText(Path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{category}] ({count}x) {clean}{Environment.NewLine}");
                }
            }
            catch { /* nicht schreibbar -> ignorieren */ }
        }
    }

    /// <summary>
    /// Schreibt eine strukturierte Gesamt-Übersicht aller erfassten unbekannten Events nach Häufigkeit.
    /// </summary>
    public static void FlushSummary()
    {
        if (Categories.IsEmpty || Categories.All(c => c.Value.IsEmpty)) return;

        try
        {
            lock (Lock)
            {
                using var sw = File.AppendText(Path);
                sw.WriteLine();
                sw.WriteLine($"=== SCLogMate Unbekannte Events Zusammenfassung · {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                foreach (var (cat, items) in Categories.OrderBy(c => c.Key))
                {
                    sw.WriteLine($"[{cat.ToUpperInvariant()}] ({items.Count} Typen, {items.Values.Sum()} Vorkommnisse)");
                    foreach (var (text, count) in items.OrderByDescending(kv => kv.Value).Take(50))
                    {
                        sw.WriteLine($"  {count,5}x  {text}");
                    }
                    sw.WriteLine();
                }
                sw.WriteLine(new string('-', 60));
            }
        }
        catch { /* ignore */ }
    }
}
