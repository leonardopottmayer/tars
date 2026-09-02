using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Multitenancy.AspNetCore.Middleware;
using Pottmayer.Tars.Multitenancy.AspNetCore.Resolvers;
using Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

namespace Pottmayer.Tars.Multitenancy.AspNetCore.DI;

/// <summary>
/// Provides ASP.NET Core-specific multitenancy registrations and middleware configuration.
/// </summary>
public static class MultitenancyAspNetCoreServicesDI
{
    /// <summary>
    /// Adds the tenant resolution middleware to the pipeline.
    /// Must be called after authentication/authorization middleware if using <see cref="Pottmayer.Tars.Multitenancy.Resolvers.ClaimTenantResolver"/>.
    /// </summary>
    /// <param name="app">The application pipeline to update.</param>
    /// <returns>The updated application pipeline.</returns>
    public static IApplicationBuilder UseTarsTenantResolution(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<TarsTenantResolutionMiddleware>();
    }

    /// <summary>
    /// Registers <see cref="HeaderTenantResolver"/> as a singleton <see cref="ITenantResolver"/>.
    /// Use with <c>AddTarsTenantResolution(o => o.AddResolver&lt;HeaderTenantResolver&gt;())</c>.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="headerName">The request header containing the tenant key.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsHeaderTenantResolver(
        this IServiceCollection services,
        string headerName = "X-Tenant-Key")
    {
        services.TryAddSingleton(new HeaderTenantResolver(headerName));
        return services;
    }

    /// <summary>
    /// Registers <see cref="SubdomainTenantResolver"/> as a singleton <see cref="ITenantResolver"/>.
    /// Use with <c>AddTarsTenantResolution(o => o.AddResolver&lt;SubdomainTenantResolver&gt;())</c>.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsSubdomainTenantResolver(this IServiceCollection services)
    {
        services.TryAddSingleton<SubdomainTenantResolver>();
        return services;
    }
}
