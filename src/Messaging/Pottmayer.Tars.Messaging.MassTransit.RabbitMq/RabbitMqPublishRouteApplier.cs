using MassTransit;
using Pottmayer.Tars.Messaging.Broker.Routing;

namespace Pottmayer.Tars.Messaging.MassTransit.RabbitMq;

/// <summary>
/// Puts the portable routing key onto RabbitMQ's send context, which is what a topic or direct
/// exchange matches bindings against.
/// </summary>
/// <remarks>
/// A broadcast event sets no key, so it lands on a fanout exchange and reaches every bound queue.
/// That is the default, and the right shape for an event with nobody in particular to route to.
/// </remarks>
public sealed class RabbitMqPublishRouteApplier : IPublishRouteApplier
{
    /// <inheritdoc />
    public void Apply(PublishContext context, IntegrationEventRoute route)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(route);

        if (route.RoutingKey is null)
            return;

        if (context.TryGetPayload<RabbitMqSendContext>(out var rabbit))
            rabbit.RoutingKey = route.RoutingKey;
    }
}
