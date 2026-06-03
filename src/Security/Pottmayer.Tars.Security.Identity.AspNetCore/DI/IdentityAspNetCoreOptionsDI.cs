using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.Identity.AspNetCore.Options;

namespace Pottmayer.Tars.Security.Identity.AspNetCore.DI;

public static class IdentityAspNetCoreOptionsDI
{
    public static IHostApplicationBuilder AddTarsIdentityAspNetCoreOptions(
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

        return builder;
    }
}
