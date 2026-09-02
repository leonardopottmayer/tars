using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.Identity.AspNetCore.Options;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.DI;

/// <summary>
/// Registers and binds <see cref="IdentityAspNetCoreOptions"/> from configuration.
/// </summary>
public static class IdentityAspNetCoreOptionsDI
{
    /// <summary>
    /// Binds <see cref="IdentityAspNetCoreOptions"/> from the given (or default) configuration section and validates it on start.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="sectionName">The configuration section name; defaults to <see cref="IdentityAspNetCoreOptions.SectionName"/>.</param>
    /// <param name="configure">Optional programmatic overrides applied after binding.</param>
    /// <returns>The options builder, for further chaining.</returns>
    public static OptionsBuilder<IdentityAspNetCoreOptions> AddTarsIdentityAspNetCoreOptions(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<IdentityAspNetCoreOptions>? configure = null)
    {
        sectionName ??= IdentityAspNetCoreOptions.SectionName;

        var section = builder.Configuration.GetSection(sectionName);

        var ob = builder.Services
            .AddOptions<IdentityAspNetCoreOptions>()
            .Bind(section)
            .Validate(IdentityAspNetCoreOptionsValidation.Validate, IdentityAspNetCoreOptions.ValidationErrorMessage)
            .ValidateOnStart();

        if (configure is not null)
            ob.Configure(configure);

        return ob;
    }
}
