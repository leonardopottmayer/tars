using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Core.Localization.Abstractions;

namespace Pottmayer.Tars.Core.Localization.DI;

/// <summary>Registration helpers for localization: the message provider and its sources.</summary>
public static class LocalizationServicesDI
{
    /// <summary>Registers <see cref="CompositeMessageProvider"/> as the <see cref="IMessageProvider"/>.</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddTarsLocalization(this IServiceCollection services)
    {
        services.TryAddSingleton<IMessageProvider, CompositeMessageProvider>();
        return services;
    }

    /// <summary>Registers an <see cref="IMessageSource"/> instance queried by the message provider.</summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="source">The message source to register.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddTarsMessageSource(
        this IServiceCollection services,
        IMessageSource source)
    {
        services.AddSingleton(source);
        return services;
    }
}
