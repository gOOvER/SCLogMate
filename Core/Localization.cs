using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SCLogReader.Core;

/// <summary>
/// Übersetzt deutsche Spieltexte (Missions-/Bauplan-Titel aus der Log) ins Englische,
/// damit englische DBs wie CitizenHQ treffen. Quelle: die lokalen global.ini der
/// deutschen und englischen Lokalisierung (DE-Wert → Key → EN-Wert).
/// Wird lazy beim ersten Aufruf geladen; fehlen die Dateien, bleibt der Text unverändert.
/// </summary>
public static class Localization
{
    static Dictionary<string, string>? _deToEn;
    static bool _tried;
    static Dictionary<string, string>? _items;
    static bool _itemsTried;
    static string? _hint;   // gemerkter Log-Pfad → Spiel-Wurzel (für Aufrufe ohne Pfad, z.B. im Parser)

    /// <summary>Log-Pfad merken, damit Parser Item-Namen ohne Pfad-Argument auflösen kann.</summary>
    public static void Hint(string? logPath)
    {
        if (string.IsNullOrEmpty(_hint) && !string.IsNullOrEmpty(logPath)) _hint = logPath;
    }

    /// <summary>Deutschen Titel ins Englische übersetzen; unbekannt/englisch → unverändert zurück.</summary>
    public static string ToEnglish(string text, string? logPath)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;
        Hint(logPath);
        var map = Load(logPath ?? _hint);
        return map != null && map.TryGetValue(text.Trim(), out var en) && !string.IsNullOrWhiteSpace(en)
            ? en : text;
    }

    /// <summary>Roh-Klassencode (z.B. rrs_specialist_light_helmet_01_01_iae2022) → echter Item-Name
    /// (z.B. „Arden-SL Helmet") aus der global.ini. Null, wenn nicht gefunden.</summary>
    public static string? ItemName(string cls)
    {
        if (string.IsNullOrWhiteSpace(cls)) return null;
        var map = LoadItems(_hint);
        return map != null && map.TryGetValue(ItemBase(cls), out var n) ? n : null;
    }

    // Varianten-/Versions-Segmente am Ende wegstrippen (alles mit Ziffer: _01, _iae2022, _tint01 …),
    // damit Log-Variante und global.ini-Variante auf dieselbe Basis matchen.
    static string ItemBase(string cls)
    {
        var segs = new List<string>(cls.ToLowerInvariant().Split('_', StringSplitOptions.RemoveEmptyEntries));
        while (segs.Count > 2 && segs[^1].Any(char.IsDigit)) segs.RemoveAt(segs.Count - 1);
        return string.Join('_', segs);
    }

    static Dictionary<string, string>? LoadItems(string? logPath)
    {
        if (_itemsTried) return _items;
        _itemsTried = true;
        try
        {
            var root = FindGameRoot(logPath);
            if (root == null) return null;
            var enFile = Path.Combine(root, "data", "Localization", "english", "global.ini");
            if (!File.Exists(enFile)) return null;

            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, val) in ReadIniPairs(enFile))
            {
                if (!key.StartsWith("item_Name", StringComparison.OrdinalIgnoreCase)) continue;
                var cls = key.Substring("item_Name".Length).TrimStart('_');
                if (cls.Length < 3 || string.IsNullOrWhiteSpace(val)) continue;
                var b = ItemBase(cls);
                if (!d.ContainsKey(b)) d[b] = val.Trim();     // erste Variante gewinnt
            }
            _items = d;
            Logger.Log($"Localization: {d.Count} Item-Namen geladen.");
        }
        catch (Exception ex) { Logger.Error("Localization items", ex); }
        return _items;
    }

    static Dictionary<string, string>? Load(string? logPath)
    {
        if (_tried) return _deToEn;
        _tried = true;
        try
        {
            var root = FindGameRoot(logPath);
            if (root == null) return null;

            string[] possibleDePaths = new[]
            {
                Path.Combine(root, "data", "Localization", "german_(germany)", "global.ini"),
                Path.Combine(root, "data", "Localization", "german", "global.ini"),
                Path.Combine(root, "data", "Localization", "deutsch", "global.ini"),
                Path.Combine(root, "data", "Localization", "de_DE", "global.ini"),
                Path.Combine(root, "data", "Localization", "de", "global.ini")
            };

            var deFile = possibleDePaths.FirstOrDefault(File.Exists);
            var enFile = Path.Combine(root, "data", "Localization", "english", "global.ini");
            if (deFile == null || !File.Exists(enFile)) return null;

            var enByKey = ReadIni(enFile);                              // Key → englischer Wert
            var deToEn = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, deVal) in ReadIniPairs(deFile))          // DE-Wert → EN-Wert
                if (!deToEn.ContainsKey(deVal) && enByKey.TryGetValue(key, out var enVal) && enVal != deVal)
                    deToEn[deVal] = enVal;
            _deToEn = deToEn;
            Logger.Log($"Localization: {deToEn.Count} DE→EN Einträge aus '{Path.GetFileName(Path.GetDirectoryName(deFile))}' geladen.");
        }
        catch (Exception ex) { Logger.Error("Localization", ex); }
        return _deToEn;
    }

    /// <summary>Spiel-Wurzel (enthält data/Localization) ausgehend vom Log-Pfad finden.</summary>
    static string? FindGameRoot(string? logPath)
    {
        var dir = string.IsNullOrEmpty(logPath) ? null : Path.GetDirectoryName(logPath);
        for (int i = 0; i < 3 && dir != null; i++)   // Game.log-Ordner, evtl. über logbackups hinaus
        {
            if (Directory.Exists(Path.Combine(dir, "data", "Localization"))) return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    static Dictionary<string, string> ReadIni(string file)
    {
        var d = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (k, v) in ReadIniPairs(file)) d[k] = v;
        return d;
    }

    static IEnumerable<(string key, string value)> ReadIniPairs(string file)
    {
        foreach (var raw in File.ReadLines(file))
        {
            var line = raw;
            int eq = line.IndexOf('=');
            if (eq <= 0) continue;
            var key = line[..eq];
            var val = line[(eq + 1)..].TrimEnd('\r', '\n');
            int comma = key.IndexOf(',');                 // "key,P" → "key" (Plural-/Param-Suffix)
            if (comma > 0) key = key[..comma];
            yield return (key, val);
        }
    }
}
