using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Tarsier.Core.Models;

/// <summary>Modifier flags of the original hotkey format, kept so profiles saved with it still load.</summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8
}

/// <summary>
/// A combination of keys, mouse buttons and at most one wheel direction bound to a profile. It fires once every
/// input is down, in whichever order they were pressed. Codes are kept platform-neutral so the core stays UI-free.
/// </summary>
[JsonConverter(typeof(HotkeyJsonConverter))]
public sealed class Hotkey : IEquatable<Hotkey>
{
    public Hotkey(params uint[] inputs) : this((IEnumerable<uint>)inputs)
    {
    }

    public Hotkey(IEnumerable<uint> inputs)
    {
        if (inputs is null)
        {
            throw new ArgumentNullException(nameof(inputs));
        }

        Inputs = inputs.Select(InputCode.Normalize).Distinct().OrderBy(code => code).ToImmutableArray();
        if (Inputs.IsEmpty)
        {
            throw new ArgumentException("A hotkey needs at least one input.", nameof(inputs));
        }

        if (Inputs.Count(InputCode.IsWheel) > 1)
        {
            throw new ArgumentException("A hotkey can hold at most one wheel direction.", nameof(inputs));
        }
    }

    public ImmutableArray<uint> Inputs { get; }

    public static Hotkey FromLegacy(HotkeyModifiers modifiers, uint virtualKey)
    {
        var inputs = new List<uint> { virtualKey };
        Add(HotkeyModifiers.Control, InputCode.Control);
        Add(HotkeyModifiers.Alt, InputCode.Alt);
        Add(HotkeyModifiers.Shift, InputCode.Shift);
        Add(HotkeyModifiers.Windows, InputCode.Windows);
        return new Hotkey(inputs);

        void Add(HotkeyModifiers flag, uint code)
        {
            if (modifiers.HasFlag(flag))
            {
                inputs.Add(code);
            }
        }
    }

    public bool Equals(Hotkey? other) => other is not null && Inputs.SequenceEqual(other.Inputs);

    public override bool Equals(object? obj) => Equals(obj as Hotkey);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var input in Inputs)
        {
            hash.Add(input);
        }

        return hash.ToHashCode();
    }

    public override string ToString() => string.Join("+", Inputs);
}
