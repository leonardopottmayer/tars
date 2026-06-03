using Microsoft.Extensions.Options;
using Pottmayer.Tars.Caching.Abstractions;
using Pottmayer.Tars.Caching.Core.Options;

namespace Pottmayer.Tars.Caching.Core
{
    public sealed class DefaultCacheKeyBuilder : ICacheKeyBuilder
    {
        private readonly IOptionsMonitor<CacheOptions> _optionsMonitor;

        public DefaultCacheKeyBuilder(IOptionsMonitor<CacheOptions> optionsMonitor)
        {
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        }

        public string Build(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key cannot be null/empty.", nameof(key));

            var options = _optionsMonitor.CurrentValue;
            return string.Concat(options.KeyPrefix, options.KeySeparator, key);
        }
    }
}
