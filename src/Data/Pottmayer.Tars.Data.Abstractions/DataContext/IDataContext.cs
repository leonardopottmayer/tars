using Pottmayer.Tars.Core.Ddd;
using Pottmayer.Tars.Data.Abstractions.Repositories;

namespace Pottmayer.Tars.Data.Abstractions.DataContext;

/// <summary>
/// Represents a unit-of-work boundary for data access.
/// Resolves repositories and manages domain-event collection.
/// </summary>
public interface IDataContext : IAsyncDisposable
{
    /// <summary>Resolver for repositories attached to this context.</summary>
    IRepositoryResolver Resolver { get; }

    /// <summary>
    /// Resolves a repository bound to this context.
    /// Equivalent to <c>Resolver.ResolveRepository&lt;TRepository&gt;()</c>.
    /// </summary>
    TRepository AcquireRepository<TRepository>() where TRepository : class, IRepository;

    /// <summary>Persists changes and dispatches collected domain events.</summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually registers domain events from an aggregate modified outside the provider's
    /// change-tracking mechanism (e.g. via Dapper or a raw SQL command).
    /// </summary>
    void CollectDomainEvents(IHasDomainEvents aggregate);
}
