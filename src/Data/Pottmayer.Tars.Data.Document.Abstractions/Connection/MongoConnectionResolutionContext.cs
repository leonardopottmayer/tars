namespace Pottmayer.Tars.Data.Document.Abstractions.Connection;

/// <summary>
/// Context passed to each <see cref="IMongoConnectionResolver"/> when resolving a connection.
/// Mirrors the relational resolution context so tenant resolution and data resolution stay separate concerns.
/// </summary>
public sealed class MongoConnectionResolutionContext
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
