using Pottmayer.Tars.Data.Abstractions.DataContext;

namespace Pottmayer.Tars.Data.Tests.Unit.Infrastructure;

/// <summary>
/// A keyed factory that stands in for a provider (relational or document). Multiple instances registered
/// under different keys let the composite factory be exercised as if several providers coexisted.
/// </summary>
public sealed class FakeKeyedDataContextFactory : IKeyedDataContextFactory
{
    private readonly string _label;

    public FakeKeyedDataContextFactory(string databaseKey, string label)
    {
        DatabaseKey = databaseKey;
        _label = label;
    }

    public string DatabaseKey { get; }

    public Task<IDataContext> CreateScopedAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IDataContext>(new FakeDataContext(_label));

    public Task<IDataContext> CreateIsolatedAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IDataContext>(new FakeDataContext(_label));
}
