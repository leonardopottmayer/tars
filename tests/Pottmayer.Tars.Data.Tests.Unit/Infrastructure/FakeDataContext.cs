using Pottmayer.Tars.Core.Ddd;
using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Abstractions.Repositories;

namespace Pottmayer.Tars.Data.Tests.Unit.Infrastructure;

/// <summary>Minimal in-memory <see cref="IDataContext"/> used to exercise the provider-agnostic orchestration.</summary>
public sealed class FakeDataContext : IDataContext
{
    public string Label { get; }
    public int CommitCount { get; private set; }
    public bool Disposed { get; private set; }

    public FakeDataContext(string label) => Label = label;

    public IRepositoryResolver Resolver => throw new NotSupportedException();

    public TRepository AcquireRepository<TRepository>() where TRepository : class, IRepository
        => throw new NotSupportedException();

    public void CollectDomainEvents(IHasDomainEvents aggregate) { }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        CommitCount++;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}
