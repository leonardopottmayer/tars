using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pottmayer.Tars.Ai.Chat.Abstractions;

namespace Pottmayer.Tars.Ai.Chat.DI;

public static class AiServicesDI
{
    /// <summary>
    /// Registers the <see cref="IAiChatCompletionClientFactory"/> that resolves providers by name. Registered
    /// via <c>TryAdd</c>, so it is idempotent; call it once alongside your provider registrations.
    /// </summary>
    public static IServiceCollection AddTarsAiClientFactory(this IServiceCollection services)
    {
        services.TryAddSingleton<IAiChatCompletionClientFactory, KeyedAiChatCompletionClientFactory>();
        return services;
    }
}
