namespace Pottmayer.Tars.Data.Relational.Abstractions.MultiDb;

/// <summary>
/// Coordinates operations across multiple databases with best-effort sequential commit (Level 1).
/// Does NOT provide distributed transactions. On partial failure, <paramref name="compensate"/> is
/// invoked when provided — idempotency is the caller's responsibility.
/// </summary>
public interface IMultiDatabaseCoordinator
{
    Task ExecuteAsync(
        IReadOnlyList<string> databaseKeys,
        Func<IMultiDatabaseExecutionContext, CancellationToken, Task> work,
        Func<IMultiDatabaseExecutionContext, Exception, CancellationToken, Task>? compensate = null,
        CancellationToken cancellationToken = default);
}
