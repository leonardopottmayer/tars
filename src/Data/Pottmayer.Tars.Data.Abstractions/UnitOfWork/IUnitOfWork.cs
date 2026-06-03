using Pottmayer.Tars.Data.Abstractions.DataContext;

namespace Pottmayer.Tars.Data.Abstractions.UnitOfWork;

/// <summary>
/// Unit of work that owns a <see cref="IDataContext"/> for a specific database key.
/// Supports two usage patterns:
/// <list type="bullet">
///   <item><b>Delegate</b> — <c>ExecuteAsync</c> auto-commits and auto-disposes.</item>
///   <item><b>Direct</b> — call <c>GetContextAsync</c> and <c>CommitAsync</c> manually for conditional flows.</item>
/// </list>
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>Returns the context, creating it lazily on first call.</summary>
    ValueTask<IDataContext> GetContextAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists changes and dispatches domain events.</summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>Runs <paramref name="work"/>, then auto-commits (unless <paramref name="options"/> opt out).</summary>
    Task ExecuteAsync(
        Func<IDataContext, CancellationToken, Task> work,
        UnitOfWorkOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Runs <paramref name="work"/>, then auto-commits (unless <paramref name="options"/> opt out). Returns the delegate result.</summary>
    Task<T> ExecuteAsync<T>(
        Func<IDataContext, CancellationToken, Task<T>> work,
        UnitOfWorkOptions? options = null,
        CancellationToken cancellationToken = default);
}
