using Confluent.Kafka;
using MassTransit;
using Pottmayer.Tars.Messaging.Broker.Options;
using Pottmayer.Tars.Messaging.MassTransit.Options;

namespace Pottmayer.Tars.Messaging.MassTransit.Kafka.Options;

/// <summary>
/// Kafka connection and topology, plus the portable subscription model shared by every provider.
/// </summary>
public sealed class MassTransitKafkaMessagingOptions : MassTransitMessagingOptions
{
    /// <summary>Default configuration section these options bind from (<c>Tars:Messaging:Kafka</c>).</summary>
    public const string SectionName = "Tars:Messaging:Kafka";

    /// <summary>Message reported when validation fails on application start.</summary>
    public const string ValidationErrorMessage =
        "Invalid MassTransitKafkaMessagingOptions. BootstrapServers is required; ConcurrentMessageLimit must be greater than zero.";

    /// <summary>Comma-separated broker list, e.g. <c>localhost:9092</c>.</summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// The consumer group. Defaults to <see cref="BrokerMessagingOptions.EndpointName"/>, so the same
    /// portable name means "queue" on RabbitMQ and "consumer group" here — instances sharing it
    /// compete for partitions, different values each get a full copy.
    /// </summary>
    public string? ConsumerGroup { get; set; }

    /// <summary>Where a brand-new consumer group starts reading.</summary>
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;

    /// <summary>
    /// Messages a consumer may hold at once. Keep it at 1 for long work so one slow message does not
    /// stack up behind it.
    /// </summary>
    public ushort ConcurrentMessageLimit { get; set; } = 16;

    /// <summary>
    /// Escape hatch for the Kafka factory: SASL, SSL, compression, anything not exposed here.
    /// </summary>
    public Action<IKafkaFactoryConfigurator>? ConfigureKafka { get; set; }

    /// <summary>
    /// Escape hatch for the in-memory bus the rider attaches to. MassTransit requires a bus for a
    /// rider to hang off; with Kafka as the only broker that bus carries no traffic and exists purely
    /// as a host.
    /// </summary>
    public Action<IInMemoryBusFactoryConfigurator>? ConfigureHostBus { get; set; }

    /// <summary>
    /// Returns <c>true</c> when the options are internally consistent: bootstrap servers is non-blank
    /// and concurrent message limit is positive.
    /// </summary>
    public override bool IsValid()
    {
        if (!base.IsValid())
            return false;

        if (string.IsNullOrWhiteSpace(BootstrapServers))
            return false;

        if (ConcurrentMessageLimit == 0)
            return false;

        return true;
    }
}
