using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pottmayer.Tars.UserContext.Options;

namespace Pottmayer.Tars.UserContext.DI;

public static class UserContextOptionsDI
{
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
