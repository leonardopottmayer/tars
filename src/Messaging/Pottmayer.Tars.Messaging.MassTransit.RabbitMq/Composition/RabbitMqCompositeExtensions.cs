using MassTransit;
using Pottmayer.Tars.Messaging.MassTransit.Composition;
using Pottmayer.Tars.Messaging.MassTransit.RabbitMq.Options;

namespace Pottmayer.Tars.Messaging.MassTransit.RabbitMq.Composition;

/// <summary>
/// Adds a RabbitMQ transport to a composite (multi-transport) MassTransit application. This is the
/// RabbitMQ half of <c>AddTarsMassTransitComposite</c>: the composite orchestrator stays transport-
/// agnostic, and referencing this package is what brings RabbitMQ into the mix — an application that does
/// not use RabbitMQ never references it.
/// </summary>
public static class RabbitMqCompositeExtensions
{
    /// <summary>
    /// Adds a RabbitMQ transport under <paramref name="key"/> as the composite's primary bus.
    /// </summary>
    /// <param name="configuration">The composite configuration.</param>
    /// <param name="key">The transport key routes refer to and the composite resolves against.</param>
    /// <param name="configure">Configures the RabbitMQ connection, topology and subscriptions.</param>
    /// <returns>The same configuration instance, for chaining.</returns>
    public static MassTransitCompositeConfiguration AddRabbitMq(
        this MassTransitCompositeConfiguration configuration, string key, Action<MassTransitRabbitMqMessagingOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new MassTransitRabbitMqMessagingOptions();
        configure(options);

        return configuration.AddTransport(new RabbitMqBusTransport(key, options));
    }

    /// <summary>
    /// Adds a RabbitMQ transport under <paramref name="key"/> as its own MassTransit bus, identified by the
    /// marker interface <typeparamref name="TBus"/>. This is how a second (or third…) non-rider transport
    /// runs in one process: MassTransit multibus gives each bus a distinct type identity, and
    /// <typeparamref name="TBus"/> is that identity.
    /// </summary>
    /// <typeparam name="TBus">
    /// A marker interface you declare, e.g. <c>public interface IAuditBus : IBus { }</c>. It is the
    /// compile-time type MassTransit resolves this bus's publish endpoint under.
    /// </typeparam>
    /// <param name="configuration">The composite configuration.</param>
    /// <param name="key">The transport key routes refer to and the composite resolves against.</param>
    /// <param name="configure">Configures the RabbitMQ connection, topology and subscriptions.</param>
    /// <returns>The same configuration instance, for chaining.</returns>
    /// <remarks>
    /// The first RabbitMQ added with the plain
    /// <see cref="AddRabbitMq(MassTransitCompositeConfiguration, string, Action{MassTransitRabbitMqMessagingOptions})"/>
    /// is the primary bus; every additional non-rider transport needs its own marker here. The composite
    /// and the map treat it like any other keyed transport — only the physical bus wiring differs.
    /// </remarks>
    public static MassTransitCompositeConfiguration AddRabbitMq<TBus>(
        this MassTransitCompositeConfiguration configuration, string key, Action<MassTransitRabbitMqMessagingOptions> configure)
        where TBus : class, IBus
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new MassTransitRabbitMqMessagingOptions();
        configure(options);

        return configuration.AddTransport(new RabbitMqMultibusTransport<TBus>(key, options));
    }
}
