using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace SCLogMate.Core.Ocr;

public sealed record ScannedShipComponent(
    string SlotType,      // Cooler, Shield, PowerPlant, QuantumDrive, Weapon, Paint
    string SlotLabel,     // z. B. "Shield Generator 1", "Cooler ×2", "Weapon 3"
    string ComponentName  // z. B. "FR-66", "Glacier", "CF-337 Panther", "Keystone Livery"
);

public sealed record ScreenshotLoadoutResult(
    bool Success,
    string? ShipName,
    string? Livery,
    IReadOnlyList<ScannedShipComponent> Components,
    string? SourceFile,
    string? Message
);

/// <summary>
/// Erkennt Schiffs-Ausrüstungen (VLM mobiGlas und ASOP Fleet Manager) aus Screenshots
/// und überwacht optional den Screenshot-Ordner von Star Citizen.
/// </summary>
public sealed partial class ScreenshotLoadoutWatcher : IDisposable
{
    private readonly OcrEngineService _ocr;
    private FileSystemWatcher? _watcher;
    private readonly ConcurrentDictionary<string, byte> _processedFiles = new(StringComparer.OrdinalIgnoreCase);

    public event Action<ScreenshotLoadoutResult>? OnLoadoutDetected;

    public bool IsWatching => _watcher != null && _watcher.EnableRaisingEvents;
    public string? WatchedFolder { get; private set; }

    public ScreenshotLoadoutWatcher(OcrEngineService ocr)
    {
        _ocr = ocr;
    }

