using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Interop;
using Microsoft.Win32;
using Tarsier.App.Interop;
using Tarsier.App.Display;
using Tarsier.App.Monitoring;
using Tarsier.App.Startup;
using Tarsier.App.ViewModels;
using Tarsier.App.Views;
using Tarsier.Core.Monitoring;
using Tarsier.Core.Profiles;
using Velopack;
using Application = System.Windows.Application;
using Binding = System.Windows.Data.Binding;

namespace Tarsier.App;

public partial class App : Application
{
    private const string TrayArgument = "--tray";

    private GdiDisplayController? _display;
    private NvapiVibranceController? _vibrance;
    private WinEventForegroundWatcher? _watcher;
    private GlobalHotkeyService? _hotkeys;
    private ProfileApplicationService? _profileApplication;
    private UpdateService? _updates;
    private NotifyIcon? _trayIcon;
    private ContextMenu? _trayMenu;
    private MenuItem? _restartToUpdate;
    private MainWindow? _window;
    private MainViewModel? _viewModel;

    /// <summary>
    /// Velopack handles its install, update and uninstall invocations here and exits before WPF starts, which is
    /// why this replaces the generated entry point.
    /// </summary>
    [STAThread]
    public static void Main()
    {
        var firstRun = false;
        VelopackApp.Build()
            .OnFirstRun(_ => firstRun = true)
            .OnBeforeUninstallFastCallback(_ => new AutostartService().SetEnabled(false))
            .Run();

        var app = new App { IsFirstRun = firstRun };
        app.InitializeComponent();
        app.Run();
    }

    /// <summary>
    /// The installer people download is pinned to one release so its SmartScreen reputation is never reset, which
    /// makes the first launch of a fresh install the moment to bring it up to date.
    /// </summary>
    public bool IsFirstRun { get; init; }

    private static string ProfilePath => ProfilePathUnder("Tarsier");

    private static string LegacyProfilePath => ProfilePathUnder("IngameGamma");

    private static string WindowPlacementPath => Path.Combine(Path.GetDirectoryName(ProfilePath)!, "window.json");

