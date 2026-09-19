using Pottmayer.Tars.Data.Abstractions.DataContext;

namespace Pottmayer.Tars.Data.Tests.Unit.Infrastructure;

/// <summary>An <see cref="IDataContextFactory"/> that hands out (and remembers) a single fake context per key.</summary>
public sealed class FakeDataContextFactory : IDataContextFactory
{
    public Dictionary<string, FakeDataContext> Created { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Task<IDataContext> CreateScopedAsync(string databaseKey, CancellationToken cancellationToken = default)
    {
        var ctx = new FakeDataContext(databaseKey);
        Created[databaseKey] = ctx;
        return Task.FromResult<IDataContext>(ctx);
    }

    public Task<IDataContext> CreateIsolatedAsync(string databaseKey, CancellationToken cancellationToken = default)
        => CreateScopedAsync(databaseKey, cancellationToken);
}
