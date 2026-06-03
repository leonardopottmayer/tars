using Microsoft.Extensions.Caching.Memory;
using Pottmayer.Tars.Caching.Abstractions;

namespace Pottmayer.Tars.Caching.Memory
{
    public sealed class MemoryCacheStore : ICacheStore
    {
        private readonly IMemoryCache _cache;
        private readonly ICacheKeyBuilder _keys;

        public MemoryCacheStore(IMemoryCache cache, ICacheKeyBuilder keys)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        }

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

        public ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var k = _keys.Build(key);

            return new ValueTask<T?>(_cache.TryGetValue(k, out var obj) ? (T?)obj : default);
        }

        public ValueTask<CacheGetResult<T>> TryGetAsync<T>(string key, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var k = _keys.Build(key);

            if (_cache.TryGetValue(k, out var obj) && obj is T typed)
                return new ValueTask<CacheGetResult<T>>(new CacheGetResult<T>(true, typed));

            return new ValueTask<CacheGetResult<T>>(new CacheGetResult<T>(false, default));
        }

        public ValueTask RemoveAsync(string key, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var k = _keys.Build(key);
            _cache.Remove(k);

            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var k = _keys.Build(key);
            return new ValueTask<bool>(_cache.TryGetValue(k, out _));
        }

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
