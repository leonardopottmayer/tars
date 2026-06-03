using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Caching.Abstractions;
using Pottmayer.Tars.Caching.Core.Options;
using StackExchange.Redis;

namespace Pottmayer.Tars.Caching.Redis
{
    public sealed class RedisCacheStore : ICacheStore
    {
        private static readonly RedisValue[] HashFields =
        [
            RedisHashFields.Value,
            RedisHashFields.AbsoluteDeadlineUtcTicks,
            RedisHashFields.SlidingExpirationMs
        ];

        private readonly IDatabase _db;
        private readonly ICacheKeyBuilder _keys;
        private readonly ICacheSerializer _serializer;
        private readonly IOptionsMonitor<CacheOptions> _cacheOptionsMonitor;
        private readonly ILogger<RedisCacheStore> _logger;

        public RedisCacheStore(
            IDatabase db,
            ICacheKeyBuilder keys,
            ICacheSerializer serializer,
            IOptionsMonitor<CacheOptions> cacheOptionsMonitor,
            ILogger<RedisCacheStore> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _keys = keys ?? throw new ArgumentNullException(nameof(keys));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _cacheOptionsMonitor = cacheOptionsMonitor ?? throw new ArgumentNullException(nameof(cacheOptionsMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async ValueTask SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            var k = _keys.Build(key);

            var now = DateTimeOffset.UtcNow;
            var (absoluteTtl, absoluteDeadlineTicks) = ResolveAbsoluteExpiration(now, options);
            var slidingMs = ResolveSlidingExpirationMs(options);

            var payload = _serializer.Serialize(value);

            // Store as Redis hash to support sliding expiration metadata.
            // Fields:
            // - v  : serialized payload bytes
            // - ad : absolute deadline UTC ticks (optional)
            // - s  : sliding expiration in milliseconds (optional)
            var entries = new List<HashEntry>(capacity: 3)
            {
                new(RedisHashFields.Value, payload)
            };

            if (absoluteDeadlineTicks is not null)
                entries.Add(new HashEntry(RedisHashFields.AbsoluteDeadlineUtcTicks, absoluteDeadlineTicks.Value));

            if (slidingMs is not null)
                entries.Add(new HashEntry(RedisHashFields.SlidingExpirationMs, slidingMs.Value));

            await _db.HashSetAsync(k, entries.ToArray()).ConfigureAwait(false);

            var initialTtl = ResolveInitialTtl(absoluteTtl, slidingMs);
            if (initialTtl is not null)
            {
                await _db.KeyExpireAsync(k, initialTtl).ConfigureAwait(false);
            }
        }

        public async ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            var result = await TryGetInternalAsync<T>(key, ct).ConfigureAwait(false);
            return result.Found ? result.Value : default;
        }

        public ValueTask<CacheGetResult<T>> TryGetAsync<T>(string key, CancellationToken ct = default)
            => TryGetInternalAsync<T>(key, ct);

        public async ValueTask RemoveAsync(string key, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var k = _keys.Build(key);
            await _db.KeyDeleteAsync(k).ConfigureAwait(false);
        }

        public async ValueTask<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var k = _keys.Build(key);
            return await _db.KeyExistsAsync(k).ConfigureAwait(false);
        }

        public async ValueTask<T> GetOrSetAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            CacheEntryOptions? options = null,
            CancellationToken ct = default)
        {
            var result = await TryGetInternalAsync<T>(key, ct).ConfigureAwait(false);
            if (result.Found)
                return result.Value!;

            var value = await factory(ct).ConfigureAwait(false);
            await SetAsync(key, value, options, ct).ConfigureAwait(false);
            return value;
        }

