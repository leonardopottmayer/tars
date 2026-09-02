using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Caching.Abstractions;

namespace Pottmayer.Tars.Caching.Memory.DI
{
    /// <summary>
    /// Registration helper for the in-memory caching provider's <see cref="Abstractions.ICacheStore"/>.
    /// </summary>
    public static class MemoryCachingServicesDI
    {
        /// <summary>
        /// Registers <see cref="MemoryCacheStore"/> as the <see cref="ICacheStore"/> implementation (singleton,
        /// via <c>TryAdd</c>). Requires an <c>IMemoryCache</c> and the tars key builder to be registered as well.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddTarsMemoryCacheProvider(this IServiceCollection services)
        {
            services.TryAddSingleton<ICacheStore, MemoryCacheStore>();
            return services;
        }
    }
}
