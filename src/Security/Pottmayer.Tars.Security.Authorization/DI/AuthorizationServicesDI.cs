using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Core.Mediator.Abstractions.Pipeline;
using Pottmayer.Tars.Security.Authorization.Abstractions;
using Pottmayer.Tars.Security.Authorization.Behaviors;
using Pottmayer.Tars.Security.Authorization.Configuration;

namespace Pottmayer.Tars.Security.Authorization.DI;

/// <summary>
/// The authorization pieces, registered one at a time. There is deliberately no "register
/// everything" method: declare permissions (once per module), then add the catalog, the checker,
/// the gate, an <see cref="IAuthorizationStore"/>, and the behavior with its actor resolver where
/// requests go through the mediator.
/// </summary>
public static class AuthorizationServicesDI
{
    /// <summary>
    /// Declares permissions. Call it once per module; every call adds to the same catalog.
    /// </summary>
    public static IServiceCollection AddTarsPermissionDefinitions(
        this IServiceCollection services,
        Action<PermissionCatalogConfiguration> declare)
    {
        ArgumentNullException.ThrowIfNull(declare);
        services.AddOptions<PermissionCatalogConfiguration>().Configure(declare);
        return services;
    }

    /// <summary>
    /// Registers <see cref="IPermissionCatalog"/> and validates the declarations at startup: segments,
    /// duplicate keys, feature flags that are not declared, and
    /// <see cref="RequiresPermissionAttribute"/> usages naming undeclared keys in the assemblies passed
    /// to <see cref="PermissionCatalogConfiguration.CheckUsagesIn"/>.
    /// </summary>
    public static IServiceCollection AddTarsPermissionCatalog(this IServiceCollection services)
    {
        services.AddOptions<PermissionCatalogConfiguration>().ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<PermissionCatalogConfiguration>, PermissionCatalogValidation>());
        services.TryAddSingleton<IPermissionCatalog, PermissionCatalog>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="IPermissionChecker"/> as a singleton that caches each subject's effective
    /// permissions until invalidated. Needs the catalog and an <see cref="IAuthorizationStore"/>.
    /// </summary>
    public static IServiceCollection AddTarsPermissionChecker(this IServiceCollection services)
    {
        services.TryAddSingleton<IPermissionChecker, PermissionChecker>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="AuthorizationGate"/>, the feature-then-permission check shared by the
    /// mediator behavior and the MVC filter. Needs <c>IFeatureFlags</c> when a permission is gated by
    /// a feature.
    /// </summary>
    public static IServiceCollection AddTarsAuthorizationGate(this IServiceCollection services)
    {
        services.TryAddSingleton<AuthorizationGate>();
        return services;
    }

    /// <summary>
    /// Registers the application's <see cref="IAuthorizationActorResolver"/>, which tells the
    /// behavior whose request it is.
    /// </summary>
    public static IServiceCollection AddTarsAuthorizationActorResolver<TResolver>(this IServiceCollection services)
        where TResolver : class, IAuthorizationActorResolver
    {
        services.TryAddSingleton<IAuthorizationActorResolver, TResolver>();
        return services;
    }

    /// <summary>
    /// Adds the authorization pipeline behavior. Behaviors run in registration order (first is
    /// outermost): register it after the exception-mapping behavior so a store failure is mapped too.
    /// </summary>
    public static IServiceCollection AddTarsAuthorizationBehavior(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>)));
        return services;
    }
}
