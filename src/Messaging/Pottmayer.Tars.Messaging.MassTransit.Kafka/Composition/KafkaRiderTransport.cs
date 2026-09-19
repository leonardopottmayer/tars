using MassTransit;
using Pottmayer.Tars.Messaging.Broker;
using Pottmayer.Tars.Messaging.Broker.Options;
using Pottmayer.Tars.Messaging.MassTransit.Composition;
using Pottmayer.Tars.Messaging.MassTransit.Kafka.Options;
using Pottmayer.Tars.Messaging.MassTransit.Kafka.Topology;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Messaging.MassTransit.Kafka.DI;

namespace Pottmayer.Tars.Messaging.MassTransit.Kafka.Composition;

/// <summary>
/// A Kafka transport contributed to a composite MassTransit application. Kafka is a MassTransit
/// <em>rider</em> — it has no standalone bus, so it attaches to whichever full transport is the primary
/// bus (or to an in-memory host when it is the only transport).
/// </summary>
public sealed class KafkaRiderTransport(string key, MassTransitKafkaMessagingOptions options)
    : IMassTransitRiderTransport
{
    /// <inheritdoc />
    public string Key => key;

    /// <inheritdoc />
    public string TransportName => "Kafka";

    /// <inheritdoc />
    public BrokerMessagingOptions Messaging => options.Messaging;

    /// <inheritdoc />
    public void Validate()
    {
        if (!options.IsValid())
            throw new InvalidOperationException(MassTransitKafkaMessagingOptions.ValidationErrorMessage);

        options.Messaging.ValidateAgainst(BrokerCapabilities.Log, "Kafka");
    }

    /// <inheritdoc />
    public void RegisterKeyedBus(IServiceCollection services)
        => services.AddTarsKeyedKafkaIntegrationEventBus(key);

    /// <inheritdoc />
    public void AttachRider(IBusRegistrationConfigurator bus)
    {
        var subscriptions = options.Messaging.Subscriptions;
        var eventTypes = options.Messaging.DiscoverEventTypes().ToArray();
        var consumerGroup = options.ConsumerGroup ?? options.Messaging.EndpointName;

        // Registration-time configuration for this transport — e.g. the EF Core outbox's
        // AddEntityFrameworkOutbox + UseBusOutbox — is applied on the bus the rider attaches to, exactly
        // as the standalone Kafka provider does. Dropping it would leave the outbox configured but inert.
        options.ConfigureRegistration?.Invoke(bus);

        bus.AddRider(rider =>
        {
            rider.AddTarsProducers(eventTypes);
            rider.AddTarsRelayConsumers(subscriptions);

            rider.UsingKafka((context, cfg) =>
            {
                cfg.Host(options.BootstrapServers);
                cfg.UseTarsTopicEndpoints(
                    context, subscriptions, consumerGroup,
                    options.AutoOffsetReset, options.ConcurrentMessageLimit);

                options.ConfigureKafka?.Invoke(cfg);
            });
        });
    }

    /// <inheritdoc />
    public void ConfigureStandaloneHost(IInMemoryBusFactoryConfigurator configurator)
        => options.ConfigureHostBus?.Invoke(configurator);
}
