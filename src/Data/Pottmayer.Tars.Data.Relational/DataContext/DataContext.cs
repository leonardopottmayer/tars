using System.Data;
using Microsoft.EntityFrameworkCore;
using Pottmayer.Tars.Core.Ddd;
using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Abstractions.Repositories;
using Pottmayer.Tars.Data.Relational.Repositories;

namespace Pottmayer.Tars.Data.Relational.DataContext;

/// <summary>
/// Combines EF Core and Dapper over a single shared connection.
/// Domain events from EF's change tracker are collected automatically;
/// events from Dapper operations must be collected via <see cref="CollectDomainEvents"/>.
/// </summary>
public sealed class DataContext : IDataContext
{
    private readonly RelationalDbContext _dbContext;
    private readonly IDataContextAccessor _accessor;
    private readonly IRepositoryResolver _resolver;
    private readonly IDomainEventDispatcher? _domainEventDispatcher;
    private readonly List<object> _manualDomainEvents = [];
    private readonly bool _isAmbientOwner;
    private bool _disposed;

    /// <summary>Key identifying the logical database this context is bound to.</summary>
    public string DatabaseKey { get; }

    /// <summary>The underlying EF Core DbContext (for repository use only).</summary>
    public RelationalDbContext DbContext => _dbContext;

    /// <summary>The database connection shared with EF — use for Dapper queries in the same transaction.</summary>
    public IDbConnection Connection => _dbContext.Database.GetDbConnection();

    /// <inheritdoc/>
    public IRepositoryResolver Resolver => _resolver;

    internal DataContext(
        string databaseKey,
        RelationalDbContext dbContext,
        IServiceProvider serviceProvider,
        IDataContextAccessor accessor,
        IDomainEventDispatcher? domainEventDispatcher,
        bool isAmbientOwner = false)
    {
        DatabaseKey = string.IsNullOrWhiteSpace(databaseKey)
            ? throw new ArgumentException("Database key must not be null or empty.", nameof(databaseKey))
            : databaseKey;
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        _domainEventDispatcher = domainEventDispatcher;
        _isAmbientOwner = isAmbientOwner;
        _resolver = new RepositoryResolver(accessor, this, serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider)));
        if (isAmbientOwner)
            accessor.SetCurrent(databaseKey, this);
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

        // Domain events are dispatched BEFORE the state is persisted, not after — for two reasons.
        //
        // Correctness: SaveChanges flips every tracked entry to Unchanged, so collecting domain events
        // afterwards (as an earlier version did) would find nothing on the change tracker and silently
        // drop them. They have to be read while the entries are still Added/Modified/Deleted.
        //
        // Atomicity (the point of doing it here): a handler that reacts to a domain event by publishing
        // an integration event through an outbox-backed bus writes that outbox row into THIS context.
        // The single SaveChanges below then persists the aggregate's state change and the outbox row in
        // one transaction — the fact and its announcement commit together, or neither does. A handler
        // that throws propagates out before SaveChanges, so the whole unit of work is aborted.
        if (_domainEventDispatcher is not null)
        {
            var events = CollectAllDomainEvents();
            if (events.Count > 0)
                await _domainEventDispatcher.DispatchAsync(events, cancellationToken).ConfigureAwait(false);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Drains domain events from tracked (Added/Modified/Deleted) aggregates and the manual buffer.</summary>
    private IReadOnlyList<object> CollectAllDomainEvents()
    {
        var events = new List<object>();

        foreach (var entry in _dbContext.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (entry.Entity is IHasDomainEvents h)
                events.AddRange(h.TakeDomainEvents());
        }

        events.AddRange(_manualDomainEvents);
        _manualDomainEvents.Clear();

        return events;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        if (_isAmbientOwner)
            _accessor.SetCurrent(DatabaseKey, null);
        await _dbContext.DisposeAsync().ConfigureAwait(false);
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
