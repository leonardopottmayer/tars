namespace Pottmayer.Tars.Communication.Telegram.Options;

/// <summary>Bot API configuration for <see cref="TelegramBotClient"/>.</summary>
public sealed class TelegramOptions
{
    /// <summary>Default configuration section these options bind from (<c>Tars:Communication:Telegram</c>).</summary>
    public const string SectionName = "Tars:Communication:Telegram";

    /// <summary>Message reported when validation fails on application start.</summary>
    public const string ValidationErrorMessage =
        "Invalid TelegramOptions. BotToken is required; ApiBaseUrl must be an absolute URI; RequestTimeout must be positive; PollTimeoutGrace must not be negative.";

    /// <summary>The bot token from BotFather. Required.</summary>
    public string BotToken { get; set; } = string.Empty;

    /// <summary>
    /// Bot API root. Override only when running a self-hosted Bot API server, which raises the file
    /// size limits.
    /// </summary>
    /// <remarks>
    /// Raising those limits does not make <see cref="TelegramBotClient.DownloadFileAsync"/> able to
    /// use them: it buffers the whole file in memory, which is only sound under the public API's
    /// 20 MB ceiling. Self-hosting to move large files needs a streaming download first.
    /// </remarks>
    public string ApiBaseUrl { get; set; } = "https://api.telegram.org";

    /// <summary>Per-request timeout for everything except long polling. Default: 30 seconds.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Slack added on top of the caller's poll timeout before the HTTP request itself is cancelled, so
    /// a long poll is never killed by its own transport. Default: 10 seconds.
    /// </summary>
    public TimeSpan PollTimeoutGrace { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Returns <c>true</c> when the options are internally consistent: a bot token is present,
    /// <see cref="ApiBaseUrl"/> is an absolute URI, <see cref="RequestTimeout"/> is strictly positive and
    /// <see cref="PollTimeoutGrace"/> is not negative.
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(BotToken))
            return false;

        if (!Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out _))
            return false;

        if (RequestTimeout <= TimeSpan.Zero)
            return false;

        if (PollTimeoutGrace < TimeSpan.Zero)
            return false;

        return true;
    }
}
