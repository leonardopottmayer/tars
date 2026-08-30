using System.Text.Json;
using Pottmayer.Tars.Messaging.Abstractions;

namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;

/// <summary>
/// Default <see cref="IIntegrationEventSerializer"/> over <c>System.Text.Json</c>. Serializes by the
/// event's runtime type so every declared property is captured; deserializes into the type the relay
/// resolved from the stored logical name.
/// </summary>
public sealed class JsonIntegrationEventSerializer(JsonSerializerOptions? options = null) : IIntegrationEventSerializer
{
    // Web defaults are a reasonable, broker-friendly baseline; an application can inject its own.
    private readonly JsonSerializerOptions _options = options ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public string SerializePayload(IIntegrationEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        // Runtime type, not IIntegrationEvent, so the concrete event's properties are all written.
        return JsonSerializer.Serialize(@event, @event.GetType(), _options);
    }

    public string? SerializeHeaders(IIntegrationEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return @event is IHeaderedIntegrationEvent headered && headered.Headers.Count > 0
            ? JsonSerializer.Serialize(headered.Headers, _options)
            : null;
    }

    public IIntegrationEvent DeserializePayload(Type eventType, string payload)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return JsonSerializer.Deserialize(payload, eventType, _options) as IIntegrationEvent
            ?? throw new InvalidOperationException(
                $"Outbox payload deserialized to null or to a type that is not an {nameof(IIntegrationEvent)}: {eventType.FullName}.");
    }
}
