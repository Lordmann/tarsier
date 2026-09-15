using Tarsier.App.Interop;
using Tarsier.Core.Models;
using Tarsier.Core.Monitoring;

namespace Tarsier.App.Monitoring;

/// <summary>
/// Reports foreground changes via WinEvent hooks. Window moves and restores are watched too, so dragging a
/// profiled window to another monitor moves the adjustment with it. Must be started on a thread with a message loop.
/// </summary>
public sealed class WinEventForegroundWatcher : IForegroundWatcher, IDisposable
{
    private readonly WinEventProc _callback;

    private IntPtr _foregroundHook;
    private IntPtr _windowStateHook;
    private ForegroundWindowInfo? _last;

    public WinEventForegroundWatcher() => _callback = OnWinEvent;

    public event EventHandler<ForegroundWindowInfo?>? ForegroundChanged;

    public void Start()
    {
        if (_foregroundHook != IntPtr.Zero)
        {
            return;
        }

        _foregroundHook = Hook(NativeMethods.EventSystemForeground, NativeMethods.EventSystemForeground);
        _windowStateHook = Hook(NativeMethods.EventSystemMoveSizeEnd, NativeMethods.EventSystemMinimizeEnd);
        Publish();
    }

    public void Stop()
    {
        Unhook(ref _foregroundHook);
        Unhook(ref _windowStateHook);
        _last = null;
    }

    public void Dispose() => Stop();

    private IntPtr Hook(uint first, uint last) => NativeMethods.SetWinEventHook(
        first,
        last,
        IntPtr.Zero,
        _callback,
        0,
        0,
        NativeMethods.WinEventOutOfContext);

    private static void Unhook(ref IntPtr hook)
    {
        if (hook == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.UnhookWinEvent(hook);
        hook = IntPtr.Zero;
    }

    private void OnWinEvent(IntPtr hook, uint eventType, IntPtr window, int idObject, int idChild, uint thread, uint time) =>
        Publish();

    private void Publish()
    {
        var current = Resolve();
        if (current == _last)
        {
            return;
        }

        _last = current;
        ForegroundChanged?.Invoke(this, current);
    }

    private static ForegroundWindowInfo? Resolve()
    {
        var window = NativeMethods.GetForegroundWindow();
        var executable = WindowInspector.GetExecutablePath(window);
        var monitorId = WindowInspector.GetMonitorId(window);

        return executable is null || monitorId is null
            ? null
            : new ForegroundWindowInfo(ExecutableKey.Normalize(executable), monitorId);
    }
}
