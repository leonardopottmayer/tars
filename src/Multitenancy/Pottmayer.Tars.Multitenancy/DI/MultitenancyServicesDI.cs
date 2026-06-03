using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Multitenancy.Abstractions.Catalog;
using Pottmayer.Tars.Multitenancy.Abstractions.Context;
using Pottmayer.Tars.Multitenancy.Abstractions.Execution;
using Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;
using Pottmayer.Tars.Multitenancy.Abstractions.Store;
using Pottmayer.Tars.Multitenancy.Catalog;
using Pottmayer.Tars.Multitenancy.Context;
using Pottmayer.Tars.Multitenancy.Execution;
using Pottmayer.Tars.Multitenancy.Resolvers;

namespace Pottmayer.Tars.Multitenancy.DI;

public static class MultitenancyServicesDI
{
    /// <summary>
    /// Registers core multitenancy services: accessor, factory, pipeline and execution helpers.
    /// Call <see cref="AddTarsTenantResolution"/> afterwards to register resolvers.
    /// </summary>
    public static IServiceCollection AddTarsMultitenancy(this IServiceCollection services)
    {
        services.TryAddSingleton<ITenantContextAccessor, TenantContextAccessor>();
        services.TryAddSingleton<ITenantContextFactory, TenantContextFactory>();
        services.TryAddSingleton<ITenantExecutionScopeFactory, TenantExecutionScopeFactory>();
        services.TryAddScoped<ITenantExecutionRunner, TenantExecutionRunner>();
        return services;
    }

    /// <summary>
    /// Registers the tenant resolver pipeline with the given resolver types/instances.
    /// </summary>
    public static IServiceCollection AddTarsTenantResolution(
        this IServiceCollection services,
        Action<TenantResolutionOptions>? configure = null)
    {
        var options = new TenantResolutionOptions();
        configure?.Invoke(options);

        foreach (var (type, instance) in options.Resolvers)
        {
            if (instance is not null)
                services.AddSingleton(type, instance);
            else
                services.TryAddSingleton(type, type);
        }

        services.AddSingleton(options);
        services.TryAddSingleton<ITenantResolverPipeline>(serviceProvider =>
        {
            var resolutionOptions = serviceProvider.GetRequiredService<TenantResolutionOptions>();
            var resolvers = resolutionOptions.Resolvers
                .Select(resolver => resolver.Instance as ITenantResolver
                    ?? (ITenantResolver)serviceProvider.GetRequiredService(resolver.ResolverType))
                .ToList();

            return new TenantResolverPipeline(resolvers);
        });
        return services;
    }

    /// <summary>
    /// Registers an <see cref="ITenantCatalog"/> backed by a fixed list of tenant keys.
    /// Useful for dev and testing. Replace with a database-backed catalog in production.
    /// </summary>
    public static IServiceCollection AddTarsInMemoryTenantCatalog(
        this IServiceCollection services,
        IEnumerable<string> tenantKeys)
    {
        services.TryAddSingleton<ITenantCatalog>(
            new InMemoryTenantCatalog(tenantKeys));
        return services;
    }

    /// <summary>
    /// Registers a custom <see cref="ITenantCatalog"/> implementation.
    /// </summary>
    public static IServiceCollection AddTarsTenantCatalog<TCatalog>(this IServiceCollection services)
        where TCatalog : class, ITenantCatalog
    {
        services.TryAddSingleton<ITenantCatalog, TCatalog>();
        return services;
    }

    /// <summary>
    /// Registers a custom <see cref="ITenantStore"/> implementation.
    /// The store provides point lookups by tenant ID or name, complementing <see cref="ITenantCatalog"/>.
    /// </summary>
    public static IServiceCollection AddTarsTenantStore<TStore>(this IServiceCollection services)
        where TStore : class, ITenantStore
    {
        services.TryAddSingleton<ITenantStore, TStore>();
        return services;
    }

    /// <summary>
    /// Registers a custom <see cref="ITenantStore"/> instance directly.
    /// </summary>
    public static IServiceCollection AddTarsTenantStore(
        this IServiceCollection services,
        ITenantStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        services.TryAddSingleton(store);
        return services;
    }
}
