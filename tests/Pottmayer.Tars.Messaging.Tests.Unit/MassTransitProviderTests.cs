using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker.Dispatch;
using Pottmayer.Tars.Messaging.Broker.Registry;
using Pottmayer.Tars.Messaging.Broker.Routing;
using Pottmayer.Tars.Messaging.MassTransit;
using Pottmayer.Tars.Messaging.MassTransit.Kafka;
using Pottmayer.Tars.Messaging.MassTransit.Kafka.DI;
using Pottmayer.Tars.Messaging.MassTransit.RabbitMq;
using Pottmayer.Tars.Messaging.MassTransit.RabbitMq.DI;

namespace Pottmayer.Tars.Messaging.Tests.Unit;

public class MassTransitRabbitMqProviderTests
{
    private static ServiceProvider Build(
        Action<Broker.Options.BrokerMessagingOptions>? messaging = null, bool validateScopes = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTarsMassTransitRabbitMq(options =>
        {
            options.Host = "rabbit.tars.local";
            options.Messaging.EndpointName = "tars-tests";
            messaging?.Invoke(options.Messaging);
        });

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = validateScopes });
    }

    [Fact]
    public async Task Resolves_from_a_scope_with_scope_validation_on()
    {
        // Regression: the bus was a singleton depending on MassTransit's scoped IPublishEndpoint, so
        // it could not be resolved at all under validation — and with validation off it captured the
        // root endpoint and published straight past the EF outbox, which is the one failure the
        // outbox exists to prevent.
        await using var provider = Build(validateScopes: true);
        await using var scope = provider.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>()
            .Should().BeOfType<MassTransitIntegrationEventBus>();
    }

    [Fact]
    public async Task Shares_the_scope_with_the_publish_endpoint_the_outbox_substitutes()
    {
        await using var provider = Build(validateScopes: true);
        await using var scope = provider.CreateAsyncScope();

        // Same scope, so an outbox-aware IPublishEndpoint registered for this scope is the one the
        // bus publishes through.
        scope.ServiceProvider.GetRequiredService<IPublishEndpoint>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>().Should().NotBeNull();
    }

    [Fact]
    public async Task Registers_the_bus_behind_the_portable_contract()
    {
        await using var provider = Build();

        provider.GetRequiredService<IIntegrationEventBus>()
            .Should().BeOfType<MassTransitIntegrationEventBus>();
    }

    [Fact]
    public async Task Registers_the_shared_broker_core()
    {
        await using var provider = Build();

        provider.GetRequiredService<IIntegrationEventRouter>().Should().BeOfType<DefaultIntegrationEventRouter>();
        provider.GetRequiredService<IIntegrationEventDispatcher>().Should().BeOfType<ScopedIntegrationEventDispatcher>();
        provider.GetRequiredService<IIntegrationEventTypeRegistry>().Should().NotBeNull();
    }

    [Fact]
    public async Task Registers_the_rabbitmq_route_applier_so_the_routing_key_reaches_the_send_context()
    {
        await using var provider = Build();

        provider.GetServices<IPublishRouteApplier>().Should().ContainSingle()
            .Which.Should().BeOfType<RabbitMqPublishRouteApplier>();
    }

    [Fact]
    public async Task Accepts_a_wildcard_subscription_because_rabbitmq_can_honour_it()
    {
        await using var provider = Build(m => m.Subscribe<InboundInteractionReceived>("agenda.#"));

        provider.GetRequiredService<IIntegrationEventBus>().Should().NotBeNull();
    }

    [Fact]
    public void AddTarsRabbitMqIntegrationEventBus_registers_the_bus_on_its_own()
    {
        var services = new ServiceCollection();

        services.AddTarsRabbitMqIntegrationEventBus();

        var descriptor = services.Single(d => d.ServiceType == typeof(IIntegrationEventBus));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
        descriptor.ImplementationType.Should().Be<MassTransitIntegrationEventBus>();
    }

    [Fact]
    public void AddTarsRabbitMqRouteApplier_registers_the_applier_on_its_own()
    {
        var services = new ServiceCollection();

        services.AddTarsRabbitMqRouteApplier();

        services.Single(d => d.ServiceType == typeof(IPublishRouteApplier))
            .ImplementationType.Should().Be<RabbitMqPublishRouteApplier>();
    }

    [Fact]
    public void AddTarsRabbitMqRouteApplier_does_not_duplicate_on_a_second_call()
    {
        var services = new ServiceCollection();

        services.AddTarsRabbitMqRouteApplier();
        services.AddTarsRabbitMqRouteApplier();

        services.Count(d => d.ServiceType == typeof(IPublishRouteApplier)).Should().Be(1);
    }

    [Fact]
    public async Task A_subscribed_event_reaches_the_registry_even_without_an_assembly_scan()
    {
        await using var provider = Build(m => m.Subscribe<InboundInteractionReceived>("agenda.#"));

        var registry = provider.GetRequiredService<IIntegrationEventTypeRegistry>();

        registry.TryResolve("inbound.interaction", out var type).Should().BeTrue();
        type.Should().Be<InboundInteractionReceived>();
    }
}

