using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker.Composite;
using Pottmayer.Tars.Messaging.Broker.Dispatch;
using Pottmayer.Tars.Messaging.Broker.Options;
using Pottmayer.Tars.Messaging.Broker.Registry;
using Pottmayer.Tars.Messaging.Broker.Routing;
using Pottmayer.Tars.Messaging.MassTransit;
using Pottmayer.Tars.Messaging.MassTransit.Composition;
using Pottmayer.Tars.Messaging.MassTransit.DI;
using Pottmayer.Tars.Messaging.MassTransit.RabbitMq.Composition;
using Pottmayer.Tars.Messaging.MassTransit.Kafka;
using Pottmayer.Tars.Messaging.MassTransit.Kafka.Composition;

namespace Pottmayer.Tars.Messaging.Tests.Unit;

// Fixtures for the dual-transport scenario: an event on the backbone, a command for a worker fleet.

[IntegrationEventName("orders.payment-processed.v1")]
public sealed record PaymentProcessed(Guid EventId, DateTimeOffset OccurredAt, string OrderId) : IIntegrationEvent;

[IntegrationEventName("orders.ship-order.v1")]
public sealed record ShipOrderCommand(Guid EventId, DateTimeOffset OccurredAt, string OrderId) : IIntegrationEvent;

[IntegrationEventName("orders.order-placed.v1")]
public sealed record OrderPlaced(Guid EventId, DateTimeOffset OccurredAt, string OrderId) : IIntegrationEvent;

[IntegrationEventName("orders.audit-recorded.v1")]
public sealed record AuditRecorded(Guid EventId, DateTimeOffset OccurredAt, string OrderId) : IIntegrationEvent;

// Marker interfaces for MassTransit multibus: each additional non-rider transport is its own bus.
public interface IAuditBus : IBus { }

public interface IArchiveBus : IBus { }

public class EventTransportMapTests
{
    private static EventTransportMap Map(Action<EventTransportMapConfiguration> configure)
    {
        var options = new EventTransportMapConfiguration();
        configure(options);
        return new EventTransportMap(options);
    }

    [Fact]
    public void An_unlisted_event_takes_the_default_transport()
    {
        var map = Map(o => o.DefaultTransport = "kafka");

        map.SelectFor(typeof(PaymentProcessed)).Should().Equal("kafka");
    }

    [Fact]
    public void An_override_wins_over_the_default()
    {
        var map = Map(o =>
        {
            o.DefaultTransport = "kafka";
            o.Route<ShipOrderCommand>("rabbitmq");
        });

        map.SelectFor(typeof(ShipOrderCommand)).Should().Equal("rabbitmq");
        map.SelectFor(typeof(PaymentProcessed)).Should().Equal("kafka");
    }

    [Fact]
    public void An_event_can_fan_out_to_several_transports()
    {
        var map = Map(o =>
        {
            o.DefaultTransport = "kafka";
            o.Route<ShipOrderCommand>("rabbitmq", "kafka");
        });

        map.SelectFor(typeof(ShipOrderCommand)).Should().BeEquivalentTo(["rabbitmq", "kafka"]);
    }

    [Fact]
    public void Repeated_transports_in_one_route_are_deduplicated()
    {
        var map = Map(o =>
        {
            o.DefaultTransport = "kafka";
            o.Route<ShipOrderCommand>("rabbitmq", "rabbitmq");
        });

        map.SelectFor(typeof(ShipOrderCommand)).Should().Equal("rabbitmq");
    }

