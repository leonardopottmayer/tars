namespace Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

/// <summary>
/// Orchestrates a sequence of <see cref="ITenantResolver"/> instances.
/// Returns the result of the first resolver that succeeds or <see cref="TenantResolutionResult.Unresolved"/>.
/// </summary>
public interface ITenantResolverPipeline
{
    /// <summary>Resolves the current tenant through the configured resolvers.</summary>
    /// <param name="request">The context available to the resolvers.</param>
    /// <param name="cancellationToken">A token that cancels the operation.</param>
    /// <returns>The first successful resolution result, or an unresolved result.</returns>
    ValueTask<TenantResolutionResult> ResolveAsync(
        TenantResolutionRequest request,
        CancellationToken cancellationToken = default);
}
