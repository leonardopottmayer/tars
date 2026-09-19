using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Messaging.Abstractions;

namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.Relay;

/// <summary>
/// Forwards a drained outbox event to a broker: it resolves the transport bus registered under
/// <paramref name="transportKey"/> and publishes through it. This is what turns the in-process outbox
/// into a transactional outbox <em>to a broker</em> — the event is written once in the producer's
/// transaction, and the relay hands it to Kafka or RabbitMQ afterwards, so a crash between the two
/// redelivers rather than loses it (at-least-once, same as local dispatch).
/// </summary>
/// <param name="scopeFactory">Creates the scope the keyed bus is resolved from.</param>
/// <param name="transportKey">
/// The key of the transport bus to forward to — the same key a composite route or
/// <c>AddTarsKeyed{RabbitMq,Kafka}IntegrationEventBus</c> registers the bus under.
/// </param>
/// <remarks>
/// <para>
/// The target is deliberately a <strong>keyed</strong> <see cref="IIntegrationEventBus"/>, not the
/// ambient one. <c>AddTarsOutboxBus</c> replaces the ambient (unkeyed) bus with the outbox writer, so
/// resolving the ambient bus here would loop straight back into the outbox; the keyed transport bus is
/// the real thing, and it survives that replacement.
/// </para>
/// <para>
/// A fresh scope per event mirrors the local dispatcher: the transport buses are scoped (an outbox can
/// substitute a scoped publish endpoint), so a bus captured on a singleton would resolve from the root
/// and publish past that substitution.
/// </para>
/// </remarks>
public sealed class BrokerOutboxDelivery(IServiceScopeFactory scopeFactory, string transportKey)
    : IOutboxRelayDelivery
{
    /// <inheritdoc />
    public async Task DeliverAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var bus = scope.ServiceProvider.GetKeyedService<IIntegrationEventBus>(transportKey)
            ?? throw new InvalidOperationException(
                $"The outbox relay is configured to forward to transport '{transportKey}', but no keyed " +
                $"IIntegrationEventBus is registered under that key. Register the transport (for example " +
                $"AddTarsKeyedKafkaIntegrationEventBus(\"{transportKey}\") or a composite route to it) " +
                "before AddTarsOutboxBrokerDelivery.");

        await bus.PublishAsync(@event, cancellationToken).ConfigureAwait(false);
    }
}
