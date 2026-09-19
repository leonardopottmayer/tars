using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker.Composite;
using Pottmayer.Tars.Messaging.Broker.Routing;

namespace Pottmayer.Tars.Messaging.Broker.DI;

/// <summary>
/// Registers the composite bus and its transport map: the one piece that lets an application publish to
/// several transports behind the single <see cref="IIntegrationEventBus"/> seam.
/// </summary>
/// <remarks>
/// <para>
/// This registers the <em>selector</em> (the map) and the <em>composite</em>, nothing else. Like the rest
/// of <see cref="BrokerMessagingServicesDI"/>, there is deliberately no "register everything" method here:
/// the shared broker core (registry, router, dispatcher, handlers) and the keyed inner buses are separate
/// public methods a provider calls in turn. A composition entry point such as <c>AddTarsMassTransitComposite</c>
/// wires them together — building the core over the union of its transports, then adding this composite on
/// top.
/// </para>
/// </remarks>
public static class CompositeMessagingServicesDI
{
    /// <summary>
    /// Builds the transport map from <paramref name="configure"/> and registers the composite bus over
    /// it.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Declares the default transport and any per-event overrides.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsCompositeIntegrationEventBus(
        this IServiceCollection services, Action<EventTransportMapConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new EventTransportMapConfiguration();
        configure(options);

        return services.AddTarsCompositeIntegrationEventBus(new EventTransportMap(options));
    }

    /// <summary>
    /// Registers the composite bus over an already-built <paramref name="map"/>.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="map">The event-to-transport map the composite resolves against.</param>
    /// <returns>The updated service collection.</returns>
    /// <remarks>
    /// Both registrations use <c>TryAdd</c>, so an application that wants its own selector or its own
    /// composite registers it first and this leaves it in place.
    /// </remarks>
    public static IServiceCollection AddTarsCompositeIntegrationEventBus(
        this IServiceCollection services, EventTransportMap map)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(map);

        services.TryAddSingleton<IEventTransportSelector>(map);
        services.TryAddScoped<IIntegrationEventBus, CompositeIntegrationEventBus>();

        return services;
    }
}
