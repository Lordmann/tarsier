using System.Runtime.InteropServices;
using System.Windows.Threading;
using Tarsier.App.Interop;
using Tarsier.Core.Models;
using Tarsier.Core.Monitoring;

namespace Tarsier.App.Monitoring;

/// <summary>
/// Watches for one profile's combinations through low-level keyboard and mouse hooks, which unlike RegisterHotKey
/// see mouse buttons, the wheel and any mix of keys. The hooks exist only while something is registered, and every
/// input is passed on untouched so the foreground application still receives it.
/// </summary>
public sealed class GlobalHotkeyService : IHotkeyService, IDisposable
{
    private readonly Dictionary<HotkeyAction, Hotkey> _bindings = new();
    private readonly HashSet<uint> _down = new();
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
    private readonly LowLevelHookProc _keyboardCallback;
    private readonly LowLevelHookProc _mouseCallback;
    private IntPtr _keyboardHook;
    private IntPtr _mouseHook;

    public GlobalHotkeyService()
    {
        _keyboardCallback = OnKeyboardInput;
        _mouseCallback = OnMouseInput;
    }

    public event EventHandler<HotkeyAction>? HotkeyPressed;

    public bool Register(HotkeyAction action, Hotkey hotkey)
    {
        _bindings[action] = hotkey ?? throw new ArgumentNullException(nameof(hotkey));
        InstallHooks();
        return _keyboardHook != IntPtr.Zero && _mouseHook != IntPtr.Zero;
    }

    public void Unregister()
    {
        _bindings.Clear();
        _down.Clear();
        RemoveHooks();
    }

    public void Dispose() => Unregister();

    private void InstallHooks()
    {
        var module = NativeMethods.GetModuleHandle(null);
        if (_keyboardHook == IntPtr.Zero)
        {
            _keyboardHook = NativeMethods.SetWindowsHookEx(NativeMethods.LowLevelKeyboardHook, _keyboardCallback, module, 0);
        }

        if (_mouseHook == IntPtr.Zero)
        {
            _mouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.LowLevelMouseHook, _mouseCallback, module, 0);
        }
    }

    private void RemoveHooks()
    {
        if (_keyboardHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }

        if (_mouseHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }
    }

    private IntPtr OnKeyboardInput(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0)
        {
            var key = InputCode.Normalize(Marshal.PtrToStructure<KeyboardHookInput>(lParam).VirtualKey);
            switch (wParam.ToInt32())
            {
                case NativeMethods.WmKeyDown or NativeMethods.WmSysKeyDown:
                    Press(key);
                    break;
                case NativeMethods.WmKeyUp or NativeMethods.WmSysKeyUp:
                    _down.Remove(key);
                    break;
            }
        }

        return NativeMethods.CallNextHookEx(_keyboardHook, code, wParam, lParam);
    }

    private IntPtr OnMouseInput(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0)
        {
            var data = Marshal.PtrToStructure<MouseHookInput>(lParam).Data >> 16;
            switch (wParam.ToInt32())
            {
                case NativeMethods.WmLButtonDown:
                    Press(InputCode.LeftButton);
                    break;
                case NativeMethods.WmRButtonDown:
                    Press(InputCode.RightButton);
                    break;
                case NativeMethods.WmMButtonDown:
                    Press(InputCode.MiddleButton);
                    break;
                case NativeMethods.WmXButtonDown:
                    Press(data == NativeMethods.XButton1 ? InputCode.XButton1 : InputCode.XButton2);
                    break;
                case NativeMethods.WmMouseWheel:
                    Fire((short)data > 0 ? InputCode.WheelUp : InputCode.WheelDown, repeat: false);
                    break;
                case NativeMethods.WmLButtonUp:
                    _down.Remove(InputCode.LeftButton);
                    break;
                case NativeMethods.WmRButtonUp:
                    _down.Remove(InputCode.RightButton);
                    break;
                case NativeMethods.WmMButtonUp:
                    _down.Remove(InputCode.MiddleButton);
                    break;
                case NativeMethods.WmXButtonUp:
                    _down.Remove(data == NativeMethods.XButton1 ? InputCode.XButton1 : InputCode.XButton2);
                    break;
            }
        }

        return NativeMethods.CallNextHookEx(_mouseHook, code, wParam, lParam);
    }

    /// <summary>Keyboard auto-repeat shows up as further down events for a key that is already down.</summary>
    private void Press(uint input) => Fire(input, repeat: !_down.Add(input));

    /// <summary>
    /// Fires the binding completed by this input, preferring the one with the most inputs when several match so that
    /// F4 + wheel wins over a bare wheel. Repeats keep stepping intensity but never toggle.
    /// </summary>
    private void Fire(uint input, bool repeat)
    {
        var matched = _bindings
            .Where(binding => binding.Value.Inputs.Contains(input) && binding.Value.Inputs.All(other => other == input || IsHeld(other)))
            .OrderByDescending(binding => binding.Value.Inputs.Length)
            .Select(binding => (HotkeyAction?)binding.Key)
            .FirstOrDefault();

        if (matched is not { } action || (repeat && action == HotkeyAction.Toggle))
        {
            return;
        }

        _dispatcher.BeginInvoke(() => HotkeyPressed?.Invoke(this, action));
    }

    private static bool IsHeld(uint input) =>
        !InputCode.IsWheel(input) && (NativeMethods.GetAsyncKeyState((int)input) & NativeMethods.KeyDownState) != 0;
}
