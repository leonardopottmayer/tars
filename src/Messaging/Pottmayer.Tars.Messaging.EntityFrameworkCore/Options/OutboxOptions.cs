namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.Options;

/// <summary>
/// Fleet-wide relay tuning, bindable from configuration (default section <c>Tars:Messaging:Outbox</c>).
/// These are the operational knobs that legitimately differ per environment — polling cadence, batch
/// size, retry budget, retention — so they live in <c>appsettings</c>. Per-database overrides and the
/// backoff function stay in code (see <see cref="OutboxDatabaseOptions"/>), the same split the broker
/// options make between connection settings and code-level subscriptions.
/// </summary>
public class OutboxOptions
{
    /// <summary>Configuration section these options bind from by default.</summary>
    public const string SectionName = "Tars:Messaging:Outbox";

    /// <summary>Message reported when validation fails on application start.</summary>
    public const string ValidationErrorMessage =
        "Invalid OutboxOptions. PollingInterval, LeaseDuration, RetentionPeriod, and PurgeInterval must be positive; BatchSize, MaxAttempts, and PurgeBatchSize must be greater than zero.";

    /// <summary>How often the relay looks for due messages. Also the floor on delivery latency when idle.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Messages claimed per tick. Larger trades latency-per-message for more work held in memory during a pass.</summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>Delivery attempts before a message is dead-lettered rather than retried again.</summary>
    public int MaxAttempts { get; set; } = 8;

    /// <summary>
    /// How long a claimed message stays invisible to other relays while it is being delivered. The relay
    /// claims a batch with <c>FOR UPDATE SKIP LOCKED</c> and pushes <c>next_attempt_at</c> this far into
    /// the future, so a second instance never picks the same rows. If a relay crashes mid-delivery, the
    /// message reappears once the lease expires (redelivery — hence idempotent handlers). Keep it
    /// comfortably above the slowest handler.
    /// </summary>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>When true, dispatched rows are periodically deleted after <see cref="RetentionPeriod"/>.</summary>
    public bool PurgeEnabled { get; set; } = true;

    /// <summary>How long a dispatched row is kept (for audit/inspection) before purge removes it.</summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(7);

    /// <summary>How often the purge pass runs.</summary>
    public TimeSpan PurgeInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>Rows deleted per purge pass, so a large backlog is cleared in bounded chunks.</summary>
    public int PurgeBatchSize { get; set; } = 500;

    /// <summary>
    /// Returns <c>true</c> when the options are internally consistent: intervals and durations are positive,
    /// and batch sizes and attempts are greater than zero.
    /// </summary>
    public virtual bool IsValid()
    {
        if (PollingInterval <= TimeSpan.Zero)
            return false;

        if (BatchSize <= 0)
            return false;

        if (MaxAttempts <= 0)
            return false;

        if (LeaseDuration <= TimeSpan.Zero)
            return false;

        if (RetentionPeriod <= TimeSpan.Zero)
            return false;

        if (PurgeInterval <= TimeSpan.Zero)
            return false;

        if (PurgeBatchSize <= 0)
            return false;

        return true;
    }
}
