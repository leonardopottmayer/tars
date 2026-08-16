using Pottmayer.Tars.Communication.Telegram.Abstractions.Models;

namespace Pottmayer.Tars.Communication.Telegram.Abstractions;

/// <summary>
/// Transport for the Telegram Bot API. Stateless: it holds no offset, no queue and no retry policy —
/// the caller owns all three. Implementations throw <see cref="TelegramException"/> on failure, with
/// <see cref="TelegramException.IsPermanent"/> telling the caller whether retrying can ever help.
/// </summary>
public interface ITelegramClient
{
    /// <summary>Sends a message, optionally carrying an inline keyboard.</summary>
    Task<TelegramSendResult> SendMessageAsync(
        TelegramMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acknowledges a callback query so the client stops showing a spinner. Must be called within a
    /// few seconds of receiving the callback, whatever the outcome of handling it.
    /// </summary>
    Task AnswerCallbackQueryAsync(
        string callbackQueryId, string? text = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Long-polls for updates. <paramref name="offset"/> is the id of the first update to return —
    /// pass <c>lastSeenUpdateId + 1</c>, which also confirms every earlier update. Returns as soon as
    /// an update arrives, or empty when <paramref name="pollTimeout"/> elapses.
    /// </summary>
    /// <remarks>
    /// Mutually exclusive with a registered webhook, and only one consumer per bot token: a second
    /// caller gets <c>409 Conflict</c>. Telegram retains unconfirmed updates for 24 hours.
    /// </remarks>
    Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(
        long offset, TimeSpan pollTimeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the content of a file the bot received. The caller owns the returned stream.
    /// </summary>
    /// <remarks>
    /// Intended for small attachments — voice notes, photos, documents within the public Bot API's
    /// 20 MB download limit. An implementation may buffer the whole file, so do not reach for this on
    /// a self-hosted Bot API server, where the limit rises to 2 GB.
    /// </remarks>
    Task<Stream> DownloadFileAsync(string fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a webhook. Telegram sends <paramref name="secretToken"/> back on every request in the
    /// <c>X-Telegram-Bot-Api-Secret-Token</c> header; validate it with
    /// <see cref="TelegramWebhook.IsValidSecretToken"/>.
    /// </summary>
    Task SetWebhookAsync(Uri url, string secretToken, CancellationToken cancellationToken = default);

    /// <summary>Removes the registered webhook, which is what re-enables <see cref="GetUpdatesAsync"/>.</summary>
    Task DeleteWebhookAsync(CancellationToken cancellationToken = default);
}
