using Tarsier.Core.Gamma;
using Tarsier.Core.Models;
using Tarsier.Core.Monitoring;
using Tarsier.Core.Tests.Fakes;

namespace Tarsier.Core.Tests.Monitoring;

public class ProfileApplicationServiceTests
{
    private const string Game = "game.exe";
    private const string Browser = "browser.exe";

    private readonly FakeForegroundWatcher _watcher = new();
    private readonly FakeDisplayController _display = new();
    private readonly FakeVibranceController _vibrance = new();
    private readonly FakeProfileProvider _profiles = new();
    private readonly FakeHotkeyService _hotkeys = new();
    private readonly ProfileApplicationService _service;

    public ProfileApplicationServiceTests()
    {
        _service = new ProfileApplicationService(_watcher, _display, _vibrance, _profiles, _hotkeys);
        _service.Start();
    }

    private static AppProfile Profile(
        string executable = Game,
        bool enabled = true,
        int gamma = 70,
        int vibrance = 0,
        Hotkey? hotkey = null) =>
        new(executable)
        {
            Enabled = enabled,
            Settings = new AdjustmentSettings(gamma, 0, 0, vibrance),
            ToggleHotkey = hotkey
        };

    [Fact]
    public void FocusingAProfiledAppAppliesItsVibranceAlongsideTheRamp()
    {
        _profiles.Set(Profile(vibrance: 60));

        _watcher.Focus(Game, Monitors.Primary);

        Assert.Equal(60, _vibrance.Applied[Monitors.Primary]);
    }

    [Fact]
    public void LosingTheForegroundRestoresVibranceWithTheRamp()
    {
        _profiles.Set(Profile(vibrance: 60));

        _watcher.Focus(Game, Monitors.Primary);
        _watcher.Focus(Browser, Monitors.Primary);

        Assert.Empty(_vibrance.Applied);
    }

    [Fact]
    public void MovingToAnotherMonitorRestoresVibranceOnTheOneLeftBehind()
    {
        _profiles.Set(Profile(vibrance: 60));

        _watcher.Focus(Game, Monitors.Primary);
        _watcher.Focus(Game, Monitors.Secondary);

        Assert.False(_vibrance.Applied.ContainsKey(Monitors.Primary));
        Assert.Equal(60, _vibrance.Applied[Monitors.Secondary]);
    }

    [Fact]
    public void DisposingRestoresVibranceOnEveryMonitor()
    {
        _profiles.Set(Profile(vibrance: 60));
        _watcher.Focus(Game, Monitors.Primary);

        _service.Dispose();

        Assert.Equal(1, _vibrance.ResetAllCount);
    }

    [Fact]
    public void FocusingAProfiledAppAppliesItsRampToTheHostingMonitor()
    {
        var profile = Profile();
        _profiles.Set(profile);

        _watcher.Focus(Game, Monitors.Primary);

        Assert.Equal(GammaRampCalculator.Calculate(profile.Settings), _display.Applied[Monitors.Primary]);
        Assert.False(_display.Applied.ContainsKey(Monitors.Secondary));
    }

    [Fact]
    public void OnlyTheMonitorHostingTheWindowIsTouched()
    {
        _profiles.Set(Profile());

        _watcher.Focus(Game, Monitors.Secondary);

        Assert.True(_display.Applied.ContainsKey(Monitors.Secondary));
        Assert.False(_display.Applied.ContainsKey(Monitors.Primary));
    }

    [Fact]
    public void FocusingAnUnprofiledAppRestoresTheMonitor()
    {
        _profiles.Set(Profile());
        _watcher.Focus(Game, Monitors.Primary);

        _watcher.Focus(Browser, Monitors.Primary);

        Assert.Empty(_display.Applied);
        Assert.Contains($"reset:{Monitors.Primary}", _display.Log);
    }

    [Fact]
    public void LosingTheForegroundEntirelyRestoresTheMonitor()
    {
        _profiles.Set(Profile());
        _watcher.Focus(Game, Monitors.Primary);

        _watcher.FocusNothing();

        Assert.Empty(_display.Applied);
    }

    [Fact]
    public void MovingTheAppToAnotherMonitorRestoresTheOneItLeft()
    {
        _profiles.Set(Profile());
        _watcher.Focus(Game, Monitors.Primary);

        _watcher.Focus(Game, Monitors.Secondary);

        Assert.Contains($"reset:{Monitors.Primary}", _display.Log);
        Assert.True(_display.Applied.ContainsKey(Monitors.Secondary));
        Assert.False(_display.Applied.ContainsKey(Monitors.Primary));
    }

    [Fact]
    public void DisabledProfilesAreIgnored()
    {
        _profiles.Set(Profile(enabled: false));

        _watcher.Focus(Game, Monitors.Primary);

        Assert.Empty(_display.Applied);
    }

    [Fact]
    public void PausingRestoresTheActiveMonitorAndResumingReappliesIt()
    {
        _profiles.Set(Profile());
        _watcher.Focus(Game, Monitors.Primary);

        _service.IsPaused = true;
        Assert.Empty(_display.Applied);

        _service.IsPaused = false;
        Assert.True(_display.Applied.ContainsKey(Monitors.Primary));
    }

    [Fact]
    public void RefreshPicksUpEditsMadeWhileTheAppIsFocused()
    {
        _profiles.Set(Profile(gamma: 70));
        _watcher.Focus(Game, Monitors.Primary);

        var edited = Profile(gamma: 30);
        _profiles.Set(edited);
        _service.Refresh();

        Assert.Equal(GammaRampCalculator.Calculate(edited.Settings), _display.Applied[Monitors.Primary]);
    }

    [Fact]
    public void StoppingRestoresTheActiveMonitor()
    {
        _profiles.Set(Profile());
        _watcher.Focus(Game, Monitors.Primary);

        _service.Stop();

        Assert.Empty(_display.Applied);
        Assert.False(_watcher.IsRunning);
    }
}
