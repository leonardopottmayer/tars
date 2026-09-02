using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Web.Http.AspNetCore.Filters;

namespace Pottmayer.Tars.Web.Http.AspNetCore.DI;

/// <summary>
/// Provides granular dependency injection registrations for ASP.NET Core HTTP services.
/// </summary>
public static class WebHttpAspNetCoreServicesDI
{
    /// <summary>Registers the MVC response-wrapper result filter.</summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsResponseWrapperResultFilter(this IServiceCollection services)
    {
        services.TryAddScoped<ResponseWrapperResultFilter>();
        return services;
    }

    /// <summary>Registers the Minimal API response-wrapper endpoint filter.</summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsResponseWrapperEndpointFilter(this IServiceCollection services)
    {
        services.TryAddScoped<ResponseWrapperEndpointFilter>();
        return services;
    }

    /// <summary>Registers the MVC options setup that applies the response-wrapper filter globally.</summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsResponseWrapperMvcOptionsSetup(this IServiceCollection services)
    {
        services.TryAddSingleton<IConfigureOptions<MvcOptions>, ResponseWrapperMvcOptionsSetup>();
        return services;
    }

    /// <summary>Registers the Tars MVC exception filter.</summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsExceptionFilter(this IServiceCollection services)
    {
        services.TryAddScoped<HttpExceptionFilter>();
        return services;
    }

    /// <summary>Registers ASP.NET Core Problem Details with the current trace ID.</summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = ctx =>
            {
                ctx.ProblemDetails.Extensions["traceId"] =
                    System.Diagnostics.Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;
            };
        });
        return services;
    }
}
