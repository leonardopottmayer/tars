namespace Pottmayer.Tars.Core.Ddd;

/// <summary>
/// Dispatches domain events after a unit of work is committed.
/// Implementations typically live in Application or Infrastructure (in-process bus, message queue, etc.).
/// The data layer calls this after commit; the interface lives in Core.Ddd so that both Data and Application can reference it.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<object> domainEvents, CancellationToken cancellationToken = default);
}
