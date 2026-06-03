using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pottmayer.Tars.Core.Primitives;

/// <summary>
/// Factory to create <see cref="OptionalJsonConverter{T}"/> for any <see cref="Optional{T}"/> type.
/// Register globally so PATCH request DTOs with Optional properties deserialize correctly.
/// </summary>
public sealed class OptionalJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
            return false;

        return typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var inner = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(OptionalJsonConverter<>).MakeGenericType(inner);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }
}
