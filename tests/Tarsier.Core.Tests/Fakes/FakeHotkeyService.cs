using Tarsier.Core.Models;
using Tarsier.Core.Monitoring;

namespace Tarsier.Core.Tests.Fakes;

public sealed class FakeHotkeyService : IHotkeyService
{
    private readonly Dictionary<HotkeyAction, Hotkey> _registered = new();

    public event EventHandler<HotkeyAction>? HotkeyPressed;

    public int RegisterCount { get; private set; }

    public Hotkey? Registered(HotkeyAction action = HotkeyAction.Toggle) => _registered.GetValueOrDefault(action);

    public bool Register(HotkeyAction action, Hotkey hotkey)
    {
        _registered[action] = hotkey;
        RegisterCount++;
        return true;
    }

    public void Unregister() => _registered.Clear();

    public void Press(HotkeyAction action = HotkeyAction.Toggle) => HotkeyPressed?.Invoke(this, action);
}
