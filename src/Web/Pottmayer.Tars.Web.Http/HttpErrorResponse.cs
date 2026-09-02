using Pottmayer.Tars.Web.Http.Abstractions;

namespace Pottmayer.Tars.Web.Http;

/// <summary>
/// Default failed HTTP response envelope.
/// </summary>
public sealed class HttpErrorResponse : IHttpErrorResponse
{
    /// <inheritdoc/>
    public bool Success { get; init; }
    /// <inheritdoc/>
    public string? ErrorCode { get; init; }
    /// <inheritdoc/>
    public string? ErrorMessage { get; init; }
    /// <inheritdoc/>
    public IReadOnlyList<IHttpFieldError>? FieldErrors { get; init; }
    /// <summary>Gets the trace identifier for the response, when included.</summary>
    public string? TraceId { get; init; }
}
