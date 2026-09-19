using Pottmayer.Tars.Data.Abstractions.Repositories;
using System.Linq.Expressions;

namespace Pottmayer.Tars.Data.Relational.Abstractions.Repositories;

/// <summary>
/// Relational extension of <see cref="IStandardRepository{TEntity, TKey}"/> that exposes the composable
/// EF Core <see cref="IQueryable{T}"/>. Only repositories that genuinely need to compose LINQ against the
/// relational store should depend on this contract — doing so couples the caller to the relational axis.
/// Provider-agnostic domain interfaces should extend the neutral <see cref="IStandardRepository{TEntity, TKey}"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type managed by the repository.</typeparam>
/// <typeparam name="TKey">The entity's key type.</typeparam>
public interface IRelationalRepository<TEntity, TKey> : IStandardRepository<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    /// <summary>
    /// Returns a composable EF Core queryable. Do not use in provider-agnostic code.
    /// </summary>
    /// <param name="predicate">Optional filter applied to the queryable.</param>
    /// <returns>A composable queryable over the entities.</returns>
    IQueryable<TEntity> Queryable(Expression<Func<TEntity, bool>>? predicate = null);
}
