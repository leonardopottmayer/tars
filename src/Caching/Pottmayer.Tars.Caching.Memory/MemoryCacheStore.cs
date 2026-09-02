using Microsoft.Extensions.Caching.Memory;
using Pottmayer.Tars.Caching.Abstractions;

namespace Pottmayer.Tars.Caching.Memory
{
    /// <summary>
    /// In-memory <see cref="ICacheStore"/> backed by <see cref="IMemoryCache"/>. Suited to single-process
    /// caching; values are stored as live object references (no serialization) under keys produced by
    /// <see cref="ICacheKeyBuilder"/>.
    /// </summary>
    public sealed class MemoryCacheStore : ICacheStore
    {
        private readonly IMemoryCache _cache;
        private readonly ICacheKeyBuilder _keys;

        /// <summary>
        /// Creates the store over the given memory cache and key builder.
        /// </summary>
        /// <param name="cache">The underlying <see cref="IMemoryCache"/>.</param>
        /// <param name="keys">Builder that namespaces logical keys into storage keys.</param>
        public MemoryCacheStore(IMemoryCache cache, ICacheKeyBuilder keys)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        }

        /// <inheritdoc/>
        public ValueTask SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var k = _keys.Build(key);

            var entryOptions = new MemoryCacheEntryOptions();

            if (options?.AbsoluteExpirationRelativeToNow is not null)
                entryOptions.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;

            if (options?.SlidingExpiration is not null)
                entryOptions.SlidingExpiration = options.SlidingExpiration;

            _cache.Set(k, value, entryOptions);
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var k = _keys.Build(key);

            return new ValueTask<T?>(_cache.TryGetValue(k, out var obj) ? (T?)obj : default);
        }

        /// <inheritdoc/>
        public ValueTask<CacheGetResult<T>> TryGetAsync<T>(string key, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var k = _keys.Build(key);

            if (_cache.TryGetValue(k, out var obj) && obj is T typed)
                return new ValueTask<CacheGetResult<T>>(new CacheGetResult<T>(true, typed));

            return new ValueTask<CacheGetResult<T>>(new CacheGetResult<T>(false, default));
        }

        /// <inheritdoc/>
        public ValueTask RemoveAsync(string key, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var k = _keys.Build(key);
            _cache.Remove(k);

            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public ValueTask<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var k = _keys.Build(key);
            return new ValueTask<bool>(_cache.TryGetValue(k, out _));
        }

        /// <inheritdoc/>
        public async ValueTask<T> GetOrSetAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            CacheEntryOptions? options = null,
            CancellationToken ct = default)
        {
            var result = await TryGetAsync<T>(key, ct).ConfigureAwait(false);
            if (result.Found)
                return result.Value!;

            var value = await factory(ct).ConfigureAwait(false);
            await SetAsync(key, value, options, ct).ConfigureAwait(false);
            return value;
        }
    }
}
