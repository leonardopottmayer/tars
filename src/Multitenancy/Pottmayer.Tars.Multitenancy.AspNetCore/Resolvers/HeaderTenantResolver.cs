using Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

namespace Pottmayer.Tars.Multitenancy.AspNetCore.Resolvers;

/// <summary>
/// Resolves tenant from an HTTP request header.
/// Reads from <see cref="TenantHttpRequestData"/> placed in <see cref="TenantResolutionRequest.Items"/> by the middleware.
/// Useful for internal APIs, gateway proxies, admin impersonation and tests.
/// </summary>
public sealed class HeaderTenantResolver : ITenantResolver
{
    private readonly string _headerName;

    public HeaderTenantResolver(string headerName = "X-Tenant-Key")
    {
        if (string.IsNullOrWhiteSpace(headerName))
            throw new ArgumentException("Header name must not be null or empty.", nameof(headerName));
        _headerName = headerName;
    }

    public ValueTask<TenantResolutionResult> ResolveAsync(
        TenantResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.Items.TryGetValue(TenantResolutionHttpKeys.HttpRequestData, out var obj) ||
            obj is not TenantHttpRequestData data)
            return ValueTask.FromResult(TenantResolutionResult.Unresolved());

        if (!data.Headers.TryGetValue(_headerName, out var value) || string.IsNullOrWhiteSpace(value))
            return ValueTask.FromResult(TenantResolutionResult.Unresolved());

        return ValueTask.FromResult(TenantResolutionResult.Resolved(value));
    }
}
