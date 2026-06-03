using System.Linq.Expressions;
using Pottmayer.Tars.Data.Abstractions.Query;

namespace Pottmayer.Tars.Data.Relational.Extensions;

/// <summary>
/// Intermediate representation of <see cref="QueryParams"/> as typed EF expressions.
/// </summary>
public sealed class DataQueryParams<TEntity> where TEntity : class
{
    public Expression<Func<TEntity, bool>>? Predicate { get; init; }
    public int? Skip { get; init; }
    public int? Take { get; init; }
    public IReadOnlyList<SortOption>? OrderBy { get; init; }
}
