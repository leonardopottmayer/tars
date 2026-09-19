using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Abstractions.UnitOfWork;
using Pottmayer.Tars.Data.DI;
using Pottmayer.Tars.Data.Tests.Unit.Infrastructure;

namespace Pottmayer.Tars.Data.Tests.Unit;

/// <summary>
/// The unit of work is provider-agnostic: it drives whatever context the factory returns for a key.
/// These tests use a fake context so the commit/dispose/ambient contract is verified without a database.
/// </summary>
public class UnitOfWorkTests
{
    private static (IUnitOfWorkFactory factory, FakeDataContextFactory contexts) Build()
    {
        var contexts = new FakeDataContextFactory();
        var provider = new ServiceCollection()
            .AddTarsDataContextAccessor()
            .AddTarsUnitOfWorkFactory()
            .AddSingleton<IDataContextFactory>(contexts)
            .BuildServiceProvider();
        return (provider.GetRequiredService<IUnitOfWorkFactory>(), contexts);
    }

    [Fact]
    public async Task ExecuteAsync_commits_and_disposes_by_default()
    {
        var (factory, contexts) = Build();

        await factory.ExecuteAsync("orders", (ctx, ct) => Task.CompletedTask);

        var ctx = contexts.Created["orders"];
        ctx.CommitCount.Should().Be(1);
        ctx.Disposed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_does_not_commit_when_opted_out()
    {
        var (factory, contexts) = Build();

        await factory.ExecuteAsync("orders", (ctx, ct) => Task.CompletedTask,
            new UnitOfWorkOptions { CommitOnSuccess = false });

        contexts.Created["orders"].CommitCount.Should().Be(0);
    }

    [Fact]
    public async Task Delegate_sees_the_context_as_ambient()
    {
        var contexts = new FakeDataContextFactory();
        var provider = new ServiceCollection()
            .AddTarsDataContextAccessor()
            .AddTarsUnitOfWorkFactory()
            .AddSingleton<IDataContextFactory>(contexts)
            .BuildServiceProvider();
        var accessor = provider.GetRequiredService<IDataContextAccessor>();
        var factory = provider.GetRequiredService<IUnitOfWorkFactory>();

        IDataContext? seen = null;
        await factory.ExecuteAsync("orders", (ctx, ct) =>
        {
            seen = accessor.Current;
            return Task.CompletedTask;
        });

        seen.Should().BeSameAs(contexts.Created["orders"]);
        accessor.Current.Should().BeNull("ambient context is restored after the unit of work");
    }

    [Fact]
    public async Task Manual_commit_flows_through_to_the_context()
    {
        var (factory, contexts) = Build();

        await using var uow = factory.Create("orders");
        await uow.GetContextAsync();
        await uow.CommitAsync();

        contexts.Created["orders"].CommitCount.Should().Be(1);
    }
}
