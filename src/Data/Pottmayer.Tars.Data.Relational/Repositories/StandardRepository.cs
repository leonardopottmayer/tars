using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Abstractions.Query;
using Pottmayer.Tars.Data.Relational.Abstractions.Repositories;
using Pottmayer.Tars.Data.Relational.Extensions;

namespace Pottmayer.Tars.Data.Relational.Repositories;

/// <summary>
/// Standard async repository using EF Core. Dapper is available in subclasses via
/// <see cref="RepositoryBase.Connection"/> for complex read queries on the same connection/transaction.
/// <para>
/// Override <see cref="AllowedQueryFields"/> to whitelist property names for <see cref="ExecuteQueryAsync"/>.
/// Fields not in the whitelist are silently ignored — protecting against arbitrary filter injection.
/// </para>
/// </summary>
public class StandardRepository<TEntity, TKey> : RepositoryBase, IStandardRepository<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    public StandardRepository(IDataContextAccessor accessor) : base(accessor) { }

    /// <summary>
    /// Whitelist of property names allowed for dynamic filter and sort in <see cref="ExecuteQueryAsync"/>.
    /// Default: empty (no dynamic filtering allowed). Override in the concrete repository to opt in.
    /// </summary>
    protected virtual IReadOnlySet<string> AllowedQueryFields { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    protected DbSet<TEntity> Set => DbContext.Set<TEntity>();

    // ── Queryable ──

    public virtual IQueryable<TEntity> Queryable(Expression<Func<TEntity, bool>>? predicate = null)
    {
        var q = Set.AsQueryable();
        return predicate is null ? q : q.Where(predicate);
    }

    // ── Get ──

    public virtual async Task<IEnumerable<TEntity>> GetAsync(
        Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
    {
        return await Queryable(predicate).ToListAsync(ct).ConfigureAwait(false);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await Set.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
    }

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default)
    {
        return await Set.FindAsync([id], ct).ConfigureAwait(false);
    }

    // ── Add ──

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var entry = await Set.AddAsync(entity, ct).ConfigureAwait(false);
        return entry.Entity;
    }

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        await Set.AddRangeAsync(entities, ct).ConfigureAwait(false);
    }

    // ── Update ──

    public virtual Task<TEntity> UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return Task.FromResult(Set.Update(entity).Entity);
    }

    public virtual Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        Set.UpdateRange(entities);
        return Task.CompletedTask;
    }

    // ── Remove ──

    public virtual async Task<TEntity?> RemoveByKeyAsync(TKey key, CancellationToken ct = default)
    {
        var entity = await Set.FindAsync([key], ct).ConfigureAwait(false);
        if (entity is null) return null;
        return Set.Remove(entity).Entity;
    }

    public virtual Task<TEntity> RemoveAsync(TEntity entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return Task.FromResult(Set.Remove(entity).Entity);
    }

    public virtual Task RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        Set.RemoveRange(entities);
        return Task.CompletedTask;
    }

    // ── Exists / Count / Any ──

    public virtual async Task<bool> ExistsKeyAsync(TKey key, CancellationToken ct = default)
    {
        return await Set.FindAsync([key], ct).ConfigureAwait(false) is not null;
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
    {
        var q = predicate is null ? Set.AsQueryable() : Set.Where(predicate);
        return await q.AnyAsync(ct).ConfigureAwait(false);
    }

    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
    {
        var q = predicate is null ? Set.AsQueryable() : Set.Where(predicate);
        return await q.CountAsync(ct).ConfigureAwait(false);
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
    {
        var q = predicate is null ? Set.AsQueryable() : Set.Where(predicate);
        return await q.AnyAsync(ct).ConfigureAwait(false);
    }

    // ── First ──

    public virtual async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
    {
        var q = predicate is null ? Set.AsQueryable() : Set.Where(predicate);
        return await q.FirstOrDefaultAsync(ct).ConfigureAwait(false);
    }

    // ── Paged ──

    public virtual async Task<IEnumerable<TEntity>> GetPagedAsync(
        int skip, int take, Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
    {
        var q = predicate is null ? Set.AsQueryable() : Set.Where(predicate);
        return await q.Skip(skip).Take(take).ToListAsync(ct).ConfigureAwait(false);
    }

    // ── Dynamic query ──

    public virtual async Task<DataQueryResult<TEntity>> ExecuteQueryAsync(QueryParams? queryParams = null, CancellationToken ct = default)
    {
        var dataParams = queryParams?.ToDataQueryParams<TEntity>(AllowedQueryFields);
        return await Set.AsNoTracking().ToQueryResultAsync(dataParams, ct).ConfigureAwait(false);
    }
}
