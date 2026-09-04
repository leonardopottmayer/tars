namespace Pottmayer.Tars.Ai.Chat.Gemini.Options;

/// <summary>
/// Options for the Gemini chat provider, bound from configuration. The API key is optional here: a host
/// that passes each user's key per request (<see cref="Abstractions.Models.ChatRequest.ApiKey"/>) needs
/// no default. When set, the key is sent in the <c>x-goog-api-key</c> header, never in the URL.
/// </summary>
public sealed class GeminiAiOptions
{
    /// <summary>Default configuration section these options bind from (<c>Tars:Ai:Chat:Gemini</c>).</summary>
    public const string SectionName = "Tars:Ai:Chat:Gemini";

    /// <summary>Message reported when validation fails on application start.</summary>
    public const string ValidationErrorMessage =
        "Invalid GeminiAiOptions. BaseUrl must be an absolute URL; RequestTimeout must be positive.";

    /// <summary>The Google AI Studio API key used as the default when a request carries none. Optional.</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>The API root. Defaults to the public Generative Language endpoint.</summary>
    public string BaseUrl { get; init; } = "https://generativelanguage.googleapis.com/";

    /// <summary>
    /// The per-request deadline for the underlying <see cref="HttpClient"/>. A cloud model answers in
    /// seconds, so a bounded default is right here — unlike a local model, which needs no ceiling.
    /// </summary>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(100);

    /// <summary>
    /// Returns <c>true</c> when the options are internally consistent: the base URL is an absolute URI and
    /// the request timeout is strictly positive. The API key is not checked — it may be supplied per request.
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl) || !Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
            return false;

        if (RequestTimeout <= TimeSpan.Zero)
            return false;

        return true;
    }
}
