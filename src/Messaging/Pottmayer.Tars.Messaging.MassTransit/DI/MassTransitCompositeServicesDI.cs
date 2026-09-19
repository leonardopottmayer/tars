using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker.DI;
using Pottmayer.Tars.Messaging.Broker.Routing;
using Pottmayer.Tars.Messaging.MassTransit.Composition;

namespace Pottmayer.Tars.Messaging.MassTransit.DI;

/// <summary>
/// Registers an application to run several MassTransit transports at once behind the single
/// <see cref="IIntegrationEventBus"/> seam: the shared broker core once over the union of the declared
/// transports, a keyed bus per transport, the composite that routes each event to the right one (or
/// several, for fan-out), and the physical MassTransit wiring.
/// </summary>
/// <remarks>
/// <para>
/// This orchestrator is transport-agnostic — it composes whatever <see cref="IMassTransitTransport"/>
/// contributors the provider packages added, and never references a concrete broker. The physical
/// MassTransit wiring in one process is bounded: one <em>primary</em> bus plus riders per
/// <c>AddMassTransit</c>, and every additional full transport is a <em>multibus</em> bus behind its own
/// marker interface. The orchestrator enforces that shape; each transport contributes the technology-
/// specific parts.
/// </para>
/// </remarks>
public static class MassTransitCompositeServicesDI
{
    /// <summary>
    /// Registers the composite multi-transport application from <paramref name="configure"/>.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Declares the transports (via provider extensions) and the routing.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddTarsMassTransitComposite(
        this IServiceCollection services, Action<MassTransitCompositeConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var configuration = new MassTransitCompositeConfiguration();
        configure(configuration);

        return services.AddTarsMassTransitComposite(configuration);
    }

    private static IServiceCollection AddTarsMassTransitComposite(
        this IServiceCollection services, MassTransitCompositeConfiguration configuration)
    {
        var transports = configuration.Transports;

        if (transports.Count == 0)
            throw new InvalidOperationException("Add at least one transport (for example AddRabbitMq or AddKafka).");

        var buses = transports.OfType<IMassTransitBusTransport>().ToList();
        var riders = transports.OfType<IMassTransitRiderTransport>().ToList();
        var primaryCandidates = buses.Where(b => b.IsPrimaryCandidate).ToList();
        var multibuses = buses.Where(b => !b.IsPrimaryCandidate).ToList();

        if (primaryCandidates.Count > 1)
        {
            var names = string.Join(", ", primaryCandidates.Select(b => $"'{b.Key}' ({b.TransportName})"));
            throw new InvalidOperationException(
                $"More than one transport wants to be the primary bus ({names}). Only one MassTransit bus " +
                "can be primary in a process; give the others a marker interface (for example " +
                "AddRabbitMq<TBus>) so each becomes its own multibus bus. See docs/messaging/multi-transport.md.");
        }

        var duplicateRider = riders.GroupBy(r => r.GetType()).FirstOrDefault(g => g.Count() > 1);
        if (duplicateRider is not null)
        {
            throw new InvalidOperationException(
                $"More than one {duplicateRider.First().TransportName} rider was added; a MassTransit rider " +
                "of a given transport can only be registered once per process.");
        }

        var primary = primaryCandidates.SingleOrDefault();

        // Fail fast per transport (its own options and capability matrix), before wiring anything.
        foreach (var transport in transports)
            transport.Validate();

        // The transport-agnostic composition, from the granular Broker pieces: the shared core once over
        // the union of every transport, then the composite over the routing map. Validation fails fast on
        // an unknown transport or an event routed to a broker it has no producer on.
        var map = new EventTransportMap(BuildTransportMap(configuration));
        ValidateComposition(map, transports);

        services.AddTarsBrokerCoreForUnion(transports);
        services.AddTarsCompositeIntegrationEventBus(map);

        // Each transport registers its own keyed bus (the composite resolves these by key).
        foreach (var transport in transports)
            transport.RegisterKeyedBus(services);

        // One AddMassTransit for the primary bus and its riders (or an in-memory host when a rider is the
        // only transport). Each additional full transport is its own multibus bus.
        if (primary is not null || riders.Count > 0)
            services.AddPrimaryBus(primary, riders);

        foreach (var multibus in multibuses)
            multibus.RegisterMultibus(services);

        return services;
    }

    private static IServiceCollection AddPrimaryBus(
        this IServiceCollection services, IMassTransitBusTransport? primary, IReadOnlyList<IMassTransitRiderTransport> riders)
    {
        services.AddMassTransit(bus =>
        {
            primary?.ConfigurePrimary(bus);

            foreach (var rider in riders)
                rider.AttachRider(bus);

            if (primary is not null)
                primary.UsePrimaryBus(bus);
            else
                // A rider needs some bus to hang off; an in-memory one carries no traffic of its own.
                bus.UsingInMemory((_, cfg) =>
                {
                    foreach (var rider in riders)
                        rider.ConfigureStandaloneHost(cfg);
                });
        });

        return services;
    }

    private static EventTransportMapConfiguration BuildTransportMap(MassTransitCompositeConfiguration configuration)
    {
        var map = new EventTransportMapConfiguration { DefaultTransport = configuration.DefaultTransport };

        foreach (var (type, keys) in configuration.Routes)
            map.Route(type, [.. keys]);

        return map;
    }

    /// <summary>
    /// Registers the shared broker core once over the union of every declared transport's events and
    /// handlers — the same four <see cref="BrokerMessagingServicesDI"/> pieces every provider registers,
    /// here across all transports so consuming from any of them hands off to the one dispatcher.
    /// </summary>
    private static IServiceCollection AddTarsBrokerCoreForUnion(
        this IServiceCollection services, IReadOnlyCollection<IMassTransitTransport> transports)
    {
        var messaging = transports.Select(t => t.Messaging).ToList();

        services.AddTarsIntegrationEventTypeRegistry(messaging.SelectMany(m => m.DiscoverEventTypes()).Distinct());
        services.AddTarsIntegrationEventRouter();
        services.AddTarsIntegrationEventDispatcher();

        foreach (var (assembly, lifetime) in messaging.SelectMany(m => m.HandlerAssemblies).Distinct())
            services.AddTarsIntegrationEventHandlers(assembly, lifetime);

        return services;
    }

    /// <summary>
    /// Fails fast on the two routing mistakes that would otherwise surface only at the first publish, in
    /// production: a route to a transport that was never declared, and an event routed to a transport it
    /// registers no producer for.
    /// </summary>
    private static void ValidateComposition(EventTransportMap map, IReadOnlyCollection<IMassTransitTransport> transports)
    {
        var declared = transports.Select(t => t.Key).ToHashSet(StringComparer.Ordinal);

        foreach (var transport in map.ConfiguredTransports)
        {
            if (!declared.Contains(transport))
            {
                throw new InvalidOperationException(
                    $"The transport map references '{transport}', which is not a declared transport. " +
                    "Declare it with AddRabbitMq or AddKafka, or fix the route.");
            }
        }

        var typesByTransport = transports.ToDictionary(
            t => t.Key,
            t => t.Messaging.DiscoverEventTypes().ToHashSet(),
            StringComparer.Ordinal);

        foreach (var (type, targets) in map.Overrides)
        {
            foreach (var target in targets)
            {
                if (!typesByTransport[target].Contains(type))
                {
                    throw new InvalidOperationException(
                        $"{type.Name} is routed to '{target}', but it is not registered as an event on " +
                        "that transport, so no producer would exist for it. Register its assembly on the " +
                        $"'{target}' transport.");
                }
            }
        }
    }
}
