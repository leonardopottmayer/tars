using Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

namespace Pottmayer.Tars.Multitenancy.Resolvers;

/// <summary>
/// Resolves tenant from a claim in the current <see cref="System.Security.Claims.ClaimsPrincipal"/>.
/// Useful when admins impersonate tenants or when workers receive contextual tokens.
/// </summary>
public sealed class ClaimTenantResolver : ITenantResolver
{
    private readonly string _claimType;

    public ClaimTenantResolver(string claimType = "tenant_key")
    {
        if (string.IsNullOrWhiteSpace(claimType))
            throw new ArgumentException("Claim type must not be null or empty.", nameof(claimType));
        _claimType = claimType;
    }

    public ValueTask<TenantResolutionResult> ResolveAsync(
        TenantResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var principal = request.Principal;
        if (principal is null)
            return ValueTask.FromResult(TenantResolutionResult.Unresolved());

        var claim = principal.FindFirst(_claimType);
        if (claim is null || string.IsNullOrWhiteSpace(claim.Value))
            return ValueTask.FromResult(TenantResolutionResult.Unresolved());

        return ValueTask.FromResult(TenantResolutionResult.Resolved(claim.Value));
    }
}
