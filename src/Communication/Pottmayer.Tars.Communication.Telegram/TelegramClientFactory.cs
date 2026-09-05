using Microsoft.Extensions.Options;
using Pottmayer.Tars.Communication.Telegram.Abstractions;
using Pottmayer.Tars.Communication.Telegram.DI;
using Pottmayer.Tars.Communication.Telegram.Options;

namespace Pottmayer.Tars.Communication.Telegram;

/// <summary>
/// Resolves a bot by looking its name up in <see cref="TelegramOptions.Bots"/> and building a
/// <see cref="TelegramBotClient"/> for it on the shared <see cref="HttpClient"/>. Data-driven: adding a
/// bot is a configuration entry, not a registration.
/// </summary>
internal sealed class TelegramClientFactory(
    IHttpClientFactory httpClientFactory,
    IOptions<TelegramOptions> options) : ITelegramClientFactory
{
    public ITelegramClient GetClient(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!options.Value.Bots.TryGetValue(name, out var bot))
        {
            throw new TelegramException(
                "getClient",
                $"No Telegram bot is configured under the name '{name}'.",
                isPermanent: true);
        }

        var http = httpClientFactory.CreateClient(TelegramServicesDI.HttpClientName);
        return new TelegramBotClient(http, bot);
    }
}
