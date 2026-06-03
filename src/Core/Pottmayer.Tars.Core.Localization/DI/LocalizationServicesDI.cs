using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Core.Localization.Abstractions;

namespace Pottmayer.Tars.Core.Localization.DI;

public static class LocalizationServicesDI
{
    public static IServiceCollection AddTarsLocalization(this IServiceCollection services)
    {
        services.TryAddSingleton<IMessageProvider, CompositeMessageProvider>();
        return services;
    }

    public static IServiceCollection AddTarsMessageSource(
        this IServiceCollection services,
        IMessageSource source)
    {
        services.AddSingleton(source);
        return services;
    }
}
