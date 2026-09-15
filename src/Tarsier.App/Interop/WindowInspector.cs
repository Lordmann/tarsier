using System.Text;
using Tarsier.App.Interop;

namespace Tarsier.App.Interop;

/// <summary>Resolves the executable and host monitor behind a window handle.</summary>
internal static class WindowInspector
{
    public static string? GetExecutablePath(IntPtr window)
    {
        if (window == IntPtr.Zero || NativeMethods.GetWindowThreadProcessId(window, out var processId) == 0)
        {
            return null;
        }

        var process = NativeMethods.OpenProcess(NativeMethods.ProcessQueryLimitedInformation, false, processId);
        if (process == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var capacity = 1024u;
            var buffer = new StringBuilder((int)capacity);
            return NativeMethods.QueryFullProcessImageName(process, 0, buffer, ref capacity) ? buffer.ToString() : null;
        }
        finally
        {
            NativeMethods.CloseHandle(process);
        }
    }

    public static string? GetMonitorId(IntPtr window)
    {
        if (window == IntPtr.Zero)
        {
            return null;
        }

        var monitor = NativeMethods.MonitorFromWindow(window, NativeMethods.MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return null;
        }

        var info = new MonitorInfoEx { Size = System.Runtime.InteropServices.Marshal.SizeOf<MonitorInfoEx>() };
        return NativeMethods.GetMonitorInfo(monitor, ref info) ? info.Device : null;
    }

    public static string GetTitle(IntPtr window)
    {
        var length = NativeMethods.GetWindowTextLength(window);
        if (length <= 0)
        {
            return string.Empty;
        }

        var buffer = new StringBuilder(length + 1);
        NativeMethods.GetWindowText(window, buffer, buffer.Capacity);
        return buffer.ToString();
    }
}
