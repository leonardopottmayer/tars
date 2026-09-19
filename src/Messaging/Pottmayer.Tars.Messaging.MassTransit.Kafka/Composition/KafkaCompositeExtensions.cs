using Pottmayer.Tars.Messaging.MassTransit.Composition;
using Pottmayer.Tars.Messaging.MassTransit.Kafka.Options;

namespace Pottmayer.Tars.Messaging.MassTransit.Kafka.Composition;

/// <summary>
/// Adds a Kafka transport to a composite (multi-transport) MassTransit application. This is the Kafka
/// half of <c>AddTarsMassTransitComposite</c>: referencing this package is what brings Kafka into the
/// mix, independent of any other transport.
/// </summary>
public static class KafkaCompositeExtensions
{
    /// <summary>
    /// Adds a Kafka transport under <paramref name="key"/> as a rider on the composite's primary bus.
    /// </summary>
    /// <param name="configuration">The composite configuration.</param>
    /// <param name="key">The transport key routes refer to and the composite resolves against.</param>
    /// <param name="configure">Configures the Kafka connection, topology and subscriptions.</param>
    /// <returns>The same configuration instance, for chaining.</returns>
    public static MassTransitCompositeConfiguration AddKafka(
        this MassTransitCompositeConfiguration configuration, string key, Action<MassTransitKafkaMessagingOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new MassTransitKafkaMessagingOptions();
        configure(options);

        return configuration.AddTransport(new KafkaRiderTransport(key, options));
    }
}
