using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Caching.Abstractions;
using Pottmayer.Tars.Caching.Options;

namespace Pottmayer.Tars.Caching.DI
{
    /// <summary>
    /// Registration helpers for the shared caching building blocks (key builder and serializer). Each
    /// provider composes these with its own options and store registrations; the consumer calls them
    /// separately.
    /// </summary>
    public static class CachingServicesDI
    {
        /// <summary>
        /// Registers the default <see cref="ICacheKeyBuilder"/> (<see cref="DefaultCacheKeyBuilder"/>) as a
        /// singleton, reading its prefix and separator from the bound <typeparamref name="TOptions"/>. Call
        /// after the provider's options have been registered. Uses <c>TryAdd</c>, so a key builder already
        /// registered is preserved.
        /// </summary>
        /// <typeparam name="TOptions">The concrete caching options to read the key prefix/separator from.</typeparam>
        /// <param name="services">The service collection to register into.</param>
        /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddTarsCacheKeyBuilder<TOptions>(this IServiceCollection services)
            where TOptions : CachingOptions
        {
            services.TryAddSingleton<ICacheKeyBuilder>(sp =>
                new DefaultCacheKeyBuilder(sp.GetRequiredService<IOptions<TOptions>>().Value));
            return services;
        }

        /// <summary>
        /// Registers the default <see cref="ICacheSerializer"/> (<see cref="SystemTextJsonCacheSerializer"/>)
        /// as a singleton. Uses <c>TryAdd</c>, so a serializer already registered is preserved.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection AddTarsCacheSerializer(this IServiceCollection services)
        {
            services.TryAddSingleton<ICacheSerializer, SystemTextJsonCacheSerializer>();
            return services;
        }
    }
}
