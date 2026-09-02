using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Core.Localization;
using Pottmayer.Tars.Core.Localization.DI;
using Pottmayer.Tars.Web.Http.Abstractions;
using Pottmayer.Tars.Web.Http.Internal;

namespace Pottmayer.Tars.Web.Http.DI;

/// <summary>
/// Provides granular dependency injection registrations for core HTTP services.
/// </summary>
public static class WebHttpServicesDI
{
    /// <summary>Registers a custom HTTP error mapper.</summary>
    /// <typeparam name="TMapper">The mapper implementation type.</typeparam>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsHttpErrorMapper<TMapper>(this IServiceCollection services)
        where TMapper : class, IHttpErrorMapper
    {
        services.TryAddSingleton<IHttpErrorMapper, TMapper>();
        services.AddTarsMessageSource(new InMemoryMessageSource(TarsHttpMessages.GetDefaultMessages()));
        return services;
    }

    /// <summary>Registers the default HTTP error mapper.</summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsDefaultHttpErrorMapper(this IServiceCollection services)
        => services.AddTarsHttpErrorMapper<DefaultHttpErrorMapper>();

    /// <summary>Registers a custom response wrapping decision service.</summary>
    /// <typeparam name="TService">The decision service implementation type.</typeparam>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsWrapDecisionService<TService>(this IServiceCollection services)
        where TService : class, IWrapDecisionService
    {
        services.TryAddSingleton<IWrapDecisionService, TService>();
        return services;
    }

    /// <summary>Registers the default response wrapping decision service.</summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsDefaultWrapDecisionService(this IServiceCollection services)
        => services.AddTarsWrapDecisionService<WrapDecisionService>();
}
