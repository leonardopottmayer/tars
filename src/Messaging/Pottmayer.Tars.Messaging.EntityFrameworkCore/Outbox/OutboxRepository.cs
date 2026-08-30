using Dapper;
using Microsoft.EntityFrameworkCore;
using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Relational.Repositories;

namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;

/// <summary>
/// EF Core <see cref="IOutboxRepository"/>. Extends <see cref="RepositoryBase"/> so it reads and writes
/// through exactly the same ambient <c>DataContext</c> — and therefore the same connection and
/// transaction — as every domain repository in the unit of work.
/// </summary>
internal sealed class OutboxRepository(IDataContextAccessor accessor) : RepositoryBase(accessor), IOutboxRepository
{
    private const string NpgsqlProvider = "Npgsql.EntityFrameworkCore.PostgreSQL";

    private DbSet<OutboxMessage> Set => DbContext.Set<OutboxMessage>();

    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        await Set.AddAsync(message, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<LeasedOutboxMessage>> LeaseDueAsync(
        DateTimeOffset now, DateTimeOffset leaseUntil, int batchSize, CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            return [];

        if (DbContext.Database.ProviderName != NpgsqlProvider)
            throw new NotSupportedException(
                "The in-process outbox relay claims messages with FOR UPDATE SKIP LOCKED, which currently " +
                $"requires PostgreSQL. The configured provider is '{DbContext.Database.ProviderName}'.");

        var table = QualifiedTableName();

        // Claim in one statement: lock a batch of due rows (skipping any another relay already holds),
        // push their next attempt past the lease window so no one else picks them while we deliver, and
        // return just what delivery needs. Dapper on the shared connection, so it is the same database.
        var sql = $"""
            UPDATE {table} AS o
            SET {OutboxStorage.NextAttemptAtColumn} = @leaseUntil
            FROM (
                SELECT {OutboxStorage.IdColumn}
                FROM {table}
                WHERE {OutboxStorage.StatusColumn} = {(short)OutboxMessageStatus.Pending}
                  AND {OutboxStorage.NextAttemptAtColumn} IS NOT NULL
                  AND {OutboxStorage.NextAttemptAtColumn} <= @now
                ORDER BY {OutboxStorage.NextAttemptAtColumn}, {OutboxStorage.IdColumn}
                LIMIT @batchSize
                FOR UPDATE SKIP LOCKED
            ) AS due
            WHERE o.{OutboxStorage.IdColumn} = due.{OutboxStorage.IdColumn}
            RETURNING o.{OutboxStorage.IdColumn} AS "Id",
                      o.{OutboxStorage.EventTypeColumn} AS "EventType",
                      o.{OutboxStorage.PayloadColumn} AS "Payload";
            """;

        var command = new CommandDefinition(sql, new { now, leaseUntil, batchSize }, cancellationToken: cancellationToken);
        var leased = await Connection.QueryAsync<LeasedOutboxMessage>(command).ConfigureAwait(false);
        return leased.ToList();
    }

    private string QualifiedTableName()
    {
        var entity = DbContext.Model.FindEntityType(typeof(OutboxMessage))
            ?? throw new InvalidOperationException(
                $"{nameof(OutboxMessage)} is not mapped on this DbContext. Call modelBuilder.AddTarsOutbox(...) in OnModelCreating.");

        var table = entity.GetTableName()
            ?? throw new InvalidOperationException($"{nameof(OutboxMessage)} has no table name.");
        var schema = entity.GetSchema();

        return schema is null ? $"\"{table}\"" : $"\"{schema}\".\"{table}\"";
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0)
            return [];

        return await Set
            .Where(m => ids.Contains(m.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> PurgeDispatchedAsync(
        DateTimeOffset olderThan, int batchSize, CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            return 0;

        var stale = await Set
            .Where(m => m.Status == OutboxMessageStatus.Dispatched
                     && m.ProcessedAt != null
                     && m.ProcessedAt < olderThan)
            .OrderBy(m => m.ProcessedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (stale.Count > 0)
            Set.RemoveRange(stale);

        return stale.Count;
    }
}
