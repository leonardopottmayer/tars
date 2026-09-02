using System.Linq.Expressions;
using Pottmayer.Tars.Data.Abstractions.Query;

namespace Pottmayer.Tars.Data.Relational.Extensions;

/// <summary>
/// Intermediate representation of <see cref="QueryParams"/> as typed EF expressions.
/// </summary>
public sealed class DataQueryParams<TEntity> where TEntity : class
{
    /// <summary>Filter predicate built from the query's filters, or null when there are none.</summary>
    public Expression<Func<TEntity, bool>>? Predicate { get; init; }

    /// <summary>Number of items to skip for paging, or null when not paged.</summary>
    public int? Skip { get; init; }

    /// <summary>Maximum number of items to take for paging, or null when not paged.</summary>
    public int? Take { get; init; }

    /// <summary>Sort options to apply, or null when there are none.</summary>
    public IReadOnlyList<SortOption>? OrderBy { get; init; }
}
