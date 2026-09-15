using Tarsier.Core.Models;

namespace Tarsier.Core.Gamma;

/// <summary>
/// Turns the -100..100 sliders into a hardware gamma ramp. Each level is pushed through gamma, then contrast,
/// then brightness, so that a profile left at 0/0/0 reproduces the display's untouched output exactly.
/// </summary>
public static class GammaRampCalculator
{
    private const double MaxGammaExponent = 2.5;
    private const double MaxContrastFactor = 2.0;
    private const double MaxBrightnessOffset = 0.5;
    private const double Midpoint = 0.5;
    private const int LastLevel = GammaRamp.ChannelLength - 1;

    public static GammaRamp Identity { get; } = Calculate(AdjustmentSettings.Default);

    public static GammaRamp Calculate(AdjustmentSettings settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        var gammaExponent = Math.Pow(MaxGammaExponent, Offset(settings.Gamma));
        var contrastFactor = Math.Pow(MaxContrastFactor, Offset(settings.Contrast));
        var brightnessOffset = Offset(settings.Brightness) * MaxBrightnessOffset;

        var values = new ushort[GammaRamp.TotalLength];
        for (var level = 0; level < GammaRamp.ChannelLength; level++)
        {
            var output = ToLevel(Adjust((double)level / LastLevel, gammaExponent, contrastFactor, brightnessOffset));
            values[level] = output;
            values[GammaRamp.ChannelLength + level] = output;
            values[(GammaRamp.ChannelLength * 2) + level] = output;
        }

        return new GammaRamp(values);
    }

    private static double Adjust(double value, double gammaExponent, double contrastFactor, double brightnessOffset)
    {
        var adjusted = Math.Pow(value, 1.0 / gammaExponent);
        adjusted = ((adjusted - Midpoint) * contrastFactor) + Midpoint;
        return adjusted + brightnessOffset;
    }

    /// <summary>Maps a slider onto -1..1, where 0 means "leave this channel alone".</summary>
    private static double Offset(int slider) => slider / (double)AdjustmentSettings.Max;

    private static ushort ToLevel(double value) =>
        (ushort)Math.Round(Math.Clamp(value, 0.0, 1.0) * ushort.MaxValue);
}
