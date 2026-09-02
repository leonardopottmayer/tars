namespace Pottmayer.Tars.Data.Abstractions.Query;

/// <summary>A single sort clause: the property to order by and its direction.</summary>
public sealed class SortOption
{
    /// <summary>Name of the property to sort by.</summary>
    public required string PropertyName { get; init; }

    /// <summary>Whether to sort in descending order.</summary>
    public bool Descending { get; init; }
}
