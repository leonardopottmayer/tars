using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.EntityFrameworkCore.Relay;

namespace Pottmayer.Tars.Messaging.Tests.Unit.Outbox;

/// <summary>
/// The broker delivery seam in isolation: it forwards to the <em>keyed</em> transport bus, not the
/// ambient one (which <c>AddTarsOutboxBus</c> has turned into the outbox writer), and it fails with a
/// pointed message when that key is not registered.
/// </summary>
public class BrokerOutboxDeliveryTests
{
    private static readonly ThingHappened Event =
        new(Guid.NewGuid(), new DateTimeOffset(2026, 9, 18, 0, 0, 0, TimeSpan.Zero), "hi");

    [Fact]
    public async Task DeliverAsync_publishes_through_the_keyed_transport_bus()
    {
        var transport = new RecordingBus();
        var services = new ServiceCollection();
        services.AddKeyedScoped<IIntegrationEventBus>("events", (_, _) => transport);
        var provider = services.BuildServiceProvider();

        var delivery = new BrokerOutboxDelivery(
            provider.GetRequiredService<IServiceScopeFactory>(), "events");

        await delivery.DeliverAsync(Event);

        transport.Published.Should().ContainSingle().Which.Should().BeSameAs(Event);
    }

    [Fact]
    public async Task DeliverAsync_throws_a_clear_error_when_the_transport_key_is_not_registered()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        var delivery = new BrokerOutboxDelivery(
            provider.GetRequiredService<IServiceScopeFactory>(), "events");

        var act = () => delivery.DeliverAsync(Event);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("transport 'events'").And.Contain("AddTarsOutboxBrokerDelivery");
    }

    private sealed class RecordingBus : IIntegrationEventBus
    {
        public List<IIntegrationEvent> Published { get; } = [];

        public Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
        {
            Published.Add(@event);
            return Task.CompletedTask;
        }
    }
}
