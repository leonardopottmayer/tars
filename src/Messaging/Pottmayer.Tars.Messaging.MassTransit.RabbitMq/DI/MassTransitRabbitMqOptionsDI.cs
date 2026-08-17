using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Messaging.MassTransit.RabbitMq.Options;

namespace Pottmayer.Tars.Messaging.MassTransit.RabbitMq.DI;

public static class MassTransitRabbitMqOptionsDI
{
    /// <summary>
    /// Binds <see cref="TarsRabbitMqOptions"/> from configuration (default section
    /// <c>Tars:Messaging:RabbitMq</c>) and registers it for injection.
    /// </summary>
    /// <remarks>
    /// Connection settings belong in configuration: host and credentials differ per environment and
    /// must not be compiled in. Subscriptions and assemblies stay in code — see
    /// <see cref="Broker.Options.BrokerMessagingOptions"/> — so use <paramref name="configure"/> for
    /// those.
    /// </remarks>
    public static OptionsBuilder<TarsRabbitMqOptions> AddTarsRabbitMqOptions(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<TarsRabbitMqOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        sectionName ??= TarsRabbitMqOptions.SectionName;

        var ob = builder.Services
            .AddOptions<TarsRabbitMqOptions>()
            .Bind(builder.Configuration.GetSection(sectionName));

        if (configure is not null)
            ob.Configure(configure);

        return ob;
    }

    /// <summary>
    /// Builds the options the same way <see cref="AddTarsRabbitMqOptions"/> does, but returns the
    /// instance instead of registering it.
    /// </summary>
    /// <remarks>
    /// The broker topology is built while services are being registered, before any provider exists
    /// to resolve <c>IOptions</c> from. So the registration path reads configuration eagerly rather
    /// than deferring it — which is also why a change to these values needs a restart.
    /// </remarks>
    internal static TarsRabbitMqOptions BuildTarsRabbitMqOptions(
        this IHostApplicationBuilder builder,
        string? sectionName,
        Action<TarsRabbitMqOptions>? configure)
    {
        var options = new TarsRabbitMqOptions();
        builder.Configuration.GetSection(sectionName ?? TarsRabbitMqOptions.SectionName).Bind(options);
        configure?.Invoke(options);

        return options;
    }
}
