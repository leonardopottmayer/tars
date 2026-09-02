using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pottmayer.Tars.UserContext.Options;

namespace Pottmayer.Tars.UserContext.DI;

/// <summary>
/// Registers and binds <see cref="UserContextOptions"/> from configuration.
/// </summary>
public static class UserContextOptionsDI
{
    /// <summary>
    /// Binds <see cref="UserContextOptions"/> from the given (or default) configuration section and validates it on start.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="sectionName">The configuration section name; defaults to <see cref="UserContextOptions.SectionName"/>.</param>
    /// <param name="configure">Optional programmatic overrides applied after binding.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHostApplicationBuilder AddTarsUserContextOptions(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<UserContextOptions>? configure = null)
    {
        sectionName ??= UserContextOptions.SectionName;

        var section = builder.Configuration.GetSection(sectionName);

        var ob = builder.Services
            .AddOptions<UserContextOptions>()
            .Bind(section)
            .Validate(UserContextOptionsValidation.Validate, UserContextOptions.ValidationErrorMessage)
            .ValidateOnStart();

        if (configure is not null)
            ob.Configure(configure);

        return builder;
    }
}
