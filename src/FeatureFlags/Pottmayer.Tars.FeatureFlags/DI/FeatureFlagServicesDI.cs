using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.FeatureFlags.Abstractions;
using Pottmayer.Tars.FeatureFlags.Configuration;

namespace Pottmayer.Tars.FeatureFlags.DI;

/// <summary>
/// The feature flag pieces, registered one at a time. There is deliberately no "register
/// everything" method: declare flags (once per module), then add the catalog, the service and a
/// store.
/// </summary>
public static class FeatureFlagServicesDI
{
    /// <summary>
    /// Declares flags. Call it once per module that owns flags; every call adds to the same catalog.
    /// </summary>
    public static IServiceCollection AddTarsFeatureFlagDefinitions(
        this IServiceCollection services,
        Action<FeatureFlagCatalogConfiguration> declare)
    {
        ArgumentNullException.ThrowIfNull(declare);
        services.AddOptions<FeatureFlagCatalogConfiguration>().Configure(declare);
        return services;
    }

    /// <summary>
    /// Registers <see cref="IFeatureFlagCatalog"/> and validates the declared flags at startup
    /// (blank or duplicate keys, unknown parents, parent cycles).
    /// </summary>
    public static IServiceCollection AddTarsFeatureFlagCatalog(this IServiceCollection services)
    {
        services.AddOptions<FeatureFlagCatalogConfiguration>().ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<FeatureFlagCatalogConfiguration>, FeatureFlagCatalogValidation>());
        services.TryAddSingleton<IFeatureFlagCatalog, FeatureFlagCatalog>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="IFeatureFlags"/> as a singleton that caches the store's overrides until
    /// <see cref="IFeatureFlags.Invalidate"/>. Needs the catalog and an <see cref="IFeatureFlagStore"/>.
    /// </summary>
    public static IServiceCollection AddTarsFeatureFlagService(this IServiceCollection services)
    {
        services.TryAddSingleton<IFeatureFlags, FeatureFlagService>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="InMemoryFeatureFlagStore"/> as the <see cref="IFeatureFlagStore"/>, also
    /// resolvable as itself so code can call <see cref="InMemoryFeatureFlagStore.Set"/>.
    /// </summary>
    public static IServiceCollection AddTarsInMemoryFeatureFlagStore(this IServiceCollection services)
    {
        services.TryAddSingleton<InMemoryFeatureFlagStore>();
        services.TryAddSingleton<IFeatureFlagStore>(sp => sp.GetRequiredService<InMemoryFeatureFlagStore>());
        return services;
    }
}
