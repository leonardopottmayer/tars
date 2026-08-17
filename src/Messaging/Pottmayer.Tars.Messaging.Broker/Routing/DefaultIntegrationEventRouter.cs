using Pottmayer.Tars.Messaging.Abstractions;

namespace Pottmayer.Tars.Messaging.Broker.Routing;

/// <summary>
/// The default routing rule: the destination is the event's logical name, the routing key exists
/// only when the event opted into <see cref="IRoutedIntegrationEvent"/>, and headers travel when the
/// event opted into <see cref="IHeaderedIntegrationEvent"/>.
/// </summary>
public sealed class DefaultIntegrationEventRouter : IIntegrationEventRouter
{
    private static readonly IReadOnlyDictionary<string, string> NoHeaders =
        new Dictionary<string, string>(0);

    public IntegrationEventRoute Resolve(IIntegrationEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var destination = IntegrationEventNaming.For(@event.GetType());

        var routingKey = @event is IRoutedIntegrationEvent routed
            ? BuildRoutingKey(destination, routed.RoutingKeySuffix, @event.GetType())
            : null;

        var headers = @event is IHeaderedIntegrationEvent headered
            ? headered.Headers ?? NoHeaders
            : NoHeaders;

        return new IntegrationEventRoute(destination, routingKey, headers);
    }

    private static string BuildRoutingKey(string destination, string suffix, Type eventType)
    {
        if (string.IsNullOrWhiteSpace(suffix))
        {
            throw new InvalidOperationException(
                $"{eventType.Name} implements {nameof(IRoutedIntegrationEvent)} but returned an empty " +
                "routing key suffix. Either return a suffix or stop implementing the interface — an " +
                "event with an empty key would be published where nothing is bound.");
        }

        // A wildcard in a published key never matches anything: they belong in subscriptions only.
        if (suffix.Contains('*', StringComparison.Ordinal) || suffix.Contains('#', StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{eventType.Name} returned the routing key suffix '{suffix}', which contains a " +
                "wildcard. Wildcards belong in a subscription pattern, never in a published key.");
        }

        return $"{destination}.{suffix.Trim('.')}";
    }
}
