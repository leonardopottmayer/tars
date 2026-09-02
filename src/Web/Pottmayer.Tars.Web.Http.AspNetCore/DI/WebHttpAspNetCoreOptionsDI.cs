using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Web.Http.AspNetCore.Options;

namespace Pottmayer.Tars.Web.Http.AspNetCore.DI;

/// <summary>
/// Provides configuration binding for <see cref="WebHttpAspNetCoreOptions"/>.
/// </summary>
public static class WebHttpAspNetCoreOptionsDI
{
    /// <summary>Binds and validates ASP.NET Core HTTP options.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="sectionName">The optional configuration section name.</param>
    /// <param name="configure">An optional in-code configuration callback.</param>
    /// <returns>The configured options builder.</returns>
    public static OptionsBuilder<WebHttpAspNetCoreOptions> AddTarsWebHttpAspNetCoreOptions(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<WebHttpAspNetCoreOptions>? configure = null)
    {
        sectionName ??= WebHttpAspNetCoreOptions.SectionName;
        var section = builder.Configuration.GetSection(sectionName);

        var ob = builder.Services
            .AddOptions<WebHttpAspNetCoreOptions>()
            .Bind(section)
            .Validate(
                WebHttpAspNetCoreOptionsValidation.Validate,
                WebHttpAspNetCoreOptions.ValidationErrorMessage)
            .ValidateOnStart();

        if (configure is not null)
            ob.Configure(configure);

        return ob;
    }
}
