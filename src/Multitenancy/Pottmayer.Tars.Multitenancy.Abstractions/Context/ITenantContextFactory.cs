using Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

namespace Pottmayer.Tars.Multitenancy.Abstractions.Context;

/// <summary>
/// Creates an <see cref="ITenantContext"/> from a resolved <see cref="TenantResolutionResult"/>.
/// </summary>
public interface ITenantContextFactory
{
    ITenantContext Create(TenantResolutionResult resolutionResult);
}
