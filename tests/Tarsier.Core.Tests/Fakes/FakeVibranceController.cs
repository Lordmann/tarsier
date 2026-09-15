using Tarsier.Core.Display;

namespace Tarsier.Core.Tests.Fakes;

public sealed class FakeVibranceController : IVibranceController
{
    public List<string> Log { get; } = new();

    public Dictionary<string, int> Applied { get; } = new(StringComparer.Ordinal);

    public int ResetAllCount { get; private set; }

    public bool IsSupported { get; set; } = true;

    public void Apply(string monitorId, int vibrance)
    {
        Log.Add($"apply:{monitorId}:{vibrance}");
        Applied[monitorId] = vibrance;
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
