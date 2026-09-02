using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.UserContext.Abstractions;
using Pottmayer.Tars.UserContext.Abstractions.Context;
using Pottmayer.Tars.UserContext.Context;
using Pottmayer.Tars.UserContext.Fallback;

namespace Pottmayer.Tars.UserContext.DI;

/// <summary>
/// Service registrations for the User Context module.
/// </summary>
public static class UserContextServicesDI
{
    /// <summary>
    /// Registers <see cref="AsyncLocalUserContextAccessor"/> as the singleton <see cref="IUserContextAccessor"/>.
    /// Works in any host: ASP.NET Core, workers, Blazor Server, and unit tests.
    /// </summary>
    public static IServiceCollection AddTarsUserContextAccessor(this IServiceCollection services)
    {
        services.TryAddSingleton<IUserContextAccessor, AsyncLocalUserContextAccessor>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="IUserContext"/> as a transient resolved from <see cref="IUserContextAccessor.Current"/>.
    /// Returns <see cref="UserContext.Anonymous"/> when no context has been set.
    /// </summary>
    public static IServiceCollection AddTarsUserContext(this IServiceCollection services)
    {
        services.TryAddTransient<IUserContext>(sp =>
            sp.GetRequiredService<IUserContextAccessor>().Current ?? UserContext.Anonymous);
        return services;
    }


    /// <summary>
    /// Registers <see cref="ClaimsUserResolver{TUser}"/> as the scoped <see cref="IUserResolver{TUser}"/>.
    /// </summary>
    public static IServiceCollection AddTarsClaimsUserResolver<TUser>(this IServiceCollection services)
        where TUser : class
    {
        services.TryAddScoped<IUserResolver<TUser>, ClaimsUserResolver<TUser>>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="DefaultUserContextFactory{TUser}"/> as the scoped <see cref="IUserContextFactory{TUser}"/>.
    /// </summary>
    public static IServiceCollection AddTarsDefaultUserContextFactory<TUser>(this IServiceCollection services)
        where TUser : class
    {
        services.TryAddScoped<IUserContextFactory<TUser>, DefaultUserContextFactory<TUser>>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="UserContextAccessor{TUser}"/> as the scoped <see cref="IUserContextAccessor{TUser}"/>.
    /// </summary>
    public static IServiceCollection AddTarsUserContextAccessor<TUser>(this IServiceCollection services)
        where TUser : class
    {
        services.TryAddScoped<IUserContextAccessor<TUser>, UserContextAccessor<TUser>>();
        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TProvider"/> as the scoped <see cref="IFallbackUserProvider{TUser}"/>.
    /// </summary>
    public static IServiceCollection AddTarsFallbackUserProvider<TUser, TProvider>(this IServiceCollection services)
        where TUser : class
        where TProvider : class, IFallbackUserProvider<TUser>
    {
        services.TryAddScoped<IFallbackUserProvider<TUser>, TProvider>();
        return services;
    }

    /// <summary>
    /// Registers a <see cref="DelegateFallbackUserProvider{TUser}"/> as the scoped <see cref="IFallbackUserProvider{TUser}"/>, backed by the given async delegate.
    /// </summary>
    public static IServiceCollection AddTarsFallbackUserProvider<TUser>(this IServiceCollection services, Func<CancellationToken, Task<TUser?>> getFallbackUserAsync)
        where TUser : class
    {
        services.TryAddScoped<IFallbackUserProvider<TUser>>(_ => new DelegateFallbackUserProvider<TUser>(getFallbackUserAsync));
        return services;
    }

    /// <summary>
    /// Registers a <see cref="DelegateFallbackUserProvider{TUser}"/> as the scoped <see cref="IFallbackUserProvider{TUser}"/>, backed by the given synchronous delegate.
    /// </summary>
    public static IServiceCollection AddTarsFallbackUserProvider<TUser>(this IServiceCollection services, Func<TUser?> getFallbackUser)
        where TUser : class
    {
        services.TryAddScoped<IFallbackUserProvider<TUser>>(_ => new DelegateFallbackUserProvider<TUser>(getFallbackUser));
        return services;
    }

}
