using Tarsier.Core.Models;
using Tarsier.Core.Profiles;

namespace Tarsier.Core.Tests.Profiles;

public class ProfilePersistenceTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"tarsier-{Guid.NewGuid():N}", "profiles.json");

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_path);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ProfilesSurviveARoundTripIncludingTheHotkeys()
    {
        var store = new JsonProfileStore(_path);
        var profile = new AppProfile(@"C:\Games\Doom\DOOM.exe")
        {
            DisplayName = "DOOM",
            ExecutablePath = @"C:\Games\Doom\DOOM.exe",
            Settings = new AdjustmentSettings(65, 40, 72, -35),
            ToggleHotkey = new Hotkey(InputCode.Alt, InputCode.Shift, 0x77),
            IncreaseIntensityHotkey = new Hotkey(0x73, InputCode.WheelUp),
            DecreaseIntensityHotkey = new Hotkey(InputCode.XButton1)
        };

        store.Save(new[] { profile });

        Assert.Equal(profile, Assert.Single(new JsonProfileStore(_path).Load()));
    }

    [Fact]
    public void AProfileSavedBeforeVibranceExistedLoadsWithItNeutral()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, @"
            [{ ""ExecutableName"": ""game.exe"", ""DisplayName"": ""Game"", ""Enabled"": true,
               ""Settings"": { ""Gamma"": 30, ""Brightness"": -20, ""Contrast"": 10 } }]");

        var profile = Assert.Single(new JsonProfileStore(_path).Load());

        Assert.Equal(AdjustmentSettings.Neutral, profile.Settings.Vibrance);
        Assert.Equal(30, profile.Settings.Gamma);
        Assert.Null(profile.ExecutablePath);
    }

    [Fact]
    public void AProfileSavedWithTheOriginalHotkeyFormatLoadsWithIntensityHotkeysUnbound()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, @"
            [{ ""ExecutableName"": ""game.exe"", ""DisplayName"": ""Game"", ""Enabled"": true,
               ""Settings"": { ""Gamma"": 30, ""Brightness"": -20, ""Contrast"": 10 },
               ""ToggleHotkey"": { ""Modifiers"": 1, ""VirtualKey"": 119 } }]");

        var profile = Assert.Single(new JsonProfileStore(_path).Load());

        Assert.Equal(new Hotkey(InputCode.Alt, 0x77), profile.ToggleHotkey);
        Assert.Null(profile.IncreaseIntensityHotkey);
        Assert.Null(profile.DecreaseIntensityHotkey);
    }

    [Fact]
    public void AMissingFileLoadsAsAnEmptyProfileList()
    {
        Assert.Empty(new JsonProfileStore(_path).Load());
    }

    [Fact]
    public void ACorruptFileLoadsAsAnEmptyProfileListRatherThanCrashing()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, "{ not json");

        Assert.Empty(new JsonProfileStore(_path).Load());
    }

    [Fact]
    public void ProfilesAreFoundByExecutableNameRegardlessOfPathOrCasing()
    {
        var repository = new ProfileRepository(new JsonProfileStore(_path));
        repository.Save(new AppProfile("Game.exe"));

        Assert.NotNull(repository.Find(@"D:\Steam\common\GAME.EXE"));
        Assert.NotNull(repository.Find("game.exe"));
        Assert.Null(repository.Find("other.exe"));
    }

    [Fact]
    public void SavingTheSameExecutableTwiceReplacesTheProfile()
    {
        var repository = new ProfileRepository(new JsonProfileStore(_path));
        repository.Save(new AppProfile("game.exe") { DisplayName = "First" });
        repository.Save(new AppProfile("game.exe") { DisplayName = "Second" });

        Assert.Equal("Second", Assert.Single(repository.All).DisplayName);
    }

    [Fact]
    public void RemovedProfilesDisappearFromDisk()
    {
        var repository = new ProfileRepository(new JsonProfileStore(_path));
        repository.Save(new AppProfile("game.exe"));

        Assert.True(repository.Remove("game.exe"));
        Assert.False(repository.Remove("game.exe"));
        Assert.Empty(new ProfileRepository(new JsonProfileStore(_path)).All);
    }
}
