using System.Drawing;
using Microsoft.Win32;
using Application = System.Windows.Application;

namespace Tarsier.App.Views;

/// <summary>
/// Windows does not recolour small icons, so the ink mark is used on a light taskbar and the snow one on a dark
/// taskbar. The taskbar follows the system theme, not the app theme.
/// </summary>
internal static class TrayIcons
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string SystemThemeValue = "SystemUsesLightTheme";

    public static Icon ForCurrentTaskbar() => Load(IsTaskbarLight() ? "tray-on-light" : "tray-on-dark");

    /// <summary>The snow mark, for chrome the app itself paints dark.</summary>
    public static Icon ForDarkChrome() => Load("tray-on-dark");

    private static Icon Load(string name)
    {
        using var stream = Application.GetResourceStream(new Uri($"pack://application:,,,/Assets/{name}.ico"))!.Stream;
        return new Icon(stream, System.Windows.Forms.SystemInformation.SmallIconSize);
    }

    private static bool IsTaskbarLight()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        return key?.GetValue(SystemThemeValue) is 1;
    }
}
