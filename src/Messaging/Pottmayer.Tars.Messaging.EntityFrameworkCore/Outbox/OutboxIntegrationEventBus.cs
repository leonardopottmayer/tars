using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Messaging.Abstractions;

namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;

/// <summary>
/// Outbox-backed <see cref="IIntegrationEventBus"/>: <see cref="PublishAsync"/> does not deliver
/// anything, it writes an <see cref="OutboxMessage"/> into the producer's current transaction. The
/// relay delivers later. Producers keep calling <c>PublishAsync</c> and never name the outbox — the
/// durability is the infrastructure's business, not theirs.
/// </summary>
/// <remarks>
/// <para>
/// Both ways of producing an event end here: an application handler calling <c>PublishAsync</c>
/// directly, and a domain-event translator that reacts to a <c>Raise</c> at commit time and publishes
/// through this same bus. The one rule it enforces is that a publish must happen <em>inside a unit of
/// work</em>: without an open context there is no transaction to join, and writing anyway would
/// recreate the very dual-write this exists to prevent — so it fails loudly instead.
/// </para>
/// <para>
/// The target is the ambient <see cref="IDataContextAccessor.Current"/> context, which the unit of
/// work publishes for the duration of its delegate and commit. That is the innermost active unit of
/// work, so a publish always joins the transaction it is written inside — no ambiguity, and nothing to
/// guess.
/// </para>
/// </remarks>
public sealed class OutboxIntegrationEventBus(
    IDataContextAccessor accessor,
    IIntegrationEventSerializer serializer,
    TimeProvider timeProvider)
    : IIntegrationEventBus
{
    public async Task PublishAsync(IIntegrationEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var context = accessor.Current
            ?? throw new InvalidOperationException(
                "IIntegrationEventBus.PublishAsync was called without an open unit of work. The outbox " +
                "writes the event in the producer's transaction, so publish from inside " +
                "IUnitOfWorkFactory.ExecuteAsync (or a domain-event handler, which runs during commit). " +
                "Publishing outside a transaction would reintroduce the dual-write this prevents.");

        var eventType = @event.GetType();
        var message = OutboxMessage.Enqueue(
            eventId: @event.EventId,
            eventType: IntegrationEventNaming.For(eventType),
            version: IntegrationEventNaming.VersionFor(eventType),
            payload: serializer.SerializePayload(@event),
            headers: serializer.SerializeHeaders(@event),
            occurredAt: @event.OccurredAt,
            clock: timeProvider);

        var outbox = context.AcquireRepository<IOutboxRepository>();
        await outbox.AddAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
