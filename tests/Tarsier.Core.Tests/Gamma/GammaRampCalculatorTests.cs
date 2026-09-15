using Tarsier.Core.Gamma;
using Tarsier.Core.Models;

namespace Tarsier.Core.Tests.Gamma;

public class GammaRampCalculatorTests
{
    private static AdjustmentSettings Settings(int gamma = 0, int brightness = 0, int contrast = 0) =>
        new(gamma, brightness, contrast);

    [Fact]
    public void NeutralSettingsProduceTheIdentityRamp()
    {
        var ramp = GammaRampCalculator.Calculate(AdjustmentSettings.Default);

        for (var channel = 0; channel < 3; channel++)
        {
            for (var i = 0; i < GammaRamp.ChannelLength; i++)
            {
                Assert.Equal((ushort)(i * 257), ramp[(channel * GammaRamp.ChannelLength) + i]);
            }
        }
    }

    [Fact]
    public void RampCoversThreeChannelsOf256Entries()
    {
        Assert.Equal(768, GammaRampCalculator.Calculate(Settings()).ToArray().Length);
    }

    [Theory]
    [InlineData(-100, -100, -100)]
    [InlineData(100, 100, 100)]
    [InlineData(-100, 100, -100)]
    [InlineData(100, -100, 100)]
    [InlineData(-50, 50, -20)]
    public void AnySettingsProduceANonDecreasingRamp(int gamma, int brightness, int contrast)
    {
        var ramp = GammaRampCalculator.Calculate(Settings(gamma, brightness, contrast));

        for (var i = 1; i < GammaRamp.ChannelLength; i++)
        {
            Assert.True(ramp[i] >= ramp[i - 1], $"Ramp decreased at index {i}.");
        }
    }

    [Fact]
    public void BrightnessAboveNeutralNeverDarkensAnyLevel()
    {
        var neutral = GammaRampCalculator.Calculate(Settings());
        var brighter = GammaRampCalculator.Calculate(Settings(brightness: 50));

        for (var i = 0; i < GammaRamp.ChannelLength; i++)
        {
            Assert.True(brighter[i] >= neutral[i], $"Level {i} got darker.");
        }
    }

    [Fact]
    public void GammaBelowNeutralDarkensMidtones()
    {
        var neutral = GammaRampCalculator.Calculate(Settings());
        var darker = GammaRampCalculator.Calculate(Settings(gamma: -60));

        Assert.True(darker[128] < neutral[128]);
    }

    [Fact]
    public void GammaLeavesTheEndpointsUntouched()
    {
        var ramp = GammaRampCalculator.Calculate(Settings(gamma: 80));

        Assert.Equal(0, ramp[0]);
        Assert.Equal(ushort.MaxValue, ramp[255]);
    }

    [Fact]
    public void ContrastAboveNeutralPushesLevelsAwayFromTheMidpoint()
    {
        var neutral = GammaRampCalculator.Calculate(Settings());
        var punchy = GammaRampCalculator.Calculate(Settings(contrast: 60));

        Assert.True(punchy[64] < neutral[64]);
        Assert.True(punchy[192] > neutral[192]);
    }

    [Fact]
    public void ContrastBelowNeutralPullsLevelsTowardTheMidpoint()
    {
        var neutral = GammaRampCalculator.Calculate(Settings());
        var flat = GammaRampCalculator.Calculate(Settings(contrast: -60));

        Assert.True(flat[64] > neutral[64]);
        Assert.True(flat[192] < neutral[192]);
    }

    [Fact]
    public void ExtremeSettingsStayWithinTheHardwareRange()
    {
        var ramp = GammaRampCalculator.Calculate(Settings(100, 100, 100)).ToArray();

        Assert.All(ramp, value => Assert.InRange(value, ushort.MinValue, ushort.MaxValue));
    }

    [Fact]
    public void IdentityMatchesTheNeutralCalculation()
    {
        Assert.Equal(GammaRampCalculator.Calculate(AdjustmentSettings.Default), GammaRampCalculator.Identity);
    }
}
