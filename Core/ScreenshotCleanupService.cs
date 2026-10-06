using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SCLogMate.Core.Ocr;
using SCLogMate.Models;

namespace SCLogMate.Core;

public sealed record ScreenshotItemDto(
    string FileName,
    string FilePath,
    long FileSizeBytes,
    string SizeFormatted,
    DateTime LastModified,
    string LastModifiedFormatted,
    string Category,      // "loadout", "reputation", "contract", "blank", "other", "unscanned"
    string CategoryLabel, // "Schiffsausrüstung", "Ruf / Delphi", "Auftrag", "Leerer Frame / HDR-Bug", "Sonstiges"
    string Details        // z.B. "ARGO MOTH (Rockwell Livery)", "Recco Battaglia · Prestige 2"
);

public sealed record ScreenshotCleanupStatusDto(
    string FolderPath,
    bool FolderExists,
    int TotalCount,
    double TotalSizeMb,
    string TotalSizeFormatted,
    int LoadoutCount,
    double LoadoutSizeMb,
    int ReputationCount,
    double ReputationSizeMb,
    int ContractCount,
    double ContractSizeMb,
    int BlankCount,
    double BlankSizeMb,
    int OtherCount,
    double OtherSizeMb,
    IReadOnlyList<ScreenshotItemDto> Items
);

public sealed record ScreenshotDeleteResultDto(
    bool Success,
    int DeletedCount,
    long FreedBytes,
    string FreedSizeFormatted,
    string Message,
    ScreenshotCleanupStatusDto Status
);

/// <summary>
/// Verwaltet und bereinigt den Star Citizen Screenshot-Ordner (ScreenShots/).
/// Erkennt und kategorisiert Screenshots nach Schiffsausrüstung (VLM/ASOP),
/// Ruf / Delphi (mobiGlas Reputation), Aufträgen, fehlerhaften/leeren Frames (HDR-Bug) und sonstigen Aufnahmen.
/// </summary>
public sealed class ScreenshotCleanupService
{
    private readonly OcrEngineService _ocr;
    private readonly ConcurrentDictionary<string, CachedClassification> _cache = new(StringComparer.OrdinalIgnoreCase);

    private sealed record CachedClassification(
        long FileSizeBytes,
        long LastWriteTicks,
        string Category,
        string CategoryLabel,
        string Details
    );

    public ScreenshotCleanupService(OcrEngineService ocr)
    {
        _ocr = ocr;
    }

    /// <summary>
    /// Ermittelt den konfigurierten oder automatisch erkannten Screenshot-Ordner von Star Citizen.
    /// </summary>
    public string? ResolveScreenshotFolder(string? customFolder = null)
    {
        if (!string.IsNullOrWhiteSpace(customFolder) && Directory.Exists(customFolder))
            return customFolder;

        var detected = ScreenshotLoadoutWatcher.DetectStarCitizenScreenshotFolder();
        if (!string.IsNullOrWhiteSpace(detected) && Directory.Exists(detected))
            return detected;

        return null;
    }

