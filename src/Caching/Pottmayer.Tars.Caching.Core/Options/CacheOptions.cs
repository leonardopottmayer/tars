namespace Pottmayer.Tars.Caching.Core.Options
{
    public sealed class CacheOptions
    {
        public const string SectionName = "Tars:Caching";

        public const string ValidationErrorMessage =
            "Invalid CacheOptions. KeyPrefix/KeySeparator are required and DefaultAbsoluteExpirationRelativeToNow must be positive when provided.";

        /// <summary>
        /// Prefix used by the default key builder (e.g. "tars", "my-service", "prod:service").
        /// </summary>
        public string KeyPrefix { get; init; } = "tars-cache";

        /// <summary>
        /// Separator used by the default key builder (":" is a strong default for Redis too).
        /// </summary>
        public string KeySeparator { get; init; } = ":";

        /// <summary>
        /// Optional default expiration applied when the caller does NOT provide CacheEntryOptions.
        /// Null means "no default TTL".
        /// </summary>
        public TimeSpan? DefaultAbsoluteExpirationRelativeToNow { get; init; } = null;

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(KeyPrefix))
                return false;

            if (string.IsNullOrWhiteSpace(KeySeparator))
                return false;

            if (DefaultAbsoluteExpirationRelativeToNow is not null &&
                DefaultAbsoluteExpirationRelativeToNow <= TimeSpan.Zero)
                return false;

            return true;
        }
    }
}