    [Fact]
    public void A_route_with_no_transports_is_rejected()
    {
        var act = () => new EventTransportMapConfiguration().Route(typeof(ShipOrderCommand));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SelectFor_accepts_an_event_instance()
    {
        var map = Map(o =>
        {
            o.DefaultTransport = "kafka";
            o.Route<ShipOrderCommand>("rabbitmq");
        });

        IEventTransportSelector selector = map;

        selector.SelectFor(new ShipOrderCommand(Guid.NewGuid(), DateTimeOffset.UtcNow, "o-1"))
            .Should().Equal("rabbitmq");
    }

    [Fact]
    public void A_map_without_a_default_fails_loudly()
    {
        var act = () => new EventTransportMap(new EventTransportMapConfiguration());

        act.Should().Throw<InvalidOperationException>().WithMessage("*needs a DefaultTransport*");
    }

    [Fact]
    public void ConfiguredTransports_lists_the_default_and_every_override_target_once()
    {
        var map = Map(o =>
        {
            o.DefaultTransport = "kafka";
            o.Route<ShipOrderCommand>("rabbitmq");
            o.Route<OrderPlaced>("rabbitmq");
        });

        map.ConfiguredTransports.Should().BeEquivalentTo(["kafka", "rabbitmq"]);
    }

    [Fact]
    public void Redefining_an_events_route_to_a_different_set_is_rejected()
    {
        var act = () => Map(o =>
        {
            o.DefaultTransport = "kafka";
            o.Route<ShipOrderCommand>("rabbitmq");
            o.Route<ShipOrderCommand>("kafka");
        });

        act.Should().Throw<InvalidOperationException>().WithMessage("*already routed to*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_transport_key_is_rejected(string transport)
    {
        var act = () => new EventTransportMapConfiguration().Route(typeof(ShipOrderCommand), transport);

        act.Should().Throw<ArgumentException>();
    }
}

public class CompositeIntegrationEventBusTests
{
    private sealed class RecordingBus : IIntegrationEventBus
    {
        public List<IIntegrationEvent> Published { get; } = [];

        public Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
        {
            Published.Add(@event);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingBus : IIntegrationEventBus
    {
        public Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("broker down");
    }

    private static (CompositeIntegrationEventBus Bus, RecordingBus Rabbit, RecordingBus Kafka) Build(
        EventTransportMap map)
    {
        var rabbit = new RecordingBus();
        var kafka = new RecordingBus();

        var provider = new ServiceCollection()
            .AddKeyedSingleton<IIntegrationEventBus>("rabbitmq", rabbit)
            .AddKeyedSingleton<IIntegrationEventBus>("kafka", kafka)
            .BuildServiceProvider();

        return (new CompositeIntegrationEventBus(map, provider), rabbit, kafka);
    }

    private static EventTransportMap Map(Action<EventTransportMapConfiguration> configure)
    {
        var options = new EventTransportMapConfiguration();
        configure(options);
        return new EventTransportMap(options);
    }

    [Fact]
    public async Task Routes_each_event_to_the_bus_its_transport_maps_to()
    {
        var (bus, rabbit, kafka) = Build(Map(o =>
        {
            o.DefaultTransport = "kafka";
            o.Route<ShipOrderCommand>("rabbitmq");
        }));

        await bus.PublishAsync(new PaymentProcessed(Guid.NewGuid(), DateTimeOffset.UtcNow, "o-1"));
        await bus.PublishAsync(new ShipOrderCommand(Guid.NewGuid(), DateTimeOffset.UtcNow, "o-1"));

        kafka.Published.Should().ContainSingle().Which.Should().BeOfType<PaymentProcessed>();
        rabbit.Published.Should().ContainSingle().Which.Should().BeOfType<ShipOrderCommand>();
    }

    [Fact]
    public async Task Explains_itself_when_the_mapped_transport_has_no_registered_bus()
    {
        // The map says "sqs", but only rabbitmq and kafka buses exist. This is a wiring bug, and the
        // message points at the two knobs that could be wrong.
        var (bus, _, _) = Build(Map(o =>
        {
            o.DefaultTransport = "kafka";
            o.Route<ShipOrderCommand>("sqs");
        }));

        var act = () => bus.PublishAsync(new ShipOrderCommand(Guid.NewGuid(), DateTimeOffset.UtcNow, "o-1"));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*routed to transport 'sqs'*no keyed IIntegrationEventBus*");
    }

    [Fact]
    public async Task Rejects_a_null_event()
    {
        var (bus, _, _) = Build(Map(o => o.DefaultTransport = "kafka"));

        var act = () => bus.PublishAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Fans_a_single_publish_out_to_every_mapped_bus()
    {
        var (bus, rabbit, kafka) = Build(Map(o =>
        {
            o.DefaultTransport = "rabbitmq";
            o.Route<PaymentProcessed>("rabbitmq", "kafka");
        }));

        // One call by the producer; the composite hits both buses.
        await bus.PublishAsync(new PaymentProcessed(Guid.NewGuid(), DateTimeOffset.UtcNow, "o-1"));

        rabbit.Published.Should().ContainSingle();
        kafka.Published.Should().ContainSingle();
    }

    [Fact]
    public async Task A_fan_out_attempts_every_transport_and_aggregates_failures()
    {
        var healthy = new RecordingBus();

        var provider = new ServiceCollection()
            .AddKeyedSingleton<IIntegrationEventBus>("ok", healthy)
            .AddKeyedSingleton<IIntegrationEventBus>("bad", new ThrowingBus())
            .BuildServiceProvider();

        var map = Map(o =>
        {
            o.DefaultTransport = "ok";
            o.Route<PaymentProcessed>("ok", "bad");
        });
        var bus = new CompositeIntegrationEventBus(map, provider);

        var act = () => bus.PublishAsync(new PaymentProcessed(Guid.NewGuid(), DateTimeOffset.UtcNow, "o-1"));

        // The bad transport throws, but the healthy one still received the message.
        (await act.Should().ThrowAsync<AggregateException>())
            .Which.InnerExceptions.Should().ContainSingle();
        healthy.Published.Should().ContainSingle();
    }
}

public class MassTransitMultiProviderTests
{
    private static IServiceCollection Configure(Action<MassTransitCompositeConfiguration> extra)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTarsMassTransitComposite(options =>
        {
            options.AddRabbitMq("rabbitmq", r =>
            {
                r.Host = "rabbit.tars.local";
                r.Messaging.EndpointName = "multi-tests";
                r.Messaging.RegisterEventsFromAssembly(typeof(PaymentProcessed).Assembly);
            });

            options.AddKafka("kafka", k =>
            {
                k.BootstrapServers = "kafka.tars.local:9092";
                k.Messaging.EndpointName = "multi-tests";
                k.Messaging.RegisterEventsFromAssembly(typeof(PaymentProcessed).Assembly);
            });

            extra(options);
        });
        return services;
    }

    private static ServiceDescriptor DefaultBus(IServiceCollection services)
        => services.Single(d => d.ServiceType == typeof(IIntegrationEventBus) && !d.IsKeyedService);

    private static ServiceDescriptor KeyedBus(IServiceCollection services, string key)
        => services.Single(d => d.ServiceType == typeof(IIntegrationEventBus)
            && d.IsKeyedService && (string?)d.ServiceKey == key);

    [Fact]
    public void The_default_bus_is_the_composite()
    {
        var services = Configure(o => o.DefaultTransport = "kafka");

        DefaultBus(services).ImplementationType.Should().Be<CompositeIntegrationEventBus>();
    }

    [Fact]
    public void Each_transport_is_registered_under_its_key()
    {
        var services = Configure(o => o.DefaultTransport = "kafka");

        KeyedBus(services, "rabbitmq").KeyedImplementationType.Should().Be<MassTransitIntegrationEventBus>();
        KeyedBus(services, "kafka").KeyedImplementationType.Should().Be<KafkaIntegrationEventBus>();
    }

    [Fact]
    public void Transports_can_use_arbitrary_keys()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTarsMassTransitComposite(o =>
        {
            o.AddKafka("events", k =>
            {
                k.BootstrapServers = "kafka.tars.local:9092";
                k.Messaging.EndpointName = "multi-tests";
                k.Messaging.RegisterEventsFromAssembly(typeof(PaymentProcessed).Assembly);
            });
            o.AddRabbitMq("commands", r =>
            {
                r.Host = "rabbit.tars.local";
                r.Messaging.EndpointName = "multi-tests";
                r.Messaging.RegisterEventsFromAssembly(typeof(PaymentProcessed).Assembly);
            });
            o.DefaultTransport = "events";
            o.Route<ShipOrderCommand>("commands");
        });

        KeyedBus(services, "events").KeyedImplementationType.Should().Be<KafkaIntegrationEventBus>();
        KeyedBus(services, "commands").KeyedImplementationType.Should().Be<MassTransitIntegrationEventBus>();
    }

    [Fact]
    public void A_single_transport_is_a_valid_configuration()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var act = () => services.AddTarsMassTransitComposite(o =>
        {
            o.AddKafka("kafka", k =>
            {
                k.BootstrapServers = "kafka.tars.local:9092";
                k.Messaging.EndpointName = "multi-tests";
                k.Messaging.RegisterEventsFromAssembly(typeof(PaymentProcessed).Assembly);
            });
            o.DefaultTransport = "kafka";
        });

        act.Should().NotThrow();
    }

    [Fact]
    public void Declaring_no_transport_fails_at_startup()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var act = () => services.AddTarsMassTransitComposite(o => o.DefaultTransport = "kafka");

        act.Should().Throw<InvalidOperationException>().WithMessage("*at least one transport*");
    }

    [Fact]
    public void A_second_primary_rabbitmq_without_a_marker_is_rejected_with_a_multibus_hint()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var act = () => services.AddTarsMassTransitComposite(o =>
        {
            o.AddRabbitMq("rabbit-a", r => { r.Host = "a.local"; r.Messaging.EndpointName = "a"; });
            o.AddRabbitMq("rabbit-b", r => { r.Host = "b.local"; r.Messaging.EndpointName = "b"; });
            o.DefaultTransport = "rabbit-a";
        });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*primary bus*AddRabbitMq<TBus>*multibus*");
    }

    [Fact]
    public void A_marked_secondary_rabbitmq_is_registered_as_its_own_keyed_bus()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTarsMassTransitComposite(o =>
        {
            o.AddKafka("events", k =>
            {
                k.BootstrapServers = "kafka.tars.local:9092";
                k.Messaging.EndpointName = "multi-tests";
                k.Messaging.RegisterEventsFromAssembly(typeof(PaymentProcessed).Assembly);
            });
            o.AddRabbitMq("commands", r =>
            {
                r.Host = "rabbit.tars.local";
                r.Messaging.EndpointName = "multi-tests";
                r.Messaging.RegisterEventsFromAssembly(typeof(PaymentProcessed).Assembly);
            });
            o.AddRabbitMq<IAuditBus>("audit", r =>
            {
                r.Host = "rabbit-audit.tars.local";
                r.Messaging.EndpointName = "audit";
                r.Messaging.RegisterEventsFromAssembly(typeof(PaymentProcessed).Assembly);
            });

            o.DefaultTransport = "events";
            o.Route<ShipOrderCommand>("commands");
            o.Route<AuditRecorded>("audit");
        });

        // Every transport — primary and the marked secondary — is a keyed IIntegrationEventBus.
        KeyedBus(services, "events").Should().NotBeNull();
        KeyedBus(services, "commands").Should().NotBeNull();
        KeyedBus(services, "audit").Should().NotBeNull();
    }

