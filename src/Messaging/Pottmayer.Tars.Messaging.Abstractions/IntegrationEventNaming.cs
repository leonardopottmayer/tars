using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace Pottmayer.Tars.Messaging.Abstractions;

/// <summary>
/// Resolves the logical, transport-level name of an integration event. A broker routes by this name
/// rather than by the .NET type, which is what lets two services exchange an event without sharing
/// the contract assembly.
/// </summary>
public static class IntegrationEventNaming
{
    private static readonly ConcurrentDictionary<Type, string> Cache = new();

    /// <summary>
    /// Returns <see cref="IntegrationEventNameAttribute.Name"/> when the type declares it, and
    /// otherwise a deterministic kebab-case form of the type name
    /// (<c>PasswordResetRequested</c> becomes <c>password-reset-requested</c>).
    /// </summary>
    /// <remarks>
    /// The fallback keeps a broker transport usable without ceremony, but it ties the wire name to a
    /// .NET identifier: renaming the class silently renames the route, and a consumer in another
    /// service stops receiving. Declare the attribute on anything that crosses a service boundary or
    /// that you intend to version.
    /// </remarks>
    public static string For(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        return Cache.GetOrAdd(eventType, static type =>
            type.GetCustomAttribute<IntegrationEventNameAttribute>(inherit: false)?.Name
            ?? ToKebabCase(type.Name));
    }

    /// <inheritdoc cref="For(Type)"/>
    public static string For<TIntegrationEvent>()
        where TIntegrationEvent : IIntegrationEvent
        => For(typeof(TIntegrationEvent));

    /// <summary>
    /// Returns <see cref="IntegrationEventNameAttribute.Version"/> when the type declares it, and
    /// <c>1</c> otherwise. This is the payload schema version a durable transport records so a consumer
    /// can resolve the right shape before deserializing.
    /// </summary>
    public static int VersionFor(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return eventType.GetCustomAttribute<IntegrationEventNameAttribute>(inherit: false)?.Version ?? 1;
    }

    /// <inheritdoc cref="VersionFor(Type)"/>
    public static int VersionFor<TIntegrationEvent>()
        where TIntegrationEvent : IIntegrationEvent
        => VersionFor(typeof(TIntegrationEvent));

    /// <summary>True when the type declares an explicit name rather than falling back to convention.</summary>
    public static bool IsExplicit(Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return eventType.GetCustomAttribute<IntegrationEventNameAttribute>(inherit: false) is not null;
    }

    private static string ToKebabCase(string name)
    {
        var builder = new StringBuilder(name.Length + 8);

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            // A hyphen goes before an upper-case letter that starts a new word, so "MFAEnabled"
            // becomes "mfa-enabled" rather than "m-f-a-enabled".
            if (char.IsUpper(c) && i > 0 && (!char.IsUpper(name[i - 1]) || IsStartOfNextWord(name, i)))
                builder.Append('-');

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    private static bool IsStartOfNextWord(string name, int index)
        => index + 1 < name.Length && char.IsLower(name[index + 1]);
}
