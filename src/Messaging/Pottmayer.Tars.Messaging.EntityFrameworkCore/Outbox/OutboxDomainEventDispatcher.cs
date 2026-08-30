using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Ddd;

namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;

/// <summary>
/// An <see cref="IDomainEventDispatcher"/> that the relational
/// <c>DataContext.CommitAsync</c> invokes just before it saves. It hands each domain event to the
/// registered <see cref="IDomainEventHandler{T}"/> translators, which react by publishing integration
/// events through the outbox-backed bus — so those outbox rows land in the same transaction as the
/// aggregate that raised the event.
/// </summary>
/// <remarks>
/// Handlers run in the <b>current</b> scope (the one that owns the open transaction), and exceptions
/// <b>propagate</b>: a failing translator aborts the commit, so the state change and the events it
/// should have produced are rolled back together. This is the opposite of the fire-and-forget
/// in-process bus, and deliberately so — here the point is atomicity, not best effort.
/// </remarks>
public sealed class OutboxDomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IReadOnlyCollection<object> domainEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            if (domainEvent is IDomainEvent typed)
                await InvokeAsync((dynamic)typed, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task InvokeAsync<TDomainEvent>(TDomainEvent domainEvent, CancellationToken cancellationToken)
        where TDomainEvent : IDomainEvent
    {
        // Resolved from the current scope so translators share the request's bus and ambient context.
        var handlers = serviceProvider.GetServices<IDomainEventHandler<TDomainEvent>>();
        foreach (var handler in handlers)
            await handler.HandleAsync(domainEvent, cancellationToken).ConfigureAwait(false);
    }
}
