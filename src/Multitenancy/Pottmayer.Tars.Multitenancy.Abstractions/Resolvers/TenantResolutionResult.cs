namespace Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

/// <summary>
/// Result produced by an <see cref="ITenantResolver"/>. Carries identity of the resolved tenant.
/// </summary>
public sealed class TenantResolutionResult
{
    public bool IsResolved { get; init; }
    public string? TenantKey { get; init; }
    public string? TenantCode { get; init; }
    public object? Metadata { get; init; }

    public static TenantResolutionResult Unresolved() => new() { IsResolved = false };

    public static TenantResolutionResult Resolved(string tenantKey, string? tenantCode = null, object? metadata = null)
        => new()
        {
            IsResolved = true,
            TenantKey = tenantKey,
            TenantCode = tenantCode ?? tenantKey,
            Metadata = metadata
        };
}
