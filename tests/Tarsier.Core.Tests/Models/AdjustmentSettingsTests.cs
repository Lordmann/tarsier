using Tarsier.Core.Models;

namespace Tarsier.Core.Tests.Models;

public class AdjustmentSettingsTests
{
    [Fact]
    public void TheScaleRunsFromMinusOneHundredToOneHundredAroundZero()
    {
        Assert.Equal(-100, AdjustmentSettings.Min);
        Assert.Equal(100, AdjustmentSettings.Max);
        Assert.Equal(0, AdjustmentSettings.Neutral);
    }

    [Fact]
    public void TheDefaultLeavesEveryChannelUntouched()
    {
        Assert.True(AdjustmentSettings.Default.IsNeutral);
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(0)]
    [InlineData(100)]
    public void ValuesInsideTheScaleAreAccepted(int value)
    {
        Assert.Equal(value, new AdjustmentSettings(value, value, value).Gamma);
    }

    [Theory]
    [InlineData(-101)]
    [InlineData(101)]
    public void ValuesOutsideTheScaleAreRejected(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AdjustmentSettings(value, 0, 0));
    }

    [Fact]
    public void VibranceSharesTheSameScaleAndNeutralPoint()
    {
        Assert.Equal(Neutral, new AdjustmentSettings(0, 0, 0).Vibrance);
        Assert.Equal(-100, new AdjustmentSettings(0, 0, 0, -100).Vibrance);
        Assert.Throws<ArgumentOutOfRangeException>(() => new AdjustmentSettings(0, 0, 0, 101));
    }

    [Fact]
    public void VibranceAloneIsEnoughToStopSettingsBeingNeutral()
    {
        Assert.False(new AdjustmentSettings(0, 0, 0, 40).IsNeutral);
    }

    [Fact]
    public void IntensifyingShiftsGammaBrightnessAndContrastTogether()
    {
        Assert.Equal(new AdjustmentSettings(41, -19, 1, 30), new AdjustmentSettings(40, -20, 0, 30).Intensified(+1));
        Assert.Equal(new AdjustmentSettings(39, -21, -1, 30), new AdjustmentSettings(40, -20, 0, 30).Intensified(-1));
    }

    [Fact]
    public void IntensifyingCrossesNeutralFreely()
    {
        Assert.Equal(new AdjustmentSettings(-1, -1, -1), AdjustmentSettings.Default.Intensified(-1));
    }

    [Fact]
    public void IntensifyingIsClampedAtTheEndsOfTheScale()
    {
        Assert.Equal(new AdjustmentSettings(100, -99, 0), new AdjustmentSettings(100, -100, -1).Intensified(+1));
        Assert.Equal(new AdjustmentSettings(99, -100, 0), new AdjustmentSettings(100, -100, 1).Intensified(-1));
    }

    [Fact]
    public void VibranceIsSaturationNotIntensityAndIsNeverStepped()
    {
        Assert.Equal(-50, new AdjustmentSettings(0, 0, 0, -50).Intensified(+1).Vibrance);
        Assert.Equal(-50, new AdjustmentSettings(0, 0, 0, -50).Intensified(-1).Vibrance);
    }

    private const int Neutral = AdjustmentSettings.Neutral;
}
