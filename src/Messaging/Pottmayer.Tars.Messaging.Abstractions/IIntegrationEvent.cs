namespace Pottmayer.Tars.Messaging.Abstractions;

/// <summary>
/// Marker for an integration event: a fact published by one module/service for others to react to.
/// Integration events cross boundaries, so they must be broker-ready POCOs (plain
/// <see cref="System.Guid"/>/<see cref="string"/>/<see cref="System.DateTimeOffset"/>) and must not leak
/// domain value objects. This lets the transport be swapped (in-process today, a broker tomorrow)
/// without touching producers or consumers.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Stable identity of this event occurrence. Consumers use it for idempotency/dedup.</summary>
    Guid EventId { get; }

    /// <summary>When the event occurred (UTC).</summary>
    DateTimeOffset OccurredAt { get; }
}
