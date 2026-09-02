using MassTransit;
using RabbitMQ.Client;
using Pottmayer.Tars.Messaging.MassTransit.Options;

namespace Pottmayer.Tars.Messaging.MassTransit.RabbitMq.Options;

/// <summary>
/// RabbitMQ connection and topology, plus the portable subscription model shared by every provider.
/// </summary>
public sealed class MassTransitRabbitMqMessagingOptions : MassTransitMessagingOptions
{
    /// <summary>Default configuration section these options bind from (<c>Tars:Messaging:RabbitMq</c>).</summary>
    public const string SectionName = "Tars:Messaging:RabbitMq";

    /// <summary>Message reported when validation fails on application start.</summary>
    public const string ValidationErrorMessage =
        "Invalid MassTransitRabbitMqMessagingOptions. Host, VirtualHost, Username, Password, and RoutedExchangeType are required; Port, RetryInterval, and PrefetchCount must be positive; RetryLimit must be non-negative.";

    /// <summary>RabbitMQ server hostname or IP address.</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>RabbitMQ server AMQP port (default 5672).</summary>
    public ushort Port { get; set; } = 5672;

    /// <summary>Virtual host path (default "/").</summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>Username for authentication.</summary>
    public string Username { get; set; } = "guest";

    /// <summary>Password for authentication.</summary>
    public string Password { get; set; } = "guest";

    /// <summary>Whether to connect using SSL/TLS.</summary>
    public bool UseSsl { get; set; }

    /// <summary>
    /// Exchange type used for events that carry a routing key. <c>Topic</c> allows wildcard bindings
    /// (<c>agenda.#</c>); <c>Direct</c> requires an exact match and is marginally cheaper.
    /// </summary>
    /// <remarks>
    /// Broadcast events ignore this and use a fanout exchange, because they have no key to match.
    /// Setting <c>Headers</c> here routes by header values instead — available on RabbitMQ, and the
    /// one topology that has no Kafka equivalent, so an application using it cannot later move to
    /// Kafka unchanged.
    /// </remarks>
    public string RoutedExchangeType { get; set; } = ExchangeType.Topic;

    /// <summary>
    /// Attempts before a message is moved to the error queue. MassTransit owns the retry; this is
    /// only how it is configured.
    /// </summary>
    public int RetryLimit { get; set; } = 3;

    /// <summary>Delay between retries.</summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Messages a consumer may hold at once. Keep it at 1 for long work — a transcription or an LLM
    /// call — so one slow message does not stack up behind it.
    /// </summary>
    public ushort PrefetchCount { get; set; } = 16;

    /// <summary>Escape hatch for anything this options class does not expose.</summary>
    public Action<IRabbitMqBusFactoryConfigurator>? ConfigureBus { get; set; }

    /// <summary>Escape hatch for the receive endpoint, e.g. concurrency or a custom retry policy.</summary>
    public Action<IRabbitMqReceiveEndpointConfigurator>? ConfigureEndpoint { get; set; }

    /// <summary>
    /// Returns <c>true</c> when the options are internally consistent: host, vhost, credentials, and exchange type
    /// are non-blank, port and prefetch count are positive, retry interval is positive, and retry limit is non-negative.
    /// </summary>
    public override bool IsValid()
    {
        if (!base.IsValid())
            return false;

        if (string.IsNullOrWhiteSpace(Host))
            return false;

        if (Port == 0)
            return false;

        if (string.IsNullOrWhiteSpace(VirtualHost))
            return false;

        if (string.IsNullOrWhiteSpace(Username))
            return false;

        if (string.IsNullOrWhiteSpace(Password))
            return false;

        if (string.IsNullOrWhiteSpace(RoutedExchangeType))
            return false;

        if (RetryLimit < 0)
            return false;

        if (RetryInterval <= TimeSpan.Zero)
            return false;

        if (PrefetchCount == 0)
            return false;

        return true;
    }
}
