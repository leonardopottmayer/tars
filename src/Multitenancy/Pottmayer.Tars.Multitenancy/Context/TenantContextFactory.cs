using Pottmayer.Tars.Multitenancy.Abstractions.Context;
using Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

namespace Pottmayer.Tars.Multitenancy.Context;

/// <summary>
/// Default implementation of <see cref="ITenantContextFactory"/>.
/// Creates a <see cref="TenantContext"/> from a <see cref="TenantResolutionResult"/>.
/// </summary>
public sealed class TenantContextFactory : ITenantContextFactory
{
    public ITenantContext Create(TenantResolutionResult resolutionResult)
    {
        ArgumentNullException.ThrowIfNull(resolutionResult);
        return TenantContext.FromResolution(resolutionResult);
    }
}
