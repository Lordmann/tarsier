using Tarsier.Core.Profiles;

namespace Tarsier.Core.Tests.Profiles;

public class LegacyProfileMigrationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"tarsier-{Guid.NewGuid():N}");

    private string LegacyPath => Path.Combine(_root, "IngameGamma", "profiles.json");

    private string CurrentPath => Path.Combine(_root, "Tarsier", "profiles.json");

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void ProfilesSavedUnderTheOldNameAreFoundUnderTheNewOne()
    {
        WriteLegacy("[]");

        Assert.True(LegacyProfileMigration.TryMigrate(LegacyPath, CurrentPath));
        Assert.Equal("[]", File.ReadAllText(CurrentPath));
    }

    [Fact]
    public void ProfilesAlreadySavedUnderTheNewNameAreNeverOverwritten()
    {
        WriteLegacy("legacy");
        Directory.CreateDirectory(Path.GetDirectoryName(CurrentPath)!);
        File.WriteAllText(CurrentPath, "current");

        Assert.False(LegacyProfileMigration.TryMigrate(LegacyPath, CurrentPath));
        Assert.Equal("current", File.ReadAllText(CurrentPath));
    }

    [Fact]
    public void AFirstInstallWithNothingToMigrateLeavesNoFileBehind()
    {
        Assert.False(LegacyProfileMigration.TryMigrate(LegacyPath, CurrentPath));
        Assert.False(File.Exists(CurrentPath));
    }

    /// <summary>The old file stays put so an older build a user rolls back to still finds its profiles.</summary>
    [Fact]
    public void TheOldFileSurvivesTheMigration()
    {
        WriteLegacy("[]");

        LegacyProfileMigration.TryMigrate(LegacyPath, CurrentPath);

        Assert.True(File.Exists(LegacyPath));
    }

    private void WriteLegacy(string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LegacyPath)!);
        File.WriteAllText(LegacyPath, contents);
    }
}
