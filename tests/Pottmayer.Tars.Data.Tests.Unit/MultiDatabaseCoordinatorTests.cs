using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Pottmayer.Tars.Data.Abstractions.UnitOfWork;
using Pottmayer.Tars.Data.Relational.Abstractions.MultiDb;
using Pottmayer.Tars.Data.Relational.DI;

namespace Pottmayer.Tars.Data.Tests.Unit;

public class MultiDatabaseCoordinatorTests
{
    private static (IMultiDatabaseCoordinator coordinator, Dictionary<string, Mock<IUnitOfWork>> units) Build()
    {
        var units = new Dictionary<string, Mock<IUnitOfWork>>();
        var factory = new Mock<IUnitOfWorkFactory>();
        factory.Setup(f => f.Create(It.IsAny<string>()))
            .Returns((string key) =>
            {
                var uow = new Mock<IUnitOfWork>();
                uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
                uow.Setup(u => u.DisposeAsync()).Returns(ValueTask.CompletedTask);
                units[key] = uow;
                return uow.Object;
            });

        var provider = new ServiceCollection()
            .AddSingleton(factory.Object)
            .AddTarsMultiDatabaseCoordination()
            .BuildServiceProvider();

        return (provider.GetRequiredService<IMultiDatabaseCoordinator>(), units);
    }

    [Fact]
    public async Task Commits_each_unit_of_work_on_success()
    {
        var (coordinator, units) = Build();

        await coordinator.ExecuteAsync(
            ["central", "primary"],
            (ctx, ct) =>
            {
                ctx.GetUnitOfWork("central");
                ctx.GetUnitOfWork("primary");
                return Task.CompletedTask;
            });

        units["central"].Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        units["primary"].Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        units["central"].Verify(u => u.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task On_failure_runs_compensation_and_rethrows()
    {
        var (coordinator, units) = Build();
        var compensated = false;

        var act = async () => await coordinator.ExecuteAsync(
            ["central"],
            (ctx, ct) =>
            {
                ctx.GetUnitOfWork("central");
                throw new InvalidOperationException("boom");
            },
            compensate: (ctx, ex, ct) => { compensated = true; return Task.CompletedTask; });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        compensated.Should().BeTrue();
        units["central"].Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        units["central"].Verify(u => u.DisposeAsync(), Times.Once);
    }
}
