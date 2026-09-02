namespace Pottmayer.Tars.Messaging.Abstractions;

/// <summary>
/// Optional stable, transport-level name for an integration event (e.g. <c>identity.account-activation.v1</c>).
/// A broker routes by this logical name, not by the .NET type, so two services can exchange the event
/// without sharing the .NET contract assembly. Ignored by the in-process transport, which dispatches by type.
/// </summary>
/// <param name="name">The logical event name used for routing and versioning across the wire.</param>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IntegrationEventNameAttribute(string name) : Attribute
{
    /// <summary>The logical event name used for routing and versioning across the wire.</summary>
    public string Name { get; } = name;

    /// <summary>
    /// The schema version of the event's payload, defaulting to <c>1</c>. Kept as a first-class value
    /// (not folded into <see cref="Name"/>) so the name stays the stable identity of the fact while the
    /// version tracks the <em>shape</em> of the body. A durable transport — the outbox — stores it in a
    /// dedicated column, which is what lets a consumer pick the right deserializer (and, later, an
    /// upcaster) before it ever opens the payload, and lets you query or gate delivery by version.
    /// </summary>
    /// <remarks>
    /// Bump this when the payload changes in a way old consumers cannot read. The usual move is a new
    /// type (<c>...V2</c>) carrying <c>Version = 2</c>, registered alongside the old one.
    /// </remarks>
    public int Version { get; init; } = 1;
}
