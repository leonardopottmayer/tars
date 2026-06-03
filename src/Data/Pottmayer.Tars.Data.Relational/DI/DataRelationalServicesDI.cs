using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Core.Ddd;
using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Abstractions.Keys;
using Pottmayer.Tars.Data.Abstractions.Repositories;
using Pottmayer.Tars.Data.Abstractions.UnitOfWork;
using Pottmayer.Tars.Data.Relational.Abstractions.DataConnection;
using Pottmayer.Tars.Data.Relational.Abstractions.DataContext;
using Pottmayer.Tars.Data.Relational.Abstractions.MultiDb;
using Pottmayer.Tars.Data.Relational.DataConnection;
using Pottmayer.Tars.Data.Relational.DataContext;
using Pottmayer.Tars.Data.Relational.MultiDb;
using Pottmayer.Tars.Data.Relational.Repositories;
using Pottmayer.Tars.Data.Relational.UnitOfWork;
using System.Reflection;

namespace Pottmayer.Tars.Data.Relational.DI;

public static class DataRelationalServicesDI
{
    // ─────────────────────────────────────────────────────────────────────────
    // Infrastructure — register each component individually
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Registers <see cref="IDataContextAccessor"/> (<see cref="DataContextAccessor"/>) as Singleton.
    /// Tracks the ambient <see cref="IDataContext"/> for the current async flow.
    /// </summary>
    public static IServiceCollection AddTarsDataContextAccessor(this IServiceCollection services)
    {
        services.TryAddSingleton<IDataContextAccessor, DataContextAccessor>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="CompositeDataConnectionResolver"/> as the main <see cref="IDataConnectionResolver"/> Singleton.
    /// The composite chains all resolvers registered via <see cref="AddTarsRelationalConfigurationConnectionResolver"/>
    /// or custom <c>TryAddEnumerable</c> calls, returning the first non-null result.
    /// </summary>
    public static IServiceCollection AddTarsRelationalCompositeConnectionResolver(this IServiceCollection services)
    {
        services.TryAddSingleton<IDataConnectionResolver, CompositeDataConnectionResolver>();
        return services;
    }

    /// <summary>
    /// Adds <see cref="ConfigurationDataConnectionResolver"/> to the <see cref="IDataConnectionResolver"/> enumerable.
    /// Reads connection strings from <c>Tars:Data:Connections</c> in <c>appsettings.json</c>.
    /// Call <see cref="AddTarsRelationalCompositeConnectionResolver"/> to wire the chain.
    /// </summary>
    public static IServiceCollection AddTarsRelationalConfigurationConnectionResolver(this IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IDataConnectionResolver, ConfigurationDataConnectionResolver>());
        return services;
    }

