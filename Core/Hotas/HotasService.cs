using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SCLogMate.Core.Hotas;

public static class HotasService
{
    private static readonly Regex ConnectedJoystickRegex = new(
        @"-\s+Connected\s+joystick(?<idx>\d+):\s+(?<product>.+?)\s+(?<guid>\{[0-9A-Fa-f\-]+\})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Dictionary<string, string> VendorLookup = new(StringComparer.OrdinalIgnoreCase)
    {
        { "044F", "Thrustmaster" },
        { "231D", "VKB-Sim" },
        { "3344", "Virpil Controls" },
        { "046D", "Logitech" },
        { "0738", "Saitek / Mad Catz" },
        { "3416", "WINWING" },
        { "16C0", "Leo Bodnar / DIY USB" },
        { "28DE", "Valve Software" },
        { "045E", "Microsoft" },
        { "2433", "Asetek SimSports" },
        { "0EB7", "Fanatec" }
    };

    private static readonly Dictionary<string, (string Category, string Label)> ActionMapLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        { "spaceship_movement", ("Flug", "Steuerung & Triebwerke") },
        { "spaceship_view", ("Flug", "Kamera & Blickfeld") },
        { "spaceship_targeting", ("Kampf", "Zielerfassung & Radar") },
        { "spaceship_weapons", ("Kampf", "Waffen & Geschütze") },
        { "spaceship_missiles", ("Kampf", "Raketen-Operator-Modus") },
        { "spaceship_defensive", ("Kampf", "Täuschkörper & Abwehr") },
        { "spaceship_power", ("Systeme", "Energie- & Schildverteilung") },
        { "spaceship_quantum", ("Systeme", "Quantenantrieb (QT) & Reise") },
        { "spaceship_mining", ("Industrie", "Bergbau & Laser") },
        { "spaceship_salvage", ("Industrie", "Bergung & Zerlegen") },
        { "spaceship_radar", ("Scanner", "Radar & Ping") },
        { "spaceship_scanning", ("Scanner", "Aktiver Scanner") },
        { "spaceship_docking", ("Flug", "Andocken & Landehilfe") },
        { "seat_general", ("Allgemein", "Pilotensitz & Bordsysteme") },
        { "vehicle_general", ("Allgemein", "Fahrzeug Allgemein") },
        { "player", ("Charakter", "Zu Fuß (On Foot)") },
        { "lights_controller", ("Systeme", "Scheinwerfer & Beleuchtung") },
        { "doors", ("Systeme", "Türen & Frachtrampen") },
        { "flight_throttle", ("Flug", "Schubregler") }
    };

