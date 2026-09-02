using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Pottmayer.Tars.Messaging.DI;

/// <summary>
/// Options for registering the in-process integration event bus: which assemblies to scan for
/// <see cref="Abstractions.IIntegrationEventHandler{T}"/> implementations.
/// </summary>
public sealed class IntegrationEventBusConfiguration
{
    private readonly List<(Assembly Assembly, ServiceLifetime Lifetime)> _handlerAssemblies = [];

    internal IReadOnlyList<(Assembly Assembly, ServiceLifetime Lifetime)> HandlerAssemblies => _handlerAssemblies;

    /// <summary>
    /// Registers the <see cref="Abstractions.IIntegrationEventHandler{T}"/> implementations found in
    /// the given assembly for scanning at build time.
    /// </summary>
    /// <param name="assembly">The assembly to scan for handler implementations.</param>
    /// <param name="lifetime">The service lifetime the discovered handlers are registered with.</param>
    /// <returns>The same options instance, for chaining.</returns>
    public IntegrationEventBusConfiguration RegisterHandlersFromAssembly(
        Assembly assembly, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        _handlerAssemblies.Add((assembly, lifetime));
        return this;
    }

    /// <summary>
    /// Registers the <see cref="Abstractions.IIntegrationEventHandler{T}"/> implementations found in
    /// the given assemblies for scanning at build time.
    /// </summary>
    /// <param name="lifetime">The service lifetime the discovered handlers are registered with.</param>
    /// <param name="assemblies">The assemblies to scan for handler implementations.</param>
    /// <returns>The same options instance, for chaining.</returns>
    public IntegrationEventBusConfiguration RegisterHandlersFromAssemblies(
        ServiceLifetime lifetime = ServiceLifetime.Scoped, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
            _handlerAssemblies.Add((assembly, lifetime));
        return this;
    }
}
