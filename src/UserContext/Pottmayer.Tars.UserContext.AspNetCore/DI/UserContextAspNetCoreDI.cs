using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.UserContext.Abstractions;

namespace Pottmayer.Tars.UserContext.AspNetCore.DI;

public static class UserContextAspNetCoreDI
{
    /// <summary>
    /// Registers <see cref="CurrentPrincipalAccessor"/> as the <see cref="ICurrentPrincipalAccessor"/> implementation.
    /// Also registers <see cref="Microsoft.AspNetCore.Http.IHttpContextAccessor"/> if not already registered.
    /// </summary>
    public static IServiceCollection AddTarsCurrentPrincipalAccessor(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentPrincipalAccessor, CurrentPrincipalAccessor>();
        return services;
    }
}
