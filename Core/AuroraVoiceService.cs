using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SCLogMate.Models;

namespace SCLogMate.Core;


public class AuroraVoiceService : IDisposable
{
    private readonly object _lock = new();
    private readonly Random _rand = new();
    private string? _auroraDir;
    private bool _isInstalled;
    private int _volume = 40; // 0 to 100
    private bool _isEnabled = true;

    // Category Toggles (managed in SCLogMate settings)
    public bool ShipGreetingsEnabled { get; set; } = true;
    public bool BlueprintsEnabled { get; set; } = true;
    public bool SafetyZonesEnabled { get; set; } = true;
    public bool RestrictedZonesEnabled { get; set; } = true;
    public bool MonitoredSpaceEnabled { get; set; } = true;
    public bool JurisdictionsEnabled { get; set; } = true;
    public bool QuantumArrivalEnabled { get; set; } = true;
    public bool PlayerDeathEnabled { get; set; } = true;
    public bool ServerErrorsEnabled { get; set; } = true;

    private bool _isAtStation;

    private readonly HashSet<string> _greetedShipsAtCurrentStation = new(StringComparer.OrdinalIgnoreCase);
    private DateTime _lastCrashOrDeathTime = DateTime.MinValue;
    private DateTime _lastSessionOrLoginTime = DateTime.UtcNow;

    // Sequential audio queue to prevent overlapping and audio cutting off
    private readonly System.Threading.Channels.Channel<(string FilePath, int DelayMs)> _audioChannel =
        System.Threading.Channels.Channel.CreateUnbounded<(string FilePath, int DelayMs)>(
            new System.Threading.Channels.UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _playbackTask;

    /// <summary>Gibt an, ob sich der Spieler aktuell auf einer Raumstation, im Hangar oder in einer Landezone befindet.</summary>
    public bool IsAtStation
    {
        get => _isAtStation;
        set
        {
            if (_isAtStation && !value)
            {
                // Station/Hangar verlassen: Reset für nächsten Hangar-Besuch
                _greetedShipsAtCurrentStation.Clear();
            }
            _isAtStation = value;
        }
    }


    // Sound Dictionaries
    private readonly Dictionary<string, List<string>> _shipSoundsByFamily = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _safetyZoneEnterSounds = new();
    private readonly List<string> _safetyZoneLeaveSounds = new();
    private readonly List<string> _restrictedZoneEnterSounds = new();
    private readonly List<string> _restrictedZoneLeaveSounds = new();
    private readonly List<string> _monitoredSpaceEnterSounds = new();
    private readonly List<string> _monitoredSpaceLeaveSounds = new();
    private readonly Dictionary<string, List<string>> _jurisdictionSounds = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _quantumArrivalSounds = new();
    private readonly List<string> _blueprintSounds = new();
    private readonly List<string> _playerDeathSounds = new();
    private readonly List<string> _serverErrorSounds = new();

    // Spam cooldowns & variant history (category/name -> last trigger timestamp)
    private readonly ConcurrentDictionary<string, DateTime> _lastTriggerTime = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _lastPlayedSound = new(StringComparer.OrdinalIgnoreCase);
    private const int CooldownSeconds = 12;
    private string? _lastGreetedFamily;
    private DateTime _lastGreetingTime = DateTime.MinValue;

    public bool IsInstalled => _isInstalled;
    public string? AuroraDirectory => _auroraDir;

    public int Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0, 100);
            ApplyCurrentVolume();
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    private static readonly (string Name, string Pattern)[] OriginalShipFamilies =
    [
        ("Aurora", @"\bAurora\b"),
        ("Mustang", @"\bMustang\b"),
        ("Avenger", @"\bAvenger\b"),
        ("Cutter", @"\bCutter\b"),
        ("Pisces", @"\bPisces\b"),
        ("Zeus", @"\bZeus\b"),
        ("Vulture", @"\bVulture\b"),
        ("Prospector", @"\bProspector\b"),
        ("Reclaimer", @"\bReclaimer\b"),
        ("Caterpillar", @"\bCaterpillar\b"),
        ("Hercules", @"\bHercules\b"),
        ("Mole", @"\bMOLE\b"),
        ("Arrow", @"\bArrow\b"),
        ("Gladius", @"\bGladius\b"),
        ("Vanguard", @"\bVanguard\b"),
        ("Hurricane", @"\bHurricane\b"),
        ("Eclipse", @"\bEclipse\b"),
        ("Scorpius", @"\bScorpius\b"),
        ("Cutlass", @"\bCutlass\b"),
        ("Constellation", @"\bConstellation\b"),
        ("Corsair", @"\bCorsair\b"),
        ("Mercury Star Runner", @"\bMercury\s+Star\s+Runner\b"),
        ("Redeemer", @"\bRedeemer\b"),
        ("Hammerhead", @"\bHammerhead\b"),
        ("Carrack", @"\bCarrack\b"),
        ("Apollo", @"\bApollo\b"),
        ("Polaris", @"\bPolaris\b"),
        ("Perseus", @"\bPerseus\b"),
        ("Paladin", @"\bPaladin\b"),
        ("Idris", @"\bIdris(?:-[MP])?\b"),
        ("Starfarer", @"\bStarfarer\b"),
        ("890 Jump", @"(?<!\d)890\s*Jump\b")
    ];

