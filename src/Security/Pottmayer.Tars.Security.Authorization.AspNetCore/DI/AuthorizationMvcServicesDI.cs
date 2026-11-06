using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Security.Authorization.AspNetCore.Filters;

namespace Pottmayer.Tars.Security.Authorization.AspNetCore.DI;

public static class AuthorizationMvcServicesDI
{
    /// <summary>
    /// Adds a global MVC filter that enforces <c>[RequiresPermission]</c> on controllers and actions,
    /// for endpoints that do not dispatch through the mediator. Needs the
    /// <see cref="AuthorizationGate"/> and an <c>IHttpErrorMapper</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Changes how the subject is read from the principal.</param>
    public static IServiceCollection AddTarsPermissionMvcFilter(
        this IServiceCollection services,
        Action<PermissionMvcFilterConfiguration>? configure = null)
    {
        var configuration = new PermissionMvcFilterConfiguration();
        configure?.Invoke(configuration);

        services.TryAddSingleton(configuration);
        services.TryAddScoped<PermissionActionFilter>();
        services.Configure<MvcOptions>(o => o.Filters.AddService<PermissionActionFilter>());
        return services;
    }
}
