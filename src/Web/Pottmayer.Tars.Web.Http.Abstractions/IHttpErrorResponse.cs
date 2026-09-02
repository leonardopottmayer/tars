namespace Pottmayer.Tars.Web.Http.Abstractions;

/// <summary>
/// Represents a failed HTTP response envelope.
/// </summary>
public interface IHttpErrorResponse : IHttpResponse
{
    /// <summary>Gets the machine-readable error code.</summary>
    string? ErrorCode { get; }
    /// <summary>Gets the human-readable error message.</summary>
    string? ErrorMessage { get; }
    /// <summary>Gets validation errors by field, when available.</summary>
    IReadOnlyList<IHttpFieldError>? FieldErrors { get; }
}
