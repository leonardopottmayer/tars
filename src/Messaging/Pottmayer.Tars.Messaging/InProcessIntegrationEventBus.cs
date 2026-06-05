using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pottmayer.Tars.Messaging.Abstractions;

namespace Pottmayer.Tars.Messaging;

/// <summary>
/// In-process implementation of <see cref="IIntegrationEventBus"/>.
/// Resolves the registered <see cref="IIntegrationEventHandler{T}"/>s in a fresh scope and invokes
/// them synchronously. A failing handler is logged and swallowed: the producer has already committed,
/// and the work is re-requestable. When the system splits into services, this type is the only thing
/// replaced by a broker-backed bus.
/// </summary>
public sealed class InProcessIntegrationEventBus(
    IServiceScopeFactory scopeFactory,
    ILogger<InProcessIntegrationEventBus> logger)
    : IIntegrationEventBus
{
    public async Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        await using var scope = scopeFactory.CreateAsyncScope();

        // Dynamic dispatch binds the closed handler type from the runtime event type.
        await DispatchAsync((dynamic)@event, scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
    }

    private async Task DispatchAsync<TIntegrationEvent>(
        TIntegrationEvent @event,
        IServiceProvider provider,
        CancellationToken cancellationToken)
        where TIntegrationEvent : IIntegrationEvent
    {
        foreach (var handler in provider.GetServices<IIntegrationEventHandler<TIntegrationEvent>>())
        {
            try
            {
                await handler.HandleAsync(@event, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Integration event handler {Handler} failed for {Event} ({EventId}).",
                    handler.GetType().Name, typeof(TIntegrationEvent).Name, @event.EventId);
            }
        }
    }
}
