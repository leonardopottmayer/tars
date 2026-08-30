using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Ddd;
using Pottmayer.Tars.Messaging.EntityFrameworkCore.Outbox;

namespace Pottmayer.Tars.Messaging.Tests.Unit.Outbox;

public sealed record UserRegistered(Guid UserId) : IDomainEvent;

internal sealed class RecordingUserRegisteredHandler : IDomainEventHandler<UserRegistered>
{
    public List<UserRegistered> Handled { get; } = [];
    public Task HandleAsync(UserRegistered domainEvent, CancellationToken cancellationToken = default)
    {
        Handled.Add(domainEvent);
        return Task.CompletedTask;
    }
}

internal sealed class ThrowingUserRegisteredHandler : IDomainEventHandler<UserRegistered>
{
    public Task HandleAsync(UserRegistered domainEvent, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("translator failed");
}

public class OutboxDomainEventDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_invokes_registered_handlers_for_the_runtime_type()
    {
        var handler = new RecordingUserRegisteredHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IDomainEventHandler<UserRegistered>>(handler);
        using var provider = services.BuildServiceProvider();

        var dispatcher = new OutboxDomainEventDispatcher(provider);
        var evt = new UserRegistered(Guid.NewGuid());

        await dispatcher.DispatchAsync([evt]);

        handler.Handled.Should().ContainSingle().Which.Should().Be(evt);
    }

    [Fact]
    public async Task DispatchAsync_propagates_handler_failures_so_the_commit_aborts()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDomainEventHandler<UserRegistered>>(new ThrowingUserRegisteredHandler());
        using var provider = services.BuildServiceProvider();

        var dispatcher = new OutboxDomainEventDispatcher(provider);

        var act = () => dispatcher.DispatchAsync([new UserRegistered(Guid.NewGuid())]);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("translator failed");
    }
}
