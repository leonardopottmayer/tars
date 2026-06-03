namespace Pottmayer.Tars.Data.Abstractions.Query;

public sealed class SortOption
{
    public required string PropertyName { get; init; }
    public bool Descending { get; init; }
}
