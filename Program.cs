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

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetClassLongPtr", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern IntPtr SetClassLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetClassLong", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern IntPtr SetClassLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private static IntPtr SetClassLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        try
        {
            return IntPtr.Size == 8
                ? SetClassLongPtr64(hWnd, nIndex, dwNewLong)
                : SetClassLong32(hWnd, nIndex, dwNewLong);
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const uint WM_SETICON = 0x0080;
    private const IntPtr ICON_SMALL = 0;
    private const IntPtr ICON_BIG = (IntPtr)1;
    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x00000010;
    private const int GCLP_HICON = -14;
    private const int GCLP_HICONSM = -34;
    private const int SM_CXICON = 11;
    private const int SM_CYICON = 12;
    private const int SM_CXSMICON = 49;
    private const int SM_CYSMICON = 50;

    private static void ApplyWindowIcons(IntPtr hWnd, string iconPath)
    {
        if (hWnd == IntPtr.Zero || !File.Exists(iconPath)) return;
        try
        {
            int cxBig = GetSystemMetrics(SM_CXICON);
            int cyBig = GetSystemMetrics(SM_CYICON);
            int cxSmall = GetSystemMetrics(SM_CXSMICON);
            int cySmall = GetSystemMetrics(SM_CYSMICON);

            IntPtr hIconBig = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, cxBig > 0 ? cxBig : 32, cyBig > 0 ? cyBig : 32, LR_LOADFROMFILE);
            IntPtr hIconSmall = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, cxSmall > 0 ? cxSmall : 16, cySmall > 0 ? cySmall : 16, LR_LOADFROMFILE);

            if (hIconBig != IntPtr.Zero)
            {
                SendMessage(hWnd, WM_SETICON, ICON_BIG, hIconBig);
                SetClassLongPtr(hWnd, GCLP_HICON, hIconBig);
            }

            if (hIconSmall != IntPtr.Zero)
            {
                SendMessage(hWnd, WM_SETICON, ICON_SMALL, hIconSmall);
                SetClassLongPtr(hWnd, GCLP_HICONSM, hIconSmall);
            }
        }
        catch (Exception ex)
        {
            Core.Logger.Error("ApplyWindowIcons", ex);
        }
    }

    private static void EnsureStartMenuShortcut(string iconPath)
    {
        try
        {
            var startMenuDir = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            if (string.IsNullOrEmpty(startMenuDir) || !Directory.Exists(startMenuDir)) return;

            var shortcutPath = Path.Combine(startMenuDir, "SCLogMate.lnk");
            var exePath = Environment.ProcessPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SCLogMate.exe");
            if (!File.Exists(exePath)) return;

            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;

            dynamic shell = Activator.CreateInstance(shellType)!;
            var shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = exePath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(exePath) ?? "";
            if (File.Exists(iconPath))
            {
                shortcut.IconLocation = $"{iconPath},0";
            }
            else
            {
                shortcut.IconLocation = $"{exePath},0";
            }
            shortcut.Description = "Star Citizen Live Log Companion";
            shortcut.Save();
        }
        catch { }
    }

    private static string EnsureIconFile()
    {
        var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SCLogMate");
        var appDataIcon = Path.Combine(appDataDir, "SCLogMate.ico");

        // Immer sicherstellen, dass das Icon im AppData-Ordner existiert (für Verknüpfungen und Taskbar)
        try
        {
            if (!File.Exists(appDataIcon))
            {
                var asm = typeof(Program).Assembly;
                var resName = asm.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith("SCLogMate.ico", StringComparison.OrdinalIgnoreCase));
                if (resName != null)
                {
                    using var stream = asm.GetManifestResourceStream(resName);
                    if (stream != null)
                    {
                        Directory.CreateDirectory(appDataDir);
                        using var fs = File.Create(appDataIcon);
                        stream.CopyTo(fs);
                    }
                }
            }
        }
        catch { }

        // 1. Direkt neben der Executable
        var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SCLogMate.ico");
        if (File.Exists(localPath)) return localPath;

        // 2. Im Arbeitsverzeichnis
        if (File.Exists("SCLogMate.ico")) return Path.GetFullPath("SCLogMate.ico");

        // 3. Im AppData-Ordner
        if (File.Exists(appDataIcon)) return appDataIcon;

        return localPath;
    }

    private static void RunPhotinoApp(string[] args)
    {
        var iconPath = EnsureIconFile();

        // Startmenü-Verknüpfung für Windows Shell Icon-Cache registrieren
        EnsureStartMenuShortcut(iconPath);

        var settings = Settings.Load();

        int screenW = GetSystemMetrics(0); // SM_CXSCREEN
        int screenH = GetSystemMetrics(1); // SM_CYSCREEN

        // Großzügige Standard-Fenstergröße: Verhindert gequetschte Tabellen und unschöne Umbrüche von Anfang an
        int width = settings.WindowWidth >= 1200 ? settings.WindowWidth : 0;
        int height = settings.WindowHeight >= 700 ? settings.WindowHeight : 0;

        if (width == 0 || height == 0)
        {
            if (screenW >= 2560 && screenH >= 1440)
            {
                width = 1920;
                height = 1140;
            }
            else if (screenW >= 1920 && screenH >= 1080)
            {
                width = 1680;
                height = 980;
            }
            else if (screenW > 0 && screenH > 0)
            {
                width = Math.Max(1200, (int)(screenW * 0.90));
                height = Math.Max(720, (int)(screenH * 0.90));
            }
            else
            {
                width = 1680;
                height = 980;
            }
        }

        // Sicherstellen, dass das Fenster auf dem primären Monitor Platz hat
        if (screenW > 0 && screenH > 0)
        {
            width = Math.Min(width, Math.Max(1200, screenW - 60));
            height = Math.Min(height, Math.Max(700, screenH - 80));
        }

        var window = new Photino.NET.PhotinoWindow()
            .SetTitle("SCLogMate — Star Citizen Live Companion")
            .SetUseOsDefaultSize(false)
            .SetSize(width, height)
            .SetMinSize(1200, 720)
            .SetNotificationRegistrationId(Guid.NewGuid().ToString())
            .Center();

        if (settings.WindowMaximized)
        {
            try { window.SetMaximized(true); } catch { }
        }

        window.RegisterWindowClosingHandler((sender, e) =>
        {
            try
            {
                var s = Settings.Load();
                s.WindowMaximized = window.Maximized;
                if (!window.Maximized && window.Size.Width >= 1200 && window.Size.Height >= 700)
                {
                    s.WindowWidth = window.Size.Width;
                    s.WindowHeight = window.Size.Height;
                }
                Settings.Save(s);
            }
            catch { }
            return false;
        });

        try { Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory; } catch { }

        if (File.Exists(iconPath))
        {
            try { window.SetIconFile(iconPath); } catch { }
        }

        window.RegisterWindowCreatedHandler((sender, e) =>
        {
            ApplyWindowIcons(window.WindowHandle, iconPath);
        });

        // Zusätzliche verzögerte Tasks, um das Taskleisten- und Klassen-Icon nach vollem Aufbau von WebView2 zu sichern
        System.Threading.Tasks.Task.Run(async () =>
        {
            foreach (var delay in new[] { 150, 600, 1500, 3000 })
            {
                await System.Threading.Tasks.Task.Delay(delay);
                try
                {
                    if (window.WindowHandle != IntPtr.Zero)
                    {
                        ApplyWindowIcons(window.WindowHandle, iconPath);
                    }
                }
                catch { }
            }
        });

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
