using Tarsier.Core.Models;

namespace Tarsier.Core.Profiles;

/// <summary>In-memory view of the saved profiles, keyed by normalized executable name.</summary>
public sealed class ProfileRepository : IProfileProvider
{
    private readonly IProfileStore _store;
    private readonly Dictionary<string, AppProfile> _profiles;

    public ProfileRepository(IProfileStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _profiles = store.Load().ToDictionary(profile => profile.ExecutableName, StringComparer.Ordinal);
    }

    public event EventHandler? Changed;

    public IReadOnlyCollection<AppProfile> All => _profiles.Values;

    public AppProfile? Find(string executableName)
    {
        if (string.IsNullOrWhiteSpace(executableName))
        {
            return null;
        }

        return _profiles.TryGetValue(ExecutableKey.Normalize(executableName), out var profile) ? profile : null;
    }

    public void Save(AppProfile profile)
    {
        if (profile is null)
        {
            throw new ArgumentNullException(nameof(profile));
        }

        _profiles[profile.ExecutableName] = profile;
        Persist();
    }

    public bool Remove(string executableName)
    {
        if (string.IsNullOrWhiteSpace(executableName) || !_profiles.Remove(ExecutableKey.Normalize(executableName)))
        {
            return false;
        }

        Persist();
        return true;
    }

    private void Persist()
    {
        _store.Save(_profiles.Values);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
