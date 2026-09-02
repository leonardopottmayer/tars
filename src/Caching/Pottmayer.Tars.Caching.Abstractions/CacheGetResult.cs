namespace Pottmayer.Tars.Caching.Abstractions
{
    /// <summary>
    /// Result of a <see cref="ICacheStore.TryGetAsync{T}"/> call, separating a cache hit from a miss so a
    /// stored <c>default</c>/<c>null</c> value is not mistaken for an absent key.
    /// </summary>
    /// <typeparam name="T">Type of the cached value.</typeparam>
    /// <param name="Found"><c>true</c> when the key existed in the cache; otherwise <c>false</c>.</param>
    /// <param name="Value">The cached value when <paramref name="Found"/> is <c>true</c>; otherwise <c>default</c>.</param>
    public readonly record struct CacheGetResult<T>(bool Found, T? Value);
}
