using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Caching.Abstractions;
using Pottmayer.Tars.Caching.Redis.Options;
using StackExchange.Redis;

namespace Pottmayer.Tars.Caching.Redis.DI
{
    /// <summary>
    /// Registration helpers for the Redis caching provider's services: the shared connection multiplexer, the
    /// database resolved from it, and the Redis <see cref="Abstractions.ICacheStore"/>.
    /// </summary>
    public static class RedisCachingServicesDI
    {
        /// <summary>
        /// Registers the Redis <see cref="IDatabase"/> (singleton, via <c>TryAdd</c>), resolved from the shared
        /// <see cref="IConnectionMultiplexer"/> using the configured <see cref="RedisCachingOptions.Database"/>.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddTarsRedisDatabase(this IServiceCollection services)
        {
            services.TryAddSingleton<IDatabase>(sp =>
            {
                var mux = sp.GetRequiredService<IConnectionMultiplexer>();
                var opts = sp.GetRequiredService<IOptionsMonitor<RedisCachingOptions>>().CurrentValue;
                return mux.GetDatabase(opts.Database ?? -1);
            });

            return services;
        }

        /// <summary>
        /// Registers the shared <see cref="IConnectionMultiplexer"/> (singleton, via <c>TryAdd</c>), connected
        /// from <see cref="RedisCachingOptions.ToConfigurationOptions"/>. The multiplexer is thread-safe and
        /// meant to be reused across the process.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddTarsRedisConnectionMultiplexer(this IServiceCollection services)
        {
            services.TryAddSingleton<IConnectionMultiplexer>(sp =>
            {
                var opts = sp.GetRequiredService<IOptionsMonitor<RedisCachingOptions>>().CurrentValue;
                var cfg = opts.ToConfigurationOptions();

                // ConnectionMultiplexer is thread-safe and should be shared (multiplexing).
                return ConnectionMultiplexer.Connect(cfg);
            });

            return services;
        }

        /// <summary>
        /// Registers <see cref="RedisCacheStore"/> as the <see cref="ICacheStore"/> implementation (singleton,
        /// via <c>TryAdd</c>). Requires the Redis database and the tars key builder and serializer to be
        /// registered as well.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddTarsRedisCacheProvider(this IServiceCollection services)
        {
            services.TryAddSingleton<ICacheStore, RedisCacheStore>();
            return services;
        }
    }
}
