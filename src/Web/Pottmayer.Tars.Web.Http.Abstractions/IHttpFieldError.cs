namespace Pottmayer.Tars.Web.Http.Abstractions;

/// <summary>
/// Represents a validation error associated with a response field.
/// </summary>
public interface IHttpFieldError
{
    /// <summary>Gets the field name.</summary>
    string Field { get; }
    /// <summary>Gets the validation message.</summary>
    string Message { get; }
}
