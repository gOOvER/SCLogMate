using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SCLogMate.Core;

public static class MissionOnlineSyncService
{
    private const string BaseApiUrl = "https://scverse.de/api/missions";
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(15) };
    private static readonly System.Threading.Lock _syncLock = new();
    private static bool _cacheLoaded;

    public static event Action? MissionsSynchronized;
    public static bool IsSyncing { get; private set; }
    public static DateTime? LastSyncUtc { get; private set; }

    private static string CacheFilePath => Path.Combine(Settings.Dir, "missions_online.json");

    /// <summary>
    /// Lädt den lokalen Cache und stößt die Online-Synchronisation mit der zentralen SCVerse Missionsdatenbank an.
    /// Läuft non-blocking im Hintergrund.
    /// </summary>
    public static async Task<int> SyncCatalogAsync(bool force = false)
    {
        // 1. Lokalen Offline-Cache sofort in den Katalog einlesen
        if (!_cacheLoaded)
        {
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

        lock (_syncLock)
        {
            if (IsSyncing) return 0;
            IsSyncing = true;
        }

        try
        {
            Database.EnsureInitialized();
            var lastSyncStr = Database.GetMeta("missions_online_last_sync");
            DateTime lastSyncTime = DateTime.MinValue;
            if (!string.IsNullOrWhiteSpace(lastSyncStr) && DateTime.TryParse(lastSyncStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsedTime))
            {
                lastSyncTime = parsedTime;
                LastSyncUtc = parsedTime;
            }

            // Wenn nicht erzwungen und die letzte Synchronisation weniger als 60 Minuten her ist: überspringen
            if (!force && lastSyncTime > DateTime.MinValue && (DateTime.UtcNow - lastSyncTime).TotalMinutes < 60)
            {
                return 0;
            }

            var requestUrl = BaseApiUrl;
            if (lastSyncTime > DateTime.MinValue)
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
            var updatedList = new List<MissionInfo>();

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
                updatedList.Add(info);
                updatedCount++;

                // Retroaktive Reconciliation lokaler Events, falls ein neuer Betrag vorhanden ist
                if (reward > 0)
                {
                    Database.UpdateMissionRewardInEvents(title, reward);
                }
            }

            if (updatedCount > 0)
            {
                // Lokalen Cache aktualisieren
                SaveCacheFile();
                Database.SetMeta("missions_online_last_sync", syncedAt);
                LastSyncUtc = DateTime.UtcNow;

                Logger.Log($"[MissionOnlineSync] {updatedCount} Missionen erfolgreich aus SCVerse synchronisiert.");
                MissionsSynchronized?.Invoke();
            }

            return updatedCount;
        }
        catch (Exception ex)
        {
            Logger.Log($"[MissionOnlineSync] Netzwerk- oder Synchronisationsfehler: {ex.Message}");
            return 0;
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

            // 2. An SCVerse Cloud senden
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
