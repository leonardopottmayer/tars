using MassTransit;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker.Routing;

namespace Pottmayer.Tars.Messaging.MassTransit;

/// <summary>
/// Publishes through MassTransit. Producers keep calling <see cref="IIntegrationEventBus"/> and never
/// learn which framework or broker is underneath.
/// </summary>
public sealed class MassTransitIntegrationEventBus(
    IPublishEndpoint publishEndpoint,
    IIntegrationEventRouter router,
    IEnumerable<IPublishRouteApplier> routeAppliers)
    : IIntegrationEventBus
{
    private readonly IPublishRouteApplier[] _routeAppliers = [.. routeAppliers];

    public Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var route = router.Resolve(@event);

        var pipe = Pipe.Execute<PublishContext>(context =>
        {
            // Headers travel on every broker, whether or not it can route by them.
            foreach (var (name, value) in route.Headers)
                context.Headers.Set(name, value);

            foreach (var applier in _routeAppliers)
                applier.Apply(context, route);
        });

        // Publishing by runtime type, because the caller holds the interface.
        return publishEndpoint.Publish(@event, @event.GetType(), pipe, cancellationToken);
    }
}
