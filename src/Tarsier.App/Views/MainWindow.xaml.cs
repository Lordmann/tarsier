using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Tarsier.App.Interop;
using Tarsier.App.ViewModels;

namespace Tarsier.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly WindowPlacementMemory _placementMemory;

    public MainWindow(MainViewModel viewModel, WindowPlacementMemory placementMemory)
    {
        _viewModel = viewModel;
        _placementMemory = placementMemory;
        DataContext = viewModel;
        InitializeComponent();
        placementMemory.Restore(this);

        viewModel.PickApplication = PickApplication;
        viewModel.ConfirmRemoval = ConfirmRemoval;
    }

    /// <summary>Set before shutting down so the window closes instead of hiding to the tray.</summary>
    public bool IsClosingForShutdown { get; set; }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        DarkTitleBar.Apply(this);
    }

    /// <summary>The preview writes to whichever monitor is showing this window.</summary>
    public string? CurrentMonitorId() =>
        WindowInspector.GetMonitorId(new WindowInteropHelper(this).Handle);

    /// <summary>Clicking empty space takes focus away from whichever input has it.</summary>
    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseDown(e);
        if (KeyboardFocusReset.IsEmptySpace(e.OriginalSource))
        {
            KeyboardFocusReset.Clear(this);
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _viewModel.StopPreview();
        _placementMemory.Remember(this);

        if (IsClosingForShutdown)
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;
        Hide();
    }

    protected override void OnDeactivated(EventArgs e)
    {
        _viewModel.StopPreview();
        base.OnDeactivated(e);
    }

    private RunningApplication? PickApplication(IReadOnlyList<RunningApplication> applications)
    {
        var picker = new AppPickerWindow(applications) { Owner = this };
        return picker.ShowDialog() == true ? picker.SelectedApplication : null;
    }

    private bool ConfirmRemoval(ProfileEditorViewModel profile)
    {
        var name = string.IsNullOrWhiteSpace(profile.DisplayName) ? profile.ExecutableName : profile.DisplayName;
        var dialog = new ConfirmationWindow(
            "Remove profile",
            $"Remove the profile for {name}? Its filter settings and hotkeys will be lost.",
            "Remove") { Owner = this };
        return dialog.ShowDialog() == true;
    }
}
