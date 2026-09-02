using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Caching.Memory.Options;

namespace Pottmayer.Tars.Caching.Memory.DI
{
    /// <summary>
    /// Registration helper that binds <see cref="Options.MemoryCachingOptions"/> from configuration.
    /// </summary>
    public static class MemoryCachingOptionsDI
    {
        /// <summary>
        /// Binds <see cref="MemoryCachingOptions"/> from configuration (default section
        /// <see cref="MemoryCachingOptions.SectionName"/>, i.e. <c>Tars:Caching:Memory</c>) and validates it on
        /// application start.
        /// </summary>
        /// <param name="builder">The host application builder whose configuration and services are used.</param>
        /// <param name="sectionName">Configuration section to bind. Defaults to <see cref="MemoryCachingOptions.SectionName"/>.</param>
        /// <param name="configure">Optional code-based overrides applied after binding.</param>
        /// <returns>The <see cref="OptionsBuilder{TOptions}"/> for further configuration.</returns>
        public static OptionsBuilder<MemoryCachingOptions> AddTarsMemoryCachingOptions(
            this IHostApplicationBuilder builder,
            string? sectionName = null,
            Action<MemoryCachingOptions>? configure = null)
        {
            sectionName ??= MemoryCachingOptions.SectionName;

            var section = builder.Configuration.GetSection(sectionName);

            var ob = builder.Services
                .AddOptions<MemoryCachingOptions>()
                .Bind(section)
                .Validate(MemoryCachingOptionsValidation.Validate, MemoryCachingOptions.ValidationErrorMessage)
                .ValidateOnStart();

            if (configure is not null)
                ob.Configure(configure);

            return ob;
        }
    }
}
