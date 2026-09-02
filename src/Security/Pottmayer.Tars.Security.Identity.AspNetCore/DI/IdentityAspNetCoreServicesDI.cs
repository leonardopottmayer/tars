using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.Identity.Abstractions.Transport;
using Pottmayer.Tars.Security.Identity.AspNetCore.Token;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.DI;

/// <summary>
/// Service registrations for the ASP.NET Core host adapter of the Identity module.
/// </summary>
public static class IdentityAspNetCoreServicesDI
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
    /// <see cref="Identity.Options.SecurityIdentityOptions"/> into JwtBearerOptions.
    /// </summary>
    public static IServiceCollection AddTarsIdentityAspNetCoreJwtBearer(this IServiceCollection services)
    {
        services.AddTarsIdentityJwtBearerOptionsConfiguration();
        return services;
    }

    /// <summary>Registers <see cref="HeaderTokenReader"/> as a singleton.</summary>
    public static IServiceCollection AddTarsIdentityHeaderTokenReader(this IServiceCollection services)
    {
        services.TryAddSingleton<HeaderTokenReader>();
        return services;
    }

    /// <summary>Registers <see cref="CookieTokenReader"/> as a singleton.</summary>
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

    /// <summary>Registers <see cref="TokenOutputWriter"/> as the singleton <see cref="ITokenOutputWriter"/>.</summary>
    public static IServiceCollection AddTarsIdentityTokenOutputWriter(this IServiceCollection services)
    {
        services.TryAddSingleton<ITokenOutputWriter, TokenOutputWriter>();
        return services;
    }

    /// <summary>Registers <see cref="ConfigureJwtBearerFromIdentityOptions"/> as an <see cref="IConfigureOptions{TOptions}"/> for <c>JwtBearerOptions</c>.</summary>
    public static IServiceCollection AddTarsIdentityJwtBearerOptionsConfiguration(this IServiceCollection services)
    {
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerFromIdentityOptions>();
        return services;
    }
}
