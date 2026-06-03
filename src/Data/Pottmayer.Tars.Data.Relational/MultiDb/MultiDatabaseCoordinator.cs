using Pottmayer.Tars.Data.Abstractions.UnitOfWork;
using Pottmayer.Tars.Data.Relational.Abstractions.MultiDb;

namespace Pottmayer.Tars.Data.Relational.MultiDb;

/// <summary>
/// Best-effort sequential coordinator (Level 1).
/// Commits each database in order; on failure, invokes <paramref name="compensate"/> when provided.
/// Does NOT guarantee atomicity — use an outbox pattern for production-grade consistency.
/// </summary>
internal sealed class MultiDatabaseCoordinator : IMultiDatabaseCoordinator
{
    private readonly IUnitOfWorkFactory _factory;

    public MultiDatabaseCoordinator(IUnitOfWorkFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public async Task ExecuteAsync(
        IReadOnlyList<string> databaseKeys,
        Func<IMultiDatabaseExecutionContext, CancellationToken, Task> work,
        Func<IMultiDatabaseExecutionContext, Exception, CancellationToken, Task>? compensate = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(databaseKeys);
        ArgumentNullException.ThrowIfNull(work);

        var ctx = new MultiDatabaseExecutionContext(databaseKeys, _factory);
        try
        {
            await work(ctx, cancellationToken).ConfigureAwait(false);
            foreach (var uow in ctx.AllUnits)
                await uow.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (compensate is not null)
        {
            await compensate(ctx, ex, cancellationToken).ConfigureAwait(false);
            throw;
        }
        finally
        {
            foreach (var uow in ctx.AllUnits)
                await uow.DisposeAsync().ConfigureAwait(false);
        }
    }
}
