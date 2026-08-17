using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.Broker.Dispatch;

namespace Pottmayer.Tars.Messaging.Tests.Unit;

public class ScopedIntegrationEventDispatcherTests
{
    private sealed class Recorder
    {
        public List<string> Calls { get; } = [];
    }

    private sealed class FirstHandler(Recorder recorder) : IIntegrationEventHandler<MfaEnabled>
    {
        public Task HandleAsync(MfaEnabled @event, CancellationToken cancellationToken = default)
        {
            recorder.Calls.Add(nameof(FirstHandler));
            return Task.CompletedTask;
        }
    }

    private sealed class FailingHandler(Recorder recorder) : IIntegrationEventHandler<MfaEnabled>
    {
        public Task HandleAsync(MfaEnabled @event, CancellationToken cancellationToken = default)
        {
            recorder.Calls.Add(nameof(FailingHandler));
            throw new InvalidOperationException("handler blew up");
        }
    }

    private sealed class NeverReachedHandler(Recorder recorder) : IIntegrationEventHandler<MfaEnabled>
    {
        public Task HandleAsync(MfaEnabled @event, CancellationToken cancellationToken = default)
        {
            recorder.Calls.Add(nameof(NeverReachedHandler));
            return Task.CompletedTask;
        }
    }

    private static (IIntegrationEventDispatcher Dispatcher, Recorder Recorder) Build(
        params Type[] handlerTypes)
    {
        var recorder = new Recorder();
        var services = new ServiceCollection();
        services.AddSingleton(recorder);

        foreach (var type in handlerTypes)
            services.AddScoped(typeof(IIntegrationEventHandler<MfaEnabled>), type);

        var provider = services.BuildServiceProvider();

        var dispatcher = new ScopedIntegrationEventDispatcher(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<ScopedIntegrationEventDispatcher>.Instance);

        return (dispatcher, recorder);
    }

    private static MfaEnabled Event() => new(Guid.NewGuid(), DateTimeOffset.UtcNow);

    [Fact]
    public async Task Invokes_every_registered_handler()
    {
        var (dispatcher, recorder) = Build(typeof(FirstHandler), typeof(NeverReachedHandler));

        await dispatcher.DispatchAsync(Event());

        recorder.Calls.Should().Equal(nameof(FirstHandler), nameof(NeverReachedHandler));
    }

    [Fact]
    public async Task Propagates_a_handler_failure_so_the_transport_can_retry()
    {
        // The opposite of the in-process bus, which swallows. On a broker, swallowing would turn a
        // durable queue back into fire-and-forget.
        var (dispatcher, _) = Build(typeof(FailingHandler));

        var act = () => dispatcher.DispatchAsync(Event());

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("handler blew up");
    }

    [Fact]
    public async Task Stops_at_the_first_failure()
    {
        var (dispatcher, recorder) = Build(typeof(FirstHandler), typeof(FailingHandler), typeof(NeverReachedHandler));

        var act = () => dispatcher.DispatchAsync(Event());
        await act.Should().ThrowAsync<InvalidOperationException>();

        recorder.Calls.Should().Equal(nameof(FirstHandler), nameof(FailingHandler));
        recorder.Calls.Should().NotContain(nameof(NeverReachedHandler));
    }

    [Fact]
    public async Task An_event_with_no_handler_is_acknowledged_rather_than_failed()
    {
        // A queue can legitimately receive an event this service does not act on.
        var (dispatcher, recorder) = Build();

        await dispatcher.DispatchAsync(Event());

        recorder.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Rejects_a_null_event()
    {
        var (dispatcher, _) = Build();

        var act = () => dispatcher.DispatchAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
