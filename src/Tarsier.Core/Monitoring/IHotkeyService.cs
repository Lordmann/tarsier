using Tarsier.Core.Models;

namespace Tarsier.Core.Monitoring;

/// <summary>
/// Holds the system-wide hotkeys of at most one profile at a time. Only the profile whose application currently
/// owns the foreground is registered, which scopes the bindings to that app and lets separate profiles reuse a
/// combination.
/// </summary>
public interface IHotkeyService
{
    event EventHandler<HotkeyAction> HotkeyPressed;

    bool Register(HotkeyAction action, Hotkey hotkey);

    /// <summary>Releases every registered hotkey.</summary>
    void Unregister();
}
