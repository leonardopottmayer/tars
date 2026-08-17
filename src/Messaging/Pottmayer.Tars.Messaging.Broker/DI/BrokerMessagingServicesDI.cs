using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker.Dispatch;
using Pottmayer.Tars.Messaging.Broker.Options;
using Pottmayer.Tars.Messaging.Broker.Registry;
using Pottmayer.Tars.Messaging.Broker.Routing;

namespace Pottmayer.Tars.Messaging.Broker.DI;

/// <summary>
/// The pieces every broker provider needs, registered one at a time. There is deliberately no
/// "register everything" method: a provider composes exactly what it uses, and an application that
/// wants to replace one piece registers its own before the provider runs.
/// </summary>
/// <remarks>
/// <para>A provider registers all four:</para>
/// <code>
/// services.AddTarsIntegrationEventTypeRegistry(options.Messaging.DiscoverEventTypes());
/// services.AddTarsIntegrationEventRouter();
/// services.AddTarsIntegrationEventDispatcher();
///
/// foreach (var assembly in handlerAssemblies)
///     services.AddTarsIntegrationEventHandlers(assembly);
/// </code>
/// <para>
/// Every registration uses <c>TryAdd</c>, so registering a replacement first wins — that is how an
/// application imposes its own routing convention without forking a provider:
/// </para>
/// <code>
/// services.AddTarsIntegrationEventRouter&lt;TenantPrefixedRouter&gt;();
/// services.AddTarsMassTransitRabbitMq(o => ...);   // keeps the router above
/// </code>
/// </remarks>
public static class BrokerMessagingServicesDI
{
    /// <summary>
    /// Registers the name-to-type map used to resolve an inbound message back into an event.
    /// </summary>
    /// <remarks>
    /// Two types resolving to one logical name throw here rather than at first delivery, because the
    /// loser would silently never be delivered and which one loses is undefined.
    /// </remarks>
    public static IServiceCollection AddTarsIntegrationEventTypeRegistry(
        this IServiceCollection services, IEnumerable<Type> eventTypes)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(eventTypes);

        var materialized = eventTypes.ToArray();
        services.TryAddSingleton<IIntegrationEventTypeRegistry>(
            _ => new IntegrationEventTypeRegistry(materialized));

        return services;
    }

    /// <inheritdoc cref="AddTarsIntegrationEventTypeRegistry(IServiceCollection, IEnumerable{Type})"/>
    public static IServiceCollection AddTarsIntegrationEventTypeRegistry(
        this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        return services.AddTarsIntegrationEventTypeRegistry(
            assemblies.SelectMany(IntegrationEventTypeRegistry.DiscoverIn));
    }

    /// <summary>
    /// Registers <see cref="DefaultIntegrationEventRouter"/>: broadcast unless the event implements
    /// <see cref="IRoutedIntegrationEvent"/>, headers when it implements
    /// <see cref="IHeaderedIntegrationEvent"/>.
    /// </summary>
    public static IServiceCollection AddTarsIntegrationEventRouter(this IServiceCollection services)
        => services.AddTarsIntegrationEventRouter<DefaultIntegrationEventRouter>();

    /// <summary>
    /// Registers a custom routing rule — a tenant prefix, an environment segment — used by every
    /// provider without touching any of them.
    /// </summary>
    public static IServiceCollection AddTarsIntegrationEventRouter<TRouter>(this IServiceCollection services)
        where TRouter : class, IIntegrationEventRouter
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IIntegrationEventRouter, TRouter>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="ScopedIntegrationEventDispatcher"/> as the last mile: a fresh scope per
    /// message, handlers in registration order, first failure propagated so the transport can retry
    /// or dead-letter.
    /// </summary>
    public static IServiceCollection AddTarsIntegrationEventDispatcher(this IServiceCollection services)
        => services.AddTarsIntegrationEventDispatcher<ScopedIntegrationEventDispatcher>();

    /// <inheritdoc cref="AddTarsIntegrationEventDispatcher(IServiceCollection)"/>
    public static IServiceCollection AddTarsIntegrationEventDispatcher<TDispatcher>(this IServiceCollection services)
        where TDispatcher : class, IIntegrationEventDispatcher
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IIntegrationEventDispatcher, TDispatcher>();
        return services;
    }

    /// <summary>
    /// Scans an assembly and registers every concrete <see cref="IIntegrationEventHandler{T}"/>
    /// against its closed handler interface.
    /// </summary>
    /// <remarks>
    /// Registered with <c>TryAddEnumerable</c>, so several handlers may subscribe to one event and
    /// calling this twice for the same assembly does not duplicate them.
    /// </remarks>
    public static IServiceCollection AddTarsIntegrationEventHandlers(
        this IServiceCollection services,
        Assembly assembly,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        var handlerInterface = typeof(IIntegrationEventHandler<>);

        foreach (var type in assembly.GetExportedTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            var closedInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface);

            foreach (var closedInterface in closedInterfaces)
                services.TryAddEnumerable(new ServiceDescriptor(closedInterface, type, lifetime));
        }

        return services;
    }
}
