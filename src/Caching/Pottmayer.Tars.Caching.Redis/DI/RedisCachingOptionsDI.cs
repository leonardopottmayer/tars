using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Caching.Redis.Options;

namespace Pottmayer.Tars.Caching.Redis.DI
{
    public static class RedisCachingOptionsDI
    {
        public static OptionsBuilder<RedisCacheOptions> AddTarsRedisCachingOptions(
            this IHostApplicationBuilder builder,
            string? sectionName = null,
            Action<RedisCacheOptions>? configure = null)
        {
            sectionName ??= RedisCacheOptions.SectionName;

            var section = builder.Configuration.GetSection(sectionName);

            var ob = builder.Services
                .AddOptions<RedisCacheOptions>()
                .Bind(section)
                .Validate(RedisCacheOptionsValidation.Validate, RedisCacheOptions.ValidationErrorMessage)
                .ValidateOnStart();

            if (configure is not null)
                ob.Configure(configure);

            return ob;
        }
    }
}

