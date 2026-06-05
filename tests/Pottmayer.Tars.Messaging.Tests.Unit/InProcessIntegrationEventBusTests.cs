using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Messaging.Abstractions;
using Pottmayer.Tars.Messaging.DI;

namespace Pottmayer.Tars.Messaging.Tests.Unit;

public class InProcessIntegrationEventBusTests
{
    private static TestEvent Event(string payload = "hi")
        => new(Guid.CreateVersion7(), DateTimeOffset.UtcNow, payload);

    private static ServiceProvider Build(Action<IServiceCollection> registerHandlers)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Recorder>();
        services.AddTarsInProcessIntegrationEventBus();
        registerHandlers(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task PublishAsync_invokes_the_registered_handler()
    {
        using var sp = Build(s =>
            s.AddScoped<IIntegrationEventHandler<TestEvent>, FirstHandler>());

        await sp.GetRequiredService<IIntegrationEventBus>().PublishAsync(Event("hello"));

        sp.GetRequiredService<Recorder>().Handled.Should().ContainSingle().Which.Should().Be("A:hello");
    }

    [Fact]
    public async Task PublishAsync_invokes_every_handler_subscribed_to_the_event()
    {
        using var sp = Build(s =>
        {
            s.AddScoped<IIntegrationEventHandler<TestEvent>, FirstHandler>();
            s.AddScoped<IIntegrationEventHandler<TestEvent>, SecondHandler>();
        });

        await sp.GetRequiredService<IIntegrationEventBus>().PublishAsync(Event("x"));

        sp.GetRequiredService<Recorder>().Handled.Should().BeEquivalentTo("A:x", "B:x");
    }

    [Fact]
    public async Task PublishAsync_swallows_a_failing_handler_and_still_runs_the_others()
    {
        using var sp = Build(s =>
        {
            s.AddScoped<IIntegrationEventHandler<TestEvent>, ThrowingHandler>();
            s.AddScoped<IIntegrationEventHandler<TestEvent>, FirstHandler>();
        });

        var bus = sp.GetRequiredService<IIntegrationEventBus>();

        var act = () => bus.PublishAsync(Event("survive"));

        await act.Should().NotThrowAsync();
        sp.GetRequiredService<Recorder>().Handled.Should().ContainSingle().Which.Should().Be("A:survive");
    }

    [Fact]
    public async Task PublishAsync_is_a_noop_when_no_handler_is_registered()
    {
        using var sp = Build(_ => { });

        var act = () => sp.GetRequiredService<IIntegrationEventBus>()
            .PublishAsync(new OrphanEvent(Guid.CreateVersion7(), DateTimeOffset.UtcNow));

        await act.Should().NotThrowAsync();
        sp.GetRequiredService<Recorder>().Handled.Should().BeEmpty();
    }

    [Fact]
    public async Task PublishAsync_throws_when_the_event_is_null()
    {
        using var sp = Build(_ => { });

        var act = () => sp.GetRequiredService<IIntegrationEventBus>().PublishAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
