namespace Pottmayer.Tars.Messaging.Abstractions;

/// <summary>
/// Handles an integration event of type <typeparamref name="TIntegrationEvent"/>.
/// Multiple handlers may subscribe to the same event. With a broker transport, the consumer
/// re-dispatches the deserialized message to these handlers (the "last mile").
/// </summary>
/// <typeparam name="TIntegrationEvent">The integration event type this handler reacts to.</typeparam>
public interface IIntegrationEventHandler<in TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    /// <summary>Handles the integration event.</summary>
    /// <param name="event">The event to handle.</param>
    /// <param name="cancellationToken">Cancels handling.</param>
    /// <returns>A task that completes when the event has been handled.</returns>
    Task HandleAsync(TIntegrationEvent @event, CancellationToken cancellationToken = default);
}
