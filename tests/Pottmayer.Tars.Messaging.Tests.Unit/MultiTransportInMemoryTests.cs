using System.Collections.Concurrent;
using System.Diagnostics;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker.DI;
using Pottmayer.Tars.Messaging.Broker.Routing;
using Pottmayer.Tars.Messaging.MassTransit;

namespace Pottmayer.Tars.Messaging.Tests.Unit;

/// <summary>
/// End-to-end proof of the composite bus over two transports, on MassTransit's in-memory transport so
/// it runs in CI without Docker. Two separate buses stand in for RabbitMQ and Kafka: an "event plane"
/// and a "work plane". The test exercises the real Tars types the size scenario uses — the composite,
/// the transport map, the relay consumer, the dispatcher and real handlers — including the money shot:
/// a handler on the event plane publishes a command that the composite routes to the work plane.
/// </summary>
public class MultiTransportInMemoryTests
{
    // Marker interfaces make two distinct MassTransit buses in one process (multibus). Each is a real,
    // independent transport, so routing across them is a genuine cross-transport hop.
    public interface IEventPlaneBus : IBus { }

    public interface IWorkPlaneBus : IBus { }

    private const string EventPlane = "event-plane";
    private const string WorkPlane = "work-plane";

    // internal, so the assembly scan (GetExportedTypes) never picks these up in the DI tests; they are
    // registered explicitly here instead.
    internal sealed class MessageSink
    {
        public ConcurrentBag<string> Handled { get; } = [];

        public void Record(string what) => Handled.Add(what);

        public async Task<bool> WaitForAsync(int count, TimeSpan timeout)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < timeout)
            {
                if (Handled.Count >= count)
                    return true;

                await Task.Delay(25);
            }

