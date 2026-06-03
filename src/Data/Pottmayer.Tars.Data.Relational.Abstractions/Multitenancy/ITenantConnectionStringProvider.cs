namespace Pottmayer.Tars.Data.Relational.Abstractions.Multitenancy;

/// <summary>
/// Resolves the database connection string for a given tenant.
/// Use when each tenant has its own database or connection string (database-per-tenant isolation).
/// </summary>
public interface ITenantConnectionStringProvider
{
    /// <summary>
    /// Returns the connection string for the given <paramref name="tenantId"/>.
    /// </summary>
    Task<string> GetConnectionStringAsync(string tenantId, CancellationToken cancellationToken = default);
}
