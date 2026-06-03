using Pottmayer.Tars.Multitenancy.Abstractions.Context;
using Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

namespace Pottmayer.Tars.Multitenancy.Context;

/// <summary>
/// Default implementation of <see cref="ITenantContext"/>.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    public bool IsResolved { get; init; }
    public string? TenantKey { get; init; }
    public string? TenantCode { get; init; }
    public IReadOnlyDictionary<string, object?> Properties { get; init; } =
        new Dictionary<string, object?>();

    public static TenantContext Unresolved() => new() { IsResolved = false };

    public static TenantContext FromResolution(TenantResolutionResult result) =>
        new()
        {
            IsResolved = result.IsResolved,
            TenantKey = result.TenantKey,
            TenantCode = result.TenantCode
        };

    public static TenantContext Create(string tenantKey, string? tenantCode = null,
        IReadOnlyDictionary<string, object?>? properties = null) =>
        new()
        {
            IsResolved = true,
            TenantKey = tenantKey,
            TenantCode = tenantCode ?? tenantKey,
            Properties = properties ?? new Dictionary<string, object?>()
        };
}
