namespace Pottmayer.Tars.Messaging.EntityFrameworkCore.Options;

/// <summary>
/// The effective options for one relay (one <c>DataKeys</c> key). Inherits the config-bound tuning from
/// <see cref="OutboxOptions"/> and adds the two things that are not fleet-wide configuration: the
/// database this relay drains, and the backoff function (a delegate, so it stays in code).
/// </summary>
public sealed class OutboxDatabaseOptions : OutboxOptions
{
    /// <summary>Creates the options for <paramref name="databaseKey"/>, optionally seeded from fleet defaults.</summary>
    /// <param name="databaseKey">The database key whose outbox this relay drains.</param>
    /// <param name="defaults">Fleet-wide tuning to copy in (typically bound from configuration); <c>null</c> uses the built-in defaults.</param>
    public OutboxDatabaseOptions(string databaseKey, OutboxOptions? defaults = null)
    {
        DatabaseKey = string.IsNullOrWhiteSpace(databaseKey)
            ? throw new ArgumentException("Database key must not be null or empty.", nameof(databaseKey))
            : databaseKey;

        if (defaults is null)
            return;

        PollingInterval = defaults.PollingInterval;
        BatchSize = defaults.BatchSize;
        MaxAttempts = defaults.MaxAttempts;
        LeaseDuration = defaults.LeaseDuration;
        PurgeEnabled = defaults.PurgeEnabled;
        RetentionPeriod = defaults.RetentionPeriod;
        PurgeInterval = defaults.PurgeInterval;
        PurgeBatchSize = defaults.PurgeBatchSize;
    }

    /// <summary>The database key whose outbox this relay drains.</summary>
    public string DatabaseKey { get; }

    /// <summary>
    /// Backoff before the next attempt, as a function of the attempt count (1-based). Default: exponential,
    /// capped at five minutes so a stuck message keeps retrying at a sane cadence instead of drifting to hours.
    /// A delegate, so it is set in code rather than configuration.
    /// </summary>
    public Func<int, TimeSpan> Backoff { get; set; } =
        attempt => TimeSpan.FromSeconds(Math.Min(300, Math.Pow(2, attempt)));

    /// <summary>
    /// Returns <c>true</c> when the options are internally consistent: shared outbox checks pass
    /// (see <see cref="OutboxOptions.IsValid"/>), <see cref="DatabaseKey"/> is non-blank, and <see cref="Backoff"/> is non-null.
    /// </summary>
    public override bool IsValid()
    {
        if (!base.IsValid())
            return false;

        if (string.IsNullOrWhiteSpace(DatabaseKey))
            return false;

        if (Backoff is null)
            return false;

        return true;
    }
}
