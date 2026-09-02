namespace Pottmayer.Tars.Caching.Abstractions
{
    /// <summary>
    /// Provider-agnostic cache contract. Implementations (in-memory, Redis, ...) store and retrieve
    /// values by key, applying the expiration policy described by <see cref="CacheEntryOptions"/>.
    /// Keys passed in are logical; the effective storage key is produced by <see cref="ICacheKeyBuilder"/>.
    /// </summary>
    public interface ICacheStore
    {
        /// <summary>
        /// Stores <paramref name="value"/> under <paramref name="key"/>, overwriting any existing entry.
        /// </summary>
        /// <typeparam name="T">Type of the value being cached.</typeparam>
        /// <param name="key">Logical cache key.</param>
        /// <param name="value">Value to store.</param>
        /// <param name="options">Expiration policy. When <c>null</c>, the provider default (if any) applies.</param>
        /// <param name="ct">Cancellation token.</param>
        ValueTask SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken ct = default);

        /// <summary>
        /// Gets the value stored under <paramref name="key"/>, or <c>default</c> when absent.
        /// </summary>
        /// <typeparam name="T">Expected type of the cached value.</typeparam>
        /// <param name="key">Logical cache key.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The cached value, or <c>default</c> if the key is not present.</returns>
        ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default);

        /// <summary>
        /// Attempts to get the value under <paramref name="key"/>, distinguishing "missing" from a
        /// stored <c>default</c>/<c>null</c> value.
        /// </summary>
        /// <typeparam name="T">Expected type of the cached value.</typeparam>
        /// <param name="key">Logical cache key.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A <see cref="CacheGetResult{T}"/> whose <c>Found</c> flag reports whether the key existed.</returns>
        ValueTask<CacheGetResult<T>> TryGetAsync<T>(string key, CancellationToken ct = default);

        /// <summary>
        /// Removes the entry under <paramref name="key"/>. No-op when the key is absent.
        /// </summary>
        /// <param name="key">Logical cache key.</param>
        /// <param name="ct">Cancellation token.</param>
        ValueTask RemoveAsync(string key, CancellationToken ct = default);

        /// <summary>
        /// Checks whether an entry exists under <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Logical cache key.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns><c>true</c> when the key is present; otherwise <c>false</c>.</returns>
        ValueTask<bool> ExistsAsync(string key, CancellationToken ct = default);

        /// <summary>
        /// Returns the cached value under <paramref name="key"/>, or invokes <paramref name="factory"/>
        /// to produce it, stores the result under <paramref name="options"/> and returns it (read-through).
        /// </summary>
        /// <typeparam name="T">Type of the value being cached.</typeparam>
        /// <param name="key">Logical cache key.</param>
        /// <param name="factory">Factory invoked only on a cache miss to build the value.</param>
        /// <param name="options">Expiration policy applied when the value is stored after a miss.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The existing cached value, or the freshly produced and stored one.</returns>
        ValueTask<T> GetOrSetAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            CacheEntryOptions? options = null,
            CancellationToken ct = default);
    }
}
