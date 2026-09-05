using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Communication.Telegram.Options;

namespace Pottmayer.Tars.Communication.Telegram.DI;

public static class TelegramOptionsDI
{
    /// <summary>
    /// Binds <see cref="TelegramOptions"/> — the set of bots under <see cref="TelegramOptions.Bots"/> — from
    /// configuration (default section <c>Tars:Communication:Telegram</c>) and validates every bot on start.
    /// </summary>
    /// <param name="builder">The host application builder whose configuration and services are used.</param>
    /// <param name="sectionName">Configuration section to bind. Defaults to <see cref="TelegramOptions.SectionName"/>.</param>
    /// <param name="configure">Optional code-based overrides applied after binding.</param>
    public static OptionsBuilder<TelegramOptions> AddTarsTelegramOptions(
        this IHostApplicationBuilder builder,
        string? sectionName = null,
        Action<TelegramOptions>? configure = null)
    {
        sectionName ??= TelegramOptions.SectionName;

        var ob = builder.Services
            .AddOptions<TelegramOptions>()
            .Bind(builder.Configuration.GetSection(sectionName))
            .Validate(TelegramOptionsValidation.Validate, TelegramOptions.ValidationErrorMessage)
            .ValidateOnStart();

        if (configure is not null)
            ob.Configure(configure);

        return ob;
    }
}
