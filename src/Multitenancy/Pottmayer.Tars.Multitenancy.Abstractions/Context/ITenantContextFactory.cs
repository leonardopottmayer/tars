using Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

namespace Pottmayer.Tars.Multitenancy.Abstractions.Context;

/// <summary>
/// Creates an <see cref="ITenantContext"/> from a resolved <see cref="TenantResolutionResult"/>.
/// </summary>
public interface ITenantContextFactory
{
    /// <summary>Creates a tenant context from a resolution result.</summary>
    /// <param name="resolutionResult">The tenant resolution result.</param>
    /// <returns>The corresponding tenant context.</returns>
    ITenantContext Create(TenantResolutionResult resolutionResult);
}
