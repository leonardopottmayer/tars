using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Caching.Abstractions;
using Pottmayer.Tars.Caching.Redis.Options;
using StackExchange.Redis;

namespace Pottmayer.Tars.Caching.Redis
{
    /// <summary>
    /// Redis-backed <see cref="ICacheStore"/> using StackExchange.Redis. Each entry is stored as a Redis hash
    /// holding the serialized payload plus optional absolute-deadline and sliding-expiration metadata, so both
    /// expiration modes are honored (sliding TTL is renewed on read). Read failures are logged and treated as
    /// cache misses rather than surfaced to the caller.
    /// </summary>
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
        private readonly IOptionsMonitor<RedisCachingOptions> _cacheOptionsMonitor;
        private readonly ILogger<RedisCacheStore> _logger;

        /// <summary>
        /// Creates the store over the given Redis database, key builder, serializer and caching options.
        /// </summary>
        /// <param name="db">The Redis database to operate on.</param>
        /// <param name="keys">Builder that namespaces logical keys into storage keys.</param>
        /// <param name="serializer">Serializer used for entry payloads.</param>
        /// <param name="cacheOptionsMonitor">Caching options, watched for the default expiration.</param>
        /// <param name="logger">Logger for best-effort/failed Redis operations.</param>
        public RedisCacheStore(
            IDatabase db,
            ICacheKeyBuilder keys,
            ICacheSerializer serializer,
            IOptionsMonitor<RedisCachingOptions> cacheOptionsMonitor,
            ILogger<RedisCacheStore> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _keys = keys ?? throw new ArgumentNullException(nameof(keys));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _cacheOptionsMonitor = cacheOptionsMonitor ?? throw new ArgumentNullException(nameof(cacheOptionsMonitor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public async ValueTask<T?> GetAsync<T>(string key, CancellationToken ct = default)
        {
            var result = await TryGetInternalAsync<T>(key, ct).ConfigureAwait(false);
            return result.Found ? result.Value : default;
        }

        /// <inheritdoc/>
        public ValueTask<CacheGetResult<T>> TryGetAsync<T>(string key, CancellationToken ct = default)
            => TryGetInternalAsync<T>(key, ct);

        /// <inheritdoc/>
        public async ValueTask RemoveAsync(string key, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var k = _keys.Build(key);
            await _db.KeyDeleteAsync(k).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async ValueTask<bool> ExistsAsync(string key, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var k = _keys.Build(key);
            return await _db.KeyExistsAsync(k).ConfigureAwait(false);
        }

        /// <inheritdoc/>
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

        /// <summary>
        /// Core read path: fetches the hash, enforces absolute and sliding expiration (deleting expired keys
        /// and renewing sliding TTL on hit), deserializes the payload, and treats any Redis or deserialization
        /// failure as a miss.
        /// </summary>
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

        /// <summary>
        /// Resolves the absolute TTL and its UTC deadline ticks from the entry options, falling back to the
        /// configured default. Returns <c>(null, null)</c> when no positive absolute expiration applies.
        /// </summary>
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

        /// <summary>
        /// Resolves the sliding expiration in milliseconds from the entry options, or <c>null</c> when none
        /// (or non-positive) is set.
        /// </summary>
        private static int? ResolveSlidingExpirationMs(CacheEntryOptions? options)
        {
            if (options?.SlidingExpiration is null)
                return null;

            if (options.SlidingExpiration <= TimeSpan.Zero)
                return null;

            var ms = (int)Math.Min(int.MaxValue, options.SlidingExpiration.Value.TotalMilliseconds);
            return ms;
        }

        /// <summary>
        /// Computes the TTL to apply on write as the smaller of the absolute and sliding windows, or
        /// <c>null</c> when neither is set.
        /// </summary>
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

        /// <summary>
        /// Computes the renewed TTL on access: the sliding window, capped by any remaining time until the
        /// absolute deadline. Returns <see cref="TimeSpan.Zero"/> when the absolute deadline has passed.
        /// </summary>
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

        /// <summary>Reads a nullable <see cref="long"/> from a Redis hash field, mapping a null field to <c>null</c>.</summary>
        private static long? TryReadInt64(RedisValue value)
        {
            if (value.IsNull) return null;
            return (long)value;
        }

        /// <summary>Reads a nullable <see cref="int"/> from a Redis hash field, mapping a null field to <c>null</c>.</summary>
        private static int? TryReadInt32(RedisValue value)
        {
            if (value.IsNull) return null;
            return (int)value;
        }

        /// <summary>
        /// Observes a best-effort background Redis operation, logging a warning if it faults without
        /// propagating the failure to the caller.
        /// </summary>
        private void FireAndForget(Task task, RedisKey key, string operation)
        {
            task.ContinueWith(
                t => _logger.LogWarning(t.Exception, "Redis best-effort operation '{Operation}' failed for key '{Key}'.", operation, (string)key),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }

        /// <summary>Redis hash field names for the stored payload and expiration metadata.</summary>
        private static class RedisHashFields
        {
            public static readonly RedisValue Value = "v";
            public static readonly RedisValue AbsoluteDeadlineUtcTicks = "ad";
            public static readonly RedisValue SlidingExpirationMs = "s";
        }
    }
}
