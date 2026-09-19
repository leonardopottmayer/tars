using Pottmayer.Tars.Messaging.Abstractions;

namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.Relay;

/// <summary>
/// The relay's last mile: where a drained, deserialized outbox event is handed off. Two shapes ship —
/// <see cref="LocalHandlerOutboxDelivery"/> (the default) hands it to the in-process
/// <c>IIntegrationEventHandler&lt;T&gt;</c> implementations, and <see cref="BrokerOutboxDelivery"/>
/// forwards it to a transport bus so the same outbox table can feed Kafka or RabbitMQ transactionally.
/// </summary>
/// <remarks>
/// The seam exists so the durability decision (write once, in the producer's transaction) stays
/// independent of the destination decision (deliver locally, or publish to a broker). The row is
/// written the same way in both; only who drains it — and where the event goes — changes.
/// </remarks>
public interface IOutboxRelayDelivery
{
    /// <summary>
    /// Delivers one event. Exceptions propagate so the relay can record the failure and retry with
    /// backoff, exactly as it already does for local dispatch.
    /// </summary>
    Task DeliverAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default);
}
