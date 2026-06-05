namespace Pottmayer.Tars.Messaging.Abstractions;

/// <summary>
/// Handles an integration event of type <typeparamref name="TIntegrationEvent"/>.
/// Multiple handlers may subscribe to the same event. With a broker transport, the consumer
/// re-dispatches the deserialized message to these handlers (the "last mile").
/// </summary>
public interface IIntegrationEventHandler<in TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    Task HandleAsync(TIntegrationEvent @event, CancellationToken cancellationToken = default);
}
