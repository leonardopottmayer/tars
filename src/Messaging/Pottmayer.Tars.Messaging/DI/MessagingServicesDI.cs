using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Messaging.Abstractions;

namespace Pottmayer.Tars.Messaging.DI;

public static class MessagingServicesDI
{
    /// <summary>
    /// Registers the in-process <see cref="IIntegrationEventBus"/> and, optionally, the
    /// <see cref="IIntegrationEventHandler{T}"/> implementations found in the configured assemblies.
    /// </summary>
    public static IServiceCollection AddTarsInProcessIntegrationEventBus(
        this IServiceCollection services,
        Action<IntegrationEventBusOptions>? configure = null)
    {
        services.TryAddSingleton<IIntegrationEventBus, InProcessIntegrationEventBus>();

        var options = new IntegrationEventBusOptions();
        configure?.Invoke(options);

        foreach (var (assembly, lifetime) in options.HandlerAssemblies)
            services.AddIntegrationEventHandlersFromAssembly(assembly, lifetime);

        return services;
    }

    /// <summary>
    /// Scans an assembly and registers every concrete <see cref="IIntegrationEventHandler{T}"/>
    /// against its closed handler interface.
    /// </summary>
    public static IServiceCollection AddIntegrationEventHandlersFromAssembly(
        this IServiceCollection services,
        Assembly assembly,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        var handlerInterface = typeof(IIntegrationEventHandler<>);

        var types = assembly.GetExportedTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false });

        foreach (var type in types)
        {
            var closedInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface);

            foreach (var closedInterface in closedInterfaces)
                services.Add(new ServiceDescriptor(closedInterface, type, lifetime));
        }

        return services;
    }
}
