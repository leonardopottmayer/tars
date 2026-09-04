using Microsoft.Extensions.Options;
using Pottmayer.Tars.Ai.Abstractions;
using Pottmayer.Tars.Ai.Chat.Abstractions;
using Pottmayer.Tars.Ai.Chat.Abstractions.Models;
using Pottmayer.Tars.Ai.Chat.Gemini.Options;
using Pottmayer.Tars.Ai.Chat.Gemini.Wire;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pottmayer.Tars.Ai.Chat.Gemini;

/// <summary>
/// An <see cref="IAiChatCompletionClient"/> over Gemini's generateContent endpoint. Stateless: the model
/// and temperature come from each <see cref="ChatRequest"/>. The base address is configured on the
/// injected <see cref="HttpClient"/>; the API key is taken from the request when it carries one
/// (<see cref="ChatRequest.ApiKey"/>) and otherwise from <see cref="GeminiAiOptions.ApiKey"/>, so a
/// multi-user host can pass each user's own key per call.
/// </summary>
public sealed class GeminiAiChatCompletionClient(HttpClient httpClient, IOptions<GeminiAiOptions> options) : IAiChatCompletionClient
{
    /// <summary>The key this provider registers under, used with the client factory.</summary>
    public const string ProviderName = "gemini";

    private static readonly JsonSerializerOptions Json = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string? _defaultApiKey = options.Value.ApiKey;

    public async Task<ChatCompletion> CompleteAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var apiKey = string.IsNullOrEmpty(request.ApiKey) ? _defaultApiKey : request.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new AiException(
                ProviderName,
                "No Gemini API key: set ChatRequest.ApiKey on the request or GeminiAiOptions.ApiKey.",
                isPermanent: true,
                model: request.Model);
        }

        GeminiRequest wire;
        try
        {
            wire = GeminiWireMapper.ToWireRequest(request);
        }
        catch (JsonException ex)
        {
            throw new AiException(
                ProviderName,
                "A tool's ParametersJsonSchema is not valid JSON.",
                isPermanent: true,
                model: request.Model,
                innerException: ex);
        }

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"v1beta/models/{request.Model}:generateContent")
        {
            Content = JsonContent.Create(wire, options: Json),
        };
        httpRequest.Headers.Add("x-goog-api-key", apiKey);

        HttpResponseMessage httpResponse;
        try
        {
            httpResponse = await httpClient.SendAsync(httpRequest, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new AiException(
                ProviderName,
                $"Could not reach the Gemini endpoint: {ex.Message}",
                isPermanent: false,
                model: request.Model,
                innerException: ex);
        }

        using (httpResponse)
        {
            if (!httpResponse.IsSuccessStatusCode)
            {
                var body = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                throw GeminiErrorClassifier.Classify(request.Model, httpResponse.StatusCode, body);
            }

            GeminiResponse? payload;
            try
            {
                payload = await httpResponse.Content.ReadFromJsonAsync<GeminiResponse>(Json, cancellationToken);
            }
            catch (JsonException ex)
            {
                throw new AiException(
                    ProviderName,
                    "The Gemini response was not valid JSON.",
                    isPermanent: false,
                    model: request.Model,
                    innerException: ex);
            }

            if (payload is null)
            {
                throw new AiException(
                    ProviderName,
                    "The Gemini response was empty.",
                    isPermanent: false,
                    model: request.Model);
            }

            return GeminiWireMapper.ToCompletion(request, payload);
        }
    }
}
