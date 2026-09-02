namespace Pottmayer.Tars.Caching.Options
{
    /// <summary>
    /// What every caching provider shares, whatever store sits underneath: the key-building inputs and the
    /// fallback expiration. Concrete providers extend this with their own connection/tuning settings and
    /// their own configuration section.
    /// </summary>
    public abstract class CachingOptions
    {
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

        /// <summary>
        /// Returns <c>true</c> when the shared options are internally consistent: key prefix and separator
        /// are non-blank and the default expiration, when set, is strictly positive. Providers override this
        /// to add their own checks, calling <c>base.IsValid()</c> first.
        /// </summary>
        public virtual bool IsValid()
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
