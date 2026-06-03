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

    public string DatabaseKey { get; }

    /// <summary>The underlying EF Core DbContext (for repository use only).</summary>
    public RelationalDbContext DbContext => _dbContext;

    /// <summary>The database connection shared with EF — use for Dapper queries in the same transaction.</summary>
    public IDbConnection Connection => _dbContext.Database.GetDbConnection();

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

    public TRepository AcquireRepository<TRepository>() where TRepository : class, IRepository
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _resolver.ResolveRepository<TRepository>();
    }

    public void CollectDomainEvents(IHasDomainEvents aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        foreach (var evt in aggregate.TakeDomainEvents())
            _manualDomainEvents.Add(evt);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (_domainEventDispatcher is not null)
        {
            var events = CollectAllDomainEvents();
            if (events.Count > 0)
                await _domainEventDispatcher.DispatchAsync(events, cancellationToken).ConfigureAwait(false);
        }
    }

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
