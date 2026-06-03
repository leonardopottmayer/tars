namespace Pottmayer.Tars.Caching.Core.Options
{
    internal static class CacheOptionsValidation
    {
        public static bool Validate(CacheOptions options)
            => options is not null && options.IsValid();
    }
}

