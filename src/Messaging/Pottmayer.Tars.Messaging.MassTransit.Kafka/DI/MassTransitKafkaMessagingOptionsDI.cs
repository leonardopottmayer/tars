using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Messaging.MassTransit.Kafka.Options;

namespace Pottmayer.Tars.Messaging.MassTransit.Kafka.DI;

/// <summary>
/// Registration helpers for binding and configuring <see cref="MassTransitKafkaMessagingOptions"/>.
/// </summary>
public static class MassTransitKafkaMessagingOptionsDI
{
    /// <summary>
    /// Binds <see cref="MassTransitKafkaMessagingOptions"/> from configuration (default section
    /// <c>Tars:Messaging:Kafka</c>) and registers it for startup validation and injection.
    /// </summary>
    /// <param name="builder">The host application builder whose configuration and services are used.</param>
    /// <param name="sectionName">Configuration section to bind. Defaults to <see cref="MassTransitKafkaMessagingOptions.SectionName"/>.</param>
    /// <param name="configure">Optional code-based overrides applied after binding.</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> for further configuration.</returns>
    /// <remarks>
    /// Connection settings belong in configuration: bootstrap servers differ per environment and must
    /// not be compiled in. Subscriptions and assemblies stay in code — see
    /// <see cref="Broker.Options.BrokerMessagingOptions"/> — so use <paramref name="configure"/> for
    /// those.
    /// </remarks>
    public static OptionsBuilder<MassTransitKafkaMessagingOptions> AddTarsKafkaOptions(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<MassTransitKafkaMessagingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        sectionName ??= MassTransitKafkaMessagingOptions.SectionName;

        var ob = builder.Services
            .AddOptions<MassTransitKafkaMessagingOptions>()
            .Bind(builder.Configuration.GetSection(sectionName))
            .Validate(MassTransitKafkaMessagingOptionsValidation.Validate, MassTransitKafkaMessagingOptions.ValidationErrorMessage)
            .ValidateOnStart();

        if (configure is not null)
            ob.Configure(configure);

        return ob;
    }

    /// <summary>
    /// Builds the options the same way <see cref="AddTarsKafkaOptions"/> does, but returns the
    /// instance instead of registering it.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="sectionName">Optional custom section name.</param>
    /// <param name="configure">Optional delegate to configure options.</param>
    /// <returns>The bound and configured options instance.</returns>
    /// <remarks>
    /// Topics and producers are bound while services are being registered, before any provider exists
    /// to resolve <c>IOptions</c> from. So the registration path reads configuration eagerly rather
    /// than deferring it — which is also why a change to these values needs a restart.
    /// </remarks>
    internal static MassTransitKafkaMessagingOptions BuildTarsKafkaOptions(
        this IHostApplicationBuilder builder,
        string? sectionName,
        Action<MassTransitKafkaMessagingOptions>? configure)
    {
        var options = new MassTransitKafkaMessagingOptions();
        builder.Configuration.GetSection(sectionName ?? MassTransitKafkaMessagingOptions.SectionName).Bind(options);
        configure?.Invoke(options);

        return options;
    }
}
