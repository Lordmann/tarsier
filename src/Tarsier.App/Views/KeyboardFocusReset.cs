using System.Windows;
using System.Windows.Input;

namespace Tarsier.App.Views;

/// <summary>
/// Takes keyboard focus away from whatever holds it. Clearing keyboard focus alone is not enough: the window
/// remembers its last focused element and hands focus straight back to it, so that memory is cleared too.
/// </summary>
public static class KeyboardFocusReset
{
    public static void Clear(DependencyObject anywhereInWindow)
    {
        if (Window.GetWindow(anywhereInWindow) is { } window)
        {
            FocusManager.SetFocusedElement(window, null);
        }

        Keyboard.ClearFocus();
    }

    /// <summary>True when the clicked element sits inside nothing that would take focus itself.</summary>
    public static bool IsEmptySpace(object? clicked)
    {
        for (var element = clicked as DependencyObject; element is not null and not Window; element = VisualParent(element))
        {
            if (element is UIElement { Focusable: true })
            {
                return false;
            }
        }

        return true;
    }

    private static DependencyObject? VisualParent(DependencyObject element) => element switch
    {
        System.Windows.Media.Visual or System.Windows.Media.Media3D.Visual3D => System.Windows.Media.VisualTreeHelper.GetParent(element),
        FrameworkContentElement content => content.Parent,
        _ => null
    };
}
