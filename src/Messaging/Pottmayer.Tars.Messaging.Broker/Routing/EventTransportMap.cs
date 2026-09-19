namespace Pottmayer.Tars.Messaging.Broker.Routing;

/// <summary>
/// The immutable event-to-transport map, built once at startup from
/// <see cref="EventTransportMapConfiguration"/>. A default answers for everything, overrides win where they
/// exist.
/// </summary>
/// <remarks>
/// An unlisted event is not an error: it takes the default. That is the point of having a default —
/// the map only names the exceptions, and adding a new event that belongs on the backbone needs no
/// change here.
/// </remarks>
public sealed class EventTransportMap : IEventTransportSelector
{
    private readonly IReadOnlyList<string> _default;
    private readonly IReadOnlyDictionary<Type, IReadOnlyList<string>> _overrides;

    /// <summary>
    /// Builds the map from configured options, failing fast when the default is missing.
    /// </summary>
    /// <param name="options">The declared default and overrides.</param>
    /// <exception cref="InvalidOperationException">No default transport was set.</exception>
    public EventTransportMap(EventTransportMapConfiguration options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.DefaultTransport))
        {
            throw new InvalidOperationException(
                "EventTransportMap needs a DefaultTransport: it is the transport every event without " +
                "an explicit route is published on. Set options.DefaultTransport to one of the " +
                "registered transport keys.");
        }

        _default = [options.DefaultTransport];
        _overrides = new Dictionary<Type, IReadOnlyList<string>>(options.Overrides);
    }

    /// <summary>The transport used for any event that has no explicit override.</summary>
    public string DefaultTransport => _default[0];

    /// <summary>Every distinct transport key this map can return: the default plus each override target.</summary>
    public IReadOnlyCollection<string> ConfiguredTransports =>
        _overrides.Values.SelectMany(t => t).Append(DefaultTransport).Distinct(StringComparer.Ordinal).ToArray();

    /// <summary>The per-event overrides, keyed by event type.</summary>
    public IReadOnlyDictionary<Type, IReadOnlyList<string>> Overrides => _overrides;

    /// <inheritdoc />
    public IReadOnlyList<string> SelectFor(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        return _overrides.TryGetValue(eventType, out var transports) ? transports : _default;
    }
}