        private async ValueTask<CacheGetResult<T>> TryGetInternalAsync<T>(string key, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var k = _keys.Build(key);

            RedisValue[] values;
            try
            {
                values = await _db.HashGetAsync(k, HashFields).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis cache read failed for key '{CacheKey}'.", k);
                return new CacheGetResult<T>(false, default);
            }

            if (values.Length != 3 || values[0].IsNull)
                return new CacheGetResult<T>(false, default);

            var payload = (byte[])values[0]!;

            var now = DateTimeOffset.UtcNow;
            var absoluteDeadlineTicks = TryReadInt64(values[1]);
            var slidingMs = TryReadInt32(values[2]);

            if (absoluteDeadlineTicks is not null)
            {
                var deadline = new DateTimeOffset(absoluteDeadlineTicks.Value, TimeSpan.Zero);
                if (deadline <= now)
                {
                    FireAndForget(_db.KeyDeleteAsync(k), k, "delete expired key");
                    return new CacheGetResult<T>(false, default);
                }
            }

            if (slidingMs is not null && slidingMs.Value > 0)
            {
                var newTtl = ComputeSlidingTtl(now, absoluteDeadlineTicks, TimeSpan.FromMilliseconds(slidingMs.Value));
                if (newTtl is null || newTtl <= TimeSpan.Zero)
                {
                    FireAndForget(_db.KeyDeleteAsync(k), k, "delete sliding-expired key");
                    return new CacheGetResult<T>(false, default);
                }

                // Renew TTL on access (best-effort).
                FireAndForget(_db.KeyExpireAsync(k, newTtl), k, "renew sliding TTL");
            }

            T? typed;
            try
            {
                typed = _serializer.Deserialize<T>(payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis cache deserialize failed for key '{CacheKey}'.", k);
                return new CacheGetResult<T>(false, default);
            }

            return new CacheGetResult<T>(true, typed);
        }

        private (TimeSpan? ttl, long? deadlineUtcTicks) ResolveAbsoluteExpiration(DateTimeOffset now, CacheEntryOptions? options)
        {
            var ttl = options?.AbsoluteExpirationRelativeToNow ?? _cacheOptionsMonitor.CurrentValue.DefaultAbsoluteExpirationRelativeToNow;
            if (ttl is null)
                return (null, null);

            if (ttl <= TimeSpan.Zero)
                return (null, null);

            var deadline = now.Add(ttl.Value);
            return (ttl, deadline.UtcTicks);
        }

        private static int? ResolveSlidingExpirationMs(CacheEntryOptions? options)
        {
            if (options?.SlidingExpiration is null)
                return null;

            if (options.SlidingExpiration <= TimeSpan.Zero)
                return null;

            var ms = (int)Math.Min(int.MaxValue, options.SlidingExpiration.Value.TotalMilliseconds);
            return ms;
        }

        private static TimeSpan? ResolveInitialTtl(TimeSpan? absoluteTtl, int? slidingMs)
        {
            if (absoluteTtl is null && slidingMs is null)
                return null;

            var sliding = slidingMs is null ? (TimeSpan?)null : TimeSpan.FromMilliseconds(slidingMs.Value);

            if (absoluteTtl is null)
                return sliding;

            if (sliding is null)
                return absoluteTtl;

            return absoluteTtl.Value <= sliding.Value ? absoluteTtl : sliding;
        }

        private static TimeSpan? ComputeSlidingTtl(DateTimeOffset now, long? absoluteDeadlineUtcTicks, TimeSpan sliding)
        {
            if (absoluteDeadlineUtcTicks is null)
                return sliding;

            var deadline = new DateTimeOffset(absoluteDeadlineUtcTicks.Value, TimeSpan.Zero);
            var remaining = deadline - now;
            if (remaining <= TimeSpan.Zero)
                return TimeSpan.Zero;

            return remaining <= sliding ? remaining : sliding;
        }

        private static long? TryReadInt64(RedisValue value)
        {
            if (value.IsNull) return null;
            return (long)value;
        }

        private static int? TryReadInt32(RedisValue value)
        {
            if (value.IsNull) return null;
            return (int)value;
        }

        private void FireAndForget(Task task, RedisKey key, string operation)
        {
            task.ContinueWith(
                t => _logger.LogWarning(t.Exception, "Redis best-effort operation '{Operation}' failed for key '{Key}'.", operation, (string)key),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        private static class RedisHashFields
        {
            public static readonly RedisValue Value = "v";
            public static readonly RedisValue AbsoluteDeadlineUtcTicks = "ad";
            public static readonly RedisValue SlidingExpirationMs = "s";
        }
    }
}
