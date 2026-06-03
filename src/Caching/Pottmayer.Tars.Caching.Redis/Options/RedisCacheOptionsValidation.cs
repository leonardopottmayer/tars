namespace Pottmayer.Tars.Caching.Redis.Options
{
    internal static class RedisCacheOptionsValidation
    {
        public static bool Validate(RedisCacheOptions options)
            => options is not null && options.IsValid();
    }
}

