namespace Pottmayer.Tars.Core.Mediator.Abstractions.Notifications;

/// <summary>
/// Defines a handler for a notification of type <typeparamref name="TNotification"/>.
/// Multiple handlers can be registered for the same notification; all are invoked when the notification is published.
/// </summary>
/// <typeparam name="TNotification">The type of the notification.</typeparam>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Handles the notification asynchronously.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask Handle(TNotification notification, CancellationToken cancellationToken = default);
}
