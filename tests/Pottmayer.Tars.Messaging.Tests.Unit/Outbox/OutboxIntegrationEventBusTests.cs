using FluentAssertions;
using Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;

namespace Pottmayer.Tars.Messaging.Tests.Unit.Outbox;

public class OutboxIntegrationEventBusTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PublishAsync_writes_a_row_into_the_single_open_context()
    {
        var repo = new FakeOutboxRepository();
        var accessor = new FakeDataContextAccessor(new FakeDataContext(repo));
        var bus = new OutboxIntegrationEventBus(accessor, new JsonIntegrationEventSerializer(), new ControllableClock(Now));

        var @event = new ThingHappened(Guid.NewGuid(), Now, "hi");
        await bus.PublishAsync(@event);

        repo.Store.Should().ContainSingle();
        var row = repo.Store[0];
        row.EventId.Should().Be(@event.EventId);
        row.EventType.Should().Be("tests.thing-happened");
        row.Version.Should().Be(3);
        row.Status.Should().Be(OutboxMessageStatus.Pending);
        row.Payload.Should().Contain("hi");
    }

    [Fact]
    public async Task PublishAsync_throws_when_there_is_no_open_unit_of_work()
    {
        var bus = new OutboxIntegrationEventBus(
            new FakeDataContextAccessor(), new JsonIntegrationEventSerializer(), new ControllableClock(Now));

        var act = () => bus.PublishAsync(new ThingHappened(Guid.NewGuid(), Now, "hi"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*without an open unit of work*");
    }
}
