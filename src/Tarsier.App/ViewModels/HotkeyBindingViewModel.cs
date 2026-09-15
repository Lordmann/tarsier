using Tarsier.Core.Models;

namespace Tarsier.App.ViewModels;

/// <summary>One editable key combination of a profile, shown as text and cleared with a button.</summary>
public sealed class HotkeyBindingViewModel : ObservableObject
{
    private Hotkey? _hotkey;

    public HotkeyBindingViewModel(Hotkey? hotkey)
    {
        _hotkey = hotkey;
        ClearCommand = new RelayCommand(() => Hotkey = null);
    }

    public event EventHandler? Changed;

    public RelayCommand ClearCommand { get; }

    public Hotkey? Hotkey
    {
        get => _hotkey;
        set
        {
            if (SetProperty(ref _hotkey, value))
            {
                OnPropertyChanged(nameof(Text));
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public string Text => HotkeyDescription.Describe(_hotkey);
}
