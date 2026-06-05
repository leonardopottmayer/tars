using Pottmayer.Tars.Messaging.Abstractions;

namespace Pottmayer.Tars.Messaging.Tests.Unit;

/// <summary>A broker-ready test integration event.</summary>
public sealed record TestEvent(Guid EventId, DateTimeOffset OccurredAt, string Payload) : IIntegrationEvent;

/// <summary>An event with no registered handlers.</summary>
public sealed record OrphanEvent(Guid EventId, DateTimeOffset OccurredAt) : IIntegrationEvent;

/// <summary>Singleton sink the handlers append to, so tests can observe what ran and in which order.</summary>
public sealed class Recorder
{
    public List<string> Handled { get; } = [];
}

public sealed class FirstHandler(Recorder recorder) : IIntegrationEventHandler<TestEvent>
{
    public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
    {
        recorder.Handled.Add($"A:{@event.Payload}");
        return Task.CompletedTask;
    }
}

public sealed class SecondHandler(Recorder recorder) : IIntegrationEventHandler<TestEvent>
{
    public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
    {
        recorder.Handled.Add($"B:{@event.Payload}");
        return Task.CompletedTask;
    }
}

public sealed class ThrowingHandler : IIntegrationEventHandler<TestEvent>
{
    public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("boom");
}
