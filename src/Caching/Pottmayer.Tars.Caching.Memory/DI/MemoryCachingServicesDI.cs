using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Caching.Abstractions;

namespace Pottmayer.Tars.Caching.Memory.DI
{
    public static class MemoryCachingServicesDI
    {
        public static IServiceCollection AddTarsMemoryCacheProvider(this IServiceCollection services)
        {
            services.TryAddSingleton<ICacheStore, MemoryCacheStore>();
            return services;
        }
    }
}
