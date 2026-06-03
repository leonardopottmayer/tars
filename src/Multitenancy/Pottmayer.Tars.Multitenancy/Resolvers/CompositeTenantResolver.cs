using Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

namespace Pottmayer.Tars.Multitenancy.Resolvers;

/// <summary>
/// Tries a fixed list of resolvers in order and returns the first resolved result.
/// Use when you want to compose resolvers manually rather than using the pipeline.
/// </summary>
public sealed class CompositeTenantResolver : ITenantResolver
{
    private readonly IReadOnlyList<ITenantResolver> _resolvers;

    public CompositeTenantResolver(IEnumerable<ITenantResolver> resolvers)
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
