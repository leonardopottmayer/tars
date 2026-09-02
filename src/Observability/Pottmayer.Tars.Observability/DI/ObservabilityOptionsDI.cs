using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Observability.Options;

namespace Pottmayer.Tars.Observability.DI;

/// <summary>
/// Registers and binds <see cref="ObservabilityOptions"/> from configuration.
/// </summary>
public static class ObservabilityOptionsDI
{
    /// <summary>
    /// Binds <see cref="ObservabilityOptions"/> from configuration (default section
    /// <c>Tars:Observability</c>), defaulting <see cref="ObservabilityOptions.ServiceName"/> to the
    /// host's application name when it is left empty.
    /// </summary>
    public static OptionsBuilder<ObservabilityOptions> AddTarsObservabilityOptions(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<ObservabilityOptions>? configure = null)
    {
        sectionName ??= ObservabilityOptions.SectionName;
        var applicationName = builder.Environment.ApplicationName;

        var ob = builder.Services
            .AddOptions<ObservabilityOptions>()
            .Bind(builder.Configuration.GetSection(sectionName))
            .PostConfigure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.ServiceName) && !string.IsNullOrWhiteSpace(applicationName))
                    options.ServiceName = applicationName;
            })
            .Validate(ObservabilityOptionsValidation.Validate, ObservabilityOptions.ValidationErrorMessage)
            .ValidateOnStart();

        if (configure is not null)
            ob.Configure(configure);

        return ob;
    }
}
