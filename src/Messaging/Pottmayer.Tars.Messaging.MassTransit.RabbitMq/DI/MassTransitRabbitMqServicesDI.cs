using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker;
using Pottmayer.Tars.Messaging.Broker.DI;
using Pottmayer.Tars.Messaging.MassTransit.RabbitMq.Options;
using Pottmayer.Tars.Messaging.MassTransit.RabbitMq.Topology;

namespace Pottmayer.Tars.Messaging.MassTransit.RabbitMq.DI;

/// <summary>
/// Service collection extensions for configuring the MassTransit RabbitMQ messaging provider.
/// </summary>
public static class MassTransitRabbitMqServicesDI
{
    /// <summary>
    /// Registers the MassTransit-backed <see cref="IIntegrationEventBus"/>, scoped.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The updated service collection.</returns>
    /// <remarks>
    /// Scoped because MassTransit's <see cref="IPublishEndpoint"/> is: the outbox works by handing
    /// the scope a publish endpoint that writes to the outbox tables instead of to the broker. A
    /// singleton bus would capture the root endpoint, which resolves under
    /// <c>ValidateScopes = false</c> and then publishes straight past the outbox — configured, and
    /// silently doing nothing.
    /// </remarks>
    public static IServiceCollection AddTarsRabbitMqIntegrationEventBus(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IIntegrationEventBus, MassTransitIntegrationEventBus>();
        return services;
    }

    /// <summary>
    /// Registers the applier that puts the portable routing key onto RabbitMQ's send context, which
    /// is what a topic or direct exchange matches bindings against.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsRabbitMqRouteApplier(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IPublishRouteApplier, RabbitMqPublishRouteApplier>());
        return services;
    }

    /// <summary>
    /// Composes the whole RabbitMQ provider: the shared broker core, the RabbitMQ-specific services,
    /// and the MassTransit bus with its topology.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Delegate to configure options.</param>
    /// <returns>The updated service collection.</returns>
    /// <remarks>
    /// <para>
    /// This is a composition of the smaller methods, nothing more — every step below is public and
    /// callable on its own. An application that needs a bus configuration this does not express can
    /// write its own <c>AddMassTransit</c> block and reuse the same
    /// <see cref="RabbitMqTopology"/> steps.
    /// </para>
    /// <para>
    /// Producers and handlers are unaware of any of it. Moving to another framework or broker is a
    /// change to this one call.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddTarsMassTransitRabbitMq(
        this IServiceCollection services,
        Action<MassTransitRabbitMqMessagingOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new MassTransitRabbitMqMessagingOptions();
        configure(options);

        return services.AddTarsMassTransitRabbitMq(options);
    }

    /// <summary>
    /// Reads connection settings from configuration (default section
    /// <c>Tars:Messaging:RabbitMq</c>), applies <paramref name="configure"/> on top, and registers
    /// the provider. This is the overload most applications want: host and credentials differ per
    /// environment, subscriptions do not.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="sectionName">Optional custom section name.</param>
    /// <param name="configure">Optional delegate to configure options.</param>
    /// <returns>The host application builder.</returns>
    public static IHostApplicationBuilder AddTarsMassTransitRabbitMq(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<MassTransitRabbitMqMessagingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddTarsMassTransitRabbitMq(
            builder.BuildTarsRabbitMqOptions(sectionName, configure));

        return builder;
    }

    private static IServiceCollection AddTarsMassTransitRabbitMq(
        this IServiceCollection services, MassTransitRabbitMqMessagingOptions options)
    {
        if (!options.IsValid())
            throw new InvalidOperationException(MassTransitRabbitMqMessagingOptions.ValidationErrorMessage);

        // Fail at startup rather than in production. RabbitMQ can honour every routing shape, so
        // anything rejected here is a bug in the subscription itself.
        options.Messaging.ValidateAgainst(BrokerCapabilities.Amqp, "RabbitMQ");

        services.AddTarsBrokerCoreFor(options);
        services.AddTarsRabbitMqRouteApplier();
        services.AddTarsRabbitMqIntegrationEventBus();
        services.AddTarsRabbitMqBus(options);

        return services;
    }

    /// <summary>
    /// Registers the transport-agnostic pieces the provider needs, from what the options declared.
    /// </summary>
    private static IServiceCollection AddTarsBrokerCoreFor(
        this IServiceCollection services, MassTransitRabbitMqMessagingOptions options)
    {
        services.AddTarsIntegrationEventTypeRegistry(options.Messaging.DiscoverEventTypes());
        services.AddTarsIntegrationEventRouter();
        services.AddTarsIntegrationEventDispatcher();

        foreach (var (assembly, lifetime) in options.Messaging.HandlerAssemblies)
            services.AddTarsIntegrationEventHandlers(assembly, lifetime);

        return services;
    }

    /// <summary>
    /// Registers MassTransit itself: the relay consumers, the host, the exchange topology and the
    /// receive endpoint.
    /// </summary>
    private static IServiceCollection AddTarsRabbitMqBus(
        this IServiceCollection services, MassTransitRabbitMqMessagingOptions options)
    {
        var subscriptions = options.Messaging.Subscriptions;

        // Every event this application knows about, not just the ones it subscribes to. A service
        // that only *publishes* a routed event still has to declare its exchange as topic/direct:
        // declaring the default fanout instead collides with the subscriber's declaration and
        // RabbitMQ answers PRECONDITION_FAILED, which MassTransit retries forever — the publish
        // never returns. This is what the Kafka provider already does for its producers.
        var eventTypes = options.Messaging.DiscoverEventTypes().ToArray();

        services.AddMassTransit(bus =>
        {
            options.ConfigureRegistration?.Invoke(bus);
            bus.AddTarsRelayConsumers(subscriptions);

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.UseTarsHost(
                    options.Host, options.Port, options.VirtualHost,
                    options.Username, options.Password, options.UseSsl);

                cfg.UseTarsEntityNaming();
                cfg.UseTarsPublishTopology(eventTypes, options.RoutedExchangeType);

                cfg.ReceiveEndpoint(
                    options.Messaging.EndpointName,
                    (IRabbitMqReceiveEndpointConfigurator endpoint) =>
                        endpoint.ConfigureTarsEndpoint(context, options));

                options.ConfigureBus?.Invoke(cfg);
            });
        });

        return services;
    }

    /// <summary>
    /// Configures the one receive endpoint: prefetch, retry, the subscription bindings and the
    /// consumers, then hands over to the caller's escape hatch.
    /// </summary>
    private static void ConfigureTarsEndpoint(
        this IRabbitMqReceiveEndpointConfigurator endpoint,
        IBusRegistrationContext context,
        MassTransitRabbitMqMessagingOptions options)
    {
        endpoint.PrefetchCount = options.PrefetchCount;
        endpoint.UseTarsRetry(options.RetryLimit, options.RetryInterval);
        endpoint.BindTarsSubscriptions(options.Messaging.Subscriptions, options.RoutedExchangeType);
        endpoint.ConfigureConsumers(context);

        options.ConfigureEndpoint?.Invoke(endpoint);
    }
}
