namespace Tarsier.Core.Monitoring;

public interface IForegroundWatcher
{
    /// <summary>Raised with the new foreground window, or null when no window can be attributed to an executable.</summary>
    event EventHandler<ForegroundWindowInfo?> ForegroundChanged;

    void Start();

    void Stop();
}
