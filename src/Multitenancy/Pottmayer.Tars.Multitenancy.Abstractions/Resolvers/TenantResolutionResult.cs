namespace Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

/// <summary>
/// Result produced by an <see cref="ITenantResolver"/>. Carries identity of the resolved tenant.
/// </summary>
public sealed class TenantResolutionResult
{
    /// <summary>Gets whether a resolver identified a tenant.</summary>
    public bool IsResolved { get; init; }
    /// <summary>Gets the resolved tenant key.</summary>
    public string? TenantKey { get; init; }
    /// <summary>Gets the optional tenant code.</summary>
    public string? TenantCode { get; init; }
    /// <summary>Gets resolver-specific metadata.</summary>
    public object? Metadata { get; init; }

    /// <summary>Creates an unresolved result.</summary>
    /// <returns>An unresolved tenant resolution result.</returns>
    public static TenantResolutionResult Unresolved() => new() { IsResolved = false };

    /// <summary>Creates a resolved result.</summary>
    /// <param name="tenantKey">The resolved tenant key.</param>
    /// <param name="tenantCode">The optional tenant code.</param>
    /// <param name="metadata">Resolver-specific metadata.</param>
    /// <returns>A resolved tenant resolution result.</returns>
    public static TenantResolutionResult Resolved(string tenantKey, string? tenantCode = null, object? metadata = null)
        => new()
        {
            IsResolved = true,
            TenantKey = tenantKey,
            TenantCode = tenantCode ?? tenantKey,
            Metadata = metadata
        };
}
