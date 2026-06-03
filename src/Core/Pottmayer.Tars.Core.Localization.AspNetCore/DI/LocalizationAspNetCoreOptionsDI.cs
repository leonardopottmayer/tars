using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Core.Localization.AspNetCore.Options;

namespace Pottmayer.Tars.Core.Localization.AspNetCore.DI;

public static class LocalizationAspNetCoreOptionsDI
{
    public static OptionsBuilder<LocalizationAspNetCoreOptions> AddTarsLocalizationAspNetCoreOptions(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<LocalizationAspNetCoreOptions>? configure = null)
    {
        sectionName ??= LocalizationAspNetCoreOptions.SectionName;
        var section = builder.Configuration.GetSection(sectionName);

        var ob = builder.Services
            .AddOptions<LocalizationAspNetCoreOptions>()
            .Bind(section)
            .Validate(
                LocalizationAspNetCoreOptionsValidation.Validate,
                LocalizationAspNetCoreOptions.ValidationErrorMessage)
            .ValidateOnStart();

        if (configure is not null)
            ob.Configure(configure);

        return ob;
    }
}
