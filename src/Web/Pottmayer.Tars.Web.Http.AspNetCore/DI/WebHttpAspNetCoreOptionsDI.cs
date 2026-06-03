using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Web.Http.AspNetCore.Options;

namespace Pottmayer.Tars.Web.Http.AspNetCore.DI;

public static class WebHttpAspNetCoreOptionsDI
{
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
