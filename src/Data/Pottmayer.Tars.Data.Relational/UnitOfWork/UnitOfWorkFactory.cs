using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Abstractions.UnitOfWork;
using Pottmayer.Tars.Data.Relational.Abstractions.DataContext;

namespace Pottmayer.Tars.Data.Relational.UnitOfWork;

internal sealed class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IDataContextFactory _factory;

    public UnitOfWorkFactory(IDataContextFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public IUnitOfWork Create(string databaseKey) => new UnitOfWork(databaseKey, _factory);

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
