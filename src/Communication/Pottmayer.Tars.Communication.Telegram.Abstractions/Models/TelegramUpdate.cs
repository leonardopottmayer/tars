namespace Pottmayer.Tars.Communication.Telegram.Abstractions.Models;

/// <summary>
/// One inbound update, normalized. Exactly one of <see cref="Message"/> / <see cref="CallbackQuery"/>
/// is set for the update kinds this transport surfaces; both are null for kinds it does not model,
/// which the caller should confirm and ignore rather than fail on.
/// </summary>
public sealed record TelegramUpdate(
    long UpdateId,
    TelegramIncomingMessage? Message = null,
    TelegramCallbackQuery? CallbackQuery = null);

/// <summary>A message sent to the bot: text, or media, or both.</summary>
public sealed record TelegramIncomingMessage(
    long MessageId,
    TelegramChat Chat,
    TelegramSender? From,
    DateTimeOffset SentAt,
    string? Text = null,
    TelegramMedia? Media = null,
    long? ReplyToMessageId = null);

/// <summary>An inline button press. <see cref="Data"/> is the button's <c>callback_data</c>.</summary>
public sealed record TelegramCallbackQuery(
    string Id,
    TelegramSender From,
    string? Data,
    TelegramChat? Chat = null,
    long? MessageId = null);

/// <summary><see cref="Type"/> is <c>private</c>, <c>group</c>, <c>supergroup</c> or <c>channel</c>.</summary>
public sealed record TelegramChat(long Id, string Type);

/// <summary>Who sent an update. Ids are stable; usernames are not.</summary>
public sealed record TelegramSender(
    long Id,
    string? Username = null,
    string? FirstName = null,
    string? LastName = null,
    string? LanguageCode = null,
    bool IsBot = false);

/// <summary>
/// A file attached to a message. <see cref="FileId"/> is what
/// <see cref="ITelegramClient.DownloadFileAsync"/> takes; it is opaque and bot-specific.
/// </summary>
public sealed record TelegramMedia(
    TelegramMediaKind Kind,
    string FileId,
    string? MimeType = null,
    int? DurationSeconds = null,
    long? FileSizeBytes = null,
    string? FileName = null);

/// <summary>The kind of media on an incoming message.</summary>
public enum TelegramMediaKind
{
    Voice = 0,
    Audio = 1,
    Photo = 2,
    Video = 3,
    VideoNote = 4,
    Document = 5,
}
