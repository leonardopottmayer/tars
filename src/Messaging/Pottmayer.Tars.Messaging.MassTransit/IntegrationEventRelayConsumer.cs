using MassTransit;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker.Dispatch;

namespace Pottmayer.Tars.Messaging.MassTransit;

/// <summary>
/// The last mile for MassTransit: one consumer per subscribed event type, handing the typed message
/// to the shared dispatcher. Everything below this — retry, redelivery, the error queue — is
/// MassTransit's, which is the point of running on it.
/// </summary>
/// <typeparam name="TIntegrationEvent">The type of integration event being consumed.</typeparam>
/// <param name="dispatcher">The integration event dispatcher.</param>
/// <remarks>
/// The dispatcher rethrows, so a handler failure reaches MassTransit and its retry policy decides
/// what happens next. Delivery is at-least-once: handlers must be idempotent on
/// <see cref="IIntegrationEvent.EventId"/>.
/// </remarks>
public sealed class IntegrationEventRelayConsumer<TIntegrationEvent>(IIntegrationEventDispatcher dispatcher)
    : IConsumer<TIntegrationEvent>
    where TIntegrationEvent : class, IIntegrationEvent
{
    /// <inheritdoc />
    public Task Consume(ConsumeContext<TIntegrationEvent> context)
        => dispatcher.DispatchAsync(context.Message, context.CancellationToken);
}
