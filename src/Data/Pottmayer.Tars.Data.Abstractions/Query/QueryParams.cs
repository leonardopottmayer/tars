namespace Pottmayer.Tars.Data.Abstractions.Query;

/// <summary>
/// Fluent container for filter, sort and paging parameters passed to <c>ExecuteQueryAsync</c>.
/// </summary>
public sealed class QueryParams
{
    public int Page { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public bool Paged { get; private set; }
    public IReadOnlyList<FilterSpec>? Filters { get; private set; }
    public IReadOnlyList<SortOption>? OrderBy { get; private set; }

    public QueryParams SetPaged(int page, int pageSize)
    {
        Page = page < 1 ? 1 : page;
        PageSize = pageSize < 1 ? 1 : pageSize;
        Paged = true;
        return this;
    }

    public QueryParams AddFilter(string field, FilterOperator op, object? value)
    {
        var list = Filters is null
            ? new List<FilterSpec>()
            : new List<FilterSpec>(Filters);
        list.Add(new FilterSpec { Field = field, Operator = op, Value = value });
        Filters = list;
        return this;
    }

    public QueryParams AddFilterIn(string field, IReadOnlyList<object?> values)
    {
        var list = Filters is null
            ? new List<FilterSpec>()
            : new List<FilterSpec>(Filters);
        list.Add(new FilterSpec { Field = field, Operator = FilterOperator.In, Values = values });
        Filters = list;
        return this;
    }

    public QueryParams SetOrderBy(string propertyName, bool descending = false)
    {
        OrderBy = [new SortOption { PropertyName = propertyName, Descending = descending }];
        return this;
    }

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
