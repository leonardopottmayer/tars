using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Abstractions.UnitOfWork;
using Pottmayer.Tars.Data.Relational.Abstractions.DataContext;

namespace Pottmayer.Tars.Data.Relational.UnitOfWork;

/// <summary>Default <see cref="IUnitOfWorkFactory"/> that creates <see cref="UnitOfWork"/> instances per database key.</summary>
internal sealed class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IDataContextFactory _factory;
    private readonly IDataContextAccessor _accessor;

    public UnitOfWorkFactory(IDataContextFactory factory, IDataContextAccessor accessor)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    public IUnitOfWork Create(string databaseKey) => new UnitOfWork(databaseKey, _factory, _accessor);

    public async Task ExecuteAsync(
        string databaseKey,
        Func<IDataContext, CancellationToken, Task> work,
        UnitOfWorkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await using var uow = Create(databaseKey);
        await uow.ExecuteAsync(work, options, cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> ExecuteAsync<T>(
        string databaseKey,
        Func<IDataContext, CancellationToken, Task<T>> work,
        UnitOfWorkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await using var uow = Create(databaseKey);
        return await uow.ExecuteAsync(work, options, cancellationToken).ConfigureAwait(false);
    }
}
