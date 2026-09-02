using Pottmayer.Tars.Data.Abstractions.Query;
using Pottmayer.Tars.Data.Abstractions.Repositories;
using System.Linq.Expressions;

namespace Pottmayer.Tars.Data.Relational.Abstractions.Repositories;

/// <summary>
/// Standard CRUD/query repository over <typeparamref name="TEntity"/> keyed by <typeparamref name="TKey"/>,
/// covering get/add/update/remove, existence/count and dynamic queries.
/// </summary>
/// <typeparam name="TEntity">The entity type managed by the repository.</typeparam>
/// <typeparam name="TKey">The entity's key type.</typeparam>
public interface IStandardRepository<TEntity, TKey> : IRepository<TEntity>
    where TEntity : class
    where TKey : notnull
{
    /// <summary>
    /// Returns a composable EF Core queryable. Do not use in provider-agnostic code.
    /// </summary>
    /// <param name="predicate">Optional filter applied to the queryable.</param>
    /// <returns>A composable queryable over the entities.</returns>
    IQueryable<TEntity> Queryable(Expression<Func<TEntity, bool>>? predicate = null);

    // ── Get ──

    /// <summary>Returns the entities matching <paramref name="predicate"/> (all when null).</summary>
    /// <param name="predicate">Optional filter.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The matching entities.</returns>
    Task<IEnumerable<TEntity>> GetAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);

    /// <summary>Returns all entities.</summary>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>All entities.</returns>
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns the entity with the given key, or null when not found.</summary>
    /// <param name="id">The entity key.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The entity, or null.</returns>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);

    // ── Add ──

    /// <summary>Adds a new entity.</summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The added entity.</returns>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>Adds a range of entities.</summary>
    /// <param name="entities">The entities to add.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the entities are added.</returns>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    // ── Update ──

    /// <summary>Updates an existing entity.</summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The updated entity.</returns>
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>Updates a range of entities.</summary>
    /// <param name="entities">The entities to update.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the entities are updated.</returns>
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    // ── Remove ──

    /// <summary>Removes the entity with the given key, if it exists.</summary>
    /// <param name="key">The entity key.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The removed entity, or null when not found.</returns>
    Task<TEntity?> RemoveByKeyAsync(TKey key, CancellationToken ct = default);

    /// <summary>Removes the given entity.</summary>
    /// <param name="entity">The entity to remove.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The removed entity.</returns>
    Task<TEntity> RemoveAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>Removes a range of entities.</summary>
    /// <param name="entities">The entities to remove.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the entities are removed.</returns>
    Task RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    // ── Exists / Count / Any ──

    /// <summary>Returns whether an entity with the given key exists.</summary>
    /// <param name="key">The entity key.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns><c>true</c> when the entity exists; otherwise <c>false</c>.</returns>
    Task<bool> ExistsKeyAsync(TKey key, CancellationToken ct = default);

    /// <summary>Returns whether any entity matches <paramref name="predicate"/> (any at all when null).</summary>
    /// <param name="predicate">Optional filter.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns><c>true</c> when a match exists; otherwise <c>false</c>.</returns>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);

    /// <summary>Counts the entities matching <paramref name="predicate"/> (all when null).</summary>
    /// <param name="predicate">Optional filter.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The number of matching entities.</returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);

    /// <summary>Returns whether any entity matches <paramref name="predicate"/> (any at all when null).</summary>
    /// <param name="predicate">Optional filter.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns><c>true</c> when a match exists; otherwise <c>false</c>.</returns>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);

    // ── First ──

    /// <summary>Returns the first entity matching <paramref name="predicate"/>, or null.</summary>
    /// <param name="predicate">Optional filter.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The first matching entity, or null.</returns>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);

    // ── Paged ──

    /// <summary>Returns a page of entities matching <paramref name="predicate"/> (all when null).</summary>
    /// <param name="skip">Number of entities to skip.</param>
    /// <param name="take">Maximum number of entities to return.</param>
    /// <param name="predicate">Optional filter.</param>
    /// <param name="ct">Token used to cancel the operation.</param>
    /// <returns>The requested page of entities.</returns>
    Task<IEnumerable<TEntity>> GetPagedAsync(int skip, int take, Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);

    // ── Dynamic query — whitelist defined in repository via AllowedQueryFields ──

    /// <summary>
    /// Executes a dynamic query (filter, sort, paging) using the repository's <c>AllowedQueryFields</c> whitelist.
    /// Fields not in the whitelist are silently ignored.
    /// </summary>
    Task<DataQueryResult<TEntity>> ExecuteQueryAsync(QueryParams? queryParams = null, CancellationToken ct = default);
}
