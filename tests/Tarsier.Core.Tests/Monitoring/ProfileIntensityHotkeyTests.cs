using Tarsier.Core.Gamma;
using Tarsier.Core.Models;
using Tarsier.Core.Monitoring;
using Tarsier.Core.Tests.Fakes;

namespace Tarsier.Core.Tests.Monitoring;

public class ProfileIntensityHotkeyTests
{
    private const string Game = "game.exe";
    private const string Other = "other.exe";
    private static readonly Hotkey Toggle = new(InputCode.Alt, 0x77);
    private static readonly Hotkey Increase = new(InputCode.WheelUp);
    private static readonly Hotkey Decrease = new(InputCode.WheelDown);
    private static readonly AdjustmentSettings Saved = new(40, -20, 0, 30);

    private readonly FakeForegroundWatcher _watcher = new();
    private readonly FakeDisplayController _display = new();
    private readonly FakeVibranceController _vibrance = new();
    private readonly FakeProfileProvider _profiles = new();
    private readonly FakeHotkeyService _hotkeys = new();
    private readonly ProfileApplicationService _service;

    public ProfileIntensityHotkeyTests()
    {
        _service = new ProfileApplicationService(_watcher, _display, _vibrance, _profiles, _hotkeys);
        _service.Start();
        _profiles.Set(new AppProfile(Game)
        {
            Settings = Saved,
            ToggleHotkey = Toggle,
            IncreaseIntensityHotkey = Increase,
            DecreaseIntensityHotkey = Decrease
        });
    }

    [Fact]
    public void EveryBoundHotkeyIsRegisteredWhileTheProfiledAppIsForeground()
    {
        _watcher.Focus(Game, Monitors.Primary);

        Assert.Equal(Toggle, _hotkeys.Registered(HotkeyAction.Toggle));
        Assert.Equal(Increase, _hotkeys.Registered(HotkeyAction.IncreaseIntensity));
        Assert.Equal(Decrease, _hotkeys.Registered(HotkeyAction.DecreaseIntensity));

        _watcher.Focus(Other, Monitors.Primary);
        Assert.Null(_hotkeys.Registered(HotkeyAction.IncreaseIntensity));
    }

    [Fact]
    public void IncreasingAppliesTheSteppedSettingsAndReportsThem()
    {
        _watcher.Focus(Game, Monitors.Primary);
        var adjusted = 0;
        _service.ActiveSettingsAdjusted += (_, _) => adjusted++;

        _hotkeys.Press(HotkeyAction.IncreaseIntensity);
        _hotkeys.Press(HotkeyAction.IncreaseIntensity);
        _hotkeys.Press(HotkeyAction.DecreaseIntensity);

        var expected = new AdjustmentSettings(41, -19, 1, 30);
        Assert.Equal(expected, _service.ActiveSettings);
        Assert.Equal(GammaRampCalculator.Calculate(expected), _display.Applied[Monitors.Primary]);
        Assert.Equal(3, adjusted);
    }

    [Fact]
    public void APressThatChangesNothingIsNotReported()
    {
        _profiles.Set(new AppProfile(Game) { Settings = new AdjustmentSettings(100, 100, 100), IncreaseIntensityHotkey = Increase });
        _watcher.Focus(Game, Monitors.Primary);
        var adjusted = 0;
        _service.ActiveSettingsAdjusted += (_, _) => adjusted++;

        _hotkeys.Press(HotkeyAction.IncreaseIntensity);

        Assert.Equal(0, adjusted);
    }

    [Fact]
    public void TheUnsavedAdjustmentSurvivesAltTabbingAwayAndBack()
    {
        _watcher.Focus(Game, Monitors.Primary);
        _hotkeys.Press(HotkeyAction.IncreaseIntensity);

        _watcher.Focus(Other, Monitors.Primary);
        _watcher.Focus(Game, Monitors.Primary);

        var expected = GammaRampCalculator.Calculate(new AdjustmentSettings(41, -19, 1, 30));
        Assert.Equal(expected, _display.Applied[Monitors.Primary]);
    }

    [Fact]
    public void DiscardingTheAdjustmentGoesBackToTheSavedSettings()
    {
        _watcher.Focus(Game, Monitors.Primary);
        _hotkeys.Press(HotkeyAction.IncreaseIntensity);

        _service.DiscardUnsaved(Game);
        _service.Refresh();

        Assert.Equal(Saved, _service.ActiveSettings);
        Assert.Equal(GammaRampCalculator.Calculate(Saved), _display.Applied[Monitors.Primary]);
    }

    [Fact]
    public void AdjustingWhileToggledOffLeavesTheMonitorRestoredUntilToggledBackOn()
    {
        _watcher.Focus(Game, Monitors.Primary);
        _hotkeys.Press(HotkeyAction.Toggle);

        _hotkeys.Press(HotkeyAction.IncreaseIntensity);
        Assert.Empty(_display.Applied);

        _hotkeys.Press(HotkeyAction.Toggle);
        Assert.Equal(GammaRampCalculator.Calculate(new AdjustmentSettings(41, -19, 1, 30)), _display.Applied[Monitors.Primary]);
    }

    [Fact]
    public void TheHotkeysAreIgnoredWhenNoProfileIsActive()
    {
        _watcher.Focus(Other, Monitors.Primary);

        _hotkeys.Press(HotkeyAction.IncreaseIntensity);

        Assert.Null(_service.ActiveSettings);
        Assert.Empty(_display.Applied);
    }
}
