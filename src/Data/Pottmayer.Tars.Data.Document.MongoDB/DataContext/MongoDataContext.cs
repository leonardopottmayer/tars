using MongoDB.Driver;
using Pottmayer.Tars.Core.Ddd;
using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Abstractions.Repositories;
using Pottmayer.Tars.Data.Repositories;

namespace Pottmayer.Tars.Data.Document.MongoDB.DataContext;

/// <summary>
/// A unit-of-work boundary over a MongoDB database. Every read and write flows through a single
/// <see cref="IClientSessionHandle"/> with an open transaction, started lazily on first use — so reads
/// see this unit's own uncommitted writes (read-your-writes) and <see cref="CommitAsync"/> flushes them
/// atomically. Requires the server to be a replica set (MongoDB transactions are replica-set only).
/// <para>
/// MongoDB has no change tracker, so aggregates are registered for domain-event collection explicitly by
/// repositories via <see cref="Track"/> (or by callers via <see cref="CollectDomainEvents"/>). Events are
/// dispatched inside the transaction, before commit — an outbox-style handler that writes through this same
/// context joins the transaction, so the fact and its announcement commit together.
/// </para>
/// </summary>
public sealed class MongoDataContext : IDataContext
{
    private readonly IMongoClient _client;
    private readonly IMongoDatabase _database;
    private readonly IDataContextAccessor _accessor;
    private readonly IDomainEventDispatcher? _domainEventDispatcher;
    private readonly RepositoryResolver _resolver;
    private readonly List<IHasDomainEvents> _tracked = [];
    private readonly List<object> _manualDomainEvents = [];
    private readonly bool _isAmbientOwner;
    private IClientSessionHandle? _session;
    private bool _committed;
    private bool _disposed;

    /// <summary>Key identifying the logical database this context is bound to.</summary>
    public string DatabaseKey { get; }

    /// <summary>The underlying MongoDB database (for repository use only).</summary>
    public IMongoDatabase Database => _database;

    /// <inheritdoc/>
    public IRepositoryResolver Resolver => _resolver;

    internal MongoDataContext(
        string databaseKey,
        IMongoClient client,
        IMongoDatabase database,
        IServiceProvider serviceProvider,
        IDataContextAccessor accessor,
        IDomainEventDispatcher? domainEventDispatcher,
        bool isAmbientOwner = false)
    {
        DatabaseKey = string.IsNullOrWhiteSpace(databaseKey)
            ? throw new ArgumentException("Database key must not be null or empty.", nameof(databaseKey))
            : databaseKey;
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        _domainEventDispatcher = domainEventDispatcher;
        _isAmbientOwner = isAmbientOwner;
        _resolver = new RepositoryResolver(accessor, this, serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider)));
        if (isAmbientOwner)
            accessor.SetCurrent(databaseKey, this);
    }

    /// <summary>
    /// Returns the ambient session, starting it (and an open transaction) on first call. Repositories call
    /// this before every operation so all work in this unit shares one transaction.
    /// </summary>
    public async ValueTask<IClientSessionHandle> GetSessionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_session is null)
        {
            _session = await _client.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            _session.StartTransaction();
        }
        return _session;
    }

    /// <summary>Registers an aggregate so its domain events are collected and dispatched on commit.</summary>
    /// <param name="aggregate">The aggregate a repository just added, updated or removed.</param>
    public void Track(IHasDomainEvents aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        _tracked.Add(aggregate);
    }

    /// <inheritdoc/>
    public TRepository AcquireRepository<TRepository>() where TRepository : class, IRepository
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _resolver.ResolveRepository<TRepository>();
    }

    /// <inheritdoc/>
    public void CollectDomainEvents(IHasDomainEvents aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        foreach (var evt in aggregate.TakeDomainEvents())
            _manualDomainEvents.Add(evt);
    }

    /// <inheritdoc/>
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Dispatch domain events BEFORE committing the transaction (see type remarks): a handler that writes
        // through this same context/session joins the transaction, so state and announcement commit together.
        // A throwing handler propagates out before the commit, aborting the whole unit of work.
        if (_domainEventDispatcher is not null)
        {
            var events = CollectAllDomainEvents();
            if (events.Count > 0)
                await _domainEventDispatcher.DispatchAsync(events, cancellationToken).ConfigureAwait(false);
        }

        if (_session is not null && _session.IsInTransaction)
        {
            await _session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
            _committed = true;
        }
    }

    /// <summary>Drains domain events from tracked aggregates and the manual buffer.</summary>
    private IReadOnlyList<object> CollectAllDomainEvents()
    {
        var events = new List<object>();
        foreach (var aggregate in _tracked)
            events.AddRange(aggregate.TakeDomainEvents());
        events.AddRange(_manualDomainEvents);
        _manualDomainEvents.Clear();
        _tracked.Clear();
        return events;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        if (_isAmbientOwner)
            _accessor.SetCurrent(DatabaseKey, null);
        if (_session is not null)
        {
            if (_session.IsInTransaction && !_committed)
            {
                try { await _session.AbortTransactionAsync().ConfigureAwait(false); }
                catch { /* best-effort rollback on dispose */ }
            }
            _session.Dispose();
        }
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
