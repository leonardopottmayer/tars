namespace Pottmayer.Tars.Core.Primitives;

/// <summary>
/// Represents an optional value for PATCH semantics: distinguishes "property not sent" from "property sent (possibly null)".
/// When a property is absent from the JSON body, it deserializes as <see cref="Absent"/>; when present, as <see cref="Some"/>.
/// </summary>
public readonly struct Optional<T>
{
    private readonly bool _isPresent;
    private readonly T? _value;

    private Optional(bool isPresent, T? value)
    {
        _isPresent = isPresent;
        _value = value;
    }

    /// <summary>True if the property was present in the request (even if its value was null).</summary>
    public bool IsPresent => _isPresent;

    /// <summary>Gets the value when <see cref="IsPresent"/> is true; otherwise default.</summary>
    public T? Value => _isPresent ? _value : default;

    /// <summary>No value (property was not sent).</summary>
    public static Optional<T> Absent() => new(false, default);

    /// <summary>Value present (property was sent; value may be null).</summary>
    public static Optional<T> Some(T? value) => new(true, value);

    public void Deconstruct(out bool isPresent, out T? value)
    {
        isPresent = _isPresent;
        value = _value;
    }
}
