namespace Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

/// <summary>
/// Resolves the current tenant from a <see cref="TenantResolutionRequest"/>.
/// Implementations can inspect HTTP context, claims, headers, subdomain, configuration, etc.
/// The framework tries resolvers in pipeline order until one returns <see cref="TenantResolutionResult.IsResolved"/> = true.
/// </summary>
public interface ITenantResolver
{
    /// <summary>Attempts to resolve the current tenant.</summary>
    /// <param name="request">The context available to the resolver.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The resolution result.</returns>
    ValueTask<TenantResolutionResult> ResolveAsync(
        TenantResolutionRequest request,
        CancellationToken cancellationToken = default);
}
