using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker.DI;
using Pottmayer.Tars.Messaging.Broker.Dispatch;
using Pottmayer.Tars.Messaging.Broker.Options;
using Pottmayer.Tars.Messaging.Broker.Registry;
using Pottmayer.Tars.Messaging.Broker.Routing;
using Pottmayer.Tars.Messaging.MassTransit.RabbitMq.DI;

namespace Pottmayer.Tars.Messaging.Tests.Unit;

public sealed class TenantPrefixedRouter : IIntegrationEventRouter
{
    public IntegrationEventRoute Resolve(IIntegrationEvent @event)
        => new($"acme.{IntegrationEventNaming.For(@event.GetType())}", null, new Dictionary<string, string>());
}

public sealed class NoopDispatcher : IIntegrationEventDispatcher
{
    public Task DispatchAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public class BrokerMessagingServicesDITests
{
    [Fact]
    public void AddTarsIntegrationEventRouter_registers_the_default_as_a_singleton()
    {
        var services = new ServiceCollection();

        services.AddTarsIntegrationEventRouter();

        var descriptor = services.Single(d => d.ServiceType == typeof(IIntegrationEventRouter));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
        descriptor.ImplementationType.Should().Be<DefaultIntegrationEventRouter>();
    }

    [Fact]
    public void AddTarsIntegrationEventRouter_accepts_a_replacement()
    {
        var services = new ServiceCollection();

        services.AddTarsIntegrationEventRouter<TenantPrefixedRouter>();

        services.Single(d => d.ServiceType == typeof(IIntegrationEventRouter))
            .ImplementationType.Should().Be<TenantPrefixedRouter>();
    }

    [Fact]
    public void AddTarsIntegrationEventDispatcher_registers_the_scoped_dispatcher_as_a_singleton()
    {
        var services = new ServiceCollection();

        services.AddTarsIntegrationEventDispatcher();

        var descriptor = services.Single(d => d.ServiceType == typeof(IIntegrationEventDispatcher));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
        descriptor.ImplementationType.Should().Be<ScopedIntegrationEventDispatcher>();
    }

    [Fact]
    public void AddTarsIntegrationEventDispatcher_accepts_a_replacement()
    {
        var services = new ServiceCollection();

        services.AddTarsIntegrationEventDispatcher<NoopDispatcher>();

        services.Single(d => d.ServiceType == typeof(IIntegrationEventDispatcher))
            .ImplementationType.Should().Be<NoopDispatcher>();
    }

    [Fact]
    public void AddTarsIntegrationEventTypeRegistry_builds_the_map_from_explicit_types()
    {
        var services = new ServiceCollection();
        services.AddTarsIntegrationEventTypeRegistry([typeof(PasswordResetRequested)]);

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IIntegrationEventTypeRegistry>();

        registry.TryResolve("identity.password-reset.v1", out var type).Should().BeTrue();
        type.Should().Be<PasswordResetRequested>();
    }

    [Fact]
    public void AddTarsIntegrationEventTypeRegistry_builds_the_map_from_an_assembly()
    {
        var services = new ServiceCollection();
        services.AddTarsIntegrationEventTypeRegistry(typeof(PasswordResetRequested).Assembly);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IIntegrationEventTypeRegistry>()
            .TryResolve("inbound.interaction", out _).Should().BeTrue();
    }

    [Fact]
    public void AddTarsIntegrationEventHandlers_registers_every_handler_for_an_event()
    {
        var services = new ServiceCollection();

        services.AddTarsIntegrationEventHandlers(typeof(BrokerMessagingServicesDITests).Assembly);

        // Several handlers may subscribe to one event; none of them may be swallowed.
        services.Count(d => d.ServiceType == typeof(IIntegrationEventHandler<MfaEnabled>))
            .Should().Be(2);
    }

    [Fact]
    public void AddTarsIntegrationEventHandlers_does_not_duplicate_on_a_second_call()
    {
        var services = new ServiceCollection();
        var assembly = typeof(BrokerMessagingServicesDITests).Assembly;

        services.AddTarsIntegrationEventHandlers(assembly);
        var afterFirst = services.Count(d => d.ServiceType == typeof(IIntegrationEventHandler<MfaEnabled>));
        services.AddTarsIntegrationEventHandlers(assembly);

        services.Count(d => d.ServiceType == typeof(IIntegrationEventHandler<MfaEnabled>))
            .Should().Be(afterFirst);
    }

    [Fact]
    public async Task A_router_registered_before_the_provider_wins()
    {
        // The seam the granular registration exists for: an application imposes its own routing
        // convention without forking a provider, because every registration is TryAdd.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTarsIntegrationEventRouter<TenantPrefixedRouter>();
        services.AddTarsMassTransitRabbitMq(o => o.Host = "rabbit.tars.local");

        await using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IIntegrationEventRouter>().Should().BeOfType<TenantPrefixedRouter>();
    }
}

public class BrokerMessagingOptionsDITests
{
    [Fact]
    public void Binds_the_endpoint_name_from_the_default_section()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tars:Messaging:Broker:EndpointName"] = "channels",
        });

        builder.AddTarsBrokerMessagingOptions();
        using var provider = builder.Services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<BrokerMessagingOptions>>().Value.EndpointName.Should().Be("channels");
    }

    [Fact]
    public void Binds_from_a_custom_section_when_one_is_given()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Bus:EndpointName"] = "agenda",
        });

        builder.AddTarsBrokerMessagingOptions(sectionName: "Bus");
        using var provider = builder.Services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<BrokerMessagingOptions>>().Value.EndpointName.Should().Be("agenda");
    }

    [Fact]
    public void Applies_the_configure_callback_over_bound_values()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tars:Messaging:Broker:EndpointName"] = "from-config",
        });

        builder.AddTarsBrokerMessagingOptions(configure: o => o.Subscribe<PasswordResetRequested>());
        using var provider = builder.Services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<BrokerMessagingOptions>>().Value;
        options.EndpointName.Should().Be("from-config");
        options.Subscriptions.Should().ContainSingle();
    }

    [Fact]
    public void Defaults_target_the_conventional_section()
    {
        BrokerMessagingOptions.SectionName.Should().Be("Tars:Messaging:Broker");
        new BrokerMessagingOptions().EndpointName.Should().Be("tars");
    }
}
