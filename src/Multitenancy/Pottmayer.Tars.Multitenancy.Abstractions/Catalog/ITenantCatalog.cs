using Pottmayer.Tars.Multitenancy.Abstractions.Context;

namespace Pottmayer.Tars.Multitenancy.Abstractions.Catalog;

/// <summary>
/// Enumerates all known tenants. Used by jobs and hosted services to iterate over all tenants.
/// Implementations can read from a central database, JSON file, configuration, external API, etc.
/// </summary>
public interface ITenantCatalog
{
    IAsyncEnumerable<ITenantContext> ListAsync(CancellationToken cancellationToken = default);
}
