using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Web.Http.Options;

namespace Pottmayer.Tars.Web.Http.DI;

public static class WebHttpOptionsDI
{
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
