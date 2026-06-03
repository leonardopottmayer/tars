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

    public static IOrderedQueryable<T> OrderByProperty<T>(this IQueryable<T> source, string propertyName, bool ascending)
    {
        var (param, access) = PropertyAccess(typeof(T), propertyName);
        var lambda = Expression.Lambda(access, param);
        var method = ascending ? "OrderBy" : "OrderByDescending";
        var call = Expression.Call(typeof(Queryable), method, [typeof(T), access.Type],
            source.Expression, Expression.Quote(lambda));
        return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
    }

    public static IOrderedQueryable<T> ThenByProperty<T>(this IOrderedQueryable<T> source, string propertyName, bool ascending)
    {
        var (param, access) = PropertyAccess(typeof(T), propertyName);
        var lambda = Expression.Lambda(access, param);
        var method = ascending ? "ThenBy" : "ThenByDescending";
        var call = Expression.Call(typeof(Queryable), method, [typeof(T), access.Type],
            source.Expression, Expression.Quote(lambda));
        return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
    }

    private static (ParameterExpression, MemberExpression) PropertyAccess(Type entityType, string name)
    {
        var param = Expression.Parameter(entityType, "x");
        var prop = GetProperty(entityType, name)
            ?? throw new ArgumentException($"Property '{name}' not found on '{entityType.Name}'.", nameof(name));
        return (param, Expression.Property(param, prop));
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
}
