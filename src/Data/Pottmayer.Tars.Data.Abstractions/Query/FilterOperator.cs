namespace Pottmayer.Tars.Data.Abstractions.Query;

/// <summary>Comparison operator applied by a <see cref="FilterSpec"/>.</summary>
public enum FilterOperator
{
    /// <summary>Equal to.</summary>
    Eq,

    /// <summary>Not equal to.</summary>
    NotEq,

    /// <summary>String contains the value.</summary>
    Contains,

    /// <summary>String starts with the value.</summary>
    StartsWith,

    /// <summary>String ends with the value.</summary>
    EndsWith,

    /// <summary>Greater than.</summary>
    Gt,

    /// <summary>Greater than or equal to.</summary>
    Gte,

    /// <summary>Less than.</summary>
    Lt,

    /// <summary>Less than or equal to.</summary>
    Lte,

    /// <summary>Value is in a set (see <see cref="FilterSpec.Values"/>).</summary>
    In
}
