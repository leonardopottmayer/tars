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
        "Invalid GeminiAiOptions. BaseUrl must be an absolute http(s) URL; RequestTimeout must be positive; "
        + "MaxRetryAttempts must be non-negative; RetryBaseDelay must be non-negative.";

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
    /// How many times a transient failure (503 overloaded, 429 rate limit, other 5xx, or a network error)
    /// is retried before the call gives up. Retries use exponential backoff with jitter and honour the
    /// server's <c>Retry-After</c> header when present. Set to <c>0</c> to disable retrying.
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>
    /// The base delay for the exponential backoff between retry attempts. The effective wait grows per
    /// attempt and carries jitter, and a <c>Retry-After</c> header from the server takes precedence.
    /// </summary>
    public TimeSpan RetryBaseDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Returns <c>true</c> when the options are internally consistent: the base URL is an absolute http(s)
    /// URL, the request timeout is strictly positive, and the retry settings are non-negative. The API key
    /// is not checked — it may be supplied per request.
    /// </summary>
    /// <remarks>
    /// The scheme is checked, not just absoluteness: on Unix <see cref="Uri.TryCreate(string, UriKind, out Uri)"/>
    /// treats a leading-slash path (e.g. <c>/relative/only</c>) as an absolute <c>file</c> URI, so requiring
    /// http/https is what keeps validation consistent across platforms.
    /// </remarks>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl)
            || !Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return false;

        if (RequestTimeout <= TimeSpan.Zero)
            return false;

        if (MaxRetryAttempts < 0 || RetryBaseDelay < TimeSpan.Zero)
            return false;

        return true;
    }
}
