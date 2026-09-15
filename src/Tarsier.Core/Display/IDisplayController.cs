using Tarsier.Core.Gamma;

namespace Tarsier.Core.Display;

public interface IDisplayController
{
    IReadOnlyList<DisplayInfo> GetDisplays();

    bool Apply(string monitorId, GammaRamp ramp);

    void Reset(string monitorId);

    void ResetAll();
}
