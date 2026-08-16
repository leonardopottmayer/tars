using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Communication.Telegram.Options;

namespace Pottmayer.Tars.Communication.Telegram.DI;

public static class TelegramOptionsDI
{
    /// <summary>
    /// Binds <see cref="TelegramOptions"/> from configuration (default section
    /// <c>Tars:Communication:Telegram</c>).
    /// </summary>
    public static OptionsBuilder<TelegramOptions> AddTarsTelegramOptions(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<TelegramOptions>? configure = null)
    {
        sectionName ??= TelegramOptions.SectionName;

        var ob = builder.Services
            .AddOptions<TelegramOptions>()
            .Bind(builder.Configuration.GetSection(sectionName));

        if (configure is not null)
            ob.Configure(configure);

        return ob;
    }
}
