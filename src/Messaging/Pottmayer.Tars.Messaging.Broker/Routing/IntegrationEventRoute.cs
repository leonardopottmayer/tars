namespace Pottmayer.Tars.Messaging.Broker.Routing;

/// <summary>
/// Where one event goes, in terms every broker can honour. A provider translates this into its own
/// vocabulary — a RabbitMQ exchange plus routing key, a Kafka topic, an Azure Service Bus topic
/// plus subject — without any caller learning that vocabulary.
/// </summary>
/// <param name="Destination">
/// The logical event name (see <see cref="Abstractions.IntegrationEventNaming"/>). This is the
/// exchange in RabbitMQ, and the topic root in Kafka.
/// </param>
/// <param name="RoutingKey">
/// Null for a <see cref="IsBroadcast">broadcast</see> event. Otherwise the full key,
/// <c>{Destination}.{suffix}</c>, built from <see cref="Abstractions.IRoutedIntegrationEvent"/>.
/// </param>
/// <param name="Headers">
/// Metadata that travels with the message on every broker. Whether it can also decide delivery is
/// provider-specific — Kafka has no broker-side header filtering.
/// </param>
public sealed record IntegrationEventRoute(
    string Destination,
    string? RoutingKey,
    IReadOnlyDictionary<string, string> Headers)
{
    /// <summary>
    /// True when the event named no destination of its own, so every subscriber receives it. This is
    /// the fanout case, and the default for an event that does not implement
    /// <see cref="Abstractions.IRoutedIntegrationEvent"/>.
    /// </summary>
    public bool IsBroadcast => RoutingKey is null;
}
