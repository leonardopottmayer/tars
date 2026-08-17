using System.Reflection;
using Confluent.Kafka;
using MassTransit;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker.Routing;

namespace Pottmayer.Tars.Messaging.MassTransit.Kafka.Topology;

/// <summary>
/// The topology steps, one concern per method. Each is usable on its own, so an application that
/// needs a rider configuration this package does not express can write its own <c>AddRider</c> block
/// and still get the Tars topics, producers and relay consumers.
/// </summary>
public static class KafkaTopology
{
    /// <summary>
    /// Binds one producer per event type, on a topic named after the event's logical name.
    /// </summary>
    /// <remarks>
    /// The topic is fixed here, at registration. MassTransit exposes no dynamic-topic producer, which
    /// is the whole reason a per-message routing key cannot become a topic on Kafka — and therefore
    /// why this provider only supports broadcast.
    /// </remarks>
    public static IRiderRegistrationConfigurator AddTarsProducers(
        this IRiderRegistrationConfigurator rider, IEnumerable<Type> eventTypes)
    {
        ArgumentNullException.ThrowIfNull(rider);
        ArgumentNullException.ThrowIfNull(eventTypes);

        foreach (var eventType in eventTypes)
            rider.AddTarsProducer(eventType);

        return rider;
    }

    /// <inheritdoc cref="AddTarsProducers(IRiderRegistrationConfigurator, IEnumerable{Type})"/>
    public static IRiderRegistrationConfigurator AddTarsProducer(
        this IRiderRegistrationConfigurator rider, Type eventType)
    {
        ArgumentNullException.ThrowIfNull(rider);
        ArgumentNullException.ThrowIfNull(eventType);

        var topic = IntegrationEventNaming.For(eventType);

        // AddProducer is generic; the event type is only known at runtime, so the closed method is
        // bound once, at startup.
        var method = typeof(KafkaProducerRegistrationExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(KafkaProducerRegistrationExtensions.AddProducer)
                && m.GetGenericArguments().Length == 1
                && m.GetParameters() is [_, { ParameterType.Name: nameof(String) }, _]);

        method.MakeGenericMethod(eventType).Invoke(null, [rider, topic, null]);

        return rider;
    }

    /// <summary>Registers one relay consumer per subscribed event type.</summary>
    public static IRiderRegistrationConfigurator AddTarsRelayConsumers(
        this IRiderRegistrationConfigurator rider,
        IEnumerable<IntegrationEventSubscription> subscriptions)
    {
        ArgumentNullException.ThrowIfNull(rider);
        ArgumentNullException.ThrowIfNull(subscriptions);

        foreach (var subscription in subscriptions)
            rider.AddConsumer(typeof(IntegrationEventRelayConsumer<>).MakeGenericType(subscription.EventType));

        return rider;
    }

    /// <summary>
    /// Attaches a topic endpoint per subscription, all under the same consumer group.
    /// </summary>
    /// <remarks>
    /// The consumer group is the portable identity: instances sharing it compete for partitions,
    /// different values each get a full copy — the same meaning <c>EndpointName</c> has as a queue on
    /// RabbitMQ.
    /// </remarks>
    public static IKafkaFactoryConfigurator UseTarsTopicEndpoints(
        this IKafkaFactoryConfigurator kafka,
        IRiderRegistrationContext context,
        IEnumerable<IntegrationEventSubscription> subscriptions,
        string consumerGroup,
        AutoOffsetReset autoOffsetReset,
        ushort concurrentMessageLimit)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);

        foreach (var subscription in subscriptions)
        {
            kafka.UseTarsTopicEndpoint(
                context, subscription, consumerGroup, autoOffsetReset, concurrentMessageLimit);
        }

        return kafka;
    }

    /// <inheritdoc cref="UseTarsTopicEndpoints(IKafkaFactoryConfigurator, IRiderRegistrationContext, IEnumerable{IntegrationEventSubscription}, string, AutoOffsetReset, ushort)"/>
    public static IKafkaFactoryConfigurator UseTarsTopicEndpoint(
        this IKafkaFactoryConfigurator kafka,
        IRiderRegistrationContext context,
        IntegrationEventSubscription subscription,
        string consumerGroup,
        AutoOffsetReset autoOffsetReset,
        ushort concurrentMessageLimit)
    {
        ArgumentNullException.ThrowIfNull(kafka);
        ArgumentNullException.ThrowIfNull(subscription);

        typeof(KafkaTopology)
            .GetMethod(nameof(ConfigureTopicEndpoint), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(subscription.EventType)
            .Invoke(null, [kafka, context, subscription.Destination, consumerGroup, autoOffsetReset, concurrentMessageLimit]);

        return kafka;
    }

    private static void ConfigureTopicEndpoint<TIntegrationEvent>(
        IKafkaFactoryConfigurator kafka,
        IRiderRegistrationContext context,
        string topic,
        string consumerGroup,
        AutoOffsetReset autoOffsetReset,
        ushort concurrentMessageLimit)
        where TIntegrationEvent : class, IIntegrationEvent
    {
        kafka.TopicEndpoint<TIntegrationEvent>(topic, consumerGroup, endpoint =>
        {
            endpoint.AutoOffsetReset = autoOffsetReset;
            endpoint.ConcurrentMessageLimit = concurrentMessageLimit;
            endpoint.ConfigureConsumer<IntegrationEventRelayConsumer<TIntegrationEvent>>(context);
        });
    }
}
