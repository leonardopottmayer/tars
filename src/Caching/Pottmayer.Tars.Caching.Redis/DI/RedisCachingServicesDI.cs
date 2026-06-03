using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Caching.Abstractions;
using Pottmayer.Tars.Caching.Redis.Options;
using StackExchange.Redis;

namespace Pottmayer.Tars.Caching.Redis.DI
{
    public static class RedisCachingServicesDI
    {
        public static IServiceCollection AddTarsRedisDatabase(this IServiceCollection services)
        {
            services.TryAddSingleton<IDatabase>(sp =>
            {
                var mux = sp.GetRequiredService<IConnectionMultiplexer>();
                var opts = sp.GetRequiredService<IOptionsMonitor<RedisCacheOptions>>().CurrentValue;
                return mux.GetDatabase(opts.Database ?? -1);
            });

            return services;
        }

        public static IServiceCollection AddTarsRedisConnectionMultiplexer(this IServiceCollection services)
        {
            services.TryAddSingleton<IConnectionMultiplexer>(sp =>
            {
                var opts = sp.GetRequiredService<IOptionsMonitor<RedisCacheOptions>>().CurrentValue;
                var cfg = opts.ToConfigurationOptions();

                // ConnectionMultiplexer is thread-safe and should be shared (multiplexing).
                return ConnectionMultiplexer.Connect(cfg);
            });

            return services;
        }

        public static IServiceCollection AddTarsRedisCacheProvider(this IServiceCollection services)
        {
            services.TryAddSingleton<ICacheStore, RedisCacheStore>();
            return services;
        }
    }
}
