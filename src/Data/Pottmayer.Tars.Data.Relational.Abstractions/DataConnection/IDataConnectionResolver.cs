namespace Pottmayer.Tars.Data.Relational.Abstractions.DataConnection;

/// <summary>Resolves a connection descriptor for a requested database, optionally scoped to a tenant.</summary>
public interface IDataConnectionResolver
{
    /// <summary>Attempts to resolve a connection descriptor for the given context.</summary>
    /// <param name="context">The resolution context (database key, tenant, service provider).</param>
    /// <param name="cancellationToken">Token used to cancel resolution.</param>
    /// <returns>The resolved descriptor, or null when this resolver cannot handle the request.</returns>
    Task<IDataConnectionDescriptor?> ResolveAsync(
        DataConnectionResolutionContext context,
        CancellationToken cancellationToken = default);
}
