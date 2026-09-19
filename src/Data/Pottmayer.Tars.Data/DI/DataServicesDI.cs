using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Abstractions.MultiDb;
using Pottmayer.Tars.Data.Abstractions.Repositories;
using Pottmayer.Tars.Data.Abstractions.UnitOfWork;
using Pottmayer.Tars.Data.DataContext;
using Pottmayer.Tars.Data.MultiDb;
using Pottmayer.Tars.Data.UnitOfWork;
using System.Reflection;

namespace Pottmayer.Tars.Data.DI;

/// <summary>
/// Provider-agnostic data infrastructure registration. These services are shared by every data axis
/// (relational, document, …); a provider package adds its own keyed pipelines on top (e.g.
/// <c>AddTarsRelationalData</c>, <c>AddTarsMongoData</c>). Each method registers a single service so
/// consumers can replace individual components.
/// </summary>
public static class DataServicesDI
{
    /// <summary>
    /// Registers <see cref="IDataContextAccessor"/> (<see cref="DataContextAccessor"/>) as Singleton.
    /// Tracks the ambient <see cref="IDataContext"/> for the current async flow, keyed by database role.
    /// </summary>
    public static IServiceCollection AddTarsDataContextAccessor(this IServiceCollection services)
    {
        services.TryAddSingleton<IDataContextAccessor, DataContextAccessor>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="CompositeDataContextFactory"/> as <see cref="IDataContextFactory"/> (Scoped).
    /// Delegates to all registered <see cref="IKeyedDataContextFactory"/> instances by database key,
    /// regardless of which provider contributed them.
    /// </summary>
    public static IServiceCollection AddTarsDataContextFactory(this IServiceCollection services)
    {
        services.TryAddScoped<IDataContextFactory, CompositeDataContextFactory>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="UnitOfWorkFactory"/> as <see cref="IUnitOfWorkFactory"/> (Scoped).
    /// Provider-agnostic — it drives whichever backend owns the requested key.
    /// </summary>
    public static IServiceCollection AddTarsUnitOfWorkFactory(this IServiceCollection services)
    {
        services.TryAddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="IMultiDatabaseCoordinator"/> (best-effort sequential commit).
    /// Required only when using <c>IMultiDatabaseCoordinator</c> directly. Works across a mix of providers.
    /// </summary>
    public static IServiceCollection AddTarsMultiDatabaseCoordination(this IServiceCollection services)
    {
        services.TryAddScoped<IMultiDatabaseCoordinator, MultiDatabaseCoordinator>();
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
