using Pottmayer.Tars.Core.Ddd;
using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Abstractions.Repositories;

namespace Pottmayer.Tars.Data.DataContext;

/// <summary>
/// Non-owning view of an existing ambient <see cref="IDataContext"/>. Returned by a keyed factory's
/// <c>CreateScopedAsync</c> when a context for the same database key is already active in the current
/// async scope, so nested code shares the same connection and transaction. Provider-agnostic.
/// <para>
/// <see cref="CommitAsync"/> and <see cref="DisposeAsync"/> are intentional no-ops: only the owning
/// (outer) context commits and disposes the real underlying context.
/// </para>
/// </summary>
public sealed class BorrowedDataContext : IDataContext
{
    private readonly IDataContext _owner;

    /// <summary>Creates a borrowed view over the owning context.</summary>
    /// <param name="owner">The owning context to delegate reads and repository resolution to.</param>
    public BorrowedDataContext(IDataContext owner) => _owner = owner ?? throw new ArgumentNullException(nameof(owner));

    /// <inheritdoc/>
    public IRepositoryResolver Resolver => _owner.Resolver;

    /// <inheritdoc/>
    public TRepository AcquireRepository<TRepository>() where TRepository : class, IRepository
        => _owner.AcquireRepository<TRepository>();

    /// <inheritdoc/>
    public void CollectDomainEvents(IHasDomainEvents aggregate)
        => _owner.CollectDomainEvents(aggregate);

    /// <inheritdoc/>
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
