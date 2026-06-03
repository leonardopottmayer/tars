using Pottmayer.Tars.Data.Abstractions.DataContext;

namespace Pottmayer.Tars.Data.Relational.Abstractions.DataContext;

/// <summary>
/// Factory for creating relational data contexts keyed by database name.
/// </summary>
public interface IDataContextFactory
{
    /// <summary>
    /// Returns the ambient context for <paramref name="databaseKey"/> if one exists in the current
    /// async flow; otherwise creates and sets a new ambient context.
    /// Use this when nested code should share the same connection and transaction.
    /// </summary>
    Task<IDataContext> CreateScopedAsync(string databaseKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Always creates a fresh context that is not tied to the ambient scope.
    /// Use this for parallel or independent units of work.
    /// </summary>
    Task<IDataContext> CreateIsolatedAsync(string databaseKey, CancellationToken cancellationToken = default);
}
