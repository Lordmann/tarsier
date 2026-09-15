using System.Collections.ObjectModel;
using Tarsier.App.Display;
using Tarsier.App.Interop;
using Tarsier.App.Startup;
using Tarsier.Core.Display;
using Tarsier.Core.Models;
using Tarsier.Core.Monitoring;
using Tarsier.Core.Profiles;

namespace Tarsier.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly ProfileRepository _repository;
    private readonly ProfileApplicationService _application;
    private readonly AutostartService _autostart;
    private readonly GammaRangePolicy _gammaRange;
    private readonly LivePreview _preview;
    private readonly IVibranceController _vibrance;

    private ProfileEditorViewModel? _selectedProfile;
    private bool _isPreviewEnabled;

    public MainViewModel(
        ProfileRepository repository,
        ProfileApplicationService application,
        AutostartService autostart,
        GammaRangePolicy gammaRange,
        LivePreview preview,
        IVibranceController vibrance)
    {
        _repository = repository;
        _application = application;
        _autostart = autostart;
        _gammaRange = gammaRange;
        _preview = preview;
        _vibrance = vibrance;

        AddProfileCommand = new RelayCommand(AddProfile);
        RemoveProfileCommand = new RelayCommand(RemoveProfile, () => _selectedProfile is not null);
        SaveProfileCommand = new RelayCommand(SaveProfile, () => _selectedProfile?.IsDirty == true);
        CancelEditsCommand = new RelayCommand(CancelEdits, () => _selectedProfile?.IsDirty == true);
        ResetSlidersCommand = new RelayCommand(() => _selectedProfile?.ResetAll(), () => _selectedProfile is not null);
        EnableFullRangeCommand = new RelayCommand(EnableFullRange);

        _application.ActiveSettingsAdjusted += OnActiveSettingsAdjusted;
        Reload();
    }

    public ObservableCollection<ProfileEditorViewModel> Profiles { get; } = new();

    public RelayCommand AddProfileCommand { get; }

    public RelayCommand RemoveProfileCommand { get; }

    public RelayCommand SaveProfileCommand { get; }

    public RelayCommand CancelEditsCommand { get; }

    public RelayCommand ResetSlidersCommand { get; }

    public RelayCommand EnableFullRangeCommand { get; }

    /// <summary>Supplied by the view so the picker can be shown without the view model referencing a window.</summary>
    public Func<IReadOnlyList<RunningApplication>, RunningApplication?>? PickApplication { get; set; }

    /// <summary>Asked before removing a profile that has filters or hotkeys set; untouched profiles are removed silently.</summary>
    public Func<ProfileEditorViewModel, bool> ConfirmRemoval { get; set; } = _ => true;

    public ProfileEditorViewModel? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (ReferenceEquals(_selectedProfile, value))
            {
                return;
            }

            if (_selectedProfile is not null)
            {
                _selectedProfile.Edited -= OnProfileEdited;
            }

            SetProperty(ref _selectedProfile, value);

            if (_selectedProfile is not null)
            {
                _selectedProfile.Edited += OnProfileEdited;
            }

            RefreshPreview();
        }
    }

    public bool IsPreviewEnabled
    {
        get => _isPreviewEnabled;
        set
        {
            if (SetProperty(ref _isPreviewEnabled, value))
            {
                RefreshPreview();
            }
        }
    }

    public bool IsPaused
    {
        get => _application.IsPaused;
        set
        {
            if (_application.IsPaused == value)
            {
                return;
            }

            _application.IsPaused = value;
            OnPropertyChanged();
        }
    }

    public bool StartsWithWindows
    {
        get => _autostart.IsEnabled;
        set
        {
            _autostart.SetEnabled(value);
            OnPropertyChanged();
        }
    }

    public bool IsFullRangeMissing => !_gammaRange.IsFullRangeEnabled;

    public static string Version => "v" + typeof(MainViewModel).Assembly.GetName().Version!.ToString(3);

    /// <summary>Vibrance needs an NVIDIA display, so the slider is hidden rather than left there doing nothing.</summary>
    public bool IsVibranceSupported => _vibrance.IsSupported;

    public void StopPreview()
    {
        _preview.Stop();
        _isPreviewEnabled = false;
        OnPropertyChanged(nameof(IsPreviewEnabled));
    }

    private void Reload()
    {
        Profiles.Clear();
        foreach (var profile in _repository.All.OrderBy(p => p.DisplayName, StringComparer.CurrentCultureIgnoreCase))
        {
            Profiles.Add(new ProfileEditorViewModel(profile));
        }

        SelectedProfile = Profiles.FirstOrDefault();
    }

    private void AddProfile()
    {
        var chosen = PickApplication?.Invoke(RunningApplications.Enumerate());
        if (chosen is null)
        {
            return;
        }

        var existing = Profiles.FirstOrDefault(p => p.ExecutableName == chosen.ExecutableName);
        if (existing is not null)
        {
            SelectedProfile = existing;
            return;
        }

        var profile = new AppProfile(chosen.ExecutableName) { DisplayName = chosen.DisplayName, ExecutablePath = chosen.ExecutablePath };
        _repository.Save(profile);

        var editor = new ProfileEditorViewModel(profile);
        Profiles.Add(editor);
        SelectedProfile = editor;
        _application.Refresh();
    }

    private void RemoveProfile()
    {
        if (_selectedProfile is null || (_selectedProfile.HasCustomizations && !ConfirmRemoval(_selectedProfile)))
        {
            return;
        }

        _repository.Remove(_selectedProfile.ExecutableName);
        _application.DiscardUnsaved(_selectedProfile.ExecutableName);
        Profiles.Remove(_selectedProfile);
        SelectedProfile = Profiles.FirstOrDefault();
        _application.Refresh();
    }

    private void SaveProfile()
    {
        if (_selectedProfile is null)
        {
            return;
        }

        var profile = _selectedProfile.ToProfile();
        _repository.Save(profile);
        _selectedProfile.MarkSaved(profile);
        _application.DiscardUnsaved(profile.ExecutableName);
        _application.Refresh();
    }

    private void CancelEdits()
    {
        if (_selectedProfile is null)
        {
            return;
        }

        _selectedProfile.Revert();
        _application.DiscardUnsaved(_selectedProfile.ExecutableName);
        _application.Refresh();
    }

    /// <summary>An intensity hotkey moved the active profile's settings; mirror them on its sliders as unsaved edits.</summary>
    private void OnActiveSettingsAdjusted(object? sender, EventArgs e)
    {
        if (_application.ActiveProfile is not { } active || _application.ActiveSettings is not { } settings)
        {
            return;
        }

        Profiles.FirstOrDefault(p => p.ExecutableName == active.ExecutableName)?.ShowSettings(settings);
    }

    private void EnableFullRange()
    {
        _gammaRange.RequestElevatedEnable();
        OnPropertyChanged(nameof(IsFullRangeMissing));
    }

    private void OnProfileEdited(object? sender, EventArgs e) => RefreshPreview();

    private void RefreshPreview()
    {
        if (_isPreviewEnabled && _selectedProfile is not null)
        {
            _preview.Show(_selectedProfile.Settings);
            return;
        }

        _preview.Stop();
    }
}
