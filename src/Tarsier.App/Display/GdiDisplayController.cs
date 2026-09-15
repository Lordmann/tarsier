using System.Collections.Generic;
using System.Runtime.InteropServices;
using Tarsier.App.Interop;
using Tarsier.Core.Display;
using Tarsier.Core.Gamma;

namespace Tarsier.App.Display;

/// <summary>Drives each monitor's hardware gamma ramp through its own GDI device context.</summary>
public sealed class GdiDisplayController : IDisplayController
{
    private const string DisplayDriver = "DISPLAY";

    private readonly HashSet<string> _adjusted = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<DisplayInfo> GetDisplays()
    {
        var displays = new List<DisplayInfo>();

        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, _, _, _) =>
        {
            var info = new MonitorInfoEx { Size = Marshal.SizeOf<MonitorInfoEx>() };
            if (NativeMethods.GetMonitorInfo(monitor, ref info))
            {
                var isPrimary = (info.Flags & NativeMethods.MonitorInfoPrimary) != 0;
                displays.Add(new DisplayInfo(info.Device, DescribeDevice(info.Device), isPrimary));
            }

            return true;
        }, IntPtr.Zero);

        return displays;
    }

    public bool Apply(string monitorId, GammaRamp ramp)
    {
        if (!WithDeviceContext(monitorId, hdc => NativeMethods.SetDeviceGammaRamp(hdc, ramp.ToArray())))
        {
            return false;
        }

        _adjusted.Add(monitorId);
        return true;
    }

    public void Reset(string monitorId)
    {
        if (_adjusted.Contains(monitorId))
        {
            ResetDevice(monitorId);
        }
    }

    public void ResetAll()
    {
        foreach (var monitorId in _adjusted.ToArray())
        {
            Reset(monitorId);
        }
    }

    /// <summary>
    /// Restores every monitor whether or not this process changed it. A crashed or killed instance leaves its
    /// last ramp applied and Windows never undoes it, so a fresh start clears whatever it inherited.
    /// </summary>
    public void ResetInheritedRamps()
    {
        foreach (var display in GetDisplays())
        {
            ResetDevice(display.Id);
        }
    }

    private void ResetDevice(string monitorId)
    {
        WithDeviceContext(monitorId, hdc => NativeMethods.SetDeviceGammaRamp(hdc, GammaRampCalculator.Identity.ToArray()));
        _adjusted.Remove(monitorId);
    }

    private static bool WithDeviceContext(string monitorId, Func<IntPtr, bool> action)
    {
        if (string.IsNullOrWhiteSpace(monitorId))
        {
            return false;
        }

        var hdc = NativeMethods.CreateDC(DisplayDriver, monitorId, null, IntPtr.Zero);
        if (hdc == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            return action(hdc);
        }
        finally
        {
            NativeMethods.DeleteDC(hdc);
        }
    }

    private static string DescribeDevice(string deviceName)
    {
        var device = new DisplayDevice { Size = Marshal.SizeOf<DisplayDevice>() };
        return NativeMethods.EnumDisplayDevices(deviceName, 0, ref device, 0) && !string.IsNullOrWhiteSpace(device.DeviceString)
            ? device.DeviceString
            : deviceName;
    }
}