public class MassTransitKafkaProviderTests
{
    private static ServiceProvider Build(
        Action<Broker.Options.BrokerMessagingOptions>? messaging = null, bool validateScopes = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTarsMassTransitKafka(options =>
        {
            options.BootstrapServers = "kafka.tars.local:9092";
            options.Messaging.EndpointName = "tars-tests";
            messaging?.Invoke(options.Messaging);
        });

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = validateScopes });
    }

    [Fact]
    public async Task Resolves_from_a_scope_with_scope_validation_on()
    {
        // Regression: as a singleton the bus captured the root IServiceProvider, so the scoped
        // ITopicProducer<T> it publishes through could not be resolved under validation — and
        // without validation it came from the root, bypassing the outbox. Scoped, the bus sees the
        // same provider the outbox registered its substitute in.
        await using var provider = Build(m => m.Subscribe<PasswordResetRequested>(), validateScopes: true);
        await using var scope = provider.CreateAsyncScope();

        scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>()
            .Should().BeOfType<KafkaIntegrationEventBus>();
    }

    [Fact]
    public async Task Registers_its_own_bus_because_the_rider_has_no_publish_endpoint()
    {
        await using var provider = Build();

        provider.GetRequiredService<IIntegrationEventBus>()
            .Should().BeOfType<KafkaIntegrationEventBus>();
    }

    [Fact]
    public void AddTarsKafkaIntegrationEventBus_registers_the_bus_on_its_own()
    {
        var services = new ServiceCollection();

        services.AddTarsKafkaIntegrationEventBus();

        var descriptor = services.Single(d => d.ServiceType == typeof(IIntegrationEventBus));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
        descriptor.ImplementationType.Should().Be<KafkaIntegrationEventBus>();
    }

    [Fact]
    public async Task Registers_the_same_shared_broker_core_as_rabbitmq()
    {
        await using var provider = Build();

        provider.GetRequiredService<IIntegrationEventRouter>().Should().BeOfType<DefaultIntegrationEventRouter>();
        provider.GetRequiredService<IIntegrationEventDispatcher>().Should().BeOfType<ScopedIntegrationEventDispatcher>();
    }

    [Fact]
    public async Task Accepts_a_broadcast_subscription()
    {
        await using var provider = Build(m => m.Subscribe<PasswordResetRequested>());

        provider.GetRequiredService<IIntegrationEventBus>().Should().NotBeNull();
    }

    [Fact]
    public void Rejects_a_routed_subscription_at_startup_instead_of_degrading_it_silently()
    {
        // The guard that matters: a Kafka topic is fixed per event type at registration, so a
        // per-message routing key cannot filter delivery. Accepting this subscription would turn
        // "only the owner wakes up" into "everyone reads and discards", hidden behind the abstraction.
        var act = () => Build(m => m.Subscribe<InboundInteractionReceived>("agenda.#"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*wildcard routing*Kafka does not support*");
    }
}

public class KafkaIntegrationEventBusTests
{
    [Fact]
    public void Names_the_routing_key_header_under_the_framework_prefix()
        => KafkaIntegrationEventBus.RoutingKeyHeader.Should().Be("tars.routing-key");

    [Fact]
    public async Task Explains_itself_when_no_producer_is_bound_for_the_event()
    {
        // Kafka binds a producer per type at startup, so an unregistered event is a registration
        // bug, not a runtime routing miss — the message should say which knob to turn.
        var services = new ServiceCollection().BuildServiceProvider();
        var bus = new KafkaIntegrationEventBus(services, new DefaultIntegrationEventRouter());

        var act = () => bus.PublishAsync(new PasswordResetRequested(Guid.NewGuid(), DateTimeOffset.UtcNow));

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*RegisterEventsFromAssembly*");
    }
}
