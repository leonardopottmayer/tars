using Pottmayer.Tars.Caching.Options;

namespace Pottmayer.Tars.Caching.Memory.Options
{
    /// <summary>
    /// Options for the in-memory caching provider. Carries the shared caching settings from
    /// <see cref="CachingOptions"/>; the in-memory store needs no connection settings of its own.
    /// </summary>
    public sealed class MemoryCachingOptions : CachingOptions
    {
        /// <summary>Default configuration section these options bind from (<c>Tars:Caching:Memory</c>).</summary>
        public const string SectionName = "Tars:Caching:Memory";

        /// <summary>Message reported when validation fails on application start.</summary>
        public const string ValidationErrorMessage =
            "Invalid MemoryCachingOptions. KeyPrefix/KeySeparator are required and DefaultAbsoluteExpirationRelativeToNow must be positive when provided.";
    }
}
