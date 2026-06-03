namespace Pottmayer.Tars.Data.Abstractions.Query;

public sealed class FilterSpec
{
    public required string Field { get; init; }
    public FilterOperator Operator { get; init; }
    public object? Value { get; init; }

    /// <summary>Used when <see cref="Operator"/> is <see cref="FilterOperator.In"/>.</summary>
    public IReadOnlyList<object?>? Values { get; init; }
}
