using Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

namespace Pottmayer.Tars.Multitenancy.DI;

/// <summary>
/// Configuration object for the tenant resolver pipeline.
/// Add resolvers in the order they should be tried.
/// </summary>
public sealed class TenantResolutionOptions
{
    internal List<(Type ResolverType, object? Instance)> Resolvers { get; } = [];

    /// <summary>Adds a resolver by type (resolved from DI).</summary>
    public TenantResolutionOptions AddResolver<TResolver>() where TResolver : class, ITenantResolver
    {
        Resolvers.Add((typeof(TResolver), null));
        return this;
    }

    /// <summary>Adds a resolver instance directly.</summary>
    public TenantResolutionOptions AddResolver(ITenantResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        Resolvers.Add((resolver.GetType(), resolver));
        return this;
    }
}
