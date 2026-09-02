namespace Pottmayer.Tars.Data.Abstractions.Query;

/// <summary>
/// Fluent container for filter, sort and paging parameters passed to <c>ExecuteQueryAsync</c>.
/// </summary>
public sealed class QueryParams
{
    /// <summary>1-based page number. Defaults to 1.</summary>
    public int Page { get; private set; } = 1;

    /// <summary>Number of items per page. Defaults to 20.</summary>
    public int PageSize { get; private set; } = 20;

    /// <summary>Whether paging has been requested via <see cref="SetPaged"/>.</summary>
    public bool Paged { get; private set; }

    /// <summary>The configured filter clauses, or null when none are set.</summary>
    public IReadOnlyList<FilterSpec>? Filters { get; private set; }

    /// <summary>The configured sort options, or null when none are set.</summary>
    public IReadOnlyList<SortOption>? OrderBy { get; private set; }

    /// <summary>Enables paging with the given page and page size (both clamped to a minimum of 1).</summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>This instance for chaining.</returns>
    public QueryParams SetPaged(int page, int pageSize)
    {
        Page = page < 1 ? 1 : page;
        PageSize = pageSize < 1 ? 1 : pageSize;
        Paged = true;
        return this;
    }

    /// <summary>Adds a single-value filter clause.</summary>
    /// <param name="field">Name of the field to filter on.</param>
    /// <param name="op">Comparison operator to apply.</param>
    /// <param name="value">Value to compare against.</param>
    /// <returns>This instance for chaining.</returns>
    public QueryParams AddFilter(string field, FilterOperator op, object? value)
    {
        var list = Filters is null
            ? new List<FilterSpec>()
            : new List<FilterSpec>(Filters);
        list.Add(new FilterSpec { Field = field, Operator = op, Value = value });
        Filters = list;
        return this;
    }

    /// <summary>Adds an <see cref="FilterOperator.In"/> filter clause matching any of <paramref name="values"/>.</summary>
    /// <param name="field">Name of the field to filter on.</param>
    /// <param name="values">The set of accepted values.</param>
    /// <returns>This instance for chaining.</returns>
    public QueryParams AddFilterIn(string field, IReadOnlyList<object?> values)
    {
        var list = Filters is null
            ? new List<FilterSpec>()
            : new List<FilterSpec>(Filters);
        list.Add(new FilterSpec { Field = field, Operator = FilterOperator.In, Values = values });
        Filters = list;
        return this;
    }

    /// <summary>Replaces the sort with a single ordering by <paramref name="propertyName"/>.</summary>
    /// <param name="propertyName">Name of the property to sort by.</param>
    /// <param name="descending">Whether to sort in descending order.</param>
    /// <returns>This instance for chaining.</returns>
    public QueryParams SetOrderBy(string propertyName, bool descending = false)
    {
        OrderBy = [new SortOption { PropertyName = propertyName, Descending = descending }];
        return this;
    }

    /// <summary>Appends a secondary ordering by <paramref name="propertyName"/>.</summary>
    /// <param name="propertyName">Name of the property to sort by.</param>
    /// <param name="descending">Whether to sort in descending order.</param>
    /// <returns>This instance for chaining.</returns>
    public QueryParams AddOrderBy(string propertyName, bool descending = false)
    {
        var list = OrderBy is null
            ? new List<SortOption>()
            : new List<SortOption>(OrderBy);
        list.Add(new SortOption { PropertyName = propertyName, Descending = descending });
        OrderBy = list;
        return this;
    }
}
