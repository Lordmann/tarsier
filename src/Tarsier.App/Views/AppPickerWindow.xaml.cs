using System.Windows;
using Tarsier.App.Interop;

namespace Tarsier.App.Views;

public partial class AppPickerWindow : Window
{
    public AppPickerWindow(IReadOnlyList<RunningApplication> applications)
    {
        InitializeComponent();
        ApplicationList.ItemsSource = applications;
        ApplicationList.SelectedIndex = applications.Count > 0 ? 0 : -1;
    }

    public RunningApplication? SelectedApplication => ApplicationList.SelectedItem as RunningApplication;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        DarkTitleBar.Apply(this);
    }

    private void OnAccept(object sender, RoutedEventArgs e)
    {
        if (SelectedApplication is null)
        {
            return;
        }

        DialogResult = true;
    }
}
