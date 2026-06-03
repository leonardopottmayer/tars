using Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

namespace Pottmayer.Tars.Multitenancy.Resolvers;

/// <summary>
/// Resolver that always returns the same static tenant key.
/// Useful for single-tenant deployments, CLI tools, local dev, and tests.
/// </summary>
public sealed class StaticTenantResolver : ITenantResolver
{
    private readonly string _tenantKey;

    public StaticTenantResolver(string tenantKey)
    {
        if (string.IsNullOrWhiteSpace(tenantKey))
            throw new ArgumentException("Tenant key must not be null or empty.", nameof(tenantKey));
        _tenantKey = tenantKey;
    }

    public ValueTask<TenantResolutionResult> ResolveAsync(
        TenantResolutionRequest request,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(TenantResolutionResult.Resolved(_tenantKey));
}
