namespace Pottmayer.Tars.Data.Relational.Abstractions.MultiDb;

/// <summary>
/// Coordinates operations across multiple databases with best-effort sequential commit (Level 1).
/// Does NOT provide distributed transactions. On partial failure, <paramref name="compensate"/> is
/// invoked when provided — idempotency is the caller's responsibility.
/// </summary>
public interface IMultiDatabaseCoordinator
{
    /// <summary>
    /// Runs <paramref name="work"/> across the given databases and commits each in sequence (best-effort).
    /// On partial failure, <paramref name="compensate"/> is invoked when provided.
    /// </summary>
    /// <param name="databaseKeys">Keys of the databases participating in the operation.</param>
    /// <param name="work">The work to run against the multi-database execution context.</param>
    /// <param name="compensate">Optional compensation invoked on failure; idempotency is the caller's responsibility.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the work (and any compensation) has finished.</returns>
    Task ExecuteAsync(
        IReadOnlyList<string> databaseKeys,
        Func<IMultiDatabaseExecutionContext, CancellationToken, Task> work,
        Func<IMultiDatabaseExecutionContext, Exception, CancellationToken, Task>? compensate = null,
        CancellationToken cancellationToken = default);
}
