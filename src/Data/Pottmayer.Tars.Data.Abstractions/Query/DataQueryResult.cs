namespace Pottmayer.Tars.Data.Abstractions.Query;

/// <summary>Result of a query: the page of items plus the total count of matches.</summary>
/// <typeparam name="TEntity">The queried entity type.</typeparam>
public sealed class DataQueryResult<TEntity>
{
    /// <summary>The items in the current page (or all items when the query is not paged).</summary>
    public required IReadOnlyList<TEntity> Items { get; init; }

    /// <summary>Total number of items matching the query, ignoring paging.</summary>
    public long TotalCount { get; init; }
}
