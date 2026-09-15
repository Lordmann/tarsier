using System.Text.Json.Serialization;

namespace Tarsier.Core.Models;

/// <summary>Gamma, brightness, contrast and vibrance expressed on the -100..100 slider scale where 0 leaves the display untouched.</summary>
public sealed record AdjustmentSettings
{
    public const int Min = -100;
    public const int Max = 100;
    public const int Neutral = 0;

    private readonly int _gamma = Neutral;
    private readonly int _brightness = Neutral;
    private readonly int _contrast = Neutral;
    private readonly int _vibrance = Neutral;

    public static AdjustmentSettings Default { get; } = new(Neutral, Neutral, Neutral);

    [JsonConstructor]
    public AdjustmentSettings(int gamma, int brightness, int contrast, int vibrance = Neutral)
    {
        Gamma = gamma;
        Brightness = brightness;
        Contrast = contrast;
        Vibrance = vibrance;
    }

    public int Gamma
    {
        get => _gamma;
        init => _gamma = Validated(value, nameof(Gamma));
    }

    public int Brightness
    {
        get => _brightness;
        init => _brightness = Validated(value, nameof(Brightness));
    }

    public int Contrast
    {
        get => _contrast;
        init => _contrast = Validated(value, nameof(Contrast));
    }

    /// <summary>Colour saturation. Unlike the other three this cannot be reached through the gamma ramp.</summary>
    public int Vibrance
    {
        get => _vibrance;
        init => _vibrance = Validated(value, nameof(Vibrance));
    }

    public bool IsNeutral => Gamma == Neutral && Brightness == Neutral && Contrast == Neutral && Vibrance == Neutral;

    /// <summary>
    /// Shifts gamma, brightness and contrast by <paramref name="step"/>, clamped to the scale. Vibrance is
    /// saturation rather than intensity and is left alone.
    /// </summary>
    public AdjustmentSettings Intensified(int step) =>
        new(Stepped(Gamma, step), Stepped(Brightness, step), Stepped(Contrast, step), Vibrance);

    private static int Stepped(int value, int step) => Math.Clamp(value + step, Min, Max);

    private static int Validated(int value, string name) => value is >= Min and <= Max
        ? value
        : throw new ArgumentOutOfRangeException(name, value, $"Must be between {Min} and {Max}.");
}
