namespace Pottmayer.Tars.Core.Ddd;

/// <summary>
/// Base type for aggregate roots. Holds domain events that can be dispatched after commit.
/// </summary>
/// <typeparam name="TKey">The type of the aggregate's identifier.</typeparam>
public abstract class AggregateRoot<TKey> : Entity<TKey>, IHasDomainEvents where TKey : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Read-only collection of domain events raised by this aggregate.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Initializes a new aggregate without setting its identifier.</summary>
    protected AggregateRoot() { }

    /// <summary>Initializes a new aggregate with the given identifier.</summary>
    /// <param name="id">The aggregate's identifier.</param>
    protected AggregateRoot(TKey id) : base(id) { }

    /// <summary>
    /// Adds a domain event to be dispatched after the unit of work is committed.
    /// </summary>
    protected void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events (e.g. after they have been dispatched).
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Returns and clears domain events in one shot. Used by persistence to dispatch without mutating during enumeration.
    /// </summary>
    public IReadOnlyList<IDomainEvent> TakeDomainEvents()
    {
        var snapshot = _domainEvents.ToArray();
        _domainEvents.Clear();
        return snapshot;
    }
}
