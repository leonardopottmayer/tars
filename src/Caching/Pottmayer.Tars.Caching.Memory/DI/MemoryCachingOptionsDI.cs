using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Caching.Memory.Options;
using Pottmayer.Tars.Caching.Core.Options;

namespace Pottmayer.Tars.Caching.Memory.DI
{
    public static class MemoryCachingOptionsDI
    {
        public static OptionsBuilder<CacheOptions> AddTarsCachingOptions(
            this IHostApplicationBuilder builder,
            string? sectionName = null,
            Action<CacheOptions>? configure = null)
        {
            sectionName ??= CacheOptions.SectionName;

            var section = builder.Configuration.GetSection(sectionName);

            var ob = builder.Services
                .AddOptions<CacheOptions>()
                .Bind(section)
                .Validate(CacheOptionsValidation.Validate, CacheOptions.ValidationErrorMessage)
                .ValidateOnStart();

            if (configure is not null)
                ob.Configure(configure);

            return ob;
        }
    }
}
