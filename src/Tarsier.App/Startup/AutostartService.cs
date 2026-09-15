using System.Diagnostics;
using Microsoft.Win32;

namespace Tarsier.App.Startup;

/// <summary>Registers the app under the per-user Run key so profiles are active from sign-in.</summary>
public sealed class AutostartService
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Tarsier";
    private const string LegacyValueName = "IngameGamma";

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is not null;
        }
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key is null)
        {
            return;
        }

        if (enabled)
        {
            key.SetValue(ValueName, RunCommand);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    /// <summary>
    /// The rename changed both the registry value name and the executable path, so an install that was set to
    /// start with Windows would quietly stop doing so. This re-points it once and drops the stale entry.
    /// </summary>
    public void MigrateLegacyEntry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key?.GetValue(LegacyValueName) is null)
        {
            return;
        }

        key.DeleteValue(LegacyValueName, throwOnMissingValue: false);
        key.SetValue(ValueName, RunCommand);
    }

    private static string RunCommand => $"\"{ExecutablePath}\" --tray";

    private static string ExecutablePath =>
        Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
}
