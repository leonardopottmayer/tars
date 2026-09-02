using Pottmayer.Tars.Multitenancy.Abstractions.Resolvers;

namespace Pottmayer.Tars.Multitenancy.DI;

/// <summary>
/// Configuration object for the tenant resolver pipeline.
/// Add resolvers in the order they should be tried.
/// </summary>
public sealed class TenantResolutionConfiguration
{
    internal List<(Type ResolverType, object? Instance)> Resolvers { get; } = [];

    /// <summary>
    /// Adds a resolver type to be resolved from the dependency injection container.
    /// </summary>
    /// <typeparam name="TResolver">The resolver type to add.</typeparam>
    /// <returns>This configuration instance.</returns>
    public TenantResolutionConfiguration AddResolver<TResolver>() where TResolver : class, ITenantResolver
    {
        Resolvers.Add((typeof(TResolver), null));
        return this;
    }

    /// <summary>
    /// Adds a resolver instance directly.
    /// </summary>
    /// <param name="resolver">The resolver instance to add.</param>
    /// <returns>This configuration instance.</returns>
    public TenantResolutionConfiguration AddResolver(ITenantResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        Resolvers.Add((resolver.GetType(), resolver));
        return this;
    }
}
