using System.Windows;

namespace Tarsier.App.Views;

/// <summary>Stands in for setup while a fresh install downloads the current release.</summary>
public partial class InstallWindow : Window
{
    public InstallWindow()
    {
        InitializeComponent();
    }

    public void Report(int percent) => Progress.Value = percent;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        DarkTitleBar.Apply(this);
    }
}
