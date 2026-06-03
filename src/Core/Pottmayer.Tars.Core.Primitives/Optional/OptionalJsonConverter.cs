using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pottmayer.Tars.Core.Primitives;

/// <summary>
/// JSON converter for <see cref="Optional{T}"/>. When a property is present in JSON (including null), deserializes as Some(value);
/// when the property is absent, the property remains default (Absent). Use for PATCH request bodies.
/// </summary>
public sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
{
    public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return Optional<T>.Some(default);

        var value = JsonSerializer.Deserialize<T>(ref reader, options);
        return Optional<T>.Some(value);
    }

    public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
    {
        if (!value.IsPresent)
        {
            writer.WriteNullValue();
            return;
        }

        if (value.Value is null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, value.Value, options);
    }
}
