namespace Pottmayer.Tars.Data.Relational.Abstractions.DataConnection;

/// <summary>
/// Context passed to each <see cref="IDataConnectionResolver"/> when resolving a connection.
/// </summary>
public sealed class DataConnectionResolutionContext
{
    /// <summary>Key identifying the logical database to resolve a connection for.</summary>
    public required string DatabaseKey { get; init; }

    /// <summary>The tenant key in scope, when resolving a tenant-scoped connection.</summary>
    public string? TenantKey { get; init; }

    /// <summary>The tenant code in scope, when available.</summary>
    public string? TenantCode { get; init; }

    /// <summary>Service provider for resolvers that need additional services.</summary>
    public required IServiceProvider ServiceProvider { get; init; }
}
