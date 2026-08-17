using System.Reflection;
using MassTransit;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker.Routing;
using RabbitMQ.Client;

namespace Pottmayer.Tars.Messaging.MassTransit.RabbitMq.Topology;

/// <summary>
/// The topology steps, one concern per method. Each is usable on its own, so an application that
/// needs a bus configuration this package does not express can write its own
/// <c>UsingRabbitMq</c> block and still get the Tars naming, exchanges and bindings.
/// </summary>
public static class RabbitMqTopology
{
    /// <summary>Registers one relay consumer per subscribed event type.</summary>
    /// <remarks>
    /// MassTransit binds a consumer to a message type at registration, and the type is only known at
    /// runtime, so the closed generic is built here.
    /// </remarks>
    public static IBusRegistrationConfigurator AddTarsRelayConsumers(
        this IBusRegistrationConfigurator bus,
        IEnumerable<IntegrationEventSubscription> subscriptions)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(subscriptions);

        foreach (var subscription in subscriptions)
            bus.AddConsumer(typeof(IntegrationEventRelayConsumer<>).MakeGenericType(subscription.EventType));

        return bus;
    }

    /// <summary>Points the bus at a RabbitMQ host with the configured credentials.</summary>
    public static IRabbitMqBusFactoryConfigurator UseTarsHost(
        this IRabbitMqBusFactoryConfigurator cfg,
        string host,
        ushort port,
        string virtualHost,
        string username,
        string password,
        bool useSsl = false)
    {
        ArgumentNullException.ThrowIfNull(cfg);

        cfg.Host(host, port, virtualHost, h =>
        {
            h.Username(username);
            h.Password(password);

            if (useSsl)
                h.UseSsl(_ => { });
        });

        return cfg;
    }

    /// <summary>
    /// Names broker entities after the event's logical name instead of its .NET type.
    /// </summary>
    /// <remarks>
    /// Without this, MassTransit derives the exchange name from namespace and class name, so moving a
    /// record to another namespace quietly repoints it at a new exchange and existing consumers go
    /// silent.
    /// </remarks>
    public static IRabbitMqBusFactoryConfigurator UseTarsEntityNaming(this IRabbitMqBusFactoryConfigurator cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);

        cfg.MessageTopology.SetEntityNameFormatter(new TarsEntityNameFormatter());
        return cfg;
    }

    /// <summary>
    /// Sets the exchange type of every given event: fanout for a broadcast event, the routed type for
    /// one that carries a routing key.
    /// </summary>
    public static IRabbitMqBusFactoryConfigurator UseTarsPublishTopology(
        this IRabbitMqBusFactoryConfigurator cfg,
        IEnumerable<Type> eventTypes,
        string routedExchangeType)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        ArgumentNullException.ThrowIfNull(eventTypes);

        foreach (var eventType in eventTypes)
            cfg.UseTarsPublishTopology(eventType, routedExchangeType);

        return cfg;
    }

    /// <inheritdoc cref="UseTarsPublishTopology(IRabbitMqBusFactoryConfigurator, IEnumerable{Type}, string)"/>
    public static IRabbitMqBusFactoryConfigurator UseTarsPublishTopology(
        this IRabbitMqBusFactoryConfigurator cfg,
        Type eventType,
        string routedExchangeType)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        ArgumentNullException.ThrowIfNull(eventType);

        var exchangeType = typeof(IRoutedIntegrationEvent).IsAssignableFrom(eventType)
            ? routedExchangeType
            : ExchangeType.Fanout;

        // MassTransit's publish topology API is generic; the event type is only known at runtime, so
        // the closed method is bound once, at startup.
        typeof(RabbitMqTopology)
            .GetMethod(nameof(SetExchangeType), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(eventType)
            .Invoke(null, [cfg, exchangeType]);

        return cfg;
    }

    /// <summary>
    /// Binds the queue to the exchange a subscription names, with the pattern it asked for.
    /// </summary>
    /// <remarks>
    /// A broadcast subscription binds with <c>#</c>, which a fanout exchange ignores and a topic
    /// exchange reads as "everything under this name".
    /// </remarks>
    public static IRabbitMqReceiveEndpointConfigurator BindTarsSubscription(
        this IRabbitMqReceiveEndpointConfigurator endpoint,
        IntegrationEventSubscription subscription,
        string routedExchangeType)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(subscription);

        endpoint.Bind(subscription.Destination, binding =>
        {
            binding.ExchangeType = subscription.RoutingKeyPattern is null
                ? ExchangeType.Fanout
                : routedExchangeType;

            binding.RoutingKey = subscription.RoutingKeyPattern ?? "#";
        });

        return endpoint;
    }

    /// <inheritdoc cref="BindTarsSubscription(IRabbitMqReceiveEndpointConfigurator, IntegrationEventSubscription, string)"/>
    public static IRabbitMqReceiveEndpointConfigurator BindTarsSubscriptions(
        this IRabbitMqReceiveEndpointConfigurator endpoint,
        IEnumerable<IntegrationEventSubscription> subscriptions,
        string routedExchangeType)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);

        foreach (var subscription in subscriptions)
            endpoint.BindTarsSubscription(subscription, routedExchangeType);

        return endpoint;
    }

    /// <summary>
    /// Applies the retry policy. What it cannot absorb goes to the endpoint's error queue, which
    /// MassTransit creates and manages.
    /// </summary>
    public static IRabbitMqReceiveEndpointConfigurator UseTarsRetry(
        this IRabbitMqReceiveEndpointConfigurator endpoint,
        int retryLimit,
        TimeSpan retryInterval)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        endpoint.UseMessageRetry(r => r.Interval(retryLimit, retryInterval));
        return endpoint;
    }

    private static void SetExchangeType<TIntegrationEvent>(
        IRabbitMqBusFactoryConfigurator cfg, string exchangeType)
        where TIntegrationEvent : class
        => cfg.Publish<TIntegrationEvent>(topology => topology.ExchangeType = exchangeType);
}
