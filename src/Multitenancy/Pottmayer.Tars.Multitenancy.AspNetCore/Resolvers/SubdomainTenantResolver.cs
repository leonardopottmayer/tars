using Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

namespace Pottmayer.Tars.Multitenancy.AspNetCore.Resolvers;

/// <summary>
/// Resolves tenant from the first subdomain segment of the HTTP request host.
/// Reads from <see cref="TenantHttpRequestData"/> placed in <see cref="TenantResolutionRequest.Items"/> by the middleware.
/// E.g. host "acme.app.com" → tenant key "acme".
/// </summary>
public sealed class SubdomainTenantResolver : ITenantResolver
{
    public ValueTask<TenantResolutionResult> ResolveAsync(
        TenantResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.Items.TryGetValue(TenantResolutionHttpKeys.HttpRequestData, out var obj) ||
            obj is not TenantHttpRequestData data)
            return ValueTask.FromResult(TenantResolutionResult.Unresolved());

        var subdomain = ExtractSubdomain(data.Host);
        if (string.IsNullOrWhiteSpace(subdomain))
            return ValueTask.FromResult(TenantResolutionResult.Unresolved());

        return ValueTask.FromResult(TenantResolutionResult.Resolved(subdomain));
    }

    private static string? ExtractSubdomain(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return null;
        var parts = host.Split('.');
        return parts.Length >= 3 ? parts[0] : null;
    }
}
