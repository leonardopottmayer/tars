using Pottmayer.Tars.Messaging.Abstractions;

namespace Pottmayer.Tars.Messaging.Broker.Dispatch;

/// <summary>
/// The "last mile": hands a deserialized event to the local
/// <see cref="IIntegrationEventHandler{T}"/> implementations. Providers call this after the broker
/// has delivered and the payload has been typed, so the re-dispatch is written once rather than once
/// per framework.
/// </summary>
public interface IIntegrationEventDispatcher
{
    /// <summary>
    /// Resolves handlers in a fresh scope and invokes them.
    /// </summary>
    /// <remarks>
    /// Unlike the in-process bus, this <strong>propagates</strong> handler exceptions. That is the
    /// point of running on a broker: a failure has to reach the transport so its retry and
    /// dead-letter machinery can act. Swallowing here would turn a durable queue back into
    /// fire-and-forget.
    /// </remarks>
    Task DispatchAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default);
}
