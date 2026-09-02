using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Core.Localization.AspNetCore.Options;

namespace Pottmayer.Tars.Core.Localization.AspNetCore.DI;

/// <summary>Registration helper that binds and validates <see cref="LocalizationAspNetCoreOptions"/>.</summary>
public static class LocalizationAspNetCoreOptionsDI
{
    /// <summary>
    /// Binds <see cref="LocalizationAspNetCoreOptions"/> from configuration (default section
    /// <see cref="LocalizationAspNetCoreOptions.SectionName"/>) and validates it on application start.
    /// </summary>
    /// <param name="builder">The host application builder whose configuration and services are used.</param>
    /// <param name="sectionName">Configuration section to bind; defaults to <see cref="LocalizationAspNetCoreOptions.SectionName"/>.</param>
    /// <param name="configure">Optional code-based overrides applied after binding.</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> for further configuration.</returns>
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