    private static readonly (string Name, string Pattern)[] NewShipFamilies =
    [
        ("Tiburon", @"\bTiburon\b"),
        ("Asgard", @"\bAsgard\b"),
        ("Hornet", @"\bHornet\b"),
        ("F8C Lightning", @"\bF8C(?:\s+Lightning)?\b|\bF8C\b"),
        ("Gladiator", @"\bGladiator\b"),
        ("Terrapin", @"\bTerrapin\b"),
        ("Valkyrie", @"\bValkyrie\b"),
        ("MOTH", @"\bMOTH\b"),
        ("RAFT", @"\bRAFT\b"),
        ("Defender", @"\bDefender\b"),
        ("Nomad", @"\bNomad\b"),
        ("Spirit", @"\b(?:A1|C1|E1)?\s*Spirit\b"),
        ("Ares", @"\bAres\b"),
        ("Intrepid", @"\bIntrepid\b"),
        ("Buccaneer", @"\bBuccaneer\b"),
        ("Clipper", @"\bClipper\b"),
        ("Golem", @"\bGolem(?:\s+OX)?\b"),
        ("Ironclad", @"\bIronclad(?:\s+Assault)?\b"),
        ("Pitbull", @"\bPitbull\b"),
        ("Railen", @"\bRailen\b"),
        ("Syulen", @"\bSyulen\b"),
        ("Tyilui", @"\bTyilui\b"),
        ("Basher", @"\bBasher\b"),
        ("Shiv", @"\bShiv\b"),
        ("Wolf", @"\b(?:L-?2[12]\s+)?(?:Alpha\s+)?Wolf\b"),
        ("Stingray", @"\b(?:S-?65\s+)?Stingray\b"),
        ("Fury", @"\bFury(?:\s+(?:LX|MX))?\b"),
        ("Guardian", @"\bGuardian(?:\s+(?:MX|QI))?\b"),
        ("Razor", @"\bRazor(?:\s+(?:EX|LX))?\b"),
        ("Fortune", @"\bFortune\b"),
        ("Freelancer", @"\bFreelancer(?:\s+(?:DUR|MAX|MIS))?\b"),
        ("Hull", @"\bHull(?:\s+[A-E])?\b"),
        ("Starlancer", @"\bStarlancer(?:\s+(?:MAX|TAC|BLD))?\b"),
        ("Starlite", @"\bStarlite\b"),
        ("M80", @"(?<![A-Za-z0-9])M80(?![A-Za-z0-9])"),
        ("400i", @"(?<![A-Za-z0-9])400i(?![A-Za-z0-9])"),
        ("600i", @"(?<![A-Za-z0-9])600i(?![A-Za-z0-9])"),
        ("Hermes", @"\bHermes\b"),
        ("Mantis", @"\bMantis\b"),
        ("Meteor", @"\bMeteor\b"),
        ("Salvation", @"\bSalvation\b")
    ];

    public AuroraVoiceService(string? customPath = null)
    {
        _playbackTask = Task.Run(() => ProcessAudioQueueAsync(_cts.Token));
        Initialize(customPath);
    }

