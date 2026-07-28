using Microsoft.Win32;

namespace IRacingSmartPlug.Services;

/// <summary>Manages the "run at Windows login" registry entry (per-user, no admin).</summary>
public sealed class StartupService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "iRacingSmartPlug";

    private static string ExePath => Environment.ProcessPath ?? "";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(ValueName) is string s &&
               s.Contains(ExePath, StringComparison.OrdinalIgnoreCase);
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                        ?? Registry.CurrentUser.CreateSubKey(RunKey);
        if (key is null) return;
        if (enabled)
            key.SetValue(ValueName, $"\"{ExePath}\" --tray"); // start hidden at login
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
