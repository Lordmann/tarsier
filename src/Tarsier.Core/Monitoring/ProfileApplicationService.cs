using Tarsier.Core.Display;
using Tarsier.Core.Gamma;
using Tarsier.Core.Models;
using Tarsier.Core.Profiles;

namespace Tarsier.Core.Monitoring;

/// <summary>
/// Applies a profile's ramp and vibrance to the monitor hosting its application while that application holds the
/// foreground, and restores the monitor as soon as it does not. Intensity hotkeys adjust a profile's settings in
/// memory only; the adjustment is kept for the session until the editor saves or discards it.
/// </summary>
public sealed class ProfileApplicationService : IDisposable
{
    private readonly IForegroundWatcher _watcher;
    private readonly IDisplayController _display;
    private readonly IVibranceController _vibrance;
    private readonly IProfileProvider _profiles;
    private readonly IHotkeyService _hotkeys;
    private readonly HashSet<string> _toggledOff = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AdjustmentSettings> _unsaved = new(StringComparer.Ordinal);

    private ForegroundWindowInfo? _foreground;
    private ActiveBinding? _active;
    private bool _paused;
    private bool _running;

    public ProfileApplicationService(
        IForegroundWatcher watcher,
        IDisplayController display,
        IVibranceController vibrance,
        IProfileProvider profiles,
        IHotkeyService hotkeys)
    {
        _watcher = watcher ?? throw new ArgumentNullException(nameof(watcher));
        _display = display ?? throw new ArgumentNullException(nameof(display));
        _vibrance = vibrance ?? throw new ArgumentNullException(nameof(vibrance));
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _hotkeys = hotkeys ?? throw new ArgumentNullException(nameof(hotkeys));
    }

    public event EventHandler? ActiveProfileChanged;

    /// <summary>Raised when an intensity hotkey changes the active profile's unsaved settings.</summary>
    public event EventHandler? ActiveSettingsAdjusted;

    public AppProfile? ActiveProfile => _active?.Profile;

    /// <summary>The settings currently in effect for the active profile, including any unsaved intensity adjustment.</summary>
    public AdjustmentSettings? ActiveSettings => _active is null ? null : EffectiveSettings(_active.Profile);

    public bool IsActiveProfileToggledOff => _active is not null && _toggledOff.Contains(_active.Profile.ExecutableName);

    public bool IsPaused
    {
        get => _paused;
        set
        {
            if (_paused == value)
            {
                return;
            }

            _paused = value;
            Evaluate();
        }
    }

    public void Start()
    {
        if (_running)
        {
            return;
        }

        _running = true;
        _watcher.ForegroundChanged += OnForegroundChanged;
        _hotkeys.HotkeyPressed += OnHotkeyPressed;
        _watcher.Start();
    }

    public void Stop()
    {
        if (!_running)
        {
            return;
        }

        _running = false;
        _watcher.ForegroundChanged -= OnForegroundChanged;
        _hotkeys.HotkeyPressed -= OnHotkeyPressed;
        _watcher.Stop();
        Deactivate();
    }

    /// <summary>Re-evaluates the current foreground window, picking up profile edits made while it was focused.</summary>
    public void Refresh() => Evaluate();

    /// <summary>Drops the unsaved intensity adjustment of a profile, once the editor has saved or cancelled it.</summary>
    public void DiscardUnsaved(string executableName) => _unsaved.Remove(executableName);

    public void Dispose()
    {
        Stop();
        _display.ResetAll();
        _vibrance.ResetAll();
    }

    private void OnForegroundChanged(object? sender, ForegroundWindowInfo? foreground)
    {
        _foreground = foreground;
        Evaluate();
    }

    private void OnHotkeyPressed(object? sender, HotkeyAction action)
    {
        if (_active is null)
        {
            return;
        }

        switch (action)
        {
            case HotkeyAction.Toggle:
                ToggleActive();
                break;
            case HotkeyAction.IncreaseIntensity:
                AdjustActive(+1);
                break;
            case HotkeyAction.DecreaseIntensity:
                AdjustActive(-1);
                break;
        }
    }

    private void ToggleActive()
    {
        var key = _active!.Profile.ExecutableName;
        if (!_toggledOff.Remove(key))
        {
            _toggledOff.Add(key);
        }

        ApplyActive();
        ActiveProfileChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AdjustActive(int step)
    {
        var current = EffectiveSettings(_active!.Profile);
        var adjusted = current.Intensified(step);
        if (adjusted == current)
        {
            return;
        }

        _unsaved[_active.Profile.ExecutableName] = adjusted;
        ApplyActive();
        ActiveSettingsAdjusted?.Invoke(this, EventArgs.Empty);
    }

    private void Evaluate()
    {
        var profile = ResolveProfile();
        if (profile is null || _foreground is null)
        {
            Deactivate();
            return;
        }

        var monitorId = _foreground.MonitorId;
        if (_active is not null && !string.Equals(_active.MonitorId, monitorId, StringComparison.Ordinal))
        {
            Restore(_active.MonitorId);
        }

        SyncHotkeys(profile);
        _active = new ActiveBinding(profile, monitorId);
        ApplyActive();
        ActiveProfileChanged?.Invoke(this, EventArgs.Empty);
    }

    private AppProfile? ResolveProfile()
    {
        if (_paused || _foreground is null)
        {
            return null;
        }

        var profile = _profiles.Find(_foreground.ExecutableName);
        return profile is { Enabled: true } ? profile : null;
    }

    private void SyncHotkeys(AppProfile profile)
    {
        var actions = Enum.GetValues<HotkeyAction>();
        if (_active is not null && actions.All(action => _active.Profile.HotkeyFor(action) == profile.HotkeyFor(action)))
        {
            return;
        }

        _hotkeys.Unregister();
        foreach (var action in actions)
        {
            if (profile.HotkeyFor(action) is { } hotkey)
            {
                _hotkeys.Register(action, hotkey);
            }
        }
    }

    private AdjustmentSettings EffectiveSettings(AppProfile profile) =>
        _unsaved.TryGetValue(profile.ExecutableName, out var unsaved) ? unsaved : profile.Settings;

    private void ApplyActive()
    {
        if (_active is null)
        {
            return;
        }

        if (_toggledOff.Contains(_active.Profile.ExecutableName))
        {
            Restore(_active.MonitorId);
            return;
        }

        var settings = EffectiveSettings(_active.Profile);
        _display.Apply(_active.MonitorId, GammaRampCalculator.Calculate(settings));
        _vibrance.Apply(_active.MonitorId, settings.Vibrance);
    }

    private void Restore(string monitorId)
    {
        _display.Reset(monitorId);
        _vibrance.Reset(monitorId);
    }

    private void Deactivate()
    {
        if (_active is null)
        {
            return;
        }

        Restore(_active.MonitorId);
        _active = null;
        _hotkeys.Unregister();
        ActiveProfileChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed record ActiveBinding(AppProfile Profile, string MonitorId);
}
