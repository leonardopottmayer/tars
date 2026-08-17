using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Pottmayer.Tars.Messaging.Abstractions;

namespace Pottmayer.Tars.Messaging.Broker.Registry;

/// <summary>
/// Immutable name-to-type map, built once at startup from the registered assemblies.
/// </summary>
/// <remarks>
/// Two types resolving to the same logical name is a startup failure rather than a runtime surprise:
/// the loser would silently never be delivered, and finding that out in production means reading
/// broker traffic to discover which of two classes won.
/// </remarks>
public sealed class IntegrationEventTypeRegistry : IIntegrationEventTypeRegistry
{
    private readonly Dictionary<string, Type> _byName;
    private readonly Dictionary<Type, string> _byType;

    public IntegrationEventTypeRegistry(IEnumerable<Type> eventTypes)
    {
        ArgumentNullException.ThrowIfNull(eventTypes);

        _byName = new Dictionary<string, Type>(StringComparer.Ordinal);
        _byType = [];

        foreach (var type in eventTypes.Distinct())
        {
            if (!typeof(IIntegrationEvent).IsAssignableFrom(type) || type is { IsAbstract: true } or { IsInterface: true })
                continue;

            var name = IntegrationEventNaming.For(type);

            if (_byName.TryGetValue(name, out var existing) && existing != type)
            {
                throw new InvalidOperationException(
                    $"Integration event name '{name}' is claimed by both {existing.FullName} and " +
                    $"{type.FullName}. Give one of them an explicit " +
                    $"[{nameof(IntegrationEventNameAttribute)}] — otherwise one of them would never " +
                    "be delivered, and which one is undefined.");
            }

            _byName[name] = type;
            _byType[type] = name;
        }
    }

    public IReadOnlyCollection<Type> KnownTypes => _byType.Keys;

    public bool TryResolve(string name, [NotNullWhen(true)] out Type? eventType)
        => _byName.TryGetValue(name, out eventType);

    public string NameOf(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        return _byType.TryGetValue(eventType, out var name)
            ? name
            : throw new InvalidOperationException(
                $"{eventType.FullName} is not a registered integration event. Register the assembly " +
                "that declares it so the transport can name and resolve it.");
    }

    /// <summary>
    /// Every concrete <see cref="IIntegrationEvent"/> exported by the assembly.
    /// </summary>
    public static IEnumerable<Type> DiscoverIn(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly.GetExportedTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IIntegrationEvent).IsAssignableFrom(t));
    }
}
