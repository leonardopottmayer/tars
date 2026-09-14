using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
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
    /// <para>
    /// A retry handler wraps the client so that transient failures — 503 (the model is overloaded), 429
    /// (rate limited), other 5xx, request timeouts and network errors — are retried with exponential
    /// backoff and jitter before the failure reaches the caller. The server's <c>Retry-After</c> header,
    /// which Gemini sends on 429/503, takes precedence over the computed backoff. Retries are transparent
    /// to <see cref="GeminiAiChatCompletionClient"/>: only the final response is classified into an
    /// <see cref="Abstractions.AiException"/>. Attempt count and base delay come from
    /// <see cref="GeminiAiOptions.MaxRetryAttempts"/> and <see cref="GeminiAiOptions.RetryBaseDelay"/>;
    /// setting the count to 0 disables retrying.
    /// </para>
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
        })
        .AddResilienceHandler("tars-gemini-retry", (pipeline, context) =>
        {
            var options = context.ServiceProvider.GetRequiredService<IOptions<GeminiAiOptions>>().Value;

            // Polly requires MaxRetryAttempts >= 1, so a configured 0 means "no retry": leave the
            // pipeline empty rather than add a strategy that would fail options validation.
            if (options.MaxRetryAttempts <= 0)
                return;

            // The default ShouldHandle (HttpClientResiliencePredicates.IsTransient) already covers
            // 408/429/500/502/503/504, HttpRequestException and timeouts — the same set the error
            // classifier treats as transient — so it does not need to be restated here.
            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxRetryAttempts,
                Delay = options.RetryBaseDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldRetryAfterHeader = true,
            });
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
