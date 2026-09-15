using System.Windows.Input;
using Tarsier.App.ViewModels;
using Tarsier.Core.Models;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace Tarsier.App.Views;

/// <summary>
/// Records a combination into the <see cref="HotkeyBindingViewModel"/> it is bound to. Clicking the box starts
/// listening: every key, mouse button and wheel notch is collected, and the combination is committed the moment
/// nothing is held down any more. Escape, Backspace or Delete on their own clear it. Either way the box then gives
/// up focus so it stops listening until clicked again.
/// </summary>
public sealed class HotkeyBox : TextBox
{
    private readonly HashSet<uint> _gesture = new();
    private readonly HashSet<uint> _held = new();

    public HotkeyBox()
    {
        IsReadOnly = true;
        IsUndoEnabled = false;
        ContextMenu = null;
    }

    private HotkeyBindingViewModel? Binding => DataContext as HotkeyBindingViewModel;

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        e.Handled = true;
        if (e.IsRepeat)
        {
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (_gesture.Count == 0 && key is Key.Escape or Key.Back or Key.Delete)
        {
            if (Binding is { } binding)
            {
                binding.Hotkey = null;
            }

            StopListening();
            return;
        }

        Press(InputCode.Normalize((uint)KeyInterop.VirtualKeyFromKey(key)));
    }

    protected override void OnPreviewKeyUp(KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        Release(InputCode.Normalize((uint)KeyInterop.VirtualKeyFromKey(key)));
    }

    /// <summary>
    /// Mouse input never reaches the text editor, which would otherwise capture the mouse on the focusing click
    /// and keep every later click for itself. A plain left click only starts or keeps listening; the left button
    /// is recorded solely as part of a combination with something already held.
    /// </summary>
    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (!IsKeyboardFocused)
        {
            Focus();
            return;
        }

        if (e.ChangedButton == MouseButton.Left && _gesture.Count == 0)
        {
            return;
        }

        Press(CodeOf(e.ChangedButton));
    }

    protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
    {
        e.Handled = true;
        Release(CodeOf(e.ChangedButton));
    }

    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        if (!IsKeyboardFocused)
        {
            base.OnPreviewMouseWheel(e);
            return;
        }

        e.Handled = true;
        _gesture.Add(e.Delta > 0 ? InputCode.WheelUp : InputCode.WheelDown);
        CommitWhenReleased();
    }

    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        _gesture.Clear();
        _held.Clear();
        base.OnLostKeyboardFocus(e);
    }

    private void Press(uint input)
    {
        _gesture.Add(input);
        _held.Add(input);
    }

    private void Release(uint input)
    {
        _held.Remove(input);
        CommitWhenReleased();
    }

    /// <summary>The wheel is never held, so a wheel-only combination commits on the notch itself.</summary>
    private void CommitWhenReleased()
    {
        if (_gesture.Count == 0 || _held.Count > 0)
        {
            return;
        }

        if (Binding is { } binding)
        {
            binding.Hotkey = new Hotkey(_gesture);
        }

        _gesture.Clear();
        StopListening();
    }

    private void StopListening() => KeyboardFocusReset.Clear(this);

    private static uint CodeOf(MouseButton button) => button switch
    {
        MouseButton.Left => InputCode.LeftButton,
        MouseButton.Right => InputCode.RightButton,
        MouseButton.Middle => InputCode.MiddleButton,
        MouseButton.XButton1 => InputCode.XButton1,
        _ => InputCode.XButton2
    };
}
