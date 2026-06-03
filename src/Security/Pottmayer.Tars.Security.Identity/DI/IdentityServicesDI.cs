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

public static class IdentityServicesDI
{
    public static IServiceCollection AddTarsIdentityInMemoryRefreshTokenStore(this IServiceCollection services)
    {
        services.TryAddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();
        return services;
    }

    public static IServiceCollection AddTarsIdentityInMemoryTokenRevocationStore(this IServiceCollection services)
    {
        services.TryAddSingleton<ITokenRevocationStore, InMemoryTokenRevocationStore>();
        return services;
    }

    public static IServiceCollection AddTarsIdentityInMemoryMagicLinkTokenStore(this IServiceCollection services)
    {
        services.TryAddSingleton<IMagicLinkTokenStore, InMemoryMagicLinkTokenStore>();
        return services;
    }

    public static IServiceCollection AddTarsIdentityJwtTokenIssuer(this IServiceCollection services)
    {
        services.TryAddSingleton<ITokenIssuer, JwtTokenIssuer>();
        return services;
    }

    public static IServiceCollection AddTarsIdentityJwtTokenValidator(this IServiceCollection services)
    {
        services.TryAddScoped<ITokenValidator, JwtTokenValidator>();
        return services;
    }

    public static IServiceCollection AddTarsIdentityRefreshTokenService(this IServiceCollection services)
    {
        services.TryAddScoped<IRefreshTokenService, RefreshTokenService>();
        return services;
    }

    public static IServiceCollection AddTarsIdentityTokenDeliveryPolicy(this IServiceCollection services)
    {
        services.TryAddSingleton<TokenDeliveryPolicy>();
        return services;
    }

    public static IServiceCollection AddTarsIdentityMagicLinkTokenService(this IServiceCollection services)
    {
        services.TryAddScoped<IMagicLinkTokenService, MagicLinkTokenService>();
        return services;
    }

    public static IServiceCollection AddTarsIdentityTokenRevocationService(this IServiceCollection services)
    {
        services.TryAddScoped<ITokenRevocationService, TokenRevocationService>();
        return services;
    }
}
