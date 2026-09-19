using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker.Dispatch;

namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.Relay;

/// <summary>
/// The default last mile: hands the event to the local <c>IIntegrationEventHandler&lt;T&gt;</c>
/// implementations through the shared <see cref="IIntegrationEventDispatcher"/> — the in-process
/// modular-monolith path. This is what the relay uses when no other delivery is registered.
/// </summary>
public sealed class LocalHandlerOutboxDelivery(IIntegrationEventDispatcher dispatcher) : IOutboxRelayDelivery
{
    /// <inheritdoc />
    public Task DeliverAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
        => dispatcher.DispatchAsync(@event, cancellationToken);
}
