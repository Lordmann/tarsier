using System.IO;
using System.Text.Json;
using System.Windows;

namespace Tarsier.App.Views;

/// <summary>
/// Reopens the settings window at the size and screen position it was last closed at. The position is only put
/// back while it still lands on a connected monitor, and anything unreadable falls back to the defaults.
/// </summary>
public sealed class WindowPlacementMemory
{
    private readonly string _path;

    public WindowPlacementMemory(string path) => _path = path ?? throw new ArgumentNullException(nameof(path));

    public void Restore(Window window)
    {
        if (!File.Exists(_path))
        {
            return;
        }

        try
        {
            var placement = JsonSerializer.Deserialize<WindowPlacement>(File.ReadAllText(_path));
            if (placement is not { Width: > 0, Height: > 0 })
            {
                return;
            }

            window.Width = Math.Max(placement.Width, window.MinWidth);
            window.Height = Math.Max(placement.Height, window.MinHeight);

            if (placement is { Left: { } left, Top: { } top } && IsOnScreen(new Rect(left, top, window.Width, window.Height)))
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Left = left;
                window.Top = top;
            }
        }
        catch (JsonException)
        {
        }
        catch (IOException)
        {
        }
    }

    public void Remember(Window window)
    {
        if (window.WindowState != WindowState.Normal)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(
                new WindowPlacement(window.ActualWidth, window.ActualHeight, window.Left, window.Top)));
        }
        catch (IOException)
        {
        }
    }

    /// <summary>Enough of the title bar must remain visible to grab, wherever the monitors now are.</summary>
    private static bool IsOnScreen(Rect window)
    {
        const double grabbable = 40;
        var screens = new Rect(
            SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        var titleBar = new Rect(window.Left, window.Top, window.Width, grabbable);
        var visible = Rect.Intersect(titleBar, screens);
        return visible.Width >= grabbable && visible.Height >= grabbable;
    }

    private sealed record WindowPlacement(double Width, double Height, double? Left = null, double? Top = null);
}
