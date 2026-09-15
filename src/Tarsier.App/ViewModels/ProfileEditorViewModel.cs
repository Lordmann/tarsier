using System.Windows.Media;
using Tarsier.App.Interop;
using Tarsier.Core.Models;

namespace Tarsier.App.ViewModels;

/// <summary>Editable view of one profile. Raises <see cref="Edited"/> so the preview can follow the sliders.</summary>
public sealed class ProfileEditorViewModel : ObservableObject
{
    private AppProfile _saved;
    private string _displayName;
    private bool _enabled;
    private int _gamma;
    private int _brightness;
    private int _contrast;
    private int _vibrance;
    private readonly Lazy<ImageSource?> _icon;

    public ProfileEditorViewModel(AppProfile profile)
    {
        _saved = profile ?? throw new ArgumentNullException(nameof(profile));
        _icon = new Lazy<ImageSource?>(() => ExecutableIcons.Load(profile.ExecutablePath));
        _displayName = profile.DisplayName;
        _enabled = profile.Enabled;
        _gamma = profile.Settings.Gamma;
        _brightness = profile.Settings.Brightness;
        _contrast = profile.Settings.Contrast;
        _vibrance = profile.Settings.Vibrance;
        ToggleHotkey = new HotkeyBindingViewModel(profile.ToggleHotkey);
        IncreaseIntensityHotkey = new HotkeyBindingViewModel(profile.IncreaseIntensityHotkey);
        DecreaseIntensityHotkey = new HotkeyBindingViewModel(profile.DecreaseIntensityHotkey);

        foreach (var hotkey in new[] { ToggleHotkey, IncreaseIntensityHotkey, DecreaseIntensityHotkey })
        {
            hotkey.Changed += (_, _) => RaiseEdited();
        }

        ResetGammaCommand = new RelayCommand(() => Gamma = AdjustmentSettings.Neutral);
        ResetBrightnessCommand = new RelayCommand(() => Brightness = AdjustmentSettings.Neutral);
        ResetContrastCommand = new RelayCommand(() => Contrast = AdjustmentSettings.Neutral);
        ResetVibranceCommand = new RelayCommand(() => Vibrance = AdjustmentSettings.Neutral);
    }

    public event EventHandler? Edited;

    public string ExecutableName => _saved.ExecutableName;

    public ImageSource? Icon => _icon.Value;

    public RelayCommand ResetGammaCommand { get; }

    public RelayCommand ResetBrightnessCommand { get; }

    public RelayCommand ResetContrastCommand { get; }

    public RelayCommand ResetVibranceCommand { get; }

    public HotkeyBindingViewModel ToggleHotkey { get; }

    public HotkeyBindingViewModel IncreaseIntensityHotkey { get; }

    public HotkeyBindingViewModel DecreaseIntensityHotkey { get; }

    public string DisplayName
    {
        get => _displayName;
        set => Edit(ref _displayName, value);
    }

    public bool Enabled
    {
        get => _enabled;
        set => Edit(ref _enabled, value);
    }

    public int Gamma
    {
        get => _gamma;
        set => Edit(ref _gamma, value);
    }

    public int Brightness
    {
        get => _brightness;
        set => Edit(ref _brightness, value);
    }

    public int Contrast
    {
        get => _contrast;
        set => Edit(ref _contrast, value);
    }

    public int Vibrance
    {
        get => _vibrance;
        set => Edit(ref _vibrance, value);
    }

    public bool IsDirty => !Equals(_saved, ToProfile());

    /// <summary>True when removing would lose adjusted filters or bound hotkeys, saved or not.</summary>
    public bool HasCustomizations => IsCustomized(_saved) || IsCustomized(ToProfile());

    public AdjustmentSettings Settings => new(_gamma, _brightness, _contrast, _vibrance);

    public AppProfile ToProfile() => new(_saved.ExecutableName)
    {
        DisplayName = _displayName,
        ExecutablePath = _saved.ExecutablePath,
        Enabled = _enabled,
        Settings = Settings,
        ToggleHotkey = ToggleHotkey.Hotkey,
        IncreaseIntensityHotkey = IncreaseIntensityHotkey.Hotkey,
        DecreaseIntensityHotkey = DecreaseIntensityHotkey.Hotkey
    };

    public void MarkSaved(AppProfile saved)
    {
        _saved = saved;
        OnPropertyChanged(nameof(IsDirty));
    }

    /// <summary>Throws away every unsaved edit and shows the last saved profile again.</summary>
    public void Revert()
    {
        DisplayName = _saved.DisplayName;
        Enabled = _saved.Enabled;
        ShowSettings(_saved.Settings);
        ToggleHotkey.Hotkey = _saved.ToggleHotkey;
        IncreaseIntensityHotkey.Hotkey = _saved.IncreaseIntensityHotkey;
        DecreaseIntensityHotkey.Hotkey = _saved.DecreaseIntensityHotkey;
    }

    /// <summary>Moves the sliders to settings adjusted elsewhere, such as by an intensity hotkey, as unsaved edits.</summary>
    public void ShowSettings(AdjustmentSettings settings)
    {
        Gamma = settings.Gamma;
        Brightness = settings.Brightness;
        Contrast = settings.Contrast;
        Vibrance = settings.Vibrance;
    }

    public void ResetAll() => ShowSettings(AdjustmentSettings.Default);

    private static bool IsCustomized(AppProfile profile) =>
        profile.Settings != AdjustmentSettings.Default
        || profile.ToggleHotkey is not null
        || profile.IncreaseIntensityHotkey is not null
        || profile.DecreaseIntensityHotkey is not null;

    private void Edit<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (SetProperty(ref field, value, propertyName))
        {
            RaiseEdited();
        }
    }

    private void RaiseEdited()
    {
        OnPropertyChanged(nameof(IsDirty));
        Edited?.Invoke(this, EventArgs.Empty);
    }
}
