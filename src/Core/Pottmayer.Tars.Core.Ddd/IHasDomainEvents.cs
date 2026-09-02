namespace Pottmayer.Tars.Core.Ddd;

/// <summary>
/// Implemented by aggregates that raise domain events. Used by persistence to collect and dispatch events after commit.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>Returns the pending domain events and clears them from the aggregate in one shot.</summary>
    /// <returns>The domain events that were pending before the call.</returns>
    IReadOnlyList<IDomainEvent> TakeDomainEvents();
}
