using Tarsier.Core.Display;
using Tarsier.Core.Gamma;
using Tarsier.Core.Models;

namespace Tarsier.App.Display;

/// <summary>Applies unsaved slider values to the monitor showing the settings window so edits can be judged by eye.</summary>
public sealed class LivePreview
{
    private readonly IDisplayController _display;
    private readonly IVibranceController _vibrance;
    private readonly Func<string?> _resolveMonitorId;

    private string? _previewing;

    public LivePreview(IDisplayController display, IVibranceController vibrance, Func<string?> resolveMonitorId)
    {
        _display = display ?? throw new ArgumentNullException(nameof(display));
        _vibrance = vibrance ?? throw new ArgumentNullException(nameof(vibrance));
        _resolveMonitorId = resolveMonitorId ?? throw new ArgumentNullException(nameof(resolveMonitorId));
    }

    public void Show(AdjustmentSettings settings)
    {
        var monitorId = _resolveMonitorId();
        if (monitorId is null)
        {
            return;
        }

        if (_previewing is not null && !string.Equals(_previewing, monitorId, StringComparison.Ordinal))
        {
            Restore(_previewing);
        }

        _previewing = monitorId;
        _display.Apply(monitorId, GammaRampCalculator.Calculate(settings));
        _vibrance.Apply(monitorId, settings.Vibrance);
    }

    public void Stop()
    {
        if (_previewing is null)
        {
            return;
        }

        Restore(_previewing);
        _previewing = null;
    }

    private void Restore(string monitorId)
    {
        _display.Reset(monitorId);
        _vibrance.Reset(monitorId);
    }
}