            return Handled.Count >= count;
        }
    }

    internal sealed class PaymentProcessedHandler(IIntegrationEventBus bus, MessageSink sink)
        : IIntegrationEventHandler<PaymentProcessed>
    {
        public async Task HandleAsync(PaymentProcessed @event, CancellationToken cancellationToken = default)
        {
            sink.Record($"payment:{@event.OrderId}");

            // The event-plane handler reacts by dropping a command onto the work plane — the exact
            // "event on Kafka triggers a job on RabbitMQ" shape, expressed through the one seam.
            await bus.PublishAsync(
                new ShipOrderCommand(Guid.NewGuid(), DateTimeOffset.UtcNow, @event.OrderId),
                cancellationToken);
        }
    }

    internal sealed class ShipOrderCommandHandler(MessageSink sink)
        : IIntegrationEventHandler<ShipOrderCommand>
    {
        public Task HandleAsync(ShipOrderCommand @event, CancellationToken cancellationToken = default)
        {
            sink.Record($"ship:{@event.OrderId}");
            return Task.CompletedTask;
        }
    }

    internal sealed class AuditRecordedHandler(MessageSink sink)
        : IIntegrationEventHandler<AuditRecorded>
    {
        public Task HandleAsync(AuditRecorded @event, CancellationToken cancellationToken = default)
        {
            sink.Record($"audit:{@event.OrderId}");
            return Task.CompletedTask;
        }
    }

    private static IHost BuildHost(MessageSink sink)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddLogging();
        builder.Services.AddSingleton(sink);

        // Shared, transport-agnostic core — one dispatcher, one registry, one router for both planes.
        builder.Services.AddTarsIntegrationEventDispatcher();
        builder.Services.AddTarsIntegrationEventRouter();
        builder.Services.AddTarsIntegrationEventTypeRegistry(typeof(PaymentProcessed).Assembly);

        builder.Services.AddScoped<IIntegrationEventHandler<PaymentProcessed>, PaymentProcessedHandler>();
        builder.Services.AddScoped<IIntegrationEventHandler<ShipOrderCommand>, ShipOrderCommandHandler>();
        builder.Services.AddScoped<IIntegrationEventHandler<AuditRecorded>, AuditRecordedHandler>();

        // Two independent in-memory buses. Each consumes its own event type; AuditRecorded is consumed
        // on BOTH, so a fan-out publish is delivered to each plane.
        builder.Services.AddMassTransit<IEventPlaneBus>(x =>
        {
            x.AddConsumer<IntegrationEventRelayConsumer<PaymentProcessed>>();
            x.AddConsumer<IntegrationEventRelayConsumer<AuditRecorded>>();
            x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
        });

        builder.Services.AddMassTransit<IWorkPlaneBus>(x =>
        {
            x.AddConsumer<IntegrationEventRelayConsumer<ShipOrderCommand>>();
            x.AddConsumer<IntegrationEventRelayConsumer<AuditRecorded>>();
            x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
        });

        // Each plane behind a keyed IIntegrationEventBus, reusing the real MassTransit publish bus and
        // pointing it at that plane's IPublishEndpoint.
        builder.Services.AddKeyedScoped<IIntegrationEventBus>(EventPlane, (sp, _) =>
            new MassTransitIntegrationEventBus(
                sp.GetRequiredService<IEventPlaneBus>(),
                sp.GetRequiredService<IIntegrationEventRouter>(),
                sp.GetServices<IPublishRouteApplier>()));

        builder.Services.AddKeyedScoped<IIntegrationEventBus>(WorkPlane, (sp, _) =>
            new MassTransitIntegrationEventBus(
                sp.GetRequiredService<IWorkPlaneBus>(),
                sp.GetRequiredService<IIntegrationEventRouter>(),
                sp.GetServices<IPublishRouteApplier>()));

        // The seam: everything defaults to the event plane, commands are peeled off to the work plane.
        builder.Services.AddTarsCompositeIntegrationEventBus(map =>
        {
            map.DefaultTransport = EventPlane;
            map.Route<ShipOrderCommand>(WorkPlane);
            map.Route<AuditRecorded>(EventPlane, WorkPlane);   // fan-out to both planes
        });

        return builder.Build();
    }

    [Fact]
    public async Task An_event_on_one_plane_triggers_a_command_the_composite_routes_to_the_other_plane()
    {
        var sink = new MessageSink();
        using var host = BuildHost(sink);
        await host.StartAsync();

        try
        {
            using var scope = host.Services.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>();

            // Published through the one seam. The map sends it to the event plane (the default).
            await bus.PublishAsync(new PaymentProcessed(Guid.NewGuid(), DateTimeOffset.UtcNow, "order-42"));

            var arrived = await sink.WaitForAsync(2, TimeSpan.FromSeconds(10));

            arrived.Should().BeTrue("both the event-plane and work-plane handlers should have run");
            sink.Handled.Should().Contain("payment:order-42");   // delivered on the event plane
            sink.Handled.Should().Contain("ship:order-42");       // routed across to the work plane
        }
        finally
        {
            await host.StopAsync();
        }
    }

    [Fact]
    public async Task A_fan_out_event_is_delivered_to_both_planes_from_one_publish()
    {
        var sink = new MessageSink();
        using var host = BuildHost(sink);
        await host.StartAsync();

        try
        {
            using var scope = host.Services.CreateScope();
            var bus = scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>();

            // One publish; AuditRecorded is mapped to both planes, so the handler runs once per plane.
            await bus.PublishAsync(new AuditRecorded(Guid.NewGuid(), DateTimeOffset.UtcNow, "order-7"));

            var arrived = await sink.WaitForAsync(2, TimeSpan.FromSeconds(10));

            arrived.Should().BeTrue("the fan-out should reach both planes");
            sink.Handled.Count(x => x == "audit:order-7").Should().Be(2);
        }
        finally
        {
            await host.StopAsync();
        }
    }

    [Fact]
    public async Task The_composite_is_the_one_seam_producers_resolve()
    {
        var sink = new MessageSink();
        using var host = BuildHost(sink);

        using var scope = host.Services.CreateScope();

        scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>()
            .Should().BeOfType<Broker.Composite.CompositeIntegrationEventBus>();
    }
}
