using Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

namespace Pottmayer.Tars.Multitenancy.Resolvers;

/// <summary>
/// Default implementation of <see cref="ITenantResolverPipeline"/>.
/// Tries registered resolvers in registration order; returns first resolved result.
/// </summary>
public sealed class TenantResolverPipeline : ITenantResolverPipeline
{
    private readonly IReadOnlyList<ITenantResolver> _resolvers;

    public TenantResolverPipeline(IEnumerable<ITenantResolver> resolvers)
    {
        _resolvers = (resolvers ?? throw new ArgumentNullException(nameof(resolvers))).ToList();
    }

    public async ValueTask<TenantResolutionResult> ResolveAsync(
        TenantResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        foreach (var resolver in _resolvers)
        {
            var result = await resolver.ResolveAsync(request, cancellationToken).ConfigureAwait(false);
            if (result.IsResolved)
                return result;
        }
        return TenantResolutionResult.Unresolved();
    }
}
