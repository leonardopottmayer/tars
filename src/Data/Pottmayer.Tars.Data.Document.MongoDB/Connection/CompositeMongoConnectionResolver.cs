using Pottmayer.Tars.Data.Document.Abstractions.Connection;

namespace Pottmayer.Tars.Data.Document.MongoDB.Connection;

/// <summary>
/// Chains all registered <see cref="IMongoConnectionResolver"/>s and returns the first non-null result.
/// </summary>
internal sealed class CompositeMongoConnectionResolver : IMongoConnectionResolver
{
    private readonly IReadOnlyList<IMongoConnectionResolver> _resolvers;

    public CompositeMongoConnectionResolver(IEnumerable<IMongoConnectionResolver> resolvers)
    {
        ArgumentNullException.ThrowIfNull(resolvers);
        _resolvers = resolvers.Where(r => r is not CompositeMongoConnectionResolver).ToList();
    }

    public async Task<IMongoConnectionDescriptor?> ResolveAsync(
        MongoConnectionResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        foreach (var resolver in _resolvers)
        {
            var descriptor = await resolver.ResolveAsync(context, cancellationToken).ConfigureAwait(false);
            if (descriptor is not null) return descriptor;
        }
        return null;
    }
}
