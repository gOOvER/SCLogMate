using System;
using System.IO;
using System.Text.Json;
using SCLogReader.Models;

namespace SCLogReader.Core;

public class AppSettings
{
    public string? LogPath { get; set; }
    public long Balance { get; set; }   // echter Kontostand (manuell eingetragen)

    /// <summary>Zeitpunkt des Kontostand-Eintrags. Nur Bewegungen DANACH werden angerechnet —
    /// der eingetragene Wert ist der Stand von JETZT, nicht der Startwert der ganzen Historie
    /// (die ist ohnehin lückenhaft, weil SC alte Logs löscht).</summary>
    public DateTime? BalanceSetAt { get; set; }

    /// <summary>Aktiviert die automatische Erkennung via mobiGlas-Screenreader (OCR).</summary>
    public bool AutoOcrEnabled { get; set; } = true;

    /// <summary>Vom Nutzer ausgewählter mobiGlas Wallet-Bereich auf dem Bildschirm.</summary>
    public ScanRegion? WalletRegion { get; set; }

    /// <summary>Vom Nutzer ausgewählter mobiGlas Auftragsmanager-Bereich (Contract Manager).</summary>
    public ScanRegion? ContractRegion { get; set; }

    /// <summary>Optionaler UEX Corp API-Schlüssel für erweiterte Abfragen & Kontoverknüpfung.</summary>
    public string? UexApiKey { get; set; }

    /// <summary>In-Game Mini-HUD Overlay aktivieren.</summary>
    public bool OverlayEnabled { get; set; } = false;

    /// <summary>X-Position des frei verschiebbaren Mini-HUD Overlays.</summary>
    public double OverlayPositionX { get; set; } = 50;

    /// <summary>Y-Position des frei verschiebbaren Mini-HUD Overlays.</summary>
    public double OverlayPositionY { get; set; } = 50;

    /// <summary>Deckkraft des Mini-HUD Overlays (0.3 bis 1.0).</summary>
    public double OverlayOpacity { get; set; } = 0.92;

    /// <summary>WoW-Style Achievement & Reward Toast Banner aktivieren.</summary>
    public bool ToastEnabled { get; set; } = true;

    /// <summary>X-Position des Achievement Toast Banners (-1 = zentriert).</summary>
    public double ToastPositionX { get; set; } = -1;

    /// <summary>Y-Position des Achievement Toast Banners.</summary>
    public double ToastPositionY { get; set; } = 60;

    /// <summary>Anzeigedauer des Banners in Sekunden.</summary>
    public double ToastDurationSeconds { get; set; } = 5.5;

    /// <summary>Beim Klick auf das Schließen-Kreuz (X) ins Tray minimieren (sonst App direkt beenden).</summary>
    public bool MinimizeToTrayOnClose { get; set; } = true;

    /// <summary>Mit Windows automatisch starten (minimiert ins System-Tray).</summary>
    public bool AutostartEnabled { get; set; } = false;
}

/// <summary>Merkt sich Einstellungen (Log-Pfad, Kontostand, OCR-Region) über Starts hinweg.</summary>
public static class Settings
{
    private static readonly string NewDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SCLogMate");
    private static readonly string LegacyDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SCLogReader");

    public static string Dir
    {
        get
        {
            if (!Directory.Exists(NewDir))
            {
                Directory.CreateDirectory(NewDir);
                try
                {
                    if (Directory.Exists(LegacyDir))
                    {
                        foreach (var file in Directory.GetFiles(LegacyDir))
                        {
                            var dest = Path.Combine(NewDir, Path.GetFileName(file));
                            if (!File.Exists(dest)) File.Copy(file, dest);
                        }
                    }
                }
                catch { /* ignore */ }
            }
            return NewDir;
        }
    }

    public static string FilePath => Path.Combine(Dir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch { /* defekte Datei ignorieren */ }
        return new AppSettings();
    }

    public static void Save(AppSettings s)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var json = JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
            Logger.Log($"[SETTINGS] Gespeichert: WalletRegion={(s.WalletRegion != null ? $"{s.WalletRegion.Width}x{s.WalletRegion.Height}@({s.WalletRegion.X},{s.WalletRegion.Y})" : "null")}, ContractRegion={(s.ContractRegion != null ? $"{s.ContractRegion.Width}x{s.ContractRegion.Height}@({s.ContractRegion.X},{s.ContractRegion.Y})" : "null")}");
        }
        catch (Exception ex)
        {
            Logger.Error("[SETTINGS] Fehler beim Speichern", ex);
        }
    }
}
