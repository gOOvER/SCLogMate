using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using SCLogMate.Core;
using SCLogMate.Models;

namespace SCLogMate;

internal static partial class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Deutsche Zahlenformatierung (Tausenderpunkte) für die Anzeige.
        var de = new System.Globalization.CultureInfo("de-DE");
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = de;
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = de;
        System.Threading.Thread.CurrentThread.CurrentCulture = de;

        // CLI-Modus:  SCLogMate.exe --scan <Datei-oder-Verzeichnis>
        if (args.Length >= 1 && args[0] == "--scan")
        {
            Scan(args.Length >= 2 ? args[1] : ".");
            return;
        }

        // Nur eine Instanz zulassen.
        System.Threading.Mutex? mutex = null;
        bool isNew = true;
        try
        {
            mutex = new System.Threading.Mutex(true, @"Global\SCLogMate_SingleInstance", out isNew);
        }
        catch (UnauthorizedAccessException)
        {
            try
            {
                mutex = new System.Threading.Mutex(true, @"Local\SCLogMate_SingleInstance", out isNew);
            }
            catch { isNew = true; }
        }
        catch
        {
            isNew = true;
        }

        using (mutex)
        {
            if (!isNew)
            {
                Core.Logger.Log("Zweite Instanz blockiert – läuft bereits.");
                return;
            }

            Core.Logger.Log($"Photino-GUI Start · {Environment.OSVersion}");
            try
            {
                Database.Init();
                RunPhotinoApp(args);
            }
            catch (Exception ex)
            {
                Core.Logger.Error("FATAL", ex);
                throw;
            }
        }
        return;
    }

    private static void RunPhotinoApp(string[] args)
    {
        var window = new Photino.NET.PhotinoWindow()
            .SetTitle("SCLogMate — Star Citizen Live Companion")
            .SetUseOsDefaultSize(false)
            .SetSize(1440, 900)
            .SetMinSize(1024, 700)
            .Center();

        try { Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory; } catch { }

        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SCLogMate.ico");
        if (File.Exists(iconPath))
        {
            try { window.SetIconFile(iconPath); } catch { }
        }

        var bridge = new Core.Photino.PhotinoBridge();
        bridge.Initialize(window);

        // Prüfen, ob Vite Dev-Server explizit per --dev angefordert wurde oder aktiv lauscht
        bool useDevServer = args.Contains("--dev");
        if (!useDevServer)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromMilliseconds(250) };
                var res = client.GetAsync("http://localhost:5173").GetAwaiter().GetResult();
                if (res.IsSuccessStatusCode)
                {
                    useDevServer = true;
                }
            }
            catch { }
        }

        if (useDevServer)
        {
            Core.Logger.Log("Photino: Lade Vite Dev-Server unter http://localhost:5173");
            window.Load(new Uri("http://localhost:5173"));
        }
        else
        {
            var targetHtmlPath = Core.Photino.EmbeddedAssets.EnsureIndexHtml();
            if (File.Exists(targetHtmlPath))
            {
                Core.Logger.Log($"Photino: Lade Frontend ({targetHtmlPath})");
                window.Load(targetHtmlPath);
            }
            else
            {
                Core.Logger.Log($"Photino: FEHLER - Frontend konnte nicht initialisiert werden: {targetHtmlPath}");
                var errorHtml = $@"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><title>SCLogMate - Frontend nicht gefunden</title>
<style>
body {{ font-family: system-ui, -apple-system, sans-serif; background: #0f172a; color: #f8fafc; padding: 40px; line-height: 1.6; }}
h1 {{ color: #ef4444; font-size: 22px; margin-bottom: 12px; }}
p {{ font-size: 14px; color: #cbd5e1; }}
pre {{ background: #1e293b; color: #38bdf8; padding: 16px; border-radius: 8px; border: 1px solid #334155; font-size: 12px; line-height: 1.5; white-space: pre-wrap; }}
</style>
</head>
<body>
<h1>Frontend-Dateien nicht gefunden</h1>
<p>Die Datei <code>{targetHtmlPath}</code> konnte nicht geladen werden.</p>
<p>Basisverzeichnis: <code>{AppDomain.CurrentDomain.BaseDirectory}</code></p>
</body>
</html>";
                window.LoadRawString(errorHtml);
            }
        }

        window.WaitForClose();
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();

    static void Scan(string target)
    {
        LogParser.Unknown.Clear();
        var files = Directory.Exists(target)
            ? Directory.GetFiles(target, "*.log", SearchOption.AllDirectories).OrderBy(f => f).ToArray()
            : new[] { target };

        var all = new System.Collections.Generic.List<Sess>();
        foreach (var file in files)
            all.Add(ScanFile(file));

        // chronologisch (älteste zuerst)
        foreach (var s in all.OrderBy(s => s.Start))
        {
            Console.WriteLine($"━━ Session {s.Start.ToLocalTime():dd.MM. HH:mm} → {s.End.ToLocalTime():HH:mm}  [{Dur(s.End - s.Start)}]  ({s.Name})");
            Console.WriteLine($"   Einnahmen {s.Income:N0}  ·  Ausgaben {s.Spend:N0}  ·  NETTO {s.Net:N0} aUEC");
            Console.WriteLine($"   Handel {s.Sales + s.Trade:N0}  (Item {s.Sales:N0} / Fracht {s.Trade:N0})  ·  Käufe {s.Spent:N0}");
            Console.WriteLine($"   Standort: {s.LastLoc ?? "—"}  ·  Schiffe: {(s.Ships.Count == 0 ? "—" : string.Join(", ", s.Ships))}");
            Console.WriteLine($"   Missionen {s.Missions} · Gebiete {s.Zones} · Party {s.PartyEv} · Med-Bett {s.Deaths} · Hangar {s.Hangars} · Ausrüstung {s.Loadout.Count}");
            Console.WriteLine($"   Verluste {s.Losses} · Kampfunfähig {s.Incaps} · Angebote {s.Offers} · Beschlagn. {s.Impounds} · Freunde {s.Friends} · Defekt {s.Defekt}");
            foreach (var mt in s.MissionTexts.Take(6)) Console.WriteLine($"   ◦ {mt}");
            if (s.Blueprints.Count > 0) Console.WriteLine($"   Baupläne: {string.Join(", ", s.Blueprints)}");
            if (s.Loot.Count > 0) Console.WriteLine($"   Loot ({s.Loot.Count}): {string.Join(", ", s.Loot)}");
        }

        var meta = all.OrderByDescending(s => s.Start).FirstOrDefault()?.Meta;
        if (meta is { Count: > 0 })
        {
            Console.WriteLine(new string('─', 60));
            Console.WriteLine("SYSTEM/CHARAKTER (neueste Session):");
            foreach (var kv in meta) Console.WriteLine($"   {kv.Key,-10}: {kv.Value}");
        }

        // Gesamt über alle Sessions
        Console.WriteLine(new string('─', 60));
        var total = TimeSpan.FromTicks(all.Sum(s => (s.End - s.Start).Ticks));
        Console.WriteLine($"GESAMT ({all.Count} Sessions · Spielzeit {Dur(total)})");
        Console.WriteLine($"   Aufträge abgeschlossen (Belohnung NICHT im Log): {all.Sum(s => s.MissionsDone)}");
        Console.WriteLine($"   Einnahmen {all.Sum(s => s.Income):N0}  ·  Ausgaben {all.Sum(s => s.Spend):N0}  ·  NETTO {all.Sum(s => s.Net):N0} aUEC");
        Console.WriteLine($"   Handel {all.Sum(s => s.Sales + s.Trade):N0}  ·  Käufe {all.Sum(s => s.Spent):N0}");
        Console.WriteLine("GRÖSSTE EINZEL-POSTEN:");
        foreach (var m in all.SelectMany(s => s.Money).OrderByDescending(m => System.Math.Abs(m.amt)).Take(8))
            Console.WriteLine($"   {m.amt,12:N0}  {m.label}");

        Console.WriteLine($"   ! unbekannte Notifications: {LogParser.Unknown.Values.Sum():N0} ({LogParser.Unknown.Count:N0} Typen)");
        Console.WriteLine($"   ! verworfene Transferköpfe: {all.Sum(session => session.ExpiredTransfers):N0}");
        foreach (var unknown in LogParser.Unknown.OrderByDescending(item => item.Value).Take(10))
            Console.WriteLine($"     {unknown.Value,5:N0}x  {unknown.Key}");
    }

    class Sess
    {
        public string Name = "";
        public DateTime Start, End;
        public long In, Out, Reward, Spent, Sales, Trade;
        public int Missions, Zones, PartyEv, Deaths, Hangars, Losses, Incaps, Offers, Impounds, Friends, Defekt, MissionsDone;
        public string? LastLoc;
        public readonly System.Collections.Generic.SortedSet<string> Ships = new();
        public readonly System.Collections.Generic.SortedSet<string> Loadout = new();
        public readonly System.Collections.Generic.SortedSet<string> Blueprints = new();
        public readonly System.Collections.Generic.List<string> Loot = new();
        public readonly System.Collections.Generic.List<(long amt, string label)> Money = new();
        public string? SampleMission;
        public readonly System.Collections.Generic.SortedSet<string> MissionTexts = new();
        public System.Collections.Generic.Dictionary<string, string> Meta = new();
        public int ExpiredTransfers;
        public long Income => In + Sales + Trade;
        public long Spend => Out + Spent;
        public long Net => Income - Spend;
    }

    static Sess ScanFile(string file)
    {
        var s = new Sess { Name = Path.GetFileName(file) };
        Core.Localization.Hint(file);   // Spiel-Wurzel für Item-Namen-Auflösung
        var parser = new LogParser();
        bool first = true;
        foreach (var line in Core.LogEntryReader.ReadEntries(ReadSharedLines(file)))
        {
            var ts = TimeOf(line);
            if (ts is { } t) { if (first) { s.Start = t; first = false; } s.End = t; }

            var e = parser.Feed(line);
            if (e == null) continue;
            if (e.Kind is EventKind.TransferIn or EventKind.TransferOut or EventKind.MissionReward
                       or EventKind.Purchase or EventKind.Sale or EventKind.Trade or EventKind.Fine)
                s.Money.Add((e.Amount, $"{e.KindText}: {e.Detail}"));
            switch (e.Kind)
            {
                case EventKind.TransferIn: s.In += e.Amount; break;
                case EventKind.TransferOut: s.Out += -e.Amount; break;
                case EventKind.Fine: s.Out += -e.Amount; break;
                case EventKind.MissionReward: s.In += e.Amount; s.Reward += e.Amount; break;
                case EventKind.Location: s.LastLoc = e.Detail; break;
                case EventKind.Purchase: s.Spent += -e.Amount; break;
                case EventKind.Sale: s.Sales += e.Amount; break;
                case EventKind.Trade: s.Trade += e.Amount; break;
                case EventKind.Mission: s.Missions++; s.SampleMission ??= e.Detail; s.MissionTexts.Add(e.Detail); break;
                case EventKind.Blueprint: s.Blueprints.Add(e.Detail); break;
                case EventKind.Jurisdiction: s.Zones++; break;
                case EventKind.Party: s.PartyEv++; break;
                case EventKind.MedBed: s.Deaths++; break;
                case EventKind.Hangar: s.Hangars++; break;
                case EventKind.Loadout: s.Loadout.Add(e.Detail); break;
                case EventKind.ShipLoss: s.Losses++; break;
                case EventKind.Death: s.Incaps++; break;
                case EventKind.Offer: s.Offers++; break;
                case EventKind.Impound: s.Impounds++; break;
                case EventKind.Friend: s.Friends++; break;
                case EventKind.Gear: s.Defekt++; break;
                case EventKind.MissionDone: s.MissionsDone++; break;
                case EventKind.Loot: s.Loot.Add(e.Detail); break;
            }
            if (e.Ship != null) s.Ships.Add(e.Ship);
        }
        s.Meta = parser.Meta;
        s.ExpiredTransfers = parser.ExpiredPendingTransfers;
        return s;
    }

    [GeneratedRegex(@"<(?<ts>\d{4}-\d{2}-\d{2}T[\d:.]+Z)>")]
    private static partial Regex TsRegex();

    static DateTime? TimeOf(string line)
    {
        var m = TsRegex().Match(line);
        return m.Success && DateTime.TryParse(m.Groups["ts"].Value, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var dt)
            ? dt : null;
    }

    static string Dur(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes}m" : $"{t.Minutes}m";

    // Liest auch Dateien, die SC gerade offen hält (Shared-Read).
    static System.Collections.Generic.IEnumerable<string> ReadSharedLines(string file)
    {
        using var fs = new FileStream(file, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var sr = new StreamReader(fs);
        string? l;
        while ((l = sr.ReadLine()) != null)
            yield return l;
    }
}
