using Pottmayer.Tars.Messaging.Abstractions;

namespace Pottmayer.Tars.Messaging.Broker.Routing;

/// <summary>
/// Turns an event into the route a provider should publish it on. Replace the default to impose a
/// house convention — a tenant prefix, an environment segment — without touching any provider.
/// </summary>
public interface IIntegrationEventRouter
{
    /// <summary>Resolves the route a provider should publish the given event on.</summary>
    /// <param name="event">The event about to be published.</param>
    /// <returns>The destination, routing key and headers the event should be published with.</returns>
    IntegrationEventRoute Resolve(IIntegrationEvent @event);
}
