using Pottmayer.Tars.Caching.Core.Options;

namespace Pottmayer.Tars.Caching.Memory.Options
{
    internal static class CacheOptionsValidation
    {
        public static bool Validate(CacheOptions options)
            => options is not null && options.IsValid();
    }
}