    private static string ProfilePathUnder(string folder) => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        folder,
        "profiles.json");

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Contains(GammaRangePolicy.ElevationArgument))
        {
            new GammaRangePolicy().TryEnableFullRange();
            Shutdown();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        _updates = new UpdateService();
        if (IsFirstRun && await TryInstallLatestAsync())
        {
            return;
        }

        LegacyProfileMigration.TryMigrate(LegacyProfilePath, ProfilePath);
        new AutostartService().MigrateLegacyEntry();
        RestoreDisplaysOnCrash();
        Compose();

        if (!e.Args.Contains(TrayArgument))
        {
            ShowWindow();
        }
    }

    /// <summary>
    /// Nothing of the app is shown on the bootstrap version: a progress window stands in for setup until the current
    /// release is downloaded, then the process restarts on it. Any failure falls through to a normal start.
    /// </summary>
    private async Task<bool> TryInstallLatestAsync()
    {
        if (!await _updates!.HasUpdateAsync())
        {
            return false;
        }

        var window = new InstallWindow();
        window.Show();
        if (!await _updates.DownloadAsync(new Progress<int>(window.Report)))
        {
            window.Close();
            return false;
        }

        _updates.RestartIntoWindow();
        return true;
    }

    /// <summary>
    /// A crash never reaches <see cref="OnExit"/>, so the monitors are restored on the way down. The exception
    /// itself is left unhandled: this only undoes the ramp, it does not keep a broken process alive.
    /// </summary>
    private void RestoreDisplaysOnCrash()
    {
        DispatcherUnhandledException += (_, _) => RestoreDisplays();
        AppDomain.CurrentDomain.UnhandledException += (_, _) => RestoreDisplays();
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        RestoreDisplays();
        base.OnSessionEnding(e);
    }

    private void RestoreDisplays()
    {
        _display?.ResetAll();
        _vibrance?.ResetAll();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _updates?.ApplyAfterExit();
        ReleaseResources();
        base.OnExit(e);
    }

    private void ReleaseResources()
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _trayIcon?.Dispose();
        _profileApplication?.Dispose();
        _watcher?.Dispose();
        _hotkeys?.Dispose();
    }

    private void Compose()
    {
        var repository = new ProfileRepository(new JsonProfileStore(ProfilePath));

        _display = new GdiDisplayController();
        _display.ResetInheritedRamps();
        _vibrance = new NvapiVibranceController();
        _watcher = new WinEventForegroundWatcher();
        _hotkeys = new GlobalHotkeyService();
        _profileApplication = new ProfileApplicationService(_watcher, _display, _vibrance, repository, _hotkeys);
        _profileApplication.Start();

        var preview = new LivePreview(_display, _vibrance, () => _window?.CurrentMonitorId());
        _viewModel = new MainViewModel(repository, _profileApplication, new AutostartService(), new GammaRangePolicy(), preview, _vibrance);
        _window = new MainWindow(_viewModel, new WindowPlacementMemory(WindowPlacementPath));

        _trayMenu = CreateTrayMenu();
        _trayIcon = CreateTrayIcon();

        _updates!.UpdateReady += (_, _) => _restartToUpdate!.Visibility = Visibility.Visible;
        _updates.Start();
    }

    /// <summary>
    /// A WPF menu rather than the WinForms one the tray icon offers: on .NET 6 that one keeps the scale of whichever
    /// monitor it last opened on, so with mixed DPI it turns up small and misplaced.
    /// </summary>
    private ContextMenu CreateTrayMenu()
    {
        var open = new MenuItem { Header = "Open" };
        open.Click += (_, _) => ShowWindow();

        var pause = new MenuItem { Header = "Pause all profiles", IsCheckable = true };
        pause.SetBinding(MenuItem.IsCheckedProperty, new Binding(nameof(MainViewModel.IsPaused)) { Source = _viewModel, Mode = System.Windows.Data.BindingMode.TwoWay });

        _restartToUpdate = new MenuItem { Header = "Restart to update", Visibility = Visibility.Collapsed };
        _restartToUpdate.Click += (_, _) => RestartToUpdate();

        var exit = new MenuItem { Header = "Exit" };
        exit.Click += (_, _) => ExitApplication();

        var menu = new ContextMenu { Placement = PlacementMode.MousePoint };
        menu.Items.Add(open);
        menu.Items.Add(pause);
        menu.Items.Add(new Separator());
        menu.Items.Add(_restartToUpdate);
        menu.Items.Add(exit);
        return menu;
    }

    /// <summary>The updater kills the process rather than shutting WPF down, so the cleanup OnExit would do happens here.</summary>
    private void RestartToUpdate()
    {
        ReleaseResources();
        _updates?.RestartIntoTray();
    }

    private NotifyIcon CreateTrayIcon()
    {
        var icon = new NotifyIcon
        {
            Icon = TrayIcons.ForCurrentTaskbar(),
            Text = "Tarsier",
            Visible = true
        };
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        icon.DoubleClick += (_, _) => ShowWindow();
        icon.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                ShowTrayMenu();
            }
        };

        return icon;
    }

    /// <summary>
    /// A menu can only take the mouse capture it needs to close on an outside click while its process owns the
    /// foreground, so the (possibly hidden) window is brought forward first.
    /// </summary>
    private void ShowTrayMenu()
    {
        if (_trayMenu is null || _window is null)
        {
            return;
        }

        NativeMethods.SetForegroundWindow(new WindowInteropHelper(_window).EnsureHandle());
        _trayMenu.IsOpen = true;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category != UserPreferenceCategory.General || _trayIcon is null)
        {
            return;
        }

        var previous = _trayIcon.Icon;
        _trayIcon.Icon = TrayIcons.ForCurrentTaskbar();
        previous?.Dispose();
    }

    private void ExitApplication()
    {
        if (_window is not null)
        {
            _window.IsClosingForShutdown = true;
        }

        Shutdown();
    }

    private void ShowWindow()
    {
        if (_window is null)
        {
            return;
        }

        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
    }
}
