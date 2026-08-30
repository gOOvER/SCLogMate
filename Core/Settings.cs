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

    /// <summary>In-Game Achievement & Reward Toast Banner aktivieren.</summary>
    public bool ToastEnabled { get; set; } = true;

    /// <summary>Toast bei erlernten Bauplänen anzeigen.</summary>
    public bool ToastBlueprintEnabled { get; set; } = true;

    /// <summary>Toast bei abgeschlossenen Missionen anzeigen.</summary>
    public bool ToastMissionEnabled { get; set; } = true;

    /// <summary>Toast bei Fraktions-Beförderungen (Rang-Aufstieg) anzeigen.</summary>
    public bool ToastReputationEnabled { get; set; } = true;

    /// <summary>Toast bei abgeschlossenen Veredelungsaufträgen anzeigen.</summary>
    public bool ToastRefineryEnabled { get; set; } = true;

    /// <summary>Toast bei Fracht- und Schiffsaufzug-Bereitstellung anzeigen.</summary>
    public bool ToastElevatorEnabled { get; set; } = true;

    /// <summary>Toast bei Schiffszerstörung oder Versicherungs-Claims anzeigen.</summary>
    public bool ToastShipDestructionEnabled { get; set; } = true;

    /// <summary>Subtiler Soundeffekt bei Benachrichtigungen abspielen.</summary>
    public bool ToastSoundEnabled { get; set; } = false;

    /// <summary>HUD-Position sperren (verhindert Verschieben im Spiel).</summary>
    public bool OverlayLocked { get; set; } = false;

    /// <summary>Klicks durch das Mini-HUD hindurch an das Spiel durchreichen (Click-Through).</summary>
    public bool OverlayClickThrough { get; set; } = false;

    /// <summary>Globaler Tastatur-Hotkey zum Ein-/Ausblenden des Mini-HUDs (z. B. Alt+H).</summary>
    public bool GlobalHotkeyEnabled { get; set; } = true;

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

    /// <summary>Vom Nutzer gewählte UI-Schriftart.</summary>
    public string SelectedFontFamily { get; set; } = "Inter";

    /// <summary>Wipe-Filter aktivieren (ignoriert Events vor dem Stichtag bei Statistiken/Summen).</summary>
    public bool WipeFilterEnabled { get; set; } = false;

    /// <summary>Stichtag des letzten Wipes (Format: YYYY-MM-DD, z.B. 2026-05-15 für Alpha 4.8).</summary>
    public string WipeDateString { get; set; } = "2026-05-15";

    /// <summary>aUEC-Finanzsaldo ab Wipe-Datum filtern.</summary>
    public bool WipeFilterMoney { get; set; } = true;

    /// <summary>Auftrags- & Missionsstatistiken ab Wipe-Datum filtern.</summary>
    public bool WipeFilterContracts { get; set; } = true;

    /// <summary>Flotte & Schiffsaktivitäten ab Wipe-Datum filtern.</summary>
    public bool WipeFilterFleet { get; set; } = false;

    /// <summary>Erlernte Baupläne ab Wipe-Datum filtern.</summary>
    public bool WipeFilterBlueprints { get; set; } = false;
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
