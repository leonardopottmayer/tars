namespace Pottmayer.Tars.Caching.Options
{
    /// <summary>
    /// Validation entry point for any <see cref="CachingOptions"/>, wired into the options pipeline by each
    /// provider's options binder (e.g. <c>AddTarsRedisCachingOptions</c>) and run on application start.
    /// Validation is polymorphic: it defers to the concrete options' <see cref="CachingOptions.IsValid"/>.
    /// </summary>
    public static class CachingOptionsValidation
    {
        /// <summary>
        /// Validates the bound <see cref="CachingOptions"/> instance.
        /// </summary>
        /// <param name="options">The options instance to validate.</param>
        /// <returns><c>true</c> when non-null and <see cref="CachingOptions.IsValid"/>; otherwise <c>false</c>.</returns>
        public static bool Validate(CachingOptions options)
            => options is not null && options.IsValid();
    }
}
