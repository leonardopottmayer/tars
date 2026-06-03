namespace Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

/// <summary>
/// Orchestrates a sequence of <see cref="ITenantResolver"/> instances.
/// Returns the result of the first resolver that succeeds or <see cref="TenantResolutionResult.Unresolved"/>.
/// </summary>
public interface ITenantResolverPipeline
{
    ValueTask<TenantResolutionResult> ResolveAsync(
        TenantResolutionRequest request,
        CancellationToken cancellationToken = default);
}
