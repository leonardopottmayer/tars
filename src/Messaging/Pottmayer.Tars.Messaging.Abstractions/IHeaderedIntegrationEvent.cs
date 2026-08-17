namespace Pottmayer.Tars.Messaging.Abstractions;

/// <summary>
/// Opt-in marker for an event that carries transport headers alongside its body — a tenant id, a
/// correlation id, a schema version. Every broker this framework targets can carry headers.
/// </summary>
/// <remarks>
/// <para>
/// Carrying headers is portable. <em>Routing</em> by them is not: RabbitMQ has headers exchanges,
/// Azure Service Bus and SNS have property filters, and <strong>Kafka has no broker-side header
/// filtering at all</strong>. So headers here are metadata that always travels; using them to decide
/// delivery is a provider-specific choice, configured on the provider and rejected at startup by one
/// that cannot honour it.
/// </para>
/// <para>
/// Values are strings because that is the intersection of what brokers guarantee. Anything richer
/// belongs in the event body, which is versioned and validated; headers are not.
/// </para>
/// </remarks>
public interface IHeaderedIntegrationEvent
{
    /// <summary>Header names should be lowercase and dot-separated, e.g. <c>tars.tenant-id</c>.</summary>
    IReadOnlyDictionary<string, string> Headers { get; }
}