    /// <summary>
    /// Startet die Überwachung des Star Citizen Screenshot-Verzeichnisses.
    /// </summary>
    public bool StartWatching(string? customFolder = null)
    {
        StopWatching();

        var folder = customFolder;
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            folder = DetectStarCitizenScreenshotFolder();
        }

        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            Logger.Log("ScreenshotLoadoutWatcher: Kein gültiger Screenshot-Ordner gefunden.");
            return false;
        }

        try
        {
            WatchedFolder = folder;
            _watcher = new FileSystemWatcher(folder)
            {
                Filter = "*.*",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            _watcher.Created += OnFileCreated;
            Logger.Log($"ScreenshotLoadoutWatcher: Überwache '{folder}' auf VLM/Flottenmanager-Screenshots.");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("ScreenshotLoadoutWatcher Start", ex);
            return false;
        }
    }

    public void StopWatching()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= OnFileCreated;
            _watcher.Dispose();
            _watcher = null;
        }
    }

    private async void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        var ext = Path.GetExtension(e.FullPath).ToLowerInvariant();
        if (ext != ".jpg" && ext != ".png" && ext != ".jpeg") return;

        if (!_processedFiles.TryAdd(e.FullPath, 0)) return;

        // Kurze Pause, damit die Bilddatei von Star Citizen vollständig geschrieben wird
        await Task.Delay(1200);

        try
        {
            var res = await AnalyzeScreenshotAsync(e.FullPath);
            if (res.Success)
            {
                OnLoadoutDetected?.Invoke(res);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"ScreenshotLoadoutWatcher analyse error: {e.FullPath}", ex);
        }
    }

    /// <summary>
    /// Analysiert eine Screenshot-Bilddatei auf Schiffsloadout (VLM mobiGlas oder ASOP Fleet Manager).
    /// </summary>
    public async Task<ScreenshotLoadoutResult> AnalyzeScreenshotAsync(string imagePath)
    {
        if (!File.Exists(imagePath))
            return new(false, null, null, Array.Empty<ScannedShipComponent>(), imagePath, "Datei existiert nicht.");

        if (!_ocr.IsAvailable)
            return new(false, null, null, Array.Empty<ScannedShipComponent>(), imagePath, "OCR-Engine nicht verfügbar.");

        string? ocrText;
        try
        {
            ocrText = await _ocr.RecognizeImageFileAsync(imagePath);
        }
        catch (Exception ex)
        {
            return new(false, null, null, Array.Empty<ScannedShipComponent>(), imagePath, $"OCR Fehler: {ex.Message}");
        }

        if (string.IsNullOrWhiteSpace(ocrText))
            return new(false, null, null, Array.Empty<ScannedShipComponent>(), imagePath, "Kein Text im Bild erkannt.");

        return ParseLoadoutFromText(ocrText, imagePath);
    }

    /// <summary>
    /// Extrahiert Schiff, Lackierung und Komponenten aus dem erkannten OCR-Text.
    /// </summary>
    public static ScreenshotLoadoutResult ParseLoadoutFromText(string ocrText, string? sourceFile = null)
    {
        var lines = ocrText.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        // 1. Prüfen, ob es sich um VLM oder Fleet Manager handelt
        bool isVlm = ocrText.Contains("Vehicle Loadout", StringComparison.OrdinalIgnoreCase) ||
                     ocrText.Contains("Loadout Manager", StringComparison.OrdinalIgnoreCase) ||
                     ocrText.Contains("VLM", StringComparison.OrdinalIgnoreCase);

        bool isFleetManager = ocrText.Contains("Fleet Manager", StringComparison.OrdinalIgnoreCase) ||
                              ocrText.Contains("Vehicle Information", StringComparison.OrdinalIgnoreCase) ||
                              ocrText.Contains("ASOP", StringComparison.OrdinalIgnoreCase);

        bool hasComponents = ocrText.Contains("Cooler", StringComparison.OrdinalIgnoreCase) ||
                             ocrText.Contains("Power Plant", StringComparison.OrdinalIgnoreCase) ||
                             ocrText.Contains("Shield", StringComparison.OrdinalIgnoreCase) ||
                             ocrText.Contains("Quantum Drive", StringComparison.OrdinalIgnoreCase) ||
                             ocrText.Contains("Weapon", StringComparison.OrdinalIgnoreCase);

        if (!isVlm && !isFleetManager && !hasComponents)
        {
            return new(false, null, null, Array.Empty<ScannedShipComponent>(), sourceFile, "Kein Schiffs-Ausrüstungsbildschirm erkannt.");
        }

        // 2. Schiffsname ermitteln
        string? shipName = DetectShipName(lines);

        // 3. Lackierung / Livery ermitteln
        string? livery = DetectLivery(lines);

        // 4. Komponenten parsen
        var components = ParseComponents(lines);

        if (components.Count == 0 && string.IsNullOrWhiteSpace(shipName))
        {
            return new(false, null, null, Array.Empty<ScannedShipComponent>(), sourceFile, "Keine Komponenten oder Schiffsnamen identifiziert.");
        }

        return new(
            Success: true,
            ShipName: shipName,
            Livery: livery,
            Components: components,
            SourceFile: sourceFile,
            Message: $"Erfolgreich eingelesen: {shipName ?? "Schiff"} ({components.Count} Komponenten erkannt)"
        );
    }

    private static readonly HashSet<string> UiNoise = new(StringComparer.OrdinalIgnoreCase)
    {
        "Avionics", "Propulsion", "Systems", "Vhcl. Wpns.", "Utility", "Liveries", "Misc.", "HEALTH",
        "HOME", "COMMS", "CONTRACTS", "MAPS", "JOURNAL", "ASSETS", "REP", "WALLET", "LANDING", "VEHICLES",
        "SIZE", "GRADE", "Slot", "Size", "Type", "Awaiting Selection...", "Select an item from the",
        "Only showing ships and equipment located in Crusader.", "For a full list use the mobiGlas Assets app.",
        "For a full list use the mobiGIas Assets app.", "view its details.", "><", "x", "X", "Empty", "EQUIPPED",
        "EOUIPPEO", "Available:", "In Use:", "In use:", "1", "2", "3", "4", "5", "6", "7", "8",
        "X UNEQUIP", "X UNEOUIP", "CURRENTLY EQUIPPED", "CURRENTLY EQUIPPEO", "Vehicle Loadout Manager",
        "Customize your ship Ioadout", "Customize your ship loadout", "Vehicle Information", "Fleet Manager",
        "L", "Item Type:", "Manufacturer:", "Class:", "Grade:"
    };

    private static string? DetectShipName(List<string> lines)
    {
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length < 3 || UiNoise.Contains(trimmed) || trimmed.StartsWith("Ä")) continue;

            // Pattern: Hersteller Präfix + Modell (z.B. "RSI HERMES", "ARGO MOTH")
            var m = Regex.Match(trimmed, @"^(?:RSI|ARGO|DRAKE|AEGIS|ANVIL|MISC|ORIGIN|CRUSADER|MIRAI|ESPERIA|GATAC|BANU|GREYCAT|TUMBRILL?)\s+(?<model>.+)$", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var candidate = m.Groups["model"].Value.Trim();
                var catEntry = FleetCatalog.Lookup(candidate);
                if (catEntry.NormalizedName != "Unbekannt" && catEntry.Role != "Raumschiff")
                {
                    return catEntry.NormalizedName;
                }
            }

            // Exakter Katalog-Abgleich auf bekannte Schiffe
            var direct = FleetCatalog.Lookup(trimmed);
            if (direct.NormalizedName != "Unbekannt" && direct.Role != "Raumschiff" && !direct.NormalizedName.Equals("Unbekannt", StringComparison.OrdinalIgnoreCase))
            {
                return direct.NormalizedName;
            }

            // Keyword check for recognizable ships in mobiGlas
            if (trimmed.Contains("HERMES", StringComparison.OrdinalIgnoreCase)) return FleetCatalog.Lookup("Hermes").NormalizedName;
            if (trimmed.Contains("MOTH", StringComparison.OrdinalIgnoreCase)) return FleetCatalog.Lookup("MOTH").NormalizedName;
            if (trimmed.Contains("RAFT", StringComparison.OrdinalIgnoreCase)) return FleetCatalog.Lookup("RAFT").NormalizedName;
            if (trimmed.Contains("CLIPPER", StringComparison.OrdinalIgnoreCase)) return FleetCatalog.Lookup("Clipper").NormalizedName;
            if (trimmed.Contains("CORSAIR", StringComparison.OrdinalIgnoreCase)) return FleetCatalog.Lookup("Corsair").NormalizedName;
            if (trimmed.Contains("VULTURE", StringComparison.OrdinalIgnoreCase)) return FleetCatalog.Lookup("Vulture").NormalizedName;
            if (trimmed.Contains("REDEEMER", StringComparison.OrdinalIgnoreCase)) return FleetCatalog.Lookup("Redeemer").NormalizedName;
            if (trimmed.Contains("TITAN", StringComparison.OrdinalIgnoreCase)) return FleetCatalog.Lookup("Avenger Titan").NormalizedName;
            if (trimmed.Contains("CUTLASS", StringComparison.OrdinalIgnoreCase)) return FleetCatalog.Lookup("Cutlass Black").NormalizedName;
        }
        return null;
    }

    private static string? DetectLivery(List<string> lines)
    {
        foreach (var line in lines)
        {
            var trimmed = Regex.Replace(line, @"\[?\s*\(?\s*BRICKE[D\]I\)]*\s*\]?", "").Trim();
            if ((trimmed.Contains("Livery", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("Paint", StringComparison.OrdinalIgnoreCase))
                && !trimmed.Equals("Liveries", StringComparison.OrdinalIgnoreCase)
                && !trimmed.Equals("Livery", StringComparison.OrdinalIgnoreCase)
                && !trimmed.Equals("Paints", StringComparison.OrdinalIgnoreCase)
                && trimmed.Length > 6)
            {
                return trimmed;
            }
        }
        return null;
    }

    private static List<ScannedShipComponent> ParseComponents(List<string> lines)
    {
        var result = new List<ScannedShipComponent>();

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            string? slotType = null;
            string? slotLabel = null;

            if (Regex.IsMatch(line, @"^Cooler(?:\s*([0-9IVX]+))?", RegexOptions.IgnoreCase))
            {
                slotType = "Cooler";
                slotLabel = CleanSlotLabel(line, "Cooler");
            }
            else if (Regex.IsMatch(line, @"^Power\s*Plant(?:\s*([0-9IVX]+))?", RegexOptions.IgnoreCase))
            {
                slotType = "PowerPlant";
                slotLabel = CleanSlotLabel(line, "Power Plant");
            }
            else if (Regex.IsMatch(line, @"^(?:Shield\s*Generator|Shield)(?:\s*([0-9IVX]+))?", RegexOptions.IgnoreCase))
            {
                slotType = "Shield";
                slotLabel = CleanSlotLabel(line, "Shield Generator");
            }
            else if (Regex.IsMatch(line, @"^Quantum\s*Drive", RegexOptions.IgnoreCase))
            {
                slotType = "QuantumDrive";
                slotLabel = "Quantum Drive";
            }
            else if (Regex.IsMatch(line, @"^Jump\s*Module", RegexOptions.IgnoreCase))
            {
                slotType = "QuantumDrive";
                slotLabel = "Jump Module";
            }
            else if (Regex.IsMatch(line, @"^Weapon\s*-\s*(Left|Right|Front|Top\s*Left|Top\s*Right|Top|Bottom|Rear)", RegexOptions.IgnoreCase))
            {
                slotType = "Weapon";
                slotLabel = line.Trim();
            }
            else if (Regex.IsMatch(line, @"^Turret\s*Weapon\s*Slot\s*\d+", RegexOptions.IgnoreCase))
            {
                slotType = "Turret";
                slotLabel = line.Trim();
            }
            else if (Regex.IsMatch(line, @"^(?:Remote|Manned)\s*Turret", RegexOptions.IgnoreCase))
            {
                slotType = "Turret";
                slotLabel = line.Trim();
            }
            else if (Regex.IsMatch(line, @"^Radar", RegexOptions.IgnoreCase))
            {
                slotType = "Avionics";
                slotLabel = "Radar";
            }
            else if (Regex.IsMatch(line, @"^Flight\s*Blade", RegexOptions.IgnoreCase))
            {
                slotType = "Avionics";
                slotLabel = "Flight Blade";
            }
            else if (Regex.IsMatch(line, @"^(?:Tractor\s*(?:Mount|Turret)|Salvage\s*Head)", RegexOptions.IgnoreCase))
            {
                slotType = "Utility";
                slotLabel = line.Trim();
            }

            if (slotType != null && slotLabel != null)
            {
                string? compLine = null;
                int k = i + 1;
                while (k < lines.Count && k <= i + 10)
                {
                    var cand = lines[k].Trim();
                    if (cand.StartsWith("Cooler", StringComparison.OrdinalIgnoreCase) ||
                        cand.StartsWith("Power Plant", StringComparison.OrdinalIgnoreCase) ||
                        cand.StartsWith("Shield", StringComparison.OrdinalIgnoreCase) ||
                        cand.StartsWith("Quantum", StringComparison.OrdinalIgnoreCase) ||
                        cand.StartsWith("Jump Module", StringComparison.OrdinalIgnoreCase) ||
                        cand.StartsWith("Weapon -", StringComparison.OrdinalIgnoreCase) ||
                        cand.StartsWith("Turret Weapon", StringComparison.OrdinalIgnoreCase) ||
                        cand.StartsWith("Radar", StringComparison.OrdinalIgnoreCase) ||
                        cand.StartsWith("Flight Blade", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    if (!UiNoise.Contains(cand) && cand.Length > 2 && !cand.StartsWith("Ä") && !cand.StartsWith("Select") &&
                        !Regex.IsMatch(cand, @"^\d+\.(?:Turret|Gun)", RegexOptions.IgnoreCase))
                    {
                        compLine = cand;
                        if (compLine.Contains("Gimbal Mount", StringComparison.OrdinalIgnoreCase) && k + 1 < lines.Count)
                        {
                            var nextGun = lines[k + 1].Trim();
                            if (!UiNoise.Contains(nextGun) && nextGun.Length > 2 && !nextGun.StartsWith("Ä"))
                            {
                                compLine = $"{nextGun} ({compLine})";
                            }
                        }
                        break;
                    }
                    k++;
                }

                if (!string.IsNullOrWhiteSpace(compLine))
                {
                    compLine = Regex.Replace(compLine, @"\[?\s*\(?\s*BRICKE[D\]I\)]*\s*\]?", "").Trim();
                    compLine = Regex.Replace(compLine, @"[O0\]]$", "").Trim();
                    result.Add(new ScannedShipComponent(slotType, slotLabel, compLine));
                }
            }
        }

        return result;
    }

    private static string CleanSlotLabel(string raw, string prefix)
    {
        var cleaned = raw.Replace(" I ", " 1 ").Replace(" II ", " 2 ").Replace(" III ", " 3 ").Replace(" IV ", " 4 ");
        cleaned = Regex.Replace(cleaned, @"\s+I$", " 1");
        cleaned = Regex.Replace(cleaned, @"\s+II$", " 2");
        cleaned = Regex.Replace(cleaned, @"\s+III$", " 3");
        cleaned = Regex.Replace(cleaned, @"\s+IV$", " 4");
        return cleaned.Trim();
    }

    public static string? DetectStarCitizenScreenshotFolder()
    {
        // Standardpfade auf gängigen Laufwerken suchen
        string[] drives = { "C", "D", "E", "F", "G", "J", "X" };
        foreach (var d in drives)
        {
            var livePath = $@"{d}:\StarCitizen\LIVE\ScreenShots";
            if (Directory.Exists(livePath)) return livePath;

            var rsiPath = $@"{d}:\Program Files\Roberts Space Industries\StarCitizen\LIVE\ScreenShots";
            if (Directory.Exists(rsiPath)) return rsiPath;
        }

        // Falls Log-Verzeichnis bekannt ist
        var defaultLogDir = @"j:\StarCitizen\LIVE";
        if (Directory.Exists(Path.Combine(defaultLogDir, "ScreenShots")))
        {
            return Path.Combine(defaultLogDir, "ScreenShots");
        }

        return null;
    }

    public void Dispose()
    {
        StopWatching();
    }
}
