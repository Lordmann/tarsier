using System.Text.Json;
using Tarsier.Core.Models;

namespace Tarsier.Core.Profiles;

public sealed class JsonProfileStore : IProfileStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly string _path;

    public JsonProfileStore(string path)
    {
        _path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("A path is required.", nameof(path)) : path;
    }

    public IReadOnlyList<AppProfile> Load()
    {
        if (!File.Exists(_path))
        {
            return Array.Empty<AppProfile>();
        }

        try
        {
            var profiles = JsonSerializer.Deserialize<List<AppProfile>>(File.ReadAllText(_path), SerializerOptions);
            return profiles ?? (IReadOnlyList<AppProfile>)Array.Empty<AppProfile>();
        }
        catch (JsonException)
        {
            return Array.Empty<AppProfile>();
        }
    }

    public void Save(IEnumerable<AppProfile> profiles)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(profiles.ToList(), SerializerOptions));
    }
}