    public static string? DetectAuroraDirectory(string? customPath = null)
    {
        if (!string.IsNullOrWhiteSpace(customPath) && Directory.Exists(customPath))
            return Path.GetFullPath(customPath);

        var candidates = new List<string>();

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userProfile))
        {
            candidates.Add(Path.Combine(userProfile, "OneDrive", "Dokumente", "VoiceAttack", "Aurora Log-Wächter"));
            candidates.Add(Path.Combine(userProfile, "OneDrive", "Documents", "VoiceAttack", "Aurora Log-Wächter"));
            candidates.Add(Path.Combine(userProfile, "Dokumente", "VoiceAttack", "Aurora Log-Wächter"));
            candidates.Add(Path.Combine(userProfile, "Documents", "VoiceAttack", "Aurora Log-Wächter"));
        }

        var myDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrEmpty(myDocs))
        {
            candidates.Add(Path.Combine(myDocs, "VoiceAttack", "Aurora Log-Wächter"));
        }

        foreach (var dir in candidates)
        {
            if (Directory.Exists(dir))
            {
                // Verify at least one sound directory or marker exists
                if (Directory.Exists(Path.Combine(dir, "Willkommen an bord + schiffsnamen")) ||
                    File.Exists(Path.Combine(dir, "Aurora-Schiffs-Waechter.ps1")))
                {
                    return Path.GetFullPath(dir);
                }
            }
        }

        return null;
    }

    public void Initialize(string? customPath = null)
    {
        lock (_lock)
        {
            _auroraDir = DetectAuroraDirectory(customPath);
            if (_auroraDir == null || !Directory.Exists(_auroraDir))
            {
                _isInstalled = false;
                return;
            }

            try
            {
                LoadSoundCatalogs(_auroraDir);
                _isInstalled = true;
                Logger.Log($"[AuroraVoiceService] Aurora Log-Wächter erfolgreich geladen aus: {_auroraDir}");
            }
            catch (Exception ex)
            {
                _isInstalled = false;
                Logger.Error("AuroraVoiceService Init", ex);
            }
        }
    }

    private void LoadSoundCatalogs(string baseDir)
    {
        _shipSoundsByFamily.Clear();
        _safetyZoneEnterSounds.Clear();
        _safetyZoneLeaveSounds.Clear();
        _restrictedZoneEnterSounds.Clear();
        _restrictedZoneLeaveSounds.Clear();
        _monitoredSpaceEnterSounds.Clear();
        _monitoredSpaceLeaveSounds.Clear();
        _jurisdictionSounds.Clear();
        _quantumArrivalSounds.Clear();
        _blueprintSounds.Clear();
        _playerDeathSounds.Clear();
        _serverErrorSounds.Clear();

        // 1. Original Ship Greetings (64 files -> 32 families)
        var origShipDir = Path.Combine(baseDir, "Willkommen an bord + schiffsnamen");
        if (Directory.Exists(origShipDir))
        {
            var origFiles = LoadIndexedFiles(origShipDir);
            for (int i = 0; i < OriginalShipFamilies.Length; i++)
            {
                var fam = OriginalShipFamilies[i].Name;
                int idx1 = (i * 2) + 1;
                int idx2 = (i * 2) + 2;
                var list = new List<string>();
                if (origFiles.TryGetValue(idx1, out var f1)) list.Add(f1);
                if (origFiles.TryGetValue(idx2, out var f2)) list.Add(f2);
                if (list.Count > 0) _shipSoundsByFamily[fam] = list;
            }
        }

        // 2. New Ship Greetings (82 files -> 41 families)
        var newShipDir = Directory.GetDirectories(baseDir, "neue schiffsbegr*").FirstOrDefault();
        if (newShipDir != null && Directory.Exists(newShipDir))
        {
            var newFiles = LoadIndexedFiles(newShipDir);
            for (int i = 0; i < NewShipFamilies.Length; i++)
            {
                var fam = NewShipFamilies[i].Name;
                int idx1 = (i * 2) + 1;
                int idx2 = (i * 2) + 2;
                var list = new List<string>();
                if (newFiles.TryGetValue(idx1, out var f1)) list.Add(f1);
                if (newFiles.TryGetValue(idx2, out var f2)) list.Add(f2);
                if (list.Count > 0) _shipSoundsByFamily[fam] = list;
            }
        }

        // 3. New Log Commands: Monitored Space, Jurisdictions, Quantum Arrival, Restricted Exit
        var newLogDir = Directory.GetDirectories(baseDir, "neue befehle*log").FirstOrDefault();
        if (newLogDir != null && Directory.Exists(newLogDir))
        {
            var logFiles = LoadIndexedFiles(newLogDir);
            if (logFiles.TryGetValue(1, out var ms1)) _monitoredSpaceEnterSounds.Add(ms1);
            if (logFiles.TryGetValue(2, out var ms2)) _monitoredSpaceEnterSounds.Add(ms2);
            if (logFiles.TryGetValue(3, out var ms3)) _monitoredSpaceLeaveSounds.Add(ms3);
            if (logFiles.TryGetValue(4, out var ms4)) _monitoredSpaceLeaveSounds.Add(ms4);

            AddJurisdictionSound("UEE", logFiles, 5);
            AddJurisdictionSound("microTech", logFiles, 5);
            AddJurisdictionSound("ArcCorp", logFiles, 6);
            AddJurisdictionSound("Hurston Dynamics", logFiles, 7);
            AddJurisdictionSound("Crusader Industries", logFiles, 8);
            AddJurisdictionSound("Klescher Rehabilitation", logFiles, 9);

            if (logFiles.TryGetValue(10, out var qa1)) _quantumArrivalSounds.Add(qa1);
            if (logFiles.TryGetValue(11, out var qa2)) _quantumArrivalSounds.Add(qa2);

            if (logFiles.TryGetValue(12, out var re1)) _restrictedZoneLeaveSounds.Add(re1);
            if (logFiles.TryGetValue(13, out var re2)) _restrictedZoneLeaveSounds.Add(re2);
        }

        // 4. Nyx & Pyro
        var nyxPyroDir = Directory.GetDirectories(baseDir, "*nyx*").FirstOrDefault() ?? Path.Combine(baseDir, "nyx und pyro");
        if (Directory.Exists(nyxPyroDir))
        {
            var files = Directory.GetFiles(nyxPyroDir, "*.mp3").OrderBy(f => f).ToList();
            if (files.Count > 0) _jurisdictionSounds["Pyro"] = files;
            if (files.Count > 0) _jurisdictionSounds["Nyx"] = files;
        }

        // 5. People's Alliance
        var paDir = Directory.GetDirectories(baseDir, "*people*").FirstOrDefault() ?? Path.Combine(baseDir, "peoples alliance");
        if (Directory.Exists(paDir))
        {
            var files = Directory.GetFiles(paDir, "*.mp3").OrderBy(f => f).ToList();
            if (files.Count > 0) _jurisdictionSounds["People's Alliance"] = files;
        }

        // 6. Safety Zones (Armistice)
        var safetyDir = Directory.GetDirectories(baseDir, "*sicherheitszone*").FirstOrDefault() ?? Path.Combine(baseDir, "sicherheitszone verlassen betreten");
        if (Directory.Exists(safetyDir))
        {
            var allMp3 = Directory.GetFiles(safetyDir, "*.mp3");
            foreach (var f in allMp3)
            {
                var name = Path.GetFileName(f);
                if (Regex.IsMatch(name, @"betreten|enter|eingetreten", RegexOptions.IgnoreCase))
                    _safetyZoneEnterSounds.Add(f);
                else if (Regex.IsMatch(name, @"verlassen|leave|verlässt", RegexOptions.IgnoreCase))
                    _safetyZoneLeaveSounds.Add(f);
            }
        }

        // 7. Restricted Area Entry (Sperrzone)
        var restrictedDir = Directory.GetDirectories(baseDir, "*sperrzone*").FirstOrDefault() ?? Path.Combine(baseDir, "sperrzone");
        if (Directory.Exists(restrictedDir))
        {
            var allMp3 = Directory.GetFiles(restrictedDir, "*.mp3");
            foreach (var f in allMp3)
            {
                if (Regex.IsMatch(Path.GetFileName(f), @"Systemwarnung|Sicherheitsprotokoll|Warnstufe", RegexOptions.IgnoreCase))
                    _restrictedZoneEnterSounds.Add(f);
            }
        }

        // 8. Blueprints
        var bpDir = Directory.GetDirectories(baseDir, "*blueprint*").FirstOrDefault() ?? Path.Combine(baseDir, "blueprint");
        if (Directory.Exists(bpDir))
        {
            var files = Directory.GetFiles(bpDir, "*.mp3").OrderBy(f => f).ToList();
            _blueprintSounds.AddRange(files);
        }

        // 9. Killcounter / Player Death
        var kcDir = Directory.GetDirectories(baseDir, "*kill*").FirstOrDefault() ?? Path.Combine(baseDir, "killcounter");
        if (Directory.Exists(kcDir))
        {
            var files = Directory.GetFiles(kcDir, "*.mp3").OrderBy(f => f).ToList();
            _playerDeathSounds.AddRange(files);
        }

        // 10. Server Error (30k)
        var seDir = Directory.GetDirectories(baseDir, "*server*error*").FirstOrDefault() ?? Path.Combine(baseDir, "server error");
        if (Directory.Exists(seDir))
        {
            var files = Directory.GetFiles(seDir, "*.mp3").OrderBy(f => f).ToList();
            _serverErrorSounds.AddRange(files);
        }

        Logger.Log($"[AuroraVoiceService] Kataloge geladen: Schiffe={_shipSoundsByFamily.Count} Familien, " +
                   $"Sicherheit(Enter/Leave)={_safetyZoneEnterSounds.Count}/{_safetyZoneLeaveSounds.Count}, " +
                   $"Monitored(Enter/Leave)={_monitoredSpaceEnterSounds.Count}/{_monitoredSpaceLeaveSounds.Count}, " +
                   $"Sperrzone(Enter/Leave)={_restrictedZoneEnterSounds.Count}/{_restrictedZoneLeaveSounds.Count}, " +
                   $"Rechtsgebiete={_jurisdictionSounds.Count} Zonen, Quantum={_quantumArrivalSounds.Count}, " +
                   $"Blueprints={_blueprintSounds.Count}, Death={_playerDeathSounds.Count}, 30k={_serverErrorSounds.Count}");
    }

    private void AddJurisdictionSound(string key, Dictionary<int, string> map, int index)
    {
        if (map.TryGetValue(index, out var path))
        {
            if (!_jurisdictionSounds.TryGetValue(key, out var list))
            {
                list = new List<string>();
                _jurisdictionSounds[key] = list;
            }
            list.Add(path);
        }
    }

    private static Dictionary<int, string> LoadIndexedFiles(string directory)
    {
        var dict = new Dictionary<int, string>();
        if (!Directory.Exists(directory)) return dict;

        foreach (var file in Directory.GetFiles(directory, "*.mp3"))
        {
            var match = Regex.Match(Path.GetFileName(file), @"^(?<index>\d+)_Chapter_1\.mp3$", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups["index"].Value, out int idx))
            {
                dict[idx] = file;
            }
        }
        return dict;
    }

    public void ProcessLiveLine(string line)
    {
        if (!_isEnabled || !_isInstalled) return;

        // Erkennung von Stationsaktivitäten / Hangar / Landung / Docking / Spawnen
        if (line.Contains("Hangaranfrage", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Assigned to Hangar", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Landefreigabe", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Landing Request", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("DockingTube", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("LoadingPlatformManager", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("OnClientSpawned", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("PlayerSpawnZone", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Spawned!", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Habitation", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("EZ_Hab", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Hospital", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Clinic", StringComparison.OrdinalIgnoreCase))
        {
            IsAtStation = true;
        }

        // Login / Spawnen Erkennung: Setzt Login-Cooldown (45s) und markiert sofort als Station
        if (line.Contains("OnClientSpawned", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Spawned!", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Loading screen", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Account login", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Connection established", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Session change", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("CSessionManager", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("PlayerSpawnZone", StringComparison.OrdinalIgnoreCase))
        {
            _lastSessionOrLoginTime = DateTime.UtcNow;
            IsAtStation = true;
        }

        // Echte Zerstörungs- & Tod-Erkennung (KEIN generisches "ClearDriver", "Collision", "Destroyed" oder "Crash"!)
        if (line.Contains("CSCActorCorpseUtils::PopulateItemPortForItemRecoveryEntitlement", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("Standby, Local Emergency Services Are En Route", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("<Vehicle Destruction Flow>", StringComparison.OrdinalIgnoreCase))
        {
            _lastCrashOrDeathTime = DateTime.UtcNow;
            _greetedShipsAtCurrentStation.Clear();
        }

        // 1. Channel join -> Ship Greetings
        if (ShipGreetingsEnabled)
        {
            var matchEn = Regex.Match(line, @"<SHUDEvent_OnNotification> Added notification ""You have joined channel ''(?<ch>.+?)''\.", RegexOptions.IgnoreCase);
            if (matchEn.Success)
            {
                OnShipIdentified(matchEn.Groups["ch"].Value);
                return;
            }
            var matchDe = Regex.Match(line, @"<SHUDEvent_OnNotification> Added notification ""Du bist Kanal \[\s*(?<ch>.+?)\s*\]\s+beigetreten", RegexOptions.IgnoreCase);
            if (matchDe.Success)
            {
                OnShipIdentified(matchDe.Groups["ch"].Value);
                return;
            }
        }

        // 2. Safety Zones (Armistice)
        if (SafetyZonesEnabled)
        {
            if (line.Contains("Entering Armistice Zone", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Schutzzone - Kampfhandlung untersagt", StringComparison.OrdinalIgnoreCase))
            {
                OnSafetyZoneChanged(true);
                return;
            }
            if (line.Contains("Leaving Armistice Zone", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Schutzzone verlassen", StringComparison.OrdinalIgnoreCase))
            {
                OnSafetyZoneChanged(false);
                return;
            }
        }

        // 3. Monitored Space
        if (MonitoredSpaceEnabled)
        {
            if (line.Contains("Entered Monitored Space", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Kontrollierten Raum betreten", StringComparison.OrdinalIgnoreCase))
            {
                OnMonitoredSpaceChanged(true);
                return;
            }
            if (line.Contains("Exited Monitored Space", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Kontrollierten Raum verlassen", StringComparison.OrdinalIgnoreCase))
            {
                OnMonitoredSpaceChanged(false);
                return;
            }
        }

        // 4. Restricted Zones
        if (RestrictedZonesEnabled)
        {
            if (line.Contains("Entering Private Property", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Restricted Area - Vehicles Will Be Impounded", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Sperrgebiet", StringComparison.OrdinalIgnoreCase))
            {
                OnRestrictedZoneChanged(true);
                return;
            }
            if (line.Contains("Leaving Private Property", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Leaving Restricted Area", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Sperrgebiet verlassen", StringComparison.OrdinalIgnoreCase))
            {
                OnRestrictedZoneChanged(false);
                return;
            }
        }

        // 5. Quantum Arrival
        if (QuantumArrivalEnabled)
        {
            if (line.Contains("<Quantum Drive Arrived - Arrived at Final Destination>", StringComparison.OrdinalIgnoreCase))
            {
                OnQuantumArrival();
                return;
            }
        }

        // 6. Server Error (30k)
        if (ServerErrorsEnabled)
        {
            if (line.Contains("Connection with the server has been lost", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Error code: 30000", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Disconnected from server", StringComparison.OrdinalIgnoreCase))
            {
                OnServerError();
                return;
            }
        }

        // 7. Player Death
        if (PlayerDeathEnabled)
        {
            if (line.Contains("Standby, Local Emergency Services Are En Route", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("CSCActorCorpseUtils::PopulateItemPortForItemRecoveryEntitlement", StringComparison.OrdinalIgnoreCase))
            {
                OnPlayerDeath();
                return;
            }
        }
    }

    public void ProcessLiveEvent(LogEntry e)
    {
        if (!_isEnabled || !_isInstalled) return;

        // Nach Crash, Spielertod oder Schiffsverlust: Sofortige Sperre für Begrüßungen
        if (e.Kind is EventKind.Death or EventKind.ShipLoss or EventKind.Crash)
        {
            _lastCrashOrDeathTime = DateTime.UtcNow;
            _greetedShipsAtCurrentStation.Clear();
            if (PlayerDeathEnabled)
            {
                OnPlayerDeath();
            }
            return;
        }

        // Schiffs-Begrüßung NUR beim Neueinstieg / Wechsel des Fahrzeugs (EventKind.Vehicle) im Hangar, NIEMALS beim Quantum-Sprung, Claim oder Crash!
        if (e.Kind == EventKind.Vehicle && e.Ship != null && ShipGreetingsEnabled)
        {
            if (!e.Detail.Contains("Claim", StringComparison.OrdinalIgnoreCase) &&
                !e.Detail.Contains("ClearDriver", StringComparison.OrdinalIgnoreCase) &&
                !e.Detail.Contains("zerstört", StringComparison.OrdinalIgnoreCase) &&
                !e.Detail.Contains("Kollision", StringComparison.OrdinalIgnoreCase))
            {
                OnShipIdentified(e.Ship);
            }
        }
        else if (e.Kind == EventKind.Quantum)
        {
            IsAtStation = false;
            _greetedShipsAtCurrentStation.Clear();
            if (QuantumArrivalEnabled)
            {
                OnQuantumArrival();
            }
        }
        else if (e.Kind == EventKind.Blueprint && BlueprintsEnabled)
        {
            OnBlueprintLearned(e.Detail);
        }
        else if (e.Kind == EventKind.Jurisdiction)
        {
            if (e.Detail.Contains("Schutzzone", StringComparison.OrdinalIgnoreCase) ||
                e.Detail.Contains("Armistice", StringComparison.OrdinalIgnoreCase))
            {
                bool entering = !e.Detail.Contains("verlassen", StringComparison.OrdinalIgnoreCase) &&
                                !e.Detail.Contains("leaving", StringComparison.OrdinalIgnoreCase);
                OnSafetyZoneChanged(entering);
            }
            else if (JurisdictionsEnabled)
            {
                OnJurisdictionChanged(e.Detail);
            }
        }
    }

    public void OnShipIdentified(string shipName)
    {
        if (!_isEnabled || !_isInstalled || !ShipGreetingsEnabled) return;
        if (string.IsNullOrWhiteSpace(shipName)) return;

        // Benutzer-Vorgabe: Willkommen NUR, wenn man das Schiff das erste Mal im Hangar auf einer Station betritt!
        if (!IsAtStation)
        {
            Logger.Log($"[AuroraVoiceService] Schiffsbegrüßung für '{shipName}' ignoriert: Nicht im Hangar / auf Station.");
            return;
        }

        // Nach einem echten Crash, Spielertod oder Schiffsverlust keine Begrüßung (Sperre für 60 Sekunden)
        if ((DateTime.UtcNow - _lastCrashOrDeathTime).TotalSeconds < 60)
        {
            Logger.Log($"[AuroraVoiceService] Schiffsbegrüßung für '{shipName}' ignoriert: Kürzlicher Crash / Spielertod vor {(DateTime.UtcNow - _lastCrashOrDeathTime).TotalSeconds:F0}s.");
            return;
        }

        // Während der Login-/Ladephase keine Schiffsbegrüßung (Sperre für 25 Sekunden nach Login/Spawn)
        if ((DateTime.UtcNow - _lastSessionOrLoginTime).TotalSeconds < 25)
        {
            Logger.Log($"[AuroraVoiceService] Schiffsbegrüßung für '{shipName}' ignoriert: Login-Phase aktiv.");
            return;
        }

        var family = MatchShipFamily(shipName);
        if (family != null && _shipSoundsByFamily.TryGetValue(family, out var sounds) && sounds.Count > 0)
        {
            // Nur beim ERSTEN Mal Betreten dieses Schiffes während des aktuellen Hangar-/Stationsaufenthalts begrüßen
            if (_greetedShipsAtCurrentStation.Contains(family))
            {
                Logger.Log($"[AuroraVoiceService] Schiffsbegrüßung für '{family}' bereits während dieses Hangar-Aufenthalts abgespielt.");
                return;
            }

            _greetedShipsAtCurrentStation.Add(family);
            _lastGreetedFamily = family;
            _lastGreetingTime = DateTime.UtcNow;
            Logger.Log($"[AuroraVoiceService] Schiffsbegrüßung im Hangar/Station für '{family}' ausgelöst.");
            PlaySoundWithCooldown($"ship_{family}", sounds, delayMs: 1000);
        }
        else
        {
            Logger.Log($"[AuroraVoiceService] Kein Soundkatalog gefunden für Schiff: '{shipName}' (Familie: '{family ?? "unbekannt"}').");
        }
    }

    public void OnSafetyZoneChanged(bool entering)
    {
        if (!_isEnabled || !_isInstalled || !SafetyZonesEnabled) return;

        // Während Login-/Ladephase unterdrücken
        if ((DateTime.UtcNow - _lastSessionOrLoginTime).TotalSeconds < 45)
        {
            Logger.Log("[AuroraVoiceService] Schutzzonen-Audio unterdrückt (Login-Phase aktiv).");
            if (entering) IsAtStation = true;
            return;
        }

        if (entering)
        {
            IsAtStation = true;
        }
        else
        {
            IsAtStation = false;
        }

        var sounds = entering ? _safetyZoneEnterSounds : _safetyZoneLeaveSounds;
        if (sounds.Count > 0)
            PlaySoundWithCooldown(entering ? "safety_enter" : "safety_leave", sounds, minCooldownSeconds: 30);
    }

    public void OnMonitoredSpaceChanged(bool entering)
    {
        if (!_isEnabled || !_isInstalled || !MonitoredSpaceEnabled) return;

        // Während Login-Phase unterdrücken
        if ((DateTime.UtcNow - _lastSessionOrLoginTime).TotalSeconds < 45)
        {
            Logger.Log("[AuroraVoiceService] MonitoredSpace-Audio unterdrückt (Login-Phase aktiv).");
            return;
        }

        var sounds = entering ? _monitoredSpaceEnterSounds : _monitoredSpaceLeaveSounds;
        if (sounds.Count > 0)
            PlaySoundWithCooldown(entering ? "monitored_enter" : "monitored_leave", sounds, minCooldownSeconds: 30);
    }

    public void OnRestrictedZoneChanged(bool entering)
    {
        if (!_isEnabled || !_isInstalled || !RestrictedZonesEnabled) return;

        // Während Login-Phase unterdrücken
        if ((DateTime.UtcNow - _lastSessionOrLoginTime).TotalSeconds < 45)
        {
            Logger.Log("[AuroraVoiceService] RestrictedZone-Audio unterdrückt (Login-Phase aktiv).");
            return;
        }

        var sounds = entering ? _restrictedZoneEnterSounds : _restrictedZoneLeaveSounds;
        if (sounds.Count > 0)
            PlaySoundWithCooldown(entering ? "restricted_enter" : "restricted_leave", sounds, minCooldownSeconds: 30);
    }

    public void OnJurisdictionChanged(string jurisdiction)
    {
        if (!_isEnabled || !_isInstalled || !JurisdictionsEnabled) return;
        if (string.IsNullOrWhiteSpace(jurisdiction)) return;

        // Falls Schutzzonen-Detail im Event, an OnSafetyZoneChanged weiterleiten
        if (jurisdiction.Contains("Schutzzone", StringComparison.OrdinalIgnoreCase) ||
            jurisdiction.Contains("Armistice", StringComparison.OrdinalIgnoreCase))
        {
            bool entering = !jurisdiction.Contains("verlassen", StringComparison.OrdinalIgnoreCase) &&
                            !jurisdiction.Contains("leaving", StringComparison.OrdinalIgnoreCase);
            OnSafetyZoneChanged(entering);
            return;
        }

        // Während Login-Phase unterdrücken
        if ((DateTime.UtcNow - _lastSessionOrLoginTime).TotalSeconds < 45)
        {
            Logger.Log($"[AuroraVoiceService] Jurisdiction-Audio für '{jurisdiction}' unterdrückt (Login-Phase aktiv).");
            return;
        }

        foreach (var kvp in _jurisdictionSounds)
        {
            if (jurisdiction.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) && kvp.Value.Count > 0)
            {
                PlaySoundWithCooldown($"jurisdiction_{kvp.Key}", kvp.Value, minCooldownSeconds: 60);
                return;
            }
        }
    }

    public void OnQuantumArrival()
    {
        if (!_isEnabled || !_isInstalled || !QuantumArrivalEnabled) return;
        if (_quantumArrivalSounds.Count > 0)
            PlaySoundWithCooldown("quantum_arrival", _quantumArrivalSounds);
    }

    public void OnBlueprintLearned(string blueprintName)
    {
        if (!_isEnabled || !_isInstalled || !BlueprintsEnabled) return;
        if (_blueprintSounds.Count > 0)
            PlaySoundWithCooldown("blueprint", _blueprintSounds);
    }

    public void OnPlayerDeath()
    {
        _lastCrashOrDeathTime = DateTime.UtcNow;
        _greetedShipsAtCurrentStation.Clear();
        if (!_isEnabled || !_isInstalled || !PlayerDeathEnabled) return;
        if (_playerDeathSounds.Count > 0)
            PlaySoundWithCooldown("player_death", _playerDeathSounds);
    }

    public void OnServerError()
    {
        if (!_isEnabled || !_isInstalled || !ServerErrorsEnabled) return;
        if (_serverErrorSounds.Count > 0)
            PlaySoundWithCooldown("server_error", _serverErrorSounds);
    }

    private Windows.Media.Playback.MediaPlayer? _mediaPlayer;

    public void PlayTestSound()
    {
        if (!_isInstalled) return;
        // Pick any available sound (e.g., first quantum arrival or first ship sound)
        var sound = _quantumArrivalSounds.FirstOrDefault() ??
                    _safetyZoneEnterSounds.FirstOrDefault() ??
                    _shipSoundsByFamily.Values.SelectMany(v => v).FirstOrDefault();

        if (sound != null && File.Exists(sound))
        {
            Logger.Log($"[AuroraVoiceService] Test-Sound angefordert: {sound}");
            PlayFileAsync(sound, 0);
        }
        else
        {
            Logger.Log($"[AuroraVoiceService] Kein Test-Sound gefunden! (Kataloge leer?)");
        }
    }

    private string? MatchShipFamily(string shipName)
    {
        foreach (var (name, pattern) in OriginalShipFamilies)
        {
            if (Regex.IsMatch(shipName, pattern, RegexOptions.IgnoreCase))
                return name;
        }
        foreach (var (name, pattern) in NewShipFamilies)
        {
            if (Regex.IsMatch(shipName, pattern, RegexOptions.IgnoreCase))
                return name;
        }
        return null;
    }

    private void PlaySoundWithCooldown(string key, List<string> soundOptions, int delayMs = 0, int minCooldownSeconds = CooldownSeconds)
    {
        var now = DateTime.UtcNow;
        if (_lastTriggerTime.TryGetValue(key, out var lastTime))
        {
            if ((now - lastTime).TotalSeconds < minCooldownSeconds)
                return;
        }

        _lastTriggerTime[key] = now;

        string? previousSound = _lastPlayedSound.TryGetValue(key, out var prev) ? prev : null;
        var eligible = soundOptions.Where(s => s != previousSound).ToList();
        if (eligible.Count == 0) eligible = soundOptions;

        var selected = eligible[_rand.Next(eligible.Count)];
        _lastPlayedSound[key] = selected;

        PlayFileAsync(selected, delayMs);
    }

    private void PlayFileAsync(string filePath, int delayMs)
    {
        _audioChannel.Writer.TryWrite((filePath, delayMs));
    }

    private async Task ProcessAudioQueueAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var item = await _audioChannel.Reader.ReadAsync(ct);
                if (item.DelayMs > 0)
                    await Task.Delay(item.DelayMs, ct);

                await PlayAudioFileCoreAsync(item.FilePath, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Logger.Error("Aurora Audio Queue", ex);
            }
        }
    }

    private async Task PlayAudioFileCoreAsync(string filePath, CancellationToken ct)
    {
        if (!File.Exists(filePath))
        {
            Logger.Log($"[AuroraVoiceService] Audiodatei existiert nicht: {filePath}");
            return;
        }

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_lock)
        {
            try
            {
                _mediaPlayer ??= new Windows.Media.Playback.MediaPlayer();

                Windows.Foundation.TypedEventHandler<Windows.Media.Playback.MediaPlayer, object>? endedHandler = null;
                Windows.Foundation.TypedEventHandler<Windows.Media.Playback.MediaPlayer, Windows.Media.Playback.MediaPlayerFailedEventArgs>? failedHandler = null;

                endedHandler = (s, e) =>
                {
                    try
                    {
                        s.MediaEnded -= endedHandler;
                        s.MediaFailed -= failedHandler;
                    }
                    catch { }
                    tcs.TrySetResult(true);
                };

                failedHandler = (s, e) =>
                {
                    try
                    {
                        s.MediaEnded -= endedHandler;
                        s.MediaFailed -= failedHandler;
                    }
                    catch { }
                    Logger.Log($"[AuroraVoiceService] MediaPlayer Fehler: {e.ErrorMessage} ({e.ExtendedErrorCode?.Message})");
                    tcs.TrySetResult(false);
                };

                _mediaPlayer.MediaEnded += endedHandler;
                _mediaPlayer.MediaFailed += failedHandler;

                _mediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(filePath));
                _mediaPlayer.Volume = Math.Clamp(_volume / 100.0, 0.0, 1.0);
                _mediaPlayer.Play();
                Logger.Log($"[AuroraVoiceService] Audio abgespielt: {Path.GetFileName(filePath)} (Lautstärke: {_volume}%)");
            }
            catch (Exception ex)
            {
                Logger.Error("Aurora Audio Playback", ex);
                tcs.TrySetResult(false);
            }
        }

        try
        {
            // Max 8 Sekunden warten, falls MediaEnded ausbleibt
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
            linked.Token.Register(() => tcs.TrySetResult(false));

            await tcs.Task;
            // 200 ms natürliche Pause zwischen zwei Sprachansagen
            await Task.Delay(200, ct);
        }
        catch (OperationCanceledException) { }
    }

    private void ApplyCurrentVolume()
    {
        lock (_lock)
        {
            try
            {
                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Volume = Math.Clamp(_volume / 100.0, 0.0, 1.0);
                }
            }
            catch { /* ignore */ }
        }
    }

    public void Dispose()
    {
        try
        {
            _cts.Cancel();
        }
        catch { }

        lock (_lock)
        {
            try
            {
                _mediaPlayer?.Dispose();
                _mediaPlayer = null;
            }
            catch { /* ignore */ }
        }

        try
        {
            _cts.Dispose();
        }
        catch { }
    }
}

