namespace Pottmayer.Tars.Messaging.Broker;

/// <summary>
/// What a broker can actually do. Providers declare theirs so a topology the broker cannot honour
/// fails at startup with a readable message instead of silently delivering nothing in production.
/// </summary>
/// <remarks>
/// The gaps are real and not worth papering over. Kafka has no broker-side header filtering, so
/// <see cref="HeaderRouting"/> cannot be emulated — only moved into the consumer, which means every
/// consumer receives everything and the filtering stops being routing at all.
/// </remarks>
[Flags]
public enum BrokerCapabilities
{
    /// <summary>No capabilities. The default before a provider declares what it supports.</summary>
    None = 0,

    /// <summary>Every subscriber receives the message. RabbitMQ fanout, Kafka consumer groups.</summary>
    Broadcast = 1 << 0,

    /// <summary>Exact-match delivery by routing key. RabbitMQ direct, a Kafka topic name.</summary>
    KeyedRouting = 1 << 1,

    /// <summary>Pattern delivery by routing key. RabbitMQ topic, Kafka regex subscription.</summary>
    WildcardRouting = 1 << 2,

    /// <summary>
    /// Delivery decided by header values. RabbitMQ headers exchange, Azure Service Bus and SNS
    /// filters. <strong>Kafka has no equivalent.</strong>
    /// </summary>
    HeaderRouting = 1 << 3,

    /// <summary>A transactional outbox is available through the underlying framework.</summary>
    Outbox = 1 << 4,

    /// <summary>What an AMQP-style broker offers: every routing shape, plus an outbox.</summary>
    Amqp = Broadcast | KeyedRouting | WildcardRouting | HeaderRouting | Outbox,

    /// <summary>
    /// What a log-style broker offers. Deliberately just broadcast: a Kafka consumer subscribes to a
    /// <em>topic</em>, and the topic is fixed per event type at registration time — neither
    /// MassTransit nor Silverback exposes a dynamic-topic producer. So a per-message routing key has
    /// nowhere to live but the message key and headers, and the broker cannot filter on either.
    /// Routing on Kafka means every subscriber reads everything and discards what is not theirs,
    /// which is not routing.
    /// </summary>
    Log = Broadcast | Outbox,
}
