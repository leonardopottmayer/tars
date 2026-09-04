using System.Net;
using System.Text.Json;
using Pottmayer.Tars.Ai.Abstractions;

namespace Pottmayer.Tars.Ai.Chat.Gemini;

/// <summary>
/// Turns a non-success Gemini HTTP response into a classified <see cref="AiException"/>. Client errors
/// that a retry cannot fix (bad key, bad request, unknown model) are permanent; rate limits (429) and
/// server errors (5xx) are transient, because a later attempt may succeed.
/// </summary>
internal static class GeminiErrorClassifier
{
    public static AiException Classify(string model, HttpStatusCode status, string body)
    {
        var code = (int)status;
        var permanent = code switch
        {
            400 or 401 or 403 or 404 or 405 or 413 or 422 => true,
            _ => false,
        };

        var message = $"Gemini returned {code} ({status}).";
        if (ExtractErrorMessage(body) is { } detail)
            message += " " + detail;

        return new AiException(
            GeminiAiChatCompletionClient.ProviderName,
            message,
            isPermanent: permanent,
            model: model,
            statusCode: code);
    }

    private static string? ExtractErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var messageElement) &&
                messageElement.ValueKind == JsonValueKind.String)
            {
                return messageElement.GetString();
            }
        }
        catch (JsonException)
        {
            // Not a JSON error envelope; the status code alone will have to do.
        }

        return null;
    }
}
