using Pottmayer.Tars.Messaging.Abstractions;

namespace Pottmayer.Tars.Messaging.Broker.Routing;

/// <summary>
/// Declares which transport each event is published on, for an application that runs more than one.
/// A default covers everything not named, and per-event overrides move the exceptions.
/// </summary>
/// <remarks>
/// <para>
/// The shape is deliberately "one default, few exceptions", because that is how these systems are
/// built in practice: a backbone transport carries almost everything (Kafka, say), and a handful of
/// event types are peeled off onto a work queue (RabbitMQ). Listing every event would be noise; the
/// default keeps the declaration to just the interesting cases.
/// </para>
/// <para>
/// Keys are opaque strings that match keyed <see cref="IIntegrationEventBus"/> registrations. They
/// are the same keys the composite bus resolves against, so "kafka" here must be the key a Kafka bus
/// was registered under.
/// </para>
/// </remarks>
public sealed class EventTransportMapConfiguration
{
    private readonly Dictionary<Type, IReadOnlyList<string>> _overrides = [];

    /// <summary>
    /// The transport every event is published on unless an override moves it. Required: a map with no
    /// default cannot answer for an event nobody listed, and silently dropping such an event is the
    /// failure this design exists to avoid.
    /// </summary>
    public string? DefaultTransport { get; set; }

    /// <summary>The per-event overrides declared so far, keyed by event type.</summary>
    public IReadOnlyDictionary<Type, IReadOnlyList<string>> Overrides => _overrides;

    /// <summary>
    /// Publishes <typeparamref name="TIntegrationEvent"/> on the given <paramref name="transports"/>
    /// instead of the default. One transport is the usual case; several fan the event out to every one
    /// of them.
    /// </summary>
    /// <typeparam name="TIntegrationEvent">The event type to move.</typeparam>
    /// <param name="transports">One or more transport keys to publish it on.</param>
    /// <returns>The same configuration instance, for chaining.</returns>
    public EventTransportMapConfiguration Route<TIntegrationEvent>(params string[] transports)
        where TIntegrationEvent : IIntegrationEvent
        => Route(typeof(TIntegrationEvent), transports);

    /// <summary>
    /// Publishes <paramref name="eventType"/> on the given <paramref name="transports"/> instead of the
    /// default.
    /// </summary>
    /// <param name="eventType">The event type to move.</param>
    /// <param name="transports">One or more transport keys to publish it on.</param>
    /// <returns>The same configuration instance, for chaining.</returns>
    /// <exception cref="ArgumentException">No transport is given, or one is null or blank.</exception>
    /// <exception cref="InvalidOperationException">The same event type is routed twice, to a different set.</exception>
    public EventTransportMapConfiguration Route(Type eventType, params string[] transports)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(transports);

        if (transports.Length == 0)
            throw new ArgumentException("At least one transport must be given.", nameof(transports));

        foreach (var transport in transports)
            ArgumentException.ThrowIfNullOrWhiteSpace(transport, nameof(transports));

        var distinct = transports.Distinct(StringComparer.Ordinal).ToArray();

        if (_overrides.TryGetValue(eventType, out var existing) && !existing.SequenceEqual(distinct, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"{eventType.Name} is already routed to [{string.Join(", ", existing)}] and cannot also " +
                $"be routed to [{string.Join(", ", distinct)}]. Declare all of an event's transports in " +
                "one Route call.");
        }

        _overrides[eventType] = distinct;
        return this;
    }
}
