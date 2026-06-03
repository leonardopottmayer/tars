using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.Identity.Abstractions.Transport;
using Pottmayer.Tars.Security.Identity.AspNetCore.Token;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.DI;

public static class IdentityAspNetCoreDI
{
    /// <summary>
    /// Registers all ASP.NET Core token transport services: header reader, cookie reader,
    /// composite reader (as <see cref="ITokenInputReader"/>) and output writer (as <see cref="ITokenOutputWriter"/>).
    /// </summary>
    public static IServiceCollection AddTarsIdentityAspNetCoreTokenTransport(this IServiceCollection services)
    {
        services.AddTarsIdentityHeaderTokenReader();
        services.AddTarsIdentityCookieTokenReader();
        services.AddTarsIdentityCompositeTokenReader();
        services.AddTarsIdentityTokenOutputWriter();
        return services;
    }

    /// <summary>
    /// Registers the <see cref="ConfigureJwtBearerFromIdentityOptions"/> that bridges
    /// <see cref="Identity.Options.IdentityOptions"/> into JwtBearerOptions.
    /// </summary>
    public static IServiceCollection AddTarsIdentityAspNetCoreJwtBearer(this IServiceCollection services)
    {
        services.AddTarsIdentityJwtBearerOptionsConfiguration();
        return services;
    }

    public static IServiceCollection AddTarsIdentityHeaderTokenReader(this IServiceCollection services)
    {
        services.TryAddSingleton<HeaderTokenReader>();
        return services;
    }

    public static IServiceCollection AddTarsIdentityCookieTokenReader(this IServiceCollection services)
    {
        services.TryAddSingleton<CookieTokenReader>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="CompositeTokenReader"/> as the singleton <see cref="ITokenInputReader"/>.
    /// Also registers header and cookie readers as prerequisites.
    /// </summary>
    public static IServiceCollection AddTarsIdentityCompositeTokenReader(this IServiceCollection services)
    {
        services.AddTarsIdentityHeaderTokenReader();
        services.AddTarsIdentityCookieTokenReader();
        services.TryAddSingleton<CompositeTokenReader>();
        services.TryAddSingleton<ITokenInputReader>(sp => sp.GetRequiredService<CompositeTokenReader>());
        return services;
    }

    public static IServiceCollection AddTarsIdentityTokenOutputWriter(this IServiceCollection services)
    {
        services.TryAddSingleton<ITokenOutputWriter, TokenOutputWriter>();
        return services;
    }

    public static IServiceCollection AddTarsIdentityJwtBearerOptionsConfiguration(this IServiceCollection services)
    {
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerFromIdentityOptions>();
        return services;
    }
}
