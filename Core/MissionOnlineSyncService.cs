using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SCLogMate.Core;

public sealed record TwoWaySyncResult(int Pushed, int Pulled, bool Success, string Message);

public static class MissionOnlineSyncService
{
    private const string BaseApiUrl = "https://scverse.de/api/missions";
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private static readonly System.Threading.Lock _syncLock = new();
    private static bool _cacheLoaded;

    public static event Action? MissionsSynchronized;
    public static bool IsSyncing { get; private set; }
    public static DateTime? LastSyncUtc { get; private set; }

    private static string CacheFilePath => Path.Combine(Settings.Dir, "missions_online.json");

    /// <summary>
    /// Stellt sicher, dass der lokale Offline-Cache in den Katalog eingelesen wurde.
    /// </summary>
    public static void EnsureCacheLoaded()
    {
        if (_cacheLoaded) return;
        _cacheLoaded = true;
        try
        {
            var cachedCount = MissionCatalog.LoadOnlineCache(CacheFilePath);
            if (cachedCount > 0)
            {
                Logger.Log($"[MissionOnlineSync] {cachedCount} lokal zwischengespeicherte Online-Missionen geladen.");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"[MissionOnlineSync] Fehler beim Laden des Caches: {ex.Message}");
        }
    }

    /// <summary>
    /// Führt eine vollständige bidirektionale Synchronisation (Two-Way Sync) durch:
    /// 1. Lokale Missionsdaten & entdeckte Belohnungen an die SCVerse Cloud senden (Push)
    /// 2. Neue & aktualisierte Missionen aus der SCVerse Cloud herunterladen (Pull)
    /// </summary>
    public static async Task<TwoWaySyncResult> TwoWaySyncAsync(bool force = false)
    {
        EnsureCacheLoaded();

        lock (_syncLock)
        {
            if (IsSyncing)
            {
                return new TwoWaySyncResult(0, 0, false, "Synchronisation läuft bereits...");
            }
            IsSyncing = true;
        }

        try
        {
            // Schritt 1: Push lokaler Missionen mit Belohnungen zur SCVerse Cloud
            int pushed = await PushLocalMissionsAsync().ConfigureAwait(false);

            // Schritt 2: Pull neuer & aktualisierter Missionen aus der SCVerse Cloud
            int pulled = await PullCloudMissionsAsync(force).ConfigureAwait(false);

            if (pushed > 0 || pulled > 0)
            {
                SaveCacheFile();
                MissionsSynchronized?.Invoke();
            }

            string msg = (pushed > 0 || pulled > 0)
                ? $"✓ SCVerse Sync: {pushed} gesendet, {pulled} aktualisiert"
                : "✓ Missionskatalog ist bereits auf dem neuesten Stand";

            return new TwoWaySyncResult(pushed, pulled, true, msg);
        }
        catch (Exception ex)
        {
            Logger.Log($"[MissionOnlineSync] Fehler bei 2-Way Sync: {ex.Message}");
            return new TwoWaySyncResult(0, 0, false, $"Fehler: {ex.Message}");
        }
        finally
        {
            lock (_syncLock)
            {
                IsSyncing = false;
            }
        }
    }

    /// <summary>
    /// Startet den bidirektionalen Sync und liefert die Summe übertragener Einträge.
    /// </summary>
    public static async Task<int> SyncCatalogAsync(bool force = false)
    {
        var res = await TwoWaySyncAsync(force).ConfigureAwait(false);
        return res.Pulled + res.Pushed;
    }

