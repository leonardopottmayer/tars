using Pottmayer.Tars.Data.Abstractions.Query;
using Pottmayer.Tars.Data.Abstractions.Repositories;
using System.Linq.Expressions;

namespace Pottmayer.Tars.Data.Relational.Abstractions.Repositories;

public interface IStandardRepository<TEntity, TKey> : IRepository<TEntity>
    where TEntity : class
    where TKey : notnull
{
    /// <summary>
    /// Returns a composable EF Core queryable. Do not use in provider-agnostic code.
    /// </summary>
    IQueryable<TEntity> Queryable(Expression<Func<TEntity, bool>>? predicate = null);

    // ── Get ──

    Task<IEnumerable<TEntity>> GetAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);

    // ── Add ──

    Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    // ── Update ──

    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    // ── Remove ──

    Task<TEntity?> RemoveByKeyAsync(TKey key, CancellationToken ct = default);
    Task<TEntity> RemoveAsync(TEntity entity, CancellationToken ct = default);
    Task RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    // ── Exists / Count / Any ──

    Task<bool> ExistsKeyAsync(TKey key, CancellationToken ct = default);
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);

    // ── First ──

    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);

    // ── Paged ──

    Task<IEnumerable<TEntity>> GetPagedAsync(int skip, int take, Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);

    // ── Dynamic query — whitelist defined in repository via AllowedQueryFields ──

    /// <summary>
    /// Executes a dynamic query (filter, sort, paging) using the repository's <c>AllowedQueryFields</c> whitelist.
    /// Fields not in the whitelist are silently ignored.
    /// </summary>
    Task<DataQueryResult<TEntity>> ExecuteQueryAsync(QueryParams? queryParams = null, CancellationToken ct = default);
}
