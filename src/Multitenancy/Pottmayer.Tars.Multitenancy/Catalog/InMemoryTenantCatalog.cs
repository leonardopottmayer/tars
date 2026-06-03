using Pottmayer.Tars.Multitenancy.Abstractions.Catalog;
using Pottmayer.Tars.Multitenancy.Abstractions.Context;
using Pottmayer.Tars.Multitenancy.Context;

namespace Pottmayer.Tars.Multitenancy.Catalog;

/// <summary>
/// Simple in-memory tenant catalog. Useful for dev, tests and single-machine deployments.
/// For production, replace with a database-backed or API-backed implementation.
/// </summary>
public sealed class InMemoryTenantCatalog : ITenantCatalog
{
    private readonly IReadOnlyList<ITenantContext> _tenants;

    public InMemoryTenantCatalog(IEnumerable<string> tenantKeys)
    {
        ArgumentNullException.ThrowIfNull(tenantKeys);
        _tenants = tenantKeys
            .Select(k => (ITenantContext)TenantContext.Create(k))
            .ToList();
    }

    public InMemoryTenantCatalog(IEnumerable<ITenantContext> tenants)
    {
        _tenants = (tenants ?? throw new ArgumentNullException(nameof(tenants))).ToList();
    }

    public async IAsyncEnumerable<ITenantContext> ListAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var tenant in _tenants)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return tenant;
            await Task.Yield();
        }
    }
}
