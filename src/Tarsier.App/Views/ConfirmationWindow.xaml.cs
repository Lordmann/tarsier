using System.Windows;

namespace Tarsier.App.Views;

public partial class ConfirmationWindow : Window
{
    public ConfirmationWindow(string title, string message, string confirmLabel)
    {
        InitializeComponent();
        Title = title;
        Message.Text = message;
        ConfirmButton.Content = confirmLabel;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        DarkTitleBar.Apply(this);
    }

    private void OnConfirm(object sender, RoutedEventArgs e) => DialogResult = true;
}
