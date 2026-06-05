namespace Pottmayer.Tars.Messaging.Abstractions;

/// <summary>
/// Optional stable, transport-level name for an integration event (e.g. <c>identity.account-activation.v1</c>).
/// A broker routes by this logical name, not by the .NET type, so two services can exchange the event
/// without sharing the .NET contract assembly. Ignored by the in-process transport, which dispatches by type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class IntegrationEventNameAttribute(string name) : Attribute
{
    /// <summary>The logical event name used for routing and versioning across the wire.</summary>
    public string Name { get; } = name;
}
