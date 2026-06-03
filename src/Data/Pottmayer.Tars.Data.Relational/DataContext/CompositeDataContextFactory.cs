using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Relational.Abstractions.DataContext;

namespace Pottmayer.Tars.Data.Relational.DataContext;

/// <summary>
/// Routes <c>CreateScopedAsync</c> / <c>CreateIsolatedAsync</c> to the
/// <see cref="IKeyedDataContextFactory"/> registered for the requested database key.
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
            $"Call services.AddTarsData<TDbContext>(\"{databaseKey}\", buildOptions) in Program.cs.");
    }
}
