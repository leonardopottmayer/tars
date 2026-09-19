using Pottmayer.Tars.Messaging.Abstractions;

namespace Pottmayer.Tars.Messaging.MassTransit.Composition;

/// <summary>
/// Configures an application to run several MassTransit transports at once behind the single composite
/// bus. Transports are contributed one at a time by the provider packages (<c>AddRabbitMq</c>,
/// <c>AddKafka</c>, and later <c>AddAmazonSqs</c>…), each under a key you choose; routing then decides
/// which transport (or transports) each event is published on.
/// </summary>
/// <remarks>
/// <para>
/// This type is transport-agnostic: it holds transports as <see cref="IMassTransitTransport"/> and never
/// references a concrete broker. Each provider package adds an extension method (for example
/// <c>AddRabbitMq</c>) that builds its contributor and calls <see cref="AddTransport"/>. Adding a new
/// broker is a new provider package, not a change here.
/// </para>
/// <para>
/// The routing is the shared default-plus-overrides map, and an override can name several transports to
/// fan an event out to all of them at once.
/// </para>
/// </remarks>
public sealed class MassTransitCompositeConfiguration
{
    private readonly Dictionary<Type, IReadOnlyList<string>> _routes = [];
    private readonly List<IMassTransitTransport> _transports = [];

    /// <summary>The transport every event without an explicit route is published on.</summary>
    public string? DefaultTransport { get; set; }

    /// <summary>The per-event routing overrides declared so far.</summary>
    public IReadOnlyDictionary<Type, IReadOnlyList<string>> Routes => _routes;

    internal IReadOnlyList<IMassTransitTransport> Transports => _transports;

    /// <summary>
    /// Adds a contributed transport. Called by the provider extension methods (<c>AddRabbitMq</c>,
    /// <c>AddKafka</c>…); you can also add a custom <see cref="IMassTransitTransport"/> directly.
    /// </summary>
    /// <param name="transport">The transport contributor to add.</param>
    /// <returns>The same configuration instance, for chaining.</returns>
    /// <exception cref="InvalidOperationException">A transport with the same key was already added.</exception>
    public MassTransitCompositeConfiguration AddTransport(IMassTransitTransport transport)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentException.ThrowIfNullOrWhiteSpace(transport.Key);

        if (_transports.Any(t => string.Equals(t.Key, transport.Key, StringComparison.Ordinal)))
            throw new InvalidOperationException($"Transport key '{transport.Key}' is already used.");

        _transports.Add(transport);
        return this;
    }

    /// <summary>Routes <typeparamref name="TIntegrationEvent"/> to one or more transports (fan-out).</summary>
    /// <typeparam name="TIntegrationEvent">The event type to route.</typeparam>
    /// <param name="transports">One or more transport keys.</param>
    /// <returns>The same configuration instance, for chaining.</returns>
    public MassTransitCompositeConfiguration Route<TIntegrationEvent>(params string[] transports)
        where TIntegrationEvent : IIntegrationEvent
        => Route(typeof(TIntegrationEvent), transports);

    /// <summary>Routes <paramref name="eventType"/> to one or more transports (fan-out).</summary>
    /// <param name="eventType">The event type to route.</param>
    /// <param name="transports">One or more transport keys.</param>
    /// <returns>The same configuration instance, for chaining.</returns>
    public MassTransitCompositeConfiguration Route(Type eventType, params string[] transports)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(transports);

        if (transports.Length == 0)
            throw new ArgumentException("At least one transport must be given.", nameof(transports));

        var distinct = transports.Distinct(StringComparer.Ordinal).ToArray();

        if (_routes.TryGetValue(eventType, out var existing) && !existing.SequenceEqual(distinct, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"{eventType.Name} is already routed to [{string.Join(", ", existing)}] and cannot also " +
                $"be routed to [{string.Join(", ", distinct)}]. Declare all of an event's transports in " +
                "one Route call.");
        }

        _routes[eventType] = distinct;
        return this;
    }
}
