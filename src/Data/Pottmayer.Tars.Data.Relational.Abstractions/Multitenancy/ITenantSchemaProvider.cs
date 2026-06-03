namespace Pottmayer.Tars.Data.Relational.Abstractions.Multitenancy;

/// <summary>
/// Resolves the database schema name for a given tenant.
/// Use when all tenants share the same database but are isolated by schema (schema-per-tenant isolation).
/// </summary>
public interface ITenantSchemaProvider
{
    /// <summary>
    /// Returns the schema name for the given <paramref name="tenantId"/>.
    /// </summary>
    string GetSchema(string tenantId);
}
