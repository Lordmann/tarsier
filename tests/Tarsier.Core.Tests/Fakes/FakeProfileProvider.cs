using Tarsier.Core.Models;
using Tarsier.Core.Profiles;

namespace Tarsier.Core.Tests.Fakes;

public sealed class FakeProfileProvider : IProfileProvider
{
    private readonly Dictionary<string, AppProfile> _profiles = new(StringComparer.Ordinal);

    public void Set(AppProfile profile) => _profiles[profile.ExecutableName] = profile;

    public AppProfile? Find(string executableName) =>
        _profiles.TryGetValue(ExecutableKey.Normalize(executableName), out var profile) ? profile : null;
}
