using MassTransit;
using Pottmayer.Tars.Messaging.Broker.Routing;

namespace Pottmayer.Tars.Messaging.MassTransit;

/// <summary>
/// Applies the portable <see cref="IntegrationEventRoute"/> to a transport that has its own idea of
/// addressing. RabbitMQ sets a routing key on the send context; Kafka picks a partition key; a
/// broker with neither does nothing.
/// </summary>
/// <remarks>
/// This is the seam that keeps the core MassTransit package free of any broker reference. Each
/// transport package registers one applier, and the bus applies whatever is registered.
/// </remarks>
public interface IPublishRouteApplier
{
    void Apply(PublishContext context, IntegrationEventRoute route);
}
