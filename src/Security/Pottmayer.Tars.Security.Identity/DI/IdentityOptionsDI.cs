using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.Identity.Options;

namespace Pottmayer.Tars.Security.Identity.DI;

public static class IdentityOptionsDI
{
    public static IHostApplicationBuilder AddTarsIdentityOptions(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<IdentityOptions>? configure = null)
    {
        sectionName ??= IdentityOptions.SectionName;

        var section = builder.Configuration.GetSection(sectionName);

        var ob = builder.Services
            .AddOptions<IdentityOptions>()
            .Bind(section)
            .Validate(IdentityOptionsValidation.Validate, IdentityOptions.ValidationErrorMessage)
            .ValidateOnStart();

        if (configure is not null)
            ob.Configure(configure);

        return builder;
    }
}
