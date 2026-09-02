using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace SCLogMate.Core;

public class LanguageOption
{
    public string Code { get; }
    public string Title { get; }
    public bool IsAuto => Code.Equals("Auto", StringComparison.OrdinalIgnoreCase);
    public bool IsGerman => Code.Equals("de", StringComparison.OrdinalIgnoreCase);
    public bool IsEnglish => Code.Equals("en", StringComparison.OrdinalIgnoreCase);

    public LanguageOption(string code, string title)
    {
        Code = code;
        Title = title;
    }

    public override string ToString() => Title;
}

/// <summary>
/// Zentraler, erweiterbarer I18n-Lokalisierungsdienst für SCLogMate.
/// Bietet reaktive Sprachumschaltung (DE / EN), Fallbacks und XAML-Indexer-Binding.
/// </summary>
public sealed class I18n : INotifyPropertyChanged
{
    public static I18n Instance { get; } = new();

    private string _currentLanguage = "de";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string CurrentLanguage
    {
        get => _currentLanguage;
        private set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                OnPropertyChanged(nameof(CurrentLanguage));
                OnPropertyChanged(nameof(IsGerman));
                OnPropertyChanged(nameof(IsEnglish));
                OnPropertyChanged("Item[]"); // Benachrichtigt Avalonia-Indexer-Bindings
                OnPropertyChanged(string.Empty);
            }
        }
    }

    public bool IsGerman => CurrentLanguage == "de";
    public bool IsEnglish => CurrentLanguage == "en";

    /// <summary>Indexer für XAML-Bindings: {Binding I18n[Key]}</summary>
    public string this[string key] => Get(key);

    private I18n()
    {
        // Initialisiere mit System- oder Settings-Sprache
        SetLanguage("Auto");
    }

    /// <summary>
    /// Setzt die aktive Sprache ("de", "en", oder "Auto" für automatische Erkennung).
    /// </summary>
    public void SetLanguage(string langOption)
    {
        var resolved = langOption?.ToLowerInvariant() switch
        {
            "de" or "deutsch" or "german" => "de",
            "en" or "english" or "englisch" => "en",
            _ => ResolveSystemLanguage()
        };

        CurrentLanguage = resolved;
    }

    private static string ResolveSystemLanguage()
    {
        var uiLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        if (uiLang == "de") return "de";
        var cultLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLowerInvariant();
        if (cultLang == "de") return "de";
        return "en"; // Standard für alle anderen Sprachen
    }

    /// <summary>
    /// Liefert die Übersetzung für einen Schlüssel. Fallback: Englisch -> Deutsch -> Schlüssel selbst.
    /// </summary>
    public string Get(string key, params object[] args)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;

        string? text = null;
        if (Translations.TryGetValue(CurrentLanguage, out var dict) && dict.TryGetValue(key, out var val))
        {
            text = val;
        }
        else if (Translations.TryGetValue("en", out var enDict) && enDict.TryGetValue(key, out var enVal))
        {
            text = enVal;
        }
        else if (Translations.TryGetValue("de", out var deDict) && deDict.TryGetValue(key, out var deVal))
        {
            text = deVal;
        }

        text ??= key;

        if (args != null && args.Length > 0)
        {
            try { return string.Format(text, args); }
            catch { return text; }
        }

        return text;
    }

    public static string T(string key, params object[] args) => Instance.Get(key, args);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Zentrales Wörterbuch aller UI-Strings.
    /// Modular und jederzeit um weitere Sprachen (z.B. "fr", "es") und Schlüssel erweiterbar.
    /// </summary>
    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["de"] = new(StringComparer.OrdinalIgnoreCase)
        {
            // Header Bar & Status
            ["Header_Session"] = "SESSION",
            ["Header_Status_Running"] = "🟢 SC BEREIT",
            ["Header_Status_Standby"] = "⚪ SC STANDBY",
            ["Header_Status_Tooltip_Running"] = "Star Citizen läuft · Live-Tracking aktiv",
            ["Header_Status_Tooltip_Standby"] = "Star Citizen ist derzeit nicht gestartet · Parser wartet auf Spielstart",
            ["Header_MiniHud"] = "Mini-HUD",
            ["Header_RsOverlay"] = "RS-Overlay",
            ["Header_Language_Tooltip"] = "Globale Sprache für SCLogMate & Begleiter wählen",

            // Dashboard Karten (KPIs)
            ["Dash_Pilot_Server"] = "◆  PILOT & SERVER",
            ["Dash_Location"] = "◆  STANDORT",
            ["Dash_Ship_Fleet"] = "◆  SCHIFF & FLOTTE",
            ["Dash_Account"] = "◆  KONTOSTAND (aUEC)",
            ["Dash_Contracts"] = "◆  AUFTRAGSMANAGER (ACCEPTED)",
            ["Dash_Waiting_Logs"] = "Warte auf Log-Daten...",
            ["Dash_No_Ship"] = "Kein Schiff registriert",
            ["Dash_No_Contract"] = "— Kein aktiver Auftrag —",
            ["Dash_No_Contracts_Active"] = "Keine aktiven Aufträge",
            ["Dash_Income"] = "▲ Einnahmen: {0}",
            ["Dash_Spend"] = "▼ Ausgaben: {0}",
            ["Dash_Net"] = "◆ Saldo: {0}",
            ["Dash_Auto_Sync"] = "⚡ Auto",
            ["Dash_Calibrate_Region"] = "⊕ Bereich",
            ["Dash_Scan"] = "▶ Scan",
            ["Dash_Armistice_Safe"] = "Schutzzone",
            ["Dash_Armistice_Free"] = "Waffen aktiv",
            ["Dash_Type_LandingZone"] = "🏛 LANDUNGSZONE",
            ["Dash_Type_SpaceStation"] = "🛸 RAUMSTATION",
            ["Dash_Type_RestStop"] = "⛏ RAFFINERIE / REST STOP",
            ["Dash_Type_Moon"] = "🪐 MOND",
            ["Dash_Type_Planet"] = "🌍 PLANET",
            ["Dash_Type_JumpPoint"] = "🌀 SPRUNGTOR",
            ["Dash_Type_Outpost"] = "🏕 AUSSENPOSTEN",
            ["Dash_Type_Default"] = "📍 STANDORT",

            // 12 Haupt-Tabs
            ["Tab_Events"] = "📜 Ereignisse",
            ["Tab_Finances"] = "💰 Finanzen",
            ["Tab_Missions"] = "❖ Missionen",
            ["Tab_Reputation"] = "🎖 Ruf",
            ["Tab_Starmap"] = "🗺 Karte",
            ["Tab_BlackBox"] = "⏱ Flugschreiber",
            ["Tab_OreScanner"] = "🛰 Erz-Scanner",
            ["Tab_Fleet"] = "🛸 Flotte",
            ["Tab_Blueprints"] = "🛠 Baupläne",
            ["Tab_Loadout"] = "🥋 Ausrüstung",
            ["Tab_Settings"] = "⚙ Einstellungen",
            ["Tab_About"] = "ℹ Über",

            // Filter Chips
            ["Filter_All"] = "Alle",
            ["Filter_Money"] = "💰 Geld",
            ["Filter_Contracts"] = "📋 Aufträge",
            ["Filter_Blueprints"] = "⬡ Baupläne",
            ["Filter_Ships"] = "✈ Schiffe",
            ["Filter_Locations"] = "◉ Orte",
            ["Filter_Crew"] = "♟ Crew",
            ["Filter_Loot"] = "◈ Loot",
            ["Filter_Misc"] = "⋯ Sonst",
            ["Filter_Search_Watermark"] = "Ereignisse filtern…",

            // Settings Sub-Tabs
            ["SubTab_General"] = "📁 Allgemein",
            ["SubTab_Wipe"] = "⏳ Wipe & Filter",
            ["SubTab_Overlays"] = "🖥 Overlays",
            ["SubTab_Ocr"] = "👁 mobiGlas / OCR",
            ["SubTab_Uex"] = "🌐 UEX Corp",
            ["SubTab_VoiceAttack"] = "🎙️ VoiceAttack",
            ["SubTab_Database"] = "💾 Datenbank",
            ["SubTab_Developer"] = "🧪 Entwickler",

            // VoiceAttack & Aurora Tab
            ["Va_Title"] = "🎙️  VOICEATTACK & AURORA LOG-WÄCHTER INTEGRATION",
            ["Va_Desc"] = "Nativer Audio-Begleiter für Star Citizen Live-Events. Greift rein lesend auf das VoiceAttack Aurora Log-Wächter Profil zu.",
            ["Va_Profile_Found"] = "Profil Gefunden & Bereit",
            ["Va_Profile_NotFound"] = "Nicht Gefunden",
            ["Va_Path_Label"] = "Installations-Pfad (Dokumente / OneDrive)",
            ["Va_Path_Error"] = "Ordner 'VoiceAttack\\Aurora Log-Wächter' nicht gefunden (Integration deaktiviert)",
            ["Va_Audio_Active"] = "Audio-Begleiter Aktiv",
            ["Va_Volume"] = "Lautstärke:",
            ["Va_Test_Audio"] = "Test-Audio",
            ["Va_Categories_Title"] = "🎛️  AKTIVE SPRACH-KATEGORIEN",
            ["Va_Armistice_Hint"] = "Schutzzonen auf Stationen werden automatisch stummgeschaltet",
            ["Va_Cat_ShipGreetings"] = "🚀 Schiffsbegrüßungen",
            ["Va_Cat_ShipGreetings_Sub"] = "73 Schiffsklassen",
            ["Va_Cat_Blueprints"] = "📜 Baupläne",
            ["Va_Cat_Blueprints_Sub"] = "Crafting-Baupläne",
            ["Va_Cat_SafetyZones"] = "🛡️ Sicherheitszonen",
            ["Va_Cat_SafetyZones_Sub"] = "Armistice Zone (im Raum)",
            ["Va_Cat_RestrictedZones"] = "⛔ Sperrzonen",
            ["Va_Cat_RestrictedZones_Sub"] = "Sperrgebiet / Privat",
            ["Va_Cat_MonitoredSpace"] = "📡 Überwachter Raum",
            ["Va_Cat_MonitoredSpace_Sub"] = "Comm-Array",
            ["Va_Cat_Jurisdictions"] = "🏛️ Hoheitsgebiete",
            ["Va_Cat_Jurisdictions_Sub"] = "UEE, Hurston, etc.",
            ["Va_Cat_QuantumArrival"] = "🌌 Quantenreise",
            ["Va_Cat_QuantumArrival_Sub"] = "Sprungziel-Ankunft",
            ["Va_Cat_PlayerDeath"] = "💀 Notfall / Tod",
            ["Va_Cat_PlayerDeath_Sub"] = "Spielertod / Med-Bett",
            ["Va_Cat_ServerErrors"] = "⚠️ Serverfehler (30k)",
            ["Va_Cat_ServerErrors_Sub"] = "Verbindungsabbruch",
            ["Va_Simulate_Uninstalled"] = "🧪 'Nicht installiert' simulieren",

            // Allgemein Tab
            ["Gen_StarCitizen_Title"] = "📁  STAR CITIZEN & LOGDATEI",
            ["Gen_StarCitizen_Desc"] = "Wähle deine Game.log Datei aus dem LIVE- oder PTU-Installationsverzeichnis von Star Citizen.",
            ["Gen_Browse"] = "📁  Durchsuchen",
            ["Gen_AutoDetect"] = "⚡ Auto-Erkennung",
            ["Gen_Language_Title"] = "🌐  SPRACHE & REGIONAL-EINSTELLUNGEN",
            ["Gen_Language_Desc"] = "Wähle die Anzeigesprache für SCLogMate. Star Citizen Logs werden in Deutsch und Englisch nahtlos unterstützt.",
            ["Gen_System_Title"] = "❖  FENSTER-, TRAY- & SYSTEM-VERHALTEN",
            ["Gen_MinimizeToTray"] = "Beim Klick auf das Schließen-Kreuz (X) ins System-Tray minimieren",
            ["Gen_MinimizeToTray_Sub"] = "Die App läuft im Hintergrund im Infobereich weiter (Tracking, OCR & Overlays bleiben aktiv).",
            ["Gen_Autostart"] = "Mit Windows automatisch im Hintergrund starten (System-Tray)",
            ["Gen_Font_Title"] = "🔤  BENUTZEROBERFLÄCHE & SCHRIFTART",

            // Overlays Tab
            ["Overlay_MiniHud_Title"] = "🖥  IN-GAME FLOATING MINI-HUD OVERLAY",
            ["Overlay_MiniHud_Desc"] = "Zeigt deinen Kontostand, aktuellen Standort, aktiven Auftrag und Server-Ping in einem verschiebbaren Overlay direkt über dem Spiel.",
            ["Overlay_Show_Hud"] = "Mini-HUD aktivieren",
            ["Overlay_Lock_Position"] = "Position sperren (Click-Through)",
            ["Overlay_Opacity"] = "Deckkraft / Transparenz:",
            ["Overlay_Reset_Pos"] = "Position zurücksetzen",
            ["Overlay_Toast_Title"] = "✦  ACHIEVEMENT & REWARD TOAST BANNER",

            // Entwickler Tab
            ["Dev_Live_Events_Title"] = "🧪  STAR CITIZEN LIVE-EVENTS SIMULIEREN",
            ["Dev_Live_Events_Desc"] = "Injiziert simulierte Star Citizen Game.log-Events zur Prüfung von Audioausgaben, Muting, Cooldowns und Benachrichtigungen.",
            ["Dev_Enter_Armistice"] = "Schutzzone betreten",
            ["Dev_Leave_Armistice"] = "Schutzzone verlassen",
            ["Dev_Ship_Join"] = "Schiffseinstieg (Cutlass)",
            ["Dev_Blueprint_Found"] = "Bauplan erlernt (Toast)",
            ["Dev_Quantum_Arrival"] = "Quantensprung Ankunft",
            ["Dev_Server_Error"] = "30k Serverfehler",
            ["Dev_Diagnostics_Title"] = "⚙  DIAGNOSE, SYSTEMSTATUS & OVERLAYS",
            ["Dev_Dump_State"] = "Status in Log dumpen",
            ["Dev_Toast_Test"] = "Achievement Toast Test",
            ["Dev_Reset_Overlays"] = "Overlays resetten (50,50)"
        },

        ["en"] = new(StringComparer.OrdinalIgnoreCase)
        {
            // Header Bar & Status
            ["Header_Session"] = "SESSION",
            ["Header_Status_Running"] = "🟢 SC RUNNING",
            ["Header_Status_Standby"] = "⚪ SC STANDBY",
            ["Header_Status_Tooltip_Running"] = "Star Citizen is running · Live tracking active",
            ["Header_Status_Tooltip_Standby"] = "Star Citizen is currently not running · Waiting for game launch",
            ["Header_MiniHud"] = "Mini-HUD",
            ["Header_RsOverlay"] = "RS Overlay",
            ["Header_Language_Tooltip"] = "Select global language for SCLogMate & companion prompts",

            // Dashboard Cards (KPIs)
            ["Dash_Pilot_Server"] = "◆  PILOT & SERVER",
            ["Dash_Location"] = "◆  LOCATION",
            ["Dash_Ship_Fleet"] = "◆  SHIP & FLEET",
            ["Dash_Account"] = "◆  ACCOUNT BALANCE (aUEC)",
            ["Dash_Contracts"] = "◆  CONTRACT MANAGER (ACCEPTED)",
            ["Dash_Waiting_Logs"] = "Waiting for log data...",
            ["Dash_No_Ship"] = "No ship registered",
            ["Dash_No_Contract"] = "— No active contract —",
            ["Dash_No_Contracts_Active"] = "No active contracts",
            ["Dash_Income"] = "▲ Income: {0}",
            ["Dash_Spend"] = "▼ Expenses: {0}",
            ["Dash_Net"] = "◆ Net Balance: {0}",
            ["Dash_Auto_Sync"] = "⚡ Auto",
            ["Dash_Calibrate_Region"] = "⊕ Region",
            ["Dash_Scan"] = "▶ Scan",
            ["Dash_Armistice_Safe"] = "Armistice Zone",
            ["Dash_Armistice_Free"] = "Weapons Free",
            ["Dash_Type_LandingZone"] = "🏛 LANDING ZONE",
            ["Dash_Type_SpaceStation"] = "🛸 SPACE STATION",
            ["Dash_Type_RestStop"] = "⛏ REFINERY / REST STOP",
            ["Dash_Type_Moon"] = "🪐 MOON",
            ["Dash_Type_Planet"] = "🌍 PLANET",
            ["Dash_Type_JumpPoint"] = "🌀 JUMP POINT",
            ["Dash_Type_Outpost"] = "🏕 OUTPOST",
            ["Dash_Type_Default"] = "📍 LOCATION",

            // 12 Main Tabs
            ["Tab_Events"] = "📜 Events",
            ["Tab_Finances"] = "💰 Finances",
            ["Tab_Missions"] = "❖ Missions",
            ["Tab_Reputation"] = "🎖 Reputation",
            ["Tab_Starmap"] = "🗺 Starmap",
            ["Tab_BlackBox"] = "⏱ Flight Recorder",
            ["Tab_OreScanner"] = "🛰 Ore Scanner",
            ["Tab_Fleet"] = "🛸 Fleet",
            ["Tab_Blueprints"] = "🛠 Blueprints",
            ["Tab_Loadout"] = "🥋 Loadout",
            ["Tab_Settings"] = "⚙ Settings",
            ["Tab_About"] = "ℹ About",

            // Filter Chips
            ["Filter_All"] = "All",
            ["Filter_Money"] = "💰 Money",
            ["Filter_Contracts"] = "📋 Contracts",
            ["Filter_Blueprints"] = "⬡ Blueprints",
            ["Filter_Ships"] = "✈ Ships",
            ["Filter_Locations"] = "◉ Locations",
            ["Filter_Crew"] = "♟ Crew",
            ["Filter_Loot"] = "◈ Loot",
            ["Filter_Misc"] = "⋯ Misc",
            ["Filter_Search_Watermark"] = "Filter events…",

            // Settings Sub-Tabs
            ["SubTab_General"] = "📁 General",
            ["SubTab_Wipe"] = "⏳ Wipe & Filter",
            ["SubTab_Overlays"] = "🖥 Overlays",
            ["SubTab_Ocr"] = "👁 mobiGlas / OCR",
            ["SubTab_Uex"] = "🌐 UEX Corp",
            ["SubTab_VoiceAttack"] = "🎙️ VoiceAttack",
            ["SubTab_Database"] = "💾 Database",
            ["SubTab_Developer"] = "🧪 Developer",

            // VoiceAttack & Aurora Tab
            ["Va_Title"] = "🎙️  VOICEATTACK & AURORA COMPANION INTEGRATION",
            ["Va_Desc"] = "Native audio companion for Star Citizen live events. Accesses VoiceAttack Aurora profile in read-only mode.",
            ["Va_Profile_Found"] = "Profile Found & Ready",
            ["Va_Profile_NotFound"] = "Not Found",
            ["Va_Path_Label"] = "Installation Path (Documents / OneDrive)",
            ["Va_Path_Error"] = "Folder 'VoiceAttack\\Aurora Log-Wächter' not found (Integration disabled)",
            ["Va_Audio_Active"] = "Audio Companion Active",
            ["Va_Volume"] = "Volume:",
            ["Va_Test_Audio"] = "Test Audio",
            ["Va_Categories_Title"] = "🎛️  ACTIVE VOICE CATEGORIES",
            ["Va_Armistice_Hint"] = "Safety zones at stations are automatically silenced",
            ["Va_Cat_ShipGreetings"] = "🚀 Ship Greetings",
            ["Va_Cat_ShipGreetings_Sub"] = "73 ship classes",
            ["Va_Cat_Blueprints"] = "📜 Blueprints",
            ["Va_Cat_Blueprints_Sub"] = "Crafting blueprints",
            ["Va_Cat_SafetyZones"] = "🛡️ Safety Zones",
            ["Va_Cat_SafetyZones_Sub"] = "Armistice zone (space only)",
            ["Va_Cat_RestrictedZones"] = "⛔ Restricted Zones",
            ["Va_Cat_RestrictedZones_Sub"] = "Restricted / Private area",
            ["Va_Cat_MonitoredSpace"] = "📡 Monitored Space",
            ["Va_Cat_MonitoredSpace_Sub"] = "Comm-Array",
            ["Va_Cat_Jurisdictions"] = "🏛️ Jurisdictions",
            ["Va_Cat_Jurisdictions_Sub"] = "UEE, Hurston, etc.",
            ["Va_Cat_QuantumArrival"] = "🌌 Quantum Travel",
            ["Va_Cat_QuantumArrival_Sub"] = "Destination arrival",
            ["Va_Cat_PlayerDeath"] = "💀 Emergency / Death",
            ["Va_Cat_PlayerDeath_Sub"] = "Player death / Med-bed",
            ["Va_Cat_ServerErrors"] = "⚠️ Server Error (30k)",
            ["Va_Cat_ServerErrors_Sub"] = "Connection loss",
            ["Va_Simulate_Uninstalled"] = "🧪 Simulate 'Not Installed'",

            // General Tab
            ["Gen_StarCitizen_Title"] = "📁  STAR CITIZEN & LOG FILE",
            ["Gen_StarCitizen_Desc"] = "Select your Game.log file from the Star Citizen LIVE or PTU directory.",
            ["Gen_Browse"] = "📁  Browse",
            ["Gen_AutoDetect"] = "⚡ Auto Detect",
            ["Gen_Language_Title"] = "🌐  LANGUAGE & REGIONAL SETTINGS",
            ["Gen_Language_Desc"] = "Choose the display language for SCLogMate. Star Citizen logs in German and English are seamlessly supported.",
            ["Gen_System_Title"] = "❖  WINDOW, TRAY & SYSTEM BEHAVIOR",
            ["Gen_MinimizeToTray"] = "Minimize to system tray when clicking close (X)",
            ["Gen_MinimizeToTray_Sub"] = "The app keeps running in the system tray (Tracking, OCR and Overlays remain active).",
            ["Gen_Autostart"] = "Start automatically with Windows in background (System Tray)",
            ["Gen_Font_Title"] = "🔤  USER INTERFACE & FONT",

            // Overlays Tab
            ["Overlay_MiniHud_Title"] = "🖥  IN-GAME FLOATING MINI-HUD OVERLAY",
            ["Overlay_MiniHud_Desc"] = "Shows your balance, current location, active contract, and server ping in a movable overlay directly above the game.",
            ["Overlay_Show_Hud"] = "Enable Mini-HUD",
            ["Overlay_Lock_Position"] = "Lock Position (Click-Through)",
            ["Overlay_Opacity"] = "Opacity / Transparency:",
            ["Overlay_Reset_Pos"] = "Reset Position",
            ["Overlay_Toast_Title"] = "✦  ACHIEVEMENT & REWARD TOAST BANNER",

            // Developer Tab
            ["Dev_Live_Events_Title"] = "🧪  SIMULATE STAR CITIZEN LIVE EVENTS",
            ["Dev_Live_Events_Desc"] = "Injects simulated Star Citizen Game.log events to test audio playback, muting, cooldowns, and notifications.",
            ["Dev_Enter_Armistice"] = "Enter Armistice",
            ["Dev_Leave_Armistice"] = "Leave Armistice",
            ["Dev_Ship_Join"] = "Ship Boarding (Cutlass)",
            ["Dev_Blueprint_Found"] = "Blueprint Learned (Toast)",
            ["Dev_Quantum_Arrival"] = "Quantum Jump Arrival",
            ["Dev_Server_Error"] = "30k Server Error",
            ["Dev_Diagnostics_Title"] = "⚙  DIAGNOSTICS, SYSTEM STATUS & OVERLAYS",
            ["Dev_Dump_State"] = "Dump Status to Log",
            ["Dev_Toast_Test"] = "Achievement Toast Test",
            ["Dev_Reset_Overlays"] = "Reset Overlays (50,50)"
        }
    };
}
