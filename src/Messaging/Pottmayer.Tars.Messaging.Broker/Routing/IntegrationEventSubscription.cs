using Pottmayer.Tars.Messaging.Abstractions;

namespace Pottmayer.Tars.Messaging.Broker.Routing;

/// <summary>
/// What one consumer wants to receive. A provider turns this into a queue binding (RabbitMQ), a
/// topic subscription (Kafka, Azure Service Bus) or a filter policy (SNS).
/// </summary>
/// <param name="EventType">The .NET type the payload is deserialized into before the last mile.</param>
/// <param name="Destination">The logical event name, i.e. what the publisher publishes to.</param>
/// <param name="RoutingKeyPattern">
/// Null to receive every message on the destination — the broadcast case. Otherwise a pattern in the
/// portable wildcard syntax: <c>*</c> matches one segment, <c>#</c> matches one or more.
/// </param>
public sealed record IntegrationEventSubscription(
    Type EventType,
    string Destination,
    string? RoutingKeyPattern)
{
    /// <summary>Receive every message published under this event's name.</summary>
    public static IntegrationEventSubscription Broadcast<TIntegrationEvent>()
        where TIntegrationEvent : IIntegrationEvent
        => new(typeof(TIntegrationEvent), IntegrationEventNaming.For<TIntegrationEvent>(), RoutingKeyPattern: null);

    /// <summary>
    /// Receive only the messages whose routing key matches <paramref name="pattern"/>. The pattern is
    /// relative to the event name, so <c>agenda.#</c> on <c>inbound.interaction</c> becomes
    /// <c>inbound.interaction.agenda.#</c>.
    /// </summary>
    public static IntegrationEventSubscription Matching<TIntegrationEvent>(string pattern)
        where TIntegrationEvent : IIntegrationEvent
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var destination = IntegrationEventNaming.For<TIntegrationEvent>();
        return new IntegrationEventSubscription(
            typeof(TIntegrationEvent), destination, $"{destination}.{pattern.Trim('.')}");
    }
}
