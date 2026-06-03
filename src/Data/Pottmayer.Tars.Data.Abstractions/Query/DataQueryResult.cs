namespace Pottmayer.Tars.Data.Abstractions.Query;

public sealed class DataQueryResult<TEntity>
{
    public required IReadOnlyList<TEntity> Items { get; init; }
    public long TotalCount { get; init; }
}
