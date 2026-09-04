using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Pottmayer.Tars.Ai.Chat.Abstractions;
using Pottmayer.Tars.Ai.Chat.Gemini.Options;

namespace Pottmayer.Tars.Ai.Chat.Gemini.DI;

/// <summary>
/// Registration helpers for the Gemini chat provider's services: the typed <see cref="HttpClient"/> that
/// backs <see cref="GeminiAiChatCompletionClient"/>, and the keyed <see cref="IAiChatCompletionClient"/> entry.
/// </summary>
public static class GeminiAiServicesDI
{
    /// <summary>
    /// Registers <see cref="GeminiAiChatCompletionClient"/> on a typed <see cref="HttpClient"/> carrying the base
    /// address and timeout from <see cref="GeminiAiOptions"/>. The API key is applied per request
    /// (<see cref="Abstractions.Models.ChatRequest.ApiKey"/>, falling back to the options), so it is not a
    /// client default header. Requires <see cref="GeminiAiOptions"/> to be registered (via
    /// <see cref="GeminiAiOptionsDI.AddTarsAiChatGeminiOptions"/>).
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddTarsAiChatGeminiHttpClient(this IServiceCollection services)
    {
        services.AddHttpClient<GeminiAiChatCompletionClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<GeminiAiOptions>>().Value;

            var baseUrl = options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = options.RequestTimeout;
        });

        return services;
    }

    /// <summary>
    /// Registers <see cref="GeminiAiChatCompletionClient"/> as the <see cref="IAiChatCompletionClient"/> keyed by
    /// <see cref="GeminiAiChatCompletionClient.ProviderName"/> (<c>gemini</c>), via <c>TryAdd</c>. Requires the typed
    /// client (via <see cref="AddTarsAiChatGeminiHttpClient"/>) and the client factory (via
    /// <c>AddTarsAiClientFactory</c>) to be registered as well.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddTarsAiChatCompletionClientGemini(this IServiceCollection services)
    {
        services.TryAddKeyedTransient<IAiChatCompletionClient>(
            GeminiAiChatCompletionClient.ProviderName, (sp, _) => sp.GetRequiredService<GeminiAiChatCompletionClient>());

        return services;
    }
}
