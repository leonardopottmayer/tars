using Pottmayer.Tars.Messaging.Abstractions;

namespace Pottmayer.Tars.Messaging.Broker.Routing;

/// <summary>
/// Turns an event into the route a provider should publish it on. Replace the default to impose a
/// house convention — a tenant prefix, an environment segment — without touching any provider.
/// </summary>
public interface IIntegrationEventRouter
{
    IntegrationEventRoute Resolve(IIntegrationEvent @event);
}
