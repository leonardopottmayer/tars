using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Ddd;
using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Relational.Abstractions.DataConnection;
using Pottmayer.Tars.Data.Relational.Abstractions.Enums;
using Pottmayer.Tars.Multitenancy.Abstractions.Context;

namespace Pottmayer.Tars.Data.Relational.DataContext;

/// <summary>
/// Creates <see cref="DataContext"/> instances for a specific database key and DbContext type.
/// Registered via <c>services.AddTarsData&lt;TDbContext&gt;(key, buildOptions)</c>.
/// </summary>
internal sealed class RelationalDataContextFactory<TDbContext> : IKeyedDataContextFactory
    where TDbContext : RelationalDbContext
{
    public string DatabaseKey { get; }

    private readonly IDataConnectionResolver _resolver;
    private readonly Func<IServiceProvider, IDataConnectionDescriptor, DbContextOptions<TDbContext>> _buildOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDataContextAccessor _accessor;
    private readonly IDomainEventDispatcher? _domainEventDispatcher;

    public RelationalDataContextFactory(
        string databaseKey,
        IDataConnectionResolver resolver,
        Func<IServiceProvider, IDataConnectionDescriptor, DbContextOptions<TDbContext>> buildOptions,
        IServiceProvider serviceProvider,
        IDataContextAccessor accessor,
        IDomainEventDispatcher? domainEventDispatcher)
    {
        DatabaseKey = string.IsNullOrWhiteSpace(databaseKey)
            ? throw new ArgumentException("Database key must not be null or empty.", nameof(databaseKey))
            : databaseKey;
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _buildOptions = buildOptions ?? throw new ArgumentNullException(nameof(buildOptions));
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
                $"No connection resolved for database key '{DatabaseKey}'. " +
                $"Ensure Tars:Data:Connections:{DatabaseKey} is configured in appsettings.json " +
                $"or register a custom IDataConnectionResolver.");

        var options = _buildOptions(_serviceProvider, descriptor);
        var dbContext = (TDbContext)Activator.CreateInstance(typeof(TDbContext), options)!;

        return new DataContext(DatabaseKey, dbContext, _serviceProvider, _accessor, _domainEventDispatcher, isAmbientOwner);
    }

    private DataConnectionResolutionContext BuildResolutionContext()
    {
        var tenantCtx = _serviceProvider.GetService<ITenantContextAccessor>()?.Current;
        return new DataConnectionResolutionContext
        {
            DatabaseKey = DatabaseKey,
            ServiceProvider = _serviceProvider,
            TenantKey = tenantCtx?.TenantKey,
            TenantCode = tenantCtx?.TenantCode
        };
    }
}
