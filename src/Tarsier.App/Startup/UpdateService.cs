using System.Windows.Threading;
using Velopack;
using Velopack.Sources;

namespace Tarsier.App.Startup;

/// <summary>
/// Keeps a Velopack install current without interrupting anything: newer GitHub releases are downloaded in the
/// background and installed once the app has exited. Outside an install (a dev build) it does nothing.
/// </summary>
public sealed class UpdateService
{
    private const string RepositoryUrl = "https://github.com/Lordmann/tarsier";
    private const string TrayArgument = "--tray";
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);

    private readonly UpdateManager _manager = new(new GithubSource(RepositoryUrl, accessToken: null, prerelease: false));
    private readonly DispatcherTimer _timer = new() { Interval = CheckInterval };
    private UpdateInfo? _available;

    /// <summary>Raised on the dispatcher once a release has been downloaded and is waiting to be applied.</summary>
    public event EventHandler? UpdateReady;

    public bool IsUpdateReady => _manager.IsInstalled && _manager.UpdatePendingRestart is not null;

    public void Start()
    {
        if (!_manager.IsInstalled)
        {
            return;
        }

        _timer.Tick += (_, _) => _ = CheckAndDownloadAsync();
        _timer.Start();
        _ = CheckAndDownloadAsync();
    }

    /// <summary>Whether a newer release is published; false when not installed or the check fails.</summary>
    public async Task<bool> HasUpdateAsync()
    {
        if (!_manager.IsInstalled)
        {
            return false;
        }

        try
        {
            _available = await _manager.CheckForUpdatesAsync();
        }
        catch
        {
            // Offline, rate-limited or a broken release: the next scheduled check tries again.
            _available = null;
        }

        return _available is not null;
    }

    /// <summary>Downloads the release found by <see cref="HasUpdateAsync"/>; false when the download fails.</summary>
    public async Task<bool> DownloadAsync(IProgress<int>? progress = null)
    {
        try
        {
            await _manager.DownloadUpdatesAsync(_available!, percent => progress?.Report(percent));
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Hands the downloaded release to the updater, which installs it after this process exits.</summary>
    public void ApplyAfterExit()
    {
        if (IsUpdateReady)
        {
            _manager.WaitExitThenApplyUpdates(null, silent: true, restart: false);
        }
    }

    /// <summary>
    /// Terminates the process without running any WPF shutdown path, so callers must release the displays first.
    /// </summary>
    public void RestartIntoTray() => Restart(TrayArgument);

    public void RestartIntoWindow() => Restart();

    private void Restart(params string[] args) => _manager.ApplyUpdatesAndRestart(null, args);

    private async Task CheckAndDownloadAsync()
    {
        if (await HasUpdateAsync() && await DownloadAsync())
        {
            UpdateReady?.Invoke(this, EventArgs.Empty);
        }
    }
}
