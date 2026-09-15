using Tarsier.App.Interop;
using Tarsier.Core.Display;
using Tarsier.Core.Models;

namespace Tarsier.App.Display;

/// <summary>
/// Drives NVIDIA digital vibrance, the same control NVIDIA Control Panel exposes. Monitors on any other adapter
/// are left alone, so a machine with mixed graphics keeps working with the slider simply doing nothing there.
/// </summary>
public sealed class NvapiVibranceController : IVibranceController
{
    private readonly Nvapi? _nvapi;
    private readonly IReadOnlyDictionary<string, IntPtr> _displays;
    private readonly Dictionary<string, int> _originalLevels = new(StringComparer.OrdinalIgnoreCase);

    public NvapiVibranceController()
    {
        _nvapi = Nvapi.TryLoad();
        _displays = _nvapi?.EnumerateDisplays() ?? new Dictionary<string, IntPtr>();
    }

    public bool IsSupported => _displays.Count > 0;

    public void Apply(string monitorId, int vibrance)
    {
        if (_nvapi is null || !_displays.TryGetValue(monitorId, out var handle) ||
            !_nvapi.TryGetVibrance(handle, out var range))
        {
            return;
        }

        if (!_originalLevels.ContainsKey(monitorId))
        {
            _originalLevels[monitorId] = range.Current;
        }

        _nvapi.SetVibrance(handle, ToDriverLevel(vibrance, range));
    }

    public void Reset(string monitorId)
    {
        if (_nvapi is null || !_originalLevels.TryGetValue(monitorId, out var original) ||
            !_displays.TryGetValue(monitorId, out var handle))
        {
            return;
        }

        _nvapi.SetVibrance(handle, original);
        _originalLevels.Remove(monitorId);
    }

    public void ResetAll()
    {
        foreach (var monitorId in _originalLevels.Keys.ToArray())
        {
            Reset(monitorId);
        }
    }

    /// <summary>
    /// Maps the -100..100 slider onto whatever scale the driver reports, so that 0 lands exactly on the driver's
    /// own default rather than on a number assumed here.
    /// </summary>
    private static int ToDriverLevel(int vibrance, VibranceRange range)
    {
        var span = vibrance >= 0 ? range.Max - range.Default : range.Default - range.Min;
        var level = range.Default + (int)Math.Round(span * (vibrance / (double)AdjustmentSettings.Max));
        return Math.Clamp(level, range.Min, range.Max);
    }
}