    private static readonly Dictionary<string, string> ActionLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        { "v_pitch", "Nickachse (Pitch - Nase Hoch/Runter)" },
        { "v_yaw", "Gierachse (Yaw - Nase Links/Rechts)" },
        { "v_roll", "Rollachse (Roll - Neigen Links/Rechts)" },
        { "v_pitch_up", "Neigen Nach Oben" },
        { "v_pitch_down", "Neigen Nach Unten" },
        { "v_yaw_left", "Gieren Nach Links" },
        { "v_yaw_right", "Gieren Nach Rechts" },
        { "v_roll_left", "Rollen Nach Links" },
        { "v_roll_right", "Rollen Nach Rechts" },
        { "v_strafe_vertical", "Vertikalschub (Auf/Ab Achse)" },
        { "v_strafe_up", "Schub Aufwärts" },
        { "v_strafe_down", "Schub Abwärts" },
        { "v_strafe_lateral", "Lateralschub (Links/Rechts Achse)" },
        { "v_strafe_left", "Schub Links" },
        { "v_strafe_right", "Schub Rechts" },
        { "v_strafe_longitudinal", "Längsschub (Vorwärts/Rückwärts)" },
        { "v_strafe_forward", "Schub Vorwärts" },
        { "v_strafe_back", "Schub Rückwärts" },
        { "v_throttle", "Schubregler (Throttle Achse)" },
        { "v_throttle_rel", "Schubregler Relativ" },
        { "v_boost", "Afterburner / Boost" },
        { "v_space_brake", "Weltraumbremse (Spacebrake)" },
        { "v_decoupled_mode_toggle", "Decoupled-Flugmodus Umschalten" },
        { "v_cruise_control_toggle", "Tempomat (Cruise Control)" },
        { "v_speed_limiter_toggle", "Speed-Limiter Umschalten" },
        { "v_speed_limiter_rel", "Speed-Limiter Achse" },
        { "v_target_lock_selected", "Ausgewähltes Ziel Fixieren" },
        { "v_target_closest_hostile", "Nächsten Feind Anvisieren" },
        { "v_target_under_reticle", "Ziel unter Fadenkreuz Anvisieren" },
        { "v_attack1_group1", "Feuer Waffengruppe 1" },
        { "v_attack1_group2", "Feuer Waffengruppe 2" },
        { "v_weapon_mode_toggle", "Waffenmodus (Gimbal / Fixiert)" },
        { "v_toggle_missile_mode", "Raketen-Operator-Modus An/Aus" },
        { "v_weapon_launch_missile", "Rakete Abfeuern" },
        { "v_weapon_launch_countermeasure", "Täuschkörper Abfeuern (Decoy / Flare)" },
        { "v_weapon_launch_countermeasure_noise", "Störfeld Abfeuern (Noise / Chaff)" },
        { "v_shield_raise_level_forward", "Schilde nach Vorne Leiten" },
        { "v_shield_raise_level_back", "Schilde nach Hinten Leiten" },
        { "v_shield_reset_levels", "Schilde Ausbalancieren" },
        { "v_quantum_toggle", "Quantenantrieb Spulen / Aktivieren" },
        { "v_quantum_travel_engage", "Quantensprung Initiieren (Jump)" },
        { "v_toggle_mining_mode", "Bergbaumodus Umschalten" },
        { "v_toggle_salvage_mode", "Salvage-Modus Umschalten" },
        { "v_toggle_landing_system", "Fahrwerk Ein-/Ausfahren" },
        { "v_autoland", "Automatisches Landen (Hold)" },
        { "v_toggle_vtol", "VTOL-Triebwerke Umschalten" },
        { "v_eject", "Notausstieg / Schleudersitz" },
        { "v_lights", "Schiffsscheinwerfer Umschalten" },
        { "v_toggle_all_doors", "Alle Türen Öffnen/Schließen" },
        { "v_lock_all_doors", "Alle Türen Verriegeln" },
        { "v_view_dynamic_zoom_rel", "Dynamischer Kamera-Zoom" }
    };

    private static readonly Dictionary<string, string> OptionLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        { "flight_move_pitch", "Nickachse (Pitch Kurve)" },
        { "flight_move_yaw", "Gierachse (Yaw Kurve)" },
        { "flight_move_roll", "Rollachse (Roll Kurve)" },
        { "flight_move_strafe_vertical", "Vertikalschub (Auf/Ab Kurve)" },
        { "flight_move_strafe_lateral", "Lateralschub (Links/Rechts Kurve)" },
        { "flight_move_strafe_longitudinal", "Längsschub (Vor/Zurück Kurve)" },
        { "flight_throttle", "Schubregler (Throttle Kurve)" },
        { "turret_pitch", "Geschützturm Neigen" },
        { "turret_yaw", "Geschützturm Drehen" },
        { "flight_view_pitch", "Blickwinkel Neigen" },
        { "flight_view_yaw", "Blickwinkel Drehen" }
    };

    public static HotasStatusDto GetHotasStatus(string? logPath)
    {
        var result = new HotasStatusDto { Enabled = true };
        var (_, _, _, actionMapsFile) = MaintenanceService.ResolveKeybindPaths(logPath);

        if (string.IsNullOrEmpty(actionMapsFile) || !File.Exists(actionMapsFile))
        {
            result.ActionMapsFound = false;
            result.ActionMapsPath = actionMapsFile;
            return result;
        }

        result.ActionMapsFound = true;
        result.ActionMapsPath = actionMapsFile;

        // 1. Scan Game.log for recently connected joysticks
        result.LogDevices = ScanLogForJoysticks(logPath);

        try
        {
            using var stream = File.Open(actionMapsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var doc = XDocument.Load(stream);
            var root = doc.Root;
            if (root == null) return result;

            var profile = root.Name.LocalName == "ActionMaps" && root.Element("ActionProfiles") is { } inner ? inner : root;
            result.ProfileName = (string?)profile.Attribute("profileName") ?? "default";

            // 2. Parse Deadzones from deviceoptions
            var deadzones = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
            foreach (var devOpt in profile.Elements("deviceoptions"))
            {
                var pName = (string?)devOpt.Attribute("name") ?? "";
                if (string.IsNullOrWhiteSpace(pName)) continue;

                var axes = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                foreach (var opt in devOpt.Elements("option"))
                {
                    var inp = (string?)opt.Attribute("input");
                    if (string.IsNullOrEmpty(inp)) continue;

                    if (double.TryParse((string?)opt.Attribute("deadzone"), NumberStyles.Float, CultureInfo.InvariantCulture, out var dz))
                    {
                        axes[inp] = dz;
                    }
                }
                deadzones[pName.Trim()] = axes;
            }

            // 3. Parse options type="joystick"
            var devices = new List<HotasDeviceDto>();
            foreach (var opt in profile.Elements("options"))
            {
                var type = (string?)opt.Attribute("type") ?? "";
                if (!type.Equals("joystick", StringComparison.OrdinalIgnoreCase)) continue;

                if (!int.TryParse((string?)opt.Attribute("instance"), out var instance)) continue;
                var rawProduct = (string?)opt.Attribute("Product");
                var (product, guid) = SplitProduct(rawProduct);
                var (vid, pid, vendor) = ParseUsbInfo(guid);

                var curves = new List<HotasCurveDto>();
                foreach (var child in opt.Elements())
                {
                    var optName = child.Name.LocalName;
                    var expStr = (string?)child.Attribute("exponent");
                    double? exp = double.TryParse(expStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedExp) ? parsedExp : null;
                    bool inverted = (string?)child.Attribute("invert") == "1";

                    curves.Add(new HotasCurveDto
                    {
                        Option = optName,
                        OptionLabel = OptionLabels.TryGetValue(optName, out var ol) ? ol : optName,
                        Exponent = exp,
                        Inverted = inverted
                    });
                }

                // Match deadzones
                var devDeadzones = new Dictionary<string, double>();
                if (rawProduct != null && deadzones.TryGetValue(rawProduct.Trim(), out var dzMatched))
                {
                    devDeadzones = dzMatched;
                }
                else if (product != null && deadzones.TryGetValue(product.Trim(), out var dzPMatched))
                {
                    devDeadzones = dzPMatched;
                }

                devices.Add(new HotasDeviceDto
                {
                    Instance = instance,
                    DeviceKey = $"js{instance}",
                    Product = product ?? (instance == 1 ? "Primärer Joystick" : $"Joystick {instance}"),
                    Guid = guid,
                    VendorName = vendor,
                    UsbVid = vid,
                    UsbPid = pid,
                    Curves = curves,
                    Deadzones = devDeadzones
                });
            }

            // Sort devices by instance
            devices = devices.OrderBy(d => d.Instance).ToList();

            // 4. Parse Bindings
            var allBindings = new List<HotasBindingDto>();
            foreach (var map in profile.Elements("actionmap"))
            {
                var mapName = (string?)map.Attribute("name") ?? "";
                var (cat, mapLbl) = ActionMapLabels.TryGetValue(mapName, out var mapInfo)
                    ? mapInfo
                    : ("Andere", mapName);

                foreach (var action in map.Elements("action"))
                {
                    var actName = (string?)action.Attribute("name") ?? "";
                    var actLbl = ActionLabels.TryGetValue(actName, out var al) ? al : actName;

                    foreach (var rebind in action.Elements("rebind"))
                    {
                        var rawInput = (string?)rebind.Attribute("input");
                        if (string.IsNullOrWhiteSpace(rawInput)) continue;

                        var (devKey, devType, inst, ctrl, ctrlLbl) = ParseInputControl(rawInput);
                        var actMode = (string?)rebind.Attribute("activationMode");
                        int? multiTap = int.TryParse((string?)rebind.Attribute("multiTap"), out var mt) ? mt : null;

                        allBindings.Add(new HotasBindingDto
                        {
                            ActionMap = mapName,
                            ActionMapLabel = mapLbl,
                            Category = cat,
                            Action = actName,
                            ActionLabel = actLbl,
                            RawInput = rawInput,
                            DeviceKey = devKey,
                            DeviceType = devType,
                            Instance = inst,
                            Control = ctrl,
                            ControlLabel = ctrlLbl,
                            ActivationMode = actMode,
                            MultiTap = multiTap
                        });
                    }
                }
            }

            // Update binding counts
            foreach (var dev in devices)
            {
                dev.BindingCount = allBindings.Count(b => b.DeviceKey.Equals(dev.DeviceKey, StringComparison.OrdinalIgnoreCase));
            }

            // Filter devices: keep only devices with a GUID, product name, or bindings
            result.Devices = devices
                .Where(d => !string.IsNullOrEmpty(d.Guid) || d.BindingCount > 0 || (d.Product != null && !d.Product.StartsWith("Joystick ")))
                .ToList();
            result.Bindings = allBindings;
            result.TotalBindingsCount = allBindings.Count;
            result.JoystickBindingsCount = allBindings.Count(b => b.DeviceType == "joystick");

            // 5. Correlate with Game.log devices & detect swaps
            CorrelateAndDetectSwaps(result);
        }
        catch (Exception ex)
        {
            Logger.Log($"[WARN] Fehler beim Auslesen von actionmaps.xml: {ex.Message}");
        }

        return result;
    }

    private static List<HotasConnectedLogDeviceDto> ScanLogForJoysticks(string? logPath)
    {
        var list = new List<HotasConnectedLogDeviceDto>();
        if (string.IsNullOrEmpty(logPath) || !File.Exists(logPath)) return list;

        try
        {
            using var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs, Encoding.UTF8);

            string? line;
            int count = 0;
            // The joysticks are enumerated near startup (first 400 lines)
            while ((line = reader.ReadLine()) != null && count++ < 1200)
            {
                var match = ConnectedJoystickRegex.Match(line);
                if (match.Success)
                {
                    int idx = int.Parse(match.Groups["idx"].Value);
                    string prod = match.Groups["product"].Value.Trim();
                    string guid = match.Groups["guid"].Value.Trim();

                    string ts = "";
                    if (line.StartsWith('<') && line.IndexOf('>') > 0)
                    {
                        ts = line.Substring(1, line.IndexOf('>') - 1);
                    }

                    list.Add(new HotasConnectedLogDeviceDto
                    {
                        LogIndex = idx,
                        ExpectedInstance = idx + 1, // joystick0 corresponds to instance 1
                        Product = prod,
                        Guid = guid,
                        Timestamp = ts
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"[WARN] Fehler beim Lesen von Game.log für Joysticks: {ex.Message}");
        }

        return list;
    }

    private static void CorrelateAndDetectSwaps(HotasStatusDto status)
    {
        if (status.LogDevices.Count == 0 || status.Devices.Count == 0) return;

        // Match log devices to actionmaps devices
        foreach (var dev in status.Devices)
        {
            // Expected log index is dev.Instance - 1
            var directMatch = status.LogDevices.FirstOrDefault(l => l.ExpectedInstance == dev.Instance);
            if (directMatch != null)
            {
                dev.ConnectedIndex = directMatch.LogIndex;
                dev.ConnectedProduct = directMatch.Product;

                // Check if product names or GUIDs align
                bool sameProduct = ProductsMatch(dev.Product, directMatch.Product) || GuidsMatch(dev.Guid, directMatch.Guid);
                dev.IsConnected = sameProduct;
            }
        }

        // Detect instance swap (most common is instance 1 and 2 swapped)
        var dev1 = status.Devices.FirstOrDefault(d => d.Instance == 1);
        var dev2 = status.Devices.FirstOrDefault(d => d.Instance == 2);
        var log0 = status.LogDevices.FirstOrDefault(l => l.LogIndex == 0);
        var log1 = status.LogDevices.FirstOrDefault(l => l.LogIndex == 1);

        if (dev1 != null && dev2 != null && log0 != null && log1 != null)
        {
            bool dev1MatchesLog0 = ProductsMatch(dev1.Product, log0.Product) || GuidsMatch(dev1.Guid, log0.Guid);
            bool dev2MatchesLog1 = ProductsMatch(dev2.Product, log1.Product) || GuidsMatch(dev2.Guid, log1.Guid);

            bool dev1MatchesLog1 = ProductsMatch(dev1.Product, log1.Product) || GuidsMatch(dev1.Guid, log1.Guid);
            bool dev2MatchesLog0 = ProductsMatch(dev2.Product, log0.Product) || GuidsMatch(dev2.Guid, log0.Guid);

            if (!dev1MatchesLog0 && !dev2MatchesLog1 && dev1MatchesLog1 && dev2MatchesLog0)
            {
                status.HasMismatch = true;
                dev1.IsSwappedWith = 2;
                dev2.IsSwappedWith = 1;

                string name1 = dev1.Product ?? "Stick 1";
                string name2 = dev2.Product ?? "Stick 2";

                status.MismatchDescription =
                    $"Windows USB-Vertauschung erkannt: '{log0.Product}' ist aktuell als Joystick 1 verbunden, dein Profil erwartet dort aber '{name1}'.";
                status.SuggestedConsoleCommand = "pp_resortdevices joystick 1 2";
            }
        }
    }

    private static bool ProductsMatch(string? p1, string? p2)
    {
        if (string.IsNullOrWhiteSpace(p1) || string.IsNullOrWhiteSpace(p2)) return false;
        return p1.Trim().Equals(p2.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool GuidsMatch(string? g1, string? g2)
    {
        if (string.IsNullOrWhiteSpace(g1) || string.IsNullOrWhiteSpace(g2)) return false;
        return g1.Trim().Equals(g2.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static HotasSwapResultDto SwapDevices(string? logPath, int instanceA = 1, int instanceB = 2)
    {
        if (instanceA <= 0 || instanceB <= 0 || instanceA == instanceB)
        {
            return new HotasSwapResultDto
            {
                Success = false,
                Message = "Ungültige Joystick-Instanzen für den Tausch."
            };
        }

        var (_, _, _, actionMapsFile) = MaintenanceService.ResolveKeybindPaths(logPath);
        if (string.IsNullOrEmpty(actionMapsFile) || !File.Exists(actionMapsFile))
        {
            return new HotasSwapResultDto
            {
                Success = false,
                Message = "Die Datei actionmaps.xml wurde nicht gefunden."
            };
        }

        try
        {
            // 1. Take a safe automatic backup first
            var (backupOk, backupMsg, _) = MaintenanceService.BackupKeybinds(logPath, customNote: $"PreSwap_js{instanceA}_js{instanceB}");

            // 2. Load XML
            XDocument doc;
            using (var stream = File.Open(actionMapsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                doc = XDocument.Load(stream);
            }

            var root = doc.Root;
            if (root == null) throw new InvalidOperationException("actionmaps.xml ist leer oder beschädigt.");

            var profile = root.Name.LocalName == "ActionMaps" && root.Element("ActionProfiles") is { } inner ? inner : root;

            // 3. Swap <options type="joystick" instance="...">
            var optA = profile.Elements("options").FirstOrDefault(o =>
                (string?)o.Attribute("type") == "joystick" && (string?)o.Attribute("instance") == instanceA.ToString());
            var optB = profile.Elements("options").FirstOrDefault(o =>
                (string?)o.Attribute("type") == "joystick" && (string?)o.Attribute("instance") == instanceB.ToString());

            if (optA != null && optB != null)
            {
                optA.SetAttributeValue("instance", instanceB.ToString());
                optB.SetAttributeValue("instance", instanceA.ToString());
            }
            else if (optA != null)
            {
                optA.SetAttributeValue("instance", instanceB.ToString());
            }
            else if (optB != null)
            {
                optB.SetAttributeValue("instance", instanceA.ToString());
            }

            // 4. Swap all <rebind input="..."> in actionmaps
            int swapCount = 0;
            string prefixA = $"js{instanceA}_";
            string prefixB = $"js{instanceB}_";
            string tempPlaceholder = $"__js_temp_swap_{instanceA}__";

            foreach (var rebind in profile.Descendants("rebind"))
            {
                var input = (string?)rebind.Attribute("input");
                if (string.IsNullOrWhiteSpace(input)) continue;

                if (input.Contains(prefixA, StringComparison.OrdinalIgnoreCase) ||
                    input.Contains(prefixB, StringComparison.OrdinalIgnoreCase))
                {
                    // Chords can contain multiple parts separated by '+'
                    var parts = input.Split('+');
                    var newParts = new string[parts.Length];
                    bool modified = false;

                    for (int i = 0; i < parts.Length; i++)
                    {
                        var seg = parts[i].Trim();
                        if (seg.StartsWith(prefixA, StringComparison.OrdinalIgnoreCase))
                        {
                            newParts[i] = tempPlaceholder + seg.Substring(prefixA.Length);
                            modified = true;
                        }
                        else if (seg.StartsWith(prefixB, StringComparison.OrdinalIgnoreCase))
                        {
                            newParts[i] = prefixA + seg.Substring(prefixB.Length);
                            modified = true;
                        }
                        else
                        {
                            newParts[i] = seg;
                        }
                    }

                    if (modified)
                    {
                        for (int i = 0; i < newParts.Length; i++)
                        {
                            if (newParts[i].StartsWith(tempPlaceholder))
                            {
                                newParts[i] = prefixB + newParts[i].Substring(tempPlaceholder.Length);
                            }
                        }

                        rebind.SetAttributeValue("input", string.Join("+", newParts));
                        swapCount++;
                    }
                }
            }

            // 5. Save back to file
            doc.Save(actionMapsFile);

            var newStatus = GetHotasStatus(logPath);
            return new HotasSwapResultDto
            {
                Success = true,
                Message = $"✓ Erfolgreich getauscht: Joystick {instanceA} ⇄ Joystick {instanceB} ({swapCount} Belegungen & Achsen angepasst).",
                BackupFolder = backupOk ? backupMsg : null,
                Status = newStatus
            };
        }
        catch (Exception ex)
        {
            Logger.Error("Fehler beim Tauschen der Joysticks in actionmaps.xml", ex);
            return new HotasSwapResultDto
            {
                Success = false,
                Message = $"Fehler beim Tausch: {ex.Message}"
            };
        }
    }

    public static (bool Success, string Message, string? TargetFile) ExportLayout(string? logPath, string layoutName)
    {
        var (_, _, mappingsDir, actionMapsFile) = MaintenanceService.ResolveKeybindPaths(logPath);
        if (string.IsNullOrEmpty(actionMapsFile) || !File.Exists(actionMapsFile))
        {
            return (false, "actionmaps.xml nicht gefunden.", null);
        }

        try
        {
            if (string.IsNullOrEmpty(mappingsDir))
            {
                var live = Path.GetDirectoryName(logPath);
                mappingsDir = Path.Combine(live ?? "", "user", "client", "0", "controls", "mappings");
            }

            Directory.CreateDirectory(mappingsDir);

            var safeName = SanitizeFileName(layoutName);
            if (string.IsNullOrWhiteSpace(safeName)) safeName = "sclogmate_profile";
            if (!safeName.StartsWith("layout_", StringComparison.OrdinalIgnoreCase)) safeName = $"layout_{safeName}";
            if (!safeName.EndsWith("_exported", StringComparison.OrdinalIgnoreCase)) safeName = $"{safeName}_exported";

            var targetFile = Path.Combine(mappingsDir, $"{safeName}.xml");

            XDocument doc;
            using (var stream = File.Open(actionMapsFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                doc = XDocument.Load(stream);
            }

            var root = doc.Root ?? throw new InvalidOperationException("Leere actionmaps.xml");
            var profile = root.Name.LocalName == "ActionMaps" && root.Element("ActionProfiles") is { } inner ? inner : root;

            var exportRoot = new XElement("ActionMaps",
                new XAttribute("version", (string?)profile.Attribute("version") ?? "1"),
                new XAttribute("optionsVersion", (string?)profile.Attribute("optionsVersion") ?? "2"),
                new XAttribute("rebindVersion", (string?)profile.Attribute("rebindVersion") ?? "2"),
                new XAttribute("profileName", layoutName));

            // CustomisationUIHeader
            var header = new XElement("CustomisationUIHeader",
                new XAttribute("label", layoutName),
                new XAttribute("description", "Exportiert mit SCLogMate HOTAS Studio"),
                new XAttribute("image", ""));
            exportRoot.Add(header);

            foreach (var elem in profile.Elements())
            {
                if (elem.Name.LocalName == "CustomisationUIHeader") continue;
                exportRoot.Add(new XElement(elem));
            }

            var exportDoc = new XDocument(new XDeclaration("1.0", "utf-8", null), exportRoot);
            exportDoc.Save(targetFile);

            return (true, $"Profil erfolgreich exportiert nach: {Path.GetFileName(targetFile)}", targetFile);
        }
        catch (Exception ex)
        {
            return (false, $"Fehler beim Export: {ex.Message}", null);
        }
    }

    private static (string DeviceKey, string DeviceType, int Instance, string Control, string ControlLabel) ParseInputControl(string raw)
    {
        var text = raw.Trim();
        var lastPart = text.Split('+')[^1].Trim();

        var underscore = lastPart.IndexOf('_');
        if (underscore >= 3 && char.IsLetter(lastPart[0]) && char.IsLetter(lastPart[1]) &&
            int.TryParse(lastPart.AsSpan(2, underscore - 2), out var inst))
        {
            var pfx = lastPart.Substring(0, 2).ToLowerInvariant();
            var devType = pfx switch
            {
                "js" => "joystick",
                "kb" => "keyboard",
                "mo" => "mouse",
                "gp" => "gamepad",
                _ => "other"
            };

            var ctrl = lastPart.Substring(underscore + 1);
            var devKey = $"{pfx}{inst}";

            string ctrlLabel = ctrl;
            if (ctrl.StartsWith("button", StringComparison.OrdinalIgnoreCase))
            {
                ctrlLabel = $"Taste {ctrl.Substring(6)}";
            }
            else if (ctrl.StartsWith("hat", StringComparison.OrdinalIgnoreCase))
            {
                ctrlLabel = $"Coolie-Hat {ctrl.Substring(3).Replace('_', ' ')}";
            }
            else if (ctrl.Equals("x", StringComparison.OrdinalIgnoreCase)) ctrlLabel = "X-Achse";
            else if (ctrl.Equals("y", StringComparison.OrdinalIgnoreCase)) ctrlLabel = "Y-Achse";
            else if (ctrl.Equals("z", StringComparison.OrdinalIgnoreCase)) ctrlLabel = "Z-Achse";
            else if (ctrl.Equals("rotx", StringComparison.OrdinalIgnoreCase)) ctrlLabel = "Rot-X (Drehung X)";
            else if (ctrl.Equals("roty", StringComparison.OrdinalIgnoreCase)) ctrlLabel = "Rot-Y (Drehung Y)";
            else if (ctrl.Equals("rotz", StringComparison.OrdinalIgnoreCase)) ctrlLabel = "Rot-Z (Drehung Z / Twist)";
            else if (ctrl.StartsWith("slider", StringComparison.OrdinalIgnoreCase)) ctrlLabel = $"Schieberegler {ctrl.Substring(6)}";

            return (devKey, devType, inst, ctrl, ctrlLabel);
        }

        return ("other", "other", 0, lastPart, lastPart);
    }

    private static (string? Product, string? Guid) SplitProduct(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return (null, null);
        var brace = raw.LastIndexOf('{');
        if (brace < 0) return (raw.Trim(), null);

        var name = raw.Substring(0, brace).Trim();
        var guid = raw.Substring(brace).Trim();
        return (name.Length > 0 ? name : null, guid.EndsWith('}') ? guid : null);
    }

    private static (string? Vid, string? Pid, string? VendorName) ParseUsbInfo(string? guid)
    {
        if (string.IsNullOrWhiteSpace(guid)) return (null, null, null);
        var g = guid.Trim('{', '}');
        if (g.Length < 8 || !g.EndsWith("504944564944", StringComparison.OrdinalIgnoreCase)) return (null, null, null);

        var pidHex = g.Substring(0, 4);
        var vidHex = g.Substring(4, 4);

        string? vendor = VendorLookup.TryGetValue(vidHex, out var vName) ? vName : null;
        return (vidHex, pidHex, vendor);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder();
        foreach (var c in name)
        {
            if (Array.IndexOf(invalid, c) < 0 && c != ' ' && c != ';')
                sb.Append(c);
            else
                sb.Append('_');
        }
        return sb.ToString().Trim('_');
    }
}
