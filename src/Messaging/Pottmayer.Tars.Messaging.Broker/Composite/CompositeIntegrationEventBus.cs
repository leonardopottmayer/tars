using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker.Routing;

namespace Pottmayer.Tars.Messaging.Broker.Composite;

/// <summary>
/// The seam, preserved when an application publishes to more than one transport. It looks at each
/// event, asks the <see cref="IEventTransportSelector"/> which transport it belongs on, and delegates
/// to the keyed <see cref="IIntegrationEventBus"/> registered under that key.
/// </summary>
/// <param name="selector">Decides the transport(s) per event type.</param>
/// <param name="services">The <em>scoped</em> provider, used to resolve the keyed inner buses.</param>
/// <remarks>
/// <para>
/// Producers keep calling <see cref="IIntegrationEventBus.PublishAsync"/> against this one type and
/// never learn a second transport exists. The choice lives in the map, not in the call site — which
/// is what keeps the seam intact when the number of transports grows from one to many, and when a
/// single event fans out to several brokers at once.
/// </para>
/// <para>
/// Registered <em>scoped</em>, and it resolves the inner buses from the scope it was resolved in.
/// That matters for the same reason the RabbitMQ and Kafka buses are scoped: an outbox substitutes a
/// scoped publish endpoint, and a bus resolved from the root would publish straight past it. Keeping
/// the resolution scoped means the inner bus the composite delegates to is the outbox-aware one — and
/// a fan-out through an outbox writes one row per transport in the same transaction, so it is atomic.
/// </para>
/// <para>
/// When an event maps to several transports, every one is attempted; a failure on one does not stop
/// the others, and the failures are gathered into a single <see cref="AggregateException"/>. Without
/// an outbox this means a partial publish is possible — consumers are idempotent on
/// <see cref="IIntegrationEvent.EventId"/>, which is what makes a retry safe.
/// </para>
/// </remarks>
public sealed class CompositeIntegrationEventBus(
    IEventTransportSelector selector,
    IServiceProvider services)
    : IIntegrationEventBus
{
    /// <inheritdoc />
    public Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var transports = selector.SelectFor(@event.GetType());

        // The overwhelmingly common case is one transport: publish directly so its exception surfaces
        // as-is, with no aggregation wrapper.
        return transports.Count == 1
            ? Resolve(transports[0], @event).PublishAsync(@event, cancellationToken)
            : PublishToManyAsync(transports, @event, cancellationToken);
    }

    private async Task PublishToManyAsync(
        IReadOnlyList<string> transports, IIntegrationEvent @event, CancellationToken cancellationToken)
    {
        List<Exception>? failures = null;

        foreach (var transport in transports)
        {
            try
            {
                await Resolve(transport, @event).PublishAsync(@event, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                (failures ??= []).Add(ex);
            }
        }

        if (failures is { Count: > 0 })
        {
            throw new AggregateException(
                $"Publishing {@event.GetType().Name} failed on {failures.Count} of {transports.Count} " +
                "transports it fans out to.", failures);
        }
    }

    private IIntegrationEventBus Resolve(string transport, IIntegrationEvent @event)
        => services.GetKeyedService<IIntegrationEventBus>(transport)
            ?? throw new InvalidOperationException(
                $"{@event.GetType().Name} is routed to transport '{transport}', but no keyed " +
                $"IIntegrationEventBus is registered under that key. Register the transport (for " +
                "example AddTarsKeyedRabbitMqIntegrationEventBus) or fix the route in the transport map.");
}
