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

    private static string? DetectShipName(List<string> lines)
    {
        // Bekannte Schiffe prüfen
        foreach (var line in lines)
        {
            foreach (var ship in FleetCatalog.AllShips)
            {
                if (line.Contains(ship.NormalizedName, StringComparison.OrdinalIgnoreCase))
                {
                    return ship.NormalizedName;
                }
            }
        }
        return null;
    }

    private static string? DetectLivery(List<string> lines)
    {
        foreach (var line in lines)
        {
            if (line.Contains("Livery", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Paint", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Skin", StringComparison.OrdinalIgnoreCase))
            {
                return line.Trim();
            }
        }
        return null;
    }

    private static List<ScannedShipComponent> ParseComponents(List<string> lines)
    {
        var result = new List<ScannedShipComponent>();

        var slotPatterns = new (string SlotType, Regex Pattern)[]
        {
            ("Cooler", new Regex(@"Cooler\s*(?:×\s*(?<cnt>\d+)|(?<num>\d+))?", RegexOptions.IgnoreCase)),
            ("Shield", new Regex(@"(?:Shield Generator|Shield)\s*(?:×\s*(?<cnt>\d+)|(?<num>\d+))?", RegexOptions.IgnoreCase)),
            ("PowerPlant", new Regex(@"Power Plant\s*(?:×\s*(?<cnt>\d+)|(?<num>\d+))?", RegexOptions.IgnoreCase)),
            ("QuantumDrive", new Regex(@"Quantum Drive\s*(?:×\s*(?<cnt>\d+)|(?<num>\d+))?", RegexOptions.IgnoreCase)),
            ("Weapon", new Regex(@"Weapon\s*(?:×\s*(?<cnt>\d+)|(?<num>\d+))?", RegexOptions.IgnoreCase)),
            ("Missile", new Regex(@"Missile Rack\s*(?:×\s*(?<cnt>\d+)|(?<num>\d+))?", RegexOptions.IgnoreCase)),
        };

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            foreach (var (slotType, pattern) in slotPatterns)
            {
                var match = pattern.Match(line);
                if (match.Success)
                {
                    // Die nächste Zeile enthält typischerweise den Komponentennamen (z.B. "FR-66", "Panther")
                    string componentName = "";
                    if (i + 1 < lines.Count && !slotPatterns.Any(p => p.Pattern.IsMatch(lines[i + 1])))
                    {
                        componentName = lines[i + 1].Trim();
                    }
                    else
                    {
                        // Falls in derselben Zeile (z.B. "Cooler 1: Glacier")
                        var colonIdx = line.IndexOf(':');
                        if (colonIdx > 0)
                        {
                            componentName = line[(colonIdx + 1)..].Trim();
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(componentName))
                    {
                        result.Add(new ScannedShipComponent(
                            SlotType: slotType,
                            SlotLabel: match.Value.Trim(),
                            ComponentName: componentName
                        ));
                    }
                    break;
                }
            }
        }

        return result;
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
