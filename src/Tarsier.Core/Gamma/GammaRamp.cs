namespace Tarsier.Core.Gamma;

/// <summary>A hardware gamma ramp: 256 16-bit levels for each of the red, green and blue channels.</summary>
public sealed class GammaRamp : IEquatable<GammaRamp>
{
    public const int ChannelLength = 256;
    public const int TotalLength = ChannelLength * 3;

    private readonly ushort[] _values;

    public GammaRamp(ushort[] values)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        if (values.Length != TotalLength)
        {
            throw new ArgumentException($"A gamma ramp needs exactly {TotalLength} entries.", nameof(values));
        }

        _values = values;
    }

    public ushort this[int index] => _values[index];

    public ushort[] ToArray() => (ushort[])_values.Clone();

    public bool Equals(GammaRamp? other) => other is not null && _values.AsSpan().SequenceEqual(other._values);

    public override bool Equals(object? obj) => Equals(obj as GammaRamp);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var value in _values)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }
}
