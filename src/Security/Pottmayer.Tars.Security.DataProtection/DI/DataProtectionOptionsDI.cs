using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Security.DataProtection.Options;

namespace Pottmayer.Tars.Security.DataProtection.DI;

public static class DataProtectionOptionsDI
{
    /// <summary>
    /// Binds <see cref="DataProtectionOptions"/> from configuration (default section
    /// <c>Tars:Security:DataProtection</c>).
    /// </summary>
    public static OptionsBuilder<DataProtectionOptions> AddTarsDataProtectionOptions(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<DataProtectionOptions>? configure = null)
    {
        sectionName ??= DataProtectionOptions.SectionName;

        var ob = builder.Services
            .AddOptions<DataProtectionOptions>()
            .Bind(builder.Configuration.GetSection(sectionName))
            .Validate(DataProtectionOptionsValidation.Validate, DataProtectionOptions.ValidationErrorMessage)
            .ValidateOnStart();

        if (configure is not null)
            ob.Configure(configure);

        return ob;
    }
}
