using Pottmayer.Tars.Core.Mediator.Abstractions.Notifications;

namespace Pottmayer.Tars.Core.Mediator.Abstractions;

/// <summary>
/// Publishes a notification to all registered handlers.
/// Handlers are invoked in parallel (or sequentially, depending on implementation); no response is returned.
/// </summary>
public interface IPublisher
{
    /// <summary>
    /// Publishes a notification to all registered handlers.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask Publish(INotification notification, CancellationToken cancellationToken = default);
}
