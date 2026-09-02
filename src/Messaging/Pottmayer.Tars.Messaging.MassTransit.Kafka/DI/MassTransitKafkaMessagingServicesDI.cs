using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker;
using Pottmayer.Tars.Messaging.Broker.DI;
using Pottmayer.Tars.Messaging.MassTransit.Kafka.Options;
using Pottmayer.Tars.Messaging.MassTransit.Kafka.Topology;

namespace Pottmayer.Tars.Messaging.MassTransit.Kafka.DI;

/// <summary>
/// Service collection extensions for configuring the MassTransit Kafka messaging provider.
/// </summary>
public static class MassTransitKafkaMessagingServicesDI
{
    /// <summary>
    /// Registers the Kafka-backed <see cref="IIntegrationEventBus"/>, scoped.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The updated service collection.</returns>
    /// <remarks>
    /// <para>
    /// A separate bus from the RabbitMQ one on purpose: Kafka in MassTransit is a rider, not a
    /// transport, so there is no <c>IPublishEndpoint</c> on this path — only an
    /// <c>ITopicProducer</c> per event type.
    /// </para>
    /// <para>
    /// Scoped for the same reason as the RabbitMQ bus: <c>ITopicProducer&lt;T&gt;</c> is scoped, and
    /// that is where the outbox substitutes its own. A singleton bus would capture the root provider
    /// and publish straight past the outbox.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddTarsKafkaIntegrationEventBus(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IIntegrationEventBus, KafkaIntegrationEventBus>();
        return services;
    }

    /// <summary>
    /// Composes the whole Kafka provider: the shared broker core, the Kafka bus, and the rider with
    /// its producers and topic endpoints.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Delegate to configure options.</param>
    /// <returns>The updated service collection.</returns>
    /// <remarks>
    /// <para>
    /// This is a composition of the smaller methods, nothing more — every step below is public and
    /// callable on its own. An application that needs a rider configuration this does not express can
    /// write its own <c>AddRider</c> block and reuse the same <see cref="KafkaTopology"/> steps.
    /// </para>
    /// <para>
    /// Producers and handlers are identical to the RabbitMQ setup. Two things differ underneath:
    /// a topic is bound per event type at startup, so every registered event gets a producer; and a
    /// rider needs a bus to attach to, so an in-memory one is stood up as a host and carries no
    /// traffic.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddTarsMassTransitKafka(
        this IServiceCollection services,
        Action<MassTransitKafkaMessagingOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new MassTransitKafkaMessagingOptions();
        configure(options);

        return services.AddTarsMassTransitKafka(options);
    }

    /// <summary>
    /// Reads connection settings from configuration (default section <c>Tars:Messaging:Kafka</c>),
    /// applies <paramref name="configure"/> on top, and registers the provider. This is the overload
    /// most applications want: bootstrap servers differ per environment, subscriptions do not.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="sectionName">Optional custom section name.</param>
    /// <param name="configure">Optional delegate to configure options.</param>
    /// <returns>The host application builder.</returns>
    public static IHostApplicationBuilder AddTarsMassTransitKafka(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<MassTransitKafkaMessagingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddTarsMassTransitKafka(
            builder.BuildTarsKafkaOptions(sectionName, configure));

        return builder;
    }

    private static IServiceCollection AddTarsMassTransitKafka(
        this IServiceCollection services, MassTransitKafkaMessagingOptions options)
    {
        if (!options.IsValid())
            throw new InvalidOperationException(MassTransitKafkaMessagingOptions.ValidationErrorMessage);

        // Kafka cannot filter on a per-message routing key, so a routed subscription is rejected here
        // rather than silently turned into "everyone reads everything and discards".
        options.Messaging.ValidateAgainst(BrokerCapabilities.Log, "Kafka");

        services.AddTarsBrokerCoreFor(options);
        services.AddTarsKafkaIntegrationEventBus();
        services.AddTarsKafkaBus(options);

        return services;
    }

    /// <summary>
    /// Registers the transport-agnostic pieces the provider needs, from what the options declared.
    /// </summary>
    private static IServiceCollection AddTarsBrokerCoreFor(
        this IServiceCollection services, MassTransitKafkaMessagingOptions options)
    {
        services.AddTarsIntegrationEventTypeRegistry(options.Messaging.DiscoverEventTypes());
        services.AddTarsIntegrationEventRouter();
        services.AddTarsIntegrationEventDispatcher();

        foreach (var (assembly, lifetime) in options.Messaging.HandlerAssemblies)
            services.AddTarsIntegrationEventHandlers(assembly, lifetime);

        return services;
    }

    /// <summary>
    /// Registers MassTransit itself: the rider with its producers, relay consumers and topic
    /// endpoints, plus the in-memory bus that hosts the rider.
    /// </summary>
    private static IServiceCollection AddTarsKafkaBus(
        this IServiceCollection services, MassTransitKafkaMessagingOptions options)
    {
        var eventTypes = options.Messaging.DiscoverEventTypes().ToArray();
        var subscriptions = options.Messaging.Subscriptions;
        var consumerGroup = options.ConsumerGroup ?? options.Messaging.EndpointName;

        services.AddMassTransit(bus =>
        {
            options.ConfigureRegistration?.Invoke(bus);

            bus.AddRider(rider =>
            {
                rider.AddTarsProducers(eventTypes);
                rider.AddTarsRelayConsumers(subscriptions);

                rider.UsingKafka((context, kafka) =>
                {
                    kafka.Host(options.BootstrapServers);
                    kafka.UseTarsTopicEndpoints(
                        context, subscriptions, consumerGroup,
                        options.AutoOffsetReset, options.ConcurrentMessageLimit);

                    options.ConfigureKafka?.Invoke(kafka);
                });
            });

            // A rider needs a bus to hang off. With Kafka as the only broker this one is a host and
            // nothing else — no endpoints, no traffic.
            bus.UsingInMemory((_, cfg) => options.ConfigureHostBus?.Invoke(cfg));
        });

        return services;
    }
}
