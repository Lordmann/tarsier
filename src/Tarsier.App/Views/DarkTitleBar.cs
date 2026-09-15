using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using Tarsier.App.Interop;

namespace Tarsier.App.Views;

/// <summary>
/// Paints a window's non-client area dark so the title bar matches the content, and gives it the snow mark so the
/// icon reads on it. Only the small icon is replaced: the taskbar and Alt-Tab keep the tile from the executable.
/// The dark-mode attribute number moved during Windows 10, and older builds ignore both, so that part is best effort.
/// </summary>
internal static class DarkTitleBar
{
    private static readonly Lazy<Icon> TitleIcon = new(TrayIcons.ForDarkChrome);

    public static void Apply(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var enabled = 1;
        if (NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmUseImmersiveDarkMode, ref enabled, sizeof(int)) != 0)
        {
            NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DwmUseImmersiveDarkModeLegacy, ref enabled, sizeof(int));
        }

        NativeMethods.SendMessage(handle, NativeMethods.WmSetIcon, NativeMethods.IconSmall, TitleIcon.Value.Handle);
    }
}
