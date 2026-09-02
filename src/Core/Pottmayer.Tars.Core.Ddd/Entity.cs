namespace Pottmayer.Tars.Core.Ddd;

/// <summary>
/// Base type for domain entities with identity.
/// </summary>
/// <typeparam name="TKey">The type of the entity's identifier.</typeparam>
public abstract class Entity<TKey> where TKey : notnull
{
    /// <summary>The entity's identifier.</summary>
    public virtual TKey Id { get; protected set; } = default!;

    /// <summary>Initializes a new entity without setting its identifier.</summary>
    protected Entity() { }

    /// <summary>Initializes a new entity with the given identifier.</summary>
    /// <param name="id">The entity's identifier.</param>
    protected Entity(TKey id)
    {
        Id = id;
    }
}
