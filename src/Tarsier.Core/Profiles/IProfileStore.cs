using Tarsier.Core.Models;

namespace Tarsier.Core.Profiles;

public interface IProfileStore
{
    IReadOnlyList<AppProfile> Load();

    void Save(IEnumerable<AppProfile> profiles);
}
