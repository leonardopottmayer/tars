using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Core.Localization;
using Pottmayer.Tars.Core.Localization.DI;
using Pottmayer.Tars.Web.Http.Abstractions;
using Pottmayer.Tars.Web.Http.Internal;

namespace Pottmayer.Tars.Web.Http.DI;

public static class WebHttpServicesDI
{
    public static IServiceCollection AddTarsHttpErrorMapper<TMapper>(this IServiceCollection services)
        where TMapper : class, IHttpErrorMapper
    {
        services.TryAddSingleton<IHttpErrorMapper, TMapper>();
        services.AddTarsMessageSource(new InMemoryMessageSource(TarsHttpMessages.GetDefaultMessages()));
        return services;
    }

    public static IServiceCollection AddTarsDefaultHttpErrorMapper(this IServiceCollection services)
        => services.AddTarsHttpErrorMapper<DefaultHttpErrorMapper>();

    public static IServiceCollection AddTarsWrapDecisionService<TService>(this IServiceCollection services)
        where TService : class, IWrapDecisionService
    {
        services.TryAddSingleton<IWrapDecisionService, TService>();
        return services;
    }

    public static IServiceCollection AddTarsDefaultWrapDecisionService(this IServiceCollection services)
        => services.AddTarsWrapDecisionService<WrapDecisionService>();
}
