namespace Pottmayer.Tars.Messaging.Abstractions;

/// <summary>
/// Opt-in marker for an event that carries its own destination. Implement it when the event itself
/// knows who it is for — a button press belongs to the module that asked for the button — and leave
/// it off when it does not, which is the common case.
/// </summary>
/// <remarks>
/// <para>
/// The choice is a property of the <em>event</em>, not of the application. "Password was reset" has
/// nobody in particular to route to: whoever cares subscribes. "Button was pressed" has an owner
/// written on it. A single application normally has both kinds.
/// </para>
/// <para>
/// Without this interface an event is <strong>broadcast</strong>: every subscriber receives it.
/// With it, the event is <strong>keyed</strong> and only subscribers whose pattern matches receive
/// it — the transport-level equivalent of RabbitMQ's direct/topic exchanges, Kafka's per-topic
/// subscription, or Azure Service Bus subscription filters.
/// </para>
/// <para>
/// Changing an event from broadcast to keyed (or back) after it is in production is a topology
/// migration, not a toggle: it changes where the event is published, so a half-deployed fleet stops
/// seeing itself. Publish to both for a window, move consumers, then retire the old route.
/// </para>
/// </remarks>
public interface IRoutedIntegrationEvent
{
    /// <summary>
    /// Appended to the event's logical name to form the routing key, so an event named
    /// <c>inbound.interaction</c> returning <c>agenda.task_done</c> is published as
    /// <c>inbound.interaction.agenda.task_done</c>.
    /// </summary>
    /// <remarks>
    /// Keep it made of dot-separated segments, lowercase, from a small closed vocabulary the
    /// publisher owns. Never build it from free text or user input: it is a routing address, and a
    /// segment containing a space or a wildcard silently stops matching.
    /// </remarks>
    string RoutingKeySuffix { get; }
}
