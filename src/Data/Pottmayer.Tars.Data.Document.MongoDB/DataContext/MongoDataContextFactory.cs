using System.Collections.Concurrent;
using MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Ddd;
using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.DataContext;
using Pottmayer.Tars.Data.Document.Abstractions.Connection;
using Pottmayer.Tars.Multitenancy.Abstractions.Context;

namespace Pottmayer.Tars.Data.Document.MongoDB.DataContext;

/// <summary>
/// Creates <see cref="MongoDataContext"/> instances for a specific database key.
/// Registered via <c>services.AddTarsMongoData(key)</c>; contributes to the shared composite
/// <see cref="IDataContextFactory"/> so a MongoDB key coexists with relational keys behind one
/// <see cref="Pottmayer.Tars.Data.Abstractions.UnitOfWork.IUnitOfWorkFactory"/>.
/// </summary>
internal sealed class MongoDataContextFactory : IKeyedDataContextFactory
{
    // MongoClient is thread-safe and owns a connection pool; reuse one per connection string across the app.
    private static readonly ConcurrentDictionary<string, IMongoClient> Clients = new();

    public string DatabaseKey { get; }

    private readonly IMongoConnectionResolver _resolver;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDataContextAccessor _accessor;
    private readonly IDomainEventDispatcher? _domainEventDispatcher;

    public MongoDataContextFactory(
        string databaseKey,
        IMongoConnectionResolver resolver,
        IServiceProvider serviceProvider,
        IDataContextAccessor accessor,
        IDomainEventDispatcher? domainEventDispatcher)
    {
        DatabaseKey = string.IsNullOrWhiteSpace(databaseKey)
            ? throw new ArgumentException("Database key must not be null or empty.", nameof(databaseKey))
            : databaseKey;
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
        _domainEventDispatcher = domainEventDispatcher;
    }

    public Task<IDataContext> CreateScopedAsync(CancellationToken cancellationToken = default)
    {
        var existing = _accessor.GetCurrent(DatabaseKey);
        if (existing is not null)
            return Task.FromResult<IDataContext>(new BorrowedDataContext(existing));
        return CreateContextAsync(cancellationToken, isAmbientOwner: true);
    }

    public Task<IDataContext> CreateIsolatedAsync(CancellationToken cancellationToken = default)
        => CreateContextAsync(cancellationToken);

    private async Task<IDataContext> CreateContextAsync(CancellationToken cancellationToken, bool isAmbientOwner = false)
    {
        var resolutionCtx = BuildResolutionContext();
        var descriptor = await _resolver.ResolveAsync(resolutionCtx, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"No Mongo connection resolved for database key '{DatabaseKey}'. " +
                $"Ensure Tars:Data:Mongo:Connections:{DatabaseKey} is configured in appsettings.json " +
                $"or register a custom IMongoConnectionResolver.");

        var client = Clients.GetOrAdd(descriptor.ConnectionString, cs => new MongoClient(cs));
        var database = client.GetDatabase(descriptor.DatabaseName);

        return new MongoDataContext(DatabaseKey, client, database, _serviceProvider, _accessor, _domainEventDispatcher, isAmbientOwner);
    }

    private MongoConnectionResolutionContext BuildResolutionContext()
    {
        var tenantCtx = _serviceProvider.GetService<ITenantContextAccessor>()?.Current;
        return new MongoConnectionResolutionContext
        {
            DatabaseKey = DatabaseKey,
            ServiceProvider = _serviceProvider,
            TenantKey = tenantCtx?.TenantKey,
            TenantCode = tenantCtx?.TenantCode
        };
    }
}
