namespace Pottmayer.Tars.Caching.Abstractions
{
    /// <summary>
    /// Turns a logical cache key into the effective storage key, typically by applying a prefix and
    /// separator so keys are namespaced and safe to share a backing store across services.
    /// </summary>
    public interface ICacheKeyBuilder
    {
        /// <summary>
        /// Builds the effective storage key for the given logical <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Logical (caller-supplied) key. Must not be null or whitespace.</param>
        /// <returns>The namespaced key to use against the cache store.</returns>
        string Build(string key);
    }
}
