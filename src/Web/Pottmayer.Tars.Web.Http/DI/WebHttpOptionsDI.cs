using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Web.Http.Options;

namespace Pottmayer.Tars.Web.Http.DI;

/// <summary>
/// Provides configuration binding for <see cref="WebHttpOptions"/>.
/// </summary>
public static class WebHttpOptionsDI
{
    /// <summary>Binds and validates core HTTP options.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="sectionName">The optional configuration section name.</param>
    /// <param name="configure">An optional in-code configuration callback.</param>
    /// <returns>The configured options builder.</returns>
    public static OptionsBuilder<WebHttpOptions> AddTarsWebHttpOptions(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<WebHttpOptions>? configure = null)
    {
        sectionName ??= WebHttpOptions.SectionName;
        var section = builder.Configuration.GetSection(sectionName);

        var ob = builder.Services
            .AddOptions<WebHttpOptions>()
            .Bind(section)
            .Validate(WebHttpOptionsValidation.Validate, WebHttpOptions.ValidationErrorMessage)
            .ValidateOnStart();

        if (configure is not null)
            ob.Configure(configure);

        return ob;
    }
}
