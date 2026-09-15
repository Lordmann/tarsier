using Tarsier.Core.Monitoring;

namespace Tarsier.Core.Tests.Fakes;

public sealed class FakeForegroundWatcher : IForegroundWatcher
{
    public event EventHandler<ForegroundWindowInfo?>? ForegroundChanged;

    public bool IsRunning { get; private set; }

    public void Start() => IsRunning = true;

    public void Stop() => IsRunning = false;

    public void Focus(string executableName, string monitorId) =>
        ForegroundChanged?.Invoke(this, new ForegroundWindowInfo(executableName, monitorId));

    public void FocusNothing() => ForegroundChanged?.Invoke(this, null);
}
