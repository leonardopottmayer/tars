using Pottmayer.Tars.Core.Ddd;
using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Abstractions.Repositories;

namespace Pottmayer.Tars.Data.Relational.DataContext;

/// <summary>
/// Non-owning view of an existing ambient <see cref="IDataContext"/>.
/// Returned by <see cref="RelationalDataContextFactory{TDbContext}.CreateScopedAsync"/> when a
/// context for the same database key is already active in the current async scope.
/// <para>
/// <see cref="CommitAsync"/> and <see cref="DisposeAsync"/> are intentional no-ops: only the
/// owning (outer) context commits and disposes the real underlying context.
/// </para>
/// </summary>
internal sealed class BorrowedDataContext : IDataContext
{
    private readonly IDataContext _owner;

    internal BorrowedDataContext(IDataContext owner) => _owner = owner;

    public IRepositoryResolver Resolver => _owner.Resolver;

    public TRepository AcquireRepository<TRepository>() where TRepository : class, IRepository
        => _owner.AcquireRepository<TRepository>();

    public void CollectDomainEvents(IHasDomainEvents aggregate)
        => _owner.CollectDomainEvents(aggregate);

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
