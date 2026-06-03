using Pottmayer.Tars.Data.Relational.Abstractions.DataConnection;

namespace Pottmayer.Tars.Data.Relational.DataConnection;

/// <summary>
/// Chains multiple <see cref="IDataConnectionResolver"/> implementations in order,
/// returning the first non-null result. Allows custom resolvers to take precedence over configuration.
/// </summary>
internal sealed class CompositeDataConnectionResolver : IDataConnectionResolver
{
    private readonly IReadOnlyList<IDataConnectionResolver> _resolvers;

    public CompositeDataConnectionResolver(IEnumerable<IDataConnectionResolver> resolvers)
    {
        ArgumentNullException.ThrowIfNull(resolvers);
        _resolvers = resolvers.ToList();
    }

    public async Task<IDataConnectionDescriptor?> ResolveAsync(
        DataConnectionResolutionContext context,
        CancellationToken cancellationToken = default)
    {
        foreach (var resolver in _resolvers)
        {
            if (resolver is CompositeDataConnectionResolver) continue;
            var result = await resolver.ResolveAsync(context, cancellationToken).ConfigureAwait(false);
            if (result is not null)
                return result;
        }
        return null;
    }
}