    /// <summary>
    /// Registers <see cref="CompositeDataContextFactory"/> as <see cref="IDataContextFactory"/> (Scoped).
    /// Delegates to all registered <see cref="IKeyedDataContextFactory"/> instances by database key.
    /// </summary>
    public static IServiceCollection AddTarsDataContextFactory(this IServiceCollection services)
    {
        services.TryAddScoped<IDataContextFactory, CompositeDataContextFactory>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="UnitOfWorkFactory"/> as <see cref="IUnitOfWorkFactory"/> (Scoped).
    /// </summary>
    public static IServiceCollection AddTarsRelationalUnitOfWorkFactory(this IServiceCollection services)
    {
        services.TryAddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
        return services;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AddTarsData — database pipeline registration
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Registers the relational data pipeline for a <b>single-database</b> application
    /// using the <c>"default"</c> key.
    /// </summary>
    /// <remarks>
    /// Infrastructure services must be registered separately before calling this method:
    /// <see cref="AddTarsDataContextAccessor"/>,
    /// <see cref="AddTarsRelationalCompositeConnectionResolver"/>,
    /// <see cref="AddTarsRelationalConfigurationConnectionResolver"/>,
    /// <see cref="AddTarsDataContextFactory"/> and
    /// <see cref="AddTarsRelationalUnitOfWorkFactory"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddTarsData&lt;AppDbContext&gt;((sp, d) =>
    ///     new DbContextOptionsBuilder&lt;AppDbContext&gt;()
    ///         .UseNpgsql(d.ConnectionString)
    ///         .Options);
    /// </code>
    /// </example>
    public static IServiceCollection AddTarsData<TDbContext>(
        this IServiceCollection services,
        Func<IServiceProvider, IDataConnectionDescriptor, DbContextOptions<TDbContext>> buildOptions)
        where TDbContext : RelationalDbContext
        => services.AddTarsData(DataKeys.Default, buildOptions);

    /// <summary>
    /// Registers the relational data pipeline for a named database key.
    /// Call once per database in multi-database applications.
    /// </summary>
    /// <remarks>
    /// Infrastructure services must be registered separately before calling this method.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddTarsData&lt;AppDbContext&gt;("default", (sp, d) =>
    ///     new DbContextOptionsBuilder&lt;AppDbContext&gt;().UseNpgsql(d.ConnectionString).Options);
    ///
    /// services.AddTarsData&lt;CentralDbContext&gt;("central", (sp, d) =>
    ///     new DbContextOptionsBuilder&lt;CentralDbContext&gt;().UseNpgsql(d.ConnectionString).Options);
    /// </code>
    /// </example>
    public static IServiceCollection AddTarsData<TDbContext>(
        this IServiceCollection services,
        string databaseKey,
        Func<IServiceProvider, IDataConnectionDescriptor, DbContextOptions<TDbContext>> buildOptions)
        where TDbContext : RelationalDbContext
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseKey);
        ArgumentNullException.ThrowIfNull(buildOptions);

        services.AddScoped<IKeyedDataContextFactory>(sp =>
            new RelationalDataContextFactory<TDbContext>(
                databaseKey,
                sp.GetRequiredService<IDataConnectionResolver>(),
                buildOptions,
                sp,
                sp.GetRequiredService<IDataContextAccessor>(),
                sp.GetService<IDomainEventDispatcher>()));

        return services;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Repositories
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Scans the given assemblies and registers every concrete class that
    /// implements <c>IRepository</c> as Transient.
    /// </summary>
    public static IServiceCollection AddTarsDataRepositoriesFromAssemblies(
        this IServiceCollection services,
        params Assembly[] assemblies)
        => services.AddTarsDataRepositoriesFromAssemblies(ServiceLifetime.Transient, assemblies);

    /// <summary>
    /// Scans the given assemblies and registers repositories with the specified lifetime.
    /// </summary>
    public static IServiceCollection AddTarsDataRepositoriesFromAssemblies(
        this IServiceCollection services,
        ServiceLifetime lifetime,
        params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            if (assembly is null) continue;
            foreach (var type in assembly.GetExportedTypes())
            {
                if (!type.IsClass || type.IsAbstract) continue;
                foreach (var iface in type.GetInterfaces())
                {
                    if (!IsRepositoryInterface(iface)) continue;
                    services.Add(new ServiceDescriptor(iface, type, lifetime));
                }
            }
        }
        return services;
    }

    /// <summary>
    /// Scans the assemblies that contain the given marker types.
    /// </summary>
    public static IServiceCollection AddTarsDataRepositoriesFromAssemblies(
        this IServiceCollection services,
        params Type[] assemblyMarkerTypes)
        => services.AddTarsDataRepositoriesFromAssemblies(
            ServiceLifetime.Transient,
            assemblyMarkerTypes.Select(t => t.Assembly).Distinct().ToArray());

    // ─────────────────────────────────────────────────────────────────────────
    // Multi-database coordination
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Registers <see cref="IMultiDatabaseCoordinator"/> (best-effort sequential commit).
    /// Required only when using <c>IMultiDatabaseCoordinator</c> directly.
    /// </summary>
    public static IServiceCollection AddTarsMultiDatabaseCoordination(this IServiceCollection services)
    {
        services.TryAddScoped<IMultiDatabaseCoordinator, MultiDatabaseCoordinator>();
        return services;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Internal helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly Type RepositoryOpenType = typeof(IRepository<>);
    private static readonly Type RepositoryMarker = typeof(IRepository);

    private static bool IsRepositoryInterface(Type iface)
    {
        if (iface == RepositoryMarker) return false;
        if (iface.IsGenericType && iface.GetGenericTypeDefinition() == RepositoryOpenType) return true;
        return iface != RepositoryMarker &&
               iface.GetInterfaces().Any(i =>
                   i == RepositoryMarker || (i.IsGenericType && i.GetGenericTypeDefinition() == RepositoryOpenType));
    }
}
