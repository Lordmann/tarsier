namespace Tarsier.Core.Profiles;

/// <summary>
/// Carries profiles across the rename from Ingame Gamma to Tarsier. The old file is copied rather than moved so
/// rolling back to an earlier build still finds it, and an existing current file always wins.
/// </summary>
public static class LegacyProfileMigration
{
    public static bool TryMigrate(string legacyPath, string currentPath)
    {
        if (File.Exists(currentPath) || !File.Exists(legacyPath))
        {
            return false;
        }

        var directory = Path.GetDirectoryName(currentPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.Copy(legacyPath, currentPath);
        return true;
    }
}
