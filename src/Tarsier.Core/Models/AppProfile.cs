using System.Text.Json.Serialization;

namespace Tarsier.Core.Models;

/// <summary>Adjustments bound to a single executable, applied to whichever monitor currently hosts its foreground window.</summary>
public sealed record AppProfile
{
    private readonly string _executableName = string.Empty;

    [JsonConstructor]
    public AppProfile(string executableName) => ExecutableName = executableName;

    public string ExecutableName
    {
        get => _executableName;
        init => _executableName = ExecutableKey.Normalize(value);
    }

    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Where the executable was when the profile was created; only used to show its icon, matching stays by name.</summary>
    public string? ExecutablePath { get; init; }

    public bool Enabled { get; init; } = true;

    public AdjustmentSettings Settings { get; init; } = AdjustmentSettings.Default;

    public Hotkey? ToggleHotkey { get; init; }

    public Hotkey? IncreaseIntensityHotkey { get; init; }

    public Hotkey? DecreaseIntensityHotkey { get; init; }

    public Hotkey? HotkeyFor(HotkeyAction action) => action switch
    {
        HotkeyAction.Toggle => ToggleHotkey,
        HotkeyAction.IncreaseIntensity => IncreaseIntensityHotkey,
        HotkeyAction.DecreaseIntensity => DecreaseIntensityHotkey,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
    };
}
