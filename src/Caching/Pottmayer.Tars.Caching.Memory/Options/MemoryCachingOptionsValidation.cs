namespace Pottmayer.Tars.Caching.Memory.Options
{
    /// <summary>
    /// Validation entry point for <see cref="MemoryCachingOptions"/>, wired into the options pipeline by
    /// <c>AddTarsMemoryCachingOptions</c> and run on application start.
    /// </summary>
    internal static class MemoryCachingOptionsValidation
    {
        /// <summary>
        /// Validates the bound <see cref="MemoryCachingOptions"/> instance.
        /// </summary>
        /// <param name="options">The options instance to validate.</param>
        /// <returns><c>true</c> when non-null and <see cref="MemoryCachingOptions.IsValid"/>; otherwise <c>false</c>.</returns>
        public static bool Validate(MemoryCachingOptions options)
            => options is not null && options.IsValid();
    }
}
