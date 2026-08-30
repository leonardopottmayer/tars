using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pottmayer.Tars.Data.Abstractions.UnitOfWork;
using Pottmayer.Tars.Messaging.Broker.Dispatch;
using Pottmayer.Tars.Messaging.Broker.Registry;
using Pottmayer.Tars.Messaging.EntityFrameworkCore.Options;
using Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;

namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.Relay;

/// <summary>
/// The work of one relay tick for a single database, factored out of the hosted service so it can be
/// driven directly from a test. Drains due messages and, separately, purges old dispatched ones.
/// </summary>
/// <remarks>
/// <para>Delivery runs in three phases, on purpose:</para>
/// <list type="number">
///   <item><b>Lease</b> — read a batch of due rows and close the transaction. Nothing is held open while handlers run.</item>
///   <item><b>Deliver</b> — dispatch each event through the shared last-mile dispatcher (fresh scope, failures propagate), recording the outcome in memory.</item>
///   <item><b>Record</b> — reopen a short transaction and stamp each row dispatched or failed-with-backoff.</item>
/// </list>
/// <para>
/// Keeping delivery outside the source transaction matters twice over: a handler is free to open its
/// own unit of work and even publish further events (the source context is no longer ambient, so the
/// publish target stays unambiguous), and a slow handler never pins the producer's connection. The
/// price is at-least-once delivery — a crash after a handler commits but before phase 3 redelivers the
/// message — which is why handlers must be idempotent on <c>EventId</c>.
/// </para>
/// </remarks>
public sealed class OutboxRelayProcessor(
    IServiceScopeFactory scopeFactory,
    IIntegrationEventTypeRegistry registry,
    IIntegrationEventDispatcher dispatcher,
    IIntegrationEventSerializer serializer,
    TimeProvider timeProvider,
    ILogger logger,
    OutboxDatabaseOptions options)
{
    /// <summary>Runs one drain pass. Returns how many messages were delivered.</summary>
    public async Task<int> DrainOnceAsync(CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
        var now = timeProvider.GetUtcNow();
        var leaseUntil = now + options.LeaseDuration;

        // Phase 1 — claim. FOR UPDATE SKIP LOCKED locks a batch and pushes it past the lease window, so a
        // second relay skips it while we deliver. The claim self-commits; no transaction is held after.
        var leased = await factory.ExecuteAsync(options.DatabaseKey, async (context, token) =>
        {
            var outbox = context.AcquireRepository<IOutboxRepository>();
            return await outbox.LeaseDueAsync(now, leaseUntil, options.BatchSize, token).ConfigureAwait(false);
        }, new UnitOfWorkOptions { CommitOnSuccess = false }, cancellationToken).ConfigureAwait(false);

        if (leased.Count == 0)
            return 0;

        // Phase 2 — deliver. null outcome = success; a string = the failure to record.
        var outcomes = new Dictionary<Guid, string?>(leased.Count);
        foreach (var message in leased)
        {
            cancellationToken.ThrowIfCancellationRequested();
            outcomes[message.Id] = await TryDeliverAsync(message, cancellationToken).ConfigureAwait(false);
        }

        // Phase 3 — record outcomes in one short transaction.
        var dispatched = 0;
        await factory.ExecuteAsync(options.DatabaseKey, async (context, token) =>
        {
            var outbox = context.AcquireRepository<IOutboxRepository>();
            var rows = await outbox.GetByIdsAsync(outcomes.Keys.ToArray(), token).ConfigureAwait(false);

            foreach (var row in rows)
            {
                var error = outcomes[row.Id];
                if (error is null)
                {
                    row.MarkDispatched(timeProvider);
                    dispatched++;
                }
                else
                {
                    row.MarkFailed(error, options.MaxAttempts, options.Backoff, timeProvider);
                }
            }
        }, cancellationToken: cancellationToken).ConfigureAwait(false);

        return dispatched;
    }

    /// <summary>Runs one purge pass, deleting dispatched rows past their retention. Returns how many were removed.</summary>
    public async Task<int> PurgeOnceAsync(CancellationToken cancellationToken = default)
    {
        if (!options.PurgeEnabled)
            return 0;

        using var scope = scopeFactory.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
        var cutoff = timeProvider.GetUtcNow() - options.RetentionPeriod;

        return await factory.ExecuteAsync(options.DatabaseKey, async (context, token) =>
        {
            var outbox = context.AcquireRepository<IOutboxRepository>();
            return await outbox.PurgeDispatchedAsync(cutoff, options.PurgeBatchSize, token).ConfigureAwait(false);
        }, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> TryDeliverAsync(LeasedOutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            if (!registry.TryResolve(message.EventType, out var eventType))
            {
                // Not transient: nobody registered this contract. Let it exhaust retries and dead-letter,
                // so it surfaces rather than blocking the queue head forever.
                return $"No integration event type is registered for logical name '{message.EventType}'. " +
                       "Register the contracts assembly via AddTarsIntegrationEventTypeRegistry.";
            }

            var @event = serializer.DeserializePayload(eventType, message.Payload);
            await dispatcher.DispatchAsync(@event, cancellationToken).ConfigureAwait(false);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Outbox delivery failed for {EventType} ({MessageId}) on database {DatabaseKey}; it will be retried or dead-lettered.",
                message.EventType, message.Id, options.DatabaseKey);
            return ex.Message;
        }
    }
}
