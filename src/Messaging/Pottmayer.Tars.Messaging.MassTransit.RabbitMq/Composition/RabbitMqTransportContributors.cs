using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker;
using Pottmayer.Tars.Messaging.Broker.Options;
using Pottmayer.Tars.Messaging.Broker.Routing;
using Pottmayer.Tars.Messaging.MassTransit.Composition;
using Pottmayer.Tars.Messaging.MassTransit.RabbitMq.DI;
using Pottmayer.Tars.Messaging.MassTransit.RabbitMq.Options;
using Pottmayer.Tars.Messaging.MassTransit.RabbitMq.Topology;

namespace Pottmayer.Tars.Messaging.MassTransit.RabbitMq.Composition;

/// <summary>
/// A RabbitMQ transport contributed as the composite's <em>primary</em> bus (added without a marker
/// interface).
/// </summary>
public sealed class RabbitMqBusTransport(string key, MassTransitRabbitMqMessagingOptions options)
    : IMassTransitBusTransport
{
    /// <inheritdoc />
    public string Key => key;

    /// <inheritdoc />
    public string TransportName => "RabbitMQ";

    /// <inheritdoc />
    public BrokerMessagingOptions Messaging => options.Messaging;

    /// <inheritdoc />
    public bool IsPrimaryCandidate => true;

    /// <inheritdoc />
    public void Validate() => RabbitMqCompositeWiring.Validate(options);

    /// <inheritdoc />
    public void RegisterKeyedBus(IServiceCollection services)
    {
        services.AddTarsRabbitMqRouteApplier();
        services.AddTarsKeyedRabbitMqIntegrationEventBus(key);
    }

    /// <inheritdoc />
    public void ConfigurePrimary(IBusRegistrationConfigurator bus)
    {
        options.ConfigureRegistration?.Invoke(bus);
        bus.AddTarsRelayConsumers(options.Messaging.Subscriptions);
    }

    /// <inheritdoc />
    public void UsePrimaryBus(IBusRegistrationConfigurator bus)
        => bus.UsingRabbitMq((context, cfg) => RabbitMqCompositeWiring.ConfigureFactory(cfg, context, options));

    /// <inheritdoc />
    public void RegisterMultibus(IServiceCollection services)
        => throw new NotSupportedException("A primary RabbitMQ transport is not a multibus bus.");
}

/// <summary>
/// A RabbitMQ transport contributed as its own MassTransit multibus bus, identified by the marker
/// interface <typeparamref name="TBus"/> (added with <c>AddRabbitMq&lt;TBus&gt;</c>).
/// </summary>
/// <typeparam name="TBus">The marker interface that is this bus's compile-time type identity.</typeparam>
public sealed class RabbitMqMultibusTransport<TBus>(string key, MassTransitRabbitMqMessagingOptions options)
    : IMassTransitBusTransport
    where TBus : class, IBus
{
    /// <inheritdoc />
    public string Key => key;

    /// <inheritdoc />
    public string TransportName => "RabbitMQ";

    /// <inheritdoc />
    public BrokerMessagingOptions Messaging => options.Messaging;

    /// <inheritdoc />
    public bool IsPrimaryCandidate => false;

    /// <inheritdoc />
    public void Validate() => RabbitMqCompositeWiring.Validate(options);

    /// <inheritdoc />
    public void RegisterKeyedBus(IServiceCollection services)
    {
        services.AddTarsRabbitMqRouteApplier();

        // The bus TBus is an IPublishEndpoint, so the standard MassTransit publish bus can point at it.
        services.TryAddKeyedScoped<IIntegrationEventBus>(key, (sp, _) =>
            new MassTransitIntegrationEventBus(
                sp.GetRequiredService<TBus>(),
                sp.GetRequiredService<IIntegrationEventRouter>(),
                sp.GetServices<IPublishRouteApplier>()));
    }

    /// <inheritdoc />
    public void ConfigurePrimary(IBusRegistrationConfigurator bus)
        => throw new NotSupportedException("A multibus RabbitMQ transport is not the primary bus.");

    /// <inheritdoc />
    public void UsePrimaryBus(IBusRegistrationConfigurator bus)
        => throw new NotSupportedException("A multibus RabbitMQ transport is not the primary bus.");

    /// <inheritdoc />
    public void RegisterMultibus(IServiceCollection services)
        => services.AddMassTransit<TBus>(bus =>
        {
            options.ConfigureRegistration?.Invoke(bus);
            bus.AddTarsRelayConsumers(options.Messaging.Subscriptions);
            bus.UsingRabbitMq((context, cfg) => RabbitMqCompositeWiring.ConfigureFactory(cfg, context, options));
        });
}

/// <summary>
/// The RabbitMQ wiring shared by the primary and multibus contributors: validation and the Tars bus
/// factory configuration, identical wherever the bus is hosted.
/// </summary>
internal static class RabbitMqCompositeWiring
{
    public static void Validate(MassTransitRabbitMqMessagingOptions options)
    {
        if (!options.IsValid())
            throw new InvalidOperationException(MassTransitRabbitMqMessagingOptions.ValidationErrorMessage);

        options.Messaging.ValidateAgainst(BrokerCapabilities.Amqp, "RabbitMQ");
    }

    public static void ConfigureFactory(
        IRabbitMqBusFactoryConfigurator cfg, IRegistrationContext context, MassTransitRabbitMqMessagingOptions options)
    {
        var eventTypes = options.Messaging.DiscoverEventTypes().ToArray();

        cfg.UseTarsHost(
            options.Host, options.Port, options.VirtualHost,
            options.Username, options.Password, options.UseSsl);

        cfg.UseTarsEntityNaming();
        cfg.UseTarsPublishTopology(eventTypes, options.RoutedExchangeType);

        cfg.ReceiveEndpoint(
            options.Messaging.EndpointName,
            (IRabbitMqReceiveEndpointConfigurator endpoint) =>
            {
                endpoint.PrefetchCount = options.PrefetchCount;
                endpoint.UseTarsRetry(options.RetryLimit, options.RetryInterval);
                endpoint.BindTarsSubscriptions(options.Messaging.Subscriptions, options.RoutedExchangeType);
                endpoint.ConfigureConsumers(context);

                options.ConfigureEndpoint?.Invoke(endpoint);
            });

        options.ConfigureBus?.Invoke(cfg);
    }
}
