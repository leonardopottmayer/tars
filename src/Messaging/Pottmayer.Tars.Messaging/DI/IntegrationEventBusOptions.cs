using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Pottmayer.Tars.Messaging.DI;

/// <summary>
/// Options for registering the in-process integration event bus: which assemblies to scan for
/// <see cref="Abstractions.IIntegrationEventHandler{T}"/> implementations.
/// </summary>
public sealed class IntegrationEventBusOptions
{
    private readonly List<(Assembly Assembly, ServiceLifetime Lifetime)> _handlerAssemblies = [];

    internal IReadOnlyList<(Assembly Assembly, ServiceLifetime Lifetime)> HandlerAssemblies => _handlerAssemblies;

    public IntegrationEventBusOptions RegisterHandlersFromAssembly(
        Assembly assembly, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        _handlerAssemblies.Add((assembly, lifetime));
        return this;
    }

    public IntegrationEventBusOptions RegisterHandlersFromAssemblies(
        ServiceLifetime lifetime = ServiceLifetime.Scoped, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
            _handlerAssemblies.Add((assembly, lifetime));
        return this;
    }
}
