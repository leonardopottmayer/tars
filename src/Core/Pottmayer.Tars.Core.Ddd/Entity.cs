namespace Pottmayer.Tars.Core.Ddd;

/// <summary>
/// Base type for domain entities with identity.
/// </summary>
/// <typeparam name="TKey">The type of the entity's identifier.</typeparam>
public abstract class Entity<TKey> where TKey : notnull
{
    public virtual TKey Id { get; protected set; } = default!;

    protected Entity() { }
    protected Entity(TKey id)
    {
        Id = id;
    }
}
