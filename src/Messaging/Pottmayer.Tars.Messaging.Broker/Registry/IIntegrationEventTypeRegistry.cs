using System.Diagnostics.CodeAnalysis;

namespace Pottmayer.Tars.Messaging.Broker.Registry;

/// <summary>
/// Maps a logical event name back to the .NET type to deserialize into. A broker delivers bytes and
/// a name; this is what turns them back into an event before the last mile.
/// </summary>
public interface IIntegrationEventTypeRegistry
{
    /// <summary>Every event type known to this application, publishable or consumable.</summary>
    IReadOnlyCollection<Type> KnownTypes { get; }

    /// <summary>Resolves a logical event name back to its .NET type.</summary>
    /// <param name="name">The logical event name delivered by the broker.</param>
    /// <param name="eventType">The resolved type when the name is known; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> when the name is registered; otherwise <c>false</c>.</returns>
    bool TryResolve(string name, [NotNullWhen(true)] out Type? eventType);

    /// <summary>The logical name registered for a type.</summary>
    /// <exception cref="InvalidOperationException">The type was never registered.</exception>
    string NameOf(Type eventType);
}
