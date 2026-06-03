namespace Pottmayer.Tars.Caching.Abstractions
{
    public interface ICacheStore
    {
        ValueTask SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken ct = default);

        ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default);

        ValueTask<CacheGetResult<T>> TryGetAsync<T>(string key, CancellationToken ct = default);

        ValueTask RemoveAsync(string key, CancellationToken ct = default);

        ValueTask<bool> ExistsAsync(string key, CancellationToken ct = default);

        ValueTask<T> GetOrSetAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            CacheEntryOptions? options = null,
            CancellationToken ct = default);
    }
}
