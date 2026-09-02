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

/// <summary>
/// Provides granular dependency injection registrations for multitenancy services.
/// Register consumer overrides before calling these methods because they use <c>TryAdd</c>.
/// </summary>
public static class MultitenancyServicesDI
{
    /// <summary>
    /// Registers the singleton <see cref="ITenantContextAccessor"/> used to hold the ambient tenant context.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsTenantContextAccessor(this IServiceCollection services)
    {
        services.TryAddSingleton<ITenantContextAccessor, TenantContextAccessor>();
        return services;
    }

    /// <summary>
    /// Registers the singleton <see cref="ITenantContextFactory"/> that creates contexts from resolution results.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsTenantContextFactory(this IServiceCollection services)
    {
        services.TryAddSingleton<ITenantContextFactory, TenantContextFactory>();
        return services;
    }

    /// <summary>
    /// Registers the singleton <see cref="ITenantExecutionScopeFactory"/> for ambient tenant scopes.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsTenantExecutionScopeFactory(this IServiceCollection services)
    {
        services.TryAddSingleton<ITenantExecutionScopeFactory, TenantExecutionScopeFactory>();
        return services;
    }

    /// <summary>
    /// Registers the scoped <see cref="ITenantExecutionRunner"/> that runs work in isolated tenant scopes.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsTenantExecutionRunner(this IServiceCollection services)
    {
        services.TryAddScoped<ITenantExecutionRunner, TenantExecutionRunner>();
        return services;
    }

    /// <summary>
    /// Registers the tenant resolver pipeline with the given resolver types/instances.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configure">Configures the resolver order and registrations.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsTenantResolution(
        this IServiceCollection services,
        Action<TenantResolutionConfiguration>? configure = null)
    {
        var configuration = new TenantResolutionConfiguration();
        configure?.Invoke(configuration);

        foreach (var (type, instance) in configuration.Resolvers)
        {
            if (instance is not null)
                services.AddSingleton(type, instance);
            else
                services.TryAddSingleton(type, type);
        }

        services.AddSingleton(configuration);
        services.TryAddSingleton<ITenantResolverPipeline>(serviceProvider =>
        {
            var resolutionConfiguration = serviceProvider.GetRequiredService<TenantResolutionConfiguration>();
            var resolvers = resolutionConfiguration.Resolvers
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
    /// <param name="services">The service collection to update.</param>
    /// <param name="tenantKeys">The tenant keys exposed by the catalog.</param>
    /// <returns>The updated service collection.</returns>
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
    /// <typeparam name="TCatalog">The catalog implementation type.</typeparam>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
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
    /// <typeparam name="TStore">The store implementation type.</typeparam>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsTenantStore<TStore>(this IServiceCollection services)
        where TStore : class, ITenantStore
    {
        services.TryAddSingleton<ITenantStore, TStore>();
        return services;
    }

    /// <summary>
    /// Registers a custom <see cref="ITenantStore"/> instance directly.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="store">The store instance to register.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsTenantStore(
        this IServiceCollection services,
        ITenantStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        services.TryAddSingleton(store);
        return services;
    }
}
