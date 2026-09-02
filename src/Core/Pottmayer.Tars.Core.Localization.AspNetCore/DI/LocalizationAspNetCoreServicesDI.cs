using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Core.Localization.Abstractions;
using Pottmayer.Tars.Core.Localization.AspNetCore.Options;

namespace Pottmayer.Tars.Core.Localization.AspNetCore.DI;

/// <summary>
/// ASP.NET Core registration and middleware for tars localization. Register the pieces separately; there is
/// no all-in-one method. Each method documents whether it is required or optional and what it depends on.
/// </summary>
public static class LocalizationAspNetCoreServicesDI
{
    /// <summary>
    /// Registers ASP.NET Core's string-localization infrastructure (<see cref="IStringLocalizerFactory"/> and
    /// friends). Required only when using <see cref="AddTarsStringLocalizerSource{TResource}"/>.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddTarsAspNetCoreStringLocalization(this IServiceCollection services)
    {
        services.AddLocalization();
        return services;
    }

    /// <summary>
    /// Registers a <see cref="StringLocalizerMessageSource"/> for the given resource type as an
    /// <see cref="IMessageSource"/> queried by the message provider. Requires
    /// <see cref="AddTarsAspNetCoreStringLocalization"/>. Optional and repeatable — one call per resource.
    /// </summary>
    /// <typeparam name="TResource">The resource type identifying the localizer's base name and assembly.</typeparam>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddTarsStringLocalizerSource<TResource>(
        this IServiceCollection services)
    {
        services.AddSingleton<IMessageSource>(sp =>
        {
            var factory = sp.GetRequiredService<IStringLocalizerFactory>();
            var type = typeof(TResource);
            return new StringLocalizerMessageSource(factory, type.FullName!, type.Assembly.FullName!);
        });
        return services;
    }

    /// <summary>
    /// Adds request localization middleware configured from the bound options (default and supported cultures).
    /// Requires the options to be bound via <c>AddTarsLocalizationAspNetCoreOptions</c>.
    /// </summary>
    /// <param name="app">The application builder to add the middleware to.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static IApplicationBuilder UseTarsLocalization(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices
            .GetRequiredService<IOptions<LocalizationAspNetCoreOptions>>()
            .Value;

        var supported = options.SupportedCultures.ToArray();

        app.UseRequestLocalization(opts =>
        {
            opts.SetDefaultCulture(options.DefaultCulture);
            opts.AddSupportedCultures(supported);
            opts.AddSupportedUICultures(supported);
        });

        return app;
    }
}
