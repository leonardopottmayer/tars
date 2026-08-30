using Pottmayer.Tars.Messaging.Abstractions;

namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;

/// <summary>
/// Turns an integration event into the strings the outbox row stores, and back. Kept behind an
/// interface so an application can swap the wire format (e.g. to match a broker it will later adopt)
/// without touching the bus or the relay.
/// </summary>
public interface IIntegrationEventSerializer
{
    /// <summary>Serializes the event body to the payload string.</summary>
    string SerializePayload(IIntegrationEvent @event);

    /// <summary>Serializes <see cref="IHeaderedIntegrationEvent.Headers"/> to a string, or returns null when the event carries none.</summary>
    string? SerializeHeaders(IIntegrationEvent @event);

    /// <summary>Reconstructs an event of <paramref name="eventType"/> from a stored payload.</summary>
    IIntegrationEvent DeserializePayload(Type eventType, string payload);
}