    /// <summary>
    /// Ermittelt den aktuellen Status des Screenshot-Ordners und klassifiziert die enthaltenen Screenshots.
    /// </summary>
    public async Task<ScreenshotCleanupStatusDto> GetStatusAsync(string? folderPath = null, bool autoScan = true)
    {
        var targetFolder = ResolveScreenshotFolder(folderPath);
        if (string.IsNullOrWhiteSpace(targetFolder) || !Directory.Exists(targetFolder))
        {
            return new ScreenshotCleanupStatusDto(
                FolderPath: targetFolder ?? "Nicht gefunden",
                FolderExists: false,
                TotalCount: 0,
                TotalSizeMb: 0,
                TotalSizeFormatted: "0 MB",
                LoadoutCount: 0,
                LoadoutSizeMb: 0,
                ReputationCount: 0,
                ReputationSizeMb: 0,
                ContractCount: 0,
                ContractSizeMb: 0,
                BlankCount: 0,
                BlankSizeMb: 0,
                OtherCount: 0,
                OtherSizeMb: 0,
                Items: Array.Empty<ScreenshotItemDto>()
            );
        }

        var dirInfo = new DirectoryInfo(targetFolder);
        var files = dirInfo.EnumerateFiles("*.*", SearchOption.TopDirectoryOnly)
            .Where(f => f.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        f.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                        f.Extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .ToList();

        var items = new List<ScreenshotItemDto>(files.Count);

        foreach (var fi in files)
        {
            var classification = await GetOrClassifyFileAsync(fi, autoScan);
            items.Add(new ScreenshotItemDto(
                FileName: fi.Name,
                FilePath: fi.FullName,
                FileSizeBytes: fi.Length,
                SizeFormatted: FormatBytes(fi.Length),
                LastModified: fi.LastWriteTime,
                LastModifiedFormatted: fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm:ss"),
                Category: classification.Category,
                CategoryLabel: classification.CategoryLabel,
                Details: classification.Details
            ));
        }

        int loadoutCount = items.Count(i => i.Category == "loadout");
        long loadoutBytes = items.Where(i => i.Category == "loadout").Sum(i => i.FileSizeBytes);

        int repCount = items.Count(i => i.Category == "reputation");
        long repBytes = items.Where(i => i.Category == "reputation").Sum(i => i.FileSizeBytes);

        int contractCount = items.Count(i => i.Category == "contract");
        long contractBytes = items.Where(i => i.Category == "contract").Sum(i => i.FileSizeBytes);

        int blankCount = items.Count(i => i.Category == "blank");
        long blankBytes = items.Where(i => i.Category == "blank").Sum(i => i.FileSizeBytes);

        int otherCount = items.Count(i => i.Category == "other" || i.Category == "unscanned");
        long otherBytes = items.Where(i => i.Category == "other" || i.Category == "unscanned").Sum(i => i.FileSizeBytes);

        long totalBytes = items.Sum(i => i.FileSizeBytes);

        return new ScreenshotCleanupStatusDto(
            FolderPath: targetFolder,
            FolderExists: true,
            TotalCount: items.Count,
            TotalSizeMb: Math.Round((double)totalBytes / (1024 * 1024), 2),
            TotalSizeFormatted: FormatBytes(totalBytes),
            LoadoutCount: loadoutCount,
            LoadoutSizeMb: Math.Round((double)loadoutBytes / (1024 * 1024), 2),
            ReputationCount: repCount,
            ReputationSizeMb: Math.Round((double)repBytes / (1024 * 1024), 2),
            ContractCount: contractCount,
            ContractSizeMb: Math.Round((double)contractBytes / (1024 * 1024), 2),
            BlankCount: blankCount,
            BlankSizeMb: Math.Round((double)blankBytes / (1024 * 1024), 2),
            OtherCount: otherCount,
            OtherSizeMb: Math.Round((double)otherBytes / (1024 * 1024), 2),
            Items: items
        );
    }

    /// <summary>
    /// Löscht Screenshots basierend auf Modus oder Dateiliste.
    /// Modus: "all", "reputation", "loadout", "contract", "blank", "other", "selected"
    /// </summary>
    public async Task<ScreenshotDeleteResultDto> DeleteScreenshotsAsync(
        string mode,
        IReadOnlyList<string>? filePaths = null,
        string? folderPath = null)
    {
        var targetFolder = ResolveScreenshotFolder(folderPath);
        if (string.IsNullOrWhiteSpace(targetFolder) || !Directory.Exists(targetFolder))
        {
            return new ScreenshotDeleteResultDto(
                Success: false,
                DeletedCount: 0,
                FreedBytes: 0,
                FreedSizeFormatted: "0 B",
                Message: "Screenshot-Ordner nicht gefunden.",
                Status: await GetStatusAsync(folderPath, autoScan: false)
            );
        }

        // Zuerst aktuellen Status mit Klassifizierungen laden
        var currentStatus = await GetStatusAsync(targetFolder, autoScan: false);
        var filesToDelete = new List<ScreenshotItemDto>();

        var normalizedMode = (mode ?? "").Trim().ToLowerInvariant();
        switch (normalizedMode)
        {
            case "all":
                filesToDelete.AddRange(currentStatus.Items);
                break;

            case "reputation":
            case "ruf":
                filesToDelete.AddRange(currentStatus.Items.Where(i => i.Category == "reputation"));
                break;

            case "loadout":
            case "schiffsausrüstung":
            case "schiffe":
                filesToDelete.AddRange(currentStatus.Items.Where(i => i.Category == "loadout"));
                break;

            case "contract":
            case "contracts":
            case "aufträge":
                filesToDelete.AddRange(currentStatus.Items.Where(i => i.Category == "contract"));
                break;

            case "blank":
            case "error":
            case "leere":
                filesToDelete.AddRange(currentStatus.Items.Where(i => i.Category == "blank"));
                break;

            case "other":
            case "sonstige":
                filesToDelete.AddRange(currentStatus.Items.Where(i => i.Category == "other"));
                break;

            case "selected":
                if (filePaths != null && filePaths.Count > 0)
                {
                    var pathSet = new HashSet<string>(filePaths, StringComparer.OrdinalIgnoreCase);
                    filesToDelete.AddRange(currentStatus.Items.Where(i => pathSet.Contains(i.FilePath)));
                }
                break;

            default:
                return new ScreenshotDeleteResultDto(
                    Success: false,
                    DeletedCount: 0,
                    FreedBytes: 0,
                    FreedSizeFormatted: "0 B",
                    Message: $"Unbekannter Löschmodus: '{mode}'. Erlaubt: all, reputation, loadout, contract, blank, other, selected.",
                    Status: currentStatus
                );
        }

        if (filesToDelete.Count == 0)
        {
            return new ScreenshotDeleteResultDto(
                Success: true,
                DeletedCount: 0,
                FreedBytes: 0,
                FreedSizeFormatted: "0 B",
                Message: "Keine Screenshots für die ausgewählte Kategorie zum Löschen vorhanden.",
                Status: currentStatus
            );
        }

        int deletedCount = 0;
        long freedBytes = 0;
        var failedFiles = new List<string>();

        foreach (var item in filesToDelete)
        {
            try
            {
                if (File.Exists(item.FilePath))
                {
                    File.Delete(item.FilePath);
                    freedBytes += item.FileSizeBytes;
                    deletedCount++;
                }
                _cache.TryRemove(item.FilePath, out _);
            }
            catch (Exception ex)
            {
                Logger.Log($"Fehler beim Löschen von Screenshot '{item.FilePath}': {ex.Message}");
                failedFiles.Add(item.FileName);
            }
        }

        // Aktualisierten Status abrufen
        var newStatus = await GetStatusAsync(targetFolder, autoScan: false);

        string label = normalizedMode switch
        {
            "all" => "alle Screenshots",
            "reputation" or "ruf" => "Ruf-Screenshots (mobiGlas Delphi)",
            "loadout" or "schiffsausrüstung" => "Schiffsausrüstungs-Screenshots (VLM / ASOP)",
            "contract" or "contracts" or "aufträge" => "Auftrags-Screenshots (mobiGlas Contracts)",
            "blank" or "error" or "leere" => "leere / fehlerhafte Frames (HDR-Bug)",
            "other" or "sonstige" => "sonstige Screenshots",
            _ => $"{deletedCount} ausgewählte Screenshots"
        };

        string msg = failedFiles.Count == 0
            ? $"✓ {deletedCount} {label} erfolgreich gelöscht ({FormatBytes(freedBytes)} freigegeben)."
            : $"✓ {deletedCount} {label} gelöscht ({FormatBytes(freedBytes)} frei), {failedFiles.Count} Datei(en) gesperrt.";

        return new ScreenshotDeleteResultDto(
            Success: deletedCount > 0 || failedFiles.Count == 0,
            DeletedCount: deletedCount,
            FreedBytes: freedBytes,
            FreedSizeFormatted: FormatBytes(freedBytes),
            Message: msg,
            Status: newStatus
        );
    }

    /// <summary>
    /// Öffnet den Screenshot-Ordner im Windows Explorer.
    /// </summary>
    public bool OpenFolderInExplorer(string? folderPath = null)
    {
        var target = ResolveScreenshotFolder(folderPath);
        if (string.IsNullOrWhiteSpace(target) || !Directory.Exists(target)) return false;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"OpenFolderInExplorer '{target}'", ex);
            return false;
        }
    }

    private async Task<CachedClassification> GetOrClassifyFileAsync(FileInfo fi, bool autoScan)
    {
        // 1. Cache-Prüfung nach Pfad + Modifikationszeit + Dateigröße
        if (_cache.TryGetValue(fi.FullName, out var cached))
        {
            if (cached.FileSizeBytes == fi.Length && cached.LastWriteTicks == fi.LastWriteTimeUtc.Ticks)
            {
                return cached;
            }
        }

        // 2. Star Citizen 101 KB HDR-Capture Bug erkennen
        // (Reine schwarze Frames, die Star Citizen bei aktiver HDR-/Fullscreen-Aufnahme erzeugt)
        if (fi.Length == 101411)
        {
            var blank = new CachedClassification(
                FileSizeBytes: fi.Length,
                LastWriteTicks: fi.LastWriteTimeUtc.Ticks,
                Category: "blank",
                CategoryLabel: "Leerer Frame (HDR-Bug)",
                Details: "Schwarzer 2560x1440 Frame (Star Citizen HDR-Capture Bug)"
            );
            _cache[fi.FullName] = blank;
            return blank;
        }

        if (!autoScan)
        {
            return new CachedClassification(
                FileSizeBytes: fi.Length,
                LastWriteTicks: fi.LastWriteTimeUtc.Ticks,
                Category: "unscanned",
                CategoryLabel: "Nicht analysiert",
                Details: "Ausstehend"
            );
        }

        // 3. OCR-Analyse durchführen
        try
        {
            if (!_ocr.IsAvailable)
            {
                var unscanned = new CachedClassification(fi.Length, fi.LastWriteTimeUtc.Ticks, "other", "Sonstiges", "OCR nicht verfügbar");
                _cache[fi.FullName] = unscanned;
                return unscanned;
            }

            string? ocrText = await _ocr.RecognizeImageFileAsync(fi.FullName);
            if (string.IsNullOrWhiteSpace(ocrText))
            {
                var empty = new CachedClassification(fi.Length, fi.LastWriteTimeUtc.Ticks, "other", "Sonstiges", "Kein lesbarer Text erkannt");
                _cache[fi.FullName] = empty;
                return empty;
            }

            // A) Schiffsloadout / VLM / ASOP prüfen
            var loadout = ScreenshotLoadoutWatcher.ParseLoadoutFromText(ocrText, fi.FullName);
            if (loadout.Success && (!string.IsNullOrWhiteSpace(loadout.ShipName) || (loadout.Components != null && loadout.Components.Count > 0) || !string.IsNullOrWhiteSpace(loadout.Livery)))
            {
                int compCount = loadout.Components?.Count ?? 0;
                string ship = loadout.ShipName ?? "Unbekanntes Schiff";
                string livery = !string.IsNullOrWhiteSpace(loadout.Livery) ? $" · {loadout.Livery}" : "";
                string details = compCount > 0 ? $"{ship} ({compCount} Komponenten){livery}" : $"{ship}{livery}";

                var res = new CachedClassification(fi.Length, fi.LastWriteTimeUtc.Ticks, "loadout", "Schiffsausrüstung", details);
                _cache[fi.FullName] = res;
                return res;
            }

            // B) Ruf / Delphi prüfen
            if (IsReputationText(ocrText, out string repDetails))
            {
                var res = new CachedClassification(fi.Length, fi.LastWriteTimeUtc.Ticks, "reputation", "Ruf / Delphi", repDetails);
                _cache[fi.FullName] = res;
                return res;
            }

            // C) Aufträge / Contract Manager prüfen
            var contract = ContractParser.Parse(ocrText, requireAccepted: false);
            if (contract != null && contract.Reward > 0 && !string.IsNullOrWhiteSpace(contract.Title))
            {
                string details = $"{contract.Title} ({contract.Reward:N0} aUEC)";
                var res = new CachedClassification(fi.Length, fi.LastWriteTimeUtc.Ticks, "contract", "Auftrag", details);
                _cache[fi.FullName] = res;
                return res;
            }

            if (ocrText.Contains("CONTRACTS", StringComparison.OrdinalIgnoreCase) ||
                ocrText.Contains("CONTRACT MANAGER", StringComparison.OrdinalIgnoreCase) ||
                ocrText.Contains("MISSION REWARD", StringComparison.OrdinalIgnoreCase))
            {
                var res = new CachedClassification(fi.Length, fi.LastWriteTimeUtc.Ticks, "contract", "Auftrag", "mobiGlas Auftragsmanager");
                _cache[fi.FullName] = res;
                return res;
            }

            // D) Sonstiger Screenshot
            string firstLine = ocrText.Split('\n')
                .Select(l => l.Trim())
                .FirstOrDefault(l => l.Length > 2) ?? "Star Citizen Spiel-Screenshot";

            if (firstLine.Length > 50) firstLine = firstLine.Substring(0, 47) + "...";

            var other = new CachedClassification(fi.Length, fi.LastWriteTimeUtc.Ticks, "other", "Sonstiges", firstLine);
            _cache[fi.FullName] = other;
            return other;
        }
        catch (Exception ex)
        {
            Logger.Log($"Screenshot-Klassifizierungsfehler für {fi.Name}: {ex.Message}");
            var fallback = new CachedClassification(fi.Length, fi.LastWriteTimeUtc.Ticks, "other", "Sonstiges", "Analysefehler");
            _cache[fi.FullName] = fallback;
            return fallback;
        }
    }

    private static bool IsReputationText(string text, out string details)
    {
        details = "Delphi Ruf & Faktionen";

        bool hasDelphi = text.Contains("DELPHI", StringComparison.OrdinalIgnoreCase);
        bool hasReputation = text.Contains("REPUTATION", StringComparison.OrdinalIgnoreCase);
        bool hasStanding = text.Contains("STANDING", StringComparison.OrdinalIgnoreCase) ||
                           text.Contains("AFFILIATION", StringComparison.OrdinalIgnoreCase) ||
                           text.Contains("ORGANIZATION", StringComparison.OrdinalIgnoreCase);

        // Bekannte Faktionen oder Missionsgeber
        string[] factions =
        {
            "RECCO BATTAGLIA", "BOUNTY HUNTERS GUILD", "RED WIND", "LING FAMILY", "INTERSEC",
            "SHUBIN INTERSTELLAR", "UNITED WAYFARERS", "NORTHROCK", "CRUSADER SECURITY",
            "HURSTON DYNAMICS", "HURSTON SECURITY", "MICROTECH PROTECTION", "BLACJAC",
            "CIVILIAN DEFENSE FORCE", "TWITCH PACHECO", "RUTO", "CLOVIS DARNEELY", "WALLACE KLIM",
            "MILES ECKHART", "VAUGHN", "CITIZENS FOR PROSPERITY", "PEOPLE'S ALLIANCE", "FOXWELL"
        };

        string? matchedFaction = factions.FirstOrDefault(f => text.Contains(f, StringComparison.OrdinalIgnoreCase));

        // Ränge / Prestige
        bool hasRank = text.Contains("PRESTIGE", StringComparison.OrdinalIgnoreCase) ||
                       text.Contains("ASSOCIATE", StringComparison.OrdinalIgnoreCase) ||
                       text.Contains("SENIOR", StringComparison.OrdinalIgnoreCase) ||
                       text.Contains("VETERAN", StringComparison.OrdinalIgnoreCase) ||
                       text.Contains("MASTER", StringComparison.OrdinalIgnoreCase);

        if (hasDelphi || (hasReputation && hasStanding) || (matchedFaction != null && (hasReputation || hasStanding || hasRank)))
        {
            if (matchedFaction != null)
            {
                if (text.Contains("PRESTIGE 1", StringComparison.OrdinalIgnoreCase))
                    details = $"{matchedFaction} · Prestige 1";
                else if (text.Contains("PRESTIGE 2", StringComparison.OrdinalIgnoreCase))
                    details = $"{matchedFaction} · Prestige 2";
                else if (text.Contains("PRESTIGE 3", StringComparison.OrdinalIgnoreCase))
                    details = $"{matchedFaction} · Prestige 3";
                else if (text.Contains("TRUSTED", StringComparison.OrdinalIgnoreCase))
                    details = $"{matchedFaction} · Trusted Associate";
                else if (text.Contains("ASSOCIATE", StringComparison.OrdinalIgnoreCase))
                    details = $"{matchedFaction} · Associate";
                else
                    details = $"{matchedFaction} · Delphi Ruf";
            }
            return true;
        }

        return false;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1024L * 1024L * 1024L)
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        if (bytes >= 1024L * 1024L)
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        if (bytes >= 1024L)
            return $"{bytes / 1024.0:F0} KB";
        return $"{bytes} B";
    }
}
