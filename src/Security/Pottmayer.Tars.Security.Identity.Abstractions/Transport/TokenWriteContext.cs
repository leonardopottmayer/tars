namespace Pottmayer.Tars.Security.Identity.Abstractions.Transport;

/// <summary>
/// Transport-agnostic instructions for writing tokens to a response.
/// The HTTP adapter applies this to HttpContext.Response; other adapters apply it to their own sinks.
/// </summary>
public sealed class TokenWriteContext
{
    public IDictionary<string, string> ResponseHeaders { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IList<TokenCookieWriteModel> CookiesToAppend { get; init; } =
        new List<TokenCookieWriteModel>();

    public IList<string> CookiesToDelete { get; init; } =
        new List<string>();

    /// <summary>
    /// Optional body to serialize as the response payload.
    /// When non-null the adapter should use this as the HTTP response body instead of the default envelope.
    /// </summary>
    public object? Body { get; set; }
}
