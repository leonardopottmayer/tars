using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Abstractions.Keys;

namespace Pottmayer.Tars.Data.Abstractions.UnitOfWork;

public interface IUnitOfWorkFactory
{
    /// <summary>
    /// Creates a unit of work for the specified <paramref name="databaseKey"/>.
    /// The context is created lazily on the first call to <see cref="IUnitOfWork.GetContextAsync"/>.
    /// </summary>
    IUnitOfWork Create(string databaseKey);

    /// <summary>
    /// Creates a unit of work for <see cref="DataKeys.Default"/>.
    /// The context is created lazily on the first call to <see cref="IUnitOfWork.GetContextAsync"/>.
    /// </summary>
    IUnitOfWork Create() => Create(DataKeys.Default);

    /// <summary>
    /// Creates a unit of work for <paramref name="databaseKey"/>, executes <paramref name="work"/>,
    /// commits on success, and disposes the unit of work. Equivalent to manually calling
    /// <see cref="Create"/>, <see cref="IUnitOfWork.ExecuteAsync"/>, and disposing.
    /// </summary>
    Task ExecuteAsync(
        string databaseKey,
        Func<IDataContext, CancellationToken, Task> work,
        UnitOfWorkOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ExecuteAsync(string,Func{IDataContext,CancellationToken,Task},UnitOfWorkOptions?,CancellationToken)"/>
    Task<T> ExecuteAsync<T>(
        string databaseKey,
        Func<IDataContext, CancellationToken, Task<T>> work,
        UnitOfWorkOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes <paramref name="work"/> against <see cref="DataKeys.Default"/>,
    /// commits on success, and disposes the unit of work.
    /// </summary>
    Task ExecuteAsync(
        Func<IDataContext, CancellationToken, Task> work,
        UnitOfWorkOptions? options = null,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(DataKeys.Default, work, options, cancellationToken);

    /// <inheritdoc cref="ExecuteAsync(Func{IDataContext,CancellationToken,Task},UnitOfWorkOptions?,CancellationToken)"/>
    Task<T> ExecuteAsync<T>(
        Func<IDataContext, CancellationToken, Task<T>> work,
        UnitOfWorkOptions? options = null,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(DataKeys.Default, work, options, cancellationToken);
}
