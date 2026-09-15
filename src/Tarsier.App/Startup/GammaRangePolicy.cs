using System.Diagnostics;
using Microsoft.Win32;

namespace Tarsier.App.Startup;

/// <summary>
/// Windows clamps gamma ramps to a narrow band around the identity curve unless this policy value is raised,
/// which makes the sliders feel inert. Changing it needs elevation and a sign-out to take effect.
/// </summary>
public sealed class GammaRangePolicy
{
    public const string ElevationArgument = "--enable-gamma-range";

    private const string IcmKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ICM";
    private const string ValueName = "GdiIcmGammaRange";
    private const int FullRange = 256;

    public bool IsFullRangeEnabled
    {
        get
        {
            using var key = Registry.LocalMachine.OpenSubKey(IcmKey);
            return key?.GetValue(ValueName) as int? == FullRange;
        }
    }

    /// <summary>Writes the policy value directly. Only succeeds in an elevated process.</summary>
    public bool TryEnableFullRange()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(IcmKey);
            key?.SetValue(ValueName, FullRange, RegistryValueKind.DWord);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (System.Security.SecurityException)
        {
            return false;
        }
    }

    /// <summary>Relaunches this executable elevated so it can write the policy value, then exits that helper.</summary>
    public bool RequestElevatedEnable()
    {
        var executable = Environment.ProcessPath;
        if (executable is null)
        {
            return false;
        }

        try
        {
            var process = Process.Start(new ProcessStartInfo(executable, ElevationArgument)
            {
                UseShellExecute = true,
                Verb = "runas"
            });

            process?.WaitForExit();
            return IsFullRangeEnabled;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
