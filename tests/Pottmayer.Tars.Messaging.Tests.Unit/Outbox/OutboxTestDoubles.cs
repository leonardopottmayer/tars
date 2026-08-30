using Pottmayer.Tars.Core.Ddd;
using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Abstractions.Repositories;
using Pottmayer.Tars.Data.Abstractions.UnitOfWork;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker.Dispatch;
using Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;

namespace Pottmayer.Tars.Messaging.Tests.Unit.Outbox;

// A small event with an explicit name and version, used across the outbox tests.
[IntegrationEventName("tests.thing-happened", Version = 3)]
public sealed record ThingHappened(Guid EventId, DateTimeOffset OccurredAt, string What) : IIntegrationEvent;

// A headered variant, to exercise header serialization.
[IntegrationEventName("tests.headered-thing")]
public sealed record HeaderedThing(Guid EventId, DateTimeOffset OccurredAt, string What)
    : IIntegrationEvent, IHeaderedIntegrationEvent
{
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
}

internal sealed class ControllableClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
}

/// <summary>In-memory <see cref="IOutboxRepository"/>. Add stages, and the lease/get/purge read the same store.</summary>
internal sealed class FakeOutboxRepository : IOutboxRepository
{
    public List<OutboxMessage> Store { get; } = [];

    public Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        Store.Add(message);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<LeasedOutboxMessage>> LeaseDueAsync(
        DateTimeOffset now, DateTimeOffset leaseUntil, int batchSize, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<LeasedOutboxMessage>>(
            Store.Where(m => m.Status == OutboxMessageStatus.Pending && m.NextAttemptAt is not null && m.NextAttemptAt <= now)
                 .OrderBy(m => m.NextAttemptAt).ThenBy(m => m.Id)
                 .Take(batchSize)
                 .Select(m => new LeasedOutboxMessage(m.Id, m.EventType, m.Payload))
                 .ToList());

    public Task<IReadOnlyList<OutboxMessage>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<OutboxMessage>>(Store.Where(m => ids.Contains(m.Id)).ToList());

    public Task<int> PurgeDispatchedAsync(DateTimeOffset olderThan, int batchSize, CancellationToken cancellationToken = default)
    {
        var stale = Store.Where(m => m.Status == OutboxMessageStatus.Dispatched && m.ProcessedAt is not null && m.ProcessedAt < olderThan)
                         .Take(batchSize).ToList();
        foreach (var s in stale) Store.Remove(s);
        return Task.FromResult(stale.Count);
    }
}

/// <summary>An <see cref="IDataContext"/> that hands out one repository, for driving the bus/processor without EF.</summary>
internal sealed class FakeDataContext(IOutboxRepository outbox) : IDataContext
{
    public IRepositoryResolver Resolver => throw new NotSupportedException();

    public TRepository AcquireRepository<TRepository>() where TRepository : class, IRepository
        => outbox as TRepository
           ?? throw new InvalidOperationException($"FakeDataContext only serves {nameof(IOutboxRepository)}.");

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void CollectDomainEvents(IHasDomainEvents aggregate) { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class FakeDataContextAccessor(IDataContext? current = null) : IDataContextAccessor
{
    public IDataContext? Current { get; private set; } = current;
    public void SetCurrent(IDataContext? context) => Current = context;
    public IDataContext? GetCurrent(string databaseKey) => Current;
    public void SetCurrent(string databaseKey, IDataContext? context) { }
}

internal sealed class FakeUnitOfWorkFactory(IDataContext context) : IUnitOfWorkFactory
{
    public IUnitOfWork Create(string databaseKey) => throw new NotSupportedException();

    public Task ExecuteAsync(string databaseKey, Func<IDataContext, CancellationToken, Task> work,
        UnitOfWorkOptions? options = null, CancellationToken cancellationToken = default)
        => work(context, cancellationToken);

    public Task<T> ExecuteAsync<T>(string databaseKey, Func<IDataContext, CancellationToken, Task<T>> work,
        UnitOfWorkOptions? options = null, CancellationToken cancellationToken = default)
        => work(context, cancellationToken);
}

internal sealed class RecordingDispatcher : IIntegrationEventDispatcher
{
    public List<IIntegrationEvent> Dispatched { get; } = [];
    public Func<IIntegrationEvent, Task>? OnDispatch { get; set; }

    public Task DispatchAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        if (OnDispatch is not null)
            return OnDispatch(@event);
        Dispatched.Add(@event);
        return Task.CompletedTask;
    }
}
