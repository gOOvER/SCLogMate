using System;
using Microsoft.Win32;

namespace SCLogMate.Core;

/// <summary>
/// Verwaltet den Windows-Autostart über den CurrentUser Run-Registry-Schlüssel.
/// </summary>
public static class AutostartHelper
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppValueName = "SCLogMate";

    public static bool IsAutostartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(AppValueName) != null;
        }
        catch
        {
            return false;
        }
    }

    public static void SetAutostart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null) return;

            if (enable)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppValueName, $"\"{exePath}\" --minimized");
                }
            }
            else
            {
                key.DeleteValue(AppValueName, false);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("AutostartHelper.SetAutostart", ex);
        }
    }
}