    /// <summary>
    /// Sendet alle lokal bekannten Missionen mit Belohnungswert in Batches an die SCVerse Cloud (Push).
    /// </summary>
    public static async Task<int> PushLocalMissionsAsync()
    {
        try
        {
            var missionsToPush = MissionCatalog.AllMissions
                .Where(m => !string.IsNullOrWhiteSpace(m.Title) && m.BaseReward > 0)
                .ToList();

            if (missionsToPush.Count == 0) return 0;

            int totalUploaded = 0;
            const int chunkSize = 150;

            for (int i = 0; i < missionsToPush.Count; i += chunkSize)
            {
                var chunk = missionsToPush.Skip(i).Take(chunkSize).Select(m => new
                {
                    title = m.Title,
                    baseReward = m.BaseReward,
                    contractor = m.Contractor,
                    faction = m.Faction,
                    missionType = m.MissionType,
                    contractFee = m.ContractFee,
                    reputationGain = m.ReputationGain,
                    isIllegal = m.IsIllegal,
                    starSystems = m.StarSystems,
                    description = m.Description,
                    gameVersion = "4.10.2"
                }).ToList();

                var payload = new { missions = chunk };
                var json = JsonSerializer.Serialize(payload);
                using var req = new HttpRequestMessage(HttpMethod.Post, BaseApiUrl)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                req.Headers.Add("User-Agent", "SCLogMate-Sync/1.4.9");

                var resp = await _httpClient.SendAsync(req).ConfigureAwait(false);
                if (resp.IsSuccessStatusCode)
                {
                    totalUploaded += chunk.Count;
                }
                else
                {
                    Logger.Log($"[MissionOnlineSync] Push Chunk {i / chunkSize + 1} Status {(int)resp.StatusCode}");
                }
            }

            if (totalUploaded > 0)
            {
                Logger.Log($"[MissionOnlineSync] {totalUploaded} lokale Missionen an SCVerse Cloud übertragen.");
            }
            return totalUploaded;
        }
        catch (Exception ex)
        {
            Logger.Log($"[MissionOnlineSync] Push-Fehler: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Lädt neue/geänderte Missionen aus der SCVerse Cloud herunter (Pull).
    /// </summary>
    public static async Task<int> PullCloudMissionsAsync(bool force = false)
    {
        Database.EnsureInitialized();
        var lastSyncStr = Database.GetMeta("missions_online_last_sync");
        DateTime lastSyncTime = DateTime.MinValue;
        if (!string.IsNullOrWhiteSpace(lastSyncStr) && DateTime.TryParse(lastSyncStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedTime))
        {
            lastSyncTime = parsedTime;
            LastSyncUtc = parsedTime;
        }

        // Wenn nicht erzwungen und die letzte Synchronisation weniger als 30 Minuten her ist: überspringen
        if (!force && lastSyncTime > DateTime.MinValue && (DateTime.UtcNow - lastSyncTime).TotalMinutes < 30)
        {
            return 0;
        }

        var requestUrl = BaseApiUrl;
        if (lastSyncTime > DateTime.MinValue && !force)
        {
            requestUrl += $"?since={Uri.EscapeDataString(lastSyncTime.ToString("o"))}&limit=2000";
        }
        else
        {
            requestUrl += "?limit=2000";
        }

        using var req = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        req.Headers.Add("User-Agent", "SCLogMate-Sync/1.4.9");

        var response = await _httpClient.SendAsync(req).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            Logger.Log($"[MissionOnlineSync] SCVerse API Status {(int)response.StatusCode} ({response.ReasonPhrase})");
            return 0;
        }

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("missions", out var missionsArr) || missionsArr.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        var syncedAt = root.TryGetProperty("syncedAt", out var sProp) ? sProp.GetString() ?? DateTime.UtcNow.ToString("o") : DateTime.UtcNow.ToString("o");

        int updatedCount = 0;

        foreach (var el in missionsArr.EnumerateArray())
        {
            var title = el.TryGetProperty("title", out var tProp) ? tProp.GetString() ?? "" : "";
            if (string.IsNullOrWhiteSpace(title)) continue;

            var id = el.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
            var contractor = el.TryGetProperty("contractor", out var cProp) ? cProp.GetString() ?? "Unbekannt" : "Unbekannt";
            var faction = el.TryGetProperty("faction", out var fProp) ? fProp.GetString() ?? contractor : contractor;
            var type = el.TryGetProperty("missionType", out var mProp) ? mProp.GetString() ?? "Auftrag" : "Auftrag";
            var reward = el.TryGetProperty("baseReward", out var rProp) && rProp.TryGetInt32(out var rVal) ? rVal : 0;
            var fee = el.TryGetProperty("contractFee", out var feeProp) && feeProp.TryGetInt32(out var feeVal) ? feeVal : 0;
            var rep = el.TryGetProperty("reputationGain", out var repProp) && repProp.TryGetInt32(out var repVal) ? repVal : 0;
            var illegal = el.TryGetProperty("isIllegal", out var illProp) && illProp.GetBoolean();
            var sys = el.TryGetProperty("starSystems", out var sysProp) ? sysProp.GetString() ?? "Stanton" : "Stanton";
            var desc = el.TryGetProperty("description", out var dProp) ? dProp.GetString() ?? "" : "";

            var info = new MissionInfo
            {
                Id = !string.IsNullOrWhiteSpace(id) ? id : ("online_" + Guid.NewGuid().ToString("N")),
                Title = title,
                Contractor = contractor,
                Faction = faction,
                MissionType = type,
                BaseReward = reward,
                ContractFee = fee,
                ReputationGain = rep,
                IsIllegal = illegal,
                StarSystems = sys,
                Description = desc
            };

            MissionCatalog.RegisterOrUpdate(info);
            updatedCount++;

            // Retroaktive Reconciliation lokaler Events, falls ein neuer Betrag vorhanden ist
            if (reward > 0)
            {
                Database.UpdateMissionRewardInEvents(title, reward);
            }
        }

        if (updatedCount > 0)
        {
            Database.SetMeta("missions_online_last_sync", syncedAt);
            LastSyncUtc = DateTime.UtcNow;
            Logger.Log($"[MissionOnlineSync] {updatedCount} Missionen erfolgreich aus SCVerse heruntergeladen.");
        }

        return updatedCount;
    }

    /// <summary>
    /// Meldet einen neu entdeckten oder korrigierten Missions-Belohnungswert an die zentrale SCVerse Cloud.
    /// Aktualisiert gleichzeitig sofort den lokalen Katalog und die Events-Datenbank.
    /// </summary>
    public static async Task<bool> ReportMissionRewardAsync(
        string title,
        int reward,
        string? contractor = null,
        string? faction = null,
        string? missionType = null)
    {
        if (string.IsNullOrWhiteSpace(title) || reward <= 0) return false;

        try
        {
            // 1. Sofort lokal registrieren
            var localInfo = new MissionInfo
            {
                Id = "custom_" + Guid.NewGuid().ToString("N"),
                Title = title.Trim(),
                Contractor = contractor ?? "Unbekannt",
                Faction = faction ?? (contractor ?? "Unbekannt"),
                MissionType = missionType ?? "Auftrag",
                BaseReward = reward,
                StarSystems = "Stanton"
            };
            MissionCatalog.RegisterOrUpdate(localInfo);
            Database.UpdateMissionRewardInEvents(title, reward);
            SaveCacheFile();
            MissionsSynchronized?.Invoke();

            // 2. An SCVerse Cloud senden (Push)
            var payload = new
            {
                title = title.Trim(),
                baseReward = reward,
                contractor = contractor?.Trim(),
                faction = faction?.Trim(),
                missionType = missionType?.Trim(),
                gameVersion = "4.10.2"
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var req = new HttpRequestMessage(HttpMethod.Post, BaseApiUrl) { Content = jsonContent };
            req.Headers.Add("User-Agent", "SCLogMate-Sync/1.4.9");

            var response = await _httpClient.SendAsync(req).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                Logger.Log($"[MissionOnlineSync] Belohnung für '{title}' ({reward:N0} aUEC) erfolgreich an SCVerse übermittelt.");
                return true;
            }

            Logger.Log($"[MissionOnlineSync] Übermittlung an SCVerse fehlgeschlagen: {(int)response.StatusCode}");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Log($"[MissionOnlineSync] Fehler beim Melden der Missionsbelohnung: {ex.Message}");
            return false;
        }
    }

    private static void SaveCacheFile()
    {
        try
        {
            var dir = Path.GetDirectoryName(CacheFilePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var list = MissionCatalog.AllMissions.Select(m => new
            {
                m.Id,
                m.Title,
                m.Contractor,
                m.Faction,
                m.MissionType,
                m.BaseReward,
                m.ContractFee,
                m.ReputationGain,
                m.IsIllegal,
                m.StarSystems,
                m.Description
            }).ToList();

            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(CacheFilePath, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Logger.Log($"[MissionOnlineSync] Fehler beim Speichern des Caches: {ex.Message}");
        }
    }
}
