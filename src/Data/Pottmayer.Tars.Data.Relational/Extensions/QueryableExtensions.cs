using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Pottmayer.Tars.Data.Abstractions.Query;

namespace Pottmayer.Tars.Data.Relational.Extensions;

/// <summary>
/// Applies <see cref="DataQueryParams{TEntity}"/> to an <see cref="IQueryable{T}"/> and
/// returns a <see cref="DataQueryResult{TEntity}"/>.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Applies the predicate, ordering and paging from <paramref name="queryParams"/> and materializes the
    /// result, including the total count before paging.
    /// </summary>
    /// <typeparam name="TEntity">The queried entity type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="queryParams">The typed query parameters, or null to return all items.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The page of items plus the total match count.</returns>
    public static async Task<DataQueryResult<TEntity>> ToQueryResultAsync<TEntity>(
        this IQueryable<TEntity> source,
        DataQueryParams<TEntity>? queryParams,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        var query = source;
        if (queryParams?.Predicate is { } predicate)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        if (queryParams?.OrderBy is { Count: > 0 } orderBy)
        {
            var first = orderBy[0];
            var ordered = query.OrderByProperty(first.PropertyName, !first.Descending);
            for (var i = 1; i < orderBy.Count; i++)
                ordered = ordered.ThenByProperty(orderBy[i].PropertyName, !orderBy[i].Descending);
            query = ordered;
        }

        if (queryParams?.Skip is { } skip) query = query.Skip(skip);
        if (queryParams?.Take is { } take) query = query.Take(take);

        var items = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        return new DataQueryResult<TEntity> { Items = items, TotalCount = totalCount };
    }

    /// <summary>Orders the queryable by a property named at runtime.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source queryable.</param>
    /// <param name="propertyName">Name of the property to order by.</param>
    /// <param name="ascending">Whether to order ascending.</param>
    /// <returns>The ordered queryable.</returns>
    public static IOrderedQueryable<T> OrderByProperty<T>(this IQueryable<T> source, string propertyName, bool ascending)
    {
        var (param, access) = PropertyAccess(typeof(T), propertyName);
        var lambda = Expression.Lambda(access, param);
        var method = ascending ? "OrderBy" : "OrderByDescending";
        var call = Expression.Call(typeof(Queryable), method, [typeof(T), access.Type],
            source.Expression, Expression.Quote(lambda));
        return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
    }

    /// <summary>Adds a secondary ordering to the queryable by a property named at runtime.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The already-ordered queryable.</param>
    /// <param name="propertyName">Name of the property to order by.</param>
    /// <param name="ascending">Whether to order ascending.</param>
    /// <returns>The ordered queryable.</returns>
    public static IOrderedQueryable<T> ThenByProperty<T>(this IOrderedQueryable<T> source, string propertyName, bool ascending)
    {
        var (param, access) = PropertyAccess(typeof(T), propertyName);
        var lambda = Expression.Lambda(access, param);
        var method = ascending ? "ThenBy" : "ThenByDescending";
        var call = Expression.Call(typeof(Queryable), method, [typeof(T), access.Type],
            source.Expression, Expression.Quote(lambda));
        return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
    }

    /// <summary>Builds the parameter and member-access expressions for a property named at runtime.</summary>
    private static (ParameterExpression, MemberExpression) PropertyAccess(Type entityType, string name)
    {
        var param = Expression.Parameter(entityType, "x");
        var prop = GetProperty(entityType, name)
            ?? throw new ArgumentException($"Property '{name}' not found on '{entityType.Name}'.", nameof(name));
        return (param, Expression.Property(param, prop));
    }

    /// <summary>Finds a public instance property by name (case-insensitive), walking the base types.</summary>
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
}
