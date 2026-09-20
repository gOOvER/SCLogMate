using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
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
    string? Message,
    bool IsFullSnapshot = false
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

        // 1. Prüfen, ob es sich um VLM, ASOP Fleet Manager oder LOADOUT ESTIMATE handelt
        bool isLoadoutEstimate = ocrText.Contains("LOADOUT ESTIMATE", StringComparison.OrdinalIgnoreCase) ||
                                 (ocrText.Contains("REPLACEMENT FEE", StringComparison.OrdinalIgnoreCase) && ocrText.Contains("FLEET MANAGE", StringComparison.OrdinalIgnoreCase));

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
                             ocrText.Contains("Weapon", StringComparison.OrdinalIgnoreCase) ||
                             ocrText.Contains("Radar", StringComparison.OrdinalIgnoreCase);

        if (!isLoadoutEstimate && !isVlm && !isFleetManager && !hasComponents)
        {
            return new(false, null, null, Array.Empty<ScannedShipComponent>(), sourceFile, "Kein Schiffs-Ausrüstungsbildschirm erkannt.");
        }

        // 2. Schiffsname ermitteln
        string? shipName = DetectShipName(lines);

        // 3. Lackierung / Livery ermitteln
        string? livery = DetectLivery(lines);
        if (isLoadoutEstimate && string.IsNullOrWhiteSpace(livery))
        {
            foreach (var l in lines)
            {
                var clean = l.Trim();
                if (clean.EndsWith("Rockwell", StringComparison.OrdinalIgnoreCase) ||
                    clean.EndsWith("Paint", StringComparison.OrdinalIgnoreCase) ||
                    clean.EndsWith("Livery", StringComparison.OrdinalIgnoreCase) ||
                    clean.EndsWith("Skin", StringComparison.OrdinalIgnoreCase))
                {
                    livery = clean;
                    break;
                }
            }
        }

        // 4. Komponenten parsen
        var components = isLoadoutEstimate
            ? ParseLoadoutEstimateComponents(lines)
            : ParseComponents(lines);

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
            Message: $"Erfolgreich eingelesen: {shipName ?? "Schiff"} ({components.Count} Komponenten erkannt)",
            IsFullSnapshot: isLoadoutEstimate
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

            if (trimmed.StartsWith("Cooler", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Shield", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Power Plant", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Quantum", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Jump", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Weapon", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Missile", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Turret", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Livery", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("Liveries", StringComparison.OrdinalIgnoreCase))
                continue;

            // Pattern: Hersteller Präfix + Modell (z.B. "RSI HERMES", "ARGO MOTH")
            var m = Regex.Match(trimmed, @"^(?:RSI|ARGO|DRAKE|AEGIS|ANVIL|MISC|ORIGIN|CRUSADER|MIRAI|ESPERIA|GATAC|BANU|GREYCAT|TUMBRILL?)\s+(?<model>.+)$", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var candidate = m.Groups["model"].Value.Trim();
                if (FleetCatalog.IsKnownCatalogShip(candidate))
                {
                    return FleetCatalog.Lookup(candidate).NormalizedName;
                }
            }

            // Exakter Katalog-Abgleich auf bekannte Schiffe
            if (FleetCatalog.IsKnownCatalogShip(trimmed))
            {
                return FleetCatalog.Lookup(trimmed).NormalizedName;
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
            var cleanLine = Regex.Replace(line, @"^[\s\-L><|•·\*\.]+\s*", "").Trim();

            string? slotType = null;
            string? slotLabel = null;

            if (Regex.IsMatch(cleanLine, @"^Cooler(?:\s*([0-9IVX]+))?", RegexOptions.IgnoreCase))
            {
                slotType = "Cooler";
                slotLabel = CleanSlotLabel(cleanLine, "Cooler");
            }
            else if (Regex.IsMatch(cleanLine, @"^Power\s*Plant(?:\s*([0-9IVX]+))?", RegexOptions.IgnoreCase))
            {
                slotType = "PowerPlant";
                slotLabel = CleanSlotLabel(cleanLine, "Power Plant");
            }
            else if (Regex.IsMatch(cleanLine, @"^(?:Shield\s*Generator|Shield)(?:\s*([0-9IVX]+))?", RegexOptions.IgnoreCase))
            {
                slotType = "Shield";
                slotLabel = CleanSlotLabel(cleanLine, "Shield Generator");
            }
            else if (Regex.IsMatch(cleanLine, @"^Quantum\s*Drive", RegexOptions.IgnoreCase))
            {
                slotType = "QuantumDrive";
                slotLabel = "Quantum Drive";
            }
            else if (Regex.IsMatch(cleanLine, @"^Jump\s*Module", RegexOptions.IgnoreCase))
            {
                slotType = "QuantumDrive";
                slotLabel = "Jump Module";
            }
            else if (Regex.IsMatch(cleanLine, @"^Weapon\s*-\s*(Left|Right|Front|Top\s*Left|Top\s*Right|Top|Bottom|Rear)", RegexOptions.IgnoreCase))
            {
                slotType = "Weapon";
                slotLabel = cleanLine;
            }
            else if (Regex.IsMatch(cleanLine, @"^Turret\s*Weapon\s*Slot\s*\d+", RegexOptions.IgnoreCase))
            {
                slotType = "Turret";
                slotLabel = cleanLine;
            }
            else if (Regex.IsMatch(cleanLine, @"^(?:Remote|Manned)\s*Turret", RegexOptions.IgnoreCase))
            {
                slotType = "Turret";
                slotLabel = cleanLine;
            }
            else if (Regex.IsMatch(cleanLine, @"(?:^|\b)Radar(?:\s*([0-9IVX]+))?", RegexOptions.IgnoreCase))
            {
                slotType = "Avionics";
                slotLabel = CleanSlotLabel(cleanLine, "Radar");
            }
            else if (Regex.IsMatch(cleanLine, @"^Flight\s*Blade", RegexOptions.IgnoreCase))
            {
                slotType = "Avionics";
                slotLabel = "Flight Blade";
            }
            else if (Regex.IsMatch(cleanLine, @"^(?:Tractor\s*(?:Mount|Turret)|Salvage\s*Head)", RegexOptions.IgnoreCase))
            {
                slotType = "Utility";
                slotLabel = cleanLine;
            }

            if (slotType != null && slotLabel != null)
            {
                string? compLine = null;
                int k = i + 1;
                while (k < lines.Count && k <= i + 35)
                {
                    var cand = lines[k].Trim();
                    var candClean = Regex.Replace(cand, @"^[\s\-L><|•·\*\.]+\s*", "").Trim();

                    if (candClean.StartsWith("Cooler", StringComparison.OrdinalIgnoreCase) ||
                        candClean.StartsWith("Power Plant", StringComparison.OrdinalIgnoreCase) ||
                        candClean.StartsWith("Shield", StringComparison.OrdinalIgnoreCase) ||
                        candClean.StartsWith("Quantum", StringComparison.OrdinalIgnoreCase) ||
                        candClean.StartsWith("Jump Module", StringComparison.OrdinalIgnoreCase) ||
                        candClean.StartsWith("Weapon -", StringComparison.OrdinalIgnoreCase) ||
                        candClean.StartsWith("Turret Weapon", StringComparison.OrdinalIgnoreCase) ||
                        candClean.StartsWith("Radar", StringComparison.OrdinalIgnoreCase) ||
                        candClean.StartsWith("Flight Blade", StringComparison.OrdinalIgnoreCase) ||
                        candClean.StartsWith("Missile", StringComparison.OrdinalIgnoreCase) ||
                        candClean.StartsWith("Tractor", StringComparison.OrdinalIgnoreCase) ||
                        candClean.StartsWith("Salvage", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    if (candClean.Equals("Empty", StringComparison.OrdinalIgnoreCase) ||
                        candClean.Equals("EOUIPPEO", StringComparison.OrdinalIgnoreCase) ||
                        candClean.Equals("EQUIPPED", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    // Ignoriere Währungszeilen (Ä 6.438,230) und den direkt darauf folgenden Spieler-Handle (z.B. GOOVER)
                    bool isAfterCurrency = k > 0 && lines[k - 1].Trim().StartsWith("Ä");
                    if (isAfterCurrency ||
                        candClean.Equals("GOOVER", StringComparison.OrdinalIgnoreCase) ||
                        candClean.Equals("gOOvER", StringComparison.OrdinalIgnoreCase) ||
                        candClean.StartsWith("Missile Slot", StringComparison.OrdinalIgnoreCase))
                    {
                        k++;
                        continue;
                    }

                    if (!UiNoise.Contains(cand) && !UiNoise.Contains(candClean) && candClean.Length > 2 &&
                        !candClean.StartsWith("Ä") && !candClean.StartsWith("Select") &&
                        !candClean.Contains("Livery", StringComparison.OrdinalIgnoreCase) &&
                        !candClean.Contains("Paint", StringComparison.OrdinalIgnoreCase) &&
                        !candClean.Contains("HERMES", StringComparison.OrdinalIgnoreCase) &&
                        !candClean.Contains("MOTH", StringComparison.OrdinalIgnoreCase) &&
                        !Regex.IsMatch(candClean, @"^\d+\.(?:Turret|Gun)", RegexOptions.IgnoreCase))
                    {
                        compLine = candClean;
                        if (compLine.Contains("Gimbal Mount", StringComparison.OrdinalIgnoreCase) && k + 1 < lines.Count)
                        {
                            var nextGun = Regex.Replace(lines[k + 1].Trim(), @"^[\s\-L><|•·\*\.]+\s*", "").Trim();
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

                    // Disambiguate Utility hardpoints (e.g. Salvage Heads on Weapon slots)
                    if (compLine.Contains("Salvage Head", StringComparison.OrdinalIgnoreCase) ||
                        compLine.Contains("Scraper", StringComparison.OrdinalIgnoreCase) ||
                        compLine.Contains("Tractor", StringComparison.OrdinalIgnoreCase) ||
                        compLine.Contains("Mining", StringComparison.OrdinalIgnoreCase))
                    {
                        slotType = "Utility";
                        if (slotLabel.StartsWith("Weapon", StringComparison.OrdinalIgnoreCase))
                        {
                            slotLabel = slotLabel.Replace("Weapon", "Utility");
                        }
                    }

                    // Disambiguate Radar vs Flight Blade in Avionics
                    if (IsKnownRadar(compLine))
                    {
                        slotType = "Avionics";
                        slotLabel = "Radar";
                    }

                    compLine = NormalizeComponentName(compLine);
                    result.Add(new ScannedShipComponent(slotType, slotLabel, compLine));
                }
            }
        }

        return result;
    }

    public static string NormalizeComponentName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;
        var name = raw.Trim();
        name = Regex.Replace(name, @"\[?\s*\(?\s*BRICKE[D\]I\)]*\s*\]?", "").Trim();
        name = Regex.Replace(name, @"[O0\]]$", "").Trim();
        name = Regex.Replace(name, @"[•·*]", "").Trim();

        // Specific component name OCR error corrections
        name = Regex.Replace(name, @"\bGin-zel\b", "Ginzel", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"\bChili-Max\b", "Chill-Max", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"\b5ca\s*'?akura['•·*]*", "5CA 'Akura'", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"\bFunstop\b", "FullStop", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"\bGirnbal\b", "Gimbal", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"\bVMP,uck\b", "VariPuck", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"\bvariPuck\b", "VariPuck");

        // Specific spec OCR distortion corrections
        name = Regex.Replace(name, @"\(Inci/MC\)", "(Ind/3/C)", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"\(Inci/(\d+)/([A-D])\)", "(Ind/$1/$2)", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"\(CiV/", "(Civ/");

        return name.Trim();
    }

    public static bool IsKnownRadar(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var lower = name.ToLowerInvariant();
        return lower.Contains("agrippa") ||
               lower.Contains("cassandra") ||
               lower.Contains("circe") ||
               lower.Contains("milvus") ||
               lower.Contains("lanner") ||
               lower.Contains("sparrow") ||
               lower.Contains("gyrfalcon") ||
               lower.Contains("echohawk") ||
               lower.Contains("nightshade") ||
               lower.Contains("fulgur") ||
               lower.Contains("predator") ||
               lower.Contains("spook") ||
               lower.Contains("perses") ||
               lower.Contains("tarsus") ||
               lower.Contains("radar");
    }

    /// <summary>
    /// Parst Komponenten aus dem übersichtlichen ASOP "LOADOUT ESTIMATE" Terminal-Popup (Versicherungs-/Loadout-Übersicht).
    /// </summary>
    private static List<ScannedShipComponent> ParseLoadoutEstimateComponents(List<string> lines)
    {
        var result = new List<ScannedShipComponent>();
        var noise = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LOADOUT ESTIMATE", "FLEET MANAGE", "FLEET MANAGER", "><", "NAME", "OTY", "QTY", "TYPE",
            "TOTAL ITEMS", "REPLACEMENT", "FEE", "FEE O", "COOLDOWN", "TIMER", "SUBMIT",
            "MEDIUM SALVAGE", "LIGHT FIGHTER", "MEDIUM FREIGHT", "HEAVY FIGHTER", "LIGHT FREIGHT",
            "HEAVY FREIGHT", "EXPLORATION", "MINING", "SALVAGE", "GUNSHIP", "CORVETTE", "CARRIER",
            "Misc.", "Fuse", "Flair", "Flair Item", "1", "2", "3", "4", "5", "6", "7", "8", "9"
        };

        int coolerCount = 0;
        int powerPlantCount = 0;
        int shieldCount = 0;
        int weaponCount = 0;
        int utilityCount = 0;

        foreach (var line in lines)
        {
            var clean = Regex.Replace(line, @"^[\s\-L><|•·\*\.]+\s*", "").Trim();
            if (clean.Length < 3 || noise.Contains(clean)) continue;
            if (Regex.IsMatch(clean, @"^[0-9:\. n¤,]+$")) continue;

            // Reine Spalten-/Typenbezeichner überspringen
            if (clean.Equals("Cooler", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Power Plant", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Quantum Drive", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Quantum Drives", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Jump Module", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Shield Generator", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Radar", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Scraper Beam", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Scraper Bearn", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Gun", StringComparison.OrdinalIgnoreCase) ||
                clean.Equals("Liveries", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Schiffsnamen oder Lackierungszeilen überspringen (werden separat erfasst)
            if (clean.StartsWith("ARGO", StringComparison.OrdinalIgnoreCase) ||
                clean.StartsWith("RSI", StringComparison.OrdinalIgnoreCase) ||
                clean.StartsWith("DRAKE", StringComparison.OrdinalIgnoreCase) ||
                clean.StartsWith("AEGIS", StringComparison.OrdinalIgnoreCase) ||
                clean.StartsWith("ANVIL", StringComparison.OrdinalIgnoreCase) ||
                clean.StartsWith("MISC", StringComparison.OrdinalIgnoreCase) ||
                clean.Contains("Livery", StringComparison.OrdinalIgnoreCase) ||
                clean.Contains("Paint", StringComparison.OrdinalIgnoreCase) ||
                clean.EndsWith("Rockwell", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string? slotType = null;
            string? slotLabel = null;

            // 1. Radar
            if (IsKnownRadar(clean))
            {
                slotType = "Avionics";
                slotLabel = "Radar";
            }
            // 2. Flight Blade
            else if (clean.Contains("Flight Blade", StringComparison.OrdinalIgnoreCase) || clean.Contains("Engine", StringComparison.OrdinalIgnoreCase))
            {
                if (result.Any(c => c.SlotLabel == "Flight Blade")) continue;
                slotType = "Avionics";
                slotLabel = "Flight Blade";
            }
            // 3. Jump Module
            else if (clean.Contains("Excelsior", StringComparison.OrdinalIgnoreCase) || clean.Contains("Jump Module", StringComparison.OrdinalIgnoreCase))
            {
                slotType = "QuantumDrive";
                slotLabel = "Jump Module";
            }
            // 4. Quantum Drive
            else if (clean.Contains("Huracan", StringComparison.OrdinalIgnoreCase) || clean.Contains("Bolt", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("Atlas", StringComparison.OrdinalIgnoreCase) || clean.Contains("Voyage", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("Crossfield", StringComparison.OrdinalIgnoreCase) || clean.Contains("Beacon", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("Siren", StringComparison.OrdinalIgnoreCase) || clean.Contains("Goliath", StringComparison.OrdinalIgnoreCase))
            {
                slotType = "QuantumDrive";
                slotLabel = "Quantum Drive";
            }
            // 5. Cooler
            else if (clean.Contains("Chill-Max", StringComparison.OrdinalIgnoreCase) || clean.Contains("Chili-Max", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("Aufeis", StringComparison.OrdinalIgnoreCase) || clean.Contains("Glacier", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("IceBox", StringComparison.OrdinalIgnoreCase) || clean.Contains("Polar", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("SnowPack", StringComparison.OrdinalIgnoreCase) || clean.Contains("ThermaMax", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("FrostStar", StringComparison.OrdinalIgnoreCase) || clean.Contains("Eco", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("Cooler", StringComparison.OrdinalIgnoreCase))
            {
                slotType = "Cooler";
                coolerCount++;
                slotLabel = coolerCount > 1 ? $"Cooler {coolerCount}" : "Cooler 1";
            }
            // 6. Power Plant
            else if (clean.Contains("Durango", StringComparison.OrdinalIgnoreCase) || clean.Contains("Ginzel", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("Gin-zel", StringComparison.OrdinalIgnoreCase) || clean.Contains("FullForce", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("QuadraCell", StringComparison.OrdinalIgnoreCase) || clean.Contains("SuperNova", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("Bolide", StringComparison.OrdinalIgnoreCase) || clean.Contains("Power Plant", StringComparison.OrdinalIgnoreCase))
            {
                slotType = "PowerPlant";
                powerPlantCount++;
                slotLabel = powerPlantCount > 1 ? $"Power Plant {powerPlantCount}" : "Power Plant 1";
            }
            // 7. Shield Generator
            else if (clean.Contains("Barbican", StringComparison.OrdinalIgnoreCase) || clean.Contains("Akura", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("FullStop", StringComparison.OrdinalIgnoreCase) || clean.Contains("Funstop", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("FR-", StringComparison.OrdinalIgnoreCase) || clean.Contains("Palisade", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("Rampart", StringComparison.OrdinalIgnoreCase) || clean.Contains("Mirage", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("AllStop", StringComparison.OrdinalIgnoreCase) || clean.Contains("STOP", StringComparison.OrdinalIgnoreCase))
            {
                slotType = "Shield";
                shieldCount++;
                slotLabel = shieldCount > 1 ? $"Shield Generator {shieldCount}" : "Shield Generator 1";
            }
            // 8. Utility (Scraper, Tractor, Mining)
            else if (clean.Contains("Scraper", StringComparison.OrdinalIgnoreCase) || clean.Contains("Abrade", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("Cinch", StringComparison.OrdinalIgnoreCase) || clean.Contains("Tractor", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("ReadyGrip", StringComparison.OrdinalIgnoreCase) || clean.Contains("Baier", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("Mining", StringComparison.OrdinalIgnoreCase))
            {
                slotType = "Utility";
                utilityCount++;
                slotLabel = $"Utility {utilityCount}";
            }
            // 9. Weapons (Guns, Repeaters, Cannons)
            else if (clean.Contains("Tarantula", StringComparison.OrdinalIgnoreCase) || clean.Contains("Badger", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("Rhino", StringComparison.OrdinalIgnoreCase) || clean.Contains("Panther", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("CF-", StringComparison.OrdinalIgnoreCase) || clean.Contains("Repeater", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("Cannon", StringComparison.OrdinalIgnoreCase) || clean.Contains("Gatling", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("GT-", StringComparison.OrdinalIgnoreCase) || clean.Contains("Laser", StringComparison.OrdinalIgnoreCase) ||
                     clean.Contains("Ballistic", StringComparison.OrdinalIgnoreCase))
            {
                slotType = "Weapon";
                weaponCount++;
                slotLabel = $"Weapon {weaponCount}";
            }

            if (slotType != null && slotLabel != null)
            {
                var normalized = NormalizeComponentName(clean);
                result.Add(new ScannedShipComponent(slotType, slotLabel, normalized));
            }
        }

        return result;
    }

    /// <summary>
    /// Speichert ein Bild aus der Windows-Zwischenablage als temporäre Datei auf der Festplatte.
    /// </summary>
    public static async Task<string?> SaveClipboardImageToFileAsync()
    {
        try
        {
            var content = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
            if (content.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Bitmap))
            {
                var bitmapRef = await content.GetBitmapAsync();
                using var stream = await bitmapRef.OpenReadAsync();
                var decoder = await BitmapDecoder.CreateAsync(stream);
                using var sBmp = await decoder.GetSoftwareBitmapAsync();

                var tempPath = Path.Combine(Path.GetTempPath(), $"sclogmate_clipboard_{DateTime.UtcNow.Ticks}.png");
                using var fileStream = File.Create(tempPath);
                var randomAccessStream = fileStream.AsRandomAccessStream();
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, randomAccessStream);
                encoder.SetSoftwareBitmap(sBmp);
                await encoder.FlushAsync();
                return tempPath;
            }
        }
        catch (Exception ex)
        {
            Logger.Error("SaveClipboardImageToFileAsync", ex);
        }
        return null;
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
