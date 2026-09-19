using System.Linq.Expressions;
using System.Reflection;
using MongoDB.Driver;
using Pottmayer.Tars.Core.Ddd;
using Pottmayer.Tars.Data.Abstractions.DataContext;
using Pottmayer.Tars.Data.Abstractions.Query;
using Pottmayer.Tars.Data.Query;

namespace Pottmayer.Tars.Data.Document.MongoDB.Repositories;

/// <summary>
/// Standard async repository over a MongoDB collection, implementing the provider-agnostic
/// <see cref="Pottmayer.Tars.Data.Abstractions.Repositories.IStandardRepository{TEntity, TKey}"/> so domain
/// repository interfaces bind to it exactly as they bind to the relational implementation.
/// <para>
/// Concrete repositories supply the key member via <see cref="IdSelector"/> and, to opt into dynamic
/// <see cref="ExecuteQueryAsync"/> filtering/sorting, override <see cref="AllowedQueryFields"/>.
/// </para>
/// </summary>
/// <typeparam name="TEntity">The document type managed by the repository.</typeparam>
/// <typeparam name="TKey">The document's key type.</typeparam>
public abstract class MongoStandardRepository<TEntity, TKey>
    : MongoRepositoryBase, Pottmayer.Tars.Data.Abstractions.Repositories.IStandardRepository<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    private Func<TEntity, TKey>? _keyAccessor;

    /// <summary>Initializes the repository over the ambient MongoDB data context.</summary>
    /// <param name="accessor">Accessor providing the ambient data context.</param>
    protected MongoStandardRepository(IDataContextAccessor accessor) : base(accessor) { }

    /// <summary>Name of the MongoDB collection backing this repository. Defaults to the entity type name.</summary>
    protected virtual string CollectionName => typeof(TEntity).Name;

    /// <summary>Expression selecting the entity's key member, used to build id filters.</summary>
    protected abstract Expression<Func<TEntity, TKey>> IdSelector { get; }

    /// <summary>
    /// Whitelist of property names allowed for dynamic filter and sort in <see cref="ExecuteQueryAsync"/>.
    /// Default: empty (no dynamic filtering allowed). Override in the concrete repository to opt in.
    /// </summary>
    protected virtual IReadOnlySet<string> AllowedQueryFields { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>The typed collection bound to this repository.</summary>
    protected IMongoCollection<TEntity> Collection => Database.GetCollection<TEntity>(CollectionName);

    private Func<TEntity, TKey> KeyOf => _keyAccessor ??= IdSelector.Compile();

    private FilterDefinition<TEntity> ByKey(TKey key) => Builders<TEntity>.Filter.Eq(IdSelector, key);

    private static FilterDefinition<TEntity> ToFilter(Expression<Func<TEntity, bool>>? predicate)
        => predicate is null ? Builders<TEntity>.Filter.Empty : Builders<TEntity>.Filter.Where(predicate);

    // ── Get ──

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<TEntity>> GetAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
    {
        var session = await SessionAsync(ct).ConfigureAwait(false);
        return await Collection.Find(session, ToFilter(predicate)).ToListAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken ct = default)
    {
        var session = await SessionAsync(ct).ConfigureAwait(false);
        return await Collection.Find(session, Builders<TEntity>.Filter.Empty).ToListAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default)
    {
        var session = await SessionAsync(ct).ConfigureAwait(false);
        return await Collection.Find(session, ByKey(id)).FirstOrDefaultAsync(ct).ConfigureAwait(false);
    }

    // ── Add ──

    /// <inheritdoc/>
    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var session = await SessionAsync(ct).ConfigureAwait(false);
        await Collection.InsertOneAsync(session, entity, null, ct).ConfigureAwait(false);
        TrackIfAggregate(entity);
        return entity;
    }

    /// <inheritdoc/>
    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        var list = entities as IReadOnlyList<TEntity> ?? entities.ToList();
        if (list.Count == 0) return;
        var session = await SessionAsync(ct).ConfigureAwait(false);
        await Collection.InsertManyAsync(session, list, null, ct).ConfigureAwait(false);
        foreach (var entity in list) TrackIfAggregate(entity);
    }

    // ── Update ──

    /// <inheritdoc/>
    public virtual async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var session = await SessionAsync(ct).ConfigureAwait(false);
        await Collection.ReplaceOneAsync(session, ByKey(KeyOf(entity)), entity, new ReplaceOptions { IsUpsert = false }, ct).ConfigureAwait(false);
        TrackIfAggregate(entity);
        return entity;
    }

    /// <inheritdoc/>
    public virtual async Task UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        var session = await SessionAsync(ct).ConfigureAwait(false);
        foreach (var entity in entities)
        {
            await Collection.ReplaceOneAsync(session, ByKey(KeyOf(entity)), entity, new ReplaceOptions { IsUpsert = false }, ct).ConfigureAwait(false);
            TrackIfAggregate(entity);
        }
    }

    // ── Remove ──

    /// <inheritdoc/>
    public virtual async Task<TEntity?> RemoveByKeyAsync(TKey key, CancellationToken ct = default)
    {
        var session = await SessionAsync(ct).ConfigureAwait(false);
        var entity = await Collection.Find(session, ByKey(key)).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (entity is null) return null;
        await Collection.DeleteOneAsync(session, ByKey(key), (DeleteOptions?)null, ct).ConfigureAwait(false);
        TrackIfAggregate(entity);
        return entity;
    }

    /// <inheritdoc/>
    public virtual async Task<TEntity> RemoveAsync(TEntity entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var session = await SessionAsync(ct).ConfigureAwait(false);
        await Collection.DeleteOneAsync(session, ByKey(KeyOf(entity)), (DeleteOptions?)null, ct).ConfigureAwait(false);
        TrackIfAggregate(entity);
        return entity;
    }

    /// <inheritdoc/>
    public virtual async Task RemoveRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        var session = await SessionAsync(ct).ConfigureAwait(false);
        foreach (var entity in entities)
        {
            await Collection.DeleteOneAsync(session, ByKey(KeyOf(entity)), (DeleteOptions?)null, ct).ConfigureAwait(false);
            TrackIfAggregate(entity);
        }
    }

    // ── Exists / Count / Any ──

    /// <inheritdoc/>
    public virtual async Task<bool> ExistsKeyAsync(TKey key, CancellationToken ct = default)
    {
        var session = await SessionAsync(ct).ConfigureAwait(false);
        return await Collection.Find(session, ByKey(key)).Limit(1).AnyAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
    {
        var session = await SessionAsync(ct).ConfigureAwait(false);
        return await Collection.Find(session, ToFilter(predicate)).Limit(1).AnyAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
    {
        var session = await SessionAsync(ct).ConfigureAwait(false);
        var count = await Collection.CountDocumentsAsync(session, ToFilter(predicate), null, ct).ConfigureAwait(false);
        return (int)count;
    }

    /// <inheritdoc/>
    public virtual Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
        => ExistsAsync(predicate, ct);

    // ── First ──

    /// <inheritdoc/>
    public virtual async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
    {
        var session = await SessionAsync(ct).ConfigureAwait(false);
        return await Collection.Find(session, ToFilter(predicate)).FirstOrDefaultAsync(ct).ConfigureAwait(false);
    }

    // ── Paged ──

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<TEntity>> GetPagedAsync(int skip, int take, Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
    {
        var session = await SessionAsync(ct).ConfigureAwait(false);
        return await Collection.Find(session, ToFilter(predicate)).Skip(skip).Limit(take).ToListAsync(ct).ConfigureAwait(false);
    }

    // ── Dynamic query ──

    /// <inheritdoc/>
    public virtual async Task<DataQueryResult<TEntity>> ExecuteQueryAsync(QueryParams? queryParams = null, CancellationToken ct = default)
    {
        var session = await SessionAsync(ct).ConfigureAwait(false);
        var dp = queryParams.ToDataQueryParams<TEntity>(AllowedQueryFields);
        var filter = dp?.Predicate is { } predicate ? Builders<TEntity>.Filter.Where(predicate) : Builders<TEntity>.Filter.Empty;

        var total = await Collection.CountDocumentsAsync(session, filter, null, ct).ConfigureAwait(false);

        var find = Collection.Find(session, filter);
        if (dp?.OrderBy is { Count: > 0 } orderBy)
            find = find.Sort(BuildSort(orderBy));
        if (dp?.Skip is { } skip) find = find.Skip(skip);
        if (dp?.Take is { } take) find = find.Limit(take);

        var items = await find.ToListAsync(ct).ConfigureAwait(false);
        return new DataQueryResult<TEntity> { Items = items, TotalCount = total };
    }

    private static SortDefinition<TEntity> BuildSort(IReadOnlyList<SortOption> orderBy)
    {
        var builder = Builders<TEntity>.Sort;
        var definitions = new List<SortDefinition<TEntity>>(orderBy.Count);
        foreach (var option in orderBy)
        {
            var field = MemberSelector(option.PropertyName);
            definitions.Add(option.Descending ? builder.Descending(field) : builder.Ascending(field));
        }
        return definitions.Count == 1 ? definitions[0] : builder.Combine(definitions);
    }

    private static Expression<Func<TEntity, object>> MemberSelector(string propertyName)
    {
        var param = Expression.Parameter(typeof(TEntity), "x");
        var prop = GetProperty(typeof(TEntity), propertyName)
            ?? throw new ArgumentException($"Property '{propertyName}' not found on '{typeof(TEntity).Name}'.", nameof(propertyName));
        var access = Expression.Property(param, prop);
        return Expression.Lambda<Func<TEntity, object>>(Expression.Convert(access, typeof(object)), param);
    }

    private static PropertyInfo? GetProperty(Type type, string name)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly;
        for (var t = type; t != null; t = t.BaseType)
        {
            var p = t.GetProperty(name, flags);
            if (p is not null) return p;
        }
        return null;
    }

    private void TrackIfAggregate(TEntity entity)
    {
        if (entity is IHasDomainEvents aggregate)
            Track(aggregate);
    }
}
