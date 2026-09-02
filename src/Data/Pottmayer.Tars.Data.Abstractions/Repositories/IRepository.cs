namespace Pottmayer.Tars.Data.Abstractions.Repositories;

/// <summary>Non-generic marker for a repository, used for type-agnostic resolution.</summary>
public interface IRepository { }

/// <summary>Marker for a repository over aggregates/entities of type <typeparamref name="TEntity"/>.</summary>
/// <typeparam name="TEntity">The entity type the repository manages.</typeparam>
public interface IRepository<TEntity> : IRepository { }
