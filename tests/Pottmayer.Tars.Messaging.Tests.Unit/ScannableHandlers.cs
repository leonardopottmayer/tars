using Pottmayer.Tars.Messaging.Abstractions;

namespace Pottmayer.Tars.Messaging.Tests.Unit;

// Public so an assembly scan can find them: GetExportedTypes ignores internal and nested-private
// types, which is what handler registration relies on.

public sealed class FirstMfaEnabledHandler : IIntegrationEventHandler<MfaEnabled>
{
    public Task HandleAsync(MfaEnabled @event, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

/// <summary>A second handler for the same event — neither may be swallowed by registration.</summary>
public sealed class SecondMfaEnabledHandler : IIntegrationEventHandler<MfaEnabled>
{
    public Task HandleAsync(MfaEnabled @event, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
