using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Security.Identity.Abstractions.Contracts;
using Pottmayer.Tars.Security.Identity.Abstractions.Services;
using Pottmayer.Tars.Security.Identity.Abstractions.Stores;
using Pottmayer.Tars.Security.Identity.Abstractions.Token;
using Pottmayer.Tars.Security.Identity.Jwt;
using Pottmayer.Tars.Security.Identity.MagicLink;
using Pottmayer.Tars.Security.Identity.Refresh;
using Pottmayer.Tars.Security.Identity.Revocation;
using Pottmayer.Tars.Security.Identity.Stores;
using Pottmayer.Tars.Security.Identity.TokenDelivery;

namespace Pottmayer.Tars.Security.Identity.DI;

/// <summary>
/// Service registrations for the Identity module.
/// </summary>
public static class IdentityServicesDI
{
    /// <summary>Registers <see cref="InMemoryRefreshTokenStore"/> as the singleton <see cref="IRefreshTokenStore"/>. For development and single-instance only.</summary>
    public static IServiceCollection AddTarsIdentityInMemoryRefreshTokenStore(this IServiceCollection services)
    {
        services.TryAddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();
        return services;
    }

    /// <summary>Registers <see cref="InMemoryTokenRevocationStore"/> as the singleton <see cref="ITokenRevocationStore"/>. For development and single-instance only.</summary>
    public static IServiceCollection AddTarsIdentityInMemoryTokenRevocationStore(this IServiceCollection services)
    {
        services.TryAddSingleton<ITokenRevocationStore, InMemoryTokenRevocationStore>();
        return services;
    }

    /// <summary>Registers <see cref="InMemoryMagicLinkTokenStore"/> as the singleton <see cref="IMagicLinkTokenStore"/>. For development and single-instance only.</summary>
    public static IServiceCollection AddTarsIdentityInMemoryMagicLinkTokenStore(this IServiceCollection services)
    {
        services.TryAddSingleton<IMagicLinkTokenStore, InMemoryMagicLinkTokenStore>();
        return services;
    }

    /// <summary>Registers <see cref="JwtTokenIssuer"/> as the singleton <see cref="ITokenIssuer"/>.</summary>
    public static IServiceCollection AddTarsIdentityJwtTokenIssuer(this IServiceCollection services)
    {
        services.TryAddSingleton<ITokenIssuer, JwtTokenIssuer>();
        return services;
    }

    /// <summary>Registers <see cref="JwtTokenValidator"/> as the scoped <see cref="ITokenValidator"/>.</summary>
    public static IServiceCollection AddTarsIdentityJwtTokenValidator(this IServiceCollection services)
    {
        services.TryAddScoped<ITokenValidator, JwtTokenValidator>();
        return services;
    }

    /// <summary>Registers <see cref="RefreshTokenService"/> as the scoped <see cref="IRefreshTokenService"/>.</summary>
    public static IServiceCollection AddTarsIdentityRefreshTokenService(this IServiceCollection services)
    {
        services.TryAddScoped<IRefreshTokenService, RefreshTokenService>();
        return services;
    }

    /// <summary>Registers <see cref="TokenDeliveryPolicy"/> as a singleton.</summary>
    public static IServiceCollection AddTarsIdentityTokenDeliveryPolicy(this IServiceCollection services)
    {
        services.TryAddSingleton<TokenDeliveryPolicy>();
        return services;
    }

    /// <summary>Registers <see cref="MagicLinkTokenService"/> as the scoped <see cref="IMagicLinkTokenService"/>.</summary>
    public static IServiceCollection AddTarsIdentityMagicLinkTokenService(this IServiceCollection services)
    {
        services.TryAddScoped<IMagicLinkTokenService, MagicLinkTokenService>();
        return services;
    }

    /// <summary>Registers <see cref="TokenRevocationService"/> as the scoped <see cref="ITokenRevocationService"/>.</summary>
    public static IServiceCollection AddTarsIdentityTokenRevocationService(this IServiceCollection services)
    {
        services.TryAddScoped<ITokenRevocationService, TokenRevocationService>();
        return services;
    }
}
