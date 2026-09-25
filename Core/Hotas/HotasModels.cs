using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SCLogMate.Core.Hotas;

public class HotasDeviceDto
{
    [JsonPropertyName("instance")] public int Instance { get; set; }
    [JsonPropertyName("deviceKey")] public string DeviceKey { get; set; } = ""; // e.g. "js1", "js2"
    [JsonPropertyName("product")] public string? Product { get; set; } // e.g. "Sol-R [R] Flightstick"
    [JsonPropertyName("guid")] public string? Guid { get; set; } // e.g. "{0422044F-0000-0000-0000-504944564944}"
    [JsonPropertyName("vendorName")] public string? VendorName { get; set; } // e.g. "Thrustmaster"
    [JsonPropertyName("usbVid")] public string? UsbVid { get; set; } // e.g. "044F"
    [JsonPropertyName("usbPid")] public string? UsbPid { get; set; } // e.g. "0422"
    [JsonPropertyName("deadzones")] public Dictionary<string, double> Deadzones { get; set; } = new();
    [JsonPropertyName("curves")] public List<HotasCurveDto> Curves { get; set; } = new();
    [JsonPropertyName("bindingCount")] public int BindingCount { get; set; }
    [JsonPropertyName("isConnected")] public bool IsConnected { get; set; }
    [JsonPropertyName("connectedIndex")] public int? ConnectedIndex { get; set; } // 0, 1 from Game.log
    [JsonPropertyName("connectedProduct")] public string? ConnectedProduct { get; set; }
    [JsonPropertyName("isSwappedWith")] public int? IsSwappedWith { get; set; }
}

public class HotasCurveDto
{
    [JsonPropertyName("option")] public string Option { get; set; } = "";
    [JsonPropertyName("optionLabel")] public string OptionLabel { get; set; } = "";
    [JsonPropertyName("exponent")] public double? Exponent { get; set; }
    [JsonPropertyName("inverted")] public bool Inverted { get; set; }
}

public class HotasBindingDto
{
    [JsonPropertyName("actionMap")] public string ActionMap { get; set; } = "";
    [JsonPropertyName("actionMapLabel")] public string ActionMapLabel { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("action")] public string Action { get; set; } = "";
    [JsonPropertyName("actionLabel")] public string ActionLabel { get; set; } = "";
    [JsonPropertyName("rawInput")] public string RawInput { get; set; } = "";
    [JsonPropertyName("deviceKey")] public string DeviceKey { get; set; } = "";
    [JsonPropertyName("deviceType")] public string DeviceType { get; set; } = ""; // joystick, keyboard, mouse, gamepad
    [JsonPropertyName("instance")] public int Instance { get; set; }
    [JsonPropertyName("control")] public string Control { get; set; } = "";
    [JsonPropertyName("controlLabel")] public string ControlLabel { get; set; } = "";
    [JsonPropertyName("activationMode")] public string? ActivationMode { get; set; }
    [JsonPropertyName("multiTap")] public int? MultiTap { get; set; }
}

public class HotasConnectedLogDeviceDto
{
    [JsonPropertyName("logIndex")] public int LogIndex { get; set; } // e.g. 0, 1
    [JsonPropertyName("expectedInstance")] public int ExpectedInstance { get; set; } // 1, 2
    [JsonPropertyName("product")] public string Product { get; set; } = "";
    [JsonPropertyName("guid")] public string Guid { get; set; } = "";
    [JsonPropertyName("timestamp")] public string Timestamp { get; set; } = "";
}

public class HotasStatusDto
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("actionMapsFound")] public bool ActionMapsFound { get; set; }
    [JsonPropertyName("actionMapsPath")] public string? ActionMapsPath { get; set; }
    [JsonPropertyName("profileName")] public string ProfileName { get; set; } = "default";
    [JsonPropertyName("devices")] public List<HotasDeviceDto> Devices { get; set; } = new();
    [JsonPropertyName("logDevices")] public List<HotasConnectedLogDeviceDto> LogDevices { get; set; } = new();
    [JsonPropertyName("hasMismatch")] public bool HasMismatch { get; set; }
    [JsonPropertyName("mismatchDescription")] public string? MismatchDescription { get; set; }
    [JsonPropertyName("suggestedConsoleCommand")] public string? SuggestedConsoleCommand { get; set; }
    [JsonPropertyName("totalBindingsCount")] public int TotalBindingsCount { get; set; }
    [JsonPropertyName("joystickBindingsCount")] public int JoystickBindingsCount { get; set; }
    [JsonPropertyName("lastBackupFolder")] public string? LastBackupFolder { get; set; }
    [JsonPropertyName("bindings")] public List<HotasBindingDto> Bindings { get; set; } = new();
}

public class HotasSwapResultDto
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("message")] public string Message { get; set; } = "";
    [JsonPropertyName("backupFolder")] public string? BackupFolder { get; set; }
    [JsonPropertyName("status")] public HotasStatusDto? Status { get; set; }
}
