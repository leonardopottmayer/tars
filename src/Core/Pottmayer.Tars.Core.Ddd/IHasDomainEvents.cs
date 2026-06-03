namespace Pottmayer.Tars.Core.Ddd;

/// <summary>
/// Implemented by aggregates that raise domain events. Used by persistence to collect and dispatch events after commit.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyList<IDomainEvent> TakeDomainEvents();
}
