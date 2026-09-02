using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Messaging.MassTransit.RabbitMq.Options;

namespace Pottmayer.Tars.Messaging.MassTransit.RabbitMq.DI;

/// <summary>
/// Registration helpers for binding and configuring <see cref="MassTransitRabbitMqMessagingOptions"/>.
/// </summary>
public static class MassTransitRabbitMqMessagingOptionsDI
{
    /// <summary>
    /// Binds <see cref="MassTransitRabbitMqMessagingOptions"/> from configuration (default section
    /// <c>Tars:Messaging:RabbitMq</c>) and registers it for startup validation and injection.
    /// </summary>
    /// <param name="builder">The host application builder whose configuration and services are used.</param>
    /// <param name="sectionName">Configuration section to bind. Defaults to <see cref="MassTransitRabbitMqMessagingOptions.SectionName"/>.</param>
    /// <param name="configure">Optional code-based overrides applied after binding.</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> for further configuration.</returns>
    /// <remarks>
    /// Connection settings belong in configuration: host and credentials differ per environment and
    /// must not be compiled in. Subscriptions and assemblies stay in code — see
    /// <see cref="Broker.Options.BrokerMessagingOptions"/> — so use <paramref name="configure"/> for
    /// those.
    /// </remarks>
    public static OptionsBuilder<MassTransitRabbitMqMessagingOptions> AddTarsRabbitMqOptions(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<MassTransitRabbitMqMessagingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        sectionName ??= MassTransitRabbitMqMessagingOptions.SectionName;

        var ob = builder.Services
            .AddOptions<MassTransitRabbitMqMessagingOptions>()
            .Bind(builder.Configuration.GetSection(sectionName))
            .Validate(MassTransitRabbitMqMessagingOptionsValidation.Validate, MassTransitRabbitMqMessagingOptions.ValidationErrorMessage)
            .ValidateOnStart();

        if (configure is not null)
            ob.Configure(configure);

        return ob;
    }

    /// <summary>
    /// Builds the options the same way <see cref="AddTarsRabbitMqOptions"/> does, but returns the
    /// instance instead of registering it.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="sectionName">Optional custom section name.</param>
    /// <param name="configure">Optional delegate to configure options.</param>
    /// <returns>The bound and configured options instance.</returns>
    /// <remarks>
    /// The broker topology is built while services are being registered, before any provider exists
    /// to resolve <c>IOptions</c> from. So the registration path reads configuration eagerly rather
    /// than deferring it — which is also why a change to these values needs a restart.
    /// </remarks>
    internal static MassTransitRabbitMqMessagingOptions BuildTarsRabbitMqOptions(
        this IHostApplicationBuilder builder,
        string? sectionName,
        Action<MassTransitRabbitMqMessagingOptions>? configure)
    {
        var options = new MassTransitRabbitMqMessagingOptions();
        builder.Configuration.GetSection(sectionName ?? MassTransitRabbitMqMessagingOptions.SectionName).Bind(options);
        configure?.Invoke(options);

        return options;
    }
}
