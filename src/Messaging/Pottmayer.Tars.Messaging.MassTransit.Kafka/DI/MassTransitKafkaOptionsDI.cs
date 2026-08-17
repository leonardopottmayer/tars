using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Messaging.MassTransit.Kafka.Options;

namespace Pottmayer.Tars.Messaging.MassTransit.Kafka.DI;

public static class MassTransitKafkaOptionsDI
{
    /// <summary>
    /// Binds <see cref="TarsKafkaOptions"/> from configuration (default section
    /// <c>Tars:Messaging:Kafka</c>) and registers it for injection.
    /// </summary>
    /// <remarks>
    /// Connection settings belong in configuration: bootstrap servers differ per environment and must
    /// not be compiled in. Subscriptions and assemblies stay in code — see
    /// <see cref="Broker.Options.BrokerMessagingOptions"/> — so use <paramref name="configure"/> for
    /// those.
    /// </remarks>
    public static OptionsBuilder<TarsKafkaOptions> AddTarsKafkaOptions(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<TarsKafkaOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        sectionName ??= TarsKafkaOptions.SectionName;

        var ob = builder.Services
            .AddOptions<TarsKafkaOptions>()
            .Bind(builder.Configuration.GetSection(sectionName));

        if (configure is not null)
            ob.Configure(configure);

        return ob;
    }

    /// <summary>
    /// Builds the options the same way <see cref="AddTarsKafkaOptions"/> does, but returns the
    /// instance instead of registering it.
    /// </summary>
    /// <remarks>
    /// Topics and producers are bound while services are being registered, before any provider exists
    /// to resolve <c>IOptions</c> from. So the registration path reads configuration eagerly rather
    /// than deferring it — which is also why a change to these values needs a restart.
    /// </remarks>
    internal static TarsKafkaOptions BuildTarsKafkaOptions(
        this IHostApplicationBuilder builder,
        string? sectionName,
        Action<TarsKafkaOptions>? configure)
    {
        var options = new TarsKafkaOptions();
        builder.Configuration.GetSection(sectionName ?? TarsKafkaOptions.SectionName).Bind(options);
        configure?.Invoke(options);

        return options;
    }
}
