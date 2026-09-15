using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tarsier.Core.Models;

/// <summary>Writes a hotkey as its input list and reads back both that shape and the original modifiers-plus-key one.</summary>
public sealed class HotkeyJsonConverter : JsonConverter<Hotkey>
{
    private const string InputsProperty = "Inputs";
    private const string LegacyModifiersProperty = "Modifiers";
    private const string LegacyVirtualKeyProperty = "VirtualKey";

    public override Hotkey? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (root.TryGetProperty(InputsProperty, out var inputs))
        {
            return new Hotkey(inputs.EnumerateArray().Select(input => input.GetUInt32()));
        }

        return Hotkey.FromLegacy(
            (HotkeyModifiers)root.GetProperty(LegacyModifiersProperty).GetInt32(),
            root.GetProperty(LegacyVirtualKeyProperty).GetUInt32());
    }

    public override void Write(Utf8JsonWriter writer, Hotkey value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteStartArray(InputsProperty);
        foreach (var input in value.Inputs)
        {
            writer.WriteNumberValue(input);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
