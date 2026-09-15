using Tarsier.Core.Models;

namespace Tarsier.Core.Profiles;

public interface IProfileProvider
{
    AppProfile? Find(string executableName);
}
