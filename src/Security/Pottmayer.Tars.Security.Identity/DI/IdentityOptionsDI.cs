using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.Identity.Options;

namespace Pottmayer.Tars.Security.Identity.DI;

/// <summary>
/// Registers and binds <see cref="SecurityIdentityOptions"/> from configuration.
/// </summary>
public static class IdentityOptionsDI
{
    /// <summary>
    /// Binds <see cref="SecurityIdentityOptions"/> from the given (or default) configuration section and validates it on start.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="sectionName">The configuration section name; defaults to <see cref="SecurityIdentityOptions.SectionName"/>.</param>
    /// <param name="configure">Optional programmatic overrides applied after binding.</param>
    /// <returns>The options builder, for further chaining.</returns>
    public static OptionsBuilder<SecurityIdentityOptions> AddTarsIdentityOptions(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<SecurityIdentityOptions>? configure = null)
    {
        sectionName ??= SecurityIdentityOptions.SectionName;

        var section = builder.Configuration.GetSection(sectionName);

        var ob = builder.Services
            .AddOptions<SecurityIdentityOptions>()
            .Bind(section)
            .Validate(IdentityOptionsValidation.Validate, SecurityIdentityOptions.ValidationErrorMessage)
            .ValidateOnStart();

        if (configure is not null)
            ob.Configure(configure);

        return ob;
    }
}
