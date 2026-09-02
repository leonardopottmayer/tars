using Pottmayer.Tars.Web.Http.Abstractions;

namespace Pottmayer.Tars.Web.Http;

/// <summary>
/// Default successful HTTP response envelope.
/// </summary>
/// <typeparam name="T">The type of response data.</typeparam>
public sealed class HttpResponse<T> : IHttpResponse<T>
{
    /// <inheritdoc/>
    public bool Success { get; init; }
    /// <inheritdoc/>
    public T? Data { get; init; }
    /// <summary>Gets the trace identifier for the response, when included.</summary>
    public string? TraceId { get; init; }
}
