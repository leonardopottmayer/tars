using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pottmayer.Tars.Messaging.Abstractions;

namespace Pottmayer.Tars.Messaging.Broker.Dispatch;

/// <summary>
/// Default last mile: a fresh DI scope per message, handlers invoked in registration order, first
/// failure propagated so the transport can retry or dead-letter.
/// </summary>
public sealed class ScopedIntegrationEventDispatcher(
    IServiceScopeFactory scopeFactory,
    ILogger<ScopedIntegrationEventDispatcher> logger)
    : IIntegrationEventDispatcher
{
    /// <inheritdoc />
    public async Task DispatchAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        await using var scope = scopeFactory.CreateAsyncScope();

        // Dynamic dispatch binds the closed handler type from the runtime event type.
        await InvokeAsync((dynamic)@event, scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
    }

    private async Task InvokeAsync<TIntegrationEvent>(
        TIntegrationEvent @event,
        IServiceProvider provider,
        CancellationToken cancellationToken)
        where TIntegrationEvent : IIntegrationEvent
    {
        var handlers = provider.GetServices<IIntegrationEventHandler<TIntegrationEvent>>().ToList();

        if (handlers.Count == 0)
        {
            // Not an error: a queue can legitimately receive an event this service does not act on.
            // It is worth a log, because the usual cause is a handler that was never registered.
            logger.LogDebug(
                "No handler registered for {Event} ({EventId}); the message is acknowledged and dropped.",
                typeof(TIntegrationEvent).Name, @event.EventId);
            return;
        }

        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(@event, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Handler {Handler} failed for {Event} ({EventId}); the message will be retried or " +
                    "dead-lettered by the transport.",
                    handler.GetType().Name, typeof(TIntegrationEvent).Name, @event.EventId);

                // Rethrow: on a retry every handler runs again, including the ones that already
                // succeeded. Delivery is at-least-once, so handlers must be idempotent — EventId is
                // the deduplication key they are given for exactly this.
                throw;
            }
        }
    }
}
