using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Communication.Telegram.Abstractions;

namespace Pottmayer.Tars.Communication.Telegram.DI;

public static class TelegramServicesDI
{
    /// <summary>
    /// Registers <see cref="TelegramBotClient"/> as the <see cref="ITelegramClient"/>, on a typed
    /// <see cref="HttpClient"/>. Pair with
    /// <see cref="TelegramOptionsDI.AddTarsTelegramOptions"/> to supply the bot token.
    /// </summary>
    /// <remarks>
    /// The handler's timeout is disabled on purpose: every call sets its own deadline, because a long
    /// poll legitimately waits far longer than any sane default and would otherwise be cancelled by
    /// its own transport.
    /// </remarks>
    public static IServiceCollection AddTarsTelegramClient(this IServiceCollection services)
    {
        services
            .AddHttpClient<ITelegramClient, TelegramBotClient>(client => client.Timeout = Timeout.InfiniteTimeSpan);

        return services;
    }
}