    [Fact]
    public void Several_marked_secondary_transports_are_allowed()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var act = () => services.AddTarsMassTransitComposite(o =>
        {
            o.AddRabbitMq("primary", r => { r.Host = "r0.local"; r.Messaging.EndpointName = "m"; });
            o.AddRabbitMq<IAuditBus>("audit", r => { r.Host = "r1.local"; r.Messaging.EndpointName = "m"; });
            o.AddRabbitMq<IArchiveBus>("archive", r => { r.Host = "r2.local"; r.Messaging.EndpointName = "m"; });
            o.DefaultTransport = "primary";
        });

        act.Should().NotThrow();
    }

    [Fact]
    public void The_selector_and_shared_core_are_registered_once()
    {
        var services = Configure(o => o.DefaultTransport = "kafka");

        services.Count(d => d.ServiceType == typeof(IEventTransportSelector)).Should().Be(1);
        services.Should().ContainSingle(d => d.ServiceType == typeof(IIntegrationEventDispatcher));
        services.Should().ContainSingle(d => d.ServiceType == typeof(IIntegrationEventRouter));
        services.Should().ContainSingle(d => d.ServiceType == typeof(IIntegrationEventTypeRegistry));
    }

    [Fact]
    public void An_override_to_an_unregistered_transport_fails_at_startup()
    {
        var act = () => Configure(o => o.DefaultTransport = "sqs");

        act.Should().Throw<InvalidOperationException>().WithMessage("*references 'sqs'*");
    }

    [Fact]
    public void Routing_an_event_to_a_transport_it_is_not_registered_on_fails_at_startup()
    {
        // Kafka side does NOT register the events assembly, so routing an event there has no producer.
        var services = new ServiceCollection();
        services.AddLogging();

        var act = () => services.AddTarsMassTransitComposite(options =>
        {
            options.AddRabbitMq("rabbitmq", r =>
            {
                r.Host = "rabbit.tars.local";
                r.Messaging.EndpointName = "multi-tests";
                r.Messaging.RegisterEventsFromAssembly(typeof(PaymentProcessed).Assembly);
            });

            options.AddKafka("kafka", k =>
            {
                k.BootstrapServers = "kafka.tars.local:9092";
                k.Messaging.EndpointName = "multi-tests";
                // Kafka registers nothing.
            });

            options.DefaultTransport = "rabbitmq";
            options.Route<PaymentProcessed>("kafka");
        });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*routed to 'kafka'*not registered as an event on that transport*");
    }

    [Fact]
    public void A_valid_multi_configuration_registers_cleanly()
    {
        var act = () => Configure(o =>
        {
            o.DefaultTransport = "kafka";
            o.Route<ShipOrderCommand>("rabbitmq");
        });

        act.Should().NotThrow();
    }

    [Fact]
    public void An_event_can_fan_out_across_transports_in_the_configuration()
    {
        var act = () => Configure(o =>
        {
            o.DefaultTransport = "kafka";
            o.Route<AuditRecorded>("kafka", "rabbitmq");
        });

        act.Should().NotThrow();
    }

    // Proves the seam is transport-agnostic: a transport the orchestrator has never heard of — no
    // RabbitMQ, no Kafka — composes as the primary bus purely through IMassTransitBusTransport. This is
    // exactly the shape a future .AmazonSqs provider plugs into, with no change to the orchestrator.
    [Fact]
    public void A_custom_transport_contributor_composes_as_the_primary_bus()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddTarsMassTransitComposite(c =>
        {
            c.AddTransport(new InMemoryPrimaryTransport("custom"));
            c.DefaultTransport = "custom";
        });

        DefaultBus(services).ImplementationType.Should().Be<CompositeIntegrationEventBus>();
        KeyedBus(services, "custom").Should().NotBeNull();
    }

    private sealed class InMemoryPrimaryTransport(string key) : IMassTransitBusTransport
    {
        public string Key => key;
        public string TransportName => "InMemory (test)";
        public BrokerMessagingOptions Messaging { get; } = new();
        public bool IsPrimaryCandidate => true;
        public void Validate() { }
        public void RegisterKeyedBus(IServiceCollection services)
            => services.AddKeyedSingleton<IIntegrationEventBus>(key, (_, _) => new StubBus());
        public void ConfigurePrimary(IBusRegistrationConfigurator bus) { }
        public void UsePrimaryBus(IBusRegistrationConfigurator bus) => bus.UsingInMemory();
        public void RegisterMultibus(IServiceCollection services) => throw new NotSupportedException();
    }

    private sealed class StubBus : IIntegrationEventBus
    {
        public Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
