using Pottmayer.Tars.Data.Abstractions.DataContext;

namespace Pottmayer.Tars.Data.DataContext;

/// <summary>
/// Routes <c>CreateScopedAsync</c> / <c>CreateIsolatedAsync</c> to the
/// <see cref="IKeyedDataContextFactory"/> registered for the requested database key.
/// Providers (relational, document, …) each contribute keyed factories; this composite lets any
/// mix of them coexist behind a single <see cref="IDataContextFactory"/>.
/// </summary>
internal sealed class CompositeDataContextFactory : IDataContextFactory
{
    private readonly IReadOnlyDictionary<string, IKeyedDataContextFactory> _factories;

    public CompositeDataContextFactory(IEnumerable<IKeyedDataContextFactory> factories)
    {
        ArgumentNullException.ThrowIfNull(factories);
        _factories = factories.ToDictionary(f => f.DatabaseKey, StringComparer.OrdinalIgnoreCase);
    }

    public Task<IDataContext> CreateScopedAsync(string databaseKey, CancellationToken cancellationToken = default)
        => Resolve(databaseKey).CreateScopedAsync(cancellationToken);

    public Task<IDataContext> CreateIsolatedAsync(string databaseKey, CancellationToken cancellationToken = default)
        => Resolve(databaseKey).CreateIsolatedAsync(cancellationToken);

    private IKeyedDataContextFactory Resolve(string databaseKey)
    {
        if (_factories.TryGetValue(databaseKey, out var factory))
            return factory;
        throw new InvalidOperationException(
            $"No data pipeline registered for key '{databaseKey}'. " +
            $"Register one, e.g. services.AddTarsRelationalData<TDbContext>(\"{databaseKey}\", ...) " +
            $"or services.AddTarsMongoData(\"{databaseKey}\", ...).");
    }
}
