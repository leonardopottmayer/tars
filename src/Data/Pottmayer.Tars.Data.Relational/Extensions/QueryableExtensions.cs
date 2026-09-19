using Microsoft.EntityFrameworkCore;
using Pottmayer.Tars.Data.Abstractions.Query;
using Pottmayer.Tars.Data.Query;

namespace Pottmayer.Tars.Data.Relational.Extensions;

/// <summary>
/// Applies <see cref="DataQueryParams{TEntity}"/> to an EF Core <see cref="IQueryable{T}"/> and
/// materializes a <see cref="DataQueryResult{TEntity}"/> (page of items + total count). The ordering
/// composition is provider-agnostic (see <see cref="Pottmayer.Tars.Data.Query.QueryableExtensions"/>);
/// only the async materialization (<c>CountAsync</c>/<c>ToListAsync</c>) is EF-specific.
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
}
