namespace Pottmayer.Tars.Data.Document.Abstractions.Connection;

/// <summary>Resolves a MongoDB connection descriptor for a requested database, optionally scoped to a tenant.</summary>
public interface IMongoConnectionResolver
{
    /// <summary>Attempts to resolve a connection descriptor for the given context.</summary>
    /// <param name="context">The resolution context (database key, tenant, service provider).</param>
    /// <param name="cancellationToken">Token used to cancel resolution.</param>
    /// <returns>The resolved descriptor, or null when this resolver cannot handle the request.</returns>
    Task<IMongoConnectionDescriptor?> ResolveAsync(
        MongoConnectionResolutionContext context,
        CancellationToken cancellationToken = default);
}
