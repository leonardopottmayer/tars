using Pottmayer.Tars.Data.Abstractions.Repositories;

namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;

/// <summary>
/// Persistence for <see cref="OutboxMessage"/>, bound to the ambient data context like any other Tars
/// repository. It is deliberately a repository and nothing more: writing a row is the whole "publish"
/// on the producing side, so the write joins the producer's transaction automatically, and the data
/// layer never learns what an event or a handler is.
/// </summary>
public interface IOutboxRepository : IRepository
{
    /// <summary>Stages a message in the current context. Persisted by the surrounding unit of work's commit — same transaction as the state change.</summary>
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically claims a batch of due pending messages (<c>NextAttemptAt &lt;= now</c>, oldest first)
    /// and returns them. The claim takes a row lock with <c>FOR UPDATE SKIP LOCKED</c> and pushes each
    /// row's <c>NextAttemptAt</c> to <paramref name="leaseUntil"/>, so a second relay skips them while
    /// this one delivers — the lock is held only for the claim, not across delivery. The relay delivers
    /// the returned snapshots, then records each outcome with <see cref="GetByIdsAsync"/>.
    /// </summary>
    /// <remarks>Requires PostgreSQL (uses <c>FOR UPDATE SKIP LOCKED</c>).</remarks>
    Task<IReadOnlyList<LeasedOutboxMessage>> LeaseDueAsync(
        DateTimeOffset now, DateTimeOffset leaseUntil, int batchSize, CancellationToken cancellationToken = default);

    /// <summary>Reloads specific messages, change-tracked, so the relay can apply their delivery outcome and commit it.</summary>
    Task<IReadOnlyList<OutboxMessage>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>Deletes up to <paramref name="batchSize"/> dispatched messages processed before <paramref name="olderThan"/>. Returns how many were removed.</summary>
    Task<int> PurgeDispatchedAsync(DateTimeOffset olderThan, int batchSize, CancellationToken cancellationToken = default);
}
