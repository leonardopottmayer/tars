using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Messaging.Broker.Options;

namespace Pottmayer.Tars.Messaging.Broker.DI;

/// <summary>
/// Registration helper that binds <see cref="BrokerMessagingOptions"/> from configuration.
/// </summary>
public static class BrokerMessagingOptionsDI
{
    /// <summary>
    /// Binds <see cref="BrokerMessagingOptions"/> from configuration (default section
    /// <c>Tars:Messaging:Broker</c>) and validates it on application start.
    /// </summary>
    /// <remarks>
    /// Only <see cref="BrokerMessagingOptions.EndpointName"/> comes from configuration — it is the one
    /// value that legitimately differs per environment. Subscriptions and assemblies are code: a
    /// subscription is a compile-time relationship between a handler and an event, and moving it into
    /// <c>appsettings</c> would turn a build error into a queue that silently never fills. Use
    /// <paramref name="configure"/> for those.
    /// </remarks>
    /// <param name="builder">The host application builder whose configuration and services are used.</param>
    /// <param name="sectionName">Configuration section to bind. Defaults to <see cref="BrokerMessagingOptions.SectionName"/>.</param>
    /// <param name="configure">Optional code-based overrides applied after binding.</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> for further configuration.</returns>
    public static OptionsBuilder<BrokerMessagingOptions> AddTarsBrokerMessagingOptions(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<BrokerMessagingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        sectionName ??= BrokerMessagingOptions.SectionName;

        var ob = builder.Services
            .AddOptions<BrokerMessagingOptions>()
            .Bind(builder.Configuration.GetSection(sectionName))
            .Validate(BrokerMessagingOptionsValidation.Validate, BrokerMessagingOptions.ValidationErrorMessage)
            .ValidateOnStart();

        if (configure is not null)
            ob.Configure(configure);

        return ob;
    }
}
