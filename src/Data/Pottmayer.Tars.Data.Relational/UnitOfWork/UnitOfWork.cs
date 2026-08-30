using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Abstractions.UnitOfWork;
using Pottmayer.Tars.Data.Relational.Abstractions.DataContext;

namespace Pottmayer.Tars.Data.Relational.UnitOfWork;

/// <summary>
/// Unit of work that lazily creates its <see cref="IDataContext"/> on first access.
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly string _databaseKey;
    private readonly IDataContextFactory _factory;
    private readonly IDataContextAccessor _accessor;
    private IDataContext? _context;
    private bool _disposed;

    public UnitOfWork(string databaseKey, IDataContextFactory factory, IDataContextAccessor accessor)
    {
        _databaseKey = string.IsNullOrWhiteSpace(databaseKey)
            ? throw new ArgumentException("Database key must not be null or empty.", nameof(databaseKey))
            : databaseKey;
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
    }

    public async ValueTask<IDataContext> GetContextAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _context ??= await _factory.CreateScopedAsync(_databaseKey, cancellationToken).ConfigureAwait(false);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_context is null) return;
        await _context.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ExecuteAsync(
        Func<IDataContext, CancellationToken, Task> work,
        UnitOfWorkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(work);
        var ctx = await GetContextAsync(cancellationToken).ConfigureAwait(false);

        // Publish the context as the ambient one for the duration of the delegate (and the commit that
        // dispatches domain events). Repositories resolve their context directly, but ambient code —
        // notably an outbox-backed event bus — needs a way to find the transaction it must join. Set
        // here, in the frame that runs the delegate, so it flows into the delegate's async calls.
        var previous = _accessor.Current;
        _accessor.SetCurrent(ctx);
        try
        {
            await work(ctx, cancellationToken).ConfigureAwait(false);
            if (options?.CommitOnSuccess != false)
                await CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _accessor.SetCurrent(previous);
        }
    }

    public async Task<T> ExecuteAsync<T>(
        Func<IDataContext, CancellationToken, Task<T>> work,
        UnitOfWorkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(work);
        var ctx = await GetContextAsync(cancellationToken).ConfigureAwait(false);

        var previous = _accessor.Current;
        _accessor.SetCurrent(ctx);
        try
        {
            var result = await work(ctx, cancellationToken).ConfigureAwait(false);
            if (options?.CommitOnSuccess != false)
                await CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        finally
        {
            _accessor.SetCurrent(previous);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        if (_context is IAsyncDisposable d)
            await d.DisposeAsync().ConfigureAwait(false);
        _context = null;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
