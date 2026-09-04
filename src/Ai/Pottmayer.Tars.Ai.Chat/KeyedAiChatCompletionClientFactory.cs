using Microsoft.Extensions.DependencyInjection;
using Pottmayer.Tars.Ai.Abstractions;
using Pottmayer.Tars.Ai.Chat.Abstractions;

namespace Pottmayer.Tars.Ai.Chat;

/// <summary>
/// Resolves providers through keyed DI: each provider registers its <see cref="IAiChatCompletionClient"/>
/// under a service key equal to its name, and this looks it up by that key.
/// </summary>
internal sealed class KeyedAiChatCompletionClientFactory(IServiceProvider services) : IAiChatCompletionClientFactory
{
    public IAiChatCompletionClient GetClient(string provider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);

        return services.GetKeyedService<IAiChatCompletionClient>(provider)
            ?? throw new AiException(
                provider,
                $"No chat completion client is registered for provider '{provider}'.",
                isPermanent: true);
    }
}
