namespace Pottmayer.Tars.Caching.Redis.Options
{
    /// <summary>
    /// Validation entry point for <see cref="RedisCachingOptions"/>, wired into the options pipeline by
    /// <c>AddTarsRedisCachingOptions</c> and run on application start.
    /// </summary>
    internal static class RedisCachingOptionsValidation
    {
        /// <summary>
        /// Validates the bound <see cref="RedisCachingOptions"/> instance.
        /// </summary>
        /// <param name="options">The options instance to validate.</param>
        /// <returns><c>true</c> when non-null and <see cref="RedisCachingOptions.IsValid"/>; otherwise <c>false</c>.</returns>
        public static bool Validate(RedisCachingOptions options)
            => options is not null && options.IsValid();
    }
}

