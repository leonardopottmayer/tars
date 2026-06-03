using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Core.Localization.Abstractions;
using Pottmayer.Tars.Core.Localization.AspNetCore.Options;
using Pottmayer.Tars.Core.Localization.DI;

namespace Pottmayer.Tars.Core.Localization.AspNetCore.DI;

public static class LocalizationAspNetCoreDI
{
    public static IHostApplicationBuilder AddTarsLocalizationAspNetCore(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<LocalizationAspNetCoreOptions>? configure = null)
    {
        builder.AddTarsLocalizationAspNetCoreOptions(sectionName, configure);
        builder.Services.AddTarsLocalization();
        builder.Services.AddLocalization();
        return builder;
    }

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
}
