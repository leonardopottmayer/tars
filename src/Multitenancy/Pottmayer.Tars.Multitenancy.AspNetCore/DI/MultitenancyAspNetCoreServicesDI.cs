using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Multitenancy.AspNetCore.Middleware;
using Pottmayer.Tars.Multitenancy.AspNetCore.Resolvers;
using Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

namespace Pottmayer.Tars.Multitenancy.AspNetCore.DI;

public static class MultitenancyAspNetCoreServicesDI
{
    /// <summary>
    /// Adds the tenant resolution middleware to the pipeline.
    /// Must be called after authentication/authorization middleware if using <see cref="Pottmayer.Tars.Multitenancy.Resolvers.ClaimTenantResolver"/>.
    /// </summary>
    public static IApplicationBuilder UseTarsTenantResolution(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<TarsTenantResolutionMiddleware>();
    }

    /// <summary>
    /// Registers <see cref="HeaderTenantResolver"/> as a singleton <see cref="ITenantResolver"/>.
    /// Use with <c>AddTarsTenantResolution(o => o.AddResolver&lt;HeaderTenantResolver&gt;())</c>.
    /// </summary>
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
    public static IServiceCollection AddTarsSubdomainTenantResolver(this IServiceCollection services)
    {
        services.TryAddSingleton<SubdomainTenantResolver>();
        return services;
    }
}
