using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Communication.Telegram.Abstractions;

namespace Pottmayer.Tars.Communication.Telegram.DI;

public static class TelegramServicesDI
{
    /// <summary>The logical name of the <see cref="HttpClient"/> shared by every bot the factory builds.</summary>
    internal const string HttpClientName = "Tars.Communication.Telegram";

    /// <summary>
    /// Registers the shared <see cref="HttpClient"/> every bot uses, the transport the
    /// <see cref="ITelegramClientFactory"/> hands each client. Required alongside
    /// <see cref="AddTarsTelegramClientFactory"/>: without it the factory's clients fall back to a 100-second
    /// default that kills long polling.
    /// </summary>
    /// <remarks>
    /// The handler timeout is disabled on purpose: every call sets its own deadline, because a long poll
    /// legitimately waits far longer than any sane default and would otherwise be cancelled by its own
    /// transport.
    /// </remarks>
    public static IServiceCollection AddTarsTelegramHttpClient(this IServiceCollection services)
    {
        services.AddHttpClient(HttpClientName, client => client.Timeout = Timeout.InfiniteTimeSpan);

        return services;
    }

    /// <summary>
    /// Registers the <see cref="ITelegramClientFactory"/> that resolves a bot by name from
    /// <see cref="Options.TelegramOptions.Bots"/>. Pair with
    /// <see cref="TelegramOptionsDI.AddTarsTelegramOptions"/> for the bots and
    /// <see cref="AddTarsTelegramHttpClient"/> for their transport. Registered via <c>TryAdd</c>, so it is
    /// idempotent.
    /// </summary>
    public static IServiceCollection AddTarsTelegramClientFactory(this IServiceCollection services)
    {
        services.TryAddSingleton<ITelegramClientFactory, TelegramClientFactory>();

        return services;
    }
}
