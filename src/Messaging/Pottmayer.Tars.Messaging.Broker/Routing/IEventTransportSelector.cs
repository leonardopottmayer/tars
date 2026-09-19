using Pottmayer.Tars.Messaging.Abstractions;

namespace Pottmayer.Tars.Messaging.Broker.Routing;

/// <summary>
/// Decides <em>which transport</em> an event is published on when an application runs more than one
/// at once — RabbitMQ and Kafka side by side, for example.
/// </summary>
/// <remarks>
/// <para>
/// This is a different question from <see cref="IIntegrationEventRouter"/>. The router answers "where
/// does this event go <em>within</em> a transport" (which exchange, which routing key, which topic).
/// The selector answers "which transport in the first place". An application with a single transport
/// never needs one; the seam collapses back to the one bus.
/// </para>
/// <para>
/// The decision is a property of the event <em>type</em> and is fixed for the life of the process, so
/// it is declared once, in configuration, and every producer stays unaware of it — they keep calling
/// <see cref="IIntegrationEventBus.PublishAsync"/> and never learn which broker answered.
/// </para>
/// </remarks>
public interface IEventTransportSelector
{
    /// <summary>
    /// Returns the transport keys the given event type is published on — one for the common case, or
    /// several when the event fans out to more than one broker. Each key matches a keyed
    /// <see cref="IIntegrationEventBus"/> registration (see the composite bus).
    /// </summary>
    /// <param name="eventType">The runtime type of the event about to be published.</param>
    /// <returns>The non-empty, distinct list of transport keys to publish through.</returns>
    IReadOnlyList<string> SelectFor(Type eventType);

    /// <summary>
    /// Convenience overload: selects for the runtime type of <paramref name="event"/>.
    /// </summary>
    /// <param name="event">The event about to be published.</param>
    /// <returns>The non-empty, distinct list of transport keys to publish through.</returns>
    IReadOnlyList<string> SelectFor(IIntegrationEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);
        return SelectFor(@event.GetType());
    }
}
