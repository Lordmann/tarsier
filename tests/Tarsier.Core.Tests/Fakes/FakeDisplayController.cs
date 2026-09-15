using Tarsier.Core.Display;
using Tarsier.Core.Gamma;

namespace Tarsier.Core.Tests.Fakes;

public sealed class FakeDisplayController : IDisplayController
{
    public List<string> Log { get; } = new();

    public Dictionary<string, GammaRamp> Applied { get; } = new(StringComparer.Ordinal);

    public int ResetAllCount { get; private set; }

    public IReadOnlyList<DisplayInfo> GetDisplays() => new[]
    {
        new DisplayInfo(Monitors.Primary, "Primary", true),
        new DisplayInfo(Monitors.Secondary, "Secondary", false)
    };

    public bool Apply(string monitorId, GammaRamp ramp)
    {
        Log.Add($"apply:{monitorId}");
        Applied[monitorId] = ramp;
        return true;
    }

    public void Reset(string monitorId)
    {
        Log.Add($"reset:{monitorId}");
        Applied.Remove(monitorId);
    }

    public void ResetAll()
    {
        Log.Add("reset:*");
        Applied.Clear();
        ResetAllCount++;
    }
}

public static class Monitors
{
    public const string Primary = @"\\.\DISPLAY1";
    public const string Secondary = @"\\.\DISPLAY2";
}
