using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Caching.Redis.Options;

namespace Pottmayer.Tars.Caching.Redis.DI
{
    /// <summary>
    /// Registration helper that binds <see cref="Options.RedisCachingOptions"/> from configuration.
    /// </summary>
    public static class RedisCachingOptionsDI
    {
        /// <summary>
        /// Binds <see cref="RedisCachingOptions"/> from configuration (default section
        /// <see cref="RedisCachingOptions.SectionName"/>, i.e. <c>Tars:Caching:Redis</c>) and validates it on
        /// application start.
        /// </summary>
        /// <param name="builder">The host application builder whose configuration and services are used.</param>
        /// <param name="sectionName">Configuration section to bind. Defaults to <see cref="RedisCachingOptions.SectionName"/>.</param>
        /// <param name="configure">Optional code-based overrides applied after binding.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> for further configuration.</returns>
        public static OptionsBuilder<RedisCachingOptions> AddTarsRedisCachingOptions(
            this IHostApplicationBuilder builder,
            string? sectionName = null,
            Action<RedisCachingOptions>? configure = null)
        {
            sectionName ??= RedisCachingOptions.SectionName;

            var section = builder.Configuration.GetSection(sectionName);

            var ob = builder.Services
                .AddOptions<RedisCachingOptions>()
                .Bind(section)
                .Validate(RedisCachingOptionsValidation.Validate, RedisCachingOptions.ValidationErrorMessage)
                .ValidateOnStart();

            if (configure is not null)
                ob.Configure(configure);

            return ob;
        }
    }
}

