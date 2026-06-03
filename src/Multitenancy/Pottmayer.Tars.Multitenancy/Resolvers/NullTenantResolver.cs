using Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

namespace Pottmayer.Tars.Multitenancy.Resolvers;

/// <summary>
/// Resolver that always returns unresolved. Useful as explicit no-op or sentinel in a composite.
/// </summary>
public sealed class NullTenantResolver : ITenantResolver
{
    public ValueTask<TenantResolutionResult> ResolveAsync(
        TenantResolutionRequest request,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(TenantResolutionResult.Unresolved());
}
