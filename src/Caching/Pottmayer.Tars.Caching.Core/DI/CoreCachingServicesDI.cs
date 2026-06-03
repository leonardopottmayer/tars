using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Caching.Abstractions;
using Pottmayer.Tars.Caching.Core;

namespace Pottmayer.Tars.Caching.Core.DI
{
    public static class CoreCachingServicesDI
    {
        public static IServiceCollection AddTarsCacheKeyBuilder(this IServiceCollection services)
        {
            services.TryAddSingleton<ICacheKeyBuilder, DefaultCacheKeyBuilder>();
            return services;
        }

        public static IServiceCollection AddTarsCacheKeyBuilder<TKeyBuilder>(this IServiceCollection services)
            where TKeyBuilder : class, ICacheKeyBuilder
        {
            services.TryAddSingleton<ICacheKeyBuilder, TKeyBuilder>();
            return services;
        }

        public static IServiceCollection AddTarsCacheSerializer(this IServiceCollection services)
        {
            services.TryAddSingleton<ICacheSerializer, SystemTextJsonCacheSerializer>();
            return services;
        }

        public static IServiceCollection AddTarsCacheSerializer<TSerializer>(this IServiceCollection services)
            where TSerializer : class, ICacheSerializer
        {
            services.TryAddSingleton<ICacheSerializer, TSerializer>();
            return services;
        }
    }
}
