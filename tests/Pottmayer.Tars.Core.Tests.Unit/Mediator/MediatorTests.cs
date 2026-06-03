using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Core.Mediator.Abstractions;
using Pottmayer.Tars.Core.Mediator.Abstractions.Messaging;
using Pottmayer.Tars.Core.Mediator.Abstractions.Notifications;
using Pottmayer.Tars.Core.Mediator.Abstractions.Pipeline;

namespace Pottmayer.Tars.Core.Tests.Unit.Mediator;

public class MediatorTests
{
    private sealed record Ping(string Text) : IRequest<string>;

    private sealed class PingHandler : IRequestHandler<Ping, string>
    {
        public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult($"pong:{request.Text}");
    }

    private sealed class AppendBehavior(string tag, List<string> log) : IPipelineBehavior<Ping, string>
    {
        public async ValueTask<string> Handle(Ping request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken = default)
        {
            log.Add($"before:{tag}");
            var response = await next();
            log.Add($"after:{tag}");
            return response + $"|{tag}";
        }
    }

    private sealed record Notified : INotification;

    private sealed class CounterHandler(List<string> log, string name) : INotificationHandler<Notified>
    {
        public ValueTask Handle(Notified notification, CancellationToken cancellationToken = default)
        {
            log.Add(name);
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task Send_dispatches_to_the_registered_handler()
    {
        var provider = new ServiceCollection()
            .AddTransient<IRequestHandler<Ping, string>, PingHandler>()
            .BuildServiceProvider();
        var mediator = new global::Pottmayer.Tars.Core.Mediator.Mediator(provider);

        var response = await mediator.Send(new Ping("hi"));

        response.Should().Be("pong:hi");
    }

    [Fact]
    public async Task Send_runs_behaviors_in_registration_order_outermost_first()
    {
        var log = new List<string>();
        var provider = new ServiceCollection()
            .AddTransient<IRequestHandler<Ping, string>, PingHandler>()
            .AddSingleton<IPipelineBehavior<Ping, string>>(new AppendBehavior("A", log))
            .AddSingleton<IPipelineBehavior<Ping, string>>(new AppendBehavior("B", log))
            .BuildServiceProvider();
        var mediator = new global::Pottmayer.Tars.Core.Mediator.Mediator(provider);

        var response = await mediator.Send(new Ping("x"));

        // A is registered first => outermost
        log.Should().Equal("before:A", "before:B", "after:B", "after:A");
        response.Should().Be("pong:x|B|A");
    }

    [Fact]
    public async Task Send_with_null_request_throws()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        var mediator = new global::Pottmayer.Tars.Core.Mediator.Mediator(provider);

        var act = async () => await mediator.Send<string>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Publish_invokes_all_registered_notification_handlers()
    {
        var log = new List<string>();
        var provider = new ServiceCollection()
            .AddSingleton<INotificationHandler<Notified>>(new CounterHandler(log, "h1"))
            .AddSingleton<INotificationHandler<Notified>>(new CounterHandler(log, "h2"))
            .BuildServiceProvider();
        var mediator = new global::Pottmayer.Tars.Core.Mediator.Mediator(provider);

        await mediator.Publish(new Notified());

        log.Should().BeEquivalentTo("h1", "h2");
    }

    [Fact]
    public async Task Publish_with_no_handlers_is_a_no_op()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        var mediator = new global::Pottmayer.Tars.Core.Mediator.Mediator(provider);

        var act = async () => await mediator.Publish(new Notified());

        await act.Should().NotThrowAsync();
    }
}
