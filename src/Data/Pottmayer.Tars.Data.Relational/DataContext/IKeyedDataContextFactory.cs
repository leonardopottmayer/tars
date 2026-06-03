using Pottmayer.Tars.Data.Abstractions.DataContext;

namespace Pottmayer.Tars.Data.Relational.DataContext;

/// <summary>
/// Internal contract — one implementation per registered pipeline.
/// <see cref="CompositeDataContextFactory"/> dispatches to the correct instance by database key.
/// </summary>
internal interface IKeyedDataContextFactory
{
    string DatabaseKey { get; }
    Task<IDataContext> CreateScopedAsync(CancellationToken cancellationToken = default);
    Task<IDataContext> CreateIsolatedAsync(CancellationToken cancellationToken = default);
}
