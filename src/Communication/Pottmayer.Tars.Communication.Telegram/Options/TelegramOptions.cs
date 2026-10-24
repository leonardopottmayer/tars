namespace Pottmayer.Tars.Communication.Telegram.Options;

/// <summary>
/// The Telegram configuration: a set of named bots, each with its own token. One application can run
/// several at once — e.g. a <c>notifications</c> bot that only sends and an <c>assistant</c> bot that
/// also polls for inbound — and resolve one by name through <c>ITelegramClientFactory.GetClient</c>.
/// A single-bot application is simply one entry in <see cref="Bots"/>.
/// </summary>
public sealed class TelegramOptions
{
    /// <summary>Default configuration section these options bind from (<c>Tars:Communication:Telegram</c>).</summary>
    public const string SectionName = "Tars:Communication:Telegram";

    /// <summary>Message reported when validation fails on application start.</summary>
    public const string ValidationErrorMessage =
        "Invalid TelegramOptions. Every bot under Bots needs a BotToken; ApiBaseUrl must be an absolute URI; RequestTimeout must be positive; PollTimeoutGrace must not be negative.";

    /// <summary>
    /// The configured bots, keyed by name. The key is what <c>ITelegramClientFactory.GetClient</c> takes,
    /// and it is bound case-insensitively.
    /// </summary>
    public Dictionary<string, TelegramBotOptions> Bots { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Masks the bot token (<c>/bot***/</c>) in the request URLs recorded by the HttpClient logs and, with
    /// <c>AddTarsHttpClientTracing</c>, by the spans. The Bot API puts the token in the URL path, so turning
    /// this off writes it to every log line and span. Default: <c>true</c>.
    /// </summary>
    public bool RedactBotToken { get; set; } = true;

    /// <summary>
    /// Returns <c>true</c> when every configured bot is valid. An empty set is valid — the feature is
    /// simply off, and asking the factory for a bot that is not configured is what fails, at the call.
    /// </summary>
    public bool IsValid() => Bots.Values.All(bot => bot is not null && bot.IsValid());
}
