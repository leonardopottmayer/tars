namespace Pottmayer.Tars.Multitenancy.Abstractions.Store;

/// <summary>
/// Provides point lookups for individual tenants by identity or name.
/// Complements <see cref="Catalog.ITenantCatalog"/>, which enumerates all known tenants.
/// </summary>
public interface ITenantStore
{
    /// <summary>
    /// Returns the tenant context for the given <paramref name="tenantId"/>, or <c>null</c> if not found.
    /// </summary>
    Task<Context.ITenantContext?> FindByIdAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the tenant context for the given <paramref name="name"/>, or <c>null</c> if not found.
    /// </summary>
    Task<Context.ITenantContext?> FindByNameAsync(string name, CancellationToken cancellationToken = default);
}
