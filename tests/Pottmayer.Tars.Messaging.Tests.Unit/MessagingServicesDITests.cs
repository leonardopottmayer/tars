using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.DI;

namespace Pottmayer.Tars.Messaging.Tests.Unit;

public class MessagingServicesDITests
{
    [Fact]
    public void AddTarsInProcessIntegrationEventBus_registers_the_bus_as_a_singleton()
    {
        var services = new ServiceCollection();

        services.AddTarsInProcessIntegrationEventBus();

        var descriptor = services.Single(d => d.ServiceType == typeof(IIntegrationEventBus));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
        descriptor.ImplementationType.Should().Be<InProcessIntegrationEventBus>();
    }

    [Fact]
    public void AddIntegrationEventHandlersFromAssembly_registers_every_handler_against_its_closed_interface()
    {
        var services = new ServiceCollection();

        services.AddIntegrationEventHandlersFromAssembly(typeof(FirstHandler).Assembly);

        var implementations = services
            .Where(d => d.ServiceType == typeof(IIntegrationEventHandler<TestEvent>))
            .Select(d => d.ImplementationType)
            .ToList();

        implementations.Should().Contain([typeof(FirstHandler), typeof(SecondHandler), typeof(ThrowingHandler)]);
    }

    [Fact]
    public void AddIntegrationEventHandlersFromAssembly_uses_scoped_lifetime_by_default()
    {
        var services = new ServiceCollection();

        services.AddIntegrationEventHandlersFromAssembly(typeof(FirstHandler).Assembly);

        services
            .Where(d => d.ServiceType == typeof(IIntegrationEventHandler<TestEvent>))
            .Should().OnlyContain(d => d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddIntegrationEventHandlersFromAssembly_honours_the_requested_lifetime()
    {
        var services = new ServiceCollection();

        services.AddIntegrationEventHandlersFromAssembly(typeof(FirstHandler).Assembly, ServiceLifetime.Singleton);

        services
            .Where(d => d.ServiceType == typeof(IIntegrationEventHandler<TestEvent>))
            .Should().OnlyContain(d => d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddTarsInProcessIntegrationEventBus_scans_the_assemblies_passed_through_options()
    {
        var services = new ServiceCollection();

        services.AddTarsInProcessIntegrationEventBus(o =>
            o.RegisterHandlersFromAssembly(typeof(FirstHandler).Assembly));

        services.Any(d => d.ServiceType == typeof(IIntegrationEventHandler<TestEvent>)).Should().BeTrue();
    }
}
