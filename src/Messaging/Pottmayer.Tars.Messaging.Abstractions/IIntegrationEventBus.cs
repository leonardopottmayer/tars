namespace Pottmayer.Tars.Messaging.Abstractions;

/// <summary>
/// Publishes integration events to interested consumers.
/// The seam that hides the transport: an in-process dispatch today, a broker (RabbitMQ, Kafka, ...)
/// tomorrow, with no change to producers or consumers.
/// </summary>
public interface IIntegrationEventBus
{
    /// <summary>
    /// Publishes an integration event. Producers call this after their unit of work has committed.
    /// </summary>
    Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default);
}
