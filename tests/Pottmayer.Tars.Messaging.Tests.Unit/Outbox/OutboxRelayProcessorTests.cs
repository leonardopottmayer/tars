using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Pottmayer.Tars.Data.Abstractions.UnitOfWork;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker.Registry;
using Pottmayer.Tars.Messaging.EntityFrameworkCore.Options;
using Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;
using Pottmayer.Tars.Messaging.EntityFrameworkCore.Relay;

namespace Pottmayer.Tars.Messaging.Tests.Unit.Outbox;

public class OutboxRelayProcessorTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeOutboxRepository _repo = new();
    private readonly JsonIntegrationEventSerializer _serializer = new();
    private readonly ControllableClock _clock = new(Now);
    private readonly IntegrationEventTypeRegistry _registry = new([typeof(ThingHappened), typeof(HeaderedThing)]);

    private OutboxRelayProcessor CreateProcessor(RecordingDispatcher dispatcher, OutboxDatabaseOptions? options = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IUnitOfWorkFactory>(new FakeUnitOfWorkFactory(new FakeDataContext(_repo)));
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        return new OutboxRelayProcessor(
            scopeFactory, _registry, dispatcher, _serializer, _clock, NullLogger.Instance,
            options ?? new OutboxDatabaseOptions("test") { Backoff = _ => TimeSpan.FromSeconds(10) });
    }

    private OutboxMessage Seed(string what = "hi")
    {
        var evt = new ThingHappened(Guid.NewGuid(), Now, what);
        var message = OutboxMessage.Enqueue(
            evt.EventId,
            IntegrationEventNaming.For(typeof(ThingHappened)),
            IntegrationEventNaming.VersionFor(typeof(ThingHappened)),
            _serializer.SerializePayload(evt), null, evt.OccurredAt, _clock);
        _repo.Store.Add(message);
        return message;
    }

    [Fact]
    public async Task DrainOnce_delivers_due_messages_and_marks_them_dispatched()
    {
        Seed("one");
        Seed("two");
        var dispatcher = new RecordingDispatcher();
        var processor = CreateProcessor(dispatcher);

        var delivered = await processor.DrainOnceAsync();

        delivered.Should().Be(2);
        dispatcher.Dispatched.Should().HaveCount(2);
        _repo.Store.Should().OnlyContain(m => m.Status == OutboxMessageStatus.Dispatched);
    }

    [Fact]
    public async Task DrainOnce_marks_failed_when_a_handler_throws_and_leaves_the_rest_dispatched()
    {
        var poison = Seed("poison");
        Seed("good");
        var dispatcher = new RecordingDispatcher
        {
            OnDispatch = e => ((ThingHappened)e).What == "poison"
                ? throw new InvalidOperationException("handler down")
                : Task.CompletedTask
        };
        var processor = CreateProcessor(dispatcher);

        var delivered = await processor.DrainOnceAsync();

        delivered.Should().Be(1);
        var poisonRow = _repo.Store.Single(m => m.Id == poison.Id);
        poisonRow.Status.Should().Be(OutboxMessageStatus.Pending);
        poisonRow.Attempts.Should().Be(1);
        poisonRow.NextAttemptAt.Should().Be(Now.AddSeconds(10));
        poisonRow.Error.Should().Be("handler down");
        _repo.Store.Count(m => m.Status == OutboxMessageStatus.Dispatched).Should().Be(1);
    }

    [Fact]
    public async Task DrainOnce_fails_a_message_whose_type_is_not_registered()
    {
        var unknown = OutboxMessage.Enqueue(
            Guid.NewGuid(), "tests.unregistered", 1, "{}", null, Now, _clock);
        _repo.Store.Add(unknown);
        var processor = CreateProcessor(new RecordingDispatcher());

        var delivered = await processor.DrainOnceAsync();

        delivered.Should().Be(0);
        var row = _repo.Store.Single();
        row.Status.Should().Be(OutboxMessageStatus.Pending);
        row.Attempts.Should().Be(1);
        row.Error.Should().Contain("No integration event type is registered");
    }

    [Fact]
    public async Task DrainOnce_ignores_messages_not_yet_due()
    {
        var future = Seed("later");
        // Push its next attempt into the future.
        future.MarkFailed("transient", maxAttempts: 8, backoff: _ => TimeSpan.FromMinutes(5), _clock);
        var processor = CreateProcessor(new RecordingDispatcher());

        var delivered = await processor.DrainOnceAsync();

        delivered.Should().Be(0, "the only message is scheduled for the future");
    }

    [Fact]
    public async Task PurgeOnce_removes_dispatched_rows_past_retention()
    {
        var old = Seed("old");
        old.MarkDispatched(_clock);
        // Age the processed timestamp beyond the retention window.
        _clock.Now = Now.AddDays(30);
        var processor = CreateProcessor(new RecordingDispatcher(),
            new OutboxDatabaseOptions("test") { RetentionPeriod = TimeSpan.FromDays(7) });

        var purged = await processor.PurgeOnceAsync();

        purged.Should().Be(1);
        _repo.Store.Should().BeEmpty();
    }
}
