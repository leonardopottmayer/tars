using Pottmayer.Tars.Caching.Abstractions;
using Pottmayer.Tars.Caching.Options;

namespace Pottmayer.Tars.Caching
{
    /// <summary>
    /// Default <see cref="ICacheKeyBuilder"/>: prefixes each key with <see cref="CachingOptions.KeyPrefix"/>
    /// joined by <see cref="CachingOptions.KeySeparator"/>. The prefix and separator are read once at
    /// construction, so they stay stable for the lifetime of the (singleton) builder.
    /// </summary>
    public sealed class DefaultCacheKeyBuilder : ICacheKeyBuilder
    {
        private readonly string _keyPrefix;
        private readonly string _keySeparator;

        /// <summary>
        /// Creates the builder, capturing the key prefix and separator from <paramref name="options"/>.
        /// </summary>
        /// <param name="options">Caching options carrying the key prefix and separator.</param>
        public DefaultCacheKeyBuilder(CachingOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            _keyPrefix = options.KeyPrefix;
            _keySeparator = options.KeySeparator;
        }

        /// <inheritdoc/>
        public string Build(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be null/empty.", nameof(key));

            return string.Concat(_keyPrefix, _keySeparator, key);
        }
    }
}
