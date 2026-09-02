namespace Pottmayer.Tars.Core.Ddd;

/// <summary>
/// Handles a domain event of type <typeparamref name="TDomainEvent"/> raised by an aggregate.
/// Multiple handlers may subscribe to the same event.
/// </summary>
/// <remarks>
/// <para>
/// Domain event handlers run <strong>inside the unit of work that raised the event</strong>: the
/// data layer dispatches them after change tracking is settled but <em>before</em> the transaction
/// commits (see the relational <c>DataContext.CommitAsync</c>). A handler that throws therefore
/// rolls the whole transaction back — the state change and everything the handler did are undone
/// together, which is what keeps the two atomic.
/// </para>
/// <para>
/// This is the seam the "domain event → integration event" translation rides on: a handler reacts to
/// a domain event and publishes an integration event through <c>IIntegrationEventBus</c>. When the bus
/// is outbox-backed, that publish writes an outbox row into the very same transaction, so the fact and
/// its announcement commit as one. The handler names the integration contract; it never names the
/// outbox.
/// </para>
/// </remarks>
public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    /// <summary>Handles the raised domain event.</summary>
    /// <param name="domainEvent">The domain event to handle.</param>
    /// <param name="cancellationToken">Token used to cancel handling.</param>
    /// <returns>A task that completes when the event has been handled.</returns>
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
