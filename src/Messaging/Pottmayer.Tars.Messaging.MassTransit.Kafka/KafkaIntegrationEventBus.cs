using System.Collections.Concurrent;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker.Routing;

namespace Pottmayer.Tars.Messaging.MassTransit.Kafka;

/// <summary>
/// Publishes through MassTransit's Kafka rider. A separate bus from the RabbitMQ one because Kafka in
/// MassTransit is not a transport: there is no <c>IPublishEndpoint</c> on this path, only an
/// <see cref="ITopicProducer{T}"/> bound to one topic per event type.
/// </summary>
/// <remarks>
/// The routing key travels as a header and as the Kafka message key, so a consumer can still see it
/// and partitioning stays stable per key. It does <strong>not</strong> filter delivery: the topic is
/// fixed at registration, so every subscriber to the topic reads every message. That is why the
/// Kafka provider rejects a routed subscription at startup instead of quietly degrading it.
/// </remarks>
public sealed class KafkaIntegrationEventBus(
    IServiceProvider services,
    IIntegrationEventRouter router)
    : IIntegrationEventBus
{
    /// <summary>Header carrying the portable routing key, for consumers that want to read it.</summary>
    public const string RoutingKeyHeader = "tars.routing-key";

    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IIntegrationEvent, IntegrationEventRoute, CancellationToken, Task>> Producers = new();

    public Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var route = router.Resolve(@event);
        var produce = Producers.GetOrAdd(@event.GetType(), BuildProducer);

        return produce(services, @event, route, cancellationToken);
    }

    /// <summary>
    /// Binds the open generic <see cref="ProduceAsync{T}"/> to a runtime event type, once per type.
    /// </summary>
    private static Func<IServiceProvider, IIntegrationEvent, IntegrationEventRoute, CancellationToken, Task> BuildProducer(Type eventType)
    {
        var method = typeof(KafkaIntegrationEventBus)
            .GetMethod(nameof(ProduceAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(eventType);

        return method.CreateDelegate<Func<IServiceProvider, IIntegrationEvent, IntegrationEventRoute, CancellationToken, Task>>();
    }

    private static Task ProduceAsync<TIntegrationEvent>(
        IServiceProvider services,
        IIntegrationEvent @event,
        IntegrationEventRoute route,
        CancellationToken cancellationToken)
        where TIntegrationEvent : class, IIntegrationEvent
    {
        var producer = services.GetService<ITopicProducer<TIntegrationEvent>>()
            ?? throw new InvalidOperationException(
                $"No Kafka producer is registered for '{route.Destination}'. Register the assembly " +
                $"declaring {typeof(TIntegrationEvent).Name} with RegisterEventsFromAssembly, so a " +
                "producer is bound to its topic at startup — Kafka topics cannot be chosen per message.");

        var pipe = Pipe.Execute<KafkaSendContext<TIntegrationEvent>>(context =>
        {
            foreach (var (name, value) in route.Headers)
                context.Headers.Set(name, value);

            if (route.RoutingKey is not null)
                context.Headers.Set(RoutingKeyHeader, route.RoutingKey);
        });

        return producer.Produce((TIntegrationEvent)@event, pipe, cancellationToken);
    }
}
