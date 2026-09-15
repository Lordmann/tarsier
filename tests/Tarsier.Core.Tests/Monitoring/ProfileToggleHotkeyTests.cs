using Tarsier.Core.Models;
using Tarsier.Core.Monitoring;
using Tarsier.Core.Tests.Fakes;

namespace Tarsier.Core.Tests.Monitoring;

public class ProfileToggleHotkeyTests
{
    private const string Game = "game.exe";
    private const string Other = "other.exe";
    private static readonly Hotkey F8 = new(InputCode.Alt, 0x77);

    private readonly FakeForegroundWatcher _watcher = new();
    private readonly FakeDisplayController _display = new();
    private readonly FakeVibranceController _vibrance = new();
    private readonly FakeProfileProvider _profiles = new();
    private readonly FakeHotkeyService _hotkeys = new();
    private readonly ProfileApplicationService _service;

    public ProfileToggleHotkeyTests()
    {
        _service = new ProfileApplicationService(_watcher, _display, _vibrance, _profiles, _hotkeys);
        _service.Start();
        _profiles.Set(new AppProfile(Game)
        {
            Settings = new AdjustmentSettings(70, 60, 55),
            ToggleHotkey = F8
        });
    }

    [Fact]
    public void TheHotkeyIsRegisteredOnlyWhileTheProfiledAppIsForeground()
    {
        Assert.Null(_hotkeys.Registered());

        _watcher.Focus(Game, Monitors.Primary);
        Assert.Equal(F8, _hotkeys.Registered());

        _watcher.Focus(Other, Monitors.Primary);
        Assert.Null(_hotkeys.Registered());
    }

    [Fact]
    public void PressingTheHotkeyTurnsTheProfileOffAndBackOn()
    {
        _watcher.Focus(Game, Monitors.Primary);

        _hotkeys.Press();
        Assert.Empty(_display.Applied);
        Assert.True(_service.IsActiveProfileToggledOff);

        _hotkeys.Press();
        Assert.True(_display.Applied.ContainsKey(Monitors.Primary));
        Assert.False(_service.IsActiveProfileToggledOff);
    }

    [Fact]
    public void ATogglingOffSurvivesAltTabbingAwayAndBack()
    {
        _watcher.Focus(Game, Monitors.Primary);
        _hotkeys.Press();

        _watcher.Focus(Other, Monitors.Primary);
        _watcher.Focus(Game, Monitors.Primary);

        Assert.True(_service.IsActiveProfileToggledOff);
        Assert.Empty(_display.Applied);
    }

    [Fact]
    public void TheHotkeyIsIgnoredWhenNoProfileIsActive()
    {
        _watcher.Focus(Other, Monitors.Primary);

        _hotkeys.Press();

        Assert.Empty(_display.Applied);
    }

    [Fact]
    public void AProfileWithoutAHotkeyRegistersNothing()
    {
        _profiles.Set(new AppProfile(Game) { Settings = new AdjustmentSettings(70, 0, 0) });

        _watcher.Focus(Game, Monitors.Primary);

        Assert.Null(_hotkeys.Registered());
        Assert.Equal(0, _hotkeys.RegisterCount);
    }

    [Fact]
    public void StayingFocusedDoesNotReRegisterTheSameHotkey()
    {
        _watcher.Focus(Game, Monitors.Primary);
        _service.Refresh();
        _service.Refresh();

        Assert.Equal(1, _hotkeys.RegisterCount);
    }
}
