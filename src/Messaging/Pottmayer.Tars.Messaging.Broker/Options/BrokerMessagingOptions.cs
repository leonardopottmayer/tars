using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker.Routing;

namespace Pottmayer.Tars.Messaging.Broker.Options;

/// <summary>
/// What this application publishes, what it subscribes to, and under what endpoint name. Shared by
/// every broker provider, so moving from one to another is a change of registration call and nothing
/// else.
/// </summary>
public sealed class BrokerMessagingOptions
{
    public const string SectionName = "Tars:Messaging:Broker";

    private readonly List<Assembly> _eventAssemblies = [];
    private readonly List<(Assembly Assembly, ServiceLifetime Lifetime)> _handlerAssemblies = [];
    private readonly List<IntegrationEventSubscription> _subscriptions = [];

    /// <summary>
    /// This consumer's identity on the broker: the queue name in RabbitMQ, the consumer group in
    /// Kafka. Two instances sharing it compete for messages; two different values each get a copy.
    /// </summary>
    public string EndpointName { get; set; } = "tars";

    /// <summary>
    /// The assemblies whose handlers should be registered, with the lifetime each was declared with.
    /// A provider iterates this and calls <c>AddTarsIntegrationEventHandlers</c> per entry.
    /// </summary>
    public IReadOnlyList<(Assembly Assembly, ServiceLifetime Lifetime)> HandlerAssemblies => _handlerAssemblies;

    /// <summary>What this application subscribes to. Providers read it to build their topology.</summary>
    public IReadOnlyList<IntegrationEventSubscription> Subscriptions => _subscriptions;

    /// <summary>
    /// Every event type this application knows about: the ones discovered in the registered
    /// assemblies plus the ones named by a subscription. Providers that must declare something per
    /// type up front — a Kafka producer is bound to one topic at registration — enumerate this.
    /// </summary>
    public IEnumerable<Type> DiscoverEventTypes()
        => _eventAssemblies
            .SelectMany(Registry.IntegrationEventTypeRegistry.DiscoverIn)
            .Concat(_subscriptions.Select(s => s.EventType))
            .Distinct();

    /// <summary>
    /// Registers the event types declared in an assembly, so the transport can name them on publish
    /// and resolve them on consume.
    /// </summary>
    public BrokerMessagingOptions RegisterEventsFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        _eventAssemblies.Add(assembly);
        return this;
    }

    /// <summary>Registers the <see cref="IIntegrationEventHandler{T}"/> implementations in an assembly.</summary>
    public BrokerMessagingOptions RegisterHandlersFromAssembly(
        Assembly assembly, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        _handlerAssemblies.Add((assembly, lifetime));
        return this;
    }

    /// <summary>
    /// Receives every message published under this event's name — the broadcast case, and the right
    /// one for an event that does not implement <see cref="IRoutedIntegrationEvent"/>.
    /// </summary>
    public BrokerMessagingOptions Subscribe<TIntegrationEvent>()
        where TIntegrationEvent : IIntegrationEvent
    {
        _subscriptions.Add(IntegrationEventSubscription.Broadcast<TIntegrationEvent>());
        return this;
    }

    /// <summary>
    /// Receives only the messages whose routing key matches <paramref name="pattern"/>, relative to
    /// the event name: <c>Subscribe&lt;InboundInteractionReceived&gt;("agenda.#")</c> binds
    /// <c>inbound.interaction.agenda.#</c> and leaves every other module's interactions alone.
    /// </summary>
    public BrokerMessagingOptions Subscribe<TIntegrationEvent>(string pattern)
        where TIntegrationEvent : IIntegrationEvent
    {
        _subscriptions.Add(IntegrationEventSubscription.Matching<TIntegrationEvent>(pattern));
        return this;
    }

    /// <summary>
    /// Fails fast when the declared topology needs something this broker does not have. Providers
    /// call this during registration, so a subscription Kafka cannot honour is a startup error and
    /// not a queue that silently never fills.
    /// </summary>
    /// <exception cref="InvalidOperationException">A subscription cannot be honoured.</exception>
    public void ValidateAgainst(BrokerCapabilities capabilities, string providerName)
    {
        foreach (var subscription in _subscriptions)
        {
            if (subscription.RoutingKeyPattern is null)
            {
                Require(capabilities, BrokerCapabilities.Broadcast, providerName, subscription,
                    "broadcast delivery");
                continue;
            }

            var needsWildcard = subscription.RoutingKeyPattern.Contains('*', StringComparison.Ordinal)
                || subscription.RoutingKeyPattern.Contains('#', StringComparison.Ordinal);

            Require(capabilities,
                needsWildcard ? BrokerCapabilities.WildcardRouting : BrokerCapabilities.KeyedRouting,
                providerName, subscription,
                needsWildcard ? "wildcard routing" : "keyed routing");
        }
    }

    private static void Require(
        BrokerCapabilities capabilities,
        BrokerCapabilities required,
        string providerName,
        IntegrationEventSubscription subscription,
        string described)
    {
        if (capabilities.HasFlag(required))
            return;

        throw new InvalidOperationException(
            $"The subscription to '{subscription.Destination}' " +
            $"(pattern '{subscription.RoutingKeyPattern ?? "(broadcast)"}') needs {described}, which " +
            $"{providerName} does not support. Change the subscription, or use a broker that has it.");
    }
}
