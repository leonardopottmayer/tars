namespace Pottmayer.Tars.Data.Abstractions.Query;

/// <summary>A single filter clause: a field, an operator and the value(s) to compare against.</summary>
public sealed class FilterSpec
{
    /// <summary>Name of the field to filter on.</summary>
    public required string Field { get; init; }

    /// <summary>Comparison operator to apply.</summary>
    public FilterOperator Operator { get; init; }

    /// <summary>Value to compare against, for single-value operators.</summary>
    public object? Value { get; init; }

    /// <summary>Used when <see cref="Operator"/> is <see cref="FilterOperator.In"/>.</summary>
    public IReadOnlyList<object?>? Values { get; init; }
}
