using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Web.Http.AspNetCore.Filters;

namespace Pottmayer.Tars.Web.Http.AspNetCore.DI;

public static class WebHttpAspNetCoreDI
{
    public static IServiceCollection AddTarsResponseWrapperResultFilter(this IServiceCollection services)
    {
        services.TryAddScoped<ResponseWrapperResultFilter>();
        return services;
    }

    public static IServiceCollection AddTarsResponseWrapperEndpointFilter(this IServiceCollection services)
    {
        services.TryAddScoped<ResponseWrapperEndpointFilter>();
        return services;
    }

    public static IServiceCollection AddTarsResponseWrapperMvcOptionsSetup(this IServiceCollection services)
    {
        services.TryAddSingleton<IConfigureOptions<MvcOptions>, ResponseWrapperMvcOptionsSetup>();
        return services;
    }

    public static IServiceCollection AddTarsExceptionFilter(this IServiceCollection services)
    {
        services.TryAddScoped<TarsExceptionFilter>();
        return services;
    }

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
