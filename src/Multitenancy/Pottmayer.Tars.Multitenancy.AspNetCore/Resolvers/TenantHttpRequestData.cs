namespace Pottmayer.Tars.Multitenancy.AspNetCore.Resolvers;

/// <summary>
/// Snapshot of HTTP request data placed in <see cref="Pottmayer.Tars.Multitenancy.Abstractions.Resolvers.TenantResolutionRequest.Items"/>
/// by <see cref="Pottmayer.Tars.Multitenancy.AspNetCore.Middleware.TarsTenantResolutionMiddleware"/>.
/// HTTP-specific resolvers read from this object instead of depending on HttpContext directly.
/// </summary>
public sealed class TenantHttpRequestData
{
    public string? Host { get; }
    public IReadOnlyDictionary<string, string?> Headers { get; }

    public TenantHttpRequestData(string? host, IReadOnlyDictionary<string, string?> headers)
    {
        Host = host;
        Headers = headers ?? throw new ArgumentNullException(nameof(headers));
    }
}
