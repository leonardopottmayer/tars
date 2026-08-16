namespace Pottmayer.Tars.Communication.Telegram.Options;

/// <summary>Bot API configuration for <see cref="TelegramBotClient"/>.</summary>
public sealed class TelegramOptions
{
    public const string SectionName = "Tars:Communication:Telegram";

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
}
